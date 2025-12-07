//------------------------------------------------------------------------------ 
// <copyright file="SqlReferenceCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
using System;
using System.Data; 
using System.Data.Common;
using System.Diagnostics;
using System.Data.ProviderBase;
 
namespace System.Data.SqlClient {
    sealed internal class SqlReferenceCollection : DbReferenceCollection { 
        internal const int DataReaderTag  = 1; 

        private int _dataReaderCount; 

        internal bool MayHaveDataReader {
            get {
                return (0 != _dataReaderCount); 
            }
        } 
 
        override public void Add(object value, int tag) {
            Debug.Assert(DataReaderTag == tag, "unexpected tag?"); 
            Debug.Assert(value is SqlDataReader, "tag doesn't match object type: SqlDataReader");
            _dataReaderCount++;

            base.AddItem(value, tag); 
        }
 
        internal void Deactivate() { 
            if (MayHaveDataReader) {
                base.Notify(0); 
                _dataReaderCount = 0;
            }
            Purge();
        } 

        internal SqlDataReader FindLiveReader(SqlCommand command) { 
            // if null == command, will find first live datareader 
            // else will find live datareader assocated with the command
            if (MayHaveDataReader) { 
                foreach (SqlDataReader dataReader in Filter(DataReaderTag)) {
                    if ((null != dataReader) && !dataReader.IsClosed && ((null == command) || (command == dataReader.Command))) {
                        return dataReader;
                    } 
                }
            } 
            return null; 
        }
 
        override protected bool NotifyItem(int message, int tag, object value) {
            Debug.Assert(0 == message, "unexpected message?");
            Debug.Assert(DataReaderTag == tag, "unexpected tag?");
            SqlDataReader rdr = (SqlDataReader)value; 

            if (!rdr.IsClosed) { 
                rdr.CloseReaderFromConnection (); 
            }
            return false;   // remove it from the collection 
        }

        override public void Remove(object value) {
            Debug.Assert(value is SqlDataReader, "SqlReferenceCollection.Remove expected a SqlDataReader"); 
            _dataReaderCount--;
            base.RemoveItem(value); 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlReferenceCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
using System;
using System.Data; 
using System.Data.Common;
using System.Diagnostics;
using System.Data.ProviderBase;
 
namespace System.Data.SqlClient {
    sealed internal class SqlReferenceCollection : DbReferenceCollection { 
        internal const int DataReaderTag  = 1; 

        private int _dataReaderCount; 

        internal bool MayHaveDataReader {
            get {
                return (0 != _dataReaderCount); 
            }
        } 
 
        override public void Add(object value, int tag) {
            Debug.Assert(DataReaderTag == tag, "unexpected tag?"); 
            Debug.Assert(value is SqlDataReader, "tag doesn't match object type: SqlDataReader");
            _dataReaderCount++;

            base.AddItem(value, tag); 
        }
 
        internal void Deactivate() { 
            if (MayHaveDataReader) {
                base.Notify(0); 
                _dataReaderCount = 0;
            }
            Purge();
        } 

        internal SqlDataReader FindLiveReader(SqlCommand command) { 
            // if null == command, will find first live datareader 
            // else will find live datareader assocated with the command
            if (MayHaveDataReader) { 
                foreach (SqlDataReader dataReader in Filter(DataReaderTag)) {
                    if ((null != dataReader) && !dataReader.IsClosed && ((null == command) || (command == dataReader.Command))) {
                        return dataReader;
                    } 
                }
            } 
            return null; 
        }
 
        override protected bool NotifyItem(int message, int tag, object value) {
            Debug.Assert(0 == message, "unexpected message?");
            Debug.Assert(DataReaderTag == tag, "unexpected tag?");
            SqlDataReader rdr = (SqlDataReader)value; 

            if (!rdr.IsClosed) { 
                rdr.CloseReaderFromConnection (); 
            }
            return false;   // remove it from the collection 
        }

        override public void Remove(object value) {
            Debug.Assert(value is SqlDataReader, "SqlReferenceCollection.Remove expected a SqlDataReader"); 
            _dataReaderCount--;
            base.RemoveItem(value); 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
