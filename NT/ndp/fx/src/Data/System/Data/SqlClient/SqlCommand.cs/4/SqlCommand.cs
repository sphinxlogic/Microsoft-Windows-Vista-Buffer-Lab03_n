//------------------------------------------------------------------------------ 
// <copyright file="SqlCommand.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="true" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.SqlClient { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.Configuration.Assemblies;
    using System.Data; 
    using System.Data.Common; 
    using System.Data.ProviderBase;
    using System.Data.Sql; 
    using System.Data.SqlTypes;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO; 
    using System.Reflection;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.Serialization.Formatters;
    using System.Security.Permissions; 
    using System.Text;
    using System.Threading;
    using System.Xml;
 
    using Microsoft.SqlServer.Server;
 
    [ 
    DefaultEvent("RecordsAffected"),
    ToolboxItem(true), 
    Designer("Microsoft.VSDesigner.Data.VS.SqlCommandDesigner, " + AssemblyRef.MicrosoftVSDesigner)
    ]
#if WINFSInternalOnly
    internal 
#else
    public 
#endif 
    sealed class SqlCommand : DbCommand, ICloneable {
 
        private  static int     _objectTypeCount; // Bid counter
        internal readonly int   ObjectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);

        private string          _commandText; 
        private CommandType     _commandType;
        private int             _commandTimeout = ADP.DefaultCommandTimeout; 
        private UpdateRowSource _updatedRowSource = UpdateRowSource.Both; 
        private bool            _designTimeInvisible;
        internal SqlDependency  _sqlDep; 

        // devnote: Prepare
        // Against 7.0 Server (Sphinx) a prepare/unprepare requires an extra roundtrip to the server.
        // 
        // From 8.0 (Shiloh) and above (Yukon) the preparation can be done as part of the command execution.
        // 
        private enum EXECTYPE { 
            UNPREPARED,         // execute unprepared commands, all server versions (results in sp_execsql call)
            PREPAREPENDING,     // prepare and execute command, 8.0 and above only  (results in sp_prepexec call) 
            PREPARED,           // execute prepared commands, all server versions   (results in sp_exec call)
        }

        // devnotes 
        //
        // _hiddenPrepare 
        // On 8.0 and above the Prepared state cannot be left. Once a command is prepared it will always be prepared. 
        // A change in parameters, commandtext etc (IsDirty) automatically causes a hidden prepare
        // 
        // _inPrepare will be set immediately before the actual prepare is done.
        // The OnReturnValue function will test this flag to determine whether the returned value is a _prepareHandle or something else.
        //
        // _prepareHandle - the handle of a prepared command. Apparently there can be multiple prepared commands at a time - a feature that we do not support yet. 

        private bool _inPrepare         = false; 
        private int  _prepareHandle     = -1; 
        private bool _hiddenPrepare     = false;
 
        private SqlParameterCollection _parameters;
        private SqlConnection          _activeConnection;
        private bool                   _dirty            = false;               // true if the user changes the commandtext or number of parameters after the command is already prepared
        private EXECTYPE               _execType         = EXECTYPE.UNPREPARED; // by default, assume the user is not sharing a connection so the command has not been prepared 
        private _SqlRPC[]              _rpcArrayOf1      = null;                // Used for RPC executes
 
        // cut down on object creation and cache all these 
        // cached metadata
        private _SqlMetaDataSet _cachedMetaData; 

        // Cached info for async executions
        private class CachedAsyncState {
            private int           _cachedAsyncCloseCount = -1;    // value of the connection's CloseCount property when the asyncResult was set; tracks when connections are closed after an async operation 
            private DbAsyncResult _cachedAsyncResult     = null;
            private SqlConnection _cachedAsyncConnection = null;  // Used to validate that the connection hasn't changed when end the connection; 
            private SqlDataReader _cachedAsyncReader     = null; 
            private RunBehavior   _cachedRunBehavior     = RunBehavior.ReturnImmediately;
            private string        _cachedSetOptions      = null; 

            internal CachedAsyncState () {
            }
 
            internal SqlDataReader CachedAsyncReader {
                get {return _cachedAsyncReader;} 
            } 
            internal  RunBehavior CachedRunBehavior {
                get {return _cachedRunBehavior;} 
            }
            internal  string CachedSetOptions {
                get {return _cachedSetOptions;}
            } 
            internal bool PendingAsyncOperation {
                get {return (null != _cachedAsyncResult);} 
            } 

            internal bool IsActiveConnectionValid(SqlConnection activeConnection) { 
                return (_cachedAsyncConnection == activeConnection && _cachedAsyncCloseCount == activeConnection.CloseCount);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
            internal void ResetAsyncState() {
                _cachedAsyncCloseCount = -1; 
                _cachedAsyncResult     = null; 
                if (_cachedAsyncConnection != null) {
                    _cachedAsyncConnection.AsycCommandInProgress = false; 
                    _cachedAsyncConnection = null;
                }
                _cachedAsyncReader     = null;
                _cachedRunBehavior     = RunBehavior.ReturnImmediately; 
                _cachedSetOptions      = null;
            } 
 
            internal void SetActiveConnectionAndResult(DbAsyncResult result, SqlConnection activeConnection) {
                _cachedAsyncCloseCount = activeConnection.CloseCount; 
                _cachedAsyncResult     = result;
                if (null != activeConnection && !activeConnection.Parser.MARSOn) {
                    if (activeConnection.AsycCommandInProgress)
                        throw SQL.MARSUnspportedOnConnection(); 
                }
                Debug.Assert(activeConnection != null, "Unexpected null connection argument on SetActiveConnectionAndResult!"); 
                _cachedAsyncConnection = activeConnection; 

                // Should only be needed for non-MARS, but set anyways. 
                _cachedAsyncConnection.AsycCommandInProgress = true;
            }

            internal void SetAsyncReaderState (SqlDataReader ds, RunBehavior runBehavior, string optionSettings) { 
                _cachedAsyncReader  = ds;
                _cachedRunBehavior  = runBehavior; 
                _cachedSetOptions   = optionSettings; 
            }
        } 

        CachedAsyncState _cachedAsyncState = null;

        private CachedAsyncState cachedAsyncState { 
            get {
                if (_cachedAsyncState == null) { 
                    _cachedAsyncState = new CachedAsyncState (); 
                }
                return  _cachedAsyncState; 
            }
        }

        // sql reader will pull this value out for each NextResult call.  It is not cumulative 
        // _rowsAffected is cumulative for ExecuteNonQuery across all rpc batches
        internal int _rowsAffected = -1; // rows affected by the command 
 
        private SqlNotificationRequest _notification;
        private bool _notificationAutoEnlist = true;            // Notifications auto enlistment is turned on by default 

        // transaction support
        private SqlTransaction _transaction;
 
        private StatementCompletedEventHandler _statementCompletedEventHandler;
 
        private TdsParserStateObject _stateObj; // this is the TDS session we're using. 

        // Volatile bool used to synchronize with cancel thread the state change of an executing 
        // command going from pre-processing to obtaining a stateObject.  The cancel synchronization
        // we require in the command is only from entering an Execute* API to obtaining a
        // stateObj.  Once a stateObj is successfully obtained, cancel synchronization is handled
        // by the stateObject. 
        private volatile bool _pendingCancel;
 
        private bool _batchRPCMode; 
        private List<_SqlRPC> _RPCList;
        private _SqlRPC[] _SqlRPCBatchArray; 
        private List<SqlParameterCollection>  _parameterCollectionList;
        private int     _currentlyExecutingBatch;

        // 
        //  Smi execution-specific stuff
        // 
        sealed private class CommandEventSink : SmiEventSink_Default { 
            private SqlCommand _command;
 
            internal CommandEventSink( SqlCommand command ) : base( ) {
                _command = command;
            }
 
            internal override void StatementCompleted( int rowsAffected ) {
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlCommand.CommandEventSink.StatementCompleted|ADV> %d#, rowsAffected=%d.\n", _command.ObjectID, rowsAffected); 
                }
                _command.InternalRecordsAffected = rowsAffected; 

//

 

 
            } 

            internal override void BatchCompleted() { 
                if (Bid.AdvancedOn) {
                    Bid.Trace("<sc.SqlCommand.CommandEventSink.BatchCompleted|ADV> %d#.\n", _command.ObjectID);
                }
            } 

            internal override void ParametersAvailable( SmiParameterMetaData[] metaData, ITypedGettersV3 parameterValues ) { 
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlCommand.CommandEventSink.ParametersAvailable|ADV> %d# metaData.Length=%d.\n", _command.ObjectID, (null!=metaData)?metaData.Length:-1);
 
                    if (null != metaData) {
                        for (int i=0; i < metaData.Length; i++) {
                            Bid.Trace("<sc.SqlCommand.CommandEventSink.ParametersAvailable|ADV> %d#, metaData[%d] is %s%s\n",
                                        _command.ObjectID, i, metaData[i].GetType().ToString(), metaData[i].TraceString()); 
                        }
                    } 
                } 
                Debug.Assert(SmiContextFactory.Instance.NegotiatedSmiVersion >= SmiContextFactory.YukonVersion);
                _command.OnParametersAvailableSmi( metaData, parameterValues ); 
            }

            internal override void ParameterAvailable(SmiParameterMetaData metaData, SmiTypedGetterSetter parameterValues, int ordinal)
            { 
                if (Bid.AdvancedOn) {
                    if (null != metaData) { 
                        Bid.Trace("<sc.SqlCommand.CommandEventSink.ParameterAvailable|ADV> %d#, metaData[%d] is %s%s\n", 
                                    _command.ObjectID, ordinal, metaData.GetType().ToString(), metaData.TraceString());
                    } 
                }
                Debug.Assert(SmiContextFactory.Instance.NegotiatedSmiVersion >= SmiContextFactory.KatmaiVersion);
                _command.OnParameterAvailableSmi(metaData, parameterValues, ordinal);
            } 
        }
 
        private SmiRequestExecutor      _smiRequest; 
        private SmiContext              _smiRequestContext; // context that _smiRequest came from
        private CommandEventSink _smiEventSink; 
        private SmiEventSink_DeferedProcessing _outParamEventSink;
        private CommandEventSink EventSink {
            get {
                if ( null == _smiEventSink ) { 
                    _smiEventSink = new CommandEventSink( this );
                } 
 
                _smiEventSink.Parent = InternalSmiConnection.CurrentEventSink;
                return _smiEventSink; 
            }
        }

        private SmiEventSink_DeferedProcessing OutParamEventSink { 
            get {
                if (null == _outParamEventSink) { 
                    _outParamEventSink = new SmiEventSink_DeferedProcessing(EventSink); 
                }
                else { 
                    _outParamEventSink.Parent = EventSink;
                }

                return _outParamEventSink; 
            }
        } 
 

        public SqlCommand() : base() { 
            GC.SuppressFinalize(this);
        }

        public SqlCommand(string cmdText) : this() { 
            CommandText = cmdText;
        } 
 
        public SqlCommand(string cmdText, SqlConnection connection) : this() {
            CommandText = cmdText; 
            Connection = connection;
        }

        public SqlCommand(string cmdText, SqlConnection connection, SqlTransaction transaction) : this() { 
            CommandText = cmdText;
            Connection = connection; 
            Transaction = transaction; 
        }
 
        private SqlCommand(SqlCommand from) : this() { // Clone
            CommandText = from.CommandText;
            CommandTimeout = from.CommandTimeout;
            CommandType = from.CommandType; 
            Connection = from.Connection;
            DesignTimeVisible = from.DesignTimeVisible; 
            Transaction = from.Transaction; 
            UpdatedRowSource = from.UpdatedRowSource;
 
            SqlParameterCollection parameters = Parameters;
            foreach(object parameter in from.Parameters) {
                parameters.Add((parameter is ICloneable) ? (parameter as ICloneable).Clone() : parameter);
            } 
        }
 
        [ 
        DefaultValue(null),
        Editor("Microsoft.VSDesigner.Data.Design.DbConnectionEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_Connection),
        ]
        new public SqlConnection Connection { 
            get {
                return _activeConnection; 
            } 
            set {
                // Don't allow the connection to be changed while in a async opperation. 
                if (_activeConnection != value && _activeConnection != null) { // If new value...
                    if (cachedAsyncState.PendingAsyncOperation) { // If in pending async state, throw.
                        throw SQL.CannotModifyPropertyAsyncOperationInProgress(SQL.Connection);
                    } 
                }
                // Check to see if the currently set transaction has completed.  If so, 
                // null out our local reference. 
                if (null != _transaction && _transaction.Connection == null)
                    _transaction = null; 
                _activeConnection = value; //
                Bid.Trace("<sc.SqlCommand.set_Connection|API> %d#, %d#\n", ObjectID, ((null != value) ? value.ObjectID : -1));
            }
        } 

        override protected DbConnection DbConnection { // V1.2.3300 
            get { 
                return Connection;
            } 
            set {
                Connection = (SqlConnection)value;
            }
        } 

        private SqlInternalConnectionSmi InternalSmiConnection { 
            get { 
                return (SqlInternalConnectionSmi)_activeConnection.InnerConnection;
            } 
        }

        private SqlInternalConnectionTds InternalTdsConnection {
            get { 
                return (SqlInternalConnectionTds)_activeConnection.InnerConnection;
            } 
        } 

        private bool IsShiloh { 
            get {
                Debug.Assert(_activeConnection != null, "The active connection is null!");
                if (_activeConnection == null)
                    return false; 
                return _activeConnection.IsShiloh;
            } 
        } 

        [ 
        DefaultValue(true),
        ResCategoryAttribute(Res.DataCategory_Notification),
        ResDescriptionAttribute(Res.SqlCommand_NotificationAutoEnlist),
        ] 
        public bool NotificationAutoEnlist {
            get { 
                return _notificationAutoEnlist; 
            }
            set { 
                _notificationAutoEnlist = value;
            }
         }
 
        [
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), // MDAC 90471 
        ResCategoryAttribute(Res.DataCategory_Notification),
        ResDescriptionAttribute(Res.SqlCommand_Notification), 
        ]
        public SqlNotificationRequest Notification {
            get {
                return _notification; 
            }
            set { 
                Bid.Trace("<sc.SqlCommand.set_Notification|API> %d#\n", ObjectID); 
                _sqlDep = null;
                _notification = value; 
            }
        }

 
        internal SqlStatistics Statistics {
            get { 
                if (null != _activeConnection) { 
                    if (_activeConnection.StatisticsEnabled) {
                        return _activeConnection.Statistics; 
                    }
                }
                return null;
            } 
        }
 
        [ 
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ResDescriptionAttribute(Res.DbCommand_Transaction),
        ]
        new public SqlTransaction Transaction {
            get { 
                // if the transaction object has been zombied, just return null
                if ((null != _transaction) && (null == _transaction.Connection)) { // MDAC 72720 
                    _transaction = null; 
                }
                return _transaction; 
            }
            set {
                // Don't allow the transaction to be changed while in a async opperation.
                if (_transaction != value && _activeConnection != null) { // If new value... 
                    if (cachedAsyncState.PendingAsyncOperation) { // If in pending async state, throw
                        throw SQL.CannotModifyPropertyAsyncOperationInProgress(SQL.Transaction); 
                    } 
                }
 
                //
                Bid.Trace("<sc.SqlCommand.set_Transaction|API> %d#\n", ObjectID);
                _transaction = value;
            } 
        }
 
        override protected DbTransaction DbTransaction { // V1.2.3300 
            get {
                return Transaction; 
            }
            set {
                Transaction = (SqlTransaction)value;
            } 
        }
 
        [ 
        DefaultValue(""),
        Editor("Microsoft.VSDesigner.Data.SQL.Design.SqlCommandTextEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing), 
        RefreshProperties(RefreshProperties.All), // MDAC 67707
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_CommandText),
        ] 
        override public string CommandText { // V1.2.3300, XXXCommand V1.0.5000
            get { 
                string value = _commandText; 
                return ((null != value) ? value : ADP.StrEmpty);
            } 
            set {
                if (Bid.TraceOn) {
                    Bid.Trace("<sc.SqlCommand.set_CommandText|API> %d#, '", ObjectID);
                    Bid.PutStr(value); // Use PutStr to write out entire string 
                    Bid.Trace("'\n");
                } 
                if (0 != ADP.SrcCompare(_commandText, value)) { 
                    PropertyChanging();
                    _commandText = value; 
                }
            }
        }
 
        [
        ResCategoryAttribute(Res.DataCategory_Data), 
        ResDescriptionAttribute(Res.DbCommand_CommandTimeout), 
        ]
        override public int CommandTimeout { // V1.2.3300, XXXCommand V1.0.5000 
            get {
                return _commandTimeout;
            }
            set { 
                Bid.Trace("<sc.SqlCommand.set_CommandTimeout|API> %d#, %d\n", ObjectID, value);
                if (value < 0) { 
                    throw ADP.InvalidCommandTimeout(value); 
                }
                if (value != _commandTimeout) { 
                    PropertyChanging();
                    _commandTimeout = value;
                }
            } 
        }
 
        public void ResetCommandTimeout() { // V1.2.3300 
            if (ADP.DefaultCommandTimeout != _commandTimeout) {
                PropertyChanging(); 
                _commandTimeout = ADP.DefaultCommandTimeout;
            }
        }
 
        private bool ShouldSerializeCommandTimeout() { // V1.2.3300
            return (ADP.DefaultCommandTimeout != _commandTimeout); 
        } 

        [ 
        DefaultValue(System.Data.CommandType.Text),
        RefreshProperties(RefreshProperties.All),
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_CommandType), 
        ]
        override public CommandType CommandType { // V1.2.3300, XXXCommand V1.0.5000 
            get { 
                CommandType cmdType = _commandType;
                return ((0 != cmdType) ? cmdType : CommandType.Text); 
            }
            set {
                Bid.Trace("<sc.SqlCommand.set_CommandType|API> %d#, %d{ds.CommandType}\n", ObjectID, (int)value);
                if (_commandType != value) { 
                    switch(value) { // @perfnote: Enum.IsDefined
                    case CommandType.Text: 
                    case CommandType.StoredProcedure: 
                        PropertyChanging();
                        _commandType = value; 
                        break;
                    case System.Data.CommandType.TableDirect:
                        throw SQL.NotSupportedCommandType(value);
                    default: 
                        throw ADP.InvalidCommandType(value);
                    } 
                } 
            }
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
        public override bool DesignTimeVisible { // V1.2.3300, XXXCommand V1.0.5000
            get { 
                return !_designTimeInvisible;
            } 
            set { 
                _designTimeInvisible = !value;
                TypeDescriptor.Refresh(this); // VS7 208845 
            }
        }

        [ 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        ResCategoryAttribute(Res.DataCategory_Data), 
        ResDescriptionAttribute(Res.DbCommand_Parameters), 
        ]
        new public SqlParameterCollection Parameters { 
            get {
                if (null == this._parameters) {
                    // delay the creation of the SqlParameterCollection
                    // until user actually uses the Parameters property 
                    this._parameters = new SqlParameterCollection();
                } 
                return this._parameters; 
            }
        } 

        override protected DbParameterCollection DbParameterCollection { // V1.2.3300
            get {
                return Parameters; 
            }
        } 
 
        [
        DefaultValue(System.Data.UpdateRowSource.Both), 
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbCommand_UpdatedRowSource),
        ]
        override public UpdateRowSource UpdatedRowSource { // V1.2.3300, XXXCommand V1.0.5000 
            get {
                return _updatedRowSource; 
            } 
            set {
                switch(value) { // @perfnote: Enum.IsDefined 
                case UpdateRowSource.None:
                case UpdateRowSource.OutputParameters:
                case UpdateRowSource.FirstReturnedRecord:
                case UpdateRowSource.Both: 
                    _updatedRowSource = value;
                    break; 
                default: 
                    throw ADP.InvalidUpdateRowSource(value);
                } 
            }
        }

        [ 
        ResCategoryAttribute(Res.DataCategory_StatementCompleted),
        ResDescriptionAttribute(Res.DbCommand_StatementCompleted), 
        ] 
        public event StatementCompletedEventHandler StatementCompleted {
            add { 
                _statementCompletedEventHandler += value;
            }
            remove {
                _statementCompletedEventHandler -= value; 
            }
        } 
 
        internal void OnStatementCompleted(int recordCount) { // V1.2.3300
             if (0 <= recordCount) { 
                StatementCompletedEventHandler handler = _statementCompletedEventHandler;
                if (null != handler) {
                    try {
                       Bid.Trace("<sc.SqlCommand.OnStatementCompleted|INFO> %d#, recordCount=%d\n", ObjectID, recordCount); 
                        handler(this, new StatementCompletedEventArgs(recordCount));
                    } 
                    catch(Exception e) { 
                        //
                        if (!ADP.IsCatchableOrSecurityExceptionType(e)) { 
                            throw;
                        }

                        ADP.TraceExceptionWithoutRethrow(e); 
                    }
                } 
            } 
        }
 
        private void PropertyChanging() { // also called from SqlParameterCollection
            this.IsDirty = true;
        }
 
        override public void Prepare() {
            SqlConnection.ExecutePermission.Demand(); 
 
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject. 
            _pendingCancel = false;

            // Context connection's prepare is a no-op
            if (_activeConnection.IsContextConnection) { 
                return;
            } 
 
            SqlStatistics statistics = null;
            IntPtr hscp; 
            SqlDataReader r = null;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.Prepare|API> %d#", ObjectID);
            statistics = SqlStatistics.StartTimer(Statistics);
 
            // only prepare if batch with parameters
            // MDAC 
            if ( 
                this.IsPrepared && !this.IsDirty
                || (this.CommandType == CommandType.StoredProcedure) 
                ||  (
                        (System.Data.CommandType.Text == this.CommandType)
                        && (0 == GetParameterCount (_parameters))
                    ) 

            ) { 
                if (null != Statistics) { 
                    Statistics.SafeIncrement (ref Statistics._prepares);
                } 
                _hiddenPrepare = false;
            }
            else {
                bool processFinallyBlock = true; 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    ValidateCommand(ADP.Prepare, false /*not async*/); 

                    GetStateObject(); 

                    // Loop through parameters ensuring that we do not have unspecified types, sizes, scales, or precisions
                    if (null != _parameters) {
                        int count = _parameters.Count; 
                        for (int i = 0; i < count; ++i) {
                            _parameters[i].Prepare(this); // MDAC 67063 
                        } 
                    }
 
#if DEBUG
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { 
                        Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                        r = InternalPrepare(0); 
#if DEBUG
                    } 
                    finally {
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                    }
#endif //DEBUG 
                }
                catch (System.OutOfMemoryException e) { 
                    processFinallyBlock = false; 
                    _activeConnection.Abort(e);
                    throw; 
                }
                catch (System.StackOverflowException e) {
                    processFinallyBlock = false;
                    _activeConnection.Abort(e); 
                    throw;
                } 
                catch (System.Threading.ThreadAbortException e)  { 
                    processFinallyBlock = false;
                    _activeConnection.Abort(e); 
                    throw;
                }
                catch (Exception e) {
                    processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                    throw;
                } 
                finally { 
                    if (processFinallyBlock) {
                        _hiddenPrepare = false; // The command is now officially prepared 

                        if (r != null) {
                            _cachedMetaData = r.MetaData;
                            r.Close(); 
                        }
                        PutStateObject(); 
                    } 
                }
            } 

            SqlStatistics.StopTimer(statistics);
            Bid.ScopeLeave(ref hscp);
        } 

        private SqlDataReader InternalPrepare(CommandBehavior behavior) { 
            SqlDataReader r = null; 

            if (this.IsDirty) { 
                Debug.Assert(_cachedMetaData == null, "dirty query should not have cached metadata!");
                //
                // someone changed the command text or the parameter schema so we must unprepare the command
                // 
                this.Unprepare(false);
                this.IsDirty = false; 
            } 
            Debug.Assert(_execType != EXECTYPE.PREPARED, "Invalid attempt to Prepare already Prepared command!");
            Debug.Assert(_activeConnection != null, "must have an open connection to Prepare"); 
            Debug.Assert(null != _stateObj, "TdsParserStateObject should not be null");
            Debug.Assert(null != _stateObj.Parser, "TdsParser class should not be null in Command.Execute!");
            Debug.Assert(_stateObj.Parser == _activeConnection.Parser, "stateobject parser not same as connection parser");
            Debug.Assert(false == _inPrepare, "Already in Prepare cycle, this.inPrepare should be false!"); 

            if (_activeConnection.IsShiloh) { 
                // In Shiloh, remember that the user wants to do a prepare 
                // but don't actually do an rpc
                _execType = EXECTYPE.PREPAREPENDING; 

                // return null results
            }
            else { 
                _SqlRPC rpc = BuildPrepare(behavior);
                _inPrepare = true; 
                Debug.Assert(_activeConnection.State == ConnectionState.Open, "activeConnection must be open!"); 
                r = new SqlDataReader(this, behavior);
                try { 
                    Debug.Assert(_rpcArrayOf1[0] == rpc);
                    _stateObj.Parser.TdsExecuteRPC(_rpcArrayOf1, this.CommandTimeout, false, null, _stateObj, CommandType.StoredProcedure == CommandType);
                    _stateObj.Parser.Run(RunBehavior.UntilDone, this, r, null, _stateObj);
                } 
                catch {
                    // In case Prepare fails, cleanup and then throw. 
                    _inPrepare = false; 
                    throw;
                } 

                r.Bind(_stateObj);
                Debug.Assert(-1 != _prepareHandle, "Handle was not filled in!");
                _execType = EXECTYPE.PREPARED; 
               Bid.Trace("<sc.SqlCommand.Prepare|INFO> %d#, Command prepared.\n", ObjectID);
            } 
 
            if (null != Statistics) {
                Statistics.SafeIncrement(ref Statistics._prepares); 
            }

            // let the connection know that it needs to unprepare the command on close
            _activeConnection.AddPreparedCommand(this); 
            return r;
        } 
 
        // SqlInternalConnectionTds needs to be able to unprepare a statement
        internal void Unprepare(bool isClosing) { 
            // Context connection's prepare is a no-op
            if (_activeConnection.IsContextConnection) {
                return;
            } 

            bool obtainedStateObj = false; 
            bool processFinallyBlock = true; 
            try {
                if (null == _stateObj) { 
                    GetStateObject();
                    obtainedStateObj = true;
                }
                InternalUnprepare(isClosing); 
            }
            catch (Exception e) { 
                processFinallyBlock = ADP.IsCatchableExceptionType (e); 
                throw;
            } 
            finally {
                Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to Unprepare");  // you need to setup for a thread abort somewhere before you call this method
                if (processFinallyBlock && obtainedStateObj) {
                    PutStateObject(); 
                }
            } 
        } 

        private void InternalUnprepare(bool isClosing) { 
            Debug.Assert(true == IsPrepared, "Invalid attempt to Unprepare a non-prepared command!");
            Debug.Assert(_activeConnection != null, "must have an open connection to UnPrepare");
            Debug.Assert(null != _stateObj, "TdsParserStateObject should not be null");
            Debug.Assert(null != _stateObj.Parser, "TdsParser class should not be null in Command.Unprepare!"); 
            Debug.Assert(_stateObj.Parser == _activeConnection.Parser, "stateobject parser not same as connection parser");
            Debug.Assert(false == _inPrepare, "_inPrepare should be false!"); 
 
            // In 7.0, unprepare always.  In 7.x, only unprepare if the connection is closing since sp_prepexec will
            // unprepare the last used handle 
            if (IsShiloh) {

                // @devnote: we're always falling back to Prepare pending
                // @devnote: This seems broken because once the command is prepared it will - always - be a 
                // @devnote: prepared execution.
                // @devnote: Even replacing the parameterlist with something completely different or 
                // @devnote: changing the commandtext to a non-parameterized query will result in prepared execution 
                // @devnote:
                // @devnote: We need to keep the behavior for backward compatibility though (non-breaking change) 
                //
                _execType = EXECTYPE.PREPAREPENDING;
                // @devnote:  Don't zero out the handle because we'll pass it in to sp_prepexec on the
                // @devnote:  next prepare, unless closing the connection when the server will drop the handle anyway. 
                if (isClosing) {
                    // reset our handle 
                    _prepareHandle = -1; 
                }
            } 
            else {
                if (_prepareHandle != -1) {
                    _SqlRPC rpc = BuildUnprepare();
                    Debug.Assert(_rpcArrayOf1[0] == rpc); 
                    _stateObj.Parser.TdsExecuteRPC(_rpcArrayOf1, this.CommandTimeout, false, null, _stateObj, CommandType.StoredProcedure == CommandType);
                    _stateObj.Parser.Run(RunBehavior.UntilDone, this, null, null, _stateObj); 
 
                    // reset our handle
                    _prepareHandle = -1; 
                }
                // reset our execType since nothing is prepared
                _execType = EXECTYPE.UNPREPARED;
            } 

            _cachedMetaData = null; 
            if (!isClosing) {   // if isClosing, the connection will remove the command 
                _activeConnection.RemovePreparedCommand(this);
            } 
            Bid.Trace("<sc.SqlCommand.Prepare|INFO> %d#, Command unprepared.\n", ObjectID);
        }

 
        // Cancel is supposed to be multi-thread safe.
        // It doesn't make sense to verify the connection exists or that it is open during cancel 
        // because immediately after checkin the connection can be closed or removed via another thread. 
        //
        override public void Cancel() { 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.Cancel|API> %d#", ObjectID);

            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
 
                // the pending data flag means that we are awaiting a response or are in the middle of proccessing a response
                // if we have no pending data, then there is nothing to cancel 
                // if we have pending data, but it is not a result of this command, then we don't cancel either.  Note that
                // this model is implementable because we only allow one active command at any one time.  This code
                // will have to change we allow multiple outstanding batches
 
                //
                if (null == _activeConnection) { 
                    return; 
                }
                SqlInternalConnectionTds connection = (_activeConnection.InnerConnection as SqlInternalConnectionTds); 
                if (null == connection) {  // Fail with out locking
                     return;
                }
 
                // The lock here is to protect against the command.cancel / connection.close race condition
                // The SqlInternalConnectionTds is set to OpenBusy during close, once this happens the cast below will fail and 
                // the command will no longer be cancelable.  It might be desirable to be able to cancel the close opperation, but this is 
                // outside of the scope of Whidbey RTM.  See (SqlConnection::Close) for other lock.
                lock (connection) { 
                    if (connection != (_activeConnection.InnerConnection as SqlInternalConnectionTds)) { // make sure the connection held on the active connection is what we have stored in our temp connection variable, if not between getting "connection" and takeing the lock, the connection has been closed
                        return;
                    }
 
                    TdsParser parser = connection.Parser;
                    if (null == parser) { 
                        return; 
                    }
 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try {
#if DEBUG
                        object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                        RuntimeHelpers.PrepareConstrainedRegions(); 
                        try { 
                            Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                            if (!_pendingCancel) { // Do nothing if aleady pending.
                                // Before attempting actual cancel, set the _pendingCancel flag to false.
                                // This denotes to other thread before obtaining stateObject from the
                                // session pool that there is another thread wishing to cancel. 
                                // The period in question is between entering the ExecuteAPI and obtaining
                                // a stateObject. 
                                _pendingCancel = true; 

                                TdsParserStateObject stateObj = _stateObj; 
                                if (null != _stateObj) {
                                    _stateObj.Cancel(ObjectID);
                                }
                                else { 
                                    SqlDataReader reader = connection.FindLiveReader(this);
                                    if (reader != null) { 
                                        reader.Cancel(ObjectID); 
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
                        _activeConnection.Abort(e);
                        throw;
                    }
                    catch (System.StackOverflowException e) { 
                        _activeConnection.Abort(e);
                        throw; 
                    } 
                    catch (System.Threading.ThreadAbortException e)  {
                        _activeConnection.Abort(e); 
                        throw;
                    }
                }
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp); 
            }
        } 

        new public SqlParameter CreateParameter() {
            return new SqlParameter();
        } 

        override protected DbParameter CreateDbParameter() { 
            return CreateParameter(); 
        }
 
        override protected void Dispose(bool disposing) {
            if (disposing) { // release mananged objects

                // V1.0, V1.1 did not reset the Connection, Parameters, CommandText, WebData 100524 
                //_parameters = null;
                //_activeConnection = null; 
                //_statistics = null; 
                //CommandText = null;
                _cachedMetaData = null; 
            }
            // release unmanaged objects
            base.Dispose(disposing);
        } 

        override public object ExecuteScalar() { 
            SqlConnection.ExecutePermission.Demand(); 

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state 
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            SqlStatistics statistics = null; 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteScalar|API> %d#", ObjectID); 
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                SqlDataReader ds = RunExecuteReader(0, RunBehavior.ReturnImmediately, true, ADP.ExecuteScalar); 
                return CompleteExecuteScalar(ds, false);
            }
            finally {
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp);
            } 
        } 

        private object CompleteExecuteScalar(SqlDataReader ds, bool returnSqlValue) { 
            object retResult = null;

            try {
                if (ds.Read()) { 
                    if (ds.FieldCount > 0) {
                        if (returnSqlValue) { 
                            retResult = ds.GetSqlValue(0); 
                        }
                        else { 
                            retResult = ds.GetValue(0);
                        }
                    }
                } 
            }
            finally { 
                // clean off the wire 
                ds.Close();
            } 

            return retResult;
        }
 
        override public int ExecuteNonQuery() {
            SqlConnection.ExecutePermission.Demand(); 
 
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject. 
            _pendingCancel = false;

            SqlStatistics statistics = null;
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteNonQuery|API> %d#", ObjectID);
            try { 
                statistics = SqlStatistics.StartTimer(Statistics); 
                return InternalExecuteNonQuery(null, ADP.ExecuteNonQuery, false);
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp);
            } 
        }
 
        // Handles in-proc execute-to-pipe functionality 
        //  Identical to ExecuteNonQuery
        internal void ExecuteToPipe( SmiContext pipeContext ) { 
            SqlConnection.ExecutePermission.Demand();

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject. 
            _pendingCancel = false;
 
            SqlStatistics statistics = null; 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteToPipe|INFO> %d#", ObjectID); 
            try {
                statistics = SqlStatistics.StartTimer(Statistics);
                InternalExecuteNonQuery(null, ADP.ExecuteNonQuery, true);
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp); 
            }
        } 

        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteNonQuery() {
            // BeginExecuteNonQuery will track ExecutionTime for us 
            return BeginExecuteNonQuery(null, null);
        } 
 
        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteNonQuery(AsyncCallback callback, object stateObject) { 
            SqlConnection.ExecutePermission.Demand();

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject. 
            _pendingCancel = false;
 
            ValidateAsyncCommand(); // Special case - done outside of try/catches to prevent putting a stateObj 
                                    // back into pool when we should not.
 
            SqlStatistics statistics = null;
            try {
                statistics = SqlStatistics.StartTimer(Statistics);
                ExecutionContext execContext = (callback == null) ? null : ExecutionContext.Capture(); 
                DbAsyncResult result = new DbAsyncResult(this, ADP.EndExecuteNonQuery, callback, stateObject, execContext);
 
                try { // InternalExecuteNonQuery already has reliability block, but if failure will not put stateObj back into pool. 
                    InternalExecuteNonQuery(result, ADP.BeginExecuteNonQuery, false);
                } 
                catch (Exception e) {
                    if (!ADP.IsCatchableOrSecurityExceptionType(e)) {
                        // If not catchable - the connection has already been caught and doomed in RunExecuteReader.
                        throw; 
                    }
 
                    // For async, RunExecuteReader will never put the stateObj back into the pool, so do so now. 
                    PutStateObject();
                    throw; 
                }

                // Read SNI does not have catches for async exceptions, handle here.
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
#if DEBUG 
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try {
                        Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                        // must finish caching information before ReadSni which can activate the callback before returning 
                        cachedAsyncState.SetActiveConnectionAndResult(result, _activeConnection);
                        _stateObj.ReadSni(result, _stateObj); 
#if DEBUG 
                    }
                    finally { 
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                    }
#endif //DEBUG
                } 
                catch (System.OutOfMemoryException e) {
                    _activeConnection.Abort(e); 
                    throw; 
                }
                catch (System.StackOverflowException e) { 
                    _activeConnection.Abort(e);
                    throw;
                }
                catch (System.Threading.ThreadAbortException e)  { 
                    _activeConnection.Abort(e);
                    throw; 
                } 
                catch (Exception) {
                    // Similarly, if an exception occurs put the stateObj back into the pool. 
                    // and reset async cache information to allow a second async execute
                    if (null != _cachedAsyncState) {
                        _cachedAsyncState.ResetAsyncState();
                    } 
                    PutStateObject();
                    throw; 
                } 
                return result;
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        private void VerifyEndExecuteState(DbAsyncResult dbAsyncResult, String endMethod) { 
            if (null == dbAsyncResult) { 
                throw ADP.ArgumentNull("asyncResult");
            } 
            if (dbAsyncResult.EndMethodName != endMethod) {
                throw ADP.MismatchedAsyncResult(dbAsyncResult.EndMethodName, endMethod);
            }
            if (!cachedAsyncState.IsActiveConnectionValid(_activeConnection)) { 
                throw ADP.CommandAsyncOperationCompleted();
            } 
 
            dbAsyncResult.CompareExchangeOwner(this, endMethod);
        } 

        private void WaitForAsyncResults(IAsyncResult asyncResult) {
            DbAsyncResult dbAsyncResult = (DbAsyncResult) asyncResult;
            if (!asyncResult.IsCompleted) { 
                asyncResult.AsyncWaitHandle.WaitOne();
            } 
            dbAsyncResult.Reset(); 
            _activeConnection.GetOpenTdsConnection().DecrementAsyncCount();
        } 

        public int EndExecuteNonQuery(IAsyncResult asyncResult) {
            SqlStatistics statistics = null;
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    statistics = SqlStatistics.StartTimer(Statistics);
                    VerifyEndExecuteState((DbAsyncResult) asyncResult, ADP.EndExecuteNonQuery); 
                    WaitForAsyncResults(asyncResult); 

                    bool processFinallyBlock = true; 
                    try {
                        NotifyDependency();
                        CheckThrowSNIException();
 
                        // only send over SQL Batch command if we are not a stored proc and have no parameters
                        if ((System.Data.CommandType.Text == this.CommandType) && (0 == GetParameterCount(_parameters))) { 
                            try { 
                                _stateObj.Parser.Run(RunBehavior.UntilDone, this, null, null, _stateObj);
                            } 
                            finally {
                                cachedAsyncState.ResetAsyncState();
                            }
                        } 
                        else  { // otherwise, use full-blown execute which can handle params and stored procs
                            SqlDataReader reader = CompleteAsyncExecuteReader(); 
                            if (null != reader) { 
                                reader.Close();
                            } 
                        }
                    }
                    catch (Exception e) {
                        processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                        throw;
                    } 
                    finally { 
                        if (processFinallyBlock) {
                            PutStateObject(); 
                        }
                    }

                    Debug.Assert(null == _stateObj, "non-null state object in EndExecuteNonQuery"); 
                    return _rowsAffected;
#if DEBUG 
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG
            }
            catch (System.OutOfMemoryException e) { 
                _activeConnection.Abort(e);
                throw; 
            } 
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e); 
                throw;
            }
            catch (System.Threading.ThreadAbortException e)  {
                _activeConnection.Abort(e); 
                throw;
            } 
            finally { 
                SqlStatistics.StopTimer(statistics);
            } 
        }

        private int InternalExecuteNonQuery(DbAsyncResult result, string methodName, bool sendToPipe) {
            bool async = (null != result); 

            SqlStatistics statistics = Statistics; 
            _rowsAffected = -1; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    // @devnote: this function may throw for an invalid connection 
                    // @devnote: returns false for empty command text
                    ValidateCommand(methodName, null != result);
                    CheckNotificationStateAndAutoEnlist(); // Only call after validate - requires non null connection!
 
                    // only send over SQL Batch command if we are not a stored proc and have no parameters and not in batch RPC mode
                    if ( _activeConnection.IsContextConnection ) { 
                        if (null != statistics) { 
                            statistics.SafeIncrement(ref statistics._unpreparedExecs);
                        } 

                        RunExecuteNonQuerySmi( sendToPipe );
                    }
                    else if (!BatchRPCMode && (System.Data.CommandType.Text == this.CommandType) && (0 == GetParameterCount(_parameters))) { 
                        Debug.Assert( !sendToPipe, "trying to send non-context command to pipe" );
                        if (null != statistics) { 
                            if (!this.IsDirty && this.IsPrepared) { 
                                statistics.SafeIncrement(ref statistics._preparedExecs);
                            } 
                            else {
                                statistics.SafeIncrement(ref statistics._unpreparedExecs);
                            }
                        } 

                        RunExecuteNonQueryTds(methodName, async); 
                    } 
                    else  { // otherwise, use full-blown execute which can handle params and stored procs
                        Debug.Assert( !sendToPipe, "trying to send non-context command to pipe" ); 
                        Bid.Trace("<sc.SqlCommand.ExecuteNonQuery|INFO> %d#, Command executed as RPC.\n", ObjectID);
                        SqlDataReader reader = RunExecuteReader(0, RunBehavior.UntilDone, false, methodName, result);
                        if (null != reader) {
                            reader.Close(); 
                        }
                    } 
                    Debug.Assert(async || null == _stateObj, "non-null state object in InternalExecuteNonQuery"); 
                    return _rowsAffected;
#if DEBUG 
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) { 
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _activeConnection.Abort(e); 
                throw;
            } 
        }

        public XmlReader ExecuteXmlReader() {
            SqlConnection.ExecutePermission.Demand(); 

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state 
            // between entry into Execute* API and the thread obtaining the stateObject. 
            _pendingCancel = false;
 
            SqlStatistics statistics = null;
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteXmlReader|API> %d#", ObjectID);
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
 
                // use the reader to consume metadata 
                SqlDataReader ds = RunExecuteReader(CommandBehavior.SequentialAccess, RunBehavior.ReturnImmediately, true, ADP.ExecuteXmlReader);
                return CompleteXmlReader(ds); 
            }
            finally {
                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteXmlReader() { 
            // BeginExecuteXmlReader will track executiontime
            return BeginExecuteXmlReader(null, null);
        }
 
        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteXmlReader(AsyncCallback callback, object stateObject) { 
            SqlConnection.ExecutePermission.Demand(); 

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state 
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            ValidateAsyncCommand(); // Special case - done outside of try/catches to prevent putting a stateObj 
                                    // back into pool when we should not.
 
            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                ExecutionContext execContext = (callback == null) ? null : ExecutionContext.Capture();
                DbAsyncResult result = new DbAsyncResult(this, ADP.EndExecuteXmlReader, callback, stateObject, execContext);

                try { // InternalExecuteNonQuery already has reliability block, but if failure will not put stateObj back into pool. 
                    RunExecuteReader(CommandBehavior.SequentialAccess, RunBehavior.ReturnImmediately, true, ADP.BeginExecuteXmlReader, result);
                } 
                catch (Exception e) { 
                    if (!ADP.IsCatchableOrSecurityExceptionType(e)) {
                        // If not catchable - the connection has already been caught and doomed in RunExecuteReader. 
                        throw;
                    }

                    // For async, RunExecuteReader will never put the stateObj back into the pool, so do so now. 
                    PutStateObject();
                    throw; 
                } 

                // Read SNI does not have catches for async exceptions, handle here. 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
#if DEBUG
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try { 
                        Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                        // must finish caching information before ReadSni which can activate the callback before returning
                        cachedAsyncState.SetActiveConnectionAndResult(result, _activeConnection);
                        _stateObj.ReadSni(result, _stateObj);
#if DEBUG 
                    }
                    finally { 
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                    }
#endif //DEBUG 
                }
                catch (System.OutOfMemoryException e) {
                    _activeConnection.Abort(e);
                    throw; 
                }
                catch (System.StackOverflowException e) { 
                    _activeConnection.Abort(e); 
                    throw;
                } 
                catch (System.Threading.ThreadAbortException e)  {
                    _activeConnection.Abort(e);
                    throw;
                } 
                catch (Exception) {
                    // Similarly, if an exception occurs put the stateObj back into the pool. 
                    // and reset async cache information to allow a second async execute 
                    if (null != _cachedAsyncState) {
                        _cachedAsyncState.ResetAsyncState(); 
                    }
                    PutStateObject();
                    throw;
                } 
                return result;
            } 
            finally { 
                SqlStatistics.StopTimer(statistics);
            } 
        }

        public XmlReader EndExecuteXmlReader(IAsyncResult asyncResult) {
            return CompleteXmlReader(InternalEndExecuteReader(asyncResult, ADP.EndExecuteXmlReader)); 
        }
 
        private XmlReader CompleteXmlReader(SqlDataReader ds) { 
            XmlReader xr = null;
 
            SmiExtendedMetaData[] md = ds.GetInternalSmiMetaData();
            bool isXmlCapable = (null != md && md.Length == 1 && (md[0].SqlDbType == SqlDbType.NText
                                                         || md[0].SqlDbType == SqlDbType.NVarChar
                                                         || md[0].SqlDbType == SqlDbType.Xml)); 

            if (isXmlCapable) { 
                try { 
                    SqlStream sqlBuf = new SqlStream(ds, true /*addByteOrderMark*/, (md[0].SqlDbType == SqlDbType.Xml) ? false : true /*process all rows*/);
                    xr = sqlBuf.ToXmlReader(); 
                }
                catch (Exception e) {
                    if (ADP.IsCatchableExceptionType(e)) {
                        ds.Close(); 
                    }
                    throw; 
                } 
            }
            if (xr == null) { 
                ds.Close();
                throw SQL.NonXmlResult();
            }
            return xr; 
        }
 
        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)] 
        public IAsyncResult BeginExecuteReader() {
            return BeginExecuteReader(null, null, CommandBehavior.Default); 
        }

        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteReader(AsyncCallback callback, object stateObject) { 
            return BeginExecuteReader(callback, stateObject, CommandBehavior.Default);
        } 
 
        override protected DbDataReader ExecuteDbDataReader(CommandBehavior behavior) {
            return ExecuteReader(behavior, ADP.ExecuteReader); 
        }

        new public SqlDataReader ExecuteReader() {
            SqlStatistics statistics = null; 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteReader|API> %d#", ObjectID); 
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                return ExecuteReader(CommandBehavior.Default, ADP.ExecuteReader); 
            }
            finally {
                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
        new public SqlDataReader ExecuteReader(CommandBehavior behavior) {
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteReader|API> %d#, behavior=%d{ds.CommandBehavior}", ObjectID, (int)behavior);
            try {
                return ExecuteReader(behavior, ADP.ExecuteReader);
            } 
            finally {
                Bid.ScopeLeave(ref hscp); 
            } 
        }
 
        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteReader(CommandBehavior behavior) {
            return BeginExecuteReader(null, null, behavior);
        } 

        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)] 
        public IAsyncResult BeginExecuteReader(AsyncCallback callback, object stateObject, CommandBehavior behavior) { 
            SqlConnection.ExecutePermission.Demand();
 
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;
 
            SqlStatistics statistics = null;
            try { 
                statistics = SqlStatistics.StartTimer(Statistics); 
                return InternalBeginExecuteReader(callback, stateObject, behavior);
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        internal SqlDataReader ExecuteReader(CommandBehavior behavior, string method) { 
            SqlConnection.ExecutePermission.Demand(); // 

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state 
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            SqlStatistics statistics = null; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    statistics = SqlStatistics.StartTimer(Statistics); 
                    SqlDataReader reader = RunExecuteReader(behavior, RunBehavior.ReturnImmediately, true, method); 
                    return reader;
#if DEBUG 
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) { 
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _activeConnection.Abort(e); 
                throw;
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        public SqlDataReader EndExecuteReader(IAsyncResult asyncResult) { 
            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                return InternalEndExecuteReader(asyncResult, ADP.EndExecuteReader);
            }
            finally {
                SqlStatistics.StopTimer(statistics); 
            }
        } 
 
        private IAsyncResult InternalBeginExecuteReader(AsyncCallback callback, object stateObject, CommandBehavior behavior) {
            ExecutionContext execContext = (callback == null) ? null : ExecutionContext.Capture(); 
            DbAsyncResult result = new DbAsyncResult(this, ADP.EndExecuteReader, callback, stateObject, execContext);

            ValidateAsyncCommand(); // Special case - done outside of try/catches to prevent putting a stateObj
                                    // back into pool when we should not. 

            try { // InternalExecuteNonQuery already has reliability block, but if failure will not put stateObj back into pool. 
                RunExecuteReader(behavior, RunBehavior.ReturnImmediately, true, ADP.BeginExecuteReader, result); 
            }
            catch (Exception e) { 
                if (!ADP.IsCatchableOrSecurityExceptionType(e)) {
                    // If not catchable - the connection has already been caught and doomed in RunExecuteReader.
                    throw;
                } 

                // For async, RunExecuteReader will never put the stateObj back into the pool, so do so now. 
                PutStateObject(); 
                throw;
            } 

            // Read SNI does not have catches for async exceptions, handle here.
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                    // must finish caching information before ReadSni which can activate the callback before returning
                    cachedAsyncState.SetActiveConnectionAndResult(result, _activeConnection); 
                    _stateObj.ReadSni(result, _stateObj);
#if DEBUG 
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG
            }
            catch (System.OutOfMemoryException e) { 
                _activeConnection.Abort(e);
                throw; 
            } 
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e); 
                throw;
            }
            catch (System.Threading.ThreadAbortException e)  {
                _activeConnection.Abort(e); 
                throw;
            } 
            catch (Exception) { 
                // Similarly, if an exception occurs put the stateObj back into the pool.
                // and reset async cache information to allow a second async execute 
                if (null != _cachedAsyncState) {
                    _cachedAsyncState.ResetAsyncState();
                }
                PutStateObject(); 
                throw;
            } 
 
            return result;
        } 

        private SqlDataReader InternalEndExecuteReader(IAsyncResult asyncResult, string endMethod) {

            VerifyEndExecuteState((DbAsyncResult) asyncResult, endMethod); 
            WaitForAsyncResults(asyncResult);
 
            CheckThrowSNIException(); 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    SqlDataReader reader = CompleteAsyncExecuteReader(); 
                    Debug.Assert(null == _stateObj, "non-null state object in InternalEndExecuteReader");
                    return reader;
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _activeConnection.Abort(e);
                throw;
            } 
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e); 
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _activeConnection.Abort(e);
                throw;
            }
        } 

        // If the user part is quoted, remove first and last brackets and then unquote any right square 
        // brackets in the procedure.  This is a very simple parser that performs no validation.  As 
        // with the function below, ideally we should have support from the server for this.
        private static string UnquoteProcedurePart(string part) { 
            if ((null != part) && (2 <= part.Length)) {
                if ('[' == part[0] && ']' == part[part.Length-1]) {
                    part = part.Substring(1, part.Length-2); // strip outer '[' & ']'
                    part = part.Replace("]]", "]"); // undo quoted "]" from "]]" to "]" 
                }
            } 
            return part; 
        }
 
        // User value in this format: [server].[database].[schema].[sp_foo];1
        // This function should only be passed "[sp_foo];1".
        // This function uses a pretty simple parser that doesn't do any validation.
        // Ideally, we would have support from the server rather than us having to do this. 
        private static string UnquoteProcedureName(string name, out object groupNumber) {
            groupNumber  = null; // Out param - initialize value to no value. 
            string sproc = name; 

            if (null != sproc) { 
                if (Char.IsDigit(sproc[sproc.Length-1])) { // If last char is a digit, parse.
                    int semicolon = sproc.LastIndexOf(';');
                    if (semicolon != -1) { // If we found a semicolon, obtain the integer.
                        string part   = sproc.Substring(semicolon+1); 
                        int    number = 0;
                        if (Int32.TryParse(part, out number)) { // No checking, just fail if this doesn't work. 
                            groupNumber = number; 
                            sproc = sproc.Substring(0, semicolon);
                        } 
                    }
                }
                sproc = UnquoteProcedurePart(sproc);
            } 
            return sproc;
        } 
 
        //index into indirection arrays for columns of interest to DeriveParameters
        private enum ProcParamsColIndex { 
            ParameterName = 0,
            ParameterType,
            DataType,                  // obsolete in katmai, use ManagedDataType instead
            ManagedDataType,          // new in katmai 
            CharacterMaximumLength,
            NumericPrecision, 
            NumericScale, 
            TypeCatalogName,
            TypeSchemaName, 
            TypeName,
            XmlSchemaCollectionCatalogName,
            XmlSchemaCollectionSchemaName,
            XmlSchemaCollectionName, 
            UdtTypeName,                // obsolete in Katmai.  Holds the actual typename if UDT, since TypeName didn't back then.
            DateTimeScale               // new in Katmai 
        }; 

        // Yukon- column ordinals (this array indexed by ProcParamsColIndex 
        static readonly internal string[] PreKatmaiProcParamsNames = new string[] {
            "PARAMETER_NAME",           // ParameterName,
            "PARAMETER_TYPE",           // ParameterType,
            "DATA_TYPE",                // DataType 
            null,                       // ManagedDataType,     introduced in Katmai
            "CHARACTER_MAXIMUM_LENGTH", // CharacterMaximumLength, 
            "NUMERIC_PRECISION",        // NumericPrecision, 
            "NUMERIC_SCALE",            // NumericScale,
            "UDT_CATALOG",              // TypeCatalogName, 
            "UDT_SCHEMA",               // TypeSchemaName,
            "TYPE_NAME",                // TypeName,
            "XML_CATALOGNAME",          // XmlSchemaCollectionCatalogName,
            "XML_SCHEMANAME",           // XmlSchemaCollectionSchemaName, 
            "XML_SCHEMACOLLECTIONNAME", // XmlSchemaCollectionName
            "UDT_NAME",                 // UdtTypeName 
            null,                       // Scale for datetime types with scale, introduced in Katmai 
        };
 
        // Katmai+ column ordinals (this array indexed by ProcParamsColIndex
        static readonly internal string[] KatmaiProcParamsNames = new string[] {
            "PARAMETER_NAME",           // ParameterName,
            "PARAMETER_TYPE",           // ParameterType, 
            null,                       // DataType, removed from Katmai+
            "MANAGED_DATA_TYPE",        // ManagedDataType, 
            "CHARACTER_MAXIMUM_LENGTH", // CharacterMaximumLength, 
            "NUMERIC_PRECISION",        // NumericPrecision,
            "NUMERIC_SCALE",            // NumericScale, 
            "TYPE_CATALOG_NAME",        // TypeCatalogName,
            "TYPE_SCHEMA_NAME",         // TypeSchemaName,
            "TYPE_NAME",                // TypeName,
            "XML_CATALOGNAME",          // XmlSchemaCollectionCatalogName, 
            "XML_SCHEMANAME",           // XmlSchemaCollectionSchemaName,
            "XML_SCHEMACOLLECTIONNAME", // XmlSchemaCollectionName 
            null,                       // UdtTypeName, removed from Katmai+ 
            "SS_DATETIME_PRECISION",    // Scale for datetime types with scale
        }; 


        internal void DeriveParameters() {
            switch (this.CommandType) { 
                case System.Data.CommandType.Text:
                    throw ADP.DeriveParametersNotSupported(this); 
                case System.Data.CommandType.StoredProcedure: 
                    break;
                case System.Data.CommandType.TableDirect: 
                    // CommandType.TableDirect - do nothing, parameters are not supported
                    throw ADP.DeriveParametersNotSupported(this);
                default:
                    throw ADP.InvalidCommandType(this.CommandType); 
            }
 
            // validate that we have a valid connection 
            ValidateCommand(ADP.DeriveParameters, false /*not async*/);
 
            // Use common parser for SqlClient and OleDb - parse into 4 parts - Server, Catalog, Schema, ProcedureName
            string[] parsedSProc = MultipartIdentifier.ParseMultipartIdentifier(this.CommandText, "[\"", "]\"", Res.SQL_SqlCommandCommandText, false);
            if (null == parsedSProc[3] || ADP.IsEmpty(parsedSProc[3]))
            { 
                throw ADP.NoStoredProcedureExists(this.CommandText);
            } 
 
            Debug.Assert(parsedSProc.Length == 4, "Invalid array length result from SqlCommandBuilder.ParseProcedureName");
 
            SqlCommand    paramsCmd = null;
            StringBuilder cmdText   = new StringBuilder();

            // Build call for sp_procedure_params_rowset built of unquoted values from user: 
            // [user server, if provided].[user catalog, else current database].[sys if Yukon, else blank].[sp_procedure_params_rowset]
 
            // Server - pass only if user provided. 
            if (!ADP.IsEmpty(parsedSProc[0])) {
                SqlCommandSet.BuildStoredProcedureName(cmdText, parsedSProc[0]); 
                cmdText.Append(".");
            }

            // Catalog - pass user provided, otherwise use current database. 
            if (ADP.IsEmpty(parsedSProc[1])) {
                parsedSProc[1] = this.Connection.Database; 
            } 
            SqlCommandSet.BuildStoredProcedureName(cmdText, parsedSProc[1]);
            cmdText.Append("."); 

            // Schema - only if Yukon, and then only pass sys.  Also - pass managed version of sproc
            // for Yukon, else older sproc.
            string[] colNames; 
            bool useManagedDataType;
            if (this.Connection.IsKatmaiOrNewer) { 
                // Procedure - [sp_procedure_params_managed] 
                cmdText.Append("[sys].[").Append(TdsEnums.SP_PARAMS_MGD10).Append("]");
 
                colNames = KatmaiProcParamsNames;
                useManagedDataType = true;
            }
            else { 
                if (this.Connection.IsYukonOrNewer) {
                    // Procedure - [sp_procedure_params_managed] 
                    cmdText.Append("[sys].[").Append(TdsEnums.SP_PARAMS_MANAGED).Append("]"); 
                }
                else { 
                    // Procedure - [sp_procedure_params_rowset]
                    cmdText.Append(".[").Append(TdsEnums.SP_PARAMS).Append("]");
                }
 
                colNames = PreKatmaiProcParamsNames;
                useManagedDataType = false; 
            } 

 
            paramsCmd = new SqlCommand(cmdText.ToString(), this.Connection, this.Transaction);
            paramsCmd.CommandType = CommandType.StoredProcedure;

            object groupNumber; 

            // Prepare parameters for sp_procedure_params_rowset: 
            // 1) procedure name - unquote user value 
            // 2) group number - parsed at the time we unquoted procedure name
            // 3) procedure schema - unquote user value 

            //

 

            paramsCmd.Parameters.Add(new SqlParameter("@procedure_name", SqlDbType.NVarChar, 255)); 
            paramsCmd.Parameters[0].Value = UnquoteProcedureName(parsedSProc[3], out groupNumber); // ProcedureName is 4rd element in parsed array 

            if (null != groupNumber) { 
                SqlParameter param = paramsCmd.Parameters.Add(new SqlParameter("@group_number", SqlDbType.Int));
                param.Value = groupNumber;
            }
 
            if (!ADP.IsEmpty(parsedSProc[2])) { // SchemaName is 3rd element in parsed array
                SqlParameter param = paramsCmd.Parameters.Add(new SqlParameter("@procedure_schema", SqlDbType.NVarChar, 255)); 
                param.Value = UnquoteProcedurePart(parsedSProc[2]); 
            }
 
            SqlDataReader r = null;

            List<SqlParameter> parameters = new List<SqlParameter>();
            bool processFinallyBlock = true; 

            try { 
                r = paramsCmd.ExecuteReader(); 

                SqlParameter p = null; 

                while (r.Read()) {
                    // each row corresponds to a parameter of the stored proc.  Fill in all the info
 
                    p = new SqlParameter();
 
                    // name 
                    p.ParameterName = (string) r[colNames[(int)ProcParamsColIndex.ParameterName]];
 
                    // type
                    if (useManagedDataType) {
                        p.SqlDbType = (SqlDbType)(short)r[colNames[(int)ProcParamsColIndex.ManagedDataType]];
 
                        // Yukon didn't have as accurate of information as we're getting for Katmai, so re-map a couple of
                        //  types for backward compatability. 
                        switch (p.SqlDbType) { 
                            case SqlDbType.Image:
                            case SqlDbType.Timestamp: 
                                p.SqlDbType = SqlDbType.VarBinary;
                                break;

                            case SqlDbType.NText: 
                                p.SqlDbType = SqlDbType.NVarChar;
                                break; 
 
                            case SqlDbType.Text:
                                p.SqlDbType = SqlDbType.VarChar; 
                                break;

                            default:
                                break; 
                        }
                    } 
                    else { 
                        p.SqlDbType = MetaType.GetSqlDbTypeFromOleDbType((short)r[colNames[(int)ProcParamsColIndex.DataType]],
                            ADP.IsNull(r[colNames[(int)ProcParamsColIndex.TypeName]]) ? 
                                ADP.StrEmpty :
                                (string)r[colNames[(int)ProcParamsColIndex.TypeName]]);
                    }
 
                    // size
                    object a = r[colNames[(int)ProcParamsColIndex.CharacterMaximumLength]]; 
                    if (a is int) { 
                        int size = (int)a;
 
                        // Map MAX sizes correctly.  The Katmai server-side proc sends 0 for these instead of -1.
                        //  Should be fixed on the Katmai side, but would likely hold up the RI, and is safer to fix here.
                        //  If we can get the server-side fixed before shipping Katmai, we can remove this mapping.
                        if (0 == size && 
                                (p.SqlDbType == SqlDbType.NVarChar ||
                                 p.SqlDbType == SqlDbType.VarBinary || 
                                 p.SqlDbType == SqlDbType.VarChar)) { 
                            size = -1;
                        } 
                        p.Size = size;
                    }

                    // direction 
                    p.Direction = ParameterDirectionFromOleDbDirection((short)r[colNames[(int)ProcParamsColIndex.ParameterType]]);
 
                    if (p.SqlDbType == SqlDbType.Decimal) { 
                        p.ScaleInternal = (byte) ((short)r[colNames[(int)ProcParamsColIndex.NumericScale]] & 0xff);
                        p.PrecisionInternal = (byte)((short)r[colNames[(int)ProcParamsColIndex.NumericPrecision]] & 0xff); 
                    }

                    // type name for Udt
                    if (SqlDbType.Udt == p.SqlDbType) { 

                        Debug.Assert(this._activeConnection.IsYukonOrNewer,"Invalid datatype token received from pre-yukon server"); 
 
                        string udtTypeName;
                        if (useManagedDataType) { 
                            udtTypeName = (string)r[colNames[(int)ProcParamsColIndex.TypeName]];
                        }
                        else {
                            udtTypeName = (string)r[colNames[(int)ProcParamsColIndex.UdtTypeName]]; 
                        }
 
                        //read the type name 
                        p.UdtTypeName = r[colNames[(int)ProcParamsColIndex.TypeCatalogName]]+"."+
                            r[colNames[(int)ProcParamsColIndex.TypeSchemaName]]+"."+ 
                            udtTypeName;
                    }

                    // type name for Structured types (same as for Udt's except assign p.TypeName instead of p.UdtTypeName 
                    if (SqlDbType.Structured == p.SqlDbType) {
 
                        Debug.Assert(this._activeConnection.IsKatmaiOrNewer,"Invalid datatype token received from pre-katmai server"); 

                        //read the type name 
                        p.TypeName = r[colNames[(int)ProcParamsColIndex.TypeCatalogName]]+"."+
                            r[colNames[(int)ProcParamsColIndex.TypeSchemaName]]+"."+
                            r[colNames[(int)ProcParamsColIndex.TypeName]];
                    } 

                    // XmlSchema name for Xml types 
                    if (SqlDbType.Xml == p.SqlDbType) { 
                        object value;
 
                        value = r[colNames[(int)ProcParamsColIndex.XmlSchemaCollectionCatalogName]];
                        p.XmlSchemaCollectionDatabase = ADP.IsNull(value) ? String.Empty : (string) value;

                        value = r[colNames[(int)ProcParamsColIndex.XmlSchemaCollectionSchemaName]]; 
                        p.XmlSchemaCollectionOwningSchema = ADP.IsNull(value) ? String.Empty : (string) value;
 
                        value = r[colNames[(int)ProcParamsColIndex.XmlSchemaCollectionName]]; 
                        p.XmlSchemaCollectionName = ADP.IsNull(value) ? String.Empty : (string) value;
                    } 

                    if (MetaType._IsVarTime(p.SqlDbType)) {
                        object value = r[colNames[(int)ProcParamsColIndex.DateTimeScale]];
                        if (value is int) { 
                            p.ScaleInternal = (byte)(((int)value) & 0xff);
                        } 
                    } 

                    parameters.Add(p); 
                }
            }
            catch (Exception e) {
                processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                throw;
            } 
            finally { 
                Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to DeriveParameters");  // you need to setup for a thread abort somewhere before you call this method
                if (processFinallyBlock) { 
                    if (null != r)
                        r.Close();

                    // always unhook the user's connection 
                    paramsCmd.Connection = null;
                } 
            } 

            if (parameters.Count == 0) { 
                throw ADP.NoStoredProcedureExists(this.CommandText);
            }

            this.Parameters.Clear(); 

            foreach (SqlParameter temp in parameters) { 
                this._parameters.Add(temp); 
            }
        } 

        private ParameterDirection ParameterDirectionFromOleDbDirection(short oledbDirection) {
            Debug.Assert(oledbDirection >= 1 && oledbDirection <= 4, "invalid parameter direction from params_rowset!");
 
            switch (oledbDirection) {
                case 2: 
                    return ParameterDirection.InputOutput; 
                case 3:
                    return ParameterDirection.Output; 
                case 4:
                    return ParameterDirection.ReturnValue;
                default:
                    return ParameterDirection.Input; 
            }
 
        } 

        // get cached metadata 
        internal _SqlMetaDataSet MetaData {
            get {
                return _cachedMetaData;
            } 
        }
 
        // Check to see if notificactions auto enlistment is turned on. Enlist if so. 
        private void CheckNotificationStateAndAutoEnlist() {
            // First, if auto-enlist is on, check server version and then obtain context if 
            // present.  If so, auto enlist to the dependency ID given in the context data.
            if (NotificationAutoEnlist) {
                if (_activeConnection.IsYukonOrNewer) { // Only supported for Yukon...
                    string notifyContext = SqlNotificationContext(); 
                    if (!ADP.IsEmpty(notifyContext)) {
                        // Map to dependency by ID set in context data. 
                        SqlDependency dependency = SqlDependencyPerAppDomainDispatcher.SingletonInstance.LookupDependencyEntry(notifyContext); 

                        if (null != dependency) { 
                            // Add this command to the dependency.
                            dependency.AddCommandDependency(this);
                        }
                    } 
                }
            } 
 
            // If we have a notification with a dependency, setup the notification options at this time.
 
            // If user passes options, then we will always have option data at the time the SqlDependency
            // ctor is called.  But, if we are using default queue, then we do not have this data until
            // Start().  Due to this, we always delay setting options until execute.
 
            // There is a variance in order between Start(), SqlDependency(), and Execute.  This is the
            // best way to solve that problem. 
            if (null != Notification) { 
                if (_sqlDep != null) {
                    if (null == _sqlDep.Options) { 
                        // If null, SqlDependency was not created with options, so we need to obtain default options now.
                        // GetDefaultOptions can and will throw under certain conditions.

                        // In order to match to the appropriate start - we need 3 pieces of info: 
                        // 1) server 2) user identity (SQL Auth or Int Sec) 3) database
 
                        SqlDependency.IdentityUserNamePair identityUserName = null; 

                        // Obtain identity from connection. 
                        SqlInternalConnectionTds internalConnection = _activeConnection.InnerConnection as SqlInternalConnectionTds;
                        if (internalConnection.Identity != null) {
                            identityUserName = new SqlDependency.IdentityUserNamePair(internalConnection.Identity, null);
                        } 
                        else {
                            identityUserName = new SqlDependency.IdentityUserNamePair(null, internalConnection.ConnectionOptions.UserID); 
                        } 

                        Notification.Options = SqlDependency.GetDefaultComposedOptions(_activeConnection.DataSource, 
                                                             InternalTdsConnection.ServerProvidedFailOverPartner,
                                                             identityUserName, _activeConnection.Database);
                    }
 
                    // Set UserData on notifications, as well as adding to the appdomain dispatcher.  The value is
                    // computed by an algorithm on the dependency - fixed and will always produce the same value 
                    // given identical commandtext + parameter values. 
                    Notification.UserData = _sqlDep.ComputeHashAndAddToDispatcher(this);
                    // Maintain server list for SqlDependency. 
                    _sqlDep.AddToServerList(_activeConnection.DataSource);
                }
            }
        } 

        [System.Security.Permissions.SecurityPermission(SecurityAction.Assert, Infrastructure=true)] 
        static internal string SqlNotificationContext() { 
            SqlConnection.VerifyExecutePermission();
 
            // since this information is protected, follow it so that it is not exposed to the user.
            //
            return (System.Runtime.Remoting.Messaging.CallContext.GetData("MS.SqlDependencyCookie") as string);
        } 

        // Tds-specific logic for ExecuteNonQuery run handling 
        private void RunExecuteNonQueryTds(string methodName, bool async) { 
            bool processFinallyBlock = true;
            try { 
                GetStateObject();

                // we just send over the raw text with no annotation
                // no parameters are sent over 
                // no data reader is returned
                // use this overload for "batch SQL" tds token type 
                Bid.Trace("<sc.SqlCommand.ExecuteNonQuery|INFO> %d#, Command executed as SQLBATCH.\n", ObjectID); 
                _stateObj.Parser.TdsExecuteSQLBatch(this.CommandText, this.CommandTimeout, this.Notification, _stateObj);
 
                NotifyDependency();
                if (async) {
                    _activeConnection.GetOpenTdsConnection(methodName).IncrementAsyncCount();
                } 
                else {
                    _stateObj.Parser.Run(RunBehavior.UntilDone, this, null, null, _stateObj); 
                } 
            }
            catch (Exception e) { 
                processFinallyBlock = ADP.IsCatchableExceptionType(e);
                throw;
            }
            finally { 
                Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to RunExecuteNonQueryTds");  // you need to setup for a thread abort somewhere before you call this method
                if (processFinallyBlock && !async) { 
                    // When executing Async, we need to keep the _stateObj alive... 
                    PutStateObject();
                } 
            }
        }

        // Smi-specific logic for ExecuteNonQuery 
        private void RunExecuteNonQuerySmi( bool sendToPipe ) {
            SqlInternalConnectionSmi innerConnection = InternalSmiConnection; 
 
            try {
                // Set it up, process all of the events, and we're done! 
                SetUpSmiRequest( innerConnection );

                long transactionId = (null != innerConnection.CurrentTransaction) ? innerConnection.CurrentTransaction.TransactionId : 0;
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlCommand.RunExecuteNonQuerySmi|ADV> %d#, innerConnection=%d#, transactionId=0x%I64x, cmdBehavior=%d.\n", ObjectID, innerConnection.ObjectID, transactionId, (int)CommandBehavior.Default);
                } 
 
                SmiExecuteType execType;
                if ( sendToPipe ) 
                    execType = SmiExecuteType.ToPipe;
                else
                    execType = SmiExecuteType.NonQuery;
 
                SmiEventStream eventStream = null;
                // Don't need a CER here because caller already has one that will doom the 
                //  connection if it's a finally-skipping type of problem. 
                bool processFinallyBlock = true;
                try { 
                    if (SmiContextFactory.Instance.NegotiatedSmiVersion >= SmiContextFactory.KatmaiVersion) {
                        eventStream = _smiRequest.Execute(
                                                                            innerConnection.SmiConnection,
                                                                            transactionId, 
                                                                            innerConnection.InternalEnlistedTransaction,
                                                                            CommandBehavior.Default, 
                                                                            execType); 
                    }
                    else { 
                        eventStream = _smiRequest.Execute(
                                                                            innerConnection.SmiConnection,
                                                                            transactionId,
                                                                            CommandBehavior.Default, 
                                                                            execType);
                    } 
 
                    while ( eventStream.HasEvents ) {
                        eventStream.ProcessEvent( EventSink ); 
                    }
                }
                catch (Exception e) {
                    processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                    throw;
                } 
                finally { 
                    Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to RunExecuteNonQuerySmi");  // you need to setup for a thread abort somewhere before you call this method
                    if (null != eventStream && processFinallyBlock) { 
                        eventStream.Close( EventSink );
                    }
                }
 
                EventSink.ProcessMessagesAndThrow();
            } 
            catch { 
                DisposeSmiRequest();
 
                throw;
            }
        }
 
        internal SqlDataReader RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream, string method) {
            return RunExecuteReader(cmdBehavior, runBehavior, returnStream, method, null); 
        } 

        internal SqlDataReader RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream, string method, DbAsyncResult result) { 
            bool async = (null != result);

            _rowsAffected = -1;
 
            if (0 != (CommandBehavior.SingleRow & cmdBehavior)) {
                // CommandBehavior.SingleRow implies CommandBehavior.SingleResult 
                cmdBehavior |= CommandBehavior.SingleResult; 
            }
 
            // @devnote: this function may throw for an invalid connection
            // @devnote: returns false for empty command text
            ValidateCommand(method, null != result);
            CheckNotificationStateAndAutoEnlist(); // Only call after validate - requires non null connection! 

            // This section needs to occur AFTER ValidateCommand - otherwise it will AV without a connection. 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    SqlStatistics statistics = Statistics; 
                    if (null != statistics) {
                        if ((!this.IsDirty && this.IsPrepared && !_hiddenPrepare) 
                            || (this.IsPrepared && _execType == EXECTYPE.PREPAREPENDING))
                        {
                            statistics.SafeIncrement(ref statistics._preparedExecs);
                        } 
                        else {
                            statistics.SafeIncrement(ref statistics._unpreparedExecs); 
                        } 
                    }
 

                    if ( _activeConnection.IsContextConnection ) {
                        return RunExecuteReaderSmi( cmdBehavior, runBehavior, returnStream );
                    } 
                    else {
                        return RunExecuteReaderTds( cmdBehavior, runBehavior, returnStream, async ); 
                    } 

#if DEBUG 
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) { 
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _activeConnection.Abort(e); 
                throw;
            } 
        }

        private SqlDataReader RunExecuteReaderTds( CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream, bool async ) {
            // make sure we have good parameter information 
            // prepare the command
            // execute 
            Debug.Assert(null != _activeConnection.Parser, "TdsParser class should not be null in Command.Execute!"); 

            bool inSchema =  (0 != (cmdBehavior & CommandBehavior.SchemaOnly)); 
            SqlDataReader ds = null;

            // create a new RPC
            _SqlRPC rpc=null; 

            string optionSettings = null; 
            bool processFinallyBlock = true; 

            try { 
                GetStateObject();

                if (BatchRPCMode) {
                    Debug.Assert(inSchema == false, "Batch RPC does not support schema only command beahvior"); 
                    Debug.Assert(!IsPrepared, "Batch RPC should not be prepared!");
                    Debug.Assert(!IsDirty, "Batch RPC should not be marked as dirty!"); 
                    //Currently returnStream is always false, but we may want to return a Reader later. 
                    //if (returnStream) {
                    //    Bid.Trace("<sc.SqlCommand.ExecuteReader|INFO> %d#, Command executed as batch RPC.\n", ObjectID); 
                    //}
                    Debug.Assert(_SqlRPCBatchArray != null, "RunExecuteReader rpc array not provided");
                    _stateObj.Parser.TdsExecuteRPC(_SqlRPCBatchArray, this.CommandTimeout, inSchema, this.Notification, _stateObj, CommandType.StoredProcedure == CommandType);
                } 
                else if ((System.Data.CommandType.Text == this.CommandType) && (0 == GetParameterCount(_parameters))) {
                    // Send over SQL Batch command if we are not a stored proc and have no parameters 
                    // MDAC 
                    Debug.Assert(!IsUserPrepared, "CommandType.Text with no params should not be prepared!");
                    if (returnStream) { 
                        Bid.Trace("<sc.SqlCommand.ExecuteReader|INFO> %d#, Command executed as SQLBATCH.\n", ObjectID);
                    }
                    string text = GetCommandText(cmdBehavior) + GetResetOptionsString(cmdBehavior);
                    _stateObj.Parser.TdsExecuteSQLBatch(text, this.CommandTimeout, this.Notification, _stateObj); 
                }
                else if (System.Data.CommandType.Text == this.CommandType) { 
                    if (this.IsDirty) { 
                        Debug.Assert(_cachedMetaData == null, "dirty query should not have cached metadata!");
                        // 
                        // someone changed the command text or the parameter schema so we must unprepare the command
                        //
                        // remeber that IsDirty includes test for IsPrepared!
                        if(_execType == EXECTYPE.PREPARED) { 
                            _hiddenPrepare = true;
                        } 
                        InternalUnprepare(false); 
                        IsDirty = false;
                    } 

                    if (_execType == EXECTYPE.PREPARED) {
                        Debug.Assert(this.IsPrepared && (_prepareHandle != -1), "invalid attempt to call sp_execute without a handle!");
                        rpc = BuildExecute(inSchema); 
                    }
                    else if (_execType == EXECTYPE.PREPAREPENDING) { 
                        Debug.Assert(_activeConnection.IsShiloh, "Invalid attempt to call sp_prepexec on non 7.x server"); 
                        rpc = BuildPrepExec(cmdBehavior);
                        // next time through, only do an exec 
                        _execType = EXECTYPE.PREPARED;
                        _activeConnection.AddPreparedCommand(this);
                        // mark ourselves as preparing the command
                        _inPrepare = true; 
                    }
                    else { 
                        Debug.Assert(_execType == EXECTYPE.UNPREPARED, "Invalid execType!"); 
                        BuildExecuteSql(cmdBehavior, null, _parameters, ref rpc);
                    } 

                    // if shiloh, then set NOMETADATA_UNLESSCHANGED flag
                    if (_activeConnection.IsShiloh)
                        rpc.options = TdsEnums.RPC_NOMETADATA; 
                    if (returnStream) {
                        Bid.Trace("<sc.SqlCommand.ExecuteReader|INFO> %d#, Command executed as RPC.\n", ObjectID); 
                    } 
                    Debug.Assert(_rpcArrayOf1[0] == rpc);
                    _stateObj.Parser.TdsExecuteRPC(_rpcArrayOf1, this.CommandTimeout, inSchema, this.Notification, _stateObj, CommandType.StoredProcedure == CommandType); 
                }
                else {
                    Debug.Assert(this.CommandType == System.Data.CommandType.StoredProcedure, "unknown command type!");
                    // note: invalid asserts on Shiloh. On 8.0 (Shiloh) and above a command is ALWAYS prepared 
                    // and IsDirty is always set if there are changes and the command is marked Prepared!
                    Debug.Assert(IsShiloh || !IsPrepared, "RPC should not be prepared!"); 
                    Debug.Assert(IsShiloh || !IsDirty, "RPC should not be marked as dirty!"); 

                    BuildRPC(inSchema, _parameters, ref rpc); 

                    // if we need to augment the command because a user has changed the command behavior (e.g. FillSchema)
                    // then batch sql them over.  This is inefficient (3 round trips) but the only way we can get metadata only from
                    // a stored proc 
                    optionSettings = GetSetOptionsString(cmdBehavior);
                    if (returnStream) { 
                        Bid.Trace("<sc.SqlCommand.ExecuteReader|INFO> %d#, Command executed as RPC.\n", ObjectID); 
                    }
                    // turn set options ON 
                    if (null != optionSettings) {
                        _stateObj.Parser.TdsExecuteSQLBatch(optionSettings, this.CommandTimeout, this.Notification, _stateObj);
                        _stateObj.Parser.Run(RunBehavior.UntilDone, this, null, null, _stateObj);
                        // and turn OFF when the ds exhausts the stream on Close() 
                        optionSettings = GetResetOptionsString(cmdBehavior);
                    } 
 
                    // turn debugging on
                    _activeConnection.CheckSQLDebug(); 
                    // execute sp
                    Debug.Assert(_rpcArrayOf1[0] == rpc);
                    _stateObj.Parser.TdsExecuteRPC(_rpcArrayOf1, this.CommandTimeout, inSchema, this.Notification, _stateObj, CommandType.StoredProcedure == CommandType);
                } 

                if (returnStream) { 
                    ds = new SqlDataReader(this, cmdBehavior); 
                }
 
                if (async) {
                    _activeConnection.GetOpenTdsConnection().IncrementAsyncCount();
                    cachedAsyncState.SetAsyncReaderState(ds, runBehavior, optionSettings);
                } 
                else {
                    // Always execute - even if no reader! 
                    FinishExecuteReader(ds, runBehavior, optionSettings); 
                }
            } 
            catch (Exception e) {
                processFinallyBlock = ADP.IsCatchableExceptionType (e);
                throw;
            } 
            finally {
                Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to RunExecuteReaderTds");  // you need to setup for a thread abort somewhere before you call this method 
                if (processFinallyBlock && !async) { 
                    // When executing async, we need to keep the _stateObj alive...
                    PutStateObject(); 
                }
            }

            Debug.Assert(async || null == _stateObj, "non-null state object in RunExecuteReader"); 
            return ds;
        } 
 
        private SqlDataReader RunExecuteReaderSmi( CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream ) {
            SqlInternalConnectionSmi innerConnection = InternalSmiConnection; 

            SmiEventStream eventStream = null;
            SqlDataReader ds = null;
            try { 
                // Set it up, process all of the events, and we're done!
                SetUpSmiRequest( innerConnection ); 
 
                long transactionId = (null != innerConnection.CurrentTransaction) ? innerConnection.CurrentTransaction.TransactionId : 0;
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlCommand.RunExecuteReaderSmi|ADV> %d#, innerConnection=%d#, transactionId=0x%I64x, commandBehavior=%d.\n", ObjectID, innerConnection.ObjectID, transactionId, (int)cmdBehavior);
                }

                if (SmiContextFactory.Instance.NegotiatedSmiVersion >= 210) { 
                    eventStream = _smiRequest.Execute(
                                                    innerConnection.SmiConnection, 
                                                    transactionId, 
                                                    innerConnection.InternalEnlistedTransaction,
                                                    cmdBehavior, 
                                                    SmiExecuteType.Reader
                                                    );
                }
                else { 
                    eventStream = _smiRequest.Execute(
                                                    innerConnection.SmiConnection, 
                                                    transactionId, 
                                                    cmdBehavior,
                                                    SmiExecuteType.Reader 
                                                    );
                }

                if ( ( runBehavior & RunBehavior.UntilDone ) != 0 ) { 

                    // Consume the results 
                    while( eventStream.HasEvents ) { 
                        eventStream.ProcessEvent( EventSink );
                    } 
                    eventStream.Close( EventSink );
                }

                if ( returnStream ) { 
                    ds = new SqlDataReaderSmi( eventStream, this, cmdBehavior, innerConnection, EventSink );
                    ds.NextResult();    // Position on first set of results 
                    _activeConnection.AddWeakReference(ds, SqlReferenceCollection.DataReaderTag); 
                }
 
                EventSink.ProcessMessagesAndThrow();
            }
            catch {
                if ( null != eventStream ) 
                    eventStream.Close( EventSink );     //
 
                DisposeSmiRequest(); 

                throw; 
            }

        return ds;
        } 

        private SqlDataReader CompleteAsyncExecuteReader() { 
            SqlDataReader ds = cachedAsyncState.CachedAsyncReader; // should not be null 
            bool processFinallyBlock = true;
            try { 
                FinishExecuteReader(ds, cachedAsyncState.CachedRunBehavior, cachedAsyncState.CachedSetOptions);
            }
            catch (Exception e) {
                processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                throw;
            } 
            finally { 
                Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to CompleteAsyncExecuteReader");  // you need to setup for a thread abort somewhere before you call this method
                if (processFinallyBlock) { 
                    cachedAsyncState.ResetAsyncState();
                    PutStateObject();
                }
            } 

            return ds; 
        } 

        private void FinishExecuteReader(SqlDataReader ds, RunBehavior runBehavior, string resetOptionsString) { 
            // always wrap with a try { FinishExecuteReader(...) } finally { PutStateObject(); }

            NotifyDependency();
            if (runBehavior == RunBehavior.UntilDone) { 
                try {
                    _stateObj.Parser.Run(RunBehavior.UntilDone, this, ds, null, _stateObj); 
                } 
                catch (Exception e) {
                    // 
                    if (ADP.IsCatchableExceptionType(e)) {
                        if (_inPrepare) {
                            // The flag is expected to be reset by OnReturnValue.  We should receive
                            // the handle unless command execution failed.  If fail, move back to pending 
                            // state.
                            _inPrepare = false;                  // reset the flag 
                            IsDirty = true;                      // mark command as dirty so it will be prepared next time we're comming through 
                            _execType = EXECTYPE.PREPAREPENDING; // reset execution type to pending
                        } 

                        if (null != ds) {
                            ds.Close();
                        } 
                    }
                    throw; 
                } 
            }
 
            // bind the parser to the reader if we get this far
            if (ds != null) {
                ds.Bind(_stateObj);
                _stateObj = null;   // the reader now owns this... 
                ds.ResetOptionsString = resetOptionsString;
 
                // 

 

                // bind this reader to this connection now
                _activeConnection.AddWeakReference(ds, SqlReferenceCollection.DataReaderTag);
 
                // force this command to start reading data off the wire.
                // this will cause an error to be reported at Execute() time instead of Read() time 
                // if the command is not set. 
                try {
                    _cachedMetaData = ds.MetaData; 
                    ds.IsInitialized = true; // Webdata 104560
                }
                catch (Exception e) {
                    // 
                    if (ADP.IsCatchableExceptionType(e)) {
                        if (_inPrepare) { 
                            // The flag is expected to be reset by OnReturnValue.  We should receive 
                            // the handle unless command execution failed.  If fail, move back to pending
                            // state. 
                            _inPrepare = false;                  // reset the flag
                            IsDirty = true;                      // mark command as dirty so it will be prepared next time we're comming through
                            _execType = EXECTYPE.PREPAREPENDING; // reset execution type to pending
                        } 

                        ds.Close(); 
                    } 

                    throw; 
                }
            }
        }
 
        private void NotifyDependency() {
            if (_sqlDep != null) { 
                _sqlDep.StartTimer(Notification); 
            }
        } 

        public SqlCommand Clone() {
            SqlCommand clone = new SqlCommand(this);
            Bid.Trace("<sc.SqlCommand.Clone|API> %d#, clone=%d#\n", ObjectID, clone.ObjectID); 
            return clone;
        } 
 
        object ICloneable.Clone() {
            return Clone(); 
        }

        // validates that a command has commandText and a non-busy open connection
        // throws exception for error case, returns false if the commandText is empty 
        private void ValidateCommand(string method, bool async) {
            if (null == _activeConnection) { 
                throw ADP.ConnectionRequired(method); 
            }
 
            // if the parser is not openloggedin, the connection is no longer good
            SqlInternalConnectionTds tdsConnection = _activeConnection.InnerConnection as SqlInternalConnectionTds;
            if (tdsConnection != null) {
                if (tdsConnection.Parser.State != TdsParserState.OpenLoggedIn) { 
                    if (tdsConnection.Parser.State == TdsParserState.Closed) {
                        throw ADP.OpenConnectionRequired(method, ConnectionState.Closed); 
                    } 
                    throw ADP.OpenConnectionRequired(method, ConnectionState.Broken);
                } 
            }

            ValidateAsyncCommand();
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    // close any non MARS dead readers, if applicable, and then throw if still busy.
                    // Throw if we have a live reader on this command 
                    _activeConnection.ValidateConnectionForExecute(method, this); 

#if DEBUG 
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) 
            {
                _activeConnection.Abort(e); 
                throw;
            }
            catch (System.StackOverflowException e)
            { 
                _activeConnection.Abort(e);
                throw; 
            } 
            catch (System.Threading.ThreadAbortException e)
            { 
                _activeConnection.Abort(e);
                throw;
            }
            // Check to see if the currently set transaction has completed.  If so, 
            // null out our local reference.
            if (null != _transaction && _transaction.Connection == null) 
                _transaction = null; 

            // throw if the connection is in a transaction but there is no 
            // locally assigned transaction object
            if (_activeConnection.HasLocalTransactionFromAPI && (null == _transaction))
                throw ADP.TransactionRequired(method);
 
            // if we have a transaction, check to ensure that the active
            // connection property matches the connection associated with 
            // the transaction 
            if (null != _transaction && _activeConnection != _transaction.Connection)
                throw ADP.TransactionConnectionMismatch(); 

            if (ADP.IsEmpty(this.CommandText))
                throw ADP.CommandTextRequired(method);
 
            // Notification property must be null for pre-Yukon connections
            if ((Notification != null) && !_activeConnection.IsYukonOrNewer) { 
                throw SQL.NotificationsRequireYukon(); 
            }
 
            if (async && !_activeConnection.Asynchronous) {
                throw (SQL.AsyncConnectionRequired());
            }
        } 

        private void ValidateAsyncCommand() { 
            // 
            if (cachedAsyncState.PendingAsyncOperation) { // Enforce only one pending async execute at a time.
                if (cachedAsyncState.IsActiveConnectionValid(_activeConnection)) { 
                    throw SQL.PendingBeginXXXExists();
                }
                else {
                    _stateObj = null; // Session was re-claimed by session pool upon connection close. 
                    cachedAsyncState.ResetAsyncState();
                } 
            } 
        }
 
        private void GetStateObject() {
            Debug.Assert (null == _stateObj,"StateObject not null on GetStateObject");
            Debug.Assert (null != _activeConnection, "no active connection?");
 
            if (_pendingCancel) {
                _pendingCancel = false; // Not really needed, but we'll reset anyways. 
 
                // If a pendingCancel exists on the object, we must have had a Cancel() call
                // between the point that we entered an Execute* API and the point in Execute* that 
                // we proceeded to call this function and obtain a stateObject.  In that case,
                // we now throw a cancelled error.
                throw SQL.OperationCancelled();
            } 

            TdsParserStateObject stateObj = _activeConnection.Parser.GetSession(this); 
            stateObj.StartSession(ObjectID); 

            _stateObj = stateObj; 

            if (_pendingCancel) {
                _pendingCancel = false; // Not really needed, but we'll reset anyways.
 
                // If a pendingCancel exists on the object, we must have had a Cancel() call
                // between the point that we entered this function and the point where we obtained 
                // and actually assigned the stateObject to the local member.  It is possible 
                // that the flag is set as well as a call to stateObj.Cancel - though that would
                // be a no-op.  So - throw. 
                throw SQL.OperationCancelled();
            }
         }
 
        private void PutStateObject() {
            TdsParserStateObject stateObj = _stateObj; 
            _stateObj = null; 

            if (null != stateObj) { 
                stateObj.CloseSession();
            }
        }
 
        internal void OnDoneProc() { // called per rpc batch complete
            if (BatchRPCMode) { 
 
                // track the records affected for the just completed rpc batch
                // _rowsAffected is cumulative for ExecuteNonQuery across all rpc batches 
                _SqlRPCBatchArray[_currentlyExecutingBatch].cumulativeRecordsAffected = _rowsAffected;

                _SqlRPCBatchArray[_currentlyExecutingBatch].recordsAffected =
                    (((0 < _currentlyExecutingBatch) && (0 <= _rowsAffected)) 
                        ? (_rowsAffected - Math.Max(_SqlRPCBatchArray[_currentlyExecutingBatch-1].cumulativeRecordsAffected, 0))
                        : _rowsAffected); 
 
                // track the error collection (not available from TdsParser after ExecuteNonQuery)
                // and the which errors are associated with the just completed rpc batch 
                _SqlRPCBatchArray[_currentlyExecutingBatch].errorsIndexStart =
                    ((0 < _currentlyExecutingBatch)
                        ? _SqlRPCBatchArray[_currentlyExecutingBatch-1].errorsIndexEnd
                        : 0); 
                _SqlRPCBatchArray[_currentlyExecutingBatch].errorsIndexEnd = _stateObj.Parser.Errors.Count;
                _SqlRPCBatchArray[_currentlyExecutingBatch].errors = _stateObj.Parser.Errors; 
 
                // track the warning collection (not available from TdsParser after ExecuteNonQuery)
                // and the which warnings are associated with the just completed rpc batch 
                _SqlRPCBatchArray[_currentlyExecutingBatch].warningsIndexStart =
                    ((0 < _currentlyExecutingBatch)
                        ? _SqlRPCBatchArray[_currentlyExecutingBatch-1].warningsIndexEnd
                        : 0); 
                _SqlRPCBatchArray[_currentlyExecutingBatch].warningsIndexEnd = _stateObj.Parser.Warnings.Count;
                _SqlRPCBatchArray[_currentlyExecutingBatch].warnings = _stateObj.Parser.Warnings; 
 
                _currentlyExecutingBatch++;
                Debug.Assert(_parameterCollectionList.Count >= _currentlyExecutingBatch, "OnDoneProc: Too many DONEPROC events"); 
            }
        }

        // 
        //
 
 
        internal void OnReturnStatus(int status) {
            if (_inPrepare) 
                return;

            SqlParameterCollection parameters = _parameters;
            if (BatchRPCMode) { 
                if (_parameterCollectionList.Count > _currentlyExecutingBatch) {
                    parameters = _parameterCollectionList[_currentlyExecutingBatch]; 
                } 
                else {
                    Debug.Assert(false, "OnReturnStatus: SqlCommand got too many DONEPROC events"); 
                    parameters = null;
                }
            }
            // see if a return value is bound 
            int count = GetParameterCount(parameters);
            for (int i = 0; i < count; i++) { 
                SqlParameter parameter = parameters[i]; 
                if (parameter.Direction == ParameterDirection.ReturnValue) {
                    object v = parameter.Value; 

                // if the user bound a sqlint32 (the only valid one for status, use it)
                if ( (null != v) && (v.GetType() == typeof(SqlInt32)) ) {
                        parameter.Value = new SqlInt32(status); // value type 
                }
                else { 
                        parameter.Value = status; 

                    } 
                    break;
                }
            }
        } 

        // 
        // Move the return value to the corresponding output parameter. 
        // Return parameters are sent in the order in which they were defined in the procedure.
        // If named, match the parameter name, otherwise fill in based on ordinal position. 
        // If the parameter is not bound, then ignore the return value.
        //
        internal void OnReturnValue(SqlReturnValue rec) {
 
            if (_inPrepare) {
                if (!rec.value.IsNull) { 
                    _prepareHandle = rec.value.Int32; 
                }
                _inPrepare = false; 
                return;
            }

            SqlParameterCollection parameters = GetCurrentParameterCollection(); 
            int  count      = GetParameterCount(parameters);
 
 
            SqlParameter thisParam = GetParameterForOutputValueExtraction(parameters, rec.parameter, count);
 
            if (null != thisParam) {
                // copy over data

                // if the value user has supplied a SqlType class, then just copy over the SqlType, otherwise convert 
                // to the com type
                object val = thisParam.Value; 
 
                //set the UDT value as typed object rather than bytes
                if (SqlDbType.Udt == thisParam.SqlDbType) { 
                    object data = null;
                    try {
                        SqlConnection.CheckGetExtendedUDTInfo(rec, true);
 
                        //extract the byte array from the param value
                        if (rec.value.IsNull) 
                            data = DBNull.Value; 
                        else {
                            data = rec.value.ByteArray; //should work for both sql and non-sql values 
                        }

                        //call the connection to instantiate the UDT object
                        thisParam.Value = Connection.GetUdtValue(data, rec, false); 
                    }
                    catch (FileNotFoundException e) { 
                        // SQL BU DT 329981 
                        // Assign Assembly.Load failure in case where assembly not on client.
                        // This allows execution to complete and failure on SqlParameter.Value. 
                        thisParam.SetUdtLoadError(e);
                    }
                    catch (FileLoadException e) {
                        // SQL BU DT 329981 
                        // Assign Assembly.Load failure in case where assembly cannot be loaded on client.
                        // This allows execution to complete and failure on SqlParameter.Value. 
                        thisParam.SetUdtLoadError(e); 
                    }
 
                    return;
                } else {
                    thisParam.SetSqlBuffer(rec.value);
                } 

                MetaType mt = MetaType.GetMetaTypeFromSqlDbType(rec.type, rec.isMultiValued); 
 
                if (rec.type == SqlDbType.Decimal) {
                    thisParam.ScaleInternal = rec.scale; 
                    thisParam.PrecisionInternal = rec.precision;
                }
                else if (mt.IsVarTime) {
                    thisParam.ScaleInternal = rec.scale; 
                }
                else if (rec.type == SqlDbType.Xml) { 
                    SqlCachedBuffer cachedBuffer = (thisParam.Value as SqlCachedBuffer); 
                    if (null != cachedBuffer) {
                        thisParam.Value = cachedBuffer.ToString(); 
                    }
                }

                if (rec.collation != null) { 
                    Debug.Assert(mt.IsCharType, "Invalid collation structure for non-char type");
                    thisParam.Collation = rec.collation; 
                } 
            }
 
            return;
        }

        internal void OnParametersAvailableSmi( SmiParameterMetaData[] paramMetaData, ITypedGettersV3 parameterValues ) { 
            Debug.Assert(null != paramMetaData);
 
            for(int index=0; index < paramMetaData.Length; index++) { 
                OnParameterAvailableSmi(paramMetaData[index], parameterValues, index);
            } 
        }

        internal void OnParameterAvailableSmi(SmiParameterMetaData metaData, ITypedGettersV3 parameterValues, int ordinal) {
            if ( ParameterDirection.Input != metaData.Direction ) { 
                string name = null;
                if (ParameterDirection.ReturnValue != metaData.Direction) { 
                    name = metaData.Name; 
                }
 
                SqlParameterCollection parameters = GetCurrentParameterCollection();
                int  count      = GetParameterCount(parameters);
                SqlParameter param = GetParameterForOutputValueExtraction(parameters, name, count);
 
                if ( null != param ) {
                    param.LocaleId = (int)metaData.LocaleId; 
                    param.CompareInfo = metaData.CompareOptions; 
                    SqlBuffer buffer = new SqlBuffer();
                    object result; 
                    if (_activeConnection.IsKatmaiOrNewer) {
                        result = ValueUtilsSmi.GetOutputParameterV200Smi(
                                OutParamEventSink, (SmiTypedGetterSetter)parameterValues, ordinal, metaData, _smiRequestContext, buffer );
                    } 
                    else {
                        result = ValueUtilsSmi.GetOutputParameterV3Smi( 
                                    OutParamEventSink, parameterValues, ordinal, metaData, _smiRequestContext, buffer ); 
                    }
                    if ( null != result ) { 
                        param.Value = result;
                    }
                    else {
                        param.SetSqlBuffer( buffer ); 
                    }
                } 
            } 
        }
 
        private SqlParameterCollection GetCurrentParameterCollection() {
            if (BatchRPCMode) {
                if (_parameterCollectionList.Count > _currentlyExecutingBatch) {
                    return _parameterCollectionList[_currentlyExecutingBatch]; 
                }
                else { 
                    Debug.Assert(false, "OnReturnValue: SqlCommand got too many DONEPROC events"); 
                    return null;
                } 
            }
            else {
                return _parameters;
            } 
        }
 
        private SqlParameter GetParameterForOutputValueExtraction( SqlParameterCollection parameters, 
                        string paramName, int paramCount ) {
            SqlParameter thisParam = null; 
            bool foundParam = false;

            if (null == paramName) {
                // rec.parameter should only be null for a return value from a function 
                for (int i = 0; i < paramCount; i++) {
                    thisParam = parameters[i]; 
                    // searching for ReturnValue 
                    if (thisParam.Direction == ParameterDirection.ReturnValue) {
                                foundParam = true; 
                            break; // found it
                    }
                }
            } 
            else {
                for (int i = 0; i < paramCount; i++) { 
                    thisParam = parameters[i]; 
                    // searching for Output or InputOutput or ReturnValue with matching name
                    if (thisParam.Direction != ParameterDirection.Input && thisParam.Direction != ParameterDirection.ReturnValue  && paramName == thisParam.ParameterNameFixed) { 
                                foundParam = true;
                            break; // found it
                        }
                    } 
            }
            if (foundParam) 
                return thisParam; 
            else
                return null; 
        }

        private void GetRPCObject(int paramCount, ref _SqlRPC rpc) {
 
            // Designed to minimize necessary allocations
            int ii; 
            if (rpc == null) { 
                if (_rpcArrayOf1 == null) {
                    _rpcArrayOf1 = new _SqlRPC[1]; 
                    _rpcArrayOf1[0] = new _SqlRPC();
                }
                rpc = _rpcArrayOf1[0] ;
            } 

            rpc.ProcID = 0; 
            rpc.rpcName = null; 
            rpc.options = 0;
 
            rpc.recordsAffected = default(int?);
            rpc.cumulativeRecordsAffected = -1;

            rpc.errorsIndexStart = 0; 
            rpc.errorsIndexEnd = 0;
            rpc.errors = null; 
 
            rpc.warningsIndexStart = 0;
            rpc.warningsIndexEnd = 0; 
            rpc.warnings = null;

            // Make sure there is enough space in the parameters and paramoptions arrays
            if(rpc.parameters == null || rpc.parameters.Length < paramCount) { 
                rpc.parameters = new SqlParameter[paramCount];
            } 
            else if (rpc.parameters.Length > paramCount) { 
                        rpc.parameters[paramCount]=null;    // Terminator
            } 
            if(rpc.paramoptions == null || (rpc.paramoptions.Length < paramCount)) {
                rpc.paramoptions = new byte[paramCount];
            }
            else { 
                for (ii = 0 ; ii < paramCount ; ii++)
                    rpc.paramoptions[ii] = 0; 
            } 
        }
 
        private void SetUpRPCParameters (_SqlRPC rpc, int startCount, bool inSchema, SqlParameterCollection parameters) {
            int ii;
            int paramCount = GetParameterCount(parameters) ;
            int j = startCount; 
            TdsParser parser = _activeConnection.Parser;
#if WINFSFunctionality 
            bool isWinfs = parser.IsWinFS; 
#endif
            bool yukonOrNewer = parser.IsYukonOrNewer; 

            for (ii = 0;  ii < paramCount; ii++) {
                SqlParameter parameter = parameters[ii];
#if WINFSFunctionality 
                parameter.Validate(ii, isWinfs);
#else 
                parameter.Validate(ii, CommandType.StoredProcedure == CommandType); 
#endif
 
                // func will change type to that with a 4 byte length if the type has a two
                // byte length and a parameter length > than that expressable in 2 bytes
#if WINFSFunctionality
                parameter.ValidateTypeLengths(yukonOrNewer, isWinfs); 
#else
                parameter.ValidateTypeLengths(yukonOrNewer); 
#endif 

                if (ShouldSendParameter(parameter)) { 
                    rpc.parameters[j] = parameter;

                    // set output bit
                    if (parameter.Direction == ParameterDirection.InputOutput || 
                        parameter.Direction == ParameterDirection.Output)
                        rpc.paramoptions[j] = TdsEnums.RPC_PARAM_BYREF; 
 
                    // set default value bit
                    if (parameter.Direction != ParameterDirection.Output) { 
                        // remember that null == Convert.IsEmpty, DBNull.Value is a database null!

                        // MDAC 62117, don't assume a default value exists for parameters in the case when
                        // the user is simply requesting schema 
                        if (null == parameter.Value && !inSchema) {
                            rpc.paramoptions[j] |= TdsEnums.RPC_PARAM_DEFAULT; 
                        } 
                    }
 
                    // Must set parameter option bit for LOB_COOKIE if unfilled LazyMat blob
#if WINFSFunctionality
                    if (isWinfs && parameter.IsNonFilledLazyMatInstance()) {
                        rpc.paramoptions[j] |= TdsEnums.RPC_PARAM_IS_LOB_COOKIE; 
                    }
#endif 
                    j++; 
                }
            } 

        }

        // 
        // 7.5
        // prototype for sp_prepexec is: 
        // sp_prepexec(@handle int IN/OUT, @batch_params ntext, @batch_text ntext, param1value,param2value...) 
        //
        private _SqlRPC  BuildPrepExec(CommandBehavior behavior) { 
            Debug.Assert(System.Data.CommandType.Text == this.CommandType, "invalid use of sp_prepexec for stored proc invocation!");
            SqlParameter sqlParam;
            int j = 3;
 
            int count = CountSendableParameters(_parameters);
 
            _SqlRPC rpc = null; 
            GetRPCObject(count + j, ref rpc);
 
            rpc.ProcID = TdsEnums.RPC_PROCID_PREPEXEC;
            rpc.rpcName = TdsEnums.SP_PREPEXEC;

            //@handle 
            sqlParam = new SqlParameter(null, SqlDbType.Int);
            sqlParam.Direction = ParameterDirection.InputOutput; 
            sqlParam.Value = _prepareHandle; 
            rpc.parameters[0] = sqlParam;
            rpc.paramoptions[0] = TdsEnums.RPC_PARAM_BYREF; 

            //@batch_params
            string paramList = BuildParamList(_stateObj.Parser, _parameters);
            sqlParam = new SqlParameter(null, ((paramList.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, paramList.Length); 
            sqlParam.Value = paramList;
            rpc.parameters[1] = sqlParam; 
 
            //@batch_text
            string text = GetCommandText(behavior); 
            sqlParam = new SqlParameter(null, ((text.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, text.Length);
            sqlParam.Value = text;
            rpc.parameters[2] = sqlParam;
 
            SetUpRPCParameters (rpc,  j, false, _parameters);
            return rpc; 
        } 

 
        //
        // returns true if the parameter is not a return value
        // and it's value is not DBNull (for a nullable parameter)
        // 
        private static bool ShouldSendParameter(SqlParameter p) {
            switch (p.Direction) { 
            case ParameterDirection.ReturnValue: 
                // return value parameters are never sent
                return false; 
            case ParameterDirection.Output:
            case ParameterDirection.InputOutput:
            case ParameterDirection.Input:
                // InputOutput/Output parameters are aways sent 
                return true;
            default: 
                Debug.Assert(false, "Invalid ParameterDirection!"); 
                return false;
            } 
        }

        private int CountSendableParameters(SqlParameterCollection parameters) {
            int cParams = 0; 

            if (parameters != null) { 
                int count = parameters.Count; 
                for (int i = 0; i < count; i++) {
                    if (ShouldSendParameter(parameters[i])) 
                        cParams++;
                }
            }
            return cParams; 
        }
 
        // Returns total number of parameters 
        private int GetParameterCount(SqlParameterCollection parameters) {
            return ((null != parameters) ? parameters.Count : 0); 
        }

        //
        // build the RPC record header for this stored proc and add parameters 
        //
        private void BuildRPC(bool inSchema, SqlParameterCollection parameters, ref _SqlRPC rpc) { 
            Debug.Assert(this.CommandType == System.Data.CommandType.StoredProcedure, "Command must be a stored proc to execute an RPC"); 
            int count = CountSendableParameters(parameters);
            GetRPCObject(count, ref rpc); 

            rpc.rpcName = this.CommandText; // just get the raw command text

            SetUpRPCParameters ( rpc, 0, inSchema, parameters); 
        }
 
        // 
        // build the RPC record header for sp_unprepare
        // 
        // prototype for sp_unprepare is:
        // sp_unprepare(@handle)
        //
        // 
        private _SqlRPC BuildUnprepare() {
            Debug.Assert(_prepareHandle != 0, "Invalid call to sp_unprepare without a valid handle!"); 
 
            _SqlRPC rpc = null;
            GetRPCObject(1, ref rpc); 
            SqlParameter sqlParam;

            rpc.ProcID = TdsEnums.RPC_PROCID_UNPREPARE;
            rpc.rpcName = TdsEnums.SP_UNPREPARE; 

            //@handle 
            sqlParam = new SqlParameter(null, SqlDbType.Int); 
            sqlParam.Value = _prepareHandle;
            rpc.parameters[0] = sqlParam; 

            return rpc;
        }
 
        //
        // build the RPC record header for sp_execute 
        // 
        // prototype for sp_execute is:
        // sp_execute(@handle int,param1value,param2value...) 
        //
        private _SqlRPC BuildExecute(bool inSchema) {
            Debug.Assert(_prepareHandle != -1, "Invalid call to sp_execute without a valid handle!");
            int j = 1; 

            int count = CountSendableParameters(_parameters); 
 
            _SqlRPC rpc = null;
            GetRPCObject(count + j, ref rpc); 

            SqlParameter sqlParam;

            rpc.ProcID = TdsEnums.RPC_PROCID_EXECUTE; 
            rpc.rpcName = TdsEnums.SP_EXECUTE;
 
            //@handle 
            sqlParam = new SqlParameter(null, SqlDbType.Int);
            sqlParam.Value = _prepareHandle; 
            rpc.parameters[0] = sqlParam;

            SetUpRPCParameters (rpc, j, inSchema, _parameters);
            return rpc; 
        }
 
        // 
        // build the RPC record header for sp_executesql and add the parameters
        // 
        // prototype for sp_executesql is:
        // sp_executesql(@batch_text nvarchar(4000),@batch_params nvarchar(4000), param1,.. paramN)
        private void BuildExecuteSql(CommandBehavior behavior, string commandText, SqlParameterCollection parameters, ref _SqlRPC rpc) {
 
            Debug.Assert(_prepareHandle == -1, "This command has an existing handle, use sp_execute!");
            Debug.Assert(System.Data.CommandType.Text == this.CommandType, "invalid use of sp_executesql for stored proc invocation!"); 
            int j; 
            SqlParameter sqlParam;
 
            int cParams = CountSendableParameters(parameters);
            if (cParams > 0) {
                j = 2;
            } 
            else {
                j =1; 
            } 

            GetRPCObject(cParams + j, ref rpc); 
            rpc.ProcID = TdsEnums.RPC_PROCID_EXECUTESQL;
            rpc.rpcName = TdsEnums.SP_EXECUTESQL;

            // @sql 
            if (commandText == null) {
                commandText = GetCommandText(behavior); 
            } 
            sqlParam = new SqlParameter(null, ((commandText.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, commandText.Length);
            sqlParam.Value = commandText; 
            rpc.parameters[0] = sqlParam;

            if (cParams > 0) {
                string paramList = BuildParamList(_stateObj.Parser, BatchRPCMode  ? parameters : _parameters); 
                sqlParam = new SqlParameter(null, ((paramList.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, paramList.Length);
                sqlParam.Value = paramList; 
                rpc.parameters[1] = sqlParam; 

                bool inSchema =  (0 != (behavior & CommandBehavior.SchemaOnly)); 
                SetUpRPCParameters (rpc, j,  inSchema, parameters);
            }
        }
 
        // paramList parameter for sp_executesql, sp_prepare, and sp_prepexec
        internal string BuildParamList(TdsParser parser, SqlParameterCollection parameters) { 
            StringBuilder paramList = new StringBuilder(); 
            bool fAddSeperator = false;
 
            bool yukonOrNewer = parser.IsYukonOrNewer;
            int count = 0;

            count = parameters.Count; 
            for (int i = 0; i < count; i++) {
                SqlParameter sqlParam = parameters[i]; 
#if WINFSFunctionality 
                sqlParam.Validate(i, parser.IsWinFS);
#else 
                sqlParam.Validate(i, CommandType.StoredProcedure == CommandType);
#endif
                // skip ReturnValue parameters; we never send them to the server
                if (!ShouldSendParameter(sqlParam)) 
                    continue;
 
                // add our separator for the ith parmeter 
                if (fAddSeperator)
                    paramList.Append(','); 

                paramList.Append(sqlParam.ParameterNameFixed);

                MetaType mt = sqlParam.InternalMetaType; 

                //for UDTs, get the actual type name. Get only the typename, omitt catalog and schema names. 
                //in TSQL you should only specify the unqualified type name 

                // paragraph above doesn't seem to be correct. Server won't find the type 
                // if we don't provide a fully qualified name
                paramList.Append(" ");
                if (mt.SqlDbType == SqlDbType.Udt) {
                    string fullTypeName = sqlParam.UdtTypeName; 
                    if(ADP.IsEmpty(fullTypeName))
                        throw SQL.MustSetUdtTypeNameForUdtParams(); 
                    // DEVNOTE: do we need to escape the full type name? 
                    paramList.Append(fullTypeName);
                } 
                else if (mt.SqlDbType == SqlDbType.Structured) {
                    string typeName = sqlParam.TypeName;
                    if (ADP.IsEmpty(typeName)) {
                        throw SQL.MustSetTypeNameForParam(mt.TypeName, sqlParam.ParameterNameFixed); 
                    }
                    paramList.Append(typeName); 
 
                    // TVPs currently are the only Structured type and must be read only, so add that keyword
                    paramList.Append(" READONLY"); 
                }
                else {
                    // func will change type to that with a 4 byte length if the type has a two
                    // byte length and a parameter length > than that expressable in 2 bytes 
#if WINFSFunctionality
                    mt  = sqlParam.ValidateTypeLengths(yukonOrNewer, parser.IsWinFS); 
#else 
                    mt  = sqlParam.ValidateTypeLengths(yukonOrNewer);
#endif 
                    paramList.Append(mt.TypeName);
                }

                fAddSeperator = true; 

                if (mt.SqlDbType == SqlDbType.Decimal) { 
                    byte precision = sqlParam.GetActualPrecision(); 
                    byte scale = sqlParam.GetActualScale();
 
                    paramList.Append('(');

                    if (0 == precision) {
                        if (IsShiloh) { 
                            precision = TdsEnums.DEFAULT_NUMERIC_PRECISION;
                        } else { 
                            precision = TdsEnums.SPHINX_DEFAULT_NUMERIC_PRECISION; 
                        }
                    } 

                    paramList.Append(precision);
                    paramList.Append(',');
                    paramList.Append(scale); 
                    paramList.Append(')');
                } 
                else if (mt.IsVarTime) { 
                    byte scale = sqlParam.GetActualScale();
 
                    paramList.Append('(');
                    paramList.Append(scale);
                    paramList.Append(')');
                } 
                else if (false == mt.IsFixed && false == mt.IsLong && mt.SqlDbType != SqlDbType.Timestamp && mt.SqlDbType != SqlDbType.Udt && SqlDbType.Structured != mt.SqlDbType) {
                    int size = sqlParam.Size; 
 
                    paramList.Append('(');
 
                    // if using non unicode types, obtain the actual byte length from the parser, with it's associated code page
                    if (mt.IsAnsiType) {
#if WINFSFunctionality
                        object val = sqlParam.GetCoercedValue(parser.IsWinFS); 
#else
                        object val = sqlParam.GetCoercedValue(); 
#endif 
                        string s = null;
 
                        // deal with the sql types
                        if ((null != val) && (DBNull.Value != val)) {
                            s = (val as string);
                            if (null == s) { 
                                SqlString sval = val is SqlString ? (SqlString)val : SqlString.Null;
                                if (!sval.IsNull) { 
                                    s = sval.Value; 
                                }
                            } 
                        }

                        if (null != s) {
#if WINFSFunctionality 
                            int actualBytes = parser.GetEncodingCharLength(s, sqlParam.GetActualSize(parser.IsWinFS), sqlParam.Offset, null);
#else 
                            int actualBytes = parser.GetEncodingCharLength(s, sqlParam.GetActualSize(), sqlParam.Offset, null); 
#endif
                            // if actual number of bytes is greater than the user given number of chars, use actual bytes 
                            if (actualBytes > size)
                                size = actualBytes;
                        }
                    } 

                    // bug 49497, if the user specifies a 0-sized parameter for a variable len field 
                    // pass over max size (8000 bytes or 4000 characters for wide types) 
                    if (0 == size)
                        size = mt.IsSizeInCharacters ? (TdsEnums.MAXSIZE >> 1) : TdsEnums.MAXSIZE; 

                    paramList.Append(size);
                    paramList.Append(')');
                } 
                else if (mt.IsPlp && (mt.SqlDbType != SqlDbType.Xml) && (mt.SqlDbType != SqlDbType.Udt)) {
                    paramList.Append("(max) "); 
                } 

                // set the output bit for Output or InputOutput parameters 
                if (sqlParam.Direction != ParameterDirection.Input)
                    paramList.Append(" " + TdsEnums.PARAM_OUTPUT);
            }
 
            return paramList.ToString();
        } 
 
        // returns set option text to turn on format only and key info on and off
        // @devnote:  When we are executing as a text command, then we never need 
        // to turn off the options since they command text is executed in the scope of sp_executesql.
        // For a stored proc command, however, we must send over batch sql and then turn off
        // the set options after we read the data.  See the code in Command.Execute()
        private string GetSetOptionsString(CommandBehavior behavior) { 
            string s = null;
 
            if ((System.Data.CommandBehavior.SchemaOnly == (behavior & CommandBehavior.SchemaOnly)) || 
               (System.Data.CommandBehavior.KeyInfo == (behavior & CommandBehavior.KeyInfo))) {
 
                // MDAC 56898 - SET FMTONLY ON will cause the server to ignore other SET OPTIONS, so turn
                // it off before we ask for browse mode metadata
                s = TdsEnums.FMTONLY_OFF;
 
                if (System.Data.CommandBehavior.KeyInfo == (behavior & CommandBehavior.KeyInfo)) {
                    s = s + TdsEnums.BROWSE_ON; 
                } 

                if (System.Data.CommandBehavior.SchemaOnly == (behavior & CommandBehavior.SchemaOnly)) { 
                    s = s + TdsEnums.FMTONLY_ON;
                }
            }
 
            return s;
        } 
 
        private string GetResetOptionsString(CommandBehavior behavior) {
            string s = null; 

            // SET FMTONLY ON OFF
            if (System.Data.CommandBehavior.SchemaOnly == (behavior & CommandBehavior.SchemaOnly)) {
                s = s + TdsEnums.FMTONLY_OFF; 
            }
 
            // SET NO_BROWSETABLE OFF 
            if (System.Data.CommandBehavior.KeyInfo == (behavior & CommandBehavior.KeyInfo)) {
                s = s + TdsEnums.BROWSE_OFF; 
            }

            return s;
        } 

        private String GetCommandText(CommandBehavior behavior) { 
            // build the batch string we send over, since we execute within a stored proc (sp_executesql), the SET options never need to be 
            // turned off since they are scoped to the sproc
            Debug.Assert(System.Data.CommandType.Text == this.CommandType, "invalid call to GetCommandText for stored proc!"); 
            return GetSetOptionsString(behavior) + this.CommandText;
        }

        // 
        // build the RPC record header for sp_executesql and add the parameters
        // 
        // the prototype for sp_prepare is: 
        // sp_prepare(@handle int OUTPUT, @batch_params ntext, @batch_text ntext, @options int default 0x1)
        private _SqlRPC BuildPrepare(CommandBehavior behavior) { 
            Debug.Assert(System.Data.CommandType.Text == this.CommandType, "invalid use of sp_prepare for stored proc invocation!");

            _SqlRPC rpc = null;
            GetRPCObject(3, ref rpc); 
            SqlParameter sqlParam;
 
            rpc.ProcID = TdsEnums.RPC_PROCID_PREPARE; 
            rpc.rpcName = TdsEnums.SP_PREPARE;
 
            //@handle
            sqlParam = new SqlParameter(null, SqlDbType.Int);
            sqlParam.Direction = ParameterDirection.Output;
            rpc.parameters[0] = sqlParam; 
            rpc.paramoptions[0] = TdsEnums.RPC_PARAM_BYREF;
 
            //@batch_params 
            string paramList = BuildParamList(_stateObj.Parser, _parameters);
            sqlParam = new SqlParameter(null, ((paramList.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, paramList.Length); 
            sqlParam.Value = paramList;
            rpc.parameters[1] = sqlParam;

            //@batch_text 
            string text = GetCommandText(behavior);
            sqlParam = new SqlParameter(null, ((text.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, text.Length); 
            sqlParam.Value = text; 
            rpc.parameters[2] = sqlParam;
 
/*
            //@options
            sqlParam = new SqlParameter(null, SqlDbType.Int);
            rpc.Parameters[3] = sqlParam; 
*/
            return rpc; 
        } 

        private void CheckThrowSNIException() { 
            if (null != _stateObj && _stateObj._error != null) {
                _stateObj.Parser.Errors.Add(_stateObj._error);
                _stateObj._error = null;
                _stateObj.Parser.ThrowExceptionAndWarning(_stateObj); 
            }
        } 
 
        private bool IsPrepared {
            get { return(_execType != EXECTYPE.UNPREPARED);} 
        }

        private bool IsUserPrepared {
            get { return IsPrepared && !_hiddenPrepare && !IsDirty; } 
        }
 
        internal bool IsDirty { 
            get {
                // only dirty if prepared 
                return (IsPrepared && (_dirty || ((null != _parameters) && _parameters.IsDirty)));
            }
            set {
                // only mark the command as dirty if it is already prepared 
                // but always clear the value if it we are clearing the dirty flag
                _dirty = value ? IsPrepared : false; 
                if (null != _parameters) { 
                    _parameters.IsDirty = _dirty;
                } 
                _cachedMetaData = null;
            }
        }
 
        internal int InternalRecordsAffected {
            get { 
                return _rowsAffected; 
            }
            set { 
                if (-1 == _rowsAffected) {
                    _rowsAffected = value;
                }
                else if (0 < value) { 
                    _rowsAffected += value;
                } 
            } 
        }
 
        internal bool BatchRPCMode {
            get {
                return _batchRPCMode;
            } 
            set {
                _batchRPCMode = value; 
 
                if (_batchRPCMode == false) {
                    ClearBatchCommand(); 
                } else {
                    if (_RPCList == null) {
                        _RPCList = new List<_SqlRPC>();
                    } 
                    if (_parameterCollectionList == null) {
                        _parameterCollectionList = new List<SqlParameterCollection>(); 
                    } 
                }
            } 
        }

        internal void ClearBatchCommand() {
            List<_SqlRPC> rpcList = _RPCList; 
            if (null != rpcList) {
                rpcList.Clear(); 
            } 
            if (null != _parameterCollectionList) {
                _parameterCollectionList.Clear(); 
            }
            _SqlRPCBatchArray = null;
            _currentlyExecutingBatch = 0;
        } 

        internal void AddBatchCommand(string commandText, SqlParameterCollection parameters, CommandType cmdType) { 
            Debug.Assert(BatchRPCMode, "Command is not in batch RPC Mode"); 
            Debug.Assert(_RPCList != null);
            Debug.Assert(_parameterCollectionList != null); 

            _SqlRPC  rpc = new _SqlRPC();

            this.CommandText = commandText; 
            this.CommandType = cmdType;
            GetStateObject(); 
            if (cmdType == CommandType.StoredProcedure) { 
                BuildRPC(false, parameters, ref rpc);
            } 
            else {
                // All batch sql statements must be executed inside sp_executesql, including those without parameters
                BuildExecuteSql(CommandBehavior.Default, commandText, parameters, ref rpc);
            } 
             _RPCList.Add(rpc);
             // Always add a parameters collection per RPC, even if there are no parameters. 
             _parameterCollectionList.Add(parameters); 
            PutStateObject();
        } 

        internal int ExecuteBatchRPCCommand() {

            Debug.Assert(BatchRPCMode, "Command is not in batch RPC Mode"); 
            Debug.Assert(_RPCList != null, "No batch commands specified");
            _SqlRPCBatchArray = _RPCList.ToArray(); 
            _currentlyExecutingBatch = 0; 
            return ExecuteNonQuery();       // Check permissions, execute, return output params
 
        }

        internal int? GetRecordsAffected(int commandIndex) {
            Debug.Assert(BatchRPCMode, "Command is not in batch RPC Mode"); 
            Debug.Assert(_SqlRPCBatchArray != null, "batch command have been cleared");
            return _SqlRPCBatchArray[commandIndex].recordsAffected; 
        } 

        internal SqlException GetErrors(int commandIndex) { 
            SqlException result = null;
            int length = (_SqlRPCBatchArray[commandIndex].errorsIndexEnd - _SqlRPCBatchArray[commandIndex].errorsIndexStart);
            if (0 < length) {
                SqlErrorCollection errors = new SqlErrorCollection(); 
                for(int i = _SqlRPCBatchArray[commandIndex].errorsIndexStart; i < _SqlRPCBatchArray[commandIndex].errorsIndexEnd; ++i) {
                    errors.Add(_SqlRPCBatchArray[commandIndex].errors[i]); 
                } 
                for(int i = _SqlRPCBatchArray[commandIndex].warningsIndexStart; i < _SqlRPCBatchArray[commandIndex].warningsIndexEnd; ++i) {
                    errors.Add(_SqlRPCBatchArray[commandIndex].warnings[i]); 
                }
                result = SqlException.CreateException(errors, Connection.ServerVersion);
            }
            return result; 
        }
 
        private void DisposeSmiRequest() { 
            if ( null != _smiRequest ) {
                SmiRequestExecutor smiRequest = _smiRequest; 
                _smiRequest = null;
                _smiRequestContext = null; // not entirely necessary, but good to do for debugging/GC
                smiRequest.Close(EventSink);
                EventSink.ProcessMessagesAndThrow(); 
            }
        } 
 
        // Allocates and initializes a new SmiRequestExecutor based on the current command state
        private void SetUpSmiRequest( SqlInternalConnectionSmi innerConnection ) { 

            // General Approach To Ensure Security of Marshalling:
            //        Only touch each item in the command once
            //        (i.e. only grab a reference to each param once, only 
            //        read the type from that param once, etc.).  The problem is
            //        that if the user changes something on the command in the 
            //        middle of marshaling, it can overwrite the native buffers 
            //        set up.  For example, if max length is used to allocate
            //        buffers, but then re-read from the parameter to truncate 
            //        strings, the user could extend the length and overwrite
            //        the buffer.

            // Clean up a bit first 
            //
 
 
                DisposeSmiRequest();
//            } 


            if (null != Notification){
                throw SQL.NotificationsNotAvailableOnContextConnection(); 
            }
 
            SmiParameterMetaData[] requestMetaData = null; 
            ParameterPeekAheadValue[] peekAheadValues = null;
 
            // Do we need to create a new request?
//            if ( null == _smiRequest ) {
                //    Length of rgMetadata becomes *the* official count of parameters to use,
                //      don't rely on Parameters.Count after this point, as the user could change it. 
                int count = GetParameterCount( Parameters );
                if ( 0 < count ) { 
                    requestMetaData = new SmiParameterMetaData[count]; 
                    peekAheadValues = new ParameterPeekAheadValue[count];
 
                    // set up the metadata
                    for ( int index=0; index<count; index++ ) {
                        SqlParameter param = Parameters[index];
#if WINFSFunctionality 
                        param.Validate(index, false); // SMI doesn't support LazyMat yet.
#else 
                        param.Validate(index, CommandType.StoredProcedure == CommandType); 
#endif
                        requestMetaData[index] = param.MetaDataForSmi(out peekAheadValues[index]); 

                        // Check for valid type for version negotiated
                        if (!innerConnection.IsKatmaiOrNewer) {
                            MetaType mt = MetaType.GetMetaTypeFromSqlDbType(requestMetaData[index].SqlDbType, requestMetaData[index].IsMultiValued); 
                            if (!mt.Is90Supported) {
                                throw ADP.VersionDoesNotSupportDataType(mt.TypeName); 
                            } 
                        }
                    } 
                }

                // Allocate the new request
                CommandType cmdType = CommandType; 
                _smiRequestContext = innerConnection.InternalContext;
                _smiRequest = _smiRequestContext.CreateRequestExecutor( 
                                        CommandText, 
                                        cmdType,
                                        requestMetaData, 
                                        EventSink
                                    );

                // deal with errors 
                EventSink.ProcessMessagesAndThrow();
//            } // 
 
            // Now assign param values
            for ( int index=0; index<count; index++ ) { 
                if ( ParameterDirection.Output != requestMetaData[index].Direction &&
                        ParameterDirection.ReturnValue != requestMetaData[index].Direction ) {
                    SqlParameter param = Parameters[index];
                    // going back to command for parameter is ok, since we'll only pick up values now. 
#if WINFSFunctionality
                    object value = param.GetCoercedValue(false); // SMI doesn't support LazyMat yet. 
#else 
                    object value = param.GetCoercedValue();
#endif 
                    ExtendedClrTypeCode typeCode = MetaDataUtilsSmi.DetermineExtendedTypeCodeForUseWithSqlDbType(requestMetaData[index].SqlDbType, requestMetaData[index].IsMultiValued, value, null /* parameters don't use CLR Type for UDTs */, SmiContextFactory.Instance.NegotiatedSmiVersion);

                    // Handle null reference as special case for parameters
                    if ( CommandType.StoredProcedure == cmdType && 
                                ExtendedClrTypeCode.Empty == typeCode ) {
                        _smiRequest.SetDefault( index ); 
                    } 
                    else {
                        // 



                        int size = param.Size; 
                        if (size != 0 && size != SmiMetaData.UnlimitedMaxLengthIndicator && !param.SizeInferred) {
                            switch(requestMetaData[index].SqlDbType) { 
                                case SqlDbType.Image: 
                                case SqlDbType.Text:
                                    if (size != Int32.MaxValue) { 
                                        throw SQL.ParameterSizeRestrictionFailure(index);
                                    }
                                    break;
 
                                case SqlDbType.NText:
                                    if (size != Int32.MaxValue/2) { 
                                        throw SQL.ParameterSizeRestrictionFailure(index); 
                                    }
                                    break; 

                                case SqlDbType.VarBinary:
                                case SqlDbType.VarChar:
                                    // Allow size==Int32.MaxValue because of DeriveParameters 
                                    if (size > 0 && size != Int32.MaxValue && requestMetaData[index].MaxLength == SmiMetaData.UnlimitedMaxLengthIndicator) {
                                        throw SQL.ParameterSizeRestrictionFailure(index); 
                                    } 
                                    break;
 
                                case SqlDbType.NVarChar:
                                    // Allow size==Int32.MaxValue/2 because of DeriveParameters
                                    if (size > 0 && size != Int32.MaxValue/2 && requestMetaData[index].MaxLength == SmiMetaData.UnlimitedMaxLengthIndicator) {
                                        throw SQL.ParameterSizeRestrictionFailure(index); 
                                    }
                                    break; 
 
                                case SqlDbType.Timestamp:
                                    // Size limiting for larger values will happen due to MaxLength 
                                    if (size < SmiMetaData.DefaultTimestamp.MaxLength) {
                                        throw SQL.ParameterSizeRestrictionFailure(index);
                                    }
                                    break; 

                                case SqlDbType.Variant: 
                                    // Variant problems happen when Size is less than maximums for character and binary values 
                                    // Size limiting for larger values will happen due to MaxLength
                                    // NOTE: assumes xml and udt types are handled in parameter value coercion 
                                    //      since server does not allow these types in a variant
                                    if (null != value) {
                                        MetaType mt = MetaType.GetMetaTypeFromValue(value);
 
                                        if ((mt.IsNCharType && size < SmiMetaData.MaxUnicodeCharacters) ||
                                                (mt.IsBinType && size < SmiMetaData.MaxBinaryLength) || 
                                                (mt.IsAnsiType && size < SmiMetaData.MaxANSICharacters)) { 
                                            throw SQL.ParameterSizeRestrictionFailure(index);
                                        } 
                                    }
                                    break;

                                 case SqlDbType.Xml: 
                                    // Xml is an issue for non-SqlXml types
                                    if (null != value && ExtendedClrTypeCode.SqlXml != typeCode) { 
                                        throw SQL.ParameterSizeRestrictionFailure(index); 
                                    }
                                    break; 

                                 // NOTE: Char, NChar, Binary and UDT do not need restricting because they are always 8k or less,
                                 //         so the metadata MaxLength will match the Size setting.
 
                                default:
                                    break; 
                            } 
                        }
 
                        if (innerConnection.IsKatmaiOrNewer) {
                            ValueUtilsSmi.SetCompatibleValueV200(EventSink, _smiRequest, index, requestMetaData[index], value, typeCode, param.Offset, param.Size, peekAheadValues[index]);
                        }
                        else { 
                            ValueUtilsSmi.SetCompatibleValue( EventSink, _smiRequest, index, requestMetaData[index], value, typeCode, param.Offset );
                        } 
                    } 
                }
            } 
        }
    }
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlCommand.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="true" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.SqlClient { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.Configuration.Assemblies;
    using System.Data; 
    using System.Data.Common; 
    using System.Data.ProviderBase;
    using System.Data.Sql; 
    using System.Data.SqlTypes;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO; 
    using System.Reflection;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.Serialization.Formatters;
    using System.Security.Permissions; 
    using System.Text;
    using System.Threading;
    using System.Xml;
 
    using Microsoft.SqlServer.Server;
 
    [ 
    DefaultEvent("RecordsAffected"),
    ToolboxItem(true), 
    Designer("Microsoft.VSDesigner.Data.VS.SqlCommandDesigner, " + AssemblyRef.MicrosoftVSDesigner)
    ]
#if WINFSInternalOnly
    internal 
#else
    public 
#endif 
    sealed class SqlCommand : DbCommand, ICloneable {
 
        private  static int     _objectTypeCount; // Bid counter
        internal readonly int   ObjectID = System.Threading.Interlocked.Increment(ref _objectTypeCount);

        private string          _commandText; 
        private CommandType     _commandType;
        private int             _commandTimeout = ADP.DefaultCommandTimeout; 
        private UpdateRowSource _updatedRowSource = UpdateRowSource.Both; 
        private bool            _designTimeInvisible;
        internal SqlDependency  _sqlDep; 

        // devnote: Prepare
        // Against 7.0 Server (Sphinx) a prepare/unprepare requires an extra roundtrip to the server.
        // 
        // From 8.0 (Shiloh) and above (Yukon) the preparation can be done as part of the command execution.
        // 
        private enum EXECTYPE { 
            UNPREPARED,         // execute unprepared commands, all server versions (results in sp_execsql call)
            PREPAREPENDING,     // prepare and execute command, 8.0 and above only  (results in sp_prepexec call) 
            PREPARED,           // execute prepared commands, all server versions   (results in sp_exec call)
        }

        // devnotes 
        //
        // _hiddenPrepare 
        // On 8.0 and above the Prepared state cannot be left. Once a command is prepared it will always be prepared. 
        // A change in parameters, commandtext etc (IsDirty) automatically causes a hidden prepare
        // 
        // _inPrepare will be set immediately before the actual prepare is done.
        // The OnReturnValue function will test this flag to determine whether the returned value is a _prepareHandle or something else.
        //
        // _prepareHandle - the handle of a prepared command. Apparently there can be multiple prepared commands at a time - a feature that we do not support yet. 

        private bool _inPrepare         = false; 
        private int  _prepareHandle     = -1; 
        private bool _hiddenPrepare     = false;
 
        private SqlParameterCollection _parameters;
        private SqlConnection          _activeConnection;
        private bool                   _dirty            = false;               // true if the user changes the commandtext or number of parameters after the command is already prepared
        private EXECTYPE               _execType         = EXECTYPE.UNPREPARED; // by default, assume the user is not sharing a connection so the command has not been prepared 
        private _SqlRPC[]              _rpcArrayOf1      = null;                // Used for RPC executes
 
        // cut down on object creation and cache all these 
        // cached metadata
        private _SqlMetaDataSet _cachedMetaData; 

        // Cached info for async executions
        private class CachedAsyncState {
            private int           _cachedAsyncCloseCount = -1;    // value of the connection's CloseCount property when the asyncResult was set; tracks when connections are closed after an async operation 
            private DbAsyncResult _cachedAsyncResult     = null;
            private SqlConnection _cachedAsyncConnection = null;  // Used to validate that the connection hasn't changed when end the connection; 
            private SqlDataReader _cachedAsyncReader     = null; 
            private RunBehavior   _cachedRunBehavior     = RunBehavior.ReturnImmediately;
            private string        _cachedSetOptions      = null; 

            internal CachedAsyncState () {
            }
 
            internal SqlDataReader CachedAsyncReader {
                get {return _cachedAsyncReader;} 
            } 
            internal  RunBehavior CachedRunBehavior {
                get {return _cachedRunBehavior;} 
            }
            internal  string CachedSetOptions {
                get {return _cachedSetOptions;}
            } 
            internal bool PendingAsyncOperation {
                get {return (null != _cachedAsyncResult);} 
            } 

            internal bool IsActiveConnectionValid(SqlConnection activeConnection) { 
                return (_cachedAsyncConnection == activeConnection && _cachedAsyncCloseCount == activeConnection.CloseCount);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
            internal void ResetAsyncState() {
                _cachedAsyncCloseCount = -1; 
                _cachedAsyncResult     = null; 
                if (_cachedAsyncConnection != null) {
                    _cachedAsyncConnection.AsycCommandInProgress = false; 
                    _cachedAsyncConnection = null;
                }
                _cachedAsyncReader     = null;
                _cachedRunBehavior     = RunBehavior.ReturnImmediately; 
                _cachedSetOptions      = null;
            } 
 
            internal void SetActiveConnectionAndResult(DbAsyncResult result, SqlConnection activeConnection) {
                _cachedAsyncCloseCount = activeConnection.CloseCount; 
                _cachedAsyncResult     = result;
                if (null != activeConnection && !activeConnection.Parser.MARSOn) {
                    if (activeConnection.AsycCommandInProgress)
                        throw SQL.MARSUnspportedOnConnection(); 
                }
                Debug.Assert(activeConnection != null, "Unexpected null connection argument on SetActiveConnectionAndResult!"); 
                _cachedAsyncConnection = activeConnection; 

                // Should only be needed for non-MARS, but set anyways. 
                _cachedAsyncConnection.AsycCommandInProgress = true;
            }

            internal void SetAsyncReaderState (SqlDataReader ds, RunBehavior runBehavior, string optionSettings) { 
                _cachedAsyncReader  = ds;
                _cachedRunBehavior  = runBehavior; 
                _cachedSetOptions   = optionSettings; 
            }
        } 

        CachedAsyncState _cachedAsyncState = null;

        private CachedAsyncState cachedAsyncState { 
            get {
                if (_cachedAsyncState == null) { 
                    _cachedAsyncState = new CachedAsyncState (); 
                }
                return  _cachedAsyncState; 
            }
        }

        // sql reader will pull this value out for each NextResult call.  It is not cumulative 
        // _rowsAffected is cumulative for ExecuteNonQuery across all rpc batches
        internal int _rowsAffected = -1; // rows affected by the command 
 
        private SqlNotificationRequest _notification;
        private bool _notificationAutoEnlist = true;            // Notifications auto enlistment is turned on by default 

        // transaction support
        private SqlTransaction _transaction;
 
        private StatementCompletedEventHandler _statementCompletedEventHandler;
 
        private TdsParserStateObject _stateObj; // this is the TDS session we're using. 

        // Volatile bool used to synchronize with cancel thread the state change of an executing 
        // command going from pre-processing to obtaining a stateObject.  The cancel synchronization
        // we require in the command is only from entering an Execute* API to obtaining a
        // stateObj.  Once a stateObj is successfully obtained, cancel synchronization is handled
        // by the stateObject. 
        private volatile bool _pendingCancel;
 
        private bool _batchRPCMode; 
        private List<_SqlRPC> _RPCList;
        private _SqlRPC[] _SqlRPCBatchArray; 
        private List<SqlParameterCollection>  _parameterCollectionList;
        private int     _currentlyExecutingBatch;

        // 
        //  Smi execution-specific stuff
        // 
        sealed private class CommandEventSink : SmiEventSink_Default { 
            private SqlCommand _command;
 
            internal CommandEventSink( SqlCommand command ) : base( ) {
                _command = command;
            }
 
            internal override void StatementCompleted( int rowsAffected ) {
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlCommand.CommandEventSink.StatementCompleted|ADV> %d#, rowsAffected=%d.\n", _command.ObjectID, rowsAffected); 
                }
                _command.InternalRecordsAffected = rowsAffected; 

//

 

 
            } 

            internal override void BatchCompleted() { 
                if (Bid.AdvancedOn) {
                    Bid.Trace("<sc.SqlCommand.CommandEventSink.BatchCompleted|ADV> %d#.\n", _command.ObjectID);
                }
            } 

            internal override void ParametersAvailable( SmiParameterMetaData[] metaData, ITypedGettersV3 parameterValues ) { 
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlCommand.CommandEventSink.ParametersAvailable|ADV> %d# metaData.Length=%d.\n", _command.ObjectID, (null!=metaData)?metaData.Length:-1);
 
                    if (null != metaData) {
                        for (int i=0; i < metaData.Length; i++) {
                            Bid.Trace("<sc.SqlCommand.CommandEventSink.ParametersAvailable|ADV> %d#, metaData[%d] is %s%s\n",
                                        _command.ObjectID, i, metaData[i].GetType().ToString(), metaData[i].TraceString()); 
                        }
                    } 
                } 
                Debug.Assert(SmiContextFactory.Instance.NegotiatedSmiVersion >= SmiContextFactory.YukonVersion);
                _command.OnParametersAvailableSmi( metaData, parameterValues ); 
            }

            internal override void ParameterAvailable(SmiParameterMetaData metaData, SmiTypedGetterSetter parameterValues, int ordinal)
            { 
                if (Bid.AdvancedOn) {
                    if (null != metaData) { 
                        Bid.Trace("<sc.SqlCommand.CommandEventSink.ParameterAvailable|ADV> %d#, metaData[%d] is %s%s\n", 
                                    _command.ObjectID, ordinal, metaData.GetType().ToString(), metaData.TraceString());
                    } 
                }
                Debug.Assert(SmiContextFactory.Instance.NegotiatedSmiVersion >= SmiContextFactory.KatmaiVersion);
                _command.OnParameterAvailableSmi(metaData, parameterValues, ordinal);
            } 
        }
 
        private SmiRequestExecutor      _smiRequest; 
        private SmiContext              _smiRequestContext; // context that _smiRequest came from
        private CommandEventSink _smiEventSink; 
        private SmiEventSink_DeferedProcessing _outParamEventSink;
        private CommandEventSink EventSink {
            get {
                if ( null == _smiEventSink ) { 
                    _smiEventSink = new CommandEventSink( this );
                } 
 
                _smiEventSink.Parent = InternalSmiConnection.CurrentEventSink;
                return _smiEventSink; 
            }
        }

        private SmiEventSink_DeferedProcessing OutParamEventSink { 
            get {
                if (null == _outParamEventSink) { 
                    _outParamEventSink = new SmiEventSink_DeferedProcessing(EventSink); 
                }
                else { 
                    _outParamEventSink.Parent = EventSink;
                }

                return _outParamEventSink; 
            }
        } 
 

        public SqlCommand() : base() { 
            GC.SuppressFinalize(this);
        }

        public SqlCommand(string cmdText) : this() { 
            CommandText = cmdText;
        } 
 
        public SqlCommand(string cmdText, SqlConnection connection) : this() {
            CommandText = cmdText; 
            Connection = connection;
        }

        public SqlCommand(string cmdText, SqlConnection connection, SqlTransaction transaction) : this() { 
            CommandText = cmdText;
            Connection = connection; 
            Transaction = transaction; 
        }
 
        private SqlCommand(SqlCommand from) : this() { // Clone
            CommandText = from.CommandText;
            CommandTimeout = from.CommandTimeout;
            CommandType = from.CommandType; 
            Connection = from.Connection;
            DesignTimeVisible = from.DesignTimeVisible; 
            Transaction = from.Transaction; 
            UpdatedRowSource = from.UpdatedRowSource;
 
            SqlParameterCollection parameters = Parameters;
            foreach(object parameter in from.Parameters) {
                parameters.Add((parameter is ICloneable) ? (parameter as ICloneable).Clone() : parameter);
            } 
        }
 
        [ 
        DefaultValue(null),
        Editor("Microsoft.VSDesigner.Data.Design.DbConnectionEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_Connection),
        ]
        new public SqlConnection Connection { 
            get {
                return _activeConnection; 
            } 
            set {
                // Don't allow the connection to be changed while in a async opperation. 
                if (_activeConnection != value && _activeConnection != null) { // If new value...
                    if (cachedAsyncState.PendingAsyncOperation) { // If in pending async state, throw.
                        throw SQL.CannotModifyPropertyAsyncOperationInProgress(SQL.Connection);
                    } 
                }
                // Check to see if the currently set transaction has completed.  If so, 
                // null out our local reference. 
                if (null != _transaction && _transaction.Connection == null)
                    _transaction = null; 
                _activeConnection = value; //
                Bid.Trace("<sc.SqlCommand.set_Connection|API> %d#, %d#\n", ObjectID, ((null != value) ? value.ObjectID : -1));
            }
        } 

        override protected DbConnection DbConnection { // V1.2.3300 
            get { 
                return Connection;
            } 
            set {
                Connection = (SqlConnection)value;
            }
        } 

        private SqlInternalConnectionSmi InternalSmiConnection { 
            get { 
                return (SqlInternalConnectionSmi)_activeConnection.InnerConnection;
            } 
        }

        private SqlInternalConnectionTds InternalTdsConnection {
            get { 
                return (SqlInternalConnectionTds)_activeConnection.InnerConnection;
            } 
        } 

        private bool IsShiloh { 
            get {
                Debug.Assert(_activeConnection != null, "The active connection is null!");
                if (_activeConnection == null)
                    return false; 
                return _activeConnection.IsShiloh;
            } 
        } 

        [ 
        DefaultValue(true),
        ResCategoryAttribute(Res.DataCategory_Notification),
        ResDescriptionAttribute(Res.SqlCommand_NotificationAutoEnlist),
        ] 
        public bool NotificationAutoEnlist {
            get { 
                return _notificationAutoEnlist; 
            }
            set { 
                _notificationAutoEnlist = value;
            }
         }
 
        [
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), // MDAC 90471 
        ResCategoryAttribute(Res.DataCategory_Notification),
        ResDescriptionAttribute(Res.SqlCommand_Notification), 
        ]
        public SqlNotificationRequest Notification {
            get {
                return _notification; 
            }
            set { 
                Bid.Trace("<sc.SqlCommand.set_Notification|API> %d#\n", ObjectID); 
                _sqlDep = null;
                _notification = value; 
            }
        }

 
        internal SqlStatistics Statistics {
            get { 
                if (null != _activeConnection) { 
                    if (_activeConnection.StatisticsEnabled) {
                        return _activeConnection.Statistics; 
                    }
                }
                return null;
            } 
        }
 
        [ 
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ResDescriptionAttribute(Res.DbCommand_Transaction),
        ]
        new public SqlTransaction Transaction {
            get { 
                // if the transaction object has been zombied, just return null
                if ((null != _transaction) && (null == _transaction.Connection)) { // MDAC 72720 
                    _transaction = null; 
                }
                return _transaction; 
            }
            set {
                // Don't allow the transaction to be changed while in a async opperation.
                if (_transaction != value && _activeConnection != null) { // If new value... 
                    if (cachedAsyncState.PendingAsyncOperation) { // If in pending async state, throw
                        throw SQL.CannotModifyPropertyAsyncOperationInProgress(SQL.Transaction); 
                    } 
                }
 
                //
                Bid.Trace("<sc.SqlCommand.set_Transaction|API> %d#\n", ObjectID);
                _transaction = value;
            } 
        }
 
        override protected DbTransaction DbTransaction { // V1.2.3300 
            get {
                return Transaction; 
            }
            set {
                Transaction = (SqlTransaction)value;
            } 
        }
 
        [ 
        DefaultValue(""),
        Editor("Microsoft.VSDesigner.Data.SQL.Design.SqlCommandTextEditor, " + AssemblyRef.MicrosoftVSDesigner, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing), 
        RefreshProperties(RefreshProperties.All), // MDAC 67707
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_CommandText),
        ] 
        override public string CommandText { // V1.2.3300, XXXCommand V1.0.5000
            get { 
                string value = _commandText; 
                return ((null != value) ? value : ADP.StrEmpty);
            } 
            set {
                if (Bid.TraceOn) {
                    Bid.Trace("<sc.SqlCommand.set_CommandText|API> %d#, '", ObjectID);
                    Bid.PutStr(value); // Use PutStr to write out entire string 
                    Bid.Trace("'\n");
                } 
                if (0 != ADP.SrcCompare(_commandText, value)) { 
                    PropertyChanging();
                    _commandText = value; 
                }
            }
        }
 
        [
        ResCategoryAttribute(Res.DataCategory_Data), 
        ResDescriptionAttribute(Res.DbCommand_CommandTimeout), 
        ]
        override public int CommandTimeout { // V1.2.3300, XXXCommand V1.0.5000 
            get {
                return _commandTimeout;
            }
            set { 
                Bid.Trace("<sc.SqlCommand.set_CommandTimeout|API> %d#, %d\n", ObjectID, value);
                if (value < 0) { 
                    throw ADP.InvalidCommandTimeout(value); 
                }
                if (value != _commandTimeout) { 
                    PropertyChanging();
                    _commandTimeout = value;
                }
            } 
        }
 
        public void ResetCommandTimeout() { // V1.2.3300 
            if (ADP.DefaultCommandTimeout != _commandTimeout) {
                PropertyChanging(); 
                _commandTimeout = ADP.DefaultCommandTimeout;
            }
        }
 
        private bool ShouldSerializeCommandTimeout() { // V1.2.3300
            return (ADP.DefaultCommandTimeout != _commandTimeout); 
        } 

        [ 
        DefaultValue(System.Data.CommandType.Text),
        RefreshProperties(RefreshProperties.All),
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbCommand_CommandType), 
        ]
        override public CommandType CommandType { // V1.2.3300, XXXCommand V1.0.5000 
            get { 
                CommandType cmdType = _commandType;
                return ((0 != cmdType) ? cmdType : CommandType.Text); 
            }
            set {
                Bid.Trace("<sc.SqlCommand.set_CommandType|API> %d#, %d{ds.CommandType}\n", ObjectID, (int)value);
                if (_commandType != value) { 
                    switch(value) { // @perfnote: Enum.IsDefined
                    case CommandType.Text: 
                    case CommandType.StoredProcedure: 
                        PropertyChanging();
                        _commandType = value; 
                        break;
                    case System.Data.CommandType.TableDirect:
                        throw SQL.NotSupportedCommandType(value);
                    default: 
                        throw ADP.InvalidCommandType(value);
                    } 
                } 
            }
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
        public override bool DesignTimeVisible { // V1.2.3300, XXXCommand V1.0.5000
            get { 
                return !_designTimeInvisible;
            } 
            set { 
                _designTimeInvisible = !value;
                TypeDescriptor.Refresh(this); // VS7 208845 
            }
        }

        [ 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        ResCategoryAttribute(Res.DataCategory_Data), 
        ResDescriptionAttribute(Res.DbCommand_Parameters), 
        ]
        new public SqlParameterCollection Parameters { 
            get {
                if (null == this._parameters) {
                    // delay the creation of the SqlParameterCollection
                    // until user actually uses the Parameters property 
                    this._parameters = new SqlParameterCollection();
                } 
                return this._parameters; 
            }
        } 

        override protected DbParameterCollection DbParameterCollection { // V1.2.3300
            get {
                return Parameters; 
            }
        } 
 
        [
        DefaultValue(System.Data.UpdateRowSource.Both), 
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbCommand_UpdatedRowSource),
        ]
        override public UpdateRowSource UpdatedRowSource { // V1.2.3300, XXXCommand V1.0.5000 
            get {
                return _updatedRowSource; 
            } 
            set {
                switch(value) { // @perfnote: Enum.IsDefined 
                case UpdateRowSource.None:
                case UpdateRowSource.OutputParameters:
                case UpdateRowSource.FirstReturnedRecord:
                case UpdateRowSource.Both: 
                    _updatedRowSource = value;
                    break; 
                default: 
                    throw ADP.InvalidUpdateRowSource(value);
                } 
            }
        }

        [ 
        ResCategoryAttribute(Res.DataCategory_StatementCompleted),
        ResDescriptionAttribute(Res.DbCommand_StatementCompleted), 
        ] 
        public event StatementCompletedEventHandler StatementCompleted {
            add { 
                _statementCompletedEventHandler += value;
            }
            remove {
                _statementCompletedEventHandler -= value; 
            }
        } 
 
        internal void OnStatementCompleted(int recordCount) { // V1.2.3300
             if (0 <= recordCount) { 
                StatementCompletedEventHandler handler = _statementCompletedEventHandler;
                if (null != handler) {
                    try {
                       Bid.Trace("<sc.SqlCommand.OnStatementCompleted|INFO> %d#, recordCount=%d\n", ObjectID, recordCount); 
                        handler(this, new StatementCompletedEventArgs(recordCount));
                    } 
                    catch(Exception e) { 
                        //
                        if (!ADP.IsCatchableOrSecurityExceptionType(e)) { 
                            throw;
                        }

                        ADP.TraceExceptionWithoutRethrow(e); 
                    }
                } 
            } 
        }
 
        private void PropertyChanging() { // also called from SqlParameterCollection
            this.IsDirty = true;
        }
 
        override public void Prepare() {
            SqlConnection.ExecutePermission.Demand(); 
 
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject. 
            _pendingCancel = false;

            // Context connection's prepare is a no-op
            if (_activeConnection.IsContextConnection) { 
                return;
            } 
 
            SqlStatistics statistics = null;
            IntPtr hscp; 
            SqlDataReader r = null;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.Prepare|API> %d#", ObjectID);
            statistics = SqlStatistics.StartTimer(Statistics);
 
            // only prepare if batch with parameters
            // MDAC 
            if ( 
                this.IsPrepared && !this.IsDirty
                || (this.CommandType == CommandType.StoredProcedure) 
                ||  (
                        (System.Data.CommandType.Text == this.CommandType)
                        && (0 == GetParameterCount (_parameters))
                    ) 

            ) { 
                if (null != Statistics) { 
                    Statistics.SafeIncrement (ref Statistics._prepares);
                } 
                _hiddenPrepare = false;
            }
            else {
                bool processFinallyBlock = true; 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    ValidateCommand(ADP.Prepare, false /*not async*/); 

                    GetStateObject(); 

                    // Loop through parameters ensuring that we do not have unspecified types, sizes, scales, or precisions
                    if (null != _parameters) {
                        int count = _parameters.Count; 
                        for (int i = 0; i < count; ++i) {
                            _parameters[i].Prepare(this); // MDAC 67063 
                        } 
                    }
 
#if DEBUG
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { 
                        Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                        r = InternalPrepare(0); 
#if DEBUG
                    } 
                    finally {
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                    }
#endif //DEBUG 
                }
                catch (System.OutOfMemoryException e) { 
                    processFinallyBlock = false; 
                    _activeConnection.Abort(e);
                    throw; 
                }
                catch (System.StackOverflowException e) {
                    processFinallyBlock = false;
                    _activeConnection.Abort(e); 
                    throw;
                } 
                catch (System.Threading.ThreadAbortException e)  { 
                    processFinallyBlock = false;
                    _activeConnection.Abort(e); 
                    throw;
                }
                catch (Exception e) {
                    processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                    throw;
                } 
                finally { 
                    if (processFinallyBlock) {
                        _hiddenPrepare = false; // The command is now officially prepared 

                        if (r != null) {
                            _cachedMetaData = r.MetaData;
                            r.Close(); 
                        }
                        PutStateObject(); 
                    } 
                }
            } 

            SqlStatistics.StopTimer(statistics);
            Bid.ScopeLeave(ref hscp);
        } 

        private SqlDataReader InternalPrepare(CommandBehavior behavior) { 
            SqlDataReader r = null; 

            if (this.IsDirty) { 
                Debug.Assert(_cachedMetaData == null, "dirty query should not have cached metadata!");
                //
                // someone changed the command text or the parameter schema so we must unprepare the command
                // 
                this.Unprepare(false);
                this.IsDirty = false; 
            } 
            Debug.Assert(_execType != EXECTYPE.PREPARED, "Invalid attempt to Prepare already Prepared command!");
            Debug.Assert(_activeConnection != null, "must have an open connection to Prepare"); 
            Debug.Assert(null != _stateObj, "TdsParserStateObject should not be null");
            Debug.Assert(null != _stateObj.Parser, "TdsParser class should not be null in Command.Execute!");
            Debug.Assert(_stateObj.Parser == _activeConnection.Parser, "stateobject parser not same as connection parser");
            Debug.Assert(false == _inPrepare, "Already in Prepare cycle, this.inPrepare should be false!"); 

            if (_activeConnection.IsShiloh) { 
                // In Shiloh, remember that the user wants to do a prepare 
                // but don't actually do an rpc
                _execType = EXECTYPE.PREPAREPENDING; 

                // return null results
            }
            else { 
                _SqlRPC rpc = BuildPrepare(behavior);
                _inPrepare = true; 
                Debug.Assert(_activeConnection.State == ConnectionState.Open, "activeConnection must be open!"); 
                r = new SqlDataReader(this, behavior);
                try { 
                    Debug.Assert(_rpcArrayOf1[0] == rpc);
                    _stateObj.Parser.TdsExecuteRPC(_rpcArrayOf1, this.CommandTimeout, false, null, _stateObj, CommandType.StoredProcedure == CommandType);
                    _stateObj.Parser.Run(RunBehavior.UntilDone, this, r, null, _stateObj);
                } 
                catch {
                    // In case Prepare fails, cleanup and then throw. 
                    _inPrepare = false; 
                    throw;
                } 

                r.Bind(_stateObj);
                Debug.Assert(-1 != _prepareHandle, "Handle was not filled in!");
                _execType = EXECTYPE.PREPARED; 
               Bid.Trace("<sc.SqlCommand.Prepare|INFO> %d#, Command prepared.\n", ObjectID);
            } 
 
            if (null != Statistics) {
                Statistics.SafeIncrement(ref Statistics._prepares); 
            }

            // let the connection know that it needs to unprepare the command on close
            _activeConnection.AddPreparedCommand(this); 
            return r;
        } 
 
        // SqlInternalConnectionTds needs to be able to unprepare a statement
        internal void Unprepare(bool isClosing) { 
            // Context connection's prepare is a no-op
            if (_activeConnection.IsContextConnection) {
                return;
            } 

            bool obtainedStateObj = false; 
            bool processFinallyBlock = true; 
            try {
                if (null == _stateObj) { 
                    GetStateObject();
                    obtainedStateObj = true;
                }
                InternalUnprepare(isClosing); 
            }
            catch (Exception e) { 
                processFinallyBlock = ADP.IsCatchableExceptionType (e); 
                throw;
            } 
            finally {
                Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to Unprepare");  // you need to setup for a thread abort somewhere before you call this method
                if (processFinallyBlock && obtainedStateObj) {
                    PutStateObject(); 
                }
            } 
        } 

        private void InternalUnprepare(bool isClosing) { 
            Debug.Assert(true == IsPrepared, "Invalid attempt to Unprepare a non-prepared command!");
            Debug.Assert(_activeConnection != null, "must have an open connection to UnPrepare");
            Debug.Assert(null != _stateObj, "TdsParserStateObject should not be null");
            Debug.Assert(null != _stateObj.Parser, "TdsParser class should not be null in Command.Unprepare!"); 
            Debug.Assert(_stateObj.Parser == _activeConnection.Parser, "stateobject parser not same as connection parser");
            Debug.Assert(false == _inPrepare, "_inPrepare should be false!"); 
 
            // In 7.0, unprepare always.  In 7.x, only unprepare if the connection is closing since sp_prepexec will
            // unprepare the last used handle 
            if (IsShiloh) {

                // @devnote: we're always falling back to Prepare pending
                // @devnote: This seems broken because once the command is prepared it will - always - be a 
                // @devnote: prepared execution.
                // @devnote: Even replacing the parameterlist with something completely different or 
                // @devnote: changing the commandtext to a non-parameterized query will result in prepared execution 
                // @devnote:
                // @devnote: We need to keep the behavior for backward compatibility though (non-breaking change) 
                //
                _execType = EXECTYPE.PREPAREPENDING;
                // @devnote:  Don't zero out the handle because we'll pass it in to sp_prepexec on the
                // @devnote:  next prepare, unless closing the connection when the server will drop the handle anyway. 
                if (isClosing) {
                    // reset our handle 
                    _prepareHandle = -1; 
                }
            } 
            else {
                if (_prepareHandle != -1) {
                    _SqlRPC rpc = BuildUnprepare();
                    Debug.Assert(_rpcArrayOf1[0] == rpc); 
                    _stateObj.Parser.TdsExecuteRPC(_rpcArrayOf1, this.CommandTimeout, false, null, _stateObj, CommandType.StoredProcedure == CommandType);
                    _stateObj.Parser.Run(RunBehavior.UntilDone, this, null, null, _stateObj); 
 
                    // reset our handle
                    _prepareHandle = -1; 
                }
                // reset our execType since nothing is prepared
                _execType = EXECTYPE.UNPREPARED;
            } 

            _cachedMetaData = null; 
            if (!isClosing) {   // if isClosing, the connection will remove the command 
                _activeConnection.RemovePreparedCommand(this);
            } 
            Bid.Trace("<sc.SqlCommand.Prepare|INFO> %d#, Command unprepared.\n", ObjectID);
        }

 
        // Cancel is supposed to be multi-thread safe.
        // It doesn't make sense to verify the connection exists or that it is open during cancel 
        // because immediately after checkin the connection can be closed or removed via another thread. 
        //
        override public void Cancel() { 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.Cancel|API> %d#", ObjectID);

            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
 
                // the pending data flag means that we are awaiting a response or are in the middle of proccessing a response
                // if we have no pending data, then there is nothing to cancel 
                // if we have pending data, but it is not a result of this command, then we don't cancel either.  Note that
                // this model is implementable because we only allow one active command at any one time.  This code
                // will have to change we allow multiple outstanding batches
 
                //
                if (null == _activeConnection) { 
                    return; 
                }
                SqlInternalConnectionTds connection = (_activeConnection.InnerConnection as SqlInternalConnectionTds); 
                if (null == connection) {  // Fail with out locking
                     return;
                }
 
                // The lock here is to protect against the command.cancel / connection.close race condition
                // The SqlInternalConnectionTds is set to OpenBusy during close, once this happens the cast below will fail and 
                // the command will no longer be cancelable.  It might be desirable to be able to cancel the close opperation, but this is 
                // outside of the scope of Whidbey RTM.  See (SqlConnection::Close) for other lock.
                lock (connection) { 
                    if (connection != (_activeConnection.InnerConnection as SqlInternalConnectionTds)) { // make sure the connection held on the active connection is what we have stored in our temp connection variable, if not between getting "connection" and takeing the lock, the connection has been closed
                        return;
                    }
 
                    TdsParser parser = connection.Parser;
                    if (null == parser) { 
                        return; 
                    }
 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try {
#if DEBUG
                        object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                        RuntimeHelpers.PrepareConstrainedRegions(); 
                        try { 
                            Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                            if (!_pendingCancel) { // Do nothing if aleady pending.
                                // Before attempting actual cancel, set the _pendingCancel flag to false.
                                // This denotes to other thread before obtaining stateObject from the
                                // session pool that there is another thread wishing to cancel. 
                                // The period in question is between entering the ExecuteAPI and obtaining
                                // a stateObject. 
                                _pendingCancel = true; 

                                TdsParserStateObject stateObj = _stateObj; 
                                if (null != _stateObj) {
                                    _stateObj.Cancel(ObjectID);
                                }
                                else { 
                                    SqlDataReader reader = connection.FindLiveReader(this);
                                    if (reader != null) { 
                                        reader.Cancel(ObjectID); 
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
                        _activeConnection.Abort(e);
                        throw;
                    }
                    catch (System.StackOverflowException e) { 
                        _activeConnection.Abort(e);
                        throw; 
                    } 
                    catch (System.Threading.ThreadAbortException e)  {
                        _activeConnection.Abort(e); 
                        throw;
                    }
                }
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp); 
            }
        } 

        new public SqlParameter CreateParameter() {
            return new SqlParameter();
        } 

        override protected DbParameter CreateDbParameter() { 
            return CreateParameter(); 
        }
 
        override protected void Dispose(bool disposing) {
            if (disposing) { // release mananged objects

                // V1.0, V1.1 did not reset the Connection, Parameters, CommandText, WebData 100524 
                //_parameters = null;
                //_activeConnection = null; 
                //_statistics = null; 
                //CommandText = null;
                _cachedMetaData = null; 
            }
            // release unmanaged objects
            base.Dispose(disposing);
        } 

        override public object ExecuteScalar() { 
            SqlConnection.ExecutePermission.Demand(); 

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state 
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            SqlStatistics statistics = null; 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteScalar|API> %d#", ObjectID); 
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                SqlDataReader ds = RunExecuteReader(0, RunBehavior.ReturnImmediately, true, ADP.ExecuteScalar); 
                return CompleteExecuteScalar(ds, false);
            }
            finally {
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp);
            } 
        } 

        private object CompleteExecuteScalar(SqlDataReader ds, bool returnSqlValue) { 
            object retResult = null;

            try {
                if (ds.Read()) { 
                    if (ds.FieldCount > 0) {
                        if (returnSqlValue) { 
                            retResult = ds.GetSqlValue(0); 
                        }
                        else { 
                            retResult = ds.GetValue(0);
                        }
                    }
                } 
            }
            finally { 
                // clean off the wire 
                ds.Close();
            } 

            return retResult;
        }
 
        override public int ExecuteNonQuery() {
            SqlConnection.ExecutePermission.Demand(); 
 
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject. 
            _pendingCancel = false;

            SqlStatistics statistics = null;
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteNonQuery|API> %d#", ObjectID);
            try { 
                statistics = SqlStatistics.StartTimer(Statistics); 
                return InternalExecuteNonQuery(null, ADP.ExecuteNonQuery, false);
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp);
            } 
        }
 
        // Handles in-proc execute-to-pipe functionality 
        //  Identical to ExecuteNonQuery
        internal void ExecuteToPipe( SmiContext pipeContext ) { 
            SqlConnection.ExecutePermission.Demand();

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject. 
            _pendingCancel = false;
 
            SqlStatistics statistics = null; 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteToPipe|INFO> %d#", ObjectID); 
            try {
                statistics = SqlStatistics.StartTimer(Statistics);
                InternalExecuteNonQuery(null, ADP.ExecuteNonQuery, true);
            } 
            finally {
                SqlStatistics.StopTimer(statistics); 
                Bid.ScopeLeave(ref hscp); 
            }
        } 

        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteNonQuery() {
            // BeginExecuteNonQuery will track ExecutionTime for us 
            return BeginExecuteNonQuery(null, null);
        } 
 
        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteNonQuery(AsyncCallback callback, object stateObject) { 
            SqlConnection.ExecutePermission.Demand();

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject. 
            _pendingCancel = false;
 
            ValidateAsyncCommand(); // Special case - done outside of try/catches to prevent putting a stateObj 
                                    // back into pool when we should not.
 
            SqlStatistics statistics = null;
            try {
                statistics = SqlStatistics.StartTimer(Statistics);
                ExecutionContext execContext = (callback == null) ? null : ExecutionContext.Capture(); 
                DbAsyncResult result = new DbAsyncResult(this, ADP.EndExecuteNonQuery, callback, stateObject, execContext);
 
                try { // InternalExecuteNonQuery already has reliability block, but if failure will not put stateObj back into pool. 
                    InternalExecuteNonQuery(result, ADP.BeginExecuteNonQuery, false);
                } 
                catch (Exception e) {
                    if (!ADP.IsCatchableOrSecurityExceptionType(e)) {
                        // If not catchable - the connection has already been caught and doomed in RunExecuteReader.
                        throw; 
                    }
 
                    // For async, RunExecuteReader will never put the stateObj back into the pool, so do so now. 
                    PutStateObject();
                    throw; 
                }

                // Read SNI does not have catches for async exceptions, handle here.
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
#if DEBUG 
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try {
                        Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                        // must finish caching information before ReadSni which can activate the callback before returning 
                        cachedAsyncState.SetActiveConnectionAndResult(result, _activeConnection);
                        _stateObj.ReadSni(result, _stateObj); 
#if DEBUG 
                    }
                    finally { 
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                    }
#endif //DEBUG
                } 
                catch (System.OutOfMemoryException e) {
                    _activeConnection.Abort(e); 
                    throw; 
                }
                catch (System.StackOverflowException e) { 
                    _activeConnection.Abort(e);
                    throw;
                }
                catch (System.Threading.ThreadAbortException e)  { 
                    _activeConnection.Abort(e);
                    throw; 
                } 
                catch (Exception) {
                    // Similarly, if an exception occurs put the stateObj back into the pool. 
                    // and reset async cache information to allow a second async execute
                    if (null != _cachedAsyncState) {
                        _cachedAsyncState.ResetAsyncState();
                    } 
                    PutStateObject();
                    throw; 
                } 
                return result;
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        private void VerifyEndExecuteState(DbAsyncResult dbAsyncResult, String endMethod) { 
            if (null == dbAsyncResult) { 
                throw ADP.ArgumentNull("asyncResult");
            } 
            if (dbAsyncResult.EndMethodName != endMethod) {
                throw ADP.MismatchedAsyncResult(dbAsyncResult.EndMethodName, endMethod);
            }
            if (!cachedAsyncState.IsActiveConnectionValid(_activeConnection)) { 
                throw ADP.CommandAsyncOperationCompleted();
            } 
 
            dbAsyncResult.CompareExchangeOwner(this, endMethod);
        } 

        private void WaitForAsyncResults(IAsyncResult asyncResult) {
            DbAsyncResult dbAsyncResult = (DbAsyncResult) asyncResult;
            if (!asyncResult.IsCompleted) { 
                asyncResult.AsyncWaitHandle.WaitOne();
            } 
            dbAsyncResult.Reset(); 
            _activeConnection.GetOpenTdsConnection().DecrementAsyncCount();
        } 

        public int EndExecuteNonQuery(IAsyncResult asyncResult) {
            SqlStatistics statistics = null;
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    statistics = SqlStatistics.StartTimer(Statistics);
                    VerifyEndExecuteState((DbAsyncResult) asyncResult, ADP.EndExecuteNonQuery); 
                    WaitForAsyncResults(asyncResult); 

                    bool processFinallyBlock = true; 
                    try {
                        NotifyDependency();
                        CheckThrowSNIException();
 
                        // only send over SQL Batch command if we are not a stored proc and have no parameters
                        if ((System.Data.CommandType.Text == this.CommandType) && (0 == GetParameterCount(_parameters))) { 
                            try { 
                                _stateObj.Parser.Run(RunBehavior.UntilDone, this, null, null, _stateObj);
                            } 
                            finally {
                                cachedAsyncState.ResetAsyncState();
                            }
                        } 
                        else  { // otherwise, use full-blown execute which can handle params and stored procs
                            SqlDataReader reader = CompleteAsyncExecuteReader(); 
                            if (null != reader) { 
                                reader.Close();
                            } 
                        }
                    }
                    catch (Exception e) {
                        processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                        throw;
                    } 
                    finally { 
                        if (processFinallyBlock) {
                            PutStateObject(); 
                        }
                    }

                    Debug.Assert(null == _stateObj, "non-null state object in EndExecuteNonQuery"); 
                    return _rowsAffected;
#if DEBUG 
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG
            }
            catch (System.OutOfMemoryException e) { 
                _activeConnection.Abort(e);
                throw; 
            } 
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e); 
                throw;
            }
            catch (System.Threading.ThreadAbortException e)  {
                _activeConnection.Abort(e); 
                throw;
            } 
            finally { 
                SqlStatistics.StopTimer(statistics);
            } 
        }

        private int InternalExecuteNonQuery(DbAsyncResult result, string methodName, bool sendToPipe) {
            bool async = (null != result); 

            SqlStatistics statistics = Statistics; 
            _rowsAffected = -1; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    // @devnote: this function may throw for an invalid connection 
                    // @devnote: returns false for empty command text
                    ValidateCommand(methodName, null != result);
                    CheckNotificationStateAndAutoEnlist(); // Only call after validate - requires non null connection!
 
                    // only send over SQL Batch command if we are not a stored proc and have no parameters and not in batch RPC mode
                    if ( _activeConnection.IsContextConnection ) { 
                        if (null != statistics) { 
                            statistics.SafeIncrement(ref statistics._unpreparedExecs);
                        } 

                        RunExecuteNonQuerySmi( sendToPipe );
                    }
                    else if (!BatchRPCMode && (System.Data.CommandType.Text == this.CommandType) && (0 == GetParameterCount(_parameters))) { 
                        Debug.Assert( !sendToPipe, "trying to send non-context command to pipe" );
                        if (null != statistics) { 
                            if (!this.IsDirty && this.IsPrepared) { 
                                statistics.SafeIncrement(ref statistics._preparedExecs);
                            } 
                            else {
                                statistics.SafeIncrement(ref statistics._unpreparedExecs);
                            }
                        } 

                        RunExecuteNonQueryTds(methodName, async); 
                    } 
                    else  { // otherwise, use full-blown execute which can handle params and stored procs
                        Debug.Assert( !sendToPipe, "trying to send non-context command to pipe" ); 
                        Bid.Trace("<sc.SqlCommand.ExecuteNonQuery|INFO> %d#, Command executed as RPC.\n", ObjectID);
                        SqlDataReader reader = RunExecuteReader(0, RunBehavior.UntilDone, false, methodName, result);
                        if (null != reader) {
                            reader.Close(); 
                        }
                    } 
                    Debug.Assert(async || null == _stateObj, "non-null state object in InternalExecuteNonQuery"); 
                    return _rowsAffected;
#if DEBUG 
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) { 
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _activeConnection.Abort(e); 
                throw;
            } 
        }

        public XmlReader ExecuteXmlReader() {
            SqlConnection.ExecutePermission.Demand(); 

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state 
            // between entry into Execute* API and the thread obtaining the stateObject. 
            _pendingCancel = false;
 
            SqlStatistics statistics = null;
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteXmlReader|API> %d#", ObjectID);
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
 
                // use the reader to consume metadata 
                SqlDataReader ds = RunExecuteReader(CommandBehavior.SequentialAccess, RunBehavior.ReturnImmediately, true, ADP.ExecuteXmlReader);
                return CompleteXmlReader(ds); 
            }
            finally {
                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteXmlReader() { 
            // BeginExecuteXmlReader will track executiontime
            return BeginExecuteXmlReader(null, null);
        }
 
        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteXmlReader(AsyncCallback callback, object stateObject) { 
            SqlConnection.ExecutePermission.Demand(); 

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state 
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            ValidateAsyncCommand(); // Special case - done outside of try/catches to prevent putting a stateObj 
                                    // back into pool when we should not.
 
            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                ExecutionContext execContext = (callback == null) ? null : ExecutionContext.Capture();
                DbAsyncResult result = new DbAsyncResult(this, ADP.EndExecuteXmlReader, callback, stateObject, execContext);

                try { // InternalExecuteNonQuery already has reliability block, but if failure will not put stateObj back into pool. 
                    RunExecuteReader(CommandBehavior.SequentialAccess, RunBehavior.ReturnImmediately, true, ADP.BeginExecuteXmlReader, result);
                } 
                catch (Exception e) { 
                    if (!ADP.IsCatchableOrSecurityExceptionType(e)) {
                        // If not catchable - the connection has already been caught and doomed in RunExecuteReader. 
                        throw;
                    }

                    // For async, RunExecuteReader will never put the stateObj back into the pool, so do so now. 
                    PutStateObject();
                    throw; 
                } 

                // Read SNI does not have catches for async exceptions, handle here. 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
#if DEBUG
                    object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                    RuntimeHelpers.PrepareConstrainedRegions(); 
                    try { 
                        Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                        // must finish caching information before ReadSni which can activate the callback before returning
                        cachedAsyncState.SetActiveConnectionAndResult(result, _activeConnection);
                        _stateObj.ReadSni(result, _stateObj);
#if DEBUG 
                    }
                    finally { 
                        Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                    }
#endif //DEBUG 
                }
                catch (System.OutOfMemoryException e) {
                    _activeConnection.Abort(e);
                    throw; 
                }
                catch (System.StackOverflowException e) { 
                    _activeConnection.Abort(e); 
                    throw;
                } 
                catch (System.Threading.ThreadAbortException e)  {
                    _activeConnection.Abort(e);
                    throw;
                } 
                catch (Exception) {
                    // Similarly, if an exception occurs put the stateObj back into the pool. 
                    // and reset async cache information to allow a second async execute 
                    if (null != _cachedAsyncState) {
                        _cachedAsyncState.ResetAsyncState(); 
                    }
                    PutStateObject();
                    throw;
                } 
                return result;
            } 
            finally { 
                SqlStatistics.StopTimer(statistics);
            } 
        }

        public XmlReader EndExecuteXmlReader(IAsyncResult asyncResult) {
            return CompleteXmlReader(InternalEndExecuteReader(asyncResult, ADP.EndExecuteXmlReader)); 
        }
 
        private XmlReader CompleteXmlReader(SqlDataReader ds) { 
            XmlReader xr = null;
 
            SmiExtendedMetaData[] md = ds.GetInternalSmiMetaData();
            bool isXmlCapable = (null != md && md.Length == 1 && (md[0].SqlDbType == SqlDbType.NText
                                                         || md[0].SqlDbType == SqlDbType.NVarChar
                                                         || md[0].SqlDbType == SqlDbType.Xml)); 

            if (isXmlCapable) { 
                try { 
                    SqlStream sqlBuf = new SqlStream(ds, true /*addByteOrderMark*/, (md[0].SqlDbType == SqlDbType.Xml) ? false : true /*process all rows*/);
                    xr = sqlBuf.ToXmlReader(); 
                }
                catch (Exception e) {
                    if (ADP.IsCatchableExceptionType(e)) {
                        ds.Close(); 
                    }
                    throw; 
                } 
            }
            if (xr == null) { 
                ds.Close();
                throw SQL.NonXmlResult();
            }
            return xr; 
        }
 
        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)] 
        public IAsyncResult BeginExecuteReader() {
            return BeginExecuteReader(null, null, CommandBehavior.Default); 
        }

        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteReader(AsyncCallback callback, object stateObject) { 
            return BeginExecuteReader(callback, stateObject, CommandBehavior.Default);
        } 
 
        override protected DbDataReader ExecuteDbDataReader(CommandBehavior behavior) {
            return ExecuteReader(behavior, ADP.ExecuteReader); 
        }

        new public SqlDataReader ExecuteReader() {
            SqlStatistics statistics = null; 
            IntPtr hscp;
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteReader|API> %d#", ObjectID); 
            try { 
                statistics = SqlStatistics.StartTimer(Statistics);
                return ExecuteReader(CommandBehavior.Default, ADP.ExecuteReader); 
            }
            finally {
                SqlStatistics.StopTimer(statistics);
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
        new public SqlDataReader ExecuteReader(CommandBehavior behavior) {
            IntPtr hscp; 
            Bid.ScopeEnter(out hscp, "<sc.SqlCommand.ExecuteReader|API> %d#, behavior=%d{ds.CommandBehavior}", ObjectID, (int)behavior);
            try {
                return ExecuteReader(behavior, ADP.ExecuteReader);
            } 
            finally {
                Bid.ScopeLeave(ref hscp); 
            } 
        }
 
        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)]
        public IAsyncResult BeginExecuteReader(CommandBehavior behavior) {
            return BeginExecuteReader(null, null, behavior);
        } 

        [System.Security.Permissions.HostProtectionAttribute(ExternalThreading=true)] 
        public IAsyncResult BeginExecuteReader(AsyncCallback callback, object stateObject, CommandBehavior behavior) { 
            SqlConnection.ExecutePermission.Demand();
 
            // Reset _pendingCancel upon entry into any Execute - used to synchronize state
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;
 
            SqlStatistics statistics = null;
            try { 
                statistics = SqlStatistics.StartTimer(Statistics); 
                return InternalBeginExecuteReader(callback, stateObject, behavior);
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        internal SqlDataReader ExecuteReader(CommandBehavior behavior, string method) { 
            SqlConnection.ExecutePermission.Demand(); // 

            // Reset _pendingCancel upon entry into any Execute - used to synchronize state 
            // between entry into Execute* API and the thread obtaining the stateObject.
            _pendingCancel = false;

            SqlStatistics statistics = null; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 

                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    statistics = SqlStatistics.StartTimer(Statistics); 
                    SqlDataReader reader = RunExecuteReader(behavior, RunBehavior.ReturnImmediately, true, method); 
                    return reader;
#if DEBUG 
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) { 
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _activeConnection.Abort(e); 
                throw;
            } 
            finally {
                SqlStatistics.StopTimer(statistics);
            }
        } 

        public SqlDataReader EndExecuteReader(IAsyncResult asyncResult) { 
            SqlStatistics statistics = null; 
            try {
                statistics = SqlStatistics.StartTimer(Statistics); 
                return InternalEndExecuteReader(asyncResult, ADP.EndExecuteReader);
            }
            finally {
                SqlStatistics.StopTimer(statistics); 
            }
        } 
 
        private IAsyncResult InternalBeginExecuteReader(AsyncCallback callback, object stateObject, CommandBehavior behavior) {
            ExecutionContext execContext = (callback == null) ? null : ExecutionContext.Capture(); 
            DbAsyncResult result = new DbAsyncResult(this, ADP.EndExecuteReader, callback, stateObject, execContext);

            ValidateAsyncCommand(); // Special case - done outside of try/catches to prevent putting a stateObj
                                    // back into pool when we should not. 

            try { // InternalExecuteNonQuery already has reliability block, but if failure will not put stateObj back into pool. 
                RunExecuteReader(behavior, RunBehavior.ReturnImmediately, true, ADP.BeginExecuteReader, result); 
            }
            catch (Exception e) { 
                if (!ADP.IsCatchableOrSecurityExceptionType(e)) {
                    // If not catchable - the connection has already been caught and doomed in RunExecuteReader.
                    throw;
                } 

                // For async, RunExecuteReader will never put the stateObj back into the pool, so do so now. 
                PutStateObject(); 
                throw;
            } 

            // Read SNI does not have catches for async exceptions, handle here.
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot); 
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG
                    // must finish caching information before ReadSni which can activate the callback before returning
                    cachedAsyncState.SetActiveConnectionAndResult(result, _activeConnection); 
                    _stateObj.ReadSni(result, _stateObj);
#if DEBUG 
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                }
#endif //DEBUG
            }
            catch (System.OutOfMemoryException e) { 
                _activeConnection.Abort(e);
                throw; 
            } 
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e); 
                throw;
            }
            catch (System.Threading.ThreadAbortException e)  {
                _activeConnection.Abort(e); 
                throw;
            } 
            catch (Exception) { 
                // Similarly, if an exception occurs put the stateObj back into the pool.
                // and reset async cache information to allow a second async execute 
                if (null != _cachedAsyncState) {
                    _cachedAsyncState.ResetAsyncState();
                }
                PutStateObject(); 
                throw;
            } 
 
            return result;
        } 

        private SqlDataReader InternalEndExecuteReader(IAsyncResult asyncResult, string endMethod) {

            VerifyEndExecuteState((DbAsyncResult) asyncResult, endMethod); 
            WaitForAsyncResults(asyncResult);
 
            CheckThrowSNIException(); 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true); 
