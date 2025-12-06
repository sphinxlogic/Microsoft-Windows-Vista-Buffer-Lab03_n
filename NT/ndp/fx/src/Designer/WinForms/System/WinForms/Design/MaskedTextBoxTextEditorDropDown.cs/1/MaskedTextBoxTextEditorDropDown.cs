using System; 
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Design; 
using System.Data;
using System.Text; 
using System.Windows.Forms; 

namespace System.Windows.Forms.Design 
{
    internal class MaskedTextBoxTextEditorDropDown : UserControl
    {
        private bool cancel; 
        private System.Windows.Forms.MaskedTextBox cloneMtb;
        private System.Windows.Forms.ErrorProvider errorProvider; 
 
        public MaskedTextBoxTextEditorDropDown(MaskedTextBox maskedTextBox)
        { 
            this.cloneMtb = MaskedTextBoxDesigner.GetDesignMaskedTextBox( maskedTextBox );
            this.errorProvider = new System.Windows.Forms.ErrorProvider();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
 
            this.SuspendLayout();
// 
// maskedTextBox 
//
            this.cloneMtb.Dock = System.Windows.Forms.DockStyle.Fill; 

            // Include prompt and literals always so editor can process the text value in a consistent way.
            this.cloneMtb.TextMaskFormat = MaskFormat.IncludePromptAndLiterals;
 
            // Escape prompt, literals and space so input is not rejected due to one of these characters.
            this.cloneMtb.ResetOnPrompt = true; 
            this.cloneMtb.SkipLiterals  = true; 
            this.cloneMtb.ResetOnSpace  = true;
 
            this.cloneMtb.Name = "MaskedTextBoxClone";
            this.cloneMtb.TabIndex = 0;
            this.cloneMtb.MaskInputRejected += new System.Windows.Forms.MaskInputRejectedEventHandler(this.maskedTextBox_MaskInputRejected);
            this.cloneMtb.KeyDown += new System.Windows.Forms.KeyEventHandler(this.maskedTextBox_KeyDown); 

// 
// errorProvider 
//
            this.errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink; 
            this.errorProvider.ContainerControl = this;

//
// MaskedTextBoxTextEditorDropDown 
//
            this.Controls.Add(this.cloneMtb); 
 
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle; 
            this.Name = "MaskedTextBoxTextEditorDropDown";
            this.Padding = new System.Windows.Forms.Padding(16);
            this.Size = new System.Drawing.Size(100, 52);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit(); 
            this.ResumeLayout(false);
            this.PerformLayout(); 
        } 

        public string Value 
        {
            get
            {
                if( this.cancel ) 
                {
                    return null; 
                } 

                // Output will include prompt and literals always to be able to get the characters at the right positions in case 
                // some of them are not set (particularly at lower positions).
                return this.cloneMtb.Text;
            }
        } 

        protected override bool ProcessDialogKey(Keys keyData) 
        { 
            if( keyData ==  Keys.Escape )
            { 
                this.cancel = true;
            }

            return base.ProcessDialogKey(keyData); 
        }
 
        private void maskedTextBox_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) 
        {
            this.errorProvider.SetError(this.cloneMtb, MaskedTextBoxDesigner.GetMaskInputRejectedErrorMessage(e)); 
        }

        private void maskedTextBox_KeyDown(object sender, KeyEventArgs e)
        { 
            this.errorProvider.Clear();
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
﻿using System; 
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Design; 
using System.Data;
using System.Text; 
using System.Windows.Forms; 

namespace System.Windows.Forms.Design 
{
    internal class MaskedTextBoxTextEditorDropDown : UserControl
    {
        private bool cancel; 
        private System.Windows.Forms.MaskedTextBox cloneMtb;
        private System.Windows.Forms.ErrorProvider errorProvider; 
 
        public MaskedTextBoxTextEditorDropDown(MaskedTextBox maskedTextBox)
        { 
            this.cloneMtb = MaskedTextBoxDesigner.GetDesignMaskedTextBox( maskedTextBox );
            this.errorProvider = new System.Windows.Forms.ErrorProvider();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
 
            this.SuspendLayout();
// 
// maskedTextBox 
//
            this.cloneMtb.Dock = System.Windows.Forms.DockStyle.Fill; 

            // Include prompt and literals always so editor can process the text value in a consistent way.
            this.cloneMtb.TextMaskFormat = MaskFormat.IncludePromptAndLiterals;
 
            // Escape prompt, literals and space so input is not rejected due to one of these characters.
            this.cloneMtb.ResetOnPrompt = true; 
            this.cloneMtb.SkipLiterals  = true; 
            this.cloneMtb.ResetOnSpace  = true;
 
            this.cloneMtb.Name = "MaskedTextBoxClone";
            this.cloneMtb.TabIndex = 0;
            this.cloneMtb.MaskInputRejected += new System.Windows.Forms.MaskInputRejectedEventHandler(this.maskedTextBox_MaskInputRejected);
            this.cloneMtb.KeyDown += new System.Windows.Forms.KeyEventHandler(this.maskedTextBox_KeyDown); 

// 
// errorProvider 
//
            this.errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink; 
            this.errorProvider.ContainerControl = this;

//
// MaskedTextBoxTextEditorDropDown 
//
            this.Controls.Add(this.cloneMtb); 
 
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle; 
            this.Name = "MaskedTextBoxTextEditorDropDown";
            this.Padding = new System.Windows.Forms.Padding(16);
            this.Size = new System.Drawing.Size(100, 52);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit(); 
            this.ResumeLayout(false);
            this.PerformLayout(); 
        } 

        public string Value 
        {
            get
            {
                if( this.cancel ) 
                {
                    return null; 
                } 

                // Output will include prompt and literals always to be able to get the characters at the right positions in case 
                // some of them are not set (particularly at lower positions).
                return this.cloneMtb.Text;
            }
        } 

        protected override bool ProcessDialogKey(Keys keyData) 
        { 
            if( keyData ==  Keys.Escape )
            { 
                this.cancel = true;
            }

            return base.ProcessDialogKey(keyData); 
        }
 
        private void maskedTextBox_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) 
        {
            this.errorProvider.SetError(this.cloneMtb, MaskedTextBoxDesigner.GetMaskInputRejectedErrorMessage(e)); 
        }

        private void maskedTextBox_KeyDown(object sender, KeyEventArgs e)
        { 
            this.errorProvider.Clear();
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
