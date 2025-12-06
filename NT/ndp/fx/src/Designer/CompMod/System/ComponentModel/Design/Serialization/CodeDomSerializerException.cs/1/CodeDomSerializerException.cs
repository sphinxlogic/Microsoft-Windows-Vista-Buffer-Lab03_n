//------------------------------------------------------------------------------ 
// <copyright file="CodeDomSerializerException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.Runtime.Serialization;

    /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException"]/*' />
    /// <devdoc> 
    ///    <para> The exception that is thrown when the code dom serializer experiences an error.
    ///    </para> 
    /// </devdoc> 
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")] 
    public class CodeDomSerializerException : SystemException {

        private CodeLinePragma linePragma;
 
        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.CodeDomSerializerException"]/*' />
        /// <devdoc> 
        /// <para>Initializes a new instance of the CodeDomSerializerException class.</para> 
        /// </devdoc>
        public CodeDomSerializerException(string message, CodeLinePragma linePragma) : base(message) { 
            this.linePragma = linePragma;
        }

        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.CodeDomSerializerException1"]/*' /> 
        /// <devdoc>
        /// <para>Initializes a new instance of the CodeDomSerializerException class.</para> 
        /// </devdoc> 
        public CodeDomSerializerException(Exception ex, CodeLinePragma linePragma) : base(ex.Message, ex) {
            this.linePragma = linePragma; 
        }

        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.CodeDomSerializerException2"]/*' />
        /// <devdoc> 
        /// <para>Initializes a new instance of the CodeDomSerializerException class.</para>
        /// </devdoc> 
        public CodeDomSerializerException(string message, IDesignerSerializationManager manager) : base(message) { 
            FillLinePragmaFromContext(manager);
        } 

        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.CodeDomSerializerException3"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the CodeDomSerializerException class.</para> 
        /// </devdoc>
        public CodeDomSerializerException(Exception ex, IDesignerSerializationManager manager) : base(ex.Message, ex) { 
            FillLinePragmaFromContext(manager); 
        }
 
        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.CodeDomSerializerException2"]/*' />
        protected CodeDomSerializerException(SerializationInfo info, StreamingContext context) : base (info, context) {
            linePragma = (CodeLinePragma)info.GetValue("linePragma", typeof(CodeLinePragma));
        } 

        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.LinePragma"]/*' /> 
        /// <devdoc> 
        ///    <para>Gets the line pragma object that is related to this error.</para>
        /// </devdoc> 
        public CodeLinePragma LinePragma {
            get {
                return linePragma;
            } 
        }
 
        /// <devdoc> 
        ///    Sniffs around in the context looking for a code statement.  if it finds one, it will add the statement's
        ///    line # information to the exception. 
        /// </devdoc>
        private void FillLinePragmaFromContext(IDesignerSerializationManager manager) {
            if (manager == null) throw new ArgumentNullException("manager");
 
            CodeStatement statement = (CodeStatement)manager.Context[typeof(CodeStatement)];
            CodeLinePragma linePragma = null; 
 
            if (statement != null) {
                linePragma = statement.LinePragma; 
            }
        }

        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.GetObjectData"]/*' /> 
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info==null) { 
                throw new ArgumentNullException("info"); 
            }
            info.AddValue("linePragma", linePragma); 
            base.GetObjectData(info, context);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="CodeDomSerializerException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.Runtime.Serialization;

    /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException"]/*' />
    /// <devdoc> 
    ///    <para> The exception that is thrown when the code dom serializer experiences an error.
    ///    </para> 
    /// </devdoc> 
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")] 
    public class CodeDomSerializerException : SystemException {

        private CodeLinePragma linePragma;
 
        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.CodeDomSerializerException"]/*' />
        /// <devdoc> 
        /// <para>Initializes a new instance of the CodeDomSerializerException class.</para> 
        /// </devdoc>
        public CodeDomSerializerException(string message, CodeLinePragma linePragma) : base(message) { 
            this.linePragma = linePragma;
        }

        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.CodeDomSerializerException1"]/*' /> 
        /// <devdoc>
        /// <para>Initializes a new instance of the CodeDomSerializerException class.</para> 
        /// </devdoc> 
        public CodeDomSerializerException(Exception ex, CodeLinePragma linePragma) : base(ex.Message, ex) {
            this.linePragma = linePragma; 
        }

        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.CodeDomSerializerException2"]/*' />
        /// <devdoc> 
        /// <para>Initializes a new instance of the CodeDomSerializerException class.</para>
        /// </devdoc> 
        public CodeDomSerializerException(string message, IDesignerSerializationManager manager) : base(message) { 
            FillLinePragmaFromContext(manager);
        } 

        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.CodeDomSerializerException3"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the CodeDomSerializerException class.</para> 
        /// </devdoc>
        public CodeDomSerializerException(Exception ex, IDesignerSerializationManager manager) : base(ex.Message, ex) { 
            FillLinePragmaFromContext(manager); 
        }
 
        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.CodeDomSerializerException2"]/*' />
        protected CodeDomSerializerException(SerializationInfo info, StreamingContext context) : base (info, context) {
            linePragma = (CodeLinePragma)info.GetValue("linePragma", typeof(CodeLinePragma));
        } 

        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.LinePragma"]/*' /> 
        /// <devdoc> 
        ///    <para>Gets the line pragma object that is related to this error.</para>
        /// </devdoc> 
        public CodeLinePragma LinePragma {
            get {
                return linePragma;
            } 
        }
 
        /// <devdoc> 
        ///    Sniffs around in the context looking for a code statement.  if it finds one, it will add the statement's
        ///    line # information to the exception. 
        /// </devdoc>
        private void FillLinePragmaFromContext(IDesignerSerializationManager manager) {
            if (manager == null) throw new ArgumentNullException("manager");
 
            CodeStatement statement = (CodeStatement)manager.Context[typeof(CodeStatement)];
            CodeLinePragma linePragma = null; 
 
            if (statement != null) {
                linePragma = statement.LinePragma; 
            }
        }

        /// <include file='doc\CodeDomSerializerException.uex' path='docs/doc[@for="CodeDomSerializerException.GetObjectData"]/*' /> 
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info==null) { 
                throw new ArgumentNullException("info"); 
            }
            info.AddValue("linePragma", linePragma); 
            base.GetObjectData(info, context);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
