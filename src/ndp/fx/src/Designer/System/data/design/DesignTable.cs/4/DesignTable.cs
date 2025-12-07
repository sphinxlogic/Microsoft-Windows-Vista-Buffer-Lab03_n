 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design{ 

    using System; 
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Data.Common; 
 	using System.Design;
	using System.Diagnostics; 
	using System.IO;
    using System.Xml;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary; 
    using System.Windows.Forms;
    using System.Text; 
 

    internal class DesignTable : DataSourceComponent, IDataSourceNamedObject, IDataSourceXmlSerializable, 
                    IDataSourceXmlSpecialOwner, IDataSourceInitAfterLoading, IDataSourceCommandTarget {

        private TableType  tableType;
        private DataTable   dataTable; 
        private DataAccessor dataAccessor;
        private DesignColumnCollection designColumns; 
        private DesignDataSource owner; 
        private TypeAttributes dataAccessorModifier = TypeAttributes.Public;
 
        private Source mainSource;
        private SourceCollection sources;
        private DataColumnMappingCollection mappings;
        private bool webServiceAttribute; 
        private string webServiceNamespace;
        private string webServiceDescription; 
 
        private string      provider;
        private string      generatorRunFillName; 
        private string      baseClass;
        private string      dataAccessorName;

        private event EventHandler      tableTypeChanged; 
        private event EventHandler      constraintsChanged;
        private bool                    inAccessConstraints; 
        private event EventHandler dataAccessorChanged; 
        private event EventHandler dataAccessorChanging;
 
        private const string DATATABLE_NAMEROOT = "DataTable";
        private const string RADTABLE_NAMEROOT = "DataTable";
        private const string KEY_NAMEROOT = "Key";
        private const string PRIMARYKEY_PROPERTY = "PrimaryKey"; 
        internal const string MAINSOURCE_PROPERTY = "MainSource";
        private const string MAINSOURCE_NAME = "Fill"; 
        internal const string NAME_PROPERTY = "Name"; 

        private string generatorDataComponentClassName = null; 
        private string userDataComponentName = null;
        private CodeGenPropertyCache codeGenPropertyCache;

        private StringCollection namingPropNames = new StringCollection(); 
        internal static string EXTPROPNAME_USER_TABLENAME = "Generator_UserTableName";
        internal static string EXTPROPNAME_GENERATOR_TABLEPROPNAME = "Generator_TablePropName"; 
        internal static string EXTPROPNAME_GENERATOR_TABLEVARNAME = "Generator_TableVarName"; 
        internal static string EXTPROPNAME_GENERATOR_TABLECLASSNAME = "Generator_TableClassName";
        internal static string EXTPROPNAME_GENERATOR_ROWCLASSNAME = "Generator_RowClassName"; 
        internal static string EXTPROPNAME_GENERATOR_ROWEVHANDLERNAME = "Generator_RowEvHandlerName";
        internal static string EXTPROPNAME_GENERATOR_ROWEVARGNAME = "Generator_RowEvArgName";
        internal static string EXTPROPNAME_GENERATOR_ROWCHANGINGNAME = "Generator_RowChangingName";
        internal static string EXTPROPNAME_GENERATOR_ROWCHANGEDNAME = "Generator_RowChangedName"; 
        internal static string EXTPROPNAME_GENERATOR_ROWDELETINGNAME = "Generator_RowDeletingName";
        internal static string EXTPROPNAME_GENERATOR_ROWDELETEDNAME = "Generator_RowDeletedName"; 
 
        /// <summary>
        ///  this is only used by serializer... 
        /// </summary>
        public DesignTable() : this(null, TableType.DataTable) {
        }
 
        /// <summary>
        /// </summary> 
        /// <param name="dataTable"></param> 
        public DesignTable(DataTable dataTable) : this(dataTable, TableType.DataTable) {
        } 

        /// <summary>
        /// </summary>
        /// <param name="dataTable"></param> 
        /// <param name="tableType"></param>
        public DesignTable(DataTable dataTable, TableType tableType){ 
            if (dataTable == null) { 
                this.dataTable = new DataTable();
                this.dataTable.Locale = System.Globalization.CultureInfo.InvariantCulture; 
            }
            else {
                this.dataTable = dataTable;
            } 

            this.TableType = tableType; 
            AddRemoveConstraintMonitor(true); 
            namingPropNames.AddRange(new string[] { "typedPlural", "typedName" });
        } 


        public DesignTable(DataTable dataTable, TableType tableType, DataColumnMappingCollection mappings):
        this(dataTable, tableType) { 
            this.mappings = mappings;
        } 
 
        /// <summary>
        /// Used by 'DataAccessor' 
        /// </summary>
        /// <value></value>
        [
            DataSourceXmlAttribute(), 
            Browsable(false),
        ] 
        public string BaseClass { 
            get {
                if (StringUtil.NotEmptyAfterTrim(baseClass)) { 
                    return baseClass;
                }
                return DataAccessor.DEFAULT_BASE_CLASS;
            } 
            set {
                baseClass = value; 
            } 
        }
 

        public IDesignConnection Connection {
            get{
                if (this.TableType == TableType.RadTable){ 
                    DbSource dbs = EnsureDbSource();
                    return dbs.Connection; 
                } 
                return null;
            } 
            set {
                if (this.TableType == TableType.RadTable){
                    DbSource dbs = EnsureDbSource();
                    dbs.Connection = value; 
                }
                else { 
                    Debug.Assert( value == null, "You should not try to set the connection property to non-RadTableObject" ); 
                }
            } 
        }

        internal event EventHandler ConstraintChanged {
            add { 
                constraintsChanged += value;
            } 
            remove { 
                constraintsChanged -= value;
            } 
        }

        /// <summary>
        /// this property can be accessed by DataSourceRootDesigner to Add/Delete/Replace a component, 
        /// DesignTableDesigner will catch the events and add/remove the component to the designer host
        /// // and update the UI. 
        /// </summary> 
        /// <value></value>
        internal DataAccessor DataAccessor { 
            get {
                return dataAccessor;
            }
            set { 
                if (dataAccessorChanging != null) {
                    dataAccessorChanging(this, new EventArgs()); 
                } 

                this.dataAccessor = value; 

                if (dataAccessorChanged != null) {
                    dataAccessorChanged(this, new EventArgs());
                } 
            }
        } 
 
        internal event EventHandler DataAccessorChanged {
            add { 
                dataAccessorChanged += value;
            }
            remove {
                dataAccessorChanged -= value; 
            }
        } 
 
        internal event EventHandler DataAccessorChanging {
            add { 
                dataAccessorChanging += value;
            }
            remove {
                dataAccessorChanging -= value; 
            }
        } 
 

        /// <summary> 
        /// Used by 'DataAccessor'
        /// </summary>
        /// <value></value>
        [ 
            DataSourceXmlAttribute(),
            Browsable(false), 
        ] 
        public string DataAccessorName {
            get { 
                if (StringUtil.NotEmptyAfterTrim(dataAccessorName)) {
                    return dataAccessorName;
                }
                return this.Name + DataAccessor.DEFAULT_NAME_POSTFIX; 
            }
            set { 
                dataAccessorName = value; 
            }
        } 

        /// <summary>
        /// </summary>
        [Browsable(false)] 
        public DataTable DataTable {
            get{ 
                return dataTable; 
            }
            set { 
                if (dataTable != value) {

                    if (dataTable != null) {
                        AddRemoveConstraintMonitor(false); 
                    }
 
                    dataTable = value; 

                    if (dataTable != null) { 
                        AddRemoveConstraintMonitor(true);
                    }
                }
            } 
        }
 
        [ 
            DefaultValue(null)
        ] 
        public DbSourceCommand DeleteCommand {
            get {
                DbSource dbs = EnsureDbSource();
                return dbs.DeleteCommand; 
            }
            set { 
                DbSource dbs = EnsureDbSource(); 
                dbs.DeleteCommand = value;
            } 
        }

        /// <summary>
        /// </summary> 
        [Browsable(false)]
        public DesignColumnCollection DesignColumns { 
            get{ 
                if (designColumns == null){
                    designColumns = new DesignColumnCollection(this); 
                }
                return designColumns;
            }
        } 

 
 
        /// <summary>
        ///  return the object supports external properties 
        /// </summary>
        protected override object ExternalPropertyHost {
            get {
                return dataTable; 
            }
        } 
 
        internal bool HasAnyUpdateCommand{
            get { 
                if (TableType == TableType.RadTable
                    && this.MainSource != null && this.MainSource is DbSource
                    && (((DbSource)this.MainSource).CommandOperation == CommandOperation.Select)
                    && (this.DeleteCommand != null || this.InsertCommand != null || this.UpdateCommand != null)) { 
                    return true;
                } 
                else { 
                    return false;
                } 
            }
        }

        internal bool HasAnyExpressionColumn { 
            get {
                DataTable table = this.DataTable; 
                foreach (DataColumn column in table.Columns) { 
                    if (column.Expression != null &&  column.Expression.Length > 0) {
                        return true; 
                    }
                }
                return false;
            } 
        }
 
        [ 
            DefaultValue(null)
        ] 
        public DbSourceCommand InsertCommand {
            get {
                DbSource dbs = EnsureDbSource();
                return dbs.InsertCommand; 
            }
            set { 
                DbSource dbs = EnsureDbSource(); 
                dbs.InsertCommand = value;
            } 
        }

        [Browsable(false), DataSourceXmlSubItem(Name=MAINSOURCE_PROPERTY, ItemType=typeof(Source))]
        public Source MainSource { 
            get {
 
                if (this.mainSource == null) { 
                    DbSource dbsource = new DbSource ();
                    if (this.Owner != null){ 
                        dbsource.Connection =  this.Owner.DefaultConnection;
                    }
                    this.MainSource = dbsource;
                } 

                return this.mainSource; 
            } 
            set {
                if( this.mainSource != null ) { 
                    this.mainSource.Owner = null;
                }

                this.mainSource = value; 

                if( value != null ) { 
                    this.mainSource.Owner = this; 
                    if (StringUtil.EmptyOrSpace(this.mainSource.Name)){
                        this.mainSource.Name = MAINSOURCE_NAME; 
                    }
                }
            }
        } 

 
        /// <summary> 
        ///  ATTENTION: Don't modify the mapping directly until you know how to handle undo/redo.
        ///   Call AddColumnMapping, ClearColumnMapping and RemoveColumnMapping, those functions know how to add UndoUnit for the operation 
        /// Please talk to [....] or lifengl about it
        /// </summary>
        [
            Browsable(false), 
            DataSourceXmlElement(Name="Mappings", SpecialWay=true),
            // 
 
        ]
        public DataColumnMappingCollection Mappings { 
            get{
                if( this.mappings == null ) {
                    this.mappings = new DataColumnMappingCollection();
                } 

                return this.mappings; 
            } 
            set {   // this is only used by undo
                this.mappings = value; 
            }
        }

        private bool ShouldSerializeMappings() { 
            return (this.mappings != null && (this.mappings.Count > 0));
        } 
 
        [
            DefaultValue(TypeAttributes.Public), 
            DataSourceXmlAttribute()
        ]
        public TypeAttributes DataAccessorModifier {
            get { 
                return this.dataAccessorModifier;
            } 
            set { 
                this.dataAccessorModifier = value;
            } 
        }

        /// <summary>
        /// Property need for all types 
        /// </summary>
        [ 
            DefaultValue(""), 
            DataSourceXmlAttribute(),
            MergableProperty(false) 
        ]
        public string Name{
            get{
                return dataTable.TableName; 
            }
            set { 
                Debug.Assert(dataTable != null, "dataTable is null"); 
                if (dataTable.TableName != value) {
                    if (this.CollectionParent != null) { 
                        CollectionParent.ValidateUniqueName (this, value);
                    }
                    dataTable.TableName = value;
                } 
            }
        } 
 
        internal DesignDataSource Owner {
            get { 
                return this.owner;
            }
            set {
                if (this.owner != value) { 
                    string oldTarget = (this.owner != null) ? this.owner.DataSet.Namespace : SchemaName.DataSourceTempTargetNamespace;
                    // if there is a new owner, set newTarget to null, so it would follow the change of the owner. 
                    // 
                    string newTarget = (value != null) ? null : SchemaName.DataSourceTempTargetNamespace;
 
                    this.owner = value;
                }
            }
        } 

        public DbSourceParameterCollection Parameters { 
            get { 
                DbSource source = MainSource as DbSource;
                if( source != null ) { 
                    // undone: we don't want this assert until the wizard is working
                    //Debug.Assert(source.SelectCommand != null, "SelectCommand should never be null");
                    if( source.SelectCommand != null ) {
                        return source.SelectCommand.Parameters; 
                    }
                } 
 
                return null;
            } 
        }

        private bool ShouldSerializeParameters() {
            if (TableType != TableType.RadTable) { 
                return false;
            } 
            else { 
                DbSourceParameterCollection parameters = Parameters;
                return (null != parameters && (0 < parameters.Count)); 
            }
        }

 

        [Browsable (false)] 
        public DataColumn[] PrimaryKeyColumns { 
            get {
                return this.DataTable.PrimaryKey; 
            }
            set {
                AddRemoveConstraintMonitor(false);
                try { 
                    this.SetPropertyValue(PRIMARYKEY_PROPERTY, value);
                    OnConstraintChanged(); 
                } 
                finally {
                    AddRemoveConstraintMonitor(true); 
                }
            }
        }
 
        /// <summary>
        /// Property need for RadTable type 
        /// </summary> 
        [
            DefaultValue(null), 
            DataSourceXmlAttribute(),
            Browsable (false)
        ]
        public string Provider{ 
            get{
                return provider; 
            } 
            set {
                provider = value; 
            }
        }

        [Browsable(false)] 
        public string PublicTypeName {
            get { 
                string name; 

                switch (tableType){ 
                    case (TableType.DataTable):
                        name = DATATABLE_NAMEROOT;
                        break;
 
                    case (TableType.RadTable):
                        name = RADTABLE_NAMEROOT; 
                        break; 

                    default: 
                        Debug.Fail("UNDONE: NYI");
                        return null;
                }
                return name; 
            }
        } 
 
        [
            Browsable(false) 
        ]
        public DbSourceCommand SelectCommand {
            get {
                DbSource dbs = EnsureDbSource(); 
                return dbs.SelectCommand;
            } 
            set { 
                DbSource dbs = EnsureDbSource();
                dbs.SelectCommand = value; 
            }
        }

 
        /// <summary>
        /// </summary> 
 
        /// <summary>
        /// </summary> 
        [
            DataSourceXmlSubItem(typeof(Source)),
            Browsable(false)
        ] 
        public SourceCollection Sources{
            get{ 
                if( this.sources == null ) { 
                    this.sources = new SourceCollection(this);
                } 

                return this.sources;
            }
        } 

        /// <summary> 
        /// </summary> 
        [Browsable(false)]
        public TableType TableType { 
            get {
                return tableType;
            }
            set { 
                tableType = value;
                if (tableType == TableType.RadTable) { 
                    this.DataAccessor = new DataAccessor(this); 
                }
                else { 
                    this.DataAccessor = null;
                }
            }
        } 

        internal event EventHandler TableTypeChanged { 
            add { 
                tableTypeChanged += value;
            } 
            remove {
                tableTypeChanged -= value;
            }
        } 

        [ 
            DefaultValue(null) 
        ]
        public DbSourceCommand UpdateCommand { 
            get {
                DbSource dbs = EnsureDbSource();
                return dbs.UpdateCommand;
            } 
            set {
                DbSource dbs = EnsureDbSource(); 
                dbs.UpdateCommand = value; 
            }
        } 

        [
            DataSourceXmlAttribute(ItemType=typeof(bool)),
            Browsable(false), 
            DefaultValue(false),
        ] 
        public bool WebServiceAttribute{ 
            get{
                return webServiceAttribute; 
            }
            set{
                webServiceAttribute = value;
            } 
        }
 
        [ 
            DataSourceXmlAttribute(),
            Browsable(false), 
        ]
        public string WebServiceDescription{
            get{
                return webServiceDescription; 
            }
            set{ 
                webServiceDescription = value; 
            }
        } 

        [
            DataSourceXmlAttribute(),
            Browsable(false), 
        ]
        public string WebServiceNamespace{ 
            get{ 
                return webServiceNamespace;
            } 
            set{
                webServiceNamespace = value;
            }
        } 

        void IDataSourceCommandTarget.AddChild(object child, bool fixName) { 
            Debug.Assert(((IDataSourceCommandTarget)this).CanAddChildOfType(child.GetType()), "invalid child"); 

            if (child is DesignColumn) { 
                DesignColumns.Add ((DesignColumn)child);
            }
            else if (child is Source) {
                if (child is DbSource) { 
                    ((DbSource) child).Connection = this.Connection;
                    if (this.Connection != null) { 
                        ((DbSource)child).ConnectionRef = this.Connection.Name; 
                    }
                } 
                Sources.Add((Source) child);
                // Note [....] 10/9/02 Add function will always gothrough the function builder.
                // Which will take care of the cascading change. We then don't need to call
                // OnQueriesChanged((Source)child); 
            }
        } 
 

 
        private void AddRemoveConstraintMonitor(bool addEventHandler) {
            if (addEventHandler) {
                if (DataTable != null) {
                    DataTable.Constraints.CollectionChanged += new CollectionChangeEventHandler(OnConstraintCollectionChanged); 
                }
            } 
            else { 
                if (DataTable != null) {
                    DataTable.Constraints.CollectionChanged -= new CollectionChangeEventHandler(OnConstraintCollectionChanged); 
                }
            }
        }
 
        bool IDataSourceCommandTarget.CanAddChildOfType(Type childType) {
            return (typeof(DesignColumn).IsAssignableFrom(childType) || 
                    (this.TableType != TableType.DataTable && typeof(Source).IsAssignableFrom(childType)) || 
                    (typeof(DesignRelation).IsAssignableFrom(childType) && this.DesignColumns.Count > 0)
                   ); 
        }

        bool IDataSourceCommandTarget.CanInsertChildOfType(Type childType, object refChild) {
            if (typeof(DesignColumn).IsAssignableFrom(childType)) { 
                return (refChild is DesignColumn);
            } 
            else if (typeof(Source).IsAssignableFrom(childType)) { 
                return (TableType != TableType.DataTable) && (refChild is Source);
            } 
            return false;
        }

        bool IDataSourceCommandTarget.CanRemoveChildren(ICollection children) { 
            bool canRemove = true;
 
            foreach (object child in children) { 
                if (child is DesignColumn) {
                    if (((DesignColumn)child).DesignTable != this ) { 
                        canRemove = false;
                        break;
                    }
                } 
                else if (child is Source) {
                    if (!Sources.Contains((Source) child)) { 
                        canRemove = false; 
                        break;
                    } 
                }
                else if (child is DataAccessor) {
                    if (((DataAccessor)child).DesignTable != this){
                        canRemove = false; 
                        break;
                    } 
                } 
                else {
                    canRemove = false; 
                    break;
                }
            }
            return canRemove; 
        }
 
 
        internal void ConvertTableTypeTo(TableType newTableType) {
            if (newTableType != tableType) { 
                IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                if (componentChangeService != null) {
                    componentChangeService.OnComponentChanging(this, null);
                } 

 
                try { 
                    TableType = newTableType;
                    mainSource = null; 
                    sources = null;
                    mappings = null;
                    provider = String.Empty;
                    OnTableTypeChanged(); 
                }
                finally { 
                    if (componentChangeService != null) { 
                        componentChangeService.OnComponentChanged(this, null, null, null);
                    } 
                }
            }
        }
 
        /// <summary>
        ///  Release event handler 
        /// </summary> 
        protected override void Dispose(bool disposing) {
            if (disposing) { 
                AddRemoveConstraintMonitor(false);
            }
            base.Dispose(disposing);
        } 

        private DbSource EnsureDbSource() { 
            // when throwing InternalException, we should disable Assert Box, since this function is always called by property Set functions 
            // PropertyGrid, and Debug often access properties without paying attention to the TableType. It won't make any bad result, we
            // should ignore those cases. 
            if( this.tableType != TableType.RadTable ) {
                throw new InternalException( null,
                                             VSDExceptions.DataSource.OP_VALID_FOR_RAD_TABLE_ONLY_MSG,
                                             VSDExceptions.DataSource.OP_VALID_FOR_RAD_TABLE_ONLY_CODE, 
                                             false,
                                             false);    // don't assert when accessing properties... 
            } 

            if( this.MainSource == null ) { 
                this.MainSource = new DbSource();
            }

            DbSource dbs = this.mainSource as DbSource; 
            if( dbs == null ) {
                throw new InternalException( null, 
                                             VSDExceptions.DataSource.OP_VALID_FOR_RAD_TABLE_ONLY_MSG, 
                                             VSDExceptions.DataSource.OP_VALID_FOR_RAD_TABLE_ONLY_CODE,
                                             false, 
                                             false);
            }

            if (dbs.DeleteCommand != null && StringUtil.EmptyOrSpace(dbs.DeleteCommand.Name)) { 
                dbs.DeleteCommand.Name = "(DeleteCommand)";
            } 
 
            if (dbs.UpdateCommand != null && StringUtil.EmptyOrSpace(dbs.UpdateCommand.Name)) {
                dbs.UpdateCommand.Name = "(UpdateCommand)"; 
            }

            if (dbs.SelectCommand != null && StringUtil.EmptyOrSpace(dbs.SelectCommand.Name)) {
                dbs.SelectCommand.Name = "(SelectCommand)"; 
            }
 
            if (dbs.InsertCommand != null && StringUtil.EmptyOrSpace(dbs.InsertCommand.Name)) { 
                dbs.InsertCommand.Name = "(InsertCommand)";
            } 

            return dbs;
        }
 
        object IDataSourceCommandTarget.GetObject (int index, bool getSiblingIfOutOfRange) {
            // The object list is composed by Columns, MainSource and then sources 
            // 
            int columnCount = this.DesignColumns.Count;
            int sourceCount = (TableType == TableType.DataTable) ? 
                               0 : this.Sources.Count;
            int count = (TableType == TableType.DataTable) ?
                        columnCount :
                        columnCount + sourceCount + 1; 

            if (count <= 0) { 
                return null; 
            }
            if (!getSiblingIfOutOfRange && (index < 0 || index >= count)) { 
                return null;
            }

            // if the index is larger than count (in the case the last item is removed) 
            // find the index close to it
            if (index >= count) { 
                index = count - 1; 
            }
 
            IList sourceList = Sources as IList;
            Debug.Assert(sourceList != null);

            // for index < 0, get the first available object 
            if (index < 0) {
                if (columnCount > 0) { 
                    return this.DesignColumns[0]; 
                }
                if (this.mainSource != null) { 
                    return this.mainSource;
                }
                if (sourceCount > 0) {
                    return sourceList[0]; 
                }
                Debug.Fail("Should get at least one object"); 
                return null; 
            }
 
            // Now find the right object
            //
            if (index < columnCount) {
                return this.DesignColumns[index]; 
            }
            if (TableType != TableType.DataTable) { 
                index -= columnCount; 
                if (index == 0){
                    return MainSource; 
                }
                index--;
                if (index < sourceCount) {
                    return sourceList[index]; 
                }
            } 
 
            Debug.Fail("Miscalculated on the index. Assign bug to [....]");
            return null; 
        }

        internal ArrayList GetRelatedDataConstraints(ICollection columns, bool uniqueOnly) {
            ArrayList relatedConstraints = new ArrayList(); 

            foreach (Constraint constraint in dataTable.Constraints) { 
                DataColumn[] constraintColumns = null; 
                if (constraint is UniqueConstraint) {
                    constraintColumns = ((UniqueConstraint)constraint).Columns; 
                }
                else if (!uniqueOnly && constraint is ForeignKeyConstraint) {
                    constraintColumns = ((ForeignKeyConstraint)constraint).Columns;
                } 

                if (constraintColumns != null) { 
                    foreach (object  obj in columns) { 
                        if (obj is DesignColumn){
                            DesignColumn designColumn = obj as DesignColumn; 
                            if (((IList)constraintColumns).Contains(designColumn.DataColumn)) {
                                relatedConstraints.Add(constraint);
                                break;
                            } 
                        }
                    } 
                } 
            }
            return relatedConstraints; 
        }

        /// <summary>
        /// Check to see if the given column is part of a foreignkey constraint 
        /// </summary>
        /// <param name="column"></param> 
        /// <returns></returns> 
        internal bool IsForeignKeyConstraint(DataColumn column) {
            foreach (Constraint constraint in dataTable.Constraints) { 
                DataColumn[] constraintColumns = null;
                if (constraint is ForeignKeyConstraint) {
                    constraintColumns = ((ForeignKeyConstraint)constraint).Columns;
                } 

                if (constraintColumns != null && (((IList)constraintColumns).Contains(column))) { 
                    return true; 
                }
            } 
            return false;
        }

        /// <summary> 
        /// Helper function to get the unique relation name
        /// considering all relations in the dataset and all constraints in the table 
        /// </summary> 
        /// <returns></returns>
        internal string GetUniqueRelationName(string proposedName){ 
            return GetUniqueRelationName(proposedName, true, 1);
        }
        internal string GetUniqueRelationName(string proposedName, int startSuffix){
            return GetUniqueRelationName(proposedName, false, startSuffix); 
        }
        /// <summary> 
        /// </summary> 
        /// <param name="proposedName"></param>
        /// <param name="firstTryProposedName">use proposedName as the first trial, otherwise use proposedName+startSuffix</param> 
        /// <param name="startSuffix"></param>
        /// <returns></returns>
        internal string GetUniqueRelationName(string proposedName, bool firstTryProposedName, int startSuffix){
            if (Owner == null){ 
                // undone
                throw new InternalException("Need have DataSource"); 
            } 
            // As we need to check the Name of relations and name of the constraints on the child table
            // 
            SimpleNamedObjectCollection simpleCollection = new SimpleNamedObjectCollection();
            foreach (DesignRelation r in this.Owner.DesignRelations){
                simpleCollection.Add(new SimpleNamedObject(r.Name));
            } 

            foreach(Constraint c in this.DataTable.Constraints){ 
                simpleCollection.Add(new SimpleNamedObject(c.ConstraintName)); 
            }
 
            INameService ns = simpleCollection.GetNameService();
            Debug.Assert(ns != null, "Cannot get name service");

            if (firstTryProposedName){ 
                return ns.CreateUniqueName(simpleCollection, proposedName);
            } 
            else { 
                return ns.CreateUniqueName(simpleCollection,proposedName, startSuffix);
            } 
        }

        int IDataSourceCommandTarget.IndexOf(object child) {
            // The object list is composed by Columns, MainSource and then sources 
            //
            if (child is DesignColumn) { 
                return this.DesignColumns.IndexOf((DesignColumn) child); 
            }
            else if (child is Source && this.TableType != TableType.DataTable){ 
                if (child == this.mainSource){
                    return  this.DesignColumns.Count;
                }
                int sourceIndex = this.Sources.IndexOf((Source) child); 
                if (sourceIndex >= 0){
                    return this.DesignColumns.Count + sourceIndex + 1; 
                } 
            }
            return -1; 
        }

        void IDataSourceInitAfterLoading.InitializeAfterLoading() {
            if (Name == null || Name.Length == 0) { 
                throw new DataSourceSerializationException( SR.GetString( SR.DTDS_NameIsRequired, "RadTable") );
            } 
 
            if (dataTable.DataSet != Owner.DataSet) {
                throw new DataSourceSerializationException( SR.GetString( SR.DTDS_TableNotMatch, Name) ); 
            }
        }

        void IDataSourceCommandTarget.InsertChild(object child, object refChild) { 
            if (refChild == null) {
                ((IDataSourceCommandTarget)this).AddChild(child, true); 
                return; 
            }
 
            Debug.Assert(((IDataSourceCommandTarget)this).CanInsertChildOfType(child.GetType(), refChild));

            if (child is DesignColumn) {
                DesignColumns.InsertBefore (child, refChild); 
            }
            else if (TableType != TableType.DataTable && child is Source) { 
                Sources.InsertBefore(child, refChild); 
            }
        } 

        private bool IsInConstraintCollection(Constraint constraint) {
            return (DataTable != null && DataTable.Constraints[constraint.ConstraintName] == constraint);
        } 

 
 
        /// <summary>
        /// </summary> 


        private void OnConstraintCollectionChanged(object sender, CollectionChangeEventArgs ccevent) {
            if (!inAccessConstraints) { 
                OnConstraintChanged();
            } 
        } 

        /// <summary> 
        /// </summary>
        private void OnConstraintChanged() {
            if (constraintsChanged != null) {
                constraintsChanged(this, new EventArgs()); 
                IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                if (componentChangeService != null) { 
                    componentChangeService.OnComponentChanged(this, null, null, null); 
                }
            } 
        }


        internal void OnTableTypeChanged() { 
            if (tableTypeChanged != null) {
                tableTypeChanged(this, EventArgs.Empty); 
            } 
        }
 



 

 
 
        /// <summary>
        /// Adds primary key to DesignTable if table passed as argument has one and we don't. 
        /// </summary>
        private bool AddPrimaryKeyFromSchemaTable( DataTable schemaTable ) {
            if( schemaTable.PrimaryKey.Length > 0 && this.DataTable.PrimaryKey.Length == 0 ) {
                DataColumn[] pkArr = new DataColumn[schemaTable.PrimaryKey.Length]; 

                for( int i = 0; i < schemaTable.PrimaryKey.Length; i++ ) { 
                    DataColumn schemaPKColumn = schemaTable.PrimaryKey[i]; 
                    if( !this.Mappings.Contains(schemaPKColumn.ColumnName) ) {
                        Debug.Fail( "We should have a mapping for the schema column already!" ); 
                        return false;
                    }

                    string myColumnName = this.Mappings[schemaPKColumn.ColumnName].DataSetColumn; 
                    if( !this.DataTable.Columns.Contains( myColumnName ) ) {
                        Debug.Fail( "There is something wrong with the mappings" ); 
                        return false; 
                    }
                    DataColumn pkColumn = this.DataTable.Columns[myColumnName]; 

                    pkArr[i] = pkColumn;
                }
 
                this.PrimaryKeyColumns = pkArr;
                return true; 
            } 

            return false; 
        }


 

        void IDataSourceXmlSpecialOwner.ReadSpecialItem(string propertyName, XmlNode xmlNode, DataSourceXmlSerializer serializer) { 
            if (propertyName == "Mappings") { 
                string sourceColumn = String.Empty;
                string dataSetColumn = String.Empty; 

                XmlElement xmlElement = xmlNode as XmlElement;
                Debug.Assert(xmlElement!=null);
 
                if (xmlElement != null) {
 
                    foreach (XmlNode itemNode in xmlElement.ChildNodes) { 
                        XmlElement itemElement = itemNode as XmlElement;
 
                        if (itemElement != null && itemElement.LocalName == "Mapping") {
                            XmlAttribute attribute;
                            attribute = itemElement.Attributes["SourceColumn"];
                            if (attribute != null) { 
                                sourceColumn = attribute.InnerText;
                            } 
 
                            attribute = itemElement.Attributes["DataSetColumn"];
                            if (attribute != null) { 
                                dataSetColumn = attribute.InnerText;
                            }

                            DataColumnMapping mapping = new DataColumnMapping(sourceColumn, dataSetColumn); 
                            Mappings.Add(mapping);
                        } 
                    } 
                }
            } 
        }

        void IDataSourceXmlSerializable.ReadXml(XmlElement xmlElement, DataSourceXmlSerializer serializer) {
            if (xmlElement.LocalName == SchemaName.RadTable || xmlElement.LocalName == SchemaName.OldRadTable) { 
                TableType = TableType.RadTable;
                serializer.DeserializeBody(xmlElement, this); 
            } 
        }
 
        // this is a helper function for RemoveChildren
        private DataColumn FindSharedColumn(ICollection dataColumns, ICollection designColumns) {
            foreach (DataColumn dataColumn in dataColumns) {
                foreach (object child in designColumns) { 
                    DesignColumn designColumn = child as DesignColumn;
                    if (designColumn != null && designColumn.DataColumn == dataColumn) { 
                        return dataColumn; 
                    }
                } 
            }
            return null;
        }
 

        // another helper function for RemoveChildren 
        private void RemoveColumnsFromSource( Source source, string[] colsToRemove ) { 
		//bugbug removed
        } 



        void IDataSourceCommandTarget.RemoveChildren(ICollection children) { 
            Debug.Assert(((IDataSourceCommandTarget)this).CanRemoveChildren(children), "invalid child");
            if (this.owner != null) { 
                ArrayList relatedRelations = owner.GetRelatedRelations(new DesignTable[1] { this }); 

                if (relatedRelations.Count > 0) { 
                    int relationAttachedCount = 0;
                    ArrayList removingRelations = new ArrayList();

                    foreach (DesignRelation relation in relatedRelations) { 
                        if (relation.ParentDesignTable == this) {
                            DataColumn matchedColumn = FindSharedColumn(relation.ParentDataColumns, children); 
 
                            if (matchedColumn != null) {
                                relationAttachedCount++; 
                                removingRelations.Add(relation);
                                continue;
                            }
                        } 

                        if (relation.ChildDesignTable == this) { 
                            DataColumn matchedColumn = FindSharedColumn(relation.ChildDataColumns, children); 

                            if (matchedColumn != null) { 
                                relationAttachedCount++;
                                removingRelations.Add(relation);
                            }
                        } 
                    }
 
                    if (relationAttachedCount > 0) { 
                        foreach (DesignRelation rel in removingRelations) {
                            if (rel.Owner != null) { 
                                rel.Owner.DesignRelations.Remove(rel);
                            }
                        }
                    } 
                }
            } 
 
            // Remove unique constraints first
            ArrayList relatedConstraints = GetRelatedDataConstraints(children, true); 

            foreach (UniqueConstraint constraint in relatedConstraints) {
                if (constraint.IsPrimaryKey) {
                    this.PrimaryKeyColumns = null; 
                }
                else { 
                    RemoveConstraint(constraint); 
                }
            } 

            relatedConstraints = GetRelatedDataConstraints(children, false);
            foreach (Constraint constraint in relatedConstraints) {
                RemoveConstraint(constraint); 
            }
 
            ArrayList colsToRemoveFromQuery = new ArrayList(); 

            foreach (object child in children) { 
                if (child is DesignColumn) {
                    DesignColumn column = (DesignColumn)child;
                    string[] mappedNames = DataDesignUtil.MapColumnNames(this.Mappings, new string[] { column.Name }, DataDesignUtil.MappingDirection.DataSetToSource);
 
                    colsToRemoveFromQuery.Add(mappedNames[0]);
 
                    DesignColumns.Remove((DesignColumn)child); 
                    RemoveColumnMapping(column.Name);
                } 
                else if (child is Source) {
                    Sources.Remove((Source)child);
                }
                else if (child is DataAccessor) { 
                    Debug.Assert(children.Count == 1, "We can only delete DataComponent by itself");
                    this.ConvertTableTypeTo(TableType.DataTable); 
                    Debug.Assert(this.DataAccessor == null, "Expected datacomponent was deleted now"); 
                }
            } 

            if (colsToRemoveFromQuery.Count > 0) {
                string[] colsToRemove = (string[])colsToRemoveFromQuery.ToArray(typeof(string));
 
                RemoveColumnsFromSource(MainSource, colsToRemove);
                foreach (Source s in Sources) { 
                    RemoveColumnsFromSource(s, colsToRemove); 
                }
            } 

        }

        /// <summary> 
        ///  This function fixes the problem that we can't remove the primaryKey directly
        /// </summary> 
        internal void RemoveConstraint(Constraint constraint) { 
            Debug.Assert(dataTable != null, "dataTable should be null any time");
 
            IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (componentChangeService != null) {
                componentChangeService.OnComponentChanging(this, null);
            } 
            // componentChangeService.OnComponentChanged will be called in OnConstraintChanged
 
            try { 
                inAccessConstraints = true;
 
                if (dataTable.Constraints.CanRemove(constraint)) {
                    dataTable.Constraints.Remove(constraint);
                }
                else if (dataTable.Constraints.Count == 1) { 
                    if (dataTable.Constraints[0] == constraint) {
                        dataTable.Constraints.Clear(); 
                    } 
                }
                else { 
                    Constraint[] constraints = new Constraint[dataTable.Constraints.Count-1];
                    ArrayList relations = new ArrayList();
                    int i = 0;
                    foreach (Constraint oldConstraint in dataTable.Constraints) { 
                        if (oldConstraint != constraint) {
                            constraints[i++] = oldConstraint; 
                        } 
                    }
 
                    // we should tempoary remove the relations connecting to the constraint here, and add them again later
                    // If we don't do that, after we remove all foreignConstraints on the table, and add back, the relationship between
                    //  the DataRelation and ForeignKeyConstraint will be broken.
                    //  This is a work-around since DataSet don't support us to remove a primary key on the table. 
                    if (Owner != null) {
                        foreach (DataRelation relation in Owner.DataSet.Relations) { 
                            if (relation.ChildTable == dataTable) { 
                                relations.Add(relation);
                            } 
                        }
                        foreach (DataRelation relation in relations) {
                            Owner.DataSet.Relations.Remove(relation);
                        } 
                    }
 
                    dataTable.Constraints.Clear(); 
                    dataTable.Constraints.AddRange(constraints);
                    if (Owner != null) { 
                        foreach (DataRelation relation in relations) {
                            Owner.DataSet.Relations.Add(relation);
                        }
                    } 
                }
 
            } 
            finally {
                inAccessConstraints = false; 
                OnConstraintChanged();
            }
        }
 

        internal void RemoveColumnMapping( string columnName ) { 
 		//bugbug removed 
        }
 

        internal void RemoveKey(UniqueConstraint constraint) {
            ArrayList relatedRelations = new ArrayList();
            foreach (DesignRelation relation in owner.DesignRelations) { 
                DataRelation dataRelation = relation.DataRelation;
                if (dataRelation != null && dataRelation.ParentKeyConstraint == constraint) { 
                    relatedRelations.Add(relation); 
                }
            } 
            foreach (DesignRelation relatedRelation in relatedRelations) {
                owner.DesignRelations.Remove(relatedRelation);
            }
 
            RemoveConstraint(constraint);
        } 
 

 

        /// <summary>
        /// Set type not through the TableType property
        /// </summary> 
        /// <param name="newType"></param>
        internal void SetTypeForUndo(TableType newType) { 
            this.tableType = newType; 
        }
 
        void IDataSourceXmlSpecialOwner.WriteSpecialItem(string propertyName, XmlWriter writer, DataSourceXmlSerializer serializer) {
            if (propertyName == "Mappings") {
                foreach (DataColumnMapping mapping in Mappings) {
                    writer.WriteStartElement(String.Empty, "Mapping", SchemaName.DataSourceNamespace); 
                    writer.WriteAttributeString("SourceColumn", mapping.SourceColumn);
                    writer.WriteAttributeString("DataSetColumn", mapping.DataSetColumn); 
                    writer.WriteEndElement(); 
                }
            } 
        }

        void IDataSourceXmlSerializable.WriteXml(XmlWriter xmlWriter, DataSourceXmlSerializer serializer) {
            switch (TableType) { 
                case TableType.DataTable:
                    break; 
                case TableType.RadTable: 
                    xmlWriter.WriteStartElement(String.Empty, SchemaName.RadTable, SchemaName.DataSourceNamespace);
                    serializer.SerializeBody(xmlWriter, this); 
                    xmlWriter.WriteFullEndElement();
                    break;
            }
        } 

        internal void UpdateColumnMappingDataSetColumnName(string oldName, string newName) { 
		//bugbug removed 
        }
 
        internal void UpdateColumnMappingSourceColumnName(string dataSetColumn, string newSourceColumn) {
 		//bugbug removed
        }
 
        internal string UserTableName {
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_USER_TABLENAME] as string; 
            }
            set { 
                this.dataTable.ExtendedProperties[EXTPROPNAME_USER_TABLENAME] = value;
            }
        }
 
        internal string GeneratorRunFillName {
            get { 
                return generatorRunFillName; 
            }
 
            set {
                generatorRunFillName = value;
            }
        } 

        internal string GeneratorTablePropName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLEPROPNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLEPROPNAME] = value;
            }
        } 

        internal string GeneratorTableVarName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLEVARNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLEVARNAME] = value;
            }
        } 

        internal string GeneratorTableClassName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLECLASSNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLECLASSNAME] = value;
            }
        } 

        internal string GeneratorRowClassName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCLASSNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCLASSNAME] = value;
            }
        } 

        internal string GeneratorRowEvHandlerName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWEVHANDLERNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWEVHANDLERNAME] = value;
            }
        } 

        internal string GeneratorRowEvArgName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWEVARGNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWEVARGNAME] = value;
            }
        } 

        internal string GeneratorRowChangingName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCHANGINGNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCHANGINGNAME] = value;
            }
        } 

        internal string GeneratorRowChangedName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCHANGEDNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCHANGEDNAME] = value;
            }
        } 

        internal string GeneratorRowDeletingName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWDELETINGNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWDELETINGNAME] = value;
            }
        } 

        internal string GeneratorRowDeletedName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWDELETEDNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWDELETEDNAME] = value;
            }
        } 

        internal override StringCollection NamingPropertyNames { 
            get { 
                return namingPropNames;
            } 
        }

        [DataSourceXmlAttribute(), Browsable(false), DefaultValue(null)]
        public string GeneratorDataComponentClassName { 
            get {
                return generatorDataComponentClassName; 
            } 
            set {
                generatorDataComponentClassName = value; 
            }
        }

