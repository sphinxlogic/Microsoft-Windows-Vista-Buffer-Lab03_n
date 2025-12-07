//------------------------------------------------------------------------------ 
// <copyright company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
namespace System.Data.Design { 
    using System; 
    using System.CodeDom;
    using System.Collections; 
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common; 
    using System.Data.SqlClient;
    using System.Design; 
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection; 
    using TAMNameHandler = TableAdapterManagerNameHandler;

    internal sealed class TableAdapterManagerMethodGenerator {
        private TypedDataSourceCodeGenerator codeGenerator; 
        private DesignDataSource dataSource;
        private CodeTypeDeclaration dataSourceType; 
        private TableAdapterManagerNameHandler nameHandler; 
        // This is the editor attribute for the TableAdapter property in the TableAdapterManager
        private const string adapterPropertyEditor = "Microsoft.VSDesigner.DataSource.Design.TableAdapterManagerPropertyEditor"; 


        internal TableAdapterManagerMethodGenerator(TypedDataSourceCodeGenerator codeGenerator, DesignDataSource dataSource, CodeTypeDeclaration dataSourceType) {
            Debug.Assert(codeGenerator != null); 
            Debug.Assert(dataSource != null);
            Debug.Assert(dataSourceType != null); 
 
            this.codeGenerator = codeGenerator;
            this.dataSource = dataSource; 
            this.dataSourceType = dataSourceType;
            this.nameHandler = new TableAdapterManagerNameHandler(codeGenerator.CodeProvider);
        }
 
        internal void AddEverything(CodeTypeDeclaration dataComponentClass) {
            if (dataComponentClass == null) { 
                throw new InternalException("dataComponent CodeTypeDeclaration should not be null."); 
            }
            AddUpdateOrderMembers(dataComponentClass); 
            AddAdapterMembers(dataComponentClass);
            // AddBackupDataSetMembers(dataComponentClass);
            this.AddVariableAndProperty(dataComponentClass,
                    MemberAttributes.Public | MemberAttributes.Final, 
                    CodeGenHelper.GlobalType(typeof(bool)),
                    TAMNameHandler.BackupDataSetBeforeUpdateProperty, 
                    TAMNameHandler.BackupDataSetBeforeUpdateVar, 
                    false);
 
            AddConnectionMembers(dataComponentClass);
            AddTableAdapterCountMembers(dataComponentClass);
            AddUpdateAll(dataComponentClass);
            AddSortSelfRefRows(dataComponentClass); 
            AddSelfRefComparer(dataComponentClass);
            AddMatchTableAdapterConnection(dataComponentClass); 
        } 

        /// <summary> 
        ///Public Enum UpdateOrderOption
        ///    InsertUpdateDelete = 1
        ///    UpdateInsertDelete = 2
        ///End Enum 
        /// </summary>
        /// <param name="dataComponentClass"></param> 
        private void AddUpdateOrderMembers(CodeTypeDeclaration dataComponentClass) { 

            CodeTypeDeclaration updateOrderEnum = CodeGenHelper.Class( 
                TAMNameHandler.UpdateOrderOptionEnum, false, TypeAttributes.NestedPublic
            );
            updateOrderEnum.IsEnum = true;
            updateOrderEnum.Comments.Add(CodeGenHelper.Comment("Update Order Option", true)); 

            CodeMemberField insertUpdateDeleteEnum = CodeGenHelper.FieldDecl(CodeGenHelper.Type(typeof(int)), TAMNameHandler.UpdateOrderOptionEnumIUD, CodeGenHelper.Primitive(0)); 
            updateOrderEnum.Members.Add(insertUpdateDeleteEnum); 
            CodeMemberField updateInsertDeleteEnum = CodeGenHelper.FieldDecl(CodeGenHelper.Type(typeof(int)), TAMNameHandler.UpdateOrderOptionEnumUID, CodeGenHelper.Primitive(1));
            updateOrderEnum.Members.Add(updateInsertDeleteEnum); 

            dataComponentClass.Members.Add(updateOrderEnum);
            // undone throw excpetion for invalid argument
            this.AddVariableAndProperty(dataComponentClass, MemberAttributes.Public | MemberAttributes.Final, 
                CodeGenHelper.Type(TAMNameHandler.UpdateOrderOptionEnum),
                TAMNameHandler.UpdateOrderOptionProperty, 
                TAMNameHandler.UpdateOrderOptionVar, 
                false);
        } 

        /// <summary>
        /// Add TableAdapter properties
        /// Example: 
        ///  private CustomersTableAdapter _customersAdapter;
        ///  [System.Diagnostics.DebuggerNonUserCodeAttribute()] 
        ///  public CustomersTableAdapter CustomersAdapter { 
        ///    get {
        ///        return _customersAdapter; 
        ///    }
        ///    set {
        ///        _customersAdapter = value;
        ///    } 
        ///  }
        /// </summary> 
        /// <param name="dataComponentClass"></param> 
        private void AddAdapterMembers(CodeTypeDeclaration dataComponentClass) {
            foreach (DesignTable table in dataSource.DesignTables) { 
                if (!this.CanAddTableAdapter(table)) {
                    continue;
                }
                // Variable 
                table.PropertyCache.TAMAdapterPropName = nameHandler.GetTableAdapterPropName(table.GeneratorDataComponentClassName);
                table.PropertyCache.TAMAdapterVarName = nameHandler.GetTableAdapterVarName(table.PropertyCache.TAMAdapterPropName); 
                string adapterVariableName = table.PropertyCache.TAMAdapterVarName; 

                CodeMemberField adapterVariable = CodeGenHelper.FieldDecl(CodeGenHelper.Type(table.GeneratorDataComponentClassName), adapterVariableName); 
                dataComponentClass.Members.Add(adapterVariable);

                // Property
                CodeMemberProperty adapterProperty = CodeGenHelper.PropertyDecl(CodeGenHelper.Type(table.GeneratorDataComponentClassName), table.PropertyCache.TAMAdapterPropName, MemberAttributes.Public | MemberAttributes.Final); 

                //EditorAttribute 
                adapterProperty.CustomAttributes.Add( 
                    CodeGenHelper.AttributeDecl(
                        "System.ComponentModel.EditorAttribute", 
                        CodeGenHelper.Str(adapterPropertyEditor + ", " + AssemblyRef.MicrosoftVSDesigner),
                        CodeGenHelper.Str("System.Drawing.Design.UITypeEditor")
                    )
                ); 

                adapterProperty.GetStatements.Add( 
                    CodeGenHelper.Return( 
                        CodeGenHelper.ThisField(adapterVariableName)
                    ) 
                );

                // If TAM has only one adapter and it is one we are going to set,
                // there is no connection conflict issue. 
                //
                //\\ if (this.TableAdapterInstanceCount == 1 && this._customerTA != null){ 
                //\\    Set ... 
                //\\    return
                //\\ } 
                adapterProperty.SetStatements.Add(
                    CodeGenHelper.If(
                        CodeGenHelper.And(
                            CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(adapterVariableName)), 
                            CodeGenHelper.EQ(
                                CodeGenHelper.ThisProperty(TAMNameHandler.TableAdapterInstanceCountProperty), 
                                CodeGenHelper.Primitive(1) 
                            )
                        ), 
                        new CodeStatement[]{
                            CodeGenHelper.Assign(CodeGenHelper.ThisField(adapterVariableName),
                                                 CodeGenHelper.Argument("value")
                            ), 
                            CodeGenHelper.Return()
                        } 
                    ) 
                );
                //\\ If value != null && !MatchConnection(value.Connection) 
                //\\      throw argument exception
                //"The connection of " + table.GeneratorDataComponentClassName + " does not match that of the TableAdapterManager";
                //string errorMsg = SR.GetString(SR.DD_E_TableAdapterConnectionInvalid, table.GeneratorDataComponentClassName);
                // Note, we decided not to add a new resource string here but just throw an argument exception 
                adapterProperty.SetStatements.Add(
                    CodeGenHelper.If( 
                        CodeGenHelper.And( 
                            CodeGenHelper.IdIsNotNull(CodeGenHelper.Variable("value")),
                            CodeGenHelper.EQ( 
                                CodeGenHelper.MethodCall(CodeGenHelper.This(), TAMNameHandler.MatchTAConnectionMethod, CodeGenHelper.Property(CodeGenHelper.Variable("value"), "Connection")),
                                CodeGenHelper.Primitive(false)
                            )
                        ), 
                        new CodeStatement[]{
                            new CodeThrowExceptionStatement(CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(ArgumentException)), new CodeExpression[] {CodeGenHelper.Str(SR.GetString(SR.CG_TableAdapterManagerNeedsSameConnString))})) 
                        } 
                    )
                ); 

