//------------------------------------------------------------------------------ 
// <copyright file="XmlDataImplementation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="false" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Xml { 
    using System;

    internal sealed class XmlDataImplementation : XmlImplementation {
 
        public XmlDataImplementation() : base() {
        } 
 
        public override XmlDocument CreateDocument() {
            return new XmlDataDocument( this ); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XmlDataImplementation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="false" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Xml { 
    using System;

    internal sealed class XmlDataImplementation : XmlImplementation {
 
        public XmlDataImplementation() : base() {
        } 
 
        public override XmlDocument CreateDocument() {
            return new XmlDataDocument( this ); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
