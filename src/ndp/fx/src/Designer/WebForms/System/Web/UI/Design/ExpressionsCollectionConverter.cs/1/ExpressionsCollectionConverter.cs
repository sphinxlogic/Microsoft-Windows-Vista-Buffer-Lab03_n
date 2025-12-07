//------------------------------------------------------------------------------ 
// <copyright file="ExpressionsCollectionConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.Globalization;

    /// <include file='doc\ExpressionsCollectionConverter.uex' path='docs/doc[@for="ExpressionsCollectionConverter"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides conversion functions for data expression collections. 
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ExpressionsCollectionConverter : TypeConverter {

        /// <include file='doc\ExpressionsCollectionConverter.uex' path='docs/doc[@for="ExpressionsCollectionConverter.ConvertTo"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Converts a data binding collection to the specified type. 
        ///    </para> 
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) { 
            if (destinationType == typeof(string)) {
                return String.Empty;
            }
            else { 
                return base.ConvertTo(context, culture, value, destinationType);
            } 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ExpressionsCollectionConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.Globalization;

    /// <include file='doc\ExpressionsCollectionConverter.uex' path='docs/doc[@for="ExpressionsCollectionConverter"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides conversion functions for data expression collections. 
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ExpressionsCollectionConverter : TypeConverter {

        /// <include file='doc\ExpressionsCollectionConverter.uex' path='docs/doc[@for="ExpressionsCollectionConverter.ConvertTo"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Converts a data binding collection to the specified type. 
        ///    </para> 
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) { 
            if (destinationType == typeof(string)) {
                return String.Empty;
            }
            else { 
                return base.ConvertTo(context, culture, value, destinationType);
            } 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
