//------------------------------------------------------------------------------ 
// <copyright file="SqlException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
 
    using System;
    using System.ComponentModel;
    using System.Data.Common;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Runtime.Serialization; 
    using System.Text; // StringBuilder 

    [Serializable] 
#if WINFSInternalOnly
    internal
#else
    public 
#endif
    sealed class SqlException : System.Data.Common.DbException { 
        private SqlErrorCollection _errors; 

        private SqlException(string message, SqlErrorCollection errorCollection) : base(message) { 
            HResult = HResults.SqlException;
            _errors = errorCollection;
        }
 
        // runtime will call even if private...
        private SqlException(SerializationInfo si, StreamingContext sc) : base(si, sc) { 
            _errors = (SqlErrorCollection) si.GetValue("Errors", typeof(SqlErrorCollection)); 
            HResult = HResults.SqlException;
        } 

        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Flags=System.Security.Permissions.SecurityPermissionFlag.SerializationFormatter)]
        override public void GetObjectData(SerializationInfo si, StreamingContext context) {
            if (null == si) { 
                throw new ArgumentNullException("si");
            } 
            si.AddValue("Errors", _errors, typeof(SqlErrorCollection)); 
            base.GetObjectData(si, context);
        } 

        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content)
        ] 
        public SqlErrorCollection Errors {
            get { 
                if (_errors == null) { 
                    _errors = new SqlErrorCollection();
                } 
                return _errors;
            }
        }
 
        /*virtual protected*/private bool ShouldSerializeErrors() { // MDAC 65548
            return ((null != _errors) && (0 < _errors.Count)); 
        } 

        public byte Class { 
            get { return this.Errors[0].Class;}
        }

        public int LineNumber { 
            get { return this.Errors[0].LineNumber;}
        } 
 
        public int Number {
            get { return this.Errors[0].Number;} 
        }

        public string Procedure {
            get { return this.Errors[0].Procedure;} 
        }
 
        public string Server { 
            get { return this.Errors[0].Server;}
        } 

        public byte State {
            get { return this.Errors[0].State;}
        } 

        override public string Source { 
            get { return this.Errors[0].Source;} 
        }
 
        static internal SqlException CreateException(SqlErrorCollection errorCollection, string serverVersion) {
            Debug.Assert(null != errorCollection, "no errorCollection?");

            // concat all messages together MDAC 65533 
            StringBuilder message = new StringBuilder();
            for (int i = 0; i < errorCollection.Count; i++) { 
                if (i > 0) { 
                    message.Append(Environment.NewLine);
                } 
                message.Append(errorCollection[i].Message);
            }
            SqlException exception = new SqlException(message.ToString(), errorCollection);
 
            exception.Data.Add("HelpLink.ProdName",    "Microsoft SQL Server");
 
            if (!ADP.IsEmpty(serverVersion)) { 
                exception.Data.Add("HelpLink.ProdVer", serverVersion);
            } 
            exception.Data.Add("HelpLink.EvtSrc",      "MSSQLServer");
            exception.Data.Add("HelpLink.EvtID",       errorCollection[0].Number.ToString(CultureInfo.InvariantCulture));
            exception.Data.Add("HelpLink.BaseHelpUrl", "http://go.microsoft.com/fwlink");
            exception.Data.Add("HelpLink.LinkId",      "20476"); 

            return exception; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
 
    using System;
    using System.ComponentModel;
    using System.Data.Common;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Runtime.Serialization; 
    using System.Text; // StringBuilder 

    [Serializable] 
#if WINFSInternalOnly
    internal
#else
    public 
#endif
    sealed class SqlException : System.Data.Common.DbException { 
        private SqlErrorCollection _errors; 

        private SqlException(string message, SqlErrorCollection errorCollection) : base(message) { 
            HResult = HResults.SqlException;
            _errors = errorCollection;
        }
 
        // runtime will call even if private...
        private SqlException(SerializationInfo si, StreamingContext sc) : base(si, sc) { 
            _errors = (SqlErrorCollection) si.GetValue("Errors", typeof(SqlErrorCollection)); 
            HResult = HResults.SqlException;
        } 

        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Flags=System.Security.Permissions.SecurityPermissionFlag.SerializationFormatter)]
        override public void GetObjectData(SerializationInfo si, StreamingContext context) {
            if (null == si) { 
                throw new ArgumentNullException("si");
            } 
            si.AddValue("Errors", _errors, typeof(SqlErrorCollection)); 
            base.GetObjectData(si, context);
        } 

        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content)
        ] 
        public SqlErrorCollection Errors {
            get { 
                if (_errors == null) { 
                    _errors = new SqlErrorCollection();
                } 
                return _errors;
            }
        }
 
        /*virtual protected*/private bool ShouldSerializeErrors() { // MDAC 65548
            return ((null != _errors) && (0 < _errors.Count)); 
        } 

        public byte Class { 
            get { return this.Errors[0].Class;}
        }

        public int LineNumber { 
            get { return this.Errors[0].LineNumber;}
        } 
 
        public int Number {
            get { return this.Errors[0].Number;} 
        }

        public string Procedure {
            get { return this.Errors[0].Procedure;} 
        }
 
        public string Server { 
            get { return this.Errors[0].Server;}
        } 

        public byte State {
            get { return this.Errors[0].State;}
        } 

        override public string Source { 
            get { return this.Errors[0].Source;} 
        }
 
        static internal SqlException CreateException(SqlErrorCollection errorCollection, string serverVersion) {
            Debug.Assert(null != errorCollection, "no errorCollection?");

            // concat all messages together MDAC 65533 
            StringBuilder message = new StringBuilder();
            for (int i = 0; i < errorCollection.Count; i++) { 
                if (i > 0) { 
                    message.Append(Environment.NewLine);
                } 
                message.Append(errorCollection[i].Message);
            }
            SqlException exception = new SqlException(message.ToString(), errorCollection);
 
            exception.Data.Add("HelpLink.ProdName",    "Microsoft SQL Server");
 
            if (!ADP.IsEmpty(serverVersion)) { 
                exception.Data.Add("HelpLink.ProdVer", serverVersion);
            } 
            exception.Data.Add("HelpLink.EvtSrc",      "MSSQLServer");
            exception.Data.Add("HelpLink.EvtID",       errorCollection[0].Number.ToString(CultureInfo.InvariantCulture));
            exception.Data.Add("HelpLink.BaseHelpUrl", "http://go.microsoft.com/fwlink");
            exception.Data.Add("HelpLink.LinkId",      "20476"); 

            return exception; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
