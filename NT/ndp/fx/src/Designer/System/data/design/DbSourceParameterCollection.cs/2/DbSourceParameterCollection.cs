 


namespace System.Data.Design {
 
    using System;
    using System.Collections; 
    using System.ComponentModel; 
    using System.Data;
    using System.Data.Common; 
    using System.Diagnostics;
    using System.Globalization;

 

    internal class DbSourceParameterCollection : DataSourceCollectionBase, IDataParameterCollection, ICloneable { 
 
        internal DbSourceParameterCollection(DataSourceComponent collectionHost) : base(collectionHost){}
 
        /// <summary>
        /// </summary>
        /// <value></value>
        protected override INameService NameService { 
            get {
                return SimpleNameService.DefaultInstance; 
            } 
        }
 
        // explicit IDataParameterCollection implementation
        object IDataParameterCollection.this[string parameterName] {
            get {
                int index = RangeCheck( parameterName ); 
                return this.List[index];
            } 
            set { 
                int index = RangeCheck( parameterName );
                this.List[index] = value; 
            }
        }

        public DesignParameter this[int index] { 
            get {
                return (DesignParameter) this.List[index]; 
            } 
        }
 
        public bool Contains( string value ) {
            return (IndexOf(value) != -1);
        }
 
        public int IndexOf(string parameterName) {
 
            int count = this.InnerList.Count; 

            // karolz 2/25/2002: Some backends allow for server settings that treat the parameter names in case-insensitive 
            // way. However it is probably good enough to assume here that all the names are case-sensitive.

            for (int i = 0; i < count; ++i) {
                if( StringUtil.EqualValue( parameterName, ((IDbDataParameter) this.InnerList[i]).ParameterName)) { 
                    return i;
                } 
            } 

            return -1; 
        }

        private int RangeCheck(string parameterName) {
            int index = IndexOf( parameterName ); 

            if( index < 0 ) { 
                throw new InternalException(string.Format(System.Globalization.CultureInfo.CurrentCulture, VSDExceptions.DataSource.PARAMETER_NOT_FOUND_MSG, parameterName), 
                                             VSDExceptions.DataSource.PARAMETER_NOT_FOUND_CODE );
            } 

            return index;
        }
 
        public void RemoveAt( string parameterName ) {
            int index = RangeCheck( parameterName ); 
            this.List.RemoveAt( index ); 
        }
 
        protected override Type ItemType {
            get {
                return typeof(DesignParameter);
            } 
        }
 
        public object Clone() { 
            DbSourceParameterCollection clone = new DbSourceParameterCollection(null);
 
            foreach( DesignParameter param in this ) {
                DesignParameter newParam = (DesignParameter) param.Clone();
                ((IList) clone).Add( newParam );
            } 

            return clone; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 


namespace System.Data.Design {
 
    using System;
    using System.Collections; 
    using System.ComponentModel; 
    using System.Data;
    using System.Data.Common; 
    using System.Diagnostics;
    using System.Globalization;

 

    internal class DbSourceParameterCollection : DataSourceCollectionBase, IDataParameterCollection, ICloneable { 
 
        internal DbSourceParameterCollection(DataSourceComponent collectionHost) : base(collectionHost){}
 
        /// <summary>
        /// </summary>
        /// <value></value>
        protected override INameService NameService { 
            get {
                return SimpleNameService.DefaultInstance; 
            } 
        }
 
        // explicit IDataParameterCollection implementation
        object IDataParameterCollection.this[string parameterName] {
            get {
                int index = RangeCheck( parameterName ); 
                return this.List[index];
            } 
            set { 
                int index = RangeCheck( parameterName );
                this.List[index] = value; 
            }
        }

        public DesignParameter this[int index] { 
            get {
                return (DesignParameter) this.List[index]; 
            } 
        }
 
        public bool Contains( string value ) {
            return (IndexOf(value) != -1);
        }
 
        public int IndexOf(string parameterName) {
 
            int count = this.InnerList.Count; 

            // karolz 2/25/2002: Some backends allow for server settings that treat the parameter names in case-insensitive 
            // way. However it is probably good enough to assume here that all the names are case-sensitive.

            for (int i = 0; i < count; ++i) {
                if( StringUtil.EqualValue( parameterName, ((IDbDataParameter) this.InnerList[i]).ParameterName)) { 
                    return i;
                } 
            } 

            return -1; 
        }

        private int RangeCheck(string parameterName) {
            int index = IndexOf( parameterName ); 

            if( index < 0 ) { 
                throw new InternalException(string.Format(System.Globalization.CultureInfo.CurrentCulture, VSDExceptions.DataSource.PARAMETER_NOT_FOUND_MSG, parameterName), 
                                             VSDExceptions.DataSource.PARAMETER_NOT_FOUND_CODE );
            } 

            return index;
        }
 
        public void RemoveAt( string parameterName ) {
            int index = RangeCheck( parameterName ); 
            this.List.RemoveAt( index ); 
        }
 
        protected override Type ItemType {
            get {
                return typeof(DesignParameter);
            } 
        }
 
        public object Clone() { 
            DbSourceParameterCollection clone = new DbSourceParameterCollection(null);
 
            foreach( DesignParameter param in this ) {
                DesignParameter newParam = (DesignParameter) param.Clone();
                ((IList) clone).Add( newParam );
            } 

            return clone; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
