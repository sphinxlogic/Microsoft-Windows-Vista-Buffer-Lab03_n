//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataColumn.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Data; 

    /// <devdoc>
    /// Represents a single column of a table or view in a data connection. A
    /// collection of this type is returned from the DesignerDataTable.Columns 
    /// and DesignerDataView.Columns properties.
    /// </devdoc> 
    public sealed class DesignerDataColumn { 

        private DbType _dataType; 
        private object _defaultValue;
        private bool _identity;
        private int _length;
        private string _name; 
        private bool _nullable;
        private int _precision; 
        private bool _primaryKey; 
        private int _scale;
 
        /// <devdoc>
        /// </devdoc>
        public DesignerDataColumn(string name, DbType dataType) :
            this(name, dataType, null, false, false, false, -1, -1, -1) { 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public DesignerDataColumn(string name, DbType dataType, object defaultValue) : 
            this(name, dataType, defaultValue, false, false, false, -1, -1, -1) {
        }

        /// <devdoc> 
        /// </devdoc>
        public DesignerDataColumn(string name, DbType dataType, object defaultValue, bool identity, bool nullable, bool primaryKey, int precision, int scale, int length) { 
            _dataType = dataType; 
            _defaultValue = defaultValue;
            _identity = identity; 
            _length = length;
            _name = name;
            _nullable = nullable;
            _precision = precision; 
            _primaryKey = primaryKey;
            _scale = scale; 
        } 

        /// <devdoc> 
        /// The type of the column.
        /// </devdoc>
        public DbType DataType {
            get { 
                return _dataType;
            } 
        } 

        /// <devdoc> 
        /// The default value of this column.
        /// </devdoc>
        public object DefaultValue {
            get { 
                return _defaultValue;
            } 
        } 

        /// <devdoc> 
        /// Whether this column is an identity column.
        /// </devdoc>
        public bool Identity {
            get { 
                return _identity;
            } 
        } 

        /// <devdoc> 
        /// Returns the length of the column.
        /// </devdoc>
        public int Length {
            get { 
                return _length;
            } 
        } 

        /// <devdoc> 
        /// The name of the column.
        /// </devdoc>
        public string Name {
            get { 
                return _name;
            } 
        } 

        /// <devdoc> 
        /// Whether this column can contain nulls.
        /// </devdoc>
        public bool Nullable {
            get { 
                return _nullable;
            } 
        } 

        /// <devdoc> 
        /// Returns the precision of the column.
        /// </devdoc>
        public int Precision {
            get { 
                return _precision;
            } 
        } 

        /// <devdoc> 
        /// Whether this column is part of the primary key of the table it is contained in.
        /// </devdoc>
        public bool PrimaryKey {
            get { 
                return _primaryKey;
            } 
        } 

        /// <devdoc> 
        ///  Returns the scale of the column.
        /// </devdoc>
        public int Scale {
            get { 
                return _scale;
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataColumn.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Data { 
 
    using System;
    using System.Data; 

    /// <devdoc>
    /// Represents a single column of a table or view in a data connection. A
    /// collection of this type is returned from the DesignerDataTable.Columns 
    /// and DesignerDataView.Columns properties.
    /// </devdoc> 
    public sealed class DesignerDataColumn { 

        private DbType _dataType; 
        private object _defaultValue;
        private bool _identity;
        private int _length;
        private string _name; 
        private bool _nullable;
        private int _precision; 
        private bool _primaryKey; 
        private int _scale;
 
        /// <devdoc>
        /// </devdoc>
        public DesignerDataColumn(string name, DbType dataType) :
            this(name, dataType, null, false, false, false, -1, -1, -1) { 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public DesignerDataColumn(string name, DbType dataType, object defaultValue) : 
            this(name, dataType, defaultValue, false, false, false, -1, -1, -1) {
        }

        /// <devdoc> 
        /// </devdoc>
        public DesignerDataColumn(string name, DbType dataType, object defaultValue, bool identity, bool nullable, bool primaryKey, int precision, int scale, int length) { 
            _dataType = dataType; 
            _defaultValue = defaultValue;
            _identity = identity; 
            _length = length;
            _name = name;
            _nullable = nullable;
            _precision = precision; 
            _primaryKey = primaryKey;
            _scale = scale; 
        } 

        /// <devdoc> 
        /// The type of the column.
        /// </devdoc>
        public DbType DataType {
            get { 
                return _dataType;
            } 
        } 

        /// <devdoc> 
        /// The default value of this column.
        /// </devdoc>
        public object DefaultValue {
            get { 
                return _defaultValue;
            } 
        } 

        /// <devdoc> 
        /// Whether this column is an identity column.
        /// </devdoc>
        public bool Identity {
            get { 
                return _identity;
            } 
        } 

        /// <devdoc> 
        /// Returns the length of the column.
        /// </devdoc>
        public int Length {
            get { 
                return _length;
            } 
        } 

        /// <devdoc> 
        /// The name of the column.
        /// </devdoc>
        public string Name {
            get { 
                return _name;
            } 
        } 

        /// <devdoc> 
        /// Whether this column can contain nulls.
        /// </devdoc>
        public bool Nullable {
            get { 
                return _nullable;
            } 
        } 

        /// <devdoc> 
        /// Returns the precision of the column.
        /// </devdoc>
        public int Precision {
            get { 
                return _precision;
            } 
        } 

        /// <devdoc> 
        /// Whether this column is part of the primary key of the table it is contained in.
        /// </devdoc>
        public bool PrimaryKey {
            get { 
                return _primaryKey;
            } 
        } 

        /// <devdoc> 
        ///  Returns the scale of the column.
        /// </devdoc>
        public int Scale {
            get { 
                return _scale;
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
