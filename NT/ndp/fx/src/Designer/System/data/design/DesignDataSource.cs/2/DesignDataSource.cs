 
//#define LoadByDataSourceOptimizedXmlSerializer
//#define perfcounter
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'> 
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright> 
//-----------------------------------------------------------------------------
namespace System.Data.Design{ 

    using System;
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design; 
    using System.Diagnostics;
    using System.Data; 
    using System.Data.OleDb;
    using System.Data.Common;
    using System.IO;
    using System.Globalization; 
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Reflection; 
    using System.Xml; 
    using System.Windows.Forms;
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Threading;

 
    /// <summary>
    /// DataSource is the so-called design time data source class intended to be used 
    /// by DataSourceDesigner, DataSourceCodeGeneration, DataService and other Data 
    /// specific tasks.
    /// DataSource is defined as Component because we want to use it as the root component in 
    /// the DataSource designer
    /// </summary>
    [
        DataSourceXmlClass(SchemaName.DataSourceRoot) 
    ]
    internal class DesignDataSource : DataSourceComponent, IDataSourceNamedObject, IDataSourceCommandTarget { 
        private DataSet dataSet; 
        private DesignTableCollection designTables;
        private DesignRelationCollection designRelations; 
        private DesignConnectionCollection designConnections;
        private int defaultConnectionIndex;
        private SourceCollection sources;
        private TypeAttributes modifier = TypeAttributes.Public; 
        private SchemaSerializationMode schemaSerializationMode = SchemaSerializationMode.IncludeSchema;
 
#if LoadByDataSourceOptimizedXmlSerializer 
        private DataSourceOptimizedXmlSerializer serializer;
#else 
        private DataSourceXmlSerializer serializer;
#endif

        private  StringCollection namingPropNames = new StringCollection(); 
        internal static string EXTPROPNAME_USER_DATASETNAME = "Generator_UserDSName";
        internal static string EXTPROPNAME_GENERATOR_DATASETNAME = "Generator_DataSetName"; 
        private const string EXTPROPNAME_ENABLE_TABLEADAPTERMANAGER = "EnableTableAdapterManager"; 
        private string functionsComponentName = null;
        private string userFunctionsComponentName = null; 
        private string generatorFunctionsComponentClassName = null;


        /// <summary> 
        /// This event encapsulates all data change events including property change events and collection change events.
        /// </summary> 
        /// <value></value> 

        /// <summary> 
        /// </summary>
        internal DataSet DataSet{
            get{
                if (dataSet == null) { 
                    dataSet = new DataSet();
                    dataSet.Locale = System.Globalization.CultureInfo.InvariantCulture; 
                    // VS Whidbey Bug 194886 
                    dataSet.EnforceConstraints = false;
                } 
                return dataSet;
            }
        }
 
        /// <summary>
        /// </summary> 
        // NOTE: as we save DefaultConnection as an Index, any ConnectionEditor should maintain it if it modified the DesignConnection collection 
        //   except operation only adds a new connection to the end of the collection.
        [ 
 			DisplayName ("DefaultConnection"),
        ]
        public DesignConnection DefaultConnection {
            get { 
                if (DesignConnections.Count > 0) {
                    if (defaultConnectionIndex >= 0 && defaultConnectionIndex < DesignConnections.Count) { 
                        return ((IList)DesignConnections)[defaultConnectionIndex] as DesignConnection; 
                    }
                } 

                return null;
            }
        } 

        /// <summary> 
        /// </summary> 
        [
            DisplayName("Connections"), 
            DataSourceXmlSubItem(Name="Connections", ItemType=typeof(DesignConnection)),
            Browsable(false)
        ]
        public DesignConnectionCollection DesignConnections { 
            get{
                if (designConnections == null){ 
                    designConnections = new DesignConnectionCollection(this); 
                }
                return designConnections; 
            }
        }

        /// <summary> 
        /// </summary>
        [Browsable(false)] 
        public DesignRelationCollection DesignRelations { 
            get{
                if (designRelations == null){ 
                    designRelations = new DesignRelationCollection(this);
                }
                return designRelations;
            } 
        }
 
        /// <summary> 
        /// </summary>
        [ 
            DataSourceXmlSubItem(Name="Tables", ItemType=typeof(DesignConnection)),
            Browsable(false),
        ]
        public DesignTableCollection DesignTables { 
            get{
                if (designTables == null){ 
                    designTables = new DesignTableCollection(this); 
                }
                return designTables; 
            }
        }

        [DefaultValue(true)] 
        public bool EnableTableAdapterManager {
            get { 
                bool result = false; 
                Boolean.TryParse(this.DataSet.ExtendedProperties[EXTPROPNAME_ENABLE_TABLEADAPTERMANAGER] as string, out result);
                return result; 
            }
            set {
                this.DataSet.ExtendedProperties[EXTPROPNAME_ENABLE_TABLEADAPTERMANAGER] = value.ToString();
            } 
        }
 
