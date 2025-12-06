//------------------------------------------------------------------------------ 
// <copyright file="GroupedContextMenuStrip.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections.Generic;
    using System.Text;
    using System.Collections.Specialized;
    using System.Windows.Forms; 
    using System.ComponentModel;
 
    /* 
    This is the Custom ContextMenu thathas the notion of groups and groupOrdering.
    The contrextmenu is divided into groups and the ordering is governed by the groupOrdering. 
    The Groups contain individual menuItems.
    */
    internal class GroupedContextMenuStrip : ContextMenuStrip {
        private StringCollection groupOrdering; 
        private ContextMenuStripGroupCollection groups;
        private bool populated = false; 
 
        public bool Populated
        { 
            set
            {
                populated = value;
            } 
        }
 
        public GroupedContextMenuStrip() { 
        }
 
        public ContextMenuStripGroupCollection Groups {
            get {
                if (groups == null) {
                    groups = new ContextMenuStripGroupCollection(); 
                }
                return groups; 
            } 
        }
        public StringCollection GroupOrdering { 
            get {
                if (groupOrdering == null) {
                    groupOrdering = new StringCollection();
                } 
                return groupOrdering;
            } 
        } 

        // merges all the items which are currently in the groups into the items collection. 
        public void Populate() {
            this.Items.Clear();
            foreach (string groupName in GroupOrdering) {
                if (groups.ContainsKey(groupName)) { 
                    List<ToolStripItem> items = groups[groupName].Items;
 
                    if (Items.Count > 0 && items.Count > 0) { 
                        this.Items.Add(new ToolStripSeparator());
                    } 
                    foreach (ToolStripItem item in items) {
                        this.Items.Add(item);
                    }
                } 
            }
            populated = true; 
        } 

        protected override void OnOpening(CancelEventArgs e) { 
            SuspendLayout();

            if (!populated) {
               Populate(); 
            }
            RefreshItems(); 
 
            ResumeLayout(true);
            PerformLayout(); 
            e.Cancel = (Items.Count == 0);
            base.OnOpening(e);

        } 

        public virtual void RefreshItems() 
        { 

        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="GroupedContextMenuStrip.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections.Generic;
    using System.Text;
    using System.Collections.Specialized;
    using System.Windows.Forms; 
    using System.ComponentModel;
 
    /* 
    This is the Custom ContextMenu thathas the notion of groups and groupOrdering.
    The contrextmenu is divided into groups and the ordering is governed by the groupOrdering. 
    The Groups contain individual menuItems.
    */
    internal class GroupedContextMenuStrip : ContextMenuStrip {
        private StringCollection groupOrdering; 
        private ContextMenuStripGroupCollection groups;
        private bool populated = false; 
 
        public bool Populated
        { 
            set
            {
                populated = value;
            } 
        }
 
        public GroupedContextMenuStrip() { 
        }
 
        public ContextMenuStripGroupCollection Groups {
            get {
                if (groups == null) {
                    groups = new ContextMenuStripGroupCollection(); 
                }
                return groups; 
            } 
        }
        public StringCollection GroupOrdering { 
            get {
                if (groupOrdering == null) {
                    groupOrdering = new StringCollection();
                } 
                return groupOrdering;
            } 
        } 

        // merges all the items which are currently in the groups into the items collection. 
        public void Populate() {
            this.Items.Clear();
            foreach (string groupName in GroupOrdering) {
                if (groups.ContainsKey(groupName)) { 
                    List<ToolStripItem> items = groups[groupName].Items;
 
                    if (Items.Count > 0 && items.Count > 0) { 
                        this.Items.Add(new ToolStripSeparator());
                    } 
                    foreach (ToolStripItem item in items) {
                        this.Items.Add(item);
                    }
                } 
            }
            populated = true; 
        } 

        protected override void OnOpening(CancelEventArgs e) { 
            SuspendLayout();

            if (!populated) {
               Populate(); 
            }
            RefreshItems(); 
 
            ResumeLayout(true);
            PerformLayout(); 
            e.Cancel = (Items.Count == 0);
            base.OnOpening(e);

        } 

        public virtual void RefreshItems() 
        { 

        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
