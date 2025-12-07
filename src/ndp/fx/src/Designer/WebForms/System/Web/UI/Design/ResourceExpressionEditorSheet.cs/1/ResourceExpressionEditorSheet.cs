//------------------------------------------------------------------------------ 
// <copyright file="ResourceExpressionEditorSheet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System.Collections; 
    using System.ComponentModel;
    using System.Design; 
    using System.Diagnostics;

    /// <summary>
    /// Summary description for ResourceExpressionEditorSheet. 
    /// </summary>
    public class ResourceExpressionEditorSheet : ExpressionEditorSheet { 
        private string _classKey; 
        private string _resourceKey;
 
        public ResourceExpressionEditorSheet(string expression, IServiceProvider serviceProvider) : base(serviceProvider) {
            // Parse the existing expression if it exists;
            if (!String.IsNullOrEmpty(expression)) {
                ResourceExpressionFields fields = ParseExpressionInternal(expression); 
                ClassKey = fields.ClassKey;
                ResourceKey = fields.ResourceKey; 
            } 
        }
 
        [DefaultValue("")]
        [SRDescription(SR.ResourceExpressionEditorSheet_ClassKey)]
        public string ClassKey {
            get { 
                if (_classKey == null) {
                    return String.Empty; 
                } 

                return _classKey; 
            }
            set {
                _classKey = value;
            } 
        }
 
        public override bool IsValid { 
            get {
                return !String.IsNullOrEmpty(ResourceKey); 
            }
        }

        [DefaultValue("")] 
        [SRDescription(SR.ResourceExpressionEditorSheet_ResourceKey)]
        [TypeConverter(typeof(ResourceKeyTypeConverter))] 
        public string ResourceKey { 
            get {
                if (_resourceKey == null) { 
                    return String.Empty;
                }

                return _resourceKey; 
            }
            set { 
                _resourceKey = value; 
            }
        } 

        public override string GetExpression() {
            string expression = String.Empty;
            if (!String.IsNullOrEmpty(_classKey)) { 
                return _classKey + ", " + _resourceKey;
            } 
            else { 
                return _resourceKey;
            } 
        }

        // The following syntaxes are accepted for the expression
        //      resourceKey 
        //      classKey, resourceKey
        // 
        private static ResourceExpressionFields ParseExpressionInternal(string expression) { 

            ResourceExpressionFields fields = new ResourceExpressionFields(); 

            int len = expression.Length;

            // Split the comma separated string 
            string[] parts = expression.Split(',');
 
            int numOfParts = parts.Length; 

            if (numOfParts > 2) return null; 

            if (numOfParts == 1) {
                fields.ResourceKey = parts[0].Trim();
            } 
            else {
                fields.ClassKey = parts[0].Trim(); 
                fields.ResourceKey = parts[1].Trim(); 
            }
 
            return fields;
        }

        internal class ResourceExpressionFields { 
            internal string ClassKey;
            internal string ResourceKey; 
        } 

        private class ResourceKeyTypeConverter : StringConverter { 

            private static ICollection GetResourceKeys(IServiceProvider serviceProvider, string classKey) {
                DesignTimeResourceProviderFactory resourceProviderFactory = ControlDesigner.GetDesignTimeResourceProviderFactory(serviceProvider);
                System.Web.Compilation.IResourceProvider resProvider; 
                if (String.IsNullOrEmpty(classKey)) {
                    resProvider = resourceProviderFactory.CreateDesignTimeLocalResourceProvider(serviceProvider); 
                } 
                else {
                    resProvider = resourceProviderFactory.CreateDesignTimeGlobalResourceProvider(serviceProvider, classKey); 
                }

                if (resProvider != null) {
                    System.Resources.IResourceReader resReader = resProvider.ResourceReader; 
                    if (resReader != null) {
                        ArrayList resourceKeys = new ArrayList(); 
                        foreach (DictionaryEntry de in resReader) { 
                            resourceKeys.Add(de.Key);
                        } 
                        return resourceKeys;
                    }
                }
                return null; 
            }
 
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
                if (context != null) {
                    if (context.Instance != null) { 
                        ResourceExpressionEditorSheet sheet = (ResourceExpressionEditorSheet)context.Instance;
                        ICollection resourceKeys = GetResourceKeys(sheet.ServiceProvider, sheet.ClassKey);
                        if ((resourceKeys != null) && (resourceKeys.Count > 0)) {
                            return new StandardValuesCollection(resourceKeys); 
                        }
                    } 
                } 
                return base.GetStandardValues(context);
            } 

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
                return false;
            } 

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
                if (context != null) { 
                    if (context.Instance != null) {
                        ResourceExpressionEditorSheet sheet = (ResourceExpressionEditorSheet)context.Instance; 
                        ICollection resourceKeys = GetResourceKeys(sheet.ServiceProvider, sheet.ClassKey);
                        if ((resourceKeys != null) && (resourceKeys.Count > 0)) {
                            return true;
                        } 
                    }
                } 
 
                return base.GetStandardValuesSupported(context);
            } 

        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ResourceExpressionEditorSheet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System.Collections; 
    using System.ComponentModel;
    using System.Design; 
    using System.Diagnostics;

    /// <summary>
    /// Summary description for ResourceExpressionEditorSheet. 
    /// </summary>
    public class ResourceExpressionEditorSheet : ExpressionEditorSheet { 
        private string _classKey; 
        private string _resourceKey;
 
        public ResourceExpressionEditorSheet(string expression, IServiceProvider serviceProvider) : base(serviceProvider) {
            // Parse the existing expression if it exists;
            if (!String.IsNullOrEmpty(expression)) {
                ResourceExpressionFields fields = ParseExpressionInternal(expression); 
                ClassKey = fields.ClassKey;
                ResourceKey = fields.ResourceKey; 
            } 
        }
 
        [DefaultValue("")]
        [SRDescription(SR.ResourceExpressionEditorSheet_ClassKey)]
        public string ClassKey {
            get { 
                if (_classKey == null) {
                    return String.Empty; 
                } 

                return _classKey; 
            }
            set {
                _classKey = value;
            } 
        }
 
        public override bool IsValid { 
            get {
                return !String.IsNullOrEmpty(ResourceKey); 
            }
        }

        [DefaultValue("")] 
        [SRDescription(SR.ResourceExpressionEditorSheet_ResourceKey)]
        [TypeConverter(typeof(ResourceKeyTypeConverter))] 
        public string ResourceKey { 
            get {
                if (_resourceKey == null) { 
                    return String.Empty;
                }

                return _resourceKey; 
            }
            set { 
                _resourceKey = value; 
            }
        } 

        public override string GetExpression() {
            string expression = String.Empty;
            if (!String.IsNullOrEmpty(_classKey)) { 
                return _classKey + ", " + _resourceKey;
            } 
            else { 
                return _resourceKey;
            } 
        }

        // The following syntaxes are accepted for the expression
        //      resourceKey 
        //      classKey, resourceKey
        // 
        private static ResourceExpressionFields ParseExpressionInternal(string expression) { 

            ResourceExpressionFields fields = new ResourceExpressionFields(); 

            int len = expression.Length;

            // Split the comma separated string 
            string[] parts = expression.Split(',');
 
            int numOfParts = parts.Length; 

            if (numOfParts > 2) return null; 

            if (numOfParts == 1) {
                fields.ResourceKey = parts[0].Trim();
            } 
            else {
                fields.ClassKey = parts[0].Trim(); 
                fields.ResourceKey = parts[1].Trim(); 
            }
 
            return fields;
        }

        internal class ResourceExpressionFields { 
            internal string ClassKey;
            internal string ResourceKey; 
        } 

        private class ResourceKeyTypeConverter : StringConverter { 

            private static ICollection GetResourceKeys(IServiceProvider serviceProvider, string classKey) {
                DesignTimeResourceProviderFactory resourceProviderFactory = ControlDesigner.GetDesignTimeResourceProviderFactory(serviceProvider);
                System.Web.Compilation.IResourceProvider resProvider; 
                if (String.IsNullOrEmpty(classKey)) {
                    resProvider = resourceProviderFactory.CreateDesignTimeLocalResourceProvider(serviceProvider); 
                } 
                else {
                    resProvider = resourceProviderFactory.CreateDesignTimeGlobalResourceProvider(serviceProvider, classKey); 
                }

                if (resProvider != null) {
                    System.Resources.IResourceReader resReader = resProvider.ResourceReader; 
                    if (resReader != null) {
                        ArrayList resourceKeys = new ArrayList(); 
                        foreach (DictionaryEntry de in resReader) { 
                            resourceKeys.Add(de.Key);
                        } 
                        return resourceKeys;
                    }
                }
                return null; 
            }
 
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
                if (context != null) {
                    if (context.Instance != null) { 
                        ResourceExpressionEditorSheet sheet = (ResourceExpressionEditorSheet)context.Instance;
                        ICollection resourceKeys = GetResourceKeys(sheet.ServiceProvider, sheet.ClassKey);
                        if ((resourceKeys != null) && (resourceKeys.Count > 0)) {
                            return new StandardValuesCollection(resourceKeys); 
                        }
                    } 
                } 
                return base.GetStandardValues(context);
            } 

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
                return false;
            } 

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
                if (context != null) { 
                    if (context.Instance != null) {
                        ResourceExpressionEditorSheet sheet = (ResourceExpressionEditorSheet)context.Instance; 
                        ICollection resourceKeys = GetResourceKeys(sheet.ServiceProvider, sheet.ClassKey);
                        if ((resourceKeys != null) && (resourceKeys.Count > 0)) {
                            return true;
                        } 
                    }
                } 
 
                return base.GetStandardValuesSupported(context);
            } 

        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
