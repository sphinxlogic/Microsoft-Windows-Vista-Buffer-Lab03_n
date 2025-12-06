 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design{ 

    using System; 
    using System.Collections;
    using System.Data;
    using System.Diagnostics;
 

    /// <summary> 
    /// </summary> 
    internal class DesignTableCollection : DataSourceCollectionBase {
 
        private DesignDataSource dataSource;

        public DesignTableCollection(DesignDataSource dataSource) : base(dataSource) {
            this.dataSource = dataSource; 
        }
 
        private DataSet DataSet{ 
            get{
                if (dataSource != null){ 
                    return dataSource.DataSet;
                }
                return null;
            } 
        }
 
        protected override Type ItemType { 
            get {
                return typeof(DesignTable); 
            }
        }

        protected override INameService NameService { 
            get {
                return DataSetNameService.DefaultInstance; 
            } 
        }
 
        /// <summary>
        /// </summary>
        internal DesignTable this[string name] {
            get { 
                return (DesignTable)FindObject(name);
            } 
        } 

        /// <summary> 
        /// </summary>
        internal DesignTable this[DataTable dataTable] {
            get {
                foreach (DesignTable designTable in this) { 
                    if (designTable.DataTable == dataTable) {
                        return designTable; 
                    } 
                }
                return null; 
            }
        }

        /// <summary> 
        /// Will throw if name is invalid or a dup
        /// Add the DataTable to the dataTable if not added yet 
        /// </summary> 
        public void Add(DesignTable designTable){
            // 
            List.Add(designTable);
        }

        public bool Contains( DesignTable table ) { 
            return List.Contains( table );
        } 
 
        public int IndexOf( DesignTable table ) {
            return List.IndexOf( table ); 
        }

        public void Remove( DesignTable table ) {
            List.Remove( table ); 
        }
 
        /// <summary> 
        /// Note: this function need to call base first
        /// to ensure the undo model work! 
        /// </summary>
        protected override void OnInsert( int index, object value ) {
            base.OnInsert(index, value);
 
            DesignTable designTable = (DesignTable)value;
 
            if (designTable.Name == null || designTable.Name.Length == 0) { 
                designTable.Name = CreateUniqueName(designTable);
            } 

            NameService.ValidateUniqueName(this, designTable.Name);

            if( (this.dataSource != null) && (designTable.Owner == this.dataSource) ) { 
                Debug.Fail( "Table already belongs to this DataSource" );
                return;  // no-op 
            } 

            if( (this.dataSource != null) && (designTable.Owner != null) ) { 
                throw new InternalException( VSDExceptions.DataSource.TABLE_BELONGS_TO_OTHER_DATA_SOURCE_MSG,
                    VSDExceptions.DataSource.TABLE_BELONGS_TO_OTHER_DATA_SOURCE_CODE );
            }
 
            DataSet dataSet = DataSet;
            if ((dataSet != null) && (!dataSet.Tables.Contains(designTable.DataTable.TableName))) { 
                Debug.Assert( this.dataSource != null, "If we were able to get the DataSet we should have a design time data source as well" ); 

 
                dataSet.Tables.Add(designTable.DataTable);
            }

            designTable.Owner = this.dataSource; 
        }
 
        /// <summary> 
        /// Remove the DataTable in the dataTable if not removed  yet
        /// Note: this function need to call base first 
        /// to ensure the undo model work!
        /// </summary>
        protected override void OnRemove( int index, object value ) {
            base.OnRemove(index, value); 
            DesignTable designTable = (DesignTable)value;
            DataSet dataSet = DataSet; 
            if (dataSet != null && designTable.DataTable != null 
                && dataSet.Tables.Contains(designTable.DataTable.TableName)){
 
                dataSet.Tables.Remove(designTable.DataTable);
            }
            designTable.Owner = null;
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
namespace System.Data.Design{ 

    using System; 
    using System.Collections;
    using System.Data;
    using System.Diagnostics;
 

    /// <summary> 
    /// </summary> 
    internal class DesignTableCollection : DataSourceCollectionBase {
 
        private DesignDataSource dataSource;

        public DesignTableCollection(DesignDataSource dataSource) : base(dataSource) {
            this.dataSource = dataSource; 
        }
 
        private DataSet DataSet{ 
            get{
                if (dataSource != null){ 
                    return dataSource.DataSet;
                }
                return null;
            } 
        }
 
        protected override Type ItemType { 
            get {
                return typeof(DesignTable); 
            }
        }

        protected override INameService NameService { 
            get {
                return DataSetNameService.DefaultInstance; 
            } 
        }
 
        /// <summary>
        /// </summary>
        internal DesignTable this[string name] {
            get { 
                return (DesignTable)FindObject(name);
            } 
        } 

        /// <summary> 
        /// </summary>
        internal DesignTable this[DataTable dataTable] {
            get {
                foreach (DesignTable designTable in this) { 
                    if (designTable.DataTable == dataTable) {
                        return designTable; 
                    } 
                }
                return null; 
            }
        }

        /// <summary> 
        /// Will throw if name is invalid or a dup
        /// Add the DataTable to the dataTable if not added yet 
        /// </summary> 
        public void Add(DesignTable designTable){
            // 
            List.Add(designTable);
        }

        public bool Contains( DesignTable table ) { 
            return List.Contains( table );
        } 
 
        public int IndexOf( DesignTable table ) {
            return List.IndexOf( table ); 
        }

        public void Remove( DesignTable table ) {
            List.Remove( table ); 
        }
 
        /// <summary> 
        /// Note: this function need to call base first
        /// to ensure the undo model work! 
        /// </summary>
        protected override void OnInsert( int index, object value ) {
            base.OnInsert(index, value);
 
            DesignTable designTable = (DesignTable)value;
 
            if (designTable.Name == null || designTable.Name.Length == 0) { 
                designTable.Name = CreateUniqueName(designTable);
            } 

            NameService.ValidateUniqueName(this, designTable.Name);

            if( (this.dataSource != null) && (designTable.Owner == this.dataSource) ) { 
                Debug.Fail( "Table already belongs to this DataSource" );
                return;  // no-op 
            } 

            if( (this.dataSource != null) && (designTable.Owner != null) ) { 
                throw new InternalException( VSDExceptions.DataSource.TABLE_BELONGS_TO_OTHER_DATA_SOURCE_MSG,
                    VSDExceptions.DataSource.TABLE_BELONGS_TO_OTHER_DATA_SOURCE_CODE );
            }
 
            DataSet dataSet = DataSet;
            if ((dataSet != null) && (!dataSet.Tables.Contains(designTable.DataTable.TableName))) { 
                Debug.Assert( this.dataSource != null, "If we were able to get the DataSet we should have a design time data source as well" ); 

 
                dataSet.Tables.Add(designTable.DataTable);
            }

            designTable.Owner = this.dataSource; 
        }
 
        /// <summary> 
        /// Remove the DataTable in the dataTable if not removed  yet
        /// Note: this function need to call base first 
        /// to ensure the undo model work!
        /// </summary>
        protected override void OnRemove( int index, object value ) {
            base.OnRemove(index, value); 
            DesignTable designTable = (DesignTable)value;
            DataSet dataSet = DataSet; 
            if (dataSet != null && designTable.DataTable != null 
                && dataSet.Tables.Contains(designTable.DataTable.TableName)){
 
                dataSet.Tables.Remove(designTable.DataTable);
            }
            designTable.Owner = null;
        } 
    }
 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
