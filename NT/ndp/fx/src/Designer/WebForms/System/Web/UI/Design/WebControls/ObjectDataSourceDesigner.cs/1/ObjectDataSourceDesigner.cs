//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ObjectDataSourceDesigner : DataSourceDesigner { 
        internal const BindingFlags MethodFilter = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
 
        private const string DesignerStateDataSourceSchemaKey = "DataSourceSchema"; 
        private const string DesignerStateDataSourceSchemaTypeNameKey = "DataSourceSchemaTypeName";
        private const string DesignerStateDataSourceSchemaSelectMethodKey = "DataSourceSchemaSelectMethod"; 
        private const string DesignerStateDataSourceSchemaSelectMethodReturnTypeNameKey = "DataSourceSchemaSelectMethodReturnTypeName";

        private const string DesignerStateShowOnlyDataComponentsStateKey = "ShowOnlyDataComponentsState";
 
        private bool _inWizard;
        private Type _selectMethodReturnType; 
 
        // Indicates that when retrieving schema, the schema should be returned even
        // if it is no longer consistent with the current state of the data source. 
        private bool _forceSchemaRetrieval;

        internal Type SelectMethodReturnType {
            get { 
                if (_selectMethodReturnType == null) {
                    string selectMethodReturnTypeName = DesignerState[DesignerStateDataSourceSchemaSelectMethodReturnTypeNameKey] as string; 
                    if (!String.IsNullOrEmpty(selectMethodReturnTypeName)) { 
                        _selectMethodReturnType = GetType(Component.Site, selectMethodReturnTypeName, true);
                    } 
                }
                return _selectMethodReturnType;
            }
        } 

 
        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.CanConfigure"]/*' /> 
        public override bool CanConfigure {
            get { 
                return TypeServiceAvailable;
            }
        }
 
        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.CanRefreshSchema"]/*' />
        public override bool CanRefreshSchema { 
            get { 
                return (!String.IsNullOrEmpty(TypeName) &&
                        !String.IsNullOrEmpty(SelectMethod) && 
                        TypeServiceAvailable);
            }
        }
 
        /// <devdoc>
        /// Stores the state of the "Show only data components" checkbox in 
        /// the wizard's panel. 
        /// </devdoc>
        internal object ShowOnlyDataComponentsState { 
            get {
                return DesignerState[DesignerStateShowOnlyDataComponentsStateKey];
            }
            set { 
                DesignerState[DesignerStateShowOnlyDataComponentsStateKey] = value;
            } 
        } 

        private bool TypeServiceAvailable { 
            get {
                // We need at least one of these two services to be able to launch any UI
                IServiceProvider serviceProvider = Component.Site;
                if (serviceProvider == null) { 
                    return false;
                } 
                ITypeResolutionService typeResolver = (ITypeResolutionService)serviceProvider.GetService(typeof(ITypeResolutionService)); 
                ITypeDiscoveryService typeDiscoverer = (ITypeDiscoveryService)serviceProvider.GetService(typeof(ITypeDiscoveryService));
                return (typeResolver != null) || (typeDiscoverer != null); 
            }
        }

        /// <devdoc> 
        /// The ObjectDataSource associated with this designer.
        /// </devdoc> 
        internal ObjectDataSource ObjectDataSource { 
            get {
                return (ObjectDataSource)Component; 
            }
        }

        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.SelectMethod"]/*' /> 
        /// <devdoc>
        /// Implements the designer's version of the SelectMethod property. 
        /// This is used to shadow the SelectMethod property of the 
        /// runtime control.
        /// </devdoc> 
        public string SelectMethod {
            get {
                return ObjectDataSource.SelectMethod;
            } 
            set {
                if (value != SelectMethod) { 
                    ObjectDataSource.SelectMethod = value; 
                    UpdateDesignTimeHtml();
                    // Only call RefreshSchema if the Configure Data Source wizard is not active 
                    if (CanRefreshSchema && !_inWizard) {
                        RefreshSchema(true);
                    }
                    else { 
                        OnDataSourceChanged(EventArgs.Empty);
                    } 
                } 
            }
        } 

        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.TypeName"]/*' />
        /// <devdoc>
        /// Implements the designer's version of the TypeName property. 
        /// This is used to shadow the TypeName property of the
        /// runtime control. 
        /// </devdoc> 
        public string TypeName {
            get { 
                return ObjectDataSource.TypeName;
            }
            set {
                if (value != TypeName) { 
                    ObjectDataSource.TypeName = value;
                    UpdateDesignTimeHtml(); 
                    if (CanRefreshSchema) { 
                        RefreshSchema(true);
                    } 
                    else {
                        OnDataSourceChanged(EventArgs.Empty);
                    }
                } 
            }
        } 
 

        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.Configure"]/*' /> 
        /// <devdoc>
        /// Handles the Configure DataSource designer verb event.
        /// </devdoc>
        public override void Configure() { 
            _inWizard = true;
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConfigureDataSourceChangeCallback), null, SR.GetString(SR.DataSource_ConfigureTransactionDescription)); 
            _inWizard = false; 
        }
 
        /// <devdoc>
        /// Transacted change callback to invoke the Configure DataSource wizard.
        /// </devdoc>
        private bool ConfigureDataSourceChangeCallback(object context) { 
            try {
                SuppressDataSourceEvents(); 
 
                IServiceProvider site = Component.Site;
 
                ObjectDataSourceWizardForm form = new ObjectDataSourceWizardForm(site, this);
                DialogResult result = UIServiceHelper.ShowDialog(site, form);
                if (result == DialogResult.OK) {
                    OnDataSourceChanged(EventArgs.Empty); 
                    return true;
                } 
                else { 
                    return false;
                } 
            }
            finally {
                ResumeDataSourceEvents();
            } 
        }
 
        /// <devdoc> 
        /// Converts a given TypeSchema object to a representative array of
        /// DataTable objects with the exact same schema. This method 
        /// specifically works for TypeSchema objects only since it does not
        /// worry about child relationships and other more advanced schema
        /// features.
        /// A DataSet cannot be used for this purpose since its Tables 
        /// collection can alter names of tables and we don't want that.
        /// </devdoc> 
        private static DataTable[] ConvertSchemaToDataTables(TypeSchema schema) { 
            if (schema == null) {
                return null; 
            }

            IDataSourceViewSchema[] views = schema.GetViews();
            if (views == null) { 
                return null;
            } 
            DataTable[] tables = new DataTable[views.Length]; 
            for (int i = 0; i < views.Length; i++) {
                IDataSourceViewSchema view = views[i]; 
                tables[i] = new DataTable(view.Name);

                IDataSourceFieldSchema[] fields = view.GetFields();
                if (fields == null) { 
                    continue;
                } 
                System.Collections.Generic.List<DataColumn> primaryKey = new System.Collections.Generic.List<DataColumn>(); 
                for (int j = 0; j < fields.Length; j++) {
                    IDataSourceFieldSchema field = fields[j]; 
                    DataColumn column = new DataColumn();
                    column.AllowDBNull = field.Nullable;
                    column.AutoIncrement = field.Identity;
                    column.ColumnName = field.Name; 
                    column.DataType = field.DataType;
                    if (column.DataType == typeof(string)) { 
                        // Length only applies to strings 
                        column.MaxLength = field.Length;
                    } 
                    column.ReadOnly = field.IsReadOnly;
                    column.Unique = field.IsUnique;
                    tables[i].Columns.Add(column);
                    if (field.PrimaryKey) { 
                        primaryKey.Add(column);
                    } 
                } 
                if (primaryKey.Count > 0) {
                    tables[i].PrimaryKey = primaryKey.ToArray(); 
                }
            }
            return tables;
        } 

        /// <devdoc> 
        /// Merges a method's parameter with the best available match in a 
        /// list of existing parameters.
        /// </devdoc> 
        private static Parameter CreateMergedParameter(ParameterInfo methodParameter, Parameter[] parameters) {
            foreach (Parameter parameter in parameters) {
                if (ParametersMatch(methodParameter, parameter)) {
                    return parameter; 
                }
            } 
            // Could not find any matching parameter, create a new one 
            Parameter newParameter = new Parameter(methodParameter.Name);
 
            // Create Direction
            if (methodParameter.IsOut) {
                newParameter.Direction = ParameterDirection.Output;
            } 
            else {
                if (methodParameter.ParameterType.IsByRef) { 
                    newParameter.Direction = ParameterDirection.InputOutput; 
                }
                else { 
                    // Default direction: Input
                    newParameter.Direction = ParameterDirection.Input;
                }
            } 

            // Create TypeCode 
            newParameter.Type = GetTypeCodeForType(methodParameter.ParameterType); 

            return newParameter; 
        }

        internal static Type GetType(IServiceProvider serviceProvider, string typeName, bool silent) {
            ITypeResolutionService typeResolver = null; 
            if (serviceProvider != null) {
                typeResolver = (ITypeResolutionService)serviceProvider.GetService(typeof(ITypeResolutionService)); 
            } 
            Debug.Assert(typeResolver != null, "ITypeResolutionService was not found.");
            if (typeResolver == null) { 
                return null;
            }

            try { 
                return typeResolver.GetType(typeName, true, true);
            } 
            catch (Exception ex) { 
                if (!silent) {
                    UIServiceHelper.ShowError(serviceProvider, ex, SR.GetString(SR.ObjectDataSourceDesigner_CannotGetType, typeName)); 
                }
                return null;
            }
        } 

        /// <devdoc> 
        /// Attempts to detect the TypeCode representing a given type. If the 
        /// TypeCode cannot be determined, TypeCode.Object is returned.
        /// </devdoc> 
        private static TypeCode GetTypeCodeForType(Type type) {
            // If the type is Nullable<T> then we just want the T
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>))) {
                type = type.GetGenericArguments()[0]; 
            }
            else { 
                if (type.IsByRef) { 
                    type = type.GetElementType();
                } 
            }

            if (typeof(Boolean).IsAssignableFrom(type)) {
                return TypeCode.Boolean; 
            }
            if (typeof(Byte).IsAssignableFrom(type)) { 
                return TypeCode.Byte; 
            }
            if (typeof(Char).IsAssignableFrom(type)) { 
                return TypeCode.Char;
            }
            if (typeof(DateTime).IsAssignableFrom(type)) {
                return TypeCode.DateTime; 
            }
            if (typeof(DBNull).IsAssignableFrom(type)) { 
                return TypeCode.DBNull; 
            }
            if (typeof(Decimal).IsAssignableFrom(type)) { 
                return TypeCode.Decimal;
            }
            if (typeof(Double).IsAssignableFrom(type)) {
                return TypeCode.Double; 
            }
            if (typeof(Int16).IsAssignableFrom(type)) { 
                return TypeCode.Int16; 
            }
            if (typeof(Int32).IsAssignableFrom(type)) { 
                return TypeCode.Int32;
            }
            if (typeof(Int64).IsAssignableFrom(type)) {
                return TypeCode.Int64; 
            }
            if (typeof(SByte).IsAssignableFrom(type)) { 
                return TypeCode.SByte; 
            }
            if (typeof(Single).IsAssignableFrom(type)) { 
                return TypeCode.Single;
            }
            if (typeof(String).IsAssignableFrom(type)) {
                return TypeCode.String; 
            }
            if (typeof(UInt16).IsAssignableFrom(type)) { 
                return TypeCode.UInt16; 
            }
            if (typeof(UInt32).IsAssignableFrom(type)) { 
                return TypeCode.UInt32;
            }
            if (typeof(UInt64).IsAssignableFrom(type)) {
                return TypeCode.UInt64; 
            }
 
            // Unknown types 
            return TypeCode.Object;
        } 

        public override DesignerDataSourceView GetView(string viewName) {
            //
 

 
            string[] viewNames = GetViewNames(); 
            Debug.Assert(viewNames != null, "Did not expect null ViewNames array");
            if ((viewNames != null) && (viewNames.Length > 0)) { 
                if (String.IsNullOrEmpty(viewName)) {
                    viewName = viewNames[0];
                }
                foreach (string vn in viewNames) { 
                    if (String.Equals(viewName, vn, StringComparison.OrdinalIgnoreCase)) {
                        return new ObjectDesignerDataSourceView(this, viewName); 
                    } 
                }
            } 
            else {
                return new ObjectDesignerDataSourceView(this, String.Empty);
            }
 
            return null;
        } 
 
        public override string[] GetViewNames() {
            System.Collections.Generic.List<string> viewNames = new System.Collections.Generic.List<string>(); 
            DataTable[] schemaTables = LoadSchema();
            if ((schemaTables != null) && (schemaTables.Length > 0)) {
                foreach (DataTable dataTable in schemaTables) {
                    viewNames.Add(dataTable.TableName); 
                }
            } 
            return viewNames.ToArray(); 
        }
 
        /// <devdoc>
        /// Returns true if the given MethodInfo matches the specified method
        /// name and parameter names.
        /// </devdoc> 
        internal static bool IsMatchingMethod(MethodInfo method, string methodName, ParameterCollection parameters, Type dataObjectType) {
            // Test if name is the same 
            if (!String.Equals(methodName, method.Name, StringComparison.OrdinalIgnoreCase)) { 
                return false;
            } 

            ParameterInfo[] methodParameters = method.GetParameters();

            if (dataObjectType != null) { 
                // If DataObjectTypeName is set, we first try to match methods that take one or two parameters of that type
                if ((methodParameters.Length == 1 && methodParameters[0].ParameterType == dataObjectType) || 
                    (methodParameters.Length == 2 && methodParameters[0].ParameterType == dataObjectType && methodParameters[1].ParameterType == dataObjectType)) { 
                    return true;
                } 
            }

            // If we couldn't match based on type, then we try to match based on name
 
            // First check if the parameter counts match
            if (methodParameters.Length != parameters.Count) { 
                return false; 
            }
 
            // Build up a case-insensitive list of parameters
            Hashtable caseInsensitiveInputParameters = new Hashtable(StringComparer.Create(CultureInfo.InvariantCulture, true));
            foreach (Parameter p in parameters) {
                if (!caseInsensitiveInputParameters.Contains(p.Name)) { 
                    caseInsensitiveInputParameters.Add(p.Name, null);
                } 
            } 

            // Check if all the parameter names match 
            foreach (ParameterInfo pi in methodParameters) {
                if (!caseInsensitiveInputParameters.Contains(pi.Name)) {
                    return false;
                } 
            }
 
            return true; 
        }
 
        /// <devdoc>
        /// Attempts to load the schema for this ObjectDataSource. If the
        /// schema is not consistent with the current properties, then it is
        /// removed from state. 
        /// </devdoc>
        internal DataTable[] LoadSchema() { 
            if (!_forceSchemaRetrieval) { 
                // Only check for consistency if we are not forcing the retrieval
                string typeName = DesignerState[DesignerStateDataSourceSchemaTypeNameKey] as string; 
                string methodName = DesignerState[DesignerStateDataSourceSchemaSelectMethodKey] as string;

                if ((!String.Equals(typeName, TypeName, StringComparison.OrdinalIgnoreCase)) ||
                    (!String.Equals(methodName, SelectMethod, StringComparison.OrdinalIgnoreCase))) { 
                    return null;
                } 
            } 

            // Either we are forcing schema retrieval, or we're not forcing but we're consistent, so get the schema 
            DataTable[] schemaTables = null;
            Pair tableData = DesignerState[DesignerStateDataSourceSchemaKey] as Pair;
            if (tableData != null) {
                // Reconstruct the array of DataTables from the falsly-named 
                // tables and the list of the true names. See the SaveSchema()
                // method for more information. 
                string[] tableNames = tableData.First as string[]; 
                DataTable[] tables = tableData.Second as DataTable[];
                Debug.Assert((tableNames != null) && (tables != null), "Did not expect null arrays in table data schema"); 
                if ((tableNames != null) && (tables != null)) {
                    int tableCount = tableNames.Length;
                    schemaTables = new DataTable[tableCount];
                    for (int i = 0; i < tableCount; i++) { 
                        // Clone the saved table and set its true name
                        schemaTables[i] = tables[i].Clone(); 
                        schemaTables[i].TableName = tableNames[i]; 
                    }
                } 
            }
            return schemaTables;
        }
 
        /// <devdoc>
        /// Merges reflected parameters with an existing set of parameters. 
        /// </devdoc> 
        internal static Parameter[] MergeParameters(Parameter[] parameters, MethodInfo methodInfo) {
            ParameterInfo[] methodParameters = methodInfo.GetParameters(); 
            Parameter[] mergedParameters = new Parameter[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++) {
                ParameterInfo methodParameter = methodParameters[i];
                mergedParameters[i] = CreateMergedParameter(methodParameter, parameters); 
            }
            return mergedParameters; 
        } 

        /// <devdoc> 
        /// Merges reflected parameters with an existing set of parameters.
        /// </devdoc>
        internal static void MergeParameters(ParameterCollection parameters, MethodInfo methodInfo, Type dataObjectType) {
            Parameter[] oldParameters = new Parameter[parameters.Count]; 
            parameters.CopyTo(oldParameters, 0);
            parameters.Clear(); 
 
            if (methodInfo == null) {
                // No method is selected, do nothing 
                return;
            }
            if (dataObjectType == null) {
                // No DataObject, just reflect on the method's parameters 
                ParameterInfo[] methodParameters = methodInfo.GetParameters();
                foreach (ParameterInfo methodParameter in methodParameters) { 
                    Parameter newParam = CreateMergedParameter(methodParameter, oldParameters); 
                    if (parameters[newParam.Name] == null) {
                        parameters.Add(newParam); 
                    }
                }
            }
            else { 
                // DataObject is present, we don't need to create any parameters
                // since they will be created automatically at runtime. 
            } 
        }
 
        /// <devdoc>
        /// Returns whether two parameters match based on name, direction, and
        /// type.
        /// </devdoc> 
        private static bool ParametersMatch(ParameterInfo methodParameter, Parameter parameter) {
            // Check if names match 
            if (!String.Equals(methodParameter.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)) { 
                return false;
            } 

            // Check if directions match
            switch (parameter.Direction) {
                case ParameterDirection.ReturnValue: 
                    // We never have a return value ParameterInfo, so it never matches
                    return false; 
 
                case ParameterDirection.Input:
                    if (methodParameter.IsOut || methodParameter.ParameterType.IsByRef) { 
                        // Method parameter is out or ref, so it doesn't match
                        return false;
                    }
                    break; 

                case ParameterDirection.InputOutput: 
                    if (!methodParameter.ParameterType.IsByRef) { 
                        // Method parameter is not ref, so it doesn't match
                        return false; 
                    }
                    break;

                case ParameterDirection.Output: 
                    if (!methodParameter.IsOut) {
                        // Method parameter is not out, so it doesn't match 
                        return false; 
                    }
                    break; 
            }

            // Check if types match
            TypeCode methodParameterType = GetTypeCodeForType(methodParameter.ParameterType); 
            if (((methodParameterType == TypeCode.Object) || (methodParameterType == TypeCode.Empty)) &&
                ((parameter.Type == TypeCode.Object) || (parameter.Type == TypeCode.Empty))) { 
                // Effectively, the method's parameter type could not be 
                // detected, and the web parameter has no type either, so we
                // assume it's OK 
                return true;
            }
            // Both method parameter and web parameter have types, so compare them
            return (methodParameterType == parameter.Type); 
        }
 
        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        /// Overridden by the designer to shadow various runtime properties 
        /// with corresponding properties that it implements.
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 

            PropertyDescriptor property; 
 
            // Shadow runtime TypeName property
            property = (PropertyDescriptor)properties["TypeName"]; 
            Debug.Assert(property != null);
            properties["TypeName"] = TypeDescriptor.CreateProperty(GetType(), property);

            // Shadow runtime SelectMethod property 
            property = (PropertyDescriptor)properties["SelectMethod"];
            Debug.Assert(property != null); 
            properties["SelectMethod"] = TypeDescriptor.CreateProperty(GetType(), property); 
        }
 
        public override void RefreshSchema(bool preferSilent) {
            try {
                SuppressDataSourceEvents();
 
                Debug.Assert(CanRefreshSchema, "CanRefreshSchema is false - RefreshSchema should not be called");
 
                Cursor originalCursor = Cursor.Current; 
                try {
                    Cursor.Current = Cursors.WaitCursor; 

                    // Try to load the type and find the appropriate method to get its schema
                    Type type = GetType(Component.Site, TypeName, preferSilent);
 
                    if (type == null) {
                        Debug.Fail("Did not expect a null type when calling ITypeResolutionService.GetType(typename, true, true)"); 
                        return; 
                    }
 
                    // Iterate through the methods of the type and try to find the best match
                    MethodInfo[] methods = type.GetMethods(MethodFilter);
                    MethodInfo methodInfo = null;
                    MethodInfo optimisticMethodMatch = null; 
                    bool optimismFailed = false;
                    Type dataObjectType = null; 
                    if (!String.IsNullOrEmpty(ObjectDataSource.DataObjectTypeName)) { 
                        dataObjectType = GetType(Component.Site, ObjectDataSource.DataObjectTypeName, preferSilent);
                    } 
                    foreach (MethodInfo mi in methods) {
                        if (String.Equals(mi.Name, SelectMethod, StringComparison.OrdinalIgnoreCase)) {
                            // We first optimistically match based on just the method name, however
                            // the optimism will fail if we find two methods with the same name but 
                            // with different return types
                            if ((optimisticMethodMatch != null) && (optimisticMethodMatch.ReturnType != mi.ReturnType)) { 
                                optimismFailed = true; 
                            }
                            else { 
                                optimisticMethodMatch = mi;
                            }

                            // If we find a perfect match, we can stop looking immediately 
                            if (IsMatchingMethod(mi, SelectMethod, ObjectDataSource.SelectParameters, dataObjectType)) {
                                methodInfo = mi; 
                                break; 
                            }
                        } 
                    }

                    // If we didn't find a true match, but instead we found an optimistic
                    // match, we can go ahead and use that 
                    if ((methodInfo == null) && (optimisticMethodMatch != null) && (!optimismFailed)) {
                        methodInfo = optimisticMethodMatch; 
                    } 

                    if (methodInfo != null) { 
                        RefreshSchema(methodInfo.ReflectedType, methodInfo.Name, methodInfo.ReturnType, preferSilent);
                    }
                }
                finally { 
                    Cursor.Current = originalCursor;
                } 
            } 
            finally {
                ResumeDataSourceEvents(); 
            }
        }

        /// <devdoc> 
        /// Refreshes the schema for this ObjectDataSourceDesigner. The new
        /// schema is automatically stored in DesignerState for persistence 
        /// across sessions. 
        /// </devdoc>
        internal void RefreshSchema(Type objectType, string methodName, Type schemaType, bool preferSilent) { 
            if ((objectType != null) && (!String.IsNullOrEmpty(methodName)) && (schemaType != null)) {
                // Get schema object for the requested type
                try {
                    TypeSchema schema = new TypeSchema(schemaType); 

                    // Store the schema into DesignerState. We can't store the actual 
                    // schema in designer state because it is not serializable, so we 
                    // wrap it in an array of DataTables with matching schema.
                    // We also store the current typename and method to make sure the 
                    // schema is valid.
                    _forceSchemaRetrieval = true;
                    DataTable[] oldSchemaTables = LoadSchema();
                    _forceSchemaRetrieval = false; 
                    IDataSourceSchema oldSchema = (oldSchemaTables == null ? null : new DataTableArraySchema(oldSchemaTables));
                    SaveSchema(objectType, methodName, ConvertSchemaToDataTables(schema), schemaType); 
                    DataTable[] newSchemaTables = LoadSchema(); 
                    IDataSourceSchema newSchema = (newSchemaTables == null ? null : new DataTableArraySchema(newSchemaTables));
 
                    if (!SchemasEquivalent(oldSchema, newSchema)) {
                        OnSchemaRefreshed(EventArgs.Empty);
                    }
                } 
                catch (Exception ex) {
                    if (!preferSilent) { 
                        UIServiceHelper.ShowError(Component.Site, ex, SR.GetString(SR.ObjectDataSourceDesigner_CannotGetSchema, schemaType.FullName)); 
                    }
                } 
            }
        }

        /// <devdoc> 
        /// Saves schema using the DesignerState. Along with the schema are
        /// stored the type and method used to generate the schema so that we 
        /// can make sure the schema is consistent. 
        /// </devdoc>
        private void SaveSchema(Type objectType, string methodName, DataTable[] schemaTables, Type schemaType) { 
            // DataTables without names cannot be serialized, so we store the
            // names in an array separately from the tables, and give all the
            // tables dummy names.
            Pair tableData = null; 
            if (schemaTables != null) {
                int tableCount = schemaTables.Length; 
                string[] tableNames = new string[tableCount]; 
                for (int i = 0; i < tableCount; i++) {
                    tableNames[i] = schemaTables[i].TableName; 
                    schemaTables[i].TableName = "Table" + i.ToString(CultureInfo.InvariantCulture);
                }
                tableData = new Pair(tableNames, schemaTables);
            } 
            DesignerState[DesignerStateDataSourceSchemaKey] = tableData;
            DesignerState[DesignerStateDataSourceSchemaTypeNameKey] = (objectType == null ? String.Empty : objectType.FullName); 
            DesignerState[DesignerStateDataSourceSchemaSelectMethodKey] = methodName; 

            // If the schema type changed, store the new type name and invalidate the cached return type 
            string oldReturnTypeName = DesignerState[DesignerStateDataSourceSchemaSelectMethodReturnTypeNameKey] as string;
            if (!String.Equals(oldReturnTypeName, schemaType.FullName, StringComparison.OrdinalIgnoreCase)) {
                DesignerState[DesignerStateDataSourceSchemaSelectMethodReturnTypeNameKey] = schemaType.FullName;
                _selectMethodReturnType = schemaType; 
            }
        } 
 
        /// <devdoc>
        /// Represents schema indicated by an array of DataTable objects. This 
        /// is similar to the DataSetSchema class, except that this one
        /// directly takes the array of DataTables instead of an entire
        /// DataSet.
        /// </devdoc> 
        private sealed class DataTableArraySchema : IDataSourceSchema {
            private DataTable[] _tables; 
 
            public DataTableArraySchema(DataTable[] tables) {
                _tables = tables; 
            }

            public IDataSourceViewSchema[] GetViews() {
                DataSetViewSchema[] viewSchemas = new DataSetViewSchema[_tables.Length]; 
                for (int i = 0; i < _tables.Length; i++) {
                    viewSchemas[i] = new DataSetViewSchema(_tables[i]); 
                } 
                return viewSchemas;
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ObjectDataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ObjectDataSourceDesigner : DataSourceDesigner { 
        internal const BindingFlags MethodFilter = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
 
        private const string DesignerStateDataSourceSchemaKey = "DataSourceSchema"; 
        private const string DesignerStateDataSourceSchemaTypeNameKey = "DataSourceSchemaTypeName";
        private const string DesignerStateDataSourceSchemaSelectMethodKey = "DataSourceSchemaSelectMethod"; 
        private const string DesignerStateDataSourceSchemaSelectMethodReturnTypeNameKey = "DataSourceSchemaSelectMethodReturnTypeName";

        private const string DesignerStateShowOnlyDataComponentsStateKey = "ShowOnlyDataComponentsState";
 
        private bool _inWizard;
        private Type _selectMethodReturnType; 
 
        // Indicates that when retrieving schema, the schema should be returned even
        // if it is no longer consistent with the current state of the data source. 
        private bool _forceSchemaRetrieval;

        internal Type SelectMethodReturnType {
            get { 
                if (_selectMethodReturnType == null) {
                    string selectMethodReturnTypeName = DesignerState[DesignerStateDataSourceSchemaSelectMethodReturnTypeNameKey] as string; 
                    if (!String.IsNullOrEmpty(selectMethodReturnTypeName)) { 
                        _selectMethodReturnType = GetType(Component.Site, selectMethodReturnTypeName, true);
                    } 
                }
                return _selectMethodReturnType;
            }
        } 

 
        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.CanConfigure"]/*' /> 
        public override bool CanConfigure {
            get { 
                return TypeServiceAvailable;
            }
        }
 
        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.CanRefreshSchema"]/*' />
        public override bool CanRefreshSchema { 
            get { 
                return (!String.IsNullOrEmpty(TypeName) &&
                        !String.IsNullOrEmpty(SelectMethod) && 
                        TypeServiceAvailable);
            }
        }
 
        /// <devdoc>
        /// Stores the state of the "Show only data components" checkbox in 
        /// the wizard's panel. 
        /// </devdoc>
        internal object ShowOnlyDataComponentsState { 
            get {
                return DesignerState[DesignerStateShowOnlyDataComponentsStateKey];
            }
            set { 
                DesignerState[DesignerStateShowOnlyDataComponentsStateKey] = value;
            } 
        } 

        private bool TypeServiceAvailable { 
            get {
                // We need at least one of these two services to be able to launch any UI
                IServiceProvider serviceProvider = Component.Site;
                if (serviceProvider == null) { 
                    return false;
                } 
                ITypeResolutionService typeResolver = (ITypeResolutionService)serviceProvider.GetService(typeof(ITypeResolutionService)); 
                ITypeDiscoveryService typeDiscoverer = (ITypeDiscoveryService)serviceProvider.GetService(typeof(ITypeDiscoveryService));
                return (typeResolver != null) || (typeDiscoverer != null); 
            }
        }

        /// <devdoc> 
        /// The ObjectDataSource associated with this designer.
        /// </devdoc> 
        internal ObjectDataSource ObjectDataSource { 
            get {
                return (ObjectDataSource)Component; 
            }
        }

        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.SelectMethod"]/*' /> 
        /// <devdoc>
        /// Implements the designer's version of the SelectMethod property. 
        /// This is used to shadow the SelectMethod property of the 
        /// runtime control.
        /// </devdoc> 
        public string SelectMethod {
            get {
                return ObjectDataSource.SelectMethod;
            } 
            set {
                if (value != SelectMethod) { 
                    ObjectDataSource.SelectMethod = value; 
                    UpdateDesignTimeHtml();
                    // Only call RefreshSchema if the Configure Data Source wizard is not active 
                    if (CanRefreshSchema && !_inWizard) {
                        RefreshSchema(true);
                    }
                    else { 
                        OnDataSourceChanged(EventArgs.Empty);
                    } 
                } 
            }
        } 

        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.TypeName"]/*' />
        /// <devdoc>
        /// Implements the designer's version of the TypeName property. 
        /// This is used to shadow the TypeName property of the
        /// runtime control. 
        /// </devdoc> 
        public string TypeName {
            get { 
                return ObjectDataSource.TypeName;
            }
            set {
                if (value != TypeName) { 
                    ObjectDataSource.TypeName = value;
                    UpdateDesignTimeHtml(); 
                    if (CanRefreshSchema) { 
                        RefreshSchema(true);
                    } 
                    else {
                        OnDataSourceChanged(EventArgs.Empty);
                    }
                } 
            }
        } 
 

        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.Configure"]/*' /> 
        /// <devdoc>
        /// Handles the Configure DataSource designer verb event.
        /// </devdoc>
        public override void Configure() { 
            _inWizard = true;
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConfigureDataSourceChangeCallback), null, SR.GetString(SR.DataSource_ConfigureTransactionDescription)); 
            _inWizard = false; 
        }
 
        /// <devdoc>
        /// Transacted change callback to invoke the Configure DataSource wizard.
        /// </devdoc>
        private bool ConfigureDataSourceChangeCallback(object context) { 
            try {
                SuppressDataSourceEvents(); 
 
                IServiceProvider site = Component.Site;
 
                ObjectDataSourceWizardForm form = new ObjectDataSourceWizardForm(site, this);
                DialogResult result = UIServiceHelper.ShowDialog(site, form);
                if (result == DialogResult.OK) {
                    OnDataSourceChanged(EventArgs.Empty); 
                    return true;
                } 
                else { 
                    return false;
                } 
            }
            finally {
                ResumeDataSourceEvents();
            } 
        }
 
        /// <devdoc> 
        /// Converts a given TypeSchema object to a representative array of
        /// DataTable objects with the exact same schema. This method 
        /// specifically works for TypeSchema objects only since it does not
        /// worry about child relationships and other more advanced schema
        /// features.
        /// A DataSet cannot be used for this purpose since its Tables 
        /// collection can alter names of tables and we don't want that.
        /// </devdoc> 
        private static DataTable[] ConvertSchemaToDataTables(TypeSchema schema) { 
            if (schema == null) {
                return null; 
            }

            IDataSourceViewSchema[] views = schema.GetViews();
            if (views == null) { 
                return null;
            } 
            DataTable[] tables = new DataTable[views.Length]; 
            for (int i = 0; i < views.Length; i++) {
                IDataSourceViewSchema view = views[i]; 
                tables[i] = new DataTable(view.Name);

                IDataSourceFieldSchema[] fields = view.GetFields();
                if (fields == null) { 
                    continue;
                } 
                System.Collections.Generic.List<DataColumn> primaryKey = new System.Collections.Generic.List<DataColumn>(); 
                for (int j = 0; j < fields.Length; j++) {
                    IDataSourceFieldSchema field = fields[j]; 
                    DataColumn column = new DataColumn();
                    column.AllowDBNull = field.Nullable;
                    column.AutoIncrement = field.Identity;
                    column.ColumnName = field.Name; 
                    column.DataType = field.DataType;
                    if (column.DataType == typeof(string)) { 
                        // Length only applies to strings 
                        column.MaxLength = field.Length;
                    } 
                    column.ReadOnly = field.IsReadOnly;
                    column.Unique = field.IsUnique;
                    tables[i].Columns.Add(column);
                    if (field.PrimaryKey) { 
                        primaryKey.Add(column);
                    } 
                } 
                if (primaryKey.Count > 0) {
                    tables[i].PrimaryKey = primaryKey.ToArray(); 
                }
            }
            return tables;
        } 

        /// <devdoc> 
        /// Merges a method's parameter with the best available match in a 
        /// list of existing parameters.
        /// </devdoc> 
        private static Parameter CreateMergedParameter(ParameterInfo methodParameter, Parameter[] parameters) {
            foreach (Parameter parameter in parameters) {
                if (ParametersMatch(methodParameter, parameter)) {
                    return parameter; 
                }
            } 
            // Could not find any matching parameter, create a new one 
            Parameter newParameter = new Parameter(methodParameter.Name);
 
            // Create Direction
            if (methodParameter.IsOut) {
                newParameter.Direction = ParameterDirection.Output;
            } 
            else {
                if (methodParameter.ParameterType.IsByRef) { 
                    newParameter.Direction = ParameterDirection.InputOutput; 
                }
                else { 
                    // Default direction: Input
                    newParameter.Direction = ParameterDirection.Input;
                }
            } 

            // Create TypeCode 
            newParameter.Type = GetTypeCodeForType(methodParameter.ParameterType); 

            return newParameter; 
        }

        internal static Type GetType(IServiceProvider serviceProvider, string typeName, bool silent) {
            ITypeResolutionService typeResolver = null; 
            if (serviceProvider != null) {
                typeResolver = (ITypeResolutionService)serviceProvider.GetService(typeof(ITypeResolutionService)); 
            } 
            Debug.Assert(typeResolver != null, "ITypeResolutionService was not found.");
            if (typeResolver == null) { 
                return null;
            }

            try { 
                return typeResolver.GetType(typeName, true, true);
            } 
            catch (Exception ex) { 
                if (!silent) {
                    UIServiceHelper.ShowError(serviceProvider, ex, SR.GetString(SR.ObjectDataSourceDesigner_CannotGetType, typeName)); 
                }
                return null;
            }
        } 

        /// <devdoc> 
        /// Attempts to detect the TypeCode representing a given type. If the 
        /// TypeCode cannot be determined, TypeCode.Object is returned.
        /// </devdoc> 
        private static TypeCode GetTypeCodeForType(Type type) {
            // If the type is Nullable<T> then we just want the T
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>))) {
                type = type.GetGenericArguments()[0]; 
            }
            else { 
                if (type.IsByRef) { 
                    type = type.GetElementType();
                } 
            }

            if (typeof(Boolean).IsAssignableFrom(type)) {
                return TypeCode.Boolean; 
            }
            if (typeof(Byte).IsAssignableFrom(type)) { 
                return TypeCode.Byte; 
            }
            if (typeof(Char).IsAssignableFrom(type)) { 
                return TypeCode.Char;
            }
            if (typeof(DateTime).IsAssignableFrom(type)) {
                return TypeCode.DateTime; 
            }
            if (typeof(DBNull).IsAssignableFrom(type)) { 
                return TypeCode.DBNull; 
            }
            if (typeof(Decimal).IsAssignableFrom(type)) { 
                return TypeCode.Decimal;
            }
            if (typeof(Double).IsAssignableFrom(type)) {
                return TypeCode.Double; 
            }
            if (typeof(Int16).IsAssignableFrom(type)) { 
                return TypeCode.Int16; 
            }
            if (typeof(Int32).IsAssignableFrom(type)) { 
                return TypeCode.Int32;
            }
            if (typeof(Int64).IsAssignableFrom(type)) {
                return TypeCode.Int64; 
            }
            if (typeof(SByte).IsAssignableFrom(type)) { 
                return TypeCode.SByte; 
            }
            if (typeof(Single).IsAssignableFrom(type)) { 
                return TypeCode.Single;
            }
            if (typeof(String).IsAssignableFrom(type)) {
                return TypeCode.String; 
            }
            if (typeof(UInt16).IsAssignableFrom(type)) { 
                return TypeCode.UInt16; 
            }
            if (typeof(UInt32).IsAssignableFrom(type)) { 
                return TypeCode.UInt32;
            }
            if (typeof(UInt64).IsAssignableFrom(type)) {
                return TypeCode.UInt64; 
            }
 
            // Unknown types 
            return TypeCode.Object;
        } 

        public override DesignerDataSourceView GetView(string viewName) {
            //
 

 
            string[] viewNames = GetViewNames(); 
            Debug.Assert(viewNames != null, "Did not expect null ViewNames array");
            if ((viewNames != null) && (viewNames.Length > 0)) { 
                if (String.IsNullOrEmpty(viewName)) {
                    viewName = viewNames[0];
                }
                foreach (string vn in viewNames) { 
                    if (String.Equals(viewName, vn, StringComparison.OrdinalIgnoreCase)) {
                        return new ObjectDesignerDataSourceView(this, viewName); 
                    } 
                }
            } 
            else {
                return new ObjectDesignerDataSourceView(this, String.Empty);
            }
 
            return null;
        } 
 
        public override string[] GetViewNames() {
            System.Collections.Generic.List<string> viewNames = new System.Collections.Generic.List<string>(); 
            DataTable[] schemaTables = LoadSchema();
            if ((schemaTables != null) && (schemaTables.Length > 0)) {
                foreach (DataTable dataTable in schemaTables) {
                    viewNames.Add(dataTable.TableName); 
                }
            } 
            return viewNames.ToArray(); 
        }
 
        /// <devdoc>
        /// Returns true if the given MethodInfo matches the specified method
        /// name and parameter names.
        /// </devdoc> 
        internal static bool IsMatchingMethod(MethodInfo method, string methodName, ParameterCollection parameters, Type dataObjectType) {
            // Test if name is the same 
            if (!String.Equals(methodName, method.Name, StringComparison.OrdinalIgnoreCase)) { 
                return false;
            } 

            ParameterInfo[] methodParameters = method.GetParameters();

            if (dataObjectType != null) { 
                // If DataObjectTypeName is set, we first try to match methods that take one or two parameters of that type
                if ((methodParameters.Length == 1 && methodParameters[0].ParameterType == dataObjectType) || 
                    (methodParameters.Length == 2 && methodParameters[0].ParameterType == dataObjectType && methodParameters[1].ParameterType == dataObjectType)) { 
                    return true;
                } 
            }

            // If we couldn't match based on type, then we try to match based on name
 
            // First check if the parameter counts match
            if (methodParameters.Length != parameters.Count) { 
                return false; 
            }
 
            // Build up a case-insensitive list of parameters
            Hashtable caseInsensitiveInputParameters = new Hashtable(StringComparer.Create(CultureInfo.InvariantCulture, true));
            foreach (Parameter p in parameters) {
                if (!caseInsensitiveInputParameters.Contains(p.Name)) { 
                    caseInsensitiveInputParameters.Add(p.Name, null);
                } 
            } 

            // Check if all the parameter names match 
            foreach (ParameterInfo pi in methodParameters) {
                if (!caseInsensitiveInputParameters.Contains(pi.Name)) {
                    return false;
                } 
            }
 
            return true; 
        }
 
        /// <devdoc>
        /// Attempts to load the schema for this ObjectDataSource. If the
        /// schema is not consistent with the current properties, then it is
        /// removed from state. 
        /// </devdoc>
        internal DataTable[] LoadSchema() { 
            if (!_forceSchemaRetrieval) { 
                // Only check for consistency if we are not forcing the retrieval
                string typeName = DesignerState[DesignerStateDataSourceSchemaTypeNameKey] as string; 
                string methodName = DesignerState[DesignerStateDataSourceSchemaSelectMethodKey] as string;

                if ((!String.Equals(typeName, TypeName, StringComparison.OrdinalIgnoreCase)) ||
                    (!String.Equals(methodName, SelectMethod, StringComparison.OrdinalIgnoreCase))) { 
                    return null;
                } 
            } 

            // Either we are forcing schema retrieval, or we're not forcing but we're consistent, so get the schema 
            DataTable[] schemaTables = null;
            Pair tableData = DesignerState[DesignerStateDataSourceSchemaKey] as Pair;
            if (tableData != null) {
                // Reconstruct the array of DataTables from the falsly-named 
                // tables and the list of the true names. See the SaveSchema()
                // method for more information. 
                string[] tableNames = tableData.First as string[]; 
                DataTable[] tables = tableData.Second as DataTable[];
                Debug.Assert((tableNames != null) && (tables != null), "Did not expect null arrays in table data schema"); 
                if ((tableNames != null) && (tables != null)) {
                    int tableCount = tableNames.Length;
                    schemaTables = new DataTable[tableCount];
                    for (int i = 0; i < tableCount; i++) { 
                        // Clone the saved table and set its true name
                        schemaTables[i] = tables[i].Clone(); 
                        schemaTables[i].TableName = tableNames[i]; 
                    }
                } 
            }
            return schemaTables;
        }
 
        /// <devdoc>
        /// Merges reflected parameters with an existing set of parameters. 
        /// </devdoc> 
        internal static Parameter[] MergeParameters(Parameter[] parameters, MethodInfo methodInfo) {
            ParameterInfo[] methodParameters = methodInfo.GetParameters(); 
            Parameter[] mergedParameters = new Parameter[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++) {
                ParameterInfo methodParameter = methodParameters[i];
                mergedParameters[i] = CreateMergedParameter(methodParameter, parameters); 
            }
            return mergedParameters; 
        } 

        /// <devdoc> 
        /// Merges reflected parameters with an existing set of parameters.
        /// </devdoc>
        internal static void MergeParameters(ParameterCollection parameters, MethodInfo methodInfo, Type dataObjectType) {
            Parameter[] oldParameters = new Parameter[parameters.Count]; 
            parameters.CopyTo(oldParameters, 0);
            parameters.Clear(); 
 
            if (methodInfo == null) {
                // No method is selected, do nothing 
                return;
            }
            if (dataObjectType == null) {
                // No DataObject, just reflect on the method's parameters 
                ParameterInfo[] methodParameters = methodInfo.GetParameters();
                foreach (ParameterInfo methodParameter in methodParameters) { 
                    Parameter newParam = CreateMergedParameter(methodParameter, oldParameters); 
                    if (parameters[newParam.Name] == null) {
                        parameters.Add(newParam); 
                    }
                }
            }
            else { 
                // DataObject is present, we don't need to create any parameters
                // since they will be created automatically at runtime. 
            } 
        }
 
        /// <devdoc>
        /// Returns whether two parameters match based on name, direction, and
        /// type.
        /// </devdoc> 
        private static bool ParametersMatch(ParameterInfo methodParameter, Parameter parameter) {
            // Check if names match 
            if (!String.Equals(methodParameter.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)) { 
                return false;
            } 

            // Check if directions match
            switch (parameter.Direction) {
                case ParameterDirection.ReturnValue: 
                    // We never have a return value ParameterInfo, so it never matches
                    return false; 
 
                case ParameterDirection.Input:
                    if (methodParameter.IsOut || methodParameter.ParameterType.IsByRef) { 
                        // Method parameter is out or ref, so it doesn't match
                        return false;
                    }
                    break; 

                case ParameterDirection.InputOutput: 
                    if (!methodParameter.ParameterType.IsByRef) { 
                        // Method parameter is not ref, so it doesn't match
                        return false; 
                    }
                    break;

                case ParameterDirection.Output: 
                    if (!methodParameter.IsOut) {
                        // Method parameter is not out, so it doesn't match 
                        return false; 
                    }
                    break; 
            }

            // Check if types match
            TypeCode methodParameterType = GetTypeCodeForType(methodParameter.ParameterType); 
            if (((methodParameterType == TypeCode.Object) || (methodParameterType == TypeCode.Empty)) &&
                ((parameter.Type == TypeCode.Object) || (parameter.Type == TypeCode.Empty))) { 
                // Effectively, the method's parameter type could not be 
                // detected, and the web parameter has no type either, so we
                // assume it's OK 
                return true;
            }
            // Both method parameter and web parameter have types, so compare them
            return (methodParameterType == parameter.Type); 
        }
 
        /// <include file='doc\ObjectDataSourceDesigner.uex' path='docs/doc[@for="ObjectDataSourceDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        /// Overridden by the designer to shadow various runtime properties 
        /// with corresponding properties that it implements.
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 

            PropertyDescriptor property; 
 
            // Shadow runtime TypeName property
            property = (PropertyDescriptor)properties["TypeName"]; 
            Debug.Assert(property != null);
            properties["TypeName"] = TypeDescriptor.CreateProperty(GetType(), property);

            // Shadow runtime SelectMethod property 
            property = (PropertyDescriptor)properties["SelectMethod"];
            Debug.Assert(property != null); 
            properties["SelectMethod"] = TypeDescriptor.CreateProperty(GetType(), property); 
        }
 
        public override void RefreshSchema(bool preferSilent) {
            try {
                SuppressDataSourceEvents();
 
                Debug.Assert(CanRefreshSchema, "CanRefreshSchema is false - RefreshSchema should not be called");
 
                Cursor originalCursor = Cursor.Current; 
                try {
                    Cursor.Current = Cursors.WaitCursor; 

                    // Try to load the type and find the appropriate method to get its schema
                    Type type = GetType(Component.Site, TypeName, preferSilent);
 
                    if (type == null) {
                        Debug.Fail("Did not expect a null type when calling ITypeResolutionService.GetType(typename, true, true)"); 
                        return; 
                    }
 
                    // Iterate through the methods of the type and try to find the best match
                    MethodInfo[] methods = type.GetMethods(MethodFilter);
                    MethodInfo methodInfo = null;
                    MethodInfo optimisticMethodMatch = null; 
                    bool optimismFailed = false;
                    Type dataObjectType = null; 
                    if (!String.IsNullOrEmpty(ObjectDataSource.DataObjectTypeName)) { 
                        dataObjectType = GetType(Component.Site, ObjectDataSource.DataObjectTypeName, preferSilent);
                    } 
                    foreach (MethodInfo mi in methods) {
                        if (String.Equals(mi.Name, SelectMethod, StringComparison.OrdinalIgnoreCase)) {
                            // We first optimistically match based on just the method name, however
                            // the optimism will fail if we find two methods with the same name but 
                            // with different return types
                            if ((optimisticMethodMatch != null) && (optimisticMethodMatch.ReturnType != mi.ReturnType)) { 
                                optimismFailed = true; 
                            }
                            else { 
                                optimisticMethodMatch = mi;
                            }

                            // If we find a perfect match, we can stop looking immediately 
                            if (IsMatchingMethod(mi, SelectMethod, ObjectDataSource.SelectParameters, dataObjectType)) {
                                methodInfo = mi; 
                                break; 
                            }
                        } 
                    }

                    // If we didn't find a true match, but instead we found an optimistic
                    // match, we can go ahead and use that 
                    if ((methodInfo == null) && (optimisticMethodMatch != null) && (!optimismFailed)) {
                        methodInfo = optimisticMethodMatch; 
                    } 

                    if (methodInfo != null) { 
                        RefreshSchema(methodInfo.ReflectedType, methodInfo.Name, methodInfo.ReturnType, preferSilent);
                    }
                }
                finally { 
                    Cursor.Current = originalCursor;
                } 
            } 
            finally {
                ResumeDataSourceEvents(); 
            }
        }

        /// <devdoc> 
        /// Refreshes the schema for this ObjectDataSourceDesigner. The new
        /// schema is automatically stored in DesignerState for persistence 
        /// across sessions. 
        /// </devdoc>
        internal void RefreshSchema(Type objectType, string methodName, Type schemaType, bool preferSilent) { 
            if ((objectType != null) && (!String.IsNullOrEmpty(methodName)) && (schemaType != null)) {
                // Get schema object for the requested type
                try {
                    TypeSchema schema = new TypeSchema(schemaType); 

                    // Store the schema into DesignerState. We can't store the actual 
                    // schema in designer state because it is not serializable, so we 
                    // wrap it in an array of DataTables with matching schema.
                    // We also store the current typename and method to make sure the 
                    // schema is valid.
                    _forceSchemaRetrieval = true;
                    DataTable[] oldSchemaTables = LoadSchema();
                    _forceSchemaRetrieval = false; 
                    IDataSourceSchema oldSchema = (oldSchemaTables == null ? null : new DataTableArraySchema(oldSchemaTables));
                    SaveSchema(objectType, methodName, ConvertSchemaToDataTables(schema), schemaType); 
                    DataTable[] newSchemaTables = LoadSchema(); 
                    IDataSourceSchema newSchema = (newSchemaTables == null ? null : new DataTableArraySchema(newSchemaTables));
 
                    if (!SchemasEquivalent(oldSchema, newSchema)) {
                        OnSchemaRefreshed(EventArgs.Empty);
                    }
                } 
                catch (Exception ex) {
                    if (!preferSilent) { 
                        UIServiceHelper.ShowError(Component.Site, ex, SR.GetString(SR.ObjectDataSourceDesigner_CannotGetSchema, schemaType.FullName)); 
                    }
                } 
            }
        }

        /// <devdoc> 
        /// Saves schema using the DesignerState. Along with the schema are
        /// stored the type and method used to generate the schema so that we 
        /// can make sure the schema is consistent. 
        /// </devdoc>
        private void SaveSchema(Type objectType, string methodName, DataTable[] schemaTables, Type schemaType) { 
            // DataTables without names cannot be serialized, so we store the
            // names in an array separately from the tables, and give all the
            // tables dummy names.
            Pair tableData = null; 
            if (schemaTables != null) {
                int tableCount = schemaTables.Length; 
                string[] tableNames = new string[tableCount]; 
                for (int i = 0; i < tableCount; i++) {
                    tableNames[i] = schemaTables[i].TableName; 
                    schemaTables[i].TableName = "Table" + i.ToString(CultureInfo.InvariantCulture);
                }
                tableData = new Pair(tableNames, schemaTables);
            } 
            DesignerState[DesignerStateDataSourceSchemaKey] = tableData;
            DesignerState[DesignerStateDataSourceSchemaTypeNameKey] = (objectType == null ? String.Empty : objectType.FullName); 
            DesignerState[DesignerStateDataSourceSchemaSelectMethodKey] = methodName; 

            // If the schema type changed, store the new type name and invalidate the cached return type 
            string oldReturnTypeName = DesignerState[DesignerStateDataSourceSchemaSelectMethodReturnTypeNameKey] as string;
            if (!String.Equals(oldReturnTypeName, schemaType.FullName, StringComparison.OrdinalIgnoreCase)) {
                DesignerState[DesignerStateDataSourceSchemaSelectMethodReturnTypeNameKey] = schemaType.FullName;
                _selectMethodReturnType = schemaType; 
            }
        } 
 
        /// <devdoc>
        /// Represents schema indicated by an array of DataTable objects. This 
        /// is similar to the DataSetSchema class, except that this one
        /// directly takes the array of DataTables instead of an entire
        /// DataSet.
        /// </devdoc> 
        private sealed class DataTableArraySchema : IDataSourceSchema {
            private DataTable[] _tables; 
 
            public DataTableArraySchema(DataTable[] tables) {
                _tables = tables; 
            }

            public IDataSourceViewSchema[] GetViews() {
                DataSetViewSchema[] viewSchemas = new DataSetViewSchema[_tables.Length]; 
                for (int i = 0; i < _tables.Length; i++) {
                    viewSchemas[i] = new DataSetViewSchema(_tables[i]); 
                } 
                return viewSchemas;
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
