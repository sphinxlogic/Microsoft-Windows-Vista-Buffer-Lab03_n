//------------------------------------------------------------------------------ 
// <copyright file="RelatedView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data {
    using System; 
    using System.Diagnostics;

    internal sealed class RelatedView : DataView, IFilter {
        private readonly DataKey key; 
        private object[] values;
 
        public RelatedView(DataColumn[] columns, object[] values) : base(columns[0].Table, false) { 
            if (values == null) {
                throw ExceptionBuilder.ArgumentNull("values"); 
            }
            this.key = new DataKey(columns, true);
            this.values = values;
            Debug.Assert (this.Table == key.Table, "Key.Table Must be equal to Current Table"); 
            base.ResetRowViewCache();
        } 
 
        public bool Invoke(DataRow row, DataRowVersion version) {
            object[] keyValues = row.GetKeyValues(key, version); 
#if false
            for (int i = 0; i < keyValues.Length; i++) {
                Debug.WriteLine("keyvalues[" + (i).ToString() + "] = " + Convert.ToString(keyValues[i]));
            } 
            for (int i = 0; i < values.Length; i++) {
                Debug.WriteLine("values[" + (i).ToString() + "] = " + Convert.ToString(values[i])); 
            } 
#endif
            bool allow = true; 
            if (keyValues.Length != values.Length) {
                allow = false;
            }
            else { 
                for (int i = 0; i < keyValues.Length; i++) {
                    if (!keyValues[i].Equals(values[i])) { 
                        allow = false; 
                        break;
                    } 
                }
            }

            IFilter baseFilter = base.GetFilter(); 
            if (baseFilter != null)
                allow &= baseFilter.Invoke(row, version); 
 
            return allow;
        } 

        internal override IFilter GetFilter() {
            return this;
        } 

        // move to OnModeChanged 
        public override DataRowView AddNew() { 
            DataRowView addNewRowView = base.AddNew();
            addNewRowView.Row.SetKeyValues(key, values); 
            return addNewRowView;
        }

        internal override void SetIndex(string newSort, DataViewRowState newRowStates, IFilter newRowFilter) { 
            SetIndex2(newSort, newRowStates, newRowFilter, false);
            Reset(); 
        } 

        public override bool Equals( DataView dv) { 
            if (dv is RelatedView == false)
            	return false;
            if (!base.Equals(dv))
            	return false; 
            return (CompareArray(this.key.ColumnsReference, ((RelatedView)dv).key.ColumnsReference) ||CompareArray(this.values, ((RelatedView)dv).values));
        } 
 
        private bool CompareArray(object[] value1, object[] value2) {
            if (value1.Length !=  value2.Length) 
                return false;
            for(int i = 0; i < value1.Length; i++) {
                if (value1[i] != value2[i])
                    return false; 
            }
            return true; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="RelatedView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data {
    using System; 
    using System.Diagnostics;

    internal sealed class RelatedView : DataView, IFilter {
        private readonly DataKey key; 
        private object[] values;
 
        public RelatedView(DataColumn[] columns, object[] values) : base(columns[0].Table, false) { 
            if (values == null) {
                throw ExceptionBuilder.ArgumentNull("values"); 
            }
            this.key = new DataKey(columns, true);
            this.values = values;
            Debug.Assert (this.Table == key.Table, "Key.Table Must be equal to Current Table"); 
            base.ResetRowViewCache();
        } 
 
        public bool Invoke(DataRow row, DataRowVersion version) {
            object[] keyValues = row.GetKeyValues(key, version); 
#if false
            for (int i = 0; i < keyValues.Length; i++) {
                Debug.WriteLine("keyvalues[" + (i).ToString() + "] = " + Convert.ToString(keyValues[i]));
            } 
            for (int i = 0; i < values.Length; i++) {
                Debug.WriteLine("values[" + (i).ToString() + "] = " + Convert.ToString(values[i])); 
            } 
#endif
            bool allow = true; 
            if (keyValues.Length != values.Length) {
                allow = false;
            }
            else { 
                for (int i = 0; i < keyValues.Length; i++) {
                    if (!keyValues[i].Equals(values[i])) { 
                        allow = false; 
                        break;
                    } 
                }
            }

            IFilter baseFilter = base.GetFilter(); 
            if (baseFilter != null)
                allow &= baseFilter.Invoke(row, version); 
 
            return allow;
        } 

        internal override IFilter GetFilter() {
            return this;
        } 

        // move to OnModeChanged 
        public override DataRowView AddNew() { 
            DataRowView addNewRowView = base.AddNew();
            addNewRowView.Row.SetKeyValues(key, values); 
            return addNewRowView;
        }

        internal override void SetIndex(string newSort, DataViewRowState newRowStates, IFilter newRowFilter) { 
            SetIndex2(newSort, newRowStates, newRowFilter, false);
            Reset(); 
        } 

        public override bool Equals( DataView dv) { 
            if (dv is RelatedView == false)
            	return false;
            if (!base.Equals(dv))
            	return false; 
            return (CompareArray(this.key.ColumnsReference, ((RelatedView)dv).key.ColumnsReference) ||CompareArray(this.values, ((RelatedView)dv).values));
        } 
 
        private bool CompareArray(object[] value1, object[] value2) {
            if (value1.Length !=  value2.Length) 
                return false;
            for(int i = 0; i < value1.Length; i++) {
                if (value1[i] != value2[i])
                    return false; 
            }
            return true; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
