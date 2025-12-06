//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataTableBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 

    /// <devdoc>
    /// Represents a base table in a data connection. DesignerDataTable and
    /// DesignerDataView derive from this class. 
    /// </devdoc>
    public abstract class DesignerDataTableBase { 
 
        private ICollection _columns;
        private string _name; 
        private string _owner;

        /// <devdoc>
        /// </devdoc> 
        protected DesignerDataTableBase(string name) {
            _name = name; 
        } 

        /// <devdoc> 
        /// </devdoc>
        protected DesignerDataTableBase(string name, string owner) {
            _name = name;
            _owner = owner; 
        }
 
        /// <devdoc> 
        /// The collection of columns in the table.
        /// </devdoc> 
        public ICollection Columns {
            get {
                if (_columns == null) {
                    _columns = CreateColumns(); 
                }
                return _columns; 
            } 
        }
 
        /// <devdoc>
        /// The name of the table.
        /// </devdoc>
        public string Name { 
            get {
                return _name; 
            } 
        }
 
        /// <devdoc>
        /// The owner of the table.
        /// </devdoc>
        public string Owner { 
            get {
                return _owner; 
            } 
        }
 
        /// <devdoc>
        /// This method will be called the first time the Columns property
        /// is accessed. It should return a collection of DesignerDataColumn
        /// objects representing this table's columns. 
        /// </devdoc>
        protected abstract ICollection CreateColumns(); 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataTableBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 

    /// <devdoc>
    /// Represents a base table in a data connection. DesignerDataTable and
    /// DesignerDataView derive from this class. 
    /// </devdoc>
    public abstract class DesignerDataTableBase { 
 
        private ICollection _columns;
        private string _name; 
        private string _owner;

        /// <devdoc>
        /// </devdoc> 
        protected DesignerDataTableBase(string name) {
            _name = name; 
        } 

        /// <devdoc> 
        /// </devdoc>
        protected DesignerDataTableBase(string name, string owner) {
            _name = name;
            _owner = owner; 
        }
 
        /// <devdoc> 
        /// The collection of columns in the table.
        /// </devdoc> 
        public ICollection Columns {
            get {
                if (_columns == null) {
                    _columns = CreateColumns(); 
                }
                return _columns; 
            } 
        }
 
        /// <devdoc>
        /// The name of the table.
        /// </devdoc>
        public string Name { 
            get {
                return _name; 
            } 
        }
 
        /// <devdoc>
        /// The owner of the table.
        /// </devdoc>
        public string Owner { 
            get {
                return _owner; 
            } 
        }
 
        /// <devdoc>
        /// This method will be called the first time the Columns property
        /// is accessed. It should return a collection of DesignerDataColumn
        /// objects representing this table's columns. 
        /// </devdoc>
        protected abstract ICollection CreateColumns(); 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
