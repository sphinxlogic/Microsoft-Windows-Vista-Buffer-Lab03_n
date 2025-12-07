using System; 
using System.Diagnostics;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization; 
using System.Windows.Forms;
using System.Drawing; 
using System.Design; 

namespace System.Windows.Forms.Design 
{
    internal class FormatStringDialog : System.Windows.Forms.Form
    {
        // we need the context for the HELP service provider 
        private ITypeDescriptorContext context;
 
        private System.Windows.Forms.Button cancelButton; 

        private System.Windows.Forms.Button okButton; 

        private FormatControl formatControl1;

        private bool dirty = false; 

        private DataGridViewCellStyle dgvCellStyle = null; 
        private ListControl listControl = null; 

        public FormatStringDialog(ITypeDescriptorContext context) 
        {
            this.context = context;
            InitializeComponent();
            // vsw 532943: set right to left property according to SR.GetString(SR.RTL) value. 
            string rtlString = SR.GetString(SR.RTL);
            if (rtlString.Equals("RTL_False")) 
            { 
                this.RightToLeft = RightToLeft.No;
                this.RightToLeftLayout = false; 
            }
            else
            {
                this.RightToLeft = RightToLeft.Yes; 
                this.RightToLeftLayout = true;
            } 
        } 

        public DataGridViewCellStyle DataGridViewCellStyle 
        {
            set
            {
                this.dgvCellStyle = value; 
                this.listControl = null;
            } 
        } 

        public bool Dirty 
        {
            get
            {
                return this.dirty || this.formatControl1.Dirty; 
            }
        } 
 
        public ListControl ListControl
        { 
            set
            {
                this.listControl = value;
                this.dgvCellStyle = null; 
            }
        } 
 
        private void FormatStringDialog_HelpButtonClicked(object sender, CancelEventArgs e)
        { 
            FormatStringDialog_HelpRequestHandled();
            e.Cancel = true;
        }
 
        private void FormatStringDialog_HelpRequested(object sender, HelpEventArgs e)
        { 
            FormatStringDialog_HelpRequestHandled(); 
            e.Handled = true;
        } 

        private void FormatStringDialog_HelpRequestHandled()
        {
            IHelpService helpService = this.context.GetService(typeof(IHelpService)) as IHelpService; 
            if (helpService != null)
            { 
                helpService.ShowHelpFromKeyword("vs.FormatStringDialog"); 
            }
        } 

        //HACK: if we're adjusting positions after the form's loaded, we didn't set the form up correctly.
        internal void FormatControlFinishedLoading()
        { 
            this.okButton.Top = this.formatControl1.Bottom + 5;
            this.cancelButton.Top = this.formatControl1.Bottom + 5; 
            int formatControlRightSideOffset = GetRightSideOffset(this.formatControl1); 
            int cancelButtonRightSideOffset = GetRightSideOffset(this.cancelButton);
            this.okButton.Left += formatControlRightSideOffset - cancelButtonRightSideOffset; 
            this.cancelButton.Left += formatControlRightSideOffset - cancelButtonRightSideOffset;
        }

        private static int GetRightSideOffset(Control ctl) 
        {
            int result = ctl.Width; 
            while (ctl != null) 
            {
                result += ctl.Left; 
                ctl = ctl.Parent;
            }
            return result;
        } 

        private void FormatStringDialog_Load(object sender, EventArgs e) 
        { 
            // make a reasonable guess what user control should be shown
            string formatString = this.dgvCellStyle != null ? this.dgvCellStyle.Format : this.listControl.FormatString; 
            object nullValue = this.dgvCellStyle != null ? this.dgvCellStyle.NullValue : null;

            string formatType = string.Empty;
            if (!String.IsNullOrEmpty(formatString)) 
            {
                formatType = FormatControl.FormatTypeStringFromFormatString(formatString); 
            } 

            // the null value text box should be enabled only when editing DataGridViewCellStyle 
            // when we are editing ListControl, it should be disabled
            if (this.dgvCellStyle != null)
            {
                this.formatControl1.NullValueTextBoxEnabled = true; 
            }
            else 
            { 
                Debug.Assert(this.listControl != null, "we check this everywhere, but it does not hurt to check it again");
                this.formatControl1.NullValueTextBoxEnabled = false; 
            }

            this.formatControl1.FormatType = formatType;
 
            // push the information from FormatString/FormatInfo/NullValue into the FormattingUserControl
            FormatControl.FormatTypeClass formatTypeItem = this.formatControl1.FormatTypeItem; 
            if (formatTypeItem != null) 
            {
                // parsing the FormatString uses the CultureInfo. So push the CultureInfo before push the FormatString. 
                formatTypeItem.PushFormatStringIntoFormatType(formatString);
            }
            else
            { 
                // make General format type the default
                this.formatControl1.FormatType = SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting); 
            } 
            this.formatControl1.NullValue = nullValue != null ? nullValue.ToString() : "";
        } 

