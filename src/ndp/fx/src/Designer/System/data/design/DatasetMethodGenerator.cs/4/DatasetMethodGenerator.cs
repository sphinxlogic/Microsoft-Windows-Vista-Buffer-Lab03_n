//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.CodeDom;
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 
    using System.Data.Common;
    using System.Xml; 
    using System.Xml.Schema; 
    using System.Xml.Serialization;
 
    internal sealed class DatasetMethodGenerator {
        private TypedDataSourceCodeGenerator codeGenerator = null;
        private DesignDataSource dataSource = null;
        private DataSet dataSet = null; 
        private CodeMemberMethod initExpressionsMethod = null;
        private static PropertyDescriptor namespaceProperty = TypeDescriptor.GetProperties(typeof(DataSet))["Namespace"]; 
        private static PropertyDescriptor localeProperty = TypeDescriptor.GetProperties(typeof(DataSet))["Locale"]; 
        private static PropertyDescriptor caseSensitiveProperty = TypeDescriptor.GetProperties(typeof(DataSet))["CaseSensitive"];
 
        internal DatasetMethodGenerator(TypedDataSourceCodeGenerator codeGenerator, DesignDataSource dataSource) {
            this.codeGenerator = codeGenerator;
            this.dataSource = dataSource;
            this.dataSet = dataSource.DataSet; 
        }
 
        internal void AddMethods(CodeTypeDeclaration dataSourceClass) { 
            AddSchemaSerializationModeMembers(dataSourceClass);
            initExpressionsMethod = InitExpressionsMethod(); 
            dataSourceClass.Members.Add(PublicConstructor());
            dataSourceClass.Members.Add( DeserializingConstructor() );
            dataSourceClass.Members.Add( InitializeDerivedDataSet() );
            dataSourceClass.Members.Add( CloneMethod(initExpressionsMethod) ); 
            dataSourceClass.Members.Add( ShouldSerializeTablesMethod() );
            dataSourceClass.Members.Add( ShouldSerializeRelationsMethod() ); 
            dataSourceClass.Members.Add( ReadXmlSerializableMethod() ); 
            dataSourceClass.Members.Add( GetSchemaSerializableMethod() );
            dataSourceClass.Members.Add( InitVarsParamLess() ); 
            CodeMemberMethod initClassMethod = null;
            CodeMemberMethod initVarsMethod = null;
            InitClassAndInitVarsMethods(out initClassMethod, out initVarsMethod);
            dataSourceClass.Members.Add( initVarsMethod ); 
            dataSourceClass.Members.Add( initClassMethod );
            AddShouldSerializeSingleTableMethods(dataSourceClass); 
            dataSourceClass.Members.Add( SchemaChangedMethod() ); 
            dataSourceClass.Members.Add( GetTypedDataSetSchema() );
            dataSourceClass.Members.Add( TablesProperty() ); 
            dataSourceClass.Members.Add( RelationsProperty() );
            if (initExpressionsMethod != null) {
                dataSourceClass.Members.Add(initExpressionsMethod);
            } 
        }
 
        private void AddSchemaSerializationModeMembers(CodeTypeDeclaration dataSourceClass) { 
            CodeMemberField schemaSerializationModeField = CodeGenHelper.FieldDecl(
                CodeGenHelper.GlobalType(typeof(System.Data.SchemaSerializationMode)), 
                "_schemaSerializationMode",
                CodeGenHelper.Field(
                    CodeGenHelper.GlobalTypeExpr(typeof(System.Data.SchemaSerializationMode)),
                    this.dataSource.SchemaSerializationMode.ToString() 
                )
            ); 
            dataSourceClass.Members.Add(schemaSerializationModeField); 

            CodeMemberProperty schemaSerializationModeProperty = CodeGenHelper.PropertyDecl( 
                CodeGenHelper.GlobalType(typeof(System.Data.SchemaSerializationMode)),
                "SchemaSerializationMode",
                MemberAttributes.Public | MemberAttributes.Override
            ); 
            schemaSerializationModeProperty.CustomAttributes.Add(
                CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.BrowsableAttribute).FullName, CodeGenHelper.Primitive(true)) 
            ); 
            schemaSerializationModeProperty.CustomAttributes.Add(
                CodeGenHelper.AttributeDecl( 
                    typeof(System.ComponentModel.DesignerSerializationVisibilityAttribute).FullName,
                    CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(DesignerSerializationVisibility)), "Visible")
                )
            ); 

 
            schemaSerializationModeProperty.GetStatements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), "_schemaSerializationMode"))
            ); 

            schemaSerializationModeProperty.SetStatements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), "_schemaSerializationMode"), 
                    CodeGenHelper.Argument("value")
                ) 
            ); 

            dataSourceClass.Members.Add(schemaSerializationModeProperty); 
        }

        private CodeConstructor PublicConstructor() {
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Public); 
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "BeginInit"));
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitClass")); 
            constructor.Statements.Add(CodeGenHelper.VariableDecl( 
                                            CodeGenHelper.GlobalType(typeof(CollectionChangeEventHandler)),
                                            "schemaChangedHandler", 
                                            new CodeDelegateCreateExpression(
                                                CodeGenHelper.GlobalType(typeof(CollectionChangeEventHandler)),
                                                CodeGenHelper.This(),
                                                "SchemaChanged"))); 
            constructor.Statements.Add(new System.CodeDom.CodeAttachEventStatement(
                                            new CodeEventReferenceExpression( 
                                                CodeGenHelper.Property(CodeGenHelper.Base(),"Tables"), 
                                                "CollectionChanged"),
                                            CodeGenHelper.Variable("schemaChangedHandler"))); 
            constructor.Statements.Add(new System.CodeDom.CodeAttachEventStatement(
                                            new CodeEventReferenceExpression(
                                                CodeGenHelper.Property(CodeGenHelper.Base(),"Relations"),
                                                "CollectionChanged"), 
                                            CodeGenHelper.Variable("schemaChangedHandler")));
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "EndInit")); 
 
            //\\ this.InitExpressions();
            // needs to be called after EndInit to ensure expressions with related tables are initialized correctly 
            if (initExpressionsMethod != null) {
                constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitExpressions"));
            }
 
            return constructor;
        } 
 
        private CodeConstructor DeserializingConstructor() {
            //\\ protected NorthwindDataSet(SerializationInfo info, StreamingContext context) : base(info, context, false) { 
            //\\    if ((this.IsBinarySerialized(info, context) == true)) {
            //\\         this.InitVars(false);
            //\\        System.ComponentModel.CollectionChangeEventHandler schemaChangedHandler1 = new System.ComponentModel.CollectionChangeEventHandler(this.SchemaChanged);
            //\\        this.Tables.CollectionChanged += schemaChangedHandler1; 
            //\\        this.Relations.CollectionChanged += schemaChangedHandler1;
            //\\        return; 
            //\\    } 
            //\\    string strSchema = ((string)(info.GetValue("XmlSchema", typeof(string))));
            //\\    if(DetermineSchemaSerializationMode(info, context) == SchemaSerializationMode.SerializeSchema) { 
            //\\        System.Data.DataSet ds = new System.Data.DataSet();
            //\\        ds.EnforceConstraints = false;
            //\\        ds.ReadXmlSchema(new System.Xml.XmlTextReader(new System.IO.StringReader(strSchema)));
            //\\        if ((ds.Tables["Categories"] != null)) { 
            //\\            base.Tables.Add(new CategoriesDataTable(ds.Tables["Categories"]));
            //\\        } 
            //\\        this.DataSetName = ds.DataSetName; 
            //\\        this.Prefix = ds.Prefix;
            //\\        this.Namespace = ds.Namespace; 
            //\\        this.Locale = ds.Locale;
            //\\        this.CaseSensitive = ds.CaseSensitive;
            //\\        this.EnforceConstraints = ds.EnforceConstraints;
            //\\        this.Merge(ds, false, System.Data.MissingSchemaAction.Add); 
            //\\        this.InitVars();
            //\\    } 
            //\\    else { 
            //\\        this.ReadXmlSchema(new System.Xml.XmlTextReader(new System.IO.StringReader(strSchema)));
            //\\    } 
            //\\
            //\\    this.GetSerializationData(info, context);
            //\\    System.ComponentModel.CollectionChangeEventHandler schemaChangedHandler = new System.ComponentModel.CollectionChangeEventHandler(this.SchemaChanged);
            //\\    base.Tables.CollectionChanged += schemaChangedHandler; 
            //\\    this.Relations.CollectionChanged += schemaChangedHandler;
            //\\ } 
 
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Family);
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Runtime.Serialization.SerializationInfo)), "info")); 
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Runtime.Serialization.StreamingContext)) , "context"));
            constructor.BaseConstructorArgs.AddRange(new CodeExpression[] { CodeGenHelper.Argument("info"), CodeGenHelper.Argument("context"), CodeGenHelper.Primitive(false) });

            constructor.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.EQ( 
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.This(),
                            "IsBinarySerialized", 
                            new CodeExpression[] { CodeGenHelper.Argument("info"), CodeGenHelper.Argument("context") }
                        ),
                        CodeGenHelper.Primitive(true)),
                    new CodeStatement[] { 
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(),"InitVars", CodeGenHelper.Primitive(false))),
                        CodeGenHelper.VariableDecl( 
                            CodeGenHelper.GlobalType(typeof(CollectionChangeEventHandler)), 
                            "schemaChangedHandler1",
                            new CodeDelegateCreateExpression( 
                                CodeGenHelper.GlobalType(typeof(CollectionChangeEventHandler)),
                                CodeGenHelper.This(),
                                "SchemaChanged"
                            ) 
                        ),
                        new System.CodeDom.CodeAttachEventStatement( 
                            new CodeEventReferenceExpression( 
                                CodeGenHelper.Property(CodeGenHelper.This(), "Tables"),
                                "CollectionChanged" 
                            ),
                            CodeGenHelper.Variable("schemaChangedHandler1")
                        ) ,
                        new System.CodeDom.CodeAttachEventStatement( 
                            new CodeEventReferenceExpression(
                                CodeGenHelper.Property(CodeGenHelper.This(), "Relations"), 
                                "CollectionChanged" 
                            ),
                            CodeGenHelper.Variable("schemaChangedHandler1") 
                        ),
                        CodeGenHelper.Return()
                    }
                ) 
            );
 
            constructor.Statements.Add( 
                CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(System.String)), "strSchema",
                CodeGenHelper.Cast(CodeGenHelper.GlobalType(typeof(System.String)), CodeGenHelper.MethodCall( 
                                                        CodeGenHelper.Argument("info"),
                                                        "GetValue",
                                                        new CodeExpression[] {
                                                            CodeGenHelper.Str("XmlSchema"), 
                                                            CodeGenHelper.TypeOf(CodeGenHelper.GlobalType(typeof(System.String)))
                                                        } )))); 
 
            ArrayList schemaBodySerializeSchema = new ArrayList();
            ArrayList schemaBodyDontSerializeSchema = new ArrayList(); 
            schemaBodySerializeSchema.Add(CodeGenHelper.VariableDecl(
                                CodeGenHelper.GlobalType(typeof(System.Data.DataSet)),
                                "ds",
                                CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Data.DataSet)), new CodeExpression[] {}))); 
            schemaBodySerializeSchema.Add(CodeGenHelper.Stm(
                                CodeGenHelper.MethodCall( 
                                    CodeGenHelper.Variable("ds"), 
                                    "ReadXmlSchema",
                                    new CodeExpression[] { 
                                        CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Xml.XmlTextReader)),
                                            new CodeExpression[] {
                                                CodeGenHelper.New(
                                                    CodeGenHelper.GlobalType(typeof(System.IO.StringReader)), 
                                                    new CodeExpression[] {
                                                        CodeGenHelper.Variable("strSchema") 
                                                    }) 
                                            })
                                    }))); 

            foreach(DesignTable table in codeGenerator.TableHandler.Tables) {
                //\\ this.Tables.Add(new <TableClassName>("<TableName>"));
                schemaBodySerializeSchema.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdNotEQ( 
                                        CodeGenHelper.Indexer( 
                                                CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Tables"),
                                                CodeGenHelper.Str(table.Name)), 
                                        CodeGenHelper.Primitive(null)),
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property(CodeGenHelper.Base(), "Tables"), 
                                "Add",
                                CodeGenHelper.New( 
                                    CodeGenHelper.Type(table.GeneratorTableClassName), 
                                    new CodeExpression[] {
                                        CodeGenHelper.Indexer( 
                                            CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Tables"),
                                            CodeGenHelper.Str(table.Name))
                                    }
                                ) 
                            )
                        ) 
                    ) 
                );
            } 

            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "DataSetName"), 
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"DataSetName")
                ) 
            ); 
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "Prefix"),
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Prefix")
                )
            ); 
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "Namespace"), 
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Namespace")
                ) 
            );
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "Locale"), 
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Locale")
                ) 
            ); 
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "CaseSensitive"),
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"CaseSensitive")
                )
            ); 
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "EnforceConstraints"), 
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"EnforceConstraints")
                ) 
            );
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.This(),
                        "Merge", 
                        new CodeExpression[] { 
                            CodeGenHelper.Variable("ds"),
                            CodeGenHelper.Primitive(false), 
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(MissingSchemaAction)),"Add")
                        }
                    )
                ) 
            );
            schemaBodySerializeSchema.Add( 
                CodeGenHelper.Stm( 
                    CodeGenHelper.MethodCall(CodeGenHelper.This(),"InitVars")
                ) 
            );

            schemaBodyDontSerializeSchema.Add(
                CodeGenHelper.Stm( 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.This(), 
                        "ReadXmlSchema", 
                        new CodeExpression[] {
                            CodeGenHelper.New( 
                                CodeGenHelper.GlobalType(typeof(System.Xml.XmlTextReader)),
                                new CodeExpression[] {
                                    CodeGenHelper.New(
                                        CodeGenHelper.GlobalType(typeof(System.IO.StringReader)), 
                                        new CodeExpression[] { CodeGenHelper.Variable("strSchema") }
                                    ) 
                                } 
                            )
                        } 
                    )
                )
            );
 
            if (initExpressionsMethod != null) {
                schemaBodyDontSerializeSchema.Add(CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitExpressions"))); 
            } 

            constructor.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.EQ(
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.This(), 
                            "DetermineSchemaSerializationMode",
                            new CodeExpression[] { CodeGenHelper.Argument("info"), CodeGenHelper.Argument("context") } 
                        ), 
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.SchemaSerializationMode)), 
                            "IncludeSchema"
                        )
                    ),
                    (CodeStatement[])schemaBodySerializeSchema.ToArray(typeof(CodeStatement)), 
                    (CodeStatement[])schemaBodyDontSerializeSchema.ToArray(typeof(CodeStatement))
                ) 
            ); 

            constructor.Statements.Add( 
                CodeGenHelper.MethodCall(
                    CodeGenHelper.This(),
                    "GetSerializationData",
                    new CodeExpression [] { 
                        CodeGenHelper.Argument("info"),
                        CodeGenHelper.Argument("context") 
                    } 
                )
            ); 
            constructor.Statements.Add(
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(CollectionChangeEventHandler)),
                    "schemaChangedHandler", 
                    new CodeDelegateCreateExpression(
                        CodeGenHelper.GlobalType( 
                            typeof(CollectionChangeEventHandler) 
                        ),
                        CodeGenHelper.This(), 
                        "SchemaChanged"
                    )
                )
            ); 
            constructor.Statements.Add(
                new System.CodeDom.CodeAttachEventStatement( 
                    new CodeEventReferenceExpression( 
                        CodeGenHelper.Property(CodeGenHelper.Base(),"Tables"),
                        "CollectionChanged" 
                    ),
                    CodeGenHelper.Variable("schemaChangedHandler")
                )
            ); 
            constructor.Statements.Add(
                new System.CodeDom.CodeAttachEventStatement( 
                    new CodeEventReferenceExpression( 
                        CodeGenHelper.Property(CodeGenHelper.This(),"Relations"),
                        "CollectionChanged" 
                    ),
                    CodeGenHelper.Variable("schemaChangedHandler")
                )
            ); 

            return constructor; 
        } 

        private CodeMemberMethod InitializeDerivedDataSet() { 
            //\\ protected override void InitializeDerivedDataSet(){
            //\\     this.BeginInit();
            //\\     this.InitClass();
            //\\     this.EndInit(); 
            //\\ }
            CodeMemberMethod initMethod = CodeGenHelper.MethodDecl( 
                CodeGenHelper.GlobalType(typeof(void)), 
                "InitializeDerivedDataSet",
                MemberAttributes.Family | MemberAttributes.Override 
            );

            initMethod.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "BeginInit"));
            initMethod.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitClass")); 
            initMethod.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "EndInit"));
 
            return initMethod; 
        }
 
        private CodeMemberMethod CloneMethod(CodeMemberMethod initExpressionsMethod) {
            //\\ public override DataSet Clone() {
            //\\     <DataSetClassName> cln = (<DataSetClassName>)base.Clone();
            //\\     cln.InitVars(); 
            //\\     cln.InitExpressions();
            //\\     cln.SchemaSerializationMode = this.SchemaSerializationMode(); 
            //\\     return cln; 
            //\\ }
            CodeMemberMethod clone = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataSet)), "Clone", MemberAttributes.Public | MemberAttributes.Override); 
                clone.Statements.Add(
                    CodeGenHelper.VariableDecl(
                        CodeGenHelper.Type(codeGenerator.DataSourceName),
                        "cln", 
                        CodeGenHelper.Cast(
                            CodeGenHelper.Type(codeGenerator.DataSourceName), 
                            CodeGenHelper.MethodCall(CodeGenHelper.Base(), "Clone", new CodeExpression[] {}) 
                        )
                    ) 
                );

                clone.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.Variable("cln"), "InitVars", new CodeExpression [] {}));
                if (initExpressionsMethod != null) { 
                    clone.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.Variable("cln"), "InitExpressions", new CodeExpression[] { }));
                } 
                clone.Statements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(CodeGenHelper.Variable("cln"), "SchemaSerializationMode"), 
                        CodeGenHelper.Property(CodeGenHelper.This(), "SchemaSerializationMode")
                    )
                );
 
                clone.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable("cln")));
 
            return clone; 
        }
 
        private CodeMemberMethod ShouldSerializeTablesMethod() {
            //\\ protected override bool ShouldSerializeTables() {
            //\\     return false;
            //\\ } 
            CodeMemberMethod shouldSerializeTables = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(System.Boolean)), "ShouldSerializeTables", MemberAttributes.Family | MemberAttributes.Override);
            shouldSerializeTables.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Primitive(false))); 
 
            return shouldSerializeTables;
        } 

        private CodeMemberMethod ShouldSerializeRelationsMethod() {
            //\\ protected override bool ShouldSerializeRelations() {
            //\\     return false; 
            //\\ }
            CodeMemberMethod shouldSerializeRelations = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(System.Boolean)), "ShouldSerializeRelations", MemberAttributes.Family | MemberAttributes.Override); 
            shouldSerializeRelations.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Primitive(false))); 

            return shouldSerializeRelations; 
        }

        private CodeMemberMethod ReadXmlSerializableMethod() {
            //\\ protected override void ReadXmlSerializable(XmlReader reader) { 
            //\\    if (GetSchemaSerializationMode(reader) == System.Data.SchemaSerializationMode.IncludeSchema) {
            //\\        this.Reset(); 
            //\\        System.Data.DataSet ds = new System.Data.DataSet(); 
            //\\        ds.ReadXml(reader);
            //\\        if ((ds.Tables["Categories"] != null)) { 
            //\\            base.Tables.Add(new CategoriesDataTable(ds.Tables["Categories"]));
            //\\        }
            //\\        this.DataSetName = ds.DataSetName;
            //\\        this.Prefix = ds.Prefix; 
            //\\        this.Namespace = ds.Namespace;
            //\\        this.Locale = ds.Locale; 
            //\\        this.CaseSensitive = ds.CaseSensitive; 
            //\\        this.EnforceConstraints = ds.EnforceConstraints;
            //\\        this.Merge(ds, false, System.Data.MissingSchemaAction.Add); 
            //\\        this.InitVars();
            //\\    }
            //\\    else {
            //\\        this.ReadXml(reader); 
            //\\        this.InitVars();
            //\\        return; 
            //\\    } 
            //\\ }
 
            CodeMemberMethod readXmlSerializable = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "ReadXmlSerializable", MemberAttributes.Family | MemberAttributes.Override);
            readXmlSerializable.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Xml.XmlReader)), "reader"));
            ArrayList includeSchemaStatements = new ArrayList();
            ArrayList excludeSchemaStatements = new ArrayList(); 
            includeSchemaStatements.Add(CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "Reset", new CodeExpression[] { })));
            includeSchemaStatements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataSet)), "ds", CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Data.DataSet)), new CodeExpression[] { }))); 
            includeSchemaStatements.Add( 
                CodeGenHelper.Stm(CodeGenHelper.MethodCall(
                    CodeGenHelper.Variable("ds"), 
                    "ReadXml",
                    new CodeExpression [] { CodeGenHelper.Argument("reader") }
                ))
            ); 
            foreach(DesignTable table in codeGenerator.TableHandler.Tables) {
                //\\ this.Tables.Add(new <TableClassName>("<TableName>")); 
                includeSchemaStatements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdNotEQ( 
                            CodeGenHelper.Indexer(
                                CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Tables"),
                                CodeGenHelper.Str(table.Name)
                            ), 
                            CodeGenHelper.Primitive(null)
                        ), 
                        CodeGenHelper.Stm( 
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property(CodeGenHelper.Base(), "Tables"), 
                                "Add",
                                CodeGenHelper.New(
                                    CodeGenHelper.Type(table.GeneratorTableClassName),
                                    new CodeExpression[] { 
                                        CodeGenHelper.Indexer(
                                            CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Tables"), 
                                            CodeGenHelper.Str(table.Name) 
                                        )
                                    } 
                                )
                            )
                        )
                    ) // If 
                );
            } 
 
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "DataSetName"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "DataSetName")));
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Prefix"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "Prefix"))); 
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Namespace"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "Namespace")));
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Locale"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "Locale")));
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "CaseSensitive"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "CaseSensitive")));
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "EnforceConstraints"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "EnforceConstraints"))); 
            includeSchemaStatements.Add(
                CodeGenHelper.Stm(CodeGenHelper.MethodCall( 
                    CodeGenHelper.This(), 
                    "Merge",
                    new CodeExpression[] { 
                        CodeGenHelper.Variable("ds"),
                        CodeGenHelper.Primitive(false),
                        CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(MissingSchemaAction)), "Add")
                    } 
                ))
            ); 
            includeSchemaStatements.Add(CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitVars"))); 

            excludeSchemaStatements.Add(CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "ReadXml", new CodeExpression[] { CodeGenHelper.Argument("reader") }))); 
            excludeSchemaStatements.Add(CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitVars")));

            readXmlSerializable.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.EQ(
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.This(), 
                            "DetermineSchemaSerializationMode",
                            new CodeExpression[] { CodeGenHelper.Argument("reader") } 
                        ),
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.SchemaSerializationMode)),
                            "IncludeSchema" 
                        )
                    ), 
                    (CodeStatement[])includeSchemaStatements.ToArray(typeof(CodeStatement)), 
                    (CodeStatement[])excludeSchemaStatements.ToArray(typeof(CodeStatement))
                ) 
            );

            return readXmlSerializable;
        } 

        private CodeMemberMethod GetSchemaSerializableMethod() { 
            //\\ protected override System.Xml.Schema.XmlSchema GetSchemaSerializable() { 
            //\\     System.IO.MemoryStream stream = new System.IO.MemoryStream();
            //\\     WriteXmlSchema(new XmlTextWriter(stream, null )); 
            //\\     stream.Position = 0;
            //\\     return System.Xml.Schema.XmlSchema.Read(new XmlTextReader(stream));
            //\\ }
            CodeMemberMethod getSchemaSerializable = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(System.Xml.Schema.XmlSchema)), "GetSchemaSerializable", MemberAttributes.Family | MemberAttributes.Override); 
            getSchemaSerializable.Statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)), "stream", CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)),new CodeExpression[] {})));
            getSchemaSerializable.Statements.Add( 
                CodeGenHelper.MethodCall( 
                    CodeGenHelper.This(),
                    "WriteXmlSchema", 
                    CodeGenHelper.New(
                        CodeGenHelper.GlobalType(typeof(System.Xml.XmlTextWriter)),
                        new CodeExpression[] {
                            CodeGenHelper.Argument("stream"), 
                            CodeGenHelper.Primitive(null)
                        } 
                    ) 
                )
            ); 
            getSchemaSerializable.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.Argument("stream"), "Position"), CodeGenHelper.Primitive(0)));
            getSchemaSerializable.Statements.Add(
                CodeGenHelper.Return(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.GlobalTypeExpr(typeof(System.Xml.Schema.XmlSchema)),
                        "Read", 
                        new CodeExpression[] { 
                            CodeGenHelper.New(
                                CodeGenHelper.GlobalType(typeof(System.Xml.XmlTextReader)), 
                                new CodeExpression[] {CodeGenHelper.Argument("stream")}
                            ),
                            CodeGenHelper.Primitive(null)
                        } 
                    )
                ) 
            ); 

            return getSchemaSerializable; 
        }

        private CodeMemberMethod InitVarsParamLess() {
            //\\ public void InitVars() { 
            //\\    InitVars(true);
            //\\ } 
            CodeMemberMethod initVarsMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitVars", MemberAttributes.Assembly | MemberAttributes.Final); 
            initVarsMethod.Statements.Add(
                CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitVars", new CodeExpression[] { CodeGenHelper.Primitive(true) }) 
            );

            return initVarsMethod;
        } 

        private void InitClassAndInitVarsMethods(out CodeMemberMethod initClassMethod, out CodeMemberMethod initVarsMethod) { 
            //\\ private void InitClass() 
            initClassMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitClass", MemberAttributes.Private);
 
            //\\ public void InitVars(bool initTable)
            initVarsMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitVars", MemberAttributes.Assembly | MemberAttributes.Final);
            initVarsMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(Boolean)), "initTable"));
 
            //\\ this.DataSetName = "<dataSet.DataSetName>"
            initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "DataSetName"), CodeGenHelper.Str(dataSet.DataSetName))); 
            //\\ this.Prefix   = "<dataSet.Prefix>" 
            initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Prefix"), CodeGenHelper.Str(dataSet.Prefix)));
            if(namespaceProperty.ShouldSerializeValue(dataSet)) { 
                //\\ this.Namespace   = "<dataSet.Namespace>"
                initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Namespace"), CodeGenHelper.Str(dataSet.Namespace)));
            }
            if(localeProperty.ShouldSerializeValue(dataSet)) { 
                //\\ this.Locale = new System.Globalization.CultureInfo("dataSet.<Locale>");
                initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Locale"), CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Globalization.CultureInfo)),new CodeExpression[] {CodeGenHelper.Str(dataSet.Locale.ToString())}))); 
            } 
            if (caseSensitiveProperty.ShouldSerializeValue(dataSet)) {
                //\\ this.CaseSensitive = <dataSet.CaseSensitive>; 
                initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "CaseSensitive"), CodeGenHelper.Primitive(dataSet.CaseSensitive)));
            }
            //\\ this.EnforceConstraints = <dataSet.EnforceConstraints>;
            initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "EnforceConstraints"), CodeGenHelper.Primitive(dataSet.EnforceConstraints))); 

            //\\ this.SchemaSerializationMode = <dataSource.SchemaSerializationMode>; 
            initClassMethod.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "SchemaSerializationMode"), 
                    CodeGenHelper.Field(
                        CodeGenHelper.GlobalTypeExpr(typeof(System.Data.SchemaSerializationMode)),
                        this.dataSource.SchemaSerializationMode.ToString()
                    ) 
                )
            ); 
 
            // add statements to initialize tables
            foreach(DesignTable table in codeGenerator.TableHandler.Tables) { 
                CodeExpression fieldTable = CodeGenHelper.Field(CodeGenHelper.This(), table.GeneratorTableVarName);

                if (TableContainsExpressions(table)) {
                    //\\ <TableVariableName> = new <TableClassName>(false); 
                    initClassMethod.Statements.Add(CodeGenHelper.Assign(fieldTable, CodeGenHelper.New(CodeGenHelper.Type(table.GeneratorTableClassName), new CodeExpression[] { CodeGenHelper.Primitive(false) })));
                } 
                else { 
                    //\\ <TableVariableName> = new <TableClassName>();
                    initClassMethod.Statements.Add(CodeGenHelper.Assign(fieldTable, CodeGenHelper.New(CodeGenHelper.Type(table.GeneratorTableClassName), new CodeExpression[] { }))); 
                }

                //\\ this.Tables.Add(this.<TableVariableName>);
                initClassMethod.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.Property(CodeGenHelper.Base(), "Tables"), "Add", fieldTable)); 

                //\\ this.table<TableFieldName> = (<TableClassName>)this.Tables["<TableName>"]; 
                //\\ if (this.table<TableFieldName> != null) 
                //\\    this.table<TableFieldName>.InitVars();
                initVarsMethod.Statements.Add( 
                    CodeGenHelper.Assign(
                        fieldTable,
                        CodeGenHelper.Cast(
                            CodeGenHelper.Type(table.GeneratorTableClassName), 
                            CodeGenHelper.Indexer(
                                CodeGenHelper.Property(CodeGenHelper.Base(), "Tables"), 
                                CodeGenHelper.Str(table.Name) 
                            )
                        ) 
                    )
                );
                initVarsMethod.Statements.Add(
                    CodeGenHelper.If( 
                        CodeGenHelper.EQ(CodeGenHelper.Variable("initTable"), CodeGenHelper.Primitive(true)),
                        new CodeStatement[] { 
                            CodeGenHelper.If( 
                                CodeGenHelper.IdNotEQ(fieldTable, CodeGenHelper.Primitive(null)),
                                CodeGenHelper.Stm(CodeGenHelper.MethodCall(fieldTable, "InitVars")) 
                            )
                        }
                    )
                ); 
            }
 
            /*----------- Add Constraints to the Tables -------------------------*/ 
            CodeExpression varFkc = null;
            foreach(DesignTable designTable in codeGenerator.TableHandler.Tables) { 
                DataTable table = designTable.DataTable;
                foreach(Constraint constraint in table.Constraints) {
                    if (constraint is ForeignKeyConstraint) {
                        // We only initialize the foreign key constraints here. 
                        //\\ ForeignKeyConstraint fkc;
                        //\\ fkc = new ForeignKeyConstraint("<ConstrainName>", 
                        //\\     new DataColumn[] {this.table<TableClassName>.<ColumnName>Column}, // parent columns 
                        //\\     new DataColumn[] {this.table<TableClassName>.<ColumnName>Column}  // child columns
                        //\\ )); 
                        //\\ this.table<TableClassName>.Constraints.Add(fkc);
                        //\\ fkc.AcceptRejectRule = constraint.AcceptRejectRule;
                        //\\ fkc.DeleteRule = constraint.DeleteRule;
                        //\\ fkc.UpdateRule = constraint.UpdateRule; 

                        ForeignKeyConstraint fkc = (ForeignKeyConstraint) constraint; 
 
                        CodeArrayCreateExpression childrenColumns = new CodeArrayCreateExpression(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), 0);
                        foreach(DataColumn c in fkc.Columns) { 
                            childrenColumns.Initializers.Add(
                                CodeGenHelper.Property(
                                    CodeGenHelper.Field(CodeGenHelper.This(), codeGenerator.TableHandler.Tables[c.Table.TableName].GeneratorTableVarName),
                                    codeGenerator.TableHandler.Tables[c.Table.TableName].DesignColumns[c.ColumnName].GeneratorColumnPropNameInTable 
                                )
                            ); 
                        } 

                        CodeArrayCreateExpression parentColumns = new CodeArrayCreateExpression(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), 0); 
                        foreach(DataColumn c in fkc.RelatedColumns) {
                            parentColumns.Initializers.Add(
                                CodeGenHelper.Property(
                                    CodeGenHelper.Field(CodeGenHelper.This(), codeGenerator.TableHandler.Tables[c.Table.TableName].GeneratorTableVarName), 
                                    codeGenerator.TableHandler.Tables[c.Table.TableName].DesignColumns[c.ColumnName].GeneratorColumnPropNameInTable
                                ) 
                            ); 
                        }
 
                        if (varFkc == null) {
                            initClassMethod.Statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(System.Data.ForeignKeyConstraint)), "fkc"));
                            varFkc = CodeGenHelper.Variable("fkc");
                        } 

                        initClassMethod.Statements.Add( 
                            CodeGenHelper.Assign( 
                                varFkc,
                                CodeGenHelper.New( 
                                    CodeGenHelper.GlobalType(typeof(System.Data.ForeignKeyConstraint)),
                                    new CodeExpression[]{CodeGenHelper.Str(fkc.ConstraintName), parentColumns, childrenColumns}
                                )
                            ) 
                        );
 
                        initClassMethod.Statements.Add( 
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property( 
                            CodeGenHelper.Field(CodeGenHelper.This(), codeGenerator.TableHandler.Tables[table.TableName].GeneratorTableVarName),
                                    "Constraints"
                                ),
                                "Add", 
                                varFkc
                            ) 
                        ); 

                        string acceptRejectRule = fkc.AcceptRejectRule.ToString(); 
                        string deleteRule = fkc.DeleteRule.ToString();
                        string updateRule = fkc.UpdateRule.ToString();
                        initClassMethod.Statements.Add(
                            CodeGenHelper.Assign( 
                                CodeGenHelper.Property(varFkc, "AcceptRejectRule"),
                                CodeGenHelper.Field( 
                                    CodeGenHelper.GlobalTypeExpr(fkc.AcceptRejectRule.GetType()), 
                                    acceptRejectRule
                                ) 
                            )
                        );

                        initClassMethod.Statements.Add( 
                            CodeGenHelper.Assign(
                                CodeGenHelper.Property(varFkc, "DeleteRule"), 
                                CodeGenHelper.Field( 
                                    CodeGenHelper.GlobalTypeExpr(fkc.DeleteRule.GetType()),
                                    deleteRule 
                                )
                            )
                        );
 
                        initClassMethod.Statements.Add(
                            CodeGenHelper.Assign( 
                                CodeGenHelper.Property(varFkc, "UpdateRule"), 
                                CodeGenHelper.Field(
                                    CodeGenHelper.GlobalTypeExpr(fkc.UpdateRule.GetType()), 
                                    updateRule
                                )
                            )
                        ); 
                    }
                } 
            } 

            /*----------- Add Relations to the Dataset -------------------------*/ 
            foreach(DesignRelation designRelation in codeGenerator.RelationHandler.Relations) {
                //\\ this.relation<RelationName>= new DataRelation("<RelationName>",
                //\\     new DataColumn[] {this.table<TableClassName>.<ColumnName>Column}, // parent columns
                //\\     new DataColumn[] {this.table<TableClassName>.<ColumnName>Column}, // child columns 
                //\\     false                                                             // createConstraints
                //\\ )); 
                DataRelation relation = designRelation.DataRelation; 
                if(relation == null) {
                    continue; 
                }
                CodeArrayCreateExpression parentColCreate =  new CodeArrayCreateExpression(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), 0);
                string parentTableField = designRelation.ParentDesignTable.GeneratorTableVarName;
                foreach(DataColumn column in relation.ParentColumns) { 
                    parentColCreate.Initializers.Add(
                        CodeGenHelper.Property( 
                            CodeGenHelper.Field(CodeGenHelper.This(), parentTableField), 
                            codeGenerator.TableHandler.Tables[column.Table.TableName].DesignColumns[column.ColumnName].GeneratorColumnPropNameInTable
                        ) 
                    );
                }

                CodeArrayCreateExpression childColCreate =  new CodeArrayCreateExpression(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), 0); 
                string childTableField = designRelation.ChildDesignTable.GeneratorTableVarName;
                foreach(DataColumn column in relation.ChildColumns) { 
                    childColCreate.Initializers.Add( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Field(CodeGenHelper.This(), childTableField), 
                            codeGenerator.TableHandler.Tables[column.Table.TableName].DesignColumns[column.ColumnName].GeneratorColumnPropNameInTable
                        )
                    );
                } 

                CodeExpression relationVarExpression = CodeGenHelper.Field( 
                    CodeGenHelper.This(), 
                    codeGenerator.RelationHandler.Relations[relation.RelationName].GeneratorRelationVarName
                ); 

                initClassMethod.Statements.Add(
                    CodeGenHelper.Assign(
                        relationVarExpression, 
                        CodeGenHelper.New(
                            CodeGenHelper.GlobalType(typeof(System.Data.DataRelation)), 
                            new CodeExpression[] { 
                                CodeGenHelper.Str(relation.RelationName),
                                parentColCreate, 
                                childColCreate,
                                CodeGenHelper.Primitive(false)
                            }
                        ) 
                    )
                ); 
 
                if (relation.Nested) {
                    //\\ this.relation<RelationName>.Nested = true; 
                    initClassMethod.Statements.Add(
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(
                                relationVarExpression, 
                                "Nested"
                            ), 
                            CodeGenHelper.Primitive(true) 
                        )
                    ); 
                }

                // Add extended properties to the DataRelation
                ExtendedPropertiesHandler.CodeGenerator = codeGenerator; 
                ExtendedPropertiesHandler.AddExtendedProperties(designRelation, relationVarExpression, initClassMethod.Statements, relation.ExtendedProperties);
 
                //\\ this.Relations.Add(this.relation<RelationName>); 
                initClassMethod.Statements.Add(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Property(
                            CodeGenHelper.This(),
                            "Relations"
                        ), 
                        "Add",
                        relationVarExpression 
                    ) 
                );
 
                //\\ this.relation<RelationName> = this.Relations["<RelationName>"];
                initVarsMethod.Statements.Add(
                    CodeGenHelper.Assign(
                        relationVarExpression, 
                        CodeGenHelper.Indexer(
                            CodeGenHelper.Property(CodeGenHelper.This(), "Relations"), 
                            CodeGenHelper.Str(relation.RelationName) 
                        )
                    ) 
                );
            }

            // Add extended properties to the dataset, if there are any in the schema file 
            ExtendedPropertiesHandler.CodeGenerator = codeGenerator;
            ExtendedPropertiesHandler.AddExtendedProperties(this.dataSource, CodeGenHelper.This(), initClassMethod.Statements, dataSet.ExtendedProperties); 
 
        } // end InitClassAndInitVarsMethods(...)
 
        private void AddShouldSerializeSingleTableMethods(CodeTypeDeclaration dataSourceClass) {
            foreach(DesignTable table in codeGenerator.TableHandler.Tables) {
                string tablePropertyName = table.GeneratorTablePropName;
                string shouldSerializeMethodName = MemberNameValidator.GenerateIdName("ShouldSerialize" + tablePropertyName, this.codeGenerator.CodeProvider, false /*useSuffix*/); 

                CodeMemberMethod shouldSerializeTableProperty = 
                    CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(System.Boolean)), shouldSerializeMethodName, MemberAttributes.Private); 
                shouldSerializeTableProperty.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Primitive(false)));
                dataSourceClass.Members.Add(shouldSerializeTableProperty); 
            }
        }

 
        private CodeMemberMethod SchemaChangedMethod() {
            CodeMemberMethod schemaChanged = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "SchemaChanged", MemberAttributes.Private); 
            schemaChanged.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(object)), "sender")); 
            schemaChanged.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(CollectionChangeEventArgs)), "e"));
            schemaChanged.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.EQ(
                        CodeGenHelper.Property(CodeGenHelper.Argument("e"), "Action"),
                        CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(CollectionChangeAction)), "Remove") 
                    ),
                    CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitVars")) 
                ) 
            );
 
            return schemaChanged;
        }

