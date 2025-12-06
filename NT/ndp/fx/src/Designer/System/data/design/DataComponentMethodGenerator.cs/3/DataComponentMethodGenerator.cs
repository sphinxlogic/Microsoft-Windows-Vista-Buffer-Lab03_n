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
    using System.Design; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Reflection;


    internal sealed class DataComponentMethodGenerator { 
        private TypedDataSourceCodeGenerator codeGenerator = null;
        private DesignTable designTable = null; 
        private DbProviderFactory providerFactory = null; 
        // C++ and J# do not want the new feature in Orcas, we will keep them unchanged
        private bool generateHierarchicalUpdate = false; 

        internal DataComponentMethodGenerator(TypedDataSourceCodeGenerator codeGenerator, DesignTable designTable, bool generateHierarchicalUpdate) {
            this.generateHierarchicalUpdate = generateHierarchicalUpdate;
            this.codeGenerator = codeGenerator; 
            this.designTable = designTable;
 
            if (designTable.Connection != null) { 
                this.providerFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
            } 
        }

        internal void AddMethods(CodeTypeDeclaration dataComponentClass, bool isFunctionsDataComponent) {
            if(dataComponentClass == null) { 
                throw new InternalException("dataComponent CodeTypeDeclaration should not be null.");
            } 
 
            if (isFunctionsDataComponent) {
                AddCommandCollectionMembers(dataComponentClass, true /*isFunctionsDataComponent*/); 
                AddInitCommandCollection(dataComponentClass, true /*isFunctionsDataComponent*/);
            }
            else {
                if (this.designTable.Connection == null || this.providerFactory == null) { 
                    return;
                } 
 
                // Add methods to the class
                AddConstructor(dataComponentClass); 

                //if (this.designTable.MainSource != null && ((DbSource)this.designTable.MainSource).GenerateShortCommands) {
                //    AddShortUpdateCommandMembers(dataComponentClass);
                //    AddInitShortUpdateCommands(dataComponentClass); 
                //}
 
                AddAdapterMembers(dataComponentClass); 
                AddInitAdapter(dataComponentClass);
 
                AddConnectionMembers(dataComponentClass);
                AddInitConnection(dataComponentClass);

                if (generateHierarchicalUpdate) { 
                    AddTransactionMembers(dataComponentClass);
                } 
 
                AddCommandCollectionMembers(dataComponentClass, false /*isFunctionsComponent*/);
                AddInitCommandCollection(dataComponentClass, false /*isFunctionsDataComponent*/); 

                AddClearBeforeFillMembers(dataComponentClass);

                //AddArgumentLessConstructor(dataComponentClass, false /*skipMain*/); 
            }
        } 
 
        private void AddConstructor(CodeTypeDeclaration dataComponentClass) {
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Public); 
            constructor.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ClearBeforeFillPropertyName),
                    CodeGenHelper.Primitive(true) 
                )
            ); 
 
            dataComponentClass.Members.Add(constructor);
        } 

