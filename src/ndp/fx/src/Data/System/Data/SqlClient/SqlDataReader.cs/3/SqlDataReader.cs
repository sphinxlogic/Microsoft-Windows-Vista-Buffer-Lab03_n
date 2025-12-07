//------------------------------------------------------------------------------ 
// <copyright file="SqlDataReader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="true" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.SqlClient { 
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel; 
    using System.Data;
    using System.Data.Sql; 
    using System.Data.SqlTypes; 
    using System.Data.Common;
    using System.Data.ProviderBase; 
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection; 
    using System.Runtime.CompilerServices;
    using System.Threading; 
    using System.Xml; 

    using Microsoft.SqlServer.Server; 

#if WINFSInternalOnly
    internal
#else 
    public
#endif 
    class SqlDataReader : DbDataReader, IDataReader { 

        private enum ALTROWSTATUS { 
            Null = 0,           // default and after Done
            AltRow,             // after calling NextResult and the first AltRow is available for read
            Done,               // after consuming the value (GetValue -> GetValueInternal)
        } 

        private TdsParser                      _parser;                 // 
        private TdsParserStateObject           _stateObj; 
        private SqlCommand                     _command;
        private SqlConnection                  _connection; 
        private int                            _defaultLCID;
        private bool                           _dataReady;              // ready to ProcessRow
        private bool                           _haltRead;               // bool to denote whether we have read first row for single row behavior
        private bool                           _metaDataConsumed; 
        private bool                           _browseModeInfoConsumed;
        private bool                           _isClosed; 
        private bool                           _isInitialized;          // Webdata 104560 
        private bool                           _hasRows;
        private ALTROWSTATUS                   _altRowStatus; 
        private int                            _recordsAffected = -1;
        private int                            _timeoutSeconds;
        private SqlConnectionString.TypeSystem _typeSystem;
 
        // SQLStatistics support
        private SqlStatistics   _statistics; 
        private SqlBuffer[]     _data;         // row buffer, filled in by ReadColumnData() 
        private SqlStreamingXml _streamingXml; // Used by Getchars on an Xml column for sequential access
 
        // buffers and metadata
        private _SqlMetaDataSet           _metaData;                 // current metaData for the stream, it is lazily loaded
        private _SqlMetaDataSetCollection _altMetaDataSetCollection;
        private FieldNameLookup           _fieldNameLookup; 
        private CommandBehavior           _commandBehavior;
 
        private  static int   _objectTypeCount; // Bid counter 
        internal readonly int ObjectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);
 
        // context
        // undone: we may still want to do this...it's nice to pass in an lpvoid (essentially) and just have the reader keep the state
        // private object _context = null; // this is never looked at by the stream object.  It is used by upper layers who wish
        // to remain stateless 

        // metadata (no explicit table, use 'Table') 
        private MultiPartTableName[] _tableNames = null; 
        private string               _resetOptionsString;
 
        private int    _nextColumnDataToRead;
        private int    _nextColumnHeaderToRead;
        private long   _columnDataBytesRead;       // last byte read by user
        private long   _columnDataBytesRemaining; 
        private long   _columnDataCharsRead;       // last char read by user
        private char[] _columnDataChars; 
 
        // handle exceptions that occur when reading a value mid-row
        private Exception _rowException; 

        internal SqlDataReader(SqlCommand command, CommandBehavior behavior) {
            SqlConnection.VerifyExecutePermission();
 
            _command = command;
            _commandBehavior = behavior; 
            if (_command != null) { 
                _timeoutSeconds = command.CommandTimeout;
                _connection = command.Connection; 
                if (_connection != null) {
                    _statistics = _connection.Statistics;
                    _typeSystem = _connection.TypeSystem;
                } 
            }
            _dataReady = false; 
            _metaDataConsumed = false; 
            _hasRows = false;
            _browseModeInfoConsumed = false; 
        }

        internal bool BrowseModeInfoConsumed {
            set { 
                _browseModeInfoConsumed = value;
            } 
        } 

        internal SqlCommand Command { 
            get {
                return _command;
            }
        } 

        protected SqlConnection Connection { 
            get { 
                return _connection;
            } 
        }

        override public int Depth {
            get { 
                if (this.IsClosed) {
                    throw ADP.DataReaderClosed("Depth"); 
                } 

                return 0; 
            }
        }

        // fields/attributes collection 
        override public int FieldCount {
            get { 
                if (this.IsClosed) { 
                    throw ADP.DataReaderClosed("FieldCount");
                } 

                if (MetaData == null) {
                    return 0;
                } 

                return _metaData.Length; 
            } 
        }
 
        override public bool HasRows {
            get {
                if (this.IsClosed) {
                    throw ADP.DataReaderClosed("HasRows"); 
                }
 
                return _hasRows; 
            }
        } 

        override public bool IsClosed {
            get {
                return _isClosed; 
            }
        } 
 
        internal bool IsInitialized {
            get { 
                return _isInitialized;
            }
            set {
                Debug.Assert(value, "attempting to uninitialize a data reader?"); 
                _isInitialized = value;
            } 
        } 

        internal _SqlMetaDataSet MetaData { 
            get {
                if (IsClosed) {
                    throw ADP.DataReaderClosed("MetaData");
                } 
                // metaData comes in pieces: colmetadata, tabname, colinfo, etc
                // if we have any metaData, return it.  If we have none, 
                // then fetch it 
                if (_metaData == null && !_metaDataConsumed) {
                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try {
#if DEBUG
                        object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try { 
                            Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                            ConsumeMetaData(); 
#if DEBUG
                        }
                        finally {
                            Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                        }
#endif //DEBUG 
                    } 
                    catch (System.OutOfMemoryException e) {
                        _isClosed = true; 
                        if (null != _connection) {
                            _connection.Abort(e);
                        }
                        throw; 
                    }
                    catch (System.StackOverflowException e) { 
                        _isClosed = true; 
                        if (null != _connection) {
                            _connection.Abort(e); 
                        }
                        throw;
                    }
                    catch (System.Threading.ThreadAbortException e)  { 
                        _isClosed = true;
                        if (null != _connection) { 
                            _connection.Abort(e); 
                        }
                        throw; 
                    }
                }
                return _metaData;
            } 
        }
 
        internal virtual SmiExtendedMetaData[] GetInternalSmiMetaData() { 
            SmiExtendedMetaData[] metaDataReturn = null;
            _SqlMetaDataSet metaData = this.MetaData; 

            if ( null != metaData && 0 < metaData.Length ) {
                metaDataReturn = new SmiExtendedMetaData[metaData.visibleColumns];
 
                for( int index=0; index < metaData.Length; index++ ) {
                    _SqlMetaData colMetaData = metaData[index]; 
 
                    if ( !colMetaData.isHidden ) {
                        SqlCollation collation = colMetaData.collation; 

                        string typeSpecificNamePart1 = null;
                        string typeSpecificNamePart2 = null;
                        string typeSpecificNamePart3 = null; 

                        if (SqlDbType.Xml == colMetaData.type) { 
                            typeSpecificNamePart1 = colMetaData.xmlSchemaCollectionDatabase; 
                            typeSpecificNamePart2 = colMetaData.xmlSchemaCollectionOwningSchema;
                            typeSpecificNamePart3 = colMetaData.xmlSchemaCollectionName; 
                        }
                        else if (SqlDbType.Udt == colMetaData.type) {
                            SqlConnection.CheckGetExtendedUDTInfo(colMetaData, true);    //
 
                            typeSpecificNamePart1 = colMetaData.udtDatabaseName;
                            typeSpecificNamePart2 = colMetaData.udtSchemaName; 
                            typeSpecificNamePart3 = colMetaData.udtTypeName; 
                        }
 
                        int length = colMetaData.length;
                        if ( length > TdsEnums.MAXSIZE ) {
                            length = (int) SmiMetaData.UnlimitedMaxLengthIndicator;
                        } 
                        else if (SqlDbType.NChar == colMetaData.type
                                ||SqlDbType.NVarChar == colMetaData.type) { 
                            length /= ADP.CharSize; 
                        }
 
                        metaDataReturn[index] = new SmiQueryMetaData(
                                                        colMetaData.type,
                                                        length,
                                                        colMetaData.precision, 
                                                        colMetaData.scale,
                                                        (null != collation) ? collation.LCID : _defaultLCID, 
                                                        (null != collation) ? collation.SqlCompareOptions : SqlCompareOptions.None, 
                                                        colMetaData.udtType,
                                                        false,  // isMultiValued 
                                                        null,   // fieldmetadata
                                                        null,   // extended properties
                                                        colMetaData.column,
                                                        typeSpecificNamePart1, 
                                                        typeSpecificNamePart2,
                                                        typeSpecificNamePart3, 
                                                        colMetaData.isNullable, 
                                                        colMetaData.serverName,
                                                        colMetaData.catalogName, 
                                                        colMetaData.schemaName,
                                                        colMetaData.tableName,
                                                        colMetaData.baseColumn,
                                                        colMetaData.isKey, 
                                                        colMetaData.isIdentity,
                                                        0==colMetaData.updatability, 
                                                        colMetaData.isExpression, 
                                                        colMetaData.isDifferentName,
                                                        colMetaData.isHidden 
                                                        );
                    }
                }
            } 

            return metaDataReturn; 
        } 

        override public int RecordsAffected { 
            get {
                if (null != _command)
                    return _command.InternalRecordsAffected;
 
                // cached locally for after Close() when command is nulled out
                return _recordsAffected; 
            } 
        }
 
        internal string ResetOptionsString {
            set {
                _resetOptionsString = value;
            } 
        }
 
        private SqlStatistics Statistics { 
            get {
                return _statistics; 
            }
        }

        internal MultiPartTableName[] TableNames { 
            get {
                return _tableNames; 
            } 
            set {
                _tableNames = value; 
            }
        }

        override public int VisibleFieldCount { 
            get {
                if (this.IsClosed) { 
                    throw ADP.DataReaderClosed("VisibleFieldCount"); 
                }
                if (MetaData == null) { 
                    return 0;
                }
                return (MetaData.visibleColumns);
            } 
        }
 
        // this operator 
        override public object this[int i] {
            get { 
                return GetValue(i);
            }
        }
 
        override public object this[string name] {
            get { 
                return GetValue(GetOrdinal(name)); 
            }
        } 

        internal void Bind(TdsParserStateObject stateObj) {
            Debug.Assert(null != stateObj, "null stateobject");
 
            stateObj.Owner = this;
            _stateObj    = stateObj; 
            _parser      = stateObj.Parser; 
            _defaultLCID = _parser.DefaultLCID;
        } 

        // Fills in a schema table with meta data information.  This function should only really be called by
        //
 
        internal DataTable BuildSchemaTable() {
            _SqlMetaDataSet md = this.MetaData; 
            Debug.Assert(null != md, "BuildSchemaTable - unexpected null metadata information"); 

            DataTable schemaTable = new DataTable("SchemaTable"); 
            schemaTable.Locale = CultureInfo.InvariantCulture;
            schemaTable.MinimumCapacity = md.Length;

            DataColumn ColumnName                       = new DataColumn(SchemaTableColumn.ColumnName,                       typeof(System.String)); 
            DataColumn Ordinal                          = new DataColumn(SchemaTableColumn.ColumnOrdinal,                    typeof(System.Int32));
            DataColumn Size                             = new DataColumn(SchemaTableColumn.ColumnSize,                       typeof(System.Int32)); 
            DataColumn Precision                        = new DataColumn(SchemaTableColumn.NumericPrecision,                 typeof(System.Int16)); 
            DataColumn Scale                            = new DataColumn(SchemaTableColumn.NumericScale,                     typeof(System.Int16));
 
            DataColumn DataType                         = new DataColumn(SchemaTableColumn.DataType,                         typeof(System.Type));
            DataColumn ProviderSpecificDataType         = new DataColumn(SchemaTableOptionalColumn.ProviderSpecificDataType, typeof(System.Type));
            DataColumn NonVersionedProviderType         = new DataColumn(SchemaTableColumn.NonVersionedProviderType,         typeof(System.Int32));
            DataColumn ProviderType                     = new DataColumn(SchemaTableColumn.ProviderType,                     typeof(System.Int32)); 

            DataColumn IsLong                           = new DataColumn(SchemaTableColumn.IsLong,                           typeof(System.Boolean)); 
            DataColumn AllowDBNull                      = new DataColumn(SchemaTableColumn.AllowDBNull,                      typeof(System.Boolean)); 
            DataColumn IsReadOnly                       = new DataColumn(SchemaTableOptionalColumn.IsReadOnly,               typeof(System.Boolean));
            DataColumn IsRowVersion                     = new DataColumn(SchemaTableOptionalColumn.IsRowVersion,             typeof(System.Boolean)); 

            DataColumn IsUnique                         = new DataColumn(SchemaTableColumn.IsUnique,                         typeof(System.Boolean));
            DataColumn IsKey                            = new DataColumn(SchemaTableColumn.IsKey,                            typeof(System.Boolean));
            DataColumn IsAutoIncrement                  = new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement,          typeof(System.Boolean)); 
            DataColumn IsHidden                         = new DataColumn(SchemaTableOptionalColumn.IsHidden,                 typeof(System.Boolean));
 
            DataColumn BaseCatalogName                  = new DataColumn(SchemaTableOptionalColumn.BaseCatalogName,          typeof(System.String)); 
            DataColumn BaseSchemaName                   = new DataColumn(SchemaTableColumn.BaseSchemaName,                   typeof(System.String));
            DataColumn BaseTableName                    = new DataColumn(SchemaTableColumn.BaseTableName,                    typeof(System.String)); 
            DataColumn BaseColumnName                   = new DataColumn(SchemaTableColumn.BaseColumnName,                   typeof(System.String));

            // unique to SqlClient
            DataColumn BaseServerName                   = new DataColumn(SchemaTableOptionalColumn.BaseServerName,           typeof(System.String)); 
            DataColumn IsAliased                        = new DataColumn(SchemaTableColumn.IsAliased,                        typeof(System.Boolean));
            DataColumn IsExpression                     = new DataColumn(SchemaTableColumn.IsExpression,                     typeof(System.Boolean)); 
            DataColumn IsIdentity                       = new DataColumn("IsIdentity",                                       typeof(System.Boolean)); 
            DataColumn DataTypeName                     = new DataColumn("DataTypeName",                                     typeof(System.String));
            DataColumn UdtAssemblyQualifiedName         = new DataColumn("UdtAssemblyQualifiedName",                         typeof(System.String)); 
            // Xml metadata specific
            DataColumn XmlSchemaCollectionDatabase      = new DataColumn("XmlSchemaCollectionDatabase",                      typeof(System.String));
            DataColumn XmlSchemaCollectionOwningSchema  = new DataColumn("XmlSchemaCollectionOwningSchema",                  typeof(System.String));
            DataColumn XmlSchemaCollectionName          = new DataColumn("XmlSchemaCollectionName",                          typeof(System.String)); 

            Ordinal.DefaultValue = 0; 
            IsLong.DefaultValue = false; 

            DataColumnCollection columns = schemaTable.Columns; 

            // must maintain order for backward compatibility
            columns.Add(ColumnName);
            columns.Add(Ordinal); 
            columns.Add(Size);
            columns.Add(Precision); 
            columns.Add(Scale); 
            columns.Add(IsUnique);
            columns.Add(IsKey); 
            columns.Add(BaseServerName);
            columns.Add(BaseCatalogName);
            columns.Add(BaseColumnName);
            columns.Add(BaseSchemaName); 
            columns.Add(BaseTableName);
            columns.Add(DataType); 
            columns.Add(AllowDBNull); 
            columns.Add(ProviderType);
            columns.Add(IsAliased); 
            columns.Add(IsExpression);
            columns.Add(IsIdentity);
            columns.Add(IsAutoIncrement);
            columns.Add(IsRowVersion); 
            columns.Add(IsHidden);
            columns.Add(IsLong); 
            columns.Add(IsReadOnly); 
            columns.Add(ProviderSpecificDataType);
            columns.Add(DataTypeName); 
            columns.Add(XmlSchemaCollectionDatabase);
            columns.Add(XmlSchemaCollectionOwningSchema);
            columns.Add(XmlSchemaCollectionName);
            columns.Add(UdtAssemblyQualifiedName); 
            columns.Add(NonVersionedProviderType);
 
            for (int i = 0; i < md.Length; i++) { 
                _SqlMetaData col = md[i];
                DataRow schemaRow = schemaTable.NewRow(); 

                schemaRow[ColumnName] = col.column;
                schemaRow[Ordinal]    = col.ordinal;
                // 
                // be sure to return character count for string types, byte count otherwise
                // col.length is always byte count so for unicode types, half the length 
                // 
                // For MAX and XML datatypes, we get 0x7fffffff from the server. Do not divide this.
                schemaRow[Size] = (col.metaType.IsSizeInCharacters && (col.length != 0x7fffffff)) ? (col.length / 2) : col.length; 

                schemaRow[DataType]                 = GetFieldTypeInternal(col);
                schemaRow[ProviderSpecificDataType] = GetProviderSpecificFieldTypeInternal(col);
                schemaRow[NonVersionedProviderType] = (int) col.type; // SqlDbType enum value - does not change with TypeSystem. 
                schemaRow[DataTypeName]             = GetDataTypeNameInternal(col);
 
                if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && col.IsNewKatmaiDateTimeType) { 
                    schemaRow[ProviderType] = SqlDbType.NVarChar;
                    switch (col.type) { 
                        case SqlDbType.Date:
                            schemaRow[Size] = TdsEnums.WHIDBEY_DATE_LENGTH;
                            break;
                        case SqlDbType.Time: 
                            Debug.Assert(TdsEnums.UNKNOWN_PRECISION_SCALE == col.scale || (0 <= col.scale && col.scale <= 7), "Invalid scale for Time column: " + col.scale);
                            schemaRow[Size] = TdsEnums.WHIDBEY_TIME_LENGTH[TdsEnums.UNKNOWN_PRECISION_SCALE != col.scale ? col.scale : col.metaType.Scale]; 
                            break; 
                        case SqlDbType.DateTime2:
                            Debug.Assert(TdsEnums.UNKNOWN_PRECISION_SCALE == col.scale || (0 <= col.scale && col.scale <= 7), "Invalid scale for DateTime2 column: " + col.scale); 
                            schemaRow[Size] = TdsEnums.WHIDBEY_DATETIME2_LENGTH[TdsEnums.UNKNOWN_PRECISION_SCALE != col.scale ? col.scale : col.metaType.Scale];
                            break;
                        case SqlDbType.DateTimeOffset:
                            Debug.Assert(TdsEnums.UNKNOWN_PRECISION_SCALE == col.scale || (0 <= col.scale && col.scale <= 7), "Invalid scale for DateTimeOffset column: " + col.scale); 
                            schemaRow[Size] = TdsEnums.WHIDBEY_DATETIMEOFFSET_LENGTH[TdsEnums.UNKNOWN_PRECISION_SCALE != col.scale ? col.scale : col.metaType.Scale];
                            break; 
                    } 
                }
                else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && col.IsLargeUdt) { 
                    if (_typeSystem == SqlConnectionString.TypeSystem.SQLServer2005) {
                        schemaRow[ProviderType] = SqlDbType.VarBinary;
                    }
                    else { 
                        // TypeSystem.SQLServer2000
                        schemaRow[ProviderType] = SqlDbType.Image; 
                    } 
                }
                else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) { 
                    // TypeSystem.SQLServer2005 and above

                    // SqlDbType enum value - always the actual type for SQLServer2005.
                    schemaRow[ProviderType] = (int) col.type; 

                    if (col.type == SqlDbType.Udt) { // Additional metadata for UDTs. 
                        Debug.Assert(Connection.IsYukonOrNewer, "Invalid Column type received from the server"); 
                        schemaRow[UdtAssemblyQualifiedName] = col.udtAssemblyQualifiedName;
                    } 
                    else if (col.type == SqlDbType.Xml) { // Additional metadata for Xml.
                        Debug.Assert(Connection.IsYukonOrNewer, "Invalid DataType (Xml) for the column");
                        schemaRow[XmlSchemaCollectionDatabase]     = col.xmlSchemaCollectionDatabase;
                        schemaRow[XmlSchemaCollectionOwningSchema] = col.xmlSchemaCollectionOwningSchema; 
                        schemaRow[XmlSchemaCollectionName]         = col.xmlSchemaCollectionName;
                    } 
                } 
                else {
                    // TypeSystem.SQLServer2000 

                    // SqlDbType enum value - variable for certain types when SQLServer2000.
                    schemaRow[ProviderType] = GetVersionedMetaType(col.metaType).SqlDbType;
                } 

 
                if (TdsEnums.UNKNOWN_PRECISION_SCALE != col.precision) { 
                    schemaRow[Precision] = col.precision;
                } 
                else {
                    schemaRow[Precision] = col.metaType.Precision;
                }
 
                if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && col.IsNewKatmaiDateTimeType) {
                    schemaRow[Scale] = MetaType.MetaNVarChar.Scale; 
                } 
                else if (TdsEnums.UNKNOWN_PRECISION_SCALE != col.scale) {
                    schemaRow[Scale] = col.scale; 
                }
                else {
                    schemaRow[Scale] = col.metaType.Scale;
                } 

                schemaRow[AllowDBNull] = col.isNullable; 
 
                // If no ColInfo token received, do not set value, leave as null.
                if (_browseModeInfoConsumed) { 
                    schemaRow[IsAliased]    = col.isDifferentName;
                    schemaRow[IsKey]        = col.isKey;
                    schemaRow[IsHidden]     = col.isHidden;
                    schemaRow[IsExpression] = col.isExpression; 
                }
 
                schemaRow[IsIdentity] = col.isIdentity; 
                schemaRow[IsAutoIncrement] = col.isIdentity;
                schemaRow[IsLong] = col.metaType.IsLong; 

                // mark unique for timestamp columns
                if (SqlDbType.Timestamp == col.type) {
                    schemaRow[IsUnique] = true; 
                    schemaRow[IsRowVersion] = true;
                } 
                else { 
                    schemaRow[IsUnique] = false;
                    schemaRow[IsRowVersion] = false; 
                }

                schemaRow[IsReadOnly] = (0 == col.updatability);
 
                if (!ADP.IsEmpty(col.serverName)) {
                    schemaRow[BaseServerName] = col.serverName; 
                } 
                if (!ADP.IsEmpty(col.catalogName)) {
                    schemaRow[BaseCatalogName] = col.catalogName; 
                }
                if (!ADP.IsEmpty(col.schemaName)) {
                    schemaRow[BaseSchemaName] = col.schemaName;
                } 
                if (!ADP.IsEmpty(col.tableName)) {
                    schemaRow[BaseTableName] = col.tableName; 
                } 
                if (!ADP.IsEmpty(col.baseColumn)) {
                    schemaRow[BaseColumnName] = col.baseColumn; 
                }
                else if (!ADP.IsEmpty(col.column)) {
                    schemaRow[BaseColumnName] = col.column;
                } 

                schemaTable.Rows.Add(schemaRow); 
                schemaRow.AcceptChanges(); 
            }
 
            // mark all columns as readonly
            foreach(DataColumn column in columns) {
                column.ReadOnly = true; // MDAC 70943
            } 

            return schemaTable; 
        } 

        internal void Cancel(int objectID) { 
            TdsParserStateObject stateObj = _stateObj;
            if (null != stateObj) {
                stateObj.Cancel(objectID);
            } 
        }
 
        // wipe any data off the wire from a partial read 
        // and reset all pointers for sequential access
        private void CleanPartialRead() { 
            Debug.Assert(true == _dataReady, "invalid call to CleanPartialRead");

            // following cases for sequential read
            // i. user called read but didn't fetch anything 
            // iia. user called read and fetched a subset of the columns
            // iib. user called read and fetched a subset of the column data 
 
            // i. user called read but didn't fetch anything
            if (0 == _nextColumnHeaderToRead) { 
                _stateObj.Parser.SkipRow(_metaData, _stateObj);
            }
            else {
                // iia.  if we still have bytes left from a partially read column, skip 
                ResetBlobState();
 
                // iib. 
                // now read the remaining values off the wire for this row
                _stateObj.Parser.SkipRow(_metaData, _nextColumnHeaderToRead, _stateObj); 
            }
        }

        override public void Close() { 
            SqlStatistics statistics = null;
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlDataReader.Close|API> %d#", ObjectID); 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                if (IsClosed)
                    return;

                SetTimeout(); 

                CloseInternal(true /*closeReader*/); 
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp);
            }
        }
 
        private void CloseInternal(bool closeReader) {
            TdsParser parser = _parser; 
            TdsParserStateObject stateObj = _stateObj; 
            bool closeConnection = (IsCommandBehavior(CommandBehavior.CloseConnection));
            _parser = null; 
            bool aborting = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                    if (parser != null && stateObj != null && stateObj._pendingData) {
                        // It is possible for this to be called during connection close on a 
                        // broken connection, so check state first.
                        if (parser.State == TdsParserState.OpenLoggedIn) { 
                            // if user called read but didn't fetch any values, skip the row 
                            // same applies after NextResult on ALTROW because NextResult starts rowconsumption in that case ...
 
                            Debug.Assert(SniContext.Snix_Read==stateObj.SniContext, String.Format((IFormatProvider)null, "The SniContext should be Snix_Read but it actually is {0}", stateObj.SniContext));

                            if (_altRowStatus == ALTROWSTATUS.AltRow) {
                                _dataReady = true;      // set _dataReady to not confuse CleanPartialRead 
                            }
                            if (_dataReady) { 
                                CleanPartialRead(); 
                            }
                            parser.Run(RunBehavior.Clean, _command, this, null, stateObj); 
                        }
                    }
                    RestoreServerSettings(parser, stateObj);
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG 
            }
            catch (System.OutOfMemoryException e) {
                _isClosed = true;
                aborting = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                } 
                throw;
            } 
            catch (System.StackOverflowException e) {
                _isClosed = true;
                aborting = true;
                if (null != _connection) { 
                    _connection.Abort(e);
                } 
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _isClosed = true;
                aborting = true;
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw; 
            } 
            finally {
                if (aborting) { 
                    _isClosed = true;
                    _command = null; // we are done at this point, don't allow navigation to the connection
                    _connection = null;
                    _statistics = null; 
                }
                else { 
 
                    if (closeReader) {
                        _stateObj = null; 
                        _data = null;

                        //
 

 
 

 


                        if (Connection != null) {
                            Connection.RemoveWeakReference(this);  // This doesn't catch everything -- the connection may be closed, but it prevents dead readers from clogging the collection 
                        }
 
 
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try { 
#if DEBUG
                            object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                            RuntimeHelpers.PrepareConstrainedRegions(); 
                            try {
                                Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG 
                                if (null != _command) {
                                    if (null != stateObj) { 
                                        stateObj.CloseSession();
                                    }
                                }
#if DEBUG 
                            }
                            finally { 
                                Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                            }
#endif //DEBUG 
                        }
                        catch (System.OutOfMemoryException e) {
                            _isClosed = true;
                            aborting = true; 
                            if (null != _connection) {
                                _connection.Abort(e); 
                            } 
                            throw;
                        } 
                        catch (System.StackOverflowException e) {
                            _isClosed = true;
                            aborting = true;
                            if (null != _connection) { 
                                _connection.Abort(e);
                            } 
                            throw; 
                        }
                        catch (System.Threading.ThreadAbortException e)  { 
                            _isClosed = true;
                            aborting = true;
                            if (null != _connection) {
                                _connection.Abort(e); 
                            }
                            throw; 
                        } 

                        SetMetaData(null, false); 
                        _dataReady = false;
                        _isClosed = true;
                        _fieldNameLookup = null;
 
                        // if the user calls ExecuteReader(CommandBehavior.CloseConnection)
                        // then we close down the connection when we are done reading results 
                        if (closeConnection) { 
                            if (Connection != null) {
                                Connection.Close(); 
                            }
                        }
                        if (_command != null) {
                            // cache recordsaffected to be returnable after DataReader.Close(); 
                            _recordsAffected = _command.InternalRecordsAffected;
                        } 
 
                        _command = null; // we are done at this point, don't allow navigation to the connection
                        _connection = null; 
                        _statistics = null;
                    }
                }
            } 
        }
 
        internal void CloseReaderFromConnection() { 
            Close();
        } 

        private void ConsumeMetaData() {
            // warning:  Don't check the MetaData property within this function
            // warning:  as it will be a reentrant call 
            while (_parser != null && _stateObj != null && _stateObj._pendingData && !_metaDataConsumed) {
                _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj); 
            } 

            // we hide hidden columns from the user so build an internal map 
            // that compacts all hidden columns from the array
            if (null != _metaData) {
                _metaData.visibleColumns = 0;
 
                Debug.Assert(null == _metaData.indexMap, "non-null metaData indexmap");
                int[] indexMap = new int[_metaData.Length]; 
                for (int i = 0; i < indexMap.Length; ++i) { 
                    indexMap[i] = _metaData.visibleColumns;
 
                    if (!(_metaData[i].isHidden)) {
                        _metaData.visibleColumns++;
                    }
                } 
                _metaData.indexMap = indexMap;
            } 
        } 

        override public string GetDataTypeName(int i) { 
            SqlStatistics statistics = null;
            try {
                statistics = SqlStatistics.StartTimer(Statistics);
                if (MetaData == null) 
                    throw SQL.InvalidRead();
 
                return GetDataTypeNameInternal(_metaData[i]); 
            }
            finally { 
                SqlStatistics.StopTimer(statistics);
            }
        }
 
        private string GetDataTypeNameInternal(_SqlMetaData metaData) {
            string dataTypeName = null; 
 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsNewKatmaiDateTimeType) {
                dataTypeName = MetaType.MetaNVarChar.TypeName; 
            }
            else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsLargeUdt) {
                if (_typeSystem == SqlConnectionString.TypeSystem.SQLServer2005) {
                    dataTypeName = MetaType.MetaMaxVarBinary.TypeName; 
                }
                else { 
                    // TypeSystem.SQLServer2000 
                    dataTypeName = MetaType.MetaImage.TypeName;
                } 
            }
            else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) {
                // TypeSystem.SQLServer2005 and above
 
                if (metaData.type == SqlDbType.Udt) {
                    Debug.Assert(Connection.IsYukonOrNewer, "Invalid Column type received from the server"); 
                    dataTypeName = metaData.udtDatabaseName + "." + metaData.udtSchemaName + "." + metaData.udtTypeName; 
                }
                else { // For all other types, including Xml - use data in MetaType. 
                    dataTypeName = metaData.metaType.TypeName;
                }
            }
            else { 
                // TypeSystem.SQLServer2000
 
                dataTypeName = GetVersionedMetaType(metaData.metaType).TypeName; 
            }
 
            return dataTypeName;
        }

        override public IEnumerator GetEnumerator() { 
            return new DbEnumerator((IDataReader)this, IsCommandBehavior(CommandBehavior.CloseConnection));
        } 
 
        override public Type GetFieldType(int i) {
            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics);
                if (MetaData == null) {
                    throw SQL.InvalidRead(); 
                }
 
                return GetFieldTypeInternal(_metaData[i]); 
            }
            finally { 
                SqlStatistics.StopTimer(statistics);
            }
        }
 
        private Type GetFieldTypeInternal(_SqlMetaData metaData) {
            Type fieldType = null; 
 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsNewKatmaiDateTimeType) {
                // Return katmai types as string 
                fieldType = MetaType.MetaNVarChar.ClassType;
            }
            else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsLargeUdt) {
                if (_typeSystem == SqlConnectionString.TypeSystem.SQLServer2005) { 
                    fieldType = MetaType.MetaMaxVarBinary.ClassType;
                } 
                else { 
                    // TypeSystem.SQLServer2000
                    fieldType = MetaType.MetaImage.ClassType; 
                }
            }
            else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) {
                // TypeSystem.SQLServer2005 and above 

                if (metaData.type == SqlDbType.Udt) { 
                    Debug.Assert(Connection.IsYukonOrNewer, "Invalid Column type received from the server"); 
                    SqlConnection.CheckGetExtendedUDTInfo(metaData, false);
                    fieldType = metaData.udtType; 
                }
                else { // For all other types, including Xml - use data in MetaType.
                    fieldType = metaData.metaType.ClassType; // Com+ type.
                } 
            }
            else { 
                // TypeSystem.SQLServer2000 

                fieldType = GetVersionedMetaType(metaData.metaType).ClassType; // Com+ type. 
            }

            return fieldType;
        } 

        virtual internal int GetLocaleId(int i) { 
            _SqlMetaData sqlMetaData = MetaData[i]; 
            int lcid;
 
            if (sqlMetaData.collation != null) {
                lcid = sqlMetaData.collation.LCID;
            }
            else { 
                lcid = 0;
            } 
            return lcid; 
        }
 
        override public string GetName(int i) {
            if (MetaData == null) {
                throw SQL.InvalidRead();
            } 
            Debug.Assert(null != _metaData[i].column, "MDAC 66681");
            return _metaData[i].column; 
        } 

        override public Type GetProviderSpecificFieldType(int i) { 
            SqlStatistics statistics = null;
            try {
                statistics = SqlStatistics.StartTimer(Statistics);
                if (MetaData == null) { 
                    throw SQL.InvalidRead();
                } 
 
                return GetProviderSpecificFieldTypeInternal(_metaData[i]);
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        private Type GetProviderSpecificFieldTypeInternal(_SqlMetaData metaData) { 
            Type providerSpecificFieldType = null; 

            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsNewKatmaiDateTimeType) { 
                providerSpecificFieldType = MetaType.MetaNVarChar.SqlType;
            }
            else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsLargeUdt) {
                if (_typeSystem == SqlConnectionString.TypeSystem.SQLServer2005) { 
                    providerSpecificFieldType = MetaType.MetaMaxVarBinary.SqlType;
                } 
                else { 
                    // TypeSystem.SQLServer2000
                    providerSpecificFieldType = MetaType.MetaImage.SqlType; 
                }
            }
            else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) {
                // TypeSystem.SQLServer2005 and above 

                if (metaData.type == SqlDbType.Udt) { 
                    Debug.Assert(Connection.IsYukonOrNewer, "Invalid Column type received from the server"); 
                    SqlConnection.CheckGetExtendedUDTInfo(metaData, false);
                    providerSpecificFieldType = metaData.udtType; 
                }
                else { // For all other types, including Xml - use data in MetaType.
                    providerSpecificFieldType = metaData.metaType.SqlType; // SqlType type.
                } 
            }
            else { 
                // TypeSystem.SQLServer2000 

                providerSpecificFieldType = GetVersionedMetaType(metaData.metaType).SqlType; // SqlType type. 
            }

            return providerSpecificFieldType;
        } 

        // named field access 
        override public int GetOrdinal(string name) { 
            SqlStatistics statistics = null;
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                if (null == _fieldNameLookup) {
                    if (null == MetaData) {
                        throw SQL.InvalidRead(); 
                    }
                    _fieldNameLookup = new FieldNameLookup(this, _defaultLCID); 
                } 
                return _fieldNameLookup.GetOrdinal(name); // MDAC 71470
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        override public object GetProviderSpecificValue(int i) { 
            return GetSqlValue(i); 
        }
 
        override public int GetProviderSpecificValues(object[] values) {
            return GetSqlValues(values);
        }
 
        override public DataTable GetSchemaTable() {
            SqlStatistics statistics = null; 
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlDataReader.GetSchemaTable|API> %d#", ObjectID);
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                if (null == _metaData || null == _metaData.schemaTable) {
                    if (null != this.MetaData) {
 
                        _metaData.schemaTable = BuildSchemaTable();
                        Debug.Assert(null != _metaData.schemaTable, "No schema information yet!"); 
                        // filter table? 
                    }
                } 
                if (null != _metaData) {
                    return _metaData.schemaTable;
                }
                return null; 
            }
            finally { 
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp);
            } 
        }

        override public bool GetBoolean(int i) {
            ReadColumn(i); 
            return _data[i].Boolean;
        } 
 
        override public byte GetByte(int i) {
            ReadColumn(i); 
            return _data[i].Byte;
        }

        override public long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length) { 
            SqlStatistics statistics = null;
            long  cbBytes = 0; 
 

            if (MetaData == null || !_dataReady) 
                throw SQL.InvalidRead();

            // don't allow get bytes on non-long or non-binary columns
            MetaType mt = _metaData[i].metaType; 
            if (!(mt.IsLong || mt.IsBinType) || (SqlDbType.Xml == mt.SqlDbType)) {
                throw SQL.NonBlobColumn(_metaData[i].column); 
            } 

            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                SetTimeout();
                cbBytes = GetBytesInternal(i, dataIndex, buffer, bufferIndex, length);
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
            } 
            return cbBytes;
        } 


        // Used (indirectly) by SqlCommand.CompleteXmlReader
        virtual internal long GetBytesInternal(int i, long dataIndex, byte[] buffer, int bufferIndex, int length) { 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    int cbytes = 0;
 
                    // sequential reading 
                    if (IsCommandBehavior(CommandBehavior.SequentialAccess)) {
 
                        if (0 > i || i >= _metaData.Length) {   // _metaData can't be null if we don't throw above.
                            throw new IndexOutOfRangeException();
                        }
 
                        if (_nextColumnDataToRead > i) {
                            // We've already read/skipped over this column header. 
                            throw ADP.NonSequentialColumnAccess(i, _nextColumnDataToRead); 
                        }
 
                        if (_nextColumnHeaderToRead <= i) {
                            ReadColumnHeader(i);
                        }
 
                        // If data is null, ReadColumnHeader sets the data.IsNull bit.
                        if (_data[i] != null && _data[i].IsNull) { 
                            throw new SqlNullValueException(); 
                        }
 
                        if (0 == _columnDataBytesRemaining) {
                            return 0; // We've read this column to the end
                        }
 
                        // if no buffer is passed in, return the number total of bytes, or -1
                        if (null == buffer) { 
                            if (_metaData[i].metaType.IsPlp) { 
                                return (long) _parser.PlpBytesTotalLength(_stateObj);
                            } 
                            return _columnDataBytesRemaining;
                        }

                        if (dataIndex < 0) 
                            throw ADP.NegativeParameter("dataIndex");
 
                        if (dataIndex < _columnDataBytesRead) { 
                            throw ADP.NonSeqByteAccess(dataIndex, _columnDataBytesRead, ADP.GetBytes);
                        } 

                        // if the dataIndex is not equal to bytes read, then we have to skip bytes
                        long cb = dataIndex - _columnDataBytesRead;
 
                        // if dataIndex is outside of the data range, return 0
                        if ((cb > _columnDataBytesRemaining) && !_metaData[i].metaType.IsPlp) { 
                            return 0; 
                        }
 
                        // if bad buffer index, throw
                        if (bufferIndex < 0 || bufferIndex >= buffer.Length)
                            throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex");
 
                        // if there is not enough room in the buffer for data
                        if (length + bufferIndex > buffer.Length) 
                            throw ADP.InvalidBufferSizeOrIndex(length, bufferIndex); 

                        if (length < 0) 
                            throw ADP.InvalidDataLength(length);

                        // if plp columns, do partial reads. Don't read the entire value in one shot.
                        if (_metaData[i].metaType.IsPlp) { 
                            if (cb > 0) {
                                cb = (long) _parser.SkipPlpValue((ulong) cb, _stateObj); 
                                _columnDataBytesRead +=cb; 
                            }
                            cb = (long) _stateObj.ReadPlpBytes(ref buffer, bufferIndex, length); 
                            _columnDataBytesRead += cb;
                            _columnDataBytesRemaining = (long)_parser.PlpBytesLeft(_stateObj);
                            return cb;
                        } 

                        if (cb > 0) { 
                            _parser.SkipLongBytes((ulong) cb, _stateObj); 
                            _columnDataBytesRead += cb;
                            _columnDataBytesRemaining -= cb; 
                        }

                        // read the min(bytesLeft, length) into the user's buffer
                        cb = (_columnDataBytesRemaining < length) ? _columnDataBytesRemaining : length; 
                        _stateObj.ReadByteArray(buffer, bufferIndex, (int)cb);
                        _columnDataBytesRead += cb; 
                        _columnDataBytesRemaining -= cb; 
                        return cb;
 

                    }

                    // random access now! 
                    // note that since we are caching in an array, and arrays aren't 64 bit ready yet,
                    // we need can cast to int if the dataIndex is in range 
                    if (dataIndex < 0) 
                        throw ADP.NegativeParameter("dataIndex");
 
                    if (dataIndex > Int32.MaxValue) {
                        throw ADP.InvalidSourceBufferIndex(cbytes, dataIndex, "dataIndex");
                    }
 
                    int ndataIndex = (int)dataIndex;
                    byte[] data; 
 
                    // WebData 99342 - in the non-sequential case, we need to support
                    //                 the use of GetBytes on string data columns, but 
                    //                 GetSqlBinary isn't supposed to.  What we end up
                    //                 doing isn't exactly pretty, but it does work.
                    if (_metaData[i].metaType.IsBinType) {
                        data = GetSqlBinary(i).Value; 
                    }
                    else { 
                        Debug.Assert(_metaData[i].metaType.IsLong, "non long type?"); 
                        Debug.Assert(_metaData[i].metaType.IsCharType, "non-char type?");
 
                        SqlString temp = GetSqlString(i);
                        if (_metaData[i].metaType.IsNCharType) {
                            data = temp.GetUnicodeBytes();
                        } 
                        else {
                            data = temp.GetNonUnicodeBytes(); 
                        } 
                    }
 
                    cbytes = data.Length;

                    // if no buffer is passed in, return the number of characters we have
                    if (null == buffer) 
                        return cbytes;
 
                    // if dataIndex is outside of data range, return 0 
                    if (ndataIndex < 0 || ndataIndex >= cbytes) {
                        return 0; 
                    }
                    try {
                        if (ndataIndex < cbytes) {
                            // help the user out in the case where there's less data than requested 
                            if ((ndataIndex + length) > cbytes)
                                cbytes = cbytes - ndataIndex; 
                            else 
                                cbytes = length;
                        } 

                        Array.Copy(data, ndataIndex, buffer, bufferIndex, cbytes);
                    }
                    catch (Exception e) { 
                        //
                        if (!ADP.IsCatchableExceptionType(e)) { 
                            throw; 
                        }
                        cbytes = data.Length; 

                        if (length < 0)
                            throw ADP.InvalidDataLength(length);
 
                        // if bad buffer index, throw
                        if (bufferIndex < 0 || bufferIndex >= buffer.Length) 
                            throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex"); 

                        // if there is not enough room in the buffer for data 
                        if (cbytes + bufferIndex > buffer.Length)
                            throw ADP.InvalidBufferSizeOrIndex(cbytes, bufferIndex);

                        throw; 
                    }
 
                    return cbytes; 
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG 
            }
            catch (System.OutOfMemoryException e) { 
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw;
            }
            catch (System.StackOverflowException e) { 
                _isClosed = true;
                if (null != _connection) { 
                    _connection.Abort(e); 
                }
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  {
                _isClosed = true;
                if (null != _connection) { 
                    _connection.Abort(e);
                } 
                throw; 
            }
        } 

        [ EditorBrowsableAttribute(EditorBrowsableState.Never) ] // MDAC 69508
        override public char GetChar(int i) {
            throw ADP.NotSupported(); 
        }
 
        override public long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length) { 
            SqlStatistics statistics = null;
 
            if (MetaData == null || !_dataReady)
                throw SQL.InvalidRead();

           if (0 > i || i >= _metaData.Length) {   // _metaData can't be null if we don't throw above. 
                throw new IndexOutOfRangeException();
            } 
 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                SetTimeout();
                if ((_metaData[i].metaType.IsPlp) &&
                    (IsCommandBehavior(CommandBehavior.SequentialAccess)) ) {
                    if (length < 0) { 
                        throw ADP.InvalidDataLength(length);
                    } 
 
                    // if bad buffer index, throw
                    if ((bufferIndex < 0) || (buffer != null && bufferIndex >= buffer.Length)) { 
                        throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex");
                    }

                    // if there is not enough room in the buffer for data 
                    if (buffer != null && (length + bufferIndex > buffer.Length)) {
                        throw ADP.InvalidBufferSizeOrIndex(length, bufferIndex); 
                    } 
                    if ( _metaData[i].type == SqlDbType.Xml ) {
                        return GetStreamingXmlChars(i, dataIndex, buffer, bufferIndex, length); 
                    }
                    else {
                        return GetCharsFromPlpData(i, dataIndex, buffer, bufferIndex, length);
                    } 
                }
 
                // Did we start reading this value yet? 
                if ((_nextColumnDataToRead == (i+1)) && (_nextColumnHeaderToRead == (i+1)) &&
                     (_columnDataChars != null)) { 

                    if ((IsCommandBehavior(CommandBehavior.SequentialAccess)) &&
                        (dataIndex < _columnDataCharsRead)) {
                        // Don't allow re-read of same chars in sequential access mode 
                        throw ADP.NonSeqByteAccess(dataIndex, _columnDataCharsRead, ADP.GetChars);
                    } 
                } 
                else {
 
                    // if the object doesn't contain a char[] then the user will get an exception
                    string s = GetSqlString(i).Value;

                    _columnDataChars = s.ToCharArray(); 
                    _columnDataCharsRead = 0;
                } 
 
                int cchars = _columnDataChars.Length;
 
                // note that since we are caching in an array, and arrays aren't 64 bit ready yet,
                // we need can cast to int if the dataIndex is in range
                if (dataIndex > Int32.MaxValue) {
                    throw ADP.InvalidSourceBufferIndex(cchars, dataIndex, "dataIndex"); 
                }
                int ndataIndex = (int)dataIndex; 
 
                // if no buffer is passed in, return the number of characters we have
                if (null == buffer) 
                    return cchars;

                // if dataIndex outside of data range, return 0
                if (ndataIndex < 0 || ndataIndex >= cchars) 
                    return 0;
 
                try { 
                    if (ndataIndex < cchars) {
                        // help the user out in the case where there's less data than requested 
                        if ((ndataIndex + length) > cchars)
                            cchars = cchars - ndataIndex;
                        else
                            cchars = length; 
                    }
 
                    Array.Copy(_columnDataChars, ndataIndex, buffer, bufferIndex, cchars); 
                    _columnDataCharsRead += cchars;
                } 
                catch (Exception e) {
                    //
                    if (!ADP.IsCatchableExceptionType(e)) {
                        throw; 
                    }
                    cchars = _columnDataChars.Length; 
 
                    if (length < 0)
                       throw ADP.InvalidDataLength(length); 

                    // if bad buffer index, throw
                    if (bufferIndex < 0 || bufferIndex >= buffer.Length)
                        throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex"); 

                    // if there is not enough room in the buffer for data 
                    if (cchars + bufferIndex > buffer.Length) 
                        throw ADP.InvalidBufferSizeOrIndex(cchars, bufferIndex);
 
                    throw;
                }

                return cchars; 
            }
            finally { 
                SqlStatistics.StopTimer(statistics); 
            }
        } 

        private long GetCharsFromPlpData(int i, long dataIndex, char[] buffer, int bufferIndex, int length) {
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                    long cch;
 
                    if (MetaData == null || !_dataReady) {
                        throw SQL.InvalidRead(); 
                    } 

                    // don't allow get bytes on non-long or non-binary columns 
                    Debug.Assert(_metaData[i].metaType.IsPlp, "GetCharsFromPlpData called on a non-plp column!");
                    // Must be sequential reading
                    Debug.Assert (IsCommandBehavior(CommandBehavior.SequentialAccess), "GetCharsFromPlpData called for non-Sequential access");
 

                    if (_nextColumnDataToRead > i) { 
                        // We've already read/skipped over this column header. 
                            throw ADP.NonSequentialColumnAccess(i, _nextColumnDataToRead);
                    } 

                    if (!_metaData[i].metaType.IsCharType) {
                        throw SQL.NonCharColumn(_metaData[i].column);
                    } 

                    if (_nextColumnHeaderToRead <= i) { 
                        ReadColumnHeader(i); 
                    }
 
                    // If data is null, ReadColumnHeader sets the data.IsNull bit.
                    if (_data[i] != null && _data[i].IsNull) {
                        throw new SqlNullValueException();
                    } 

                    if (dataIndex < _columnDataCharsRead) { 
                        // Don't allow re-read of same chars in sequential access mode 
                        throw ADP.NonSeqByteAccess(dataIndex, _columnDataCharsRead, ADP.GetChars);
                    } 


                    bool isUnicode = _metaData[i].metaType.IsNCharType;
 
                    if (0 == _columnDataBytesRemaining) {
                        return 0; // We've read this column to the end 
                    } 

                    // if no buffer is passed in, return the total number of characters or -1 
                    if (null == buffer) {
                        cch = (long) _parser.PlpBytesTotalLength(_stateObj);
                        return (isUnicode && (cch > 0)) ? cch >> 1 : cch;
                    } 
                    if (dataIndex > _columnDataCharsRead) {
                        // Skip chars 
                        cch = dataIndex - _columnDataCharsRead; 
                        cch = isUnicode ? (cch << 1 ) : cch;
                        cch = (long) _parser.SkipPlpValue((ulong)(cch), _stateObj); 
                        _columnDataBytesRead += cch;
                        _columnDataCharsRead += (isUnicode && (cch > 0)) ? cch >> 1 : cch;
                    }
                    cch = length; 

                    if (isUnicode) { 
                        cch = (long) _parser.ReadPlpUnicodeChars(ref buffer, bufferIndex, length, _stateObj); 
                        _columnDataBytesRead += (cch << 1);
                    } 
                    else {
                        cch = (long) _parser.ReadPlpAnsiChars(ref buffer, bufferIndex, length, _metaData[i], _stateObj);
                        _columnDataBytesRead += cch << 1;
                    } 
                    _columnDataCharsRead += cch;
                    _columnDataBytesRemaining = (long)_parser.PlpBytesLeft(_stateObj); 
                    return cch; 
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG 
            }
            catch (System.OutOfMemoryException e) { 
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw;
            }
            catch (System.StackOverflowException e) { 
                _isClosed = true;
                if (null != _connection) { 
                    _connection.Abort(e); 
                }
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  {
                _isClosed = true;
                if (null != _connection) { 
                    _connection.Abort(e);
                } 
                throw; 
            }
        } 

        internal long GetStreamingXmlChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length) {
           //  return GetCharsFromPlpData(i, dataIndex, buffer, bufferIndex, length);
           SqlStreamingXml localSXml = null; 
           if ((_streamingXml != null) && ( _streamingXml.ColumnOrdinal != i)) {
                _streamingXml.Close(); 
                _streamingXml = null; 
           }
            if (_streamingXml == null) { 
                localSXml = new SqlStreamingXml(i, this);
            }
            else {
                localSXml = _streamingXml; 
            }
            long cnt = localSXml.GetChars(dataIndex, buffer, bufferIndex, length); 
            if (_streamingXml == null) { 
                // Data is read through GetBytesInternal which may dispose _streamingXml if it has to advance the column ordinal.
                // Therefore save the new SqlStreamingXml class after the read succeeds. 
                _streamingXml = localSXml;
            }
            return cnt;
        } 

        [ EditorBrowsableAttribute(EditorBrowsableState.Never) ] // MDAC 69508 
        IDataReader IDataRecord.GetData(int i) { 
            throw ADP.NotSupported();
        } 

        override public DateTime GetDateTime(int i) {
            ReadColumn(i);
 
            DateTime dt = _data[i].DateTime;
            // This accessor can be called for regular DateTime column. In this case we should not throw 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType) { 
                // TypeSystem.SQLServer2005 or less
 
                // If the above succeeds, then we received a valid DateTime instance, now we need to force
                // an InvalidCastException since DateTime is not exposed with the version knob in this setting.
                // To do so, we simply force the exception by casting the string representation of the value
                // To DateTime. 
                object temp = (object) _data[i].String;
                dt = (DateTime) temp; 
            } 

            return dt; 
        }

        override public Decimal GetDecimal(int i) {
            ReadColumn(i); 
            return _data[i].Decimal;
        } 
 
        override public double GetDouble(int i) {
            ReadColumn(i); 
            return _data[i].Double;
        }

        override public float GetFloat(int i) { 
            ReadColumn(i);
            return _data[i].Single; 
        } 

        override public Guid GetGuid(int i) { 
            ReadColumn(i);
            return _data[i].SqlGuid.Value;
        }
 
        override public Int16 GetInt16(int i) {
            ReadColumn(i); 
            return _data[i].Int16; 
        }
 
        override public Int32 GetInt32(int i) {
            ReadColumn(i);
            return _data[i].Int32;
        } 

        override public Int64 GetInt64(int i) { 
            ReadColumn(i); 
            return _data[i].Int64;
        } 

        virtual public SqlBoolean GetSqlBoolean(int i) {
            ReadColumn(i);
            return _data[i].SqlBoolean; 
        }
 
        virtual public SqlBinary GetSqlBinary(int i) { 
            ReadColumn(i);
            return _data[i].SqlBinary; 
        }

        virtual public SqlByte GetSqlByte(int i) {
            ReadColumn(i); 
            return _data[i].SqlByte;
        } 
 
        virtual public SqlBytes GetSqlBytes(int i) {
            if (MetaData == null) 
                throw SQL.InvalidRead();

            ReadColumn(i);
            SqlBinary data = _data[i].SqlBinary; 
            return new SqlBytes(data);
        } 
 
        virtual public SqlChars GetSqlChars(int i) {
            ReadColumn(i); 
            SqlString data;
            // Convert Katmai types to string
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType)
            { 
                data = _data[i].KatmaiDateTimeSqlString;
            } else { 
                data = _data[i].SqlString; 
            }
            return new SqlChars(data); 
        }

        virtual public SqlDateTime GetSqlDateTime(int i) {
            ReadColumn(i); 
            return _data[i].SqlDateTime;
        } 
 
        virtual public SqlDecimal GetSqlDecimal(int i) {
            ReadColumn(i); 
            return _data[i].SqlDecimal;
        }

        virtual public SqlGuid GetSqlGuid(int i) { 
            ReadColumn(i);
            return _data[i].SqlGuid; 
        } 

        virtual public SqlDouble GetSqlDouble(int i) { 
            ReadColumn(i);
            return _data[i].SqlDouble;
        }
 
        virtual public SqlInt16 GetSqlInt16(int i) {
            ReadColumn(i); 
            return _data[i].SqlInt16; 
        }
 
        virtual public SqlInt32 GetSqlInt32(int i) {
            ReadColumn(i);
            return _data[i].SqlInt32;
        } 

        virtual public SqlInt64 GetSqlInt64(int i) { 
            ReadColumn(i); 
            return _data[i].SqlInt64;
        } 

        virtual public SqlMoney GetSqlMoney(int i) {
            ReadColumn(i);
            return _data[i].SqlMoney; 
        }
 
        virtual public SqlSingle GetSqlSingle(int i) { 
            ReadColumn(i);
            return _data[i].SqlSingle; 
        }

        //
        virtual public SqlString GetSqlString(int i) { 
            ReadColumn(i);
 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType) { 
                return _data[i].KatmaiDateTimeSqlString;
            } 

            return _data[i].SqlString;
        }
 
        virtual public SqlXml GetSqlXml(int i){
            ReadColumn(i); 
            SqlXml sx = null; 

            if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) { 
                // TypeSystem.SQLServer2005

                sx = _data[i].IsNull ? SqlXml.Null : _data[i].SqlCachedBuffer.ToSqlXml();
            } 
            else {
                // TypeSystem.SQLServer2000 
 
                // First, attempt to obtain SqlXml value.  If not SqlXml, we will throw the appropriate
                // cast exception. 
                sx = _data[i].IsNull ? SqlXml.Null : _data[i].SqlCachedBuffer.ToSqlXml();

                // If the above succeeds, then we received a valid SqlXml instance, now we need to force
                // an InvalidCastException since SqlXml is not exposed with the version knob in this setting. 
                // To do so, we simply force the exception by casting the string representation of the value
                // To SqlXml. 
                object temp = (object) _data[i].String; 
                sx = (SqlXml) temp;
            } 

            return sx;
        }
 
        virtual public object GetSqlValue(int i) {
            SqlStatistics statistics = null; 
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
 
                if (MetaData == null || !_dataReady) {
                    throw SQL.InvalidRead();
                }
 
                SetTimeout();
 
                Object o = GetSqlValueInternal(i); 
                return o;
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        private object GetSqlValueInternal(int i) { 
            Debug.Assert (_dataReady, "Attempting to GetValue without data ready?"); 

            ReadColumn(i, false); // timeout set on outer call 

            Debug.Assert(null != _data, "no data columns?");                // should have been caught already.
            Debug.Assert(i < _data.Length, "reading beyond data length?");  // should have been caught already.
 
            object o;
 
            // Convert Katmai types to string 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType) {
                return _data[i].KatmaiDateTimeSqlString; 
            }
            else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsLargeUdt) {
                o = _data[i].SqlValue;
            } 
            else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) {
                // TypeSystem.SQLServer2005 
 
                if (_metaData[i].type == SqlDbType.Udt) {
                    SqlConnection.CheckGetExtendedUDTInfo(_metaData[i], true); 
                    o = Connection.GetUdtValue(_data[i].Value, _metaData[i], false);
                }
                else {
                    o = _data[i].SqlValue; 
                }
            } 
            else { 
                // TypeSystem.SQLServer2000
 
                if (_metaData[i].type == SqlDbType.Xml) {
                    o = _data[i].SqlString;
                }
                else { 
                    o = _data[i].SqlValue;
                } 
            } 

            return o; 
        }

        virtual public int GetSqlValues(object[] values){
            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                if (MetaData == null || !_dataReady) { 
                    throw SQL.InvalidRead();
                } 
                if (null == values) {
                    throw ADP.ArgumentNull("values");
                }
 
                SetTimeout();
 
                int copyLen = (values.Length < _metaData.visibleColumns) ? values.Length : _metaData.visibleColumns; 

                for (int i = 0; i < copyLen; i++) { 
                    values[_metaData.indexMap[i]] = GetSqlValueInternal(i);
                }
                return copyLen;
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
            } 
        }
 
        override public string GetString(int i) {
            ReadColumn(i);

            // Convert katmai value to string if type system knob is 2005 or earlier 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType) {
                return _data[i].KatmaiDateTimeString; 
            } 

            return _data[i].String; 
        }

        override public object GetValue(int i) {
            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
 
                if (MetaData == null || !_dataReady) {
                    throw SQL.InvalidRead(); 
                }

                SetTimeout();
 
                object o = GetValueInternal(i);
                return o; 
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
            }
        }

        virtual public TimeSpan GetTimeSpan(int i) { 
            ReadColumn(i);
 
            TimeSpan t = _data[i].Time; 

            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005) { 
                // TypeSystem.SQLServer2005 or less

                // If the above succeeds, then we received a valid TimeSpan instance, now we need to force
                // an InvalidCastException since TimeSpan is not exposed with the version knob in this setting. 
                // To do so, we simply force the exception by casting the string representation of the value
                // To TimeSpan. 
                object temp = (object) _data[i].String; 
                t = (TimeSpan) temp;
            } 

            return t;
        }
 
        virtual public DateTimeOffset GetDateTimeOffset(int i) {
            ReadColumn(i); 
 
            DateTimeOffset dto = _data[i].DateTimeOffset;
 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005) {
                // TypeSystem.SQLServer2005 or less

                // If the above succeeds, then we received a valid DateTimeOffset instance, now we need to force 
                // an InvalidCastException since DateTime is not exposed with the version knob in this setting.
                // To do so, we simply force the exception by casting the string representation of the value 
                // To DateTimeOffset. 
                object temp = (object) _data[i].String;
                dto = (DateTimeOffset) temp; 
            }

            return dto;
        } 

        private object GetValueInternal(int i) { 
            Debug.Assert (_dataReady, "Attempting to GetValue without data ready?"); 
            ReadColumn(i, false); // timeout set on outer call
 
            Debug.Assert(null != _data, "no data columns?");                // should have been caught already.
            Debug.Assert(i < _data.Length, "reading beyond data length?");  // should have been caught already.

            object o; 

            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType) { 
                if (_data[i].IsNull) { 
                    return DBNull.Value;
                } 
                else {
                    return _data[i].KatmaiDateTimeString;
                }
            } 
            else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsLargeUdt) {
                o = _data[i].Value; 
            } 
            else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) {
                // TypeSystem.SQLServer2005 

                if (_metaData[i].type != SqlDbType.Udt) {
                    o = _data[i].Value;
                } 
                else {
                    SqlConnection.CheckGetExtendedUDTInfo(_metaData[i], true); 
                    o = Connection.GetUdtValue(_data[i].Value, _metaData[i], true); 
                }
            } 
            else {
                // TypeSystem.SQLServer2000

                o = _data[i].Value; 
            }
 
            return o; 
        }
 
        override public int GetValues(object[] values) {
            SqlStatistics statistics = null;
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                if (MetaData == null || !_dataReady)
                    throw SQL.InvalidRead(); 
 
                if (null == values) {
                    throw ADP.ArgumentNull("values"); 
                }

                int copyLen = (values.Length < _metaData.visibleColumns) ? values.Length : _metaData.visibleColumns;
 
                SetTimeout();
 
                for (int i = 0; i < copyLen; i++) { 
                    values[_metaData.indexMap[i]] = GetValueInternal(i);
                } 

                if (null != _rowException) {
                    throw _rowException;
                } 
                return copyLen;
            } 
            finally { 
                SqlStatistics.StopTimer(statistics);
            } 
        }

        private MetaType GetVersionedMetaType(MetaType actualMetaType) {
            Debug.Assert(_typeSystem == SqlConnectionString.TypeSystem.SQLServer2000, "Should not be in this function under anything else but SQLServer2000"); 

            MetaType metaType = null; 
 
            if      (actualMetaType == MetaType.MetaUdt) {
                metaType = MetaType.MetaVarBinary; 
            }
            else if (actualMetaType == MetaType.MetaXml) {
                metaType = MetaType.MetaNText;
            } 
            else if (actualMetaType == MetaType.MetaMaxVarBinary) {
                metaType = MetaType.MetaImage; 
            } 
            else if (actualMetaType == MetaType.MetaMaxVarChar) {
                metaType = MetaType.MetaText; 
            }
            else if (actualMetaType == MetaType.MetaMaxNVarChar) {
                metaType = MetaType.MetaNText;
            } 
            else {
                metaType = actualMetaType; 
            } 

            return metaType; 
        }

        private bool HasMoreResults() {
            if(null != _parser) { 
                if(HasMoreRows()) {
                    // When does this happen?  This is only called from NextResult(), which loops until Read() false. 
                    return true; 
                }
 
                Debug.Assert(null != _command, "unexpected null command from the data reader!");

                while(_stateObj._pendingData) {
                    byte token = _stateObj.PeekByte(); 

                    switch(token) { 
                        case TdsEnums.SQLALTROW: 
                            if(_altRowStatus == ALTROWSTATUS.Null) {
                                // cache the regular metadata 
                                _altMetaDataSetCollection.metaDataSet = _metaData;
                                _metaData = null;
                            }
                            else { 
                                Debug.Assert(_altRowStatus == ALTROWSTATUS.Done, "invalid AltRowStatus");
                            } 
                            _altRowStatus = ALTROWSTATUS.AltRow; 
                            _hasRows = true;
                            return true; 
                        case TdsEnums.SQLROW:
                            // always happens if there is a row following an altrow
                            return true;
                        case TdsEnums.SQLDONE: 
                            Debug.Assert(_altRowStatus == ALTROWSTATUS.Done || _altRowStatus == ALTROWSTATUS.Null, "invalid AltRowStatus");
                            _altRowStatus = ALTROWSTATUS.Null; 
                            _metaData = null; 
                            _altMetaDataSetCollection = null;
                            return true; 
                        case TdsEnums.SQLCOLMETADATA:
                            return true;
                    }
                    _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj); 
                }
            } 
            return false; 
        }
 
        private bool HasMoreRows() {
            if (null != _parser) {
                if (_dataReady) {
                    return true; 
                }
 
                // NextResult: previous call to NextResult started to process the altrowpackage, can't peek anymore 
                // Read: Read prepared for final processing of altrow package, No more Rows until NextResult ...
                // Done: Done processing the altrow, no more rows until NextResult ... 
                switch (_altRowStatus) {
                    case ALTROWSTATUS.AltRow:
                        return true;
                    case ALTROWSTATUS.Done: 
                        return false;
                } 
                if (_stateObj._pendingData) { 
                    // Consume error's, info's, done's on HasMoreRows, so user obtains error on Read.
                    // Previous bug where Read() would return false with error on the wire in the case 
                    // of metadata and error immediately following.  See MDAC 78285 and 75225.

                    //
 

 
 

 

                    // process any done, doneproc and doneinproc token streams and
                    // any order, error or info token preceeding the first done, doneproc or doneinproc token stream
                    byte b = _stateObj.PeekByte(); 
                    bool ParsedDoneToken = false;
 
                    while ( b == TdsEnums.SQLDONE || 
                            b == TdsEnums.SQLDONEPROC   ||
                            b == TdsEnums.SQLDONEINPROC || 
                            !ParsedDoneToken && b == TdsEnums.SQLORDER  ||
                            !ParsedDoneToken && b == TdsEnums.SQLERROR  ||
                            !ParsedDoneToken && b == TdsEnums.SQLINFO ) {
 
                        if (b == TdsEnums.SQLDONE ||
                            b == TdsEnums.SQLDONEPROC   || 
                            b == TdsEnums.SQLDONEINPROC) { 
                            ParsedDoneToken = true;
                        } 

                        _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj);
                        if ( _stateObj._pendingData) {
                            b = _stateObj.PeekByte(); 
                        }
                        else { 
                            break; 
                        }
                    } 

                    // Only return true when we are positioned on row b.
                    if (TdsEnums.SQLROW == b)
                        return true; 
                }
            } 
            return false; 
        }
 
        override public bool IsDBNull(int i) {
            SetTimeout();
            ReadColumnHeader(i);    // header data only
            return _data[i].IsNull; 
        }
 
        protected bool IsCommandBehavior(CommandBehavior condition) { 
            return (condition == (condition & _commandBehavior));
        } 

        // recordset is automatically positioned on the first result set
        override public bool NextResult() {
            SqlStatistics statistics = null; 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlDataReader.NextResult|API> %d#", ObjectID); 
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG 
                    statistics = SqlStatistics.StartTimer(Statistics);
 
                    SetTimeout();

                    if (IsClosed) {
                        throw ADP.DataReaderClosed("NextResult"); 
                    }
                    _fieldNameLookup = null; 
 
                    bool success = false; // WebData 100390
                    _hasRows = false; // reset HasRows 

                    // if we are specifically only processing a single result, then read all the results off the wire and detach
                    if (IsCommandBehavior(CommandBehavior.SingleResult)) {
                        CloseInternal(false /*closeReader*/); 

                        // In the case of not closing the reader, null out the metadata AFTER 
                        // CloseInternal finishes - since CloseInternal may go to the wire 
                        // and use the metadata.
                        ClearMetaData(); 
                        return success;
                    }

                    if (null != _parser) { 
                        // if there are more rows, then skip them, the user wants the next result
                        while (ReadInternal(false)) { // don't reset set the timeout value 
                            ; // intentional 
                        }
                    } 

                    // we may be done, so continue only if we have not detached ourselves from the parser
                    if (null != _parser) {
                        if (HasMoreResults()) { 
                            _metaDataConsumed = false;
                            _browseModeInfoConsumed = false; 
 
                            switch (_altRowStatus) {
                                case ALTROWSTATUS.AltRow: 
                                    int altRowId = _parser.GetAltRowId(_stateObj);
                                    _SqlMetaDataSet altMetaDataSet = _altMetaDataSetCollection[altRowId];
                                    if (altMetaDataSet != null) {
                                        _metaData = altMetaDataSet; 
                                        _metaData.indexMap = altMetaDataSet.indexMap;
                                    } 
                                    Debug.Assert ((_metaData != null), "Can't match up altrowmetadata"); 
                                    break;
                                case ALTROWSTATUS.Done: 
                                    // restore the row-metaData
                                    _metaData = _altMetaDataSetCollection.metaDataSet;
                                    Debug.Assert (_altRowStatus == ALTROWSTATUS.Done, "invalid AltRowStatus");
                                    _altRowStatus = ALTROWSTATUS.Null; 
                                    break;
                                default: 
                                    ConsumeMetaData(); 
                                    if (_metaData == null) {
                                        return false; 
                                    }
                                    break;
                            }
 
                            success = true;
                        } 
                        else { 
                            // detach the parser from this reader now
                            CloseInternal(false /*closeReader*/); 

                            // In the case of not closing the reader, null out the metadata AFTER
                            // CloseInternal finishes - since CloseInternal may go to the wire
                            // and use the metadata. 
                            SetMetaData(null, false);
                        } 
                    } 
                    else {
                        // Clear state in case of Read calling CloseInternal() then user calls NextResult() 
                        // MDAC 81986.  Or, also the case where the Read() above will do essentially the same
                        // thing.
                        ClearMetaData();
                    } 

                    return success; 
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _isClosed = true; 
                if (null != _connection) { 
                    _connection.Abort(e);
                } 
                throw;
            }
            catch (System.StackOverflowException e) {
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                } 
                throw;
            } 
            catch (System.Threading.ThreadAbortException e)  {
                _isClosed = true;
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw; 
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp);
            }
        }
 
        // user must call Read() to position on the first row
        override public bool Read() { 
            return ReadInternal(true); 
        }
 
        // user must call Read() to position on the first row
        private bool ReadInternal(bool setTimeout) {
            SqlStatistics statistics = null;
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlDataReader.Read|API> %d#", ObjectID);
 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    statistics = SqlStatistics.StartTimer(Statistics); 

                    if (null != _parser) { 
                        if (setTimeout) {
                            SetTimeout();
                        }
                        if (_dataReady) { 
                            CleanPartialRead();
                        } 
                        // clear out our buffers 
                        _dataReady = false;
                        SqlBuffer.Clear(_data); 

                        _nextColumnHeaderToRead = 0;
                        _nextColumnDataToRead = 0;
                        _columnDataBytesRemaining = -1; // unknown 

                        if (!_haltRead) { 
                            if (HasMoreRows()) { 
                                // read the row from the backend (unless it's an altrow were the marker is already inside the altrow ...)
                                while (_stateObj._pendingData) { 
                                    if (_altRowStatus != ALTROWSTATUS.AltRow) {
                                        // if this is an ordinary row we let the run method consume the ROW token
                                        _dataReady = _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj);
                                        if (_dataReady) { 
                                            break;
                                        } 
                                    } 
                                    else {
                                        // ALTROW token and AltrowId are already consumed ... 
                                        Debug.Assert (_altRowStatus == ALTROWSTATUS.AltRow, "invalid AltRowStatus");
                                        _altRowStatus = ALTROWSTATUS.Done;
                                        _dataReady = true;
                                        break; 
                                    }
                                } 
                                if (_dataReady) { 
                                    _haltRead = IsCommandBehavior(CommandBehavior.SingleRow);
                                    return true; 
                                }
                            }

                            if (!_stateObj._pendingData) { 
                                CloseInternal(false /*closeReader*/);
                            } 
                        } 
                        else {
                            // if we did not get a row and halt is true, clean off rows of result 
                            // success must be false - or else we could have just read off row and set
                            // halt to true
                            while (HasMoreRows()) {
                                // if we are in SingleRow mode, and we've read the first row, 
                                // read the rest of the rows, if any
                                while (_stateObj._pendingData && !_dataReady) { 
                                    _dataReady = _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj); 
                                }
 
                                if (_dataReady) {
                                    CleanPartialRead();
                                }
 
                                // clear out our buffers
                                _dataReady = false; 
                                SqlBuffer.Clear(_data); 

                                _nextColumnHeaderToRead = 0; 
                            }

                            // reset haltRead
                            _haltRead = false; 
                         }
                    } 
                    else if (IsClosed) { 
                        throw ADP.DataReaderClosed("Read");
                    } 

                    return false;
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _isClosed = true;
                SqlConnection con = _connection;
                if (con != null) { 
                    con.Abort(e);
                } 
                throw; 
            }
            catch (System.StackOverflowException e) { 
                _isClosed = true;
                SqlConnection con = _connection;
                if (con != null) {
                    con.Abort(e); 
                }
                throw; 
            } 
            catch (System.Threading.ThreadAbortException e)  {
               _isClosed = true; 
                SqlConnection con = _connection;
                if (con != null) {
                    con.Abort(e);
                } 
                throw;
            } 
            finally { 
                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp); 
            }
        }

        private void ReadColumn(int i) { 
            ReadColumn(i, true);
        } 
 
        private void ReadColumn(int i, bool setTimeout) {
            if (MetaData == null || !_dataReady) { 
                throw SQL.InvalidRead();
            }

            if (0 > i || i >= _metaData.Length) {   // _metaData can't be null if we don't throw above. 
                throw new IndexOutOfRangeException();
            } 
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG 
                    Debug.Assert(_nextColumnHeaderToRead <= _metaData.Length, "_nextColumnHeaderToRead too large");
                    Debug.Assert(_nextColumnDataToRead <= _metaData.Length, "_nextColumnDataToRead too large"); 

                    if (setTimeout) {
                        SetTimeout();
                    } 
                    if (_nextColumnHeaderToRead <= i) {
                        ReadColumnHeader(i); 
                    } 
                    if (_nextColumnDataToRead == i) {
                        ReadColumnData(); 
                    }
                    else if (_nextColumnDataToRead > i) {
                        // We've already read/skipped over this column header.
 
                        // CommandBehavior.SequentialAccess: allow sequential, non-repeatable
                        // reads.  If we specify a column that we've already read, error 
                        if (IsCommandBehavior(CommandBehavior.SequentialAccess)) { 
                            throw ADP.NonSequentialColumnAccess(i, _nextColumnDataToRead);
                        } 
                    }
                    Debug.Assert(null != _data[i], " data buffer is null?");
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _isClosed = true;
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw; 
            } 
            catch (System.StackOverflowException e) {
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e);
                }
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw;
            }
        } 

        private void ReadColumnData() { 
            // If we've already read the value (because it was NULL) we don't 
            // bother to read here.
            if (!_data[_nextColumnDataToRead].IsNull) { 
                _SqlMetaData columnMetaData = _metaData[_nextColumnDataToRead];

                _parser.ReadSqlValue(_data[_nextColumnDataToRead], columnMetaData, (int)_columnDataBytesRemaining, _stateObj); // will read UDTs as VARBINARY.
                _columnDataBytesRemaining = 0; 
            }
            _nextColumnDataToRead++; 
        } 

        private void ReadColumnHeader(int i) { 
            if (!_dataReady) {
                throw SQL.InvalidRead();
            }
 
            Debug.Assert (i < _data.Length, "reading past end of data buffer?");
 
            if (i < _nextColumnDataToRead) { 
                return;
            } 

            Debug.Assert(_data[i].IsEmpty, "re-reading column value?");

            bool skippingColumnData = IsCommandBehavior(CommandBehavior.SequentialAccess); 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    // If we're in sequential access mode, we can safely clear out any 
                    // data from the previous column. 
                    if (skippingColumnData) {
                        if (0 < _nextColumnDataToRead) { 
                            _data[_nextColumnDataToRead-1].Clear();
                        }
                    }
                    else if (_nextColumnDataToRead < _nextColumnHeaderToRead) { 
                        // We read the header but not the column for the previous column
                        ReadColumnData(); 
                        Debug.Assert(_nextColumnDataToRead == _nextColumnHeaderToRead); 
                    }
 
                    while (_nextColumnHeaderToRead <= i) {
                        // if we still have bytes left from the previous blob read, clear
                        // the wire and reset
                        ResetBlobState(); 

                        // Turn off column skipping once we reach the actual column 
                        // we're supposed to read. 
                        if (skippingColumnData) {
                            skippingColumnData = (_nextColumnHeaderToRead < i); 
                        }

                        _SqlMetaData columnMetaData = _metaData[_nextColumnHeaderToRead];
                        if (skippingColumnData && columnMetaData.metaType.IsPlp) { 
                            _parser.SkipPlpValue(UInt64.MaxValue, _stateObj);
                            _nextColumnDataToRead = _nextColumnHeaderToRead; 
                            _nextColumnHeaderToRead++; 
                            _columnDataBytesRemaining = 0;
                        } 
                        else {
                            bool isNull = false;
                            ulong dataLength = _parser.ProcessColumnHeader(columnMetaData, _stateObj, out isNull);
 
                            _nextColumnDataToRead = _nextColumnHeaderToRead;
                            _nextColumnHeaderToRead++;  // We read this one 
 
                            if (skippingColumnData) {
                                _parser.SkipLongBytes(dataLength, _stateObj); 
                                _columnDataBytesRemaining = 0;
                            }
                            else if (isNull) {
                                _parser.GetNullSqlValue(_data[_nextColumnDataToRead], columnMetaData); 
                                _columnDataBytesRemaining = 0;
                            } 
                            else { 
                                _columnDataBytesRemaining = (long)dataLength;
 
                                if (i > _nextColumnDataToRead) {
                                    // If we're not in sequential access mode, we have to
                                    // save the data we skip over so that the consumer
                                    // can read it out of order 
                                    ReadColumnData();
                                } 
                            } 
                        }
                    } 
#if DEBUG
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG 
            } 
            catch (System.OutOfMemoryException e) {
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e);
                }
                throw; 
            }
            catch (System.StackOverflowException e) { 
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw;
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _isClosed = true;
                if (null != _connection) { 
                    _connection.Abort(e); 
                }
                throw; 
            }
        }

 
        // clean remainder bytes for the column off the wire
        private void ResetBlobState() { 
            Debug.Assert(null != _stateObj, "null state object"); // _parser may be null at this point 
            Debug.Assert(_nextColumnHeaderToRead <= _metaData.Length, "_nextColumnHeaderToRead too large");
            int currentColumn = _nextColumnHeaderToRead - 1; 
            if ((currentColumn >= 0) && _metaData[currentColumn].metaType.IsPlp) {
                if (_stateObj._longlen != 0) {
                    _stateObj.Parser.SkipPlpValue(UInt64.MaxValue, _stateObj);
                } 
                if (_streamingXml != null) {
                    SqlStreamingXml localSXml = _streamingXml; 
                    _streamingXml = null; 
                    localSXml.Close();
                } 
            }
            else if (0 < _columnDataBytesRemaining) {
                    _stateObj.Parser.SkipLongBytes((ulong)_columnDataBytesRemaining, _stateObj);
            } 

            _columnDataBytesRemaining = -1; // unknown 
            _columnDataBytesRead = 0; 
            _columnDataCharsRead = 0;
            _columnDataChars = null; 
        }

        private void RestoreServerSettings(TdsParser parser, TdsParserStateObject stateObj) {
            // turn off any set options 
            if (null != parser && null != _resetOptionsString) {
                // It is possible for this to be called during connection close on a 
                // broken connection, so check state first. 
                if (parser.State == TdsParserState.OpenLoggedIn) {
                    parser.TdsExecuteSQLBatch(_resetOptionsString, (_command != null) ? _command.CommandTimeout : 0, null, stateObj); 
                    parser.Run(RunBehavior.UntilDone, _command, this, null, stateObj);
                }
                _resetOptionsString = null;
            } 
        }
 
        internal void SetAltMetaDataSet(_SqlMetaDataSet metaDataSet, bool metaDataConsumed) { 
            if (_altMetaDataSetCollection == null) {
                _altMetaDataSetCollection = new _SqlMetaDataSetCollection(); 
            }
            _altMetaDataSetCollection.Add(metaDataSet);
            _metaDataConsumed = metaDataConsumed;
            if (_metaDataConsumed) { 
                byte b = _stateObj.PeekByte();
                if (TdsEnums.SQLORDER == b) { 
                    _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj); 
                    b = _stateObj.PeekByte();
                } 
                _hasRows = (TdsEnums.SQLROW == b);
            }
            if (metaDataSet != null) {
                if (_data == null || _data.Length<metaDataSet.Length) { 
                    _data = SqlBuffer.CreateBufferArray(metaDataSet.Length);
                } 
            } 
        }
 
        private void ClearMetaData() {
            _metaData = null;
            _tableNames = null;
            _fieldNameLookup = null; 
            _metaDataConsumed = false;
            _browseModeInfoConsumed = false; 
        } 

        internal void SetMetaData(_SqlMetaDataSet metaData, bool moreInfo) { 
            _metaData = metaData;

            // get rid of cached metadata info as well
            _tableNames = null; 
            if (_metaData != null) {
                _metaData.schemaTable = null; 
                _data = SqlBuffer.CreateBufferArray(metaData.Length); 
            }
 
            _fieldNameLookup = null;

            if (null != metaData) {
                // we are done consuming metadata only if there is no moreInfo 
                if (!moreInfo) {
                    _metaDataConsumed = true; 
 
                    if (_parser != null) { // There is a valid case where parser is null
                        // Peek, and if row token present, set _hasRows true since there is a 
                        // row in the result
                        byte b = _stateObj.PeekByte();

                        // 

 
                        // simply rip the order token off the wire 
                        if (b == TdsEnums.SQLORDER) {                     //  same logic as SetAltMetaDataSet
// Devnote: That's not the right place to process TDS 
// Can this result in Reentrance to Run?
//
                             _parser.Run(RunBehavior.ReturnImmediately, null, null, null, _stateObj);
                            b = _stateObj.PeekByte(); 
                        }
                        _hasRows = (TdsEnums.SQLROW == b); 
                        if (TdsEnums.SQLALTMETADATA == b) 
                        {
                            _metaDataConsumed = false; 
                        }
                    }
                }
            } 
            else {
                _metaDataConsumed = false; 
            } 

            _browseModeInfoConsumed = false; 
        }

        private void SetTimeout() {
            // WebData 111653,112003 -- we now set timeouts per operation, not 
            // per command (it's not supposed to be a cumulative per command).
            TdsParserStateObject stateObj = _stateObj; 
 
            if (null != stateObj) {
                stateObj.SetTimeoutSeconds(_timeoutSeconds); 
            }
        }

        // Used by SqlResultSet to avoid XML to string, and UDT object conversion. 
        internal object GetSqlValueWithNoConvert(int i) {
           if (MetaData == null || !_dataReady) 
                throw SQL.InvalidRead(); 

            ReadColumn(i, false); 

            Debug.Assert(null != _data, "no data columns?");                // should have been caught already.
            Debug.Assert(i < _data.Length, "reading beyond data length?");  // should have been caught already.
 
            object o = null;
 
            if (_metaData[i].type == SqlDbType.Xml) { 
                // Return SqlCachedBuffer instead of string
                o = _data[i].SqlCachedBuffer; 
            } else {
                // For UDTs, this returns SqlBinary
                o = _data[i].SqlValue;
            } 

            return o; 
        } 
    }// SqlDataReader
}// namespace 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataReader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="true" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.SqlClient { 
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel; 
    using System.Data;
    using System.Data.Sql; 
    using System.Data.SqlTypes; 
    using System.Data.Common;
    using System.Data.ProviderBase; 
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection; 
    using System.Runtime.CompilerServices;
    using System.Threading; 
    using System.Xml; 

    using Microsoft.SqlServer.Server; 

