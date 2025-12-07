//------------------------------------------------------------------------------ 
// <copyright file="XmlNullResolver.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Xml { 
    using System;
 
    internal class XmlNullResolver : XmlUrlResolver {
        public static readonly XmlNullResolver Singleton = new XmlNullResolver();

        public override Object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) { 
            throw new XmlException(Res.Xml_NullResolver, string.Empty);
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XmlNullResolver.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Xml { 
    using System;
 
    internal class XmlNullResolver : XmlUrlResolver {
        public static readonly XmlNullResolver Singleton = new XmlNullResolver();

        public override Object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) { 
            throw new XmlException(Res.Xml_NullResolver, string.Empty);
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