                ///  this.TA = value
                adapterProperty.SetStatements.Add(
                    CodeGenHelper.Assign(CodeGenHelper.ThisField(adapterVariableName), 
                                         CodeGenHelper.Argument("value")
                    ) 
                ); 
                dataComponentClass.Members.Add(adapterProperty);
            } 
        }

        /// <summary>
        /// Connection member Example: 
        /// Private _connection As IDbConnection
        /// Friend Property Connection() As IDbConnection 
        ///     Get 
        ///         If (_connection IsNot Nothing) Then
        ///           Return _connection 
        ///         End If
        ///         If (Me._customersTableAdapter IsNot Nothing AndAlso Me._customersTableAdapter.Connection IsNot Nothing) Then
        ///           Return Me._customersTableAdapter.Connection
        ///         End If 
        ///         If (Me._ordersTableAdapter IsNot Nothing AndAlso Me._ordersTableAdapter.Connection IsNot Nothing) Then
        ///             Return Me._ordersTableAdapter.Connection 
        ///         End If 
        ///         Return Nothing
        ///     End Get 
        ///     Set(ByVal value As IDbConnection)
        ///         _connection = value
        ///     End Set
        /// End Property 
        /// <param name="dataComponentClass"></param>
        private void AddConnectionMembers(CodeTypeDeclaration dataComponentClass) { 
            string connectionVariableName = TAMNameHandler.ConnectionVar; 
            CodeMemberField connectionVariable = CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(IDbConnection)), connectionVariableName);
            dataComponentClass.Members.Add(connectionVariable); 

            // Property
            CodeMemberProperty property = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(typeof(IDbConnection)), TAMNameHandler.ConnectionProperty, MemberAttributes.Public | MemberAttributes.Final);
            property.CustomAttributes.Add( 
                CodeGenHelper.AttributeDecl("System.ComponentModel.Browsable", CodeGenHelper.Primitive(false))
            ); 
 
            //\\ If (_connection IsNot Nothing) Then
            //\\    Return _connection 
            //\\ End If
            property.GetStatements.Add(
                CodeGenHelper.If(CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(connectionVariableName)),
                    CodeGenHelper.Return(CodeGenHelper.ThisField(connectionVariableName)) 
                )
            ); 
 
            foreach (DesignTable table in dataSource.DesignTables) {
                if (!this.CanAddTableAdapter(table)) { 
                    continue;
                }

                //\\  If (Me._customersTableAdapter IsNot Nothing AndAlso Me._customersTableAdapter.Connection IsNot Nothing) Then 
                //\\     Return Me._customersTableAdapter.Connection
                //\\  End If 
                string adapterVariableName = table.PropertyCache.TAMAdapterVarName; 
                property.GetStatements.Add(
                    CodeGenHelper.If( 
                        CodeGenHelper.And(
                            CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(adapterVariableName)),
                            CodeGenHelper.IdIsNotNull(CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Connection"))
                        ), 
                        CodeGenHelper.Return(
                            CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Connection") 
                        ) 
                    )
                ); 
            }

            //\\    Return null
            property.GetStatements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Primitive(null))
            ); 
 
            //\\ Set(ByVal value As IDbConnection)
            //\\    _connection = value 
            //\\ End Set
            property.SetStatements.Add(
                CodeGenHelper.Assign(CodeGenHelper.ThisField(connectionVariableName),
                                     CodeGenHelper.Argument("value") 
                )
            ); 
 
            dataComponentClass.Members.Add(property);
        } 

        /// <summary>
        /// Create a TableAdapterInstanceCount property
        /// Example: 
        /// Public Property TableAdapterInstanceCount() As integer
        ///     Get 
        ///         count = 0; 
        ///         If (Me._customersTableAdapter IsNot Nothing ) Then
        ///             count += 1 
        ///         End If
        ///         If (Me._ordersTableAdapter IsNot Nothing) Then
        ///             count += 1
        ///         End If 
        ///         Return count
        ///     End Get 
        /// End Property 
        /// <param name="dataComponentClass"></param>
        private void AddTableAdapterCountMembers(CodeTypeDeclaration dataComponentClass) { 
            string countVariableName = "count";
            CodeExpression countVariable = CodeGenHelper.Variable(countVariableName);

            CodeMemberProperty property = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(typeof(int)), TAMNameHandler.TableAdapterInstanceCountProperty, MemberAttributes.Public | MemberAttributes.Final); 
            property.CustomAttributes.Add(
                CodeGenHelper.AttributeDecl("System.ComponentModel.Browsable", CodeGenHelper.Primitive(false)) 
            ); 

            //\\ count = 0; 
            property.GetStatements.Add(
                CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(int)), countVariableName, CodeGenHelper.Primitive(0))
            );
 
            foreach (DesignTable table in dataSource.DesignTables) {
                if (!this.CanAddTableAdapter(table)) { 
                    continue; 
                }
                //\\If (Me._customersTableAdapter IsNot Nothing ) Then 
                //\\    count += 1
                //\\End If
                string adapterVariableName = table.PropertyCache.TAMAdapterVarName;
                property.GetStatements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(adapterVariableName)) 
                        , 
                        CodeGenHelper.Assign(countVariable, CodeGenHelper.BinOperator(countVariable, CodeBinaryOperatorType.Add, CodeGenHelper.Primitive(1)))
                    ) 
                );
            }

            //\\    Return count 
            property.GetStatements.Add(
                CodeGenHelper.Return(countVariable) 
            ); 
            dataComponentClass.Members.Add(property);
        } 

        /// <summary>
        /// Add SortSelfRefRows methods. Code Example:
        /// protected virtual void SortSelfReferencedRows(System.Data.DataRow[] rows, DataRelation relation, bool childFirst) { 
        ///     System.Array.Sort<DataRow>(rows, new SelfReferenceComparer(relation, childFirst));
        /// } 
        /// </summary> 
        /// <param name="dataComponentClass"></param>
        private void AddSortSelfRefRows(CodeTypeDeclaration dataComponentClass) { 
            string rowsStr = "rows";
            string relationStr = "relation";
            string childFirstStr = "childFirst";
 
            CodeMemberMethod method =
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), 
                TAMNameHandler.SortSelfRefRowsMethod, 
                MemberAttributes.Family
            ); 

            method.Parameters.AddRange(
                new CodeParameterDeclarationExpression[]{
                    CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRow), 1), rowsStr), 
                    CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRelation)), relationStr),
                    CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(bool)), childFirstStr) 
                } 
            );
 
            CodeMethodReferenceExpression sortMethodRef = new CodeMethodReferenceExpression(CodeGenHelper.GlobalTypeExpr("System.Array"), "Sort", CodeGenHelper.GlobalType(typeof(DataRow)));
            CodeMethodInvokeExpression sortMethodExpr =
                new CodeMethodInvokeExpression(
                    sortMethodRef, 
                    new CodeExpression[]{
                        CodeGenHelper.Argument(rowsStr), 
                        CodeGenHelper.New( 
                            CodeGenHelper.Type(TAMNameHandler.SelfRefComparerClass),
                            new CodeExpression[]{ 
                                CodeGenHelper.Argument(relationStr),
                                CodeGenHelper.Argument(childFirstStr)
                            }
                        ) 
                   }
                ); 
 
            //\\ System.Array.Sort<DataRow>(rows, new SelfReferenceComparer(relation, childFirst));
            method.Statements.Add(CodeGenHelper.Stm(sortMethodExpr)); 
            dataComponentClass.Members.Add(method);
        }

        /// <summary> 
        /// Add SelfReferenceComparer class. Code Example:
        /// private class SelfReferenceComparer : System.Collections.Generic.IComparer<DataRow> { 
        ///     private readonly DataRelation m_relation; 
        ///     private readonly int m_childFirst;
        ///     internal SelfReferenceComparer(DataRelation relation, bool childFirst) { 
        ///         this.m_relation = relation;
        ///         this.m_childFirst = childFirst ? -1 : 1;
        ///     }
        ///     // return (0 if row1 == row2), (-1 if row1 < row2) or (1 if row1 > row2) 
        ///     int System.Collections.Generic.IComparer<DataRow>.Compare(DataRow row1, DataRow row2) {
        ///     if (object.ReferenceEquals(row1, row2)) { 
        ///         return 0; // either row1 && row2 are same instance or null 
        ///     }
        ///     if (null == row1) { 
        ///         return -1; // null row1 is < non-null row2
        ///     }
        ///     if (null == row2) {
        ///         return 1; // non-null row1 > null row2 
        ///     }
        ///     // Is row1 the child or grandchild of row2 
        ///     if (this.IsChildAndParent(row1, row2)) { 
        ///         return this._childFirst;
        ///     } 
        ///     // Is row2 the child or grandchild of row1
        ///     if (this.IsChildAndParent(row2, row1)) {
        ///         return (-1 * this._childFirst);
        ///     } 
        ///     return 0;
        /// } 
        /// </summary> 
        /// <param name="dataComponentClass"></param>
        private void AddSelfRefComparer(CodeTypeDeclaration dataComponentClass) { 
            string relationVarStr = "_relation";
            string childFirstVarStr = "_childFirst";

            //\\private class SelfReferenceComparer : System.Collections.Generic.IComparer<DataRow> { 
            CodeTypeDeclaration codeType = CodeGenHelper.Class(
                TAMNameHandler.SelfRefComparerClass, false, TypeAttributes.NestedPrivate 
            ); 
            CodeTypeReference icomparerInterface = CodeGenHelper.GlobalGenericType(
                "System.Collections.Generic.IComparer", typeof(DataRow) 
            );
            // To generate a class in Visual Basic that does not inherit from a base type,
            // but that does implement one or more interfaces, you must include Object as the first item in
            // the BaseTypes collection. from <http://msdn2.microsoft.com/en-us/library/system.codedom.codetypedeclaration.basetypes.aspx> 
            codeType.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(object)));
            codeType.BaseTypes.Add(icomparerInterface); 
 
            codeType.Comments.Add(CodeGenHelper.Comment("Used to sort self-referenced table's rows", true));
 
            dataComponentClass.Members.Add(codeType);

            //\\ private DataRelation m_relation;
            //\\ private int m_childFirst; 
            codeType.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(DataRelation)), relationVarStr));
            codeType.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(int)), childFirstVarStr)); 
 
            //\\ internal SelfReferenceComparer(DataRelation relation, bool childFirst) {
            //\\    this.m_relation = relation; 
            //\\    this.m_childFirst = childFirst ? -1 : 1;
            //\\ }
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Assembly);
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRelation)), "relation")); 
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(bool)), "childFirst"));
            constructor.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.ThisField(relationVarStr), CodeGenHelper.Argument("relation"))); 
            constructor.Statements.Add(CodeGenHelper.If( 
                CodeGenHelper.Argument("childFirst"),
                CodeGenHelper.Assign(CodeGenHelper.ThisField(childFirstVarStr), CodeGenHelper.Primitive(-1)), 
                CodeGenHelper.Assign(CodeGenHelper.ThisField(childFirstVarStr), CodeGenHelper.Primitive(1))
            ));
            codeType.Members.Add(constructor);
 
            // ---- isChildAndParentMethod -------------
 
            //\\private bool IsChildAndParent(global::System.Data.DataRow child, global::System.Data.DataRow parent){ 
            //\\    System.Data.DataRow newParent = child.GetParentRow(this._relation);
            //\\    while (newParent != null && newParent != child && newParent != parent){ 
            //\\        newParent = newParent.GetParentRow(this._relation);
            //\\    }
            //\\    if (newParent == null){
            //\\        newParent = child.GetParentRow(this._relation, System.Data.DataRowVersion.Original  ); 
            //\\        while (newParent != null && newParent != child && newParent != parent){
            //\\            newParent = newParent.GetParentRow(this._relation, System.Data.DataRowVersion.Original  ); 
            //\\        } 
            //\\    }
            //\\    if (object.ReferenceEquals(newParent, parent)){ 
            //\\        return true;
            //\\    }
            //\\    return false;
            //\\} 
            string childStr = "child";
            string parentStr = "parent"; 
            string newParentStr = "newParent"; 
            string isChildAndParentStr = "IsChildAndParent";
            CodeMemberMethod isChildAndParentMethod = CodeGenHelper.MethodDecl( 
                CodeGenHelper.GlobalType(typeof(bool)),
                isChildAndParentStr,
                MemberAttributes.Private
            ); 
            isChildAndParentMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRow)), childStr));
            isChildAndParentMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRow)), parentStr)); 
 
            isChildAndParentMethod.Statements.Add(
                CodeGenHelper.MethodCallStm( 
                    CodeGenHelper.GlobalTypeExpr(typeof(System.Diagnostics.Debug)),
                    "Assert",
                    CodeGenHelper.IdIsNotNull(CodeGenHelper.Argument(childStr))
                ) 
            );
            isChildAndParentMethod.Statements.Add( 
                CodeGenHelper.MethodCallStm( 
                    CodeGenHelper.GlobalTypeExpr(typeof(System.Diagnostics.Debug)),
                    "Assert", 
                    CodeGenHelper.IdIsNotNull(CodeGenHelper.Argument(parentStr))
                )
            );
 
            //\\System.Data.DataRow newParent = child.GetParentRow(this._relation);
            isChildAndParentMethod.Statements.Add( 
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(DataRow)),
                    newParentStr, 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Argument(childStr),
                        "GetParentRow",
                        new CodeExpression[] { 
                            CodeGenHelper.ThisField(relationVarStr),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(DataRowVersion)),"Default") 
                        } 
                   )
                ) 
            );
            //\\    while (newParent != null && newParent != child && newParent != parent){
            //\\        newParent = newParent.GetParentRow(this._relation);
            //\\    } 
            CodeIterationStatement whileStatement = new CodeIterationStatement();
            whileStatement.TestExpression = CodeGenHelper.And( 
                CodeGenHelper.IdIsNotNull(CodeGenHelper.Variable(newParentStr)), 
                CodeGenHelper.And(
                    CodeGenHelper.ReferenceNotEquals(CodeGenHelper.Variable(newParentStr), CodeGenHelper.Argument(childStr)), 
                    CodeGenHelper.ReferenceNotEquals(CodeGenHelper.Variable(newParentStr), CodeGenHelper.Argument(parentStr))
                )
            );
            whileStatement.InitStatement = new CodeSnippetStatement(); 
            whileStatement.IncrementStatement = new CodeSnippetStatement();
            whileStatement.Statements.Add( 
                CodeGenHelper.Assign( 
                    CodeGenHelper.Variable(newParentStr),
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Variable(newParentStr),
                        "GetParentRow",
                        new CodeExpression[] {
                            CodeGenHelper.ThisField(relationVarStr), 
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(DataRowVersion)),"Default")
                        } 
                   ) 
                )
            ); 
            isChildAndParentMethod.Statements.Add(whileStatement);

            //\\    if (newParent == null){
            //\\        newParent = child.GetParentRow(this._relation, System.Data.DataRowVersion.Original  ); 
            //\\        while (newParent != null && newParent != child && newParent != parent){
            //\\            newParent = newParent.GetParentRow(this._relation, System.Data.DataRowVersion.Original  ); 
            //\\        } 
            //\\    }
            whileStatement = new CodeIterationStatement(); 
            whileStatement.TestExpression = CodeGenHelper.And(
                CodeGenHelper.IdIsNotNull(CodeGenHelper.Variable(newParentStr)),
                CodeGenHelper.And(
                    CodeGenHelper.ReferenceNotEquals(CodeGenHelper.Variable(newParentStr), CodeGenHelper.Argument(childStr)), 
                    CodeGenHelper.ReferenceNotEquals(CodeGenHelper.Variable(newParentStr), CodeGenHelper.Argument(parentStr))
                ) 
            ); 
            whileStatement.InitStatement = CodeGenHelper.Assign(
                    CodeGenHelper.Variable(newParentStr), 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Argument(childStr),
                        "GetParentRow",
                        new CodeExpression[] { 
                            CodeGenHelper.ThisField(relationVarStr),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(DataRowVersion)),"Original") 
                        } 
                    )
            ); 
            whileStatement.IncrementStatement = new CodeSnippetStatement();
            whileStatement.Statements.Add(CodeGenHelper.Assign(
                    CodeGenHelper.Variable(newParentStr),
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Argument(newParentStr),
                        "GetParentRow", 
                        new CodeExpression[] { 
                            CodeGenHelper.ThisField(relationVarStr),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(DataRowVersion)),"Original") 
                        }
                    )
            ));
 
            isChildAndParentMethod.Statements.Add(CodeGenHelper.If(
                CodeGenHelper.IdIsNull(CodeGenHelper.Variable(newParentStr)), 
                whileStatement 
            ));
 
            //\\    if (object.ReferenceEquals(newParent, parent)){
            //\\        return true;
            //\\    }
            //\\    return false; 
            isChildAndParentMethod.Statements.Add(CodeGenHelper.If(
                CodeGenHelper.ReferenceEquals(CodeGenHelper.Variable(newParentStr), CodeGenHelper.Argument(parentStr)), 
                CodeGenHelper.Return(CodeGenHelper.Primitive(true)) 
            ));
            isChildAndParentMethod.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Primitive(false))); 

            codeType.Members.Add(isChildAndParentMethod);

            //--------compareMethod----------------- 

            //\\  // return (0 if row1 == row2), (-1 if row1 < row2) or (1 if row1 > row2) 
            //\\  int System.Collections.Generic.IComparer<DataRow>.Compare(DataRow row1, DataRow row2) { 
            string row1Str = "row1";
            string row2Str = "row2"; 
            CodeMemberMethod compareMethod = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(int)),
                "Compare",
                 MemberAttributes.Public | MemberAttributes.Final 
            );
            compareMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRow)), row1Str)); 
            compareMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRow)), row2Str)); 
            compareMethod.ImplementationTypes.Add(icomparerInterface);
            codeType.Members.Add(compareMethod); 

            //\\ if (object.ReferenceEquals(row1, row2)) {
            //\\    return 0; // either row1 && row2 are same instance or null
            //\\ } 
            compareMethod.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.ReferenceEquals(CodeGenHelper.Argument(row1Str), CodeGenHelper.Argument(row2Str)), 
                    CodeGenHelper.Return(CodeGenHelper.Primitive(0))
                ) 
            );
            //\\ if (null == row1) {
            //\\    return -1; // null row1 is < non-null row2
            //\\ } 
            //\\ if (null == row2) {
            //\\    return 1; // non-null row1 > null row2 
            //\\ } 
            compareMethod.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdIsNull(CodeGenHelper.Argument(row1Str)),
                    CodeGenHelper.Return(CodeGenHelper.Primitive(-1))
                )
            ); 
            compareMethod.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdIsNull(CodeGenHelper.Argument(row2Str)), 
                    CodeGenHelper.Return(CodeGenHelper.Primitive(1))
                ) 
            );

            //\\ // is row1 the child of row2
            compareMethod.Statements.Add( 
                new CodeSnippetStatement() // empty line
            ); 
            compareMethod.Statements.Add( 
                new CodeCommentStatement("Is row1 the child or grandchild of row2")
            ); 
            //\\if (this.IsChildAndParent(row1, row2)) {
            //\\    return this._childFirst;
            //\\}
            compareMethod.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.This(), 
                        isChildAndParentStr,
                        new CodeExpression[]{ 
                            CodeGenHelper.Argument(row1Str),
                            CodeGenHelper.Argument(row2Str)
                        }
                    ), 
                    CodeGenHelper.Return(CodeGenHelper.ThisField(childFirstVarStr))
                ) 
            ); 
            //\\ // is row2 the child of row1
            compareMethod.Statements.Add( 
                new CodeSnippetStatement() // empty line
            );
            compareMethod.Statements.Add(
                new CodeCommentStatement("Is row2 the child or grandchild of row1") 
            );
            //\\ if (row2.GetParentRow(m_relation) == row1) { 
            //\\     -return this.m_childFirst; 
            //\\ }
            compareMethod.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.This(),
                        isChildAndParentStr, 
                        new CodeExpression[]{
                            CodeGenHelper.Argument(row2Str), 
                            CodeGenHelper.Argument(row1Str) 
                        }
                    ), 
                    CodeGenHelper.Return(CodeGenHelper.BinOperator(CodeGenHelper.Primitive(-1), CodeBinaryOperatorType.Multiply, CodeGenHelper.ThisField(childFirstVarStr)))
                )
            );
            //\\ return 0; 
            compareMethod.Statements.Add(
                CodeGenHelper.Return(CodeGenHelper.Primitive(0)) 
            ); 
        }
 
        /// <summary>
        /// Add MatchTableAdapterConnection method. Code Example:
        /// virtual protected MatchTableAdapterConnection(IDbConnection inputConnection)  {
        ///   if (this._conection != null){ 
        ///       return true;
        ///   } 
        ///   if (this.Connection == null || inputConnection == null) 
        ///     return true
        ///   } 
        /// }
        /// </summary>
        /// <param name="dataComponentClass"></param>
        private void AddMatchTableAdapterConnection(CodeTypeDeclaration dataComponentClass) { 
            string inputConnStr = "inputConnection";
 
            CodeMemberMethod method = 
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(bool)),
                TAMNameHandler.MatchTAConnectionMethod, 
                MemberAttributes.Family
            );
            CodeTypeReference connTypeRef = CodeGenHelper.GlobalType(typeof(IDbConnection));
            CodeParameterDeclarationExpression dataSetPara = CodeGenHelper.ParameterDecl(connTypeRef, inputConnStr); 
            method.Parameters.Add(dataSetPara);
 
            //\\if (this._connection != null) 
            //\\    return true
            method.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(TAMNameHandler.ConnectionVar)),
                    CodeGenHelper.Return(CodeGenHelper.Primitive(true))
                ) 
            );
            //\\if (this.Connection == null || inputConnection == null) 
            //\\    return true 
            method.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.Or(
                        CodeGenHelper.IdIsNull(CodeGenHelper.ThisProperty(TAMNameHandler.ConnectionProperty)),
                        CodeGenHelper.IdIsNull(CodeGenHelper.Argument(inputConnStr))
                    ), 
                    CodeGenHelper.Return(CodeGenHelper.Primitive(true))
                ) 
            ); 
            //\\ if (string.Equals(a, b, StringComparison.Ordinal)
            method.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.GlobalTypeExpr(typeof(string)),
                        "Equals", 
                        new CodeExpression[]{
                            CodeGenHelper.Property( 
                                    CodeGenHelper.ThisProperty(TAMNameHandler.ConnectionProperty), 
                                    "ConnectionString"
                            ), 
                            CodeGenHelper.Property(CodeGenHelper.Argument(inputConnStr),"ConnectionString"),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.StringComparison)), "Ordinal")
                        }
                    ), 
                    CodeGenHelper.Return(CodeGenHelper.Primitive(true))
                ) 
            ); 

            method.Statements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Primitive(false))
            );

            dataComponentClass.Members.Add(method); 
        }
 
        /// <summary> 
        /// The UpdateAll method
        /// </summary> 
        /// <param name="dataComponentClass"></param>
        private void AddUpdateAll(CodeTypeDeclaration dataComponentClass) {
            string dataSetStr = "dataSet";
            string backupDataSetStr = "backupDataSet"; 
            string deletedRowsStr = "deletedRows";
            string addedRowsStr = "addedRows"; 
            string updatedRowsStr = "updatedRows"; 
            string resultStr = "result";
            string workConnStr = "workConnection"; 
            string workTransStr = "workTransaction";
            string workConnOpenedStr = "workConnOpened";
            string allChangedRowsStr = "allChangedRows";
            string allAddedRowsStr = "allAddedRows"; 
            string adaptersWithACDUStr = "adaptersWithAcceptChangesDuringUpdate";
            string revertConnectionsVar = "revertConnections"; 
            CodeExpression resultExp = CodeGenHelper.Variable(resultStr); 

            CodeMemberMethod method = 
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(int)),
                TAMNameHandler.UpdateAllMethod,
                MemberAttributes.Public
            ); 

            string dataSourceTypeRefName = this.dataSourceType.Name; 
 
            // DDBugs 126914: Use fully-qualified names for datasets
            if (this.codeGenerator.DataSetNamespace != null) { 
                dataSourceTypeRefName = CodeGenHelper.GetTypeName(this.codeGenerator.CodeProvider, this.codeGenerator.DataSetNamespace, dataSourceTypeRefName);
            }

            CodeTypeReference dataSourceTypeRef = CodeGenHelper.Type(dataSourceTypeRefName); 

            CodeParameterDeclarationExpression dataSetPara = CodeGenHelper.ParameterDecl(dataSourceTypeRef, dataSetStr); 
            method.Parameters.Add(dataSetPara); 

            method.Comments.Add(CodeGenHelper.Comment("Update all changes to the dataset.", true)); 

            //-----------------------------------------------------------------------------
            //\\If (dataSet Is Nothing) Then
            //\\    Throw New System.ArgumentNullException("dataSet") 
            //\\End If
            method.Statements.Add( 
                CodeGenHelper.If( 
                    CodeGenHelper.IdIsNull(CodeGenHelper.Argument(dataSetStr)),
                    CodeGenHelper.Throw(CodeGenHelper.GlobalType(typeof(ArgumentNullException)), dataSetStr) 
                )
            );

            //----------------------------------------------------------------------------- 
            //\\if (dataSet.HasChanges() == fase)
            //\\    return 0 
            method.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.EQ( 
                        CodeGenHelper.MethodCall(CodeGenHelper.Argument(dataSetStr), "HasChanges"),
                        CodeGenHelper.Primitive(false)
                    ),
                    CodeGenHelper.Return(CodeGenHelper.Primitive(0)) 
                )
            ); 
 
            //----------------------------------------------------------------------------
            //\\Dim workConnection As IDbConnection = Me.Connection 
            method.Statements.Add(
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(IDbConnection)), workConnStr, CodeGenHelper.ThisProperty("Connection")
                ) 
            );
            //\\If workConnection Is Nothing Then 
            //\\    throw new ApplicationException(No connection) 
            //\\End If
            method.Statements.Add( 
                CodeGenHelper.If(CodeGenHelper.IdIsNull(CodeGenHelper.Variable(workConnStr)),
                    CodeGenHelper.Throw(
                        CodeGenHelper.GlobalType(typeof(ApplicationException)),
                        SR.GetString(SR.CG_TableAdapterManagerHasNoConnection)) 
                )
            ); 
            //\\Dim workConnOpened As Boolean = False 
            method.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(bool)), workConnOpenedStr, CodeGenHelper.Primitive(false)
                )
            );
            //\\If ((workConnection.State And Global.System.Data.ConnectionState.Closed) _ 
            //\\         = Global.System.Data.ConnectionState.Closed) Then
            //\\    workCnnection.Open() 
            //\\    workConnOpened = True 
            //\\End If
            method.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.EQ(
                        CodeGenHelper.BitwiseAnd(
                            CodeGenHelper.Property(CodeGenHelper.Variable(workConnStr), "State"), 
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Closed")
                        ), 
                        CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Closed") 
                    ),
                    new CodeStatement[]{ 
                        CodeGenHelper.MethodCallStm(CodeGenHelper.Variable(workConnStr),"Open"),
                        CodeGenHelper.Assign(CodeGenHelper.Variable(workConnOpenedStr), CodeGenHelper.Primitive(true))
                    }
                ) 
            );
            //\\Dim workTransaction As IDbTransaction = workConnection.BeginTransaction() 
            method.Statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(IDbTransaction)), 
                    workTransStr,
                    CodeGenHelper.MethodCall(CodeGenHelper.Variable(workConnStr), "BeginTransaction")
                )
            ); 
            //\\ if (workTransaction == null){Throw}
            method.Statements.Add( 
                CodeGenHelper.If(CodeGenHelper.IdIsNull(CodeGenHelper.Variable(workTransStr)), 
                    CodeGenHelper.Throw(
                        CodeGenHelper.GlobalType(typeof(ApplicationException)), 
                        SR.GetString(SR.CG_TableAdapterManagerNotSupportTransaction))
                )
            );
            //\\Dim allChangedRows As New System.Collections.Generic.List(Of System.Data.DataRow)() 
            CodeTypeReference typeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(DataRow));
            method.Statements.Add( 
                CodeGenHelper.VariableDecl(typeRef, allChangedRowsStr, CodeGenHelper.New(typeRef, new CodeExpression[] { })) 
            );
            //\\Dim allAddedRows As New System.Collections.Generic.List(Of System.Data.DataRow)() 
            method.Statements.Add(
                CodeGenHelper.VariableDecl(typeRef, allAddedRowsStr, CodeGenHelper.New(typeRef, new CodeExpression[] { }))
            );
            //\\Dim adaptersWithACDU As New System.Collections.Generic.List(Of System.Data.Common.DataAdapter)() 
            typeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(System.Data.Common.DataAdapter));
            method.Statements.Add( 
                CodeGenHelper.VariableDecl(typeRef, adaptersWithACDUStr, CodeGenHelper.New(typeRef, new CodeExpression[] { })) 
            );
 

            //-----------------------------------------------------------------------------
            //\\System.Collections.Generic.IDictionary<object, System.Data.IDbConnection> revertConnections =
            //\\    new System.Collections.Generic.Dictionary<object, System.Data.IDbConnection>(); 
            CodeTypeReference genericTypeRef = new CodeTypeReference("System.Collections.Generic.Dictionary",
                CodeGenHelper.GlobalType(typeof(object)), 
                CodeGenHelper.GlobalType(typeof(System.Data.IDbConnection)) 
            );
            genericTypeRef.Options = CodeTypeReferenceOptions.GlobalReference; 
            method.Statements.Add(
                CodeGenHelper.VariableDecl(
                    genericTypeRef,
                    revertConnectionsVar, 
                    CodeGenHelper.New(genericTypeRef, new CodeExpression[] { })
                ) 
            ); 

            //---------------------------------------------------------------------------------------- 
            //\\int result = 0
            method.Statements.Add(
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.Type(typeof(int)), resultStr, CodeGenHelper.Primitive(0) 
                )
            ); 
 
            //----------------------------------------------------------------------------
            //\\Dim backupDataSet As DataSet = nothing 
            //\\If (this.BackUpDataSetBeforeUpdate) then
            //\\   backupDataSet = new DataSet
            //\\   backupDataSet.Merge(dataSet)
            //\\Endif 
            method.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(DataSet)), 
                    backupDataSetStr,
                    CodeGenHelper.Primitive(null) 
                )
            );
            method.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.ThisProperty(TAMNameHandler.BackupDataSetBeforeUpdateProperty),
                    new CodeStatement[]{ 
                        CodeGenHelper.Assign( 
                            CodeGenHelper.Variable(backupDataSetStr),
                            CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(DataSet)),new CodeExpression[]{}) 
                        ),
                        CodeGenHelper.MethodCallStm(CodeGenHelper.Variable(backupDataSetStr), "Merge", CodeGenHelper.Argument(dataSetStr))
                    }
                ) 
            );
 
            //--------------------------------------------------------------------------------------- 
            // Big try block
            //\\try { 
            //\\}
            //\\catch {
            //\\}
            //\\finally { 
            //\\}
            List<CodeStatement> tryUpdates = new List<CodeStatement>(); 
 
            tryUpdates.Add(new CodeCommentStatement("---- Prepare for update -----------\r\n"));
            //\\If (Not (Me._customersTableAdapter) Is Nothing) Then 
            //\\    revertConnections.Add(Me._customersTableAdapter, Me._customersTableAdapter.Connection)
            //\\    Me._customersTableAdapter.Connection = CType(workConnection, Global.System.Data.SqlClient.SqlConnection)
            //\\    Me._customersTableAdapter.Transaction = CType(workTransaction, Global.System.Data.SqlClient.SqlTransaction)
            //\\    If (_customersTableAdapter.Adapter.AcceptChangesDuringUpdate) Then 
            //\\ _customersTableAdapter.Adapter.AcceptChangesDuringUpdate = False
            //\\ adaptersWithACDU.Add(_customersTableAdapter.Adapter) 
            //\\    End If 
            //\\End If
            foreach (DesignTable table in dataSource.DesignTables) { 
                if (!CanAddTableAdapter(table)) {
                    continue;
                }
                string adapterVariableName = table.PropertyCache.TAMAdapterVarName; 

                CodeStatement assignTransactionStm = null; 
                if (table.PropertyCache.TransactionType != null) { 
                    //\\    _customersTableAdapter.Transaction = CType(workTransaction, Global.System.Data.SqlClient.SqlTransaction)
                    assignTransactionStm = CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Transaction"),
                        CodeGenHelper.Cast(CodeGenHelper.GlobalType(table.PropertyCache.TransactionType),
                            CodeGenHelper.Variable(workTransStr)
                        ) 
                    );
                } 
                else { 
                    assignTransactionStm = new CodeCommentStatement("Note: The TableAdapter does not have the Transaction property.");
                } 

                CodeStatement adaptersWithACDUStatement = null;
                // We need to make sure the _customersTableAdapter.Adapter is DataAdapter
                if (table.PropertyCache.AdapterType != null && typeof(DataAdapter).IsAssignableFrom(table.PropertyCache.AdapterType)) { 
                    //\\ If (_customersTableAdapter.Adapter.AcceptChangesDuringUpdate) Then
                    //\\    _customersTableAdapter.Adapter.AcceptChangesDuringUpdate = False 
                    //\\    adaptersWithACDU.Add(_customersTableAdapter.Adapter) 
                    //\\  End If
                    adaptersWithACDUStatement = CodeGenHelper.If( 
                        CodeGenHelper.Property(CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Adapter"), "AcceptChangesDuringUpdate"),
                        new CodeStatement[]{
                            CodeGenHelper.Assign(
                                CodeGenHelper.Property(CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName),"Adapter"),"AcceptChangesDuringUpdate"), 
                                CodeGenHelper.Primitive(false)
                            ), 
                            CodeGenHelper.Stm(CodeGenHelper.MethodCall( 
                                CodeGenHelper.Variable(adaptersWithACDUStr),
                                "Add", 
                                CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName),"Adapter")
                            ))
                        }
                    ); 
                }
                else { 
                    adaptersWithACDUStatement = new CodeCommentStatement("Note: Adapter is not a DataAdapter, so AcceptChangesDuringUpdate cannot be set to false."); 
                }
 
                tryUpdates.Add(
                    CodeGenHelper.If(
                        CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(adapterVariableName)),
                        new CodeStatement[]{ 
                            //\\revertConnections.Add(this._customersAdapter, this._customersAdapter.Connection);
                            CodeGenHelper.Stm( 
                                CodeGenHelper.MethodCall( 
                                    CodeGenHelper.Variable(revertConnectionsVar),
                                    "Add", 
                                    new CodeExpression[]{CodeGenHelper.ThisField(adapterVariableName),
                                        CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Connection")
                                    }
                                ) 
                            ),
                            //\\_customersTableAdapter.Connection = CType(workConnection, Global.System.Data.SqlClient.SqlConnection) 
                            CodeGenHelper.Assign( 
                                CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Connection"),
                                CodeGenHelper.Cast(CodeGenHelper.GlobalType(table.PropertyCache.ConnectionType), 
                                    CodeGenHelper.Variable(workConnStr)
                                )
                            ),
                            assignTransactionStm, 
                            adaptersWithACDUStatement
                        } 
                    ) 
                );
 
            }

            //-----start update----------------------------------------
            DataTable[] orderedTables = TableAdapterManagerHelper.GetUpdateOrder(dataSource.DataSet); 

            AddUpdateUpdatedMethod(dataComponentClass, orderedTables, dataSetPara, dataSetStr, resultStr, updatedRowsStr, allChangedRowsStr, allAddedRowsStr); 
            AddUpdateInsertedMethod(dataComponentClass, orderedTables, dataSetPara, dataSetStr, resultStr, addedRowsStr, allAddedRowsStr); 
            AddUpdateDeletedMethod(dataComponentClass, orderedTables, dataSetPara, dataSetStr, resultStr, deletedRowsStr, allChangedRowsStr);
            AddRealUpdatedRowsMethod(dataComponentClass, updatedRowsStr, allAddedRowsStr); 

            tryUpdates.Add(new CodeCommentStatement("\r\n---- Perform updates -----------\r\n"));

            //\\If (Me.UpdateOrder = UpdateOrderOption.UpdateInsertDelete) Then 
            //\\    result = Me.UpdateUpdatedRows(dataSet, allChangedRows, Nothing)
            //\\    result = Me.UpdateInsertedRows(dataSet, allAddedRows) 
            //\\ElseIf (Me.UpdateOrder = UpdateOrderOption.InsertUpdateDelete) Then 
            //\\    result = Me.UpdateInsertedRows(dataSet, allAddedRows)
            //\\    result = Me.UpdateUpdatedRows(dataSet, allChangedRows, allAddedRows) 
            //\\End If


 
            CodeStatement insertStm = CodeGenHelper.Assign(
                resultExp, 
                CodeGenHelper.BinOperator( 
                    resultExp,
                    CodeBinaryOperatorType.Add, 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.This(),
                        TAMNameHandler.UpdateInsertedRowsMethod,
                        new CodeExpression[] { CodeGenHelper.Argument(dataSetStr), CodeGenHelper.Variable(allAddedRowsStr) } 
                    )
                ) 
            ); 

            CodeStatement updateStm = CodeGenHelper.Assign( 
                resultExp, CodeGenHelper.BinOperator(
                    resultExp,
                    CodeBinaryOperatorType.Add,
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.This(),
                        TAMNameHandler.UpdateUpdatedRowsMethod, 
                        new CodeExpression[] { CodeGenHelper.Argument(dataSetStr), CodeGenHelper.Variable(allChangedRowsStr), CodeGenHelper.Variable(allAddedRowsStr) } 
                    )
                ) 
            );


            // Update and Insert 
            tryUpdates.Add(CodeGenHelper.If(
                CodeGenHelper.EQ( 
                    CodeGenHelper.ThisProperty(TAMNameHandler.UpdateOrderOptionProperty), 
                    CodeGenHelper.Field(CodeGenHelper.TypeExpr(CodeGenHelper.Type(TAMNameHandler.UpdateOrderOptionEnum)), TAMNameHandler.UpdateOrderOptionEnumUID)
                ), 
                new CodeStatement[] { updateStm, insertStm },
                new CodeStatement[] { insertStm, updateStm }
            ));
 
            // Delete last
            tryUpdates.Add( 
                CodeGenHelper.Assign(resultExp, CodeGenHelper.BinOperator(resultExp, CodeBinaryOperatorType.Add, 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.This(), 
                        TAMNameHandler.UpdateDeletedRowsMethod,
                        new CodeExpression[] { CodeGenHelper.Argument(dataSetStr), CodeGenHelper.Variable(allChangedRowsStr) }
                    )
                )) 
            );
 
            //---------------------------------------------------------------------------------- 
            //\\workTransaction.Commit()
            tryUpdates.Add(new CodeCommentStatement("\r\n---- Commit updates -----------\r\n")); 
            tryUpdates.Add(
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable(workTransStr), "Commit" 
                    )
                ) 
            ); 
            //\\For Each row As DataRow In allAddedRows
            //\\    row.AcceptChanges() 
            //\\Next
            tryUpdates.Add(
                this.HandleForEachRowInList(allAddedRowsStr, new string[] { "AcceptChanges" })
            ); 
            //\\For Each row As DataRow In allChangedRows
            //\\    row.AcceptChanges() 
            //\\Next 
            tryUpdates.Add(
                this.HandleForEachRowInList(allChangedRowsStr, new string[] { "AcceptChanges" }) 
            );

            //---catch block-------------------------------------------------------------
            //\\workTransaction.Rollback() 
            CodeCatchClause catchUpdate = new CodeCatchClause();
            catchUpdate.Statements.Add( 
                CodeGenHelper.MethodCall( 
                    CodeGenHelper.Variable(workTransStr),
                    "Rollback" 
                )
            );
            catchUpdate.Statements.Add(new CodeCommentStatement("---- Restore the dataset -----------"));
            //\\If (Me.BackupDataSetBeforeUpdate) Then 
            //\\    dataSet.Clear()
            //\\    dataSet.Merge(backupDataSet) 
            //\\Else 
            //\\  For Each row As DataRow In allAddedRows
            //\\    row.AcceptChanges() 
            //\\    row.SetAdded()
            //\\  Next
            //\\Endif
            catchUpdate.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.ThisProperty(TAMNameHandler.BackupDataSetBeforeUpdateProperty), 
                    new CodeStatement[]{ 
                        CodeGenHelper.MethodCallStm(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Diagnostics.Debug)), 
                            "Assert",
                            CodeGenHelper.IdIsNotNull(CodeGenHelper.Variable(backupDataSetStr))
                        ),
                        CodeGenHelper.MethodCallStm(CodeGenHelper.Argument(dataSetStr), "Clear"), 
                        CodeGenHelper.MethodCallStm(CodeGenHelper.Argument(dataSetStr),"Merge", CodeGenHelper.Variable(backupDataSetStr))
                    }, 
                    new CodeStatement[]{ 
                        this.HandleForEachRowInList(allAddedRowsStr, new string[] { "AcceptChanges", "SetAdded" })
                    } 
                )
            );

            catchUpdate.CatchExceptionType = CodeGenHelper.GlobalType(typeof(System.Exception)); 
            catchUpdate.LocalName = "ex";
            catchUpdate.Statements.Add(new CodeThrowExceptionStatement(CodeGenHelper.Variable("ex"))); 
 
            //
            //---Final statements--------------------------------------------------------- 
            //
            //\\finally {
            List<CodeStatement> finalUpdates = new List<CodeStatement>();
            //\\    If workConnOpened Then 
            //\\      workConnection.Close()
            //\\    End If 
            finalUpdates.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.Variable(workConnOpenedStr), 
                    CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.Variable(workConnStr), "Close"))
                )
            );
            //\\    if ((_customersTableAdapter != null)) { 
            //\\        _customersTableAdapter.Connection = ((System.Data.SqlClient.SqlConnection)(revertConnections[_customersTableAdapter]));
            //\\        _customersTableAdapter.Transaction = null; 
            //\\    } 
            foreach (DesignTable table in dataSource.DesignTables) {
                if (!CanAddTableAdapter(table)) { 
                    continue;
                }
                string adapterVariableName = table.PropertyCache.TAMAdapterVarName;
                CodeStatement assignTransactionStm = null; 
                if (table.PropertyCache.TransactionType != null) {
                    assignTransactionStm = CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Transaction"), 
                        CodeGenHelper.Primitive(null)
                    ); 
                }
                else {
                    assignTransactionStm = new CodeCommentStatement("Note: No Transaction property of the TableAdapter");
                } 

                finalUpdates.Add( 
                    CodeGenHelper.If( 
                        CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(adapterVariableName)),
                        new CodeStatement[]{ 
                            //\\ this._customersAdapter.Connection = revertConnections[_customersAdapter] as System.Data.SqlClient.SqlConnection;
                            CodeGenHelper.Assign(
                                CodeGenHelper.Property(
                                    CodeGenHelper.ThisField(adapterVariableName), 
                                    "Connection"
                                ), 
                                CodeGenHelper.Cast( 
                                    CodeGenHelper.GlobalType(table.PropertyCache.ConnectionType),
                                    CodeGenHelper.Indexer( 
                                        CodeGenHelper.Variable(revertConnectionsVar),
                                        CodeGenHelper.ThisField(adapterVariableName)
                                    )
                                ) 
                            ),
                            assignTransactionStm 
                        } 
                    )
                ); 
            }

            //\\For Each adapter As Data.Common.DataAdapter In adaptersWithACDU
            //\\     adapter.AcceptChangesDuringUpdate = True 
            //\\ Next
            finalUpdates.Add(this.RestoreAdaptersWithACDU(adaptersWithACDUStr)); 
 
            //---Add the try block ----------------------------
            method.Statements.Add( 
                CodeGenHelper.Try(
                    tryUpdates.ToArray(),
                    new CodeCatchClause[] { catchUpdate },
                    finalUpdates.ToArray() 
                )
            ); 
            //--------------------------------------------------------------------------------------- 
            //\\return result
            method.Statements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Variable(resultStr))
            );
            dataComponentClass.Members.Add(method);
        } 

        /// <summary> 
        /// Create UpdateInsertdRows method 
        ///----insert in forward order-----
        ///\\if ((_customersTableAdapter != null)) { 
        ///\\    System.Data.DataRow[] insertedRows = changes.Customers.Select(null, null, global::System.Data.DataViewRowState.Added);
        ///\\    _customersTableAdapter.Update(insertedRows);
        ///\\}
        /// </summary> 
        /// <param name="dataComponentClass"></param>
        /// <param name="orderedTables"></param> 
        /// <param name="dataSetPara"></param> 
        /// <param name="dataSetStr"></param>
        /// <param name="resultStr"></param> 
        /// <param name="addedRowsStr"></param>
        /// <param name="allAddedRowsStr"></param>
        private void AddUpdateInsertedMethod(CodeTypeDeclaration dataComponentClass, DataTable[] orderedTables, CodeParameterDeclarationExpression dataSetPara, string dataSetStr, string resultStr, string addedRowsStr, string allAddedRowsStr) {
            CodeMemberMethod method = 
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(int)),
                TAMNameHandler.UpdateInsertedRowsMethod, 
                MemberAttributes.Private 
            );
 
            CodeTypeReference dataRowListTypeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(DataRow));
            CodeParameterDeclarationExpression dataRowListPara = CodeGenHelper.ParameterDecl(dataRowListTypeRef, allAddedRowsStr);

            method.Parameters.AddRange( 
                new CodeParameterDeclarationExpression[]{
                    dataSetPara, 
                    dataRowListPara 
                }
            ); 

            //\\int result = 0
            method.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.Type(typeof(int)), resultStr, CodeGenHelper.Primitive(0)
                ) 
            ); 

            method.Comments.Add(CodeGenHelper.Comment("Insert rows in top-down order.", true)); 
            for (int i = 0; i < orderedTables.Length; i++) {
                DesignTable table = dataSource.DesignTables[orderedTables[i]];
                if (!CanAddTableAdapter(table)) {
                    continue; 
                }
                method.Statements.Add(this.AddUpdateAllTAUpdate(table, dataSetStr, resultStr, addedRowsStr, allAddedRowsStr, "Added", null)); 
            } 

            //\\return result 
            method.Statements.Add(
                CodeGenHelper.Return(CodeGenHelper.Variable(resultStr))
            );
 
            dataComponentClass.Members.Add(method);
        } 
 
        private void AddUpdateDeletedMethod(CodeTypeDeclaration dataComponentClass, DataTable[] orderedTables, CodeParameterDeclarationExpression dataSetPara, string dataSetStr, string resultStr, string deletedRowsStr, string allChangedRowsStr) {
            CodeMemberMethod method = 
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(int)),
                TAMNameHandler.UpdateDeletedRowsMethod,
                MemberAttributes.Private
            ); 

            CodeTypeReference dataRowListTypeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(DataRow)); 
            CodeParameterDeclarationExpression dataRowListPara = CodeGenHelper.ParameterDecl(dataRowListTypeRef, allChangedRowsStr); 

            method.Parameters.AddRange( 
                new CodeParameterDeclarationExpression[]{
                    dataSetPara,
                    dataRowListPara
                } 
            );
 
            method.Comments.Add(CodeGenHelper.Comment("Delete rows in bottom-up order.", true)); 

            //\\int result = 0 
            method.Statements.Add(
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.Type(typeof(int)), resultStr, CodeGenHelper.Primitive(0)
                ) 
            );
 
            //\\if ((_customersTableAdapter != null)) { 
            //\\    System.Data.DataRow[] deletedRows = changes.Customers.Select(null, null, global::System.Data.DataViewRowState.Deleted);
            //\\    _customersTableAdapter.Update(deletedRows); 
            //\\}
            for (int i = orderedTables.Length - 1; i >= 0; i--) {
                DesignTable table = dataSource.DesignTables[orderedTables[i]];
                if (!CanAddTableAdapter(table)) { 
                    continue;
                } 
                method.Statements.Add(this.AddUpdateAllTAUpdate(table, dataSetStr, resultStr, deletedRowsStr, allChangedRowsStr, "Deleted", null)); 
            }
 
            //\\return result
            method.Statements.Add(
                CodeGenHelper.Return(CodeGenHelper.Variable(resultStr))
            ); 

            dataComponentClass.Members.Add(method); 
        } 

        private void AddUpdateUpdatedMethod(CodeTypeDeclaration dataComponentClass, DataTable[] orderedTables, CodeParameterDeclarationExpression dataSetPara, string dataSetStr, string resultStr, string updatedRowsStr, string allChangedRowsStr, string allAddedRowsStr) { 
            CodeMemberMethod method =
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(int)),
                TAMNameHandler.UpdateUpdatedRowsMethod,
                MemberAttributes.Private 
            );
 
            CodeTypeReference dataRowListTypeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(DataRow)); 
            CodeParameterDeclarationExpression dataRowListPara = CodeGenHelper.ParameterDecl(dataRowListTypeRef, allChangedRowsStr);
            CodeParameterDeclarationExpression addedDataRowListPara = CodeGenHelper.ParameterDecl(dataRowListTypeRef, allAddedRowsStr); 

            method.Parameters.AddRange(
                new CodeParameterDeclarationExpression[]{
                    dataSetPara, 
                    dataRowListPara,
                    addedDataRowListPara 
                } 
            );
 
            method.Comments.Add(CodeGenHelper.Comment("Update rows in top-down order.", true));

            //\\int result = 0
            method.Statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.Type(typeof(int)), resultStr, CodeGenHelper.Primitive(0) 
                ) 
            );
 
            //\\    updatedRows = GetRealUpdatedRows(updatedRows, allAddedRows)
            //\\    if (this._customersAdapter != null) {
            //\\        result = result + this._customersAdapter.Update(changes.Customers);
            //\\    } 
            for (int i = 0; i < orderedTables.Length; i++) {
                DesignTable table = dataSource.DesignTables[orderedTables[i]]; 
                if (!CanAddTableAdapter(table)) { 
                    continue;
                } 
                method.Statements.Add(this.AddUpdateAllTAUpdate(table, dataSetStr, resultStr, updatedRowsStr, allChangedRowsStr, "ModifiedCurrent", allAddedRowsStr));
            }

            //\\return result 
            method.Statements.Add(
                CodeGenHelper.Return(CodeGenHelper.Variable(resultStr)) 
            ); 

            dataComponentClass.Members.Add(method); 
        }

        /// <summary>
        /// Used to filter out inserted rows, that become updated rows after calling TA.Update 
        ///If (updatedRows IsNot Nothing AndAlso updatedRows.Length > 0 AndAlso allAddedRows IsNot Nothing AndAlso allAddedRows.Count > 0) Then
        ///    Dim realUpdatedRows As New Global.System.Collections.Generic.List(Of Global.System.Data.DataRow) 
        ///    For Each row As DataRow In updatedRows 
        ///        If (Not allAddedRows.Contains(row)) Then
        ///            realUpdatedRows.Add(row) 
        ///        End If
        ///    Next
        ///    If (realUpdatedRows.Count < updatedRows.Length) Then
        ///        updatedRows = realUpdatedRows.ToArray() 
        ///    End If
        ///End If 
        /// </summary> 
        /// <param name="dataComponentClass"></param>
        /// <param name="updatedRowsStr"></param> 
        /// <param name="allAddedRowsStr"></param>
        private void AddRealUpdatedRowsMethod(CodeTypeDeclaration dataComponentClass, string updatedRowsStr, string allAddedRowsStr) {
            string realUpdatedRowsStr = "realUpdatedRows";
 
            CodeMemberMethod method =
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(DataRow), 1), 
                TAMNameHandler.GetRealUpdatedRowsMethod, 
                MemberAttributes.Private
            ); 

            CodeTypeReference dataRowListTypeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(DataRow));
            CodeParameterDeclarationExpression addedDataRowListPara = CodeGenHelper.ParameterDecl(dataRowListTypeRef, allAddedRowsStr);
 
            CodeTypeReference dataRowArrryTypeRef = CodeGenHelper.GlobalType(typeof(DataRow), 1);
            CodeParameterDeclarationExpression dataRowArrayPara = CodeGenHelper.ParameterDecl(dataRowArrryTypeRef, updatedRowsStr); 
 
            method.Comments.Add(CodeGenHelper.Comment("Remove inserted rows that become updated rows after calling TableAdapter.Update(inserted rows) first", true));
 
            method.Parameters.AddRange(
                new CodeParameterDeclarationExpression[]{
                    dataRowArrayPara,
                    addedDataRowListPara 
                }
            ); 
 
            //\\If (updatedRows Is Nothing OrElse updatedRows.Length < 1
            //\\ return updatedRows 
            method.Statements.Add(
                CodeGenHelper.If(CodeGenHelper.Or(
                        CodeGenHelper.IdIsNull(CodeGenHelper.Argument(updatedRowsStr)),
                        CodeGenHelper.Less(CodeGenHelper.Property(CodeGenHelper.Argument(updatedRowsStr), "Length"), CodeGenHelper.Primitive(1)) 
                    ),
                    CodeGenHelper.Return(CodeGenHelper.Variable(updatedRowsStr)) 
               ) 
            );
            //\\If (allAddedRows Is Nothing OrElse allAddedRows.Count < 1 
            //\\ return updatedRows
            method.Statements.Add(
                CodeGenHelper.If(CodeGenHelper.Or(
                        CodeGenHelper.IdIsNull(CodeGenHelper.Argument(allAddedRowsStr)), 
                        CodeGenHelper.Less(CodeGenHelper.Property(CodeGenHelper.Argument(allAddedRowsStr), "Count"), CodeGenHelper.Primitive(1))
                    ), 
                    CodeGenHelper.Return(CodeGenHelper.Variable(updatedRowsStr)) 
               )
            ); 

            //\\    Dim realUpdatedRows As New Global.System.Collections.Generic.List(Of Global.System.Data.DataRow)
            //\\    For Each row As DataRow In updatedRows
            //\\        If (Not allAddedRows.Contains(row)) Then 
            //\\            realUpdatedRows.Add(row)
            //\\        End If 
            //\\    Next 
            method.Statements.Add(
                CodeGenHelper.VariableDecl(dataRowListTypeRef, realUpdatedRowsStr, CodeGenHelper.New(dataRowListTypeRef, new CodeExpression[] { })) 
            );
            string rowStr = "row";
            CodeStatement[] forStms = new CodeStatement[2];
            forStms[0] = CodeGenHelper.VariableDecl( 
                            CodeGenHelper.GlobalType(typeof(DataRow)),
                            rowStr, 
                            CodeGenHelper.Indexer(CodeGenHelper.Variable(updatedRowsStr), CodeGenHelper.Variable("i")) 
                        );
            forStms[1] = CodeGenHelper.If( 
                            CodeGenHelper.EQ(CodeGenHelper.MethodCall(CodeGenHelper.Argument(allAddedRowsStr), "Contains", CodeGenHelper.Variable(rowStr)), CodeGenHelper.Primitive(false)),
                            CodeGenHelper.MethodCallStm(CodeGenHelper.Variable(realUpdatedRowsStr), "Add", CodeGenHelper.Variable(rowStr))
                        );
 
            method.Statements.Add(this.GetForLoopItoCount(CodeGenHelper.Property(CodeGenHelper.Argument(updatedRowsStr), "Length"), forStms));
 
            //\\Return realUpdatedRows.ToArray 
            method.Statements.Add(
                CodeGenHelper.Return(CodeGenHelper.MethodCall(CodeGenHelper.Variable(realUpdatedRowsStr), "ToArray")) 
            );

            dataComponentClass.Members.Add(method);
        } 

 
        /// <summary> 
        /// Helper function used by AddUpdateAll
        /// </summary> 
        private CodeStatement AddUpdateAllTAUpdate(DesignTable table, string dataSetStr, string resultStr, string updateRowsStr, string allUpdateRowsStr, string rowState, string allAddedRowsStr) {
            Debug.Assert(table != null);
            Debug.Assert(StringUtil.NotEmptyAfterTrim(dataSetStr));
            Debug.Assert(StringUtil.NotEmptyAfterTrim(resultStr)); 
            Debug.Assert(StringUtil.NotEmptyAfterTrim(updateRowsStr));
            Debug.Assert(StringUtil.NotEmptyAfterTrim(allUpdateRowsStr)); 
            Debug.Assert(StringUtil.NotEmptyAfterTrim(rowState)); 

            string adapterVariableName = table.PropertyCache.TAMAdapterVarName; 
            CodeStatement[] updateStatementsArray =
                new CodeStatement[]{
                    CodeGenHelper.Assign(
                        CodeGenHelper.Variable(resultStr), 
                        CodeGenHelper.BinOperator(CodeGenHelper.Variable(resultStr),
                            CodeBinaryOperatorType.Add, 
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.ThisField(adapterVariableName),
                                "Update", 
                                CodeGenHelper.Variable(updateRowsStr)
                            )
                        )
                    ), 
                    CodeGenHelper.Stm(CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable(allUpdateRowsStr),"AddRange",CodeGenHelper.Variable(updateRowsStr) 
                    )) 
                };
 
            // Handle self referenced relation
            DataRelation[] selfRefs = TableAdapterManagerHelper.GetSelfRefRelations(table.DataTable);
            if (selfRefs != null && selfRefs.Length > 0) {
                bool childFirst = StringUtil.EqualValue("Deleted", rowState, true); 
                List<CodeStatement> updateStatementsList = new List<CodeStatement>(updateStatementsArray.Length + selfRefs.Length);
                for (int i = 0; i < selfRefs.Length; i++) { 
                    if (i > 0) { 
                        updateStatementsList.Add(
                            new CodeCommentStatement("Note: More than one self-referenced relation found.  The generated code may not work correctly.") 
                        );
                    }
                    updateStatementsList.Add(
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall( 
                            CodeGenHelper.This(),
                            TAMNameHandler.SortSelfRefRowsMethod, 
                            new CodeExpression[]{ 
                                CodeGenHelper.Variable(updateRowsStr),
                                CodeGenHelper.Indexer( 
                                    CodeGenHelper.Property(CodeGenHelper.Argument(dataSetStr),"Relations"),
                                    CodeGenHelper.Str(selfRefs[i].RelationName)
                                ),
                                CodeGenHelper.Primitive(childFirst) 
                            }
                        )) 
                    ); 
                }
 
                updateStatementsList.AddRange(updateStatementsArray);
                updateStatementsArray = updateStatementsList.ToArray();
            }
 
            List<CodeStatement> ifUpdateBody = new List<CodeStatement>(3);
            //\\Dim updatedRows() As Global.System.Data.DataRow = dataSet.Customers.Select(Nothing, Nothing, Global.System.Data.DataViewRowState.ModifiedCurrent) 
            ifUpdateBody.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(System.Data.DataRow), 1), 
                    updateRowsStr,
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Argument(dataSetStr), table.GeneratorTablePropName),
                        "Select", 
                        new CodeExpression[]{
                            CodeGenHelper.Primitive(null), 
                            CodeGenHelper.Primitive(null), 
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataViewRowState)), rowState)
                        } 
                    )
                )
            );
 
            //\\updatedRows = GetRealUpdatedRows(updatedRows, allAddedRows)
            if (StringUtil.NotEmptyAfterTrim(allAddedRowsStr)) { 
                ifUpdateBody.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Argument(updateRowsStr), 
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.This(),
                            TAMNameHandler.GetRealUpdatedRowsMethod,
                            new CodeExpression[]{ 
                                CodeGenHelper.Argument(updateRowsStr),
                                CodeGenHelper.Argument(allAddedRowsStr) 
                            } 
                        )
                    ) 
                );
            }

            //\\    if (updateRows != null && updateRows.Length > 0) 
            //\\        result = result + _customersTableAdapter.Update(updateRows);
            //\\        allUpdatedRows.AddRange(updateRows) 
            ifUpdateBody.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.And( 
                        CodeGenHelper.IdNotEQ(CodeGenHelper.Variable(updateRowsStr), CodeGenHelper.Primitive(null)),
                        CodeGenHelper.Less(CodeGenHelper.Primitive(0),
                            CodeGenHelper.Property(CodeGenHelper.Variable(updateRowsStr), "Length")
                        ) 
                    ),
                    updateStatementsArray 
                ) 
            );
 
            //\\If (Not (Me._customersTableAdapter) Is Nothing) Then
            CodeStatement result =
                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ( 
                        CodeGenHelper.ThisField(adapterVariableName), CodeGenHelper.Primitive(null)
                    ), 
                    ifUpdateBody.ToArray() 
                );
            return result; 
        }

        /// <summary>
        /// Helper method to add variable as well as Property 
        /// example:
        /// private System.Data.IDbTransaction _transaction; 
        /// [global::System.Diagnostics.DebuggerNonUserCodeAttribute()] 
        /// public System.Data.IDbTransaction Transaction {
        ///     get { 
        ///         return this._transaction;
        ///     }
        /// }
        /// </summary> 
        /// <param name="codeType"></param>
        /// <param name="memberAttributes"></param> 
        /// <param name="propertyType"></param> 
        /// <param name="propertyName"></param>
        /// <param name="variableName"></param> 
        /// <param name="getOnly"></param>
        private void AddVariableAndProperty(CodeTypeDeclaration codeType, MemberAttributes memberAttributes, CodeTypeReference propertyType, string propertyName, string variableName, bool getOnly) {
            Debug.Assert(codeType != null);
            Debug.Assert(propertyType != null); 
            Debug.Assert(StringUtil.NotEmptyAfterTrim(propertyName));
            Debug.Assert(StringUtil.NotEmptyAfterTrim(variableName)); 
 
            codeType.Members.Add(
                CodeGenHelper.FieldDecl( 
                    propertyType,
                    variableName
                )
            ); 

            CodeMemberProperty property = 
                CodeGenHelper.PropertyDecl( 
                    propertyType,
                    propertyName, 
                    memberAttributes
            );
            property.GetStatements.Add(
                    CodeGenHelper.Return( 
                        CodeGenHelper.ThisField(variableName)
                    ) 
            ); 
            if (!getOnly) {
                property.SetStatements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.ThisField(variableName),
                        CodeGenHelper.Argument("value")
                    ) 
                );
            } 
            codeType.Members.Add(property); 
        }
 
        /// <summary>
        /// Check to see if a TableAdapter is qualified as a property in TableAdapterManager
        /// To be qualified, a TableAdapter needs to have update commands and the connection is accessable.
        /// </summary> 
        /// <param name="table"></param>
        /// <returns></returns> 
        private bool CanAddTableAdapter(DesignTable table) { 
            Debug.Assert(table != null, "table is null");
 
            if (table != null && table.HasAnyUpdateCommand) {
                MemberAttributes connectionModifier = ((DesignConnection)table.Connection).Modifier & MemberAttributes.AccessMask;
                if (connectionModifier == MemberAttributes.FamilyOrAssembly
                   || connectionModifier == MemberAttributes.Assembly 
                   || connectionModifier == MemberAttributes.Public
                   || connectionModifier == MemberAttributes.FamilyAndAssembly) { 
                    return true; 
                }
            } 
            return false;
        }

        /// <summary> 
        /// Generated code that restore the DataAdapter.AcceptChangeDuringUpdate property
        /// For Each adapter As Data.Common.DataAdapter In adaptersWithACDU 
        ///      adapter.AcceptChangesDuringUpdate = True 
        /// Next
        /// </summary> 
        /// <param name="listStr"></param>
        /// <param name="methods"></param>
        /// <returns></returns>
        private CodeStatement RestoreAdaptersWithACDU(string listStr) { 
            Debug.Assert(StringUtil.NotEmptyAfterTrim(listStr));
 
            CodeStatement[] forStms = new CodeStatement[2]; 
            forStms[0] = CodeGenHelper.VariableDecl(
                            CodeGenHelper.GlobalType(typeof(DataAdapter)), 
                            "adapter",
                            CodeGenHelper.Indexer(CodeGenHelper.Variable("adapters"), CodeGenHelper.Variable("i"))
                        );
            forStms[1] = CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.Variable("adapter"), "AcceptChangesDuringUpdate"), CodeGenHelper.Primitive(true)); 

            CodeStatement result = 
                CodeGenHelper.If( 
                     CodeGenHelper.Less(CodeGenHelper.Primitive(0), CodeGenHelper.Property(CodeGenHelper.Variable(listStr), "Count")),
                     new CodeStatement[]{ 
                        CodeGenHelper.VariableDecl(
                            CodeGenHelper.GlobalType(typeof(DataAdapter),1),
                            "adapters",
                            this.NewArray(CodeGenHelper.GlobalType(typeof(DataAdapter),1),CodeGenHelper.Property(CodeGenHelper.Variable(listStr),"Count")) 
                        ),
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.Variable(listStr),"CopyTo",CodeGenHelper.Variable("adapters"))), 
                        this.GetForLoopItoCount( 
                            CodeGenHelper.Property(CodeGenHelper.Variable("adapters"), "Length"),
                            forStms 
                        )
                    }
                 );
            return result; 
        }
 
        /// <summary> 
        /// Helper method to generate for each code like
        /// // For Each row As DataRow In allAddedRows 
        /// //    methods
        /// // Next
        /// </summary>
        /// <param name="listStr"></param> 
        /// <param name="methods"></param>
        /// <returns></returns> 
        private CodeStatement HandleForEachRowInList(string listStr, string[] methods) { 
            Debug.Assert(methods != null && methods.Length > 0);
            Debug.Assert(StringUtil.NotEmptyAfterTrim(listStr)); 

            CodeStatement[] forStms = new CodeStatement[methods.Length + 1];
            forStms[0] = CodeGenHelper.VariableDecl(
                            CodeGenHelper.GlobalType(typeof(DataRow)), 
                            "row",
                            CodeGenHelper.Indexer(CodeGenHelper.Variable("rows"), CodeGenHelper.Variable("i")) 
                        ); 
            for (int i = 0; i < methods.Length; i++) {
                forStms[i + 1] = CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.Variable("row"), methods[i])); 
            }

            CodeStatement result =
                CodeGenHelper.If( 
                     CodeGenHelper.Less(CodeGenHelper.Primitive(0), CodeGenHelper.Property(CodeGenHelper.Variable(listStr), "Count")),
                     new CodeStatement[]{ 
                        CodeGenHelper.VariableDecl( 
                            CodeGenHelper.GlobalType(typeof(DataRow),1),
                            "rows", 
                            this.NewArray(CodeGenHelper.GlobalType(typeof(DataRow),1),CodeGenHelper.Property(CodeGenHelper.Variable(listStr),"Count"))
                        ),
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.Variable(listStr),"CopyTo",CodeGenHelper.Variable("rows"))),
                        this.GetForLoopItoCount( 
                            CodeGenHelper.Property(CodeGenHelper.Variable("rows"), "Length"),
                            forStms 
                        ) 
                    }
                 ); 
            return result;
        }

        /// <summary> 
        /// Helper to generate the For loop code
        /// Get for(int i=0; i<Count;i++){ 
        /// } 
        /// </summary>
        /// <returns></returns> 
        private CodeStatement GetForLoopItoCount(CodeExpression countExp, CodeStatement[] forStms) {
            return this.GetForLoopItoCount("i", countExp, forStms);
        }
 
        /// <summary>
        /// Helper to generate the For loop code 
        /// </summary> 
        /// <param name="iStr">the i variable in the loop</param>
        /// <param name="countExp">end condition</param> 
        /// <param name="forStms">body</param>
        /// <returns>code</returns>
        private CodeStatement GetForLoopItoCount(string iStr, CodeExpression countExp, CodeStatement[] forStms) {
            CodeStatement forInit = CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(int)), iStr, CodeGenHelper.Primitive(0)); 
            CodeStatement forIncrement = CodeGenHelper.Assign(
                                        CodeGenHelper.Variable(iStr), 
                                        CodeGenHelper.BinOperator( 
                                            CodeGenHelper.Variable(iStr),
                                            CodeBinaryOperatorType.Add, 
                                            CodeGenHelper.Primitive(1)
                                        )
                                     );
            CodeExpression forTest = CodeGenHelper.Less( 
                                        CodeGenHelper.Variable(iStr),
                                        countExp 
                                     ); 
            return CodeGenHelper.ForLoop(forInit, forTest, forIncrement, forStms);
        } 

        /// <summary>
        /// Helper function to generate an array with size
        /// </summary> 
        /// <param name="type"></param>
        /// <param name="size"></param> 
        /// <returns></returns> 
        private CodeExpression NewArray(CodeTypeReference type, CodeExpression size) { return new CodeArrayCreateExpression(type, size); }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
