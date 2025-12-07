//------------------------------------------------------------------------------ 
// <copyright file="DataBindingCollectionConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.Globalization;

    /// <include file='doc\DataBindingCollectionConverter.uex' path='docs/doc[@for="DataBindingCollectionConverter"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides conversion functions for data binding collections. 
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [Obsolete("Use of this type is not recommended because DataBindings editing is launched via a DesignerActionList instead of the property grid. http://go.microsoft.com/fwlink/?linkid=14202")]
    public class DataBindingCollectionConverter : TypeConverter {

        /// <include file='doc\DataBindingCollectionConverter.uex' path='docs/doc[@for="DataBindingCollectionConverter.ConvertTo"]/*' /> 
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
// <copyright file="DataBindingCollectionConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.Globalization;

    /// <include file='doc\DataBindingCollectionConverter.uex' path='docs/doc[@for="DataBindingCollectionConverter"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides conversion functions for data binding collections. 
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [Obsolete("Use of this type is not recommended because DataBindings editing is launched via a DesignerActionList instead of the property grid. http://go.microsoft.com/fwlink/?linkid=14202")]
    public class DataBindingCollectionConverter : TypeConverter {

        /// <include file='doc\DataBindingCollectionConverter.uex' path='docs/doc[@for="DataBindingCollectionConverter.ConvertTo"]/*' /> 
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
