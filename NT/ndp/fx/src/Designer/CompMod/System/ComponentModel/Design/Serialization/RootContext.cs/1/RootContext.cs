//------------------------------------------------------------------------------ 
// <copyright file="RootContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.Collections;
    using System.Collections.Generic;

    /// <include file='doc\RootContext.uex' path='docs/doc[@for="RootContext"]/*' /> 
    /// <devdoc>
    ///    The root context is added by a type code dom serailizier to provide a definiton 
    ///    of the "root" object. 
    /// </devdoc>
    public sealed class RootContext { 

        private CodeExpression expression;
        private object value;
 
        /// <include file='doc\RootContext.uex' path='docs/doc[@for="RootContext.RootContext"]/*' />
        /// <devdoc> 
        ///    This object can be placed on the context stack to represent the object that is the root 
        ///    of the serialization hierarchy.  In addition to this instance, the RootContext also
        ///    contains an expression that can be used to reference the RootContext. 
        /// </devdoc>
        public RootContext(CodeExpression expression, object value) {
            if (expression == null) throw new ArgumentNullException("expression");
            if (value == null) throw new ArgumentNullException("value"); 

            this.expression = expression; 
            this.value = value; 
        }
 
        /// <include file='doc\RootContext.uex' path='docs/doc[@for="RootContext.Expression"]/*' />
        /// <devdoc>
        ///    The expression representing the root object in the object graph.
        /// </devdoc> 
        public CodeExpression Expression {
            get { 
                return expression; 
            }
        } 

        /// <include file='doc\RootContext.uex' path='docs/doc[@for="RootContext.Value"]/*' />
        /// <devdoc>
        ///    The root object of the object graph. 
        /// </devdoc>
        public object Value { 
            get { 
                return value;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="RootContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.Collections;
    using System.Collections.Generic;

    /// <include file='doc\RootContext.uex' path='docs/doc[@for="RootContext"]/*' /> 
    /// <devdoc>
    ///    The root context is added by a type code dom serailizier to provide a definiton 
    ///    of the "root" object. 
    /// </devdoc>
    public sealed class RootContext { 

        private CodeExpression expression;
        private object value;
 
        /// <include file='doc\RootContext.uex' path='docs/doc[@for="RootContext.RootContext"]/*' />
        /// <devdoc> 
        ///    This object can be placed on the context stack to represent the object that is the root 
        ///    of the serialization hierarchy.  In addition to this instance, the RootContext also
        ///    contains an expression that can be used to reference the RootContext. 
        /// </devdoc>
        public RootContext(CodeExpression expression, object value) {
            if (expression == null) throw new ArgumentNullException("expression");
            if (value == null) throw new ArgumentNullException("value"); 

            this.expression = expression; 
            this.value = value; 
        }
 
        /// <include file='doc\RootContext.uex' path='docs/doc[@for="RootContext.Expression"]/*' />
        /// <devdoc>
        ///    The expression representing the root object in the object graph.
        /// </devdoc> 
        public CodeExpression Expression {
            get { 
                return expression; 
            }
        } 

        /// <include file='doc\RootContext.uex' path='docs/doc[@for="RootContext.Value"]/*' />
        /// <devdoc>
        ///    The root object of the object graph. 
        /// </devdoc>
        public object Value { 
            get { 
                return value;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
