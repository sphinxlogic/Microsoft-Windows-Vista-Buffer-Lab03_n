//------------------------------------------------------------------------------ 
// <copyright file="Operator.cs" company="Microsoft">
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
    internal class Operator : AstNode { 
        public enum Op {
            LT, 
            GT, 
            LE,
            GE, 
            EQ,
            NE,
            PLUS,
            MINUS, 
            MUL,
            MOD, 
            DIV, 
            OR,
            AND, 
            UNION,
            INVALID
        };
 
        private Op opType;
        private AstNode opnd1; 
        private AstNode opnd2; 

        public Operator(Op op, AstNode opnd1, AstNode opnd2) { 
            this.opType = op;
            this.opnd1 = opnd1;
            this.opnd2 = opnd2;
        } 

        public override AstType Type { get {return  AstType.Operator;} } 
        public override XPathResultType ReturnType { 
            get {
                if (opType < Op.LT) { 
                    return XPathResultType.Number;
                }
                if (opType < Op.UNION) {
                    return XPathResultType.Boolean; 
                }
                return XPathResultType.NodeSet; 
            } 
        }
 
        public Op      OperatorType { get { return opType; } }
        public AstNode Operand1     { get { return opnd1;  } }
        public AstNode Operand2     { get { return opnd2;  } }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="Operator.cs" company="Microsoft">
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
    internal class Operator : AstNode { 
        public enum Op {
            LT, 
            GT, 
            LE,
            GE, 
            EQ,
            NE,
            PLUS,
            MINUS, 
            MUL,
            MOD, 
            DIV, 
            OR,
            AND, 
            UNION,
            INVALID
        };
 
        private Op opType;
        private AstNode opnd1; 
        private AstNode opnd2; 

        public Operator(Op op, AstNode opnd1, AstNode opnd2) { 
            this.opType = op;
            this.opnd1 = opnd1;
            this.opnd2 = opnd2;
        } 

        public override AstType Type { get {return  AstType.Operator;} } 
        public override XPathResultType ReturnType { 
            get {
                if (opType < Op.LT) { 
                    return XPathResultType.Number;
                }
                if (opType < Op.UNION) {
                    return XPathResultType.Boolean; 
                }
                return XPathResultType.NodeSet; 
            } 
        }
 
        public Op      OperatorType { get { return opType; } }
        public AstNode Operand1     { get { return opnd1;  } }
        public AstNode Operand2     { get { return opnd2;  } }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
