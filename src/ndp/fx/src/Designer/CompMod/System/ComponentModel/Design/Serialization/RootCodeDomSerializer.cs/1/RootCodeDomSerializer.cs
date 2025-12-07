 
//------------------------------------------------------------------------------
// <copyright file="RootCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.ComponentModel.Design.Serialization { 

    using System.Design;
    using System;
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Collections; 
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Resources;
    using System.Runtime.Serialization;
    using System.Globalization; 

 
    /// ******************************************************************************************* 
    /// NOTE:
    /// 
    /// This class is NOT used for Whidbey.  It has been replaced by TypeCodeDomSerializer as root
    /// serializers are no longer in vogue.  It is still here because we need to be compatible with
    /// existing code bases that use root serialization.  Don't remove it, but be careful when fixing
    /// bugs because you might not be changing the right bits. 
    ///
    /// ******************************************************************************************* 
    /// 
    /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer"]/*' />
    /// <devdoc> 
    ///     This is our root serialization object.  It is responsible for organizing all of the
    ///     serialization for subsequent objects.  This inherits from ComponentCodeDomSerializer
    ///     in order to share useful methods.
    /// </devdoc> 
    internal sealed class RootCodeDomSerializer : ComponentCodeDomSerializer {
 
        // Used only during deserialization to provide name to object mapping. 
        //
        private IDictionary      nameTable; 
        private IDictionary      statementTable;
        private CodeMemberMethod initMethod;
        private bool             containerRequired;
 
        private static readonly Attribute[] designTimeProperties = new Attribute[] { DesignOnlyAttribute.Yes};
        private static readonly Attribute[] runTimeProperties = new Attribute[] { DesignOnlyAttribute.No}; 
 
        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.ContainerName"]/*' />
        /// <devdoc> 
        ///     The name of the IContainer we will use for components that require a container.
        ///     Note that compnent model serializer also has this property.
        /// </devdoc>
        public string ContainerName { 
            get {
                return "components"; 
            } 
        }
 
        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.ContainerRequired"]/*' />
        /// <devdoc>
        ///     The component serializer will set this to true if it emitted a compnent declaration that required
        ///     a container. 
        /// </devdoc>
        public bool ContainerRequired { 
            get { 
                return containerRequired;
            } 
            set {
                containerRequired = value;
            }
        } 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.InitMethodName"]/*' /> 
        /// <devdoc> 
        ///     The name of the method we will serialize into.  We always use this, so if there
        ///     is a need to change it down the road, we can make it virtual. 
        /// </devdoc>
        public string InitMethodName {
            get {
                return "InitializeComponent"; 
            }
        } 
 
        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.AddStatement"]/*' />
        /// <devdoc> 
        ///     Unility method that adds the given statement to our statementTable dictionary under the
        ///     given name.
        /// </devdoc>
        private void AddStatement(string name, CodeStatement statement) { 
            OrderedCodeStatementCollection statements = (OrderedCodeStatementCollection)statementTable[name];
 
            if (statements == null) { 
                statements = new OrderedCodeStatementCollection();
 
                // push in an order key so we know what position this item was in the list of declarations.
                // this allows us to preserve ZOrder.
                //
                statements.Order = statementTable.Count; 
                statements.Name = name;
                statementTable[name] = statements; 
            } 

            statements.Add(statement); 
        }

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.Deserialize"]/*' />
        /// <devdoc> 
        ///     Deserilizes the given CodeDom element into a real object.  This
        ///     will use the serialization manager to create objects and resolve 
        ///     data types.  The root of the object graph is returned. 
        /// </devdoc>
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject) { 

            if (manager == null || codeObject == null) {
                throw new ArgumentNullException( manager == null ? "manager" : "codeObject");
            } 

            object documentObject = null; 
 
            using (TraceScope("RootCodeDomSerializer::Deserialize")) {
                if (!(codeObject is CodeTypeDeclaration)) { 
                    Debug.Fail("RootCodeDomSerializer::Deserialize requires a CodeTypeDeclaration to parse");
                    throw new ArgumentException(SR.GetString(SR.SerializerBadElementType, typeof(CodeTypeDeclaration).FullName));
                }
 
                // Determine case-sensitivity
                // 
                bool caseInsensitive = false; 
                CodeDomProvider provider = manager.GetService(typeof(CodeDomProvider)) as CodeDomProvider;
 
                TraceWarningIf(provider == null, "CodeDomProvider is not a service provided by the serialization manager.  We use this to determine case sensitivity.  Assuming language is not case sensitive.");
                if (provider != null) {
                    caseInsensitive = ((provider.LanguageOptions & LanguageOptions.CaseInsensitive) != 0);
                } 

                // Get and initialize the document type. 
                // 
                CodeTypeDeclaration docType = (CodeTypeDeclaration)codeObject;
                CodeTypeReference baseType = null; 
                Type type = null;

                foreach (CodeTypeReference typeRef in docType.BaseTypes) {
                    Type t = manager.GetType(GetTypeNameFromCodeTypeReference(manager, typeRef)); 

                    if (t != null && !(t.IsInterface)) { 
                        baseType = typeRef; 
                        type = t;
                        break; 
                    }
                }

                Trace("Document type: {0} of type {1}", docType.Name, baseType.BaseType); 

                if (type == null) { 
                    Exception ex = new SerializationException(SR.GetString(SR.SerializerTypeNotFound, baseType.BaseType)); 

                    ex.HelpLink = SR.SerializerTypeNotFound; 
                    throw ex;
                }

                if (type.IsAbstract) { 
                    Exception ex = new SerializationException(SR.GetString(SR.SerializerTypeAbstract, type.FullName));
 
                    ex.HelpLink = SR.SerializerTypeAbstract; 
                    throw ex;
                } 

                ResolveNameEventHandler onResolveName = new ResolveNameEventHandler(OnResolveName);

                manager.ResolveName += onResolveName; 

                // HACK for backwards compatibility. 
                if (!(manager is DesignerSerializationManager)) { 
                    manager.AddSerializationProvider(new CodeDomSerializationProvider());
                } 

                documentObject = manager.CreateInstance(type, null, docType.Name, true);

                // Now that we have the document type, we create a nametable and fill it with member declarations. 
                // During this time we also search for our initialization method and save it off for later
                // processing. 
                // 
                nameTable = new HybridDictionary(docType.Members.Count, caseInsensitive);
                statementTable = new HybridDictionary(docType.Members.Count, caseInsensitive); 
                initMethod = null;

                RootContext rootExp = new RootContext(new CodeThisReferenceExpression(), documentObject);
 
                manager.Context.Push(rootExp);
                try { 
                    foreach (CodeTypeMember member in docType.Members) { 
                        if (member is CodeMemberField) {
                            if (string.Compare(member.Name, docType.Name, caseInsensitive, CultureInfo.InvariantCulture) != 0) { 
                                // always skip members with the same name as the type -- because that's the name
                                // we use when we resolve "base" and "this" items...
                                //
                                nameTable[member.Name] = member; 
                            }
                            // [....]: I added this because C# doesn't allow member names to match enclosing class names: 
                            // 
                            //    public class Foo {
                            //        public int Foo; 
                            //    }
                            //
                            //    doesn't compile.  So I wanted to stop users from getting into this state but VB allows this so this could
                            //    break some existing forms.  Since the compiler does a pretty good job of catching this with a meaningful error 
                            //    and we don't let you change the name of a member to match the class name (since both are sited components),
                            //    this happens somewhat automatically. 
                            // 
                            //
                            // 
                            //
                            //else {
                            //    // see if the type of the field is a component...
                            //    // 
                            //    string typeName = ((CodeMemberField)member).Type.BaseType;
                            //    Type fieldType = manager.GetType(typeName); 
                            //    if (fieldType != null && typeof(IComponent).IsAssignableFrom(fieldType)) { 
                            //        throw new Exception(SR.GetString(SR.SerializerMemberNameSameAsTypeName, member.Name));
                            //        // (from system.design.txt) SerializerMemberNameSameAsTypeName=Member name '{0}' cannot be the same as the enclosing type name. 
                            //    }
                            //}
                        }
                        else if (initMethod == null && member is CodeMemberMethod) { 
                            CodeMemberMethod method = (CodeMemberMethod)member;
 
                            if ((string.Compare(method.Name, InitMethodName, caseInsensitive, CultureInfo.InvariantCulture) == 0) && method.Parameters.Count == 0) { 
                                initMethod = method;
                            } 
                        }
                    }

                    Trace("Encountered {0} members to deserialize.", nameTable.Keys.Count); 
                    TraceWarningIf(initMethod == null, "Init method {0} wasn't found.", InitMethodName);
 
                    // We have the members, and possibly the init method.  Walk the init method looking for local variable declarations, 
                    // and add them to the pile too.
                    // 
                    if (initMethod != null) {
                        foreach (CodeStatement statement in initMethod.Statements) {
                            CodeVariableDeclarationStatement local = statement as CodeVariableDeclarationStatement;
 
                            if (local != null) {
                                nameTable[local.Name] = statement; 
                            } 
                        }
                    } 

                    // Check for the case of a reference that has the same variable name as our root
                    // object.  If we find such a reference, we pre-populate the name table with our
                    // document object.  We don't really have to populate, but it is very important 
                    // that we don't leave the original field in there.  Otherwise, we will try to
                    // load up the field, and since the class we're designing doesn't yet exist, this 
                    // will cause an error. 
                    //
                    if (nameTable[docType.Name] != null) { 
                        nameTable[docType.Name] = documentObject;
                    }

                    // We fill a "statement table" for everything in our init method.  This statement 
                    // table is a dictionary whose keys contain object names and whose values contain
                    // a statement collection of all statements with a LHS resolving to an object 
                    // by that name. 
                    //
                    if (initMethod != null) { 
                        FillStatementTable(initMethod, docType.Name);
                    }

                    // Interesting problem.  The CodeDom parser may auto generate statements 
                    // that are associated with other methods. VB does this, for example, to
                    // create statements automatically for Handles clauses.  The problem with 
                    // this technique is that we will end up with statements that are related 
                    // to variables that live solely in user code and not in InitializeComponent.
                    // We will attempt to construct instances of these objects with limited 
                    // success.  To guard against this, we check to see if the manager
                    // even supports this feature, and if it does, we must walk each
                    // statement.
                    // 
                    PropertyDescriptor supportGenerate = manager.Properties["SupportsStatementGeneration"];
 
                    if (supportGenerate != null && supportGenerate.PropertyType == typeof(bool) && ((bool)supportGenerate.GetValue(manager)) == true) { 
                        // Ok, we must do the more expensive work of validating the statements we get.
                        // 
                        foreach (string name in nameTable.Keys) {
                            OrderedCodeStatementCollection statements = (OrderedCodeStatementCollection)statementTable[name];

                            if (statements != null) { 
                                bool acceptStatement = false;
 
                                foreach (CodeStatement statement in statements) { 
                                    object genFlag = statement.UserData["GeneratedStatement"];
 
                                    if (genFlag == null || !(genFlag is bool) || !((bool)genFlag)) {
                                        acceptStatement = true;
                                        break;
                                    } 
                                }
 
                                if (!acceptStatement) { 
                                    statementTable.Remove(name);
                                } 
                            }
                        }
                    }
 
                    // Design time properties must be resolved before runtime properties to make
                    // sure that properties like "language" get established before we need to read 
                    // values out the resource bundle. 
                    //
                    Trace("--------------------------------------------------------------------"); 
                    Trace("     Beginning deserialization of {0} (design time)", docType.Name);
                    Trace("--------------------------------------------------------------------");

                    // Deserialize design time properties for the root component and any inherited component. 
                    //
                    IContainer container = (IContainer)manager.GetService(typeof(IContainer)); 
 
                    if (container != null) {
                        foreach (object comp in container.Components) { 
                            DeserializePropertiesFromResources(manager, comp, designTimeProperties);
                        }
                    }
 
                    // make sure we have fully deserialized everything that is referenced in the statement table.
                    // 
                    object[] keyValues = new object[statementTable.Values.Count]; 

                    statementTable.Values.CopyTo(keyValues, 0); 

                    // sort by the order so we deserialize in the same order the objects
                    // were decleared in.
                    // 
                    Array.Sort(keyValues, StatementOrderComparer.Default);
                    foreach (OrderedCodeStatementCollection statements in keyValues) { 
                        string name = statements.Name; 

                        if (name != null && !name.Equals(docType.Name)) { 
                            DeserializeName(manager, name);
                        }
                    }
 
                    // Now do the runtime part of the
                    // we must do the document class itself. 
                    // 
                    Trace("--------------------------------------------------------------------");
                    Trace("     Beginning deserialization of {0} (run time)", docType.Name); 
                    Trace("--------------------------------------------------------------------");

                    CodeStatementCollection rootStatements = (CodeStatementCollection)statementTable[docType.Name];
 
                    if (rootStatements != null && rootStatements.Count > 0) {
                        foreach (CodeStatement statement in rootStatements) { 
                            DeserializeStatement(manager, statement); 
                        }
                    } 
                }
                finally {
                    manager.ResolveName -= onResolveName;
                    initMethod = null; 
                    nameTable = null;
                    statementTable = null; 
                    Debug.Assert(manager.Context.Current == rootExp, "Context stack corrupted"); 
                    manager.Context.Pop();
                } 
            }

            return documentObject;
        } 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.DeserializeName"]/*' /> 
        /// <devdoc> 
        ///     This takes the given name and deserializes it from our name table.  Before blindly
        ///     deserializing it checks the contents of the name table to see if the object already 
        ///     exists within it.  We do this because deserializing one object may call back
        ///     into us through OnResolveName and deserialize another.
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        private object DeserializeName(IDesignerSerializationManager manager, string name) {
            string typeName = null; 
            Type type = null; 
            object value = nameTable[name];
 
            using (TraceScope("RootCodeDomSerializer::DeserializeName")) {
                Trace("Name: {0}", name);

                // If the name we're looking for isn't in our dictionary, we return null.  It is up to the caller 
                // to decide if this is an error or not.
                // 
                CodeMemberField field = null; 

                TraceIf(!(value is CodeObject), "Name already deserialized.  Type: {0}", (value == null ? "(null)" : value.GetType().Name)); 

                CodeObject codeObject = value as CodeObject;

                if (codeObject != null) { 
                    // If we fail, don't return a CodeDom element to the caller!
                    // 
                    value = null; 

                    // Clear out our nametable entry here -- A badly written serializer may cause a recursion here, and 
                    // we want to stop it.
                    //
                    nameTable[name] = null;
 
                    // What kind of code object is this?
                    // 
                    Trace("CodeDom type: {0}", codeObject.GetType().Name); 
                    if (codeObject is CodeVariableDeclarationStatement) {
                        CodeVariableDeclarationStatement declaration = (CodeVariableDeclarationStatement)codeObject; 

                        typeName = GetTypeNameFromCodeTypeReference(manager, declaration.Type);
                    }
                    else if (codeObject is CodeMemberField) { 
                        field = (CodeMemberField)codeObject;
                        typeName = GetTypeNameFromCodeTypeReference(manager, field.Type); 
                    } 
                }
                else if (value != null) { 
                    return value;
                }
                else {
                    IContainer container = (IContainer)manager.GetService(typeof(IContainer)); 

                    if (container != null) { 
                        Trace("Try to get the type name from the container: {0}", name); 

                        IComponent comp = container.Components[name]; 

                        if (comp != null) {
                            typeName = comp.GetType().FullName;
 
                            // we had to go to the host here, so there isn't a nametable entry here --
                            // push in the component here so we don't accidentally recurse when 
                            // we try to deserialize this object. 
                            //
                            nameTable[name] = comp; 
                        }
                    }
                }
 
                // Special case our container name to point to the designer host -- it is our container at design time.
                // 
                if (name.Equals(ContainerName)) { 
                    IContainer container = (IContainer)manager.GetService(typeof(IContainer));
 
                    if (container != null) {
                        Trace("Substituted serialization manager's container");
                        value = container;
                    } 
                }
                else if (typeName != null) { 
                    // Default case -- something that needs to be deserialized 
                    //
                    type = manager.GetType(typeName); 
                    if (type == null) {
                        TraceError("Type does not exist: {0}", typeName);
                        manager.ReportError(new SerializationException(SR.GetString(SR.SerializerTypeNotFound, typeName)));
                    } 
                    else {
                        CodeStatementCollection statements = (CodeStatementCollection)statementTable[name]; 
 
                        if (statements != null && statements.Count > 0) {
                            CodeDomSerializer serializer = (CodeDomSerializer)manager.GetSerializer(type, typeof(CodeDomSerializer)); 

                            if (serializer == null) {
                                // We report this as an error.  This indicates that there are code statements
                                // in initialize component that we do not know how to load. 
                                //
                                TraceError("Type referenced in init method has no serializer: {0}", type.Name); 
                                manager.ReportError(SR.GetString(SR.SerializerNoSerializerForComponent, type.FullName)); 
                            }
                            else { 
                                Trace("--------------------------------------------------------------------");
                                Trace("     Beginning deserialization of {0}", name);
                                Trace("--------------------------------------------------------------------");
                                try { 
                                    value = serializer.Deserialize(manager, statements);
 
                                    // Search for a modifiers property, and set it. 
                                    //
                                    if (value != null && field != null) { 
                                        PropertyDescriptor prop = TypeDescriptor.GetProperties(value)["Modifiers"];

                                        if (prop != null && prop.PropertyType == typeof(MemberAttributes)) {
                                            MemberAttributes modifiers = field.Attributes & MemberAttributes.AccessMask; 

                                            prop.SetValue(value, modifiers); 
                                        } 
                                    }
                                } 
                                catch (Exception ex) {
                                    manager.ReportError(ex);
                                }
                            } 
                        }
                    } 
                } 

                nameTable[name] = value; 
            }

            return value;
        } 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.FillStatementTable"]/*' /> 
        /// <devdoc> 
        ///     This method enumerates all the statements in the given method.  For those statements who
        ///     have a LHS that points to a name in our nametable, we add the statement to a Statement 
        ///     Collection within the statementTable dictionary.  This allows us to very quickly
        ///     put to gether what statements are associated with what names.
        /// </devdoc>
        private void FillStatementTable(CodeMemberMethod method, string className) { 
            using (TraceScope("RootCodeDomSerializer::FillStatementTable")) {
                // Look in the method body to try to find statements with a LHS that 
                // points to a name in our nametable. 
                //
                foreach (CodeStatement statement in method.Statements) { 
                    CodeExpression expression = null;

                    if (statement is CodeAssignStatement) {
                        Trace("Processing CodeAssignStatement"); 
                        expression = ((CodeAssignStatement)statement).Left;
                    } 
                    else if (statement is CodeAttachEventStatement) { 
                        Trace("Processing CodeAttachEventStatement");
                        expression = ((CodeAttachEventStatement)statement).Event; 
                    }
                    else if (statement is CodeRemoveEventStatement) {
                        Trace("Processing CodeRemoveEventStatement");
                        expression = ((CodeRemoveEventStatement)statement).Event; 
                    }
                    else if (statement is CodeExpressionStatement) { 
                        Trace("Processing CodeExpressionStatement"); 
                        expression = ((CodeExpressionStatement)statement).Expression;
                    } 
                    else if (statement is CodeVariableDeclarationStatement) {
                        // Local variables are different because their LHS contains no expression.
                        //
                        Trace("Processing CodeVariableDeclarationStatement"); 

                        CodeVariableDeclarationStatement localVar = (CodeVariableDeclarationStatement)statement; 
 
                        if (localVar.InitExpression != null && nameTable.Contains(localVar.Name)) {
                            AddStatement(localVar.Name, localVar); 
                        }

                        expression = null;
                    } 

                    if (expression != null) { 
                        // Simplify the expression as much as we can, looking for our target 
                        // object in the process.  If we find an expression that refers to our target
                        // object, we're done and can move on to the next statement. 
                        //
                        while (true) {
                            if (expression is CodeCastExpression) {
                                Trace("Simplifying CodeCastExpression"); 
                                expression = ((CodeCastExpression)expression).Expression;
                            } 
                            else if (expression is CodeDelegateCreateExpression) { 
                                Trace("Simplifying CodeDelegateCreateExpression");
                                expression = ((CodeDelegateCreateExpression)expression).TargetObject; 
                            }
                            else if (expression is CodeDelegateInvokeExpression) {
                                Trace("Simplifying CodeDelegateInvokeExpression");
                                expression = ((CodeDelegateInvokeExpression)expression).TargetObject; 
                            }
                            else if (expression is CodeDirectionExpression) { 
                                Trace("Simplifying CodeDirectionExpression"); 
                                expression = ((CodeDirectionExpression)expression).Expression;
                            } 
                            else if (expression is CodeEventReferenceExpression) {
                                Trace("Simplifying CodeEventReferenceExpression");
                                expression = ((CodeEventReferenceExpression)expression).TargetObject;
                            } 
                            else if (expression is CodeMethodInvokeExpression) {
                                Trace("Simplifying CodeMethodInvokeExpression"); 
                                expression = ((CodeMethodInvokeExpression)expression).Method; 
                            }
                            else if (expression is CodeMethodReferenceExpression) { 
                                Trace("Simplifying CodeMethodReferenceExpression");
                                expression = ((CodeMethodReferenceExpression)expression).TargetObject;
                            }
                            else if (expression is CodeArrayIndexerExpression) { 
                                Trace("Simplifying CodeArrayIndexerExpression");
                                expression = ((CodeArrayIndexerExpression)expression).TargetObject; 
                            } 
                            else if (expression is CodeFieldReferenceExpression) {
                                // For fields we need to check to see if the field name is equal to the target object. 
                                // If it is, then we have the expression we want.  We can add the statement here
                                // and then break out of our loop.
                                //
                                // Note:  We cannot validate that this is a name in our nametable.  The nametable 
                                // only contains names we have discovered through code parsing and will not include
                                // data from any inherited objects.  We accept the field now, and then fail later 
                                // if we try to resolve it to an object and we can't find it. 
                                //
                                CodeFieldReferenceExpression field = (CodeFieldReferenceExpression)expression; 

                                if (field.TargetObject is CodeThisReferenceExpression) {
                                    AddStatement(field.FieldName, statement);
                                    break; 
                                }
                                else { 
                                    Trace("Simplifying CodeFieldReferenceExpression"); 
                                    expression = field.TargetObject;
                                } 
                            }
                            else if (expression is CodePropertyReferenceExpression) {
                                // For properties we need to check to see if the property name is equal to the target object.
                                // If it is, then we have the expression we want.  We can add the statement here 
                                // and then break out of our loop.
                                // 
                                CodePropertyReferenceExpression property = (CodePropertyReferenceExpression)expression; 

                                if (property.TargetObject is CodeThisReferenceExpression && nameTable.Contains(property.PropertyName)) { 
                                    AddStatement(property.PropertyName, statement);
                                    break;
                                }
                                else { 
                                    Trace("Simplifying CodePropertyReferenceExpression");
                                    expression = property.TargetObject; 
                                } 
                            }
                            else if (expression is CodeVariableReferenceExpression) { 
                                // For variables we need to check to see if the variable name is equal to the target object.
                                // If it is, then we have the expression we want.  We can add the statement here
                                // and then break out of our loop.
                                // 
                                CodeVariableReferenceExpression variable = (CodeVariableReferenceExpression)expression;
 
                                if (nameTable.Contains(variable.VariableName)) { 
                                    AddStatement(variable.VariableName, statement);
                                } 
                                else {
                                    TraceWarning("Variable {0} used before it was declared.", variable.VariableName);
                                }
 
                                break;
                            } 
                            else if (expression is CodeThisReferenceExpression || expression is CodeBaseReferenceExpression) { 
                                // We cannot go any further than "this".  So, we break out
                                // of the loop.  We file this statement under the root object. 
                                //
                                AddStatement(className, statement);
                                break;
                            } 
                            else {
                                // We cannot simplify this expression any further, so we stop looping. 
                                // 
                                break;
                            } 
                        }
                    }
                }
            } 
        }
 
        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.GetMethodName"]/*' /> 
        /// <devdoc>
        ///     If this statement is a method invoke, this gets the name of the method. 
        ///     Otherwise, it returns null.
        /// </devdoc>
        private string GetMethodName(object statement) {
            string name = null; 

            while(name == null) { 
                if (statement is CodeExpressionStatement) { 
                    statement = ((CodeExpressionStatement)statement).Expression;
                } 
                else if (statement is CodeMethodInvokeExpression) {
                    statement = ((CodeMethodInvokeExpression)statement).Method;
                }
                else if (statement is CodeMethodReferenceExpression) { 
                    return ((CodeMethodReferenceExpression)statement).MethodName;
                } 
                else { 
                    break;
                } 
            }

            return name;
        } 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.OnResolveName"]/*' /> 
        /// <devdoc> 
        ///     Called by the serialization manager to resolve a name to an object.
        /// </devdoc> 
        private void OnResolveName(object sender, ResolveNameEventArgs e) {
            Debug.Assert(nameTable != null, "OnResolveName called and we are not deserializing!");

            using(TraceScope("RootCodeDomSerializer::OnResolveName")) { 
                Trace("Name: {0}", e.Name);
 
                // If someone else already found a value, who are we to complain? 
                //
                if (e.Value != null) { 
                    TraceWarning("Another name resolver has already found the value for {0}.", e.Name);
                }
                else {
                    IDesignerSerializationManager manager = (IDesignerSerializationManager)sender; 
                    object value = DeserializeName(manager, e.Name);
                    e.Value = value; 
                } 
            }
        } 

