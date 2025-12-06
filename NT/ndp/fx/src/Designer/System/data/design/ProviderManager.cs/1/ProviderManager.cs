//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
using System; 
using System.Collections;
using System.Collections.Specialized; 
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection; 

 
namespace System.Data.Design { 

    internal sealed class ProviderManager { 
        private static DataTable factoryTable = null;
        private static CachedProviderData providerData = new CachedProviderData();

        internal static Hashtable CustomDBProviders = null; 
        internal static DbProviderFactory ActiveFactoryContext = null;
 
        // Column names for DbProviderFactories section in machine.config file. 
        private static readonly string PROVIDER_NAME = "Name";
        private static readonly string PROVIDER_INVARIANT_NAME = "InvariantName"; 
        private static readonly string PROVIDER_ASSEMBLY = "AssemblyQualifiedName";

        internal enum ProviderSupportedClasses {
            DbConnection = 0, 
            DbDataAdapter = 1,
            DbParameter = 2, 
            DbCommand = 3, 
            DbCommandBuilder = 4,
            DbDataSourceEnumerator = 5, 
            CodeAccessPermission = 6,
            DbConnectionStringBuilder = 7,
        }
 

        private class CachedProviderData { 
            public DbProviderFactory CachedFactory = null; 
            public Type CachedType = null;
            public string CachedInvariantProviderName = string.Empty; 
            public string CachedDisplayName = string.Empty;
            private PropertyInfo providerTypeProperty = null;
            private bool useCachedPropertyValue = false;
 
            public PropertyInfo ProviderTypeProperty {
                get { return this.providerTypeProperty; } 
                set { this.providerTypeProperty = value; } 
            }
 
            public bool UseCachedPropertyValue {
                get { return this.useCachedPropertyValue; }
                set { this.useCachedPropertyValue= value; }
            } 

 
            public bool Matches( Type type ) { 
                if( (this.CachedFactory != null) && (this.CachedType != null) && (this.CachedType.Equals(type)) ) {
                    return true; 
                }
                else return false;
            }
 

            public bool Matches( string invariantName ) { 
                if( (this.CachedFactory != null) && (this.CachedInvariantProviderName != null) 
                && StringUtil.EqualValue(this.CachedInvariantProviderName, invariantName) ) {
                    return true; 
                }
                else return false;
            }
 

            public bool Matches( DbProviderFactory factory ) { 
                if( (this.CachedFactory != null) && this.CachedFactory.GetType().Equals(factory.GetType()) ) { 
                    return true;
                } 
                else return false;
            }

 
            public void Initialize( DbProviderFactory factory, string invariantProviderName, string displayName ) {
                this.CachedFactory = factory; 
                this.CachedInvariantProviderName = invariantProviderName; 
                this.CachedType = null;
                this.CachedDisplayName = displayName; 
                this.ProviderTypeProperty = null;
                this.UseCachedPropertyValue = false;
            }
 
            public void Initialize( DbProviderFactory factory, string invariantProviderName, string displayName, Type type ) {
                Initialize( factory, invariantProviderName, displayName ); 
                this.CachedType = type; 
            }
        } 