namespace System.Data.Design { 
    using System; 
    using System.CodeDom;
    using System.Collections; 
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common; 
    using System.Data.SqlClient;
    using System.Design; 
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection; 
    using TAMNameHandler = TableAdapterManagerNameHandler;

    internal sealed class TableAdapterManagerMethodGenerator {
        private TypedDataSourceCodeGenerator codeGenerator; 
        private DesignDataSource dataSource;
        private CodeTypeDeclaration dataSourceType; 
        private TableAdapterManagerNameHandler nameHandler; 
        // This is the editor attribute for the TableAdapter property in the TableAdapterManager
        private const string adapterPropertyEditor = "Microsoft.VSDesigner.DataSource.Design.TableAdapterManagerPropertyEditor"; 


        internal TableAdapterManagerMethodGenerator(TypedDataSourceCodeGenerator codeGenerator, DesignDataSource dataSource, CodeTypeDeclaration dataSourceType) {
            Debug.Assert(codeGenerator != null); 
            Debug.Assert(dataSource != null);
            Debug.Assert(dataSourceType != null); 
 
            this.codeGenerator = codeGenerator;
            this.dataSource = dataSource; 
            this.dataSourceType = dataSourceType;
            this.nameHandler = new TableAdapterManagerNameHandler(codeGenerator.CodeProvider);
        }
 
