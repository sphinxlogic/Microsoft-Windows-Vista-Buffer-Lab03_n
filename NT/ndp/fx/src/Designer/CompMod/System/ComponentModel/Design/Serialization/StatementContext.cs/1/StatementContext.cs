//------------------------------------------------------------------------------ 
// <copyright file="StatementContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.Collections;
    using System.Collections.Generic;

    /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext"]/*' /> 
    /// <devdoc>
    ///    This object can be placed on the context stack to provide a place for statements 
    ///    to be serialized into.  Normally, statements are serialized into whatever statement 
    ///    collection that is on the context stack.  You can modify this behavior by creating
    ///    a statement context and calling Populate with a collection of objects whose statements 
    ///    you would like stored in the statement table.  As each object is serialized in
    ///    SerializeToExpression it will have its contents placed in the statement table.
    ///    saved in a table within the context.  If you push this object on the stack it is your
    ///    responsibility to integrate the statements added to it into your own collection of statements. 
    /// </devdoc>
    public sealed class StatementContext { 
 
        private ObjectStatementCollection _statements;
 
        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.StatementCollection"]/*' />
        /// <devdoc>
        ///    This is a table of statements that is offered by the statement context.
        /// </devdoc> 
        public ObjectStatementCollection StatementCollection {
            get { 
                if (_statements == null) { 
                    _statements = new ObjectStatementCollection();
                } 

                return _statements;
            }
        } 
    }
 
    /// <include file='doc\StatementContext.uex' path='docs/doc[@for="ObjectStatementCollection"]/*' /> 
    /// <devdoc>
    ///    This is a table of statements that is offered by the statement context. 
    /// </devdoc>
    public sealed class ObjectStatementCollection : IEnumerable {
        private List<TableEntry> _table;
        private int _version; 

        /// <devdoc> 
        ///    Only creatable by the StatementContext. 
        /// </devdoc>
        internal ObjectStatementCollection() { 
        }

        /// <devdoc>
        ///    Adds an owner to the table.  Statements can be null, in which case it 
        ///    will be demand created when fished out of the table.  This will throw
        ///    if there is already a valid collection for the owner. 
        /// </devdoc> 
        private void AddOwner(object statementOwner, CodeStatementCollection statements) {
            if (_table == null) { 
                _table = new List<TableEntry>();
            }
            else {
                for (int idx = 0; idx < _table.Count; idx++) { 
                    if (object.ReferenceEquals(_table[idx].Owner, statementOwner)) {
                        if (_table[idx].Statements != null) { 
                            throw new InvalidOperationException(); 
                        }
                        else { 
                            if (statements != null) {
                                _table[idx] = new TableEntry(statementOwner, statements);
                            }
                            return; 
                        }
                    } 
 
                }
            } 

            _table.Add(new TableEntry(statementOwner, statements));
            _version++;
        } 

        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.Item"]/*' /> 
        /// <devdoc> 
        ///    Indexer.  This will return the statement collection for the given owner.
        ///    It will return null only if the owner is not in the table. 
        /// </devdoc>
        public CodeStatementCollection this[object statementOwner] {
            get {
                if (statementOwner == null) { 
                    throw new ArgumentNullException("statementOwner");
                } 
 
                if (_table != null) {
                    for (int idx = 0; idx < _table.Count; idx++) { 
                        if (object.ReferenceEquals(_table[idx].Owner, statementOwner)) {
                            if (_table[idx].Statements == null) {
                                _table[idx] = new TableEntry(statementOwner, new CodeStatementCollection());
                            } 
                            return _table[idx].Statements;
                        } 
                    } 
                    foreach(TableEntry e in _table) {
                        if (object.ReferenceEquals(e.Owner, statementOwner)) { 
                            return e.Statements;
                        }
                    }
                } 

                return null; 
            } 
        }
 
        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.ContainsKey"]/*' />
        /// <devdoc>
        ///    Returns true if the given statement owner is in the table.
        /// </devdoc> 
        public bool ContainsKey(object statementOwner) {
            if (statementOwner == null) { 
                throw new ArgumentNullException("statementOwner"); 
            }
 
            if (_table != null) {
                return (this[statementOwner] != null);
            }
 
            return false;
        } 
 
        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.GetEnumerator"]/*' />
        /// <devdoc> 
        ///    Returns an enumerator for this table.  The keys of the enumerator are statement
        ///    owner objects and the values are instances of CodeStatementCollection.
        /// </devdoc>
        public IDictionaryEnumerator GetEnumerator() { 
            return new TableEnumerator(this);
        } 
 
        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.Populate"]/*' />
        /// <devdoc> 
        ///    This method populates the statement table with a collection of statement owners.
        ///    The creator of the statement context should do this if it wishes statement tables to be used to store
        ///    values for certain objects.
        /// </devdoc> 
        public void Populate(ICollection statementOwners) {
            if (statementOwners == null) { 
                throw new ArgumentNullException("statementOwners"); 
            }
            foreach(object o in statementOwners) { 
                Populate(o);
            }
        }
 
        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.Populate1"]/*' />
        /// <devdoc> 
        ///    This method populates the statement table with a collection of statement owners. 
        ///    The creator of the statement context should do this if it wishes statement tables to be used to store
        ///    values for certain objects. 
        /// </devdoc>
        public void Populate(object owner) {
            if (owner == null) {
                throw new ArgumentNullException("owner"); 
            }
            AddOwner(owner, null); 
        } 

        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.IEnumerable.GetEnumerator"]/*' /> 
        /// <devdoc>
        ///    Returns an enumerator for this table.  The value is a DictionaryEntry containing
        ///    the statement owner and the statement collection.
        /// </devdoc> 
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator(); 
        } 

        private struct TableEntry { 
            public object Owner;
            public CodeStatementCollection Statements;
            public TableEntry(object owner, CodeStatementCollection statements) {
                this.Owner = owner; 
                this.Statements = statements;
            } 
        } 

        private struct TableEnumerator : IDictionaryEnumerator { 
            private ObjectStatementCollection _table;
            private int _version;
            int _position;
 
            public TableEnumerator(ObjectStatementCollection table) {
                _table = table; 
                _version = _table._version; 
                _position = -1;
            } 

            public object Current {
                get {
                    return Entry; 
                }
            } 
 
            public DictionaryEntry Entry {
                get { 
                    if (_version != _table._version) {
                        throw new InvalidOperationException();
                    }
                    if (_position < 0 || _table._table == null || _position >= _table._table.Count) { 
                        throw new InvalidOperationException();
                    } 
 
                    if (_table._table[_position].Statements == null) {
                        _table._table[_position] = new TableEntry(_table._table[_position].Owner, new CodeStatementCollection()); 
                    }

                    TableEntry entry = _table._table[_position];
                    return new DictionaryEntry(entry.Owner, entry.Statements); 
                }
            } 
 
            public object Key {
                get { 
                    return Entry.Key;
                }
            }
 
            public object Value {
                get { 
                    return Entry.Value; 
                }
            } 

            public bool MoveNext() {
                if (_table._table != null && (_position+1) < _table._table.Count) {
                    _position++; 
                    return true;
                } 
                return false; 
            }
 
            public void Reset() {
                _position = -1;
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="StatementContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 
    using System.Collections;
    using System.Collections.Generic;

    /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext"]/*' /> 
    /// <devdoc>
    ///    This object can be placed on the context stack to provide a place for statements 
    ///    to be serialized into.  Normally, statements are serialized into whatever statement 
    ///    collection that is on the context stack.  You can modify this behavior by creating
    ///    a statement context and calling Populate with a collection of objects whose statements 
    ///    you would like stored in the statement table.  As each object is serialized in
    ///    SerializeToExpression it will have its contents placed in the statement table.
    ///    saved in a table within the context.  If you push this object on the stack it is your
    ///    responsibility to integrate the statements added to it into your own collection of statements. 
    /// </devdoc>
    public sealed class StatementContext { 
 
        private ObjectStatementCollection _statements;
 
        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.StatementCollection"]/*' />
        /// <devdoc>
        ///    This is a table of statements that is offered by the statement context.
        /// </devdoc> 
        public ObjectStatementCollection StatementCollection {
            get { 
                if (_statements == null) { 
                    _statements = new ObjectStatementCollection();
                } 

                return _statements;
            }
        } 
    }
 
    /// <include file='doc\StatementContext.uex' path='docs/doc[@for="ObjectStatementCollection"]/*' /> 
    /// <devdoc>
    ///    This is a table of statements that is offered by the statement context. 
    /// </devdoc>
    public sealed class ObjectStatementCollection : IEnumerable {
        private List<TableEntry> _table;
        private int _version; 

        /// <devdoc> 
        ///    Only creatable by the StatementContext. 
        /// </devdoc>
        internal ObjectStatementCollection() { 
        }

        /// <devdoc>
        ///    Adds an owner to the table.  Statements can be null, in which case it 
        ///    will be demand created when fished out of the table.  This will throw
        ///    if there is already a valid collection for the owner. 
        /// </devdoc> 
        private void AddOwner(object statementOwner, CodeStatementCollection statements) {
            if (_table == null) { 
                _table = new List<TableEntry>();
            }
            else {
                for (int idx = 0; idx < _table.Count; idx++) { 
                    if (object.ReferenceEquals(_table[idx].Owner, statementOwner)) {
                        if (_table[idx].Statements != null) { 
                            throw new InvalidOperationException(); 
                        }
                        else { 
                            if (statements != null) {
                                _table[idx] = new TableEntry(statementOwner, statements);
                            }
                            return; 
                        }
                    } 
 
                }
            } 

            _table.Add(new TableEntry(statementOwner, statements));
            _version++;
        } 

        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.Item"]/*' /> 
        /// <devdoc> 
        ///    Indexer.  This will return the statement collection for the given owner.
        ///    It will return null only if the owner is not in the table. 
        /// </devdoc>
        public CodeStatementCollection this[object statementOwner] {
            get {
                if (statementOwner == null) { 
                    throw new ArgumentNullException("statementOwner");
                } 
 
                if (_table != null) {
                    for (int idx = 0; idx < _table.Count; idx++) { 
                        if (object.ReferenceEquals(_table[idx].Owner, statementOwner)) {
                            if (_table[idx].Statements == null) {
                                _table[idx] = new TableEntry(statementOwner, new CodeStatementCollection());
                            } 
                            return _table[idx].Statements;
                        } 
                    } 
                    foreach(TableEntry e in _table) {
                        if (object.ReferenceEquals(e.Owner, statementOwner)) { 
                            return e.Statements;
                        }
                    }
                } 

                return null; 
            } 
        }
 
        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.ContainsKey"]/*' />
        /// <devdoc>
        ///    Returns true if the given statement owner is in the table.
        /// </devdoc> 
        public bool ContainsKey(object statementOwner) {
            if (statementOwner == null) { 
                throw new ArgumentNullException("statementOwner"); 
            }
 
            if (_table != null) {
                return (this[statementOwner] != null);
            }
 
            return false;
        } 
 
        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.GetEnumerator"]/*' />
        /// <devdoc> 
        ///    Returns an enumerator for this table.  The keys of the enumerator are statement
        ///    owner objects and the values are instances of CodeStatementCollection.
        /// </devdoc>
        public IDictionaryEnumerator GetEnumerator() { 
            return new TableEnumerator(this);
        } 
 
        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.Populate"]/*' />
        /// <devdoc> 
        ///    This method populates the statement table with a collection of statement owners.
        ///    The creator of the statement context should do this if it wishes statement tables to be used to store
        ///    values for certain objects.
        /// </devdoc> 
        public void Populate(ICollection statementOwners) {
            if (statementOwners == null) { 
                throw new ArgumentNullException("statementOwners"); 
            }
            foreach(object o in statementOwners) { 
                Populate(o);
            }
        }
 
        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.Populate1"]/*' />
        /// <devdoc> 
        ///    This method populates the statement table with a collection of statement owners. 
        ///    The creator of the statement context should do this if it wishes statement tables to be used to store
        ///    values for certain objects. 
        /// </devdoc>
        public void Populate(object owner) {
            if (owner == null) {
                throw new ArgumentNullException("owner"); 
            }
            AddOwner(owner, null); 
        } 

        /// <include file='doc\StatementContext.uex' path='docs/doc[@for="StatementContext.ObjectStatementCollection.IEnumerable.GetEnumerator"]/*' /> 
        /// <devdoc>
        ///    Returns an enumerator for this table.  The value is a DictionaryEntry containing
        ///    the statement owner and the statement collection.
        /// </devdoc> 
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator(); 
        } 

        private struct TableEntry { 
            public object Owner;
            public CodeStatementCollection Statements;
            public TableEntry(object owner, CodeStatementCollection statements) {
                this.Owner = owner; 
                this.Statements = statements;
            } 
        } 

        private struct TableEnumerator : IDictionaryEnumerator { 
            private ObjectStatementCollection _table;
            private int _version;
            int _position;
 
            public TableEnumerator(ObjectStatementCollection table) {
                _table = table; 
                _version = _table._version; 
                _position = -1;
            } 

            public object Current {
                get {
                    return Entry; 
                }
            } 
 
            public DictionaryEntry Entry {
                get { 
                    if (_version != _table._version) {
                        throw new InvalidOperationException();
                    }
                    if (_position < 0 || _table._table == null || _position >= _table._table.Count) { 
                        throw new InvalidOperationException();
                    } 
 
                    if (_table._table[_position].Statements == null) {
                        _table._table[_position] = new TableEntry(_table._table[_position].Owner, new CodeStatementCollection()); 
                    }

                    TableEntry entry = _table._table[_position];
                    return new DictionaryEntry(entry.Owner, entry.Statements); 
                }
            } 
 
            public object Key {
                get { 
                    return Entry.Key;
                }
            }
 
            public object Value {
                get { 
                    return Entry.Value; 
                }
            } 

            public bool MoveNext() {
                if (_table._table != null && (_position+1) < _table._table.Count) {
                    _position++; 
                    return true;
                } 
                return false; 
            }
 
            public void Reset() {
                _position = -1;
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
