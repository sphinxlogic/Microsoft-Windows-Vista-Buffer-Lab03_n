 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
 
    using System.Diagnostics;
    using System;
    using System.IO;
    using System.Data; 
    using System.CodeDom;
    using System.Text; 
    using System.Xml; 
    using System.Collections;
    using System.Reflection; 
    using System.Globalization;

    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices; 

    internal sealed class DataComponentGenerator { 
 
        private TypedDataSourceCodeGenerator     dataSourceGenerator = null;
        private static string adapterDesigner = "Microsoft.VSDesigner.DataSource.Design.TableAdapterDesigner"; 

        internal DataComponentGenerator(TypedDataSourceCodeGenerator codeGenerator) {
            this.dataSourceGenerator = codeGenerator;
        } 

//        internal CodeTypeDeclaration GenerateDataComponentInterface(DesignTable designTable, bool isFunctionsComponent) { 
//            // Get DataComponent class Name 
//            string dataComponentInterfaceName = designTable.GeneratorDataComponentInterfaceName;
// 
//            // Create CodeTypeDeclaration
//            CodeTypeDeclaration dataComponentInterface = new CodeTypeDeclaration(dataComponentInterfaceName);
//            dataComponentInterface.IsInterface = true;
//            dataComponentInterface.Attributes = MemberAttributes.Public; 
//
//            // create handler for queries 
//            QueryHandler queryHandler = new QueryHandler(this.dataSourceGenerator, designTable); 
//            queryHandler.DeclarationsOnly = true;
// 
//            // generate query declarations on DataComponent interface
//            if(isFunctionsComponent) {
//                queryHandler.AddFunctionsToDataComponent(dataComponentInterface, true /*skipMain*/);
//            } 
//            else {
//                queryHandler.AddQueriesToDataComponent(dataComponentInterface); 
//            } 
//
//            return dataComponentInterface; 
//        }


        internal CodeTypeDeclaration GenerateDataComponent(DesignTable designTable, bool isFunctionsComponent, bool generateHierarchicalUpdate){ 
            // Get DataComponent class Name
            string dataComponentClassName = designTable.GeneratorDataComponentClassName; 
 
            // Create CodeTypeDeclaration
            CodeTypeDeclaration dataComponentClass = CodeGenHelper.Class(dataComponentClassName, true, designTable.DataAccessorModifier); 

            // Set BaseType
            dataComponentClass.BaseTypes.Add(CodeGenHelper.GlobalType(designTable.BaseClass));
 
            // Set Attributes
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerCategoryAttribute", CodeGenHelper.Str("code"))); 
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.ToolboxItem", CodeGenHelper.Primitive(true))); 
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DataObjectAttribute", CodeGenHelper.Primitive(true)));
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerAttribute", CodeGenHelper.Str(adapterDesigner + ", " + AssemblyRef.MicrosoftVSDesigner))); 
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.Design.HelpKeywordAttribute).FullName, CodeGenHelper.Str("vs.data.TableAdapter")));

            if (designTable.WebServiceAttribute) {
                CodeAttributeDeclaration wsAttribute = new CodeAttributeDeclaration("System.Web.Services.WebService"); 
                wsAttribute.Arguments.Add(new CodeAttributeArgument("Namespace", CodeGenHelper.Str(designTable.WebServiceNamespace)));
                wsAttribute.Arguments.Add(new CodeAttributeArgument("Description", CodeGenHelper.Str(designTable.WebServiceDescription))); 
                dataComponentClass.CustomAttributes.Add(wsAttribute); 
            }
 
            dataComponentClass.Comments.Add(CodeGenHelper.Comment(@"Represents the connection and commands used to retrieve and save data.",true));

            // Create and Init the Typed DataSet Method Generator
            DataComponentMethodGenerator dcMethodGenerator = new DataComponentMethodGenerator(this.dataSourceGenerator, designTable, generateHierarchicalUpdate); 

            // Generate methods 
            dcMethodGenerator.AddMethods(dataComponentClass, isFunctionsComponent); 

            // Make sure that what we added so far doesn't contain any code injection (for the queries we're going to add right 
            // after this all user input is validated in DataComponentNameHandler, so we're safe there).
            System.CodeDom.Compiler.CodeGenerator.ValidateIdentifiers(dataComponentClass);

            // create handler for queries 
            QueryHandler queryHandler = new QueryHandler(this.dataSourceGenerator, designTable);
 
            // generate queries on DataComponent 
            if(isFunctionsComponent) {
                queryHandler.AddFunctionsToDataComponent(dataComponentClass, true /*skipMain*/); 
            }
            else {
                queryHandler.AddQueriesToDataComponent(dataComponentClass);
            } 

            return dataComponentClass; 
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
 
    using System.Diagnostics;
    using System;
    using System.IO;
    using System.Data; 
    using System.CodeDom;
    using System.Text; 
    using System.Xml; 
    using System.Collections;
    using System.Reflection; 
    using System.Globalization;

    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices; 

    internal sealed class DataComponentGenerator { 
 
        private TypedDataSourceCodeGenerator     dataSourceGenerator = null;
        private static string adapterDesigner = "Microsoft.VSDesigner.DataSource.Design.TableAdapterDesigner"; 

        internal DataComponentGenerator(TypedDataSourceCodeGenerator codeGenerator) {
            this.dataSourceGenerator = codeGenerator;
        } 

//        internal CodeTypeDeclaration GenerateDataComponentInterface(DesignTable designTable, bool isFunctionsComponent) { 
//            // Get DataComponent class Name 
//            string dataComponentInterfaceName = designTable.GeneratorDataComponentInterfaceName;
// 
//            // Create CodeTypeDeclaration
//            CodeTypeDeclaration dataComponentInterface = new CodeTypeDeclaration(dataComponentInterfaceName);
//            dataComponentInterface.IsInterface = true;
//            dataComponentInterface.Attributes = MemberAttributes.Public; 
//
//            // create handler for queries 
//            QueryHandler queryHandler = new QueryHandler(this.dataSourceGenerator, designTable); 
//            queryHandler.DeclarationsOnly = true;
// 
//            // generate query declarations on DataComponent interface
//            if(isFunctionsComponent) {
//                queryHandler.AddFunctionsToDataComponent(dataComponentInterface, true /*skipMain*/);
//            } 
//            else {
//                queryHandler.AddQueriesToDataComponent(dataComponentInterface); 
//            } 
//
//            return dataComponentInterface; 
//        }


        internal CodeTypeDeclaration GenerateDataComponent(DesignTable designTable, bool isFunctionsComponent, bool generateHierarchicalUpdate){ 
            // Get DataComponent class Name
            string dataComponentClassName = designTable.GeneratorDataComponentClassName; 
 
            // Create CodeTypeDeclaration
            CodeTypeDeclaration dataComponentClass = CodeGenHelper.Class(dataComponentClassName, true, designTable.DataAccessorModifier); 

            // Set BaseType
            dataComponentClass.BaseTypes.Add(CodeGenHelper.GlobalType(designTable.BaseClass));
 
            // Set Attributes
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerCategoryAttribute", CodeGenHelper.Str("code"))); 
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.ToolboxItem", CodeGenHelper.Primitive(true))); 
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DataObjectAttribute", CodeGenHelper.Primitive(true)));
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerAttribute", CodeGenHelper.Str(adapterDesigner + ", " + AssemblyRef.MicrosoftVSDesigner))); 
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.Design.HelpKeywordAttribute).FullName, CodeGenHelper.Str("vs.data.TableAdapter")));

            if (designTable.WebServiceAttribute) {
                CodeAttributeDeclaration wsAttribute = new CodeAttributeDeclaration("System.Web.Services.WebService"); 
                wsAttribute.Arguments.Add(new CodeAttributeArgument("Namespace", CodeGenHelper.Str(designTable.WebServiceNamespace)));
                wsAttribute.Arguments.Add(new CodeAttributeArgument("Description", CodeGenHelper.Str(designTable.WebServiceDescription))); 
                dataComponentClass.CustomAttributes.Add(wsAttribute); 
            }
 
            dataComponentClass.Comments.Add(CodeGenHelper.Comment(@"Represents the connection and commands used to retrieve and save data.",true));

            // Create and Init the Typed DataSet Method Generator
            DataComponentMethodGenerator dcMethodGenerator = new DataComponentMethodGenerator(this.dataSourceGenerator, designTable, generateHierarchicalUpdate); 

            // Generate methods 
            dcMethodGenerator.AddMethods(dataComponentClass, isFunctionsComponent); 

            // Make sure that what we added so far doesn't contain any code injection (for the queries we're going to add right 
            // after this all user input is validated in DataComponentNameHandler, so we're safe there).
            System.CodeDom.Compiler.CodeGenerator.ValidateIdentifiers(dataComponentClass);

            // create handler for queries 
            QueryHandler queryHandler = new QueryHandler(this.dataSourceGenerator, designTable);
 
            // generate queries on DataComponent 
            if(isFunctionsComponent) {
                queryHandler.AddFunctionsToDataComponent(dataComponentClass, true /*skipMain*/); 
            }
            else {
                queryHandler.AddQueriesToDataComponent(dataComponentClass);
            } 

            return dataComponentClass; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
