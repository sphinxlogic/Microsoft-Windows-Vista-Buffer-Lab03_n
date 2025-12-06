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
    using System.Data; 
    using System.Data.SqlTypes;
    using System.Design; 
    using System.Diagnostics; 
    using System.IO;
    using System.Reflection; 


    internal sealed class CodeGenHelper {
        /// <summary> 
        /// Private contstructor to avoid class being instantiated.
        /// </summary> 
        private CodeGenHelper() { 
        }
 
        // CodeGen Helper functions :
        // -------------------- Expressions: ----------------------------
        //\\ this
        internal static CodeExpression     This() { return new CodeThisReferenceExpression();} 
        //\\ base
        internal static CodeExpression     Base() { return new CodeBaseReferenceExpression();} 
        //\\ value 
        internal static CodeExpression     Value() { return new CodePropertySetValueReferenceExpression();}
        //\\ <type> 
        internal static CodeTypeReference  Type(string type) { return new CodeTypeReference(type); }
        internal static CodeTypeReference  Type(Type type) { return new CodeTypeReference(type); }
        internal static CodeTypeReference  NullableType(Type type) {
            CodeTypeReference ctr = new CodeTypeReference(typeof(System.Nullable)); 
            ctr.Options = CodeTypeReferenceOptions.GlobalReference;
            ctr.TypeArguments.Add(CodeGenHelper.GlobalType(type)); 
 
            return ctr;
        } 
        //\\ <type>[<rank>]
        //internal static CodeTypeReference  Type(Type type, Int32 rank) { return new CodeTypeReference(type.ToString(), rank); }
        internal static CodeTypeReference  Type(string type, Int32 rank) { return new CodeTypeReference(type, rank); }
        internal static CodeTypeReference  GlobalType(Type type) { return new CodeTypeReference(type.ToString(), CodeTypeReferenceOptions.GlobalReference); } 
        internal static CodeTypeReference  GlobalType(Type type, Int32 rank) { return new CodeTypeReference(CodeGenHelper.GlobalType(type), rank); }
        internal static CodeTypeReference  GlobalType(string type) { return new CodeTypeReference(type, CodeTypeReferenceOptions.GlobalReference); } 
        //\\ <type> 
        internal static CodeTypeReferenceExpression TypeExpr(CodeTypeReference   type) { return new CodeTypeReferenceExpression(type); }
        internal static CodeTypeReferenceExpression GlobalTypeExpr(Type type) { return new CodeTypeReferenceExpression(GlobalType(type)); } 
        internal static CodeTypeReferenceExpression GlobalTypeExpr(string type) { return new CodeTypeReferenceExpression(GlobalType(type)); }
        //\\ GlobalGenericType
        internal static CodeTypeReference GlobalGenericType(string fullTypeName, Type itemType) {
            return GlobalGenericType(fullTypeName, CodeGenHelper.GlobalType(itemType)); 
        }
        internal static CodeTypeReference GlobalGenericType(string fullTypeName, CodeTypeReference itemType) { 
            CodeTypeReference genericTypeRef = new CodeTypeReference(fullTypeName, itemType); 
            genericTypeRef.Options = CodeTypeReferenceOptions.GlobalReference;
            return genericTypeRef; 
        }

        //\\ ((<type>)<expr>)
        internal static CodeExpression     Cast(CodeTypeReference type, CodeExpression expr) { return new CodeCastExpression(type, expr); } 
        //\\ typeof(<type>)
        internal static CodeExpression     TypeOf(CodeTypeReference type) { return new CodeTypeOfExpression(type); } 
        //\\ <exp>.field 
        internal static CodeExpression     Field(CodeExpression exp, string field) { return new CodeFieldReferenceExpression(exp, field);}
        //\\ this.field 
        internal static CodeExpression     ThisField(string field) { return new CodeFieldReferenceExpression(This(), field); }
        //\\ <exp>.property
        internal static CodeExpression     Property(CodeExpression exp, string property) { return new CodePropertyReferenceExpression(exp, property);}
        //\\ this.property 
        internal static CodeExpression     ThisProperty(string property) { return new CodePropertyReferenceExpression(This(), property); }
        //\\ argument 
        internal static CodeExpression     Argument(string argument) { return new CodeArgumentReferenceExpression(argument);} 
        //\\ variable
        internal static CodeExpression     Variable(string variable) { return new CodeVariableReferenceExpression(variable);} 
        //\\ this.eventName
        internal static CodeExpression     Event(string eventName) { return new CodeEventReferenceExpression(This(), eventName);}
        //\\ new <type>(<parameters>)
        internal static CodeExpression     New(CodeTypeReference type, CodeExpression[] parameters) { return new CodeObjectCreateExpression(type, parameters);} 
        //\\ new <type>[<size>]
        internal static CodeExpression     NewArray(CodeTypeReference type, int size) { return new CodeArrayCreateExpression(type, size); } 
        //\\ new <type> {<param1>, <param2>, ...} 
        internal static CodeExpression     NewArray(CodeTypeReference type, params CodeExpression[] initializers) { return new CodeArrayCreateExpression(type, initializers); }
        //\\ <primitive> 
        internal static CodeExpression     Primitive(object primitive) { return new CodePrimitiveExpression(primitive);}
        //\\ "<str>"
        internal static CodeExpression     Str(string str) { return Primitive(str);}
        //\\ <targetObject>.<methodName>(<parameters>) 
        internal static CodeExpression     MethodCall(CodeExpression targetObject, String methodName, CodeExpression[] parameters) {
            return new CodeMethodInvokeExpression(targetObject, methodName, parameters); 
        } 
        //\\ <targetObject>.<methodName>(<parameters>)
        internal static CodeStatement      MethodCallStm(CodeExpression targetObject, String methodName, CodeExpression[] parameters) { 
            return Stm(MethodCall(targetObject, methodName, parameters));
        }
        //\\ <targetObject>.<methodName>()
        internal static CodeExpression     MethodCall(CodeExpression targetObject, String methodName) { 
            return new CodeMethodInvokeExpression(targetObject, methodName);
        } 
        //\\ <targetObject>.<methodName>(<parameters>) 
        internal static CodeStatement     MethodCallStm(CodeExpression targetObject, String methodName) {
            return Stm(MethodCall(targetObject, methodName)); 
        }
        //\\ <targetObject>.<methodName>(par)
        internal static CodeExpression     MethodCall(CodeExpression targetObject, String methodName, CodeExpression par) {
            return new CodeMethodInvokeExpression(targetObject, methodName, new CodeExpression[] {par}); 
        }
        //\\ <targetObject>.<methodName>(par) 
        internal static CodeStatement      MethodCallStm(CodeExpression targetObject, String methodName, CodeExpression par) { 
            return Stm(MethodCall(targetObject, methodName, par));
        } 
        //\\ <targetObject>(par)
        internal static CodeExpression     DelegateCall(CodeExpression targetObject, CodeExpression par) {
            return new CodeDelegateInvokeExpression(targetObject, new CodeExpression[] {This(), par});
        } 
        //\\ <targetObject>[indices]()
        internal static CodeExpression     Indexer(CodeExpression targetObject, CodeExpression indices) {return new CodeIndexerExpression(targetObject, indices);} 
        //\\ <targetObject>[indices]() 
        internal static CodeExpression     ArrayIndexer(CodeExpression targetObject, CodeExpression indices) { return new CodeArrayIndexerExpression(targetObject, indices); }
 
        //\\ ReferenceEquals
        internal static CodeExpression ReferenceEquals(CodeExpression left, CodeExpression right) {
            return CodeGenHelper.MethodCall(
                       CodeGenHelper.GlobalTypeExpr(typeof(object)), 
                       "ReferenceEquals",
                       new CodeExpression[] { left, right } 
           ); 
        }
        //\\ ReferenceNotEquals 
        internal static CodeExpression ReferenceNotEquals(CodeExpression left, CodeExpression right) {
            return CodeGenHelper.EQ(ReferenceEquals(left, right), Primitive(false));
        }
 
        // -------------------- Binary Operators: ----------------------------
        internal static CodeBinaryOperatorExpression      BinOperator(CodeExpression left, CodeBinaryOperatorType op, CodeExpression right) { 
            return new CodeBinaryOperatorExpression(left, op, right); 
        }
        //\\ (left) != (right) 
        internal static CodeBinaryOperatorExpression      IdNotEQ(CodeExpression left, CodeExpression right) {return BinOperator(left, CodeBinaryOperatorType.IdentityInequality, right);}
        //\\ (left) != (right)
        internal static CodeBinaryOperatorExpression      IdEQ(CodeExpression left, CodeExpression right) {return BinOperator(left, CodeBinaryOperatorType.IdentityEquality, right);}
        //\\ (left) is null 
        internal static CodeBinaryOperatorExpression      IdIsNull(CodeExpression id) { return IdEQ(id, Primitive(null)); }
        //\\ (left) isnot null 
        internal static CodeBinaryOperatorExpression      IdIsNotNull(CodeExpression id) { return IdNotEQ(id, Primitive(null)); } 
        //\\ (left) == (right)
        internal static CodeBinaryOperatorExpression      EQ(CodeExpression left, CodeExpression right) {return BinOperator(left, CodeBinaryOperatorType.ValueEquality, right);} 
        //\\ (left) != (right)
        internal static CodeBinaryOperatorExpression      NotEQ(CodeExpression left, CodeExpression right) { return EQ(EQ(left, right), Primitive(false)); }
        //\\ (left) && (right)
        internal static CodeBinaryOperatorExpression      BitwiseAnd(CodeExpression left, CodeExpression right) {return BinOperator(left, CodeBinaryOperatorType.BitwiseAnd, right);} 
        //\\ (left) && (right)
        internal static CodeBinaryOperatorExpression      And(CodeExpression left, CodeExpression right) { return BinOperator(left, CodeBinaryOperatorType.BooleanAnd, right); } 
        //\\ (left) || (right) 
        internal static CodeBinaryOperatorExpression      Or(CodeExpression left, CodeExpression right) { return BinOperator(left, CodeBinaryOperatorType.BooleanOr, right); }
        //\\ (left) < (right) 
        internal static CodeBinaryOperatorExpression      Less(CodeExpression left, CodeExpression right) {return BinOperator(left, CodeBinaryOperatorType.LessThan     , right);}

        // -------------------- Statments: ----------------------------
        //\\ <expr>; 
        internal static CodeStatement      Stm(CodeExpression expr) { return new CodeExpressionStatement(expr);}
        //\\ return(<expr>); 
        internal static CodeStatement      Return(CodeExpression expr) { return new CodeMethodReturnStatement(expr);} 
        //\\ return;
        internal static CodeStatement      Return() { return new CodeMethodReturnStatement(); } 
        //\\ left = right;
        internal static CodeStatement      Assign(CodeExpression left, CodeExpression right) { return new CodeAssignStatement(left, right);}
        //\\ throw new <exception>(<arg>)
        internal static CodeStatement      Throw(CodeTypeReference exception, string arg) { 
            return new CodeThrowExceptionStatement(New(exception, new CodeExpression[] {Str(arg)}));
        } 
        //\\ throw new <exception>(<arg>, <inner>) 
        internal static CodeStatement      Throw(CodeTypeReference exception, string arg, string inner) {
            return new CodeThrowExceptionStatement(New(exception, new CodeExpression[] {Str(arg), Variable(inner)})); 
        }
        //\\ throw new <exception>(<arg>, inner)
        internal static CodeStatement      Throw(CodeTypeReference exception, string arg, CodeExpression inner) {
            return new CodeThrowExceptionStatement(New(exception, new CodeExpression[] {Str(arg), inner})); 
        }
        // ------------Comments -------------------------------- 
        internal static CodeCommentStatement Comment(string comment, bool docSummary) { 
            if (docSummary) {
                return new CodeCommentStatement("<summary>\r\n" + comment + "\r\n</summary>", docSummary); 
            }
            return new CodeCommentStatement(comment);
        }
 

        // -------------------- If: ---------------------------- 
        internal static CodeStatement If(CodeExpression cond, CodeStatement[] trueStms, CodeStatement[] falseStms) { 
            return new CodeConditionStatement(cond, trueStms, falseStms);
        } 
        internal static CodeStatement If(CodeExpression cond, CodeStatement trueStm, CodeStatement falseStm) {
            return new CodeConditionStatement(cond, new CodeStatement[] {trueStm}, new CodeStatement[] {falseStm});
        }
        internal static CodeStatement If(CodeExpression cond, CodeStatement[] trueStms ) {return new CodeConditionStatement(cond, trueStms);} 
        internal static CodeStatement If(CodeExpression cond, CodeStatement   trueStm  ) {return If(   cond, new CodeStatement[] {trueStm });}
        // -------------------- Declarations: ---------------------------- 
        internal static CodeMemberField  FieldDecl(CodeTypeReference type, String name) { return new CodeMemberField(type, name); } 

        internal static CodeMemberField  FieldDecl(CodeTypeReference type, String name, CodeExpression initExpr) { 
            CodeMemberField field = new CodeMemberField(type, name);
            field.InitExpression = initExpr;

            return field; 
        }
 
        internal static CodeTypeDeclaration Class(string name, bool isPartial, TypeAttributes typeAttributes) { 
            CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(name);
            typeDeclaration.IsPartial = isPartial; 
            typeDeclaration.TypeAttributes = typeAttributes;

            CodeAttributeDeclaration generatedCodeAttribute = new CodeAttributeDeclaration(
                CodeGenHelper.GlobalType(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute)), 
                new CodeAttributeArgument(Str(typeof(System.Data.Design.TypedDataSetGenerator).FullName)),
                new CodeAttributeArgument(Str(ThisAssembly.Version))); 
            typeDeclaration.CustomAttributes.Add(generatedCodeAttribute); 

            return typeDeclaration; 
        }

        internal static CodeConstructor Constructor(MemberAttributes attributes) {
            CodeConstructor constructor = new CodeConstructor(); 
            constructor.Attributes = attributes;
            constructor.CustomAttributes.Add(AttributeDecl(typeof(System.Diagnostics.DebuggerNonUserCodeAttribute).FullName)); 
 
            return constructor;
        } 

        internal static CodeMemberMethod MethodDecl(CodeTypeReference type, String name, MemberAttributes attributes) {
            CodeMemberMethod method = new CodeMemberMethod();
            method.ReturnType = type; 
            method.Name       = name;
            method.Attributes = attributes; 
            method.CustomAttributes.Add(AttributeDecl(typeof(System.Diagnostics.DebuggerNonUserCodeAttribute).FullName)); 

            return method; 
        }

        internal static CodeMemberProperty PropertyDecl(CodeTypeReference type, String name, MemberAttributes attributes) {
            CodeMemberProperty property = new CodeMemberProperty(); 
            property.Type       = type;
            property.Name       = name; 
            property.Attributes = attributes; 
            property.CustomAttributes.Add(AttributeDecl(typeof(System.Diagnostics.DebuggerNonUserCodeAttribute).FullName));
 
            return property;
        }

        internal static CodeStatement VariableDecl(CodeTypeReference type, String name) { return new CodeVariableDeclarationStatement(type, name); } 
        internal static CodeStatement VariableDecl(CodeTypeReference type, String name, CodeExpression initExpr) { return new CodeVariableDeclarationStatement(type, name, initExpr); }
 
        internal static CodeStatement ForLoop(CodeStatement initStmt, CodeExpression testExpression, CodeStatement incrementStmt, CodeStatement[] statements) { 
            return new CodeIterationStatement(
                initStmt, 
                testExpression,
                incrementStmt,
                statements
                ); 
        }
 
        internal static CodeMemberEvent EventDecl(string type, String name) { 
            CodeMemberEvent anEvent = new CodeMemberEvent();
            anEvent.Name       = name; 
            anEvent.Type       = Type(type);
            anEvent.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            return anEvent; 
        }
 
        internal static CodeParameterDeclarationExpression     ParameterDecl(CodeTypeReference type, string name) { return new CodeParameterDeclarationExpression(type, name); } 
        internal static CodeAttributeDeclaration AttributeDecl(string name) {
            return new CodeAttributeDeclaration(GlobalType(name)); 
        }
        internal static CodeAttributeDeclaration AttributeDecl(string name, CodeExpression value) {
            return new CodeAttributeDeclaration(GlobalType(name), new CodeAttributeArgument[] { new CodeAttributeArgument(value) });
        } 
        internal static CodeAttributeDeclaration AttributeDecl(string name, CodeExpression value1, CodeExpression value2) {
            return new CodeAttributeDeclaration(GlobalType(name), new CodeAttributeArgument[] { new CodeAttributeArgument(value1), new CodeAttributeArgument(value2)}); 
        } 
        // -------------------- Try/Catch ---------------------------
        //\\ try {<tryStmnt>} <catchClause> 
        internal static CodeStatement      Try(CodeStatement tryStmnt, CodeCatchClause catchClause) {
            return new CodeTryCatchFinallyStatement(
                new CodeStatement[] {tryStmnt},
                new CodeCatchClause[] {catchClause} 
                );
        } 
        //\\ try {<tryStmnt>;<tryStmnt>;...} <catchClause>; <catchClause>; ... Finally {<finallyStmt>;<finallyStmt>;...} 
        internal static CodeStatement      Try(CodeStatement[] tryStmnts, CodeCatchClause[] catchClauses, CodeStatement[] finallyStmnts) {
            return new CodeTryCatchFinallyStatement( 
                tryStmnts,
                catchClauses,
                finallyStmnts
                ); 
        }
 
        //\\ catch(<type> <name>) {<catchStmnt>} 
        internal static CodeCatchClause Catch(CodeTypeReference type, string name, CodeStatement catchStmnt) {
            CodeCatchClause ccc = new CodeCatchClause(); 
            ccc.CatchExceptionType = type;
            ccc.LocalName = name;
            if (catchStmnt != null) {
                ccc.Statements.Add(catchStmnt); 
            }
            return ccc; 
        } 

        internal static FieldDirection ParameterDirectionToFieldDirection(ParameterDirection paramDirection) { 
            FieldDirection fieldDir = FieldDirection.In;
            switch (paramDirection) {
                case(ParameterDirection.Output) :
                    fieldDir = FieldDirection.Out; 
                    break;
                case(ParameterDirection.Input) : 
                    fieldDir = FieldDirection.In; 
                    break;
                case(ParameterDirection.InputOutput) : 
                    fieldDir = FieldDirection.Ref;
                    break;
                case(ParameterDirection.ReturnValue) :
                    throw new InternalException("Can't map from ParameterDirection.ReturnValue to FieldDirection."); 
                default:
                    throw new InternalException("Unknown ParameterDirection."); 
            } 

            return fieldDir; 
        }

        internal static CodeExpression GenerateDbNullCheck(CodeExpression returnParam) {
            return CodeGenHelper.Or( 
                CodeGenHelper.IdEQ(
                    returnParam, 
                    CodeGenHelper.Primitive(null) 
                ),
                CodeGenHelper.IdEQ( 
                    CodeGenHelper.MethodCall(
                        returnParam,
                        "GetType"
                    ), 
                    CodeGenHelper.TypeOf(CodeGenHelper.GlobalType(typeof(System.DBNull)))
                ) 
            ); 
        }
 
        internal static CodeExpression GenerateNullExpression(Type returnType) {
            if(IsSqlType(returnType)) {
                return CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(returnType), "Null");
            } 
            else {
                // 
 
                if(returnType == typeof(object)) {
                    return CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.DBNull)), "Value"); 
                }
                else if (!returnType.IsValueType){
                    return CodeGenHelper.Primitive(null);
                } 
                else {
                    return null; 
                } 
            }
        } 

        internal static CodeExpression GenerateConvertExpression(CodeExpression sourceExpression, Type sourceType, Type targetType) {
            if (sourceType == targetType) {
                return sourceExpression; 
            }
 
            if (IsSqlType(sourceType)) { 
                if (IsSqlType(targetType)) {
                    throw new InternalException("Cannot perform the conversion between 2 SqlTypes."); 
                }
                else {
                    // Source is SqlType but not target.
                    PropertyInfo valuePropertyInfo = sourceType.GetProperty("Value"); 
                    if (valuePropertyInfo == null) throw new InternalException("Type does not expose a 'Value' property.");
 
                    Type equivalentUrtType = valuePropertyInfo.PropertyType; 

                    CodeExpression valueExpression = new CodePropertyReferenceExpression(sourceExpression, "Value"); 
                    return GenerateUrtConvertExpression(valueExpression, equivalentUrtType, targetType);
                }
            }
            else { 
                if (IsSqlType(targetType)) {
                    // Source is NOT SqlType but target is. 
                    PropertyInfo valuePropertyInfo = targetType.GetProperty("Value"); 
                    Type equivalentUrtType = valuePropertyInfo.PropertyType;
 
                    CodeExpression urtConvertExpression = GenerateUrtConvertExpression(sourceExpression, sourceType, equivalentUrtType);

                    return new CodeObjectCreateExpression(targetType, urtConvertExpression);
                } 
                else {
                    // Neither Source Nor Target are SqlType. 
                    return GenerateUrtConvertExpression(sourceExpression, sourceType, targetType); 
                }
            } 
        }

        internal static string GetTypeName(System.CodeDom.Compiler.CodeDomProvider codeProvider, string string1, string string2) {
            string activatorTypeName = codeProvider.GetTypeOutput(CodeGenHelper.Type(typeof(System.Activator))); 

            string typeSeparator = activatorTypeName.Replace("System", "").Replace("Activator", ""); 
 
            return string1 + typeSeparator + string2;
        } 

        internal static bool SupportsMultipleNamespaces(System.CodeDom.Compiler.CodeDomProvider codeProvider) {
            string ns1Name = MemberNameValidator.GenerateIdName("TestNs1", codeProvider, false /*useSuffix*/);
            string ns2Name = MemberNameValidator.GenerateIdName("TestNs2", codeProvider, false /*useSuffix*/); 
            CodeNamespace ns1 = new CodeNamespace(ns1Name);
            CodeNamespace ns2 = new CodeNamespace(ns2Name); 
 
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(ns1); 
            compileUnit.Namespaces.Add(ns2);

            StringWriter writer = new StringWriter(System.Globalization.CultureInfo.CurrentCulture);
            codeProvider.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions()); 

            string generatedCode = writer.GetStringBuilder().ToString(); 
 
            return (generatedCode.Contains(ns1Name) && generatedCode.Contains(ns2Name));
        } 

        internal static DSGeneratorProblem GenerateValueExprAndFieldInit(DesignColumn designColumn,
            object valueObj,
            object value, 
            string className,
            string fieldName, 
            out CodeExpression valueExpr, 
            out CodeExpression fieldInit)
        { 
            DataColumn column = designColumn.DataColumn;
            valueExpr = null;
            fieldInit = null;
 
            if(
                column.DataType == typeof(char)   || column.DataType == typeof(string) || 
                column.DataType == typeof(decimal)|| column.DataType == typeof(bool)   || 
                column.DataType == typeof(Single) || column.DataType == typeof(double) ||
                column.DataType == typeof(SByte)  || column.DataType == typeof(Byte)   || 
                column.DataType == typeof(Int16)  || column.DataType == typeof(UInt16) ||
                column.DataType == typeof(Int32)  || column.DataType == typeof(UInt32) ||
                column.DataType == typeof(Int64)  || column.DataType == typeof(UInt64)
            ) { // types can be presented by literal. Really this is language dependent :-( 
                valueExpr = CodeGenHelper.Primitive(valueObj);
            } 
            else { 
                valueExpr = CodeGenHelper.Field(
                    CodeGenHelper.TypeExpr(CodeGenHelper.Type(className)), 
                    fieldName
                );
                //\\ private static <ColumnType> <ColumnName>_nullValue = new <ColumnType>("<nullValue>");
                if(column.DataType == typeof(Byte[])) { 
                    fieldInit = CodeGenHelper.MethodCall(
                        CodeGenHelper.GlobalTypeExpr(typeof(System.Convert)), 
                        "FromBase64String", 
                        CodeGenHelper.Primitive(value)
                    ); 
                }
                else if(column.DataType == typeof(DateTime)) {
                    fieldInit = CodeGenHelper.MethodCall(
                        CodeGenHelper.GlobalTypeExpr(column.DataType), 
                        "Parse",
                        CodeGenHelper.Primitive(((DateTime)valueObj).ToString(System.Globalization.DateTimeFormatInfo.InvariantInfo))); 
                } 
                else if(column.DataType == typeof(TimeSpan)) {
                    fieldInit = CodeGenHelper.MethodCall( 
                        CodeGenHelper.GlobalTypeExpr(column.DataType),
                        "Parse",
                        CodeGenHelper.Primitive(valueObj.ToString()));
                }else /*object*/ { 
                    /* check that type can be constructed from this string */ {
                        ConstructorInfo constructor = column.DataType.GetConstructor(new Type[] {typeof(string)}); 
                        if(constructor == null) { 
                            return new DSGeneratorProblem(SR.GetString(SR.CG_NoCtor1, column.ColumnName, column.DataType.Name), ProblemSeverity.NonFatalError, designColumn);
                        } 
                        constructor.Invoke(new Object[] {value}); // can throw here.
                    }
                    fieldInit = CodeGenHelper.New(
                        CodeGenHelper.GlobalType(column.DataType), 
                        new CodeExpression[] {CodeGenHelper.Primitive(value)}
                    ); 
                } 
            }
 
            return null;
        }

        internal static string GetLanguageExtension(CodeDomProvider codeProvider) { 
            if (codeProvider == null) {
                return string.Empty; 
            } 
            else {
                string extension = "." + codeProvider.FileExtension; 
                if (extension.StartsWith("..", StringComparison.Ordinal)) {
                    extension = extension.Substring(1);
                }
 
                return extension;
            } 
        } 

        internal static bool IsGeneratingJSharpCode(CodeDomProvider codeProvider) { 
            return StringUtil.EqualValue(GetLanguageExtension(codeProvider), ".jsl");
        }

        //internal static bool IsGeneratingCSharpCode(CodeDomProvider codeProvider) { 
        //    return StringUtil.EqualValue(GetLanguageExtension(codeProvider), ".cs");
        //} 
 

        private static bool IsSqlType(Type type){ 
            if (type == typeof(SqlBinary) ||
                type == typeof(SqlBoolean) ||
                type == typeof(SqlByte) ||
                type == typeof(SqlDateTime) || 
                type == typeof(SqlDecimal) ||
                type == typeof(SqlDouble) || 
                type == typeof(SqlGuid) || 
                type == typeof(SqlInt16) ||
                type == typeof(SqlInt32) || 
                type == typeof(SqlInt64) ||
                type == typeof(SqlMoney) ||
                type == typeof(SqlSingle) ||
                type == typeof(SqlString)) { 

                return true; 
            } else { 
                return false;
            } 
        }


        // GenerateUrtConvertExpression -- Generates appropriate code to do the conversion between 2 URT types. 
        //                                 e.g. : Convert.ToInt32( intExpression )
        private static CodeExpression GenerateUrtConvertExpression(CodeExpression sourceExpression, Type sourceUrtType, Type targetUrtType) { 
 
            if (sourceUrtType == targetUrtType) {
                return sourceExpression; 
            }

            if (sourceUrtType == typeof(System.Object)) {
                // in this case we just generate a cast expression 
                return CodeGenHelper.Cast(CodeGenHelper.GlobalType(targetUrtType), sourceExpression);
            } 
 
            // First see if System.Convert can do the conversion directly using the ToXXX methods.
            if (ConversionHelper.CanConvert(sourceUrtType, targetUrtType)) { 
                return new CodeMethodInvokeExpression(CodeGenHelper.GlobalTypeExpr("System.Convert"), ConversionHelper.GetConversionMethodName(sourceUrtType, targetUrtType), sourceExpression);
            }
            else {
                return new CodeCastExpression(CodeGenHelper.GlobalType(targetUrtType), new CodeMethodInvokeExpression(CodeGenHelper.GlobalTypeExpr("System.Convert"), "ChangeType", sourceExpression, CodeGenHelper.TypeOf(CodeGenHelper.GlobalType(targetUrtType)))); 
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
    using System.Data; 
    using System.Data.SqlTypes;
    using System.Design; 
    using System.Diagnostics; 
    using System.IO;
    using System.Reflection; 


    internal sealed class CodeGenHelper {
        /// <summary> 
        /// Private contstructor to avoid class being instantiated.
        /// </summary> 
        private CodeGenHelper() { 
        }
 
        // CodeGen Helper functions :
        // -------------------- Expressions: ----------------------------
        //\\ this
        internal static CodeExpression     This() { return new CodeThisReferenceExpression();} 
        //\\ base
        internal static CodeExpression     Base() { return new CodeBaseReferenceExpression();} 
        //\\ value 
        internal static CodeExpression     Value() { return new CodePropertySetValueReferenceExpression();}
        //\\ <type> 
        internal static CodeTypeReference  Type(string type) { return new CodeTypeReference(type); }
        internal static CodeTypeReference  Type(Type type) { return new CodeTypeReference(type); }
        internal static CodeTypeReference  NullableType(Type type) {
            CodeTypeReference ctr = new CodeTypeReference(typeof(System.Nullable)); 
            ctr.Options = CodeTypeReferenceOptions.GlobalReference;
            ctr.TypeArguments.Add(CodeGenHelper.GlobalType(type)); 
 
            return ctr;
        } 
        //\\ <type>[<rank>]
        //internal static CodeTypeReference  Type(Type type, Int32 rank) { return new CodeTypeReference(type.ToString(), rank); }
        internal static CodeTypeReference  Type(string type, Int32 rank) { return new CodeTypeReference(type, rank); }
        internal static CodeTypeReference  GlobalType(Type type) { return new CodeTypeReference(type.ToString(), CodeTypeReferenceOptions.GlobalReference); } 
        internal static CodeTypeReference  GlobalType(Type type, Int32 rank) { return new CodeTypeReference(CodeGenHelper.GlobalType(type), rank); }
        internal static CodeTypeReference  GlobalType(string type) { return new CodeTypeReference(type, CodeTypeReferenceOptions.GlobalReference); } 
        //\\ <type> 
        internal static CodeTypeReferenceExpression TypeExpr(CodeTypeReference   type) { return new CodeTypeReferenceExpression(type); }
        internal static CodeTypeReferenceExpression GlobalTypeExpr(Type type) { return new CodeTypeReferenceExpression(GlobalType(type)); } 
        internal static CodeTypeReferenceExpression GlobalTypeExpr(string type) { return new CodeTypeReferenceExpression(GlobalType(type)); }
        //\\ GlobalGenericType
        internal static CodeTypeReference GlobalGenericType(string fullTypeName, Type itemType) {
            return GlobalGenericType(fullTypeName, CodeGenHelper.GlobalType(itemType)); 
        }
        internal static CodeTypeReference GlobalGenericType(string fullTypeName, CodeTypeReference itemType) { 
            CodeTypeReference genericTypeRef = new CodeTypeReference(fullTypeName, itemType); 
            genericTypeRef.Options = CodeTypeReferenceOptions.GlobalReference;
            return genericTypeRef; 
        }

        //\\ ((<type>)<expr>)
        internal static CodeExpression     Cast(CodeTypeReference type, CodeExpression expr) { return new CodeCastExpression(type, expr); } 
        //\\ typeof(<type>)
        internal static CodeExpression     TypeOf(CodeTypeReference type) { return new CodeTypeOfExpression(type); } 
        //\\ <exp>.field 
        internal static CodeExpression     Field(CodeExpression exp, string field) { return new CodeFieldReferenceExpression(exp, field);}
        //\\ this.field 
        internal static CodeExpression     ThisField(string field) { return new CodeFieldReferenceExpression(This(), field); }
        //\\ <exp>.property
        internal static CodeExpression     Property(CodeExpression exp, string property) { return new CodePropertyReferenceExpression(exp, property);}
        //\\ this.property 
        internal static CodeExpression     ThisProperty(string property) { return new CodePropertyReferenceExpression(This(), property); }
        //\\ argument 
        internal static CodeExpression     Argument(string argument) { return new CodeArgumentReferenceExpression(argument);} 
        //\\ variable
        internal static CodeExpression     Variable(string variable) { return new CodeVariableReferenceExpression(variable);} 
        //\\ this.eventName
        internal static CodeExpression     Event(string eventName) { return new CodeEventReferenceExpression(This(), eventName);}
        //\\ new <type>(<parameters>)
        internal static CodeExpression     New(CodeTypeReference type, CodeExpression[] parameters) { return new CodeObjectCreateExpression(type, parameters);} 
        //\\ new <type>[<size>]
        internal static CodeExpression     NewArray(CodeTypeReference type, int size) { return new CodeArrayCreateExpression(type, size); } 
        //\\ new <type> {<param1>, <param2>, ...} 
        internal static CodeExpression     NewArray(CodeTypeReference type, params CodeExpression[] initializers) { return new CodeArrayCreateExpression(type, initializers); }
        //\\ <primitive> 
        internal static CodeExpression     Primitive(object primitive) { return new CodePrimitiveExpression(primitive);}
        //\\ "<str>"
        internal static CodeExpression     Str(string str) { return Primitive(str);}
        //\\ <targetObject>.<methodName>(<parameters>) 
        internal static CodeExpression     MethodCall(CodeExpression targetObject, String methodName, CodeExpression[] parameters) {
            return new CodeMethodInvokeExpression(targetObject, methodName, parameters); 
        } 
        //\\ <targetObject>.<methodName>(<parameters>)
        internal static CodeStatement      MethodCallStm(CodeExpression targetObject, String methodName, CodeExpression[] parameters) { 
            return Stm(MethodCall(targetObject, methodName, parameters));
        }
        //\\ <targetObject>.<methodName>()
        internal static CodeExpression     MethodCall(CodeExpression targetObject, String methodName) { 
            return new CodeMethodInvokeExpression(targetObject, methodName);
        } 
        //\\ <targetObject>.<methodName>(<parameters>) 
        internal static CodeStatement     MethodCallStm(CodeExpression targetObject, String methodName) {
            return Stm(MethodCall(targetObject, methodName)); 
        }
        //\\ <targetObject>.<methodName>(par)
        internal static CodeExpression     MethodCall(CodeExpression targetObject, String methodName, CodeExpression par) {
            return new CodeMethodInvokeExpression(targetObject, methodName, new CodeExpression[] {par}); 
        }
        //\\ <targetObject>.<methodName>(par) 
        internal static CodeStatement      MethodCallStm(CodeExpression targetObject, String methodName, CodeExpression par) { 
            return Stm(MethodCall(targetObject, methodName, par));
        } 
        //\\ <targetObject>(par)
        internal static CodeExpression     DelegateCall(CodeExpression targetObject, CodeExpression par) {
            return new CodeDelegateInvokeExpression(targetObject, new CodeExpression[] {This(), par});
        } 
        //\\ <targetObject>[indices]()
        internal static CodeExpression     Indexer(CodeExpression targetObject, CodeExpression indices) {return new CodeIndexerExpression(targetObject, indices);} 
        //\\ <targetObject>[indices]() 
        internal static CodeExpression     ArrayIndexer(CodeExpression targetObject, CodeExpression indices) { return new CodeArrayIndexerExpression(targetObject, indices); }
 
        //\\ ReferenceEquals
        internal static CodeExpression ReferenceEquals(CodeExpression left, CodeExpression right) {
            return CodeGenHelper.MethodCall(
                       CodeGenHelper.GlobalTypeExpr(typeof(object)), 
                       "ReferenceEquals",
                       new CodeExpression[] { left, right } 
           ); 
        }
        //\\ ReferenceNotEquals 
        internal static CodeExpression ReferenceNotEquals(CodeExpression left, CodeExpression right) {
            return CodeGenHelper.EQ(ReferenceEquals(left, right), Primitive(false));
        }
 
        // -------------------- Binary Operators: ----------------------------
        internal static CodeBinaryOperatorExpression      BinOperator(CodeExpression left, CodeBinaryOperatorType op, CodeExpression right) { 
            return new CodeBinaryOperatorExpression(left, op, right); 
        }
        //\\ (left) != (right) 
        internal static CodeBinaryOperatorExpression      IdNotEQ(CodeExpression left, CodeExpression right) {return BinOperator(left, CodeBinaryOperatorType.IdentityInequality, right);}
        //\\ (left) != (right)
        internal static CodeBinaryOperatorExpression      IdEQ(CodeExpression left, CodeExpression right) {return BinOperator(left, CodeBinaryOperatorType.IdentityEquality, right);}
        //\\ (left) is null 
        internal static CodeBinaryOperatorExpression      IdIsNull(CodeExpression id) { return IdEQ(id, Primitive(null)); }
        //\\ (left) isnot null 
        internal static CodeBinaryOperatorExpression      IdIsNotNull(CodeExpression id) { return IdNotEQ(id, Primitive(null)); } 
        //\\ (left) == (right)
        internal static CodeBinaryOperatorExpression      EQ(CodeExpression left, CodeExpression right) {return BinOperator(left, CodeBinaryOperatorType.ValueEquality, right);} 
        //\\ (left) != (right)
        internal static CodeBinaryOperatorExpression      NotEQ(CodeExpression left, CodeExpression right) { return EQ(EQ(left, right), Primitive(false)); }
        //\\ (left) && (right)
        internal static CodeBinaryOperatorExpression      BitwiseAnd(CodeExpression left, CodeExpression right) {return BinOperator(left, CodeBinaryOperatorType.BitwiseAnd, right);} 
        //\\ (left) && (right)
        internal static CodeBinaryOperatorExpression      And(CodeExpression left, CodeExpression right) { return BinOperator(left, CodeBinaryOperatorType.BooleanAnd, right); } 
        //\\ (left) || (right) 
        internal static CodeBinaryOperatorExpression      Or(CodeExpression left, CodeExpression right) { return BinOperator(left, CodeBinaryOperatorType.BooleanOr, right); }
        //\\ (left) < (right) 
        internal static CodeBinaryOperatorExpression      Less(CodeExpression left, CodeExpression right) {return BinOperator(left, CodeBinaryOperatorType.LessThan     , right);}

        // -------------------- Statments: ----------------------------
        //\\ <expr>; 
        internal static CodeStatement      Stm(CodeExpression expr) { return new CodeExpressionStatement(expr);}
        //\\ return(<expr>); 
        internal static CodeStatement      Return(CodeExpression expr) { return new CodeMethodReturnStatement(expr);} 
        //\\ return;
        internal static CodeStatement      Return() { return new CodeMethodReturnStatement(); } 
        //\\ left = right;
        internal static CodeStatement      Assign(CodeExpression left, CodeExpression right) { return new CodeAssignStatement(left, right);}
        //\\ throw new <exception>(<arg>)
        internal static CodeStatement      Throw(CodeTypeReference exception, string arg) { 
            return new CodeThrowExceptionStatement(New(exception, new CodeExpression[] {Str(arg)}));
        } 
        //\\ throw new <exception>(<arg>, <inner>) 
        internal static CodeStatement      Throw(CodeTypeReference exception, string arg, string inner) {
            return new CodeThrowExceptionStatement(New(exception, new CodeExpression[] {Str(arg), Variable(inner)})); 
        }
        //\\ throw new <exception>(<arg>, inner)
        internal static CodeStatement      Throw(CodeTypeReference exception, string arg, CodeExpression inner) {
            return new CodeThrowExceptionStatement(New(exception, new CodeExpression[] {Str(arg), inner})); 
        }
        // ------------Comments -------------------------------- 
        internal static CodeCommentStatement Comment(string comment, bool docSummary) { 
            if (docSummary) {
                return new CodeCommentStatement("<summary>\r\n" + comment + "\r\n</summary>", docSummary); 
            }
            return new CodeCommentStatement(comment);
        }
 

        // -------------------- If: ---------------------------- 
        internal static CodeStatement If(CodeExpression cond, CodeStatement[] trueStms, CodeStatement[] falseStms) { 
            return new CodeConditionStatement(cond, trueStms, falseStms);
        } 
        internal static CodeStatement If(CodeExpression cond, CodeStatement trueStm, CodeStatement falseStm) {
            return new CodeConditionStatement(cond, new CodeStatement[] {trueStm}, new CodeStatement[] {falseStm});
        }
        internal static CodeStatement If(CodeExpression cond, CodeStatement[] trueStms ) {return new CodeConditionStatement(cond, trueStms);} 
        internal static CodeStatement If(CodeExpression cond, CodeStatement   trueStm  ) {return If(   cond, new CodeStatement[] {trueStm });}
        // -------------------- Declarations: ---------------------------- 
        internal static CodeMemberField  FieldDecl(CodeTypeReference type, String name) { return new CodeMemberField(type, name); } 

        internal static CodeMemberField  FieldDecl(CodeTypeReference type, String name, CodeExpression initExpr) { 
            CodeMemberField field = new CodeMemberField(type, name);
            field.InitExpression = initExpr;

            return field; 
        }
 
        internal static CodeTypeDeclaration Class(string name, bool isPartial, TypeAttributes typeAttributes) { 
            CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(name);
            typeDeclaration.IsPartial = isPartial; 
            typeDeclaration.TypeAttributes = typeAttributes;

            CodeAttributeDeclaration generatedCodeAttribute = new CodeAttributeDeclaration(
                CodeGenHelper.GlobalType(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute)), 
                new CodeAttributeArgument(Str(typeof(System.Data.Design.TypedDataSetGenerator).FullName)),
                new CodeAttributeArgument(Str(ThisAssembly.Version))); 
            typeDeclaration.CustomAttributes.Add(generatedCodeAttribute); 

            return typeDeclaration; 
        }

        internal static CodeConstructor Constructor(MemberAttributes attributes) {
            CodeConstructor constructor = new CodeConstructor(); 
            constructor.Attributes = attributes;
            constructor.CustomAttributes.Add(AttributeDecl(typeof(System.Diagnostics.DebuggerNonUserCodeAttribute).FullName)); 
 
            return constructor;
        } 

        internal static CodeMemberMethod MethodDecl(CodeTypeReference type, String name, MemberAttributes attributes) {
            CodeMemberMethod method = new CodeMemberMethod();
            method.ReturnType = type; 
            method.Name       = name;
            method.Attributes = attributes; 
            method.CustomAttributes.Add(AttributeDecl(typeof(System.Diagnostics.DebuggerNonUserCodeAttribute).FullName)); 

            return method; 
        }

        internal static CodeMemberProperty PropertyDecl(CodeTypeReference type, String name, MemberAttributes attributes) {
            CodeMemberProperty property = new CodeMemberProperty(); 
            property.Type       = type;
            property.Name       = name; 
            property.Attributes = attributes; 
            property.CustomAttributes.Add(AttributeDecl(typeof(System.Diagnostics.DebuggerNonUserCodeAttribute).FullName));
 
            return property;
        }

        internal static CodeStatement VariableDecl(CodeTypeReference type, String name) { return new CodeVariableDeclarationStatement(type, name); } 
        internal static CodeStatement VariableDecl(CodeTypeReference type, String name, CodeExpression initExpr) { return new CodeVariableDeclarationStatement(type, name, initExpr); }
 
        internal static CodeStatement ForLoop(CodeStatement initStmt, CodeExpression testExpression, CodeStatement incrementStmt, CodeStatement[] statements) { 
            return new CodeIterationStatement(
                initStmt, 
                testExpression,
                incrementStmt,
                statements
                ); 
        }
 
        internal static CodeMemberEvent EventDecl(string type, String name) { 
            CodeMemberEvent anEvent = new CodeMemberEvent();
            anEvent.Name       = name; 
            anEvent.Type       = Type(type);
            anEvent.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            return anEvent; 
        }
 
        internal static CodeParameterDeclarationExpression     ParameterDecl(CodeTypeReference type, string name) { return new CodeParameterDeclarationExpression(type, name); } 
        internal static CodeAttributeDeclaration AttributeDecl(string name) {
            return new CodeAttributeDeclaration(GlobalType(name)); 
        }
        internal static CodeAttributeDeclaration AttributeDecl(string name, CodeExpression value) {
            return new CodeAttributeDeclaration(GlobalType(name), new CodeAttributeArgument[] { new CodeAttributeArgument(value) });
        } 
        internal static CodeAttributeDeclaration AttributeDecl(string name, CodeExpression value1, CodeExpression value2) {
            return new CodeAttributeDeclaration(GlobalType(name), new CodeAttributeArgument[] { new CodeAttributeArgument(value1), new CodeAttributeArgument(value2)}); 
        } 
        // -------------------- Try/Catch ---------------------------
        //\\ try {<tryStmnt>} <catchClause> 
        internal static CodeStatement      Try(CodeStatement tryStmnt, CodeCatchClause catchClause) {
            return new CodeTryCatchFinallyStatement(
                new CodeStatement[] {tryStmnt},
                new CodeCatchClause[] {catchClause} 
                );
        } 
        //\\ try {<tryStmnt>;<tryStmnt>;...} <catchClause>; <catchClause>; ... Finally {<finallyStmt>;<finallyStmt>;...} 
        internal static CodeStatement      Try(CodeStatement[] tryStmnts, CodeCatchClause[] catchClauses, CodeStatement[] finallyStmnts) {
            return new CodeTryCatchFinallyStatement( 
                tryStmnts,
                catchClauses,
                finallyStmnts
                ); 
        }
 
        //\\ catch(<type> <name>) {<catchStmnt>} 
        internal static CodeCatchClause Catch(CodeTypeReference type, string name, CodeStatement catchStmnt) {
            CodeCatchClause ccc = new CodeCatchClause(); 
            ccc.CatchExceptionType = type;
            ccc.LocalName = name;
            if (catchStmnt != null) {
                ccc.Statements.Add(catchStmnt); 
            }
            return ccc; 
        } 

        internal static FieldDirection ParameterDirectionToFieldDirection(ParameterDirection paramDirection) { 
            FieldDirection fieldDir = FieldDirection.In;
            switch (paramDirection) {
                case(ParameterDirection.Output) :
                    fieldDir = FieldDirection.Out; 
                    break;
                case(ParameterDirection.Input) : 
                    fieldDir = FieldDirection.In; 
                    break;
                case(ParameterDirection.InputOutput) : 
                    fieldDir = FieldDirection.Ref;
                    break;
                case(ParameterDirection.ReturnValue) :
                    throw new InternalException("Can't map from ParameterDirection.ReturnValue to FieldDirection."); 
                default:
                    throw new InternalException("Unknown ParameterDirection."); 
            } 

            return fieldDir; 
        }

        internal static CodeExpression GenerateDbNullCheck(CodeExpression returnParam) {
            return CodeGenHelper.Or( 
                CodeGenHelper.IdEQ(
                    returnParam, 
                    CodeGenHelper.Primitive(null) 
                ),
                CodeGenHelper.IdEQ( 
                    CodeGenHelper.MethodCall(
                        returnParam,
                        "GetType"
                    ), 
                    CodeGenHelper.TypeOf(CodeGenHelper.GlobalType(typeof(System.DBNull)))
                ) 
            ); 
        }
 
        internal static CodeExpression GenerateNullExpression(Type returnType) {
            if(IsSqlType(returnType)) {
                return CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(returnType), "Null");
            } 
            else {
                // 
 
                if(returnType == typeof(object)) {
                    return CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.DBNull)), "Value"); 
                }
                else if (!returnType.IsValueType){
                    return CodeGenHelper.Primitive(null);
                } 
                else {
                    return null; 
                } 
            }
        } 

        internal static CodeExpression GenerateConvertExpression(CodeExpression sourceExpression, Type sourceType, Type targetType) {
            if (sourceType == targetType) {
                return sourceExpression; 
            }
 
            if (IsSqlType(sourceType)) { 
                if (IsSqlType(targetType)) {
                    throw new InternalException("Cannot perform the conversion between 2 SqlTypes."); 
                }
                else {
                    // Source is SqlType but not target.
                    PropertyInfo valuePropertyInfo = sourceType.GetProperty("Value"); 
                    if (valuePropertyInfo == null) throw new InternalException("Type does not expose a 'Value' property.");
 
                    Type equivalentUrtType = valuePropertyInfo.PropertyType; 

                    CodeExpression valueExpression = new CodePropertyReferenceExpression(sourceExpression, "Value"); 
                    return GenerateUrtConvertExpression(valueExpression, equivalentUrtType, targetType);
                }
            }
            else { 
                if (IsSqlType(targetType)) {
                    // Source is NOT SqlType but target is. 
                    PropertyInfo valuePropertyInfo = targetType.GetProperty("Value"); 
                    Type equivalentUrtType = valuePropertyInfo.PropertyType;
 
                    CodeExpression urtConvertExpression = GenerateUrtConvertExpression(sourceExpression, sourceType, equivalentUrtType);

                    return new CodeObjectCreateExpression(targetType, urtConvertExpression);
                } 
                else {
                    // Neither Source Nor Target are SqlType. 
                    return GenerateUrtConvertExpression(sourceExpression, sourceType, targetType); 
                }
            } 
        }

        internal static string GetTypeName(System.CodeDom.Compiler.CodeDomProvider codeProvider, string string1, string string2) {
            string activatorTypeName = codeProvider.GetTypeOutput(CodeGenHelper.Type(typeof(System.Activator))); 

            string typeSeparator = activatorTypeName.Replace("System", "").Replace("Activator", ""); 
 
            return string1 + typeSeparator + string2;
        } 

        internal static bool SupportsMultipleNamespaces(System.CodeDom.Compiler.CodeDomProvider codeProvider) {
            string ns1Name = MemberNameValidator.GenerateIdName("TestNs1", codeProvider, false /*useSuffix*/);
            string ns2Name = MemberNameValidator.GenerateIdName("TestNs2", codeProvider, false /*useSuffix*/); 
            CodeNamespace ns1 = new CodeNamespace(ns1Name);
            CodeNamespace ns2 = new CodeNamespace(ns2Name); 
 
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(ns1); 
            compileUnit.Namespaces.Add(ns2);

            StringWriter writer = new StringWriter(System.Globalization.CultureInfo.CurrentCulture);
            codeProvider.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions()); 

            string generatedCode = writer.GetStringBuilder().ToString(); 
 
            return (generatedCode.Contains(ns1Name) && generatedCode.Contains(ns2Name));
        } 

        internal static DSGeneratorProblem GenerateValueExprAndFieldInit(DesignColumn designColumn,
            object valueObj,
            object value, 
            string className,
            string fieldName, 
            out CodeExpression valueExpr, 
            out CodeExpression fieldInit)
        { 
            DataColumn column = designColumn.DataColumn;
            valueExpr = null;
            fieldInit = null;
 
            if(
                column.DataType == typeof(char)   || column.DataType == typeof(string) || 
                column.DataType == typeof(decimal)|| column.DataType == typeof(bool)   || 
                column.DataType == typeof(Single) || column.DataType == typeof(double) ||
                column.DataType == typeof(SByte)  || column.DataType == typeof(Byte)   || 
                column.DataType == typeof(Int16)  || column.DataType == typeof(UInt16) ||
                column.DataType == typeof(Int32)  || column.DataType == typeof(UInt32) ||
                column.DataType == typeof(Int64)  || column.DataType == typeof(UInt64)
            ) { // types can be presented by literal. Really this is language dependent :-( 
                valueExpr = CodeGenHelper.Primitive(valueObj);
            } 
            else { 
                valueExpr = CodeGenHelper.Field(
                    CodeGenHelper.TypeExpr(CodeGenHelper.Type(className)), 
                    fieldName
                );
                //\\ private static <ColumnType> <ColumnName>_nullValue = new <ColumnType>("<nullValue>");
                if(column.DataType == typeof(Byte[])) { 
                    fieldInit = CodeGenHelper.MethodCall(
                        CodeGenHelper.GlobalTypeExpr(typeof(System.Convert)), 
                        "FromBase64String", 
                        CodeGenHelper.Primitive(value)
                    ); 
                }
                else if(column.DataType == typeof(DateTime)) {
                    fieldInit = CodeGenHelper.MethodCall(
                        CodeGenHelper.GlobalTypeExpr(column.DataType), 
                        "Parse",
                        CodeGenHelper.Primitive(((DateTime)valueObj).ToString(System.Globalization.DateTimeFormatInfo.InvariantInfo))); 
                } 
                else if(column.DataType == typeof(TimeSpan)) {
                    fieldInit = CodeGenHelper.MethodCall( 
                        CodeGenHelper.GlobalTypeExpr(column.DataType),
                        "Parse",
                        CodeGenHelper.Primitive(valueObj.ToString()));
                }else /*object*/ { 
                    /* check that type can be constructed from this string */ {
                        ConstructorInfo constructor = column.DataType.GetConstructor(new Type[] {typeof(string)}); 
                        if(constructor == null) { 
                            return new DSGeneratorProblem(SR.GetString(SR.CG_NoCtor1, column.ColumnName, column.DataType.Name), ProblemSeverity.NonFatalError, designColumn);
                        } 
                        constructor.Invoke(new Object[] {value}); // can throw here.
                    }
                    fieldInit = CodeGenHelper.New(
                        CodeGenHelper.GlobalType(column.DataType), 
                        new CodeExpression[] {CodeGenHelper.Primitive(value)}
                    ); 
                } 
            }
 
            return null;
        }

        internal static string GetLanguageExtension(CodeDomProvider codeProvider) { 
            if (codeProvider == null) {
                return string.Empty; 
            } 
            else {
                string extension = "." + codeProvider.FileExtension; 
                if (extension.StartsWith("..", StringComparison.Ordinal)) {
                    extension = extension.Substring(1);
                }
 
                return extension;
            } 
        } 

        internal static bool IsGeneratingJSharpCode(CodeDomProvider codeProvider) { 
            return StringUtil.EqualValue(GetLanguageExtension(codeProvider), ".jsl");
        }

        //internal static bool IsGeneratingCSharpCode(CodeDomProvider codeProvider) { 
        //    return StringUtil.EqualValue(GetLanguageExtension(codeProvider), ".cs");
        //} 
 

        private static bool IsSqlType(Type type){ 
            if (type == typeof(SqlBinary) ||
                type == typeof(SqlBoolean) ||
                type == typeof(SqlByte) ||
                type == typeof(SqlDateTime) || 
                type == typeof(SqlDecimal) ||
                type == typeof(SqlDouble) || 
                type == typeof(SqlGuid) || 
                type == typeof(SqlInt16) ||
                type == typeof(SqlInt32) || 
                type == typeof(SqlInt64) ||
                type == typeof(SqlMoney) ||
                type == typeof(SqlSingle) ||
                type == typeof(SqlString)) { 

                return true; 
            } else { 
                return false;
            } 
        }


        // GenerateUrtConvertExpression -- Generates appropriate code to do the conversion between 2 URT types. 
        //                                 e.g. : Convert.ToInt32( intExpression )
        private static CodeExpression GenerateUrtConvertExpression(CodeExpression sourceExpression, Type sourceUrtType, Type targetUrtType) { 
 
            if (sourceUrtType == targetUrtType) {
                return sourceExpression; 
            }

            if (sourceUrtType == typeof(System.Object)) {
                // in this case we just generate a cast expression 
                return CodeGenHelper.Cast(CodeGenHelper.GlobalType(targetUrtType), sourceExpression);
            } 
 
            // First see if System.Convert can do the conversion directly using the ToXXX methods.
            if (ConversionHelper.CanConvert(sourceUrtType, targetUrtType)) { 
                return new CodeMethodInvokeExpression(CodeGenHelper.GlobalTypeExpr("System.Convert"), ConversionHelper.GetConversionMethodName(sourceUrtType, targetUrtType), sourceExpression);
            }
            else {
                return new CodeCastExpression(CodeGenHelper.GlobalType(targetUrtType), new CodeMethodInvokeExpression(CodeGenHelper.GlobalTypeExpr("System.Convert"), "ChangeType", sourceExpression, CodeGenHelper.TypeOf(CodeGenHelper.GlobalType(targetUrtType)))); 
            }
        } 
    } 

} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