//        [DataSourceXmlAttribute(), Browsable(false), DefaultValue(null)] 
//        public string GeneratorDataComponentInterfaceName {
//            get { 
//                return generatorDataComponentInterfaceName; 
//            }
//            set { 
//                generatorDataComponentInterfaceName = value;
//            }
//        }
 
        [DataSourceXmlAttribute(), Browsable(false), DefaultValue(null)]
        public string UserDataComponentName { 
            get { 
                return userDataComponentName;
            } 
            set {
                userDataComponentName = value;
            }
        } 

        // IDataSourceRenamableObject implementation 
        [Browsable(false)] 
        public override string GeneratorName {
            get { 
                return GeneratorTablePropName;
            }
        }
 
        /// <summary>
        /// This class is used during the code gen. The caller 
        /// needs to set the value before use it. 
        /// </summary>
        internal CodeGenPropertyCache PropertyCache { 
            get{
                Debug.Assert(this.codeGenPropertyCache != null, "You should assign PropertyCache before use it");
                return this.codeGenPropertyCache;
            } 
            set{
                this.codeGenPropertyCache = value; 
            } 
        }
 
        /// <summary>
        /// </summary>
        internal class CodeGenPropertyCache {
            private DesignTable designTable; 
            private Type connectionType;
            private Type transactionType; 
            private Type adapterType; 
            private string tamAdapterPropName;
            private string tamAdapterVarName; 

            internal Type AdapterType {
                get {
                    if (adapterType == null){ 
                        if (this.designTable == null || this.designTable.Connection == null || designTable.Connection.Provider == null) {
                            return null; 
                        } 
                        System.Data.Common.DbProviderFactory providerFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
                        if (providerFactory != null) { 
                            DataAdapter adapter = providerFactory.CreateDataAdapter();
                            if (adapter != null){
                                adapterType = adapter.GetType();
                            } 
                        }
                    } 
                    return adapterType; 
                }
            } 
            internal Type ConnectionType {
                get {
                    if (connectionType == null) {
                        if (this.designTable != null && this.designTable.Connection != null) { 
                            IDbConnection connection = this.designTable.Connection.CreateEmptyDbConnection();
                            if (connection != null) { 
                                connectionType = connection.GetType(); 
                            }
                        } 
                    }
                    return connectionType;
                }
            } 
            internal Type TransactionType {
                get { 
                    if (transactionType == null){ 
                        if (this.designTable == null || this.designTable.Connection == null || designTable.Connection.Provider == null) {
                            return null; 
                        }
                        System.Data.Common.DbProviderFactory providerFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
                        if (providerFactory != null) {
                            Type commandType = providerFactory.CreateCommand().GetType(); 
                            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(commandType)) {
                                if (StringUtil.EqualValue(pd.Name, "Transaction")) { 
                                    transactionType = pd.PropertyType; 
                                    break;
                                } 
                            }
                        }
                        if (transactionType == null) {
                            transactionType = typeof(System.Data.IDbTransaction); 
                        }
                    } 
                    return transactionType; 
                }
            } 

            /// <summary>
            /// Used by TableAdapterManager to generate TableAdapter property name
            /// </summary> 
            internal string TAMAdapterPropName {
                get { 
                    return tamAdapterPropName; 
                }
                set { 
                    tamAdapterPropName = value;
                }
            }
 
            /// <summary>
            /// Used by TableAdapterManager to generate TableAdapter variable name 
            /// </summary> 
            internal string TAMAdapterVarName {
                get { 
                    return tamAdapterVarName;
                }
                set {
                    tamAdapterVarName = value; 
                }
            } 
 

            internal CodeGenPropertyCache(DesignTable designTable) { 
                this.designTable = designTable;
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
namespace System.Data.Design{ 

    using System; 
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Data.Common; 
 	using System.Design;
	using System.Diagnostics; 
	using System.IO;
    using System.Xml;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary; 
    using System.Windows.Forms;
    using System.Text; 
 

    internal class DesignTable : DataSourceComponent, IDataSourceNamedObject, IDataSourceXmlSerializable, 
                    IDataSourceXmlSpecialOwner, IDataSourceInitAfterLoading, IDataSourceCommandTarget {

        private TableType  tableType;
        private DataTable   dataTable; 
        private DataAccessor dataAccessor;
        private DesignColumnCollection designColumns; 
        private DesignDataSource owner; 
        private TypeAttributes dataAccessorModifier = TypeAttributes.Public;
 
        private Source mainSource;
        private SourceCollection sources;
        private DataColumnMappingCollection mappings;
        private bool webServiceAttribute; 
        private string webServiceNamespace;
        private string webServiceDescription; 
 
        private string      provider;
        private string      generatorRunFillName; 
        private string      baseClass;
        private string      dataAccessorName;

        private event EventHandler      tableTypeChanged; 
        private event EventHandler      constraintsChanged;
        private bool                    inAccessConstraints; 
        private event EventHandler dataAccessorChanged; 
        private event EventHandler dataAccessorChanging;
 
        private const string DATATABLE_NAMEROOT = "DataTable";
        private const string RADTABLE_NAMEROOT = "DataTable";
        private const string KEY_NAMEROOT = "Key";
        private const string PRIMARYKEY_PROPERTY = "PrimaryKey"; 
        internal const string MAINSOURCE_PROPERTY = "MainSource";
        private const string MAINSOURCE_NAME = "Fill"; 
        internal const string NAME_PROPERTY = "Name"; 

        private string generatorDataComponentClassName = null; 
        private string userDataComponentName = null;
        private CodeGenPropertyCache codeGenPropertyCache;

        private StringCollection namingPropNames = new StringCollection(); 
        internal static string EXTPROPNAME_USER_TABLENAME = "Generator_UserTableName";
        internal static string EXTPROPNAME_GENERATOR_TABLEPROPNAME = "Generator_TablePropName"; 
        internal static string EXTPROPNAME_GENERATOR_TABLEVARNAME = "Generator_TableVarName"; 
        internal static string EXTPROPNAME_GENERATOR_TABLECLASSNAME = "Generator_TableClassName";
        internal static string EXTPROPNAME_GENERATOR_ROWCLASSNAME = "Generator_RowClassName"; 
        internal static string EXTPROPNAME_GENERATOR_ROWEVHANDLERNAME = "Generator_RowEvHandlerName";
        internal static string EXTPROPNAME_GENERATOR_ROWEVARGNAME = "Generator_RowEvArgName";
        internal static string EXTPROPNAME_GENERATOR_ROWCHANGINGNAME = "Generator_RowChangingName";
        internal static string EXTPROPNAME_GENERATOR_ROWCHANGEDNAME = "Generator_RowChangedName"; 
        internal static string EXTPROPNAME_GENERATOR_ROWDELETINGNAME = "Generator_RowDeletingName";
        internal static string EXTPROPNAME_GENERATOR_ROWDELETEDNAME = "Generator_RowDeletedName"; 
 
        /// <summary>
        ///  this is only used by serializer... 
        /// </summary>
        public DesignTable() : this(null, TableType.DataTable) {
        }
 
        /// <summary>
        /// </summary> 
        /// <param name="dataTable"></param> 
        public DesignTable(DataTable dataTable) : this(dataTable, TableType.DataTable) {
        } 

        /// <summary>
        /// </summary>
        /// <param name="dataTable"></param> 
        /// <param name="tableType"></param>
        public DesignTable(DataTable dataTable, TableType tableType){ 
            if (dataTable == null) { 
                this.dataTable = new DataTable();
                this.dataTable.Locale = System.Globalization.CultureInfo.InvariantCulture; 
            }
            else {
                this.dataTable = dataTable;
            } 

            this.TableType = tableType; 
            AddRemoveConstraintMonitor(true); 
            namingPropNames.AddRange(new string[] { "typedPlural", "typedName" });
        } 


        public DesignTable(DataTable dataTable, TableType tableType, DataColumnMappingCollection mappings):
        this(dataTable, tableType) { 
            this.mappings = mappings;
        } 
 
        /// <summary>
        /// Used by 'DataAccessor' 
        /// </summary>
        /// <value></value>
        [
            DataSourceXmlAttribute(), 
            Browsable(false),
        ] 
        public string BaseClass { 
            get {
                if (StringUtil.NotEmptyAfterTrim(baseClass)) { 
                    return baseClass;
                }
                return DataAccessor.DEFAULT_BASE_CLASS;
            } 
            set {
                baseClass = value; 
            } 
        }
 

        public IDesignConnection Connection {
            get{
                if (this.TableType == TableType.RadTable){ 
                    DbSource dbs = EnsureDbSource();
                    return dbs.Connection; 
                } 
                return null;
            } 
            set {
                if (this.TableType == TableType.RadTable){
                    DbSource dbs = EnsureDbSource();
                    dbs.Connection = value; 
                }
                else { 
                    Debug.Assert( value == null, "You should not try to set the connection property to non-RadTableObject" ); 
                }
            } 
        }

        internal event EventHandler ConstraintChanged {
            add { 
                constraintsChanged += value;
            } 
            remove { 
                constraintsChanged -= value;
            } 
        }

        /// <summary>
        /// this property can be accessed by DataSourceRootDesigner to Add/Delete/Replace a component, 
        /// DesignTableDesigner will catch the events and add/remove the component to the designer host
        /// // and update the UI. 
        /// </summary> 
        /// <value></value>
        internal DataAccessor DataAccessor { 
            get {
                return dataAccessor;
            }
            set { 
                if (dataAccessorChanging != null) {
                    dataAccessorChanging(this, new EventArgs()); 
                } 

                this.dataAccessor = value; 

                if (dataAccessorChanged != null) {
                    dataAccessorChanged(this, new EventArgs());
                } 
            }
        } 
 
        internal event EventHandler DataAccessorChanged {
            add { 
                dataAccessorChanged += value;
            }
            remove {
                dataAccessorChanged -= value; 
            }
        } 
 
        internal event EventHandler DataAccessorChanging {
            add { 
                dataAccessorChanging += value;
            }
            remove {
                dataAccessorChanging -= value; 
            }
        } 
 

        /// <summary> 
        /// Used by 'DataAccessor'
        /// </summary>
        /// <value></value>
        [ 
            DataSourceXmlAttribute(),
            Browsable(false), 
        ] 
        public string DataAccessorName {
            get { 
                if (StringUtil.NotEmptyAfterTrim(dataAccessorName)) {
                    return dataAccessorName;
                }
                return this.Name + DataAccessor.DEFAULT_NAME_POSTFIX; 
            }
            set { 
                dataAccessorName = value; 
            }
        } 

        /// <summary>
        /// </summary>
        [Browsable(false)] 
        public DataTable DataTable {
            get{ 
                return dataTable; 
            }
            set { 
                if (dataTable != value) {

                    if (dataTable != null) {
                        AddRemoveConstraintMonitor(false); 
                    }
 
                    dataTable = value; 

                    if (dataTable != null) { 
                        AddRemoveConstraintMonitor(true);
                    }
                }
            } 
        }
 
        [ 
            DefaultValue(null)
        ] 
        public DbSourceCommand DeleteCommand {
            get {
                DbSource dbs = EnsureDbSource();
                return dbs.DeleteCommand; 
            }
            set { 
                DbSource dbs = EnsureDbSource(); 
                dbs.DeleteCommand = value;
            } 
        }

        /// <summary>
        /// </summary> 
        [Browsable(false)]
        public DesignColumnCollection DesignColumns { 
            get{ 
                if (designColumns == null){
                    designColumns = new DesignColumnCollection(this); 
                }
                return designColumns;
            }
        } 

 
 
        /// <summary>
        ///  return the object supports external properties 
        /// </summary>
        protected override object ExternalPropertyHost {
            get {
                return dataTable; 
            }
        } 
 
        internal bool HasAnyUpdateCommand{
            get { 
                if (TableType == TableType.RadTable
                    && this.MainSource != null && this.MainSource is DbSource
                    && (((DbSource)this.MainSource).CommandOperation == CommandOperation.Select)
                    && (this.DeleteCommand != null || this.InsertCommand != null || this.UpdateCommand != null)) { 
                    return true;
                } 
                else { 
                    return false;
                } 
            }
        }

        internal bool HasAnyExpressionColumn { 
            get {
                DataTable table = this.DataTable; 
                foreach (DataColumn column in table.Columns) { 
                    if (column.Expression != null &&  column.Expression.Length > 0) {
                        return true; 
                    }
                }
                return false;
            } 
        }
 
        [ 
            DefaultValue(null)
        ] 
        public DbSourceCommand InsertCommand {
            get {
                DbSource dbs = EnsureDbSource();
                return dbs.InsertCommand; 
            }
            set { 
                DbSource dbs = EnsureDbSource(); 
                dbs.InsertCommand = value;
            } 
        }

        [Browsable(false), DataSourceXmlSubItem(Name=MAINSOURCE_PROPERTY, ItemType=typeof(Source))]
        public Source MainSource { 
            get {
 
                if (this.mainSource == null) { 
                    DbSource dbsource = new DbSource ();
                    if (this.Owner != null){ 
                        dbsource.Connection =  this.Owner.DefaultConnection;
                    }
                    this.MainSource = dbsource;
                } 

                return this.mainSource; 
            } 
            set {
                if( this.mainSource != null ) { 
                    this.mainSource.Owner = null;
                }

                this.mainSource = value; 

                if( value != null ) { 
                    this.mainSource.Owner = this; 
                    if (StringUtil.EmptyOrSpace(this.mainSource.Name)){
                        this.mainSource.Name = MAINSOURCE_NAME; 
                    }
                }
            }
        } 

 
        /// <summary> 
        ///  ATTENTION: Don't modify the mapping directly until you know how to handle undo/redo.
        ///   Call AddColumnMapping, ClearColumnMapping and RemoveColumnMapping, those functions know how to add UndoUnit for the operation 
        /// Please talk to [....] or lifengl about it
        /// </summary>
        [
            Browsable(false), 
            DataSourceXmlElement(Name="Mappings", SpecialWay=true),
            // 
 
        ]
        public DataColumnMappingCollection Mappings { 
            get{
                if( this.mappings == null ) {
                    this.mappings = new DataColumnMappingCollection();
                } 

                return this.mappings; 
            } 
            set {   // this is only used by undo
                this.mappings = value; 
            }
        }

        private bool ShouldSerializeMappings() { 
            return (this.mappings != null && (this.mappings.Count > 0));
        } 
 
        [
            DefaultValue(TypeAttributes.Public), 
            DataSourceXmlAttribute()
        ]
        public TypeAttributes DataAccessorModifier {
            get { 
                return this.dataAccessorModifier;
            } 
            set { 
                this.dataAccessorModifier = value;
            } 
        }

        /// <summary>
        /// Property need for all types 
        /// </summary>
        [ 
            DefaultValue(""), 
            DataSourceXmlAttribute(),
            MergableProperty(false) 
        ]
        public string Name{
            get{
                return dataTable.TableName; 
            }
            set { 
                Debug.Assert(dataTable != null, "dataTable is null"); 
                if (dataTable.TableName != value) {
                    if (this.CollectionParent != null) { 
                        CollectionParent.ValidateUniqueName (this, value);
                    }
                    dataTable.TableName = value;
                } 
            }
        } 
 
        internal DesignDataSource Owner {
            get { 
                return this.owner;
            }
            set {
                if (this.owner != value) { 
                    string oldTarget = (this.owner != null) ? this.owner.DataSet.Namespace : SchemaName.DataSourceTempTargetNamespace;
                    // if there is a new owner, set newTarget to null, so it would follow the change of the owner. 
                    // 
                    string newTarget = (value != null) ? null : SchemaName.DataSourceTempTargetNamespace;
 
                    this.owner = value;
                }
            }
        } 

        public DbSourceParameterCollection Parameters { 
            get { 
                DbSource source = MainSource as DbSource;
                if( source != null ) { 
                    // undone: we don't want this assert until the wizard is working
                    //Debug.Assert(source.SelectCommand != null, "SelectCommand should never be null");
                    if( source.SelectCommand != null ) {
                        return source.SelectCommand.Parameters; 
                    }
                } 
 
                return null;
            } 
        }

        private bool ShouldSerializeParameters() {
            if (TableType != TableType.RadTable) { 
                return false;
            } 
            else { 
                DbSourceParameterCollection parameters = Parameters;
                return (null != parameters && (0 < parameters.Count)); 
            }
        }

 

        [Browsable (false)] 
        public DataColumn[] PrimaryKeyColumns { 
            get {
                return this.DataTable.PrimaryKey; 
            }
            set {
                AddRemoveConstraintMonitor(false);
                try { 
                    this.SetPropertyValue(PRIMARYKEY_PROPERTY, value);
                    OnConstraintChanged(); 
                } 
                finally {
                    AddRemoveConstraintMonitor(true); 
                }
            }
        }
 
        /// <summary>
        /// Property need for RadTable type 
        /// </summary> 
        [
            DefaultValue(null), 
            DataSourceXmlAttribute(),
            Browsable (false)
        ]
        public string Provider{ 
            get{
                return provider; 
            } 
            set {
                provider = value; 
            }
        }

        [Browsable(false)] 
        public string PublicTypeName {
            get { 
                string name; 

                switch (tableType){ 
                    case (TableType.DataTable):
                        name = DATATABLE_NAMEROOT;
                        break;
 
                    case (TableType.RadTable):
                        name = RADTABLE_NAMEROOT; 
                        break; 

                    default: 
                        Debug.Fail("UNDONE: NYI");
                        return null;
                }
                return name; 
            }
        } 
 
        [
            Browsable(false) 
        ]
        public DbSourceCommand SelectCommand {
            get {
                DbSource dbs = EnsureDbSource(); 
                return dbs.SelectCommand;
            } 
            set { 
                DbSource dbs = EnsureDbSource();
                dbs.SelectCommand = value; 
            }
        }

 
        /// <summary>
        /// </summary> 
 
        /// <summary>
        /// </summary> 
        [
            DataSourceXmlSubItem(typeof(Source)),
            Browsable(false)
        ] 
        public SourceCollection Sources{
            get{ 
                if( this.sources == null ) { 
                    this.sources = new SourceCollection(this);
                } 

                return this.sources;
            }
        } 

        /// <summary> 
        /// </summary> 
        [Browsable(false)]
        public TableType TableType { 
            get {
                return tableType;
            }
            set { 
                tableType = value;
                if (tableType == TableType.RadTable) { 
                    this.DataAccessor = new DataAccessor(this); 
                }
                else { 
                    this.DataAccessor = null;
                }
            }
        } 

        internal event EventHandler TableTypeChanged { 
            add { 
                tableTypeChanged += value;
            } 
            remove {
                tableTypeChanged -= value;
            }
        } 

        [ 
            DefaultValue(null) 
        ]
        public DbSourceCommand UpdateCommand { 
            get {
                DbSource dbs = EnsureDbSource();
                return dbs.UpdateCommand;
            } 
            set {
                DbSource dbs = EnsureDbSource(); 
                dbs.UpdateCommand = value; 
            }
        } 

        [
            DataSourceXmlAttribute(ItemType=typeof(bool)),
            Browsable(false), 
            DefaultValue(false),
        ] 
        public bool WebServiceAttribute{ 
            get{
                return webServiceAttribute; 
            }
            set{
                webServiceAttribute = value;
            } 
        }
 
        [ 
            DataSourceXmlAttribute(),
            Browsable(false), 
        ]
        public string WebServiceDescription{
            get{
                return webServiceDescription; 
            }
            set{ 
                webServiceDescription = value; 
            }
        } 

        [
            DataSourceXmlAttribute(),
            Browsable(false), 
        ]
        public string WebServiceNamespace{ 
            get{ 
                return webServiceNamespace;
            } 
            set{
                webServiceNamespace = value;
            }
        } 

        void IDataSourceCommandTarget.AddChild(object child, bool fixName) { 
            Debug.Assert(((IDataSourceCommandTarget)this).CanAddChildOfType(child.GetType()), "invalid child"); 

            if (child is DesignColumn) { 
                DesignColumns.Add ((DesignColumn)child);
            }
            else if (child is Source) {
                if (child is DbSource) { 
                    ((DbSource) child).Connection = this.Connection;
                    if (this.Connection != null) { 
                        ((DbSource)child).ConnectionRef = this.Connection.Name; 
                    }
                } 
                Sources.Add((Source) child);
                // Note [....] 10/9/02 Add function will always gothrough the function builder.
                // Which will take care of the cascading change. We then don't need to call
                // OnQueriesChanged((Source)child); 
            }
        } 
 

 
        private void AddRemoveConstraintMonitor(bool addEventHandler) {
            if (addEventHandler) {
                if (DataTable != null) {
                    DataTable.Constraints.CollectionChanged += new CollectionChangeEventHandler(OnConstraintCollectionChanged); 
                }
            } 
            else { 
                if (DataTable != null) {
                    DataTable.Constraints.CollectionChanged -= new CollectionChangeEventHandler(OnConstraintCollectionChanged); 
                }
            }
        }
 
        bool IDataSourceCommandTarget.CanAddChildOfType(Type childType) {
            return (typeof(DesignColumn).IsAssignableFrom(childType) || 
                    (this.TableType != TableType.DataTable && typeof(Source).IsAssignableFrom(childType)) || 
                    (typeof(DesignRelation).IsAssignableFrom(childType) && this.DesignColumns.Count > 0)
                   ); 
        }

        bool IDataSourceCommandTarget.CanInsertChildOfType(Type childType, object refChild) {
            if (typeof(DesignColumn).IsAssignableFrom(childType)) { 
                return (refChild is DesignColumn);
            } 
            else if (typeof(Source).IsAssignableFrom(childType)) { 
                return (TableType != TableType.DataTable) && (refChild is Source);
            } 
            return false;
        }

        bool IDataSourceCommandTarget.CanRemoveChildren(ICollection children) { 
            bool canRemove = true;
 
            foreach (object child in children) { 
                if (child is DesignColumn) {
                    if (((DesignColumn)child).DesignTable != this ) { 
                        canRemove = false;
                        break;
                    }
                } 
                else if (child is Source) {
                    if (!Sources.Contains((Source) child)) { 
                        canRemove = false; 
                        break;
                    } 
                }
                else if (child is DataAccessor) {
                    if (((DataAccessor)child).DesignTable != this){
                        canRemove = false; 
                        break;
                    } 
                } 
                else {
                    canRemove = false; 
                    break;
                }
            }
            return canRemove; 
        }
 
 
        internal void ConvertTableTypeTo(TableType newTableType) {
            if (newTableType != tableType) { 
                IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                if (componentChangeService != null) {
                    componentChangeService.OnComponentChanging(this, null);
                } 

 
                try { 
                    TableType = newTableType;
                    mainSource = null; 
                    sources = null;
                    mappings = null;
                    provider = String.Empty;
                    OnTableTypeChanged(); 
                }
                finally { 
                    if (componentChangeService != null) { 
                        componentChangeService.OnComponentChanged(this, null, null, null);
                    } 
                }
            }
        }
 
        /// <summary>
        ///  Release event handler 
        /// </summary> 
        protected override void Dispose(bool disposing) {
            if (disposing) { 
                AddRemoveConstraintMonitor(false);
            }
            base.Dispose(disposing);
        } 

        private DbSource EnsureDbSource() { 
            // when throwing InternalException, we should disable Assert Box, since this function is always called by property Set functions 
            // PropertyGrid, and Debug often access properties without paying attention to the TableType. It won't make any bad result, we
            // should ignore those cases. 
            if( this.tableType != TableType.RadTable ) {
                throw new InternalException( null,
                                             VSDExceptions.DataSource.OP_VALID_FOR_RAD_TABLE_ONLY_MSG,
                                             VSDExceptions.DataSource.OP_VALID_FOR_RAD_TABLE_ONLY_CODE, 
                                             false,
                                             false);    // don't assert when accessing properties... 
            } 

            if( this.MainSource == null ) { 
                this.MainSource = new DbSource();
            }

            DbSource dbs = this.mainSource as DbSource; 
            if( dbs == null ) {
                throw new InternalException( null, 
                                             VSDExceptions.DataSource.OP_VALID_FOR_RAD_TABLE_ONLY_MSG, 
                                             VSDExceptions.DataSource.OP_VALID_FOR_RAD_TABLE_ONLY_CODE,
                                             false, 
                                             false);
            }

            if (dbs.DeleteCommand != null && StringUtil.EmptyOrSpace(dbs.DeleteCommand.Name)) { 
                dbs.DeleteCommand.Name = "(DeleteCommand)";
            } 
 
            if (dbs.UpdateCommand != null && StringUtil.EmptyOrSpace(dbs.UpdateCommand.Name)) {
                dbs.UpdateCommand.Name = "(UpdateCommand)"; 
            }

            if (dbs.SelectCommand != null && StringUtil.EmptyOrSpace(dbs.SelectCommand.Name)) {
                dbs.SelectCommand.Name = "(SelectCommand)"; 
            }
 
            if (dbs.InsertCommand != null && StringUtil.EmptyOrSpace(dbs.InsertCommand.Name)) { 
                dbs.InsertCommand.Name = "(InsertCommand)";
            } 

            return dbs;
        }
 
        object IDataSourceCommandTarget.GetObject (int index, bool getSiblingIfOutOfRange) {
            // The object list is composed by Columns, MainSource and then sources 
            // 
            int columnCount = this.DesignColumns.Count;
            int sourceCount = (TableType == TableType.DataTable) ? 
                               0 : this.Sources.Count;
            int count = (TableType == TableType.DataTable) ?
                        columnCount :
                        columnCount + sourceCount + 1; 

            if (count <= 0) { 
                return null; 
            }
            if (!getSiblingIfOutOfRange && (index < 0 || index >= count)) { 
                return null;
            }

            // if the index is larger than count (in the case the last item is removed) 
            // find the index close to it
            if (index >= count) { 
                index = count - 1; 
            }
 
            IList sourceList = Sources as IList;
            Debug.Assert(sourceList != null);

            // for index < 0, get the first available object 
            if (index < 0) {
                if (columnCount > 0) { 
                    return this.DesignColumns[0]; 
                }
                if (this.mainSource != null) { 
                    return this.mainSource;
                }
                if (sourceCount > 0) {
                    return sourceList[0]; 
                }
                Debug.Fail("Should get at least one object"); 
                return null; 
            }
 
            // Now find the right object
            //
            if (index < columnCount) {
                return this.DesignColumns[index]; 
            }
            if (TableType != TableType.DataTable) { 
                index -= columnCount; 
                if (index == 0){
                    return MainSource; 
                }
                index--;
                if (index < sourceCount) {
                    return sourceList[index]; 
                }
            } 
 
            Debug.Fail("Miscalculated on the index. Assign bug to [....]");
            return null; 
        }

        internal ArrayList GetRelatedDataConstraints(ICollection columns, bool uniqueOnly) {
            ArrayList relatedConstraints = new ArrayList(); 

            foreach (Constraint constraint in dataTable.Constraints) { 
                DataColumn[] constraintColumns = null; 
                if (constraint is UniqueConstraint) {
                    constraintColumns = ((UniqueConstraint)constraint).Columns; 
                }
                else if (!uniqueOnly && constraint is ForeignKeyConstraint) {
                    constraintColumns = ((ForeignKeyConstraint)constraint).Columns;
                } 

                if (constraintColumns != null) { 
                    foreach (object  obj in columns) { 
                        if (obj is DesignColumn){
                            DesignColumn designColumn = obj as DesignColumn; 
                            if (((IList)constraintColumns).Contains(designColumn.DataColumn)) {
                                relatedConstraints.Add(constraint);
                                break;
                            } 
                        }
                    } 
                } 
            }
            return relatedConstraints; 
        }

        /// <summary>
        /// Check to see if the given column is part of a foreignkey constraint 
        /// </summary>
        /// <param name="column"></param> 
        /// <returns></returns> 
        internal bool IsForeignKeyConstraint(DataColumn column) {
            foreach (Constraint constraint in dataTable.Constraints) { 
                DataColumn[] constraintColumns = null;
                if (constraint is ForeignKeyConstraint) {
                    constraintColumns = ((ForeignKeyConstraint)constraint).Columns;
                } 

                if (constraintColumns != null && (((IList)constraintColumns).Contains(column))) { 
                    return true; 
                }
            } 
            return false;
        }

        /// <summary> 
        /// Helper function to get the unique relation name
        /// considering all relations in the dataset and all constraints in the table 
        /// </summary> 
        /// <returns></returns>
        internal string GetUniqueRelationName(string proposedName){ 
            return GetUniqueRelationName(proposedName, true, 1);
        }
        internal string GetUniqueRelationName(string proposedName, int startSuffix){
            return GetUniqueRelationName(proposedName, false, startSuffix); 
        }
        /// <summary> 
        /// </summary> 
        /// <param name="proposedName"></param>
        /// <param name="firstTryProposedName">use proposedName as the first trial, otherwise use proposedName+startSuffix</param> 
        /// <param name="startSuffix"></param>
        /// <returns></returns>
        internal string GetUniqueRelationName(string proposedName, bool firstTryProposedName, int startSuffix){
            if (Owner == null){ 
                // undone
                throw new InternalException("Need have DataSource"); 
            } 
            // As we need to check the Name of relations and name of the constraints on the child table
            // 
            SimpleNamedObjectCollection simpleCollection = new SimpleNamedObjectCollection();
            foreach (DesignRelation r in this.Owner.DesignRelations){
                simpleCollection.Add(new SimpleNamedObject(r.Name));
            } 

            foreach(Constraint c in this.DataTable.Constraints){ 
                simpleCollection.Add(new SimpleNamedObject(c.ConstraintName)); 
            }
 
            INameService ns = simpleCollection.GetNameService();
            Debug.Assert(ns != null, "Cannot get name service");

            if (firstTryProposedName){ 
                return ns.CreateUniqueName(simpleCollection, proposedName);
            } 
            else { 
                return ns.CreateUniqueName(simpleCollection,proposedName, startSuffix);
            } 
        }

        int IDataSourceCommandTarget.IndexOf(object child) {
            // The object list is composed by Columns, MainSource and then sources 
            //
            if (child is DesignColumn) { 
                return this.DesignColumns.IndexOf((DesignColumn) child); 
            }
            else if (child is Source && this.TableType != TableType.DataTable){ 
                if (child == this.mainSource){
                    return  this.DesignColumns.Count;
                }
                int sourceIndex = this.Sources.IndexOf((Source) child); 
                if (sourceIndex >= 0){
                    return this.DesignColumns.Count + sourceIndex + 1; 
                } 
            }
            return -1; 
        }

        void IDataSourceInitAfterLoading.InitializeAfterLoading() {
            if (Name == null || Name.Length == 0) { 
                throw new DataSourceSerializationException( SR.GetString( SR.DTDS_NameIsRequired, "RadTable") );
            } 
 
            if (dataTable.DataSet != Owner.DataSet) {
                throw new DataSourceSerializationException( SR.GetString( SR.DTDS_TableNotMatch, Name) ); 
            }
        }

        void IDataSourceCommandTarget.InsertChild(object child, object refChild) { 
            if (refChild == null) {
                ((IDataSourceCommandTarget)this).AddChild(child, true); 
                return; 
            }
 
            Debug.Assert(((IDataSourceCommandTarget)this).CanInsertChildOfType(child.GetType(), refChild));

            if (child is DesignColumn) {
                DesignColumns.InsertBefore (child, refChild); 
            }
            else if (TableType != TableType.DataTable && child is Source) { 
                Sources.InsertBefore(child, refChild); 
            }
        } 

        private bool IsInConstraintCollection(Constraint constraint) {
            return (DataTable != null && DataTable.Constraints[constraint.ConstraintName] == constraint);
        } 

 
 
        /// <summary>
        /// </summary> 


        private void OnConstraintCollectionChanged(object sender, CollectionChangeEventArgs ccevent) {
            if (!inAccessConstraints) { 
                OnConstraintChanged();
            } 
        } 

        /// <summary> 
        /// </summary>
        private void OnConstraintChanged() {
            if (constraintsChanged != null) {
                constraintsChanged(this, new EventArgs()); 
                IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                if (componentChangeService != null) { 
                    componentChangeService.OnComponentChanged(this, null, null, null); 
                }
            } 
        }


        internal void OnTableTypeChanged() { 
            if (tableTypeChanged != null) {
                tableTypeChanged(this, EventArgs.Empty); 
            } 
        }
 



 

 
 
        /// <summary>
        /// Adds primary key to DesignTable if table passed as argument has one and we don't. 
        /// </summary>
        private bool AddPrimaryKeyFromSchemaTable( DataTable schemaTable ) {
            if( schemaTable.PrimaryKey.Length > 0 && this.DataTable.PrimaryKey.Length == 0 ) {
                DataColumn[] pkArr = new DataColumn[schemaTable.PrimaryKey.Length]; 

                for( int i = 0; i < schemaTable.PrimaryKey.Length; i++ ) { 
                    DataColumn schemaPKColumn = schemaTable.PrimaryKey[i]; 
                    if( !this.Mappings.Contains(schemaPKColumn.ColumnName) ) {
                        Debug.Fail( "We should have a mapping for the schema column already!" ); 
                        return false;
                    }

                    string myColumnName = this.Mappings[schemaPKColumn.ColumnName].DataSetColumn; 
                    if( !this.DataTable.Columns.Contains( myColumnName ) ) {
                        Debug.Fail( "There is something wrong with the mappings" ); 
                        return false; 
                    }
                    DataColumn pkColumn = this.DataTable.Columns[myColumnName]; 

                    pkArr[i] = pkColumn;
                }
 
                this.PrimaryKeyColumns = pkArr;
                return true; 
            } 

            return false; 
        }


 

        void IDataSourceXmlSpecialOwner.ReadSpecialItem(string propertyName, XmlNode xmlNode, DataSourceXmlSerializer serializer) { 
            if (propertyName == "Mappings") { 
                string sourceColumn = String.Empty;
                string dataSetColumn = String.Empty; 

                XmlElement xmlElement = xmlNode as XmlElement;
                Debug.Assert(xmlElement!=null);
 
                if (xmlElement != null) {
 
                    foreach (XmlNode itemNode in xmlElement.ChildNodes) { 
                        XmlElement itemElement = itemNode as XmlElement;
 
                        if (itemElement != null && itemElement.LocalName == "Mapping") {
                            XmlAttribute attribute;
                            attribute = itemElement.Attributes["SourceColumn"];
                            if (attribute != null) { 
                                sourceColumn = attribute.InnerText;
                            } 
 
                            attribute = itemElement.Attributes["DataSetColumn"];
                            if (attribute != null) { 
                                dataSetColumn = attribute.InnerText;
                            }

                            DataColumnMapping mapping = new DataColumnMapping(sourceColumn, dataSetColumn); 
                            Mappings.Add(mapping);
                        } 
                    } 
                }
            } 
        }

        void IDataSourceXmlSerializable.ReadXml(XmlElement xmlElement, DataSourceXmlSerializer serializer) {
            if (xmlElement.LocalName == SchemaName.RadTable || xmlElement.LocalName == SchemaName.OldRadTable) { 
                TableType = TableType.RadTable;
                serializer.DeserializeBody(xmlElement, this); 
            } 
        }
 
        // this is a helper function for RemoveChildren
        private DataColumn FindSharedColumn(ICollection dataColumns, ICollection designColumns) {
            foreach (DataColumn dataColumn in dataColumns) {
                foreach (object child in designColumns) { 
                    DesignColumn designColumn = child as DesignColumn;
                    if (designColumn != null && designColumn.DataColumn == dataColumn) { 
                        return dataColumn; 
                    }
                } 
            }
            return null;
        }
 

        // another helper function for RemoveChildren 
        private void RemoveColumnsFromSource( Source source, string[] colsToRemove ) { 
		//bugbug removed
        } 



        void IDataSourceCommandTarget.RemoveChildren(ICollection children) { 
            Debug.Assert(((IDataSourceCommandTarget)this).CanRemoveChildren(children), "invalid child");
            if (this.owner != null) { 
                ArrayList relatedRelations = owner.GetRelatedRelations(new DesignTable[1] { this }); 

                if (relatedRelations.Count > 0) { 
                    int relationAttachedCount = 0;
                    ArrayList removingRelations = new ArrayList();

                    foreach (DesignRelation relation in relatedRelations) { 
                        if (relation.ParentDesignTable == this) {
                            DataColumn matchedColumn = FindSharedColumn(relation.ParentDataColumns, children); 
 
                            if (matchedColumn != null) {
                                relationAttachedCount++; 
                                removingRelations.Add(relation);
                                continue;
                            }
                        } 

                        if (relation.ChildDesignTable == this) { 
                            DataColumn matchedColumn = FindSharedColumn(relation.ChildDataColumns, children); 

                            if (matchedColumn != null) { 
                                relationAttachedCount++;
                                removingRelations.Add(relation);
                            }
                        } 
                    }
 
                    if (relationAttachedCount > 0) { 
                        foreach (DesignRelation rel in removingRelations) {
                            if (rel.Owner != null) { 
                                rel.Owner.DesignRelations.Remove(rel);
                            }
                        }
                    } 
                }
            } 
 
            // Remove unique constraints first
            ArrayList relatedConstraints = GetRelatedDataConstraints(children, true); 

            foreach (UniqueConstraint constraint in relatedConstraints) {
                if (constraint.IsPrimaryKey) {
                    this.PrimaryKeyColumns = null; 
                }
                else { 
                    RemoveConstraint(constraint); 
                }
            } 

            relatedConstraints = GetRelatedDataConstraints(children, false);
            foreach (Constraint constraint in relatedConstraints) {
                RemoveConstraint(constraint); 
            }
 
            ArrayList colsToRemoveFromQuery = new ArrayList(); 

            foreach (object child in children) { 
                if (child is DesignColumn) {
                    DesignColumn column = (DesignColumn)child;
                    string[] mappedNames = DataDesignUtil.MapColumnNames(this.Mappings, new string[] { column.Name }, DataDesignUtil.MappingDirection.DataSetToSource);
 
                    colsToRemoveFromQuery.Add(mappedNames[0]);
 
                    DesignColumns.Remove((DesignColumn)child); 
                    RemoveColumnMapping(column.Name);
                } 
                else if (child is Source) {
                    Sources.Remove((Source)child);
                }
                else if (child is DataAccessor) { 
                    Debug.Assert(children.Count == 1, "We can only delete DataComponent by itself");
                    this.ConvertTableTypeTo(TableType.DataTable); 
                    Debug.Assert(this.DataAccessor == null, "Expected datacomponent was deleted now"); 
                }
            } 

            if (colsToRemoveFromQuery.Count > 0) {
                string[] colsToRemove = (string[])colsToRemoveFromQuery.ToArray(typeof(string));
 
                RemoveColumnsFromSource(MainSource, colsToRemove);
                foreach (Source s in Sources) { 
                    RemoveColumnsFromSource(s, colsToRemove); 
                }
            } 

        }

        /// <summary> 
        ///  This function fixes the problem that we can't remove the primaryKey directly
        /// </summary> 
        internal void RemoveConstraint(Constraint constraint) { 
            Debug.Assert(dataTable != null, "dataTable should be null any time");
 
            IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (componentChangeService != null) {
                componentChangeService.OnComponentChanging(this, null);
            } 
            // componentChangeService.OnComponentChanged will be called in OnConstraintChanged
 
            try { 
                inAccessConstraints = true;
 
                if (dataTable.Constraints.CanRemove(constraint)) {
                    dataTable.Constraints.Remove(constraint);
                }
                else if (dataTable.Constraints.Count == 1) { 
                    if (dataTable.Constraints[0] == constraint) {
                        dataTable.Constraints.Clear(); 
                    } 
                }
                else { 
                    Constraint[] constraints = new Constraint[dataTable.Constraints.Count-1];
                    ArrayList relations = new ArrayList();
                    int i = 0;
                    foreach (Constraint oldConstraint in dataTable.Constraints) { 
                        if (oldConstraint != constraint) {
                            constraints[i++] = oldConstraint; 
                        } 
                    }
 
                    // we should tempoary remove the relations connecting to the constraint here, and add them again later
                    // If we don't do that, after we remove all foreignConstraints on the table, and add back, the relationship between
                    //  the DataRelation and ForeignKeyConstraint will be broken.
                    //  This is a work-around since DataSet don't support us to remove a primary key on the table. 
                    if (Owner != null) {
                        foreach (DataRelation relation in Owner.DataSet.Relations) { 
                            if (relation.ChildTable == dataTable) { 
                                relations.Add(relation);
                            } 
                        }
                        foreach (DataRelation relation in relations) {
                            Owner.DataSet.Relations.Remove(relation);
                        } 
                    }
 
                    dataTable.Constraints.Clear(); 
                    dataTable.Constraints.AddRange(constraints);
                    if (Owner != null) { 
                        foreach (DataRelation relation in relations) {
                            Owner.DataSet.Relations.Add(relation);
                        }
                    } 
                }
 
            } 
            finally {
                inAccessConstraints = false; 
                OnConstraintChanged();
            }
        }
 

        internal void RemoveColumnMapping( string columnName ) { 
 		//bugbug removed 
        }
 

        internal void RemoveKey(UniqueConstraint constraint) {
            ArrayList relatedRelations = new ArrayList();
            foreach (DesignRelation relation in owner.DesignRelations) { 
                DataRelation dataRelation = relation.DataRelation;
                if (dataRelation != null && dataRelation.ParentKeyConstraint == constraint) { 
                    relatedRelations.Add(relation); 
                }
            } 
            foreach (DesignRelation relatedRelation in relatedRelations) {
                owner.DesignRelations.Remove(relatedRelation);
            }
 
            RemoveConstraint(constraint);
        } 
 

 

        /// <summary>
        /// Set type not through the TableType property
        /// </summary> 
        /// <param name="newType"></param>
        internal void SetTypeForUndo(TableType newType) { 
            this.tableType = newType; 
        }
 
        void IDataSourceXmlSpecialOwner.WriteSpecialItem(string propertyName, XmlWriter writer, DataSourceXmlSerializer serializer) {
            if (propertyName == "Mappings") {
                foreach (DataColumnMapping mapping in Mappings) {
                    writer.WriteStartElement(String.Empty, "Mapping", SchemaName.DataSourceNamespace); 
                    writer.WriteAttributeString("SourceColumn", mapping.SourceColumn);
                    writer.WriteAttributeString("DataSetColumn", mapping.DataSetColumn); 
                    writer.WriteEndElement(); 
                }
            } 
        }

        void IDataSourceXmlSerializable.WriteXml(XmlWriter xmlWriter, DataSourceXmlSerializer serializer) {
            switch (TableType) { 
                case TableType.DataTable:
                    break; 
                case TableType.RadTable: 
                    xmlWriter.WriteStartElement(String.Empty, SchemaName.RadTable, SchemaName.DataSourceNamespace);
                    serializer.SerializeBody(xmlWriter, this); 
                    xmlWriter.WriteFullEndElement();
                    break;
            }
        } 

        internal void UpdateColumnMappingDataSetColumnName(string oldName, string newName) { 
		//bugbug removed 
        }
 
        internal void UpdateColumnMappingSourceColumnName(string dataSetColumn, string newSourceColumn) {
 		//bugbug removed
        }
 
        internal string UserTableName {
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_USER_TABLENAME] as string; 
            }
            set { 
                this.dataTable.ExtendedProperties[EXTPROPNAME_USER_TABLENAME] = value;
            }
        }
 
        internal string GeneratorRunFillName {
            get { 
                return generatorRunFillName; 
            }
 
            set {
                generatorRunFillName = value;
            }
        } 

        internal string GeneratorTablePropName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLEPROPNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLEPROPNAME] = value;
            }
        } 

        internal string GeneratorTableVarName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLEVARNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLEVARNAME] = value;
            }
        } 

        internal string GeneratorTableClassName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLECLASSNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_TABLECLASSNAME] = value;
            }
        } 

        internal string GeneratorRowClassName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCLASSNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCLASSNAME] = value;
            }
        } 

        internal string GeneratorRowEvHandlerName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWEVHANDLERNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWEVHANDLERNAME] = value;
            }
        } 

        internal string GeneratorRowEvArgName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWEVARGNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWEVARGNAME] = value;
            }
        } 

        internal string GeneratorRowChangingName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCHANGINGNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCHANGINGNAME] = value;
            }
        } 

        internal string GeneratorRowChangedName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCHANGEDNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWCHANGEDNAME] = value;
            }
        } 

        internal string GeneratorRowDeletingName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWDELETINGNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWDELETINGNAME] = value;
            }
        } 

        internal string GeneratorRowDeletedName { 
            get { 
                return this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWDELETEDNAME] as string;
            } 
            set {
                this.dataTable.ExtendedProperties[EXTPROPNAME_GENERATOR_ROWDELETEDNAME] = value;
            }
        } 

        internal override StringCollection NamingPropertyNames { 
            get { 
                return namingPropNames;
            } 
        }

        [DataSourceXmlAttribute(), Browsable(false), DefaultValue(null)]
        public string GeneratorDataComponentClassName { 
            get {
                return generatorDataComponentClassName; 
            } 
            set {
                generatorDataComponentClassName = value; 
            }
        }