        /// <summary> 
        /// Dataset class modifier
        /// </summary> 
        [
            DefaultValue(TypeAttributes.Public),
            DataSourceXmlAttribute()
        ] 
        public TypeAttributes Modifier {
            get { 
                return modifier; 
            }
            set { 
                modifier = value;
            }
        }
 

        [ 
            MergableProperty(false), 
            DefaultValue("")
        ] 
        public string Name {
            get {
                return this.DataSet.DataSetName;
            } 
            set {
                this.DataSet.DataSetName = value; 
            } 
        }
 


        [Browsable(false)]
        public string PublicTypeName { 
            get {
                return "DataSet"; 
            } 
        }
 
        /// <summary>
        /// </summary>
        [
            DataSourceXmlSubItem(typeof(Source)), 
            Browsable(false),
        ] 
        public SourceCollection Sources{ 
            get{
                if (sources == null){ 
                    sources = new SourceCollection(this);
                }
                return sources;
            } 
        }
 
        [DataSourceXmlAttribute()] 
        public SchemaSerializationMode SchemaSerializationMode {
            get { 
                return schemaSerializationMode;
            }
            set {
                schemaSerializationMode = value; 
            }
        } 
 
        internal string UserDataSetName {
            get { 
                return this.DataSet.ExtendedProperties[EXTPROPNAME_USER_DATASETNAME] as string;
            }
            set {
                this.DataSet.ExtendedProperties[EXTPROPNAME_USER_DATASETNAME] = value; 
            }
        } 
 
        internal string GeneratorDataSetName {
            get { 
                return this.DataSet.ExtendedProperties[EXTPROPNAME_GENERATOR_DATASETNAME] as string;
            }
            set {
                this.DataSet.ExtendedProperties[EXTPROPNAME_GENERATOR_DATASETNAME] = value; 
            }
        } 
 
        [DataSourceXmlAttribute(), Browsable(false), DefaultValue(null)]
        public string FunctionsComponentName { 
            get {
                return this.functionsComponentName;
            }
            set { 
                this.functionsComponentName = value;
            } 
        } 

