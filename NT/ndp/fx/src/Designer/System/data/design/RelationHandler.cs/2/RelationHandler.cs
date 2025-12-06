 
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
 
    internal sealed class RelationHandler { 
        private TypedDataSourceCodeGenerator codeGenerator = null;
        private DesignRelationCollection relations = null; 

        internal RelationHandler(TypedDataSourceCodeGenerator codeGenerator, DesignRelationCollection relations) {
            this.codeGenerator = codeGenerator;
            this.relations = relations; 
        }
 
        internal DesignRelationCollection Relations { 
            get {
                return relations; 
            }
        }

        internal void AddPrivateVars(CodeTypeDeclaration dataSourceClass) { 
            if(dataSourceClass == null) {
                throw new InternalException("DataSource CodeTypeDeclaration should not be null."); 
            } 

            if( relations == null ) { 
                return;
            }

            foreach(DesignRelation relation in relations) { 
 				if(relation.DataRelation != null) {
					//\\ private DataRelation <relationVariableName> 
                    string relationVariableName = relation.GeneratorRelationVarName; 
					dataSourceClass.Members.Add( CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRelation)), relationVariableName) );
				} 
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
 
    internal sealed class RelationHandler { 
        private TypedDataSourceCodeGenerator codeGenerator = null;
        private DesignRelationCollection relations = null; 

        internal RelationHandler(TypedDataSourceCodeGenerator codeGenerator, DesignRelationCollection relations) {
            this.codeGenerator = codeGenerator;
            this.relations = relations; 
        }
 
        internal DesignRelationCollection Relations { 
            get {
                return relations; 
            }
        }

        internal void AddPrivateVars(CodeTypeDeclaration dataSourceClass) { 
            if(dataSourceClass == null) {
                throw new InternalException("DataSource CodeTypeDeclaration should not be null."); 
            } 

            if( relations == null ) { 
                return;
            }

            foreach(DesignRelation relation in relations) { 
 				if(relation.DataRelation != null) {
					//\\ private DataRelation <relationVariableName> 
                    string relationVariableName = relation.GeneratorRelationVarName; 
					dataSourceClass.Members.Add( CodeGenHelper.FieldDecl(CodeGenHelper.GlobalType(typeof(System.Data.DataRelation)), relationVariableName) );
				} 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
