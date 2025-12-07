 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
using System; 
using System.Data;
using System.Data.Common; 
using System.Design;
using System.Diagnostics;

namespace System.Data.Design { 
    using System;
    using System.ComponentModel; 
 
    [
        DataSourceXmlClass (SchemaName.DbCommand), 
        DefaultProperty ("CommandText")
    ]
    internal class DbSourceCommand : DataSourceComponent, ICloneable, INamedObject {
        private DbSource _parent; 
        private CommandOperation commandOperation;
        private string commandText; 
        private CommandType commandType; 
        private DbSourceParameterCollection parameterCollection;
        private bool modifiedByUser = false; 
        private string name;

        public DbSourceCommand () {
            commandText = String.Empty; 
            commandType = CommandType.Text;
            parameterCollection = new DbSourceParameterCollection (this); 
        } 

        public DbSourceCommand (DbSource parent, CommandOperation operation) : this() { 
            SetParent (parent);
            CommandOperation = operation;
        }
 

        internal CommandOperation CommandOperation { 
            get { 
                return commandOperation;
            } 
            set {
                commandOperation = value;
            }
        } 

        [ 
            DataSourceXmlElement (), 
            Browsable (false),
        ] 
        public string CommandText {
            get {
                return this.commandText;
            } 
            set {
                this.commandText = value; 
            } 
        }
 

        [
            DataSourceXmlAttribute (ItemType = typeof(CommandType)),
            DefaultValue (CommandType.Text) 
        ]
        public CommandType CommandType { 
            get { 
                return this.commandType;
            } 
            set {
                if( value == CommandType.TableDirect ) {

                    if( this._parent != null && this._parent.Connection != null ) { 

                        string provider = this._parent.Connection.Provider; 
 
                        if( !StringUtil.EqualValue(provider, "System.Data.OleDb") ) {
                            throw new Exception( SR.GetString(SR.DD_E_TableDirectValidForOleDbOnly) ); 
                        }
                    }
                }
 

                this.commandType = value; 
            } 
        }
 
        [
            Browsable (false),
            DataSourceXmlAttribute (ItemType = typeof(bool))
        ] 
        public bool ModifiedByUser {
            get { 
                return this.modifiedByUser; 
            }
            set { 
                this.modifiedByUser = value;
            }
        }
 
        /// <summary>
        /// Name is primarily used to display it in proerty grid 
        /// Name is setting by its parent 
        /// </summary>
        /// <value></value> 
        [Browsable(false)]
        public string Name {
            get {
                return name; 
            }
            set { 
                name = value; 
            }
        } 

        [
            DataSourceXmlSubItem (ItemType = typeof(DesignParameter))
        ] 
        public DbSourceParameterCollection Parameters {
            get { 
                return this.parameterCollection; 
            }
        } 

        private bool ShouldSerializeParameters () {
            return (null != this.parameterCollection && (0 < this.parameterCollection.Count));
        } 

        /// <summary> 
        /// for IObjectWithParent 
        /// </summary>
        [Browsable (false)] 
        public override object Parent {
            get {
                return _parent;
            } 
        }
        public object Clone() { 
            DbSourceCommand cmd = new DbSourceCommand (); 

            cmd.commandText = this.commandText; 
            cmd.commandType = this.commandType;
            cmd.commandOperation = this.commandOperation;
            cmd.parameterCollection = (DbSourceParameterCollection)this.parameterCollection.Clone ();
            cmd.parameterCollection.CollectionHost = cmd; 

            // ModifiedByUser flag is reset to default (false). 
            return cmd; 
        }
 

        internal void SetParent (DbSource parent) {
            _parent = parent;
        } 

        public override string ToString() { 
            if (StringUtil.NotEmptyAfterTrim (((INamedObject)this).Name)){ 
                return ((INamedObject)this).Name;
            } 
            return base.ToString();
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
using System; 
using System.Data;
using System.Data.Common; 
using System.Design;
using System.Diagnostics;

namespace System.Data.Design { 
    using System;
    using System.ComponentModel; 
 
    [
        DataSourceXmlClass (SchemaName.DbCommand), 
        DefaultProperty ("CommandText")
    ]
    internal class DbSourceCommand : DataSourceComponent, ICloneable, INamedObject {
        private DbSource _parent; 
        private CommandOperation commandOperation;
        private string commandText; 
        private CommandType commandType; 
        private DbSourceParameterCollection parameterCollection;
        private bool modifiedByUser = false; 
        private string name;

        public DbSourceCommand () {
            commandText = String.Empty; 
            commandType = CommandType.Text;
            parameterCollection = new DbSourceParameterCollection (this); 
        } 

        public DbSourceCommand (DbSource parent, CommandOperation operation) : this() { 
            SetParent (parent);
            CommandOperation = operation;
        }
 

        internal CommandOperation CommandOperation { 
            get { 
                return commandOperation;
            } 
            set {
                commandOperation = value;
            }
        } 

        [ 
            DataSourceXmlElement (), 
            Browsable (false),
        ] 
        public string CommandText {
            get {
                return this.commandText;
            } 
            set {
                this.commandText = value; 
            } 
        }
 

        [
            DataSourceXmlAttribute (ItemType = typeof(CommandType)),
            DefaultValue (CommandType.Text) 
        ]
        public CommandType CommandType { 
            get { 
                return this.commandType;
            } 
            set {
                if( value == CommandType.TableDirect ) {

                    if( this._parent != null && this._parent.Connection != null ) { 

                        string provider = this._parent.Connection.Provider; 
 
                        if( !StringUtil.EqualValue(provider, "System.Data.OleDb") ) {
                            throw new Exception( SR.GetString(SR.DD_E_TableDirectValidForOleDbOnly) ); 
                        }
                    }
                }
 

                this.commandType = value; 
            } 
        }
 
        [
            Browsable (false),
            DataSourceXmlAttribute (ItemType = typeof(bool))
        ] 
        public bool ModifiedByUser {
            get { 
                return this.modifiedByUser; 
            }
            set { 
                this.modifiedByUser = value;
            }
        }
 
        /// <summary>
        /// Name is primarily used to display it in proerty grid 
        /// Name is setting by its parent 
        /// </summary>
        /// <value></value> 
        [Browsable(false)]
        public string Name {
            get {
                return name; 
            }
            set { 
                name = value; 
            }
        } 

        [
            DataSourceXmlSubItem (ItemType = typeof(DesignParameter))
        ] 
        public DbSourceParameterCollection Parameters {
            get { 
                return this.parameterCollection; 
            }
        } 

        private bool ShouldSerializeParameters () {
            return (null != this.parameterCollection && (0 < this.parameterCollection.Count));
        } 

        /// <summary> 
        /// for IObjectWithParent 
        /// </summary>
        [Browsable (false)] 
        public override object Parent {
            get {
                return _parent;
            } 
        }
        public object Clone() { 
            DbSourceCommand cmd = new DbSourceCommand (); 

            cmd.commandText = this.commandText; 
            cmd.commandType = this.commandType;
            cmd.commandOperation = this.commandOperation;
            cmd.parameterCollection = (DbSourceParameterCollection)this.parameterCollection.Clone ();
            cmd.parameterCollection.CollectionHost = cmd; 

            // ModifiedByUser flag is reset to default (false). 
            return cmd; 
        }
 

        internal void SetParent (DbSource parent) {
            _parent = parent;
        } 

        public override string ToString() { 
            if (StringUtil.NotEmptyAfterTrim (((INamedObject)this).Name)){ 
                return ((INamedObject)this).Name;
            } 
            return base.ToString();
        }

    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
