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
    using System.Design; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Reflection; 
    using System.CodeDom.Compiler;
 
    internal sealed class DataSourceNameHandler {
        private MemberNameValidator validator = null;
        private bool languageCaseInsensitive = false;
        private static string tablesPropertyName = "Tables"; 
        private static string relationsPropertyName = "Relations";
        //private static string enforceConstraintsPropertyName = "EnforceConstraints"; 
 
        internal void GenerateMemberNames(DesignDataSource dataSource, CodeDomProvider codeProvider, bool languageCaseInsensitive, ArrayList problemList) {
            this.languageCaseInsensitive = languageCaseInsensitive; 
            validator = new MemberNameValidator(new string[] { tablesPropertyName, relationsPropertyName/*, enforceConstraintsPropertyName*/ }, codeProvider, this.languageCaseInsensitive);

            // generate names for added/renamed members,
            ProcessMemberNames(dataSource); 
        }
 
        internal void ProcessMemberNames(DesignDataSource dataSource) { 

            ProcessDataSourceName(dataSource); 

            // process table and row related names
            if(dataSource.DesignTables != null) {
                foreach(DesignTable table in dataSource.DesignTables) { 
                    this.ProcessTableRelatedNames(table);
                } 
            } 

            // process relation related names 
            if(dataSource.DesignRelations != null) {
                foreach(DesignRelation relation in dataSource.DesignRelations) {
                    ProcessRelationRelatedNames(relation);
                } 
            }
        } 
 
        internal void ProcessDataSourceName(DesignDataSource dataSource) {
            if(StringUtil.Empty(dataSource.Name)) { 
                throw new DataSourceGeneratorException(SR.GetString(SR.CG_EmptyDSName));
            }

            bool objectRenamed = !StringUtil.EqualValue(dataSource.Name, dataSource.UserDataSetName, this.languageCaseInsensitive); 

            if (objectRenamed || StringUtil.Empty(dataSource.GeneratorDataSetName)) { 
                dataSource.GeneratorDataSetName = NameHandler.FixIdName(dataSource.Name); 
            }
            else { 
                dataSource.GeneratorDataSetName = this.validator.GenerateIdName(dataSource.GeneratorDataSetName);
            }

            dataSource.UserDataSetName = dataSource.Name; 

            if (!StringUtil.EqualValue(NameHandler.FixIdName(dataSource.Name), dataSource.GeneratorDataSetName)) { 
                dataSource.NamingPropertyNames.Add(DesignDataSource.EXTPROPNAME_USER_DATASETNAME); 
                dataSource.NamingPropertyNames.Add(DesignDataSource.EXTPROPNAME_GENERATOR_DATASETNAME);
            } 
        }

        internal void ProcessTableRelatedNames(DesignTable table) {
            bool annotation = false; 
            bool storedNamingProperty = false;
            bool objectRenamed = !StringUtil.EqualValue(table.Name, table.UserTableName, this.languageCaseInsensitive); 
 

            string tablePropName = this.TablePropertyName(table.DataTable, out annotation); 
            string plainTablePropName = this.PlainTablePropertyName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorTablePropName = plainTablePropName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorTablePropName)) { 
                    table.GeneratorTablePropName = this.validator.GenerateIdName(tablePropName); 
                }
                else { 
                    table.GeneratorTablePropName = this.validator.GenerateIdName(table.GeneratorTablePropName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(tablePropName), table.GeneratorTablePropName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_TABLEPROPNAME); 
                    storedNamingProperty = true;
                } 
            } 

            string tableVarName = this.TableVariableName(table.DataTable, out annotation); 
            string plainTableVarName = this.PlainTableVariableName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorTableVarName = plainTableVarName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorTableVarName)) { 
                    table.GeneratorTableVarName = this.validator.GenerateIdName(tableVarName); 
                }
                else { 
                    table.GeneratorTableVarName = this.validator.GenerateIdName(table.GeneratorTableVarName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(tableVarName), table.GeneratorTableVarName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_TABLEVARNAME); 
                    storedNamingProperty = true;
                } 
            } 

            string tableClassName = this.TableClassName(table.DataTable, out annotation); 
            string plainTableClassName = this.PlainTableClassName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorTableClassName = plainTableClassName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorTableClassName)) { 
                    table.GeneratorTableClassName = this.validator.GenerateIdName(tableClassName); 
                }
                else { 
                    table.GeneratorTableClassName = this.validator.GenerateIdName(table.GeneratorTableClassName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(tableClassName), table.GeneratorTableClassName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_TABLECLASSNAME); 
                    storedNamingProperty = true;
                } 
            } 

            string rowClassName = this.RowClassName(table.DataTable, out annotation); 
            string plainRowClassName = this.PlainRowClassName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorRowClassName = plainRowClassName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorRowClassName)) { 
                    table.GeneratorRowClassName = this.validator.GenerateIdName(rowClassName); 
                }
                else { 
                    table.GeneratorRowClassName = this.validator.GenerateIdName(table.GeneratorRowClassName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowClassName), table.GeneratorRowClassName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWCLASSNAME); 
                    storedNamingProperty = true;
                } 
            } 

            string rowEventHandlerName = this.RowEventHandlerName(table.DataTable, out annotation); 
            string plainRowEventHandlerName = this.PlainRowEventHandlerName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorRowEvHandlerName = plainRowEventHandlerName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorRowEvHandlerName)) { 
                    table.GeneratorRowEvHandlerName = this.validator.GenerateIdName(rowEventHandlerName); 
                }
                else { 
                    table.GeneratorRowEvHandlerName = this.validator.GenerateIdName(table.GeneratorRowEvHandlerName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowEventHandlerName), table.GeneratorRowEvHandlerName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWEVHANDLERNAME); 
                    storedNamingProperty = true;
                } 
            } 

            string rowEventArgName = this.RowEventArgClassName(table.DataTable, out annotation); 
            string plainRowEventArgName = this.PlainRowEventArgClassName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorRowEvArgName = plainRowEventArgName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorRowEvArgName)) { 
                    table.GeneratorRowEvArgName = this.validator.GenerateIdName(rowEventArgName); 
                }
                else { 
                    table.GeneratorRowEvArgName = this.validator.GenerateIdName(table.GeneratorRowEvArgName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowEventArgName), table.GeneratorRowEvArgName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWEVARGNAME); 
                    storedNamingProperty = true;
                } 
            } 

            if (storedNamingProperty) { 
                table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_USER_TABLENAME);
            }
        }
 
        internal void ProcessRelationRelatedNames(DesignRelation relation) {
            if(relation.DataRelation == null) { 
                return; 
            }
 
            bool objectRenamed = !StringUtil.EqualValue(relation.Name, relation.UserRelationName, this.languageCaseInsensitive);

            if (objectRenamed || StringUtil.Empty(relation.GeneratorRelationVarName)) {
                relation.GeneratorRelationVarName = this.validator.GenerateIdName(this.RelationVariableName(relation.DataRelation)); 
            }
            else { 
                relation.GeneratorRelationVarName = this.validator.GenerateIdName(relation.GeneratorRelationVarName); 
            }
        } 

        internal static string TablesPropertyName {
            get {
                return tablesPropertyName; 
            }
        } 
 
        internal static string RelationsPropertyName {
            get { 
                return relationsPropertyName;
            }
        }
 
        //internal static string EnforceConstraintsPropertyName {
        //    get { 
        //        return enforceConstraintsPropertyName; 
        //    }
        //} 

        // Typed table class name
        private string TableClassName(DataTable table, out bool usesAnnotations) {
            usesAnnotations = true; 
            string className = (string)table.ExtendedProperties["typedPlural"];
 
            if(StringUtil.Empty(className)) { 
                className = (string)table.ExtendedProperties["typedName"];
                if(StringUtil.Empty(className)) { 
                    usesAnnotations = false;
                    className = NameHandler.FixIdName(table.TableName);
                }
            } 

            return className + "DataTable"; 
        } 

        private string PlainTableClassName(DataTable table, out bool usesAnnotations) { 
            usesAnnotations = true;
            string className = (string)table.ExtendedProperties["typedPlural"];

            if (StringUtil.Empty(className)) { 
                className = (string)table.ExtendedProperties["typedName"];
                if (StringUtil.Empty(className)) { 
                    usesAnnotations = false; 
                    className = table.TableName;
                } 
            }

            return className + "DataTable";
        } 

        // Name of the property of typed dataset wich returns typed table: 
        private string TablePropertyName(DataTable table, out bool usesAnnotations) { 
            usesAnnotations = true;
            string typedName = (string)table.ExtendedProperties["typedPlural"]; 

            if(StringUtil.Empty(typedName)) {
                typedName = (string)table.ExtendedProperties["typedName"];
                if(StringUtil.Empty(typedName)) { 
                    usesAnnotations = false;
                    typedName = NameHandler.FixIdName(table.TableName); 
                } 
                else {
                    typedName = typedName + "Table"; 
                }
            }

            return typedName; 
        }
 
        private string PlainTablePropertyName(DataTable table, out bool usesAnnotations) { 
            usesAnnotations = true;
            string typedName = (string)table.ExtendedProperties["typedPlural"]; 

            if (StringUtil.Empty(typedName)) {
                typedName = (string)table.ExtendedProperties["typedName"];
                if (StringUtil.Empty(typedName)) { 
                    usesAnnotations = false;
                    typedName = table.TableName; 
                } 
                else {
                    typedName = typedName + "Table"; 
                }
            }

            return typedName; 
        }
 
        // Name of the variable of typed dataset wich holds typed table 
        private string TableVariableName(DataTable table, out bool usesAnnotations) {
            return "table" + TablePropertyName(table, out usesAnnotations); 
        }

        private string PlainTableVariableName(DataTable table, out bool usesAnnotations) {
            return "table" + PlainTablePropertyName(table, out usesAnnotations); 
        }
 
        // Name of a class for typed row 
        private string RowClassName(DataTable table, out bool usesAnnotations) {
            usesAnnotations = true; 
            string className = (string) table.ExtendedProperties["typedName"];

            if(StringUtil.Empty(className)) {
                usesAnnotations = false; 
                className = NameHandler.FixIdName(table.TableName) + "Row";
            } 
 
            return className;
        } 

        private string PlainRowClassName(DataTable table, out bool usesAnnotations) {
            usesAnnotations = true;
            string className = (string)table.ExtendedProperties["typedName"]; 

            if (StringUtil.Empty(className)) { 
                usesAnnotations = false; 
                className = table.TableName + "Row";
            } 

            return className;
        }
 
        // Name of row event arg class
        private string RowEventArgClassName(DataTable table, out bool usesAnnotations) { 
            return RowClassName(table, out usesAnnotations) + "ChangeEvent"; 
        }
 
        private string PlainRowEventArgClassName(DataTable table, out bool usesAnnotations) {
            return PlainRowClassName(table, out usesAnnotations) + "ChangeEvent";
        }
 
        // Name of row event handler
        private string RowEventHandlerName(DataTable table, out bool usesAnnotations) { 
            return RowClassName(table, out usesAnnotations) + "ChangeEventHandler"; 
        }
 
        private string PlainRowEventHandlerName(DataTable table, out bool usesAnnotations) {
            return PlainRowClassName(table, out usesAnnotations) + "ChangeEventHandler";
        }
 
        // Name of private variable for relation
        private string RelationVariableName(DataRelation relation) { 
            return NameHandler.FixIdName("relation" + relation.RelationName); 
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
    using System.Design; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Reflection; 
    using System.CodeDom.Compiler;
 
    internal sealed class DataSourceNameHandler {
        private MemberNameValidator validator = null;
        private bool languageCaseInsensitive = false;
        private static string tablesPropertyName = "Tables"; 
        private static string relationsPropertyName = "Relations";
        //private static string enforceConstraintsPropertyName = "EnforceConstraints"; 
 
        internal void GenerateMemberNames(DesignDataSource dataSource, CodeDomProvider codeProvider, bool languageCaseInsensitive, ArrayList problemList) {
            this.languageCaseInsensitive = languageCaseInsensitive; 
            validator = new MemberNameValidator(new string[] { tablesPropertyName, relationsPropertyName/*, enforceConstraintsPropertyName*/ }, codeProvider, this.languageCaseInsensitive);

            // generate names for added/renamed members,
            ProcessMemberNames(dataSource); 
        }
 
        internal void ProcessMemberNames(DesignDataSource dataSource) { 

            ProcessDataSourceName(dataSource); 

            // process table and row related names
            if(dataSource.DesignTables != null) {
                foreach(DesignTable table in dataSource.DesignTables) { 
                    this.ProcessTableRelatedNames(table);
                } 
            } 

            // process relation related names 
            if(dataSource.DesignRelations != null) {
                foreach(DesignRelation relation in dataSource.DesignRelations) {
                    ProcessRelationRelatedNames(relation);
                } 
            }
        } 
 
        internal void ProcessDataSourceName(DesignDataSource dataSource) {
            if(StringUtil.Empty(dataSource.Name)) { 
                throw new DataSourceGeneratorException(SR.GetString(SR.CG_EmptyDSName));
            }

            bool objectRenamed = !StringUtil.EqualValue(dataSource.Name, dataSource.UserDataSetName, this.languageCaseInsensitive); 

            if (objectRenamed || StringUtil.Empty(dataSource.GeneratorDataSetName)) { 
                dataSource.GeneratorDataSetName = NameHandler.FixIdName(dataSource.Name); 
            }
            else { 
                dataSource.GeneratorDataSetName = this.validator.GenerateIdName(dataSource.GeneratorDataSetName);
            }

            dataSource.UserDataSetName = dataSource.Name; 

            if (!StringUtil.EqualValue(NameHandler.FixIdName(dataSource.Name), dataSource.GeneratorDataSetName)) { 
                dataSource.NamingPropertyNames.Add(DesignDataSource.EXTPROPNAME_USER_DATASETNAME); 
                dataSource.NamingPropertyNames.Add(DesignDataSource.EXTPROPNAME_GENERATOR_DATASETNAME);
            } 
        }

        internal void ProcessTableRelatedNames(DesignTable table) {
            bool annotation = false; 
            bool storedNamingProperty = false;
            bool objectRenamed = !StringUtil.EqualValue(table.Name, table.UserTableName, this.languageCaseInsensitive); 
 

            string tablePropName = this.TablePropertyName(table.DataTable, out annotation); 
            string plainTablePropName = this.PlainTablePropertyName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorTablePropName = plainTablePropName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorTablePropName)) { 
                    table.GeneratorTablePropName = this.validator.GenerateIdName(tablePropName); 
                }
                else { 
                    table.GeneratorTablePropName = this.validator.GenerateIdName(table.GeneratorTablePropName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(tablePropName), table.GeneratorTablePropName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_TABLEPROPNAME); 
                    storedNamingProperty = true;
                } 
            } 

            string tableVarName = this.TableVariableName(table.DataTable, out annotation); 
            string plainTableVarName = this.PlainTableVariableName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorTableVarName = plainTableVarName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorTableVarName)) { 
                    table.GeneratorTableVarName = this.validator.GenerateIdName(tableVarName); 
                }
                else { 
                    table.GeneratorTableVarName = this.validator.GenerateIdName(table.GeneratorTableVarName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(tableVarName), table.GeneratorTableVarName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_TABLEVARNAME); 
                    storedNamingProperty = true;
                } 
            } 

            string tableClassName = this.TableClassName(table.DataTable, out annotation); 
            string plainTableClassName = this.PlainTableClassName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorTableClassName = plainTableClassName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorTableClassName)) { 
                    table.GeneratorTableClassName = this.validator.GenerateIdName(tableClassName); 
                }
                else { 
                    table.GeneratorTableClassName = this.validator.GenerateIdName(table.GeneratorTableClassName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(tableClassName), table.GeneratorTableClassName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_TABLECLASSNAME); 
                    storedNamingProperty = true;
                } 
            } 

            string rowClassName = this.RowClassName(table.DataTable, out annotation); 
            string plainRowClassName = this.PlainRowClassName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorRowClassName = plainRowClassName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorRowClassName)) { 
                    table.GeneratorRowClassName = this.validator.GenerateIdName(rowClassName); 
                }
                else { 
                    table.GeneratorRowClassName = this.validator.GenerateIdName(table.GeneratorRowClassName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowClassName), table.GeneratorRowClassName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWCLASSNAME); 
                    storedNamingProperty = true;
                } 
            } 

            string rowEventHandlerName = this.RowEventHandlerName(table.DataTable, out annotation); 
            string plainRowEventHandlerName = this.PlainRowEventHandlerName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorRowEvHandlerName = plainRowEventHandlerName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorRowEvHandlerName)) { 
                    table.GeneratorRowEvHandlerName = this.validator.GenerateIdName(rowEventHandlerName); 
                }
                else { 
                    table.GeneratorRowEvHandlerName = this.validator.GenerateIdName(table.GeneratorRowEvHandlerName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowEventHandlerName), table.GeneratorRowEvHandlerName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWEVHANDLERNAME); 
                    storedNamingProperty = true;
                } 
            } 

            string rowEventArgName = this.RowEventArgClassName(table.DataTable, out annotation); 
            string plainRowEventArgName = this.PlainRowEventArgClassName(table.DataTable, out annotation);
            if (annotation) {
                table.GeneratorRowEvArgName = plainRowEventArgName;
            } 
            else {
                if (objectRenamed || StringUtil.Empty(table.GeneratorRowEvArgName)) { 
                    table.GeneratorRowEvArgName = this.validator.GenerateIdName(rowEventArgName); 
                }
                else { 
                    table.GeneratorRowEvArgName = this.validator.GenerateIdName(table.GeneratorRowEvArgName);
                }
                if (!StringUtil.EqualValue(this.validator.GenerateIdName(rowEventArgName), table.GeneratorRowEvArgName)) {
                    table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_GENERATOR_ROWEVARGNAME); 
                    storedNamingProperty = true;
                } 
            } 

            if (storedNamingProperty) { 
                table.NamingPropertyNames.Add(DesignTable.EXTPROPNAME_USER_TABLENAME);
            }
        }
 
        internal void ProcessRelationRelatedNames(DesignRelation relation) {
            if(relation.DataRelation == null) { 
                return; 
            }
 
            bool objectRenamed = !StringUtil.EqualValue(relation.Name, relation.UserRelationName, this.languageCaseInsensitive);

            if (objectRenamed || StringUtil.Empty(relation.GeneratorRelationVarName)) {
                relation.GeneratorRelationVarName = this.validator.GenerateIdName(this.RelationVariableName(relation.DataRelation)); 
            }
            else { 
                relation.GeneratorRelationVarName = this.validator.GenerateIdName(relation.GeneratorRelationVarName); 
            }
        } 

        internal static string TablesPropertyName {
            get {
                return tablesPropertyName; 
            }
        } 
 
        internal static string RelationsPropertyName {
            get { 
                return relationsPropertyName;
            }
        }
 
        //internal static string EnforceConstraintsPropertyName {
        //    get { 
        //        return enforceConstraintsPropertyName; 
        //    }
        //} 

        // Typed table class name
        private string TableClassName(DataTable table, out bool usesAnnotations) {
            usesAnnotations = true; 
            string className = (string)table.ExtendedProperties["typedPlural"];
 
            if(StringUtil.Empty(className)) { 
                className = (string)table.ExtendedProperties["typedName"];
                if(StringUtil.Empty(className)) { 
                    usesAnnotations = false;
                    className = NameHandler.FixIdName(table.TableName);
                }
            } 

            return className + "DataTable"; 
        } 

        private string PlainTableClassName(DataTable table, out bool usesAnnotations) { 
            usesAnnotations = true;
            string className = (string)table.ExtendedProperties["typedPlural"];

            if (StringUtil.Empty(className)) { 
                className = (string)table.ExtendedProperties["typedName"];
                if (StringUtil.Empty(className)) { 
                    usesAnnotations = false; 
                    className = table.TableName;
                } 
            }

            return className + "DataTable";
        } 

        // Name of the property of typed dataset wich returns typed table: 
        private string TablePropertyName(DataTable table, out bool usesAnnotations) { 
            usesAnnotations = true;
            string typedName = (string)table.ExtendedProperties["typedPlural"]; 

            if(StringUtil.Empty(typedName)) {
                typedName = (string)table.ExtendedProperties["typedName"];
                if(StringUtil.Empty(typedName)) { 
                    usesAnnotations = false;
                    typedName = NameHandler.FixIdName(table.TableName); 
                } 
                else {
                    typedName = typedName + "Table"; 
                }
            }

            return typedName; 
        }
 
        private string PlainTablePropertyName(DataTable table, out bool usesAnnotations) { 
            usesAnnotations = true;
            string typedName = (string)table.ExtendedProperties["typedPlural"]; 

            if (StringUtil.Empty(typedName)) {
                typedName = (string)table.ExtendedProperties["typedName"];
                if (StringUtil.Empty(typedName)) { 
                    usesAnnotations = false;
                    typedName = table.TableName; 
                } 
                else {
                    typedName = typedName + "Table"; 
                }
            }

            return typedName; 
        }
 
        // Name of the variable of typed dataset wich holds typed table 
        private string TableVariableName(DataTable table, out bool usesAnnotations) {
            return "table" + TablePropertyName(table, out usesAnnotations); 
        }

        private string PlainTableVariableName(DataTable table, out bool usesAnnotations) {
            return "table" + PlainTablePropertyName(table, out usesAnnotations); 
        }
 
        // Name of a class for typed row 
        private string RowClassName(DataTable table, out bool usesAnnotations) {
            usesAnnotations = true; 
            string className = (string) table.ExtendedProperties["typedName"];

            if(StringUtil.Empty(className)) {
                usesAnnotations = false; 
                className = NameHandler.FixIdName(table.TableName) + "Row";
            } 
 
            return className;
        } 

        private string PlainRowClassName(DataTable table, out bool usesAnnotations) {
            usesAnnotations = true;
            string className = (string)table.ExtendedProperties["typedName"]; 

            if (StringUtil.Empty(className)) { 
                usesAnnotations = false; 
                className = table.TableName + "Row";
            } 

            return className;
        }
 
        // Name of row event arg class
        private string RowEventArgClassName(DataTable table, out bool usesAnnotations) { 
            return RowClassName(table, out usesAnnotations) + "ChangeEvent"; 
        }
 
        private string PlainRowEventArgClassName(DataTable table, out bool usesAnnotations) {
            return PlainRowClassName(table, out usesAnnotations) + "ChangeEvent";
        }
 
        // Name of row event handler
        private string RowEventHandlerName(DataTable table, out bool usesAnnotations) { 
            return RowClassName(table, out usesAnnotations) + "ChangeEventHandler"; 
        }
 
        private string PlainRowEventHandlerName(DataTable table, out bool usesAnnotations) {
            return PlainRowClassName(table, out usesAnnotations) + "ChangeEventHandler";
        }
 
        // Name of private variable for relation
        private string RelationVariableName(DataRelation relation) { 
            return NameHandler.FixIdName("relation" + relation.RelationName); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
