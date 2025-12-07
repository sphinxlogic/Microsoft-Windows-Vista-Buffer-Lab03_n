//------------------------------------------------------------------------------ 
// <copyright file="ContextMenuStripGroup.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections.Generic; 
    using System.Text;
    using System.Windows.Forms; 

    /// Collection of toolStripItems which can be added as a group to the contextMenuStrip
    /// which is shown by control designer.
    /// 
    internal class ContextMenuStripGroup {
        private List<ToolStripItem> items; 
        private string name; 

        public ContextMenuStripGroup(string name) { 
            this.name = name;
        }

        public List<ToolStripItem> Items { 
            get {
                if (items == null) { 
                    items = new List<ToolStripItem>(); 
                }
                return items; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ContextMenuStripGroup.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections.Generic; 
    using System.Text;
    using System.Windows.Forms; 

    /// Collection of toolStripItems which can be added as a group to the contextMenuStrip
    /// which is shown by control designer.
    /// 
    internal class ContextMenuStripGroup {
        private List<ToolStripItem> items; 
        private string name; 

        public ContextMenuStripGroup(string name) { 
            this.name = name;
        }

        public List<ToolStripItem> Items { 
            get {
                if (items == null) { 
                    items = new List<ToolStripItem>(); 
                }
                return items; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
