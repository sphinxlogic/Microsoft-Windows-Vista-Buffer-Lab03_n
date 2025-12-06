namespace System.Web.UI.Design.WebControls { 
    using System;
    using System.ComponentModel;

    public class TreeNodeBindingDepthConverter : Int32Converter { 
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            string strValue = value as string; 
            if ((strValue != null) && (strValue.Length == 0)) { 
                return -1;
            } 

            return base.ConvertFrom(context, culture, value);
        }
 
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            if ((value is int) && ((int)value == -1)) { 
                return String.Empty; 
            }
 
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Web.UI.Design.WebControls { 
    using System;
    using System.ComponentModel;

    public class TreeNodeBindingDepthConverter : Int32Converter { 
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            string strValue = value as string; 
            if ((strValue != null) && (strValue.Length == 0)) { 
                return -1;
            } 

            return base.ConvertFrom(context, culture, value);
        }
 
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            if ((value is int) && ((int)value == -1)) { 
                return String.Empty; 
            }
 
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
