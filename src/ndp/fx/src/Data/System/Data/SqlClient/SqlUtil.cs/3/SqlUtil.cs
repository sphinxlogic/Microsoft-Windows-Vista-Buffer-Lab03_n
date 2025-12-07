//------------------------------------------------------------------------------ 
// <copyright file="SqlUtil.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
    using System; 
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection; 
    using System.Runtime.Serialization.Formatters; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Security.Principal;
    using System.Threading;
    using System.Text;
    using SysTx = System.Transactions; 

    sealed internal class InOutOfProcHelper { 
        private static readonly InOutOfProcHelper SingletonInstance = new InOutOfProcHelper(); 

        private bool _inProc = false; 

        // InOutOfProcHelper detects whether it's running inside the server or not.  It does this
        //  by checking for the existence of a well-known function export on the current process.
        //  Note that calling conventions, etc. do not matter -- we'll never call the function, so 
        //  only the name match or lack thereof matter.
        private InOutOfProcHelper() { 
            // Don't need to close this handle... 
            IntPtr handle = SafeNativeMethods.GetModuleHandle(null);
            if (IntPtr.Zero != handle) { 
                //


                if (IntPtr.Zero != SafeNativeMethods.GetProcAddress(handle, "_______SQL______Process______Available@0")) { 
                    _inProc = true;
                } 
                else if (IntPtr.Zero != SafeNativeMethods.GetProcAddress(handle, "______SQL______Process______Available")) { 
                    _inProc = true;
                } 
            }
        }

        internal static bool InProc { 
            get {
                return SingletonInstance._inProc; 
            } 
        }
    } 

    sealed internal class SQL {

        private SQL() { /* prevent utility class from being insantiated*/ } 

        // The class SQL defines the exceptions that are specific to the SQL Adapter. 
        // The class contains functions that take the proper informational variables and then construct 
        // the appropriate exception with an error string obtained from the resource Framework.txt.
        // The exception is then returned to the caller, so that the caller may then throw from its 
        // location so that the catcher of the exception will have the appropriate call stack.
        // This class is used so that there will be compile time checking of error
        // messages.  The resource Framework.txt will ensure proper string text based on the appropriate
        // locale. 

        // 
        // SQL specific exceptions 
        //
 
        //
        // SQL.Connection
        //
 
        static internal Exception CannotGetDTCAddress() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_CannotGetDTCAddress)); 
        } 

        static internal Exception InvalidOptionLength(string key) { 
            return ADP.Argument(Res.GetString(Res.SQL_InvalidOptionLength, key));
        }
        static internal Exception InvalidInternalPacketSize (string str) {
            return ADP.ArgumentOutOfRange (str); 
        }
        static internal Exception InvalidPacketSize() { 
            return ADP.ArgumentOutOfRange (Res.GetString(Res.SQL_InvalidTDSPacketSize)); 
        }
        static internal Exception InvalidPacketSizeValue() { 
            return ADP.Argument(Res.GetString(Res.SQL_InvalidPacketSizeValue));
        }
        static internal Exception InvalidSSPIPacketSize() {
            return ADP.Argument(Res.GetString(Res.SQL_InvalidSSPIPacketSize)); 
        }
        static internal Exception NullEmptyTransactionName() { 
            return ADP.Argument(Res.GetString(Res.SQL_NullEmptyTransactionName)); 
        }
        static internal Exception SnapshotNotSupported(IsolationLevel level) { 
            return ADP.Argument(Res.GetString(Res.SQL_SnapshotNotSupported, typeof(IsolationLevel), level.ToString()));
        }
        static internal Exception UserInstanceFailoverNotCompatible() {
            return ADP.Argument(Res.GetString(Res.SQL_UserInstanceFailoverNotCompatible)); 
        }
        static internal Exception InvalidSQLServerVersionUnknown() { 
            return ADP.DataAdapter(Res.GetString(Res.SQL_InvalidSQLServerVersionUnknown)); 
        }
        static internal Exception ConnectionLockedForBcpEvent() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ConnectionLockedForBcpEvent));
        }
        static internal Exception AsyncConnectionRequired() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_AsyncConnectionRequired)); 
        }
        static internal Exception FatalTimeout() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_FatalTimeout)); 
        }
        static internal Exception InstanceFailure() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InstanceFailure));
        }
        static internal Exception ChangePasswordArgumentMissing(string argumentName) {
            return ADP.ArgumentNull(Res.GetString(Res.SQL_ChangePasswordArgumentMissing, argumentName)); 
        }
        static internal Exception ChangePasswordConflictsWithSSPI() { 
            return ADP.Argument(Res.GetString(Res.SQL_ChangePasswordConflictsWithSSPI)); 
        }
        static internal Exception ChangePasswordRequiresYukon() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ChangePasswordRequiresYukon));
        }
        static internal Exception UnknownSysTxIsolationLevel(SysTx.IsolationLevel isolationLevel) {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_UnknownSysTxIsolationLevel, isolationLevel.ToString())); 
        }
        static internal Exception ChangePasswordUseOfUnallowedKey (string key) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ChangePasswordUseOfUnallowedKey, key)); 
        }
        static internal Exception InvalidPartnerConfiguration (string server, string database) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InvalidPartnerConfiguration, server, database));
        }
        static internal Exception MARSUnspportedOnConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_MarsUnsupportedOnConnection)); 
        }
        static internal Exception AsyncInProcNotSupported() { 
            return ADP.NotSupported(Res.GetString(Res.SQL_AsyncInProcNotSupported)); 
        }
        static internal Exception CannotModifyPropertyAsyncOperationInProgress(string property) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_CannotModifyPropertyAsyncOperationInProgress, property));
        }
        static internal Exception NonLocalSSEInstance() {
            return ADP.NotSupported(Res.GetString(Res.SQL_NonLocalSSEInstance)); 
        }
        // 
        // SQL.DataCommand 
        //
        static internal Exception NotificationsRequireYukon() { 
            return ADP.NotSupported(Res.GetString(Res.SQL_NotificationsRequireYukon));
        }

        static internal ArgumentOutOfRangeException NotSupportedEnumerationValue(Type type, int value) { 
            return ADP.ArgumentOutOfRange(Res.GetString(Res.SQL_NotSupportedEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
        } 
 
        static internal ArgumentOutOfRangeException NotSupportedCommandType(CommandType value) {
#if DEBUG 
            switch(value) {
            case CommandType.Text:
            case CommandType.StoredProcedure:
                Debug.Assert(false, "valid CommandType " + value.ToString()); 
                break;
            case CommandType.TableDirect: 
                break; 
            default:
                Debug.Assert(false, "invalid CommandType " + value.ToString()); 
                break;
            }
#endif
            return NotSupportedEnumerationValue(typeof(CommandType), (int)value); 
        }
        static internal ArgumentOutOfRangeException NotSupportedIsolationLevel(IsolationLevel value) { 
#if DEBUG 
            switch(value) {
            case IsolationLevel.Unspecified: 
            case IsolationLevel.ReadCommitted:
            case IsolationLevel.ReadUncommitted:
            case IsolationLevel.RepeatableRead:
            case IsolationLevel.Serializable: 
            case IsolationLevel.Snapshot:
                Debug.Assert(false, "valid IsolationLevel " + value.ToString()); 
                break; 
            case IsolationLevel.Chaos:
                break; 
            default:
                Debug.Assert(false, "invalid IsolationLevel " + value.ToString());
                break;
            } 
#endif
            return NotSupportedEnumerationValue(typeof(IsolationLevel), (int)value); 
        } 

        static internal Exception OperationCancelled() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_OperationCancelled));
        }

        static internal Exception PendingBeginXXXExists() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_PendingBeginXXXExists));
        } 
 
        static internal ArgumentOutOfRangeException InvalidSqlDependencyTimeout(string param) {
            return ADP.ArgumentOutOfRange(Res.GetString(Res.SqlDependency_InvalidTimeout), param); 
        }

        static internal Exception NonXmlResult() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_NonXmlResult)); 
        }
 
        // 
        // SQL.DataParameter
        // 
        static internal Exception InvalidUdt3PartNameFormat() {
            return ADP.Argument(Res.GetString(Res.SQL_InvalidUdt3PartNameFormat));
        }
        static internal Exception InvalidParameterTypeNameFormat() { 
            return ADP.Argument(Res.GetString(Res.SQL_InvalidParameterTypeNameFormat));
        } 
        static internal Exception InvalidParameterNameLength(string value) { 
            return ADP.Argument(Res.GetString(Res.SQL_InvalidParameterNameLength, value));
        } 
        static internal Exception PrecisionValueOutOfRange(byte precision) {
            return ADP.Argument(Res.GetString(Res.SQL_PrecisionValueOutOfRange, precision.ToString(CultureInfo.InvariantCulture)));
        }
        static internal Exception ScaleValueOutOfRange(byte scale) { 
            return ADP.Argument(Res.GetString(Res.SQL_ScaleValueOutOfRange, scale.ToString(CultureInfo.InvariantCulture)));
        } 
        static internal Exception TimeScaleValueOutOfRange(byte scale) { 
            return ADP.Argument(Res.GetString(Res.SQL_TimeScaleValueOutOfRange, scale.ToString(CultureInfo.InvariantCulture)));
        } 
        static internal Exception InvalidSqlDbType(SqlDbType value) {
            return ADP.InvalidEnumerationValue(typeof(SqlDbType), (int) value);
        }
        static internal Exception UnsupportedTVPOutputParameter(ParameterDirection direction, string paramName) { 
            return ADP.NotSupported(Res.GetString(Res.SqlParameter_UnsupportedTVPOutputParameter,
                        direction.ToString(CultureInfo.InvariantCulture), paramName)); 
        } 
        static internal Exception DBNullNotSupportedForTVPValues(string paramName) {
            return ADP.NotSupported(Res.GetString(Res.SqlParameter_DBNullNotSupportedForTVP, paramName)); 
        }
        static internal Exception UnexpectedTypeNameForNonStructParams(string paramName) {
            return ADP.NotSupported(Res.GetString(Res.SqlParameter_UnexpectedTypeNameForNonStruct, paramName));
        } 
        static internal Exception SingleValuedStructNotSupported() {
            return ADP.NotSupported(Res.GetString(Res.MetaType_SingleValuedStructNotSupported)); 
        } 
        static internal Exception ParameterInvalidVariant(string paramName) {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ParameterInvalidVariant, paramName)); 
        }

        static internal Exception MustSetTypeNameForParam(string paramType, string paramName) {
            return ADP.Argument(Res.GetString(Res.SQL_ParameterTypeNameRequired, paramType, paramName)); 
        }
        static internal Exception NullSchemaTableDataTypeNotSupported(string columnName) { 
            return ADP.Argument(Res.GetString(Res.NullSchemaTableDataTypeNotSupported, columnName)); 
        }
        static internal Exception InvalidSchemaTableOrdinals() { 
            return ADP.Argument(Res.GetString(Res.InvalidSchemaTableOrdinals));
        }
        static internal Exception EnumeratedRecordMetaDataChanged(string fieldName, int recordNumber) {
            return ADP.Argument(Res.GetString(Res.SQL_EnumeratedRecordMetaDataChanged, fieldName, recordNumber)); 
        }
        static internal Exception EnumeratedRecordFieldCountChanged(int recordNumber) { 
            return ADP.Argument(Res.GetString(Res.SQL_EnumeratedRecordFieldCountChanged, recordNumber)); 
        }
 
        //
        // SQL.SqlDataAdapter
        //
 
        //
        // SQL.TDSParser 
        // 
        static internal Exception InvalidTDSVersion() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InvalidTDSVersion)); 
        }
        static internal Exception ParsingError() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ParsingError));
        } 
        static internal Exception MoneyOverflow(string moneyValue) {
            return ADP.Overflow(Res.GetString(Res.SQL_MoneyOverflow, moneyValue)); 
        } 
        static internal Exception SmallDateTimeOverflow(string datetime) {
            return ADP.Overflow(Res.GetString(Res.SQL_SmallDateTimeOverflow, datetime)); 
        }
        static internal Exception SNIPacketAllocationFailure() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SNIPacketAllocationFailure));
        } 
        static internal Exception TimeOverflow(string time) {
            return ADP.Overflow(Res.GetString(Res.SQL_TimeOverflow, time)); 
        } 

        // 
        // SQL.SqlDataReader
        //
        static internal Exception InvalidRead() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InvalidRead)); 
        }
 
        static internal Exception NonBlobColumn(string columnName) { 
            return ADP.InvalidCast(Res.GetString(Res.SQL_NonBlobColumn, columnName));
        } 

        static internal Exception NonCharColumn(string columnName) {
            return ADP.InvalidCast(Res.GetString(Res.SQL_NonCharColumn, columnName));
        } 
        static internal Exception UDTUnexpectedResult(string exceptionText){
        	return ADP.TypeLoad(Res.GetString(Res.SQLUDT_Unexpected,exceptionText)); 
        } 