/*        private void AddShortUpdateCommandMembers(CodeTypeDeclaration dataComponentClass) {
            Type commandType = this.providerFactory.CreateCommand().GetType();
            CodeMemberProperty commandProperty = null; 

            if (((DbSource)this.designTable.MainSource).DeleteCommand != null) { 
                dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(commandType, DataComponentNameHandler.ShortDeleteCmdVariableName)); 
                // private SqlCommand DeleteCommand {
                //     get { 
                //         if(this.deleteCmd == null) {
                //             this.InitDeleteCmd();
                //         }
                //         return this.deleteCmd; 
                //     }
                // } 
                commandProperty = CodeGenHelper.PropertyDecl(commandType, DataComponentNameHandler.ShortDeleteCmdPropertyName, MemberAttributes.Private | MemberAttributes.Final); 
                commandProperty.GetStatements.Add(
                    CodeGenHelper.If( 
                        CodeGenHelper.IdEQ(
                            CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortDeleteCmdVariableName),
                            CodeGenHelper.Primitive(null)
                        ), 
                        new CodeStatement[] {
                            CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitShortDeleteCmd, new CodeExpression[] {})) 
                        } 
                    )
                ); 
                commandProperty.GetStatements.Add(
                    CodeGenHelper.Return(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortDeleteCmdVariableName)
                    ) 
                );
 
                dataComponentClass.Members.Add(commandProperty); 
                this.shortDeleteAdded = true;
            } 

            if (((DbSource)this.designTable.MainSource).InsertCommand != null) {
                dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(commandType, DataComponentNameHandler.ShortInsertCmdVariableName));
                // private SqlCommand InsertCommand { 
                //     get {
                //         if(this.insertCmd == null) { 
                //             this.InitInsertCmd(); 
                //         }
                //         return this.insertCmd; 
                //     }
                // }
                commandProperty = CodeGenHelper.PropertyDecl(commandType, DataComponentNameHandler.ShortInsertCmdPropertyName, MemberAttributes.Private | MemberAttributes.Final);
                commandProperty.GetStatements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdEQ( 
                            CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortInsertCmdVariableName), 
                            CodeGenHelper.Primitive(null)
                        ), 
                        new CodeStatement[] {
                            CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitShortInsertCmd, new CodeExpression[] {}))
                        }
                    ) 
                );
                commandProperty.GetStatements.Add( 
                    CodeGenHelper.Return( 
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortInsertCmdVariableName)
                    ) 
                );

                dataComponentClass.Members.Add(commandProperty);
                this.shortInsertAdded = true; 
            }
 
            if (((DbSource)this.designTable.MainSource).UpdateCommand != null) { 
                dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(commandType, DataComponentNameHandler.ShortUpdateCmdVariableName));
                // private SqlCommand UpdateCommand { 
                //     get {
                //         if(this.updateCmd == null) {
                //             this.InitUpdateCmd();
                //         } 
                //         return this.updateCmd;
                //     } 
                // } 
                commandProperty = CodeGenHelper.PropertyDecl(commandType, DataComponentNameHandler.ShortUpdateCmdPropertyName, MemberAttributes.Private | MemberAttributes.Final);
                commandProperty.GetStatements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdEQ(
                            CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortUpdateCmdVariableName),
                            CodeGenHelper.Primitive(null) 
                        ),
                        new CodeStatement[] { 
                            CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitShortUpdateCmd, new CodeExpression[] {})) 
                        }
                    ) 
                );
                commandProperty.GetStatements.Add(
                    CodeGenHelper.Return(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortUpdateCmdVariableName) 
                    )
                ); 
 
                dataComponentClass.Members.Add(commandProperty);
                this.shortUpdateAdded = true; 
            }
        }

        private void AddInitShortUpdateCommands(CodeTypeDeclaration dataComponentClass) { 
            CodeMemberMethod initMethod = null;
 
            DbSourceCommand shortDeleteCommand = ((DbSource)this.designTable.MainSource).DeleteCommand; 
            if (shortDeleteCommand != null) {
                initMethod = CodeGenHelper.MethodDecl(typeof(void), DataComponentNameHandler.InitShortDeleteCmd, MemberAttributes.Private | MemberAttributes.Final); 
                AddSetCommandStatements(initMethod.Statements, DataComponentNameHandler.ShortDeleteCmdVariableName, shortDeleteCommand);
                dataComponentClass.Members.Add(initMethod);
            }
 
            DbSourceCommand shortInsertCommand = ((DbSource)this.designTable.MainSource).InsertCommand;
            if (shortInsertCommand != null) { 
                initMethod = CodeGenHelper.MethodDecl(typeof(void), DataComponentNameHandler.InitShortInsertCmd, MemberAttributes.Private | MemberAttributes.Final); 
                AddSetCommandStatements(initMethod.Statements, DataComponentNameHandler.ShortInsertCmdVariableName, shortInsertCommand);
                dataComponentClass.Members.Add(initMethod); 
            }

            DbSourceCommand shortUpdateCommand = ((DbSource)this.designTable.MainSource).UpdateCommand;
            if (shortUpdateCommand != null) { 
                initMethod = CodeGenHelper.MethodDecl(typeof(void), DataComponentNameHandler.InitShortUpdateCmd, MemberAttributes.Private | MemberAttributes.Final);
                AddSetCommandStatements(initMethod.Statements, DataComponentNameHandler.ShortUpdateCmdVariableName, shortUpdateCommand); 
                dataComponentClass.Members.Add(initMethod); 
            }
        } 

        private void AddSetCommandStatements(IList statements, string commandVariableName, DbSourceCommand activeCommand) {
            Type commandType = this.providerFactory.CreateCommand().GetType();
            Type parameterType = this.providerFactory.CreateParameter().GetType(); 
            CodeExpression parameterVariable = null;
            CodeExpression commandExpression = CodeGenHelper.Field(CodeGenHelper.This(), commandVariableName); 
 
            //\\ this.deleteCommand = new SqlCommand();
            statements.Add( 
                CodeGenHelper.Assign(
                    commandExpression,
                    CodeGenHelper.New(commandType, new CodeExpression[] {})
                ) 
            );
 
            //\\ this.deleteCommand.Connection = this.Connection; 
            statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(commandExpression, "Connection"),
                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionPropertyName)
                )
            ); 

            //\\ this.deleteCommand.CommandText = <CommandText>; 
            statements.Add(QueryGenerator.SetCommandTextStatement(commandExpression, activeCommand.CommandText)); 
            //\\ this.deleteCommand.CommandType = <CommandType>;
            statements.Add(QueryGenerator.SetCommandTypeStatement(commandExpression, activeCommand.CommandType)); 

            if (activeCommand.Parameters != null) {
                foreach (DesignParameter parameter in activeCommand.Parameters) {
                    //\\ command.Parameters.Add(new SqlParameter("<parameterName>", <Type>, <Size>, <Direction>, 
                    //\\        <isNullable>, <precision>, <scale>, <sourceColumn>, <DataRowVersion>, <Value> ));
                    parameterVariable = QueryGenerator.AddNewParameterStatements(parameter, parameterType, this.providerFactory, statements, parameterVariable); 
                    statements.Add( 
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Property(commandExpression, "Parameters"),
                                "Add",
                                new CodeExpression[] { parameterVariable }
                            ) 
                        )
                    ); 
                } 
            }
        } 
*/
        private void AddAdapterMembers(CodeTypeDeclaration dataComponentClass) {
            Type adapterType = this.providerFactory.CreateDataAdapter().GetType();
            CodeMemberField adapterVariable = CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(adapterType), DataComponentNameHandler.AdapterVariableName); 
            adapterVariable.UserData.Add("WithEvents", true);
 
            dataComponentClass.Members.Add(adapterVariable); 
            CodeMemberProperty adapterProperty = null;
            if (generateHierarchicalUpdate){ 
                adapterProperty = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(adapterType), DataComponentNameHandler.AdapterPropertyName, MemberAttributes.FamilyOrAssembly | MemberAttributes.Final);
            }
            else {
                adapterProperty = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(adapterType), DataComponentNameHandler.AdapterPropertyName, MemberAttributes.Private | MemberAttributes.Final); 
            }
            adapterProperty.GetStatements.Add( 
                CodeGenHelper.If( 
                    CodeGenHelper.IdEQ(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName), 
                        CodeGenHelper.Primitive(null)
                    ),
                    new CodeStatement[] {
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitAdapter, new CodeExpression[] {})) 
                    }
                ) 
            ); 
            adapterProperty.GetStatements.Add(
                CodeGenHelper.Return( 
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName)
                )
            );
 
            dataComponentClass.Members.Add(adapterProperty);
        } 
 
        private void AddInitAdapter(CodeTypeDeclaration dataComponentClass) {
            CodeMemberMethod initMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), DataComponentNameHandler.InitAdapter, MemberAttributes.Private | MemberAttributes.Final); 

            //\\ this.m_adapter = new System.Data.SqlClient.SqlDataAdapter();
            initMethod.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName),
                    CodeGenHelper.New(CodeGenHelper.GlobalType(this.providerFactory.CreateDataAdapter().GetType()), new CodeExpression[] {}) 
                ) 
            );
            if (designTable.Mappings != null && designTable.Mappings.Count > 0) { 
                //\\ DataTableMapping tableMapping = new DataTableMapping();
                initMethod.Statements.Add(
                    CodeGenHelper.VariableDecl(
                        CodeGenHelper.GlobalType(typeof(System.Data.Common.DataTableMapping)), 
                        "tableMapping",
                        CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Data.Common.DataTableMapping)), new CodeExpression[] {}) 
                    ) 
                );
 
                //\\ tableMapping.SourceTable = "Table"
                initMethod.Statements.Add(
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(CodeGenHelper.Variable("tableMapping"), "SourceTable"), 
                        CodeGenHelper.Str("Table")
                    ) 
                ); 
                //\\ tableMapping.DataSetTable = <TableName>
                initMethod.Statements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(CodeGenHelper.Variable("tableMapping"), "DataSetTable"),
                        CodeGenHelper.Str(designTable.Name)
                    ) 
                );
 
                // If there are any column mappings add them 
                foreach (DataColumnMapping mapping in designTable.Mappings) {
                    //\\ tableMapping.ColumnMappings.Add(<SourceColumnName>, <DataSetColumnName>); 
                    initMethod.Statements.Add(
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property(CodeGenHelper.Variable("tableMapping"), "ColumnMappings"), 
                                "Add",
                                new CodeExpression[] { CodeGenHelper.Str(mapping.SourceColumn), CodeGenHelper.Str(mapping.DataSetColumn) } 
                            ) 
                        )
                    ); 
                }

                //\\ _adapter.TableMappings.Add(tableMapping);
                initMethod.Statements.Add( 
                    CodeGenHelper.Stm(
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Property( 
                                CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName),
                                "TableMappings" 
                            ),
                            "Add",
                            new CodeExpression[] { CodeGenHelper.Variable("tableMapping") }
                        ) 
                    )
                ); 
            } 

            AddInitAdapterCommands(initMethod); 

            dataComponentClass.Members.Add(initMethod);
        }
 
        private void AddCommandCollectionMembers(CodeTypeDeclaration dataComponentClass, bool isFunctionsDataComponent) {
            Type commandType = null; 
            if (isFunctionsDataComponent) { 
                commandType = typeof(IDbCommand);
            } 
            else {
                commandType = this.providerFactory.CreateCommand().GetType();
            }
 
            dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(commandType, 1), DataComponentNameHandler.SelectCmdCollectionVariableName));
 
            CodeMemberProperty cmdCollectionProperty = CodeGenHelper.PropertyDecl( 
                CodeGenHelper.GlobalType(commandType, 1),
                DataComponentNameHandler.SelectCmdCollectionPropertyName, 
                MemberAttributes.Family | MemberAttributes.Final
            );
            cmdCollectionProperty.GetStatements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdEQ(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionVariableName), 
                        CodeGenHelper.Primitive(null) 
                    ),
                    new CodeStatement[] { 
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitCmdCollection, new CodeExpression[] {}))
                    }
                )
            ); 
            cmdCollectionProperty.GetStatements.Add(
                CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionVariableName)) 
            ); 

            dataComponentClass.Members.Add(cmdCollectionProperty); 
        }

        private void AddInitCommandCollection(CodeTypeDeclaration dataComponentClass, bool isFunctionsDataComponent) {
            int arraySize = designTable.Sources.Count; 
            if (!isFunctionsDataComponent) {
                arraySize++; 
            } 

            CodeMemberMethod initMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), DataComponentNameHandler.InitCmdCollection, MemberAttributes.Private | MemberAttributes.Final); 

            Type arrayCmdType = null;
            if (isFunctionsDataComponent) {
                arrayCmdType = typeof(System.Data.IDbCommand); 
            }
            else { 
                arrayCmdType = this.providerFactory.CreateCommand().GetType(); 
            }
            initMethod.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionVariableName),
                    CodeGenHelper.NewArray(CodeGenHelper.GlobalType(arrayCmdType), arraySize)
                ) 
            );
 
            if (!isFunctionsDataComponent && designTable.MainSource != null && designTable.MainSource is DbSource) { 
                DbSource mainDbSource = (DbSource)designTable.MainSource;
                DbSourceCommand mainCommand = mainDbSource.GetActiveCommand(); 

                if (mainCommand != null) {
                    CodeExpression selectCmdExpression = CodeGenHelper.ArrayIndexer(
                                CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionVariableName), 
                                CodeGenHelper.Primitive(0));
 
                    this.AddCommandInitStatements(initMethod.Statements, selectCmdExpression, mainCommand, this.providerFactory, isFunctionsDataComponent); 
                }
            } 

            if (designTable.Sources != null) {
                int i = 0;
                if (isFunctionsDataComponent) { 
                    i--;
                } 
                foreach (Source source in designTable.Sources) { 
                    DbSource dbSource = source as DbSource;
                    i++; 

                    if (dbSource != null) {
                        DbProviderFactory currentFactory = this.providerFactory;
                        if (dbSource.Connection != null) { 
                            currentFactory = ProviderManager.GetFactory(dbSource.Connection.Provider);
                        } 
 
                        if(currentFactory == null) {
                            continue; 
                        }

                        DbSourceCommand command = dbSource.GetActiveCommand();
 
                        // Only Add statement if there's an active command
                        if (command != null) { 
                            CodeExpression selectCmdExpression = CodeGenHelper.ArrayIndexer( 
                                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionVariableName),
                                        CodeGenHelper.Primitive(i)); 

                            this.AddCommandInitStatements(initMethod.Statements, selectCmdExpression, command, currentFactory, isFunctionsDataComponent);
                        }
                    } 
                }
            } 
 
            dataComponentClass.Members.Add(initMethod);
        } 

        private void AddConnectionMembers(CodeTypeDeclaration dataComponentClass) {
            Type connectionType = this.providerFactory.CreateConnection().GetType();
            MemberAttributes connectionModifier = ((DesignConnection)this.designTable.Connection).Modifier; 

            //\\ private System.Data.SqlClient.SqlConnection m_connection; 
            dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(connectionType), DataComponentNameHandler.DefaultConnectionVariableName)); 

            //\\ internal System.Data.SqlClient.SqlConnection Connection { 
            //\\     get {
            //\\         if ((this.m_connection == null)) {
            //\\             this.InitConnection();
            //\\         } 
            //\\         return this.m_connection;
            //\\     } 
            //\\     set { 
            //\\         this.m_connection = value;
            //\\         if ((this.Adapter.InsertCommand != null)) { 
            //\\             this.Adapter.InsertCommand.Connection = value;
            //\\         }
            //\\         if ((this.Adapter.DeleteCommand != null)) {
            //\\             this.Adapter.DeleteCommand.Connection = value; 
            //\\         }
            //\\         if ((this.Adapter.UpdateCommand != null)) { 
            //\\             this.Adapter.UpdateCommand.Connection = value; 
            //\\         }
            //\\         for (int i = 0; (i < this.CommandCollection.Length); i = (i + 1)) { 
            //\\             if ((this.CommandCollection[i] != null)) {
            //\\                 ((SqlCommand)this.CommandCollection[i]).Connection = value;
            //\\             }
            //\\         } 
            //\\     }
            //\\ } 
            CodeMemberProperty connectionProperty = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(connectionType), DataComponentNameHandler.DefaultConnectionPropertyName, connectionModifier | MemberAttributes.Final); 
            connectionProperty.GetStatements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdEQ(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionVariableName),
                        CodeGenHelper.Primitive(null)
                    ), 
                    new CodeStatement[] {
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitConnection, new CodeExpression[] {})) 
                    } 
                )
            ); 
            connectionProperty.GetStatements.Add(
                CodeGenHelper.Return(
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionVariableName)
                ) 
            );
 
            connectionProperty.SetStatements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionVariableName), 
                    CodeGenHelper.Argument("value")
                )
            );
            connectionProperty.SetStatements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ( 
                        CodeGenHelper.Property( 
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                            "InsertCommand" 
                        ),
                        CodeGenHelper.Primitive(null)
                    ),
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Property( 
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                                "InsertCommand"
                            ), 
                            "Connection"
                        ),
                        CodeGenHelper.Argument("value")
                    ) 
                )
            ); 
            connectionProperty.SetStatements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                            "DeleteCommand"
                        ), 
                        CodeGenHelper.Primitive(null)
                    ), 
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Property( 
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                "DeleteCommand"
                            ),
                            "Connection" 
                        ),
                        CodeGenHelper.Argument("value") 
                    ) 
                )
            ); 
            connectionProperty.SetStatements.Add(
                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Property( 
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                            "UpdateCommand" 
                        ), 
                        CodeGenHelper.Primitive(null)
                    ), 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(
                            CodeGenHelper.Property(
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                                "UpdateCommand"
                            ), 
                            "Connection" 
                        ),
                        CodeGenHelper.Argument("value") 
                    )
                )
            );
 
            //if(this.shortDeleteAdded) {
            //    connectionProperty.SetStatements.Add( 
            //        CodeGenHelper.If( 
            //            CodeGenHelper.IdNotEQ(
            //                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortDeleteCmdPropertyName), 
            //                CodeGenHelper.Primitive(null)
            //            ),
            //            CodeGenHelper.Assign(
            //                CodeGenHelper.Property( 
            //                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortDeleteCmdPropertyName),
            //                    "Connection" 
            //                ), 
            //                CodeGenHelper.Argument("value")
            //            ) 
            //        )
            //    );
            //}
            //if (this.shortInsertAdded) { 
            //    connectionProperty.SetStatements.Add(
            //        CodeGenHelper.If( 
            //            CodeGenHelper.IdNotEQ( 
            //                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortInsertCmdPropertyName),
            //                CodeGenHelper.Primitive(null) 
            //            ),
            //            CodeGenHelper.Assign(
            //                CodeGenHelper.Property(
            //                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortInsertCmdPropertyName), 
            //                    "Connection"
            //                ), 
            //                CodeGenHelper.Argument("value") 
            //            )
            //        ) 
            //    );
            //}
            //if (this.shortUpdateAdded) {
            //    connectionProperty.SetStatements.Add( 
            //        CodeGenHelper.If(
            //            CodeGenHelper.IdNotEQ( 
            //                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortUpdateCmdPropertyName), 
            //                CodeGenHelper.Primitive(null)
            //            ), 
            //            CodeGenHelper.Assign(
            //                CodeGenHelper.Property(
            //                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortUpdateCmdPropertyName),
            //                    "Connection" 
            //                ),
            //                CodeGenHelper.Argument("value") 
            //            ) 
            //        )
            //    ); 
            //}

            int cmdCollectionSize = designTable.Sources.Count + 1;
            CodeStatement forInit = CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(int)), "i", CodeGenHelper.Primitive(0)); 
            CodeExpression forTest = CodeGenHelper.Less(
                                        CodeGenHelper.Variable("i"), 
                                        CodeGenHelper.Property( 
                                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                                            "Length" 
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

            CodeExpression connection = CodeGenHelper.Property(
                                            CodeGenHelper.Cast( 
                                                CodeGenHelper.GlobalType(this.providerFactory.CreateCommand().GetType()),
                                                CodeGenHelper.Indexer( 
                                                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName), 
                                                    CodeGenHelper.Variable("i")
                                                ) 
                                            ),
                                            "Connection"
                                         );
            CodeExpression command = CodeGenHelper.Indexer( 
                                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                                        CodeGenHelper.Variable("i") 
                                     ); 

            CodeStatement forStmt = CodeGenHelper.If( 
                CodeGenHelper.IdNotEQ(
                    command,
                    CodeGenHelper.Primitive(null)
                ), 
                CodeGenHelper.Assign(
                    connection, 
                    CodeGenHelper.Argument("value") 
                )
            ); 

            connectionProperty.SetStatements.Add(CodeGenHelper.ForLoop(forInit, forTest, forIncrement, new CodeStatement[] {forStmt}));

            dataComponentClass.Members.Add(connectionProperty); 
        }
 
        private void AddInitConnection(CodeTypeDeclaration dataComponentClass) { 
            IDesignConnection connection = this.designTable.Connection;
            CodeMemberMethod initMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), DataComponentNameHandler.InitConnection, MemberAttributes.Private | MemberAttributes.Final); 

            //\\ this.m_connection = new System.Data.SqlClient.SqlConnection();
            initMethod.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionVariableName),
                    CodeGenHelper.New(CodeGenHelper.GlobalType(this.providerFactory.CreateConnection().GetType()), new CodeExpression[] { }) 
                ) 
            );
 

            CodeExpression connStrProperty = null;
            if (connection.PropertyReference == null) {
                connStrProperty = CodeGenHelper.Str(connection.ConnectionStringObject.ToFullString()); 
            }
            else { 
                connStrProperty = connection.PropertyReference; 
            }
            //\\ this.m_connection.ConnectionString = "..."; 
            initMethod.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionVariableName), 
                        "ConnectionString"
                    ), 
                    connStrProperty 
                )
            ); 

            dataComponentClass.Members.Add(initMethod);
        }
 
        private void AddTransactionMembers(CodeTypeDeclaration dataComponentClass) {
            // 
            Debug.Assert(this.designTable.PropertyCache != null); 
            Type transactionType = this.designTable.PropertyCache.TransactionType;
            if (transactionType == null){ 
                return;
                // Consider: System does not support transaction, spit warning code
            }
            CodeTypeReference transactionTypeRef = CodeGenHelper.GlobalType(transactionType); 
            dataComponentClass.Members.Add(
                CodeGenHelper.FieldDecl( 
                    transactionTypeRef, 
                    DataComponentNameHandler.TransactionVariableName));
 
            CodeMemberProperty transactionProperty = CodeGenHelper.PropertyDecl(transactionTypeRef, DataComponentNameHandler.TransactionPropertyName, MemberAttributes.Assembly | MemberAttributes.Final);

            //\\ return this.m_transaction;
            transactionProperty.GetStatements.Add(CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.TransactionVariableName))); 

            //\\ this.m_transaction = value 
            transactionProperty.SetStatements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.TransactionVariableName), 
                    CodeGenHelper.Argument("value")
                )
            );
 
            //\\ for(int i = 0; i < this.CommandCollection.Length; i++) {
            //\\    if(this.selectCommandCollection[i].Transaction == oldTransaction) { 
            //\\        this.selectCommandCollection[i].Transaction = this.m_transaction; 
            //\\    }
            //\\ } 
            CodeStatement forInit = CodeGenHelper.VariableDecl(CodeGenHelper.Type(typeof(int)), "i", CodeGenHelper.Primitive(0));
            CodeExpression forTest = CodeGenHelper.Less(
                                        CodeGenHelper.Variable("i"),
                                        CodeGenHelper.Property( 
                                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                                            "Length" 
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

            CodeExpression transaction = CodeGenHelper.Property( 
                                            CodeGenHelper.Indexer(
                                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                                                CodeGenHelper.Variable("i")
                                            ), 
                                            "Transaction"
                                         ); 
 
            CodeExpression oldTransaction = CodeGenHelper.Variable("oldTransaction");
            CodeExpression newTransaction = CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.TransactionVariableName); 

            CodeStatement forStmt = this.GenerateSetTransactionStmt(transaction, oldTransaction, newTransaction);

            transactionProperty.SetStatements.Add(CodeGenHelper.ForLoop(forInit, forTest, forIncrement, new CodeStatement[] { forStmt })); 

            //\\ if(this.m_adapter != null && this.m_adapter.DeleteCommand != null) { 
            //\\    if(this.m_adapter.DeleteCommand.Transaction == oldTransaction) { 
            //\\        this.m_adapter.DeleteCommand.Transaction = this.m_transaction;
            //\\    } 
            //\\ }
            CodeExpression adapter = CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName);
            CodeExpression command = CodeGenHelper.Property(adapter, "DeleteCommand");
            transaction = CodeGenHelper.Property(command, "Transaction"); 

            transactionProperty.SetStatements.Add( 
                CodeGenHelper.If( 
                    CodeGenHelper.And(
                        CodeGenHelper.IdNotEQ( 
                            adapter,
                            CodeGenHelper.Primitive(null)
                        ),
                        CodeGenHelper.IdNotEQ( 
                            command,
                            CodeGenHelper.Primitive(null) 
                        ) 
                    ),
                    this.GenerateSetTransactionStmt(transaction, oldTransaction, newTransaction) 
                )
            );

            //\\ if(this.m_adapter != null && this.m_adapter.InsertCommand != null) { 
            //\\    if(this.m_adapter.InsertCommand.Transaction == oldTransaction) {
            //\\        this.m_adapter.InsertCommand.Transaction = this.m_transaction; 
            //\\    } 
            //\\ }
            command = CodeGenHelper.Property(adapter, "InsertCommand"); 
            transaction = CodeGenHelper.Property(command, "Transaction");

            transactionProperty.SetStatements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.And(
                        CodeGenHelper.IdNotEQ( 
                            adapter, 
                            CodeGenHelper.Primitive(null)
                        ), 
                        CodeGenHelper.IdNotEQ(
                            command,
                            CodeGenHelper.Primitive(null)
                        ) 
                    ),
                    this.GenerateSetTransactionStmt(transaction, oldTransaction, newTransaction) 
                ) 
            );
 

            //\\ if(this.m_adapter != null && this.m_adapter.UpdateCommand != null) {
            //\\    if(this.m_adapter.UpdateCommand.Transaction == oldTransaction) {
            //\\        this.m_adapter.UpdateCommand.Transaction = this.m_transaction; 
            //\\    }
            //\\ } 
            command = CodeGenHelper.Property(adapter, "UpdateCommand"); 
            transaction = CodeGenHelper.Property(command, "Transaction");
 
            transactionProperty.SetStatements.Add(
                CodeGenHelper.If(
                    CodeGenHelper.And(
                        CodeGenHelper.IdNotEQ( 
                            adapter,
                            CodeGenHelper.Primitive(null) 
                        ), 
                        CodeGenHelper.IdNotEQ(
                            command, 
                            CodeGenHelper.Primitive(null)
                        )
                    ),
                    this.GenerateSetTransactionStmt(transaction, oldTransaction, newTransaction) 
                )
            ); 
 
            dataComponentClass.Members.Add(transactionProperty);
        } 

        private CodeStatement GenerateSetTransactionStmt(CodeExpression transaction, CodeExpression oldTransaction, CodeExpression newTransaction) {
            return CodeGenHelper.Assign(
                    transaction, 
                    newTransaction
            ); 
        } 

        private void AddClearBeforeFillMembers(CodeTypeDeclaration dataComponentClass) { 
            dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(bool)), DataComponentNameHandler.ClearBeforeFillVariableName));

            CodeMemberProperty clearProperty = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(typeof(bool)), DataComponentNameHandler.ClearBeforeFillPropertyName, MemberAttributes.Public | MemberAttributes.Final);
 
            clearProperty.GetStatements.Add(CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ClearBeforeFillVariableName)));
            clearProperty.SetStatements.Add( 
                CodeGenHelper.Assign( 
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ClearBeforeFillVariableName),
                    CodeGenHelper.Argument("value") 
                )
            );

            dataComponentClass.Members.Add(clearProperty); 
        }
 
 
        private void AddInitAdapterCommands(CodeMemberMethod method) {
            if (designTable.DeleteCommand != null) { 
                CodeExpression deleteCmdExpression = CodeGenHelper.Property(
                               CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName),
                               "DeleteCommand");
 
                this.AddCommandInitStatements(method.Statements, deleteCmdExpression, designTable.DeleteCommand, this.providerFactory, false /*isFunctionsDataComponent*/);
            } 
            if (designTable.InsertCommand != null) { 
                CodeExpression insertCmdExpression = CodeGenHelper.Property(
                               CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName), 
                               "InsertCommand");

                this.AddCommandInitStatements(method.Statements, insertCmdExpression, designTable.InsertCommand, this.providerFactory, false /*isFunctionsDataComponent*/);
            } 
            if (designTable.UpdateCommand != null) {
                CodeExpression updateCmdExpression = CodeGenHelper.Property( 
                               CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName), 
                               "UpdateCommand");
 
                this.AddCommandInitStatements(method.Statements, updateCmdExpression, designTable.UpdateCommand, this.providerFactory, false /*isFunctionsDataComponent*/);
            }
        }
 
        private void AddCommandInitStatements(IList statements, CodeExpression commandExpression, DbSourceCommand command, DbProviderFactory currentFactory, bool isFunctionsDataComponent) {
            if(statements == null || commandExpression == null || command == null) { 
                throw new InternalException("Argument should not be null."); 
            }
 
            Type parameterType = currentFactory.CreateParameter().GetType();
            Type commandType = currentFactory.CreateCommand().GetType();
            CodeExpression parameterVariable = null;
            // Here we add statements to Set the Main Select, Delete, Insert and Update Commands 

            //\\    this._adapter.SelectCommand = new SqlCommand(); 
            //\\    this._adapter.SelectCommand.Connection = <connection>; 
            //\\    this._adapter.SelectCommand.CommandText = <SelectCommandText>;
            //\\    this._adapter.SelectCommand.CommandType = <SelectCommandType>; 
            //\\    this._adapter.SelectCommand.Parameters.Add(new SqlParameter(...));
            //\\    ...

            //\\    this._adapter.SelectCommand = new SqlCommand(); 
            statements.Add(
                CodeGenHelper.Assign( 
                    commandExpression, 
                    CodeGenHelper.New(CodeGenHelper.GlobalType(commandType), new CodeExpression[] { })
                ) 
            );

            if (isFunctionsDataComponent) {
                commandExpression = CodeGenHelper.Cast(CodeGenHelper.GlobalType(commandType), commandExpression); 
            }
 
            //\\    <commandExpression>.Connection = this.DefaultConnection; 
            if (((DbSource)command.Parent).Connection == null || (this.designTable.Connection != null && this.designTable.Connection == ((DbSource)command.Parent).Connection)) {
                statements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(commandExpression, "Connection"),
                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionPropertyName)
                    ) 
                );
            } 
            else { 
                Type connectionType = currentFactory.CreateConnection().GetType();
                IDesignConnection connection = ((DbSource)command.Parent).Connection; 

                CodeExpression connStrProperty = null;
                if (connection.PropertyReference == null) {
                    connStrProperty = CodeGenHelper.Str(connection.ConnectionStringObject.ToFullString()); 
                }
                else { 
                    connStrProperty = connection.PropertyReference; 
                }
 
                //\\ <commandExpression>.Connection = new SqlConnection(<connStrProperty>);
                statements.Add(
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(commandExpression, "Connection"), 
                        CodeGenHelper.New(CodeGenHelper.GlobalType(connectionType), new CodeExpression[] { connStrProperty })
                    ) 
                ); 
            }
 
            //\\    this._adapter.SelectCommand.CommandText = <SelectCommandText>;
            statements.Add(QueryGenerator.SetCommandTextStatement(commandExpression, command.CommandText));
            //\\    this._adapter.SelectCommand.CommandType = <SelectCommandType>;
            statements.Add(QueryGenerator.SetCommandTypeStatement(commandExpression, command.CommandType)); 

            if (command.Parameters != null) { 
                foreach (DesignParameter parameter in command.Parameters) { 
                    //\\ this._adapter.SelectCommand.Parameters.Add(new SqlParameter("<parameterName>", <Type>, <Size>, <Direction>,
                    //\\        <isNullable>, <precision>, <scale>, <sourceColumn>, <DataRowVersion>, <SourceColumnNullMapping>, <Value>, "", "" )); 
                    parameterVariable = QueryGenerator.AddNewParameterStatements(parameter, parameterType, currentFactory, statements, parameterVariable);
                    statements.Add(
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Property(commandExpression, "Parameters"),
                                "Add", 
                                new CodeExpression[] {parameterVariable} 
                            )
                        ) 
                    );
                }
            }
        } 

 
