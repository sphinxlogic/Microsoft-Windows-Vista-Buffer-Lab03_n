 
//------------------------------------------------------------------------------
// <copyright file="CollectionCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.ComponentModel.Design.Serialization { 

    using System;
    using System.Design;
    using System.CodeDom; 
    using System.Collections;
    using System.Collections.Generic; 
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Reflection;
    using System.Globalization;
 
    /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer"]/*' />
    /// <devdoc> 
    ///     This serializer serializes collections.  This can either 
    ///     create statements or expressions.  It will create an
    ///     expression and assign it to the statement in the current 
    ///     context stack if the object is an array.  If it is
    ///     a collection with an add range or similar method,
    ///     it will create a statement calling the method.
    /// </devdoc> 
    public class CollectionCodeDomSerializer : CodeDomSerializer {
 
        private static CollectionCodeDomSerializer defaultSerializer; 

        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.Default"]/*' /> 
        /// <devdoc>
        ///     Retrieves a default static instance of this serializer.
        /// </devdoc>
        internal new static CollectionCodeDomSerializer Default { 
            get {
                if (defaultSerializer == null) { 
                    defaultSerializer = new CollectionCodeDomSerializer(); 
                }
                return defaultSerializer; 
            }
        }

        /// <devdoc> 
        ///     Computes the delta between an existing collection and a modified one.
        ///     This is for the case of inherited items that have collection properties so we only 
        ///     generate Add/AddRange calls for the items that have been added.  It works by 
        ///     Hashing up the items in the original collection and then walking the modified collection
        ///     and only returning those items which do not exist in the base collection. 
        /// </devdoc>
        private ICollection GetCollectionDelta(ICollection original, ICollection modified) {

            if (original == null || modified == null || original.Count == 0) { 
                return modified;
            } 
 
            IEnumerator modifiedEnum = modified.GetEnumerator();
 
            // yikes! who wrote this thing?
            if (modifiedEnum == null) {
                Debug.Fail("Collection of type " + modified.GetType().FullName + " doesn't return an enumerator");
                return modified; 
            }
            // first hash up the values so we can quickly decide if it's a new one or not 
            // 
            IDictionary originalValues = new HybridDictionary();
            foreach (object originalValue in original) { 

                // the array could contain multiple copies of the same value (think of a string collection),
                // so we need to be sensitive of that.
                // 
                if (originalValues.Contains(originalValue)) {
                    int count = (int)originalValues[originalValue]; 
                    originalValues[originalValue] = ++count; 
                }
                else { 
                    originalValues.Add(originalValue, 1);
                }
            }
 
            // now walk through and delete existing values
            // 
            ArrayList result = null; 

            // now compute the delta. 
            //
            for (int i = 0; i < modified.Count && modifiedEnum.MoveNext(); i++) {
                object value = modifiedEnum.Current;
 
                if (originalValues.Contains(value)) {
                    // we've got one we need to remove, so 
                    // create our array list, and push all the values we've passed 
                    // into it.
                    // 
                    if (result == null) {
                        result = new ArrayList();
                        modifiedEnum.Reset();
                        for (int n = 0; n < i && modifiedEnum.MoveNext(); n++) { 
                            result.Add(modifiedEnum.Current);
                        } 
 
                        // and finally skip the one we're on
                        // 
                        modifiedEnum.MoveNext();
                    }

                    // decrement the count if we've got more than one... 
                    //
                    int count = (int)originalValues[value]; 
 
                    if (--count == 0) {
                        originalValues.Remove(value); 
                    }
                    else {
                        originalValues[value] = count;
                    } 
                }
                else if (result != null) { 
                    // this one isn't in the old list, so add it to our 
                    // result list.
                    // 
                    result.Add(value);
                }

                // this item isn't in the list and we haven't yet created our array list 
                // so just keep on going.
                // 
            } 

            if (result != null) { 
                return result;
            }
            return modified;
        } 

        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.MethodSupportsSerialization"]/*' /> 
        /// <devdoc> 
        ///    Checks the attributes on this method to see if they support serialization.
        /// </devdoc> 
        protected bool MethodSupportsSerialization(MethodInfo method) {

            if (method == null) {
                throw new ArgumentNullException("method"); 
            }
 
            object[] attrs = method.GetCustomAttributes(typeof(DesignerSerializationVisibilityAttribute), true); 
            if (attrs.Length > 0) {
                DesignerSerializationVisibilityAttribute vis = (DesignerSerializationVisibilityAttribute)attrs[0]; 

                if (vis != null && vis.Visibility == DesignerSerializationVisibility.Hidden) {
                    Trace("Member {0} does not support serialization.", method.Name);
                    return false; 
                }
            } 
 
            return true;
        } 

        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.Serialize"]/*' />
        /// <devdoc>
        ///     Serializes the given object into a CodeDom object. 
        /// </devdoc>
        public override object Serialize(IDesignerSerializationManager manager, object value) { 
 
            if (manager == null) {
                throw new ArgumentNullException("manager"); 
            }

            if (value == null) {
                throw new ArgumentNullException("value"); 
            }
            object result = null; 
 
            using (TraceScope("CollectionCodeDomSerializer::Serialize")) {
 
                // We serialize collections as follows:
                //
                //      If the collection is an array, we write out the array.
                // 
                //      If the collection has a method called AddRange, we will
                //      call that, providing an array. 
                // 
                //      If the colleciton has an Add method, we will call it
                //      repeatedly. 
                //
                //      If the collection is an IList, we will cast to IList
                //      and add to it.
                // 
                //      If the collection has no add method, but is marked
                //      with PersistContents, we will enumerate the collection 
                //      and serialize each element. 
                // Check to see if there is a CodePropertyReferenceExpression on the stack.  If there is,
                // we can use it as a guide for serialization. 
                //
                ExpressionContext cxt = manager.Context[typeof(ExpressionContext)] as ExpressionContext;
                PropertyDescriptor prop = manager.Context[typeof(PropertyDescriptor)] as PropertyDescriptor;
                CodeExpression target; 

                if (cxt != null && cxt.PresetValue == value && prop != null && prop.PropertyType == cxt.ExpressionType) { 
 
                    // We only want to give out an expression target if
                    // this is our context (we find this out by comparing types above) and 
                    // if the context type is not an array.  If it is an array, we will
                    // just return the array create expression.

                    target = cxt.Expression; 
                    Trace("Valid context and property descriptor found on context stack.");
                } 
                else { 

                    // This context is either the wrong context or doesn't match the property descriptor 
                    // we found.

                    target = null;
                    cxt = null; 
                    prop = null;
                    Trace("No valid context.  We can only serialize if this is an array."); 
                } 

 
                // If we have a target expression see if we can create a delta for the collection.
                // We want to do this only if the propery the collection is associated with is
                // inherited, and if the collection is not an array.
 
                ICollection collection = value as ICollection;
                if (collection != null) { 
                    ICollection subset = collection; 
                    InheritedPropertyDescriptor inheritedDesc = prop as InheritedPropertyDescriptor;
                    Type collectionType = cxt == null ? collection.GetType() : cxt.ExpressionType; 
                    bool isArray = typeof(Array).IsAssignableFrom(collectionType);

                    // If we don't have a target expression and this isn't an array, let's try to create
                    // one. 
                    if (target == null && !isArray) {
                        bool isComplete; 
                        target = SerializeCreationExpression(manager, collection, out isComplete); 

                        if (isComplete) { 
                            return target;
                        }
                    }
 
                    if (target != null || isArray) {
                        if (inheritedDesc != null && !isArray) { 
                            subset = GetCollectionDelta(inheritedDesc.OriginalValue as ICollection, collection); 
                        }
 
                        result = SerializeCollection(manager, target, collectionType, collection, subset);

                        // See if we should emit a clear for this collection.
                        if (target != null && ShouldClearCollection(manager, collection)) { 

                            CodeStatementCollection resultCol = result as CodeStatementCollection; 
 
                            // VSWhidbey 482953
                            // If non empty collection is being serialized, but no statements 
                            // were generated, there is no need to clear.
                            if (collection.Count > 0 && (result == null || (resultCol != null && resultCol.Count == 0))) {
                                return null;
                            } 

                            if (resultCol == null) { 
                                resultCol = new CodeStatementCollection(); 
                                CodeStatement resultStmt = result as CodeStatement;
                                if (resultStmt != null) { 
                                    resultCol.Add(resultStmt);
                                }
                                result = resultCol;
                            } 

                            if (resultCol != null) { 
                                CodeMethodInvokeExpression clearMethod = new CodeMethodInvokeExpression(target, "Clear"); 
                                CodeExpressionStatement clearStmt = new CodeExpressionStatement(clearMethod);
                                resultCol.Insert(0, clearStmt); 
                            }
                        }
                    }
                } 
                else {
                    Debug.Fail("Collection serializer invoked for non-collection: " + (value == null ? "(null)" : value.GetType().Name)); 
                    TraceError("Collection serializer invoked for non collection: {0}", (value == null ? "(null)" : value.GetType().Name)); 
                }
            } 

            return result;
        }
 
        /// <devdoc>
        ///      Given a set of methods and objects, determines the method with the correct of 
        ///      parameter type for all objects. 
        /// </devdoc>
        private static MethodInfo ChooseMethodByType(List<MethodInfo> methods, ICollection values) { 

            MethodInfo final = null;
            Type finalType = null;
 
            foreach (object obj in values) {
                Type objType = TypeDescriptor.GetReflectionType(obj); 
 
                MethodInfo candidate = null;
                Type candidateType = null; 

                if (final == null || (finalType != null && !finalType.IsAssignableFrom(objType))) {
                    foreach (MethodInfo method in methods) {
                        ParameterInfo parameter = method.GetParameters()[0]; 
                        if (parameter != null) {
                            Type type = parameter.ParameterType.IsArray ? parameter.ParameterType.GetElementType() : parameter.ParameterType; 
                            if (type != null && type.IsAssignableFrom(objType)) { 
                                if (final != null) {
                                    if (type.IsAssignableFrom(finalType)) { 
                                        final = method;
                                        finalType = type;
                                        break;
                                    } 
                                }
                                else { 
                                    if (candidate == null) { 
                                        candidate = method;
                                        candidateType = type; 
                                    }
                                    else {
                                        // we found another method.  Pick the one that uses the most derived type.
                                        Debug.Assert(candidateType.IsAssignableFrom(type) || type.IsAssignableFrom(candidateType), "These two types are not related.  how were they chosen based on the base type"); 
                                        bool assignable = candidateType.IsAssignableFrom(type);
                                        candidate = assignable ? method : candidate; 
                                        candidateType = assignable ? type : candidateType; 
                                    }
                                } 
                            }
                        }
                    }
                } 

                if (final == null) { 
                    final = candidate; 
                    finalType = candidateType;
                } 
            }

            return final;
        } 

        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.SerializeCollection"]/*' /> 
        /// <devdoc> 
        ///     Serializes the given collection.  targetExpression will refer to the expression used to rever to the
        ///     collection, but it can be null. 
        /// </devdoc>
        protected virtual object SerializeCollection(IDesignerSerializationManager manager, CodeExpression targetExpression, Type targetType, ICollection originalCollection, ICollection valuesToSerialize) {

            if (manager == null) throw new ArgumentNullException("manager"); 
            if (targetType == null) throw new ArgumentNullException("targetType");
            if (originalCollection == null) throw new ArgumentNullException("originalCollection"); 
            if (valuesToSerialize == null) throw new ArgumentNullException("valuesToSerialize"); 

            object result = null; 

            bool serialized = false;

            if (typeof(Array).IsAssignableFrom(targetType)) { 

                Trace("Collection is array"); 
                CodeArrayCreateExpression arrayCreate = SerializeArray(manager, targetType, originalCollection, valuesToSerialize); 
                if (arrayCreate != null) {
                    if (targetExpression != null) { 
                        result = new CodeAssignStatement(targetExpression, arrayCreate);
                    }
                    else {
                        result = arrayCreate; 
                    }
                    serialized = true; 
                } 
            }
            else if (valuesToSerialize.Count > 0) { 
                Trace("Searching for AddRange or Add");

                MethodInfo[] methods = TypeDescriptor.GetReflectionType(originalCollection).GetMethods(BindingFlags.Public | BindingFlags.Instance);
 
                ParameterInfo[] parameters;
                List<MethodInfo> addRangeMethods = new List<MethodInfo>(); 
                List<MethodInfo> addMethods = new List<MethodInfo>(); 

                foreach (MethodInfo method in methods) { 
                    if (method.Name.Equals("AddRange")) {
                        parameters = method.GetParameters();
                        if (parameters.Length == 1 && parameters[0].ParameterType.IsArray) {
                            if (MethodSupportsSerialization(method)) { 
                                addRangeMethods.Add(method);
                            } 
                        } 
                    }
 
                    if (method.Name.Equals("Add")) {
                        parameters = method.GetParameters();
                        if (parameters.Length == 1) {
                            if (MethodSupportsSerialization(method)) { 
                                addMethods.Add(method);
                            } 
                        } 
                    }
                } 

                MethodInfo addRangeMethodToUse = ChooseMethodByType(addRangeMethods, valuesToSerialize);
                if (addRangeMethodToUse != null) {
                    result = SerializeViaAddRange(manager, targetExpression, targetType, addRangeMethodToUse.GetParameters()[0], valuesToSerialize); 
                    serialized = true;
                } 
                else { 
                    MethodInfo addMethodToUse = ChooseMethodByType(addMethods, valuesToSerialize);
                    if (addMethodToUse != null) { 
                        result = SerializeViaAdd(manager, targetExpression, targetType, addMethodToUse.GetParameters()[0], valuesToSerialize);
                        serialized = true;
                    }
                } 

                if (!serialized && originalCollection.GetType().IsSerializable) { 
                    result = SerializeToResourceExpression(manager, originalCollection, false); 
                }
            } 
            else {
                Trace("Collection has no values to serialize.");
            }
 
            return result;
        } 
 
        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.SerializeArray"]/*' />
        /// <devdoc> 
        ///     Serializes the given array.
        /// </devdoc>
        private CodeArrayCreateExpression SerializeArray(IDesignerSerializationManager manager, Type targetType, ICollection array, ICollection valuesToSerialize) {
 
            CodeArrayCreateExpression result = null;
 
            using (TraceScope("CollectionCodeDomSerializer::SerializeArray")) { 
                if (((Array)array).Rank != 1) {
                    TraceError("Cannot serialize arrays with rank > 1."); 
                    manager.ReportError(SR.GetString(SR.SerializerInvalidArrayRank, ((Array)array).Rank.ToString(CultureInfo.InvariantCulture)));
                }
                else {
                    // For an array, we need an array create expression.  First, get the array type 
                    //
                    Type elementType = targetType.GetElementType(); 
                    CodeTypeReference elementTypeRef = new CodeTypeReference(elementType); 

                    Trace("Array type: {0}", elementType.Name); 
                    Trace("Count: {0}", valuesToSerialize.Count);

                    // Now create an ArrayCreateExpression, and fill its initializers.
                    // 
                    CodeArrayCreateExpression arrayCreate = new CodeArrayCreateExpression();
 
                    arrayCreate.CreateType = elementTypeRef; 

                    bool arrayOk = true; 

                    foreach (object o in valuesToSerialize) {

                        // If this object is being privately inherited, it cannot be inside 
                        // this collection.  Since we're writing an entire array here, we
                        // cannot write any of it. 
                        // 
                        if (o is IComponent && TypeDescriptor.GetAttributes(o).Contains(InheritanceAttribute.InheritedReadOnly)) {
                            arrayOk = false; 
                            break;
                        }

                        CodeExpression expression = null; 

                        // VSWhidbey #266085: If there is an expression context on the stack at this point, 
                        // we need to fix up the ExpressionType on it to be the array element type. 
                        ExpressionContext newCxt = null;
                        ExpressionContext cxt = manager.Context[typeof(ExpressionContext)] as ExpressionContext; 

                        if (cxt != null) {
                            newCxt = new ExpressionContext(cxt.Expression, elementType, cxt.Owner);
                            manager.Context.Push(newCxt); 
                        }
 
                        try { 
                            expression = SerializeToExpression(manager, o);
                        } 
                        finally {
                            if (newCxt != null) {
                                Debug.Assert(manager.Context.Current == newCxt, "Context stack corrupted.");
                                manager.Context.Pop(); 
                            }
                        } 
 
                        if (expression is CodeExpression) {
                            if (o != null && o.GetType() != elementType) { 
                                expression = new CodeCastExpression(elementType, expression);
                            }

                            arrayCreate.Initializers.Add((CodeExpression)expression); 
                        }
                        else { 
                            arrayOk = false; 
                            break;
                        } 
                    }

                    if (arrayOk) {
                        result = arrayCreate; 
                    }
                } 
            } 

            return result; 
        }

        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.SerializeViaAdd"]/*' />
        /// <devdoc> 
        ///     Serializes the given collection by creating multiple calls to an Add method.
        /// </devdoc> 
        private object SerializeViaAdd( 
            IDesignerSerializationManager manager,
            CodeExpression targetExpression, 
            Type targetType,
            ParameterInfo parameter,
            ICollection valuesToSerialize) {
 
            CodeStatementCollection statements = new CodeStatementCollection();
            using (TraceScope("CollectionCodeDomSerializer::SerializeViaAdd")) { 
                Trace("Elements: {0}", valuesToSerialize.Count.ToString(CultureInfo.InvariantCulture)); 

                // Here we need to invoke Add once for each and every item in the collection. We can re-use the property 
                // reference and method reference, but we will need to recreate the invoke statement each time.
                //
                CodeMethodReferenceExpression methodRef = new CodeMethodReferenceExpression(targetExpression, "Add");
 
                if (valuesToSerialize.Count > 0) {
 
                    ExpressionContext cxt = manager.Context[typeof(ExpressionContext)] as ExpressionContext; 

                    foreach (object o in valuesToSerialize) { 
                        // If this object is being privately inherited, it cannot be inside
                        // this collection.
                        //
                        bool genCode = !(o is IComponent); 

                        if (!genCode) { 
                            InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(o)[typeof(InheritanceAttribute)]; 

                            if (ia != null) { 
                                if (ia.InheritanceLevel == InheritanceLevel.InheritedReadOnly)
                                    genCode = false;
                                else
                                    genCode = true; 
                            }
                            else { 
                                genCode = true; 
                            }
                        } 

                        Debug.Assert(genCode, "Why didn't GetCollectionDelta calculate the same thing?");
                        if (genCode) {
                            CodeMethodInvokeExpression statement = new CodeMethodInvokeExpression(); 

                            statement.Method = methodRef; 
 
                            CodeExpression serializedObj = null;
                            Type elementType = parameter.ParameterType; 

                            // VSWhidbey #266085: If there is an expression context on the stack at this point,
                            // we need to fix up the ExpressionType on it to be the element type.
                            ExpressionContext newCxt = null; 

                            if (cxt != null) { 
                                newCxt = new ExpressionContext(cxt.Expression, elementType, cxt.Owner); 
                                manager.Context.Push(newCxt);
                            } 

                            try {
                                serializedObj =  SerializeToExpression(manager, o);
                            } 
                            finally {
                                if (newCxt != null) { 
                                    Debug.Assert(manager.Context.Current == newCxt, "Context stack corrupted."); 
                                    manager.Context.Pop();
                                } 
                            }

                            if (o != null && !elementType.IsAssignableFrom(o.GetType()) && o.GetType().IsPrimitive) {
                                serializedObj = new CodeCastExpression(elementType, serializedObj); 
                            }
 
                            if (serializedObj != null) { 
                                statement.Parameters.Add(serializedObj);
                                statements.Add(statement); 
                            }
                        }
                    }
                } 
            }
 
            return statements; 
        }
 
        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.SerializeViaAddRange"]/*' />
        /// <devdoc>
        ///     Serializes the given collection by creating an array and passing it to the AddRange method.
        /// </devdoc> 
        private object SerializeViaAddRange(
            IDesignerSerializationManager manager, 
            CodeExpression targetExpression, 
            Type targetType,
            ParameterInfo parameter, 
            ICollection valuesToSerialize) {


            CodeStatementCollection statements = new CodeStatementCollection(); 

            using (TraceScope("CollectionCodeDomSerializer::SerializeViaAddRange")) { 
                Trace("Elements: {0}", valuesToSerialize.Count.ToString(CultureInfo.InvariantCulture)); 

                if (valuesToSerialize.Count > 0) { 

                    ArrayList arrayList = new ArrayList(valuesToSerialize.Count);
                    ExpressionContext cxt = manager.Context[typeof(ExpressionContext)] as ExpressionContext;
                    Type elementType = parameter.ParameterType.GetElementType(); 

                    foreach (object o in valuesToSerialize) { 
 
                        // If this object is being privately inherited, it cannot be inside
                        // this collection. 
                        //
                        bool genCode = !(o is IComponent);

                        if (!genCode) { 
                            InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(o)[typeof(InheritanceAttribute)];
 
                            if (ia != null) { 
                                if (ia.InheritanceLevel == InheritanceLevel.InheritedReadOnly)
                                    genCode = false; 
                                else
                                    genCode = true;
                            }
                            else { 
                                genCode = true;
                            } 
                        } 

                        Debug.Assert(genCode, "Why didn't GetCollectionDelta calculate the same thing?"); 
                        if (genCode) {
                            CodeExpression exp = null;

                            // VSWhidbey #266085: If there is an expression context on the stack at this point, 
                            // we need to fix up the ExpressionType on it to be the element type.
                            ExpressionContext newCxt = null; 
 
                            if (cxt != null) {
                                newCxt = new ExpressionContext(cxt.Expression, elementType, cxt.Owner); 
                                manager.Context.Push(newCxt);
                            }

                            try { 
                                exp =  SerializeToExpression(manager, o);
                            } 
                            finally { 
                                if (newCxt != null) {
                                    Debug.Assert(manager.Context.Current == newCxt, "Context stack corrupted."); 
                                    manager.Context.Pop();
                                }
                            }
 
                            if (exp != null) {
                                // Check to see if we need a cast 
                                if (o != null && !elementType.IsAssignableFrom(o.GetType())) { 
                                    exp = new CodeCastExpression(elementType, exp);
                                } 

                                arrayList.Add(exp);
                            }
                        } 
                    }
 
                    if (arrayList.Count > 0) { 
                        // Now convert the array list into an array create expression.
                        // 
                        CodeTypeReference elementTypeRef = new CodeTypeReference(elementType);

                        // Now create an ArrayCreateExpression, and fill its initializers.
                        // 
                        CodeArrayCreateExpression arrayCreate = new CodeArrayCreateExpression();
 
                        arrayCreate.CreateType = elementTypeRef; 
                        foreach (CodeExpression exp in arrayList) {
                            arrayCreate.Initializers.Add(exp); 
                        }

                        CodeMethodReferenceExpression methodRef = new CodeMethodReferenceExpression(targetExpression, "AddRange");
                        CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(); 

                        methodInvoke.Method = methodRef; 
                        methodInvoke.Parameters.Add(arrayCreate); 
                        statements.Add(new CodeExpressionStatement(methodInvoke));
                    } 
                }
            }

            return statements; 
        }
 
        /// <devdoc> 
        ///     Returns true if we should clear the collection contents.
        /// </devdoc> 
        private bool ShouldClearCollection(IDesignerSerializationManager manager, ICollection collection) {

            bool shouldClear = false;
 
            PropertyDescriptor clearProp = manager.Properties["ClearCollections"];
            if (clearProp != null && clearProp.PropertyType == typeof(bool) && ((bool)clearProp.GetValue(manager) == true)) { 
                shouldClear = true; 
            }
 
            if (!shouldClear) {
                SerializeAbsoluteContext absolute = (SerializeAbsoluteContext)manager.Context[typeof(SerializeAbsoluteContext)];
                PropertyDescriptor prop = manager.Context[typeof(PropertyDescriptor)] as PropertyDescriptor;
                if (absolute != null && absolute.ShouldSerialize(prop)) { 
                    shouldClear = true;
                } 
            } 

            if (shouldClear) { 
                MethodInfo clearMethod = TypeDescriptor.GetReflectionType(collection).GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null);

                if (clearMethod == null || !MethodSupportsSerialization(clearMethod)) {
                    shouldClear = false; 
                }
            } 
 
            return shouldClear;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="CollectionCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.ComponentModel.Design.Serialization { 

    using System;
    using System.Design;
    using System.CodeDom; 
    using System.Collections;
    using System.Collections.Generic; 
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics;
    using System.Reflection;
    using System.Globalization;
 
    /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer"]/*' />
    /// <devdoc> 
    ///     This serializer serializes collections.  This can either 
    ///     create statements or expressions.  It will create an
    ///     expression and assign it to the statement in the current 
    ///     context stack if the object is an array.  If it is
    ///     a collection with an add range or similar method,
    ///     it will create a statement calling the method.
    /// </devdoc> 
    public class CollectionCodeDomSerializer : CodeDomSerializer {
 
        private static CollectionCodeDomSerializer defaultSerializer; 

        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.Default"]/*' /> 
        /// <devdoc>
        ///     Retrieves a default static instance of this serializer.
        /// </devdoc>
        internal new static CollectionCodeDomSerializer Default { 
            get {
                if (defaultSerializer == null) { 
                    defaultSerializer = new CollectionCodeDomSerializer(); 
                }
                return defaultSerializer; 
            }
        }

        /// <devdoc> 
        ///     Computes the delta between an existing collection and a modified one.
        ///     This is for the case of inherited items that have collection properties so we only 
        ///     generate Add/AddRange calls for the items that have been added.  It works by 
        ///     Hashing up the items in the original collection and then walking the modified collection
        ///     and only returning those items which do not exist in the base collection. 
        /// </devdoc>
        private ICollection GetCollectionDelta(ICollection original, ICollection modified) {

            if (original == null || modified == null || original.Count == 0) { 
                return modified;
            } 
 
            IEnumerator modifiedEnum = modified.GetEnumerator();
 
            // yikes! who wrote this thing?
            if (modifiedEnum == null) {
                Debug.Fail("Collection of type " + modified.GetType().FullName + " doesn't return an enumerator");
                return modified; 
            }
            // first hash up the values so we can quickly decide if it's a new one or not 
            // 
            IDictionary originalValues = new HybridDictionary();
            foreach (object originalValue in original) { 

                // the array could contain multiple copies of the same value (think of a string collection),
                // so we need to be sensitive of that.
                // 
                if (originalValues.Contains(originalValue)) {
                    int count = (int)originalValues[originalValue]; 
                    originalValues[originalValue] = ++count; 
                }
                else { 
                    originalValues.Add(originalValue, 1);
                }
            }
 
            // now walk through and delete existing values
            // 
            ArrayList result = null; 

            // now compute the delta. 
            //
            for (int i = 0; i < modified.Count && modifiedEnum.MoveNext(); i++) {
                object value = modifiedEnum.Current;
 
                if (originalValues.Contains(value)) {
                    // we've got one we need to remove, so 
                    // create our array list, and push all the values we've passed 
                    // into it.
                    // 
                    if (result == null) {
                        result = new ArrayList();
                        modifiedEnum.Reset();
                        for (int n = 0; n < i && modifiedEnum.MoveNext(); n++) { 
                            result.Add(modifiedEnum.Current);
                        } 
 
                        // and finally skip the one we're on
                        // 
                        modifiedEnum.MoveNext();
                    }

                    // decrement the count if we've got more than one... 
                    //
                    int count = (int)originalValues[value]; 
 
                    if (--count == 0) {
                        originalValues.Remove(value); 
                    }
                    else {
                        originalValues[value] = count;
                    } 
                }
                else if (result != null) { 
                    // this one isn't in the old list, so add it to our 
                    // result list.
                    // 
                    result.Add(value);
                }

                // this item isn't in the list and we haven't yet created our array list 
                // so just keep on going.
                // 
            } 

            if (result != null) { 
                return result;
            }
            return modified;
        } 

        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.MethodSupportsSerialization"]/*' /> 
        /// <devdoc> 
        ///    Checks the attributes on this method to see if they support serialization.
        /// </devdoc> 
        protected bool MethodSupportsSerialization(MethodInfo method) {

            if (method == null) {
                throw new ArgumentNullException("method"); 
            }
 
            object[] attrs = method.GetCustomAttributes(typeof(DesignerSerializationVisibilityAttribute), true); 
            if (attrs.Length > 0) {
                DesignerSerializationVisibilityAttribute vis = (DesignerSerializationVisibilityAttribute)attrs[0]; 

                if (vis != null && vis.Visibility == DesignerSerializationVisibility.Hidden) {
                    Trace("Member {0} does not support serialization.", method.Name);
                    return false; 
                }
            } 
 
            return true;
        } 

        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.Serialize"]/*' />
        /// <devdoc>
        ///     Serializes the given object into a CodeDom object. 
        /// </devdoc>
        public override object Serialize(IDesignerSerializationManager manager, object value) { 
 
            if (manager == null) {
                throw new ArgumentNullException("manager"); 
            }

            if (value == null) {
                throw new ArgumentNullException("value"); 
            }
            object result = null; 
 
            using (TraceScope("CollectionCodeDomSerializer::Serialize")) {
 
                // We serialize collections as follows:
                //
                //      If the collection is an array, we write out the array.
                // 
                //      If the collection has a method called AddRange, we will
                //      call that, providing an array. 
                // 
                //      If the colleciton has an Add method, we will call it
                //      repeatedly. 
                //
                //      If the collection is an IList, we will cast to IList
                //      and add to it.
                // 
                //      If the collection has no add method, but is marked
                //      with PersistContents, we will enumerate the collection 
                //      and serialize each element. 
                // Check to see if there is a CodePropertyReferenceExpression on the stack.  If there is,
                // we can use it as a guide for serialization. 
                //
                ExpressionContext cxt = manager.Context[typeof(ExpressionContext)] as ExpressionContext;
                PropertyDescriptor prop = manager.Context[typeof(PropertyDescriptor)] as PropertyDescriptor;
                CodeExpression target; 

                if (cxt != null && cxt.PresetValue == value && prop != null && prop.PropertyType == cxt.ExpressionType) { 
 
                    // We only want to give out an expression target if
                    // this is our context (we find this out by comparing types above) and 
                    // if the context type is not an array.  If it is an array, we will
                    // just return the array create expression.

                    target = cxt.Expression; 
                    Trace("Valid context and property descriptor found on context stack.");
                } 
                else { 

                    // This context is either the wrong context or doesn't match the property descriptor 
                    // we found.

                    target = null;
                    cxt = null; 
                    prop = null;
                    Trace("No valid context.  We can only serialize if this is an array."); 
                } 

 
                // If we have a target expression see if we can create a delta for the collection.
                // We want to do this only if the propery the collection is associated with is
                // inherited, and if the collection is not an array.
 
                ICollection collection = value as ICollection;
                if (collection != null) { 
                    ICollection subset = collection; 
                    InheritedPropertyDescriptor inheritedDesc = prop as InheritedPropertyDescriptor;
                    Type collectionType = cxt == null ? collection.GetType() : cxt.ExpressionType; 
                    bool isArray = typeof(Array).IsAssignableFrom(collectionType);

                    // If we don't have a target expression and this isn't an array, let's try to create
                    // one. 
                    if (target == null && !isArray) {
                        bool isComplete; 
                        target = SerializeCreationExpression(manager, collection, out isComplete); 

                        if (isComplete) { 
                            return target;
                        }
                    }
 
                    if (target != null || isArray) {
                        if (inheritedDesc != null && !isArray) { 
                            subset = GetCollectionDelta(inheritedDesc.OriginalValue as ICollection, collection); 
                        }
 
                        result = SerializeCollection(manager, target, collectionType, collection, subset);

                        // See if we should emit a clear for this collection.
                        if (target != null && ShouldClearCollection(manager, collection)) { 

                            CodeStatementCollection resultCol = result as CodeStatementCollection; 
 
                            // VSWhidbey 482953
                            // If non empty collection is being serialized, but no statements 
                            // were generated, there is no need to clear.
                            if (collection.Count > 0 && (result == null || (resultCol != null && resultCol.Count == 0))) {
                                return null;
                            } 

                            if (resultCol == null) { 
                                resultCol = new CodeStatementCollection(); 
                                CodeStatement resultStmt = result as CodeStatement;
                                if (resultStmt != null) { 
                                    resultCol.Add(resultStmt);
                                }
                                result = resultCol;
                            } 

                            if (resultCol != null) { 
                                CodeMethodInvokeExpression clearMethod = new CodeMethodInvokeExpression(target, "Clear"); 
                                CodeExpressionStatement clearStmt = new CodeExpressionStatement(clearMethod);
                                resultCol.Insert(0, clearStmt); 
                            }
                        }
                    }
                } 
                else {
                    Debug.Fail("Collection serializer invoked for non-collection: " + (value == null ? "(null)" : value.GetType().Name)); 
                    TraceError("Collection serializer invoked for non collection: {0}", (value == null ? "(null)" : value.GetType().Name)); 
                }
            } 

            return result;
        }
 
        /// <devdoc>
        ///      Given a set of methods and objects, determines the method with the correct of 
        ///      parameter type for all objects. 
        /// </devdoc>
        private static MethodInfo ChooseMethodByType(List<MethodInfo> methods, ICollection values) { 

            MethodInfo final = null;
            Type finalType = null;
 
            foreach (object obj in values) {
                Type objType = TypeDescriptor.GetReflectionType(obj); 
 
                MethodInfo candidate = null;
                Type candidateType = null; 

                if (final == null || (finalType != null && !finalType.IsAssignableFrom(objType))) {
                    foreach (MethodInfo method in methods) {
                        ParameterInfo parameter = method.GetParameters()[0]; 
                        if (parameter != null) {
                            Type type = parameter.ParameterType.IsArray ? parameter.ParameterType.GetElementType() : parameter.ParameterType; 
                            if (type != null && type.IsAssignableFrom(objType)) { 
                                if (final != null) {
                                    if (type.IsAssignableFrom(finalType)) { 
                                        final = method;
                                        finalType = type;
                                        break;
                                    } 
                                }
                                else { 
                                    if (candidate == null) { 
                                        candidate = method;
                                        candidateType = type; 
                                    }
                                    else {
                                        // we found another method.  Pick the one that uses the most derived type.
                                        Debug.Assert(candidateType.IsAssignableFrom(type) || type.IsAssignableFrom(candidateType), "These two types are not related.  how were they chosen based on the base type"); 
                                        bool assignable = candidateType.IsAssignableFrom(type);
                                        candidate = assignable ? method : candidate; 
                                        candidateType = assignable ? type : candidateType; 
                                    }
                                } 
                            }
                        }
                    }
                } 

                if (final == null) { 
                    final = candidate; 
                    finalType = candidateType;
                } 
            }

            return final;
        } 

        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.SerializeCollection"]/*' /> 
        /// <devdoc> 
        ///     Serializes the given collection.  targetExpression will refer to the expression used to rever to the
        ///     collection, but it can be null. 
        /// </devdoc>
        protected virtual object SerializeCollection(IDesignerSerializationManager manager, CodeExpression targetExpression, Type targetType, ICollection originalCollection, ICollection valuesToSerialize) {

            if (manager == null) throw new ArgumentNullException("manager"); 
            if (targetType == null) throw new ArgumentNullException("targetType");
            if (originalCollection == null) throw new ArgumentNullException("originalCollection"); 
            if (valuesToSerialize == null) throw new ArgumentNullException("valuesToSerialize"); 

            object result = null; 

            bool serialized = false;

            if (typeof(Array).IsAssignableFrom(targetType)) { 

                Trace("Collection is array"); 
                CodeArrayCreateExpression arrayCreate = SerializeArray(manager, targetType, originalCollection, valuesToSerialize); 
                if (arrayCreate != null) {
                    if (targetExpression != null) { 
                        result = new CodeAssignStatement(targetExpression, arrayCreate);
                    }
                    else {
                        result = arrayCreate; 
                    }
                    serialized = true; 
                } 
            }
            else if (valuesToSerialize.Count > 0) { 
                Trace("Searching for AddRange or Add");

                MethodInfo[] methods = TypeDescriptor.GetReflectionType(originalCollection).GetMethods(BindingFlags.Public | BindingFlags.Instance);
 
                ParameterInfo[] parameters;
                List<MethodInfo> addRangeMethods = new List<MethodInfo>(); 
                List<MethodInfo> addMethods = new List<MethodInfo>(); 

                foreach (MethodInfo method in methods) { 
                    if (method.Name.Equals("AddRange")) {
                        parameters = method.GetParameters();
                        if (parameters.Length == 1 && parameters[0].ParameterType.IsArray) {
                            if (MethodSupportsSerialization(method)) { 
                                addRangeMethods.Add(method);
                            } 
                        } 
                    }
 
                    if (method.Name.Equals("Add")) {
                        parameters = method.GetParameters();
                        if (parameters.Length == 1) {
                            if (MethodSupportsSerialization(method)) { 
                                addMethods.Add(method);
                            } 
                        } 
                    }
                } 

                MethodInfo addRangeMethodToUse = ChooseMethodByType(addRangeMethods, valuesToSerialize);
                if (addRangeMethodToUse != null) {
                    result = SerializeViaAddRange(manager, targetExpression, targetType, addRangeMethodToUse.GetParameters()[0], valuesToSerialize); 
                    serialized = true;
                } 
                else { 
                    MethodInfo addMethodToUse = ChooseMethodByType(addMethods, valuesToSerialize);
                    if (addMethodToUse != null) { 
                        result = SerializeViaAdd(manager, targetExpression, targetType, addMethodToUse.GetParameters()[0], valuesToSerialize);
                        serialized = true;
                    }
                } 

                if (!serialized && originalCollection.GetType().IsSerializable) { 
                    result = SerializeToResourceExpression(manager, originalCollection, false); 
                }
            } 
            else {
                Trace("Collection has no values to serialize.");
            }
 
            return result;
        } 
 
        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.SerializeArray"]/*' />
        /// <devdoc> 
        ///     Serializes the given array.
        /// </devdoc>
        private CodeArrayCreateExpression SerializeArray(IDesignerSerializationManager manager, Type targetType, ICollection array, ICollection valuesToSerialize) {
 
            CodeArrayCreateExpression result = null;
 
            using (TraceScope("CollectionCodeDomSerializer::SerializeArray")) { 
                if (((Array)array).Rank != 1) {
                    TraceError("Cannot serialize arrays with rank > 1."); 
                    manager.ReportError(SR.GetString(SR.SerializerInvalidArrayRank, ((Array)array).Rank.ToString(CultureInfo.InvariantCulture)));
                }
                else {
                    // For an array, we need an array create expression.  First, get the array type 
                    //
                    Type elementType = targetType.GetElementType(); 
                    CodeTypeReference elementTypeRef = new CodeTypeReference(elementType); 

                    Trace("Array type: {0}", elementType.Name); 
                    Trace("Count: {0}", valuesToSerialize.Count);

                    // Now create an ArrayCreateExpression, and fill its initializers.
                    // 
                    CodeArrayCreateExpression arrayCreate = new CodeArrayCreateExpression();
 
                    arrayCreate.CreateType = elementTypeRef; 

                    bool arrayOk = true; 

                    foreach (object o in valuesToSerialize) {

                        // If this object is being privately inherited, it cannot be inside 
                        // this collection.  Since we're writing an entire array here, we
                        // cannot write any of it. 
                        // 
                        if (o is IComponent && TypeDescriptor.GetAttributes(o).Contains(InheritanceAttribute.InheritedReadOnly)) {
                            arrayOk = false; 
                            break;
                        }

                        CodeExpression expression = null; 

                        // VSWhidbey #266085: If there is an expression context on the stack at this point, 
                        // we need to fix up the ExpressionType on it to be the array element type. 
                        ExpressionContext newCxt = null;
                        ExpressionContext cxt = manager.Context[typeof(ExpressionContext)] as ExpressionContext; 

                        if (cxt != null) {
                            newCxt = new ExpressionContext(cxt.Expression, elementType, cxt.Owner);
                            manager.Context.Push(newCxt); 
                        }
 
                        try { 
                            expression = SerializeToExpression(manager, o);
                        } 
                        finally {
                            if (newCxt != null) {
                                Debug.Assert(manager.Context.Current == newCxt, "Context stack corrupted.");
                                manager.Context.Pop(); 
                            }
                        } 
 
                        if (expression is CodeExpression) {
                            if (o != null && o.GetType() != elementType) { 
                                expression = new CodeCastExpression(elementType, expression);
                            }

                            arrayCreate.Initializers.Add((CodeExpression)expression); 
                        }
                        else { 
                            arrayOk = false; 
                            break;
                        } 
                    }

                    if (arrayOk) {
                        result = arrayCreate; 
                    }
                } 
            } 

            return result; 
        }

        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.SerializeViaAdd"]/*' />
        /// <devdoc> 
        ///     Serializes the given collection by creating multiple calls to an Add method.
        /// </devdoc> 
        private object SerializeViaAdd( 
            IDesignerSerializationManager manager,
            CodeExpression targetExpression, 
            Type targetType,
            ParameterInfo parameter,
            ICollection valuesToSerialize) {
 
            CodeStatementCollection statements = new CodeStatementCollection();
            using (TraceScope("CollectionCodeDomSerializer::SerializeViaAdd")) { 
                Trace("Elements: {0}", valuesToSerialize.Count.ToString(CultureInfo.InvariantCulture)); 

                // Here we need to invoke Add once for each and every item in the collection. We can re-use the property 
                // reference and method reference, but we will need to recreate the invoke statement each time.
                //
                CodeMethodReferenceExpression methodRef = new CodeMethodReferenceExpression(targetExpression, "Add");
 
                if (valuesToSerialize.Count > 0) {
 
                    ExpressionContext cxt = manager.Context[typeof(ExpressionContext)] as ExpressionContext; 

                    foreach (object o in valuesToSerialize) { 
                        // If this object is being privately inherited, it cannot be inside
                        // this collection.
                        //
                        bool genCode = !(o is IComponent); 

                        if (!genCode) { 
                            InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(o)[typeof(InheritanceAttribute)]; 

                            if (ia != null) { 
                                if (ia.InheritanceLevel == InheritanceLevel.InheritedReadOnly)
                                    genCode = false;
                                else
                                    genCode = true; 
                            }
                            else { 
                                genCode = true; 
                            }
                        } 

                        Debug.Assert(genCode, "Why didn't GetCollectionDelta calculate the same thing?");
                        if (genCode) {
                            CodeMethodInvokeExpression statement = new CodeMethodInvokeExpression(); 

                            statement.Method = methodRef; 
 
                            CodeExpression serializedObj = null;
                            Type elementType = parameter.ParameterType; 

                            // VSWhidbey #266085: If there is an expression context on the stack at this point,
                            // we need to fix up the ExpressionType on it to be the element type.
                            ExpressionContext newCxt = null; 

                            if (cxt != null) { 
                                newCxt = new ExpressionContext(cxt.Expression, elementType, cxt.Owner); 
                                manager.Context.Push(newCxt);
                            } 

                            try {
                                serializedObj =  SerializeToExpression(manager, o);
                            } 
                            finally {
                                if (newCxt != null) { 
                                    Debug.Assert(manager.Context.Current == newCxt, "Context stack corrupted."); 
                                    manager.Context.Pop();
                                } 
                            }

                            if (o != null && !elementType.IsAssignableFrom(o.GetType()) && o.GetType().IsPrimitive) {
                                serializedObj = new CodeCastExpression(elementType, serializedObj); 
                            }
 
                            if (serializedObj != null) { 
                                statement.Parameters.Add(serializedObj);
                                statements.Add(statement); 
                            }
                        }
                    }
                } 
            }
 
            return statements; 
        }
 
        /// <include file='doc\CollectionCodeDomSerializer.uex' path='docs/doc[@for="CollectionCodeDomSerializer.SerializeViaAddRange"]/*' />
        /// <devdoc>
        ///     Serializes the given collection by creating an array and passing it to the AddRange method.
        /// </devdoc> 
        private object SerializeViaAddRange(
            IDesignerSerializationManager manager, 
            CodeExpression targetExpression, 
            Type targetType,
            ParameterInfo parameter, 
            ICollection valuesToSerialize) {


            CodeStatementCollection statements = new CodeStatementCollection(); 

            using (TraceScope("CollectionCodeDomSerializer::SerializeViaAddRange")) { 
                Trace("Elements: {0}", valuesToSerialize.Count.ToString(CultureInfo.InvariantCulture)); 

                if (valuesToSerialize.Count > 0) { 

                    ArrayList arrayList = new ArrayList(valuesToSerialize.Count);
                    ExpressionContext cxt = manager.Context[typeof(ExpressionContext)] as ExpressionContext;
                    Type elementType = parameter.ParameterType.GetElementType(); 

                    foreach (object o in valuesToSerialize) { 
 
                        // If this object is being privately inherited, it cannot be inside
                        // this collection. 
                        //
                        bool genCode = !(o is IComponent);

                        if (!genCode) { 
                            InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(o)[typeof(InheritanceAttribute)];
 
                            if (ia != null) { 
                                if (ia.InheritanceLevel == InheritanceLevel.InheritedReadOnly)
                                    genCode = false; 
                                else
                                    genCode = true;
                            }
                            else { 
                                genCode = true;
                            } 
                        } 

                        Debug.Assert(genCode, "Why didn't GetCollectionDelta calculate the same thing?"); 
                        if (genCode) {
                            CodeExpression exp = null;

                            // VSWhidbey #266085: If there is an expression context on the stack at this point, 
                            // we need to fix up the ExpressionType on it to be the element type.
                            ExpressionContext newCxt = null; 
 
                            if (cxt != null) {
                                newCxt = new ExpressionContext(cxt.Expression, elementType, cxt.Owner); 
                                manager.Context.Push(newCxt);
                            }

                            try { 
                                exp =  SerializeToExpression(manager, o);
                            } 
                            finally { 
                                if (newCxt != null) {
                                    Debug.Assert(manager.Context.Current == newCxt, "Context stack corrupted."); 
                                    manager.Context.Pop();
                                }
                            }
 
                            if (exp != null) {
                                // Check to see if we need a cast 
                                if (o != null && !elementType.IsAssignableFrom(o.GetType())) { 
                                    exp = new CodeCastExpression(elementType, exp);
                                } 

                                arrayList.Add(exp);
                            }
                        } 
                    }
 
                    if (arrayList.Count > 0) { 
                        // Now convert the array list into an array create expression.
                        // 
                        CodeTypeReference elementTypeRef = new CodeTypeReference(elementType);

                        // Now create an ArrayCreateExpression, and fill its initializers.
                        // 
                        CodeArrayCreateExpression arrayCreate = new CodeArrayCreateExpression();
 
                        arrayCreate.CreateType = elementTypeRef; 
                        foreach (CodeExpression exp in arrayList) {
                            arrayCreate.Initializers.Add(exp); 
                        }

                        CodeMethodReferenceExpression methodRef = new CodeMethodReferenceExpression(targetExpression, "AddRange");
                        CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(); 

                        methodInvoke.Method = methodRef; 
                        methodInvoke.Parameters.Add(arrayCreate); 
                        statements.Add(new CodeExpressionStatement(methodInvoke));
                    } 
                }
            }

            return statements; 
        }
 
        /// <devdoc> 
        ///     Returns true if we should clear the collection contents.
        /// </devdoc> 
        private bool ShouldClearCollection(IDesignerSerializationManager manager, ICollection collection) {

            bool shouldClear = false;
 
            PropertyDescriptor clearProp = manager.Properties["ClearCollections"];
            if (clearProp != null && clearProp.PropertyType == typeof(bool) && ((bool)clearProp.GetValue(manager) == true)) { 
                shouldClear = true; 
            }
 
            if (!shouldClear) {
                SerializeAbsoluteContext absolute = (SerializeAbsoluteContext)manager.Context[typeof(SerializeAbsoluteContext)];
                PropertyDescriptor prop = manager.Context[typeof(PropertyDescriptor)] as PropertyDescriptor;
                if (absolute != null && absolute.ShouldSerialize(prop)) { 
                    shouldClear = true;
                } 
            } 

            if (shouldClear) { 
                MethodInfo clearMethod = TypeDescriptor.GetReflectionType(collection).GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null);

                if (clearMethod == null || !MethodSupportsSerialization(clearMethod)) {
                    shouldClear = false; 
                }
            } 
 
            return shouldClear;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
