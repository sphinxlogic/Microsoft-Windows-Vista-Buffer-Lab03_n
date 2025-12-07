//------------------------------------------------------------------------------ 
// <copyright file="XslCompiledTransform.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <spec>http://webdata/xml/specs/XslCompiledTransform.xml</spec>
//----------------------------------------------------------------------------- 
 
using System.CodeDom.Compiler;
using System.Diagnostics; 
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security; 
using System.Security.Permissions;
using System.Xml.XPath; 
using System.Xml.Xsl.Qil; 
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.Xslt; 

namespace System.Xml.Xsl {
#if ! HIDE_XSL
 
    //---------------------------------------------------------------------------------------------------
    //  Clarification on null values in this API: 
    //      stylesheet, stylesheetUri   - cannot be null 
    //      settings                    - if null, XsltSettings.Default will be used
    //      stylesheetResolver          - if null, XmlNullResolver will be used for includes/imports. 
    //                                    However, if the principal stylesheet is given by its URI, that
    //                                    URI will be resolved using XmlUrlResolver (for compatibility
    //                                    with XslTransform and XmlReader).
    //      typeBuilder                 - cannot be null 
    //      scriptAssemblyPath          - can be null only if scripts are disabled
    //      compiledStylesheet          - cannot be null 
    //      executeMethod, queryData    - cannot be null 
    //      earlyBoundTypes             - null means no script types
    //      documentResolver            - if null, XmlNullResolver will be used 
    //      input, inputUri             - cannot be null
    //      arguments                   - null means no arguments
    //      results, resultsFile        - cannot be null
    //--------------------------------------------------------------------------------------------------- 

    public sealed class XslCompiledTransform { 
        // Permission set that contains Reflection [MemberAccess] permissions 
        private static readonly PermissionSet MemberAccessPermissionSet;
 
        // Version for GeneratedCodeAttribute
        private const string Version = ThisAssembly.Version;

