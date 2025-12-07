//------------------------------------------------------------------------------ 
// <copyright file="DataGridColumnStyleMappingNameEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System.Design;
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Drawing;
    using System.Drawing.Design; 

    internal class DataGridColumnStyleMappingNameEditor : UITypeEditor { 
 
        // FxCop made me add this constructor
        private DataGridColumnStyleMappingNameEditor() : base() {} 
        private DesignBindingPicker designBindingPicker;

        public override bool IsDropDownResizable {
            get { 
                return true;
            } 
        } 

        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) { 
            if (provider != null && context.Instance != null) {
                object instance = context.Instance;
                DataGridColumnStyle columnStyle = (DataGridColumnStyle) context.Instance;
                if (columnStyle.DataGridTableStyle == null || columnStyle.DataGridTableStyle.DataGrid == null) 
                    return value;
                PropertyDescriptor dataSourceProperty = TypeDescriptor.GetProperties(columnStyle.DataGridTableStyle.DataGrid)["DataSource"]; 
                if (dataSourceProperty != null) { 
                    object dataSource = dataSourceProperty.GetValue(columnStyle.DataGridTableStyle.DataGrid);
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
                    if (dataSource != null && newSelection != null) {
                        if (String.IsNullOrEmpty(newSelection.DataMember) || newSelection.DataMember == null)
                            value = "";
                        else 
                            value = newSelection.DataField;
 
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
// <copyright file="DataGridColumnStyleMappingNameEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System.Design;
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Drawing;
    using System.Drawing.Design; 

    internal class DataGridColumnStyleMappingNameEditor : UITypeEditor { 
 
        // FxCop made me add this constructor
        private DataGridColumnStyleMappingNameEditor() : base() {} 
        private DesignBindingPicker designBindingPicker;

        public override bool IsDropDownResizable {
            get { 
                return true;
            } 
        } 

        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) { 
            if (provider != null && context.Instance != null) {
                object instance = context.Instance;
                DataGridColumnStyle columnStyle = (DataGridColumnStyle) context.Instance;
                if (columnStyle.DataGridTableStyle == null || columnStyle.DataGridTableStyle.DataGrid == null) 
                    return value;
                PropertyDescriptor dataSourceProperty = TypeDescriptor.GetProperties(columnStyle.DataGridTableStyle.DataGrid)["DataSource"]; 
                if (dataSourceProperty != null) { 
                    object dataSource = dataSourceProperty.GetValue(columnStyle.DataGridTableStyle.DataGrid);
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
                    if (dataSource != null && newSelection != null) {
                        if (String.IsNullOrEmpty(newSelection.DataMember) || newSelection.DataMember == null)
                            value = "";
                        else 
                            value = newSelection.DataField;
 
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
