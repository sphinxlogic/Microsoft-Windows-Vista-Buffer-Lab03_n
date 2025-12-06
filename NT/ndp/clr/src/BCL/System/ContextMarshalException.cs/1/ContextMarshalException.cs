// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: ContextMarshalException 
**
** 
** Purpose: Exception class for attempting to pass an instance through a context
**          boundary, when the formal type and the instance's marshal style are
**          incompatible.
** 
**
=============================================================================*/ 
 
namespace System {
 	using System.Runtime.InteropServices; 
	using System.Runtime.Remoting;
	using System;
	using System.Runtime.Serialization;
    [Obsolete("ContextMarshalException is obsolete.")] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable()] public class ContextMarshalException : SystemException { 
        public ContextMarshalException() 
            : base(Environment.GetResourceString("Arg_ContextMarshalException")) {
    		SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL); 
        }

        public ContextMarshalException(String message)
            : base(message) { 
    		SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL);
        } 
    	 
        public ContextMarshalException(String message, Exception inner)
            : base(message, inner) { 
    		SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL);
        }

        protected ContextMarshalException(SerializationInfo info, StreamingContext context) : base(info, context) { 
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
** Class: ContextMarshalException 
**
** 
** Purpose: Exception class for attempting to pass an instance through a context
**          boundary, when the formal type and the instance's marshal style are
**          incompatible.
** 
**
=============================================================================*/ 
 
namespace System {
 	using System.Runtime.InteropServices; 
	using System.Runtime.Remoting;
	using System;
	using System.Runtime.Serialization;
    [Obsolete("ContextMarshalException is obsolete.")] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable()] public class ContextMarshalException : SystemException { 
        public ContextMarshalException() 
            : base(Environment.GetResourceString("Arg_ContextMarshalException")) {
    		SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL); 
        }

        public ContextMarshalException(String message)
            : base(message) { 
    		SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL);
        } 
    	 
        public ContextMarshalException(String message, Exception inner)
            : base(message, inner) { 
    		SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL);
        }

        protected ContextMarshalException(SerializationInfo info, StreamingContext context) : base(info, context) { 
        }
 
    } 

} 
