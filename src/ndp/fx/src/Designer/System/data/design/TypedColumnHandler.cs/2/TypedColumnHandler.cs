//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.CodeDom;
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 
    using System.Design;
    using System.Reflection; 
 
    internal sealed class TypedColumnHandler {
        private TypedDataSourceCodeGenerator codeGenerator = null; 
        private DataTable table = null;
        private DesignTable designTable = null;
        private DesignColumnCollection columns = null;
 
        internal TypedColumnHandler(DesignTable designTable, TypedDataSourceCodeGenerator codeGenerator) {
            this.codeGenerator = codeGenerator; 
            this.table = designTable.DataTable; 
            this.designTable = designTable;
            this.columns = designTable.DesignColumns; 

        }

 
        internal void AddPrivateVariables(CodeTypeDeclaration dataTableClass) {
            if(dataTableClass == null) { 
                throw new InternalException("Table CodeTypeDeclaration should not be null."); 
            }
            if( columns == null ) { 
                return;
            }

            foreach(DesignColumn column in columns) { 
                //\\ private DataColumn <TableColumnVariableName>;
                dataTableClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), column.GeneratorColumnVarNameInTable)); 
            } 
        }
 
        internal void AddTableColumnProperties(CodeTypeDeclaration dataTableClass) {
            if( columns == null ) {
                return;
            } 

            foreach(DesignColumn column in columns) { 
                //\\ public DataColumn <ColumnPropertyName> { 
                //\\     get { return this.<ColumnVariableName>; }
                //\\ } 
                CodeMemberProperty colProp = CodeGenHelper.PropertyDecl(
                    CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)),
                    column.GeneratorColumnPropNameInTable,
                    MemberAttributes.Public | MemberAttributes.Final 
                );
                colProp.GetStatements.Add( 
                    CodeGenHelper.Return( 
                CodeGenHelper.Field(CodeGenHelper.This(), column.GeneratorColumnVarNameInTable)
                    ) 
                );

                dataTableClass.Members.Add(colProp);
            } 
        }
 
        internal void AddRowColumnProperties(CodeTypeDeclaration rowClass) { 
            bool storageInitialized = false;
            string rowClassName = codeGenerator.TableHandler.Tables[table.TableName].GeneratorRowClassName; 
            string tableFieldName = codeGenerator.TableHandler.Tables[table.TableName].GeneratorTableVarName;

            foreach(DesignColumn designColumn in columns) {
                DataColumn column = designColumn.DataColumn; 
                Type dataType = column.DataType;
                string rowColumnName   = designColumn.GeneratorColumnPropNameInRow; 
                string tableColumnName = designColumn.GeneratorColumnPropNameInTable; 
                GenericNameHandler propertyScopeNameHandler = new GenericNameHandler(new string[] { rowColumnName }, codeGenerator.CodeProvider);
 
                //\\ public <ColumnType> <ColumnName> {
                //\\     get {
                //\\         try{
                //\\             return ((<ColumnType>)(this[this.table<TableName>.<ColumnName>Column])); 
                //\\         }catch(InvalidCastException e) {
                //\\             throw new StrongTypingException("StrongTyping_CananotAccessDBNull", e); 
                //\\         } 
                //\\     }
                //\\or 
                //\\     get {
                //\\         if(Is<ColumnName>Null()){
                //\\             return (<nullValue>);
                //\\         }else { 
                //\\             return ((<ColumnType>)(this[this.table<TableName>.<ColumnName>Column]));
                //\\         } 
                //\\     } 
                //\\or
                //\\     get { 
                //\\         if(Is<ColumnName>Null()){
                //\\             return <ColumnName>_nullValue;
                //\\         }else {
                //\\             return ((<ColumnType>)(this[this.table<TableName>.<ColumnName>Column])); 
                //\\         }
                //\\     } 
                //\\ 
                //\\     set {this[this.table<TableName>.<ColumnName>Column] = value;}
                //\\ } 
                //\\
                //\\if required:
                //\\ private static <ColumnType> <ColumnName>_nullValue = ...;
                CodeMemberProperty rowProp = CodeGenHelper.PropertyDecl( 
                    CodeGenHelper.Type(dataType),
                    rowColumnName, 
                    MemberAttributes.Public | MemberAttributes.Final 
                );
                CodeStatement getStmnt = CodeGenHelper.Return( 
                    CodeGenHelper.Cast(
                        CodeGenHelper.GlobalType(dataType),
                        CodeGenHelper.Indexer(
                            CodeGenHelper.This(), 
                            CodeGenHelper.Property(
                                CodeGenHelper.Field(CodeGenHelper.This(), tableFieldName), 
                                tableColumnName 
                            )
                        ) 
                    )
                );

                if(column.AllowDBNull) { 
                    string nullValue = (string) column.ExtendedProperties["nullValue"];
                    if(nullValue == null || nullValue == "_throw") { 
                        getStmnt = CodeGenHelper.Try( 
                            getStmnt,
                            CodeGenHelper.Catch( 
                                CodeGenHelper.GlobalType(typeof(System.InvalidCastException)),
                                propertyScopeNameHandler.AddNameToList("e"),
                                CodeGenHelper.Throw(
                                    CodeGenHelper.GlobalType(typeof(System.Data.StrongTypingException)), 
                                    SR.GetString(SR.CG_ColumnIsDBNull, column.ColumnName, table.TableName),
                                    propertyScopeNameHandler.GetNameFromList("e") 
                                ) 
                            )
                        ); 
                    }else {
                        CodeExpression nullValueFieldInit = null; // in some cases we generate it
                        CodeExpression nullValueExpr;
                        if(nullValue == "_null") { 
                            if(column.DataType.IsSubclassOf(typeof(System.ValueType))) {
                                codeGenerator.ProblemList.Add( new DSGeneratorProblem(SR.GetString(SR.CG_TypeCantBeNull, column.ColumnName, column.DataType.Name), ProblemSeverity.NonFatalError, designColumn) ); 
                                continue; // with next column. 
                            }
                            nullValueExpr = CodeGenHelper.Primitive(null); 
                        }
                        else if (nullValue == "_empty") {
                            if (column.DataType == typeof(string)) {
                                nullValueExpr = CodeGenHelper.Property(CodeGenHelper.TypeExpr(CodeGenHelper.GlobalType(column.DataType)), "Empty"); 
                            }
                            else { 
                                nullValueExpr = CodeGenHelper.Field( 
                                    CodeGenHelper.TypeExpr(CodeGenHelper.Type(rowClassName)),
                                    rowColumnName + "_nullValue" 
                                );
                                //\\ private static <ColumnType> <ColumnName>_nullValue = new <ColumnType>();
                                /* check that object can be constructed with parameterless constructor */
                                ConstructorInfo constructor = column.DataType.GetConstructor(new Type[] { typeof(string) }); 
                                if (constructor == null) {
                                    codeGenerator.ProblemList.Add(new DSGeneratorProblem(SR.GetString(SR.CG_NoCtor0, column.ColumnName, column.DataType.Name), ProblemSeverity.NonFatalError, designColumn)); 
                                    continue; // with next column. 
                                }
                                constructor.Invoke(new Object[] { }); // can throw here. 

                                nullValueFieldInit = CodeGenHelper.New(CodeGenHelper.Type(column.DataType), new CodeExpression[] { });
                            }
                        } 
                        else {
                            if (!storageInitialized) { 
                                table.NewRow(); // by this we force DataTable create DataStorage for each column in a table. 
                                storageInitialized = true;
                            } 
                            object nullValueObj = codeGenerator.RowHandler.RowGenerator.ConvertXmlToObject.Invoke(column, new object[] { nullValue }); // an exception will be thrown if nullValue can't be conwerted to col.DataType

                            DSGeneratorProblem problem = CodeGenHelper.GenerateValueExprAndFieldInit(designColumn, nullValueObj, nullValue, rowClassName, rowColumnName + "_nullValue", out nullValueExpr, out nullValueFieldInit);
                            if (problem != null) { 
                                codeGenerator.ProblemList.Add(problem);
                                continue; // with next column 
                            } 
                        }
                        getStmnt = CodeGenHelper.If( 
                            CodeGenHelper.MethodCall(CodeGenHelper.This(), "Is" + rowColumnName + "Null"),
                            new CodeStatement[] {CodeGenHelper.Return(nullValueExpr)},
                            new CodeStatement[] {getStmnt}
                        ); 
                        if(nullValueFieldInit != null) {
                            CodeMemberField nullValueField = CodeGenHelper.FieldDecl( 
                                CodeGenHelper.Type(column.DataType.FullName), 
                                rowColumnName + "_nullValue"
                            ); 
                            nullValueField.Attributes     = MemberAttributes.Static | MemberAttributes.Private;
                            nullValueField.InitExpression = nullValueFieldInit;

                            rowClass.Members.Add(nullValueField); 
                        }
                    } 
                } 

                rowProp.GetStatements.Add(getStmnt); 

                rowProp.SetStatements.Add(
                    CodeGenHelper.Assign(
                        CodeGenHelper.Indexer( 
                            CodeGenHelper.This(),
                            CodeGenHelper.Property( 
                                CodeGenHelper.Field(CodeGenHelper.This(), tableFieldName), 
                                tableColumnName
                            ) 
                        ),
                        CodeGenHelper.Value()
                    )
                ); 

                rowClass.Members.Add(rowProp); 
 
                if (column.AllowDBNull) {
                    //\\ public bool Is<ColumnName>Null() { 
                    //\\     return this.IsNull(this.table<TableName>.<ColumnName>Column);
                    //\\ }
                    string candidateName = "Is" + rowColumnName + "Null";
                    string validatedName = MemberNameValidator.GenerateIdName(candidateName, this.codeGenerator.CodeProvider, false /*useSuffix*/); 
                    CodeMemberMethod isNull = CodeGenHelper.MethodDecl(
                        CodeGenHelper.GlobalType(typeof(System.Boolean)), 
                        validatedName, 
                        MemberAttributes.Public | MemberAttributes.Final
                    ); 
                    isNull.Statements.Add(
                        CodeGenHelper.Return(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.This(), 
                                "IsNull",
                                CodeGenHelper.Property( 
                                    CodeGenHelper.Field(CodeGenHelper.This(), tableFieldName), 
                                    tableColumnName
                                ) 
                            )
                        )
                    );
 
                    rowClass.Members.Add(isNull);
 
                    //\\ public void Set<ColumnName>Null() { 
                    //\\     this[this.table<TableName>.<ColumnName>Column] = DBNull.Value;
                    //\\ } 
                    candidateName = "Set" + rowColumnName + "Null";
                    validatedName = MemberNameValidator.GenerateIdName(candidateName, this.codeGenerator.CodeProvider, false /*useSuffix*/);
                    CodeMemberMethod setNull =
                        CodeGenHelper.MethodDecl( 
                            CodeGenHelper.GlobalType(typeof(void)),
                            validatedName, 
                            MemberAttributes.Public | MemberAttributes.Final 
                        );
                    setNull.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Indexer(
                                CodeGenHelper.This(),
                                CodeGenHelper.Property( 
                                    CodeGenHelper.Field(CodeGenHelper.This(), tableFieldName),
                                    tableColumnName 
                                ) 
                            ),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(Convert)), "DBNull") 
                        )
                    );

                    rowClass.Members.Add(setNull); 
                }
            } 
        } 

        internal void AddRowGetRelatedRowsMethods(CodeTypeDeclaration rowClass) { 
            DataRelationCollection childRelations = table.ChildRelations;

            for (int i = 0; i < childRelations.Count; i++) {
                //\\ public <rowConcreteClassName>[] Get<ChildTableName>Rows() { 
                //\\     return (<rowConcrateClassName>[]) base.GetChildRows(this.Table.ChildRelations["<RelationName>"]);
                //\\  } 
                DataRelation relation = childRelations[i]; 
                string rowConcreteClassName = codeGenerator.TableHandler.Tables[relation.ChildTable.TableName].GeneratorRowClassName;
 
                CodeMemberMethod childArray = CodeGenHelper.MethodDecl(
                    CodeGenHelper.Type(rowConcreteClassName, 1),
                codeGenerator.RelationHandler.Relations[relation.RelationName].GeneratorChildPropName,
                    MemberAttributes.Public | MemberAttributes.Final 
                );
 
                childArray.Statements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdEQ( 
                            CodeGenHelper.Indexer(
                                CodeGenHelper.Property(
                                    CodeGenHelper.Property(CodeGenHelper.This(), "Table"),
                                    "ChildRelations" 
                                    ),
                                CodeGenHelper.Str(relation.RelationName) 
                            ), 
                            CodeGenHelper.Primitive(null)
                        ), 
                        CodeGenHelper.Return(
                            new CodeArrayCreateExpression(rowConcreteClassName, 0)
                        ),
                        CodeGenHelper.Return( 
                            CodeGenHelper.Cast(
                                CodeGenHelper.Type(rowConcreteClassName, 1), 
                                CodeGenHelper.MethodCall( 
                                    CodeGenHelper.Base(),
                                    "GetChildRows", 
                                    CodeGenHelper.Indexer(
                                        CodeGenHelper.Property(
                                            CodeGenHelper.Property(CodeGenHelper.This(), "Table"),
                                            "ChildRelations" 
                                            ),
                                        CodeGenHelper.Str(relation.RelationName) 
                                    ) 
                                )
                            ) 
                        )
                    )
                );
 
                rowClass.Members.Add(childArray);
            } 
 
            DataRelationCollection parentRelations = table.ParentRelations;
            for (int i = 0; i < parentRelations.Count; i++) { 
                //\\ public <ParentRowClassName> <ParentRowClassName>Parent {
                //\\     get {
                //\\         return ((<ParentRowClassName>)(this.GetParentRow(this.Table.ParentRelations["<RelationName>"])));
                //\\     } 
                //\\     set {
                //\\         this.SetParentRow(value, this.Table.ParentRelations["<RelationName>"]); 
                //\\     } 
                //\\ }
                DataRelation relation = parentRelations[i]; 
                string parentTypedRowName = codeGenerator.TableHandler.Tables[relation.ParentTable.TableName].GeneratorRowClassName;

                CodeMemberProperty parentTableProp = CodeGenHelper.PropertyDecl(
                    CodeGenHelper.Type(parentTypedRowName), 
                    codeGenerator.RelationHandler.Relations[relation.RelationName].GeneratorParentPropName,
                    MemberAttributes.Public | MemberAttributes.Final 
                ); 
                parentTableProp.GetStatements.Add(
                    CodeGenHelper.Return( 
                        CodeGenHelper.Cast(
                            CodeGenHelper.Type(parentTypedRowName),
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.This(), 
                                "GetParentRow",
                                CodeGenHelper.Indexer( 
                                    CodeGenHelper.Property( 
                                        CodeGenHelper.Property(CodeGenHelper.This(), "Table"),
                                        "ParentRelations" 
                                    ),
                                    CodeGenHelper.Str(relation.RelationName)
                                )
                            ) 
                        )
                    ) 
                ); 
                parentTableProp.SetStatements.Add(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.This(),
                        "SetParentRow",
                        new CodeExpression[] {
                            CodeGenHelper.Value(), 
                            CodeGenHelper.Indexer(
                                CodeGenHelper.Property( 
                                    CodeGenHelper.Property(CodeGenHelper.This(), "Table"), 
                                    "ParentRelations"
                                ), 
                                CodeGenHelper.Str(relation.RelationName)
                            )
                        }
                    ) 
                );
 
                rowClass.Members.Add(parentTableProp); 
            }
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
    using System.CodeDom;
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 
    using System.Design;
    using System.Reflection; 
 
    internal sealed class TypedColumnHandler {
        private TypedDataSourceCodeGenerator codeGenerator = null; 
        private DataTable table = null;
        private DesignTable designTable = null;
        private DesignColumnCollection columns = null;
 
        internal TypedColumnHandler(DesignTable designTable, TypedDataSourceCodeGenerator codeGenerator) {
            this.codeGenerator = codeGenerator; 
            this.table = designTable.DataTable; 
            this.designTable = designTable;
            this.columns = designTable.DesignColumns; 

        }

 
        internal void AddPrivateVariables(CodeTypeDeclaration dataTableClass) {
            if(dataTableClass == null) { 
                throw new InternalException("Table CodeTypeDeclaration should not be null."); 
            }
            if( columns == null ) { 
                return;
            }

            foreach(DesignColumn column in columns) { 
                //\\ private DataColumn <TableColumnVariableName>;
                dataTableClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), column.GeneratorColumnVarNameInTable)); 
            } 
        }
 
        internal void AddTableColumnProperties(CodeTypeDeclaration dataTableClass) {
            if( columns == null ) {
                return;
            } 

            foreach(DesignColumn column in columns) { 
                //\\ public DataColumn <ColumnPropertyName> { 
                //\\     get { return this.<ColumnVariableName>; }
                //\\ } 
                CodeMemberProperty colProp = CodeGenHelper.PropertyDecl(
                    CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)),
                    column.GeneratorColumnPropNameInTable,
                    MemberAttributes.Public | MemberAttributes.Final 
                );
                colProp.GetStatements.Add( 
                    CodeGenHelper.Return( 
                CodeGenHelper.Field(CodeGenHelper.This(), column.GeneratorColumnVarNameInTable)
                    ) 
                );

                dataTableClass.Members.Add(colProp);
            } 
        }
 
        internal void AddRowColumnProperties(CodeTypeDeclaration rowClass) { 
            bool storageInitialized = false;
            string rowClassName = codeGenerator.TableHandler.Tables[table.TableName].GeneratorRowClassName; 
            string tableFieldName = codeGenerator.TableHandler.Tables[table.TableName].GeneratorTableVarName;

            foreach(DesignColumn designColumn in columns) {
                DataColumn column = designColumn.DataColumn; 
                Type dataType = column.DataType;
                string rowColumnName   = designColumn.GeneratorColumnPropNameInRow; 
                string tableColumnName = designColumn.GeneratorColumnPropNameInTable; 
                GenericNameHandler propertyScopeNameHandler = new GenericNameHandler(new string[] { rowColumnName }, codeGenerator.CodeProvider);
 
                //\\ public <ColumnType> <ColumnName> {
                //\\     get {
                //\\         try{
                //\\             return ((<ColumnType>)(this[this.table<TableName>.<ColumnName>Column])); 
                //\\         }catch(InvalidCastException e) {
                //\\             throw new StrongTypingException("StrongTyping_CananotAccessDBNull", e); 
                //\\         } 
                //\\     }
                //\\or 
                //\\     get {
                //\\         if(Is<ColumnName>Null()){
                //\\             return (<nullValue>);
                //\\         }else { 
                //\\             return ((<ColumnType>)(this[this.table<TableName>.<ColumnName>Column]));
                //\\         } 
                //\\     } 
                //\\or
                //\\     get { 
                //\\         if(Is<ColumnName>Null()){
                //\\             return <ColumnName>_nullValue;
                //\\         }else {
                //\\             return ((<ColumnType>)(this[this.table<TableName>.<ColumnName>Column])); 
                //\\         }
                //\\     } 
                //\\ 
                //\\     set {this[this.table<TableName>.<ColumnName>Column] = value;}
                //\\ } 
                //\\
                //\\if required:
                //\\ private static <ColumnType> <ColumnName>_nullValue = ...;
                CodeMemberProperty rowProp = CodeGenHelper.PropertyDecl( 
                    CodeGenHelper.Type(dataType),
                    rowColumnName, 
                    MemberAttributes.Public | MemberAttributes.Final 
                );
                CodeStatement getStmnt = CodeGenHelper.Return( 
                    CodeGenHelper.Cast(
                        CodeGenHelper.GlobalType(dataType),
                        CodeGenHelper.Indexer(
                            CodeGenHelper.This(), 
                            CodeGenHelper.Property(
                                CodeGenHelper.Field(CodeGenHelper.This(), tableFieldName), 
                                tableColumnName 
                            )
                        ) 
                    )
                );

                if(column.AllowDBNull) { 
                    string nullValue = (string) column.ExtendedProperties["nullValue"];
                    if(nullValue == null || nullValue == "_throw") { 
                        getStmnt = CodeGenHelper.Try( 
                            getStmnt,
                            CodeGenHelper.Catch( 
                                CodeGenHelper.GlobalType(typeof(System.InvalidCastException)),
                                propertyScopeNameHandler.AddNameToList("e"),
                                CodeGenHelper.Throw(
                                    CodeGenHelper.GlobalType(typeof(System.Data.StrongTypingException)), 
                                    SR.GetString(SR.CG_ColumnIsDBNull, column.ColumnName, table.TableName),
                                    propertyScopeNameHandler.GetNameFromList("e") 
                                ) 
                            )
                        ); 
                    }else {
                        CodeExpression nullValueFieldInit = null; // in some cases we generate it
                        CodeExpression nullValueExpr;
                        if(nullValue == "_null") { 
                            if(column.DataType.IsSubclassOf(typeof(System.ValueType))) {
                                codeGenerator.ProblemList.Add( new DSGeneratorProblem(SR.GetString(SR.CG_TypeCantBeNull, column.ColumnName, column.DataType.Name), ProblemSeverity.NonFatalError, designColumn) ); 
                                continue; // with next column. 
                            }
                            nullValueExpr = CodeGenHelper.Primitive(null); 
                        }
                        else if (nullValue == "_empty") {
                            if (column.DataType == typeof(string)) {
                                nullValueExpr = CodeGenHelper.Property(CodeGenHelper.TypeExpr(CodeGenHelper.GlobalType(column.DataType)), "Empty"); 
                            }
                            else { 
                                nullValueExpr = CodeGenHelper.Field( 
                                    CodeGenHelper.TypeExpr(CodeGenHelper.Type(rowClassName)),
                                    rowColumnName + "_nullValue" 
                                );
                                //\\ private static <ColumnType> <ColumnName>_nullValue = new <ColumnType>();
                                /* check that object can be constructed with parameterless constructor */
                                ConstructorInfo constructor = column.DataType.GetConstructor(new Type[] { typeof(string) }); 
                                if (constructor == null) {
                                    codeGenerator.ProblemList.Add(new DSGeneratorProblem(SR.GetString(SR.CG_NoCtor0, column.ColumnName, column.DataType.Name), ProblemSeverity.NonFatalError, designColumn)); 
                                    continue; // with next column. 
                                }
                                constructor.Invoke(new Object[] { }); // can throw here. 

                                nullValueFieldInit = CodeGenHelper.New(CodeGenHelper.Type(column.DataType), new CodeExpression[] { });
                            }
                        } 
                        else {
                            if (!storageInitialized) { 
                                table.NewRow(); // by this we force DataTable create DataStorage for each column in a table. 
                                storageInitialized = true;
                            } 
                            object nullValueObj = codeGenerator.RowHandler.RowGenerator.ConvertXmlToObject.Invoke(column, new object[] { nullValue }); // an exception will be thrown if nullValue can't be conwerted to col.DataType

                            DSGeneratorProblem problem = CodeGenHelper.GenerateValueExprAndFieldInit(designColumn, nullValueObj, nullValue, rowClassName, rowColumnName + "_nullValue", out nullValueExpr, out nullValueFieldInit);
                            if (problem != null) { 
                                codeGenerator.ProblemList.Add(problem);
                                continue; // with next column 
                            } 
                        }
                        getStmnt = CodeGenHelper.If( 
                            CodeGenHelper.MethodCall(CodeGenHelper.This(), "Is" + rowColumnName + "Null"),
                            new CodeStatement[] {CodeGenHelper.Return(nullValueExpr)},
                            new CodeStatement[] {getStmnt}
                        ); 
                        if(nullValueFieldInit != null) {
                            CodeMemberField nullValueField = CodeGenHelper.FieldDecl( 
                                CodeGenHelper.Type(column.DataType.FullName), 
                                rowColumnName + "_nullValue"
                            ); 
                            nullValueField.Attributes     = MemberAttributes.Static | MemberAttributes.Private;
                            nullValueField.InitExpression = nullValueFieldInit;

                            rowClass.Members.Add(nullValueField); 
                        }
                    } 
                } 

                rowProp.GetStatements.Add(getStmnt); 

                rowProp.SetStatements.Add(
                    CodeGenHelper.Assign(
                        CodeGenHelper.Indexer( 
                            CodeGenHelper.This(),
                            CodeGenHelper.Property( 
                                CodeGenHelper.Field(CodeGenHelper.This(), tableFieldName), 
                                tableColumnName
                            ) 
                        ),
                        CodeGenHelper.Value()
                    )
                ); 

                rowClass.Members.Add(rowProp); 
 
                if (column.AllowDBNull) {
                    //\\ public bool Is<ColumnName>Null() { 
                    //\\     return this.IsNull(this.table<TableName>.<ColumnName>Column);
                    //\\ }
                    string candidateName = "Is" + rowColumnName + "Null";
                    string validatedName = MemberNameValidator.GenerateIdName(candidateName, this.codeGenerator.CodeProvider, false /*useSuffix*/); 
                    CodeMemberMethod isNull = CodeGenHelper.MethodDecl(
                        CodeGenHelper.GlobalType(typeof(System.Boolean)), 
                        validatedName, 
                        MemberAttributes.Public | MemberAttributes.Final
                    ); 
                    isNull.Statements.Add(
                        CodeGenHelper.Return(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.This(), 
                                "IsNull",
                                CodeGenHelper.Property( 
                                    CodeGenHelper.Field(CodeGenHelper.This(), tableFieldName), 
                                    tableColumnName
                                ) 
                            )
                        )
                    );
 
                    rowClass.Members.Add(isNull);
 
                    //\\ public void Set<ColumnName>Null() { 
                    //\\     this[this.table<TableName>.<ColumnName>Column] = DBNull.Value;
                    //\\ } 
                    candidateName = "Set" + rowColumnName + "Null";
                    validatedName = MemberNameValidator.GenerateIdName(candidateName, this.codeGenerator.CodeProvider, false /*useSuffix*/);
                    CodeMemberMethod setNull =
                        CodeGenHelper.MethodDecl( 
                            CodeGenHelper.GlobalType(typeof(void)),
                            validatedName, 
                            MemberAttributes.Public | MemberAttributes.Final 
                        );
                    setNull.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Indexer(
                                CodeGenHelper.This(),
                                CodeGenHelper.Property( 
                                    CodeGenHelper.Field(CodeGenHelper.This(), tableFieldName),
                                    tableColumnName 
                                ) 
                            ),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(Convert)), "DBNull") 
                        )
                    );

                    rowClass.Members.Add(setNull); 
                }
            } 
        } 

        internal void AddRowGetRelatedRowsMethods(CodeTypeDeclaration rowClass) { 
            DataRelationCollection childRelations = table.ChildRelations;

            for (int i = 0; i < childRelations.Count; i++) {
                //\\ public <rowConcreteClassName>[] Get<ChildTableName>Rows() { 
                //\\     return (<rowConcrateClassName>[]) base.GetChildRows(this.Table.ChildRelations["<RelationName>"]);
                //\\  } 
                DataRelation relation = childRelations[i]; 
                string rowConcreteClassName = codeGenerator.TableHandler.Tables[relation.ChildTable.TableName].GeneratorRowClassName;
 
                CodeMemberMethod childArray = CodeGenHelper.MethodDecl(
                    CodeGenHelper.Type(rowConcreteClassName, 1),
                codeGenerator.RelationHandler.Relations[relation.RelationName].GeneratorChildPropName,
                    MemberAttributes.Public | MemberAttributes.Final 
                );
 
                childArray.Statements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdEQ( 
                            CodeGenHelper.Indexer(
                                CodeGenHelper.Property(
                                    CodeGenHelper.Property(CodeGenHelper.This(), "Table"),
                                    "ChildRelations" 
                                    ),
                                CodeGenHelper.Str(relation.RelationName) 
                            ), 
                            CodeGenHelper.Primitive(null)
                        ), 
                        CodeGenHelper.Return(
                            new CodeArrayCreateExpression(rowConcreteClassName, 0)
                        ),
                        CodeGenHelper.Return( 
                            CodeGenHelper.Cast(
                                CodeGenHelper.Type(rowConcreteClassName, 1), 
                                CodeGenHelper.MethodCall( 
                                    CodeGenHelper.Base(),
                                    "GetChildRows", 
                                    CodeGenHelper.Indexer(
                                        CodeGenHelper.Property(
                                            CodeGenHelper.Property(CodeGenHelper.This(), "Table"),
                                            "ChildRelations" 
                                            ),
                                        CodeGenHelper.Str(relation.RelationName) 
                                    ) 
                                )
                            ) 
                        )
                    )
                );
 
                rowClass.Members.Add(childArray);
            } 
 
            DataRelationCollection parentRelations = table.ParentRelations;
            for (int i = 0; i < parentRelations.Count; i++) { 
                //\\ public <ParentRowClassName> <ParentRowClassName>Parent {
                //\\     get {
                //\\         return ((<ParentRowClassName>)(this.GetParentRow(this.Table.ParentRelations["<RelationName>"])));
                //\\     } 
                //\\     set {
                //\\         this.SetParentRow(value, this.Table.ParentRelations["<RelationName>"]); 
                //\\     } 
                //\\ }
                DataRelation relation = parentRelations[i]; 
                string parentTypedRowName = codeGenerator.TableHandler.Tables[relation.ParentTable.TableName].GeneratorRowClassName;

                CodeMemberProperty parentTableProp = CodeGenHelper.PropertyDecl(
                    CodeGenHelper.Type(parentTypedRowName), 
                    codeGenerator.RelationHandler.Relations[relation.RelationName].GeneratorParentPropName,
                    MemberAttributes.Public | MemberAttributes.Final 
                ); 
                parentTableProp.GetStatements.Add(
                    CodeGenHelper.Return( 
                        CodeGenHelper.Cast(
                            CodeGenHelper.Type(parentTypedRowName),
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.This(), 
                                "GetParentRow",
                                CodeGenHelper.Indexer( 
                                    CodeGenHelper.Property( 
                                        CodeGenHelper.Property(CodeGenHelper.This(), "Table"),
                                        "ParentRelations" 
                                    ),
                                    CodeGenHelper.Str(relation.RelationName)
                                )
                            ) 
                        )
                    ) 
                ); 
                parentTableProp.SetStatements.Add(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.This(),
                        "SetParentRow",
                        new CodeExpression[] {
                            CodeGenHelper.Value(), 
                            CodeGenHelper.Indexer(
                                CodeGenHelper.Property( 
                                    CodeGenHelper.Property(CodeGenHelper.This(), "Table"), 
                                    "ParentRelations"
                                ), 
                                CodeGenHelper.Str(relation.RelationName)
                            )
                        }
                    ) 
                );
 
                rowClass.Members.Add(parentTableProp); 
            }
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
