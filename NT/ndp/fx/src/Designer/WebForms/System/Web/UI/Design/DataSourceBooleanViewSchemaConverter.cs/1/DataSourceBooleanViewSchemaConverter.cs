//------------------------------------------------------------------------------ 
// <copyright file="DataSourceBooleanViewSchemaConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.Runtime.InteropServices; 
    using System.Web.UI;
 
 
    /// <devdoc>
    ///    <para> 
    ///       Provides design-time support for getting schema from an object
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class DataSourceBooleanViewSchemaConverter : DataSourceViewSchemaConverter {
 
        /// <devdoc> 
        /// </devdoc>
        public DataSourceBooleanViewSchemaConverter() { 
        }


        /// <devdoc> 
        ///    <para>
        ///       Gets the fields present within the selected object's schema 
        ///    </para> 
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
            return GetStandardValues(context, typeof(bool));
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataSourceBooleanViewSchemaConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.Runtime.InteropServices; 
    using System.Web.UI;
 
 
    /// <devdoc>
    ///    <para> 
    ///       Provides design-time support for getting schema from an object
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class DataSourceBooleanViewSchemaConverter : DataSourceViewSchemaConverter {
 
        /// <devdoc> 
        /// </devdoc>
        public DataSourceBooleanViewSchemaConverter() { 
        }


        /// <devdoc> 
        ///    <para>
        ///       Gets the fields present within the selected object's schema 
        ///    </para> 
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
            return GetStandardValues(context, typeof(bool));
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
