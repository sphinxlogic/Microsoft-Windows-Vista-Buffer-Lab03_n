//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceQueryConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Design;
    using System.Diagnostics; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Globalization;
 
    /// <devdoc>
    /// Provides a type converter to convert query properties to a simple string. 
    /// </devdoc> 
    internal class SqlDataSourceQueryConverter : TypeConverter {
 
        /// <devdoc>
        /// Converts the given value object to the specified destination type.
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) { 
            if (destinationType == typeof(string)) {
                return SR.GetString(SR.SqlDataSourceQueryConverter_Text); 
            } 
            else {
                return base.ConvertTo(context, culture, value, destinationType); 
            }
        }

        /// <devdoc> 
        /// Gets a collection of properties for the type of array specified by the value
        /// parameter using the specified context and attributes. 
        /// </devdoc> 
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) {
            return null; 
        }

        /// <devdoc>
        /// Gets a value indicating whether this object supports properties. 
        /// </devdoc>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) { 
            return false; 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceQueryConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Design;
    using System.Diagnostics; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Globalization;
 
    /// <devdoc>
    /// Provides a type converter to convert query properties to a simple string. 
    /// </devdoc> 
    internal class SqlDataSourceQueryConverter : TypeConverter {
 
        /// <devdoc>
        /// Converts the given value object to the specified destination type.
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) { 
            if (destinationType == typeof(string)) {
                return SR.GetString(SR.SqlDataSourceQueryConverter_Text); 
            } 
            else {
                return base.ConvertTo(context, culture, value, destinationType); 
            }
        }

        /// <devdoc> 
        /// Gets a collection of properties for the type of array specified by the value
        /// parameter using the specified context and attributes. 
        /// </devdoc> 
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) {
            return null; 
        }

        /// <devdoc>
        /// Gets a value indicating whether this object supports properties. 
        /// </devdoc>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) { 
            return false; 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
