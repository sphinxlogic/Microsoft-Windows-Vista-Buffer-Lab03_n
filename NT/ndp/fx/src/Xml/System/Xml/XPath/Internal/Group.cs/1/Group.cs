//------------------------------------------------------------------------------ 
// <copyright file="Group.cs" company="Microsoft">
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
    internal class Group : AstNode { 
        private AstNode groupNode;
 
        public Group(AstNode groupNode) { 
            this.groupNode = groupNode;
        } 
        public override AstType         Type       { get { return AstType.Group;           } }
        public override XPathResultType ReturnType { get { return XPathResultType.NodeSet; } }

        public AstNode GroupNode { get { return groupNode; } } 
    }
} 
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="Group.cs" company="Microsoft">
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
    internal class Group : AstNode { 
        private AstNode groupNode;
 
        public Group(AstNode groupNode) { 
            this.groupNode = groupNode;
        } 
        public override AstType         Type       { get { return AstType.Group;           } }
        public override XPathResultType ReturnType { get { return XPathResultType.NodeSet; } }

        public AstNode GroupNode { get { return groupNode; } } 
    }
} 
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