        static XslCompiledTransform() { 
            MemberAccessPermissionSet = new PermissionSet(PermissionState.None);
            MemberAccessPermissionSet.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess)); 
        } 

        // Options of compilation 
        private bool                enableDebug     = false;

        // Results of compilation
        private CompilerResults     compilerResults = null; 
        private XmlWriterSettings   outputSettings  = null;
        private QilExpression       qil             = null; 
 
        // Executable command for the compiled stylesheet
        private XmlILCommand        command         = null; 

        public XslCompiledTransform() {}

        public XslCompiledTransform(bool enableDebug) { 
            this.enableDebug = enableDebug;
        } 
 
        /// <summary>
        /// This function is called on every recompilation to discard all previous results 
        /// </summary>
        private void Reset() {
            compilerResults = null;
            outputSettings  = null; 
            qil             = null;
            command         = null; 
        } 

        internal CompilerErrorCollection Errors { 
            get { return compilerResults != null ? compilerResults.Errors : null; }
        }

        /// <summary> 
        /// Writer settings specified in the stylesheet
        /// </summary> 
        public XmlWriterSettings OutputSettings { 
            get { return outputSettings; }
        } 

        public TempFileCollection TemporaryFiles {
            [PermissionSet(SecurityAction.LinkDemand, Name="FullTrust")]
            get { return compilerResults != null ? compilerResults.TempFiles : null; } 
        }
 
        //------------------------------------------------ 
        // Load methods
        //------------------------------------------------ 

        public void Load(XmlReader stylesheet) {
            Reset();
            LoadInternal(stylesheet, XsltSettings.Default, new XmlUrlResolver()); 
        }
 
        public void Load(XmlReader stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) { 
            Reset();
            LoadInternal(stylesheet, settings, stylesheetResolver); 
        }

        public void Load(IXPathNavigable stylesheet) {
            Reset(); 
            LoadInternal(stylesheet, XsltSettings.Default, new XmlUrlResolver());
        } 
 
        public void Load(IXPathNavigable stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            Reset(); 
            LoadInternal(stylesheet, settings, stylesheetResolver);
        }

        public void Load(string stylesheetUri) { 
            Reset();
            if (stylesheetUri == null) { 
                throw new ArgumentNullException("stylesheetUri"); 
            }
            LoadInternal(stylesheetUri, XsltSettings.Default, new XmlUrlResolver()); 
        }

        public void Load(string stylesheetUri, XsltSettings settings, XmlResolver stylesheetResolver) {
            Reset(); 
            if (stylesheetUri == null) {
                throw new ArgumentNullException("stylesheetUri"); 
            } 
            LoadInternal(stylesheetUri, settings, stylesheetResolver);
        } 

        private CompilerResults LoadInternal(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            if (stylesheet == null) {
                throw new ArgumentNullException("stylesheet"); 
            }
            if (settings == null) { 
                settings = XsltSettings.Default; 
            }
            CompileXsltToQil(stylesheet, settings, stylesheetResolver); 
            CompilerError error = GetFirstError();
            if (error != null) {
                throw new XslLoadException(error);
            } 
            if (!settings.CheckOnly) {
                CompileQilToMsil(settings); 
            } 
            return compilerResults;
        } 

        private void CompileXsltToQil(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            this.compilerResults = new Compiler(settings, this.enableDebug, null).Compile(stylesheet, stylesheetResolver, out this.qil);
        } 

        /// <summary> 
        /// Returns the first compiler error except warnings 
        /// </summary>
        private CompilerError GetFirstError() { 
            foreach (CompilerError error in compilerResults.Errors) {
                if (!error.IsWarning) {
                    return error;
                } 
            }
            return null; 
        } 

        private void CompileQilToMsil(XsltSettings settings) { 
            this.command = new XmlILGenerator().Generate(this.qil, /*typeBuilder:*/null);
            this.outputSettings = this.command.StaticData.DefaultWriterSettings;
            this.qil = null;
        } 

        //------------------------------------------------ 
        // Compile stylesheet to a TypeBuilder 
        //------------------------------------------------
 
        private static ConstructorInfo GeneratedCodeCtor;

        public static CompilerErrorCollection CompileToType(XmlReader stylesheet, XsltSettings settings, XmlResolver stylesheetResolver, bool debug, TypeBuilder typeBuilder, string scriptAssemblyPath) {
            if (stylesheet == null) 
                throw new ArgumentNullException("stylesheet");
 
            if (typeBuilder == null) 
                throw new ArgumentNullException("typeBuilder");
 
            if (settings == null)
                settings = XsltSettings.Default;

            if (settings.EnableScript && scriptAssemblyPath == null) 
                throw new ArgumentNullException("scriptAssemblyPath");
 
            if (scriptAssemblyPath != null) 
                scriptAssemblyPath = Path.GetFullPath(scriptAssemblyPath);
 
            QilExpression qil;
            CompilerErrorCollection errors = new Compiler(settings, debug, scriptAssemblyPath).Compile(stylesheet, stylesheetResolver, out qil).Errors;

            if (!errors.HasErrors) { 
                // Mark the type with GeneratedCodeAttribute to identify its origin
                if (GeneratedCodeCtor == null) 
                    GeneratedCodeCtor = typeof(GeneratedCodeAttribute).GetConstructor(new Type[] { typeof(string), typeof(string) }); 

                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(GeneratedCodeCtor, 
                    new object[] { typeof(XslCompiledTransform).FullName, Version }));

                new XmlILGenerator().Generate(qil, typeBuilder);
            } 

            return errors; 
        } 

        //------------------------------------------------ 
        // Load compiled stylesheet from a Type
        //------------------------------------------------

        public void Load(Type compiledStylesheet) { 
            Reset();
            if (compiledStylesheet == null) 
                throw new ArgumentNullException("compiledStylesheet"); 

            object[] customAttrs = compiledStylesheet.GetCustomAttributes(typeof(GeneratedCodeAttribute), /*inherit:*/false); 
            GeneratedCodeAttribute generatedCodeAttr = customAttrs.Length > 0 ? (GeneratedCodeAttribute)customAttrs[0] : null;

            // If GeneratedCodeAttribute is not there, it is not a compiled stylesheet class
            if (generatedCodeAttr != null && generatedCodeAttr.Tool == typeof(XslCompiledTransform).FullName && generatedCodeAttr.Version == Version) { 
                FieldInfo fldData  = compiledStylesheet.GetField(XmlQueryStaticData.DataFieldName,  BindingFlags.Static | BindingFlags.NonPublic);
                FieldInfo fldTypes = compiledStylesheet.GetField(XmlQueryStaticData.TypesFieldName, BindingFlags.Static | BindingFlags.NonPublic); 
 
                // If private fields are not there, it is not a compiled stylesheet class
                if (fldData != null && fldTypes != null) { 
                    // Need MemberAccess reflection permission to access a private data field and create a delegate
                    new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();

                    // Check if query static data is already deserialized 
                    object value = fldData.GetValue(/*this:*/null);
                    byte[] data = value as byte[]; 
 
                    if (data != null) {
                        lock (data) { 
                            // After acquiring the lock check the field again
                            value = fldData.GetValue(/*this:*/null);
                            if (value == data) {
                                // Deserialize query static data and create the command 
                                MethodInfo methExec = compiledStylesheet.GetMethod("Execute", BindingFlags.Static | BindingFlags.NonPublic);
                                Delegate delExec = Delegate.CreateDelegate(typeof(ExecuteDelegate), methExec); 
                                value = new XmlILCommand((ExecuteDelegate)delExec, new XmlQueryStaticData(data, (Type[])fldTypes.GetValue(/*this:*/null))); 

                                // Store the constructed command in the same field 
                                System.Threading.Thread.MemoryBarrier();
                                fldData.SetValue(/*this:*/null, value);
                            }
                        } 
                    }
 
                    this.command = value as XmlILCommand; 
                }
            } 

            // Throw an exception if the command was not loaded
            if (this.command == null)
                throw new ArgumentException(Res.GetString(Res.Xslt_NotCompiledStylesheet, compiledStylesheet.FullName), "compiledStylesheet"); 

            // Otherwise set the OutputSettings property 
            this.outputSettings = this.command.StaticData.DefaultWriterSettings; 
        }
 
        public void Load(MethodInfo executeMethod, byte[] queryData, Type[] earlyBoundTypes) {
            Reset();
            if (executeMethod == null)
                throw new ArgumentNullException("executeMethod"); 

            if (queryData == null) 
                throw new ArgumentNullException("queryData"); 

            // earlyBoundTypes may be null 

            DynamicMethod dm = executeMethod as DynamicMethod;
            Delegate delExec = (dm != null) ? dm.CreateDelegate(typeof(ExecuteDelegate)) : Delegate.CreateDelegate(typeof(ExecuteDelegate), executeMethod);
            this.command = new XmlILCommand((ExecuteDelegate)delExec, new XmlQueryStaticData(queryData, earlyBoundTypes)); 
            this.outputSettings = this.command.StaticData.DefaultWriterSettings;
        } 
 
        //------------------------------------------------
        // Transform methods which take an IXPathNavigable 
        //------------------------------------------------

        public void Transform(IXPathNavigable input, XmlWriter results) {
            CheckCommand(); 
            CheckInput(input);
            command.Execute(input, new XmlUrlResolver(), (XsltArgumentList)null, results); 
        } 

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, XmlWriter results) { 
            CheckCommand();
            CheckInput(input);
            command.Execute(input, new XmlUrlResolver(), arguments, results);
        } 

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, TextWriter results) { 
            CheckCommand(); 
            CheckInput(input);
            command.Execute(input, new XmlUrlResolver(), arguments, results); 
        }

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, Stream results) {
            CheckCommand(); 
            CheckInput(input);
            command.Execute(input, new XmlUrlResolver(), arguments, results); 
        } 

        //------------------------------------------------ 
        // Transform methods which take an XmlReader
        //------------------------------------------------

        public void Transform(XmlReader input, XmlWriter results) { 
            CheckCommand();
            CheckInput(input); 
            command.Execute(input, new XmlUrlResolver(), (XsltArgumentList)null, results); 
        }
 
        public void Transform(XmlReader input, XsltArgumentList arguments, XmlWriter results) {
            CheckCommand();
            CheckInput(input);
            command.Execute(input, new XmlUrlResolver(), arguments, results); 
        }
 
        public void Transform(XmlReader input, XsltArgumentList arguments, TextWriter results) { 
            CheckCommand();
            CheckInput(input); 
            command.Execute(input, new XmlUrlResolver(), arguments, results);
        }

        public void Transform(XmlReader input, XsltArgumentList arguments, Stream results) { 
            CheckCommand();
            CheckInput(input); 
            command.Execute(input, new XmlUrlResolver(), arguments, results); 
        }
 
        public void Transform(XmlReader input, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver) {
            CheckCommand();
            CheckInput(input);
            command.Execute(input, documentResolver, arguments, results); 
        }
 
        //------------------------------------------------ 
        // Transform methods which take a uri
        //------------------------------------------------ 

        public void Transform(string inputUri, string resultsFile) {
            CheckCommand();
            // SQLBUDT 276415: Prevent wiping out the content of the input file if the output file is the same 
            using (XmlReader input = CreateReader(inputUri)) {
                if (resultsFile == null) { 
                    throw new ArgumentNullException("resultsFile"); 
                }
                using (FileStream output = new FileStream(resultsFile, FileMode.Create, FileAccess.Write)) { 
                    command.Execute(input, new XmlUrlResolver(), (XsltArgumentList)null, output);
                }
            }
        } 

        public void Transform(string inputUri, XmlWriter results) { 
            CheckCommand(); 
            using (XmlReader input = CreateReader(inputUri)) {
                command.Execute(input, new XmlUrlResolver(), (XsltArgumentList)null, results); 
            }
        }

        public void Transform(string inputUri, XsltArgumentList arguments, XmlWriter results) { 
            CheckCommand();
            using (XmlReader input = CreateReader(inputUri)) { 
                command.Execute(input, new XmlUrlResolver(), arguments, results); 
            }
        } 

        public void Transform(string inputUri, XsltArgumentList arguments, TextWriter results) {
            CheckCommand();
            using (XmlReader input = CreateReader(inputUri)) { 
                command.Execute(input, new XmlUrlResolver(), arguments, results);
            } 
        } 

        public void Transform(string inputUri, XsltArgumentList arguments, Stream results) { 
            CheckCommand();
            using (XmlReader input = CreateReader(inputUri)) {
                command.Execute(input, new XmlUrlResolver(), arguments, results);
            } 
        }
 
        //------------------------------------------------ 
        // Helper methods
        //------------------------------------------------ 

        private void CheckCommand() {
            if (command == null) {
                throw new InvalidOperationException(Res.GetString(Res.Xslt_NoStylesheetLoaded)); 
            }
        } 
 
        private void CheckInput(object input) {
            if (input == null) { 
                throw new System.ArgumentNullException("input");
            }
        }
 
        private XmlReader CreateReader(string inputUri) {
            if (inputUri == null) { 
                throw new ArgumentNullException("inputUri"); 
            }
            return XmlReader.Create(inputUri); 
        }

        //------------------------------------------------
        // Test suites entry points 
        //------------------------------------------------
 
        private QilExpression TestCompile(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) { 
            Reset();
            CompileXsltToQil(stylesheet, settings, stylesheetResolver); 
            return qil;
        }

        private void TestGenerate(XsltSettings settings) { 
            Debug.Assert(qil != null, "You must compile to Qil first");
            CompileQilToMsil(settings); 
        } 

        private void Transform(string inputUri, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver) { 
            command.Execute(inputUri, documentResolver, arguments, results);
        }

        internal static void PrintQil(object qil, XmlWriter xw, bool printComments, bool printTypes, bool printLineInfo) { 
            QilExpression qilExpr = (QilExpression)qil;
            QilXmlWriter.Options options = QilXmlWriter.Options.None; 
            QilValidationVisitor.Validate(qilExpr); 
            if (printComments) options |= QilXmlWriter.Options.Annotations;
            if (printTypes) options |= QilXmlWriter.Options.TypeInfo; 
            if (printLineInfo) options |= QilXmlWriter.Options.LineInfo;
            QilXmlWriter qw = new QilXmlWriter(xw, options);
            qw.ToXml(qilExpr);
            xw.Flush(); 
        }
    } 