        public static DbProviderFactory GetFactoryFromType(Type type, ProviderSupportedClasses kindOfObject) { 
            if(type == null) {
                throw new ArgumentNullException("type"); 
            } 

            if( ProviderManager.providerData.Matches(type) ) { 
                return ProviderManager.providerData.CachedFactory;
            }

            EnsureFactoryTable(); 

            foreach(DataRow row in ProviderManager.factoryTable.Rows) { 
                DbProviderFactory factory = DbProviderFactories.GetFactory(row); 

                string providerName = (string) row[PROVIDER_NAME]; 
                object o = CreateObject( factory, kindOfObject, providerName );

                if( type.Equals(o.GetType()) ) {
                    ProviderManager.providerData.Initialize( factory, (string) row[PROVIDER_INVARIANT_NAME], 
                                                             (string) row[PROVIDER_NAME], type );
 
                    return factory; 
                }
            } 

            throw new InternalException( String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unable to find DbProviderFactory for type {0}", type.ToString()) );
        }
 

 
        public static string GetInvariantProviderName( DbProviderFactory factory ) { 
            if( factory == null ) {
                throw new ArgumentNullException("factory"); 
            }

            if( ProviderManager.providerData.Matches(factory) ) {
                return ProviderManager.providerData.CachedInvariantProviderName; 
            }
 
            EnsureFactoryTable(); 
            string factoryTypeName = factory.GetType().AssemblyQualifiedName;
 
            foreach(DataRow row in ProviderManager.factoryTable.Rows) {
                if( StringUtil.EqualValue( (string) row[PROVIDER_ASSEMBLY], factoryTypeName ) ) {
                    ProviderManager.providerData.Initialize( factory, (string) row[PROVIDER_INVARIANT_NAME], (string) row[PROVIDER_NAME] );
                    return ProviderManager.providerData.CachedInvariantProviderName; 
                }
            } 
 
            throw new InternalException( String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unable to get invariant name from factory. Factory type is {0}",
                                                       factory.GetType().ToString()) ); 
        }


        // DbProviderFactories expose a method that takes invariantName and returns a factory, 
        // but it is slow, so we want to wrap it and speed things up by a little caching.
        public static DbProviderFactory GetFactory( string invariantName ) { 
            if( StringUtil.EmptyOrSpace(invariantName) ) { 
                throw new ArgumentNullException("invariantName");
            } 

            // [Obsoleted] - Custom DBProviderFactory specified when TypedDataSetGenerator.Generate is
            //               invoked with ActiveFactoryContext. The ActiveFactoryContext is always used
            //               regardless of the invariantName specified. 
            //
            if ( ActiveFactoryContext != null ) { 
                ProviderManager.providerData.Initialize( ActiveFactoryContext, invariantName, invariantName ); 
                return ActiveFactoryContext;				
            } 

            // Custom DBProviderFactories specified when TypedDataSetGenerator.Generate is invoked with
            // a Hashtable of CustomFactories, indexed by InvariantName.
            // 
            if ( CustomDBProviders != null && CustomDBProviders.ContainsKey(invariantName) ) {
                DbProviderFactory customFactory = CustomDBProviders[invariantName] as DbProviderFactory; 
                if (customFactory != null) { 
                    ProviderManager.providerData.Initialize( customFactory, invariantName, invariantName );
                    return customFactory; 
                }
            }

            if( ProviderManager.providerData.Matches(invariantName) ) { 
                return ProviderManager.providerData.CachedFactory;
            } 
 
            EnsureFactoryTable();
 
            DataRow[] rows = ProviderManager.factoryTable.Select( string.Format(System.Globalization.CultureInfo.CurrentCulture, "InvariantName = '{0}'", invariantName) );
            if( rows.Length == 0 ) {
                throw new InternalException( string.Format(System.Globalization.CultureInfo.CurrentCulture, "Cannot find provider factory for provider named {0}", invariantName) );
            } 
            if( rows.Length > 1 ) {
                throw new InternalException( string.Format(System.Globalization.CultureInfo.CurrentCulture, "More that one data row for provider named {0}", invariantName) ); 
            } 

            DbProviderFactory factory = DbProviderFactories.GetFactory(rows[0]); 
            ProviderManager.providerData.Initialize( factory, invariantName, (string) rows[0][PROVIDER_NAME] );
            return factory;
        }
 
        public static PropertyInfo GetProviderTypeProperty(DbProviderFactory factory) {
            if(factory == null) { 
                throw new ArgumentNullException("factory should not be null."); 
            }
 
            if (ProviderManager.providerData.UseCachedPropertyValue) {
                return ProviderManager.providerData.ProviderTypeProperty;
            }
 
            ProviderManager.providerData.UseCachedPropertyValue = true;
            DbParameter parameter = factory.CreateParameter(); 
 
            PropertyInfo[] properties = parameter.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo pi in properties) { 
                if (!pi.PropertyType.IsEnum) { continue; }

                object[] attributes = pi.GetCustomAttributes(typeof(DbProviderSpecificTypePropertyAttribute), true /* search inheritance chain */ );
 
                if (attributes.Length > 0 && ((DbProviderSpecificTypePropertyAttribute)attributes[0]).IsProviderSpecificTypeProperty) {
                    ProviderManager.providerData.ProviderTypeProperty = pi; 
                    return pi; 
                }
            } 