        internal void AddEverything(CodeTypeDeclaration dataComponentClass) {
            if (dataComponentClass == null) { 
                throw new InternalException("dataComponent CodeTypeDeclaration should not be null."); 
            }
            AddUpdateOrderMembers(dataComponentClass); 
            AddAdapterMembers(dataComponentClass);
            // AddBackupDataSetMembers(dataComponentClass);
            this.AddVariableAndProperty(dataComponentClass,
                    MemberAttributes.Public | MemberAttributes.Final, 
                    CodeGenHelper.GlobalType(typeof(bool)),
                    TAMNameHandler.BackupDataSetBeforeUpdateProperty, 
                    TAMNameHandler.BackupDataSetBeforeUpdateVar, 
                    false);
 
            AddConnectionMembers(dataComponentClass);
            AddTableAdapterCountMembers(dataComponentClass);
            AddUpdateAll(dataComponentClass);
            AddSortSelfRefRows(dataComponentClass); 
            AddSelfRefComparer(dataComponentClass);
            AddMatchTableAdapterConnection(dataComponentClass); 
        } 

        /// <summary> 
        ///Public Enum UpdateOrderOption
        ///    InsertUpdateDelete = 1
        ///    UpdateInsertDelete = 2
        ///End Enum 
        /// </summary>
        /// <param name="dataComponentClass"></param> 
        private void AddUpdateOrderMembers(CodeTypeDeclaration dataComponentClass) { 

            CodeTypeDeclaration updateOrderEnum = CodeGenHelper.Class( 
                TAMNameHandler.UpdateOrderOptionEnum, false, TypeAttributes.NestedPublic
            );
            updateOrderEnum.IsEnum = true;
            updateOrderEnum.Comments.Add(CodeGenHelper.Comment("Update Order Option", true)); 

            CodeMemberField insertUpdateDeleteEnum = CodeGenHelper.FieldDecl(CodeGenHelper.Type(typeof(int)), TAMNameHandler.UpdateOrderOptionEnumIUD, CodeGenHelper.Primitive(0)); 
            updateOrderEnum.Members.Add(insertUpdateDeleteEnum); 
            CodeMemberField updateInsertDeleteEnum = CodeGenHelper.FieldDecl(CodeGenHelper.Type(typeof(int)), TAMNameHandler.UpdateOrderOptionEnumUID, CodeGenHelper.Primitive(1));
            updateOrderEnum.Members.Add(updateInsertDeleteEnum); 

            dataComponentClass.Members.Add(updateOrderEnum);
            // undone throw excpetion for invalid argument
            this.AddVariableAndProperty(dataComponentClass, MemberAttributes.Public | MemberAttributes.Final, 
                CodeGenHelper.Type(TAMNameHandler.UpdateOrderOptionEnum),
                TAMNameHandler.UpdateOrderOptionProperty, 
                TAMNameHandler.UpdateOrderOptionVar, 
                false);
        } 

