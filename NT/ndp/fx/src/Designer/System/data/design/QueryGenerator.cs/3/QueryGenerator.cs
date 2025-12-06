//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
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
    using System.Data.Common;
    using System.Data.SqlClient; 
    using System.Data.SqlTypes; 
    using System.Data.Odbc;
    using System.Data.OleDb; 
    using System.Data.OracleClient;
    using System.Design;

 
    internal class QueryGenerator : QueryGeneratorBase {
        internal QueryGenerator(TypedDataSourceCodeGenerator codeGenerator) : base(codeGenerator) {} 
 
        internal override CodeMemberMethod Generate() {
            if(this.methodSource == null) { 
                throw new InternalException("MethodSource should not be null.");
            }
            if (StringUtil.Empty(this.ContainerParameterName)) {
                throw new InternalException("ContainerParameterName should not be empty."); 
            }
 
            // get select command 
            if(methodSource.SelectCommand == null) {
                codeGenerator.ProblemList.Add( new DSGeneratorProblem(SR.GetString(SR.CG_MainSelectCommandNotSet, this.DesignTable.Name), ProblemSeverity.NonFatalError, this.methodSource) ); 
                return null;
            }
            else {
                this.activeCommand = this.methodSource.SelectCommand; 
            }
 
            // get attributes 
            this.methodAttributes = MemberAttributes.Overloaded;
            if(this.getMethod) { 
                this.methodAttributes |= this.MethodSource.GetMethodModifier;
            }
            else {
                this.methodAttributes |= this.MethodSource.Modifier; 
            }
 
            if(this.codeProvider == null) { 
                this.codeProvider = this.codeGenerator.CodeProvider;
            } 
            // init the namehandler with the list of reserved strings
            this.nameHandler = new GenericNameHandler(new string[] {this.MethodName, returnVariableName}, this.codeProvider);

            // do the actual generation 
            return GenerateInternal();
        } 
 
        private CodeMemberMethod GenerateInternal() {
            // get return type 
            //DesignParameter returnParameter = GetReturnParameter(this.activeCommand);
            //if (returnParameter != null) {
            //    this.returnType = GetParameterUrtType(returnParameter);
            //} 
            //else {
            //    this.returnType = typeof(int); 
            //} 

            // always return the return value of Fill method, ignore eventual return parameter 
            this.returnType = typeof(int);

            // create the method declaration
            CodeMemberMethod dbMethod = null; 
            if(this.getMethod) {
                dbMethod = CodeGenHelper.MethodDecl(CodeGenHelper.Type(this.ContainerParameterTypeName), this.MethodName, this.methodAttributes); 
            } 
            else {
                //if (returnParameter != null && returnParameter.AllowDbNull && this.returnType.IsValueType) { 
                //    dbMethod = CodeGenHelper.MethodDecl(CodeGenHelper.NullableType(this.returnType), this.MethodName, this.methodAttributes);
                //}
                //else {
                dbMethod = CodeGenHelper.MethodDecl(CodeGenHelper.Type(this.returnType), this.MethodName, this.methodAttributes); 
                //}
            } 
 
            // add help keyword attribute to method
            dbMethod.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.Design.HelpKeywordAttribute).FullName, CodeGenHelper.Str("vs.data.TableAdapter"))); 

            // add parameters to the method
            AddParametersToMethod(dbMethod);
 
            if(this.declarationOnly) {
                AddThrowsClauseIfNeeded(dbMethod); 
                return dbMethod; 
            }
            else { 
                AddCustomAttributesToMethod(dbMethod);

                // add statements
                if(AddStatementsToMethod(dbMethod)) { 
                    return dbMethod;
                } 
                else { 
                    return null;
                } 
            }
        }

        private void AddParametersToMethod(CodeMemberMethod dbMethod) { 
            CodeParameterDeclarationExpression codeParam = null;
            // add container parameter to the method 
            if(!this.getMethod) { 
                string contParamName = this.nameHandler.AddNameToList(this.ContainerParameterName);
                codeParam = CodeGenHelper.ParameterDecl(CodeGenHelper.Type(this.ContainerParameterTypeName), contParamName); 
                dbMethod.Parameters.Add(codeParam);
            }

            if(this.GeneratePagingMethod) { 
                // add the startRecord and maxRecords parameters to the method declaration
                string startRecordParamName = this.nameHandler.AddNameToList(startRecordParameterName); 
                codeParam = CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(int)), startRecordParamName); 
                dbMethod.Parameters.Add(codeParam);
 
                string maxRecordsParamName = this.nameHandler.AddNameToList(maxRecordsParameterName);
                codeParam = CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(int)), maxRecordsParamName);
                dbMethod.Parameters.Add(codeParam);
            } 

            if(activeCommand.Parameters == null) { 
                return; 
            }
 
            DesignConnection connection = (DesignConnection)this.methodSource.Connection;
 			if (connection == null) {
				throw new InternalException(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Connection for query {0} is null.", this.methodSource.Name));
			} 
			string paramPrefix = connection.ParameterPrefix;
 
 			foreach(DesignParameter parameter in this.activeCommand.Parameters) { 
                if(parameter.Direction == ParameterDirection.ReturnValue) {
                    // skip over return parameter 
                    continue;
                }

                // get the type of the parameter 
                Type parameterType = this.GetParameterUrtType(parameter);
 
                // create parameter decl expression 
                string parameterName = this.nameHandler.AddParameterNameToList(parameter.ParameterName, paramPrefix);
                CodeTypeReference ctr = null; 
                if (parameter.AllowDbNull && parameterType.IsValueType) {
                    ctr = CodeGenHelper.NullableType(parameterType);
                }
                else { 
                    ctr = CodeGenHelper.Type(parameterType);
                } 
 
                codeParam = CodeGenHelper.ParameterDecl(ctr, parameterName);
 
                // set parameter direction
                codeParam.Direction = CodeGenHelper.ParameterDirectionToFieldDirection(parameter.Direction);

                // add parameter to method decl 
                dbMethod.Parameters.Add(codeParam);
            } 
        } 

        private bool AddStatementsToMethod(CodeMemberMethod dbMethod) { 
            bool succeeded = true;

            // Add statements to set SelectCommand
            succeeded = AddSetCommandStatements(dbMethod.Statements); 
            if(!succeeded) {
                return false; 
            } 

            // Add statements to set parameters on command object 
            succeeded = AddSetParametersStatements(dbMethod.Statements);
            if(!succeeded) {
                return false;
            } 

            succeeded = AddClearStatements(dbMethod.Statements); 
            if (!succeeded) { 
                return false;
            } 

            // Add statements to execute the command
            if (this.GeneratePagingMethod) {
                succeeded = AddExecuteCommandStatementsForPaging(dbMethod.Statements); 
            }
            else { 
                succeeded = AddExecuteCommandStatements(dbMethod.Statements); 
            }
            if(!succeeded) { 
                return false;
            }

            // Add statements to set output parameter values 
            succeeded = AddSetReturnParamValuesStatements(dbMethod.Statements);
            if(!succeeded) { 
                return false; 
            }
 
            // Add return statements (used by Fill methods only)
            succeeded = AddReturnStatements(dbMethod.Statements);
            if(!succeeded) {
                return false; 
            }
 
            return true; 
        }
 
        private bool AddSetCommandStatements(IList statements) {
            Type commandType = this.ProviderFactory.CreateCommand().GetType();
            statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(
                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                        "SelectCommand" 
                    ),
                    CodeGenHelper.ArrayIndexer( 
                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                        CodeGenHelper.Primitive(this.CommandIndex)
                    )
                ) 
            );
 
            return true; 
        }
 
        private bool AddSetParametersStatements(IList statements) {
            int paramCount = 0;
            if(this.activeCommand.Parameters != null) {
                paramCount = this.activeCommand.Parameters.Count; 
            }
 
 
            for(int i = 0; i < paramCount; i++) {
                DesignParameter parameter = activeCommand.Parameters[i] as DesignParameter; 
                if(parameter == null) {
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter.");
                }
 
                if(parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput) {
                    string parameterName = nameHandler.GetNameFromList(parameter.ParameterName); 
                    CodeExpression selectCmdExpression = CodeGenHelper.Property( 
                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                        "SelectCommand" 
                    );

                    AddSetParameterStatements(parameter, parameterName, selectCmdExpression, i, statements);
                } 
            }
 
            return true; 
        }
 
        private bool AddClearStatements(IList statements) {
            if (!this.getMethod) {
                CodeStatement trueStatement = null;
                if (this.containerParamType == typeof(System.Data.DataTable)) { 
                    //\\ table.Clear();
                    trueStatement = CodeGenHelper.Stm( 
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Argument(this.ContainerParameterName),
                            "Clear", 
                            new CodeExpression[] {}
                        )
                    );
                } 
                else if (this.containerParamType == typeof(System.Data.DataSet)) {
                    //\\ dataSet.<TablePropertyName>.Clear() 
                    trueStatement = CodeGenHelper.Stm( 
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Property(CodeGenHelper.Argument(this.ContainerParameterName), this.DesignTable.GeneratorTablePropName), 
                            "Clear",
                            new CodeExpression[] {}
                        )
                    ); 
                }
                else { 
                    throw new InternalException("Unknown containerParameterType."); 
                }
 
                statements.Add(
                    CodeGenHelper.If(
                        CodeGenHelper.EQ(
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ClearBeforeFillPropertyName), 
                            CodeGenHelper.Primitive(true)
                        ), 
                        trueStatement 
                    )
                ); 
            }

            return true;
        } 

        private bool AddExecuteCommandStatements(IList statements) { 
            if(this.getMethod) { 
                //\\ NorthwindDataSet.CustomersDataTable table = new NorthwindDataSet.CustomersDataTable();
                CodeExpression[] argCodeExps = new CodeExpression []{}; 
                bool hasExpressionColumn = this.designTable != null && this.designTable.HasAnyExpressionColumn;
                if (hasExpressionColumn){
                    argCodeExps = new CodeExpression []{CodeGenHelper.Primitive(true)};
                } 

                statements.Add( 
                    CodeGenHelper.VariableDecl( 
                        CodeGenHelper.Type(this.ContainerParameterTypeName),
                        this.ContainerParameterName, 
                        CodeGenHelper.New(CodeGenHelper.Type(this.ContainerParameterTypeName), argCodeExps)
                    )
                );
            } 

            //\\ this._adapter.Fill(dataSet); 
            //\\ OR 
            //\\ int retVal = this._adapter.Fill(dataSet);
            CodeExpression[] fillParameters = new CodeExpression[1]; 
            fillParameters[0] = CodeGenHelper.Variable(this.ContainerParameterName);

            if(!this.getMethod) {
                // Ignore return parameter for the command, we'll return the return value from the fill method 
                statements.Add(
                    CodeGenHelper.VariableDecl( 
                        CodeGenHelper.GlobalType(typeof(int)), 
                        returnVariableName,
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                            "Fill",
                            fillParameters
                        ) 
                    )
                ); 
            } 
            else {
                // It's a GetData() method, we'll return the DataTable 
                statements.Add(
                    CodeGenHelper.Stm(
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                            "Fill",
                            fillParameters 
                        ) 
                    )
                ); 
            }

            return true;
        } 

        private bool AddExecuteCommandStatementsForPaging(IList statements) { 
            if(this.containerParamType == typeof(System.Data.DataTable)) { 
                // we need to instantiate a new dataset
                //\\ NorthwindDataSet dataSet = new NorthwindDataSet(); 
                statements.Add(
                    CodeGenHelper.VariableDecl(
                        CodeGenHelper.Type(this.codeGenerator.DataSourceName),
                        nameHandler.AddNameToList("dataSet"), 
                        CodeGenHelper.New(CodeGenHelper.Type(this.codeGenerator.DataSourceName), new CodeExpression[] {})
                    ) 
                ); 
            }
 
            //\\ this._adapter.Fill(dataSet, startRecord, maxRecords, "Table");
            //\\ OR
            //\\ int retVal = this._adapter.Fill(dataSet, startRecord, maxRecords, "Table");
            CodeExpression[] fillParameters = new CodeExpression[4]; 
            if(this.containerParamType == typeof(System.Data.DataTable)) {
                fillParameters[0] = CodeGenHelper.Variable(this.nameHandler.GetNameFromList("dataSet")); 
            } 
            else {
                fillParameters[0] = CodeGenHelper.Argument(this.ContainerParameterName); 
            }
            fillParameters[1] = CodeGenHelper.Argument(this.nameHandler.GetNameFromList(startRecordParameterName));
            fillParameters[2] = CodeGenHelper.Argument(this.nameHandler.GetNameFromList(maxRecordsParameterName));
            fillParameters[3] = CodeGenHelper.Str("Table"); 

            if(!this.getMethod) { 
                // Ignore return parameter for the command, we'll return the return value from the fill method 
                statements.Add(
                    CodeGenHelper.VariableDecl( 
                        CodeGenHelper.GlobalType(typeof(int)),
                        returnVariableName,
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                            "Fill",
                            fillParameters 
                        ) 
                    )
                ); 
            }
            else {
                // It's a GetData() method, we'll return the DataTable
                statements.Add( 
                    CodeGenHelper.Stm(
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                            "Fill",
                            fillParameters 
                        )
                    )
                );
            } 

            if(this.containerParamType == typeof(System.Data.DataTable) && !this.getMethod) { 
                // we need to move the filled rows to the DataTable that was passed in as argument to this method 
                //\\ for(int i = 0; i < dataSet.Customers.Rows.Count; i++) {
                //\\    dataTable.ImportRow(dataSet.Customers.Rows[i]; 
                //\\ }
                CodeStatement forInit = CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(int)), "i", CodeGenHelper.Primitive(0));
                CodeExpression forTest = CodeGenHelper.Less(
                                            CodeGenHelper.Variable("i"), 
                                            CodeGenHelper.Property(
                                                CodeGenHelper.Property( 
                                                    CodeGenHelper.Property( 
                                                        CodeGenHelper.Variable(this.nameHandler.GetNameFromList("dataSet")),
                                                        this.DesignTable.GeneratorName 
                                                    ),
                                                    "Rows"
                                                ),
                                                "Count" 
                                            )
                                        ); 
                CodeStatement forIncrement = CodeGenHelper.Assign( 
                                                CodeGenHelper.Variable("i"),
                                                CodeGenHelper.BinOperator( 
                                                    CodeGenHelper.Variable("i"),
                                                    CodeBinaryOperatorType.Add,
                                                    CodeGenHelper.Primitive(1)
                                                ) 
                                            );
 
                CodeStatement forStatement = 
                    CodeGenHelper.Stm(CodeGenHelper.MethodCall(
                              CodeGenHelper.Argument(this.ContainerParameterName), 
                              "ImportRow",
                              new CodeExpression[] {
                                    CodeGenHelper.Indexer(
                                        CodeGenHelper.Property( 
                                            CodeGenHelper.Property(
                                                CodeGenHelper.Variable(this.nameHandler.GetNameFromList("dataSet")), 
                                                this.DesignTable.GeneratorName 
                                            ),
                                            "Rows" 
                                        ),
                                        CodeGenHelper.Variable("i")
                                    )
                              } 
                   ));
 
                statements.Add(CodeGenHelper.ForLoop(forInit, forTest, forIncrement, new CodeStatement[] {forStatement})); 

            } 
            return true;
        }

        protected bool AddSetReturnParamValuesStatements(IList statements) { 
            //\\ this.adapter.SelectCommand
            CodeExpression commandExpression = 
                CodeGenHelper.Property( 
                    CodeGenHelper.Property(
                        CodeGenHelper.This(), 
                        DataComponentNameHandler.AdapterPropertyName
                    ),
                    "SelectCommand"
                ); 

            return base.AddSetReturnParamValuesStatements(statements, commandExpression); 
        } 

        private bool AddReturnStatements(IList statements) { 
            //int returnParamPos = GetReturnParameterPosition(activeCommand);

            //if(returnParamPos >= 0 && !this.getMethod) {
            //    //\\ if( typeof(command.Parameters[<returnParamPos>].Value) == System.DBNull ) { 
            //    //\\    return <SqlReturnType>.Null;
            //    //\\    or 
            //    //\\    return System.DBNull; 
            //    //\\    or
            //    //\\    throw new StrongTypingException("StrongTyping_CannotAccessDBNull"); 
            //    //\\ }
            //    //\\ else {
            //    //\\    return new <SqlReturnType> ( (CLRType) command.Parameters[<returnParamPos>].Value );
            //    //\\ } 
            //    DesignParameter returnParameter = (DesignParameter)activeCommand.Parameters[returnParamPos];
            //    Type returnType = GetParameterUrtType(returnParameter); 
 
            //    //\\ this.Adapter.SelectCommand.Parameters[<returnParameterPosition>].Value
            //    CodeExpression returnParamExpression = CodeGenHelper.Property( 
            //        CodeGenHelper.Indexer(
            //            CodeGenHelper.Property(
            //                CodeGenHelper.Property(CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), "SelectCommand"),
            //                "Parameters" 
            //            ),
            //            CodeGenHelper.Primitive(returnParamPos) 
            //        ), 
            //        "Value"
            //    ); 

            //    CodeExpression isEqualDbNullCondition = CodeGenHelper.GenerateDbNullCheck(returnParamExpression);

            //    CodeExpression nullExpression = CodeGenHelper.GenerateNullExpression(returnType); 
            //    CodeStatement trueStatement = null;
            //    if(nullExpression == null) { 
            //        if (returnParameter.AllowDbNull && returnType.IsValueType) { 
            //            //\\ return new System.Nullable<parameterType>();
            //            trueStatement = CodeGenHelper.Return(CodeGenHelper.GenericMethodCall( 
            //                new CodeTypeReferenceExpression(CodeGenHelper.Type(typeof(System.Nullable))),
            //                "FromObject",
            //                CodeGenHelper.Type(returnType),
            //                CodeGenHelper.Primitive(null) 
            //            ));
            //        } 
            //        else if (returnParameter.AllowDbNull && !returnType.IsValueType) { 
            //            //\\ return null;
            //            trueStatement = CodeGenHelper.Return( 
            //                CodeGenHelper.Primitive(null)
            //            );
            //        }
            //        else { 
            //            // in this case we can't return null
            //            //\\ throw new StrongTypingException("StrongTyping_CannotAccessDBNull"); 
            //            trueStatement = CodeGenHelper.Throw( 
            //                typeof(System.Data.StrongTypingException),
            //                SR.GetString(SR.CG_ParameterIsDBNull, ((DesignParameter)activeCommand.Parameters[returnParamPos]).ParameterName), 
            //                CodeGenHelper.Primitive(null)
            //            );
            //        }
            //    } 
            //    else {
            //        trueStatement = CodeGenHelper.Return(nullExpression); 
            //    } 

 
            //    CodeStatement falseStatement = null;
            //    if (returnParameter.AllowDbNull && returnType.IsValueType) {
            //        //\\ return new System.Nullable<returnType>((<returnType>) command.Parameter[i].Value);
            //        falseStatement = CodeGenHelper.Return( 
            //            CodeGenHelper.New(
            //                CodeGenHelper.NullableType(returnType), 
            //                new CodeExpression[] { CodeGenHelper.Cast(CodeGenHelper.Type(returnType), returnParamExpression) } 
            //            )
            //        ); 
            //    }
            //    else {
            //        CodeExpression convertExpression = CodeGenHelper.GenerateConvertExpression(returnParamExpression, typeof(System.Object), returnType);
            //        falseStatement = CodeGenHelper.Return(convertExpression); 
            //    }
 
            //    statements.Add( 
            //        CodeGenHelper.If(
            //            isEqualDbNullCondition, 
            //            trueStatement,
            //            falseStatement
            //        )
            //    ); 
            //}
            //else { 
            if(this.getMethod) { 
                if (this.GeneratePagingMethod) {
                    //\\ return dataSet.Customers 
                    statements.Add(
                        CodeGenHelper.Return(
                            CodeGenHelper.Property(
                                CodeGenHelper.Variable(this.nameHandler.GetNameFromList("dataSet")), 
                                this.DesignTable.GeneratorName
                            ) 
                        ) 
                    );
                } 
                else {
                    //\\ return dataTable;
                    statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable(this.ContainerParameterName)));
                } 
            }
            else { 
                //\\ return retVal; 
                statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable(returnVariableName)));
            } 
            //}

            return true;
        } 

 
        private void AddCustomAttributesToMethod(CodeMemberMethod dbMethod) { 
            bool isDefault = false;
            DataObjectMethodType methodType = DataObjectMethodType.Fill; 

            if (this.methodSource.EnableWebMethods && this.getMethod) {
                CodeAttributeDeclaration wmAttribute = new CodeAttributeDeclaration("System.Web.Services.WebMethod");
 
                wmAttribute.Arguments.Add(new CodeAttributeArgument("Description", CodeGenHelper.Str(this.methodSource.WebMethodDescription)));
                dbMethod.CustomAttributes.Add(wmAttribute); 
            } 

            // we generate DataObjectMethodAttributes only on the fill query that takes a dataTable, and on the getMethods 
            // in all other cases let's just return
            if (this.GeneratePagingMethod) {
                return;
            } 
            if (!this.getMethod && this.ContainerParameterType != typeof(System.Data.DataTable)) {
                return; 
            } 

            if(this.MethodSource == this.DesignTable.MainSource) { 
                // if the query comes from the main source, then it's the default query
                isDefault = true;
            }
 
            if(this.getMethod) {
                methodType = DataObjectMethodType.Select; 
            } 
            else {
                methodType = DataObjectMethodType.Fill; 
            }

            dbMethod.CustomAttributes.Add(
                new CodeAttributeDeclaration( 
                    CodeGenHelper.GlobalType(typeof(System.ComponentModel.DataObjectMethodAttribute)),
                    new CodeAttributeArgument[] { 
                        new CodeAttributeArgument(CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.ComponentModel.DataObjectMethodType)), methodType.ToString())), 
                        new CodeAttributeArgument(CodeGenHelper.Primitive(isDefault))
                    } 
                )
            );

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
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 
    using System.Data.Common;
    using System.Data.SqlClient; 
    using System.Data.SqlTypes; 
    using System.Data.Odbc;
    using System.Data.OleDb; 
    using System.Data.OracleClient;
    using System.Design;

 
    internal class QueryGenerator : QueryGeneratorBase {
        internal QueryGenerator(TypedDataSourceCodeGenerator codeGenerator) : base(codeGenerator) {} 
 
        internal override CodeMemberMethod Generate() {
            if(this.methodSource == null) { 
                throw new InternalException("MethodSource should not be null.");
            }
            if (StringUtil.Empty(this.ContainerParameterName)) {
                throw new InternalException("ContainerParameterName should not be empty."); 
            }
 
            // get select command 
            if(methodSource.SelectCommand == null) {
                codeGenerator.ProblemList.Add( new DSGeneratorProblem(SR.GetString(SR.CG_MainSelectCommandNotSet, this.DesignTable.Name), ProblemSeverity.NonFatalError, this.methodSource) ); 
                return null;
            }
            else {
                this.activeCommand = this.methodSource.SelectCommand; 
            }
 
            // get attributes 
            this.methodAttributes = MemberAttributes.Overloaded;
            if(this.getMethod) { 
                this.methodAttributes |= this.MethodSource.GetMethodModifier;
            }
            else {
                this.methodAttributes |= this.MethodSource.Modifier; 
            }
 
            if(this.codeProvider == null) { 
                this.codeProvider = this.codeGenerator.CodeProvider;
            } 
            // init the namehandler with the list of reserved strings
            this.nameHandler = new GenericNameHandler(new string[] {this.MethodName, returnVariableName}, this.codeProvider);

            // do the actual generation 
            return GenerateInternal();
        } 
 
        private CodeMemberMethod GenerateInternal() {
            // get return type 
            //DesignParameter returnParameter = GetReturnParameter(this.activeCommand);
            //if (returnParameter != null) {
            //    this.returnType = GetParameterUrtType(returnParameter);
            //} 
            //else {
            //    this.returnType = typeof(int); 
            //} 

            // always return the return value of Fill method, ignore eventual return parameter 
            this.returnType = typeof(int);

            // create the method declaration
            CodeMemberMethod dbMethod = null; 
            if(this.getMethod) {
                dbMethod = CodeGenHelper.MethodDecl(CodeGenHelper.Type(this.ContainerParameterTypeName), this.MethodName, this.methodAttributes); 
            } 
            else {
                //if (returnParameter != null && returnParameter.AllowDbNull && this.returnType.IsValueType) { 
                //    dbMethod = CodeGenHelper.MethodDecl(CodeGenHelper.NullableType(this.returnType), this.MethodName, this.methodAttributes);
                //}
                //else {
                dbMethod = CodeGenHelper.MethodDecl(CodeGenHelper.Type(this.returnType), this.MethodName, this.methodAttributes); 
                //}
            } 
 
            // add help keyword attribute to method
            dbMethod.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.Design.HelpKeywordAttribute).FullName, CodeGenHelper.Str("vs.data.TableAdapter"))); 

            // add parameters to the method
            AddParametersToMethod(dbMethod);
 
            if(this.declarationOnly) {
                AddThrowsClauseIfNeeded(dbMethod); 
                return dbMethod; 
            }
            else { 
                AddCustomAttributesToMethod(dbMethod);

                // add statements
                if(AddStatementsToMethod(dbMethod)) { 
                    return dbMethod;
                } 
                else { 
                    return null;
                } 
            }
        }

        private void AddParametersToMethod(CodeMemberMethod dbMethod) { 
            CodeParameterDeclarationExpression codeParam = null;
            // add container parameter to the method 
            if(!this.getMethod) { 
                string contParamName = this.nameHandler.AddNameToList(this.ContainerParameterName);
                codeParam = CodeGenHelper.ParameterDecl(CodeGenHelper.Type(this.ContainerParameterTypeName), contParamName); 
                dbMethod.Parameters.Add(codeParam);
            }

            if(this.GeneratePagingMethod) { 
                // add the startRecord and maxRecords parameters to the method declaration
                string startRecordParamName = this.nameHandler.AddNameToList(startRecordParameterName); 
                codeParam = CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(int)), startRecordParamName); 
                dbMethod.Parameters.Add(codeParam);
 
                string maxRecordsParamName = this.nameHandler.AddNameToList(maxRecordsParameterName);
                codeParam = CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(int)), maxRecordsParamName);
                dbMethod.Parameters.Add(codeParam);
            } 

            if(activeCommand.Parameters == null) { 
                return; 
            }
 
            DesignConnection connection = (DesignConnection)this.methodSource.Connection;
 			if (connection == null) {
				throw new InternalException(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Connection for query {0} is null.", this.methodSource.Name));
			} 
			string paramPrefix = connection.ParameterPrefix;
 
 			foreach(DesignParameter parameter in this.activeCommand.Parameters) { 
                if(parameter.Direction == ParameterDirection.ReturnValue) {
                    // skip over return parameter 
                    continue;
                }

                // get the type of the parameter 
                Type parameterType = this.GetParameterUrtType(parameter);
 
                // create parameter decl expression 
                string parameterName = this.nameHandler.AddParameterNameToList(parameter.ParameterName, paramPrefix);
                CodeTypeReference ctr = null; 
                if (parameter.AllowDbNull && parameterType.IsValueType) {
                    ctr = CodeGenHelper.NullableType(parameterType);
                }
                else { 
                    ctr = CodeGenHelper.Type(parameterType);
                } 
 
                codeParam = CodeGenHelper.ParameterDecl(ctr, parameterName);
 
                // set parameter direction
                codeParam.Direction = CodeGenHelper.ParameterDirectionToFieldDirection(parameter.Direction);

                // add parameter to method decl 
                dbMethod.Parameters.Add(codeParam);
            } 
        } 

        private bool AddStatementsToMethod(CodeMemberMethod dbMethod) { 
            bool succeeded = true;

            // Add statements to set SelectCommand
            succeeded = AddSetCommandStatements(dbMethod.Statements); 
            if(!succeeded) {
                return false; 
            } 

            // Add statements to set parameters on command object 
            succeeded = AddSetParametersStatements(dbMethod.Statements);
            if(!succeeded) {
                return false;
            } 

            succeeded = AddClearStatements(dbMethod.Statements); 
            if (!succeeded) { 
                return false;
            } 

            // Add statements to execute the command
            if (this.GeneratePagingMethod) {
                succeeded = AddExecuteCommandStatementsForPaging(dbMethod.Statements); 
            }
            else { 
                succeeded = AddExecuteCommandStatements(dbMethod.Statements); 
            }
            if(!succeeded) { 
                return false;
            }

            // Add statements to set output parameter values 
            succeeded = AddSetReturnParamValuesStatements(dbMethod.Statements);
            if(!succeeded) { 
                return false; 
            }
 
            // Add return statements (used by Fill methods only)
            succeeded = AddReturnStatements(dbMethod.Statements);
            if(!succeeded) {
                return false; 
            }
 
            return true; 
        }
 
        private bool AddSetCommandStatements(IList statements) {
            Type commandType = this.ProviderFactory.CreateCommand().GetType();
            statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(
                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                        "SelectCommand" 
                    ),
                    CodeGenHelper.ArrayIndexer( 
                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                        CodeGenHelper.Primitive(this.CommandIndex)
                    )
                ) 
            );
 
            return true; 
        }
 
        private bool AddSetParametersStatements(IList statements) {
            int paramCount = 0;
            if(this.activeCommand.Parameters != null) {
                paramCount = this.activeCommand.Parameters.Count; 
            }
 
 
            for(int i = 0; i < paramCount; i++) {
                DesignParameter parameter = activeCommand.Parameters[i] as DesignParameter; 
                if(parameter == null) {
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter.");
                }
 
                if(parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput) {
                    string parameterName = nameHandler.GetNameFromList(parameter.ParameterName); 
                    CodeExpression selectCmdExpression = CodeGenHelper.Property( 
                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                        "SelectCommand" 
                    );

                    AddSetParameterStatements(parameter, parameterName, selectCmdExpression, i, statements);
                } 
            }
 
            return true; 
        }
 
        private bool AddClearStatements(IList statements) {
            if (!this.getMethod) {
                CodeStatement trueStatement = null;
                if (this.containerParamType == typeof(System.Data.DataTable)) { 
                    //\\ table.Clear();
                    trueStatement = CodeGenHelper.Stm( 
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Argument(this.ContainerParameterName),
                            "Clear", 
                            new CodeExpression[] {}
                        )
                    );
                } 
                else if (this.containerParamType == typeof(System.Data.DataSet)) {
                    //\\ dataSet.<TablePropertyName>.Clear() 
                    trueStatement = CodeGenHelper.Stm( 
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Property(CodeGenHelper.Argument(this.ContainerParameterName), this.DesignTable.GeneratorTablePropName), 
                            "Clear",
                            new CodeExpression[] {}
                        )
                    ); 
                }
                else { 
                    throw new InternalException("Unknown containerParameterType."); 
                }
 
                statements.Add(
                    CodeGenHelper.If(
                        CodeGenHelper.EQ(
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ClearBeforeFillPropertyName), 
                            CodeGenHelper.Primitive(true)
                        ), 
                        trueStatement 
                    )
                ); 
            }

            return true;
        } 

        private bool AddExecuteCommandStatements(IList statements) { 
            if(this.getMethod) { 
                //\\ NorthwindDataSet.CustomersDataTable table = new NorthwindDataSet.CustomersDataTable();
                CodeExpression[] argCodeExps = new CodeExpression []{}; 
                bool hasExpressionColumn = this.designTable != null && this.designTable.HasAnyExpressionColumn;
                if (hasExpressionColumn){
                    argCodeExps = new CodeExpression []{CodeGenHelper.Primitive(true)};
                } 

                statements.Add( 
                    CodeGenHelper.VariableDecl( 
                        CodeGenHelper.Type(this.ContainerParameterTypeName),
                        this.ContainerParameterName, 
                        CodeGenHelper.New(CodeGenHelper.Type(this.ContainerParameterTypeName), argCodeExps)
                    )
                );
            } 

            //\\ this._adapter.Fill(dataSet); 
            //\\ OR 
            //\\ int retVal = this._adapter.Fill(dataSet);
            CodeExpression[] fillParameters = new CodeExpression[1]; 
            fillParameters[0] = CodeGenHelper.Variable(this.ContainerParameterName);

            if(!this.getMethod) {
                // Ignore return parameter for the command, we'll return the return value from the fill method 
                statements.Add(
                    CodeGenHelper.VariableDecl( 
                        CodeGenHelper.GlobalType(typeof(int)), 
                        returnVariableName,
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                            "Fill",
                            fillParameters
                        ) 
                    )
                ); 
            } 
            else {
                // It's a GetData() method, we'll return the DataTable 
                statements.Add(
                    CodeGenHelper.Stm(
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                            "Fill",
                            fillParameters 
                        ) 
                    )
                ); 
            }

            return true;
        } 

        private bool AddExecuteCommandStatementsForPaging(IList statements) { 
            if(this.containerParamType == typeof(System.Data.DataTable)) { 
                // we need to instantiate a new dataset
                //\\ NorthwindDataSet dataSet = new NorthwindDataSet(); 
                statements.Add(
                    CodeGenHelper.VariableDecl(
                        CodeGenHelper.Type(this.codeGenerator.DataSourceName),
                        nameHandler.AddNameToList("dataSet"), 
                        CodeGenHelper.New(CodeGenHelper.Type(this.codeGenerator.DataSourceName), new CodeExpression[] {})
                    ) 
                ); 
            }
 
            //\\ this._adapter.Fill(dataSet, startRecord, maxRecords, "Table");
            //\\ OR
            //\\ int retVal = this._adapter.Fill(dataSet, startRecord, maxRecords, "Table");
            CodeExpression[] fillParameters = new CodeExpression[4]; 
            if(this.containerParamType == typeof(System.Data.DataTable)) {
                fillParameters[0] = CodeGenHelper.Variable(this.nameHandler.GetNameFromList("dataSet")); 
            } 
            else {
                fillParameters[0] = CodeGenHelper.Argument(this.ContainerParameterName); 
            }
            fillParameters[1] = CodeGenHelper.Argument(this.nameHandler.GetNameFromList(startRecordParameterName));
            fillParameters[2] = CodeGenHelper.Argument(this.nameHandler.GetNameFromList(maxRecordsParameterName));
            fillParameters[3] = CodeGenHelper.Str("Table"); 

            if(!this.getMethod) { 
                // Ignore return parameter for the command, we'll return the return value from the fill method 
                statements.Add(
                    CodeGenHelper.VariableDecl( 
                        CodeGenHelper.GlobalType(typeof(int)),
                        returnVariableName,
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                            "Fill",
                            fillParameters 
                        ) 
                    )
                ); 
            }
            else {
                // It's a GetData() method, we'll return the DataTable
                statements.Add( 
                    CodeGenHelper.Stm(
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                            "Fill",
                            fillParameters 
                        )
                    )
                );
            } 

            if(this.containerParamType == typeof(System.Data.DataTable) && !this.getMethod) { 
                // we need to move the filled rows to the DataTable that was passed in as argument to this method 
                //\\ for(int i = 0; i < dataSet.Customers.Rows.Count; i++) {
                //\\    dataTable.ImportRow(dataSet.Customers.Rows[i]; 
                //\\ }
                CodeStatement forInit = CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(int)), "i", CodeGenHelper.Primitive(0));
                CodeExpression forTest = CodeGenHelper.Less(
                                            CodeGenHelper.Variable("i"), 
                                            CodeGenHelper.Property(
                                                CodeGenHelper.Property( 
                                                    CodeGenHelper.Property( 
                                                        CodeGenHelper.Variable(this.nameHandler.GetNameFromList("dataSet")),
                                                        this.DesignTable.GeneratorName 
                                                    ),
                                                    "Rows"
                                                ),
                                                "Count" 
                                            )
                                        ); 
                CodeStatement forIncrement = CodeGenHelper.Assign( 
                                                CodeGenHelper.Variable("i"),
                                                CodeGenHelper.BinOperator( 
                                                    CodeGenHelper.Variable("i"),
                                                    CodeBinaryOperatorType.Add,
                                                    CodeGenHelper.Primitive(1)
                                                ) 
                                            );
 
                CodeStatement forStatement = 
                    CodeGenHelper.Stm(CodeGenHelper.MethodCall(
                              CodeGenHelper.Argument(this.ContainerParameterName), 
                              "ImportRow",
                              new CodeExpression[] {
                                    CodeGenHelper.Indexer(
                                        CodeGenHelper.Property( 
                                            CodeGenHelper.Property(
                                                CodeGenHelper.Variable(this.nameHandler.GetNameFromList("dataSet")), 
                                                this.DesignTable.GeneratorName 
                                            ),
                                            "Rows" 
                                        ),
                                        CodeGenHelper.Variable("i")
                                    )
                              } 
                   ));
 
                statements.Add(CodeGenHelper.ForLoop(forInit, forTest, forIncrement, new CodeStatement[] {forStatement})); 

            } 
            return true;
        }

        protected bool AddSetReturnParamValuesStatements(IList statements) { 
            //\\ this.adapter.SelectCommand
            CodeExpression commandExpression = 
                CodeGenHelper.Property( 
                    CodeGenHelper.Property(
                        CodeGenHelper.This(), 
                        DataComponentNameHandler.AdapterPropertyName
                    ),
                    "SelectCommand"
                ); 

            return base.AddSetReturnParamValuesStatements(statements, commandExpression); 
        } 

        private bool AddReturnStatements(IList statements) { 
            //int returnParamPos = GetReturnParameterPosition(activeCommand);

            //if(returnParamPos >= 0 && !this.getMethod) {
            //    //\\ if( typeof(command.Parameters[<returnParamPos>].Value) == System.DBNull ) { 
            //    //\\    return <SqlReturnType>.Null;
            //    //\\    or 
            //    //\\    return System.DBNull; 
            //    //\\    or
            //    //\\    throw new StrongTypingException("StrongTyping_CannotAccessDBNull"); 
            //    //\\ }
            //    //\\ else {
            //    //\\    return new <SqlReturnType> ( (CLRType) command.Parameters[<returnParamPos>].Value );
            //    //\\ } 
            //    DesignParameter returnParameter = (DesignParameter)activeCommand.Parameters[returnParamPos];
            //    Type returnType = GetParameterUrtType(returnParameter); 
 
            //    //\\ this.Adapter.SelectCommand.Parameters[<returnParameterPosition>].Value
            //    CodeExpression returnParamExpression = CodeGenHelper.Property( 
            //        CodeGenHelper.Indexer(
            //            CodeGenHelper.Property(
            //                CodeGenHelper.Property(CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), "SelectCommand"),
            //                "Parameters" 
            //            ),
            //            CodeGenHelper.Primitive(returnParamPos) 
            //        ), 
            //        "Value"
            //    ); 

            //    CodeExpression isEqualDbNullCondition = CodeGenHelper.GenerateDbNullCheck(returnParamExpression);

            //    CodeExpression nullExpression = CodeGenHelper.GenerateNullExpression(returnType); 
            //    CodeStatement trueStatement = null;
            //    if(nullExpression == null) { 
            //        if (returnParameter.AllowDbNull && returnType.IsValueType) { 
            //            //\\ return new System.Nullable<parameterType>();
            //            trueStatement = CodeGenHelper.Return(CodeGenHelper.GenericMethodCall( 
            //                new CodeTypeReferenceExpression(CodeGenHelper.Type(typeof(System.Nullable))),
            //                "FromObject",
            //                CodeGenHelper.Type(returnType),
            //                CodeGenHelper.Primitive(null) 
            //            ));
            //        } 
            //        else if (returnParameter.AllowDbNull && !returnType.IsValueType) { 
            //            //\\ return null;
            //            trueStatement = CodeGenHelper.Return( 
            //                CodeGenHelper.Primitive(null)
            //            );
            //        }
            //        else { 
            //            // in this case we can't return null
            //            //\\ throw new StrongTypingException("StrongTyping_CannotAccessDBNull"); 
            //            trueStatement = CodeGenHelper.Throw( 
            //                typeof(System.Data.StrongTypingException),
            //                SR.GetString(SR.CG_ParameterIsDBNull, ((DesignParameter)activeCommand.Parameters[returnParamPos]).ParameterName), 
            //                CodeGenHelper.Primitive(null)
            //            );
            //        }
            //    } 
            //    else {
            //        trueStatement = CodeGenHelper.Return(nullExpression); 
            //    } 

 
            //    CodeStatement falseStatement = null;
            //    if (returnParameter.AllowDbNull && returnType.IsValueType) {
            //        //\\ return new System.Nullable<returnType>((<returnType>) command.Parameter[i].Value);
            //        falseStatement = CodeGenHelper.Return( 
            //            CodeGenHelper.New(
            //                CodeGenHelper.NullableType(returnType), 
            //                new CodeExpression[] { CodeGenHelper.Cast(CodeGenHelper.Type(returnType), returnParamExpression) } 
            //            )
            //        ); 
            //    }
            //    else {
            //        CodeExpression convertExpression = CodeGenHelper.GenerateConvertExpression(returnParamExpression, typeof(System.Object), returnType);
            //        falseStatement = CodeGenHelper.Return(convertExpression); 
            //    }
 
            //    statements.Add( 
            //        CodeGenHelper.If(
            //            isEqualDbNullCondition, 
            //            trueStatement,
            //            falseStatement
            //        )
            //    ); 
            //}
            //else { 
            if(this.getMethod) { 
                if (this.GeneratePagingMethod) {
                    //\\ return dataSet.Customers 
                    statements.Add(
                        CodeGenHelper.Return(
                            CodeGenHelper.Property(
                                CodeGenHelper.Variable(this.nameHandler.GetNameFromList("dataSet")), 
                                this.DesignTable.GeneratorName
                            ) 
                        ) 
                    );
                } 
                else {
                    //\\ return dataTable;
                    statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable(this.ContainerParameterName)));
                } 
            }
            else { 
                //\\ return retVal; 
                statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable(returnVariableName)));
            } 
            //}

            return true;
        } 

 
        private void AddCustomAttributesToMethod(CodeMemberMethod dbMethod) { 
            bool isDefault = false;
            DataObjectMethodType methodType = DataObjectMethodType.Fill; 

            if (this.methodSource.EnableWebMethods && this.getMethod) {
                CodeAttributeDeclaration wmAttribute = new CodeAttributeDeclaration("System.Web.Services.WebMethod");
 
                wmAttribute.Arguments.Add(new CodeAttributeArgument("Description", CodeGenHelper.Str(this.methodSource.WebMethodDescription)));
                dbMethod.CustomAttributes.Add(wmAttribute); 
            } 

            // we generate DataObjectMethodAttributes only on the fill query that takes a dataTable, and on the getMethods 
            // in all other cases let's just return
            if (this.GeneratePagingMethod) {
                return;
            } 
            if (!this.getMethod && this.ContainerParameterType != typeof(System.Data.DataTable)) {
                return; 
            } 

            if(this.MethodSource == this.DesignTable.MainSource) { 
                // if the query comes from the main source, then it's the default query
                isDefault = true;
            }
 
            if(this.getMethod) {
                methodType = DataObjectMethodType.Select; 
            } 
            else {
                methodType = DataObjectMethodType.Fill; 
            }

            dbMethod.CustomAttributes.Add(
                new CodeAttributeDeclaration( 
                    CodeGenHelper.GlobalType(typeof(System.ComponentModel.DataObjectMethodAttribute)),
                    new CodeAttributeArgument[] { 
                        new CodeAttributeArgument(CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.ComponentModel.DataObjectMethodType)), methodType.ToString())), 
                        new CodeAttributeArgument(CodeGenHelper.Primitive(isDefault))
                    } 
                )
            );

        } 

    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
