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
    using System.Collections;
    using System.ComponentModel; 
    using System.Data;
    using System.Data.SqlClient; 
    using System.Data.SqlTypes; 
    using System.IO;
    using System.Diagnostics; 

    public class MethodSignatureGenerator  {
        private static readonly char endOfStatement = ';';
 
        private CodeDomProvider codeProvider = null;
        private DbSource methodSource = null; 
        private Type containerParameterType = typeof(System.Data.DataSet); 
        private bool pagingMethod = false;
        private bool getMethod = false; 
        private ParameterGenerationOption parameterOption = ParameterGenerationOption.ClrTypes;
        private string tableClassName = null;
        private string datasetClassName = null;
        private DesignTable designTable = null; 

        public CodeDomProvider CodeProvider  { 
            get { 
                return this.codeProvider;
            } 
            set {
                this.codeProvider = value;
            }
        } 

        public Type ContainerParameterType { 
            get { 
                return this.containerParameterType;
            } 
            set {
                if(value != typeof(System.Data.DataSet) && value != typeof(System.Data.DataTable)) {
                    throw new InternalException("Unsupported container parameter type.");
                } 

                this.containerParameterType = value; 
            } 
        }
 
        public bool IsGetMethod {
            get {
                return this.getMethod;
            } 
            set {
                this.getMethod = value; 
            } 
        }
 
        public bool PagingMethod {
            get {
                return this.pagingMethod;
            } 
            set {
                this.pagingMethod = value; 
            } 
        }
 
        public ParameterGenerationOption ParameterOption {
            get {
                return this.parameterOption;
            } 
            set {
                this.parameterOption = value; 
            } 
        }
 
        public string TableClassName {
            get {
                return this.tableClassName;
            } 
            set {
                this.tableClassName = value; 
            } 
        }
 
        public string DataSetClassName {
            get {
                return this.datasetClassName;
            } 
            set {
                this.datasetClassName = value; 
            } 
        }
 
        public void SetDesignTableContent(string designTableContent) {
            DesignDataSource dataSource = new DesignDataSource();
            StringReader stringReader = new StringReader(designTableContent);
 
            dataSource.ReadXmlSchema(stringReader);
 
            if (dataSource.DesignTables == null || dataSource.DesignTables.Count != 1) { 
                throw new InternalException("Unexpected number of sources in deserialized DataSource.");
            } 

            IEnumerator e = dataSource.DesignTables.GetEnumerator();
            e.MoveNext();
            this.designTable = (DesignTable)e.Current; 
        }
 
        public void SetMethodSourceContent(string methodSourceContent) { 
            DesignDataSource dataSource = new DesignDataSource();
            StringReader stringReader = new StringReader(methodSourceContent); 

            dataSource.ReadXmlSchema(stringReader);

            if (dataSource.Sources == null || dataSource.Sources.Count != 1) { 
                throw new InternalException("Unexpected number of sources in deserialized DataSource.");
            } 
 
            IEnumerator e = dataSource.Sources.GetEnumerator();
            e.MoveNext(); 
            this.methodSource = (DbSource)e.Current;
        }

        public string GenerateMethodSignature() { 
            if(this.codeProvider == null) {
                throw new ArgumentException("codeProvider"); 
            } 
            if(this.methodSource == null) {
                throw new ArgumentException("MethodSource"); 
            }

            string methodName = null;
            CodeTypeDeclaration methodWrapper = this.GenerateMethodWrapper(out methodName); 

            StringWriter writer = new StringWriter(System.Globalization.CultureInfo.CurrentCulture); 
            codeProvider.GenerateCodeFromType(methodWrapper, writer, null); 

            string wrapperCode = writer.GetStringBuilder().ToString(); 
            string[] codeLines = wrapperCode.Split(Environment.NewLine.ToCharArray());

            foreach(string line in codeLines) {
                if(line.Contains(methodName)) { 
                    return line.Trim().TrimEnd(endOfStatement);
                } 
            } 

            return null; 
        }

        private CodeTypeDeclaration GenerateMethodWrapper(out string methodName) {
            CodeTypeDeclaration wrapper = new CodeTypeDeclaration("Wrapper"); 
            wrapper.IsInterface = true;
 
            CodeMemberMethod method = GenerateMethod(); 
            wrapper.Members.Add(method);
 
            methodName = method.Name;

            return wrapper;
        } 

 
 
        public CodeMemberMethod GenerateMethod() {
            if(this.codeProvider == null) { 
                throw new ArgumentException("codeProvider");
            }
            if(this.methodSource == null) {
                throw new ArgumentException("MethodSource"); 
            }
 
            QueryGeneratorBase queryGenerator = null; 
            if(this.methodSource.QueryType == QueryType.Rowset && this.methodSource.CommandOperation == CommandOperation.Select) {
                queryGenerator = new QueryGenerator(null); 

                queryGenerator.ContainerParameterTypeName = this.GetParameterTypeName();
                queryGenerator.ContainerParameterName = this.GetParameterName();
                queryGenerator.ContainerParameterType = this.containerParameterType; 
            }
            else { 
                queryGenerator = new FunctionGenerator(null); 
            }
            queryGenerator.DeclarationOnly = true; 
            queryGenerator.CodeProvider = this.codeProvider;
            queryGenerator.MethodSource = this.methodSource;
            queryGenerator.MethodName = this.GetMethodName();
            queryGenerator.ParameterOption = this.parameterOption; 
            queryGenerator.GeneratePagingMethod = this.pagingMethod;
            queryGenerator.GenerateGetMethod = this.getMethod; 
 
            CodeMemberMethod method = queryGenerator.Generate();
 
            return method;
        }

 
        public CodeTypeDeclaration GenerateUpdatingMethods() {
            if (this.designTable == null) { 
                throw new InternalException("DesignTable should not be null."); 
            }
            if (StringUtil.Empty(this.datasetClassName)) { 
                throw new InternalException("DatasetClassName should not be empty.");
            }

            CodeTypeDeclaration wrapper = new CodeTypeDeclaration("wrapper"); 
            wrapper.IsInterface = true;
 
            QueryHandler queryHandler = new QueryHandler( this.codeProvider, this.designTable ); 
            queryHandler.DeclarationsOnly = true;
            queryHandler.AddUpdateQueriesToDataComponent( wrapper, this.datasetClassName, this.codeProvider ); 

            return wrapper;
        }
 

        private string GetParameterName() { 
            if(this.containerParameterType == typeof(System.Data.DataTable)) { 
                return QueryHandler.tableParameterName;
            } 
            else {
                return QueryHandler.dataSetParameterName;
            }
        } 

        private string GetParameterTypeName() { 
            if (StringUtil.Empty(this.datasetClassName)) { 
                throw new InternalException("DatasetClassName should not be empty.");
            } 

            if (this.containerParameterType == typeof(System.Data.DataTable)) {
                if (StringUtil.Empty(this.tableClassName)) {
                    throw new InternalException("TableClassName should not be empty."); 
                }
 
                return CodeGenHelper.GetTypeName(this.codeProvider, this.datasetClassName, this.tableClassName); 
            }
            else { 
                return this.datasetClassName;
            }
        }
 
        private string GetMethodName() {
            if(this.methodSource.QueryType == QueryType.Rowset) { 
                if(this.getMethod) { 
                    if(this.pagingMethod) {
                        // GetPage method 
                        if(methodSource.GeneratorGetMethodNameForPaging != null) {
                            return methodSource.GeneratorGetMethodNameForPaging;
                        }
                        else { 
                            return methodSource.GetMethodName + DataComponentNameHandler.PagingMethodSuffix;
                        } 
                    } 
                    else {
                        // Get method 
                        if(methodSource.GeneratorGetMethodName != null) {
                            return methodSource.GeneratorGetMethodName;
                        }
                        else { 
                            return methodSource.GetMethodName;
                        } 
                    } 
                }
                else { 
                    if(this.pagingMethod) {
                        // FillPage method
                        if(methodSource.GeneratorSourceNameForPaging != null) {
                            return methodSource.GeneratorSourceNameForPaging; 
                        }
                        else { 
                            return methodSource.Name + DataComponentNameHandler.PagingMethodSuffix; 
                        }
                    } 
                    else {
                        // Fill method
                        if(methodSource.GeneratorSourceName != null) {
                            return methodSource.GeneratorSourceName; 
                        }
                        else { 
                            return methodSource.Name; 
                        }
                    } 
                }
            }
            else {
                if(methodSource.GeneratorSourceName != null) { 
                    return methodSource.GeneratorSourceName;
                } 
                else { 
                    return methodSource.Name;
                } 
            }
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
    using System.Collections;
    using System.ComponentModel; 
    using System.Data;
    using System.Data.SqlClient; 
    using System.Data.SqlTypes; 
    using System.IO;
    using System.Diagnostics; 

    public class MethodSignatureGenerator  {
        private static readonly char endOfStatement = ';';
 
        private CodeDomProvider codeProvider = null;
        private DbSource methodSource = null; 
        private Type containerParameterType = typeof(System.Data.DataSet); 
        private bool pagingMethod = false;
        private bool getMethod = false; 
        private ParameterGenerationOption parameterOption = ParameterGenerationOption.ClrTypes;
        private string tableClassName = null;
        private string datasetClassName = null;
        private DesignTable designTable = null; 

        public CodeDomProvider CodeProvider  { 
            get { 
                return this.codeProvider;
            } 
            set {
                this.codeProvider = value;
            }
        } 

        public Type ContainerParameterType { 
            get { 
                return this.containerParameterType;
            } 
            set {
                if(value != typeof(System.Data.DataSet) && value != typeof(System.Data.DataTable)) {
                    throw new InternalException("Unsupported container parameter type.");
                } 

                this.containerParameterType = value; 
            } 
        }
 
        public bool IsGetMethod {
            get {
                return this.getMethod;
            } 
            set {
                this.getMethod = value; 
            } 
        }
 
        public bool PagingMethod {
            get {
                return this.pagingMethod;
            } 
            set {
                this.pagingMethod = value; 
            } 
        }
 
        public ParameterGenerationOption ParameterOption {
            get {
                return this.parameterOption;
            } 
            set {
                this.parameterOption = value; 
            } 
        }
 
        public string TableClassName {
            get {
                return this.tableClassName;
            } 
            set {
                this.tableClassName = value; 
            } 
        }
 
        public string DataSetClassName {
            get {
                return this.datasetClassName;
            } 
            set {
                this.datasetClassName = value; 
            } 
        }
 
        public void SetDesignTableContent(string designTableContent) {
            DesignDataSource dataSource = new DesignDataSource();
            StringReader stringReader = new StringReader(designTableContent);
 
            dataSource.ReadXmlSchema(stringReader);
 
            if (dataSource.DesignTables == null || dataSource.DesignTables.Count != 1) { 
                throw new InternalException("Unexpected number of sources in deserialized DataSource.");
            } 

            IEnumerator e = dataSource.DesignTables.GetEnumerator();
            e.MoveNext();
            this.designTable = (DesignTable)e.Current; 
        }
 
        public void SetMethodSourceContent(string methodSourceContent) { 
            DesignDataSource dataSource = new DesignDataSource();
            StringReader stringReader = new StringReader(methodSourceContent); 

            dataSource.ReadXmlSchema(stringReader);

            if (dataSource.Sources == null || dataSource.Sources.Count != 1) { 
                throw new InternalException("Unexpected number of sources in deserialized DataSource.");
            } 
 
            IEnumerator e = dataSource.Sources.GetEnumerator();
            e.MoveNext(); 
            this.methodSource = (DbSource)e.Current;
        }

        public string GenerateMethodSignature() { 
            if(this.codeProvider == null) {
                throw new ArgumentException("codeProvider"); 
            } 
            if(this.methodSource == null) {
                throw new ArgumentException("MethodSource"); 
            }

            string methodName = null;
            CodeTypeDeclaration methodWrapper = this.GenerateMethodWrapper(out methodName); 

            StringWriter writer = new StringWriter(System.Globalization.CultureInfo.CurrentCulture); 
            codeProvider.GenerateCodeFromType(methodWrapper, writer, null); 

            string wrapperCode = writer.GetStringBuilder().ToString(); 
            string[] codeLines = wrapperCode.Split(Environment.NewLine.ToCharArray());

            foreach(string line in codeLines) {
                if(line.Contains(methodName)) { 
                    return line.Trim().TrimEnd(endOfStatement);
                } 
            } 

            return null; 
        }

        private CodeTypeDeclaration GenerateMethodWrapper(out string methodName) {
            CodeTypeDeclaration wrapper = new CodeTypeDeclaration("Wrapper"); 
            wrapper.IsInterface = true;
 
            CodeMemberMethod method = GenerateMethod(); 
            wrapper.Members.Add(method);
 
            methodName = method.Name;

            return wrapper;
        } 

 
 
        public CodeMemberMethod GenerateMethod() {
            if(this.codeProvider == null) { 
                throw new ArgumentException("codeProvider");
            }
            if(this.methodSource == null) {
                throw new ArgumentException("MethodSource"); 
            }
 
            QueryGeneratorBase queryGenerator = null; 
            if(this.methodSource.QueryType == QueryType.Rowset && this.methodSource.CommandOperation == CommandOperation.Select) {
                queryGenerator = new QueryGenerator(null); 

                queryGenerator.ContainerParameterTypeName = this.GetParameterTypeName();
                queryGenerator.ContainerParameterName = this.GetParameterName();
                queryGenerator.ContainerParameterType = this.containerParameterType; 
            }
            else { 
                queryGenerator = new FunctionGenerator(null); 
            }
            queryGenerator.DeclarationOnly = true; 
            queryGenerator.CodeProvider = this.codeProvider;
            queryGenerator.MethodSource = this.methodSource;
            queryGenerator.MethodName = this.GetMethodName();
            queryGenerator.ParameterOption = this.parameterOption; 
            queryGenerator.GeneratePagingMethod = this.pagingMethod;
            queryGenerator.GenerateGetMethod = this.getMethod; 
 
            CodeMemberMethod method = queryGenerator.Generate();
 
            return method;
        }

 
        public CodeTypeDeclaration GenerateUpdatingMethods() {
            if (this.designTable == null) { 
                throw new InternalException("DesignTable should not be null."); 
            }
            if (StringUtil.Empty(this.datasetClassName)) { 
                throw new InternalException("DatasetClassName should not be empty.");
            }

            CodeTypeDeclaration wrapper = new CodeTypeDeclaration("wrapper"); 
            wrapper.IsInterface = true;
 
            QueryHandler queryHandler = new QueryHandler( this.codeProvider, this.designTable ); 
            queryHandler.DeclarationsOnly = true;
            queryHandler.AddUpdateQueriesToDataComponent( wrapper, this.datasetClassName, this.codeProvider ); 

            return wrapper;
        }
 

        private string GetParameterName() { 
            if(this.containerParameterType == typeof(System.Data.DataTable)) { 
                return QueryHandler.tableParameterName;
            } 
            else {
                return QueryHandler.dataSetParameterName;
            }
        } 

        private string GetParameterTypeName() { 
            if (StringUtil.Empty(this.datasetClassName)) { 
                throw new InternalException("DatasetClassName should not be empty.");
            } 

            if (this.containerParameterType == typeof(System.Data.DataTable)) {
                if (StringUtil.Empty(this.tableClassName)) {
                    throw new InternalException("TableClassName should not be empty."); 
                }
 
                return CodeGenHelper.GetTypeName(this.codeProvider, this.datasetClassName, this.tableClassName); 
            }
            else { 
                return this.datasetClassName;
            }
        }
 
        private string GetMethodName() {
            if(this.methodSource.QueryType == QueryType.Rowset) { 
                if(this.getMethod) { 
                    if(this.pagingMethod) {
                        // GetPage method 
                        if(methodSource.GeneratorGetMethodNameForPaging != null) {
                            return methodSource.GeneratorGetMethodNameForPaging;
                        }
                        else { 
                            return methodSource.GetMethodName + DataComponentNameHandler.PagingMethodSuffix;
                        } 
                    } 
                    else {
                        // Get method 
                        if(methodSource.GeneratorGetMethodName != null) {
                            return methodSource.GeneratorGetMethodName;
                        }
                        else { 
                            return methodSource.GetMethodName;
                        } 
                    } 
                }
                else { 
                    if(this.pagingMethod) {
                        // FillPage method
                        if(methodSource.GeneratorSourceNameForPaging != null) {
                            return methodSource.GeneratorSourceNameForPaging; 
                        }
                        else { 
                            return methodSource.Name + DataComponentNameHandler.PagingMethodSuffix; 
                        }
                    } 
                    else {
                        // Fill method
                        if(methodSource.GeneratorSourceName != null) {
                            return methodSource.GeneratorSourceName; 
                        }
                        else { 
                            return methodSource.Name; 
                        }
                    } 
                }
            }
            else {
                if(methodSource.GeneratorSourceName != null) { 
                    return methodSource.GeneratorSourceName;
                } 
                else { 
                    return methodSource.Name;
                } 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
