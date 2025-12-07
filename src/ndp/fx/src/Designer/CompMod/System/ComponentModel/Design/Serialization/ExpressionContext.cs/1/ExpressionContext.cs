//------------------------------------------------------------------------------ 
// <copyright file="ExpressionContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.ComponentModel.Design.Serialization;
    using System.Diagnostics;

    /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext"]/*' /> 
    /// <devdoc>
    ///    An expression context is an object that is placed on the context stack and contains 
    ///    the most relevant expression during serialization.  For example, take the following 
    ///    statement:
    /// 
    ///    button1.Text = "Hello";
    ///
    ///    During serialization several serializers will be responsible for creating this single
    ///    statement.  One of those serializers will be responsible for writing "Hello".  There 
    ///    are times when that serializer may need to know the context in which it is creating
    ///    its expression.  In the above example, this isn't needed, but take this slightly 
    ///    modified example: 
    ///
    ///    button1.Text = rm.GetString("button1_Text"); 
    ///
    ///    Here, the serializer responsible for writing the resource expression needs to
    ///    know the names of the target objects.  The ExpressionContext class can be used
    ///    for this.  As each serializer creates an expression and invokes a serializer 
    ///    to handle a smaller part of the statement as a whole, the serializer pushes
    ///    an expression context on the context stack.  Each expression context has 
    ///    a parent property that locates the next expression context on the stack, which 
    ///    provides a way for easy traversal.
    /// </devdoc> 
    public sealed class ExpressionContext {

        private CodeExpression _expression;
        private Type _expressionType; 
        private object _owner;
        private object _presetValue; 
 
        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.ExpressionContext"]/*' />
        /// <devdoc> 
        ///    Creates a new expression context.
        /// </devdoc>
        public ExpressionContext(CodeExpression expression, Type expressionType, object owner, object presetValue) {
            // To make this public, we cannot have random special cases for what the args mean. 
            Debug.Assert(expression != null && expressionType != null && owner != null, "Obsolete use of expression context.");
            if (expression == null) throw new ArgumentNullException("expression"); 
            if (expressionType == null) throw new ArgumentNullException("expressionType"); 
            if (owner == null) throw new ArgumentNullException("owner");
 
            _expression = expression;
            _expressionType = expressionType;
            _owner = owner;
            _presetValue = presetValue; 
        }
 
        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.ExpressionContext1"]/*' /> 
        /// <devdoc>
        ///    Creates a new expression context. 
        /// </devdoc>
        public ExpressionContext(CodeExpression expression, Type expressionType, object owner) : this(expression, expressionType, owner, null) {
        }
 
        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.Expression"]/*' />
        /// <devdoc> 
        ///    The expression this context represents. 
        /// </devdoc>
        public CodeExpression Expression { 
            get { return _expression; }
        }

        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.ExpressionType"]/*' /> 
        /// <devdoc>
        ///    The type of the expression.  This can be used to determine if a cast is needed when assigning 
        ///    to the expression. 
        /// </devdoc>
        public Type ExpressionType { 
            get { return _expressionType; }
        }

        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.Owner"]/*' /> 
        /// <devdoc>
        ///    The object owning this expression.  For example, if the expression was a property reference 
        ///    to button1's Text property, Owner would return button1. 
        /// </devdoc>
        public object Owner { 
            get { return _owner; }
        }

        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.PresetValue"]/*' /> 
        /// <devdoc>
        ///     Contains the preset value of an expression, should one exist.  For example, if the 
        ///     expression is a property reference expression referring to the Controls property of 
        ///     a button, PresetValue will contain the instance of Controls property because the property
        ///     is read-only and preset by the object to contain a value.  On the other hand, a property 
        ///     such as Text or Visible does not have a preset value and therefore the PresetValue property
        ///     will return null.
        ///
        ///     Serializers can use this information to guide serialization. For example, take the 
        ///     following two snippts of code:
        /// 
        ///     Padding p = new Padding(); 
        ///     p.Left = 5;
        ///     button1.Padding = p; 
        ///
        ///     button1.Padding.Left = 5;
        ///
        ///     The serializer of the Padding class needs to know if it should generate the 
        ///     first or second form.  The first form would be generated by default.  The
        ///     second form will only be generated if there is an ExpressionContext on the 
        ///     stack that contains a PresetValue equal to the value of the Padding object 
        ///     currently being serialized.
        /// </devdoc> 
        public object PresetValue {
            get { return _presetValue; }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ExpressionContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.ComponentModel.Design.Serialization;
    using System.Diagnostics;

    /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext"]/*' /> 
    /// <devdoc>
    ///    An expression context is an object that is placed on the context stack and contains 
    ///    the most relevant expression during serialization.  For example, take the following 
    ///    statement:
    /// 
    ///    button1.Text = "Hello";
    ///
    ///    During serialization several serializers will be responsible for creating this single
    ///    statement.  One of those serializers will be responsible for writing "Hello".  There 
    ///    are times when that serializer may need to know the context in which it is creating
    ///    its expression.  In the above example, this isn't needed, but take this slightly 
    ///    modified example: 
    ///
    ///    button1.Text = rm.GetString("button1_Text"); 
    ///
    ///    Here, the serializer responsible for writing the resource expression needs to
    ///    know the names of the target objects.  The ExpressionContext class can be used
    ///    for this.  As each serializer creates an expression and invokes a serializer 
    ///    to handle a smaller part of the statement as a whole, the serializer pushes
    ///    an expression context on the context stack.  Each expression context has 
    ///    a parent property that locates the next expression context on the stack, which 
    ///    provides a way for easy traversal.
    /// </devdoc> 
    public sealed class ExpressionContext {

        private CodeExpression _expression;
        private Type _expressionType; 
        private object _owner;
        private object _presetValue; 
 
        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.ExpressionContext"]/*' />
        /// <devdoc> 
        ///    Creates a new expression context.
        /// </devdoc>
        public ExpressionContext(CodeExpression expression, Type expressionType, object owner, object presetValue) {
            // To make this public, we cannot have random special cases for what the args mean. 
            Debug.Assert(expression != null && expressionType != null && owner != null, "Obsolete use of expression context.");
            if (expression == null) throw new ArgumentNullException("expression"); 
            if (expressionType == null) throw new ArgumentNullException("expressionType"); 
            if (owner == null) throw new ArgumentNullException("owner");
 
            _expression = expression;
            _expressionType = expressionType;
            _owner = owner;
            _presetValue = presetValue; 
        }
 
        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.ExpressionContext1"]/*' /> 
        /// <devdoc>
        ///    Creates a new expression context. 
        /// </devdoc>
        public ExpressionContext(CodeExpression expression, Type expressionType, object owner) : this(expression, expressionType, owner, null) {
        }
 
        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.Expression"]/*' />
        /// <devdoc> 
        ///    The expression this context represents. 
        /// </devdoc>
        public CodeExpression Expression { 
            get { return _expression; }
        }

        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.ExpressionType"]/*' /> 
        /// <devdoc>
        ///    The type of the expression.  This can be used to determine if a cast is needed when assigning 
        ///    to the expression. 
        /// </devdoc>
        public Type ExpressionType { 
            get { return _expressionType; }
        }

        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.Owner"]/*' /> 
        /// <devdoc>
        ///    The object owning this expression.  For example, if the expression was a property reference 
        ///    to button1's Text property, Owner would return button1. 
        /// </devdoc>
        public object Owner { 
            get { return _owner; }
        }

        /// <include file='doc\ExpressionContext.uex' path='docs/doc[@for="ExpressionContext.PresetValue"]/*' /> 
        /// <devdoc>
        ///     Contains the preset value of an expression, should one exist.  For example, if the 
        ///     expression is a property reference expression referring to the Controls property of 
        ///     a button, PresetValue will contain the instance of Controls property because the property
        ///     is read-only and preset by the object to contain a value.  On the other hand, a property 
        ///     such as Text or Visible does not have a preset value and therefore the PresetValue property
        ///     will return null.
        ///
        ///     Serializers can use this information to guide serialization. For example, take the 
        ///     following two snippts of code:
        /// 
        ///     Padding p = new Padding(); 
        ///     p.Left = 5;
        ///     button1.Padding = p; 
        ///
        ///     button1.Padding.Left = 5;
        ///
        ///     The serializer of the Padding class needs to know if it should generate the 
        ///     first or second form.  The first form would be generated by default.  The
        ///     second form will only be generated if there is an ExpressionContext on the 
        ///     stack that contains a PresetValue equal to the value of the Padding object 
        ///     currently being serialized.
        /// </devdoc> 
        public object PresetValue {
            get { return _presetValue; }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
