//------------------------------------------------------------------------------ 
// <copyright company='Microsoft Corporation'>
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
    using System.Collections.Generic;
    using System.Reflection; 
    using System.Globalization;

    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices; 

    /// <summary> 
    /// This class is used to generate the TableAdapterManager in the Hierarchical Update feature 
    /// TypedDataSourceCodeGenerator will instanciate this class to generate TableAdapterManager related code.
    /// </summary> 
    internal sealed class TableAdapterManagerGenerator {

        private TypedDataSourceCodeGenerator dataSourceGenerator = null;
        private const string adapterDesigner = "Microsoft.VSDesigner.DataSource.Design.TableAdapterManagerDesigner"; 
        private const string helpKeyword = "vs.data.TableAdapterManager";
 
        internal TableAdapterManagerGenerator(TypedDataSourceCodeGenerator codeGenerator) { 
            this.dataSourceGenerator = codeGenerator;
        } 

        internal CodeTypeDeclaration GenerateAdapterManager(DesignDataSource dataSource, CodeTypeDeclaration dataSourceClass) {
            // Create CodeTypeDeclaration
            // Type is internal if any TableAdapter is internal 
            //
            TypeAttributes typeAttributes = TypeAttributes.Public; 
            foreach (DesignTable table in dataSource.DesignTables) { 
                if ((table.DataAccessorModifier & TypeAttributes.Public) != TypeAttributes.Public) {
                    typeAttributes = table.DataAccessorModifier; 
                }
            }

            CodeTypeDeclaration dataComponentClass = CodeGenHelper.Class(TableAdapterManagerNameHandler.TableAdapterManagerClassName, true, typeAttributes); 
            dataComponentClass.Comments.Add(CodeGenHelper.Comment("TableAdapterManager is used to coordinate TableAdapters in the dataset to enable Hierarchical Update scenarios", true));
            dataComponentClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(ComponentModel.Component))); 
 
            // Set Attributes
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerCategoryAttribute", CodeGenHelper.Str("code"))); 
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.ToolboxItem", CodeGenHelper.Primitive(true)));
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerAttribute", CodeGenHelper.Str(adapterDesigner + ", " + AssemblyRef.MicrosoftVSDesigner)));
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.Design.HelpKeywordAttribute).FullName, CodeGenHelper.Str(helpKeyword)));
 
            // Create and Init the TableAdapterManager Method Generator
            TableAdapterManagerMethodGenerator dcMethodGenerator = new TableAdapterManagerMethodGenerator(this.dataSourceGenerator, dataSource, dataSourceClass); 
 
            // Generate methods
            dcMethodGenerator.AddEverything(dataComponentClass); 

            // Make sure that what we added so far doesn't contain any code injection (for the queries we're going to add right
            // after this all user input is validated in DataComponentNameHandler, so we're safe there).
            try { 
                System.CodeDom.Compiler.CodeGenerator.ValidateIdentifiers(dataComponentClass);
            } 
            catch (Exception er) { 
                Debug.Fail(er.ToString());
            } 

            return dataComponentClass;
        }
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

    using System.Diagnostics; 
    using System;
    using System.IO;
    using System.Data;
    using System.CodeDom; 
    using System.Text;
    using System.Xml; 
    using System.Collections; 
    using System.Collections.Generic;
    using System.Reflection; 
    using System.Globalization;

    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices; 

    /// <summary> 
    /// This class is used to generate the TableAdapterManager in the Hierarchical Update feature 
    /// TypedDataSourceCodeGenerator will instanciate this class to generate TableAdapterManager related code.
    /// </summary> 
    internal sealed class TableAdapterManagerGenerator {

        private TypedDataSourceCodeGenerator dataSourceGenerator = null;
        private const string adapterDesigner = "Microsoft.VSDesigner.DataSource.Design.TableAdapterManagerDesigner"; 
        private const string helpKeyword = "vs.data.TableAdapterManager";
 
        internal TableAdapterManagerGenerator(TypedDataSourceCodeGenerator codeGenerator) { 
            this.dataSourceGenerator = codeGenerator;
        } 

        internal CodeTypeDeclaration GenerateAdapterManager(DesignDataSource dataSource, CodeTypeDeclaration dataSourceClass) {
            // Create CodeTypeDeclaration
            // Type is internal if any TableAdapter is internal 
            //
            TypeAttributes typeAttributes = TypeAttributes.Public; 
            foreach (DesignTable table in dataSource.DesignTables) { 
                if ((table.DataAccessorModifier & TypeAttributes.Public) != TypeAttributes.Public) {
                    typeAttributes = table.DataAccessorModifier; 
                }
            }

            CodeTypeDeclaration dataComponentClass = CodeGenHelper.Class(TableAdapterManagerNameHandler.TableAdapterManagerClassName, true, typeAttributes); 
            dataComponentClass.Comments.Add(CodeGenHelper.Comment("TableAdapterManager is used to coordinate TableAdapters in the dataset to enable Hierarchical Update scenarios", true));
            dataComponentClass.BaseTypes.Add(CodeGenHelper.GlobalType(typeof(ComponentModel.Component))); 
 
            // Set Attributes
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerCategoryAttribute", CodeGenHelper.Str("code"))); 
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.ToolboxItem", CodeGenHelper.Primitive(true)));
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl("System.ComponentModel.DesignerAttribute", CodeGenHelper.Str(adapterDesigner + ", " + AssemblyRef.MicrosoftVSDesigner)));
            dataComponentClass.CustomAttributes.Add(CodeGenHelper.AttributeDecl(typeof(System.ComponentModel.Design.HelpKeywordAttribute).FullName, CodeGenHelper.Str(helpKeyword)));
 
            // Create and Init the TableAdapterManager Method Generator
            TableAdapterManagerMethodGenerator dcMethodGenerator = new TableAdapterManagerMethodGenerator(this.dataSourceGenerator, dataSource, dataSourceClass); 
 
            // Generate methods
            dcMethodGenerator.AddEverything(dataComponentClass); 

            // Make sure that what we added so far doesn't contain any code injection (for the queries we're going to add right
            // after this all user input is validated in DataComponentNameHandler, so we're safe there).
            try { 
                System.CodeDom.Compiler.CodeGenerator.ValidateIdentifiers(dataComponentClass);
            } 
            catch (Exception er) { 
                Debug.Fail(er.ToString());
            } 

            return dataComponentClass;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
