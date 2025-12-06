//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewComboBoxColumnDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using System.ComponentModel; 
    using System.Diagnostics;
    using System; 
    using System.Collections;
    using System.Windows.Forms;
    using System.Data;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using Microsoft.Win32; 
    using System.Windows.Forms.ComponentModel; 

 
    /// <include file='doc\TableComboBoxColumnDesigner.uex' path='docs/doc[@for="DataGridViewComboBoxColumnDesigner"]/*' />
    /// <devdoc>
    ///    <para>Provides a base designer for data grid view columns.</para>
    /// </devdoc> 
    internal class DataGridViewComboBoxColumnDesigner : DataGridViewColumnDesigner {
        static BindingContext bc; 
        private string ValueMember 
        {
            get 
            {
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component;
                return col.ValueMember;
            } 
            set
            { 
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component; 
                if (col.DataSource == null)
                { 
                    return;
                }

                if (ValidDataMember(col.DataSource, value)) 
                {
                    col.ValueMember = value; 
                } 
            }
        } 

        private string DisplayMember
        {
            get 
            {
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component; 
                return col.DisplayMember; 
            }
            set 
            {
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component;
                if (col.DataSource == null)
                { 
                    return;
                } 
 
                if (ValidDataMember(col.DataSource, value))
                { 
                    col.DisplayMember = value;
                }
            }
        } 

        private bool ShouldSerializeDisplayMember() 
        { 
            DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component;
            return !String.IsNullOrEmpty(col.DisplayMember); 
        }

        private bool ShouldSerializeValueMember()
        { 
            DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component;
            return !String.IsNullOrEmpty(col.ValueMember); 
        } 

        private static bool ValidDataMember(object dataSource, string dataMember) 
        {
            if (String.IsNullOrEmpty(dataMember))
            {
                // a null string is a valid value 
                return true;
            } 
 
            if (bc == null)
            { 
                bc = new BindingContext();
            }

            // 
            // scrub the hashTable inside the BindingContext every time we access this method.
            // 
            int count = ((ICollection) bc).Count; 

            BindingMemberInfo bmi = new BindingMemberInfo(dataMember); 
            PropertyDescriptorCollection props = null;
            BindingManagerBase bmb;
            try
            { 
                bmb = bc[dataSource, bmi.BindingPath];
            } 
            catch (System.ArgumentException) 
            {
                return false; 
            }

            if (bmb == null)
            { 
                return false;
            } 
 
            props = bmb.GetItemProperties();
 
            if (props == null)
            {
                return false;
            } 

            if (props[bmi.BindingField] == null) 
            { 
                return false;
            } 

            return true;
        }
 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 
 
            PropertyDescriptor prop = (PropertyDescriptor) properties["ValueMember"];
            if (prop != null) 
            {
                properties["ValueMember"] = TypeDescriptor.CreateProperty(typeof(DataGridViewComboBoxColumnDesigner), prop, new Attribute[0]);
            }
 
            prop = (PropertyDescriptor) properties["DisplayMember"];
            if (prop != null) 
            { 
                properties["DisplayMember"] = TypeDescriptor.CreateProperty(typeof(DataGridViewComboBoxColumnDesigner), prop, new Attribute[0]);
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewComboBoxColumnDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using System.ComponentModel; 
    using System.Diagnostics;
    using System; 
    using System.Collections;
    using System.Windows.Forms;
    using System.Data;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using Microsoft.Win32; 
    using System.Windows.Forms.ComponentModel; 

 
    /// <include file='doc\TableComboBoxColumnDesigner.uex' path='docs/doc[@for="DataGridViewComboBoxColumnDesigner"]/*' />
    /// <devdoc>
    ///    <para>Provides a base designer for data grid view columns.</para>
    /// </devdoc> 
    internal class DataGridViewComboBoxColumnDesigner : DataGridViewColumnDesigner {
        static BindingContext bc; 
        private string ValueMember 
        {
            get 
            {
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component;
                return col.ValueMember;
            } 
            set
            { 
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component; 
                if (col.DataSource == null)
                { 
                    return;
                }

                if (ValidDataMember(col.DataSource, value)) 
                {
                    col.ValueMember = value; 
                } 
            }
        } 

        private string DisplayMember
        {
            get 
            {
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component; 
                return col.DisplayMember; 
            }
            set 
            {
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component;
                if (col.DataSource == null)
                { 
                    return;
                } 
 
                if (ValidDataMember(col.DataSource, value))
                { 
                    col.DisplayMember = value;
                }
            }
        } 

        private bool ShouldSerializeDisplayMember() 
        { 
            DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component;
            return !String.IsNullOrEmpty(col.DisplayMember); 
        }

        private bool ShouldSerializeValueMember()
        { 
            DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn) this.Component;
            return !String.IsNullOrEmpty(col.ValueMember); 
        } 

        private static bool ValidDataMember(object dataSource, string dataMember) 
        {
            if (String.IsNullOrEmpty(dataMember))
            {
                // a null string is a valid value 
                return true;
            } 
 
            if (bc == null)
            { 
                bc = new BindingContext();
            }

            // 
            // scrub the hashTable inside the BindingContext every time we access this method.
            // 
            int count = ((ICollection) bc).Count; 

            BindingMemberInfo bmi = new BindingMemberInfo(dataMember); 
            PropertyDescriptorCollection props = null;
            BindingManagerBase bmb;
            try
            { 
                bmb = bc[dataSource, bmi.BindingPath];
            } 
            catch (System.ArgumentException) 
            {
                return false; 
            }

            if (bmb == null)
            { 
                return false;
            } 
 
            props = bmb.GetItemProperties();
 
            if (props == null)
            {
                return false;
            } 

            if (props[bmi.BindingField] == null) 
            { 
                return false;
            } 

            return true;
        }
 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 
 
            PropertyDescriptor prop = (PropertyDescriptor) properties["ValueMember"];
            if (prop != null) 
            {
                properties["ValueMember"] = TypeDescriptor.CreateProperty(typeof(DataGridViewComboBoxColumnDesigner), prop, new Attribute[0]);
            }
 
            prop = (PropertyDescriptor) properties["DisplayMember"];
            if (prop != null) 
            { 
                properties["DisplayMember"] = TypeDescriptor.CreateProperty(typeof(DataGridViewComboBoxColumnDesigner), prop, new Attribute[0]);
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
