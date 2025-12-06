//------------------------------------------------------------------------------ 
// <copyright file="DataGridDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
    using System.Design; 
    using System.ComponentModel;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
    using System;
    using System.Collections;
    using System.Windows.Forms; 
    using System.Data;
    using System.ComponentModel.Design; 
    using System.Drawing; 
    using Microsoft.Win32;
    using System.Windows.Forms.ComponentModel; 


    /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner"]/*' />
    /// <devdoc> 
    ///    <para>Provides a base designer for data grids.</para>
    /// </devdoc> 
    internal class DataGridDesigner : System.Windows.Forms.Design.ControlDesigner { 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.designerVerbs"]/*' />
        /// <devdoc> 
        ///    <para>Gets the design-time verbs suppoted by the component associated with the
        ///       designer.</para>
        /// </devdoc>
        protected DesignerVerbCollection designerVerbs; 
        private IComponentChangeService changeNotificationService = null;
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.DataGridDesigner"]/*' /> 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Windows.Forms.Design.DataGridDesigner'/> class.</para> 
        /// </devdoc>
        private DataGridDesigner() {
            designerVerbs = new DesignerVerbCollection();
            designerVerbs.Add(new DesignerVerb(SR.GetString(SR.DataGridAutoFormatString), new EventHandler(this.OnAutoFormat))); 
            AutoResizeHandles = true;
        } 
 
        public override void Initialize(IComponent component) {
            base.Initialize(component); 

            IDesignerHost dh = (IDesignerHost) this.GetService(typeof(IDesignerHost));
            if (dh != null) {
                changeNotificationService = (IComponentChangeService) dh.GetService(typeof(IComponentChangeService)); 
                if (changeNotificationService != null)
                    changeNotificationService.ComponentRemoved += new ComponentEventHandler(DataSource_ComponentRemoved); 
            } 
        }
 
        private void DataSource_ComponentRemoved(object sender, ComponentEventArgs e) {
            DataGrid d = (DataGrid) this.Component;
            if (e.Component == d.DataSource)
                d.DataSource = null; 
        }
 
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                if (changeNotificationService != null) 
                    changeNotificationService.ComponentRemoved -= new ComponentEventHandler(DataSource_ComponentRemoved);
            }
            base.Dispose(disposing);
        } 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.Verbs"]/*' /> 
        /// <devdoc> 
        ///    <para>Gets the design-time verbs supported by the component associated with the
        ///       designer.</para> 
        /// </devdoc>
        public override DesignerVerbCollection Verbs {
            get {
                return designerVerbs; 
            }
        } 
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.OnAutoFormat"]/*' />
        /// <devdoc> 
        ///    <para>Raises the AutoFormat event.</para>
        /// </devdoc>
        [
            SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")   // See comment inside the method about ignoring errors. 
        ]
        private void OnAutoFormat(object sender, EventArgs e) { 
            object o = Component; 
            DataGrid dgrid = o as DataGrid;
            Debug.Assert(dgrid != null, "DataGrid expected."); 
            DataGridAutoFormatDialog dialog = new DataGridAutoFormatDialog(dgrid);
            if (dialog.ShowDialog() == DialogResult.OK) {
                DataRow selectedData = dialog.SelectedData;
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                DesignerTransaction trans = host.CreateTransaction(SR.GetString(SR.DataGridAutoFormatUndoTitle, Component.Site.Name));
                try { 
                    if (selectedData != null) { 
                        PropertyDescriptorCollection gridProperties = TypeDescriptor.GetProperties(typeof(DataGrid));
                        foreach (DataColumn c in selectedData.Table.Columns) { 
                            object value = selectedData[c];
                            PropertyDescriptor prop = gridProperties[c.ColumnName];
                            if (prop != null) {
                                if (Convert.IsDBNull(value)  || value.ToString().Length == 0) { 
                                    prop.ResetValue(dgrid);
                                } 
                                else { 
                                    try {
                                        TypeConverter converter = prop.Converter; 
                                        object convertedValue = converter.ConvertFromString(value.ToString());
                                        prop.SetValue(dgrid, convertedValue);
                                    }
                                    catch { 
                                        // Ignore errors... the only one we really care about is Font names.
                                        // The TypeConverter will throw if the font isn't on the user's machine 
                                    } 
                                }
                            } 
                        }
                    }
                }
                finally { 
                    trans.Commit();
                } 
                // now invalidate the grid 
                dgrid.Invalidate();
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataGridDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
    using System.Design; 
    using System.ComponentModel;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
    using System;
    using System.Collections;
    using System.Windows.Forms; 
    using System.Data;
    using System.ComponentModel.Design; 
    using System.Drawing; 
    using Microsoft.Win32;
    using System.Windows.Forms.ComponentModel; 


    /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner"]/*' />
    /// <devdoc> 
    ///    <para>Provides a base designer for data grids.</para>
    /// </devdoc> 
    internal class DataGridDesigner : System.Windows.Forms.Design.ControlDesigner { 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.designerVerbs"]/*' />
        /// <devdoc> 
        ///    <para>Gets the design-time verbs suppoted by the component associated with the
        ///       designer.</para>
        /// </devdoc>
        protected DesignerVerbCollection designerVerbs; 
        private IComponentChangeService changeNotificationService = null;
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.DataGridDesigner"]/*' /> 
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Windows.Forms.Design.DataGridDesigner'/> class.</para> 
        /// </devdoc>
        private DataGridDesigner() {
            designerVerbs = new DesignerVerbCollection();
            designerVerbs.Add(new DesignerVerb(SR.GetString(SR.DataGridAutoFormatString), new EventHandler(this.OnAutoFormat))); 
            AutoResizeHandles = true;
        } 
 
        public override void Initialize(IComponent component) {
            base.Initialize(component); 

            IDesignerHost dh = (IDesignerHost) this.GetService(typeof(IDesignerHost));
            if (dh != null) {
                changeNotificationService = (IComponentChangeService) dh.GetService(typeof(IComponentChangeService)); 
                if (changeNotificationService != null)
                    changeNotificationService.ComponentRemoved += new ComponentEventHandler(DataSource_ComponentRemoved); 
            } 
        }
 
        private void DataSource_ComponentRemoved(object sender, ComponentEventArgs e) {
            DataGrid d = (DataGrid) this.Component;
            if (e.Component == d.DataSource)
                d.DataSource = null; 
        }
 
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                if (changeNotificationService != null) 
                    changeNotificationService.ComponentRemoved -= new ComponentEventHandler(DataSource_ComponentRemoved);
            }
            base.Dispose(disposing);
        } 

        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.Verbs"]/*' /> 
        /// <devdoc> 
        ///    <para>Gets the design-time verbs supported by the component associated with the
        ///       designer.</para> 
        /// </devdoc>
        public override DesignerVerbCollection Verbs {
            get {
                return designerVerbs; 
            }
        } 
 
        /// <include file='doc\DataGridDesigner.uex' path='docs/doc[@for="DataGridDesigner.OnAutoFormat"]/*' />
        /// <devdoc> 
        ///    <para>Raises the AutoFormat event.</para>
        /// </devdoc>
        [
            SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")   // See comment inside the method about ignoring errors. 
        ]
        private void OnAutoFormat(object sender, EventArgs e) { 
            object o = Component; 
            DataGrid dgrid = o as DataGrid;
            Debug.Assert(dgrid != null, "DataGrid expected."); 
            DataGridAutoFormatDialog dialog = new DataGridAutoFormatDialog(dgrid);
            if (dialog.ShowDialog() == DialogResult.OK) {
                DataRow selectedData = dialog.SelectedData;
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                DesignerTransaction trans = host.CreateTransaction(SR.GetString(SR.DataGridAutoFormatUndoTitle, Component.Site.Name));
                try { 
                    if (selectedData != null) { 
                        PropertyDescriptorCollection gridProperties = TypeDescriptor.GetProperties(typeof(DataGrid));
                        foreach (DataColumn c in selectedData.Table.Columns) { 
                            object value = selectedData[c];
                            PropertyDescriptor prop = gridProperties[c.ColumnName];
                            if (prop != null) {
                                if (Convert.IsDBNull(value)  || value.ToString().Length == 0) { 
                                    prop.ResetValue(dgrid);
                                } 
                                else { 
                                    try {
                                        TypeConverter converter = prop.Converter; 
                                        object convertedValue = converter.ConvertFromString(value.ToString());
                                        prop.SetValue(dgrid, convertedValue);
                                    }
                                    catch { 
                                        // Ignore errors... the only one we really care about is Font names.
                                        // The TypeConverter will throw if the font isn't on the user's machine 
                                    } 
                                }
                            } 
                        }
                    }
                }
                finally { 
                    trans.Commit();
                } 
                // now invalidate the grid 
                dgrid.Invalidate();
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
