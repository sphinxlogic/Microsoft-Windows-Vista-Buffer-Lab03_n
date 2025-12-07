//------------------------------------------------------------------------------ 
// <copyright file="DbProviderFactories.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    using System;
    using System.Collections;
    using System.Configuration;
    using System.Data; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Xml; 

#if WINFSInternalOnly 
    internal
#else
    public
#endif 
    static class DbProviderFactories {
 
        private const string AssemblyQualifiedName = "AssemblyQualifiedName"; 
        private const string Instance = "Instance";
        private const string InvariantName = "InvariantName"; 

        private static ConnectionState _initState; // closed, connecting, open
        private static DataSet _configTable;
        private static object _lockobj = new object(); 

        static public DbProviderFactory GetFactory(string providerInvariantName) { 
            ADP.CheckArgumentLength(providerInvariantName, "providerInvariantName"); 

            DataSet configTable = GetConfigTable(); 
            DataTable providerTable = ((null != configTable) ? configTable.Tables[DbProviderFactoriesConfigurationHandler.providerGroup] : null);
            if (null != providerTable) {
                // we don't need to copy the DataTable because its used in a controlled fashion
                // also we don't need to create a blank datatable because we know the information won't exist 

#if DEBUG 
                DataColumn[] pkey = providerTable.PrimaryKey; 
                Debug.Assert(null != providerTable.Columns[InvariantName], "missing primary key column");
                Debug.Assert((null != pkey) && (1 == pkey.Length) && (InvariantName == pkey[0].ColumnName), "bad primary key"); 
#endif
                DataRow providerRow = providerTable.Rows.Find(providerInvariantName);
                if (null != providerRow) {
                    return DbProviderFactories.GetFactory(providerRow); 
                }
            } 
            throw ADP.ConfigProviderNotFound(); 
        }
 
        static public DbProviderFactory GetFactory(DataRow providerRow) {
            ADP.CheckArgumentNull(providerRow, "providerRow");

            // fail with ConfigProviderMissing rather than ColumnNotInTheTable exception 
            DataColumn column = providerRow.Table.Columns[AssemblyQualifiedName];
            if (null != column) { 
                // column value may not be a string 
                string assemblyQualifiedName = providerRow[column] as string;
                if (!ADP.IsEmpty(assemblyQualifiedName)) { 

    // FXCop is concerned about the following line call to Get Type,
    // If this code is deemed safe during our security review we should add this warning to our exclusion list.
    // FXCop Message, pertaining to the call to GetType. 
    //
    // Secure late-binding methods,System.Data.dll!System.Data.Common.DbProviderFactories.GetFactory(System.Data.DataRow):System.Data.Common.DbProviderFactory, 
                    Type providerType = Type.GetType(assemblyQualifiedName); 
                    if (null != providerType) {
 
                        System.Reflection.FieldInfo providerInstance = providerType.GetField(Instance, System.Reflection.BindingFlags.DeclaredOnly|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
                        if (null != providerInstance) {
                            Debug.Assert(providerInstance.IsPublic, "field not public");
                            Debug.Assert(providerInstance.IsStatic, "field not static"); 

                             if (providerInstance.FieldType.IsSubclassOf(typeof(DbProviderFactory))) { 
 
                                object factory = providerInstance.GetValue(null);
                                if (null != factory) { 
                                    return (DbProviderFactory)factory;
                                }
                                // else throw ConfigProviderInvalid
                            } 
                            // else throw ConfigProviderInvalid
                        } 
                        throw ADP.ConfigProviderInvalid(); 
                    }
                    throw ADP.ConfigProviderNotInstalled(); 
                }
                // else throw ConfigProviderMissing
            }
            throw ADP.ConfigProviderMissing(); 
        }
 
        static public DataTable GetFactoryClasses() { // V1.2.3300 
            DataSet configTable = GetConfigTable();
            DataTable dataTable = ((null != configTable) ? configTable.Tables[DbProviderFactoriesConfigurationHandler.providerGroup] : null); 
            if (null != dataTable) {
                dataTable = dataTable.Copy();
            }
            else { 
                dataTable = DbProviderFactoriesConfigurationHandler.CreateProviderDataTable();
            } 
            return dataTable; 
        }
 
        static private DataSet GetConfigTable() {
            Initialize();
            return _configTable;
        } 

        static private void Initialize() { 
            if (ConnectionState.Open != _initState) { 
                lock (_lockobj) {
                    switch(_initState) { 
                    case ConnectionState.Closed:
                        _initState = ConnectionState.Connecting; // used for preventing recursion
                        try {
                            _configTable = PrivilegedConfigurationManager.GetSection(DbProviderFactoriesConfigurationHandler.sectionName) as DataSet; 
                        }
                        finally { 
                            _initState = ConnectionState.Open; 
                        }
                        break; 
                    case ConnectionState.Connecting:
                    case ConnectionState.Open:
                        break;
                    default: 
                        Debug.Assert(false, "unexpected state");
                        break; 
                    } 
                }
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbProviderFactories.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    using System;
    using System.Collections;
    using System.Configuration;
    using System.Data; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Xml; 

#if WINFSInternalOnly 
    internal
#else
    public
#endif 
    static class DbProviderFactories {
 
        private const string AssemblyQualifiedName = "AssemblyQualifiedName"; 
        private const string Instance = "Instance";
        private const string InvariantName = "InvariantName"; 

        private static ConnectionState _initState; // closed, connecting, open
        private static DataSet _configTable;
        private static object _lockobj = new object(); 

        static public DbProviderFactory GetFactory(string providerInvariantName) { 
            ADP.CheckArgumentLength(providerInvariantName, "providerInvariantName"); 

            DataSet configTable = GetConfigTable(); 
            DataTable providerTable = ((null != configTable) ? configTable.Tables[DbProviderFactoriesConfigurationHandler.providerGroup] : null);
            if (null != providerTable) {
                // we don't need to copy the DataTable because its used in a controlled fashion
                // also we don't need to create a blank datatable because we know the information won't exist 

#if DEBUG 
                DataColumn[] pkey = providerTable.PrimaryKey; 
                Debug.Assert(null != providerTable.Columns[InvariantName], "missing primary key column");
                Debug.Assert((null != pkey) && (1 == pkey.Length) && (InvariantName == pkey[0].ColumnName), "bad primary key"); 
#endif
                DataRow providerRow = providerTable.Rows.Find(providerInvariantName);
                if (null != providerRow) {
                    return DbProviderFactories.GetFactory(providerRow); 
                }
            } 
            throw ADP.ConfigProviderNotFound(); 
        }
 
        static public DbProviderFactory GetFactory(DataRow providerRow) {
            ADP.CheckArgumentNull(providerRow, "providerRow");

            // fail with ConfigProviderMissing rather than ColumnNotInTheTable exception 
            DataColumn column = providerRow.Table.Columns[AssemblyQualifiedName];
            if (null != column) { 
                // column value may not be a string 
                string assemblyQualifiedName = providerRow[column] as string;
                if (!ADP.IsEmpty(assemblyQualifiedName)) { 

    // FXCop is concerned about the following line call to Get Type,
    // If this code is deemed safe during our security review we should add this warning to our exclusion list.
    // FXCop Message, pertaining to the call to GetType. 
    //
    // Secure late-binding methods,System.Data.dll!System.Data.Common.DbProviderFactories.GetFactory(System.Data.DataRow):System.Data.Common.DbProviderFactory, 
                    Type providerType = Type.GetType(assemblyQualifiedName); 
                    if (null != providerType) {
 
                        System.Reflection.FieldInfo providerInstance = providerType.GetField(Instance, System.Reflection.BindingFlags.DeclaredOnly|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
                        if (null != providerInstance) {
                            Debug.Assert(providerInstance.IsPublic, "field not public");
                            Debug.Assert(providerInstance.IsStatic, "field not static"); 

                             if (providerInstance.FieldType.IsSubclassOf(typeof(DbProviderFactory))) { 
 
                                object factory = providerInstance.GetValue(null);
                                if (null != factory) { 
                                    return (DbProviderFactory)factory;
                                }
                                // else throw ConfigProviderInvalid
                            } 
                            // else throw ConfigProviderInvalid
                        } 
                        throw ADP.ConfigProviderInvalid(); 
                    }
                    throw ADP.ConfigProviderNotInstalled(); 
                }
                // else throw ConfigProviderMissing
            }
            throw ADP.ConfigProviderMissing(); 
        }
 
        static public DataTable GetFactoryClasses() { // V1.2.3300 
            DataSet configTable = GetConfigTable();
            DataTable dataTable = ((null != configTable) ? configTable.Tables[DbProviderFactoriesConfigurationHandler.providerGroup] : null); 
            if (null != dataTable) {
                dataTable = dataTable.Copy();
            }
            else { 
                dataTable = DbProviderFactoriesConfigurationHandler.CreateProviderDataTable();
            } 
            return dataTable; 
        }
 
        static private DataSet GetConfigTable() {
            Initialize();
            return _configTable;
        } 

        static private void Initialize() { 
            if (ConnectionState.Open != _initState) { 
                lock (_lockobj) {
                    switch(_initState) { 
                    case ConnectionState.Closed:
                        _initState = ConnectionState.Connecting; // used for preventing recursion
                        try {
                            _configTable = PrivilegedConfigurationManager.GetSection(DbProviderFactoriesConfigurationHandler.sectionName) as DataSet; 
                        }
                        finally { 
                            _initState = ConnectionState.Open; 
                        }
                        break; 
                    case ConnectionState.Connecting:
                    case ConnectionState.Open:
                        break;
                    default: 
                        Debug.Assert(false, "unexpected state");
                        break; 
                    } 
                }
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
