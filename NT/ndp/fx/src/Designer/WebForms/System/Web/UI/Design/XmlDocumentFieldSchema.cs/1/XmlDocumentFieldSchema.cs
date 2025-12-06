//------------------------------------------------------------------------------ 
// <copyright file="XmlDocumentFieldSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.Diagnostics; 
    using System.Xml;
    using System.Xml.XPath;

    /// <devdoc> 
    /// A class to expose hierarchical schema from an XmlDocument object.
    /// This is used by data source designers to enable data-bound to 
    /// traverse their schema at design time. 
    /// </devdoc>
    internal sealed class XmlDocumentFieldSchema : IDataSourceFieldSchema { 
        private string _name;

        public XmlDocumentFieldSchema(string name) {
            Debug.Assert(name != null && name.Length > 0); 
            _name = name;
        } 
 
        public Type DataType {
            get { 
                return typeof(string);
            }
        }
 
        public bool Identity {
            get { 
                return false; 
            }
        } 

        public bool IsReadOnly {
            get {
                return false; 
            }
        } 
 
        public bool IsUnique {
            get { 
                return false;
            }
        }
 
        public int Length {
            get { 
                return -1; 
            }
        } 

        public string Name {
            get {
                return _name; 
            }
        } 
 
        public bool Nullable {
            get { 
                return true;
            }
        }
 
        public int Precision {
            get { 
                return -1; 
            }
        } 

        public bool PrimaryKey {
            get {
                return false; 
            }
        } 
 
        public int Scale {
            get { 
                return -1;
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XmlDocumentFieldSchema.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.Diagnostics; 
    using System.Xml;
    using System.Xml.XPath;

    /// <devdoc> 
    /// A class to expose hierarchical schema from an XmlDocument object.
    /// This is used by data source designers to enable data-bound to 
    /// traverse their schema at design time. 
    /// </devdoc>
    internal sealed class XmlDocumentFieldSchema : IDataSourceFieldSchema { 
        private string _name;

        public XmlDocumentFieldSchema(string name) {
            Debug.Assert(name != null && name.Length > 0); 
            _name = name;
        } 
 
        public Type DataType {
            get { 
                return typeof(string);
            }
        }
 
        public bool Identity {
            get { 
                return false; 
            }
        } 

        public bool IsReadOnly {
            get {
                return false; 
            }
        } 
 
        public bool IsUnique {
            get { 
                return false;
            }
        }
 
        public int Length {
            get { 
                return -1; 
            }
        } 

        public string Name {
            get {
                return _name; 
            }
        } 
 
        public bool Nullable {
            get { 
                return true;
            }
        }
 
        public int Precision {
            get { 
                return -1; 
            }
        } 

        public bool PrimaryKey {
            get {
                return false; 
            }
        } 
 
        public int Scale {
            get { 
                return -1;
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
