//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
 
namespace System.Data.Design {
 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data; 
    using System.Data.Common;
    using System.Design; 
    using System.Diagnostics; 
    using System.IO;
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Text;
    using System.Reflection;
 

    /// <summary> 
    ///     Public class to expose the DataSource Generator functionality. 
    /// </summary>
    public sealed class TypedDataSetGenerator  { 
        private static Assembly systemAssembly = Assembly.GetAssembly(typeof(System.Uri));
        private static Assembly dataAssembly = Assembly.GetAssembly(typeof(System.Data.SqlClient.SqlDataAdapter));
        private static Assembly xmlAssembly = Assembly.GetAssembly(typeof(System.Xml.Schema.XmlSchemaType));
 
        private static Assembly[] fixedReferences = new Assembly[] { systemAssembly, dataAssembly, xmlAssembly};
        private static Assembly[] referencedAssemblies = null; 
        private static Assembly entityAssembly; 
        private static string LINQOverTDSAssemblyName = "System.Data.DataSetExtensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
 
        private static string[] imports = new String[] { };

// we disable the warning about this type not being CLS-Compliant; the toolset is being updated and the type is compliant
        //#pragma warning disable 3003 
        public static ICollection<Assembly> ReferencedAssemblies {
//#pragma warning restore 3003 
            get { 
                return referencedAssemblies;
            } 
        }

        [Flags]
        public enum GenerateOption { 
            None = 0,
            HierarchicalUpdate = 1, 
            LinqOverTypedDatasets = 2, 
        }
 
        private TypedDataSetGenerator() {
            // avoid the class being Instantiated.
        }
 
        public static string GetProviderName(string inputFileContent) {
            return GetProviderName(inputFileContent, null); 
        } 

        public static string GetProviderName(string inputFileContent, string tableName) { 
            // Validate parameters.
            if (inputFileContent == null || inputFileContent.Length == 0) {
                throw new ArgumentException(SR.GetString(SR.CG_DataSetGeneratorFail_InputFileEmpty));
            } 

            // Make sure the XMLContent is valid (can generate a DataSet). 
            StringReader stringReader = new StringReader(inputFileContent); 

            DesignDataSource designDS = new DesignDataSource(); 

            try {
                designDS.ReadXmlSchema(stringReader);
            } 
            catch (Exception e) {
                string errorMessage = SR.GetString(SR.CG_DataSetGeneratorFail_UnableToConvertToDataSet, CreateExceptionMessage(e)); 
 
                throw new Exception(errorMessage, e);
            } 


            if (tableName == null || tableName.Length == 0) {
                if (designDS.DefaultConnection != null) { 
                    return designDS.DefaultConnection.Provider;
                } 
            } 
            else {
                DesignTable designTable = designDS.DesignTables[tableName]; 
                if (designTable != null) {
                    return designTable.Connection.Provider;
                }
            } 

            return null; 
        } 

        public static string Generate(DataSet dataSet, CodeNamespace codeNamespace, CodeDomProvider codeProvider) { 
            if(codeProvider == null) {
                throw new ArgumentException("codeProvider");
            }
            if (dataSet == null) { 
                throw new ArgumentException(SR.GetString(SR.CG_DataSetGeneratorFail_DatasetNull));
            } 
 
            StringWriter writer = new StringWriter(System.Globalization.CultureInfo.CurrentCulture);
            dataSet.WriteXmlSchema(writer); 

            StringBuilder schema = writer.GetStringBuilder();

            return Generate(schema.ToString(), null, codeNamespace, codeProvider); 
        }
 
        public static void Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, 
            CodeDomProvider codeProvider, DbProviderFactory specifiedFactory) {
            if (specifiedFactory != null) { 
                ProviderManager.ActiveFactoryContext = specifiedFactory;
            }

            try { 
                Generate(inputFileContent, compileUnit, mainNamespace, codeProvider);
            } 
            finally { 
                ProviderManager.ActiveFactoryContext = null;
            } 
        }

