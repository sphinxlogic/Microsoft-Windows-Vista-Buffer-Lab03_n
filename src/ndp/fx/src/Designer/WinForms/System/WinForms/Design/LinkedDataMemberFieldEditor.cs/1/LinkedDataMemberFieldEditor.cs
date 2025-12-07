//------------------------------------------------------------------------------ 
// <copyright file="LinkedDataMemberFieldEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.LinkedDataMemberFieldEditor..ctor()")] 
 

namespace System.Windows.Forms.Design { 

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Design; 
 
    internal class LinkedDataMemberFieldEditor : UITypeEditor {
 
        private DesignBindingPicker designBindingPicker;

        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) {
            if (provider != null && context.Instance != null) { 
                PropertyDescriptor dataSourceProperty = TypeDescriptor.GetProperties(context.Instance)["LinkedDataSource"];
                if (dataSourceProperty != null) { 
                    object dataSource = dataSourceProperty.GetValue(context.Instance); 
                    if (dataSource != null) {
                        if (designBindingPicker == null) { 
                            designBindingPicker = new DesignBindingPicker();
                        }
                        DesignBinding oldSelection = new DesignBinding(null, (string) value);
                        DesignBinding newSelection = designBindingPicker.Pick(context, provider, 
                                                                              false, /* showDataSources   */
                                                                              true,  /* showDataMembers   */ 
                                                                              false, /* selectListMembers */ 
                                                                              dataSource, String.Empty,
                                                                              oldSelection); 
                        if (newSelection != null) {
                            value = newSelection.DataMember;
                        }
                    } 
                }
            } 
 
            return value;
        } 

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.DropDown;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="LinkedDataMemberFieldEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.LinkedDataMemberFieldEditor..ctor()")] 
 

namespace System.Windows.Forms.Design { 

    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Design; 
 
    internal class LinkedDataMemberFieldEditor : UITypeEditor {
 
        private DesignBindingPicker designBindingPicker;

        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) {
            if (provider != null && context.Instance != null) { 
                PropertyDescriptor dataSourceProperty = TypeDescriptor.GetProperties(context.Instance)["LinkedDataSource"];
                if (dataSourceProperty != null) { 
                    object dataSource = dataSourceProperty.GetValue(context.Instance); 
                    if (dataSource != null) {
                        if (designBindingPicker == null) { 
                            designBindingPicker = new DesignBindingPicker();
                        }
                        DesignBinding oldSelection = new DesignBinding(null, (string) value);
                        DesignBinding newSelection = designBindingPicker.Pick(context, provider, 
                                                                              false, /* showDataSources   */
                                                                              true,  /* showDataMembers   */ 
                                                                              false, /* selectListMembers */ 
                                                                              dataSource, String.Empty,
                                                                              oldSelection); 
                        if (newSelection != null) {
                            value = newSelection.DataMember;
                        }
                    } 
                }
            } 
 
            return value;
        } 

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.DropDown;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
