//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.Data;
    using System.CodeDom;
    using System.Collections;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection; 
    using System.CodeDom.Compiler; 

    internal sealed class DataTableNameHandler { 
        private MemberNameValidator validator = null;
        private bool languageCaseInsensitive = false;

        private const string onRowChangingMethodName = "OnRowChanging"; 
        private const string onRowChangedMethodName = "OnRowChanged";
        private const string onRowDeletingMethodName = "OnRowDeleting"; 
        private const string onRowDeletedMethodName = "OnRowDeleted"; 

        internal void GenerateMemberNames(DesignTable designTable, CodeDomProvider codeProvider, bool languageCaseInsensitive, ArrayList problemList) { 
            this.languageCaseInsensitive = languageCaseInsensitive;
            this.validator = new MemberNameValidator(null, codeProvider, this.languageCaseInsensitive);

            AddReservedNames(); 

            // generate names for added/renamed members, 
            ProcessMemberNames(designTable); 
        }
 
        private void AddReservedNames() {
            // add row changing/changed/deleting/deleted overrides to the table name validator
            this.validator.GetNewMemberName(onRowChangingMethodName);
            this.validator.GetNewMemberName(onRowChangedMethodName); 
            this.validator.GetNewMemberName(onRowDeletingMethodName);
            this.validator.GetNewMemberName(onRowDeletedMethodName); 
        } 

        private void ProcessMemberNames(DesignTable designTable) { 
            // process column names
            if(designTable.DesignColumns != null) {
                foreach(DesignColumn column in designTable.DesignColumns) {
                    this.ProcessColumnRelatedNames(column); 
                }
            } 
 
            // process child relation names
            DataRelationCollection childRelations = designTable.DataTable.ChildRelations; 
            if(childRelations != null) {
                foreach(DataRelation relation in childRelations) {
                    DesignRelation designRelation = FindCorrespondingDesignRelation(designTable, relation);
                    ProcessChildRelationName(designRelation); 
                }
            } 
 
            // process parent relation names
            DataRelationCollection parentRelations = designTable.DataTable.ParentRelations; 
            if(parentRelations != null) {
                foreach(DataRelation relation in parentRelations) {
                    DesignRelation designRelation = FindCorrespondingDesignRelation(designTable, relation);
                    ProcessParentRelationName(designRelation); 
                }
            } 
 
            // process event names
            ProcessEventNames(designTable); 
        }

        private DesignRelation FindCorrespondingDesignRelation(DesignTable designTable, DataRelation relation) {
            DesignDataSource dataSource = designTable.Owner; 
            if(dataSource == null) {
                throw new InternalException("Unable to find DataSource for table."); 
            } 

            foreach(DesignRelation designRelation in dataSource.DesignRelations) { 
                if(designRelation.DataRelation == null) {
                    continue;
                }
                if(StringUtil.EqualValue(designRelation.DataRelation.ChildTable.TableName, relation.ChildTable.TableName) 
                    && StringUtil.EqualValue(designRelation.DataRelation.ParentTable.TableName, relation.ParentTable.TableName)
                    && StringUtil.EqualValue(designRelation.Name, relation.RelationName)) { 
                        return designRelation; 
                }
            } 

            Debug.Assert(false, "Unable to find a designRelation corresponding to a table's parent/child DataRelation.");

            return null; 
        }
 
        private void ProcessColumnRelatedNames(DesignColumn column) { 
            bool columnRenamed = !StringUtil.EqualValue(column.Name, column.UserColumnName, this.languageCaseInsensitive);
            bool annotation = false; 
            bool storedNamingProperty = false;

            string columnPropNameInTable = this.TableColumnPropertyName(column.DataColumn, out annotation);
            string plainColumnPropNameInTable = this.PlainTableColumnPropertyName(column.DataColumn, out annotation); 
            if (annotation) {
                column.GeneratorColumnPropNameInTable = plainColumnPropNameInTable; 
            } 
            else {
                if (columnRenamed || StringUtil.Empty(column.GeneratorColumnPropNameInTable)) { 
                    column.GeneratorColumnPropNameInTable = this.validator.GenerateIdName(columnPropNameInTable);
                }
                else {
                    column.GeneratorColumnPropNameInTable = this.validator.GenerateIdName(column.GeneratorColumnPropNameInTable); 
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(columnPropNameInTable), column.GeneratorColumnPropNameInTable)) { 
                    column.NamingPropertyNames.Add(DesignColumn.EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINTABLE); 
                    storedNamingProperty = true;
                } 
            }

            string columnVarNameInTable = this.TableColumnVariableName(column.DataColumn, out annotation);
            string plainColumnVarNameInTable = this.PlainTableColumnVariableName(column.DataColumn, out annotation); 
            if (annotation) {
                column.GeneratorColumnVarNameInTable = plainColumnVarNameInTable; 
            } 
            else {
                if (columnRenamed || StringUtil.Empty(column.GeneratorColumnVarNameInTable)) { 
                    column.GeneratorColumnVarNameInTable = this.validator.GenerateIdName(columnVarNameInTable);
                }
                else {
                    column.GeneratorColumnVarNameInTable = this.validator.GenerateIdName(column.GeneratorColumnVarNameInTable); 
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(columnVarNameInTable), column.GeneratorColumnVarNameInTable)) { 
                    column.NamingPropertyNames.Add(DesignColumn.EXTPROPNAME_GENERATOR_COLUMNVARNAMEINTABLE); 
                    storedNamingProperty = true;
                } 
            }

            string columnPropNameInRow = this.RowColumnPropertyName(column.DataColumn, out annotation);
            string plainColumnPropNameInRow = this.PlainRowColumnPropertyName(column.DataColumn, out annotation); 
            if (annotation) {
                column.GeneratorColumnPropNameInRow = plainColumnPropNameInRow; 
            } 
            else {
                if (columnRenamed || StringUtil.Empty(column.GeneratorColumnPropNameInRow)) { 
                    column.GeneratorColumnPropNameInRow = this.validator.GenerateIdName(columnPropNameInRow);
                }
                else {
                    column.GeneratorColumnPropNameInRow = this.validator.GenerateIdName(column.GeneratorColumnPropNameInRow); 
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(columnPropNameInRow), column.GeneratorColumnPropNameInRow)) { 
                    column.NamingPropertyNames.Add(DesignColumn.EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINROW); 
                    storedNamingProperty = true;
                } 
            }

            column.UserColumnName = column.Name;
 
            if (storedNamingProperty) {
                column.NamingPropertyNames.Add(DesignColumn.EXTPROPNAME_USER_COLUMNNAME); 
            } 
        }
 
        internal void ProcessChildRelationName(DesignRelation relation) {
            bool relationRenamed = !StringUtil.EqualValue(relation.Name, relation.UserRelationName, this.languageCaseInsensitive)
                || !StringUtil.EqualValue(relation.ChildDesignTable.Name, relation.UserChildTable, this.languageCaseInsensitive)
                || !StringUtil.EqualValue(relation.ParentDesignTable.Name, relation.UserParentTable, this.languageCaseInsensitive); 
            bool annotation = false;
 
            string childPropName = this.ChildPropertyName(relation.DataRelation, out annotation); 
            if (annotation) {
                relation.GeneratorChildPropName = childPropName; 
            }
            else {
                if (relationRenamed || StringUtil.Empty(relation.GeneratorChildPropName)) {
                    relation.GeneratorChildPropName = this.validator.GenerateIdName(childPropName); 
                }
                else { 
                    relation.GeneratorChildPropName = this.validator.GenerateIdName(relation.GeneratorChildPropName); 
                }
            } 
        }

        internal void ProcessParentRelationName(DesignRelation relation) {
            bool relationRenamed = !StringUtil.EqualValue(relation.Name, relation.UserRelationName, this.languageCaseInsensitive) 
                || !StringUtil.EqualValue(relation.ChildDesignTable.Name, relation.UserChildTable, this.languageCaseInsensitive)
                || !StringUtil.EqualValue(relation.ParentDesignTable.Name, relation.UserParentTable, this.languageCaseInsensitive); 
            bool annotation = false; 

            string parentPropName = this.ParentPropertyName(relation.DataRelation, out annotation); 
            if (annotation) {
                relation.GeneratorParentPropName = parentPropName;
            }
            else { 
                if (relationRenamed || StringUtil.Empty(relation.GeneratorParentPropName)) {
                    relation.GeneratorParentPropName = this.validator.GenerateIdName(parentPropName); 
                } 
                else {
                    relation.GeneratorParentPropName = this.validator.GenerateIdName(relation.GeneratorParentPropName); 
                }
            }
        }
 
        internal void ProcessEventNames(DesignTable designTable) {
            bool storedNamingProperty = false; 
            bool tableRenamed = !StringUtil.EqualValue(designTable.Name, designTable.UserTableName, this.languageCaseInsensitive); 

            string rowChangingName = designTable.GeneratorRowClassName + "Changing"; 
            if (tableRenamed || StringUtil.Empty(designTable.GeneratorRowChangingName)) {
                designTable.GeneratorRowChangingName = this.validator.GenerateIdName(rowChangingName);
            }
            else { 
                designTable.GeneratorRowChangingName = this.validator.GenerateIdName(designTable.GeneratorRowChangingName);
            } 
 
            if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowChangingName), designTable.GeneratorRowChangingName)) {
                designTable.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWCHANGINGNAME); 
                storedNamingProperty = true;
            }

            string rowChangedName = designTable.GeneratorRowClassName + "Changed"; 
            if (tableRenamed || StringUtil.Empty(designTable.GeneratorRowChangedName)) {
                designTable.GeneratorRowChangedName = this.validator.GenerateIdName(rowChangedName); 
            } 
            else {
                designTable.GeneratorRowChangedName = this.validator.GenerateIdName(designTable.GeneratorRowChangedName); 
            }

            if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowChangedName), designTable.GeneratorRowChangedName)) {
                designTable.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWCHANGEDNAME); 
                storedNamingProperty = true;
            } 
 
            string rowDeletingName = designTable.GeneratorRowClassName + "Deleting";
            if (tableRenamed || StringUtil.Empty(designTable.GeneratorRowDeletingName)) { 
                designTable.GeneratorRowDeletingName = this.validator.GenerateIdName(rowDeletingName);
            }
            else {
                designTable.GeneratorRowDeletingName = this.validator.GenerateIdName(designTable.GeneratorRowDeletingName); 
            }
 
            if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowDeletingName), designTable.GeneratorRowDeletingName)) { 
                designTable.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWDELETINGNAME);
                storedNamingProperty = true; 
            }

            string rowDeletedName = designTable.GeneratorRowClassName + "Deleted";
            if (tableRenamed || StringUtil.Empty(designTable.GeneratorRowDeletedName)) { 
                designTable.GeneratorRowDeletedName = this.validator.GenerateIdName(rowDeletedName);
            } 
            else { 
                designTable.GeneratorRowDeletedName = this.validator.GenerateIdName(designTable.GeneratorRowDeletedName);
            } 

            if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowDeletedName), designTable.GeneratorRowDeletedName)) {
                designTable.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWDELETEDNAME);
                storedNamingProperty = true; 
            }
 
            if (storedNamingProperty) { 
                if (!designTable.NamingPropertyNames.Contains(DesignTable.EXTPROPNAME_USER_TABLENAME)) {
                    designTable.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_USER_TABLENAME); 
                }
            }
        }
 
        private string RowColumnPropertyName(DataColumn column, out bool usesAnnotations) {
            usesAnnotations = true; 
            string typedName = (string) column.ExtendedProperties["typedName"]; 

            if(StringUtil.Empty(typedName)) { 
                usesAnnotations = false;
                typedName = NameHandler.FixIdName(column.ColumnName);
            }
 
            return typedName;
        } 
 
        private string PlainRowColumnPropertyName(DataColumn column, out bool usesAnnotations) {
            usesAnnotations = true; 
            string typedName = (string)column.ExtendedProperties["typedName"];

            if (StringUtil.Empty(typedName)) {
                usesAnnotations = false; 
                typedName = column.ColumnName;
            } 
 
            return typedName;
        } 

        private string TableColumnVariableName(DataColumn column, out bool usesAnnotations) {
            string columnName = RowColumnPropertyName(column, out usesAnnotations);
            string variableName = null; 

            if(StringUtil.EqualValue("column", columnName, true)) { 
                variableName = "columnField" + columnName; 
            }
            else { 
                variableName = "column" + columnName;
            }

            if (!StringUtil.EqualValue(variableName, "Columns", this.languageCaseInsensitive)) { 
                return variableName;
            } 
            else { 
                return "_" + variableName;
            } 
        }

        private string PlainTableColumnVariableName(DataColumn column, out bool usesAnnotations) {
            return "column" + PlainRowColumnPropertyName(column, out usesAnnotations); 
        }
 
        private string TableColumnPropertyName(DataColumn column, out bool usesAnnotations) { 
            return RowColumnPropertyName(column, out usesAnnotations) + "Column";
        } 

        private string PlainTableColumnPropertyName(DataColumn column, out bool usesAnnotations) {
            return PlainRowColumnPropertyName(column, out usesAnnotations) + "Column";
        } 

        private string ChildPropertyName(DataRelation relation, out bool usesAnnotations) { 
            usesAnnotations = true; 
            string typedName = (string) relation.ExtendedProperties["typedChildren"];
 
            if(StringUtil.Empty(typedName)) {
                string arrayName = (string)relation.ChildTable.ExtendedProperties["typedPlural"];

                if(StringUtil.Empty(arrayName)) { 
                    arrayName = (string)relation.ChildTable.ExtendedProperties["typedName"];
 
                    if(StringUtil.Empty(arrayName)) { 
                        usesAnnotations = false;
                        typedName = "Get" + relation.ChildTable.TableName + "Rows"; 
                        if(1 < TablesConnectedness(relation.ParentTable, relation.ChildTable)) {
                            typedName += "By" + relation.RelationName;
                        }
 
                        return NameHandler.FixIdName(typedName);
                    } 
 
                    arrayName += "Rows";
                } 

                typedName = "Get" + arrayName;
            }
 
            return typedName;
        } 
 
        private string ParentPropertyName(DataRelation relation, out bool usesAnnotations) {
            usesAnnotations = true; 
            string typedName = null;
            typedName = (string) relation.ExtendedProperties["typedParent"];

            if(StringUtil.Empty(typedName)) { 
                typedName = this.RowClassName(relation.ParentTable, out usesAnnotations);
                if(relation.ChildTable == relation.ParentTable || relation.ChildColumns.Length != 1) { 
                    // Complex case self-join, multicolumn key 
                    typedName += "Parent";
                } 

                if(1 < TablesConnectedness(relation.ParentTable, relation.ChildTable)) {
                    typedName += "By" + NameHandler.FixIdName(relation.RelationName);
                } 
            }
 
            return typedName; 
        }
 
        private static int TablesConnectedness(DataTable parentTable, DataTable childTable) {
            int connectedness = 0;
            DataRelationCollection relations = childTable.ParentRelations;
            for (int i = 0; i < relations.Count; i++) { 
                if (relations[i].ParentTable == parentTable) {
                    connectedness++; 
                } 
            }
            return connectedness; 
        }

        // Name of a class for typed row
        private string RowClassName(DataTable table, out bool usesAnnotations) { 
            usesAnnotations = true;
            string className = (string) table.ExtendedProperties["typedName"]; 
 
            if(StringUtil.Empty(className)) {
                usesAnnotations = false; 
                className = table.TableName + "Row";
            }

            return className; 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.Data;
    using System.CodeDom;
    using System.Collections;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection; 
    using System.CodeDom.Compiler; 

    internal sealed class DataTableNameHandler { 
        private MemberNameValidator validator = null;
        private bool languageCaseInsensitive = false;

        private const string onRowChangingMethodName = "OnRowChanging"; 
        private const string onRowChangedMethodName = "OnRowChanged";
        private const string onRowDeletingMethodName = "OnRowDeleting"; 
        private const string onRowDeletedMethodName = "OnRowDeleted"; 

        internal void GenerateMemberNames(DesignTable designTable, CodeDomProvider codeProvider, bool languageCaseInsensitive, ArrayList problemList) { 
            this.languageCaseInsensitive = languageCaseInsensitive;
            this.validator = new MemberNameValidator(null, codeProvider, this.languageCaseInsensitive);

            AddReservedNames(); 

            // generate names for added/renamed members, 
            ProcessMemberNames(designTable); 
        }
 
        private void AddReservedNames() {
            // add row changing/changed/deleting/deleted overrides to the table name validator
            this.validator.GetNewMemberName(onRowChangingMethodName);
            this.validator.GetNewMemberName(onRowChangedMethodName); 
            this.validator.GetNewMemberName(onRowDeletingMethodName);
            this.validator.GetNewMemberName(onRowDeletedMethodName); 
        } 

        private void ProcessMemberNames(DesignTable designTable) { 
            // process column names
            if(designTable.DesignColumns != null) {
                foreach(DesignColumn column in designTable.DesignColumns) {
                    this.ProcessColumnRelatedNames(column); 
                }
            } 
 
            // process child relation names
            DataRelationCollection childRelations = designTable.DataTable.ChildRelations; 
            if(childRelations != null) {
                foreach(DataRelation relation in childRelations) {
                    DesignRelation designRelation = FindCorrespondingDesignRelation(designTable, relation);
                    ProcessChildRelationName(designRelation); 
                }
            } 
 
            // process parent relation names
            DataRelationCollection parentRelations = designTable.DataTable.ParentRelations; 
            if(parentRelations != null) {
                foreach(DataRelation relation in parentRelations) {
                    DesignRelation designRelation = FindCorrespondingDesignRelation(designTable, relation);
                    ProcessParentRelationName(designRelation); 
                }
            } 
 
            // process event names
            ProcessEventNames(designTable); 
        }

        private DesignRelation FindCorrespondingDesignRelation(DesignTable designTable, DataRelation relation) {
            DesignDataSource dataSource = designTable.Owner; 
            if(dataSource == null) {
                throw new InternalException("Unable to find DataSource for table."); 
            } 

            foreach(DesignRelation designRelation in dataSource.DesignRelations) { 
                if(designRelation.DataRelation == null) {
                    continue;
                }
                if(StringUtil.EqualValue(designRelation.DataRelation.ChildTable.TableName, relation.ChildTable.TableName) 
                    && StringUtil.EqualValue(designRelation.DataRelation.ParentTable.TableName, relation.ParentTable.TableName)
                    && StringUtil.EqualValue(designRelation.Name, relation.RelationName)) { 
                        return designRelation; 
                }
            } 

            Debug.Assert(false, "Unable to find a designRelation corresponding to a table's parent/child DataRelation.");

            return null; 
        }
 
        private void ProcessColumnRelatedNames(DesignColumn column) { 
            bool columnRenamed = !StringUtil.EqualValue(column.Name, column.UserColumnName, this.languageCaseInsensitive);
            bool annotation = false; 
            bool storedNamingProperty = false;

            string columnPropNameInTable = this.TableColumnPropertyName(column.DataColumn, out annotation);
            string plainColumnPropNameInTable = this.PlainTableColumnPropertyName(column.DataColumn, out annotation); 
            if (annotation) {
                column.GeneratorColumnPropNameInTable = plainColumnPropNameInTable; 
            } 
            else {
                if (columnRenamed || StringUtil.Empty(column.GeneratorColumnPropNameInTable)) { 
                    column.GeneratorColumnPropNameInTable = this.validator.GenerateIdName(columnPropNameInTable);
                }
                else {
                    column.GeneratorColumnPropNameInTable = this.validator.GenerateIdName(column.GeneratorColumnPropNameInTable); 
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(columnPropNameInTable), column.GeneratorColumnPropNameInTable)) { 
                    column.NamingPropertyNames.Add(DesignColumn.EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINTABLE); 
                    storedNamingProperty = true;
                } 
            }

            string columnVarNameInTable = this.TableColumnVariableName(column.DataColumn, out annotation);
            string plainColumnVarNameInTable = this.PlainTableColumnVariableName(column.DataColumn, out annotation); 
            if (annotation) {
                column.GeneratorColumnVarNameInTable = plainColumnVarNameInTable; 
            } 
            else {
                if (columnRenamed || StringUtil.Empty(column.GeneratorColumnVarNameInTable)) { 
                    column.GeneratorColumnVarNameInTable = this.validator.GenerateIdName(columnVarNameInTable);
                }
                else {
                    column.GeneratorColumnVarNameInTable = this.validator.GenerateIdName(column.GeneratorColumnVarNameInTable); 
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(columnVarNameInTable), column.GeneratorColumnVarNameInTable)) { 
                    column.NamingPropertyNames.Add(DesignColumn.EXTPROPNAME_GENERATOR_COLUMNVARNAMEINTABLE); 
                    storedNamingProperty = true;
                } 
            }

            string columnPropNameInRow = this.RowColumnPropertyName(column.DataColumn, out annotation);
            string plainColumnPropNameInRow = this.PlainRowColumnPropertyName(column.DataColumn, out annotation); 
            if (annotation) {
                column.GeneratorColumnPropNameInRow = plainColumnPropNameInRow; 
            } 
            else {
                if (columnRenamed || StringUtil.Empty(column.GeneratorColumnPropNameInRow)) { 
                    column.GeneratorColumnPropNameInRow = this.validator.GenerateIdName(columnPropNameInRow);
                }
                else {
                    column.GeneratorColumnPropNameInRow = this.validator.GenerateIdName(column.GeneratorColumnPropNameInRow); 
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(columnPropNameInRow), column.GeneratorColumnPropNameInRow)) { 
                    column.NamingPropertyNames.Add(DesignColumn.EXTPROPNAME_GENERATOR_COLUMNPROPNAMEINROW); 
                    storedNamingProperty = true;
                } 
            }

            column.UserColumnName = column.Name;
 
            if (storedNamingProperty) {
                column.NamingPropertyNames.Add(DesignColumn.EXTPROPNAME_USER_COLUMNNAME); 
            } 
        }
 
        internal void ProcessChildRelationName(DesignRelation relation) {
            bool relationRenamed = !StringUtil.EqualValue(relation.Name, relation.UserRelationName, this.languageCaseInsensitive)
                || !StringUtil.EqualValue(relation.ChildDesignTable.Name, relation.UserChildTable, this.languageCaseInsensitive)
                || !StringUtil.EqualValue(relation.ParentDesignTable.Name, relation.UserParentTable, this.languageCaseInsensitive); 
            bool annotation = false;
 
            string childPropName = this.ChildPropertyName(relation.DataRelation, out annotation); 
            if (annotation) {
                relation.GeneratorChildPropName = childPropName; 
            }
            else {
                if (relationRenamed || StringUtil.Empty(relation.GeneratorChildPropName)) {
                    relation.GeneratorChildPropName = this.validator.GenerateIdName(childPropName); 
                }
                else { 
                    relation.GeneratorChildPropName = this.validator.GenerateIdName(relation.GeneratorChildPropName); 
                }
            } 
        }

        internal void ProcessParentRelationName(DesignRelation relation) {
            bool relationRenamed = !StringUtil.EqualValue(relation.Name, relation.UserRelationName, this.languageCaseInsensitive) 
                || !StringUtil.EqualValue(relation.ChildDesignTable.Name, relation.UserChildTable, this.languageCaseInsensitive)
                || !StringUtil.EqualValue(relation.ParentDesignTable.Name, relation.UserParentTable, this.languageCaseInsensitive); 
            bool annotation = false; 

            string parentPropName = this.ParentPropertyName(relation.DataRelation, out annotation); 
            if (annotation) {
                relation.GeneratorParentPropName = parentPropName;
            }
            else { 
                if (relationRenamed || StringUtil.Empty(relation.GeneratorParentPropName)) {
                    relation.GeneratorParentPropName = this.validator.GenerateIdName(parentPropName); 
                } 
                else {
                    relation.GeneratorParentPropName = this.validator.GenerateIdName(relation.GeneratorParentPropName); 
                }
            }
        }
 
        internal void ProcessEventNames(DesignTable designTable) {
            bool storedNamingProperty = false; 
            bool tableRenamed = !StringUtil.EqualValue(designTable.Name, designTable.UserTableName, this.languageCaseInsensitive); 

            string rowChangingName = designTable.GeneratorRowClassName + "Changing"; 
            if (tableRenamed || StringUtil.Empty(designTable.GeneratorRowChangingName)) {
                designTable.GeneratorRowChangingName = this.validator.GenerateIdName(rowChangingName);
            }
            else { 
                designTable.GeneratorRowChangingName = this.validator.GenerateIdName(designTable.GeneratorRowChangingName);
            } 
 
            if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowChangingName), designTable.GeneratorRowChangingName)) {
                designTable.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWCHANGINGNAME); 
                storedNamingProperty = true;
            }

            string rowChangedName = designTable.GeneratorRowClassName + "Changed"; 
            if (tableRenamed || StringUtil.Empty(designTable.GeneratorRowChangedName)) {
                designTable.GeneratorRowChangedName = this.validator.GenerateIdName(rowChangedName); 
            } 
            else {
                designTable.GeneratorRowChangedName = this.validator.GenerateIdName(designTable.GeneratorRowChangedName); 
            }

            if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowChangedName), designTable.GeneratorRowChangedName)) {
                designTable.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWCHANGEDNAME); 
                storedNamingProperty = true;
            } 
 
            string rowDeletingName = designTable.GeneratorRowClassName + "Deleting";
            if (tableRenamed || StringUtil.Empty(designTable.GeneratorRowDeletingName)) { 
                designTable.GeneratorRowDeletingName = this.validator.GenerateIdName(rowDeletingName);
            }
            else {
                designTable.GeneratorRowDeletingName = this.validator.GenerateIdName(designTable.GeneratorRowDeletingName); 
            }
 
            if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowDeletingName), designTable.GeneratorRowDeletingName)) { 
                designTable.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWDELETINGNAME);
                storedNamingProperty = true; 
            }

            string rowDeletedName = designTable.GeneratorRowClassName + "Deleted";
            if (tableRenamed || StringUtil.Empty(designTable.GeneratorRowDeletedName)) { 
                designTable.GeneratorRowDeletedName = this.validator.GenerateIdName(rowDeletedName);
            } 
            else { 
                designTable.GeneratorRowDeletedName = this.validator.GenerateIdName(designTable.GeneratorRowDeletedName);
            } 

            if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowDeletedName), designTable.GeneratorRowDeletedName)) {
                designTable.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWDELETEDNAME);
                storedNamingProperty = true; 
            }
 
            if (storedNamingProperty) { 
                if (!designTable.NamingPropertyNames.Contains(DesignTable.EXTPROPNAME_USER_TABLENAME)) {
                    designTable.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_USER_TABLENAME); 
                }
            }
        }
 
        private string RowColumnPropertyName(DataColumn column, out bool usesAnnotations) {
            usesAnnotations = true; 
            string typedName = (string) column.ExtendedProperties["typedName"]; 

            if(StringUtil.Empty(typedName)) { 
                usesAnnotations = false;
                typedName = NameHandler.FixIdName(column.ColumnName);
            }
 
            return typedName;
        } 
 
        private string PlainRowColumnPropertyName(DataColumn column, out bool usesAnnotations) {
            usesAnnotations = true; 
            string typedName = (string)column.ExtendedProperties["typedName"];

            if (StringUtil.Empty(typedName)) {
                usesAnnotations = false; 
                typedName = column.ColumnName;
            } 
 
            return typedName;
        } 

        private string TableColumnVariableName(DataColumn column, out bool usesAnnotations) {
            string columnName = RowColumnPropertyName(column, out usesAnnotations);
            string variableName = null; 

            if(StringUtil.EqualValue("column", columnName, true)) { 
                variableName = "columnField" + columnName; 
            }
            else { 
                variableName = "column" + columnName;
            }

            if (!StringUtil.EqualValue(variableName, "Columns", this.languageCaseInsensitive)) { 
                return variableName;
            } 
            else { 
                return "_" + variableName;
            } 
        }

        private string PlainTableColumnVariableName(DataColumn column, out bool usesAnnotations) {
            return "column" + PlainRowColumnPropertyName(column, out usesAnnotations); 
        }
 
        private string TableColumnPropertyName(DataColumn column, out bool usesAnnotations) { 
            return RowColumnPropertyName(column, out usesAnnotations) + "Column";
        } 

        private string PlainTableColumnPropertyName(DataColumn column, out bool usesAnnotations) {
            return PlainRowColumnPropertyName(column, out usesAnnotations) + "Column";
        } 

        private string ChildPropertyName(DataRelation relation, out bool usesAnnotations) { 
            usesAnnotations = true; 
            string typedName = (string) relation.ExtendedProperties["typedChildren"];
 
            if(StringUtil.Empty(typedName)) {
                string arrayName = (string)relation.ChildTable.ExtendedProperties["typedPlural"];

                if(StringUtil.Empty(arrayName)) { 
                    arrayName = (string)relation.ChildTable.ExtendedProperties["typedName"];
 
                    if(StringUtil.Empty(arrayName)) { 
                        usesAnnotations = false;
                        typedName = "Get" + relation.ChildTable.TableName + "Rows"; 
                        if(1 < TablesConnectedness(relation.ParentTable, relation.ChildTable)) {
                            typedName += "By" + relation.RelationName;
                        }
 
                        return NameHandler.FixIdName(typedName);
                    } 
 
                    arrayName += "Rows";
                } 

                typedName = "Get" + arrayName;
            }
 
            return typedName;
        } 
 
        private string ParentPropertyName(DataRelation relation, out bool usesAnnotations) {
            usesAnnotations = true; 
            string typedName = null;
            typedName = (string) relation.ExtendedProperties["typedParent"];

            if(StringUtil.Empty(typedName)) { 
                typedName = this.RowClassName(relation.ParentTable, out usesAnnotations);
                if(relation.ChildTable == relation.ParentTable || relation.ChildColumns.Length != 1) { 
                    // Complex case self-join, multicolumn key 
                    typedName += "Parent";
                } 

                if(1 < TablesConnectedness(relation.ParentTable, relation.ChildTable)) {
                    typedName += "By" + NameHandler.FixIdName(relation.RelationName);
                } 
            }
 
            return typedName; 
        }
 
        private static int TablesConnectedness(DataTable parentTable, DataTable childTable) {
            int connectedness = 0;
            DataRelationCollection relations = childTable.ParentRelations;
            for (int i = 0; i < relations.Count; i++) { 
                if (relations[i].ParentTable == parentTable) {
                    connectedness++; 
                } 
            }
            return connectedness; 
        }

        // Name of a class for typed row
        private string RowClassName(DataTable table, out bool usesAnnotations) { 
            usesAnnotations = true;
            string className = (string) table.ExtendedProperties["typedName"]; 
 
            if(StringUtil.Empty(className)) {
                usesAnnotations = false; 
                className = table.TableName + "Row";
            }

            return className; 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
