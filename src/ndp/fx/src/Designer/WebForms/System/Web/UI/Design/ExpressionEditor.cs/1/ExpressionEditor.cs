//------------------------------------------------------------------------------ 
// <copyright file="ExpressionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Configuration;
    using System.Design; 
    using System.Web.Compilation;
    using System.Web.Configuration; 
 
    using ExpressionBuilder = System.Web.Configuration.ExpressionBuilder;
 
    /// <include file='doc\ExpressionEditor.uex' path='docs/doc[@for="ExpressionEditor"]/*' />
    public abstract class ExpressionEditor {
        private const string expressionEditorsByTypeKey = "ExpressionEditorsByType";
        private const string expressionEditorsKey = "ExpressionEditors"; 

        private string _expressionPrefix; 
 
        public string ExpressionPrefix {
            get { 
                return _expressionPrefix;
            }
        }
 
        /// <include file='doc\ExpressionEditor.uex' path='docs/doc[@for="ExpressionEditor.EvaluateExpression"]/*' />
        /// <devdoc> 
        /// Evalutes the specified expression 
        /// </devdoc>
        public abstract object EvaluateExpression(string expression, object parseTimeData, Type propertyType, IServiceProvider serviceProvider); 

        private static IDictionary GetExpressionEditorsCache(IWebApplication webApp) {
            IDictionaryService dictionaryService = (IDictionaryService)webApp.GetService(typeof(IDictionaryService));
            if (dictionaryService == null) { 
                // If there is no IDictionaryService, we don't have a cache
                return null; 
            } 
            IDictionary expressionEditors = (IDictionary)dictionaryService.GetValue(expressionEditorsKey);
            if (expressionEditors == null) { 
                expressionEditors = new HybridDictionary(true);
                dictionaryService.SetValue(expressionEditorsKey, expressionEditors);
            }
 
            return expressionEditors;
        } 
 
        private static IDictionary GetExpressionEditorsByTypeCache(IWebApplication webApp) {
            IDictionaryService dictionaryService = (IDictionaryService)webApp.GetService(typeof(IDictionaryService)); 
            if (dictionaryService == null) {
                // If there is no IDictionaryService, we don't have a cache
                return null;
            } 
            IDictionary expressionEditorsByType = (IDictionary)dictionaryService.GetValue(expressionEditorsByTypeKey);
            if (expressionEditorsByType == null) { 
                expressionEditorsByType = new HybridDictionary(); 
                dictionaryService.SetValue(expressionEditorsByTypeKey, expressionEditorsByType);
            } 

            return expressionEditorsByType;
        }
 
        public static ExpressionEditor GetExpressionEditor(Type expressionBuilderType, IServiceProvider serviceProvider) {
            if (serviceProvider == null) { 
                throw new ArgumentNullException("serviceProvider"); 
            }
 
            if (expressionBuilderType == null) {
                throw new ArgumentNullException("expressionBuilderType");
            }
 
            ExpressionEditor expressionEditor = null;
            IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication)); 
            if (webApp != null) { 
                // See if we have a cached instance already
                IDictionary expressionEditorsByType = GetExpressionEditorsByTypeCache(webApp); 
                if (expressionEditorsByType != null) {
                    expressionEditor = (ExpressionEditor)expressionEditorsByType[expressionBuilderType];
                }
 
                // If not, try to get one
                if (expressionEditor == null) { 
                    Configuration config = webApp.OpenWebConfiguration(true); 
                    if (config != null) {
                        // Get the compilation config section to get the list of expressionbuilders 
                        CompilationSection compilationSection = (CompilationSection)config.GetSection("system.web/compilation");
                        ExpressionBuilderCollection builders = compilationSection.ExpressionBuilders;

                        bool foundEditor = false; 
                        string desiredTypeName = expressionBuilderType.FullName;
                        // Find the one corresponding to this prefix 
                        foreach (ExpressionBuilder expressionBuilder in builders) { 
                            if (String.Equals(expressionBuilder.Type, desiredTypeName, StringComparison.OrdinalIgnoreCase)) {
                                expressionEditor = GetExpressionEditorInternal(expressionBuilderType, expressionBuilder.ExpressionPrefix, webApp, serviceProvider); 
                                foundEditor = true;
                            }
                        }
 
                        // If we didn't find the expression builder, register it
                        if (!foundEditor) { 
                            // Check the ExpressionPrefixAttribute to get the correct ExpressionPrefix 
                            object[] attrs = expressionBuilderType.GetCustomAttributes(typeof(ExpressionPrefixAttribute), true);
                            ExpressionPrefixAttribute prefixAttr = null; 
                            if (attrs.Length > 0) {
                                prefixAttr = (ExpressionPrefixAttribute)attrs[0];
                            }
 
                            // If there is a default prefix, try to register the expression builder
                            if (prefixAttr != null) { 
                                ExpressionBuilder newBuilder = new ExpressionBuilder(prefixAttr.ExpressionPrefix,expressionBuilderType.FullName); 

                                // Not open the configuration as not readonly 
                                config = webApp.OpenWebConfiguration(false);
                                compilationSection = (CompilationSection)config.GetSection("system.web/compilation");
                                builders = compilationSection.ExpressionBuilders;
                                builders.Add(newBuilder); 
                                config.Save();
 
                                expressionEditor = GetExpressionEditorInternal(expressionBuilderType, newBuilder.ExpressionPrefix, webApp, serviceProvider); 
                            }
                        } 
                    }
                }
            }
 
            return expressionEditor;
        } 
 
        // Internal way to get ExpressionEditors and also add them to the cache
        internal static ExpressionEditor GetExpressionEditorInternal(Type expressionBuilderType, string expressionPrefix, IWebApplication webApp, IServiceProvider serviceProvider) { 
            if (expressionBuilderType == null) {
                throw new ArgumentNullException("expressionBuilderType");
            }
 
            ExpressionEditor expressionEditor = null;
 
            // Check the ExpressionEditorAttribute to get the correct ExpressionEditor 
            object[] attrs = expressionBuilderType.GetCustomAttributes(typeof(ExpressionEditorAttribute), true);
            ExpressionEditorAttribute editorAttr = null; 
            if (attrs.Length > 0) {
                editorAttr = (ExpressionEditorAttribute)attrs[0];
            }
 
            if (editorAttr != null) {
                // Instantiate the ExpressionEditor 
                string editorTypeName = editorAttr.EditorTypeName; 
                Type editorType = Type.GetType(editorTypeName);
 
                // If GetType didn't work, try the typeResolutionService
                if (editorType == null) {
                    ITypeResolutionService typeResolutionService = (ITypeResolutionService)serviceProvider.GetService(typeof(ITypeResolutionService));
                    if (typeResolutionService != null) { 
                        editorType = typeResolutionService.GetType(editorTypeName);
                    } 
                } 

                if ((editorType != null) && (typeof(ExpressionEditor).IsAssignableFrom(editorType))) { 
                    expressionEditor = (ExpressionEditor)Activator.CreateInstance(editorType);
                    expressionEditor.SetExpressionPrefix(expressionPrefix);
                }
 
                // Add it to both caches (if we have caches)
                IDictionary expressionEditors = GetExpressionEditorsCache(webApp); 
                if (expressionEditors != null) { 
                    expressionEditors[expressionPrefix] = expressionEditor;
                } 

                IDictionary expressionEditorsByType = GetExpressionEditorsByTypeCache(webApp);
                if (expressionEditorsByType != null) {
                    expressionEditorsByType[expressionBuilderType] = expressionEditor; 
                }
            } 
 
            return expressionEditor;
        } 

        public static ExpressionEditor GetExpressionEditor(string expressionPrefix, IServiceProvider serviceProvider) {
            if (serviceProvider == null) {
                throw new ArgumentNullException("serviceProvider"); 
            }
            // If there is no expressionPrefix, it's a v1 style databinding expression 
            if (expressionPrefix.Length == 0) { 
                return null;
            } 

            ExpressionEditor expressionEditor = null;
            IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication));
            if (webApp != null) { 
                // See if we have a cached instance already
 
                IDictionary expressionEditors = GetExpressionEditorsCache(webApp); 
                if (expressionEditors != null) {
                    expressionEditor = (ExpressionEditor)expressionEditors[expressionPrefix]; 
                }

                // If not, try to get one
                if (expressionEditor == null) { 
                    string trueExpressionPrefix;
                    Type type = GetExpressionBuilderType(expressionPrefix, serviceProvider, out trueExpressionPrefix); 
                    if (type != null) { 
                        expressionEditor = GetExpressionEditorInternal(type, trueExpressionPrefix, webApp, serviceProvider);
                    } 
                }
            }

            return expressionEditor; 
        }
 
        internal static Type GetExpressionBuilderType(string expressionPrefix, IServiceProvider serviceProvider, out string trueExpressionPrefix) { 
            if (serviceProvider == null) {
                throw new ArgumentNullException("serviceProvider"); 
            }

            trueExpressionPrefix = expressionPrefix;
 
            // If there is no expressionPrefix, it's a v1 style databinding expression
            if (expressionPrefix.Length == 0) { 
                return null; 
            }
 
            Type type = null;

            IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication));
            if (webApp != null) { 
                Configuration config = webApp.OpenWebConfiguration(true);
                if (config != null) { 
                    // Get the compilation config section to get the list of expressionbuilders 
                    CompilationSection compilationSection = (CompilationSection)config.GetSection("system.web/compilation");
                    ExpressionBuilderCollection builders = compilationSection.ExpressionBuilders; 

                    // Find the one corresponding to this prefix
                    foreach (ExpressionBuilder expressionBuilder in builders) {
                        if (String.Equals(expressionPrefix, expressionBuilder.ExpressionPrefix, StringComparison.OrdinalIgnoreCase)) { 
                            trueExpressionPrefix = expressionBuilder.ExpressionPrefix;
                            // Try to get the type 
                            type = Type.GetType(expressionBuilder.Type); 

                            // If GetType didn't work, try the typeResolutionService 
                            if (type == null) {
                                ITypeResolutionService typeResolutionService = (ITypeResolutionService)serviceProvider.GetService(typeof(ITypeResolutionService));
                                if (typeResolutionService != null) {
                                    type = typeResolutionService.GetType(expressionBuilder.Type); 
                                }
                            } 
                        } 
                    }
                } 
            }

            return type;
        } 

 
        /// <include file='doc\ExpressionEditor.uex' path='docs/doc[@for="ExpressionEditor.GetExpressionEditorSheet"]/*' /> 
        /// <devdoc>
        /// Returns an expression editor sheet that has properties corresponding 
        /// to an expression that this editor knows how to handle.
        /// </devdoc>
        public virtual ExpressionEditorSheet GetExpressionEditorSheet(string expression, IServiceProvider serviceProvider) {
            return new GenericExpressionEditorSheet(expression, serviceProvider); 
        }
 
        internal void SetExpressionPrefix(string expressionPrefix) { 
            _expressionPrefix = expressionPrefix;
        } 

        private class GenericExpressionEditorSheet : ExpressionEditorSheet {
            private string _expression;
 
            public GenericExpressionEditorSheet(string expression, IServiceProvider serviceProvider) : base(serviceProvider) {
                _expression = expression; 
            } 

            [DefaultValue("")] 
            [SRDescription(SR.ExpressionEditor_Expression)]
            public string Expression {
                get {
                    if (_expression == null) { 
                        return String.Empty;
                    } 
                    return _expression; 
                }
                set { 
                    _expression = value;
                }
            }
 
            public override string GetExpression() {
                return _expression; 
            } 
        }
    } 

}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ExpressionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Configuration;
    using System.Design; 
    using System.Web.Compilation;
    using System.Web.Configuration; 
 
    using ExpressionBuilder = System.Web.Configuration.ExpressionBuilder;
 
    /// <include file='doc\ExpressionEditor.uex' path='docs/doc[@for="ExpressionEditor"]/*' />
    public abstract class ExpressionEditor {
        private const string expressionEditorsByTypeKey = "ExpressionEditorsByType";
        private const string expressionEditorsKey = "ExpressionEditors"; 

        private string _expressionPrefix; 
 
        public string ExpressionPrefix {
            get { 
                return _expressionPrefix;
            }
        }
 
        /// <include file='doc\ExpressionEditor.uex' path='docs/doc[@for="ExpressionEditor.EvaluateExpression"]/*' />
        /// <devdoc> 
        /// Evalutes the specified expression 
        /// </devdoc>
        public abstract object EvaluateExpression(string expression, object parseTimeData, Type propertyType, IServiceProvider serviceProvider); 

        private static IDictionary GetExpressionEditorsCache(IWebApplication webApp) {
            IDictionaryService dictionaryService = (IDictionaryService)webApp.GetService(typeof(IDictionaryService));
            if (dictionaryService == null) { 
                // If there is no IDictionaryService, we don't have a cache
                return null; 
            } 
            IDictionary expressionEditors = (IDictionary)dictionaryService.GetValue(expressionEditorsKey);
            if (expressionEditors == null) { 
                expressionEditors = new HybridDictionary(true);
                dictionaryService.SetValue(expressionEditorsKey, expressionEditors);
            }
 
            return expressionEditors;
        } 
 
        private static IDictionary GetExpressionEditorsByTypeCache(IWebApplication webApp) {
            IDictionaryService dictionaryService = (IDictionaryService)webApp.GetService(typeof(IDictionaryService)); 
            if (dictionaryService == null) {
                // If there is no IDictionaryService, we don't have a cache
                return null;
            } 
            IDictionary expressionEditorsByType = (IDictionary)dictionaryService.GetValue(expressionEditorsByTypeKey);
            if (expressionEditorsByType == null) { 
                expressionEditorsByType = new HybridDictionary(); 
                dictionaryService.SetValue(expressionEditorsByTypeKey, expressionEditorsByType);
            } 

            return expressionEditorsByType;
        }
 
        public static ExpressionEditor GetExpressionEditor(Type expressionBuilderType, IServiceProvider serviceProvider) {
            if (serviceProvider == null) { 
                throw new ArgumentNullException("serviceProvider"); 
            }
 
            if (expressionBuilderType == null) {
                throw new ArgumentNullException("expressionBuilderType");
            }
 
            ExpressionEditor expressionEditor = null;
            IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication)); 
            if (webApp != null) { 
                // See if we have a cached instance already
                IDictionary expressionEditorsByType = GetExpressionEditorsByTypeCache(webApp); 
                if (expressionEditorsByType != null) {
                    expressionEditor = (ExpressionEditor)expressionEditorsByType[expressionBuilderType];
                }
 
                // If not, try to get one
                if (expressionEditor == null) { 
                    Configuration config = webApp.OpenWebConfiguration(true); 
                    if (config != null) {
                        // Get the compilation config section to get the list of expressionbuilders 
                        CompilationSection compilationSection = (CompilationSection)config.GetSection("system.web/compilation");
                        ExpressionBuilderCollection builders = compilationSection.ExpressionBuilders;

                        bool foundEditor = false; 
                        string desiredTypeName = expressionBuilderType.FullName;
                        // Find the one corresponding to this prefix 
                        foreach (ExpressionBuilder expressionBuilder in builders) { 
                            if (String.Equals(expressionBuilder.Type, desiredTypeName, StringComparison.OrdinalIgnoreCase)) {
                                expressionEditor = GetExpressionEditorInternal(expressionBuilderType, expressionBuilder.ExpressionPrefix, webApp, serviceProvider); 
                                foundEditor = true;
                            }
                        }
 
                        // If we didn't find the expression builder, register it
                        if (!foundEditor) { 
                            // Check the ExpressionPrefixAttribute to get the correct ExpressionPrefix 
                            object[] attrs = expressionBuilderType.GetCustomAttributes(typeof(ExpressionPrefixAttribute), true);
                            ExpressionPrefixAttribute prefixAttr = null; 
                            if (attrs.Length > 0) {
                                prefixAttr = (ExpressionPrefixAttribute)attrs[0];
                            }
 
                            // If there is a default prefix, try to register the expression builder
                            if (prefixAttr != null) { 
                                ExpressionBuilder newBuilder = new ExpressionBuilder(prefixAttr.ExpressionPrefix,expressionBuilderType.FullName); 

                                // Not open the configuration as not readonly 
                                config = webApp.OpenWebConfiguration(false);
                                compilationSection = (CompilationSection)config.GetSection("system.web/compilation");
                                builders = compilationSection.ExpressionBuilders;
                                builders.Add(newBuilder); 
                                config.Save();
 
                                expressionEditor = GetExpressionEditorInternal(expressionBuilderType, newBuilder.ExpressionPrefix, webApp, serviceProvider); 
                            }
                        } 
                    }
                }
            }
 
            return expressionEditor;
        } 
 
        // Internal way to get ExpressionEditors and also add them to the cache
        internal static ExpressionEditor GetExpressionEditorInternal(Type expressionBuilderType, string expressionPrefix, IWebApplication webApp, IServiceProvider serviceProvider) { 
            if (expressionBuilderType == null) {
                throw new ArgumentNullException("expressionBuilderType");
            }
 
            ExpressionEditor expressionEditor = null;
 
            // Check the ExpressionEditorAttribute to get the correct ExpressionEditor 
            object[] attrs = expressionBuilderType.GetCustomAttributes(typeof(ExpressionEditorAttribute), true);
            ExpressionEditorAttribute editorAttr = null; 
            if (attrs.Length > 0) {
                editorAttr = (ExpressionEditorAttribute)attrs[0];
            }
 
            if (editorAttr != null) {
                // Instantiate the ExpressionEditor 
                string editorTypeName = editorAttr.EditorTypeName; 
                Type editorType = Type.GetType(editorTypeName);
 
                // If GetType didn't work, try the typeResolutionService
                if (editorType == null) {
                    ITypeResolutionService typeResolutionService = (ITypeResolutionService)serviceProvider.GetService(typeof(ITypeResolutionService));
                    if (typeResolutionService != null) { 
                        editorType = typeResolutionService.GetType(editorTypeName);
                    } 
                } 

                if ((editorType != null) && (typeof(ExpressionEditor).IsAssignableFrom(editorType))) { 
                    expressionEditor = (ExpressionEditor)Activator.CreateInstance(editorType);
                    expressionEditor.SetExpressionPrefix(expressionPrefix);
                }
 
                // Add it to both caches (if we have caches)
                IDictionary expressionEditors = GetExpressionEditorsCache(webApp); 
                if (expressionEditors != null) { 
                    expressionEditors[expressionPrefix] = expressionEditor;
                } 

                IDictionary expressionEditorsByType = GetExpressionEditorsByTypeCache(webApp);
                if (expressionEditorsByType != null) {
                    expressionEditorsByType[expressionBuilderType] = expressionEditor; 
                }
            } 
 
            return expressionEditor;
        } 

        public static ExpressionEditor GetExpressionEditor(string expressionPrefix, IServiceProvider serviceProvider) {
            if (serviceProvider == null) {
                throw new ArgumentNullException("serviceProvider"); 
            }
            // If there is no expressionPrefix, it's a v1 style databinding expression 
            if (expressionPrefix.Length == 0) { 
                return null;
            } 

            ExpressionEditor expressionEditor = null;
            IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication));
            if (webApp != null) { 
                // See if we have a cached instance already
 
                IDictionary expressionEditors = GetExpressionEditorsCache(webApp); 
                if (expressionEditors != null) {
                    expressionEditor = (ExpressionEditor)expressionEditors[expressionPrefix]; 
                }

                // If not, try to get one
                if (expressionEditor == null) { 
                    string trueExpressionPrefix;
                    Type type = GetExpressionBuilderType(expressionPrefix, serviceProvider, out trueExpressionPrefix); 
                    if (type != null) { 
                        expressionEditor = GetExpressionEditorInternal(type, trueExpressionPrefix, webApp, serviceProvider);
                    } 
                }
            }

            return expressionEditor; 
        }
 
        internal static Type GetExpressionBuilderType(string expressionPrefix, IServiceProvider serviceProvider, out string trueExpressionPrefix) { 
            if (serviceProvider == null) {
                throw new ArgumentNullException("serviceProvider"); 
            }

            trueExpressionPrefix = expressionPrefix;
 
            // If there is no expressionPrefix, it's a v1 style databinding expression
            if (expressionPrefix.Length == 0) { 
                return null; 
            }
 
            Type type = null;

            IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication));
            if (webApp != null) { 
                Configuration config = webApp.OpenWebConfiguration(true);
                if (config != null) { 
                    // Get the compilation config section to get the list of expressionbuilders 
                    CompilationSection compilationSection = (CompilationSection)config.GetSection("system.web/compilation");
                    ExpressionBuilderCollection builders = compilationSection.ExpressionBuilders; 

                    // Find the one corresponding to this prefix
                    foreach (ExpressionBuilder expressionBuilder in builders) {
                        if (String.Equals(expressionPrefix, expressionBuilder.ExpressionPrefix, StringComparison.OrdinalIgnoreCase)) { 
                            trueExpressionPrefix = expressionBuilder.ExpressionPrefix;
                            // Try to get the type 
                            type = Type.GetType(expressionBuilder.Type); 

                            // If GetType didn't work, try the typeResolutionService 
                            if (type == null) {
                                ITypeResolutionService typeResolutionService = (ITypeResolutionService)serviceProvider.GetService(typeof(ITypeResolutionService));
                                if (typeResolutionService != null) {
                                    type = typeResolutionService.GetType(expressionBuilder.Type); 
                                }
                            } 
                        } 
                    }
                } 
            }

            return type;
        } 

 
        /// <include file='doc\ExpressionEditor.uex' path='docs/doc[@for="ExpressionEditor.GetExpressionEditorSheet"]/*' /> 
        /// <devdoc>
        /// Returns an expression editor sheet that has properties corresponding 
        /// to an expression that this editor knows how to handle.
        /// </devdoc>
        public virtual ExpressionEditorSheet GetExpressionEditorSheet(string expression, IServiceProvider serviceProvider) {
            return new GenericExpressionEditorSheet(expression, serviceProvider); 
        }
 
        internal void SetExpressionPrefix(string expressionPrefix) { 
            _expressionPrefix = expressionPrefix;
        } 

        private class GenericExpressionEditorSheet : ExpressionEditorSheet {
            private string _expression;
 
            public GenericExpressionEditorSheet(string expression, IServiceProvider serviceProvider) : base(serviceProvider) {
                _expression = expression; 
            } 

            [DefaultValue("")] 
            [SRDescription(SR.ExpressionEditor_Expression)]
            public string Expression {
                get {
                    if (_expression == null) { 
                        return String.Empty;
                    } 
                    return _expression; 
                }
                set { 
                    _expression = value;
                }
            }
 
            public override string GetExpression() {
                return _expression; 
            } 
        }
    } 

}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