#endif // ! HIDE_XSL 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XslCompiledTransform.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <spec>http://webdata/xml/specs/XslCompiledTransform.xml</spec>
//----------------------------------------------------------------------------- 
 
using System.CodeDom.Compiler;
using System.Diagnostics; 
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security; 
using System.Security.Permissions;
using System.Xml.XPath; 
using System.Xml.Xsl.Qil; 
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.Xslt; 

namespace System.Xml.Xsl {
#if ! HIDE_XSL
 
    //---------------------------------------------------------------------------------------------------
    //  Clarification on null values in this API: 
    //      stylesheet, stylesheetUri   - cannot be null 
    //      settings                    - if null, XsltSettings.Default will be used
    //      stylesheetResolver          - if null, XmlNullResolver will be used for includes/imports. 
    //                                    However, if the principal stylesheet is given by its URI, that
    //                                    URI will be resolved using XmlUrlResolver (for compatibility
    //                                    with XslTransform and XmlReader).
    //      typeBuilder                 - cannot be null 
    //      scriptAssemblyPath          - can be null only if scripts are disabled
    //      compiledStylesheet          - cannot be null 
    //      executeMethod, queryData    - cannot be null 
    //      earlyBoundTypes             - null means no script types
    //      documentResolver            - if null, XmlNullResolver will be used 
    //      input, inputUri             - cannot be null
    //      arguments                   - null means no arguments
    //      results, resultsFile        - cannot be null
    //--------------------------------------------------------------------------------------------------- 

