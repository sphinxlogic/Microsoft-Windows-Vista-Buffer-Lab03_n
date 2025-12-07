//------------------------------------------------------------------------------ 
// <copyright file="DbTransaction.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    using System;
    using System.Data;

#if WINFSInternalOnly 
    internal
#else 
    public 
#endif
    abstract class DbTransaction : MarshalByRefObject, IDbTransaction { // V1.2.3300 
        protected DbTransaction() : base() {
        }

        public DbConnection Connection { 
            get {
                return DbConnection; 
            } 
        }
 
        IDbConnection IDbTransaction.Connection {
            get {
                return DbConnection;
            } 
        }
 
        abstract protected DbConnection DbConnection { 
            get;
        } 

        abstract public IsolationLevel IsolationLevel {
            get;
        } 

        abstract public void Commit(); 
 
        public void Dispose() {
            Dispose(true); 
        }

        protected virtual void Dispose(bool disposing) {
        } 

        abstract public void Rollback(); 
 
    }
 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbTransaction.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    using System;
    using System.Data;

#if WINFSInternalOnly 
    internal
#else 
    public 
#endif
    abstract class DbTransaction : MarshalByRefObject, IDbTransaction { // V1.2.3300 
        protected DbTransaction() : base() {
        }

        public DbConnection Connection { 
            get {
                return DbConnection; 
            } 
        }
 
        IDbConnection IDbTransaction.Connection {
            get {
                return DbConnection;
            } 
        }
 
        abstract protected DbConnection DbConnection { 
            get;
        } 

        abstract public IsolationLevel IsolationLevel {
            get;
        } 

        abstract public void Commit(); 
 
        public void Dispose() {
            Dispose(true); 
        }

        protected virtual void Dispose(bool disposing) {
        } 

        abstract public void Rollback(); 
 
    }
 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
