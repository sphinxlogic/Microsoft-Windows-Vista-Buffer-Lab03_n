//------------------------------------------------------------------------------ 
// <copyright file="DataGridGeneralPage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.ListControls { 
 
    using System;
    using System.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Web.UI; 
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    using WebControls = System.Web.UI.WebControls;
    using DataBinding = System.Web.UI.DataBinding;
    using DataGrid = System.Web.UI.WebControls.DataGrid; 

    using CheckBox = System.Windows.Forms.CheckBox; 
    using Control = System.Windows.Forms.Control; 
    using Label = System.Windows.Forms.Label;
    using PropertyDescriptor = System.ComponentModel.PropertyDescriptor; 
    using System.Globalization;

    /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage"]/*' />
    /// <devdoc> 
    ///   The General page for the DataGrid control.
    /// </devdoc> 
    /// <internalonly/> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class DataGridGeneralPage : BaseDataListPage { 

        private CheckBox showHeaderCheck;
        private CheckBox showFooterCheck;
        private CheckBox allowSortingCheck; 

        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.HelpKeyword"]/*' /> 
        protected override string HelpKeyword { 
            get {
                return "net.Asp.DataGridProperties.General"; 
            }
        }

        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.InitForm"]/*' /> 
        /// <devdoc>
        ///   Initializes the UI of the form. 
        /// </devdoc> 
        private void InitForm() {
            GroupLabel headerFooterGroup = new GroupLabel(); 
            this.showHeaderCheck = new CheckBox();
            this.showFooterCheck = new CheckBox();
            GroupLabel behaviorGroup = new GroupLabel();
            this.allowSortingCheck = new CheckBox(); 

            headerFooterGroup.SetBounds(4, 4, 431, 16); 
            headerFooterGroup.Text = SR.GetString(SR.DGGen_HeaderFooterGroup); 
            headerFooterGroup.TabIndex = 8;
            headerFooterGroup.TabStop = false; 

            showHeaderCheck.SetBounds(12, 24, 160, 16);
            showHeaderCheck.TabIndex = 9;
            showHeaderCheck.Text = SR.GetString(SR.DGGen_ShowHeader); 
            showHeaderCheck.TextAlign = ContentAlignment.MiddleLeft;
            showHeaderCheck.FlatStyle = FlatStyle.System; 
            showHeaderCheck.CheckedChanged += new EventHandler(this.OnCheckChangedShowHeader); 

            showFooterCheck.SetBounds(12, 44, 160, 16); 
            showFooterCheck.TabIndex = 10;
            showFooterCheck.Text = SR.GetString(SR.DGGen_ShowFooter);
            showFooterCheck.TextAlign = ContentAlignment.MiddleLeft;
            showFooterCheck.FlatStyle = FlatStyle.System; 
            showFooterCheck.CheckedChanged += new EventHandler(this.OnCheckChangedShowFooter);
 
            behaviorGroup.SetBounds(4, 70, 431, 16); 
            behaviorGroup.Text = SR.GetString(SR.DGGen_BehaviorGroup);
            behaviorGroup.TabIndex = 11; 
            behaviorGroup.TabStop = false;

            allowSortingCheck.SetBounds(12, 88, 160, 16);
            allowSortingCheck.Text = SR.GetString(SR.DGGen_AllowSorting); 
            allowSortingCheck.TabIndex = 12;
            allowSortingCheck.TextAlign = ContentAlignment.MiddleLeft; 
            allowSortingCheck.FlatStyle = FlatStyle.System; 
            allowSortingCheck.CheckedChanged += new EventHandler(this.OnCheckChangedAllowSorting);
 
            this.Text = SR.GetString(SR.DGGen_Text);
            this.AccessibleDescription = SR.GetString(SR.DGGen_Desc);
            this.Size = new Size(464, 272);
            this.CommitOnDeactivate = true; 
            this.Icon = new Icon(this.GetType(), "DataGridGeneralPage.ico");
 
            Controls.Clear(); 
            Controls.AddRange(new Control[] {
                               allowSortingCheck, 
                               behaviorGroup,
                               showFooterCheck,
                               showHeaderCheck,
                               headerFooterGroup 
                           });
        } 
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.InitPage"]/*' />
        /// <devdoc> 
        ///   Initializes the page before it can be loaded with the component.
        /// </devdoc>
        private void InitPage() {
            showHeaderCheck.Checked = false; 
            showFooterCheck.Checked = false;
            allowSortingCheck.Checked = false; 
        } 

        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.LoadComponent"]/*' /> 
        /// <devdoc>
        ///   Loads the component into the page.
        /// </devdoc>
        protected override void LoadComponent() { 
            InitPage();
 
            DataGrid dataGrid = (DataGrid)GetBaseControl(); 

            showHeaderCheck.Checked = dataGrid.ShowHeader; 
            showFooterCheck.Checked = dataGrid.ShowFooter;
            allowSortingCheck.Checked = dataGrid.AllowSorting;
        }
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.OnCheckChangedShowHeader"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        private void OnCheckChangedShowHeader(object source, EventArgs e) {
            if (IsLoading()) 
                return;
            SetDirty();
        }
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.OnCheckChangedShowFooter"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        private void OnCheckChangedShowFooter(object source, EventArgs e) {
            if (IsLoading()) 
                return;
            SetDirty();
        }
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.OnCheckChangedAllowSorting"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        private void OnCheckChangedAllowSorting(object source, EventArgs e) {
            if (IsLoading()) 
                return;
            SetDirty();
        }
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.SaveComponent"]/*' />
        /// <devdoc> 
        ///   Saves the component loaded into the page. 
        /// </devdoc>
        protected override void SaveComponent() { 
            DataGrid dataGrid = (DataGrid)GetBaseControl();

            dataGrid.ShowHeader = showHeaderCheck.Checked;
            dataGrid.ShowFooter = showFooterCheck.Checked; 
            dataGrid.AllowSorting = allowSortingCheck.Checked;
        } 
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.SetComponent"]/*' />
        /// <devdoc> 
        ///   Sets the component that is to be edited in the page.
        /// </devdoc>
        public override void SetComponent(IComponent component) {
            base.SetComponent(component); 
            InitForm();
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataGridGeneralPage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.ListControls { 
 
    using System;
    using System.Design; 
    using System.Collections;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Web.UI; 
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 

    using WebControls = System.Web.UI.WebControls;
    using DataBinding = System.Web.UI.DataBinding;
    using DataGrid = System.Web.UI.WebControls.DataGrid; 

    using CheckBox = System.Windows.Forms.CheckBox; 
    using Control = System.Windows.Forms.Control; 
    using Label = System.Windows.Forms.Label;
    using PropertyDescriptor = System.ComponentModel.PropertyDescriptor; 
    using System.Globalization;

    /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage"]/*' />
    /// <devdoc> 
    ///   The General page for the DataGrid control.
    /// </devdoc> 
    /// <internalonly/> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class DataGridGeneralPage : BaseDataListPage { 

        private CheckBox showHeaderCheck;
        private CheckBox showFooterCheck;
        private CheckBox allowSortingCheck; 

        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.HelpKeyword"]/*' /> 
        protected override string HelpKeyword { 
            get {
                return "net.Asp.DataGridProperties.General"; 
            }
        }

        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.InitForm"]/*' /> 
        /// <devdoc>
        ///   Initializes the UI of the form. 
        /// </devdoc> 
        private void InitForm() {
            GroupLabel headerFooterGroup = new GroupLabel(); 
            this.showHeaderCheck = new CheckBox();
            this.showFooterCheck = new CheckBox();
            GroupLabel behaviorGroup = new GroupLabel();
            this.allowSortingCheck = new CheckBox(); 

            headerFooterGroup.SetBounds(4, 4, 431, 16); 
            headerFooterGroup.Text = SR.GetString(SR.DGGen_HeaderFooterGroup); 
            headerFooterGroup.TabIndex = 8;
            headerFooterGroup.TabStop = false; 

            showHeaderCheck.SetBounds(12, 24, 160, 16);
            showHeaderCheck.TabIndex = 9;
            showHeaderCheck.Text = SR.GetString(SR.DGGen_ShowHeader); 
            showHeaderCheck.TextAlign = ContentAlignment.MiddleLeft;
            showHeaderCheck.FlatStyle = FlatStyle.System; 
            showHeaderCheck.CheckedChanged += new EventHandler(this.OnCheckChangedShowHeader); 

            showFooterCheck.SetBounds(12, 44, 160, 16); 
            showFooterCheck.TabIndex = 10;
            showFooterCheck.Text = SR.GetString(SR.DGGen_ShowFooter);
            showFooterCheck.TextAlign = ContentAlignment.MiddleLeft;
            showFooterCheck.FlatStyle = FlatStyle.System; 
            showFooterCheck.CheckedChanged += new EventHandler(this.OnCheckChangedShowFooter);
 
            behaviorGroup.SetBounds(4, 70, 431, 16); 
            behaviorGroup.Text = SR.GetString(SR.DGGen_BehaviorGroup);
            behaviorGroup.TabIndex = 11; 
            behaviorGroup.TabStop = false;

            allowSortingCheck.SetBounds(12, 88, 160, 16);
            allowSortingCheck.Text = SR.GetString(SR.DGGen_AllowSorting); 
            allowSortingCheck.TabIndex = 12;
            allowSortingCheck.TextAlign = ContentAlignment.MiddleLeft; 
            allowSortingCheck.FlatStyle = FlatStyle.System; 
            allowSortingCheck.CheckedChanged += new EventHandler(this.OnCheckChangedAllowSorting);
 
            this.Text = SR.GetString(SR.DGGen_Text);
            this.AccessibleDescription = SR.GetString(SR.DGGen_Desc);
            this.Size = new Size(464, 272);
            this.CommitOnDeactivate = true; 
            this.Icon = new Icon(this.GetType(), "DataGridGeneralPage.ico");
 
            Controls.Clear(); 
            Controls.AddRange(new Control[] {
                               allowSortingCheck, 
                               behaviorGroup,
                               showFooterCheck,
                               showHeaderCheck,
                               headerFooterGroup 
                           });
        } 
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.InitPage"]/*' />
        /// <devdoc> 
        ///   Initializes the page before it can be loaded with the component.
        /// </devdoc>
        private void InitPage() {
            showHeaderCheck.Checked = false; 
            showFooterCheck.Checked = false;
            allowSortingCheck.Checked = false; 
        } 

        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.LoadComponent"]/*' /> 
        /// <devdoc>
        ///   Loads the component into the page.
        /// </devdoc>
        protected override void LoadComponent() { 
            InitPage();
 
            DataGrid dataGrid = (DataGrid)GetBaseControl(); 

            showHeaderCheck.Checked = dataGrid.ShowHeader; 
            showFooterCheck.Checked = dataGrid.ShowFooter;
            allowSortingCheck.Checked = dataGrid.AllowSorting;
        }
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.OnCheckChangedShowHeader"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        private void OnCheckChangedShowHeader(object source, EventArgs e) {
            if (IsLoading()) 
                return;
            SetDirty();
        }
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.OnCheckChangedShowFooter"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        private void OnCheckChangedShowFooter(object source, EventArgs e) {
            if (IsLoading()) 
                return;
            SetDirty();
        }
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.OnCheckChangedAllowSorting"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        private void OnCheckChangedAllowSorting(object source, EventArgs e) {
            if (IsLoading()) 
                return;
            SetDirty();
        }
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.SaveComponent"]/*' />
        /// <devdoc> 
        ///   Saves the component loaded into the page. 
        /// </devdoc>
        protected override void SaveComponent() { 
            DataGrid dataGrid = (DataGrid)GetBaseControl();

            dataGrid.ShowHeader = showHeaderCheck.Checked;
            dataGrid.ShowFooter = showFooterCheck.Checked; 
            dataGrid.AllowSorting = allowSortingCheck.Checked;
        } 
 
        /// <include file='doc\DataGridGeneralPage.uex' path='docs/doc[@for="DataGridGeneralPage.SetComponent"]/*' />
        /// <devdoc> 
        ///   Sets the component that is to be edited in the page.
        /// </devdoc>
        public override void SetComponent(IComponent component) {
            base.SetComponent(component); 
            InitForm();
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
