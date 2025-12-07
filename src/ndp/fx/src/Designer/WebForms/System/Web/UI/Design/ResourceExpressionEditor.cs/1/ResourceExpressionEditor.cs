//------------------------------------------------------------------------------ 
// <copyright file="ResourceExpressionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Web.Compilation;

    /// <include file='doc\ResourceExpressionEditor.uex' path='docs/doc[@for="ResourceExpressionEditor"]/*' /> 
    public class ResourceExpressionEditor : ExpressionEditor {
 
        /// <include file='doc\ResourceExpressionEditor.uex' path='docs/doc[@for="ResourceExpressionEditor.EvaluateExpression"]/*' /> 
        public override object EvaluateExpression(string expression, object parseTimeData, Type propertyType, IServiceProvider serviceProvider) {
            ResourceExpressionFields fields; 
            if (parseTimeData is ResourceExpressionFields) {
                fields = (ResourceExpressionFields)parseTimeData;
            }
            else { 
                fields = ResourceExpressionBuilder.ParseExpression(expression);
            } 
 
            if (String.IsNullOrEmpty(fields.ResourceKey)) {
                return null; 
            }

            object resource = null;
            DesignTimeResourceProviderFactory resourceProviderFactory = ControlDesigner.GetDesignTimeResourceProviderFactory(serviceProvider); 
            IResourceProvider resProvider;
            if (String.IsNullOrEmpty(fields.ClassKey)) { 
                resProvider = resourceProviderFactory.CreateDesignTimeLocalResourceProvider(serviceProvider); 
            }
            else { 
                resProvider = resourceProviderFactory.CreateDesignTimeGlobalResourceProvider(serviceProvider, fields.ClassKey);
            }
            if (resProvider != null) {
                resource = resProvider.GetObject(fields.ResourceKey, System.Globalization.CultureInfo.InvariantCulture); 
            }
 
            if (resource != null) { 
                Type resourceType = resource.GetType();
                if (!propertyType.IsAssignableFrom(resourceType)) { 
                    TypeConverter converter = TypeDescriptor.GetConverter(propertyType);
                    if ((converter != null) && converter.CanConvertFrom(resourceType)) {
                        return converter.ConvertFrom(resource);
                    } 
                }
            } 
            return resource; 
        }
 
        public override ExpressionEditorSheet GetExpressionEditorSheet(string expression, IServiceProvider serviceProvider) {
            return new ResourceExpressionEditorSheet(expression, serviceProvider);
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ResourceExpressionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Web.Compilation;

    /// <include file='doc\ResourceExpressionEditor.uex' path='docs/doc[@for="ResourceExpressionEditor"]/*' /> 
    public class ResourceExpressionEditor : ExpressionEditor {
 
        /// <include file='doc\ResourceExpressionEditor.uex' path='docs/doc[@for="ResourceExpressionEditor.EvaluateExpression"]/*' /> 
        public override object EvaluateExpression(string expression, object parseTimeData, Type propertyType, IServiceProvider serviceProvider) {
            ResourceExpressionFields fields; 
            if (parseTimeData is ResourceExpressionFields) {
                fields = (ResourceExpressionFields)parseTimeData;
            }
            else { 
                fields = ResourceExpressionBuilder.ParseExpression(expression);
            } 
 
            if (String.IsNullOrEmpty(fields.ResourceKey)) {
                return null; 
            }

            object resource = null;
            DesignTimeResourceProviderFactory resourceProviderFactory = ControlDesigner.GetDesignTimeResourceProviderFactory(serviceProvider); 
            IResourceProvider resProvider;
            if (String.IsNullOrEmpty(fields.ClassKey)) { 
                resProvider = resourceProviderFactory.CreateDesignTimeLocalResourceProvider(serviceProvider); 
            }
            else { 
                resProvider = resourceProviderFactory.CreateDesignTimeGlobalResourceProvider(serviceProvider, fields.ClassKey);
            }
            if (resProvider != null) {
                resource = resProvider.GetObject(fields.ResourceKey, System.Globalization.CultureInfo.InvariantCulture); 
            }
 
            if (resource != null) { 
                Type resourceType = resource.GetType();
                if (!propertyType.IsAssignableFrom(resourceType)) { 
                    TypeConverter converter = TypeDescriptor.GetConverter(propertyType);
                    if ((converter != null) && converter.CanConvertFrom(resourceType)) {
                        return converter.ConvertFrom(resource);
                    } 
                }
            } 
            return resource; 
        }
 
        public override ExpressionEditorSheet GetExpressionEditorSheet(string expression, IServiceProvider serviceProvider) {
            return new ResourceExpressionEditorSheet(expression, serviceProvider);
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
