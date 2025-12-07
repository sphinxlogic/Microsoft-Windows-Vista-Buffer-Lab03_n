//------------------------------------------------------------------------------ 
// <copyright file="DataListGeneralPage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.ListControls { 
 
    using System;
    using System.Design; 
    using System.Collections;
    using System.Globalization;
    using System.ComponentModel;
    using System.Data; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Web.UI; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;

    using DataBinding = System.Web.UI.DataBinding;
    using DataList = System.Web.UI.WebControls.DataList; 

    using CheckBox = System.Windows.Forms.CheckBox; 
    using Control = System.Windows.Forms.Control; 
    using Label = System.Windows.Forms.Label;
    using PropertyDescriptor = System.ComponentModel.PropertyDescriptor; 

    /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage"]/*' />
    /// <devdoc>
    ///   The General page for the DataList control. 
    /// </devdoc>
    /// <internalonly/> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    internal sealed class DataListGeneralPage : BaseDataListPage {
 
        private const int IDX_DIR_HORIZONTAL = 0;
        private const int IDX_DIR_VERTICAL = 1;

        private const int IDX_MODE_TABLE = 0; 
        private const int IDX_MODE_FLOW = 1;
 
        private CheckBox showHeaderCheck; 
        private CheckBox showFooterCheck;
        private NumberEdit repeatColumnsEdit; 
        private ComboBox repeatDirectionCombo;
        private ComboBox repeatLayoutCombo;
        private CheckBox extractRowsCheck;
 
        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.HelpKeyword"]/*' />
        protected override string HelpKeyword { 
            get { 
                return "net.Asp.DataListProperties.General";
            } 
        }

        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.InitForm"]/*' />
        /// <devdoc> 
        ///   Initializes the UI of the form.
        /// </devdoc> 
        private void InitForm() { 
            GroupLabel headerFooterGroup = new GroupLabel();
            this.showHeaderCheck = new CheckBox(); 
            this.showFooterCheck = new CheckBox();
            GroupLabel repeatGroup = new GroupLabel();
            Label repeatColumnsLabel = new Label();
            this.repeatColumnsEdit = new NumberEdit(); 
            Label repeatDirectionLabel = new Label();
            this.repeatDirectionCombo = new ComboBox(); 
            Label repeatLayoutLabel = new Label(); 
            this.repeatLayoutCombo = new ComboBox();
            GroupLabel templatesGroup = new GroupLabel(); 
            this.extractRowsCheck = new CheckBox();

            headerFooterGroup.SetBounds(4, 4, 360, 16);
            headerFooterGroup.Text = SR.GetString(SR.DLGen_HeaderFooterGroup); 
            headerFooterGroup.TabIndex = 7;
            headerFooterGroup.TabStop = false; 
 
            showHeaderCheck.SetBounds(8, 24, 170, 16);
            showHeaderCheck.TabIndex = 8; 
            showHeaderCheck.Text = SR.GetString(SR.DLGen_ShowHeader);
            showHeaderCheck.TextAlign = ContentAlignment.MiddleLeft;
            showHeaderCheck.FlatStyle = FlatStyle.System;
            showHeaderCheck.CheckedChanged += new EventHandler(this.OnCheckChangedShowHeader); 

            showFooterCheck.SetBounds(8, 42, 170, 16); 
            showFooterCheck.TabIndex = 9; 
            showFooterCheck.Text = SR.GetString(SR.DLGen_ShowFooter);
            showFooterCheck.TextAlign = ContentAlignment.MiddleLeft; 
            showFooterCheck.FlatStyle = FlatStyle.System;
            showFooterCheck.CheckedChanged += new EventHandler(this.OnCheckChangedShowFooter);

            repeatGroup.SetBounds(4, 68, 360, 16); 
            repeatGroup.Text = SR.GetString(SR.DLGen_RepeatLayoutGroup);
            repeatGroup.TabIndex = 10; 
            repeatGroup.TabStop = false; 

            repeatColumnsLabel.SetBounds(8, 88, 106, 16); 
            repeatColumnsLabel.Text = SR.GetString(SR.DLGen_RepeatColumns);
            repeatColumnsLabel.TabStop = false;
            repeatColumnsLabel.TabIndex = 11;
 
            repeatColumnsEdit.SetBounds(112, 84, 40, 21);
            repeatColumnsEdit.AllowDecimal = false; 
            repeatColumnsEdit.AllowNegative = false; 
            repeatColumnsEdit.TabIndex = 12;
            repeatColumnsEdit.TextChanged += new EventHandler(this.OnChangedRepeatProps); 

            repeatDirectionLabel.SetBounds(8, 113, 106, 16);
            repeatDirectionLabel.Text = SR.GetString(SR.DLGen_RepeatDirection);
            repeatDirectionLabel.TabStop = false; 
            repeatDirectionLabel.TabIndex = 13;
 
            repeatDirectionCombo.SetBounds(112, 109, 140, 56); 
            repeatDirectionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            repeatDirectionCombo.Items.AddRange(new object[] { 
                                                 SR.GetString(SR.DLGen_RD_Horz),
                                                 SR.GetString(SR.DLGen_RD_Vert)
                                             });
            repeatDirectionCombo.TabIndex = 14; 
            repeatDirectionCombo.SelectedIndexChanged += new EventHandler(this.OnChangedRepeatProps);
 
            repeatLayoutLabel.SetBounds(8, 138, 106, 16); 
            repeatLayoutLabel.Text = SR.GetString(SR.DLGen_RepeatLayout);
            repeatLayoutLabel.TabStop = false; 
            repeatLayoutLabel.TabIndex = 15;

            repeatLayoutCombo.SetBounds(112, 134, 140, 21);
            repeatLayoutCombo.DropDownStyle = ComboBoxStyle.DropDownList; 
            repeatLayoutCombo.Items.AddRange(new object[] {
                                              SR.GetString(SR.DLGen_RL_Table), 
                                              SR.GetString(SR.DLGen_RL_Flow) 
                                          });
            repeatLayoutCombo.TabIndex = 16; 
            repeatLayoutCombo.SelectedIndexChanged += new EventHandler(this.OnChangedRepeatProps);

            templatesGroup.SetBounds(4, 162, 360, 16);
            templatesGroup.Text = SR.GetString(SR.DLGen_Templates); 
            templatesGroup.TabIndex = 17;
            templatesGroup.TabStop = false; 
            templatesGroup.Visible = false; 

            extractRowsCheck.SetBounds(8, 182, 260, 16); 
            extractRowsCheck.Text = SR.GetString(SR.DLGen_ExtractRows);
            extractRowsCheck.TabIndex = 18;
            extractRowsCheck.Visible = false;
            extractRowsCheck.FlatStyle = FlatStyle.System; 
            extractRowsCheck.CheckedChanged += new EventHandler(this.OnCheckChangedExtractRows);
 
            this.Text = SR.GetString(SR.DLGen_Text); 
            this.AccessibleDescription = SR.GetString(SR.DLGen_Desc);
            this.Size = new Size(368, 280); 
            this.CommitOnDeactivate = true;
            this.Icon = new Icon(this.GetType(), "DataListGeneralPage.ico");

            Controls.Clear(); 
            Controls.AddRange(new Control[] {
                               extractRowsCheck, 
                               templatesGroup, 
                               repeatLayoutCombo,
                               repeatLayoutLabel, 
                               repeatDirectionCombo,
                               repeatDirectionLabel,
                               repeatColumnsEdit,
                               repeatColumnsLabel, 
                               repeatGroup,
                               showFooterCheck, 
                               showHeaderCheck, 
                               headerFooterGroup
                           }); 
        }

        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.InitPage"]/*' />
        /// <devdoc> 
        ///   Initializes the page before it can be loaded with the component.
        /// </devdoc> 
        private void InitPage() { 
            showHeaderCheck.Checked = false;
            showFooterCheck.Checked = false; 

            repeatColumnsEdit.Clear();
            repeatDirectionCombo.SelectedIndex = -1;
            repeatLayoutCombo.SelectedIndex = -1; 

            extractRowsCheck.Checked = false; 
        } 

        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.LoadComponent"]/*' /> 
        /// <devdoc>
        ///   Loads the component into the page.
        /// </devdoc>
        protected override void LoadComponent() { 
            InitPage();
 
            DataList dataList = (DataList)GetBaseControl(); 

            showHeaderCheck.Checked = dataList.ShowHeader; 
            showFooterCheck.Checked = dataList.ShowFooter;

            repeatColumnsEdit.Text = (dataList.RepeatColumns).ToString(NumberFormatInfo.CurrentInfo);
 
            switch (dataList.RepeatDirection) {
                case RepeatDirection.Horizontal: 
                    repeatDirectionCombo.SelectedIndex = IDX_DIR_HORIZONTAL; 
                    break;
                case RepeatDirection.Vertical: 
                    repeatDirectionCombo.SelectedIndex = IDX_DIR_VERTICAL;
                    break;
            }
 
            switch (dataList.RepeatLayout) {
                case RepeatLayout.Table: 
                    repeatLayoutCombo.SelectedIndex = IDX_MODE_TABLE; 
                    break;
                case RepeatLayout.Flow: 
                    repeatLayoutCombo.SelectedIndex = IDX_MODE_FLOW;
                    break;
            }
 
            extractRowsCheck.Checked = dataList.ExtractTemplateRows;
        } 
 
        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.OnCheckChangedExtractRows"]/*' />
        /// <devdoc> 
        ///   Handles changes to the extract rows checkbox
        /// </devdoc>
        private void OnCheckChangedExtractRows(object source, EventArgs e) {
            if (IsLoading()) 
                return;
            SetDirty(); 
        } 

        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.OnChangedRepeatProps"]/*' /> 
        /// <devdoc>
        ///   Handles changes to the different repeater properties
        /// </devdoc>
        private void OnChangedRepeatProps(object source, EventArgs e) { 
            if (IsLoading())
                return; 
            SetDirty(); 
        }
 
        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.OnCheckChangedShowHeader"]/*' />
        /// <devdoc>
        /// </devdoc>
        private void OnCheckChangedShowHeader(object source, EventArgs e) { 
            if (IsLoading())
                return; 
            SetDirty(); 
        }
 
        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.OnCheckChangedShowFooter"]/*' />
        /// <devdoc>
        /// </devdoc>
        private void OnCheckChangedShowFooter(object source, EventArgs e) { 
            if (IsLoading())
                return; 
            SetDirty(); 
        }
 
        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.SaveComponent"]/*' />
        /// <devdoc>
        ///   Saves the component loaded into the page.
        /// </devdoc> 
        protected override void SaveComponent() {
            DataList dataList = (DataList)GetBaseControl(); 
 
            dataList.ShowHeader = showHeaderCheck.Checked;
            dataList.ShowFooter = showFooterCheck.Checked; 

            string repeatColumnsValue = repeatColumnsEdit.Text.Trim();
            if (repeatColumnsValue.Length != 0) {
                try { 
                    dataList.RepeatColumns = Int32.Parse(repeatColumnsValue, CultureInfo.CurrentCulture);
                } 
                catch { 
                    repeatColumnsEdit.Text = (dataList.RepeatColumns).ToString(CultureInfo.CurrentCulture);
                } 
            }

            switch (repeatDirectionCombo.SelectedIndex) {
                case IDX_DIR_HORIZONTAL: 
                    dataList.RepeatDirection = RepeatDirection.Horizontal;
                    break; 
                case IDX_DIR_VERTICAL: 
                    dataList.RepeatDirection = RepeatDirection.Vertical;
                    break; 
            }

            switch (repeatLayoutCombo.SelectedIndex) {
                case IDX_MODE_TABLE: 
                    dataList.RepeatLayout = RepeatLayout.Table;
                    break; 
                case IDX_MODE_FLOW: 
                    dataList.RepeatLayout = RepeatLayout.Flow;
                    break; 
            }

            dataList.ExtractTemplateRows = extractRowsCheck.Checked;
        } 

        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.SetComponent"]/*' /> 
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
// <copyright file="DataListGeneralPage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.ListControls { 
 
    using System;
    using System.Design; 
    using System.Collections;
    using System.Globalization;
    using System.ComponentModel;
    using System.Data; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Web.UI; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;

    using DataBinding = System.Web.UI.DataBinding;
    using DataList = System.Web.UI.WebControls.DataList; 

    using CheckBox = System.Windows.Forms.CheckBox; 
    using Control = System.Windows.Forms.Control; 
    using Label = System.Windows.Forms.Label;
    using PropertyDescriptor = System.ComponentModel.PropertyDescriptor; 

    /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage"]/*' />
    /// <devdoc>
    ///   The General page for the DataList control. 
    /// </devdoc>
    /// <internalonly/> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    internal sealed class DataListGeneralPage : BaseDataListPage {
 
        private const int IDX_DIR_HORIZONTAL = 0;
        private const int IDX_DIR_VERTICAL = 1;

        private const int IDX_MODE_TABLE = 0; 
        private const int IDX_MODE_FLOW = 1;
 
        private CheckBox showHeaderCheck; 
        private CheckBox showFooterCheck;
        private NumberEdit repeatColumnsEdit; 
        private ComboBox repeatDirectionCombo;
        private ComboBox repeatLayoutCombo;
        private CheckBox extractRowsCheck;
 
        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.HelpKeyword"]/*' />
        protected override string HelpKeyword { 
            get { 
                return "net.Asp.DataListProperties.General";
            } 
        }

        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.InitForm"]/*' />
        /// <devdoc> 
        ///   Initializes the UI of the form.
        /// </devdoc> 
        private void InitForm() { 
            GroupLabel headerFooterGroup = new GroupLabel();
            this.showHeaderCheck = new CheckBox(); 
            this.showFooterCheck = new CheckBox();
            GroupLabel repeatGroup = new GroupLabel();
            Label repeatColumnsLabel = new Label();
            this.repeatColumnsEdit = new NumberEdit(); 
            Label repeatDirectionLabel = new Label();
            this.repeatDirectionCombo = new ComboBox(); 
            Label repeatLayoutLabel = new Label(); 
            this.repeatLayoutCombo = new ComboBox();
            GroupLabel templatesGroup = new GroupLabel(); 
            this.extractRowsCheck = new CheckBox();

            headerFooterGroup.SetBounds(4, 4, 360, 16);
            headerFooterGroup.Text = SR.GetString(SR.DLGen_HeaderFooterGroup); 
            headerFooterGroup.TabIndex = 7;
            headerFooterGroup.TabStop = false; 
 
            showHeaderCheck.SetBounds(8, 24, 170, 16);
            showHeaderCheck.TabIndex = 8; 
            showHeaderCheck.Text = SR.GetString(SR.DLGen_ShowHeader);
            showHeaderCheck.TextAlign = ContentAlignment.MiddleLeft;
            showHeaderCheck.FlatStyle = FlatStyle.System;
            showHeaderCheck.CheckedChanged += new EventHandler(this.OnCheckChangedShowHeader); 

            showFooterCheck.SetBounds(8, 42, 170, 16); 
            showFooterCheck.TabIndex = 9; 
            showFooterCheck.Text = SR.GetString(SR.DLGen_ShowFooter);
            showFooterCheck.TextAlign = ContentAlignment.MiddleLeft; 
            showFooterCheck.FlatStyle = FlatStyle.System;
            showFooterCheck.CheckedChanged += new EventHandler(this.OnCheckChangedShowFooter);

            repeatGroup.SetBounds(4, 68, 360, 16); 
            repeatGroup.Text = SR.GetString(SR.DLGen_RepeatLayoutGroup);
            repeatGroup.TabIndex = 10; 
            repeatGroup.TabStop = false; 

            repeatColumnsLabel.SetBounds(8, 88, 106, 16); 
            repeatColumnsLabel.Text = SR.GetString(SR.DLGen_RepeatColumns);
            repeatColumnsLabel.TabStop = false;
            repeatColumnsLabel.TabIndex = 11;
 
            repeatColumnsEdit.SetBounds(112, 84, 40, 21);
            repeatColumnsEdit.AllowDecimal = false; 
            repeatColumnsEdit.AllowNegative = false; 
            repeatColumnsEdit.TabIndex = 12;
            repeatColumnsEdit.TextChanged += new EventHandler(this.OnChangedRepeatProps); 

            repeatDirectionLabel.SetBounds(8, 113, 106, 16);
            repeatDirectionLabel.Text = SR.GetString(SR.DLGen_RepeatDirection);
            repeatDirectionLabel.TabStop = false; 
            repeatDirectionLabel.TabIndex = 13;
 
            repeatDirectionCombo.SetBounds(112, 109, 140, 56); 
            repeatDirectionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            repeatDirectionCombo.Items.AddRange(new object[] { 
                                                 SR.GetString(SR.DLGen_RD_Horz),
                                                 SR.GetString(SR.DLGen_RD_Vert)
                                             });
            repeatDirectionCombo.TabIndex = 14; 
            repeatDirectionCombo.SelectedIndexChanged += new EventHandler(this.OnChangedRepeatProps);
 
            repeatLayoutLabel.SetBounds(8, 138, 106, 16); 
            repeatLayoutLabel.Text = SR.GetString(SR.DLGen_RepeatLayout);
            repeatLayoutLabel.TabStop = false; 
            repeatLayoutLabel.TabIndex = 15;

            repeatLayoutCombo.SetBounds(112, 134, 140, 21);
            repeatLayoutCombo.DropDownStyle = ComboBoxStyle.DropDownList; 
            repeatLayoutCombo.Items.AddRange(new object[] {
                                              SR.GetString(SR.DLGen_RL_Table), 
                                              SR.GetString(SR.DLGen_RL_Flow) 
                                          });
            repeatLayoutCombo.TabIndex = 16; 
            repeatLayoutCombo.SelectedIndexChanged += new EventHandler(this.OnChangedRepeatProps);

            templatesGroup.SetBounds(4, 162, 360, 16);
            templatesGroup.Text = SR.GetString(SR.DLGen_Templates); 
            templatesGroup.TabIndex = 17;
            templatesGroup.TabStop = false; 
            templatesGroup.Visible = false; 

            extractRowsCheck.SetBounds(8, 182, 260, 16); 
            extractRowsCheck.Text = SR.GetString(SR.DLGen_ExtractRows);
            extractRowsCheck.TabIndex = 18;
            extractRowsCheck.Visible = false;
            extractRowsCheck.FlatStyle = FlatStyle.System; 
            extractRowsCheck.CheckedChanged += new EventHandler(this.OnCheckChangedExtractRows);
 
            this.Text = SR.GetString(SR.DLGen_Text); 
            this.AccessibleDescription = SR.GetString(SR.DLGen_Desc);
            this.Size = new Size(368, 280); 
            this.CommitOnDeactivate = true;
            this.Icon = new Icon(this.GetType(), "DataListGeneralPage.ico");

            Controls.Clear(); 
            Controls.AddRange(new Control[] {
                               extractRowsCheck, 
                               templatesGroup, 
                               repeatLayoutCombo,
                               repeatLayoutLabel, 
                               repeatDirectionCombo,
                               repeatDirectionLabel,
                               repeatColumnsEdit,
                               repeatColumnsLabel, 
                               repeatGroup,
                               showFooterCheck, 
                               showHeaderCheck, 
                               headerFooterGroup
                           }); 
        }

        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.InitPage"]/*' />
        /// <devdoc> 
        ///   Initializes the page before it can be loaded with the component.
        /// </devdoc> 
        private void InitPage() { 
            showHeaderCheck.Checked = false;
            showFooterCheck.Checked = false; 

            repeatColumnsEdit.Clear();
            repeatDirectionCombo.SelectedIndex = -1;
            repeatLayoutCombo.SelectedIndex = -1; 

            extractRowsCheck.Checked = false; 
        } 

        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.LoadComponent"]/*' /> 
        /// <devdoc>
        ///   Loads the component into the page.
        /// </devdoc>
        protected override void LoadComponent() { 
            InitPage();
 
            DataList dataList = (DataList)GetBaseControl(); 

            showHeaderCheck.Checked = dataList.ShowHeader; 
            showFooterCheck.Checked = dataList.ShowFooter;

            repeatColumnsEdit.Text = (dataList.RepeatColumns).ToString(NumberFormatInfo.CurrentInfo);
 
            switch (dataList.RepeatDirection) {
                case RepeatDirection.Horizontal: 
                    repeatDirectionCombo.SelectedIndex = IDX_DIR_HORIZONTAL; 
                    break;
                case RepeatDirection.Vertical: 
                    repeatDirectionCombo.SelectedIndex = IDX_DIR_VERTICAL;
                    break;
            }
 
            switch (dataList.RepeatLayout) {
                case RepeatLayout.Table: 
                    repeatLayoutCombo.SelectedIndex = IDX_MODE_TABLE; 
                    break;
                case RepeatLayout.Flow: 
                    repeatLayoutCombo.SelectedIndex = IDX_MODE_FLOW;
                    break;
            }
 
            extractRowsCheck.Checked = dataList.ExtractTemplateRows;
        } 
 
        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.OnCheckChangedExtractRows"]/*' />
        /// <devdoc> 
        ///   Handles changes to the extract rows checkbox
        /// </devdoc>
        private void OnCheckChangedExtractRows(object source, EventArgs e) {
            if (IsLoading()) 
                return;
            SetDirty(); 
        } 

        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.OnChangedRepeatProps"]/*' /> 
        /// <devdoc>
        ///   Handles changes to the different repeater properties
        /// </devdoc>
        private void OnChangedRepeatProps(object source, EventArgs e) { 
            if (IsLoading())
                return; 
            SetDirty(); 
        }
 
        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.OnCheckChangedShowHeader"]/*' />
        /// <devdoc>
        /// </devdoc>
        private void OnCheckChangedShowHeader(object source, EventArgs e) { 
            if (IsLoading())
                return; 
            SetDirty(); 
        }
 
        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.OnCheckChangedShowFooter"]/*' />
        /// <devdoc>
        /// </devdoc>
        private void OnCheckChangedShowFooter(object source, EventArgs e) { 
            if (IsLoading())
                return; 
            SetDirty(); 
        }
 
        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.SaveComponent"]/*' />
        /// <devdoc>
        ///   Saves the component loaded into the page.
        /// </devdoc> 
        protected override void SaveComponent() {
            DataList dataList = (DataList)GetBaseControl(); 
 
            dataList.ShowHeader = showHeaderCheck.Checked;
            dataList.ShowFooter = showFooterCheck.Checked; 

            string repeatColumnsValue = repeatColumnsEdit.Text.Trim();
            if (repeatColumnsValue.Length != 0) {
                try { 
                    dataList.RepeatColumns = Int32.Parse(repeatColumnsValue, CultureInfo.CurrentCulture);
                } 
                catch { 
                    repeatColumnsEdit.Text = (dataList.RepeatColumns).ToString(CultureInfo.CurrentCulture);
                } 
            }

            switch (repeatDirectionCombo.SelectedIndex) {
                case IDX_DIR_HORIZONTAL: 
                    dataList.RepeatDirection = RepeatDirection.Horizontal;
                    break; 
                case IDX_DIR_VERTICAL: 
                    dataList.RepeatDirection = RepeatDirection.Vertical;
                    break; 
            }

            switch (repeatLayoutCombo.SelectedIndex) {
                case IDX_MODE_TABLE: 
                    dataList.RepeatLayout = RepeatLayout.Table;
                    break; 
                case IDX_MODE_FLOW: 
                    dataList.RepeatLayout = RepeatLayout.Flow;
                    break; 
            }

            dataList.ExtractTemplateRows = extractRowsCheck.Checked;
        } 

        /// <include file='doc\DataListGeneralPage.uex' path='docs/doc[@for="DataListGeneralPage.SetComponent"]/*' /> 
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
