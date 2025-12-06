//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewColumnCollectionEditor.cs" company="Microsoft">
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

    internal class DataGridViewColumnCollectionEditor : UITypeEditor { 
 
        // FxCop made me add this constructor
        private DataGridViewColumnCollectionEditor() : base() {} 

        DataGridViewColumnCollectionDialog dataGridViewColumnCollectionDialog;

        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) { 
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 
 
                if (edSvc != null && context.Instance != null) {
                    IDesignerHost host = (IDesignerHost)provider.GetService(typeof(IDesignerHost)); 
                    if (host == null)
                    {
                        return value;
                    } 

                    if (dataGridViewColumnCollectionDialog == null) { 
                        dataGridViewColumnCollectionDialog = new DataGridViewColumnCollectionDialog(); 
                    }
 
                    dataGridViewColumnCollectionDialog.SetLiveDataGridView((DataGridView) context.Instance);

                    using(DesignerTransaction trans = host.CreateTransaction(SR.GetString(SR.DataGridViewColumnCollectionTransaction)))
                    { 
                        if (edSvc.ShowDialog(dataGridViewColumnCollectionDialog) == DialogResult.OK)
                        { 
                            trans.Commit(); 
                        }
                        else 
                        {
                            trans.Cancel();
                        }
                    } 
                }
            } 
 
            return value;
        } 

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewColumnCollectionEditor.cs" company="Microsoft">
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

    internal class DataGridViewColumnCollectionEditor : UITypeEditor { 
 
        // FxCop made me add this constructor
        private DataGridViewColumnCollectionEditor() : base() {} 

        DataGridViewColumnCollectionDialog dataGridViewColumnCollectionDialog;

        public override object EditValue(ITypeDescriptorContext context,  IServiceProvider  provider, object value) { 
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)); 
 
                if (edSvc != null && context.Instance != null) {
                    IDesignerHost host = (IDesignerHost)provider.GetService(typeof(IDesignerHost)); 
                    if (host == null)
                    {
                        return value;
                    } 

                    if (dataGridViewColumnCollectionDialog == null) { 
                        dataGridViewColumnCollectionDialog = new DataGridViewColumnCollectionDialog(); 
                    }
 
                    dataGridViewColumnCollectionDialog.SetLiveDataGridView((DataGridView) context.Instance);

                    using(DesignerTransaction trans = host.CreateTransaction(SR.GetString(SR.DataGridViewColumnCollectionTransaction)))
                    { 
                        if (edSvc.ShowDialog(dataGridViewColumnCollectionDialog) == DialogResult.OK)
                        { 
                            trans.Commit(); 
                        }
                        else 
                        {
                            trans.Cancel();
                        }
                    } 
                }
            } 
 
            return value;
        } 

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
            return UITypeEditorEditStyle.Modal;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
