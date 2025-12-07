 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
 
    using System;
    using System.Data;
    using System.CodeDom;
    using System.Reflection; 

    using System.CodeDom.Compiler; 
 
    internal sealed class TypedRowGenerator {
        private TypedDataSourceCodeGenerator codeGenerator = null; 
        private MethodInfo convertXmlToObject = null;


        internal TypedRowGenerator(TypedDataSourceCodeGenerator codeGenerator) { 
            this.codeGenerator = codeGenerator;
            // this is an internal method in the DataColumn class that we need to access 
            // 

            convertXmlToObject = typeof(DataColumn).GetMethod("ConvertXmlToObject", BindingFlags.Instance | BindingFlags.NonPublic, 
                                                               null, CallingConventions.Any, new Type[] { typeof(string)}, null);

        }
 
        internal MethodInfo ConvertXmlToObject {
            get { 
                return convertXmlToObject; 
            }
        } 

        internal void GenerateRows(CodeTypeDeclaration dataSourceClass) {
            foreach(DesignTable table in codeGenerator.TableHandler.Tables) {
                dataSourceClass.Members.Add(GenerateRow(table)); 
            }
        } 
 
        internal void GenerateTypedRowEventHandlers(CodeTypeDeclaration dataSourceClass) {
            if (this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareEvents) && this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareDelegates)) { 
                foreach (DesignTable table in codeGenerator.TableHandler.Tables) {
                    dataSourceClass.Members.Add(GenerateTypedRowEventHandler(table));
                }
            } 
        }
 
        internal void GenerateTypedRowEventArgs(CodeTypeDeclaration dataSourceClass) { 
            if (this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareEvents) && this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareDelegates)) {
                foreach (DesignTable table in codeGenerator.TableHandler.Tables) { 
                    dataSourceClass.Members.Add(CreateTypedRowEventArg(table));
                }
            }
        } 

        // generates the typed row EventArg class for a specific table 
        private CodeTypeDeclaration CreateTypedRowEventArg(DesignTable designTable) { 
            if(designTable == null) {
                throw new InternalException("DesignTable should not be null."); 
            }

            DataTable table = designTable.DataTable;
            string rowClassName = designTable.GeneratorRowClassName; 
            string tableClassName = designTable.GeneratorTableClassName;
            string rowConcreteClassName = designTable.GeneratorRowClassName; 
 
            CodeTypeDeclaration rowEventArgClass = CodeGenHelper.Class(designTable.GeneratorRowEvArgName, false, TypeAttributes.Public);
            rowEventArgClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(EventArgs))); 

            rowEventArgClass.Comments.Add(CodeGenHelper.Comment("Row event argument class", true));

            //\\ private <RowConcreteClassName> eventRow; 
            rowEventArgClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.Type(rowConcreteClassName), "eventRow"));
            //\\ private DataRowAction eventAction; 
            rowEventArgClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRowAction)), "eventAction")); 

            // add constructor 
            rowEventArgClass.Members.Add(EventArgConstructor(rowConcreteClassName));

            //\\ public <rowConcreteClassName> COMPUTERRow {
            //\\     get { return this.eventRow; } 
            //\\ }
            CodeMemberProperty rowProp = CodeGenHelper.PropertyDecl( 
                CodeGenHelper.Type(rowConcreteClassName), 
                "Row",
                MemberAttributes.Public | MemberAttributes.Final 
            );
            rowProp.GetStatements.Add(CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), "eventRow")));

            rowEventArgClass.Members.Add(rowProp); 

            //\\ public DataRowAction Action { 
            //\\     get { return this.eventAction; } 
            //\\ }
            rowProp = CodeGenHelper.PropertyDecl( 
                CodeGenHelper.GlobalType(typeof(System.Data.DataRowAction)),
                "Action",
                MemberAttributes.Public | MemberAttributes.Final
            ); 
            rowProp.GetStatements.Add(CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), "eventAction")));
            rowEventArgClass.Members.Add(rowProp); 
 
            return rowEventArgClass;
        } 

        private CodeTypeDelegate GenerateTypedRowEventHandler(DesignTable table) {
            if(table == null) {
                throw new InternalException("DesignTable should not be null."); 
            }
                string rowClassName = table.GeneratorRowClassName; 
 
            //\\ public delegate void <RowClassName>ChangeEventHandler(object sender, <RowClassName>ChangeEvent e);
                CodeTypeDelegate delegateClass = new CodeTypeDelegate(table.GeneratorRowEvHandlerName); 
            delegateClass.TypeAttributes |= TypeAttributes.Public;
            delegateClass.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(object)), "sender"));
            delegateClass.Parameters.Add(
                CodeGenHelper.ParameterDecl( 
                    CodeGenHelper.Type(table.GeneratorRowEvArgName),
                    "e" 
                ) 
            );
 
            return delegateClass;
        }

 
        private CodeTypeDeclaration GenerateRow(DesignTable table) {
            if(table == null) { 
                throw new InternalException("DesignTable should not be null."); 
            }
            string rowClassName                 = table.GeneratorRowClassName; 
            string tableClassName               = table.GeneratorTableClassName;
            string tableFieldName               = table.GeneratorTableVarName;
            TypedColumnHandler columnHandler    = codeGenerator.TableHandler.GetColumnHandler(table.Name);
 
            // create CodeTypeDeclaration, set class name and base type
            CodeTypeDeclaration rowClass = CodeGenHelper.Class(rowClassName, true, TypeAttributes.Public); 
            rowClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(System.Data.DataRow))); 

            rowClass.Comments.Add(CodeGenHelper.Comment("Represents strongly named DataRow class.", true)); 

            //\\ <TableClassName> <TableFieldName>;
            rowClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.Type(tableClassName), tableFieldName));
 
            // add constructor
            rowClass.Members.Add(RowConstructor(tableClassName, tableFieldName)); 
 
            // add Column Properties
            columnHandler.AddRowColumnProperties(rowClass); 

            // add GetChildRows/GetParentRows methods
            columnHandler.AddRowGetRelatedRowsMethods(rowClass);
 

            return rowClass; 
        } 

        private CodeConstructor RowConstructor(string tableClassName, string tableFieldName) { 
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Assembly | MemberAttributes.Final);
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRowBuilder)), "rb"));
            constructor.BaseConstructorArgs.Add(CodeGenHelper.Argument("rb"));
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), tableFieldName), 
                    CodeGenHelper.Cast( 
                        CodeGenHelper.Type(tableClassName),
                        CodeGenHelper.Property(CodeGenHelper.This(), "Table") 
                    )
                )
            );
 
            return constructor;
        } 
 
        private CodeConstructor EventArgConstructor(string rowConcreteClassName) {
            //\\ public <rowEventArgClassName>ChangeEvent(rowEventArgClassName row, DataRowAction action) { 
            //\\     this.eventRow    = row;
            //\\     this.eventAction = action;
            //\\ }
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Public | MemberAttributes.Final); 
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.Type(rowConcreteClassName), "row"   ));
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRowAction)), "action")); 
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), "eventRow"), 
                    CodeGenHelper.Argument("row")
                )
            );
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), "eventAction"), 
                    CodeGenHelper.Argument("action") 
                )
            ); 

            return constructor;
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
    using System.Data;
    using System.CodeDom;
    using System.Reflection; 

    using System.CodeDom.Compiler; 
 
    internal sealed class TypedRowGenerator {
        private TypedDataSourceCodeGenerator codeGenerator = null; 
        private MethodInfo convertXmlToObject = null;


        internal TypedRowGenerator(TypedDataSourceCodeGenerator codeGenerator) { 
            this.codeGenerator = codeGenerator;
            // this is an internal method in the DataColumn class that we need to access 
            // 

            convertXmlToObject = typeof(DataColumn).GetMethod("ConvertXmlToObject", BindingFlags.Instance | BindingFlags.NonPublic, 
                                                               null, CallingConventions.Any, new Type[] { typeof(string)}, null);

        }
 
        internal MethodInfo ConvertXmlToObject {
            get { 
                return convertXmlToObject; 
            }
        } 

        internal void GenerateRows(CodeTypeDeclaration dataSourceClass) {
            foreach(DesignTable table in codeGenerator.TableHandler.Tables) {
                dataSourceClass.Members.Add(GenerateRow(table)); 
            }
        } 
 
        internal void GenerateTypedRowEventHandlers(CodeTypeDeclaration dataSourceClass) {
            if (this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareEvents) && this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareDelegates)) { 
                foreach (DesignTable table in codeGenerator.TableHandler.Tables) {
                    dataSourceClass.Members.Add(GenerateTypedRowEventHandler(table));
                }
            } 
        }
 
        internal void GenerateTypedRowEventArgs(CodeTypeDeclaration dataSourceClass) { 
            if (this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareEvents) && this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareDelegates)) {
                foreach (DesignTable table in codeGenerator.TableHandler.Tables) { 
                    dataSourceClass.Members.Add(CreateTypedRowEventArg(table));
                }
            }
        } 

        // generates the typed row EventArg class for a specific table 
        private CodeTypeDeclaration CreateTypedRowEventArg(DesignTable designTable) { 
            if(designTable == null) {
                throw new InternalException("DesignTable should not be null."); 
            }

            DataTable table = designTable.DataTable;
            string rowClassName = designTable.GeneratorRowClassName; 
            string tableClassName = designTable.GeneratorTableClassName;
            string rowConcreteClassName = designTable.GeneratorRowClassName; 
 
            CodeTypeDeclaration rowEventArgClass = CodeGenHelper.Class(designTable.GeneratorRowEvArgName, false, TypeAttributes.Public);
            rowEventArgClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(EventArgs))); 

            rowEventArgClass.Comments.Add(CodeGenHelper.Comment("Row event argument class", true));

            //\\ private <RowConcreteClassName> eventRow; 
            rowEventArgClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.Type(rowConcreteClassName), "eventRow"));
            //\\ private DataRowAction eventAction; 
            rowEventArgClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRowAction)), "eventAction")); 

            // add constructor 
            rowEventArgClass.Members.Add(EventArgConstructor(rowConcreteClassName));

            //\\ public <rowConcreteClassName> COMPUTERRow {
            //\\     get { return this.eventRow; } 
            //\\ }
            CodeMemberProperty rowProp = CodeGenHelper.PropertyDecl( 
                CodeGenHelper.Type(rowConcreteClassName), 
                "Row",
                MemberAttributes.Public | MemberAttributes.Final 
            );
            rowProp.GetStatements.Add(CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), "eventRow")));

            rowEventArgClass.Members.Add(rowProp); 

            //\\ public DataRowAction Action { 
            //\\     get { return this.eventAction; } 
            //\\ }
            rowProp = CodeGenHelper.PropertyDecl( 
                CodeGenHelper.GlobalType(typeof(System.Data.DataRowAction)),
                "Action",
                MemberAttributes.Public | MemberAttributes.Final
            ); 
            rowProp.GetStatements.Add(CodeGenHelper.Return(CodeGenHelper.Field(CodeGenHelper.This(), "eventAction")));
            rowEventArgClass.Members.Add(rowProp); 
 
            return rowEventArgClass;
        } 

        private CodeTypeDelegate GenerateTypedRowEventHandler(DesignTable table) {
            if(table == null) {
                throw new InternalException("DesignTable should not be null."); 
            }
                string rowClassName = table.GeneratorRowClassName; 
 
            //\\ public delegate void <RowClassName>ChangeEventHandler(object sender, <RowClassName>ChangeEvent e);
                CodeTypeDelegate delegateClass = new CodeTypeDelegate(table.GeneratorRowEvHandlerName); 
            delegateClass.TypeAttributes |= TypeAttributes.Public;
            delegateClass.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(object)), "sender"));
            delegateClass.Parameters.Add(
                CodeGenHelper.ParameterDecl( 
                    CodeGenHelper.Type(table.GeneratorRowEvArgName),
                    "e" 
                ) 
            );
 
            return delegateClass;
        }

 
        private CodeTypeDeclaration GenerateRow(DesignTable table) {
            if(table == null) { 
                throw new InternalException("DesignTable should not be null."); 
            }
            string rowClassName                 = table.GeneratorRowClassName; 
            string tableClassName               = table.GeneratorTableClassName;
            string tableFieldName               = table.GeneratorTableVarName;
            TypedColumnHandler columnHandler    = codeGenerator.TableHandler.GetColumnHandler(table.Name);
 
            // create CodeTypeDeclaration, set class name and base type
            CodeTypeDeclaration rowClass = CodeGenHelper.Class(rowClassName, true, TypeAttributes.Public); 
            rowClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(System.Data.DataRow))); 

            rowClass.Comments.Add(CodeGenHelper.Comment("Represents strongly named DataRow class.", true)); 

            //\\ <TableClassName> <TableFieldName>;
            rowClass.Members.Add(CodeGenHelper.FieldDecl(CodeGenHelper.Type(tableClassName), tableFieldName));
 
            // add constructor
            rowClass.Members.Add(RowConstructor(tableClassName, tableFieldName)); 
 
            // add Column Properties
            columnHandler.AddRowColumnProperties(rowClass); 

            // add GetChildRows/GetParentRows methods
            columnHandler.AddRowGetRelatedRowsMethods(rowClass);
 

            return rowClass; 
        } 

        private CodeConstructor RowConstructor(string tableClassName, string tableFieldName) { 
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Assembly | MemberAttributes.Final);
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRowBuilder)), "rb"));
            constructor.BaseConstructorArgs.Add(CodeGenHelper.Argument("rb"));
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), tableFieldName), 
                    CodeGenHelper.Cast( 
                        CodeGenHelper.Type(tableClassName),
                        CodeGenHelper.Property(CodeGenHelper.This(), "Table") 
                    )
                )
            );
 
            return constructor;
        } 
 
        private CodeConstructor EventArgConstructor(string rowConcreteClassName) {
            //\\ public <rowEventArgClassName>ChangeEvent(rowEventArgClassName row, DataRowAction action) { 
            //\\     this.eventRow    = row;
            //\\     this.eventAction = action;
            //\\ }
            CodeConstructor constructor = CodeGenHelper.Constructor(MemberAttributes.Public | MemberAttributes.Final); 
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.Type(rowConcreteClassName), "row"   ));
            constructor.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRowAction)), "action")); 
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), "eventRow"), 
                    CodeGenHelper.Argument("row")
                )
            );
            constructor.Statements.Add( 
                CodeGenHelper.Assign(
                    CodeGenHelper.Field(CodeGenHelper.This(), "eventAction"), 
                    CodeGenHelper.Argument("action") 
                )
            ); 

            return constructor;
        }
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
