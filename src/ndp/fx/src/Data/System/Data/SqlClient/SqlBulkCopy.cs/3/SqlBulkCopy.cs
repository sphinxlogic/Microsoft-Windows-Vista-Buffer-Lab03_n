//------------------------------------------------------------------------------ 
// <copyright file="SqlBulkCopy.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
// todo list:
// * An ID column need to be ignored - even if there is an association 
// * Spec: ID columns will be ignored - even if there is an association
// * Spec: How do we publish CommandTimeout on the bcpoperation?
//
 
namespace System.Data.SqlClient {
    using System; 
    using System.Collections; 
    using System.ComponentModel;
    using System.Data; 
    using System.Data.Common;
    using System.Data.Sql;
    using System.Data.SqlTypes;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution; 
    using System.Text;
    using System.Threading; 
    using System.Xml;

    // -------------------------------------------------------------------------------------------------
    // this internal class helps us to associate the metadata (from the target) 
    // with columnordinals (from the source)
    // 
    sealed internal class _ColumnMapping { 
        internal int _sourceColumnOrdinal;
        internal _SqlMetaData _metadata; 

        internal _ColumnMapping(int columnId, _SqlMetaData metadata) {
            _sourceColumnOrdinal = columnId;
            _metadata = metadata; 
        }
    } 
 
    sealed internal class Row {
        private object[] _dataFields; 

        internal Row(int rowCount) {
            _dataFields = new object[rowCount];
        } 

        internal object[] DataFields { 
            get { 
                return _dataFields;
            } 
        }

        internal object this[int index] {
            get { 
                return _dataFields[index];
            } 
        } 
    }
 
    // the controlling class for one result (metadata + rows)
    //
    sealed internal class Result {
        private _SqlMetaDataSet _metadata; 
        private ArrayList _rowset;
 
        internal Result(_SqlMetaDataSet metadata) { 
            this._metadata = metadata;
            this._rowset = new ArrayList(); 
        }

        internal int Count {
            get { 
                return _rowset.Count;
            } 
        } 

        internal _SqlMetaDataSet MetaData { 
            get {
                return _metadata;
            }
        } 

        internal Row this[int index] { 
            get { 
                return (Row)_rowset[index];
            } 
        }

        internal void AddRow(Row row) {
            _rowset.Add(row); 
        }
    } 
 
    // A wrapper object for metadata and rowsets returned by our initial queries
    // 
    sealed internal class BulkCopySimpleResultSet {
        private ArrayList _results;                   // the list of results
        private Result resultSet;                     // the current result
        private int[] indexmap;                       // associates columnids with indexes in the rowarray 

        // c-tor 
        // 
        internal BulkCopySimpleResultSet() {
            _results = new ArrayList(); 
        }

        // indexer
        // 
        internal Result this[int idx] {
            get { 
                return (Result)_results[idx]; 
            }
        } 
        // callback function for the tdsparser
        // note that setting the metadata adds a resultset
        //
        internal void SetMetaData(_SqlMetaDataSet metadata) { 
            resultSet = new Result(metadata);
            _results.Add(resultSet); 
 
            indexmap = new int[resultSet.MetaData.Length];
            for(int i = 0; i < indexmap.Length; i++) { 
                indexmap[i] = i;
            }
        }
 
        // callback function for the tdsparser
        // this will create an indexmap for the active resultset 
        // 
        internal int[] CreateIndexMap() {
            return indexmap; 
        }

        // callback function for the tdsparser
        // this will return an array of rows to store the rowdata 
        //
        internal object[] CreateRowBuffer() { 
            Row row = new Row(resultSet.MetaData.Length); 
            resultSet.AddRow(row);
            return row.DataFields; 
        }
    }

    // ------------------------------------------------------------------------------------------------- 
    //
    // 
#if WINFSInternalOnly 
    internal
#else 
    public
#endif
 sealed class SqlBulkCopy : IDisposable {
        private enum TableNameComponents { 
            Server = 0,
            Catalog, 
            Owner, 
            TableName,
        } 
        private enum ValueSourceType {
            Unspecified = 0,
            IDataReader,
            DataTable, 
            RowArray
        } 
 
        // The initial query will return three tables.
        // Transaction count has only one value in one column and one row 
        // MetaData has n columns but no rows
        // Collation has 4 columns and n rows

        private const int TranCountResultId = 0; 
        private const int TranCountRowId = 0;
        private const int TranCountValueId = 0; 
 
        private const int MetaDataResultId = 1;
 
        private const int CollationResultId = 2;
        private const int ColIdId = 0;
        private const int NameId = 1;
        private const int Tds_CollationId = 2; 
        private const int CollationId = 3;
 
        private const int DefaultCommandTimeout = 30; 

        private int _batchSize; 
        private bool _ownConnection;
        private SqlBulkCopyOptions _copyOptions;
        private int _timeout = DefaultCommandTimeout;
        private string _destinationTableName; 
        private int _rowsCopied;
        private int _notifyAfter; 
        private int _rowsUntilNotification; 
        private bool _insideRowsCopiedEvent;
 
        private object _rowSource;
        private SqlDataReader _SqlDataReaderRowSource;

        private SqlBulkCopyColumnMappingCollection _columnMappings; 
        private SqlBulkCopyColumnMappingCollection _localColumnMappings;
 
        private SqlConnection _connection; 
        private SqlTransaction _internalTransaction;
        private SqlTransaction _externalTransaction; 

        private ValueSourceType _rowSourceType = ValueSourceType.Unspecified;
        private DataRow _currentRow;
        private int _currentRowLength; 
        private DataRowState _rowState;
        private IEnumerator _rowEnumerator; 
 
        private TdsParser _parser;
        private TdsParserStateObject _stateObj; 
        private ArrayList _sortedColumnMappings;

        private SqlRowsCopiedEventHandler _rowsCopiedEventHandler;
 
        private static int _objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount); 
 
        // ctor
        // 
        public SqlBulkCopy(SqlConnection connection) {
            if(connection == null) {
                throw ADP.ArgumentNull("connection");
            } 
            _connection = connection;
            _columnMappings = new SqlBulkCopyColumnMappingCollection(); 
        } 

        public SqlBulkCopy(SqlConnection connection, SqlBulkCopyOptions copyOptions, SqlTransaction externalTransaction) 
            : this (connection) {

            _copyOptions = copyOptions;
            if(externalTransaction != null && IsCopyOption(SqlBulkCopyOptions.UseInternalTransaction)) { 
                throw SQL.BulkLoadConflictingTransactionOption();
            } 
 
            if(!IsCopyOption(SqlBulkCopyOptions.UseInternalTransaction)) {
                _externalTransaction = externalTransaction; 
            }
        }

        public SqlBulkCopy(string connectionString) : this (new SqlConnection(connectionString)) { 
            if(connectionString == null) {
                throw ADP.ArgumentNull("connectionString"); 
            } 
            _connection = new SqlConnection(connectionString);
            _columnMappings = new SqlBulkCopyColumnMappingCollection(); 
            _ownConnection = true;
        }

        public SqlBulkCopy(string connectionString, SqlBulkCopyOptions copyOptions) 
            : this (connectionString) {
            _copyOptions = copyOptions; 
        } 

        public int BatchSize { 
            get {
                return _batchSize;
            }
            set { 
                if(value >= 0) {
                    _batchSize = value; 
                } 
                else {
                    throw ADP.ArgumentOutOfRange("BatchSize"); 
                }
            }
        }
 
        public int BulkCopyTimeout {
            get { 
                return _timeout; 
            }
            set { 
                if(value < 0) {
                    throw SQL.BulkLoadInvalidTimeout(value);
                }
                _timeout = value; 
            }
        } 
 
        public SqlBulkCopyColumnMappingCollection ColumnMappings {
            get { 
                return _columnMappings;
            }
        }
 
        public string DestinationTableName {
            get { 
                return _destinationTableName; 
            }
            set { 
                if(value == null) {
                    throw ADP.ArgumentNull("DestinationTableName");
                }
                else if(value.Length == 0) { 
                    throw ADP.ArgumentOutOfRange("DestinationTableName");
                } 
                _destinationTableName = value; 
            }
        } 

        public int NotifyAfter {
            get {
                return _notifyAfter; 
            }
            set { 
                if(value >= 0) { 
                    _notifyAfter = value;
                } 
                else {
                    throw ADP.ArgumentOutOfRange("NotifyAfter");
                }
            } 
        }
 
        internal int ObjectID { 
            get {
                return _objectID; 
            }
        }

        public event SqlRowsCopiedEventHandler SqlRowsCopied { 
            add {
                _rowsCopiedEventHandler += value; 
            } 
            remove {
                _rowsCopiedEventHandler -= value; 
            }

        }
 
        internal SqlStatistics Statistics {
            get { 
                if(null != _connection) { 
                    if(_connection.StatisticsEnabled) {
                        return _connection.Statistics; 
                    }
                }
                return null;
            } 
        }
 
