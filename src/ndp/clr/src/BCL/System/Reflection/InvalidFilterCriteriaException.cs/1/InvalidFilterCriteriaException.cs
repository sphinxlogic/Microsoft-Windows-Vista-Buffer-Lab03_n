// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////// 
// 
// InvalidFilterCriteriaException is thrown in FindMembers when the
//	filter criteria is not valid for the type of filter being used. 
//
//
//
// 
namespace System.Reflection {
 
 	using System; 
	using System.Runtime.Serialization;
	using ApplicationException = System.ApplicationException; 
	[Serializable()]
[System.Runtime.InteropServices.ComVisible(true)]
    public class InvalidFilterCriteriaException  : ApplicationException {
 
        public InvalidFilterCriteriaException()
 	        : base(Environment.GetResourceString("Arg_InvalidFilterCriteriaException")) { 
    		SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA); 
        }
 
        public InvalidFilterCriteriaException(String message) : base(message) {
    		SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA);
        }
    	 
        public InvalidFilterCriteriaException(String message, Exception inner) : base(message, inner) {
    		SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA); 
        } 

        protected InvalidFilterCriteriaException(SerializationInfo info, StreamingContext context) : base(info, context) { 
        }

    }
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////// 
// 
// InvalidFilterCriteriaException is thrown in FindMembers when the
//	filter criteria is not valid for the type of filter being used. 
//
//
//
// 
namespace System.Reflection {
 
 	using System; 
	using System.Runtime.Serialization;
	using ApplicationException = System.ApplicationException; 
	[Serializable()]
[System.Runtime.InteropServices.ComVisible(true)]
    public class InvalidFilterCriteriaException  : ApplicationException {
 
        public InvalidFilterCriteriaException()
 	        : base(Environment.GetResourceString("Arg_InvalidFilterCriteriaException")) { 
    		SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA); 
        }
 
        public InvalidFilterCriteriaException(String message) : base(message) {
    		SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA);
        }
    	 
        public InvalidFilterCriteriaException(String message, Exception inner) : base(message, inner) {
    		SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA); 
        } 

        protected InvalidFilterCriteriaException(SerializationInfo info, StreamingContext context) : base(info, context) { 
        }

    }
} 
