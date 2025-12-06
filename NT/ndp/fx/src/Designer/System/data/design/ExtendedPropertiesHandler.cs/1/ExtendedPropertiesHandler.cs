 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
 
    using System;
    using System.CodeDom;
    using System.Collections;
    using System.ComponentModel; 
    using System.Data;
 	using System.Design; 
 
	internal sealed class ExtendedPropertiesHandler {
        private static TypedDataSourceCodeGenerator codeGenerator = null; 
        private static DataSourceComponent targetObject = null;

        // private constructor to avoid class being instantiated.
        private ExtendedPropertiesHandler() { } 

        internal static TypedDataSourceCodeGenerator CodeGenerator { 
            set { 
                codeGenerator = value;
            } 
        }

        internal static void AddExtendedProperties(DataSourceComponent targetObj, CodeExpression addTarget, IList statementCollection, Hashtable extendedProperties) {
            if(extendedProperties == null) { 
                return;
            } 
            if (addTarget == null) { 
                throw new InternalException("ExtendedPropertiesHandler.AddExtendedProperties: addTarget cannot be null");
            } 
            if (statementCollection == null) {
                throw new InternalException("ExtendedPropertiesHandler.AddExtendedProperties: statementCollection cannot be null");
            }
            if (codeGenerator == null) { 
                throw new InternalException("ExtendedPropertiesHandler.AddExtendedProperties: codeGenerator cannot be null");
            } 
            if (targetObj == null) { 
                throw new InternalException("ExtendedPropertiesHandler.AddExtendedProperties: targetObject cannot be null");
            } 

            targetObject = targetObj;

            if(codeGenerator.GenerateExtendedProperties) { 
                GenerateProperties(addTarget, statementCollection, extendedProperties);
            } 
            else { 
                // Generating extended properties could break compatibility with typed DataSets v1, so, if we're not explicitly
                // requested to do so, we generate only the ones used for naming (added by us) 
                SortedList namingProperties = new SortedList(new Comparer(System.Globalization.CultureInfo.InvariantCulture));

                foreach(string extPropName in targetObject.NamingPropertyNames) {
                    string extPropValue = extendedProperties[extPropName] as string; 
                    if(!StringUtil.Empty(extPropValue)) {
                        namingProperties.Add((string)extPropName, extPropValue); 
                    } 
                }
 
                GenerateProperties(addTarget, statementCollection, namingProperties);
            }
        }
 
        private static void GenerateProperties(CodeExpression addTarget, IList statementCollection, ICollection extendedProperties) {
            if (extendedProperties != null) { 
                IDictionaryEnumerator enumerator = (IDictionaryEnumerator) extendedProperties.GetEnumerator(); 
                if (enumerator != null) {
                    enumerator.Reset(); 
                    while (enumerator.MoveNext()) {
                        string key = enumerator.Key as string;
                        string val = enumerator.Value as string;
 
                        if (key == null || val == null) {
                            codeGenerator.ProblemList.Add( new DSGeneratorProblem(SR.GetString(SR.CG_UnableToReadExtProperties), ProblemSeverity.NonFatalError, targetObject) ); 
 
                            continue;
                        } 
                        else {
                            //\\ <addTarget>.ExtendedProperties.Add(<key>, <value>);
                            statementCollection.Add(
								CodeGenHelper.Stm( 
									CodeGenHelper.MethodCall(
 										CodeGenHelper.Property( 
											addTarget, 
 											"ExtendedProperties"
 										), 
										"Add",
 										new CodeExpression[] {
											CodeGenHelper.Primitive(key),
											CodeGenHelper.Primitive(val) 
										}
 									) 
								) 
                            );
                        } 
                    }
                }
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
    using System.CodeDom;
    using System.Collections;
    using System.ComponentModel; 
    using System.Data;
 	using System.Design; 
 
	internal sealed class ExtendedPropertiesHandler {
        private static TypedDataSourceCodeGenerator codeGenerator = null; 
        private static DataSourceComponent targetObject = null;

        // private constructor to avoid class being instantiated.
        private ExtendedPropertiesHandler() { } 

        internal static TypedDataSourceCodeGenerator CodeGenerator { 
            set { 
                codeGenerator = value;
            } 
        }

        internal static void AddExtendedProperties(DataSourceComponent targetObj, CodeExpression addTarget, IList statementCollection, Hashtable extendedProperties) {
            if(extendedProperties == null) { 
                return;
            } 
            if (addTarget == null) { 
                throw new InternalException("ExtendedPropertiesHandler.AddExtendedProperties: addTarget cannot be null");
            } 
            if (statementCollection == null) {
                throw new InternalException("ExtendedPropertiesHandler.AddExtendedProperties: statementCollection cannot be null");
            }
            if (codeGenerator == null) { 
                throw new InternalException("ExtendedPropertiesHandler.AddExtendedProperties: codeGenerator cannot be null");
            } 
            if (targetObj == null) { 
                throw new InternalException("ExtendedPropertiesHandler.AddExtendedProperties: targetObject cannot be null");
            } 

            targetObject = targetObj;

            if(codeGenerator.GenerateExtendedProperties) { 
                GenerateProperties(addTarget, statementCollection, extendedProperties);
            } 
            else { 
                // Generating extended properties could break compatibility with typed DataSets v1, so, if we're not explicitly
                // requested to do so, we generate only the ones used for naming (added by us) 
                SortedList namingProperties = new SortedList(new Comparer(System.Globalization.CultureInfo.InvariantCulture));

                foreach(string extPropName in targetObject.NamingPropertyNames) {
                    string extPropValue = extendedProperties[extPropName] as string; 
                    if(!StringUtil.Empty(extPropValue)) {
                        namingProperties.Add((string)extPropName, extPropValue); 
                    } 
                }
 
                GenerateProperties(addTarget, statementCollection, namingProperties);
            }
        }
 
        private static void GenerateProperties(CodeExpression addTarget, IList statementCollection, ICollection extendedProperties) {
            if (extendedProperties != null) { 
                IDictionaryEnumerator enumerator = (IDictionaryEnumerator) extendedProperties.GetEnumerator(); 
                if (enumerator != null) {
                    enumerator.Reset(); 
                    while (enumerator.MoveNext()) {
                        string key = enumerator.Key as string;
                        string val = enumerator.Value as string;
 
                        if (key == null || val == null) {
                            codeGenerator.ProblemList.Add( new DSGeneratorProblem(SR.GetString(SR.CG_UnableToReadExtProperties), ProblemSeverity.NonFatalError, targetObject) ); 
 
                            continue;
                        } 
                        else {
                            //\\ <addTarget>.ExtendedProperties.Add(<key>, <value>);
                            statementCollection.Add(
								CodeGenHelper.Stm( 
									CodeGenHelper.MethodCall(
 										CodeGenHelper.Property( 
											addTarget, 
 											"ExtendedProperties"
 										), 
										"Add",
 										new CodeExpression[] {
											CodeGenHelper.Primitive(key),
											CodeGenHelper.Primitive(val) 
										}
 									) 
								) 
                            );
                        } 
                    }
                }
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
