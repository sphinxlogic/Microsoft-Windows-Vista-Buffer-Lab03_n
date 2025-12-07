//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.ComponentModel; 
    using System.Data;
    using System.Data.Common; 
    using System.Data.SqlClient; 
    using System.Globalization;
    using System.Xml; 
    using System.Xml.Schema;
    using System.Xml.Serialization;

    using GenerateOption = TypedDataSetGenerator.GenerateOption; 

    internal sealed class TableMethodGenerator { 
        private TypedDataSourceCodeGenerator codeGenerator = null; 
        private DesignTable designTable = null;
        private string rowClassName = null; 
        private string rowConcreteClassName = null;
        private string tableClassName = null;
        private CodeMemberMethod initExpressionsMethod = null;
 
        private static PropertyDescriptor namespaceProperty = TypeDescriptor.GetProperties(typeof(DataTable))["Namespace"];
        private static PropertyDescriptor localeProperty = TypeDescriptor.GetProperties(typeof(DataTable))["Locale"]; 
        private static PropertyDescriptor caseSensitiveProperty = TypeDescriptor.GetProperties(typeof(DataTable))["CaseSensitive"]; 
        private static PropertyDescriptor columnNamespaceProperty = TypeDescriptor.GetProperties(typeof(DataColumn))["Namespace"];
        private static PropertyDescriptor dateTimeModeProperty = TypeDescriptor.GetProperties(typeof(DataColumn))["DateTimeMode"]; 

        private static string columnValuesArrayName = "columnValuesArray";

        internal TableMethodGenerator(TypedDataSourceCodeGenerator codeGenerator, DesignTable designTable) { 
            this.codeGenerator = codeGenerator;
            this.designTable = designTable; 
        } 

        internal void AddMethods(CodeTypeDeclaration dataTableClass) { 
            if(dataTableClass == null) {
                throw new InternalException("Table CodeTypeDeclaration should not be null.");
            }
 
            // Get necessary identifier names
            this.rowClassName = designTable.GeneratorRowClassName; 
            this.rowConcreteClassName = designTable.GeneratorRowClassName; 
            this.tableClassName = designTable.GeneratorTableClassName;
 
            // Add methods to the class
            initExpressionsMethod = InitExpressionsMethod();
            if (initExpressionsMethod != null) {
                dataTableClass.Members.Add(ArgumentLessConstructorInitExpressions()); 
                dataTableClass.Members.Add(ConstructorWithBoolArgument());
            } 
            else { 
                dataTableClass.Members.Add(ArgumentLessConstructorNoInitExpressions());
            } 
            dataTableClass.Members.Add( ConstructorWithArguments() );
            dataTableClass.Members.Add( DeserializingConstructor() );
            dataTableClass.Members.Add(AddTypedRowMethod());
            AddTypedRowByColumnsMethods(dataTableClass); 
            AddFindByMethods( dataTableClass );
 
            // TypedTableBase handles our IEnumerator implementation, so we don't have to do it here 
            if ((this.codeGenerator.GenerateOptions & GenerateOption.LinqOverTypedDatasets) != GenerateOption.LinqOverTypedDatasets) {
                dataTableClass.Members.Add(GetEnumeratorMethod()); 
            }

            dataTableClass.Members.Add( CloneMethod() );
            dataTableClass.Members.Add( CreateInstanceMethod() ); 
            CodeMemberMethod initClassMethod = null;
            CodeMemberMethod initVarsMethod = null; 
            InitClassAndInitVarsMethods(dataTableClass, out initClassMethod, out initVarsMethod); 
            dataTableClass.Members.Add( initVarsMethod );
            dataTableClass.Members.Add( initClassMethod ); 
            dataTableClass.Members.Add( NewTypedRowMethod() );
            dataTableClass.Members.Add( NewRowFromBuilderMethod() );
            dataTableClass.Members.Add( GetRowTypeMethod() );
            if( initExpressionsMethod != null ) { 
                dataTableClass.Members.Add( initExpressionsMethod );
            } 
            if (this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareEvents) && this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareDelegates)) { 
                AddOnRowEventMethods(dataTableClass);
            } 
            dataTableClass.Members.Add( RemoveRowMethod() );
            dataTableClass.Members.Add( GetTypedTableSchema() );
        }
 
        private CodeConstructor ArgumentLessConstructorInitExpressions() {
            //\\ public <TableClassName>() : this(false){} 
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Public | MemberAttributes.Final); 
            // by default we don't Init expressions when a standalone typed table is created. This behavior is consistent with
            // Everett and avoids failures if some expression have references to related tables (see VSWhidbey #517338) 
            constructor.ChainedConstructorArgs.Add(CodeGenHelper.Primitive(false));

            return constructor;
        } 

        private CodeConstructor ConstructorWithBoolArgument() { 
            //\\ public <TableClassName>(bool initExpressions) : base() { 
            //\\    this.TableName = <TableName>
            //\\    this.BeginInit(); 
            //\\    this.InitClass();
            //\\    if(initExpressions) {
            //\\        this.InitExpressions();
            //\\    } 
            //\\    this.EndInit();
            //\\ } 
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Assembly | MemberAttributes.Final); 
            // DevDiv Bug 6208, considering NT-ier scenario, make the method public
            constructor.Attributes = MemberAttributes.Public | MemberAttributes.Final; 
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(bool)), "initExpressions"));

            //\\ this.TableName = <TableName>;
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "TableName"), 
                    CodeGenHelper.Str(designTable.Name) 
                )
            ); 

            //\\ this.BeginInit();
            //\\ this.InitClass();
            //\\ if(initExpressions) { 
            //\\    this.InitExpressions();
            //\\ } 
            //\\ this.EndInit(); 
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "BeginInit"));
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitClass")); 
            constructor.Statements.Add(
                CodeGenHelper.If(
                    CodeGenHelper.EQ(
                        CodeGenHelper.Argument("initExpressions"), 
                        CodeGenHelper.Primitive(true)
                    ), 
                    new CodeStatement[] { CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitExpressions")) } 
                )
            ); 
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "EndInit"));

            return constructor;
        } 

        private CodeConstructor ArgumentLessConstructorNoInitExpressions() { 
            //\\ public <TableClassName>() : base() { 
            //\\    this.TableName = <TableName>
            //\\    this.BeginInit(); 
            //\\    this.InitClass();
            //\\    this.EndInit();
            //\\ }
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Public | MemberAttributes.Final); 

            //\\ this.TableName = <TableName>; 
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "TableName"), 
                    CodeGenHelper.Str(designTable.Name)
                )
            );
 
            //\\ this.BeginInit();
            //\\ this.InitClass(); 
            //\\ this.EndInit(); 
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "BeginInit"));
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitClass")); 
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "EndInit"));

            return constructor;
        } 

        private CodeConstructor ConstructorWithArguments() { 
            //\\ internal <TableClassName>(DataTable table) : base() { // Assuming incoming table always associated with DataSet 
            //\\ this.TableName = table.TableName
            //\\ if (table.CaseSensitive != table.DataSet.CaseSensitive) 
            //\\    this.CaseSensitive = table.CaseSensitive;
            //\\ if (table.Locale.ToString() != table.DataSet.Locale.ToString())
            //\\    this.Locale = table.Locale;
            //\\ if (table.Namespace != table.DataSet.Namespace) 
            //\\    this.Namespace = table.Namespace;
            //\\ this.Prefix = table.Prefix; 
            //\\ this.MinimumCapacity = table.MinimumCapacity; 
            //\\ }
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Assembly | MemberAttributes.Final); 
            constructor.Attributes = MemberAttributes.Assembly | MemberAttributes.Final;
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataTable)), "table"));

            //\\ this.TableName = <TableName>; 
            constructor.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "TableName"), 
                    CodeGenHelper.Property(CodeGenHelper.Argument("table"), "TableName")
                ) 
            );

            constructor.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Property(CodeGenHelper.Argument("table"), "CaseSensitive"), 
                        CodeGenHelper.Property( 
                            CodeGenHelper.Property(CodeGenHelper.Argument("table"), "DataSet"),
                            "CaseSensitive" 
                        )
                    ),
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(CodeGenHelper.This(), "CaseSensitive"), 
                        CodeGenHelper.Property(CodeGenHelper.Argument("table"), "CaseSensitive")
                    ) 
                ) 
            );
            constructor.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Property(CodeGenHelper.Argument("table"),"Locale"), 
                            "ToString"
                        ), 
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Property(
                                CodeGenHelper.Property(CodeGenHelper.Argument("table"),"DataSet"), 
                                "Locale"
                            ),
                            "ToString"
                        ) 
                    ),
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Locale"), 
                        CodeGenHelper.Property(CodeGenHelper.Argument("table"), "Locale")
                    ) 
                )
            );
            constructor.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Property(CodeGenHelper.Argument("table"), "Namespace"), 
                        CodeGenHelper.Property( 
                            CodeGenHelper.Property(CodeGenHelper.Argument("table"), "DataSet"),
                            "Namespace" 
                        )
                    ),
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(CodeGenHelper.This(), "Namespace"), 
                        CodeGenHelper.Property(CodeGenHelper.Argument("table"), "Namespace")
                    ) 
                ) 
            );
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "Prefix"),
                    CodeGenHelper.Property(CodeGenHelper.Argument("table"),"Prefix")
                ) 
            );
            constructor.Statements.Add( 
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "MinimumCapacity"),
                    CodeGenHelper.Property(CodeGenHelper.Argument("table"), "MinimumCapacity") 
                )
            );

            return constructor; 
        }
 
        private CodeConstructor DeserializingConstructor() { 
            //\\ protected <TableName>DataTableClass("<info>,<context>") : base("<info>,<context>") {
            //\\    InitVars(); 
            //\\ }
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Family);
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Runtime.Serialization.SerializationInfo)), "info"));
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Runtime.Serialization.StreamingContext)), "context")); 
            constructor.BaseConstructorArgs.AddRange(new CodeExpression[] { CodeGenHelper.Argument("info"), CodeGenHelper.Argument("context") });
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitVars")); 
 
            return constructor;
        } 

        private CodeMemberMethod InitExpressionsMethod() {
            bool bInitExpressions = false;
 
            //\\  private void InitExpressions {
            //\\    this.<ColumnProperty>.Expression = "<ColumnExpression>"; 
            //\\    ... 
            //\\  }
            CodeMemberMethod initExpressions = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitExpressions", MemberAttributes.Private); 

            DataTable table = designTable.DataTable;

            foreach(DataColumn column in table.Columns) { 

                if (column.Expression.Length > 0) { 
 
                    CodeExpression codeField = CodeGenHelper.Property(
                        CodeGenHelper.This(), 
                        codeGenerator.TableHandler.Tables[column.Table.TableName].DesignColumns[column.ColumnName].GeneratorColumnPropNameInTable
                    );

                    bInitExpressions = true; 
                    initExpressions.Statements.Add(
                        CodeGenHelper.Assign( 
                            CodeGenHelper.Property(codeField, "Expression"), 
                            CodeGenHelper.Str(column.Expression)
                        ) 
                    );
                }
            }
 

            if (bInitExpressions) { 
                return initExpressions; 
            }
            else { 
                return null;
            }
        }
 

        private CodeMemberMethod AddTypedRowMethod() { 
            //\\ public void Add<RowClassName>(<RowClassName>  row) { 
            //\\     this.Rows.Add(row);
            //\\ } 
            CodeMemberMethod addMethod = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(void)),
                NameHandler.FixIdName("Add" + rowClassName),
                MemberAttributes.Public | MemberAttributes.Final 
            );
            addMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.Type(rowConcreteClassName), "row")); 
            addMethod.Statements.Add( 
                CodeGenHelper.MethodCall(
                    CodeGenHelper.Property(CodeGenHelper.This(), "Rows"), 
                    "Add",
                    CodeGenHelper.Argument("row")
                )
            ); 

            return addMethod; 
        } 

        private void AddTypedRowByColumnsMethods(CodeTypeDeclaration dataTableClass) { 
            //\\ public <RowClassName> Add<RowClassName>(<ColType> <ColName>[, <ColType> <ColName> ...]) {
            //\\     <RowClassName> row;
            //\\     row = ((COMPUTERRow)(this.NewRow()));
            //\\     row.ItemArray = new Object[] {NAME, VERSION, null}; 
            //\\     this.Rows.Add(row);
            //\\     return row; 
            //\\ } 
            DataTable table = designTable.DataTable;
            ArrayList parameterColumnList = new ArrayList(); 
            bool needOverloadWithoutExpressionColumns = false;
            for (int i = 0; i < table.Columns.Count; i++) {
                if (!table.Columns[i].AutoIncrement) {
                    parameterColumnList.Add(table.Columns[i]); 

                    if (!StringUtil.Empty(table.Columns[i].Expression)) { 
                        needOverloadWithoutExpressionColumns = true; 
                    }
                } 
            }


            string methodName = NameHandler.FixIdName("Add" + rowClassName); 
            GenericNameHandler nameHandler = new GenericNameHandler(new string[] { methodName, columnValuesArrayName }, this.codeGenerator.CodeProvider);
 
            CodeMemberMethod addByColName = CodeGenHelper.MethodDecl( 
                CodeGenHelper.Type(rowConcreteClassName),
                methodName, 
                MemberAttributes.Public | MemberAttributes.Final
            );
            CodeMemberMethod addByColNameNoExpressionColumns = CodeGenHelper.MethodDecl(
                CodeGenHelper.Type(rowConcreteClassName), 
                methodName,
                MemberAttributes.Public | MemberAttributes.Final 
            ); 

            DataColumn[] index = new DataColumn[parameterColumnList.Count]; 
            parameterColumnList.CopyTo(index, 0);

            for (int i = 0; i < index.Length; i++) {
                Type dataType = index[i].DataType; 
                DataRelation relation = FindParentRelation(index[i]);
                if(ChildRelationFollowable(relation)) { 
                    string parentTypedRowName = codeGenerator.TableHandler.Tables[relation.ParentTable.TableName].GeneratorRowClassName; 
                    string argumentName = NameHandler.FixIdName("parent" + parentTypedRowName + "By" + relation.RelationName);
                    addByColName.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.Type(parentTypedRowName), nameHandler.AddNameToList(argumentName))); 
                    addByColNameNoExpressionColumns.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.Type(parentTypedRowName), nameHandler.GetNameFromList(argumentName)));
                }
                else {
                    addByColName.Parameters.Add( 
                        CodeGenHelper.ParameterDecl(
                            CodeGenHelper.Type(dataType), 
                            nameHandler.AddNameToList(codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow) 
                        )
                    ); 
                    if (StringUtil.Empty(index[i].Expression)) {
                        addByColNameNoExpressionColumns.Parameters.Add(
                            CodeGenHelper.ParameterDecl(
                                CodeGenHelper.Type(dataType), 
                                nameHandler.GetNameFromList(codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow)
                            ) 
                        ); 
                    }
                } 
            }

            CodeStatement rowVariableDeclaration = CodeGenHelper.VariableDecl(
                CodeGenHelper.Type(rowConcreteClassName), 
                NameHandler.FixIdName("row" + rowClassName),
                CodeGenHelper.Cast( 
                    CodeGenHelper.Type(rowConcreteClassName), 
                    CodeGenHelper.MethodCall(CodeGenHelper.This(), "NewRow")
                ) 
            );
            addByColName.Statements.Add(rowVariableDeclaration);
            addByColNameNoExpressionColumns.Statements.Add(rowVariableDeclaration);
 
            CodeExpression varRow = CodeGenHelper.Variable(NameHandler.FixIdName("row" + rowClassName));
            CodeAssignStatement assignStmt = new CodeAssignStatement(); 
            assignStmt.Left = CodeGenHelper.Property(varRow, "ItemArray"); 
            CodeArrayCreateExpression newArray = new CodeArrayCreateExpression();
            newArray.CreateType = CodeGenHelper.GlobalType(typeof(object)); 
            CodeArrayCreateExpression newArrayNoExpressionColumns = new CodeArrayCreateExpression();
            newArrayNoExpressionColumns.CreateType = CodeGenHelper.GlobalType(typeof(object));

            index = new DataColumn[table.Columns.Count]; 
            table.Columns.CopyTo(index, 0);
 
            for (int i = 0; i < index.Length; i++) { 
                if (index[i].AutoIncrement) {
                    newArray.Initializers.Add(CodeGenHelper.Primitive(null)); 
                    newArrayNoExpressionColumns.Initializers.Add(CodeGenHelper.Primitive(null));
                }
                else {
                    DataRelation relation = FindParentRelation(index[i]); 
                    if (ChildRelationFollowable(relation)) {
                        newArray.Initializers.Add(CodeGenHelper.Primitive(null)); 
                        newArrayNoExpressionColumns.Initializers.Add(CodeGenHelper.Primitive(null)); 
                    }
                    else { 
                        newArray.Initializers.Add(
                            CodeGenHelper.Argument(
                                nameHandler.GetNameFromList(codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow)
                            ) 
                        );
                        if (StringUtil.Empty(index[i].Expression)) { 
                            newArrayNoExpressionColumns.Initializers.Add( 
                                CodeGenHelper.Argument(
                                    nameHandler.GetNameFromList(codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow) 
                                )
                            );
                        }
                        else { 
                            newArrayNoExpressionColumns.Initializers.Add(CodeGenHelper.Primitive(null));
                        } 
                    } 
                }
            } 

            addByColName.Statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(object), 1), columnValuesArrayName, newArray));
            addByColNameNoExpressionColumns.Statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(object), 1), columnValuesArrayName, newArrayNoExpressionColumns));
 
            for (int i = 0; i < index.Length; i++) {
                if (!index[i].AutoIncrement) { 
                    DataRelation relation = FindParentRelation(index[i]); 
                    if (ChildRelationFollowable(relation)) {
                        string parentTypedRowName = codeGenerator.TableHandler.Tables[relation.ParentTable.TableName].GeneratorRowClassName; 
                        string argumentName = NameHandler.FixIdName("parent" + parentTypedRowName + "By" + relation.RelationName);

                        CodeStatement ifStatement = CodeGenHelper.If(
                            CodeGenHelper.IdNotEQ( 
                                CodeGenHelper.Argument(nameHandler.GetNameFromList(argumentName)),
                                CodeGenHelper.Primitive(null) 
                            ), 
                            CodeGenHelper.Assign(
                                CodeGenHelper.Indexer( 
                                    CodeGenHelper.Variable(columnValuesArrayName),
                                    CodeGenHelper.Primitive(i)
                                ),
                                CodeGenHelper.Indexer( 
                                    CodeGenHelper.Argument(nameHandler.GetNameFromList(argumentName)),
                                    CodeGenHelper.Primitive(relation.ParentColumns[0].Ordinal) 
                                ) 
                            )
                        ); 

                        addByColName.Statements.Add(ifStatement);
                        addByColNameNoExpressionColumns.Statements.Add(ifStatement);
                    } 
                }
            } 
 
            assignStmt.Right = CodeGenHelper.Variable(columnValuesArrayName);
            addByColName.Statements.Add(assignStmt); 
            addByColNameNoExpressionColumns.Statements.Add(assignStmt);

            CodeExpression methodCall = CodeGenHelper.MethodCall(
                    CodeGenHelper.Property(CodeGenHelper.This(), "Rows"), 
                    "Add",
                    varRow 
            ); 

            addByColName.Statements.Add(methodCall); 
            addByColNameNoExpressionColumns.Statements.Add(methodCall);

            addByColName.Statements.Add(CodeGenHelper.Return(varRow));
            addByColNameNoExpressionColumns.Statements.Add(CodeGenHelper.Return(varRow)); 

            dataTableClass.Members.Add(addByColName); 
            if (needOverloadWithoutExpressionColumns) { 
                dataTableClass.Members.Add(addByColNameNoExpressionColumns);
            } 
        }

        private void AddFindByMethods( CodeTypeDeclaration dataTableClass ) {
            DataTable table = designTable.DataTable; 

            for (int j = 0; j < table.Constraints.Count; j++) { 
                if (!(table.Constraints[j] is UniqueConstraint)) { 
                    continue;
                } 

                if (!(((UniqueConstraint)(table.Constraints[j])).IsPrimaryKey)) {
                    continue;
                } 

                DataColumn[] index = ((UniqueConstraint)table.Constraints[j]).Columns; 
                string findByName = "FindBy"; 
                bool allHidden = true;
                for (int i = 0; i < index.Length; i++) { 
                    findByName += codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow;
                    if(index[i].ColumnMapping != MappingType.Hidden) {
                        allHidden = false;
                    } 
                }
 
                if(allHidden) { 
                    continue; // We are not generating FindBy* methods for hidden columns
                } 

                //\\ public <RowClassName> FindBy<ColName>[...](<ColType> <ColName>[, ...]) {
                //\\    return (<RowClassName>)(this.Rows.Find(new Object[] {<ColName>[, ...]}));
                //\\ } 
                CodeMemberMethod findBy = CodeGenHelper.MethodDecl(
                    CodeGenHelper.Type(rowClassName), 
                    NameHandler.FixIdName(findByName), 
                    MemberAttributes.Public | MemberAttributes.Final
                ); 
                for (int i = 0; i < index.Length; i++) {
                    findBy.Parameters.Add(
                        CodeGenHelper.ParameterDecl(
                            CodeGenHelper.Type(index[i].DataType), 
                        codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow
                        ) 
                    ); 
                }
 
                CodeArrayCreateExpression arrayCreate = new CodeArrayCreateExpression(typeof(object), index.Length);
                for (int i = 0; i < index.Length; i++) {
                    arrayCreate.Initializers.Add(
                        CodeGenHelper.Argument( 
                        codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow
                        ) 
                    ); 
                }
                findBy.Statements.Add( 
                    CodeGenHelper.Return(
                        CodeGenHelper.Cast(
                            CodeGenHelper.Type(rowClassName),
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Property(CodeGenHelper.This(), "Rows"),
                                "Find", 
                                arrayCreate 
                            )
                        ) 
                    )
                );

                dataTableClass.Members.Add(findBy); 
            }
 
        } 

        private CodeMemberMethod GetEnumeratorMethod() { 
            CodeMemberMethod getEnumerator = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(System.Collections.IEnumerator)),
                "GetEnumerator",
                MemberAttributes.Public 
            );
            getEnumerator.ImplementationTypes.Add(CodeGenHelper.GlobalType(typeof(System.Collections.IEnumerable))); 
            getEnumerator.Statements.Add( 
                CodeGenHelper.Return(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Rows"),
                        "GetEnumerator"
                    )
                ) 
            );
 
            return getEnumerator; 
        }
 
        private CodeMemberMethod CloneMethod() {
            //\\ public override DataTable Clone() {
            //\\     <TableClassName> cln = (<TableClassName)base.Clone();
            //\\     cln.InitVars(); 
            //\\     return cln;
            //\\ } 
            CodeMemberMethod clone = CodeGenHelper.MethodDecl( 
                CodeGenHelper.GlobalType(typeof(System.Data.DataTable)),
                "Clone", 
                MemberAttributes.Public | MemberAttributes.Override
            );
            clone.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.Type(tableClassName),
                    "cln", 
                    CodeGenHelper.Cast( 
                        CodeGenHelper.Type(tableClassName),
                        CodeGenHelper.MethodCall(CodeGenHelper.Base(), "Clone", new CodeExpression[] {}) 
                    )
                )
            );
            clone.Statements.Add( 
                CodeGenHelper.MethodCall(
                    CodeGenHelper.Variable("cln"), 
                    "InitVars", 
                    new CodeExpression[] {}
                ) 
            );
            clone.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable("cln")));

            return clone; 
        }
 
        private CodeMemberMethod CreateInstanceMethod() { 
            //\\ protected override DataTable CreateInstance() {
            //\\     return new <TableClassName>() 
            //\\ }
            CodeMemberMethod createInstance = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(System.Data.DataTable)),
                "CreateInstance", 
                MemberAttributes.Family | MemberAttributes.Override
            ); 
            createInstance.Statements.Add( 
                CodeGenHelper.Return(
                    CodeGenHelper.New(CodeGenHelper.Type(tableClassName), new CodeExpression[] {}) 
                )
            );

            return createInstance; 
        }
 
        private void InitClassAndInitVarsMethods(CodeTypeDeclaration tableClass, out CodeMemberMethod tableInitClass, out CodeMemberMethod tableInitVars) { 
            DataTable table = designTable.DataTable;
            //\\ private void InitClass() ... 
            tableInitClass = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitClass", MemberAttributes.Private);

            //\\ public void InitVars() ...
            tableInitVars = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitVars", MemberAttributes.Assembly | MemberAttributes.Final); 

            for (int i = 0; i < table.Columns.Count; i++) { 
                DataColumn column = table.Columns[i]; 
                string columnName = codeGenerator.TableHandler.Tables[table.TableName].DesignColumns[column.ColumnName].GeneratorColumnVarNameInTable;
                //\\ this.column<ColumnName> 
                CodeExpression codeField = CodeGenHelper.Field(CodeGenHelper.This(), columnName);

                //\\ this.column<ColumnName> = new DataColumn("<ColumnName>", typeof(<ColumnType>), "", MappingType.Hidden);
                string mappingType = "Element"; 
                if(column.ColumnMapping == MappingType.SimpleContent) {
                    mappingType = "SimpleContent"; 
                } 
                else if(column.ColumnMapping == MappingType.Attribute) {
                    mappingType = "Attribute"; 
                }
                else if(column.ColumnMapping == MappingType.Hidden) {
                    mappingType = "Hidden";
                } 
                tableInitClass.Statements.Add(
                    CodeGenHelper.Assign( 
                        codeField, 
                        CodeGenHelper.New(
                            CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), 
                            new CodeExpression[] {
                                CodeGenHelper.Str(column.ColumnName),
                                CodeGenHelper.TypeOf(CodeGenHelper.GlobalType(column.DataType)),
                                CodeGenHelper.Primitive(null), 
                                CodeGenHelper.Field(
                                    CodeGenHelper.GlobalTypeExpr(typeof(MappingType)), 
                                    mappingType 
                                )
                            } 
                        )
                ));

                // Add extended properties to the DataColumn, if there are any in the schema file 
                ExtendedPropertiesHandler.CodeGenerator = codeGenerator;
                ExtendedPropertiesHandler.AddExtendedProperties(designTable.DesignColumns[column.ColumnName], codeField, tableInitClass.Statements, column.ExtendedProperties); 
 
                //\\ this.Columns.Add(this.<ColumnName>);
                tableInitClass.Statements.Add( 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Base(), "Columns"),
                        "Add",
                        CodeGenHelper.Field(CodeGenHelper.This(), columnName) 
                    )
                ); 
            } 

            for (int i = 0; i < table.Constraints.Count; i++) { 
                if (!(table.Constraints[i] is UniqueConstraint)) {
                    continue;
                }
                //\\ this.Constraints.Add = new UniqueConstraint(<constraintName>, new DataColumn[] {this.column<ColumnName> [, ...]}); 
                UniqueConstraint uc = (UniqueConstraint)(table.Constraints[i]);
                DataColumn[] columns = uc.Columns; 
                CodeExpression[] createArgs = new CodeExpression[columns.Length]; 
                for (int j = 0; j < columns.Length; j++) {
                    createArgs[j] = CodeGenHelper.Field( 
                        CodeGenHelper.This(),
                        codeGenerator.TableHandler.Tables[columns[j].Table.TableName].DesignColumns[columns[j].ColumnName].GeneratorColumnVarNameInTable
                    );
                } 
                tableInitClass.Statements.Add(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Constraints"), 
                        "Add",
                        CodeGenHelper.New( 
                            CodeGenHelper.GlobalType(typeof(System.Data.UniqueConstraint)),
                            new CodeExpression[] {
                                CodeGenHelper.Str(uc.ConstraintName),
                                new CodeArrayCreateExpression(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), createArgs), 
                                CodeGenHelper.Primitive(uc.IsPrimaryKey)
                            } 
                        ) 
                    )
                ); 
            }

            for (int i = 0; i < table.Columns.Count; i++) {
                DataColumn column = table.Columns[i]; 
                string columnName = codeGenerator.TableHandler.Tables[table.TableName].DesignColumns[column.ColumnName].GeneratorColumnVarNameInTable;
                //\\ this.<ColumnVariableName> 
                CodeExpression codeField = CodeGenHelper.Field(CodeGenHelper.This(), columnName); 

                //\\ this.<ColumnVariableName> = this.Columns["<ColumnName>"]; 
                tableInitVars.Statements.Add(
                    CodeGenHelper.Assign(
                        codeField,
                        CodeGenHelper.Indexer( 
                            CodeGenHelper.Property(CodeGenHelper.Base(), "Columns"),
                            CodeGenHelper.Str(column.ColumnName) 
                        ) 
                    )
                ); 

                if (column.AutoIncrement) {
                    //\\ this.<ColumnVariableName>.AutoIncrement = true;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "AutoIncrement"), 
                            CodeGenHelper.Primitive(true) 
                        )
                    ); 
                }
                if (column.AutoIncrementSeed != 0) {
                    //\\ this.<ColumnVariableName>.AutoIncrementSeed = <column.AutoIncrementSeed>;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "AutoIncrementSeed"), 
                            CodeGenHelper.Primitive(column.AutoIncrementSeed) 
                        )
                    ); 
                }
                if (column.AutoIncrementStep != 1) {
                    //\\ this.<ColumnVariableName>.AutoIncrementStep = <column.AutoIncrementStep>;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "AutoIncrementStep"), 
                            CodeGenHelper.Primitive(column.AutoIncrementStep) 
                        )
                    ); 
                }
                if (!column.AllowDBNull) {
                    //\\ this.<ColumnVariableName>.AllowDBNull = false;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "AllowDBNull"), 
                            CodeGenHelper.Primitive(false) 
                        )
                    ); 
                }
                if (column.ReadOnly) {
                    //\\ this.<ColumnVariableName>.ReadOnly = true;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "ReadOnly"), 
                            CodeGenHelper.Primitive(true) 
                        )
                    ); 
                }
                if (column.Unique) {
                    //\\ this.<ColumnVariableName>.Unique = true;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "Unique"), 
                            CodeGenHelper.Primitive(true) 
                        )
                    ); 
                }
                if (!StringUtil.Empty(column.Prefix)) {
                    //\\ this.<ColumnVariableName>.Prefix = "<column.Prefix>";
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "Prefix"), 
                            CodeGenHelper.Str(column.Prefix) 
                        )
                    ); 
                }
                if(columnNamespaceProperty.ShouldSerializeValue(column)) {
                    //\\ this.<ColumnVariableName>.Namespace = "<column.Namespace>";
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "Namespace"), 
                            CodeGenHelper.Str(column.Namespace) 
                        )
                    ); 
                }
                if (column.Caption != column.ColumnName) {
                    //\\ this.<ColumnVariableName>.Caption = "<column.Caption>";
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "Caption"), 
                            CodeGenHelper.Str(column.Caption) 
                        )
                    ); 
                }
                if (column.DefaultValue != DBNull.Value) {
                    CodeExpression defaultValueExpr = null;
                    CodeExpression defaultValueFieldInit = null; 
                    DesignColumn designColumn = codeGenerator.TableHandler.Tables[table.TableName].DesignColumns[column.ColumnName];
 
                    DSGeneratorProblem problem = CodeGenHelper.GenerateValueExprAndFieldInit(designColumn, column.DefaultValue, column.DefaultValue, designTable.GeneratorTableClassName, columnName + "_defaultValue", out defaultValueExpr, out defaultValueFieldInit); 
                    if (problem != null) {
                        codeGenerator.ProblemList.Add(problem); 
                    }
                    else {
                        if (defaultValueFieldInit != null) {
                            CodeMemberField defaultValueField = CodeGenHelper.FieldDecl( 
                                CodeGenHelper.Type(column.DataType.FullName),
                                columnName + "_defaultValue" 
                            ); 
                            defaultValueField.Attributes = MemberAttributes.Static | MemberAttributes.Private;
                            defaultValueField.InitExpression = defaultValueFieldInit; 

                            tableClass.Members.Add(defaultValueField);
                        }
 

                        //\\ this.<ColumnVariableName>.DefaultValue = "<column.DefaultValue>"; 
                        CodeCastExpression cce = new CodeCastExpression(column.DataType, defaultValueExpr); 
                        // J# specific UserData
                        cce.UserData.Add("CastIsBoxing", true); 

                        tableInitClass.Statements.Add(
                            CodeGenHelper.Assign(
                                CodeGenHelper.Property(codeField, "DefaultValue"), 
                                cce
                            ) 
                        ); 
                    }
                } 
                if (column.MaxLength != -1) {
                    //\\ this.<ColumnVariableName>.MaxLength = "<column.MaxLength>";
                    tableInitClass.Statements.Add(
                        CodeGenHelper.Assign( 
                            CodeGenHelper.Property(codeField, "MaxLength"),
                            CodeGenHelper.Primitive(column.MaxLength) 
                        ) 
                    );
                } 
                if (column.DateTimeMode != DataSetDateTime.UnspecifiedLocal) {
                    //\\ this.<ColumnVariableName>.DateTimeMode = "<column.DateTimeMode>";
                    tableInitClass.Statements.Add(
                        CodeGenHelper.Assign( 
                            CodeGenHelper.Property(codeField, "DateTimeMode"),
                            CodeGenHelper.Field( 
                                CodeGenHelper.GlobalTypeExpr(typeof(DataSetDateTime)), 
                                column.DateTimeMode.ToString()
                            ) 
                        )
                    );
                }
            } 

            if (caseSensitiveProperty.ShouldSerializeValue(table)) { 
                //\\ this.CaseSensitive = <CaseSensitive>; 
                tableInitClass.Statements.Add(
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "CaseSensitive"),
                        CodeGenHelper.Primitive(table.CaseSensitive)
                    )
                ); 
            }
 
            CultureInfo culture = table.Locale; 
            if (culture != null) {
                if (localeProperty.ShouldSerializeValue(table)) { 
                    //\\ this.Locale = new System.Globalization.CultureInfo("<Locale>");
                    tableInitClass.Statements.Add(
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(CodeGenHelper.This(), "Locale"), 
                            CodeGenHelper.New(
                                CodeGenHelper.GlobalType(typeof(System.Globalization.CultureInfo)), 
                                new CodeExpression[] { CodeGenHelper.Str(table.Locale.ToString()) } 
                            )
                        ) 
                    );
                }
            }
            if (!StringUtil.Empty(table.Prefix)) { 
                //\\ this.Prefix = "<Prefix>";
                tableInitClass.Statements.Add( 
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Prefix"),
                        CodeGenHelper.Str(table.Prefix) 
                    )
                );
            }
            if(namespaceProperty.ShouldSerializeValue(table)) { 
                //\\ this.Namespace = <Namespace>;
                tableInitClass.Statements.Add( 
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Namespace"),
                        CodeGenHelper.Str(table.Namespace) 
                    )
                );
            }
            if (table.MinimumCapacity != 50) { 
                //\\ this.MinimumCapacity = <MinimumCapacity>;
                tableInitClass.Statements.Add( 
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "MinimumCapacity"),
                        CodeGenHelper.Primitive(table.MinimumCapacity) 
                    )
                );
            }
 
            // Add extended properties to the DataTable, if there are any in the schema file
            ExtendedPropertiesHandler.CodeGenerator = codeGenerator; 
            ExtendedPropertiesHandler.AddExtendedProperties(designTable, CodeGenHelper.This(), tableInitClass.Statements, table.ExtendedProperties); 
        }
 
        private CodeMemberMethod NewTypedRowMethod() {
            //\\ public <RowClassName> New<RowClassName>() {
            //\\     return (<RowClassName>) NewRow();
            //\\ } 
            CodeMemberMethod newTableRow = CodeGenHelper.MethodDecl(
                CodeGenHelper.Type(rowConcreteClassName), 
                NameHandler.FixIdName("New" + rowClassName), 
                MemberAttributes.Public | MemberAttributes.Final
            ); 
            newTableRow.Statements.Add(
                CodeGenHelper.Return(
                    CodeGenHelper.Cast(
                        CodeGenHelper.Type(rowConcreteClassName), 
                        CodeGenHelper.MethodCall(CodeGenHelper.This(), "NewRow")
                    ) 
                ) 
            );
 
            return newTableRow;
        }

        private CodeMemberMethod NewRowFromBuilderMethod() { 
            //\\ protected override DataRow NewRowFromBuilder(DataRowBuilder builder) {
            //\\     return new<RowClassName>(builder); 
            //\\ } 
            CodeMemberMethod newRowFromBuilder = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(System.Data.DataRow)), 
                "NewRowFromBuilder",
                MemberAttributes.Family | MemberAttributes.Override
            );
            newRowFromBuilder.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRowBuilder)), "builder")); 
            newRowFromBuilder.Statements.Add(
                CodeGenHelper.Return( 
                    CodeGenHelper.New( 
                        CodeGenHelper.Type(rowConcreteClassName),
                        new CodeExpression[] {CodeGenHelper.Argument("builder")} 
                    )
                )
            );
 
            return newRowFromBuilder;
        } 
 
        private CodeMemberMethod GetRowTypeMethod() {
            //\\ protected override System.Type GetRowType() { 
            //\\     return typeof(<RowConcreateClassName>);
            //\\ }
            CodeMemberMethod getRowType = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(System.Type)), 
                "GetRowType",
                MemberAttributes.Family | MemberAttributes.Override 
            ); 
            getRowType.Statements.Add(CodeGenHelper.Return(CodeGenHelper.TypeOf(CodeGenHelper.Type(rowConcreteClassName))));
 
            return getRowType;
        }

        private CodeMemberMethod CreateOnRowEventMethod(string eventName, string typedEventName) { 
            //\\ protected override void OnRow<eventName>(DataRowChangeEventArgs e) {
            //\\     base.OnRow<eventName>(e); 
            //\\     if (((this.<typedEventName>) != (null))) { 
            //\\         this.<typedEventName>(this, new <TypedRowEvArgName>(((<eventName>)(e.Row)), e.Action));
            //\\     } 
            //\\ }
            CodeMemberMethod onRowEvent = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(void)),
                "OnRow" + eventName, 
                MemberAttributes.Family | MemberAttributes.Override
            ); 
            onRowEvent.Parameters.Add( 
                CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRowChangeEventArgs)), "e")
            ); 
            onRowEvent.Statements.Add(
                CodeGenHelper.MethodCall(
                    CodeGenHelper.Base(),
                    "OnRow" + eventName, 
                    CodeGenHelper.Argument("e")
                ) 
            ); 
            onRowEvent.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Event(typedEventName),
                        CodeGenHelper.Primitive(null)
                    ), 
                    CodeGenHelper.Stm(
                        CodeGenHelper.DelegateCall( 
                            CodeGenHelper.Event(typedEventName), 
                            CodeGenHelper.New(
                                CodeGenHelper.Type(designTable.GeneratorRowEvArgName), 
                                new CodeExpression[] {
                                    CodeGenHelper.Cast(
                                        CodeGenHelper.Type(rowClassName),
                                        CodeGenHelper.Property(CodeGenHelper.Argument("e"), "Row") 
                                    ),
                                    CodeGenHelper.Property(CodeGenHelper.Argument("e"), "Action") 
                                } 
                            )
                        ) 
                    )
                )
            );
 
            return onRowEvent;
        }// CreateOnRowEventMethod 
 

        private void AddOnRowEventMethods(CodeTypeDeclaration dataTableClass) { 
            dataTableClass.Members.Add(CreateOnRowEventMethod("Changed", this.designTable.GeneratorRowChangedName));
            dataTableClass.Members.Add(CreateOnRowEventMethod("Changing", this.designTable.GeneratorRowChangingName));
            dataTableClass.Members.Add(CreateOnRowEventMethod("Deleted", this.designTable.GeneratorRowDeletedName));
            dataTableClass.Members.Add(CreateOnRowEventMethod("Deleting", this.designTable.GeneratorRowDeletingName)); 
        }
 
        private CodeMemberMethod RemoveRowMethod() { 
            //\\ public void Remove<RowClassName>(<RowClassName> row) {
            //\\     this.Rows.Remove(row); 
            //\\ }
            CodeMemberMethod removeMethod = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(void)),
                NameHandler.FixIdName("Remove" + rowClassName), 
                MemberAttributes.Public | MemberAttributes.Final
            ); 
            removeMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.Type(rowConcreteClassName), "row")); 
            removeMethod.Statements.Add(
                CodeGenHelper.MethodCall( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "Rows"),
                    "Remove",
                    CodeGenHelper.Argument("row")
                ) 
            );
 
            return removeMethod; 
        }
 
        private bool ChildRelationFollowable(DataRelation relation) {
            if (relation != null) {
                if (relation.ChildTable == relation.ParentTable) {
                    if (relation.ChildTable.Columns.Count == 1) { 
                        return false;
                    } 
                } 
                return true;
            } 
            return false;
        }

        private DataRelation FindParentRelation(DataColumn column) { 
            DataRelation[] parentRelations = new DataRelation[column.Table.ParentRelations.Count];
            column.Table.ParentRelations.CopyTo(parentRelations, 0); 
 
            for (int i = 0; i < parentRelations.Length; i++) {
                DataRelation relation = parentRelations[i]; 
                if (relation.ChildColumns.Length == 1 && relation.ChildColumns[0] == column) {
                    return relation;
                }
            } 
            // should we throw an exception?
            return null; 
        } 

        private CodeMemberMethod GetTypedTableSchema() { 
            //\\ public static XmlSchemaComplexType GetTypedTableSchema(XmlSchemaSet xs) {
            //\\    XmlSchemaComplexType type = new XmlSchemaComplexType();
            //\\    XmlSchemaSequence sequence = new XmlSchemaSequence();
            //\\    NorthwindDataSet ds = new NorthwindDataSet(); 
            //\\    XmlSchemaAny any1 = new XmlSchemaAny();
            //\\    any1.Namespace = XmlSchema.Namespace; 
            //\\    any1.MinOccurs = 0; 
            //\\    any1.MaxOccurs = Decimal.MaxValue;
            //\\    any1.ProcessContents = XmlSchemaContentProcessing.Lax; 
            //\\    sequence.Items.Add(any1);
            //\\    XmlSchemaAny any2 = new XmlSchemaAny();
            //\\    any2.Namespace = Keywords.DFFNS;
            //\\    any2.MinOccurs = 1; 
            //\\    any2.ProcessContents = XmlSchemaContentProcessing.Lax;
            //\\    sequence.Items.Add(any2); 
            //\\    XmlSchemaAttribute attribute1 = new XmlSchemaAttribute(); 
            //\\    attribute1.Name = "namespace";
            //\\    attribute1.FixedValue = ds.Namespace; 
            //\\    type.Attributes.Add(attribute1);
            //\\    XmlSchemaAttribute attribute2 = new XmlSchemaAttribute();
            //\\    attribute2.Name = "tableTypeName";
            //\\    attribute2.FixedValue = "CustomersDataTable"; 
            //\\    type.Attributes.Add(attribute2);
            //\\    type.Particle = sequence; 
            //\\    XmlSchema dsSchema = ds.GetSchemaSerializable(); 
            //\\    if (xs.Contains(dsSchema.TargetNamespace)) {
            //\\        MemoryStream s1 = new MemoryStream(); 
            //\\        MemoryStream s2 = new MemoryStream();
            //\\        try {
            //\\            XmlSchema schema = null;
            //\\            dsSchema.Write(s1); 
            //\\            for (IEnumerator schemas = xs.Schemas(dsSchema.TargetNamespace).GetEnumerator(); schemas.MoveNext(); ) {
            //\\                schema = (XmlSchema)schemas.Current; 
            //\\                s2.SetLength(0); 
            //\\                schema.Write(s2);
            //\\                if ((s1.Length == s2.Length)) { 
            //\\                    s1.Position = 0;
            //\\                    s2.Position = 0;
            //\\                    for (; ((s1.Position != s1.Length) && (s1.ReadByte() == s2.ReadByte())); ) { }
            //\\                    if ((s1.Position == s1.Length)) { 
            //\\                       return type;
            //\\                    } 
            //\\                } 
            //\\            }
            //\\        } 
            //\\        finally {
            //\\            if (s1 != null) {
            //\\                s1.Dispose();
            //\\            } 
            //\\            if (s2 != null) {
            //\\                s2.Dispose(); 
            //\\            } 
            //\\        }
            //\\    } 
            //\\    xs.Add(dsSchema);
            //\\    return type;
            //\\ }
 
            CodeMemberMethod getTableSchema = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)), "GetTypedTableSchema", MemberAttributes.Static | MemberAttributes.Public);
            getTableSchema.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(XmlSchemaSet)), "xs")); 
 
            getTableSchema.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)),
                    "type",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)), new CodeExpression[] {})
                ) 
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaSequence)),
                    "sequence", 
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaSequence)), new CodeExpression[] {})
                )
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.Type(codeGenerator.DataSourceName), 
                    "ds", 
                    CodeGenHelper.New(
                        CodeGenHelper.Type(codeGenerator.DataSourceName), 
                        new CodeExpression[] {}
                    )
                )
            ); 

            getTableSchema.Statements.Add( 
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaAny)),
                    "any1", 
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaAny)), new CodeExpression[] {})
                )
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any1"), "Namespace"), 
                    CodeGenHelper.Str(XmlSchema.Namespace) 
                )
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any1"), "MinOccurs"),
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Decimal)), new CodeExpression[] { CodeGenHelper.Primitive(0) }) 
                )
            ); 
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any1"), "MaxOccurs"), 
                    CodeGenHelper.Field(
                        CodeGenHelper.GlobalTypeExpr(typeof(System.Decimal)),
                        "MaxValue"
                    ) 
                )
            ); 
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any1"), "ProcessContents"), 
                    CodeGenHelper.Field(
                        CodeGenHelper.GlobalTypeExpr(typeof(XmlSchemaContentProcessing)),
                        "Lax"
                    ) 
                )
            ); 
            getTableSchema.Statements.Add( 
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Property(CodeGenHelper.Variable("sequence"), "Items"),
                        "Add",
                        new CodeExpression[] { CodeGenHelper.Variable("any1") }
                    ) 
                )
            ); 
            getTableSchema.Statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(XmlSchemaAny)), 
                    "any2",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaAny)), new CodeExpression[] {})
                )
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.Variable("any2"), "Namespace"), 
                    CodeGenHelper.Str(Keywords.DFFNS)
                ) 
            );
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any2"), "MinOccurs"), 
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Decimal)), new CodeExpression[] { CodeGenHelper.Primitive(1) })
                ) 
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.Variable("any2"), "ProcessContents"),
                    CodeGenHelper.Field(
                        CodeGenHelper.GlobalTypeExpr(typeof(XmlSchemaContentProcessing)),
                        "Lax" 
                    )
                ) 
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Stm( 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Variable("sequence"), "Items"),
                        "Add",
                        new CodeExpression[] { CodeGenHelper.Variable("any2") } 
                    )
                ) 
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaAttribute)),
                    "attribute1",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaAttribute)), new CodeExpression[] {})
                ) 
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.Variable("attribute1"), "Name"),
                    CodeGenHelper.Primitive("namespace") 
                )
            );
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.Variable("attribute1"), "FixedValue"),
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "Namespace") 
                ) 
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Variable("type"), "Attributes"),
                        "Add", 
                        new CodeExpression[] { CodeGenHelper.Variable("attribute1") }
                    ) 
                ) 
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(XmlSchemaAttribute)),
                    "attribute2",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaAttribute)), new CodeExpression[] {}) 
                )
            ); 
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("attribute2"), "Name"), 
                    CodeGenHelper.Primitive("tableTypeName")
                )
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("attribute2"), "FixedValue"), 
                    CodeGenHelper.Str(designTable.GeneratorTableClassName) 
                )
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Variable("type"), "Attributes"), 
                        "Add",
                        new CodeExpression[] { CodeGenHelper.Variable("attribute2") } 
                    ) 
                )
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("type"), "Particle"),
                    CodeGenHelper.Variable("sequence") 
                )
            ); 
 
            // DDBugs 126260: Avoid adding the same schema twice
            DatasetMethodGenerator.GetSchemaIsInCollection(getTableSchema.Statements, "ds", "xs"); 

            getTableSchema.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable("type")));

            return getTableSchema; 
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
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.ComponentModel; 
    using System.Data;
    using System.Data.Common; 
    using System.Data.SqlClient; 
    using System.Globalization;
    using System.Xml; 
    using System.Xml.Schema;
    using System.Xml.Serialization;

    using GenerateOption = TypedDataSetGenerator.GenerateOption; 

    internal sealed class TableMethodGenerator { 
        private TypedDataSourceCodeGenerator codeGenerator = null; 
        private DesignTable designTable = null;
        private string rowClassName = null; 
        private string rowConcreteClassName = null;
        private string tableClassName = null;
        private CodeMemberMethod initExpressionsMethod = null;
 
        private static PropertyDescriptor namespaceProperty = TypeDescriptor.GetProperties(typeof(DataTable))["Namespace"];
        private static PropertyDescriptor localeProperty = TypeDescriptor.GetProperties(typeof(DataTable))["Locale"]; 
        private static PropertyDescriptor caseSensitiveProperty = TypeDescriptor.GetProperties(typeof(DataTable))["CaseSensitive"]; 
        private static PropertyDescriptor columnNamespaceProperty = TypeDescriptor.GetProperties(typeof(DataColumn))["Namespace"];
        private static PropertyDescriptor dateTimeModeProperty = TypeDescriptor.GetProperties(typeof(DataColumn))["DateTimeMode"]; 

        private static string columnValuesArrayName = "columnValuesArray";

        internal TableMethodGenerator(TypedDataSourceCodeGenerator codeGenerator, DesignTable designTable) { 
            this.codeGenerator = codeGenerator;
            this.designTable = designTable; 
        } 

        internal void AddMethods(CodeTypeDeclaration dataTableClass) { 
            if(dataTableClass == null) {
                throw new InternalException("Table CodeTypeDeclaration should not be null.");
            }
 
            // Get necessary identifier names
            this.rowClassName = designTable.GeneratorRowClassName; 
            this.rowConcreteClassName = designTable.GeneratorRowClassName; 
            this.tableClassName = designTable.GeneratorTableClassName;
 
            // Add methods to the class
            initExpressionsMethod = InitExpressionsMethod();
            if (initExpressionsMethod != null) {
                dataTableClass.Members.Add(ArgumentLessConstructorInitExpressions()); 
                dataTableClass.Members.Add(ConstructorWithBoolArgument());
            } 
            else { 
                dataTableClass.Members.Add(ArgumentLessConstructorNoInitExpressions());
            } 
            dataTableClass.Members.Add( ConstructorWithArguments() );
            dataTableClass.Members.Add( DeserializingConstructor() );
            dataTableClass.Members.Add(AddTypedRowMethod());
            AddTypedRowByColumnsMethods(dataTableClass); 
            AddFindByMethods( dataTableClass );
 
            // TypedTableBase handles our IEnumerator implementation, so we don't have to do it here 
            if ((this.codeGenerator.GenerateOptions & GenerateOption.LinqOverTypedDatasets) != GenerateOption.LinqOverTypedDatasets) {
                dataTableClass.Members.Add(GetEnumeratorMethod()); 
            }

            dataTableClass.Members.Add( CloneMethod() );
            dataTableClass.Members.Add( CreateInstanceMethod() ); 
            CodeMemberMethod initClassMethod = null;
            CodeMemberMethod initVarsMethod = null; 
            InitClassAndInitVarsMethods(dataTableClass, out initClassMethod, out initVarsMethod); 
            dataTableClass.Members.Add( initVarsMethod );
            dataTableClass.Members.Add( initClassMethod ); 
            dataTableClass.Members.Add( NewTypedRowMethod() );
            dataTableClass.Members.Add( NewRowFromBuilderMethod() );
            dataTableClass.Members.Add( GetRowTypeMethod() );
            if( initExpressionsMethod != null ) { 
                dataTableClass.Members.Add( initExpressionsMethod );
            } 
            if (this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareEvents) && this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareDelegates)) { 
                AddOnRowEventMethods(dataTableClass);
            } 
            dataTableClass.Members.Add( RemoveRowMethod() );
            dataTableClass.Members.Add( GetTypedTableSchema() );
        }
 
        private CodeConstructor ArgumentLessConstructorInitExpressions() {
            //\\ public <TableClassName>() : this(false){} 
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Public | MemberAttributes.Final); 
            // by default we don't Init expressions when a standalone typed table is created. This behavior is consistent with
            // Everett and avoids failures if some expression have references to related tables (see VSWhidbey #517338) 
            constructor.ChainedConstructorArgs.Add(CodeGenHelper.Primitive(false));

            return constructor;
        } 

        private CodeConstructor ConstructorWithBoolArgument() { 
            //\\ public <TableClassName>(bool initExpressions) : base() { 
            //\\    this.TableName = <TableName>
            //\\    this.BeginInit(); 
            //\\    this.InitClass();
            //\\    if(initExpressions) {
            //\\        this.InitExpressions();
            //\\    } 
            //\\    this.EndInit();
            //\\ } 
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Assembly | MemberAttributes.Final); 
            // DevDiv Bug 6208, considering NT-ier scenario, make the method public
            constructor.Attributes = MemberAttributes.Public | MemberAttributes.Final; 
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(bool)), "initExpressions"));

            //\\ this.TableName = <TableName>;
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "TableName"), 
                    CodeGenHelper.Str(designTable.Name) 
                )
            ); 

            //\\ this.BeginInit();
            //\\ this.InitClass();
            //\\ if(initExpressions) { 
            //\\    this.InitExpressions();
            //\\ } 
            //\\ this.EndInit(); 
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "BeginInit"));
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitClass")); 
            constructor.Statements.Add(
                CodeGenHelper.If(
                    CodeGenHelper.EQ(
                        CodeGenHelper.Argument("initExpressions"), 
                        CodeGenHelper.Primitive(true)
                    ), 
                    new CodeStatement[] { CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitExpressions")) } 
                )
            ); 
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "EndInit"));

            return constructor;
        } 

        private CodeConstructor ArgumentLessConstructorNoInitExpressions() { 
            //\\ public <TableClassName>() : base() { 
            //\\    this.TableName = <TableName>
            //\\    this.BeginInit(); 
            //\\    this.InitClass();
            //\\    this.EndInit();
            //\\ }
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Public | MemberAttributes.Final); 

            //\\ this.TableName = <TableName>; 
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "TableName"), 
                    CodeGenHelper.Str(designTable.Name)
                )
            );
 
            //\\ this.BeginInit();
            //\\ this.InitClass(); 
            //\\ this.EndInit(); 
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "BeginInit"));
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitClass")); 
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "EndInit"));

            return constructor;
        } 

        private CodeConstructor ConstructorWithArguments() { 
            //\\ internal <TableClassName>(DataTable table) : base() { // Assuming incoming table always associated with DataSet 
            //\\ this.TableName = table.TableName
            //\\ if (table.CaseSensitive != table.DataSet.CaseSensitive) 
            //\\    this.CaseSensitive = table.CaseSensitive;
            //\\ if (table.Locale.ToString() != table.DataSet.Locale.ToString())
            //\\    this.Locale = table.Locale;
            //\\ if (table.Namespace != table.DataSet.Namespace) 
            //\\    this.Namespace = table.Namespace;
            //\\ this.Prefix = table.Prefix; 
            //\\ this.MinimumCapacity = table.MinimumCapacity; 
            //\\ }
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Assembly | MemberAttributes.Final); 
            constructor.Attributes = MemberAttributes.Assembly | MemberAttributes.Final;
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataTable)), "table"));

            //\\ this.TableName = <TableName>; 
            constructor.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "TableName"), 
                    CodeGenHelper.Property(CodeGenHelper.Argument("table"), "TableName")
                ) 
            );

            constructor.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Property(CodeGenHelper.Argument("table"), "CaseSensitive"), 
                        CodeGenHelper.Property( 
                            CodeGenHelper.Property(CodeGenHelper.Argument("table"), "DataSet"),
                            "CaseSensitive" 
                        )
                    ),
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(CodeGenHelper.This(), "CaseSensitive"), 
                        CodeGenHelper.Property(CodeGenHelper.Argument("table"), "CaseSensitive")
                    ) 
                ) 
            );
            constructor.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Property(CodeGenHelper.Argument("table"),"Locale"), 
                            "ToString"
                        ), 
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Property(
                                CodeGenHelper.Property(CodeGenHelper.Argument("table"),"DataSet"), 
                                "Locale"
                            ),
                            "ToString"
                        ) 
                    ),
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Locale"), 
                        CodeGenHelper.Property(CodeGenHelper.Argument("table"), "Locale")
                    ) 
                )
            );
            constructor.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Property(CodeGenHelper.Argument("table"), "Namespace"), 
                        CodeGenHelper.Property( 
                            CodeGenHelper.Property(CodeGenHelper.Argument("table"), "DataSet"),
                            "Namespace" 
                        )
                    ),
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(CodeGenHelper.This(), "Namespace"), 
                        CodeGenHelper.Property(CodeGenHelper.Argument("table"), "Namespace")
                    ) 
                ) 
            );
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "Prefix"),
                    CodeGenHelper.Property(CodeGenHelper.Argument("table"),"Prefix")
                ) 
            );
            constructor.Statements.Add( 
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "MinimumCapacity"),
                    CodeGenHelper.Property(CodeGenHelper.Argument("table"), "MinimumCapacity") 
                )
            );

            return constructor; 
        }
 
        private CodeConstructor DeserializingConstructor() { 
            //\\ protected <TableName>DataTableClass("<info>,<context>") : base("<info>,<context>") {
            //\\    InitVars(); 
            //\\ }
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Family);
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Runtime.Serialization.SerializationInfo)), "info"));
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Runtime.Serialization.StreamingContext)), "context")); 
            constructor.BaseConstructorArgs.AddRange(new CodeExpression[] { CodeGenHelper.Argument("info"), CodeGenHelper.Argument("context") });
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitVars")); 
 
            return constructor;
        } 

        private CodeMemberMethod InitExpressionsMethod() {
            bool bInitExpressions = false;
 
            //\\  private void InitExpressions {
            //\\    this.<ColumnProperty>.Expression = "<ColumnExpression>"; 
            //\\    ... 
            //\\  }
            CodeMemberMethod initExpressions = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitExpressions", MemberAttributes.Private); 

            DataTable table = designTable.DataTable;

            foreach(DataColumn column in table.Columns) { 

                if (column.Expression.Length > 0) { 
 
                    CodeExpression codeField = CodeGenHelper.Property(
                        CodeGenHelper.This(), 
                        codeGenerator.TableHandler.Tables[column.Table.TableName].DesignColumns[column.ColumnName].GeneratorColumnPropNameInTable
                    );

                    bInitExpressions = true; 
                    initExpressions.Statements.Add(
                        CodeGenHelper.Assign( 
                            CodeGenHelper.Property(codeField, "Expression"), 
                            CodeGenHelper.Str(column.Expression)
                        ) 
                    );
                }
            }
 

            if (bInitExpressions) { 
                return initExpressions; 
            }
            else { 
                return null;
            }
        }
 

        private CodeMemberMethod AddTypedRowMethod() { 
            //\\ public void Add<RowClassName>(<RowClassName>  row) { 
            //\\     this.Rows.Add(row);
            //\\ } 
            CodeMemberMethod addMethod = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(void)),
                NameHandler.FixIdName("Add" + rowClassName),
                MemberAttributes.Public | MemberAttributes.Final 
            );
            addMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.Type(rowConcreteClassName), "row")); 
            addMethod.Statements.Add( 
                CodeGenHelper.MethodCall(
                    CodeGenHelper.Property(CodeGenHelper.This(), "Rows"), 
                    "Add",
                    CodeGenHelper.Argument("row")
                )
            ); 

            return addMethod; 
        } 

        private void AddTypedRowByColumnsMethods(CodeTypeDeclaration dataTableClass) { 
            //\\ public <RowClassName> Add<RowClassName>(<ColType> <ColName>[, <ColType> <ColName> ...]) {
            //\\     <RowClassName> row;
            //\\     row = ((COMPUTERRow)(this.NewRow()));
            //\\     row.ItemArray = new Object[] {NAME, VERSION, null}; 
            //\\     this.Rows.Add(row);
            //\\     return row; 
            //\\ } 
            DataTable table = designTable.DataTable;
            ArrayList parameterColumnList = new ArrayList(); 
            bool needOverloadWithoutExpressionColumns = false;
            for (int i = 0; i < table.Columns.Count; i++) {
                if (!table.Columns[i].AutoIncrement) {
                    parameterColumnList.Add(table.Columns[i]); 

                    if (!StringUtil.Empty(table.Columns[i].Expression)) { 
                        needOverloadWithoutExpressionColumns = true; 
                    }
                } 
            }


            string methodName = NameHandler.FixIdName("Add" + rowClassName); 
            GenericNameHandler nameHandler = new GenericNameHandler(new string[] { methodName, columnValuesArrayName }, this.codeGenerator.CodeProvider);
 
            CodeMemberMethod addByColName = CodeGenHelper.MethodDecl( 
                CodeGenHelper.Type(rowConcreteClassName),
                methodName, 
                MemberAttributes.Public | MemberAttributes.Final
            );
            CodeMemberMethod addByColNameNoExpressionColumns = CodeGenHelper.MethodDecl(
                CodeGenHelper.Type(rowConcreteClassName), 
                methodName,
                MemberAttributes.Public | MemberAttributes.Final 
            ); 

            DataColumn[] index = new DataColumn[parameterColumnList.Count]; 
            parameterColumnList.CopyTo(index, 0);

            for (int i = 0; i < index.Length; i++) {
                Type dataType = index[i].DataType; 
                DataRelation relation = FindParentRelation(index[i]);
                if(ChildRelationFollowable(relation)) { 
                    string parentTypedRowName = codeGenerator.TableHandler.Tables[relation.ParentTable.TableName].GeneratorRowClassName; 
                    string argumentName = NameHandler.FixIdName("parent" + parentTypedRowName + "By" + relation.RelationName);
                    addByColName.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.Type(parentTypedRowName), nameHandler.AddNameToList(argumentName))); 
                    addByColNameNoExpressionColumns.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.Type(parentTypedRowName), nameHandler.GetNameFromList(argumentName)));
                }
                else {
                    addByColName.Parameters.Add( 
                        CodeGenHelper.ParameterDecl(
                            CodeGenHelper.Type(dataType), 
                            nameHandler.AddNameToList(codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow) 
                        )
                    ); 
                    if (StringUtil.Empty(index[i].Expression)) {
                        addByColNameNoExpressionColumns.Parameters.Add(
                            CodeGenHelper.ParameterDecl(
                                CodeGenHelper.Type(dataType), 
                                nameHandler.GetNameFromList(codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow)
                            ) 
                        ); 
                    }
                } 
            }

            CodeStatement rowVariableDeclaration = CodeGenHelper.VariableDecl(
                CodeGenHelper.Type(rowConcreteClassName), 
                NameHandler.FixIdName("row" + rowClassName),
                CodeGenHelper.Cast( 
                    CodeGenHelper.Type(rowConcreteClassName), 
                    CodeGenHelper.MethodCall(CodeGenHelper.This(), "NewRow")
                ) 
            );
            addByColName.Statements.Add(rowVariableDeclaration);
            addByColNameNoExpressionColumns.Statements.Add(rowVariableDeclaration);
 
            CodeExpression varRow = CodeGenHelper.Variable(NameHandler.FixIdName("row" + rowClassName));
            CodeAssignStatement assignStmt = new CodeAssignStatement(); 
            assignStmt.Left = CodeGenHelper.Property(varRow, "ItemArray"); 
            CodeArrayCreateExpression newArray = new CodeArrayCreateExpression();
            newArray.CreateType = CodeGenHelper.GlobalType(typeof(object)); 
            CodeArrayCreateExpression newArrayNoExpressionColumns = new CodeArrayCreateExpression();
            newArrayNoExpressionColumns.CreateType = CodeGenHelper.GlobalType(typeof(object));

            index = new DataColumn[table.Columns.Count]; 
            table.Columns.CopyTo(index, 0);
 
            for (int i = 0; i < index.Length; i++) { 
                if (index[i].AutoIncrement) {
                    newArray.Initializers.Add(CodeGenHelper.Primitive(null)); 
                    newArrayNoExpressionColumns.Initializers.Add(CodeGenHelper.Primitive(null));
                }
                else {
                    DataRelation relation = FindParentRelation(index[i]); 
                    if (ChildRelationFollowable(relation)) {
                        newArray.Initializers.Add(CodeGenHelper.Primitive(null)); 
                        newArrayNoExpressionColumns.Initializers.Add(CodeGenHelper.Primitive(null)); 
                    }
                    else { 
                        newArray.Initializers.Add(
                            CodeGenHelper.Argument(
                                nameHandler.GetNameFromList(codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow)
                            ) 
                        );
                        if (StringUtil.Empty(index[i].Expression)) { 
                            newArrayNoExpressionColumns.Initializers.Add( 
                                CodeGenHelper.Argument(
                                    nameHandler.GetNameFromList(codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow) 
                                )
                            );
                        }
                        else { 
                            newArrayNoExpressionColumns.Initializers.Add(CodeGenHelper.Primitive(null));
                        } 
                    } 
                }
            } 

            addByColName.Statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(object), 1), columnValuesArrayName, newArray));
            addByColNameNoExpressionColumns.Statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(object), 1), columnValuesArrayName, newArrayNoExpressionColumns));
 
            for (int i = 0; i < index.Length; i++) {
                if (!index[i].AutoIncrement) { 
                    DataRelation relation = FindParentRelation(index[i]); 
                    if (ChildRelationFollowable(relation)) {
                        string parentTypedRowName = codeGenerator.TableHandler.Tables[relation.ParentTable.TableName].GeneratorRowClassName; 
                        string argumentName = NameHandler.FixIdName("parent" + parentTypedRowName + "By" + relation.RelationName);

                        CodeStatement ifStatement = CodeGenHelper.If(
                            CodeGenHelper.IdNotEQ( 
                                CodeGenHelper.Argument(nameHandler.GetNameFromList(argumentName)),
                                CodeGenHelper.Primitive(null) 
                            ), 
                            CodeGenHelper.Assign(
                                CodeGenHelper.Indexer( 
                                    CodeGenHelper.Variable(columnValuesArrayName),
                                    CodeGenHelper.Primitive(i)
                                ),
                                CodeGenHelper.Indexer( 
                                    CodeGenHelper.Argument(nameHandler.GetNameFromList(argumentName)),
                                    CodeGenHelper.Primitive(relation.ParentColumns[0].Ordinal) 
                                ) 
                            )
                        ); 

                        addByColName.Statements.Add(ifStatement);
                        addByColNameNoExpressionColumns.Statements.Add(ifStatement);
                    } 
                }
            } 
 
            assignStmt.Right = CodeGenHelper.Variable(columnValuesArrayName);
            addByColName.Statements.Add(assignStmt); 
            addByColNameNoExpressionColumns.Statements.Add(assignStmt);

            CodeExpression methodCall = CodeGenHelper.MethodCall(
                    CodeGenHelper.Property(CodeGenHelper.This(), "Rows"), 
                    "Add",
                    varRow 
            ); 

            addByColName.Statements.Add(methodCall); 
            addByColNameNoExpressionColumns.Statements.Add(methodCall);

            addByColName.Statements.Add(CodeGenHelper.Return(varRow));
            addByColNameNoExpressionColumns.Statements.Add(CodeGenHelper.Return(varRow)); 

            dataTableClass.Members.Add(addByColName); 
            if (needOverloadWithoutExpressionColumns) { 
                dataTableClass.Members.Add(addByColNameNoExpressionColumns);
            } 
        }

        private void AddFindByMethods( CodeTypeDeclaration dataTableClass ) {
            DataTable table = designTable.DataTable; 

            for (int j = 0; j < table.Constraints.Count; j++) { 
                if (!(table.Constraints[j] is UniqueConstraint)) { 
                    continue;
                } 

                if (!(((UniqueConstraint)(table.Constraints[j])).IsPrimaryKey)) {
                    continue;
                } 

                DataColumn[] index = ((UniqueConstraint)table.Constraints[j]).Columns; 
                string findByName = "FindBy"; 
                bool allHidden = true;
                for (int i = 0; i < index.Length; i++) { 
                    findByName += codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow;
                    if(index[i].ColumnMapping != MappingType.Hidden) {
                        allHidden = false;
                    } 
                }
 
                if(allHidden) { 
                    continue; // We are not generating FindBy* methods for hidden columns
                } 

                //\\ public <RowClassName> FindBy<ColName>[...](<ColType> <ColName>[, ...]) {
                //\\    return (<RowClassName>)(this.Rows.Find(new Object[] {<ColName>[, ...]}));
                //\\ } 
                CodeMemberMethod findBy = CodeGenHelper.MethodDecl(
                    CodeGenHelper.Type(rowClassName), 
                    NameHandler.FixIdName(findByName), 
                    MemberAttributes.Public | MemberAttributes.Final
                ); 
                for (int i = 0; i < index.Length; i++) {
                    findBy.Parameters.Add(
                        CodeGenHelper.ParameterDecl(
                            CodeGenHelper.Type(index[i].DataType), 
                        codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow
                        ) 
                    ); 
                }
 
                CodeArrayCreateExpression arrayCreate = new CodeArrayCreateExpression(typeof(object), index.Length);
                for (int i = 0; i < index.Length; i++) {
                    arrayCreate.Initializers.Add(
                        CodeGenHelper.Argument( 
                        codeGenerator.TableHandler.Tables[index[i].Table.TableName].DesignColumns[index[i].ColumnName].GeneratorColumnPropNameInRow
                        ) 
                    ); 
                }
                findBy.Statements.Add( 
                    CodeGenHelper.Return(
                        CodeGenHelper.Cast(
                            CodeGenHelper.Type(rowClassName),
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Property(CodeGenHelper.This(), "Rows"),
                                "Find", 
                                arrayCreate 
                            )
                        ) 
                    )
                );

                dataTableClass.Members.Add(findBy); 
            }
 
        } 

        private CodeMemberMethod GetEnumeratorMethod() { 
            CodeMemberMethod getEnumerator = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(System.Collections.IEnumerator)),
                "GetEnumerator",
                MemberAttributes.Public 
            );
            getEnumerator.ImplementationTypes.Add(CodeGenHelper.GlobalType(typeof(System.Collections.IEnumerable))); 
            getEnumerator.Statements.Add( 
                CodeGenHelper.Return(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Rows"),
                        "GetEnumerator"
                    )
                ) 
            );
 
            return getEnumerator; 
        }
 
        private CodeMemberMethod CloneMethod() {
            //\\ public override DataTable Clone() {
            //\\     <TableClassName> cln = (<TableClassName)base.Clone();
            //\\     cln.InitVars(); 
            //\\     return cln;
            //\\ } 
            CodeMemberMethod clone = CodeGenHelper.MethodDecl( 
                CodeGenHelper.GlobalType(typeof(System.Data.DataTable)),
                "Clone", 
                MemberAttributes.Public | MemberAttributes.Override
            );
            clone.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.Type(tableClassName),
                    "cln", 
                    CodeGenHelper.Cast( 
                        CodeGenHelper.Type(tableClassName),
                        CodeGenHelper.MethodCall(CodeGenHelper.Base(), "Clone", new CodeExpression[] {}) 
                    )
                )
            );
            clone.Statements.Add( 
                CodeGenHelper.MethodCall(
                    CodeGenHelper.Variable("cln"), 
                    "InitVars", 
                    new CodeExpression[] {}
                ) 
            );
            clone.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable("cln")));

            return clone; 
        }
 
        private CodeMemberMethod CreateInstanceMethod() { 
            //\\ protected override DataTable CreateInstance() {
            //\\     return new <TableClassName>() 
            //\\ }
            CodeMemberMethod createInstance = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(System.Data.DataTable)),
                "CreateInstance", 
                MemberAttributes.Family | MemberAttributes.Override
            ); 
            createInstance.Statements.Add( 
                CodeGenHelper.Return(
                    CodeGenHelper.New(CodeGenHelper.Type(tableClassName), new CodeExpression[] {}) 
                )
            );

            return createInstance; 
        }
 
        private void InitClassAndInitVarsMethods(CodeTypeDeclaration tableClass, out CodeMemberMethod tableInitClass, out CodeMemberMethod tableInitVars) { 
            DataTable table = designTable.DataTable;
            //\\ private void InitClass() ... 
            tableInitClass = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitClass", MemberAttributes.Private);

            //\\ public void InitVars() ...
            tableInitVars = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitVars", MemberAttributes.Assembly | MemberAttributes.Final); 

            for (int i = 0; i < table.Columns.Count; i++) { 
                DataColumn column = table.Columns[i]; 
                string columnName = codeGenerator.TableHandler.Tables[table.TableName].DesignColumns[column.ColumnName].GeneratorColumnVarNameInTable;
                //\\ this.column<ColumnName> 
                CodeExpression codeField = CodeGenHelper.Field(CodeGenHelper.This(), columnName);

                //\\ this.column<ColumnName> = new DataColumn("<ColumnName>", typeof(<ColumnType>), "", MappingType.Hidden);
                string mappingType = "Element"; 
                if(column.ColumnMapping == MappingType.SimpleContent) {
                    mappingType = "SimpleContent"; 
                } 
                else if(column.ColumnMapping == MappingType.Attribute) {
                    mappingType = "Attribute"; 
                }
                else if(column.ColumnMapping == MappingType.Hidden) {
                    mappingType = "Hidden";
                } 
                tableInitClass.Statements.Add(
                    CodeGenHelper.Assign( 
                        codeField, 
                        CodeGenHelper.New(
                            CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), 
                            new CodeExpression[] {
                                CodeGenHelper.Str(column.ColumnName),
                                CodeGenHelper.TypeOf(CodeGenHelper.GlobalType(column.DataType)),
                                CodeGenHelper.Primitive(null), 
                                CodeGenHelper.Field(
                                    CodeGenHelper.GlobalTypeExpr(typeof(MappingType)), 
                                    mappingType 
                                )
                            } 
                        )
                ));

                // Add extended properties to the DataColumn, if there are any in the schema file 
                ExtendedPropertiesHandler.CodeGenerator = codeGenerator;
                ExtendedPropertiesHandler.AddExtendedProperties(designTable.DesignColumns[column.ColumnName], codeField, tableInitClass.Statements, column.ExtendedProperties); 
 
                //\\ this.Columns.Add(this.<ColumnName>);
                tableInitClass.Statements.Add( 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Base(), "Columns"),
                        "Add",
                        CodeGenHelper.Field(CodeGenHelper.This(), columnName) 
                    )
                ); 
            } 

            for (int i = 0; i < table.Constraints.Count; i++) { 
                if (!(table.Constraints[i] is UniqueConstraint)) {
                    continue;
                }
                //\\ this.Constraints.Add = new UniqueConstraint(<constraintName>, new DataColumn[] {this.column<ColumnName> [, ...]}); 
                UniqueConstraint uc = (UniqueConstraint)(table.Constraints[i]);
                DataColumn[] columns = uc.Columns; 
                CodeExpression[] createArgs = new CodeExpression[columns.Length]; 
                for (int j = 0; j < columns.Length; j++) {
                    createArgs[j] = CodeGenHelper.Field( 
                        CodeGenHelper.This(),
                        codeGenerator.TableHandler.Tables[columns[j].Table.TableName].DesignColumns[columns[j].ColumnName].GeneratorColumnVarNameInTable
                    );
                } 
                tableInitClass.Statements.Add(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Constraints"), 
                        "Add",
                        CodeGenHelper.New( 
                            CodeGenHelper.GlobalType(typeof(System.Data.UniqueConstraint)),
                            new CodeExpression[] {
                                CodeGenHelper.Str(uc.ConstraintName),
                                new CodeArrayCreateExpression(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), createArgs), 
                                CodeGenHelper.Primitive(uc.IsPrimaryKey)
                            } 
                        ) 
                    )
                ); 
            }

            for (int i = 0; i < table.Columns.Count; i++) {
                DataColumn column = table.Columns[i]; 
                string columnName = codeGenerator.TableHandler.Tables[table.TableName].DesignColumns[column.ColumnName].GeneratorColumnVarNameInTable;
                //\\ this.<ColumnVariableName> 
                CodeExpression codeField = CodeGenHelper.Field(CodeGenHelper.This(), columnName); 

                //\\ this.<ColumnVariableName> = this.Columns["<ColumnName>"]; 
                tableInitVars.Statements.Add(
                    CodeGenHelper.Assign(
                        codeField,
                        CodeGenHelper.Indexer( 
                            CodeGenHelper.Property(CodeGenHelper.Base(), "Columns"),
                            CodeGenHelper.Str(column.ColumnName) 
                        ) 
                    )
                ); 

                if (column.AutoIncrement) {
                    //\\ this.<ColumnVariableName>.AutoIncrement = true;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "AutoIncrement"), 
                            CodeGenHelper.Primitive(true) 
                        )
                    ); 
                }
                if (column.AutoIncrementSeed != 0) {
                    //\\ this.<ColumnVariableName>.AutoIncrementSeed = <column.AutoIncrementSeed>;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "AutoIncrementSeed"), 
                            CodeGenHelper.Primitive(column.AutoIncrementSeed) 
                        )
                    ); 
                }
                if (column.AutoIncrementStep != 1) {
                    //\\ this.<ColumnVariableName>.AutoIncrementStep = <column.AutoIncrementStep>;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "AutoIncrementStep"), 
                            CodeGenHelper.Primitive(column.AutoIncrementStep) 
                        )
                    ); 
                }
                if (!column.AllowDBNull) {
                    //\\ this.<ColumnVariableName>.AllowDBNull = false;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "AllowDBNull"), 
                            CodeGenHelper.Primitive(false) 
                        )
                    ); 
                }
                if (column.ReadOnly) {
                    //\\ this.<ColumnVariableName>.ReadOnly = true;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "ReadOnly"), 
                            CodeGenHelper.Primitive(true) 
                        )
                    ); 
                }
                if (column.Unique) {
                    //\\ this.<ColumnVariableName>.Unique = true;
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "Unique"), 
                            CodeGenHelper.Primitive(true) 
                        )
                    ); 
                }
                if (!StringUtil.Empty(column.Prefix)) {
                    //\\ this.<ColumnVariableName>.Prefix = "<column.Prefix>";
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "Prefix"), 
                            CodeGenHelper.Str(column.Prefix) 
                        )
                    ); 
                }
                if(columnNamespaceProperty.ShouldSerializeValue(column)) {
                    //\\ this.<ColumnVariableName>.Namespace = "<column.Namespace>";
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "Namespace"), 
                            CodeGenHelper.Str(column.Namespace) 
                        )
                    ); 
                }
                if (column.Caption != column.ColumnName) {
                    //\\ this.<ColumnVariableName>.Caption = "<column.Caption>";
                    tableInitClass.Statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(codeField, "Caption"), 
                            CodeGenHelper.Str(column.Caption) 
                        )
                    ); 
                }
                if (column.DefaultValue != DBNull.Value) {
                    CodeExpression defaultValueExpr = null;
                    CodeExpression defaultValueFieldInit = null; 
                    DesignColumn designColumn = codeGenerator.TableHandler.Tables[table.TableName].DesignColumns[column.ColumnName];
 
                    DSGeneratorProblem problem = CodeGenHelper.GenerateValueExprAndFieldInit(designColumn, column.DefaultValue, column.DefaultValue, designTable.GeneratorTableClassName, columnName + "_defaultValue", out defaultValueExpr, out defaultValueFieldInit); 
                    if (problem != null) {
                        codeGenerator.ProblemList.Add(problem); 
                    }
                    else {
                        if (defaultValueFieldInit != null) {
                            CodeMemberField defaultValueField = CodeGenHelper.FieldDecl( 
                                CodeGenHelper.Type(column.DataType.FullName),
                                columnName + "_defaultValue" 
                            ); 
                            defaultValueField.Attributes = MemberAttributes.Static | MemberAttributes.Private;
                            defaultValueField.InitExpression = defaultValueFieldInit; 

                            tableClass.Members.Add(defaultValueField);
                        }
 

                        //\\ this.<ColumnVariableName>.DefaultValue = "<column.DefaultValue>"; 
                        CodeCastExpression cce = new CodeCastExpression(column.DataType, defaultValueExpr); 
                        // J# specific UserData
                        cce.UserData.Add("CastIsBoxing", true); 

                        tableInitClass.Statements.Add(
                            CodeGenHelper.Assign(
                                CodeGenHelper.Property(codeField, "DefaultValue"), 
                                cce
                            ) 
                        ); 
                    }
                } 
                if (column.MaxLength != -1) {
                    //\\ this.<ColumnVariableName>.MaxLength = "<column.MaxLength>";
                    tableInitClass.Statements.Add(
                        CodeGenHelper.Assign( 
                            CodeGenHelper.Property(codeField, "MaxLength"),
                            CodeGenHelper.Primitive(column.MaxLength) 
                        ) 
                    );
                } 
                if (column.DateTimeMode != DataSetDateTime.UnspecifiedLocal) {
                    //\\ this.<ColumnVariableName>.DateTimeMode = "<column.DateTimeMode>";
                    tableInitClass.Statements.Add(
                        CodeGenHelper.Assign( 
                            CodeGenHelper.Property(codeField, "DateTimeMode"),
                            CodeGenHelper.Field( 
                                CodeGenHelper.GlobalTypeExpr(typeof(DataSetDateTime)), 
                                column.DateTimeMode.ToString()
                            ) 
                        )
                    );
                }
            } 

            if (caseSensitiveProperty.ShouldSerializeValue(table)) { 
                //\\ this.CaseSensitive = <CaseSensitive>; 
                tableInitClass.Statements.Add(
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "CaseSensitive"),
                        CodeGenHelper.Primitive(table.CaseSensitive)
                    )
                ); 
            }
 
            CultureInfo culture = table.Locale; 
            if (culture != null) {
                if (localeProperty.ShouldSerializeValue(table)) { 
                    //\\ this.Locale = new System.Globalization.CultureInfo("<Locale>");
                    tableInitClass.Statements.Add(
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(CodeGenHelper.This(), "Locale"), 
                            CodeGenHelper.New(
                                CodeGenHelper.GlobalType(typeof(System.Globalization.CultureInfo)), 
                                new CodeExpression[] { CodeGenHelper.Str(table.Locale.ToString()) } 
                            )
                        ) 
                    );
                }
            }
            if (!StringUtil.Empty(table.Prefix)) { 
                //\\ this.Prefix = "<Prefix>";
                tableInitClass.Statements.Add( 
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Prefix"),
                        CodeGenHelper.Str(table.Prefix) 
                    )
                );
            }
            if(namespaceProperty.ShouldSerializeValue(table)) { 
                //\\ this.Namespace = <Namespace>;
                tableInitClass.Statements.Add( 
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Namespace"),
                        CodeGenHelper.Str(table.Namespace) 
                    )
                );
            }
            if (table.MinimumCapacity != 50) { 
                //\\ this.MinimumCapacity = <MinimumCapacity>;
                tableInitClass.Statements.Add( 
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "MinimumCapacity"),
                        CodeGenHelper.Primitive(table.MinimumCapacity) 
                    )
                );
            }
 
            // Add extended properties to the DataTable, if there are any in the schema file
            ExtendedPropertiesHandler.CodeGenerator = codeGenerator; 
            ExtendedPropertiesHandler.AddExtendedProperties(designTable, CodeGenHelper.This(), tableInitClass.Statements, table.ExtendedProperties); 
        }
 
        private CodeMemberMethod NewTypedRowMethod() {
            //\\ public <RowClassName> New<RowClassName>() {
            //\\     return (<RowClassName>) NewRow();
            //\\ } 
            CodeMemberMethod newTableRow = CodeGenHelper.MethodDecl(
                CodeGenHelper.Type(rowConcreteClassName), 
                NameHandler.FixIdName("New" + rowClassName), 
                MemberAttributes.Public | MemberAttributes.Final
            ); 
            newTableRow.Statements.Add(
                CodeGenHelper.Return(
                    CodeGenHelper.Cast(
                        CodeGenHelper.Type(rowConcreteClassName), 
                        CodeGenHelper.MethodCall(CodeGenHelper.This(), "NewRow")
                    ) 
                ) 
            );
 
            return newTableRow;
        }

        private CodeMemberMethod NewRowFromBuilderMethod() { 
            //\\ protected override DataRow NewRowFromBuilder(DataRowBuilder builder) {
            //\\     return new<RowClassName>(builder); 
            //\\ } 
            CodeMemberMethod newRowFromBuilder = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(System.Data.DataRow)), 
                "NewRowFromBuilder",
                MemberAttributes.Family | MemberAttributes.Override
            );
            newRowFromBuilder.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRowBuilder)), "builder")); 
            newRowFromBuilder.Statements.Add(
                CodeGenHelper.Return( 
                    CodeGenHelper.New( 
                        CodeGenHelper.Type(rowConcreteClassName),
                        new CodeExpression[] {CodeGenHelper.Argument("builder")} 
                    )
                )
            );
 
            return newRowFromBuilder;
        } 
 
        private CodeMemberMethod GetRowTypeMethod() {
            //\\ protected override System.Type GetRowType() { 
            //\\     return typeof(<RowConcreateClassName>);
            //\\ }
            CodeMemberMethod getRowType = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(System.Type)), 
                "GetRowType",
                MemberAttributes.Family | MemberAttributes.Override 
            ); 
            getRowType.Statements.Add(CodeGenHelper.Return(CodeGenHelper.TypeOf(CodeGenHelper.Type(rowConcreteClassName))));
 
            return getRowType;
        }

        private CodeMemberMethod CreateOnRowEventMethod(string eventName, string typedEventName) { 
            //\\ protected override void OnRow<eventName>(DataRowChangeEventArgs e) {
            //\\     base.OnRow<eventName>(e); 
            //\\     if (((this.<typedEventName>) != (null))) { 
            //\\         this.<typedEventName>(this, new <TypedRowEvArgName>(((<eventName>)(e.Row)), e.Action));
            //\\     } 
            //\\ }
            CodeMemberMethod onRowEvent = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(void)),
                "OnRow" + eventName, 
                MemberAttributes.Family | MemberAttributes.Override
            ); 
            onRowEvent.Parameters.Add( 
                CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRowChangeEventArgs)), "e")
            ); 
            onRowEvent.Statements.Add(
                CodeGenHelper.MethodCall(
                    CodeGenHelper.Base(),
                    "OnRow" + eventName, 
                    CodeGenHelper.Argument("e")
                ) 
            ); 
            onRowEvent.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Event(typedEventName),
                        CodeGenHelper.Primitive(null)
                    ), 
                    CodeGenHelper.Stm(
                        CodeGenHelper.DelegateCall( 
                            CodeGenHelper.Event(typedEventName), 
                            CodeGenHelper.New(
                                CodeGenHelper.Type(designTable.GeneratorRowEvArgName), 
                                new CodeExpression[] {
                                    CodeGenHelper.Cast(
                                        CodeGenHelper.Type(rowClassName),
                                        CodeGenHelper.Property(CodeGenHelper.Argument("e"), "Row") 
                                    ),
                                    CodeGenHelper.Property(CodeGenHelper.Argument("e"), "Action") 
                                } 
                            )
                        ) 
                    )
                )
            );
 
            return onRowEvent;
        }// CreateOnRowEventMethod 
 

        private void AddOnRowEventMethods(CodeTypeDeclaration dataTableClass) { 
            dataTableClass.Members.Add(CreateOnRowEventMethod("Changed", this.designTable.GeneratorRowChangedName));
            dataTableClass.Members.Add(CreateOnRowEventMethod("Changing", this.designTable.GeneratorRowChangingName));
            dataTableClass.Members.Add(CreateOnRowEventMethod("Deleted", this.designTable.GeneratorRowDeletedName));
            dataTableClass.Members.Add(CreateOnRowEventMethod("Deleting", this.designTable.GeneratorRowDeletingName)); 
        }
 
        private CodeMemberMethod RemoveRowMethod() { 
            //\\ public void Remove<RowClassName>(<RowClassName> row) {
            //\\     this.Rows.Remove(row); 
            //\\ }
            CodeMemberMethod removeMethod = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(void)),
                NameHandler.FixIdName("Remove" + rowClassName), 
                MemberAttributes.Public | MemberAttributes.Final
            ); 
            removeMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.Type(rowConcreteClassName), "row")); 
            removeMethod.Statements.Add(
                CodeGenHelper.MethodCall( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "Rows"),
                    "Remove",
                    CodeGenHelper.Argument("row")
                ) 
            );
 
            return removeMethod; 
        }
 
        private bool ChildRelationFollowable(DataRelation relation) {
            if (relation != null) {
                if (relation.ChildTable == relation.ParentTable) {
                    if (relation.ChildTable.Columns.Count == 1) { 
                        return false;
                    } 
                } 
                return true;
            } 
            return false;
        }

        private DataRelation FindParentRelation(DataColumn column) { 
            DataRelation[] parentRelations = new DataRelation[column.Table.ParentRelations.Count];
            column.Table.ParentRelations.CopyTo(parentRelations, 0); 
 
            for (int i = 0; i < parentRelations.Length; i++) {
                DataRelation relation = parentRelations[i]; 
                if (relation.ChildColumns.Length == 1 && relation.ChildColumns[0] == column) {
                    return relation;
                }
            } 
            // should we throw an exception?
            return null; 
        } 

        private CodeMemberMethod GetTypedTableSchema() { 
            //\\ public static XmlSchemaComplexType GetTypedTableSchema(XmlSchemaSet xs) {
            //\\    XmlSchemaComplexType type = new XmlSchemaComplexType();
            //\\    XmlSchemaSequence sequence = new XmlSchemaSequence();
            //\\    NorthwindDataSet ds = new NorthwindDataSet(); 
            //\\    XmlSchemaAny any1 = new XmlSchemaAny();
            //\\    any1.Namespace = XmlSchema.Namespace; 
            //\\    any1.MinOccurs = 0; 
            //\\    any1.MaxOccurs = Decimal.MaxValue;
            //\\    any1.ProcessContents = XmlSchemaContentProcessing.Lax; 
            //\\    sequence.Items.Add(any1);
            //\\    XmlSchemaAny any2 = new XmlSchemaAny();
            //\\    any2.Namespace = Keywords.DFFNS;
            //\\    any2.MinOccurs = 1; 
            //\\    any2.ProcessContents = XmlSchemaContentProcessing.Lax;
            //\\    sequence.Items.Add(any2); 
            //\\    XmlSchemaAttribute attribute1 = new XmlSchemaAttribute(); 
            //\\    attribute1.Name = "namespace";
            //\\    attribute1.FixedValue = ds.Namespace; 
            //\\    type.Attributes.Add(attribute1);
            //\\    XmlSchemaAttribute attribute2 = new XmlSchemaAttribute();
            //\\    attribute2.Name = "tableTypeName";
            //\\    attribute2.FixedValue = "CustomersDataTable"; 
            //\\    type.Attributes.Add(attribute2);
            //\\    type.Particle = sequence; 
            //\\    XmlSchema dsSchema = ds.GetSchemaSerializable(); 
            //\\    if (xs.Contains(dsSchema.TargetNamespace)) {
            //\\        MemoryStream s1 = new MemoryStream(); 
            //\\        MemoryStream s2 = new MemoryStream();
            //\\        try {
            //\\            XmlSchema schema = null;
            //\\            dsSchema.Write(s1); 
            //\\            for (IEnumerator schemas = xs.Schemas(dsSchema.TargetNamespace).GetEnumerator(); schemas.MoveNext(); ) {
            //\\                schema = (XmlSchema)schemas.Current; 
            //\\                s2.SetLength(0); 
            //\\                schema.Write(s2);
            //\\                if ((s1.Length == s2.Length)) { 
            //\\                    s1.Position = 0;
            //\\                    s2.Position = 0;
            //\\                    for (; ((s1.Position != s1.Length) && (s1.ReadByte() == s2.ReadByte())); ) { }
            //\\                    if ((s1.Position == s1.Length)) { 
            //\\                       return type;
            //\\                    } 
            //\\                } 
            //\\            }
            //\\        } 
            //\\        finally {
            //\\            if (s1 != null) {
            //\\                s1.Dispose();
            //\\            } 
            //\\            if (s2 != null) {
            //\\                s2.Dispose(); 
            //\\            } 
            //\\        }
            //\\    } 
            //\\    xs.Add(dsSchema);
            //\\    return type;
            //\\ }
 
            CodeMemberMethod getTableSchema = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)), "GetTypedTableSchema", MemberAttributes.Static | MemberAttributes.Public);
            getTableSchema.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(XmlSchemaSet)), "xs")); 
 
            getTableSchema.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)),
                    "type",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)), new CodeExpression[] {})
                ) 
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaSequence)),
                    "sequence", 
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaSequence)), new CodeExpression[] {})
                )
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.Type(codeGenerator.DataSourceName), 
                    "ds", 
                    CodeGenHelper.New(
                        CodeGenHelper.Type(codeGenerator.DataSourceName), 
                        new CodeExpression[] {}
                    )
                )
            ); 

            getTableSchema.Statements.Add( 
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaAny)),
                    "any1", 
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaAny)), new CodeExpression[] {})
                )
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any1"), "Namespace"), 
                    CodeGenHelper.Str(XmlSchema.Namespace) 
                )
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any1"), "MinOccurs"),
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Decimal)), new CodeExpression[] { CodeGenHelper.Primitive(0) }) 
                )
            ); 
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any1"), "MaxOccurs"), 
                    CodeGenHelper.Field(
                        CodeGenHelper.GlobalTypeExpr(typeof(System.Decimal)),
                        "MaxValue"
                    ) 
                )
            ); 
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any1"), "ProcessContents"), 
                    CodeGenHelper.Field(
                        CodeGenHelper.GlobalTypeExpr(typeof(XmlSchemaContentProcessing)),
                        "Lax"
                    ) 
                )
            ); 
            getTableSchema.Statements.Add( 
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Property(CodeGenHelper.Variable("sequence"), "Items"),
                        "Add",
                        new CodeExpression[] { CodeGenHelper.Variable("any1") }
                    ) 
                )
            ); 
            getTableSchema.Statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(XmlSchemaAny)), 
                    "any2",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaAny)), new CodeExpression[] {})
                )
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.Variable("any2"), "Namespace"), 
                    CodeGenHelper.Str(Keywords.DFFNS)
                ) 
            );
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any2"), "MinOccurs"), 
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Decimal)), new CodeExpression[] { CodeGenHelper.Primitive(1) })
                ) 
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.Variable("any2"), "ProcessContents"),
                    CodeGenHelper.Field(
                        CodeGenHelper.GlobalTypeExpr(typeof(XmlSchemaContentProcessing)),
                        "Lax" 
                    )
                ) 
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Stm( 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Variable("sequence"), "Items"),
                        "Add",
                        new CodeExpression[] { CodeGenHelper.Variable("any2") } 
                    )
                ) 
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaAttribute)),
                    "attribute1",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaAttribute)), new CodeExpression[] {})
                ) 
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.Variable("attribute1"), "Name"),
                    CodeGenHelper.Primitive("namespace") 
                )
            );
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.Variable("attribute1"), "FixedValue"),
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "Namespace") 
                ) 
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Variable("type"), "Attributes"),
                        "Add", 
                        new CodeExpression[] { CodeGenHelper.Variable("attribute1") }
                    ) 
                ) 
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(XmlSchemaAttribute)),
                    "attribute2",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaAttribute)), new CodeExpression[] {}) 
                )
            ); 
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("attribute2"), "Name"), 
                    CodeGenHelper.Primitive("tableTypeName")
                )
            );
            getTableSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("attribute2"), "FixedValue"), 
                    CodeGenHelper.Str(designTable.GeneratorTableClassName) 
                )
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Variable("type"), "Attributes"), 
                        "Add",
                        new CodeExpression[] { CodeGenHelper.Variable("attribute2") } 
                    ) 
                )
            ); 
            getTableSchema.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("type"), "Particle"),
                    CodeGenHelper.Variable("sequence") 
                )
            ); 
 
            // DDBugs 126260: Avoid adding the same schema twice
            DatasetMethodGenerator.GetSchemaIsInCollection(getTableSchema.Statements, "ds", "xs"); 

            getTableSchema.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable("type")));

            return getTableSchema; 
        }
 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
