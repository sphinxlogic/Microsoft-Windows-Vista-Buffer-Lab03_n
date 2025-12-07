 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design{ 

    using System; 
    using System.Data;
    using System.Data.Common;
    using System.ComponentModel;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Diagnostics; 
    using System.Windows.Forms; 

 
    /// <summary>
    /// </summary>
    internal class DesignColumn : DataSourceComponent, IDataSourceNamedObject, ICloneable {
 
        private const string NullValuePropertyName = "nullValue";
        private const string NullValueThrow = "_throw"; 
 
        private DataColumn dataColumn;
        private DesignTable designTable; 

        private StringCollection namingPropNames = new StringCollection();
        internal static string EXTPROPNAME_USER_COLUMNNAME                   = "Generator_UserColumnName";
        internal static string EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINTABLE = "Generator_ColumnPropNameInTable"; 
        internal static string EXTPROPNAME_GENERATOR_COLUMNVARNAMEINTABLE = "Generator_ColumnVarNameInTable";
        internal static string EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINROW = "Generator_ColumnPropNameInRow"; 
 
        private const string ROPNAME_EXPRESSION = "Expression";
 
        public DesignColumn() {
            dataColumn = new DataColumn();
            designTable = null;
            this.namingPropNames.Add("typedName"); 
        }
 
        public DesignColumn(DataColumn dataColumn) { 
            if (dataColumn == null) {
                throw new InternalException(VSDExceptions.DataSource.DESIGN_COLUMN_NEEDS_DATA_COLUMN_MSG, 
                    VSDExceptions.DataSource.DESIGN_COLUMN_NEEDS_DATA_COLUMN_CODE);
            }

            this.dataColumn = dataColumn; 
            this.namingPropNames.Add("typedName");
        } 
 
        [
            RefreshProperties(RefreshProperties.All), 
            DefaultValue(false)
        ]
        public bool AutoIncrement {
            get { 
                return dataColumn.AutoIncrement;
            } 
            set { 
                if (dataColumn.AutoIncrement != value) {
                    Type oldDataType = DataType; 

                    dataColumn.AutoIncrement = value;

                    // DataSet will automatically update DataType when AutoIncrement is changed, we have to hack it to support Undo 
                    if (DataType != oldDataType) {
                    } 
                } 
            }
        } 

        public DataColumn DataColumn {
            get {
                return dataColumn; 
            }
        } 
 
        [
            RefreshProperties(RefreshProperties.All), 
            DefaultValue(typeof(System.String))
        ]
        public Type DataType {
            get { 
                return dataColumn.DataType;
            } 
            set { 
                if (dataColumn.DataType != value) {
 
                    bool oldAutoIncrement = AutoIncrement;
                    dataColumn.DataType = value;

                    OnDataTypeChanged(); 

                    // DataSet will automatically update AutoIncrement when DataType is changed, we have to hack it to support Undo 
                    if (AutoIncrement != oldAutoIncrement) { 
                    }
                } 
            }
        }

        internal DesignTable DesignTable { 
            get {
                return designTable; 
            } 
            set {
                designTable = value; 
            }
        }

        /// <summary> 
        /// Wrap it to add cascading "ReadOnly" undo step
        /// Refer to DataColumn.cs for set_Expression's behaviour 
        /// </summary> 
        /// <value></value>
        [ 
            RefreshProperties(RefreshProperties.All),
            DefaultValue("")
        ]
        public string Expression { 
            get {
                return this.dataColumn.Expression; 
            } 
            set {
                bool oldReadOnly = this.dataColumn.ReadOnly; 
                this.dataColumn.Expression = value;
            }
        }
 
        /// <summary>
        ///  return the object supports external properties 
        /// </summary> 
        protected override object ExternalPropertyHost {
            get { 
                return dataColumn;
            }
        }
 
        /// <summary>
        /// See VS Whidbey Bug 149549. DataColumn does not allow you to increase the MaxLength 
        /// Workaround it in design time by set it to -1 first 
        /// </summary>
        /// <value></value> 
        [
            DefaultValue(-1)
        ]
        public int MaxLength { 
            get {
                return dataColumn.MaxLength; 
            } 
            set {
                if (MaxLength >= 0 && value > MaxLength ) { 
                    dataColumn.MaxLength = -1;
                }
                dataColumn.MaxLength = value;
            } 
        }
 
        [ 
            DefaultValue(""),
            MergableProperty(false) 
        ]
        public string Name {
            get {
                return dataColumn.ColumnName; 
            }
            set { 
                string oldName = dataColumn.ColumnName; 

                if (!StringUtil.EqualValue(value, oldName)) { 
                    if (this.CollectionParent != null) {
                        this.CollectionParent.ValidateUniqueName(this, value);
                    }
 
                    dataColumn.ColumnName = value;
 
                    if (oldName.Length > 0 && value.Length > 0) { 
                        // update ColumnMapping...
                        DesignTable designTable = DesignTable; 
                        if (designTable != null) {
                            designTable.UpdateColumnMappingDataSetColumnName(oldName, value);
                        }
                    } 
                }
            } 
        } 

        [ 
            DefaultValue(NullValueThrow)
        ]
        public string NullValue {
            get { 
                if (dataColumn.ExtendedProperties.Contains(NullValuePropertyName)) {
                    return dataColumn.ExtendedProperties[NullValuePropertyName] as string; 
                } 
                return NullValueThrow;
            } 
            set {
                if (value != NullValue) {

 
                    dataColumn.ExtendedProperties[NullValuePropertyName] = value;
                } 
            } 
        }
 
        [Browsable(false)]
        public string PublicTypeName {
            get {
                return "Column"; 
            }
        } 
 
        [
            DefaultValue("") 
        ]
        public string Source {
            get {
                if (this.DesignTable != null && this.DesignTable.Mappings != null) { 
                    int index = this.DesignTable.Mappings.IndexOfDataSetColumn(this.DataColumn.ColumnName);
                    DataColumnMapping columnMapping = null; 
                    if (index >= 0){ 
                        columnMapping = this.DesignTable.Mappings.GetByDataSetColumn(this.DataColumn.ColumnName);
                    } 
                    if (columnMapping != null) {
                        return columnMapping.SourceColumn;
                    }
                } 
                return string.Empty;
            } 
            set { 
                if (this.DesignTable != null) {
                    this.DesignTable.UpdateColumnMappingSourceColumnName(this.DataColumn.ColumnName, value); 
                }
            }
        }
 
        [
            DefaultValue(false) 
        ] 
        public bool Unique {
            get { 
                return dataColumn.Unique;
            }
            set {
            } 
        }
 
        public object Clone() { 
            DataColumn dc = DataDesignUtil.CloneColumn(this.dataColumn);
            DesignColumn clone = new DesignColumn(dc); 

            return clone;
        }
 
        /// <summary>
        /// Indicate the column is part of a constraint 
        /// </summary> 
        /// <returns></returns>
        internal bool IsKeyColumn() { 
            if (DesignTable == null) {
                return false;
            }
            ArrayList list = DesignTable.GetRelatedDataConstraints(new DesignColumn[]{this}, true/*uniqueOnly, exclude*/); 
            return (list != null && list.Count > 0);
        } 
 
        /// <summary>
        /// When DataType was changed we need update NullValue 
        /// </summary>
        private void OnDataTypeChanged() {
        }
 
        public override string ToString() {
            return this.PublicTypeName + " " + this.Name; 
        } 

        internal string UserColumnName { 
            get {
                return this.dataColumn.ExtendedProperties[EXTPROPNAME_USER_COLUMNNAME] as string;
            }
            set { 
                this.dataColumn.ExtendedProperties[EXTPROPNAME_USER_COLUMNNAME] = value;
            } 
        } 

        internal string GeneratorColumnPropNameInTable { 
            get {
                return this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINTABLE] as string;
            }
            set { 
                this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINTABLE] = value;
            } 
        } 

        internal string GeneratorColumnVarNameInTable { 
            get {
                return this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNVARNAMEINTABLE] as string;
            }
            set { 
                this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNVARNAMEINTABLE] = value;
            } 
        } 

        internal string GeneratorColumnPropNameInRow { 
            get {
                return this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINROW] as string;
            }
            set { 
                this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINROW] = value;
            } 
        } 

        internal override StringCollection NamingPropertyNames { 
            get {
                return namingPropNames;
            }
        } 

        // IDataSourceRenamableObject implementation 
        [Browsable(false)] 
        public override string GeneratorName {
            get { 
                return GeneratorColumnPropNameInRow;
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
    using System.Data;
    using System.Data.Common;
    using System.ComponentModel;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Diagnostics; 
    using System.Windows.Forms; 

 
    /// <summary>
    /// </summary>
    internal class DesignColumn : DataSourceComponent, IDataSourceNamedObject, ICloneable {
 
        private const string NullValuePropertyName = "nullValue";
        private const string NullValueThrow = "_throw"; 
 
        private DataColumn dataColumn;
        private DesignTable designTable; 

        private StringCollection namingPropNames = new StringCollection();
        internal static string EXTPROPNAME_USER_COLUMNNAME                   = "Generator_UserColumnName";
        internal static string EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINTABLE = "Generator_ColumnPropNameInTable"; 
        internal static string EXTPROPNAME_GENERATOR_COLUMNVARNAMEINTABLE = "Generator_ColumnVarNameInTable";
        internal static string EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINROW = "Generator_ColumnPropNameInRow"; 
 
        private const string ROPNAME_EXPRESSION = "Expression";
 
        public DesignColumn() {
            dataColumn = new DataColumn();
            designTable = null;
            this.namingPropNames.Add("typedName"); 
        }
 
        public DesignColumn(DataColumn dataColumn) { 
            if (dataColumn == null) {
                throw new InternalException(VSDExceptions.DataSource.DESIGN_COLUMN_NEEDS_DATA_COLUMN_MSG, 
                    VSDExceptions.DataSource.DESIGN_COLUMN_NEEDS_DATA_COLUMN_CODE);
            }

            this.dataColumn = dataColumn; 
            this.namingPropNames.Add("typedName");
        } 
 
        [
            RefreshProperties(RefreshProperties.All), 
            DefaultValue(false)
        ]
        public bool AutoIncrement {
            get { 
                return dataColumn.AutoIncrement;
            } 
            set { 
                if (dataColumn.AutoIncrement != value) {
                    Type oldDataType = DataType; 

                    dataColumn.AutoIncrement = value;

                    // DataSet will automatically update DataType when AutoIncrement is changed, we have to hack it to support Undo 
                    if (DataType != oldDataType) {
                    } 
                } 
            }
        } 

        public DataColumn DataColumn {
            get {
                return dataColumn; 
            }
        } 
 
        [
            RefreshProperties(RefreshProperties.All), 
            DefaultValue(typeof(System.String))
        ]
        public Type DataType {
            get { 
                return dataColumn.DataType;
            } 
            set { 
                if (dataColumn.DataType != value) {
 
                    bool oldAutoIncrement = AutoIncrement;
                    dataColumn.DataType = value;

                    OnDataTypeChanged(); 

                    // DataSet will automatically update AutoIncrement when DataType is changed, we have to hack it to support Undo 
                    if (AutoIncrement != oldAutoIncrement) { 
                    }
                } 
            }
        }

        internal DesignTable DesignTable { 
            get {
                return designTable; 
            } 
            set {
                designTable = value; 
            }
        }

        /// <summary> 
        /// Wrap it to add cascading "ReadOnly" undo step
        /// Refer to DataColumn.cs for set_Expression's behaviour 
        /// </summary> 
        /// <value></value>
        [ 
            RefreshProperties(RefreshProperties.All),
            DefaultValue("")
        ]
        public string Expression { 
            get {
                return this.dataColumn.Expression; 
            } 
            set {
                bool oldReadOnly = this.dataColumn.ReadOnly; 
                this.dataColumn.Expression = value;
            }
        }
 
        /// <summary>
        ///  return the object supports external properties 
        /// </summary> 
        protected override object ExternalPropertyHost {
            get { 
                return dataColumn;
            }
        }
 
        /// <summary>
        /// See VS Whidbey Bug 149549. DataColumn does not allow you to increase the MaxLength 
        /// Workaround it in design time by set it to -1 first 
        /// </summary>
        /// <value></value> 
        [
            DefaultValue(-1)
        ]
        public int MaxLength { 
            get {
                return dataColumn.MaxLength; 
            } 
            set {
                if (MaxLength >= 0 && value > MaxLength ) { 
                    dataColumn.MaxLength = -1;
                }
                dataColumn.MaxLength = value;
            } 
        }
 
        [ 
            DefaultValue(""),
            MergableProperty(false) 
        ]
        public string Name {
            get {
                return dataColumn.ColumnName; 
            }
            set { 
                string oldName = dataColumn.ColumnName; 

                if (!StringUtil.EqualValue(value, oldName)) { 
                    if (this.CollectionParent != null) {
                        this.CollectionParent.ValidateUniqueName(this, value);
                    }
 
                    dataColumn.ColumnName = value;
 
                    if (oldName.Length > 0 && value.Length > 0) { 
                        // update ColumnMapping...
                        DesignTable designTable = DesignTable; 
                        if (designTable != null) {
                            designTable.UpdateColumnMappingDataSetColumnName(oldName, value);
                        }
                    } 
                }
            } 
        } 

        [ 
            DefaultValue(NullValueThrow)
        ]
        public string NullValue {
            get { 
                if (dataColumn.ExtendedProperties.Contains(NullValuePropertyName)) {
                    return dataColumn.ExtendedProperties[NullValuePropertyName] as string; 
                } 
                return NullValueThrow;
            } 
            set {
                if (value != NullValue) {

 
                    dataColumn.ExtendedProperties[NullValuePropertyName] = value;
                } 
            } 
        }
 
        [Browsable(false)]
        public string PublicTypeName {
            get {
                return "Column"; 
            }
        } 
 
        [
            DefaultValue("") 
        ]
        public string Source {
            get {
                if (this.DesignTable != null && this.DesignTable.Mappings != null) { 
                    int index = this.DesignTable.Mappings.IndexOfDataSetColumn(this.DataColumn.ColumnName);
                    DataColumnMapping columnMapping = null; 
                    if (index >= 0){ 
                        columnMapping = this.DesignTable.Mappings.GetByDataSetColumn(this.DataColumn.ColumnName);
                    } 
                    if (columnMapping != null) {
                        return columnMapping.SourceColumn;
                    }
                } 
                return string.Empty;
            } 
            set { 
                if (this.DesignTable != null) {
                    this.DesignTable.UpdateColumnMappingSourceColumnName(this.DataColumn.ColumnName, value); 
                }
            }
        }
 
        [
            DefaultValue(false) 
        ] 
        public bool Unique {
            get { 
                return dataColumn.Unique;
            }
            set {
            } 
        }
 
        public object Clone() { 
            DataColumn dc = DataDesignUtil.CloneColumn(this.dataColumn);
            DesignColumn clone = new DesignColumn(dc); 

            return clone;
        }
 
        /// <summary>
        /// Indicate the column is part of a constraint 
        /// </summary> 
        /// <returns></returns>
        internal bool IsKeyColumn() { 
            if (DesignTable == null) {
                return false;
            }
            ArrayList list = DesignTable.GetRelatedDataConstraints(new DesignColumn[]{this}, true/*uniqueOnly, exclude*/); 
            return (list != null && list.Count > 0);
        } 
 
        /// <summary>
        /// When DataType was changed we need update NullValue 
        /// </summary>
        private void OnDataTypeChanged() {
        }
 
        public override string ToString() {
            return this.PublicTypeName + " " + this.Name; 
        } 

        internal string UserColumnName { 
            get {
                return this.dataColumn.ExtendedProperties[EXTPROPNAME_USER_COLUMNNAME] as string;
            }
            set { 
                this.dataColumn.ExtendedProperties[EXTPROPNAME_USER_COLUMNNAME] = value;
            } 
        } 

        internal string GeneratorColumnPropNameInTable { 
            get {
                return this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINTABLE] as string;
            }
            set { 
                this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINTABLE] = value;
            } 
        } 

        internal string GeneratorColumnVarNameInTable { 
            get {
                return this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNVARNAMEINTABLE] as string;
            }
            set { 
                this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNVARNAMEINTABLE] = value;
            } 
        } 

        internal string GeneratorColumnPropNameInRow { 
            get {
                return this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINROW] as string;
            }
            set { 
                this.dataColumn.ExtendedProperties[EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINROW] = value;
            } 
        } 

        internal override StringCollection NamingPropertyNames { 
            get {
                return namingPropNames;
            }
        } 

        // IDataSourceRenamableObject implementation 
        [Browsable(false)] 
        public override string GeneratorName {
            get { 
                return GeneratorColumnPropNameInRow;
            }
        }
 

 
 

 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
