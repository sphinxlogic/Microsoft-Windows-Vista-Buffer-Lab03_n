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
    using System.Data.SqlTypes;
    using System.Data.Odbc; 
    using System.Data.OleDb;
    using System.Data.OracleClient;

 
    internal enum MethodTypeEnum {
        ColumnParameters = 0, 
        GenericUpdate = 1 
    }
 
    internal class UpdateCommandGenerator : QueryGeneratorBase {

        private bool generateOverloadWithoutCurrentPKParameters;
 
        internal bool GenerateOverloadWithoutCurrentPKParameters {
            get { 
                return generateOverloadWithoutCurrentPKParameters; 
            }
            set { 
                generateOverloadWithoutCurrentPKParameters = value;
            }
        }
 
        internal UpdateCommandGenerator(TypedDataSourceCodeGenerator codeGenerator) : base(codeGenerator) {}
 
        internal override CodeMemberMethod Generate() { 
            if(this.methodSource == null) {
                throw new InternalException("MethodSource should not be null."); 
            }
            if(this.MethodType == MethodTypeEnum.ColumnParameters && this.activeCommand == null) {
                throw new InternalException("ActiveCommand should not be null.");
            } 

            // get attributes 
            this.methodAttributes = this.MethodSource.Modifier | MemberAttributes.Overloaded; 

            // set return type 
            this.returnType = typeof(int);

            // init the namehandler with the list of reserved strings
            CodeDomProvider codeProvider = (this.codeProvider != null) ? this.codeGenerator.CodeProvider : this.CodeProvider; 
            this.nameHandler = new GenericNameHandler(new string[] {this.MethodName, commandVariableName, returnVariableName}, codeProvider);
 
            // do the actual generation 
            return GenerateInternal();
        } 

        private CodeMemberMethod GenerateInternal() {
            // create the method declaration
            CodeMemberMethod dbMethod = null; 

            dbMethod = CodeGenHelper.MethodDecl(CodeGenHelper.Type(this.returnType), this.MethodName, this.methodAttributes); 
 
            // add help keyword attribute to method
            dbMethod.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.Design.HelpKeywordAttribute).FullName, CodeGenHelper.Str("vs.data.TableAdapter"))); 

            // add parameters to the method
            AddParametersToMethod(dbMethod);
 
            if(this.declarationOnly) {
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
            DesignConnection connection = (DesignConnection)this.methodSource.Connection; 

            if (connection == null) { 
                throw new InternalException(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Connection for query {0} is null.", this.methodSource.Name)); 
            }
 
            string paramPrefix = connection.ParameterPrefix;

            if (this.MethodType == MethodTypeEnum.ColumnParameters) {
                if(activeCommand.Parameters == null) { 
                    return;
                } 
 
                CodeParameterDeclarationExpression codeParam = null;
 
                foreach(DesignParameter parameter in this.activeCommand.Parameters) {
                    if(parameter.Direction == ParameterDirection.ReturnValue) {
                        // skip over return parameter
                        continue; 
                    }
                    if (parameter.SourceColumnNullMapping) { 
                        // skip over IsNull_<ColumnName> parameters 
                        continue;
                    } 
                    // Skip over current version PrimaryColumn parameter when GenerateOverloadWithoutCurrentPKParameters
                    // parameter.SourceColumn is the name that mapped to dataset column name
                    if (this.GenerateOverloadWithoutCurrentPKParameters
                        && parameter.SourceVersion == DataRowVersion.Current 
                        && this.IsPrimaryColumn(parameter.SourceColumn) ){
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
            else { 
                CodeParameterDeclarationExpression codeParam = null;
                string updateParamName = this.nameHandler.AddParameterNameToList(this.UpdateParameterName, paramPrefix); 
 
                if(this.UpdateParameterTypeName != null) {
                    codeParam = CodeGenHelper.ParameterDecl(CodeGenHelper.Type(this.UpdateParameterTypeName), updateParamName); 
                }
                else {
                    codeParam = CodeGenHelper.ParameterDecl(this.UpdateParameterTypeReference, updateParamName);
                } 

                dbMethod.Parameters.Add(codeParam); 
            } 
        }
 
        private bool AddStatementsToMethod(CodeMemberMethod dbMethod) {
            bool succeeded = true;
            if (this.GenerateOverloadWithoutCurrentPKParameters) {
                // Call the overload Update method with OriginalParameters 
                return AddCallOverloadUpdateStm(dbMethod);
            } 
 
            if(this.MethodType == MethodTypeEnum.ColumnParameters) {
                // Add statements to set parameters on command object 
                succeeded = AddSetParametersStatements(dbMethod.Statements);
                if(!succeeded) {
                    return false;
                } 
            }
 
            // Add statements to execute the command 
            succeeded = AddExecuteCommandStatements(dbMethod.Statements);
            if(!succeeded) { 
                return false;
            }

            if (this.MethodType == MethodTypeEnum.ColumnParameters) { 
                // Add statements to set output parameter values
                succeeded = AddSetReturnParamValuesStatements(dbMethod.Statements); 
                if (!succeeded) { 
                    return false;
                } 

                // Add return statements (used by Fill methods only)
                succeeded = AddReturnStatements(dbMethod.Statements);
                if (!succeeded) { 
                    return false;
                } 
            } 

            return true; 
        }

        /// <summary>
        /// </summary> 
        /// <param name="dbMethod"></param>
        /// <returns></returns> 
        private bool AddCallOverloadUpdateStm(CodeMemberMethod dbMethod) { 
            int paramCount = 0;
            if (this.activeCommand.Parameters != null) { 
                paramCount = this.activeCommand.Parameters.Count;
            }
            if (paramCount <= 0) {
                return false; 
            }
            System.Collections.Generic.List<CodeExpression> paramList = new System.Collections.Generic.List<CodeExpression>(); 
            bool canCreateOverload = false; 
            for (int i = 0; i < paramCount; i++) {
                DesignParameter parameter = activeCommand.Parameters[i] as DesignParameter; 
                if (parameter == null) {
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter.");
                }
                if (parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput) { 
                    if (parameter.SourceColumnNullMapping) {
                        // skip over IsNull_<ColumnName> parameters 
                        continue; 
                    }
                    // For CurrentVersion PK parameter, passing the OriginalVersion parameter 
                    // parameter.SourceColumn is the name that mapped to dataset column name
                    if (parameter.SourceVersion == DataRowVersion.Current &&
                        this.IsPrimaryColumn(parameter.SourceColumn) ){
                        parameter = GetOriginalVersionParameter(parameter); 
                        if (parameter != null) {
                            canCreateOverload = true; 
                        } 
                    }
                    if (parameter != null) { 
                        string parameterName = nameHandler.GetNameFromList(parameter.ParameterName);
                        paramList.Add(CodeGenHelper.Argument(parameterName));
                    }
                } 
            }
 
            if (!canCreateOverload) { 
                // No original parameters found, not generating the overload
                return false; 
            }

            CodeStatement retStm = CodeGenHelper.Return(
                CodeGenHelper.MethodCall(CodeGenHelper.This(), "Update", paramList.ToArray()) 
            );
            dbMethod.Statements.Add(retStm); 
            return true; 
        }
 
        /// <summary>
        /// Given a CurrentVersion parameter, find out its OriginalVersion parameter
        /// </summary>
        /// <param name="originalParameter"></param> 
        /// <returns></returns>
        private DesignParameter GetOriginalVersionParameter(DesignParameter currentVersionParameter) { 
            if (currentVersionParameter == null || currentVersionParameter.SourceVersion != DataRowVersion.Current) { 
                throw new InternalException("Invalid argutment currentVersionParameter");
            } 
            int paramCount = 0;
            if (this.activeCommand.Parameters != null) {
                paramCount = this.activeCommand.Parameters.Count;
            } 
            for (int i = 0; i < paramCount; i++) {
                DesignParameter parameter = activeCommand.Parameters[i] as DesignParameter; 
                if (parameter == null) { 
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter.");
                } 

                if (parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput) {
                    if (parameter.SourceColumnNullMapping) {
                        // skip over IsNull_<ColumnName> parameters 
                        continue;
                    } 
                    if ((parameter.SourceVersion == DataRowVersion.Original) && 
                        StringUtil.EqualValue(parameter.SourceColumn, currentVersionParameter.SourceColumn)){
                        return parameter; 
                    }
                }
            }
            return null; 
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
                    if (parameter.SourceColumnNullMapping) {
                        // skip over IsNull_<ColumnName> parameters 
                        continue;
                    } 
                    string parameterName = nameHandler.GetNameFromList(parameter.ParameterName); 
                    CodeExpression updateCmdExpression = CodeGenHelper.Property(
                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                        this.UpdateCommandName
                    );

                    DesignParameter isNullParameter = null; 
                    int isNullParameterIndex = 0;
 
                    if (parameter.SourceVersion == DataRowVersion.Original) { 
                        isNullParameter = this.FindCorrespondingIsNullParameter(parameter, out isNullParameterIndex);
                    } 

                    AddSetParameterStatements(parameter, parameterName, isNullParameter, updateCmdExpression, i, isNullParameterIndex, statements);
                }
            } 

            return true; 
        } 

        private bool AddExecuteCommandStatements(IList statements) { 
            if(this.MethodType == MethodTypeEnum.ColumnParameters) {
                CodeStatement[] tryStatements = new CodeStatement[1];
                CodeStatement[] finallyStatements = new CodeStatement[1];
                //\\ System.Data.ConnectionState previousConnectionState = command.Connection.State; 
                statements.Add(
                    CodeGenHelper.VariableDecl( 
                        CodeGenHelper.GlobalType(typeof(System.Data.ConnectionState)), 
                        this.nameHandler.AddNameToList("previousConnectionState"),
                        CodeGenHelper.Property( 
                            CodeGenHelper.Property(
                                CodeGenHelper.Property(
                                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                    this.UpdateCommandName 
                                ),
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
                                    CodeGenHelper.Property(
                                        CodeGenHelper.Property( 
                                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                            this.UpdateCommandName 
                                        ), 
                                        "Connection"
                                    ), 
                                    "State"
                                ),
                                CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Open")
                            ), 
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Open")
                        ), 
                        CodeGenHelper.Stm( 
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property( 
                                    CodeGenHelper.Property(
                                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                        this.UpdateCommandName
                                    ), 
                                    "Connection"
                                ), 
                                "Open" 
                            )
                        ) 
                    )
                );

                //\\ int retVal = command.ExecuteNonQuery(); 
                tryStatements[0] = CodeGenHelper.VariableDecl(
                                    CodeGenHelper.GlobalType(typeof(int)), 
                                    returnVariableName, 
                                    CodeGenHelper.MethodCall(
                                        CodeGenHelper.Property( 
                                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                            this.UpdateCommandName
                                        ),
                                        "ExecuteNonQuery", 
                                        new CodeExpression[] {}
                                    ) 
                ); 

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
                            CodeGenHelper.Property(
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                this.UpdateCommandName
                            ), 
                            "Connection"
                        ), 
                        "Close" 
                    ))
                ); 

                statements.Add(CodeGenHelper.Try(tryStatements, new CodeCatchClause[] {}, finallyStatements));
            }
            else { 
                if (StringUtil.EqualValue(this.UpdateParameterTypeReference.BaseType, typeof(System.Data.DataRow).FullName)
                    && this.UpdateParameterTypeReference.ArrayRank == 0) { 
                    //\\ return this.m_adapter.Update(new System.Data.DataRow[] {dataRow}); 
                    statements.Add(
                        CodeGenHelper.Return( 
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                "Update",
                                new CodeExpression[] { 
                                    CodeGenHelper.NewArray(this.UpdateParameterTypeReference, CodeGenHelper.Argument(this.UpdateParameterName))
                                } 
                            ) 
                        )
                    ); 
                }
                else if (StringUtil.EqualValue(this.UpdateParameterTypeReference.BaseType, typeof(System.Data.DataSet).FullName)) {
                    //\\ return this.m_adapter.Update(dataSet, <tableName>);
                    statements.Add( 
                        CodeGenHelper.Return(
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                                "Update",
                                new CodeExpression[] { CodeGenHelper.Argument(this.UpdateParameterName), CodeGenHelper.Str(this.DesignTable.Name) } 
                            )
                        )
                    );
                } 
                else {
                    //\\ return this.m_adapter.Update(dataTable); 
                    //\\ OR 
                    //\\ return this.m_adapter.Update(dataRows);
                    statements.Add( 
                        CodeGenHelper.Return(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                "Update", 
                                new CodeExpression[] { CodeGenHelper.Argument(this.UpdateParameterName) }
                            ) 
                        ) 
                    );
                } 
            }

            return true;
        } 

        protected bool AddSetReturnParamValuesStatements(IList statements) { 
            //\\ this.Adapter.UpdateCommand 
            CodeExpression commandExpression =
                CodeGenHelper.Property( 
                    CodeGenHelper.Property(
                        CodeGenHelper.This(),
                        DataComponentNameHandler.AdapterPropertyName
                    ), 
                    this.UpdateCommandName
                ); 
 
            CodeTryCatchFinallyStatement tryCatchStmt = (CodeTryCatchFinallyStatement)statements[statements.Count - 1];
 
            return base.AddSetReturnParamValuesStatements(tryCatchStmt.TryStatements, commandExpression);
        }

        private bool AddReturnStatements(IList statements) { 
            CodeTryCatchFinallyStatement tryCatchStmt = (CodeTryCatchFinallyStatement)statements[statements.Count - 1];
 
            //\\ return retVal; 
            tryCatchStmt.TryStatements.Add(CodeGenHelper.Return(CodeGenHelper.Variable(returnVariableName)));
 
            return true;
        }

 
        private void AddCustomAttributesToMethod(CodeMemberMethod dbMethod) {
            DataObjectMethodType methodType = DataObjectMethodType.Update; 
 
            if (this.methodSource.EnableWebMethods) {
                CodeAttributeDeclaration wmAttribute = new CodeAttributeDeclaration("System.Web.Services.WebMethod"); 

                wmAttribute.Arguments.Add(new CodeAttributeArgument("Description", CodeGenHelper.Str(this.methodSource.WebMethodDescription)));
                dbMethod.CustomAttributes.Add(wmAttribute);
            } 

            if (this.MethodType == MethodTypeEnum.GenericUpdate) { 
                // we don't generate any attributes for the generic update 
                return;
            } 
            else {
                if(this.activeCommand == this.methodSource.DeleteCommand) {
                    methodType = DataObjectMethodType.Delete;
                } 
                else if (this.activeCommand == this.methodSource.InsertCommand) {
                    methodType = DataObjectMethodType.Insert; 
                } 
                else if (this.activeCommand == this.methodSource.UpdateCommand) {
                    methodType = DataObjectMethodType.Update; 
                }
            }

            dbMethod.CustomAttributes.Add( 
                new CodeAttributeDeclaration(
                    CodeGenHelper.GlobalType(typeof(System.ComponentModel.DataObjectMethodAttribute)), 
                    new CodeAttributeArgument[] { 
                        new CodeAttributeArgument(CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.ComponentModel.DataObjectMethodType)), methodType.ToString())),
                        new CodeAttributeArgument(CodeGenHelper.Primitive(true)) 
                    }
                )
            );
        } 

        private DesignParameter FindCorrespondingIsNullParameter(DesignParameter originalParameter, out int isNullParameterIndex) { 
            if (originalParameter == null || originalParameter.SourceVersion != DataRowVersion.Original || originalParameter.SourceColumnNullMapping) { 
                throw new InternalException("'originalParameter' is not valid.");
            } 

            isNullParameterIndex = 0;
            for (int i = 0; i < this.activeCommand.Parameters.Count; i++) {
                DesignParameter parameter = this.activeCommand.Parameters[i] as DesignParameter; 
                if (parameter == null) {
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter."); 
                } 

                if (parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput) { 
                    if (!parameter.SourceColumnNullMapping || parameter.SourceVersion != DataRowVersion.Original) {
                        // we only take into account IsNull_<ColumnName> parameters here
                        continue;
                    } 
                }
 
                if (StringUtil.EqualValue(originalParameter.SourceColumn, parameter.SourceColumn)) { 
                    isNullParameterIndex = i;
                    return parameter; 
                }
            }

            return null; 
        }
 
        private bool IsPrimaryColumn(string columnName) { 
            DataColumn[] pkColumns = this.DesignTable.PrimaryKeyColumns;
            if (pkColumns == null || pkColumns.Length == 0) { 
                return false;
            }
            foreach (DataColumn column in pkColumns) {
                if (StringUtil.EqualValue(column.ColumnName, columnName)) { 
                    return true;
                } 
            } 
            return false;
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
    using System.Data.SqlTypes;
    using System.Data.Odbc; 
    using System.Data.OleDb;
    using System.Data.OracleClient;

 
    internal enum MethodTypeEnum {
        ColumnParameters = 0, 
        GenericUpdate = 1 
    }
 
    internal class UpdateCommandGenerator : QueryGeneratorBase {

        private bool generateOverloadWithoutCurrentPKParameters;
 
        internal bool GenerateOverloadWithoutCurrentPKParameters {
            get { 
                return generateOverloadWithoutCurrentPKParameters; 
            }
            set { 
                generateOverloadWithoutCurrentPKParameters = value;
            }
        }
 
        internal UpdateCommandGenerator(TypedDataSourceCodeGenerator codeGenerator) : base(codeGenerator) {}
 
        internal override CodeMemberMethod Generate() { 
            if(this.methodSource == null) {
                throw new InternalException("MethodSource should not be null."); 
            }
            if(this.MethodType == MethodTypeEnum.ColumnParameters && this.activeCommand == null) {
                throw new InternalException("ActiveCommand should not be null.");
            } 

            // get attributes 
            this.methodAttributes = this.MethodSource.Modifier | MemberAttributes.Overloaded; 

            // set return type 
            this.returnType = typeof(int);

            // init the namehandler with the list of reserved strings
            CodeDomProvider codeProvider = (this.codeProvider != null) ? this.codeGenerator.CodeProvider : this.CodeProvider; 
            this.nameHandler = new GenericNameHandler(new string[] {this.MethodName, commandVariableName, returnVariableName}, codeProvider);
 
            // do the actual generation 
            return GenerateInternal();
        } 

        private CodeMemberMethod GenerateInternal() {
            // create the method declaration
            CodeMemberMethod dbMethod = null; 

            dbMethod = CodeGenHelper.MethodDecl(CodeGenHelper.Type(this.returnType), this.MethodName, this.methodAttributes); 
 
            // add help keyword attribute to method
            dbMethod.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.Design.HelpKeywordAttribute).FullName, CodeGenHelper.Str("vs.data.TableAdapter"))); 

            // add parameters to the method
            AddParametersToMethod(dbMethod);
 
            if(this.declarationOnly) {
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
            DesignConnection connection = (DesignConnection)this.methodSource.Connection; 

            if (connection == null) { 
                throw new InternalException(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Connection for query {0} is null.", this.methodSource.Name)); 
            }
 
            string paramPrefix = connection.ParameterPrefix;

            if (this.MethodType == MethodTypeEnum.ColumnParameters) {
                if(activeCommand.Parameters == null) { 
                    return;
                } 
 
                CodeParameterDeclarationExpression codeParam = null;
 
                foreach(DesignParameter parameter in this.activeCommand.Parameters) {
                    if(parameter.Direction == ParameterDirection.ReturnValue) {
                        // skip over return parameter
                        continue; 
                    }
                    if (parameter.SourceColumnNullMapping) { 
                        // skip over IsNull_<ColumnName> parameters 
                        continue;
                    } 
                    // Skip over current version PrimaryColumn parameter when GenerateOverloadWithoutCurrentPKParameters
                    // parameter.SourceColumn is the name that mapped to dataset column name
                    if (this.GenerateOverloadWithoutCurrentPKParameters
                        && parameter.SourceVersion == DataRowVersion.Current 
                        && this.IsPrimaryColumn(parameter.SourceColumn) ){
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
            else { 
                CodeParameterDeclarationExpression codeParam = null;
                string updateParamName = this.nameHandler.AddParameterNameToList(this.UpdateParameterName, paramPrefix); 
 
                if(this.UpdateParameterTypeName != null) {
                    codeParam = CodeGenHelper.ParameterDecl(CodeGenHelper.Type(this.UpdateParameterTypeName), updateParamName); 
                }
                else {
                    codeParam = CodeGenHelper.ParameterDecl(this.UpdateParameterTypeReference, updateParamName);
                } 

                dbMethod.Parameters.Add(codeParam); 
            } 
        }
 
        private bool AddStatementsToMethod(CodeMemberMethod dbMethod) {
            bool succeeded = true;
            if (this.GenerateOverloadWithoutCurrentPKParameters) {
                // Call the overload Update method with OriginalParameters 
                return AddCallOverloadUpdateStm(dbMethod);
            } 
 
            if(this.MethodType == MethodTypeEnum.ColumnParameters) {
                // Add statements to set parameters on command object 
                succeeded = AddSetParametersStatements(dbMethod.Statements);
                if(!succeeded) {
                    return false;
                } 
            }
 
            // Add statements to execute the command 
            succeeded = AddExecuteCommandStatements(dbMethod.Statements);
            if(!succeeded) { 
                return false;
            }

            if (this.MethodType == MethodTypeEnum.ColumnParameters) { 
                // Add statements to set output parameter values
                succeeded = AddSetReturnParamValuesStatements(dbMethod.Statements); 
                if (!succeeded) { 
                    return false;
                } 

                // Add return statements (used by Fill methods only)
                succeeded = AddReturnStatements(dbMethod.Statements);
                if (!succeeded) { 
                    return false;
                } 
            } 

            return true; 
        }

        /// <summary>
        /// </summary> 
        /// <param name="dbMethod"></param>
        /// <returns></returns> 
        private bool AddCallOverloadUpdateStm(CodeMemberMethod dbMethod) { 
            int paramCount = 0;
            if (this.activeCommand.Parameters != null) { 
                paramCount = this.activeCommand.Parameters.Count;
            }
            if (paramCount <= 0) {
                return false; 
            }
            System.Collections.Generic.List<CodeExpression> paramList = new System.Collections.Generic.List<CodeExpression>(); 
            bool canCreateOverload = false; 
            for (int i = 0; i < paramCount; i++) {
                DesignParameter parameter = activeCommand.Parameters[i] as DesignParameter; 
                if (parameter == null) {
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter.");
                }
                if (parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput) { 
                    if (parameter.SourceColumnNullMapping) {
                        // skip over IsNull_<ColumnName> parameters 
                        continue; 
                    }
                    // For CurrentVersion PK parameter, passing the OriginalVersion parameter 
                    // parameter.SourceColumn is the name that mapped to dataset column name
                    if (parameter.SourceVersion == DataRowVersion.Current &&
                        this.IsPrimaryColumn(parameter.SourceColumn) ){
                        parameter = GetOriginalVersionParameter(parameter); 
                        if (parameter != null) {
                            canCreateOverload = true; 
                        } 
                    }
                    if (parameter != null) { 
                        string parameterName = nameHandler.GetNameFromList(parameter.ParameterName);
                        paramList.Add(CodeGenHelper.Argument(parameterName));
                    }
                } 
            }
 
            if (!canCreateOverload) { 
                // No original parameters found, not generating the overload
                return false; 
            }

            CodeStatement retStm = CodeGenHelper.Return(
                CodeGenHelper.MethodCall(CodeGenHelper.This(), "Update", paramList.ToArray()) 
            );
            dbMethod.Statements.Add(retStm); 
            return true; 
        }
 
        /// <summary>
        /// Given a CurrentVersion parameter, find out its OriginalVersion parameter
        /// </summary>
        /// <param name="originalParameter"></param> 
        /// <returns></returns>
        private DesignParameter GetOriginalVersionParameter(DesignParameter currentVersionParameter) { 
            if (currentVersionParameter == null || currentVersionParameter.SourceVersion != DataRowVersion.Current) { 
                throw new InternalException("Invalid argutment currentVersionParameter");
            } 
            int paramCount = 0;
            if (this.activeCommand.Parameters != null) {
                paramCount = this.activeCommand.Parameters.Count;
            } 
            for (int i = 0; i < paramCount; i++) {
                DesignParameter parameter = activeCommand.Parameters[i] as DesignParameter; 
                if (parameter == null) { 
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter.");
                } 

                if (parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput) {
                    if (parameter.SourceColumnNullMapping) {
                        // skip over IsNull_<ColumnName> parameters 
                        continue;
                    } 
                    if ((parameter.SourceVersion == DataRowVersion.Original) && 
                        StringUtil.EqualValue(parameter.SourceColumn, currentVersionParameter.SourceColumn)){
                        return parameter; 
                    }
                }
            }
            return null; 
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
                    if (parameter.SourceColumnNullMapping) {
                        // skip over IsNull_<ColumnName> parameters 
                        continue;
                    } 
                    string parameterName = nameHandler.GetNameFromList(parameter.ParameterName); 
                    CodeExpression updateCmdExpression = CodeGenHelper.Property(
                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                        this.UpdateCommandName
                    );

                    DesignParameter isNullParameter = null; 
                    int isNullParameterIndex = 0;
 
                    if (parameter.SourceVersion == DataRowVersion.Original) { 
                        isNullParameter = this.FindCorrespondingIsNullParameter(parameter, out isNullParameterIndex);
                    } 

                    AddSetParameterStatements(parameter, parameterName, isNullParameter, updateCmdExpression, i, isNullParameterIndex, statements);
                }
            } 

            return true; 
        } 

        private bool AddExecuteCommandStatements(IList statements) { 
            if(this.MethodType == MethodTypeEnum.ColumnParameters) {
                CodeStatement[] tryStatements = new CodeStatement[1];
                CodeStatement[] finallyStatements = new CodeStatement[1];
                //\\ System.Data.ConnectionState previousConnectionState = command.Connection.State; 
                statements.Add(
                    CodeGenHelper.VariableDecl( 
                        CodeGenHelper.GlobalType(typeof(System.Data.ConnectionState)), 
                        this.nameHandler.AddNameToList("previousConnectionState"),
                        CodeGenHelper.Property( 
                            CodeGenHelper.Property(
                                CodeGenHelper.Property(
                                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                    this.UpdateCommandName 
                                ),
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
                                    CodeGenHelper.Property(
                                        CodeGenHelper.Property( 
                                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                            this.UpdateCommandName 
                                        ), 
                                        "Connection"
                                    ), 
                                    "State"
                                ),
                                CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Open")
                            ), 
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Open")
                        ), 
                        CodeGenHelper.Stm( 
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property( 
                                    CodeGenHelper.Property(
                                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                        this.UpdateCommandName
                                    ), 
                                    "Connection"
                                ), 
                                "Open" 
                            )
                        ) 
                    )
                );

                //\\ int retVal = command.ExecuteNonQuery(); 
                tryStatements[0] = CodeGenHelper.VariableDecl(
                                    CodeGenHelper.GlobalType(typeof(int)), 
                                    returnVariableName, 
                                    CodeGenHelper.MethodCall(
                                        CodeGenHelper.Property( 
                                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                            this.UpdateCommandName
                                        ),
                                        "ExecuteNonQuery", 
                                        new CodeExpression[] {}
                                    ) 
                ); 

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
                            CodeGenHelper.Property(
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                this.UpdateCommandName
                            ), 
                            "Connection"
                        ), 
                        "Close" 
                    ))
                ); 

                statements.Add(CodeGenHelper.Try(tryStatements, new CodeCatchClause[] {}, finallyStatements));
            }
            else { 
                if (StringUtil.EqualValue(this.UpdateParameterTypeReference.BaseType, typeof(System.Data.DataRow).FullName)
                    && this.UpdateParameterTypeReference.ArrayRank == 0) { 
                    //\\ return this.m_adapter.Update(new System.Data.DataRow[] {dataRow}); 
                    statements.Add(
                        CodeGenHelper.Return( 
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                "Update",
                                new CodeExpression[] { 
                                    CodeGenHelper.NewArray(this.UpdateParameterTypeReference, CodeGenHelper.Argument(this.UpdateParameterName))
                                } 
                            ) 
                        )
                    ); 
                }
                else if (StringUtil.EqualValue(this.UpdateParameterTypeReference.BaseType, typeof(System.Data.DataSet).FullName)) {
                    //\\ return this.m_adapter.Update(dataSet, <tableName>);
                    statements.Add( 
                        CodeGenHelper.Return(
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                                "Update",
                                new CodeExpression[] { CodeGenHelper.Argument(this.UpdateParameterName), CodeGenHelper.Str(this.DesignTable.Name) } 
                            )
                        )
                    );
                } 
                else {
                    //\\ return this.m_adapter.Update(dataTable); 
                    //\\ OR 
                    //\\ return this.m_adapter.Update(dataRows);
                    statements.Add( 
                        CodeGenHelper.Return(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                "Update", 
                                new CodeExpression[] { CodeGenHelper.Argument(this.UpdateParameterName) }
                            ) 
                        ) 
                    );
                } 
            }

            return true;
        } 

        protected bool AddSetReturnParamValuesStatements(IList statements) { 
            //\\ this.Adapter.UpdateCommand 
            CodeExpression commandExpression =
                CodeGenHelper.Property( 
                    CodeGenHelper.Property(
                        CodeGenHelper.This(),
                        DataComponentNameHandler.AdapterPropertyName
                    ), 
                    this.UpdateCommandName
                ); 
 
            CodeTryCatchFinallyStatement tryCatchStmt = (CodeTryCatchFinallyStatement)statements[statements.Count - 1];
 
            return base.AddSetReturnParamValuesStatements(tryCatchStmt.TryStatements, commandExpression);
        }

        private bool AddReturnStatements(IList statements) { 
            CodeTryCatchFinallyStatement tryCatchStmt = (CodeTryCatchFinallyStatement)statements[statements.Count - 1];
 
            //\\ return retVal; 
            tryCatchStmt.TryStatements.Add(CodeGenHelper.Return(CodeGenHelper.Variable(returnVariableName)));
 
            return true;
        }

 
        private void AddCustomAttributesToMethod(CodeMemberMethod dbMethod) {
            DataObjectMethodType methodType = DataObjectMethodType.Update; 
 
            if (this.methodSource.EnableWebMethods) {
                CodeAttributeDeclaration wmAttribute = new CodeAttributeDeclaration("System.Web.Services.WebMethod"); 

                wmAttribute.Arguments.Add(new CodeAttributeArgument("Description", CodeGenHelper.Str(this.methodSource.WebMethodDescription)));
                dbMethod.CustomAttributes.Add(wmAttribute);
            } 

            if (this.MethodType == MethodTypeEnum.GenericUpdate) { 
                // we don't generate any attributes for the generic update 
                return;
            } 
            else {
                if(this.activeCommand == this.methodSource.DeleteCommand) {
                    methodType = DataObjectMethodType.Delete;
                } 
                else if (this.activeCommand == this.methodSource.InsertCommand) {
                    methodType = DataObjectMethodType.Insert; 
                } 
                else if (this.activeCommand == this.methodSource.UpdateCommand) {
                    methodType = DataObjectMethodType.Update; 
                }
            }

            dbMethod.CustomAttributes.Add( 
                new CodeAttributeDeclaration(
                    CodeGenHelper.GlobalType(typeof(System.ComponentModel.DataObjectMethodAttribute)), 
                    new CodeAttributeArgument[] { 
                        new CodeAttributeArgument(CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.ComponentModel.DataObjectMethodType)), methodType.ToString())),
                        new CodeAttributeArgument(CodeGenHelper.Primitive(true)) 
                    }
                )
            );
        } 

        private DesignParameter FindCorrespondingIsNullParameter(DesignParameter originalParameter, out int isNullParameterIndex) { 
            if (originalParameter == null || originalParameter.SourceVersion != DataRowVersion.Original || originalParameter.SourceColumnNullMapping) { 
                throw new InternalException("'originalParameter' is not valid.");
            } 

            isNullParameterIndex = 0;
            for (int i = 0; i < this.activeCommand.Parameters.Count; i++) {
                DesignParameter parameter = this.activeCommand.Parameters[i] as DesignParameter; 
                if (parameter == null) {
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter."); 
                } 

                if (parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput) { 
                    if (!parameter.SourceColumnNullMapping || parameter.SourceVersion != DataRowVersion.Original) {
                        // we only take into account IsNull_<ColumnName> parameters here
                        continue;
                    } 
                }
 
                if (StringUtil.EqualValue(originalParameter.SourceColumn, parameter.SourceColumn)) { 
                    isNullParameterIndex = i;
                    return parameter; 
                }
            }

            return null; 
        }
 
        private bool IsPrimaryColumn(string columnName) { 
            DataColumn[] pkColumns = this.DesignTable.PrimaryKeyColumns;
            if (pkColumns == null || pkColumns.Length == 0) { 
                return false;
            }
            foreach (DataColumn column in pkColumns) {
                if (StringUtil.EqualValue(column.ColumnName, columnName)) { 
                    return true;
                } 
            } 
            return false;
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
