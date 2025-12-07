//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.IO;
    using System.Text; 
    using System.Diagnostics;
    using System.Runtime.Serialization; 
 

    /// <summary> 
    /// </summary>
    internal class PropertyReferenceSerializer {

        private const string applicationSettingsPrefix = "ApplicationSettings"; 
        private const string appConfigPrefix = "AppConfig";
 
        // private constructor to avoid class being instantiated. 
 		private PropertyReferenceSerializer() { }
 
        internal static string Serialize(CodePropertyReferenceExpression expression) {
            if (IsWellKnownApplicationSettingsExpression(expression)) {
                return SerializeApplicationSettingsExpression(expression);
            } 

            if (IsWellKnownAppConfigExpression(expression)) { 
                return SerializeAppConfigExpression(expression); 
            }
 
            Debug.Assert(false, "Unable to recognize Connection Property Reference for serialization, falling back to SOAPFormatter.");
            return SerializeWithSoapFormatter(expression);
        }
 
        internal static CodePropertyReferenceExpression Deserialize(string expressionString) {
            string[] expressionParts = expressionString.Split('.'); 
            if (expressionParts != null && expressionParts.Length > 0) { 
                if (StringUtil.EqualValue(expressionParts[0], applicationSettingsPrefix)) {
                    return DeserializeApplicationSettingsExpression(expressionParts); 
                }

                if (StringUtil.EqualValue(expressionParts[0], appConfigPrefix)) {
                    return DeserializeAppConfigExpression(expressionParts); 
                }
            } 
 
            // if our custom deserialization failed, fall back to the SOAP Formatter; this will also provide backward compatibility
            // with Beta2 
            UTF8Encoding encoding = new UTF8Encoding();
            MemoryStream stream = new MemoryStream(encoding.GetBytes(expressionString));

            IFormatter formatter = new System.Runtime.Serialization.Formatters.Soap.SoapFormatter(); 
            return (CodePropertyReferenceExpression)formatter.Deserialize(stream);
        } 
 

 
        private static string SerializeWithSoapFormatter(CodePropertyReferenceExpression expression) {
            MemoryStream stream = new MemoryStream();

            IFormatter formatter = new System.Runtime.Serialization.Formatters.Soap.SoapFormatter(); 
            formatter.Serialize(stream, expression);
            if (stream.Length > (long)int.MaxValue) { 
                throw new InternalException("Serialized property expression is too long."); 
            }
 
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(buffer, 0, (int)stream.Length); 

            return encoding.GetString(buffer); 
        } 

        private static string SerializeApplicationSettingsExpression(CodePropertyReferenceExpression expression) { 
            string serializationString = expression.PropertyName;

            CodePropertyReferenceExpression targetObject = (CodePropertyReferenceExpression)expression.TargetObject;
            serializationString = targetObject.PropertyName + "." + serializationString; 

            CodeTypeReferenceExpression typeReference = (CodeTypeReferenceExpression)targetObject.TargetObject; 
            serializationString = typeReference.Type.Options.ToString() + "." + serializationString; 
            serializationString = typeReference.Type.BaseType + "." + serializationString;
 
            serializationString = applicationSettingsPrefix + "." + serializationString;

            return serializationString;
        } 

        private static string SerializeAppConfigExpression(CodePropertyReferenceExpression expression) { 
            string serializationString = expression.PropertyName; 

            CodeIndexerExpression targetObject = (CodeIndexerExpression)expression.TargetObject; 
            string indexValue = ((CodePrimitiveExpression)targetObject.Indices[0]).Value as string;
            serializationString = indexValue + "." + serializationString;

            CodePropertyReferenceExpression indexTarget = (CodePropertyReferenceExpression)targetObject.TargetObject; 
            serializationString = indexTarget.PropertyName + "." + serializationString;
 
            CodeTypeReferenceExpression typeReference = (CodeTypeReferenceExpression)indexTarget.TargetObject; 
            serializationString = typeReference.Type.Options.ToString() + "." + serializationString;
            serializationString = typeReference.Type.BaseType + "." + serializationString; 

            serializationString = appConfigPrefix + "." + serializationString;

            return serializationString; 
        }
 
        private static bool IsWellKnownApplicationSettingsExpression(CodePropertyReferenceExpression expression) { 
            if (expression.UserData != null && expression.UserData.Count > 0) {
                return false; 
            }
            if (!(expression.TargetObject is CodePropertyReferenceExpression)) {
                return false;
            } 

            CodePropertyReferenceExpression targetObject = (CodePropertyReferenceExpression)expression.TargetObject; 
            if (targetObject.UserData != null && targetObject.UserData.Count > 0) { 
                return false;
            } 
            if (!(targetObject.TargetObject is CodeTypeReferenceExpression)) {
                return false;
            }
 
            CodeTypeReferenceExpression typeReference = (CodeTypeReferenceExpression)targetObject.TargetObject;
            if (typeReference.UserData != null && typeReference.UserData.Count > 0) { 
                return false; 
            }
            CodeTypeReference type = typeReference.Type; 
            if (type.UserData != null && type.UserData.Count > 0) {
                return false;
            }
            if (type.TypeArguments != null && type.TypeArguments.Count > 0) { 
                return false;
            } 
            if (type.ArrayElementType != null || type.ArrayRank > 0) { 
                return false;
            } 

            return true;
        }
 
        private static bool IsWellKnownAppConfigExpression(CodePropertyReferenceExpression expression) {
            if (expression.UserData != null && expression.UserData.Count > 0) { 
                return false; 
            }
            if (!(expression.TargetObject is CodeIndexerExpression)) { 
                return false;
            }

            CodeIndexerExpression targetObject = (CodeIndexerExpression)expression.TargetObject; 
            if (targetObject.UserData != null && targetObject.UserData.Count > 0) {
                return false; 
            } 
            if (targetObject.Indices == null || targetObject.Indices.Count != 1 || !(targetObject.Indices[0] is CodePrimitiveExpression)) {
                return false; 
            }
            if (!(((CodePrimitiveExpression)targetObject.Indices[0]).Value is string)) {
                return false;
            } 
            if (!(targetObject.TargetObject is CodePropertyReferenceExpression)) {
                return false; 
            } 

            CodePropertyReferenceExpression indexTarget = (CodePropertyReferenceExpression)targetObject.TargetObject; 
            if (indexTarget.UserData != null && indexTarget.UserData.Count > 0) {
                return false;
            }
            if (!(indexTarget.TargetObject is CodeTypeReferenceExpression)) { 
                return false;
            } 
 
            CodeTypeReferenceExpression typeReference = (CodeTypeReferenceExpression)indexTarget.TargetObject;
            if (typeReference.UserData != null && typeReference.UserData.Count > 0) { 
                return false;
            }
            CodeTypeReference type = typeReference.Type;
            if (type.UserData != null && type.UserData.Count > 0) { 
                return false;
            } 
            if (type.TypeArguments != null && type.TypeArguments.Count > 0) { 
                return false;
            } 
            if (type.ArrayElementType != null || type.ArrayRank > 0) {
                return false;
            }
 
            return true;
        } 
 
        private static CodePropertyReferenceExpression DeserializeApplicationSettingsExpression(string[] expressionParts) {
            int currentPart = expressionParts.Length - 1; 

            CodePropertyReferenceExpression outerExpression = new CodePropertyReferenceExpression();
            outerExpression.PropertyName = expressionParts[currentPart];
            currentPart--; 

            CodePropertyReferenceExpression targetObject = new CodePropertyReferenceExpression(); 
            outerExpression.TargetObject = targetObject; 
            targetObject.PropertyName = expressionParts[currentPart];
            currentPart--; 

            CodeTypeReferenceExpression targetType = new CodeTypeReferenceExpression();
            targetObject.TargetObject = targetType;
            targetType.Type.Options = (CodeTypeReferenceOptions)Enum.Parse(typeof(CodeTypeReferenceOptions), expressionParts[currentPart]); 
            currentPart--;
            targetType.Type.BaseType = expressionParts[currentPart]; 
            currentPart--; 
            while (currentPart > 0) {
                targetType.Type.BaseType = expressionParts[currentPart] + "." + targetType.Type.BaseType; 
                currentPart--;
            }

            return outerExpression; 
        }
 
        private static CodePropertyReferenceExpression DeserializeAppConfigExpression(string[] expressionParts) { 
            int currentPart = expressionParts.Length - 1;
 
            CodePropertyReferenceExpression outerExpression = new CodePropertyReferenceExpression();
            outerExpression.PropertyName = expressionParts[currentPart];
            currentPart--;
 
            CodeIndexerExpression indexerExpression = new CodeIndexerExpression();
            outerExpression.TargetObject = indexerExpression; 
            indexerExpression.Indices.Add(new CodePrimitiveExpression(expressionParts[currentPart])); 
            currentPart--;
 
            CodePropertyReferenceExpression indexTarget = new CodePropertyReferenceExpression();
            indexerExpression.TargetObject = indexTarget;
            indexTarget.PropertyName = expressionParts[currentPart];
            currentPart--; 

            CodeTypeReferenceExpression targetType = new CodeTypeReferenceExpression(); 
            indexTarget.TargetObject = targetType; 
            targetType.Type.Options = (CodeTypeReferenceOptions)Enum.Parse(typeof(CodeTypeReferenceOptions), expressionParts[currentPart]);
            currentPart--; 
            targetType.Type.BaseType = expressionParts[currentPart];
            currentPart--;
            while (currentPart > 0) {
                targetType.Type.BaseType = expressionParts[currentPart] + "." + targetType.Type.BaseType; 
                currentPart--;
            } 
 
            return outerExpression;
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.IO;
    using System.Text; 
    using System.Diagnostics;
    using System.Runtime.Serialization; 
 

    /// <summary> 
    /// </summary>
    internal class PropertyReferenceSerializer {

        private const string applicationSettingsPrefix = "ApplicationSettings"; 
        private const string appConfigPrefix = "AppConfig";
 
        // private constructor to avoid class being instantiated. 
 		private PropertyReferenceSerializer() { }
 
        internal static string Serialize(CodePropertyReferenceExpression expression) {
            if (IsWellKnownApplicationSettingsExpression(expression)) {
                return SerializeApplicationSettingsExpression(expression);
            } 

            if (IsWellKnownAppConfigExpression(expression)) { 
                return SerializeAppConfigExpression(expression); 
            }
 
            Debug.Assert(false, "Unable to recognize Connection Property Reference for serialization, falling back to SOAPFormatter.");
            return SerializeWithSoapFormatter(expression);
        }
 
        internal static CodePropertyReferenceExpression Deserialize(string expressionString) {
            string[] expressionParts = expressionString.Split('.'); 
            if (expressionParts != null && expressionParts.Length > 0) { 
                if (StringUtil.EqualValue(expressionParts[0], applicationSettingsPrefix)) {
                    return DeserializeApplicationSettingsExpression(expressionParts); 
                }

                if (StringUtil.EqualValue(expressionParts[0], appConfigPrefix)) {
                    return DeserializeAppConfigExpression(expressionParts); 
                }
            } 
 
            // if our custom deserialization failed, fall back to the SOAP Formatter; this will also provide backward compatibility
            // with Beta2 
            UTF8Encoding encoding = new UTF8Encoding();
            MemoryStream stream = new MemoryStream(encoding.GetBytes(expressionString));

            IFormatter formatter = new System.Runtime.Serialization.Formatters.Soap.SoapFormatter(); 
            return (CodePropertyReferenceExpression)formatter.Deserialize(stream);
        } 
 

 
        private static string SerializeWithSoapFormatter(CodePropertyReferenceExpression expression) {
            MemoryStream stream = new MemoryStream();

            IFormatter formatter = new System.Runtime.Serialization.Formatters.Soap.SoapFormatter(); 
            formatter.Serialize(stream, expression);
            if (stream.Length > (long)int.MaxValue) { 
                throw new InternalException("Serialized property expression is too long."); 
            }
 
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(buffer, 0, (int)stream.Length); 

            return encoding.GetString(buffer); 
        } 

        private static string SerializeApplicationSettingsExpression(CodePropertyReferenceExpression expression) { 
            string serializationString = expression.PropertyName;

            CodePropertyReferenceExpression targetObject = (CodePropertyReferenceExpression)expression.TargetObject;
            serializationString = targetObject.PropertyName + "." + serializationString; 

            CodeTypeReferenceExpression typeReference = (CodeTypeReferenceExpression)targetObject.TargetObject; 
            serializationString = typeReference.Type.Options.ToString() + "." + serializationString; 
            serializationString = typeReference.Type.BaseType + "." + serializationString;
 
            serializationString = applicationSettingsPrefix + "." + serializationString;

            return serializationString;
        } 

        private static string SerializeAppConfigExpression(CodePropertyReferenceExpression expression) { 
            string serializationString = expression.PropertyName; 

            CodeIndexerExpression targetObject = (CodeIndexerExpression)expression.TargetObject; 
            string indexValue = ((CodePrimitiveExpression)targetObject.Indices[0]).Value as string;
            serializationString = indexValue + "." + serializationString;

            CodePropertyReferenceExpression indexTarget = (CodePropertyReferenceExpression)targetObject.TargetObject; 
            serializationString = indexTarget.PropertyName + "." + serializationString;
 
            CodeTypeReferenceExpression typeReference = (CodeTypeReferenceExpression)indexTarget.TargetObject; 
            serializationString = typeReference.Type.Options.ToString() + "." + serializationString;
            serializationString = typeReference.Type.BaseType + "." + serializationString; 

            serializationString = appConfigPrefix + "." + serializationString;

            return serializationString; 
        }
 
        private static bool IsWellKnownApplicationSettingsExpression(CodePropertyReferenceExpression expression) { 
            if (expression.UserData != null && expression.UserData.Count > 0) {
                return false; 
            }
            if (!(expression.TargetObject is CodePropertyReferenceExpression)) {
                return false;
            } 

            CodePropertyReferenceExpression targetObject = (CodePropertyReferenceExpression)expression.TargetObject; 
            if (targetObject.UserData != null && targetObject.UserData.Count > 0) { 
                return false;
            } 
            if (!(targetObject.TargetObject is CodeTypeReferenceExpression)) {
                return false;
            }
 
            CodeTypeReferenceExpression typeReference = (CodeTypeReferenceExpression)targetObject.TargetObject;
            if (typeReference.UserData != null && typeReference.UserData.Count > 0) { 
                return false; 
            }
            CodeTypeReference type = typeReference.Type; 
            if (type.UserData != null && type.UserData.Count > 0) {
                return false;
            }
            if (type.TypeArguments != null && type.TypeArguments.Count > 0) { 
                return false;
            } 
            if (type.ArrayElementType != null || type.ArrayRank > 0) { 
                return false;
            } 

            return true;
        }
 
        private static bool IsWellKnownAppConfigExpression(CodePropertyReferenceExpression expression) {
            if (expression.UserData != null && expression.UserData.Count > 0) { 
                return false; 
            }
            if (!(expression.TargetObject is CodeIndexerExpression)) { 
                return false;
            }

            CodeIndexerExpression targetObject = (CodeIndexerExpression)expression.TargetObject; 
            if (targetObject.UserData != null && targetObject.UserData.Count > 0) {
                return false; 
            } 
            if (targetObject.Indices == null || targetObject.Indices.Count != 1 || !(targetObject.Indices[0] is CodePrimitiveExpression)) {
                return false; 
            }
            if (!(((CodePrimitiveExpression)targetObject.Indices[0]).Value is string)) {
                return false;
            } 
            if (!(targetObject.TargetObject is CodePropertyReferenceExpression)) {
                return false; 
            } 

            CodePropertyReferenceExpression indexTarget = (CodePropertyReferenceExpression)targetObject.TargetObject; 
            if (indexTarget.UserData != null && indexTarget.UserData.Count > 0) {
                return false;
            }
            if (!(indexTarget.TargetObject is CodeTypeReferenceExpression)) { 
                return false;
            } 
 
            CodeTypeReferenceExpression typeReference = (CodeTypeReferenceExpression)indexTarget.TargetObject;
            if (typeReference.UserData != null && typeReference.UserData.Count > 0) { 
                return false;
            }
            CodeTypeReference type = typeReference.Type;
            if (type.UserData != null && type.UserData.Count > 0) { 
                return false;
            } 
            if (type.TypeArguments != null && type.TypeArguments.Count > 0) { 
                return false;
            } 
            if (type.ArrayElementType != null || type.ArrayRank > 0) {
                return false;
            }
 
            return true;
        } 
 
        private static CodePropertyReferenceExpression DeserializeApplicationSettingsExpression(string[] expressionParts) {
            int currentPart = expressionParts.Length - 1; 

            CodePropertyReferenceExpression outerExpression = new CodePropertyReferenceExpression();
            outerExpression.PropertyName = expressionParts[currentPart];
            currentPart--; 

            CodePropertyReferenceExpression targetObject = new CodePropertyReferenceExpression(); 
            outerExpression.TargetObject = targetObject; 
            targetObject.PropertyName = expressionParts[currentPart];
            currentPart--; 

            CodeTypeReferenceExpression targetType = new CodeTypeReferenceExpression();
            targetObject.TargetObject = targetType;
            targetType.Type.Options = (CodeTypeReferenceOptions)Enum.Parse(typeof(CodeTypeReferenceOptions), expressionParts[currentPart]); 
            currentPart--;
            targetType.Type.BaseType = expressionParts[currentPart]; 
            currentPart--; 
            while (currentPart > 0) {
                targetType.Type.BaseType = expressionParts[currentPart] + "." + targetType.Type.BaseType; 
                currentPart--;
            }

            return outerExpression; 
        }
 
        private static CodePropertyReferenceExpression DeserializeAppConfigExpression(string[] expressionParts) { 
            int currentPart = expressionParts.Length - 1;
 
            CodePropertyReferenceExpression outerExpression = new CodePropertyReferenceExpression();
            outerExpression.PropertyName = expressionParts[currentPart];
            currentPart--;
 
            CodeIndexerExpression indexerExpression = new CodeIndexerExpression();
            outerExpression.TargetObject = indexerExpression; 
            indexerExpression.Indices.Add(new CodePrimitiveExpression(expressionParts[currentPart])); 
            currentPart--;
 
            CodePropertyReferenceExpression indexTarget = new CodePropertyReferenceExpression();
            indexerExpression.TargetObject = indexTarget;
            indexTarget.PropertyName = expressionParts[currentPart];
            currentPart--; 

            CodeTypeReferenceExpression targetType = new CodeTypeReferenceExpression(); 
            indexTarget.TargetObject = targetType; 
            targetType.Type.Options = (CodeTypeReferenceOptions)Enum.Parse(typeof(CodeTypeReferenceOptions), expressionParts[currentPart]);
            currentPart--; 
            targetType.Type.BaseType = expressionParts[currentPart];
            currentPart--;
            while (currentPart > 0) {
                targetType.Type.BaseType = expressionParts[currentPart] + "." + targetType.Type.BaseType; 
                currentPart--;
            } 
 
            return outerExpression;
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
