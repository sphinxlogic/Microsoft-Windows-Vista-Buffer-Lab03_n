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
    using System.Data.Common; 
    using System.Data.SqlClient; 
    using System.Data.SqlTypes;
    using System.Data.Odbc; 
    using System.Data.OleDb;
    using System.Data.OracleClient;
    using System.Design;
    using System.Reflection; 

 
    public enum ParameterGenerationOption                                                                                                            { 
        ClrTypes    = 0,
        SqlTypes    = 1, 
        Objects     = 2
    }

    internal abstract class QueryGeneratorBase { 
        protected TypedDataSourceCodeGenerator codeGenerator = null;
        protected GenericNameHandler nameHandler = null; 
 
        protected static string returnVariableName = "returnValue";
        protected static string commandVariableName = "command"; 
        protected static string startRecordParameterName = "startRecord";
        protected static string maxRecordsParameterName = "maxRecords";

 
        // generation settings
        protected DbProviderFactory providerFactory = null; 
        protected DbSource methodSource = null; 
        protected DbSourceCommand activeCommand = null;
        protected string methodName = null; 
        protected MemberAttributes methodAttributes;
        protected Type containerParamType = typeof(System.Data.DataSet);
        protected string containerParamTypeName = null;
        protected string containerParamName = "dataSet"; 
        protected ParameterGenerationOption parameterOption = ParameterGenerationOption.ClrTypes;
        protected Type returnType = typeof(void); 
        protected int commandIndex = 0; 
        protected DesignTable designTable = null;
        protected bool getMethod = false; 
        protected bool pagingMethod = false;
        protected bool declarationOnly = false;
        protected MethodTypeEnum methodType = MethodTypeEnum.ColumnParameters;
        protected string updateParameterName = null; 
        protected CodeTypeReference updateParameterTypeReference = CodeGenHelper.GlobalType(typeof(System.Data.DataSet));
        protected string updateParameterTypeName = null; 
        protected CodeDomProvider codeProvider = null; 
        protected string updateCommandName = null;
        protected bool isFunctionsDataComponent = false; 

        //
        // SqlCeParameter is in DotNet Framework 3.5, while our code generator is in 2.0
        // So we need to workaround to use runtime type. 
        //
        private static Type sqlCeParameterType; 
        private const string SqlCeParameterTypeName = @"System.Data.SqlServerCe.SqlCeParameter, System.Data.SqlServerCe, Version=3.5.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91"; 
        private static object sqlCeParameterInstance;
        private static PropertyDescriptor sqlCeParaDbTypeDescriptor; 

        internal QueryGeneratorBase(TypedDataSourceCodeGenerator codeGenerator) {
            this.codeGenerator = codeGenerator;
        } 

        internal DbProviderFactory ProviderFactory { 
            get { 
                return this.providerFactory;
            } 
            set {
                this.providerFactory = value;
            }
        } 

        internal DbSource MethodSource { 
            get { 
                return this.methodSource;
            } 
            set {
                this.methodSource = value;
            }
        } 

        internal DbSourceCommand ActiveCommand { 
            get { 
                return this.activeCommand;
            } 
            set {
                this.activeCommand = value;
            }
        } 

        internal string MethodName { 
            get { 
                return this.methodName;
            } 
            set {
                this.methodName = value;
            }
        } 

        internal ParameterGenerationOption ParameterOption { 
            get { 
                return this.parameterOption;
            } 
            set {
                this.parameterOption = value;
            }
        } 

        internal Type ContainerParameterType { 
            get { 
                return this.containerParamType;
            } 
            set {
                this.containerParamType = value;
            }
        } 

        internal string ContainerParameterTypeName { 
            get { 
                return this.containerParamTypeName;
            } 
            set {
                this.containerParamTypeName = value;
            }
        } 

        internal string ContainerParameterName { 
            get { 
                return this.containerParamName;
            } 
            set {
                this.containerParamName = value;
            }
        } 

        internal int CommandIndex { 
            get { 
                return this.commandIndex;
            } 
            set {
                this.commandIndex = value;
            }
        } 

        internal DesignTable DesignTable { 
            get { 
                return this.designTable;
            } 
            set {
                this.designTable = value;
            }
        } 

        //internal bool GenerateOverloads { 
        //    get { 
        //        if (methodSource != null) {
        //            DbSourceCommand activeCmd = methodSource.GetActiveCommand(); 
        //            if (activeCmd != null && activeCmd.Parameters != null && activeCmd.Parameters.Count > 0
        //                && !(activeCmd.Parameters.Count == 1 && activeCmd.Parameters[0].Direction == ParameterDirection.ReturnValue)) {
        //                    // we know we have parameters, now let's check if they're all of type object, or if we have strongly
        //                    // typed ones 
        //                    bool nonObjectParameter = false;
        //                    foreach (DesignParameter parameter in this.activeCommand.Parameters) { 
        //                        if (parameter.Direction == ParameterDirection.ReturnValue) { 
        //                            // skip over return parameter
        //                            continue; 
        //                        }

        //                        // get the type of the parameter
        //                        Type parameterType = this.GetParameterUrtType(parameter); 
        //                        if (parameterType != typeof(object)) {
        //                            nonObjectParameter = true; 
        //                            break; 
        //                        }
        //                    } 

        //                    return nonObjectParameter;
        //            }
        //        } 

        //        return false; 
        //    } 
        //}
 
        internal bool GenerateGetMethod {
            get {
                return this.getMethod;
            } 
            set {
                this.getMethod = value; 
            } 
        }
 
        internal bool GeneratePagingMethod {
            get {
                return this.pagingMethod;
            } 
            set {
                this.pagingMethod = value; 
            } 
        }
 
        internal bool DeclarationOnly {
            get {
                return this.declarationOnly;
            } 
            set {
                this.declarationOnly = value; 
            } 
        }
 
        internal MethodTypeEnum MethodType {
            get {
                return this.methodType;
            } 
            set {
                this.methodType = value; 
            } 
        }
 
        internal string UpdateParameterName {
            get {
                return this.updateParameterName;
            } 
            set {
                this.updateParameterName = value; 
            } 
        }
 
        internal string UpdateParameterTypeName {
            get {
                return this.updateParameterTypeName;
            } 
            set {
                this.updateParameterTypeName = value; 
            } 
        }
 
        internal CodeTypeReference UpdateParameterTypeReference {
            get {
                return this.updateParameterTypeReference;
            } 
            set {
                this.updateParameterTypeReference = value; 
            } 
        }
 
        internal CodeDomProvider CodeProvider {
            get {
                return this.codeProvider;
            } 
            set {
                this.codeProvider = value; 
            } 
        }
 
        internal string UpdateCommandName {
            get {
                return this.updateCommandName;
            } 
            set {
                this.updateCommandName = value; 
            } 
        }
 
        internal bool IsFunctionsDataComponent {
            get {
                return this.isFunctionsDataComponent;
            } 
            set {
                this.isFunctionsDataComponent = value; 
            } 

        } 

        internal static Type SqlCeParameterType {
            get {
                if (sqlCeParameterType == null) { 
                    try {
                        sqlCeParameterType = Type.GetType(SqlCeParameterTypeName); 
                    } 
                    catch (System.IO.FileLoadException) {
                    } 
                }
                return sqlCeParameterType;
            }
        } 

        internal static object SqlCeParameterInstance { 
            get { 
                if (sqlCeParameterInstance == null && SqlCeParameterType != null) {
                    sqlCeParameterInstance = Activator.CreateInstance(SqlCeParameterType); 
                }
                return sqlCeParameterInstance;
            }
        } 

        internal static PropertyDescriptor SqlCeParaDbTypeDescriptor { 
            get { 
                if (sqlCeParaDbTypeDescriptor == null && SqlCeParameterType != null) {
                    sqlCeParaDbTypeDescriptor = TypeDescriptor.GetProperties(SqlCeParameterType)["DbType"]; 
                }
                return sqlCeParaDbTypeDescriptor;
            }
        } 

        internal static CodeStatement SetCommandTextStatement(CodeExpression commandExpression, string commandText) { 
            //\\ <command>.CommandText = "<commandText>"; 
            CodeStatement statement = CodeGenHelper.Assign(
                CodeGenHelper.Property( 
                    commandExpression,
                    "CommandText"
                ),
                CodeGenHelper.Str(commandText) 
            );
 
            return statement; 
        }
 
        internal static CodeStatement SetCommandTypeStatement(CodeExpression commandExpression, CommandType commandType) {
            //\\ <command>.CommandType = <CommandType>
            CodeStatement statement = CodeGenHelper.Assign(
                CodeGenHelper.Property( 
                    commandExpression,
                    "CommandType" 
                ), 
                CodeGenHelper.Field(
                    CodeGenHelper.GlobalTypeExpr(typeof(System.Data.CommandType)), 
                    ((CommandType) commandType).ToString()
                )
            );
 
            return statement;
        } 
 

        internal abstract CodeMemberMethod Generate(); 


        protected DesignParameter GetReturnParameter(DbSourceCommand command) {
            foreach (DesignParameter parameter in command.Parameters) { 
                if (parameter.Direction == ParameterDirection.ReturnValue) {
                    return parameter; 
                } 
            }
 
            return null;
        }

        protected int GetReturnParameterPosition(DbSourceCommand command) { 
            if(command == null || command.Parameters == null) {
                return -1; 
            } 

            for(int i = 0; i < command.Parameters.Count; i++) { 
                if( ((DesignParameter) command.Parameters[i]).Direction == ParameterDirection.ReturnValue ) {
                    return i;
                }
            } 

            return -1; 
        } 

 

        internal static CodeExpression AddNewParameterStatements(DesignParameter parameter, Type parameterType, DbProviderFactory factory, IList statements, CodeExpression parameterVariable) {
            if(parameterType == typeof(System.Data.SqlClient.SqlParameter)) {
                return BuildNewSqlParameterStatement(parameter); 
            }
            else if(parameterType == typeof(System.Data.OleDb.OleDbParameter)) { 
                return BuildNewOleDbParameterStatement(parameter); 
            }
            else if(parameterType == typeof(System.Data.Odbc.OdbcParameter)) { 
                return BuildNewOdbcParameterStatement(parameter);
            }
            else if(parameterType == typeof(System.Data.OracleClient.OracleParameter)) {
                return BuildNewOracleParameterStatement(parameter); 
            }
            else if (parameterType == SqlCeParameterType && StringUtil.NotEmptyAfterTrim(parameter.ProviderType)) { 
                // DDB 111450 -- Only Use BuildNewSqlCeParameterStatement if we know parameter.ProviderType is defined. 
                return BuildNewSqlCeParameterStatement(parameter);
            } 
            else {
                // for extensibility, we also deal with unknown providers, and thus unknown connection/command/parameter types
                return BuildNewUnknownParameterStatements(parameter, parameterType, factory, statements, parameterVariable);
            } 
        }
 
        private static CodeExpression BuildNewSqlParameterStatement(DesignParameter parameter) { 
            SqlParameter sqlParameter = new SqlParameter();
            SqlDbType sqlDbType = SqlDbType.Char;   // make compiler happy 
            bool parsed = false;

            if( parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) {
                try { 
                    sqlDbType = (SqlDbType) Enum.Parse( typeof(SqlDbType), parameter.ProviderType );
                    parsed = true; 
                } 
                catch { }
            } 

            if( !parsed ) {
                // setting the DbType on a SqlParameter automatically sets the SqlDbType, which is what we need
                sqlParameter.DbType = parameter.DbType; 
                sqlDbType = sqlParameter.SqlDbType;
            } 
 
            return NewParameter(parameter, typeof(System.Data.SqlClient.SqlParameter), typeof(System.Data.SqlDbType), sqlDbType.ToString());
        } 

        /// <summary>
        /// </summary>
        /// <param name="parameter"></param> 
        /// <returns></returns>
        private static CodeExpression BuildNewSqlCeParameterStatement(DesignParameter parameter) { 
            SqlDbType sqlDbType = SqlDbType.Char;   // make compiler happy 
            bool parsed = false;
 
            if (parameter.ProviderType != null && parameter.ProviderType.Length > 0) {
                try {
                    sqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), parameter.ProviderType);
                    parsed = true; 
                }
                catch { } 
            } 

            if (!parsed){ 
                object sqlParaInstance = SqlCeParameterInstance;
                if (sqlParaInstance != null) {
                    PropertyDescriptor dbTypePropDesc = SqlCeParaDbTypeDescriptor;
                    if (dbTypePropDesc != null) { 
                        // setting the DbType on a SqlParameter automatically sets the SqlDbType, which is what we need
                        dbTypePropDesc.SetValue(sqlParaInstance, parameter.DbType); 
                        sqlDbType = (SqlDbType)dbTypePropDesc.GetValue(sqlParaInstance); 
                    }
                } 
            }

            return NewParameter(parameter, SqlCeParameterType, typeof(System.Data.SqlDbType), sqlDbType.ToString());
        } 

        private static CodeExpression BuildNewOleDbParameterStatement(DesignParameter parameter) { 
            OleDbParameter oleDbParameter = new OleDbParameter(); 
            OleDbType oleDbType = OleDbType.Char; // make compiler happy
            bool parsed = false; 

            if( parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) {
                try {
                    oleDbType = (OleDbType) Enum.Parse( typeof(OleDbType), parameter.ProviderType ); 
                    parsed = true;
                } 
                catch { } 
            }
 
            if( !parsed ) {
                // setting the DbType on an OleDbParameter automatically sets the OleDbType, which is what we need
                oleDbParameter.DbType = parameter.DbType;
                oleDbType = oleDbParameter.OleDbType; 
            }
 
            return NewParameter(parameter, typeof(System.Data.OleDb.OleDbParameter), typeof(System.Data.OleDb.OleDbType), oleDbType.ToString()); 
        }
 
        private static CodeExpression BuildNewOdbcParameterStatement(DesignParameter parameter) {
            OdbcParameter odbcParameter = new OdbcParameter();
            OdbcType odbcType = OdbcType.Char;  // make compiler happy
            bool parsed = false; 

            if( parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) { 
                try { 
                    odbcType = (OdbcType) Enum.Parse( typeof(OdbcType), parameter.ProviderType );
                    parsed = true; 
                }
                catch { }
            }
 
            if( !parsed ) {
                // setting the DbType on an OdbcParameter automatically sets the OdbcDbType, which is what we need 
                odbcParameter.DbType = parameter.DbType; 
                odbcType = odbcParameter.OdbcType;
            } 

            return NewParameter(parameter, typeof(System.Data.Odbc.OdbcParameter), typeof(System.Data.Odbc.OdbcType), odbcType.ToString());
        }
 
        private static CodeExpression BuildNewOracleParameterStatement(DesignParameter parameter) {
            OracleParameter oracleParameter = new OracleParameter(); 
            OracleType oracleType = OracleType.Char;    // make compiler happy 
            bool parsed = false;
 
            if( parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) {
                try {
                    oracleType = (OracleType) Enum.Parse( typeof(OracleType), parameter.ProviderType );
                    parsed = true; 
                }
                catch { } 
            } 

            if( !parsed ) { 
                // setting the DbType on an OracleParameter automatically sets the OracleDbType, which is what we need
                oracleParameter.DbType = parameter.DbType;
                oracleType = oracleParameter.OracleType;
            } 

 
            return NewParameter(parameter, typeof(System.Data.OracleClient.OracleParameter), typeof(System.Data.OracleClient.OracleType), oracleType.ToString()); 
        }
 
        private static CodeExpression NewParameter(DesignParameter parameter, Type parameterType, Type typeEnumType, string typeEnumValue) {
            CodeExpression newParam = null;

            if(parameterType == typeof(System.Data.SqlClient.SqlParameter)) { 
                //\\ new SqlParameter("<parameterName>", <Type>, <Size>, <Direction>,
                //\\        <precision>, <scale>, <sourceColumn>, <DataRowVersion>, <SourceColumnNullMapping>, <Value>, "", "", "" ) 
                newParam = CodeGenHelper.New( 
                    CodeGenHelper.GlobalType(parameterType),
                    new CodeExpression[] { 
                        CodeGenHelper.Str(parameter.ParameterName),
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeEnumType),
                            typeEnumValue 
                        ),
                        CodeGenHelper.Primitive(parameter.Size), 
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ParameterDirection)),
                            parameter.Direction.ToString() 
                        ),
                        CodeGenHelper.Primitive(parameter.Precision),
                        CodeGenHelper.Primitive(parameter.Scale),
                        CodeGenHelper.Str(parameter.SourceColumn), 
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataRowVersion)), 
                            parameter.SourceVersion.ToString() 
                        ),
                        CodeGenHelper.Primitive(parameter.SourceColumnNullMapping), 
                        CodeGenHelper.Primitive(null),
                        CodeGenHelper.Str(string.Empty),
                        CodeGenHelper.Str(string.Empty),
                        CodeGenHelper.Str(string.Empty) 
                    }
                ); 
            } 
            else if (parameterType == SqlCeParameterType) {
                //\\ new SqlCeParameter("<parameterName>", <Type>, <Size>, <Direction>,<IsNullable>, 
                //\\        <precision>, <scale>, <sourceColumn>, <DataRowVersion>, Value>)
                newParam = CodeGenHelper.New(
                    CodeGenHelper.GlobalType(parameterType),
                    new CodeExpression[] { 
                        CodeGenHelper.Str(parameter.ParameterName),
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeEnumType), 
                            typeEnumValue
                        ), 
                        CodeGenHelper.Primitive(parameter.Size),
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ParameterDirection)),
                            parameter.Direction.ToString() 
                        ),
                        CodeGenHelper.Primitive(parameter.IsNullable), 
                        CodeGenHelper.Primitive(parameter.Precision), 
                        CodeGenHelper.Primitive(parameter.Scale),
                        CodeGenHelper.Str(parameter.SourceColumn), 
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataRowVersion)),
                            parameter.SourceVersion.ToString()
                        ), 
                        CodeGenHelper.Primitive(null)
                    } 
                ); 
            }
            else if (parameterType == typeof(System.Data.OracleClient.OracleParameter)) { 
                //\\ new OracleParameter("<parameterName>", <Type>, <Size>, <Direction>,
                //\\        <sourceColumn>, <DataRowVersion>, <SourceColumnNullMapping>, <Value> )
                newParam = CodeGenHelper.New(
                    CodeGenHelper.GlobalType(parameterType), 
                    new CodeExpression[] {
                        CodeGenHelper.Str(parameter.ParameterName), 
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeEnumType),
                            typeEnumValue 
                        ),
                        CodeGenHelper.Primitive(parameter.Size),
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ParameterDirection)), 
                            parameter.Direction.ToString()
                        ), 
                        CodeGenHelper.Str(parameter.SourceColumn), 
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataRowVersion)), 
                            parameter.SourceVersion.ToString()
                        ),
                        CodeGenHelper.Primitive(parameter.SourceColumnNullMapping),
                        CodeGenHelper.Primitive(null) 
                    }
                ); 
            } 
            else {
                //\\ new OleDb/OdbcParameter("<parameterName>", <Type>, <Size>, <Direction>, 
                //\\       (Byte) <precision>, (Byte) <scale>, <sourceColumn>, <DataRowVersion>, <SourceColumnNullMapping>, <Value> )
                newParam = CodeGenHelper.New(
                    CodeGenHelper.GlobalType(parameterType),
                    new CodeExpression[] { 
                        CodeGenHelper.Str(parameter.ParameterName),
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeEnumType), 
                            typeEnumValue
                        ), 
                        CodeGenHelper.Primitive(parameter.Size),
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ParameterDirection)),
                            parameter.Direction.ToString() 
                        ),
                        CodeGenHelper.Cast(CodeGenHelper.GlobalType(typeof(System.Byte)), CodeGenHelper.Primitive(parameter.Precision)), 
                        CodeGenHelper.Cast(CodeGenHelper.GlobalType(typeof(System.Byte)), CodeGenHelper.Primitive(parameter.Scale)), 
                        CodeGenHelper.Str(parameter.SourceColumn),
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataRowVersion)),
                            parameter.SourceVersion.ToString()
                        ),
                        CodeGenHelper.Primitive(parameter.SourceColumnNullMapping), 
                        CodeGenHelper.Primitive(null)
                    } 
                ); 
            }
 
            return newParam;
        }

        private static bool ParamVariableDeclared(IList statements) 
        {
            foreach (object statement in statements) 
            { 
                if (statement is System.CodeDom.CodeVariableDeclarationStatement)
                { 
                    CodeVariableDeclarationStatement declarationStatement = statement as System.CodeDom.CodeVariableDeclarationStatement;

                    if (declarationStatement.Name == "param")
                    { 
                        return true;
                    } 
                } 

            } 

            return false;

        } 

        private static CodeExpression BuildNewUnknownParameterStatements(DesignParameter parameter, Type parameterType, DbProviderFactory factory, IList statements, CodeExpression parameterVariable) { 
            // We're dealing with an unknown parameter type. All we assume is that the parameter implements 
            // the IDbDataParameter interface. Thus, we won't rely on a specific constructor, like we do for
            // the known parameter types. Instead we will create the parameter with the parameterless constructor 
            // and then set the properties exposed in IDbDataParameter one by one.

            if (!ParamVariableDeclared(statements)) {
                // add the variable declaration statement 
                statements.Add(
                    CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(parameterType), 
                        "param", 
                        CodeGenHelper.New(CodeGenHelper.GlobalType(parameterType), new CodeExpression[] { })
                    ) 
                );
                parameterVariable = CodeGenHelper.Variable("param");
            }
            else { 
                if (parameterVariable == null) {
                    parameterVariable = CodeGenHelper.Variable("param"); 
                } 

                // just assign the new parameter to the variable 
                statements.Add(
                    CodeGenHelper.Assign(
                        parameterVariable,
                        CodeGenHelper.New(CodeGenHelper.GlobalType(parameterType), new CodeExpression[] { }) 
                    )
                ); 
            } 

            IDbDataParameter testParameter = (IDbDataParameter) Activator.CreateInstance(parameterType); 

            //\\ param.ParameterName = <ParameterName>
            statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(parameterVariable, "ParameterName"),
                    CodeGenHelper.Str(parameter.ParameterName) 
                ) 
            );
 
            if(parameter.DbType != testParameter.DbType) {
                //\\ param.DbType = <DbType>
                statements.Add(
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(parameterVariable, "DbType"),
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DbType)), 
                            parameter.DbType.ToString()
                        ) 
                    )
                );
            }
 
            PropertyInfo pi = ProviderManager.GetProviderTypeProperty(factory);
            if( pi != null && parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) { 
                object value = null; 
                try {
                    value = Enum.Parse( pi.PropertyType, parameter.ProviderType ); 
                }
                catch { }

                if( value != null ) { 
                    //\\ param.<provider-specific type property> = <value>
                    statements.Add( 
                        CodeGenHelper.Assign( 
                            CodeGenHelper.Property(parameterVariable, pi.Name),
                            CodeGenHelper.Field( 
                                CodeGenHelper.TypeExpr(CodeGenHelper.GlobalType(pi.PropertyType)),
                                value.ToString()
                            )
                        ) 
                    );
                } 
            } 

            if(parameter.Size != testParameter.Size) { 
                //\\ param.Size = <Size>
                statements.Add(
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(parameterVariable, "Size"), 
                        CodeGenHelper.Primitive(parameter.Size)
                    ) 
                ); 
            }
 
            if(parameter.Direction != testParameter.Direction) {
                //\\ param.Direction = <Direction>
                statements.Add(
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(parameterVariable, "Direction"),
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ParameterDirection)), 
                            parameter.Direction.ToString()
                        ) 
                    )
                );
            }
 
            if(parameter.IsNullable != testParameter.IsNullable) {
                //\\ param.IsNullable = <IsNullable> 
                statements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(parameterVariable, "IsNullable"), 
                        CodeGenHelper.Primitive(parameter.IsNullable)
                    )
                );
            } 

