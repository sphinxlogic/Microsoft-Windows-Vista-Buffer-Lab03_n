//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
    using System.Data.Common; 
    using System.Diagnostics; 
    using System.Globalization;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Windows.Forms;
    using System.Xml;
    using System.Xml.Schema; 
    using System.Xml.Serialization;
 
 

 
    internal enum QueryType {
        Rowset,
        Scalar,
        NoData 
    }
 
    [Flags] 
    internal enum GenerateMethodTypes {
        Fill = 1, 
        Get = 2,
        Both = Fill + Get,
    }
 
    [
        DataSourceXmlClass(SchemaName.DbSource) 
    ] 
    internal class DbSource: Source, IDataSourceXmlSpecialOwner {
        private IDesignConnection connection; 
        private DbSourceCommand selectCommand;
        private DbSourceCommand insertCommand;
        private DbSourceCommand updateCommand;
        private DbSourceCommand deleteCommand; 
        private DbObjectType dbObjectType = DbObjectType.Unknown;
        private string connectionRef; 
        private Type scalarCallRetval = typeof(object); 
        private string userGetMethodName;
        private string getMethodName;// = INSTANCE_NAME_FOR_GETMETHOD_MAIN; 
        private MemberAttributes getMethodModifier = MemberAttributes.Public; // undone: review the default value for modifier
        private QueryType queryType = QueryType.Rowset;
        private GenerateMethodTypes generateMethods = GenerateMethodTypes.Both;
        private bool generatePagingMethods = false; 
        private bool generateShortCommands = true;
        private bool useOptimisticConcurrency = true; 
 
        private TypeEnum parameterType = TypeEnum.CLR;
 
        internal const string TYPE_NAME_FOR_QUERY = "Query";
        internal const string TYPE_NAME_FOR_FUNCTION = "Query";
        private  const string PROPERTY_COMMANDTEXT = "CommandText";
 
        internal const string INSTANCE_NAME_FOR_FILLMETHOD_MAIN = "Fill";
        internal const string INSTANCE_NAME_FOR_GETMETHOD_MAIN = "GetData"; 
        internal const string INSTANCE_NAME_FOR_FILLMETHOD = "FillBy"; 
        internal const string INSTANCE_NAME_FOR_GETMETHOD = "GetDataBy";
        internal const string INSTANCE_NAME_FOR_FUNCTION = "Query"; 

        public DbSource() { }

        //public DbSource(string name){ 
        //    this.Name = name;
        //} 
 
        protected internal override DataSourceCollectionBase CollectionParent {
            get { 
                if (base.CollectionParent != null) {
                    return base.CollectionParent;
                }
                if (this.owner != null && owner is DesignTable && ((DesignTable)this.owner).MainSource == this){ 
                    return ((DesignTable)this.owner).Sources;
                } 
                return null; 
            }
        } 


        [
            Browsable(false), 
            DataSourceXmlAttribute(),
        ] 
        public string ConnectionRef { 
            get {
                if (this.connection != null) { 
                    return this.connection.Name;
                }
                return connectionRef;
            } 
            set {
                connectionRef = value; 
            } 
        }
 


        [
            Browsable(false), 
            DataSourceXmlAttribute(SpecialWay=true)
        ] 
        public Type ScalarCallRetval { 
            get {
                return this.scalarCallRetval; 
            }
        }

        [ 
            DefaultValue(null),
            RefreshProperties(RefreshProperties.All) 
        ] 
        public IDesignConnection Connection {
            get { 
                return this.connection;
            }
            set {
                this.connection = value; 
            }
        } 
 

 
        [
            DefaultValue(TypeEnum.CLR),
            DataSourceXmlAttribute()
        ] 
        public TypeEnum MethodsParameterType {
            get { 
                return parameterType; 
            }
            set { 
                parameterType = value;
            }
        }
 
        public CommandOperation CommandOperation {
            get { 
                // Order is important here. 
                if (this.SelectCommand != null) {
                    return CommandOperation.Select; 
                }

                // If this source does not have a SELECT command, it may only have one of the three remaining commands.
                if (this.InsertCommand != null) { 
                    Debug.Assert(this.UpdateCommand == null && this.DeleteCommand == null);
                    return CommandOperation.Insert; 
                } 

                if (this.UpdateCommand != null) { 
                    Debug.Assert(this.DeleteCommand == null);
                    return CommandOperation.Update;
                }
 
                if (this.DeleteCommand != null) {
                    return CommandOperation.Delete; 
                } 

                return CommandOperation.Unknown; 
            }
        }

        #region Modifiers 
        [
            DefaultValue(MemberAttributes.Public), 
            DataSourceXmlAttribute() 
        ]
        public MemberAttributes FillMethodModifier { 
            get {
                return this.Modifier;
            }
            set { 
                this.Modifier = value;
            } 
        } 

        [ 
            DefaultValue(MemberAttributes.Public),
            DataSourceXmlAttribute()
        ]
        public MemberAttributes GetMethodModifier { 
            get {
                return this.getMethodModifier; 
            } 
            set {
                this.getMethodModifier = value; 
            }
        }
        #endregion
 

        #region Names 
        /// <summary> 
        /// </summary>
        [ 
            Browsable(false),
        ]
        public override string Name {
            get { 
                if (StringUtil.Empty(base.Name)) {
                    if (this.generateMethods == GenerateMethodTypes.Get) { 
                        return this.GetMethodName; 
                    }
                } 
                return base.Name;
            }
            set {
                if (name != value) { 
                    base.name = value;
                    SourceCollection sc = this.CollectionParent as SourceCollection; 
                    if (sc != null) { 
                        sc.ValidateUniqueDbSourceName(this, value, true);
                    } 
                }
            }
        }
 
        /// <summary>
        /// </summary> 
        [ 
            DataSourceXmlAttribute (),
            DefaultValue(INSTANCE_NAME_FOR_FILLMETHOD_MAIN) 
        ]
        public string FillMethodName {
            get {
                return this.Name; 
            }
            set { 
                this.Name = value; 
            }
        } 


        /// <summary>
        /// </summary> 
        [
            DefaultValue(INSTANCE_NAME_FOR_GETMETHOD_MAIN), 
            DataSourceXmlAttribute() 
        ]
        public string GetMethodName { 
            get {
                if (StringUtil.EmptyOrSpace(this.getMethodName) && this.CollectionParent != null) {
                    if (this.IsMainSource) {
                        this.GetMethodName = DbSource.INSTANCE_NAME_FOR_GETMETHOD_MAIN; 
                    }
                    else { 
                        this.GetMethodName = DbSource.INSTANCE_NAME_FOR_GETMETHOD; 
                    }
                } 

                return this.getMethodName;
            }
            set { 
                getMethodName = value;
            } 
        } 

        [ 
            DataSourceXmlAttribute()
        ]
        public string UserGetMethodName {
            get { 
                return this.userGetMethodName;
            } 
            set { 
                this.userGetMethodName = value;
            } 
        }

        #endregion
 

        /// <summary> 
        /// </summary> 
        [
            DefaultValue(GenerateMethodTypes.Both), 
            DataSourceXmlAttribute(),
            RefreshProperties(RefreshProperties.All)
        ]
        public GenerateMethodTypes GenerateMethods { 
            get {
                return this.generateMethods; 
            } 
            set {
                this.generateMethods = value; 
            }
        }

        /// <summary> 
        /// </summary>
        [ 
            DefaultValue(true), 
            DataSourceXmlAttribute()
        ] 
        public bool GeneratePagingMethods {
            get {
                return this.generatePagingMethods;
            } 
            set {
                this.generatePagingMethods = value; 
            } 
        }
 
        [Browsable(false)]
        public override object Parent {
            get {
                if (base.Parent != null) { 
                    return base.Parent;
                } 
                return this.Owner; 
            }
        } 

        [Browsable(false)]
        public override string PublicTypeName {
            get { 
                if( this.Owner is DesignTable ) {
                    return DbSource.TYPE_NAME_FOR_QUERY; 
                } 
                else {
                    return DbSource.TYPE_NAME_FOR_FUNCTION; 
                }
            }
        }
 
        [
            DataSourceXmlAttribute(), 
            Browsable(false), 
        ]
        public QueryType QueryType { 
            get {
                return queryType;
            }
            set { 
                queryType = value;
                if (queryType != QueryType.Rowset) { 
                    this.GenerateMethods = GenerateMethodTypes.Fill; 
                }
            } 
        }

        #region Commands
        [ 
            DataSourceXmlSubItem(Name="SelectCommand", ItemType=typeof(DbSourceCommand)),
            Browsable(false), 
        ] 
        public DbSourceCommand SelectCommand {
            get { 
                return this.selectCommand;
            }
            set {
                if (this.selectCommand != null){ 
                    this.selectCommand.SetParent(null);
                } 
                this.selectCommand = value; 
                if (this.selectCommand != null){
                    this.selectCommand.SetParent(this); 
                    this.selectCommand.CommandOperation = CommandOperation.Select;
                }
            }
        } 

        [ 
            DataSourceXmlSubItem(Name="UpdateCommand", ItemType=typeof(DbSourceCommand)), 
            Browsable(false),
        ] 
        public DbSourceCommand UpdateCommand {
            get {
                return this.updateCommand;
            } 
            set {
                if (this.updateCommand != null){ 
                    this.updateCommand.SetParent(null); 
                }
                this.updateCommand = value; 
                if (this.updateCommand != null){
                    this.updateCommand.SetParent(this);
                    this.updateCommand.CommandOperation = CommandOperation.Update;
                } 
            }
        } 
 
        [DataSourceXmlSubItem(Name = "DeleteCommand", ItemType = typeof(DbSourceCommand)), Browsable(false),]
        public DbSourceCommand DeleteCommand { 
            get {
                return this.deleteCommand;
            }
            set { 
                if (this.deleteCommand != null) {
                    this.deleteCommand.SetParent(null); 
                } 

                this.deleteCommand = value; 
                if (this.deleteCommand != null) {
                    this.deleteCommand.SetParent(this);
                    this.deleteCommand.CommandOperation = CommandOperation.Delete;
                } 
            }
        } 
 
        [DataSourceXmlSubItem(Name = "InsertCommand", ItemType = typeof(DbSourceCommand)), Browsable(false),]
        public DbSourceCommand InsertCommand { 
            get {
                return this.insertCommand;
            }
            set { 
                if (this.insertCommand != null) {
                    this.insertCommand.SetParent(null); 
                } 

                this.insertCommand = value; 
                if (this.insertCommand != null) {
                    this.insertCommand.SetParent(this);
                    this.insertCommand.CommandOperation = CommandOperation.Insert;
                } 
            }
        } 
 
        #endregion
 

        [DataSourceXmlAttribute()]
        public DbObjectType DbObjectType {
            get { 
                return this.dbObjectType;
            } 
            set { 
                this.dbObjectType = value;
            } 
        }

        [DataSourceXmlAttribute()]
        public bool UseOptimisticConcurrency { 
            get {
                return this.useOptimisticConcurrency; 
            } 

            set { 
                this.useOptimisticConcurrency = value;
            }
        }
 
        /// <summary>
        /// Compare the source by name 
        /// </summary> 
        /// <param name="srcToComapre"></param>
        /// <returns></returns> 
        internal override bool NameExist(string nameToCheck) {
            Debug.Assert(StringUtil.NotEmptyAfterTrim(nameToCheck));
                return StringUtil.EqualValue(this.FillMethodName, nameToCheck, true /*CaseInsensitive*/) ||
                       StringUtil.EqualValue(this.GetMethodName, nameToCheck, true /*CaseInsensitive*/); 
        }
 
 
        public override object Clone() {
            DbSource dbs = new DbSource(); 

            if( this.connection != null ) {
                dbs.connection = (DesignConnection) this.connection.Clone();
            } 

            if( this.selectCommand != null ) { 
                dbs.selectCommand = (DbSourceCommand) this.selectCommand.Clone(); 
                dbs.selectCommand.SetParent(dbs);
            } 

            if( this.insertCommand != null ) {
                dbs.insertCommand = (DbSourceCommand) this.insertCommand.Clone();
                dbs.insertCommand.SetParent(dbs); 
            }
 
            if( this.updateCommand != null ) { 
                dbs.updateCommand = (DbSourceCommand) this.updateCommand.Clone();
                dbs.updateCommand.SetParent(dbs); 
            }

            if( this.deleteCommand != null ) {
                dbs.deleteCommand = (DbSourceCommand) this.deleteCommand.Clone(); 
                dbs.deleteCommand.SetParent(dbs);
            } 
 
            dbs.Name = this.Name;
            dbs.Modifier = this.Modifier; 
            dbs.scalarCallRetval = this.scalarCallRetval;
            dbs.generateMethods = this.generateMethods;
            dbs.queryType = this.queryType;
            dbs.getMethodModifier = this.getMethodModifier; 
            dbs.getMethodName = this.getMethodName;
            dbs.generatePagingMethods = this.generatePagingMethods; 
            return dbs; 
        }
 
        [DataSourceXmlAttribute()]
        public bool GenerateShortCommands {
            get {
                return generateShortCommands; 
            }
            set { 
                generateShortCommands = value; 
            }
        } 

        internal DbSourceCommand GetActiveCommand() {
            CommandOperation operation = this.CommandOperation;
            switch (operation) { 
                case CommandOperation.Select:
                    return this.SelectCommand; 
 
                case CommandOperation.Insert:
                    return this.InsertCommand; 

                case CommandOperation.Update:
                    return this.UpdateCommand;
 
                case CommandOperation.Delete:
                    return this.DeleteCommand; 
 
                default:
                    return null; 
            }
        }

 
        void IDataSourceXmlSpecialOwner.ReadSpecialItem(string propertyName, XmlNode xmlNode, DataSourceXmlSerializer serializer) {
            if (propertyName.Equals("ScalarCallRetval")) { 
                this.scalarCallRetval = typeof(object); 

                if( StringUtil.NotEmptyAfterTrim(xmlNode.InnerText) ) { 
                    this.scalarCallRetval = Type.GetType( xmlNode.InnerText, false );
                }
            }
        } 

 
        void IDataSourceXmlSpecialOwner.WriteSpecialItem(string propertyName, XmlWriter writer, DataSourceXmlSerializer serializer) { 
            if (propertyName.Equals("ScalarCallRetval")) {
                Debug.Assert( this.scalarCallRetval != null ); 

                writer.WriteString( this.scalarCallRetval.AssemblyQualifiedName );
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
    using System.Data.Common; 
    using System.Diagnostics; 
    using System.Globalization;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Windows.Forms;
    using System.Xml;
    using System.Xml.Schema; 
    using System.Xml.Serialization;
 
 

 
    internal enum QueryType {
        Rowset,
        Scalar,
        NoData 
    }
 
    [Flags] 
    internal enum GenerateMethodTypes {
        Fill = 1, 
        Get = 2,
        Both = Fill + Get,
    }
 
    [
        DataSourceXmlClass(SchemaName.DbSource) 
    ] 
    internal class DbSource: Source, IDataSourceXmlSpecialOwner {
        private IDesignConnection connection; 
        private DbSourceCommand selectCommand;
        private DbSourceCommand insertCommand;
        private DbSourceCommand updateCommand;
        private DbSourceCommand deleteCommand; 
        private DbObjectType dbObjectType = DbObjectType.Unknown;
        private string connectionRef; 
        private Type scalarCallRetval = typeof(object); 
        private string userGetMethodName;
        private string getMethodName;// = INSTANCE_NAME_FOR_GETMETHOD_MAIN; 
        private MemberAttributes getMethodModifier = MemberAttributes.Public; // undone: review the default value for modifier
        private QueryType queryType = QueryType.Rowset;
        private GenerateMethodTypes generateMethods = GenerateMethodTypes.Both;
        private bool generatePagingMethods = false; 
        private bool generateShortCommands = true;
        private bool useOptimisticConcurrency = true; 
 
        private TypeEnum parameterType = TypeEnum.CLR;
 
        internal const string TYPE_NAME_FOR_QUERY = "Query";
        internal const string TYPE_NAME_FOR_FUNCTION = "Query";
        private  const string PROPERTY_COMMANDTEXT = "CommandText";
 
        internal const string INSTANCE_NAME_FOR_FILLMETHOD_MAIN = "Fill";
        internal const string INSTANCE_NAME_FOR_GETMETHOD_MAIN = "GetData"; 
        internal const string INSTANCE_NAME_FOR_FILLMETHOD = "FillBy"; 
        internal const string INSTANCE_NAME_FOR_GETMETHOD = "GetDataBy";
        internal const string INSTANCE_NAME_FOR_FUNCTION = "Query"; 

        public DbSource() { }

        //public DbSource(string name){ 
        //    this.Name = name;
        //} 
 
        protected internal override DataSourceCollectionBase CollectionParent {
            get { 
                if (base.CollectionParent != null) {
                    return base.CollectionParent;
                }
                if (this.owner != null && owner is DesignTable && ((DesignTable)this.owner).MainSource == this){ 
                    return ((DesignTable)this.owner).Sources;
                } 
                return null; 
            }
        } 


        [
            Browsable(false), 
            DataSourceXmlAttribute(),
        ] 
        public string ConnectionRef { 
            get {
                if (this.connection != null) { 
                    return this.connection.Name;
                }
                return connectionRef;
            } 
            set {
                connectionRef = value; 
            } 
        }
 


        [
            Browsable(false), 
            DataSourceXmlAttribute(SpecialWay=true)
        ] 
        public Type ScalarCallRetval { 
            get {
                return this.scalarCallRetval; 
            }
        }

        [ 
            DefaultValue(null),
            RefreshProperties(RefreshProperties.All) 
        ] 
        public IDesignConnection Connection {
            get { 
                return this.connection;
            }
            set {
                this.connection = value; 
            }
        } 
 

 
        [
            DefaultValue(TypeEnum.CLR),
            DataSourceXmlAttribute()
        ] 
        public TypeEnum MethodsParameterType {
            get { 
                return parameterType; 
            }
            set { 
                parameterType = value;
            }
        }
 
        public CommandOperation CommandOperation {
            get { 
                // Order is important here. 
                if (this.SelectCommand != null) {
                    return CommandOperation.Select; 
                }

                // If this source does not have a SELECT command, it may only have one of the three remaining commands.
                if (this.InsertCommand != null) { 
                    Debug.Assert(this.UpdateCommand == null && this.DeleteCommand == null);
                    return CommandOperation.Insert; 
                } 

                if (this.UpdateCommand != null) { 
                    Debug.Assert(this.DeleteCommand == null);
                    return CommandOperation.Update;
                }
 
                if (this.DeleteCommand != null) {
                    return CommandOperation.Delete; 
                } 

                return CommandOperation.Unknown; 
            }
        }

        #region Modifiers 
        [
            DefaultValue(MemberAttributes.Public), 
            DataSourceXmlAttribute() 
        ]
        public MemberAttributes FillMethodModifier { 
            get {
                return this.Modifier;
            }
            set { 
                this.Modifier = value;
            } 
        } 

        [ 
            DefaultValue(MemberAttributes.Public),
            DataSourceXmlAttribute()
        ]
        public MemberAttributes GetMethodModifier { 
            get {
                return this.getMethodModifier; 
            } 
            set {
                this.getMethodModifier = value; 
            }
        }
        #endregion
 

        #region Names 
        /// <summary> 
        /// </summary>
        [ 
            Browsable(false),
        ]
        public override string Name {
            get { 
                if (StringUtil.Empty(base.Name)) {
                    if (this.generateMethods == GenerateMethodTypes.Get) { 
                        return this.GetMethodName; 
                    }
                } 
                return base.Name;
            }
            set {
                if (name != value) { 
                    base.name = value;
                    SourceCollection sc = this.CollectionParent as SourceCollection; 
                    if (sc != null) { 
                        sc.ValidateUniqueDbSourceName(this, value, true);
                    } 
                }
            }
        }
 
        /// <summary>
        /// </summary> 
        [ 
            DataSourceXmlAttribute (),
            DefaultValue(INSTANCE_NAME_FOR_FILLMETHOD_MAIN) 
        ]
        public string FillMethodName {
            get {
                return this.Name; 
            }
            set { 
                this.Name = value; 
            }
        } 


        /// <summary>
        /// </summary> 
        [
            DefaultValue(INSTANCE_NAME_FOR_GETMETHOD_MAIN), 
            DataSourceXmlAttribute() 
        ]
        public string GetMethodName { 
            get {
                if (StringUtil.EmptyOrSpace(this.getMethodName) && this.CollectionParent != null) {
                    if (this.IsMainSource) {
                        this.GetMethodName = DbSource.INSTANCE_NAME_FOR_GETMETHOD_MAIN; 
                    }
                    else { 
                        this.GetMethodName = DbSource.INSTANCE_NAME_FOR_GETMETHOD; 
                    }
                } 

                return this.getMethodName;
            }
            set { 
                getMethodName = value;
            } 
        } 

        [ 
            DataSourceXmlAttribute()
        ]
        public string UserGetMethodName {
            get { 
                return this.userGetMethodName;
            } 
            set { 
                this.userGetMethodName = value;
            } 
        }

        #endregion
 

        /// <summary> 
        /// </summary> 
        [
            DefaultValue(GenerateMethodTypes.Both), 
            DataSourceXmlAttribute(),
            RefreshProperties(RefreshProperties.All)
        ]
        public GenerateMethodTypes GenerateMethods { 
            get {
                return this.generateMethods; 
            } 
            set {
                this.generateMethods = value; 
            }
        }

        /// <summary> 
        /// </summary>
        [ 
            DefaultValue(true), 
            DataSourceXmlAttribute()
        ] 
        public bool GeneratePagingMethods {
            get {
                return this.generatePagingMethods;
            } 
            set {
                this.generatePagingMethods = value; 
            } 
        }
 
        [Browsable(false)]
        public override object Parent {
            get {
                if (base.Parent != null) { 
                    return base.Parent;
                } 
                return this.Owner; 
            }
        } 

        [Browsable(false)]
        public override string PublicTypeName {
            get { 
                if( this.Owner is DesignTable ) {
                    return DbSource.TYPE_NAME_FOR_QUERY; 
                } 
                else {
                    return DbSource.TYPE_NAME_FOR_FUNCTION; 
                }
            }
        }
 
        [
            DataSourceXmlAttribute(), 
            Browsable(false), 
        ]
        public QueryType QueryType { 
            get {
                return queryType;
            }
            set { 
                queryType = value;
                if (queryType != QueryType.Rowset) { 
                    this.GenerateMethods = GenerateMethodTypes.Fill; 
                }
            } 
        }

        #region Commands
        [ 
            DataSourceXmlSubItem(Name="SelectCommand", ItemType=typeof(DbSourceCommand)),
            Browsable(false), 
        ] 
        public DbSourceCommand SelectCommand {
            get { 
                return this.selectCommand;
            }
            set {
                if (this.selectCommand != null){ 
                    this.selectCommand.SetParent(null);
                } 
                this.selectCommand = value; 
                if (this.selectCommand != null){
                    this.selectCommand.SetParent(this); 
                    this.selectCommand.CommandOperation = CommandOperation.Select;
                }
            }
        } 

        [ 
            DataSourceXmlSubItem(Name="UpdateCommand", ItemType=typeof(DbSourceCommand)), 
            Browsable(false),
        ] 
        public DbSourceCommand UpdateCommand {
            get {
                return this.updateCommand;
            } 
            set {
                if (this.updateCommand != null){ 
                    this.updateCommand.SetParent(null); 
                }
                this.updateCommand = value; 
                if (this.updateCommand != null){
                    this.updateCommand.SetParent(this);
                    this.updateCommand.CommandOperation = CommandOperation.Update;
                } 
            }
        } 
 
        [DataSourceXmlSubItem(Name = "DeleteCommand", ItemType = typeof(DbSourceCommand)), Browsable(false),]
        public DbSourceCommand DeleteCommand { 
            get {
                return this.deleteCommand;
            }
            set { 
                if (this.deleteCommand != null) {
                    this.deleteCommand.SetParent(null); 
                } 

                this.deleteCommand = value; 
                if (this.deleteCommand != null) {
                    this.deleteCommand.SetParent(this);
                    this.deleteCommand.CommandOperation = CommandOperation.Delete;
                } 
            }
        } 
 
        [DataSourceXmlSubItem(Name = "InsertCommand", ItemType = typeof(DbSourceCommand)), Browsable(false),]
        public DbSourceCommand InsertCommand { 
            get {
                return this.insertCommand;
            }
            set { 
                if (this.insertCommand != null) {
                    this.insertCommand.SetParent(null); 
                } 

                this.insertCommand = value; 
                if (this.insertCommand != null) {
                    this.insertCommand.SetParent(this);
                    this.insertCommand.CommandOperation = CommandOperation.Insert;
                } 
            }
        } 
 
        #endregion
 

        [DataSourceXmlAttribute()]
        public DbObjectType DbObjectType {
            get { 
                return this.dbObjectType;
            } 
            set { 
                this.dbObjectType = value;
            } 
        }

        [DataSourceXmlAttribute()]
        public bool UseOptimisticConcurrency { 
            get {
                return this.useOptimisticConcurrency; 
            } 

            set { 
                this.useOptimisticConcurrency = value;
            }
        }
 
        /// <summary>
        /// Compare the source by name 
        /// </summary> 
        /// <param name="srcToComapre"></param>
        /// <returns></returns> 
        internal override bool NameExist(string nameToCheck) {
            Debug.Assert(StringUtil.NotEmptyAfterTrim(nameToCheck));
                return StringUtil.EqualValue(this.FillMethodName, nameToCheck, true /*CaseInsensitive*/) ||
                       StringUtil.EqualValue(this.GetMethodName, nameToCheck, true /*CaseInsensitive*/); 
        }
 
 
        public override object Clone() {
            DbSource dbs = new DbSource(); 

            if( this.connection != null ) {
                dbs.connection = (DesignConnection) this.connection.Clone();
            } 

            if( this.selectCommand != null ) { 
                dbs.selectCommand = (DbSourceCommand) this.selectCommand.Clone(); 
                dbs.selectCommand.SetParent(dbs);
            } 

            if( this.insertCommand != null ) {
                dbs.insertCommand = (DbSourceCommand) this.insertCommand.Clone();
                dbs.insertCommand.SetParent(dbs); 
            }
 
            if( this.updateCommand != null ) { 
                dbs.updateCommand = (DbSourceCommand) this.updateCommand.Clone();
                dbs.updateCommand.SetParent(dbs); 
            }

            if( this.deleteCommand != null ) {
                dbs.deleteCommand = (DbSourceCommand) this.deleteCommand.Clone(); 
                dbs.deleteCommand.SetParent(dbs);
            } 
 
            dbs.Name = this.Name;
            dbs.Modifier = this.Modifier; 
            dbs.scalarCallRetval = this.scalarCallRetval;
            dbs.generateMethods = this.generateMethods;
            dbs.queryType = this.queryType;
            dbs.getMethodModifier = this.getMethodModifier; 
            dbs.getMethodName = this.getMethodName;
            dbs.generatePagingMethods = this.generatePagingMethods; 
            return dbs; 
        }
 
        [DataSourceXmlAttribute()]
        public bool GenerateShortCommands {
            get {
                return generateShortCommands; 
            }
            set { 
                generateShortCommands = value; 
            }
        } 

        internal DbSourceCommand GetActiveCommand() {
            CommandOperation operation = this.CommandOperation;
            switch (operation) { 
                case CommandOperation.Select:
                    return this.SelectCommand; 
 
                case CommandOperation.Insert:
                    return this.InsertCommand; 

                case CommandOperation.Update:
                    return this.UpdateCommand;
 
                case CommandOperation.Delete:
                    return this.DeleteCommand; 
 
                default:
                    return null; 
            }
        }

 
        void IDataSourceXmlSpecialOwner.ReadSpecialItem(string propertyName, XmlNode xmlNode, DataSourceXmlSerializer serializer) {
            if (propertyName.Equals("ScalarCallRetval")) { 
                this.scalarCallRetval = typeof(object); 

                if( StringUtil.NotEmptyAfterTrim(xmlNode.InnerText) ) { 
                    this.scalarCallRetval = Type.GetType( xmlNode.InnerText, false );
                }
            }
        } 

 
        void IDataSourceXmlSpecialOwner.WriteSpecialItem(string propertyName, XmlWriter writer, DataSourceXmlSerializer serializer) { 
            if (propertyName.Equals("ScalarCallRetval")) {
                Debug.Assert( this.scalarCallRetval != null ); 

                writer.WriteString( this.scalarCallRetval.AssemblyQualifiedName );
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
