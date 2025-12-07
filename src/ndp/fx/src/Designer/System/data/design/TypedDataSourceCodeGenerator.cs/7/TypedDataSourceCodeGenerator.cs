//------------------------------------------------------------------------------ 
// <copyright company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System.Diagnostics; 
    using System;
    using System.IO;
    using System.Data;
    using System.Data.Common; 
    using System.CodeDom;
    using System.Text; 
    using System.Xml; 
    using System.Xml.Serialization;
    using System.Collections; 
    using System.Reflection;
    using System.Globalization;

    using System.CodeDom.Compiler; 
    using System.Runtime.InteropServices;
 
    using GenerateOption = TypedDataSetGenerator.GenerateOption; 

    internal enum TypeEnum { 
        CLR,
        SQL,
    }
 
    internal enum DbObjectType {
        Unknown, 
        Table, 
        View,
        StoredProcedure, 
        Function,
        Package,
        PackageBody
    } 

    internal enum CommandOperation { 
        Unknown, 
        Select,
        Insert, 
        Update,
        Delete
    }
 
    // Well-known, invariant provider names.
    internal class ManagedProviderNames { 
        // private constructor to avoid class being instantiated. 
        private ManagedProviderNames() { }
 
        public static string SqlClient {
            get {
                return "System.Data.SqlClient";
            } 
        }
    } 
 

    internal sealed class TypedDataSourceCodeGenerator{ 
        private DesignDataSource    designDataSource = null;

        private CodeDomProvider     codeProvider = null;
        private ArrayList           problemList = new ArrayList(); 
        private TypedTableHandler   tableHandler = null;
        private RelationHandler     relationHandler = null; 
        private TypedRowHandler     rowHandler = null; 
        private bool                generateExtendedProperties = false;
        private IDictionary         userData = null; 
        private bool                generateSingleNamespace = false;
        private GenerateOption      generateOption;
        private string              dataSetNamespace = null;
 
        internal TypedDataSourceCodeGenerator() : base() { }
 
        internal CodeDomProvider CodeProvider { 
            get {
                return this.codeProvider; 
            }
            set {
                this.codeProvider = value;
            } 
        }
 
        internal IDictionary UserData { 
            get {
                return userData; 
            }
            set {
                userData = value;
            } 
        }
 
        internal string DataSourceName { 
            get {
                return designDataSource.GeneratorDataSetName; 
            }
        }

        internal ArrayList ProblemList { 
            get {
                return problemList; 
            } 
        }
 

        internal TypedTableHandler TableHandler {
            get {
                return tableHandler; 
            }
        } 
 
        internal RelationHandler RelationHandler {
            get { 
                return relationHandler;
            }
        }
 
        internal TypedRowHandler RowHandler {
            get { 
                return rowHandler; 
            }
        } 

        internal bool GenerateExtendedProperties {
            get {
                return generateExtendedProperties; 
            }
        } 
 
        internal bool GenerateSingleNamespace {
            get { 
                return generateSingleNamespace;
            }
            set {
                generateSingleNamespace = value; 
            }
        } 
 
        internal GenerateOption GenerateOptions {
            get { 
                return generateOption;
            }
        }
 
        internal string DataSetNamespace {
            get { 
                return dataSetNamespace; 
            }
        } 

        internal void GenerateDataSource(DesignDataSource dtDataSource, CodeCompileUnit codeCompileUnit, CodeNamespace mainNamespace, string dataSetNamespace, GenerateOption generateOption) {
            Debug.Assert(dtDataSource != null);
            designDataSource = dtDataSource; 

            this.generateOption = generateOption; 
            this.dataSetNamespace = dataSetNamespace; 

            bool generateHierarchicalUpdate = (generateOption & GenerateOption.HierarchicalUpdate) == GenerateOption.HierarchicalUpdate; 
            generateHierarchicalUpdate = generateHierarchicalUpdate && dtDataSource.EnableTableAdapterManager;

            AddUserData(codeCompileUnit);
 
            // create the typed datasource class declaration and add it to the namespace
            CodeTypeDeclaration dataSourceClass = CreateDataSourceDeclaration(dtDataSource); 
            mainNamespace.Types.Add(dataSourceClass); 

            bool supportsMultipleNamespaces = CodeGenHelper.SupportsMultipleNamespaces(this.codeProvider); 
            CodeNamespace adaptersNamespace = null;
            if (!GenerateSingleNamespace && supportsMultipleNamespaces) {
                string adaptersNsName = this.CreateAdaptersNamespace(dtDataSource.GeneratorDataSetName);
                if (!StringUtil.Empty(mainNamespace.Name)) { 
                    adaptersNsName = mainNamespace.Name + "." + adaptersNsName;
                } 
 
                adaptersNamespace = new CodeNamespace(adaptersNsName);
            } 

            DataComponentGenerator componentGenerator = new DataComponentGenerator(this);
            bool hasAnyTableAdapter = false;
            foreach(DesignTable table in dtDataSource.DesignTables) { 
                if(table.TableType != TableType.RadTable) {
                    continue; 
                } 
                hasAnyTableAdapter = true;
 
                table.PropertyCache = new DesignTable.CodeGenPropertyCache(table);

                CodeTypeDeclaration componentType = componentGenerator.GenerateDataComponent(table, false, generateHierarchicalUpdate);
 
                if (GenerateSingleNamespace) {
                    mainNamespace.Types.Add(componentType); 
                } 
                else {
                    if (supportsMultipleNamespaces) { 
                        adaptersNamespace.Types.Add(componentType);
                    }
                    else {
                        componentType.Name = dataSourceClass.Name + componentType.Name; 
                        mainNamespace.Types.Add(componentType);
                    } 
                } 
            }
 
            generateHierarchicalUpdate = generateHierarchicalUpdate && hasAnyTableAdapter;

            if(dtDataSource.Sources != null && dtDataSource.Sources.Count > 0) {
                // create a 'fake' table and set names and sources on it 
                DesignTable functionsTable = new DesignTable();
                functionsTable.TableType = TableType.RadTable; 
                functionsTable.MainSource = null; 
                functionsTable.GeneratorDataComponentClassName = dtDataSource.GeneratorFunctionsComponentClassName;
 
                foreach(Source source in dtDataSource.Sources) {
                    functionsTable.Sources.Add(source);
                }
 
                CodeTypeDeclaration componentType = componentGenerator.GenerateDataComponent(functionsTable, true, generateHierarchicalUpdate);
 
                // generate the FunctionsDataComponent out of the fake table 
                if (GenerateSingleNamespace) {
                    mainNamespace.Types.Add(componentType); 
                }
                else {
                    if (supportsMultipleNamespaces) {
                        adaptersNamespace.Types.Add(componentType); 
                    }
                    else { 
                        componentType.Name = dataSourceClass.Name + componentType.Name; 
                        mainNamespace.Types.Add(componentType);
                    } 
                }
            }

            if (adaptersNamespace != null && adaptersNamespace.Types.Count > 0) { 
                codeCompileUnit.Namespaces.Add(adaptersNamespace);
            } 
 
            if (generateHierarchicalUpdate) {
                TableAdapterManagerGenerator adapterManagerGenerator = new TableAdapterManagerGenerator(this); 
                CodeTypeDeclaration adapterManagerType = adapterManagerGenerator.GenerateAdapterManager(designDataSource,dataSourceClass);

                if (GenerateSingleNamespace) {
                    mainNamespace.Types.Add(adapterManagerType); 
                }
                else { 
                    if (supportsMultipleNamespaces) { 
                        adaptersNamespace.Types.Add(adapterManagerType);
                    } 
                    else {
                        adapterManagerType.Name = dataSourceClass.Name + adapterManagerType.Name;
                        mainNamespace.Types.Add(adapterManagerType);
                    } 
                }
            } 
        } 

        private void AddUserData(CodeCompileUnit compileUnit) { 
            if (this.UserData != null) {
                foreach (object key in this.UserData.Keys) {
                    compileUnit.UserData.Add(key, userData[key]);
                } 
            }
        } 
 
        private CodeTypeDeclaration CreateDataSourceDeclaration(DesignDataSource dtDataSource) {
            // Let's check if we have a DataSource name 
            if( dtDataSource.Name == null ) {
                //
                throw new DataSourceGeneratorException("DataSource name cannot be null.");
            } 

            // Ensure the generator names are set in the DesignDataSource 
            NameHandler nameHandler = new NameHandler(this.codeProvider); 
            nameHandler.GenerateMemberNames(dtDataSource, this.problemList);
 
            // Create CodeTypeDeclaration for DataSource class
            CodeTypeDeclaration dataSourceClass = CodeGenHelper.Class(dtDataSource.GeneratorDataSetName, true, dtDataSource.Modifier);

            // Set BaseType 
            dataSourceClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(System.Data.DataSet)));
 
            // Set Attributes 
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.Serializable"));
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerCategoryAttribute", CodeGenHelper.Str("code"))); 
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.ToolboxItem", CodeGenHelper.Primitive(true)));
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(XmlSchemaProviderAttribute).FullName, CodeGenHelper.Primitive("GetTypedDataSetSchema")));
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(XmlRootAttribute).FullName, CodeGenHelper.Primitive(dtDataSource.GeneratorDataSetName)));
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.Design.HelpKeywordAttribute).FullName, CodeGenHelper.Str("vs.data.DataSet"))); 

            dataSourceClass.Comments.Add(CodeGenHelper.Comment("Represents a strongly typed in-memory cache of data.", true)); 
 

            // Create and Init the Table Handler 
            tableHandler = new TypedTableHandler(this, dtDataSource.DesignTables);
            // Create and Init the Relation Handler
            relationHandler = new RelationHandler(this, dtDataSource.DesignRelations);
            // Create the Typed Row Handler 
            rowHandler = new TypedRowHandler(this, dtDataSource.DesignTables);
            // Create and Init the Typed DataSet Method Generator 
            DatasetMethodGenerator dsMethodGenerator = new DatasetMethodGenerator(this, dtDataSource); 

 
            // Generate 1 private variable for each typed table
            tableHandler.AddPrivateVars(dataSourceClass);
            // Generate 1 public property for each typed table
            tableHandler.AddTableProperties(dataSourceClass); 

            // Generate 1 private variable for each relation 
            relationHandler.AddPrivateVars(dataSourceClass); 

            // Generate typed dataset methods 
            dsMethodGenerator.AddMethods(dataSourceClass);

            // Generate typed row event handlers
            rowHandler.AddTypedRowEventHandlers(dataSourceClass); 

            // Generate typed table classes 
            tableHandler.AddTableClasses(dataSourceClass); 

            // Generate typed row classes 
            rowHandler.AddTypedRows(dataSourceClass);

            // Generate typed row event args
            rowHandler.AddTypedRowEventArgs(dataSourceClass); 