            ProviderManager.providerData.ProviderTypeProperty = null;
            return null;
        } 

        private static object CreateObject( DbProviderFactory factory, ProviderSupportedClasses kindOfObject, string providerName ) { 
            Debug.Assert( factory != null ); 

            switch( kindOfObject ) { 
                case ProviderSupportedClasses.DbConnection:
                    return factory.CreateConnection();

                case ProviderSupportedClasses.DbDataAdapter: 
                    return factory.CreateDataAdapter();
 
                case ProviderSupportedClasses.DbParameter: 
                    return factory.CreateParameter();
 
                case ProviderSupportedClasses.DbCommand:
                    return factory.CreateCommand();

                case ProviderSupportedClasses.DbCommandBuilder: 
                    return factory.CreateCommandBuilder();
 
                case ProviderSupportedClasses.DbDataSourceEnumerator: 
                    return factory.CreateDataSourceEnumerator();
 
                case ProviderSupportedClasses.CodeAccessPermission:
                    return factory.CreatePermission(System.Security.Permissions.PermissionState.None);

                default: 
                    string errorMessage = string.Format( System.Globalization.CultureInfo.CurrentCulture, "Cannot create object of provider class identified by enum {0} for provider {1}",
                                                         Enum.GetName(typeof(ProviderSupportedClasses), kindOfObject), providerName ); 
                    throw new InternalException( errorMessage ); 
            }
        } 



