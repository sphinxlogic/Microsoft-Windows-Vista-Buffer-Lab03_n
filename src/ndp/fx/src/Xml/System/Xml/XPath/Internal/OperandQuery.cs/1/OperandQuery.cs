//------------------------------------------------------------------------------ 
// <copyright file="OperandQuery.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace MS.Internal.Xml.XPath { 
    using System;
    using System.Xml; 
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;
    using System.Collections; 

    internal sealed class OperandQuery : ValueQuery { 
        internal object val; 

        public OperandQuery(object val) { 
            this.val = val;
        }

        public override object Evaluate(XPathNodeIterator nodeIterator) { 
            return val;
        } 
        public override XPathResultType StaticType { get { return GetXPathType(val); } } 
        public override XPathNodeIterator Clone() { return this; }
 
        public override void PrintQuery(XmlWriter w) {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("value", val.ToString());
            w.WriteEndElement(); 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="OperandQuery.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace MS.Internal.Xml.XPath { 
    using System;
    using System.Xml; 
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;
    using System.Collections; 

    internal sealed class OperandQuery : ValueQuery { 
        internal object val; 

        public OperandQuery(object val) { 
            this.val = val;
        }

        public override object Evaluate(XPathNodeIterator nodeIterator) { 
            return val;
        } 
        public override XPathResultType StaticType { get { return GetXPathType(val); } } 
        public override XPathNodeIterator Clone() { return this; }
 
        public override void PrintQuery(XmlWriter w) {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("value", val.ToString());
            w.WriteEndElement(); 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
