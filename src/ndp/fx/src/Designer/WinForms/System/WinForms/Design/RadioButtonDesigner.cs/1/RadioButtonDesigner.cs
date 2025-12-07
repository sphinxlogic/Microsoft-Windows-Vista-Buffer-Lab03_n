//------------------------------------------------------------------------------ 
// <copyright file="ButtonBaseDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Drawing;
    using System.Windows.Forms; 
    using System.Diagnostics; 

    /// <include file='doc\ButtonBaseDesigner.uex' path='docs/doc[@for="ButtonBaseDesigner"]/*' /> 
    /// <devdoc>
    ///    <para>
    ///       Provides a designer that can design components
    ///       that extend ButtonBase.</para> 
    /// </devdoc>
    internal class RadioButtonDesigner: ButtonBaseDesigner { 
 

        public override void InitializeNewComponent(IDictionary defaultValues) { 
           base.InitializeNewComponent(defaultValues);

           // In Whidbey, default the TabStop to true.
           PropertyDescriptor prop = TypeDescriptor.GetProperties(Component)["TabStop"]; 
           if (prop != null && prop.PropertyType == typeof(bool) && !prop.IsReadOnly && prop.IsBrowsable) {
               prop.SetValue(Component, true); 
           } 

       } 

    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ButtonBaseDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Drawing;
    using System.Windows.Forms; 
    using System.Diagnostics; 

    /// <include file='doc\ButtonBaseDesigner.uex' path='docs/doc[@for="ButtonBaseDesigner"]/*' /> 
    /// <devdoc>
    ///    <para>
    ///       Provides a designer that can design components
    ///       that extend ButtonBase.</para> 
    /// </devdoc>
    internal class RadioButtonDesigner: ButtonBaseDesigner { 
 

        public override void InitializeNewComponent(IDictionary defaultValues) { 
           base.InitializeNewComponent(defaultValues);

           // In Whidbey, default the TabStop to true.
           PropertyDescriptor prop = TypeDescriptor.GetProperties(Component)["TabStop"]; 
           if (prop != null && prop.PropertyType == typeof(bool) && !prop.IsReadOnly && prop.IsBrowsable) {
               prop.SetValue(Component, true); 
           } 

       } 

    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