#if WINFSInternalOnly
    internal
#else 
    public
#endif 
    class SqlDataReader : DbDataReader, IDataReader { 

        private enum ALTROWSTATUS { 
            Null = 0,           // default and after Done
            AltRow,             // after calling NextResult and the first AltRow is available for read
            Done,               // after consuming the value (GetValue -> GetValueInternal)
        } 

        private TdsParser                      _parser;                 // 
        private TdsParserStateObject           _stateObj; 
        private SqlCommand                     _command;
        private SqlConnection                  _connection; 
        private int                            _defaultLCID;
        private bool                           _dataReady;              // ready to ProcessRow
        private bool                           _haltRead;               // bool to denote whether we have read first row for single row behavior
        private bool                           _metaDataConsumed; 
        private bool                           _browseModeInfoConsumed;
        private bool                           _isClosed; 
        private bool                           _isInitialized;          // Webdata 104560 
        private bool                           _hasRows;
        private ALTROWSTATUS                   _altRowStatus; 
        private int                            _recordsAffected = -1;
        private int                            _timeoutSeconds;
        private SqlConnectionString.TypeSystem _typeSystem;
 
        // SQLStatistics support
        private SqlStatistics   _statistics; 
        private SqlBuffer[]     _data;         // row buffer, filled in by ReadColumnData() 
        private SqlStreamingXml _streamingXml; // Used by Getchars on an Xml column for sequential access
 
        // buffers and metadata
        private _SqlMetaDataSet           _metaData;                 // current metaData for the stream, it is lazily loaded
        private _SqlMetaDataSetCollection _altMetaDataSetCollection;
        private FieldNameLookup           _fieldNameLookup; 
        private CommandBehavior           _commandBehavior;
 
        private  static int   _objectTypeCount; // Bid counter 
        internal readonly int ObjectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);
 
        // context
        // undone: we may still want to do this...it's nice to pass in an lpvoid (essentially) and just have the reader keep the state
        // private object _context = null; // this is never looked at by the stream object.  It is used by upper layers who wish
        // to remain stateless 

        // metadata (no explicit table, use 'Table') 
        private MultiPartTableName[] _tableNames = null; 
        private string               _resetOptionsString;
 
        private int    _nextColumnDataToRead;
        private int    _nextColumnHeaderToRead;
        private long   _columnDataBytesRead;       // last byte read by user
        private long   _columnDataBytesRemaining; 
        private long   _columnDataCharsRead;       // last char read by user
        private char[] _columnDataChars; 
 
        // handle exceptions that occur when reading a value mid-row
        private Exception _rowException; 

        internal SqlDataReader(SqlCommand command, CommandBehavior behavior) {
            SqlConnection.VerifyExecutePermission();
 
            _command = command;
            _commandBehavior = behavior; 
            if (_command != null) { 
                _timeoutSeconds = command.CommandTimeout;
                _connection = command.Connection; 
                if (_connection != null) {
                    _statistics = _connection.Statistics;
                    _typeSystem = _connection.TypeSystem;
                } 
            }
            _dataReady = false; 
            _metaDataConsumed = false; 
            _hasRows = false;
            _browseModeInfoConsumed = false; 
        }

        internal bool BrowseModeInfoConsumed {
            set { 
                _browseModeInfoConsumed = value;
            } 
        } 

        internal SqlCommand Command { 
            get {
                return _command;
            }
        } 

        protected SqlConnection Connection { 
            get { 
                return _connection;
            } 
        }

        override public int Depth {
            get { 
                if (this.IsClosed) {
                    throw ADP.DataReaderClosed("Depth"); 
                } 

                return 0; 
            }
        }

        // fields/attributes collection 
        override public int FieldCount {
            get { 
                if (this.IsClosed) { 
                    throw ADP.DataReaderClosed("FieldCount");
                } 

                if (MetaData == null) {
                    return 0;
                } 

                return _metaData.Length; 
            } 
        }
 
        override public bool HasRows {
            get {
                if (this.IsClosed) {
                    throw ADP.DataReaderClosed("HasRows"); 
                }
 
                return _hasRows; 
            }
        } 

        override public bool IsClosed {
            get {
                return _isClosed; 
            }
        } 
 
        internal bool IsInitialized {
            get { 
                return _isInitialized;
            }
            set {
                Debug.Assert(value, "attempting to uninitialize a data reader?"); 
                _isInitialized = value;
            } 
        } 

        internal _SqlMetaDataSet MetaData { 
            get {
                if (IsClosed) {
                    throw ADP.DataReaderClosed("MetaData");
                } 
                // metaData comes in pieces: colmetadata, tabname, colinfo, etc
                // if we have any metaData, return it.  If we have none, 
                // then fetch it 
                if (_metaData == null && !_metaDataConsumed) {
                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try {
#if DEBUG
                        object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try { 
                            Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                            ConsumeMetaData(); 
#if DEBUG
                        }
                        finally {
                            Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                        }
#endif //DEBUG 
                    } 
                    catch (System.OutOfMemoryException e) {
                        _isClosed = true; 
                        if (null != _connection) {
                            _connection.Abort(e);
                        }
                        throw; 
                    }
                    catch (System.StackOverflowException e) { 
                        _isClosed = true; 
                        if (null != _connection) {
                            _connection.Abort(e); 
                        }
                        throw;
                    }
                    catch (System.Threading.ThreadAbortException e)  { 
                        _isClosed = true;
                        if (null != _connection) { 
                            _connection.Abort(e); 
                        }
                        throw; 
                    }
                }
                return _metaData;
            } 
        }
 
        internal virtual SmiExtendedMetaData[] GetInternalSmiMetaData() { 
            SmiExtendedMetaData[] metaDataReturn = null;
            _SqlMetaDataSet metaData = this.MetaData; 

            if ( null != metaData && 0 < metaData.Length ) {
                metaDataReturn = new SmiExtendedMetaData[metaData.visibleColumns];
 
                for( int index=0; index < metaData.Length; index++ ) {
                    _SqlMetaData colMetaData = metaData[index]; 
 
                    if ( !colMetaData.isHidden ) {
                        SqlCollation collation = colMetaData.collation; 

                        string typeSpecificNamePart1 = null;
                        string typeSpecificNamePart2 = null;
                        string typeSpecificNamePart3 = null; 

                        if (SqlDbType.Xml == colMetaData.type) { 
                            typeSpecificNamePart1 = colMetaData.xmlSchemaCollectionDatabase; 
                            typeSpecificNamePart2 = colMetaData.xmlSchemaCollectionOwningSchema;
                            typeSpecificNamePart3 = colMetaData.xmlSchemaCollectionName; 
                        }
                        else if (SqlDbType.Udt == colMetaData.type) {
                            SqlConnection.CheckGetExtendedUDTInfo(colMetaData, true);    //
 
                            typeSpecificNamePart1 = colMetaData.udtDatabaseName;
                            typeSpecificNamePart2 = colMetaData.udtSchemaName; 
                            typeSpecificNamePart3 = colMetaData.udtTypeName; 
                        }
 
                        int length = colMetaData.length;
                        if ( length > TdsEnums.MAXSIZE ) {
                            length = (int) SmiMetaData.UnlimitedMaxLengthIndicator;
                        } 
                        else if (SqlDbType.NChar == colMetaData.type
                                ||SqlDbType.NVarChar == colMetaData.type) { 
                            length /= ADP.CharSize; 
                        }
 
                        metaDataReturn[index] = new SmiQueryMetaData(
                                                        colMetaData.type,
                                                        length,
                                                        colMetaData.precision, 
                                                        colMetaData.scale,
                                                        (null != collation) ? collation.LCID : _defaultLCID, 
                                                        (null != collation) ? collation.SqlCompareOptions : SqlCompareOptions.None, 
                                                        colMetaData.udtType,
                                                        false,  // isMultiValued 
                                                        null,   // fieldmetadata
                                                        null,   // extended properties
                                                        colMetaData.column,
                                                        typeSpecificNamePart1, 
                                                        typeSpecificNamePart2,
                                                        typeSpecificNamePart3, 
                                                        colMetaData.isNullable, 
                                                        colMetaData.serverName,
                                                        colMetaData.catalogName, 
                                                        colMetaData.schemaName,
                                                        colMetaData.tableName,
                                                        colMetaData.baseColumn,
                                                        colMetaData.isKey, 
                                                        colMetaData.isIdentity,
                                                        0==colMetaData.updatability, 
                                                        colMetaData.isExpression, 
                                                        colMetaData.isDifferentName,
                                                        colMetaData.isHidden 
                                                        );
                    }
                }
            } 

            return metaDataReturn; 
        } 

        override public int RecordsAffected { 
            get {
                if (null != _command)
                    return _command.InternalRecordsAffected;
 
                // cached locally for after Close() when command is nulled out
                return _recordsAffected; 
            } 
        }
 
        internal string ResetOptionsString {
            set {
                _resetOptionsString = value;
            } 
        }
 
        private SqlStatistics Statistics { 
            get {
                return _statistics; 
            }
        }

        internal MultiPartTableName[] TableNames { 
            get {
                return _tableNames; 
            } 
            set {
                _tableNames = value; 
            }
        }

        override public int VisibleFieldCount { 
            get {
                if (this.IsClosed) { 
                    throw ADP.DataReaderClosed("VisibleFieldCount"); 
                }
                if (MetaData == null) { 
                    return 0;
                }
                return (MetaData.visibleColumns);
            } 
        }
 
        // this operator 
        override public object this[int i] {
            get { 
                return GetValue(i);
            }
        }
 
        override public object this[string name] {
            get { 
                return GetValue(GetOrdinal(name)); 
            }
        } 

        internal void Bind(TdsParserStateObject stateObj) {
            Debug.Assert(null != stateObj, "null stateobject");
 
            stateObj.Owner = this;
            _stateObj    = stateObj; 
            _parser      = stateObj.Parser; 
            _defaultLCID = _parser.DefaultLCID;
        } 

        // Fills in a schema table with meta data information.  This function should only really be called by
        //
 
        internal DataTable BuildSchemaTable() {
            _SqlMetaDataSet md = this.MetaData; 
            Debug.Assert(null != md, "BuildSchemaTable - unexpected null metadata information"); 

            DataTable schemaTable = new DataTable("SchemaTable"); 
            schemaTable.Locale = CultureInfo.InvariantCulture;
            schemaTable.MinimumCapacity = md.Length;

            DataColumn ColumnName                       = new DataColumn(SchemaTableColumn.ColumnName,                       typeof(System.String)); 
            DataColumn Ordinal                          = new DataColumn(SchemaTableColumn.ColumnOrdinal,                    typeof(System.Int32));
            DataColumn Size                             = new DataColumn(SchemaTableColumn.ColumnSize,                       typeof(System.Int32)); 
            DataColumn Precision                        = new DataColumn(SchemaTableColumn.NumericPrecision,                 typeof(System.Int16)); 
            DataColumn Scale                            = new DataColumn(SchemaTableColumn.NumericScale,                     typeof(System.Int16));
 
            DataColumn DataType                         = new DataColumn(SchemaTableColumn.DataType,                         typeof(System.Type));
            DataColumn ProviderSpecificDataType         = new DataColumn(SchemaTableOptionalColumn.ProviderSpecificDataType, typeof(System.Type));
            DataColumn NonVersionedProviderType         = new DataColumn(SchemaTableColumn.NonVersionedProviderType,         typeof(System.Int32));
            DataColumn ProviderType                     = new DataColumn(SchemaTableColumn.ProviderType,                     typeof(System.Int32)); 

            DataColumn IsLong                           = new DataColumn(SchemaTableColumn.IsLong,                           typeof(System.Boolean)); 
            DataColumn AllowDBNull                      = new DataColumn(SchemaTableColumn.AllowDBNull,                      typeof(System.Boolean)); 
            DataColumn IsReadOnly                       = new DataColumn(SchemaTableOptionalColumn.IsReadOnly,               typeof(System.Boolean));
            DataColumn IsRowVersion                     = new DataColumn(SchemaTableOptionalColumn.IsRowVersion,             typeof(System.Boolean)); 

            DataColumn IsUnique                         = new DataColumn(SchemaTableColumn.IsUnique,                         typeof(System.Boolean));
            DataColumn IsKey                            = new DataColumn(SchemaTableColumn.IsKey,                            typeof(System.Boolean));
            DataColumn IsAutoIncrement                  = new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement,          typeof(System.Boolean)); 
            DataColumn IsHidden                         = new DataColumn(SchemaTableOptionalColumn.IsHidden,                 typeof(System.Boolean));
 
            DataColumn BaseCatalogName                  = new DataColumn(SchemaTableOptionalColumn.BaseCatalogName,          typeof(System.String)); 
            DataColumn BaseSchemaName                   = new DataColumn(SchemaTableColumn.BaseSchemaName,                   typeof(System.String));
            DataColumn BaseTableName                    = new DataColumn(SchemaTableColumn.BaseTableName,                    typeof(System.String)); 
            DataColumn BaseColumnName                   = new DataColumn(SchemaTableColumn.BaseColumnName,                   typeof(System.String));

            // unique to SqlClient
            DataColumn BaseServerName                   = new DataColumn(SchemaTableOptionalColumn.BaseServerName,           typeof(System.String)); 
            DataColumn IsAliased                        = new DataColumn(SchemaTableColumn.IsAliased,                        typeof(System.Boolean));
            DataColumn IsExpression                     = new DataColumn(SchemaTableColumn.IsExpression,                     typeof(System.Boolean)); 
            DataColumn IsIdentity                       = new DataColumn("IsIdentity",                                       typeof(System.Boolean)); 
            DataColumn DataTypeName                     = new DataColumn("DataTypeName",                                     typeof(System.String));
            DataColumn UdtAssemblyQualifiedName         = new DataColumn("UdtAssemblyQualifiedName",                         typeof(System.String)); 
            // Xml metadata specific
            DataColumn XmlSchemaCollectionDatabase      = new DataColumn("XmlSchemaCollectionDatabase",                      typeof(System.String));
            DataColumn XmlSchemaCollectionOwningSchema  = new DataColumn("XmlSchemaCollectionOwningSchema",                  typeof(System.String));
            DataColumn XmlSchemaCollectionName          = new DataColumn("XmlSchemaCollectionName",                          typeof(System.String)); 

            Ordinal.DefaultValue = 0; 
            IsLong.DefaultValue = false; 

            DataColumnCollection columns = schemaTable.Columns; 

            // must maintain order for backward compatibility
            columns.Add(ColumnName);
            columns.Add(Ordinal); 
            columns.Add(Size);
            columns.Add(Precision); 
            columns.Add(Scale); 
            columns.Add(IsUnique);
            columns.Add(IsKey); 
            columns.Add(BaseServerName);
            columns.Add(BaseCatalogName);
            columns.Add(BaseColumnName);
            columns.Add(BaseSchemaName); 
            columns.Add(BaseTableName);
            columns.Add(DataType); 
            columns.Add(AllowDBNull); 
            columns.Add(ProviderType);
            columns.Add(IsAliased); 
            columns.Add(IsExpression);
            columns.Add(IsIdentity);
            columns.Add(IsAutoIncrement);
            columns.Add(IsRowVersion); 
            columns.Add(IsHidden);
            columns.Add(IsLong); 
            columns.Add(IsReadOnly); 
            columns.Add(ProviderSpecificDataType);
            columns.Add(DataTypeName); 
            columns.Add(XmlSchemaCollectionDatabase);
            columns.Add(XmlSchemaCollectionOwningSchema);
            columns.Add(XmlSchemaCollectionName);
            columns.Add(UdtAssemblyQualifiedName); 
            columns.Add(NonVersionedProviderType);
 
            for (int i = 0; i < md.Length; i++) { 
                _SqlMetaData col = md[i];
                DataRow schemaRow = schemaTable.NewRow(); 

                schemaRow[ColumnName] = col.column;
                schemaRow[Ordinal]    = col.ordinal;
                // 
                // be sure to return character count for string types, byte count otherwise
                // col.length is always byte count so for unicode types, half the length 
                // 
                // For MAX and XML datatypes, we get 0x7fffffff from the server. Do not divide this.
                schemaRow[Size] = (col.metaType.IsSizeInCharacters && (col.length != 0x7fffffff)) ? (col.length / 2) : col.length; 

                schemaRow[DataType]                 = GetFieldTypeInternal(col);
                schemaRow[ProviderSpecificDataType] = GetProviderSpecificFieldTypeInternal(col);
                schemaRow[NonVersionedProviderType] = (int) col.type; // SqlDbType enum value - does not change with TypeSystem. 
                schemaRow[DataTypeName]             = GetDataTypeNameInternal(col);
 
                if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && col.IsNewKatmaiDateTimeType) { 
                    schemaRow[ProviderType] = SqlDbType.NVarChar;
                    switch (col.type) { 
                        case SqlDbType.Date:
                            schemaRow[Size] = TdsEnums.WHIDBEY_DATE_LENGTH;
                            break;
                        case SqlDbType.Time: 
                            Debug.Assert(TdsEnums.UNKNOWN_PRECISION_SCALE == col.scale || (0 <= col.scale && col.scale <= 7), "Invalid scale for Time column: " + col.scale);
                            schemaRow[Size] = TdsEnums.WHIDBEY_TIME_LENGTH[TdsEnums.UNKNOWN_PRECISION_SCALE != col.scale ? col.scale : col.metaType.Scale]; 
                            break; 
                        case SqlDbType.DateTime2:
                            Debug.Assert(TdsEnums.UNKNOWN_PRECISION_SCALE == col.scale || (0 <= col.scale && col.scale <= 7), "Invalid scale for DateTime2 column: " + col.scale); 
                            schemaRow[Size] = TdsEnums.WHIDBEY_DATETIME2_LENGTH[TdsEnums.UNKNOWN_PRECISION_SCALE != col.scale ? col.scale : col.metaType.Scale];
                            break;
                        case SqlDbType.DateTimeOffset:
                            Debug.Assert(TdsEnums.UNKNOWN_PRECISION_SCALE == col.scale || (0 <= col.scale && col.scale <= 7), "Invalid scale for DateTimeOffset column: " + col.scale); 
                            schemaRow[Size] = TdsEnums.WHIDBEY_DATETIMEOFFSET_LENGTH[TdsEnums.UNKNOWN_PRECISION_SCALE != col.scale ? col.scale : col.metaType.Scale];
                            break; 
                    } 
                }
                else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && col.IsLargeUdt) { 
                    if (_typeSystem == SqlConnectionString.TypeSystem.SQLServer2005) {
                        schemaRow[ProviderType] = SqlDbType.VarBinary;
                    }
                    else { 
                        // TypeSystem.SQLServer2000
                        schemaRow[ProviderType] = SqlDbType.Image; 
                    } 
                }
                else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) { 
                    // TypeSystem.SQLServer2005 and above

                    // SqlDbType enum value - always the actual type for SQLServer2005.
                    schemaRow[ProviderType] = (int) col.type; 

                    if (col.type == SqlDbType.Udt) { // Additional metadata for UDTs. 
                        Debug.Assert(Connection.IsYukonOrNewer, "Invalid Column type received from the server"); 
                        schemaRow[UdtAssemblyQualifiedName] = col.udtAssemblyQualifiedName;
                    } 
                    else if (col.type == SqlDbType.Xml) { // Additional metadata for Xml.
                        Debug.Assert(Connection.IsYukonOrNewer, "Invalid DataType (Xml) for the column");
                        schemaRow[XmlSchemaCollectionDatabase]     = col.xmlSchemaCollectionDatabase;
                        schemaRow[XmlSchemaCollectionOwningSchema] = col.xmlSchemaCollectionOwningSchema; 
                        schemaRow[XmlSchemaCollectionName]         = col.xmlSchemaCollectionName;
                    } 
                } 
                else {
                    // TypeSystem.SQLServer2000 

                    // SqlDbType enum value - variable for certain types when SQLServer2000.
                    schemaRow[ProviderType] = GetVersionedMetaType(col.metaType).SqlDbType;
                } 

 
                if (TdsEnums.UNKNOWN_PRECISION_SCALE != col.precision) { 
                    schemaRow[Precision] = col.precision;
                } 
                else {
                    schemaRow[Precision] = col.metaType.Precision;
                }
 
                if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && col.IsNewKatmaiDateTimeType) {
                    schemaRow[Scale] = MetaType.MetaNVarChar.Scale; 
                } 
                else if (TdsEnums.UNKNOWN_PRECISION_SCALE != col.scale) {
                    schemaRow[Scale] = col.scale; 
                }
                else {
                    schemaRow[Scale] = col.metaType.Scale;
                } 

                schemaRow[AllowDBNull] = col.isNullable; 
 
                // If no ColInfo token received, do not set value, leave as null.
                if (_browseModeInfoConsumed) { 
                    schemaRow[IsAliased]    = col.isDifferentName;
                    schemaRow[IsKey]        = col.isKey;
                    schemaRow[IsHidden]     = col.isHidden;
                    schemaRow[IsExpression] = col.isExpression; 
                }
 
                schemaRow[IsIdentity] = col.isIdentity; 
                schemaRow[IsAutoIncrement] = col.isIdentity;
                schemaRow[IsLong] = col.metaType.IsLong; 

                // mark unique for timestamp columns
                if (SqlDbType.Timestamp == col.type) {
                    schemaRow[IsUnique] = true; 
                    schemaRow[IsRowVersion] = true;
                } 
                else { 
                    schemaRow[IsUnique] = false;
                    schemaRow[IsRowVersion] = false; 
                }

                schemaRow[IsReadOnly] = (0 == col.updatability);
 
                if (!ADP.IsEmpty(col.serverName)) {
                    schemaRow[BaseServerName] = col.serverName; 
                } 
                if (!ADP.IsEmpty(col.catalogName)) {
                    schemaRow[BaseCatalogName] = col.catalogName; 
                }
                if (!ADP.IsEmpty(col.schemaName)) {
                    schemaRow[BaseSchemaName] = col.schemaName;
                } 
                if (!ADP.IsEmpty(col.tableName)) {
                    schemaRow[BaseTableName] = col.tableName; 
                } 
                if (!ADP.IsEmpty(col.baseColumn)) {
                    schemaRow[BaseColumnName] = col.baseColumn; 
                }
                else if (!ADP.IsEmpty(col.column)) {
                    schemaRow[BaseColumnName] = col.column;
                } 

                schemaTable.Rows.Add(schemaRow); 
                schemaRow.AcceptChanges(); 
            }
 
            // mark all columns as readonly
            foreach(DataColumn column in columns) {
                column.ReadOnly = true; // MDAC 70943
            } 

            return schemaTable; 
        } 

        internal void Cancel(int objectID) { 
            TdsParserStateObject stateObj = _stateObj;
            if (null != stateObj) {
                stateObj.Cancel(objectID);
            } 
        }
 
        // wipe any data off the wire from a partial read 
        // and reset all pointers for sequential access
        private void CleanPartialRead() { 
            Debug.Assert(true == _dataReady, "invalid call to CleanPartialRead");

            // following cases for sequential read
            // i. user called read but didn't fetch anything 
            // iia. user called read and fetched a subset of the columns
            // iib. user called read and fetched a subset of the column data 
 
            // i. user called read but didn't fetch anything
            if (0 == _nextColumnHeaderToRead) { 
                _stateObj.Parser.SkipRow(_metaData, _stateObj);
            }
            else {
                // iia.  if we still have bytes left from a partially read column, skip 
                ResetBlobState();
 
                // iib. 
                // now read the remaining values off the wire for this row
                _stateObj.Parser.SkipRow(_metaData, _nextColumnHeaderToRead, _stateObj); 
            }
        }

        override public void Close() { 
            SqlStatistics statistics = null;
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlDataReader.Close|API> %d#", ObjectID); 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                if (IsClosed)
                    return;

                SetTimeout(); 

                CloseInternal(true /*closeReader*/); 
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp);
            }
        }
 
        private void CloseInternal(bool closeReader) {
            TdsParser parser = _parser; 
            TdsParserStateObject stateObj = _stateObj; 
            bool closeConnection = (IsCommandBehavior(CommandBehavior.CloseConnection));
            _parser = null; 
            bool aborting = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                    if (parser != null && stateObj != null && stateObj._pendingData) {
                        // It is possible for this to be called during connection close on a 
                        // broken connection, so check state first.
                        if (parser.State == TdsParserState.OpenLoggedIn) { 
                            // if user called read but didn't fetch any values, skip the row 
                            // same applies after NextResult on ALTROW because NextResult starts rowconsumption in that case ...
 
                            Debug.Assert(SniContext.Snix_Read==stateObj.SniContext, String.Format((IFormatProvider)null, "The SniContext should be Snix_Read but it actually is {0}", stateObj.SniContext));

                            if (_altRowStatus == ALTROWSTATUS.AltRow) {
                                _dataReady = true;      // set _dataReady to not confuse CleanPartialRead 
                            }
                            if (_dataReady) { 
                                CleanPartialRead(); 
                            }
                            parser.Run(RunBehavior.Clean, _command, this, null, stateObj); 
                        }
                    }
                    RestoreServerSettings(parser, stateObj);
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG 
            }
            catch (System.OutOfMemoryException e) {
                _isClosed = true;
                aborting = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                } 
                throw;
            } 
            catch (System.StackOverflowException e) {
                _isClosed = true;
                aborting = true;
                if (null != _connection) { 
                    _connection.Abort(e);
                } 
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _isClosed = true;
                aborting = true;
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw; 
            } 
            finally {
                if (aborting) { 
                    _isClosed = true;
                    _command = null; // we are done at this point, don't allow navigation to the connection
                    _connection = null;
                    _statistics = null; 
                }
                else { 
 
                    if (closeReader) {
                        _stateObj = null; 
                        _data = null;

                        //
 

 
 

 


                        if (Connection != null) {
                            Connection.RemoveWeakReference(this);  // This doesn't catch everything -- the connection may be closed, but it prevents dead readers from clogging the collection 
                        }
 
 
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try { 
#if DEBUG
                            object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                            RuntimeHelpers.PrepareConstrainedRegions(); 
                            try {
                                Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG 
                                if (null != _command) {
                                    if (null != stateObj) { 
                                        stateObj.CloseSession();
                                    }
                                }
#if DEBUG 
                            }
                            finally { 
                                Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                            }
#endif //DEBUG 
                        }
                        catch (System.OutOfMemoryException e) {
                            _isClosed = true;
                            aborting = true; 
                            if (null != _connection) {
                                _connection.Abort(e); 
                            } 
                            throw;
                        } 
                        catch (System.StackOverflowException e) {
                            _isClosed = true;
                            aborting = true;
                            if (null != _connection) { 
                                _connection.Abort(e);
                            } 
                            throw; 
                        }
                        catch (System.Threading.ThreadAbortException e)  { 
                            _isClosed = true;
                            aborting = true;
                            if (null != _connection) {
                                _connection.Abort(e); 
                            }
                            throw; 
                        } 

                        SetMetaData(null, false); 
                        _dataReady = false;
                        _isClosed = true;
                        _fieldNameLookup = null;
 
                        // if the user calls ExecuteReader(CommandBehavior.CloseConnection)
                        // then we close down the connection when we are done reading results 
                        if (closeConnection) { 
                            if (Connection != null) {
                                Connection.Close(); 
                            }
                        }
                        if (_command != null) {
                            // cache recordsaffected to be returnable after DataReader.Close(); 
                            _recordsAffected = _command.InternalRecordsAffected;
                        } 
 
                        _command = null; // we are done at this point, don't allow navigation to the connection
                        _connection = null; 
                        _statistics = null;
                    }
                }
            } 
        }
 
        internal void CloseReaderFromConnection() { 
            Close();
        } 

        private void ConsumeMetaData() {
            // warning:  Don't check the MetaData property within this function
            // warning:  as it will be a reentrant call 
            while (_parser != null && _stateObj != null && _stateObj._pendingData && !_metaDataConsumed) {
                _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj); 
            } 

            // we hide hidden columns from the user so build an internal map 
            // that compacts all hidden columns from the array
            if (null != _metaData) {
                _metaData.visibleColumns = 0;
 
                Debug.Assert(null == _metaData.indexMap, "non-null metaData indexmap");
                int[] indexMap = new int[_metaData.Length]; 
                for (int i = 0; i < indexMap.Length; ++i) { 
                    indexMap[i] = _metaData.visibleColumns;
 
                    if (!(_metaData[i].isHidden)) {
                        _metaData.visibleColumns++;
                    }
                } 
                _metaData.indexMap = indexMap;
            } 
        } 

        override public string GetDataTypeName(int i) { 
            SqlStatistics statistics = null;
            try {
                statistics = SqlStatistics.StartTimer(Statistics);
                if (MetaData == null) 
                    throw SQL.InvalidRead();
 
                return GetDataTypeNameInternal(_metaData[i]); 
            }
            finally { 
                SqlStatistics.StopTimer(statistics);
            }
        }
 
        private string GetDataTypeNameInternal(_SqlMetaData metaData) {
            string dataTypeName = null; 
 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsNewKatmaiDateTimeType) {
                dataTypeName = MetaType.MetaNVarChar.TypeName; 
            }
            else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsLargeUdt) {
                if (_typeSystem == SqlConnectionString.TypeSystem.SQLServer2005) {
                    dataTypeName = MetaType.MetaMaxVarBinary.TypeName; 
                }
                else { 
                    // TypeSystem.SQLServer2000 
                    dataTypeName = MetaType.MetaImage.TypeName;
                } 
            }
            else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) {
                // TypeSystem.SQLServer2005 and above
 
                if (metaData.type == SqlDbType.Udt) {
                    Debug.Assert(Connection.IsYukonOrNewer, "Invalid Column type received from the server"); 
                    dataTypeName = metaData.udtDatabaseName + "." + metaData.udtSchemaName + "." + metaData.udtTypeName; 
                }
                else { // For all other types, including Xml - use data in MetaType. 
                    dataTypeName = metaData.metaType.TypeName;
                }
            }
            else { 
                // TypeSystem.SQLServer2000
 
                dataTypeName = GetVersionedMetaType(metaData.metaType).TypeName; 
            }
 
            return dataTypeName;
        }

        override public IEnumerator GetEnumerator() { 
            return new DbEnumerator((IDataReader)this, IsCommandBehavior(CommandBehavior.CloseConnection));
        } 
 
        override public Type GetFieldType(int i) {
            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics);
                if (MetaData == null) {
                    throw SQL.InvalidRead(); 
                }
 
                return GetFieldTypeInternal(_metaData[i]); 
            }
            finally { 
                SqlStatistics.StopTimer(statistics);
            }
        }
 
        private Type GetFieldTypeInternal(_SqlMetaData metaData) {
            Type fieldType = null; 
 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsNewKatmaiDateTimeType) {
                // Return katmai types as string 
                fieldType = MetaType.MetaNVarChar.ClassType;
            }
            else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsLargeUdt) {
                if (_typeSystem == SqlConnectionString.TypeSystem.SQLServer2005) { 
                    fieldType = MetaType.MetaMaxVarBinary.ClassType;
                } 
                else { 
                    // TypeSystem.SQLServer2000
                    fieldType = MetaType.MetaImage.ClassType; 
                }
            }
            else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) {
                // TypeSystem.SQLServer2005 and above 

                if (metaData.type == SqlDbType.Udt) { 
                    Debug.Assert(Connection.IsYukonOrNewer, "Invalid Column type received from the server"); 
                    SqlConnection.CheckGetExtendedUDTInfo(metaData, false);
                    fieldType = metaData.udtType; 
                }
                else { // For all other types, including Xml - use data in MetaType.
                    fieldType = metaData.metaType.ClassType; // Com+ type.
                } 
            }
            else { 
                // TypeSystem.SQLServer2000 

                fieldType = GetVersionedMetaType(metaData.metaType).ClassType; // Com+ type. 
            }

            return fieldType;
        } 

        virtual internal int GetLocaleId(int i) { 
            _SqlMetaData sqlMetaData = MetaData[i]; 
            int lcid;
 
            if (sqlMetaData.collation != null) {
                lcid = sqlMetaData.collation.LCID;
            }
            else { 
                lcid = 0;
            } 
            return lcid; 
        }
 
        override public string GetName(int i) {
            if (MetaData == null) {
                throw SQL.InvalidRead();
            } 
            Debug.Assert(null != _metaData[i].column, "MDAC 66681");
            return _metaData[i].column; 
        } 

        override public Type GetProviderSpecificFieldType(int i) { 
            SqlStatistics statistics = null;
            try {
                statistics = SqlStatistics.StartTimer(Statistics);
                if (MetaData == null) { 
                    throw SQL.InvalidRead();
                } 
 
                return GetProviderSpecificFieldTypeInternal(_metaData[i]);
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        private Type GetProviderSpecificFieldTypeInternal(_SqlMetaData metaData) { 
            Type providerSpecificFieldType = null; 

            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsNewKatmaiDateTimeType) { 
                providerSpecificFieldType = MetaType.MetaNVarChar.SqlType;
            }
            else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && metaData.IsLargeUdt) {
                if (_typeSystem == SqlConnectionString.TypeSystem.SQLServer2005) { 
                    providerSpecificFieldType = MetaType.MetaMaxVarBinary.SqlType;
                } 
                else { 
                    // TypeSystem.SQLServer2000
                    providerSpecificFieldType = MetaType.MetaImage.SqlType; 
                }
            }
            else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) {
                // TypeSystem.SQLServer2005 and above 

                if (metaData.type == SqlDbType.Udt) { 
                    Debug.Assert(Connection.IsYukonOrNewer, "Invalid Column type received from the server"); 
                    SqlConnection.CheckGetExtendedUDTInfo(metaData, false);
                    providerSpecificFieldType = metaData.udtType; 
                }
                else { // For all other types, including Xml - use data in MetaType.
                    providerSpecificFieldType = metaData.metaType.SqlType; // SqlType type.
                } 
            }
            else { 
                // TypeSystem.SQLServer2000 

                providerSpecificFieldType = GetVersionedMetaType(metaData.metaType).SqlType; // SqlType type. 
            }

            return providerSpecificFieldType;
        } 

        // named field access 
        override public int GetOrdinal(string name) { 
            SqlStatistics statistics = null;
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                if (null == _fieldNameLookup) {
                    if (null == MetaData) {
                        throw SQL.InvalidRead(); 
                    }
                    _fieldNameLookup = new FieldNameLookup(this, _defaultLCID); 
                } 
                return _fieldNameLookup.GetOrdinal(name); // MDAC 71470
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        override public object GetProviderSpecificValue(int i) { 
            return GetSqlValue(i); 
        }
 
        override public int GetProviderSpecificValues(object[] values) {
            return GetSqlValues(values);
        }
 
        override public DataTable GetSchemaTable() {
            SqlStatistics statistics = null; 
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlDataReader.GetSchemaTable|API> %d#", ObjectID);
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                if (null == _metaData || null == _metaData.schemaTable) {
                    if (null != this.MetaData) {
 
                        _metaData.schemaTable = BuildSchemaTable();
                        Debug.Assert(null != _metaData.schemaTable, "No schema information yet!"); 
                        // filter table? 
                    }
                } 
                if (null != _metaData) {
                    return _metaData.schemaTable;
                }
                return null; 
            }
            finally { 
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp);
            } 
        }

        override public bool GetBoolean(int i) {
            ReadColumn(i); 
            return _data[i].Boolean;
        } 
 
        override public byte GetByte(int i) {
            ReadColumn(i); 
            return _data[i].Byte;
        }

        override public long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length) { 
            SqlStatistics statistics = null;
            long  cbBytes = 0; 
 

            if (MetaData == null || !_dataReady) 
                throw SQL.InvalidRead();

            // don't allow get bytes on non-long or non-binary columns
            MetaType mt = _metaData[i].metaType; 
            if (!(mt.IsLong || mt.IsBinType) || (SqlDbType.Xml == mt.SqlDbType)) {
                throw SQL.NonBlobColumn(_metaData[i].column); 
            } 

            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                SetTimeout();
                cbBytes = GetBytesInternal(i, dataIndex, buffer, bufferIndex, length);
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
            } 
            return cbBytes;
        } 


        // Used (indirectly) by SqlCommand.CompleteXmlReader
        virtual internal long GetBytesInternal(int i, long dataIndex, byte[] buffer, int bufferIndex, int length) { 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    int cbytes = 0;
 
                    // sequential reading 
                    if (IsCommandBehavior(CommandBehavior.SequentialAccess)) {
 
                        if (0 > i || i >= _metaData.Length) {   // _metaData can't be null if we don't throw above.
                            throw new IndexOutOfRangeException();
                        }
 
                        if (_nextColumnDataToRead > i) {
                            // We've already read/skipped over this column header. 
                            throw ADP.NonSequentialColumnAccess(i, _nextColumnDataToRead); 
                        }
 
                        if (_nextColumnHeaderToRead <= i) {
                            ReadColumnHeader(i);
                        }
 
                        // If data is null, ReadColumnHeader sets the data.IsNull bit.
                        if (_data[i] != null && _data[i].IsNull) { 
                            throw new SqlNullValueException(); 
                        }
 
                        if (0 == _columnDataBytesRemaining) {
                            return 0; // We've read this column to the end
                        }
 
                        // if no buffer is passed in, return the number total of bytes, or -1
                        if (null == buffer) { 
                            if (_metaData[i].metaType.IsPlp) { 
                                return (long) _parser.PlpBytesTotalLength(_stateObj);
                            } 
                            return _columnDataBytesRemaining;
                        }

                        if (dataIndex < 0) 
                            throw ADP.NegativeParameter("dataIndex");
 
                        if (dataIndex < _columnDataBytesRead) { 
                            throw ADP.NonSeqByteAccess(dataIndex, _columnDataBytesRead, ADP.GetBytes);
                        } 

                        // if the dataIndex is not equal to bytes read, then we have to skip bytes
                        long cb = dataIndex - _columnDataBytesRead;
 
                        // if dataIndex is outside of the data range, return 0
                        if ((cb > _columnDataBytesRemaining) && !_metaData[i].metaType.IsPlp) { 
                            return 0; 
                        }
 
                        // if bad buffer index, throw
                        if (bufferIndex < 0 || bufferIndex >= buffer.Length)
                            throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex");
 
                        // if there is not enough room in the buffer for data
                        if (length + bufferIndex > buffer.Length) 
                            throw ADP.InvalidBufferSizeOrIndex(length, bufferIndex); 

                        if (length < 0) 
                            throw ADP.InvalidDataLength(length);

                        // if plp columns, do partial reads. Don't read the entire value in one shot.
                        if (_metaData[i].metaType.IsPlp) { 
                            if (cb > 0) {
                                cb = (long) _parser.SkipPlpValue((ulong) cb, _stateObj); 
                                _columnDataBytesRead +=cb; 
                            }
                            cb = (long) _stateObj.ReadPlpBytes(ref buffer, bufferIndex, length); 
                            _columnDataBytesRead += cb;
                            _columnDataBytesRemaining = (long)_parser.PlpBytesLeft(_stateObj);
                            return cb;
                        } 

                        if (cb > 0) { 
                            _parser.SkipLongBytes((ulong) cb, _stateObj); 
                            _columnDataBytesRead += cb;
                            _columnDataBytesRemaining -= cb; 
                        }

                        // read the min(bytesLeft, length) into the user's buffer
                        cb = (_columnDataBytesRemaining < length) ? _columnDataBytesRemaining : length; 
                        _stateObj.ReadByteArray(buffer, bufferIndex, (int)cb);
                        _columnDataBytesRead += cb; 
                        _columnDataBytesRemaining -= cb; 
                        return cb;
 

                    }

                    // random access now! 
                    // note that since we are caching in an array, and arrays aren't 64 bit ready yet,
                    // we need can cast to int if the dataIndex is in range 
                    if (dataIndex < 0) 
                        throw ADP.NegativeParameter("dataIndex");
 
                    if (dataIndex > Int32.MaxValue) {
                        throw ADP.InvalidSourceBufferIndex(cbytes, dataIndex, "dataIndex");
                    }
 
                    int ndataIndex = (int)dataIndex;
                    byte[] data; 
 
                    // WebData 99342 - in the non-sequential case, we need to support
                    //                 the use of GetBytes on string data columns, but 
                    //                 GetSqlBinary isn't supposed to.  What we end up
                    //                 doing isn't exactly pretty, but it does work.
                    if (_metaData[i].metaType.IsBinType) {
                        data = GetSqlBinary(i).Value; 
                    }
                    else { 
                        Debug.Assert(_metaData[i].metaType.IsLong, "non long type?"); 
                        Debug.Assert(_metaData[i].metaType.IsCharType, "non-char type?");
 
                        SqlString temp = GetSqlString(i);
                        if (_metaData[i].metaType.IsNCharType) {
                            data = temp.GetUnicodeBytes();
                        } 
                        else {
                            data = temp.GetNonUnicodeBytes(); 
                        } 
                    }
 
                    cbytes = data.Length;

                    // if no buffer is passed in, return the number of characters we have
                    if (null == buffer) 
                        return cbytes;
 
                    // if dataIndex is outside of data range, return 0 
                    if (ndataIndex < 0 || ndataIndex >= cbytes) {
                        return 0; 
                    }
                    try {
                        if (ndataIndex < cbytes) {
                            // help the user out in the case where there's less data than requested 
                            if ((ndataIndex + length) > cbytes)
                                cbytes = cbytes - ndataIndex; 
                            else 
                                cbytes = length;
                        } 

                        Array.Copy(data, ndataIndex, buffer, bufferIndex, cbytes);
                    }
                    catch (Exception e) { 
                        //
                        if (!ADP.IsCatchableExceptionType(e)) { 
                            throw; 
                        }
                        cbytes = data.Length; 

                        if (length < 0)
                            throw ADP.InvalidDataLength(length);
 
                        // if bad buffer index, throw
                        if (bufferIndex < 0 || bufferIndex >= buffer.Length) 
                            throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex"); 

                        // if there is not enough room in the buffer for data 
                        if (cbytes + bufferIndex > buffer.Length)
                            throw ADP.InvalidBufferSizeOrIndex(cbytes, bufferIndex);

                        throw; 
                    }
 
                    return cbytes; 
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG 
            }
            catch (System.OutOfMemoryException e) { 
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw;
            }
            catch (System.StackOverflowException e) { 
                _isClosed = true;
                if (null != _connection) { 
                    _connection.Abort(e); 
                }
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  {
                _isClosed = true;
                if (null != _connection) { 
                    _connection.Abort(e);
                } 
                throw; 
            }
        } 

        [ EditorBrowsableAttribute(EditorBrowsableState.Never) ] // MDAC 69508
        override public char GetChar(int i) {
            throw ADP.NotSupported(); 
        }
 
        override public long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length) { 
            SqlStatistics statistics = null;
 
            if (MetaData == null || !_dataReady)
                throw SQL.InvalidRead();

           if (0 > i || i >= _metaData.Length) {   // _metaData can't be null if we don't throw above. 
                throw new IndexOutOfRangeException();
            } 
 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                SetTimeout();
                if ((_metaData[i].metaType.IsPlp) &&
                    (IsCommandBehavior(CommandBehavior.SequentialAccess)) ) {
                    if (length < 0) { 
                        throw ADP.InvalidDataLength(length);
                    } 
 
                    // if bad buffer index, throw
                    if ((bufferIndex < 0) || (buffer != null && bufferIndex >= buffer.Length)) { 
                        throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex");
                    }

                    // if there is not enough room in the buffer for data 
                    if (buffer != null && (length + bufferIndex > buffer.Length)) {
                        throw ADP.InvalidBufferSizeOrIndex(length, bufferIndex); 
                    } 
                    if ( _metaData[i].type == SqlDbType.Xml ) {
                        return GetStreamingXmlChars(i, dataIndex, buffer, bufferIndex, length); 
                    }
                    else {
                        return GetCharsFromPlpData(i, dataIndex, buffer, bufferIndex, length);
                    } 
                }
 
                // Did we start reading this value yet? 
                if ((_nextColumnDataToRead == (i+1)) && (_nextColumnHeaderToRead == (i+1)) &&
                     (_columnDataChars != null)) { 

                    if ((IsCommandBehavior(CommandBehavior.SequentialAccess)) &&
                        (dataIndex < _columnDataCharsRead)) {
                        // Don't allow re-read of same chars in sequential access mode 
                        throw ADP.NonSeqByteAccess(dataIndex, _columnDataCharsRead, ADP.GetChars);
                    } 
                } 
                else {
 
                    // if the object doesn't contain a char[] then the user will get an exception
                    string s = GetSqlString(i).Value;

                    _columnDataChars = s.ToCharArray(); 
                    _columnDataCharsRead = 0;
                } 
 
                int cchars = _columnDataChars.Length;
 
                // note that since we are caching in an array, and arrays aren't 64 bit ready yet,
                // we need can cast to int if the dataIndex is in range
                if (dataIndex > Int32.MaxValue) {
                    throw ADP.InvalidSourceBufferIndex(cchars, dataIndex, "dataIndex"); 
                }
                int ndataIndex = (int)dataIndex; 
 
                // if no buffer is passed in, return the number of characters we have
                if (null == buffer) 
                    return cchars;

                // if dataIndex outside of data range, return 0
                if (ndataIndex < 0 || ndataIndex >= cchars) 
                    return 0;
 
                try { 
                    if (ndataIndex < cchars) {
                        // help the user out in the case where there's less data than requested 
                        if ((ndataIndex + length) > cchars)
                            cchars = cchars - ndataIndex;
                        else
                            cchars = length; 
                    }
 
                    Array.Copy(_columnDataChars, ndataIndex, buffer, bufferIndex, cchars); 
                    _columnDataCharsRead += cchars;
                } 
                catch (Exception e) {
                    //
                    if (!ADP.IsCatchableExceptionType(e)) {
                        throw; 
                    }
                    cchars = _columnDataChars.Length; 
 
                    if (length < 0)
                       throw ADP.InvalidDataLength(length); 

                    // if bad buffer index, throw
                    if (bufferIndex < 0 || bufferIndex >= buffer.Length)
                        throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex"); 

                    // if there is not enough room in the buffer for data 
                    if (cchars + bufferIndex > buffer.Length) 
                        throw ADP.InvalidBufferSizeOrIndex(cchars, bufferIndex);
 
                    throw;
                }

                return cchars; 
            }
            finally { 
                SqlStatistics.StopTimer(statistics); 
            }
        } 

        private long GetCharsFromPlpData(int i, long dataIndex, char[] buffer, int bufferIndex, int length) {
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                    long cch;
 
                    if (MetaData == null || !_dataReady) {
                        throw SQL.InvalidRead(); 
                    } 

                    // don't allow get bytes on non-long or non-binary columns 
                    Debug.Assert(_metaData[i].metaType.IsPlp, "GetCharsFromPlpData called on a non-plp column!");
                    // Must be sequential reading
                    Debug.Assert (IsCommandBehavior(CommandBehavior.SequentialAccess), "GetCharsFromPlpData called for non-Sequential access");
 

                    if (_nextColumnDataToRead > i) { 
                        // We've already read/skipped over this column header. 
                            throw ADP.NonSequentialColumnAccess(i, _nextColumnDataToRead);
                    } 

                    if (!_metaData[i].metaType.IsCharType) {
                        throw SQL.NonCharColumn(_metaData[i].column);
                    } 

                    if (_nextColumnHeaderToRead <= i) { 
                        ReadColumnHeader(i); 
                    }
 
                    // If data is null, ReadColumnHeader sets the data.IsNull bit.
                    if (_data[i] != null && _data[i].IsNull) {
                        throw new SqlNullValueException();
                    } 

                    if (dataIndex < _columnDataCharsRead) { 
                        // Don't allow re-read of same chars in sequential access mode 
                        throw ADP.NonSeqByteAccess(dataIndex, _columnDataCharsRead, ADP.GetChars);
                    } 


                    bool isUnicode = _metaData[i].metaType.IsNCharType;
 
                    if (0 == _columnDataBytesRemaining) {
                        return 0; // We've read this column to the end 
                    } 

                    // if no buffer is passed in, return the total number of characters or -1 
                    if (null == buffer) {
                        cch = (long) _parser.PlpBytesTotalLength(_stateObj);
                        return (isUnicode && (cch > 0)) ? cch >> 1 : cch;
                    } 
                    if (dataIndex > _columnDataCharsRead) {
                        // Skip chars 
                        cch = dataIndex - _columnDataCharsRead; 
                        cch = isUnicode ? (cch << 1 ) : cch;
                        cch = (long) _parser.SkipPlpValue((ulong)(cch), _stateObj); 
                        _columnDataBytesRead += cch;
                        _columnDataCharsRead += (isUnicode && (cch > 0)) ? cch >> 1 : cch;
                    }
                    cch = length; 

                    if (isUnicode) { 
                        cch = (long) _parser.ReadPlpUnicodeChars(ref buffer, bufferIndex, length, _stateObj); 
                        _columnDataBytesRead += (cch << 1);
                    } 
                    else {
                        cch = (long) _parser.ReadPlpAnsiChars(ref buffer, bufferIndex, length, _metaData[i], _stateObj);
                        _columnDataBytesRead += cch << 1;
                    } 
                    _columnDataCharsRead += cch;
                    _columnDataBytesRemaining = (long)_parser.PlpBytesLeft(_stateObj); 
                    return cch; 
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG 
            }
            catch (System.OutOfMemoryException e) { 
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw;
            }
            catch (System.StackOverflowException e) { 
                _isClosed = true;
                if (null != _connection) { 
                    _connection.Abort(e); 
                }
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  {
                _isClosed = true;
                if (null != _connection) { 
                    _connection.Abort(e);
                } 
                throw; 
            }
        } 

        internal long GetStreamingXmlChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length) {
           //  return GetCharsFromPlpData(i, dataIndex, buffer, bufferIndex, length);
           SqlStreamingXml localSXml = null; 
           if ((_streamingXml != null) && ( _streamingXml.ColumnOrdinal != i)) {
                _streamingXml.Close(); 
                _streamingXml = null; 
           }
            if (_streamingXml == null) { 
                localSXml = new SqlStreamingXml(i, this);
            }
            else {
                localSXml = _streamingXml; 
            }
            long cnt = localSXml.GetChars(dataIndex, buffer, bufferIndex, length); 
            if (_streamingXml == null) { 
                // Data is read through GetBytesInternal which may dispose _streamingXml if it has to advance the column ordinal.
                // Therefore save the new SqlStreamingXml class after the read succeeds. 
                _streamingXml = localSXml;
            }
            return cnt;
        } 

        [ EditorBrowsableAttribute(EditorBrowsableState.Never) ] // MDAC 69508 
        IDataReader IDataRecord.GetData(int i) { 
            throw ADP.NotSupported();
        } 

        override public DateTime GetDateTime(int i) {
            ReadColumn(i);
 
            DateTime dt = _data[i].DateTime;
            // This accessor can be called for regular DateTime column. In this case we should not throw 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType) { 
                // TypeSystem.SQLServer2005 or less
 
                // If the above succeeds, then we received a valid DateTime instance, now we need to force
                // an InvalidCastException since DateTime is not exposed with the version knob in this setting.
                // To do so, we simply force the exception by casting the string representation of the value
                // To DateTime. 
                object temp = (object) _data[i].String;
                dt = (DateTime) temp; 
            } 

            return dt; 
        }

        override public Decimal GetDecimal(int i) {
            ReadColumn(i); 
            return _data[i].Decimal;
        } 
 
        override public double GetDouble(int i) {
            ReadColumn(i); 
            return _data[i].Double;
        }

        override public float GetFloat(int i) { 
            ReadColumn(i);
            return _data[i].Single; 
        } 

        override public Guid GetGuid(int i) { 
            ReadColumn(i);
            return _data[i].SqlGuid.Value;
        }
 
        override public Int16 GetInt16(int i) {
            ReadColumn(i); 
            return _data[i].Int16; 
        }
 
        override public Int32 GetInt32(int i) {
            ReadColumn(i);
            return _data[i].Int32;
        } 

        override public Int64 GetInt64(int i) { 
            ReadColumn(i); 
            return _data[i].Int64;
        } 

        virtual public SqlBoolean GetSqlBoolean(int i) {
            ReadColumn(i);
            return _data[i].SqlBoolean; 
        }
 
        virtual public SqlBinary GetSqlBinary(int i) { 
            ReadColumn(i);
            return _data[i].SqlBinary; 
        }

        virtual public SqlByte GetSqlByte(int i) {
            ReadColumn(i); 
            return _data[i].SqlByte;
        } 
 
        virtual public SqlBytes GetSqlBytes(int i) {
            if (MetaData == null) 
                throw SQL.InvalidRead();

            ReadColumn(i);
            SqlBinary data = _data[i].SqlBinary; 
            return new SqlBytes(data);
        } 
 
        virtual public SqlChars GetSqlChars(int i) {
            ReadColumn(i); 
            SqlString data;
            // Convert Katmai types to string
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType)
            { 
                data = _data[i].KatmaiDateTimeSqlString;
            } else { 
                data = _data[i].SqlString; 
            }
            return new SqlChars(data); 
        }

        virtual public SqlDateTime GetSqlDateTime(int i) {
            ReadColumn(i); 
            return _data[i].SqlDateTime;
        } 
 
        virtual public SqlDecimal GetSqlDecimal(int i) {
            ReadColumn(i); 
            return _data[i].SqlDecimal;
        }

        virtual public SqlGuid GetSqlGuid(int i) { 
            ReadColumn(i);
            return _data[i].SqlGuid; 
        } 

        virtual public SqlDouble GetSqlDouble(int i) { 
            ReadColumn(i);
            return _data[i].SqlDouble;
        }
 
        virtual public SqlInt16 GetSqlInt16(int i) {
            ReadColumn(i); 
            return _data[i].SqlInt16; 
        }
 
        virtual public SqlInt32 GetSqlInt32(int i) {
            ReadColumn(i);
            return _data[i].SqlInt32;
        } 

        virtual public SqlInt64 GetSqlInt64(int i) { 
            ReadColumn(i); 
            return _data[i].SqlInt64;
        } 

        virtual public SqlMoney GetSqlMoney(int i) {
            ReadColumn(i);
            return _data[i].SqlMoney; 
        }
 
        virtual public SqlSingle GetSqlSingle(int i) { 
            ReadColumn(i);
            return _data[i].SqlSingle; 
        }

        //
        virtual public SqlString GetSqlString(int i) { 
            ReadColumn(i);
 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType) { 
                return _data[i].KatmaiDateTimeSqlString;
            } 

            return _data[i].SqlString;
        }
 
        virtual public SqlXml GetSqlXml(int i){
            ReadColumn(i); 
            SqlXml sx = null; 

            if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) { 
                // TypeSystem.SQLServer2005

                sx = _data[i].IsNull ? SqlXml.Null : _data[i].SqlCachedBuffer.ToSqlXml();
            } 
            else {
                // TypeSystem.SQLServer2000 
 
                // First, attempt to obtain SqlXml value.  If not SqlXml, we will throw the appropriate
                // cast exception. 
                sx = _data[i].IsNull ? SqlXml.Null : _data[i].SqlCachedBuffer.ToSqlXml();

                // If the above succeeds, then we received a valid SqlXml instance, now we need to force
                // an InvalidCastException since SqlXml is not exposed with the version knob in this setting. 
                // To do so, we simply force the exception by casting the string representation of the value
                // To SqlXml. 
                object temp = (object) _data[i].String; 
                sx = (SqlXml) temp;
            } 

            return sx;
        }
 
        virtual public object GetSqlValue(int i) {
            SqlStatistics statistics = null; 
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
 
                if (MetaData == null || !_dataReady) {
                    throw SQL.InvalidRead();
                }
 
                SetTimeout();
 
                Object o = GetSqlValueInternal(i); 
                return o;
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        private object GetSqlValueInternal(int i) { 
            Debug.Assert (_dataReady, "Attempting to GetValue without data ready?"); 

            ReadColumn(i, false); // timeout set on outer call 

            Debug.Assert(null != _data, "no data columns?");                // should have been caught already.
            Debug.Assert(i < _data.Length, "reading beyond data length?");  // should have been caught already.
 
            object o;
 
            // Convert Katmai types to string 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType) {
                return _data[i].KatmaiDateTimeSqlString; 
            }
            else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsLargeUdt) {
                o = _data[i].SqlValue;
            } 
            else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) {
                // TypeSystem.SQLServer2005 
 
                if (_metaData[i].type == SqlDbType.Udt) {
                    SqlConnection.CheckGetExtendedUDTInfo(_metaData[i], true); 
                    o = Connection.GetUdtValue(_data[i].Value, _metaData[i], false);
                }
                else {
                    o = _data[i].SqlValue; 
                }
            } 
            else { 
                // TypeSystem.SQLServer2000
 
                if (_metaData[i].type == SqlDbType.Xml) {
                    o = _data[i].SqlString;
                }
                else { 
                    o = _data[i].SqlValue;
                } 
            } 

            return o; 
        }

        virtual public int GetSqlValues(object[] values){
            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                if (MetaData == null || !_dataReady) { 
                    throw SQL.InvalidRead();
                } 
                if (null == values) {
                    throw ADP.ArgumentNull("values");
                }
 
                SetTimeout();
 
                int copyLen = (values.Length < _metaData.visibleColumns) ? values.Length : _metaData.visibleColumns; 

                for (int i = 0; i < copyLen; i++) { 
                    values[_metaData.indexMap[i]] = GetSqlValueInternal(i);
                }
                return copyLen;
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
            } 
        }
 
        override public string GetString(int i) {
            ReadColumn(i);

            // Convert katmai value to string if type system knob is 2005 or earlier 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType) {
                return _data[i].KatmaiDateTimeString; 
            } 

            return _data[i].String; 
        }

        override public object GetValue(int i) {
            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
 
                if (MetaData == null || !_dataReady) {
                    throw SQL.InvalidRead(); 
                }

                SetTimeout();
 
                object o = GetValueInternal(i);
                return o; 
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
            }
        }

        virtual public TimeSpan GetTimeSpan(int i) { 
            ReadColumn(i);
 
            TimeSpan t = _data[i].Time; 

            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005) { 
                // TypeSystem.SQLServer2005 or less

                // If the above succeeds, then we received a valid TimeSpan instance, now we need to force
                // an InvalidCastException since TimeSpan is not exposed with the version knob in this setting. 
                // To do so, we simply force the exception by casting the string representation of the value
                // To TimeSpan. 
                object temp = (object) _data[i].String; 
                t = (TimeSpan) temp;
            } 

            return t;
        }
 
        virtual public DateTimeOffset GetDateTimeOffset(int i) {
            ReadColumn(i); 
 
            DateTimeOffset dto = _data[i].DateTimeOffset;
 
            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005) {
                // TypeSystem.SQLServer2005 or less

                // If the above succeeds, then we received a valid DateTimeOffset instance, now we need to force 
                // an InvalidCastException since DateTime is not exposed with the version knob in this setting.
                // To do so, we simply force the exception by casting the string representation of the value 
                // To DateTimeOffset. 
                object temp = (object) _data[i].String;
                dto = (DateTimeOffset) temp; 
            }

            return dto;
        } 

        private object GetValueInternal(int i) { 
            Debug.Assert (_dataReady, "Attempting to GetValue without data ready?"); 
            ReadColumn(i, false); // timeout set on outer call
 
            Debug.Assert(null != _data, "no data columns?");                // should have been caught already.
            Debug.Assert(i < _data.Length, "reading beyond data length?");  // should have been caught already.

            object o; 

            if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsNewKatmaiDateTimeType) { 
                if (_data[i].IsNull) { 
                    return DBNull.Value;
                } 
                else {
                    return _data[i].KatmaiDateTimeString;
                }
            } 
            else if (_typeSystem <= SqlConnectionString.TypeSystem.SQLServer2005 && _metaData[i].IsLargeUdt) {
                o = _data[i].Value; 
            } 
            else if (_typeSystem != SqlConnectionString.TypeSystem.SQLServer2000) {
                // TypeSystem.SQLServer2005 

                if (_metaData[i].type != SqlDbType.Udt) {
                    o = _data[i].Value;
                } 
                else {
                    SqlConnection.CheckGetExtendedUDTInfo(_metaData[i], true); 
                    o = Connection.GetUdtValue(_data[i].Value, _metaData[i], true); 
                }
            } 
            else {
                // TypeSystem.SQLServer2000

                o = _data[i].Value; 
            }
 
            return o; 
        }
 
        override public int GetValues(object[] values) {
            SqlStatistics statistics = null;
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                if (MetaData == null || !_dataReady)
                    throw SQL.InvalidRead(); 
 
                if (null == values) {
                    throw ADP.ArgumentNull("values"); 
                }

                int copyLen = (values.Length < _metaData.visibleColumns) ? values.Length : _metaData.visibleColumns;
 
                SetTimeout();
 
                for (int i = 0; i < copyLen; i++) { 
                    values[_metaData.indexMap[i]] = GetValueInternal(i);
                } 

                if (null != _rowException) {
                    throw _rowException;
                } 
                return copyLen;
            } 
            finally { 
                SqlStatistics.StopTimer(statistics);
            } 
        }

        private MetaType GetVersionedMetaType(MetaType actualMetaType) {
            Debug.Assert(_typeSystem == SqlConnectionString.TypeSystem.SQLServer2000, "Should not be in this function under anything else but SQLServer2000"); 

            MetaType metaType = null; 
 
            if      (actualMetaType == MetaType.MetaUdt) {
                metaType = MetaType.MetaVarBinary; 
            }
            else if (actualMetaType == MetaType.MetaXml) {
                metaType = MetaType.MetaNText;
            } 
            else if (actualMetaType == MetaType.MetaMaxVarBinary) {
                metaType = MetaType.MetaImage; 
            } 
            else if (actualMetaType == MetaType.MetaMaxVarChar) {
                metaType = MetaType.MetaText; 
            }
            else if (actualMetaType == MetaType.MetaMaxNVarChar) {
                metaType = MetaType.MetaNText;
            } 
            else {
                metaType = actualMetaType; 
            } 

            return metaType; 
        }

        private bool HasMoreResults() {
            if(null != _parser) { 
                if(HasMoreRows()) {
                    // When does this happen?  This is only called from NextResult(), which loops until Read() false. 
                    return true; 
                }
 
                Debug.Assert(null != _command, "unexpected null command from the data reader!");

                while(_stateObj._pendingData) {
                    byte token = _stateObj.PeekByte(); 

                    switch(token) { 
                        case TdsEnums.SQLALTROW: 
                            if(_altRowStatus == ALTROWSTATUS.Null) {
                                // cache the regular metadata 
                                _altMetaDataSetCollection.metaDataSet = _metaData;
                                _metaData = null;
                            }
                            else { 
                                Debug.Assert(_altRowStatus == ALTROWSTATUS.Done, "invalid AltRowStatus");
                            } 
                            _altRowStatus = ALTROWSTATUS.AltRow; 
                            _hasRows = true;
                            return true; 
                        case TdsEnums.SQLROW:
                            // always happens if there is a row following an altrow
                            return true;
                        case TdsEnums.SQLDONE: 
                            Debug.Assert(_altRowStatus == ALTROWSTATUS.Done || _altRowStatus == ALTROWSTATUS.Null, "invalid AltRowStatus");
                            _altRowStatus = ALTROWSTATUS.Null; 
                            _metaData = null; 
                            _altMetaDataSetCollection = null;
                            return true; 
                        case TdsEnums.SQLCOLMETADATA:
                            return true;
                    }
                    _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj); 
                }
            } 
            return false; 
        }
 
        private bool HasMoreRows() {
            if (null != _parser) {
                if (_dataReady) {
                    return true; 
                }
 
                // NextResult: previous call to NextResult started to process the altrowpackage, can't peek anymore 
                // Read: Read prepared for final processing of altrow package, No more Rows until NextResult ...
                // Done: Done processing the altrow, no more rows until NextResult ... 
                switch (_altRowStatus) {
                    case ALTROWSTATUS.AltRow:
                        return true;
                    case ALTROWSTATUS.Done: 
                        return false;
                } 
                if (_stateObj._pendingData) { 
                    // Consume error's, info's, done's on HasMoreRows, so user obtains error on Read.
                    // Previous bug where Read() would return false with error on the wire in the case 
                    // of metadata and error immediately following.  See MDAC 78285 and 75225.

                    //
 

 
 

 

                    // process any done, doneproc and doneinproc token streams and
                    // any order, error or info token preceeding the first done, doneproc or doneinproc token stream
                    byte b = _stateObj.PeekByte(); 
                    bool ParsedDoneToken = false;
 
                    while ( b == TdsEnums.SQLDONE || 
                            b == TdsEnums.SQLDONEPROC   ||
                            b == TdsEnums.SQLDONEINPROC || 
                            !ParsedDoneToken && b == TdsEnums.SQLORDER  ||
                            !ParsedDoneToken && b == TdsEnums.SQLERROR  ||
                            !ParsedDoneToken && b == TdsEnums.SQLINFO ) {
 
                        if (b == TdsEnums.SQLDONE ||
                            b == TdsEnums.SQLDONEPROC   || 
                            b == TdsEnums.SQLDONEINPROC) { 
                            ParsedDoneToken = true;
                        } 

                        _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj);
                        if ( _stateObj._pendingData) {
                            b = _stateObj.PeekByte(); 
                        }
                        else { 
                            break; 
                        }
                    } 

                    // Only return true when we are positioned on row b.
                    if (TdsEnums.SQLROW == b)
                        return true; 
                }
            } 
            return false; 
        }
 
        override public bool IsDBNull(int i) {
            SetTimeout();
            ReadColumnHeader(i);    // header data only
            return _data[i].IsNull; 
        }
 
        protected bool IsCommandBehavior(CommandBehavior condition) { 
            return (condition == (condition & _commandBehavior));
        } 

        // recordset is automatically positioned on the first result set
        override public bool NextResult() {
            SqlStatistics statistics = null; 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlDataReader.NextResult|API> %d#", ObjectID); 
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG 
                    statistics = SqlStatistics.StartTimer(Statistics);
 
                    SetTimeout();

                    if (IsClosed) {
                        throw ADP.DataReaderClosed("NextResult"); 
                    }
                    _fieldNameLookup = null; 
 
                    bool success = false; // WebData 100390
                    _hasRows = false; // reset HasRows 

                    // if we are specifically only processing a single result, then read all the results off the wire and detach
                    if (IsCommandBehavior(CommandBehavior.SingleResult)) {
                        CloseInternal(false /*closeReader*/); 

                        // In the case of not closing the reader, null out the metadata AFTER 
                        // CloseInternal finishes - since CloseInternal may go to the wire 
                        // and use the metadata.
                        ClearMetaData(); 
                        return success;
                    }

                    if (null != _parser) { 
                        // if there are more rows, then skip them, the user wants the next result
                        while (ReadInternal(false)) { // don't reset set the timeout value 
                            ; // intentional 
                        }
                    } 

                    // we may be done, so continue only if we have not detached ourselves from the parser
                    if (null != _parser) {
                        if (HasMoreResults()) { 
                            _metaDataConsumed = false;
                            _browseModeInfoConsumed = false; 
 
                            switch (_altRowStatus) {
                                case ALTROWSTATUS.AltRow: 
                                    int altRowId = _parser.GetAltRowId(_stateObj);
                                    _SqlMetaDataSet altMetaDataSet = _altMetaDataSetCollection[altRowId];
                                    if (altMetaDataSet != null) {
                                        _metaData = altMetaDataSet; 
                                        _metaData.indexMap = altMetaDataSet.indexMap;
                                    } 
                                    Debug.Assert ((_metaData != null), "Can't match up altrowmetadata"); 
                                    break;
                                case ALTROWSTATUS.Done: 
                                    // restore the row-metaData
                                    _metaData = _altMetaDataSetCollection.metaDataSet;
                                    Debug.Assert (_altRowStatus == ALTROWSTATUS.Done, "invalid AltRowStatus");
                                    _altRowStatus = ALTROWSTATUS.Null; 
                                    break;
                                default: 
                                    ConsumeMetaData(); 
                                    if (_metaData == null) {
                                        return false; 
                                    }
                                    break;
                            }
 
                            success = true;
                        } 
                        else { 
                            // detach the parser from this reader now
                            CloseInternal(false /*closeReader*/); 

                            // In the case of not closing the reader, null out the metadata AFTER
                            // CloseInternal finishes - since CloseInternal may go to the wire
                            // and use the metadata. 
                            SetMetaData(null, false);
                        } 
                    } 
                    else {
                        // Clear state in case of Read calling CloseInternal() then user calls NextResult() 
                        // MDAC 81986.  Or, also the case where the Read() above will do essentially the same
                        // thing.
                        ClearMetaData();
                    } 

                    return success; 
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _isClosed = true; 
                if (null != _connection) { 
                    _connection.Abort(e);
                } 
                throw;
            }
            catch (System.StackOverflowException e) {
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                } 
                throw;
            } 
            catch (System.Threading.ThreadAbortException e)  {
                _isClosed = true;
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw; 
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp);
            }
        }
 
        // user must call Read() to position on the first row
        override public bool Read() { 
            return ReadInternal(true); 
        }
 
        // user must call Read() to position on the first row
        private bool ReadInternal(bool setTimeout) {
            SqlStatistics statistics = null;
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlDataReader.Read|API> %d#", ObjectID);
 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    statistics = SqlStatistics.StartTimer(Statistics); 

                    if (null != _parser) { 
                        if (setTimeout) {
                            SetTimeout();
                        }
                        if (_dataReady) { 
                            CleanPartialRead();
                        } 
                        // clear out our buffers 
                        _dataReady = false;
                        SqlBuffer.Clear(_data); 

                        _nextColumnHeaderToRead = 0;
                        _nextColumnDataToRead = 0;
                        _columnDataBytesRemaining = -1; // unknown 

                        if (!_haltRead) { 
                            if (HasMoreRows()) { 
                                // read the row from the backend (unless it's an altrow were the marker is already inside the altrow ...)
                                while (_stateObj._pendingData) { 
                                    if (_altRowStatus != ALTROWSTATUS.AltRow) {
                                        // if this is an ordinary row we let the run method consume the ROW token
                                        _dataReady = _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj);
                                        if (_dataReady) { 
                                            break;
                                        } 
                                    } 
                                    else {
                                        // ALTROW token and AltrowId are already consumed ... 
                                        Debug.Assert (_altRowStatus == ALTROWSTATUS.AltRow, "invalid AltRowStatus");
                                        _altRowStatus = ALTROWSTATUS.Done;
                                        _dataReady = true;
                                        break; 
                                    }
                                } 
                                if (_dataReady) { 
                                    _haltRead = IsCommandBehavior(CommandBehavior.SingleRow);
                                    return true; 
                                }
                            }

                            if (!_stateObj._pendingData) { 
                                CloseInternal(false /*closeReader*/);
                            } 
                        } 
                        else {
                            // if we did not get a row and halt is true, clean off rows of result 
                            // success must be false - or else we could have just read off row and set
                            // halt to true
                            while (HasMoreRows()) {
                                // if we are in SingleRow mode, and we've read the first row, 
                                // read the rest of the rows, if any
                                while (_stateObj._pendingData && !_dataReady) { 
                                    _dataReady = _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj); 
                                }
 
                                if (_dataReady) {
                                    CleanPartialRead();
                                }
 
                                // clear out our buffers
                                _dataReady = false; 
                                SqlBuffer.Clear(_data); 

                                _nextColumnHeaderToRead = 0; 
                            }

                            // reset haltRead
                            _haltRead = false; 
                         }
                    } 
                    else if (IsClosed) { 
                        throw ADP.DataReaderClosed("Read");
                    } 

                    return false;
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _isClosed = true;
                SqlConnection con = _connection;
                if (con != null) { 
                    con.Abort(e);
                } 
                throw; 
            }
            catch (System.StackOverflowException e) { 
                _isClosed = true;
                SqlConnection con = _connection;
                if (con != null) {
                    con.Abort(e); 
                }
                throw; 
            } 
            catch (System.Threading.ThreadAbortException e)  {
               _isClosed = true; 
                SqlConnection con = _connection;
                if (con != null) {
                    con.Abort(e);
                } 
                throw;
            } 
            finally { 
                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp); 
            }
        }

        private void ReadColumn(int i) { 
            ReadColumn(i, true);
        } 
 
        private void ReadColumn(int i, bool setTimeout) {
            if (MetaData == null || !_dataReady) { 
                throw SQL.InvalidRead();
            }

            if (0 > i || i >= _metaData.Length) {   // _metaData can't be null if we don't throw above. 
                throw new IndexOutOfRangeException();
            } 
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG 
                    Debug.Assert(_nextColumnHeaderToRead <= _metaData.Length, "_nextColumnHeaderToRead too large");
                    Debug.Assert(_nextColumnDataToRead <= _metaData.Length, "_nextColumnDataToRead too large"); 

                    if (setTimeout) {
                        SetTimeout();
                    } 
                    if (_nextColumnHeaderToRead <= i) {
                        ReadColumnHeader(i); 
                    } 
                    if (_nextColumnDataToRead == i) {
                        ReadColumnData(); 
                    }
                    else if (_nextColumnDataToRead > i) {
                        // We've already read/skipped over this column header.
 
                        // CommandBehavior.SequentialAccess: allow sequential, non-repeatable
                        // reads.  If we specify a column that we've already read, error 
                        if (IsCommandBehavior(CommandBehavior.SequentialAccess)) { 
                            throw ADP.NonSequentialColumnAccess(i, _nextColumnDataToRead);
                        } 
                    }
                    Debug.Assert(null != _data[i], " data buffer is null?");
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _isClosed = true;
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw; 
            } 
            catch (System.StackOverflowException e) {
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e);
                }
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw;
            }
        } 

        private void ReadColumnData() { 
            // If we've already read the value (because it was NULL) we don't 
            // bother to read here.
            if (!_data[_nextColumnDataToRead].IsNull) { 
                _SqlMetaData columnMetaData = _metaData[_nextColumnDataToRead];

                _parser.ReadSqlValue(_data[_nextColumnDataToRead], columnMetaData, (int)_columnDataBytesRemaining, _stateObj); // will read UDTs as VARBINARY.
                _columnDataBytesRemaining = 0; 
            }
            _nextColumnDataToRead++; 
        } 

        private void ReadColumnHeader(int i) { 
            if (!_dataReady) {
                throw SQL.InvalidRead();
            }
 
            Debug.Assert (i < _data.Length, "reading past end of data buffer?");
 
            if (i < _nextColumnDataToRead) { 
                return;
            } 

            Debug.Assert(_data[i].IsEmpty, "re-reading column value?");

            bool skippingColumnData = IsCommandBehavior(CommandBehavior.SequentialAccess); 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    // If we're in sequential access mode, we can safely clear out any 
                    // data from the previous column. 
                    if (skippingColumnData) {
                        if (0 < _nextColumnDataToRead) { 
                            _data[_nextColumnDataToRead-1].Clear();
                        }
                    }
                    else if (_nextColumnDataToRead < _nextColumnHeaderToRead) { 
                        // We read the header but not the column for the previous column
                        ReadColumnData(); 
                        Debug.Assert(_nextColumnDataToRead == _nextColumnHeaderToRead); 
                    }
 
                    while (_nextColumnHeaderToRead <= i) {
                        // if we still have bytes left from the previous blob read, clear
                        // the wire and reset
                        ResetBlobState(); 

                        // Turn off column skipping once we reach the actual column 
                        // we're supposed to read. 
                        if (skippingColumnData) {
                            skippingColumnData = (_nextColumnHeaderToRead < i); 
                        }

                        _SqlMetaData columnMetaData = _metaData[_nextColumnHeaderToRead];
                        if (skippingColumnData && columnMetaData.metaType.IsPlp) { 
                            _parser.SkipPlpValue(UInt64.MaxValue, _stateObj);
                            _nextColumnDataToRead = _nextColumnHeaderToRead; 
                            _nextColumnHeaderToRead++; 
                            _columnDataBytesRemaining = 0;
                        } 
                        else {
                            bool isNull = false;
                            ulong dataLength = _parser.ProcessColumnHeader(columnMetaData, _stateObj, out isNull);
 
                            _nextColumnDataToRead = _nextColumnHeaderToRead;
                            _nextColumnHeaderToRead++;  // We read this one 
 
                            if (skippingColumnData) {
                                _parser.SkipLongBytes(dataLength, _stateObj); 
                                _columnDataBytesRemaining = 0;
                            }
                            else if (isNull) {
                                _parser.GetNullSqlValue(_data[_nextColumnDataToRead], columnMetaData); 
                                _columnDataBytesRemaining = 0;
                            } 
                            else { 
                                _columnDataBytesRemaining = (long)dataLength;
 
                                if (i > _nextColumnDataToRead) {
                                    // If we're not in sequential access mode, we have to
                                    // save the data we skip over so that the consumer
                                    // can read it out of order 
                                    ReadColumnData();
                                } 
                            } 
                        }
                    } 
#if DEBUG
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG 
            } 
            catch (System.OutOfMemoryException e) {
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e);
                }
                throw; 
            }
            catch (System.StackOverflowException e) { 
                _isClosed = true; 
                if (null != _connection) {
                    _connection.Abort(e); 
                }
                throw;
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _isClosed = true;
                if (null != _connection) { 
                    _connection.Abort(e); 
                }
                throw; 
            }
        }

 
        // clean remainder bytes for the column off the wire
        private void ResetBlobState() { 
            Debug.Assert(null != _stateObj, "null state object"); // _parser may be null at this point 
            Debug.Assert(_nextColumnHeaderToRead <= _metaData.Length, "_nextColumnHeaderToRead too large");
            int currentColumn = _nextColumnHeaderToRead - 1; 
            if ((currentColumn >= 0) && _metaData[currentColumn].metaType.IsPlp) {
                if (_stateObj._longlen != 0) {
                    _stateObj.Parser.SkipPlpValue(UInt64.MaxValue, _stateObj);
                } 
                if (_streamingXml != null) {
                    SqlStreamingXml localSXml = _streamingXml; 
                    _streamingXml = null; 
                    localSXml.Close();
                } 
            }
            else if (0 < _columnDataBytesRemaining) {
                    _stateObj.Parser.SkipLongBytes((ulong)_columnDataBytesRemaining, _stateObj);
            } 

            _columnDataBytesRemaining = -1; // unknown 
            _columnDataBytesRead = 0; 
            _columnDataCharsRead = 0;
            _columnDataChars = null; 
        }

        private void RestoreServerSettings(TdsParser parser, TdsParserStateObject stateObj) {
            // turn off any set options 
            if (null != parser && null != _resetOptionsString) {
                // It is possible for this to be called during connection close on a 
                // broken connection, so check state first. 
                if (parser.State == TdsParserState.OpenLoggedIn) {
                    parser.TdsExecuteSQLBatch(_resetOptionsString, (_command != null) ? _command.CommandTimeout : 0, null, stateObj); 
                    parser.Run(RunBehavior.UntilDone, _command, this, null, stateObj);
                }
                _resetOptionsString = null;
            } 
        }
 
        internal void SetAltMetaDataSet(_SqlMetaDataSet metaDataSet, bool metaDataConsumed) { 
            if (_altMetaDataSetCollection == null) {
                _altMetaDataSetCollection = new _SqlMetaDataSetCollection(); 
            }
            _altMetaDataSetCollection.Add(metaDataSet);
            _metaDataConsumed = metaDataConsumed;
            if (_metaDataConsumed) { 
                byte b = _stateObj.PeekByte();
                if (TdsEnums.SQLORDER == b) { 
                    _parser.Run(RunBehavior.ReturnImmediately, _command, this, null, _stateObj); 
                    b = _stateObj.PeekByte();
                } 
                _hasRows = (TdsEnums.SQLROW == b);
            }
            if (metaDataSet != null) {
                if (_data == null || _data.Length<metaDataSet.Length) { 
                    _data = SqlBuffer.CreateBufferArray(metaDataSet.Length);
                } 
            } 
        }
 
        private void ClearMetaData() {
            _metaData = null;
            _tableNames = null;
            _fieldNameLookup = null; 
            _metaDataConsumed = false;
            _browseModeInfoConsumed = false; 
        } 

        internal void SetMetaData(_SqlMetaDataSet metaData, bool moreInfo) { 
            _metaData = metaData;

            // get rid of cached metadata info as well
            _tableNames = null; 
            if (_metaData != null) {
                _metaData.schemaTable = null; 
                _data = SqlBuffer.CreateBufferArray(metaData.Length); 
            }
 
            _fieldNameLookup = null;

            if (null != metaData) {
                // we are done consuming metadata only if there is no moreInfo 
                if (!moreInfo) {
                    _metaDataConsumed = true; 
 
                    if (_parser != null) { // There is a valid case where parser is null
                        // Peek, and if row token present, set _hasRows true since there is a 
                        // row in the result
                        byte b = _stateObj.PeekByte();

                        // 

 
                        // simply rip the order token off the wire 
                        if (b == TdsEnums.SQLORDER) {                     //  same logic as SetAltMetaDataSet
// Devnote: That's not the right place to process TDS 
// Can this result in Reentrance to Run?
//
                             _parser.Run(RunBehavior.ReturnImmediately, null, null, null, _stateObj);
                            b = _stateObj.PeekByte(); 
                        }
                        _hasRows = (TdsEnums.SQLROW == b); 
                        if (TdsEnums.SQLALTMETADATA == b) 
                        {
                            _metaDataConsumed = false; 
                        }
                    }
                }
            } 
            else {
                _metaDataConsumed = false; 
            } 

            _browseModeInfoConsumed = false; 
        }

        private void SetTimeout() {
            // WebData 111653,112003 -- we now set timeouts per operation, not 
            // per command (it's not supposed to be a cumulative per command).
            TdsParserStateObject stateObj = _stateObj; 
 
            if (null != stateObj) {
                stateObj.SetTimeoutSeconds(_timeoutSeconds); 
            }
        }

        // Used by SqlResultSet to avoid XML to string, and UDT object conversion. 
        internal object GetSqlValueWithNoConvert(int i) {
           if (MetaData == null || !_dataReady) 
                throw SQL.InvalidRead(); 

            ReadColumn(i, false); 

            Debug.Assert(null != _data, "no data columns?");                // should have been caught already.
            Debug.Assert(i < _data.Length, "reading beyond data length?");  // should have been caught already.
 
            object o = null;
 
            if (_metaData[i].type == SqlDbType.Xml) { 
                // Return SqlCachedBuffer instead of string
                o = _data[i].SqlCachedBuffer; 
            } else {
                // For UDTs, this returns SqlBinary
                o = _data[i].SqlValue;
            } 

            return o; 
        } 
    }// SqlDataReader
}// namespace 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