#if _WHACK_ // twhitney: we don't use default instances anymore 
#endif 

            return dataSourceClass; 
        }

        internal static ArrayList GetProviderAssemblies(DesignDataSource designDS) {
            Debug.Assert(designDS != null); 

            ArrayList providerAssemblies = new ArrayList(); 
 
            foreach (IDesignConnection connection in designDS.DesignConnections) {
                IDbConnection dbConnection = connection.CreateEmptyDbConnection(); 
                Debug.Assert(dbConnection != null);

                if (dbConnection != null) {
                    Assembly providerAssembly = dbConnection.GetType().Assembly; 

                    if (!providerAssemblies.Contains(providerAssembly)) { 
                        providerAssemblies.Add(providerAssembly); 
                    }
                } 
            }

            return providerAssemblies;
        } 

        private string CreateAdaptersNamespace(string generatorDataSetName) { 
            if (generatorDataSetName.StartsWith("[", StringComparison.Ordinal) && generatorDataSetName.EndsWith("]", StringComparison.Ordinal)) { 
                // special case for VB: the class name can be enclosed in brackets, but if we concatenate it to
                // create the namespace name it's not valid any more (see VSWhidbey #361647). 
                generatorDataSetName = generatorDataSetName.Substring(1, generatorDataSetName.Length - 2);
            }

            return MemberNameValidator.GenerateIdName(generatorDataSetName + "TableAdapters", this.CodeProvider, false/*useSuffix*/); 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System.Diagnostics; 
    using System;
    using System.IO;
    using System.Data;
    using System.Data.Common; 
    using System.CodeDom;
    using System.Text; 
    using System.Xml; 
    using System.Xml.Serialization;
    using System.Collections; 
    using System.Reflection;
    using System.Globalization;

    using System.CodeDom.Compiler; 
    using System.Runtime.InteropServices;
 
    using GenerateOption = TypedDataSetGenerator.GenerateOption; 

    internal enum TypeEnum { 
        CLR,
        SQL,
    }
 
    internal enum DbObjectType {
        Unknown, 
        Table, 
        View,
        StoredProcedure, 
        Function,
        Package,
        PackageBody
    } 

    internal enum CommandOperation { 
        Unknown, 
        Select,
        Insert, 
        Update,
        Delete
    }
 
    // Well-known, invariant provider names.
    internal class ManagedProviderNames { 
        // private constructor to avoid class being instantiated. 
        private ManagedProviderNames() { }
 
        public static string SqlClient {
            get {
                return "System.Data.SqlClient";
            } 
        }
    } 
 

    internal sealed class TypedDataSourceCodeGenerator{ 
        private DesignDataSource    designDataSource = null;

        private CodeDomProvider     codeProvider = null;
        private ArrayList           problemList = new ArrayList(); 
        private TypedTableHandler   tableHandler = null;
        private RelationHandler     relationHandler = null; 
        private TypedRowHandler     rowHandler = null; 
        private bool                generateExtendedProperties = false;
        private IDictionary         userData = null; 
        private bool                generateSingleNamespace = false;
        private GenerateOption      generateOption;
        private string              dataSetNamespace = null;
 
        internal TypedDataSourceCodeGenerator() : base() { }
 
        internal CodeDomProvider CodeProvider { 
            get {
                return this.codeProvider; 
            }
            set {
                this.codeProvider = value;
            } 
        }
 
        internal IDictionary UserData { 
            get {
                return userData; 
            }
            set {
                userData = value;
            } 
        }
 
        internal string DataSourceName { 
            get {
                return designDataSource.GeneratorDataSetName; 
            }
        }

        internal ArrayList ProblemList { 
            get {
                return problemList; 
            } 
        }
 

        internal TypedTableHandler TableHandler {
            get {
                return tableHandler; 
            }
        } 
 
        internal RelationHandler RelationHandler {
            get { 
                return relationHandler;
            }
        }
 
        internal TypedRowHandler RowHandler {
            get { 
                return rowHandler; 
            }
        } 

        internal bool GenerateExtendedProperties {
            get {
                return generateExtendedProperties; 
            }
        } 
 
        internal bool GenerateSingleNamespace {
            get { 
                return generateSingleNamespace;
            }
            set {
                generateSingleNamespace = value; 
            }
        } 
 
        internal GenerateOption GenerateOptions {
            get { 
                return generateOption;
            }
        }
 
        internal string DataSetNamespace {
            get { 
                return dataSetNamespace; 
            }
        } 

        internal void GenerateDataSource(DesignDataSource dtDataSource, CodeCompileUnit codeCompileUnit, CodeNamespace mainNamespace, string dataSetNamespace, GenerateOption generateOption) {
            Debug.Assert(dtDataSource != null);
            designDataSource = dtDataSource; 

            this.generateOption = generateOption; 
            this.dataSetNamespace = dataSetNamespace; 

            bool generateHierarchicalUpdate = (generateOption & GenerateOption.HierarchicalUpdate) == GenerateOption.HierarchicalUpdate; 
            generateHierarchicalUpdate = generateHierarchicalUpdate && dtDataSource.EnableTableAdapterManager;

            AddUserData(codeCompileUnit);
 
            // create the typed datasource class declaration and add it to the namespace
            CodeTypeDeclaration dataSourceClass = CreateDataSourceDeclaration(dtDataSource); 
            mainNamespace.Types.Add(dataSourceClass); 

            bool supportsMultipleNamespaces = CodeGenHelper.SupportsMultipleNamespaces(this.codeProvider); 
            CodeNamespace adaptersNamespace = null;
            if (!GenerateSingleNamespace && supportsMultipleNamespaces) {
                string adaptersNsName = this.CreateAdaptersNamespace(dtDataSource.GeneratorDataSetName);
                if (!StringUtil.Empty(mainNamespace.Name)) { 
                    adaptersNsName = mainNamespace.Name + "." + adaptersNsName;
                } 
 
                adaptersNamespace = new CodeNamespace(adaptersNsName);
            } 

            DataComponentGenerator componentGenerator = new DataComponentGenerator(this);
            bool hasAnyTableAdapter = false;
            foreach(DesignTable table in dtDataSource.DesignTables) { 
                if(table.TableType != TableType.RadTable) {
                    continue; 
                } 
                hasAnyTableAdapter = true;
 
                table.PropertyCache = new DesignTable.CodeGenPropertyCache(table);

                CodeTypeDeclaration componentType = componentGenerator.GenerateDataComponent(table, false, generateHierarchicalUpdate);
 
                if (GenerateSingleNamespace) {
                    mainNamespace.Types.Add(componentType); 
                } 
                else {
                    if (supportsMultipleNamespaces) { 
                        adaptersNamespace.Types.Add(componentType);
                    }
                    else {
                        componentType.Name = dataSourceClass.Name + componentType.Name; 
                        mainNamespace.Types.Add(componentType);
                    } 
                } 
            }
 
            generateHierarchicalUpdate = generateHierarchicalUpdate && hasAnyTableAdapter;

            if(dtDataSource.Sources != null && dtDataSource.Sources.Count > 0) {
                // create a 'fake' table and set names and sources on it 
                DesignTable functionsTable = new DesignTable();
                functionsTable.TableType = TableType.RadTable; 
                functionsTable.MainSource = null; 
                functionsTable.GeneratorDataComponentClassName = dtDataSource.GeneratorFunctionsComponentClassName;
 
                foreach(Source source in dtDataSource.Sources) {
                    functionsTable.Sources.Add(source);
                }
 
                CodeTypeDeclaration componentType = componentGenerator.GenerateDataComponent(functionsTable, true, generateHierarchicalUpdate);
 
                // generate the FunctionsDataComponent out of the fake table 
                if (GenerateSingleNamespace) {
                    mainNamespace.Types.Add(componentType); 
                }
                else {
                    if (supportsMultipleNamespaces) {
                        adaptersNamespace.Types.Add(componentType); 
                    }
                    else { 
                        componentType.Name = dataSourceClass.Name + componentType.Name; 
                        mainNamespace.Types.Add(componentType);
                    } 
                }
            }

            if (adaptersNamespace != null && adaptersNamespace.Types.Count > 0) { 
                codeCompileUnit.Namespaces.Add(adaptersNamespace);
            } 
 
            if (generateHierarchicalUpdate) {
                TableAdapterManagerGenerator adapterManagerGenerator = new TableAdapterManagerGenerator(this); 
                CodeTypeDeclaration adapterManagerType = adapterManagerGenerator.GenerateAdapterManager(designDataSource,dataSourceClass);

                if (GenerateSingleNamespace) {
                    mainNamespace.Types.Add(adapterManagerType); 
                }
                else { 
                    if (supportsMultipleNamespaces) { 
                        adaptersNamespace.Types.Add(adapterManagerType);
                    } 
                    else {
                        adapterManagerType.Name = dataSourceClass.Name + adapterManagerType.Name;
                        mainNamespace.Types.Add(adapterManagerType);
                    } 
                }
            } 
        } 

        private void AddUserData(CodeCompileUnit compileUnit) { 
            if (this.UserData != null) {
                foreach (object key in this.UserData.Keys) {
                    compileUnit.UserData.Add(key, userData[key]);
                } 
            }
        } 
 
        private CodeTypeDeclaration CreateDataSourceDeclaration(DesignDataSource dtDataSource) {
            // Let's check if we have a DataSource name 
            if( dtDataSource.Name == null ) {
                //
                throw new DataSourceGeneratorException("DataSource name cannot be null.");
            } 

            // Ensure the generator names are set in the DesignDataSource 
            NameHandler nameHandler = new NameHandler(this.codeProvider); 
            nameHandler.GenerateMemberNames(dtDataSource, this.problemList);
 
            // Create CodeTypeDeclaration for DataSource class
            CodeTypeDeclaration dataSourceClass = CodeGenHelper.Class(dtDataSource.GeneratorDataSetName, true, dtDataSource.Modifier);

            // Set BaseType 
            dataSourceClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(System.Data.DataSet)));
 
            // Set Attributes 
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.Serializable"));
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerCategoryAttribute", CodeGenHelper.Str("code"))); 
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.ToolboxItem", CodeGenHelper.Primitive(true)));
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(XmlSchemaProviderAttribute).FullName, CodeGenHelper.Primitive("GetTypedDataSetSchema")));
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(XmlRootAttribute).FullName, CodeGenHelper.Primitive(dtDataSource.GeneratorDataSetName)));
            dataSourceClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.Design.HelpKeywordAttribute).FullName, CodeGenHelper.Str("vs.data.DataSet"))); 

            dataSourceClass.Comments.Add(CodeGenHelper.Comment("Represents a strongly typed in-memory cache of data.", true)); 
 

            // Create and Init the Table Handler 
            tableHandler = new TypedTableHandler(this, dtDataSource.DesignTables);
            // Create and Init the Relation Handler
            relationHandler = new RelationHandler(this, dtDataSource.DesignRelations);
            // Create the Typed Row Handler 
            rowHandler = new TypedRowHandler(this, dtDataSource.DesignTables);
            // Create and Init the Typed DataSet Method Generator 
            DatasetMethodGenerator dsMethodGenerator = new DatasetMethodGenerator(this, dtDataSource); 

 
            // Generate 1 private variable for each typed table
            tableHandler.AddPrivateVars(dataSourceClass);
            // Generate 1 public property for each typed table
            tableHandler.AddTableProperties(dataSourceClass); 

            // Generate 1 private variable for each relation 
            relationHandler.AddPrivateVars(dataSourceClass); 

            // Generate typed dataset methods 
            dsMethodGenerator.AddMethods(dataSourceClass);

            // Generate typed row event handlers
            rowHandler.AddTypedRowEventHandlers(dataSourceClass); 

            // Generate typed table classes 
            tableHandler.AddTableClasses(dataSourceClass); 

            // Generate typed row classes 
            rowHandler.AddTypedRows(dataSourceClass);

            // Generate typed row event args
            rowHandler.AddTypedRowEventArgs(dataSourceClass); 

