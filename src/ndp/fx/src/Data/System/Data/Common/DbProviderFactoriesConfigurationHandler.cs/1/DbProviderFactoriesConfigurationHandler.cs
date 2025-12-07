//------------------------------------------------------------------------------ 
// <copyright file="DbProviderFactoriesConfigurationHandler.cs" company="Microsoft">
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

    // <configSections> 
    //     <section name="system.data" type="System.Data.Common.DbProviderFactoriesConfigurationHandler, System.Data, Version=%ASSEMBLY_VERSION%, Culture=neutral, PublicKeyToken=%ECMA_PUBLICKEY%" />
    // </configSections>
    // <system.data>
    //     <DbProviderFactories> 
    //         <add name="Odbc Data Provider"         invariant="System.Data.Odbc"         support="1BF" description=".Net Framework Data Provider for Odbc"      type="System.Data.Odbc.OdbcFactory, System.Data, Version=%ASSEMBLY_VERSION%, Culture=neutral, PublicKeyToken=%ECMA_PUBLICKEY%"/>
    //         <add name="OleDb Data Provider"        invariant="System.Data.OleDb"        support="1BF" description=".Net Framework Data Provider for OleDb"     type="System.Data.OleDb.OleDbFactory, System.Data, Version=%ASSEMBLY_VERSION%, Culture=neutral, PublicKeyToken=%ECMA_PUBLICKEY%"/> 
    //         <add name="OracleClient Data Provider" invariant="System.Data.OracleClient" support="1AF" description=".Net Framework Data Provider for Oracle"    type="System.Data.OracleClient.OracleFactory, System.Data.OracleClient, Version=%ASSEMBLY_VERSION%, Culture=neutral, PublicKeyToken=%ECMA_PUBLICKEY%"/> 
    //         <add name="SqlClient Data Provider"    invariant="System.Data.SqlClient"    support="1FF" description=".Net Framework Data Provider for SqlServer" type="System.Data.SqlClient.SqlClientFactory, System.Data, Version=%ASSEMBLY_VERSION%, Culture=neutral, PublicKeyToken=%ECMA_PUBLICKEY%"/>
    //     </DbProviderFactories> 
    // </system.data>
    // this class is delayed created, use ConfigurationSettings.GetSection("system.data") to obtain
#if WINFSInternalOnly
    internal 
#else
    public 
