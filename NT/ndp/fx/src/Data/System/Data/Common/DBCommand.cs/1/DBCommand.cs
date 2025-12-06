//------------------------------------------------------------------------------ 
// <copyright file="DbCommand.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    using System;
    using System.ComponentModel;
    using System.Data;
 
#if WINFSInternalOnly
    internal 
#else 
    public
#endif 
    abstract class DbCommand : Component, IDbCommand { // V1.2.3300
        protected DbCommand() : base() {
        }
 
        [
        DefaultValue(""), 
        RefreshProperties(RefreshProperties.All), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_CommandText), 
        ]
        abstract public string CommandText {
            get;
            set; 
        }
 
        [ 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_CommandTimeout), 
        ]
        abstract public int CommandTimeout {
            get;
            set; 
        }
 
        [ 
        DefaultValue(System.Data.CommandType.Text),
        RefreshProperties(RefreshProperties.All), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_CommandType),
        ]
        abstract public CommandType CommandType { 
            get;
            set; 
        } 

        [ 
        Browsable(false),
        DefaultValue(null),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResCategoryAttribute(Res.DataCategory_Data), 
        ResDescriptionAttribute(Res.DbCommand_Connection),
        ] 
        public DbConnection Connection { 
            get {
                return DbConnection; 
            }
            set {
                DbConnection = value;
            } 
        }
 
        IDbConnection IDbCommand.Connection { 
            get {
                return DbConnection; 
            }
            set {
                DbConnection = (DbConnection)value;
            } 
        }
 
        abstract protected DbConnection DbConnection { // V1.2.3300 
            get;
            set; 
        }

        abstract protected DbParameterCollection DbParameterCollection { // V1.2.3300
            get; 
        }
 
        abstract protected DbTransaction DbTransaction { // V1.2.3300 
            get;
            set; 
        }

        // @devnote: By default, the cmd object is visible on the design surface (i.e. VS7 Server Tray)
        // to limit the number of components that clutter the design surface, 
        // when the DataAdapter design wizard generates the insert/update/delete commands it will
        // set the DesignTimeVisible property to false so that cmds won't appear as individual objects 
        [ 
        DefaultValue(true),
        DesignOnly(true), 
        Browsable(false),
        EditorBrowsableAttribute(EditorBrowsableState.Never),
        ]
        public abstract bool DesignTimeVisible { 
            get;
            set; 
        } 

        [ 
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_Parameters), 
        ]
        public DbParameterCollection Parameters { 
            get { 
                return DbParameterCollection;
            } 
        }

        IDataParameterCollection IDbCommand.Parameters {
            get { 
                return (DbParameterCollection)DbParameterCollection;
            } 
        } 

        [ 
        Browsable(false),
        DefaultValue(null),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResDescriptionAttribute(Res.DbCommand_Transaction), 
        ]
        public DbTransaction Transaction { 
            get { 
                return DbTransaction;
            } 
            set {
                DbTransaction = value;
            }
        } 

        IDbTransaction IDbCommand.Transaction { 
            get { 
                return DbTransaction;
            } 
            set {
                DbTransaction = (DbTransaction)value;
            }
        } 

        [ 
        DefaultValue(System.Data.UpdateRowSource.Both), 
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbCommand_UpdatedRowSource), 
        ]
        abstract public UpdateRowSource UpdatedRowSource {
            get;
            set; 
        }
 
        abstract public void Cancel(); 

        public DbParameter CreateParameter(){ // V1.2.3300 
            return CreateDbParameter();
        }

        IDbDataParameter IDbCommand.CreateParameter() { // V1.2.3300 
            return CreateDbParameter();
        } 
 
        abstract protected DbParameter CreateDbParameter();
 
        abstract protected DbDataReader ExecuteDbDataReader(CommandBehavior behavior);

        abstract public int ExecuteNonQuery();
 
        public DbDataReader ExecuteReader() {
            return (DbDataReader)ExecuteDbDataReader(CommandBehavior.Default); 
        } 

        IDataReader IDbCommand.ExecuteReader() { 
            return (DbDataReader)ExecuteDbDataReader(CommandBehavior.Default);
        }

        public DbDataReader ExecuteReader(CommandBehavior behavior){ 
            return (DbDataReader)ExecuteDbDataReader(behavior);
        } 
 
        IDataReader IDbCommand.ExecuteReader(CommandBehavior behavior) {
            return (DbDataReader)ExecuteDbDataReader(behavior); 
        }

        abstract public object ExecuteScalar();
 
        abstract public void Prepare();
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbCommand.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    using System;
    using System.ComponentModel;
    using System.Data;
 
