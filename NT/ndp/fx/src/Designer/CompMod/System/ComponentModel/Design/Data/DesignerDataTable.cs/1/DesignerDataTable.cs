//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataTable.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 

    /// <devdoc>
    /// Represents a single table in a data connection. A collection of this
    /// type is returned from IDesignerDataSchema.GetSchemaItems when it is 
    /// passed DesignerDataSchemaClass.Tables.
    /// </devdoc> 
    public abstract class DesignerDataTable : DesignerDataTableBase { 

        private ICollection _relationships; 

        /// <devdoc>
        /// </devdoc>
        protected DesignerDataTable(string name) : base(name) { 
        }
 
        /// <devdoc> 
        /// </devdoc>
        protected DesignerDataTable(string name, string owner) : base(name, owner) { 
        }

        /// <devdoc>
        /// The collection of relationships in the table. 
        /// </devdoc>
        public ICollection Relationships { 
            get { 
                if (_relationships == null) {
                    _relationships = CreateRelationships(); 
                }
                return _relationships;
            }
        } 

        /// <devdoc> 
        /// This method will be called the first time the Relationships 
        /// property is accessed. It should return a collection of
        /// DesignerDataRelationship objects representing this table's 
        /// columns.
        /// </devdoc>
        protected abstract ICollection CreateRelationships();
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataTable.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 

    /// <devdoc>
    /// Represents a single table in a data connection. A collection of this
    /// type is returned from IDesignerDataSchema.GetSchemaItems when it is 
    /// passed DesignerDataSchemaClass.Tables.
    /// </devdoc> 
    public abstract class DesignerDataTable : DesignerDataTableBase { 

        private ICollection _relationships; 

        /// <devdoc>
        /// </devdoc>
        protected DesignerDataTable(string name) : base(name) { 
        }
 
        /// <devdoc> 
        /// </devdoc>
        protected DesignerDataTable(string name, string owner) : base(name, owner) { 
        }

        /// <devdoc>
        /// The collection of relationships in the table. 
        /// </devdoc>
        public ICollection Relationships { 
            get { 
                if (_relationships == null) {
                    _relationships = CreateRelationships(); 
                }
                return _relationships;
            }
        } 

        /// <devdoc> 
        /// This method will be called the first time the Relationships 
        /// property is accessed. It should return a collection of
        /// DesignerDataRelationship objects representing this table's 
        /// columns.
        /// </devdoc>
        protected abstract ICollection CreateRelationships();
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