#endif 
    class DbProviderFactoriesConfigurationHandler : IConfigurationSectionHandler { // V1.2.3300
        internal const string sectionName = "system.data"; 
        internal const string providerGroup = "DbProviderFactories";

        public DbProviderFactoriesConfigurationHandler() { // V1.2.3300
        } 

        virtual public object Create(object parent, object configContext, XmlNode section) { // V1.2.3300 
#if DEBUG 
            try {
#endif 
                return CreateStatic(parent, configContext, section);
#if DEBUG
            }
            catch(Exception e) { 
                ADP.TraceExceptionWithoutRethrow(e); // it will be rethrown
                throw; 
            } 
#endif
        } 

        static internal object CreateStatic(object parent, object configContext, XmlNode section) {
            object config = parent;
            if (null != section) { 
                config = HandlerBase.CloneParent(parent as DataSet, false);
                bool foundFactories = false; 
 
                HandlerBase.CheckForUnrecognizedAttributes(section);
                foreach (XmlNode child in section.ChildNodes) { 
                    if (HandlerBase.IsIgnorableAlsoCheckForNonElement(child)) {
                        continue;
                    }
                    string sectionGroup = child.Name; 
                    switch(sectionGroup) {
                    case DbProviderFactoriesConfigurationHandler.providerGroup: 
                        if (foundFactories) { 
                            throw ADP.ConfigSectionsUnique(DbProviderFactoriesConfigurationHandler.providerGroup);
                        } 
                        foundFactories = true;
                        HandleProviders(config as DataSet, configContext, child, sectionGroup);
                        break;
                    default: 
                        throw ADP.ConfigUnrecognizedElement(child);
                    } 
                } 
            }
            return config; 
        }

        // sectionName - i.e. "providerconfiguration"
        private static void HandleProviders(DataSet config, object configContext, XmlNode section, string sectionName) { 
            DataTableCollection tables = config.Tables;
            DataTable dataTable = tables[sectionName]; 
            bool tableExisted = (null != dataTable); 
            dataTable = DbProviderDictionarySectionHandler.CreateStatic(dataTable, configContext, section);
            if (!tableExisted) { 
                tables.Add(dataTable);
            }
        }
 
        // based off of DictionarySectionHandler
        private static class DbProviderDictionarySectionHandler/* : IConfigurationSectionHandler*/ { 
            /* 
            internal DbProviderDictionarySectionHandler() {
            } 

            public object Create(Object parent, Object context, XmlNode section) {
                return CreateStatic(parent, context, section);
            } 
            */
 
            static internal DataTable CreateStatic(DataTable config, Object context, XmlNode section) { 
                if (null != section) {
                    HandlerBase.CheckForUnrecognizedAttributes(section); 

                    if (null == config) {
                        config = DbProviderFactoriesConfigurationHandler.CreateProviderDataTable();
                    } 
                    // else already copied via DataSet.Copy
 
                    foreach (XmlNode child in section.ChildNodes) { 
                        if (HandlerBase.IsIgnorableAlsoCheckForNonElement(child)) {
                            continue; 
                        }
                        switch(child.Name) {
                        case "add":
                            HandleAdd(child, config); 
                            break;
                        case "remove": 
                            HandleRemove(child, config); 
                            break;
                        case "clear": 
                            HandleClear(child, config);
                            break;
                        default:
                            throw ADP.ConfigUnrecognizedElement(child); 
                        }
                    } 
                    config.AcceptChanges(); 
                }
                return config; 
            }
            static private void HandleAdd(XmlNode child, DataTable config) {
                HandlerBase.CheckForChildNodes(child);
                DataRow values = config.NewRow(); 
                values[0] = HandlerBase.RemoveAttribute(child, "name", true, false);
                values[1] = HandlerBase.RemoveAttribute(child, "description", true, false); 
                values[2] = HandlerBase.RemoveAttribute(child, "invariant", true, false); 
                values[3] = HandlerBase.RemoveAttribute(child, "type", true, false);
 
                // because beta shipped recognizing "support=hex#", need to give
                // more time for other providers to remove it from the .config files
                HandlerBase.RemoveAttribute(child, "support", false, false);
 
                HandlerBase.CheckForUnrecognizedAttributes(child);
                config.Rows.Add(values); 
            } 
            static private void HandleRemove(XmlNode child, DataTable config) {
                HandlerBase.CheckForChildNodes(child); 
                String invr = HandlerBase.RemoveAttribute(child, "invariant", true, false);
                HandlerBase.CheckForUnrecognizedAttributes(child);
                DataRow row = config.Rows.Find(invr);
                if (null != row) { // ignore invariants that don't exist 
                    row.Delete();
                } 
            } 
            static private void HandleClear(XmlNode child, DataTable config) {
                HandlerBase.CheckForChildNodes(child); 
                HandlerBase.CheckForUnrecognizedAttributes(child);
                config.Clear();
            }
        } 

        internal static DataTable CreateProviderDataTable() { 
            DataColumn frme = new DataColumn("Name", typeof(string)); 
            frme.ReadOnly = true;
            DataColumn desc = new DataColumn("Description", typeof(string)); 
            desc.ReadOnly = true;
            DataColumn invr = new DataColumn("InvariantName", typeof(string));
            invr.ReadOnly = true;
            DataColumn qual = new DataColumn("AssemblyQualifiedName", typeof(string)); 
            qual.ReadOnly = true;
 
            DataColumn[] primaryKey = new DataColumn[] { invr }; 
            DataColumn[] columns = new DataColumn[] {frme, desc, invr, qual };
            DataTable table = new DataTable(DbProviderFactoriesConfigurationHandler.providerGroup); 
            table.Locale = CultureInfo.InvariantCulture;
            table.Columns.AddRange(columns);
            table.PrimaryKey = primaryKey;
            return table; 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbProviderFactoriesConfigurationHandler.cs" company="Microsoft">
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

    // <configSections> 
    //     <section name="system.data" type="System.Data.Common.DbProviderFactoriesConfigurationHandler, System.Data, Version=%ASSEMBLY_VERSION%, Culture=neutral, PublicKeyToken=%ECMA_PUBLICKEY%" />
    // </configSections>
    // <system.data>
    //     <DbProviderFactories> 
    //         <add name="Odbc Data Provider"         invariant="System.Data.Odbc"         support="1BF" description=".Net Framework Data Provider for Odbc"      type="System.Data.Odbc.OdbcFactory, System.Data, Version=%ASSEMBLY_VERSION%, Culture=neutral, PublicKeyToken=%ECMA_PUBLICKEY%"/>
    //         <add name="OleDb Data Provider"        invariant="System.Data.OleDb"        support="1BF" description=".Net Framework Data Provider for OleDb"     type="System.Data.OleDb.OleDbFactory, System.Data, Version=%ASSEMBLY_VERSION%, Culture=neutral, PublicKeyToken=%ECMA_PUBLICKEY%"/> 
    //         <add name="OracleClient Data Provider" invariant="System.Data.OracleClient" support="1AF" description=".Net Framework Data Provider for Oracle"    type="System.Data.OracleClient.OracleFactory, System.Data.OracleClient, Version=%ASSEMBLY_VERSION%, Culture=neutral, PublicKeyToken=%ECMA_PUBLICKEY%"/> 
    //         <add name="SqlClient Data Provider"    invariant="System.Data.SqlClient"    support="1FF" description=".Net Framework Data Provider for SqlServer" type="System.Data.SqlClient.SqlClientFactory, System.Data, Version=%ASSEMBLY_VERSION%, Culture=neutral, PublicKeyToken=%ECMA_PUBLICKEY%"/>
    //     </DbProviderFactories> 
    // </system.data>
    // this class is delayed created, use ConfigurationSettings.GetSection("system.data") to obtain
#if WINFSInternalOnly
    internal 
#else
    public 
#endif 
    class DbProviderFactoriesConfigurationHandler : IConfigurationSectionHandler { // V1.2.3300
        internal const string sectionName = "system.data"; 
        internal const string providerGroup = "DbProviderFactories";

        public DbProviderFactoriesConfigurationHandler() { // V1.2.3300
        } 

        virtual public object Create(object parent, object configContext, XmlNode section) { // V1.2.3300 
#if DEBUG 
            try {
#endif 
                return CreateStatic(parent, configContext, section);
#if DEBUG
            }
            catch(Exception e) { 
                ADP.TraceExceptionWithoutRethrow(e); // it will be rethrown
                throw; 
            } 
#endif
        } 

        static internal object CreateStatic(object parent, object configContext, XmlNode section) {
            object config = parent;
            if (null != section) { 
                config = HandlerBase.CloneParent(parent as DataSet, false);
                bool foundFactories = false; 
 
                HandlerBase.CheckForUnrecognizedAttributes(section);
                foreach (XmlNode child in section.ChildNodes) { 
                    if (HandlerBase.IsIgnorableAlsoCheckForNonElement(child)) {
                        continue;
                    }
                    string sectionGroup = child.Name; 
                    switch(sectionGroup) {
                    case DbProviderFactoriesConfigurationHandler.providerGroup: 
                        if (foundFactories) { 
                            throw ADP.ConfigSectionsUnique(DbProviderFactoriesConfigurationHandler.providerGroup);
                        } 
                        foundFactories = true;
                        HandleProviders(config as DataSet, configContext, child, sectionGroup);
                        break;
                    default: 
                        throw ADP.ConfigUnrecognizedElement(child);
                    } 
                } 
            }
            return config; 
        }

        // sectionName - i.e. "providerconfiguration"
        private static void HandleProviders(DataSet config, object configContext, XmlNode section, string sectionName) { 
            DataTableCollection tables = config.Tables;
            DataTable dataTable = tables[sectionName]; 
            bool tableExisted = (null != dataTable); 
            dataTable = DbProviderDictionarySectionHandler.CreateStatic(dataTable, configContext, section);
            if (!tableExisted) { 
                tables.Add(dataTable);
            }
        }
 
        // based off of DictionarySectionHandler
        private static class DbProviderDictionarySectionHandler/* : IConfigurationSectionHandler*/ { 
            /* 
            internal DbProviderDictionarySectionHandler() {
            } 

            public object Create(Object parent, Object context, XmlNode section) {
                return CreateStatic(parent, context, section);
            } 
            */
 
            static internal DataTable CreateStatic(DataTable config, Object context, XmlNode section) { 
                if (null != section) {
                    HandlerBase.CheckForUnrecognizedAttributes(section); 

                    if (null == config) {
                        config = DbProviderFactoriesConfigurationHandler.CreateProviderDataTable();
                    } 
                    // else already copied via DataSet.Copy
 
                    foreach (XmlNode child in section.ChildNodes) { 
                        if (HandlerBase.IsIgnorableAlsoCheckForNonElement(child)) {
                            continue; 
                        }
                        switch(child.Name) {
                        case "add":
                            HandleAdd(child, config); 
                            break;
                        case "remove": 
                            HandleRemove(child, config); 
                            break;
                        case "clear": 
                            HandleClear(child, config);
                            break;
                        default:
                            throw ADP.ConfigUnrecognizedElement(child); 
                        }
                    } 
                    config.AcceptChanges(); 
                }
                return config; 
            }
            static private void HandleAdd(XmlNode child, DataTable config) {
                HandlerBase.CheckForChildNodes(child);
                DataRow values = config.NewRow(); 
                values[0] = HandlerBase.RemoveAttribute(child, "name", true, false);
                values[1] = HandlerBase.RemoveAttribute(child, "description", true, false); 
                values[2] = HandlerBase.RemoveAttribute(child, "invariant", true, false); 
                values[3] = HandlerBase.RemoveAttribute(child, "type", true, false);
 
                // because beta shipped recognizing "support=hex#", need to give
                // more time for other providers to remove it from the .config files
                HandlerBase.RemoveAttribute(child, "support", false, false);
 
                HandlerBase.CheckForUnrecognizedAttributes(child);
                config.Rows.Add(values); 
            } 
            static private void HandleRemove(XmlNode child, DataTable config) {
                HandlerBase.CheckForChildNodes(child); 
                String invr = HandlerBase.RemoveAttribute(child, "invariant", true, false);
                HandlerBase.CheckForUnrecognizedAttributes(child);
                DataRow row = config.Rows.Find(invr);
                if (null != row) { // ignore invariants that don't exist 
                    row.Delete();
                } 
            } 
            static private void HandleClear(XmlNode child, DataTable config) {
                HandlerBase.CheckForChildNodes(child); 
                HandlerBase.CheckForUnrecognizedAttributes(child);
                config.Clear();
            }
        } 

        internal static DataTable CreateProviderDataTable() { 
            DataColumn frme = new DataColumn("Name", typeof(string)); 
            frme.ReadOnly = true;
            DataColumn desc = new DataColumn("Description", typeof(string)); 
            desc.ReadOnly = true;
            DataColumn invr = new DataColumn("InvariantName", typeof(string));
            invr.ReadOnly = true;
            DataColumn qual = new DataColumn("AssemblyQualifiedName", typeof(string)); 
            qual.ReadOnly = true;
 
            DataColumn[] primaryKey = new DataColumn[] { invr }; 
            DataColumn[] columns = new DataColumn[] {frme, desc, invr, qual };
            DataTable table = new DataTable(DbProviderFactoriesConfigurationHandler.providerGroup); 
            table.Locale = CultureInfo.InvariantCulture;
            table.Columns.AddRange(columns);
            table.PrimaryKey = primaryKey;
            return table; 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
