//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataStoredProcedure.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 

    /// <devdoc>
    /// Represents a single stored procedure in a data connection. A
    /// collection of this type is returned from 
    /// IDesignerDataSchema.GetSchemaItems when it is passed
    /// DesignerDataSchemaClass.StoredProcedures. 
    /// </devdoc> 
    public abstract class DesignerDataStoredProcedure {
 
        private string _name;
        private string _owner;
        private ICollection _parameters;
 
        /// <devdoc>
        /// </devdoc> 
        protected DesignerDataStoredProcedure(string name) { 
            _name = name;
        } 

        /// <devdoc>
        /// </devdoc>
        protected DesignerDataStoredProcedure(string name, string owner) { 
            _name = name;
            _owner = owner; 
        } 

        /// <devdoc> 
        /// The name of the stored procedure.
        /// </devdoc>
        public string Name {
            get { 
                return _name;
            } 
        } 

        /// <devdoc> 
        /// The owner of the stored procedure.
        /// </devdoc>
        public string Owner {
            get { 
                return _owner;
            } 
        } 

        /// <devdoc> 
        /// The collection of parameters accepted by the stored procedure.
        /// </devdoc>
        public ICollection Parameters {
            get { 
                if (_parameters == null) {
                    _parameters = CreateParameters(); 
                } 
                return _parameters;
            } 
        }

        /// <devdoc>
        /// This method will be called the first time the Parameters property 
        /// is accessed. It should return a collection of
        /// DesignerDataParameter objects representing this stored procedure's 
        /// parameters. If there are no parameters, it should return an empty 
        /// collection (not null).
        /// </devdoc> 
        protected abstract ICollection CreateParameters();
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataStoredProcedure.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 

    /// <devdoc>
    /// Represents a single stored procedure in a data connection. A
    /// collection of this type is returned from 
    /// IDesignerDataSchema.GetSchemaItems when it is passed
    /// DesignerDataSchemaClass.StoredProcedures. 
    /// </devdoc> 
    public abstract class DesignerDataStoredProcedure {
 
        private string _name;
        private string _owner;
        private ICollection _parameters;
 
        /// <devdoc>
        /// </devdoc> 
        protected DesignerDataStoredProcedure(string name) { 
            _name = name;
        } 

        /// <devdoc>
        /// </devdoc>
        protected DesignerDataStoredProcedure(string name, string owner) { 
            _name = name;
            _owner = owner; 
        } 

        /// <devdoc> 
        /// The name of the stored procedure.
        /// </devdoc>
        public string Name {
            get { 
                return _name;
            } 
        } 

        /// <devdoc> 
        /// The owner of the stored procedure.
        /// </devdoc>
        public string Owner {
            get { 
                return _owner;
            } 
        } 

        /// <devdoc> 
        /// The collection of parameters accepted by the stored procedure.
        /// </devdoc>
        public ICollection Parameters {
            get { 
                if (_parameters == null) {
                    _parameters = CreateParameters(); 
                } 
                return _parameters;
            } 
        }

        /// <devdoc>
        /// This method will be called the first time the Parameters property 
        /// is accessed. It should return a collection of
        /// DesignerDataParameter objects representing this stored procedure's 
        /// parameters. If there are no parameters, it should return an empty 
        /// collection (not null).
        /// </devdoc> 
        protected abstract ICollection CreateParameters();
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