//          IDbDataParameter.Precision/Scale are obsolete; no need to set them 
//            if(parameter.Precision != testParameter.Precision) { 
//                //\\ param.Precision = <Precision>
//                statements.Add( 
//                    CodeGenHelper.Assign(
//                        CodeGenHelper.Property(parameterVariable, "Precision"),
//                        CodeGenHelper.Primitive(parameter.Precision)
//                    ) 
//                );
//            } 
// 
//            if(parameter.Scale != testParameter.Scale) {
//                //\\ param.Scale = <Scale> 
//                statements.Add(
//                    CodeGenHelper.Assign(
//                        CodeGenHelper.Property(parameterVariable, "Scale"),
//                        CodeGenHelper.Primitive(parameter.Scale) 
//                    )
//                ); 
//            } 

 
            if(parameter.SourceColumn != testParameter.SourceColumn) {
                //\\ param.SourceColumn = <SourceColumn>
                statements.Add(
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(parameterVariable, "SourceColumn"),
                        CodeGenHelper.Str(parameter.SourceColumn) 
                    ) 
                );
            } 

            if(parameter.SourceVersion != testParameter.SourceVersion) {
                //\\ param.DataRowVersion = <DataRowVersion>
                statements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(parameterVariable, "SourceVersion"), 
                        CodeGenHelper.Field( 
                                CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataRowVersion)),
                                parameter.SourceVersion.ToString() 
                        )
                    )
                );
            } 

            if(testParameter is DbParameter) { 
                if(parameter.SourceColumnNullMapping != ((DbParameter)testParameter).SourceColumnNullMapping) { 
                    //\\ param.SourceColumnNullMapping = <SourceColumnNullMapping>
                    statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(parameterVariable, "SourceColumnNullMapping"),
                            CodeGenHelper.Primitive(parameter.SourceColumnNullMapping)
                        ) 
                    );
                } 
            } 

            return parameterVariable; 
        }


        protected Type GetParameterUrtType(DesignParameter parameter) { 
            if(this.ParameterOption == ParameterGenerationOption.SqlTypes) {
                return GetParameterSqlType(parameter); 
            } 
            else if(this.ParameterOption == ParameterGenerationOption.Objects) {
                return typeof(object); 
            }
            else if(this.ParameterOption == ParameterGenerationOption.ClrTypes) {
                Type parameterType = TypeConvertions.DbTypeToUrtType(parameter.DbType);
 
                if(parameterType == null) {
                    if(codeGenerator != null) { 
                        codeGenerator.ProblemList.Add( new DSGeneratorProblem(SR.GetString(SR.CG_UnableToConvertDbTypeToUrtType, this.MethodName, parameter.Name), ProblemSeverity.NonFatalError, this.methodSource) ); 
                    }
                    parameterType = typeof(object); 
                }

                return parameterType;
            } 
            else {
                throw new InternalException("Unknown parameter generation option."); 
            } 
        }
 
        private Type GetParameterSqlType(DesignParameter parameter) {
            // find design connection
            IDesignConnection designConnection = null; //codeGenerator.ConnectionHandler.Connections.Get(this.connectionName);
 
            if(StringUtil.EqualValue(designConnection.Provider, ManagedProviderNames.SqlClient)) {
                SqlDbType sqlDbType = SqlDbType.Char;    // make compiler happy 
                bool parsed = false; 

                if( parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) { 
                    try {
                        sqlDbType = (SqlDbType) Enum.Parse( typeof(SqlDbType), parameter.ProviderType );
                        parsed = true;
                    } 
                    catch { }
                } 
 
                if( !parsed ) {
                    // setting the DbType on a SqlParameter automatically sets the SqlDbType, which is what we need 
                    SqlParameter sqlParameter = new SqlParameter();
                    sqlParameter.DbType = parameter.DbType;
                    sqlDbType = sqlParameter.SqlDbType;
                } 

                Type parameterType = TypeConvertions.SqlDbTypeToSqlType(sqlDbType); 
 
                if(parameterType == null) {
                    if(codeGenerator != null) { 
                        codeGenerator.ProblemList.Add( new DSGeneratorProblem(SR.GetString(SR.CG_UnableToConvertSqlDbTypeToSqlType, this.MethodName, parameter.Name), ProblemSeverity.NonFatalError, this.methodSource) );
                    }
                    parameterType = typeof(object);
                } 

                return parameterType; 
            } 
            else {
                throw new InternalException("We should never attempt to generate SqlType-parameters for non-Sql providers."); 
            }
        }

        protected void AddThrowsClauseIfNeeded(CodeMemberMethod dbMethod) { 
            CodeTypeReference[] throwsArray = new CodeTypeReference[1];
            int paramCount = 0; 
            bool methodThrows = false; 

            if (this.activeCommand.Parameters != null) { 
                paramCount = this.activeCommand.Parameters.Count;
            }

            for (int i = 0; i < paramCount; i++) { 
                DesignParameter parameter = activeCommand.Parameters[i] as DesignParameter;
                if (parameter == null) { 
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter."); 
                }
 
                if (parameter.Direction == ParameterDirection.Output || parameter.Direction == ParameterDirection.InputOutput) {
                    // get parameter type
                    Type parameterType = GetParameterUrtType(parameter);
 
                    CodeExpression nullExpression = CodeGenHelper.GenerateNullExpression(parameterType);
                    if (nullExpression == null) { 
                        // in this case we can't assign null to the parameter and the method is gonna throw a StrongTypingException 
                        throwsArray[0] = CodeGenHelper.GlobalType(typeof(System.Data.StrongTypingException));
                        methodThrows = true; 
                    }
                }
            }
 
            if (!methodThrows) {
                int returnParamPos = GetReturnParameterPosition(activeCommand); 
                if (returnParamPos >= 0 && !this.getMethod && this.methodSource.QueryType != QueryType.Scalar) { 
                    Type returnType = GetParameterUrtType((DesignParameter)activeCommand.Parameters[returnParamPos]);
                    CodeExpression nullExpression = CodeGenHelper.GenerateNullExpression(returnType); 
                    if (nullExpression == null) {
                        // in this case we can't assign null to the parameter and the method is gonna throw a StrongTypingException
                        throwsArray[0] = CodeGenHelper.GlobalType(typeof(System.Data.StrongTypingException));
                        methodThrows = true; 
                    }
                } 
            } 

            if (methodThrows) { 
                dbMethod.UserData.Add("throwsCollection", new CodeTypeReferenceCollection(throwsArray));
            }
        }
 
        protected void AddSetParameterStatements(DesignParameter parameter, string parameterName, CodeExpression cmdExpression,
            int parameterIndex, IList statements) { 
 
            AddSetParameterStatements(parameter, parameterName, null, cmdExpression, parameterIndex, 0, statements);
        } 

        protected void AddSetParameterStatements(DesignParameter parameter, string parameterName, DesignParameter isNullParameter,
            CodeExpression cmdExpression, int parameterIndex, int isNullParameterIndex, IList statements) {
 
            Type parameterType = GetParameterUrtType(parameter);
            CodeCastExpression cce = new CodeCastExpression(parameterType, CodeGenHelper.Argument(parameterName)); 
            // J# specific UserData 
            cce.UserData.Add("CastIsBoxing", true);
 
            CodeCastExpression zero = null;
            CodeCastExpression one = null;
            if (this.codeGenerator != null && CodeGenHelper.IsGeneratingJSharpCode(this.codeGenerator.CodeProvider)) {
                // J# has casting rules that conflict with the ones of other languages, so we need to create a different expression 
                // this is unfortunate, as it forces us to special-case the CodeDom-tree for a specific language.
                zero = new CodeCastExpression(typeof(int), CodeGenHelper.Primitive(0)); 
                // J# specific UserData 
                zero.UserData.Add("CastIsBoxing", true);
                one = new CodeCastExpression(typeof(int), CodeGenHelper.Primitive(1)); 
                // J# specific UserData
                one.UserData.Add("CastIsBoxing", true);
            }
            else { 
                zero = new CodeCastExpression(typeof(object), CodeGenHelper.Primitive(0));
                one = new CodeCastExpression(typeof(object), CodeGenHelper.Primitive(1)); 
            } 

            CodeExpression paramValueExpression = CodeGenHelper.Property( 
                CodeGenHelper.Indexer(
                    CodeGenHelper.Property(
                        cmdExpression,
                        "Parameters" 
                    ),
                    CodeGenHelper.Primitive(parameterIndex) 
                ), 
                "Value"
            ); 

            CodeExpression isNullParamValueExpression = null;
            if (isNullParameter != null) {
                isNullParamValueExpression = CodeGenHelper.Property( 
                    CodeGenHelper.Indexer(
                        CodeGenHelper.Property( 
                            cmdExpression, 
                            "Parameters"
                        ), 
                        CodeGenHelper.Primitive(isNullParameterIndex)
                    ),
                    "Value"
                ); 
            }
 
            int statementCount = (isNullParameter == null) ? 1 : 2; 
            CodeStatement[] trueStatements = new CodeStatement[statementCount];
            CodeStatement[] falseStatements = new CodeStatement[statementCount]; 

            if (parameter.AllowDbNull && parameterType.IsValueType) {
                //\\ if(<paramName>.HasValue) {
                //\\    <Command>.Parameters[i].Value = (<paramType>)<paramName>.Value; 
                //\\    <Command>.Parameters[j].Value = 0;
                //\\ } 
                //\\ else { 
                //\\    <Command>.Parameters[i].Value = System.DBNull;
                //\\    <Command>.Parameters[j].Value = 1; 
                //\\ }
                cce = new CodeCastExpression(
                    parameterType,
                    CodeGenHelper.Property( 
                        CodeGenHelper.Argument(
                            parameterName 
                        ), 
                        "Value"
                    ) 
                );
                // J# specific UserData
                cce.UserData.Add("CastIsBoxing", true);
 
                trueStatements[0] = CodeGenHelper.Assign(paramValueExpression, cce);
                falseStatements[0] = CodeGenHelper.Assign( 
                    paramValueExpression, 
                    CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.DBNull)), "Value")
                ); 

                if (isNullParameter != null) {
                    trueStatements[1] = trueStatements[0];
                    falseStatements[1] = falseStatements[0]; 
                    trueStatements[0] = CodeGenHelper.Assign(isNullParamValueExpression, zero);
                    falseStatements[0] = CodeGenHelper.Assign(isNullParamValueExpression, one); 
                } 

                statements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.EQ(
                            CodeGenHelper.Property(CodeGenHelper.Argument(parameterName), "HasValue"),
                            CodeGenHelper.Primitive(true) 
                        ),
                        trueStatements, 
                        falseStatements 
                    )
                ); 
            }
            else if (parameter.AllowDbNull && !parameterType.IsValueType) {
                //\\ if(<paramName> == null) {
                //\\    <Command>.Parameters[i].Value = System.DBNull.Value; 
                //\\    <Command>.Parameters[j].Value = 1;
                //\\ } 
                //\\ else { 
                //\\    <Command>.Parameters[i].Value = (<paramType>)<paramName>;
                //\\    <Command>.Parameters[j].Value = 0; 
                //\\ }
                trueStatements[0] = CodeGenHelper.Assign(
                    paramValueExpression,
                    CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.DBNull)), "Value") 
                );
                falseStatements[0] = CodeGenHelper.Assign(paramValueExpression, cce); 
 
                if (isNullParameter != null) {
                    trueStatements[1] = trueStatements[0]; 
                    falseStatements[1] = falseStatements[0];
                    trueStatements[0] = CodeGenHelper.Assign(isNullParamValueExpression, one);
                    falseStatements[0] = CodeGenHelper.Assign(isNullParamValueExpression, zero);
                } 

                statements.Add( 
                    CodeGenHelper.If( 
                        CodeGenHelper.IdEQ(
                            CodeGenHelper.Argument(parameterName), 
                            CodeGenHelper.Primitive(null)
                        ),
                        trueStatements,
                        falseStatements 
                    )
                ); 
            } 
            else if (!parameter.AllowDbNull && !parameterType.IsValueType) {
                //\\ if(<paramName> == null) { 
                //\\    throw new ArgumentNullException("<paramName>");
                //\\ }
                //\\ else {
                //\\    <Command>.Parameters[i].Value = (<paramType>)<paramName>; 
                //\\    <Command>.Parameters[i].Value = 0;
                //\\ } 
                CodeStatement[] trueStatement = new CodeStatement[1]; 
                trueStatement[0] = CodeGenHelper.Throw(CodeGenHelper.GlobalType(typeof(System.ArgumentNullException)), parameterName);
                falseStatements[0] = CodeGenHelper.Assign(paramValueExpression, cce); 

                if (isNullParameter != null) {
                    falseStatements[1] = falseStatements[0];
                    falseStatements[0] = CodeGenHelper.Assign(isNullParamValueExpression, zero); 
                }
 
                statements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdEQ( 
                            CodeGenHelper.Argument(parameterName),
                            CodeGenHelper.Primitive(null)
                        ),
                        trueStatement, 
                        falseStatements
                    ) 
                ); 
            }
            else if (!parameter.AllowDbNull && parameterType.IsValueType) { 
                //\\ <Command>.Parameters[j].Value = 0;
                //\\ <Command>.Parameters[i].Value = (<paramType>)<paramName>;
                if (isNullParameter != null) {
                    statements.Add(CodeGenHelper.Assign(isNullParamValueExpression, zero)); 
                }
                statements.Add(CodeGenHelper.Assign(paramValueExpression, cce)); 
            } 
        }
 
        protected bool AddSetReturnParamValuesStatements(IList statements, CodeExpression commandExpression) {
            int paramCount = 0;
            if (this.activeCommand.Parameters != null) {
                paramCount = this.activeCommand.Parameters.Count; 
            }
 
            for (int i = 0; i < paramCount; i++) { 
                DesignParameter parameter = activeCommand.Parameters[i] as DesignParameter;
                if (parameter == null) { 
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter.");
                }

                if (parameter.Direction == ParameterDirection.Output || parameter.Direction == ParameterDirection.InputOutput) { 
                    // get parameter type
                    Type parameterType = GetParameterUrtType(parameter); 
                    string parameterName = nameHandler.GetNameFromList(parameter.ParameterName); 

                    // create parameter expression 
                    //\\ <commandExpression>.Parameters[i].Value
                    CodeExpression outputParamExpression = CodeGenHelper.Property(
                        CodeGenHelper.Indexer(
                            CodeGenHelper.Property( 
                                commandExpression,
                                "Parameters" 
                            ), 
                            CodeGenHelper.Primitive(i)
                        ), 
                        "Value"
                    );

                    // if(command.Parameters[i].Value.GetType() == typeof(System.DBNull)) 
                    CodeExpression isEqualDbNullCondition = CodeGenHelper.GenerateDbNullCheck(outputParamExpression);
 
                    CodeExpression nullExpression = CodeGenHelper.GenerateNullExpression(parameterType); 
                    CodeStatement trueStatement = null;
 
                    if (nullExpression == null) {
                        if (parameter.AllowDbNull && parameterType.IsValueType) {
                            //\\ <parameter> = new System.Nullable<parameterType>();
                            trueStatement = CodeGenHelper.Assign( 
                                CodeGenHelper.Argument(parameterName),
                                CodeGenHelper.New( 
                                    CodeGenHelper.NullableType(parameterType), 
                                    new CodeExpression[] { }
                                ) 
                            );
                        }
                        else if (parameter.AllowDbNull && !parameterType.IsValueType) {
                            //\\ <parameter> = null; 
                            trueStatement = CodeGenHelper.Assign(
                                CodeGenHelper.Argument(parameterName), 
                                CodeGenHelper.Primitive(null) 
                            );
                        } 
                        else {
                            // in this case we can't assign null to the parameter
                            //\\ throw new StrongTypingException("StrongTyping_CannotAccessDBNull");
                            trueStatement = CodeGenHelper.Throw(CodeGenHelper.GlobalType(typeof(System.Data.StrongTypingException)), SR.GetString(SR.CG_ParameterIsDBNull, parameterName), CodeGenHelper.Primitive(null)); 
                        }
                    } 
                    else { 
                        //\\ <parameter> = <nullExpression>
                        trueStatement = CodeGenHelper.Assign( 
                            CodeGenHelper.Argument(nameHandler.GetNameFromList(parameter.ParameterName)),
                            nullExpression
                        );
                    } 

                    CodeStatement falseStatement = null; 
                    if (parameter.AllowDbNull && parameterType.IsValueType) { 
                        //\\ <parameter> = new System.Nullable<parameterType>((<parameterType>) command.Parameter[i].Value);
                        falseStatement = CodeGenHelper.Assign( 
                            CodeGenHelper.Argument(parameterName),
                            CodeGenHelper.New(
                                CodeGenHelper.NullableType(parameterType),
                                new CodeExpression[] { CodeGenHelper.Cast(CodeGenHelper.GlobalType(parameterType), outputParamExpression) } 
                            )
                        ); 
                    } 
                    else {
                        //\\ <parameter> = (<parameterType>) command.Parameters[i].Value; 
                        falseStatement = CodeGenHelper.Assign(
                            CodeGenHelper.Argument(parameterName),
                            CodeGenHelper.Cast(CodeGenHelper.GlobalType(parameterType), outputParamExpression)
                        ); 
                    }
 
                    statements.Add( 
                        CodeGenHelper.If(
                            isEqualDbNullCondition, 
                            trueStatement,
                            falseStatement
                        )
                    ); 
                }
            } 
 
            return true;
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
    using System.Data.Common; 
    using System.Data.SqlClient; 
    using System.Data.SqlTypes;
    using System.Data.Odbc; 
    using System.Data.OleDb;
    using System.Data.OracleClient;
    using System.Design;
    using System.Reflection; 

 
    public enum ParameterGenerationOption                                                                                                            { 
        ClrTypes    = 0,
        SqlTypes    = 1, 
        Objects     = 2
    }

    internal abstract class QueryGeneratorBase { 
        protected TypedDataSourceCodeGenerator codeGenerator = null;
        protected GenericNameHandler nameHandler = null; 
 
        protected static string returnVariableName = "returnValue";
        protected static string commandVariableName = "command"; 
        protected static string startRecordParameterName = "startRecord";
        protected static string maxRecordsParameterName = "maxRecords";

 
        // generation settings
        protected DbProviderFactory providerFactory = null; 
        protected DbSource methodSource = null; 
        protected DbSourceCommand activeCommand = null;
        protected string methodName = null; 
        protected MemberAttributes methodAttributes;
        protected Type containerParamType = typeof(System.Data.DataSet);
        protected string containerParamTypeName = null;
        protected string containerParamName = "dataSet"; 
        protected ParameterGenerationOption parameterOption = ParameterGenerationOption.ClrTypes;
        protected Type returnType = typeof(void); 
        protected int commandIndex = 0; 
        protected DesignTable designTable = null;
        protected bool getMethod = false; 
        protected bool pagingMethod = false;
        protected bool declarationOnly = false;
        protected MethodTypeEnum methodType = MethodTypeEnum.ColumnParameters;
        protected string updateParameterName = null; 
        protected CodeTypeReference updateParameterTypeReference = CodeGenHelper.GlobalType(typeof(System.Data.DataSet));
        protected string updateParameterTypeName = null; 
        protected CodeDomProvider codeProvider = null; 
        protected string updateCommandName = null;
        protected bool isFunctionsDataComponent = false; 

        //
        // SqlCeParameter is in DotNet Framework 3.5, while our code generator is in 2.0
        // So we need to workaround to use runtime type. 
        //
        private static Type sqlCeParameterType; 
        private const string SqlCeParameterTypeName = @"System.Data.SqlServerCe.SqlCeParameter, System.Data.SqlServerCe, Version=3.5.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91"; 
        private static object sqlCeParameterInstance;
        private static PropertyDescriptor sqlCeParaDbTypeDescriptor; 

        internal QueryGeneratorBase(TypedDataSourceCodeGenerator codeGenerator) {
            this.codeGenerator = codeGenerator;
        } 

        internal DbProviderFactory ProviderFactory { 
            get { 
                return this.providerFactory;
            } 
            set {
                this.providerFactory = value;
            }
        } 

        internal DbSource MethodSource { 
            get { 
                return this.methodSource;
            } 
            set {
                this.methodSource = value;
            }
        } 

        internal DbSourceCommand ActiveCommand { 
            get { 
                return this.activeCommand;
            } 
            set {
                this.activeCommand = value;
            }
        } 

        internal string MethodName { 
            get { 
                return this.methodName;
            } 
            set {
                this.methodName = value;
            }
        } 

        internal ParameterGenerationOption ParameterOption { 
            get { 
                return this.parameterOption;
            } 
            set {
                this.parameterOption = value;
            }
        } 

        internal Type ContainerParameterType { 
            get { 
                return this.containerParamType;
            } 
            set {
                this.containerParamType = value;
            }
        } 

        internal string ContainerParameterTypeName { 
            get { 
                return this.containerParamTypeName;
            } 
            set {
                this.containerParamTypeName = value;
            }
        } 

        internal string ContainerParameterName { 
            get { 
                return this.containerParamName;
            } 
            set {
                this.containerParamName = value;
            }
        } 

        internal int CommandIndex { 
            get { 
                return this.commandIndex;
            } 
            set {
                this.commandIndex = value;
            }
        } 

        internal DesignTable DesignTable { 
            get { 
                return this.designTable;
            } 
            set {
                this.designTable = value;
            }
        } 

        //internal bool GenerateOverloads { 
        //    get { 
        //        if (methodSource != null) {
        //            DbSourceCommand activeCmd = methodSource.GetActiveCommand(); 
        //            if (activeCmd != null && activeCmd.Parameters != null && activeCmd.Parameters.Count > 0
        //                && !(activeCmd.Parameters.Count == 1 && activeCmd.Parameters[0].Direction == ParameterDirection.ReturnValue)) {
        //                    // we know we have parameters, now let's check if they're all of type object, or if we have strongly
        //                    // typed ones 
        //                    bool nonObjectParameter = false;
        //                    foreach (DesignParameter parameter in this.activeCommand.Parameters) { 
        //                        if (parameter.Direction == ParameterDirection.ReturnValue) { 
        //                            // skip over return parameter
        //                            continue; 
        //                        }

        //                        // get the type of the parameter
        //                        Type parameterType = this.GetParameterUrtType(parameter); 
        //                        if (parameterType != typeof(object)) {
        //                            nonObjectParameter = true; 
        //                            break; 
        //                        }
        //                    } 

        //                    return nonObjectParameter;
        //            }
        //        } 

        //        return false; 
        //    } 
        //}
 
        internal bool GenerateGetMethod {
            get {
                return this.getMethod;
            } 
            set {
                this.getMethod = value; 
            } 
        }
 
        internal bool GeneratePagingMethod {
            get {
                return this.pagingMethod;
            } 
            set {
                this.pagingMethod = value; 
            } 
        }
 
        internal bool DeclarationOnly {
            get {
                return this.declarationOnly;
            } 
            set {
                this.declarationOnly = value; 
            } 
        }
 
        internal MethodTypeEnum MethodType {
            get {
                return this.methodType;
            } 
            set {
                this.methodType = value; 
            } 
        }
 
        internal string UpdateParameterName {
            get {
                return this.updateParameterName;
            } 
            set {
                this.updateParameterName = value; 
            } 
        }
 
        internal string UpdateParameterTypeName {
            get {
                return this.updateParameterTypeName;
            } 
            set {
                this.updateParameterTypeName = value; 
            } 
        }
 
        internal CodeTypeReference UpdateParameterTypeReference {
            get {
                return this.updateParameterTypeReference;
            } 
            set {
                this.updateParameterTypeReference = value; 
            } 
        }
 
        internal CodeDomProvider CodeProvider {
            get {
                return this.codeProvider;
            } 
            set {
                this.codeProvider = value; 
            } 
        }
 
        internal string UpdateCommandName {
            get {
                return this.updateCommandName;
            } 
            set {
                this.updateCommandName = value; 
            } 
        }
 
        internal bool IsFunctionsDataComponent {
            get {
                return this.isFunctionsDataComponent;
            } 
            set {
                this.isFunctionsDataComponent = value; 
            } 

        } 

        internal static Type SqlCeParameterType {
            get {
                if (sqlCeParameterType == null) { 
                    try {
                        sqlCeParameterType = Type.GetType(SqlCeParameterTypeName); 
                    } 
                    catch (System.IO.FileLoadException) {
                    } 
                }
                return sqlCeParameterType;
            }
        } 

        internal static object SqlCeParameterInstance { 
            get { 
                if (sqlCeParameterInstance == null && SqlCeParameterType != null) {
                    sqlCeParameterInstance = Activator.CreateInstance(SqlCeParameterType); 
                }
                return sqlCeParameterInstance;
            }
        } 

        internal static PropertyDescriptor SqlCeParaDbTypeDescriptor { 
            get { 
                if (sqlCeParaDbTypeDescriptor == null && SqlCeParameterType != null) {
                    sqlCeParaDbTypeDescriptor = TypeDescriptor.GetProperties(SqlCeParameterType)["DbType"]; 
                }
                return sqlCeParaDbTypeDescriptor;
            }
        } 

        internal static CodeStatement SetCommandTextStatement(CodeExpression commandExpression, string commandText) { 
            //\\ <command>.CommandText = "<commandText>"; 
            CodeStatement statement = CodeGenHelper.Assign(
                CodeGenHelper.Property( 
                    commandExpression,
                    "CommandText"
                ),
                CodeGenHelper.Str(commandText) 
            );
 
            return statement; 
        }
 
        internal static CodeStatement SetCommandTypeStatement(CodeExpression commandExpression, CommandType commandType) {
            //\\ <command>.CommandType = <CommandType>
            CodeStatement statement = CodeGenHelper.Assign(
                CodeGenHelper.Property( 
                    commandExpression,
                    "CommandType" 
                ), 
                CodeGenHelper.Field(
                    CodeGenHelper.GlobalTypeExpr(typeof(System.Data.CommandType)), 
                    ((CommandType) commandType).ToString()
                )
            );
 
            return statement;
        } 
 

        internal abstract CodeMemberMethod Generate(); 


        protected DesignParameter GetReturnParameter(DbSourceCommand command) {
            foreach (DesignParameter parameter in command.Parameters) { 
                if (parameter.Direction == ParameterDirection.ReturnValue) {
                    return parameter; 
                } 
            }
 
            return null;
        }

        protected int GetReturnParameterPosition(DbSourceCommand command) { 
            if(command == null || command.Parameters == null) {
                return -1; 
            } 

            for(int i = 0; i < command.Parameters.Count; i++) { 
                if( ((DesignParameter) command.Parameters[i]).Direction == ParameterDirection.ReturnValue ) {
                    return i;
                }
            } 

            return -1; 
        } 

 

        internal static CodeExpression AddNewParameterStatements(DesignParameter parameter, Type parameterType, DbProviderFactory factory, IList statements, CodeExpression parameterVariable) {
            if(parameterType == typeof(System.Data.SqlClient.SqlParameter)) {
                return BuildNewSqlParameterStatement(parameter); 
            }
            else if(parameterType == typeof(System.Data.OleDb.OleDbParameter)) { 
                return BuildNewOleDbParameterStatement(parameter); 
            }
            else if(parameterType == typeof(System.Data.Odbc.OdbcParameter)) { 
                return BuildNewOdbcParameterStatement(parameter);
            }
            else if(parameterType == typeof(System.Data.OracleClient.OracleParameter)) {
                return BuildNewOracleParameterStatement(parameter); 
            }
            else if (parameterType == SqlCeParameterType && StringUtil.NotEmptyAfterTrim(parameter.ProviderType)) { 
                // DDB 111450 -- Only Use BuildNewSqlCeParameterStatement if we know parameter.ProviderType is defined. 
                return BuildNewSqlCeParameterStatement(parameter);
            } 
            else {
                // for extensibility, we also deal with unknown providers, and thus unknown connection/command/parameter types
                return BuildNewUnknownParameterStatements(parameter, parameterType, factory, statements, parameterVariable);
            } 
        }
 
        private static CodeExpression BuildNewSqlParameterStatement(DesignParameter parameter) { 
            SqlParameter sqlParameter = new SqlParameter();
            SqlDbType sqlDbType = SqlDbType.Char;   // make compiler happy 
            bool parsed = false;

            if( parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) {
                try { 
                    sqlDbType = (SqlDbType) Enum.Parse( typeof(SqlDbType), parameter.ProviderType );
                    parsed = true; 
                } 
                catch { }
            } 

            if( !parsed ) {
                // setting the DbType on a SqlParameter automatically sets the SqlDbType, which is what we need
                sqlParameter.DbType = parameter.DbType; 
                sqlDbType = sqlParameter.SqlDbType;
            } 
 
            return NewParameter(parameter, typeof(System.Data.SqlClient.SqlParameter), typeof(System.Data.SqlDbType), sqlDbType.ToString());
        } 

        /// <summary>
        /// </summary>
        /// <param name="parameter"></param> 
        /// <returns></returns>
        private static CodeExpression BuildNewSqlCeParameterStatement(DesignParameter parameter) { 
            SqlDbType sqlDbType = SqlDbType.Char;   // make compiler happy 
            bool parsed = false;
 
            if (parameter.ProviderType != null && parameter.ProviderType.Length > 0) {
                try {
                    sqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), parameter.ProviderType);
                    parsed = true; 
                }
                catch { } 
            } 

            if (!parsed){ 
                object sqlParaInstance = SqlCeParameterInstance;
                if (sqlParaInstance != null) {
                    PropertyDescriptor dbTypePropDesc = SqlCeParaDbTypeDescriptor;
                    if (dbTypePropDesc != null) { 
                        // setting the DbType on a SqlParameter automatically sets the SqlDbType, which is what we need
                        dbTypePropDesc.SetValue(sqlParaInstance, parameter.DbType); 
                        sqlDbType = (SqlDbType)dbTypePropDesc.GetValue(sqlParaInstance); 
                    }
                } 
            }

            return NewParameter(parameter, SqlCeParameterType, typeof(System.Data.SqlDbType), sqlDbType.ToString());
        } 

        private static CodeExpression BuildNewOleDbParameterStatement(DesignParameter parameter) { 
            OleDbParameter oleDbParameter = new OleDbParameter(); 
            OleDbType oleDbType = OleDbType.Char; // make compiler happy
            bool parsed = false; 

            if( parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) {
                try {
                    oleDbType = (OleDbType) Enum.Parse( typeof(OleDbType), parameter.ProviderType ); 
                    parsed = true;
                } 
                catch { } 
            }
 
            if( !parsed ) {
                // setting the DbType on an OleDbParameter automatically sets the OleDbType, which is what we need
                oleDbParameter.DbType = parameter.DbType;
                oleDbType = oleDbParameter.OleDbType; 
            }
 
            return NewParameter(parameter, typeof(System.Data.OleDb.OleDbParameter), typeof(System.Data.OleDb.OleDbType), oleDbType.ToString()); 
        }
 
        private static CodeExpression BuildNewOdbcParameterStatement(DesignParameter parameter) {
            OdbcParameter odbcParameter = new OdbcParameter();
            OdbcType odbcType = OdbcType.Char;  // make compiler happy
            bool parsed = false; 

            if( parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) { 
                try { 
                    odbcType = (OdbcType) Enum.Parse( typeof(OdbcType), parameter.ProviderType );
                    parsed = true; 
                }
                catch { }
            }
 
            if( !parsed ) {
                // setting the DbType on an OdbcParameter automatically sets the OdbcDbType, which is what we need 
                odbcParameter.DbType = parameter.DbType; 
                odbcType = odbcParameter.OdbcType;
            } 

            return NewParameter(parameter, typeof(System.Data.Odbc.OdbcParameter), typeof(System.Data.Odbc.OdbcType), odbcType.ToString());
        }
 
        private static CodeExpression BuildNewOracleParameterStatement(DesignParameter parameter) {
            OracleParameter oracleParameter = new OracleParameter(); 
            OracleType oracleType = OracleType.Char;    // make compiler happy 
            bool parsed = false;
 
            if( parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) {
                try {
                    oracleType = (OracleType) Enum.Parse( typeof(OracleType), parameter.ProviderType );
                    parsed = true; 
                }
                catch { } 
            } 

            if( !parsed ) { 
                // setting the DbType on an OracleParameter automatically sets the OracleDbType, which is what we need
                oracleParameter.DbType = parameter.DbType;
                oracleType = oracleParameter.OracleType;
            } 

 
            return NewParameter(parameter, typeof(System.Data.OracleClient.OracleParameter), typeof(System.Data.OracleClient.OracleType), oracleType.ToString()); 
        }
 
        private static CodeExpression NewParameter(DesignParameter parameter, Type parameterType, Type typeEnumType, string typeEnumValue) {
            CodeExpression newParam = null;

            if(parameterType == typeof(System.Data.SqlClient.SqlParameter)) { 
                //\\ new SqlParameter("<parameterName>", <Type>, <Size>, <Direction>,
                //\\        <precision>, <scale>, <sourceColumn>, <DataRowVersion>, <SourceColumnNullMapping>, <Value>, "", "", "" ) 
                newParam = CodeGenHelper.New( 
                    CodeGenHelper.GlobalType(parameterType),
                    new CodeExpression[] { 
                        CodeGenHelper.Str(parameter.ParameterName),
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeEnumType),
                            typeEnumValue 
                        ),
                        CodeGenHelper.Primitive(parameter.Size), 
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ParameterDirection)),
                            parameter.Direction.ToString() 
                        ),
                        CodeGenHelper.Primitive(parameter.Precision),
                        CodeGenHelper.Primitive(parameter.Scale),
                        CodeGenHelper.Str(parameter.SourceColumn), 
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataRowVersion)), 
                            parameter.SourceVersion.ToString() 
                        ),
                        CodeGenHelper.Primitive(parameter.SourceColumnNullMapping), 
                        CodeGenHelper.Primitive(null),
                        CodeGenHelper.Str(string.Empty),
                        CodeGenHelper.Str(string.Empty),
                        CodeGenHelper.Str(string.Empty) 
                    }
                ); 
            } 
            else if (parameterType == SqlCeParameterType) {
                //\\ new SqlCeParameter("<parameterName>", <Type>, <Size>, <Direction>,<IsNullable>, 
                //\\        <precision>, <scale>, <sourceColumn>, <DataRowVersion>, Value>)
                newParam = CodeGenHelper.New(
                    CodeGenHelper.GlobalType(parameterType),
                    new CodeExpression[] { 
                        CodeGenHelper.Str(parameter.ParameterName),
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeEnumType), 
                            typeEnumValue
                        ), 
                        CodeGenHelper.Primitive(parameter.Size),
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ParameterDirection)),
                            parameter.Direction.ToString() 
                        ),
                        CodeGenHelper.Primitive(parameter.IsNullable), 
                        CodeGenHelper.Primitive(parameter.Precision), 
                        CodeGenHelper.Primitive(parameter.Scale),
                        CodeGenHelper.Str(parameter.SourceColumn), 
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataRowVersion)),
                            parameter.SourceVersion.ToString()
                        ), 
                        CodeGenHelper.Primitive(null)
                    } 
                ); 
            }
            else if (parameterType == typeof(System.Data.OracleClient.OracleParameter)) { 
                //\\ new OracleParameter("<parameterName>", <Type>, <Size>, <Direction>,
                //\\        <sourceColumn>, <DataRowVersion>, <SourceColumnNullMapping>, <Value> )
                newParam = CodeGenHelper.New(
                    CodeGenHelper.GlobalType(parameterType), 
                    new CodeExpression[] {
                        CodeGenHelper.Str(parameter.ParameterName), 
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeEnumType),
                            typeEnumValue 
                        ),
                        CodeGenHelper.Primitive(parameter.Size),
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ParameterDirection)), 
                            parameter.Direction.ToString()
                        ), 
                        CodeGenHelper.Str(parameter.SourceColumn), 
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataRowVersion)), 
                            parameter.SourceVersion.ToString()
                        ),
                        CodeGenHelper.Primitive(parameter.SourceColumnNullMapping),
                        CodeGenHelper.Primitive(null) 
                    }
                ); 
            } 
            else {
                //\\ new OleDb/OdbcParameter("<parameterName>", <Type>, <Size>, <Direction>, 
                //\\       (Byte) <precision>, (Byte) <scale>, <sourceColumn>, <DataRowVersion>, <SourceColumnNullMapping>, <Value> )
                newParam = CodeGenHelper.New(
                    CodeGenHelper.GlobalType(parameterType),
                    new CodeExpression[] { 
                        CodeGenHelper.Str(parameter.ParameterName),
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeEnumType), 
                            typeEnumValue
                        ), 
                        CodeGenHelper.Primitive(parameter.Size),
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ParameterDirection)),
                            parameter.Direction.ToString() 
                        ),
                        CodeGenHelper.Cast(CodeGenHelper.GlobalType(typeof(System.Byte)), CodeGenHelper.Primitive(parameter.Precision)), 
                        CodeGenHelper.Cast(CodeGenHelper.GlobalType(typeof(System.Byte)), CodeGenHelper.Primitive(parameter.Scale)), 
                        CodeGenHelper.Str(parameter.SourceColumn),
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataRowVersion)),
                            parameter.SourceVersion.ToString()
                        ),
                        CodeGenHelper.Primitive(parameter.SourceColumnNullMapping), 
                        CodeGenHelper.Primitive(null)
                    } 
                ); 
            }
 
            return newParam;
        }

        private static bool ParamVariableDeclared(IList statements) 
        {
            foreach (object statement in statements) 
            { 
                if (statement is System.CodeDom.CodeVariableDeclarationStatement)
                { 
                    CodeVariableDeclarationStatement declarationStatement = statement as System.CodeDom.CodeVariableDeclarationStatement;

                    if (declarationStatement.Name == "param")
                    { 
                        return true;
                    } 
                } 

            } 

            return false;

        } 

        private static CodeExpression BuildNewUnknownParameterStatements(DesignParameter parameter, Type parameterType, DbProviderFactory factory, IList statements, CodeExpression parameterVariable) { 
            // We're dealing with an unknown parameter type. All we assume is that the parameter implements 
            // the IDbDataParameter interface. Thus, we won't rely on a specific constructor, like we do for
            // the known parameter types. Instead we will create the parameter with the parameterless constructor 
            // and then set the properties exposed in IDbDataParameter one by one.

            if (!ParamVariableDeclared(statements)) {
                // add the variable declaration statement 
                statements.Add(
                    CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(parameterType), 
                        "param", 
                        CodeGenHelper.New(CodeGenHelper.GlobalType(parameterType), new CodeExpression[] { })
                    ) 
                );
                parameterVariable = CodeGenHelper.Variable("param");
            }
            else { 
                if (parameterVariable == null) {
                    parameterVariable = CodeGenHelper.Variable("param"); 
                } 

                // just assign the new parameter to the variable 
                statements.Add(
                    CodeGenHelper.Assign(
                        parameterVariable,
                        CodeGenHelper.New(CodeGenHelper.GlobalType(parameterType), new CodeExpression[] { }) 
                    )
                ); 
            } 

            IDbDataParameter testParameter = (IDbDataParameter) Activator.CreateInstance(parameterType); 

            //\\ param.ParameterName = <ParameterName>
            statements.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(parameterVariable, "ParameterName"),
                    CodeGenHelper.Str(parameter.ParameterName) 
                ) 
            );
 
            if(parameter.DbType != testParameter.DbType) {
                //\\ param.DbType = <DbType>
                statements.Add(
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(parameterVariable, "DbType"),
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DbType)), 
                            parameter.DbType.ToString()
                        ) 
                    )
                );
            }
 
            PropertyInfo pi = ProviderManager.GetProviderTypeProperty(factory);
            if( pi != null && parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) { 
                object value = null; 
                try {
                    value = Enum.Parse( pi.PropertyType, parameter.ProviderType ); 
                }
                catch { }

                if( value != null ) { 
                    //\\ param.<provider-specific type property> = <value>
                    statements.Add( 
                        CodeGenHelper.Assign( 
                            CodeGenHelper.Property(parameterVariable, pi.Name),
                            CodeGenHelper.Field( 
                                CodeGenHelper.TypeExpr(CodeGenHelper.GlobalType(pi.PropertyType)),
                                value.ToString()
                            )
                        ) 
                    );
                } 
            } 

            if(parameter.Size != testParameter.Size) { 
                //\\ param.Size = <Size>
                statements.Add(
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(parameterVariable, "Size"), 
                        CodeGenHelper.Primitive(parameter.Size)
                    ) 
                ); 
            }
 
            if(parameter.Direction != testParameter.Direction) {
                //\\ param.Direction = <Direction>
                statements.Add(
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(parameterVariable, "Direction"),
                        CodeGenHelper.Field( 
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ParameterDirection)), 
                            parameter.Direction.ToString()
                        ) 
                    )
                );
            }
 
            if(parameter.IsNullable != testParameter.IsNullable) {
                //\\ param.IsNullable = <IsNullable> 
                statements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(parameterVariable, "IsNullable"), 
                        CodeGenHelper.Primitive(parameter.IsNullable)
                    )
                );
            } 

