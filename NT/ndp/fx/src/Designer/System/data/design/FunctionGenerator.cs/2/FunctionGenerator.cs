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

    internal class FunctionGenerator : QueryGeneratorBase { 

        internal FunctionGenerator(TypedDataSourceCodeGenerator codeGenerator) : base(codeGenerator) {} 
 
        internal override CodeMemberMethod Generate() {
            if(this.methodSource == null) { 
                throw new InternalException("MethodSource should not be null.");
            }

            // get active command 
            this.activeCommand = this.methodSource.GetActiveCommand();
            if(this.activeCommand == null) { 
                return null; 
            }
 

            // get attributes
            this.methodAttributes = this.MethodSource.Modifier | MemberAttributes.Overloaded;
 
            if(this.codeProvider == null) {
                this.codeProvider = this.codeGenerator.CodeProvider; 
            } 
            // init the namehandler with the list of reserved strings
            this.nameHandler = new GenericNameHandler(new string[] {this.MethodName, returnVariableName, commandVariableName}, this.codeProvider); 

            // do the actual generation
            return GenerateInternal();
        } 

        private CodeMemberMethod GenerateInternal() { 
            DesignParameter returnParameter = GetReturnParameter(this.activeCommand); 
            CodeTypeReference methodReturnType = null;
 
            // get return type
            if (this.methodSource.QueryType == QueryType.Scalar) {
                this.returnType = this.methodSource.ScalarCallRetval;
                if (this.returnType.IsValueType) { 
                    methodReturnType = CodeGenHelper.NullableType(this.returnType);
                } 
                else { 
                    methodReturnType = CodeGenHelper.Type(this.returnType);
                } 
            }
            else if (this.methodSource.DbObjectType == DbObjectType.Function && returnParameter != null) {
                // for functions with a return parameter we return that one
                this.returnType = GetParameterUrtType(returnParameter); 
                if (returnParameter.AllowDbNull && this.returnType.IsValueType) {
                    methodReturnType = CodeGenHelper.NullableType(this.returnType); 
                } 
                else {
                    methodReturnType = CodeGenHelper.Type(this.returnType); 
                }
            }
            else {
                // in all other cases we return what ExecuteNonQuery returns 
                this.returnType = typeof(int);
                methodReturnType = CodeGenHelper.Type(this.returnType); 
            } 

            // create the method declaration 
            CodeMemberMethod dbMethod = null;
            dbMethod = CodeGenHelper.MethodDecl(methodReturnType, this.MethodName, this.methodAttributes);

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

            if(activeCommand.Parameters == null) {
                return; 
            }
 
            DesignConnection connection = (DesignConnection)this.methodSource.Connection; 
 			if(connection == null) {
				throw new InternalException("Connection for query '" + this.methodSource.Name + "' is null."); 
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

            // Add statements to execute the command 
            succeeded = AddExecuteCommandStatements(dbMethod.Statements);
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
 
            //\\ SqlCommand command = selectCommandCollection[i];
            //\\ or
            //\\ SqlCommand command = (SqlCommand) selectCommandCollection[i];
            CodeExpression commandExpression = CodeGenHelper.ArrayIndexer( 
                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                CodeGenHelper.Primitive(this.CommandIndex) 
            ); 

            if (this.IsFunctionsDataComponent) { 
                commandExpression = CodeGenHelper.Cast(CodeGenHelper.GlobalType(commandType), commandExpression);
            }

            statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(commandType), 
                    commandVariableName, 
                    commandExpression
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
                    AddSetParameterStatements(parameter, parameterName, CodeGenHelper.Variable(commandVariableName), i, statements);
                } 
            }
 
            return true; 
        }
 
        private bool AddExecuteCommandStatements(IList statements) {
            CodeStatement[] tryStatements = new CodeStatement[1];
            CodeStatement[] finallyStatements = new CodeStatement[1];
            //\\ System.Data.ConnectionState previousConnectionState = command.Connection.State; 
            statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(System.Data.ConnectionState)), 
                    this.nameHandler.AddNameToList("previousConnectionState"),
                    CodeGenHelper.Property( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Variable(commandVariableName),
                            "Connection"
                        ), 
                        "State"
                    ) 
                ) 
            );
            //\\ if((command.Connection.State & System.Data.ConnectionState.Open) != System.Data.ConnectionState.Open) { 
            //\\    command.Connection.Open();
            //\\ }
            statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.BitwiseAnd( 
                            CodeGenHelper.Property( 
                                CodeGenHelper.Property(CodeGenHelper.Variable(commandVariableName), "Connection"),
                                "State" 
                            ),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Open")
                        ),
                        CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Open") 
                    ),
                    CodeGenHelper.Stm(CodeGenHelper.MethodCall( 
                        CodeGenHelper.Property( 
                            CodeGenHelper.Variable(commandVariableName),
                            "Connection" 
                        ),
                        "Open"
                    ))
                ) 
            );
 
 
            if(this.methodSource.QueryType == QueryType.Scalar) {
                //\\ object returnValue; 
                //\\ try {
                //\\    returnValue = command.ExecuteScalar();
                //\\ }
                statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(System.Object)), returnVariableName)); 

                tryStatements[0] = CodeGenHelper.Assign( 
                    CodeGenHelper.Variable(returnVariableName), 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable(commandVariableName), 
                        "ExecuteScalar",
                        new CodeExpression[] {}
                    )
                ); 
            }
            else if(this.methodSource.DbObjectType == DbObjectType.Function && this.GetReturnParameterPosition(activeCommand) >= 0) { 
                // If it's a function and there is a return parameter for the command we return that one 
                tryStatements[0] =
                    CodeGenHelper.Stm( 
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Variable(commandVariableName),
                            "ExecuteNonQuery",
                            new CodeExpression[] {} 
                        )
                    ); 
            } 
            else  {
                // in all other cases we return ExecuteNonQuery's return value 
                statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(int)), returnVariableName));
                tryStatements[0] =
                    CodeGenHelper.Assign(
                        CodeGenHelper.Variable(returnVariableName), 
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Variable(commandVariableName), 
                            "ExecuteNonQuery", 
                            new CodeExpression[] {}
                        ) 
                    );
            }

            //\\ if(previousConnectionState == System.Data.ConnectionState.Closed) { 
            //\\    command.Connection.Close();
            //\\ } 
            finallyStatements[0] = CodeGenHelper.If( 
                CodeGenHelper.EQ(
                    CodeGenHelper.Variable(this.nameHandler.GetNameFromList("previousConnectionState")), 
                    CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Closed")
                ),
                CodeGenHelper.Stm(CodeGenHelper.MethodCall(
                    CodeGenHelper.Property( 
                        CodeGenHelper.Variable(commandVariableName),
                        "Connection" 
                    ), 
                    "Close"
                )) 
            );

            statements.Add(CodeGenHelper.Try(tryStatements, new CodeCatchClause[] {}, finallyStatements));
 
            return true;
        } 
 
        protected bool AddSetReturnParamValuesStatements(IList statements) {
            return base.AddSetReturnParamValuesStatements(statements, CodeGenHelper.Variable(commandVariableName)); 
        }


        private bool AddReturnStatements(IList statements) { 
            int returnParamPos = GetReturnParameterPosition(activeCommand);
 
            if(this.methodSource.DbObjectType == DbObjectType.Function && this.methodSource.QueryType != QueryType.Scalar && returnParamPos >= 0) { 
                // if it's a function we return the return parameter
                //\\ if( typeof(command.Parameters[<returnParamPos>].Value) == System.DBNull ) { 
                //\\    return <SqlReturnType>.Null;
                //\\    or
                //\\    return System.DBNull;
                //\\    or 
                //\\    throw new StrongTypingException("StrongTyping_CannotAccessDBNull for parameter <paramName>");
                //\\ } 
                //\\ else { 
                //\\    return new <SqlReturnType> ( (CLRType) command.Parameters[<returnParamPos>].Value );
                //\\ } 
                DesignParameter returnParameter = (DesignParameter)activeCommand.Parameters[returnParamPos];
                Type returnType = GetParameterUrtType(returnParameter);

                //\\ <command>.Parameters[<returnParamPosition>].Value 
                CodeExpression returnParamExpression = CodeGenHelper.Property(
                    CodeGenHelper.Indexer( 
                        CodeGenHelper.Property( 
                            CodeGenHelper.Variable(commandVariableName),
                            "Parameters" 
                        ),
                        CodeGenHelper.Primitive(returnParamPos)
                    ),
                    "Value" 
                );
 
                CodeExpression isEqualDbNullCondition = CodeGenHelper.GenerateDbNullCheck(returnParamExpression); 

                CodeExpression nullExpression = CodeGenHelper.GenerateNullExpression(returnType); 
                CodeStatement trueStatement = null;
                if(nullExpression == null) {
                    if (returnParameter.AllowDbNull && returnType.IsValueType) {
                        //\\ return new System.Nullable<returnType>(); 
                        trueStatement = CodeGenHelper.Return(
                            CodeGenHelper.New( 
                                CodeGenHelper.NullableType(returnType), 
                                new CodeExpression[] { }
                            ) 
                        );
                    }
                    else if (returnParameter.AllowDbNull && !returnType.IsValueType) {
                        //\\ return null; 
                        trueStatement = CodeGenHelper.Return(
                            CodeGenHelper.Primitive(null) 
                        ); 
                    }
                    else { 
                        // in this case we can't return null
                        //\\ throw new StrongTypingException("StrongTyping_CannotAccessDBNull");
                        trueStatement = CodeGenHelper.Throw(
                            CodeGenHelper.GlobalType(typeof(System.Data.StrongTypingException)), 
                            SR.GetString(SR.CG_ParameterIsDBNull, ((DesignParameter)activeCommand.Parameters[returnParamPos]).ParameterName),
                            CodeGenHelper.Primitive(null) 
                        ); 
                    }
                } 
                else {
                    trueStatement = CodeGenHelper.Return(nullExpression);
                }
 
                CodeStatement falseStatement = null;
                if (returnParameter.AllowDbNull && returnType.IsValueType) { 
                    //\\ return new System.Nullable<returnType>((<returnType>) command.Parameter[i].Value); 
                    falseStatement = CodeGenHelper.Return(
                        CodeGenHelper.New( 
                            CodeGenHelper.NullableType(returnType),
                            new CodeExpression[] { CodeGenHelper.Cast(CodeGenHelper.GlobalType(returnType), returnParamExpression) }
                        )
                    ); 
                }
                else { 
                    CodeExpression convertExpression = CodeGenHelper.GenerateConvertExpression(returnParamExpression, typeof(System.Object), returnType); 
                    falseStatement = CodeGenHelper.Return(convertExpression);
                } 

                statements.Add(
                    CodeGenHelper.If(
                        isEqualDbNullCondition, 
                        trueStatement,
                        falseStatement 
                    ) 
                );
            } 
            else if (this.methodSource.QueryType == QueryType.Scalar) {
                //\\ if(returnValue is typeof(System.DbNull)) {
                //\\    return new System.Nullable<type>();
                //\\    OR 
                //\\    return null;
                //\\ } 
                //\\ else { 
                //\\    return new System.Nullable<type>((type) returnValue);
                //\\    OR 
                //\\    return (type) returnValue;
                //\\ }
                CodeExpression isEqualDbNullCondition = CodeGenHelper.GenerateDbNullCheck(CodeGenHelper.Variable(returnVariableName));
                CodeStatement trueStatement = null; 
                CodeStatement falseStatement = null;
 
                if (this.returnType.IsValueType) { 
                    trueStatement = CodeGenHelper.Return(
                        CodeGenHelper.New( 
                            CodeGenHelper.NullableType(returnType),
                            new CodeExpression[] { }
                        )
                    ); 

                    falseStatement = CodeGenHelper.Return( 
                        CodeGenHelper.New( 
                            CodeGenHelper.NullableType(this.returnType),
                            new CodeExpression[] { 
                                CodeGenHelper.Cast(
                                    CodeGenHelper.GlobalType(this.returnType),
                                    CodeGenHelper.Variable(returnVariableName)
                                ) 
                            }
                        ) 
                    ); 
                }
                else { 
                    trueStatement = CodeGenHelper.Return(CodeGenHelper.Primitive(null));
                    falseStatement = CodeGenHelper.Return(CodeGenHelper.Cast(CodeGenHelper.GlobalType(this.returnType), CodeGenHelper.Variable(returnVariableName)));
                }
 
                statements.Add(
                    CodeGenHelper.If( 
                        isEqualDbNullCondition, 
                        trueStatement,
                        falseStatement 
                    )
                );

 
            }
            else { 
                //\\ return returnValue; 
                statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable(returnVariableName)));
            } 

            return true;
        }
 
        private void AddCustomAttributesToMethod(CodeMemberMethod dbMethod) {
            if (this.methodSource.EnableWebMethods) { 
                CodeAttributeDeclaration wmAttribute = new CodeAttributeDeclaration("System.Web.Services.WebMethod"); 

                wmAttribute.Arguments.Add(new CodeAttributeArgument("Description", CodeGenHelper.Str(this.methodSource.WebMethodDescription))); 
                dbMethod.CustomAttributes.Add(wmAttribute);
            }

            DataObjectMethodType methodType = DataObjectMethodType.Select; 

            if (this.methodSource.CommandOperation == CommandOperation.Update) { 
                methodType = DataObjectMethodType.Update; 
            }
            else if (this.methodSource.CommandOperation == CommandOperation.Delete) { 
                methodType = DataObjectMethodType.Delete;
            }
            else if (this.methodSource.CommandOperation == CommandOperation.Insert) {
                methodType = DataObjectMethodType.Insert; 
            }
 
            if (methodType != DataObjectMethodType.Select) { 
                dbMethod.CustomAttributes.Add(
                    new CodeAttributeDeclaration( 
                        CodeGenHelper.GlobalType(typeof(System.ComponentModel.DataObjectMethodAttribute)),
                        new CodeAttributeArgument[] {
                            new CodeAttributeArgument(CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.ComponentModel.DataObjectMethodType)), methodType.ToString())),
                            new CodeAttributeArgument(CodeGenHelper.Primitive(false)) 
                        }
                    ) 
                ); 
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

    internal class FunctionGenerator : QueryGeneratorBase { 

        internal FunctionGenerator(TypedDataSourceCodeGenerator codeGenerator) : base(codeGenerator) {} 
 
        internal override CodeMemberMethod Generate() {
            if(this.methodSource == null) { 
                throw new InternalException("MethodSource should not be null.");
            }

            // get active command 
            this.activeCommand = this.methodSource.GetActiveCommand();
            if(this.activeCommand == null) { 
                return null; 
            }
 

            // get attributes
            this.methodAttributes = this.MethodSource.Modifier | MemberAttributes.Overloaded;
 
            if(this.codeProvider == null) {
                this.codeProvider = this.codeGenerator.CodeProvider; 
            } 
            // init the namehandler with the list of reserved strings
            this.nameHandler = new GenericNameHandler(new string[] {this.MethodName, returnVariableName, commandVariableName}, this.codeProvider); 

            // do the actual generation
            return GenerateInternal();
        } 

        private CodeMemberMethod GenerateInternal() { 
            DesignParameter returnParameter = GetReturnParameter(this.activeCommand); 
            CodeTypeReference methodReturnType = null;
 
            // get return type
            if (this.methodSource.QueryType == QueryType.Scalar) {
                this.returnType = this.methodSource.ScalarCallRetval;
                if (this.returnType.IsValueType) { 
                    methodReturnType = CodeGenHelper.NullableType(this.returnType);
                } 
                else { 
                    methodReturnType = CodeGenHelper.Type(this.returnType);
                } 
            }
            else if (this.methodSource.DbObjectType == DbObjectType.Function && returnParameter != null) {
                // for functions with a return parameter we return that one
                this.returnType = GetParameterUrtType(returnParameter); 
                if (returnParameter.AllowDbNull && this.returnType.IsValueType) {
                    methodReturnType = CodeGenHelper.NullableType(this.returnType); 
                } 
                else {
                    methodReturnType = CodeGenHelper.Type(this.returnType); 
                }
            }
            else {
                // in all other cases we return what ExecuteNonQuery returns 
                this.returnType = typeof(int);
                methodReturnType = CodeGenHelper.Type(this.returnType); 
            } 

            // create the method declaration 
            CodeMemberMethod dbMethod = null;
            dbMethod = CodeGenHelper.MethodDecl(methodReturnType, this.MethodName, this.methodAttributes);

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

            if(activeCommand.Parameters == null) {
                return; 
            }
 
            DesignConnection connection = (DesignConnection)this.methodSource.Connection; 
 			if(connection == null) {
				throw new InternalException("Connection for query '" + this.methodSource.Name + "' is null."); 
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

            // Add statements to execute the command 
            succeeded = AddExecuteCommandStatements(dbMethod.Statements);
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
 
            //\\ SqlCommand command = selectCommandCollection[i];
            //\\ or
            //\\ SqlCommand command = (SqlCommand) selectCommandCollection[i];
            CodeExpression commandExpression = CodeGenHelper.ArrayIndexer( 
                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                CodeGenHelper.Primitive(this.CommandIndex) 
            ); 

            if (this.IsFunctionsDataComponent) { 
                commandExpression = CodeGenHelper.Cast(CodeGenHelper.GlobalType(commandType), commandExpression);
            }

            statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(commandType), 
                    commandVariableName, 
                    commandExpression
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
                    AddSetParameterStatements(parameter, parameterName, CodeGenHelper.Variable(commandVariableName), i, statements);
                } 
            }
 
            return true; 
        }
 
        private bool AddExecuteCommandStatements(IList statements) {
            CodeStatement[] tryStatements = new CodeStatement[1];
            CodeStatement[] finallyStatements = new CodeStatement[1];
            //\\ System.Data.ConnectionState previousConnectionState = command.Connection.State; 
            statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(System.Data.ConnectionState)), 
                    this.nameHandler.AddNameToList("previousConnectionState"),
                    CodeGenHelper.Property( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Variable(commandVariableName),
                            "Connection"
                        ), 
                        "State"
                    ) 
                ) 
            );
            //\\ if((command.Connection.State & System.Data.ConnectionState.Open) != System.Data.ConnectionState.Open) { 
            //\\    command.Connection.Open();
            //\\ }
            statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.BitwiseAnd( 
                            CodeGenHelper.Property( 
                                CodeGenHelper.Property(CodeGenHelper.Variable(commandVariableName), "Connection"),
                                "State" 
                            ),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Open")
                        ),
                        CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Open") 
                    ),
                    CodeGenHelper.Stm(CodeGenHelper.MethodCall( 
                        CodeGenHelper.Property( 
                            CodeGenHelper.Variable(commandVariableName),
                            "Connection" 
                        ),
                        "Open"
                    ))
                ) 
            );
 
 
            if(this.methodSource.QueryType == QueryType.Scalar) {
                //\\ object returnValue; 
                //\\ try {
                //\\    returnValue = command.ExecuteScalar();
                //\\ }
                statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(System.Object)), returnVariableName)); 

                tryStatements[0] = CodeGenHelper.Assign( 
                    CodeGenHelper.Variable(returnVariableName), 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable(commandVariableName), 
                        "ExecuteScalar",
                        new CodeExpression[] {}
                    )
                ); 
            }
            else if(this.methodSource.DbObjectType == DbObjectType.Function && this.GetReturnParameterPosition(activeCommand) >= 0) { 
                // If it's a function and there is a return parameter for the command we return that one 
                tryStatements[0] =
                    CodeGenHelper.Stm( 
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Variable(commandVariableName),
                            "ExecuteNonQuery",
                            new CodeExpression[] {} 
                        )
                    ); 
            } 
            else  {
                // in all other cases we return ExecuteNonQuery's return value 
                statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(int)), returnVariableName));
                tryStatements[0] =
                    CodeGenHelper.Assign(
                        CodeGenHelper.Variable(returnVariableName), 
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.Variable(commandVariableName), 
                            "ExecuteNonQuery", 
                            new CodeExpression[] {}
                        ) 
                    );
            }

            //\\ if(previousConnectionState == System.Data.ConnectionState.Closed) { 
            //\\    command.Connection.Close();
            //\\ } 
            finallyStatements[0] = CodeGenHelper.If( 
                CodeGenHelper.EQ(
                    CodeGenHelper.Variable(this.nameHandler.GetNameFromList("previousConnectionState")), 
                    CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Closed")
                ),
                CodeGenHelper.Stm(CodeGenHelper.MethodCall(
                    CodeGenHelper.Property( 
                        CodeGenHelper.Variable(commandVariableName),
                        "Connection" 
                    ), 
                    "Close"
                )) 
            );

            statements.Add(CodeGenHelper.Try(tryStatements, new CodeCatchClause[] {}, finallyStatements));
 
            return true;
        } 
 
        protected bool AddSetReturnParamValuesStatements(IList statements) {
            return base.AddSetReturnParamValuesStatements(statements, CodeGenHelper.Variable(commandVariableName)); 
        }


        private bool AddReturnStatements(IList statements) { 
            int returnParamPos = GetReturnParameterPosition(activeCommand);
 
            if(this.methodSource.DbObjectType == DbObjectType.Function && this.methodSource.QueryType != QueryType.Scalar && returnParamPos >= 0) { 
                // if it's a function we return the return parameter
                //\\ if( typeof(command.Parameters[<returnParamPos>].Value) == System.DBNull ) { 
                //\\    return <SqlReturnType>.Null;
                //\\    or
                //\\    return System.DBNull;
                //\\    or 
                //\\    throw new StrongTypingException("StrongTyping_CannotAccessDBNull for parameter <paramName>");
                //\\ } 
                //\\ else { 
                //\\    return new <SqlReturnType> ( (CLRType) command.Parameters[<returnParamPos>].Value );
                //\\ } 
                DesignParameter returnParameter = (DesignParameter)activeCommand.Parameters[returnParamPos];
                Type returnType = GetParameterUrtType(returnParameter);

                //\\ <command>.Parameters[<returnParamPosition>].Value 
                CodeExpression returnParamExpression = CodeGenHelper.Property(
                    CodeGenHelper.Indexer( 
                        CodeGenHelper.Property( 
                            CodeGenHelper.Variable(commandVariableName),
                            "Parameters" 
                        ),
                        CodeGenHelper.Primitive(returnParamPos)
                    ),
                    "Value" 
                );
 
                CodeExpression isEqualDbNullCondition = CodeGenHelper.GenerateDbNullCheck(returnParamExpression); 

                CodeExpression nullExpression = CodeGenHelper.GenerateNullExpression(returnType); 
                CodeStatement trueStatement = null;
                if(nullExpression == null) {
                    if (returnParameter.AllowDbNull && returnType.IsValueType) {
                        //\\ return new System.Nullable<returnType>(); 
                        trueStatement = CodeGenHelper.Return(
                            CodeGenHelper.New( 
                                CodeGenHelper.NullableType(returnType), 
                                new CodeExpression[] { }
                            ) 
                        );
                    }
                    else if (returnParameter.AllowDbNull && !returnType.IsValueType) {
                        //\\ return null; 
                        trueStatement = CodeGenHelper.Return(
                            CodeGenHelper.Primitive(null) 
                        ); 
                    }
                    else { 
                        // in this case we can't return null
                        //\\ throw new StrongTypingException("StrongTyping_CannotAccessDBNull");
                        trueStatement = CodeGenHelper.Throw(
                            CodeGenHelper.GlobalType(typeof(System.Data.StrongTypingException)), 
                            SR.GetString(SR.CG_ParameterIsDBNull, ((DesignParameter)activeCommand.Parameters[returnParamPos]).ParameterName),
                            CodeGenHelper.Primitive(null) 
                        ); 
                    }
                } 
                else {
                    trueStatement = CodeGenHelper.Return(nullExpression);
                }
 
                CodeStatement falseStatement = null;
                if (returnParameter.AllowDbNull && returnType.IsValueType) { 
                    //\\ return new System.Nullable<returnType>((<returnType>) command.Parameter[i].Value); 
                    falseStatement = CodeGenHelper.Return(
                        CodeGenHelper.New( 
                            CodeGenHelper.NullableType(returnType),
                            new CodeExpression[] { CodeGenHelper.Cast(CodeGenHelper.GlobalType(returnType), returnParamExpression) }
                        )
                    ); 
                }
                else { 
                    CodeExpression convertExpression = CodeGenHelper.GenerateConvertExpression(returnParamExpression, typeof(System.Object), returnType); 
                    falseStatement = CodeGenHelper.Return(convertExpression);
                } 

                statements.Add(
                    CodeGenHelper.If(
                        isEqualDbNullCondition, 
                        trueStatement,
                        falseStatement 
                    ) 
                );
            } 
            else if (this.methodSource.QueryType == QueryType.Scalar) {
                //\\ if(returnValue is typeof(System.DbNull)) {
                //\\    return new System.Nullable<type>();
                //\\    OR 
                //\\    return null;
                //\\ } 
                //\\ else { 
                //\\    return new System.Nullable<type>((type) returnValue);
                //\\    OR 
                //\\    return (type) returnValue;
                //\\ }
                CodeExpression isEqualDbNullCondition = CodeGenHelper.GenerateDbNullCheck(CodeGenHelper.Variable(returnVariableName));
                CodeStatement trueStatement = null; 
                CodeStatement falseStatement = null;
 
                if (this.returnType.IsValueType) { 
                    trueStatement = CodeGenHelper.Return(
                        CodeGenHelper.New( 
                            CodeGenHelper.NullableType(returnType),
                            new CodeExpression[] { }
                        )
                    ); 

                    falseStatement = CodeGenHelper.Return( 
                        CodeGenHelper.New( 
                            CodeGenHelper.NullableType(this.returnType),
                            new CodeExpression[] { 
                                CodeGenHelper.Cast(
                                    CodeGenHelper.GlobalType(this.returnType),
                                    CodeGenHelper.Variable(returnVariableName)
                                ) 
                            }
                        ) 
                    ); 
                }
                else { 
                    trueStatement = CodeGenHelper.Return(CodeGenHelper.Primitive(null));
                    falseStatement = CodeGenHelper.Return(CodeGenHelper.Cast(CodeGenHelper.GlobalType(this.returnType), CodeGenHelper.Variable(returnVariableName)));
                }
 
                statements.Add(
                    CodeGenHelper.If( 
                        isEqualDbNullCondition, 
                        trueStatement,
                        falseStatement 
                    )
                );

 
            }
            else { 
                //\\ return returnValue; 
                statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable(returnVariableName)));
            } 

            return true;
        }
 
        private void AddCustomAttributesToMethod(CodeMemberMethod dbMethod) {
            if (this.methodSource.EnableWebMethods) { 
                CodeAttributeDeclaration wmAttribute = new CodeAttributeDeclaration("System.Web.Services.WebMethod"); 

                wmAttribute.Arguments.Add(new CodeAttributeArgument("Description", CodeGenHelper.Str(this.methodSource.WebMethodDescription))); 
                dbMethod.CustomAttributes.Add(wmAttribute);
            }

            DataObjectMethodType methodType = DataObjectMethodType.Select; 

            if (this.methodSource.CommandOperation == CommandOperation.Update) { 
                methodType = DataObjectMethodType.Update; 
            }
            else if (this.methodSource.CommandOperation == CommandOperation.Delete) { 
                methodType = DataObjectMethodType.Delete;
            }
            else if (this.methodSource.CommandOperation == CommandOperation.Insert) {
                methodType = DataObjectMethodType.Insert; 
            }
 
            if (methodType != DataObjectMethodType.Select) { 
                dbMethod.CustomAttributes.Add(
                    new CodeAttributeDeclaration( 
                        CodeGenHelper.GlobalType(typeof(System.ComponentModel.DataObjectMethodAttribute)),
                        new CodeAttributeArgument[] {
                            new CodeAttributeArgument(CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.ComponentModel.DataObjectMethodType)), methodType.ToString())),
                            new CodeAttributeArgument(CodeGenHelper.Primitive(false)) 
                        }
                    ) 
                ); 
            }
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
