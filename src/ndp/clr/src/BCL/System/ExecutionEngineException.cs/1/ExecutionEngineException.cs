// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: ExecutionEngineException 
**
** 
** Purpose: The exception class for misc execution engine exceptions.
**
**
=============================================================================*/ 

namespace System { 
 
 	using System;
	using System.Runtime.Serialization; 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable()] public sealed class ExecutionEngineException : SystemException {
        public ExecutionEngineException()
            : base(Environment.GetResourceString("Arg_ExecutionEngineException")) { 
    		SetErrorCode(__HResults.COR_E_EXECUTIONENGINE);
        } 
 
        public ExecutionEngineException(String message)
            : base(message) { 
    		SetErrorCode(__HResults.COR_E_EXECUTIONENGINE);
        }

        public ExecutionEngineException(String message, Exception innerException) 
            : base(message, innerException) {
    		SetErrorCode(__HResults.COR_E_EXECUTIONENGINE); 
        } 

        internal ExecutionEngineException(SerializationInfo info, StreamingContext context) : base(info, context) { 
        }
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: ExecutionEngineException 
**
** 
** Purpose: The exception class for misc execution engine exceptions.
**
**
=============================================================================*/ 

namespace System { 
 
 	using System;
	using System.Runtime.Serialization; 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable()] public sealed class ExecutionEngineException : SystemException {
        public ExecutionEngineException()
            : base(Environment.GetResourceString("Arg_ExecutionEngineException")) { 
    		SetErrorCode(__HResults.COR_E_EXECUTIONENGINE);
        } 
 
        public ExecutionEngineException(String message)
            : base(message) { 
    		SetErrorCode(__HResults.COR_E_EXECUTIONENGINE);
        }

        public ExecutionEngineException(String message, Exception innerException) 
            : base(message, innerException) {
    		SetErrorCode(__HResults.COR_E_EXECUTIONENGINE); 
        } 

        internal ExecutionEngineException(SerializationInfo info, StreamingContext context) : base(info, context) { 
        }
    }
}
