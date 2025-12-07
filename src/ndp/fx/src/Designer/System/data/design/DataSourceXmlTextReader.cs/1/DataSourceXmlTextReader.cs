 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
 
    using System;
    using System.Collections;
    using System.IO;
    using System.Text; 

    using System.Xml; 
    using System.Xml.Schema; 

    internal class DataSourceXmlTextReader : XmlTextReader { 
        private DesignDataSource    dataSource;
        private bool                readingDataSource;

        internal DataSourceXmlTextReader(DesignDataSource dataSource, TextReader textReader) : base(textReader) { 
            this.dataSource = dataSource;
            this.readingDataSource = false; 
        } 

        internal DataSourceXmlTextReader(DesignDataSource dataSource, Stream stream) : base(stream) { 
            this.dataSource = dataSource;
            this.readingDataSource = false;
        }
 
        public override bool Read() {
            bool result = base.Read(); 
 
            if (result && !readingDataSource) {
                if (NodeType == XmlNodeType.Element) { 
                    if (LocalName == SchemaName.DataSourceRoot && NamespaceURI == SchemaName.DataSourceNamespace) {
                        readingDataSource = true;
                        dataSource.ReadDataSourceExtraInformation(this);
                        result = !EOF; 
                    }
                } 
            } 
            return result;
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
 
namespace System.Data.Design {
 
    using System;
    using System.Collections;
    using System.IO;
    using System.Text; 

    using System.Xml; 
    using System.Xml.Schema; 

    internal class DataSourceXmlTextReader : XmlTextReader { 
        private DesignDataSource    dataSource;
        private bool                readingDataSource;

        internal DataSourceXmlTextReader(DesignDataSource dataSource, TextReader textReader) : base(textReader) { 
            this.dataSource = dataSource;
            this.readingDataSource = false; 
        } 

        internal DataSourceXmlTextReader(DesignDataSource dataSource, Stream stream) : base(stream) { 
            this.dataSource = dataSource;
            this.readingDataSource = false;
        }
 
        public override bool Read() {
            bool result = base.Read(); 
 
            if (result && !readingDataSource) {
                if (NodeType == XmlNodeType.Element) { 
                    if (LocalName == SchemaName.DataSourceRoot && NamespaceURI == SchemaName.DataSourceNamespace) {
                        readingDataSource = true;
                        dataSource.ReadDataSourceExtraInformation(this);
                        result = !EOF; 
                    }
                } 
            } 
            return result;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
