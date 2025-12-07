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
    using System.CodeDom.Compiler;

    internal sealed class TableAdapterManagerNameHandler { 
        // Non-private class/method/property names used in TableAdapterManager
        internal const string TableAdapterManagerClassName = "TableAdapterManager"; 
        internal const string SelfRefComparerClass = "SelfReferenceComparer"; 
        internal const string UpdateAllMethod = "UpdateAll";
        internal const string SortSelfRefRowsMethod = "SortSelfReferenceRows"; 
        internal const string MatchTAConnectionMethod = "MatchTableAdapterConnection";
        internal const string UpdateAllRevertConnectionsVar = "revertConnections";
        internal const string ConnectionVar = "_connection";
        internal const string ConnectionProperty = "Connection"; 
        internal const string BackupDataSetBeforeUpdateVar = "_backupDataSetBeforeUpdate";
        internal const string BackupDataSetBeforeUpdateProperty = "BackupDataSetBeforeUpdate"; 
        internal const string TableAdapterInstanceCountProperty = "TableAdapterInstanceCount"; 
        internal const string UpdateOrderOptionProperty = "UpdateOrder";
        internal const string UpdateOrderOptionVar = "_updateOrder"; 
        internal const string UpdateOrderOptionEnum = "UpdateOrderOption";
        internal const string UpdateOrderOptionEnumIUD = "InsertUpdateDelete";
        internal const string UpdateOrderOptionEnumUID = "UpdateInsertDelete";
        internal const string UpdateUpdatedRowsMethod = "UpdateUpdatedRows"; 
        internal const string UpdateInsertedRowsMethod = "UpdateInsertedRows";
        internal const string UpdateDeletedRowsMethod =  "UpdateDeletedRows"; 
        internal const string GetRealUpdatedRowsMethod = "GetRealUpdatedRows"; 

        private MemberNameValidator tableAdapterManagerValidator = null; 
        private bool languageCaseInsensitive = false;
        private CodeDomProvider codePrivider = null;

        public TableAdapterManagerNameHandler(CodeDomProvider provider) { 
            this.codePrivider = provider;
            this.languageCaseInsensitive = (this.codePrivider.LanguageOptions & LanguageOptions.CaseInsensitive) == LanguageOptions.CaseInsensitive; 
        } 

        private MemberNameValidator TableAdapterManagerValidator { 
            get {
                if (tableAdapterManagerValidator == null) {
                    tableAdapterManagerValidator = new MemberNameValidator(
                        new string[]{ 
                             SelfRefComparerClass,
                             UpdateAllMethod, 
                             SortSelfRefRowsMethod, 
                             MatchTAConnectionMethod,
                             ConnectionVar, 
                             ConnectionProperty,
                             BackupDataSetBeforeUpdateVar,
                             BackupDataSetBeforeUpdateProperty,
                             TableAdapterInstanceCountProperty, 
                             UpdateOrderOptionProperty,
                             UpdateOrderOptionVar, 
                             UpdateOrderOptionEnum, 
                             UpdateUpdatedRowsMethod,
                             UpdateInsertedRowsMethod, 
                             UpdateDeletedRowsMethod,
                             GetRealUpdatedRowsMethod
                        },
                        this.codePrivider, this.languageCaseInsensitive); 
                }
                return this.tableAdapterManagerValidator; 
            } 
        }
 
        /// <summary>
        /// Get a valid member name not conflict with known reserved name like ConnectionManager
        /// </summary>
        /// <param name="memberName"></param> 
        /// <returns></returns>
        internal string GetNewMemberName(string memberName) { 
            return this.TableAdapterManagerValidator.GetNewMemberName(memberName); 
        }
 
        /// <summary>
        /// Get an valid TableAdapter property name
        /// e.g. the class name can be CustomerTableAdapter
        /// the property name can be CustomerTableAdapter as well if not conflict 
        /// </summary>
        /// <param name="className"></param> 
        /// <returns></returns> 
        internal string GetTableAdapterPropName(string className) {
            return this.GetNewMemberName(className); 
        }

        /// <summary>
        /// Helper function to get the TableAdapter variable name 
        /// </summary>
        /// <param name="propName">Property Name, e.g. CustomerTableAdapter</param> 
        /// <returns>variable name like _customerTableAdapter</returns> 
        internal string GetTableAdapterVarName(string propName) {
            Debug.Assert(propName != null && propName.Length > 0); 
            Debug.Assert(propName.IndexOf('.') < 0);
            propName = "_" + Char.ToLower(propName[0],CultureInfo.InvariantCulture) + propName.Remove(0, 1);
            //
            return this.GetNewMemberName(propName); 
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
    using System.CodeDom.Compiler;

    internal sealed class TableAdapterManagerNameHandler { 
        // Non-private class/method/property names used in TableAdapterManager
        internal const string TableAdapterManagerClassName = "TableAdapterManager"; 
        internal const string SelfRefComparerClass = "SelfReferenceComparer"; 
        internal const string UpdateAllMethod = "UpdateAll";
        internal const string SortSelfRefRowsMethod = "SortSelfReferenceRows"; 
        internal const string MatchTAConnectionMethod = "MatchTableAdapterConnection";
        internal const string UpdateAllRevertConnectionsVar = "revertConnections";
        internal const string ConnectionVar = "_connection";
        internal const string ConnectionProperty = "Connection"; 
        internal const string BackupDataSetBeforeUpdateVar = "_backupDataSetBeforeUpdate";
        internal const string BackupDataSetBeforeUpdateProperty = "BackupDataSetBeforeUpdate"; 
        internal const string TableAdapterInstanceCountProperty = "TableAdapterInstanceCount"; 
        internal const string UpdateOrderOptionProperty = "UpdateOrder";
        internal const string UpdateOrderOptionVar = "_updateOrder"; 
        internal const string UpdateOrderOptionEnum = "UpdateOrderOption";
        internal const string UpdateOrderOptionEnumIUD = "InsertUpdateDelete";
        internal const string UpdateOrderOptionEnumUID = "UpdateInsertDelete";
        internal const string UpdateUpdatedRowsMethod = "UpdateUpdatedRows"; 
        internal const string UpdateInsertedRowsMethod = "UpdateInsertedRows";
        internal const string UpdateDeletedRowsMethod =  "UpdateDeletedRows"; 
        internal const string GetRealUpdatedRowsMethod = "GetRealUpdatedRows"; 

        private MemberNameValidator tableAdapterManagerValidator = null; 
        private bool languageCaseInsensitive = false;
        private CodeDomProvider codePrivider = null;

        public TableAdapterManagerNameHandler(CodeDomProvider provider) { 
            this.codePrivider = provider;
            this.languageCaseInsensitive = (this.codePrivider.LanguageOptions & LanguageOptions.CaseInsensitive) == LanguageOptions.CaseInsensitive; 
        } 

        private MemberNameValidator TableAdapterManagerValidator { 
            get {
                if (tableAdapterManagerValidator == null) {
                    tableAdapterManagerValidator = new MemberNameValidator(
                        new string[]{ 
                             SelfRefComparerClass,
                             UpdateAllMethod, 
                             SortSelfRefRowsMethod, 
                             MatchTAConnectionMethod,
                             ConnectionVar, 
                             ConnectionProperty,
                             BackupDataSetBeforeUpdateVar,
                             BackupDataSetBeforeUpdateProperty,
                             TableAdapterInstanceCountProperty, 
                             UpdateOrderOptionProperty,
                             UpdateOrderOptionVar, 
                             UpdateOrderOptionEnum, 
                             UpdateUpdatedRowsMethod,
                             UpdateInsertedRowsMethod, 
                             UpdateDeletedRowsMethod,
                             GetRealUpdatedRowsMethod
                        },
                        this.codePrivider, this.languageCaseInsensitive); 
                }
                return this.tableAdapterManagerValidator; 
            } 
        }
 
        /// <summary>
        /// Get a valid member name not conflict with known reserved name like ConnectionManager
        /// </summary>
        /// <param name="memberName"></param> 
        /// <returns></returns>
        internal string GetNewMemberName(string memberName) { 
            return this.TableAdapterManagerValidator.GetNewMemberName(memberName); 
        }
 
        /// <summary>
        /// Get an valid TableAdapter property name
        /// e.g. the class name can be CustomerTableAdapter
        /// the property name can be CustomerTableAdapter as well if not conflict 
        /// </summary>
        /// <param name="className"></param> 
        /// <returns></returns> 
        internal string GetTableAdapterPropName(string className) {
            return this.GetNewMemberName(className); 
        }

        /// <summary>
        /// Helper function to get the TableAdapter variable name 
        /// </summary>
        /// <param name="propName">Property Name, e.g. CustomerTableAdapter</param> 
        /// <returns>variable name like _customerTableAdapter</returns> 
        internal string GetTableAdapterVarName(string propName) {
            Debug.Assert(propName != null && propName.Length > 0); 
            Debug.Assert(propName.IndexOf('.') < 0);
            propName = "_" + Char.ToLower(propName[0],CultureInfo.InvariantCulture) + propName.Remove(0, 1);
            //
            return this.GetNewMemberName(propName); 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
