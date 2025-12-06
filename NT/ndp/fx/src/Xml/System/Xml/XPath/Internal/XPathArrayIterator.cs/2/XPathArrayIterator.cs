//------------------------------------------------------------------------------ 
// <copyright file="XPathArrayIterator.cs" company="Microsoft">
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

    [DebuggerDisplay("Position={CurrentPosition}, Current={debuggerDisplayProxy, nq}")] 
    internal class XPathArrayIterator : ResetableIterator { 
        protected IList     list;
        protected int       index; 

        public XPathArrayIterator(IList list) {
            this.list = list;
        } 

        public XPathArrayIterator(XPathArrayIterator it) { 
            this.list = it.list; 
            this.index = it.index;
        } 

        public XPathArrayIterator(XPathNodeIterator nodeIterator) {
            this.list = new ArrayList();
            while (nodeIterator.MoveNext()) { 
                this.list.Add(nodeIterator.Current.Clone());
            } 
        } 

        public IList AsList { 
            get { return this.list; }
        }

        public override XPathNodeIterator Clone() { 
            return new XPathArrayIterator(this);
        } 
 
        public override XPathNavigator Current {
            get { 
                Debug.Assert(index <= list.Count);

                if (index < 1)
                    throw new InvalidOperationException(Res.GetString(Res.Sch_EnumNotStarted, string.Empty)); 

                return (XPathNavigator) list[index - 1]; 
            } 
        }
 
        public override int CurrentPosition { get { return index; } }
        public override int Count           { get { return list.Count; } }

        public override bool MoveNext() { 
            Debug.Assert(index <= list.Count);
            if (index == list.Count) { 
                return false; 
            }
            index++; 
            return true;
        }

        public override void Reset() { 
            index = 0;
        } 
 
        public override IEnumerator GetEnumerator() {
            return list.GetEnumerator(); 
        }

        private object debuggerDisplayProxy { get { return index < 1 ? null : (object)new XPathNavigator.DebuggerDisplayProxy(Current); } }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XPathArrayIterator.cs" company="Microsoft">
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

    [DebuggerDisplay("Position={CurrentPosition}, Current={debuggerDisplayProxy, nq}")] 
    internal class XPathArrayIterator : ResetableIterator { 
        protected IList     list;
        protected int       index; 

        public XPathArrayIterator(IList list) {
            this.list = list;
        } 

        public XPathArrayIterator(XPathArrayIterator it) { 
            this.list = it.list; 
            this.index = it.index;
        } 

        public XPathArrayIterator(XPathNodeIterator nodeIterator) {
            this.list = new ArrayList();
            while (nodeIterator.MoveNext()) { 
                this.list.Add(nodeIterator.Current.Clone());
            } 
        } 

        public IList AsList { 
            get { return this.list; }
        }

        public override XPathNodeIterator Clone() { 
            return new XPathArrayIterator(this);
        } 
 
        public override XPathNavigator Current {
            get { 
                Debug.Assert(index <= list.Count);

                if (index < 1)
                    throw new InvalidOperationException(Res.GetString(Res.Sch_EnumNotStarted, string.Empty)); 

                return (XPathNavigator) list[index - 1]; 
            } 
        }
 
        public override int CurrentPosition { get { return index; } }
        public override int Count           { get { return list.Count; } }

        public override bool MoveNext() { 
            Debug.Assert(index <= list.Count);
            if (index == list.Count) { 
                return false; 
            }
            index++; 
            return true;
        }

        public override void Reset() { 
            index = 0;
        } 
 
        public override IEnumerator GetEnumerator() {
            return list.GetEnumerator(); 
        }

        private object debuggerDisplayProxy { get { return index < 1 ? null : (object)new XPathNavigator.DebuggerDisplayProxy(Current); } }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