/*        private CodeExpression AddLateBindingCodeForPropertyReference(IDesignConnection connection, IList statements, CodePropertyReferenceExpression propertyReference) { 
            if (StringUtil.EqualValue(connection.AppSettingsObjectName, "Web.config")) {
                return connection.PropertyReference; 
            }

            // Hack: this will be removed once we can directly reference the settings class in the TempPE. At that point, we'll be able to use
            // the PropertyReference directly 
            string propName = propertyReference.PropertyName;
            Debug.Assert(!StringUtil.Empty(propName)); 
 
            CodePropertyReferenceExpression defaultInstanceReference = propertyReference.TargetObject as CodePropertyReferenceExpression;
            Debug.Assert(defaultInstanceReference != null); 

            string defaultInstanceName = defaultInstanceReference.PropertyName;
            Debug.Assert(!StringUtil.Empty(defaultInstanceName));
 
            CodeTypeReferenceExpression targetType = defaultInstanceReference.TargetObject as CodeTypeReferenceExpression;
            Debug.Assert(targetType != null); 
 
            string targetName = targetType.Type.BaseType;
            Debug.Assert(!StringUtil.Empty(targetName)); 

            if (!this.lateBindingVarsDeclared) {
                this.lateBindingVarsDeclared = true;
                //\\ string csValue = null; 
                statements.Add(
                    CodeGenHelper.VariableDecl( 
                        typeof(System.String), 
                        "csValue",
                        CodeGenHelper.Primitive(null) 
                    )
                );
                //\\ System.Type settingsType = null;
                statements.Add( 
                    CodeGenHelper.VariableDecl(
                        typeof(System.Type), 
                        "settingsType", 
                        CodeGenHelper.Primitive(null)
                    ) 
                );
                //\\ System.ComponentModel.Design.ITypeResolutionService trs = null;
                statements.Add(
                    CodeGenHelper.VariableDecl( 
                        typeof(System.ComponentModel.Design.ITypeResolutionService),
                        "trs", 
                        CodeGenHelper.Primitive(null) 
                    )
                ); 
            }

            statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Property(CodeGenHelper.This(), "Site"), 
                        CodeGenHelper.Primitive(null) 
                    ),
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Variable("trs"),
                        CodeGenHelper.Cast(
                            typeof(System.ComponentModel.Design.ITypeResolutionService),
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Property(CodeGenHelper.This(), "Site"),
                                "GetService", 
                                new CodeExpression[] { CodeGenHelper.TypeOf(typeof(System.ComponentModel.Design.ITypeResolutionService)) } 
                            )
                        ) 
                    )
                )
            );
 
            statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ( 
                        CodeGenHelper.Variable("trs"),
                        CodeGenHelper.Primitive(null) 
                    ),
                    CodeGenHelper.Assign(
                        CodeGenHelper.Variable("settingsType"),
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Variable("trs"),
                            "GetType", 
                            new CodeExpression[] { CodeGenHelper.Str(targetName) } 
                        )
                    ), 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Variable("settingsType"),
                        CodeGenHelper.MethodCall(CodeGenHelper.TypeExpr(typeof(Type)), "GetType", new CodeExpression[] { CodeGenHelper.Str(targetName) })
                    ) 
                )
            ); 
 
            CodeStatement[] trueStatements = new CodeStatement[4];
            //\\ PropertyInfo diProperty = settingsType.GetProperty(<defaultInstanceName>); 
            trueStatements[0] = CodeGenHelper.VariableDecl(
                typeof(System.Reflection.PropertyInfo),
                "diProperty",
                CodeGenHelper.MethodCall(CodeGenHelper.Variable("settingsType"), "GetProperty", new CodeExpression[] { CodeGenHelper.Str(defaultInstanceName) }) 
            );
            //\\ PropertyInfo csProperty = settingsType.GetProperty(<propName>); 
            trueStatements[1] = CodeGenHelper.VariableDecl( 
                typeof(System.Reflection.PropertyInfo),
                "csProperty", 
                CodeGenHelper.MethodCall(CodeGenHelper.Variable("settingsType"), "GetProperty", new CodeExpression[] { CodeGenHelper.Str(propName) })
            );
            //\\ object diValue = diProperty.GetValue(null, null);
            trueStatements[2] = CodeGenHelper.VariableDecl( 
                typeof(System.Object),
                "diValue", 
                CodeGenHelper.MethodCall(CodeGenHelper.Variable("diProperty"), "GetValue", new CodeExpression[] { CodeGenHelper.Primitive(null), CodeGenHelper.Primitive(null) }) 
            );
 
            //\\ csValue = csProperty.GetValue(diValue, null).ToString();
            trueStatements[3] = CodeGenHelper.Assign(
                CodeGenHelper.Variable("csValue"),
                CodeGenHelper.MethodCall( 
                    CodeGenHelper.MethodCall(CodeGenHelper.Variable("csProperty"), "GetValue", new CodeExpression[] { CodeGenHelper.Variable("diValue"), CodeGenHelper.Primitive(null) }),
                    "ToString", 
                    new CodeExpression[] {} 
                )
            ); 

            // if(settingsType != null) {....}
            statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Variable("settingsType"), 
                        CodeGenHelper.Primitive(null) 
                    ),
                    trueStatements 
                )
            );

            return CodeGenHelper.Variable("csValue"); 
        }
*/ 
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
    using System.Design; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Reflection;


    internal sealed class DataComponentMethodGenerator { 
        private TypedDataSourceCodeGenerator codeGenerator = null;
        private DesignTable designTable = null; 
        private DbProviderFactory providerFactory = null; 
        // C++ and J# do not want the new feature in Orcas, we will keep them unchanged
        private bool generateHierarchicalUpdate = false; 

        internal DataComponentMethodGenerator(TypedDataSourceCodeGenerator codeGenerator, DesignTable designTable, bool generateHierarchicalUpdate) {
            this.generateHierarchicalUpdate = generateHierarchicalUpdate;
            this.codeGenerator = codeGenerator; 
            this.designTable = designTable;
 
            if (designTable.Connection != null) { 
                this.providerFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
            } 
        }

        internal void AddMethods(CodeTypeDeclaration dataComponentClass, bool isFunctionsDataComponent) {
            if(dataComponentClass == null) { 
                throw new InternalException("dataComponent CodeTypeDeclaration should not be null.");
            } 
 
            if (isFunctionsDataComponent) {
                AddCommandCollectionMembers(dataComponentClass, true /*isFunctionsDataComponent*/); 
                AddInitCommandCollection(dataComponentClass, true /*isFunctionsDataComponent*/);
            }
            else {
                if (this.designTable.Connection == null || this.providerFactory == null) { 
                    return;
                } 
 
                // Add methods to the class
                AddConstructor(dataComponentClass); 

                //if (this.designTable.MainSource != null && ((DbSource)this.designTable.MainSource).GenerateShortCommands) {
                //    AddShortUpdateCommandMembers(dataComponentClass);
                //    AddInitShortUpdateCommands(dataComponentClass); 
                //}
 
                AddAdapterMembers(dataComponentClass); 
                AddInitAdapter(dataComponentClass);
 
                AddConnectionMembers(dataComponentClass);
                AddInitConnection(dataComponentClass);

                if (generateHierarchicalUpdate) { 
                    AddTransactionMembers(dataComponentClass);
                } 
 
                AddCommandCollectionMembers(dataComponentClass, false /*isFunctionsComponent*/);
                AddInitCommandCollection(dataComponentClass, false /*isFunctionsDataComponent*/); 

                AddClearBeforeFillMembers(dataComponentClass);

                //AddArgumentLessConstructor(dataComponentClass, false /*skipMain*/); 
            }
        } 
 
        private void AddConstructor(CodeTypeDeclaration dataComponentClass) {
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Public); 
            constructor.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ClearBeforeFillPropertyName),
                    CodeGenHelper.Primitive(true) 
                )
            ); 
 
            dataComponentClass.Members.Add(constructor);
        } 

