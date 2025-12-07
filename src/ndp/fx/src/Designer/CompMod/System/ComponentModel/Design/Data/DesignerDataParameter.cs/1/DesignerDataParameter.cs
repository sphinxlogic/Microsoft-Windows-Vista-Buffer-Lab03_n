//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataParameter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 
    using System.Data;

    /// <devdoc>
    /// Represents a parameter of a stored procedure in a data connection. 
    /// A collection of this type is returned from the
    /// DesignerStoredProcedure.Parameters property. 
    /// </devdoc> 
    public sealed class DesignerDataParameter {
 
        private DbType _dataType;
        private ParameterDirection _direction;
        private string _name;
 
        /// <devdoc>
        /// </devdoc> 
        public DesignerDataParameter(string name, DbType dataType, ParameterDirection direction) { 
            _dataType = dataType;
            _direction = direction; 
            _name = name;
        }

        /// <devdoc> 
        /// The type of the parameter.
        /// </devdoc> 
        public DbType DataType { 
            get {
                return _dataType; 
            }
        }

        /// <devdoc> 
        /// The in/out semantics of the parameter.
        /// </devdoc> 
        public ParameterDirection Direction { 
            get {
                return _direction; 
            }
        }

        /// <devdoc> 
        /// The name of the parameter.
        /// </devdoc> 
        public string Name { 
            get {
                return _name; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataParameter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 
    using System.Data;

    /// <devdoc>
    /// Represents a parameter of a stored procedure in a data connection. 
    /// A collection of this type is returned from the
    /// DesignerStoredProcedure.Parameters property. 
    /// </devdoc> 
    public sealed class DesignerDataParameter {
 
        private DbType _dataType;
        private ParameterDirection _direction;
        private string _name;
 
        /// <devdoc>
        /// </devdoc> 
        public DesignerDataParameter(string name, DbType dataType, ParameterDirection direction) { 
            _dataType = dataType;
            _direction = direction; 
            _name = name;
        }

        /// <devdoc> 
        /// The type of the parameter.
        /// </devdoc> 
        public DbType DataType { 
            get {
                return _dataType; 
            }
        }

        /// <devdoc> 
        /// The in/out semantics of the parameter.
        /// </devdoc> 
        public ParameterDirection Direction { 
            get {
                return _direction; 
            }
        }

        /// <devdoc> 
        /// The name of the parameter.
        /// </devdoc> 
        public string Name { 
            get {
                return _name; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
