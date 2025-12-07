// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: ExternalException 
**
** 
** Purpose: Exception base class for all errors from Interop or Structured
**          Exception Handling code.
**
** 
=============================================================================*/
 
namespace System.Runtime.InteropServices { 

 	using System; 
	using System.Runtime.Serialization;
    // Base exception for COM Interop errors &; Structured Exception Handler
    // exceptions.
    // 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable()] public class ExternalException : SystemException { 
        public ExternalException() 
            : base(Environment.GetResourceString("Arg_ExternalException")) {
    		SetErrorCode(__HResults.E_FAIL); 
        }
    	
        public ExternalException(String message)
            : base(message) { 
    		SetErrorCode(__HResults.E_FAIL);
        } 
    	 
        public ExternalException(String message, Exception inner)
            : base(message, inner) { 
    		SetErrorCode(__HResults.E_FAIL);
        }

		public ExternalException(String message,int errorCode) 
            : base(message) {
    		SetErrorCode(errorCode); 
        } 

        protected ExternalException(SerializationInfo info, StreamingContext context) : base(info, context) { 
        }

		public virtual int ErrorCode {
    		get { return HResult; } 
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
** Class: ExternalException 
**
** 
** Purpose: Exception base class for all errors from Interop or Structured
**          Exception Handling code.
**
** 
=============================================================================*/
 
namespace System.Runtime.InteropServices { 

 	using System; 
	using System.Runtime.Serialization;
    // Base exception for COM Interop errors &; Structured Exception Handler
    // exceptions.
    // 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable()] public class ExternalException : SystemException { 
        public ExternalException() 
            : base(Environment.GetResourceString("Arg_ExternalException")) {
    		SetErrorCode(__HResults.E_FAIL); 
        }
    	
        public ExternalException(String message)
            : base(message) { 
    		SetErrorCode(__HResults.E_FAIL);
        } 
    	 
        public ExternalException(String message, Exception inner)
            : base(message, inner) { 
    		SetErrorCode(__HResults.E_FAIL);
        }

		public ExternalException(String message,int errorCode) 
            : base(message) {
    		SetErrorCode(errorCode); 
        } 

        protected ExternalException(SerializationInfo info, StreamingContext context) : base(info, context) { 
        }

		public virtual int ErrorCode {
    		get { return HResult; } 
        }
    } 
} 
