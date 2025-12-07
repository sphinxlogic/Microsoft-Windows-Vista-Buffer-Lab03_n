 
//------------------------------------------------------------------------------
// <copyright file="ResourceCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.ComponentModel.Design.Serialization { 

    using System;
    using System.CodeDom;
    using System.Collections; 
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Resources; 
    using System.Runtime.Serialization;
 
    /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer"]/*' /> 
    /// <devdoc>
    ///     Code model serializer for resource managers.  This is called 
    ///     in one of two ways.  On Deserialization, we are associated
    ///     with a ResourceManager object.  Instead of creating a
    ///     ResourceManager, however, we create an object called a
    ///     SerializationResourceManager.  This class inherits 
    ///     from ResourceManager, but overrides all of the methods.
    ///     Instead of letting resource manager maintain resource 
    ///     sets, it uses the designer host's IResourceService 
    ///     for this purpose.
    /// 
    ///     During serialization, this class will also create
    ///     a SerializationResourceManager.  This will be added
    ///     to the serialization manager as a service so other
    ///     resource serializers can get at it.  SerializationResourceManager 
    ///     has additional methods on it to support writing data
    ///     into the resource streams for various cultures. 
    /// </devdoc> 
    internal class ResourceCodeDomSerializer : CodeDomSerializer {
 
        private static ResourceCodeDomSerializer defaultSerializer;

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.Default"]/*' />
        /// <devdoc> 
        ///     Retrieves a default static instance of this serializer.
        /// </devdoc> 
        internal new static ResourceCodeDomSerializer Default { 
            get {
                if (defaultSerializer == null) { 
                    defaultSerializer = new ResourceCodeDomSerializer();
                }
                return defaultSerializer;
            } 
        }
 
        public override string GetTargetComponentName(CodeStatement statement, CodeExpression expression, Type type) { 
            string name = null;
 
            CodeExpressionStatement expStatement = statement as CodeExpressionStatement;
            if (expStatement != null) {
                CodeMethodInvokeExpression methodInvokeEx = expStatement.Expression as CodeMethodInvokeExpression;
                if (methodInvokeEx != null) { 
                    CodeMethodReferenceExpression methodReferenceEx = methodInvokeEx.Method as CodeMethodReferenceExpression;
 
                    if (methodReferenceEx != null && 
                        string.Equals(methodReferenceEx.MethodName, "ApplyResources", StringComparison.OrdinalIgnoreCase) &&
                        methodInvokeEx.Parameters.Count > 0) { 

                        // We've found a call to the ApplyResources method on a ComponentResourceManager object.
                        // now we just need to figure out which component ApplyResources is being called for, and
                        // put it into that component's bucket. 
                        CodeFieldReferenceExpression fieldReferenceEx = methodInvokeEx.Parameters[0] as CodeFieldReferenceExpression;
                        CodeVariableReferenceExpression variableReferenceEx = methodInvokeEx.Parameters[0] as CodeVariableReferenceExpression; 
                        if (fieldReferenceEx != null && fieldReferenceEx.TargetObject is CodeThisReferenceExpression) { 
                            name = fieldReferenceEx.FieldName;
                        } 
                        else if (variableReferenceEx != null) {
                            name = variableReferenceEx.VariableName;
                        }
                    } 
                }
            } 
 
            if (string.IsNullOrEmpty(name)) {
                name = base.GetTargetComponentName(statement, expression, type); 
            }

            return name;
        } 

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.ResourceManagerName"]/*' /> 
        /// <devdoc> 
        ///     This is the name of the resource manager object we declare
        ///     on the component surface. 
        /// </devdoc>
        private string ResourceManagerName {
            get {
                return "resources"; 
            }
        } 
 
        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.Deserialize"]/*' />
        /// <devdoc> 
        ///     Deserilizes the given CodeDom object into a real object.  This
        ///     will use the serialization manager to create objects and resolve
        ///     data types.  The root of the object graph is returned.
        /// </devdoc> 
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject) {
            object instance  = null; 
 
            if (manager == null || codeObject == null) {
                throw new ArgumentNullException(manager == null ? "manager" : "codeObject"); 
            }

            using (TraceScope("ResourceCodeDomSerializer::Deserialize")) {
                // What is the code object?  We support an expression, a statement or a collection of statements 
                CodeExpression expression = codeObject as CodeExpression;
 
                if (expression != null) { 
                    instance = DeserializeExpression(manager, null, expression);
                } 
                else {
                    CodeStatementCollection statements = codeObject as CodeStatementCollection;

                    if (statements != null) { 
                        foreach (CodeStatement element in statements) {
                            // Do special parsing of the resources statement 
                            if (element is CodeVariableDeclarationStatement) { 

                                // We create the resource manager ouselves here because it's not just a straight 
                                // parse of the code.
                                //
                                CodeVariableDeclarationStatement statement = (CodeVariableDeclarationStatement)element;
 
                                TraceWarningIf(!statement.Name.Equals(ResourceManagerName), "WARNING: Resource manager serializer being invoked to deserialize a collection we didn't create.");
                                if (statement.Name.Equals(ResourceManagerName)) { 
                                    instance = CreateResourceManager(manager); 
                                }
                            } 
                            else {

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
                    } 
                    else {
                        CodeStatement statement = codeObject as CodeStatement; 
 
                        if (statement == null) {
                            Debug.Fail("ResourceCodeDomSerializer::Deserialize requires a CodeExpression, CodeStatement or CodeStatementCollection to parse"); 

                            string supportedTypes = string.Format(CultureInfo.CurrentCulture, "{0}, {1}, {2}", typeof(CodeExpression).Name, typeof(CodeStatement).Name, typeof(CodeStatementCollection).Name);

                            throw new ArgumentException(SR.GetString(SR.SerializerBadElementTypes, codeObject.GetType().Name, supportedTypes)); 
                        }
                    } 
                } 
            }
 
            return instance;
        }

        private SerializationResourceManager CreateResourceManager(IDesignerSerializationManager manager) { 
            Trace("Variable is our resource manager.  Creating it");
            SerializationResourceManager sm = GetResourceManager(manager); 
 
            TraceWarningIf(sm.DeclarationAdded, "We have already created a resource manager.");
            if (!sm.DeclarationAdded) { 
                sm.DeclarationAdded = true;
                manager.SetName(sm, ResourceManagerName);
            }
 
            return sm;
        } 
 
        /// <devdoc>
        ///    This method is invoked during deserialization to obtain an instance of an object.  When this is called, an instance 
        ///    of the requested type should be returned.  Our implementation provides a design time resource manager.
        /// </devdoc>
        protected override object DeserializeInstance(IDesignerSerializationManager manager, Type type, object[] parameters, string name, bool addToContainer) {
            if (manager == null) throw new ArgumentNullException("manager"); 
            if (type == null) throw new ArgumentNullException("type");
 
            if (name != null && name.Equals(ResourceManagerName) && typeof(ResourceManager).IsAssignableFrom(type)) { 
                return CreateResourceManager(manager);
            } 
            else {
                // if it isn't our special resource manager, just create it.
                return manager.CreateInstance(type, parameters, name, addToContainer);
            } 
        }
 
        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.DeserializeInvariant"]/*' /> 
        /// <devdoc>
        ///     Deserilizes the given CodeDom object into a real object.  This 
        ///     will use the serialization manager to create objects and resolve
        ///     data types.  It uses the invariant resource blob to obtain resources.
        /// </devdoc>
        public object DeserializeInvariant(IDesignerSerializationManager manager, string resourceName) { 
            SerializationResourceManager resources = GetResourceManager(manager);
            return resources.GetObject(resourceName, true); 
        } 

        /// <devdoc> 
        ///     Try to discover the data type we should apply a cast for.  To do this, we
        ///     first search the context stack for an ExpressionContext to decrypt, and if
        ///     we fail that we try the actual object.  If we can't find a cast type we
        ///     return null. 
        /// </devdoc>
        private Type GetCastType(IDesignerSerializationManager manager, object value) { 
 
            // Is there an ExpressionContext we can work with?
            // 
            ExpressionContext tree = (ExpressionContext)manager.Context[typeof(ExpressionContext)];
            if (tree != null) {
                return tree.ExpressionType;
            } 

            // Party on the object, if we can.  It is the best identity we can get. 
            // 
            if (value != null) {
                Type castTo = value.GetType(); 
                while (!castTo.IsPublic && !castTo.IsNestedPublic) {
                    castTo = castTo.BaseType;
                }
                return castTo; 
            }
            // Object is null. Nothing we can do 
            // 
            TraceError("We need to supply a cast, but we cannot determine the cast type.");
            return null; 
        }

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.GetEnumerator"]/*' />
        /// <devdoc> 
        ///     Retrieves a dictionary enumerator for the requested culture, or null if no resources for that culture exist.
        /// </devdoc> 
        public IDictionaryEnumerator GetEnumerator(IDesignerSerializationManager manager, CultureInfo culture) { 
            SerializationResourceManager resources = GetResourceManager(manager);
            return resources.GetEnumerator(culture); 
        }

        /// <devdoc>
        ///     Retrieves a dictionary enumerator for the requested culture, or null if no resources for that culture exist. 
        /// </devdoc>
        public IDictionaryEnumerator GetMetadataEnumerator(IDesignerSerializationManager manager) { 
            SerializationResourceManager resources = GetResourceManager(manager); 
            return resources.GetMetadataEnumerator();
        } 

        /// <devdoc>
        ///     Demand creates the serialization resource manager.  Stores the manager as an appended context value.
        /// </devdoc> 
        private SerializationResourceManager GetResourceManager(IDesignerSerializationManager manager) {
            SerializationResourceManager sm = manager.Context[typeof(SerializationResourceManager)] as SerializationResourceManager; 
            if (sm == null) { 
                sm = new SerializationResourceManager(manager);
                manager.Context.Append(sm); 
            }
            return sm;
        }
 
        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.Serialize"]/*' />
        /// <devdoc> 
        ///     Serializes the given object into a CodeDom object.  This expects the following 
        ///     values to be available on the context stack:
        /// 
        ///         A CodeStatementCollection that we can add our resource declaration to,
        ///         if necessary.
        ///
        ///         An ExpressionContext that contains the property, field or method 
        ///         that is being serialized, along with the object being serialized.
        ///         We need this so we can create a unique resource name for the 
        ///         object. 
        ///
        /// </devdoc> 
        public override object Serialize(IDesignerSerializationManager manager, object value) {
            return Serialize(manager, value, false, false, true);
        }
 
        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.Serialize2"]/*' />
        /// <devdoc> 
        ///     Serializes the given object into a CodeDom object.  This expects the following 
        ///     values to be available on the context stack:
        /// 
        ///         A CodeStatementCollection that we can add our resource declaration to,
        ///         if necessary.
        ///
        ///         An ExpressionContext that contains the property, field or method 
        ///         that is being serialized, along with the object being serialized.
        ///         We need this so we can create a unique resource name for the 
        ///         object. 
        ///
        /// </devdoc> 
        public object Serialize(IDesignerSerializationManager manager, object value, bool shouldSerializeInvariant) {
            return Serialize(manager, value, false, shouldSerializeInvariant, true);
        }
 
        /// <devdoc>
        ///     Serializes the given object into a CodeDom object.  This expects the following 
        ///     values to be available on the context stack: 
        ///
        ///         A CodeStatementCollection that we can add our resource declaration to, 
        ///         if necessary.
        ///
        ///         An ExpressionContext that contains the property, field or method
        ///         that is being serialized, along with the object being serialized. 
        ///         We need this so we can create a unique resource name for the
        ///         object. 
        /// 
        /// </devdoc>
        public object Serialize(IDesignerSerializationManager manager, object value, bool shouldSerializeInvariant, bool ensureInvariant) { 
            return Serialize(manager, value, false, shouldSerializeInvariant, ensureInvariant);
        }

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.Serialize1"]/*' /> 
        /// <devdoc>
        ///     This performs the actual work of serialization between Serialize and SerializeInvariant. 
        /// </devdoc> 
        private object Serialize(IDesignerSerializationManager manager, object value, bool forceInvariant, bool shouldSerializeInvariant, bool ensureInvariant) {
            CodeExpression expression = null; 

            using (TraceScope("ResourceCodeDomSerializer::Serialize")) {
                // Resource serialization is a little inconsistent.  We deserialize our own resource manager
                // creation statement, but we will never be asked to serialize a resource manager, because 
                // it doesn't exist as a product of the design container; it is purely an artifact of
                // serializing.  Some not-so-obvious side effects of this are: 
                // 
                //      This method will never ever be called by the serialization system directly.
                //      There is no attribute or metadata that will invoke it.  Instead, other 
                //      serializers will call this method to see if we should serialize to resources.
                //
                //      We need a way to inject the local variable declaration into the method body
                //      for the resource manager if we actually do emit a resource, which we shove 
                //      into the statements collection.
                SerializationResourceManager sm = GetResourceManager(manager); 
                CodeStatementCollection statements = (CodeStatementCollection)manager.Context[typeof(CodeStatementCollection)]; 

                // If this serialization resource manager has never been used to output 
                // culture-sensitive statements, then we must emit the local variable hookup.  Culture
                // invariant statements are used to save random data that is not representable in code,
                // so there is no need to emit a declaration.
                // 
                if (!forceInvariant) {
                    if (!sm.DeclarationAdded) { 
                        sm.DeclarationAdded = true; 

                        // If we have a root context, then we can write out a reasonable resource manager constructor. 
                        // If not, then we're a bit hobbled because we have to guess at the resource name.
                        RootContext rootCxt = manager.Context[typeof(RootContext)] as RootContext;
                        TraceWarningIf(statements == null, "No CodeStatementCollection on serialization stack, we cannot serialize resource manager creation statements.");
                        if (statements != null) { 
                            CodeExpression[] parameters;
 
                            if (rootCxt != null) { 
                                string baseType = manager.GetName(rootCxt.Value);
 
                                parameters = new CodeExpression[] { new CodeTypeOfExpression(baseType) };
                            }
                            else {
                                TraceWarning("No root context, we can only assume the resource manager resource name."); 
                                parameters = new CodeExpression[] { new CodePrimitiveExpression(ResourceManagerName) };
                            } 
 
                            CodeExpression initExpression = new CodeObjectCreateExpression(typeof(ComponentResourceManager), parameters);
 
                            statements.Add(new CodeVariableDeclarationStatement(typeof(ComponentResourceManager), ResourceManagerName, initExpression));
                            SetExpression(manager, sm, new CodeVariableReferenceExpression(ResourceManagerName));
                            sm.ExpressionAdded = true;
 
                            ComponentCache cache = manager.Context[typeof(ComponentCache)] as ComponentCache;
                            ComponentCache.Entry entry = manager.Context[typeof(ComponentCache.Entry)] as ComponentCache.Entry; 
                        } 
                    }
                    else { 
                        // Check to see if we have an expression for SM yet.  If we have cached the declaration
                        // in the component cache, the expression may not be setup so we should re-apply it.
                        if (!sm.ExpressionAdded) {
                            if (GetExpression(manager, sm) == null) { 
                                SetExpression(manager, sm, new CodeVariableReferenceExpression(ResourceManagerName));
                            } 
                            sm.ExpressionAdded = true; 
                        }
                    } 
                }

                // Retrieve the ExpressionContext on the context stack, and save the value as a resource.
                ExpressionContext tree = (ExpressionContext)manager.Context[typeof(ExpressionContext)]; 

                TraceWarningIf(tree == null, "No ExpressionContext on stack.  We can serialize, but we cannot create a well-formed name."); 
 
                string resourceName = sm.SetValue(manager, tree, value, forceInvariant, shouldSerializeInvariant, ensureInvariant, false);
 
                // Now the next step is to discover the type of the given value.  If it is a string,
                // we will invoke "GetString"  Otherwise, we will invoke "GetObject" and supply a
                // cast to the proper value.
                // 
                bool needCast;
                string methodName; 
 
                if (value is string || (tree != null && tree.ExpressionType == typeof(string))) {
                    needCast = false; 
                    methodName = "GetString";
                }
                else {
                    needCast = true; 
                    methodName = "GetObject";
                } 
 
                // Finally, all we need to do is create a CodeExpression that represents the resource manager
                // method invoke. 
                //
                Trace("Creating method invoke to {0}.{1}", ResourceManagerName, methodName);

                CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(); 

                methodInvoke.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(ResourceManagerName), methodName); 
                methodInvoke.Parameters.Add(new CodePrimitiveExpression(resourceName)); 
                if (needCast) {
                    Type castTo = GetCastType(manager, value); 

                    if (castTo != null) {
                        Trace("Supplying cast to {0}", castTo.Name);
                        expression = new CodeCastExpression(castTo, methodInvoke); 
                    }
                    else { 
                        expression = methodInvoke; 
                    }
                } 
                else {
                    expression = methodInvoke;
                }
            } 

            return expression; 
        } 

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializeInvariant"]/*' /> 
        /// <devdoc>
        ///     Serializes the given object into a CodeDom object saving resources
        ///     in the invariant culture, rather than the current culture.  This expects the following
        ///     values to be available on the context stack: 
        ///
        ///         A CodeStatementCollection that we can add our resource declaration to, 
        ///         if necessary. 
        ///
        ///         An ExpressionContext that contains the property, field or method 
        ///         that is being serialized, along with the object being serialized.
        ///         We need this so we can create a unique resource name for the
        ///         object.
        /// 
        /// </devdoc>
        public object SerializeInvariant(IDesignerSerializationManager manager, object value, bool shouldSerializeValue) { 
            return Serialize(manager, value, true, shouldSerializeValue, true); 
        }
 
        /// <devdoc>
        ///     Writes out the given metadata.
        /// </devdoc>
        public void SerializeMetadata(IDesignerSerializationManager manager, string name, object value, bool shouldSerializeValue) { 
            using (TraceScope("ResourceCodeDomSerializer::SerializeMetadata")) {
                Trace("Name: {0}", name); 
                Trace("Value: {0}", (value == null ? "(null)" : value.ToString())); 

                SerializationResourceManager sm = GetResourceManager(manager); 
                sm.SetMetadata(manager, name, value, shouldSerializeValue, false);
            }
        }
 
        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.WriteResource"]/*' />
        /// <devdoc> 
        ///     Serializes the given resource value into the resource set.  This does not effect 
        ///     the code dom values.  The resource is written into the current culture.
        /// </devdoc> 
        public void WriteResource(IDesignerSerializationManager manager, string name, object value) {
            using (TraceScope("ResourceCodeDomSerializer::WriteResource")) {
                Trace("Name: {0}", name);
                Trace("Value: {0}", (value == null ? "(null)" : value.ToString())); 

                SerializationResourceManager sm = GetResourceManager(manager); 
                sm.SetValue(manager, name, value, false, false, true, false); 
            }
        } 

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.WriteResourceInvariant"]/*' />
        /// <devdoc>
        ///     Serializes the given resource value into the resource set.  This does not effect 
        ///     the code dom values.  The resource is written into the invariant culture.
        /// </devdoc> 
        public void WriteResourceInvariant(IDesignerSerializationManager manager, string name, object value) { 
            using (TraceScope("ResourceCodeDomSerializer::WriteResourceInvariant")) {
                Trace("Name: {0}", name); 
                Trace("Value: {0}", (value == null ? "(null)" : value.ToString()));

                SerializationResourceManager sm = GetResourceManager(manager);
                sm.SetValue(manager, name, value, true, true, true, false); 
            }
        } 
 
        /// <devdoc>
        ///     This is called by the component code dom serializer's caching logic to save cached 
        ///     resource data back into the resx files.
        /// </devdoc>
        internal void ApplyCacheEntry(IDesignerSerializationManager manager, ComponentCache.Entry entry) {
            SerializationResourceManager sm = GetResourceManager(manager); 

            if (entry.Metadata != null) { 
                foreach(ComponentCache.ResourceEntry re in entry.Metadata) { 
                    sm.SetMetadata(manager, re.Name, re.Value, re.ShouldSerializeValue, true);
                } 
            }

            if (entry.Resources != null) {
                foreach(ComponentCache.ResourceEntry re in entry.Resources) { 
                    manager.Context.Push(re.PropertyDescriptor);
                    manager.Context.Push(re.ExpressionContext); 
                    try { 
                        sm.SetValue(manager, re.Name, re.Value, re.ForceInvariant, re.ShouldSerializeValue, re.EnsureInvariant, true);
                    } finally { 
                        Debug.Assert(manager.Context.Current == re.ExpressionContext, "Someone corrupted the context stack");
                        manager.Context.Pop();
                        Debug.Assert(manager.Context.Current == re.PropertyDescriptor, "Someone corrupted the context stack");
                        manager.Context.Pop(); 
                    }
                } 
            } 
        }
 
        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager"]/*' />
        /// <devdoc>
        ///     This is the meat of resource serialization.  This implements
        ///     a resource manager through a host-provided IResourceService 
        ///     interface.  The resource service feeds us with resource
        ///     readers and writers, and we simulate a runtime ResourceManager. 
        ///     There is one instance of this object for the entire serialization 
        ///     process, just like there is one resource manager in runtime
        ///     code.  When an instance of this object is created, it 
        ///     adds itself to the serialization manager's service list,
        ///     and listens for the SerializationComplete event.  When
        ///     serialization is complete, this will close and flush
        ///     any readers or writers it may have opened and will 
        ///     also remove itself from the service list.
        /// </devdoc> 
        private class SerializationResourceManager : ComponentResourceManager { 

            private static object resourceSetSentinel = new object(); 
            private IDesignerSerializationManager   manager;
            private bool                            checkedLocalizationLanguage;
            private CultureInfo                     localizationLanguage;
            private IResourceWriter                 writer; 
            private CultureInfo                     readCulture;
            private Hashtable                       nameTable; 
            private Hashtable                       resourceSets; 
            private Hashtable                       metadata;
            private Hashtable                       mergedMetadata; 
            private object rootComponent;
            private bool                            declarationAdded = false;
            private bool                            expressionAdded = false;
            private Hashtable                       propertyFillAdded; 
            private bool                            invariantCultureResourcesDirty = false;
            private bool                            metadataResourcesDirty = false; 
 

            public SerializationResourceManager(IDesignerSerializationManager manager) { 
                this.manager = manager;
                this.nameTable = new Hashtable();

                // We need to know when we're done so we can push the resource file out. 
                //
                manager.SerializationComplete += new EventHandler(OnSerializationComplete); 
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.DeclarationAdded"]/*' /> 
            /// <devdoc>
            ///     State the serializers use to determine if the declaration
            ///     of this resource manager has been performed.  This is just
            ///     per-document state we keep; we do not actually care about 
            ///     this value.
            /// </devdoc> 
            public bool DeclarationAdded { 
                get {
                    return declarationAdded; 
                }
                set {
                    declarationAdded = value;
                } 
            }
 
            /// <devdoc> 
            ///     When a declaration is added, we also setup an expression other serializers
            ///     can use to reference our resource declaration.  This bit tracks if we 
            ///     have setup this expression yet.  Note that the expression and declaration may
            ///     be added at diffrerent times, if the declaration was added by a cached
            ///     component.
            /// </devdoc> 
            public bool ExpressionAdded {
                get { 
                    return expressionAdded; 
                }
                set { 
                    expressionAdded = value;
                }
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.LocalizationLanguage"]/*' />
            /// <devdoc> 
            ///     The language we should be localizing into. 
            /// </devdoc>
            private CultureInfo LocalizationLanguage { 
                get {
                    if (!checkedLocalizationLanguage) {
                        // Check to see if our base component's localizable prop is true
                        RootContext rootCxt = manager.Context[typeof(RootContext)] as RootContext; 
                        if (rootCxt != null) {
                            object comp = rootCxt.Value; 
                            PropertyDescriptor prop = TypeDescriptor.GetProperties(comp)["LoadLanguage"]; 
                            if (prop != null && prop.PropertyType == typeof(CultureInfo)) {
                                localizationLanguage = (CultureInfo)prop.GetValue(comp); 
                            }
                        }
                        checkedLocalizationLanguage = true;
                    } 
                    return localizationLanguage;
                } 
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.ReadCulture"]/*' /> 
            /// <devdoc>
            ///     This is the culture info we should use to read and write resources.  We always write
            ///     using the same culture we read with so we don't stomp on data.
            /// </devdoc> 
            private CultureInfo ReadCulture {
                get { 
                    if (readCulture == null) { 
                        CultureInfo locCulture = LocalizationLanguage;
                        if (locCulture != null) { 
                            readCulture = locCulture;
                        }
                        else {
                            readCulture = CultureInfo.InvariantCulture; 
                        }
                    } 
 
                    return readCulture;
                } 
            }

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.ResourceTable"]/*' />
            /// <devdoc> 
            ///     Returns a hash table where we shove resource sets.
            /// </devdoc> 
            private Hashtable ResourceTable { 
                get {
                    if (resourceSets == null) { 
                        resourceSets = new Hashtable();
                    }
                    return resourceSets;
                } 
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.RootComponent"]/*' /> 
            /// <devdoc>
            ///     Retrieves the root component we're designing. 
            /// </devdoc>
            private object RootComponent {
                get {
                    if (rootComponent == null) { 
                        RootContext rootCxt = manager.Context[typeof(RootContext)] as RootContext;
                        if (rootCxt != null) { 
                            rootComponent = rootCxt.Value; 
                        }
                    } 
                    return rootComponent;
                }
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.Writer"]/*' />
            /// <devdoc> 
            ///     Retrieves a resource writer we should write into. 
            /// </devdoc>
            private IResourceWriter Writer { 
                get {
                    if (writer == null) {
                        IResourceService rs = (IResourceService)manager.GetService(typeof(IResourceService));
 
                        if (rs != null) {
 
                            // We always write with the culture we read with.  In the event of a language change 
                            // during localization, we will write the new language to the source code and then
                            // perform a reload. 
                            //
                            writer = rs.GetResourceWriter(ReadCulture);
                        }
                        else { 

                            // No resource service, so there is no way to create a resource writer for the 
                            // object.  In this case we just create an empty one so the resources go into 
                            // the bit-bucket.
                            // 
                            Debug.Fail("We expected to get IResourceService -- no resource serialization will be available");
                            writer = new ResourceWriter(new MemoryStream());
                        }
                    } 
                    return writer;
                } 
            } 

            /// <devdoc> 
            ///     The component serializer supports caching serialized outputs for speed.  It holds both a collection of statements as well
            ///     as an opaque blob for resources.  This function adds data to that blob.  The parameters to this function are the
            ///     same as those to SetValue, or SetMetadata (when isMetadata is true).
            /// </devdoc> 
            private void AddCacheEntry(IDesignerSerializationManager manager, string name, object value, bool isMetadata, bool forceInvariant, bool shouldSerializeValue, bool ensureInvariant) {
                ComponentCache.Entry entry = manager.Context[typeof(ComponentCache.Entry)] as ComponentCache.Entry; 
                if (entry != null) { 
                    ComponentCache.ResourceEntry re = new ComponentCache.ResourceEntry();
                    re.Name = name; 
                    re.Value = value;
                    re.ForceInvariant = forceInvariant;
                    re.ShouldSerializeValue = shouldSerializeValue;
                    re.EnsureInvariant = ensureInvariant; 
                    re.PropertyDescriptor = (PropertyDescriptor)manager.Context[typeof(PropertyDescriptor)];
                    re.ExpressionContext = (ExpressionContext)manager.Context[typeof(ExpressionContext)]; 
 
                    if (isMetadata) {
                        entry.AddMetadata(re); 
                    }
                    else {
                        entry.AddResource(re);
                    } 
                }
            } 
 
            /// <devdoc>
            ///     Returns true if the caller should add a property fill statement 
            ///     for the given object.  A property fill is required for the
            ///     component only once, so this remembers the value.
            /// </devdoc>
            public bool AddPropertyFill(object value) { 
                bool added = false;
                if (propertyFillAdded == null) { 
                    propertyFillAdded = new Hashtable(); 
                }
                else { 
                    added = propertyFillAdded.ContainsKey(value);
                }
                if (!added) {
                    propertyFillAdded[value] = value; 
                }
                return !added; 
            } 

            /// <devdoc> 
            ///     This method examines all the resources for the provided culture.
            ///     When it finds a resource with a key in the format of
            ///     &quot;[objectName].[property name]&quot; it will apply that resources value
            ///     to the corresponding property on the object. 
            /// </devdoc>
            public override void ApplyResources(object value, string objectName, CultureInfo culture) { 
 
                if (culture == null) {
                    culture = ReadCulture; 
                }

                base.ApplyResources(value, objectName, culture);
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.CompareWithParentValue"]/*' /> 
            /// <devdoc> 
            ///     This determines if the given resource name/value pair can be retrieved
            ///     from a parent culture.  We don't want to write duplicate resources for 
            ///     each language, so we do a check of the parent culture.
            /// </devdoc>
            private CompareValue CompareWithParentValue(string name, object value) {
                Debug.Assert(name != null, "name is null"); 

                // If there is no parent culture, treat that as being different from the parent's resource, 
                // which results in the "normal" code path for the caller. 
                if (ReadCulture.Equals(CultureInfo.InvariantCulture))
                    return CompareValue.Different; 

                CultureInfo culture = ReadCulture;

                for (;;) { 
                    Debug.Assert(culture.Parent != culture, "should have exited loop when culture = InvariantCulture");
                    culture = culture.Parent; 
 
                    Hashtable rs = GetResourceSet(culture);
 
                    bool contains = (rs == null) ? false : rs.ContainsKey(name);

                    if (contains) {
                        object parentValue = (rs != null) ? rs[name] : null; 

                        if (parentValue == value) { 
                            return CompareValue.Same; 
                        }
                        else if (parentValue != null) { 
                            if (parentValue.Equals(value))
                                return CompareValue.Same;
                            else
                                return CompareValue.Different; 
                        }
                        else { 
                            return CompareValue.Different; 
                        }
                    } 
                    else if (culture.Equals(CultureInfo.InvariantCulture)) {
                        return CompareValue.New;
                    }
                } 
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.CreateResourceSet"]/*' /> 
            /// <devdoc>
            ///     Creates a resource set hashtable for the given resource 
            ///     reader.
            /// </devdoc>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            private Hashtable CreateResourceSet(IResourceReader reader, CultureInfo culture) { 
                Hashtable result = new Hashtable();
 
                // We need to guard against bad or unloadable resources.  We warn the user in the task 
                // list here, but we will still load the designer.
                // 
                try {
                    IDictionaryEnumerator resEnum = reader.GetEnumerator();
                    while (resEnum.MoveNext()) {
                        string name = (string)resEnum.Key; 
                        object value = resEnum.Value;
                        result[name] = value; 
                    } 
                }
                catch (Exception e) { 
                    string message = e.Message;
                    if (message == null || message.Length == 0) {
                        message = e.GetType().Name;
                    } 

                    Exception se; 
 
                    if (culture == CultureInfo.InvariantCulture) {
                        se = new SerializationException(SR.GetString(SR.SerializerResourceExceptionInvariant, message), e); 
                    }
                    else {
                        se = new SerializationException(SR.GetString(SR.SerializerResourceException, culture.ToString(), message), e);
                    } 

                    manager.ReportError(se); 
                } 

                return result; 
            }

            /// <devdoc>
            ///     This returns a dictionary enumerator for metadata on the invariant culture.  If no metadata 
            ///    can be found this will return null..
            /// </devdoc> 
            public IDictionaryEnumerator GetMetadataEnumerator() { 
                if (mergedMetadata == null)
                { 
 	                Hashtable t = GetMetadata();
	                if (t != null) {

		                // This is for backwards compatibility and also for the case when our reader/writer 
		                // don't support metadata.  We must merge the original enumeration data in here or
 		                // else existing design time properties won't show up.  That would be really 
		                // bad for things like Localizable. 

 		                Hashtable it = GetResourceSet(CultureInfo.InvariantCulture); 
 		                if (it != null) {
			                foreach(DictionaryEntry de in it) {
 				                if (!t.ContainsKey(de.Key)) {
					                t.Add(de.Key, de.Value); 
				                }
			                } 
 		                } 
		                mergedMetadata = t;
 	                } 
                }
                if (mergedMetadata != null)
                {  	
 	                return mergedMetadata.GetEnumerator(); 
                }
                return null; 
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.GetEnumerator"]/*' /> 
            /// <devdoc>
            ///     This returns a dictionary enumerator for the given culture.  If no such resource file exists for the culture this
            ///     will return null.
            /// </devdoc> 
            public IDictionaryEnumerator GetEnumerator(CultureInfo culture) {
                Hashtable ht = GetResourceSet(culture); 
                if (ht != null) { 
                    return ht.GetEnumerator();
                } 

                return null;
            }
 
            /// <devdoc>
            ///     Loads the metadata table 
            /// </devdoc> 
            private Hashtable GetMetadata() {
                if (metadata == null) { 
                    IResourceService resSvc = (IResourceService)manager.GetService(typeof(IResourceService));

                    if (resSvc != null) {
                        IResourceReader reader = resSvc.GetResourceReader(CultureInfo.InvariantCulture); 

                        if (reader != null) { 
                            try { 
                                ResXResourceReader resxReader = reader as ResXResourceReader;
                                if (resxReader != null) { 
                                    metadata = new Hashtable();
                                    IDictionaryEnumerator de = resxReader.GetMetadataEnumerator();
                                    while (de.MoveNext()) {
                                        metadata[de.Key] = de.Value; 
                                    }
                                } 
                            } 

                            finally { 
                                reader.Close();
                            }
                        }
                    } 
                }
                return metadata; 
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.GetObject"]/*' /> 
            /// <devdoc>
            ///     Overrides ResourceManager.GetObject to return the requested
            ///     object.  Returns null if the object couldn't be found.
            /// </devdoc> 
            public override object GetObject(string resourceName) {
                return GetObject(resourceName, false); 
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.GetObject1"]/*' /> 
            /// <devdoc>
            ///     Retrieves the object of the given name from our resource bundle.
            ///     If forceInvariant is true, this will always use the invariant
            ///     resource, rather than using the current language. 
            ///     Returns null if the object couldn't be found.
            /// </devdoc> 
            public object GetObject(string resourceName, bool forceInvariant) { 

                Debug.Assert(manager != null, "This resource manager object has been destroyed."); 

                // We fetch the read culture if someone asks for a
                // culture-sensitive string.  If forceInvariant is set, we always
                // use the invariant culture. 
                //
                CultureInfo culture; 
 
                if (forceInvariant) {
                    culture = CultureInfo.InvariantCulture; 
                }
                else {
                    culture = ReadCulture;
                } 

                object value = null; 
 
                while (value == null) {
                    Hashtable rs = GetResourceSet(culture); 

                    if (rs != null) {
                        value = rs[resourceName];
                    } 

                    CultureInfo lastCulture = culture; 
                    culture = culture.Parent; 
                    if (lastCulture.Equals(culture)) {
                        break; 
                    }
                }

                return value; 
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.GetResourceSet"]/*' /> 
            /// <devdoc>
            ///     Looks up the resource set in the resourceSets hash table, loading the set if it hasn't been loaded already. 
            ///     Returns null if no resource that exists for that culture.
            /// </devdoc>
            private Hashtable GetResourceSet(CultureInfo culture) {
                Debug.Assert(culture != null, "null parameter"); 
                Hashtable rs = null;
                object objRs = ResourceTable[culture]; 
                if (objRs == null) { 
                    IResourceService resSvc = (IResourceService)manager.GetService(typeof(IResourceService));
 
                    TraceErrorIf(resSvc == null, "IResourceService is not available.  We will not be able to load resources.");
                    if (resSvc != null) {
                        IResourceReader reader = resSvc.GetResourceReader(culture);
                        if (reader != null) { 
                            try {
                                rs = CreateResourceSet(reader, culture); 
                            } 
                            finally {
                                reader.Close(); 
                            }
                            ResourceTable[culture] = rs;
                        }
                        else { 

                            // Provide a sentinel so we don't repeatedly ask 
                            // for the same resource.  If this is the invariant 
                            // culture, always provide one.
                            // 
                            if (culture.Equals(CultureInfo.InvariantCulture)) {
                                rs = new Hashtable();
                                ResourceTable[culture] = rs;
                            } 
                            else {
                                ResourceTable[culture] = resourceSetSentinel; 
                            } 
                        }
                    } 
                }
                else {
                    rs = objRs as Hashtable;
                    if (rs == null) { 
                        // the resourceSets hash table may contain our "this" pointer as a sentinel value
                        Debug.Assert(objRs == resourceSetSentinel, "unknown object in resourceSets: " + objRs); 
                    } 
                }
 
                return rs;
            }

            /// <devdoc> 
            ///     Override of GetResourceSet from ResourceManager.
            /// </devdoc> 
            public override ResourceSet GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents) { 

                if (culture == null) { 
                    throw new ArgumentNullException("culture");
                }

                CultureInfo lastCulture = culture; 

                do { 
                    Hashtable ht = GetResourceSet(culture); 
                    if (ht != null) {
                        return new CodeDomResourceSet(ht); 
                    }

                    lastCulture = culture;
                    culture = culture.Parent; 

                } while (tryParents && !lastCulture.Equals(culture)); 
 
                if (createIfNotExists) {
                    return new CodeDomResourceSet(); 
                }

                return null;
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.GetString"]/*' /> 
            /// <devdoc> 
            ///     Overrides ResourceManager.GetString to return the requested
            ///     string.  Returns null if the string couldn't be found. 
            /// </devdoc>
            public override string GetString(string resourceName) {
                return GetObject(resourceName, false) as string;
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.OnSerializationComplete"]/*' /> 
            /// <devdoc> 
            ///     Event handler that gets called when serialization or deserialization
            ///     is complete. Here we need to write any resources to disk.  Sine 
            ///     we open resources for write on demand, this code handles the case
            ///     of reading resources as well.
            /// </devdoc>
            private void OnSerializationComplete(object sender, EventArgs e) { 
                // Commit any changes we have made.
                // 
                if (writer != null) { 
                    writer.Close();
                    writer = null; 
                }

                if (invariantCultureResourcesDirty || metadataResourcesDirty) {
 
                    IResourceService service = (IResourceService)manager.GetService(typeof(IResourceService));
                    if (service != null) { 
                        IResourceWriter invariantWriter = service.GetResourceWriter(CultureInfo.InvariantCulture); 

                        Debug.Assert(invariantWriter != null, "GetResourceWriter returned null for the InvariantCulture"); 

                        try {
                            // Do the invariant resources first
                            Debug.Assert(!ReadCulture.Equals(CultureInfo.InvariantCulture), "invariantCultureResourcesDirty should only come into play when readCulture != CultureInfo.InvariantCulture; check that CompareWithParentValue is correct"); 

                            object objRs = ResourceTable[CultureInfo.InvariantCulture]; 
 
                            Debug.Assert(objRs != null && objRs is Hashtable, "ResourceSet for the InvariantCulture not loaded, but it's considered dirty?");
 
                            Hashtable resourceSet = (Hashtable)objRs;

                            // Dump the hash table to the resource writer
                            // 
                            IDictionaryEnumerator resEnum = resourceSet.GetEnumerator();
 
                            while (resEnum.MoveNext()) { 
                                string name = (string)resEnum.Key;
                                object value = resEnum.Value; 

                                invariantWriter.AddResource(name, value);
                            }
 
                            invariantCultureResourcesDirty = false;
 
 
                            // Followed by the metadata.
                            Debug.Assert(metadata != null, "No metadata, but it's dirty?"); 

                            ResXResourceWriter resxWriter = invariantWriter as ResXResourceWriter;

                            if (resxWriter != null) { 
                                foreach (DictionaryEntry de in metadata) {
                                    resxWriter.AddMetadata((string)de.Key, de.Value); 
                                } 
                            }
                            else { 
                                Debug.Fail("Metadata not supported, but it's dirty?");
                            }

                            metadataResourcesDirty = false; 
                        }
                        finally { 
                            invariantWriter.Close(); 
                        }
                    } 
                    else {
                        Debug.Fail("Couldn't find IResourceService");
                        invariantCultureResourcesDirty = false;
                        metadataResourcesDirty = false; 
                    }
                } 
            } 

            /// <devdoc> 
            ///     Writes a metadata tag to the resource, or writes a normal
            ///     tag if the resource writer doesn't support metadata.
            /// </devdoc>
            public void SetMetadata(IDesignerSerializationManager manager, string resourceName, object value, bool shouldSerializeValue, bool applyingCachedResources) { 

                if (value != null && (!value.GetType().IsSerializable)) { 
                    Debug.Fail("Cannot save a non-serializable value into resources.  Add serializable to " + (value == null ? "(null)" : value.GetType().Name)); 
                    return;
                } 

                // If we are currently the invariant culture then we may be able to
                // write directly.
                if (ReadCulture.Equals(CultureInfo.InvariantCulture)) { 
                    ResXResourceWriter resxWriter = Writer as ResXResourceWriter;
                    if (shouldSerializeValue) { 
                        if (resxWriter != null) { 
                            resxWriter.AddMetadata(resourceName, value);
                        } 
                        else {
                            Writer.AddResource(resourceName, value);
                        }
                    } 
                }
                else { 
                    Hashtable t = null; 

                    // Check if the invariant writer supports metadata. If not, we need to push metadata 
                    // as regular data.
                    IResourceWriter invariantWriter = null;
                    IResourceService service = (IResourceService)manager.GetService(typeof(IResourceService));
                    if (service != null) { 
                        invariantWriter = service.GetResourceWriter(CultureInfo.InvariantCulture);
                    } 
 
                    Hashtable invariant = GetResourceSet(CultureInfo.InvariantCulture);
 
                    if (invariantWriter == null || invariantWriter is ResXResourceWriter) {
                        t = GetMetadata();
                        if (t == null) {
                            metadata = new Hashtable(); 
                            t = metadata;
                        } 
 
                        // Note that when we read metadata, for backwards compatibility, we also merge in regular data
                        // from the invariant resource. We need to clear that data here, since we are going to write 
                        // out metadata separately.
                        if (invariant.ContainsKey(resourceName)) {
                            invariant.Remove(resourceName);
                        } 

                        metadataResourcesDirty = true; 
                    } 
                    else {
                        t = invariant; 

                        invariantCultureResourcesDirty = true;
                    }
 
                    Debug.Assert(t != null, "Don't know where to push metadata.");
 
                    if (t != null) { 
                        if (shouldSerializeValue) {
                            t[resourceName] = value; 
                        }
                        else {
                            t.Remove(resourceName);
                        } 
                    }
                    mergedMetadata = null; 
                } 

                // Update the component cache, if we have one active 

                if (!applyingCachedResources) {
                    AddCacheEntry(manager, resourceName, value, true, false, shouldSerializeValue, false);
                } 
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.SetValue"]/*' /> 
            /// <devdoc>
            ///     Writes the given resource value under the given name. 
            ///     This checks the parent resource to see if the values are the
            ///     same.  If they are, the resource is not written.  If not, then
            ///     the resource is written.  We always write using the resource language
            ///     we read in with, so we don't stomp on the wrong resource data in the 
            ///     event that someone changes the language.
            /// </devdoc> 
            public void SetValue(IDesignerSerializationManager manager, string resourceName, object value, bool forceInvariant, bool shouldSerializeInvariant, bool ensureInvariant, bool applyingCachedResources) { 

                // Values we are going to serialize must be serializable or else 
                // the resource writer will fail when we close it.
                //
                if (value != null && (!value.GetType().IsSerializable)) {
                    Debug.Fail("Cannot save a non-serializable value into resources.  Add serializable to " + (value == null ? "(null)" : value.GetType().Name)); 
                    return;
                } 
 
                if (forceInvariant) {
                    if (ReadCulture.Equals(CultureInfo.InvariantCulture)) { 
                        if (shouldSerializeInvariant) {
                            Writer.AddResource(resourceName, value);
                        }
                    } 
                    else {
                        Hashtable resourceSet = GetResourceSet(CultureInfo.InvariantCulture); 
 
                        Debug.Assert(resourceSet != null, "No ResourceSet for the InvariantCulture?");
 
                        if (shouldSerializeInvariant) {
                            resourceSet[resourceName] = value;
                        }
                        else { 
                            resourceSet.Remove(resourceName);
                        } 
 
                        invariantCultureResourcesDirty = true;
                    } 
                }
                else {
                    CompareValue comparison = CompareWithParentValue(resourceName, value);
                    switch (comparison) { 
                        case CompareValue.Same:
                            // don't add to any resource set 
                            break; 

                        case CompareValue.Different: 
                            Writer.AddResource(resourceName, value);
                            break;

                        case CompareValue.New: 

                            if (ensureInvariant) { 
                                // Add resource to InvariantCulture 
                                Debug.Assert(!ReadCulture.Equals(CultureInfo.InvariantCulture), "invariantCultureResourcesDirty should only come into play when readCulture != CultureInfo.InvariantCulture; check that CompareWithParentValue is correct");
 
                                Hashtable resourceSet = GetResourceSet(CultureInfo.InvariantCulture);

                                Debug.Assert(resourceSet != null, "No ResourceSet for the InvariantCulture?");
                                resourceSet[resourceName] = value; 
                                invariantCultureResourcesDirty = true;
                                Writer.AddResource(resourceName, value); 
                            } 
                            else {
                                // This is a new value.  We want to write it out, PROVIDED 
                                // that the value is not associated with a property that is currently
                                // returning false from ShouldSerializeValue.  This allows us to skip writing out
                                // Font == NULL on all non-invariant cultures, but still allow us to
                                // write out the value if the user is resetting a font back to null. 
                                // If we cannot associate the value with a property we will write
                                // it out just to be safe. 
                                // 
                                // In addition, we need to handle the case of the user adding a new
                                // component to the non-invariant language.  This would be bad, because 
                                // when he/she moved back to the invariant language the component's properties
                                // would all be defaults.  In order to minimize this problem, but still allow
                                // holes in the invariant resx, we also check to see if the property can
                                // be reset.  If it cannot be reset, that means that it has no meaningful 
                                // default. Therefore, it should have appeared in the invariant resx and its
                                // absence indicates a new component. 
                                // 
                                bool writeValue = true;
                                bool writeInvariant = false; 
                                PropertyDescriptor prop = (PropertyDescriptor)manager.Context[typeof(PropertyDescriptor)];

                                if (prop != null) {
                                    ExpressionContext tree = (ExpressionContext)manager.Context[typeof(ExpressionContext)]; 

                                    if (tree != null && tree.Expression is CodePropertyReferenceExpression) { 
                                        writeValue = prop.ShouldSerializeValue(tree.Owner); 
                                        writeInvariant = !prop.CanResetValue(tree.Owner);
                                    } 
                                }

                                if (writeValue) {
                                    Writer.AddResource(resourceName, value); 
                                    if (writeInvariant) {
                                        // Add resource to InvariantCulture 
                                        Debug.Assert(!ReadCulture.Equals(CultureInfo.InvariantCulture), "invariantCultureResourcesDirty should only come into play when readCulture != CultureInfo.InvariantCulture; check that CompareWithParentValue is correct"); 

                                        Hashtable resourceSet = GetResourceSet(CultureInfo.InvariantCulture); 

                                        Debug.Assert(resourceSet != null, "No ResourceSet for the InvariantCulture?");
                                        resourceSet[resourceName] = value;
                                        invariantCultureResourcesDirty = true; 
                                    }
                                } 
                            } 

                            break; 

                        default:
                            Debug.Fail("Unknown CompareValue " + comparison);
                            break; 
                    }
                } 
 
                // Update the component cache, if we have one active.  We don't have to be fancy here
                // because updating this cache just indicates that code in the component cache will later 
                // call us to re-apply the resources, and our logic above will be called again.

                if (!applyingCachedResources) {
                    AddCacheEntry(manager, resourceName, value, false, forceInvariant, shouldSerializeInvariant, ensureInvariant); 
                }
            } 
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.SetValue1"]/*' />
            /// <devdoc> 
            ///     Writes the given resource value under the given name.
            ///     This checks the parent resource to see if the values are the
            ///     same.  If they are, the resource is not written.  If not, then
            ///     the resource is written.  We always write using the resource language 
            ///     we read in with, so we don't stomp on the wrong resource data in the
            ///     event that someone changes the language. 
            /// </devdoc> 
            public string SetValue(IDesignerSerializationManager manager, ExpressionContext tree, object value, bool forceInvariant, bool shouldSerializeInvariant, bool ensureInvariant, bool applyingCachedResources) {
                string nameBase = null; 
                bool appendCount = false;

                if (tree != null) {
                    if (tree.Owner == RootComponent) { 
                        nameBase = "$this";
                    } 
                    else { 
                        nameBase = manager.GetName(tree.Owner);
 
                        if (nameBase == null) {
                            IReferenceService referenceService = (IReferenceService)manager.GetService(typeof(IReferenceService));
                            if (referenceService != null) {
                                nameBase = referenceService.GetName(tree.Owner); 
                            }
                        } 
                    } 
                    CodeExpression expression = tree.Expression;
 
                    string expressionName;

                    if (expression is CodePropertyReferenceExpression) {
                        expressionName = ((CodePropertyReferenceExpression)expression).PropertyName; 
                    }
                    else if (expression is CodeFieldReferenceExpression) { 
                        expressionName = ((CodeFieldReferenceExpression)expression).FieldName; 
                    }
                    else if (expression is CodeMethodReferenceExpression) { 
                        expressionName = ((CodeMethodReferenceExpression)expression).MethodName;
                        if (expressionName.StartsWith("Set")) {
                            expressionName = expressionName.Substring(3);
                        } 
                    }
                    else { 
                        expressionName = null; 
                    }
 
                    if (nameBase == null) {
                        nameBase = "resource";
                    }
 
                    if (expressionName != null) {
                        nameBase += "." + expressionName; 
                    } 
                }
                else { 
                    nameBase = "resource";
                    appendCount = true;
                }
 
                // Now find an unused name
                // 
                string resourceName = nameBase; 
                int count = 1;
 
                for(;;) {
                    if (appendCount) {
                        resourceName = nameBase + count.ToString(CultureInfo.InvariantCulture);
                        count++; 
                    }
                    else { 
                        appendCount = true; 
                    }
 
                    if (!nameTable.ContainsKey(resourceName)) {
                        break;
                    }
                } 

                // Now that we have a name, write out the resource. 
                // 
                SetValue(manager, resourceName, value, forceInvariant, shouldSerializeInvariant, ensureInvariant, applyingCachedResources);
 
                nameTable[resourceName] = resourceName;
                return resourceName;
            }
 
            private class CodeDomResourceSet : ResourceSet {
 
                public CodeDomResourceSet() { 
                }
 
                public CodeDomResourceSet(Hashtable resources) {
                    Table = resources;
                }
            } 

            private enum CompareValue { 
                Same, // parent value == child value 
                Different, // parent value exists, but != child value
                New, // parent value does not exist 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="ResourceCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.ComponentModel.Design.Serialization { 

    using System;
    using System.CodeDom;
    using System.Collections; 
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Resources; 
    using System.Runtime.Serialization;
 
    /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer"]/*' /> 
    /// <devdoc>
    ///     Code model serializer for resource managers.  This is called 
    ///     in one of two ways.  On Deserialization, we are associated
    ///     with a ResourceManager object.  Instead of creating a
    ///     ResourceManager, however, we create an object called a
    ///     SerializationResourceManager.  This class inherits 
    ///     from ResourceManager, but overrides all of the methods.
    ///     Instead of letting resource manager maintain resource 
    ///     sets, it uses the designer host's IResourceService 
    ///     for this purpose.
    /// 
    ///     During serialization, this class will also create
    ///     a SerializationResourceManager.  This will be added
    ///     to the serialization manager as a service so other
    ///     resource serializers can get at it.  SerializationResourceManager 
    ///     has additional methods on it to support writing data
    ///     into the resource streams for various cultures. 
    /// </devdoc> 
    internal class ResourceCodeDomSerializer : CodeDomSerializer {
 
        private static ResourceCodeDomSerializer defaultSerializer;

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.Default"]/*' />
        /// <devdoc> 
        ///     Retrieves a default static instance of this serializer.
        /// </devdoc> 
        internal new static ResourceCodeDomSerializer Default { 
            get {
                if (defaultSerializer == null) { 
                    defaultSerializer = new ResourceCodeDomSerializer();
                }
                return defaultSerializer;
            } 
        }
 
        public override string GetTargetComponentName(CodeStatement statement, CodeExpression expression, Type type) { 
            string name = null;
 
            CodeExpressionStatement expStatement = statement as CodeExpressionStatement;
            if (expStatement != null) {
                CodeMethodInvokeExpression methodInvokeEx = expStatement.Expression as CodeMethodInvokeExpression;
                if (methodInvokeEx != null) { 
                    CodeMethodReferenceExpression methodReferenceEx = methodInvokeEx.Method as CodeMethodReferenceExpression;
 
                    if (methodReferenceEx != null && 
                        string.Equals(methodReferenceEx.MethodName, "ApplyResources", StringComparison.OrdinalIgnoreCase) &&
                        methodInvokeEx.Parameters.Count > 0) { 

                        // We've found a call to the ApplyResources method on a ComponentResourceManager object.
                        // now we just need to figure out which component ApplyResources is being called for, and
                        // put it into that component's bucket. 
                        CodeFieldReferenceExpression fieldReferenceEx = methodInvokeEx.Parameters[0] as CodeFieldReferenceExpression;
                        CodeVariableReferenceExpression variableReferenceEx = methodInvokeEx.Parameters[0] as CodeVariableReferenceExpression; 
                        if (fieldReferenceEx != null && fieldReferenceEx.TargetObject is CodeThisReferenceExpression) { 
                            name = fieldReferenceEx.FieldName;
                        } 
                        else if (variableReferenceEx != null) {
                            name = variableReferenceEx.VariableName;
                        }
                    } 
                }
            } 
 
            if (string.IsNullOrEmpty(name)) {
                name = base.GetTargetComponentName(statement, expression, type); 
            }

            return name;
        } 

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.ResourceManagerName"]/*' /> 
        /// <devdoc> 
        ///     This is the name of the resource manager object we declare
        ///     on the component surface. 
        /// </devdoc>
        private string ResourceManagerName {
            get {
                return "resources"; 
            }
        } 
 
        /// <include file='doc\CodeDomSerializer.uex' path='docs/doc[@for="CodeDomSerializer.Deserialize"]/*' />
        /// <devdoc> 
        ///     Deserilizes the given CodeDom object into a real object.  This
        ///     will use the serialization manager to create objects and resolve
        ///     data types.  The root of the object graph is returned.
        /// </devdoc> 
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject) {
            object instance  = null; 
 
            if (manager == null || codeObject == null) {
                throw new ArgumentNullException(manager == null ? "manager" : "codeObject"); 
            }

            using (TraceScope("ResourceCodeDomSerializer::Deserialize")) {
                // What is the code object?  We support an expression, a statement or a collection of statements 
                CodeExpression expression = codeObject as CodeExpression;
 
                if (expression != null) { 
                    instance = DeserializeExpression(manager, null, expression);
                } 
                else {
                    CodeStatementCollection statements = codeObject as CodeStatementCollection;

                    if (statements != null) { 
                        foreach (CodeStatement element in statements) {
                            // Do special parsing of the resources statement 
                            if (element is CodeVariableDeclarationStatement) { 

                                // We create the resource manager ouselves here because it's not just a straight 
                                // parse of the code.
                                //
                                CodeVariableDeclarationStatement statement = (CodeVariableDeclarationStatement)element;
 
                                TraceWarningIf(!statement.Name.Equals(ResourceManagerName), "WARNING: Resource manager serializer being invoked to deserialize a collection we didn't create.");
                                if (statement.Name.Equals(ResourceManagerName)) { 
                                    instance = CreateResourceManager(manager); 
                                }
                            } 
                            else {

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
                    } 
                    else {
                        CodeStatement statement = codeObject as CodeStatement; 
 
                        if (statement == null) {
                            Debug.Fail("ResourceCodeDomSerializer::Deserialize requires a CodeExpression, CodeStatement or CodeStatementCollection to parse"); 

                            string supportedTypes = string.Format(CultureInfo.CurrentCulture, "{0}, {1}, {2}", typeof(CodeExpression).Name, typeof(CodeStatement).Name, typeof(CodeStatementCollection).Name);

                            throw new ArgumentException(SR.GetString(SR.SerializerBadElementTypes, codeObject.GetType().Name, supportedTypes)); 
                        }
                    } 
                } 
            }
 
            return instance;
        }

        private SerializationResourceManager CreateResourceManager(IDesignerSerializationManager manager) { 
            Trace("Variable is our resource manager.  Creating it");
            SerializationResourceManager sm = GetResourceManager(manager); 
 
            TraceWarningIf(sm.DeclarationAdded, "We have already created a resource manager.");
            if (!sm.DeclarationAdded) { 
                sm.DeclarationAdded = true;
                manager.SetName(sm, ResourceManagerName);
            }
 
            return sm;
        } 
 
        /// <devdoc>
        ///    This method is invoked during deserialization to obtain an instance of an object.  When this is called, an instance 
        ///    of the requested type should be returned.  Our implementation provides a design time resource manager.
        /// </devdoc>
        protected override object DeserializeInstance(IDesignerSerializationManager manager, Type type, object[] parameters, string name, bool addToContainer) {
            if (manager == null) throw new ArgumentNullException("manager"); 
            if (type == null) throw new ArgumentNullException("type");
 
            if (name != null && name.Equals(ResourceManagerName) && typeof(ResourceManager).IsAssignableFrom(type)) { 
                return CreateResourceManager(manager);
            } 
            else {
                // if it isn't our special resource manager, just create it.
                return manager.CreateInstance(type, parameters, name, addToContainer);
            } 
        }
 
        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.DeserializeInvariant"]/*' /> 
        /// <devdoc>
        ///     Deserilizes the given CodeDom object into a real object.  This 
        ///     will use the serialization manager to create objects and resolve
        ///     data types.  It uses the invariant resource blob to obtain resources.
        /// </devdoc>
        public object DeserializeInvariant(IDesignerSerializationManager manager, string resourceName) { 
            SerializationResourceManager resources = GetResourceManager(manager);
            return resources.GetObject(resourceName, true); 
        } 

        /// <devdoc> 
        ///     Try to discover the data type we should apply a cast for.  To do this, we
        ///     first search the context stack for an ExpressionContext to decrypt, and if
        ///     we fail that we try the actual object.  If we can't find a cast type we
        ///     return null. 
        /// </devdoc>
        private Type GetCastType(IDesignerSerializationManager manager, object value) { 
 
            // Is there an ExpressionContext we can work with?
            // 
            ExpressionContext tree = (ExpressionContext)manager.Context[typeof(ExpressionContext)];
            if (tree != null) {
                return tree.ExpressionType;
            } 

            // Party on the object, if we can.  It is the best identity we can get. 
            // 
            if (value != null) {
                Type castTo = value.GetType(); 
                while (!castTo.IsPublic && !castTo.IsNestedPublic) {
                    castTo = castTo.BaseType;
                }
                return castTo; 
            }
            // Object is null. Nothing we can do 
            // 
            TraceError("We need to supply a cast, but we cannot determine the cast type.");
            return null; 
        }

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.GetEnumerator"]/*' />
        /// <devdoc> 
        ///     Retrieves a dictionary enumerator for the requested culture, or null if no resources for that culture exist.
        /// </devdoc> 
        public IDictionaryEnumerator GetEnumerator(IDesignerSerializationManager manager, CultureInfo culture) { 
            SerializationResourceManager resources = GetResourceManager(manager);
            return resources.GetEnumerator(culture); 
        }

        /// <devdoc>
        ///     Retrieves a dictionary enumerator for the requested culture, or null if no resources for that culture exist. 
        /// </devdoc>
        public IDictionaryEnumerator GetMetadataEnumerator(IDesignerSerializationManager manager) { 
            SerializationResourceManager resources = GetResourceManager(manager); 
            return resources.GetMetadataEnumerator();
        } 

        /// <devdoc>
        ///     Demand creates the serialization resource manager.  Stores the manager as an appended context value.
        /// </devdoc> 
        private SerializationResourceManager GetResourceManager(IDesignerSerializationManager manager) {
            SerializationResourceManager sm = manager.Context[typeof(SerializationResourceManager)] as SerializationResourceManager; 
            if (sm == null) { 
                sm = new SerializationResourceManager(manager);
                manager.Context.Append(sm); 
            }
            return sm;
        }
 
        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.Serialize"]/*' />
        /// <devdoc> 
        ///     Serializes the given object into a CodeDom object.  This expects the following 
        ///     values to be available on the context stack:
        /// 
        ///         A CodeStatementCollection that we can add our resource declaration to,
        ///         if necessary.
        ///
        ///         An ExpressionContext that contains the property, field or method 
        ///         that is being serialized, along with the object being serialized.
        ///         We need this so we can create a unique resource name for the 
        ///         object. 
        ///
        /// </devdoc> 
        public override object Serialize(IDesignerSerializationManager manager, object value) {
            return Serialize(manager, value, false, false, true);
        }
 
        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.Serialize2"]/*' />
        /// <devdoc> 
        ///     Serializes the given object into a CodeDom object.  This expects the following 
        ///     values to be available on the context stack:
        /// 
        ///         A CodeStatementCollection that we can add our resource declaration to,
        ///         if necessary.
        ///
        ///         An ExpressionContext that contains the property, field or method 
        ///         that is being serialized, along with the object being serialized.
        ///         We need this so we can create a unique resource name for the 
        ///         object. 
        ///
        /// </devdoc> 
        public object Serialize(IDesignerSerializationManager manager, object value, bool shouldSerializeInvariant) {
            return Serialize(manager, value, false, shouldSerializeInvariant, true);
        }
 
        /// <devdoc>
        ///     Serializes the given object into a CodeDom object.  This expects the following 
        ///     values to be available on the context stack: 
        ///
        ///         A CodeStatementCollection that we can add our resource declaration to, 
        ///         if necessary.
        ///
        ///         An ExpressionContext that contains the property, field or method
        ///         that is being serialized, along with the object being serialized. 
        ///         We need this so we can create a unique resource name for the
        ///         object. 
        /// 
        /// </devdoc>
        public object Serialize(IDesignerSerializationManager manager, object value, bool shouldSerializeInvariant, bool ensureInvariant) { 
            return Serialize(manager, value, false, shouldSerializeInvariant, ensureInvariant);
        }

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.Serialize1"]/*' /> 
        /// <devdoc>
        ///     This performs the actual work of serialization between Serialize and SerializeInvariant. 
        /// </devdoc> 
        private object Serialize(IDesignerSerializationManager manager, object value, bool forceInvariant, bool shouldSerializeInvariant, bool ensureInvariant) {
            CodeExpression expression = null; 

            using (TraceScope("ResourceCodeDomSerializer::Serialize")) {
                // Resource serialization is a little inconsistent.  We deserialize our own resource manager
                // creation statement, but we will never be asked to serialize a resource manager, because 
                // it doesn't exist as a product of the design container; it is purely an artifact of
                // serializing.  Some not-so-obvious side effects of this are: 
                // 
                //      This method will never ever be called by the serialization system directly.
                //      There is no attribute or metadata that will invoke it.  Instead, other 
                //      serializers will call this method to see if we should serialize to resources.
                //
                //      We need a way to inject the local variable declaration into the method body
                //      for the resource manager if we actually do emit a resource, which we shove 
                //      into the statements collection.
                SerializationResourceManager sm = GetResourceManager(manager); 
                CodeStatementCollection statements = (CodeStatementCollection)manager.Context[typeof(CodeStatementCollection)]; 

                // If this serialization resource manager has never been used to output 
                // culture-sensitive statements, then we must emit the local variable hookup.  Culture
                // invariant statements are used to save random data that is not representable in code,
                // so there is no need to emit a declaration.
                // 
                if (!forceInvariant) {
                    if (!sm.DeclarationAdded) { 
                        sm.DeclarationAdded = true; 

                        // If we have a root context, then we can write out a reasonable resource manager constructor. 
                        // If not, then we're a bit hobbled because we have to guess at the resource name.
                        RootContext rootCxt = manager.Context[typeof(RootContext)] as RootContext;
                        TraceWarningIf(statements == null, "No CodeStatementCollection on serialization stack, we cannot serialize resource manager creation statements.");
                        if (statements != null) { 
                            CodeExpression[] parameters;
 
                            if (rootCxt != null) { 
                                string baseType = manager.GetName(rootCxt.Value);
 
                                parameters = new CodeExpression[] { new CodeTypeOfExpression(baseType) };
                            }
                            else {
                                TraceWarning("No root context, we can only assume the resource manager resource name."); 
                                parameters = new CodeExpression[] { new CodePrimitiveExpression(ResourceManagerName) };
                            } 
 
                            CodeExpression initExpression = new CodeObjectCreateExpression(typeof(ComponentResourceManager), parameters);
 
                            statements.Add(new CodeVariableDeclarationStatement(typeof(ComponentResourceManager), ResourceManagerName, initExpression));
                            SetExpression(manager, sm, new CodeVariableReferenceExpression(ResourceManagerName));
                            sm.ExpressionAdded = true;
 
                            ComponentCache cache = manager.Context[typeof(ComponentCache)] as ComponentCache;
                            ComponentCache.Entry entry = manager.Context[typeof(ComponentCache.Entry)] as ComponentCache.Entry; 
                        } 
                    }
                    else { 
                        // Check to see if we have an expression for SM yet.  If we have cached the declaration
                        // in the component cache, the expression may not be setup so we should re-apply it.
                        if (!sm.ExpressionAdded) {
                            if (GetExpression(manager, sm) == null) { 
                                SetExpression(manager, sm, new CodeVariableReferenceExpression(ResourceManagerName));
                            } 
                            sm.ExpressionAdded = true; 
                        }
                    } 
                }

                // Retrieve the ExpressionContext on the context stack, and save the value as a resource.
                ExpressionContext tree = (ExpressionContext)manager.Context[typeof(ExpressionContext)]; 

                TraceWarningIf(tree == null, "No ExpressionContext on stack.  We can serialize, but we cannot create a well-formed name."); 
 
                string resourceName = sm.SetValue(manager, tree, value, forceInvariant, shouldSerializeInvariant, ensureInvariant, false);
 
                // Now the next step is to discover the type of the given value.  If it is a string,
                // we will invoke "GetString"  Otherwise, we will invoke "GetObject" and supply a
                // cast to the proper value.
                // 
                bool needCast;
                string methodName; 
 
                if (value is string || (tree != null && tree.ExpressionType == typeof(string))) {
                    needCast = false; 
                    methodName = "GetString";
                }
                else {
                    needCast = true; 
                    methodName = "GetObject";
                } 
 
                // Finally, all we need to do is create a CodeExpression that represents the resource manager
                // method invoke. 
                //
                Trace("Creating method invoke to {0}.{1}", ResourceManagerName, methodName);

                CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(); 

                methodInvoke.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(ResourceManagerName), methodName); 
                methodInvoke.Parameters.Add(new CodePrimitiveExpression(resourceName)); 
                if (needCast) {
                    Type castTo = GetCastType(manager, value); 

                    if (castTo != null) {
                        Trace("Supplying cast to {0}", castTo.Name);
                        expression = new CodeCastExpression(castTo, methodInvoke); 
                    }
                    else { 
                        expression = methodInvoke; 
                    }
                } 
                else {
                    expression = methodInvoke;
                }
            } 

            return expression; 
        } 

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializeInvariant"]/*' /> 
        /// <devdoc>
        ///     Serializes the given object into a CodeDom object saving resources
        ///     in the invariant culture, rather than the current culture.  This expects the following
        ///     values to be available on the context stack: 
        ///
        ///         A CodeStatementCollection that we can add our resource declaration to, 
        ///         if necessary. 
        ///
        ///         An ExpressionContext that contains the property, field or method 
        ///         that is being serialized, along with the object being serialized.
        ///         We need this so we can create a unique resource name for the
        ///         object.
        /// 
        /// </devdoc>
        public object SerializeInvariant(IDesignerSerializationManager manager, object value, bool shouldSerializeValue) { 
            return Serialize(manager, value, true, shouldSerializeValue, true); 
        }
 
        /// <devdoc>
        ///     Writes out the given metadata.
        /// </devdoc>
        public void SerializeMetadata(IDesignerSerializationManager manager, string name, object value, bool shouldSerializeValue) { 
            using (TraceScope("ResourceCodeDomSerializer::SerializeMetadata")) {
                Trace("Name: {0}", name); 
                Trace("Value: {0}", (value == null ? "(null)" : value.ToString())); 

                SerializationResourceManager sm = GetResourceManager(manager); 
                sm.SetMetadata(manager, name, value, shouldSerializeValue, false);
            }
        }
 
        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.WriteResource"]/*' />
        /// <devdoc> 
        ///     Serializes the given resource value into the resource set.  This does not effect 
        ///     the code dom values.  The resource is written into the current culture.
        /// </devdoc> 
        public void WriteResource(IDesignerSerializationManager manager, string name, object value) {
            using (TraceScope("ResourceCodeDomSerializer::WriteResource")) {
                Trace("Name: {0}", name);
                Trace("Value: {0}", (value == null ? "(null)" : value.ToString())); 

                SerializationResourceManager sm = GetResourceManager(manager); 
                sm.SetValue(manager, name, value, false, false, true, false); 
            }
        } 

        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.WriteResourceInvariant"]/*' />
        /// <devdoc>
        ///     Serializes the given resource value into the resource set.  This does not effect 
        ///     the code dom values.  The resource is written into the invariant culture.
        /// </devdoc> 
        public void WriteResourceInvariant(IDesignerSerializationManager manager, string name, object value) { 
            using (TraceScope("ResourceCodeDomSerializer::WriteResourceInvariant")) {
                Trace("Name: {0}", name); 
                Trace("Value: {0}", (value == null ? "(null)" : value.ToString()));

                SerializationResourceManager sm = GetResourceManager(manager);
                sm.SetValue(manager, name, value, true, true, true, false); 
            }
        } 
 
        /// <devdoc>
        ///     This is called by the component code dom serializer's caching logic to save cached 
        ///     resource data back into the resx files.
        /// </devdoc>
        internal void ApplyCacheEntry(IDesignerSerializationManager manager, ComponentCache.Entry entry) {
            SerializationResourceManager sm = GetResourceManager(manager); 

            if (entry.Metadata != null) { 
                foreach(ComponentCache.ResourceEntry re in entry.Metadata) { 
                    sm.SetMetadata(manager, re.Name, re.Value, re.ShouldSerializeValue, true);
                } 
            }

            if (entry.Resources != null) {
                foreach(ComponentCache.ResourceEntry re in entry.Resources) { 
                    manager.Context.Push(re.PropertyDescriptor);
                    manager.Context.Push(re.ExpressionContext); 
                    try { 
                        sm.SetValue(manager, re.Name, re.Value, re.ForceInvariant, re.ShouldSerializeValue, re.EnsureInvariant, true);
                    } finally { 
                        Debug.Assert(manager.Context.Current == re.ExpressionContext, "Someone corrupted the context stack");
                        manager.Context.Pop();
                        Debug.Assert(manager.Context.Current == re.PropertyDescriptor, "Someone corrupted the context stack");
                        manager.Context.Pop(); 
                    }
                } 
            } 
        }
 
        /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager"]/*' />
        /// <devdoc>
        ///     This is the meat of resource serialization.  This implements
        ///     a resource manager through a host-provided IResourceService 
        ///     interface.  The resource service feeds us with resource
        ///     readers and writers, and we simulate a runtime ResourceManager. 
        ///     There is one instance of this object for the entire serialization 
        ///     process, just like there is one resource manager in runtime
        ///     code.  When an instance of this object is created, it 
        ///     adds itself to the serialization manager's service list,
        ///     and listens for the SerializationComplete event.  When
        ///     serialization is complete, this will close and flush
        ///     any readers or writers it may have opened and will 
        ///     also remove itself from the service list.
        /// </devdoc> 
        private class SerializationResourceManager : ComponentResourceManager { 

            private static object resourceSetSentinel = new object(); 
            private IDesignerSerializationManager   manager;
            private bool                            checkedLocalizationLanguage;
            private CultureInfo                     localizationLanguage;
            private IResourceWriter                 writer; 
            private CultureInfo                     readCulture;
            private Hashtable                       nameTable; 
            private Hashtable                       resourceSets; 
            private Hashtable                       metadata;
            private Hashtable                       mergedMetadata; 
            private object rootComponent;
            private bool                            declarationAdded = false;
            private bool                            expressionAdded = false;
            private Hashtable                       propertyFillAdded; 
            private bool                            invariantCultureResourcesDirty = false;
            private bool                            metadataResourcesDirty = false; 
 

            public SerializationResourceManager(IDesignerSerializationManager manager) { 
                this.manager = manager;
                this.nameTable = new Hashtable();

                // We need to know when we're done so we can push the resource file out. 
                //
                manager.SerializationComplete += new EventHandler(OnSerializationComplete); 
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.DeclarationAdded"]/*' /> 
            /// <devdoc>
            ///     State the serializers use to determine if the declaration
            ///     of this resource manager has been performed.  This is just
            ///     per-document state we keep; we do not actually care about 
            ///     this value.
            /// </devdoc> 
            public bool DeclarationAdded { 
                get {
                    return declarationAdded; 
                }
                set {
                    declarationAdded = value;
                } 
            }
 
            /// <devdoc> 
            ///     When a declaration is added, we also setup an expression other serializers
            ///     can use to reference our resource declaration.  This bit tracks if we 
            ///     have setup this expression yet.  Note that the expression and declaration may
            ///     be added at diffrerent times, if the declaration was added by a cached
            ///     component.
            /// </devdoc> 
            public bool ExpressionAdded {
                get { 
                    return expressionAdded; 
                }
                set { 
                    expressionAdded = value;
                }
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.LocalizationLanguage"]/*' />
            /// <devdoc> 
            ///     The language we should be localizing into. 
            /// </devdoc>
            private CultureInfo LocalizationLanguage { 
                get {
                    if (!checkedLocalizationLanguage) {
                        // Check to see if our base component's localizable prop is true
                        RootContext rootCxt = manager.Context[typeof(RootContext)] as RootContext; 
                        if (rootCxt != null) {
                            object comp = rootCxt.Value; 
                            PropertyDescriptor prop = TypeDescriptor.GetProperties(comp)["LoadLanguage"]; 
                            if (prop != null && prop.PropertyType == typeof(CultureInfo)) {
                                localizationLanguage = (CultureInfo)prop.GetValue(comp); 
                            }
                        }
                        checkedLocalizationLanguage = true;
                    } 
                    return localizationLanguage;
                } 
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.ReadCulture"]/*' /> 
            /// <devdoc>
            ///     This is the culture info we should use to read and write resources.  We always write
            ///     using the same culture we read with so we don't stomp on data.
            /// </devdoc> 
            private CultureInfo ReadCulture {
                get { 
                    if (readCulture == null) { 
                        CultureInfo locCulture = LocalizationLanguage;
                        if (locCulture != null) { 
                            readCulture = locCulture;
                        }
                        else {
                            readCulture = CultureInfo.InvariantCulture; 
                        }
                    } 
 
                    return readCulture;
                } 
            }

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.ResourceTable"]/*' />
            /// <devdoc> 
            ///     Returns a hash table where we shove resource sets.
            /// </devdoc> 
            private Hashtable ResourceTable { 
                get {
                    if (resourceSets == null) { 
                        resourceSets = new Hashtable();
                    }
                    return resourceSets;
                } 
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.RootComponent"]/*' /> 
            /// <devdoc>
            ///     Retrieves the root component we're designing. 
            /// </devdoc>
            private object RootComponent {
                get {
                    if (rootComponent == null) { 
                        RootContext rootCxt = manager.Context[typeof(RootContext)] as RootContext;
                        if (rootCxt != null) { 
                            rootComponent = rootCxt.Value; 
                        }
                    } 
                    return rootComponent;
                }
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.Writer"]/*' />
            /// <devdoc> 
            ///     Retrieves a resource writer we should write into. 
            /// </devdoc>
            private IResourceWriter Writer { 
                get {
                    if (writer == null) {
                        IResourceService rs = (IResourceService)manager.GetService(typeof(IResourceService));
 
                        if (rs != null) {
 
                            // We always write with the culture we read with.  In the event of a language change 
                            // during localization, we will write the new language to the source code and then
                            // perform a reload. 
                            //
                            writer = rs.GetResourceWriter(ReadCulture);
                        }
                        else { 

                            // No resource service, so there is no way to create a resource writer for the 
                            // object.  In this case we just create an empty one so the resources go into 
                            // the bit-bucket.
                            // 
                            Debug.Fail("We expected to get IResourceService -- no resource serialization will be available");
                            writer = new ResourceWriter(new MemoryStream());
                        }
                    } 
                    return writer;
                } 
            } 

            /// <devdoc> 
            ///     The component serializer supports caching serialized outputs for speed.  It holds both a collection of statements as well
            ///     as an opaque blob for resources.  This function adds data to that blob.  The parameters to this function are the
            ///     same as those to SetValue, or SetMetadata (when isMetadata is true).
            /// </devdoc> 
            private void AddCacheEntry(IDesignerSerializationManager manager, string name, object value, bool isMetadata, bool forceInvariant, bool shouldSerializeValue, bool ensureInvariant) {
                ComponentCache.Entry entry = manager.Context[typeof(ComponentCache.Entry)] as ComponentCache.Entry; 
                if (entry != null) { 
                    ComponentCache.ResourceEntry re = new ComponentCache.ResourceEntry();
                    re.Name = name; 
                    re.Value = value;
                    re.ForceInvariant = forceInvariant;
                    re.ShouldSerializeValue = shouldSerializeValue;
                    re.EnsureInvariant = ensureInvariant; 
                    re.PropertyDescriptor = (PropertyDescriptor)manager.Context[typeof(PropertyDescriptor)];
                    re.ExpressionContext = (ExpressionContext)manager.Context[typeof(ExpressionContext)]; 
 
                    if (isMetadata) {
                        entry.AddMetadata(re); 
                    }
                    else {
                        entry.AddResource(re);
                    } 
                }
            } 
 
            /// <devdoc>
            ///     Returns true if the caller should add a property fill statement 
            ///     for the given object.  A property fill is required for the
            ///     component only once, so this remembers the value.
            /// </devdoc>
            public bool AddPropertyFill(object value) { 
                bool added = false;
                if (propertyFillAdded == null) { 
                    propertyFillAdded = new Hashtable(); 
                }
                else { 
                    added = propertyFillAdded.ContainsKey(value);
                }
                if (!added) {
                    propertyFillAdded[value] = value; 
                }
                return !added; 
            } 

            /// <devdoc> 
            ///     This method examines all the resources for the provided culture.
            ///     When it finds a resource with a key in the format of
            ///     &quot;[objectName].[property name]&quot; it will apply that resources value
            ///     to the corresponding property on the object. 
            /// </devdoc>
            public override void ApplyResources(object value, string objectName, CultureInfo culture) { 
 
                if (culture == null) {
                    culture = ReadCulture; 
                }

                base.ApplyResources(value, objectName, culture);
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.CompareWithParentValue"]/*' /> 
            /// <devdoc> 
            ///     This determines if the given resource name/value pair can be retrieved
            ///     from a parent culture.  We don't want to write duplicate resources for 
            ///     each language, so we do a check of the parent culture.
            /// </devdoc>
            private CompareValue CompareWithParentValue(string name, object value) {
                Debug.Assert(name != null, "name is null"); 

                // If there is no parent culture, treat that as being different from the parent's resource, 
                // which results in the "normal" code path for the caller. 
                if (ReadCulture.Equals(CultureInfo.InvariantCulture))
                    return CompareValue.Different; 

                CultureInfo culture = ReadCulture;

                for (;;) { 
                    Debug.Assert(culture.Parent != culture, "should have exited loop when culture = InvariantCulture");
                    culture = culture.Parent; 
 
                    Hashtable rs = GetResourceSet(culture);
 
                    bool contains = (rs == null) ? false : rs.ContainsKey(name);

                    if (contains) {
                        object parentValue = (rs != null) ? rs[name] : null; 

                        if (parentValue == value) { 
                            return CompareValue.Same; 
                        }
                        else if (parentValue != null) { 
                            if (parentValue.Equals(value))
                                return CompareValue.Same;
                            else
                                return CompareValue.Different; 
                        }
                        else { 
                            return CompareValue.Different; 
                        }
                    } 
                    else if (culture.Equals(CultureInfo.InvariantCulture)) {
                        return CompareValue.New;
                    }
                } 
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.CreateResourceSet"]/*' /> 
            /// <devdoc>
            ///     Creates a resource set hashtable for the given resource 
            ///     reader.
            /// </devdoc>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            private Hashtable CreateResourceSet(IResourceReader reader, CultureInfo culture) { 
                Hashtable result = new Hashtable();
 
                // We need to guard against bad or unloadable resources.  We warn the user in the task 
                // list here, but we will still load the designer.
                // 
                try {
                    IDictionaryEnumerator resEnum = reader.GetEnumerator();
                    while (resEnum.MoveNext()) {
                        string name = (string)resEnum.Key; 
                        object value = resEnum.Value;
                        result[name] = value; 
                    } 
                }
                catch (Exception e) { 
                    string message = e.Message;
                    if (message == null || message.Length == 0) {
                        message = e.GetType().Name;
                    } 

                    Exception se; 
 
                    if (culture == CultureInfo.InvariantCulture) {
                        se = new SerializationException(SR.GetString(SR.SerializerResourceExceptionInvariant, message), e); 
                    }
                    else {
                        se = new SerializationException(SR.GetString(SR.SerializerResourceException, culture.ToString(), message), e);
                    } 

                    manager.ReportError(se); 
                } 

                return result; 
            }

            /// <devdoc>
            ///     This returns a dictionary enumerator for metadata on the invariant culture.  If no metadata 
            ///    can be found this will return null..
            /// </devdoc> 
            public IDictionaryEnumerator GetMetadataEnumerator() { 
                if (mergedMetadata == null)
                { 
 	                Hashtable t = GetMetadata();
	                if (t != null) {

		                // This is for backwards compatibility and also for the case when our reader/writer 
		                // don't support metadata.  We must merge the original enumeration data in here or
 		                // else existing design time properties won't show up.  That would be really 
		                // bad for things like Localizable. 

 		                Hashtable it = GetResourceSet(CultureInfo.InvariantCulture); 
 		                if (it != null) {
			                foreach(DictionaryEntry de in it) {
 				                if (!t.ContainsKey(de.Key)) {
					                t.Add(de.Key, de.Value); 
				                }
			                } 
 		                } 
		                mergedMetadata = t;
 	                } 
                }
                if (mergedMetadata != null)
                {  	
 	                return mergedMetadata.GetEnumerator(); 
                }
                return null; 
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.GetEnumerator"]/*' /> 
            /// <devdoc>
            ///     This returns a dictionary enumerator for the given culture.  If no such resource file exists for the culture this
            ///     will return null.
            /// </devdoc> 
            public IDictionaryEnumerator GetEnumerator(CultureInfo culture) {
                Hashtable ht = GetResourceSet(culture); 
                if (ht != null) { 
                    return ht.GetEnumerator();
                } 

                return null;
            }
 
            /// <devdoc>
            ///     Loads the metadata table 
            /// </devdoc> 
            private Hashtable GetMetadata() {
                if (metadata == null) { 
                    IResourceService resSvc = (IResourceService)manager.GetService(typeof(IResourceService));

                    if (resSvc != null) {
                        IResourceReader reader = resSvc.GetResourceReader(CultureInfo.InvariantCulture); 

                        if (reader != null) { 
                            try { 
                                ResXResourceReader resxReader = reader as ResXResourceReader;
                                if (resxReader != null) { 
                                    metadata = new Hashtable();
                                    IDictionaryEnumerator de = resxReader.GetMetadataEnumerator();
                                    while (de.MoveNext()) {
                                        metadata[de.Key] = de.Value; 
                                    }
                                } 
                            } 

                            finally { 
                                reader.Close();
                            }
                        }
                    } 
                }
                return metadata; 
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.GetObject"]/*' /> 
            /// <devdoc>
            ///     Overrides ResourceManager.GetObject to return the requested
            ///     object.  Returns null if the object couldn't be found.
            /// </devdoc> 
            public override object GetObject(string resourceName) {
                return GetObject(resourceName, false); 
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.GetObject1"]/*' /> 
            /// <devdoc>
            ///     Retrieves the object of the given name from our resource bundle.
            ///     If forceInvariant is true, this will always use the invariant
            ///     resource, rather than using the current language. 
            ///     Returns null if the object couldn't be found.
            /// </devdoc> 
            public object GetObject(string resourceName, bool forceInvariant) { 

                Debug.Assert(manager != null, "This resource manager object has been destroyed."); 

                // We fetch the read culture if someone asks for a
                // culture-sensitive string.  If forceInvariant is set, we always
                // use the invariant culture. 
                //
                CultureInfo culture; 
 
                if (forceInvariant) {
                    culture = CultureInfo.InvariantCulture; 
                }
                else {
                    culture = ReadCulture;
                } 

                object value = null; 
 
                while (value == null) {
                    Hashtable rs = GetResourceSet(culture); 

                    if (rs != null) {
                        value = rs[resourceName];
                    } 

                    CultureInfo lastCulture = culture; 
                    culture = culture.Parent; 
                    if (lastCulture.Equals(culture)) {
                        break; 
                    }
                }

                return value; 
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.GetResourceSet"]/*' /> 
            /// <devdoc>
            ///     Looks up the resource set in the resourceSets hash table, loading the set if it hasn't been loaded already. 
            ///     Returns null if no resource that exists for that culture.
            /// </devdoc>
            private Hashtable GetResourceSet(CultureInfo culture) {
                Debug.Assert(culture != null, "null parameter"); 
                Hashtable rs = null;
                object objRs = ResourceTable[culture]; 
                if (objRs == null) { 
                    IResourceService resSvc = (IResourceService)manager.GetService(typeof(IResourceService));
 
                    TraceErrorIf(resSvc == null, "IResourceService is not available.  We will not be able to load resources.");
                    if (resSvc != null) {
                        IResourceReader reader = resSvc.GetResourceReader(culture);
                        if (reader != null) { 
                            try {
                                rs = CreateResourceSet(reader, culture); 
                            } 
                            finally {
                                reader.Close(); 
                            }
                            ResourceTable[culture] = rs;
                        }
                        else { 

                            // Provide a sentinel so we don't repeatedly ask 
                            // for the same resource.  If this is the invariant 
                            // culture, always provide one.
                            // 
                            if (culture.Equals(CultureInfo.InvariantCulture)) {
                                rs = new Hashtable();
                                ResourceTable[culture] = rs;
                            } 
                            else {
                                ResourceTable[culture] = resourceSetSentinel; 
                            } 
                        }
                    } 
                }
                else {
                    rs = objRs as Hashtable;
                    if (rs == null) { 
                        // the resourceSets hash table may contain our "this" pointer as a sentinel value
                        Debug.Assert(objRs == resourceSetSentinel, "unknown object in resourceSets: " + objRs); 
                    } 
                }
 
                return rs;
            }

            /// <devdoc> 
            ///     Override of GetResourceSet from ResourceManager.
            /// </devdoc> 
            public override ResourceSet GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents) { 

                if (culture == null) { 
                    throw new ArgumentNullException("culture");
                }

                CultureInfo lastCulture = culture; 

                do { 
                    Hashtable ht = GetResourceSet(culture); 
                    if (ht != null) {
                        return new CodeDomResourceSet(ht); 
                    }

                    lastCulture = culture;
                    culture = culture.Parent; 

                } while (tryParents && !lastCulture.Equals(culture)); 
 
                if (createIfNotExists) {
                    return new CodeDomResourceSet(); 
                }

                return null;
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.GetString"]/*' /> 
            /// <devdoc> 
            ///     Overrides ResourceManager.GetString to return the requested
            ///     string.  Returns null if the string couldn't be found. 
            /// </devdoc>
            public override string GetString(string resourceName) {
                return GetObject(resourceName, false) as string;
            } 

            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.OnSerializationComplete"]/*' /> 
            /// <devdoc> 
            ///     Event handler that gets called when serialization or deserialization
            ///     is complete. Here we need to write any resources to disk.  Sine 
            ///     we open resources for write on demand, this code handles the case
            ///     of reading resources as well.
            /// </devdoc>
            private void OnSerializationComplete(object sender, EventArgs e) { 
                // Commit any changes we have made.
                // 
                if (writer != null) { 
                    writer.Close();
                    writer = null; 
                }

                if (invariantCultureResourcesDirty || metadataResourcesDirty) {
 
                    IResourceService service = (IResourceService)manager.GetService(typeof(IResourceService));
                    if (service != null) { 
                        IResourceWriter invariantWriter = service.GetResourceWriter(CultureInfo.InvariantCulture); 

                        Debug.Assert(invariantWriter != null, "GetResourceWriter returned null for the InvariantCulture"); 

                        try {
                            // Do the invariant resources first
                            Debug.Assert(!ReadCulture.Equals(CultureInfo.InvariantCulture), "invariantCultureResourcesDirty should only come into play when readCulture != CultureInfo.InvariantCulture; check that CompareWithParentValue is correct"); 

                            object objRs = ResourceTable[CultureInfo.InvariantCulture]; 
 
                            Debug.Assert(objRs != null && objRs is Hashtable, "ResourceSet for the InvariantCulture not loaded, but it's considered dirty?");
 
                            Hashtable resourceSet = (Hashtable)objRs;

                            // Dump the hash table to the resource writer
                            // 
                            IDictionaryEnumerator resEnum = resourceSet.GetEnumerator();
 
                            while (resEnum.MoveNext()) { 
                                string name = (string)resEnum.Key;
                                object value = resEnum.Value; 

                                invariantWriter.AddResource(name, value);
                            }
 
                            invariantCultureResourcesDirty = false;
 
 
                            // Followed by the metadata.
                            Debug.Assert(metadata != null, "No metadata, but it's dirty?"); 

                            ResXResourceWriter resxWriter = invariantWriter as ResXResourceWriter;

                            if (resxWriter != null) { 
                                foreach (DictionaryEntry de in metadata) {
                                    resxWriter.AddMetadata((string)de.Key, de.Value); 
                                } 
                            }
                            else { 
                                Debug.Fail("Metadata not supported, but it's dirty?");
                            }

                            metadataResourcesDirty = false; 
                        }
                        finally { 
                            invariantWriter.Close(); 
                        }
                    } 
                    else {
                        Debug.Fail("Couldn't find IResourceService");
                        invariantCultureResourcesDirty = false;
                        metadataResourcesDirty = false; 
                    }
                } 
            } 

            /// <devdoc> 
            ///     Writes a metadata tag to the resource, or writes a normal
            ///     tag if the resource writer doesn't support metadata.
            /// </devdoc>
            public void SetMetadata(IDesignerSerializationManager manager, string resourceName, object value, bool shouldSerializeValue, bool applyingCachedResources) { 

                if (value != null && (!value.GetType().IsSerializable)) { 
                    Debug.Fail("Cannot save a non-serializable value into resources.  Add serializable to " + (value == null ? "(null)" : value.GetType().Name)); 
                    return;
                } 

                // If we are currently the invariant culture then we may be able to
                // write directly.
                if (ReadCulture.Equals(CultureInfo.InvariantCulture)) { 
                    ResXResourceWriter resxWriter = Writer as ResXResourceWriter;
                    if (shouldSerializeValue) { 
                        if (resxWriter != null) { 
                            resxWriter.AddMetadata(resourceName, value);
                        } 
                        else {
                            Writer.AddResource(resourceName, value);
                        }
                    } 
                }
                else { 
                    Hashtable t = null; 

                    // Check if the invariant writer supports metadata. If not, we need to push metadata 
                    // as regular data.
                    IResourceWriter invariantWriter = null;
                    IResourceService service = (IResourceService)manager.GetService(typeof(IResourceService));
                    if (service != null) { 
                        invariantWriter = service.GetResourceWriter(CultureInfo.InvariantCulture);
                    } 
 
                    Hashtable invariant = GetResourceSet(CultureInfo.InvariantCulture);
 
                    if (invariantWriter == null || invariantWriter is ResXResourceWriter) {
                        t = GetMetadata();
                        if (t == null) {
                            metadata = new Hashtable(); 
                            t = metadata;
                        } 
 
                        // Note that when we read metadata, for backwards compatibility, we also merge in regular data
                        // from the invariant resource. We need to clear that data here, since we are going to write 
                        // out metadata separately.
                        if (invariant.ContainsKey(resourceName)) {
                            invariant.Remove(resourceName);
                        } 

                        metadataResourcesDirty = true; 
                    } 
                    else {
                        t = invariant; 

                        invariantCultureResourcesDirty = true;
                    }
 
                    Debug.Assert(t != null, "Don't know where to push metadata.");
 
                    if (t != null) { 
                        if (shouldSerializeValue) {
                            t[resourceName] = value; 
                        }
                        else {
                            t.Remove(resourceName);
                        } 
                    }
                    mergedMetadata = null; 
                } 

                // Update the component cache, if we have one active 

                if (!applyingCachedResources) {
                    AddCacheEntry(manager, resourceName, value, true, false, shouldSerializeValue, false);
                } 
            }
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.SetValue"]/*' /> 
            /// <devdoc>
            ///     Writes the given resource value under the given name. 
            ///     This checks the parent resource to see if the values are the
            ///     same.  If they are, the resource is not written.  If not, then
            ///     the resource is written.  We always write using the resource language
            ///     we read in with, so we don't stomp on the wrong resource data in the 
            ///     event that someone changes the language.
            /// </devdoc> 
            public void SetValue(IDesignerSerializationManager manager, string resourceName, object value, bool forceInvariant, bool shouldSerializeInvariant, bool ensureInvariant, bool applyingCachedResources) { 

                // Values we are going to serialize must be serializable or else 
                // the resource writer will fail when we close it.
                //
                if (value != null && (!value.GetType().IsSerializable)) {
                    Debug.Fail("Cannot save a non-serializable value into resources.  Add serializable to " + (value == null ? "(null)" : value.GetType().Name)); 
                    return;
                } 
 
                if (forceInvariant) {
                    if (ReadCulture.Equals(CultureInfo.InvariantCulture)) { 
                        if (shouldSerializeInvariant) {
                            Writer.AddResource(resourceName, value);
                        }
                    } 
                    else {
                        Hashtable resourceSet = GetResourceSet(CultureInfo.InvariantCulture); 
 
                        Debug.Assert(resourceSet != null, "No ResourceSet for the InvariantCulture?");
 
                        if (shouldSerializeInvariant) {
                            resourceSet[resourceName] = value;
                        }
                        else { 
                            resourceSet.Remove(resourceName);
                        } 
 
                        invariantCultureResourcesDirty = true;
                    } 
                }
                else {
                    CompareValue comparison = CompareWithParentValue(resourceName, value);
                    switch (comparison) { 
                        case CompareValue.Same:
                            // don't add to any resource set 
                            break; 

                        case CompareValue.Different: 
                            Writer.AddResource(resourceName, value);
                            break;

                        case CompareValue.New: 

                            if (ensureInvariant) { 
                                // Add resource to InvariantCulture 
                                Debug.Assert(!ReadCulture.Equals(CultureInfo.InvariantCulture), "invariantCultureResourcesDirty should only come into play when readCulture != CultureInfo.InvariantCulture; check that CompareWithParentValue is correct");
 
                                Hashtable resourceSet = GetResourceSet(CultureInfo.InvariantCulture);

                                Debug.Assert(resourceSet != null, "No ResourceSet for the InvariantCulture?");
                                resourceSet[resourceName] = value; 
                                invariantCultureResourcesDirty = true;
                                Writer.AddResource(resourceName, value); 
                            } 
                            else {
                                // This is a new value.  We want to write it out, PROVIDED 
                                // that the value is not associated with a property that is currently
                                // returning false from ShouldSerializeValue.  This allows us to skip writing out
                                // Font == NULL on all non-invariant cultures, but still allow us to
                                // write out the value if the user is resetting a font back to null. 
                                // If we cannot associate the value with a property we will write
                                // it out just to be safe. 
                                // 
                                // In addition, we need to handle the case of the user adding a new
                                // component to the non-invariant language.  This would be bad, because 
                                // when he/she moved back to the invariant language the component's properties
                                // would all be defaults.  In order to minimize this problem, but still allow
                                // holes in the invariant resx, we also check to see if the property can
                                // be reset.  If it cannot be reset, that means that it has no meaningful 
                                // default. Therefore, it should have appeared in the invariant resx and its
                                // absence indicates a new component. 
                                // 
                                bool writeValue = true;
                                bool writeInvariant = false; 
                                PropertyDescriptor prop = (PropertyDescriptor)manager.Context[typeof(PropertyDescriptor)];

                                if (prop != null) {
                                    ExpressionContext tree = (ExpressionContext)manager.Context[typeof(ExpressionContext)]; 

                                    if (tree != null && tree.Expression is CodePropertyReferenceExpression) { 
                                        writeValue = prop.ShouldSerializeValue(tree.Owner); 
                                        writeInvariant = !prop.CanResetValue(tree.Owner);
                                    } 
                                }

                                if (writeValue) {
                                    Writer.AddResource(resourceName, value); 
                                    if (writeInvariant) {
                                        // Add resource to InvariantCulture 
                                        Debug.Assert(!ReadCulture.Equals(CultureInfo.InvariantCulture), "invariantCultureResourcesDirty should only come into play when readCulture != CultureInfo.InvariantCulture; check that CompareWithParentValue is correct"); 

                                        Hashtable resourceSet = GetResourceSet(CultureInfo.InvariantCulture); 

                                        Debug.Assert(resourceSet != null, "No ResourceSet for the InvariantCulture?");
                                        resourceSet[resourceName] = value;
                                        invariantCultureResourcesDirty = true; 
                                    }
                                } 
                            } 

                            break; 

                        default:
                            Debug.Fail("Unknown CompareValue " + comparison);
                            break; 
                    }
                } 
 
                // Update the component cache, if we have one active.  We don't have to be fancy here
                // because updating this cache just indicates that code in the component cache will later 
                // call us to re-apply the resources, and our logic above will be called again.

                if (!applyingCachedResources) {
                    AddCacheEntry(manager, resourceName, value, false, forceInvariant, shouldSerializeInvariant, ensureInvariant); 
                }
            } 
 
            /// <include file='doc\ResourceCodeDomSerializer.uex' path='docs/doc[@for="ResourceCodeDomSerializer.SerializationResourceManager.SetValue1"]/*' />
            /// <devdoc> 
            ///     Writes the given resource value under the given name.
            ///     This checks the parent resource to see if the values are the
            ///     same.  If they are, the resource is not written.  If not, then
            ///     the resource is written.  We always write using the resource language 
            ///     we read in with, so we don't stomp on the wrong resource data in the
            ///     event that someone changes the language. 
            /// </devdoc> 
            public string SetValue(IDesignerSerializationManager manager, ExpressionContext tree, object value, bool forceInvariant, bool shouldSerializeInvariant, bool ensureInvariant, bool applyingCachedResources) {
                string nameBase = null; 
                bool appendCount = false;

                if (tree != null) {
                    if (tree.Owner == RootComponent) { 
                        nameBase = "$this";
                    } 
                    else { 
                        nameBase = manager.GetName(tree.Owner);
 
                        if (nameBase == null) {
                            IReferenceService referenceService = (IReferenceService)manager.GetService(typeof(IReferenceService));
                            if (referenceService != null) {
                                nameBase = referenceService.GetName(tree.Owner); 
                            }
                        } 
                    } 
                    CodeExpression expression = tree.Expression;
 
                    string expressionName;

                    if (expression is CodePropertyReferenceExpression) {
                        expressionName = ((CodePropertyReferenceExpression)expression).PropertyName; 
                    }
                    else if (expression is CodeFieldReferenceExpression) { 
                        expressionName = ((CodeFieldReferenceExpression)expression).FieldName; 
                    }
                    else if (expression is CodeMethodReferenceExpression) { 
                        expressionName = ((CodeMethodReferenceExpression)expression).MethodName;
                        if (expressionName.StartsWith("Set")) {
                            expressionName = expressionName.Substring(3);
                        } 
                    }
                    else { 
                        expressionName = null; 
                    }
 
                    if (nameBase == null) {
                        nameBase = "resource";
                    }
 
                    if (expressionName != null) {
                        nameBase += "." + expressionName; 
                    } 
                }
                else { 
                    nameBase = "resource";
                    appendCount = true;
                }
 
                // Now find an unused name
                // 
                string resourceName = nameBase; 
                int count = 1;
 
                for(;;) {
                    if (appendCount) {
                        resourceName = nameBase + count.ToString(CultureInfo.InvariantCulture);
                        count++; 
                    }
                    else { 
                        appendCount = true; 
                    }
 
                    if (!nameTable.ContainsKey(resourceName)) {
                        break;
                    }
                } 

                // Now that we have a name, write out the resource. 
                // 
                SetValue(manager, resourceName, value, forceInvariant, shouldSerializeInvariant, ensureInvariant, applyingCachedResources);
 
                nameTable[resourceName] = resourceName;
                return resourceName;
            }
 
            private class CodeDomResourceSet : ResourceSet {
 
                public CodeDomResourceSet() { 
                }
 
                public CodeDomResourceSet(Hashtable resources) {
                    Table = resources;
                }
            } 

            private enum CompareValue { 
                Same, // parent value == child value 
                Different, // parent value exists, but != child value
                New, // parent value does not exist 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