        [DataSourceXmlAttribute(), Browsable(false), DefaultValue(null)] 
        public string UserFunctionsComponentName {
            get {
                return this.userFunctionsComponentName;
            } 
            set {
                this.userFunctionsComponentName = value; 
            } 
        }
 
        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)]
        public string GeneratorFunctionsComponentClassName {
            get {
                return this.generatorFunctionsComponentClassName; 
            }
            set { 
                this.generatorFunctionsComponentClassName = value; 
            }
        } 

        internal override StringCollection NamingPropertyNames {
            get {
                return namingPropNames; 
            }
        } 
 

        void IDataSourceCommandTarget.AddChild(object child, bool fixName) { 
            Type childType = child.GetType();

            Debug.Assert(((IDataSourceCommandTarget)this).CanAddChildOfType(childType), "We can't add this type of child");
 
            if (typeof(DesignTable).IsAssignableFrom(childType)) {
                DesignTables.Add((DesignTable)child); 
            } 
            else if (typeof(DesignRelation).IsAssignableFrom(childType)) {
                DesignRelations.Add((DesignRelation) child); 
            }
            else if (typeof(IDesignConnection).IsAssignableFrom(childType)) {
                DesignConnections.Add( (IDesignConnection) child);
            } 
            else if (typeof(Source).IsAssignableFrom(childType)) {
                Sources.Add((Source) child); 
            } 
        }
 
        bool IDataSourceCommandTarget.CanAddChildOfType(Type childType) {
            return (typeof(DesignTable).IsAssignableFrom(childType) ||
                    typeof(IDesignConnection).IsAssignableFrom(childType) ||
                    typeof(Source).IsAssignableFrom(childType) || 
                    (typeof(DesignRelation).IsAssignableFrom(childType) && ((ICollection)this.DesignTables).Count > 0)
                ); 
        } 

        bool IDataSourceCommandTarget.CanInsertChildOfType(Type childType, object refChild) { 
            if (typeof(Source).IsAssignableFrom(childType)) {
                return (refChild is Source);
            }
            else if (typeof(IDesignConnection).IsAssignableFrom(childType)) { 
                return (refChild is IDesignConnection);
            } 
            if (typeof(DesignTable).IsAssignableFrom(childType)) { 
                return true;
            } 
            return false;
        }

        bool IDataSourceCommandTarget.CanRemoveChildren(ICollection children) { 
            foreach (object child in children) {
                if (!CanRemoveChild(child)) { 
                    return false; 
                }
            } 
            return true;
        }

        private bool CanRemoveChild(object child) { 
            bool canRemove = false;
 
            Type childType = child.GetType(); 

            if (typeof(DesignTable).IsAssignableFrom(childType)) { 
                canRemove = DesignTables.Contains((DesignTable) child);
            }
            else if (typeof(DesignRelation).IsAssignableFrom(childType)) {
                canRemove = DesignRelations.Contains((DesignRelation) child); 
            }
            else if (typeof(IDesignConnection).IsAssignableFrom(childType)) { 
                canRemove = DesignConnections.Contains((IDesignConnection) child); 
            }
            else if (typeof(Source).IsAssignableFrom(childType)) { 
                canRemove = Sources.Contains((Source) child);
            }
            return canRemove;
        } 

        internal ArrayList GetRelatedRelations(ICollection tableList) { 
            ArrayList relatedRelations = new ArrayList(); 
            foreach (DesignRelation relation in DesignRelations) {
                DesignTable parent = relation.ParentDesignTable; 
                DesignTable child = relation.ChildDesignTable;

                foreach (object t in tableList) {
                    if (parent == t || child == t) { 
                        relatedRelations.Add(relation);
                        break; 
                    } 
                }
            } 
            return relatedRelations;
        }

        void IDataSourceCommandTarget.InsertChild(object child, object refChild) { 
            Debug.Assert(((IDataSourceCommandTarget)this).CanInsertChildOfType(child.GetType(), refChild));
            if (child is DesignTable) { 
                DesignTables.InsertBefore(child, refChild); 
            }
            else if (child is DesignRelation) { 
                DesignRelations.InsertBefore(child, refChild);
            }
            else if (child is Source) {
                Sources.InsertBefore(child, refChild); 
            }
            else if (child is IDesignConnection) { 
                DesignConnections.InsertBefore(child, refChild); 
            }
        } 



        object IDataSourceCommandTarget.GetObject(int index, bool getSiblingIfOutOfRange) { 
            throw new NotImplementedException();
        } 
 

        int IDataSourceCommandTarget.IndexOf(object child) { 
            throw new NotImplementedException();
        }

 
        /// <summary>
        /// </summary> 
        public void ReadXmlSchema(Stream stream){ 
            DataSourceXmlTextReader xmlReader = new DataSourceXmlTextReader(this, stream);
            ReadXmlSchema(xmlReader); 
        }

        /// <summary>
        /// ReadXmlSchema, the DesignTables will be created 
        /// </summary>
        public void ReadXmlSchema(TextReader textReader){ 
            DataSourceXmlTextReader xmlReader = new DataSourceXmlTextReader(this, textReader); 
            ReadXmlSchema(xmlReader);
        } 

        private void ReadXmlSchema(DataSourceXmlTextReader xmlReader) {
            designConnections = new DesignConnectionCollection(this);
            designTables = new DesignTableCollection(this); 
            designRelations = new DesignRelationCollection(this);
            sources = new SourceCollection(this); 
 
#if LoadByDataSourceOptimizedXmlSerializer
            serializer = new DataSourceOptimizedXmlSerializer(); 
            dataSet = new DataSet();

            // Let dataSet to read the sceham and create the dataSet
            // during read, we save the appInfo dataSourceXmlNode 
            //
            dataSet.ReadXmlSchema(xmlReader); 
 
            // Create designTables
            foreach (DataTable dataTable in dataSet.Tables) { 
                designTables.Add(new DesignTable(dataTable, TableType.DataTable));
            }

            // Now read back the datasource extra info from dataSourceXmlNode 
            //
            if (appinfoDataSourceXmlElement != null){ 
                serializer.DeserializeDataSource(this, appinfoDataSourceXmlElement); 
                appinfoDataSourceXmlElement = null;
                appinfoXmlDoc = null; 

            }
#else
            serializer = new DataSourceXmlSerializer(); 

            // we create a temporary dataSet here to load our RadTable, and create another DataSet to load schema 
            // later we merge them together with the name of table... 
            // This hacks a little bit, because when we load our own data from <appInfo> tree, we will load it into
            // the current dataSet, and newDataSet will contains real DataTables. Right now, that's only for RadTable, 
            // where we merge them with name.
            dataSet = new DataSet();
            dataSet.Locale = System.Globalization.CultureInfo.InvariantCulture;
 
            DataSet newDataSet = new DataSet();
            newDataSet.Locale = System.Globalization.CultureInfo.InvariantCulture; 
 
            newDataSet.ReadXmlSchema(xmlReader);
            dataSet = newDataSet; 
#endif

            foreach (DataTable dataTable in dataSet.Tables) {
                DesignTable table = designTables[dataTable.TableName]; 
#if !LoadByDataSourceOptimizedXmlSerializer
                if (table == null) { 
                    designTables.Add(new DesignTable(dataTable, TableType.DataTable)); 
                }
                else { 
                    table.DataTable = dataTable;
                }
#endif
                foreach (Constraint constraint in dataTable.Constraints) { 
                    ForeignKeyConstraint relationConstraint = constraint as ForeignKeyConstraint;
 
                    if (relationConstraint != null) { 
                        designRelations.Add(new DesignRelation(relationConstraint));
                    } 
                }
            }

            foreach (DataRelation dataRelation in dataSet.Relations) { 
                DesignRelation relation = designRelations[dataRelation.ChildKeyConstraint];
 
                if (relation != null) { 
                    relation.DataRelation = dataRelation;
                } 
                else {
                    designRelations.Add(new DesignRelation(dataRelation));
                }
            } 

            // we need connect sources to connections 
            foreach (Source source in Sources) { 
                SetConnectionProperty(source);
            } 

            foreach (DesignTable table in this.DesignTables) {
                SetConnectionProperty(table.MainSource);
                foreach (Source function in table.Sources) { 
                    SetConnectionProperty(function);
                } 
            } 

            serializer.InitializeObjects(); 
        }

        private void SetConnectionProperty( Source source ) {
            DbSource dbSource = source as DbSource; 
            if( dbSource == null ) {
                return; 
            } 

 
            string connectionRef = dbSource.ConnectionRef;
            if (connectionRef == null || connectionRef.Length == 0) {
                //
 

 
            } 
            else {
                IDesignConnection connection = this.DesignConnections.Get(connectionRef); 
                if (connection == null) {
                //

                } 
                else {
                    dbSource.Connection = connection; 
                } 
            }
        } 



 

 
 

 



 

 
