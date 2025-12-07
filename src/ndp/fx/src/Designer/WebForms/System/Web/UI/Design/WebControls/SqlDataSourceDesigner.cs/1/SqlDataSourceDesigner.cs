//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Data.Common;
    using System.ComponentModel.Design.Data; 
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing.Design; 
    using System.Globalization;
    using System.IO;
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms; 
    using System.Windows.Forms.Design;
 

    /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner"]/*' />
    /// <devdoc>
    /// SqlDataSourceDesigner is the designer associated with a SqlDataSource. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class SqlDataSourceDesigner : DataSourceDesigner { 
        internal const string AspNetDatabaseObjectPrefix = "AspNet_";
        internal const string DefaultProviderName = "System.Data.SqlClient"; 
        internal const string DefaultViewName = "DefaultView";

        private const string DesignerStateDataSourceSchemaKey = "DataSourceSchema";
        private const string DesignerStateDataSourceSchemaConnectionStringHashKey = "DataSourceSchemaConnectionStringHash"; 
        private const string DesignerStateDataSourceSchemaProviderNameKey = "DataSourceSchemaProviderName";
        private const string DesignerStateDataSourceSchemaSelectCommandKey = "DataSourceSchemaSelectMethod"; 
 
        private const string DesignerStateTableQueryStateKey = "TableQueryState";
        private const string DesignerStateSaveConfiguredConnectionStateKey = "SaveConfiguredConnectionState"; 

        // Properties to be removed from the property grid at design time
        // The SelectCommand is not hidden here, it is hidden separately
        // because it has a special design time shadow property. 
        private static readonly string[] _hiddenProperties = new string[] {
            "DeleteCommand", 
            "DeleteParameters", 
            "InsertCommand",
            "InsertParameters", 
            "SelectParameters",
            "UpdateCommand",
            "UpdateParameters",
        }; 

        private DesignerDataSourceView _view; 
 
        // Indicates that when retrieving schema, the schema should be returned even
        // if it is no longer consistent with the current state of the data source. 
        private bool _forceSchemaRetrieval;


        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.CanConfigure"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public override bool CanConfigure { 
            get {
                IDataEnvironment dataEnvironment = (IDataEnvironment)Component.Site.GetService(typeof(IDataEnvironment)); 
                return (dataEnvironment != null);
            }
        }
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.CanRefreshSchema"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public override bool CanRefreshSchema {
            get { 
                string connectionString = ConnectionString;
                return ((connectionString != null) &&
                        (connectionString.Trim().Length != 0) &&
                        (SelectCommand.Trim().Length != 0)); 
            }
        } 
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.ConnectionString"]/*' />
        /// <devdoc> 
        /// Implements the designer's version of the ConnectionString property.
        /// This is used to shadow the ConnectionString property of the
        /// runtime control.
        /// </devdoc> 
        public string ConnectionString {
            get { 
                return GetConnectionString(); 
            }
            set { 
                if (value != ConnectionString) {
                    SqlDataSource.ConnectionString = value;
                    UpdateDesignTimeHtml();
                    OnDataSourceChanged(EventArgs.Empty); 
                }
            } 
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.DeleteQuery"]/*' /> 
        /// <devdoc>
        /// Dummy design-time property.
        /// </devdoc>
        [ 
        Category("Data"),
        DefaultValue(DataSourceOperation.Delete), 
        SRDescription(SR.SqlDataSourceDesigner_DeleteQuery), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        Editor(typeof(SqlDataSourceQueryEditor), typeof(UITypeEditor)), 
        MergableProperty(false),
        TypeConverter(typeof(SqlDataSourceQueryConverter)),
        ]
        public DataSourceOperation DeleteQuery { 
            get {
                return DataSourceOperation.Delete; 
            } 
            set {
            } 
        }

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.InsertQuery"]/*' />
        /// <devdoc> 
        /// Dummy design-time property.
        /// </devdoc> 
        [ 
        Category("Data"),
        DefaultValue(DataSourceOperation.Insert), 
        SRDescription(SR.SqlDataSourceDesigner_InsertQuery),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        Editor(typeof(SqlDataSourceQueryEditor), typeof(UITypeEditor)),
        MergableProperty(false), 
        TypeConverter(typeof(SqlDataSourceQueryConverter)),
        ] 
        public DataSourceOperation InsertQuery { 
            get {
                return DataSourceOperation.Insert; 
            }
            set {
            }
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.ProviderName"]/*' /> 
        /// <devdoc> 
        /// Implements the designer's version of the ProviderName property.
        /// This is used to shadow the ProviderName property of the 
        /// runtime control.
        /// </devdoc>
        public string ProviderName {
            get { 
                return SqlDataSource.ProviderName;
            } 
            set { 
                if (value != ProviderName) {
                    SqlDataSource.ProviderName = value; 
                    UpdateDesignTimeHtml();
                    OnDataSourceChanged(EventArgs.Empty);
                }
            } 
        }
 
        /// <devdoc> 
        /// Stores the state of the "Save Configured Connection" checkbox in
        /// the wizard's panel. 
        /// </devdoc>
        internal bool SaveConfiguredConnectionState {
            get {
                object o = DesignerState[DesignerStateSaveConfiguredConnectionStateKey]; 
                if (o == null) {
                    return true; 
                } 
                return (bool)o;
            } 
            set {
                DesignerState[DesignerStateSaveConfiguredConnectionStateKey] = value;
            }
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.SelectCommand"]/*' /> 
        /// <devdoc> 
        /// Implements the designer's version of the SelectCommand property.
        /// This is used to shadow the SelectCommand property of the 
        /// runtime control.
        /// </devdoc>
        public string SelectCommand {
            get { 
                return SqlDataSource.SelectCommand;
            } 
            set { 
                if (value != SelectCommand) {
                    SqlDataSource.SelectCommand = value; 
                    UpdateDesignTimeHtml();
                    OnDataSourceChanged(EventArgs.Empty);
                }
            } 
        }
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.SelectQuery"]/*' /> 
        /// <devdoc>
        /// Dummy design-time property. 
        /// </devdoc>
        [
        Category("Data"),
        DefaultValue(DataSourceOperation.Select), 
        SRDescription(SR.SqlDataSourceDesigner_SelectQuery),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        Editor(typeof(SqlDataSourceQueryEditor), typeof(UITypeEditor)), 
        MergableProperty(false),
        TypeConverter(typeof(SqlDataSourceQueryConverter)), 
        ]
        public DataSourceOperation SelectQuery {
            get {
                return DataSourceOperation.Select; 
            }
            set { 
            } 
        }
 
        /// <devdoc>
        /// The SqlDataSource associated with this designer.
        /// </devdoc>
        internal SqlDataSource SqlDataSource { 
            get {
                return (SqlDataSource)Component; 
            } 
        }
 
        /// <devdoc>
        /// Stores the state of the Configure Data Source wizard's table/field
        /// picker for smart re-entrancy.
        /// </devdoc> 
        internal Hashtable TableQueryState {
            get { 
                return DesignerState[DesignerStateTableQueryStateKey] as Hashtable; 
            }
            set { 
                DesignerState[DesignerStateTableQueryStateKey] = value;
            }
        }
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.UpdateQuery"]/*' />
        /// <devdoc> 
        /// Dummy design-time property. 
        /// </devdoc>
        [ 
        Category("Data"),
        DefaultValue(DataSourceOperation.Update),
        SRDescription(SR.SqlDataSourceDesigner_UpdateQuery),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        Editor(typeof(SqlDataSourceQueryEditor), typeof(UITypeEditor)),
        MergableProperty(false), 
        TypeConverter(typeof(SqlDataSourceQueryConverter)), 
        ]
        public DataSourceOperation UpdateQuery { 
            get {
                return DataSourceOperation.Update;
            }
            set { 
            }
        } 
 
        /// <devdoc>
        /// Builds a select command with associated parameters. 
        /// </devdoc>
        internal DbCommand BuildSelectCommand(DbProviderFactory factory, DbConnection connection, string commandText, ParameterCollection parameters, SqlDataSourceCommandType commandType) {
            //
            DbCommand command = CreateCommand(factory, commandText, connection); 

            // Add parameters, if any 
            if (parameters != null && parameters.Count > 0) { 
                IOrderedDictionary parameterValues = parameters.GetValues(null, null);
                string parameterPrefix = GetParameterPrefix(factory); 
                for (int i = 0; i < parameters.Count; i++ ) {
                    Parameter parameter = parameters[i];
                    DbParameter dbParam = CreateParameter(factory);
                    dbParam.ParameterName = parameterPrefix + parameter.Name; 
                    if (parameter.Type != TypeCode.Empty && parameter.Type != TypeCode.DBNull) {
                        dbParam.DbType = ConvertTypeCodeToDbType(parameter.Type); 
                    } 
                    if (parameter.Type == TypeCode.Empty && ProviderRequiresDbTypeSet(factory)) {
                        // VSWhidbey 493918: ODBC and OLE DB apparently require that the type of the 
                        // parameter be set explicitly, so we just set is to Object.
                        dbParam.DbType = DbType.Object;
                    }
                    dbParam.Value = parameterValues[i]; 
                    if (dbParam.Value == null) {
                        dbParam.Value = DBNull.Value; 
                    } 
                    // For some providers, variable length types require explicit sizes
                    if (parameter.Type == TypeCode.String) { 
                        if ((dbParam.Value is string) && (dbParam.Value != null)) {
                            dbParam.Size = ((string)dbParam.Value).Length;
                        }
                        else { 
                            dbParam.Size = 1;
                        } 
                    } 
                    command.Parameters.Add(dbParam);
                } 
            }

            // Set commmand type
            command.CommandType = GetCommandType(commandType); 

            return command; 
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.Configure"]/*' /> 
        /// <devdoc>
        /// Handles the Configure DataSource designer verb event.
        /// </devdoc>
        public override void Configure() { 
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConfigureDataSourceChangeCallback), null, SR.GetString(SR.DataSource_ConfigureTransactionDescription));
        } 
 
        /// <devdoc>
        /// Transacted change callback to invoke the Configure DataSource wizard. 
        /// </devdoc>
        private bool ConfigureDataSourceChangeCallback(object context) {
            try {
                SuppressDataSourceEvents(); 

                IServiceProvider site = Component.Site; 
 
                IDataEnvironment dataEnvironment = (IDataEnvironment)site.GetService(typeof(IDataEnvironment));
                if (dataEnvironment == null) { 
                    Debug.Fail("Cannot launch Configure DataSource Wizard without IDataEnvironment service");
                    return false;
                }
 
                IDataSourceViewSchema oldViewSchema = GetView(DefaultViewName).Schema;
                bool wasForceUsed = false; 
                if (oldViewSchema == null) { 
                    _forceSchemaRetrieval = true;
                    oldViewSchema = GetView(DefaultViewName).Schema; 
                    _forceSchemaRetrieval = false;
                    if (oldViewSchema != null) {
                        // Only consider it to be a "forced" schema retrieval if we actually got something out of it
                        wasForceUsed = true; 
                    }
                } 
 
                SqlDataSourceWizardForm form = CreateConfigureDataSourceWizardForm(site, dataEnvironment);
                DialogResult result = UIServiceHelper.ShowDialog(site, form); 
                if (result == DialogResult.OK) {
                    // We force this call to make sure that expression-bound properties such as
                    // ConnectionString and ProviderName get updated with their evaluated values.
                    OnComponentChanged(this, new ComponentChangedEventArgs(Component, null, null, null)); 

                    // Compare new schema to old schema and if it changed, raise the SchemaRefreshed event 
                    IDataSourceViewSchema newViewSchema = null; 
                    try {
                        _forceSchemaRetrieval = true; 
                        newViewSchema = GetView(DefaultViewName).Schema;
                    }
                    finally {
                        _forceSchemaRetrieval = false; 
                    }
                    if (!wasForceUsed && !ViewSchemasEquivalent(oldViewSchema, newViewSchema)) { 
                        OnSchemaRefreshed(EventArgs.Empty); 
                    }
                    OnDataSourceChanged(EventArgs.Empty); 
                    return true;
                }
                else {
                    return false; 
                }
            } 
            finally { 
                ResumeDataSourceEvents();
            } 
        }

        /// <devdoc>
        /// Returns true if these two connections are equivalent. Currently the 
        /// check is to make sure they use the exact same connection string and
        /// provider, though this could potentially be improved to check for 
        /// the specific database and server. 
        /// </devdoc>
        internal static bool ConnectionsEqual(DesignerDataConnection connection1, DesignerDataConnection connection2) { 
            Debug.Assert((connection1 != null) || (connection2 != null), "At least one of the connections must be non null");

            // If either connection is null, these are not the same connection
            if ((connection1 == null) || (connection2 == null)) { 
                return false;
            } 
 
            // Compare connection strings
            if (connection1.ConnectionString != connection2.ConnectionString) { 
                return false;
            }

            // Compare provider names, taking into account the default provider 
            string providerName1 = (connection1.ProviderName.Trim().Length == 0 ? DefaultProviderName : connection1.ProviderName);
            string providerName2 = (connection2.ProviderName.Trim().Length == 0 ? DefaultProviderName : connection2.ProviderName); 
            return (providerName1 == providerName2); 
        }
 
        /// <devdoc>
        /// Gets the equivalent TypeCode from a DbType.
        /// </devdoc>
        internal static TypeCode ConvertDbTypeToTypeCode(DbType dbType) { 
            switch (dbType) {
                case DbType.AnsiString: 
                case DbType.AnsiStringFixedLength: 
                case DbType.String:
                case DbType.StringFixedLength: 
                    return TypeCode.String;
                case DbType.Boolean:
                    return TypeCode.Boolean;
                case DbType.Byte: 
                    return TypeCode.Byte;
                case DbType.VarNumeric:     // ??? 
                case DbType.Currency: 
                case DbType.Decimal:
                    return TypeCode.Decimal; 
                case DbType.Date:
                case DbType.DateTime:
                case DbType.Time:
                    return TypeCode.DateTime; 
                case DbType.Double:
                    return TypeCode.Double; 
                case DbType.Int16: 
                    return TypeCode.Int16;
                case DbType.Int32: 
                    return TypeCode.Int32;
                case DbType.Int64:
                    return TypeCode.Int64;
                case DbType.SByte: 
                    return TypeCode.SByte;
                case DbType.Single: 
                    return TypeCode.Single; 
                case DbType.UInt16:
                    return TypeCode.UInt16; 
                case DbType.UInt32:
                    return TypeCode.UInt32;
                case DbType.UInt64:
                    return TypeCode.UInt64; 
                case DbType.Guid:           // ???
                case DbType.Binary: 
                case DbType.Object: 
                default:
                    return TypeCode.Object; 
            }
        }

        /// <devdoc> 
        /// Gets the equivalent DbType from a TypeCode.
        /// </devdoc> 
        internal static DbType ConvertTypeCodeToDbType(TypeCode typeCode) { 
            switch (typeCode) {
                case TypeCode.Boolean: 
                    return DbType.Boolean;
                case TypeCode.Byte:
                    return DbType.Byte;
                case TypeCode.Char: 
                    return DbType.StringFixedLength;    // ???
                case TypeCode.DateTime: 
                    return DbType.DateTime; 
                case TypeCode.Decimal:
                    return DbType.Decimal; 
                case TypeCode.Double:
                    return DbType.Double;
                case TypeCode.Int16:
                    return DbType.Int16; 
                case TypeCode.Int32:
                    return DbType.Int32; 
                case TypeCode.Int64: 
                    return DbType.Int64;
                case TypeCode.SByte: 
                    return DbType.SByte;
                case TypeCode.Single:
                    return DbType.Single;
                case TypeCode.String: 
                    return DbType.String;
                case TypeCode.UInt16: 
                    return DbType.UInt16; 
                case TypeCode.UInt32:
                    return DbType.UInt32; 
                case TypeCode.UInt64:
                    return DbType.UInt64;
                case TypeCode.DBNull:
                case TypeCode.Empty: 
                case TypeCode.Object:
                default: 
                    return DbType.Object; 
            }
        } 

        /// <devdoc>
        /// Copies an ICollection of ICloneable items to another IList.
        /// </devdoc> 
        internal void CopyList(ICollection source, IList dest) {
            dest.Clear(); 
            foreach (ICloneable item in source) { 
                object clonedItem = item.Clone();
                RegisterClone(item, clonedItem); 
                dest.Add(clonedItem);
            }
        }
 
        /// <devdoc>
        /// Creates the appropriate wizard for the Configure Data Source task. 
        /// </devdoc> 
        internal virtual SqlDataSourceWizardForm CreateConfigureDataSourceWizardForm(IServiceProvider serviceProvider, IDataEnvironment dataEnvironment) {
            return new SqlDataSourceWizardForm(serviceProvider, this, dataEnvironment); 
        }

        /// <devdoc>
        /// Creates an DbCommand based on the ProviderName. 
        /// </devdoc>
        internal static DbCommand CreateCommand(DbProviderFactory factory, string commandText, DbConnection connection) { 
            DbCommand command = factory.CreateCommand(); 
            command.CommandText = commandText;
            command.Connection = connection; 
            return command;
        }

        /// <devdoc> 
        /// Creates an DbDataAdapter based on the ProviderName.
        /// </devdoc> 
        internal static DbDataAdapter CreateDataAdapter(DbProviderFactory factory, DbCommand command) { 
            DbDataAdapter dataAdapter = factory.CreateDataAdapter();
            ((IDbDataAdapter)dataAdapter).SelectCommand = command; 
            return dataAdapter;
        }

        /// <devdoc> 
        /// Creates an DbParameter based on the ProviderName.
        /// </devdoc> 
        internal static DbParameter CreateParameter(DbProviderFactory factory) { 
            return factory.CreateParameter();
        } 

        protected virtual SqlDesignerDataSourceView CreateView(string viewName) {
            return new SqlDesignerDataSourceView(this, viewName);
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.DeriveParameters"]/*' /> 
        /// <devdoc> 
        /// Calls the appropriate CommandBuilder (e.g. SqlCommandBuilder) to derive the parameters of a stored procedure.
        /// </devdoc> 
        protected virtual void DeriveParameters(string providerName, DbCommand command) {
            //

            if (String.Equals(providerName, "System.Data.Odbc", StringComparison.OrdinalIgnoreCase)) { 
                System.Data.Odbc.OdbcCommandBuilder.DeriveParameters((System.Data.Odbc.OdbcCommand)command);
            } 
            else { 
                if (String.Equals(providerName, "System.Data.OleDb", StringComparison.OrdinalIgnoreCase)) {
                    System.Data.OleDb.OleDbCommandBuilder.DeriveParameters((System.Data.OleDb.OleDbCommand)command); 
                }
                else {
                    if (String.Equals(providerName, "System.Data.SqlClient", StringComparison.OrdinalIgnoreCase) ||
                        String.IsNullOrEmpty(providerName)) { 
                        System.Data.SqlClient.SqlCommandBuilder.DeriveParameters((System.Data.SqlClient.SqlCommand)command);
                    } 
                    else { 
                        UIServiceHelper.ShowError(
                            SqlDataSource.Site, 
                            SR.GetString(SR.SqlDataSourceDesigner_InferStoredProcedureNotSupported, providerName));
                    }
                }
            } 
        }
 
        /// <devdoc> 
        /// Converts a SqlDataSourceCommandType to a System.Data.CommandType.
        /// </devdoc> 
        private static CommandType GetCommandType(SqlDataSourceCommandType commandType) {
            if (commandType == SqlDataSourceCommandType.Text) {
                return CommandType.Text;
            } 
            return CommandType.StoredProcedure;
        } 
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.GetConnectionString"]/*' />
        /// <devdoc> 
        /// Gets the data source's connection string. Override this if the data source
        /// has to perform any additional operations in order for the connection string
        /// to be retrieved. By default this method returns the runtime control's
        /// connection string. 
        /// </devdoc>
        protected virtual string GetConnectionString() { 
            return SqlDataSource.ConnectionString; 
        }
 
        /// <devdoc>
        /// Gets the DbProviderFactory associated with the provider type specified in the ProviderName property.
        /// If no provider is specified, the System.Data.SqlClient factory is used.
        /// </devdoc> 
        internal static DbProviderFactory GetDbProviderFactory(string providerName) {
            // Default to SQL provider 
            if (providerName.Length == 0) { 
                providerName = DefaultProviderName;
            } 
            DbProviderFactory factory = DbProviderFactories.GetFactory(providerName);
            Debug.Assert(factory != null);
            return factory;
        } 

        /// <devdoc> 
        /// Gets a design-time version of a given connection. This might 
        /// involve using an alternate set of credentials or mapping paths
        /// in the connection string such that they are valid at design time. 
        /// </devdoc>
        internal static DbConnection GetDesignTimeConnection(IServiceProvider serviceProvider, DesignerDataConnection connection) {
            if (serviceProvider != null) {
                IDataEnvironment de = (IDataEnvironment)serviceProvider.GetService(typeof(IDataEnvironment)); 
                if (de != null) {
                    if (String.IsNullOrEmpty(connection.ProviderName)) { 
                        connection = new DesignerDataConnection(connection.Name, DefaultProviderName, connection.ConnectionString); 
                    }
                    return de.GetDesignTimeConnection(connection); 
                }
            }
            return null;
        } 

        public override DesignerDataSourceView GetView(string viewName) { 
            if (String.IsNullOrEmpty(viewName)) { 
                viewName = DefaultViewName;
            } 
            if (String.Equals(viewName, DefaultViewName, StringComparison.OrdinalIgnoreCase)) {
                if (_view == null) {
                    _view = CreateView(viewName);
                } 
                return _view;
            } 
            return null; 
        }
 
        public override string[] GetViewNames() {
            return new string[] { DefaultViewName };
        }
 
        /// <devdoc>
        /// Indicates the prefix for parameter placeholders (as used in 
        /// DbCommand.CommandText property). 
        /// </devdoc>
        internal static string GetParameterPlaceholderPrefix(DbProviderFactory factory) { 
            if (factory == null) {
                throw new ArgumentNullException("factory");
            }
            // 
            if (factory == System.Data.SqlClient.SqlClientFactory.Instance) {
                return "@"; 
            } 
            else {
                if (factory == System.Data.OracleClient.OracleClientFactory.Instance) { 
                    return ":";
                }
                else {
                    return "?"; 
                }
            } 
        } 

        /// <devdoc> 
        /// Indicates the prefix for parameters (as used in the
        /// DbCommand.Parameters collection).
        /// </devdoc>
        internal static string GetParameterPrefix(DbProviderFactory factory) { 
            if (factory == null) {
                throw new ArgumentNullException("factory"); 
            } 
            //
            if (factory == System.Data.SqlClient.SqlClientFactory.Instance) { 
                return "@";
            }
            else {
                return String.Empty; 
            }
        } 
 
        /// <devdoc>
        /// Returns an array of all known parameter prefixes. 
        /// </devdoc>
        private static string[] GetParameterPrefixes() {
            return new string[] { "@", "?", ":" };
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.InferParameterNames"]/*' /> 
        /// <devdoc> 
        /// Gets an array of Parameter objects from a SqlDataSource's command.
        /// If the command text indicates a stored procedure, a call will be made to the server 
        /// to get the parameter types. Otherwise a SQL parser is used to extract the parameter
        /// names.
        /// </devdoc>
        protected internal virtual Parameter[] InferParameterNames(DesignerDataConnection connection, string commandText, SqlDataSourceCommandType commandType) { 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor; 

                if (commandText.Length == 0) { 
                    UIServiceHelper.ShowError(
                        SqlDataSource.Site,
                        SR.GetString(SR.SqlDataSourceDesigner_NoCommand));
                    return null; 
                }
 
                if (commandType == SqlDataSourceCommandType.Text) { 
                    // Command text
                    return SqlDataSourceParameterParser.ParseCommandText(connection.ProviderName, commandText); 
                }
                else {
                    // Stored procedure
                    DbProviderFactory factory = GetDbProviderFactory(connection.ProviderName); 
                    DbConnection conn = null;
                    try { 
                        conn = GetDesignTimeConnection(Component.Site, connection); 
                    }
                    catch (Exception ex) { 
                        if (conn == null) {
                            UIServiceHelper.ShowError(
                                SqlDataSource.Site,
                                ex, 
                                SR.GetString(SR.SqlDataSourceDesigner_CouldNotCreateConnection));
                            return null; 
                        } 
                    }
 
                    if (conn == null) {
                        UIServiceHelper.ShowError(
                            SqlDataSource.Site,
                            SR.GetString(SR.SqlDataSourceDesigner_CouldNotCreateConnection)); 
                        return null;
                    } 
                    DbCommand command = BuildSelectCommand(factory, conn, commandText, null, commandType); 
                    command.CommandType = CommandType.StoredProcedure;
                    try { 
                        Debug.Assert(conn.State == ConnectionState.Open, "Expected connection state to be open - IDataEnvironment.GetDesignTimeConnection() should have done this");
                        DeriveParameters(connection.ProviderName, command);
                    }
                    catch (Exception ex) { 
                        // If there were any errors in deriving the parameters, abort
                        // the entire operation. 
                        UIServiceHelper.ShowError( 
                            SqlDataSource.Site,
                            SR.GetString(SR.SqlDataSourceDesigner_InferStoredProcedureError, ex.Message)); 

                        return null;
                    }
                    finally { 
                        if (command.Connection.State == ConnectionState.Open) {
                            conn.Close(); 
                        } 
                    }
 
                    int paramCount = command.Parameters.Count;
                    Parameter[] derivedParameters = new Parameter[paramCount];
                    for (int i = 0; i < paramCount; i++) {
                        IDataParameter parameter = command.Parameters[i] as IDataParameter; 
                        if (parameter != null) {
                            // Trim parameter prefix if present 
                            string paramName = StripParameterPrefix(parameter.ParameterName); 
                            // Convert type from DB type to CLR type
                            TypeCode type = ConvertDbTypeToTypeCode(parameter.DbType); 
                            derivedParameters[i] = new Parameter(paramName, type);
                            derivedParameters[i].Direction = parameter.Direction;
                        }
                        else { 
                            //
                            Debug.Fail("Parameter is not an IDataParameter"); 
                        } 
                    }
                    return derivedParameters; 
                }
            }
            finally {
                Cursor.Current = originalCursor; 
            }
        } 
 
        /// <devdoc>
        /// Attempts to load the schema for this SqlDataSource. If the 
        /// schema is not consistent with the current properties, then it is
        /// removed from state.
        /// </devdoc>
        internal DataTable LoadSchema() { 
            if (!_forceSchemaRetrieval) {
                // Only check for consistency if we are not forcing the retrieval 
                object connectionStringHash = DesignerState[DesignerStateDataSourceSchemaConnectionStringHashKey]; 
                string providerName = DesignerState[DesignerStateDataSourceSchemaProviderNameKey] as string;
                string selectCommand = DesignerState[DesignerStateDataSourceSchemaSelectCommandKey] as string; 
                if (String.IsNullOrEmpty(providerName)) {
                    providerName = DefaultProviderName;
                }
 
                if (String.IsNullOrEmpty(ConnectionString)) {
                    // If there is no connection string, we definitely don't have any schema 
                    return null; 
                }
 
                DesignerDataConnection oldConnection = new DesignerDataConnection(String.Empty, ProviderName, ConnectionString);

                string oldConnectionString = oldConnection.ConnectionString;
                int oldConnectionStringHash = oldConnectionString.GetHashCode(); 
                string oldProviderName = oldConnection.ProviderName;
                string oldSelectCommand = SelectCommand; 
                if (String.IsNullOrEmpty(oldProviderName)) { 
                    oldProviderName = DefaultProviderName;
                } 

                if ((connectionStringHash == null) ||
                    ((int)connectionStringHash != oldConnectionStringHash) ||
                    (!String.Equals(providerName, oldProviderName, StringComparison.OrdinalIgnoreCase)) || 
                    (!String.Equals(selectCommand, oldSelectCommand, StringComparison.Ordinal))) {
 
                    // The schema is not consistent with the current properties, return nothing 
                    return null;
                } 
            }

            // Either we are forcing schema retrieval, or we're not forcing but we're consistent, so get the schema
            DataTable schema = DesignerState[DesignerStateDataSourceSchemaKey] as DataTable; 
            if (schema != null) {
                schema.TableName = DefaultViewName; 
                return schema; 
            }
            else { 
                return null;
            }
        }
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        /// Overridden by the designer to shadow various runtime properties 
        /// with corresponding properties that it implements.
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);

            PropertyDescriptor property; 

            // Hide runtime properties 
            foreach (string propertyName in _hiddenProperties) { 
                property = (PropertyDescriptor)properties[propertyName];
                if (property != null) { 
                    properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                }
            }
 
            // Add design-time properties
            properties["DeleteQuery"] = TypeDescriptor.CreateProperty( 
                                            GetType(), 
                                            "DeleteQuery",
                                            typeof(DataSourceOperation)); 
            properties["InsertQuery"] = TypeDescriptor.CreateProperty(
                                            GetType(),
                                            "InsertQuery",
                                            typeof(DataSourceOperation)); 
            properties["SelectQuery"] = TypeDescriptor.CreateProperty(
                                            GetType(), 
                                            "SelectQuery", 
                                            typeof(DataSourceOperation));
            properties["UpdateQuery"] = TypeDescriptor.CreateProperty( 
                                            GetType(),
                                            "UpdateQuery",
                                            typeof(DataSourceOperation));
 
            // Shadow runtime ConnectionString property
            property = (PropertyDescriptor)properties["ConnectionString"]; 
            Debug.Assert(property != null); 
            properties["ConnectionString"] = TypeDescriptor.CreateProperty(this.GetType(), property, new Attribute[0]);
 
            // Shadow runtime ProviderName property
            property = (PropertyDescriptor)properties["ProviderName"];
            Debug.Assert(property != null);
            properties["ProviderName"] = TypeDescriptor.CreateProperty(this.GetType(), property, new Attribute[0]); 

            // Shadow runtime SelectCommand property 
            property = (PropertyDescriptor)properties["SelectCommand"]; 
            Debug.Assert(property != null);
            properties["SelectCommand"] = TypeDescriptor.CreateProperty(this.GetType(), property, BrowsableAttribute.No); 
        }

        /// <devdoc>
        /// Returns true is the provider requires that the DbType property of a DbParameter 
        /// be set explicitly.
        /// </devdoc> 
        private static bool ProviderRequiresDbTypeSet(DbProviderFactory factory) { 
            return (factory == System.Data.OleDb.OleDbFactory.Instance ||
                    factory == System.Data.Odbc.OdbcFactory.Instance); 
        }

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.RefreshSchema"]/*' />
        /// <devdoc> 
        /// Refreshes the data source's schema. Override this method to perform whatever actions are necessary
        /// for the Schema property to return correct schema. 
        /// </devdoc> 
        public override void RefreshSchema(bool preferSilent) {
            try { 
                SuppressDataSourceEvents();

                bool success = false;
 
                IServiceProvider site = SqlDataSource.Site;
 
                // If the data source is not yet set up, abort the Refresh Schema operation 
                if (!CanRefreshSchema) {
                    if (!preferSilent) { 
                        UIServiceHelper.ShowError(
                            site,
                            SR.GetString(SR.SqlDataSourceDesigner_RefreshSchemaRequiresSettings));
                    } 
                    return;
                } 
 
                IDataSourceViewSchema oldViewSchema = GetView(DefaultViewName).Schema;
                bool wasForceUsed = false; 
                if (oldViewSchema == null) {
                    _forceSchemaRetrieval = true;
                    oldViewSchema = GetView(DefaultViewName).Schema;
                    _forceSchemaRetrieval = false; 
                    wasForceUsed = true;
                } 
 
                DesignerDataConnection runtimeConnection = new DesignerDataConnection(String.Empty, ProviderName, ConnectionString);
 
                if (preferSilent) {
                    // Silent mode - just get schema directly, and ignore errors
                    success = RefreshSchema(runtimeConnection, SelectCommand, SqlDataSource.SelectCommandType, SqlDataSource.SelectParameters, true);
                } 
                else {
                    // Non-silent mode - show UI to get parameter information 
                    // Infer parameters 
                    Parameter[] inferedParameters = InferParameterNames(runtimeConnection, SelectCommand, SqlDataSource.SelectCommandType);
 
                    // A null return value implies there was a problem retrieving parameters
                    if (inferedParameters == null) {
                        return;
                    } 

                    ParameterCollection inputParameters = new ParameterCollection(); 
 
                    ParameterCollection existingParameters = new ParameterCollection();
                    foreach (ICloneable param in SqlDataSource.SelectParameters) { 
                        existingParameters.Add((Parameter)param.Clone());
                    }

                    // Filter out all non-input parameters 
                    foreach (Parameter inferedParameter in inferedParameters) {
                        if ((inferedParameter.Direction == ParameterDirection.Input) || 
                            (inferedParameter.Direction == ParameterDirection.InputOutput)) { 

                            // Try to match this parameter with an existing parameter so we 
                            // can automatically pick up on a previously set default value
                            // or type.
                            Parameter existingParameter = existingParameters[inferedParameter.Name];
                            if (existingParameter != null) { 
                                inferedParameter.DefaultValue = existingParameter.DefaultValue;
                                if (inferedParameter.Type == TypeCode.Empty) { 
                                    inferedParameter.Type = existingParameter.Type; 
                                }
                                existingParameters.Remove(existingParameter); 
                            }
                            inputParameters.Add(inferedParameter);
                        }
                    } 

                    if (inputParameters.Count > 0) { 
                        SqlDataSourceRefreshSchemaForm form = new SqlDataSourceRefreshSchemaForm(site, this, inputParameters); 
                        DialogResult result = UIServiceHelper.ShowDialog(site, form);
                        success = (result == DialogResult.OK); 
                    }
                    else {
                        success = RefreshSchema(runtimeConnection, SelectCommand, SqlDataSource.SelectCommandType, inputParameters, false);
                    } 
                }
 
                if (success) { 
                    // Compare new schema to old schema and if it changed, raise the SchemaRefreshed event
                    IDataSourceViewSchema newViewSchema = GetView(DefaultViewName).Schema; 
                    if (wasForceUsed && ViewSchemasEquivalent(oldViewSchema, newViewSchema)) {
                        OnDataSourceChanged(EventArgs.Empty);
                    }
                    else { 
                        if (!ViewSchemasEquivalent(oldViewSchema, newViewSchema)) {
                            OnSchemaRefreshed(EventArgs.Empty); 
                        } 
                    }
                } 
            }
            finally {
                ResumeDataSourceEvents();
            } 
        }
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.RefreshSchema1"]/*' /> 
        /// <devdoc>
        /// Refreshes the schema of the data source. 
        /// </devdoc>
        internal bool RefreshSchema(DesignerDataConnection connection, string commandText, SqlDataSourceCommandType commandType, ParameterCollection parameters, bool preferSilent) {
            IServiceProvider site = SqlDataSource.Site;
 
            // Get schema from database
            DbCommand selectCommand = null; 
 
            try {
                DbProviderFactory factory = GetDbProviderFactory(connection.ProviderName); 
                DbConnection conn = GetDesignTimeConnection(Component.Site, connection);
                if (conn == null) {
                    if (!preferSilent) {
                        UIServiceHelper.ShowError( 
                            SqlDataSource.Site,
                            SR.GetString(SR.SqlDataSourceDesigner_CouldNotCreateConnection)); 
                    } 
                    return false;
                } 
                selectCommand = BuildSelectCommand(factory, conn, commandText, parameters, commandType);
                DbDataAdapter adapter = CreateDataAdapter(factory, selectCommand);
                adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
 
                DataSet dataSet = new DataSet();
 
                adapter.FillSchema(dataSet, SchemaType.Source, DefaultViewName); 

                // Some providers, such as Microsoft JET, do not necessarily throw an exception 
                // when they are unable to retrieve schema. Instead, they silently fail, so we
                // have to check if they returned a schema table.
                DataTable schemaTable = dataSet.Tables[DefaultViewName];
                if (schemaTable == null) { 
                    if (!preferSilent) {
                        UIServiceHelper.ShowError(site, SR.GetString(SR.SqlDataSourceDesigner_CannotGetSchema)); 
                    } 
                    return false;
                } 

                // Save the schema using the state service
                SaveSchema(connection, commandText, schemaTable);
 
                return true;
            } 
            catch (Exception ex) { 
                if (!preferSilent) {
                    UIServiceHelper.ShowError(site, ex, SR.GetString(SR.SqlDataSourceDesigner_CannotGetSchema)); 
                }
            }
            finally {
                if (selectCommand != null && selectCommand.Connection.State == ConnectionState.Open) { 
                    selectCommand.Connection.Close();
                } 
            } 
            return false;
        } 

        /// <devdoc>
        /// Saves schema using the DesignerState. Along with the schema are
        /// stored the type and method used to generate the schema so that we 
        /// can make sure the schema is consistent.
        /// </devdoc> 
        private void SaveSchema(DesignerDataConnection connection, string selectCommand, DataTable schemaTable) { 
            DesignerState[DesignerStateDataSourceSchemaKey] = schemaTable;
            DesignerState[DesignerStateDataSourceSchemaConnectionStringHashKey] = connection.ConnectionString.GetHashCode(); 
            DesignerState[DesignerStateDataSourceSchemaProviderNameKey] = connection.ProviderName;
            DesignerState[DesignerStateDataSourceSchemaSelectCommandKey] = selectCommand;
        }
 
        /// <devdoc>
        /// Strips all known parameter prefixes from a parameter name. 
        /// </devdoc> 
        internal static string StripParameterPrefix(string parameterName) {
            foreach (string prefix in GetParameterPrefixes()) { 
                if (parameterName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                    return parameterName.Substring(prefix.Length);
                }
            } 
            return parameterName;
        } 
 
        /// <devdoc>
        /// Indicates whether the provider supports named parameters. 
        /// </devdoc>
        internal static bool SupportsNamedParameters(DbProviderFactory factory) {
            if (factory == null) {
                throw new ArgumentNullException("factory"); 
            }
            // 
            if ((factory == System.Data.SqlClient.SqlClientFactory.Instance) || 
                (factory == System.Data.OracleClient.OracleClientFactory.Instance)) {
                return true; 
            }
            else {
                return false;
            } 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Data.Common;
    using System.ComponentModel.Design.Data; 
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing.Design; 
    using System.Globalization;
    using System.IO;
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms; 
    using System.Windows.Forms.Design;
 

    /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner"]/*' />
    /// <devdoc>
    /// SqlDataSourceDesigner is the designer associated with a SqlDataSource. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class SqlDataSourceDesigner : DataSourceDesigner { 
        internal const string AspNetDatabaseObjectPrefix = "AspNet_";
        internal const string DefaultProviderName = "System.Data.SqlClient"; 
        internal const string DefaultViewName = "DefaultView";

        private const string DesignerStateDataSourceSchemaKey = "DataSourceSchema";
        private const string DesignerStateDataSourceSchemaConnectionStringHashKey = "DataSourceSchemaConnectionStringHash"; 
        private const string DesignerStateDataSourceSchemaProviderNameKey = "DataSourceSchemaProviderName";
        private const string DesignerStateDataSourceSchemaSelectCommandKey = "DataSourceSchemaSelectMethod"; 
 
        private const string DesignerStateTableQueryStateKey = "TableQueryState";
        private const string DesignerStateSaveConfiguredConnectionStateKey = "SaveConfiguredConnectionState"; 

        // Properties to be removed from the property grid at design time
        // The SelectCommand is not hidden here, it is hidden separately
        // because it has a special design time shadow property. 
        private static readonly string[] _hiddenProperties = new string[] {
            "DeleteCommand", 
            "DeleteParameters", 
            "InsertCommand",
            "InsertParameters", 
            "SelectParameters",
            "UpdateCommand",
            "UpdateParameters",
        }; 

        private DesignerDataSourceView _view; 
 
        // Indicates that when retrieving schema, the schema should be returned even
        // if it is no longer consistent with the current state of the data source. 
        private bool _forceSchemaRetrieval;


        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.CanConfigure"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public override bool CanConfigure { 
            get {
                IDataEnvironment dataEnvironment = (IDataEnvironment)Component.Site.GetService(typeof(IDataEnvironment)); 
                return (dataEnvironment != null);
            }
        }
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.CanRefreshSchema"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public override bool CanRefreshSchema {
            get { 
                string connectionString = ConnectionString;
                return ((connectionString != null) &&
                        (connectionString.Trim().Length != 0) &&
                        (SelectCommand.Trim().Length != 0)); 
            }
        } 
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.ConnectionString"]/*' />
        /// <devdoc> 
        /// Implements the designer's version of the ConnectionString property.
        /// This is used to shadow the ConnectionString property of the
        /// runtime control.
        /// </devdoc> 
        public string ConnectionString {
            get { 
                return GetConnectionString(); 
            }
            set { 
                if (value != ConnectionString) {
                    SqlDataSource.ConnectionString = value;
                    UpdateDesignTimeHtml();
                    OnDataSourceChanged(EventArgs.Empty); 
                }
            } 
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.DeleteQuery"]/*' /> 
        /// <devdoc>
        /// Dummy design-time property.
        /// </devdoc>
        [ 
        Category("Data"),
        DefaultValue(DataSourceOperation.Delete), 
        SRDescription(SR.SqlDataSourceDesigner_DeleteQuery), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        Editor(typeof(SqlDataSourceQueryEditor), typeof(UITypeEditor)), 
        MergableProperty(false),
        TypeConverter(typeof(SqlDataSourceQueryConverter)),
        ]
        public DataSourceOperation DeleteQuery { 
            get {
                return DataSourceOperation.Delete; 
            } 
            set {
            } 
        }

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.InsertQuery"]/*' />
        /// <devdoc> 
        /// Dummy design-time property.
        /// </devdoc> 
        [ 
        Category("Data"),
        DefaultValue(DataSourceOperation.Insert), 
        SRDescription(SR.SqlDataSourceDesigner_InsertQuery),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        Editor(typeof(SqlDataSourceQueryEditor), typeof(UITypeEditor)),
        MergableProperty(false), 
        TypeConverter(typeof(SqlDataSourceQueryConverter)),
        ] 
        public DataSourceOperation InsertQuery { 
            get {
                return DataSourceOperation.Insert; 
            }
            set {
            }
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.ProviderName"]/*' /> 
        /// <devdoc> 
        /// Implements the designer's version of the ProviderName property.
        /// This is used to shadow the ProviderName property of the 
        /// runtime control.
        /// </devdoc>
        public string ProviderName {
            get { 
                return SqlDataSource.ProviderName;
            } 
            set { 
                if (value != ProviderName) {
                    SqlDataSource.ProviderName = value; 
                    UpdateDesignTimeHtml();
                    OnDataSourceChanged(EventArgs.Empty);
                }
            } 
        }
 
        /// <devdoc> 
        /// Stores the state of the "Save Configured Connection" checkbox in
        /// the wizard's panel. 
        /// </devdoc>
        internal bool SaveConfiguredConnectionState {
            get {
                object o = DesignerState[DesignerStateSaveConfiguredConnectionStateKey]; 
                if (o == null) {
                    return true; 
                } 
                return (bool)o;
            } 
            set {
                DesignerState[DesignerStateSaveConfiguredConnectionStateKey] = value;
            }
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.SelectCommand"]/*' /> 
        /// <devdoc> 
        /// Implements the designer's version of the SelectCommand property.
        /// This is used to shadow the SelectCommand property of the 
        /// runtime control.
        /// </devdoc>
        public string SelectCommand {
            get { 
                return SqlDataSource.SelectCommand;
            } 
            set { 
                if (value != SelectCommand) {
                    SqlDataSource.SelectCommand = value; 
                    UpdateDesignTimeHtml();
                    OnDataSourceChanged(EventArgs.Empty);
                }
            } 
        }
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.SelectQuery"]/*' /> 
        /// <devdoc>
        /// Dummy design-time property. 
        /// </devdoc>
        [
        Category("Data"),
        DefaultValue(DataSourceOperation.Select), 
        SRDescription(SR.SqlDataSourceDesigner_SelectQuery),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        Editor(typeof(SqlDataSourceQueryEditor), typeof(UITypeEditor)), 
        MergableProperty(false),
        TypeConverter(typeof(SqlDataSourceQueryConverter)), 
        ]
        public DataSourceOperation SelectQuery {
            get {
                return DataSourceOperation.Select; 
            }
            set { 
            } 
        }
 
        /// <devdoc>
        /// The SqlDataSource associated with this designer.
        /// </devdoc>
        internal SqlDataSource SqlDataSource { 
            get {
                return (SqlDataSource)Component; 
            } 
        }
 
        /// <devdoc>
        /// Stores the state of the Configure Data Source wizard's table/field
        /// picker for smart re-entrancy.
        /// </devdoc> 
        internal Hashtable TableQueryState {
            get { 
                return DesignerState[DesignerStateTableQueryStateKey] as Hashtable; 
            }
            set { 
                DesignerState[DesignerStateTableQueryStateKey] = value;
            }
        }
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.UpdateQuery"]/*' />
        /// <devdoc> 
        /// Dummy design-time property. 
        /// </devdoc>
        [ 
        Category("Data"),
        DefaultValue(DataSourceOperation.Update),
        SRDescription(SR.SqlDataSourceDesigner_UpdateQuery),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        Editor(typeof(SqlDataSourceQueryEditor), typeof(UITypeEditor)),
        MergableProperty(false), 
        TypeConverter(typeof(SqlDataSourceQueryConverter)), 
        ]
        public DataSourceOperation UpdateQuery { 
            get {
                return DataSourceOperation.Update;
            }
            set { 
            }
        } 
 
        /// <devdoc>
        /// Builds a select command with associated parameters. 
        /// </devdoc>
        internal DbCommand BuildSelectCommand(DbProviderFactory factory, DbConnection connection, string commandText, ParameterCollection parameters, SqlDataSourceCommandType commandType) {
            //
            DbCommand command = CreateCommand(factory, commandText, connection); 

            // Add parameters, if any 
            if (parameters != null && parameters.Count > 0) { 
                IOrderedDictionary parameterValues = parameters.GetValues(null, null);
                string parameterPrefix = GetParameterPrefix(factory); 
                for (int i = 0; i < parameters.Count; i++ ) {
                    Parameter parameter = parameters[i];
                    DbParameter dbParam = CreateParameter(factory);
                    dbParam.ParameterName = parameterPrefix + parameter.Name; 
                    if (parameter.Type != TypeCode.Empty && parameter.Type != TypeCode.DBNull) {
                        dbParam.DbType = ConvertTypeCodeToDbType(parameter.Type); 
                    } 
                    if (parameter.Type == TypeCode.Empty && ProviderRequiresDbTypeSet(factory)) {
                        // VSWhidbey 493918: ODBC and OLE DB apparently require that the type of the 
                        // parameter be set explicitly, so we just set is to Object.
                        dbParam.DbType = DbType.Object;
                    }
                    dbParam.Value = parameterValues[i]; 
                    if (dbParam.Value == null) {
                        dbParam.Value = DBNull.Value; 
                    } 
                    // For some providers, variable length types require explicit sizes
                    if (parameter.Type == TypeCode.String) { 
                        if ((dbParam.Value is string) && (dbParam.Value != null)) {
                            dbParam.Size = ((string)dbParam.Value).Length;
                        }
                        else { 
                            dbParam.Size = 1;
                        } 
                    } 
                    command.Parameters.Add(dbParam);
                } 
            }

            // Set commmand type
            command.CommandType = GetCommandType(commandType); 

            return command; 
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.Configure"]/*' /> 
        /// <devdoc>
        /// Handles the Configure DataSource designer verb event.
        /// </devdoc>
        public override void Configure() { 
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConfigureDataSourceChangeCallback), null, SR.GetString(SR.DataSource_ConfigureTransactionDescription));
        } 
 
        /// <devdoc>
        /// Transacted change callback to invoke the Configure DataSource wizard. 
        /// </devdoc>
        private bool ConfigureDataSourceChangeCallback(object context) {
            try {
                SuppressDataSourceEvents(); 

                IServiceProvider site = Component.Site; 
 
                IDataEnvironment dataEnvironment = (IDataEnvironment)site.GetService(typeof(IDataEnvironment));
                if (dataEnvironment == null) { 
                    Debug.Fail("Cannot launch Configure DataSource Wizard without IDataEnvironment service");
                    return false;
                }
 
                IDataSourceViewSchema oldViewSchema = GetView(DefaultViewName).Schema;
                bool wasForceUsed = false; 
                if (oldViewSchema == null) { 
                    _forceSchemaRetrieval = true;
                    oldViewSchema = GetView(DefaultViewName).Schema; 
                    _forceSchemaRetrieval = false;
                    if (oldViewSchema != null) {
                        // Only consider it to be a "forced" schema retrieval if we actually got something out of it
                        wasForceUsed = true; 
                    }
                } 
 
                SqlDataSourceWizardForm form = CreateConfigureDataSourceWizardForm(site, dataEnvironment);
                DialogResult result = UIServiceHelper.ShowDialog(site, form); 
                if (result == DialogResult.OK) {
                    // We force this call to make sure that expression-bound properties such as
                    // ConnectionString and ProviderName get updated with their evaluated values.
                    OnComponentChanged(this, new ComponentChangedEventArgs(Component, null, null, null)); 

                    // Compare new schema to old schema and if it changed, raise the SchemaRefreshed event 
                    IDataSourceViewSchema newViewSchema = null; 
                    try {
                        _forceSchemaRetrieval = true; 
                        newViewSchema = GetView(DefaultViewName).Schema;
                    }
                    finally {
                        _forceSchemaRetrieval = false; 
                    }
                    if (!wasForceUsed && !ViewSchemasEquivalent(oldViewSchema, newViewSchema)) { 
                        OnSchemaRefreshed(EventArgs.Empty); 
                    }
                    OnDataSourceChanged(EventArgs.Empty); 
                    return true;
                }
                else {
                    return false; 
                }
            } 
            finally { 
                ResumeDataSourceEvents();
            } 
        }

        /// <devdoc>
        /// Returns true if these two connections are equivalent. Currently the 
        /// check is to make sure they use the exact same connection string and
        /// provider, though this could potentially be improved to check for 
        /// the specific database and server. 
        /// </devdoc>
        internal static bool ConnectionsEqual(DesignerDataConnection connection1, DesignerDataConnection connection2) { 
            Debug.Assert((connection1 != null) || (connection2 != null), "At least one of the connections must be non null");

            // If either connection is null, these are not the same connection
            if ((connection1 == null) || (connection2 == null)) { 
                return false;
            } 
 
            // Compare connection strings
            if (connection1.ConnectionString != connection2.ConnectionString) { 
                return false;
            }

            // Compare provider names, taking into account the default provider 
            string providerName1 = (connection1.ProviderName.Trim().Length == 0 ? DefaultProviderName : connection1.ProviderName);
            string providerName2 = (connection2.ProviderName.Trim().Length == 0 ? DefaultProviderName : connection2.ProviderName); 
            return (providerName1 == providerName2); 
        }
 
        /// <devdoc>
        /// Gets the equivalent TypeCode from a DbType.
        /// </devdoc>
        internal static TypeCode ConvertDbTypeToTypeCode(DbType dbType) { 
            switch (dbType) {
                case DbType.AnsiString: 
                case DbType.AnsiStringFixedLength: 
                case DbType.String:
                case DbType.StringFixedLength: 
                    return TypeCode.String;
                case DbType.Boolean:
                    return TypeCode.Boolean;
                case DbType.Byte: 
                    return TypeCode.Byte;
                case DbType.VarNumeric:     // ??? 
                case DbType.Currency: 
                case DbType.Decimal:
                    return TypeCode.Decimal; 
                case DbType.Date:
                case DbType.DateTime:
                case DbType.Time:
                    return TypeCode.DateTime; 
                case DbType.Double:
                    return TypeCode.Double; 
                case DbType.Int16: 
                    return TypeCode.Int16;
                case DbType.Int32: 
                    return TypeCode.Int32;
                case DbType.Int64:
                    return TypeCode.Int64;
                case DbType.SByte: 
                    return TypeCode.SByte;
                case DbType.Single: 
                    return TypeCode.Single; 
                case DbType.UInt16:
                    return TypeCode.UInt16; 
                case DbType.UInt32:
                    return TypeCode.UInt32;
                case DbType.UInt64:
                    return TypeCode.UInt64; 
                case DbType.Guid:           // ???
                case DbType.Binary: 
                case DbType.Object: 
                default:
                    return TypeCode.Object; 
            }
        }

        /// <devdoc> 
        /// Gets the equivalent DbType from a TypeCode.
        /// </devdoc> 
        internal static DbType ConvertTypeCodeToDbType(TypeCode typeCode) { 
            switch (typeCode) {
                case TypeCode.Boolean: 
                    return DbType.Boolean;
                case TypeCode.Byte:
                    return DbType.Byte;
                case TypeCode.Char: 
                    return DbType.StringFixedLength;    // ???
                case TypeCode.DateTime: 
                    return DbType.DateTime; 
                case TypeCode.Decimal:
                    return DbType.Decimal; 
                case TypeCode.Double:
                    return DbType.Double;
                case TypeCode.Int16:
                    return DbType.Int16; 
                case TypeCode.Int32:
                    return DbType.Int32; 
                case TypeCode.Int64: 
                    return DbType.Int64;
                case TypeCode.SByte: 
                    return DbType.SByte;
                case TypeCode.Single:
                    return DbType.Single;
                case TypeCode.String: 
                    return DbType.String;
                case TypeCode.UInt16: 
                    return DbType.UInt16; 
                case TypeCode.UInt32:
                    return DbType.UInt32; 
                case TypeCode.UInt64:
                    return DbType.UInt64;
                case TypeCode.DBNull:
                case TypeCode.Empty: 
                case TypeCode.Object:
                default: 
                    return DbType.Object; 
            }
        } 

        /// <devdoc>
        /// Copies an ICollection of ICloneable items to another IList.
        /// </devdoc> 
        internal void CopyList(ICollection source, IList dest) {
            dest.Clear(); 
            foreach (ICloneable item in source) { 
                object clonedItem = item.Clone();
                RegisterClone(item, clonedItem); 
                dest.Add(clonedItem);
            }
        }
 
        /// <devdoc>
        /// Creates the appropriate wizard for the Configure Data Source task. 
        /// </devdoc> 
        internal virtual SqlDataSourceWizardForm CreateConfigureDataSourceWizardForm(IServiceProvider serviceProvider, IDataEnvironment dataEnvironment) {
            return new SqlDataSourceWizardForm(serviceProvider, this, dataEnvironment); 
        }

        /// <devdoc>
        /// Creates an DbCommand based on the ProviderName. 
        /// </devdoc>
        internal static DbCommand CreateCommand(DbProviderFactory factory, string commandText, DbConnection connection) { 
            DbCommand command = factory.CreateCommand(); 
            command.CommandText = commandText;
            command.Connection = connection; 
            return command;
        }

        /// <devdoc> 
        /// Creates an DbDataAdapter based on the ProviderName.
        /// </devdoc> 
        internal static DbDataAdapter CreateDataAdapter(DbProviderFactory factory, DbCommand command) { 
            DbDataAdapter dataAdapter = factory.CreateDataAdapter();
            ((IDbDataAdapter)dataAdapter).SelectCommand = command; 
            return dataAdapter;
        }

        /// <devdoc> 
        /// Creates an DbParameter based on the ProviderName.
        /// </devdoc> 
        internal static DbParameter CreateParameter(DbProviderFactory factory) { 
            return factory.CreateParameter();
        } 

        protected virtual SqlDesignerDataSourceView CreateView(string viewName) {
            return new SqlDesignerDataSourceView(this, viewName);
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.DeriveParameters"]/*' /> 
        /// <devdoc> 
        /// Calls the appropriate CommandBuilder (e.g. SqlCommandBuilder) to derive the parameters of a stored procedure.
        /// </devdoc> 
        protected virtual void DeriveParameters(string providerName, DbCommand command) {
            //

            if (String.Equals(providerName, "System.Data.Odbc", StringComparison.OrdinalIgnoreCase)) { 
                System.Data.Odbc.OdbcCommandBuilder.DeriveParameters((System.Data.Odbc.OdbcCommand)command);
            } 
            else { 
                if (String.Equals(providerName, "System.Data.OleDb", StringComparison.OrdinalIgnoreCase)) {
                    System.Data.OleDb.OleDbCommandBuilder.DeriveParameters((System.Data.OleDb.OleDbCommand)command); 
                }
                else {
                    if (String.Equals(providerName, "System.Data.SqlClient", StringComparison.OrdinalIgnoreCase) ||
                        String.IsNullOrEmpty(providerName)) { 
                        System.Data.SqlClient.SqlCommandBuilder.DeriveParameters((System.Data.SqlClient.SqlCommand)command);
                    } 
                    else { 
                        UIServiceHelper.ShowError(
                            SqlDataSource.Site, 
                            SR.GetString(SR.SqlDataSourceDesigner_InferStoredProcedureNotSupported, providerName));
                    }
                }
            } 
        }
 
        /// <devdoc> 
        /// Converts a SqlDataSourceCommandType to a System.Data.CommandType.
        /// </devdoc> 
        private static CommandType GetCommandType(SqlDataSourceCommandType commandType) {
            if (commandType == SqlDataSourceCommandType.Text) {
                return CommandType.Text;
            } 
            return CommandType.StoredProcedure;
        } 
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.GetConnectionString"]/*' />
        /// <devdoc> 
        /// Gets the data source's connection string. Override this if the data source
        /// has to perform any additional operations in order for the connection string
        /// to be retrieved. By default this method returns the runtime control's
        /// connection string. 
        /// </devdoc>
        protected virtual string GetConnectionString() { 
            return SqlDataSource.ConnectionString; 
        }
 
        /// <devdoc>
        /// Gets the DbProviderFactory associated with the provider type specified in the ProviderName property.
        /// If no provider is specified, the System.Data.SqlClient factory is used.
        /// </devdoc> 
        internal static DbProviderFactory GetDbProviderFactory(string providerName) {
            // Default to SQL provider 
            if (providerName.Length == 0) { 
                providerName = DefaultProviderName;
            } 
            DbProviderFactory factory = DbProviderFactories.GetFactory(providerName);
            Debug.Assert(factory != null);
            return factory;
        } 

        /// <devdoc> 
        /// Gets a design-time version of a given connection. This might 
        /// involve using an alternate set of credentials or mapping paths
        /// in the connection string such that they are valid at design time. 
        /// </devdoc>
        internal static DbConnection GetDesignTimeConnection(IServiceProvider serviceProvider, DesignerDataConnection connection) {
            if (serviceProvider != null) {
                IDataEnvironment de = (IDataEnvironment)serviceProvider.GetService(typeof(IDataEnvironment)); 
                if (de != null) {
                    if (String.IsNullOrEmpty(connection.ProviderName)) { 
                        connection = new DesignerDataConnection(connection.Name, DefaultProviderName, connection.ConnectionString); 
                    }
                    return de.GetDesignTimeConnection(connection); 
                }
            }
            return null;
        } 

        public override DesignerDataSourceView GetView(string viewName) { 
            if (String.IsNullOrEmpty(viewName)) { 
                viewName = DefaultViewName;
            } 
            if (String.Equals(viewName, DefaultViewName, StringComparison.OrdinalIgnoreCase)) {
                if (_view == null) {
                    _view = CreateView(viewName);
                } 
                return _view;
            } 
            return null; 
        }
 
        public override string[] GetViewNames() {
            return new string[] { DefaultViewName };
        }
 
        /// <devdoc>
        /// Indicates the prefix for parameter placeholders (as used in 
        /// DbCommand.CommandText property). 
        /// </devdoc>
        internal static string GetParameterPlaceholderPrefix(DbProviderFactory factory) { 
            if (factory == null) {
                throw new ArgumentNullException("factory");
            }
            // 
            if (factory == System.Data.SqlClient.SqlClientFactory.Instance) {
                return "@"; 
            } 
            else {
                if (factory == System.Data.OracleClient.OracleClientFactory.Instance) { 
                    return ":";
                }
                else {
                    return "?"; 
                }
            } 
        } 

        /// <devdoc> 
        /// Indicates the prefix for parameters (as used in the
        /// DbCommand.Parameters collection).
        /// </devdoc>
        internal static string GetParameterPrefix(DbProviderFactory factory) { 
            if (factory == null) {
                throw new ArgumentNullException("factory"); 
            } 
            //
            if (factory == System.Data.SqlClient.SqlClientFactory.Instance) { 
                return "@";
            }
            else {
                return String.Empty; 
            }
        } 
 
        /// <devdoc>
        /// Returns an array of all known parameter prefixes. 
        /// </devdoc>
        private static string[] GetParameterPrefixes() {
            return new string[] { "@", "?", ":" };
        } 

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.InferParameterNames"]/*' /> 
        /// <devdoc> 
        /// Gets an array of Parameter objects from a SqlDataSource's command.
        /// If the command text indicates a stored procedure, a call will be made to the server 
        /// to get the parameter types. Otherwise a SQL parser is used to extract the parameter
        /// names.
        /// </devdoc>
        protected internal virtual Parameter[] InferParameterNames(DesignerDataConnection connection, string commandText, SqlDataSourceCommandType commandType) { 
            Cursor originalCursor = Cursor.Current;
            try { 
                Cursor.Current = Cursors.WaitCursor; 

                if (commandText.Length == 0) { 
                    UIServiceHelper.ShowError(
                        SqlDataSource.Site,
                        SR.GetString(SR.SqlDataSourceDesigner_NoCommand));
                    return null; 
                }
 
                if (commandType == SqlDataSourceCommandType.Text) { 
                    // Command text
                    return SqlDataSourceParameterParser.ParseCommandText(connection.ProviderName, commandText); 
                }
                else {
                    // Stored procedure
                    DbProviderFactory factory = GetDbProviderFactory(connection.ProviderName); 
                    DbConnection conn = null;
                    try { 
                        conn = GetDesignTimeConnection(Component.Site, connection); 
                    }
                    catch (Exception ex) { 
                        if (conn == null) {
                            UIServiceHelper.ShowError(
                                SqlDataSource.Site,
                                ex, 
                                SR.GetString(SR.SqlDataSourceDesigner_CouldNotCreateConnection));
                            return null; 
                        } 
                    }
 
                    if (conn == null) {
                        UIServiceHelper.ShowError(
                            SqlDataSource.Site,
                            SR.GetString(SR.SqlDataSourceDesigner_CouldNotCreateConnection)); 
                        return null;
                    } 
                    DbCommand command = BuildSelectCommand(factory, conn, commandText, null, commandType); 
                    command.CommandType = CommandType.StoredProcedure;
                    try { 
                        Debug.Assert(conn.State == ConnectionState.Open, "Expected connection state to be open - IDataEnvironment.GetDesignTimeConnection() should have done this");
                        DeriveParameters(connection.ProviderName, command);
                    }
                    catch (Exception ex) { 
                        // If there were any errors in deriving the parameters, abort
                        // the entire operation. 
                        UIServiceHelper.ShowError( 
                            SqlDataSource.Site,
                            SR.GetString(SR.SqlDataSourceDesigner_InferStoredProcedureError, ex.Message)); 

                        return null;
                    }
                    finally { 
                        if (command.Connection.State == ConnectionState.Open) {
                            conn.Close(); 
                        } 
                    }
 
                    int paramCount = command.Parameters.Count;
                    Parameter[] derivedParameters = new Parameter[paramCount];
                    for (int i = 0; i < paramCount; i++) {
                        IDataParameter parameter = command.Parameters[i] as IDataParameter; 
                        if (parameter != null) {
                            // Trim parameter prefix if present 
                            string paramName = StripParameterPrefix(parameter.ParameterName); 
                            // Convert type from DB type to CLR type
                            TypeCode type = ConvertDbTypeToTypeCode(parameter.DbType); 
                            derivedParameters[i] = new Parameter(paramName, type);
                            derivedParameters[i].Direction = parameter.Direction;
                        }
                        else { 
                            //
                            Debug.Fail("Parameter is not an IDataParameter"); 
                        } 
                    }
                    return derivedParameters; 
                }
            }
            finally {
                Cursor.Current = originalCursor; 
            }
        } 
 
        /// <devdoc>
        /// Attempts to load the schema for this SqlDataSource. If the 
        /// schema is not consistent with the current properties, then it is
        /// removed from state.
        /// </devdoc>
        internal DataTable LoadSchema() { 
            if (!_forceSchemaRetrieval) {
                // Only check for consistency if we are not forcing the retrieval 
                object connectionStringHash = DesignerState[DesignerStateDataSourceSchemaConnectionStringHashKey]; 
                string providerName = DesignerState[DesignerStateDataSourceSchemaProviderNameKey] as string;
                string selectCommand = DesignerState[DesignerStateDataSourceSchemaSelectCommandKey] as string; 
                if (String.IsNullOrEmpty(providerName)) {
                    providerName = DefaultProviderName;
                }
 
                if (String.IsNullOrEmpty(ConnectionString)) {
                    // If there is no connection string, we definitely don't have any schema 
                    return null; 
                }
 
                DesignerDataConnection oldConnection = new DesignerDataConnection(String.Empty, ProviderName, ConnectionString);

                string oldConnectionString = oldConnection.ConnectionString;
                int oldConnectionStringHash = oldConnectionString.GetHashCode(); 
                string oldProviderName = oldConnection.ProviderName;
                string oldSelectCommand = SelectCommand; 
                if (String.IsNullOrEmpty(oldProviderName)) { 
                    oldProviderName = DefaultProviderName;
                } 

                if ((connectionStringHash == null) ||
                    ((int)connectionStringHash != oldConnectionStringHash) ||
                    (!String.Equals(providerName, oldProviderName, StringComparison.OrdinalIgnoreCase)) || 
                    (!String.Equals(selectCommand, oldSelectCommand, StringComparison.Ordinal))) {
 
                    // The schema is not consistent with the current properties, return nothing 
                    return null;
                } 
            }

            // Either we are forcing schema retrieval, or we're not forcing but we're consistent, so get the schema
            DataTable schema = DesignerState[DesignerStateDataSourceSchemaKey] as DataTable; 
            if (schema != null) {
                schema.TableName = DefaultViewName; 
                return schema; 
            }
            else { 
                return null;
            }
        }
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        /// Overridden by the designer to shadow various runtime properties 
        /// with corresponding properties that it implements.
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);

            PropertyDescriptor property; 

            // Hide runtime properties 
            foreach (string propertyName in _hiddenProperties) { 
                property = (PropertyDescriptor)properties[propertyName];
                if (property != null) { 
                    properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                }
            }
 
            // Add design-time properties
            properties["DeleteQuery"] = TypeDescriptor.CreateProperty( 
                                            GetType(), 
                                            "DeleteQuery",
                                            typeof(DataSourceOperation)); 
            properties["InsertQuery"] = TypeDescriptor.CreateProperty(
                                            GetType(),
                                            "InsertQuery",
                                            typeof(DataSourceOperation)); 
            properties["SelectQuery"] = TypeDescriptor.CreateProperty(
                                            GetType(), 
                                            "SelectQuery", 
                                            typeof(DataSourceOperation));
            properties["UpdateQuery"] = TypeDescriptor.CreateProperty( 
                                            GetType(),
                                            "UpdateQuery",
                                            typeof(DataSourceOperation));
 
            // Shadow runtime ConnectionString property
            property = (PropertyDescriptor)properties["ConnectionString"]; 
            Debug.Assert(property != null); 
            properties["ConnectionString"] = TypeDescriptor.CreateProperty(this.GetType(), property, new Attribute[0]);
 
            // Shadow runtime ProviderName property
            property = (PropertyDescriptor)properties["ProviderName"];
            Debug.Assert(property != null);
            properties["ProviderName"] = TypeDescriptor.CreateProperty(this.GetType(), property, new Attribute[0]); 

            // Shadow runtime SelectCommand property 
            property = (PropertyDescriptor)properties["SelectCommand"]; 
            Debug.Assert(property != null);
            properties["SelectCommand"] = TypeDescriptor.CreateProperty(this.GetType(), property, BrowsableAttribute.No); 
        }

        /// <devdoc>
        /// Returns true is the provider requires that the DbType property of a DbParameter 
        /// be set explicitly.
        /// </devdoc> 
        private static bool ProviderRequiresDbTypeSet(DbProviderFactory factory) { 
            return (factory == System.Data.OleDb.OleDbFactory.Instance ||
                    factory == System.Data.Odbc.OdbcFactory.Instance); 
        }

        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.RefreshSchema"]/*' />
        /// <devdoc> 
        /// Refreshes the data source's schema. Override this method to perform whatever actions are necessary
        /// for the Schema property to return correct schema. 
        /// </devdoc> 
        public override void RefreshSchema(bool preferSilent) {
            try { 
                SuppressDataSourceEvents();

                bool success = false;
 
                IServiceProvider site = SqlDataSource.Site;
 
                // If the data source is not yet set up, abort the Refresh Schema operation 
                if (!CanRefreshSchema) {
                    if (!preferSilent) { 
                        UIServiceHelper.ShowError(
                            site,
                            SR.GetString(SR.SqlDataSourceDesigner_RefreshSchemaRequiresSettings));
                    } 
                    return;
                } 
 
                IDataSourceViewSchema oldViewSchema = GetView(DefaultViewName).Schema;
                bool wasForceUsed = false; 
                if (oldViewSchema == null) {
                    _forceSchemaRetrieval = true;
                    oldViewSchema = GetView(DefaultViewName).Schema;
                    _forceSchemaRetrieval = false; 
                    wasForceUsed = true;
                } 
 
                DesignerDataConnection runtimeConnection = new DesignerDataConnection(String.Empty, ProviderName, ConnectionString);
 
                if (preferSilent) {
                    // Silent mode - just get schema directly, and ignore errors
                    success = RefreshSchema(runtimeConnection, SelectCommand, SqlDataSource.SelectCommandType, SqlDataSource.SelectParameters, true);
                } 
                else {
                    // Non-silent mode - show UI to get parameter information 
                    // Infer parameters 
                    Parameter[] inferedParameters = InferParameterNames(runtimeConnection, SelectCommand, SqlDataSource.SelectCommandType);
 
                    // A null return value implies there was a problem retrieving parameters
                    if (inferedParameters == null) {
                        return;
                    } 

                    ParameterCollection inputParameters = new ParameterCollection(); 
 
                    ParameterCollection existingParameters = new ParameterCollection();
                    foreach (ICloneable param in SqlDataSource.SelectParameters) { 
                        existingParameters.Add((Parameter)param.Clone());
                    }

                    // Filter out all non-input parameters 
                    foreach (Parameter inferedParameter in inferedParameters) {
                        if ((inferedParameter.Direction == ParameterDirection.Input) || 
                            (inferedParameter.Direction == ParameterDirection.InputOutput)) { 

                            // Try to match this parameter with an existing parameter so we 
                            // can automatically pick up on a previously set default value
                            // or type.
                            Parameter existingParameter = existingParameters[inferedParameter.Name];
                            if (existingParameter != null) { 
                                inferedParameter.DefaultValue = existingParameter.DefaultValue;
                                if (inferedParameter.Type == TypeCode.Empty) { 
                                    inferedParameter.Type = existingParameter.Type; 
                                }
                                existingParameters.Remove(existingParameter); 
                            }
                            inputParameters.Add(inferedParameter);
                        }
                    } 

                    if (inputParameters.Count > 0) { 
                        SqlDataSourceRefreshSchemaForm form = new SqlDataSourceRefreshSchemaForm(site, this, inputParameters); 
                        DialogResult result = UIServiceHelper.ShowDialog(site, form);
                        success = (result == DialogResult.OK); 
                    }
                    else {
                        success = RefreshSchema(runtimeConnection, SelectCommand, SqlDataSource.SelectCommandType, inputParameters, false);
                    } 
                }
 
                if (success) { 
                    // Compare new schema to old schema and if it changed, raise the SchemaRefreshed event
                    IDataSourceViewSchema newViewSchema = GetView(DefaultViewName).Schema; 
                    if (wasForceUsed && ViewSchemasEquivalent(oldViewSchema, newViewSchema)) {
                        OnDataSourceChanged(EventArgs.Empty);
                    }
                    else { 
                        if (!ViewSchemasEquivalent(oldViewSchema, newViewSchema)) {
                            OnSchemaRefreshed(EventArgs.Empty); 
                        } 
                    }
                } 
            }
            finally {
                ResumeDataSourceEvents();
            } 
        }
 
        /// <include file='doc\SqlDataSourceDesigner.uex' path='docs/doc[@for="SqlDataSourceDesigner.RefreshSchema1"]/*' /> 
        /// <devdoc>
        /// Refreshes the schema of the data source. 
        /// </devdoc>
        internal bool RefreshSchema(DesignerDataConnection connection, string commandText, SqlDataSourceCommandType commandType, ParameterCollection parameters, bool preferSilent) {
            IServiceProvider site = SqlDataSource.Site;
 
            // Get schema from database
            DbCommand selectCommand = null; 
 
            try {
                DbProviderFactory factory = GetDbProviderFactory(connection.ProviderName); 
                DbConnection conn = GetDesignTimeConnection(Component.Site, connection);
                if (conn == null) {
                    if (!preferSilent) {
                        UIServiceHelper.ShowError( 
                            SqlDataSource.Site,
                            SR.GetString(SR.SqlDataSourceDesigner_CouldNotCreateConnection)); 
                    } 
                    return false;
                } 
                selectCommand = BuildSelectCommand(factory, conn, commandText, parameters, commandType);
                DbDataAdapter adapter = CreateDataAdapter(factory, selectCommand);
                adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
 
                DataSet dataSet = new DataSet();
 
                adapter.FillSchema(dataSet, SchemaType.Source, DefaultViewName); 

                // Some providers, such as Microsoft JET, do not necessarily throw an exception 
                // when they are unable to retrieve schema. Instead, they silently fail, so we
                // have to check if they returned a schema table.
                DataTable schemaTable = dataSet.Tables[DefaultViewName];
                if (schemaTable == null) { 
                    if (!preferSilent) {
                        UIServiceHelper.ShowError(site, SR.GetString(SR.SqlDataSourceDesigner_CannotGetSchema)); 
                    } 
                    return false;
                } 

                // Save the schema using the state service
                SaveSchema(connection, commandText, schemaTable);
 
                return true;
            } 
            catch (Exception ex) { 
                if (!preferSilent) {
                    UIServiceHelper.ShowError(site, ex, SR.GetString(SR.SqlDataSourceDesigner_CannotGetSchema)); 
                }
            }
            finally {
                if (selectCommand != null && selectCommand.Connection.State == ConnectionState.Open) { 
                    selectCommand.Connection.Close();
                } 
            } 
            return false;
        } 

        /// <devdoc>
        /// Saves schema using the DesignerState. Along with the schema are
        /// stored the type and method used to generate the schema so that we 
        /// can make sure the schema is consistent.
        /// </devdoc> 
        private void SaveSchema(DesignerDataConnection connection, string selectCommand, DataTable schemaTable) { 
            DesignerState[DesignerStateDataSourceSchemaKey] = schemaTable;
            DesignerState[DesignerStateDataSourceSchemaConnectionStringHashKey] = connection.ConnectionString.GetHashCode(); 
            DesignerState[DesignerStateDataSourceSchemaProviderNameKey] = connection.ProviderName;
            DesignerState[DesignerStateDataSourceSchemaSelectCommandKey] = selectCommand;
        }
 
        /// <devdoc>
        /// Strips all known parameter prefixes from a parameter name. 
        /// </devdoc> 
        internal static string StripParameterPrefix(string parameterName) {
            foreach (string prefix in GetParameterPrefixes()) { 
                if (parameterName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                    return parameterName.Substring(prefix.Length);
                }
            } 
            return parameterName;
        } 
 
        /// <devdoc>
        /// Indicates whether the provider supports named parameters. 
        /// </devdoc>
        internal static bool SupportsNamedParameters(DbProviderFactory factory) {
            if (factory == null) {
                throw new ArgumentNullException("factory"); 
            }
            // 
            if ((factory == System.Data.SqlClient.SqlClientFactory.Instance) || 
                (factory == System.Data.OracleClient.OracleClientFactory.Instance)) {
                return true; 
            }
            else {
                return false;
            } 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
