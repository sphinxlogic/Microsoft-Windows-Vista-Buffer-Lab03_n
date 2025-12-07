//------------------------------------------------------------------------------ 
// <copyright file="TypeCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized; 
    using System.Design;
    using System.Diagnostics; 
 

 
   /// <devdoc>
        ///    This structure is used by IntegrateStatements to put statements in the right place.
        /// </devdoc>
        internal class CodeMethodMap { 
            private CodeStatementCollection _container;
            private CodeStatementCollection _begin; 
            private CodeStatementCollection _end; 
            private CodeStatementCollection _statements;
            private CodeStatementCollection _locals; 
            private CodeStatementCollection _fields;
            private CodeStatementCollection _variables;
            private CodeStatementCollection _targetStatements;
            private CodeMemberMethod        _method; 

            internal CodeMethodMap(CodeMemberMethod method) : this(null, method){ 
            } 

            internal CodeMethodMap(CodeStatementCollection targetStatements, CodeMemberMethod method) { 
                _method = method;
                if (targetStatements != null) {
                    _targetStatements = targetStatements;
                } 
                else {
                    _targetStatements = _method.Statements; 
                } 
            }
 
            internal CodeStatementCollection BeginStatements {
                get {
                    if (_begin == null) _begin = new CodeStatementCollection();
                    return _begin; 
                }
            } 
 
            internal CodeStatementCollection EndStatements {
                get { 
                    if (_end == null) _end = new CodeStatementCollection();

                    return _end;
                } 
            }
 
            internal CodeStatementCollection ContainerStatements { 
                get {
                    if (_container == null) _container = new CodeStatementCollection(); 

                    return _container;
                }
            } 

            internal CodeMemberMethod Method { 
                get { 
                    return _method;
                } 
            }

            internal CodeStatementCollection Statements {
                get { 
                    if (_statements == null) _statements = new CodeStatementCollection();
 
                    return _statements; 
                }
            } 

            internal CodeStatementCollection LocalVariables {
                get {
                    if (_locals == null) _locals = new CodeStatementCollection(); 

                    return _locals; 
                } 
            }
 
            internal CodeStatementCollection FieldAssignments {
                get {
                    if (_fields == null) _fields = new CodeStatementCollection();
 
                    return _fields;
                } 
            } 

            // 
            internal CodeStatementCollection VariableAssignments {
                get {
                    if (_variables == null) _variables = new CodeStatementCollection();
 
                    return _variables;
                } 
            } 

            internal void Add(CodeStatementCollection statements) { 
                foreach (CodeStatement statement in statements) {

                    string isContainer = statement.UserData["IContainer"] as string;
                    if (isContainer != null && isContainer == "IContainer") { 
                        ContainerStatements.Add(statement);
                    } 
                    else if (statement is CodeAssignStatement && ((CodeAssignStatement)statement).Left is CodeFieldReferenceExpression) { 
                        FieldAssignments.Add(statement);
                    } 
                    else if (statement is CodeAssignStatement && ((CodeAssignStatement)statement).Left is CodeVariableReferenceExpression) {
                        VariableAssignments.Add(statement);
                    }
                    else if (statement is CodeVariableDeclarationStatement) { 
                        LocalVariables.Add(statement);
                    } 
                    else { 
                        string order = statement.UserData["statement-ordering"] as string;
 
                        if (order != null) {
                            switch (order) {
                                case "begin":
                                    BeginStatements.Add(statement); 
                                    break;
 
                                case "end": 
                                    EndStatements.Add(statement);
                                    break; 

                                case "default":
                                default:
                                    Statements.Add(statement); 
                                    break;
                            } 
                        } 
                        else {
                            Statements.Add(statement); 
                        }
                    }
                }
            } 

            internal void Combine() { 
                if (_container != null) _targetStatements.AddRange(_container); 
                if (_locals != null) _targetStatements.AddRange(_locals);
                if (_fields != null) _targetStatements.AddRange(_fields); 
                if (_variables != null) _targetStatements.AddRange(_variables);
                if (_begin != null) _targetStatements.AddRange(_begin);
                if (_statements != null) _targetStatements.AddRange(_statements);
                if (_end != null) _targetStatements.AddRange(_end); 
            }
        } 
 
 }

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TypeCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized; 
    using System.Design;
    using System.Diagnostics; 
 

 
   /// <devdoc>
        ///    This structure is used by IntegrateStatements to put statements in the right place.
        /// </devdoc>
        internal class CodeMethodMap { 
            private CodeStatementCollection _container;
            private CodeStatementCollection _begin; 
            private CodeStatementCollection _end; 
            private CodeStatementCollection _statements;
            private CodeStatementCollection _locals; 
            private CodeStatementCollection _fields;
            private CodeStatementCollection _variables;
            private CodeStatementCollection _targetStatements;
            private CodeMemberMethod        _method; 

            internal CodeMethodMap(CodeMemberMethod method) : this(null, method){ 
            } 

            internal CodeMethodMap(CodeStatementCollection targetStatements, CodeMemberMethod method) { 
                _method = method;
                if (targetStatements != null) {
                    _targetStatements = targetStatements;
                } 
                else {
                    _targetStatements = _method.Statements; 
                } 
            }
 
            internal CodeStatementCollection BeginStatements {
                get {
                    if (_begin == null) _begin = new CodeStatementCollection();
                    return _begin; 
                }
            } 
 
            internal CodeStatementCollection EndStatements {
                get { 
                    if (_end == null) _end = new CodeStatementCollection();

                    return _end;
                } 
            }
 
            internal CodeStatementCollection ContainerStatements { 
                get {
                    if (_container == null) _container = new CodeStatementCollection(); 

                    return _container;
                }
            } 

            internal CodeMemberMethod Method { 
                get { 
                    return _method;
                } 
            }

            internal CodeStatementCollection Statements {
                get { 
                    if (_statements == null) _statements = new CodeStatementCollection();
 
                    return _statements; 
                }
            } 

            internal CodeStatementCollection LocalVariables {
                get {
                    if (_locals == null) _locals = new CodeStatementCollection(); 

                    return _locals; 
                } 
            }
 
            internal CodeStatementCollection FieldAssignments {
                get {
                    if (_fields == null) _fields = new CodeStatementCollection();
 
                    return _fields;
                } 
            } 

            // 
            internal CodeStatementCollection VariableAssignments {
                get {
                    if (_variables == null) _variables = new CodeStatementCollection();
 
                    return _variables;
                } 
            } 

            internal void Add(CodeStatementCollection statements) { 
                foreach (CodeStatement statement in statements) {

                    string isContainer = statement.UserData["IContainer"] as string;
                    if (isContainer != null && isContainer == "IContainer") { 
                        ContainerStatements.Add(statement);
                    } 
                    else if (statement is CodeAssignStatement && ((CodeAssignStatement)statement).Left is CodeFieldReferenceExpression) { 
                        FieldAssignments.Add(statement);
                    } 
                    else if (statement is CodeAssignStatement && ((CodeAssignStatement)statement).Left is CodeVariableReferenceExpression) {
                        VariableAssignments.Add(statement);
                    }
                    else if (statement is CodeVariableDeclarationStatement) { 
                        LocalVariables.Add(statement);
                    } 
                    else { 
                        string order = statement.UserData["statement-ordering"] as string;
 
                        if (order != null) {
                            switch (order) {
                                case "begin":
                                    BeginStatements.Add(statement); 
                                    break;
 
                                case "end": 
                                    EndStatements.Add(statement);
                                    break; 

                                case "default":
                                default:
                                    Statements.Add(statement); 
                                    break;
                            } 
                        } 
                        else {
                            Statements.Add(statement); 
                        }
                    }
                }
            } 

            internal void Combine() { 
                if (_container != null) _targetStatements.AddRange(_container); 
                if (_locals != null) _targetStatements.AddRange(_locals);
                if (_fields != null) _targetStatements.AddRange(_fields); 
                if (_variables != null) _targetStatements.AddRange(_variables);
                if (_begin != null) _targetStatements.AddRange(_begin);
                if (_statements != null) _targetStatements.AddRange(_statements);
                if (_end != null) _targetStatements.AddRange(_end); 
            }
        } 
 
 }

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