        private static void EnsureFactoryTable() { 
            if( ProviderManager.factoryTable == null ) {
                ProviderManager.factoryTable = DbProviderFactories.GetFactoryClasses(); 
                if( ProviderManager.factoryTable == null ) { 
                    throw new InternalException("Unable to get factory-table.");
                } 
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
 
using System; 
using System.Collections;
using System.Collections.Specialized; 
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection; 

 
namespace System.Data.Design { 

    internal sealed class ProviderManager { 
        private static DataTable factoryTable = null;
        private static CachedProviderData providerData = new CachedProviderData();

        internal static Hashtable CustomDBProviders = null; 
        internal static DbProviderFactory ActiveFactoryContext = null;
 
        // Column names for DbProviderFactories section in machine.config file. 
        private static readonly string PROVIDER_NAME = "Name";
        private static readonly string PROVIDER_INVARIANT_NAME = "InvariantName"; 
        private static readonly string PROVIDER_ASSEMBLY = "AssemblyQualifiedName";

        internal enum ProviderSupportedClasses {
            DbConnection = 0, 
            DbDataAdapter = 1,
            DbParameter = 2, 
            DbCommand = 3, 
            DbCommandBuilder = 4,
            DbDataSourceEnumerator = 5, 
            CodeAccessPermission = 6,
            DbConnectionStringBuilder = 7,
        }
 

        private class CachedProviderData { 
            public DbProviderFactory CachedFactory = null; 
            public Type CachedType = null;
            public string CachedInvariantProviderName = string.Empty; 
            public string CachedDisplayName = string.Empty;
            private PropertyInfo providerTypeProperty = null;
            private bool useCachedPropertyValue = false;
 
            public PropertyInfo ProviderTypeProperty {
                get { return this.providerTypeProperty; } 
                set { this.providerTypeProperty = value; } 
            }
 
            public bool UseCachedPropertyValue {
                get { return this.useCachedPropertyValue; }
                set { this.useCachedPropertyValue= value; }
            } 

 
            public bool Matches( Type type ) { 
                if( (this.CachedFactory != null) && (this.CachedType != null) && (this.CachedType.Equals(type)) ) {
                    return true; 
                }
                else return false;
            }
 

            public bool Matches( string invariantName ) { 
                if( (this.CachedFactory != null) && (this.CachedInvariantProviderName != null) 
                && StringUtil.EqualValue(this.CachedInvariantProviderName, invariantName) ) {
                    return true; 
                }
                else return false;
            }
 

            public bool Matches( DbProviderFactory factory ) { 
                if( (this.CachedFactory != null) && this.CachedFactory.GetType().Equals(factory.GetType()) ) { 
                    return true;
                } 
                else return false;
            }

 
            public void Initialize( DbProviderFactory factory, string invariantProviderName, string displayName ) {
                this.CachedFactory = factory; 
                this.CachedInvariantProviderName = invariantProviderName; 
                this.CachedType = null;
                this.CachedDisplayName = displayName; 
                this.ProviderTypeProperty = null;
                this.UseCachedPropertyValue = false;
            }
 
            public void Initialize( DbProviderFactory factory, string invariantProviderName, string displayName, Type type ) {
                Initialize( factory, invariantProviderName, displayName ); 
                this.CachedType = type; 
            }
        } 



        public static DbProviderFactory GetFactoryFromType(Type type, ProviderSupportedClasses kindOfObject) { 
            if(type == null) {
                throw new ArgumentNullException("type"); 
            } 

            if( ProviderManager.providerData.Matches(type) ) { 
                return ProviderManager.providerData.CachedFactory;
            }

            EnsureFactoryTable(); 

            foreach(DataRow row in ProviderManager.factoryTable.Rows) { 
                DbProviderFactory factory = DbProviderFactories.GetFactory(row); 

                string providerName = (string) row[PROVIDER_NAME]; 
                object o = CreateObject( factory, kindOfObject, providerName );

                if( type.Equals(o.GetType()) ) {
                    ProviderManager.providerData.Initialize( factory, (string) row[PROVIDER_INVARIANT_NAME], 
                                                             (string) row[PROVIDER_NAME], type );
 
                    return factory; 
                }
            } 

            throw new InternalException( String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unable to find DbProviderFactory for type {0}", type.ToString()) );
        }
 

 
        public static string GetInvariantProviderName( DbProviderFactory factory ) { 
            if( factory == null ) {
                throw new ArgumentNullException("factory"); 
            }

            if( ProviderManager.providerData.Matches(factory) ) {
                return ProviderManager.providerData.CachedInvariantProviderName; 
            }
 
            EnsureFactoryTable(); 
            string factoryTypeName = factory.GetType().AssemblyQualifiedName;
 
            foreach(DataRow row in ProviderManager.factoryTable.Rows) {
                if( StringUtil.EqualValue( (string) row[PROVIDER_ASSEMBLY], factoryTypeName ) ) {
                    ProviderManager.providerData.Initialize( factory, (string) row[PROVIDER_INVARIANT_NAME], (string) row[PROVIDER_NAME] );
                    return ProviderManager.providerData.CachedInvariantProviderName; 
                }
            } 
 
            throw new InternalException( String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unable to get invariant name from factory. Factory type is {0}",
                                                       factory.GetType().ToString()) ); 
        }


        // DbProviderFactories expose a method that takes invariantName and returns a factory, 
        // but it is slow, so we want to wrap it and speed things up by a little caching.
        public static DbProviderFactory GetFactory( string invariantName ) { 
            if( StringUtil.EmptyOrSpace(invariantName) ) { 
                throw new ArgumentNullException("invariantName");
            } 

            // [Obsoleted] - Custom DBProviderFactory specified when TypedDataSetGenerator.Generate is
            //               invoked with ActiveFactoryContext. The ActiveFactoryContext is always used
            //               regardless of the invariantName specified. 
            //
            if ( ActiveFactoryContext != null ) { 
                ProviderManager.providerData.Initialize( ActiveFactoryContext, invariantName, invariantName ); 
                return ActiveFactoryContext;				
            } 

            // Custom DBProviderFactories specified when TypedDataSetGenerator.Generate is invoked with
            // a Hashtable of CustomFactories, indexed by InvariantName.
            // 
            if ( CustomDBProviders != null && CustomDBProviders.ContainsKey(invariantName) ) {
                DbProviderFactory customFactory = CustomDBProviders[invariantName] as DbProviderFactory; 
                if (customFactory != null) { 
                    ProviderManager.providerData.Initialize( customFactory, invariantName, invariantName );
                    return customFactory; 
                }
            }

            if( ProviderManager.providerData.Matches(invariantName) ) { 
                return ProviderManager.providerData.CachedFactory;
            } 
 
            EnsureFactoryTable();
 
            DataRow[] rows = ProviderManager.factoryTable.Select( string.Format(System.Globalization.CultureInfo.CurrentCulture, "InvariantName = '{0}'", invariantName) );
            if( rows.Length == 0 ) {
                throw new InternalException( string.Format(System.Globalization.CultureInfo.CurrentCulture, "Cannot find provider factory for provider named {0}", invariantName) );
            } 
            if( rows.Length > 1 ) {
                throw new InternalException( string.Format(System.Globalization.CultureInfo.CurrentCulture, "More that one data row for provider named {0}", invariantName) ); 
            } 

            DbProviderFactory factory = DbProviderFactories.GetFactory(rows[0]); 
            ProviderManager.providerData.Initialize( factory, invariantName, (string) rows[0][PROVIDER_NAME] );
            return factory;
        }
 
        public static PropertyInfo GetProviderTypeProperty(DbProviderFactory factory) {
            if(factory == null) { 
                throw new ArgumentNullException("factory should not be null."); 
            }
 
            if (ProviderManager.providerData.UseCachedPropertyValue) {
                return ProviderManager.providerData.ProviderTypeProperty;
            }
 
            ProviderManager.providerData.UseCachedPropertyValue = true;
            DbParameter parameter = factory.CreateParameter(); 
 
            PropertyInfo[] properties = parameter.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo pi in properties) { 
                if (!pi.PropertyType.IsEnum) { continue; }

                object[] attributes = pi.GetCustomAttributes(typeof(DbProviderSpecificTypePropertyAttribute), true /* search inheritance chain */ );
 
                if (attributes.Length > 0 && ((DbProviderSpecificTypePropertyAttribute)attributes[0]).IsProviderSpecificTypeProperty) {
                    ProviderManager.providerData.ProviderTypeProperty = pi; 
                    return pi; 
                }
            } 

            ProviderManager.providerData.ProviderTypeProperty = null;
            return null;
        } 

        private static object CreateObject( DbProviderFactory factory, ProviderSupportedClasses kindOfObject, string providerName ) { 
            Debug.Assert( factory != null ); 

            switch( kindOfObject ) { 
                case ProviderSupportedClasses.DbConnection:
                    return factory.CreateConnection();

                case ProviderSupportedClasses.DbDataAdapter: 
                    return factory.CreateDataAdapter();
 
                case ProviderSupportedClasses.DbParameter: 
                    return factory.CreateParameter();
 
                case ProviderSupportedClasses.DbCommand:
                    return factory.CreateCommand();

                case ProviderSupportedClasses.DbCommandBuilder: 
                    return factory.CreateCommandBuilder();
 
                case ProviderSupportedClasses.DbDataSourceEnumerator: 
                    return factory.CreateDataSourceEnumerator();
 
                case ProviderSupportedClasses.CodeAccessPermission:
                    return factory.CreatePermission(System.Security.Permissions.PermissionState.None);

                default: 
                    string errorMessage = string.Format( System.Globalization.CultureInfo.CurrentCulture, "Cannot create object of provider class identified by enum {0} for provider {1}",
                                                         Enum.GetName(typeof(ProviderSupportedClasses), kindOfObject), providerName ); 
                    throw new InternalException( errorMessage ); 
            }
        } 



        private static void EnsureFactoryTable() { 
            if( ProviderManager.factoryTable == null ) {
                ProviderManager.factoryTable = DbProviderFactories.GetFactoryClasses(); 
                if( ProviderManager.factoryTable == null ) { 
                    throw new InternalException("Unable to get factory-table.");
                } 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
