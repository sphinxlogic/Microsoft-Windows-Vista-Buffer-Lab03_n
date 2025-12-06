//------------------------------------------------------------------------------ 
// <copyright file="ContextMenuStripGroupCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections.Generic;
    using System.Text;
    using System.Collections;
 

    internal class ContextMenuStripGroupCollection : DictionaryBase { 
 
        public ContextMenuStripGroupCollection() {
 
        }

        public ContextMenuStripGroup this[string key] {
            get { 
                if (!this.InnerHashtable.ContainsKey(key)) {
                    this.InnerHashtable[key] = new ContextMenuStripGroup(key); 
                } 
                return this.InnerHashtable[key] as ContextMenuStripGroup;
 
            }
        }

        public bool ContainsKey(string key) { 
            return InnerHashtable.ContainsKey(key);
        } 
        protected override void OnInsert(object key, object value) { 
            if (!(value is ContextMenuStripGroup)) {
                throw new NotSupportedException(); 
            }
            base.OnInsert(key, value);
        }
 
        protected override void OnSet(object key, object oldValue, object newValue) {
            if (!(newValue is ContextMenuStripGroup)) { 
                throw new NotSupportedException(); 
            }
            base.OnSet(key, oldValue, newValue); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ContextMenuStripGroupCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections.Generic;
    using System.Text;
    using System.Collections;
 

    internal class ContextMenuStripGroupCollection : DictionaryBase { 
 
        public ContextMenuStripGroupCollection() {
 
        }

        public ContextMenuStripGroup this[string key] {
            get { 
                if (!this.InnerHashtable.ContainsKey(key)) {
                    this.InnerHashtable[key] = new ContextMenuStripGroup(key); 
                } 
                return this.InnerHashtable[key] as ContextMenuStripGroup;
 
            }
        }

        public bool ContainsKey(string key) { 
            return InnerHashtable.ContainsKey(key);
        } 
        protected override void OnInsert(object key, object value) { 
            if (!(value is ContextMenuStripGroup)) {
                throw new NotSupportedException(); 
            }
            base.OnInsert(key, value);
        }
 
        protected override void OnSet(object key, object oldValue, object newValue) {
            if (!(newValue is ContextMenuStripGroup)) { 
                throw new NotSupportedException(); 
            }
            base.OnSet(key, oldValue, newValue); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
