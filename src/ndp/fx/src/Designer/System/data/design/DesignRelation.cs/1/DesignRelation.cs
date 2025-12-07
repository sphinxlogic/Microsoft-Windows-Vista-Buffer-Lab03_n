 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design{ 

    using System; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.Data;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics; 
 

 
    /// <summary>
    /// This is the design time Relation class
    /// -----------------------------------------
    ///  The important thing is that we are using this object to present both DataRelation and ForeignKeyConstraint 
    ///  One DesignRelation may contain both DataRelation and ForeignKeyConstraint or only ForeignKeyConstraint,
    ///  all code to access this object should know about it, and take care that DataRelation could be null. 
    ///  And there are a strange status in DataSet, where the DataRelation and the constraint don't match each other, 
    ///  they could have different names, and different DataColumns. That's a very strange status, and we don't support it well.
    ///  (Actually, when you create a DataRelation, a matched constraint will be created for you. It only happens when you change 
    ///   the DataRelation, but haven't changed the constraint. I think that our designer should prevent it happens
    ///  However, if it happened, and saved into the schema file, we won't show the constraint. (That only happens when you write
    ///  code to operate DataSet, create a relation, remove the relation but not the constraint, then create another relation).
    /// </summary> 
    internal class DesignRelation: DataSourceComponent, IDataSourceNamedObject{
        internal const string NAMEROOT = "Relation"; 
 
        private DesignDataSource        owner;
        private DataRelation            dataRelation; 
        private ForeignKeyConstraint    dataForeignKeyConstraint;

        private const string EXTPROPNAME_USER_RELATIONNAME          = "Generator_UserRelationName";
        private const string EXTPROPNAME_USER_PARENTTABLE           = "Generator_UserParentTable"; 
        private const string EXTPROPNAME_USER_CHILDTABLE            = "Generator_UserChildTable";
        private const string EXTPROPNAME_GENERATOR_RELATIONVARNAME  = "Generator_RelationVarName"; 
        private const string EXTPROPNAME_GENERATOR_PARENTPROPNAME   = "Generator_ParentPropName"; 
        private const string EXTPROPNAME_GENERATOR_CHILDPROPNAME    = "Generator_ChildPropName";
 

        public DesignRelation(DataRelation dataRelation) {
            this.DataRelation = dataRelation;
        } 

        public DesignRelation(ForeignKeyConstraint foreignKeyConstraint) { 
            this.DataRelation = null; 
            this.dataForeignKeyConstraint = foreignKeyConstraint;
        } 

        internal DataColumn[] ChildDataColumns {
            get {
                if (dataRelation != null) { 
                    return dataRelation.ChildColumns;
                } 
                else if (dataForeignKeyConstraint != null) { 
                    return dataForeignKeyConstraint.Columns;
                } 
                return new DataColumn[0];
            }
        }
 
        internal DesignTable ChildDesignTable {
            get { 
                DataTable childTable = null; 
                if (dataRelation != null) {
                    childTable = dataRelation.ChildTable; 
                }
                else if (dataForeignKeyConstraint != null) {
                    childTable = dataForeignKeyConstraint.Table;
                } 

                if (childTable != null && Owner != null) { 
                    return Owner.DesignTables[childTable]; 
                }
                return null; 
            }
        }

        internal DataRelation DataRelation{ 
            get{
                return dataRelation; 
            } 
            set {
                dataRelation = value; 
                if (dataRelation != null) {
                    dataForeignKeyConstraint = null;
                }
            } 
        }
 
        internal ForeignKeyConstraint ForeignKeyConstraint { 
            get {
                if (dataRelation != null && dataRelation.ChildKeyConstraint != null){ 
                    return dataRelation.ChildKeyConstraint;
                }
                return dataForeignKeyConstraint;
            } 
            set {
                dataForeignKeyConstraint = value; 
            } 
        }
 
        /// <summary>
        /// </summary>
        [
            MergableProperty(false), 
            DefaultValue("")
        ] 
        public string Name { 
            get{
                if (dataRelation != null){ 
                    return dataRelation.RelationName;
                }
                else if (dataForeignKeyConstraint != null) {
                    return dataForeignKeyConstraint.ConstraintName; 
                }
 
                Debug.Fail("Access a null dataRelation & null foreignKeyConstraint"); 
                return string.Empty;
            } 
            set{
                Debug.Assert(dataRelation != null, "Access a null dataRelation & null foreignKeyConstraint");
                if (!StringUtil.EqualValue(this.Name, value)) {
                    if (this.CollectionParent != null) { 
                        CollectionParent.ValidateUniqueName(this, value);
                    } 
 
                    if (dataRelation != null) {
                        dataRelation.RelationName = value; 
                    }

                    if (dataForeignKeyConstraint != null) {
                        dataForeignKeyConstraint.ConstraintName = value; 
                    }
                } 
            } 
        }
 
        /// <summary>
        /// Owner is typically set when this is added to the designrelation collection
        /// </summary>
        internal DesignDataSource Owner { 
            get {
                return this.owner; 
            } 
            set {
                this.owner = value; 
            }
        }

        internal DataColumn[] ParentDataColumns { 
            get {
                if (dataRelation != null) { 
                    return dataRelation.ParentColumns; 
                }
                else if (dataForeignKeyConstraint != null) { 
                    return dataForeignKeyConstraint.RelatedColumns;
                }
                return new DataColumn[0];
            } 
        }
 
        internal DesignTable ParentDesignTable { 
            get {
                DataTable parentTable = null; 
                if (dataRelation != null) {
                    parentTable = dataRelation.ParentTable;
                }
                else if (dataForeignKeyConstraint != null) { 
                    parentTable = dataForeignKeyConstraint.RelatedTable;
                } 
 
                if (parentTable != null && Owner != null) {
                    return Owner.DesignTables[parentTable]; 
                }
                return null;
            }
        } 

        [Browsable(false)] 
        public string PublicTypeName { 
            get {
                return NAMEROOT; 
            }
        }

        [Flags] 
        public enum CompareOption {
            Columns, 
            Tables, 
            ForeignKeyConstraints
        } 


        internal string UserRelationName {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_RELATIONNAME] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_RELATIONNAME] = value;
            } 
        }

        internal string UserParentTable {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_PARENTTABLE] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_PARENTTABLE] = value;
            } 
        }

        internal string UserChildTable {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_CHILDTABLE] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_CHILDTABLE] = value;
            } 
        }

        internal string GeneratorRelationVarName {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_RELATIONVARNAME] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_RELATIONVARNAME] = value;
            } 
        }

        internal string GeneratorChildPropName {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_CHILDPROPNAME] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_CHILDPROPNAME] = value;
            } 
        }

        internal string GeneratorParentPropName {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_PARENTPROPNAME] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_PARENTPROPNAME] = value;
            } 
        }

        internal override StringCollection NamingPropertyNames {
            get { 
                StringCollection namingPropNames = new StringCollection();
                namingPropNames.AddRange(new string[] { "typedParent", "typedChildren" }); 
 
                return namingPropNames;
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
namespace System.Data.Design{ 

    using System; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.Data;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics; 
 

 
    /// <summary>
    /// This is the design time Relation class
    /// -----------------------------------------
    ///  The important thing is that we are using this object to present both DataRelation and ForeignKeyConstraint 
    ///  One DesignRelation may contain both DataRelation and ForeignKeyConstraint or only ForeignKeyConstraint,
    ///  all code to access this object should know about it, and take care that DataRelation could be null. 
    ///  And there are a strange status in DataSet, where the DataRelation and the constraint don't match each other, 
    ///  they could have different names, and different DataColumns. That's a very strange status, and we don't support it well.
    ///  (Actually, when you create a DataRelation, a matched constraint will be created for you. It only happens when you change 
    ///   the DataRelation, but haven't changed the constraint. I think that our designer should prevent it happens
    ///  However, if it happened, and saved into the schema file, we won't show the constraint. (That only happens when you write
    ///  code to operate DataSet, create a relation, remove the relation but not the constraint, then create another relation).
    /// </summary> 
    internal class DesignRelation: DataSourceComponent, IDataSourceNamedObject{
        internal const string NAMEROOT = "Relation"; 
 
        private DesignDataSource        owner;
        private DataRelation            dataRelation; 
        private ForeignKeyConstraint    dataForeignKeyConstraint;

        private const string EXTPROPNAME_USER_RELATIONNAME          = "Generator_UserRelationName";
        private const string EXTPROPNAME_USER_PARENTTABLE           = "Generator_UserParentTable"; 
        private const string EXTPROPNAME_USER_CHILDTABLE            = "Generator_UserChildTable";
        private const string EXTPROPNAME_GENERATOR_RELATIONVARNAME  = "Generator_RelationVarName"; 
        private const string EXTPROPNAME_GENERATOR_PARENTPROPNAME   = "Generator_ParentPropName"; 
        private const string EXTPROPNAME_GENERATOR_CHILDPROPNAME    = "Generator_ChildPropName";
 

        public DesignRelation(DataRelation dataRelation) {
            this.DataRelation = dataRelation;
        } 

        public DesignRelation(ForeignKeyConstraint foreignKeyConstraint) { 
            this.DataRelation = null; 
            this.dataForeignKeyConstraint = foreignKeyConstraint;
        } 

        internal DataColumn[] ChildDataColumns {
            get {
                if (dataRelation != null) { 
                    return dataRelation.ChildColumns;
                } 
                else if (dataForeignKeyConstraint != null) { 
                    return dataForeignKeyConstraint.Columns;
                } 
                return new DataColumn[0];
            }
        }
 
        internal DesignTable ChildDesignTable {
            get { 
                DataTable childTable = null; 
                if (dataRelation != null) {
                    childTable = dataRelation.ChildTable; 
                }
                else if (dataForeignKeyConstraint != null) {
                    childTable = dataForeignKeyConstraint.Table;
                } 

                if (childTable != null && Owner != null) { 
                    return Owner.DesignTables[childTable]; 
                }
                return null; 
            }
        }

        internal DataRelation DataRelation{ 
            get{
                return dataRelation; 
            } 
            set {
                dataRelation = value; 
                if (dataRelation != null) {
                    dataForeignKeyConstraint = null;
                }
            } 
        }
 
        internal ForeignKeyConstraint ForeignKeyConstraint { 
            get {
                if (dataRelation != null && dataRelation.ChildKeyConstraint != null){ 
                    return dataRelation.ChildKeyConstraint;
                }
                return dataForeignKeyConstraint;
            } 
            set {
                dataForeignKeyConstraint = value; 
            } 
        }
 
        /// <summary>
        /// </summary>
        [
            MergableProperty(false), 
            DefaultValue("")
        ] 
        public string Name { 
            get{
                if (dataRelation != null){ 
                    return dataRelation.RelationName;
                }
                else if (dataForeignKeyConstraint != null) {
                    return dataForeignKeyConstraint.ConstraintName; 
                }
 
                Debug.Fail("Access a null dataRelation & null foreignKeyConstraint"); 
                return string.Empty;
            } 
            set{
                Debug.Assert(dataRelation != null, "Access a null dataRelation & null foreignKeyConstraint");
                if (!StringUtil.EqualValue(this.Name, value)) {
                    if (this.CollectionParent != null) { 
                        CollectionParent.ValidateUniqueName(this, value);
                    } 
 
                    if (dataRelation != null) {
                        dataRelation.RelationName = value; 
                    }

                    if (dataForeignKeyConstraint != null) {
                        dataForeignKeyConstraint.ConstraintName = value; 
                    }
                } 
            } 
        }
 
        /// <summary>
        /// Owner is typically set when this is added to the designrelation collection
        /// </summary>
        internal DesignDataSource Owner { 
            get {
                return this.owner; 
            } 
            set {
                this.owner = value; 
            }
        }

        internal DataColumn[] ParentDataColumns { 
            get {
                if (dataRelation != null) { 
                    return dataRelation.ParentColumns; 
                }
                else if (dataForeignKeyConstraint != null) { 
                    return dataForeignKeyConstraint.RelatedColumns;
                }
                return new DataColumn[0];
            } 
        }
 
        internal DesignTable ParentDesignTable { 
            get {
                DataTable parentTable = null; 
                if (dataRelation != null) {
                    parentTable = dataRelation.ParentTable;
                }
                else if (dataForeignKeyConstraint != null) { 
                    parentTable = dataForeignKeyConstraint.RelatedTable;
                } 
 
                if (parentTable != null && Owner != null) {
                    return Owner.DesignTables[parentTable]; 
                }
                return null;
            }
        } 

        [Browsable(false)] 
        public string PublicTypeName { 
            get {
                return NAMEROOT; 
            }
        }

        [Flags] 
        public enum CompareOption {
            Columns, 
            Tables, 
            ForeignKeyConstraints
        } 


        internal string UserRelationName {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_RELATIONNAME] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_RELATIONNAME] = value;
            } 
        }

        internal string UserParentTable {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_PARENTTABLE] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_PARENTTABLE] = value;
            } 
        }

        internal string UserChildTable {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_CHILDTABLE] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_USER_CHILDTABLE] = value;
            } 
        }

        internal string GeneratorRelationVarName {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_RELATIONVARNAME] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_RELATIONVARNAME] = value;
            } 
        }

        internal string GeneratorChildPropName {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_CHILDPROPNAME] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_CHILDPROPNAME] = value;
            } 
        }

        internal string GeneratorParentPropName {
            get { 
                return this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_PARENTPROPNAME] as string;
            } 
            set { 
                this.dataRelation.ExtendedProperties[EXTPROPNAME_GENERATOR_PARENTPROPNAME] = value;
            } 
        }

        internal override StringCollection NamingPropertyNames {
            get { 
                StringCollection namingPropNames = new StringCollection();
                namingPropNames.AddRange(new string[] { "typedParent", "typedChildren" }); 
 
                return namingPropNames;
            } 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