/*        private void AddShortUpdateCommandMembers(CodeTypeDeclaration dataComponentClass) {
            Type commandType = this.providerFactory.CreateCommand().GetType();
            CodeMemberProperty commandProperty = null; 

            if (((DbSource)this.designTable.MainSource).DeleteCommand != null) { 
                dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(commandType, DataComponentNameHandler.ShortDeleteCmdVariableName)); 
                // private SqlCommand DeleteCommand {
                //     get { 
                //         if(this.deleteCmd == null) {
                //             this.InitDeleteCmd();
                //         }
                //         return this.deleteCmd; 
                //     }
                // } 
                commandProperty = CodeGenHelper.PropertyDecl(commandType, DataComponentNameHandler.ShortDeleteCmdPropertyName, MemberAttributes.Private | MemberAttributes.Final); 
                commandProperty.GetStatements.Add(
                    CodeGenHelper.If( 
                        CodeGenHelper.IdEQ(
                            CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortDeleteCmdVariableName),
                            CodeGenHelper.Primitive(null)
                        ), 
                        new CodeStatement[] {
                            CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitShortDeleteCmd, new CodeExpression[] {})) 
                        } 
                    )
                ); 
                commandProperty.GetStatements.Add(
                    CodeGenHelper.Return(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortDeleteCmdVariableName)
                    ) 
                );
 
                dataComponentClass.Members.Add(commandProperty); 
                this.shortDeleteAdded = true;
            } 

            if (((DbSource)this.designTable.MainSource).InsertCommand != null) {
                dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(commandType, DataComponentNameHandler.ShortInsertCmdVariableName));
                // private SqlCommand InsertCommand { 
                //     get {
                //         if(this.insertCmd == null) { 
                //             this.InitInsertCmd(); 
                //         }
                //         return this.insertCmd; 
                //     }
                // }
                commandProperty = CodeGenHelper.PropertyDecl(commandType, DataComponentNameHandler.ShortInsertCmdPropertyName, MemberAttributes.Private | MemberAttributes.Final);
                commandProperty.GetStatements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdEQ( 
                            CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortInsertCmdVariableName), 
                            CodeGenHelper.Primitive(null)
                        ), 
                        new CodeStatement[] {
                            CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitShortInsertCmd, new CodeExpression[] {}))
                        }
                    ) 
                );
                commandProperty.GetStatements.Add( 
                    CodeGenHelper.Return( 
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortInsertCmdVariableName)
                    ) 
                );

                dataComponentClass.Members.Add(commandProperty);
                this.shortInsertAdded = true; 
            }
 
            if (((DbSource)this.designTable.MainSource).UpdateCommand != null) { 
                dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(commandType, DataComponentNameHandler.ShortUpdateCmdVariableName));
                // private SqlCommand UpdateCommand { 
                //     get {
                //         if(this.updateCmd == null) {
                //             this.InitUpdateCmd();
                //         } 
                //         return this.updateCmd;
                //     } 
                // } 
                commandProperty = CodeGenHelper.PropertyDecl(commandType, DataComponentNameHandler.ShortUpdateCmdPropertyName, MemberAttributes.Private | MemberAttributes.Final);
                commandProperty.GetStatements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdEQ(
                            CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortUpdateCmdVariableName),
                            CodeGenHelper.Primitive(null) 
                        ),
                        new CodeStatement[] { 
                            CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitShortUpdateCmd, new CodeExpression[] {})) 
                        }
                    ) 
                );
                commandProperty.GetStatements.Add(
                    CodeGenHelper.Return(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ShortUpdateCmdVariableName) 
                    )
                ); 
 
                dataComponentClass.Members.Add(commandProperty);
                this.shortUpdateAdded = true; 
            }
        }

        private void AddInitShortUpdateCommands(CodeTypeDeclaration dataComponentClass) { 
            CodeMemberMethod initMethod = null;
 
            DbSourceCommand shortDeleteCommand = ((DbSource)this.designTable.MainSource).DeleteCommand; 
            if (shortDeleteCommand != null) {
                initMethod = CodeGenHelper.MethodDecl(typeof(void), DataComponentNameHandler.InitShortDeleteCmd, MemberAttributes.Private | MemberAttributes.Final); 
                AddSetCommandStatements(initMethod.Statements, DataComponentNameHandler.ShortDeleteCmdVariableName, shortDeleteCommand);
                dataComponentClass.Members.Add(initMethod);
            }
 
            DbSourceCommand shortInsertCommand = ((DbSource)this.designTable.MainSource).InsertCommand;
            if (shortInsertCommand != null) { 
                initMethod = CodeGenHelper.MethodDecl(typeof(void), DataComponentNameHandler.InitShortInsertCmd, MemberAttributes.Private | MemberAttributes.Final); 
                AddSetCommandStatements(initMethod.Statements, DataComponentNameHandler.ShortInsertCmdVariableName, shortInsertCommand);
                dataComponentClass.Members.Add(initMethod); 
            }

            DbSourceCommand shortUpdateCommand = ((DbSource)this.designTable.MainSource).UpdateCommand;
            if (shortUpdateCommand != null) { 
                initMethod = CodeGenHelper.MethodDecl(typeof(void), DataComponentNameHandler.InitShortUpdateCmd, MemberAttributes.Private | MemberAttributes.Final);
                AddSetCommandStatements(initMethod.Statements, DataComponentNameHandler.ShortUpdateCmdVariableName, shortUpdateCommand); 
                dataComponentClass.Members.Add(initMethod); 
            }
        } 

        private void AddSetCommandStatements(IList statements, string commandVariableName, DbSourceCommand activeCommand) {
            Type commandType = this.providerFactory.CreateCommand().GetType();
            Type parameterType = this.providerFactory.CreateParameter().GetType(); 
            CodeExpression parameterVariable = null;
            CodeExpression commandExpression = CodeGenHelper.Field(CodeGenHelper.This(), commandVariableName); 
 
            //\\ this.deleteCommand = new SqlCommand();
            statements.Add( 
                CodeGenHelper.Assign(
                    commandExpression,
                    CodeGenHelper.New(commandType, new CodeExpression[] {})
                ) 
            );
 
            //\\ this.deleteCommand.Connection = this.Connection; 
            statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(commandExpression, "Connection"),
                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionPropertyName)
                )
            ); 

            //\\ this.deleteCommand.CommandText = <CommandText>; 
            statements.Add(QueryGenerator.SetCommandTextStatement(commandExpression, activeCommand.CommandText)); 
            //\\ this.deleteCommand.CommandType = <CommandType>;
            statements.Add(QueryGenerator.SetCommandTypeStatement(commandExpression, activeCommand.CommandType)); 

            if (activeCommand.Parameters != null) {
                foreach (DesignParameter parameter in activeCommand.Parameters) {
                    //\\ command.Parameters.Add(new SqlParameter("<parameterName>", <Type>, <Size>, <Direction>, 
                    //\\        <isNullable>, <precision>, <scale>, <sourceColumn>, <DataRowVersion>, <Value> ));
                    parameterVariable = QueryGenerator.AddNewParameterStatements(parameter, parameterType, this.providerFactory, statements, parameterVariable); 
                    statements.Add( 
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Property(commandExpression, "Parameters"),
                                "Add",
                                new CodeExpression[] { parameterVariable }
                            ) 
                        )
                    ); 
                } 
            }
        } 
*/
        private void AddAdapterMembers(CodeTypeDeclaration dataComponentClass) {
            Type adapterType = this.providerFactory.CreateDataAdapter().GetType();
            CodeMemberField adapterVariable = CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(adapterType), DataComponentNameHandler.AdapterVariableName); 
            adapterVariable.UserData.Add("WithEvents", true);
 
            dataComponentClass.Members.Add(adapterVariable); 
            CodeMemberProperty adapterProperty = null;
            if (generateHierarchicalUpdate){ 
                adapterProperty = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(adapterType), DataComponentNameHandler.AdapterPropertyName, MemberAttributes.FamilyOrAssembly | MemberAttributes.Final);
            }
            else {
                adapterProperty = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(adapterType), DataComponentNameHandler.AdapterPropertyName, MemberAttributes.Private | MemberAttributes.Final); 
            }
            adapterProperty.GetStatements.Add( 
                CodeGenHelper.If( 
                    CodeGenHelper.IdEQ(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName), 
                        CodeGenHelper.Primitive(null)
                    ),
                    new CodeStatement[] {
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitAdapter, new CodeExpression[] {})) 
                    }
                ) 
            ); 
            adapterProperty.GetStatements.Add(
                CodeGenHelper.Return( 
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName)
                )
            );
 
            dataComponentClass.Members.Add(adapterProperty);
        } 
 
        private void AddInitAdapter(CodeTypeDeclaration dataComponentClass) {
            CodeMemberMethod initMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), DataComponentNameHandler.InitAdapter, MemberAttributes.Private | MemberAttributes.Final); 

            //\\ this.m_adapter = new System.Data.SqlClient.SqlDataAdapter();
            initMethod.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName),
                    CodeGenHelper.New(CodeGenHelper.GlobalType(this.providerFactory.CreateDataAdapter().GetType()), new CodeExpression[] {}) 
                ) 
            );
            if (designTable.Mappings != null && designTable.Mappings.Count > 0) { 
                //\\ DataTableMapping tableMapping = new DataTableMapping();
                initMethod.Statements.Add(
                    CodeGenHelper.VariableDecl(
                        CodeGenHelper.GlobalType(typeof(System.Data.Common.DataTableMapping)), 
                        "tableMapping",
                        CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Data.Common.DataTableMapping)), new CodeExpression[] {}) 
                    ) 
                );
 
                //\\ tableMapping.SourceTable = "Table"
                initMethod.Statements.Add(
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(CodeGenHelper.Variable("tableMapping"), "SourceTable"), 
                        CodeGenHelper.Str("Table")
                    ) 
                ); 
                //\\ tableMapping.DataSetTable = <TableName>
                initMethod.Statements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(CodeGenHelper.Variable("tableMapping"), "DataSetTable"),
                        CodeGenHelper.Str(designTable.Name)
                    ) 
                );
 
                // If there are any column mappings add them 
                foreach (DataColumnMapping mapping in designTable.Mappings) {
                    //\\ tableMapping.ColumnMappings.Add(<SourceColumnName>, <DataSetColumnName>); 
                    initMethod.Statements.Add(
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property(CodeGenHelper.Variable("tableMapping"), "ColumnMappings"), 
                                "Add",
                                new CodeExpression[] { CodeGenHelper.Str(mapping.SourceColumn), CodeGenHelper.Str(mapping.DataSetColumn) } 
                            ) 
                        )
                    ); 
                }

                //\\ _adapter.TableMappings.Add(tableMapping);
                initMethod.Statements.Add( 
                    CodeGenHelper.Stm(
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Property( 
                                CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName),
                                "TableMappings" 
                            ),
                            "Add",
                            new CodeExpression[] { CodeGenHelper.Variable("tableMapping") }
                        ) 
                    )
                ); 
            } 

            AddInitAdapterCommands(initMethod); 

            dataComponentClass.Members.Add(initMethod);
        }
 
        private void AddCommandCollectionMembers(CodeTypeDeclaration dataComponentClass, bool isFunctionsDataComponent) {
            Type commandType = null; 
            if (isFunctionsDataComponent) { 
                commandType = typeof(IDbCommand);
            } 
            else {
                commandType = this.providerFactory.CreateCommand().GetType();
            }
 
            dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(commandType, 1), DataComponentNameHandler.SelectCmdCollectionVariableName));
 
            CodeMemberProperty cmdCollectionProperty = CodeGenHelper.PropertyDecl( 
                CodeGenHelper.GlobalType(commandType, 1),
                DataComponentNameHandler.SelectCmdCollectionPropertyName, 
                MemberAttributes.Family | MemberAttributes.Final
            );
            cmdCollectionProperty.GetStatements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdEQ(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionVariableName), 
                        CodeGenHelper.Primitive(null) 
                    ),
                    new CodeStatement[] { 
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitCmdCollection, new CodeExpression[] {}))
                    }
                )
            ); 
            cmdCollectionProperty.GetStatements.Add(
                CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionVariableName)) 
            ); 

            dataComponentClass.Members.Add(cmdCollectionProperty); 
        }

        private void AddInitCommandCollection(CodeTypeDeclaration dataComponentClass, bool isFunctionsDataComponent) {
            int arraySize = designTable.Sources.Count; 
            if (!isFunctionsDataComponent) {
                arraySize++; 
            } 

            CodeMemberMethod initMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), DataComponentNameHandler.InitCmdCollection, MemberAttributes.Private | MemberAttributes.Final); 

            Type arrayCmdType = null;
            if (isFunctionsDataComponent) {
                arrayCmdType = typeof(System.Data.IDbCommand); 
            }
            else { 
                arrayCmdType = this.providerFactory.CreateCommand().GetType(); 
            }
            initMethod.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionVariableName),
                    CodeGenHelper.NewArray(CodeGenHelper.GlobalType(arrayCmdType), arraySize)
                ) 
            );
 
            if (!isFunctionsDataComponent && designTable.MainSource != null && designTable.MainSource is DbSource) { 
                DbSource mainDbSource = (DbSource)designTable.MainSource;
                DbSourceCommand mainCommand = mainDbSource.GetActiveCommand(); 

                if (mainCommand != null) {
                    CodeExpression selectCmdExpression = CodeGenHelper.ArrayIndexer(
                                CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionVariableName), 
                                CodeGenHelper.Primitive(0));
 
                    this.AddCommandInitStatements(initMethod.Statements, selectCmdExpression, mainCommand, this.providerFactory, isFunctionsDataComponent); 
                }
            } 

            if (designTable.Sources != null) {
                int i = 0;
                if (isFunctionsDataComponent) { 
                    i--;
                } 
                foreach (Source source in designTable.Sources) { 
                    DbSource dbSource = source as DbSource;
                    i++; 

                    if (dbSource != null) {
                        DbProviderFactory currentFactory = this.providerFactory;
                        if (dbSource.Connection != null) { 
                            currentFactory = ProviderManager.GetFactory(dbSource.Connection.Provider);
                        } 
 
                        if(currentFactory == null) {
                            continue; 
                        }

                        DbSourceCommand command = dbSource.GetActiveCommand();
 
                        // Only Add statement if there's an active command
                        if (command != null) { 
                            CodeExpression selectCmdExpression = CodeGenHelper.ArrayIndexer( 
                                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionVariableName),
                                        CodeGenHelper.Primitive(i)); 

                            this.AddCommandInitStatements(initMethod.Statements, selectCmdExpression, command, currentFactory, isFunctionsDataComponent);
                        }
                    } 
                }
            } 
 
            dataComponentClass.Members.Add(initMethod);
        } 

        private void AddConnectionMembers(CodeTypeDeclaration dataComponentClass) {
            Type connectionType = this.providerFactory.CreateConnection().GetType();
            MemberAttributes connectionModifier = ((DesignConnection)this.designTable.Connection).Modifier; 

            //\\ private System.Data.SqlClient.SqlConnection m_connection; 
            dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(connectionType), DataComponentNameHandler.DefaultConnectionVariableName)); 

            //\\ internal System.Data.SqlClient.SqlConnection Connection { 
            //\\     get {
            //\\         if ((this.m_connection == null)) {
            //\\             this.InitConnection();
            //\\         } 
            //\\         return this.m_connection;
            //\\     } 
            //\\     set { 
            //\\         this.m_connection = value;
            //\\         if ((this.Adapter.InsertCommand != null)) { 
            //\\             this.Adapter.InsertCommand.Connection = value;
            //\\         }
            //\\         if ((this.Adapter.DeleteCommand != null)) {
            //\\             this.Adapter.DeleteCommand.Connection = value; 
            //\\         }
            //\\         if ((this.Adapter.UpdateCommand != null)) { 
            //\\             this.Adapter.UpdateCommand.Connection = value; 
            //\\         }
            //\\         for (int i = 0; (i < this.CommandCollection.Length); i = (i + 1)) { 
            //\\             if ((this.CommandCollection[i] != null)) {
            //\\                 ((SqlCommand)this.CommandCollection[i]).Connection = value;
            //\\             }
            //\\         } 
            //\\     }
            //\\ } 
            CodeMemberProperty connectionProperty = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(connectionType), DataComponentNameHandler.DefaultConnectionPropertyName, connectionModifier | MemberAttributes.Final); 
            connectionProperty.GetStatements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdEQ(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionVariableName),
                        CodeGenHelper.Primitive(null)
                    ), 
                    new CodeStatement[] {
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), DataComponentNameHandler.InitConnection, new CodeExpression[] {})) 
                    } 
                )
            ); 
            connectionProperty.GetStatements.Add(
                CodeGenHelper.Return(
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionVariableName)
                ) 
            );
 
            connectionProperty.SetStatements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionVariableName), 
                    CodeGenHelper.Argument("value")
                )
            );
            connectionProperty.SetStatements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ( 
                        CodeGenHelper.Property( 
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                            "InsertCommand" 
                        ),
                        CodeGenHelper.Primitive(null)
                    ),
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Property( 
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                                "InsertCommand"
                            ), 
                            "Connection"
                        ),
                        CodeGenHelper.Argument("value")
                    ) 
                )
            ); 
            connectionProperty.SetStatements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                            "DeleteCommand"
                        ), 
                        CodeGenHelper.Primitive(null)
                    ), 
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Property( 
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                                "DeleteCommand"
                            ),
                            "Connection" 
                        ),
                        CodeGenHelper.Argument("value") 
                    ) 
                )
            ); 
            connectionProperty.SetStatements.Add(
                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Property( 
                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName),
                            "UpdateCommand" 
                        ), 
                        CodeGenHelper.Primitive(null)
                    ), 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(
                            CodeGenHelper.Property(
                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName), 
                                "UpdateCommand"
                            ), 
                            "Connection" 
                        ),
                        CodeGenHelper.Argument("value") 
                    )
                )
            );
 
            //if(this.shortDeleteAdded) {
            //    connectionProperty.SetStatements.Add( 
            //        CodeGenHelper.If( 
            //            CodeGenHelper.IdNotEQ(
            //                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortDeleteCmdPropertyName), 
            //                CodeGenHelper.Primitive(null)
            //            ),
            //            CodeGenHelper.Assign(
            //                CodeGenHelper.Property( 
            //                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortDeleteCmdPropertyName),
            //                    "Connection" 
            //                ), 
            //                CodeGenHelper.Argument("value")
            //            ) 
            //        )
            //    );
            //}
            //if (this.shortInsertAdded) { 
            //    connectionProperty.SetStatements.Add(
            //        CodeGenHelper.If( 
            //            CodeGenHelper.IdNotEQ( 
            //                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortInsertCmdPropertyName),
            //                CodeGenHelper.Primitive(null) 
            //            ),
            //            CodeGenHelper.Assign(
            //                CodeGenHelper.Property(
            //                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortInsertCmdPropertyName), 
            //                    "Connection"
            //                ), 
            //                CodeGenHelper.Argument("value") 
            //            )
            //        ) 
            //    );
            //}
            //if (this.shortUpdateAdded) {
            //    connectionProperty.SetStatements.Add( 
            //        CodeGenHelper.If(
            //            CodeGenHelper.IdNotEQ( 
            //                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortUpdateCmdPropertyName), 
            //                CodeGenHelper.Primitive(null)
            //            ), 
            //            CodeGenHelper.Assign(
            //                CodeGenHelper.Property(
            //                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.ShortUpdateCmdPropertyName),
            //                    "Connection" 
            //                ),
            //                CodeGenHelper.Argument("value") 
            //            ) 
            //        )
            //    ); 
            //}

            int cmdCollectionSize = designTable.Sources.Count + 1;
            CodeStatement forInit = CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(int)), "i", CodeGenHelper.Primitive(0)); 
            CodeExpression forTest = CodeGenHelper.Less(
                                        CodeGenHelper.Variable("i"), 
                                        CodeGenHelper.Property( 
                                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                                            "Length" 
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

            CodeExpression connection = CodeGenHelper.Property(
                                            CodeGenHelper.Cast( 
                                                CodeGenHelper.GlobalType(this.providerFactory.CreateCommand().GetType()),
                                                CodeGenHelper.Indexer( 
                                                    CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName), 
                                                    CodeGenHelper.Variable("i")
                                                ) 
                                            ),
                                            "Connection"
                                         );
            CodeExpression command = CodeGenHelper.Indexer( 
                                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                                        CodeGenHelper.Variable("i") 
                                     ); 

            CodeStatement forStmt = CodeGenHelper.If( 
                CodeGenHelper.IdNotEQ(
                    command,
                    CodeGenHelper.Primitive(null)
                ), 
                CodeGenHelper.Assign(
                    connection, 
                    CodeGenHelper.Argument("value") 
                )
            ); 

            connectionProperty.SetStatements.Add(CodeGenHelper.ForLoop(forInit, forTest, forIncrement, new CodeStatement[] {forStmt}));

            dataComponentClass.Members.Add(connectionProperty); 
        }
 
        private void AddInitConnection(CodeTypeDeclaration dataComponentClass) { 
            IDesignConnection connection = this.designTable.Connection;
            CodeMemberMethod initMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), DataComponentNameHandler.InitConnection, MemberAttributes.Private | MemberAttributes.Final); 

            //\\ this.m_connection = new System.Data.SqlClient.SqlConnection();
            initMethod.Statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionVariableName),
                    CodeGenHelper.New(CodeGenHelper.GlobalType(this.providerFactory.CreateConnection().GetType()), new CodeExpression[] { }) 
                ) 
            );
 

            CodeExpression connStrProperty = null;
            if (connection.PropertyReference == null) {
                connStrProperty = CodeGenHelper.Str(connection.ConnectionStringObject.ToFullString()); 
            }
            else { 
                connStrProperty = connection.PropertyReference; 
            }
            //\\ this.m_connection.ConnectionString = "..."; 
            initMethod.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(
                        CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionVariableName), 
                        "ConnectionString"
                    ), 
                    connStrProperty 
                )
            ); 

            dataComponentClass.Members.Add(initMethod);
        }
 
        private void AddTransactionMembers(CodeTypeDeclaration dataComponentClass) {
            // 
            Debug.Assert(this.designTable.PropertyCache != null); 
            Type transactionType = this.designTable.PropertyCache.TransactionType;
            if (transactionType == null){ 
                return;
                // Consider: System does not support transaction, spit warning code
            }
            CodeTypeReference transactionTypeRef = CodeGenHelper.GlobalType(transactionType); 
            dataComponentClass.Members.Add(
                CodeGenHelper.FieldDecl( 
                    transactionTypeRef, 
                    DataComponentNameHandler.TransactionVariableName));
 
            CodeMemberProperty transactionProperty = CodeGenHelper.PropertyDecl(transactionTypeRef, DataComponentNameHandler.TransactionPropertyName, MemberAttributes.Assembly | MemberAttributes.Final);

            //\\ return this.m_transaction;
            transactionProperty.GetStatements.Add(CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.TransactionVariableName))); 

            //\\ this.m_transaction = value 
            transactionProperty.SetStatements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.TransactionVariableName), 
                    CodeGenHelper.Argument("value")
                )
            );
 
            //\\ for(int i = 0; i < this.CommandCollection.Length; i++) {
            //\\    if(this.selectCommandCollection[i].Transaction == oldTransaction) { 
            //\\        this.selectCommandCollection[i].Transaction = this.m_transaction; 
            //\\    }
            //\\ } 
            CodeStatement forInit = CodeGenHelper.VariableDecl(CodeGenHelper.Type(typeof(int)), "i", CodeGenHelper.Primitive(0));
            CodeExpression forTest = CodeGenHelper.Less(
                                        CodeGenHelper.Variable("i"),
                                        CodeGenHelper.Property( 
                                            CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                                            "Length" 
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

            CodeExpression transaction = CodeGenHelper.Property( 
                                            CodeGenHelper.Indexer(
                                                CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.SelectCmdCollectionPropertyName),
                                                CodeGenHelper.Variable("i")
                                            ), 
                                            "Transaction"
                                         ); 
 
            CodeExpression oldTransaction = CodeGenHelper.Variable("oldTransaction");
            CodeExpression newTransaction = CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.TransactionVariableName); 

            CodeStatement forStmt = this.GenerateSetTransactionStmt(transaction, oldTransaction, newTransaction);

            transactionProperty.SetStatements.Add(CodeGenHelper.ForLoop(forInit, forTest, forIncrement, new CodeStatement[] { forStmt })); 

            //\\ if(this.m_adapter != null && this.m_adapter.DeleteCommand != null) { 
            //\\    if(this.m_adapter.DeleteCommand.Transaction == oldTransaction) { 
            //\\        this.m_adapter.DeleteCommand.Transaction = this.m_transaction;
            //\\    } 
            //\\ }
            CodeExpression adapter = CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.AdapterPropertyName);
            CodeExpression command = CodeGenHelper.Property(adapter, "DeleteCommand");
            transaction = CodeGenHelper.Property(command, "Transaction"); 

            transactionProperty.SetStatements.Add( 
                CodeGenHelper.If( 
                    CodeGenHelper.And(
                        CodeGenHelper.IdNotEQ( 
                            adapter,
                            CodeGenHelper.Primitive(null)
                        ),
                        CodeGenHelper.IdNotEQ( 
                            command,
                            CodeGenHelper.Primitive(null) 
                        ) 
                    ),
                    this.GenerateSetTransactionStmt(transaction, oldTransaction, newTransaction) 
                )
            );

            //\\ if(this.m_adapter != null && this.m_adapter.InsertCommand != null) { 
            //\\    if(this.m_adapter.InsertCommand.Transaction == oldTransaction) {
            //\\        this.m_adapter.InsertCommand.Transaction = this.m_transaction; 
            //\\    } 
            //\\ }
            command = CodeGenHelper.Property(adapter, "InsertCommand"); 
            transaction = CodeGenHelper.Property(command, "Transaction");

            transactionProperty.SetStatements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.And(
                        CodeGenHelper.IdNotEQ( 
                            adapter, 
                            CodeGenHelper.Primitive(null)
                        ), 
                        CodeGenHelper.IdNotEQ(
                            command,
                            CodeGenHelper.Primitive(null)
                        ) 
                    ),
                    this.GenerateSetTransactionStmt(transaction, oldTransaction, newTransaction) 
                ) 
            );
 

            //\\ if(this.m_adapter != null && this.m_adapter.UpdateCommand != null) {
            //\\    if(this.m_adapter.UpdateCommand.Transaction == oldTransaction) {
            //\\        this.m_adapter.UpdateCommand.Transaction = this.m_transaction; 
            //\\    }
            //\\ } 
            command = CodeGenHelper.Property(adapter, "UpdateCommand"); 
            transaction = CodeGenHelper.Property(command, "Transaction");
 
            transactionProperty.SetStatements.Add(
                CodeGenHelper.If(
                    CodeGenHelper.And(
                        CodeGenHelper.IdNotEQ( 
                            adapter,
                            CodeGenHelper.Primitive(null) 
                        ), 
                        CodeGenHelper.IdNotEQ(
                            command, 
                            CodeGenHelper.Primitive(null)
                        )
                    ),
                    this.GenerateSetTransactionStmt(transaction, oldTransaction, newTransaction) 
                )
            ); 
 
            dataComponentClass.Members.Add(transactionProperty);
        } 

        private CodeStatement GenerateSetTransactionStmt(CodeExpression transaction, CodeExpression oldTransaction, CodeExpression newTransaction) {
            return CodeGenHelper.Assign(
                    transaction, 
                    newTransaction
            ); 
        } 

        private void AddClearBeforeFillMembers(CodeTypeDeclaration dataComponentClass) { 
            dataComponentClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(bool)), DataComponentNameHandler.ClearBeforeFillVariableName));

            CodeMemberProperty clearProperty = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(typeof(bool)), DataComponentNameHandler.ClearBeforeFillPropertyName, MemberAttributes.Public | MemberAttributes.Final);
 
            clearProperty.GetStatements.Add(CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ClearBeforeFillVariableName)));
            clearProperty.SetStatements.Add( 
                CodeGenHelper.Assign( 
                    CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.ClearBeforeFillVariableName),
                    CodeGenHelper.Argument("value") 
                )
            );

            dataComponentClass.Members.Add(clearProperty); 
        }
 
 
        private void AddInitAdapterCommands(CodeMemberMethod method) {
            if (designTable.DeleteCommand != null) { 
                CodeExpression deleteCmdExpression = CodeGenHelper.Property(
                               CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName),
                               "DeleteCommand");
 
                this.AddCommandInitStatements(method.Statements, deleteCmdExpression, designTable.DeleteCommand, this.providerFactory, false /*isFunctionsDataComponent*/);
            } 
            if (designTable.InsertCommand != null) { 
                CodeExpression insertCmdExpression = CodeGenHelper.Property(
                               CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName), 
                               "InsertCommand");

                this.AddCommandInitStatements(method.Statements, insertCmdExpression, designTable.InsertCommand, this.providerFactory, false /*isFunctionsDataComponent*/);
            } 
            if (designTable.UpdateCommand != null) {
                CodeExpression updateCmdExpression = CodeGenHelper.Property( 
                               CodeGenHelper.Field(CodeGenHelper.This(), DataComponentNameHandler.AdapterVariableName), 
                               "UpdateCommand");
 
                this.AddCommandInitStatements(method.Statements, updateCmdExpression, designTable.UpdateCommand, this.providerFactory, false /*isFunctionsDataComponent*/);
            }
        }
 
        private void AddCommandInitStatements(IList statements, CodeExpression commandExpression, DbSourceCommand command, DbProviderFactory currentFactory, bool isFunctionsDataComponent) {
            if(statements == null || commandExpression == null || command == null) { 
                throw new InternalException("Argument should not be null."); 
            }
 
            Type parameterType = currentFactory.CreateParameter().GetType();
            Type commandType = currentFactory.CreateCommand().GetType();
            CodeExpression parameterVariable = null;
            // Here we add statements to Set the Main Select, Delete, Insert and Update Commands 

            //\\    this._adapter.SelectCommand = new SqlCommand(); 
            //\\    this._adapter.SelectCommand.Connection = <connection>; 
            //\\    this._adapter.SelectCommand.CommandText = <SelectCommandText>;
            //\\    this._adapter.SelectCommand.CommandType = <SelectCommandType>; 
            //\\    this._adapter.SelectCommand.Parameters.Add(new SqlParameter(...));
            //\\    ...

            //\\    this._adapter.SelectCommand = new SqlCommand(); 
            statements.Add(
                CodeGenHelper.Assign( 
                    commandExpression, 
                    CodeGenHelper.New(CodeGenHelper.GlobalType(commandType), new CodeExpression[] { })
                ) 
            );

            if (isFunctionsDataComponent) {
                commandExpression = CodeGenHelper.Cast(CodeGenHelper.GlobalType(commandType), commandExpression); 
            }
 
            //\\    <commandExpression>.Connection = this.DefaultConnection; 
            if (((DbSource)command.Parent).Connection == null || (this.designTable.Connection != null && this.designTable.Connection == ((DbSource)command.Parent).Connection)) {
                statements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(commandExpression, "Connection"),
                        CodeGenHelper.Property(CodeGenHelper.This(), DataComponentNameHandler.DefaultConnectionPropertyName)
                    ) 
                );
            } 
            else { 
                Type connectionType = currentFactory.CreateConnection().GetType();
                IDesignConnection connection = ((DbSource)command.Parent).Connection; 

                CodeExpression connStrProperty = null;
                if (connection.PropertyReference == null) {
                    connStrProperty = CodeGenHelper.Str(connection.ConnectionStringObject.ToFullString()); 
                }
                else { 
                    connStrProperty = connection.PropertyReference; 
                }
 
                //\\ <commandExpression>.Connection = new SqlConnection(<connStrProperty>);
                statements.Add(
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(commandExpression, "Connection"), 
                        CodeGenHelper.New(CodeGenHelper.GlobalType(connectionType), new CodeExpression[] { connStrProperty })
                    ) 
                ); 
            }
 
            //\\    this._adapter.SelectCommand.CommandText = <SelectCommandText>;
            statements.Add(QueryGenerator.SetCommandTextStatement(commandExpression, command.CommandText));
            //\\    this._adapter.SelectCommand.CommandType = <SelectCommandType>;
            statements.Add(QueryGenerator.SetCommandTypeStatement(commandExpression, command.CommandType)); 

            if (command.Parameters != null) { 
                foreach (DesignParameter parameter in command.Parameters) { 
                    //\\ this._adapter.SelectCommand.Parameters.Add(new SqlParameter("<parameterName>", <Type>, <Size>, <Direction>,
                    //\\        <isNullable>, <precision>, <scale>, <sourceColumn>, <DataRowVersion>, <SourceColumnNullMapping>, <Value>, "", "" )); 
                    parameterVariable = QueryGenerator.AddNewParameterStatements(parameter, parameterType, currentFactory, statements, parameterVariable);
                    statements.Add(
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Property(commandExpression, "Parameters"),
                                "Add", 
                                new CodeExpression[] {parameterVariable} 
                            )
                        ) 
                    );
                }
            }
        } 

 