#if LoadByDataSourceOptimizedXmlSerializer 
        XmlDocument appinfoXmlDoc;
        XmlElement appinfoDataSourceXmlElement; 
        /// <summary>
        /// </summary>
        internal void ReadDataSourceExtraInformation(XmlTextReader xmlTextReader) {
            appinfoXmlDoc = new XmlDocument(); 
            appinfoDataSourceXmlElement = appinfoXmlDoc.ReadNode(xmlTextReader) as XmlElement;
            appinfoXmlDoc.AppendChild(appinfoDataSourceXmlElement); 
        } 
#else
        /// <summary> 
        /// </summary>
        internal void ReadDataSourceExtraInformation(XmlTextReader xmlTextReader) {
            XmlDocument xdoc = new XmlDocument();
            XmlNode schNode = xdoc.ReadNode(xmlTextReader); 
            xdoc.AppendChild(schNode);
 
            Debug.Assert(serializer != null, "This function should only be called by DataSetXmlTextReader"); 
            if (serializer != null) {
                serializer.DeserializeBody((XmlElement)schNode, this); 
            }
        }
#endif
 

        void IDataSourceCommandTarget.RemoveChildren(ICollection children) { 
            // we should remove table later than Relations, 
            // if there is a dataRelation on the table, we won't be able to remove the table until we remove the relation
            SortedList tableList = new SortedList(); 
            foreach (object child in children) {
                if (child is DesignTable){
                    Debug.Assert(this.DesignTables.Contains((DesignTable) child), "Try to remove designTable that not in the collection");
 
                    // Use nagative index value as key, so the sorted order
                    // is the reverse order of the index, then we can remove tables 
                    // in reverse order. 
                    //
                    tableList.Add(-DesignTables.IndexOf((DesignTable) child), child); 
                }
                else {
                    RemoveChild(child);
                } 
            }
 
            // we should remove relations connecting to this table at first... 
            ArrayList relatedRelations = GetRelatedRelations(children);
            foreach (DesignRelation relatedRelation in relatedRelations) { 
                RemoveChild(relatedRelation);
            }

            // To keep the order when undo, we either need to keep the remove as a group undo unit 
            // or need to remove the children in the revers order
            // We should not rely the given children's order, which is defined by UI. 
            // 
            foreach (object child in tableList.Values) {
                if (child is DesignTable) { 
                    RemoveChild(child);
                }
            }
        } 

        private void RemoveChild(object child) { 
 
            Debug.Assert(CanRemoveChild(child), "Invalid child");
 
            Type childType = child.GetType();

            if (typeof(DesignTable).IsAssignableFrom(childType)) {
                DesignTables.Remove((DesignTable) child); 
            }
            else if (typeof(DesignRelation).IsAssignableFrom(childType)) { 
                DesignRelations.Remove((DesignRelation) child); 
            }
            else if (typeof(IDesignConnection).IsAssignableFrom(childType)) { 
                DesignConnections.Remove((IDesignConnection) child);
            }
            else if (typeof(Source).IsAssignableFrom(childType)) {
                Sources.Remove((Source) child); 
            }
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//#define LoadByDataSourceOptimizedXmlSerializer
//#define perfcounter
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'> 
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright> 
//-----------------------------------------------------------------------------
namespace System.Data.Design{ 

    using System;
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design; 
    using System.Diagnostics;
    using System.Data; 
    using System.Data.OleDb;
    using System.Data.Common;
    using System.IO;
    using System.Globalization; 
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Reflection; 
    using System.Xml; 
    using System.Windows.Forms;
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Threading;

 
    /// <summary>
    /// DataSource is the so-called design time data source class intended to be used 
    /// by DataSourceDesigner, DataSourceCodeGeneration, DataService and other Data 
    /// specific tasks.
    /// DataSource is defined as Component because we want to use it as the root component in 
    /// the DataSource designer
    /// </summary>
    [
        DataSourceXmlClass(SchemaName.DataSourceRoot) 
    ]
    internal class DesignDataSource : DataSourceComponent, IDataSourceNamedObject, IDataSourceCommandTarget { 
        private DataSet dataSet; 
        private DesignTableCollection designTables;
        private DesignRelationCollection designRelations; 
        private DesignConnectionCollection designConnections;
        private int defaultConnectionIndex;
        private SourceCollection sources;
        private TypeAttributes modifier = TypeAttributes.Public; 
        private SchemaSerializationMode schemaSerializationMode = SchemaSerializationMode.IncludeSchema;
 
#if LoadByDataSourceOptimizedXmlSerializer 
        private DataSourceOptimizedXmlSerializer serializer;
#else 
        private DataSourceXmlSerializer serializer;
#endif

        private  StringCollection namingPropNames = new StringCollection(); 
        internal static string EXTPROPNAME_USER_DATASETNAME = "Generator_UserDSName";
        internal static string EXTPROPNAME_GENERATOR_DATASETNAME = "Generator_DataSetName"; 
        private const string EXTPROPNAME_ENABLE_TABLEADAPTERMANAGER = "EnableTableAdapterManager"; 
        private string functionsComponentName = null;
        private string userFunctionsComponentName = null; 
        private string generatorFunctionsComponentClassName = null;


        /// <summary> 
        /// This event encapsulates all data change events including property change events and collection change events.
        /// </summary> 
        /// <value></value> 

        /// <summary> 
        /// </summary>
        internal DataSet DataSet{
            get{
                if (dataSet == null) { 
                    dataSet = new DataSet();
                    dataSet.Locale = System.Globalization.CultureInfo.InvariantCulture; 
                    // VS Whidbey Bug 194886 
                    dataSet.EnforceConstraints = false;
                } 
                return dataSet;
            }
        }
 
        /// <summary>
        /// </summary> 
        // NOTE: as we save DefaultConnection as an Index, any ConnectionEditor should maintain it if it modified the DesignConnection collection 
        //   except operation only adds a new connection to the end of the collection.
        [ 
 			DisplayName ("DefaultConnection"),
        ]
        public DesignConnection DefaultConnection {
            get { 
                if (DesignConnections.Count > 0) {
                    if (defaultConnectionIndex >= 0 && defaultConnectionIndex < DesignConnections.Count) { 
                        return ((IList)DesignConnections)[defaultConnectionIndex] as DesignConnection; 
                    }
                } 

                return null;
            }
        } 

        /// <summary> 
        /// </summary> 
        [
            DisplayName("Connections"), 
            DataSourceXmlSubItem(Name="Connections", ItemType=typeof(DesignConnection)),
            Browsable(false)
        ]
        public DesignConnectionCollection DesignConnections { 
            get{
                if (designConnections == null){ 
                    designConnections = new DesignConnectionCollection(this); 
                }
                return designConnections; 
            }
        }

        /// <summary> 
        /// </summary>
        [Browsable(false)] 
        public DesignRelationCollection DesignRelations { 
            get{
                if (designRelations == null){ 
                    designRelations = new DesignRelationCollection(this);
                }
                return designRelations;
            } 
        }
 
        /// <summary> 
        /// </summary>
        [ 
            DataSourceXmlSubItem(Name="Tables", ItemType=typeof(DesignConnection)),
            Browsable(false),
        ]
        public DesignTableCollection DesignTables { 
            get{
                if (designTables == null){ 
                    designTables = new DesignTableCollection(this); 
                }
                return designTables; 
            }
        }

        [DefaultValue(true)] 
        public bool EnableTableAdapterManager {
            get { 
                bool result = false; 
                Boolean.TryParse(this.DataSet.ExtendedProperties[EXTPROPNAME_ENABLE_TABLEADAPTERMANAGER] as string, out result);
                return result; 
            }
            set {
                this.DataSet.ExtendedProperties[EXTPROPNAME_ENABLE_TABLEADAPTERMANAGER] = value.ToString();
            } 
        }
 
        /// <summary> 
        /// Dataset class modifier
        /// </summary> 
        [
            DefaultValue(TypeAttributes.Public),
            DataSourceXmlAttribute()
        ] 
        public TypeAttributes Modifier {
            get { 
                return modifier; 
            }
            set { 
                modifier = value;
            }
        }
 

        [ 
            MergableProperty(false), 
            DefaultValue("")
        ] 
        public string Name {
            get {
                return this.DataSet.DataSetName;
            } 
            set {
                this.DataSet.DataSetName = value; 
            } 
        }
 


        [Browsable(false)]
        public string PublicTypeName { 
            get {
                return "DataSet"; 
            } 
        }
 
        /// <summary>
        /// </summary>
        [
            DataSourceXmlSubItem(typeof(Source)), 
            Browsable(false),
        ] 
        public SourceCollection Sources{ 
            get{
                if (sources == null){ 
                    sources = new SourceCollection(this);
                }
                return sources;
            } 
        }
 
        [DataSourceXmlAttribute()] 
        public SchemaSerializationMode SchemaSerializationMode {
            get { 
                return schemaSerializationMode;
            }
            set {
                schemaSerializationMode = value; 
            }
        } 
 
        internal string UserDataSetName {
            get { 
                return this.DataSet.ExtendedProperties[EXTPROPNAME_USER_DATASETNAME] as string;
            }
            set {
                this.DataSet.ExtendedProperties[EXTPROPNAME_USER_DATASETNAME] = value; 
            }
        } 
 
        internal string GeneratorDataSetName {
            get { 
                return this.DataSet.ExtendedProperties[EXTPROPNAME_GENERATOR_DATASETNAME] as string;
            }
            set {
                this.DataSet.ExtendedProperties[EXTPROPNAME_GENERATOR_DATASETNAME] = value; 
            }
        } 
 
        [DataSourceXmlAttribute(), Browsable(false), DefaultValue(null)]
        public string FunctionsComponentName { 
            get {
                return this.functionsComponentName;
            }
            set { 
                this.functionsComponentName = value;
            } 
        } 

        [DataSourceXmlAttribute(), Browsable(false), DefaultValue(null)] 
        public string UserFunctionsComponentName {
            get {
                return this.userFunctionsComponentName;
            } 
            set {
                this.userFunctionsComponentName = value; 
            } 
        }
 
        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)]
        public string GeneratorFunctionsComponentClassName {
            get {
                return this.generatorFunctionsComponentClassName; 
            }
            set { 
                this.generatorFunctionsComponentClassName = value; 
            }
        } 

        internal override StringCollection NamingPropertyNames {
            get {
                return namingPropNames; 
            }
        } 
 

        void IDataSourceCommandTarget.AddChild(object child, bool fixName) { 
            Type childType = child.GetType();

            Debug.Assert(((IDataSourceCommandTarget)this).CanAddChildOfType(childType), "We can't add this type of child");
 
            if (typeof(DesignTable).IsAssignableFrom(childType)) {
                DesignTables.Add((DesignTable)child); 
            } 
            else if (typeof(DesignRelation).IsAssignableFrom(childType)) {
                DesignRelations.Add((DesignRelation) child); 
            }
            else if (typeof(IDesignConnection).IsAssignableFrom(childType)) {
                DesignConnections.Add( (IDesignConnection) child);
            } 
            else if (typeof(Source).IsAssignableFrom(childType)) {
                Sources.Add((Source) child); 
            } 
        }
 
        bool IDataSourceCommandTarget.CanAddChildOfType(Type childType) {
            return (typeof(DesignTable).IsAssignableFrom(childType) ||
                    typeof(IDesignConnection).IsAssignableFrom(childType) ||
                    typeof(Source).IsAssignableFrom(childType) || 
                    (typeof(DesignRelation).IsAssignableFrom(childType) && ((ICollection)this.DesignTables).Count > 0)
                ); 
        } 

        bool IDataSourceCommandTarget.CanInsertChildOfType(Type childType, object refChild) { 
            if (typeof(Source).IsAssignableFrom(childType)) {
                return (refChild is Source);
            }
            else if (typeof(IDesignConnection).IsAssignableFrom(childType)) { 
                return (refChild is IDesignConnection);
            } 
            if (typeof(DesignTable).IsAssignableFrom(childType)) { 
                return true;
            } 
            return false;
        }

        bool IDataSourceCommandTarget.CanRemoveChildren(ICollection children) { 
            foreach (object child in children) {
                if (!CanRemoveChild(child)) { 
                    return false; 
                }
            } 
            return true;
        }

        private bool CanRemoveChild(object child) { 
            bool canRemove = false;
 
            Type childType = child.GetType(); 

            if (typeof(DesignTable).IsAssignableFrom(childType)) { 
                canRemove = DesignTables.Contains((DesignTable) child);
            }
            else if (typeof(DesignRelation).IsAssignableFrom(childType)) {
                canRemove = DesignRelations.Contains((DesignRelation) child); 
            }
            else if (typeof(IDesignConnection).IsAssignableFrom(childType)) { 
                canRemove = DesignConnections.Contains((IDesignConnection) child); 
            }
            else if (typeof(Source).IsAssignableFrom(childType)) { 
                canRemove = Sources.Contains((Source) child);
            }
            return canRemove;
        } 

        internal ArrayList GetRelatedRelations(ICollection tableList) { 
            ArrayList relatedRelations = new ArrayList(); 
            foreach (DesignRelation relation in DesignRelations) {
                DesignTable parent = relation.ParentDesignTable; 
                DesignTable child = relation.ChildDesignTable;

                foreach (object t in tableList) {
                    if (parent == t || child == t) { 
                        relatedRelations.Add(relation);
                        break; 
                    } 
                }
            } 
            return relatedRelations;
        }

        void IDataSourceCommandTarget.InsertChild(object child, object refChild) { 
            Debug.Assert(((IDataSourceCommandTarget)this).CanInsertChildOfType(child.GetType(), refChild));
            if (child is DesignTable) { 
                DesignTables.InsertBefore(child, refChild); 
            }
            else if (child is DesignRelation) { 
                DesignRelations.InsertBefore(child, refChild);
            }
            else if (child is Source) {
                Sources.InsertBefore(child, refChild); 
            }
            else if (child is IDesignConnection) { 
                DesignConnections.InsertBefore(child, refChild); 
            }
        } 



        object IDataSourceCommandTarget.GetObject(int index, bool getSiblingIfOutOfRange) { 
            throw new NotImplementedException();
        } 
 

        int IDataSourceCommandTarget.IndexOf(object child) { 
            throw new NotImplementedException();
        }

 
        /// <summary>
        /// </summary> 
        public void ReadXmlSchema(Stream stream){ 
            DataSourceXmlTextReader xmlReader = new DataSourceXmlTextReader(this, stream);
            ReadXmlSchema(xmlReader); 
        }

        /// <summary>
        /// ReadXmlSchema, the DesignTables will be created 
        /// </summary>
        public void ReadXmlSchema(TextReader textReader){ 
            DataSourceXmlTextReader xmlReader = new DataSourceXmlTextReader(this, textReader); 
            ReadXmlSchema(xmlReader);
        } 

        private void ReadXmlSchema(DataSourceXmlTextReader xmlReader) {
            designConnections = new DesignConnectionCollection(this);
            designTables = new DesignTableCollection(this); 
            designRelations = new DesignRelationCollection(this);
            sources = new SourceCollection(this); 
 
#if LoadByDataSourceOptimizedXmlSerializer
            serializer = new DataSourceOptimizedXmlSerializer(); 
            dataSet = new DataSet();

            // Let dataSet to read the sceham and create the dataSet
            // during read, we save the appInfo dataSourceXmlNode 
            //
            dataSet.ReadXmlSchema(xmlReader); 
 
            // Create designTables
            foreach (DataTable dataTable in dataSet.Tables) { 
                designTables.Add(new DesignTable(dataTable, TableType.DataTable));
            }

            // Now read back the datasource extra info from dataSourceXmlNode 
            //
            if (appinfoDataSourceXmlElement != null){ 
                serializer.DeserializeDataSource(this, appinfoDataSourceXmlElement); 
                appinfoDataSourceXmlElement = null;
                appinfoXmlDoc = null; 

            }
#else
            serializer = new DataSourceXmlSerializer(); 

            // we create a temporary dataSet here to load our RadTable, and create another DataSet to load schema 
            // later we merge them together with the name of table... 
            // This hacks a little bit, because when we load our own data from <appInfo> tree, we will load it into
            // the current dataSet, and newDataSet will contains real DataTables. Right now, that's only for RadTable, 
            // where we merge them with name.
            dataSet = new DataSet();
            dataSet.Locale = System.Globalization.CultureInfo.InvariantCulture;
 
            DataSet newDataSet = new DataSet();
            newDataSet.Locale = System.Globalization.CultureInfo.InvariantCulture; 
 
            newDataSet.ReadXmlSchema(xmlReader);
            dataSet = newDataSet; 
#endif

            foreach (DataTable dataTable in dataSet.Tables) {
                DesignTable table = designTables[dataTable.TableName]; 
#if !LoadByDataSourceOptimizedXmlSerializer
                if (table == null) { 
                    designTables.Add(new DesignTable(dataTable, TableType.DataTable)); 
                }
                else { 
                    table.DataTable = dataTable;
                }
#endif
                foreach (Constraint constraint in dataTable.Constraints) { 
                    ForeignKeyConstraint relationConstraint = constraint as ForeignKeyConstraint;
 
                    if (relationConstraint != null) { 
                        designRelations.Add(new DesignRelation(relationConstraint));
                    } 
                }
            }

            foreach (DataRelation dataRelation in dataSet.Relations) { 
                DesignRelation relation = designRelations[dataRelation.ChildKeyConstraint];
 
                if (relation != null) { 
                    relation.DataRelation = dataRelation;
                } 
                else {
                    designRelations.Add(new DesignRelation(dataRelation));
                }
            } 

            // we need connect sources to connections 
            foreach (Source source in Sources) { 
                SetConnectionProperty(source);
            } 

            foreach (DesignTable table in this.DesignTables) {
                SetConnectionProperty(table.MainSource);
                foreach (Source function in table.Sources) { 
                    SetConnectionProperty(function);
                } 
            } 

            serializer.InitializeObjects(); 
        }

        private void SetConnectionProperty( Source source ) {
            DbSource dbSource = source as DbSource; 
            if( dbSource == null ) {
                return; 
            } 

 
            string connectionRef = dbSource.ConnectionRef;
            if (connectionRef == null || connectionRef.Length == 0) {
                //
 

 
            } 
            else {
                IDesignConnection connection = this.DesignConnections.Get(connectionRef); 
                if (connection == null) {
                //

                } 
                else {
                    dbSource.Connection = connection; 
                } 
            }
        } 



 

 
 

 



 

 
#if LoadByDataSourceOptimizedXmlSerializer 
        XmlDocument appinfoXmlDoc;
        XmlElement appinfoDataSourceXmlElement; 
        /// <summary>
        /// </summary>
        internal void ReadDataSourceExtraInformation(XmlTextReader xmlTextReader) {
            appinfoXmlDoc = new XmlDocument(); 
            appinfoDataSourceXmlElement = appinfoXmlDoc.ReadNode(xmlTextReader) as XmlElement;
            appinfoXmlDoc.AppendChild(appinfoDataSourceXmlElement); 
        } 
#else
        /// <summary> 
        /// </summary>
        internal void ReadDataSourceExtraInformation(XmlTextReader xmlTextReader) {
            XmlDocument xdoc = new XmlDocument();
            XmlNode schNode = xdoc.ReadNode(xmlTextReader); 
            xdoc.AppendChild(schNode);
 
            Debug.Assert(serializer != null, "This function should only be called by DataSetXmlTextReader"); 
            if (serializer != null) {
                serializer.DeserializeBody((XmlElement)schNode, this); 
            }
        }
#endif
 

        void IDataSourceCommandTarget.RemoveChildren(ICollection children) { 
            // we should remove table later than Relations, 
            // if there is a dataRelation on the table, we won't be able to remove the table until we remove the relation
            SortedList tableList = new SortedList(); 
            foreach (object child in children) {
                if (child is DesignTable){
                    Debug.Assert(this.DesignTables.Contains((DesignTable) child), "Try to remove designTable that not in the collection");
 
                    // Use nagative index value as key, so the sorted order
                    // is the reverse order of the index, then we can remove tables 
                    // in reverse order. 
                    //
                    tableList.Add(-DesignTables.IndexOf((DesignTable) child), child); 
                }
                else {
                    RemoveChild(child);
                } 
            }
 
            // we should remove relations connecting to this table at first... 
            ArrayList relatedRelations = GetRelatedRelations(children);
            foreach (DesignRelation relatedRelation in relatedRelations) { 
                RemoveChild(relatedRelation);
            }

            // To keep the order when undo, we either need to keep the remove as a group undo unit 
            // or need to remove the children in the revers order
            // We should not rely the given children's order, which is defined by UI. 
            // 
            foreach (object child in tableList.Values) {
                if (child is DesignTable) { 
                    RemoveChild(child);
                }
            }
        } 

        private void RemoveChild(object child) { 
 
            Debug.Assert(CanRemoveChild(child), "Invalid child");
 
            Type childType = child.GetType();

            if (typeof(DesignTable).IsAssignableFrom(childType)) {
                DesignTables.Remove((DesignTable) child); 
            }
            else if (typeof(DesignRelation).IsAssignableFrom(childType)) { 
                DesignRelations.Remove((DesignRelation) child); 
            }
            else if (typeof(IDesignConnection).IsAssignableFrom(childType)) { 
                DesignConnections.Remove((IDesignConnection) child);
            }
            else if (typeof(Source).IsAssignableFrom(childType)) {
                Sources.Remove((Source) child); 
            }
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
