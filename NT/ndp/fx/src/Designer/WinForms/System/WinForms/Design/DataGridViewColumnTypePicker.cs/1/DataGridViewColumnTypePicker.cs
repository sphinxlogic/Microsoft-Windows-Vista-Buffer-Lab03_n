//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewColumnTypePicker.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.Design; 
    using System.Windows.Forms;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Collections; 
    using System.Drawing;
    using System.Diagnostics; 
    using System.Drawing.Design; 

    [ 
    ToolboxItem(false),
    DesignTimeVisible(false)
    ]
    internal class DataGridViewColumnTypePicker : ContainerControl { 

 
        ListBox typesListBox; 
        Type selectedType = null;
 
        private IWindowsFormsEditorService      edSvc;        // the current editor service that we need to close the drop down.
        private static Type dataGridViewColumnType = typeof(System.Windows.Forms.DataGridViewColumn);

        private const int                       MinimumHeight = 90; 
        private const int                       MinimumWidth  = 100;
 
        public DataGridViewColumnTypePicker() { 
            typesListBox = new ListBox();
            this.Size = typesListBox.Size; 
            this.typesListBox.Dock = DockStyle.Fill;
            this.typesListBox.Sorted = true;
            this.typesListBox.HorizontalScrollbar = true;
            this.typesListBox.SelectedIndexChanged += new EventHandler(typesListBox_SelectedIndexChanged); 
            this.Controls.Add(typesListBox);
            this.BackColor = SystemColors.Control; 
            this.ActiveControl = typesListBox; 

        } 

        public Type SelectedType
        {
            get 
            {
                return this.selectedType; 
            } 
        }
 
        private int PreferredWidth
        {
            get
            { 
                int width = 0;
                Graphics g = this.typesListBox.CreateGraphics(); 
                try { 
                    for (int i = 0; i < this.typesListBox.Items.Count; i ++)
                    { 
                        ListBoxItem item = (ListBoxItem) this.typesListBox.Items[i];
                        width = Math.Max(width, System.Drawing.Size.Ceiling(g.MeasureString(item.ToString(), this.typesListBox.Font)).Width);
                    }
                } finally { 
                    g.Dispose();
                } 
 
                return width;
            } 
        }

        private void CloseDropDown() {
            if (edSvc != null) { 
                edSvc.CloseDropDown();
            } 
        } 

        /* 
        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
            if (Visible) {
                TreeNode selectedNode = treeView.SelectedNode; 

                // ensure the selected node is visible 
                // 
                if (selectedNode != null) {
                    treeView.SelectedNode = null; 
                    treeView.SelectedNode = selectedNode;
                }
                treeView.AfterExpand += new TreeViewEventHandler(treeView_AfterExpand);
            } 
        }
        */ 
 
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified) {
 
            if ((BoundsSpecified.Width & specified) == BoundsSpecified.Width) {
                width = Math.Max(width, MinimumWidth);
            }
 
            if ((BoundsSpecified.Height & specified) == BoundsSpecified.Height) {
                height = Math.Max(height, MinimumHeight); 
            } 

            base.SetBoundsCore(x, y, width, height, specified); 
        }

        /// <devdoc>
        /// Setup the picker, and fill it with type information 
        /// </devdoc>
        public void Start(IWindowsFormsEditorService edSvc, ITypeDiscoveryService discoveryService, Type defaultType) { 
            this.edSvc = edSvc; 

            this.typesListBox.Items.Clear(); 

            ICollection columnTypes = DesignerUtils.FilterGenericTypes(discoveryService.GetTypes(dataGridViewColumnType, false /*excludeGlobalTypes*/));

            foreach (Type t in columnTypes) 
            {
                if (t == dataGridViewColumnType) 
                { 
                    continue;
                } 

                if (t.IsAbstract)
                {
                    continue; 
                }
 
                if (!t.IsPublic && !t.IsNestedPublic) 
                {
                    continue; 
                }

                DataGridViewColumnDesignTimeVisibleAttribute attr = TypeDescriptor.GetAttributes(t)[typeof(DataGridViewColumnDesignTimeVisibleAttribute)] as DataGridViewColumnDesignTimeVisibleAttribute;
                if (attr != null && !attr.Visible) 
                {
                    continue; 
                } 

                this.typesListBox.Items.Add(new ListBoxItem(t)); 
            }

            this.typesListBox.SelectedIndex = TypeToSelectedIndex(defaultType);
 
            this.selectedType = null;
 
            // set our default width. 
            //
            this.Width = Math.Max(this.Width, this.PreferredWidth + (SystemInformation.VerticalScrollBarWidth * 2)); 
        }

        private void typesListBox_SelectedIndexChanged(object sender, EventArgs e)
        { 
            this.selectedType = ((ListBoxItem) this.typesListBox.SelectedItem).ColumnType;
            this.edSvc.CloseDropDown(); 
        } 

        private int TypeToSelectedIndex(Type type) 
        {
            for (int i = 0; i < this.typesListBox.Items.Count; i ++)
            {
                if (type == ((ListBoxItem) this.typesListBox.Items[i]).ColumnType) 
                {
                    return i; 
                } 
            }
 
            Debug.Assert(false, "we should have found a type by now");

            return -1;
        } 

 
        private class ListBoxItem 
        {
            Type columnType; 
            public ListBoxItem(Type columnType)
            {
                this.columnType = columnType;
            } 

            public override string ToString() 
            { 
                return this.columnType.Name;
            } 

            public Type ColumnType
            {
                get 
                {
                    return this.columnType; 
                } 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewColumnTypePicker.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.Design; 
    using System.Windows.Forms;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Collections; 
    using System.Drawing;
    using System.Diagnostics; 
    using System.Drawing.Design; 

    [ 
    ToolboxItem(false),
    DesignTimeVisible(false)
    ]
    internal class DataGridViewColumnTypePicker : ContainerControl { 

 
        ListBox typesListBox; 
        Type selectedType = null;
 
        private IWindowsFormsEditorService      edSvc;        // the current editor service that we need to close the drop down.
        private static Type dataGridViewColumnType = typeof(System.Windows.Forms.DataGridViewColumn);

        private const int                       MinimumHeight = 90; 
        private const int                       MinimumWidth  = 100;
 
        public DataGridViewColumnTypePicker() { 
            typesListBox = new ListBox();
            this.Size = typesListBox.Size; 
            this.typesListBox.Dock = DockStyle.Fill;
            this.typesListBox.Sorted = true;
            this.typesListBox.HorizontalScrollbar = true;
            this.typesListBox.SelectedIndexChanged += new EventHandler(typesListBox_SelectedIndexChanged); 
            this.Controls.Add(typesListBox);
            this.BackColor = SystemColors.Control; 
            this.ActiveControl = typesListBox; 

        } 

        public Type SelectedType
        {
            get 
            {
                return this.selectedType; 
            } 
        }
 
        private int PreferredWidth
        {
            get
            { 
                int width = 0;
                Graphics g = this.typesListBox.CreateGraphics(); 
                try { 
                    for (int i = 0; i < this.typesListBox.Items.Count; i ++)
                    { 
                        ListBoxItem item = (ListBoxItem) this.typesListBox.Items[i];
                        width = Math.Max(width, System.Drawing.Size.Ceiling(g.MeasureString(item.ToString(), this.typesListBox.Font)).Width);
                    }
                } finally { 
                    g.Dispose();
                } 
 
                return width;
            } 
        }

        private void CloseDropDown() {
            if (edSvc != null) { 
                edSvc.CloseDropDown();
            } 
        } 

        /* 
        protected override void OnVisibleChanged(EventArgs e) {
            base.OnVisibleChanged(e);
            if (Visible) {
                TreeNode selectedNode = treeView.SelectedNode; 

                // ensure the selected node is visible 
                // 
                if (selectedNode != null) {
                    treeView.SelectedNode = null; 
                    treeView.SelectedNode = selectedNode;
                }
                treeView.AfterExpand += new TreeViewEventHandler(treeView_AfterExpand);
            } 
        }
        */ 
 
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified) {
 
            if ((BoundsSpecified.Width & specified) == BoundsSpecified.Width) {
                width = Math.Max(width, MinimumWidth);
            }
 
            if ((BoundsSpecified.Height & specified) == BoundsSpecified.Height) {
                height = Math.Max(height, MinimumHeight); 
            } 

            base.SetBoundsCore(x, y, width, height, specified); 
        }

        /// <devdoc>
        /// Setup the picker, and fill it with type information 
        /// </devdoc>
        public void Start(IWindowsFormsEditorService edSvc, ITypeDiscoveryService discoveryService, Type defaultType) { 
            this.edSvc = edSvc; 

            this.typesListBox.Items.Clear(); 

            ICollection columnTypes = DesignerUtils.FilterGenericTypes(discoveryService.GetTypes(dataGridViewColumnType, false /*excludeGlobalTypes*/));

            foreach (Type t in columnTypes) 
            {
                if (t == dataGridViewColumnType) 
                { 
                    continue;
                } 

                if (t.IsAbstract)
                {
                    continue; 
                }
 
                if (!t.IsPublic && !t.IsNestedPublic) 
                {
                    continue; 
                }

                DataGridViewColumnDesignTimeVisibleAttribute attr = TypeDescriptor.GetAttributes(t)[typeof(DataGridViewColumnDesignTimeVisibleAttribute)] as DataGridViewColumnDesignTimeVisibleAttribute;
                if (attr != null && !attr.Visible) 
                {
                    continue; 
                } 

                this.typesListBox.Items.Add(new ListBoxItem(t)); 
            }

            this.typesListBox.SelectedIndex = TypeToSelectedIndex(defaultType);
 
            this.selectedType = null;
 
            // set our default width. 
            //
            this.Width = Math.Max(this.Width, this.PreferredWidth + (SystemInformation.VerticalScrollBarWidth * 2)); 
        }

        private void typesListBox_SelectedIndexChanged(object sender, EventArgs e)
        { 
            this.selectedType = ((ListBoxItem) this.typesListBox.SelectedItem).ColumnType;
            this.edSvc.CloseDropDown(); 
        } 

        private int TypeToSelectedIndex(Type type) 
        {
            for (int i = 0; i < this.typesListBox.Items.Count; i ++)
            {
                if (type == ((ListBoxItem) this.typesListBox.Items[i]).ColumnType) 
                {
                    return i; 
                } 
            }
 
            Debug.Assert(false, "we should have found a type by now");

            return -1;
        } 

 
        private class ListBoxItem 
        {
            Type columnType; 
            public ListBoxItem(Type columnType)
            {
                this.columnType = columnType;
            } 

            public override string ToString() 
            { 
                return this.columnType.Name;
            } 

            public Type ColumnType
            {
                get 
                {
                    return this.columnType; 
                } 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