    public sealed class XslCompiledTransform { 
        // Permission set that contains Reflection [MemberAccess] permissions 
        private static readonly PermissionSet MemberAccessPermissionSet;
 
        // Version for GeneratedCodeAttribute
        private const string Version = ThisAssembly.Version;

        static XslCompiledTransform() { 
            MemberAccessPermissionSet = new PermissionSet(PermissionState.None);
            MemberAccessPermissionSet.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess)); 
        } 

        // Options of compilation 
        private bool                enableDebug     = false;

        // Results of compilation
        private CompilerResults     compilerResults = null; 
        private XmlWriterSettings   outputSettings  = null;
        private QilExpression       qil             = null; 
 
        // Executable command for the compiled stylesheet
        private XmlILCommand        command         = null; 

        public XslCompiledTransform() {}

        public XslCompiledTransform(bool enableDebug) { 
            this.enableDebug = enableDebug;
        } 
 
        /// <summary>
        /// This function is called on every recompilation to discard all previous results 
        /// </summary>
        private void Reset() {
            compilerResults = null;
            outputSettings  = null; 
            qil             = null;
            command         = null; 
        } 

        internal CompilerErrorCollection Errors { 
            get { return compilerResults != null ? compilerResults.Errors : null; }
        }

        /// <summary> 
        /// Writer settings specified in the stylesheet
        /// </summary> 
        public XmlWriterSettings OutputSettings { 
            get { return outputSettings; }
        } 

        public TempFileCollection TemporaryFiles {
            [PermissionSet(SecurityAction.LinkDemand, Name="FullTrust")]
            get { return compilerResults != null ? compilerResults.TempFiles : null; } 
        }
 
        //------------------------------------------------ 
        // Load methods
        //------------------------------------------------ 

        public void Load(XmlReader stylesheet) {
            Reset();
            LoadInternal(stylesheet, XsltSettings.Default, new XmlUrlResolver()); 
        }
 
        public void Load(XmlReader stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) { 
            Reset();
            LoadInternal(stylesheet, settings, stylesheetResolver); 
        }

        public void Load(IXPathNavigable stylesheet) {
            Reset(); 
            LoadInternal(stylesheet, XsltSettings.Default, new XmlUrlResolver());
        } 
 
        public void Load(IXPathNavigable stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            Reset(); 
            LoadInternal(stylesheet, settings, stylesheetResolver);
        }

        public void Load(string stylesheetUri) { 
            Reset();
            if (stylesheetUri == null) { 
                throw new ArgumentNullException("stylesheetUri"); 
            }
            LoadInternal(stylesheetUri, XsltSettings.Default, new XmlUrlResolver()); 
        }

        public void Load(string stylesheetUri, XsltSettings settings, XmlResolver stylesheetResolver) {
            Reset(); 
            if (stylesheetUri == null) {
                throw new ArgumentNullException("stylesheetUri"); 
            } 
            LoadInternal(stylesheetUri, settings, stylesheetResolver);
        } 

        private CompilerResults LoadInternal(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            if (stylesheet == null) {
                throw new ArgumentNullException("stylesheet"); 
            }
            if (settings == null) { 
                settings = XsltSettings.Default; 
            }
            CompileXsltToQil(stylesheet, settings, stylesheetResolver); 
            CompilerError error = GetFirstError();
            if (error != null) {
                throw new XslLoadException(error);
            } 
            if (!settings.CheckOnly) {
                CompileQilToMsil(settings); 
            } 
            return compilerResults;
        } 

        private void CompileXsltToQil(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) {
            this.compilerResults = new Compiler(settings, this.enableDebug, null).Compile(stylesheet, stylesheetResolver, out this.qil);
        } 

        /// <summary> 
        /// Returns the first compiler error except warnings 
        /// </summary>
        private CompilerError GetFirstError() { 
            foreach (CompilerError error in compilerResults.Errors) {
                if (!error.IsWarning) {
                    return error;
                } 
            }
            return null; 
        } 

        private void CompileQilToMsil(XsltSettings settings) { 
            this.command = new XmlILGenerator().Generate(this.qil, /*typeBuilder:*/null);
            this.outputSettings = this.command.StaticData.DefaultWriterSettings;
            this.qil = null;
        } 

        //------------------------------------------------ 
        // Compile stylesheet to a TypeBuilder 
        //------------------------------------------------
 
        private static ConstructorInfo GeneratedCodeCtor;

        public static CompilerErrorCollection CompileToType(XmlReader stylesheet, XsltSettings settings, XmlResolver stylesheetResolver, bool debug, TypeBuilder typeBuilder, string scriptAssemblyPath) {
            if (stylesheet == null) 
                throw new ArgumentNullException("stylesheet");
 
            if (typeBuilder == null) 
                throw new ArgumentNullException("typeBuilder");
 
            if (settings == null)
                settings = XsltSettings.Default;

            if (settings.EnableScript && scriptAssemblyPath == null) 
                throw new ArgumentNullException("scriptAssemblyPath");
 
            if (scriptAssemblyPath != null) 
                scriptAssemblyPath = Path.GetFullPath(scriptAssemblyPath);
 
            QilExpression qil;
            CompilerErrorCollection errors = new Compiler(settings, debug, scriptAssemblyPath).Compile(stylesheet, stylesheetResolver, out qil).Errors;

            if (!errors.HasErrors) { 
                // Mark the type with GeneratedCodeAttribute to identify its origin
                if (GeneratedCodeCtor == null) 
                    GeneratedCodeCtor = typeof(GeneratedCodeAttribute).GetConstructor(new Type[] { typeof(string), typeof(string) }); 

                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(GeneratedCodeCtor, 
                    new object[] { typeof(XslCompiledTransform).FullName, Version }));

                new XmlILGenerator().Generate(qil, typeBuilder);
            } 

            return errors; 
        } 

        //------------------------------------------------ 
        // Load compiled stylesheet from a Type
        //------------------------------------------------

        public void Load(Type compiledStylesheet) { 
            Reset();
            if (compiledStylesheet == null) 
                throw new ArgumentNullException("compiledStylesheet"); 

            object[] customAttrs = compiledStylesheet.GetCustomAttributes(typeof(GeneratedCodeAttribute), /*inherit:*/false); 
            GeneratedCodeAttribute generatedCodeAttr = customAttrs.Length > 0 ? (GeneratedCodeAttribute)customAttrs[0] : null;

            // If GeneratedCodeAttribute is not there, it is not a compiled stylesheet class
            if (generatedCodeAttr != null && generatedCodeAttr.Tool == typeof(XslCompiledTransform).FullName && generatedCodeAttr.Version == Version) { 
                FieldInfo fldData  = compiledStylesheet.GetField(XmlQueryStaticData.DataFieldName,  BindingFlags.Static | BindingFlags.NonPublic);
                FieldInfo fldTypes = compiledStylesheet.GetField(XmlQueryStaticData.TypesFieldName, BindingFlags.Static | BindingFlags.NonPublic); 
 
                // If private fields are not there, it is not a compiled stylesheet class
                if (fldData != null && fldTypes != null) { 
                    // Need MemberAccess reflection permission to access a private data field and create a delegate
                    new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();

                    // Check if query static data is already deserialized 
                    object value = fldData.GetValue(/*this:*/null);
                    byte[] data = value as byte[]; 
 
                    if (data != null) {
                        lock (data) { 
                            // After acquiring the lock check the field again
                            value = fldData.GetValue(/*this:*/null);
                            if (value == data) {
                                // Deserialize query static data and create the command 
                                MethodInfo methExec = compiledStylesheet.GetMethod("Execute", BindingFlags.Static | BindingFlags.NonPublic);
                                Delegate delExec = Delegate.CreateDelegate(typeof(ExecuteDelegate), methExec); 
                                value = new XmlILCommand((ExecuteDelegate)delExec, new XmlQueryStaticData(data, (Type[])fldTypes.GetValue(/*this:*/null))); 

                                // Store the constructed command in the same field 
                                System.Threading.Thread.MemoryBarrier();
                                fldData.SetValue(/*this:*/null, value);
                            }
                        } 
                    }
 
                    this.command = value as XmlILCommand; 
                }
            } 

            // Throw an exception if the command was not loaded
            if (this.command == null)
                throw new ArgumentException(Res.GetString(Res.Xslt_NotCompiledStylesheet, compiledStylesheet.FullName), "compiledStylesheet"); 

            // Otherwise set the OutputSettings property 
            this.outputSettings = this.command.StaticData.DefaultWriterSettings; 
        }
 
        public void Load(MethodInfo executeMethod, byte[] queryData, Type[] earlyBoundTypes) {
            Reset();
            if (executeMethod == null)
                throw new ArgumentNullException("executeMethod"); 

            if (queryData == null) 
                throw new ArgumentNullException("queryData"); 

            // earlyBoundTypes may be null 

            DynamicMethod dm = executeMethod as DynamicMethod;
            Delegate delExec = (dm != null) ? dm.CreateDelegate(typeof(ExecuteDelegate)) : Delegate.CreateDelegate(typeof(ExecuteDelegate), executeMethod);
            this.command = new XmlILCommand((ExecuteDelegate)delExec, new XmlQueryStaticData(queryData, earlyBoundTypes)); 
            this.outputSettings = this.command.StaticData.DefaultWriterSettings;
        } 
 
        //------------------------------------------------
        // Transform methods which take an IXPathNavigable 
        //------------------------------------------------

        public void Transform(IXPathNavigable input, XmlWriter results) {
            CheckCommand(); 
            CheckInput(input);
            command.Execute(input, new XmlUrlResolver(), (XsltArgumentList)null, results); 
        } 

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, XmlWriter results) { 
            CheckCommand();
            CheckInput(input);
            command.Execute(input, new XmlUrlResolver(), arguments, results);
        } 

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, TextWriter results) { 
            CheckCommand(); 
            CheckInput(input);
            command.Execute(input, new XmlUrlResolver(), arguments, results); 
        }

        public void Transform(IXPathNavigable input, XsltArgumentList arguments, Stream results) {
            CheckCommand(); 
            CheckInput(input);
            command.Execute(input, new XmlUrlResolver(), arguments, results); 
        } 

        //------------------------------------------------ 
        // Transform methods which take an XmlReader
        //------------------------------------------------

        public void Transform(XmlReader input, XmlWriter results) { 
            CheckCommand();
            CheckInput(input); 
            command.Execute(input, new XmlUrlResolver(), (XsltArgumentList)null, results); 
        }
 
        public void Transform(XmlReader input, XsltArgumentList arguments, XmlWriter results) {
            CheckCommand();
            CheckInput(input);
            command.Execute(input, new XmlUrlResolver(), arguments, results); 
        }
 
        public void Transform(XmlReader input, XsltArgumentList arguments, TextWriter results) { 
            CheckCommand();
            CheckInput(input); 
            command.Execute(input, new XmlUrlResolver(), arguments, results);
        }

        public void Transform(XmlReader input, XsltArgumentList arguments, Stream results) { 
            CheckCommand();
            CheckInput(input); 
            command.Execute(input, new XmlUrlResolver(), arguments, results); 
        }
 
        public void Transform(XmlReader input, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver) {
            CheckCommand();
            CheckInput(input);
            command.Execute(input, documentResolver, arguments, results); 
        }
 
        //------------------------------------------------ 
        // Transform methods which take a uri
        //------------------------------------------------ 

        public void Transform(string inputUri, string resultsFile) {
            CheckCommand();
            // SQLBUDT 276415: Prevent wiping out the content of the input file if the output file is the same 
            using (XmlReader input = CreateReader(inputUri)) {
                if (resultsFile == null) { 
                    throw new ArgumentNullException("resultsFile"); 
                }
                using (FileStream output = new FileStream(resultsFile, FileMode.Create, FileAccess.Write)) { 
                    command.Execute(input, new XmlUrlResolver(), (XsltArgumentList)null, output);
                }
            }
        } 

        public void Transform(string inputUri, XmlWriter results) { 
            CheckCommand(); 
            using (XmlReader input = CreateReader(inputUri)) {
                command.Execute(input, new XmlUrlResolver(), (XsltArgumentList)null, results); 
            }
        }

        public void Transform(string inputUri, XsltArgumentList arguments, XmlWriter results) { 
            CheckCommand();
            using (XmlReader input = CreateReader(inputUri)) { 
                command.Execute(input, new XmlUrlResolver(), arguments, results); 
            }
        } 

        public void Transform(string inputUri, XsltArgumentList arguments, TextWriter results) {
            CheckCommand();
            using (XmlReader input = CreateReader(inputUri)) { 
                command.Execute(input, new XmlUrlResolver(), arguments, results);
            } 
        } 

        public void Transform(string inputUri, XsltArgumentList arguments, Stream results) { 
            CheckCommand();
            using (XmlReader input = CreateReader(inputUri)) {
                command.Execute(input, new XmlUrlResolver(), arguments, results);
            } 
        }
 
        //------------------------------------------------ 
        // Helper methods
        //------------------------------------------------ 

        private void CheckCommand() {
            if (command == null) {
                throw new InvalidOperationException(Res.GetString(Res.Xslt_NoStylesheetLoaded)); 
            }
        } 
 
        private void CheckInput(object input) {
            if (input == null) { 
                throw new System.ArgumentNullException("input");
            }
        }
 
        private XmlReader CreateReader(string inputUri) {
            if (inputUri == null) { 
                throw new ArgumentNullException("inputUri"); 
            }
            return XmlReader.Create(inputUri); 
        }

        //------------------------------------------------
        // Test suites entry points 
        //------------------------------------------------
 
        private QilExpression TestCompile(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver) { 
            Reset();
            CompileXsltToQil(stylesheet, settings, stylesheetResolver); 
            return qil;
        }

        private void TestGenerate(XsltSettings settings) { 
            Debug.Assert(qil != null, "You must compile to Qil first");
            CompileQilToMsil(settings); 
        } 

        private void Transform(string inputUri, XsltArgumentList arguments, XmlWriter results, XmlResolver documentResolver) { 
            command.Execute(inputUri, documentResolver, arguments, results);
        }

        internal static void PrintQil(object qil, XmlWriter xw, bool printComments, bool printTypes, bool printLineInfo) { 
            QilExpression qilExpr = (QilExpression)qil;
            QilXmlWriter.Options options = QilXmlWriter.Options.None; 
            QilValidationVisitor.Validate(qilExpr); 
            if (printComments) options |= QilXmlWriter.Options.Annotations;
            if (printTypes) options |= QilXmlWriter.Options.TypeInfo; 
            if (printLineInfo) options |= QilXmlWriter.Options.LineInfo;
            QilXmlWriter qw = new QilXmlWriter(xw, options);
            qw.ToXml(qilExpr);
            xw.Flush(); 
        }
    } 
#endif // ! HIDE_XSL 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