        /// <summary>
        /// Add TableAdapter properties
        /// Example: 
        ///  private CustomersTableAdapter _customersAdapter;
        ///  [System.Diagnostics.DebuggerNonUserCodeAttribute()] 
        ///  public CustomersTableAdapter CustomersAdapter { 
        ///    get {
        ///        return _customersAdapter; 
        ///    }
        ///    set {
        ///        _customersAdapter = value;
        ///    } 
        ///  }
        /// </summary> 
        /// <param name="dataComponentClass"></param> 
        private void AddAdapterMembers(CodeTypeDeclaration dataComponentClass) {
            foreach (DesignTable table in dataSource.DesignTables) { 
                if (!this.CanAddTableAdapter(table)) {
                    continue;
                }
                // Variable 
                table.PropertyCache.TAMAdapterPropName = nameHandler.GetTableAdapterPropName(table.GeneratorDataComponentClassName);
                table.PropertyCache.TAMAdapterVarName = nameHandler.GetTableAdapterVarName(table.PropertyCache.TAMAdapterPropName); 
                string adapterVariableName = table.PropertyCache.TAMAdapterVarName; 

                CodeMemberField adapterVariable = CodeGenHelper.FieldDecl(CodeGenHelper.Type(table.GeneratorDataComponentClassName), adapterVariableName); 
                dataComponentClass.Members.Add(adapterVariable);

                // Property
                CodeMemberProperty adapterProperty = CodeGenHelper.PropertyDecl(CodeGenHelper.Type(table.GeneratorDataComponentClassName), table.PropertyCache.TAMAdapterPropName, MemberAttributes.Public | MemberAttributes.Final); 

                //EditorAttribute 
                adapterProperty.CustomAttributes.Add( 
                    CodeGenHelper.AttributeDecl(
                        "System.ComponentModel.EditorAttribute", 
                        CodeGenHelper.Str(adapterPropertyEditor + ", " + AssemblyRef.MicrosoftVSDesigner),
                        CodeGenHelper.Str("System.Drawing.Design.UITypeEditor")
                    )
                ); 

                adapterProperty.GetStatements.Add( 
                    CodeGenHelper.Return( 
                        CodeGenHelper.ThisField(adapterVariableName)
                    ) 
                );

                // If TAM has only one adapter and it is one we are going to set,
                // there is no connection conflict issue. 
                //
                //\\ if (this.TableAdapterInstanceCount == 1 && this._customerTA != null){ 
                //\\    Set ... 
                //\\    return
                //\\ } 
                adapterProperty.SetStatements.Add(
                    CodeGenHelper.If(
                        CodeGenHelper.And(
                            CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(adapterVariableName)), 
                            CodeGenHelper.EQ(
                                CodeGenHelper.ThisProperty(TAMNameHandler.TableAdapterInstanceCountProperty), 
                                CodeGenHelper.Primitive(1) 
                            )
                        ), 
                        new CodeStatement[]{
                            CodeGenHelper.Assign(CodeGenHelper.ThisField(adapterVariableName),
                                                 CodeGenHelper.Argument("value")
                            ), 
                            CodeGenHelper.Return()
                        } 
                    ) 
                );
                //\\ If value != null && !MatchConnection(value.Connection) 
                //\\      throw argument exception
                //"The connection of " + table.GeneratorDataComponentClassName + " does not match that of the TableAdapterManager";
                //string errorMsg = SR.GetString(SR.DD_E_TableAdapterConnectionInvalid, table.GeneratorDataComponentClassName);
                // Note, we decided not to add a new resource string here but just throw an argument exception 
                adapterProperty.SetStatements.Add(
                    CodeGenHelper.If( 
                        CodeGenHelper.And( 
                            CodeGenHelper.IdIsNotNull(CodeGenHelper.Variable("value")),
                            CodeGenHelper.EQ( 
                                CodeGenHelper.MethodCall(CodeGenHelper.This(), TAMNameHandler.MatchTAConnectionMethod, CodeGenHelper.Property(CodeGenHelper.Variable("value"), "Connection")),
                                CodeGenHelper.Primitive(false)
                            )
                        ), 
                        new CodeStatement[]{
                            new CodeThrowExceptionStatement(CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(ArgumentException)), new CodeExpression[] {CodeGenHelper.Str(SR.GetString(SR.CG_TableAdapterManagerNeedsSameConnString))})) 
                        } 
                    )
                ); 

