///------------------------------------------------------------------------------ 
// <copyright from='1997' to='2004' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
using System; 
using System.Data;
using System.Data.Common; 
using System.Design;
using System.ComponentModel;
using System.CodeDom;
using System.Xml; 
using System.Xml.Schema;
using System.Xml.Serialization; 
using System.Collections; 
using System.Collections.Specialized;
using System.IO; 
using System.Globalization;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text; 

 
namespace System.Data.Design { 

    internal interface IDesignConnection : IDataSourceNamedObject, ICloneable, IDataSourceInitAfterLoading, IDataSourceXmlSpecialOwner { 
        ConnectionString ConnectionStringObject { get; set; }
        string ConnectionString { get; set; }
        string Provider { get; set; }
        bool IsAppSettingsProperty { get; set; } 
        string AppSettingsObjectName { get; set; }
        CodePropertyReferenceExpression PropertyReference { get; set; } 
        IDictionary Properties { get; } 
        IDbConnection CreateEmptyDbConnection();
    } 


    [
        DataSourceXmlClass (SchemaName.Connection) 
    ]
    internal class DesignConnection: DataSourceComponent, IDesignConnection, IDataSourceCollectionMember { 
        private string name; 
        private ConnectionString connectionStringObject;
        private string connectionStringValue; 
        private string provider;
        private bool isAppSettingsProperty = false;
        private string appSettingsObjectName;
        private CodePropertyReferenceExpression propertyReference; 
        private HybridDictionary properties = new HybridDictionary();
        private MemberAttributes modifier = MemberAttributes.Assembly; 
 
        private static readonly string regexAlphaCharacter = @"[\p{L}\p{Nl}]";
        private static readonly string regexUnderscoreCharacter = @"\p{Pc}"; 
        private static readonly string regexIdentifierCharacter = @"[\p{L}\p{Nl}\p{Nd}\p{Mn}\p{Mc}\p{Cf}]";
        private static readonly string regexIdentifierStart = "(" + regexAlphaCharacter + "|(" + regexUnderscoreCharacter + regexIdentifierCharacter + "))";
        private static readonly string regexIdentifier = regexIdentifierStart + regexIdentifierCharacter + "*";
 
        private string parameterPrefix = null;
 
        public DesignConnection() { } 

        public DesignConnection( string connectionName, ConnectionString cs, string provider ) { 
            this.name = connectionName;
            this.connectionStringObject = cs;
            this.provider = provider;
        } 

        public DesignConnection( string connectionName, IDbConnection conn ) { 
            if( conn == null ) { 
                throw new ArgumentNullException("conn");
            } 

            this.name = connectionName;
            DbProviderFactory factory = ProviderManager.GetFactoryFromType( conn.GetType(), ProviderManager.ProviderSupportedClasses.DbConnection );
            this.provider = ProviderManager.GetInvariantProviderName( factory ); 
            this.connectionStringObject = new ConnectionString( this.provider, conn.ConnectionString );
        } 
 

        internal static string ConnectionNameRegex { 
            get {
                return DesignConnection.regexIdentifier;
            }
        } 

        [ 
            DefaultValue (MemberAttributes.Assembly), 
            DataSourceXmlAttribute ()
        ] 
        public MemberAttributes Modifier {
            get {
                return modifier;
            } 
            set {
                modifier = value; 
            } 
        }
 
        [
            DataSourceXmlAttribute()
        ]
        public string Name { 
            get {
                return this.name; 
            } 
            set {
                if (this.name != value) { 
                    if (this.CollectionParent != null) {
                        CollectionParent.ValidateUniqueName(this, value);
                    }
                    this.name = value; 
                }
            } 
        } 

 
        [DataSourceXmlAttribute(SpecialWay = true), Browsable(false)]
        public ConnectionString ConnectionStringObject {
            get {
                return this.connectionStringObject; 
            }
            set { 
                ConnectionString oldConnectionString = this.connectionStringObject; 

                this.connectionStringObject = value; 
            }
        }

