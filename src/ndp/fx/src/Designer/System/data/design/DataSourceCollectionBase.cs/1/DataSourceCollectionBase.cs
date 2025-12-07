 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design{ 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization; 

 
    /// <summary> 
    /// </summary>
    internal abstract class DataSourceCollectionBase : CollectionBase, INamedObjectCollection, IObjectWithParent { 
        private DataSourceComponent collectionHost;


        internal DataSourceCollectionBase(DataSourceComponent collectionHost) { 
            this.collectionHost = collectionHost;
        } 
 
        internal virtual DataSourceComponent CollectionHost {
            get { 
                return this.collectionHost;
            }
            set {
                this.collectionHost = value; 
            }
        } 
 
        protected virtual Type ItemType {
            get { 
                return typeof(IDataSourceNamedObject);
            }
        }
 
        protected abstract INameService NameService {
            get; 
        } 

 
        [Browsable(false)]
        object IObjectWithParent.Parent {
            get {
                return this.collectionHost; 
            }
        } 
 

 
        protected virtual string CreateUniqueName(IDataSourceNamedObject value) {
            string suggestedName = StringUtil.NotEmpty(value.Name) ?
                                   value.Name :
                                   value.PublicTypeName; 
            return NameService.CreateUniqueName(this, suggestedName, 1);
        } 
 
        /// <summary>
        /// This function will check to see namedObject have a name conflict with the names 
        /// used in the collection and fix the name for namedObject if there is a conflict
        /// </summary>
        /// <param name="namedObject"></param>
        internal protected virtual void EnsureUniqueName(IDataSourceNamedObject namedObject) { 
            if (namedObject.Name == null || namedObject.Name.Length == 0 || this.FindObject(namedObject.Name) != null) {
                namedObject.Name = CreateUniqueName(namedObject); 
            } 
        }
 
        internal virtual protected IDataSourceNamedObject FindObject(string name) {
            IEnumerator e = this.InnerList.GetEnumerator();

            while( e.MoveNext() ) { 
                IDataSourceNamedObject existing = (IDataSourceNamedObject) e.Current;
                if (StringUtil.EqualValue(existing.Name, name)) { 
                    return existing; 
                }
            } 
            return null;
        }

 
        public void InsertBefore(object value, object refObject) {
            int index = List.IndexOf(refObject); 
            if (index >= 0) { 
                List.Insert(index, value);
            } 
            else {
                List.Add(value);
            }
        } 

 
 
        protected override void OnValidate( object value ) {
            base.OnValidate( value ); 
            ValidateType( value );
        }

 
        public void Remove(string name) {
            INamedObject obj = NamedObjectUtil.Find( this, name ); 
 
            if( obj != null ) {
                this.List.Remove( obj ); 
            }
        }

 
        /// <summary>
        /// This function only check the name to be valid, typically used by Insert function, which already ensure the name is unique 
        /// </summary> 
        /// <param name="obj"></param>
        internal protected virtual void ValidateName(IDataSourceNamedObject obj) { 
            this.NameService.ValidateName(obj.Name);
        }

        /// <summary> 
        /// This function checks the name to be unique and valid, typically used by rename
        /// </summary> 
        /// <param name="obj"></param> 
        /// <param name="proposedName"></param>
        internal protected virtual void ValidateUniqueName(IDataSourceNamedObject obj, string proposedName) { 
            this.NameService.ValidateUniqueName(this, obj, proposedName);
        }

        protected void ValidateType( object value ) { 
            if (!ItemType.IsInstanceOfType(value)) {
                throw new InternalException(VSDExceptions.DataSource.INVALID_COLLECTIONTYPE_MSG, VSDExceptions.DataSource.INVALID_COLLECTIONTYPE_CODE, true); 
            } 
        }
 
        public INameService GetNameService() {
            return NameService;
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
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization; 

 
    /// <summary> 
    /// </summary>
    internal abstract class DataSourceCollectionBase : CollectionBase, INamedObjectCollection, IObjectWithParent { 
        private DataSourceComponent collectionHost;


        internal DataSourceCollectionBase(DataSourceComponent collectionHost) { 
            this.collectionHost = collectionHost;
        } 
 
        internal virtual DataSourceComponent CollectionHost {
            get { 
                return this.collectionHost;
            }
            set {
                this.collectionHost = value; 
            }
        } 
 
        protected virtual Type ItemType {
            get { 
                return typeof(IDataSourceNamedObject);
            }
        }
 
        protected abstract INameService NameService {
            get; 
        } 

 
        [Browsable(false)]
        object IObjectWithParent.Parent {
            get {
                return this.collectionHost; 
            }
        } 
 

 
        protected virtual string CreateUniqueName(IDataSourceNamedObject value) {
            string suggestedName = StringUtil.NotEmpty(value.Name) ?
                                   value.Name :
                                   value.PublicTypeName; 
            return NameService.CreateUniqueName(this, suggestedName, 1);
        } 
 
        /// <summary>
        /// This function will check to see namedObject have a name conflict with the names 
        /// used in the collection and fix the name for namedObject if there is a conflict
        /// </summary>
        /// <param name="namedObject"></param>
        internal protected virtual void EnsureUniqueName(IDataSourceNamedObject namedObject) { 
            if (namedObject.Name == null || namedObject.Name.Length == 0 || this.FindObject(namedObject.Name) != null) {
                namedObject.Name = CreateUniqueName(namedObject); 
            } 
        }
 
        internal virtual protected IDataSourceNamedObject FindObject(string name) {
            IEnumerator e = this.InnerList.GetEnumerator();

            while( e.MoveNext() ) { 
                IDataSourceNamedObject existing = (IDataSourceNamedObject) e.Current;
                if (StringUtil.EqualValue(existing.Name, name)) { 
                    return existing; 
                }
            } 
            return null;
        }

 
        public void InsertBefore(object value, object refObject) {
            int index = List.IndexOf(refObject); 
            if (index >= 0) { 
                List.Insert(index, value);
            } 
            else {
                List.Add(value);
            }
        } 

 
 
        protected override void OnValidate( object value ) {
            base.OnValidate( value ); 
            ValidateType( value );
        }

 
        public void Remove(string name) {
            INamedObject obj = NamedObjectUtil.Find( this, name ); 
 
            if( obj != null ) {
                this.List.Remove( obj ); 
            }
        }

 
        /// <summary>
        /// This function only check the name to be valid, typically used by Insert function, which already ensure the name is unique 
        /// </summary> 
        /// <param name="obj"></param>
        internal protected virtual void ValidateName(IDataSourceNamedObject obj) { 
            this.NameService.ValidateName(obj.Name);
        }

        /// <summary> 
        /// This function checks the name to be unique and valid, typically used by rename
        /// </summary> 
        /// <param name="obj"></param> 
        /// <param name="proposedName"></param>
        internal protected virtual void ValidateUniqueName(IDataSourceNamedObject obj, string proposedName) { 
            this.NameService.ValidateUniqueName(this, obj, proposedName);
        }

        protected void ValidateType( object value ) { 
            if (!ItemType.IsInstanceOfType(value)) {
                throw new InternalException(VSDExceptions.DataSource.INVALID_COLLECTIONTYPE_MSG, VSDExceptions.DataSource.INVALID_COLLECTIONTYPE_CODE, true); 
            } 
        }
 
        public INameService GetNameService() {
            return NameService;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
