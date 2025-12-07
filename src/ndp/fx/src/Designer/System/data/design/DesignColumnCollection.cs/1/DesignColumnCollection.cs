 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 

    using System; 
    using System.Data;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization; 
    using System.ComponentModel;
 
 
    /// <summary>
    /// </summary> 
    internal class DesignColumnCollection : DataSourceCollectionBase {
        private DesignTable table;

        private DesignTable designTable; 

        protected override Type ItemType { 
            get { 
                return typeof(DesignColumn);
            } 
        }

        public DesignColumnCollection(DesignTable designTable) : base(designTable) {
            this.designTable = designTable; 
            if (designTable != null) {
                Debug.Assert(designTable.DataTable != null, "How can the designTable does not have a data table?"); 
                if (designTable.DataTable != null) { 
                    foreach (DataColumn dataColumn in designTable.DataTable.Columns) {
                        this.Add(new DesignColumn(dataColumn)); 
                    }
                }
            }
 
            table = designTable;
        } 
 
        protected override INameService NameService {
            get { 
                return DataSetNameService.DefaultInstance;
            }
        }
 
        public void Add(DesignColumn designColumn) {
            if (designColumn.DesignTable != null && designColumn.DesignTable != designTable) { 
                throw new InternalException("Cannot insert a DesignColumn object in two collections."); 
            }
 
            designColumn.DesignTable = designTable;
            List.Add(designColumn);
            if (designColumn.DataColumn != null && this.designTable != null) {
                if (this.designTable.DataTable != null && !this.designTable.DataTable.Columns.Contains(designColumn.Name)) { 
                    this.designTable.DataTable.Columns.Add(designColumn.DataColumn);
                } 
            } 
        }
 
        public void Remove(DesignColumn column) {
            List.Remove(column);
        }
 
        public int IndexOf(DesignColumn column) {
            return List.IndexOf(column); 
        } 

        public DesignColumn this[string columnName] { 
            get {
                return (DesignColumn)FindObject(columnName);
            }
        } 

        /// <summary> 
        /// Note: this function need to call base first 
        /// to ensure the undo model work!
        /// </summary> 
        protected override void OnInsert(int index, object value) {
            base.OnInsert(index, value);
            ValidateType(value);
 
            DesignColumn newColumn = (DesignColumn)value;
 
            if ((newColumn.DataColumn != null) && (this.table != null)) { 
                Debug.Assert(this.table.DataTable != null);
                if (!this.table.DataTable.Columns.Contains(newColumn.DataColumn.ColumnName)) { 
                    this.table.DataTable.Columns.Add(newColumn.DataColumn);
                }
            }
 
            newColumn.DesignTable = designTable;
        } 
 
        /// <summary>
        /// Note: this function need to call base first 
        /// to ensure the undo model work!
        /// </summary>
        protected override void OnSet(int index, object oldValue, object newValue) {
            base.OnSet(index, oldValue, newValue); 
            ValidateType(newValue); ValidateType(oldValue);
 
            DesignColumn oldColumn = (DesignColumn)oldValue; 
            DesignColumn newColumn = (DesignColumn)newValue;
 
            if (this.table != null && oldValue != newValue) {
                Debug.Assert(this.table.DataTable != null);
                if (oldColumn.DataColumn != null) {
                    this.table.DataTable.Columns.Remove(oldColumn.DataColumn); 
                    oldColumn.DesignTable = null;
                } 
 
                if (newColumn.DataColumn != null && !this.table.DataTable.Columns.Contains(newColumn.DataColumn.ColumnName)) {
                    this.table.DataTable.Columns.Add(newColumn.DataColumn); 
                    newColumn.DesignTable = designTable;
                }
            }
        } 

        /// <summary> 
        /// Note: this function need to call base first 
        /// to ensure the undo model work!
        /// </summary> 
        protected override void OnRemove(int index, object value) {
            // Remove the constraint first.
            // As we have wrap everything in transaction and the action is
            // undoable, we now like to add this feature: 
            // Remove the constraints before remove the column
            // 
            base.OnRemove(index, value); 
            ValidateType(value);
 
            DesignColumn column = (DesignColumn)value;

            if ((this.table != null) && (column.DataColumn != null)) {
                Debug.Assert(this.table.DataTable != null); 
                this.table.DataTable.Columns.Remove(column.DataColumn);
            } 
 
            column.DesignTable = null;
        } 
#if not
        /// <summary>
        /// Helper function used to remove constraints first when removing a column
        /// </summary> 
        /// <param name="designColumn"></param>
        private void RemoveConstraints(DesignColumn designColumn) { 
            DesignTable designTable = designColumn.DesignTable; 
            if (designTable == null) {
                return; 
            }

            // Remove uniqueConstraint first
            ArrayList constraints = designTable.GetRelatedDataConstraints(new DesignColumn[]{designColumn}, false); 
            foreach (Constraint constraint in constraints) {
                if (constraint is UniqueConstraint) { 
                    if (((UniqueConstraint)constraint).IsPrimaryKey) { 
                        // Well, we are going to remove the primary key
                        // Call Table.PrimaryKeyColumns = null to let it recored in undo 
                        //
                        designTable.PrimaryKeyColumns = null;
                    }
                    else { 
                        designTable.RemoveKey(constraint as UniqueConstraint);
                    } 
                } 
            }
            constraints = designTable.GetRelatedDataConstraints(new DesignColumn[] { designColumn }, false); 
            foreach (Constraint constraint in constraints) {
                designTable.RemoveConstraint(constraint);
            }
        } 
#endif
        public DesignColumn this[int index] { 
            get { 
                int count = 0;
 
                foreach (DesignColumn designColumn in InnerList) {
                    if (index == count) {
                        return designColumn;
                    } 

                    count++; 
                } 

                throw new InternalException(VSDExceptions.DataSource.INVALID_COLUMN_INDEX_MSG, 
                    VSDExceptions.DataSource.INVALID_COLUMN_INDEX_CODE);
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 

    using System; 
    using System.Data;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization; 
    using System.ComponentModel;
 
 
    /// <summary>
    /// </summary> 
    internal class DesignColumnCollection : DataSourceCollectionBase {
        private DesignTable table;

        private DesignTable designTable; 

        protected override Type ItemType { 
            get { 
                return typeof(DesignColumn);
            } 
        }

        public DesignColumnCollection(DesignTable designTable) : base(designTable) {
            this.designTable = designTable; 
            if (designTable != null) {
                Debug.Assert(designTable.DataTable != null, "How can the designTable does not have a data table?"); 
                if (designTable.DataTable != null) { 
                    foreach (DataColumn dataColumn in designTable.DataTable.Columns) {
                        this.Add(new DesignColumn(dataColumn)); 
                    }
                }
            }
 
            table = designTable;
        } 
 
        protected override INameService NameService {
            get { 
                return DataSetNameService.DefaultInstance;
            }
        }
 
        public void Add(DesignColumn designColumn) {
            if (designColumn.DesignTable != null && designColumn.DesignTable != designTable) { 
                throw new InternalException("Cannot insert a DesignColumn object in two collections."); 
            }
 
            designColumn.DesignTable = designTable;
            List.Add(designColumn);
            if (designColumn.DataColumn != null && this.designTable != null) {
                if (this.designTable.DataTable != null && !this.designTable.DataTable.Columns.Contains(designColumn.Name)) { 
                    this.designTable.DataTable.Columns.Add(designColumn.DataColumn);
                } 
            } 
        }
 
        public void Remove(DesignColumn column) {
            List.Remove(column);
        }
 
        public int IndexOf(DesignColumn column) {
            return List.IndexOf(column); 
        } 

        public DesignColumn this[string columnName] { 
            get {
                return (DesignColumn)FindObject(columnName);
            }
        } 

        /// <summary> 
        /// Note: this function need to call base first 
        /// to ensure the undo model work!
        /// </summary> 
        protected override void OnInsert(int index, object value) {
            base.OnInsert(index, value);
            ValidateType(value);
 
            DesignColumn newColumn = (DesignColumn)value;
 
            if ((newColumn.DataColumn != null) && (this.table != null)) { 
                Debug.Assert(this.table.DataTable != null);
                if (!this.table.DataTable.Columns.Contains(newColumn.DataColumn.ColumnName)) { 
                    this.table.DataTable.Columns.Add(newColumn.DataColumn);
                }
            }
 
            newColumn.DesignTable = designTable;
        } 
 
        /// <summary>
        /// Note: this function need to call base first 
        /// to ensure the undo model work!
        /// </summary>
        protected override void OnSet(int index, object oldValue, object newValue) {
            base.OnSet(index, oldValue, newValue); 
            ValidateType(newValue); ValidateType(oldValue);
 
            DesignColumn oldColumn = (DesignColumn)oldValue; 
            DesignColumn newColumn = (DesignColumn)newValue;
 
            if (this.table != null && oldValue != newValue) {
                Debug.Assert(this.table.DataTable != null);
                if (oldColumn.DataColumn != null) {
                    this.table.DataTable.Columns.Remove(oldColumn.DataColumn); 
                    oldColumn.DesignTable = null;
                } 
 
                if (newColumn.DataColumn != null && !this.table.DataTable.Columns.Contains(newColumn.DataColumn.ColumnName)) {
                    this.table.DataTable.Columns.Add(newColumn.DataColumn); 
                    newColumn.DesignTable = designTable;
                }
            }
        } 

        /// <summary> 
        /// Note: this function need to call base first 
        /// to ensure the undo model work!
        /// </summary> 
        protected override void OnRemove(int index, object value) {
            // Remove the constraint first.
            // As we have wrap everything in transaction and the action is
            // undoable, we now like to add this feature: 
            // Remove the constraints before remove the column
            // 
            base.OnRemove(index, value); 
            ValidateType(value);
 
            DesignColumn column = (DesignColumn)value;

            if ((this.table != null) && (column.DataColumn != null)) {
                Debug.Assert(this.table.DataTable != null); 
                this.table.DataTable.Columns.Remove(column.DataColumn);
            } 
 
            column.DesignTable = null;
        } 
#if not
        /// <summary>
        /// Helper function used to remove constraints first when removing a column
        /// </summary> 
        /// <param name="designColumn"></param>
        private void RemoveConstraints(DesignColumn designColumn) { 
            DesignTable designTable = designColumn.DesignTable; 
            if (designTable == null) {
                return; 
            }

            // Remove uniqueConstraint first
            ArrayList constraints = designTable.GetRelatedDataConstraints(new DesignColumn[]{designColumn}, false); 
            foreach (Constraint constraint in constraints) {
                if (constraint is UniqueConstraint) { 
                    if (((UniqueConstraint)constraint).IsPrimaryKey) { 
                        // Well, we are going to remove the primary key
                        // Call Table.PrimaryKeyColumns = null to let it recored in undo 
                        //
                        designTable.PrimaryKeyColumns = null;
                    }
                    else { 
                        designTable.RemoveKey(constraint as UniqueConstraint);
                    } 
                } 
            }
            constraints = designTable.GetRelatedDataConstraints(new DesignColumn[] { designColumn }, false); 
            foreach (Constraint constraint in constraints) {
                designTable.RemoveConstraint(constraint);
            }
        } 
#endif
        public DesignColumn this[int index] { 
            get { 
                int count = 0;
 
                foreach (DesignColumn designColumn in InnerList) {
                    if (index == count) {
                        return designColumn;
                    } 

                    count++; 
                } 

                throw new InternalException(VSDExceptions.DataSource.INVALID_COLUMN_INDEX_MSG, 
                    VSDExceptions.DataSource.INVALID_COLUMN_INDEX_CODE);
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
