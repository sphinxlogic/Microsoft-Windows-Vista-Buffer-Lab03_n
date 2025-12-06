//------------------------------------------------------------------------------ 
// <copyright file="XmlDocumentSchema.cs" company="Microsoft">
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
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class XmlDocumentSchema : IDataSourceSchema {
        private OrderedDictionary _rootSchema;
        private IDataSourceViewSchema[] _viewSchemas; 
        private bool _includeSpecialSchema;
 
        public XmlDocumentSchema(XmlDocument xmlDocument, string xPath) : this(xmlDocument, xPath, false) { 
        }
 
        internal XmlDocumentSchema(XmlDocument xmlDocument, string xPath, bool includeSpecialSchema) {
            if (xmlDocument == null) {
                throw new ArgumentNullException("xmlDocument");
            } 

            _includeSpecialSchema = includeSpecialSchema; 
            _rootSchema = new OrderedDictionary(); 

            // Get a list of all element nodes in document order and add them 
            // to the schema hierarchy
            XPathNavigator nav = xmlDocument.CreateNavigator();
            if (!String.IsNullOrEmpty(xPath)) {
                // If there is an XPath, go through each XPath result and add its descendants 
                XPathNodeIterator xPathIterator = nav.Select(xPath);
                while (xPathIterator.MoveNext()) { 
                    XPathNodeIterator childIterator = xPathIterator.Current.SelectDescendants(XPathNodeType.Element, true); 
                    while (childIterator.MoveNext()) {
                        Debug.Assert(childIterator.Current.NodeType == XPathNodeType.Element); 
                        AddSchemaElement(childIterator.Current, xPathIterator.Current);
                    }
                }
            } 
            else {
                // If there is no XPath, just go through the root's descendants 
                XPathNodeIterator childIterator = nav.SelectDescendants(XPathNodeType.Element, true); 
                while (childIterator.MoveNext()) {
                    Debug.Assert(childIterator.Current.NodeType == XPathNodeType.Element); 
                    AddSchemaElement(childIterator.Current, nav);
                }
            }
        } 

        private void AddSchemaElement(XPathNavigator nav, XPathNavigator rootNav) { 
            // Get list of all ancestors in reverse document order in order 
            // to get a hierarchical path to this element
            System.Collections.Generic.List<string> path = new System.Collections.Generic.List<string>(); 
            XPathNodeIterator parentIterator = nav.SelectAncestors(XPathNodeType.Element, true);
            while (parentIterator.MoveNext()) {
                Debug.Assert(parentIterator.Current.NodeType == XPathNodeType.Element);
                path.Add(parentIterator.Current.Name); 
                if (parentIterator.Current.IsSamePosition(rootNav)) {
                    // Stop if we get to the root of this sub-tree 
                    break; 
                }
            } 
            path.Reverse();

            Debug.Assert(path.Count > 0, "Expected path to contain at least one entry (the node itself!)");
 
            // Find the location in the schema where the attributes of this element belong
            OrderedDictionary schemaLevel = _rootSchema; 
            Pair nodeEntry = null; 
            foreach (string pathPart in path) {
                nodeEntry = schemaLevel[pathPart] as Pair; 
                if (nodeEntry == null) {
                    // Demand-create an entry for this type of node if it is not found
                    nodeEntry = new Pair(new OrderedDictionary() /* list of child node types */, new ArrayList() /* list of attributes */);
 
                    // Add this type of child node to the current parent type we are at
                    schemaLevel.Add(pathPart, nodeEntry); 
                } 

                // Advance one level deeper 
                schemaLevel = (OrderedDictionary)nodeEntry.First;
            }

            Debug.Assert(nodeEntry != null, "Did not expect null schema entry - it should be auto-created"); 

            // Add the list of attributes to this type of node 
            AddAttributeList(nav, (ArrayList)nodeEntry.Second); 
        }
 
        private void AddAttributeList(XPathNavigator nav, ArrayList attrs) {
            Debug.Assert(attrs != null);
            if (!nav.HasAttributes) {
                return; 
            }
            bool success = nav.MoveToFirstAttribute(); 
            Debug.Assert(success); 
            // Go through all the attributes and add new ones to the list
            do { 
                if (!attrs.Contains(nav.Name)) {
                    attrs.Add(nav.Name);
                }
            } while (nav.MoveToNextAttribute()); 
            success = nav.MoveToParent();
            Debug.Assert(success); 
        } 

        public IDataSourceViewSchema[] GetViews() { 
            if (_viewSchemas == null) {
                _viewSchemas = new IDataSourceViewSchema[_rootSchema.Count];
                int i = 0;
                foreach (DictionaryEntry de in _rootSchema) { 
                    _viewSchemas[i] = new XmlDocumentViewSchema((string)de.Key, (Pair)de.Value, _includeSpecialSchema);
                    i++; 
                } 
            }
            return _viewSchemas; 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XmlDocumentSchema.cs" company="Microsoft">
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
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class XmlDocumentSchema : IDataSourceSchema {
        private OrderedDictionary _rootSchema;
        private IDataSourceViewSchema[] _viewSchemas; 
        private bool _includeSpecialSchema;
 
        public XmlDocumentSchema(XmlDocument xmlDocument, string xPath) : this(xmlDocument, xPath, false) { 
        }
 
        internal XmlDocumentSchema(XmlDocument xmlDocument, string xPath, bool includeSpecialSchema) {
            if (xmlDocument == null) {
                throw new ArgumentNullException("xmlDocument");
            } 

            _includeSpecialSchema = includeSpecialSchema; 
            _rootSchema = new OrderedDictionary(); 

            // Get a list of all element nodes in document order and add them 
            // to the schema hierarchy
            XPathNavigator nav = xmlDocument.CreateNavigator();
            if (!String.IsNullOrEmpty(xPath)) {
                // If there is an XPath, go through each XPath result and add its descendants 
                XPathNodeIterator xPathIterator = nav.Select(xPath);
                while (xPathIterator.MoveNext()) { 
                    XPathNodeIterator childIterator = xPathIterator.Current.SelectDescendants(XPathNodeType.Element, true); 
                    while (childIterator.MoveNext()) {
                        Debug.Assert(childIterator.Current.NodeType == XPathNodeType.Element); 
                        AddSchemaElement(childIterator.Current, xPathIterator.Current);
                    }
                }
            } 
            else {
                // If there is no XPath, just go through the root's descendants 
                XPathNodeIterator childIterator = nav.SelectDescendants(XPathNodeType.Element, true); 
                while (childIterator.MoveNext()) {
                    Debug.Assert(childIterator.Current.NodeType == XPathNodeType.Element); 
                    AddSchemaElement(childIterator.Current, nav);
                }
            }
        } 

        private void AddSchemaElement(XPathNavigator nav, XPathNavigator rootNav) { 
            // Get list of all ancestors in reverse document order in order 
            // to get a hierarchical path to this element
            System.Collections.Generic.List<string> path = new System.Collections.Generic.List<string>(); 
            XPathNodeIterator parentIterator = nav.SelectAncestors(XPathNodeType.Element, true);
            while (parentIterator.MoveNext()) {
                Debug.Assert(parentIterator.Current.NodeType == XPathNodeType.Element);
                path.Add(parentIterator.Current.Name); 
                if (parentIterator.Current.IsSamePosition(rootNav)) {
                    // Stop if we get to the root of this sub-tree 
                    break; 
                }
            } 
            path.Reverse();

            Debug.Assert(path.Count > 0, "Expected path to contain at least one entry (the node itself!)");
 
            // Find the location in the schema where the attributes of this element belong
            OrderedDictionary schemaLevel = _rootSchema; 
            Pair nodeEntry = null; 
            foreach (string pathPart in path) {
                nodeEntry = schemaLevel[pathPart] as Pair; 
                if (nodeEntry == null) {
                    // Demand-create an entry for this type of node if it is not found
                    nodeEntry = new Pair(new OrderedDictionary() /* list of child node types */, new ArrayList() /* list of attributes */);
 
                    // Add this type of child node to the current parent type we are at
                    schemaLevel.Add(pathPart, nodeEntry); 
                } 

                // Advance one level deeper 
                schemaLevel = (OrderedDictionary)nodeEntry.First;
            }

            Debug.Assert(nodeEntry != null, "Did not expect null schema entry - it should be auto-created"); 

            // Add the list of attributes to this type of node 
            AddAttributeList(nav, (ArrayList)nodeEntry.Second); 
        }
 
        private void AddAttributeList(XPathNavigator nav, ArrayList attrs) {
            Debug.Assert(attrs != null);
            if (!nav.HasAttributes) {
                return; 
            }
            bool success = nav.MoveToFirstAttribute(); 
            Debug.Assert(success); 
            // Go through all the attributes and add new ones to the list
            do { 
                if (!attrs.Contains(nav.Name)) {
                    attrs.Add(nav.Name);
                }
            } while (nav.MoveToNextAttribute()); 
            success = nav.MoveToParent();
            Debug.Assert(success); 
        } 

        public IDataSourceViewSchema[] GetViews() { 
            if (_viewSchemas == null) {
                _viewSchemas = new IDataSourceViewSchema[_rootSchema.Count];
                int i = 0;
                foreach (DictionaryEntry de in _rootSchema) { 
                    _viewSchemas[i] = new XmlDocumentViewSchema((string)de.Key, (Pair)de.Value, _includeSpecialSchema);
                    i++; 
                } 
            }
            return _viewSchemas; 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
