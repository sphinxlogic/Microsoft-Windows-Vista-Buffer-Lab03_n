//------------------------------------------------------------------------------ 
// <copyright file="DbConnectionClosed.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Data.ProviderBase { 

    using System; 
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Threading; 
    using SysTx = System.Transactions; 

    abstract internal class DbConnectionClosed : DbConnectionInternal { 

        // Construct an "empty" connection
        protected DbConnectionClosed(ConnectionState state, bool hidePassword, bool allowSetConnectionString) : base(state, hidePassword, allowSetConnectionString) {
        } 

        override public string ServerVersion { 
            get { 
                throw ADP.ClosedConnectionError();
            } 
        }

        override protected void Activate(SysTx.Transaction transaction) {
            throw ADP.ClosedConnectionError(); 
        }
 
        override public DbTransaction BeginTransaction(IsolationLevel il) { 
            throw ADP.ClosedConnectionError();
        } 

        override public void ChangeDatabase(string database) {
            throw ADP.ClosedConnectionError();
        } 

        internal override void CloseConnection(DbConnection owningObject, DbConnectionFactory connectionFactory) { 
            // not much to do here... 
        }
 
        override protected void Deactivate() {
            throw ADP.ClosedConnectionError();
        }
 
        override public void EnlistTransaction(SysTx.Transaction transaction) {
            throw ADP.ClosedConnectionError(); 
        } 

        override protected internal DataTable GetSchema(DbConnectionFactory factory, DbConnectionPoolGroup poolGroup, DbConnection outerConnection, string collectionName, string[] restrictions) { 
            throw ADP.ClosedConnectionError();
        }

        internal override void OpenConnection(DbConnection outerConnection, DbConnectionFactory connectionFactory) { 
            // Closed->Connecting: prevent set_ConnectionString during Open
            if (connectionFactory.SetInnerConnectionFrom(outerConnection, DbConnectionClosedConnecting.SingletonInstance, this)) { 
                DbConnectionInternal openConnection = null; 
                try {
                    connectionFactory.PermissionDemand(outerConnection); 
                    openConnection = connectionFactory.GetConnection(outerConnection);
                }
                catch {
                    // This should occure for all exceptions, even ADP.UnCatchableExceptions. 
                    connectionFactory.SetInnerConnectionTo(outerConnection, this);
                    throw; 
                } 
                if (null == openConnection) {
                    connectionFactory.SetInnerConnectionTo(outerConnection, this); 
                    throw ADP.InternalConnectionError(ADP.ConnectionError.GetConnectionReturnsNull);
                }
                connectionFactory.SetInnerConnectionEvent(outerConnection, openConnection);
            } 
        }
    } 
 
    abstract internal class DbConnectionBusy : DbConnectionClosed {
 
        protected DbConnectionBusy(ConnectionState state) : base(state, true, false) {
        }

        internal override void OpenConnection(DbConnection outerConnection, DbConnectionFactory connectionFactory) { 
            throw ADP.ConnectionAlreadyOpen(State);
        } 
    } 

    sealed internal class DbConnectionClosedBusy : DbConnectionBusy { 
        // Closed Connection, Currently Busy - changing connection string
        internal static readonly DbConnectionInternal SingletonInstance = new DbConnectionClosedBusy();   // singleton object

            private DbConnectionClosedBusy() : base(ConnectionState.Closed) { 
        }
    } 
 
    sealed internal class DbConnectionOpenBusy : DbConnectionBusy {
        // Open Connection, Currently Busy - closing connection 
        internal static readonly DbConnectionInternal SingletonInstance = new DbConnectionOpenBusy();   // singleton object

        private DbConnectionOpenBusy() : base(ConnectionState.Open) {
        } 
    }
 
    sealed internal class DbConnectionClosedConnecting : DbConnectionBusy { 
        // Closed Connection, Currently Connecting
 
        internal static readonly DbConnectionInternal SingletonInstance = new DbConnectionClosedConnecting();   // singleton object

        private DbConnectionClosedConnecting() : base(ConnectionState.Connecting) {
        } 

    } 
 
    sealed internal class DbConnectionClosedNeverOpened : DbConnectionClosed {
        // Closed Connection, Has Never Been Opened 

        internal static readonly DbConnectionInternal SingletonInstance = new DbConnectionClosedNeverOpened();   // singleton object

        private DbConnectionClosedNeverOpened() : base(ConnectionState.Closed, false, true) { 
        }
    } 
 
    sealed internal class DbConnectionClosedPreviouslyOpened : DbConnectionClosed {
        // Closed Connection, Has Previously Been Opened 

        internal static readonly DbConnectionInternal SingletonInstance = new DbConnectionClosedPreviouslyOpened();   // singleton object

        private DbConnectionClosedPreviouslyOpened() : base(ConnectionState.Closed, true, true) { 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbConnectionClosed.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Data.ProviderBase { 

    using System; 
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Threading; 
    using SysTx = System.Transactions; 

    abstract internal class DbConnectionClosed : DbConnectionInternal { 

        // Construct an "empty" connection
        protected DbConnectionClosed(ConnectionState state, bool hidePassword, bool allowSetConnectionString) : base(state, hidePassword, allowSetConnectionString) {
        } 

        override public string ServerVersion { 
            get { 
                throw ADP.ClosedConnectionError();
            } 
        }

        override protected void Activate(SysTx.Transaction transaction) {
            throw ADP.ClosedConnectionError(); 
        }
 
        override public DbTransaction BeginTransaction(IsolationLevel il) { 
            throw ADP.ClosedConnectionError();
        } 

        override public void ChangeDatabase(string database) {
            throw ADP.ClosedConnectionError();
        } 

        internal override void CloseConnection(DbConnection owningObject, DbConnectionFactory connectionFactory) { 
            // not much to do here... 
        }
 
        override protected void Deactivate() {
            throw ADP.ClosedConnectionError();
        }
 
        override public void EnlistTransaction(SysTx.Transaction transaction) {
            throw ADP.ClosedConnectionError(); 
        } 

        override protected internal DataTable GetSchema(DbConnectionFactory factory, DbConnectionPoolGroup poolGroup, DbConnection outerConnection, string collectionName, string[] restrictions) { 
            throw ADP.ClosedConnectionError();
        }

        internal override void OpenConnection(DbConnection outerConnection, DbConnectionFactory connectionFactory) { 
            // Closed->Connecting: prevent set_ConnectionString during Open
            if (connectionFactory.SetInnerConnectionFrom(outerConnection, DbConnectionClosedConnecting.SingletonInstance, this)) { 
                DbConnectionInternal openConnection = null; 
                try {
                    connectionFactory.PermissionDemand(outerConnection); 
                    openConnection = connectionFactory.GetConnection(outerConnection);
                }
                catch {
                    // This should occure for all exceptions, even ADP.UnCatchableExceptions. 
                    connectionFactory.SetInnerConnectionTo(outerConnection, this);
                    throw; 
                } 
                if (null == openConnection) {
                    connectionFactory.SetInnerConnectionTo(outerConnection, this); 
                    throw ADP.InternalConnectionError(ADP.ConnectionError.GetConnectionReturnsNull);
                }
                connectionFactory.SetInnerConnectionEvent(outerConnection, openConnection);
            } 
        }
    } 
 
    abstract internal class DbConnectionBusy : DbConnectionClosed {
 
        protected DbConnectionBusy(ConnectionState state) : base(state, true, false) {
        }

        internal override void OpenConnection(DbConnection outerConnection, DbConnectionFactory connectionFactory) { 
            throw ADP.ConnectionAlreadyOpen(State);
        } 
    } 

    sealed internal class DbConnectionClosedBusy : DbConnectionBusy { 
        // Closed Connection, Currently Busy - changing connection string
        internal static readonly DbConnectionInternal SingletonInstance = new DbConnectionClosedBusy();   // singleton object

            private DbConnectionClosedBusy() : base(ConnectionState.Closed) { 
        }
    } 
 
    sealed internal class DbConnectionOpenBusy : DbConnectionBusy {
        // Open Connection, Currently Busy - closing connection 
        internal static readonly DbConnectionInternal SingletonInstance = new DbConnectionOpenBusy();   // singleton object

        private DbConnectionOpenBusy() : base(ConnectionState.Open) {
        } 
    }
 
    sealed internal class DbConnectionClosedConnecting : DbConnectionBusy { 
        // Closed Connection, Currently Connecting
 
        internal static readonly DbConnectionInternal SingletonInstance = new DbConnectionClosedConnecting();   // singleton object

        private DbConnectionClosedConnecting() : base(ConnectionState.Connecting) {
        } 

    } 
 
    sealed internal class DbConnectionClosedNeverOpened : DbConnectionClosed {
        // Closed Connection, Has Never Been Opened 

        internal static readonly DbConnectionInternal SingletonInstance = new DbConnectionClosedNeverOpened();   // singleton object

        private DbConnectionClosedNeverOpened() : base(ConnectionState.Closed, false, true) { 
        }
    } 
 
    sealed internal class DbConnectionClosedPreviouslyOpened : DbConnectionClosed {
        // Closed Connection, Has Previously Been Opened 

        internal static readonly DbConnectionInternal SingletonInstance = new DbConnectionClosedPreviouslyOpened();   // singleton object

        private DbConnectionClosedPreviouslyOpened() : base(ConnectionState.Closed, true, true) { 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
