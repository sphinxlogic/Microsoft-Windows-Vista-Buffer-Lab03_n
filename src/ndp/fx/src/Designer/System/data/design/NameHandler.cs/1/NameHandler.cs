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
    using System.ComponentModel.Design; 
    using System.ComponentModel;

    internal sealed class NameHandler {
        private const string FunctionsTableName = "Queries"; 

        private DataSourceNameHandler dataSourceHandler = null; 
        private static CodeDomProvider codeProvider = null; 
        private bool languageCaseInsensitive = false;
 
        private static Hashtable lookupIdentifiers;

        internal NameHandler(CodeDomProvider codeProvider) {
            if(codeProvider == null) { 
                throw new ArgumentException("codeProvider");
            } 
 
            NameHandler.codeProvider = codeProvider;
        } 

        internal void GenerateMemberNames(DesignDataSource dataSource, ArrayList problemList) {
            if(dataSource == null || codeProvider == null) {
                throw new InternalException("DesignDataSource or/and CodeDomProvider parameters are null."); 
            }
 
            // init the lookup-identifiers hashtable; we need this for compatibility with WebData's generator 
            InitLookupIdentifiers();
 
            // generate names for DataSource-class members
            dataSourceHandler = new DataSourceNameHandler();
            dataSourceHandler.GenerateMemberNames(dataSource, codeProvider, this.languageCaseInsensitive, problemList);
 
            foreach (DesignTable table in dataSource.DesignTables) {
                // create table name handler 
                DataTableNameHandler currentTableHandler = new DataTableNameHandler(); 

                currentTableHandler.GenerateMemberNames(table, codeProvider, this.languageCaseInsensitive, problemList); 

                // create component name handler
                DataComponentNameHandler currentComponentHandler = new DataComponentNameHandler();
 
                currentComponentHandler.GenerateMemberNames(table, codeProvider, this.languageCaseInsensitive, problemList);
            } 
 
            // process names for the 'Functions' component
            if (dataSource.Sources != null && dataSource.Sources.Count > 0) { 
                // create a 'fake' table and set names and sources on it
                DesignTable functionsTable = new DesignTable();

                functionsTable.TableType = TableType.RadTable; 
                functionsTable.DataAccessorName = dataSource.FunctionsComponentName;
                functionsTable.UserDataComponentName = dataSource.UserFunctionsComponentName; 
                functionsTable.GeneratorDataComponentClassName = dataSource.GeneratorFunctionsComponentClassName; 

                foreach (Source source in dataSource.Sources) { 
                    functionsTable.Sources.Add(source);
                }

                // do the name generation for the fake table 
                DataComponentNameHandler functionsComponentHandler = new DataComponentNameHandler();
                functionsComponentHandler.GlobalSources = true; 
 
                functionsComponentHandler.GenerateMemberNames(functionsTable, codeProvider, this.languageCaseInsensitive, problemList);
 
                // copy the generated names back to the DesignDataSource, we'll use them when generating the FunctionsDataComponent
                dataSource.GeneratorFunctionsComponentClassName = functionsTable.GeneratorDataComponentClassName;
            }
        } 

        internal static string FixIdName(string inVarName) { 
            if (lookupIdentifiers == null) { 
                InitLookupIdentifiers();
            } 
            string newName = (string)lookupIdentifiers[inVarName];
            if (newName == null) {
                newName = MemberNameValidator.GenerateIdName(inVarName, codeProvider, false /*useSuffix*/);
                while (lookupIdentifiers.ContainsValue(newName)) { 
                    newName = '_' + newName;
                } 
                lookupIdentifiers[inVarName] = newName; 
            }
 
            return newName;
        }

        private static void InitLookupIdentifiers() { 
            lookupIdentifiers = new Hashtable();
 
            System.Reflection.PropertyInfo[] props = typeof(DataRow).GetProperties(); 
            foreach (System.Reflection.PropertyInfo p in props) {
                lookupIdentifiers[p.Name] = '_' + p.Name; 
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
    using System.Data;
    using System.CodeDom;
    using System.Collections; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Reflection; 
    using System.CodeDom.Compiler;
    using System.ComponentModel.Design; 
    using System.ComponentModel;

    internal sealed class NameHandler {
        private const string FunctionsTableName = "Queries"; 

        private DataSourceNameHandler dataSourceHandler = null; 
        private static CodeDomProvider codeProvider = null; 
        private bool languageCaseInsensitive = false;
 
        private static Hashtable lookupIdentifiers;

        internal NameHandler(CodeDomProvider codeProvider) {
            if(codeProvider == null) { 
                throw new ArgumentException("codeProvider");
            } 
 
            NameHandler.codeProvider = codeProvider;
        } 

        internal void GenerateMemberNames(DesignDataSource dataSource, ArrayList problemList) {
            if(dataSource == null || codeProvider == null) {
                throw new InternalException("DesignDataSource or/and CodeDomProvider parameters are null."); 
            }
 
            // init the lookup-identifiers hashtable; we need this for compatibility with WebData's generator 
            InitLookupIdentifiers();
 
            // generate names for DataSource-class members
            dataSourceHandler = new DataSourceNameHandler();
            dataSourceHandler.GenerateMemberNames(dataSource, codeProvider, this.languageCaseInsensitive, problemList);
 
            foreach (DesignTable table in dataSource.DesignTables) {
                // create table name handler 
                DataTableNameHandler currentTableHandler = new DataTableNameHandler(); 

                currentTableHandler.GenerateMemberNames(table, codeProvider, this.languageCaseInsensitive, problemList); 

                // create component name handler
                DataComponentNameHandler currentComponentHandler = new DataComponentNameHandler();
 
                currentComponentHandler.GenerateMemberNames(table, codeProvider, this.languageCaseInsensitive, problemList);
            } 
 
            // process names for the 'Functions' component
            if (dataSource.Sources != null && dataSource.Sources.Count > 0) { 
                // create a 'fake' table and set names and sources on it
                DesignTable functionsTable = new DesignTable();

                functionsTable.TableType = TableType.RadTable; 
                functionsTable.DataAccessorName = dataSource.FunctionsComponentName;
                functionsTable.UserDataComponentName = dataSource.UserFunctionsComponentName; 
                functionsTable.GeneratorDataComponentClassName = dataSource.GeneratorFunctionsComponentClassName; 

                foreach (Source source in dataSource.Sources) { 
                    functionsTable.Sources.Add(source);
                }

                // do the name generation for the fake table 
                DataComponentNameHandler functionsComponentHandler = new DataComponentNameHandler();
                functionsComponentHandler.GlobalSources = true; 
 
                functionsComponentHandler.GenerateMemberNames(functionsTable, codeProvider, this.languageCaseInsensitive, problemList);
 
                // copy the generated names back to the DesignDataSource, we'll use them when generating the FunctionsDataComponent
                dataSource.GeneratorFunctionsComponentClassName = functionsTable.GeneratorDataComponentClassName;
            }
        } 

        internal static string FixIdName(string inVarName) { 
            if (lookupIdentifiers == null) { 
                InitLookupIdentifiers();
            } 
            string newName = (string)lookupIdentifiers[inVarName];
            if (newName == null) {
                newName = MemberNameValidator.GenerateIdName(inVarName, codeProvider, false /*useSuffix*/);
                while (lookupIdentifiers.ContainsValue(newName)) { 
                    newName = '_' + newName;
                } 
                lookupIdentifiers[inVarName] = newName; 
            }
 
            return newName;
        }

        private static void InitLookupIdentifiers() { 
            lookupIdentifiers = new Hashtable();
 
            System.Reflection.PropertyInfo[] props = typeof(DataRow).GetProperties(); 
            foreach (System.Reflection.PropertyInfo p in props) {
                lookupIdentifiers[p.Name] = '_' + p.Name; 
            }
        }
    }
 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