/*        private CodeExpression AddLateBindingCodeForPropertyReference(IDesignConnection connection, IList statements, CodePropertyReferenceExpression propertyReference) { 
            if (StringUtil.EqualValue(connection.AppSettingsObjectName, "Web.config")) {
                return connection.PropertyReference; 
            }

            // Hack: this will be removed once we can directly reference the settings class in the TempPE. At that point, we'll be able to use
            // the PropertyReference directly 
            string propName = propertyReference.PropertyName;
            Debug.Assert(!StringUtil.Empty(propName)); 
 
            CodePropertyReferenceExpression defaultInstanceReference = propertyReference.TargetObject as CodePropertyReferenceExpression;
            Debug.Assert(defaultInstanceReference != null); 

            string defaultInstanceName = defaultInstanceReference.PropertyName;
            Debug.Assert(!StringUtil.Empty(defaultInstanceName));
 
            CodeTypeReferenceExpression targetType = defaultInstanceReference.TargetObject as CodeTypeReferenceExpression;
            Debug.Assert(targetType != null); 
 
            string targetName = targetType.Type.BaseType;
            Debug.Assert(!StringUtil.Empty(targetName)); 

            if (!this.lateBindingVarsDeclared) {
                this.lateBindingVarsDeclared = true;
                //\\ string csValue = null; 
                statements.Add(
                    CodeGenHelper.VariableDecl( 
                        typeof(System.String), 
                        "csValue",
                        CodeGenHelper.Primitive(null) 
                    )
                );
                //\\ System.Type settingsType = null;
                statements.Add( 
                    CodeGenHelper.VariableDecl(
                        typeof(System.Type), 
                        "settingsType", 
                        CodeGenHelper.Primitive(null)
                    ) 
                );
                //\\ System.ComponentModel.Design.ITypeResolutionService trs = null;
                statements.Add(
                    CodeGenHelper.VariableDecl( 
                        typeof(System.ComponentModel.Design.ITypeResolutionService),
                        "trs", 
                        CodeGenHelper.Primitive(null) 
                    )
                ); 
            }

            statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Property(CodeGenHelper.This(), "Site"), 
                        CodeGenHelper.Primitive(null) 
                    ),
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Variable("trs"),
                        CodeGenHelper.Cast(
                            typeof(System.ComponentModel.Design.ITypeResolutionService),
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Property(CodeGenHelper.This(), "Site"),
                                "GetService", 
                                new CodeExpression[] { CodeGenHelper.TypeOf(typeof(System.ComponentModel.Design.ITypeResolutionService)) } 
                            )
                        ) 
                    )
                )
            );
 
            statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ( 
                        CodeGenHelper.Variable("trs"),
                        CodeGenHelper.Primitive(null) 
                    ),
                    CodeGenHelper.Assign(
                        CodeGenHelper.Variable("settingsType"),
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.Variable("trs"),
                            "GetType", 
                            new CodeExpression[] { CodeGenHelper.Str(targetName) } 
                        )
                    ), 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Variable("settingsType"),
                        CodeGenHelper.MethodCall(CodeGenHelper.TypeExpr(typeof(Type)), "GetType", new CodeExpression[] { CodeGenHelper.Str(targetName) })
                    ) 
                )
            ); 
 
            CodeStatement[] trueStatements = new CodeStatement[4];
            //\\ PropertyInfo diProperty = settingsType.GetProperty(<defaultInstanceName>); 
            trueStatements[0] = CodeGenHelper.VariableDecl(
                typeof(System.Reflection.PropertyInfo),
                "diProperty",
                CodeGenHelper.MethodCall(CodeGenHelper.Variable("settingsType"), "GetProperty", new CodeExpression[] { CodeGenHelper.Str(defaultInstanceName) }) 
            );
            //\\ PropertyInfo csProperty = settingsType.GetProperty(<propName>); 
            trueStatements[1] = CodeGenHelper.VariableDecl( 
                typeof(System.Reflection.PropertyInfo),
                "csProperty", 
                CodeGenHelper.MethodCall(CodeGenHelper.Variable("settingsType"), "GetProperty", new CodeExpression[] { CodeGenHelper.Str(propName) })
            );
            //\\ object diValue = diProperty.GetValue(null, null);
            trueStatements[2] = CodeGenHelper.VariableDecl( 
                typeof(System.Object),
                "diValue", 
                CodeGenHelper.MethodCall(CodeGenHelper.Variable("diProperty"), "GetValue", new CodeExpression[] { CodeGenHelper.Primitive(null), CodeGenHelper.Primitive(null) }) 
            );
 
            //\\ csValue = csProperty.GetValue(diValue, null).ToString();
            trueStatements[3] = CodeGenHelper.Assign(
                CodeGenHelper.Variable("csValue"),
                CodeGenHelper.MethodCall( 
                    CodeGenHelper.MethodCall(CodeGenHelper.Variable("csProperty"), "GetValue", new CodeExpression[] { CodeGenHelper.Variable("diValue"), CodeGenHelper.Primitive(null) }),
                    "ToString", 
                    new CodeExpression[] {} 
                )
            ); 

            // if(settingsType != null) {....}
            statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Variable("settingsType"), 
                        CodeGenHelper.Primitive(null) 
                    ),
                    trueStatements 
                )
            );

            return CodeGenHelper.Variable("csValue"); 
        }
*/ 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
