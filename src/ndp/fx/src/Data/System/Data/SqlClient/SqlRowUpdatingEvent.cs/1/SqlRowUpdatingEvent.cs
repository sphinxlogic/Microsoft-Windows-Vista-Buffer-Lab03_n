//------------------------------------------------------------------------------ 
// <copyright file="SqlRowUpdatingEvent.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
    using System; 
    using System.Data;
    using System.Data.Common;

#if WINFSInternalOnly 
    internal
#else 
    public 
#endif
    sealed class SqlRowUpdatingEventArgs : RowUpdatingEventArgs { 

        public SqlRowUpdatingEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        : base(row, command, statementType, tableMapping) {
        } 

        new public SqlCommand Command { 
            get { return (base.Command as SqlCommand); } 
            set { base.Command = value; }
        } 

        override protected IDbCommand BaseCommand {
            get { return base.BaseCommand; }
            set { base.BaseCommand = (value as SqlCommand); } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlRowUpdatingEvent.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
    using System; 
    using System.Data;
    using System.Data.Common;

#if WINFSInternalOnly 
    internal
#else 
    public 
#endif
    sealed class SqlRowUpdatingEventArgs : RowUpdatingEventArgs { 

        public SqlRowUpdatingEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        : base(row, command, statementType, tableMapping) {
        } 

        new public SqlCommand Command { 
            get { return (base.Command as SqlCommand); } 
            set { base.Command = value; }
        } 

        override protected IDbCommand BaseCommand {
            get { return base.BaseCommand; }
            set { base.BaseCommand = (value as SqlCommand); } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
