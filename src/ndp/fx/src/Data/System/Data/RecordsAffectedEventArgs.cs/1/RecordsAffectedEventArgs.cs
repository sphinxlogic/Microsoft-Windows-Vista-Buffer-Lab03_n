//------------------------------------------------------------------------------ 
// <copyright file="StatementCompletedEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data {
    using System; 

#if WINFSInternalOnly
    internal
#else 
    public
#endif 
    sealed class StatementCompletedEventArgs : System.EventArgs { 
        private readonly int _recordCount;
 
        public StatementCompletedEventArgs(int recordCount) {
            _recordCount = recordCount;
        }
 
        public int RecordCount {
            get { 
                return _recordCount; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="StatementCompletedEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data {
    using System; 

#if WINFSInternalOnly
    internal
#else 
    public
#endif 
    sealed class StatementCompletedEventArgs : System.EventArgs { 
        private readonly int _recordCount;
 
        public StatementCompletedEventArgs(int recordCount) {
            _recordCount = recordCount;
        }
 
        public int RecordCount {
            get { 
                return _recordCount; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
