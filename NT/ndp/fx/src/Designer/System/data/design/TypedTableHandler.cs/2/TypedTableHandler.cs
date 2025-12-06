 
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
 
    internal sealed class TypedTableHandler { 
        private TypedDataSourceCodeGenerator codeGenerator = null;
        private TypedTableGenerator tableGenerator = null; 
        private DesignTableCollection tables = null;
        private Hashtable columnHandlers = null;

        internal TypedTableHandler(TypedDataSourceCodeGenerator codeGenerator, DesignTableCollection tables) { 
            this.codeGenerator = codeGenerator;
            this.tables = tables; 
            tableGenerator = new TypedTableGenerator(codeGenerator); 

            SetColumnHandlers(); 
        }

        internal DesignTableCollection Tables {
            get { 
                return tables;
            } 
        } 

        internal TypedColumnHandler GetColumnHandler(string tableName) { 
            if( tableName == null ) {
                return null;
            }
 
            return (TypedColumnHandler) columnHandlers[tableName];
        } 
 
        internal void AddPrivateVars(CodeTypeDeclaration dataSourceClass) {
            if( tables == null ) { 
                return;
            }

            foreach(DesignTable table in tables) { 
                string tableClassName = table.GeneratorTableClassName;
                string tableVariableName = table.GeneratorTableVarName; 
 
                //\\ private <TableClassName> <TableVariableName>;
                dataSourceClass.Members.Add( CodeGenHelper.FieldDecl(CodeGenHelper.Type(tableClassName), tableVariableName) ); 
            }
        }

        internal void AddTableProperties(CodeTypeDeclaration dataSourceClass) { 
            if( tables == null ) {
                return; 
            } 

            foreach(DesignTable table in tables) { 
                // get class/property/variable names
                string tableClassName = table.GeneratorTableClassName;
                string tablePropertyName = table.GeneratorTablePropName;
                string tableVariableName = table.GeneratorTableVarName; 

                // generate 1 public property for each typed table 
 
                //\\ [System.ComponentModel.Browsable(false)]
                //\\ [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)] 
                //\\ public <TableClassName> <TablePropertyName> {
                //\\    get {
                //\\        return this.<TableVariableName>;
                //\\    } 
                //\\ }
                CodeMemberProperty tableProperty = CodeGenHelper.PropertyDecl( 
                    CodeGenHelper.Type(tableClassName), 
                    tablePropertyName,
                    MemberAttributes.Public | MemberAttributes.Final 
                );

                tableProperty.CustomAttributes.Add(
                    CodeGenHelper.AttributeDecl("System.ComponentModel.Browsable", 
                        CodeGenHelper.Primitive(false)));
                tableProperty.CustomAttributes.Add( 
                    CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerSerializationVisibility", 
                        CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.ComponentModel.DesignerSerializationVisibility)), "Content"))
                ); 


                tableProperty.GetStatements.Add(
                    CodeGenHelper.Return( 
                        CodeGenHelper.Field(
                            CodeGenHelper.This(), 
                            tableVariableName))); 
                dataSourceClass.Members.Add(tableProperty);
            } 
        }

        internal void AddTableClasses(CodeTypeDeclaration dataSourceClass) {
            tableGenerator.GenerateTables(dataSourceClass); 
        }
 
 
        private void SetColumnHandlers() {
            this.columnHandlers = new Hashtable(); 

            foreach(DesignTable table in tables) {
                columnHandlers.Add(table.Name, new TypedColumnHandler(table, codeGenerator));
            } 
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
 
    internal sealed class TypedTableHandler { 
        private TypedDataSourceCodeGenerator codeGenerator = null;
        private TypedTableGenerator tableGenerator = null; 
        private DesignTableCollection tables = null;
        private Hashtable columnHandlers = null;

        internal TypedTableHandler(TypedDataSourceCodeGenerator codeGenerator, DesignTableCollection tables) { 
            this.codeGenerator = codeGenerator;
            this.tables = tables; 
            tableGenerator = new TypedTableGenerator(codeGenerator); 

            SetColumnHandlers(); 
        }

        internal DesignTableCollection Tables {
            get { 
                return tables;
            } 
        } 

        internal TypedColumnHandler GetColumnHandler(string tableName) { 
            if( tableName == null ) {
                return null;
            }
 
            return (TypedColumnHandler) columnHandlers[tableName];
        } 
 
        internal void AddPrivateVars(CodeTypeDeclaration dataSourceClass) {
            if( tables == null ) { 
                return;
            }

            foreach(DesignTable table in tables) { 
                string tableClassName = table.GeneratorTableClassName;
                string tableVariableName = table.GeneratorTableVarName; 
 
                //\\ private <TableClassName> <TableVariableName>;
                dataSourceClass.Members.Add( CodeGenHelper.FieldDecl(CodeGenHelper.Type(tableClassName), tableVariableName) ); 
            }
        }

        internal void AddTableProperties(CodeTypeDeclaration dataSourceClass) { 
            if( tables == null ) {
                return; 
            } 

            foreach(DesignTable table in tables) { 
                // get class/property/variable names
                string tableClassName = table.GeneratorTableClassName;
                string tablePropertyName = table.GeneratorTablePropName;
                string tableVariableName = table.GeneratorTableVarName; 

                // generate 1 public property for each typed table 
 
                //\\ [System.ComponentModel.Browsable(false)]
                //\\ [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)] 
                //\\ public <TableClassName> <TablePropertyName> {
                //\\    get {
                //\\        return this.<TableVariableName>;
                //\\    } 
                //\\ }
                CodeMemberProperty tableProperty = CodeGenHelper.PropertyDecl( 
                    CodeGenHelper.Type(tableClassName), 
                    tablePropertyName,
                    MemberAttributes.Public | MemberAttributes.Final 
                );

                tableProperty.CustomAttributes.Add(
                    CodeGenHelper.AttributeDecl("System.ComponentModel.Browsable", 
                        CodeGenHelper.Primitive(false)));
                tableProperty.CustomAttributes.Add( 
                    CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerSerializationVisibility", 
                        CodeGenHelper.Field(CodeGenHelper.GlobalTypeExpr(typeof(System.ComponentModel.DesignerSerializationVisibility)), "Content"))
                ); 


                tableProperty.GetStatements.Add(
                    CodeGenHelper.Return( 
                        CodeGenHelper.Field(
                            CodeGenHelper.This(), 
                            tableVariableName))); 
                dataSourceClass.Members.Add(tableProperty);
            } 
        }

        internal void AddTableClasses(CodeTypeDeclaration dataSourceClass) {
            tableGenerator.GenerateTables(dataSourceClass); 
        }
 
 
        private void SetColumnHandlers() {
            this.columnHandlers = new Hashtable(); 

            foreach(DesignTable table in tables) {
                columnHandlers.Add(table.Name, new TypedColumnHandler(table, codeGenerator));
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
