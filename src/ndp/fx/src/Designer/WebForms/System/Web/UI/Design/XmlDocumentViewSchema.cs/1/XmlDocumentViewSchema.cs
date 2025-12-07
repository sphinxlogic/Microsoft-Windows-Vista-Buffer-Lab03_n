//------------------------------------------------------------------------------ 
// <copyright file="XmlDocumentViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
 
    /// <devdoc>
    /// A class to expose hierarchical schema from an XmlDocument object. 
    /// This is used by data source designers to enable data-bound to 
    /// traverse their schema at design time.
    /// </devdoc> 
    internal sealed class XmlDocumentViewSchema : IDataSourceViewSchema {
        private string _name;
        private OrderedDictionary _children;
        private ArrayList _attrs; 
        private IDataSourceViewSchema[] _viewSchemas;
        private IDataSourceFieldSchema[] _fieldSchemas; 
        private bool _includeSpecialSchema; 

        public XmlDocumentViewSchema(string name, Pair data, bool includeSpecialSchema) { 
            _includeSpecialSchema = includeSpecialSchema;
            Debug.Assert(name != null && name.Length > 0);
            Debug.Assert(data != null);
            _children = (OrderedDictionary)data.First; 
            Debug.Assert(_children != null);
            _attrs = (ArrayList)data.Second; 
            Debug.Assert(_attrs != null); 
            _name = name;
        } 

        public string Name {
            get {
                return _name; 
            }
        } 
 
        public IDataSourceViewSchema[] GetChildren() {
            if (_viewSchemas == null) { 
                _viewSchemas = new IDataSourceViewSchema[_children.Count];
                int i = 0;
                foreach (DictionaryEntry de in _children) {
                    _viewSchemas[i] = new XmlDocumentViewSchema((string)de.Key, (Pair)de.Value, _includeSpecialSchema); 
                    i++;
                } 
            } 
            return _viewSchemas;
        } 

        public IDataSourceFieldSchema[] GetFields() {
            if (_fieldSchemas == null) {
                // The three extra slots are for the "special" field names 
                int specialSchemaCount = (_includeSpecialSchema ? 3 : 0);
                _fieldSchemas = new IDataSourceFieldSchema[_attrs.Count + specialSchemaCount]; 
                if (_includeSpecialSchema) { 
                    _fieldSchemas[0] = new XmlDocumentFieldSchema("#Name");
                    _fieldSchemas[1] = new XmlDocumentFieldSchema("#Value"); 
                    _fieldSchemas[2] = new XmlDocumentFieldSchema("#InnerText");
                }
                for (int i = 0; i < _attrs.Count; i++) {
                    _fieldSchemas[i + specialSchemaCount] = new XmlDocumentFieldSchema((string)_attrs[i]); 
                }
            } 
            return _fieldSchemas; 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XmlDocumentViewSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
 
    /// <devdoc>
    /// A class to expose hierarchical schema from an XmlDocument object. 
    /// This is used by data source designers to enable data-bound to 
    /// traverse their schema at design time.
    /// </devdoc> 
    internal sealed class XmlDocumentViewSchema : IDataSourceViewSchema {
        private string _name;
        private OrderedDictionary _children;
        private ArrayList _attrs; 
        private IDataSourceViewSchema[] _viewSchemas;
        private IDataSourceFieldSchema[] _fieldSchemas; 
        private bool _includeSpecialSchema; 

        public XmlDocumentViewSchema(string name, Pair data, bool includeSpecialSchema) { 
            _includeSpecialSchema = includeSpecialSchema;
            Debug.Assert(name != null && name.Length > 0);
            Debug.Assert(data != null);
            _children = (OrderedDictionary)data.First; 
            Debug.Assert(_children != null);
            _attrs = (ArrayList)data.Second; 
            Debug.Assert(_attrs != null); 
            _name = name;
        } 

        public string Name {
            get {
                return _name; 
            }
        } 
 
        public IDataSourceViewSchema[] GetChildren() {
            if (_viewSchemas == null) { 
                _viewSchemas = new IDataSourceViewSchema[_children.Count];
                int i = 0;
                foreach (DictionaryEntry de in _children) {
                    _viewSchemas[i] = new XmlDocumentViewSchema((string)de.Key, (Pair)de.Value, _includeSpecialSchema); 
                    i++;
                } 
            } 
            return _viewSchemas;
        } 

        public IDataSourceFieldSchema[] GetFields() {
            if (_fieldSchemas == null) {
                // The three extra slots are for the "special" field names 
                int specialSchemaCount = (_includeSpecialSchema ? 3 : 0);
                _fieldSchemas = new IDataSourceFieldSchema[_attrs.Count + specialSchemaCount]; 
                if (_includeSpecialSchema) { 
                    _fieldSchemas[0] = new XmlDocumentFieldSchema("#Name");
                    _fieldSchemas[1] = new XmlDocumentFieldSchema("#Value"); 
                    _fieldSchemas[2] = new XmlDocumentFieldSchema("#InnerText");
                }
                for (int i = 0; i < _attrs.Count; i++) {
                    _fieldSchemas[i + specialSchemaCount] = new XmlDocumentFieldSchema((string)_attrs[i]); 
                }
            } 
            return _fieldSchemas; 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
