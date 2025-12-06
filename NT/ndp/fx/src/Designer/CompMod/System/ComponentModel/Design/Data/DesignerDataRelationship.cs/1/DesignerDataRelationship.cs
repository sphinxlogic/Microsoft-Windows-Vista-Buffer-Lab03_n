//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataRelationship.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 

    /// <devdoc>
    /// Represents a 1-to-1 or 1-to-many relationship between two tables in a
    /// data connection. A collection of this type is returned from the 
    /// DesignerDataTable.Relationships property.
    /// </devdoc> 
    public sealed class DesignerDataRelationship { 

        private ICollection _childColumns; 
        private DesignerDataTable _childTable;
        private string _name;
        private ICollection _parentColumns;
 
        /// <devdoc>
        /// </devdoc> 
        public DesignerDataRelationship(string name, ICollection parentColumns, DesignerDataTable childTable, ICollection childColumns) { 
            _childColumns = childColumns;
            _childTable = childTable; 
            _name = name;
            _parentColumns = parentColumns;
        }
 
        /// <devdoc>
        /// The columns in the child table that are part of the relationship. 
        /// </devdoc> 
        public ICollection ChildColumns {
            get { 
                return _childColumns;
            }
        }
 
        /// <devdoc>
        /// The child table referenced by this relationship. 
        /// </devdoc> 
        public DesignerDataTable ChildTable {
            get { 
                return _childTable;
            }
        }
 
        /// <devdoc>
        /// The name of the relationship, if any. 
        /// </devdoc> 
        public string Name {
            get { 
                return _name;
            }
        }
 
        /// <devdoc>
        /// The columns in the parent table that are part of the relationship. 
        /// </devdoc> 
        public ICollection ParentColumns {
            get { 
                return _parentColumns;
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataRelationship.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 

    /// <devdoc>
    /// Represents a 1-to-1 or 1-to-many relationship between two tables in a
    /// data connection. A collection of this type is returned from the 
    /// DesignerDataTable.Relationships property.
    /// </devdoc> 
    public sealed class DesignerDataRelationship { 

        private ICollection _childColumns; 
        private DesignerDataTable _childTable;
        private string _name;
        private ICollection _parentColumns;
 
        /// <devdoc>
        /// </devdoc> 
        public DesignerDataRelationship(string name, ICollection parentColumns, DesignerDataTable childTable, ICollection childColumns) { 
            _childColumns = childColumns;
            _childTable = childTable; 
            _name = name;
            _parentColumns = parentColumns;
        }
 
        /// <devdoc>
        /// The columns in the child table that are part of the relationship. 
        /// </devdoc> 
        public ICollection ChildColumns {
            get { 
                return _childColumns;
            }
        }
 
        /// <devdoc>
        /// The child table referenced by this relationship. 
        /// </devdoc> 
        public DesignerDataTable ChildTable {
            get { 
                return _childTable;
            }
        }
 
        /// <devdoc>
        /// The name of the relationship, if any. 
        /// </devdoc> 
        public string Name {
            get { 
                return _name;
            }
        }
 
        /// <devdoc>
        /// The columns in the parent table that are part of the relationship. 
        /// </devdoc> 
        public ICollection ParentColumns {
            get { 
                return _parentColumns;
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