#if WINFSFunctionality 
        static internal Exception UDTInvalidDbId(int dbId,int typeId){
        	return ADP.TypeLoad(Res.GetString(Res.SQLUDT_InvalidDbId,dbId,typeId));
        }
 
        static internal Exception UDTCantLoadAssembly(string assemblyName){
        	return ADP.TypeLoad(Res.GetString(Res.SQLUDT_CantLoadAssembly,assemblyName)); 
        } 

        static internal InvalidOperationException UDTInWhereClause() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQLUDT_InWhereClause));
        }
#endif
 
/*
        Auto assembly download disabled for Whidbey. 
        static internal Exception UDTAssemblyDownloadNotEnabled(){ 
        	return ADP.TypeLoad(Res.GetString(Res.SQLUDT_CantLoadAssembly, Res.GetString(Res.SQLUDT_AssemblyDownloadNotEnabled)));
        } 
*/


        // 
        // SQL.SqlDelegatedTransaction
        // 
        static internal Exception CannotCompleteDelegatedTransactionWithOpenResults() { 
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(TdsEnums.TIMEOUT_EXPIRED, (byte)0x00, TdsEnums.MIN_ERROR_CLASS, null, (Res.GetString(Res.ADP_OpenReaderExists)), "", 0)); 
            return SqlException.CreateException(errors, null);
        }
        static internal SysTx.TransactionPromotionException PromotionFailed(Exception inner) {
            SysTx.TransactionPromotionException e = new SysTx.TransactionPromotionException(Res.GetString(Res.SqlDelegatedTransaction_PromotionFailed), inner); 
            ADP.TraceExceptionAsReturnValue(e);
            return e; 
        } 

        // 
        // SQL.SqlDependency
        //
        static internal Exception SqlCommandHasExistingSqlNotificationRequest(){
            return ADP.InvalidOperation(Res.GetString(Res.SQLNotify_AlreadyHasCommand)); 
        }
 
        static internal Exception SqlDepCannotBeCreatedInProc() { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlNotify_SqlDepCannotBeCreatedInProc));
        } 

        static internal Exception SqlDepDefaultOptionsButNoStart() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_DefaultOptionsButNoStart));
        } 

        static internal Exception SqlDependencyDatabaseBrokerDisabled() { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_DatabaseBrokerDisabled)); 
        }
 
        static internal Exception SqlDependencyEventNoDuplicate() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_EventNoDuplicate));
        }
 
        static internal Exception SqlDependencyDuplicateStart() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_DuplicateStart)); 
        } 

        static internal Exception SqlDependencyIdMismatch() { 
            // do not include the id because it may require SecurityPermission(Infrastructure) permission
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_IdMismatch));
        }
 
        static internal Exception SqlDependencyNoMatchingServerStart() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_NoMatchingServerStart)); 
        } 

        static internal Exception SqlDependencyNoMatchingServerDatabaseStart() { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_NoMatchingServerDatabaseStart));
        }

        static internal Exception SqlNotificationException(SqlNotificationEventArgs notify){ 
            return ADP.InvalidOperation(Res.GetString(Res.SQLNotify_ErrorFormat, notify.Type,notify.Info,notify.Source));
        } 
 
        //
        // SQL.SqlMetaData 
        //
        static internal Exception SqlMetaDataNoMetaData(){
            return ADP.InvalidOperation(Res.GetString(Res.SqlMetaData_NoMetadata));
        } 

        static internal Exception MustSetUdtTypeNameForUdtParams(){ 
            return ADP.Argument(Res.GetString(Res.SQLUDT_InvalidUdtTypeName)); 
        }
 
        static internal Exception UnexpectedUdtTypeNameForNonUdtParams(){
            return ADP.Argument(Res.GetString(Res.SQLUDT_UnexpectedUdtTypeName));
        }
 
        static internal Exception UDTInvalidSqlType(string typeName){
            return ADP.Argument(Res.GetString(Res.SQLUDT_InvalidSqlType, typeName)); 
        } 

        static internal Exception InvalidSqlDbTypeForConstructor(SqlDbType type) { 
            return ADP.Argument(Res.GetString(Res.SqlMetaData_InvalidSqlDbTypeForConstructorFormat, type.ToString()));
        }

        static internal Exception NameTooLong(string parameterName){ 
            return ADP.Argument(Res.GetString(Res.SqlMetaData_NameTooLong), parameterName);
        } 
 
        static internal Exception InvalidSortOrder(SortOrder order) {
            return ADP.InvalidEnumerationValue(typeof(SortOrder), (int)order); 
        }

        static internal Exception MustSpecifyBothSortOrderAndOrdinal(SortOrder order, int ordinal) {
            return ADP.InvalidOperation(Res.GetString(Res.SqlMetaData_SpecifyBothSortOrderAndOrdinal, order.ToString(), ordinal)); 
        }
 
        static internal Exception TableTypeCanOnlyBeParameter() { 
            return ADP.Argument(Res.GetString(Res.SQLTVP_TableTypeCanOnlyBeParameter));
        } 
        static internal Exception UnsupportedColumnTypeForSqlProvider(string columnName, string typeName) {
            return ADP.Argument(Res.GetString(Res.SqlProvider_InvalidDataColumnType, columnName, typeName));
        }
        static internal Exception InvalidColumnMaxLength(string columnName, long maxLength) { 
            return ADP.Argument(Res.GetString(Res.SqlProvider_InvalidDataColumnMaxLength, columnName, maxLength));
        } 
        static internal Exception InvalidColumnPrecScale() { 
            return ADP.Argument(Res.GetString(Res.SqlMisc_InvalidPrecScaleMessage));
        } 
        static internal Exception NotEnoughColumnsInStructuredType() {
            return ADP.Argument(Res.GetString(Res.SqlProvider_NotEnoughColumnsInStructuredType));
        }
        static internal Exception DuplicateSortOrdinal(int sortOrdinal) { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlProvider_DuplicateSortOrdinal, sortOrdinal));
        } 
        static internal Exception MissingSortOrdinal(int sortOrdinal) { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlProvider_MissingSortOrdinal, sortOrdinal));
        } 
        static internal Exception SortOrdinalGreaterThanFieldCount(int columnOrdinal, int sortOrdinal) {
            return ADP.InvalidOperation(Res.GetString(Res.SqlProvider_SortOrdinalGreaterThanFieldCount, sortOrdinal, columnOrdinal));
        }
        static internal Exception IEnumerableOfSqlDataRecordHasNoRows() { 
            return ADP.Argument(Res.GetString(Res.IEnumerableOfSqlDataRecordHasNoRows));
        } 
 

 
        //
        //  SqlPipe
        //
        static internal Exception SqlPipeCommandHookedUpToNonContextConnection() { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlPipe_CommandHookedUpToNonContextConnection));
        } 
 
        static internal Exception SqlPipeMessageTooLong( int messageLength ) {
            return ADP.Argument(Res.GetString(Res.SqlPipe_MessageTooLong, messageLength)); 
        }

        static internal Exception SqlPipeIsBusy() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlPipe_IsBusy)); 
        }
 
        static internal Exception SqlPipeAlreadyHasAnOpenResultSet( string methodName ) { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlPipe_AlreadyHasAnOpenResultSet, methodName));
        } 

        static internal Exception SqlPipeDoesNotHaveAnOpenResultSet( string methodName ) {
            return ADP.InvalidOperation(Res.GetString(Res.SqlPipe_DoesNotHaveAnOpenResultSet, methodName));
        } 

        // 
        // : ISqlResultSet 
        //
        static internal Exception SqlResultSetClosed(string methodname) { 
            if (methodname == null) {
                return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetClosed2));
            }
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetClosed, methodname)); 
        }
        static internal Exception SqlResultSetNoData(string methodname) { 
            return ADP.InvalidOperation(Res.GetString(Res.ADP_DataReaderNoData, methodname)); 
        }
        static internal Exception SqlRecordReadOnly(string methodname) { 
            if (methodname == null) {
                return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlRecordReadOnly2));
            }
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlRecordReadOnly, methodname)); 
        }
 
        static internal Exception SqlResultSetRowDeleted(string methodname) { 
            if (methodname == null) {
                return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetRowDeleted2)); 
            }
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetRowDeleted, methodname));
        }
 
        static internal Exception SqlResultSetCommandNotInSameConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetCommandNotInSameConnection)); 
        } 

        static internal Exception SqlResultSetNoAcceptableCursor() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetNoAcceptableCursor));
        }

        // 
        // SQL.BulkLoad
        // 
        static internal Exception BulkLoadMappingInaccessible() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadMappingInaccessible));
        } 
        static internal Exception BulkLoadMappingsNamesOrOrdinalsOnly() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadMappingsNamesOrOrdinalsOnly));
        }
        static internal Exception BulkLoadCannotConvertValue(Type sourcetype, MetaType metatype, Exception e) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadCannotConvertValue, sourcetype.Name, metatype.TypeName), e);
        } 
        static internal Exception BulkLoadNonMatchingColumnMapping() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNonMatchingColumnMapping));
        } 
        static internal Exception BulkLoadNonMatchingColumnName(string columnName) {
            return BulkLoadNonMatchingColumnName(columnName, null);
        }
        static internal Exception BulkLoadNonMatchingColumnName(string columnName, Exception e) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNonMatchingColumnName, columnName), e);
        } 
        static internal Exception BulkLoadStringTooLong() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadStringTooLong));
        } 
        static internal Exception BulkLoadInvalidVariantValue() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadInvalidVariantValue));
        }
        static internal Exception BulkLoadInvalidTimeout(int timeout) { 
            return ADP.Argument(Res.GetString(Res.SQL_BulkLoadInvalidTimeout, timeout.ToString(CultureInfo.InvariantCulture)));
        } 
        static internal Exception BulkLoadExistingTransaction() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadExistingTransaction));
        } 
        static internal Exception BulkLoadNoCollation() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNoCollation));
        }
        static internal Exception BulkLoadConflictingTransactionOption() { 
            return ADP.Argument(Res.GetString(Res.SQL_BulkLoadConflictingTransactionOption));
        } 
        static internal Exception BulkLoadLcidMismatch(int sourceLcid, string sourceColumnName, int destinationLcid, string destinationColumnName) { 
            return ADP.InvalidOperation (Res.GetString (Res.Sql_BulkLoadLcidMismatch, sourceLcid, sourceColumnName, destinationLcid, destinationColumnName));
        } 
        static internal Exception InvalidOperationInsideEvent() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadInvalidOperationInsideEvent));
        }
        static internal Exception BulkLoadMissingDestinationTable() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadMissingDestinationTable));
        } 
        static internal Exception BulkLoadInvalidDestinationTable(string tableName, Exception inner) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadInvalidDestinationTable, tableName), inner);
        } 
        static internal Exception BulkLoadBulkLoadNotAllowDBNull(string columnName) {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNotAllowDBNull, columnName));
        }
 
        //
        // transactions. 
        // 
        static internal Exception ConnectionDoomed() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ConnectionDoomed)); 
        }

        static internal readonly byte[] AttentionHeader = new byte[] {
            TdsEnums.MT_ATTN,               // Message Type 
            TdsEnums.ST_EOM,                // Status
            TdsEnums.HEADER_LEN >> 8,       // length - upper byte 
            TdsEnums.HEADER_LEN & 0xff,     // length - lower byte 
            0,                              // spid
            0,                              // spid 
            0,                              // packet (out of band)
            0                               // window
        };
 
        //
        // Merged Provider 
        // 
        static internal Exception BatchedUpdatesNotAvailableOnContextConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BatchedUpdatesNotAvailableOnContextConnection)); 
        }
        static internal Exception ContextAllowsLimitedKeywords() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ContextAllowsLimitedKeywords));
        } 
        static internal Exception ContextAllowsOnlyTypeSystem2005() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ContextAllowsOnlyTypeSystem2005)); 
        } 
        static internal Exception ContextConnectionIsInUse() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ContextConnectionIsInUse)); 
        }
        static internal Exception ContextUnavailableOutOfProc() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ContextUnavailableOutOfProc));
        } 
        static internal Exception ContextUnavailableWhileInProc() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ContextUnavailableWhileInProc)); 
        } 
        static internal Exception NestedTransactionScopesNotSupported() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_NestedTransactionScopesNotSupported)); 
        }
        static internal Exception NotAvailableOnContextConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_NotAvailableOnContextConnection));
        } 
        static internal Exception NotificationsNotAvailableOnContextConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_NotificationsNotAvailableOnContextConnection)); 
        } 
        static internal Exception UnexpectedSmiEvent(Microsoft.SqlServer.Server.SmiEventSink_Default.UnexpectedEventType eventType) {
            Debug.Assert(false, "UnexpectedSmiEvent: "+eventType.ToString());    // Assert here, because these exceptions will most likely be eaten by the server. 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_UnexpectedSmiEvent, (int)eventType));
        }
        static internal Exception UserInstanceNotAvailableInProc() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_UserInstanceNotAvailableInProc)); 
        }
        static internal Exception ArgumentLengthMismatch( string arg1, string arg2 ) { 
            return ADP.Argument( Res.GetString( Res.SQL_ArgumentLengthMismatch, arg1, arg2 ) ); 
        }
        static internal Exception InvalidSqlDbTypeOneAllowedType( SqlDbType invalidType, string method, SqlDbType allowedType ) { 
            return ADP.Argument( Res.GetString( Res.SQL_InvalidSqlDbTypeWithOneAllowedType, invalidType, method, allowedType ) );
        }
        static internal Exception SqlPipeErrorRequiresSendEnd( ) {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_PipeErrorRequiresSendEnd)); 
        }
        static internal Exception TooManyValues(string arg) { 
            return ADP.Argument(Res.GetString(Res.SQL_TooManyValues), arg); 
        }
        static internal Exception StreamWriteNotSupported() { 
            return ADP.NotSupported(Res.GetString(Res.SQL_StreamWriteNotSupported));
        }
        static internal Exception StreamReadNotSupported() {
            return ADP.NotSupported(Res.GetString(Res.SQL_StreamReadNotSupported)); 
        }
        static internal Exception StreamSeekNotSupported() { 
            return ADP.NotSupported(Res.GetString(Res.SQL_StreamSeekNotSupported)); 
        }
        static internal System.Data.SqlTypes.SqlNullValueException SqlNullValue() { 
            System.Data.SqlTypes.SqlNullValueException e = new System.Data.SqlTypes.SqlNullValueException();
            ADP.TraceExceptionAsReturnValue(e);
            return e;
        } 
        //
 
        static internal Exception ParameterSizeRestrictionFailure(int index) { 
            return ADP.InvalidOperation(Res.GetString(Res.OleDb_CommandParameterError, index.ToString(CultureInfo.InvariantCulture), "SqlParameter.Size"));
        } 
        static internal Exception SubclassMustOverride() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlMisc_SubclassMustOverride));
        }
 
        // BulkLoad
        internal const string WriteToServer = "WriteToServer"; 
 
        // Default values for SqlDependency and SqlNotificationRequest
        internal const int SqlDependencyTimeoutDefault = 0; 
        internal const int SqlDependencyServerTimeout  = 5 * 24 * 3600; // 5 days - used to compute default TTL of the dependency
        internal const string SqlNotificationServiceDefault         = "SqlQueryNotificationService";
        internal const string SqlNotificationStoredProcedureDefault = "SqlQueryNotificationStoredProcedure";
 
         // constant strings
        internal const string Transaction= "Transaction"; 
        internal const string Connection = "Connection"; 
    }
 
    sealed internal class SQLMessage {

        //
 
        private SQLMessage() { /* prevent utility class from being insantiated*/ }
 
        // The class SQLMessage defines the error messages that are specific to the SqlDataAdapter 
        // that are caused by a netlib error.  The functions will be called and then return the
        // appropriate error message from the resource Framework.txt.  The SqlDataAdapter will then 
        // take the error message and then create a SqlError for the message and then place
        // that into a SqlException that is either thrown to the user or cached for throwing at
        // a later time.  This class is used so that there will be compile time checking of error
        // messages.  The resource Framework.txt will ensure proper string text based on the appropriate 
        // locale.
 
        static internal string CultureIdError() { 
            return Res.GetString(Res.SQL_CultureIdError);
        } 
        static internal string EncryptionNotSupportedByClient() {
            return Res.GetString(Res.SQL_EncryptionNotSupportedByClient);
        }
        static internal string EncryptionNotSupportedByServer() { 
            return Res.GetString(Res.SQL_EncryptionNotSupportedByServer);
        } 
        static internal string OperationCancelled() { 
            return Res.GetString(Res.SQL_OperationCancelled);
        } 
        static internal string SevereError() {
            return Res.GetString(Res.SQL_SevereError);
        }
        static internal string SSPIInitializeError() { 
            return Res.GetString(Res.SQL_SSPIInitializeError);
        } 
        static internal string SSPIGenerateError() { 
            return Res.GetString(Res.SQL_SSPIGenerateError);
        } 
        static internal string Timeout() {
            return Res.GetString(Res.SQL_Timeout);
        }
        static internal string UserInstanceFailure() { 
            return Res.GetString(Res.SQL_UserInstanceFailure);
        } 
    } 
}//namespace

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlUtil.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
    using System; 
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection; 
    using System.Runtime.Serialization.Formatters; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Security.Principal;
    using System.Threading;
    using System.Text;
    using SysTx = System.Transactions; 

    sealed internal class InOutOfProcHelper { 
        private static readonly InOutOfProcHelper SingletonInstance = new InOutOfProcHelper(); 

        private bool _inProc = false; 

        // InOutOfProcHelper detects whether it's running inside the server or not.  It does this
        //  by checking for the existence of a well-known function export on the current process.
        //  Note that calling conventions, etc. do not matter -- we'll never call the function, so 
        //  only the name match or lack thereof matter.
        private InOutOfProcHelper() { 
            // Don't need to close this handle... 
            IntPtr handle = SafeNativeMethods.GetModuleHandle(null);
            if (IntPtr.Zero != handle) { 
                //


                if (IntPtr.Zero != SafeNativeMethods.GetProcAddress(handle, "_______SQL______Process______Available@0")) { 
                    _inProc = true;
                } 
                else if (IntPtr.Zero != SafeNativeMethods.GetProcAddress(handle, "______SQL______Process______Available")) { 
                    _inProc = true;
                } 
            }
        }

        internal static bool InProc { 
            get {
                return SingletonInstance._inProc; 
            } 
        }
    } 

    sealed internal class SQL {

        private SQL() { /* prevent utility class from being insantiated*/ } 

        // The class SQL defines the exceptions that are specific to the SQL Adapter. 
        // The class contains functions that take the proper informational variables and then construct 
        // the appropriate exception with an error string obtained from the resource Framework.txt.
        // The exception is then returned to the caller, so that the caller may then throw from its 
        // location so that the catcher of the exception will have the appropriate call stack.
        // This class is used so that there will be compile time checking of error
        // messages.  The resource Framework.txt will ensure proper string text based on the appropriate
        // locale. 

        // 
        // SQL specific exceptions 
        //
 
        //
        // SQL.Connection
        //
 
        static internal Exception CannotGetDTCAddress() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_CannotGetDTCAddress)); 
        } 

        static internal Exception InvalidOptionLength(string key) { 
            return ADP.Argument(Res.GetString(Res.SQL_InvalidOptionLength, key));
        }
        static internal Exception InvalidInternalPacketSize (string str) {
            return ADP.ArgumentOutOfRange (str); 
        }
        static internal Exception InvalidPacketSize() { 
            return ADP.ArgumentOutOfRange (Res.GetString(Res.SQL_InvalidTDSPacketSize)); 
        }
        static internal Exception InvalidPacketSizeValue() { 
            return ADP.Argument(Res.GetString(Res.SQL_InvalidPacketSizeValue));
        }
        static internal Exception InvalidSSPIPacketSize() {
            return ADP.Argument(Res.GetString(Res.SQL_InvalidSSPIPacketSize)); 
        }
        static internal Exception NullEmptyTransactionName() { 
            return ADP.Argument(Res.GetString(Res.SQL_NullEmptyTransactionName)); 
        }
        static internal Exception SnapshotNotSupported(IsolationLevel level) { 
            return ADP.Argument(Res.GetString(Res.SQL_SnapshotNotSupported, typeof(IsolationLevel), level.ToString()));
        }
        static internal Exception UserInstanceFailoverNotCompatible() {
            return ADP.Argument(Res.GetString(Res.SQL_UserInstanceFailoverNotCompatible)); 
        }
        static internal Exception InvalidSQLServerVersionUnknown() { 
            return ADP.DataAdapter(Res.GetString(Res.SQL_InvalidSQLServerVersionUnknown)); 
        }
        static internal Exception ConnectionLockedForBcpEvent() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ConnectionLockedForBcpEvent));
        }
        static internal Exception AsyncConnectionRequired() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_AsyncConnectionRequired)); 
        }
        static internal Exception FatalTimeout() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_FatalTimeout)); 
        }
        static internal Exception InstanceFailure() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InstanceFailure));
        }
        static internal Exception ChangePasswordArgumentMissing(string argumentName) {
            return ADP.ArgumentNull(Res.GetString(Res.SQL_ChangePasswordArgumentMissing, argumentName)); 
        }
        static internal Exception ChangePasswordConflictsWithSSPI() { 
            return ADP.Argument(Res.GetString(Res.SQL_ChangePasswordConflictsWithSSPI)); 
        }
        static internal Exception ChangePasswordRequiresYukon() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ChangePasswordRequiresYukon));
        }
        static internal Exception UnknownSysTxIsolationLevel(SysTx.IsolationLevel isolationLevel) {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_UnknownSysTxIsolationLevel, isolationLevel.ToString())); 
        }
        static internal Exception ChangePasswordUseOfUnallowedKey (string key) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ChangePasswordUseOfUnallowedKey, key)); 
        }
        static internal Exception InvalidPartnerConfiguration (string server, string database) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InvalidPartnerConfiguration, server, database));
        }
        static internal Exception MARSUnspportedOnConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_MarsUnsupportedOnConnection)); 
        }
        static internal Exception AsyncInProcNotSupported() { 
            return ADP.NotSupported(Res.GetString(Res.SQL_AsyncInProcNotSupported)); 
        }
        static internal Exception CannotModifyPropertyAsyncOperationInProgress(string property) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_CannotModifyPropertyAsyncOperationInProgress, property));
        }
        static internal Exception NonLocalSSEInstance() {
            return ADP.NotSupported(Res.GetString(Res.SQL_NonLocalSSEInstance)); 
        }
        // 
        // SQL.DataCommand 
        //
        static internal Exception NotificationsRequireYukon() { 
            return ADP.NotSupported(Res.GetString(Res.SQL_NotificationsRequireYukon));
        }

        static internal ArgumentOutOfRangeException NotSupportedEnumerationValue(Type type, int value) { 
            return ADP.ArgumentOutOfRange(Res.GetString(Res.SQL_NotSupportedEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
        } 
 
        static internal ArgumentOutOfRangeException NotSupportedCommandType(CommandType value) {
#if DEBUG 
            switch(value) {
            case CommandType.Text:
            case CommandType.StoredProcedure:
                Debug.Assert(false, "valid CommandType " + value.ToString()); 
                break;
            case CommandType.TableDirect: 
                break; 
            default:
                Debug.Assert(false, "invalid CommandType " + value.ToString()); 
                break;
            }
#endif
            return NotSupportedEnumerationValue(typeof(CommandType), (int)value); 
        }
        static internal ArgumentOutOfRangeException NotSupportedIsolationLevel(IsolationLevel value) { 
#if DEBUG 
            switch(value) {
            case IsolationLevel.Unspecified: 
            case IsolationLevel.ReadCommitted:
            case IsolationLevel.ReadUncommitted:
            case IsolationLevel.RepeatableRead:
            case IsolationLevel.Serializable: 
            case IsolationLevel.Snapshot:
                Debug.Assert(false, "valid IsolationLevel " + value.ToString()); 
                break; 
            case IsolationLevel.Chaos:
                break; 
            default:
                Debug.Assert(false, "invalid IsolationLevel " + value.ToString());
                break;
            } 
#endif
            return NotSupportedEnumerationValue(typeof(IsolationLevel), (int)value); 
        } 

        static internal Exception OperationCancelled() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_OperationCancelled));
        }

        static internal Exception PendingBeginXXXExists() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_PendingBeginXXXExists));
        } 
 
        static internal ArgumentOutOfRangeException InvalidSqlDependencyTimeout(string param) {
            return ADP.ArgumentOutOfRange(Res.GetString(Res.SqlDependency_InvalidTimeout), param); 
        }

        static internal Exception NonXmlResult() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_NonXmlResult)); 
        }
 
        // 
        // SQL.DataParameter
        // 
        static internal Exception InvalidUdt3PartNameFormat() {
            return ADP.Argument(Res.GetString(Res.SQL_InvalidUdt3PartNameFormat));
        }
        static internal Exception InvalidParameterTypeNameFormat() { 
            return ADP.Argument(Res.GetString(Res.SQL_InvalidParameterTypeNameFormat));
        } 
        static internal Exception InvalidParameterNameLength(string value) { 
            return ADP.Argument(Res.GetString(Res.SQL_InvalidParameterNameLength, value));
        } 
        static internal Exception PrecisionValueOutOfRange(byte precision) {
            return ADP.Argument(Res.GetString(Res.SQL_PrecisionValueOutOfRange, precision.ToString(CultureInfo.InvariantCulture)));
        }
        static internal Exception ScaleValueOutOfRange(byte scale) { 
            return ADP.Argument(Res.GetString(Res.SQL_ScaleValueOutOfRange, scale.ToString(CultureInfo.InvariantCulture)));
        } 
        static internal Exception TimeScaleValueOutOfRange(byte scale) { 
            return ADP.Argument(Res.GetString(Res.SQL_TimeScaleValueOutOfRange, scale.ToString(CultureInfo.InvariantCulture)));
        } 
        static internal Exception InvalidSqlDbType(SqlDbType value) {
            return ADP.InvalidEnumerationValue(typeof(SqlDbType), (int) value);
        }
        static internal Exception UnsupportedTVPOutputParameter(ParameterDirection direction, string paramName) { 
            return ADP.NotSupported(Res.GetString(Res.SqlParameter_UnsupportedTVPOutputParameter,
                        direction.ToString(CultureInfo.InvariantCulture), paramName)); 
        } 
        static internal Exception DBNullNotSupportedForTVPValues(string paramName) {
            return ADP.NotSupported(Res.GetString(Res.SqlParameter_DBNullNotSupportedForTVP, paramName)); 
        }
        static internal Exception UnexpectedTypeNameForNonStructParams(string paramName) {
            return ADP.NotSupported(Res.GetString(Res.SqlParameter_UnexpectedTypeNameForNonStruct, paramName));
        } 
        static internal Exception SingleValuedStructNotSupported() {
            return ADP.NotSupported(Res.GetString(Res.MetaType_SingleValuedStructNotSupported)); 
        } 
        static internal Exception ParameterInvalidVariant(string paramName) {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ParameterInvalidVariant, paramName)); 
        }

        static internal Exception MustSetTypeNameForParam(string paramType, string paramName) {
            return ADP.Argument(Res.GetString(Res.SQL_ParameterTypeNameRequired, paramType, paramName)); 
        }
        static internal Exception NullSchemaTableDataTypeNotSupported(string columnName) { 
            return ADP.Argument(Res.GetString(Res.NullSchemaTableDataTypeNotSupported, columnName)); 
        }
        static internal Exception InvalidSchemaTableOrdinals() { 
            return ADP.Argument(Res.GetString(Res.InvalidSchemaTableOrdinals));
        }
        static internal Exception EnumeratedRecordMetaDataChanged(string fieldName, int recordNumber) {
            return ADP.Argument(Res.GetString(Res.SQL_EnumeratedRecordMetaDataChanged, fieldName, recordNumber)); 
        }
        static internal Exception EnumeratedRecordFieldCountChanged(int recordNumber) { 
            return ADP.Argument(Res.GetString(Res.SQL_EnumeratedRecordFieldCountChanged, recordNumber)); 
        }
 
        //
        // SQL.SqlDataAdapter
        //
 
        //
        // SQL.TDSParser 
        // 
        static internal Exception InvalidTDSVersion() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InvalidTDSVersion)); 
        }
        static internal Exception ParsingError() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ParsingError));
        } 
        static internal Exception MoneyOverflow(string moneyValue) {
            return ADP.Overflow(Res.GetString(Res.SQL_MoneyOverflow, moneyValue)); 
        } 
        static internal Exception SmallDateTimeOverflow(string datetime) {
            return ADP.Overflow(Res.GetString(Res.SQL_SmallDateTimeOverflow, datetime)); 
        }
        static internal Exception SNIPacketAllocationFailure() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SNIPacketAllocationFailure));
        } 
        static internal Exception TimeOverflow(string time) {
            return ADP.Overflow(Res.GetString(Res.SQL_TimeOverflow, time)); 
        } 

        // 
        // SQL.SqlDataReader
        //
        static internal Exception InvalidRead() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InvalidRead)); 
        }
 
        static internal Exception NonBlobColumn(string columnName) { 
            return ADP.InvalidCast(Res.GetString(Res.SQL_NonBlobColumn, columnName));
        } 

        static internal Exception NonCharColumn(string columnName) {
            return ADP.InvalidCast(Res.GetString(Res.SQL_NonCharColumn, columnName));
        } 
        static internal Exception UDTUnexpectedResult(string exceptionText){
        	return ADP.TypeLoad(Res.GetString(Res.SQLUDT_Unexpected,exceptionText)); 
        } 