//        private CodeMemberMethod GetTypedDataSetSchema() { 
//            //\\ public static XmlSchemaType GetTypedDataSetSchema(XmlSchemaSet xs) {
//            //\\     Authors_DS ds = new Authors_DS(); 
//            //\\     xs.Add(ds.GetSchemaSerializable()); 
//            //\\     XmlSchemaComplexType type = DataSet.GetDataSetSchema(xs);
//            //\\     XmlSchemaAttribute attribute = new XmlSchemaAttribute(); 
//            //\\     attribute.Name = "namespace";
//            //\\     attribute.FixedValue = ds.Namespace;
//            //\\     type.Attributes.Add(attribute);
//            //\\     return type; 
//            //\\ }
//            CodeMemberMethod getTypedDataSetSchema = CodeGenHelper.MethodDecl(typeof(XmlSchemaType), "GetTypedDataSetSchema", MemberAttributes.Static | MemberAttributes.Public); 
//            getTypedDataSetSchema.Parameters.Add(CodeGenHelper.ParameterDecl(typeof(XmlSchemaSet), "xs")); 
//
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.VariableDecl(dataSource.GeneratorDataSetName, "ds", CodeGenHelper.New(dataSource.GeneratorDataSetName, new CodeExpression[] {}))); 
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.Argument("xs"), "Add", new CodeExpression [] { CodeGenHelper.MethodCall(CodeGenHelper.Variable("ds"), "GetSchemaSerializable", new CodeExpression[] {})}));
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.VariableDecl(typeof(XmlSchemaComplexType), "type", CodeGenHelper.MethodCall(CodeGenHelper.TypeExpr(typeof(DataSet)), "GetDataSetSchema", new CodeExpression[] {CodeGenHelper.Argument("xs")})));
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.VariableDecl(typeof(XmlSchemaAttribute), "attribute", CodeGenHelper.New(typeof(XmlSchemaAttribute), new CodeExpression[] {})));
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.Variable("attribute"), "Name"), CodeGenHelper.Primitive("namespace"))); 
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.Variable("attribute"),"FixedValue"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "Namespace")));
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.Property(CodeGenHelper.Variable("type"),"Attributes"), "Add", new CodeExpression [] { CodeGenHelper.Variable("attribute") })); 
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable("type"))); 
//
//            return getTypedDataSetSchema; 
//        }

        internal static void GetSchemaIsInCollection(CodeStatementCollection statements, string dsName, string collectionName)
        { 
            CodeStatement[] condBodyInner = new CodeStatement[] {
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property( 
                        CodeGenHelper.Variable("s1"),
                        "Position" 
                    ),
                    CodeGenHelper.Primitive(0)
                ),
 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property( 
                        CodeGenHelper.Variable("s2"), 
                        "Position"
                    ), 
                    CodeGenHelper.Primitive(0)
                ),

                CodeGenHelper.ForLoop( 
                    CodeGenHelper.Stm(
                        new CodeSnippetExpression("") 
                    ), 
                    CodeGenHelper.And(
                        CodeGenHelper.IdNotEQ( 
                            CodeGenHelper.Property(
                                CodeGenHelper.Variable("s1"),
                                "Position"
                            ), 
                            CodeGenHelper.Property(
                                CodeGenHelper.Variable("s1"), 
                                "Length" 
                            )
                        ), 
                        CodeGenHelper.EQ(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Variable("s1"),
                                "ReadByte", 
                                new CodeExpression[] {}
                            ), 
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Variable("s2"),
                                "ReadByte", 
                                new CodeExpression[] {}
                            )
                        )
                    ), 
                    CodeGenHelper.Stm(
                        new CodeSnippetExpression("") 
                    ), 
                    new CodeStatement[] {
                        CodeGenHelper.Stm( 
                            new CodeSnippetExpression("")
                        )
                    }
                ), 

                CodeGenHelper.If( 
                    CodeGenHelper.EQ( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Variable("s1"), 
                            "Position"
                        ),
                        CodeGenHelper.Property(
                            CodeGenHelper.Variable("s1"), 
                            "Length"
                        ) 
                    ), 
                    new CodeStatement[] {
                        CodeGenHelper.Return( 
                            CodeGenHelper.Variable("type")
                        )
                    }
                ) 
            };
 
            CodeStatement[] loopBody = new CodeStatement[] { 
                CodeGenHelper.Assign(
                    CodeGenHelper.Variable("schema"), 
                    CodeGenHelper.Cast(
                        CodeGenHelper.GlobalType(typeof(XmlSchema)),
                        CodeGenHelper.Property(
                            CodeGenHelper.Variable("schemas"), 
                            "Current"
                        ) 
                    ) 
                ),
 
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable("s2"),
                        "SetLength", 
                        new CodeExpression[] {
                            CodeGenHelper.Primitive(0) 
                        } 
                    )
                ), 

                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable("schema"), 
                        "Write",
                        new CodeExpression[] { 
                            CodeGenHelper.Variable("s2") 
                        }
                    ) 
                ),

                CodeGenHelper.If(
                    CodeGenHelper.EQ( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Variable("s1"), 
                            "Length" 
                        ),
                        CodeGenHelper.Property( 
                            CodeGenHelper.Variable("s2"),
                            "Length"
                        )
                    ), 
                    condBodyInner
                ) 
            }; 

            CodeStatement[] tryBody = new CodeStatement[] { 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(XmlSchema)),
                    "schema",
                    CodeGenHelper.Primitive(null) 
                ),
 
                CodeGenHelper.Stm( 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable("dsSchema"), 
                        "Write",
                        new CodeExpression[] {
                            CodeGenHelper.Variable("s1")
                        } 
                    )
                ), 
 
                CodeGenHelper.ForLoop(
                    CodeGenHelper.VariableDecl( 
                        CodeGenHelper.GlobalType(typeof(IEnumerator)),
                        "schemas",
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Variable(collectionName),
                                "Schemas", 
                                new CodeExpression[] { 
                                    CodeGenHelper.Property(
                                        CodeGenHelper.Variable("dsSchema"), 
                                        "TargetNamespace"
                                    )
                                }
                            ), 
                            "GetEnumerator",
                            new CodeExpression[] { } 
                        ) 
                    ),
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Variable("schemas"),
                        "MoveNext",
                        new CodeExpression[] { }
                    ), 
                    CodeGenHelper.Stm(
                        new CodeSnippetExpression("") 
                    ), 
                    loopBody
                ) 
            };

            CodeStatement[] finallyBody = new CodeStatement[] {
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Variable("s1"), 
                        CodeGenHelper.Primitive(null) 
                    ),
                    new CodeStatement[] { 
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Variable("s1"),
                                "Close", 
                                new CodeExpression[] { }
                            ) 
                        ) 
                    }
                ), 

                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Variable("s2"), 
                        CodeGenHelper.Primitive(null)
                    ), 
                    new CodeStatement[] { 
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Variable("s2"),
                                "Close",
                                new CodeExpression[] { }
                            ) 
                        )
                    } 
                ) 
            };
 
            CodeStatement[] condBodyOuter = new CodeStatement[] {
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)),
                    "s1", 
                    CodeGenHelper.New(
                        CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)), 
                        new CodeExpression[] { } 
                    )
                ), 

                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)),
                    "s2", 
                    CodeGenHelper.New(
                        CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)), 
                        new CodeExpression[] { } 
                    )
                ), 

                CodeGenHelper.Try(
                    tryBody,
                    new CodeCatchClause[] { }, 
                    finallyBody
                ) 
            }; 

            statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(XmlSchema)),
                    "dsSchema",
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Variable(dsName),
                        "GetSchemaSerializable", 
                        new CodeExpression[] { } 
                    )
                ) 
            );

            statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable(collectionName), 
                        "Contains", 
                        new CodeExpression[] {
                            CodeGenHelper.Property( 
                                CodeGenHelper.Variable("dsSchema"),
                                "TargetNamespace"
                            )
                        } 
                    ),
                    condBodyOuter 
                ) 
            );
 
            statements.Add(
                CodeGenHelper.MethodCall(
                    CodeGenHelper.Argument("xs"),
                    "Add", 
                    new CodeExpression[] {
                        CodeGenHelper.Variable("dsSchema") 
                    } 
                )
            ); 
        }

        private CodeMemberMethod GetTypedDataSetSchema() {
            //\\ public static XmlSchemaComplexType GetDataSetSchema(XmlSchemaSet xs) { 
            //\\    XmlSchemaComplexType type = new XmlSchemaComplexType();
            //\\    XmlSchemaSequence sequence = new XmlSchemaSequence(); 
            //\\    XmlSchemaAny any = new XmlSchemaAny(); 
            //\\    any.Namespace = <dsNamespace>
            //\\    sequence.Items.Add(any); 
            //\\    type.Particle = sequence;
            //\\    XmlSchema dsSchema = ds.GetSchemaSerializable();
            //\\    if (xs.Contains(dsSchema.TargetNamespace)) {
            //\\        MemoryStream s1 = new MemoryStream(); 
            //\\        MemoryStream s2 = new MemoryStream();
            //\\        try { 
            //\\            XmlSchema schema = null; 
            //\\            dsSchema.Write(s1);
            //\\            for (IEnumerator schemas = xs.Schemas(dsSchema.TargetNamespace).GetEnumerator(); schemas.MoveNext(); ) { 
            //\\                schema = (XmlSchema)schemas.Current;
            //\\                s2.SetLength(0);
            //\\                schema.Write(s2);
            //\\                if ((s1.Length == s2.Length)) { 
            //\\                    s1.Position = 0;
            //\\                    s2.Position = 0; 
            //\\                    for (; ((s1.Position != s1.Length) && (s1.ReadByte() == s2.ReadByte())); ) { } 
            //\\                    if ((s1.Position == s1.Length)) {
            //\\                       return type; 
            //\\                    }
            //\\                }
            //\\            }
            //\\        } 
            //\\        finally {
            //\\            if (s1 != null) { 
            //\\                s1.Dispose(); 
            //\\            }
            //\\            if (s2 != null) { 
            //\\                s2.Dispose();
            //\\            }
            //\\        }
            //\\    } 
            //\\    xs.Add(dsSchema);
            //\\    return type; 
            //\\ } 

            CodeMemberMethod getDataSetSchema = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)), "GetTypedDataSetSchema", MemberAttributes.Static | MemberAttributes.Public); 
            getDataSetSchema.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(XmlSchemaSet)), "xs"));

            getDataSetSchema.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.Type(dataSource.GeneratorDataSetName),
                    "ds", 
                    CodeGenHelper.New( 
                        CodeGenHelper.Type(dataSource.GeneratorDataSetName),
                        new CodeExpression[] {} 
                    )
                )
            );
 
            getDataSetSchema.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)), 
                    "type",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)), new CodeExpression[] {}) 
                )
            );
            getDataSetSchema.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaSequence)),
                    "sequence", 
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaSequence)), new CodeExpression[] {}) 
                )
            ); 

            getDataSetSchema.Statements.Add(
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(XmlSchemaAny)), 
                    "any",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaAny)), new CodeExpression[] {}) 
                ) 
            );
            getDataSetSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any"), "Namespace"),
                    CodeGenHelper.Property(
                        CodeGenHelper.Variable("ds"), 
                        "Namespace"
                    ) 
                ) 
            );
            getDataSetSchema.Statements.Add( 
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Variable("sequence"), "Items"),
                        "Add", 
                        new CodeExpression[] { CodeGenHelper.Variable("any") }
                    ) 
                ) 
            );
 
            getDataSetSchema.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("type"), "Particle"),
                    CodeGenHelper.Variable("sequence") 
                )
            ); 
 
            // DDBugs 126260: Avoid adding the same schema twice
            GetSchemaIsInCollection(getDataSetSchema.Statements, "ds", "xs"); 

            getDataSetSchema.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable("type")));

            return getDataSetSchema; 
        }
 
        private CodeMemberProperty TablesProperty() { 
            CodeMemberProperty tablesProperty = CodeGenHelper.PropertyDecl(
                CodeGenHelper.GlobalType(typeof(System.Data.DataTableCollection)), 
                DataSourceNameHandler.TablesPropertyName,
                MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final
            );
            tablesProperty.CustomAttributes.Add( 
                CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerSerializationVisibilityAttribute",
                    CodeGenHelper.Field( 
                        CodeGenHelper.GlobalTypeExpr(typeof(DesignerSerializationVisibility)), "Hidden"))); 

            tablesProperty.GetStatements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Property(CodeGenHelper.Base(), "Tables"))
            );

            return tablesProperty; 
        }
 
        private CodeMemberProperty RelationsProperty() { 
            CodeMemberProperty relationsProperty = CodeGenHelper.PropertyDecl(
                CodeGenHelper.GlobalType(typeof(System.Data.DataRelationCollection)), 
                DataSourceNameHandler.RelationsPropertyName,
                MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final
            );
            relationsProperty.CustomAttributes.Add( 
                CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerSerializationVisibilityAttribute",
                    CodeGenHelper.Field( 
                        CodeGenHelper.GlobalTypeExpr(typeof(DesignerSerializationVisibility)), "Hidden"))); 

            relationsProperty.GetStatements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Property(CodeGenHelper.Base(), "Relations"))
            );

            return relationsProperty; 
        }
 
        private CodeMemberMethod InitExpressionsMethod() { 
            bool bInitExpressions = false;
 
            //\\  private void InitExpressions {
            //\\    this.<TableName>.<ColumnProperty>.Expression = "<ColumnExpression>";
            //\\    ....
            //\\  } 
            CodeMemberMethod initExpressions = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitExpressions", MemberAttributes.Private);
 
            foreach (DesignTable designTable in this.dataSource.DesignTables) { 
                DataTable table = designTable.DataTable;
                foreach (DataColumn column in table.Columns) { 
                    if (column.Expression.Length > 0) {
                        CodeExpression codeField = CodeGenHelper.Property(
                            CodeGenHelper.Property(CodeGenHelper.This(), designTable.GeneratorTablePropName),
                            codeGenerator.TableHandler.Tables[column.Table.TableName].DesignColumns[column.ColumnName].GeneratorColumnPropNameInTable 
                        );
 
                        bInitExpressions = true; 
                        initExpressions.Statements.Add(
                            CodeGenHelper.Assign( 
                                CodeGenHelper.Property(codeField, "Expression"),
                                CodeGenHelper.Str(column.Expression)
                            )
                        ); 
                    }
                } 
            } 

            if (bInitExpressions) { 
                return initExpressions;
            }
            else {
                return null; 
            }
        } 
 
        private bool TableContainsExpressions(DesignTable designTable) {
            DataTable table = designTable.DataTable; 
            foreach (DataColumn column in table.Columns) {
                if (column.Expression.Length > 0) {
                    return true;
                } 
            }
 
            return false; 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.CodeDom;
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 
    using System.Data.Common;
    using System.Xml; 
    using System.Xml.Schema; 
    using System.Xml.Serialization;
 
    internal sealed class DatasetMethodGenerator {
        private TypedDataSourceCodeGenerator codeGenerator = null;
        private DesignDataSource dataSource = null;
        private DataSet dataSet = null; 
        private CodeMemberMethod initExpressionsMethod = null;
        private static PropertyDescriptor namespaceProperty = TypeDescriptor.GetProperties(typeof(DataSet))["Namespace"]; 
        private static PropertyDescriptor localeProperty = TypeDescriptor.GetProperties(typeof(DataSet))["Locale"]; 
        private static PropertyDescriptor caseSensitiveProperty = TypeDescriptor.GetProperties(typeof(DataSet))["CaseSensitive"];
 
        internal DatasetMethodGenerator(TypedDataSourceCodeGenerator codeGenerator, DesignDataSource dataSource) {
            this.codeGenerator = codeGenerator;
            this.dataSource = dataSource;
            this.dataSet = dataSource.DataSet; 
        }
 
        internal void AddMethods(CodeTypeDeclaration dataSourceClass) { 
            AddSchemaSerializationModeMembers(dataSourceClass);
            initExpressionsMethod = InitExpressionsMethod(); 
            dataSourceClass.Members.Add(PublicConstructor());
            dataSourceClass.Members.Add( DeserializingConstructor() );
            dataSourceClass.Members.Add( InitializeDerivedDataSet() );
            dataSourceClass.Members.Add( CloneMethod(initExpressionsMethod) ); 
            dataSourceClass.Members.Add( ShouldSerializeTablesMethod() );
            dataSourceClass.Members.Add( ShouldSerializeRelationsMethod() ); 
            dataSourceClass.Members.Add( ReadXmlSerializableMethod() ); 
            dataSourceClass.Members.Add( GetSchemaSerializableMethod() );
            dataSourceClass.Members.Add( InitVarsParamLess() ); 
            CodeMemberMethod initClassMethod = null;
            CodeMemberMethod initVarsMethod = null;
            InitClassAndInitVarsMethods(out initClassMethod, out initVarsMethod);
            dataSourceClass.Members.Add( initVarsMethod ); 
            dataSourceClass.Members.Add( initClassMethod );
            AddShouldSerializeSingleTableMethods(dataSourceClass); 
            dataSourceClass.Members.Add( SchemaChangedMethod() ); 
            dataSourceClass.Members.Add( GetTypedDataSetSchema() );
            dataSourceClass.Members.Add( TablesProperty() ); 
            dataSourceClass.Members.Add( RelationsProperty() );
            if (initExpressionsMethod != null) {
                dataSourceClass.Members.Add(initExpressionsMethod);
            } 
        }
 
        private void AddSchemaSerializationModeMembers(CodeTypeDeclaration dataSourceClass) { 
            CodeMemberField schemaSerializationModeField = CodeGenHelper.FieldDecl(
                CodeGenHelper.GlobalType(typeof(System.Data.SchemaSerializationMode)), 
                "_schemaSerializationMode",
                CodeGenHelper.Field(
                    CodeGenHelper.GlobalTypeExpr(typeof(System.Data.SchemaSerializationMode)),
                    this.dataSource.SchemaSerializationMode.ToString() 
                )
            ); 
            dataSourceClass.Members.Add(schemaSerializationModeField); 

            CodeMemberProperty schemaSerializationModeProperty = CodeGenHelper.PropertyDecl( 
                CodeGenHelper.GlobalType(typeof(System.Data.SchemaSerializationMode)),
                "SchemaSerializationMode",
                MemberAttributes.Public | MemberAttributes.Override
            ); 
            schemaSerializationModeProperty.CustomAttributes.Add(
                CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.BrowsableAttribute).FullName, CodeGenHelper.Primitive(true)) 
            ); 
            schemaSerializationModeProperty.CustomAttributes.Add(
                CodeGenHelper.AttributeDecl( 
                    typeof(System.ComponentModel.DesignerSerializationVisibilityAttribute).FullName,
                    CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(DesignerSerializationVisibility)), "Visible")
                )
            ); 

 
            schemaSerializationModeProperty.GetStatements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), "_schemaSerializationMode"))
            ); 

            schemaSerializationModeProperty.SetStatements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), "_schemaSerializationMode"), 
                    CodeGenHelper.Argument("value")
                ) 
            ); 

            dataSourceClass.Members.Add(schemaSerializationModeProperty); 
        }

        private CodeConstructor PublicConstructor() {
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Public); 
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "BeginInit"));
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitClass")); 
            constructor.Statements.Add(CodeGenHelper.VariableDecl( 
                                            CodeGenHelper.GlobalType(typeof(CollectionChangeEventHandler)),
                                            "schemaChangedHandler", 
                                            new CodeDelegateCreateExpression(
                                                CodeGenHelper.GlobalType(typeof(CollectionChangeEventHandler)),
                                                CodeGenHelper.This(),
                                                "SchemaChanged"))); 
            constructor.Statements.Add(new System.CodeDom.CodeAttachEventStatement(
                                            new CodeEventReferenceExpression( 
                                                CodeGenHelper.Property(CodeGenHelper.Base(),"Tables"), 
                                                "CollectionChanged"),
                                            CodeGenHelper.Variable("schemaChangedHandler"))); 
            constructor.Statements.Add(new System.CodeDom.CodeAttachEventStatement(
                                            new CodeEventReferenceExpression(
                                                CodeGenHelper.Property(CodeGenHelper.Base(),"Relations"),
                                                "CollectionChanged"), 
                                            CodeGenHelper.Variable("schemaChangedHandler")));
            constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "EndInit")); 
 
            //\\ this.InitExpressions();
            // needs to be called after EndInit to ensure expressions with related tables are initialized correctly 
            if (initExpressionsMethod != null) {
                constructor.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitExpressions"));
            }
 
            return constructor;
        } 
 
        private CodeConstructor DeserializingConstructor() {
            //\\ protected NorthwindDataSet(SerializationInfo info, StreamingContext context) : base(info, context, false) { 
            //\\    if ((this.IsBinarySerialized(info, context) == true)) {
            //\\         this.InitVars(false);
            //\\        System.ComponentModel.CollectionChangeEventHandler schemaChangedHandler1 = new System.ComponentModel.CollectionChangeEventHandler(this.SchemaChanged);
            //\\        this.Tables.CollectionChanged += schemaChangedHandler1; 
            //\\        this.Relations.CollectionChanged += schemaChangedHandler1;
            //\\        return; 
            //\\    } 
            //\\    string strSchema = ((string)(info.GetValue("XmlSchema", typeof(string))));
            //\\    if(DetermineSchemaSerializationMode(info, context) == SchemaSerializationMode.SerializeSchema) { 
            //\\        System.Data.DataSet ds = new System.Data.DataSet();
            //\\        ds.EnforceConstraints = false;
            //\\        ds.ReadXmlSchema(new System.Xml.XmlTextReader(new System.IO.StringReader(strSchema)));
            //\\        if ((ds.Tables["Categories"] != null)) { 
            //\\            base.Tables.Add(new CategoriesDataTable(ds.Tables["Categories"]));
            //\\        } 
            //\\        this.DataSetName = ds.DataSetName; 
            //\\        this.Prefix = ds.Prefix;
            //\\        this.Namespace = ds.Namespace; 
            //\\        this.Locale = ds.Locale;
            //\\        this.CaseSensitive = ds.CaseSensitive;
            //\\        this.EnforceConstraints = ds.EnforceConstraints;
            //\\        this.Merge(ds, false, System.Data.MissingSchemaAction.Add); 
            //\\        this.InitVars();
            //\\    } 
            //\\    else { 
            //\\        this.ReadXmlSchema(new System.Xml.XmlTextReader(new System.IO.StringReader(strSchema)));
            //\\    } 
            //\\
            //\\    this.GetSerializationData(info, context);
            //\\    System.ComponentModel.CollectionChangeEventHandler schemaChangedHandler = new System.ComponentModel.CollectionChangeEventHandler(this.SchemaChanged);
            //\\    base.Tables.CollectionChanged += schemaChangedHandler; 
            //\\    this.Relations.CollectionChanged += schemaChangedHandler;
            //\\ } 
 
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Family);
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Runtime.Serialization.SerializationInfo)), "info")); 
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Runtime.Serialization.StreamingContext)) , "context"));
            constructor.BaseConstructorArgs.AddRange(new CodeExpression[] { CodeGenHelper.Argument("info"), CodeGenHelper.Argument("context"), CodeGenHelper.Primitive(false) });

            constructor.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.EQ( 
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.This(),
                            "IsBinarySerialized", 
                            new CodeExpression[] { CodeGenHelper.Argument("info"), CodeGenHelper.Argument("context") }
                        ),
                        CodeGenHelper.Primitive(true)),
                    new CodeStatement[] { 
                        CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(),"InitVars", CodeGenHelper.Primitive(false))),
                        CodeGenHelper.VariableDecl( 
                            CodeGenHelper.GlobalType(typeof(CollectionChangeEventHandler)), 
                            "schemaChangedHandler1",
                            new CodeDelegateCreateExpression( 
                                CodeGenHelper.GlobalType(typeof(CollectionChangeEventHandler)),
                                CodeGenHelper.This(),
                                "SchemaChanged"
                            ) 
                        ),
                        new System.CodeDom.CodeAttachEventStatement( 
                            new CodeEventReferenceExpression( 
                                CodeGenHelper.Property(CodeGenHelper.This(), "Tables"),
                                "CollectionChanged" 
                            ),
                            CodeGenHelper.Variable("schemaChangedHandler1")
                        ) ,
                        new System.CodeDom.CodeAttachEventStatement( 
                            new CodeEventReferenceExpression(
                                CodeGenHelper.Property(CodeGenHelper.This(), "Relations"), 
                                "CollectionChanged" 
                            ),
                            CodeGenHelper.Variable("schemaChangedHandler1") 
                        ),
                        CodeGenHelper.Return()
                    }
                ) 
            );
 
            constructor.Statements.Add( 
                CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(System.String)), "strSchema",
                CodeGenHelper.Cast(CodeGenHelper.GlobalType(typeof(System.String)), CodeGenHelper.MethodCall( 
                                                        CodeGenHelper.Argument("info"),
                                                        "GetValue",
                                                        new CodeExpression[] {
                                                            CodeGenHelper.Str("XmlSchema"), 
                                                            CodeGenHelper.TypeOf(CodeGenHelper.GlobalType(typeof(System.String)))
                                                        } )))); 
 
            ArrayList schemaBodySerializeSchema = new ArrayList();
            ArrayList schemaBodyDontSerializeSchema = new ArrayList(); 
            schemaBodySerializeSchema.Add(CodeGenHelper.VariableDecl(
                                CodeGenHelper.GlobalType(typeof(System.Data.DataSet)),
                                "ds",
                                CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Data.DataSet)), new CodeExpression[] {}))); 
            schemaBodySerializeSchema.Add(CodeGenHelper.Stm(
                                CodeGenHelper.MethodCall( 
                                    CodeGenHelper.Variable("ds"), 
                                    "ReadXmlSchema",
                                    new CodeExpression[] { 
                                        CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Xml.XmlTextReader)),
                                            new CodeExpression[] {
                                                CodeGenHelper.New(
                                                    CodeGenHelper.GlobalType(typeof(System.IO.StringReader)), 
                                                    new CodeExpression[] {
                                                        CodeGenHelper.Variable("strSchema") 
                                                    }) 
                                            })
                                    }))); 

            foreach(DesignTable table in codeGenerator.TableHandler.Tables) {
                //\\ this.Tables.Add(new <TableClassName>("<TableName>"));
                schemaBodySerializeSchema.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdNotEQ( 
                                        CodeGenHelper.Indexer( 
                                                CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Tables"),
                                                CodeGenHelper.Str(table.Name)), 
                                        CodeGenHelper.Primitive(null)),
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property(CodeGenHelper.Base(), "Tables"), 
                                "Add",
                                CodeGenHelper.New( 
                                    CodeGenHelper.Type(table.GeneratorTableClassName), 
                                    new CodeExpression[] {
                                        CodeGenHelper.Indexer( 
                                            CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Tables"),
                                            CodeGenHelper.Str(table.Name))
                                    }
                                ) 
                            )
                        ) 
                    ) 
                );
            } 

            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "DataSetName"), 
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"DataSetName")
                ) 
            ); 
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "Prefix"),
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Prefix")
                )
            ); 
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "Namespace"), 
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Namespace")
                ) 
            );
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "Locale"), 
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Locale")
                ) 
            ); 
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "CaseSensitive"),
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"CaseSensitive")
                )
            ); 
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property(CodeGenHelper.This(), "EnforceConstraints"), 
                    CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"EnforceConstraints")
                ) 
            );
            schemaBodySerializeSchema.Add(
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.This(),
                        "Merge", 
                        new CodeExpression[] { 
                            CodeGenHelper.Variable("ds"),
                            CodeGenHelper.Primitive(false), 
                            CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(MissingSchemaAction)),"Add")
                        }
                    )
                ) 
            );
            schemaBodySerializeSchema.Add( 
                CodeGenHelper.Stm( 
                    CodeGenHelper.MethodCall(CodeGenHelper.This(),"InitVars")
                ) 
            );

            schemaBodyDontSerializeSchema.Add(
                CodeGenHelper.Stm( 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.This(), 
                        "ReadXmlSchema", 
                        new CodeExpression[] {
                            CodeGenHelper.New( 
                                CodeGenHelper.GlobalType(typeof(System.Xml.XmlTextReader)),
                                new CodeExpression[] {
                                    CodeGenHelper.New(
                                        CodeGenHelper.GlobalType(typeof(System.IO.StringReader)), 
                                        new CodeExpression[] { CodeGenHelper.Variable("strSchema") }
                                    ) 
                                } 
                            )
                        } 
                    )
                )
            );
 
            if (initExpressionsMethod != null) {
                schemaBodyDontSerializeSchema.Add(CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitExpressions"))); 
            } 

            constructor.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.EQ(
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.This(), 
                            "DetermineSchemaSerializationMode",
                            new CodeExpression[] { CodeGenHelper.Argument("info"), CodeGenHelper.Argument("context") } 
                        ), 
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.SchemaSerializationMode)), 
                            "IncludeSchema"
                        )
                    ),
                    (CodeStatement[])schemaBodySerializeSchema.ToArray(typeof(CodeStatement)), 
                    (CodeStatement[])schemaBodyDontSerializeSchema.ToArray(typeof(CodeStatement))
                ) 
            ); 

            constructor.Statements.Add( 
                CodeGenHelper.MethodCall(
                    CodeGenHelper.This(),
                    "GetSerializationData",
                    new CodeExpression [] { 
                        CodeGenHelper.Argument("info"),
                        CodeGenHelper.Argument("context") 
                    } 
                )
            ); 
            constructor.Statements.Add(
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(CollectionChangeEventHandler)),
                    "schemaChangedHandler", 
                    new CodeDelegateCreateExpression(
                        CodeGenHelper.GlobalType( 
                            typeof(CollectionChangeEventHandler) 
                        ),
                        CodeGenHelper.This(), 
                        "SchemaChanged"
                    )
                )
            ); 
            constructor.Statements.Add(
                new System.CodeDom.CodeAttachEventStatement( 
                    new CodeEventReferenceExpression( 
                        CodeGenHelper.Property(CodeGenHelper.Base(),"Tables"),
                        "CollectionChanged" 
                    ),
                    CodeGenHelper.Variable("schemaChangedHandler")
                )
            ); 
            constructor.Statements.Add(
                new System.CodeDom.CodeAttachEventStatement( 
                    new CodeEventReferenceExpression( 
                        CodeGenHelper.Property(CodeGenHelper.This(),"Relations"),
                        "CollectionChanged" 
                    ),
                    CodeGenHelper.Variable("schemaChangedHandler")
                )
            ); 

            return constructor; 
        } 

        private CodeMemberMethod InitializeDerivedDataSet() { 
            //\\ protected override void InitializeDerivedDataSet(){
            //\\     this.BeginInit();
            //\\     this.InitClass();
            //\\     this.EndInit(); 
            //\\ }
            CodeMemberMethod initMethod = CodeGenHelper.MethodDecl( 
                CodeGenHelper.GlobalType(typeof(void)), 
                "InitializeDerivedDataSet",
                MemberAttributes.Family | MemberAttributes.Override 
            );

            initMethod.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "BeginInit"));
            initMethod.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitClass")); 
            initMethod.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.This(), "EndInit"));
 
            return initMethod; 
        }
 
        private CodeMemberMethod CloneMethod(CodeMemberMethod initExpressionsMethod) {
            //\\ public override DataSet Clone() {
            //\\     <DataSetClassName> cln = (<DataSetClassName>)base.Clone();
            //\\     cln.InitVars(); 
            //\\     cln.InitExpressions();
            //\\     cln.SchemaSerializationMode = this.SchemaSerializationMode(); 
            //\\     return cln; 
            //\\ }
            CodeMemberMethod clone = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataSet)), "Clone", MemberAttributes.Public | MemberAttributes.Override); 
                clone.Statements.Add(
                    CodeGenHelper.VariableDecl(
                        CodeGenHelper.Type(codeGenerator.DataSourceName),
                        "cln", 
                        CodeGenHelper.Cast(
                            CodeGenHelper.Type(codeGenerator.DataSourceName), 
                            CodeGenHelper.MethodCall(CodeGenHelper.Base(), "Clone", new CodeExpression[] {}) 
                        )
                    ) 
                );

                clone.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.Variable("cln"), "InitVars", new CodeExpression [] {}));
                if (initExpressionsMethod != null) { 
                    clone.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.Variable("cln"), "InitExpressions", new CodeExpression[] { }));
                } 
                clone.Statements.Add( 
                    CodeGenHelper.Assign(
                        CodeGenHelper.Property(CodeGenHelper.Variable("cln"), "SchemaSerializationMode"), 
                        CodeGenHelper.Property(CodeGenHelper.This(), "SchemaSerializationMode")
                    )
                );
 
                clone.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable("cln")));
 
            return clone; 
        }
 
        private CodeMemberMethod ShouldSerializeTablesMethod() {
            //\\ protected override bool ShouldSerializeTables() {
            //\\     return false;
            //\\ } 
            CodeMemberMethod shouldSerializeTables = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(System.Boolean)), "ShouldSerializeTables", MemberAttributes.Family | MemberAttributes.Override);
            shouldSerializeTables.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Primitive(false))); 
 
            return shouldSerializeTables;
        } 

        private CodeMemberMethod ShouldSerializeRelationsMethod() {
            //\\ protected override bool ShouldSerializeRelations() {
            //\\     return false; 
            //\\ }
            CodeMemberMethod shouldSerializeRelations = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(System.Boolean)), "ShouldSerializeRelations", MemberAttributes.Family | MemberAttributes.Override); 
            shouldSerializeRelations.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Primitive(false))); 

            return shouldSerializeRelations; 
        }

        private CodeMemberMethod ReadXmlSerializableMethod() {
            //\\ protected override void ReadXmlSerializable(XmlReader reader) { 
            //\\    if (GetSchemaSerializationMode(reader) == System.Data.SchemaSerializationMode.IncludeSchema) {
            //\\        this.Reset(); 
            //\\        System.Data.DataSet ds = new System.Data.DataSet(); 
            //\\        ds.ReadXml(reader);
            //\\        if ((ds.Tables["Categories"] != null)) { 
            //\\            base.Tables.Add(new CategoriesDataTable(ds.Tables["Categories"]));
            //\\        }
            //\\        this.DataSetName = ds.DataSetName;
            //\\        this.Prefix = ds.Prefix; 
            //\\        this.Namespace = ds.Namespace;
            //\\        this.Locale = ds.Locale; 
            //\\        this.CaseSensitive = ds.CaseSensitive; 
            //\\        this.EnforceConstraints = ds.EnforceConstraints;
            //\\        this.Merge(ds, false, System.Data.MissingSchemaAction.Add); 
            //\\        this.InitVars();
            //\\    }
            //\\    else {
            //\\        this.ReadXml(reader); 
            //\\        this.InitVars();
            //\\        return; 
            //\\    } 
            //\\ }
 
            CodeMemberMethod readXmlSerializable = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "ReadXmlSerializable", MemberAttributes.Family | MemberAttributes.Override);
            readXmlSerializable.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Xml.XmlReader)), "reader"));
            ArrayList includeSchemaStatements = new ArrayList();
            ArrayList excludeSchemaStatements = new ArrayList(); 
            includeSchemaStatements.Add(CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "Reset", new CodeExpression[] { })));
            includeSchemaStatements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataSet)), "ds", CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Data.DataSet)), new CodeExpression[] { }))); 
            includeSchemaStatements.Add( 
                CodeGenHelper.Stm(CodeGenHelper.MethodCall(
                    CodeGenHelper.Variable("ds"), 
                    "ReadXml",
                    new CodeExpression [] { CodeGenHelper.Argument("reader") }
                ))
            ); 
            foreach(DesignTable table in codeGenerator.TableHandler.Tables) {
                //\\ this.Tables.Add(new <TableClassName>("<TableName>")); 
                includeSchemaStatements.Add( 
                    CodeGenHelper.If(
                        CodeGenHelper.IdNotEQ( 
                            CodeGenHelper.Indexer(
                                CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Tables"),
                                CodeGenHelper.Str(table.Name)
                            ), 
                            CodeGenHelper.Primitive(null)
                        ), 
                        CodeGenHelper.Stm( 
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property(CodeGenHelper.Base(), "Tables"), 
                                "Add",
                                CodeGenHelper.New(
                                    CodeGenHelper.Type(table.GeneratorTableClassName),
                                    new CodeExpression[] { 
                                        CodeGenHelper.Indexer(
                                            CodeGenHelper.Property(CodeGenHelper.Variable("ds"),"Tables"), 
                                            CodeGenHelper.Str(table.Name) 
                                        )
                                    } 
                                )
                            )
                        )
                    ) // If 
                );
            } 
 
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "DataSetName"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "DataSetName")));
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Prefix"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "Prefix"))); 
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Namespace"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "Namespace")));
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Locale"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "Locale")));
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "CaseSensitive"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "CaseSensitive")));
            includeSchemaStatements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "EnforceConstraints"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "EnforceConstraints"))); 
            includeSchemaStatements.Add(
                CodeGenHelper.Stm(CodeGenHelper.MethodCall( 
                    CodeGenHelper.This(), 
                    "Merge",
                    new CodeExpression[] { 
                        CodeGenHelper.Variable("ds"),
                        CodeGenHelper.Primitive(false),
                        CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(MissingSchemaAction)), "Add")
                    } 
                ))
            ); 
            includeSchemaStatements.Add(CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitVars"))); 

            excludeSchemaStatements.Add(CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "ReadXml", new CodeExpression[] { CodeGenHelper.Argument("reader") }))); 
            excludeSchemaStatements.Add(CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitVars")));

            readXmlSerializable.Statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.EQ(
                        CodeGenHelper.MethodCall( 
                            CodeGenHelper.This(), 
                            "DetermineSchemaSerializationMode",
                            new CodeExpression[] { CodeGenHelper.Argument("reader") } 
                        ),
                        CodeGenHelper.Field(
                            CodeGenHelper.GlobalTypeExpr(typeof(System.Data.SchemaSerializationMode)),
                            "IncludeSchema" 
                        )
                    ), 
                    (CodeStatement[])includeSchemaStatements.ToArray(typeof(CodeStatement)), 
                    (CodeStatement[])excludeSchemaStatements.ToArray(typeof(CodeStatement))
                ) 
            );

            return readXmlSerializable;
        } 

        private CodeMemberMethod GetSchemaSerializableMethod() { 
            //\\ protected override System.Xml.Schema.XmlSchema GetSchemaSerializable() { 
            //\\     System.IO.MemoryStream stream = new System.IO.MemoryStream();
            //\\     WriteXmlSchema(new XmlTextWriter(stream, null )); 
            //\\     stream.Position = 0;
            //\\     return System.Xml.Schema.XmlSchema.Read(new XmlTextReader(stream));
            //\\ }
            CodeMemberMethod getSchemaSerializable = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(System.Xml.Schema.XmlSchema)), "GetSchemaSerializable", MemberAttributes.Family | MemberAttributes.Override); 
            getSchemaSerializable.Statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)), "stream", CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)),new CodeExpression[] {})));
            getSchemaSerializable.Statements.Add( 
                CodeGenHelper.MethodCall( 
                    CodeGenHelper.This(),
                    "WriteXmlSchema", 
                    CodeGenHelper.New(
                        CodeGenHelper.GlobalType(typeof(System.Xml.XmlTextWriter)),
                        new CodeExpression[] {
                            CodeGenHelper.Argument("stream"), 
                            CodeGenHelper.Primitive(null)
                        } 
                    ) 
                )
            ); 
            getSchemaSerializable.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.Argument("stream"), "Position"), CodeGenHelper.Primitive(0)));
            getSchemaSerializable.Statements.Add(
                CodeGenHelper.Return(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.GlobalTypeExpr(typeof(System.Xml.Schema.XmlSchema)),
                        "Read", 
                        new CodeExpression[] { 
                            CodeGenHelper.New(
                                CodeGenHelper.GlobalType(typeof(System.Xml.XmlTextReader)), 
                                new CodeExpression[] {CodeGenHelper.Argument("stream")}
                            ),
                            CodeGenHelper.Primitive(null)
                        } 
                    )
                ) 
            ); 

            return getSchemaSerializable; 
        }

        private CodeMemberMethod InitVarsParamLess() {
            //\\ public void InitVars() { 
            //\\    InitVars(true);
            //\\ } 
            CodeMemberMethod initVarsMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitVars", MemberAttributes.Assembly | MemberAttributes.Final); 
            initVarsMethod.Statements.Add(
                CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitVars", new CodeExpression[] { CodeGenHelper.Primitive(true) }) 
            );

            return initVarsMethod;
        } 

        private void InitClassAndInitVarsMethods(out CodeMemberMethod initClassMethod, out CodeMemberMethod initVarsMethod) { 
            //\\ private void InitClass() 
            initClassMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitClass", MemberAttributes.Private);
 
            //\\ public void InitVars(bool initTable)
            initVarsMethod = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitVars", MemberAttributes.Assembly | MemberAttributes.Final);
            initVarsMethod.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(Boolean)), "initTable"));
 
            //\\ this.DataSetName = "<dataSet.DataSetName>"
            initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "DataSetName"), CodeGenHelper.Str(dataSet.DataSetName))); 
            //\\ this.Prefix   = "<dataSet.Prefix>" 
            initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Prefix"), CodeGenHelper.Str(dataSet.Prefix)));
            if(namespaceProperty.ShouldSerializeValue(dataSet)) { 
                //\\ this.Namespace   = "<dataSet.Namespace>"
                initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Namespace"), CodeGenHelper.Str(dataSet.Namespace)));
            }
            if(localeProperty.ShouldSerializeValue(dataSet)) { 
                //\\ this.Locale = new System.Globalization.CultureInfo("dataSet.<Locale>");
                initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "Locale"), CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(System.Globalization.CultureInfo)),new CodeExpression[] {CodeGenHelper.Str(dataSet.Locale.ToString())}))); 
            } 
            if (caseSensitiveProperty.ShouldSerializeValue(dataSet)) {
                //\\ this.CaseSensitive = <dataSet.CaseSensitive>; 
                initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "CaseSensitive"), CodeGenHelper.Primitive(dataSet.CaseSensitive)));
            }
            //\\ this.EnforceConstraints = <dataSet.EnforceConstraints>;
            initClassMethod.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.This(), "EnforceConstraints"), CodeGenHelper.Primitive(dataSet.EnforceConstraints))); 

            //\\ this.SchemaSerializationMode = <dataSource.SchemaSerializationMode>; 
            initClassMethod.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.This(), "SchemaSerializationMode"), 
                    CodeGenHelper.Field(
                        CodeGenHelper.GlobalTypeExpr(typeof(System.Data.SchemaSerializationMode)),
                        this.dataSource.SchemaSerializationMode.ToString()
                    ) 
                )
            ); 
 
            // add statements to initialize tables
            foreach(DesignTable table in codeGenerator.TableHandler.Tables) { 
                CodeExpression fieldTable = CodeGenHelper.Field(CodeGenHelper.This(), table.GeneratorTableVarName);

                if (TableContainsExpressions(table)) {
                    //\\ <TableVariableName> = new <TableClassName>(false); 
                    initClassMethod.Statements.Add(CodeGenHelper.Assign(fieldTable, CodeGenHelper.New(CodeGenHelper.Type(table.GeneratorTableClassName), new CodeExpression[] { CodeGenHelper.Primitive(false) })));
                } 
                else { 
                    //\\ <TableVariableName> = new <TableClassName>();
                    initClassMethod.Statements.Add(CodeGenHelper.Assign(fieldTable, CodeGenHelper.New(CodeGenHelper.Type(table.GeneratorTableClassName), new CodeExpression[] { }))); 
                }

                //\\ this.Tables.Add(this.<TableVariableName>);
                initClassMethod.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.Property(CodeGenHelper.Base(), "Tables"), "Add", fieldTable)); 

                //\\ this.table<TableFieldName> = (<TableClassName>)this.Tables["<TableName>"]; 
                //\\ if (this.table<TableFieldName> != null) 
                //\\    this.table<TableFieldName>.InitVars();
                initVarsMethod.Statements.Add( 
                    CodeGenHelper.Assign(
                        fieldTable,
                        CodeGenHelper.Cast(
                            CodeGenHelper.Type(table.GeneratorTableClassName), 
                            CodeGenHelper.Indexer(
                                CodeGenHelper.Property(CodeGenHelper.Base(), "Tables"), 
                                CodeGenHelper.Str(table.Name) 
                            )
                        ) 
                    )
                );
                initVarsMethod.Statements.Add(
                    CodeGenHelper.If( 
                        CodeGenHelper.EQ(CodeGenHelper.Variable("initTable"), CodeGenHelper.Primitive(true)),
                        new CodeStatement[] { 
                            CodeGenHelper.If( 
                                CodeGenHelper.IdNotEQ(fieldTable, CodeGenHelper.Primitive(null)),
                                CodeGenHelper.Stm(CodeGenHelper.MethodCall(fieldTable, "InitVars")) 
                            )
                        }
                    )
                ); 
            }
 
            /*----------- Add Constraints to the Tables -------------------------*/ 
            CodeExpression varFkc = null;
            foreach(DesignTable designTable in codeGenerator.TableHandler.Tables) { 
                DataTable table = designTable.DataTable;
                foreach(Constraint constraint in table.Constraints) {
                    if (constraint is ForeignKeyConstraint) {
                        // We only initialize the foreign key constraints here. 
                        //\\ ForeignKeyConstraint fkc;
                        //\\ fkc = new ForeignKeyConstraint("<ConstrainName>", 
                        //\\     new DataColumn[] {this.table<TableClassName>.<ColumnName>Column}, // parent columns 
                        //\\     new DataColumn[] {this.table<TableClassName>.<ColumnName>Column}  // child columns
                        //\\ )); 
                        //\\ this.table<TableClassName>.Constraints.Add(fkc);
                        //\\ fkc.AcceptRejectRule = constraint.AcceptRejectRule;
                        //\\ fkc.DeleteRule = constraint.DeleteRule;
                        //\\ fkc.UpdateRule = constraint.UpdateRule; 

                        ForeignKeyConstraint fkc = (ForeignKeyConstraint) constraint; 
 
                        CodeArrayCreateExpression childrenColumns = new CodeArrayCreateExpression(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), 0);
                        foreach(DataColumn c in fkc.Columns) { 
                            childrenColumns.Initializers.Add(
                                CodeGenHelper.Property(
                                    CodeGenHelper.Field(CodeGenHelper.This(), codeGenerator.TableHandler.Tables[c.Table.TableName].GeneratorTableVarName),
                                    codeGenerator.TableHandler.Tables[c.Table.TableName].DesignColumns[c.ColumnName].GeneratorColumnPropNameInTable 
                                )
                            ); 
                        } 

                        CodeArrayCreateExpression parentColumns = new CodeArrayCreateExpression(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), 0); 
                        foreach(DataColumn c in fkc.RelatedColumns) {
                            parentColumns.Initializers.Add(
                                CodeGenHelper.Property(
                                    CodeGenHelper.Field(CodeGenHelper.This(), codeGenerator.TableHandler.Tables[c.Table.TableName].GeneratorTableVarName), 
                                    codeGenerator.TableHandler.Tables[c.Table.TableName].DesignColumns[c.ColumnName].GeneratorColumnPropNameInTable
                                ) 
                            ); 
                        }
 
                        if (varFkc == null) {
                            initClassMethod.Statements.Add(CodeGenHelper.VariableDecl(CodeGenHelper.GlobalType(typeof(System.Data.ForeignKeyConstraint)), "fkc"));
                            varFkc = CodeGenHelper.Variable("fkc");
                        } 

                        initClassMethod.Statements.Add( 
                            CodeGenHelper.Assign( 
                                varFkc,
                                CodeGenHelper.New( 
                                    CodeGenHelper.GlobalType(typeof(System.Data.ForeignKeyConstraint)),
                                    new CodeExpression[]{CodeGenHelper.Str(fkc.ConstraintName), parentColumns, childrenColumns}
                                )
                            ) 
                        );
 
                        initClassMethod.Statements.Add( 
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Property( 
                            CodeGenHelper.Field(CodeGenHelper.This(), codeGenerator.TableHandler.Tables[table.TableName].GeneratorTableVarName),
                                    "Constraints"
                                ),
                                "Add", 
                                varFkc
                            ) 
                        ); 

                        string acceptRejectRule = fkc.AcceptRejectRule.ToString(); 
                        string deleteRule = fkc.DeleteRule.ToString();
                        string updateRule = fkc.UpdateRule.ToString();
                        initClassMethod.Statements.Add(
                            CodeGenHelper.Assign( 
                                CodeGenHelper.Property(varFkc, "AcceptRejectRule"),
                                CodeGenHelper.Field( 
                                    CodeGenHelper.GlobalTypeExpr(fkc.AcceptRejectRule.GetType()), 
                                    acceptRejectRule
                                ) 
                            )
                        );

                        initClassMethod.Statements.Add( 
                            CodeGenHelper.Assign(
                                CodeGenHelper.Property(varFkc, "DeleteRule"), 
                                CodeGenHelper.Field( 
                                    CodeGenHelper.GlobalTypeExpr(fkc.DeleteRule.GetType()),
                                    deleteRule 
                                )
                            )
                        );
 
                        initClassMethod.Statements.Add(
                            CodeGenHelper.Assign( 
                                CodeGenHelper.Property(varFkc, "UpdateRule"), 
                                CodeGenHelper.Field(
                                    CodeGenHelper.GlobalTypeExpr(fkc.UpdateRule.GetType()), 
                                    updateRule
                                )
                            )
                        ); 
                    }
                } 
            } 

            /*----------- Add Relations to the Dataset -------------------------*/ 
            foreach(DesignRelation designRelation in codeGenerator.RelationHandler.Relations) {
                //\\ this.relation<RelationName>= new DataRelation("<RelationName>",
                //\\     new DataColumn[] {this.table<TableClassName>.<ColumnName>Column}, // parent columns
                //\\     new DataColumn[] {this.table<TableClassName>.<ColumnName>Column}, // child columns 
                //\\     false                                                             // createConstraints
                //\\ )); 
                DataRelation relation = designRelation.DataRelation; 
                if(relation == null) {
                    continue; 
                }
                CodeArrayCreateExpression parentColCreate =  new CodeArrayCreateExpression(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), 0);
                string parentTableField = designRelation.ParentDesignTable.GeneratorTableVarName;
                foreach(DataColumn column in relation.ParentColumns) { 
                    parentColCreate.Initializers.Add(
                        CodeGenHelper.Property( 
                            CodeGenHelper.Field(CodeGenHelper.This(), parentTableField), 
                            codeGenerator.TableHandler.Tables[column.Table.TableName].DesignColumns[column.ColumnName].GeneratorColumnPropNameInTable
                        ) 
                    );
                }

                CodeArrayCreateExpression childColCreate =  new CodeArrayCreateExpression(CodeGenHelper.GlobalType(typeof(System.Data.DataColumn)), 0); 
                string childTableField = designRelation.ChildDesignTable.GeneratorTableVarName;
                foreach(DataColumn column in relation.ChildColumns) { 
                    childColCreate.Initializers.Add( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Field(CodeGenHelper.This(), childTableField), 
                            codeGenerator.TableHandler.Tables[column.Table.TableName].DesignColumns[column.ColumnName].GeneratorColumnPropNameInTable
                        )
                    );
                } 

                CodeExpression relationVarExpression = CodeGenHelper.Field( 
                    CodeGenHelper.This(), 
                    codeGenerator.RelationHandler.Relations[relation.RelationName].GeneratorRelationVarName
                ); 

                initClassMethod.Statements.Add(
                    CodeGenHelper.Assign(
                        relationVarExpression, 
                        CodeGenHelper.New(
                            CodeGenHelper.GlobalType(typeof(System.Data.DataRelation)), 
                            new CodeExpression[] { 
                                CodeGenHelper.Str(relation.RelationName),
                                parentColCreate, 
                                childColCreate,
                                CodeGenHelper.Primitive(false)
                            }
                        ) 
                    )
                ); 
 
                if (relation.Nested) {
                    //\\ this.relation<RelationName>.Nested = true; 
                    initClassMethod.Statements.Add(
                        CodeGenHelper.Assign(
                            CodeGenHelper.Property(
                                relationVarExpression, 
                                "Nested"
                            ), 
                            CodeGenHelper.Primitive(true) 
                        )
                    ); 
                }

                // Add extended properties to the DataRelation
                ExtendedPropertiesHandler.CodeGenerator = codeGenerator; 
                ExtendedPropertiesHandler.AddExtendedProperties(designRelation, relationVarExpression, initClassMethod.Statements, relation.ExtendedProperties);
 
                //\\ this.Relations.Add(this.relation<RelationName>); 
                initClassMethod.Statements.Add(
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Property(
                            CodeGenHelper.This(),
                            "Relations"
                        ), 
                        "Add",
                        relationVarExpression 
                    ) 
                );
 
                //\\ this.relation<RelationName> = this.Relations["<RelationName>"];
                initVarsMethod.Statements.Add(
                    CodeGenHelper.Assign(
                        relationVarExpression, 
                        CodeGenHelper.Indexer(
                            CodeGenHelper.Property(CodeGenHelper.This(), "Relations"), 
                            CodeGenHelper.Str(relation.RelationName) 
                        )
                    ) 
                );
            }

            // Add extended properties to the dataset, if there are any in the schema file 
            ExtendedPropertiesHandler.CodeGenerator = codeGenerator;
            ExtendedPropertiesHandler.AddExtendedProperties(this.dataSource, CodeGenHelper.This(), initClassMethod.Statements, dataSet.ExtendedProperties); 
 
        } // end InitClassAndInitVarsMethods(...)
 
        private void AddShouldSerializeSingleTableMethods(CodeTypeDeclaration dataSourceClass) {
            foreach(DesignTable table in codeGenerator.TableHandler.Tables) {
                string tablePropertyName = table.GeneratorTablePropName;
                string shouldSerializeMethodName = MemberNameValidator.GenerateIdName("ShouldSerialize" + tablePropertyName, this.codeGenerator.CodeProvider, false /*useSuffix*/); 

                CodeMemberMethod shouldSerializeTableProperty = 
                    CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(System.Boolean)), shouldSerializeMethodName, MemberAttributes.Private); 
                shouldSerializeTableProperty.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Primitive(false)));
                dataSourceClass.Members.Add(shouldSerializeTableProperty); 
            }
        }

 
        private CodeMemberMethod SchemaChangedMethod() {
            CodeMemberMethod schemaChanged = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "SchemaChanged", MemberAttributes.Private); 
            schemaChanged.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(object)), "sender")); 
            schemaChanged.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(CollectionChangeEventArgs)), "e"));
            schemaChanged.Statements.Add( 
                CodeGenHelper.If(
                    CodeGenHelper.EQ(
                        CodeGenHelper.Property(CodeGenHelper.Argument("e"), "Action"),
                        CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(CollectionChangeAction)), "Remove") 
                    ),
                    CodeGenHelper.Stm(CodeGenHelper.MethodCall(CodeGenHelper.This(), "InitVars")) 
                ) 
            );
 
            return schemaChanged;
        }