        //================================================================ 
        // IDisposable
        //=============================================================== 
        void IDisposable.Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
 
        }
 
        private bool IsCopyOption(SqlBulkCopyOptions copyOption) { 
            return (_copyOptions & copyOption) == copyOption;
        } 

        // Create and execute initial query to get information about the targettable
        //
        // devnote: most of the stuff here will be moved to the TDSParser's Run method 
        //
        private BulkCopySimpleResultSet CreateAndExecuteInitialQuery() { 
            string[] parts; 
            try {
                parts = MultipartIdentifier.ParseMultipartIdentifier (this.DestinationTableName, "[\"", "]\"", Res.SQL_BulkCopyDestinationTableName, true); 
            } catch (Exception e) {
                throw SQL.BulkLoadInvalidDestinationTable(this.DestinationTableName, e);
            }
            if (ADP.IsEmpty (parts[MultipartIdentifier.TableIndex])) { 
                throw SQL.BulkLoadInvalidDestinationTable(this.DestinationTableName, null);
            } 
            string TDSCommand; 
            BulkCopySimpleResultSet internalResults = new BulkCopySimpleResultSet();
 
            TDSCommand = "select @@trancount; SET FMTONLY ON select * from " + this.DestinationTableName + " SET FMTONLY OFF ";
            if(_connection.IsShiloh) {
                // If its a temp DB then try to connect
 
                string TableCollationsStoredProc;
                if (_connection.IsKatmaiOrNewer) { 
                    TableCollationsStoredProc = "sp_tablecollations_100"; 
                }
                else if (_connection.IsYukonOrNewer) { 
                    TableCollationsStoredProc = "sp_tablecollations_90";
                }
                else {
                    TableCollationsStoredProc = "sp_tablecollations"; 
                }
 
                string TableName = parts[MultipartIdentifier.TableIndex].Replace("'", "''"); 
                string SchemaName = parts[MultipartIdentifier.SchemaIndex];
                if (SchemaName != null) { 
                    SchemaName = SchemaName.Replace("'", "''");
                }
                string CatalogName = parts[MultipartIdentifier.CatalogIndex];
                if (TableName.Length > 0 && '#' == TableName[0] && ADP.IsEmpty (CatalogName)) { 
                    TDSCommand += String.Format((IFormatProvider)null, "exec tempdb..{0} N'{1}.{2}'",
                        TableCollationsStoredProc, 
                        SchemaName, 
                        TableName
                    ); 
                } else {
                    TDSCommand += String.Format((IFormatProvider)null, "exec {0}..{1} N'{2}.{3}'",
                        CatalogName,
                        TableCollationsStoredProc, 
                        SchemaName,
                        TableName 
                    ); 
                }
            } 

            Bid.Trace("<sc.SqlBulkCopy.CreateAndExecuteInitialQuery|INFO> Initial Query: '%ls' \n", TDSCommand);

            _parser.TdsExecuteSQLBatch(TDSCommand, this.BulkCopyTimeout, null, _stateObj); 
            _parser.Run(RunBehavior.UntilDone, null, null, internalResults, _stateObj);
            return internalResults; 
        } 

        // Matches associated columns with metadata from initial query 
        // builds and executes the update bulk command
        //
        private string AnalyzeTargetAndCreateUpdateBulkCommand(BulkCopySimpleResultSet internalResults) {
            _sortedColumnMappings = new ArrayList(); 
            StringBuilder updateBulkCommandText = new StringBuilder();
 
            if (_connection.IsShiloh && 0 == internalResults[CollationResultId].Count) { 
                throw SQL.BulkLoadNoCollation();
            } 

            Debug.Assert((internalResults != null), "Where are the results from the initial query?");

            updateBulkCommandText.Append("insert bulk " + this.DestinationTableName + " ("); 
            int nmatched = 0;               // number of columns that match and are accepted
            int nrejected = 0;              // number of columns that match but were rejected 
            bool rejectColumn;            // true if a column is rejected because of an excluded type 

            bool isInTransaction; 

            if(_parser.IsYukonOrNewer) {
                isInTransaction = _connection.HasLocalTransaction;
            } 
            else {
                isInTransaction = (bool)(0 < (SqlInt32)(internalResults[TranCountResultId][TranCountRowId][TranCountValueId])); 
            } 
            // Throw if there is a transaction but no flag is set
            if(isInTransaction && null == _externalTransaction && null == _internalTransaction && (_connection.Parser != null && _connection.Parser.CurrentTransaction != null && _connection.Parser.CurrentTransaction.IsLocal)) { 
                throw SQL.BulkLoadExistingTransaction();
            }

            // loop over the metadata for each column 
            //
            for(int i = 0; i < internalResults[MetaDataResultId].MetaData.Length; i++) { 
                _SqlMetaData metadata = internalResults[MetaDataResultId].MetaData[i]; 
                rejectColumn = false;
 
                // Check for excluded types
                //
                if((metadata.type == SqlDbType.Timestamp)
                    || ((metadata.isIdentity) && !IsCopyOption(SqlBulkCopyOptions.KeepIdentity))) { 
                    // remove metadata for excluded columns
                    internalResults[MetaDataResultId].MetaData[i] = null; 
                    rejectColumn = true; 
                    // we still need to find a matching column association
                } 

                // find out if this column is associated
                int assocId;
                for(assocId = 0; assocId < _localColumnMappings.Count; assocId++) { 
                    if((_localColumnMappings[assocId]._destinationColumnOrdinal == metadata.ordinal) ||
                        (UnquotedName(_localColumnMappings[assocId]._destinationColumnName) == metadata.column)) { 
                        if(rejectColumn) { 
                            nrejected++;       // count matched columns only
                            break; 
                        }

                        _sortedColumnMappings.Add(new _ColumnMapping(_localColumnMappings[assocId]._internalSourceColumnOrdinal, metadata));
                        nmatched++; 

                        if(nmatched > 1) { 
                            updateBulkCommandText.Append(", ");         // a leading comma for all but the first one 
                        }
 
                        // some datatypes need special handling ...
                        //
                        if(metadata.type == SqlDbType.Variant) {
                            AppendColumnNameAndTypeName(updateBulkCommandText, metadata.column, "sql_variant"); 
                        }
                        else if(metadata.type == SqlDbType.Udt) { 
                            // UDTs are sent as varbinary 
                            AppendColumnNameAndTypeName(updateBulkCommandText, metadata.column, "varbinary");
                        } 
                        else {
                            AppendColumnNameAndTypeName(updateBulkCommandText, metadata.column, metadata.type.ToString());
                        }
 
                        switch(metadata.metaType.NullableType) {
                            case TdsEnums.SQLNUMERICN: 
                            case TdsEnums.SQLDECIMALN: 
                                // decimal and numeric need to include precision and scale
                                // 
                                updateBulkCommandText.Append("(" + metadata.precision.ToString((IFormatProvider)null) + "," + metadata.scale.ToString((IFormatProvider)null) + ")");
                                break;
                            case TdsEnums.SQLUDT: {
                                    if (metadata.IsLargeUdt) { 
                                        updateBulkCommandText.Append("(max)");
                                    } else { 
                                        int size = metadata.length; 
                                        updateBulkCommandText.Append("(" + size.ToString((IFormatProvider)null) + ")");
                                    } 
                                    break;
                                }
                            case TdsEnums.SQLTIME:
                            case TdsEnums.SQLDATETIME2: 
                            case TdsEnums.SQLDATETIMEOFFSET:
                                // date, dateime2, and datetimeoffset need to include scale 
                                // 
                                updateBulkCommandText.Append("(" + metadata.scale.ToString((IFormatProvider)null) + ")");
                                break; 
                            default: {
                                    // for non-long non-fixed types we need to add the Size
                                    //
                                    if(!metadata.metaType.IsFixed && !metadata.metaType.IsLong) { 
                                        int size = metadata.length;
                                        switch(metadata.metaType.NullableType) { 
                                            case TdsEnums.SQLNCHAR: 
                                            case TdsEnums.SQLNVARCHAR:
                                            case TdsEnums.SQLNTEXT: 
                                                size /= 2;
                                                break;
                                            default:
                                                break; 
                                        }
                                        updateBulkCommandText.Append("(" + size.ToString((IFormatProvider)null) + ")"); 
                                    } 
                                    else if(metadata.metaType.IsPlp && metadata.metaType.SqlDbType != SqlDbType.Xml) {
                                        // Partial length column prefix (max) 
                                        updateBulkCommandText.Append("(max)");
                                    }
                                    break;
                                } 
                        }
 
                        if(_connection.IsShiloh) { 
                            // Shiloh or above!
                            // get collation for column i 

                            Result rowset = internalResults[CollationResultId];
                                object rowvalue = rowset[i][CollationId];
                                if(rowvalue != null) { 
                                    Debug.Assert(rowvalue is SqlString);
                                    SqlString collation_name = (SqlString)rowvalue; 
                                    if(!collation_name.IsNull) { 
                                        updateBulkCommandText.Append(" COLLATE " + collation_name.ToString());
                                        if(null != _SqlDataReaderRowSource) { 
                                            // On SqlDataReader we can verify the sourcecolumn collation!
                                            int sourceColumnId = _localColumnMappings[assocId]._internalSourceColumnOrdinal;
                                            int destinationLcid = internalResults[MetaDataResultId].MetaData[i].collation.LCID;
                                            int sourceLcid = _SqlDataReaderRowSource.GetLocaleId(sourceColumnId); 
                                            if(sourceLcid != destinationLcid) {
                                                throw SQL.BulkLoadLcidMismatch(sourceLcid, _SqlDataReaderRowSource.GetName(sourceColumnId), destinationLcid, metadata.column); 
                                            } 
                                        }
                                    } 
                                }
                        }
                        break;
                    } // end if found 
                } // end of (inner) for loop
                if(assocId == _localColumnMappings.Count) { 
                    // remove metadata for unmatched columns 
                    internalResults[MetaDataResultId].MetaData[i] = null;
                } 
            } // end of (outer) for loop

            // all columnmappings should have matched up
            if(nmatched + nrejected != _localColumnMappings.Count) { 
                throw (SQL.BulkLoadNonMatchingColumnMapping());
            } 
 
            updateBulkCommandText.Append(")");
 
            if((_copyOptions & (
                    SqlBulkCopyOptions.KeepNulls
                    | SqlBulkCopyOptions.TableLock
                    | SqlBulkCopyOptions.CheckConstraints 
                    | SqlBulkCopyOptions.FireTriggers)) != SqlBulkCopyOptions.Default) {
                bool addSeparator = false;  // insert a comma character if multiple options in list ... 
                updateBulkCommandText.Append(" with ("); 
                if(IsCopyOption(SqlBulkCopyOptions.KeepNulls)) {
                    updateBulkCommandText.Append("KEEP_NULLS"); 
                    addSeparator = true;
                }
                if(IsCopyOption(SqlBulkCopyOptions.TableLock)) {
                    updateBulkCommandText.Append((addSeparator ? ", " : "") + "TABLOCK"); 
                    addSeparator = true;
                } 
                if(IsCopyOption(SqlBulkCopyOptions.CheckConstraints)) { 
                    updateBulkCommandText.Append((addSeparator ? ", " : "") + "CHECK_CONSTRAINTS");
                    addSeparator = true; 
                }
                if(IsCopyOption(SqlBulkCopyOptions.FireTriggers)) {
                    updateBulkCommandText.Append((addSeparator ? ", " : "") + "FIRE_TRIGGERS");
                    addSeparator = true; 
                }
                updateBulkCommandText.Append(")"); 
            } 
            return (updateBulkCommandText.ToString());
        } 

        // submitts the updatebulk command
        //
        private void SubmitUpdateBulkCommand(BulkCopySimpleResultSet internalResults, string TDSCommand) { 
            _parser.TdsExecuteSQLBatch(TDSCommand, this.BulkCopyTimeout, null, _stateObj);
            _parser.Run(RunBehavior.UntilDone, null, null, null, _stateObj); 
        } 

        // Starts writing the Bulkcopy data stream 
        //
        private void WriteMetaData(BulkCopySimpleResultSet internalResults) {
            _stateObj.SetTimeoutSeconds(this.BulkCopyTimeout);
 
            _SqlMetaDataSet metadataCollection = internalResults[MetaDataResultId].MetaData;
            _stateObj._outputMessageType = TdsEnums.MT_BULK; 
            _parser.WriteBulkCopyMetaData(metadataCollection, _sortedColumnMappings.Count, _stateObj); 
        }
 
        //================================================================
        // Close()
        //
        // Terminates the bulk copy operation. 
        // Must be called at the end of the bulk copy session.
        //================================================================ 
        public void Close() { 
            if(_insideRowsCopiedEvent) {
                throw SQL.InvalidOperationInsideEvent(); 
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        } 

        private void Dispose(bool disposing) { 
            if(disposing) { 
                // dispose dependend objects
                _columnMappings = null; 
                _parser = null;
                try {

                    // This code should be removed in RTM, its not needed 
                    // The _internalTransaction should just be a internal variable.
                    // Start Cut 
                    try { 
                        // cleanup managed objects
                        if(_internalTransaction != null) { 
                            // do not commit on dispose but rollback
                            _internalTransaction.Rollback();
                            _internalTransaction.Dispose ();
                            _internalTransaction = null; 
                        }
                    } 
                    catch(Exception e) { 
                        //
                        if(!ADP.IsCatchableExceptionType(e)) { 
                            throw;
                        }
                        ADP.TraceExceptionWithoutRethrow(e);
                    } 
                    // End Cut
                } 
                finally { 
                    if(_connection != null) {
                        if(_ownConnection) { 
                            _connection.Dispose();
                        }
                        _connection = null;
                    } 
                }
            } 
            // free unmanaged objects 
        }
 
        // unified method to read a value from the current row
        //
        private object GetValueFromSourceRow(int columnOrdinal, _SqlMetaData metadata, int[] UseSqlValue, int destRowIndex) {
 
            if (UseSqlValue[destRowIndex] == 0) {  // If we haven't determined if the source and dest should marshal via sqlvalue
                UseSqlValue[destRowIndex] = -1; // Default state, they should marshal via get value 
 
                // Special case code..
                // We can't just call GetValue for SqlDecimals, the SqlDecmail is a superset of of the CLR.Decmimal values 
                // Check if we are indeed going from a SqlDecimal to a SqlDecimal, if we are use the true SqlDecimal value to avoid overflows

                if (metadata.metaType.NullableType == TdsEnums.SQLDECIMALN || metadata.metaType.NullableType == TdsEnums.SQLNUMERICN) {
                    Type t = null; 
                    switch(_rowSourceType) {	
                        case ValueSourceType.IDataReader: 
                            if (null != _SqlDataReaderRowSource) { 
                                t = _SqlDataReaderRowSource.GetFieldType(columnOrdinal);
                            } 
                         break;

                        case ValueSourceType.DataTable:
                        case ValueSourceType.RowArray: 
                           Debug.Assert(_currentRow != null, "uninitialized _currentRow");
                           Debug.Assert(columnOrdinal < _currentRowLength, "inconsistency of length of rows from rowsource!"); 
                           t = _currentRow.Table.Columns[columnOrdinal].DataType; 
                        break;
                    } 

                    if (typeof(SqlDecimal) == t || typeof(Decimal) == t) {
                        UseSqlValue[destRowIndex] = (int)SqlBuffer.StorageType.Decimal;  // Source Type Decimal
                    } 
                    else if (typeof(SqlDouble) == t || typeof (double) == t) {
                        UseSqlValue[destRowIndex] = (int)SqlBuffer.StorageType.Double;  // Source Type SqlDouble 
                    } 
                    else if (typeof(SqlSingle) == t || typeof (float) == t) {
                        UseSqlValue[destRowIndex] = (int)SqlBuffer.StorageType.Single;  // Source Type SqlSingle 
                    }
                }
            }
 
            switch(_rowSourceType) {
                case ValueSourceType.IDataReader: 
                    if (null != _SqlDataReaderRowSource) { 
                        switch (UseSqlValue[destRowIndex]) {
                            case (int)SqlBuffer.StorageType.Decimal : { 
                                return _SqlDataReaderRowSource.GetSqlDecimal(columnOrdinal);
                            }
                            case (int)SqlBuffer.StorageType.Double : {
                                return new SqlDecimal (_SqlDataReaderRowSource.GetSqlDouble (columnOrdinal).Value); 
                            }
                            case (int)SqlBuffer.StorageType.Single : { 
                                return new SqlDecimal (_SqlDataReaderRowSource.GetSqlSingle(columnOrdinal).Value); 
                            }
                            default: 
                                return _SqlDataReaderRowSource.GetValue(columnOrdinal);
                        }
                    }
                    else { 
                        return ((IDataReader)_rowSource).GetValue(columnOrdinal);
                    } 
 
                case ValueSourceType.DataTable:
                case ValueSourceType.RowArray: 
                {
                    Debug.Assert(_currentRow != null, "uninitialized _currentRow");
                    Debug.Assert(columnOrdinal < _currentRowLength, "inconsistency of length of rows from rowsource!");
                    object currentRowValue = _currentRow[columnOrdinal]; 
                    if (null != currentRowValue && DBNull.Value != currentRowValue) {
                            if ((int)SqlBuffer.StorageType.Single == UseSqlValue[destRowIndex] || 
                                (int)SqlBuffer.StorageType.Double == UseSqlValue[destRowIndex] || 
                                (int)SqlBuffer.StorageType.Decimal == UseSqlValue[destRowIndex]) {								
                                INullable inullable = currentRowValue as INullable; 
                                if (null == inullable || !inullable.IsNull) { // If the value is are a CLR Type, or are not null
                                    switch ((SqlBuffer.StorageType)UseSqlValue[destRowIndex]) {
                                        case SqlBuffer.StorageType.Single : {
                                            if (null != inullable) { 
                                                return  new SqlDecimal (((SqlSingle)currentRowValue).Value);
                                            } 
                                            else { 
                                                float f = (float)currentRowValue;
                                                if (!float.IsNaN (f)) { 
                                                   return new SqlDecimal (f);
                                                }
                                                break;
                                            } 
                                        }
                                        case SqlBuffer.StorageType.Double : { 
                                            if (null != inullable) { 
                                                return new SqlDecimal (((SqlDouble)currentRowValue).Value);
                                            } 
                                            else {
                                                double d = (double)currentRowValue;
                                                if (!double.IsNaN (d)) {
                                                    return  new SqlDecimal (d); 
                                                }
                                                break; 
                                            } 
                                        }
                                        case SqlBuffer.StorageType.Decimal : { 
                                            if (null != inullable) {
                                                return (SqlDecimal)currentRowValue;
                                            } else {
                                                return   new SqlDecimal ((Decimal)currentRowValue); 
                                            }
                                        } 
                                    } 
                                }
                            } 
                    }
                    return currentRowValue;
                }
 
                default:
                    Debug.Assert(false, "ValueSourcType unspecified"); 
                    throw ADP.NotSupported(); 
            }
        } 

        // unified method to read a row from the current rowsource
        //
        private bool ReadFromRowSource() { 
            switch(_rowSourceType) {
                case ValueSourceType.IDataReader: 
                    return ((IDataReader)_rowSource).Read(); 
                case ValueSourceType.DataTable:
                    // repeat until we get a row that is not deleted or there are no more rows ... 
                    do {
                        if(!_rowEnumerator.MoveNext()) {
                            return false;
                        } 
                        _currentRow = (DataRow)_rowEnumerator.Current;
                        _currentRowLength = _currentRow.ItemArray.Length; 
                    } 
                    while(((_currentRow.RowState & DataRowState.Deleted) != 0)           // repeat on delete row - always
                        || ((_rowState != 0) && ((_currentRow.RowState & _rowState) == 0)));       // repeat if there is an unexpected rowstate 

                    return true;
                case ValueSourceType.RowArray:
                    Debug.Assert(_rowEnumerator != null, "uninitialized _rowEnumerator"); 
                    if(_rowEnumerator.MoveNext()) {
                        _currentRow = (DataRow)_rowEnumerator.Current; 
                        _currentRowLength = _currentRow.ItemArray.Length; 
                        return true;
                    } 
                    return false;
                default:
                    Debug.Assert(false, "ValueSourcType unspecified");
                    throw ADP.NotSupported(); 
            }
        } 
 
        //
        // 
        private void CreateOrValidateConnection(string method) {
            if(null == _connection) {
                throw ADP.ConnectionRequired(method);
            } 
            if (_connection.IsContextConnection) {
                throw SQL.NotAvailableOnContextConnection(); 
            } 

            if(_ownConnection && _connection.State != ConnectionState.Open) { 
                _connection.Open();
            }

            // close any non MARS dead readers, if applicable, and then throw if still busy. 
            _connection.ValidateConnectionForExecute(method, null);
 
            // if we have a transaction, check to ensure that the active 
            // connection property matches the connection associated with
            // the transaction 
            if(null != _externalTransaction && _connection != _externalTransaction.Connection) {
                throw ADP.TransactionConnectionMismatch();
            }
        } 

        // Appends columnname in square brackets, a space and the typename to the query 
        // putting the name in quotes also requires doubling existing ']' so that they are not mistaken for 
        // the closing quote
        // example: abc will become [abc] but abc[] will becom [abc[]]] 
        //
        private void AppendColumnNameAndTypeName(StringBuilder query, string columnName, string typeName) {
            query.Append('[');
            query.Append(columnName.Replace("]", "]]")); 
            query.Append("] ");
            query.Append(typeName); 
        } 

        private string UnquotedName(string name) { 
            if(ADP.IsEmpty(name)) return null;
            if(name[0] == '[') {
                int l = name.Length;
                Debug.Assert(name[l - 1] == ']', "Name starts with [ but doesn not end with ]"); 
                name = name.Substring(1, l - 2);
            } 
            return name; 
        }
 
        private object ValidateBulkCopyVariant(object value) {
            // from the spec:
            // "The only acceptable types are ..."
            // GUID, BIGVARBINARY, BIGBINARY, BIGVARCHAR, BIGCHAR, NVARCHAR, NCHAR, BIT, INT1, INT2, INT4, INT8, 
            // MONEY4, MONEY, DECIMALN, NUMERICN, FTL4, FLT8, DATETIME4 and DATETIME
            // 
            MetaType metatype = MetaType.GetMetaTypeFromValue(value); 
            switch(metatype.TDSType) {
                case TdsEnums.SQLFLT4: 
                case TdsEnums.SQLFLT8:
                case TdsEnums.SQLINT8:
                case TdsEnums.SQLINT4:
                case TdsEnums.SQLINT2: 
                case TdsEnums.SQLINT1:
                case TdsEnums.SQLBIT: 
                case TdsEnums.SQLBIGVARBINARY: 
                case TdsEnums.SQLBIGVARCHAR:
                case TdsEnums.SQLUNIQUEID: 
                case TdsEnums.SQLNVARCHAR:
                case TdsEnums.SQLDATETIME:
                case TdsEnums.SQLMONEY:
                case TdsEnums.SQLNUMERICN: 
                case TdsEnums.SQLDATE:
                case TdsEnums.SQLTIME: 
                case TdsEnums.SQLDATETIME2: 
                case TdsEnums.SQLDATETIMEOFFSET:
                    if (value is INullable) {   // Current limitation in the SqlBulkCopy Variant code limits BulkCopy to CLR/COM Types. 
                        return MetaType.GetComValueFromSqlVariant (value);
                    } else {
                        return value;
                    } 
                default:
                    throw SQL.BulkLoadInvalidVariantValue(); 
            } 
        }
 
        private object ConvertValue(object value, _SqlMetaData metadata) {
            if(ADP.IsNull(value)) {
                if(!metadata.isNullable) {
                    throw SQL.BulkLoadBulkLoadNotAllowDBNull(metadata.column); 
                }
                return value; 
            } 
            MetaType type = metadata.metaType;
            try { 
                MetaType mt;
                switch(type.NullableType) {
                    case TdsEnums.SQLNUMERICN:
                    case TdsEnums.SQLDECIMALN: { 
                        mt = MetaType.GetMetaTypeFromSqlDbType (type.SqlDbType, false);
                        value = SqlParameter.CoerceValue (value, mt); 
 
                        // Convert Source Decimal Percision and Scale to Destination Percision and Scale
                        // Fix Bug: 385971 sql decimal data could get corrupted on insert if the scale of 
                        // the source and destination weren't the same.  The BCP protocal, specifies the
                        // scale of the incoming data in the insert statement, we just tell the server we
                        // are inserting the same scale back. This then created a bug inside the BCP opperation
                        // if the scales didn't match.  The fix is to do the same thing that SQL Paramater does, 
                        // and adjust the scale before writing.  In Orcas is scale adjustment should be removed from
                        // SqlParamater and SqlBulkCopy and Isoloated inside SqlParamater.CoerceValue, but becouse of 
                        // where we are in the cycle, the changes must be kept at minimum, so I'm just bringing the 
                        // code over to SqlBulkCopy.
 
                        SqlDecimal sqlValue;
                        if (value is SqlDecimal) {
                            sqlValue = (SqlDecimal)value;
                        } 
                        else {
                            sqlValue = new SqlDecimal((Decimal)value); 
                        } 

                        if (sqlValue.Scale  != metadata.scale) { 
                           sqlValue = TdsParser.AdjustSqlDecimalScale(sqlValue, metadata.scale);
                           value = sqlValue;
                        }
 
                        if (sqlValue.Precision > metadata.precision) {
                            throw SQL.BulkLoadCannotConvertValue (value.GetType (), mt ,ADP.ParameterValueOutOfRange(sqlValue)); 
                        } 
                    }
                    break; 

                    case TdsEnums.SQLINTN:
                    case TdsEnums.SQLFLTN:
                    case TdsEnums.SQLFLT4: 
                    case TdsEnums.SQLFLT8:
                    case TdsEnums.SQLMONEYN: 
                    case TdsEnums.SQLDATETIM4: 
                    case TdsEnums.SQLDATETIME:
                    case TdsEnums.SQLDATETIMN: 
                    case TdsEnums.SQLBIT:
                    case TdsEnums.SQLBITN:
                    case TdsEnums.SQLUNIQUEID:
                    case TdsEnums.SQLBIGBINARY: 
                    case TdsEnums.SQLBIGVARBINARY:
                    case TdsEnums.SQLIMAGE: 
                    case TdsEnums.SQLBIGCHAR: 
                    case TdsEnums.SQLBIGVARCHAR:
                    case TdsEnums.SQLTEXT: 
                    case TdsEnums.SQLDATE:
                    case TdsEnums.SQLTIME:
                    case TdsEnums.SQLDATETIME2:
                    case TdsEnums.SQLDATETIMEOFFSET: 
                        mt = MetaType.GetMetaTypeFromSqlDbType (type.SqlDbType, false);
                        value = SqlParameter.CoerceValue (value, mt); 
                    break; 
                    case TdsEnums.SQLNCHAR:
                    case TdsEnums.SQLNVARCHAR: 
                    case TdsEnums.SQLNTEXT:
                        mt = MetaType.GetMetaTypeFromSqlDbType (type.SqlDbType, false);
                        value = SqlParameter.CoerceValue (value, mt);
                        int len = (value is string) ? ((string)value).Length : ((SqlString)value).Value.Length; 
                        if (len  > metadata.length / 2) {
                            throw SQL.BulkLoadStringTooLong(); 
                        } 
                        break;
 
                    case TdsEnums.SQLVARIANT:
                        value = ValidateBulkCopyVariant(value);
                        break;
 
                    case TdsEnums.SQLUDT:
                        // UDTs are sent as varbinary so we need to get the raw bytes 
                        // unlike other types the parser does not like SQLUDT in form of SqlType 
                        // so we cast to a CLR type.
 
                        // Hack for type system version knob - only call GetBytes if the value is not already
                        // in byte[] form.
                        if (value.GetType() != typeof(byte[])) {
                            value = _connection.GetBytes(value); 
                        }
                        break; 
 
                    case TdsEnums.SQLXMLTYPE:
                        // Could be either string, SqlCachedBuffer or XmlReader 
                        Debug.Assert((value is XmlReader) || (value is SqlCachedBuffer) || (value is string) || (value is SqlString), "Invalid value type of Xml datatype");
                        if(value is XmlReader) {
                            value = MetaType.GetStringFromXml((XmlReader)value);
                        } 
                        break;
 
                    default: 
                        Debug.Assert(false, "Unknown TdsType!" + type.NullableType.ToString("x2", (IFormatProvider)null));
                        throw SQL.BulkLoadCannotConvertValue(value.GetType(), metadata.metaType, null); 
                }
                return value;
            }
            catch(Exception e) { 
                if(!ADP.IsCatchableExceptionType(e)) {
                    throw; 
                } 
                throw SQL.BulkLoadCannotConvertValue(value.GetType(), metadata.metaType, e);
            } 
        }

        public void WriteToServer(IDataReader reader) {
            SqlConnection.ExecutePermission.Demand(); 

            SqlStatistics statistics = Statistics; 
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                if(reader == null) { 
                    throw new ArgumentNullException("reader");
                }
                _rowSource = reader;
                _SqlDataReaderRowSource = _rowSource as SqlDataReader; 
                _rowSourceType = ValueSourceType.IDataReader;
                WriteRowSourceToServer(reader.FieldCount); 
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
            }
        }

        public void WriteToServer(DataTable table) { 
            WriteToServer(table, 0);
        } 
 
        public void WriteToServer(DataTable table, DataRowState rowState) {
            SqlConnection.ExecutePermission.Demand(); 

            SqlStatistics statistics = Statistics;
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                if(table == null) {
                    throw new ArgumentNullException("table"); 
                } 
                _rowState = rowState & ~DataRowState.Deleted;
                _rowSource = table; 
                _SqlDataReaderRowSource = null;
                _rowSourceType = ValueSourceType.DataTable;
                _rowEnumerator = table.Rows.GetEnumerator();
                WriteRowSourceToServer(table.Columns.Count); 
            }
            finally { 
                SqlStatistics.StopTimer(statistics); 
            }
        } 

        public void WriteToServer(DataRow[] rows) {
            SqlConnection.ExecutePermission.Demand();
 
            SqlStatistics statistics = Statistics;
            try { 
                statistics = SqlStatistics.StartTimer(Statistics); 
                if(rows == null) {
                    throw new ArgumentNullException("rows"); 
                }
                if(rows.Length == 0) {
                    return; // nothing to do. user passed us an empty array
                } 
                DataTable table = rows[0].Table;
                Debug.Assert(null != table, "How can we have rows without a table?"); 
                _rowSource = rows; 
                _SqlDataReaderRowSource = null;
                _rowSourceType = ValueSourceType.RowArray; 
                _rowEnumerator = rows.GetEnumerator();
                WriteRowSourceToServer(table.Columns.Count);
            }
            finally { 
                SqlStatistics.StopTimer(statistics);
            } 
        } 

        private void WriteRowSourceToServer(int columnCount) { 
            CreateOrValidateConnection(SQL.WriteToServer);

            // Find out if we need to get the source column ordinal from the datatable
            // 
            bool unspecifiedColumnOrdinals = false;
 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    _columnMappings.ReadOnly = true; 
                    _localColumnMappings = _columnMappings;
                    if(_localColumnMappings.Count > 0) { 
                        _localColumnMappings.ValidateCollection();
                        foreach(SqlBulkCopyColumnMapping bulkCopyColumn in _localColumnMappings) {
                            if(bulkCopyColumn._internalSourceColumnOrdinal == -1) {
                                unspecifiedColumnOrdinals = true; 
                                break;
                            } 
                        } 
                    }
                    else { 
                        _localColumnMappings = new SqlBulkCopyColumnMappingCollection();
                        _localColumnMappings.CreateDefaultMapping(columnCount);
                    }
 
                    // perf: If the user specified all column ordinals we do not need to get a schematable
                    // 
                    if(unspecifiedColumnOrdinals) { 
                        int index = -1;
                        unspecifiedColumnOrdinals = false; 

                        // Match up sourceColumn names with sourceColumn ordinals
                        //
                        if(_localColumnMappings.Count > 0) { 
                            foreach(SqlBulkCopyColumnMapping bulkCopyColumn in _localColumnMappings) {
                                if(bulkCopyColumn._internalSourceColumnOrdinal == -1) { 
                                    string unquotedColumnName = UnquotedName(bulkCopyColumn.SourceColumn); 

                                    switch(this._rowSourceType) { 
                                        case ValueSourceType.DataTable:
                                            index = ((DataTable)_rowSource).Columns.IndexOf(unquotedColumnName);
                                            break;
                                        case ValueSourceType.RowArray: 
                                            index = ((DataRow[])_rowSource)[0].Table.Columns.IndexOf(unquotedColumnName);
                                            break; 
                                        case ValueSourceType.IDataReader: 
                                            try {
                                                index = ((IDataRecord)this._rowSource).GetOrdinal(unquotedColumnName); 
                                            }
                                            catch(IndexOutOfRangeException e) {
                                                throw (SQL.BulkLoadNonMatchingColumnName(unquotedColumnName, e));
                                            } 
                                            break;
                                    } 
                                    if(index == -1) { 
                                        throw (SQL.BulkLoadNonMatchingColumnName(unquotedColumnName));
                                    } 
                                    bulkCopyColumn._internalSourceColumnOrdinal = index;
                                }
                            }
                        } 
                    }
                    WriteToServerInternal(); 
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG
            } 
            catch(System.OutOfMemoryException e) {
                _connection.Abort(e); 
                throw; 
            }
            catch(System.StackOverflowException e) { 
                _connection.Abort(e);
                throw;
            }
            catch(System.Threading.ThreadAbortException e) { 
                _connection.Abort(e);
                throw; 
            } 
            finally {
                _columnMappings.ReadOnly = false; 
            }
        }

        //=============================================================== 
        // WriteToServerInternal()
        // 
        // Writes the entire data to the server 
        //================================================================
        private void WriteToServerInternal() { 
            string updateBulkCommandText = null;
            int rowsInCurrentBatch;
            bool moreData = false;
            bool abortOperation = false; 
            int[] UseSqlValue = null;                           // Used to store the state if a sqlvalue should be returned when using the sqldatareader
            int localBatchSize = _batchSize;                    // changes will not be accepted while this method executes 
            bool batchCopyMode = false; 
            if (_batchSize > 0)
                batchCopyMode = true; 
            Exception exception = null;
            _rowsCopied = 0;

            // must have a destination table name 
            if(_destinationTableName == null) {
                throw SQL.BulkLoadMissingDestinationTable(); 
            } 
            // must have at least one row on the source
            if(!ReadFromRowSource()) { 
                return;
            }
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                bool processFinallyBlock = true;
                _parser = _connection.Parser; 
                _stateObj = _parser.GetSession(this); 
                _stateObj._bulkCopyOpperationInProgress = true;
                try { 
                    _stateObj.StartSession(ObjectID);

                    BulkCopySimpleResultSet internalResults;
                    try { 
                        internalResults = CreateAndExecuteInitialQuery();
                    } 
                    catch(SqlException e) { 
                        throw SQL.BulkLoadInvalidDestinationTable(_destinationTableName, e);
                    } 

                    _rowsUntilNotification = _notifyAfter;

                    updateBulkCommandText = AnalyzeTargetAndCreateUpdateBulkCommand(internalResults); 

                    if(_sortedColumnMappings.Count == 0) { 
                        // nothing to do ... 
                        return;
                    } 

                    // initiate the bulk insert and send the specified number of rows to the target
                    // eventually we need to repeat that until there is no more data on the source
                    // 
                    _stateObj.SniContext=SniContext.Snix_SendRows;
                    do { 
                        // if not already in an transaction initiate transaction 
                        if(IsCopyOption(SqlBulkCopyOptions.UseInternalTransaction)) {
                            _internalTransaction = _connection.BeginTransaction(); 
                        }
                        // from here everything should happen transacted
                        //
 
                        // At this step we start to write data to the stream.
                        // It might not have been written out to the server yet 
                        SubmitUpdateBulkCommand(internalResults, updateBulkCommandText); 

                        // starting here we  h a v e  to cancel out of the current operation 
                        //
                        try {
                            WriteMetaData(internalResults);
 
                            object[] rowValues = new object[_sortedColumnMappings.Count];
 
                            if (null == UseSqlValue) { 
                                UseSqlValue = new int[rowValues.Length]; // take advantage of the the CLR default initilization to all zero..
                            } 

                            object value;
                            rowsInCurrentBatch = localBatchSize;
                            do { 

                            // The do-loop sends an entire row to the server. 
                            // the loop repeats until the max number of rows in a batch is sent, 
                            // all rows have been sent or an exception happened
                            // 
                            for(int i = 0; i < rowValues.Length; i++) {
                                _ColumnMapping bulkCopyColumn = (_ColumnMapping)_sortedColumnMappings[i];
                                _SqlMetaData metadata = bulkCopyColumn._metadata;
                                value = GetValueFromSourceRow(bulkCopyColumn._sourceColumnOrdinal, metadata, UseSqlValue ,i); 
                                rowValues[i] =  ConvertValue(value, metadata);
                            } 
 
                                // Write the entire row
                                // This might cause packets to be written out to the server 
                                //
                                _parser.WriteByte(TdsEnums.SQLROW, _stateObj);
                                for(int i = 0; i < rowValues.Length; i++) {
                                    _ColumnMapping bulkCopyColumn = (_ColumnMapping)_sortedColumnMappings[i]; 
                                    _SqlMetaData metadata = bulkCopyColumn._metadata;
 
                                    if(metadata.type != SqlDbType.Variant) { 
                                        _parser.WriteBulkCopyValue(rowValues[i], metadata, _stateObj);
                                    } 
                                    else {
                                        _parser.WriteSqlVariantDataRowValue(rowValues[i], _stateObj);
                                    }
                                } 
                                _rowsCopied++;
 
                                // Fire event logic 
                                if(_notifyAfter > 0) {                      // no action if no value specified
                                    // (0=no notification) 
                                    if(_rowsUntilNotification > 0) {       // > 0?
                                        if(--_rowsUntilNotification == 0) {        // decrement counter
                                            // Fire event during operation. This is the users chance to abort the operation
                                            try { 
                                                // it's also the user's chance to cause an exception ...
                                                _stateObj.BcpLock=true; 
                                                abortOperation = FireRowsCopiedEvent(_rowsCopied); 
                                                Bid.Trace("<sc.SqlBulkCopy.WriteToServerInternal|INFO> \n");
 
                                                // just in case some pathological person closes the target connection ...
                                                if (ConnectionState.Open != _connection.State) {
                                                    break;
                                                } 
                                            }
                                            catch(Exception e) { 
                                                // 
                                                if(!ADP.IsCatchableExceptionType(e)) {
                                                    throw; 
                                                }
                                                exception = OperationAbortedException.Aborted(e);
                                                break;
                                            } 
                                            finally {
                                                _stateObj.BcpLock = false; 
                                            } 
                                            if(abortOperation) {
                                                break; 
                                            }
                                            _rowsUntilNotification = _notifyAfter;
                                        }
                                    } 
                                }
                                if(_rowsUntilNotification > _notifyAfter) {    // if the specified counter decreased we update 
                                    _rowsUntilNotification = _notifyAfter;      // decreased we update otherwise not 
                                }
 
                                // one more chance to cause an exception ...
                                moreData = ReadFromRowSource(); // proceed to the next row

                                if(batchCopyMode) { 
                                    rowsInCurrentBatch--;
                                    if(rowsInCurrentBatch == 0) { 
                                        break; 
                                    }
                                } 
                            } while(moreData);
                        }
                        catch(Exception e) {
                            if(ADP.IsCatchableExceptionType(e)) { 
                                _stateObj.CancelRequest();
                            } 
                            throw; 
                        }
 
                        if(ConnectionState.Open != _connection.State) {
                            throw ADP.OpenConnectionRequired(SQL.WriteToServer, _connection.State);
                        }
 
                        _parser.WriteBulkCopyDone(_stateObj);
                        _parser.Run(RunBehavior.UntilDone, null, null, null, _stateObj); 
 
                        if (abortOperation || null!=exception) {
                            throw OperationAbortedException.Aborted(exception); 
                        }

                        // commit transaction (batchcopymode only)
                        // 
                        if(null != _internalTransaction) {
                            _internalTransaction.Commit(); 
                            _internalTransaction = null; 
                        }
                    } while(moreData); 

                    _localColumnMappings = null;
                }
                catch(Exception e) { 
                    //
                    processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                    if(processFinallyBlock) { 
                        _stateObj._internalTimeout = false; // a timeout in ExecuteDone results in SendAttention/ProcessAttention. All we need to do is clear the flag
                        if(null != _internalTransaction) { 
                            if(!_internalTransaction.IsZombied) {
                                _internalTransaction.Rollback();
                            }
                            _internalTransaction = null; 
                        }
                    } 
                    throw; 
                }
                finally { 
                    Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to WriteToServerInternal");  // you need to setup for a thread abort somewhere before you call this method
                    if(processFinallyBlock && null != _stateObj) {
                        _stateObj.CloseSession();
#if DEBUG 
                        _stateObj.InvalidateDebugOnlyCopyOfSniContext();
#endif 
                    } 
                }
            } 
            finally {
                if(null != _stateObj) {
                    _stateObj._bulkCopyOpperationInProgress = false;
                    _stateObj = null; 
                }
            } 
        } 

        private void OnRowsCopied(SqlRowsCopiedEventArgs value) { 
            SqlRowsCopiedEventHandler handler = _rowsCopiedEventHandler;
            if(handler != null) {
                handler(this, value);
            } 
        }
        // fxcop: 
        // Use the .Net Event System whenever appropriate. 

        private bool FireRowsCopiedEvent(long rowsCopied) { 
            SqlRowsCopiedEventArgs eventArgs = new SqlRowsCopiedEventArgs(rowsCopied);
            try {
                _insideRowsCopiedEvent = true;
                this.OnRowsCopied(eventArgs); 
            }
            finally { 
                _insideRowsCopiedEvent = false; 
            }
            return eventArgs.Abort; 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlBulkCopy.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
// todo list:
// * An ID column need to be ignored - even if there is an association 
// * Spec: ID columns will be ignored - even if there is an association
// * Spec: How do we publish CommandTimeout on the bcpoperation?
//
 
namespace System.Data.SqlClient {
    using System; 
    using System.Collections; 
    using System.ComponentModel;
    using System.Data; 
    using System.Data.Common;
    using System.Data.Sql;
    using System.Data.SqlTypes;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution; 
    using System.Text;
    using System.Threading; 
    using System.Xml;

    // -------------------------------------------------------------------------------------------------
    // this internal class helps us to associate the metadata (from the target) 
    // with columnordinals (from the source)
    // 
    sealed internal class _ColumnMapping { 
        internal int _sourceColumnOrdinal;
        internal _SqlMetaData _metadata; 

        internal _ColumnMapping(int columnId, _SqlMetaData metadata) {
            _sourceColumnOrdinal = columnId;
            _metadata = metadata; 
        }
    } 
 
    sealed internal class Row {
        private object[] _dataFields; 

        internal Row(int rowCount) {
            _dataFields = new object[rowCount];
        } 

        internal object[] DataFields { 
            get { 
                return _dataFields;
            } 
        }

        internal object this[int index] {
            get { 
                return _dataFields[index];
            } 
        } 
    }
 
    // the controlling class for one result (metadata + rows)
    //
    sealed internal class Result {
        private _SqlMetaDataSet _metadata; 
        private ArrayList _rowset;
 
        internal Result(_SqlMetaDataSet metadata) { 
            this._metadata = metadata;
            this._rowset = new ArrayList(); 
        }

        internal int Count {
            get { 
                return _rowset.Count;
            } 
        } 

        internal _SqlMetaDataSet MetaData { 
            get {
                return _metadata;
            }
        } 

        internal Row this[int index] { 
            get { 
                return (Row)_rowset[index];
            } 
        }

        internal void AddRow(Row row) {
            _rowset.Add(row); 
        }
    } 
 
    // A wrapper object for metadata and rowsets returned by our initial queries
    // 
    sealed internal class BulkCopySimpleResultSet {
        private ArrayList _results;                   // the list of results
        private Result resultSet;                     // the current result
        private int[] indexmap;                       // associates columnids with indexes in the rowarray 

        // c-tor 
        // 
        internal BulkCopySimpleResultSet() {
            _results = new ArrayList(); 
        }

        // indexer
        // 
        internal Result this[int idx] {
            get { 
                return (Result)_results[idx]; 
            }
        } 
        // callback function for the tdsparser
        // note that setting the metadata adds a resultset
        //
        internal void SetMetaData(_SqlMetaDataSet metadata) { 
            resultSet = new Result(metadata);
            _results.Add(resultSet); 
 
            indexmap = new int[resultSet.MetaData.Length];
            for(int i = 0; i < indexmap.Length; i++) { 
                indexmap[i] = i;
            }
        }
 
        // callback function for the tdsparser
        // this will create an indexmap for the active resultset 
        // 
        internal int[] CreateIndexMap() {
            return indexmap; 
        }

        // callback function for the tdsparser
        // this will return an array of rows to store the rowdata 
        //
        internal object[] CreateRowBuffer() { 
            Row row = new Row(resultSet.MetaData.Length); 
            resultSet.AddRow(row);
            return row.DataFields; 
        }
    }

    // ------------------------------------------------------------------------------------------------- 
    //
    // 
#if WINFSInternalOnly 
    internal
#else 
    public
#endif
 sealed class SqlBulkCopy : IDisposable {
        private enum TableNameComponents { 
            Server = 0,
            Catalog, 
            Owner, 
            TableName,
        } 
        private enum ValueSourceType {
            Unspecified = 0,
            IDataReader,
            DataTable, 
            RowArray
        } 
 
        // The initial query will return three tables.
        // Transaction count has only one value in one column and one row 
        // MetaData has n columns but no rows
        // Collation has 4 columns and n rows

        private const int TranCountResultId = 0; 
        private const int TranCountRowId = 0;
        private const int TranCountValueId = 0; 
 
        private const int MetaDataResultId = 1;
 
        private const int CollationResultId = 2;
        private const int ColIdId = 0;
        private const int NameId = 1;
        private const int Tds_CollationId = 2; 
        private const int CollationId = 3;
 
        private const int DefaultCommandTimeout = 30; 

        private int _batchSize; 
        private bool _ownConnection;
        private SqlBulkCopyOptions _copyOptions;
        private int _timeout = DefaultCommandTimeout;
        private string _destinationTableName; 
        private int _rowsCopied;
        private int _notifyAfter; 
        private int _rowsUntilNotification; 
        private bool _insideRowsCopiedEvent;
 
        private object _rowSource;
        private SqlDataReader _SqlDataReaderRowSource;

        private SqlBulkCopyColumnMappingCollection _columnMappings; 
        private SqlBulkCopyColumnMappingCollection _localColumnMappings;
 
        private SqlConnection _connection; 
        private SqlTransaction _internalTransaction;
        private SqlTransaction _externalTransaction; 

        private ValueSourceType _rowSourceType = ValueSourceType.Unspecified;
        private DataRow _currentRow;
        private int _currentRowLength; 
        private DataRowState _rowState;
        private IEnumerator _rowEnumerator; 
 
        private TdsParser _parser;
        private TdsParserStateObject _stateObj; 
        private ArrayList _sortedColumnMappings;

        private SqlRowsCopiedEventHandler _rowsCopiedEventHandler;
 
        private static int _objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref _objectTypeCount); 
 
        // ctor
        // 
        public SqlBulkCopy(SqlConnection connection) {
            if(connection == null) {
                throw ADP.ArgumentNull("connection");
            } 
            _connection = connection;
            _columnMappings = new SqlBulkCopyColumnMappingCollection(); 
        } 

        public SqlBulkCopy(SqlConnection connection, SqlBulkCopyOptions copyOptions, SqlTransaction externalTransaction) 
            : this (connection) {

            _copyOptions = copyOptions;
            if(externalTransaction != null && IsCopyOption(SqlBulkCopyOptions.UseInternalTransaction)) { 
                throw SQL.BulkLoadConflictingTransactionOption();
            } 
 
            if(!IsCopyOption(SqlBulkCopyOptions.UseInternalTransaction)) {
                _externalTransaction = externalTransaction; 
            }
        }

        public SqlBulkCopy(string connectionString) : this (new SqlConnection(connectionString)) { 
            if(connectionString == null) {
                throw ADP.ArgumentNull("connectionString"); 
            } 
            _connection = new SqlConnection(connectionString);
            _columnMappings = new SqlBulkCopyColumnMappingCollection(); 
            _ownConnection = true;
        }

        public SqlBulkCopy(string connectionString, SqlBulkCopyOptions copyOptions) 
            : this (connectionString) {
            _copyOptions = copyOptions; 
        } 

        public int BatchSize { 
            get {
                return _batchSize;
            }
            set { 
                if(value >= 0) {
                    _batchSize = value; 
                } 
                else {
                    throw ADP.ArgumentOutOfRange("BatchSize"); 
                }
            }
        }
 
        public int BulkCopyTimeout {
            get { 
                return _timeout; 
            }
            set { 
                if(value < 0) {
                    throw SQL.BulkLoadInvalidTimeout(value);
                }
                _timeout = value; 
            }
        } 
 
        public SqlBulkCopyColumnMappingCollection ColumnMappings {
            get { 
                return _columnMappings;
            }
        }
 
        public string DestinationTableName {
            get { 
                return _destinationTableName; 
            }
            set { 
                if(value == null) {
                    throw ADP.ArgumentNull("DestinationTableName");
                }
                else if(value.Length == 0) { 
                    throw ADP.ArgumentOutOfRange("DestinationTableName");
                } 
                _destinationTableName = value; 
            }
        } 

        public int NotifyAfter {
            get {
                return _notifyAfter; 
            }
            set { 
                if(value >= 0) { 
                    _notifyAfter = value;
                } 
                else {
                    throw ADP.ArgumentOutOfRange("NotifyAfter");
                }
            } 
        }
 
        internal int ObjectID { 
            get {
                return _objectID; 
            }
        }

        public event SqlRowsCopiedEventHandler SqlRowsCopied { 
            add {
                _rowsCopiedEventHandler += value; 
            } 
            remove {
                _rowsCopiedEventHandler -= value; 
            }

        }
 
        internal SqlStatistics Statistics {
            get { 
                if(null != _connection) { 
                    if(_connection.StatisticsEnabled) {
                        return _connection.Statistics; 
                    }
                }
                return null;
            } 
        }
 
        //================================================================ 
        // IDisposable
        //=============================================================== 
        void IDisposable.Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
 
        }
 
        private bool IsCopyOption(SqlBulkCopyOptions copyOption) { 
            return (_copyOptions & copyOption) == copyOption;
        } 

        // Create and execute initial query to get information about the targettable
        //
        // devnote: most of the stuff here will be moved to the TDSParser's Run method 
        //
        private BulkCopySimpleResultSet CreateAndExecuteInitialQuery() { 
            string[] parts; 
            try {
                parts = MultipartIdentifier.ParseMultipartIdentifier (this.DestinationTableName, "[\"", "]\"", Res.SQL_BulkCopyDestinationTableName, true); 
            } catch (Exception e) {
                throw SQL.BulkLoadInvalidDestinationTable(this.DestinationTableName, e);
            }
            if (ADP.IsEmpty (parts[MultipartIdentifier.TableIndex])) { 
                throw SQL.BulkLoadInvalidDestinationTable(this.DestinationTableName, null);
            } 
            string TDSCommand; 
            BulkCopySimpleResultSet internalResults = new BulkCopySimpleResultSet();
 
            TDSCommand = "select @@trancount; SET FMTONLY ON select * from " + this.DestinationTableName + " SET FMTONLY OFF ";
            if(_connection.IsShiloh) {
                // If its a temp DB then try to connect
 
                string TableCollationsStoredProc;
                if (_connection.IsKatmaiOrNewer) { 
                    TableCollationsStoredProc = "sp_tablecollations_100"; 
                }
                else if (_connection.IsYukonOrNewer) { 
                    TableCollationsStoredProc = "sp_tablecollations_90";
                }
                else {
                    TableCollationsStoredProc = "sp_tablecollations"; 
                }
 
                string TableName = parts[MultipartIdentifier.TableIndex].Replace("'", "''"); 
                string SchemaName = parts[MultipartIdentifier.SchemaIndex];
                if (SchemaName != null) { 
                    SchemaName = SchemaName.Replace("'", "''");
                }
                string CatalogName = parts[MultipartIdentifier.CatalogIndex];
                if (TableName.Length > 0 && '#' == TableName[0] && ADP.IsEmpty (CatalogName)) { 
                    TDSCommand += String.Format((IFormatProvider)null, "exec tempdb..{0} N'{1}.{2}'",
                        TableCollationsStoredProc, 
                        SchemaName, 
                        TableName
                    ); 
                } else {
                    TDSCommand += String.Format((IFormatProvider)null, "exec {0}..{1} N'{2}.{3}'",
                        CatalogName,
                        TableCollationsStoredProc, 
                        SchemaName,
                        TableName 
                    ); 
                }
            } 

            Bid.Trace("<sc.SqlBulkCopy.CreateAndExecuteInitialQuery|INFO> Initial Query: '%ls' \n", TDSCommand);

            _parser.TdsExecuteSQLBatch(TDSCommand, this.BulkCopyTimeout, null, _stateObj); 
            _parser.Run(RunBehavior.UntilDone, null, null, internalResults, _stateObj);
            return internalResults; 
        } 

        // Matches associated columns with metadata from initial query 
        // builds and executes the update bulk command
        //
        private string AnalyzeTargetAndCreateUpdateBulkCommand(BulkCopySimpleResultSet internalResults) {
            _sortedColumnMappings = new ArrayList(); 
            StringBuilder updateBulkCommandText = new StringBuilder();
 
            if (_connection.IsShiloh && 0 == internalResults[CollationResultId].Count) { 
                throw SQL.BulkLoadNoCollation();
            } 

            Debug.Assert((internalResults != null), "Where are the results from the initial query?");

            updateBulkCommandText.Append("insert bulk " + this.DestinationTableName + " ("); 
            int nmatched = 0;               // number of columns that match and are accepted
            int nrejected = 0;              // number of columns that match but were rejected 
            bool rejectColumn;            // true if a column is rejected because of an excluded type 

            bool isInTransaction; 

            if(_parser.IsYukonOrNewer) {
                isInTransaction = _connection.HasLocalTransaction;
            } 
            else {
                isInTransaction = (bool)(0 < (SqlInt32)(internalResults[TranCountResultId][TranCountRowId][TranCountValueId])); 
            } 
            // Throw if there is a transaction but no flag is set
            if(isInTransaction && null == _externalTransaction && null == _internalTransaction && (_connection.Parser != null && _connection.Parser.CurrentTransaction != null && _connection.Parser.CurrentTransaction.IsLocal)) { 
                throw SQL.BulkLoadExistingTransaction();
            }

            // loop over the metadata for each column 
            //
            for(int i = 0; i < internalResults[MetaDataResultId].MetaData.Length; i++) { 
                _SqlMetaData metadata = internalResults[MetaDataResultId].MetaData[i]; 
                rejectColumn = false;
 
                // Check for excluded types
                //
                if((metadata.type == SqlDbType.Timestamp)
                    || ((metadata.isIdentity) && !IsCopyOption(SqlBulkCopyOptions.KeepIdentity))) { 
                    // remove metadata for excluded columns
                    internalResults[MetaDataResultId].MetaData[i] = null; 
                    rejectColumn = true; 
                    // we still need to find a matching column association
                } 

                // find out if this column is associated
                int assocId;
                for(assocId = 0; assocId < _localColumnMappings.Count; assocId++) { 
                    if((_localColumnMappings[assocId]._destinationColumnOrdinal == metadata.ordinal) ||
                        (UnquotedName(_localColumnMappings[assocId]._destinationColumnName) == metadata.column)) { 
                        if(rejectColumn) { 
                            nrejected++;       // count matched columns only
                            break; 
                        }

                        _sortedColumnMappings.Add(new _ColumnMapping(_localColumnMappings[assocId]._internalSourceColumnOrdinal, metadata));
                        nmatched++; 

                        if(nmatched > 1) { 
                            updateBulkCommandText.Append(", ");         // a leading comma for all but the first one 
                        }
 
                        // some datatypes need special handling ...
                        //
                        if(metadata.type == SqlDbType.Variant) {
                            AppendColumnNameAndTypeName(updateBulkCommandText, metadata.column, "sql_variant"); 
                        }
                        else if(metadata.type == SqlDbType.Udt) { 
                            // UDTs are sent as varbinary 
                            AppendColumnNameAndTypeName(updateBulkCommandText, metadata.column, "varbinary");
                        } 
                        else {
                            AppendColumnNameAndTypeName(updateBulkCommandText, metadata.column, metadata.type.ToString());
                        }
 
                        switch(metadata.metaType.NullableType) {
                            case TdsEnums.SQLNUMERICN: 
                            case TdsEnums.SQLDECIMALN: 
                                // decimal and numeric need to include precision and scale
                                // 
                                updateBulkCommandText.Append("(" + metadata.precision.ToString((IFormatProvider)null) + "," + metadata.scale.ToString((IFormatProvider)null) + ")");
                                break;
                            case TdsEnums.SQLUDT: {
                                    if (metadata.IsLargeUdt) { 
                                        updateBulkCommandText.Append("(max)");
                                    } else { 
                                        int size = metadata.length; 
                                        updateBulkCommandText.Append("(" + size.ToString((IFormatProvider)null) + ")");
                                    } 
                                    break;
                                }
                            case TdsEnums.SQLTIME:
                            case TdsEnums.SQLDATETIME2: 
                            case TdsEnums.SQLDATETIMEOFFSET:
                                // date, dateime2, and datetimeoffset need to include scale 
                                // 
                                updateBulkCommandText.Append("(" + metadata.scale.ToString((IFormatProvider)null) + ")");
                                break; 
                            default: {
                                    // for non-long non-fixed types we need to add the Size
                                    //
                                    if(!metadata.metaType.IsFixed && !metadata.metaType.IsLong) { 
                                        int size = metadata.length;
                                        switch(metadata.metaType.NullableType) { 
                                            case TdsEnums.SQLNCHAR: 
                                            case TdsEnums.SQLNVARCHAR:
                                            case TdsEnums.SQLNTEXT: 
                                                size /= 2;
                                                break;
                                            default:
                                                break; 
                                        }
                                        updateBulkCommandText.Append("(" + size.ToString((IFormatProvider)null) + ")"); 
                                    } 
                                    else if(metadata.metaType.IsPlp && metadata.metaType.SqlDbType != SqlDbType.Xml) {
                                        // Partial length column prefix (max) 
                                        updateBulkCommandText.Append("(max)");
                                    }
                                    break;
                                } 
                        }
 
                        if(_connection.IsShiloh) { 
                            // Shiloh or above!
                            // get collation for column i 

                            Result rowset = internalResults[CollationResultId];
                                object rowvalue = rowset[i][CollationId];
                                if(rowvalue != null) { 
                                    Debug.Assert(rowvalue is SqlString);
                                    SqlString collation_name = (SqlString)rowvalue; 
                                    if(!collation_name.IsNull) { 
                                        updateBulkCommandText.Append(" COLLATE " + collation_name.ToString());
                                        if(null != _SqlDataReaderRowSource) { 
                                            // On SqlDataReader we can verify the sourcecolumn collation!
                                            int sourceColumnId = _localColumnMappings[assocId]._internalSourceColumnOrdinal;
                                            int destinationLcid = internalResults[MetaDataResultId].MetaData[i].collation.LCID;
                                            int sourceLcid = _SqlDataReaderRowSource.GetLocaleId(sourceColumnId); 
                                            if(sourceLcid != destinationLcid) {
                                                throw SQL.BulkLoadLcidMismatch(sourceLcid, _SqlDataReaderRowSource.GetName(sourceColumnId), destinationLcid, metadata.column); 
                                            } 
                                        }
                                    } 
                                }
                        }
                        break;
                    } // end if found 
                } // end of (inner) for loop
                if(assocId == _localColumnMappings.Count) { 
                    // remove metadata for unmatched columns 
                    internalResults[MetaDataResultId].MetaData[i] = null;
                } 
            } // end of (outer) for loop

            // all columnmappings should have matched up
            if(nmatched + nrejected != _localColumnMappings.Count) { 
                throw (SQL.BulkLoadNonMatchingColumnMapping());
            } 
 
            updateBulkCommandText.Append(")");
 
            if((_copyOptions & (
                    SqlBulkCopyOptions.KeepNulls
                    | SqlBulkCopyOptions.TableLock
                    | SqlBulkCopyOptions.CheckConstraints 
                    | SqlBulkCopyOptions.FireTriggers)) != SqlBulkCopyOptions.Default) {
                bool addSeparator = false;  // insert a comma character if multiple options in list ... 
                updateBulkCommandText.Append(" with ("); 
                if(IsCopyOption(SqlBulkCopyOptions.KeepNulls)) {
                    updateBulkCommandText.Append("KEEP_NULLS"); 
                    addSeparator = true;
                }
                if(IsCopyOption(SqlBulkCopyOptions.TableLock)) {
                    updateBulkCommandText.Append((addSeparator ? ", " : "") + "TABLOCK"); 
                    addSeparator = true;
                } 
                if(IsCopyOption(SqlBulkCopyOptions.CheckConstraints)) { 
                    updateBulkCommandText.Append((addSeparator ? ", " : "") + "CHECK_CONSTRAINTS");
                    addSeparator = true; 
                }
                if(IsCopyOption(SqlBulkCopyOptions.FireTriggers)) {
                    updateBulkCommandText.Append((addSeparator ? ", " : "") + "FIRE_TRIGGERS");
                    addSeparator = true; 
                }
                updateBulkCommandText.Append(")"); 
            } 
            return (updateBulkCommandText.ToString());
        } 

        // submitts the updatebulk command
        //
        private void SubmitUpdateBulkCommand(BulkCopySimpleResultSet internalResults, string TDSCommand) { 
            _parser.TdsExecuteSQLBatch(TDSCommand, this.BulkCopyTimeout, null, _stateObj);
            _parser.Run(RunBehavior.UntilDone, null, null, null, _stateObj); 
        } 

        // Starts writing the Bulkcopy data stream 
        //
        private void WriteMetaData(BulkCopySimpleResultSet internalResults) {
            _stateObj.SetTimeoutSeconds(this.BulkCopyTimeout);
 
            _SqlMetaDataSet metadataCollection = internalResults[MetaDataResultId].MetaData;
            _stateObj._outputMessageType = TdsEnums.MT_BULK; 
            _parser.WriteBulkCopyMetaData(metadataCollection, _sortedColumnMappings.Count, _stateObj); 
        }
 
        //================================================================
        // Close()
        //
        // Terminates the bulk copy operation. 
        // Must be called at the end of the bulk copy session.
        //================================================================ 
        public void Close() { 
            if(_insideRowsCopiedEvent) {
                throw SQL.InvalidOperationInsideEvent(); 
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        } 

        private void Dispose(bool disposing) { 
            if(disposing) { 
                // dispose dependend objects
                _columnMappings = null; 
                _parser = null;
                try {

                    // This code should be removed in RTM, its not needed 
                    // The _internalTransaction should just be a internal variable.
                    // Start Cut 
                    try { 
                        // cleanup managed objects
                        if(_internalTransaction != null) { 
                            // do not commit on dispose but rollback
                            _internalTransaction.Rollback();
                            _internalTransaction.Dispose ();
                            _internalTransaction = null; 
                        }
                    } 
                    catch(Exception e) { 
                        //
                        if(!ADP.IsCatchableExceptionType(e)) { 
                            throw;
                        }
                        ADP.TraceExceptionWithoutRethrow(e);
                    } 
                    // End Cut
                } 
                finally { 
                    if(_connection != null) {
                        if(_ownConnection) { 
                            _connection.Dispose();
                        }
                        _connection = null;
                    } 
                }
            } 
            // free unmanaged objects 
        }
 
        // unified method to read a value from the current row
        //
        private object GetValueFromSourceRow(int columnOrdinal, _SqlMetaData metadata, int[] UseSqlValue, int destRowIndex) {
 
            if (UseSqlValue[destRowIndex] == 0) {  // If we haven't determined if the source and dest should marshal via sqlvalue
                UseSqlValue[destRowIndex] = -1; // Default state, they should marshal via get value 
 
                // Special case code..
                // We can't just call GetValue for SqlDecimals, the SqlDecmail is a superset of of the CLR.Decmimal values 
                // Check if we are indeed going from a SqlDecimal to a SqlDecimal, if we are use the true SqlDecimal value to avoid overflows

                if (metadata.metaType.NullableType == TdsEnums.SQLDECIMALN || metadata.metaType.NullableType == TdsEnums.SQLNUMERICN) {
                    Type t = null; 
                    switch(_rowSourceType) {	
                        case ValueSourceType.IDataReader: 
                            if (null != _SqlDataReaderRowSource) { 
                                t = _SqlDataReaderRowSource.GetFieldType(columnOrdinal);
                            } 
                         break;

                        case ValueSourceType.DataTable:
                        case ValueSourceType.RowArray: 
                           Debug.Assert(_currentRow != null, "uninitialized _currentRow");
                           Debug.Assert(columnOrdinal < _currentRowLength, "inconsistency of length of rows from rowsource!"); 
                           t = _currentRow.Table.Columns[columnOrdinal].DataType; 
                        break;
                    } 

                    if (typeof(SqlDecimal) == t || typeof(Decimal) == t) {
                        UseSqlValue[destRowIndex] = (int)SqlBuffer.StorageType.Decimal;  // Source Type Decimal
                    } 
                    else if (typeof(SqlDouble) == t || typeof (double) == t) {
                        UseSqlValue[destRowIndex] = (int)SqlBuffer.StorageType.Double;  // Source Type SqlDouble 
                    } 
                    else if (typeof(SqlSingle) == t || typeof (float) == t) {
                        UseSqlValue[destRowIndex] = (int)SqlBuffer.StorageType.Single;  // Source Type SqlSingle 
                    }
                }
            }
 
            switch(_rowSourceType) {
                case ValueSourceType.IDataReader: 
                    if (null != _SqlDataReaderRowSource) { 
                        switch (UseSqlValue[destRowIndex]) {
                            case (int)SqlBuffer.StorageType.Decimal : { 
                                return _SqlDataReaderRowSource.GetSqlDecimal(columnOrdinal);
                            }
                            case (int)SqlBuffer.StorageType.Double : {
                                return new SqlDecimal (_SqlDataReaderRowSource.GetSqlDouble (columnOrdinal).Value); 
                            }
                            case (int)SqlBuffer.StorageType.Single : { 
                                return new SqlDecimal (_SqlDataReaderRowSource.GetSqlSingle(columnOrdinal).Value); 
                            }
                            default: 
                                return _SqlDataReaderRowSource.GetValue(columnOrdinal);
                        }
                    }
                    else { 
                        return ((IDataReader)_rowSource).GetValue(columnOrdinal);
                    } 
 
                case ValueSourceType.DataTable:
                case ValueSourceType.RowArray: 
                {
                    Debug.Assert(_currentRow != null, "uninitialized _currentRow");
                    Debug.Assert(columnOrdinal < _currentRowLength, "inconsistency of length of rows from rowsource!");
                    object currentRowValue = _currentRow[columnOrdinal]; 
                    if (null != currentRowValue && DBNull.Value != currentRowValue) {
                            if ((int)SqlBuffer.StorageType.Single == UseSqlValue[destRowIndex] || 
                                (int)SqlBuffer.StorageType.Double == UseSqlValue[destRowIndex] || 
                                (int)SqlBuffer.StorageType.Decimal == UseSqlValue[destRowIndex]) {								
                                INullable inullable = currentRowValue as INullable; 
                                if (null == inullable || !inullable.IsNull) { // If the value is are a CLR Type, or are not null
                                    switch ((SqlBuffer.StorageType)UseSqlValue[destRowIndex]) {
                                        case SqlBuffer.StorageType.Single : {
                                            if (null != inullable) { 
                                                return  new SqlDecimal (((SqlSingle)currentRowValue).Value);
                                            } 
                                            else { 
                                                float f = (float)currentRowValue;
                                                if (!float.IsNaN (f)) { 
                                                   return new SqlDecimal (f);
                                                }
                                                break;
                                            } 
                                        }
                                        case SqlBuffer.StorageType.Double : { 
                                            if (null != inullable) { 
                                                return new SqlDecimal (((SqlDouble)currentRowValue).Value);
                                            } 
                                            else {
                                                double d = (double)currentRowValue;
                                                if (!double.IsNaN (d)) {
                                                    return  new SqlDecimal (d); 
                                                }
                                                break; 
                                            } 
                                        }
                                        case SqlBuffer.StorageType.Decimal : { 
                                            if (null != inullable) {
                                                return (SqlDecimal)currentRowValue;
                                            } else {
                                                return   new SqlDecimal ((Decimal)currentRowValue); 
                                            }
                                        } 
                                    } 
                                }
                            } 
                    }
                    return currentRowValue;
                }
 
                default:
                    Debug.Assert(false, "ValueSourcType unspecified"); 
                    throw ADP.NotSupported(); 
            }
        } 

        // unified method to read a row from the current rowsource
        //
        private bool ReadFromRowSource() { 
            switch(_rowSourceType) {
                case ValueSourceType.IDataReader: 
                    return ((IDataReader)_rowSource).Read(); 
                case ValueSourceType.DataTable:
                    // repeat until we get a row that is not deleted or there are no more rows ... 
                    do {
                        if(!_rowEnumerator.MoveNext()) {
                            return false;
                        } 
                        _currentRow = (DataRow)_rowEnumerator.Current;
                        _currentRowLength = _currentRow.ItemArray.Length; 
                    } 
                    while(((_currentRow.RowState & DataRowState.Deleted) != 0)           // repeat on delete row - always
                        || ((_rowState != 0) && ((_currentRow.RowState & _rowState) == 0)));       // repeat if there is an unexpected rowstate 

                    return true;
                case ValueSourceType.RowArray:
                    Debug.Assert(_rowEnumerator != null, "uninitialized _rowEnumerator"); 
                    if(_rowEnumerator.MoveNext()) {
                        _currentRow = (DataRow)_rowEnumerator.Current; 
                        _currentRowLength = _currentRow.ItemArray.Length; 
                        return true;
                    } 
                    return false;
                default:
                    Debug.Assert(false, "ValueSourcType unspecified");
                    throw ADP.NotSupported(); 
            }
        } 
 
        //
        // 
        private void CreateOrValidateConnection(string method) {
            if(null == _connection) {
                throw ADP.ConnectionRequired(method);
            } 
            if (_connection.IsContextConnection) {
                throw SQL.NotAvailableOnContextConnection(); 
            } 

            if(_ownConnection && _connection.State != ConnectionState.Open) { 
                _connection.Open();
            }

            // close any non MARS dead readers, if applicable, and then throw if still busy. 
            _connection.ValidateConnectionForExecute(method, null);
 
            // if we have a transaction, check to ensure that the active 
            // connection property matches the connection associated with
            // the transaction 
            if(null != _externalTransaction && _connection != _externalTransaction.Connection) {
                throw ADP.TransactionConnectionMismatch();
            }
        } 

        // Appends columnname in square brackets, a space and the typename to the query 
        // putting the name in quotes also requires doubling existing ']' so that they are not mistaken for 
        // the closing quote
        // example: abc will become [abc] but abc[] will becom [abc[]]] 
        //
        private void AppendColumnNameAndTypeName(StringBuilder query, string columnName, string typeName) {
            query.Append('[');
            query.Append(columnName.Replace("]", "]]")); 
            query.Append("] ");
            query.Append(typeName); 
        } 

        private string UnquotedName(string name) { 
            if(ADP.IsEmpty(name)) return null;
            if(name[0] == '[') {
                int l = name.Length;
                Debug.Assert(name[l - 1] == ']', "Name starts with [ but doesn not end with ]"); 
                name = name.Substring(1, l - 2);
            } 
            return name; 
        }
 
        private object ValidateBulkCopyVariant(object value) {
            // from the spec:
            // "The only acceptable types are ..."
            // GUID, BIGVARBINARY, BIGBINARY, BIGVARCHAR, BIGCHAR, NVARCHAR, NCHAR, BIT, INT1, INT2, INT4, INT8, 
            // MONEY4, MONEY, DECIMALN, NUMERICN, FTL4, FLT8, DATETIME4 and DATETIME
            // 
            MetaType metatype = MetaType.GetMetaTypeFromValue(value); 
            switch(metatype.TDSType) {
                case TdsEnums.SQLFLT4: 
                case TdsEnums.SQLFLT8:
                case TdsEnums.SQLINT8:
                case TdsEnums.SQLINT4:
                case TdsEnums.SQLINT2: 
                case TdsEnums.SQLINT1:
                case TdsEnums.SQLBIT: 
                case TdsEnums.SQLBIGVARBINARY: 
                case TdsEnums.SQLBIGVARCHAR:
                case TdsEnums.SQLUNIQUEID: 
                case TdsEnums.SQLNVARCHAR:
                case TdsEnums.SQLDATETIME:
                case TdsEnums.SQLMONEY:
                case TdsEnums.SQLNUMERICN: 
                case TdsEnums.SQLDATE:
                case TdsEnums.SQLTIME: 
                case TdsEnums.SQLDATETIME2: 
                case TdsEnums.SQLDATETIMEOFFSET:
                    if (value is INullable) {   // Current limitation in the SqlBulkCopy Variant code limits BulkCopy to CLR/COM Types. 
                        return MetaType.GetComValueFromSqlVariant (value);
                    } else {
                        return value;
                    } 
                default:
                    throw SQL.BulkLoadInvalidVariantValue(); 
            } 
        }
 
        private object ConvertValue(object value, _SqlMetaData metadata) {
            if(ADP.IsNull(value)) {
                if(!metadata.isNullable) {
                    throw SQL.BulkLoadBulkLoadNotAllowDBNull(metadata.column); 
                }
                return value; 
            } 
            MetaType type = metadata.metaType;
            try { 
                MetaType mt;
                switch(type.NullableType) {
                    case TdsEnums.SQLNUMERICN:
                    case TdsEnums.SQLDECIMALN: { 
                        mt = MetaType.GetMetaTypeFromSqlDbType (type.SqlDbType, false);
                        value = SqlParameter.CoerceValue (value, mt); 
 
                        // Convert Source Decimal Percision and Scale to Destination Percision and Scale
                        // Fix Bug: 385971 sql decimal data could get corrupted on insert if the scale of 
                        // the source and destination weren't the same.  The BCP protocal, specifies the
                        // scale of the incoming data in the insert statement, we just tell the server we
                        // are inserting the same scale back. This then created a bug inside the BCP opperation
                        // if the scales didn't match.  The fix is to do the same thing that SQL Paramater does, 
                        // and adjust the scale before writing.  In Orcas is scale adjustment should be removed from
                        // SqlParamater and SqlBulkCopy and Isoloated inside SqlParamater.CoerceValue, but becouse of 
                        // where we are in the cycle, the changes must be kept at minimum, so I'm just bringing the 
                        // code over to SqlBulkCopy.
 
                        SqlDecimal sqlValue;
                        if (value is SqlDecimal) {
                            sqlValue = (SqlDecimal)value;
                        } 
                        else {
                            sqlValue = new SqlDecimal((Decimal)value); 
                        } 

                        if (sqlValue.Scale  != metadata.scale) { 
                           sqlValue = TdsParser.AdjustSqlDecimalScale(sqlValue, metadata.scale);
                           value = sqlValue;
                        }
 
                        if (sqlValue.Precision > metadata.precision) {
                            throw SQL.BulkLoadCannotConvertValue (value.GetType (), mt ,ADP.ParameterValueOutOfRange(sqlValue)); 
                        } 
                    }
                    break; 

                    case TdsEnums.SQLINTN:
                    case TdsEnums.SQLFLTN:
                    case TdsEnums.SQLFLT4: 
                    case TdsEnums.SQLFLT8:
                    case TdsEnums.SQLMONEYN: 
                    case TdsEnums.SQLDATETIM4: 
                    case TdsEnums.SQLDATETIME:
                    case TdsEnums.SQLDATETIMN: 
                    case TdsEnums.SQLBIT:
                    case TdsEnums.SQLBITN:
                    case TdsEnums.SQLUNIQUEID:
                    case TdsEnums.SQLBIGBINARY: 
                    case TdsEnums.SQLBIGVARBINARY:
                    case TdsEnums.SQLIMAGE: 
                    case TdsEnums.SQLBIGCHAR: 
                    case TdsEnums.SQLBIGVARCHAR:
                    case TdsEnums.SQLTEXT: 
                    case TdsEnums.SQLDATE:
                    case TdsEnums.SQLTIME:
                    case TdsEnums.SQLDATETIME2:
                    case TdsEnums.SQLDATETIMEOFFSET: 
                        mt = MetaType.GetMetaTypeFromSqlDbType (type.SqlDbType, false);
                        value = SqlParameter.CoerceValue (value, mt); 
                    break; 
                    case TdsEnums.SQLNCHAR:
                    case TdsEnums.SQLNVARCHAR: 
                    case TdsEnums.SQLNTEXT:
                        mt = MetaType.GetMetaTypeFromSqlDbType (type.SqlDbType, false);
                        value = SqlParameter.CoerceValue (value, mt);
                        int len = (value is string) ? ((string)value).Length : ((SqlString)value).Value.Length; 
                        if (len  > metadata.length / 2) {
                            throw SQL.BulkLoadStringTooLong(); 
                        } 
                        break;
 
                    case TdsEnums.SQLVARIANT:
                        value = ValidateBulkCopyVariant(value);
                        break;
 
                    case TdsEnums.SQLUDT:
                        // UDTs are sent as varbinary so we need to get the raw bytes 
                        // unlike other types the parser does not like SQLUDT in form of SqlType 
                        // so we cast to a CLR type.
 
                        // Hack for type system version knob - only call GetBytes if the value is not already
                        // in byte[] form.
                        if (value.GetType() != typeof(byte[])) {
                            value = _connection.GetBytes(value); 
                        }
                        break; 
 
                    case TdsEnums.SQLXMLTYPE:
                        // Could be either string, SqlCachedBuffer or XmlReader 
                        Debug.Assert((value is XmlReader) || (value is SqlCachedBuffer) || (value is string) || (value is SqlString), "Invalid value type of Xml datatype");
                        if(value is XmlReader) {
                            value = MetaType.GetStringFromXml((XmlReader)value);
                        } 
                        break;
 
                    default: 
                        Debug.Assert(false, "Unknown TdsType!" + type.NullableType.ToString("x2", (IFormatProvider)null));
                        throw SQL.BulkLoadCannotConvertValue(value.GetType(), metadata.metaType, null); 
                }
                return value;
            }
            catch(Exception e) { 
                if(!ADP.IsCatchableExceptionType(e)) {
                    throw; 
                } 
                throw SQL.BulkLoadCannotConvertValue(value.GetType(), metadata.metaType, e);
            } 
        }

        public void WriteToServer(IDataReader reader) {
            SqlConnection.ExecutePermission.Demand(); 

            SqlStatistics statistics = Statistics; 
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                if(reader == null) { 
                    throw new ArgumentNullException("reader");
                }
                _rowSource = reader;
                _SqlDataReaderRowSource = _rowSource as SqlDataReader; 
                _rowSourceType = ValueSourceType.IDataReader;
                WriteRowSourceToServer(reader.FieldCount); 
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
            }
        }

        public void WriteToServer(DataTable table) { 
            WriteToServer(table, 0);
        } 
 
        public void WriteToServer(DataTable table, DataRowState rowState) {
            SqlConnection.ExecutePermission.Demand(); 

            SqlStatistics statistics = Statistics;
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                if(table == null) {
                    throw new ArgumentNullException("table"); 
                } 
                _rowState = rowState & ~DataRowState.Deleted;
                _rowSource = table; 
                _SqlDataReaderRowSource = null;
                _rowSourceType = ValueSourceType.DataTable;
                _rowEnumerator = table.Rows.GetEnumerator();
                WriteRowSourceToServer(table.Columns.Count); 
            }
            finally { 
                SqlStatistics.StopTimer(statistics); 
            }
        } 

        public void WriteToServer(DataRow[] rows) {
            SqlConnection.ExecutePermission.Demand();
 
            SqlStatistics statistics = Statistics;
            try { 
                statistics = SqlStatistics.StartTimer(Statistics); 
                if(rows == null) {
                    throw new ArgumentNullException("rows"); 
                }
                if(rows.Length == 0) {
                    return; // nothing to do. user passed us an empty array
                } 
                DataTable table = rows[0].Table;
                Debug.Assert(null != table, "How can we have rows without a table?"); 
                _rowSource = rows; 
                _SqlDataReaderRowSource = null;
                _rowSourceType = ValueSourceType.RowArray; 
                _rowEnumerator = rows.GetEnumerator();
                WriteRowSourceToServer(table.Columns.Count);
            }
            finally { 
                SqlStatistics.StopTimer(statistics);
            } 
        } 

        private void WriteRowSourceToServer(int columnCount) { 
            CreateOrValidateConnection(SQL.WriteToServer);

            // Find out if we need to get the source column ordinal from the datatable
            // 
            bool unspecifiedColumnOrdinals = false;
 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    _columnMappings.ReadOnly = true; 
                    _localColumnMappings = _columnMappings;
                    if(_localColumnMappings.Count > 0) { 
                        _localColumnMappings.ValidateCollection();
                        foreach(SqlBulkCopyColumnMapping bulkCopyColumn in _localColumnMappings) {
                            if(bulkCopyColumn._internalSourceColumnOrdinal == -1) {
                                unspecifiedColumnOrdinals = true; 
                                break;
                            } 
                        } 
                    }
                    else { 
                        _localColumnMappings = new SqlBulkCopyColumnMappingCollection();
                        _localColumnMappings.CreateDefaultMapping(columnCount);
                    }
 
                    // perf: If the user specified all column ordinals we do not need to get a schematable
                    // 
                    if(unspecifiedColumnOrdinals) { 
                        int index = -1;
                        unspecifiedColumnOrdinals = false; 

                        // Match up sourceColumn names with sourceColumn ordinals
                        //
                        if(_localColumnMappings.Count > 0) { 
                            foreach(SqlBulkCopyColumnMapping bulkCopyColumn in _localColumnMappings) {
                                if(bulkCopyColumn._internalSourceColumnOrdinal == -1) { 
                                    string unquotedColumnName = UnquotedName(bulkCopyColumn.SourceColumn); 

                                    switch(this._rowSourceType) { 
                                        case ValueSourceType.DataTable:
                                            index = ((DataTable)_rowSource).Columns.IndexOf(unquotedColumnName);
                                            break;
                                        case ValueSourceType.RowArray: 
                                            index = ((DataRow[])_rowSource)[0].Table.Columns.IndexOf(unquotedColumnName);
                                            break; 
                                        case ValueSourceType.IDataReader: 
                                            try {
                                                index = ((IDataRecord)this._rowSource).GetOrdinal(unquotedColumnName); 
                                            }
                                            catch(IndexOutOfRangeException e) {
                                                throw (SQL.BulkLoadNonMatchingColumnName(unquotedColumnName, e));
                                            } 
                                            break;
                                    } 
                                    if(index == -1) { 
                                        throw (SQL.BulkLoadNonMatchingColumnName(unquotedColumnName));
                                    } 
                                    bulkCopyColumn._internalSourceColumnOrdinal = index;
                                }
                            }
                        } 
                    }
                    WriteToServerInternal(); 
#if DEBUG 
                }
                finally { 
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                }
#endif //DEBUG
            } 
            catch(System.OutOfMemoryException e) {
                _connection.Abort(e); 
                throw; 
            }
            catch(System.StackOverflowException e) { 
                _connection.Abort(e);
                throw;
            }
            catch(System.Threading.ThreadAbortException e) { 
                _connection.Abort(e);
                throw; 
            } 
            finally {
                _columnMappings.ReadOnly = false; 
            }
        }

        //=============================================================== 
        // WriteToServerInternal()
        // 
        // Writes the entire data to the server 
        //================================================================
        private void WriteToServerInternal() { 
            string updateBulkCommandText = null;
            int rowsInCurrentBatch;
            bool moreData = false;
            bool abortOperation = false; 
            int[] UseSqlValue = null;                           // Used to store the state if a sqlvalue should be returned when using the sqldatareader
            int localBatchSize = _batchSize;                    // changes will not be accepted while this method executes 
            bool batchCopyMode = false; 
            if (_batchSize > 0)
                batchCopyMode = true; 
            Exception exception = null;
            _rowsCopied = 0;

            // must have a destination table name 
            if(_destinationTableName == null) {
                throw SQL.BulkLoadMissingDestinationTable(); 
            } 
            // must have at least one row on the source
            if(!ReadFromRowSource()) { 
                return;
            }
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                bool processFinallyBlock = true;
                _parser = _connection.Parser; 
                _stateObj = _parser.GetSession(this); 
                _stateObj._bulkCopyOpperationInProgress = true;
                try { 
                    _stateObj.StartSession(ObjectID);

                    BulkCopySimpleResultSet internalResults;
                    try { 
                        internalResults = CreateAndExecuteInitialQuery();
                    } 
                    catch(SqlException e) { 
                        throw SQL.BulkLoadInvalidDestinationTable(_destinationTableName, e);
                    } 

                    _rowsUntilNotification = _notifyAfter;

                    updateBulkCommandText = AnalyzeTargetAndCreateUpdateBulkCommand(internalResults); 

                    if(_sortedColumnMappings.Count == 0) { 
                        // nothing to do ... 
                        return;
                    } 

                    // initiate the bulk insert and send the specified number of rows to the target
                    // eventually we need to repeat that until there is no more data on the source
                    // 
                    _stateObj.SniContext=SniContext.Snix_SendRows;
                    do { 
                        // if not already in an transaction initiate transaction 
                        if(IsCopyOption(SqlBulkCopyOptions.UseInternalTransaction)) {
                            _internalTransaction = _connection.BeginTransaction(); 
                        }
                        // from here everything should happen transacted
                        //
 
                        // At this step we start to write data to the stream.
                        // It might not have been written out to the server yet 
                        SubmitUpdateBulkCommand(internalResults, updateBulkCommandText); 

                        // starting here we  h a v e  to cancel out of the current operation 
                        //
                        try {
                            WriteMetaData(internalResults);
 
                            object[] rowValues = new object[_sortedColumnMappings.Count];
 
                            if (null == UseSqlValue) { 
                                UseSqlValue = new int[rowValues.Length]; // take advantage of the the CLR default initilization to all zero..
                            } 

                            object value;
                            rowsInCurrentBatch = localBatchSize;
                            do { 

                            // The do-loop sends an entire row to the server. 
                            // the loop repeats until the max number of rows in a batch is sent, 
                            // all rows have been sent or an exception happened
                            // 
                            for(int i = 0; i < rowValues.Length; i++) {
                                _ColumnMapping bulkCopyColumn = (_ColumnMapping)_sortedColumnMappings[i];
                                _SqlMetaData metadata = bulkCopyColumn._metadata;
                                value = GetValueFromSourceRow(bulkCopyColumn._sourceColumnOrdinal, metadata, UseSqlValue ,i); 
                                rowValues[i] =  ConvertValue(value, metadata);
                            } 
 
                                // Write the entire row
                                // This might cause packets to be written out to the server 
                                //
                                _parser.WriteByte(TdsEnums.SQLROW, _stateObj);
                                for(int i = 0; i < rowValues.Length; i++) {
                                    _ColumnMapping bulkCopyColumn = (_ColumnMapping)_sortedColumnMappings[i]; 
                                    _SqlMetaData metadata = bulkCopyColumn._metadata;
 
                                    if(metadata.type != SqlDbType.Variant) { 
                                        _parser.WriteBulkCopyValue(rowValues[i], metadata, _stateObj);
                                    } 
                                    else {
                                        _parser.WriteSqlVariantDataRowValue(rowValues[i], _stateObj);
                                    }
                                } 
                                _rowsCopied++;
 
                                // Fire event logic 
                                if(_notifyAfter > 0) {                      // no action if no value specified
                                    // (0=no notification) 
                                    if(_rowsUntilNotification > 0) {       // > 0?
                                        if(--_rowsUntilNotification == 0) {        // decrement counter
                                            // Fire event during operation. This is the users chance to abort the operation
                                            try { 
                                                // it's also the user's chance to cause an exception ...
                                                _stateObj.BcpLock=true; 
                                                abortOperation = FireRowsCopiedEvent(_rowsCopied); 
                                                Bid.Trace("<sc.SqlBulkCopy.WriteToServerInternal|INFO> \n");
 
                                                // just in case some pathological person closes the target connection ...
                                                if (ConnectionState.Open != _connection.State) {
                                                    break;
                                                } 
                                            }
                                            catch(Exception e) { 
                                                // 
                                                if(!ADP.IsCatchableExceptionType(e)) {
                                                    throw; 
                                                }
                                                exception = OperationAbortedException.Aborted(e);
                                                break;
                                            } 
                                            finally {
                                                _stateObj.BcpLock = false; 
                                            } 
                                            if(abortOperation) {
                                                break; 
                                            }
                                            _rowsUntilNotification = _notifyAfter;
                                        }
                                    } 
                                }
                                if(_rowsUntilNotification > _notifyAfter) {    // if the specified counter decreased we update 
                                    _rowsUntilNotification = _notifyAfter;      // decreased we update otherwise not 
                                }
 
                                // one more chance to cause an exception ...
                                moreData = ReadFromRowSource(); // proceed to the next row

                                if(batchCopyMode) { 
                                    rowsInCurrentBatch--;
                                    if(rowsInCurrentBatch == 0) { 
                                        break; 
                                    }
                                } 
                            } while(moreData);
                        }
                        catch(Exception e) {
                            if(ADP.IsCatchableExceptionType(e)) { 
                                _stateObj.CancelRequest();
                            } 
                            throw; 
                        }
 
                        if(ConnectionState.Open != _connection.State) {
                            throw ADP.OpenConnectionRequired(SQL.WriteToServer, _connection.State);
                        }
 
                        _parser.WriteBulkCopyDone(_stateObj);
                        _parser.Run(RunBehavior.UntilDone, null, null, null, _stateObj); 
 
                        if (abortOperation || null!=exception) {
                            throw OperationAbortedException.Aborted(exception); 
                        }

                        // commit transaction (batchcopymode only)
                        // 
                        if(null != _internalTransaction) {
                            _internalTransaction.Commit(); 
                            _internalTransaction = null; 
                        }
                    } while(moreData); 

                    _localColumnMappings = null;
                }
                catch(Exception e) { 
                    //
                    processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                    if(processFinallyBlock) { 
                        _stateObj._internalTimeout = false; // a timeout in ExecuteDone results in SendAttention/ProcessAttention. All we need to do is clear the flag
                        if(null != _internalTransaction) { 
                            if(!_internalTransaction.IsZombied) {
                                _internalTransaction.Rollback();
                            }
                            _internalTransaction = null; 
                        }
                    } 
                    throw; 
                }
                finally { 
                    Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to WriteToServerInternal");  // you need to setup for a thread abort somewhere before you call this method
                    if(processFinallyBlock && null != _stateObj) {
                        _stateObj.CloseSession();
#if DEBUG 
                        _stateObj.InvalidateDebugOnlyCopyOfSniContext();
#endif 
                    } 
                }
            } 
            finally {
                if(null != _stateObj) {
                    _stateObj._bulkCopyOpperationInProgress = false;
                    _stateObj = null; 
                }
            } 
        } 

        private void OnRowsCopied(SqlRowsCopiedEventArgs value) { 
            SqlRowsCopiedEventHandler handler = _rowsCopiedEventHandler;
            if(handler != null) {
                handler(this, value);
            } 
        }
        // fxcop: 
        // Use the .Net Event System whenever appropriate. 

        private bool FireRowsCopiedEvent(long rowsCopied) { 
            SqlRowsCopiedEventArgs eventArgs = new SqlRowsCopiedEventArgs(rowsCopied);
            try {
                _insideRowsCopiedEvent = true;
                this.OnRowsCopied(eventArgs); 
            }
            finally { 
                _insideRowsCopiedEvent = false; 
            }
            return eventArgs.Abort; 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