#if WINFSFunctionality 
        static internal Exception UDTInvalidDbId(int dbId,int typeId){
        	return ADP.TypeLoad(Res.GetString(Res.SQLUDT_InvalidDbId,dbId,typeId));
        }
 
        static internal Exception UDTCantLoadAssembly(string assemblyName){
        	return ADP.TypeLoad(Res.GetString(Res.SQLUDT_CantLoadAssembly,assemblyName)); 
        } 

        static internal InvalidOperationException UDTInWhereClause() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQLUDT_InWhereClause));
        }
#endif
 
/*
        Auto assembly download disabled for Whidbey. 
        static internal Exception UDTAssemblyDownloadNotEnabled(){ 
        	return ADP.TypeLoad(Res.GetString(Res.SQLUDT_CantLoadAssembly, Res.GetString(Res.SQLUDT_AssemblyDownloadNotEnabled)));
        } 
*/


        // 
        // SQL.SqlDelegatedTransaction
        // 
        static internal Exception CannotCompleteDelegatedTransactionWithOpenResults() { 
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(TdsEnums.TIMEOUT_EXPIRED, (byte)0x00, TdsEnums.MIN_ERROR_CLASS, null, (Res.GetString(Res.ADP_OpenReaderExists)), "", 0)); 
            return SqlException.CreateException(errors, null);
        }
        static internal SysTx.TransactionPromotionException PromotionFailed(Exception inner) {
            SysTx.TransactionPromotionException e = new SysTx.TransactionPromotionException(Res.GetString(Res.SqlDelegatedTransaction_PromotionFailed), inner); 
            ADP.TraceExceptionAsReturnValue(e);
            return e; 
        } 

        // 
        // SQL.SqlDependency
        //
        static internal Exception SqlCommandHasExistingSqlNotificationRequest(){
            return ADP.InvalidOperation(Res.GetString(Res.SQLNotify_AlreadyHasCommand)); 
        }
 
        static internal Exception SqlDepCannotBeCreatedInProc() { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlNotify_SqlDepCannotBeCreatedInProc));
        } 

        static internal Exception SqlDepDefaultOptionsButNoStart() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_DefaultOptionsButNoStart));
        } 

        static internal Exception SqlDependencyDatabaseBrokerDisabled() { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_DatabaseBrokerDisabled)); 
        }
 
        static internal Exception SqlDependencyEventNoDuplicate() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_EventNoDuplicate));
        }
 
        static internal Exception SqlDependencyDuplicateStart() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_DuplicateStart)); 
        } 

        static internal Exception SqlDependencyIdMismatch() { 
            // do not include the id because it may require SecurityPermission(Infrastructure) permission
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_IdMismatch));
        }
 
        static internal Exception SqlDependencyNoMatchingServerStart() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_NoMatchingServerStart)); 
        } 

        static internal Exception SqlDependencyNoMatchingServerDatabaseStart() { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlDependency_NoMatchingServerDatabaseStart));
        }

        static internal Exception SqlNotificationException(SqlNotificationEventArgs notify){ 
            return ADP.InvalidOperation(Res.GetString(Res.SQLNotify_ErrorFormat, notify.Type,notify.Info,notify.Source));
        } 
 
        //
        // SQL.SqlMetaData 
        //
        static internal Exception SqlMetaDataNoMetaData(){
            return ADP.InvalidOperation(Res.GetString(Res.SqlMetaData_NoMetadata));
        } 

        static internal Exception MustSetUdtTypeNameForUdtParams(){ 
            return ADP.Argument(Res.GetString(Res.SQLUDT_InvalidUdtTypeName)); 
        }
 
        static internal Exception UnexpectedUdtTypeNameForNonUdtParams(){
            return ADP.Argument(Res.GetString(Res.SQLUDT_UnexpectedUdtTypeName));
        }
 
        static internal Exception UDTInvalidSqlType(string typeName){
            return ADP.Argument(Res.GetString(Res.SQLUDT_InvalidSqlType, typeName)); 
        } 

        static internal Exception InvalidSqlDbTypeForConstructor(SqlDbType type) { 
            return ADP.Argument(Res.GetString(Res.SqlMetaData_InvalidSqlDbTypeForConstructorFormat, type.ToString()));
        }

        static internal Exception NameTooLong(string parameterName){ 
            return ADP.Argument(Res.GetString(Res.SqlMetaData_NameTooLong), parameterName);
        } 
 
        static internal Exception InvalidSortOrder(SortOrder order) {
            return ADP.InvalidEnumerationValue(typeof(SortOrder), (int)order); 
        }

        static internal Exception MustSpecifyBothSortOrderAndOrdinal(SortOrder order, int ordinal) {
            return ADP.InvalidOperation(Res.GetString(Res.SqlMetaData_SpecifyBothSortOrderAndOrdinal, order.ToString(), ordinal)); 
        }
 
        static internal Exception TableTypeCanOnlyBeParameter() { 
            return ADP.Argument(Res.GetString(Res.SQLTVP_TableTypeCanOnlyBeParameter));
        } 
        static internal Exception UnsupportedColumnTypeForSqlProvider(string columnName, string typeName) {
            return ADP.Argument(Res.GetString(Res.SqlProvider_InvalidDataColumnType, columnName, typeName));
        }
        static internal Exception InvalidColumnMaxLength(string columnName, long maxLength) { 
            return ADP.Argument(Res.GetString(Res.SqlProvider_InvalidDataColumnMaxLength, columnName, maxLength));
        } 
        static internal Exception InvalidColumnPrecScale() { 
            return ADP.Argument(Res.GetString(Res.SqlMisc_InvalidPrecScaleMessage));
        } 
        static internal Exception NotEnoughColumnsInStructuredType() {
            return ADP.Argument(Res.GetString(Res.SqlProvider_NotEnoughColumnsInStructuredType));
        }
        static internal Exception DuplicateSortOrdinal(int sortOrdinal) { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlProvider_DuplicateSortOrdinal, sortOrdinal));
        } 
        static internal Exception MissingSortOrdinal(int sortOrdinal) { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlProvider_MissingSortOrdinal, sortOrdinal));
        } 
        static internal Exception SortOrdinalGreaterThanFieldCount(int columnOrdinal, int sortOrdinal) {
            return ADP.InvalidOperation(Res.GetString(Res.SqlProvider_SortOrdinalGreaterThanFieldCount, sortOrdinal, columnOrdinal));
        }
        static internal Exception IEnumerableOfSqlDataRecordHasNoRows() { 
            return ADP.Argument(Res.GetString(Res.IEnumerableOfSqlDataRecordHasNoRows));
        } 
 

 
        //
        //  SqlPipe
        //
        static internal Exception SqlPipeCommandHookedUpToNonContextConnection() { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlPipe_CommandHookedUpToNonContextConnection));
        } 
 
        static internal Exception SqlPipeMessageTooLong( int messageLength ) {
            return ADP.Argument(Res.GetString(Res.SqlPipe_MessageTooLong, messageLength)); 
        }

        static internal Exception SqlPipeIsBusy() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlPipe_IsBusy)); 
        }
 
        static internal Exception SqlPipeAlreadyHasAnOpenResultSet( string methodName ) { 
            return ADP.InvalidOperation(Res.GetString(Res.SqlPipe_AlreadyHasAnOpenResultSet, methodName));
        } 

        static internal Exception SqlPipeDoesNotHaveAnOpenResultSet( string methodName ) {
            return ADP.InvalidOperation(Res.GetString(Res.SqlPipe_DoesNotHaveAnOpenResultSet, methodName));
        } 

        // 
        // : ISqlResultSet 
        //
        static internal Exception SqlResultSetClosed(string methodname) { 
            if (methodname == null) {
                return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetClosed2));
            }
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetClosed, methodname)); 
        }
        static internal Exception SqlResultSetNoData(string methodname) { 
            return ADP.InvalidOperation(Res.GetString(Res.ADP_DataReaderNoData, methodname)); 
        }
        static internal Exception SqlRecordReadOnly(string methodname) { 
            if (methodname == null) {
                return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlRecordReadOnly2));
            }
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlRecordReadOnly, methodname)); 
        }
 
        static internal Exception SqlResultSetRowDeleted(string methodname) { 
            if (methodname == null) {
                return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetRowDeleted2)); 
            }
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetRowDeleted, methodname));
        }
 
        static internal Exception SqlResultSetCommandNotInSameConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetCommandNotInSameConnection)); 
        } 

        static internal Exception SqlResultSetNoAcceptableCursor() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SqlResultSetNoAcceptableCursor));
        }

        // 
        // SQL.BulkLoad
        // 
        static internal Exception BulkLoadMappingInaccessible() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadMappingInaccessible));
        } 
        static internal Exception BulkLoadMappingsNamesOrOrdinalsOnly() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadMappingsNamesOrOrdinalsOnly));
        }
        static internal Exception BulkLoadCannotConvertValue(Type sourcetype, MetaType metatype, Exception e) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadCannotConvertValue, sourcetype.Name, metatype.TypeName), e);
        } 
        static internal Exception BulkLoadNonMatchingColumnMapping() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNonMatchingColumnMapping));
        } 
        static internal Exception BulkLoadNonMatchingColumnName(string columnName) {
            return BulkLoadNonMatchingColumnName(columnName, null);
        }
        static internal Exception BulkLoadNonMatchingColumnName(string columnName, Exception e) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNonMatchingColumnName, columnName), e);
        } 
        static internal Exception BulkLoadStringTooLong() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadStringTooLong));
        } 
        static internal Exception BulkLoadInvalidVariantValue() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadInvalidVariantValue));
        }
        static internal Exception BulkLoadInvalidTimeout(int timeout) { 
            return ADP.Argument(Res.GetString(Res.SQL_BulkLoadInvalidTimeout, timeout.ToString(CultureInfo.InvariantCulture)));
        } 
        static internal Exception BulkLoadExistingTransaction() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadExistingTransaction));
        } 
        static internal Exception BulkLoadNoCollation() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNoCollation));
        }
        static internal Exception BulkLoadConflictingTransactionOption() { 
            return ADP.Argument(Res.GetString(Res.SQL_BulkLoadConflictingTransactionOption));
        } 
        static internal Exception BulkLoadLcidMismatch(int sourceLcid, string sourceColumnName, int destinationLcid, string destinationColumnName) { 
            return ADP.InvalidOperation (Res.GetString (Res.Sql_BulkLoadLcidMismatch, sourceLcid, sourceColumnName, destinationLcid, destinationColumnName));
        } 
        static internal Exception InvalidOperationInsideEvent() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadInvalidOperationInsideEvent));
        }
        static internal Exception BulkLoadMissingDestinationTable() { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadMissingDestinationTable));
        } 
        static internal Exception BulkLoadInvalidDestinationTable(string tableName, Exception inner) { 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadInvalidDestinationTable, tableName), inner);
        } 
        static internal Exception BulkLoadBulkLoadNotAllowDBNull(string columnName) {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNotAllowDBNull, columnName));
        }
 
        //
        // transactions. 
        // 
        static internal Exception ConnectionDoomed() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ConnectionDoomed)); 
        }

        static internal readonly byte[] AttentionHeader = new byte[] {
            TdsEnums.MT_ATTN,               // Message Type 
            TdsEnums.ST_EOM,                // Status
            TdsEnums.HEADER_LEN >> 8,       // length - upper byte 
            TdsEnums.HEADER_LEN & 0xff,     // length - lower byte 
            0,                              // spid
            0,                              // spid 
            0,                              // packet (out of band)
            0                               // window
        };
 
        //
        // Merged Provider 
        // 
        static internal Exception BatchedUpdatesNotAvailableOnContextConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BatchedUpdatesNotAvailableOnContextConnection)); 
        }
        static internal Exception ContextAllowsLimitedKeywords() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ContextAllowsLimitedKeywords));
        } 
        static internal Exception ContextAllowsOnlyTypeSystem2005() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ContextAllowsOnlyTypeSystem2005)); 
        } 
        static internal Exception ContextConnectionIsInUse() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ContextConnectionIsInUse)); 
        }
        static internal Exception ContextUnavailableOutOfProc() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ContextUnavailableOutOfProc));
        } 
        static internal Exception ContextUnavailableWhileInProc() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ContextUnavailableWhileInProc)); 
        } 
        static internal Exception NestedTransactionScopesNotSupported() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_NestedTransactionScopesNotSupported)); 
        }
        static internal Exception NotAvailableOnContextConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_NotAvailableOnContextConnection));
        } 
        static internal Exception NotificationsNotAvailableOnContextConnection() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_NotificationsNotAvailableOnContextConnection)); 
        } 
        static internal Exception UnexpectedSmiEvent(Microsoft.SqlServer.Server.SmiEventSink_Default.UnexpectedEventType eventType) {
            Debug.Assert(false, "UnexpectedSmiEvent: "+eventType.ToString());    // Assert here, because these exceptions will most likely be eaten by the server. 
            return ADP.InvalidOperation(Res.GetString(Res.SQL_UnexpectedSmiEvent, (int)eventType));
        }
        static internal Exception UserInstanceNotAvailableInProc() {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_UserInstanceNotAvailableInProc)); 
        }
        static internal Exception ArgumentLengthMismatch( string arg1, string arg2 ) { 
            return ADP.Argument( Res.GetString( Res.SQL_ArgumentLengthMismatch, arg1, arg2 ) ); 
        }
        static internal Exception InvalidSqlDbTypeOneAllowedType( SqlDbType invalidType, string method, SqlDbType allowedType ) { 
            return ADP.Argument( Res.GetString( Res.SQL_InvalidSqlDbTypeWithOneAllowedType, invalidType, method, allowedType ) );
        }
        static internal Exception SqlPipeErrorRequiresSendEnd( ) {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_PipeErrorRequiresSendEnd)); 
        }
        static internal Exception TooManyValues(string arg) { 
            return ADP.Argument(Res.GetString(Res.SQL_TooManyValues), arg); 
        }
        static internal Exception StreamWriteNotSupported() { 
            return ADP.NotSupported(Res.GetString(Res.SQL_StreamWriteNotSupported));
        }
        static internal Exception StreamReadNotSupported() {
            return ADP.NotSupported(Res.GetString(Res.SQL_StreamReadNotSupported)); 
        }
        static internal Exception StreamSeekNotSupported() { 
            return ADP.NotSupported(Res.GetString(Res.SQL_StreamSeekNotSupported)); 
        }
        static internal System.Data.SqlTypes.SqlNullValueException SqlNullValue() { 
            System.Data.SqlTypes.SqlNullValueException e = new System.Data.SqlTypes.SqlNullValueException();
            ADP.TraceExceptionAsReturnValue(e);
            return e;
        } 
        //
 
        static internal Exception ParameterSizeRestrictionFailure(int index) { 
            return ADP.InvalidOperation(Res.GetString(Res.OleDb_CommandParameterError, index.ToString(CultureInfo.InvariantCulture), "SqlParameter.Size"));
        } 
        static internal Exception SubclassMustOverride() {
            return ADP.InvalidOperation(Res.GetString(Res.SqlMisc_SubclassMustOverride));
        }
 
        // BulkLoad
        internal const string WriteToServer = "WriteToServer"; 
 
        // Default values for SqlDependency and SqlNotificationRequest
        internal const int SqlDependencyTimeoutDefault = 0; 
        internal const int SqlDependencyServerTimeout  = 5 * 24 * 3600; // 5 days - used to compute default TTL of the dependency
        internal const string SqlNotificationServiceDefault         = "SqlQueryNotificationService";
        internal const string SqlNotificationStoredProcedureDefault = "SqlQueryNotificationStoredProcedure";
 
         // constant strings
        internal const string Transaction= "Transaction"; 
        internal const string Connection = "Connection"; 
    }
 
    sealed internal class SQLMessage {

        //
 
        private SQLMessage() { /* prevent utility class from being insantiated*/ }
 
        // The class SQLMessage defines the error messages that are specific to the SqlDataAdapter 
        // that are caused by a netlib error.  The functions will be called and then return the
        // appropriate error message from the resource Framework.txt.  The SqlDataAdapter will then 
        // take the error message and then create a SqlError for the message and then place
        // that into a SqlException that is either thrown to the user or cached for throwing at
        // a later time.  This class is used so that there will be compile time checking of error
        // messages.  The resource Framework.txt will ensure proper string text based on the appropriate 
        // locale.
 
        static internal string CultureIdError() { 
            return Res.GetString(Res.SQL_CultureIdError);
        } 
        static internal string EncryptionNotSupportedByClient() {
            return Res.GetString(Res.SQL_EncryptionNotSupportedByClient);
        }
        static internal string EncryptionNotSupportedByServer() { 
            return Res.GetString(Res.SQL_EncryptionNotSupportedByServer);
        } 
        static internal string OperationCancelled() { 
            return Res.GetString(Res.SQL_OperationCancelled);
        } 
        static internal string SevereError() {
            return Res.GetString(Res.SQL_SevereError);
        }
        static internal string SSPIInitializeError() { 
            return Res.GetString(Res.SQL_SSPIInitializeError);
        } 
        static internal string SSPIGenerateError() { 
            return Res.GetString(Res.SQL_SSPIGenerateError);
        } 
        static internal string Timeout() {
            return Res.GetString(Res.SQL_Timeout);
        }
        static internal string UserInstanceFailure() { 
            return Res.GetString(Res.SQL_UserInstanceFailure);
        } 
    } 
}//namespace

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