//        [DataSourceXmlAttribute(), Browsable(false), DefaultValue(null)] 
//        public string GeneratorDataComponentInterfaceName {
//            get { 
//                return generatorDataComponentInterfaceName; 
//            }
//            set { 
//                generatorDataComponentInterfaceName = value;
//            }
//        }
 
        [DataSourceXmlAttribute(), Browsable(false), DefaultValue(null)]
        public string UserDataComponentName { 
            get { 
                return userDataComponentName;
            } 
            set {
                userDataComponentName = value;
            }
        } 

        // IDataSourceRenamableObject implementation 
        [Browsable(false)] 
        public override string GeneratorName {
            get { 
                return GeneratorTablePropName;
            }
        }
 
        /// <summary>
        /// This class is used during the code gen. The caller 
        /// needs to set the value before use it. 
        /// </summary>
        internal CodeGenPropertyCache PropertyCache { 
            get{
                Debug.Assert(this.codeGenPropertyCache != null, "You should assign PropertyCache before use it");
                return this.codeGenPropertyCache;
            } 
            set{
                this.codeGenPropertyCache = value; 
            } 
        }
 
        /// <summary>
        /// </summary>
        internal class CodeGenPropertyCache {
            private DesignTable designTable; 
            private Type connectionType;
            private Type transactionType; 
            private Type adapterType; 
            private string tamAdapterPropName;
            private string tamAdapterVarName; 

            internal Type AdapterType {
                get {
                    if (adapterType == null){ 
                        if (this.designTable == null || this.designTable.Connection == null || designTable.Connection.Provider == null) {
                            return null; 
                        } 
                        System.Data.Common.DbProviderFactory providerFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
                        if (providerFactory != null) { 
                            DataAdapter adapter = providerFactory.CreateDataAdapter();
                            if (adapter != null){
                                adapterType = adapter.GetType();
                            } 
                        }
                    } 
                    return adapterType; 
                }
            } 
            internal Type ConnectionType {
                get {
                    if (connectionType == null) {
                        if (this.designTable != null && this.designTable.Connection != null) { 
                            IDbConnection connection = this.designTable.Connection.CreateEmptyDbConnection();
                            if (connection != null) { 
                                connectionType = connection.GetType(); 
                            }
                        } 
                    }
                    return connectionType;
                }
            } 
            internal Type TransactionType {
                get { 
                    if (transactionType == null){ 
                        if (this.designTable == null || this.designTable.Connection == null || designTable.Connection.Provider == null) {
                            return null; 
                        }
                        System.Data.Common.DbProviderFactory providerFactory = ProviderManager.GetFactory(designTable.Connection.Provider);
                        if (providerFactory != null) {
                            Type commandType = providerFactory.CreateCommand().GetType(); 
                            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(commandType)) {
                                if (StringUtil.EqualValue(pd.Name, "Transaction")) { 
                                    transactionType = pd.PropertyType; 
                                    break;
                                } 
                            }
                        }
                        if (transactionType == null) {
                            transactionType = typeof(System.Data.IDbTransaction); 
                        }
                    } 
                    return transactionType; 
                }
            } 

            /// <summary>
            /// Used by TableAdapterManager to generate TableAdapter property name
            /// </summary> 
            internal string TAMAdapterPropName {
                get { 
                    return tamAdapterPropName; 
                }
                set { 
                    tamAdapterPropName = value;
                }
            }
 
            /// <summary>
            /// Used by TableAdapterManager to generate TableAdapter variable name 
            /// </summary> 
            internal string TAMAdapterVarName {
                get { 
                    return tamAdapterVarName;
                }
                set {
                    tamAdapterVarName = value; 
                }
            } 
 

            internal CodeGenPropertyCache(DesignTable designTable) { 
                this.designTable = designTable;
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
