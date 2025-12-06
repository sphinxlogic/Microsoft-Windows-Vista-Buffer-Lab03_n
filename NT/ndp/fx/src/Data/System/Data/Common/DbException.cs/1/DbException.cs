//------------------------------------------------------------------------------ 
// <copyright file="DbException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    [Serializable]
#if WINFSInternalOnly
    internal
#else 
    public
#endif 
    abstract class DbException : System.Runtime.InteropServices.ExternalException { 

        protected DbException() : base() { 
        }

        protected DbException(System.String message) : base(message) {
        } 

        protected DbException(System.String message, System.Exception innerException) : base(message, innerException) { 
        } 

        protected DbException(System.String message, System.Int32 errorCode) : base(message, errorCode) { 
        }

        protected DbException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) {
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    [Serializable]
#if WINFSInternalOnly
    internal
#else 
    public
#endif 
    abstract class DbException : System.Runtime.InteropServices.ExternalException { 

        protected DbException() : base() { 
        }

        protected DbException(System.String message) : base(message) {
        } 

        protected DbException(System.String message, System.Exception innerException) : base(message, innerException) { 
        } 

        protected DbException(System.String message, System.Int32 errorCode) : base(message, errorCode) { 
        }

        protected DbException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) {
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
