 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
/*
 



*/ 

namespace System.Data.Design { 
 
    using System;
 	using System.Design; 
	using System.Diagnostics;
	using System.Runtime.Serialization;

    [Serializable] 
    internal class InternalException : Exception, ISerializable {
        private const string internalExceptionMessageID = "ERR_INTERNAL"; 
 
        private string internalMessage = String.Empty;
        // showErrorMesageOnReport let you hide the sensitive error msg to the end user. 
        private bool   showErrorMesageOnReport;
        private int errorCode = -1;

        internal InternalException(string internalMessage) : this(internalMessage, null) {} 

        internal InternalException(string internalMessage, Exception innerException): this(innerException, internalMessage, -1, false) { 
        } 

        internal InternalException(string internalMessage, int errorCode): this(null, internalMessage, errorCode, false) { 
        }

        internal InternalException(string internalMessage, int errorCode, bool showTextOnReport): this(null, internalMessage, errorCode, showTextOnReport) {
        } 

        internal InternalException(Exception innerException, string internalMessage, int errorCode, bool showErrorMesageOnReport) 
                : this(innerException, internalMessage, errorCode, showErrorMesageOnReport, true) { 
        }
 
        internal InternalException(Exception innerException, string internalMessage, int errorCode, bool showErrorMesageOnReport, bool needAssert)
                : base(SR.GetString(internalExceptionMessageID), innerException) {

            this.errorCode = errorCode; 
            this.showErrorMesageOnReport = showErrorMesageOnReport;
            if (needAssert) { 
                Debug.Fail(internalMessage); 
            }
        } 

        private InternalException(SerializationInfo info, StreamingContext context) : base(info, context){
            internalMessage = info.GetString("InternalMessage");
            errorCode = info.GetInt32("ErrorCode"); 
            showErrorMesageOnReport = info.GetBoolean("ShowErrorMesageOnReport");
        } 
 
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context){
            info.AddValue("InternalMessage", internalMessage); 
            info.AddValue("ErrorCode", errorCode);
            info.AddValue("ShowErrorMesageOnReport", showErrorMesageOnReport);

            base.GetObjectData(info, context); 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
/*
 



*/ 

namespace System.Data.Design { 
 
    using System;
 	using System.Design; 
	using System.Diagnostics;
	using System.Runtime.Serialization;

    [Serializable] 
    internal class InternalException : Exception, ISerializable {
        private const string internalExceptionMessageID = "ERR_INTERNAL"; 
 
        private string internalMessage = String.Empty;
        // showErrorMesageOnReport let you hide the sensitive error msg to the end user. 
        private bool   showErrorMesageOnReport;
        private int errorCode = -1;

        internal InternalException(string internalMessage) : this(internalMessage, null) {} 

        internal InternalException(string internalMessage, Exception innerException): this(innerException, internalMessage, -1, false) { 
        } 

        internal InternalException(string internalMessage, int errorCode): this(null, internalMessage, errorCode, false) { 
        }

        internal InternalException(string internalMessage, int errorCode, bool showTextOnReport): this(null, internalMessage, errorCode, showTextOnReport) {
        } 

        internal InternalException(Exception innerException, string internalMessage, int errorCode, bool showErrorMesageOnReport) 
                : this(innerException, internalMessage, errorCode, showErrorMesageOnReport, true) { 
        }
 
        internal InternalException(Exception innerException, string internalMessage, int errorCode, bool showErrorMesageOnReport, bool needAssert)
                : base(SR.GetString(internalExceptionMessageID), innerException) {

            this.errorCode = errorCode; 
            this.showErrorMesageOnReport = showErrorMesageOnReport;
            if (needAssert) { 
                Debug.Fail(internalMessage); 
            }
        } 

        private InternalException(SerializationInfo info, StreamingContext context) : base(info, context){
            internalMessage = info.GetString("InternalMessage");
            errorCode = info.GetInt32("ErrorCode"); 
            showErrorMesageOnReport = info.GetBoolean("ShowErrorMesageOnReport");
        } 
 
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context){
            info.AddValue("InternalMessage", internalMessage); 
            info.AddValue("ErrorCode", errorCode);
            info.AddValue("ShowErrorMesageOnReport", showErrorMesageOnReport);

            base.GetObjectData(info, context); 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