        public static void Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace,
            CodeDomProvider codeProvider, Hashtable customDBProviders) { 
            Generate(inputFileContent, compileUnit, mainNamespace,
            codeProvider, customDBProviders, GenerateOption.None); 
        } 

        public static void Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, 
            CodeDomProvider codeProvider, Hashtable customDBProviders, GenerateOption option) {
            Generate(inputFileContent, compileUnit, mainNamespace, codeProvider, customDBProviders, option, null);
        }
 
        public static void Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace,
            CodeDomProvider codeProvider, Hashtable customDBProviders, GenerateOption option, string dataSetNamespace) {		 
            if (customDBProviders != null) { 
                ProviderManager.CustomDBProviders = customDBProviders;
            } 

            try {
                Generate(inputFileContent, compileUnit, mainNamespace, codeProvider, option, dataSetNamespace);
            } 
            finally {
                ProviderManager.CustomDBProviders = null; 
            } 
        }
 
        public static string Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace,
            CodeDomProvider codeProvider) {
            return Generate(inputFileContent, compileUnit, mainNamespace,
            codeProvider, GenerateOption.None); 
        }
 
        public static string Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, 
            CodeDomProvider codeProvider, GenerateOption option) {
            return Generate(inputFileContent, compileUnit, mainNamespace, codeProvider, option, null); 
        }

        public static string Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace,
            CodeDomProvider codeProvider, GenerateOption option, string dataSetNamespace) { 
            // Validate parameters.
            if (inputFileContent == null || inputFileContent.Length == 0) { 
                throw new ArgumentException(SR.GetString(SR.CG_DataSetGeneratorFail_InputFileEmpty)); 
            }
            if (mainNamespace == null) { 
                throw new ArgumentException(SR.GetString(SR.CG_DataSetGeneratorFail_CodeNamespaceNull));
            }
            if (codeProvider == null) {
                throw new ArgumentException("codeProvider"); 
            }
 
            // Make sure the XMLContent is valid (can generate a DataSet). 
            StringReader stringReader = new StringReader(inputFileContent);
 
            DesignDataSource designDS = new DesignDataSource();

            try {
                designDS.ReadXmlSchema(stringReader); 
            }
            catch (Exception e) { 
                string errorMessage = SR.GetString(SR.CG_DataSetGeneratorFail_UnableToConvertToDataSet, CreateExceptionMessage(e)); 

                throw new Exception(errorMessage, e); 
            }

            return GenerateInternal(designDS, compileUnit, mainNamespace, codeProvider, option, dataSetNamespace);
        } 

 
 
        internal static string GenerateInternal(DesignDataSource designDS, CodeCompileUnit compileUnit, CodeNamespace mainNamespace,
            CodeDomProvider codeProvider, GenerateOption generateOption, string dataSetNamespace) { 
            if(StringUtil.Empty(designDS.Name)) {
                designDS.Name = "DataSet1";
            }
 
            try {
                TypedDataSourceCodeGenerator generator = new TypedDataSourceCodeGenerator(); 
                generator.CodeProvider = codeProvider; 
                generator.GenerateSingleNamespace = false;
 
                if (mainNamespace == null) {
                    mainNamespace = new CodeNamespace();
                }
                if (compileUnit == null) { 
                    compileUnit = new CodeCompileUnit();
                    compileUnit.Namespaces.Add(mainNamespace); 
                } 

                generator.GenerateDataSource(designDS, compileUnit, mainNamespace, dataSetNamespace, generateOption); 

                foreach (string import in imports) {
                    mainNamespace.Imports.Add(new CodeNamespaceImport(import));
                } 
            }
            catch (Exception e) { 
                string errorMessage = SR.GetString(SR.CG_DataSetGeneratorFail_FailToGenerateCode, CreateExceptionMessage(e)); 

                throw new Exception(errorMessage, e); 
            }

            System.Collections.ArrayList refAssemblies = new System.Collections.ArrayList(fixedReferences);
            refAssemblies.AddRange(TypedDataSourceCodeGenerator.GetProviderAssemblies(designDS)); 

            // Add a reference to the new LINQ over Typed Datasets DLL if necessary 
            if ((generateOption & GenerateOption.LinqOverTypedDatasets) == GenerateOption.LinqOverTypedDatasets) { 
                Assembly entityAssembly = EntityAssembly;
 
                if (entityAssembly != null) {
                    refAssemblies.Add(entityAssembly);
                }
                else { 
                    Debug.Fail("Could not load LinqOverTDS assembly!");
                } 
            } 

            referencedAssemblies = (Assembly[])refAssemblies.ToArray(typeof(Assembly)); 

            // Add the referenced assemblies to the CodeCompileUnit
            foreach (Assembly a in referencedAssemblies) {
                compileUnit.ReferencedAssemblies.Add(a.GetName().Name + ".dll"); 
            }
 
            return designDS.GeneratorDataSetName; 
        }
 

        private static Assembly EntityAssembly {
            get {
                if (entityAssembly == null) { 
                    try {
                        entityAssembly = Assembly.Load(LINQOverTDSAssemblyName); 
                    } 
                    catch { }
 
                    Debug.Assert(entityAssembly != null, "Could not load LinqOverTDS assembly!");
                }
                return entityAssembly;
            } 
        }
 
        private static string CreateExceptionMessage(Exception e) { 
            string message = (e.Message != null ? e.Message : string.Empty);
 
            Exception innerException = e.InnerException;
            while (innerException != null) {
                string innerMessage = innerException.Message;
                if (innerMessage != null && innerMessage.Length > 0) { 
                    message = message + " " + innerMessage;
                } 
                innerException = innerException.InnerException; 
            }
 
            return message;
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
    using System.Collections;
    using System.Collections.Generic;
    using System.Data; 
    using System.Data.Common;
    using System.Design; 
    using System.Diagnostics; 
    using System.IO;
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Text;
    using System.Reflection;
 

    /// <summary> 
    ///     Public class to expose the DataSource Generator functionality. 
    /// </summary>
    public sealed class TypedDataSetGenerator  { 
        private static Assembly systemAssembly = Assembly.GetAssembly(typeof(System.Uri));
        private static Assembly dataAssembly = Assembly.GetAssembly(typeof(System.Data.SqlClient.SqlDataAdapter));
        private static Assembly xmlAssembly = Assembly.GetAssembly(typeof(System.Xml.Schema.XmlSchemaType));
 
        private static Assembly[] fixedReferences = new Assembly[] { systemAssembly, dataAssembly, xmlAssembly};
        private static Assembly[] referencedAssemblies = null; 
        private static Assembly entityAssembly; 
        private static string LINQOverTDSAssemblyName = "System.Data.DataSetExtensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
 
        private static string[] imports = new String[] { };

// we disable the warning about this type not being CLS-Compliant; the toolset is being updated and the type is compliant
        //#pragma warning disable 3003 
        public static ICollection<Assembly> ReferencedAssemblies {
//#pragma warning restore 3003 
            get { 
                return referencedAssemblies;
            } 
        }

        [Flags]
        public enum GenerateOption { 
            None = 0,
            HierarchicalUpdate = 1, 
            LinqOverTypedDatasets = 2, 
        }
 
        private TypedDataSetGenerator() {
            // avoid the class being Instantiated.
        }
 
        public static string GetProviderName(string inputFileContent) {
            return GetProviderName(inputFileContent, null); 
        } 

        public static string GetProviderName(string inputFileContent, string tableName) { 
            // Validate parameters.
            if (inputFileContent == null || inputFileContent.Length == 0) {
                throw new ArgumentException(SR.GetString(SR.CG_DataSetGeneratorFail_InputFileEmpty));
            } 

            // Make sure the XMLContent is valid (can generate a DataSet). 
            StringReader stringReader = new StringReader(inputFileContent); 

            DesignDataSource designDS = new DesignDataSource(); 

            try {
                designDS.ReadXmlSchema(stringReader);
            } 
            catch (Exception e) {
                string errorMessage = SR.GetString(SR.CG_DataSetGeneratorFail_UnableToConvertToDataSet, CreateExceptionMessage(e)); 
 
                throw new Exception(errorMessage, e);
            } 


            if (tableName == null || tableName.Length == 0) {
                if (designDS.DefaultConnection != null) { 
                    return designDS.DefaultConnection.Provider;
                } 
            } 
            else {
                DesignTable designTable = designDS.DesignTables[tableName]; 
                if (designTable != null) {
                    return designTable.Connection.Provider;
                }
            } 

            return null; 
        } 

        public static string Generate(DataSet dataSet, CodeNamespace codeNamespace, CodeDomProvider codeProvider) { 
            if(codeProvider == null) {
                throw new ArgumentException("codeProvider");
            }
            if (dataSet == null) { 
                throw new ArgumentException(SR.GetString(SR.CG_DataSetGeneratorFail_DatasetNull));
            } 
 
            StringWriter writer = new StringWriter(System.Globalization.CultureInfo.CurrentCulture);
            dataSet.WriteXmlSchema(writer); 

            StringBuilder schema = writer.GetStringBuilder();

            return Generate(schema.ToString(), null, codeNamespace, codeProvider); 
        }
 
        public static void Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, 
            CodeDomProvider codeProvider, DbProviderFactory specifiedFactory) {
            if (specifiedFactory != null) { 
                ProviderManager.ActiveFactoryContext = specifiedFactory;
            }

            try { 
                Generate(inputFileContent, compileUnit, mainNamespace, codeProvider);
            } 
            finally { 
                ProviderManager.ActiveFactoryContext = null;
            } 
        }

        public static void Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace,
            CodeDomProvider codeProvider, Hashtable customDBProviders) { 
            Generate(inputFileContent, compileUnit, mainNamespace,
            codeProvider, customDBProviders, GenerateOption.None); 
        } 

        public static void Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, 
            CodeDomProvider codeProvider, Hashtable customDBProviders, GenerateOption option) {
            Generate(inputFileContent, compileUnit, mainNamespace, codeProvider, customDBProviders, option, null);
        }
 
        public static void Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace,
            CodeDomProvider codeProvider, Hashtable customDBProviders, GenerateOption option, string dataSetNamespace) {		 
            if (customDBProviders != null) { 
                ProviderManager.CustomDBProviders = customDBProviders;
            } 

            try {
                Generate(inputFileContent, compileUnit, mainNamespace, codeProvider, option, dataSetNamespace);
            } 
            finally {
                ProviderManager.CustomDBProviders = null; 
            } 
        }
 
        public static string Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace,
            CodeDomProvider codeProvider) {
            return Generate(inputFileContent, compileUnit, mainNamespace,
            codeProvider, GenerateOption.None); 
        }
 
        public static string Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, 
            CodeDomProvider codeProvider, GenerateOption option) {
            return Generate(inputFileContent, compileUnit, mainNamespace, codeProvider, option, null); 
        }

        public static string Generate(string inputFileContent, CodeCompileUnit compileUnit, CodeNamespace mainNamespace,
            CodeDomProvider codeProvider, GenerateOption option, string dataSetNamespace) { 
            // Validate parameters.
            if (inputFileContent == null || inputFileContent.Length == 0) { 
                throw new ArgumentException(SR.GetString(SR.CG_DataSetGeneratorFail_InputFileEmpty)); 
            }
            if (mainNamespace == null) { 
                throw new ArgumentException(SR.GetString(SR.CG_DataSetGeneratorFail_CodeNamespaceNull));
            }
            if (codeProvider == null) {
                throw new ArgumentException("codeProvider"); 
            }
 
            // Make sure the XMLContent is valid (can generate a DataSet). 
            StringReader stringReader = new StringReader(inputFileContent);
 
            DesignDataSource designDS = new DesignDataSource();

            try {
                designDS.ReadXmlSchema(stringReader); 
            }
            catch (Exception e) { 
                string errorMessage = SR.GetString(SR.CG_DataSetGeneratorFail_UnableToConvertToDataSet, CreateExceptionMessage(e)); 

                throw new Exception(errorMessage, e); 
            }

            return GenerateInternal(designDS, compileUnit, mainNamespace, codeProvider, option, dataSetNamespace);
        } 

 
 
        internal static string GenerateInternal(DesignDataSource designDS, CodeCompileUnit compileUnit, CodeNamespace mainNamespace,
            CodeDomProvider codeProvider, GenerateOption generateOption, string dataSetNamespace) { 
            if(StringUtil.Empty(designDS.Name)) {
                designDS.Name = "DataSet1";
            }
 
            try {
                TypedDataSourceCodeGenerator generator = new TypedDataSourceCodeGenerator(); 
                generator.CodeProvider = codeProvider; 
                generator.GenerateSingleNamespace = false;
 
                if (mainNamespace == null) {
                    mainNamespace = new CodeNamespace();
                }
                if (compileUnit == null) { 
                    compileUnit = new CodeCompileUnit();
                    compileUnit.Namespaces.Add(mainNamespace); 
                } 

                generator.GenerateDataSource(designDS, compileUnit, mainNamespace, dataSetNamespace, generateOption); 

                foreach (string import in imports) {
                    mainNamespace.Imports.Add(new CodeNamespaceImport(import));
                } 
            }
            catch (Exception e) { 
                string errorMessage = SR.GetString(SR.CG_DataSetGeneratorFail_FailToGenerateCode, CreateExceptionMessage(e)); 

                throw new Exception(errorMessage, e); 
            }

            System.Collections.ArrayList refAssemblies = new System.Collections.ArrayList(fixedReferences);
            refAssemblies.AddRange(TypedDataSourceCodeGenerator.GetProviderAssemblies(designDS)); 

            // Add a reference to the new LINQ over Typed Datasets DLL if necessary 
            if ((generateOption & GenerateOption.LinqOverTypedDatasets) == GenerateOption.LinqOverTypedDatasets) { 
                Assembly entityAssembly = EntityAssembly;
 
                if (entityAssembly != null) {
                    refAssemblies.Add(entityAssembly);
                }
                else { 
                    Debug.Fail("Could not load LinqOverTDS assembly!");
                } 
            } 

            referencedAssemblies = (Assembly[])refAssemblies.ToArray(typeof(Assembly)); 

            // Add the referenced assemblies to the CodeCompileUnit
            foreach (Assembly a in referencedAssemblies) {
                compileUnit.ReferencedAssemblies.Add(a.GetName().Name + ".dll"); 
            }
 
            return designDS.GeneratorDataSetName; 
        }
 

        private static Assembly EntityAssembly {
            get {
                if (entityAssembly == null) { 
                    try {
                        entityAssembly = Assembly.Load(LINQOverTDSAssemblyName); 
                    } 
                    catch { }
 
                    Debug.Assert(entityAssembly != null, "Could not load LinqOverTDS assembly!");
                }
                return entityAssembly;
            } 
        }
 
        private static string CreateExceptionMessage(Exception e) { 
            string message = (e.Message != null ? e.Message : string.Empty);
 
            Exception innerException = e.InnerException;
            while (innerException != null) {
                string innerMessage = innerException.Message;
                if (innerMessage != null && innerMessage.Length > 0) { 
                    message = message + " " + innerMessage;
                } 
                innerException = innerException.InnerException; 
            }
 
            return message;
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