        public string ConnectionString { 
            get {
                if (ConnectionStringObject != null) { 
                    return this.ConnectionStringObject.ToString (); // we can customize this string by calling ToStringUsing(...) if needed 
                }
                return string.Empty; 
            }
            set {
                Debug.Assert(this.ConnectionStringObject != null," Don't know how to set the connection string");
                if (this.ConnectionStringObject != null) { 
                    this.ConnectionStringObject = new ConnectionString( this.provider, value );
                } 
            } 
        }
 
        [DataSourceXmlAttribute(), Browsable (false)]
        public string Provider {
            get {
                return this.provider; 
            }
            set { 
                this.provider = value; 
            }
        } 

        [DataSourceXmlAttribute(), Browsable(false)]
        public bool IsAppSettingsProperty {
            get { 
                return this.isAppSettingsProperty;
            } 
            set { 
                this.isAppSettingsProperty = value;
            } 
        }

        [DataSourceXmlAttribute(), Browsable(false)]
        public string AppSettingsObjectName { 
            get {
                return this.appSettingsObjectName; 
            } 
            set {
                this.appSettingsObjectName = value; 
            }
        }

        [DataSourceXmlAttribute(SpecialWay = true), Browsable(false)] 
        public CodePropertyReferenceExpression PropertyReference {
            get { 
                return this.propertyReference; 
            }
            set { 
                this.propertyReference = value;
            }
        }
 
        [DataSourceXmlAttribute(), Browsable(false)]
        public string ParameterPrefix { 
            get { 
                return this.parameterPrefix;
            } 
            set {
                this.parameterPrefix = value;
            }
        } 

        [Browsable(false)] 
        public IDictionary Properties { 
            get {
                return this.properties; 
            }
        }

        [Browsable(false)] 
        public string PublicTypeName {
            get { 
                return "Connection"; 
            }
        } 



        public IDbConnection CreateEmptyDbConnection() { 
            DbProviderFactory factory = ProviderManager.GetFactory( this.provider );
            return factory.CreateConnection(); 
        } 

 


        public object Clone() {
            DesignConnection clone = new DesignConnection(); 

            clone.Name = this.name; 
            if (this.ConnectionStringObject != null){ 
                clone.ConnectionStringObject = (ConnectionString) ((ICloneable) this.ConnectionStringObject).Clone();
            } 
            clone.provider = this.provider;
            clone.isAppSettingsProperty = this.isAppSettingsProperty;
            clone.propertyReference = this.propertyReference;
            clone.properties = (HybridDictionary) DesignUtil.CloneDictionary( this.properties ); 

            return clone; 
        } 

 
        void IDataSourceInitAfterLoading.InitializeAfterLoading() {
            if (name == null || name.Length == 0) {
                throw new DataSourceSerializationException( SR.GetString( SR.DTDS_NameIsRequired, "Connection") );
            } 

            if (StringUtil.EmptyOrSpace(this.provider)) { 
                throw new DataSourceSerializationException( SR.GetString( SR.DTDS_CouldNotDeserializeConnection ) ); 
            }
 
            if (connectionStringValue != null) {
                this.ConnectionStringObject = new ConnectionString( this.provider, this.connectionStringValue );
            }
 
            this.properties.Clear();
        } 
 
        void IDataSourceXmlSpecialOwner.ReadSpecialItem(string propertyName, XmlNode xmlNode, DataSourceXmlSerializer serializer) {
            if (propertyName == "ConnectionStringObject") { 
                this.connectionStringValue = xmlNode.InnerText;
            }
            else if (propertyName == "PropertyReference") {
                this.propertyReference = PropertyReferenceSerializer.Deserialize(xmlNode.InnerText); 
            }
        } 
 
