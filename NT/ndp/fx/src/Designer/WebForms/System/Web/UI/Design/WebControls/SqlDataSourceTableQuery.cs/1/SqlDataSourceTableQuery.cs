//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceTableQuery.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.ComponentModel.Design.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Globalization;
    using System.Text.RegularExpressions; 
    using System.Text;
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls;
 
    /// <devdoc>
    /// Represents a filtered query against a table for a SqlDataSource. 
    /// </devdoc> 
    internal sealed class SqlDataSourceTableQuery {
 
        private DesignerDataConnection _designerDataConnection;
        private DesignerDataTableBase _designerDataTable;

        private System.Collections.Generic.IList<SqlDataSourceFilterClause> _filterClauses = new System.Collections.Generic.List<SqlDataSourceFilterClause>(); 
        private System.Collections.Generic.IList<SqlDataSourceOrderClause> _orderClauses = new System.Collections.Generic.List<SqlDataSourceOrderClause>();
        private bool _distinct; 
        private bool _asteriskField; 
        private System.Collections.Generic.IList<DesignerDataColumn> _fields = new System.Collections.Generic.List<DesignerDataColumn>();
 

        /// <devdoc>
        /// Creates a new SqlDataSourceTableQuery for a specified DesignerDataTable.
        /// </devdoc> 
        public SqlDataSourceTableQuery(DesignerDataConnection designerDataConnection, DesignerDataTableBase designerDataTable) {
            Debug.Assert(designerDataConnection != null); 
            Debug.Assert(designerDataTable != null); 
            _designerDataConnection = designerDataConnection;
            _designerDataTable = designerDataTable; 
        }


        public bool AsteriskField { 
            get {
                return _asteriskField; 
            } 
            set {
                _asteriskField = value; 
                if (value) {
                    // If setting the asterisk field, clear out all other fields
                    Fields.Clear();
                } 
            }
        } 
 
        public DesignerDataConnection DesignerDataConnection {
            get { 
                return _designerDataConnection;
            }
        }
 
        public DesignerDataTableBase DesignerDataTable {
            get { 
                return _designerDataTable; 
            }
        } 

        public bool Distinct {
            get {
                return _distinct; 
            }
            set { 
                _distinct = value; 
            }
        } 

        public System.Collections.Generic.IList<DesignerDataColumn> Fields {
            get {
                return _fields; 
            }
        } 
 
        public System.Collections.Generic.IList<SqlDataSourceFilterClause> FilterClauses {
            get { 
                return _filterClauses;
            }
        }
 
        public System.Collections.Generic.IList<SqlDataSourceOrderClause> OrderClauses {
            get { 
                return _orderClauses; 
            }
        } 


        private bool CanAutoGenerateQueries() {
            // We should never generate modification queries from queries that aggregate data 
            if (Distinct) {
                return false; 
            } 

            // Abort if there are no fields in the query 
            if ((!AsteriskField) && (_fields.Count == 0)) {
                return false;
            }
 
            return true;
        } 
 
        /// <devdoc>
        /// Creates a shallow copy of this table query object. 
        /// </devdoc>
        public SqlDataSourceTableQuery Clone() {
            SqlDataSourceTableQuery clone = new SqlDataSourceTableQuery(DesignerDataConnection, DesignerDataTable);
            clone.Distinct = Distinct; 
            clone.AsteriskField = AsteriskField;
            foreach (DesignerDataColumn field in Fields) { 
                clone.Fields.Add(field); 
            }
            foreach (SqlDataSourceFilterClause clause in FilterClauses) { 
                //
                clone.FilterClauses.Add(clause);
            }
            foreach (SqlDataSourceOrderClause clause in OrderClauses) { 
                clone.OrderClauses.Add(clause);
            } 
            return clone; 
        }
 
        /// <devdoc>
        /// Attempts to generate a DELETE command for the current query.
        /// If the DELETE command cannot be generated, the return value is null.
        /// </devdoc> 
        public SqlDataSourceQuery GetDeleteQuery(string oldValuesFormatString, bool includeOldValues) {
            if (!CanAutoGenerateQueries()) { 
                return null; 
            }
 
            StringBuilder commandText = new StringBuilder("DELETE FROM ");
            commandText.Append(GetTableName());

            // For DELETE commands if we generate pessimistic queries, we create parameters 
            // for the old values, however their names do not include the format string.
            SqlDataSourceQuery whereClause = GetWhereClause(oldValuesFormatString, includeOldValues); 
            // If there was a problem generating the WHERE clause, abort 
            if (whereClause == null) {
                return null; 
            }
            commandText.Append(whereClause.Command);

 
            return new SqlDataSourceQuery(commandText.ToString(), SqlDataSourceCommandType.Text, whereClause.Parameters);
        } 
 
        /// <devdoc>
        /// Attempts to generate an INSERT command for the current query. 
        /// If the INSERT command cannot be generated, the return value is null.
        /// </devdoc>
        public SqlDataSourceQuery GetInsertQuery() {
            if (!CanAutoGenerateQueries()) { 
                return null;
            } 
 
            System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>();
 
            StringBuilder commandText = new StringBuilder("INSERT INTO ");
            commandText.Append(GetTableName());

            // Get a list of the fields that would be selected 
            System.Collections.Generic.List<SqlDataSourceColumnData> fields = GetEffectiveColumns();
 
            // Generate insert clauses 
            StringBuilder fieldNames = new StringBuilder();
            StringBuilder fieldValues = new StringBuilder(); 
            bool firstField = true;
            foreach (SqlDataSourceColumnData columnData in fields) {
                // Never insert into identity columns
                if (columnData.Column.Identity) { 
                    continue;
                } 
 
                if (!firstField) {
                    fieldNames.Append(", "); 
                    fieldValues.Append(", ");
                }

                fieldNames.Append(columnData.EscapedName); 
                fieldValues.Append(columnData.ParameterPlaceholder);
 
                // Create a Web Parameter for this column 
                parameters.Add(new Parameter(columnData.WebParameterName, SqlDataSourceDesigner.ConvertDbTypeToTypeCode(columnData.Column.DataType)));
 
                firstField = false;
            }

            if (firstField) { 
                // If we haven't found any valid (non-identity) fields yet, do not generate an insert statement at all
                return null; 
            } 

            commandText.Append(" ("); 
            commandText.Append(fieldNames.ToString());

            commandText.Append(") VALUES (");
            commandText.Append(fieldValues.ToString()); 
            commandText.Append(")");
 
 
            return new SqlDataSourceQuery(commandText.ToString(), SqlDataSourceCommandType.Text, parameters);
        } 

        /// <devdoc>
        /// Gets the list of columns that would be retrieved by the SELECT
        /// statement. For example, if the asterisk column is selected, then 
        /// all columns are returned. Otherwise, only the specifically
        /// selected columns are returned. 
        /// </devdoc> 
        private System.Collections.Generic.List<SqlDataSourceColumnData> GetEffectiveColumns() {
            StringCollection usedNames = new StringCollection(); 
            System.Collections.Generic.List<SqlDataSourceColumnData> columns = new System.Collections.Generic.List<SqlDataSourceColumnData>();

            // Get a list of the fields that would be selected
            if (AsteriskField) { 
                foreach (DesignerDataColumn designerDataColumn in _designerDataTable.Columns) {
                    columns.Add(new SqlDataSourceColumnData(DesignerDataConnection, designerDataColumn, usedNames)); 
                } 
            }
            else { 
                foreach (DesignerDataColumn designerDataColumn in _fields) {
                    columns.Add(new SqlDataSourceColumnData(DesignerDataConnection, designerDataColumn, usedNames));
                }
            } 
            return columns;
        } 
 
        /// <devdoc>
        /// Generates a SELECT command for the current query. 
        /// </devdoc>
        public SqlDataSourceQuery GetSelectQuery() {
            // Return empty string if no fields are selected
            if ((!_asteriskField) && (_fields.Count == 0)) { 
                return null;
            } 
 
            StringBuilder commandText = new StringBuilder(2048);
 
            // Select
            commandText.Append("SELECT");

            // Distinct 
            if (_distinct) {
                commandText.Append(" DISTINCT"); 
            } 

            // Fields 
            if (_asteriskField) {
                Debug.Assert(_fields.Count == 0);
                // Special case the asterisk field since it is always by itself
                commandText.Append(" "); 
                SqlDataSourceColumnData columnData = new SqlDataSourceColumnData(DesignerDataConnection, null);
                commandText.Append(columnData.SelectName); 
            } 
            if (_fields.Count > 0) {
                Debug.Assert(!_asteriskField); 
                // If not the asterisk field, iterate through columns
                commandText.Append(" ");
                bool firstField = true;
                System.Collections.Generic.List<SqlDataSourceColumnData> fields = GetEffectiveColumns(); 
                foreach (SqlDataSourceColumnData columnData in fields) {
                    if (!firstField) { 
                        commandText.Append(", "); 
                    }
                    commandText.Append(columnData.SelectName); 
                    firstField = false;
                }
            }
 
            // From
            commandText.Append(" FROM"); 
 
            // Table
            commandText.Append(" " + GetTableName()); 

            System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>();

            if (_filterClauses.Count > 0) { 
                // Where
                commandText.Append(" WHERE "); 
                if (_filterClauses.Count > 1) { 
                    commandText.Append("(");
                } 

                // Clauses
                bool firstClause = true;
                foreach (SqlDataSourceFilterClause filterClause in _filterClauses) { 
                    if (!firstClause) {
                        commandText.Append(" AND "); 
                    } 
                    commandText.Append("(" + filterClause.ToString() + ")");
                    firstClause = false; 

                    if (filterClause.Parameter != null) {
                        parameters.Add(filterClause.Parameter);
                    } 
                }
 
                if (_filterClauses.Count > 1) { 
                    commandText.Append(")");
                } 
            }

            if (_orderClauses.Count > 0) {
                // Order by 
                commandText.Append(" ORDER BY ");
 
                // Clauses 
                bool firstClause = true;
                foreach (SqlDataSourceOrderClause orderClause in _orderClauses) { 
                    if (!firstClause) {
                        commandText.Append(", ");
                    }
                    commandText.Append(orderClause.ToString()); 
                    firstClause = false;
                } 
            } 

            string selectCommandText = commandText.ToString(); 
            return new SqlDataSourceQuery(selectCommandText, SqlDataSourceCommandType.Text, parameters.ToArray());
        }

        /// <devdoc> 
        /// Creates an escaped table name.
        /// </devdoc> 
        public string GetTableName() { 
            return SqlDataSourceColumnData.EscapeObjectName(DesignerDataConnection, DesignerDataTable.Name);
        } 

        /// <devdoc>
        /// Attempts to generate an UPDATE command for the current query.
        /// If the UPDATE command cannot be generated, the return value is null. 
        /// </devdoc>
        public SqlDataSourceQuery GetUpdateQuery(string oldValuesFormatString, bool includeOldValues) { 
            if (!CanAutoGenerateQueries()) { 
                return null;
            } 

            StringBuilder commandText = new StringBuilder("UPDATE ");
            commandText.Append(GetTableName());
            commandText.Append(" SET "); 

 
            // Get a list of the fields that would be selected 
            System.Collections.Generic.List<SqlDataSourceColumnData> fields = GetEffectiveColumns();
 
            System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>();

            // Generate update clauses
            bool firstField = true; 
            foreach (SqlDataSourceColumnData columnData in fields) {
                // Never update primary key columns 
                if (columnData.Column.PrimaryKey) { 
                    continue;
                } 

                if (!firstField) {
                    commandText.Append(", ");
                } 

                commandText.Append(columnData.EscapedName); 
                commandText.Append(" = "); 
                commandText.Append(columnData.ParameterPlaceholder);
 
                firstField = false;

                // Create a Web Parameter for this column
                parameters.Add(new Parameter(columnData.WebParameterName, SqlDataSourceDesigner.ConvertDbTypeToTypeCode(columnData.Column.DataType))); 
            }
 
 
            // Abort if there are no updateable fields in the query
            if (firstField) { 
                return null;
            }

            // For UPDATE commands if we generate pessimistic queries, we do not create 
            // parameters for the old values.
            SqlDataSourceQuery whereClause = GetWhereClause(oldValuesFormatString, includeOldValues); 
            // If there was a problem generating the WHERE clause, abort 
            if (whereClause == null) {
                return null; 
            }
            commandText.Append(whereClause.Command);

            // Append the WHERE clause parameters to the UPDATE parameters 
            foreach (Parameter p in whereClause.Parameters) {
                parameters.Add(p); 
            } 
            return new SqlDataSourceQuery(commandText.ToString(), SqlDataSourceCommandType.Text, parameters);
        } 

        /// <devdoc>
        /// Attempts to generate a WHERE clause for the current query.
        /// For this to succeed, all columns in the primary key of the table 
        /// must be in the query, either through the catch-all asterisk
        /// field, or individual selected fields. 
        /// If the WHERE clause cannot be generated, the return value is null. 
        /// </devdoc>
        private SqlDataSourceQuery GetWhereClause(string oldValuesFormatString, bool includeOldValues) { 
            // Get a list of the fields that would be selected
            System.Collections.Generic.List<SqlDataSourceColumnData> fields = GetEffectiveColumns();

            System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>(); 

            // Abort if there are no fields in the query 
            if (fields.Count == 0) { 
                return null;
            } 


            // Build the WHERE clause out of the primary keys and possible old values
            StringBuilder commandText = new StringBuilder(); 
            commandText.Append(" WHERE ");
            int columnsInQuery = 0; 
 
            // First do primary key fields
            foreach (SqlDataSourceColumnData columnData in fields) { 
                if (columnData.Column.PrimaryKey) {
                    if (columnsInQuery > 0) {
                        commandText.Append(" AND ");
                    } 
                    columnsInQuery++;
                    commandText.Append(columnData.EscapedName); 
                    commandText.Append(" = "); 
                    commandText.Append(columnData.GetOldValueParameterPlaceHolder(oldValuesFormatString));
 
                    // Create a Web Parameter for this column
                    parameters.Add(new Parameter(columnData.GetOldValueWebParameterName(oldValuesFormatString), SqlDataSourceDesigner.ConvertDbTypeToTypeCode(columnData.Column.DataType)));
                }
            } 

            if (columnsInQuery == 0) { 
                // If there are no columns yet in the query that means there 
                // are no primary keys selected, and we do not allow that for
                // our auto-generated queries. 
                return null;
            }

            // Then do non-primary key fields 
            if (includeOldValues) {
                foreach (SqlDataSourceColumnData columnData in fields) { 
                    if (!columnData.Column.PrimaryKey) { 
                        commandText.Append(" AND ");
                        columnsInQuery++; 
                        commandText.Append(columnData.EscapedName);
                        commandText.Append(" = ");
                        commandText.Append(columnData.GetOldValueParameterPlaceHolder(oldValuesFormatString));
 
                        // Create a Web Parameter for this column
                        parameters.Add(new Parameter(columnData.GetOldValueWebParameterName(oldValuesFormatString), SqlDataSourceDesigner.ConvertDbTypeToTypeCode(columnData.Column.DataType))); 
                    } 
                }
            } 

            return new SqlDataSourceQuery(commandText.ToString(), SqlDataSourceCommandType.Text, parameters);
        }
 
        /// <devdoc>
        /// Returns true if the entire primary key is selected (i.e. all the 
        /// columns in the primary key are part of the query). 
        /// </devdoc>
        public bool IsPrimaryKeySelected() { 
            // Get a list of the fields that would be selected
            System.Collections.Generic.List<SqlDataSourceColumnData> fields = GetEffectiveColumns();

            // Abort if there are no fields in the query 
            if (fields.Count == 0) {
                return false; 
            } 

            // Count how many columns are in the primary key to make sure we got them all 
            int columnsInPrimaryKey = 0;
            foreach (DesignerDataColumn designerDataColumn in _designerDataTable.Columns) {
                if (designerDataColumn.PrimaryKey) {
                    columnsInPrimaryKey++; 
                }
            } 
 
            // Abort if there is no primary key
            if (columnsInPrimaryKey == 0) { 
                return false;
            }

            // See how many columns that are selected are in the primary key 
            int columnsInQuery = 0;
            foreach (SqlDataSourceColumnData columnData in fields) { 
                if (columnData.Column.PrimaryKey) { 
                    columnsInQuery++;
                } 
            }

            // Check if all the primary key columns were in the query
            return (columnsInPrimaryKey == columnsInQuery); 
        }
    } 
 
    /// <devdoc>
    /// Represents an individual filter clause of a query against a table for a SqlDataSource. 
    /// E.g. field1 >= @field1
    /// </devdoc>
    internal sealed class SqlDataSourceFilterClause {
 
        private DesignerDataColumn _designerDataColumn;
        private DesignerDataTableBase _designerDataTable; 
        private DesignerDataConnection _designerDataConnection; 
        private bool _isBinary;
        private string _operatorFormat; 
        private string _value;
        private Parameter _parameter;

        /// <devdoc> 
        /// Creates a new SqlDataSourceFilterClause with the specified constraints.
        /// </devdoc> 
        public SqlDataSourceFilterClause(DesignerDataConnection designerDataConnection, DesignerDataTableBase designerDataTable, DesignerDataColumn designerDataColumn, string operatorFormat, bool isBinary, string value, Parameter parameter) { 
            Debug.Assert(designerDataConnection != null);
            Debug.Assert(designerDataTable != null); 
            Debug.Assert(designerDataColumn != null);
            Debug.Assert(operatorFormat != null && operatorFormat.Length > 0);
            //Debug.Assert(isBinary ^ (value != null && value.Length == 0));
 
            _designerDataConnection = designerDataConnection;
            _designerDataTable = designerDataTable; 
            _designerDataColumn = designerDataColumn; 
            _isBinary = isBinary;
            _operatorFormat = operatorFormat; 
            _value = value;
            _parameter = parameter;
        }
 
        public DesignerDataColumn DesignerDataColumn {
            get { 
                return _designerDataColumn; 
            }
        } 

        public bool IsBinary {
            get {
                return _isBinary; 
            }
        } 
 
        public string OperatorFormat {
            get { 
                return _operatorFormat;
            }
        }
 
        public Parameter Parameter {
            get { 
                return _parameter; 
            }
        } 

        public string Value {
            get {
                return _value; 
            }
        } 
 
        public override string ToString() {
            // Note: Can't show parameter expression here because this is used 
            // to generate actual commands. We'd need another method for that.
            SqlDataSourceColumnData columnData = new SqlDataSourceColumnData(_designerDataConnection, _designerDataColumn);
            if (_isBinary) {
                return String.Format(CultureInfo.InvariantCulture, _operatorFormat, columnData.EscapedName, _value); 
            }
            else { 
                return String.Format(CultureInfo.InvariantCulture, _operatorFormat, columnData.EscapedName); 
            }
        } 
    }

    /// <devdoc>
    /// Represents an individual order clause of a query against a table for a SqlDataSource. 
    /// E.g. ORDER BY field1 DESC
    /// SQL Books Online reference: 
    /// mk:@MSITStore:C:\Program%20Files\Microsoft%20SQL%20Server%20Books%20Online\1033\tsqlref.chm::/ts_sa-ses_9sfo.htm#_order_by_clause 
    /// The ORDER BY clause can include items not appearing in the select list. However, if SELECT DISTINCT is specified,
    /// or if the SELECT statement contains a UNION operator, the sort columns must appear in the select list. 
    /// </devdoc>
    internal sealed class SqlDataSourceOrderClause {

        private DesignerDataColumn _designerDataColumn; 
        private DesignerDataTableBase _designerDataTable;
        private DesignerDataConnection _designerDataConnection; 
        private bool _isDescending; 

        /// <devdoc> 
        /// Creates a new SqlDataSourceOrderClause with the specified constraints.
        /// </devdoc>
        public SqlDataSourceOrderClause(DesignerDataConnection designerDataConnection, DesignerDataTableBase designerDataTable, DesignerDataColumn designerDataColumn, bool isDescending) {
            Debug.Assert(designerDataConnection != null); 
            Debug.Assert(designerDataTable != null);
            Debug.Assert(designerDataColumn != null); 
 
            _designerDataConnection = designerDataConnection;
            _designerDataTable = designerDataTable; 
            _designerDataColumn = designerDataColumn;
            _isDescending = isDescending;
        }
 
        public DesignerDataColumn DesignerDataColumn {
            get { 
                return _designerDataColumn; 
            }
        } 

        public bool IsDescending {
            get {
                return _isDescending; 
            }
        } 
 
        public override string ToString() {
            SqlDataSourceColumnData columnData = new SqlDataSourceColumnData(_designerDataConnection, _designerDataColumn); 
            if (_isDescending) {
                return columnData.EscapedName + " DESC";
            }
            else { 
                return columnData.EscapedName;
            } 
        } 
    }
 
    internal sealed class SqlDataSourceColumnData {

        private DesignerDataConnection _connection;
        private DesignerDataColumn _column; 
        private StringCollection _usedNames;
 
        private string _cachedAliasedName; 
        private string _cachedEscapedName;
        private string _cachedParameterPlaceholder; 
        private string _cachedWebParameterName;


        public SqlDataSourceColumnData(DesignerDataConnection connection, DesignerDataColumn column) 
            : this(connection, column, null) {
        } 
 
        public SqlDataSourceColumnData(DesignerDataConnection connection, DesignerDataColumn column, StringCollection usedNames) {
            _connection = connection; 
            _column = column;
            _usedNames = usedNames;
        }
 
        /// <devdoc>
        /// Gets the aliased name of this column. This is done by stripping 
        /// out invalid characters. 
        /// </devdoc>
        public string AliasedName { 
            get {
                if (_cachedAliasedName == null) {
                    _cachedAliasedName = CreateAliasedName();
                } 
                return _cachedAliasedName;
            } 
        } 

        /// <devdoc> 
        /// Gets the column that this object represents.
        /// </devdoc>
        public DesignerDataColumn Column {
            get { 
                return _column;
            } 
        } 

        /// <devdoc> 
        /// Gets how this column should be represented in a SELECT statement.
        /// This can be in the form [table].[column foo] AS column_foo
        /// </devdoc>
        public string SelectName { 
            get {
                if (_column == null) { 
                    return EscapedName; 
                }
                else { 
                    string alias = AliasedName;
                    if (alias != _column.Name) {
                        // Only use the alias if it is different from the original column name
                        return EscapedName + " AS " + AliasedName; 
                    }
                    else { 
                        return EscapedName; 
                    }
                } 
            }
        }

        /// <devdoc> 
        /// Gets the full name of the column, qualified by the table name.
        /// </devdoc> 
        public string EscapedName { 
            get {
                if (_cachedEscapedName == null) { 
                    _cachedEscapedName = CreateEscapedName();
                }
                return _cachedEscapedName;
            } 
        }
 
        /// <devdoc> 
        /// Creates a parameter placeholder for use with this column.
        /// </devdoc> 
        public string ParameterPlaceholder {
            get {
                if (_cachedParameterPlaceholder == null) {
                    _cachedParameterPlaceholder = CreateParameterPlaceholder(null); 
                }
                return _cachedParameterPlaceholder; 
            } 
        }
 
        /// <devdoc>
        /// Creates a parameter name for use with this column.
        /// </devdoc>
        public string WebParameterName { 
            get {
                if (_cachedWebParameterName == null) { 
                    _cachedWebParameterName = CreateWebParameterName(null); 
                }
                return _cachedWebParameterName; 
            }
        }

        /// <devdoc> 
        /// Escapes an object identifier using the provider's escaping
        /// mechanism and format. 
        /// </devdoc> 
        internal static string EscapeObjectName(DesignerDataConnection connection, string objectName) {
            string quotePrefix = "["; 
            string quoteSuffix = "]";

            try {
                DbProviderFactory factory = SqlDataSourceDesigner.GetDbProviderFactory(connection.ProviderName); 
                DbCommandBuilder builder = factory.CreateCommandBuilder();
                // It would be nice if we could find the prefix/suffix from metadata, but the info isn't there. 
                // This is required now because otherwise QuoteIdentifier() requires an open connection. 
                // Until we can get the info from the schema without an open connection, switch on the factory type.
                if (factory == System.Data.OracleClient.OracleClientFactory.Instance) { 
                    quotePrefix = quoteSuffix = "\"";
                }
                builder.QuotePrefix = quotePrefix;
                builder.QuoteSuffix = quoteSuffix; 
                return builder.QuoteIdentifier(objectName);
            } 
            catch (Exception ex) { 
                Debug.WriteLine(ex);
                // NOTE: This is a temporary workaround for SQL connections and will be 
                // removed when the above code is changed to use metadata services.
                return quotePrefix + objectName + quoteSuffix;
            }
        } 

        /// <devdoc> 
        /// Creates a parameter placeholder for the column. 
        /// "FooColumn" --> "FooColumn" (all valid chars)
        /// "Foo   Column" --> "Foo_Column" (whitespace gets collapsed to single underscore) 
        /// "Foo!Column" --> "column1" (any non-letter/non-digit/non-whitespace/non-underscore
        ///                             characters invalidate the entire name)
        /// </devdoc>
        private string CreateAliasedName() { 
            string name = _column.Name;
            Debug.Assert(!String.IsNullOrEmpty(name), "Expected a column name to exist already"); 
            StringBuilder sb = new StringBuilder(); 
            bool hasInvalidChars = false;
            bool lastCharWasSpace = false; 
            foreach (char c in name) {
                if (Char.IsWhiteSpace(c) || (c == '_')) {
                    if (!lastCharWasSpace) {
                        // Never have more than one underscore replacing a space in a row 
                        sb.Append('_');
                        lastCharWasSpace = true; 
                    } 
                }
                else { 
                    if (Char.IsLetterOrDigit(c)) {
                        sb.Append(c);
                        lastCharWasSpace = false;
                    } 
                    else {
                        hasInvalidChars = true; 
                        break; 
                    }
                } 
            }

            // If the column name still starts with invalid characters, start from scratch
            if (sb.Length == 0 || !Char.IsLetter(sb[0])) { 
                hasInvalidChars = true;
            } 
 
            int count;
            string initialAliasedName; 
            string uniqueAliasedName;
            if (hasInvalidChars) {
                // If there are no valid characters at all, create a dummy name
                initialAliasedName = "column"; 
                count = 1;
                uniqueAliasedName = initialAliasedName + '1'; 
            } 
            else {
                // There are valid characters in the name, but we still might 
                // have to validate uniqueness
                count = 2;
                initialAliasedName = sb.ToString();
                uniqueAliasedName = initialAliasedName; 
            }
 
            // Ensure uniqueness by comparing against existing name list, if available 
            if (_usedNames != null) {
                if (_usedNames.Contains(uniqueAliasedName)) { 
                    //
                    do {
                        uniqueAliasedName = initialAliasedName + count.ToString(CultureInfo.InvariantCulture);
                        count++; 
                    }
                    while (_usedNames.Contains(uniqueAliasedName)); 
                } 

                // Add new name to list 
                _usedNames.Add(uniqueAliasedName);
            }
            return uniqueAliasedName;
        } 

        /// <devdoc> 
        /// Creates an escaped and qualified column name. If the column is 
        /// null, it is assumed to be the asterisk column.
        /// It generates names like [CustomerID] and * 
        /// </devdoc>
        private string CreateEscapedName() {
            StringBuilder sb = new StringBuilder();
            if (_column == null) { 
                sb.Append("*");
            } 
            else { 
                sb.Append(EscapeObjectName(_connection, _column.Name));
            } 
            return sb.ToString();
        }

        /// <devdoc> 
        /// Creates a parameter placeholder for a given column.
        /// </devdoc> 
        private string CreateParameterPlaceholder(string oldValueFormatString) { 
            DbProviderFactory factory = SqlDataSourceDesigner.GetDbProviderFactory(_connection.ProviderName);
            string paramPlaceHolderPrefix = SqlDataSourceDesigner.GetParameterPlaceholderPrefix(factory); 
            string parameterName = paramPlaceHolderPrefix;
            if (SqlDataSourceDesigner.SupportsNamedParameters(factory)) {
                if (oldValueFormatString == null) {
                    parameterName += AliasedName; 
                }
                else { 
                    parameterName += String.Format(CultureInfo.InvariantCulture, oldValueFormatString, AliasedName); 
                }
            } 
            return parameterName;
        }

        /// <devdoc> 
        /// Creates a parameter name for a given column.
        /// </devdoc> 
        private string CreateWebParameterName(string oldValueFormatString) { 
            if (oldValueFormatString == null) {
                return AliasedName; 
            }
            else {
                return String.Format(CultureInfo.InvariantCulture, oldValueFormatString, AliasedName);
            } 
        }
 
        public string GetOldValueParameterPlaceHolder(string oldValueFormatString) { 
            Debug.Assert(oldValueFormatString != null);
            return CreateParameterPlaceholder(oldValueFormatString); 
        }

        public string GetOldValueWebParameterName(string oldValueFormatString) {
            Debug.Assert(oldValueFormatString != null); 
            return CreateWebParameterName(oldValueFormatString);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceTableQuery.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.ComponentModel.Design.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Globalization;
    using System.Text.RegularExpressions; 
    using System.Text;
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls;
 
    /// <devdoc>
    /// Represents a filtered query against a table for a SqlDataSource. 
    /// </devdoc> 
    internal sealed class SqlDataSourceTableQuery {
 
        private DesignerDataConnection _designerDataConnection;
        private DesignerDataTableBase _designerDataTable;

        private System.Collections.Generic.IList<SqlDataSourceFilterClause> _filterClauses = new System.Collections.Generic.List<SqlDataSourceFilterClause>(); 
        private System.Collections.Generic.IList<SqlDataSourceOrderClause> _orderClauses = new System.Collections.Generic.List<SqlDataSourceOrderClause>();
        private bool _distinct; 
        private bool _asteriskField; 
        private System.Collections.Generic.IList<DesignerDataColumn> _fields = new System.Collections.Generic.List<DesignerDataColumn>();
 

        /// <devdoc>
        /// Creates a new SqlDataSourceTableQuery for a specified DesignerDataTable.
        /// </devdoc> 
        public SqlDataSourceTableQuery(DesignerDataConnection designerDataConnection, DesignerDataTableBase designerDataTable) {
            Debug.Assert(designerDataConnection != null); 
            Debug.Assert(designerDataTable != null); 
            _designerDataConnection = designerDataConnection;
            _designerDataTable = designerDataTable; 
        }


        public bool AsteriskField { 
            get {
                return _asteriskField; 
            } 
            set {
                _asteriskField = value; 
                if (value) {
                    // If setting the asterisk field, clear out all other fields
                    Fields.Clear();
                } 
            }
        } 
 
        public DesignerDataConnection DesignerDataConnection {
            get { 
                return _designerDataConnection;
            }
        }
 
        public DesignerDataTableBase DesignerDataTable {
            get { 
                return _designerDataTable; 
            }
        } 

        public bool Distinct {
            get {
                return _distinct; 
            }
            set { 
                _distinct = value; 
            }
        } 

        public System.Collections.Generic.IList<DesignerDataColumn> Fields {
            get {
                return _fields; 
            }
        } 
 
        public System.Collections.Generic.IList<SqlDataSourceFilterClause> FilterClauses {
            get { 
                return _filterClauses;
            }
        }
 
        public System.Collections.Generic.IList<SqlDataSourceOrderClause> OrderClauses {
            get { 
                return _orderClauses; 
            }
        } 


        private bool CanAutoGenerateQueries() {
            // We should never generate modification queries from queries that aggregate data 
            if (Distinct) {
                return false; 
            } 

            // Abort if there are no fields in the query 
            if ((!AsteriskField) && (_fields.Count == 0)) {
                return false;
            }
 
            return true;
        } 
 
        /// <devdoc>
        /// Creates a shallow copy of this table query object. 
        /// </devdoc>
        public SqlDataSourceTableQuery Clone() {
            SqlDataSourceTableQuery clone = new SqlDataSourceTableQuery(DesignerDataConnection, DesignerDataTable);
            clone.Distinct = Distinct; 
            clone.AsteriskField = AsteriskField;
            foreach (DesignerDataColumn field in Fields) { 
                clone.Fields.Add(field); 
            }
            foreach (SqlDataSourceFilterClause clause in FilterClauses) { 
                //
                clone.FilterClauses.Add(clause);
            }
            foreach (SqlDataSourceOrderClause clause in OrderClauses) { 
                clone.OrderClauses.Add(clause);
            } 
            return clone; 
        }
 
        /// <devdoc>
        /// Attempts to generate a DELETE command for the current query.
        /// If the DELETE command cannot be generated, the return value is null.
        /// </devdoc> 
        public SqlDataSourceQuery GetDeleteQuery(string oldValuesFormatString, bool includeOldValues) {
            if (!CanAutoGenerateQueries()) { 
                return null; 
            }
 
            StringBuilder commandText = new StringBuilder("DELETE FROM ");
            commandText.Append(GetTableName());

            // For DELETE commands if we generate pessimistic queries, we create parameters 
            // for the old values, however their names do not include the format string.
            SqlDataSourceQuery whereClause = GetWhereClause(oldValuesFormatString, includeOldValues); 
            // If there was a problem generating the WHERE clause, abort 
            if (whereClause == null) {
                return null; 
            }
            commandText.Append(whereClause.Command);

 
            return new SqlDataSourceQuery(commandText.ToString(), SqlDataSourceCommandType.Text, whereClause.Parameters);
        } 
 
        /// <devdoc>
        /// Attempts to generate an INSERT command for the current query. 
        /// If the INSERT command cannot be generated, the return value is null.
        /// </devdoc>
        public SqlDataSourceQuery GetInsertQuery() {
            if (!CanAutoGenerateQueries()) { 
                return null;
            } 
 
            System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>();
 
            StringBuilder commandText = new StringBuilder("INSERT INTO ");
            commandText.Append(GetTableName());

            // Get a list of the fields that would be selected 
            System.Collections.Generic.List<SqlDataSourceColumnData> fields = GetEffectiveColumns();
 
            // Generate insert clauses 
            StringBuilder fieldNames = new StringBuilder();
            StringBuilder fieldValues = new StringBuilder(); 
            bool firstField = true;
            foreach (SqlDataSourceColumnData columnData in fields) {
                // Never insert into identity columns
                if (columnData.Column.Identity) { 
                    continue;
                } 
 
                if (!firstField) {
                    fieldNames.Append(", "); 
                    fieldValues.Append(", ");
                }

                fieldNames.Append(columnData.EscapedName); 
                fieldValues.Append(columnData.ParameterPlaceholder);
 
                // Create a Web Parameter for this column 
                parameters.Add(new Parameter(columnData.WebParameterName, SqlDataSourceDesigner.ConvertDbTypeToTypeCode(columnData.Column.DataType)));
 
                firstField = false;
            }

            if (firstField) { 
                // If we haven't found any valid (non-identity) fields yet, do not generate an insert statement at all
                return null; 
            } 

            commandText.Append(" ("); 
            commandText.Append(fieldNames.ToString());

            commandText.Append(") VALUES (");
            commandText.Append(fieldValues.ToString()); 
            commandText.Append(")");
 
 
            return new SqlDataSourceQuery(commandText.ToString(), SqlDataSourceCommandType.Text, parameters);
        } 

        /// <devdoc>
        /// Gets the list of columns that would be retrieved by the SELECT
        /// statement. For example, if the asterisk column is selected, then 
        /// all columns are returned. Otherwise, only the specifically
        /// selected columns are returned. 
        /// </devdoc> 
        private System.Collections.Generic.List<SqlDataSourceColumnData> GetEffectiveColumns() {
            StringCollection usedNames = new StringCollection(); 
            System.Collections.Generic.List<SqlDataSourceColumnData> columns = new System.Collections.Generic.List<SqlDataSourceColumnData>();

            // Get a list of the fields that would be selected
            if (AsteriskField) { 
                foreach (DesignerDataColumn designerDataColumn in _designerDataTable.Columns) {
                    columns.Add(new SqlDataSourceColumnData(DesignerDataConnection, designerDataColumn, usedNames)); 
                } 
            }
            else { 
                foreach (DesignerDataColumn designerDataColumn in _fields) {
                    columns.Add(new SqlDataSourceColumnData(DesignerDataConnection, designerDataColumn, usedNames));
                }
            } 
            return columns;
        } 
 
        /// <devdoc>
        /// Generates a SELECT command for the current query. 
        /// </devdoc>
        public SqlDataSourceQuery GetSelectQuery() {
            // Return empty string if no fields are selected
            if ((!_asteriskField) && (_fields.Count == 0)) { 
                return null;
            } 
 
            StringBuilder commandText = new StringBuilder(2048);
 
            // Select
            commandText.Append("SELECT");

            // Distinct 
            if (_distinct) {
                commandText.Append(" DISTINCT"); 
            } 

            // Fields 
            if (_asteriskField) {
                Debug.Assert(_fields.Count == 0);
                // Special case the asterisk field since it is always by itself
                commandText.Append(" "); 
                SqlDataSourceColumnData columnData = new SqlDataSourceColumnData(DesignerDataConnection, null);
                commandText.Append(columnData.SelectName); 
            } 
            if (_fields.Count > 0) {
                Debug.Assert(!_asteriskField); 
                // If not the asterisk field, iterate through columns
                commandText.Append(" ");
                bool firstField = true;
                System.Collections.Generic.List<SqlDataSourceColumnData> fields = GetEffectiveColumns(); 
                foreach (SqlDataSourceColumnData columnData in fields) {
                    if (!firstField) { 
                        commandText.Append(", "); 
                    }
                    commandText.Append(columnData.SelectName); 
                    firstField = false;
                }
            }
 
            // From
            commandText.Append(" FROM"); 
 
            // Table
            commandText.Append(" " + GetTableName()); 

            System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>();

            if (_filterClauses.Count > 0) { 
                // Where
                commandText.Append(" WHERE "); 
                if (_filterClauses.Count > 1) { 
                    commandText.Append("(");
                } 

                // Clauses
                bool firstClause = true;
                foreach (SqlDataSourceFilterClause filterClause in _filterClauses) { 
                    if (!firstClause) {
                        commandText.Append(" AND "); 
                    } 
                    commandText.Append("(" + filterClause.ToString() + ")");
                    firstClause = false; 

                    if (filterClause.Parameter != null) {
                        parameters.Add(filterClause.Parameter);
                    } 
                }
 
                if (_filterClauses.Count > 1) { 
                    commandText.Append(")");
                } 
            }

            if (_orderClauses.Count > 0) {
                // Order by 
                commandText.Append(" ORDER BY ");
 
                // Clauses 
                bool firstClause = true;
                foreach (SqlDataSourceOrderClause orderClause in _orderClauses) { 
                    if (!firstClause) {
                        commandText.Append(", ");
                    }
                    commandText.Append(orderClause.ToString()); 
                    firstClause = false;
                } 
            } 

            string selectCommandText = commandText.ToString(); 
            return new SqlDataSourceQuery(selectCommandText, SqlDataSourceCommandType.Text, parameters.ToArray());
        }

        /// <devdoc> 
        /// Creates an escaped table name.
        /// </devdoc> 
        public string GetTableName() { 
            return SqlDataSourceColumnData.EscapeObjectName(DesignerDataConnection, DesignerDataTable.Name);
        } 

        /// <devdoc>
        /// Attempts to generate an UPDATE command for the current query.
        /// If the UPDATE command cannot be generated, the return value is null. 
        /// </devdoc>
        public SqlDataSourceQuery GetUpdateQuery(string oldValuesFormatString, bool includeOldValues) { 
            if (!CanAutoGenerateQueries()) { 
                return null;
            } 

            StringBuilder commandText = new StringBuilder("UPDATE ");
            commandText.Append(GetTableName());
            commandText.Append(" SET "); 

 
            // Get a list of the fields that would be selected 
            System.Collections.Generic.List<SqlDataSourceColumnData> fields = GetEffectiveColumns();
 
            System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>();

            // Generate update clauses
            bool firstField = true; 
            foreach (SqlDataSourceColumnData columnData in fields) {
                // Never update primary key columns 
                if (columnData.Column.PrimaryKey) { 
                    continue;
                } 

                if (!firstField) {
                    commandText.Append(", ");
                } 

                commandText.Append(columnData.EscapedName); 
                commandText.Append(" = "); 
                commandText.Append(columnData.ParameterPlaceholder);
 
                firstField = false;

                // Create a Web Parameter for this column
                parameters.Add(new Parameter(columnData.WebParameterName, SqlDataSourceDesigner.ConvertDbTypeToTypeCode(columnData.Column.DataType))); 
            }
 
 
            // Abort if there are no updateable fields in the query
            if (firstField) { 
                return null;
            }

            // For UPDATE commands if we generate pessimistic queries, we do not create 
            // parameters for the old values.
            SqlDataSourceQuery whereClause = GetWhereClause(oldValuesFormatString, includeOldValues); 
            // If there was a problem generating the WHERE clause, abort 
            if (whereClause == null) {
                return null; 
            }
            commandText.Append(whereClause.Command);

            // Append the WHERE clause parameters to the UPDATE parameters 
            foreach (Parameter p in whereClause.Parameters) {
                parameters.Add(p); 
            } 
            return new SqlDataSourceQuery(commandText.ToString(), SqlDataSourceCommandType.Text, parameters);
        } 

        /// <devdoc>
        /// Attempts to generate a WHERE clause for the current query.
        /// For this to succeed, all columns in the primary key of the table 
        /// must be in the query, either through the catch-all asterisk
        /// field, or individual selected fields. 
        /// If the WHERE clause cannot be generated, the return value is null. 
        /// </devdoc>
        private SqlDataSourceQuery GetWhereClause(string oldValuesFormatString, bool includeOldValues) { 
            // Get a list of the fields that would be selected
            System.Collections.Generic.List<SqlDataSourceColumnData> fields = GetEffectiveColumns();

            System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>(); 

            // Abort if there are no fields in the query 
            if (fields.Count == 0) { 
                return null;
            } 


            // Build the WHERE clause out of the primary keys and possible old values
            StringBuilder commandText = new StringBuilder(); 
            commandText.Append(" WHERE ");
            int columnsInQuery = 0; 
 
            // First do primary key fields
            foreach (SqlDataSourceColumnData columnData in fields) { 
                if (columnData.Column.PrimaryKey) {
                    if (columnsInQuery > 0) {
                        commandText.Append(" AND ");
                    } 
                    columnsInQuery++;
                    commandText.Append(columnData.EscapedName); 
                    commandText.Append(" = "); 
                    commandText.Append(columnData.GetOldValueParameterPlaceHolder(oldValuesFormatString));
 
                    // Create a Web Parameter for this column
                    parameters.Add(new Parameter(columnData.GetOldValueWebParameterName(oldValuesFormatString), SqlDataSourceDesigner.ConvertDbTypeToTypeCode(columnData.Column.DataType)));
                }
            } 

            if (columnsInQuery == 0) { 
                // If there are no columns yet in the query that means there 
                // are no primary keys selected, and we do not allow that for
                // our auto-generated queries. 
                return null;
            }

            // Then do non-primary key fields 
            if (includeOldValues) {
                foreach (SqlDataSourceColumnData columnData in fields) { 
                    if (!columnData.Column.PrimaryKey) { 
                        commandText.Append(" AND ");
                        columnsInQuery++; 
                        commandText.Append(columnData.EscapedName);
                        commandText.Append(" = ");
                        commandText.Append(columnData.GetOldValueParameterPlaceHolder(oldValuesFormatString));
 
                        // Create a Web Parameter for this column
                        parameters.Add(new Parameter(columnData.GetOldValueWebParameterName(oldValuesFormatString), SqlDataSourceDesigner.ConvertDbTypeToTypeCode(columnData.Column.DataType))); 
                    } 
                }
            } 

            return new SqlDataSourceQuery(commandText.ToString(), SqlDataSourceCommandType.Text, parameters);
        }
 
        /// <devdoc>
        /// Returns true if the entire primary key is selected (i.e. all the 
        /// columns in the primary key are part of the query). 
        /// </devdoc>
        public bool IsPrimaryKeySelected() { 
            // Get a list of the fields that would be selected
            System.Collections.Generic.List<SqlDataSourceColumnData> fields = GetEffectiveColumns();

            // Abort if there are no fields in the query 
            if (fields.Count == 0) {
                return false; 
            } 

            // Count how many columns are in the primary key to make sure we got them all 
            int columnsInPrimaryKey = 0;
            foreach (DesignerDataColumn designerDataColumn in _designerDataTable.Columns) {
                if (designerDataColumn.PrimaryKey) {
                    columnsInPrimaryKey++; 
                }
            } 
 
            // Abort if there is no primary key
            if (columnsInPrimaryKey == 0) { 
                return false;
            }

            // See how many columns that are selected are in the primary key 
            int columnsInQuery = 0;
            foreach (SqlDataSourceColumnData columnData in fields) { 
                if (columnData.Column.PrimaryKey) { 
                    columnsInQuery++;
                } 
            }

            // Check if all the primary key columns were in the query
            return (columnsInPrimaryKey == columnsInQuery); 
        }
    } 
 
    /// <devdoc>
    /// Represents an individual filter clause of a query against a table for a SqlDataSource. 
    /// E.g. field1 >= @field1
    /// </devdoc>
    internal sealed class SqlDataSourceFilterClause {
 
        private DesignerDataColumn _designerDataColumn;
        private DesignerDataTableBase _designerDataTable; 
        private DesignerDataConnection _designerDataConnection; 
        private bool _isBinary;
        private string _operatorFormat; 
        private string _value;
        private Parameter _parameter;

        /// <devdoc> 
        /// Creates a new SqlDataSourceFilterClause with the specified constraints.
        /// </devdoc> 
        public SqlDataSourceFilterClause(DesignerDataConnection designerDataConnection, DesignerDataTableBase designerDataTable, DesignerDataColumn designerDataColumn, string operatorFormat, bool isBinary, string value, Parameter parameter) { 
            Debug.Assert(designerDataConnection != null);
            Debug.Assert(designerDataTable != null); 
            Debug.Assert(designerDataColumn != null);
            Debug.Assert(operatorFormat != null && operatorFormat.Length > 0);
            //Debug.Assert(isBinary ^ (value != null && value.Length == 0));
 
            _designerDataConnection = designerDataConnection;
            _designerDataTable = designerDataTable; 
            _designerDataColumn = designerDataColumn; 
            _isBinary = isBinary;
            _operatorFormat = operatorFormat; 
            _value = value;
            _parameter = parameter;
        }
 
        public DesignerDataColumn DesignerDataColumn {
            get { 
                return _designerDataColumn; 
            }
        } 

        public bool IsBinary {
            get {
                return _isBinary; 
            }
        } 
 
        public string OperatorFormat {
            get { 
                return _operatorFormat;
            }
        }
 
        public Parameter Parameter {
            get { 
                return _parameter; 
            }
        } 

        public string Value {
            get {
                return _value; 
            }
        } 
 
        public override string ToString() {
            // Note: Can't show parameter expression here because this is used 
            // to generate actual commands. We'd need another method for that.
            SqlDataSourceColumnData columnData = new SqlDataSourceColumnData(_designerDataConnection, _designerDataColumn);
            if (_isBinary) {
                return String.Format(CultureInfo.InvariantCulture, _operatorFormat, columnData.EscapedName, _value); 
            }
            else { 
                return String.Format(CultureInfo.InvariantCulture, _operatorFormat, columnData.EscapedName); 
            }
        } 
    }

    /// <devdoc>
    /// Represents an individual order clause of a query against a table for a SqlDataSource. 
    /// E.g. ORDER BY field1 DESC
    /// SQL Books Online reference: 
    /// mk:@MSITStore:C:\Program%20Files\Microsoft%20SQL%20Server%20Books%20Online\1033\tsqlref.chm::/ts_sa-ses_9sfo.htm#_order_by_clause 
    /// The ORDER BY clause can include items not appearing in the select list. However, if SELECT DISTINCT is specified,
    /// or if the SELECT statement contains a UNION operator, the sort columns must appear in the select list. 
    /// </devdoc>
    internal sealed class SqlDataSourceOrderClause {

        private DesignerDataColumn _designerDataColumn; 
        private DesignerDataTableBase _designerDataTable;
        private DesignerDataConnection _designerDataConnection; 
        private bool _isDescending; 

        /// <devdoc> 
        /// Creates a new SqlDataSourceOrderClause with the specified constraints.
        /// </devdoc>
        public SqlDataSourceOrderClause(DesignerDataConnection designerDataConnection, DesignerDataTableBase designerDataTable, DesignerDataColumn designerDataColumn, bool isDescending) {
            Debug.Assert(designerDataConnection != null); 
            Debug.Assert(designerDataTable != null);
            Debug.Assert(designerDataColumn != null); 
 
            _designerDataConnection = designerDataConnection;
            _designerDataTable = designerDataTable; 
            _designerDataColumn = designerDataColumn;
            _isDescending = isDescending;
        }
 
        public DesignerDataColumn DesignerDataColumn {
            get { 
                return _designerDataColumn; 
            }
        } 

        public bool IsDescending {
            get {
                return _isDescending; 
            }
        } 
 
        public override string ToString() {
            SqlDataSourceColumnData columnData = new SqlDataSourceColumnData(_designerDataConnection, _designerDataColumn); 
            if (_isDescending) {
                return columnData.EscapedName + " DESC";
            }
            else { 
                return columnData.EscapedName;
            } 
        } 
    }
 
    internal sealed class SqlDataSourceColumnData {

        private DesignerDataConnection _connection;
        private DesignerDataColumn _column; 
        private StringCollection _usedNames;
 
        private string _cachedAliasedName; 
        private string _cachedEscapedName;
        private string _cachedParameterPlaceholder; 
        private string _cachedWebParameterName;


        public SqlDataSourceColumnData(DesignerDataConnection connection, DesignerDataColumn column) 
            : this(connection, column, null) {
        } 
 
        public SqlDataSourceColumnData(DesignerDataConnection connection, DesignerDataColumn column, StringCollection usedNames) {
            _connection = connection; 
            _column = column;
            _usedNames = usedNames;
        }
 
        /// <devdoc>
        /// Gets the aliased name of this column. This is done by stripping 
        /// out invalid characters. 
        /// </devdoc>
        public string AliasedName { 
            get {
                if (_cachedAliasedName == null) {
                    _cachedAliasedName = CreateAliasedName();
                } 
                return _cachedAliasedName;
            } 
        } 

        /// <devdoc> 
        /// Gets the column that this object represents.
        /// </devdoc>
        public DesignerDataColumn Column {
            get { 
                return _column;
            } 
        } 

        /// <devdoc> 
        /// Gets how this column should be represented in a SELECT statement.
        /// This can be in the form [table].[column foo] AS column_foo
        /// </devdoc>
        public string SelectName { 
            get {
                if (_column == null) { 
                    return EscapedName; 
                }
                else { 
                    string alias = AliasedName;
                    if (alias != _column.Name) {
                        // Only use the alias if it is different from the original column name
                        return EscapedName + " AS " + AliasedName; 
                    }
                    else { 
                        return EscapedName; 
                    }
                } 
            }
        }

        /// <devdoc> 
        /// Gets the full name of the column, qualified by the table name.
        /// </devdoc> 
        public string EscapedName { 
            get {
                if (_cachedEscapedName == null) { 
                    _cachedEscapedName = CreateEscapedName();
                }
                return _cachedEscapedName;
            } 
        }
 
        /// <devdoc> 
        /// Creates a parameter placeholder for use with this column.
        /// </devdoc> 
        public string ParameterPlaceholder {
            get {
                if (_cachedParameterPlaceholder == null) {
                    _cachedParameterPlaceholder = CreateParameterPlaceholder(null); 
                }
                return _cachedParameterPlaceholder; 
            } 
        }
 
        /// <devdoc>
        /// Creates a parameter name for use with this column.
        /// </devdoc>
        public string WebParameterName { 
            get {
                if (_cachedWebParameterName == null) { 
                    _cachedWebParameterName = CreateWebParameterName(null); 
                }
                return _cachedWebParameterName; 
            }
        }

        /// <devdoc> 
        /// Escapes an object identifier using the provider's escaping
        /// mechanism and format. 
        /// </devdoc> 
        internal static string EscapeObjectName(DesignerDataConnection connection, string objectName) {
            string quotePrefix = "["; 
            string quoteSuffix = "]";

            try {
                DbProviderFactory factory = SqlDataSourceDesigner.GetDbProviderFactory(connection.ProviderName); 
                DbCommandBuilder builder = factory.CreateCommandBuilder();
                // It would be nice if we could find the prefix/suffix from metadata, but the info isn't there. 
                // This is required now because otherwise QuoteIdentifier() requires an open connection. 
                // Until we can get the info from the schema without an open connection, switch on the factory type.
                if (factory == System.Data.OracleClient.OracleClientFactory.Instance) { 
                    quotePrefix = quoteSuffix = "\"";
                }
                builder.QuotePrefix = quotePrefix;
                builder.QuoteSuffix = quoteSuffix; 
                return builder.QuoteIdentifier(objectName);
            } 
            catch (Exception ex) { 
                Debug.WriteLine(ex);
                // NOTE: This is a temporary workaround for SQL connections and will be 
                // removed when the above code is changed to use metadata services.
                return quotePrefix + objectName + quoteSuffix;
            }
        } 

        /// <devdoc> 
        /// Creates a parameter placeholder for the column. 
        /// "FooColumn" --> "FooColumn" (all valid chars)
        /// "Foo   Column" --> "Foo_Column" (whitespace gets collapsed to single underscore) 
        /// "Foo!Column" --> "column1" (any non-letter/non-digit/non-whitespace/non-underscore
        ///                             characters invalidate the entire name)
        /// </devdoc>
        private string CreateAliasedName() { 
            string name = _column.Name;
            Debug.Assert(!String.IsNullOrEmpty(name), "Expected a column name to exist already"); 
            StringBuilder sb = new StringBuilder(); 
            bool hasInvalidChars = false;
            bool lastCharWasSpace = false; 
            foreach (char c in name) {
                if (Char.IsWhiteSpace(c) || (c == '_')) {
                    if (!lastCharWasSpace) {
                        // Never have more than one underscore replacing a space in a row 
                        sb.Append('_');
                        lastCharWasSpace = true; 
                    } 
                }
                else { 
                    if (Char.IsLetterOrDigit(c)) {
                        sb.Append(c);
                        lastCharWasSpace = false;
                    } 
                    else {
                        hasInvalidChars = true; 
                        break; 
                    }
                } 
            }

            // If the column name still starts with invalid characters, start from scratch
            if (sb.Length == 0 || !Char.IsLetter(sb[0])) { 
                hasInvalidChars = true;
            } 
 
            int count;
            string initialAliasedName; 
            string uniqueAliasedName;
            if (hasInvalidChars) {
                // If there are no valid characters at all, create a dummy name
                initialAliasedName = "column"; 
                count = 1;
                uniqueAliasedName = initialAliasedName + '1'; 
            } 
            else {
                // There are valid characters in the name, but we still might 
                // have to validate uniqueness
                count = 2;
                initialAliasedName = sb.ToString();
                uniqueAliasedName = initialAliasedName; 
            }
 
            // Ensure uniqueness by comparing against existing name list, if available 
            if (_usedNames != null) {
                if (_usedNames.Contains(uniqueAliasedName)) { 
                    //
                    do {
                        uniqueAliasedName = initialAliasedName + count.ToString(CultureInfo.InvariantCulture);
                        count++; 
                    }
                    while (_usedNames.Contains(uniqueAliasedName)); 
                } 

                // Add new name to list 
                _usedNames.Add(uniqueAliasedName);
            }
            return uniqueAliasedName;
        } 

        /// <devdoc> 
        /// Creates an escaped and qualified column name. If the column is 
        /// null, it is assumed to be the asterisk column.
        /// It generates names like [CustomerID] and * 
        /// </devdoc>
        private string CreateEscapedName() {
            StringBuilder sb = new StringBuilder();
            if (_column == null) { 
                sb.Append("*");
            } 
            else { 
                sb.Append(EscapeObjectName(_connection, _column.Name));
            } 
            return sb.ToString();
        }

        /// <devdoc> 
        /// Creates a parameter placeholder for a given column.
        /// </devdoc> 
        private string CreateParameterPlaceholder(string oldValueFormatString) { 
            DbProviderFactory factory = SqlDataSourceDesigner.GetDbProviderFactory(_connection.ProviderName);
            string paramPlaceHolderPrefix = SqlDataSourceDesigner.GetParameterPlaceholderPrefix(factory); 
            string parameterName = paramPlaceHolderPrefix;
            if (SqlDataSourceDesigner.SupportsNamedParameters(factory)) {
                if (oldValueFormatString == null) {
                    parameterName += AliasedName; 
                }
                else { 
                    parameterName += String.Format(CultureInfo.InvariantCulture, oldValueFormatString, AliasedName); 
                }
            } 
            return parameterName;
        }

        /// <devdoc> 
        /// Creates a parameter name for a given column.
        /// </devdoc> 
        private string CreateWebParameterName(string oldValueFormatString) { 
            if (oldValueFormatString == null) {
                return AliasedName; 
            }
            else {
                return String.Format(CultureInfo.InvariantCulture, oldValueFormatString, AliasedName);
            } 
        }
 
        public string GetOldValueParameterPlaceHolder(string oldValueFormatString) { 
            Debug.Assert(oldValueFormatString != null);
            return CreateParameterPlaceholder(oldValueFormatString); 
        }

        public string GetOldValueWebParameterName(string oldValueFormatString) {
            Debug.Assert(oldValueFormatString != null); 
            return CreateWebParameterName(oldValueFormatString);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