                ///  this.TA = value
                adapterProperty.SetStatements.Add(
                    CodeGenHelper.Assign(CodeGenHelper.ThisField(adapterVariableName), 
                                         CodeGenHelper.Argument("value")
                    ) 
                ); 
                dataComponentClass.Members.Add(adapterProperty);
            } 
        }

        /// <summary>
        /// Connection member Example: 
        /// Private _connection As IDbConnection
        /// Friend Property Connection() As IDbConnection 
        ///     Get 
        ///         If (_connection IsNot Nothing) Then
        ///           Return _connection 
        ///         End If
        ///         If (Me._customersTableAdapter IsNot Nothing AndAlso Me._customersTableAdapter.Connection IsNot Nothing) Then
        ///           Return Me._customersTableAdapter.Connection
        ///         End If 
        ///         If (Me._ordersTableAdapter IsNot Nothing AndAlso Me._ordersTableAdapter.Connection IsNot Nothing) Then
        ///             Return Me._ordersTableAdapter.Connection 
        ///         End If 
        ///         Return Nothing
        ///     End Get 
        ///     Set(ByVal value As IDbConnection)
        ///         _connection = value
        ///     End Set
        /// End Property 
        /// <param name="dataComponentClass"></param>
        private void AddConnectionMembers(CodeTypeDeclaration dataComponentClass) { 
            string connectionVariableName = TAMNameHandler.ConnectionVar; 
            CodeMemberField connectionVariable = CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(IDbConnection)), connectionVariableName);
            dataComponentClass.Members.Add(connectionVariable); 

            // Property
            CodeMemberProperty property = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(typeof(IDbConnection)), TAMNameHandler.ConnectionProperty, MemberAttributes.Public | MemberAttributes.Final);
            property.CustomAttributes.Add( 
                CodeGenHelper.AttributeDecl("System.ComponentModel.Browsable", CodeGenHelper.Primitive(false))
            ); 
 
            //\\ If (_connection IsNot Nothing) Then
            //\\    Return _connection 
            //\\ End If
            property.GetStatements.Add(
                CodeGenHelper.If(CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(connectionVariableName)),
                    CodeGenHelper.Return(CodeGenHelper.ThisField(connectionVariableName)) 
                )
            ); 
 
            foreach (DesignTable table in dataSource.DesignTables) {
                if (!this.CanAddTableAdapter(table)) { 
                    continue;
                }

                //\\  If (Me._customersTableAdapter IsNot Nothing AndAlso Me._customersTableAdapter.Connection IsNot Nothing) Then 
                //\\     Return Me._customersTableAdapter.Connection
                //\\  End If 
                string adapterVariableName = table.PropertyCache.TAMAdapterVarName; 
                property.GetStatements.Add(
                    CodeGenHelper.If( 
                        CodeGenHelper.And(
                            CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(adapterVariableName)),
                            CodeGenHelper.IdIsNotNull(CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Connection"))
                        ), 
                        CodeGenHelper.Return(
                            CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Connection") 
                        ) 
                    )
                ); 
            }

            //\\    Return null
            property.GetStatements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Primitive(null))
            ); 
 
            //\\ Set(ByVal value As IDbConnection)
            //\\    _connection = value 
            //\\ End Set
            property.SetStatements.Add(
                CodeGenHelper.Assign(CodeGenHelper.ThisField(connectionVariableName),
                                     CodeGenHelper.Argument("value") 
                )
            ); 
 
            dataComponentClass.Members.Add(property);
        } 

        /// <summary>
        /// Create a TableAdapterInstanceCount property
        /// Example: 
        /// Public Property TableAdapterInstanceCount() As integer
        ///     Get 
        ///         count = 0; 
        ///         If (Me._customersTableAdapter IsNot Nothing ) Then
        ///             count += 1 
        ///         End If
        ///         If (Me._ordersTableAdapter IsNot Nothing) Then
        ///             count += 1
        ///         End If 
        ///         Return count
        ///     End Get 
        /// End Property 
        /// <param name="dataComponentClass"></param>
        private void AddTableAdapterCountMembers(CodeTypeDeclaration dataComponentClass) { 
            string countVariableName = "count";
            CodeExpression countVariable = CodeGenHelper.Variable(countVariableName);

            CodeMemberProperty property = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(typeof(int)), TAMNameHandler.TableAdapterInstanceCountProperty, MemberAttributes.Public | MemberAttributes.Final); 
            property.CustomAttributes.Add(
                CodeGenHelper.AttributeDecl("System.ComponentModel.Browsable", CodeGenHelper.Primitive(false)) 
            ); 

            //\\ count = 0; 
            property.GetStatements.Add(
                CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(int)), countVariableName, CodeGenHelper.Primitive(0))
            );
 
            foreach (DesignTable table in dataSource.DesignTables) {
                if (!this.CanAddTableAdapter(table)) { 
                    continue; 
                }
                //\\If (Me._customersTableAdapter IsNot Nothing ) Then 
                //\\    count += 1
                //\\End If
                string adapterVariableName = table.PropertyCache.TAMAdapterVarName;
                property.GetStatements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(adapterVariableName)) 
                        , 
                        CodeGenHelper.Assign(countVariable, CodeGenHelper.BinOperator(countVariable, CodeBinaryOperatorType.Add, CodeGenHelper.Primitive(1)))
                    ) 
                );
            }

            //\\    Return count 
            property.GetStatements.Add(
                CodeGenHelper.Return(countVariable) 
            ); 
            dataComponentClass.Members.Add(property);
        } 

        /// <summary>
        /// Add SortSelfRefRows methods. Code Example:
        /// protected virtual void SortSelfReferencedRows(System.Data.DataRow[] rows, DataRelation relation, bool childFirst) { 
        ///     System.Array.Sort<DataRow>(rows, new SelfReferenceComparer(relation, childFirst));
        /// } 
        /// </summary> 
        /// <param name="dataComponentClass"></param>
        private void AddSortSelfRefRows(CodeTypeDeclaration dataComponentClass) { 
            string rowsStr = "rows";
            string relationStr = "relation";
            string childFirstStr = "childFirst";
 
            CodeMemberMethod method =
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), 
                TAMNameHandler.SortSelfRefRowsMethod, 
                MemberAttributes.Family
            ); 

            method.Parameters.AddRange(
                new CodeParameterDeclarationExpression[]{
                    CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRow), 1), rowsStr), 
                    CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRelation)), relationStr),
                    CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(bool)), childFirstStr) 
                } 
            );
 
            CodeMethodReferenceExpression sortMethodRef = new CodeMethodReferenceExpression(CodeGenHelper.GlobalTypeExpr("System.Array"), "Sort", CodeGenHelper.GlobalType(typeof(DataRow)));
            CodeMethodInvokeExpression sortMethodExpr =
                new CodeMethodInvokeExpression(
                    sortMethodRef, 
                    new CodeExpression[]{
                        CodeGenHelper.Argument(rowsStr), 
                        CodeGenHelper.New( 
                            CodeGenHelper.Type(TAMNameHandler.SelfRefComparerClass),
                            new CodeExpression[]{ 
                                CodeGenHelper.Argument(relationStr),
                                CodeGenHelper.Argument(childFirstStr)
                            }
                        ) 
                   }
                ); 
 
            //\\ System.Array.Sort<DataRow>(rows, new SelfReferenceComparer(relation, childFirst));
            method.Statements.Add(CodeGenHelper.Stm(sortMethodExpr)); 
            dataComponentClass.Members.Add(method);
        }

        /// <summary> 
        /// Add SelfReferenceComparer class. Code Example:
        /// private class SelfReferenceComparer : System.Collections.Generic.IComparer<DataRow> { 
        ///     private readonly DataRelation m_relation; 
        ///     private readonly int m_childFirst;
        ///     internal SelfReferenceComparer(DataRelation relation, bool childFirst) { 
        ///         this.m_relation = relation;
        ///         this.m_childFirst = childFirst ? -1 : 1;
        ///     }
        ///     // return (0 if row1 == row2), (-1 if row1 < row2) or (1 if row1 > row2) 
        ///     int System.Collections.Generic.IComparer<DataRow>.Compare(DataRow row1, DataRow row2) {
        ///     if (object.ReferenceEquals(row1, row2)) { 
        ///         return 0; // either row1 && row2 are same instance or null 
        ///     }
        ///     if (null == row1) { 
        ///         return -1; // null row1 is < non-null row2
        ///     }
        ///     if (null == row2) {
        ///         return 1; // non-null row1 > null row2 
        ///     }
        ///     // Is row1 the child or grandchild of row2 
        ///     if (this.IsChildAndParent(row1, row2)) { 
        ///         return this._childFirst;
        ///     } 
        ///     // Is row2 the child or grandchild of row1
        ///     if (this.IsChildAndParent(row2, row1)) {
        ///         return (-1 * this._childFirst);
        ///     } 
        ///     return 0;
        /// } 
        /// </summary> 
        /// <param name="dataComponentClass"></param>
        private void AddSelfRefComparer(CodeTypeDeclaration dataComponentClass) { 
            string relationVarStr = "_relation";
            string childFirstVarStr = "_childFirst";

            //\\private class SelfReferenceComparer : System.Collections.Generic.IComparer<DataRow> { 
            CodeTypeDeclaration codeType = CodeGenHelper.Class(
                TAMNameHandler.SelfRefComparerClass, false, TypeAttributes.NestedPrivate 
            ); 
            CodeTypeReference icomparerInterface = CodeGenHelper.GlobalGenericType(
                "System.Collections.Generic.IComparer", typeof(DataRow) 
            );
            // To generate a class in Visual Basic that does not inherit from a base type,
            // but that does implement one or more interfaces, you must include Object as the first item in
            // the BaseTypes collection. from <http://msdn2.microsoft.com/en-us/library/system.codedom.codetypedeclaration.basetypes.aspx> 
            codeType.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(object)));
            codeType.BaseTypes.Add(icomparerInterface); 
 
            codeType.Comments.Add(CodeGenHelper.Comment("Used to sort self-referenced table's rows", true));
 
            dataComponentClass.Members.Add(codeType);

            //\\ private DataRelation m_relation;
            //\\ private int m_childFirst; 
            codeType.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(DataRelation)), relationVarStr));
            codeType.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(int)), childFirstVarStr)); 
 
            //\\ internal SelfReferenceComparer(DataRelation relation, bool childFirst) {
            //\\    this.m_relation = relation; 
            //\\    this.m_childFirst = childFirst ? -1 : 1;
            //\\ }
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Assembly);
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRelation)), "relation")); 
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(bool)), "childFirst"));
            constructor.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.ThisField(relationVarStr), CodeGenHelper.Argument("relation"))); 
            constructor.Statements.Add(CodeGenHelper.If( 
                CodeGenHelper.Argument("childFirst"),
                CodeGenHelper.Assign(CodeGenHelper.ThisField(childFirstVarStr), CodeGenHelper.Primitive(-1)), 
                CodeGenHelper.Assign(CodeGenHelper.ThisField(childFirstVarStr), CodeGenHelper.Primitive(1))
            ));
            codeType.Members.Add(constructor);
 
            // ---- isChildAndParentMethod -------------
 
            //\\private bool IsChildAndParent(global::System.Data.DataRow child, global::System.Data.DataRow parent){ 
            //\\    System.Data.DataRow newParent = child.GetParentRow(this._relation);
            //\\    while (newParent != null && newParent != child && newParent != parent){ 
            //\\        newParent = newParent.GetParentRow(this._relation);
            //\\    }
            //\\    if (newParent == null){
            //\\        newParent = child.GetParentRow(this._relation, System.Data.DataRowVersion.Original  ); 
            //\\        while (newParent != null && newParent != child && newParent != parent){
            //\\            newParent = newParent.GetParentRow(this._relation, System.Data.DataRowVersion.Original  ); 
            //\\        } 
            //\\    }
            //\\    if (object.ReferenceEquals(newParent, parent)){ 
            //\\        return true;
            //\\    }
            //\\    return false;
            //\\} 
            string childStr = "child";
            string parentStr = "parent"; 
            string newParentStr = "newParent"; 
            string isChildAndParentStr = "IsChildAndParent";
            CodeMemberMethod isChildAndParentMethod = CodeGenHelper.MethodDecl( 
                CodeGenHelper.GlobalType(typeof(bool)),
                isChildAndParentStr,
                MemberAttributes.Private
            ); 
            isChildAndParentMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRow)), childStr));
            isChildAndParentMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRow)), parentStr)); 
 
            isChildAndParentMethod.Statements.Add(
                CodeGenHelper.MethodCallStm( 
                    CodeGenHelper.GlobalTypeExpr(typeof(System.Diagnostics.Debug)),
                    "Assert",
                    CodeGenHelper.IdIsNotNull(CodeGenHelper.Argument(childStr))
                ) 
            );
            isChildAndParentMethod.Statements.Add( 
                CodeGenHelper.MethodCallStm( 
                    CodeGenHelper.GlobalTypeExpr(typeof(System.Diagnostics.Debug)),
                    "Assert", 
                    CodeGenHelper.IdIsNotNull(CodeGenHelper.Argument(parentStr))
                )
            );
 
            //\\System.Data.DataRow newParent = child.GetParentRow(this._relation);
            isChildAndParentMethod.Statements.Add( 
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(DataRow)),
                    newParentStr, 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Argument(childStr),
                        "GetParentRow",
                        new CodeExpression[] { 
                            CodeGenHelper.ThisField(relationVarStr),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(DataRowVersion)),"Default") 
                        } 
                   )
                ) 
            );
            //\\    while (newParent != null && newParent != child && newParent != parent){
            //\\        newParent = newParent.GetParentRow(this._relation);
            //\\    } 
            CodeIterationStatement whileStatement = new CodeIterationStatement();
            whileStatement.TestExpression = CodeGenHelper.And( 
                CodeGenHelper.IdIsNotNull(CodeGenHelper.Variable(newParentStr)), 
                CodeGenHelper.And(
                    CodeGenHelper.ReferenceNotEquals(CodeGenHelper.Variable(newParentStr), CodeGenHelper.Argument(childStr)), 
                    CodeGenHelper.ReferenceNotEquals(CodeGenHelper.Variable(newParentStr), CodeGenHelper.Argument(parentStr))
                )
            );
            whileStatement.InitStatement = new CodeSnippetStatement(); 
            whileStatement.IncrementStatement = new CodeSnippetStatement();
            whileStatement.Statements.Add( 
                CodeGenHelper.Assign( 
                    CodeGenHelper.Variable(newParentStr),
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Variable(newParentStr),
                        "GetParentRow",
                        new CodeExpression[] {
                            CodeGenHelper.ThisField(relationVarStr), 
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(DataRowVersion)),"Default")
                        } 
                   ) 
                )
            ); 
            isChildAndParentMethod.Statements.Add(whileStatement);

            //\\    if (newParent == null){
            //\\        newParent = child.GetParentRow(this._relation, System.Data.DataRowVersion.Original  ); 
            //\\        while (newParent != null && newParent != child && newParent != parent){
            //\\            newParent = newParent.GetParentRow(this._relation, System.Data.DataRowVersion.Original  ); 
            //\\        } 
            //\\    }
            whileStatement = new CodeIterationStatement(); 
            whileStatement.TestExpression = CodeGenHelper.And(
                CodeGenHelper.IdIsNotNull(CodeGenHelper.Variable(newParentStr)),
                CodeGenHelper.And(
                    CodeGenHelper.ReferenceNotEquals(CodeGenHelper.Variable(newParentStr), CodeGenHelper.Argument(childStr)), 
                    CodeGenHelper.ReferenceNotEquals(CodeGenHelper.Variable(newParentStr), CodeGenHelper.Argument(parentStr))
                ) 
            ); 
            whileStatement.InitStatement = CodeGenHelper.Assign(
                    CodeGenHelper.Variable(newParentStr), 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Argument(childStr),
                        "GetParentRow",
                        new CodeExpression[] { 
                            CodeGenHelper.ThisField(relationVarStr),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(DataRowVersion)),"Original") 
                        } 
                    )
            ); 
            whileStatement.IncrementStatement = new CodeSnippetStatement();
            whileStatement.Statements.Add(CodeGenHelper.Assign(
                    CodeGenHelper.Variable(newParentStr),
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Argument(newParentStr),
                        "GetParentRow", 
                        new CodeExpression[] { 
                            CodeGenHelper.ThisField(relationVarStr),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(DataRowVersion)),"Original") 
                        }
                    )
            ));
 
            isChildAndParentMethod.Statements.Add(CodeGenHelper.If(
                CodeGenHelper.IdIsNull(CodeGenHelper.Variable(newParentStr)), 
                whileStatement 
            ));
 
            //\\    if (object.ReferenceEquals(newParent, parent)){
            //\\        return true;
            //\\    }
            //\\    return false; 
            isChildAndParentMethod.Statements.Add(CodeGenHelper.If(
                CodeGenHelper.ReferenceEquals(CodeGenHelper.Variable(newParentStr), CodeGenHelper.Argument(parentStr)), 
                CodeGenHelper.Return(CodeGenHelper.Primitive(true)) 
            ));
            isChildAndParentMethod.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Primitive(false))); 

            codeType.Members.Add(isChildAndParentMethod);

            //--------compareMethod----------------- 

            //\\  // return (0 if row1 == row2), (-1 if row1 < row2) or (1 if row1 > row2) 
            //\\  int System.Collections.Generic.IComparer<DataRow>.Compare(DataRow row1, DataRow row2) { 
            string row1Str = "row1";
            string row2Str = "row2"; 
            CodeMemberMethod compareMethod = CodeGenHelper.MethodDecl(
                CodeGenHelper.GlobalType(typeof(int)),
                "Compare",
                 MemberAttributes.Public | MemberAttributes.Final 
            );
            compareMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRow)), row1Str)); 
            compareMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(DataRow)), row2Str)); 
            compareMethod.ImplementationTypes.Add(icomparerInterface);
            codeType.Members.Add(compareMethod); 

            //\\ if (object.ReferenceEquals(row1, row2)) {
            //\\    return 0; // either row1 && row2 are same instance or null
            //\\ } 
            compareMethod.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.ReferenceEquals(CodeGenHelper.Argument(row1Str), CodeGenHelper.Argument(row2Str)), 
                    CodeGenHelper.Return(CodeGenHelper.Primitive(0))
                ) 
            );
            //\\ if (null == row1) {
            //\\    return -1; // null row1 is < non-null row2
            //\\ } 
            //\\ if (null == row2) {
            //\\    return 1; // non-null row1 > null row2 
            //\\ } 
            compareMethod.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdIsNull(CodeGenHelper.Argument(row1Str)),
                    CodeGenHelper.Return(CodeGenHelper.Primitive(-1))
                )
            ); 
            compareMethod.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.IdIsNull(CodeGenHelper.Argument(row2Str)), 
                    CodeGenHelper.Return(CodeGenHelper.Primitive(1))
                ) 
            );

            //\\ // is row1 the child of row2
            compareMethod.Statements.Add( 
                new CodeSnippetStatement() // empty line
            ); 
            compareMethod.Statements.Add( 
                new CodeCommentStatement("Is row1 the child or grandchild of row2")
            ); 
            //\\if (this.IsChildAndParent(row1, row2)) {
            //\\    return this._childFirst;
            //\\}
            compareMethod.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.This(), 
                        isChildAndParentStr,
                        new CodeExpression[]{ 
                            CodeGenHelper.Argument(row1Str),
                            CodeGenHelper.Argument(row2Str)
                        }
                    ), 
                    CodeGenHelper.Return(CodeGenHelper.ThisField(childFirstVarStr))
                ) 
            ); 
            //\\ // is row2 the child of row1
            compareMethod.Statements.Add( 
                new CodeSnippetStatement() // empty line
            );
            compareMethod.Statements.Add(
                new CodeCommentStatement("Is row2 the child or grandchild of row1") 
            );
            //\\ if (row2.GetParentRow(m_relation) == row1) { 
            //\\     -return this.m_childFirst; 
            //\\ }
            compareMethod.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.This(),
                        isChildAndParentStr, 
                        new CodeExpression[]{
                            CodeGenHelper.Argument(row2Str), 
                            CodeGenHelper.Argument(row1Str) 
                        }
                    ), 
                    CodeGenHelper.Return(CodeGenHelper.BinOperator(CodeGenHelper.Primitive(-1), CodeBinaryOperatorType.Multiply, CodeGenHelper.ThisField(childFirstVarStr)))
                )
            );
            //\\ return 0; 
            compareMethod.Statements.Add(
                CodeGenHelper.Return(CodeGenHelper.Primitive(0)) 
            ); 
        }
 
        /// <summary>
        /// Add MatchTableAdapterConnection method. Code Example:
        /// virtual protected MatchTableAdapterConnection(IDbConnection inputConnection)  {
        ///   if (this._conection != null){ 
        ///       return true;
        ///   } 
        ///   if (this.Connection == null || inputConnection == null) 
        ///     return true
        ///   } 
        /// }
        /// </summary>
        /// <param name="dataComponentClass"></param>
        private void AddMatchTableAdapterConnection(CodeTypeDeclaration dataComponentClass) { 
            string inputConnStr = "inputConnection";
 
            CodeMemberMethod method = 
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(bool)),
                TAMNameHandler.MatchTAConnectionMethod, 
                MemberAttributes.Family
            );
            CodeTypeReference connTypeRef = CodeGenHelper.GlobalType(typeof(IDbConnection));
            CodeParameterDeclarationExpression dataSetPara = CodeGenHelper.ParameterDecl(connTypeRef, inputConnStr); 
            method.Parameters.Add(dataSetPara);
 
            //\\if (this._connection != null) 
            //\\    return true
            method.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(TAMNameHandler.ConnectionVar)),
                    CodeGenHelper.Return(CodeGenHelper.Primitive(true))
                ) 
            );
            //\\if (this.Connection == null || inputConnection == null) 
            //\\    return true 
            method.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.Or(
                        CodeGenHelper.IdIsNull(CodeGenHelper.ThisProperty(TAMNameHandler.ConnectionProperty)),
                        CodeGenHelper.IdIsNull(CodeGenHelper.Argument(inputConnStr))
                    ), 
                    CodeGenHelper.Return(CodeGenHelper.Primitive(true))
                ) 
            ); 
            //\\ if (string.Equals(a, b, StringComparison.Ordinal)
            method.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.GlobalTypeExpr(typeof(string)),
                        "Equals", 
                        new CodeExpression[]{
                            CodeGenHelper.Property( 
                                    CodeGenHelper.ThisProperty(TAMNameHandler.ConnectionProperty), 
                                    "ConnectionString"
                            ), 
                            CodeGenHelper.Property(CodeGenHelper.Argument(inputConnStr),"ConnectionString"),
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.StringComparison)), "Ordinal")
                        }
                    ), 
                    CodeGenHelper.Return(CodeGenHelper.Primitive(true))
                ) 
            ); 

            method.Statements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Primitive(false))
            );

            dataComponentClass.Members.Add(method); 
        }
 
        /// <summary> 
        /// The UpdateAll method
        /// </summary> 
        /// <param name="dataComponentClass"></param>
        private void AddUpdateAll(CodeTypeDeclaration dataComponentClass) {
            string dataSetStr = "dataSet";
            string backupDataSetStr = "backupDataSet"; 
            string deletedRowsStr = "deletedRows";
            string addedRowsStr = "addedRows"; 
            string updatedRowsStr = "updatedRows"; 
            string resultStr = "result";
            string workConnStr = "workConnection"; 
            string workTransStr = "workTransaction";
            string workConnOpenedStr = "workConnOpened";
            string allChangedRowsStr = "allChangedRows";
            string allAddedRowsStr = "allAddedRows"; 
            string adaptersWithACDUStr = "adaptersWithAcceptChangesDuringUpdate";
            string revertConnectionsVar = "revertConnections"; 
            CodeExpression resultExp = CodeGenHelper.Variable(resultStr); 

            CodeMemberMethod method = 
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(int)),
                TAMNameHandler.UpdateAllMethod,
                MemberAttributes.Public
            ); 

            string dataSourceTypeRefName = this.dataSourceType.Name; 
 
            // DDBugs 126914: Use fully-qualified names for datasets
            if (this.codeGenerator.DataSetNamespace != null) { 
                dataSourceTypeRefName = CodeGenHelper.GetTypeName(this.codeGenerator.CodeProvider, this.codeGenerator.DataSetNamespace, dataSourceTypeRefName);
            }

            CodeTypeReference dataSourceTypeRef = CodeGenHelper.Type(dataSourceTypeRefName); 

            CodeParameterDeclarationExpression dataSetPara = CodeGenHelper.ParameterDecl(dataSourceTypeRef, dataSetStr); 
            method.Parameters.Add(dataSetPara); 

            method.Comments.Add(CodeGenHelper.Comment("Update all changes to the dataset.", true)); 

            //-----------------------------------------------------------------------------
            //\\If (dataSet Is Nothing) Then
            //\\    Throw New System.ArgumentNullException("dataSet") 
            //\\End If
            method.Statements.Add( 
                CodeGenHelper.If( 
                    CodeGenHelper.IdIsNull(CodeGenHelper.Argument(dataSetStr)),
                    CodeGenHelper.Throw(CodeGenHelper.GlobalType(typeof(ArgumentNullException)), dataSetStr) 
                )
            );

            //----------------------------------------------------------------------------- 
            //\\if (dataSet.HasChanges() == fase)
            //\\    return 0 
            method.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.EQ( 
                        CodeGenHelper.MethodCall(CodeGenHelper.Argument(dataSetStr), "HasChanges"),
                        CodeGenHelper.Primitive(false)
                    ),
                    CodeGenHelper.Return(CodeGenHelper.Primitive(0)) 
                )
            ); 
 
            //----------------------------------------------------------------------------
            //\\Dim workConnection As IDbConnection = Me.Connection 
            method.Statements.Add(
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(IDbConnection)), workConnStr, CodeGenHelper.ThisProperty("Connection")
                ) 
            );
            //\\If workConnection Is Nothing Then 
            //\\    throw new ApplicationException(No connection) 
            //\\End If
            method.Statements.Add( 
                CodeGenHelper.If(CodeGenHelper.IdIsNull(CodeGenHelper.Variable(workConnStr)),
                    CodeGenHelper.Throw(
                        CodeGenHelper.GlobalType(typeof(ApplicationException)),
                        SR.GetString(SR.CG_TableAdapterManagerHasNoConnection)) 
                )
            ); 
            //\\Dim workConnOpened As Boolean = False 
            method.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(bool)), workConnOpenedStr, CodeGenHelper.Primitive(false)
                )
            );
            //\\If ((workConnection.State And Global.System.Data.ConnectionState.Closed) _ 
            //\\         = Global.System.Data.ConnectionState.Closed) Then
            //\\    workCnnection.Open() 
            //\\    workConnOpened = True 
            //\\End If
            method.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.EQ(
                        CodeGenHelper.BitwiseAnd(
                            CodeGenHelper.Property(CodeGenHelper.Variable(workConnStr), "State"), 
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Closed")
                        ), 
                        CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.ConnectionState)), "Closed") 
                    ),
                    new CodeStatement[]{ 
                        CodeGenHelper.MethodCallStm(CodeGenHelper.Variable(workConnStr),"Open"),
                        CodeGenHelper.Assign(CodeGenHelper.Variable(workConnOpenedStr), CodeGenHelper.Primitive(true))
                    }
                ) 
            );
            //\\Dim workTransaction As IDbTransaction = workConnection.BeginTransaction() 
            method.Statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(IDbTransaction)), 
                    workTransStr,
                    CodeGenHelper.MethodCall(CodeGenHelper.Variable(workConnStr), "BeginTransaction")
                )
            ); 
            //\\ if (workTransaction == null){Throw}
            method.Statements.Add( 
                CodeGenHelper.If(CodeGenHelper.IdIsNull(CodeGenHelper.Variable(workTransStr)), 
                    CodeGenHelper.Throw(
                        CodeGenHelper.GlobalType(typeof(ApplicationException)), 
                        SR.GetString(SR.CG_TableAdapterManagerNotSupportTransaction))
                )
            );
            //\\Dim allChangedRows As New System.Collections.Generic.List(Of System.Data.DataRow)() 
            CodeTypeReference typeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(DataRow));
            method.Statements.Add( 
                CodeGenHelper.VariableDecl(typeRef, allChangedRowsStr, CodeGenHelper.New(typeRef, new CodeExpression[] { })) 
            );
            //\\Dim allAddedRows As New System.Collections.Generic.List(Of System.Data.DataRow)() 
            method.Statements.Add(
                CodeGenHelper.VariableDecl(typeRef, allAddedRowsStr, CodeGenHelper.New(typeRef, new CodeExpression[] { }))
            );
            //\\Dim adaptersWithACDU As New System.Collections.Generic.List(Of System.Data.Common.DataAdapter)() 
            typeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(System.Data.Common.DataAdapter));
            method.Statements.Add( 
                CodeGenHelper.VariableDecl(typeRef, adaptersWithACDUStr, CodeGenHelper.New(typeRef, new CodeExpression[] { })) 
            );
 

            //-----------------------------------------------------------------------------
            //\\System.Collections.Generic.IDictionary<object, System.Data.IDbConnection> revertConnections =
            //\\    new System.Collections.Generic.Dictionary<object, System.Data.IDbConnection>(); 
            CodeTypeReference genericTypeRef = new CodeTypeReference("System.Collections.Generic.Dictionary",
                CodeGenHelper.GlobalType(typeof(object)), 
                CodeGenHelper.GlobalType(typeof(System.Data.IDbConnection)) 
            );
            genericTypeRef.Options = CodeTypeReferenceOptions.GlobalReference; 
            method.Statements.Add(
                CodeGenHelper.VariableDecl(
                    genericTypeRef,
                    revertConnectionsVar, 
                    CodeGenHelper.New(genericTypeRef, new CodeExpression[] { })
                ) 
            ); 

            //---------------------------------------------------------------------------------------- 
            //\\int result = 0
            method.Statements.Add(
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.Type(typeof(int)), resultStr, CodeGenHelper.Primitive(0) 
                )
            ); 
 
            //----------------------------------------------------------------------------
            //\\Dim backupDataSet As DataSet = nothing 
            //\\If (this.BackUpDataSetBeforeUpdate) then
            //\\   backupDataSet = new DataSet
            //\\   backupDataSet.Merge(dataSet)
            //\\Endif 
            method.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(DataSet)), 
                    backupDataSetStr,
                    CodeGenHelper.Primitive(null) 
                )
            );
            method.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.ThisProperty(TAMNameHandler.BackupDataSetBeforeUpdateProperty),
                    new CodeStatement[]{ 
                        CodeGenHelper.Assign( 
                            CodeGenHelper.Variable(backupDataSetStr),
                            CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(DataSet)),new CodeExpression[]{}) 
                        ),
                        CodeGenHelper.MethodCallStm(CodeGenHelper.Variable(backupDataSetStr), "Merge", CodeGenHelper.Argument(dataSetStr))
                    }
                ) 
            );
 
            //--------------------------------------------------------------------------------------- 
            // Big try block
            //\\try { 
            //\\}
            //\\catch {
            //\\}
            //\\finally { 
            //\\}
            List<CodeStatement> tryUpdates = new List<CodeStatement>(); 
 
            tryUpdates.Add(new CodeCommentStatement("---- Prepare for update -----------\r\n"));
            //\\If (Not (Me._customersTableAdapter) Is Nothing) Then 
            //\\    revertConnections.Add(Me._customersTableAdapter, Me._customersTableAdapter.Connection)
            //\\    Me._customersTableAdapter.Connection = CType(workConnection, Global.System.Data.SqlClient.SqlConnection)
            //\\    Me._customersTableAdapter.Transaction = CType(workTransaction, Global.System.Data.SqlClient.SqlTransaction)
            //\\    If (_customersTableAdapter.Adapter.AcceptChangesDuringUpdate) Then 
            //\\ _customersTableAdapter.Adapter.AcceptChangesDuringUpdate = False
            //\\ adaptersWithACDU.Add(_customersTableAdapter.Adapter) 
            //\\    End If 
            //\\End If
            foreach (DesignTable table in dataSource.DesignTables) { 
                if (!CanAddTableAdapter(table)) {
                    continue;
                }
                string adapterVariableName = table.PropertyCache.TAMAdapterVarName; 

                CodeStatement assignTransactionStm = null; 
                if (table.PropertyCache.TransactionType != null) { 
                    //\\    _customersTableAdapter.Transaction = CType(workTransaction, Global.System.Data.SqlClient.SqlTransaction)
                    assignTransactionStm = CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Transaction"),
                        CodeGenHelper.Cast(CodeGenHelper.GlobalType(table.PropertyCache.TransactionType),
                            CodeGenHelper.Variable(workTransStr)
                        ) 
                    );
                } 
                else { 
                    assignTransactionStm = new CodeCommentStatement("Note: The TableAdapter does not have the Transaction property.");
                } 

                CodeStatement adaptersWithACDUStatement = null;
                // We need to make sure the _customersTableAdapter.Adapter is DataAdapter
                if (table.PropertyCache.AdapterType != null && typeof(DataAdapter).IsAssignableFrom(table.PropertyCache.AdapterType)) { 
                    //\\ If (_customersTableAdapter.Adapter.AcceptChangesDuringUpdate) Then
                    //\\    _customersTableAdapter.Adapter.AcceptChangesDuringUpdate = False 
                    //\\    adaptersWithACDU.Add(_customersTableAdapter.Adapter) 
                    //\\  End If
                    adaptersWithACDUStatement = CodeGenHelper.If( 
                        CodeGenHelper.Property(CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Adapter"), "AcceptChangesDuringUpdate"),
                        new CodeStatement[]{
                            CodeGenHelper.Assign(
                                CodeGenHelper.Property(CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName),"Adapter"),"AcceptChangesDuringUpdate"), 
                                CodeGenHelper.Primitive(false)
                            ), 
                            CodeGenHelper.Stm(CodeGenHelper.MethodCall( 
                                CodeGenHelper.Variable(adaptersWithACDUStr),
                                "Add", 
                                CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName),"Adapter")
                            ))
                        }
                    ); 
                }
                else { 
                    adaptersWithACDUStatement = new CodeCommentStatement("Note: Adapter is not a DataAdapter, so AcceptChangesDuringUpdate cannot be set to false."); 
                }
 
                tryUpdates.Add(
                    CodeGenHelper.If(
                        CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(adapterVariableName)),
                        new CodeStatement[]{ 
                            //\\revertConnections.Add(this._customersAdapter, this._customersAdapter.Connection);
                            CodeGenHelper.Stm( 
                                CodeGenHelper.MethodCall( 
                                    CodeGenHelper.Variable(revertConnectionsVar),
                                    "Add", 
                                    new CodeExpression[]{CodeGenHelper.ThisField(adapterVariableName),
                                        CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Connection")
                                    }
                                ) 
                            ),
                            //\\_customersTableAdapter.Connection = CType(workConnection, Global.System.Data.SqlClient.SqlConnection) 
                            CodeGenHelper.Assign( 
                                CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Connection"),
                                CodeGenHelper.Cast(CodeGenHelper.GlobalType(table.PropertyCache.ConnectionType), 
                                    CodeGenHelper.Variable(workConnStr)
                                )
                            ),
                            assignTransactionStm, 
                            adaptersWithACDUStatement
                        } 
                    ) 
                );
 
            }

            //-----start update----------------------------------------
            DataTable[] orderedTables = TableAdapterManagerHelper.GetUpdateOrder(dataSource.DataSet); 

            AddUpdateUpdatedMethod(dataComponentClass, orderedTables, dataSetPara, dataSetStr, resultStr, updatedRowsStr, allChangedRowsStr, allAddedRowsStr); 
            AddUpdateInsertedMethod(dataComponentClass, orderedTables, dataSetPara, dataSetStr, resultStr, addedRowsStr, allAddedRowsStr); 
            AddUpdateDeletedMethod(dataComponentClass, orderedTables, dataSetPara, dataSetStr, resultStr, deletedRowsStr, allChangedRowsStr);
            AddRealUpdatedRowsMethod(dataComponentClass, updatedRowsStr, allAddedRowsStr); 

            tryUpdates.Add(new CodeCommentStatement("\r\n---- Perform updates -----------\r\n"));

            //\\If (Me.UpdateOrder = UpdateOrderOption.UpdateInsertDelete) Then 
            //\\    result = Me.UpdateUpdatedRows(dataSet, allChangedRows, Nothing)
            //\\    result = Me.UpdateInsertedRows(dataSet, allAddedRows) 
            //\\ElseIf (Me.UpdateOrder = UpdateOrderOption.InsertUpdateDelete) Then 
            //\\    result = Me.UpdateInsertedRows(dataSet, allAddedRows)
            //\\    result = Me.UpdateUpdatedRows(dataSet, allChangedRows, allAddedRows) 
            //\\End If


 
            CodeStatement insertStm = CodeGenHelper.Assign(
                resultExp, 
                CodeGenHelper.BinOperator( 
                    resultExp,
                    CodeBinaryOperatorType.Add, 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.This(),
                        TAMNameHandler.UpdateInsertedRowsMethod,
                        new CodeExpression[] { CodeGenHelper.Argument(dataSetStr), CodeGenHelper.Variable(allAddedRowsStr) } 
                    )
                ) 
            ); 

            CodeStatement updateStm = CodeGenHelper.Assign( 
                resultExp, CodeGenHelper.BinOperator(
                    resultExp,
                    CodeBinaryOperatorType.Add,
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.This(),
                        TAMNameHandler.UpdateUpdatedRowsMethod, 
                        new CodeExpression[] { CodeGenHelper.Argument(dataSetStr), CodeGenHelper.Variable(allChangedRowsStr), CodeGenHelper.Variable(allAddedRowsStr) } 
                    )
                ) 
            );


            // Update and Insert 
            tryUpdates.Add(CodeGenHelper.If(
                CodeGenHelper.EQ( 
                    CodeGenHelper.ThisProperty(TAMNameHandler.UpdateOrderOptionProperty), 
                    CodeGenHelper.Field(CodeGenHelper.TypeExpr(CodeGenHelper.Type(TAMNameHandler.UpdateOrderOptionEnum)), TAMNameHandler.UpdateOrderOptionEnumUID)
                ), 
                new CodeStatement[] { updateStm, insertStm },
                new CodeStatement[] { insertStm, updateStm }
            ));
 
            // Delete last
            tryUpdates.Add( 
                CodeGenHelper.Assign(resultExp, CodeGenHelper.BinOperator(resultExp, CodeBinaryOperatorType.Add, 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.This(), 
                        TAMNameHandler.UpdateDeletedRowsMethod,
                        new CodeExpression[] { CodeGenHelper.Argument(dataSetStr), CodeGenHelper.Variable(allChangedRowsStr) }
                    )
                )) 
            );
 
            //---------------------------------------------------------------------------------- 
            //\\workTransaction.Commit()
            tryUpdates.Add(new CodeCommentStatement("\r\n---- Commit updates -----------\r\n")); 
            tryUpdates.Add(
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable(workTransStr), "Commit" 
                    )
                ) 
            ); 
            //\\For Each row As DataRow In allAddedRows
            //\\    row.AcceptChanges() 
            //\\Next
            tryUpdates.Add(
                this.HandleForEachRowInList(allAddedRowsStr, new string[] { "AcceptChanges" })
            ); 
            //\\For Each row As DataRow In allChangedRows
            //\\    row.AcceptChanges() 
            //\\Next 
            tryUpdates.Add(
                this.HandleForEachRowInList(allChangedRowsStr, new string[] { "AcceptChanges" }) 
            );

            //---catch block-------------------------------------------------------------
            //\\workTransaction.Rollback() 
            CodeCatchClause catchUpdate = new CodeCatchClause();
            catchUpdate.Statements.Add( 
                CodeGenHelper.MethodCall( 
                    CodeGenHelper.Variable(workTransStr),
                    "Rollback" 
                )
            );
            catchUpdate.Statements.Add(new CodeCommentStatement("---- Restore the dataset -----------"));
            //\\If (Me.BackupDataSetBeforeUpdate) Then 
            //\\    dataSet.Clear()
            //\\    dataSet.Merge(backupDataSet) 
            //\\Else 
            //\\  For Each row As DataRow In allAddedRows
            //\\    row.AcceptChanges() 
            //\\    row.SetAdded()
            //\\  Next
            //\\Endif
            catchUpdate.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.ThisProperty(TAMNameHandler.BackupDataSetBeforeUpdateProperty), 
                    new CodeStatement[]{ 
                        CodeGenHelper.MethodCallStm(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Diagnostics.Debug)), 
                            "Assert",
                            CodeGenHelper.IdIsNotNull(CodeGenHelper.Variable(backupDataSetStr))
                        ),
                        CodeGenHelper.MethodCallStm(CodeGenHelper.Argument(dataSetStr), "Clear"), 
                        CodeGenHelper.MethodCallStm(CodeGenHelper.Argument(dataSetStr),"Merge", CodeGenHelper.Variable(backupDataSetStr))
                    }, 
                    new CodeStatement[]{ 
                        this.HandleForEachRowInList(allAddedRowsStr, new string[] { "AcceptChanges", "SetAdded" })
                    } 
                )
            );

            catchUpdate.CatchExceptionType = CodeGenHelper.GlobalType(typeof(System.Exception)); 
            catchUpdate.LocalName = "ex";
            catchUpdate.Statements.Add(new CodeThrowExceptionStatement(CodeGenHelper.Variable("ex"))); 
 
            //
            //---Final statements--------------------------------------------------------- 
            //
            //\\finally {
            List<CodeStatement> finalUpdates = new List<CodeStatement>();
            //\\    If workConnOpened Then 
            //\\      workConnection.Close()
            //\\    End If 
            finalUpdates.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.Variable(workConnOpenedStr), 
                    CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.Variable(workConnStr), "Close"))
                )
            );
            //\\    if ((_customersTableAdapter != null)) { 
            //\\        _customersTableAdapter.Connection = ((System.Data.SqlClient.SqlConnection)(revertConnections[_customersTableAdapter]));
            //\\        _customersTableAdapter.Transaction = null; 
            //\\    } 
            foreach (DesignTable table in dataSource.DesignTables) {
                if (!CanAddTableAdapter(table)) { 
                    continue;
                }
                string adapterVariableName = table.PropertyCache.TAMAdapterVarName;
                CodeStatement assignTransactionStm = null; 
                if (table.PropertyCache.TransactionType != null) {
                    assignTransactionStm = CodeGenHelper.Assign( 
                        CodeGenHelper.Property(CodeGenHelper.ThisField(adapterVariableName), "Transaction"), 
                        CodeGenHelper.Primitive(null)
                    ); 
                }
                else {
                    assignTransactionStm = new CodeCommentStatement("Note: No Transaction property of the TableAdapter");
                } 

                finalUpdates.Add( 
                    CodeGenHelper.If( 
                        CodeGenHelper.IdIsNotNull(CodeGenHelper.ThisField(adapterVariableName)),
                        new CodeStatement[]{ 
                            //\\ this._customersAdapter.Connection = revertConnections[_customersAdapter] as System.Data.SqlClient.SqlConnection;
                            CodeGenHelper.Assign(
                                CodeGenHelper.Property(
                                    CodeGenHelper.ThisField(adapterVariableName), 
                                    "Connection"
                                ), 
                                CodeGenHelper.Cast( 
                                    CodeGenHelper.GlobalType(table.PropertyCache.ConnectionType),
                                    CodeGenHelper.Indexer( 
                                        CodeGenHelper.Variable(revertConnectionsVar),
                                        CodeGenHelper.ThisField(adapterVariableName)
                                    )
                                ) 
                            ),
                            assignTransactionStm 
                        } 
                    )
                ); 
            }

            //\\For Each adapter As Data.Common.DataAdapter In adaptersWithACDU
            //\\     adapter.AcceptChangesDuringUpdate = True 
            //\\ Next
            finalUpdates.Add(this.RestoreAdaptersWithACDU(adaptersWithACDUStr)); 
 
            //---Add the try block ----------------------------
            method.Statements.Add( 
                CodeGenHelper.Try(
                    tryUpdates.ToArray(),
                    new CodeCatchClause[] { catchUpdate },
                    finalUpdates.ToArray() 
                )
            ); 
            //--------------------------------------------------------------------------------------- 
            //\\return result
            method.Statements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Variable(resultStr))
            );
            dataComponentClass.Members.Add(method);
        } 

        /// <summary> 
        /// Create UpdateInsertdRows method 
        ///----insert in forward order-----
        ///\\if ((_customersTableAdapter != null)) { 
        ///\\    System.Data.DataRow[] insertedRows = changes.Customers.Select(null, null, global::System.Data.DataViewRowState.Added);
        ///\\    _customersTableAdapter.Update(insertedRows);
        ///\\}
        /// </summary> 
        /// <param name="dataComponentClass"></param>
        /// <param name="orderedTables"></param> 
        /// <param name="dataSetPara"></param> 
        /// <param name="dataSetStr"></param>
        /// <param name="resultStr"></param> 
        /// <param name="addedRowsStr"></param>
        /// <param name="allAddedRowsStr"></param>
        private void AddUpdateInsertedMethod(CodeTypeDeclaration dataComponentClass, DataTable[] orderedTables, CodeParameterDeclarationExpression dataSetPara, string dataSetStr, string resultStr, string addedRowsStr, string allAddedRowsStr) {
            CodeMemberMethod method = 
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(int)),
                TAMNameHandler.UpdateInsertedRowsMethod, 
                MemberAttributes.Private 
            );
 
            CodeTypeReference dataRowListTypeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(DataRow));
            CodeParameterDeclarationExpression dataRowListPara = CodeGenHelper.ParameterDecl(dataRowListTypeRef, allAddedRowsStr);

            method.Parameters.AddRange( 
                new CodeParameterDeclarationExpression[]{
                    dataSetPara, 
                    dataRowListPara 
                }
            ); 

            //\\int result = 0
            method.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.Type(typeof(int)), resultStr, CodeGenHelper.Primitive(0)
                ) 
            ); 

            method.Comments.Add(CodeGenHelper.Comment("Insert rows in top-down order.", true)); 
            for (int i = 0; i < orderedTables.Length; i++) {
                DesignTable table = dataSource.DesignTables[orderedTables[i]];
                if (!CanAddTableAdapter(table)) {
                    continue; 
                }
                method.Statements.Add(this.AddUpdateAllTAUpdate(table, dataSetStr, resultStr, addedRowsStr, allAddedRowsStr, "Added", null)); 
            } 

            //\\return result 
            method.Statements.Add(
                CodeGenHelper.Return(CodeGenHelper.Variable(resultStr))
            );
 
            dataComponentClass.Members.Add(method);
        } 
 
        private void AddUpdateDeletedMethod(CodeTypeDeclaration dataComponentClass, DataTable[] orderedTables, CodeParameterDeclarationExpression dataSetPara, string dataSetStr, string resultStr, string deletedRowsStr, string allChangedRowsStr) {
            CodeMemberMethod method = 
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(int)),
                TAMNameHandler.UpdateDeletedRowsMethod,
                MemberAttributes.Private
            ); 

            CodeTypeReference dataRowListTypeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(DataRow)); 
            CodeParameterDeclarationExpression dataRowListPara = CodeGenHelper.ParameterDecl(dataRowListTypeRef, allChangedRowsStr); 

            method.Parameters.AddRange( 
                new CodeParameterDeclarationExpression[]{
                    dataSetPara,
                    dataRowListPara
                } 
            );
 
            method.Comments.Add(CodeGenHelper.Comment("Delete rows in bottom-up order.", true)); 

            //\\int result = 0 
            method.Statements.Add(
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.Type(typeof(int)), resultStr, CodeGenHelper.Primitive(0)
                ) 
            );
 
            //\\if ((_customersTableAdapter != null)) { 
            //\\    System.Data.DataRow[] deletedRows = changes.Customers.Select(null, null, global::System.Data.DataViewRowState.Deleted);
            //\\    _customersTableAdapter.Update(deletedRows); 
            //\\}
            for (int i = orderedTables.Length - 1; i >= 0; i--) {
                DesignTable table = dataSource.DesignTables[orderedTables[i]];
                if (!CanAddTableAdapter(table)) { 
                    continue;
                } 
                method.Statements.Add(this.AddUpdateAllTAUpdate(table, dataSetStr, resultStr, deletedRowsStr, allChangedRowsStr, "Deleted", null)); 
            }
 
            //\\return result
            method.Statements.Add(
                CodeGenHelper.Return(CodeGenHelper.Variable(resultStr))
            ); 

            dataComponentClass.Members.Add(method); 
        } 

        private void AddUpdateUpdatedMethod(CodeTypeDeclaration dataComponentClass, DataTable[] orderedTables, CodeParameterDeclarationExpression dataSetPara, string dataSetStr, string resultStr, string updatedRowsStr, string allChangedRowsStr, string allAddedRowsStr) { 
            CodeMemberMethod method =
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(int)),
                TAMNameHandler.UpdateUpdatedRowsMethod,
                MemberAttributes.Private 
            );
 
            CodeTypeReference dataRowListTypeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(DataRow)); 
            CodeParameterDeclarationExpression dataRowListPara = CodeGenHelper.ParameterDecl(dataRowListTypeRef, allChangedRowsStr);
            CodeParameterDeclarationExpression addedDataRowListPara = CodeGenHelper.ParameterDecl(dataRowListTypeRef, allAddedRowsStr); 

            method.Parameters.AddRange(
                new CodeParameterDeclarationExpression[]{
                    dataSetPara, 
                    dataRowListPara,
                    addedDataRowListPara 
                } 
            );
 
            method.Comments.Add(CodeGenHelper.Comment("Update rows in top-down order.", true));

            //\\int result = 0
            method.Statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.Type(typeof(int)), resultStr, CodeGenHelper.Primitive(0) 
                ) 
            );
 
            //\\    updatedRows = GetRealUpdatedRows(updatedRows, allAddedRows)
            //\\    if (this._customersAdapter != null) {
            //\\        result = result + this._customersAdapter.Update(changes.Customers);
            //\\    } 
            for (int i = 0; i < orderedTables.Length; i++) {
                DesignTable table = dataSource.DesignTables[orderedTables[i]]; 
                if (!CanAddTableAdapter(table)) { 
                    continue;
                } 
                method.Statements.Add(this.AddUpdateAllTAUpdate(table, dataSetStr, resultStr, updatedRowsStr, allChangedRowsStr, "ModifiedCurrent", allAddedRowsStr));
            }

            //\\return result 
            method.Statements.Add(
                CodeGenHelper.Return(CodeGenHelper.Variable(resultStr)) 
            ); 

            dataComponentClass.Members.Add(method); 
        }

        /// <summary>
        /// Used to filter out inserted rows, that become updated rows after calling TA.Update 
        ///If (updatedRows IsNot Nothing AndAlso updatedRows.Length > 0 AndAlso allAddedRows IsNot Nothing AndAlso allAddedRows.Count > 0) Then
        ///    Dim realUpdatedRows As New Global.System.Collections.Generic.List(Of Global.System.Data.DataRow) 
        ///    For Each row As DataRow In updatedRows 
        ///        If (Not allAddedRows.Contains(row)) Then
        ///            realUpdatedRows.Add(row) 
        ///        End If
        ///    Next
        ///    If (realUpdatedRows.Count < updatedRows.Length) Then
        ///        updatedRows = realUpdatedRows.ToArray() 
        ///    End If
        ///End If 
        /// </summary> 
        /// <param name="dataComponentClass"></param>
        /// <param name="updatedRowsStr"></param> 
        /// <param name="allAddedRowsStr"></param>
        private void AddRealUpdatedRowsMethod(CodeTypeDeclaration dataComponentClass, string updatedRowsStr, string allAddedRowsStr) {
            string realUpdatedRowsStr = "realUpdatedRows";
 
            CodeMemberMethod method =
                CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(DataRow), 1), 
                TAMNameHandler.GetRealUpdatedRowsMethod, 
                MemberAttributes.Private
            ); 

            CodeTypeReference dataRowListTypeRef = CodeGenHelper.GlobalGenericType("System.Collections.Generic.List", typeof(DataRow));
            CodeParameterDeclarationExpression addedDataRowListPara = CodeGenHelper.ParameterDecl(dataRowListTypeRef, allAddedRowsStr);
 
            CodeTypeReference dataRowArrryTypeRef = CodeGenHelper.GlobalType(typeof(DataRow), 1);
            CodeParameterDeclarationExpression dataRowArrayPara = CodeGenHelper.ParameterDecl(dataRowArrryTypeRef, updatedRowsStr); 
 
            method.Comments.Add(CodeGenHelper.Comment("Remove inserted rows that become updated rows after calling TableAdapter.Update(inserted rows) first", true));
 
            method.Parameters.AddRange(
                new CodeParameterDeclarationExpression[]{
                    dataRowArrayPara,
                    addedDataRowListPara 
                }
            ); 
 
            //\\If (updatedRows Is Nothing OrElse updatedRows.Length < 1
            //\\ return updatedRows 
            method.Statements.Add(
                CodeGenHelper.If(CodeGenHelper.Or(
                        CodeGenHelper.IdIsNull(CodeGenHelper.Argument(updatedRowsStr)),
                        CodeGenHelper.Less(CodeGenHelper.Property(CodeGenHelper.Argument(updatedRowsStr), "Length"), CodeGenHelper.Primitive(1)) 
                    ),
                    CodeGenHelper.Return(CodeGenHelper.Variable(updatedRowsStr)) 
               ) 
            );
            //\\If (allAddedRows Is Nothing OrElse allAddedRows.Count < 1 
            //\\ return updatedRows
            method.Statements.Add(
                CodeGenHelper.If(CodeGenHelper.Or(
                        CodeGenHelper.IdIsNull(CodeGenHelper.Argument(allAddedRowsStr)), 
                        CodeGenHelper.Less(CodeGenHelper.Property(CodeGenHelper.Argument(allAddedRowsStr), "Count"), CodeGenHelper.Primitive(1))
                    ), 
                    CodeGenHelper.Return(CodeGenHelper.Variable(updatedRowsStr)) 
               )
            ); 

            //\\    Dim realUpdatedRows As New Global.System.Collections.Generic.List(Of Global.System.Data.DataRow)
            //\\    For Each row As DataRow In updatedRows
            //\\        If (Not allAddedRows.Contains(row)) Then 
            //\\            realUpdatedRows.Add(row)
            //\\        End If 
            //\\    Next 
            method.Statements.Add(
                CodeGenHelper.VariableDecl(dataRowListTypeRef, realUpdatedRowsStr, CodeGenHelper.New(dataRowListTypeRef, new CodeExpression[] { })) 
            );
            string rowStr = "row";
            CodeStatement[] forStms = new CodeStatement[2];
            forStms[0] = CodeGenHelper.VariableDecl( 
                            CodeGenHelper.GlobalType(typeof(DataRow)),
                            rowStr, 
                            CodeGenHelper.Indexer(CodeGenHelper.Variable(updatedRowsStr), CodeGenHelper.Variable("i")) 
                        );
            forStms[1] = CodeGenHelper.If( 
                            CodeGenHelper.EQ(CodeGenHelper.MethodCall(CodeGenHelper.Argument(allAddedRowsStr), "Contains", CodeGenHelper.Variable(rowStr)), CodeGenHelper.Primitive(false)),
                            CodeGenHelper.MethodCallStm(CodeGenHelper.Variable(realUpdatedRowsStr), "Add", CodeGenHelper.Variable(rowStr))
                        );
 
            method.Statements.Add(this.GetForLoopItoCount(CodeGenHelper.Property(CodeGenHelper.Argument(updatedRowsStr), "Length"), forStms));
 
            //\\Return realUpdatedRows.ToArray 
            method.Statements.Add(
                CodeGenHelper.Return(CodeGenHelper.MethodCall(CodeGenHelper.Variable(realUpdatedRowsStr), "ToArray")) 
            );

            dataComponentClass.Members.Add(method);
        } 

 
        /// <summary> 
        /// Helper function used by AddUpdateAll
        /// </summary> 
        private CodeStatement AddUpdateAllTAUpdate(DesignTable table, string dataSetStr, string resultStr, string updateRowsStr, string allUpdateRowsStr, string rowState, string allAddedRowsStr) {
            Debug.Assert(table != null);
            Debug.Assert(StringUtil.NotEmptyAfterTrim(dataSetStr));
            Debug.Assert(StringUtil.NotEmptyAfterTrim(resultStr)); 
            Debug.Assert(StringUtil.NotEmptyAfterTrim(updateRowsStr));
            Debug.Assert(StringUtil.NotEmptyAfterTrim(allUpdateRowsStr)); 
            Debug.Assert(StringUtil.NotEmptyAfterTrim(rowState)); 

            string adapterVariableName = table.PropertyCache.TAMAdapterVarName; 
            CodeStatement[] updateStatementsArray =
                new CodeStatement[]{
                    CodeGenHelper.Assign(
                        CodeGenHelper.Variable(resultStr), 
                        CodeGenHelper.BinOperator(CodeGenHelper.Variable(resultStr),
                            CodeBinaryOperatorType.Add, 
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.ThisField(adapterVariableName),
                                "Update", 
                                CodeGenHelper.Variable(updateRowsStr)
                            )
                        )
                    ), 
                    CodeGenHelper.Stm(CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable(allUpdateRowsStr),"AddRange",CodeGenHelper.Variable(updateRowsStr) 
                    )) 
                };
 
            // Handle self referenced relation
            DataRelation[] selfRefs = TableAdapterManagerHelper.GetSelfRefRelations(table.DataTable);
            if (selfRefs != null && selfRefs.Length > 0) {
                bool childFirst = StringUtil.EqualValue("Deleted", rowState, true); 
                List<CodeStatement> updateStatementsList = new List<CodeStatement>(updateStatementsArray.Length + selfRefs.Length);
                for (int i = 0; i < selfRefs.Length; i++) { 
                    if (i > 0) { 
                        updateStatementsList.Add(
                            new CodeCommentStatement("Note: More than one self-referenced relation found.  The generated code may not work correctly.") 
                        );
                    }
                    updateStatementsList.Add(
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall( 
                            CodeGenHelper.This(),
                            TAMNameHandler.SortSelfRefRowsMethod, 
                            new CodeExpression[]{ 
                                CodeGenHelper.Variable(updateRowsStr),
                                CodeGenHelper.Indexer( 
                                    CodeGenHelper.Property(CodeGenHelper.Argument(dataSetStr),"Relations"),
                                    CodeGenHelper.Str(selfRefs[i].RelationName)
                                ),
                                CodeGenHelper.Primitive(childFirst) 
                            }
                        )) 
                    ); 
                }
 
                updateStatementsList.AddRange(updateStatementsArray);
                updateStatementsArray = updateStatementsList.ToArray();
            }
 
            List<CodeStatement> ifUpdateBody = new List<CodeStatement>(3);
            //\\Dim updatedRows() As Global.System.Data.DataRow = dataSet.Customers.Select(Nothing, Nothing, Global.System.Data.DataViewRowState.ModifiedCurrent) 
            ifUpdateBody.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(System.Data.DataRow), 1), 
                    updateRowsStr,
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Argument(dataSetStr), table.GeneratorTablePropName),
                        "Select", 
                        new CodeExpression[]{
                            CodeGenHelper.Primitive(null), 
                            CodeGenHelper.Primitive(null), 
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.Data.DataViewRowState)), rowState)
                        } 
                    )
                )
            );
 
            //\\updatedRows = GetRealUpdatedRows(updatedRows, allAddedRows)
            if (StringUtil.NotEmptyAfterTrim(allAddedRowsStr)) { 
                ifUpdateBody.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Argument(updateRowsStr), 
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.This(),
                            TAMNameHandler.GetRealUpdatedRowsMethod,
                            new CodeExpression[]{ 
                                CodeGenHelper.Argument(updateRowsStr),
                                CodeGenHelper.Argument(allAddedRowsStr) 
                            } 
                        )
                    ) 
                );
            }

            //\\    if (updateRows != null && updateRows.Length > 0) 
            //\\        result = result + _customersTableAdapter.Update(updateRows);
            //\\        allUpdatedRows.AddRange(updateRows) 
            ifUpdateBody.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.And( 
                        CodeGenHelper.IdNotEQ(CodeGenHelper.Variable(updateRowsStr), CodeGenHelper.Primitive(null)),
                        CodeGenHelper.Less(CodeGenHelper.Primitive(0),
                            CodeGenHelper.Property(CodeGenHelper.Variable(updateRowsStr), "Length")
                        ) 
                    ),
                    updateStatementsArray 
                ) 
            );
 
            //\\If (Not (Me._customersTableAdapter) Is Nothing) Then
            CodeStatement result =
                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ( 
                        CodeGenHelper.ThisField(adapterVariableName), CodeGenHelper.Primitive(null)
                    ), 
                    ifUpdateBody.ToArray() 
                );
            return result; 
        }

        /// <summary>
        /// Helper method to add variable as well as Property 
        /// example:
        /// private System.Data.IDbTransaction _transaction; 
        /// [global::System.Diagnostics.DebuggerNonUserCodeAttribute()] 
        /// public System.Data.IDbTransaction Transaction {
        ///     get { 
        ///         return this._transaction;
        ///     }
        /// }
        /// </summary> 
        /// <param name="codeType"></param>
        /// <param name="memberAttributes"></param> 
        /// <param name="propertyType"></param> 
        /// <param name="propertyName"></param>
        /// <param name="variableName"></param> 
        /// <param name="getOnly"></param>
        private void AddVariableAndProperty(CodeTypeDeclaration codeType, MemberAttributes memberAttributes, CodeTypeReference propertyType, string propertyName, string variableName, bool getOnly) {
            Debug.Assert(codeType != null);
            Debug.Assert(propertyType != null); 
            Debug.Assert(StringUtil.NotEmptyAfterTrim(propertyName));
            Debug.Assert(StringUtil.NotEmptyAfterTrim(variableName)); 
 
            codeType.Members.Add(
                CodeGenHelper.FieldDecl( 
                    propertyType,
                    variableName
                )
            ); 

            CodeMemberProperty property = 
                CodeGenHelper.PropertyDecl( 
                    propertyType,
                    propertyName, 
                    memberAttributes
            );
            property.GetStatements.Add(
                    CodeGenHelper.Return( 
                        CodeGenHelper.ThisField(variableName)
                    ) 
            ); 
            if (!getOnly) {
                property.SetStatements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.ThisField(variableName),
                        CodeGenHelper.Argument("value")
                    ) 
                );
            } 
            codeType.Members.Add(property); 
        }
 
        /// <summary>
        /// Check to see if a TableAdapter is qualified as a property in TableAdapterManager
        /// To be qualified, a TableAdapter needs to have update commands and the connection is accessable.
        /// </summary> 
        /// <param name="table"></param>
        /// <returns></returns> 
        private bool CanAddTableAdapter(DesignTable table) { 
            Debug.Assert(table != null, "table is null");
 
            if (table != null && table.HasAnyUpdateCommand) {
                MemberAttributes connectionModifier = ((DesignConnection)table.Connection).Modifier & MemberAttributes.AccessMask;
                if (connectionModifier == MemberAttributes.FamilyOrAssembly
                   || connectionModifier == MemberAttributes.Assembly 
                   || connectionModifier == MemberAttributes.Public
                   || connectionModifier == MemberAttributes.FamilyAndAssembly) { 
                    return true; 
                }
            } 
            return false;
        }

        /// <summary> 
        /// Generated code that restore the DataAdapter.AcceptChangeDuringUpdate property
        /// For Each adapter As Data.Common.DataAdapter In adaptersWithACDU 
        ///      adapter.AcceptChangesDuringUpdate = True 
        /// Next
        /// </summary> 
        /// <param name="listStr"></param>
        /// <param name="methods"></param>
        /// <returns></returns>
        private CodeStatement RestoreAdaptersWithACDU(string listStr) { 
            Debug.Assert(StringUtil.NotEmptyAfterTrim(listStr));
 
            CodeStatement[] forStms = new CodeStatement[2]; 
            forStms[0] = CodeGenHelper.VariableDecl(
                            CodeGenHelper.GlobalType(typeof(DataAdapter)), 
                            "adapter",
                            CodeGenHelper.Indexer(CodeGenHelper.Variable("adapters"), CodeGenHelper.Variable("i"))
                        );
            forStms[1] = CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.Variable("adapter"), "AcceptChangesDuringUpdate"), CodeGenHelper.Primitive(true)); 

            CodeStatement result = 
                CodeGenHelper.If( 
                     CodeGenHelper.Less(CodeGenHelper.Primitive(0), CodeGenHelper.Property(CodeGenHelper.Variable(listStr), "Count")),
                     new CodeStatement[]{ 
                        CodeGenHelper.VariableDecl(
                            CodeGenHelper.GlobalType(typeof(DataAdapter),1),
                            "adapters",
                            this.NewArray(CodeGenHelper.GlobalType(typeof(DataAdapter),1),CodeGenHelper.Property(CodeGenHelper.Variable(listStr),"Count")) 
                        ),
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.Variable(listStr),"CopyTo",CodeGenHelper.Variable("adapters"))), 
                        this.GetForLoopItoCount( 
                            CodeGenHelper.Property(CodeGenHelper.Variable("adapters"), "Length"),
                            forStms 
                        )
                    }
                 );
            return result; 
        }
 
        /// <summary> 
        /// Helper method to generate for each code like
        /// // For Each row As DataRow In allAddedRows 
        /// //    methods
        /// // Next
        /// </summary>
        /// <param name="listStr"></param> 
        /// <param name="methods"></param>
        /// <returns></returns> 
        private CodeStatement HandleForEachRowInList(string listStr, string[] methods) { 
            Debug.Assert(methods != null && methods.Length > 0);
            Debug.Assert(StringUtil.NotEmptyAfterTrim(listStr)); 

            CodeStatement[] forStms = new CodeStatement[methods.Length + 1];
            forStms[0] = CodeGenHelper.VariableDecl(
                            CodeGenHelper.GlobalType(typeof(DataRow)), 
                            "row",
                            CodeGenHelper.Indexer(CodeGenHelper.Variable("rows"), CodeGenHelper.Variable("i")) 
                        ); 
            for (int i = 0; i < methods.Length; i++) {
                forStms[i + 1] = CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.Variable("row"), methods[i])); 
            }

            CodeStatement result =
                CodeGenHelper.If( 
                     CodeGenHelper.Less(CodeGenHelper.Primitive(0), CodeGenHelper.Property(CodeGenHelper.Variable(listStr), "Count")),
                     new CodeStatement[]{ 
                        CodeGenHelper.VariableDecl( 
                            CodeGenHelper.GlobalType(typeof(DataRow),1),
                            "rows", 
                            this.NewArray(CodeGenHelper.GlobalType(typeof(DataRow),1),CodeGenHelper.Property(CodeGenHelper.Variable(listStr),"Count"))
                        ),
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.Variable(listStr),"CopyTo",CodeGenHelper.Variable("rows"))),
                        this.GetForLoopItoCount( 
                            CodeGenHelper.Property(CodeGenHelper.Variable("rows"), "Length"),
                            forStms 
                        ) 
                    }
                 ); 
            return result;
        }

        /// <summary> 
        /// Helper to generate the For loop code
        /// Get for(int i=0; i<Count;i++){ 
        /// } 
        /// </summary>
        /// <returns></returns> 
        private CodeStatement GetForLoopItoCount(CodeExpression countExp, CodeStatement[] forStms) {
            return this.GetForLoopItoCount("i", countExp, forStms);
        }
 
        /// <summary>
        /// Helper to generate the For loop code 
        /// </summary> 
        /// <param name="iStr">the i variable in the loop</param>
        /// <param name="countExp">end condition</param> 
        /// <param name="forStms">body</param>
        /// <returns>code</returns>
        private CodeStatement GetForLoopItoCount(string iStr, CodeExpression countExp, CodeStatement[] forStms) {
            CodeStatement forInit = CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(int)), iStr, CodeGenHelper.Primitive(0)); 
            CodeStatement forIncrement = CodeGenHelper.Assign(
                                        CodeGenHelper.Variable(iStr), 
                                        CodeGenHelper.BinOperator( 
                                            CodeGenHelper.Variable(iStr),
                                            CodeBinaryOperatorType.Add, 
                                            CodeGenHelper.Primitive(1)
                                        )
                                     );
            CodeExpression forTest = CodeGenHelper.Less( 
                                        CodeGenHelper.Variable(iStr),
                                        countExp 
                                     ); 
            return CodeGenHelper.ForLoop(forInit, forTest, forIncrement, forStms);
        } 

        /// <summary>
        /// Helper function to generate an array with size
        /// </summary> 
        /// <param name="type"></param>
        /// <param name="size"></param> 
        /// <returns></returns> 
        private CodeExpression NewArray(CodeTypeReference type, CodeExpression size) { return new CodeArrayCreateExpression(type, size); }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
