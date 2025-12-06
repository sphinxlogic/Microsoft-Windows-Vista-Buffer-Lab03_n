//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 

    /// <devdoc>
    /// Represents a single view in a data connection. A collection of this
    /// type is returned from IDesignerDataSchema.GetSchemaItems when it is 
    /// passed DesignerDataSchemaClass.Views.
    /// </devdoc> 
    public abstract class DesignerDataView : DesignerDataTableBase { 

        /// <devdoc> 
        /// </devdoc>
        protected DesignerDataView(string name) : base(name) {
        }
 
        /// <devdoc>
        /// </devdoc> 
        protected DesignerDataView(string name, string owner) : base(name, owner) { 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Collections; 

    /// <devdoc>
    /// Represents a single view in a data connection. A collection of this
    /// type is returned from IDesignerDataSchema.GetSchemaItems when it is 
    /// passed DesignerDataSchemaClass.Views.
    /// </devdoc> 
    public abstract class DesignerDataView : DesignerDataTableBase { 

        /// <devdoc> 
        /// </devdoc>
        protected DesignerDataView(string name) : base(name) {
        }
 
        /// <devdoc>
        /// </devdoc> 
        protected DesignerDataView(string name, string owner) : base(name, owner) { 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
