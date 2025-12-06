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
    using System.ComponentModel.Design;
    using System.Collections; 
    using System.ComponentModel;
    using System.Data; 
    using System.Data.SqlClient; 
    using System.Data.SqlTypes;
    using System.Diagnostics; 

    internal class QueryHandler {
        internal    const string tableParameterName = "dataTable";
        internal    const string dataSetParameterName = "dataSet"; 

        private     TypedDataSourceCodeGenerator codeGenerator = null; 
        private     DesignTable designTable = null; 
        private     bool declarationsOnly = false;
        private     bool languageSupportsNullables = false; 

        internal bool DeclarationsOnly {
            get {
                return this.declarationsOnly; 
            }
            set { 
                this.declarationsOnly = value; 
            }
        } 

        internal QueryHandler(TypedDataSourceCodeGenerator codeGenerator, DesignTable designTable) {
            this.codeGenerator = codeGenerator;
            this.designTable = designTable; 

            this.languageSupportsNullables = this.codeGenerator.CodeProvider.Supports(GeneratorSupport.GenericTypeReference); 
        } 

        internal QueryHandler(CodeDomProvider codeProvider, DesignTable designTable) { 
            this.codeGenerator = new TypedDataSourceCodeGenerator();
            this.codeGenerator.CodeProvider = codeProvider;
            this.designTable = designTable;
 
            this.languageSupportsNullables = this.codeGenerator.CodeProvider.Supports(GeneratorSupport.GenericTypeReference);
        } 
 
        internal void AddQueriesToDataComponent(CodeTypeDeclaration classDeclaration) {
            AddMainQueriesToDataComponent(classDeclaration); 
            AddSecondaryQueriesToDataComponent(classDeclaration);
            AddUpdateQueriesToDataComponent(classDeclaration);
            AddFunctionsToDataComponent(classDeclaration, false);
        } 

        private void AddMainQueriesToDataComponent(CodeTypeDeclaration classDeclaration) { 
            if(designTable == null) { 
                throw new InternalException("Design Table should not be null.");
            } 

            if(designTable.MainSource != null) {
                QueryGenerator queryGenerator = new QueryGenerator(codeGenerator);
                queryGenerator.DeclarationOnly = this.declarationsOnly; 
                queryGenerator.MethodSource = designTable.MainSource as DbSource;
                queryGenerator.CommandIndex = 0; 
                queryGenerator.DesignTable = designTable; 
                if(queryGenerator.MethodSource.Connection != null) {
                    queryGenerator.ProviderFactory = ProviderManager.GetFactory(queryGenerator.MethodSource.Connection.Provider); 
                }
                else if (designTable.Connection != null) {
                    queryGenerator.ProviderFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
                } 
                else {
                    return; 
                } 

                if(queryGenerator.MethodSource.QueryType == QueryType.Rowset && queryGenerator.MethodSource.CommandOperation == CommandOperation.Select) { 
                    if(queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Fill || queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Both) {
                        queryGenerator.MethodName = designTable.MainSource.GeneratorSourceName;
                        this.GenerateQueries(classDeclaration, queryGenerator);
 
                        if(queryGenerator.MethodSource.GeneratePagingMethods) {
                            queryGenerator.MethodName = designTable.MainSource.GeneratorSourceNameForPaging; 
                            queryGenerator.GeneratePagingMethod = true; 
                            this.GenerateQueries(classDeclaration, queryGenerator);
                            queryGenerator.GeneratePagingMethod = false; 
                        }
                    }

 
                    if(queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Get || queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Both) {
                        queryGenerator.GenerateGetMethod = true; 
                        queryGenerator.MethodName = designTable.MainSource.GeneratorGetMethodName; 
                        this.GenerateQueries(classDeclaration, queryGenerator);
 
                        if(queryGenerator.MethodSource.GeneratePagingMethods) {
                            queryGenerator.MethodName = designTable.MainSource.GeneratorGetMethodNameForPaging;
                            queryGenerator.GeneratePagingMethod = true;
                            this.GenerateQueries(classDeclaration, queryGenerator); 
                            queryGenerator.GeneratePagingMethod = false;
                        } 
 
                    }
                } 
            }
        }

        private void AddSecondaryQueriesToDataComponent(CodeTypeDeclaration classDeclaration) { 
            if(designTable == null) {
                throw new InternalException("Design Table should not be null."); 
            } 
            if(designTable.Sources == null) {
                return; 
            }

            QueryGenerator queryGenerator = new QueryGenerator(codeGenerator);
            queryGenerator.DeclarationOnly = this.declarationsOnly; 
            queryGenerator.DesignTable = designTable;
            if(designTable.Connection != null) { 
                queryGenerator.ProviderFactory = ProviderManager.GetFactory(designTable.Connection.Provider); 
            }
 
            int cmdIndex = 1;
            foreach(Source source in designTable.Sources) {
                queryGenerator.MethodSource = source as DbSource;
                queryGenerator.CommandIndex = cmdIndex++; 

                if(queryGenerator.MethodSource.QueryType != QueryType.Rowset || queryGenerator.MethodSource.CommandOperation != CommandOperation.Select) { 
                    continue; 
                }
 
                if(queryGenerator.MethodSource.Connection != null) {
                    queryGenerator.ProviderFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
                }
 
                if(queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Fill || queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Both) {
                    queryGenerator.GenerateGetMethod = false; 
                    queryGenerator.MethodName = source.GeneratorSourceName; 
                    this.GenerateQueries(classDeclaration, queryGenerator);
 
                    if(queryGenerator.MethodSource.GeneratePagingMethods) {
                        queryGenerator.MethodName = source.GeneratorSourceNameForPaging;
                        queryGenerator.GeneratePagingMethod = true;
                        this.GenerateQueries(classDeclaration, queryGenerator); 
                        queryGenerator.GeneratePagingMethod = false;
                    } 
                } 

                if(queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Get || queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Both) { 
                    queryGenerator.GenerateGetMethod = true;
                    queryGenerator.MethodName = source.GeneratorGetMethodName;
                    this.GenerateQueries(classDeclaration, queryGenerator);
 
                    if(queryGenerator.MethodSource.GeneratePagingMethods) {
                        queryGenerator.MethodName = source.GeneratorGetMethodNameForPaging; 
                        queryGenerator.GeneratePagingMethod = true; 
                        this.GenerateQueries(classDeclaration, queryGenerator);
                        queryGenerator.GeneratePagingMethod = false; 
                    }
                }
            }
        } 

        internal void AddFunctionsToDataComponent(CodeTypeDeclaration classDeclaration, bool isFunctionsDataComponent) { 
            if(designTable == null) { 
                throw new InternalException("Design Table should not be null.");
            } 

            if(!isFunctionsDataComponent) {
                if(designTable.MainSource != null &&
                    ( ((DbSource) designTable.MainSource).QueryType != QueryType.Rowset 
                        || ((DbSource) designTable.MainSource).CommandOperation != CommandOperation.Select )
                ) { 
                    AddFunctionToDataComponent(classDeclaration, (DbSource) designTable.MainSource, 0, isFunctionsDataComponent); 
                }
            } 

            if(designTable.Sources != null) {
                int cmdIndex = 1;
                if(isFunctionsDataComponent) { 
                    cmdIndex = 0;
                } 
 
                foreach(Source source in designTable.Sources) {
                    if( ((DbSource) source).QueryType != QueryType.Rowset || ((DbSource) source).CommandOperation != CommandOperation.Select ) { 
                        AddFunctionToDataComponent(classDeclaration, (DbSource)source, cmdIndex, isFunctionsDataComponent);
                    }

                    cmdIndex++; 
                }
            } 
        } 

        private void AddFunctionToDataComponent(CodeTypeDeclaration classDeclaration, DbSource dbSource, int commandIndex, bool isFunctionsDataComponent) { 
            if(this.DeclarationsOnly && dbSource.Modifier != MemberAttributes.Public) {
                // Don't generate method in interface if the function is not public
                return;
            } 

            FunctionGenerator functionGenerator = new FunctionGenerator(this.codeGenerator); 
            functionGenerator.DeclarationOnly = this.declarationsOnly; 
            functionGenerator.MethodSource = dbSource;
            functionGenerator.CommandIndex = commandIndex; 
            functionGenerator.DesignTable = this.designTable;
            functionGenerator.IsFunctionsDataComponent = isFunctionsDataComponent;
            if(functionGenerator.MethodSource.Connection != null) {
                functionGenerator.ProviderFactory = ProviderManager.GetFactory(functionGenerator.MethodSource.Connection.Provider); 
            }
            else if (designTable.Connection != null) { 
                functionGenerator.ProviderFactory = ProviderManager.GetFactory(designTable.Connection.Provider); 
            }
            else { 
                return;
            }

            functionGenerator.MethodName = dbSource.GeneratorSourceName; 

            functionGenerator.ParameterOption = this.languageSupportsNullables ? ParameterGenerationOption.ClrTypes : ParameterGenerationOption.Objects; 
            CodeMemberMethod currentMethod = functionGenerator.Generate(); 
            if(currentMethod != null) {
                classDeclaration.Members.Add(currentMethod); 
            }

//            if(functionGenerator.GenerateOverloads) {
//                functionGenerator.ParameterOption = ParameterGenerationOption.Objects; 
//                currentMethod = functionGenerator.Generate();
//                if(currentMethod != null) { 
//                    classDeclaration.Members.Add(currentMethod); 
//                }
//            } 
        }


 
        private void AddUpdateQueriesToDataComponent( CodeTypeDeclaration classDeclaration ) {
            Debug.Assert( this.codeGenerator != null ); 
            AddUpdateQueriesToDataComponent( classDeclaration, this.codeGenerator.DataSourceName, 
                                             this.codeGenerator.CodeProvider );
        } 

        internal void AddUpdateQueriesToDataComponent(CodeTypeDeclaration classDeclaration, string dataSourceClassName, CodeDomProvider codeProvider) {
            if(designTable == null) {
                throw new InternalException("Design Table should not be null."); 
            }
            if( StringUtil.EmptyOrSpace(dataSourceClassName) ) { 
                throw new InternalException("Data source class name should not be empty"); 
            }
 
            if (designTable.HasAnyUpdateCommand) {
                UpdateCommandGenerator commandGenerator = new UpdateCommandGenerator(this.codeGenerator);
                commandGenerator.CodeProvider = codeProvider;
                commandGenerator.DeclarationOnly = this.declarationsOnly; 
                commandGenerator.MethodSource = designTable.MainSource as DbSource;
                commandGenerator.DesignTable = designTable; 
                if (designTable.Connection != null) { 
                    commandGenerator.ProviderFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
                } 
                else if (!this.declarationsOnly) {
                    throw new InternalException("DesignTable.Connection should not be null to generate update query statements.");
                }
 
                CodeMemberMethod currentMethod = null;
 
                commandGenerator.MethodName = DataComponentNameHandler.UpdateMethodName; 
                commandGenerator.ActiveCommand = commandGenerator.MethodSource.UpdateCommand;
                commandGenerator.MethodType = MethodTypeEnum.GenericUpdate; 

                commandGenerator.UpdateParameterTypeReference = CodeGenHelper.GlobalType(typeof(System.Data.DataTable));
                commandGenerator.UpdateParameterName = tableParameterName;
                commandGenerator.UpdateParameterTypeName = CodeGenHelper.GetTypeName(codeProvider, dataSourceClassName, this.designTable.GeneratorTableClassName); 

                // DDBugs 126914: Use fully-qualified names for datasets 
                if (this.codeGenerator.DataSetNamespace != null) { 
                    commandGenerator.UpdateParameterTypeName = CodeGenHelper.GetTypeName(this.codeGenerator.CodeProvider, this.codeGenerator.DataSetNamespace, commandGenerator.UpdateParameterTypeName);
                } 

                currentMethod = commandGenerator.Generate();
                if (currentMethod != null) {
                    classDeclaration.Members.Add(currentMethod); 
                }
 
                commandGenerator.UpdateParameterTypeReference = CodeGenHelper.GlobalType(typeof(System.Data.DataSet)); 
                commandGenerator.UpdateParameterName = dataSetParameterName;
                commandGenerator.UpdateParameterTypeName = dataSourceClassName; 

                // DDBugs 126914: Use fully-qualified names for datasets
                if (this.codeGenerator.DataSetNamespace != null) {
                    commandGenerator.UpdateParameterTypeName = CodeGenHelper.GetTypeName(this.codeGenerator.CodeProvider, this.codeGenerator.DataSetNamespace, commandGenerator.UpdateParameterTypeName); 
                }
 
                currentMethod = commandGenerator.Generate(); 
                if (currentMethod != null) {
                    classDeclaration.Members.Add(currentMethod); 
                }

                commandGenerator.UpdateParameterTypeReference = CodeGenHelper.GlobalType(typeof(System.Data.DataRow));
                commandGenerator.UpdateParameterName = "dataRow"; 
                commandGenerator.UpdateParameterTypeName = null;
                currentMethod = commandGenerator.Generate(); 
                if (currentMethod != null) { 
                    classDeclaration.Members.Add(currentMethod);
                } 

                commandGenerator.UpdateParameterTypeReference = CodeGenHelper.GlobalType(typeof(System.Data.DataRow), 1);
                commandGenerator.UpdateParameterName = "dataRows";
                commandGenerator.UpdateParameterTypeName = null; 
                currentMethod = commandGenerator.Generate();
                if (currentMethod != null) { 
                    classDeclaration.Members.Add(currentMethod); 
                }
 
                if (commandGenerator.MethodSource.GenerateShortCommands) {
                    commandGenerator.MethodType = MethodTypeEnum.ColumnParameters;

                    commandGenerator.ActiveCommand = commandGenerator.MethodSource.DeleteCommand; 
                    if (commandGenerator.ActiveCommand != null) {
                        commandGenerator.MethodName = DataComponentNameHandler.DeleteMethodName; 
                        commandGenerator.UpdateCommandName = "DeleteCommand"; 

                        commandGenerator.ParameterOption = this.languageSupportsNullables ? ParameterGenerationOption.ClrTypes : ParameterGenerationOption.Objects; 
                        currentMethod = commandGenerator.Generate();
                        if (currentMethod != null) {
                            classDeclaration.Members.Add(currentMethod);
                        } 
                    }
 
                    commandGenerator.ActiveCommand = commandGenerator.MethodSource.InsertCommand; 
                    if (commandGenerator.ActiveCommand != null) {
                        commandGenerator.MethodName = DataComponentNameHandler.InsertMethodName; 
                        commandGenerator.UpdateCommandName = "InsertCommand";

                        commandGenerator.ParameterOption = this.languageSupportsNullables ? ParameterGenerationOption.ClrTypes : ParameterGenerationOption.Objects;
                        currentMethod = commandGenerator.Generate(); 
                        if (currentMethod != null) {
                            classDeclaration.Members.Add(currentMethod); 
                        } 
                    }
 
                    commandGenerator.ActiveCommand = commandGenerator.MethodSource.UpdateCommand;
                    if (commandGenerator.ActiveCommand != null) {
                        commandGenerator.MethodName = DataComponentNameHandler.UpdateMethodName;
                        commandGenerator.UpdateCommandName = "UpdateCommand"; 

                        commandGenerator.ParameterOption = this.languageSupportsNullables ? ParameterGenerationOption.ClrTypes : ParameterGenerationOption.Objects; 
                        currentMethod = commandGenerator.Generate(); 
                        if (currentMethod != null) {
                            classDeclaration.Members.Add(currentMethod); 
                            // DevDiv Bugs # 75077 -- Adding an overload Update Method
                            // DevDiv Bugs # 106672 -- Always generate overload Update Method now
                            currentMethod = null;
                            commandGenerator.GenerateOverloadWithoutCurrentPKParameters = true; 
                            try {
                                currentMethod = commandGenerator.Generate(); 
                            } 
                            finally {
                                commandGenerator.GenerateOverloadWithoutCurrentPKParameters = false; 
                            }
                            if (currentMethod != null) {
                                classDeclaration.Members.Add(currentMethod);
                            } 
                        }
                    } 
                } 

            } 
        }


        private void GenerateQueries(CodeTypeDeclaration classDeclaration, QueryGenerator queryGenerator) { 
            CodeMemberMethod currentMethod = null;
 
            if(queryGenerator.DeclarationOnly) { 
                // Don't generate the method for the interface if the query is not supposed to be public
                if(!queryGenerator.GenerateGetMethod && queryGenerator.MethodSource.Modifier != MemberAttributes.Public) { 
                    return;
                }
                if(queryGenerator.GenerateGetMethod && queryGenerator.MethodSource.GetMethodModifier != MemberAttributes.Public) {
                    return; 
                }
            } 
 
            queryGenerator.ContainerParameterType = typeof(System.Data.DataTable);
            queryGenerator.ContainerParameterTypeName = CodeGenHelper.GetTypeName(this.codeGenerator.CodeProvider, this.codeGenerator.DataSourceName, this.designTable.GeneratorTableClassName); 

            // DDBugs 126914: Use fully-qualified names for datasets
            if (this.codeGenerator.DataSetNamespace != null) {
                queryGenerator.ContainerParameterTypeName = CodeGenHelper.GetTypeName(this.codeGenerator.CodeProvider, this.codeGenerator.DataSetNamespace, queryGenerator.ContainerParameterTypeName); 
            }
 
            queryGenerator.ContainerParameterName = tableParameterName; 
            queryGenerator.ParameterOption = this.languageSupportsNullables ? ParameterGenerationOption.ClrTypes : ParameterGenerationOption.Objects;
            currentMethod = queryGenerator.Generate(); 
            if(currentMethod != null) {
                classDeclaration.Members.Add(currentMethod);
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
    using System.CodeDom.Compiler;
    using System.ComponentModel.Design;
    using System.Collections; 
    using System.ComponentModel;
    using System.Data; 
    using System.Data.SqlClient; 
    using System.Data.SqlTypes;
    using System.Diagnostics; 

    internal class QueryHandler {
        internal    const string tableParameterName = "dataTable";
        internal    const string dataSetParameterName = "dataSet"; 

        private     TypedDataSourceCodeGenerator codeGenerator = null; 
        private     DesignTable designTable = null; 
        private     bool declarationsOnly = false;
        private     bool languageSupportsNullables = false; 

        internal bool DeclarationsOnly {
            get {
                return this.declarationsOnly; 
            }
            set { 
                this.declarationsOnly = value; 
            }
        } 

        internal QueryHandler(TypedDataSourceCodeGenerator codeGenerator, DesignTable designTable) {
            this.codeGenerator = codeGenerator;
            this.designTable = designTable; 

            this.languageSupportsNullables = this.codeGenerator.CodeProvider.Supports(GeneratorSupport.GenericTypeReference); 
        } 

        internal QueryHandler(CodeDomProvider codeProvider, DesignTable designTable) { 
            this.codeGenerator = new TypedDataSourceCodeGenerator();
            this.codeGenerator.CodeProvider = codeProvider;
            this.designTable = designTable;
 
            this.languageSupportsNullables = this.codeGenerator.CodeProvider.Supports(GeneratorSupport.GenericTypeReference);
        } 
 
        internal void AddQueriesToDataComponent(CodeTypeDeclaration classDeclaration) {
            AddMainQueriesToDataComponent(classDeclaration); 
            AddSecondaryQueriesToDataComponent(classDeclaration);
            AddUpdateQueriesToDataComponent(classDeclaration);
            AddFunctionsToDataComponent(classDeclaration, false);
        } 

        private void AddMainQueriesToDataComponent(CodeTypeDeclaration classDeclaration) { 
            if(designTable == null) { 
                throw new InternalException("Design Table should not be null.");
            } 

            if(designTable.MainSource != null) {
                QueryGenerator queryGenerator = new QueryGenerator(codeGenerator);
                queryGenerator.DeclarationOnly = this.declarationsOnly; 
                queryGenerator.MethodSource = designTable.MainSource as DbSource;
                queryGenerator.CommandIndex = 0; 
                queryGenerator.DesignTable = designTable; 
                if(queryGenerator.MethodSource.Connection != null) {
                    queryGenerator.ProviderFactory = ProviderManager.GetFactory(queryGenerator.MethodSource.Connection.Provider); 
                }
                else if (designTable.Connection != null) {
                    queryGenerator.ProviderFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
                } 
                else {
                    return; 
                } 

                if(queryGenerator.MethodSource.QueryType == QueryType.Rowset && queryGenerator.MethodSource.CommandOperation == CommandOperation.Select) { 
                    if(queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Fill || queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Both) {
                        queryGenerator.MethodName = designTable.MainSource.GeneratorSourceName;
                        this.GenerateQueries(classDeclaration, queryGenerator);
 
                        if(queryGenerator.MethodSource.GeneratePagingMethods) {
                            queryGenerator.MethodName = designTable.MainSource.GeneratorSourceNameForPaging; 
                            queryGenerator.GeneratePagingMethod = true; 
                            this.GenerateQueries(classDeclaration, queryGenerator);
                            queryGenerator.GeneratePagingMethod = false; 
                        }
                    }

 
                    if(queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Get || queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Both) {
                        queryGenerator.GenerateGetMethod = true; 
                        queryGenerator.MethodName = designTable.MainSource.GeneratorGetMethodName; 
                        this.GenerateQueries(classDeclaration, queryGenerator);
 
                        if(queryGenerator.MethodSource.GeneratePagingMethods) {
                            queryGenerator.MethodName = designTable.MainSource.GeneratorGetMethodNameForPaging;
                            queryGenerator.GeneratePagingMethod = true;
                            this.GenerateQueries(classDeclaration, queryGenerator); 
                            queryGenerator.GeneratePagingMethod = false;
                        } 
 
                    }
                } 
            }
        }

        private void AddSecondaryQueriesToDataComponent(CodeTypeDeclaration classDeclaration) { 
            if(designTable == null) {
                throw new InternalException("Design Table should not be null."); 
            } 
            if(designTable.Sources == null) {
                return; 
            }

            QueryGenerator queryGenerator = new QueryGenerator(codeGenerator);
            queryGenerator.DeclarationOnly = this.declarationsOnly; 
            queryGenerator.DesignTable = designTable;
            if(designTable.Connection != null) { 
                queryGenerator.ProviderFactory = ProviderManager.GetFactory(designTable.Connection.Provider); 
            }
 
            int cmdIndex = 1;
            foreach(Source source in designTable.Sources) {
                queryGenerator.MethodSource = source as DbSource;
                queryGenerator.CommandIndex = cmdIndex++; 

                if(queryGenerator.MethodSource.QueryType != QueryType.Rowset || queryGenerator.MethodSource.CommandOperation != CommandOperation.Select) { 
                    continue; 
                }
 
                if(queryGenerator.MethodSource.Connection != null) {
                    queryGenerator.ProviderFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
                }
 
                if(queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Fill || queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Both) {
                    queryGenerator.GenerateGetMethod = false; 
                    queryGenerator.MethodName = source.GeneratorSourceName; 
                    this.GenerateQueries(classDeclaration, queryGenerator);
 
                    if(queryGenerator.MethodSource.GeneratePagingMethods) {
                        queryGenerator.MethodName = source.GeneratorSourceNameForPaging;
                        queryGenerator.GeneratePagingMethod = true;
                        this.GenerateQueries(classDeclaration, queryGenerator); 
                        queryGenerator.GeneratePagingMethod = false;
                    } 
                } 

                if(queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Get || queryGenerator.MethodSource.GenerateMethods == GenerateMethodTypes.Both) { 
                    queryGenerator.GenerateGetMethod = true;
                    queryGenerator.MethodName = source.GeneratorGetMethodName;
                    this.GenerateQueries(classDeclaration, queryGenerator);
 
                    if(queryGenerator.MethodSource.GeneratePagingMethods) {
                        queryGenerator.MethodName = source.GeneratorGetMethodNameForPaging; 
                        queryGenerator.GeneratePagingMethod = true; 
                        this.GenerateQueries(classDeclaration, queryGenerator);
                        queryGenerator.GeneratePagingMethod = false; 
                    }
                }
            }
        } 

        internal void AddFunctionsToDataComponent(CodeTypeDeclaration classDeclaration, bool isFunctionsDataComponent) { 
            if(designTable == null) { 
                throw new InternalException("Design Table should not be null.");
            } 

            if(!isFunctionsDataComponent) {
                if(designTable.MainSource != null &&
                    ( ((DbSource) designTable.MainSource).QueryType != QueryType.Rowset 
                        || ((DbSource) designTable.MainSource).CommandOperation != CommandOperation.Select )
                ) { 
                    AddFunctionToDataComponent(classDeclaration, (DbSource) designTable.MainSource, 0, isFunctionsDataComponent); 
                }
            } 

            if(designTable.Sources != null) {
                int cmdIndex = 1;
                if(isFunctionsDataComponent) { 
                    cmdIndex = 0;
                } 
 
                foreach(Source source in designTable.Sources) {
                    if( ((DbSource) source).QueryType != QueryType.Rowset || ((DbSource) source).CommandOperation != CommandOperation.Select ) { 
                        AddFunctionToDataComponent(classDeclaration, (DbSource)source, cmdIndex, isFunctionsDataComponent);
                    }

                    cmdIndex++; 
                }
            } 
        } 

        private void AddFunctionToDataComponent(CodeTypeDeclaration classDeclaration, DbSource dbSource, int commandIndex, bool isFunctionsDataComponent) { 
            if(this.DeclarationsOnly && dbSource.Modifier != MemberAttributes.Public) {
                // Don't generate method in interface if the function is not public
                return;
            } 

            FunctionGenerator functionGenerator = new FunctionGenerator(this.codeGenerator); 
            functionGenerator.DeclarationOnly = this.declarationsOnly; 
            functionGenerator.MethodSource = dbSource;
            functionGenerator.CommandIndex = commandIndex; 
            functionGenerator.DesignTable = this.designTable;
            functionGenerator.IsFunctionsDataComponent = isFunctionsDataComponent;
            if(functionGenerator.MethodSource.Connection != null) {
                functionGenerator.ProviderFactory = ProviderManager.GetFactory(functionGenerator.MethodSource.Connection.Provider); 
            }
            else if (designTable.Connection != null) { 
                functionGenerator.ProviderFactory = ProviderManager.GetFactory(designTable.Connection.Provider); 
            }
            else { 
                return;
            }

            functionGenerator.MethodName = dbSource.GeneratorSourceName; 

            functionGenerator.ParameterOption = this.languageSupportsNullables ? ParameterGenerationOption.ClrTypes : ParameterGenerationOption.Objects; 
            CodeMemberMethod currentMethod = functionGenerator.Generate(); 
            if(currentMethod != null) {
                classDeclaration.Members.Add(currentMethod); 
            }

//            if(functionGenerator.GenerateOverloads) {
//                functionGenerator.ParameterOption = ParameterGenerationOption.Objects; 
//                currentMethod = functionGenerator.Generate();
//                if(currentMethod != null) { 
//                    classDeclaration.Members.Add(currentMethod); 
//                }
//            } 
        }


 
        private void AddUpdateQueriesToDataComponent( CodeTypeDeclaration classDeclaration ) {
            Debug.Assert( this.codeGenerator != null ); 
            AddUpdateQueriesToDataComponent( classDeclaration, this.codeGenerator.DataSourceName, 
                                             this.codeGenerator.CodeProvider );
        } 

        internal void AddUpdateQueriesToDataComponent(CodeTypeDeclaration classDeclaration, string dataSourceClassName, CodeDomProvider codeProvider) {
            if(designTable == null) {
                throw new InternalException("Design Table should not be null."); 
            }
            if( StringUtil.EmptyOrSpace(dataSourceClassName) ) { 
                throw new InternalException("Data source class name should not be empty"); 
            }
 
            if (designTable.HasAnyUpdateCommand) {
                UpdateCommandGenerator commandGenerator = new UpdateCommandGenerator(this.codeGenerator);
                commandGenerator.CodeProvider = codeProvider;
                commandGenerator.DeclarationOnly = this.declarationsOnly; 
                commandGenerator.MethodSource = designTable.MainSource as DbSource;
                commandGenerator.DesignTable = designTable; 
                if (designTable.Connection != null) { 
                    commandGenerator.ProviderFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
                } 
                else if (!this.declarationsOnly) {
                    throw new InternalException("DesignTable.Connection should not be null to generate update query statements.");
                }
 
                CodeMemberMethod currentMethod = null;
 
                commandGenerator.MethodName = DataComponentNameHandler.UpdateMethodName; 
                commandGenerator.ActiveCommand = commandGenerator.MethodSource.UpdateCommand;
                commandGenerator.MethodType = MethodTypeEnum.GenericUpdate; 

                commandGenerator.UpdateParameterTypeReference = CodeGenHelper.GlobalType(typeof(System.Data.DataTable));
                commandGenerator.UpdateParameterName = tableParameterName;
                commandGenerator.UpdateParameterTypeName = CodeGenHelper.GetTypeName(codeProvider, dataSourceClassName, this.designTable.GeneratorTableClassName); 

                // DDBugs 126914: Use fully-qualified names for datasets 
                if (this.codeGenerator.DataSetNamespace != null) { 
                    commandGenerator.UpdateParameterTypeName = CodeGenHelper.GetTypeName(this.codeGenerator.CodeProvider, this.codeGenerator.DataSetNamespace, commandGenerator.UpdateParameterTypeName);
                } 

                currentMethod = commandGenerator.Generate();
                if (currentMethod != null) {
                    classDeclaration.Members.Add(currentMethod); 
                }
 
                commandGenerator.UpdateParameterTypeReference = CodeGenHelper.GlobalType(typeof(System.Data.DataSet)); 
                commandGenerator.UpdateParameterName = dataSetParameterName;
                commandGenerator.UpdateParameterTypeName = dataSourceClassName; 

                // DDBugs 126914: Use fully-qualified names for datasets
                if (this.codeGenerator.DataSetNamespace != null) {
                    commandGenerator.UpdateParameterTypeName = CodeGenHelper.GetTypeName(this.codeGenerator.CodeProvider, this.codeGenerator.DataSetNamespace, commandGenerator.UpdateParameterTypeName); 
                }
 
                currentMethod = commandGenerator.Generate(); 
                if (currentMethod != null) {
                    classDeclaration.Members.Add(currentMethod); 
                }

                commandGenerator.UpdateParameterTypeReference = CodeGenHelper.GlobalType(typeof(System.Data.DataRow));
                commandGenerator.UpdateParameterName = "dataRow"; 
                commandGenerator.UpdateParameterTypeName = null;
                currentMethod = commandGenerator.Generate(); 
                if (currentMethod != null) { 
                    classDeclaration.Members.Add(currentMethod);
                } 

                commandGenerator.UpdateParameterTypeReference = CodeGenHelper.GlobalType(typeof(System.Data.DataRow), 1);
                commandGenerator.UpdateParameterName = "dataRows";
                commandGenerator.UpdateParameterTypeName = null; 
                currentMethod = commandGenerator.Generate();
                if (currentMethod != null) { 
                    classDeclaration.Members.Add(currentMethod); 
                }
 
                if (commandGenerator.MethodSource.GenerateShortCommands) {
                    commandGenerator.MethodType = MethodTypeEnum.ColumnParameters;

                    commandGenerator.ActiveCommand = commandGenerator.MethodSource.DeleteCommand; 
                    if (commandGenerator.ActiveCommand != null) {
                        commandGenerator.MethodName = DataComponentNameHandler.DeleteMethodName; 
                        commandGenerator.UpdateCommandName = "DeleteCommand"; 

                        commandGenerator.ParameterOption = this.languageSupportsNullables ? ParameterGenerationOption.ClrTypes : ParameterGenerationOption.Objects; 
                        currentMethod = commandGenerator.Generate();
                        if (currentMethod != null) {
                            classDeclaration.Members.Add(currentMethod);
                        } 
                    }
 
                    commandGenerator.ActiveCommand = commandGenerator.MethodSource.InsertCommand; 
                    if (commandGenerator.ActiveCommand != null) {
                        commandGenerator.MethodName = DataComponentNameHandler.InsertMethodName; 
                        commandGenerator.UpdateCommandName = "InsertCommand";

                        commandGenerator.ParameterOption = this.languageSupportsNullables ? ParameterGenerationOption.ClrTypes : ParameterGenerationOption.Objects;
                        currentMethod = commandGenerator.Generate(); 
                        if (currentMethod != null) {
                            classDeclaration.Members.Add(currentMethod); 
                        } 
                    }
 
                    commandGenerator.ActiveCommand = commandGenerator.MethodSource.UpdateCommand;
                    if (commandGenerator.ActiveCommand != null) {
                        commandGenerator.MethodName = DataComponentNameHandler.UpdateMethodName;
                        commandGenerator.UpdateCommandName = "UpdateCommand"; 

                        commandGenerator.ParameterOption = this.languageSupportsNullables ? ParameterGenerationOption.ClrTypes : ParameterGenerationOption.Objects; 
                        currentMethod = commandGenerator.Generate(); 
                        if (currentMethod != null) {
                            classDeclaration.Members.Add(currentMethod); 
                            // DevDiv Bugs # 75077 -- Adding an overload Update Method
                            // DevDiv Bugs # 106672 -- Always generate overload Update Method now
                            currentMethod = null;
                            commandGenerator.GenerateOverloadWithoutCurrentPKParameters = true; 
                            try {
                                currentMethod = commandGenerator.Generate(); 
                            } 
                            finally {
                                commandGenerator.GenerateOverloadWithoutCurrentPKParameters = false; 
                            }
                            if (currentMethod != null) {
                                classDeclaration.Members.Add(currentMethod);
                            } 
                        }
                    } 
                } 

            } 
        }


        private void GenerateQueries(CodeTypeDeclaration classDeclaration, QueryGenerator queryGenerator) { 
            CodeMemberMethod currentMethod = null;
 
            if(queryGenerator.DeclarationOnly) { 
                // Don't generate the method for the interface if the query is not supposed to be public
                if(!queryGenerator.GenerateGetMethod && queryGenerator.MethodSource.Modifier != MemberAttributes.Public) { 
                    return;
                }
                if(queryGenerator.GenerateGetMethod && queryGenerator.MethodSource.GetMethodModifier != MemberAttributes.Public) {
                    return; 
                }
            } 
 
            queryGenerator.ContainerParameterType = typeof(System.Data.DataTable);
            queryGenerator.ContainerParameterTypeName = CodeGenHelper.GetTypeName(this.codeGenerator.CodeProvider, this.codeGenerator.DataSourceName, this.designTable.GeneratorTableClassName); 

            // DDBugs 126914: Use fully-qualified names for datasets
            if (this.codeGenerator.DataSetNamespace != null) {
                queryGenerator.ContainerParameterTypeName = CodeGenHelper.GetTypeName(this.codeGenerator.CodeProvider, this.codeGenerator.DataSetNamespace, queryGenerator.ContainerParameterTypeName); 
            }
 
            queryGenerator.ContainerParameterName = tableParameterName; 
            queryGenerator.ParameterOption = this.languageSupportsNullables ? ParameterGenerationOption.ClrTypes : ParameterGenerationOption.Objects;
            currentMethod = queryGenerator.Generate(); 
            if(currentMethod != null) {
                classDeclaration.Members.Add(currentMethod);
            }
        } 

    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
