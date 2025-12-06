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
    using System.Collections;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection; 
    using System.CodeDom.Compiler; 

    internal sealed class DataComponentNameHandler { 
        private MemberNameValidator validator = null;
        private bool languageCaseInsensitive = false;
        private bool globalSources = false;
 
        private static readonly string pagingMethodSuffix = "Page";
 
        private static readonly string initMethodName = "InitClass"; 
        private static readonly string deleteMethodName = "Delete";
        private static readonly string insertMethodName = "Insert"; 
        private static readonly string updateMethodName = "Update";
        private static readonly string adapterVariableName = "_adapter";
        private static readonly string adapterPropertyName = "Adapter";
        private static readonly string initAdapter = "InitAdapter"; 
        private static readonly string selectCmdCollectionVariableName = "_commandCollection";
        private static readonly string selectCmdCollectionPropertyName = "CommandCollection"; 
        private static readonly string initCmdCollection = "InitCommandCollection"; 
        private static readonly string defaultConnectionVariableName = "_connection";
        private static readonly string defaultConnectionPropertyName = "Connection"; 
        private static readonly string transactionVariableName = "_transaction";
        private static readonly string transactionPropertyName = "Transaction";
        private static readonly string initConnection = "InitConnection";
        private static readonly string clearBeforeFillVariableName = "_clearBeforeFill"; 
        private static readonly string clearBeforeFillPropertyName = "ClearBeforeFill";
        //private static readonly string shortDeleteCmdVariableName = "m_DeleteCommand"; 
        //private static readonly string shortDeleteCmdPropertyName = "DeleteCommand"; 
        //private static readonly string shortInsertCmdVariableName = "m_InsertCommand";
        //private static readonly string shortInsertCmdPropertyName = "InsertCommand"; 
        //private static readonly string shortUpdateCmdVariableName = "m_UpdateCommand";
        //private static readonly string shortUpdateCmdPropertyName = "UpdateCommand";
        //private static readonly string initShortDeleteCmd = "InitDeleteCommand";
        //private static readonly string initShortInsertCmd = "InitInsertCommand"; 
        //private static readonly string initShortUpdateCmd = "InitUpdateCommand";
 
 
        internal bool GlobalSources {
            get { 
                return this.globalSources;
            }
            set {
                this.globalSources = value; 
            }
        } 
 

        internal void GenerateMemberNames(DesignTable designTable, CodeDomProvider codeProvider, bool languageCaseInsensitive, ArrayList problemList) { 
            this.languageCaseInsensitive = languageCaseInsensitive;
            this.validator = new MemberNameValidator(null, codeProvider, this.languageCaseInsensitive);
            // tell the validator to fix up names by appending a number as suffix instead of an underscore as prefix for the
            // TableAdapters (we don't have to care about backward compatibility here). 
            this.validator.UseSuffix = true;
 
            this.AddReservedNames(); 

            // generate names for added/renamed members, 
            ProcessMemberNames(designTable);
        }

        private void AddReservedNames() { 
            this.validator.GetNewMemberName(initMethodName);
            this.validator.GetNewMemberName(deleteMethodName); 
            this.validator.GetNewMemberName(insertMethodName); 
            this.validator.GetNewMemberName(updateMethodName);
            this.validator.GetNewMemberName(adapterVariableName); 
            this.validator.GetNewMemberName(adapterPropertyName);
            this.validator.GetNewMemberName(initAdapter);
            this.validator.GetNewMemberName(selectCmdCollectionVariableName);
            this.validator.GetNewMemberName(selectCmdCollectionPropertyName); 
            this.validator.GetNewMemberName(initCmdCollection);
            this.validator.GetNewMemberName(defaultConnectionVariableName); 
            this.validator.GetNewMemberName(defaultConnectionPropertyName); 
            this.validator.GetNewMemberName(transactionVariableName);
            this.validator.GetNewMemberName(transactionPropertyName); 
            this.validator.GetNewMemberName(initConnection);
            this.validator.GetNewMemberName(clearBeforeFillVariableName);
            this.validator.GetNewMemberName(clearBeforeFillPropertyName);
 
            this.validator.GetNewMemberName(TableAdapterManagerNameHandler.TableAdapterManagerClassName);
            this.validator.GetNewMemberName(TableAdapterManagerNameHandler.UpdateAllMethod); 
 
            //this.validator.GetNewMemberName(shortDeleteCmdVariableName);
            //this.validator.GetNewMemberName(shortDeleteCmdPropertyName); 
            //this.validator.GetNewMemberName(shortInsertCmdVariableName);
            //this.validator.GetNewMemberName(shortInsertCmdPropertyName);
            //this.validator.GetNewMemberName(shortUpdateCmdVariableName);
            //this.validator.GetNewMemberName(shortUpdateCmdPropertyName); 
            //this.validator.GetNewMemberName(initShortInsertCmd);
            //this.validator.GetNewMemberName(initShortDeleteCmd); 
            //this.validator.GetNewMemberName(initShortUpdateCmd); 
        }
 

        private void ProcessMemberNames(DesignTable designTable) {
            // process class name
            this.ProcessClassName(designTable); 

            // process interface name 
//            this.ProcessInterfaceName(designTable); 

            // process source names 
            if(!this.GlobalSources && designTable.MainSource != null) {
                this.ProcessSourceName((DbSource) designTable.MainSource);
            }
            if(designTable.Sources != null) { 
                foreach(Source source in designTable.Sources) {
                    this.ProcessSourceName((DbSource) source); 
                } 
            }
        } 

        internal void ProcessClassName(DesignTable table) {
            bool componentRenamed = !StringUtil.EqualValue(table.DataAccessorName, table.UserDataComponentName, this.languageCaseInsensitive);
 
            if (componentRenamed || StringUtil.Empty(table.GeneratorDataComponentClassName)) {
                table.GeneratorDataComponentClassName = this.validator.GenerateIdName(table.DataAccessorName); 
            } 
            else {
                table.GeneratorDataComponentClassName = this.validator.GenerateIdName(table.GeneratorDataComponentClassName); 
            }
        }

//        internal void ProcessInterfaceName(DesignTable table) { 
//            bool componentRenamed = !StringUtil.EqualValue(table.DataAccessorName, table.UserDataComponentName, this.languageCaseInsensitive);
// 
//            if (componentRenamed || StringUtil.Empty(table.GeneratorDataComponentInterfaceName)) { 
//                table.GeneratorDataComponentInterfaceName = this.validator.GenerateIdName("I" + table.DataAccessorName);
//            } 
//        }


        internal void ProcessSourceName(DbSource source) { 
            bool sourceFillRenamed = !StringUtil.EqualValue(source.Name, source.UserSourceName, this.languageCaseInsensitive);
            bool sourceGetRenamed = !StringUtil.EqualValue(source.GetMethodName, source.UserGetMethodName, this.languageCaseInsensitive); 
 
            if(source.GenerateMethods == GenerateMethodTypes.Fill || source.GenerateMethods == GenerateMethodTypes.Both) {
                if (sourceFillRenamed || StringUtil.Empty(source.GeneratorSourceName)) { 
                    source.GeneratorSourceName = this.validator.GenerateIdName(source.Name);
                }
                else {
                    source.GeneratorSourceName = this.validator.GenerateIdName(source.GeneratorSourceName); 
                }
            } 
 
            if(source.QueryType == QueryType.Rowset && (source.GenerateMethods == GenerateMethodTypes.Get || source.GenerateMethods == GenerateMethodTypes.Both)) {
                if (sourceGetRenamed || StringUtil.Empty(source.GeneratorGetMethodName)) { 
                    source.GeneratorGetMethodName = this.validator.GenerateIdName(source.GetMethodName);
                }
                else {
                    source.GeneratorGetMethodName = this.validator.GenerateIdName(source.GeneratorGetMethodName); 
                }
            } 
 
            if(source.QueryType == QueryType.Rowset && source.GeneratePagingMethods) {
                if(source.GenerateMethods == GenerateMethodTypes.Fill || source.GenerateMethods == GenerateMethodTypes.Both) { 
                    if (sourceFillRenamed || StringUtil.Empty(source.GeneratorSourceNameForPaging)) {
                        source.GeneratorSourceNameForPaging = this.validator.GenerateIdName(source.Name + pagingMethodSuffix);
                    }
                    else { 
                        source.GeneratorSourceNameForPaging = this.validator.GenerateIdName(source.GeneratorSourceNameForPaging);
                    } 
                } 

                if(source.GenerateMethods == GenerateMethodTypes.Get || source.GenerateMethods == GenerateMethodTypes.Both) { 
                    if (sourceGetRenamed || StringUtil.Empty(source.GeneratorGetMethodNameForPaging)) {
                        source.GeneratorGetMethodNameForPaging = this.validator.GenerateIdName(source.GetMethodName + pagingMethodSuffix);
                    }
                    else { 
                        source.GeneratorGetMethodNameForPaging = this.validator.GenerateIdName(source.GeneratorGetMethodNameForPaging);
                    } 
                } 
            }
        } 


        internal static string DeleteMethodName {
            get { 
                return deleteMethodName;
            } 
        } 
        internal static string UpdateMethodName {
            get { 
                return updateMethodName;
            }
        }
        internal static string InsertMethodName { 
            get {
                return insertMethodName; 
            } 
        }
        internal static string AdapterVariableName { 
            get {
                return adapterVariableName;
            }
        } 
        internal static string AdapterPropertyName {
            get { 
                return adapterPropertyName; 
            }
        } 
        internal static string InitAdapter {
            get {
                return initAdapter;
            } 
        }
        internal static string SelectCmdCollectionVariableName { 
            get { 
                return selectCmdCollectionVariableName;
            } 
        }
        internal static string SelectCmdCollectionPropertyName {
            get {
                return selectCmdCollectionPropertyName; 
            }
        } 
        internal static string InitCmdCollection { 
            get {
                return initCmdCollection; 
            }
        }
        internal static string DefaultConnectionVariableName {
            get { 
                return defaultConnectionVariableName;
            } 
        } 
        internal static string DefaultConnectionPropertyName {
            get { 
                return defaultConnectionPropertyName;
            }
        }
        internal static string TransactionPropertyName { 
            get {
                return transactionPropertyName; 
            } 
        }
        internal static string TransactionVariableName { 
            get {
                return transactionVariableName;
            }
        } 
        internal static string InitConnection {
            get { 
                return initConnection; 
            }
        } 
        internal static string PagingMethodSuffix {
            get {
                return pagingMethodSuffix;
            } 
        }
        internal static string ClearBeforeFillVariableName { 
            get { 
                return clearBeforeFillVariableName;
            } 
        }
        internal static string ClearBeforeFillPropertyName {
            get {
                return clearBeforeFillPropertyName; 
            }
        } 
        //internal static string ShortDeleteCmdVariableName { 
        //    get {
        //        return shortDeleteCmdVariableName; 
        //    }
        //}
        //internal static string ShortDeleteCmdPropertyName {
        //    get { 
        //        return shortDeleteCmdPropertyName;
        //    } 
        //} 
        //internal static string ShortInsertCmdVariableName {
        //    get { 
        //        return shortInsertCmdVariableName;
        //    }
        //}
        //internal static string ShortInsertCmdPropertyName { 
        //    get {
        //        return shortInsertCmdPropertyName; 
        //    } 
        //}
        //internal static string ShortUpdateCmdVariableName { 
        //    get {
        //        return shortUpdateCmdVariableName;
        //    }
        //} 
        //internal static string ShortUpdateCmdPropertyName {
        //    get { 
        //        return shortUpdateCmdPropertyName; 
        //    }
        //} 
        //internal static string InitShortDeleteCmd {
        //    get {
        //        return initShortDeleteCmd;
        //    } 
        //}
        //internal static string InitShortInsertCmd { 
        //    get { 
        //        return initShortInsertCmd;
        //    } 
        //}
        //internal static string InitShortUpdateCmd {
        //    get {
        //        return initShortUpdateCmd; 
        //    }
        //} 
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
    using System.Collections;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection; 
    using System.CodeDom.Compiler; 

    internal sealed class DataComponentNameHandler { 
        private MemberNameValidator validator = null;
        private bool languageCaseInsensitive = false;
        private bool globalSources = false;
 
        private static readonly string pagingMethodSuffix = "Page";
 
        private static readonly string initMethodName = "InitClass"; 
        private static readonly string deleteMethodName = "Delete";
        private static readonly string insertMethodName = "Insert"; 
        private static readonly string updateMethodName = "Update";
        private static readonly string adapterVariableName = "_adapter";
        private static readonly string adapterPropertyName = "Adapter";
        private static readonly string initAdapter = "InitAdapter"; 
        private static readonly string selectCmdCollectionVariableName = "_commandCollection";
        private static readonly string selectCmdCollectionPropertyName = "CommandCollection"; 
        private static readonly string initCmdCollection = "InitCommandCollection"; 
        private static readonly string defaultConnectionVariableName = "_connection";
        private static readonly string defaultConnectionPropertyName = "Connection"; 
        private static readonly string transactionVariableName = "_transaction";
        private static readonly string transactionPropertyName = "Transaction";
        private static readonly string initConnection = "InitConnection";
        private static readonly string clearBeforeFillVariableName = "_clearBeforeFill"; 
        private static readonly string clearBeforeFillPropertyName = "ClearBeforeFill";
        //private static readonly string shortDeleteCmdVariableName = "m_DeleteCommand"; 
        //private static readonly string shortDeleteCmdPropertyName = "DeleteCommand"; 
        //private static readonly string shortInsertCmdVariableName = "m_InsertCommand";
        //private static readonly string shortInsertCmdPropertyName = "InsertCommand"; 
        //private static readonly string shortUpdateCmdVariableName = "m_UpdateCommand";
        //private static readonly string shortUpdateCmdPropertyName = "UpdateCommand";
        //private static readonly string initShortDeleteCmd = "InitDeleteCommand";
        //private static readonly string initShortInsertCmd = "InitInsertCommand"; 
        //private static readonly string initShortUpdateCmd = "InitUpdateCommand";
 
 
        internal bool GlobalSources {
            get { 
                return this.globalSources;
            }
            set {
                this.globalSources = value; 
            }
        } 
 

        internal void GenerateMemberNames(DesignTable designTable, CodeDomProvider codeProvider, bool languageCaseInsensitive, ArrayList problemList) { 
            this.languageCaseInsensitive = languageCaseInsensitive;
            this.validator = new MemberNameValidator(null, codeProvider, this.languageCaseInsensitive);
            // tell the validator to fix up names by appending a number as suffix instead of an underscore as prefix for the
            // TableAdapters (we don't have to care about backward compatibility here). 
            this.validator.UseSuffix = true;
 
            this.AddReservedNames(); 

            // generate names for added/renamed members, 
            ProcessMemberNames(designTable);
        }

        private void AddReservedNames() { 
            this.validator.GetNewMemberName(initMethodName);
            this.validator.GetNewMemberName(deleteMethodName); 
            this.validator.GetNewMemberName(insertMethodName); 
            this.validator.GetNewMemberName(updateMethodName);
            this.validator.GetNewMemberName(adapterVariableName); 
            this.validator.GetNewMemberName(adapterPropertyName);
            this.validator.GetNewMemberName(initAdapter);
            this.validator.GetNewMemberName(selectCmdCollectionVariableName);
            this.validator.GetNewMemberName(selectCmdCollectionPropertyName); 
            this.validator.GetNewMemberName(initCmdCollection);
            this.validator.GetNewMemberName(defaultConnectionVariableName); 
            this.validator.GetNewMemberName(defaultConnectionPropertyName); 
            this.validator.GetNewMemberName(transactionVariableName);
            this.validator.GetNewMemberName(transactionPropertyName); 
            this.validator.GetNewMemberName(initConnection);
            this.validator.GetNewMemberName(clearBeforeFillVariableName);
            this.validator.GetNewMemberName(clearBeforeFillPropertyName);
 
            this.validator.GetNewMemberName(TableAdapterManagerNameHandler.TableAdapterManagerClassName);
            this.validator.GetNewMemberName(TableAdapterManagerNameHandler.UpdateAllMethod); 
 
            //this.validator.GetNewMemberName(shortDeleteCmdVariableName);
            //this.validator.GetNewMemberName(shortDeleteCmdPropertyName); 
            //this.validator.GetNewMemberName(shortInsertCmdVariableName);
            //this.validator.GetNewMemberName(shortInsertCmdPropertyName);
            //this.validator.GetNewMemberName(shortUpdateCmdVariableName);
            //this.validator.GetNewMemberName(shortUpdateCmdPropertyName); 
            //this.validator.GetNewMemberName(initShortInsertCmd);
            //this.validator.GetNewMemberName(initShortDeleteCmd); 
            //this.validator.GetNewMemberName(initShortUpdateCmd); 
        }
 

        private void ProcessMemberNames(DesignTable designTable) {
            // process class name
            this.ProcessClassName(designTable); 

            // process interface name 
//            this.ProcessInterfaceName(designTable); 

            // process source names 
            if(!this.GlobalSources && designTable.MainSource != null) {
                this.ProcessSourceName((DbSource) designTable.MainSource);
            }
            if(designTable.Sources != null) { 
                foreach(Source source in designTable.Sources) {
                    this.ProcessSourceName((DbSource) source); 
                } 
            }
        } 

        internal void ProcessClassName(DesignTable table) {
            bool componentRenamed = !StringUtil.EqualValue(table.DataAccessorName, table.UserDataComponentName, this.languageCaseInsensitive);
 
            if (componentRenamed || StringUtil.Empty(table.GeneratorDataComponentClassName)) {
                table.GeneratorDataComponentClassName = this.validator.GenerateIdName(table.DataAccessorName); 
            } 
            else {
                table.GeneratorDataComponentClassName = this.validator.GenerateIdName(table.GeneratorDataComponentClassName); 
            }
        }

//        internal void ProcessInterfaceName(DesignTable table) { 
//            bool componentRenamed = !StringUtil.EqualValue(table.DataAccessorName, table.UserDataComponentName, this.languageCaseInsensitive);
// 
//            if (componentRenamed || StringUtil.Empty(table.GeneratorDataComponentInterfaceName)) { 
//                table.GeneratorDataComponentInterfaceName = this.validator.GenerateIdName("I" + table.DataAccessorName);
//            } 
//        }


        internal void ProcessSourceName(DbSource source) { 
            bool sourceFillRenamed = !StringUtil.EqualValue(source.Name, source.UserSourceName, this.languageCaseInsensitive);
            bool sourceGetRenamed = !StringUtil.EqualValue(source.GetMethodName, source.UserGetMethodName, this.languageCaseInsensitive); 
 
            if(source.GenerateMethods == GenerateMethodTypes.Fill || source.GenerateMethods == GenerateMethodTypes.Both) {
                if (sourceFillRenamed || StringUtil.Empty(source.GeneratorSourceName)) { 
                    source.GeneratorSourceName = this.validator.GenerateIdName(source.Name);
                }
                else {
                    source.GeneratorSourceName = this.validator.GenerateIdName(source.GeneratorSourceName); 
                }
            } 
 
            if(source.QueryType == QueryType.Rowset && (source.GenerateMethods == GenerateMethodTypes.Get || source.GenerateMethods == GenerateMethodTypes.Both)) {
                if (sourceGetRenamed || StringUtil.Empty(source.GeneratorGetMethodName)) { 
                    source.GeneratorGetMethodName = this.validator.GenerateIdName(source.GetMethodName);
                }
                else {
                    source.GeneratorGetMethodName = this.validator.GenerateIdName(source.GeneratorGetMethodName); 
                }
            } 
 
            if(source.QueryType == QueryType.Rowset && source.GeneratePagingMethods) {
                if(source.GenerateMethods == GenerateMethodTypes.Fill || source.GenerateMethods == GenerateMethodTypes.Both) { 
                    if (sourceFillRenamed || StringUtil.Empty(source.GeneratorSourceNameForPaging)) {
                        source.GeneratorSourceNameForPaging = this.validator.GenerateIdName(source.Name + pagingMethodSuffix);
                    }
                    else { 
                        source.GeneratorSourceNameForPaging = this.validator.GenerateIdName(source.GeneratorSourceNameForPaging);
                    } 
                } 

                if(source.GenerateMethods == GenerateMethodTypes.Get || source.GenerateMethods == GenerateMethodTypes.Both) { 
                    if (sourceGetRenamed || StringUtil.Empty(source.GeneratorGetMethodNameForPaging)) {
                        source.GeneratorGetMethodNameForPaging = this.validator.GenerateIdName(source.GetMethodName + pagingMethodSuffix);
                    }
                    else { 
                        source.GeneratorGetMethodNameForPaging = this.validator.GenerateIdName(source.GeneratorGetMethodNameForPaging);
                    } 
                } 
            }
        } 


        internal static string DeleteMethodName {
            get { 
                return deleteMethodName;
            } 
        } 
        internal static string UpdateMethodName {
            get { 
                return updateMethodName;
            }
        }
        internal static string InsertMethodName { 
            get {
                return insertMethodName; 
            } 
        }
        internal static string AdapterVariableName { 
            get {
                return adapterVariableName;
            }
        } 
        internal static string AdapterPropertyName {
            get { 
                return adapterPropertyName; 
            }
        } 
        internal static string InitAdapter {
            get {
                return initAdapter;
            } 
        }
        internal static string SelectCmdCollectionVariableName { 
            get { 
                return selectCmdCollectionVariableName;
            } 
        }
        internal static string SelectCmdCollectionPropertyName {
            get {
                return selectCmdCollectionPropertyName; 
            }
        } 
        internal static string InitCmdCollection { 
            get {
                return initCmdCollection; 
            }
        }
        internal static string DefaultConnectionVariableName {
            get { 
                return defaultConnectionVariableName;
            } 
        } 
        internal static string DefaultConnectionPropertyName {
            get { 
                return defaultConnectionPropertyName;
            }
        }
        internal static string TransactionPropertyName { 
            get {
                return transactionPropertyName; 
            } 
        }
        internal static string TransactionVariableName { 
            get {
                return transactionVariableName;
            }
        } 
        internal static string InitConnection {
            get { 
                return initConnection; 
            }
        } 
        internal static string PagingMethodSuffix {
            get {
                return pagingMethodSuffix;
            } 
        }
        internal static string ClearBeforeFillVariableName { 
            get { 
                return clearBeforeFillVariableName;
            } 
        }
        internal static string ClearBeforeFillPropertyName {
            get {
                return clearBeforeFillPropertyName; 
            }
        } 
        //internal static string ShortDeleteCmdVariableName { 
        //    get {
        //        return shortDeleteCmdVariableName; 
        //    }
        //}
        //internal static string ShortDeleteCmdPropertyName {
        //    get { 
        //        return shortDeleteCmdPropertyName;
        //    } 
        //} 
        //internal static string ShortInsertCmdVariableName {
        //    get { 
        //        return shortInsertCmdVariableName;
        //    }
        //}
        //internal static string ShortInsertCmdPropertyName { 
        //    get {
        //        return shortInsertCmdPropertyName; 
        //    } 
        //}
        //internal static string ShortUpdateCmdVariableName { 
        //    get {
        //        return shortUpdateCmdVariableName;
        //    }
        //} 
        //internal static string ShortUpdateCmdPropertyName {
        //    get { 
        //        return shortUpdateCmdPropertyName; 
        //    }
        //} 
        //internal static string InitShortDeleteCmd {
        //    get {
        //        return initShortDeleteCmd;
        //    } 
        //}
        //internal static string InitShortInsertCmd { 
        //    get { 
        //        return initShortInsertCmd;
        //    } 
        //}
        //internal static string InitShortUpdateCmd {
        //    get {
        //        return initShortUpdateCmd; 
        //    }
        //} 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