        public void End()
        {
            // clear the tree nodes collection 
        }
 
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor. 
        /// </summary>
        private void InitializeComponent()
        {
            this.cancelButton = new System.Windows.Forms.Button(); 
            this.okButton = new System.Windows.Forms.Button();
            this.formatControl1 = new FormatControl(); 
            this.SuspendLayout(); 
//
// formatControl1 
//
            this.formatControl1.Location = new System.Drawing.Point(10, 10);
            this.formatControl1.Margin = new System.Windows.Forms.Padding(0);
            this.formatControl1.Name = "formatControl1"; 
            this.formatControl1.Size = new System.Drawing.Size(376, 268);
            this.formatControl1.TabIndex = 0; 
// 
// cancelButton
// 
            this.cancelButton.Location = new System.Drawing.Point(299, 288);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(87, 23);
            this.cancelButton.TabIndex = 2; 
            this.cancelButton.Text = SR.GetString(SR.DataGridView_Cancel);
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click); 
//
// okButton 
//
            this.okButton.Location = new System.Drawing.Point(203, 288);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(87, 23); 
            this.okButton.TabIndex = 1;
            this.okButton.Text = SR.GetString(SR.DataGridView_OK); 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK; 
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
// 
// Form1
//
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6, 13); 
            this.ClientSize = new System.Drawing.Size(396, 295);
            this.AutoSize = true; 
            this.HelpButton = true; 
            this.MaximizeBox = false;
            this.MinimizeBox = false; 
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;
            this.Icon = null; 
            this.Name = "Form1";
            this.Controls.Add(okButton); 
            this.Controls.Add(formatControl1); 
            this.Controls.Add(cancelButton);
            this.Padding = new System.Windows.Forms.Padding(0); 
            this.Text = SR.GetString(SR.FormatStringDialogTitle);
            this.HelpButtonClicked += new CancelEventHandler(FormatStringDialog_HelpButtonClicked);
            this.HelpRequested += new HelpEventHandler(FormatStringDialog_HelpRequested);
            this.Load += new EventHandler(FormatStringDialog_Load); 
            this.ResumeLayout(false);
        } 
 
        private void cancelButton_Click(object sender, System.EventArgs e)
        { 
            this.dirty = false;
        }

        private void okButton_Click(object sender, System.EventArgs e) 
        {
            this.PushChanges(); 
        } 

        protected override bool ProcessDialogKey(Keys keyData) 
        {
            if ((keyData & Keys.Modifiers) == 0)
            {
                switch (keyData & Keys.KeyCode) 
                {
                    case Keys.Enter: 
                        this.DialogResult = DialogResult.OK; 
                        this.PushChanges();
                        this.Close(); 
                        return true;
                    case Keys.Escape:
                        this.dirty = false;
                        this.DialogResult = DialogResult.Cancel; 
                        this.Close();
                        return true; 
                    default: 
                        return base.ProcessDialogKey(keyData);
                } 
            }
            else
            {
                return base.ProcessDialogKey(keyData); 
            }
        } 
 
        private void PushChanges()
        { 
            FormatControl.FormatTypeClass formatTypeItem = this.formatControl1.FormatTypeItem;
            if (formatTypeItem != null)
            {
                if (this.dgvCellStyle != null) 
                {
                    this.dgvCellStyle.Format = formatTypeItem.FormatString; 
                    this.dgvCellStyle.NullValue = this.formatControl1.NullValue; 
                }
                else 
                {
                    this.listControl.FormatString = formatTypeItem.FormatString;
                }
                this.dirty = true; 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
using System; 
using System.Diagnostics;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization; 
using System.Windows.Forms;
using System.Drawing; 
using System.Design; 

namespace System.Windows.Forms.Design 
{
    internal class FormatStringDialog : System.Windows.Forms.Form
    {
        // we need the context for the HELP service provider 
        private ITypeDescriptorContext context;
 
        private System.Windows.Forms.Button cancelButton; 

        private System.Windows.Forms.Button okButton; 

        private FormatControl formatControl1;

        private bool dirty = false; 

        private DataGridViewCellStyle dgvCellStyle = null; 
        private ListControl listControl = null; 

        public FormatStringDialog(ITypeDescriptorContext context) 
        {
            this.context = context;
            InitializeComponent();
            // vsw 532943: set right to left property according to SR.GetString(SR.RTL) value. 
            string rtlString = SR.GetString(SR.RTL);
            if (rtlString.Equals("RTL_False")) 
            { 
                this.RightToLeft = RightToLeft.No;
                this.RightToLeftLayout = false; 
            }
            else
            {
                this.RightToLeft = RightToLeft.Yes; 
                this.RightToLeftLayout = true;
            } 
        } 

        public DataGridViewCellStyle DataGridViewCellStyle 
        {
            set
            {
                this.dgvCellStyle = value; 
                this.listControl = null;
            } 
        } 

        public bool Dirty 
        {
            get
            {
                return this.dirty || this.formatControl1.Dirty; 
            }
        } 
 
        public ListControl ListControl
        { 
            set
            {
                this.listControl = value;
                this.dgvCellStyle = null; 
            }
        } 
 
        private void FormatStringDialog_HelpButtonClicked(object sender, CancelEventArgs e)
        { 
            FormatStringDialog_HelpRequestHandled();
            e.Cancel = true;
        }
 
        private void FormatStringDialog_HelpRequested(object sender, HelpEventArgs e)
        { 
            FormatStringDialog_HelpRequestHandled(); 
            e.Handled = true;
        } 

        private void FormatStringDialog_HelpRequestHandled()
        {
            IHelpService helpService = this.context.GetService(typeof(IHelpService)) as IHelpService; 
            if (helpService != null)
            { 
                helpService.ShowHelpFromKeyword("vs.FormatStringDialog"); 
            }
        } 

        //HACK: if we're adjusting positions after the form's loaded, we didn't set the form up correctly.
        internal void FormatControlFinishedLoading()
        { 
            this.okButton.Top = this.formatControl1.Bottom + 5;
            this.cancelButton.Top = this.formatControl1.Bottom + 5; 
            int formatControlRightSideOffset = GetRightSideOffset(this.formatControl1); 
            int cancelButtonRightSideOffset = GetRightSideOffset(this.cancelButton);
            this.okButton.Left += formatControlRightSideOffset - cancelButtonRightSideOffset; 
            this.cancelButton.Left += formatControlRightSideOffset - cancelButtonRightSideOffset;
        }

        private static int GetRightSideOffset(Control ctl) 
        {
            int result = ctl.Width; 
            while (ctl != null) 
            {
                result += ctl.Left; 
                ctl = ctl.Parent;
            }
            return result;
        } 

        private void FormatStringDialog_Load(object sender, EventArgs e) 
        { 
            // make a reasonable guess what user control should be shown
            string formatString = this.dgvCellStyle != null ? this.dgvCellStyle.Format : this.listControl.FormatString; 
            object nullValue = this.dgvCellStyle != null ? this.dgvCellStyle.NullValue : null;

            string formatType = string.Empty;
            if (!String.IsNullOrEmpty(formatString)) 
            {
                formatType = FormatControl.FormatTypeStringFromFormatString(formatString); 
            } 

            // the null value text box should be enabled only when editing DataGridViewCellStyle 
            // when we are editing ListControl, it should be disabled
            if (this.dgvCellStyle != null)
            {
                this.formatControl1.NullValueTextBoxEnabled = true; 
            }
            else 
            { 
                Debug.Assert(this.listControl != null, "we check this everywhere, but it does not hurt to check it again");
                this.formatControl1.NullValueTextBoxEnabled = false; 
            }

            this.formatControl1.FormatType = formatType;
 
            // push the information from FormatString/FormatInfo/NullValue into the FormattingUserControl
            FormatControl.FormatTypeClass formatTypeItem = this.formatControl1.FormatTypeItem; 
            if (formatTypeItem != null) 
            {
                // parsing the FormatString uses the CultureInfo. So push the CultureInfo before push the FormatString. 
                formatTypeItem.PushFormatStringIntoFormatType(formatString);
            }
            else
            { 
                // make General format type the default
                this.formatControl1.FormatType = SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting); 
            } 
            this.formatControl1.NullValue = nullValue != null ? nullValue.ToString() : "";
        } 

        public void End()
        {
            // clear the tree nodes collection 
        }
 
        /// <summary> 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor. 
        /// </summary>
        private void InitializeComponent()
        {
            this.cancelButton = new System.Windows.Forms.Button(); 
            this.okButton = new System.Windows.Forms.Button();
            this.formatControl1 = new FormatControl(); 
            this.SuspendLayout(); 
//
// formatControl1 
//
            this.formatControl1.Location = new System.Drawing.Point(10, 10);
            this.formatControl1.Margin = new System.Windows.Forms.Padding(0);
            this.formatControl1.Name = "formatControl1"; 
            this.formatControl1.Size = new System.Drawing.Size(376, 268);
            this.formatControl1.TabIndex = 0; 
// 
// cancelButton
// 
            this.cancelButton.Location = new System.Drawing.Point(299, 288);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(87, 23);
            this.cancelButton.TabIndex = 2; 
            this.cancelButton.Text = SR.GetString(SR.DataGridView_Cancel);
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click); 
//
// okButton 
//
            this.okButton.Location = new System.Drawing.Point(203, 288);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(87, 23); 
            this.okButton.TabIndex = 1;
            this.okButton.Text = SR.GetString(SR.DataGridView_OK); 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK; 
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
// 
// Form1
//
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6, 13); 
            this.ClientSize = new System.Drawing.Size(396, 295);
            this.AutoSize = true; 
            this.HelpButton = true; 
            this.MaximizeBox = false;
            this.MinimizeBox = false; 
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;
            this.Icon = null; 
            this.Name = "Form1";
            this.Controls.Add(okButton); 
            this.Controls.Add(formatControl1); 
            this.Controls.Add(cancelButton);
            this.Padding = new System.Windows.Forms.Padding(0); 
            this.Text = SR.GetString(SR.FormatStringDialogTitle);
            this.HelpButtonClicked += new CancelEventHandler(FormatStringDialog_HelpButtonClicked);
            this.HelpRequested += new HelpEventHandler(FormatStringDialog_HelpRequested);
            this.Load += new EventHandler(FormatStringDialog_Load); 
            this.ResumeLayout(false);
        } 
 
        private void cancelButton_Click(object sender, System.EventArgs e)
        { 
            this.dirty = false;
        }

        private void okButton_Click(object sender, System.EventArgs e) 
        {
            this.PushChanges(); 
        } 

        protected override bool ProcessDialogKey(Keys keyData) 
        {
            if ((keyData & Keys.Modifiers) == 0)
            {
                switch (keyData & Keys.KeyCode) 
                {
                    case Keys.Enter: 
                        this.DialogResult = DialogResult.OK; 
                        this.PushChanges();
                        this.Close(); 
                        return true;
                    case Keys.Escape:
                        this.dirty = false;
                        this.DialogResult = DialogResult.Cancel; 
                        this.Close();
                        return true; 
                    default: 
                        return base.ProcessDialogKey(keyData);
                } 
            }
            else
            {
                return base.ProcessDialogKey(keyData); 
            }
        } 
 
        private void PushChanges()
        { 
            FormatControl.FormatTypeClass formatTypeItem = this.formatControl1.FormatTypeItem;
            if (formatTypeItem != null)
            {
                if (this.dgvCellStyle != null) 
                {
                    this.dgvCellStyle.Format = formatTypeItem.FormatString; 
                    this.dgvCellStyle.NullValue = this.formatControl1.NullValue; 
                }
                else 
                {
                    this.listControl.FormatString = formatTypeItem.FormatString;
                }
                this.dirty = true; 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
