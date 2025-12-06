 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
using System;
using System.Collections; 
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization; 
using System.IO;
using System.Diagnostics; 
using System.Globalization; 
using System.Text.RegularExpressions;
 


namespace System.Data.Design {
 

    internal interface IDesignConnectionCollection: INamedObjectCollection { 
        IDesignConnection Get( string name ); 
        void Set( IDesignConnection connection );
        void Remove( string name ); 
        void Clear();
    }

 

    internal class DesignConnectionCollection: DataSourceCollectionBase, IDesignConnectionCollection  { 
 
        internal DesignConnectionCollection(DataSourceComponent collectionHost) : base(collectionHost) {
        } 

        protected override Type ItemType {
            get {
                return typeof(IDesignConnection); 
            }
        } 
        protected override INameService NameService { 
            get {
                return SimpleNameService.DefaultInstance; 
            }
        }

        // 
        // IDesignConnectionCollection implementation
        // 
        public IDesignConnection Get( string name ) { 
            return (IDesignConnection) NamedObjectUtil.Find( this, name );
        } 


        protected override void OnSet( int index, object oldValue, object newValue ) {
            base.OnSet( index, oldValue, newValue ); 

            ValidateType( newValue ); 
 
            IDesignConnection oldConn = (IDesignConnection) oldValue;
            IDesignConnection newConn = (IDesignConnection) newValue; 

            if( !StringUtil.EqualValue( oldConn.Name, newConn.Name)) {
                ValidateUniqueName(newConn, newConn.Name);
            } 
        }
 
        public void Set( IDesignConnection connection ) { 
            INamedObject oldConnection = NamedObjectUtil.Find( this, connection.Name );
            if( oldConnection != null ) { 
                this.List.Remove( oldConnection );
            }

            this.List.Add( connection ); 
        }
 
 
        public bool Contains( IDesignConnection connection ) {
            return List.Contains( connection ); 
        }

        public int Add( IDesignConnection connection ) {
            return List.Add( connection ); 
        }
 
        public void Remove( IDesignConnection connection ) { 
            List.Remove( connection );
        } 
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
 
using System;
using System.Collections; 
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization; 
using System.IO;
using System.Diagnostics; 
using System.Globalization; 
using System.Text.RegularExpressions;
 


namespace System.Data.Design {
 

    internal interface IDesignConnectionCollection: INamedObjectCollection { 
        IDesignConnection Get( string name ); 
        void Set( IDesignConnection connection );
        void Remove( string name ); 
        void Clear();
    }

 

    internal class DesignConnectionCollection: DataSourceCollectionBase, IDesignConnectionCollection  { 
 
        internal DesignConnectionCollection(DataSourceComponent collectionHost) : base(collectionHost) {
        } 

        protected override Type ItemType {
            get {
                return typeof(IDesignConnection); 
            }
        } 
        protected override INameService NameService { 
            get {
                return SimpleNameService.DefaultInstance; 
            }
        }

        // 
        // IDesignConnectionCollection implementation
        // 
        public IDesignConnection Get( string name ) { 
            return (IDesignConnection) NamedObjectUtil.Find( this, name );
        } 


        protected override void OnSet( int index, object oldValue, object newValue ) {
            base.OnSet( index, oldValue, newValue ); 

            ValidateType( newValue ); 
 
            IDesignConnection oldConn = (IDesignConnection) oldValue;
            IDesignConnection newConn = (IDesignConnection) newValue; 

            if( !StringUtil.EqualValue( oldConn.Name, newConn.Name)) {
                ValidateUniqueName(newConn, newConn.Name);
            } 
        }
 
        public void Set( IDesignConnection connection ) { 
            INamedObject oldConnection = NamedObjectUtil.Find( this, connection.Name );
            if( oldConnection != null ) { 
                this.List.Remove( oldConnection );
            }

            this.List.Add( connection ); 
        }
 
 
        public bool Contains( IDesignConnection connection ) {
            return List.Contains( connection ); 
        }

        public int Add( IDesignConnection connection ) {
            return List.Add( connection ); 
        }
 
        public void Remove( IDesignConnection connection ) { 
            List.Remove( connection );
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