#endif //DEBUG
                    SqlDataReader reader = CompleteAsyncExecuteReader(); 
                    Debug.Assert(null == _stateObj, "non-null state object in InternalEndExecuteReader");
                    return reader;
#if DEBUG
                } 
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue); 
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) {
                _activeConnection.Abort(e);
                throw;
            } 
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e); 
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _activeConnection.Abort(e);
                throw;
            }
        } 

        // If the user part is quoted, remove first and last brackets and then unquote any right square 
        // brackets in the procedure.  This is a very simple parser that performs no validation.  As 
        // with the function below, ideally we should have support from the server for this.
        private static string UnquoteProcedurePart(string part) { 
            if ((null != part) && (2 <= part.Length)) {
                if ('[' == part[0] && ']' == part[part.Length-1]) {
                    part = part.Substring(1, part.Length-2); // strip outer '[' & ']'
                    part = part.Replace("]]", "]"); // undo quoted "]" from "]]" to "]" 
                }
            } 
            return part; 
        }
 
        // User value in this format: [server].[database].[schema].[sp_foo];1
        // This function should only be passed "[sp_foo];1".
        // This function uses a pretty simple parser that doesn't do any validation.
        // Ideally, we would have support from the server rather than us having to do this. 
        private static string UnquoteProcedureName(string name, out object groupNumber) {
            groupNumber  = null; // Out param - initialize value to no value. 
            string sproc = name; 

            if (null != sproc) { 
                if (Char.IsDigit(sproc[sproc.Length-1])) { // If last char is a digit, parse.
                    int semicolon = sproc.LastIndexOf(';');
                    if (semicolon != -1) { // If we found a semicolon, obtain the integer.
                        string part   = sproc.Substring(semicolon+1); 
                        int    number = 0;
                        if (Int32.TryParse(part, out number)) { // No checking, just fail if this doesn't work. 
                            groupNumber = number; 
                            sproc = sproc.Substring(0, semicolon);
                        } 
                    }
                }
                sproc = UnquoteProcedurePart(sproc);
            } 
            return sproc;
        } 
 
        //index into indirection arrays for columns of interest to DeriveParameters
        private enum ProcParamsColIndex { 
            ParameterName = 0,
            ParameterType,
            DataType,                  // obsolete in katmai, use ManagedDataType instead
            ManagedDataType,          // new in katmai 
            CharacterMaximumLength,
            NumericPrecision, 
            NumericScale, 
            TypeCatalogName,
            TypeSchemaName, 
            TypeName,
            XmlSchemaCollectionCatalogName,
            XmlSchemaCollectionSchemaName,
            XmlSchemaCollectionName, 
            UdtTypeName,                // obsolete in Katmai.  Holds the actual typename if UDT, since TypeName didn't back then.
            DateTimeScale               // new in Katmai 
        }; 

        // Yukon- column ordinals (this array indexed by ProcParamsColIndex 
        static readonly internal string[] PreKatmaiProcParamsNames = new string[] {
            "PARAMETER_NAME",           // ParameterName,
            "PARAMETER_TYPE",           // ParameterType,
            "DATA_TYPE",                // DataType 
            null,                       // ManagedDataType,     introduced in Katmai
            "CHARACTER_MAXIMUM_LENGTH", // CharacterMaximumLength, 
            "NUMERIC_PRECISION",        // NumericPrecision, 
            "NUMERIC_SCALE",            // NumericScale,
            "UDT_CATALOG",              // TypeCatalogName, 
            "UDT_SCHEMA",               // TypeSchemaName,
            "TYPE_NAME",                // TypeName,
            "XML_CATALOGNAME",          // XmlSchemaCollectionCatalogName,
            "XML_SCHEMANAME",           // XmlSchemaCollectionSchemaName, 
            "XML_SCHEMACOLLECTIONNAME", // XmlSchemaCollectionName
            "UDT_NAME",                 // UdtTypeName 
            null,                       // Scale for datetime types with scale, introduced in Katmai 
        };
 
        // Katmai+ column ordinals (this array indexed by ProcParamsColIndex
        static readonly internal string[] KatmaiProcParamsNames = new string[] {
            "PARAMETER_NAME",           // ParameterName,
            "PARAMETER_TYPE",           // ParameterType, 
            null,                       // DataType, removed from Katmai+
            "MANAGED_DATA_TYPE",        // ManagedDataType, 
            "CHARACTER_MAXIMUM_LENGTH", // CharacterMaximumLength, 
            "NUMERIC_PRECISION",        // NumericPrecision,
            "NUMERIC_SCALE",            // NumericScale, 
            "TYPE_CATALOG_NAME",        // TypeCatalogName,
            "TYPE_SCHEMA_NAME",         // TypeSchemaName,
            "TYPE_NAME",                // TypeName,
            "XML_CATALOGNAME",          // XmlSchemaCollectionCatalogName, 
            "XML_SCHEMANAME",           // XmlSchemaCollectionSchemaName,
            "XML_SCHEMACOLLECTIONNAME", // XmlSchemaCollectionName 
            null,                       // UdtTypeName, removed from Katmai+ 
            "SS_DATETIME_PRECISION",    // Scale for datetime types with scale
        }; 


        internal void DeriveParameters() {
            switch (this.CommandType) { 
                case System.Data.CommandType.Text:
                    throw ADP.DeriveParametersNotSupported(this); 
                case System.Data.CommandType.StoredProcedure: 
                    break;
                case System.Data.CommandType.TableDirect: 
                    // CommandType.TableDirect - do nothing, parameters are not supported
                    throw ADP.DeriveParametersNotSupported(this);
                default:
                    throw ADP.InvalidCommandType(this.CommandType); 
            }
 
            // validate that we have a valid connection 
            ValidateCommand(ADP.DeriveParameters, false /*not async*/);
 
            // Use common parser for SqlClient and OleDb - parse into 4 parts - Server, Catalog, Schema, ProcedureName
            string[] parsedSProc = MultipartIdentifier.ParseMultipartIdentifier(this.CommandText, "[\"", "]\"", Res.SQL_SqlCommandCommandText, false);
            if (null == parsedSProc[3] || ADP.IsEmpty(parsedSProc[3]))
            { 
                throw ADP.NoStoredProcedureExists(this.CommandText);
            } 
 
            Debug.Assert(parsedSProc.Length == 4, "Invalid array length result from SqlCommandBuilder.ParseProcedureName");
 
            SqlCommand    paramsCmd = null;
            StringBuilder cmdText   = new StringBuilder();

            // Build call for sp_procedure_params_rowset built of unquoted values from user: 
            // [user server, if provided].[user catalog, else current database].[sys if Yukon, else blank].[sp_procedure_params_rowset]
 
            // Server - pass only if user provided. 
            if (!ADP.IsEmpty(parsedSProc[0])) {
                SqlCommandSet.BuildStoredProcedureName(cmdText, parsedSProc[0]); 
                cmdText.Append(".");
            }

            // Catalog - pass user provided, otherwise use current database. 
            if (ADP.IsEmpty(parsedSProc[1])) {
                parsedSProc[1] = this.Connection.Database; 
            } 
            SqlCommandSet.BuildStoredProcedureName(cmdText, parsedSProc[1]);
            cmdText.Append("."); 

            // Schema - only if Yukon, and then only pass sys.  Also - pass managed version of sproc
            // for Yukon, else older sproc.
            string[] colNames; 
            bool useManagedDataType;
            if (this.Connection.IsKatmaiOrNewer) { 
                // Procedure - [sp_procedure_params_managed] 
                cmdText.Append("[sys].[").Append(TdsEnums.SP_PARAMS_MGD10).Append("]");
 
                colNames = KatmaiProcParamsNames;
                useManagedDataType = true;
            }
            else { 
                if (this.Connection.IsYukonOrNewer) {
                    // Procedure - [sp_procedure_params_managed] 
                    cmdText.Append("[sys].[").Append(TdsEnums.SP_PARAMS_MANAGED).Append("]"); 
                }
                else { 
                    // Procedure - [sp_procedure_params_rowset]
                    cmdText.Append(".[").Append(TdsEnums.SP_PARAMS).Append("]");
                }
 
                colNames = PreKatmaiProcParamsNames;
                useManagedDataType = false; 
            } 

 
            paramsCmd = new SqlCommand(cmdText.ToString(), this.Connection, this.Transaction);
            paramsCmd.CommandType = CommandType.StoredProcedure;

            object groupNumber; 

            // Prepare parameters for sp_procedure_params_rowset: 
            // 1) procedure name - unquote user value 
            // 2) group number - parsed at the time we unquoted procedure name
            // 3) procedure schema - unquote user value 

            //

 

            paramsCmd.Parameters.Add(new SqlParameter("@procedure_name", SqlDbType.NVarChar, 255)); 
            paramsCmd.Parameters[0].Value = UnquoteProcedureName(parsedSProc[3], out groupNumber); // ProcedureName is 4rd element in parsed array 

            if (null != groupNumber) { 
                SqlParameter param = paramsCmd.Parameters.Add(new SqlParameter("@group_number", SqlDbType.Int));
                param.Value = groupNumber;
            }
 
            if (!ADP.IsEmpty(parsedSProc[2])) { // SchemaName is 3rd element in parsed array
                SqlParameter param = paramsCmd.Parameters.Add(new SqlParameter("@procedure_schema", SqlDbType.NVarChar, 255)); 
                param.Value = UnquoteProcedurePart(parsedSProc[2]); 
            }
 
            SqlDataReader r = null;

            List<SqlParameter> parameters = new List<SqlParameter>();
            bool processFinallyBlock = true; 

            try { 
                r = paramsCmd.ExecuteReader(); 

                SqlParameter p = null; 

                while (r.Read()) {
                    // each row corresponds to a parameter of the stored proc.  Fill in all the info
 
                    p = new SqlParameter();
 
                    // name 
                    p.ParameterName = (string) r[colNames[(int)ProcParamsColIndex.ParameterName]];
 
                    // type
                    if (useManagedDataType) {
                        p.SqlDbType = (SqlDbType)(short)r[colNames[(int)ProcParamsColIndex.ManagedDataType]];
 
                        // Yukon didn't have as accurate of information as we're getting for Katmai, so re-map a couple of
                        //  types for backward compatability. 
                        switch (p.SqlDbType) { 
                            case SqlDbType.Image:
                            case SqlDbType.Timestamp: 
                                p.SqlDbType = SqlDbType.VarBinary;
                                break;

                            case SqlDbType.NText: 
                                p.SqlDbType = SqlDbType.NVarChar;
                                break; 
 
                            case SqlDbType.Text:
                                p.SqlDbType = SqlDbType.VarChar; 
                                break;

                            default:
                                break; 
                        }
                    } 
                    else { 
                        p.SqlDbType = MetaType.GetSqlDbTypeFromOleDbType((short)r[colNames[(int)ProcParamsColIndex.DataType]],
                            ADP.IsNull(r[colNames[(int)ProcParamsColIndex.TypeName]]) ? 
                                ADP.StrEmpty :
                                (string)r[colNames[(int)ProcParamsColIndex.TypeName]]);
                    }
 
                    // size
                    object a = r[colNames[(int)ProcParamsColIndex.CharacterMaximumLength]]; 
                    if (a is int) { 
                        int size = (int)a;
 
                        // Map MAX sizes correctly.  The Katmai server-side proc sends 0 for these instead of -1.
                        //  Should be fixed on the Katmai side, but would likely hold up the RI, and is safer to fix here.
                        //  If we can get the server-side fixed before shipping Katmai, we can remove this mapping.
                        if (0 == size && 
                                (p.SqlDbType == SqlDbType.NVarChar ||
                                 p.SqlDbType == SqlDbType.VarBinary || 
                                 p.SqlDbType == SqlDbType.VarChar)) { 
                            size = -1;
                        } 
                        p.Size = size;
                    }

                    // direction 
                    p.Direction = ParameterDirectionFromOleDbDirection((short)r[colNames[(int)ProcParamsColIndex.ParameterType]]);
 
                    if (p.SqlDbType == SqlDbType.Decimal) { 
                        p.ScaleInternal = (byte) ((short)r[colNames[(int)ProcParamsColIndex.NumericScale]] & 0xff);
                        p.PrecisionInternal = (byte)((short)r[colNames[(int)ProcParamsColIndex.NumericPrecision]] & 0xff); 
                    }

                    // type name for Udt
                    if (SqlDbType.Udt == p.SqlDbType) { 

                        Debug.Assert(this._activeConnection.IsYukonOrNewer,"Invalid datatype token received from pre-yukon server"); 
 
                        string udtTypeName;
                        if (useManagedDataType) { 
                            udtTypeName = (string)r[colNames[(int)ProcParamsColIndex.TypeName]];
                        }
                        else {
                            udtTypeName = (string)r[colNames[(int)ProcParamsColIndex.UdtTypeName]]; 
                        }
 
                        //read the type name 
                        p.UdtTypeName = r[colNames[(int)ProcParamsColIndex.TypeCatalogName]]+"."+
                            r[colNames[(int)ProcParamsColIndex.TypeSchemaName]]+"."+ 
                            udtTypeName;
                    }

                    // type name for Structured types (same as for Udt's except assign p.TypeName instead of p.UdtTypeName 
                    if (SqlDbType.Structured == p.SqlDbType) {
 
                        Debug.Assert(this._activeConnection.IsKatmaiOrNewer,"Invalid datatype token received from pre-katmai server"); 

                        //read the type name 
                        p.TypeName = r[colNames[(int)ProcParamsColIndex.TypeCatalogName]]+"."+
                            r[colNames[(int)ProcParamsColIndex.TypeSchemaName]]+"."+
                            r[colNames[(int)ProcParamsColIndex.TypeName]];
                    } 

                    // XmlSchema name for Xml types 
                    if (SqlDbType.Xml == p.SqlDbType) { 
                        object value;
 
                        value = r[colNames[(int)ProcParamsColIndex.XmlSchemaCollectionCatalogName]];
                        p.XmlSchemaCollectionDatabase = ADP.IsNull(value) ? String.Empty : (string) value;

                        value = r[colNames[(int)ProcParamsColIndex.XmlSchemaCollectionSchemaName]]; 
                        p.XmlSchemaCollectionOwningSchema = ADP.IsNull(value) ? String.Empty : (string) value;
 
                        value = r[colNames[(int)ProcParamsColIndex.XmlSchemaCollectionName]]; 
                        p.XmlSchemaCollectionName = ADP.IsNull(value) ? String.Empty : (string) value;
                    } 

                    if (MetaType._IsVarTime(p.SqlDbType)) {
                        object value = r[colNames[(int)ProcParamsColIndex.DateTimeScale]];
                        if (value is int) { 
                            p.ScaleInternal = (byte)(((int)value) & 0xff);
                        } 
                    } 

                    parameters.Add(p); 
                }
            }
            catch (Exception e) {
                processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                throw;
            } 
            finally { 
                Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to DeriveParameters");  // you need to setup for a thread abort somewhere before you call this method
                if (processFinallyBlock) { 
                    if (null != r)
                        r.Close();

                    // always unhook the user's connection 
                    paramsCmd.Connection = null;
                } 
            } 

            if (parameters.Count == 0) { 
                throw ADP.NoStoredProcedureExists(this.CommandText);
            }

            this.Parameters.Clear(); 

            foreach (SqlParameter temp in parameters) { 
                this._parameters.Add(temp); 
            }
        } 

        private ParameterDirection ParameterDirectionFromOleDbDirection(short oledbDirection) {
            Debug.Assert(oledbDirection >= 1 && oledbDirection <= 4, "invalid parameter direction from params_rowset!");
 
            switch (oledbDirection) {
                case 2: 
                    return ParameterDirection.InputOutput; 
                case 3:
                    return ParameterDirection.Output; 
                case 4:
                    return ParameterDirection.ReturnValue;
                default:
                    return ParameterDirection.Input; 
            }
 
        } 

        // get cached metadata 
        internal _SqlMetaDataSet MetaData {
            get {
                return _cachedMetaData;
            } 
        }
 
        // Check to see if notificactions auto enlistment is turned on. Enlist if so. 
        private void CheckNotificationStateAndAutoEnlist() {
            // First, if auto-enlist is on, check server version and then obtain context if 
            // present.  If so, auto enlist to the dependency ID given in the context data.
            if (NotificationAutoEnlist) {
                if (_activeConnection.IsYukonOrNewer) { // Only supported for Yukon...
                    string notifyContext = SqlNotificationContext(); 
                    if (!ADP.IsEmpty(notifyContext)) {
                        // Map to dependency by ID set in context data. 
                        SqlDependency dependency = SqlDependencyPerAppDomainDispatcher.SingletonInstance.LookupDependencyEntry(notifyContext); 

                        if (null != dependency) { 
                            // Add this command to the dependency.
                            dependency.AddCommandDependency(this);
                        }
                    } 
                }
            } 
 
            // If we have a notification with a dependency, setup the notification options at this time.
 
            // If user passes options, then we will always have option data at the time the SqlDependency
            // ctor is called.  But, if we are using default queue, then we do not have this data until
            // Start().  Due to this, we always delay setting options until execute.
 
            // There is a variance in order between Start(), SqlDependency(), and Execute.  This is the
            // best way to solve that problem. 
            if (null != Notification) { 
                if (_sqlDep != null) {
                    if (null == _sqlDep.Options) { 
                        // If null, SqlDependency was not created with options, so we need to obtain default options now.
                        // GetDefaultOptions can and will throw under certain conditions.

                        // In order to match to the appropriate start - we need 3 pieces of info: 
                        // 1) server 2) user identity (SQL Auth or Int Sec) 3) database
 
                        SqlDependency.IdentityUserNamePair identityUserName = null; 

                        // Obtain identity from connection. 
                        SqlInternalConnectionTds internalConnection = _activeConnection.InnerConnection as SqlInternalConnectionTds;
                        if (internalConnection.Identity != null) {
                            identityUserName = new SqlDependency.IdentityUserNamePair(internalConnection.Identity, null);
                        } 
                        else {
                            identityUserName = new SqlDependency.IdentityUserNamePair(null, internalConnection.ConnectionOptions.UserID); 
                        } 

                        Notification.Options = SqlDependency.GetDefaultComposedOptions(_activeConnection.DataSource, 
                                                             InternalTdsConnection.ServerProvidedFailOverPartner,
                                                             identityUserName, _activeConnection.Database);
                    }
 
                    // Set UserData on notifications, as well as adding to the appdomain dispatcher.  The value is
                    // computed by an algorithm on the dependency - fixed and will always produce the same value 
                    // given identical commandtext + parameter values. 
                    Notification.UserData = _sqlDep.ComputeHashAndAddToDispatcher(this);
                    // Maintain server list for SqlDependency. 
                    _sqlDep.AddToServerList(_activeConnection.DataSource);
                }
            }
        } 

        [System.Security.Permissions.SecurityPermission(SecurityAction.Assert, Infrastructure=true)] 
        static internal string SqlNotificationContext() { 
            SqlConnection.VerifyExecutePermission();
 
            // since this information is protected, follow it so that it is not exposed to the user.
            //
            return (System.Runtime.Remoting.Messaging.CallContext.GetData("MS.SqlDependencyCookie") as string);
        } 

        // Tds-specific logic for ExecuteNonQuery run handling 
        private void RunExecuteNonQueryTds(string methodName, bool async) { 
            bool processFinallyBlock = true;
            try { 
                GetStateObject();

                // we just send over the raw text with no annotation
                // no parameters are sent over 
                // no data reader is returned
                // use this overload for "batch SQL" tds token type 
                Bid.Trace("<sc.SqlCommand.ExecuteNonQuery|INFO> %d#, Command executed as SQLBATCH.\n", ObjectID); 
                _stateObj.Parser.TdsExecuteSQLBatch(this.CommandText, this.CommandTimeout, this.Notification, _stateObj);
 
                NotifyDependency();
                if (async) {
                    _activeConnection.GetOpenTdsConnection(methodName).IncrementAsyncCount();
                } 
                else {
                    _stateObj.Parser.Run(RunBehavior.UntilDone, this, null, null, _stateObj); 
                } 
            }
            catch (Exception e) { 
                processFinallyBlock = ADP.IsCatchableExceptionType(e);
                throw;
            }
            finally { 
                Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to RunExecuteNonQueryTds");  // you need to setup for a thread abort somewhere before you call this method
                if (processFinallyBlock && !async) { 
                    // When executing Async, we need to keep the _stateObj alive... 
                    PutStateObject();
                } 
            }
        }

        // Smi-specific logic for ExecuteNonQuery 
        private void RunExecuteNonQuerySmi( bool sendToPipe ) {
            SqlInternalConnectionSmi innerConnection = InternalSmiConnection; 
 
            try {
                // Set it up, process all of the events, and we're done! 
                SetUpSmiRequest( innerConnection );

                long transactionId = (null != innerConnection.CurrentTransaction) ? innerConnection.CurrentTransaction.TransactionId : 0;
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlCommand.RunExecuteNonQuerySmi|ADV> %d#, innerConnection=%d#, transactionId=0x%I64x, cmdBehavior=%d.\n", ObjectID, innerConnection.ObjectID, transactionId, (int)CommandBehavior.Default);
                } 
 
                SmiExecuteType execType;
                if ( sendToPipe ) 
                    execType = SmiExecuteType.ToPipe;
                else
                    execType = SmiExecuteType.NonQuery;
 
                SmiEventStream eventStream = null;
                // Don't need a CER here because caller already has one that will doom the 
                //  connection if it's a finally-skipping type of problem. 
                bool processFinallyBlock = true;
                try { 
                    if (SmiContextFactory.Instance.NegotiatedSmiVersion >= SmiContextFactory.KatmaiVersion) {
                        eventStream = _smiRequest.Execute(
                                                                            innerConnection.SmiConnection,
                                                                            transactionId, 
                                                                            innerConnection.InternalEnlistedTransaction,
                                                                            CommandBehavior.Default, 
                                                                            execType); 
                    }
                    else { 
                        eventStream = _smiRequest.Execute(
                                                                            innerConnection.SmiConnection,
                                                                            transactionId,
                                                                            CommandBehavior.Default, 
                                                                            execType);
                    } 
 
                    while ( eventStream.HasEvents ) {
                        eventStream.ProcessEvent( EventSink ); 
                    }
                }
                catch (Exception e) {
                    processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                    throw;
                } 
                finally { 
                    Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to RunExecuteNonQuerySmi");  // you need to setup for a thread abort somewhere before you call this method
                    if (null != eventStream && processFinallyBlock) { 
                        eventStream.Close( EventSink );
                    }
                }
 
                EventSink.ProcessMessagesAndThrow();
            } 
            catch { 
                DisposeSmiRequest();
 
                throw;
            }
        }
 
        internal SqlDataReader RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream, string method) {
            return RunExecuteReader(cmdBehavior, runBehavior, returnStream, method, null); 
        } 

        internal SqlDataReader RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream, string method, DbAsyncResult result) { 
            bool async = (null != result);

            _rowsAffected = -1;
 
            if (0 != (CommandBehavior.SingleRow & cmdBehavior)) {
                // CommandBehavior.SingleRow implies CommandBehavior.SingleResult 
                cmdBehavior |= CommandBehavior.SingleResult; 
            }
 
            // @devnote: this function may throw for an invalid connection
            // @devnote: returns false for empty command text
            ValidateCommand(method, null != result);
            CheckNotificationStateAndAutoEnlist(); // Only call after validate - requires non null connection! 

            // This section needs to occur AFTER ValidateCommand - otherwise it will AV without a connection. 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);

                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    SqlStatistics statistics = Statistics; 
                    if (null != statistics) {
                        if ((!this.IsDirty && this.IsPrepared && !_hiddenPrepare) 
                            || (this.IsPrepared && _execType == EXECTYPE.PREPAREPENDING))
                        {
                            statistics.SafeIncrement(ref statistics._preparedExecs);
                        } 
                        else {
                            statistics.SafeIncrement(ref statistics._unpreparedExecs); 
                        } 
                    }
 

                    if ( _activeConnection.IsContextConnection ) {
                        return RunExecuteReaderSmi( cmdBehavior, runBehavior, returnStream );
                    } 
                    else {
                        return RunExecuteReaderTds( cmdBehavior, runBehavior, returnStream, async ); 
                    } 

#if DEBUG 
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) { 
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.StackOverflowException e) {
                _activeConnection.Abort(e);
                throw; 
            }
            catch (System.Threading.ThreadAbortException e)  { 
                _activeConnection.Abort(e); 
                throw;
            } 
        }

        private SqlDataReader RunExecuteReaderTds( CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream, bool async ) {
            // make sure we have good parameter information 
            // prepare the command
            // execute 
            Debug.Assert(null != _activeConnection.Parser, "TdsParser class should not be null in Command.Execute!"); 

            bool inSchema =  (0 != (cmdBehavior & CommandBehavior.SchemaOnly)); 
            SqlDataReader ds = null;

            // create a new RPC
            _SqlRPC rpc=null; 

            string optionSettings = null; 
            bool processFinallyBlock = true; 

            try { 
                GetStateObject();

                if (BatchRPCMode) {
                    Debug.Assert(inSchema == false, "Batch RPC does not support schema only command beahvior"); 
                    Debug.Assert(!IsPrepared, "Batch RPC should not be prepared!");
                    Debug.Assert(!IsDirty, "Batch RPC should not be marked as dirty!"); 
                    //Currently returnStream is always false, but we may want to return a Reader later. 
                    //if (returnStream) {
                    //    Bid.Trace("<sc.SqlCommand.ExecuteReader|INFO> %d#, Command executed as batch RPC.\n", ObjectID); 
                    //}
                    Debug.Assert(_SqlRPCBatchArray != null, "RunExecuteReader rpc array not provided");
                    _stateObj.Parser.TdsExecuteRPC(_SqlRPCBatchArray, this.CommandTimeout, inSchema, this.Notification, _stateObj, CommandType.StoredProcedure == CommandType);
                } 
                else if ((System.Data.CommandType.Text == this.CommandType) && (0 == GetParameterCount(_parameters))) {
                    // Send over SQL Batch command if we are not a stored proc and have no parameters 
                    // MDAC 
                    Debug.Assert(!IsUserPrepared, "CommandType.Text with no params should not be prepared!");
                    if (returnStream) { 
                        Bid.Trace("<sc.SqlCommand.ExecuteReader|INFO> %d#, Command executed as SQLBATCH.\n", ObjectID);
                    }
                    string text = GetCommandText(cmdBehavior) + GetResetOptionsString(cmdBehavior);
                    _stateObj.Parser.TdsExecuteSQLBatch(text, this.CommandTimeout, this.Notification, _stateObj); 
                }
                else if (System.Data.CommandType.Text == this.CommandType) { 
                    if (this.IsDirty) { 
                        Debug.Assert(_cachedMetaData == null, "dirty query should not have cached metadata!");
                        // 
                        // someone changed the command text or the parameter schema so we must unprepare the command
                        //
                        // remeber that IsDirty includes test for IsPrepared!
                        if(_execType == EXECTYPE.PREPARED) { 
                            _hiddenPrepare = true;
                        } 
                        InternalUnprepare(false); 
                        IsDirty = false;
                    } 

                    if (_execType == EXECTYPE.PREPARED) {
                        Debug.Assert(this.IsPrepared && (_prepareHandle != -1), "invalid attempt to call sp_execute without a handle!");
                        rpc = BuildExecute(inSchema); 
                    }
                    else if (_execType == EXECTYPE.PREPAREPENDING) { 
                        Debug.Assert(_activeConnection.IsShiloh, "Invalid attempt to call sp_prepexec on non 7.x server"); 
                        rpc = BuildPrepExec(cmdBehavior);
                        // next time through, only do an exec 
                        _execType = EXECTYPE.PREPARED;
                        _activeConnection.AddPreparedCommand(this);
                        // mark ourselves as preparing the command
                        _inPrepare = true; 
                    }
                    else { 
                        Debug.Assert(_execType == EXECTYPE.UNPREPARED, "Invalid execType!"); 
                        BuildExecuteSql(cmdBehavior, null, _parameters, ref rpc);
                    } 

                    // if shiloh, then set NOMETADATA_UNLESSCHANGED flag
                    if (_activeConnection.IsShiloh)
                        rpc.options = TdsEnums.RPC_NOMETADATA; 
                    if (returnStream) {
                        Bid.Trace("<sc.SqlCommand.ExecuteReader|INFO> %d#, Command executed as RPC.\n", ObjectID); 
                    } 
                    Debug.Assert(_rpcArrayOf1[0] == rpc);
                    _stateObj.Parser.TdsExecuteRPC(_rpcArrayOf1, this.CommandTimeout, inSchema, this.Notification, _stateObj, CommandType.StoredProcedure == CommandType); 
                }
                else {
                    Debug.Assert(this.CommandType == System.Data.CommandType.StoredProcedure, "unknown command type!");
                    // note: invalid asserts on Shiloh. On 8.0 (Shiloh) and above a command is ALWAYS prepared 
                    // and IsDirty is always set if there are changes and the command is marked Prepared!
                    Debug.Assert(IsShiloh || !IsPrepared, "RPC should not be prepared!"); 
                    Debug.Assert(IsShiloh || !IsDirty, "RPC should not be marked as dirty!"); 

                    BuildRPC(inSchema, _parameters, ref rpc); 

                    // if we need to augment the command because a user has changed the command behavior (e.g. FillSchema)
                    // then batch sql them over.  This is inefficient (3 round trips) but the only way we can get metadata only from
                    // a stored proc 
                    optionSettings = GetSetOptionsString(cmdBehavior);
                    if (returnStream) { 
                        Bid.Trace("<sc.SqlCommand.ExecuteReader|INFO> %d#, Command executed as RPC.\n", ObjectID); 
                    }
                    // turn set options ON 
                    if (null != optionSettings) {
                        _stateObj.Parser.TdsExecuteSQLBatch(optionSettings, this.CommandTimeout, this.Notification, _stateObj);
                        _stateObj.Parser.Run(RunBehavior.UntilDone, this, null, null, _stateObj);
                        // and turn OFF when the ds exhausts the stream on Close() 
                        optionSettings = GetResetOptionsString(cmdBehavior);
                    } 
 
                    // turn debugging on
                    _activeConnection.CheckSQLDebug(); 
                    // execute sp
                    Debug.Assert(_rpcArrayOf1[0] == rpc);
                    _stateObj.Parser.TdsExecuteRPC(_rpcArrayOf1, this.CommandTimeout, inSchema, this.Notification, _stateObj, CommandType.StoredProcedure == CommandType);
                } 

                if (returnStream) { 
                    ds = new SqlDataReader(this, cmdBehavior); 
                }
 
                if (async) {
                    _activeConnection.GetOpenTdsConnection().IncrementAsyncCount();
                    cachedAsyncState.SetAsyncReaderState(ds, runBehavior, optionSettings);
                } 
                else {
                    // Always execute - even if no reader! 
                    FinishExecuteReader(ds, runBehavior, optionSettings); 
                }
            } 
            catch (Exception e) {
                processFinallyBlock = ADP.IsCatchableExceptionType (e);
                throw;
            } 
            finally {
                Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to RunExecuteReaderTds");  // you need to setup for a thread abort somewhere before you call this method 
                if (processFinallyBlock && !async) { 
                    // When executing async, we need to keep the _stateObj alive...
                    PutStateObject(); 
                }
            }

            Debug.Assert(async || null == _stateObj, "non-null state object in RunExecuteReader"); 
            return ds;
        } 
 
        private SqlDataReader RunExecuteReaderSmi( CommandBehavior cmdBehavior, RunBehavior runBehavior, bool returnStream ) {
            SqlInternalConnectionSmi innerConnection = InternalSmiConnection; 

            SmiEventStream eventStream = null;
            SqlDataReader ds = null;
            try { 
                // Set it up, process all of the events, and we're done!
                SetUpSmiRequest( innerConnection ); 
 
                long transactionId = (null != innerConnection.CurrentTransaction) ? innerConnection.CurrentTransaction.TransactionId : 0;
                if (Bid.AdvancedOn) { 
                    Bid.Trace("<sc.SqlCommand.RunExecuteReaderSmi|ADV> %d#, innerConnection=%d#, transactionId=0x%I64x, commandBehavior=%d.\n", ObjectID, innerConnection.ObjectID, transactionId, (int)cmdBehavior);
                }

                if (SmiContextFactory.Instance.NegotiatedSmiVersion >= 210) { 
                    eventStream = _smiRequest.Execute(
                                                    innerConnection.SmiConnection, 
                                                    transactionId, 
                                                    innerConnection.InternalEnlistedTransaction,
                                                    cmdBehavior, 
                                                    SmiExecuteType.Reader
                                                    );
                }
                else { 
                    eventStream = _smiRequest.Execute(
                                                    innerConnection.SmiConnection, 
                                                    transactionId, 
                                                    cmdBehavior,
                                                    SmiExecuteType.Reader 
                                                    );
                }

                if ( ( runBehavior & RunBehavior.UntilDone ) != 0 ) { 

                    // Consume the results 
                    while( eventStream.HasEvents ) { 
                        eventStream.ProcessEvent( EventSink );
                    } 
                    eventStream.Close( EventSink );
                }

                if ( returnStream ) { 
                    ds = new SqlDataReaderSmi( eventStream, this, cmdBehavior, innerConnection, EventSink );
                    ds.NextResult();    // Position on first set of results 
                    _activeConnection.AddWeakReference(ds, SqlReferenceCollection.DataReaderTag); 
                }
 
                EventSink.ProcessMessagesAndThrow();
            }
            catch {
                if ( null != eventStream ) 
                    eventStream.Close( EventSink );     //
 
                DisposeSmiRequest(); 

                throw; 
            }

        return ds;
        } 

        private SqlDataReader CompleteAsyncExecuteReader() { 
            SqlDataReader ds = cachedAsyncState.CachedAsyncReader; // should not be null 
            bool processFinallyBlock = true;
            try { 
                FinishExecuteReader(ds, cachedAsyncState.CachedRunBehavior, cachedAsyncState.CachedSetOptions);
            }
            catch (Exception e) {
                processFinallyBlock = ADP.IsCatchableExceptionType(e); 
                throw;
            } 
            finally { 
                Debug.Assert(null != Thread.GetData(TdsParser.ReliabilitySlot), "unreliable call to CompleteAsyncExecuteReader");  // you need to setup for a thread abort somewhere before you call this method
                if (processFinallyBlock) { 
                    cachedAsyncState.ResetAsyncState();
                    PutStateObject();
                }
            } 

            return ds; 
        } 

        private void FinishExecuteReader(SqlDataReader ds, RunBehavior runBehavior, string resetOptionsString) { 
            // always wrap with a try { FinishExecuteReader(...) } finally { PutStateObject(); }

            NotifyDependency();
            if (runBehavior == RunBehavior.UntilDone) { 
                try {
                    _stateObj.Parser.Run(RunBehavior.UntilDone, this, ds, null, _stateObj); 
                } 
                catch (Exception e) {
                    // 
                    if (ADP.IsCatchableExceptionType(e)) {
                        if (_inPrepare) {
                            // The flag is expected to be reset by OnReturnValue.  We should receive
                            // the handle unless command execution failed.  If fail, move back to pending 
                            // state.
                            _inPrepare = false;                  // reset the flag 
                            IsDirty = true;                      // mark command as dirty so it will be prepared next time we're comming through 
                            _execType = EXECTYPE.PREPAREPENDING; // reset execution type to pending
                        } 

                        if (null != ds) {
                            ds.Close();
                        } 
                    }
                    throw; 
                } 
            }
 
            // bind the parser to the reader if we get this far
            if (ds != null) {
                ds.Bind(_stateObj);
                _stateObj = null;   // the reader now owns this... 
                ds.ResetOptionsString = resetOptionsString;
 
                // 

 

                // bind this reader to this connection now
                _activeConnection.AddWeakReference(ds, SqlReferenceCollection.DataReaderTag);
 
                // force this command to start reading data off the wire.
                // this will cause an error to be reported at Execute() time instead of Read() time 
                // if the command is not set. 
                try {
                    _cachedMetaData = ds.MetaData; 
                    ds.IsInitialized = true; // Webdata 104560
                }
                catch (Exception e) {
                    // 
                    if (ADP.IsCatchableExceptionType(e)) {
                        if (_inPrepare) { 
                            // The flag is expected to be reset by OnReturnValue.  We should receive 
                            // the handle unless command execution failed.  If fail, move back to pending
                            // state. 
                            _inPrepare = false;                  // reset the flag
                            IsDirty = true;                      // mark command as dirty so it will be prepared next time we're comming through
                            _execType = EXECTYPE.PREPAREPENDING; // reset execution type to pending
                        } 

                        ds.Close(); 
                    } 

                    throw; 
                }
            }
        }
 
        private void NotifyDependency() {
            if (_sqlDep != null) { 
                _sqlDep.StartTimer(Notification); 
            }
        } 

        public SqlCommand Clone() {
            SqlCommand clone = new SqlCommand(this);
            Bid.Trace("<sc.SqlCommand.Clone|API> %d#, clone=%d#\n", ObjectID, clone.ObjectID); 
            return clone;
        } 
 
        object ICloneable.Clone() {
            return Clone(); 
        }

        // validates that a command has commandText and a non-busy open connection
        // throws exception for error case, returns false if the commandText is empty 
        private void ValidateCommand(string method, bool async) {
            if (null == _activeConnection) { 
                throw ADP.ConnectionRequired(method); 
            }
 
            // if the parser is not openloggedin, the connection is no longer good
            SqlInternalConnectionTds tdsConnection = _activeConnection.InnerConnection as SqlInternalConnectionTds;
            if (tdsConnection != null) {
                if (tdsConnection.Parser.State != TdsParserState.OpenLoggedIn) { 
                    if (tdsConnection.Parser.State == TdsParserState.Closed) {
                        throw ADP.OpenConnectionRequired(method, ConnectionState.Closed); 
                    } 
                    throw ADP.OpenConnectionRequired(method, ConnectionState.Broken);
                } 
            }

            ValidateAsyncCommand();
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
#if DEBUG 
                object initialReliabilitySlotValue = Thread.GetData(TdsParser.ReliabilitySlot);
 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    Thread.SetData(TdsParser.ReliabilitySlot, true);
#endif //DEBUG 
                    // close any non MARS dead readers, if applicable, and then throw if still busy.
                    // Throw if we have a live reader on this command 
                    _activeConnection.ValidateConnectionForExecute(method, this); 

#if DEBUG 
                }
                finally {
                    Thread.SetData(TdsParser.ReliabilitySlot, initialReliabilitySlotValue);
                } 
