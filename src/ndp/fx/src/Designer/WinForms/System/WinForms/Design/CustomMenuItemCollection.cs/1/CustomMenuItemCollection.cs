namespace System.Windows.Forms.Design { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Windows.Forms; 

    /// <include file='doc\CustomMenuItemCollection.uex' path='docs/doc[@for="CustomMenuItemCollection"]/*' /> 
    /// <devdoc>
    /// A strongly-typed collection that stores ToolStripMenuItem objects for DesignerContextMenu
    /// </devdoc>
    internal class CustomMenuItemCollection : CollectionBase { 

        /// <include file='doc\CustomMenuItemCollection.uex' path='docs/doc[@for="CustomMenuItemCollection.CustomMenuItemCollection"]/*' /> 
        /// <devdoc> 
        ///   Constructor
        /// </devdoc> 
        public CustomMenuItemCollection() {
        }

        /// <include file='doc\CustomMenuItemCollection.uex' path='docs/doc[@for="CustomMenuItemCollection.Add"]/*' /> 
        /// <devdoc>
        ///    Add value to the collection 
        /// </devdoc> 
        public int Add(ToolStripItem value) {
            return List.Add(value); 
        }

        /// <include file='doc\CustomMenuItemCollection.uex' path='docs/doc[@for="CustomMenuItemCollection.AddRange"]/*' />
        /// <devdoc> 
        ///    Add range of values to the collection
        /// </devdoc> 
        public void AddRange(ToolStripItem[] value) { 
            for (int i = 0; (i < value.Length); i = (i + 1)) {
                this.Add(value[i]); 
            }
        }

        /// <devdoc> 
        ///    Abstract base class version for refreshing the items
        /// </devdoc> 
        public virtual void RefreshItems() 
        {
 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Windows.Forms; 

    /// <include file='doc\CustomMenuItemCollection.uex' path='docs/doc[@for="CustomMenuItemCollection"]/*' /> 
    /// <devdoc>
    /// A strongly-typed collection that stores ToolStripMenuItem objects for DesignerContextMenu
    /// </devdoc>
    internal class CustomMenuItemCollection : CollectionBase { 

        /// <include file='doc\CustomMenuItemCollection.uex' path='docs/doc[@for="CustomMenuItemCollection.CustomMenuItemCollection"]/*' /> 
        /// <devdoc> 
        ///   Constructor
        /// </devdoc> 
        public CustomMenuItemCollection() {
        }

        /// <include file='doc\CustomMenuItemCollection.uex' path='docs/doc[@for="CustomMenuItemCollection.Add"]/*' /> 
        /// <devdoc>
        ///    Add value to the collection 
        /// </devdoc> 
        public int Add(ToolStripItem value) {
            return List.Add(value); 
        }

        /// <include file='doc\CustomMenuItemCollection.uex' path='docs/doc[@for="CustomMenuItemCollection.AddRange"]/*' />
        /// <devdoc> 
        ///    Add range of values to the collection
        /// </devdoc> 
        public void AddRange(ToolStripItem[] value) { 
            for (int i = 0; (i < value.Length); i = (i + 1)) {
                this.Add(value[i]); 
            }
        }

        /// <devdoc> 
        ///    Abstract base class version for refreshing the items
        /// </devdoc> 
        public virtual void RefreshItems() 
        {
 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