#if _WHACK_ // twhitney: we don't use default instances anymore 
#endif 

            return dataSourceClass; 
        }

        internal static ArrayList GetProviderAssemblies(DesignDataSource designDS) {
            Debug.Assert(designDS != null); 

            ArrayList providerAssemblies = new ArrayList(); 
 
            foreach (IDesignConnection connection in designDS.DesignConnections) {
                IDbConnection dbConnection = connection.CreateEmptyDbConnection(); 
                Debug.Assert(dbConnection != null);

                if (dbConnection != null) {
                    Assembly providerAssembly = dbConnection.GetType().Assembly; 

                    if (!providerAssemblies.Contains(providerAssembly)) { 
                        providerAssemblies.Add(providerAssembly); 
                    }
                } 
            }

            return providerAssemblies;
        } 

        private string CreateAdaptersNamespace(string generatorDataSetName) { 
            if (generatorDataSetName.StartsWith("[", StringComparison.Ordinal) && generatorDataSetName.EndsWith("]", StringComparison.Ordinal)) { 
                // special case for VB: the class name can be enclosed in brackets, but if we concatenate it to
                // create the namespace name it's not valid any more (see VSWhidbey #361647). 
                generatorDataSetName = generatorDataSetName.Substring(1, generatorDataSetName.Length - 2);
            }

            return MemberNameValidator.GenerateIdName(generatorDataSetName + "TableAdapters", this.CodeProvider, false/*useSuffix*/); 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
