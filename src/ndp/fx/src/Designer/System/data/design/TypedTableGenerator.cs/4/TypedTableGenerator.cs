//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.Data;
    using System.Data.Common;
    using System.CodeDom;
    using System.Reflection; 
    using System.Xml.Serialization;
 
    using System.CodeDom.Compiler; 

    using GenerateOption = TypedDataSetGenerator.GenerateOption; 

    internal sealed class TypedTableGenerator {
        private TypedDataSourceCodeGenerator codeGenerator = null;
        private static string LINQOverTDSTableBaseClass = "System.Data.TypedTableBase"; 

 
        internal TypedTableGenerator(TypedDataSourceCodeGenerator codeGenerator) { 
            this.codeGenerator = codeGenerator;
        } 


        internal void GenerateTables(CodeTypeDeclaration dataSourceClass) {
            if(dataSourceClass == null) { 
                throw new InternalException("DataSource CodeTypeDeclaration should not be null.");
            } 
            foreach(DesignTable table in codeGenerator.TableHandler.Tables) { 
                dataSourceClass.Members.Add(GenerateTable(table, dataSourceClass));
            } 
        }

        private CodeTypeDeclaration GenerateTable(DesignTable designTable, CodeTypeDeclaration dataSourceClass) {
            // get class name for table 
                string tableClassName = designTable.GeneratorTableClassName;
            // get table-specific column handler 
            TypedColumnHandler columnHandler = codeGenerator.TableHandler.GetColumnHandler(designTable.Name); 

            // create CodeTypeDeclaration 
            CodeTypeDeclaration dataTableClass = CodeGenHelper.Class(tableClassName, true, TypeAttributes.Public);

            // set BaseTypes
            if ((this.codeGenerator.GenerateOptions & GenerateOption.LinqOverTypedDatasets) == GenerateOption.LinqOverTypedDatasets) { 
                dataTableClass.BaseTypes.Add(
                    CodeGenHelper.GlobalGenericType( 
                        LINQOverTDSTableBaseClass, 
                        CodeGenHelper.Type(designTable.GeneratorRowClassName)
                    ) 
                );
            }
            else {
                dataTableClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(System.Data.DataTable))); 
                dataTableClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(System.Collections.IEnumerable)));
            } 
 
            // set Attributes
            dataTableClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.Serializable")); 
            dataTableClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(XmlSchemaProviderAttribute).FullName, CodeGenHelper.Primitive("GetTypedTableSchema")));

            dataTableClass.Comments.Add(CodeGenHelper.Comment("Represents the strongly named DataTable class.", true));
 

 
            // add 1 private variable of type DataColumn for each column in the table 
            columnHandler.AddPrivateVariables(dataTableClass);
 
            // add 1 property for each column in the table
            columnHandler.AddTableColumnProperties(dataTableClass);

            // add count property 
            dataTableClass.Members.Add( CountProperty() );
            if (this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareIndexerProperties)) { 
                // add index property 
                dataTableClass.Members.Add(IndexProperty(designTable));
            } 

            if (this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareEvents) && this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareDelegates)) {
                // add typed row changing/changed/deleting/deleted events
                codeGenerator.RowHandler.AddTypedRowEvents(dataTableClass, designTable.Name); 
            }
 
            // create table-method generator 
            TableMethodGenerator tableMethodGenerator = new TableMethodGenerator(codeGenerator, designTable);
            // generate typed table methods 
            tableMethodGenerator.AddMethods(dataTableClass);

            return dataTableClass;
        } 

        private CodeMemberProperty CountProperty() { 
            //\\ public int Count { 
            //\\     get { return this.Rows.Count; }
            //\\ } 
            CodeMemberProperty countProp = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(typeof(System.Int32)), "Count", MemberAttributes.Public | MemberAttributes.Final);
            countProp.CustomAttributes.Add(
                CodeGenHelper.AttributeDecl("System.ComponentModel.Browsable", CodeGenHelper.Primitive(false))
            ); 
            countProp.GetStatements.Add(
                CodeGenHelper.Return( 
                    CodeGenHelper.Property( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Rows"),
                        "Count" 
                    )
                )
            );
 
            return countProp;
        } 
 
        private CodeMemberProperty IndexProperty(DesignTable designTable) {
            string rowConcreteClassName = designTable.GeneratorRowClassName; 
            //\\ public <RowClassName> this[int index] {
            //\\     return (<RowClassName>) this.Rows[index];
            //\\ }
            CodeMemberProperty thisIndex = CodeGenHelper.PropertyDecl( 
                    CodeGenHelper.Type(rowConcreteClassName),
                    "Item", 
                    MemberAttributes.Public | MemberAttributes.Final 
            );
            thisIndex.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Int32)), "index")); 
            thisIndex.GetStatements.Add(
                CodeGenHelper.Return(
                    CodeGenHelper.Cast(
                        CodeGenHelper.Type(rowConcreteClassName), 
                        CodeGenHelper.Indexer(
                            CodeGenHelper.Property(CodeGenHelper.This(), "Rows"), 
                            CodeGenHelper.Argument("index") 
                        )
                    ) 
                )
            );

            return thisIndex; 
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
    using System.Data.Common;
    using System.CodeDom;
    using System.Reflection; 
    using System.Xml.Serialization;
 
    using System.CodeDom.Compiler; 

    using GenerateOption = TypedDataSetGenerator.GenerateOption; 

    internal sealed class TypedTableGenerator {
        private TypedDataSourceCodeGenerator codeGenerator = null;
        private static string LINQOverTDSTableBaseClass = "System.Data.TypedTableBase"; 

 
        internal TypedTableGenerator(TypedDataSourceCodeGenerator codeGenerator) { 
            this.codeGenerator = codeGenerator;
        } 


        internal void GenerateTables(CodeTypeDeclaration dataSourceClass) {
            if(dataSourceClass == null) { 
                throw new InternalException("DataSource CodeTypeDeclaration should not be null.");
            } 
            foreach(DesignTable table in codeGenerator.TableHandler.Tables) { 
                dataSourceClass.Members.Add(GenerateTable(table, dataSourceClass));
            } 
        }

        private CodeTypeDeclaration GenerateTable(DesignTable designTable, CodeTypeDeclaration dataSourceClass) {
            // get class name for table 
                string tableClassName = designTable.GeneratorTableClassName;
            // get table-specific column handler 
            TypedColumnHandler columnHandler = codeGenerator.TableHandler.GetColumnHandler(designTable.Name); 

            // create CodeTypeDeclaration 
            CodeTypeDeclaration dataTableClass = CodeGenHelper.Class(tableClassName, true, TypeAttributes.Public);

            // set BaseTypes
            if ((this.codeGenerator.GenerateOptions & GenerateOption.LinqOverTypedDatasets) == GenerateOption.LinqOverTypedDatasets) { 
                dataTableClass.BaseTypes.Add(
                    CodeGenHelper.GlobalGenericType( 
                        LINQOverTDSTableBaseClass, 
                        CodeGenHelper.Type(designTable.GeneratorRowClassName)
                    ) 
                );
            }
            else {
                dataTableClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(System.Data.DataTable))); 
                dataTableClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(System.Collections.IEnumerable)));
            } 
 
            // set Attributes
            dataTableClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.Serializable")); 
            dataTableClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(XmlSchemaProviderAttribute).FullName, CodeGenHelper.Primitive("GetTypedTableSchema")));

            dataTableClass.Comments.Add(CodeGenHelper.Comment("Represents the strongly named DataTable class.", true));
 

 
            // add 1 private variable of type DataColumn for each column in the table 
            columnHandler.AddPrivateVariables(dataTableClass);
 
            // add 1 property for each column in the table
            columnHandler.AddTableColumnProperties(dataTableClass);

            // add count property 
            dataTableClass.Members.Add( CountProperty() );
            if (this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareIndexerProperties)) { 
                // add index property 
                dataTableClass.Members.Add(IndexProperty(designTable));
            } 

            if (this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareEvents) && this.codeGenerator.CodeProvider.Supports(GeneratorSupport.DeclareDelegates)) {
                // add typed row changing/changed/deleting/deleted events
                codeGenerator.RowHandler.AddTypedRowEvents(dataTableClass, designTable.Name); 
            }
 
            // create table-method generator 
            TableMethodGenerator tableMethodGenerator = new TableMethodGenerator(codeGenerator, designTable);
            // generate typed table methods 
            tableMethodGenerator.AddMethods(dataTableClass);

            return dataTableClass;
        } 

        private CodeMemberProperty CountProperty() { 
            //\\ public int Count { 
            //\\     get { return this.Rows.Count; }
            //\\ } 
            CodeMemberProperty countProp = CodeGenHelper.PropertyDecl(CodeGenHelper.GlobalType(typeof(System.Int32)), "Count", MemberAttributes.Public | MemberAttributes.Final);
            countProp.CustomAttributes.Add(
                CodeGenHelper.AttributeDecl("System.ComponentModel.Browsable", CodeGenHelper.Primitive(false))
            ); 
            countProp.GetStatements.Add(
                CodeGenHelper.Return( 
                    CodeGenHelper.Property( 
                        CodeGenHelper.Property(CodeGenHelper.This(), "Rows"),
                        "Count" 
                    )
                )
            );
 
            return countProp;
        } 
 
        private CodeMemberProperty IndexProperty(DesignTable designTable) {
            string rowConcreteClassName = designTable.GeneratorRowClassName; 
            //\\ public <RowClassName> this[int index] {
            //\\     return (<RowClassName>) this.Rows[index];
            //\\ }
            CodeMemberProperty thisIndex = CodeGenHelper.PropertyDecl( 
                    CodeGenHelper.Type(rowConcreteClassName),
                    "Item", 
                    MemberAttributes.Public | MemberAttributes.Final 
            );
            thisIndex.Parameters.Add(CodeGenHelper.ParameterDecl(CodeGenHelper.GlobalType(typeof(System.Int32)), "index")); 
            thisIndex.GetStatements.Add(
                CodeGenHelper.Return(
                    CodeGenHelper.Cast(
                        CodeGenHelper.Type(rowConcreteClassName), 
                        CodeGenHelper.Indexer(
                            CodeGenHelper.Property(CodeGenHelper.This(), "Rows"), 
                            CodeGenHelper.Argument("index") 
                        )
                    ) 
                )
            );

            return thisIndex; 
        }
 
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
