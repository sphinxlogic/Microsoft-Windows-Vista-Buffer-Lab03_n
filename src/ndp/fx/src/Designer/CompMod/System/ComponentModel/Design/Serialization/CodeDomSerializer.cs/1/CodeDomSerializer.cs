 
//------------------------------------------------------------------------------
// <copyright file="CodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.ComponentModel.Design.Serialization { 

    using System;
    using System.Design;
    using System.Resources; 
    using System.CodeDom;
    using System.Collections; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection;

    /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer"]/*' /> 
    /// <devdoc>
    ///     The is a base class that can be used to serialize an object graph to a series of 
    ///     CodeDom statements. 
    /// </devdoc>
    [DefaultSerializationProvider(typeof(CodeDomSerializationProvider))] 
    public class CodeDomSerializer : CodeDomSerializerBase {

        private static CodeDomSerializer _default;
        private static readonly Attribute[] _runTimeFilter = new Attribute[] { DesignOnlyAttribute.No }; 
        private static readonly Attribute[] _designTimeFilter = new Attribute[] { DesignOnlyAttribute.Yes };
        private static CodeThisReferenceExpression _thisRef = new CodeThisReferenceExpression(); 
 
        internal static CodeDomSerializer Default {
            get { 
                if (_default == null) {
                    _default = new CodeDomSerializer();
                }
 
                return _default;
            } 
        } 

        /// <devdoc> 
        ///     Determines which statement group the given statement should belong to.  The expression parameter
        ///     is an expression that the statement has been reduced to, and targetType represents the type
        ///     of this statement.  This method returns the name of the component this statement should be grouped
        ///     with. 
        /// </devdoc>
        public virtual string GetTargetComponentName(CodeStatement statement, CodeExpression expression, Type targetType) { 
            string name = null; 
            CodeVariableReferenceExpression variableReferenceEx;
            CodeFieldReferenceExpression fieldReferenceEx; 

            if ((variableReferenceEx = expression as CodeVariableReferenceExpression) != null) {
                name = variableReferenceEx.VariableName;
            } 
            else if ((fieldReferenceEx = expression as CodeFieldReferenceExpression) != null) {
                name = fieldReferenceEx.FieldName; 
            } 

            return name; 
        }

        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.Deserialize"]/*' />
        /// <devdoc> 
        ///     Deserilizes the given CodeDom object into a real object.  This
        ///     will use the serialization manager to create objects and resolve 
        ///     data types.  The root of the object graph is returned. 
        /// </devdoc>
        public virtual object Deserialize(IDesignerSerializationManager manager, object codeObject) { 
            object instance = null;

            if (manager == null || codeObject == null) {
                throw new ArgumentNullException(manager == null ? "manager" : "codeObject"); 
            }
 
            using (TraceScope("CodeDomSerializer::Deserialize")) { 
                // What is the code object?  We support an expression, a statement or a collection of statements
                CodeExpression expression = codeObject as CodeExpression; 

                if (expression != null) {
                    instance = DeserializeExpression(manager, null, expression);
                } 
                else {
                    CodeStatementCollection statements = codeObject as CodeStatementCollection; 
 
                    if (statements != null) {
                        foreach (CodeStatement element in statements) { 
                            // If we do not yet have an instance, we will need to pick through the
                            // statements and see if we can find one.
                            if (instance == null) {
                                instance = DeserializeStatementToInstance(manager, element); 
                            }
                            else { 
                                DeserializeStatement(manager, element); 
                            }
                        } 
                    }
                    else {
                        CodeStatement statement = codeObject as CodeStatement;
 
                        if (statement == null) {
                            Debug.Fail("CodeDomSerializer::Deserialize requires a CodeExpression, CodeStatement or CodeStatementCollection to parse"); 
 
                            string supportedTypes = string.Format(CultureInfo.CurrentCulture, "{0}, {1}, {2}", typeof(CodeExpression).Name, typeof(CodeStatement).Name, typeof(CodeStatementCollection).Name);
 
                            throw new ArgumentException(SR.GetString(SR.SerializerBadElementTypes, codeObject.GetType().Name, supportedTypes));
                        }
                    }
                } 
            }
 
            return instance; 
        }
 
        /// <devdoc>
        ///    This method deserializes a single statement.  It is equivalent of calling
        ///    DeserializeStatement, except that it returns an object instance if the
        ///    resulting statement was a variable assign statement, a variable 
        ///    declaration with an init expression, or a field assign statement.
        /// </devdoc> 
        protected object DeserializeStatementToInstance(IDesignerSerializationManager manager, CodeStatement statement) { 
            object instance = null;
            CodeAssignStatement assign; 
            CodeVariableDeclarationStatement varDecl;


            if ((assign = statement as CodeAssignStatement) != null) { 
                // CodeAssignStatement
                CodeFieldReferenceExpression fieldRef = assign.Left as CodeFieldReferenceExpression; 
 
                if (fieldRef != null) {
                    Trace("Assigning instance to field {0}", fieldRef.FieldName); 
                    instance = DeserializeExpression(manager, fieldRef.FieldName, assign.Right);
                }
                else {
                    CodeVariableReferenceExpression varRef = assign.Left as CodeVariableReferenceExpression; 

                    if (varRef != null) { 
                        Trace("Assigning instance to variable {0}", varRef.VariableName); 
                        instance = DeserializeExpression(manager, varRef.VariableName, assign.Right);
                    } 
                    else {
                        DeserializeStatement(manager, assign);
                    }
                } 
            }
            else if ((varDecl = statement as CodeVariableDeclarationStatement) != null && varDecl.InitExpression != null) { 
                // CodeVariableDeclarationStatement 
                Trace("Initializing variable declaration for variable {0}", varDecl.Name);
                instance = DeserializeExpression(manager, varDecl.Name, varDecl.InitExpression); 
            }
            else {
                // This statement isn't one that will return a named object.  Deserialize
                // it normally. 
                DeserializeStatement(manager, statement);
            } 
 
            return instance;
        } 

        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.Serialize"]/*' />
        /// <devdoc>
        ///     Serializes the given object into a CodeDom object. 
        /// </devdoc>
        public virtual object Serialize(IDesignerSerializationManager manager, object value) { 
            object result = null; 

            if (manager == null || value == null) { 
                throw new ArgumentNullException(manager == null ? "manager" : "value");
            }

            using (TraceScope("CodeDomSerializer::Serialize")) { 
                Trace("Type: {0}", value.GetType().Name);
 
                if (value is Type) { 
                    result = new CodeTypeOfExpression((Type)value);
                } 
                else {
                    bool isComplete = false;
                    bool isCompleteExpression;
                    bool isPreset; 
                    CodeExpression expression = SerializeCreationExpression(manager, value, out isCompleteExpression);
 
                    // if the object is not a component we will honor the return value 
                    // from SerializeCreationExpression.  For compat reasons we ignore
                    // the value if the object is a component. 
                    if (!(value is IComponent)) {
                        isComplete = isCompleteExpression;
                    }
 
                    // We need to find out if SerializeCreationExpression returned a preset expression.
                    ExpressionContext cxt = manager.Context[typeof(ExpressionContext)] as ExpressionContext; 
                    if (cxt != null && object.ReferenceEquals(cxt.PresetValue, value)) { 
                        isPreset = true;
                    } 
                    else {
                        isPreset = false;
                    }
 
                    TraceIf(expression == null, "Unable to create object; aborting.");
                    // Short circuit common cases 
                    if (expression != null) { 
                        if (isComplete) {
                            Trace("Single expression : {0}", expression); 
                            result = expression;
                        }
                        else {
                            // Ok, we have an incomplete expression. That means we've created the object but we will 
                            // need to set properties on it to configure it.  Therefore, we need to have a variable
                            // reference to it unless we were given a preset expression already. 
 
                            CodeStatementCollection statements = new CodeStatementCollection();
 
                            if (isPreset) {
                                SetExpression(manager, value, expression, true);
                            }
                            else { 
                                CodeExpression variableReference;
                                string varName = GetUniqueName(manager, value); 
                                string varTypeName = TypeDescriptor.GetClassName(value); 

                                CodeVariableDeclarationStatement varDecl = new CodeVariableDeclarationStatement(varTypeName, varName); 

                                Trace("Generating local : {0}", varName);
                                varDecl.InitExpression = expression;
                                statements.Add(varDecl); 
                                variableReference = new CodeVariableReferenceExpression(varName);
                                SetExpression(manager, value, variableReference); 
                            } 

                            // Finally, we need to walk properties and events for this object 
                            SerializePropertiesToResources(manager, statements, value, _designTimeFilter);
                            SerializeProperties(manager, statements, value, _runTimeFilter);
                            SerializeEvents(manager, statements, value, _runTimeFilter);
                            result = statements; 
                        }
                    } 
 
                }
            } 

            return result;
        }
 
        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.SerializeAbsolute"]/*' />
        /// <devdoc> 
        ///     Serializes the given object into a CodeDom object. 
        /// </devdoc>
        public virtual object SerializeAbsolute(IDesignerSerializationManager manager, object value) { 

            object data;
            SerializeAbsoluteContext abs = new SerializeAbsoluteContext();
            manager.Context.Push(abs); 

            try { 
                data = Serialize(manager, value); 
            }
            finally { 
                Debug.Assert(manager.Context.Current == abs, "Serializer added a context it didn't remove.");
                manager.Context.Pop();
            }
 
            return data;
        } 
 
        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.SerializeMember"]/*' />
        /// <devdoc> 
        ///     This serializes the given member on the given object.
        /// </devdoc>
        public virtual CodeStatementCollection SerializeMember(IDesignerSerializationManager manager, object owningObject, MemberDescriptor member) {
 
            if (manager == null)        throw new ArgumentNullException("manager");
            if (owningObject == null)   throw new ArgumentNullException("owningObject"); 
            if (member == null)         throw new ArgumentNullException("member"); 

            CodeStatementCollection statements = new CodeStatementCollection(); 

            // See if we have an existing expression for this member.  If not, fabricate one
            //
            CodeExpression expression = GetExpression(manager, owningObject); 

            if (expression == null) { 
                string name = GetUniqueName(manager, owningObject); 
                expression = new CodeVariableReferenceExpression(name);
                SetExpression(manager, owningObject, expression); 
            }

            PropertyDescriptor property = member as PropertyDescriptor;
            if (property != null) { 
                SerializeProperty(manager, statements, owningObject, property);
            } 
            else { 
                EventDescriptor evt = member as EventDescriptor;
                if (evt != null) { 
                    SerializeEvent(manager, statements, owningObject, evt);
                }
                else {
                    throw new NotSupportedException(SR.GetString(SR.SerializerMemberTypeNotSerializable, member.GetType().FullName)); 
                }
            } 
 
            return statements;
        } 

        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.SerializeMemberDifference"]/*' />
        /// <devdoc>
        ///     This serializes the given member on the given object. 
        /// </devdoc>
        public virtual CodeStatementCollection SerializeMemberAbsolute(IDesignerSerializationManager manager, object owningObject, MemberDescriptor member) { 
 
            if (manager == null)      throw new ArgumentNullException("manager");
            if (owningObject == null) throw new ArgumentNullException("owningObject"); 
            if (member == null)       throw new ArgumentNullException("member");

            CodeStatementCollection statements;
            SerializeAbsoluteContext abs = new SerializeAbsoluteContext(member); 
            manager.Context.Push(abs);
 
            try { 
                statements = SerializeMember(manager, owningObject, member);
            } 
            finally {
                Debug.Assert(manager.Context.Current == abs, "Serializer added a context it didn't remove.");
                manager.Context.Pop();
            } 

            return statements; 
        } 

        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.SerializeToReferenceExpression"]/*' /> 
        /// <devdoc>
        ///     This serializes the given value to an expression.  It will return null if the value could not be
        ///     serialized.  This is similar to SerializeToExpression, except that it will stop
        ///     if it cannot obtain a simple reference expression for the value.  Call this method 
        ///     when you expect the resulting expression to be used as a parameter or target
        ///     of a statement. 
        /// </devdoc> 
        [Obsolete("This method has been deprecated. Use SerializeToExpression or GetExpression instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        protected CodeExpression SerializeToReferenceExpression(IDesignerSerializationManager manager, object value) { 
            CodeExpression expression = null;

            using (TraceScope("CodeDomSerializer::SerializeToReferenceExpression")) {
                // First - try GetExpression 

                expression = GetExpression(manager, value); 
 
                //      Next, we check for a named IComponent, and return a reference to it.
                if (expression == null && value is IComponent) { 
                    string name = manager.GetName(value);
                    bool referenceName = false;

                    if (name == null) { 
                        IReferenceService referenceService = (IReferenceService)manager.GetService(typeof(IReferenceService));
 
                        if (referenceService != null) { 
                            name = referenceService.GetName(value);
                            referenceName = name != null; 
                        }
                    }

                    if (name != null) { 
                        Trace("Object is reference ({0}) Creating reference expression", name);
 
                        // Check to see if this is a reference to the root component.  If it is, then use "this". 
                        //
                        RootContext root = (RootContext)manager.Context[typeof(RootContext)]; 

                        if (root != null && root.Value == value) {
                            expression = root.Expression;
                        } 
                        else if (referenceName && name.IndexOf('.') != -1) {
                            // if it's a reference name with a dot, we've actually got a property here... 
                            // 
                            int dotIndex = name.IndexOf('.');
 
                            expression = new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(_thisRef, name.Substring(0, dotIndex)), name.Substring(dotIndex + 1));
                        }
                        else {
                            expression = new CodeFieldReferenceExpression(_thisRef, name); 
                        }
                    } 
                } 
            }
 
            return expression;
        }
    }
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="CodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.ComponentModel.Design.Serialization { 

    using System;
    using System.Design;
    using System.Resources; 
    using System.CodeDom;
    using System.Collections; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection;

    /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer"]/*' /> 
    /// <devdoc>
    ///     The is a base class that can be used to serialize an object graph to a series of 
    ///     CodeDom statements. 
    /// </devdoc>
    [DefaultSerializationProvider(typeof(CodeDomSerializationProvider))] 
    public class CodeDomSerializer : CodeDomSerializerBase {

        private static CodeDomSerializer _default;
        private static readonly Attribute[] _runTimeFilter = new Attribute[] { DesignOnlyAttribute.No }; 
        private static readonly Attribute[] _designTimeFilter = new Attribute[] { DesignOnlyAttribute.Yes };
        private static CodeThisReferenceExpression _thisRef = new CodeThisReferenceExpression(); 
 
        internal static CodeDomSerializer Default {
            get { 
                if (_default == null) {
                    _default = new CodeDomSerializer();
                }
 
                return _default;
            } 
        } 

        /// <devdoc> 
        ///     Determines which statement group the given statement should belong to.  The expression parameter
        ///     is an expression that the statement has been reduced to, and targetType represents the type
        ///     of this statement.  This method returns the name of the component this statement should be grouped
        ///     with. 
        /// </devdoc>
        public virtual string GetTargetComponentName(CodeStatement statement, CodeExpression expression, Type targetType) { 
            string name = null; 
            CodeVariableReferenceExpression variableReferenceEx;
            CodeFieldReferenceExpression fieldReferenceEx; 

            if ((variableReferenceEx = expression as CodeVariableReferenceExpression) != null) {
                name = variableReferenceEx.VariableName;
            } 
            else if ((fieldReferenceEx = expression as CodeFieldReferenceExpression) != null) {
                name = fieldReferenceEx.FieldName; 
            } 

            return name; 
        }

        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.Deserialize"]/*' />
        /// <devdoc> 
        ///     Deserilizes the given CodeDom object into a real object.  This
        ///     will use the serialization manager to create objects and resolve 
        ///     data types.  The root of the object graph is returned. 
        /// </devdoc>
        public virtual object Deserialize(IDesignerSerializationManager manager, object codeObject) { 
            object instance = null;

            if (manager == null || codeObject == null) {
                throw new ArgumentNullException(manager == null ? "manager" : "codeObject"); 
            }
 
            using (TraceScope("CodeDomSerializer::Deserialize")) { 
                // What is the code object?  We support an expression, a statement or a collection of statements
                CodeExpression expression = codeObject as CodeExpression; 

                if (expression != null) {
                    instance = DeserializeExpression(manager, null, expression);
                } 
                else {
                    CodeStatementCollection statements = codeObject as CodeStatementCollection; 
 
                    if (statements != null) {
                        foreach (CodeStatement element in statements) { 
                            // If we do not yet have an instance, we will need to pick through the
                            // statements and see if we can find one.
                            if (instance == null) {
                                instance = DeserializeStatementToInstance(manager, element); 
                            }
                            else { 
                                DeserializeStatement(manager, element); 
                            }
                        } 
                    }
                    else {
                        CodeStatement statement = codeObject as CodeStatement;
 
                        if (statement == null) {
                            Debug.Fail("CodeDomSerializer::Deserialize requires a CodeExpression, CodeStatement or CodeStatementCollection to parse"); 
 
                            string supportedTypes = string.Format(CultureInfo.CurrentCulture, "{0}, {1}, {2}", typeof(CodeExpression).Name, typeof(CodeStatement).Name, typeof(CodeStatementCollection).Name);
 
                            throw new ArgumentException(SR.GetString(SR.SerializerBadElementTypes, codeObject.GetType().Name, supportedTypes));
                        }
                    }
                } 
            }
 
            return instance; 
        }
 
        /// <devdoc>
        ///    This method deserializes a single statement.  It is equivalent of calling
        ///    DeserializeStatement, except that it returns an object instance if the
        ///    resulting statement was a variable assign statement, a variable 
        ///    declaration with an init expression, or a field assign statement.
        /// </devdoc> 
        protected object DeserializeStatementToInstance(IDesignerSerializationManager manager, CodeStatement statement) { 
            object instance = null;
            CodeAssignStatement assign; 
            CodeVariableDeclarationStatement varDecl;


            if ((assign = statement as CodeAssignStatement) != null) { 
                // CodeAssignStatement
                CodeFieldReferenceExpression fieldRef = assign.Left as CodeFieldReferenceExpression; 
 
                if (fieldRef != null) {
                    Trace("Assigning instance to field {0}", fieldRef.FieldName); 
                    instance = DeserializeExpression(manager, fieldRef.FieldName, assign.Right);
                }
                else {
                    CodeVariableReferenceExpression varRef = assign.Left as CodeVariableReferenceExpression; 

                    if (varRef != null) { 
                        Trace("Assigning instance to variable {0}", varRef.VariableName); 
                        instance = DeserializeExpression(manager, varRef.VariableName, assign.Right);
                    } 
                    else {
                        DeserializeStatement(manager, assign);
                    }
                } 
            }
            else if ((varDecl = statement as CodeVariableDeclarationStatement) != null && varDecl.InitExpression != null) { 
                // CodeVariableDeclarationStatement 
                Trace("Initializing variable declaration for variable {0}", varDecl.Name);
                instance = DeserializeExpression(manager, varDecl.Name, varDecl.InitExpression); 
            }
            else {
                // This statement isn't one that will return a named object.  Deserialize
                // it normally. 
                DeserializeStatement(manager, statement);
            } 
 
            return instance;
        } 

        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.Serialize"]/*' />
        /// <devdoc>
        ///     Serializes the given object into a CodeDom object. 
        /// </devdoc>
        public virtual object Serialize(IDesignerSerializationManager manager, object value) { 
            object result = null; 

            if (manager == null || value == null) { 
                throw new ArgumentNullException(manager == null ? "manager" : "value");
            }

            using (TraceScope("CodeDomSerializer::Serialize")) { 
                Trace("Type: {0}", value.GetType().Name);
 
                if (value is Type) { 
                    result = new CodeTypeOfExpression((Type)value);
                } 
                else {
                    bool isComplete = false;
                    bool isCompleteExpression;
                    bool isPreset; 
                    CodeExpression expression = SerializeCreationExpression(manager, value, out isCompleteExpression);
 
                    // if the object is not a component we will honor the return value 
                    // from SerializeCreationExpression.  For compat reasons we ignore
                    // the value if the object is a component. 
                    if (!(value is IComponent)) {
                        isComplete = isCompleteExpression;
                    }
 
                    // We need to find out if SerializeCreationExpression returned a preset expression.
                    ExpressionContext cxt = manager.Context[typeof(ExpressionContext)] as ExpressionContext; 
                    if (cxt != null && object.ReferenceEquals(cxt.PresetValue, value)) { 
                        isPreset = true;
                    } 
                    else {
                        isPreset = false;
                    }
 
                    TraceIf(expression == null, "Unable to create object; aborting.");
                    // Short circuit common cases 
                    if (expression != null) { 
                        if (isComplete) {
                            Trace("Single expression : {0}", expression); 
                            result = expression;
                        }
                        else {
                            // Ok, we have an incomplete expression. That means we've created the object but we will 
                            // need to set properties on it to configure it.  Therefore, we need to have a variable
                            // reference to it unless we were given a preset expression already. 
 
                            CodeStatementCollection statements = new CodeStatementCollection();
 
                            if (isPreset) {
                                SetExpression(manager, value, expression, true);
                            }
                            else { 
                                CodeExpression variableReference;
                                string varName = GetUniqueName(manager, value); 
                                string varTypeName = TypeDescriptor.GetClassName(value); 

                                CodeVariableDeclarationStatement varDecl = new CodeVariableDeclarationStatement(varTypeName, varName); 

                                Trace("Generating local : {0}", varName);
                                varDecl.InitExpression = expression;
                                statements.Add(varDecl); 
                                variableReference = new CodeVariableReferenceExpression(varName);
                                SetExpression(manager, value, variableReference); 
                            } 

                            // Finally, we need to walk properties and events for this object 
                            SerializePropertiesToResources(manager, statements, value, _designTimeFilter);
                            SerializeProperties(manager, statements, value, _runTimeFilter);
                            SerializeEvents(manager, statements, value, _runTimeFilter);
                            result = statements; 
                        }
                    } 
 
                }
            } 

            return result;
        }
 
        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.SerializeAbsolute"]/*' />
        /// <devdoc> 
        ///     Serializes the given object into a CodeDom object. 
        /// </devdoc>
        public virtual object SerializeAbsolute(IDesignerSerializationManager manager, object value) { 

            object data;
            SerializeAbsoluteContext abs = new SerializeAbsoluteContext();
            manager.Context.Push(abs); 

            try { 
                data = Serialize(manager, value); 
            }
            finally { 
                Debug.Assert(manager.Context.Current == abs, "Serializer added a context it didn't remove.");
                manager.Context.Pop();
            }
 
            return data;
        } 
 
        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.SerializeMember"]/*' />
        /// <devdoc> 
        ///     This serializes the given member on the given object.
        /// </devdoc>
        public virtual CodeStatementCollection SerializeMember(IDesignerSerializationManager manager, object owningObject, MemberDescriptor member) {
 
            if (manager == null)        throw new ArgumentNullException("manager");
            if (owningObject == null)   throw new ArgumentNullException("owningObject"); 
            if (member == null)         throw new ArgumentNullException("member"); 

            CodeStatementCollection statements = new CodeStatementCollection(); 

            // See if we have an existing expression for this member.  If not, fabricate one
            //
            CodeExpression expression = GetExpression(manager, owningObject); 

            if (expression == null) { 
                string name = GetUniqueName(manager, owningObject); 
                expression = new CodeVariableReferenceExpression(name);
                SetExpression(manager, owningObject, expression); 
            }

            PropertyDescriptor property = member as PropertyDescriptor;
            if (property != null) { 
                SerializeProperty(manager, statements, owningObject, property);
            } 
            else { 
                EventDescriptor evt = member as EventDescriptor;
                if (evt != null) { 
                    SerializeEvent(manager, statements, owningObject, evt);
                }
                else {
                    throw new NotSupportedException(SR.GetString(SR.SerializerMemberTypeNotSerializable, member.GetType().FullName)); 
                }
            } 
 
            return statements;
        } 

        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.SerializeMemberDifference"]/*' />
        /// <devdoc>
        ///     This serializes the given member on the given object. 
        /// </devdoc>
        public virtual CodeStatementCollection SerializeMemberAbsolute(IDesignerSerializationManager manager, object owningObject, MemberDescriptor member) { 
 
            if (manager == null)      throw new ArgumentNullException("manager");
            if (owningObject == null) throw new ArgumentNullException("owningObject"); 
            if (member == null)       throw new ArgumentNullException("member");

            CodeStatementCollection statements;
            SerializeAbsoluteContext abs = new SerializeAbsoluteContext(member); 
            manager.Context.Push(abs);
 
            try { 
                statements = SerializeMember(manager, owningObject, member);
            } 
            finally {
                Debug.Assert(manager.Context.Current == abs, "Serializer added a context it didn't remove.");
                manager.Context.Pop();
            } 

            return statements; 
        } 

        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.SerializeToReferenceExpression"]/*' /> 
        /// <devdoc>
        ///     This serializes the given value to an expression.  It will return null if the value could not be
        ///     serialized.  This is similar to SerializeToExpression, except that it will stop
        ///     if it cannot obtain a simple reference expression for the value.  Call this method 
        ///     when you expect the resulting expression to be used as a parameter or target
        ///     of a statement. 
        /// </devdoc> 
        [Obsolete("This method has been deprecated. Use SerializeToExpression or GetExpression instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        protected CodeExpression SerializeToReferenceExpression(IDesignerSerializationManager manager, object value) { 
            CodeExpression expression = null;

            using (TraceScope("CodeDomSerializer::SerializeToReferenceExpression")) {
                // First - try GetExpression 

                expression = GetExpression(manager, value); 
 
                //      Next, we check for a named IComponent, and return a reference to it.
                if (expression == null && value is IComponent) { 
                    string name = manager.GetName(value);
                    bool referenceName = false;

                    if (name == null) { 
                        IReferenceService referenceService = (IReferenceService)manager.GetService(typeof(IReferenceService));
 
                        if (referenceService != null) { 
                            name = referenceService.GetName(value);
                            referenceName = name != null; 
                        }
                    }

                    if (name != null) { 
                        Trace("Object is reference ({0}) Creating reference expression", name);
 
                        // Check to see if this is a reference to the root component.  If it is, then use "this". 
                        //
                        RootContext root = (RootContext)manager.Context[typeof(RootContext)]; 

                        if (root != null && root.Value == value) {
                            expression = root.Expression;
                        } 
                        else if (referenceName && name.IndexOf('.') != -1) {
                            // if it's a reference name with a dot, we've actually got a property here... 
                            // 
                            int dotIndex = name.IndexOf('.');
 
                            expression = new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(_thisRef, name.Substring(0, dotIndex)), name.Substring(dotIndex + 1));
                        }
                        else {
                            expression = new CodeFieldReferenceExpression(_thisRef, name); 
                        }
                    } 
                } 
            }
 
            return expression;
        }
    }
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