//        private CodeMemberMethod GetTypedDataSetSchema() { 
//            //\\ public static XmlSchemaType GetTypedDataSetSchema(XmlSchemaSet xs) {
//            //\\     Authors_DS ds = new Authors_DS(); 
//            //\\     xs.Add(ds.GetSchemaSerializable()); 
//            //\\     XmlSchemaComplexType type = DataSet.GetDataSetSchema(xs);
//            //\\     XmlSchemaAttribute attribute = new XmlSchemaAttribute(); 
//            //\\     attribute.Name = "namespace";
//            //\\     attribute.FixedValue = ds.Namespace;
//            //\\     type.Attributes.Add(attribute);
//            //\\     return type; 
//            //\\ }
//            CodeMemberMethod getTypedDataSetSchema = CodeGenHelper.MethodDecl(typeof(XmlSchemaType), "GetTypedDataSetSchema", MemberAttributes.Static | MemberAttributes.Public); 
//            getTypedDataSetSchema.Parameters.Add(CodeGenHelper.ParameterDecl(typeof(XmlSchemaSet), "xs")); 
//
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.VariableDecl(dataSource.GeneratorDataSetName, "ds", CodeGenHelper.New(dataSource.GeneratorDataSetName, new CodeExpression[] {}))); 
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.Argument("xs"), "Add", new CodeExpression [] { CodeGenHelper.MethodCall(CodeGenHelper.Variable("ds"), "GetSchemaSerializable", new CodeExpression[] {})}));
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.VariableDecl(typeof(XmlSchemaComplexType), "type", CodeGenHelper.MethodCall(CodeGenHelper.TypeExpr(typeof(DataSet)), "GetDataSetSchema", new CodeExpression[] {CodeGenHelper.Argument("xs")})));
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.VariableDecl(typeof(XmlSchemaAttribute), "attribute", CodeGenHelper.New(typeof(XmlSchemaAttribute), new CodeExpression[] {})));
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.Variable("attribute"), "Name"), CodeGenHelper.Primitive("namespace"))); 
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.Assign(CodeGenHelper.Property(CodeGenHelper.Variable("attribute"),"FixedValue"), CodeGenHelper.Property(CodeGenHelper.Variable("ds"), "Namespace")));
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.MethodCall(CodeGenHelper.Property(CodeGenHelper.Variable("type"),"Attributes"), "Add", new CodeExpression [] { CodeGenHelper.Variable("attribute") })); 
//            getTypedDataSetSchema.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable("type"))); 
//
//            return getTypedDataSetSchema; 
//        }

        internal static void GetSchemaIsInCollection(CodeStatementCollection statements, string dsName, string collectionName)
        { 
            CodeStatement[] condBodyInner = new CodeStatement[] {
                CodeGenHelper.Assign( 
                    CodeGenHelper.Property( 
                        CodeGenHelper.Variable("s1"),
                        "Position" 
                    ),
                    CodeGenHelper.Primitive(0)
                ),
 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property( 
                        CodeGenHelper.Variable("s2"), 
                        "Position"
                    ), 
                    CodeGenHelper.Primitive(0)
                ),

                CodeGenHelper.ForLoop( 
                    CodeGenHelper.Stm(
                        new CodeSnippetExpression("") 
                    ), 
                    CodeGenHelper.And(
                        CodeGenHelper.IdNotEQ( 
                            CodeGenHelper.Property(
                                CodeGenHelper.Variable("s1"),
                                "Position"
                            ), 
                            CodeGenHelper.Property(
                                CodeGenHelper.Variable("s1"), 
                                "Length" 
                            )
                        ), 
                        CodeGenHelper.EQ(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Variable("s1"),
                                "ReadByte", 
                                new CodeExpression[] {}
                            ), 
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Variable("s2"),
                                "ReadByte", 
                                new CodeExpression[] {}
                            )
                        )
                    ), 
                    CodeGenHelper.Stm(
                        new CodeSnippetExpression("") 
                    ), 
                    new CodeStatement[] {
                        CodeGenHelper.Stm( 
                            new CodeSnippetExpression("")
                        )
                    }
                ), 

                CodeGenHelper.If( 
                    CodeGenHelper.EQ( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Variable("s1"), 
                            "Position"
                        ),
                        CodeGenHelper.Property(
                            CodeGenHelper.Variable("s1"), 
                            "Length"
                        ) 
                    ), 
                    new CodeStatement[] {
                        CodeGenHelper.Return( 
                            CodeGenHelper.Variable("type")
                        )
                    }
                ) 
            };
 
            CodeStatement[] loopBody = new CodeStatement[] { 
                CodeGenHelper.Assign(
                    CodeGenHelper.Variable("schema"), 
                    CodeGenHelper.Cast(
                        CodeGenHelper.GlobalType(typeof(XmlSchema)),
                        CodeGenHelper.Property(
                            CodeGenHelper.Variable("schemas"), 
                            "Current"
                        ) 
                    ) 
                ),
 
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable("s2"),
                        "SetLength", 
                        new CodeExpression[] {
                            CodeGenHelper.Primitive(0) 
                        } 
                    )
                ), 

                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable("schema"), 
                        "Write",
                        new CodeExpression[] { 
                            CodeGenHelper.Variable("s2") 
                        }
                    ) 
                ),

                CodeGenHelper.If(
                    CodeGenHelper.EQ( 
                        CodeGenHelper.Property(
                            CodeGenHelper.Variable("s1"), 
                            "Length" 
                        ),
                        CodeGenHelper.Property( 
                            CodeGenHelper.Variable("s2"),
                            "Length"
                        )
                    ), 
                    condBodyInner
                ) 
            }; 

            CodeStatement[] tryBody = new CodeStatement[] { 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(XmlSchema)),
                    "schema",
                    CodeGenHelper.Primitive(null) 
                ),
 
                CodeGenHelper.Stm( 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable("dsSchema"), 
                        "Write",
                        new CodeExpression[] {
                            CodeGenHelper.Variable("s1")
                        } 
                    )
                ), 
 
                CodeGenHelper.ForLoop(
                    CodeGenHelper.VariableDecl( 
                        CodeGenHelper.GlobalType(typeof(IEnumerator)),
                        "schemas",
                        CodeGenHelper.MethodCall(
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Variable(collectionName),
                                "Schemas", 
                                new CodeExpression[] { 
                                    CodeGenHelper.Property(
                                        CodeGenHelper.Variable("dsSchema"), 
                                        "TargetNamespace"
                                    )
                                }
                            ), 
                            "GetEnumerator",
                            new CodeExpression[] { } 
                        ) 
                    ),
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Variable("schemas"),
                        "MoveNext",
                        new CodeExpression[] { }
                    ), 
                    CodeGenHelper.Stm(
                        new CodeSnippetExpression("") 
                    ), 
                    loopBody
                ) 
            };

            CodeStatement[] finallyBody = new CodeStatement[] {
                CodeGenHelper.If( 
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Variable("s1"), 
                        CodeGenHelper.Primitive(null) 
                    ),
                    new CodeStatement[] { 
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall(
                                CodeGenHelper.Variable("s1"),
                                "Close", 
                                new CodeExpression[] { }
                            ) 
                        ) 
                    }
                ), 

                CodeGenHelper.If(
                    CodeGenHelper.IdNotEQ(
                        CodeGenHelper.Variable("s2"), 
                        CodeGenHelper.Primitive(null)
                    ), 
                    new CodeStatement[] { 
                        CodeGenHelper.Stm(
                            CodeGenHelper.MethodCall( 
                                CodeGenHelper.Variable("s2"),
                                "Close",
                                new CodeExpression[] { }
                            ) 
                        )
                    } 
                ) 
            };
 
            CodeStatement[] condBodyOuter = new CodeStatement[] {
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)),
                    "s1", 
                    CodeGenHelper.New(
                        CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)), 
                        new CodeExpression[] { } 
                    )
                ), 

                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)),
                    "s2", 
                    CodeGenHelper.New(
                        CodeGenHelper.GlobalType(typeof(System.IO.MemoryStream)), 
                        new CodeExpression[] { } 
                    )
                ), 

                CodeGenHelper.Try(
                    tryBody,
                    new CodeCatchClause[] { }, 
                    finallyBody
                ) 
            }; 

            statements.Add( 
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(XmlSchema)),
                    "dsSchema",
                    CodeGenHelper.MethodCall( 
                        CodeGenHelper.Variable(dsName),
                        "GetSchemaSerializable", 
                        new CodeExpression[] { } 
                    )
                ) 
            );

            statements.Add(
                CodeGenHelper.If( 
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Variable(collectionName), 
                        "Contains", 
                        new CodeExpression[] {
                            CodeGenHelper.Property( 
                                CodeGenHelper.Variable("dsSchema"),
                                "TargetNamespace"
                            )
                        } 
                    ),
                    condBodyOuter 
                ) 
            );
 
            statements.Add(
                CodeGenHelper.MethodCall(
                    CodeGenHelper.Argument("xs"),
                    "Add", 
                    new CodeExpression[] {
                        CodeGenHelper.Variable("dsSchema") 
                    } 
                )
            ); 
        }

        private CodeMemberMethod GetTypedDataSetSchema() {
            //\\ public static XmlSchemaComplexType GetDataSetSchema(XmlSchemaSet xs) { 
            //\\    XmlSchemaComplexType type = new XmlSchemaComplexType();
            //\\    XmlSchemaSequence sequence = new XmlSchemaSequence(); 
            //\\    XmlSchemaAny any = new XmlSchemaAny(); 
            //\\    any.Namespace = <dsNamespace>
            //\\    sequence.Items.Add(any); 
            //\\    type.Particle = sequence;
            //\\    XmlSchema dsSchema = ds.GetSchemaSerializable();
            //\\    if (xs.Contains(dsSchema.TargetNamespace)) {
            //\\        MemoryStream s1 = new MemoryStream(); 
            //\\        MemoryStream s2 = new MemoryStream();
            //\\        try { 
            //\\            XmlSchema schema = null; 
            //\\            dsSchema.Write(s1);
            //\\            for (IEnumerator schemas = xs.Schemas(dsSchema.TargetNamespace).GetEnumerator(); schemas.MoveNext(); ) { 
            //\\                schema = (XmlSchema)schemas.Current;
            //\\                s2.SetLength(0);
            //\\                schema.Write(s2);
            //\\                if ((s1.Length == s2.Length)) { 
            //\\                    s1.Position = 0;
            //\\                    s2.Position = 0; 
            //\\                    for (; ((s1.Position != s1.Length) && (s1.ReadByte() == s2.ReadByte())); ) { } 
            //\\                    if ((s1.Position == s1.Length)) {
            //\\                       return type; 
            //\\                    }
            //\\                }
            //\\            }
            //\\        } 
            //\\        finally {
            //\\            if (s1 != null) { 
            //\\                s1.Dispose(); 
            //\\            }
            //\\            if (s2 != null) { 
            //\\                s2.Dispose();
            //\\            }
            //\\        }
            //\\    } 
            //\\    xs.Add(dsSchema);
            //\\    return type; 
            //\\ } 

            CodeMemberMethod getDataSetSchema = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)), "GetTypedDataSetSchema", MemberAttributes.Static | MemberAttributes.Public); 
            getDataSetSchema.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(XmlSchemaSet)), "xs"));

            getDataSetSchema.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.Type(dataSource.GeneratorDataSetName),
                    "ds", 
                    CodeGenHelper.New( 
                        CodeGenHelper.Type(dataSource.GeneratorDataSetName),
                        new CodeExpression[] {} 
                    )
                )
            );
 
            getDataSetSchema.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)), 
                    "type",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaComplexType)), new CodeExpression[] {}) 
                )
            );
            getDataSetSchema.Statements.Add(
                CodeGenHelper.VariableDecl( 
                    CodeGenHelper.GlobalType(typeof(XmlSchemaSequence)),
                    "sequence", 
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaSequence)), new CodeExpression[] {}) 
                )
            ); 

            getDataSetSchema.Statements.Add(
                CodeGenHelper.VariableDecl(
                    CodeGenHelper.GlobalType(typeof(XmlSchemaAny)), 
                    "any",
                    CodeGenHelper.New(CodeGenHelper.GlobalType(typeof(XmlSchemaAny)), new CodeExpression[] {}) 
                ) 
            );
            getDataSetSchema.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("any"), "Namespace"),
                    CodeGenHelper.Property(
                        CodeGenHelper.Variable("ds"), 
                        "Namespace"
                    ) 
                ) 
            );
            getDataSetSchema.Statements.Add( 
                CodeGenHelper.Stm(
                    CodeGenHelper.MethodCall(
                        CodeGenHelper.Property(CodeGenHelper.Variable("sequence"), "Items"),
                        "Add", 
                        new CodeExpression[] { CodeGenHelper.Variable("any") }
                    ) 
                ) 
            );
 
            getDataSetSchema.Statements.Add(
                CodeGenHelper.Assign(
                    CodeGenHelper.Property(CodeGenHelper.Variable("type"), "Particle"),
                    CodeGenHelper.Variable("sequence") 
                )
            ); 
 
            // DDBugs 126260: Avoid adding the same schema twice
            GetSchemaIsInCollection(getDataSetSchema.Statements, "ds", "xs"); 

            getDataSetSchema.Statements.Add(CodeGenHelper.Return(CodeGenHelper.Variable("type")));

            return getDataSetSchema; 
        }
 
        private CodeMemberProperty TablesProperty() { 
            CodeMemberProperty tablesProperty = CodeGenHelper.PropertyDecl(
                CodeGenHelper.GlobalType(typeof(System.Data.DataTableCollection)), 
                DataSourceNameHandler.TablesPropertyName,
                MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final
            );
            tablesProperty.CustomAttributes.Add( 
                CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerSerializationVisibilityAttribute",
                    CodeGenHelper.Field( 
                        CodeGenHelper.GlobalTypeExpr(typeof(DesignerSerializationVisibility)), "Hidden"))); 

            tablesProperty.GetStatements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Property(CodeGenHelper.Base(), "Tables"))
            );

            return tablesProperty; 
        }
 
        private CodeMemberProperty RelationsProperty() { 
            CodeMemberProperty relationsProperty = CodeGenHelper.PropertyDecl(
                CodeGenHelper.GlobalType(typeof(System.Data.DataRelationCollection)), 
                DataSourceNameHandler.RelationsPropertyName,
                MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final
            );
            relationsProperty.CustomAttributes.Add( 
                CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerSerializationVisibilityAttribute",
                    CodeGenHelper.Field( 
                        CodeGenHelper.GlobalTypeExpr(typeof(DesignerSerializationVisibility)), "Hidden"))); 

            relationsProperty.GetStatements.Add( 
                CodeGenHelper.Return(CodeGenHelper.Property(CodeGenHelper.Base(), "Relations"))
            );

            return relationsProperty; 
        }
 
        private CodeMemberMethod InitExpressionsMethod() { 
            bool bInitExpressions = false;
 
            //\\  private void InitExpressions {
            //\\    this.<TableName>.<ColumnProperty>.Expression = "<ColumnExpression>";
            //\\    ....
            //\\  } 
            CodeMemberMethod initExpressions = CodeGenHelper.MethodDecl(CodeGenHelper.GlobalType(typeof(void)), "InitExpressions", MemberAttributes.Private);
 
            foreach (DesignTable designTable in this.dataSource.DesignTables) { 
                DataTable table = designTable.DataTable;
                foreach (DataColumn column in table.Columns) { 
                    if (column.Expression.Length > 0) {
                        CodeExpression codeField = CodeGenHelper.Property(
                            CodeGenHelper.Property(CodeGenHelper.This(), designTable.GeneratorTablePropName),
                            codeGenerator.TableHandler.Tables[column.Table.TableName].DesignColumns[column.ColumnName].GeneratorColumnPropNameInTable 
                        );
 
                        bInitExpressions = true; 
                        initExpressions.Statements.Add(
                            CodeGenHelper.Assign( 
                                CodeGenHelper.Property(codeField, "Expression"),
                                CodeGenHelper.Str(column.Expression)
                            )
                        ); 
                    }
                } 
            } 

            if (bInitExpressions) { 
                return initExpressions;
            }
            else {
                return null; 
            }
        } 
 
        private bool TableContainsExpressions(DesignTable designTable) {
            DataTable table = designTable.DataTable; 
            foreach (DataColumn column in table.Columns) {
                if (column.Expression.Length > 0) {
                    return true;
                } 
            }
 
            return false; 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