#if WINFSInternalOnly
    internal 
#else 
    public
#endif 
    abstract class DbCommand : Component, IDbCommand { // V1.2.3300
        protected DbCommand() : base() {
        }
 
        [
        DefaultValue(""), 
        RefreshProperties(RefreshProperties.All), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_CommandText), 
        ]
        abstract public string CommandText {
            get;
            set; 
        }
 
        [ 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_CommandTimeout), 
        ]
        abstract public int CommandTimeout {
            get;
            set; 
        }
 
        [ 
        DefaultValue(System.Data.CommandType.Text),
        RefreshProperties(RefreshProperties.All), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_CommandType),
        ]
        abstract public CommandType CommandType { 
            get;
            set; 
        } 

        [ 
        Browsable(false),
        DefaultValue(null),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResCategoryAttribute(Res.DataCategory_Data), 
        ResDescriptionAttribute(Res.DbCommand_Connection),
        ] 
        public DbConnection Connection { 
            get {
                return DbConnection; 
            }
            set {
                DbConnection = value;
            } 
        }
 
        IDbConnection IDbCommand.Connection { 
            get {
                return DbConnection; 
            }
            set {
                DbConnection = (DbConnection)value;
            } 
        }
 
        abstract protected DbConnection DbConnection { // V1.2.3300 
            get;
            set; 
        }

        abstract protected DbParameterCollection DbParameterCollection { // V1.2.3300
            get; 
        }
 
        abstract protected DbTransaction DbTransaction { // V1.2.3300 
            get;
            set; 
        }

        // @devnote: By default, the cmd object is visible on the design surface (i.e. VS7 Server Tray)
        // to limit the number of components that clutter the design surface, 
        // when the DataAdapter design wizard generates the insert/update/delete commands it will
        // set the DesignTimeVisible property to false so that cmds won't appear as individual objects 
        [ 
        DefaultValue(true),
        DesignOnly(true), 
        Browsable(false),
        EditorBrowsableAttribute(EditorBrowsableState.Never),
        ]
        public abstract bool DesignTimeVisible { 
            get;
            set; 
        } 

        [ 
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_Parameters), 
        ]
        public DbParameterCollection Parameters { 
            get { 
                return DbParameterCollection;
            } 
        }

        IDataParameterCollection IDbCommand.Parameters {
            get { 
                return (DbParameterCollection)DbParameterCollection;
            } 
        } 

        [ 
        Browsable(false),
        DefaultValue(null),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ResDescriptionAttribute(Res.DbCommand_Transaction), 
        ]
        public DbTransaction Transaction { 
            get { 
                return DbTransaction;
            } 
            set {
                DbTransaction = value;
            }
        } 

        IDbTransaction IDbCommand.Transaction { 
            get { 
                return DbTransaction;
            } 
            set {
                DbTransaction = (DbTransaction)value;
            }
        } 

        [ 
        DefaultValue(System.Data.UpdateRowSource.Both), 
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbCommand_UpdatedRowSource), 
        ]
        abstract public UpdateRowSource UpdatedRowSource {
            get;
            set; 
        }
 
        abstract public void Cancel(); 

        public DbParameter CreateParameter(){ // V1.2.3300 
            return CreateDbParameter();
        }

        IDbDataParameter IDbCommand.CreateParameter() { // V1.2.3300 
            return CreateDbParameter();
        } 
 
        abstract protected DbParameter CreateDbParameter();
 
        abstract protected DbDataReader ExecuteDbDataReader(CommandBehavior behavior);

        abstract public int ExecuteNonQuery();
 
        public DbDataReader ExecuteReader() {
            return (DbDataReader)ExecuteDbDataReader(CommandBehavior.Default); 
        } 

        IDataReader IDbCommand.ExecuteReader() { 
            return (DbDataReader)ExecuteDbDataReader(CommandBehavior.Default);
        }

        public DbDataReader ExecuteReader(CommandBehavior behavior){ 
            return (DbDataReader)ExecuteDbDataReader(behavior);
        } 
 
        IDataReader IDbCommand.ExecuteReader(CommandBehavior behavior) {
            return (DbDataReader)ExecuteDbDataReader(behavior); 
        }

        abstract public object ExecuteScalar();
 
        abstract public void Prepare();
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
