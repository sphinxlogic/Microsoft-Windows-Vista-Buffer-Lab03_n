// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  OperationCanceledException 
**
** 
** Purpose: Exception for cancelled IO requests.
**
**
===========================================================*/ 

using System; 
using System.Runtime.Serialization; 

namespace System { 

 	[Serializable]
	[System.Runtime.InteropServices.ComVisible(true)]
	public class OperationCanceledException : SystemException 
	{
 		public OperationCanceledException() 
			: base(Environment.GetResourceString("OperationCanceled")) { 
 			SetErrorCode(__HResults.COR_E_OPERATIONCANCELED);
 		} 
		
 		public OperationCanceledException(String message)
			: base(message) {
			SetErrorCode(__HResults.COR_E_OPERATIONCANCELED); 
		}
 	 
		public OperationCanceledException(String message, Exception innerException) 
 			: base(message, innerException) {
 			SetErrorCode(__HResults.COR_E_OPERATIONCANCELED); 
		}
 	
		protected OperationCanceledException(SerializationInfo info, StreamingContext context) : base (info, context) {
		} 
	}
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  OperationCanceledException 
**
** 
** Purpose: Exception for cancelled IO requests.
**
**
===========================================================*/ 

using System; 
using System.Runtime.Serialization; 

namespace System { 

 	[Serializable]
	[System.Runtime.InteropServices.ComVisible(true)]
	public class OperationCanceledException : SystemException 
	{
 		public OperationCanceledException() 
			: base(Environment.GetResourceString("OperationCanceled")) { 
 			SetErrorCode(__HResults.COR_E_OPERATIONCANCELED);
 		} 
		
 		public OperationCanceledException(String message)
			: base(message) {
			SetErrorCode(__HResults.COR_E_OPERATIONCANCELED); 
		}
 	 
		public OperationCanceledException(String message, Exception innerException) 
 			: base(message, innerException) {
 			SetErrorCode(__HResults.COR_E_OPERATIONCANCELED); 
		}
 	
		protected OperationCanceledException(SerializationInfo info, StreamingContext context) : base (info, context) {
		} 
	}
} 