#endif //DEBUG
            } 
            catch (System.OutOfMemoryException e) 
            {
                _activeConnection.Abort(e); 
                throw;
            }
            catch (System.StackOverflowException e)
            { 
                _activeConnection.Abort(e);
                throw; 
            } 
            catch (System.Threading.ThreadAbortException e)
            { 
                _activeConnection.Abort(e);
                throw;
            }
            // Check to see if the currently set transaction has completed.  If so, 
            // null out our local reference.
            if (null != _transaction && _transaction.Connection == null) 
                _transaction = null; 

            // throw if the connection is in a transaction but there is no 
            // locally assigned transaction object
            if (_activeConnection.HasLocalTransactionFromAPI && (null == _transaction))
                throw ADP.TransactionRequired(method);
 
            // if we have a transaction, check to ensure that the active
            // connection property matches the connection associated with 
            // the transaction 
            if (null != _transaction && _activeConnection != _transaction.Connection)
                throw ADP.TransactionConnectionMismatch(); 

            if (ADP.IsEmpty(this.CommandText))
                throw ADP.CommandTextRequired(method);
 
            // Notification property must be null for pre-Yukon connections
            if ((Notification != null) && !_activeConnection.IsYukonOrNewer) { 
                throw SQL.NotificationsRequireYukon(); 
            }
 
            if (async && !_activeConnection.Asynchronous) {
                throw (SQL.AsyncConnectionRequired());
            }
        } 

        private void ValidateAsyncCommand() { 
            // 
            if (cachedAsyncState.PendingAsyncOperation) { // Enforce only one pending async execute at a time.
                if (cachedAsyncState.IsActiveConnectionValid(_activeConnection)) { 
                    throw SQL.PendingBeginXXXExists();
                }
                else {
                    _stateObj = null; // Session was re-claimed by session pool upon connection close. 
                    cachedAsyncState.ResetAsyncState();
                } 
            } 
        }
 
        private void GetStateObject() {
            Debug.Assert (null == _stateObj,"StateObject not null on GetStateObject");
            Debug.Assert (null != _activeConnection, "no active connection?");
 
            if (_pendingCancel) {
                _pendingCancel = false; // Not really needed, but we'll reset anyways. 
 
                // If a pendingCancel exists on the object, we must have had a Cancel() call
                // between the point that we entered an Execute* API and the point in Execute* that 
                // we proceeded to call this function and obtain a stateObject.  In that case,
                // we now throw a cancelled error.
                throw SQL.OperationCancelled();
            } 

            TdsParserStateObject stateObj = _activeConnection.Parser.GetSession(this); 
            stateObj.StartSession(ObjectID); 

            _stateObj = stateObj; 

            if (_pendingCancel) {
                _pendingCancel = false; // Not really needed, but we'll reset anyways.
 
                // If a pendingCancel exists on the object, we must have had a Cancel() call
                // between the point that we entered this function and the point where we obtained 
                // and actually assigned the stateObject to the local member.  It is possible 
                // that the flag is set as well as a call to stateObj.Cancel - though that would
                // be a no-op.  So - throw. 
                throw SQL.OperationCancelled();
            }
         }
 
        private void PutStateObject() {
            TdsParserStateObject stateObj = _stateObj; 
            _stateObj = null; 

            if (null != stateObj) { 
                stateObj.CloseSession();
            }
        }
 
        internal void OnDoneProc() { // called per rpc batch complete
            if (BatchRPCMode) { 
 
                // track the records affected for the just completed rpc batch
                // _rowsAffected is cumulative for ExecuteNonQuery across all rpc batches 
                _SqlRPCBatchArray[_currentlyExecutingBatch].cumulativeRecordsAffected = _rowsAffected;

                _SqlRPCBatchArray[_currentlyExecutingBatch].recordsAffected =
                    (((0 < _currentlyExecutingBatch) && (0 <= _rowsAffected)) 
                        ? (_rowsAffected - Math.Max(_SqlRPCBatchArray[_currentlyExecutingBatch-1].cumulativeRecordsAffected, 0))
                        : _rowsAffected); 
 
                // track the error collection (not available from TdsParser after ExecuteNonQuery)
                // and the which errors are associated with the just completed rpc batch 
                _SqlRPCBatchArray[_currentlyExecutingBatch].errorsIndexStart =
                    ((0 < _currentlyExecutingBatch)
                        ? _SqlRPCBatchArray[_currentlyExecutingBatch-1].errorsIndexEnd
                        : 0); 
                _SqlRPCBatchArray[_currentlyExecutingBatch].errorsIndexEnd = _stateObj.Parser.Errors.Count;
                _SqlRPCBatchArray[_currentlyExecutingBatch].errors = _stateObj.Parser.Errors; 
 
                // track the warning collection (not available from TdsParser after ExecuteNonQuery)
                // and the which warnings are associated with the just completed rpc batch 
                _SqlRPCBatchArray[_currentlyExecutingBatch].warningsIndexStart =
                    ((0 < _currentlyExecutingBatch)
                        ? _SqlRPCBatchArray[_currentlyExecutingBatch-1].warningsIndexEnd
                        : 0); 
                _SqlRPCBatchArray[_currentlyExecutingBatch].warningsIndexEnd = _stateObj.Parser.Warnings.Count;
                _SqlRPCBatchArray[_currentlyExecutingBatch].warnings = _stateObj.Parser.Warnings; 
 
                _currentlyExecutingBatch++;
                Debug.Assert(_parameterCollectionList.Count >= _currentlyExecutingBatch, "OnDoneProc: Too many DONEPROC events"); 
            }
        }

        // 
        //
 
 
        internal void OnReturnStatus(int status) {
            if (_inPrepare) 
                return;

            SqlParameterCollection parameters = _parameters;
            if (BatchRPCMode) { 
                if (_parameterCollectionList.Count > _currentlyExecutingBatch) {
                    parameters = _parameterCollectionList[_currentlyExecutingBatch]; 
                } 
                else {
                    Debug.Assert(false, "OnReturnStatus: SqlCommand got too many DONEPROC events"); 
                    parameters = null;
                }
            }
            // see if a return value is bound 
            int count = GetParameterCount(parameters);
            for (int i = 0; i < count; i++) { 
                SqlParameter parameter = parameters[i]; 
                if (parameter.Direction == ParameterDirection.ReturnValue) {
                    object v = parameter.Value; 

                // if the user bound a sqlint32 (the only valid one for status, use it)
                if ( (null != v) && (v.GetType() == typeof(SqlInt32)) ) {
                        parameter.Value = new SqlInt32(status); // value type 
                }
                else { 
                        parameter.Value = status; 

                    } 
                    break;
                }
            }
        } 

        // 
        // Move the return value to the corresponding output parameter. 
        // Return parameters are sent in the order in which they were defined in the procedure.
        // If named, match the parameter name, otherwise fill in based on ordinal position. 
        // If the parameter is not bound, then ignore the return value.
        //
        internal void OnReturnValue(SqlReturnValue rec) {
 
            if (_inPrepare) {
                if (!rec.value.IsNull) { 
                    _prepareHandle = rec.value.Int32; 
                }
                _inPrepare = false; 
                return;
            }

            SqlParameterCollection parameters = GetCurrentParameterCollection(); 
            int  count      = GetParameterCount(parameters);
 
 
            SqlParameter thisParam = GetParameterForOutputValueExtraction(parameters, rec.parameter, count);
 
            if (null != thisParam) {
                // copy over data

                // if the value user has supplied a SqlType class, then just copy over the SqlType, otherwise convert 
                // to the com type
                object val = thisParam.Value; 
 
                //set the UDT value as typed object rather than bytes
                if (SqlDbType.Udt == thisParam.SqlDbType) { 
                    object data = null;
                    try {
                        SqlConnection.CheckGetExtendedUDTInfo(rec, true);
 
                        //extract the byte array from the param value
                        if (rec.value.IsNull) 
                            data = DBNull.Value; 
                        else {
                            data = rec.value.ByteArray; //should work for both sql and non-sql values 
                        }

                        //call the connection to instantiate the UDT object
                        thisParam.Value = Connection.GetUdtValue(data, rec, false); 
                    }
                    catch (FileNotFoundException e) { 
                        // SQL BU DT 329981 
                        // Assign Assembly.Load failure in case where assembly not on client.
                        // This allows execution to complete and failure on SqlParameter.Value. 
                        thisParam.SetUdtLoadError(e);
                    }
                    catch (FileLoadException e) {
                        // SQL BU DT 329981 
                        // Assign Assembly.Load failure in case where assembly cannot be loaded on client.
                        // This allows execution to complete and failure on SqlParameter.Value. 
                        thisParam.SetUdtLoadError(e); 
                    }
 
                    return;
                } else {
                    thisParam.SetSqlBuffer(rec.value);
                } 

                MetaType mt = MetaType.GetMetaTypeFromSqlDbType(rec.type, rec.isMultiValued); 
 
                if (rec.type == SqlDbType.Decimal) {
                    thisParam.ScaleInternal = rec.scale; 
                    thisParam.PrecisionInternal = rec.precision;
                }
                else if (mt.IsVarTime) {
                    thisParam.ScaleInternal = rec.scale; 
                }
                else if (rec.type == SqlDbType.Xml) { 
                    SqlCachedBuffer cachedBuffer = (thisParam.Value as SqlCachedBuffer); 
                    if (null != cachedBuffer) {
                        thisParam.Value = cachedBuffer.ToString(); 
                    }
                }

                if (rec.collation != null) { 
                    Debug.Assert(mt.IsCharType, "Invalid collation structure for non-char type");
                    thisParam.Collation = rec.collation; 
                } 
            }
 
            return;
        }

        internal void OnParametersAvailableSmi( SmiParameterMetaData[] paramMetaData, ITypedGettersV3 parameterValues ) { 
            Debug.Assert(null != paramMetaData);
 
            for(int index=0; index < paramMetaData.Length; index++) { 
                OnParameterAvailableSmi(paramMetaData[index], parameterValues, index);
            } 
        }

        internal void OnParameterAvailableSmi(SmiParameterMetaData metaData, ITypedGettersV3 parameterValues, int ordinal) {
            if ( ParameterDirection.Input != metaData.Direction ) { 
                string name = null;
                if (ParameterDirection.ReturnValue != metaData.Direction) { 
                    name = metaData.Name; 
                }
 
                SqlParameterCollection parameters = GetCurrentParameterCollection();
                int  count      = GetParameterCount(parameters);
                SqlParameter param = GetParameterForOutputValueExtraction(parameters, name, count);
 
                if ( null != param ) {
                    param.LocaleId = (int)metaData.LocaleId; 
                    param.CompareInfo = metaData.CompareOptions; 
                    SqlBuffer buffer = new SqlBuffer();
                    object result; 
                    if (_activeConnection.IsKatmaiOrNewer) {
                        result = ValueUtilsSmi.GetOutputParameterV200Smi(
                                OutParamEventSink, (SmiTypedGetterSetter)parameterValues, ordinal, metaData, _smiRequestContext, buffer );
                    } 
                    else {
                        result = ValueUtilsSmi.GetOutputParameterV3Smi( 
                                    OutParamEventSink, parameterValues, ordinal, metaData, _smiRequestContext, buffer ); 
                    }
                    if ( null != result ) { 
                        param.Value = result;
                    }
                    else {
                        param.SetSqlBuffer( buffer ); 
                    }
                } 
            } 
        }
 
        private SqlParameterCollection GetCurrentParameterCollection() {
            if (BatchRPCMode) {
                if (_parameterCollectionList.Count > _currentlyExecutingBatch) {
                    return _parameterCollectionList[_currentlyExecutingBatch]; 
                }
                else { 
                    Debug.Assert(false, "OnReturnValue: SqlCommand got too many DONEPROC events"); 
                    return null;
                } 
            }
            else {
                return _parameters;
            } 
        }
 
        private SqlParameter GetParameterForOutputValueExtraction( SqlParameterCollection parameters, 
                        string paramName, int paramCount ) {
            SqlParameter thisParam = null; 
            bool foundParam = false;

            if (null == paramName) {
                // rec.parameter should only be null for a return value from a function 
                for (int i = 0; i < paramCount; i++) {
                    thisParam = parameters[i]; 
                    // searching for ReturnValue 
                    if (thisParam.Direction == ParameterDirection.ReturnValue) {
                                foundParam = true; 
                            break; // found it
                    }
                }
            } 
            else {
                for (int i = 0; i < paramCount; i++) { 
                    thisParam = parameters[i]; 
                    // searching for Output or InputOutput or ReturnValue with matching name
                    if (thisParam.Direction != ParameterDirection.Input && thisParam.Direction != ParameterDirection.ReturnValue  && paramName == thisParam.ParameterNameFixed) { 
                                foundParam = true;
                            break; // found it
                        }
                    } 
            }
            if (foundParam) 
                return thisParam; 
            else
                return null; 
        }

        private void GetRPCObject(int paramCount, ref _SqlRPC rpc) {
 
            // Designed to minimize necessary allocations
            int ii; 
            if (rpc == null) { 
                if (_rpcArrayOf1 == null) {
                    _rpcArrayOf1 = new _SqlRPC[1]; 
                    _rpcArrayOf1[0] = new _SqlRPC();
                }
                rpc = _rpcArrayOf1[0] ;
            } 

            rpc.ProcID = 0; 
            rpc.rpcName = null; 
            rpc.options = 0;
 
            rpc.recordsAffected = default(int?);
            rpc.cumulativeRecordsAffected = -1;

            rpc.errorsIndexStart = 0; 
            rpc.errorsIndexEnd = 0;
            rpc.errors = null; 
 
            rpc.warningsIndexStart = 0;
            rpc.warningsIndexEnd = 0; 
            rpc.warnings = null;

            // Make sure there is enough space in the parameters and paramoptions arrays
            if(rpc.parameters == null || rpc.parameters.Length < paramCount) { 
                rpc.parameters = new SqlParameter[paramCount];
            } 
            else if (rpc.parameters.Length > paramCount) { 
                        rpc.parameters[paramCount]=null;    // Terminator
            } 
            if(rpc.paramoptions == null || (rpc.paramoptions.Length < paramCount)) {
                rpc.paramoptions = new byte[paramCount];
            }
            else { 
                for (ii = 0 ; ii < paramCount ; ii++)
                    rpc.paramoptions[ii] = 0; 
            } 
        }
 
        private void SetUpRPCParameters (_SqlRPC rpc, int startCount, bool inSchema, SqlParameterCollection parameters) {
            int ii;
            int paramCount = GetParameterCount(parameters) ;
            int j = startCount; 
            TdsParser parser = _activeConnection.Parser;
#if WINFSFunctionality 
            bool isWinfs = parser.IsWinFS; 
#endif
            bool yukonOrNewer = parser.IsYukonOrNewer; 

            for (ii = 0;  ii < paramCount; ii++) {
                SqlParameter parameter = parameters[ii];
#if WINFSFunctionality 
                parameter.Validate(ii, isWinfs);
#else 
                parameter.Validate(ii, CommandType.StoredProcedure == CommandType); 
#endif
 
                // func will change type to that with a 4 byte length if the type has a two
                // byte length and a parameter length > than that expressable in 2 bytes
#if WINFSFunctionality
                parameter.ValidateTypeLengths(yukonOrNewer, isWinfs); 
#else
                parameter.ValidateTypeLengths(yukonOrNewer); 
#endif 

                if (ShouldSendParameter(parameter)) { 
                    rpc.parameters[j] = parameter;

                    // set output bit
                    if (parameter.Direction == ParameterDirection.InputOutput || 
                        parameter.Direction == ParameterDirection.Output)
                        rpc.paramoptions[j] = TdsEnums.RPC_PARAM_BYREF; 
 
                    // set default value bit
                    if (parameter.Direction != ParameterDirection.Output) { 
                        // remember that null == Convert.IsEmpty, DBNull.Value is a database null!

                        // MDAC 62117, don't assume a default value exists for parameters in the case when
                        // the user is simply requesting schema 
                        if (null == parameter.Value && !inSchema) {
                            rpc.paramoptions[j] |= TdsEnums.RPC_PARAM_DEFAULT; 
                        } 
                    }
 
                    // Must set parameter option bit for LOB_COOKIE if unfilled LazyMat blob
#if WINFSFunctionality
                    if (isWinfs && parameter.IsNonFilledLazyMatInstance()) {
                        rpc.paramoptions[j] |= TdsEnums.RPC_PARAM_IS_LOB_COOKIE; 
                    }
#endif 
                    j++; 
                }
            } 

        }

        // 
        // 7.5
        // prototype for sp_prepexec is: 
        // sp_prepexec(@handle int IN/OUT, @batch_params ntext, @batch_text ntext, param1value,param2value...) 
        //
        private _SqlRPC  BuildPrepExec(CommandBehavior behavior) { 
            Debug.Assert(System.Data.CommandType.Text == this.CommandType, "invalid use of sp_prepexec for stored proc invocation!");
            SqlParameter sqlParam;
            int j = 3;
 
            int count = CountSendableParameters(_parameters);
 
            _SqlRPC rpc = null; 
            GetRPCObject(count + j, ref rpc);
 
            rpc.ProcID = TdsEnums.RPC_PROCID_PREPEXEC;
            rpc.rpcName = TdsEnums.SP_PREPEXEC;

            //@handle 
            sqlParam = new SqlParameter(null, SqlDbType.Int);
            sqlParam.Direction = ParameterDirection.InputOutput; 
            sqlParam.Value = _prepareHandle; 
            rpc.parameters[0] = sqlParam;
            rpc.paramoptions[0] = TdsEnums.RPC_PARAM_BYREF; 

            //@batch_params
            string paramList = BuildParamList(_stateObj.Parser, _parameters);
            sqlParam = new SqlParameter(null, ((paramList.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, paramList.Length); 
            sqlParam.Value = paramList;
            rpc.parameters[1] = sqlParam; 
 
            //@batch_text
            string text = GetCommandText(behavior); 
            sqlParam = new SqlParameter(null, ((text.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, text.Length);
            sqlParam.Value = text;
            rpc.parameters[2] = sqlParam;
 
            SetUpRPCParameters (rpc,  j, false, _parameters);
            return rpc; 
        } 

 
        //
        // returns true if the parameter is not a return value
        // and it's value is not DBNull (for a nullable parameter)
        // 
        private static bool ShouldSendParameter(SqlParameter p) {
            switch (p.Direction) { 
            case ParameterDirection.ReturnValue: 
                // return value parameters are never sent
                return false; 
            case ParameterDirection.Output:
            case ParameterDirection.InputOutput:
            case ParameterDirection.Input:
                // InputOutput/Output parameters are aways sent 
                return true;
            default: 
                Debug.Assert(false, "Invalid ParameterDirection!"); 
                return false;
            } 
        }

        private int CountSendableParameters(SqlParameterCollection parameters) {
            int cParams = 0; 

            if (parameters != null) { 
                int count = parameters.Count; 
                for (int i = 0; i < count; i++) {
                    if (ShouldSendParameter(parameters[i])) 
                        cParams++;
                }
            }
            return cParams; 
        }
 
        // Returns total number of parameters 
        private int GetParameterCount(SqlParameterCollection parameters) {
            return ((null != parameters) ? parameters.Count : 0); 
        }

        //
        // build the RPC record header for this stored proc and add parameters 
        //
        private void BuildRPC(bool inSchema, SqlParameterCollection parameters, ref _SqlRPC rpc) { 
            Debug.Assert(this.CommandType == System.Data.CommandType.StoredProcedure, "Command must be a stored proc to execute an RPC"); 
            int count = CountSendableParameters(parameters);
            GetRPCObject(count, ref rpc); 

            rpc.rpcName = this.CommandText; // just get the raw command text

            SetUpRPCParameters ( rpc, 0, inSchema, parameters); 
        }
 
        // 
        // build the RPC record header for sp_unprepare
        // 
        // prototype for sp_unprepare is:
        // sp_unprepare(@handle)
        //
        // 
        private _SqlRPC BuildUnprepare() {
            Debug.Assert(_prepareHandle != 0, "Invalid call to sp_unprepare without a valid handle!"); 
 
            _SqlRPC rpc = null;
            GetRPCObject(1, ref rpc); 
            SqlParameter sqlParam;

            rpc.ProcID = TdsEnums.RPC_PROCID_UNPREPARE;
            rpc.rpcName = TdsEnums.SP_UNPREPARE; 

            //@handle 
            sqlParam = new SqlParameter(null, SqlDbType.Int); 
            sqlParam.Value = _prepareHandle;
            rpc.parameters[0] = sqlParam; 

            return rpc;
        }
 
        //
        // build the RPC record header for sp_execute 
        // 
        // prototype for sp_execute is:
        // sp_execute(@handle int,param1value,param2value...) 
        //
        private _SqlRPC BuildExecute(bool inSchema) {
            Debug.Assert(_prepareHandle != -1, "Invalid call to sp_execute without a valid handle!");
            int j = 1; 

            int count = CountSendableParameters(_parameters); 
 
            _SqlRPC rpc = null;
            GetRPCObject(count + j, ref rpc); 

            SqlParameter sqlParam;

            rpc.ProcID = TdsEnums.RPC_PROCID_EXECUTE; 
            rpc.rpcName = TdsEnums.SP_EXECUTE;
 
            //@handle 
            sqlParam = new SqlParameter(null, SqlDbType.Int);
            sqlParam.Value = _prepareHandle; 
            rpc.parameters[0] = sqlParam;

            SetUpRPCParameters (rpc, j, inSchema, _parameters);
            return rpc; 
        }
 
        // 
        // build the RPC record header for sp_executesql and add the parameters
        // 
        // prototype for sp_executesql is:
        // sp_executesql(@batch_text nvarchar(4000),@batch_params nvarchar(4000), param1,.. paramN)
        private void BuildExecuteSql(CommandBehavior behavior, string commandText, SqlParameterCollection parameters, ref _SqlRPC rpc) {
 
            Debug.Assert(_prepareHandle == -1, "This command has an existing handle, use sp_execute!");
            Debug.Assert(System.Data.CommandType.Text == this.CommandType, "invalid use of sp_executesql for stored proc invocation!"); 
            int j; 
            SqlParameter sqlParam;
 
            int cParams = CountSendableParameters(parameters);
            if (cParams > 0) {
                j = 2;
            } 
            else {
                j =1; 
            } 

            GetRPCObject(cParams + j, ref rpc); 
            rpc.ProcID = TdsEnums.RPC_PROCID_EXECUTESQL;
            rpc.rpcName = TdsEnums.SP_EXECUTESQL;

            // @sql 
            if (commandText == null) {
                commandText = GetCommandText(behavior); 
            } 
            sqlParam = new SqlParameter(null, ((commandText.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, commandText.Length);
            sqlParam.Value = commandText; 
            rpc.parameters[0] = sqlParam;

            if (cParams > 0) {
                string paramList = BuildParamList(_stateObj.Parser, BatchRPCMode  ? parameters : _parameters); 
                sqlParam = new SqlParameter(null, ((paramList.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, paramList.Length);
                sqlParam.Value = paramList; 
                rpc.parameters[1] = sqlParam; 

                bool inSchema =  (0 != (behavior & CommandBehavior.SchemaOnly)); 
                SetUpRPCParameters (rpc, j,  inSchema, parameters);
            }
        }
 
        // paramList parameter for sp_executesql, sp_prepare, and sp_prepexec
        internal string BuildParamList(TdsParser parser, SqlParameterCollection parameters) { 
            StringBuilder paramList = new StringBuilder(); 
            bool fAddSeperator = false;
 
            bool yukonOrNewer = parser.IsYukonOrNewer;
            int count = 0;

            count = parameters.Count; 
            for (int i = 0; i < count; i++) {
                SqlParameter sqlParam = parameters[i]; 
#if WINFSFunctionality 
                sqlParam.Validate(i, parser.IsWinFS);
#else 
                sqlParam.Validate(i, CommandType.StoredProcedure == CommandType);
#endif
                // skip ReturnValue parameters; we never send them to the server
                if (!ShouldSendParameter(sqlParam)) 
                    continue;
 
                // add our separator for the ith parmeter 
                if (fAddSeperator)
                    paramList.Append(','); 

                paramList.Append(sqlParam.ParameterNameFixed);

                MetaType mt = sqlParam.InternalMetaType; 

                //for UDTs, get the actual type name. Get only the typename, omitt catalog and schema names. 
                //in TSQL you should only specify the unqualified type name 

                // paragraph above doesn't seem to be correct. Server won't find the type 
                // if we don't provide a fully qualified name
                paramList.Append(" ");
                if (mt.SqlDbType == SqlDbType.Udt) {
                    string fullTypeName = sqlParam.UdtTypeName; 
                    if(ADP.IsEmpty(fullTypeName))
                        throw SQL.MustSetUdtTypeNameForUdtParams(); 
                    // DEVNOTE: do we need to escape the full type name? 
                    paramList.Append(fullTypeName);
                } 
                else if (mt.SqlDbType == SqlDbType.Structured) {
                    string typeName = sqlParam.TypeName;
                    if (ADP.IsEmpty(typeName)) {
                        throw SQL.MustSetTypeNameForParam(mt.TypeName, sqlParam.ParameterNameFixed); 
                    }
                    paramList.Append(typeName); 
 
                    // TVPs currently are the only Structured type and must be read only, so add that keyword
                    paramList.Append(" READONLY"); 
                }
                else {
                    // func will change type to that with a 4 byte length if the type has a two
                    // byte length and a parameter length > than that expressable in 2 bytes 
#if WINFSFunctionality
                    mt  = sqlParam.ValidateTypeLengths(yukonOrNewer, parser.IsWinFS); 
#else 
                    mt  = sqlParam.ValidateTypeLengths(yukonOrNewer);
#endif 
                    paramList.Append(mt.TypeName);
                }

                fAddSeperator = true; 

                if (mt.SqlDbType == SqlDbType.Decimal) { 
                    byte precision = sqlParam.GetActualPrecision(); 
                    byte scale = sqlParam.GetActualScale();
 
                    paramList.Append('(');

                    if (0 == precision) {
                        if (IsShiloh) { 
                            precision = TdsEnums.DEFAULT_NUMERIC_PRECISION;
                        } else { 
                            precision = TdsEnums.SPHINX_DEFAULT_NUMERIC_PRECISION; 
                        }
                    } 

                    paramList.Append(precision);
                    paramList.Append(',');
                    paramList.Append(scale); 
                    paramList.Append(')');
                } 
                else if (mt.IsVarTime) { 
                    byte scale = sqlParam.GetActualScale();
 
                    paramList.Append('(');
                    paramList.Append(scale);
                    paramList.Append(')');
                } 
                else if (false == mt.IsFixed && false == mt.IsLong && mt.SqlDbType != SqlDbType.Timestamp && mt.SqlDbType != SqlDbType.Udt && SqlDbType.Structured != mt.SqlDbType) {
                    int size = sqlParam.Size; 
 
                    paramList.Append('(');
 
                    // if using non unicode types, obtain the actual byte length from the parser, with it's associated code page
                    if (mt.IsAnsiType) {
#if WINFSFunctionality
                        object val = sqlParam.GetCoercedValue(parser.IsWinFS); 
#else
                        object val = sqlParam.GetCoercedValue(); 
#endif 
                        string s = null;
 
                        // deal with the sql types
                        if ((null != val) && (DBNull.Value != val)) {
                            s = (val as string);
                            if (null == s) { 
                                SqlString sval = val is SqlString ? (SqlString)val : SqlString.Null;
                                if (!sval.IsNull) { 
                                    s = sval.Value; 
                                }
                            } 
                        }

                        if (null != s) {
#if WINFSFunctionality 
                            int actualBytes = parser.GetEncodingCharLength(s, sqlParam.GetActualSize(parser.IsWinFS), sqlParam.Offset, null);
#else 
                            int actualBytes = parser.GetEncodingCharLength(s, sqlParam.GetActualSize(), sqlParam.Offset, null); 
#endif
                            // if actual number of bytes is greater than the user given number of chars, use actual bytes 
                            if (actualBytes > size)
                                size = actualBytes;
                        }
                    } 

                    // bug 49497, if the user specifies a 0-sized parameter for a variable len field 
                    // pass over max size (8000 bytes or 4000 characters for wide types) 
                    if (0 == size)
                        size = mt.IsSizeInCharacters ? (TdsEnums.MAXSIZE >> 1) : TdsEnums.MAXSIZE; 

                    paramList.Append(size);
                    paramList.Append(')');
                } 
                else if (mt.IsPlp && (mt.SqlDbType != SqlDbType.Xml) && (mt.SqlDbType != SqlDbType.Udt)) {
                    paramList.Append("(max) "); 
                } 

                // set the output bit for Output or InputOutput parameters 
                if (sqlParam.Direction != ParameterDirection.Input)
                    paramList.Append(" " + TdsEnums.PARAM_OUTPUT);
            }
 
            return paramList.ToString();
        } 
 
        // returns set option text to turn on format only and key info on and off
        // @devnote:  When we are executing as a text command, then we never need 
        // to turn off the options since they command text is executed in the scope of sp_executesql.
        // For a stored proc command, however, we must send over batch sql and then turn off
        // the set options after we read the data.  See the code in Command.Execute()
        private string GetSetOptionsString(CommandBehavior behavior) { 
            string s = null;
 
            if ((System.Data.CommandBehavior.SchemaOnly == (behavior & CommandBehavior.SchemaOnly)) || 
               (System.Data.CommandBehavior.KeyInfo == (behavior & CommandBehavior.KeyInfo))) {
 
                // MDAC 56898 - SET FMTONLY ON will cause the server to ignore other SET OPTIONS, so turn
                // it off before we ask for browse mode metadata
                s = TdsEnums.FMTONLY_OFF;
 
                if (System.Data.CommandBehavior.KeyInfo == (behavior & CommandBehavior.KeyInfo)) {
                    s = s + TdsEnums.BROWSE_ON; 
                } 

                if (System.Data.CommandBehavior.SchemaOnly == (behavior & CommandBehavior.SchemaOnly)) { 
                    s = s + TdsEnums.FMTONLY_ON;
                }
            }
 
            return s;
        } 
 
        private string GetResetOptionsString(CommandBehavior behavior) {
            string s = null; 

            // SET FMTONLY ON OFF
            if (System.Data.CommandBehavior.SchemaOnly == (behavior & CommandBehavior.SchemaOnly)) {
                s = s + TdsEnums.FMTONLY_OFF; 
            }
 
            // SET NO_BROWSETABLE OFF 
            if (System.Data.CommandBehavior.KeyInfo == (behavior & CommandBehavior.KeyInfo)) {
                s = s + TdsEnums.BROWSE_OFF; 
            }

            return s;
        } 

        private String GetCommandText(CommandBehavior behavior) { 
            // build the batch string we send over, since we execute within a stored proc (sp_executesql), the SET options never need to be 
            // turned off since they are scoped to the sproc
            Debug.Assert(System.Data.CommandType.Text == this.CommandType, "invalid call to GetCommandText for stored proc!"); 
            return GetSetOptionsString(behavior) + this.CommandText;
        }

        // 
        // build the RPC record header for sp_executesql and add the parameters
        // 
        // the prototype for sp_prepare is: 
        // sp_prepare(@handle int OUTPUT, @batch_params ntext, @batch_text ntext, @options int default 0x1)
        private _SqlRPC BuildPrepare(CommandBehavior behavior) { 
            Debug.Assert(System.Data.CommandType.Text == this.CommandType, "invalid use of sp_prepare for stored proc invocation!");

            _SqlRPC rpc = null;
            GetRPCObject(3, ref rpc); 
            SqlParameter sqlParam;
 
            rpc.ProcID = TdsEnums.RPC_PROCID_PREPARE; 
            rpc.rpcName = TdsEnums.SP_PREPARE;
 
            //@handle
            sqlParam = new SqlParameter(null, SqlDbType.Int);
            sqlParam.Direction = ParameterDirection.Output;
            rpc.parameters[0] = sqlParam; 
            rpc.paramoptions[0] = TdsEnums.RPC_PARAM_BYREF;
 
            //@batch_params 
            string paramList = BuildParamList(_stateObj.Parser, _parameters);
            sqlParam = new SqlParameter(null, ((paramList.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, paramList.Length); 
            sqlParam.Value = paramList;
            rpc.parameters[1] = sqlParam;

            //@batch_text 
            string text = GetCommandText(behavior);
            sqlParam = new SqlParameter(null, ((text.Length<<1)<=TdsEnums.TYPE_SIZE_LIMIT)?SqlDbType.NVarChar:SqlDbType.NText, text.Length); 
            sqlParam.Value = text; 
            rpc.parameters[2] = sqlParam;
 
/*
            //@options
            sqlParam = new SqlParameter(null, SqlDbType.Int);
            rpc.Parameters[3] = sqlParam; 
*/
            return rpc; 
        } 

        private void CheckThrowSNIException() { 
            if (null != _stateObj && _stateObj._error != null) {
                _stateObj.Parser.Errors.Add(_stateObj._error);
                _stateObj._error = null;
                _stateObj.Parser.ThrowExceptionAndWarning(_stateObj); 
            }
        } 
 
        private bool IsPrepared {
            get { return(_execType != EXECTYPE.UNPREPARED);} 
        }

        private bool IsUserPrepared {
            get { return IsPrepared && !_hiddenPrepare && !IsDirty; } 
        }
 
        internal bool IsDirty { 
            get {
                // only dirty if prepared 
                return (IsPrepared && (_dirty || ((null != _parameters) && _parameters.IsDirty)));
            }
            set {
                // only mark the command as dirty if it is already prepared 
                // but always clear the value if it we are clearing the dirty flag
                _dirty = value ? IsPrepared : false; 
                if (null != _parameters) { 
                    _parameters.IsDirty = _dirty;
                } 
                _cachedMetaData = null;
            }
        }
 
        internal int InternalRecordsAffected {
            get { 
                return _rowsAffected; 
            }
            set { 
                if (-1 == _rowsAffected) {
                    _rowsAffected = value;
                }
                else if (0 < value) { 
                    _rowsAffected += value;
                } 
            } 
        }
 
        internal bool BatchRPCMode {
            get {
                return _batchRPCMode;
            } 
            set {
                _batchRPCMode = value; 
 
                if (_batchRPCMode == false) {
                    ClearBatchCommand(); 
                } else {
                    if (_RPCList == null) {
                        _RPCList = new List<_SqlRPC>();
                    } 
                    if (_parameterCollectionList == null) {
                        _parameterCollectionList = new List<SqlParameterCollection>(); 
                    } 
                }
            } 
        }

        internal void ClearBatchCommand() {
            List<_SqlRPC> rpcList = _RPCList; 
            if (null != rpcList) {
                rpcList.Clear(); 
            } 
            if (null != _parameterCollectionList) {
                _parameterCollectionList.Clear(); 
            }
            _SqlRPCBatchArray = null;
            _currentlyExecutingBatch = 0;
        } 

        internal void AddBatchCommand(string commandText, SqlParameterCollection parameters, CommandType cmdType) { 
            Debug.Assert(BatchRPCMode, "Command is not in batch RPC Mode"); 
            Debug.Assert(_RPCList != null);
            Debug.Assert(_parameterCollectionList != null); 

            _SqlRPC  rpc = new _SqlRPC();

            this.CommandText = commandText; 
            this.CommandType = cmdType;
            GetStateObject(); 
            if (cmdType == CommandType.StoredProcedure) { 
                BuildRPC(false, parameters, ref rpc);
            } 
            else {
                // All batch sql statements must be executed inside sp_executesql, including those without parameters
                BuildExecuteSql(CommandBehavior.Default, commandText, parameters, ref rpc);
            } 
             _RPCList.Add(rpc);
             // Always add a parameters collection per RPC, even if there are no parameters. 
             _parameterCollectionList.Add(parameters); 
            PutStateObject();
        } 

        internal int ExecuteBatchRPCCommand() {

            Debug.Assert(BatchRPCMode, "Command is not in batch RPC Mode"); 
            Debug.Assert(_RPCList != null, "No batch commands specified");
            _SqlRPCBatchArray = _RPCList.ToArray(); 
            _currentlyExecutingBatch = 0; 
            return ExecuteNonQuery();       // Check permissions, execute, return output params
 
        }

        internal int? GetRecordsAffected(int commandIndex) {
            Debug.Assert(BatchRPCMode, "Command is not in batch RPC Mode"); 
            Debug.Assert(_SqlRPCBatchArray != null, "batch command have been cleared");
            return _SqlRPCBatchArray[commandIndex].recordsAffected; 
        } 

        internal SqlException GetErrors(int commandIndex) { 
            SqlException result = null;
            int length = (_SqlRPCBatchArray[commandIndex].errorsIndexEnd - _SqlRPCBatchArray[commandIndex].errorsIndexStart);
            if (0 < length) {
                SqlErrorCollection errors = new SqlErrorCollection(); 
                for(int i = _SqlRPCBatchArray[commandIndex].errorsIndexStart; i < _SqlRPCBatchArray[commandIndex].errorsIndexEnd; ++i) {
                    errors.Add(_SqlRPCBatchArray[commandIndex].errors[i]); 
                } 
                for(int i = _SqlRPCBatchArray[commandIndex].warningsIndexStart; i < _SqlRPCBatchArray[commandIndex].warningsIndexEnd; ++i) {
                    errors.Add(_SqlRPCBatchArray[commandIndex].warnings[i]); 
                }
                result = SqlException.CreateException(errors, Connection.ServerVersion);
            }
            return result; 
        }
 
        private void DisposeSmiRequest() { 
            if ( null != _smiRequest ) {
                SmiRequestExecutor smiRequest = _smiRequest; 
                _smiRequest = null;
                _smiRequestContext = null; // not entirely necessary, but good to do for debugging/GC
                smiRequest.Close(EventSink);
                EventSink.ProcessMessagesAndThrow(); 
            }
        } 
 
        // Allocates and initializes a new SmiRequestExecutor based on the current command state
        private void SetUpSmiRequest( SqlInternalConnectionSmi innerConnection ) { 

            // General Approach To Ensure Security of Marshalling:
            //        Only touch each item in the command once
            //        (i.e. only grab a reference to each param once, only 
            //        read the type from that param once, etc.).  The problem is
            //        that if the user changes something on the command in the 
            //        middle of marshaling, it can overwrite the native buffers 
            //        set up.  For example, if max length is used to allocate
            //        buffers, but then re-read from the parameter to truncate 
            //        strings, the user could extend the length and overwrite
            //        the buffer.

            // Clean up a bit first 
            //
 
 
                DisposeSmiRequest();
//            } 


            if (null != Notification){
                throw SQL.NotificationsNotAvailableOnContextConnection(); 
            }
 
            SmiParameterMetaData[] requestMetaData = null; 
            ParameterPeekAheadValue[] peekAheadValues = null;
 
            // Do we need to create a new request?
//            if ( null == _smiRequest ) {
                //    Length of rgMetadata becomes *the* official count of parameters to use,
                //      don't rely on Parameters.Count after this point, as the user could change it. 
                int count = GetParameterCount( Parameters );
                if ( 0 < count ) { 
                    requestMetaData = new SmiParameterMetaData[count]; 
                    peekAheadValues = new ParameterPeekAheadValue[count];
 
                    // set up the metadata
                    for ( int index=0; index<count; index++ ) {
                        SqlParameter param = Parameters[index];
#if WINFSFunctionality 
                        param.Validate(index, false); // SMI doesn't support LazyMat yet.
#else 
                        param.Validate(index, CommandType.StoredProcedure == CommandType); 
#endif
                        requestMetaData[index] = param.MetaDataForSmi(out peekAheadValues[index]); 

                        // Check for valid type for version negotiated
                        if (!innerConnection.IsKatmaiOrNewer) {
                            MetaType mt = MetaType.GetMetaTypeFromSqlDbType(requestMetaData[index].SqlDbType, requestMetaData[index].IsMultiValued); 
                            if (!mt.Is90Supported) {
                                throw ADP.VersionDoesNotSupportDataType(mt.TypeName); 
                            } 
                        }
                    } 
                }

                // Allocate the new request
                CommandType cmdType = CommandType; 
                _smiRequestContext = innerConnection.InternalContext;
                _smiRequest = _smiRequestContext.CreateRequestExecutor( 
                                        CommandText, 
                                        cmdType,
                                        requestMetaData, 
                                        EventSink
                                    );

                // deal with errors 
                EventSink.ProcessMessagesAndThrow();
//            } // 
 
            // Now assign param values
            for ( int index=0; index<count; index++ ) { 
                if ( ParameterDirection.Output != requestMetaData[index].Direction &&
                        ParameterDirection.ReturnValue != requestMetaData[index].Direction ) {
                    SqlParameter param = Parameters[index];
                    // going back to command for parameter is ok, since we'll only pick up values now. 
#if WINFSFunctionality
                    object value = param.GetCoercedValue(false); // SMI doesn't support LazyMat yet. 
#else 
                    object value = param.GetCoercedValue();
#endif 
                    ExtendedClrTypeCode typeCode = MetaDataUtilsSmi.DetermineExtendedTypeCodeForUseWithSqlDbType(requestMetaData[index].SqlDbType, requestMetaData[index].IsMultiValued, value, null /* parameters don't use CLR Type for UDTs */, SmiContextFactory.Instance.NegotiatedSmiVersion);

                    // Handle null reference as special case for parameters
                    if ( CommandType.StoredProcedure == cmdType && 
                                ExtendedClrTypeCode.Empty == typeCode ) {
                        _smiRequest.SetDefault( index ); 
                    } 
                    else {
                        // 



                        int size = param.Size; 
                        if (size != 0 && size != SmiMetaData.UnlimitedMaxLengthIndicator && !param.SizeInferred) {
                            switch(requestMetaData[index].SqlDbType) { 
                                case SqlDbType.Image: 
                                case SqlDbType.Text:
                                    if (size != Int32.MaxValue) { 
                                        throw SQL.ParameterSizeRestrictionFailure(index);
                                    }
                                    break;
 
                                case SqlDbType.NText:
                                    if (size != Int32.MaxValue/2) { 
                                        throw SQL.ParameterSizeRestrictionFailure(index); 
                                    }
                                    break; 

                                case SqlDbType.VarBinary:
                                case SqlDbType.VarChar:
                                    // Allow size==Int32.MaxValue because of DeriveParameters 
                                    if (size > 0 && size != Int32.MaxValue && requestMetaData[index].MaxLength == SmiMetaData.UnlimitedMaxLengthIndicator) {
                                        throw SQL.ParameterSizeRestrictionFailure(index); 
                                    } 
                                    break;
 
                                case SqlDbType.NVarChar:
                                    // Allow size==Int32.MaxValue/2 because of DeriveParameters
                                    if (size > 0 && size != Int32.MaxValue/2 && requestMetaData[index].MaxLength == SmiMetaData.UnlimitedMaxLengthIndicator) {
                                        throw SQL.ParameterSizeRestrictionFailure(index); 
                                    }
                                    break; 
 
                                case SqlDbType.Timestamp:
                                    // Size limiting for larger values will happen due to MaxLength 
                                    if (size < SmiMetaData.DefaultTimestamp.MaxLength) {
                                        throw SQL.ParameterSizeRestrictionFailure(index);
                                    }
                                    break; 

                                case SqlDbType.Variant: 
                                    // Variant problems happen when Size is less than maximums for character and binary values 
                                    // Size limiting for larger values will happen due to MaxLength
                                    // NOTE: assumes xml and udt types are handled in parameter value coercion 
                                    //      since server does not allow these types in a variant
                                    if (null != value) {
                                        MetaType mt = MetaType.GetMetaTypeFromValue(value);
 
                                        if ((mt.IsNCharType && size < SmiMetaData.MaxUnicodeCharacters) ||
                                                (mt.IsBinType && size < SmiMetaData.MaxBinaryLength) || 
                                                (mt.IsAnsiType && size < SmiMetaData.MaxANSICharacters)) { 
                                            throw SQL.ParameterSizeRestrictionFailure(index);
                                        } 
                                    }
                                    break;

                                 case SqlDbType.Xml: 
                                    // Xml is an issue for non-SqlXml types
                                    if (null != value && ExtendedClrTypeCode.SqlXml != typeCode) { 
                                        throw SQL.ParameterSizeRestrictionFailure(index); 
                                    }
                                    break; 

                                 // NOTE: Char, NChar, Binary and UDT do not need restricting because they are always 8k or less,
                                 //         so the metadata MaxLength will match the Size setting.
 
                                default:
                                    break; 
                            } 
                        }
 
                        if (innerConnection.IsKatmaiOrNewer) {
                            ValueUtilsSmi.SetCompatibleValueV200(EventSink, _smiRequest, index, requestMetaData[index], value, typeCode, param.Offset, param.Size, peekAheadValues[index]);
                        }
                        else { 
                            ValueUtilsSmi.SetCompatibleValue( EventSink, _smiRequest, index, requestMetaData[index], value, typeCode, param.Offset );
                        } 
                    } 
                }
            } 
        }
    }
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
