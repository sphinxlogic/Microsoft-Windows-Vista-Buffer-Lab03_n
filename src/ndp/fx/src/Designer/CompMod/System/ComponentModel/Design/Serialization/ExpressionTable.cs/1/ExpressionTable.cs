//------------------------------------------------------------------------------ 
// <copyright file="ExpressionTable.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.ComponentModel.Design.Serialization {
 
    using Microsoft.CSharp;
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler; 
    using System.Collections;
    using System.Diagnostics; 
    using System.IO; 

    /// <devdoc> 
    ///     An expression table allows a lookup from expression to object and object to
    ///     expression.  It is similar to the serialization manager's GetName and GetInstance
    ///     methods, only with rich code expressions.
    /// </devdoc> 
    internal sealed class ExpressionTable {
 
        private Hashtable _expressions; 

        private Hashtable Expressions { 
            get {
                if (_expressions == null) {
                    _expressions = new Hashtable(new ReferenceComparer());
                } 
                return _expressions;
            } 
        } 

        internal void SetExpression(object value, CodeExpression expression, bool isPreset) { 
            Expressions[value] = new ExpressionInfo(expression, isPreset);
        }

        internal CodeExpression GetExpression(object value) { 
            CodeExpression expression = null;
 
            ExpressionInfo info = Expressions[value] as ExpressionInfo; 
            if (info != null) {
                expression = info.Expression; 
            }

            return expression;
        } 

        internal bool ContainsPresetExpression(object value) { 
            ExpressionInfo info = Expressions[value] as ExpressionInfo; 
            if (info != null) {
                return info.IsPreset; 
            }
            else {
                return false;
            } 
        }
 
        private class ExpressionInfo { 
            CodeExpression _expression;
            bool _isPreset; 

            internal ExpressionInfo(CodeExpression expression, bool isPreset) {
                _expression = expression;
                _isPreset = isPreset; 
            }
 
            internal CodeExpression Expression { 
                get {
                    return _expression; 
                }
            }

            internal bool IsPreset { 
                get {
                    return _isPreset; 
                } 
            }
        } 

        private class ReferenceComparer : IEqualityComparer {
            bool IEqualityComparer.Equals(object x, object y) {
                return object.ReferenceEquals(x, y); 
            }
 
            int IEqualityComparer.GetHashCode(object x) { 
                if (x != null) {
                    return x.GetHashCode(); 
                }
                return 0;
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ExpressionTable.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.ComponentModel.Design.Serialization {
 
    using Microsoft.CSharp;
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler; 
    using System.Collections;
    using System.Diagnostics; 
    using System.IO; 

    /// <devdoc> 
    ///     An expression table allows a lookup from expression to object and object to
    ///     expression.  It is similar to the serialization manager's GetName and GetInstance
    ///     methods, only with rich code expressions.
    /// </devdoc> 
    internal sealed class ExpressionTable {
 
        private Hashtable _expressions; 

        private Hashtable Expressions { 
            get {
                if (_expressions == null) {
                    _expressions = new Hashtable(new ReferenceComparer());
                } 
                return _expressions;
            } 
        } 

        internal void SetExpression(object value, CodeExpression expression, bool isPreset) { 
            Expressions[value] = new ExpressionInfo(expression, isPreset);
        }

        internal CodeExpression GetExpression(object value) { 
            CodeExpression expression = null;
 
            ExpressionInfo info = Expressions[value] as ExpressionInfo; 
            if (info != null) {
                expression = info.Expression; 
            }

            return expression;
        } 

        internal bool ContainsPresetExpression(object value) { 
            ExpressionInfo info = Expressions[value] as ExpressionInfo; 
            if (info != null) {
                return info.IsPreset; 
            }
            else {
                return false;
            } 
        }
 
        private class ExpressionInfo { 
            CodeExpression _expression;
            bool _isPreset; 

            internal ExpressionInfo(CodeExpression expression, bool isPreset) {
                _expression = expression;
                _isPreset = isPreset; 
            }
 
            internal CodeExpression Expression { 
                get {
                    return _expression; 
                }
            }

            internal bool IsPreset { 
                get {
                    return _isPreset; 
                } 
            }
        } 

        private class ReferenceComparer : IEqualityComparer {
            bool IEqualityComparer.Equals(object x, object y) {
                return object.ReferenceEquals(x, y); 
            }
 
            int IEqualityComparer.GetHashCode(object x) { 
                if (x != null) {
                    return x.GetHashCode(); 
                }
                return 0;
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