//          IDbDataParameter.Precision/Scale are obsolete; no need to set them 
//            if(parameter.Precision != testParameter.Precision) { 
//                //\\ param.Precision = <Precision>
//                statements.Add( 
//                    CodeGenHelper.Assign(
//                        CodeGenHelper.Property(parameterVariable, "Precision"),
//                        CodeGenHelper.Primitive(parameter.Precision)
//                    ) 
//                );
//            } 
// 
//            if(parameter.Scale != testParameter.Scale) {
//                //\\ param.Scale = <Scale> 
//                statements.Add(
//                    CodeGenHelper.Assign(
//                        CodeGenHelper.Property(parameterVariable, "Scale"),
//                        CodeGenHelper.Primitive(parameter.Scale) 
//                    )
//                ); 
//            } 

 
            if(parameter.SourceColumn != testParameter.SourceColumn) {
                //\\ param.SourceColumn = <SourceColumn>
                statements.Add(
                    CodeGenHelper.Assign( 
                        CodeGenHelper.Property(parameterVariable, "SourceColumn"),
                        CodeGenHelper.Str(parameter.SourceColumn) 
                    ) 
                );
            } 

            if(parameter.SourceVersion != testParameter.SourceVersion) {
                //\\ param.DataRowVersion = <DataRowVersion>
                statements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(parameterVariable, "SourceVersion"), 
                        CodeGenHelper.Field( 
                                CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataRowVersion)),
                                parameter.SourceVersion.ToString() 
                        )
                    )
                );
            } 

            if(testParameter is DbParameter) { 
                if(parameter.SourceColumnNullMapping != ((DbParameter)testParameter).SourceColumnNullMapping) { 
                    //\\ param.SourceColumnNullMapping = <SourceColumnNullMapping>
                    statements.Add( 
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(parameterVariable, "SourceColumnNullMapping"),
                            CodeGenHelper.Primitive(parameter.SourceColumnNullMapping)
                        ) 
                    );
                } 
            } 

            return parameterVariable; 
        }


        protected Type GetParameterUrtType(DesignParameter parameter) { 
            if(this.ParameterOption == ParameterGenerationOption.SqlTypes) {
                return GetParameterSqlType(parameter); 
            } 
            else if(this.ParameterOption == ParameterGenerationOption.Objects) {
                return typeof(object); 
            }
            else if(this.ParameterOption == ParameterGenerationOption.ClrTypes) {
                Type parameterType = TypeConvertions.DbTypeToUrtType(parameter.DbType);
 
                if(parameterType == null) {
                    if(codeGenerator != null) { 
                        codeGenerator.ProblemList.Add( new DSGeneratorProblem(SR.GetString(SR.CG_UnableToConvertDbTypeToUrtType, this.MethodName, parameter.Name), ProblemSeverity.NonFatalError, this.methodSource) ); 
                    }
                    parameterType = typeof(object); 
                }

                return parameterType;
            } 
            else {
                throw new InternalException("Unknown parameter generation option."); 
            } 
        }
 
        private Type GetParameterSqlType(DesignParameter parameter) {
            // find design connection
            IDesignConnection designConnection = null; //codeGenerator.ConnectionHandler.Connections.Get(this.connectionName);
 
            if(StringUtil.EqualValue(designConnection.Provider, ManagedProviderNames.SqlClient)) {
                SqlDbType sqlDbType = SqlDbType.Char;    // make compiler happy 
                bool parsed = false; 

                if( parameter.ProviderType != null && parameter.ProviderType.Length > 0 ) { 
                    try {
                        sqlDbType = (SqlDbType) Enum.Parse( typeof(SqlDbType), parameter.ProviderType );
                        parsed = true;
                    } 
                    catch { }
                } 
 
                if( !parsed ) {
                    // setting the DbType on a SqlParameter automatically sets the SqlDbType, which is what we need 
                    SqlParameter sqlParameter = new SqlParameter();
                    sqlParameter.DbType = parameter.DbType;
                    sqlDbType = sqlParameter.SqlDbType;
                } 

                Type parameterType = TypeConvertions.SqlDbTypeToSqlType(sqlDbType); 
 
                if(parameterType == null) {
                    if(codeGenerator != null) { 
                        codeGenerator.ProblemList.Add( new DSGeneratorProblem(SR.GetString(SR.CG_UnableToConvertSqlDbTypeToSqlType, this.MethodName, parameter.Name), ProblemSeverity.NonFatalError, this.methodSource) );
                    }
                    parameterType = typeof(object);
                } 

                return parameterType; 
            } 
            else {
                throw new InternalException("We should never attempt to generate SqlType-parameters for non-Sql providers."); 
            }
        }

        protected void AddThrowsClauseIfNeeded(CodeMemberMethod dbMethod) { 
            CodeTypeReference[] throwsArray = new CodeTypeReference[1];
            int paramCount = 0; 
            bool methodThrows = false; 

            if (this.activeCommand.Parameters != null) { 
                paramCount = this.activeCommand.Parameters.Count;
            }

            for (int i = 0; i < paramCount; i++) { 
                DesignParameter parameter = activeCommand.Parameters[i] as DesignParameter;
                if (parameter == null) { 
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter."); 
                }
 
                if (parameter.Direction == ParameterDirection.Output || parameter.Direction == ParameterDirection.InputOutput) {
                    // get parameter type
                    Type parameterType = GetParameterUrtType(parameter);
 
                    CodeExpression nullExpression = CodeGenHelper.GenerateNullExpression(parameterType);
                    if (nullExpression == null) { 
                        // in this case we can't assign null to the parameter and the method is gonna throw a StrongTypingException 
                        throwsArray[0] = CodeGenHelper.GlobalType(typeof(System.Data.StrongTypingException));
                        methodThrows = true; 
                    }
                }
            }
 
            if (!methodThrows) {
                int returnParamPos = GetReturnParameterPosition(activeCommand); 
                if (returnParamPos >= 0 && !this.getMethod && this.methodSource.QueryType != QueryType.Scalar) { 
                    Type returnType = GetParameterUrtType((DesignParameter)activeCommand.Parameters[returnParamPos]);
                    CodeExpression nullExpression = CodeGenHelper.GenerateNullExpression(returnType); 
                    if (nullExpression == null) {
                        // in this case we can't assign null to the parameter and the method is gonna throw a StrongTypingException
                        throwsArray[0] = CodeGenHelper.GlobalType(typeof(System.Data.StrongTypingException));
                        methodThrows = true; 
                    }
                } 
            } 

            if (methodThrows) { 
                dbMethod.UserData.Add("throwsCollection", new CodeTypeReferenceCollection(throwsArray));
            }
        }
 
        protected void AddSetParameterStatements(DesignParameter parameter, string parameterName, CodeExpression cmdExpression,
            int parameterIndex, IList statements) { 
 
            AddSetParameterStatements(parameter, parameterName, null, cmdExpression, parameterIndex, 0, statements);
        } 

        protected void AddSetParameterStatements(DesignParameter parameter, string parameterName, DesignParameter isNullParameter,
            CodeExpression cmdExpression, int parameterIndex, int isNullParameterIndex, IList statements) {
 
            Type parameterType = GetParameterUrtType(parameter);
            CodeCastExpression cce = new CodeCastExpression(parameterType, CodeGenHelper.Argument(parameterName)); 
            // J# specific UserData 
            cce.UserData.Add("CastIsBoxing", true);
 
            CodeCastExpression zero = null;
            CodeCastExpression one = null;
            if (this.codeGenerator != null && CodeGenHelper.IsGeneratingJSharpCode(this.codeGenerator.CodeProvider)) {
                // J# has casting rules that conflict with the ones of other languages, so we need to create a different expression 
                // this is unfortunate, as it forces us to special-case the CodeDom-tree for a specific language.
                zero = new CodeCastExpression(typeof(int), CodeGenHelper.Primitive(0)); 
                // J# specific UserData 
                zero.UserData.Add("CastIsBoxing", true);
                one = new CodeCastExpression(typeof(int), CodeGenHelper.Primitive(1)); 
                // J# specific UserData
                one.UserData.Add("CastIsBoxing", true);
            }
            else { 
                zero = new CodeCastExpression(typeof(object), CodeGenHelper.Primitive(0));
                one = new CodeCastExpression(typeof(object), CodeGenHelper.Primitive(1)); 
            } 

            CodeExpression paramValueExpression = CodeGenHelper.Property( 
                CodeGenHelper.Indexer(
                    CodeGenHelper.Property(
                        cmdExpression,
                        "Parameters" 
                    ),
                    CodeGenHelper.Primitive(parameterIndex) 
                ), 
                "Value"
            ); 

            CodeExpression isNullParamValueExpression = null;
            if (isNullParameter != null) {
                isNullParamValueExpression = CodeGenHelper.Property( 
                    CodeGenHelper.Indexer(
                        CodeGenHelper.Property( 
                            cmdExpression, 
                            "Parameters"
                        ), 
                        CodeGenHelper.Primitive(isNullParameterIndex)
                    ),
                    "Value"
                ); 
            }
 
            int statementCount = (isNullParameter == null) ? 1 : 2; 
            CodeStatement[] trueStatements = new CodeStatement[statementCount];
            CodeStatement[] falseStatements = new CodeStatement[statementCount]; 

            if (parameter.AllowDbNull && parameterType.IsValueType) {
                //\\ if(<paramName>.HasValue) {
                //\\    <Command>.Parameters[i].Value = (<paramType>)<paramName>.Value; 
                //\\    <Command>.Parameters[j].Value = 0;
                //\\ } 
                //\\ else { 
                //\\    <Command>.Parameters[i].Value = System.DBNull;
                //\\    <Command>.Parameters[j].Value = 1; 
                //\\ }
                cce = new CodeCastExpression(
                    parameterType,
                    CodeGenHelper.Property( 
                        CodeGenHelper.Argument(
                            parameterName 
                        ), 
                        "Value"
                    ) 
                );
                // J# specific UserData
                cce.UserData.Add("CastIsBoxing", true);
 
                trueStatements[0] = CodeGenHelper.Assign(paramValueExpression, cce);
                falseStatements[0] = CodeGenHelper.Assign( 
                    paramValueExpression, 
                    CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.DBNull)), "Value")
                ); 

                if (isNullParameter != null) {
                    trueStatements[1] = trueStatements[0];
                    falseStatements[1] = falseStatements[0]; 
                    trueStatements[0] = CodeGenHelper.Assign(isNullParamValueExpression, zero);
                    falseStatements[0] = CodeGenHelper.Assign(isNullParamValueExpression, one); 
                } 

                statements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.EQ(
                            CodeGenHelper.Property(CodeGenHelper.Argument(parameterName), "HasValue"),
                            CodeGenHelper.Primitive(true) 
                        ),
                        trueStatements, 
                        falseStatements 
                    )
                ); 
            }
            else if (parameter.AllowDbNull && !parameterType.IsValueType) {
                //\\ if(<paramName> == null) {
                //\\    <Command>.Parameters[i].Value = System.DBNull.Value; 
                //\\    <Command>.Parameters[j].Value = 1;
                //\\ } 
                //\\ else { 
                //\\    <Command>.Parameters[i].Value = (<paramType>)<paramName>;
                //\\    <Command>.Parameters[j].Value = 0; 
                //\\ }
                trueStatements[0] = CodeGenHelper.Assign(
                    paramValueExpression,
                    CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.DBNull)), "Value") 
                );
                falseStatements[0] = CodeGenHelper.Assign(paramValueExpression, cce); 
 
                if (isNullParameter != null) {
                    trueStatements[1] = trueStatements[0]; 
                    falseStatements[1] = falseStatements[0];
                    trueStatements[0] = CodeGenHelper.Assign(isNullParamValueExpression, one);
                    falseStatements[0] = CodeGenHelper.Assign(isNullParamValueExpression, zero);
                } 

                statements.Add( 
                    CodeGenHelper.If( 
                        CodeGenHelper.IdEQ(
                            CodeGenHelper.Argument(parameterName), 
                            CodeGenHelper.Primitive(null)
                        ),
                        trueStatements,
                        falseStatements 
                    )
                ); 
            } 
            else if (!parameter.AllowDbNull && !parameterType.IsValueType) {
                //\\ if(<paramName> == null) { 
                //\\    throw new ArgumentNullException("<paramName>");
                //\\ }
                //\\ else {
                //\\    <Command>.Parameters[i].Value = (<paramType>)<paramName>; 
                //\\    <Command>.Parameters[i].Value = 0;
                //\\ } 
                CodeStatement[] trueStatement = new CodeStatement[1]; 
                trueStatement[0] = CodeGenHelper.Throw(CodeGenHelper.GlobalType(typeof(System.ArgumentNullException)), parameterName);
                falseStatements[0] = CodeGenHelper.Assign(paramValueExpression, cce); 

                if (isNullParameter != null) {
                    falseStatements[1] = falseStatements[0];
                    falseStatements[0] = CodeGenHelper.Assign(isNullParamValueExpression, zero); 
                }
 
                statements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdEQ( 
                            CodeGenHelper.Argument(parameterName),
                            CodeGenHelper.Primitive(null)
                        ),
                        trueStatement, 
                        falseStatements
                    ) 
                ); 
            }
            else if (!parameter.AllowDbNull && parameterType.IsValueType) { 
                //\\ <Command>.Parameters[j].Value = 0;
                //\\ <Command>.Parameters[i].Value = (<paramType>)<paramName>;
                if (isNullParameter != null) {
                    statements.Add(CodeGenHelper.Assign(isNullParamValueExpression, zero)); 
                }
                statements.Add(CodeGenHelper.Assign(paramValueExpression, cce)); 
            } 
        }
 
        protected bool AddSetReturnParamValuesStatements(IList statements, CodeExpression commandExpression) {
            int paramCount = 0;
            if (this.activeCommand.Parameters != null) {
                paramCount = this.activeCommand.Parameters.Count; 
            }
 
            for (int i = 0; i < paramCount; i++) { 
                DesignParameter parameter = activeCommand.Parameters[i] as DesignParameter;
                if (parameter == null) { 
                    throw new DataSourceGeneratorException("Parameter type is not DesignParameter.");
                }

                if (parameter.Direction == ParameterDirection.Output || parameter.Direction == ParameterDirection.InputOutput) { 
                    // get parameter type
                    Type parameterType = GetParameterUrtType(parameter); 
                    string parameterName = nameHandler.GetNameFromList(parameter.ParameterName); 

                    // create parameter expression 
                    //\\ <commandExpression>.Parameters[i].Value
                    CodeExpression outputParamExpression = CodeGenHelper.Property(
                        CodeGenHelper.Indexer(
                            CodeGenHelper.Property( 
                                commandExpression,
                                "Parameters" 
                            ), 
                            CodeGenHelper.Primitive(i)
                        ), 
                        "Value"
                    );

                    // if(command.Parameters[i].Value.GetType() == typeof(System.DBNull)) 
                    CodeExpression isEqualDbNullCondition = CodeGenHelper.GenerateDbNullCheck(outputParamExpression);
 
                    CodeExpression nullExpression = CodeGenHelper.GenerateNullExpression(parameterType); 
                    CodeStatement trueStatement = null;
 
                    if (nullExpression == null) {
                        if (parameter.AllowDbNull && parameterType.IsValueType) {
                            //\\ <parameter> = new System.Nullable<parameterType>();
                            trueStatement = CodeGenHelper.Assign( 
                                CodeGenHelper.Argument(parameterName),
                                CodeGenHelper.New( 
                                    CodeGenHelper.NullableType(parameterType), 
                                    new CodeExpression[] { }
                                ) 
                            );
                        }
                        else if (parameter.AllowDbNull && !parameterType.IsValueType) {
                            //\\ <parameter> = null; 
                            trueStatement = CodeGenHelper.Assign(
                                CodeGenHelper.Argument(parameterName), 
                                CodeGenHelper.Primitive(null) 
                            );
                        } 
                        else {
                            // in this case we can't assign null to the parameter
                            //\\ throw new StrongTypingException("StrongTyping_CannotAccessDBNull");
                            trueStatement = CodeGenHelper.Throw(CodeGenHelper.GlobalType(typeof(System.Data.StrongTypingException)), SR.GetString(SR.CG_ParameterIsDBNull, parameterName), CodeGenHelper.Primitive(null)); 
                        }
                    } 
                    else { 
                        //\\ <parameter> = <nullExpression>
                        trueStatement = CodeGenHelper.Assign( 
                            CodeGenHelper.Argument(nameHandler.GetNameFromList(parameter.ParameterName)),
                            nullExpression
                        );
                    } 

                    CodeStatement falseStatement = null; 
                    if (parameter.AllowDbNull && parameterType.IsValueType) { 
                        //\\ <parameter> = new System.Nullable<parameterType>((<parameterType>) command.Parameter[i].Value);
                        falseStatement = CodeGenHelper.Assign( 
                            CodeGenHelper.Argument(parameterName),
                            CodeGenHelper.New(
                                CodeGenHelper.NullableType(parameterType),
                                new CodeExpression[] { CodeGenHelper.Cast(CodeGenHelper.GlobalType(parameterType), outputParamExpression) } 
                            )
                        ); 
                    } 
                    else {
                        //\\ <parameter> = (<parameterType>) command.Parameters[i].Value; 
                        falseStatement = CodeGenHelper.Assign(
                            CodeGenHelper.Argument(parameterName),
                            CodeGenHelper.Cast(CodeGenHelper.GlobalType(parameterType), outputParamExpression)
                        ); 
                    }
 
                    statements.Add( 
                        CodeGenHelper.If(
                            isEqualDbNullCondition, 
                            trueStatement,
                            falseStatement
                        )
                    ); 
                }
            } 
 
            return true;
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