#if DEBUG
        static int NextSerializeSessionId = 0;
#endif 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.Serialize"]/*' /> 
        /// <devdoc> 
        ///     Serializes the given object into a CodeDom object.
        /// </devdoc> 
        public override object Serialize(IDesignerSerializationManager manager, object value) {

            if (manager == null || value == null) {
                throw new ArgumentNullException( manager == null ? "manager" : "value"); 
            }
 
            // As the root serializer, we are responsible for creating the code class for our 
            // object.  We will create the class, and the init method, and then push both
            // on the context stack. 
            // These will be used by other serializers to insert statements and members.
            //
            CodeTypeDeclaration docType = new CodeTypeDeclaration(manager.GetName(value));
            RootContext rootExp = new RootContext(new CodeThisReferenceExpression(), value); 

            using (TraceScope("RootCodeDomSerializer::Serialize")) { 
 
    #if DEBUG
                int serializeSessionId = ++NextSerializeSessionId; 
                Trace("Serializer  id={0}", serializeSessionId);
    #endif
                Trace("Value: {0}", value);
 
                docType.BaseTypes.Add(value.GetType());
 
                containerRequired = false; 
                manager.Context.Push(rootExp);
                manager.Context.Push(this); 
                manager.Context.Push(docType);

                // HACK for backwards compatibility.
                if (!(manager is DesignerSerializationManager)) { 
                    manager.AddSerializationProvider(new CodeDomSerializationProvider());
                } 
 
                try {
                    TraceWarningIf(!(value is IComponent), "Object {0} is not an IComponent but the root serializer is attempting to serialize it.", value); 
                    if (value is IComponent) {
                        ISite site = ((IComponent)value).Site;
                        TraceWarningIf(site == null, "Object {0} is not sited but the root serializer is attempting to serialize it.", value);
                        if (site != null) { 

                            // Do each component, skipping us, since we handle our own serialization. 
                            // 
                            ICollection components = site.Container.Components;
                            StatementContext cxt = new StatementContext(); 
                            cxt.StatementCollection.Populate(components);
                            manager.Context.Push(cxt);

                            try { 

                                // This looks really sweet, but is it worth it?  We take the 
                                // perf hit of a quicksort + the allocation overhead of 4 
                                // bytes for each component.  Profiles show this as a 2%
                                // cost for a form with 100 controls.  Let's meet the perf 
                                // goals first, then consider uncommenting this.
                                //
                                //ArrayList sortedComponents = new ArrayList(components);
                                //sortedComponents.Sort(ComponentComparer.Default); 
                                //components = sortedComponents;
 
                                foreach(IComponent component in components) { 
                                    if (component != value && !IsSerialized(manager, component)) {
                                        Trace("--------------------------------------------------------------------"); 
                                        Trace("     Beginning serialization of {0}", component.Site.Name);
                                        Trace("--------------------------------------------------------------------");
                                        CodeDomSerializer ser = GetSerializer(manager, component);
                                        if (ser != null) { 
                                            SerializeToExpression(manager, component);
                                        } 
                                        else { 
                                            TraceError("Component has no serializer: {0}", component.GetType().Name);
                                            manager.ReportError(SR.GetString(SR.SerializerNoSerializerForComponent, component.GetType().FullName)); 
                                        }
                                    }
                                }
 
                                // Push the component being serialized onto the stack.  It may be handy to
                                // be able to discover this. 
                                // 
                                manager.Context.Push(value);
 
                                try {
                                    Trace("--------------------------------------------------------------------");
                                    Trace("     Beginning serialization of root component {0}", manager.GetName(value));
                                    Trace("--------------------------------------------------------------------"); 
                                    CodeDomSerializer rootSer = GetSerializer(manager, value);
                                    if (rootSer != null && !IsSerialized(manager, value)) { 
                                        SerializeToExpression(manager, value); 
                                    }
                                    else { 
                                        TraceError("Component has no serializer: {0}", value.GetType().Name);
                                        manager.ReportError(SR.GetString(SR.SerializerNoSerializerForComponent, value.GetType().FullName));
                                    }
                                } 
                                finally {
                                    Debug.Assert(manager.Context.Current == value, "Context stack corrupted"); 
                                    manager.Context.Pop(); 
                                }
                            } 
                            finally {
                                Debug.Assert(manager.Context.Current == cxt, "Context stack corrupted");
                                manager.Context.Pop();
                            } 

                            CodeMemberMethod method = new CodeMemberMethod(); 
                            method.Name = InitMethodName; 
                            method.Attributes = MemberAttributes.Private;
                            docType.Members.Add(method); 

                            // We write the code into the method in the following order:
                            //
                            // components = new Container() assignment 
                            // individual component assignments
                            // root object design time proeprties 
                            // individual component properties / events 
                            // root object properties / events
                            // 
                            ArrayList codeElements = new ArrayList();
                            foreach(object o in components) {
                                if (o != value) {
                                    codeElements.Add(cxt.StatementCollection[o]); 
                                }
                            } 
 
                            CodeStatementCollection sroot = cxt.StatementCollection[value];
                            if (sroot != null) { 
                                codeElements.Add(cxt.StatementCollection[value]);
                            }

                            Trace("Assembling init method from {0} statements", codeElements.Count); 

                            if (ContainerRequired) { 
                                SerializeContainerDeclaration(manager, method.Statements); 
                            }
 
                            SerializeElementsToStatements(codeElements, method.Statements);
                        }
                    }
                } 
                finally {
                    Debug.Assert(manager.Context.Current == docType, "Somebody messed up our context stack"); 
                    manager.Context.Pop(); 
                    manager.Context.Pop();
                    manager.Context.Pop(); 
                }

                Trace("--------------------------------------------------------------------");
                Trace("     Generated code for {0}", manager.GetName(value)); 
                Trace("--------------------------------------------------------------------");
                Trace(docType); 
 
    #if DEBUG
                Trace("end serialize  id={0}", serializeSessionId); 
    #endif
            }
            return docType;
        } 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.SerializeContainerDeclaration"]/*' /> 
        /// <devdoc> 
        ///     This ensures that the declaration for IContainer exists in the class, and that
        ///     the init method creates an instance of Conatiner. 
        /// </devdoc>
        private void SerializeContainerDeclaration(IDesignerSerializationManager manager, CodeStatementCollection statements) {

            // Get some services we need up front. 
            //
            CodeTypeDeclaration docType = (CodeTypeDeclaration)manager.Context[typeof(CodeTypeDeclaration)]; 
 
            if (docType == null) {
                Debug.Fail("Missing CodeDom objects in context."); 
                return;
            }

            Trace("RootCodeDomSerializer::SerializeContainerDeclaration"); 

            // Add the definition for IContainer to the class. 
            // 
            Type containerType = typeof(IContainer);
            CodeTypeReference containerTypeRef = new CodeTypeReference(containerType); 

            CodeMemberField componentsDeclaration = new CodeMemberField(containerTypeRef, ContainerName);
            componentsDeclaration.Attributes = MemberAttributes.Private;
            docType.Members.Add(componentsDeclaration); 

            // Next, add the instance creation to the init method.  We change containerType 
            // here from IContainer to Container. 
            //
            containerType = typeof(Container); 
            containerTypeRef = new CodeTypeReference(containerType);

            CodeObjectCreateExpression objectCreate = new CodeObjectCreateExpression();
            objectCreate.CreateType = containerTypeRef; 

            CodeFieldReferenceExpression fieldRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), ContainerName); 
            CodeAssignStatement assignment = new CodeAssignStatement(fieldRef, objectCreate); 

            statements.Add(assignment); 
        }

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.SerializeElementsToStatements"]/*' />
        /// <devdoc> 
        ///     Takes the given list of elements and serializes them into the statement
        ///     collection.  This performs a simple sorting algorithm as well, putting 
        ///     local variables at the top, assignments next, and statements last. 
        /// </devdoc>
        private void SerializeElementsToStatements(ArrayList elements, CodeStatementCollection statements) { 

            ArrayList beginInitStatements = new ArrayList();
            ArrayList endInitStatements = new ArrayList();
            ArrayList localVariables = new ArrayList(); 
            ArrayList fieldAssignments = new ArrayList();
            ArrayList codeStatements = new ArrayList(); 
 
            foreach(object element in elements) {
                Trace("ElementStatement: {0}", element.GetType().FullName); 

                if (element is CodeAssignStatement && ((CodeAssignStatement)element).Left is CodeFieldReferenceExpression) {
                    fieldAssignments.Add(element);
                } 
                else if (element is CodeVariableDeclarationStatement) {
                    localVariables.Add(element); 
                } 
                else if (element is CodeStatement) {
                    string order = ((CodeObject)element).UserData["statement-ordering"] as string; 
                    if (order != null) {
                        switch (order) {
                            case "begin":
                                beginInitStatements.Add(element); 
                                break;
                            case "end": 
                                endInitStatements.Add(element); 
                                break;
                            case "default": 
                            default:
                                codeStatements.Add(element);
                                break;
                        } 
                    }
                    else { 
                        codeStatements.Add(element); 
                    }
                } 
                else if (element is CodeStatementCollection) {
                    CodeStatementCollection childStatements = (CodeStatementCollection)element;
                    foreach(CodeStatement statement in childStatements) {
                        if (statement is CodeAssignStatement && ((CodeAssignStatement)statement).Left is CodeFieldReferenceExpression) { 
                            fieldAssignments.Add(statement);
                        } 
                        else if (statement is CodeVariableDeclarationStatement) { 
                            localVariables.Add(statement);
                        } 
                        else {
                            string order = statement.UserData["statement-ordering"] as string;
                            if (order != null) {
                                switch (order) { 
                                    case "begin":
                                        beginInitStatements.Add(statement); 
                                        break; 
                                    case "end":
                                        endInitStatements.Add(statement); 
                                        break;
                                    case "default":
                                    default:
                                        codeStatements.Add(statement); 
                                        break;
                                } 
                            } 
                            else {
                                codeStatements.Add(statement); 
                            }
                        }
                    }
                } 
            }
 
            // Now that we have our lists, we can actually add them in the 
            // proper order to the statement collection.
            // 
            statements.AddRange((CodeStatement[])localVariables.ToArray(typeof(CodeStatement)));
            statements.AddRange((CodeStatement[])fieldAssignments.ToArray(typeof(CodeStatement)));
            statements.AddRange((CodeStatement[])beginInitStatements.ToArray(typeof(CodeStatement)));
            statements.AddRange((CodeStatement[])codeStatements.ToArray(typeof(CodeStatement))); 
            statements.AddRange((CodeStatement[])endInitStatements.ToArray(typeof(CodeStatement)));
        } 
 
        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.SerializeRootObject"]/*' />
        /// <devdoc> 
        ///     Serializes the root object of the object graph.
        /// </devdoc>
        private CodeStatementCollection SerializeRootObject(IDesignerSerializationManager manager, object value, bool designTime) {
            // Get some services we need up front. 
            //
            CodeTypeDeclaration docType = (CodeTypeDeclaration)manager.Context[typeof(CodeTypeDeclaration)]; 
 
            if (docType == null) {
                Debug.Fail("Missing CodeDom objects in context."); 
                return null;
            }

            CodeStatementCollection statements = new CodeStatementCollection(); 
            using (TraceScope("RootCodeDomSerializer::SerializeRootObject")) {
                Trace("Design time values: {0}", designTime.ToString()); 
 
                if (designTime) {
                    SerializeProperties(manager, statements, value, designTimeProperties); 
                }
                else {
                    SerializeProperties(manager, statements, value, runTimeProperties);
                    SerializeEvents(manager, statements, value, null); 
                }
            } 
            return statements; 
        }
 
        private class StatementOrderComparer : IComparer {

            public static readonly StatementOrderComparer Default = new StatementOrderComparer();
 
            private StatementOrderComparer() {
            } 
 
            public int Compare(object left, object right) {
                OrderedCodeStatementCollection cscLeft = left as OrderedCodeStatementCollection; 
                OrderedCodeStatementCollection cscRight = right as OrderedCodeStatementCollection;

                if (left == null) {
                    return 1; 
                }
                else if (right == null) { 
                    return -1; 
                }
                else if (right == left) { 
                    return 0;
                }

                return cscLeft.Order - cscRight.Order; 
            }
        } 
 
        private class ComponentComparer : IComparer {
 
            public static readonly ComponentComparer Default = new ComponentComparer();

            private ComponentComparer() {
            } 

            public int Compare(object left, object right) { 
                int n = string.Compare(((IComponent)left).GetType().Name, 
                                      ((IComponent)right).GetType().Name, false, CultureInfo.InvariantCulture);
 
                if (n == 0) {
                    n = string.Compare(((IComponent)left).Site.Name,
                                       ((IComponent)right).Site.Name,
                                          true, CultureInfo.InvariantCulture); 
                }
 
                return n; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="RootCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.ComponentModel.Design.Serialization { 

    using System.Design;
    using System;
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Collections; 
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Resources;
    using System.Runtime.Serialization;
    using System.Globalization; 

 
    /// ******************************************************************************************* 
    /// NOTE:
    /// 
    /// This class is NOT used for Whidbey.  It has been replaced by TypeCodeDomSerializer as root
    /// serializers are no longer in vogue.  It is still here because we need to be compatible with
    /// existing code bases that use root serialization.  Don't remove it, but be careful when fixing
    /// bugs because you might not be changing the right bits. 
    ///
    /// ******************************************************************************************* 
    /// 
    /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer"]/*' />
    /// <devdoc> 
    ///     This is our root serialization object.  It is responsible for organizing all of the
    ///     serialization for subsequent objects.  This inherits from ComponentCodeDomSerializer
    ///     in order to share useful methods.
    /// </devdoc> 
    internal sealed class RootCodeDomSerializer : ComponentCodeDomSerializer {
 
        // Used only during deserialization to provide name to object mapping. 
        //
        private IDictionary      nameTable; 
        private IDictionary      statementTable;
        private CodeMemberMethod initMethod;
        private bool             containerRequired;
 
        private static readonly Attribute[] designTimeProperties = new Attribute[] { DesignOnlyAttribute.Yes};
        private static readonly Attribute[] runTimeProperties = new Attribute[] { DesignOnlyAttribute.No}; 
 
        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.ContainerName"]/*' />
        /// <devdoc> 
        ///     The name of the IContainer we will use for components that require a container.
        ///     Note that compnent model serializer also has this property.
        /// </devdoc>
        public string ContainerName { 
            get {
                return "components"; 
            } 
        }
 
        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.ContainerRequired"]/*' />
        /// <devdoc>
        ///     The component serializer will set this to true if it emitted a compnent declaration that required
        ///     a container. 
        /// </devdoc>
        public bool ContainerRequired { 
            get { 
                return containerRequired;
            } 
            set {
                containerRequired = value;
            }
        } 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.InitMethodName"]/*' /> 
        /// <devdoc> 
        ///     The name of the method we will serialize into.  We always use this, so if there
        ///     is a need to change it down the road, we can make it virtual. 
        /// </devdoc>
        public string InitMethodName {
            get {
                return "InitializeComponent"; 
            }
        } 
 
        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.AddStatement"]/*' />
        /// <devdoc> 
        ///     Unility method that adds the given statement to our statementTable dictionary under the
        ///     given name.
        /// </devdoc>
        private void AddStatement(string name, CodeStatement statement) { 
            OrderedCodeStatementCollection statements = (OrderedCodeStatementCollection)statementTable[name];
 
            if (statements == null) { 
                statements = new OrderedCodeStatementCollection();
 
                // push in an order key so we know what position this item was in the list of declarations.
                // this allows us to preserve ZOrder.
                //
                statements.Order = statementTable.Count; 
                statements.Name = name;
                statementTable[name] = statements; 
            } 

            statements.Add(statement); 
        }

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.Deserialize"]/*' />
        /// <devdoc> 
        ///     Deserilizes the given CodeDom element into a real object.  This
        ///     will use the serialization manager to create objects and resolve 
        ///     data types.  The root of the object graph is returned. 
        /// </devdoc>
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject) { 

            if (manager == null || codeObject == null) {
                throw new ArgumentNullException( manager == null ? "manager" : "codeObject");
            } 

            object documentObject = null; 
 
            using (TraceScope("RootCodeDomSerializer::Deserialize")) {
                if (!(codeObject is CodeTypeDeclaration)) { 
                    Debug.Fail("RootCodeDomSerializer::Deserialize requires a CodeTypeDeclaration to parse");
                    throw new ArgumentException(SR.GetString(SR.SerializerBadElementType, typeof(CodeTypeDeclaration).FullName));
                }
 
                // Determine case-sensitivity
                // 
                bool caseInsensitive = false; 
                CodeDomProvider provider = manager.GetService(typeof(CodeDomProvider)) as CodeDomProvider;
 
                TraceWarningIf(provider == null, "CodeDomProvider is not a service provided by the serialization manager.  We use this to determine case sensitivity.  Assuming language is not case sensitive.");
                if (provider != null) {
                    caseInsensitive = ((provider.LanguageOptions & LanguageOptions.CaseInsensitive) != 0);
                } 

                // Get and initialize the document type. 
                // 
                CodeTypeDeclaration docType = (CodeTypeDeclaration)codeObject;
                CodeTypeReference baseType = null; 
                Type type = null;

                foreach (CodeTypeReference typeRef in docType.BaseTypes) {
                    Type t = manager.GetType(GetTypeNameFromCodeTypeReference(manager, typeRef)); 

                    if (t != null && !(t.IsInterface)) { 
                        baseType = typeRef; 
                        type = t;
                        break; 
                    }
                }

                Trace("Document type: {0} of type {1}", docType.Name, baseType.BaseType); 

                if (type == null) { 
                    Exception ex = new SerializationException(SR.GetString(SR.SerializerTypeNotFound, baseType.BaseType)); 

                    ex.HelpLink = SR.SerializerTypeNotFound; 
                    throw ex;
                }

                if (type.IsAbstract) { 
                    Exception ex = new SerializationException(SR.GetString(SR.SerializerTypeAbstract, type.FullName));
 
                    ex.HelpLink = SR.SerializerTypeAbstract; 
                    throw ex;
                } 

                ResolveNameEventHandler onResolveName = new ResolveNameEventHandler(OnResolveName);

                manager.ResolveName += onResolveName; 

                // HACK for backwards compatibility. 
                if (!(manager is DesignerSerializationManager)) { 
                    manager.AddSerializationProvider(new CodeDomSerializationProvider());
                } 

                documentObject = manager.CreateInstance(type, null, docType.Name, true);

                // Now that we have the document type, we create a nametable and fill it with member declarations. 
                // During this time we also search for our initialization method and save it off for later
                // processing. 
                // 
                nameTable = new HybridDictionary(docType.Members.Count, caseInsensitive);
                statementTable = new HybridDictionary(docType.Members.Count, caseInsensitive); 
                initMethod = null;

                RootContext rootExp = new RootContext(new CodeThisReferenceExpression(), documentObject);
 
                manager.Context.Push(rootExp);
                try { 
                    foreach (CodeTypeMember member in docType.Members) { 
                        if (member is CodeMemberField) {
                            if (string.Compare(member.Name, docType.Name, caseInsensitive, CultureInfo.InvariantCulture) != 0) { 
                                // always skip members with the same name as the type -- because that's the name
                                // we use when we resolve "base" and "this" items...
                                //
                                nameTable[member.Name] = member; 
                            }
                            // [....]: I added this because C# doesn't allow member names to match enclosing class names: 
                            // 
                            //    public class Foo {
                            //        public int Foo; 
                            //    }
                            //
                            //    doesn't compile.  So I wanted to stop users from getting into this state but VB allows this so this could
                            //    break some existing forms.  Since the compiler does a pretty good job of catching this with a meaningful error 
                            //    and we don't let you change the name of a member to match the class name (since both are sited components),
                            //    this happens somewhat automatically. 
                            // 
                            //
                            // 
                            //
                            //else {
                            //    // see if the type of the field is a component...
                            //    // 
                            //    string typeName = ((CodeMemberField)member).Type.BaseType;
                            //    Type fieldType = manager.GetType(typeName); 
                            //    if (fieldType != null && typeof(IComponent).IsAssignableFrom(fieldType)) { 
                            //        throw new Exception(SR.GetString(SR.SerializerMemberNameSameAsTypeName, member.Name));
                            //        // (from system.design.txt) SerializerMemberNameSameAsTypeName=Member name '{0}' cannot be the same as the enclosing type name. 
                            //    }
                            //}
                        }
                        else if (initMethod == null && member is CodeMemberMethod) { 
                            CodeMemberMethod method = (CodeMemberMethod)member;
 
                            if ((string.Compare(method.Name, InitMethodName, caseInsensitive, CultureInfo.InvariantCulture) == 0) && method.Parameters.Count == 0) { 
                                initMethod = method;
                            } 
                        }
                    }

                    Trace("Encountered {0} members to deserialize.", nameTable.Keys.Count); 
                    TraceWarningIf(initMethod == null, "Init method {0} wasn't found.", InitMethodName);
 
                    // We have the members, and possibly the init method.  Walk the init method looking for local variable declarations, 
                    // and add them to the pile too.
                    // 
                    if (initMethod != null) {
                        foreach (CodeStatement statement in initMethod.Statements) {
                            CodeVariableDeclarationStatement local = statement as CodeVariableDeclarationStatement;
 
                            if (local != null) {
                                nameTable[local.Name] = statement; 
                            } 
                        }
                    } 

                    // Check for the case of a reference that has the same variable name as our root
                    // object.  If we find such a reference, we pre-populate the name table with our
                    // document object.  We don't really have to populate, but it is very important 
                    // that we don't leave the original field in there.  Otherwise, we will try to
                    // load up the field, and since the class we're designing doesn't yet exist, this 
                    // will cause an error. 
                    //
                    if (nameTable[docType.Name] != null) { 
                        nameTable[docType.Name] = documentObject;
                    }

                    // We fill a "statement table" for everything in our init method.  This statement 
                    // table is a dictionary whose keys contain object names and whose values contain
                    // a statement collection of all statements with a LHS resolving to an object 
                    // by that name. 
                    //
                    if (initMethod != null) { 
                        FillStatementTable(initMethod, docType.Name);
                    }

                    // Interesting problem.  The CodeDom parser may auto generate statements 
                    // that are associated with other methods. VB does this, for example, to
                    // create statements automatically for Handles clauses.  The problem with 
                    // this technique is that we will end up with statements that are related 
                    // to variables that live solely in user code and not in InitializeComponent.
                    // We will attempt to construct instances of these objects with limited 
                    // success.  To guard against this, we check to see if the manager
                    // even supports this feature, and if it does, we must walk each
                    // statement.
                    // 
                    PropertyDescriptor supportGenerate = manager.Properties["SupportsStatementGeneration"];
 
                    if (supportGenerate != null && supportGenerate.PropertyType == typeof(bool) && ((bool)supportGenerate.GetValue(manager)) == true) { 
                        // Ok, we must do the more expensive work of validating the statements we get.
                        // 
                        foreach (string name in nameTable.Keys) {
                            OrderedCodeStatementCollection statements = (OrderedCodeStatementCollection)statementTable[name];

                            if (statements != null) { 
                                bool acceptStatement = false;
 
                                foreach (CodeStatement statement in statements) { 
                                    object genFlag = statement.UserData["GeneratedStatement"];
 
                                    if (genFlag == null || !(genFlag is bool) || !((bool)genFlag)) {
                                        acceptStatement = true;
                                        break;
                                    } 
                                }
 
                                if (!acceptStatement) { 
                                    statementTable.Remove(name);
                                } 
                            }
                        }
                    }
 
                    // Design time properties must be resolved before runtime properties to make
                    // sure that properties like "language" get established before we need to read 
                    // values out the resource bundle. 
                    //
                    Trace("--------------------------------------------------------------------"); 
                    Trace("     Beginning deserialization of {0} (design time)", docType.Name);
                    Trace("--------------------------------------------------------------------");

                    // Deserialize design time properties for the root component and any inherited component. 
                    //
                    IContainer container = (IContainer)manager.GetService(typeof(IContainer)); 
 
                    if (container != null) {
                        foreach (object comp in container.Components) { 
                            DeserializePropertiesFromResources(manager, comp, designTimeProperties);
                        }
                    }
 
                    // make sure we have fully deserialized everything that is referenced in the statement table.
                    // 
                    object[] keyValues = new object[statementTable.Values.Count]; 

                    statementTable.Values.CopyTo(keyValues, 0); 

                    // sort by the order so we deserialize in the same order the objects
                    // were decleared in.
                    // 
                    Array.Sort(keyValues, StatementOrderComparer.Default);
                    foreach (OrderedCodeStatementCollection statements in keyValues) { 
                        string name = statements.Name; 

                        if (name != null && !name.Equals(docType.Name)) { 
                            DeserializeName(manager, name);
                        }
                    }
 
                    // Now do the runtime part of the
                    // we must do the document class itself. 
                    // 
                    Trace("--------------------------------------------------------------------");
                    Trace("     Beginning deserialization of {0} (run time)", docType.Name); 
                    Trace("--------------------------------------------------------------------");

                    CodeStatementCollection rootStatements = (CodeStatementCollection)statementTable[docType.Name];
 
                    if (rootStatements != null && rootStatements.Count > 0) {
                        foreach (CodeStatement statement in rootStatements) { 
                            DeserializeStatement(manager, statement); 
                        }
                    } 
                }
                finally {
                    manager.ResolveName -= onResolveName;
                    initMethod = null; 
                    nameTable = null;
                    statementTable = null; 
                    Debug.Assert(manager.Context.Current == rootExp, "Context stack corrupted"); 
                    manager.Context.Pop();
                } 
            }

            return documentObject;
        } 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.DeserializeName"]/*' /> 
        /// <devdoc> 
        ///     This takes the given name and deserializes it from our name table.  Before blindly
        ///     deserializing it checks the contents of the name table to see if the object already 
        ///     exists within it.  We do this because deserializing one object may call back
        ///     into us through OnResolveName and deserialize another.
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        private object DeserializeName(IDesignerSerializationManager manager, string name) {
            string typeName = null; 
            Type type = null; 
            object value = nameTable[name];
 
            using (TraceScope("RootCodeDomSerializer::DeserializeName")) {
                Trace("Name: {0}", name);

                // If the name we're looking for isn't in our dictionary, we return null.  It is up to the caller 
                // to decide if this is an error or not.
                // 
                CodeMemberField field = null; 

                TraceIf(!(value is CodeObject), "Name already deserialized.  Type: {0}", (value == null ? "(null)" : value.GetType().Name)); 

                CodeObject codeObject = value as CodeObject;

                if (codeObject != null) { 
                    // If we fail, don't return a CodeDom element to the caller!
                    // 
                    value = null; 

                    // Clear out our nametable entry here -- A badly written serializer may cause a recursion here, and 
                    // we want to stop it.
                    //
                    nameTable[name] = null;
 
                    // What kind of code object is this?
                    // 
                    Trace("CodeDom type: {0}", codeObject.GetType().Name); 
                    if (codeObject is CodeVariableDeclarationStatement) {
                        CodeVariableDeclarationStatement declaration = (CodeVariableDeclarationStatement)codeObject; 

                        typeName = GetTypeNameFromCodeTypeReference(manager, declaration.Type);
                    }
                    else if (codeObject is CodeMemberField) { 
                        field = (CodeMemberField)codeObject;
                        typeName = GetTypeNameFromCodeTypeReference(manager, field.Type); 
                    } 
                }
                else if (value != null) { 
                    return value;
                }
                else {
                    IContainer container = (IContainer)manager.GetService(typeof(IContainer)); 

                    if (container != null) { 
                        Trace("Try to get the type name from the container: {0}", name); 

                        IComponent comp = container.Components[name]; 

                        if (comp != null) {
                            typeName = comp.GetType().FullName;
 
                            // we had to go to the host here, so there isn't a nametable entry here --
                            // push in the component here so we don't accidentally recurse when 
                            // we try to deserialize this object. 
                            //
                            nameTable[name] = comp; 
                        }
                    }
                }
 
                // Special case our container name to point to the designer host -- it is our container at design time.
                // 
                if (name.Equals(ContainerName)) { 
                    IContainer container = (IContainer)manager.GetService(typeof(IContainer));
 
                    if (container != null) {
                        Trace("Substituted serialization manager's container");
                        value = container;
                    } 
                }
                else if (typeName != null) { 
                    // Default case -- something that needs to be deserialized 
                    //
                    type = manager.GetType(typeName); 
                    if (type == null) {
                        TraceError("Type does not exist: {0}", typeName);
                        manager.ReportError(new SerializationException(SR.GetString(SR.SerializerTypeNotFound, typeName)));
                    } 
                    else {
                        CodeStatementCollection statements = (CodeStatementCollection)statementTable[name]; 
 
                        if (statements != null && statements.Count > 0) {
                            CodeDomSerializer serializer = (CodeDomSerializer)manager.GetSerializer(type, typeof(CodeDomSerializer)); 

                            if (serializer == null) {
                                // We report this as an error.  This indicates that there are code statements
                                // in initialize component that we do not know how to load. 
                                //
                                TraceError("Type referenced in init method has no serializer: {0}", type.Name); 
                                manager.ReportError(SR.GetString(SR.SerializerNoSerializerForComponent, type.FullName)); 
                            }
                            else { 
                                Trace("--------------------------------------------------------------------");
                                Trace("     Beginning deserialization of {0}", name);
                                Trace("--------------------------------------------------------------------");
                                try { 
                                    value = serializer.Deserialize(manager, statements);
 
                                    // Search for a modifiers property, and set it. 
                                    //
                                    if (value != null && field != null) { 
                                        PropertyDescriptor prop = TypeDescriptor.GetProperties(value)["Modifiers"];

                                        if (prop != null && prop.PropertyType == typeof(MemberAttributes)) {
                                            MemberAttributes modifiers = field.Attributes & MemberAttributes.AccessMask; 

                                            prop.SetValue(value, modifiers); 
                                        } 
                                    }
                                } 
                                catch (Exception ex) {
                                    manager.ReportError(ex);
                                }
                            } 
                        }
                    } 
                } 

                nameTable[name] = value; 
            }

            return value;
        } 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.FillStatementTable"]/*' /> 
        /// <devdoc> 
        ///     This method enumerates all the statements in the given method.  For those statements who
        ///     have a LHS that points to a name in our nametable, we add the statement to a Statement 
        ///     Collection within the statementTable dictionary.  This allows us to very quickly
        ///     put to gether what statements are associated with what names.
        /// </devdoc>
        private void FillStatementTable(CodeMemberMethod method, string className) { 
            using (TraceScope("RootCodeDomSerializer::FillStatementTable")) {
                // Look in the method body to try to find statements with a LHS that 
                // points to a name in our nametable. 
                //
                foreach (CodeStatement statement in method.Statements) { 
                    CodeExpression expression = null;

                    if (statement is CodeAssignStatement) {
                        Trace("Processing CodeAssignStatement"); 
                        expression = ((CodeAssignStatement)statement).Left;
                    } 
                    else if (statement is CodeAttachEventStatement) { 
                        Trace("Processing CodeAttachEventStatement");
                        expression = ((CodeAttachEventStatement)statement).Event; 
                    }
                    else if (statement is CodeRemoveEventStatement) {
                        Trace("Processing CodeRemoveEventStatement");
                        expression = ((CodeRemoveEventStatement)statement).Event; 
                    }
                    else if (statement is CodeExpressionStatement) { 
                        Trace("Processing CodeExpressionStatement"); 
                        expression = ((CodeExpressionStatement)statement).Expression;
                    } 
                    else if (statement is CodeVariableDeclarationStatement) {
                        // Local variables are different because their LHS contains no expression.
                        //
                        Trace("Processing CodeVariableDeclarationStatement"); 

                        CodeVariableDeclarationStatement localVar = (CodeVariableDeclarationStatement)statement; 
 
                        if (localVar.InitExpression != null && nameTable.Contains(localVar.Name)) {
                            AddStatement(localVar.Name, localVar); 
                        }

                        expression = null;
                    } 

                    if (expression != null) { 
                        // Simplify the expression as much as we can, looking for our target 
                        // object in the process.  If we find an expression that refers to our target
                        // object, we're done and can move on to the next statement. 
                        //
                        while (true) {
                            if (expression is CodeCastExpression) {
                                Trace("Simplifying CodeCastExpression"); 
                                expression = ((CodeCastExpression)expression).Expression;
                            } 
                            else if (expression is CodeDelegateCreateExpression) { 
                                Trace("Simplifying CodeDelegateCreateExpression");
                                expression = ((CodeDelegateCreateExpression)expression).TargetObject; 
                            }
                            else if (expression is CodeDelegateInvokeExpression) {
                                Trace("Simplifying CodeDelegateInvokeExpression");
                                expression = ((CodeDelegateInvokeExpression)expression).TargetObject; 
                            }
                            else if (expression is CodeDirectionExpression) { 
                                Trace("Simplifying CodeDirectionExpression"); 
                                expression = ((CodeDirectionExpression)expression).Expression;
                            } 
                            else if (expression is CodeEventReferenceExpression) {
                                Trace("Simplifying CodeEventReferenceExpression");
                                expression = ((CodeEventReferenceExpression)expression).TargetObject;
                            } 
                            else if (expression is CodeMethodInvokeExpression) {
                                Trace("Simplifying CodeMethodInvokeExpression"); 
                                expression = ((CodeMethodInvokeExpression)expression).Method; 
                            }
                            else if (expression is CodeMethodReferenceExpression) { 
                                Trace("Simplifying CodeMethodReferenceExpression");
                                expression = ((CodeMethodReferenceExpression)expression).TargetObject;
                            }
                            else if (expression is CodeArrayIndexerExpression) { 
                                Trace("Simplifying CodeArrayIndexerExpression");
                                expression = ((CodeArrayIndexerExpression)expression).TargetObject; 
                            } 
                            else if (expression is CodeFieldReferenceExpression) {
                                // For fields we need to check to see if the field name is equal to the target object. 
                                // If it is, then we have the expression we want.  We can add the statement here
                                // and then break out of our loop.
                                //
                                // Note:  We cannot validate that this is a name in our nametable.  The nametable 
                                // only contains names we have discovered through code parsing and will not include
                                // data from any inherited objects.  We accept the field now, and then fail later 
                                // if we try to resolve it to an object and we can't find it. 
                                //
                                CodeFieldReferenceExpression field = (CodeFieldReferenceExpression)expression; 

                                if (field.TargetObject is CodeThisReferenceExpression) {
                                    AddStatement(field.FieldName, statement);
                                    break; 
                                }
                                else { 
                                    Trace("Simplifying CodeFieldReferenceExpression"); 
                                    expression = field.TargetObject;
                                } 
                            }
                            else if (expression is CodePropertyReferenceExpression) {
                                // For properties we need to check to see if the property name is equal to the target object.
                                // If it is, then we have the expression we want.  We can add the statement here 
                                // and then break out of our loop.
                                // 
                                CodePropertyReferenceExpression property = (CodePropertyReferenceExpression)expression; 

                                if (property.TargetObject is CodeThisReferenceExpression && nameTable.Contains(property.PropertyName)) { 
                                    AddStatement(property.PropertyName, statement);
                                    break;
                                }
                                else { 
                                    Trace("Simplifying CodePropertyReferenceExpression");
                                    expression = property.TargetObject; 
                                } 
                            }
                            else if (expression is CodeVariableReferenceExpression) { 
                                // For variables we need to check to see if the variable name is equal to the target object.
                                // If it is, then we have the expression we want.  We can add the statement here
                                // and then break out of our loop.
                                // 
                                CodeVariableReferenceExpression variable = (CodeVariableReferenceExpression)expression;
 
                                if (nameTable.Contains(variable.VariableName)) { 
                                    AddStatement(variable.VariableName, statement);
                                } 
                                else {
                                    TraceWarning("Variable {0} used before it was declared.", variable.VariableName);
                                }
 
                                break;
                            } 
                            else if (expression is CodeThisReferenceExpression || expression is CodeBaseReferenceExpression) { 
                                // We cannot go any further than "this".  So, we break out
                                // of the loop.  We file this statement under the root object. 
                                //
                                AddStatement(className, statement);
                                break;
                            } 
                            else {
                                // We cannot simplify this expression any further, so we stop looping. 
                                // 
                                break;
                            } 
                        }
                    }
                }
            } 
        }
 
        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.GetMethodName"]/*' /> 
        /// <devdoc>
        ///     If this statement is a method invoke, this gets the name of the method. 
        ///     Otherwise, it returns null.
        /// </devdoc>
        private string GetMethodName(object statement) {
            string name = null; 

            while(name == null) { 
                if (statement is CodeExpressionStatement) { 
                    statement = ((CodeExpressionStatement)statement).Expression;
                } 
                else if (statement is CodeMethodInvokeExpression) {
                    statement = ((CodeMethodInvokeExpression)statement).Method;
                }
                else if (statement is CodeMethodReferenceExpression) { 
                    return ((CodeMethodReferenceExpression)statement).MethodName;
                } 
                else { 
                    break;
                } 
            }

            return name;
        } 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.OnResolveName"]/*' /> 
        /// <devdoc> 
        ///     Called by the serialization manager to resolve a name to an object.
        /// </devdoc> 
        private void OnResolveName(object sender, ResolveNameEventArgs e) {
            Debug.Assert(nameTable != null, "OnResolveName called and we are not deserializing!");

            using(TraceScope("RootCodeDomSerializer::OnResolveName")) { 
                Trace("Name: {0}", e.Name);
 
                // If someone else already found a value, who are we to complain? 
                //
                if (e.Value != null) { 
                    TraceWarning("Another name resolver has already found the value for {0}.", e.Name);
                }
                else {
                    IDesignerSerializationManager manager = (IDesignerSerializationManager)sender; 
                    object value = DeserializeName(manager, e.Name);
                    e.Value = value; 
                } 
            }
        } 

#if DEBUG
        static int NextSerializeSessionId = 0;
#endif 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.Serialize"]/*' /> 
        /// <devdoc> 
        ///     Serializes the given object into a CodeDom object.
        /// </devdoc> 
        public override object Serialize(IDesignerSerializationManager manager, object value) {

            if (manager == null || value == null) {
                throw new ArgumentNullException( manager == null ? "manager" : "value"); 
            }
 
            // As the root serializer, we are responsible for creating the code class for our 
            // object.  We will create the class, and the init method, and then push both
            // on the context stack. 
            // These will be used by other serializers to insert statements and members.
            //
            CodeTypeDeclaration docType = new CodeTypeDeclaration(manager.GetName(value));
            RootContext rootExp = new RootContext(new CodeThisReferenceExpression(), value); 

            using (TraceScope("RootCodeDomSerializer::Serialize")) { 
 
    #if DEBUG
                int serializeSessionId = ++NextSerializeSessionId; 
                Trace("Serializer  id={0}", serializeSessionId);
    #endif
                Trace("Value: {0}", value);
 
                docType.BaseTypes.Add(value.GetType());
 
                containerRequired = false; 
                manager.Context.Push(rootExp);
                manager.Context.Push(this); 
                manager.Context.Push(docType);

                // HACK for backwards compatibility.
                if (!(manager is DesignerSerializationManager)) { 
                    manager.AddSerializationProvider(new CodeDomSerializationProvider());
                } 
 
                try {
                    TraceWarningIf(!(value is IComponent), "Object {0} is not an IComponent but the root serializer is attempting to serialize it.", value); 
                    if (value is IComponent) {
                        ISite site = ((IComponent)value).Site;
                        TraceWarningIf(site == null, "Object {0} is not sited but the root serializer is attempting to serialize it.", value);
                        if (site != null) { 

                            // Do each component, skipping us, since we handle our own serialization. 
                            // 
                            ICollection components = site.Container.Components;
                            StatementContext cxt = new StatementContext(); 
                            cxt.StatementCollection.Populate(components);
                            manager.Context.Push(cxt);

                            try { 

                                // This looks really sweet, but is it worth it?  We take the 
                                // perf hit of a quicksort + the allocation overhead of 4 
                                // bytes for each component.  Profiles show this as a 2%
                                // cost for a form with 100 controls.  Let's meet the perf 
                                // goals first, then consider uncommenting this.
                                //
                                //ArrayList sortedComponents = new ArrayList(components);
                                //sortedComponents.Sort(ComponentComparer.Default); 
                                //components = sortedComponents;
 
                                foreach(IComponent component in components) { 
                                    if (component != value && !IsSerialized(manager, component)) {
                                        Trace("--------------------------------------------------------------------"); 
                                        Trace("     Beginning serialization of {0}", component.Site.Name);
                                        Trace("--------------------------------------------------------------------");
                                        CodeDomSerializer ser = GetSerializer(manager, component);
                                        if (ser != null) { 
                                            SerializeToExpression(manager, component);
                                        } 
                                        else { 
                                            TraceError("Component has no serializer: {0}", component.GetType().Name);
                                            manager.ReportError(SR.GetString(SR.SerializerNoSerializerForComponent, component.GetType().FullName)); 
                                        }
                                    }
                                }
 
                                // Push the component being serialized onto the stack.  It may be handy to
                                // be able to discover this. 
                                // 
                                manager.Context.Push(value);
 
                                try {
                                    Trace("--------------------------------------------------------------------");
                                    Trace("     Beginning serialization of root component {0}", manager.GetName(value));
                                    Trace("--------------------------------------------------------------------"); 
                                    CodeDomSerializer rootSer = GetSerializer(manager, value);
                                    if (rootSer != null && !IsSerialized(manager, value)) { 
                                        SerializeToExpression(manager, value); 
                                    }
                                    else { 
                                        TraceError("Component has no serializer: {0}", value.GetType().Name);
                                        manager.ReportError(SR.GetString(SR.SerializerNoSerializerForComponent, value.GetType().FullName));
                                    }
                                } 
                                finally {
                                    Debug.Assert(manager.Context.Current == value, "Context stack corrupted"); 
                                    manager.Context.Pop(); 
                                }
                            } 
                            finally {
                                Debug.Assert(manager.Context.Current == cxt, "Context stack corrupted");
                                manager.Context.Pop();
                            } 

                            CodeMemberMethod method = new CodeMemberMethod(); 
                            method.Name = InitMethodName; 
                            method.Attributes = MemberAttributes.Private;
                            docType.Members.Add(method); 

                            // We write the code into the method in the following order:
                            //
                            // components = new Container() assignment 
                            // individual component assignments
                            // root object design time proeprties 
                            // individual component properties / events 
                            // root object properties / events
                            // 
                            ArrayList codeElements = new ArrayList();
                            foreach(object o in components) {
                                if (o != value) {
                                    codeElements.Add(cxt.StatementCollection[o]); 
                                }
                            } 
 
                            CodeStatementCollection sroot = cxt.StatementCollection[value];
                            if (sroot != null) { 
                                codeElements.Add(cxt.StatementCollection[value]);
                            }

                            Trace("Assembling init method from {0} statements", codeElements.Count); 

                            if (ContainerRequired) { 
                                SerializeContainerDeclaration(manager, method.Statements); 
                            }
 
                            SerializeElementsToStatements(codeElements, method.Statements);
                        }
                    }
                } 
                finally {
                    Debug.Assert(manager.Context.Current == docType, "Somebody messed up our context stack"); 
                    manager.Context.Pop(); 
                    manager.Context.Pop();
                    manager.Context.Pop(); 
                }

                Trace("--------------------------------------------------------------------");
                Trace("     Generated code for {0}", manager.GetName(value)); 
                Trace("--------------------------------------------------------------------");
                Trace(docType); 
 
    #if DEBUG
                Trace("end serialize  id={0}", serializeSessionId); 
    #endif
            }
            return docType;
        } 

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.SerializeContainerDeclaration"]/*' /> 
        /// <devdoc> 
        ///     This ensures that the declaration for IContainer exists in the class, and that
        ///     the init method creates an instance of Conatiner. 
        /// </devdoc>
        private void SerializeContainerDeclaration(IDesignerSerializationManager manager, CodeStatementCollection statements) {

            // Get some services we need up front. 
            //
            CodeTypeDeclaration docType = (CodeTypeDeclaration)manager.Context[typeof(CodeTypeDeclaration)]; 
 
            if (docType == null) {
                Debug.Fail("Missing CodeDom objects in context."); 
                return;
            }

            Trace("RootCodeDomSerializer::SerializeContainerDeclaration"); 

            // Add the definition for IContainer to the class. 
            // 
            Type containerType = typeof(IContainer);
            CodeTypeReference containerTypeRef = new CodeTypeReference(containerType); 

            CodeMemberField componentsDeclaration = new CodeMemberField(containerTypeRef, ContainerName);
            componentsDeclaration.Attributes = MemberAttributes.Private;
            docType.Members.Add(componentsDeclaration); 

            // Next, add the instance creation to the init method.  We change containerType 
            // here from IContainer to Container. 
            //
            containerType = typeof(Container); 
            containerTypeRef = new CodeTypeReference(containerType);

            CodeObjectCreateExpression objectCreate = new CodeObjectCreateExpression();
            objectCreate.CreateType = containerTypeRef; 

            CodeFieldReferenceExpression fieldRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), ContainerName); 
            CodeAssignStatement assignment = new CodeAssignStatement(fieldRef, objectCreate); 

            statements.Add(assignment); 
        }

        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.SerializeElementsToStatements"]/*' />
        /// <devdoc> 
        ///     Takes the given list of elements and serializes them into the statement
        ///     collection.  This performs a simple sorting algorithm as well, putting 
        ///     local variables at the top, assignments next, and statements last. 
        /// </devdoc>
        private void SerializeElementsToStatements(ArrayList elements, CodeStatementCollection statements) { 

            ArrayList beginInitStatements = new ArrayList();
            ArrayList endInitStatements = new ArrayList();
            ArrayList localVariables = new ArrayList(); 
            ArrayList fieldAssignments = new ArrayList();
            ArrayList codeStatements = new ArrayList(); 
 
            foreach(object element in elements) {
                Trace("ElementStatement: {0}", element.GetType().FullName); 

                if (element is CodeAssignStatement && ((CodeAssignStatement)element).Left is CodeFieldReferenceExpression) {
                    fieldAssignments.Add(element);
                } 
                else if (element is CodeVariableDeclarationStatement) {
                    localVariables.Add(element); 
                } 
                else if (element is CodeStatement) {
                    string order = ((CodeObject)element).UserData["statement-ordering"] as string; 
                    if (order != null) {
                        switch (order) {
                            case "begin":
                                beginInitStatements.Add(element); 
                                break;
                            case "end": 
                                endInitStatements.Add(element); 
                                break;
                            case "default": 
                            default:
                                codeStatements.Add(element);
                                break;
                        } 
                    }
                    else { 
                        codeStatements.Add(element); 
                    }
                } 
                else if (element is CodeStatementCollection) {
                    CodeStatementCollection childStatements = (CodeStatementCollection)element;
                    foreach(CodeStatement statement in childStatements) {
                        if (statement is CodeAssignStatement && ((CodeAssignStatement)statement).Left is CodeFieldReferenceExpression) { 
                            fieldAssignments.Add(statement);
                        } 
                        else if (statement is CodeVariableDeclarationStatement) { 
                            localVariables.Add(statement);
                        } 
                        else {
                            string order = statement.UserData["statement-ordering"] as string;
                            if (order != null) {
                                switch (order) { 
                                    case "begin":
                                        beginInitStatements.Add(statement); 
                                        break; 
                                    case "end":
                                        endInitStatements.Add(statement); 
                                        break;
                                    case "default":
                                    default:
                                        codeStatements.Add(statement); 
                                        break;
                                } 
                            } 
                            else {
                                codeStatements.Add(statement); 
                            }
                        }
                    }
                } 
            }
 
            // Now that we have our lists, we can actually add them in the 
            // proper order to the statement collection.
            // 
            statements.AddRange((CodeStatement[])localVariables.ToArray(typeof(CodeStatement)));
            statements.AddRange((CodeStatement[])fieldAssignments.ToArray(typeof(CodeStatement)));
            statements.AddRange((CodeStatement[])beginInitStatements.ToArray(typeof(CodeStatement)));
            statements.AddRange((CodeStatement[])codeStatements.ToArray(typeof(CodeStatement))); 
            statements.AddRange((CodeStatement[])endInitStatements.ToArray(typeof(CodeStatement)));
        } 
 
        /// <include file='doc\RootCodeDomSerializer.uex' path='docs/doc[@for="RootCodeDomSerializer.SerializeRootObject"]/*' />
        /// <devdoc> 
        ///     Serializes the root object of the object graph.
        /// </devdoc>
        private CodeStatementCollection SerializeRootObject(IDesignerSerializationManager manager, object value, bool designTime) {
            // Get some services we need up front. 
            //
            CodeTypeDeclaration docType = (CodeTypeDeclaration)manager.Context[typeof(CodeTypeDeclaration)]; 
 
            if (docType == null) {
                Debug.Fail("Missing CodeDom objects in context."); 
                return null;
            }

            CodeStatementCollection statements = new CodeStatementCollection(); 
            using (TraceScope("RootCodeDomSerializer::SerializeRootObject")) {
                Trace("Design time values: {0}", designTime.ToString()); 
 
                if (designTime) {
                    SerializeProperties(manager, statements, value, designTimeProperties); 
                }
                else {
                    SerializeProperties(manager, statements, value, runTimeProperties);
                    SerializeEvents(manager, statements, value, null); 
                }
            } 
            return statements; 
        }
 
        private class StatementOrderComparer : IComparer {

            public static readonly StatementOrderComparer Default = new StatementOrderComparer();
 
            private StatementOrderComparer() {
            } 
 
            public int Compare(object left, object right) {
                OrderedCodeStatementCollection cscLeft = left as OrderedCodeStatementCollection; 
                OrderedCodeStatementCollection cscRight = right as OrderedCodeStatementCollection;

                if (left == null) {
                    return 1; 
                }
                else if (right == null) { 
                    return -1; 
                }
                else if (right == left) { 
                    return 0;
                }

                return cscLeft.Order - cscRight.Order; 
            }
        } 
 
        private class ComponentComparer : IComparer {
 
            public static readonly ComponentComparer Default = new ComponentComparer();

            private ComponentComparer() {
            } 

            public int Compare(object left, object right) { 
                int n = string.Compare(((IComponent)left).GetType().Name, 
                                      ((IComponent)right).GetType().Name, false, CultureInfo.InvariantCulture);
 
                if (n == 0) {
                    n = string.Compare(((IComponent)left).Site.Name,
                                       ((IComponent)right).Site.Name,
                                          true, CultureInfo.InvariantCulture); 
                }
 
                return n; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