        void IDataSourceXmlSpecialOwner.WriteSpecialItem(string propertyName, XmlWriter writer, DataSourceXmlSerializer serializer) {
            if (propertyName == "ConnectionStringObject") { 
                writer.WriteString(this.ConnectionStringObject.ToFullString());
            }
            else if (propertyName == "PropertyReference") {
                writer.WriteString(PropertyReferenceSerializer.Serialize(this.PropertyReference)); 
            }
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
///------------------------------------------------------------------------------ 
// <copyright from='1997' to='2004' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
using System; 
using System.Data;
using System.Data.Common; 
using System.Design;
using System.ComponentModel;
using System.CodeDom;
using System.Xml; 
using System.Xml.Schema;
using System.Xml.Serialization; 
using System.Collections; 
using System.Collections.Specialized;
using System.IO; 
using System.Globalization;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text; 

 
namespace System.Data.Design { 

    internal interface IDesignConnection : IDataSourceNamedObject, ICloneable, IDataSourceInitAfterLoading, IDataSourceXmlSpecialOwner { 
        ConnectionString ConnectionStringObject { get; set; }
        string ConnectionString { get; set; }
        string Provider { get; set; }
        bool IsAppSettingsProperty { get; set; } 
        string AppSettingsObjectName { get; set; }
        CodePropertyReferenceExpression PropertyReference { get; set; } 
        IDictionary Properties { get; } 
        IDbConnection CreateEmptyDbConnection();
    } 


    [
        DataSourceXmlClass (SchemaName.Connection) 
    ]
    internal class DesignConnection: DataSourceComponent, IDesignConnection, IDataSourceCollectionMember { 
        private string name; 
        private ConnectionString connectionStringObject;
        private string connectionStringValue; 
        private string provider;
        private bool isAppSettingsProperty = false;
        private string appSettingsObjectName;
        private CodePropertyReferenceExpression propertyReference; 
        private HybridDictionary properties = new HybridDictionary();
        private MemberAttributes modifier = MemberAttributes.Assembly; 
 
        private static readonly string regexAlphaCharacter = @"[\p{L}\p{Nl}]";
        private static readonly string regexUnderscoreCharacter = @"\p{Pc}"; 
        private static readonly string regexIdentifierCharacter = @"[\p{L}\p{Nl}\p{Nd}\p{Mn}\p{Mc}\p{Cf}]";
        private static readonly string regexIdentifierStart = "(" + regexAlphaCharacter + "|(" + regexUnderscoreCharacter + regexIdentifierCharacter + "))";
        private static readonly string regexIdentifier = regexIdentifierStart + regexIdentifierCharacter + "*";
 
        private string parameterPrefix = null;
 
        public DesignConnection() { } 

        public DesignConnection( string connectionName, ConnectionString cs, string provider ) { 
            this.name = connectionName;
            this.connectionStringObject = cs;
            this.provider = provider;
        } 

        public DesignConnection( string connectionName, IDbConnection conn ) { 
            if( conn == null ) { 
                throw new ArgumentNullException("conn");
            } 

            this.name = connectionName;
            DbProviderFactory factory = ProviderManager.GetFactoryFromType( conn.GetType(), ProviderManager.ProviderSupportedClasses.DbConnection );
            this.provider = ProviderManager.GetInvariantProviderName( factory ); 
            this.connectionStringObject = new ConnectionString( this.provider, conn.ConnectionString );
        } 
 

        internal static string ConnectionNameRegex { 
            get {
                return DesignConnection.regexIdentifier;
            }
        } 

        [ 
            DefaultValue (MemberAttributes.Assembly), 
            DataSourceXmlAttribute ()
        ] 
        public MemberAttributes Modifier {
            get {
                return modifier;
            } 
            set {
                modifier = value; 
            } 
        }
 
        [
            DataSourceXmlAttribute()
        ]
        public string Name { 
            get {
                return this.name; 
            } 
            set {
                if (this.name != value) { 
                    if (this.CollectionParent != null) {
                        CollectionParent.ValidateUniqueName(this, value);
                    }
                    this.name = value; 
                }
            } 
        } 

 
        [DataSourceXmlAttribute(SpecialWay = true), Browsable(false)]
        public ConnectionString ConnectionStringObject {
            get {
                return this.connectionStringObject; 
            }
            set { 
                ConnectionString oldConnectionString = this.connectionStringObject; 

                this.connectionStringObject = value; 
            }
        }

        public string ConnectionString { 
            get {
                if (ConnectionStringObject != null) { 
                    return this.ConnectionStringObject.ToString (); // we can customize this string by calling ToStringUsing(...) if needed 
                }
                return string.Empty; 
            }
            set {
                Debug.Assert(this.ConnectionStringObject != null," Don't know how to set the connection string");
                if (this.ConnectionStringObject != null) { 
                    this.ConnectionStringObject = new ConnectionString( this.provider, value );
                } 
            } 
        }
 
        [DataSourceXmlAttribute(), Browsable (false)]
        public string Provider {
            get {
                return this.provider; 
            }
            set { 
                this.provider = value; 
            }
        } 

        [DataSourceXmlAttribute(), Browsable(false)]
        public bool IsAppSettingsProperty {
            get { 
                return this.isAppSettingsProperty;
            } 
            set { 
                this.isAppSettingsProperty = value;
            } 
        }

        [DataSourceXmlAttribute(), Browsable(false)]
        public string AppSettingsObjectName { 
            get {
                return this.appSettingsObjectName; 
            } 
            set {
                this.appSettingsObjectName = value; 
            }
        }

        [DataSourceXmlAttribute(SpecialWay = true), Browsable(false)] 
        public CodePropertyReferenceExpression PropertyReference {
            get { 
                return this.propertyReference; 
            }
            set { 
                this.propertyReference = value;
            }
        }
 
        [DataSourceXmlAttribute(), Browsable(false)]
        public string ParameterPrefix { 
            get { 
                return this.parameterPrefix;
            } 
            set {
                this.parameterPrefix = value;
            }
        } 

        [Browsable(false)] 
        public IDictionary Properties { 
            get {
                return this.properties; 
            }
        }

        [Browsable(false)] 
        public string PublicTypeName {
            get { 
                return "Connection"; 
            }
        } 



        public IDbConnection CreateEmptyDbConnection() { 
            DbProviderFactory factory = ProviderManager.GetFactory( this.provider );
            return factory.CreateConnection(); 
        } 

 


        public object Clone() {
            DesignConnection clone = new DesignConnection(); 

            clone.Name = this.name; 
            if (this.ConnectionStringObject != null){ 
                clone.ConnectionStringObject = (ConnectionString) ((ICloneable) this.ConnectionStringObject).Clone();
            } 
            clone.provider = this.provider;
            clone.isAppSettingsProperty = this.isAppSettingsProperty;
            clone.propertyReference = this.propertyReference;
            clone.properties = (HybridDictionary) DesignUtil.CloneDictionary( this.properties ); 

            return clone; 
        } 

 
        void IDataSourceInitAfterLoading.InitializeAfterLoading() {
            if (name == null || name.Length == 0) {
                throw new DataSourceSerializationException( SR.GetString( SR.DTDS_NameIsRequired, "Connection") );
            } 

            if (StringUtil.EmptyOrSpace(this.provider)) { 
                throw new DataSourceSerializationException( SR.GetString( SR.DTDS_CouldNotDeserializeConnection ) ); 
            }
 
            if (connectionStringValue != null) {
                this.ConnectionStringObject = new ConnectionString( this.provider, this.connectionStringValue );
            }
 
            this.properties.Clear();
        } 
 
        void IDataSourceXmlSpecialOwner.ReadSpecialItem(string propertyName, XmlNode xmlNode, DataSourceXmlSerializer serializer) {
            if (propertyName == "ConnectionStringObject") { 
                this.connectionStringValue = xmlNode.InnerText;
            }
            else if (propertyName == "PropertyReference") {
                this.propertyReference = PropertyReferenceSerializer.Deserialize(xmlNode.InnerText); 
            }
        } 
 
        void IDataSourceXmlSpecialOwner.WriteSpecialItem(string propertyName, XmlWriter writer, DataSourceXmlSerializer serializer) {
            if (propertyName == "ConnectionStringObject") { 
                writer.WriteString(this.ConnectionStringObject.ToFullString());
            }
            else if (propertyName == "PropertyReference") {
                writer.WriteString(PropertyReferenceSerializer.Serialize(this.PropertyReference)); 
            }
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
