 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design{ 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics; 

 
    /// <summary> 
    /// </summary>
    internal class DesignRelationCollection: DataSourceCollectionBase { 
        private DesignDataSource dataSource;

        public DesignRelationCollection(DesignDataSource dataSource) : base(dataSource) {
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
                return typeof(DesignRelation); 
            }
        }

        protected override INameService NameService { 
            get {
                return DataSetNameService.DefaultInstance; 
            } 
        }
 
        internal DesignRelation this[ForeignKeyConstraint constraint] {
            get {
 				if (constraint == null) {
					return null; 
				}
 
				foreach (DesignRelation relation in this) { 
                    if (relation.ForeignKeyConstraint == constraint) {
                        return relation; 
                    }
                }
                return null;
            } 
        }
 
        internal DesignRelation this[string name] { 
            get {
                return (DesignRelation) FindObject(name); 
            }
        }

 
        public void Remove( DesignRelation rel ) {
            List.Remove( rel ); 
        } 

        public int Add( DesignRelation rel ) { 
            return List.Add( rel );
        }

        public bool Contains( DesignRelation rel ) { 
            return List.Contains( rel );
        } 
 
        /// <summary>
        /// Note: this function need to call base first 
        /// to ensure the undo model work!
        /// </summary>
        protected override void OnInsert( int index, object value ) {
            ValidateType( value ); 

            DesignRelation designRelation = (DesignRelation)value; 
 
            if( (this.dataSource != null) && (designRelation.Owner == this.dataSource) ) {
                Debug.Fail( "Relation already belongs to this DataSource" ); 
                return;  // no-op
            }

            if( (this.dataSource != null) && (designRelation.Owner != null) ) { 
                throw new InternalException( VSDExceptions.DataSource.RELATION_BELONGS_TO_OTHER_DATA_SOURCE_MSG,
                    VSDExceptions.DataSource.RELATION_BELONGS_TO_OTHER_DATA_SOURCE_CODE ); 
            } 

            if (designRelation.Name == null || designRelation.Name.Length == 0) { 
                designRelation.Name = CreateUniqueName(designRelation);
            }

            ValidateName(designRelation); 

            DataSet dataSet = DataSet; 
            if (dataSet != null) { 
                if (designRelation.ForeignKeyConstraint != null) {
                    ForeignKeyConstraint constraint = designRelation.ForeignKeyConstraint; 
                    if (constraint.Columns.Length > 0) {
                        DataTable dataTable = constraint.Columns[0].Table;
                        if (dataTable != null && !dataTable.Constraints.Contains(constraint.ConstraintName)) {
                            dataTable.Constraints.Add(constraint); 
                        }
                    } 
                } 
                if (designRelation.DataRelation != null &&
                    (!dataSet.Relations.Contains(designRelation.DataRelation.RelationName))){ 
                    dataSet.Relations.Add(designRelation.DataRelation);
                }
            }
 
            // we should insert to the collection later than we insert to DataSet
            // the reason is the DataSet will create a keyConstraint for relation, we want the constraint undo unit to be added before 
            // our relation undo unit... otherwise, the undo/redo will fail, as DataSet will create another constraint for us.. 
            base.OnInsert( index, value );
 
            designRelation.Owner = dataSource;
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
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics; 

 
    /// <summary> 
    /// </summary>
    internal class DesignRelationCollection: DataSourceCollectionBase { 
        private DesignDataSource dataSource;

        public DesignRelationCollection(DesignDataSource dataSource) : base(dataSource) {
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
                return typeof(DesignRelation); 
            }
        }

        protected override INameService NameService { 
            get {
                return DataSetNameService.DefaultInstance; 
            } 
        }
 
        internal DesignRelation this[ForeignKeyConstraint constraint] {
            get {
 				if (constraint == null) {
					return null; 
				}
 
				foreach (DesignRelation relation in this) { 
                    if (relation.ForeignKeyConstraint == constraint) {
                        return relation; 
                    }
                }
                return null;
            } 
        }
 
        internal DesignRelation this[string name] { 
            get {
                return (DesignRelation) FindObject(name); 
            }
        }

 
        public void Remove( DesignRelation rel ) {
            List.Remove( rel ); 
        } 

        public int Add( DesignRelation rel ) { 
            return List.Add( rel );
        }

        public bool Contains( DesignRelation rel ) { 
            return List.Contains( rel );
        } 
 
        /// <summary>
        /// Note: this function need to call base first 
        /// to ensure the undo model work!
        /// </summary>
        protected override void OnInsert( int index, object value ) {
            ValidateType( value ); 

            DesignRelation designRelation = (DesignRelation)value; 
 
            if( (this.dataSource != null) && (designRelation.Owner == this.dataSource) ) {
                Debug.Fail( "Relation already belongs to this DataSource" ); 
                return;  // no-op
            }

            if( (this.dataSource != null) && (designRelation.Owner != null) ) { 
                throw new InternalException( VSDExceptions.DataSource.RELATION_BELONGS_TO_OTHER_DATA_SOURCE_MSG,
                    VSDExceptions.DataSource.RELATION_BELONGS_TO_OTHER_DATA_SOURCE_CODE ); 
            } 

            if (designRelation.Name == null || designRelation.Name.Length == 0) { 
                designRelation.Name = CreateUniqueName(designRelation);
            }

            ValidateName(designRelation); 

            DataSet dataSet = DataSet; 
            if (dataSet != null) { 
                if (designRelation.ForeignKeyConstraint != null) {
                    ForeignKeyConstraint constraint = designRelation.ForeignKeyConstraint; 
                    if (constraint.Columns.Length > 0) {
                        DataTable dataTable = constraint.Columns[0].Table;
                        if (dataTable != null && !dataTable.Constraints.Contains(constraint.ConstraintName)) {
                            dataTable.Constraints.Add(constraint); 
                        }
                    } 
                } 
                if (designRelation.DataRelation != null &&
                    (!dataSet.Relations.Contains(designRelation.DataRelation.RelationName))){ 
                    dataSet.Relations.Add(designRelation.DataRelation);
                }
            }
 
            // we should insert to the collection later than we insert to DataSet
            // the reason is the DataSet will create a keyConstraint for relation, we want the constraint undo unit to be added before 
            // our relation undo unit... otherwise, the undo/redo will fail, as DataSet will create another constraint for us.. 
            base.OnInsert( index, value );
 
            designRelation.Owner = dataSource;
        }
    }
 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
