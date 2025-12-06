//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewColumnTypeEditor.cs" company="Microsoft">
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

    internal class DataGridViewColumnTypeEditor : UITypeEditor { 
 
        // FxCop made me add this constructor.
        private DataGridViewColumnTypeEditor() : base() {} 

        DataGridViewColumnTypePicker columnTypePicker = null;

        public override bool IsDropDownResizable { 
            get {
                return true; 
            } 
        }
 
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) {
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
 
                if (edSvc != null && context.Instance != null) {
                    if (this.columnTypePicker == null) { 
                        this.columnTypePicker = new DataGridViewColumnTypePicker(); 
                    }
 
                    DataGridViewColumnCollectionDialog.ListBoxItem item = (DataGridViewColumnCollectionDialog.ListBoxItem) context.Instance;

                    IDesignerHost host = (IDesignerHost) provider.GetService(typeof(IDesignerHost));
                    ITypeDiscoveryService discoveryService = null; 
                    if (host != null)
                    { 
                        discoveryService = (ITypeDiscoveryService) host.GetService(typeof(ITypeDiscoveryService)); 
                    }
 
                    columnTypePicker.Start(edSvc, discoveryService, item.DataGridViewColumn.GetType());
                    edSvc.DropDownControl(columnTypePicker);
                    if (columnTypePicker.SelectedType != null) {
                        value = columnTypePicker.SelectedType; 
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
// <copyright file="DataGridViewColumnTypeEditor.cs" company="Microsoft">
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

    internal class DataGridViewColumnTypeEditor : UITypeEditor { 
 
        // FxCop made me add this constructor.
        private DataGridViewColumnTypeEditor() : base() {} 

        DataGridViewColumnTypePicker columnTypePicker = null;

        public override bool IsDropDownResizable { 
            get {
                return true; 
            } 
        }
 
        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) {
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
 
                if (edSvc != null && context.Instance != null) {
                    if (this.columnTypePicker == null) { 
                        this.columnTypePicker = new DataGridViewColumnTypePicker(); 
                    }
 
                    DataGridViewColumnCollectionDialog.ListBoxItem item = (DataGridViewColumnCollectionDialog.ListBoxItem) context.Instance;

                    IDesignerHost host = (IDesignerHost) provider.GetService(typeof(IDesignerHost));
                    ITypeDiscoveryService discoveryService = null; 
                    if (host != null)
                    { 
                        discoveryService = (ITypeDiscoveryService) host.GetService(typeof(ITypeDiscoveryService)); 
                    }
 
                    columnTypePicker.Start(edSvc, discoveryService, item.DataGridViewColumn.GetType());
                    edSvc.DropDownControl(columnTypePicker);
                    if (columnTypePicker.SelectedType != null) {
                        value = columnTypePicker.SelectedType; 
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
