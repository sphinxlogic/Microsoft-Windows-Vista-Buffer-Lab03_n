// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: SemaphoreFullException 
**
** 
=============================================================================*/
namespace System.Threading {
    using System;
    using System.Runtime.Serialization; 
    using System.Runtime.InteropServices;
 
    [Serializable()] 
    [ComVisibleAttribute(false)]
    public class SemaphoreFullException : SystemException { 

        public SemaphoreFullException() : base(SR.GetString(SR.Threading_SemaphoreFullException)){
        }
 
        public SemaphoreFullException(String message) : base(message) {
        } 
 
        public SemaphoreFullException(String message, Exception innerException) : base(message, innerException) {
        } 

        protected SemaphoreFullException(SerializationInfo info, StreamingContext context) : base (info, context) {
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
** Class: SemaphoreFullException 
**
** 
=============================================================================*/
namespace System.Threading {
    using System;
    using System.Runtime.Serialization; 
    using System.Runtime.InteropServices;
 
    [Serializable()] 
    [ComVisibleAttribute(false)]
    public class SemaphoreFullException : SystemException { 

        public SemaphoreFullException() : base(SR.GetString(SR.Threading_SemaphoreFullException)){
        }
 
        public SemaphoreFullException(String message) : base(message) {
        } 
 
        public SemaphoreFullException(String message, Exception innerException) : base(message, innerException) {
        } 

        protected SemaphoreFullException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    } 
}
 
