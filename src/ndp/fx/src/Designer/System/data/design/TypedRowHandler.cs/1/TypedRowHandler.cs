 
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
    using System.Reflection; 
 
    internal sealed class TypedRowHandler {
        private TypedDataSourceCodeGenerator codeGenerator = null; 
        private DesignTableCollection tables = null;
        private TypedRowGenerator rowGenerator = null;

        internal TypedRowHandler(TypedDataSourceCodeGenerator codeGenerator, DesignTableCollection tables) { 
            this.codeGenerator = codeGenerator;
            this.tables = tables; 
            this.rowGenerator = new TypedRowGenerator(codeGenerator); 
        }
 
        internal TypedRowGenerator RowGenerator {
            get {
                return rowGenerator;
            } 
        }
 
 
        internal void AddTypedRowEvents(CodeTypeDeclaration dataTableClass, string tableName) {
            DesignTable designTable = codeGenerator.TableHandler.Tables[tableName]; 
            string rowClassName = designTable.GeneratorRowClassName;
            string rowEventHandlerName = designTable.GeneratorRowEvHandlerName;
            dataTableClass.Members.Add(
                CodeGenHelper.EventDecl( 
                    rowEventHandlerName,
                    designTable.GeneratorRowChangingName 
                ) 
            );
            dataTableClass.Members.Add( 
                CodeGenHelper.EventDecl(
                    rowEventHandlerName,
                    designTable.GeneratorRowChangedName
                ) 
            );
            dataTableClass.Members.Add( 
                CodeGenHelper.EventDecl( 
                    rowEventHandlerName,
                    designTable.GeneratorRowDeletingName 
                )
            );
            dataTableClass.Members.Add(
                CodeGenHelper.EventDecl( 
                    rowEventHandlerName,
                    designTable.GeneratorRowDeletedName 
                ) 
            );
        } 

        internal void AddTypedRows(CodeTypeDeclaration dataSourceClass) {
            rowGenerator.GenerateRows(dataSourceClass);
        } 

        internal void AddTypedRowEventHandlers(CodeTypeDeclaration dataSourceClass) { 
            rowGenerator.GenerateTypedRowEventHandlers(dataSourceClass); 
        }
 
        internal void AddTypedRowEventArgs(CodeTypeDeclaration dataSourceClass) {
            rowGenerator.GenerateTypedRowEventArgs(dataSourceClass);
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
    using System.Reflection; 
 
    internal sealed class TypedRowHandler {
        private TypedDataSourceCodeGenerator codeGenerator = null; 
        private DesignTableCollection tables = null;
        private TypedRowGenerator rowGenerator = null;

        internal TypedRowHandler(TypedDataSourceCodeGenerator codeGenerator, DesignTableCollection tables) { 
            this.codeGenerator = codeGenerator;
            this.tables = tables; 
            this.rowGenerator = new TypedRowGenerator(codeGenerator); 
        }
 
        internal TypedRowGenerator RowGenerator {
            get {
                return rowGenerator;
            } 
        }
 
 
        internal void AddTypedRowEvents(CodeTypeDeclaration dataTableClass, string tableName) {
            DesignTable designTable = codeGenerator.TableHandler.Tables[tableName]; 
            string rowClassName = designTable.GeneratorRowClassName;
            string rowEventHandlerName = designTable.GeneratorRowEvHandlerName;
            dataTableClass.Members.Add(
                CodeGenHelper.EventDecl( 
                    rowEventHandlerName,
                    designTable.GeneratorRowChangingName 
                ) 
            );
            dataTableClass.Members.Add( 
                CodeGenHelper.EventDecl(
                    rowEventHandlerName,
                    designTable.GeneratorRowChangedName
                ) 
            );
            dataTableClass.Members.Add( 
                CodeGenHelper.EventDecl( 
                    rowEventHandlerName,
                    designTable.GeneratorRowDeletingName 
                )
            );
            dataTableClass.Members.Add(
                CodeGenHelper.EventDecl( 
                    rowEventHandlerName,
                    designTable.GeneratorRowDeletedName 
                ) 
            );
        } 

        internal void AddTypedRows(CodeTypeDeclaration dataSourceClass) {
            rowGenerator.GenerateRows(dataSourceClass);
        } 

        internal void AddTypedRowEventHandlers(CodeTypeDeclaration dataSourceClass) { 
            rowGenerator.GenerateTypedRowEventHandlers(dataSourceClass); 
        }
 
        internal void AddTypedRowEventArgs(CodeTypeDeclaration dataSourceClass) {
            rowGenerator.GenerateTypedRowEventArgs(dataSourceClass);
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
