//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.Collections;
    using System.Data;
    using System.Runtime.Serialization;
    using System.Security.Permissions; 

    internal sealed class DataSourceGeneratorException : Exception { 
        internal DataSourceGeneratorException(string message) : base(message) {} 
    }
 
    [Serializable]
    public class TypedDataSetGeneratorException : DataException {
        private ArrayList errorList;
        private string KEY_ARRAYCOUNT = "KEY_ARRAYCOUNT"; 
        private string KEY_ARRAYVALUES = "KEY_ARRAYVALUES";
 
        /// <include file='doc\StrongTypingException.uex' path='docs/doc[@for="TypedDataSetGeneratorException.TypedDataSetGeneratorException"]/*' /> 
        protected TypedDataSetGeneratorException(SerializationInfo info, StreamingContext context)
        : base(info, context) { 
            int count = (int) info.GetValue(KEY_ARRAYCOUNT, typeof(System.Int32));
            if (count > 0) {
                errorList = new ArrayList();
                for (int i = 0; i < count; i++) { 
                    errorList.Add(info.GetValue(KEY_ARRAYVALUES + i, typeof(System.String)));
                } 
            } 
            else
                errorList = null; 
        }

        /// <include file='doc\StrongTypingException.uex' path='docs/doc[@for="StrongTypingCodegenException.StrongTypingCodegenException"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public TypedDataSetGeneratorException() : base() { 
            errorList = null;
            HResult = HResults.StrongTyping; 
        }

        public TypedDataSetGeneratorException(string message)  : base(message) {
            HResult = HResults.StrongTyping; 
        }
 
        public TypedDataSetGeneratorException(string message, Exception innerException)  : base(message, innerException) { 
            HResult = HResults.StrongTyping;
        } 

        /// <include file='doc\StrongTypingException.uex' path='docs/doc[@for="StrongTypingCodegenException.StrongTypingCodegenException1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public TypedDataSetGeneratorException(IList list) : this() { 
            errorList = new ArrayList(list); 
            HResult = HResults.StrongTyping;
        } 

        /// <include file='doc\StrongTypingException.uex' path='docs/doc[@for="StrongTypingCodegenException.ErrorList"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public IList ErrorList { 
            get { 
                return (IList)errorList;
            } 
        }

        /// <include file='doc\StrongTypingException.uex' path='docs/doc[@for="TypedDataSetGeneratorException.GetObjectData"]/*' />
 		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)] 
		public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context); 
 
            if (errorList != null) {
                info.AddValue(KEY_ARRAYCOUNT, errorList.Count); 
                for (int i = 0; i < errorList.Count; i++) {
                    info.AddValue(KEY_ARRAYVALUES + i, errorList[i].ToString());
                }
            } 
            else {
                info.AddValue(KEY_ARRAYCOUNT, 0); 
            } 
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
 
namespace System.Data.Design { 

    using System; 
    using System.Collections;
    using System.Data;
    using System.Runtime.Serialization;
    using System.Security.Permissions; 

    internal sealed class DataSourceGeneratorException : Exception { 
        internal DataSourceGeneratorException(string message) : base(message) {} 
    }
 
    [Serializable]
    public class TypedDataSetGeneratorException : DataException {
        private ArrayList errorList;
        private string KEY_ARRAYCOUNT = "KEY_ARRAYCOUNT"; 
        private string KEY_ARRAYVALUES = "KEY_ARRAYVALUES";
 
        /// <include file='doc\StrongTypingException.uex' path='docs/doc[@for="TypedDataSetGeneratorException.TypedDataSetGeneratorException"]/*' /> 
        protected TypedDataSetGeneratorException(SerializationInfo info, StreamingContext context)
        : base(info, context) { 
            int count = (int) info.GetValue(KEY_ARRAYCOUNT, typeof(System.Int32));
            if (count > 0) {
                errorList = new ArrayList();
                for (int i = 0; i < count; i++) { 
                    errorList.Add(info.GetValue(KEY_ARRAYVALUES + i, typeof(System.String)));
                } 
            } 
            else
                errorList = null; 
        }

        /// <include file='doc\StrongTypingException.uex' path='docs/doc[@for="StrongTypingCodegenException.StrongTypingCodegenException"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public TypedDataSetGeneratorException() : base() { 
            errorList = null;
            HResult = HResults.StrongTyping; 
        }

        public TypedDataSetGeneratorException(string message)  : base(message) {
            HResult = HResults.StrongTyping; 
        }
 
        public TypedDataSetGeneratorException(string message, Exception innerException)  : base(message, innerException) { 
            HResult = HResults.StrongTyping;
        } 

        /// <include file='doc\StrongTypingException.uex' path='docs/doc[@for="StrongTypingCodegenException.StrongTypingCodegenException1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public TypedDataSetGeneratorException(IList list) : this() { 
            errorList = new ArrayList(list); 
            HResult = HResults.StrongTyping;
        } 

        /// <include file='doc\StrongTypingException.uex' path='docs/doc[@for="StrongTypingCodegenException.ErrorList"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public IList ErrorList { 
            get { 
                return (IList)errorList;
            } 
        }

        /// <include file='doc\StrongTypingException.uex' path='docs/doc[@for="TypedDataSetGeneratorException.GetObjectData"]/*' />
 		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)] 
		public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context); 
 
            if (errorList != null) {
                info.AddValue(KEY_ARRAYCOUNT, errorList.Count); 
                for (int i = 0; i < errorList.Count; i++) {
                    info.AddValue(KEY_ARRAYVALUES + i, errorList[i].ToString());
                }
            } 
            else {
                info.AddValue(KEY_ARRAYCOUNT, 0); 
            } 
        }
    } 

}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
