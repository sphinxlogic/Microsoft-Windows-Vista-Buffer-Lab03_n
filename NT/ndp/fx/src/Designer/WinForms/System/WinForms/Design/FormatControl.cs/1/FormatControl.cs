#region Using directives 

using System;
using System.Collections.Generic;
using System.ComponentModel; 
using System.Drawing;
using System.Data; 
using System.Text; 
using System.Windows.Forms;
using System.Design; 
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

#endregion 

namespace System.Windows.Forms.Design 
{ 
    internal partial class FormatControl : UserControl
    { 
        private const int NoFormattingIndex = 0;
        private const int NumericIndex = 1;
        private const int CurrencyIndex = 2;
        private const int DateTimeIndex = 3; 
        private const int ScientificIndex = 4;
        private const int CustomIndex = 5; 
 
        private TextBox customStringTextBox = new TextBox();
 
        // static because we want this value to be the same across a
        // VS session
        private static DateTime dateTimeFormatValue = System.DateTime.Now;
 
        private bool dirty = false;
        private bool loaded = false; 
 
        public FormatControl()
        { 
            InitializeComponent();
        }

        public bool Dirty 
        {
            get 
            { 
                return this.dirty;
            } 
            set
            {
                this.dirty = value;
            } 
        }
 
        public string FormatType 
        {
            get 
            {
                FormatTypeClass formatType = this.formatTypeListBox.SelectedItem as FormatTypeClass;
                if (formatType != null)
                { 
                    return formatType.ToString();
                } 
                else 
                {
                    return string.Empty; 
                }
            }
            set
            { 
                this.formatTypeListBox.SelectedIndex = 0;
                for (int i = 0; i < this.formatTypeListBox.Items.Count; i++) 
                { 
                    FormatTypeClass formatType = this.formatTypeListBox.Items[i] as FormatTypeClass;
                    if (formatType.ToString().Equals(value)) 
                    {
                        this.formatTypeListBox.SelectedIndex = i;
                    }
                } 
            }
        } 
 
        public FormatTypeClass FormatTypeItem
        { 
            get
            {
                return this.formatTypeListBox.SelectedItem as FormatTypeClass;
            } 
        }
 
        public string NullValue 
        {
            get 
            {
                // VSWhidbey#408448: If text box is empty or contains just whitespace, return 'null' for the NullValue.
                // Otherwise we end up pushing empty string as the NullValue for every binding, which breaks non-string
                // bindings. This does mean that setting the NullValue to empty string now becomes a code-only scenario 
                // for users, but that is an acceptible trade-off.
                // 
                String nullValue = this.nullValueTextBox.Text.Trim(); 
                return (nullValue.Length == 0) ? null : nullValue;
            } 
            set
            {
                this.nullValueTextBox.TextChanged -= new System.EventHandler(this.nullValueTextBox_TextChanged);
                this.nullValueTextBox.Text = value; 
                this.nullValueTextBox.TextChanged += new System.EventHandler(this.nullValueTextBox_TextChanged);
            } 
        } 

        public bool NullValueTextBoxEnabled 
        {
            set
            {
                this.nullValueTextBox.Enabled = value; 
            }
        } 
 
        void customStringTextBox_TextChanged(object sender, EventArgs e)
        { 
            CustomFormatType customFormatType = this.formatTypeListBox.SelectedItem as CustomFormatType;
            this.sampleLabel.Text = customFormatType.SampleString;
            this.dirty = true;
        } 

        private void dateTimeFormatsListBox_SelectedIndexChanged(object sender, System.EventArgs e) 
        { 
            // recompute the SampleLabel
            FormatTypeClass item = this.formatTypeListBox.SelectedItem as FormatTypeClass; 
            this.sampleLabel.Text = item.SampleString;
            this.dirty = true;
        }
 
        private void decimalPlacesUpDown_ValueChanged(object sender, EventArgs e)
        { 
            // update the sample label 
            FormatTypeClass item = this.formatTypeListBox.SelectedItem as FormatTypeClass;
            this.sampleLabel.Text = item.SampleString; 
            this.dirty = true;
        }

        private void formatGroupBox_Enter(object sender, EventArgs e) 
        {
        } 
 
        private void formatTypeListBox_SelectedIndexChanged(object sender, EventArgs e)
        { 
            FormatTypeClass formatType = this.formatTypeListBox.SelectedItem as FormatTypeClass;
            UpdateControlVisibility(formatType);
            this.sampleLabel.Text = formatType.SampleString;
            this.explanationLabel.Text = formatType.TopLabelString; 
            this.dirty = true;
        } 
 
        // given a formatString/formatInfo combination, this method suggest the most appropiate user control
        // the result of the function is one of the strings "Numeric", "Currency", "DateTime", "Percentage", "Scientific", "Custom" 
        public static string FormatTypeStringFromFormatString(string formatString)
        {
            if (String.IsNullOrEmpty(formatString))
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting);
            } 
            if (System.Windows.Forms.Design.FormatControl.NumericFormatType.ParseStatic(formatString)) 
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeNumeric); 
            }
            else if (System.Windows.Forms.Design.FormatControl.CurrencyFormatType.ParseStatic(formatString))
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeCurrency); 
            }
            else if (System.Windows.Forms.Design.FormatControl.DateTimeFormatType.ParseStatic(formatString)) 
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeDateTime);
            } 
            else if (System.Windows.Forms.Design.FormatControl.ScientificFormatType.ParseStatic(formatString))
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeScientific);
            } 
            else
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeCustom); 
            }
        } 

        protected override bool ProcessMnemonic(char charCode)
        {
            if (System.Windows.Forms.Control.IsMnemonic(charCode, this.formatTypeLabel.Text)) 
            {
                this.formatTypeListBox.Focus(); 
                return true; 
            }
 
            if (System.Windows.Forms.Control.IsMnemonic(charCode, this.nullValueLabel.Text))
            {
                this.nullValueTextBox.Focus();
                return true; 
            }
 
            int selIndex = this.formatTypeListBox.SelectedIndex; 
            switch (selIndex)
            { 
                case NoFormattingIndex:
                    return false;
                case ScientificIndex:
                    // FALL THRU 
                case CurrencyIndex:
                    // FALL THRU 
                case NumericIndex: 
                    System.Diagnostics.Debug.Assert(this.decimalPlacesUpDown.Visible);
                    if (System.Windows.Forms.Control.IsMnemonic(charCode, this.secondRowLabel.Text)) 
                    {
                        this.decimalPlacesUpDown.Focus();
                        return true;
                    } 
                    else
                    { 
                        return false; 
                    }
                case DateTimeIndex: 
                    System.Diagnostics.Debug.Assert(this.dateTimeFormatsListBox.Visible);
                    if (System.Windows.Forms.Control.IsMnemonic(charCode, this.secondRowLabel.Text))
                    {
                        this.dateTimeFormatsListBox.Focus(); 
                        return true;
                    } 
                    else 
                    {
                        return false; 
                    }
                case CustomIndex:
                    System.Diagnostics.Debug.Assert(this.customStringTextBox.Visible);
                    if (System.Windows.Forms.Control.IsMnemonic(charCode, this.secondRowLabel.Text)) 
                    {
                        this.customStringTextBox.Focus(); 
                        return true; 
                    }
                    else 
                    {
                        return false;
                    }
 
                default:
                    return false; 
            } 
        }
 
        public void ResetFormattingInfo()
        {
            this.decimalPlacesUpDown.ValueChanged -= new EventHandler(decimalPlacesUpDown_ValueChanged);
            this.customStringTextBox.TextChanged -= new EventHandler(customStringTextBox_TextChanged); 
            this.dateTimeFormatsListBox.SelectedIndexChanged -= new EventHandler(dateTimeFormatsListBox_SelectedIndexChanged);
            this.formatTypeListBox.SelectedIndexChanged -= new EventHandler(formatTypeListBox_SelectedIndexChanged); 
 
            this.decimalPlacesUpDown.Value = 2;
            this.nullValueTextBox.Text = String.Empty; 
            this.dateTimeFormatsListBox.SelectedIndex = -1;
            this.formatTypeListBox.SelectedIndex = -1;
            this.customStringTextBox.Text = String.Empty;
 
            this.decimalPlacesUpDown.ValueChanged += new EventHandler(decimalPlacesUpDown_ValueChanged);
            this.customStringTextBox.TextChanged += new EventHandler(customStringTextBox_TextChanged); 
            this.dateTimeFormatsListBox.SelectedIndexChanged += new EventHandler(dateTimeFormatsListBox_SelectedIndexChanged); 
            this.formatTypeListBox.SelectedIndexChanged += new EventHandler(formatTypeListBox_SelectedIndexChanged);
        } 

        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        private void UpdateControlVisibility(FormatTypeClass formatType)
        { 
            if (formatType == null)
            { 
                this.explanationLabel.Visible = false; 
                this.sampleLabel.Visible = false;
                this.nullValueLabel.Visible = false; 
                this.secondRowLabel.Visible = false;
                this.nullValueTextBox.Visible = false;
                this.thirdRowLabel.Visible = false;
                this.dateTimeFormatsListBox.Visible = false; 
                this.customStringTextBox.Visible = false;
                this.decimalPlacesUpDown.Visible = false; 
                return; 
            }
 
            this.tableLayoutPanel1.SuspendLayout();
            this.secondRowLabel.Text = "";

            // process the decimalPlacesLabelVisible 
            if (formatType.DropDownVisible)
            { 
                this.secondRowLabel.Text = SR.GetString(SR.BindingFormattingDialogDecimalPlaces); 
                this.decimalPlacesUpDown.Visible = true;
            } 
            else
            {
                this.decimalPlacesUpDown.Visible = false;
            } 

            // process customFormatLabelVisible 
            if (formatType.FormatStringTextBoxVisible) 
            {
                this.secondRowLabel.Text = SR.GetString(SR.BindingFormattingDialogCustomFormat); 
                this.thirdRowLabel.Visible = true;
                this.tableLayoutPanel1.SetColumn(this.thirdRowLabel, 0);
                this.tableLayoutPanel1.SetColumnSpan(this.thirdRowLabel, 2);
                this.customStringTextBox.Visible = true; 
                if (this.tableLayoutPanel1.Controls.Contains(this.dateTimeFormatsListBox))
                { 
                    this.tableLayoutPanel1.Controls.Remove(this.dateTimeFormatsListBox); 
                }
                this.tableLayoutPanel1.Controls.Add(this.customStringTextBox, 1, 1); 
            }
            else
            {
                this.thirdRowLabel.Visible = false; 
                this.customStringTextBox.Visible = false;
            } 
 
            if (formatType.ListBoxVisible)
            { 
                this.secondRowLabel.Text = SR.GetString(SR.BindingFormattingDialogType);

                if (this.tableLayoutPanel1.Controls.Contains(this.customStringTextBox))
                { 
                    this.tableLayoutPanel1.Controls.Remove(this.customStringTextBox);
                } 
 
                this.dateTimeFormatsListBox.Visible = true;
                this.tableLayoutPanel1.Controls.Add(this.dateTimeFormatsListBox, 0, 2); 
                this.tableLayoutPanel1.SetColumn(this.dateTimeFormatsListBox, 0);
                this.tableLayoutPanel1.SetColumnSpan(this.dateTimeFormatsListBox, 2);
            }
            else 
            {
                this.dateTimeFormatsListBox.Visible = false; 
            } 
            this.tableLayoutPanel1.ResumeLayout(true /*performLayout*/);
        } 

        private void UpdateCustomStringTextBox()
        {
            this.customStringTextBox = new TextBox(); 
            this.customStringTextBox.AccessibleDescription = SR.GetString(SR.BindingFormattingDialogCustomFormatAccessibleDescription);
            this.customStringTextBox.Margin = new Padding(0, 3, 0, 3); 
            this.customStringTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right; 
            this.customStringTextBox.TabIndex = 3;
            this.customStringTextBox.TextChanged += new EventHandler(customStringTextBox_TextChanged); 
        }

        private void UpdateFormatTypeListBoxHeight()
        { 
            // there seems to be a bug in layout because setting
            // the anchor on the list box does not work. 
            this.formatTypeListBox.Height = this.tableLayoutPanel1.Bottom - this.formatTypeListBox.Top; 
        }
 
        private void UpdateFormatTypeListBoxItems()
        {
            this.dateTimeFormatsListBox.SelectedIndexChanged -= new System.EventHandler(dateTimeFormatsListBox_SelectedIndexChanged);
            this.dateTimeFormatsListBox.Items.Clear(); 
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "d"));
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "D")); 
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "f")); 
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "F"));
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "g")); 
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "G"));
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "t"));
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "T"));
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "M")); 
            this.dateTimeFormatsListBox.SelectedIndex = 0;
            this.dateTimeFormatsListBox.SelectedIndexChanged += new System.EventHandler(dateTimeFormatsListBox_SelectedIndexChanged); 
        } 

        private void UpdateTBLHeight() 
        {
            this.tableLayoutPanel1.SuspendLayout();

            // set up the customStringTextBox 
            this.tableLayoutPanel1.Controls.Add(this.customStringTextBox, 1, 1);
            this.customStringTextBox.Visible = false; 
 
            // set the thirdRowLabel
            this.thirdRowLabel.MaximumSize = new Size(this.tableLayoutPanel1.Width, 0); 
            this.dateTimeFormatsListBox.Visible = false;
            this.tableLayoutPanel1.SetColumn(this.thirdRowLabel, 0);
            this.tableLayoutPanel1.SetColumnSpan(this.thirdRowLabel, 2);
            this.thirdRowLabel.AutoSize = true; 
            this.tableLayoutPanel1.ResumeLayout(true /*performLayout*/);
 
            // Now that PerformLayout set the bounds for the tableLayoutPanel 
            // we can use these bounds to specify the tableLayoutPanel minimumSize.
            this.tableLayoutPanel1.MinimumSize = new System.Drawing.Size(this.tableLayoutPanel1.Width, this.tableLayoutPanel1.Height); 
        }

        private void FormatControl_Load(object sender, EventArgs e)
        { 
            if (this.loaded)
            { 
                // we already did the setup work 
                return;
            } 

            int minWidth, minHeight;
            this.nullValueLabel.Text = SR.GetString(SR.BindingFormattingDialogNullValue);
            minWidth = this.nullValueLabel.Width; 
            minHeight = this.nullValueLabel.Height;
 
            this.secondRowLabel.Text = SR.GetString(SR.BindingFormattingDialogDecimalPlaces); 
            minWidth = Math.Max(minWidth, this.secondRowLabel.Width);
            minHeight = Math.Max(minHeight, this.secondRowLabel.Height); 

            this.secondRowLabel.Text = SR.GetString(SR.BindingFormattingDialogCustomFormat);
            minWidth = Math.Max(minWidth, this.secondRowLabel.Width);
            minHeight = Math.Max(minHeight, this.secondRowLabel.Height); 

            this.nullValueLabel.MinimumSize = new Size(minWidth, minHeight); 
            this.secondRowLabel.MinimumSize = new Size(minWidth, minHeight); 

            // add items to the list box 
            this.formatTypeListBox.SelectedIndexChanged -= new EventHandler(this.formatTypeListBox_SelectedIndexChanged);
            this.formatTypeListBox.Items.Clear();
            this.formatTypeListBox.Items.Add(new NoFormattingFormatType());
            this.formatTypeListBox.Items.Add(new NumericFormatType(this)); 
            this.formatTypeListBox.Items.Add(new CurrencyFormatType(this));
            this.formatTypeListBox.Items.Add(new DateTimeFormatType(this)); 
            this.formatTypeListBox.Items.Add(new ScientificFormatType(this)); 
            this.formatTypeListBox.Items.Add(new CustomFormatType(this));
            this.formatTypeListBox.SelectedIndex = 0; 
            this.formatTypeListBox.SelectedIndexChanged += new EventHandler(this.formatTypeListBox_SelectedIndexChanged);

            UpdateCustomStringTextBox();
            UpdateTBLHeight(); 
            UpdateFormatTypeListBoxHeight();
            UpdateFormatTypeListBoxItems(); 
 
            UpdateControlVisibility(this.formatTypeListBox.SelectedItem as FormatTypeClass);
            this.sampleLabel.Text = ((this.formatTypeListBox.SelectedItem) as FormatTypeClass).SampleString; 
            this.explanationLabel.Size = new System.Drawing.Size(this.formatGroupBox.Width - 10, 30);
            this.explanationLabel.Text = ((this.formatTypeListBox.SelectedItem) as FormatTypeClass).TopLabelString;

            this.dirty = false; 

            FormatControlFinishedLoading(); 
 
            this.loaded = true;
        } 

        //
        // This method tells the BindingFormattingDialog and the FormatStringDialog
        // that the FormatControl is loaded and resized. 
        //
        // 
        private void FormatControlFinishedLoading() 
        {
            BindingFormattingDialog bfd = null; 
            FormatStringDialog fsd = null;

            //
            Control ctl = this.Parent; 
            while (ctl != null)
            { 
                bfd = ctl as BindingFormattingDialog; 
                fsd = ctl as FormatStringDialog;
                if (bfd != null || fsd != null) 
                {
                    break;
                }
                ctl = ctl.Parent; 
            }
 
            if (fsd != null) 
            {
                fsd.FormatControlFinishedLoading(); 
            }

        }
 
        private class DateTimeFormatsListBoxItem
        { 
            DateTime value; 
            string formatString;
            public DateTimeFormatsListBoxItem(DateTime value, string formatString) 
            {
                this.value = value;
                this.formatString = formatString;
            } 

            public string FormatString 
            { 
                get
                { 
                    return this.formatString;
                }
            }
 
            public override string ToString()
            { 
 	            return value.ToString(this.formatString, CultureInfo.CurrentCulture); 
            }
        } 

        internal abstract class FormatTypeClass
        {
            public abstract string TopLabelString { get;} 
            public abstract string SampleString { get;}
            public abstract bool DropDownVisible { get;} 
            public abstract bool ListBoxVisible { get;} 
            public abstract bool FormatStringTextBoxVisible { get;}
            public abstract bool FormatLabelVisible { get;} 
            public abstract string FormatString { get;}
            public abstract bool Parse(string formatString);
            public abstract void PushFormatStringIntoFormatType(string formatString);
        } 

        private class NoFormattingFormatType : FormatTypeClass 
        { 
            public override string TopLabelString
            { 
                get
                {
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormattingExplanation);
                } 
            }
            public override string SampleString 
            { 
                get
                { 
                    return "-1234.5";
                }
            }
            public override bool DropDownVisible 
            {
                get 
                { 
                    return false;
                } 
            }
            public override bool ListBoxVisible
            {
                get 
                {
                    return false; 
                } 
            }
 
            public override bool FormatLabelVisible
            {
                get
                { 
                    return false;
                } 
            } 

            public override string FormatString 
            {
                get
                {
                    return ""; 
                }
            } 
 
            public override bool FormatStringTextBoxVisible
            { 
                get
                {
                    return false;
                } 
            }
 
            public override bool Parse(string formatString) 
            {
                return false; 
            }

            public override void PushFormatStringIntoFormatType(string formatString)
            { 
                // nothing to do;
            } 
 
            public override string ToString()
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting);
            }
        }
 
        private class NumericFormatType : FormatTypeClass
        { 
            FormatControl owner; 
            public NumericFormatType(FormatControl owner)
            { 
                this.owner = owner;
            }

            public override string TopLabelString 
            {
                get 
                { 
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeNumericExplanation);
                } 
            }
            public override string SampleString
            {
                get 
                {
                    return (-1234.5678).ToString(this.FormatString, CultureInfo.CurrentCulture); 
                } 
            }
            public override bool DropDownVisible 
            {
                get
                {
                    return true; 
                }
            } 
            public override bool ListBoxVisible 
            {
                get 
                {
                    return false;
                }
            } 

            public override bool FormatLabelVisible 
            { 
                get
                { 
                    return false;
                }
            }
 
            public override string FormatString
            { 
                get 
                {
                    switch ((int)this.owner.decimalPlacesUpDown.Value) 
                    {
                        case 0:
                            return "N0";
                        case 1: 
                            return "N1";
                        case 2: 
                            return "N2"; 
                        case 3:
                            return "N3"; 
                        case 4:
                            return "N4";
                        case 5:
                            return "N5"; 
                        case 6:
                            return "N6"; 
                        default: 
                            System.Diagnostics.Debug.Fail("decimalPlacesUpDown should allow only up to 6 digits");
                            return ""; 
                    }
                }
            }
 
            public override bool FormatStringTextBoxVisible
            { 
                get 
                {
                    return false; 
                }
            }

            public static bool ParseStatic(string formatString) 
            {
                return formatString.Equals("N0") || 
                       formatString.Equals("N1") || 
                       formatString.Equals("N2") ||
                       formatString.Equals("N3") || 
                       formatString.Equals("N4") ||
                       formatString.Equals("N5") ||
                       formatString.Equals("N6");
            } 

            public override bool Parse(string formatString) 
            { 
                return ParseStatic(formatString);
            } 

            public override void PushFormatStringIntoFormatType(string formatString)
            {
#if DEBUG 
                System.Diagnostics.Debug.Assert(Parse(formatString), "we only push valid strings");
#endif // DEBUG 
                if (formatString.Equals("N0")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 0; 
                }
                else if (formatString.Equals("N1"))
                {
                    this.owner.decimalPlacesUpDown.Value = 1; 
                }
                else if (formatString.Equals("N2")) 
                { 
                    this.owner.decimalPlacesUpDown.Value = 2;
                } 
                else if (formatString.Equals("N3"))
                {
                    this.owner.decimalPlacesUpDown.Value = 3;
                } 
                else if (formatString.Equals("N4"))
                { 
                    this.owner.decimalPlacesUpDown.Value = 4; 
                }
                else if (formatString.Equals("N5")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 5;
                }
                else if (formatString.Equals("N6")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 6; 
                } 
            }
 
            public override string ToString()
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeNumeric);
            } 
        }
 
        private class CurrencyFormatType : FormatTypeClass 
        {
            FormatControl owner; 
            public CurrencyFormatType(FormatControl owner)
            {
                this.owner = owner;
            } 

            public override string TopLabelString 
            { 
                get
                { 
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeCurrencyExplanation);
                }
            }
            public override string SampleString 
            {
                get 
                { 
                    return (-1234.5678).ToString(this.FormatString, CultureInfo.CurrentCulture);
                } 
            }
            public override bool DropDownVisible
            {
                get 
                {
                    return true; 
                } 
            }
            public override bool ListBoxVisible 
            {
                get
                {
                    return false; 
                }
            } 
 
            public override bool FormatLabelVisible
            { 
                get
                {
                    return false;
                } 
            }
 
            public override string FormatString 
            {
                get 
                {
                    switch ((int)this.owner.decimalPlacesUpDown.Value)
                    {
                        case 0: 
                            return "C0";
                        case 1: 
                            return "C1"; 
                        case 2:
                            return "C2"; 
                        case 3:
                            return "C3";
                        case 4:
                            return "C4"; 
                        case 5:
                            return "C5"; 
                        case 6: 
                            return "C6";
                        default: 
                            System.Diagnostics.Debug.Fail("decimalPlacesUpDown should allow only up to 6 digits");
                            return "";
                    }
                } 
            }
 
            public override bool FormatStringTextBoxVisible 
            {
                get 
                {
                    return false;
                }
            } 

            public static bool ParseStatic(string formatString) 
            { 
                return formatString.Equals("C0") ||
                       formatString.Equals("C1") || 
                       formatString.Equals("C2") ||
                       formatString.Equals("C3") ||
                       formatString.Equals("C4") ||
                       formatString.Equals("C5") || 
                       formatString.Equals("C6");
            } 
 
            public override bool Parse(string formatString)
            { 
                return ParseStatic(formatString);
            }

            public override void PushFormatStringIntoFormatType(string formatString) 
            {
#if DEBUG 
                System.Diagnostics.Debug.Assert(Parse(formatString), "we only push valid strings"); 
#endif // DEBUG
                if (formatString.Equals("C0")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 0;
                }
                else if (formatString.Equals("C1")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 1; 
                } 
                else if (formatString.Equals("C2"))
                { 
                    this.owner.decimalPlacesUpDown.Value = 2;
                }
                else if (formatString.Equals("C3"))
                { 
                    this.owner.decimalPlacesUpDown.Value = 3;
                } 
                else if (formatString.Equals("C4")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 4; 
                }
                else if (formatString.Equals("C5"))
                {
                    this.owner.decimalPlacesUpDown.Value = 5; 
                }
                else if (formatString.Equals("C6")) 
                { 
                    this.owner.decimalPlacesUpDown.Value = 6;
                } 
            }


            public override string ToString() 
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeCurrency); 
            } 
        }
 
        private class DateTimeFormatType : FormatTypeClass
        {
            FormatControl owner;
            public DateTimeFormatType(FormatControl owner) 
            {
                this.owner = owner; 
            } 

            public override string TopLabelString 
            {
                get
                {
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeDateTimeExplanation); 
                }
            } 
            public override string SampleString 
            {
                get 
                {
                    if (this.owner.dateTimeFormatsListBox.SelectedItem == null)
                    {
                        return ""; 
                    }
 
                    return System.Windows.Forms.Design.FormatControl.dateTimeFormatValue.ToString(this.FormatString, CultureInfo.CurrentCulture); 
                }
            } 
            public override bool DropDownVisible
            {
                get
                { 
                    return false;
                } 
            } 
            public override bool ListBoxVisible
            { 
                get
                {
                    return true;
                } 
            }
 
            public override bool FormatLabelVisible 
            {
                get 
                {
                    return false;
                }
            } 

            public override string FormatString 
            { 
                get
                { 
                    DateTimeFormatsListBoxItem item = this.owner.dateTimeFormatsListBox.SelectedItem as DateTimeFormatsListBoxItem;
                    return item.FormatString;
                }
            } 

            public override bool FormatStringTextBoxVisible 
            { 
                get
                { 
                    return false;
                }
            }
 
            public static bool ParseStatic(string formatString)
            { 
                return formatString.Equals("d") || 
                       formatString.Equals("D") ||
                       formatString.Equals("f") || 
                       formatString.Equals("F") ||
                       formatString.Equals("g") ||
                       formatString.Equals("G") ||
                       formatString.Equals("t") || 
                       formatString.Equals("T") ||
                       formatString.Equals("M"); 
            } 

            public override bool Parse(string formatString) 
            {
                return ParseStatic(formatString);
            }
 
            public override void PushFormatStringIntoFormatType(string formatString)
            { 
#if DEBUG 
                System.Diagnostics.Debug.Assert(Parse(formatString), "we only push valid strings");
#endif // DEBUG 
                int selectedIndex = -1;
                if (formatString.Equals("d"))
                {
                    selectedIndex = 0; 
                }
                else if (formatString.Equals("D")) 
                { 
                    selectedIndex = 1;
                } 
                else if (formatString.Equals("f"))
                {
                    selectedIndex = 2;
                } 
                else if (formatString.Equals("F"))
                { 
                    selectedIndex = 3; 
                }
                else if (formatString.Equals("g")) 
                {
                    selectedIndex = 4;
                }
                else if (formatString.Equals("G")) 
                {
                    selectedIndex = 5; 
                } 
                else if (formatString.Equals("t"))
                { 
                    selectedIndex = 6;
                }
                else if (formatString.Equals("T"))
                { 
                    selectedIndex = 7;
                } 
                else if (formatString.Equals("M")) 
                {
                    selectedIndex = 8; 
                }

                this.owner.dateTimeFormatsListBox.SelectedIndex = selectedIndex;
            } 
            public override string ToString()
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeDateTime); 
            }
        } 

        private class ScientificFormatType : FormatTypeClass
        {
            FormatControl owner; 
            public ScientificFormatType(FormatControl owner)
            { 
                this.owner = owner; 
            }
 
            public override string TopLabelString
            {
                get
                { 
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeScientificExplanation);
                } 
            } 
            public override string SampleString
            { 
                get
                {
                    return (-1234.5678).ToString(this.FormatString, CultureInfo.CurrentCulture);
                } 
            }
            public override bool DropDownVisible 
            { 
                get
                { 
                    return true;
                }
            }
            public override bool ListBoxVisible 
            {
                get 
                { 
                    return false;
                } 
            }

            public override bool FormatLabelVisible
            { 
                get
                { 
                    return false; 
                }
            } 

            public override string FormatString
            {
                get 
                {
                    switch ((int)this.owner.decimalPlacesUpDown.Value) 
                    { 
                        case 0:
                            return "E0"; 
                        case 1:
                            return "E1";
                        case 2:
                            return "E2"; 
                        case 3:
                            return "E3"; 
                        case 4: 
                            return "E4";
                        case 5: 
                            return "E5";
                        case 6:
                            return "E6";
                        default: 
                            System.Diagnostics.Debug.Fail("decimalPlacesUpDown should allow only up to 6 digits");
                            return ""; 
                    } 
                }
            } 

            public override bool FormatStringTextBoxVisible
            {
                get 
                {
                    return false; 
                } 
            }
 
            public static bool ParseStatic(string formatString)
            {
                return formatString.Equals("E0") ||
                       formatString.Equals("E1") || 
                       formatString.Equals("E2") ||
                       formatString.Equals("E3") || 
                       formatString.Equals("E4") || 
                       formatString.Equals("E5") ||
                       formatString.Equals("E6"); 
            }

            public override bool Parse(string formatString)
            { 
                return ParseStatic(formatString);
            } 
 
            public override void PushFormatStringIntoFormatType(string formatString)
            { 
#if DEBUG
                System.Diagnostics.Debug.Assert(Parse(formatString), "we only push valid strings");
#endif // DEBUG
                if (formatString.Equals("E0")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 0; 
                } 
                else if (formatString.Equals("E1"))
                { 
                    this.owner.decimalPlacesUpDown.Value = 1;
                }
                else if (formatString.Equals("E2"))
                { 
                    this.owner.decimalPlacesUpDown.Value = 2;
                } 
                else if (formatString.Equals("E3")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 3; 
                }
                else if (formatString.Equals("E4"))
                {
                    this.owner.decimalPlacesUpDown.Value = 4; 
                }
                else if (formatString.Equals("E5")) 
                { 
                    this.owner.decimalPlacesUpDown.Value = 5;
                } 
                else if (formatString.Equals("E6"))
                {
                    this.owner.decimalPlacesUpDown.Value = 6;
                } 
            }
 
 
            public override string ToString()
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeScientific);
            }
        }
 
        private class CustomFormatType : FormatTypeClass
        { 
            FormatControl owner; 
            public CustomFormatType(FormatControl owner)
            { 
                this.owner = owner;
            }
            public override string TopLabelString
            { 
                get
                { 
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeCustomExplanation); 
                }
            } 
            public override string SampleString
            {
                get
                { 
                    string formatString = this.FormatString;
                    if (String.IsNullOrEmpty(formatString)) 
                    { 
                        return "";
                    } 

                    string label = "";

                    // first see if the formatString is one of DateTime's format strings 
                    if (System.Windows.Forms.Design.FormatControl.DateTimeFormatType.ParseStatic(formatString))
                    { 
                        label = System.Windows.Forms.Design.FormatControl.dateTimeFormatValue.ToString(formatString, CultureInfo.CurrentCulture); 
                    }
 
                    // the formatString was not one of DateTime's strings
                    // Try a double
                    if (label.Equals(""))
                    { 
                        try
                        { 
                            label = (-1234.5678).ToString(formatString, CultureInfo.CurrentCulture); 
                        }
                        catch (FormatException) 
                        {
                            label = "";
                        }
                    } 

                    // double failed. 
                    // Try an Int 
                    if (label.Equals(""))
                    { 
                        try
                        {
                            label = (-1234).ToString(formatString, CultureInfo.CurrentCulture);
                        } 
                        catch (FormatException)
                        { 
                            label = ""; 
                        }
                    } 

                    // int failed.
                    // apply the formatString to the dateTime value
                    if (label.Equals("")) 
                    {
                        try 
                        { 
                            label = System.Windows.Forms.Design.FormatControl.dateTimeFormatValue.ToString(formatString, CultureInfo.CurrentCulture);
                        } 
                        catch (FormatException)
                        {
                            label = "";
                        } 
                    }
 
                    if (label.Equals("")) 
                    {
                        label = SR.GetString(SR.BindingFormattingDialogFormatTypeCustomInvalidFormat); 
                    }

                    return label;
                } 
            }
            public override bool DropDownVisible 
            { 
                get
                { 
                    return false;
                }
            }
            public override bool ListBoxVisible 
            {
                get 
                { 
                    return false;
                } 
            }
            public override bool FormatStringTextBoxVisible
            {
                get 
                {
                    return true; 
                } 
            }
            public override bool FormatLabelVisible 
            {
                get
                {
                    return false; 
                }
            } 
            public override string FormatString 
            {
                get 
                {
                    return this.owner.customStringTextBox.Text;
                }
            } 

            public static bool ParseStatic(string formatString) 
            { 
                // anything goes...
                return true; 
            }

            public override bool Parse(string formatString)
            { 
                return ParseStatic(formatString);
            } 
 
            public override void PushFormatStringIntoFormatType(string formatString)
            { 
                this.owner.customStringTextBox.Text = formatString;
            }

            public override string ToString() 
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeCustom); 
            } 

        } 

        private void nullValueTextBox_TextChanged(object sender, EventArgs e)
        {
            this.dirty = true; 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
#region Using directives 

using System;
using System.Collections.Generic;
using System.ComponentModel; 
using System.Drawing;
using System.Data; 
using System.Text; 
using System.Windows.Forms;
using System.Design; 
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

#endregion 

namespace System.Windows.Forms.Design 
{ 
    internal partial class FormatControl : UserControl
    { 
        private const int NoFormattingIndex = 0;
        private const int NumericIndex = 1;
        private const int CurrencyIndex = 2;
        private const int DateTimeIndex = 3; 
        private const int ScientificIndex = 4;
        private const int CustomIndex = 5; 
 
        private TextBox customStringTextBox = new TextBox();
 
        // static because we want this value to be the same across a
        // VS session
        private static DateTime dateTimeFormatValue = System.DateTime.Now;
 
        private bool dirty = false;
        private bool loaded = false; 
 
        public FormatControl()
        { 
            InitializeComponent();
        }

        public bool Dirty 
        {
            get 
            { 
                return this.dirty;
            } 
            set
            {
                this.dirty = value;
            } 
        }
 
        public string FormatType 
        {
            get 
            {
                FormatTypeClass formatType = this.formatTypeListBox.SelectedItem as FormatTypeClass;
                if (formatType != null)
                { 
                    return formatType.ToString();
                } 
                else 
                {
                    return string.Empty; 
                }
            }
            set
            { 
                this.formatTypeListBox.SelectedIndex = 0;
                for (int i = 0; i < this.formatTypeListBox.Items.Count; i++) 
                { 
                    FormatTypeClass formatType = this.formatTypeListBox.Items[i] as FormatTypeClass;
                    if (formatType.ToString().Equals(value)) 
                    {
                        this.formatTypeListBox.SelectedIndex = i;
                    }
                } 
            }
        } 
 
        public FormatTypeClass FormatTypeItem
        { 
            get
            {
                return this.formatTypeListBox.SelectedItem as FormatTypeClass;
            } 
        }
 
        public string NullValue 
        {
            get 
            {
                // VSWhidbey#408448: If text box is empty or contains just whitespace, return 'null' for the NullValue.
                // Otherwise we end up pushing empty string as the NullValue for every binding, which breaks non-string
                // bindings. This does mean that setting the NullValue to empty string now becomes a code-only scenario 
                // for users, but that is an acceptible trade-off.
                // 
                String nullValue = this.nullValueTextBox.Text.Trim(); 
                return (nullValue.Length == 0) ? null : nullValue;
            } 
            set
            {
                this.nullValueTextBox.TextChanged -= new System.EventHandler(this.nullValueTextBox_TextChanged);
                this.nullValueTextBox.Text = value; 
                this.nullValueTextBox.TextChanged += new System.EventHandler(this.nullValueTextBox_TextChanged);
            } 
        } 

        public bool NullValueTextBoxEnabled 
        {
            set
            {
                this.nullValueTextBox.Enabled = value; 
            }
        } 
 
        void customStringTextBox_TextChanged(object sender, EventArgs e)
        { 
            CustomFormatType customFormatType = this.formatTypeListBox.SelectedItem as CustomFormatType;
            this.sampleLabel.Text = customFormatType.SampleString;
            this.dirty = true;
        } 

        private void dateTimeFormatsListBox_SelectedIndexChanged(object sender, System.EventArgs e) 
        { 
            // recompute the SampleLabel
            FormatTypeClass item = this.formatTypeListBox.SelectedItem as FormatTypeClass; 
            this.sampleLabel.Text = item.SampleString;
            this.dirty = true;
        }
 
        private void decimalPlacesUpDown_ValueChanged(object sender, EventArgs e)
        { 
            // update the sample label 
            FormatTypeClass item = this.formatTypeListBox.SelectedItem as FormatTypeClass;
            this.sampleLabel.Text = item.SampleString; 
            this.dirty = true;
        }

        private void formatGroupBox_Enter(object sender, EventArgs e) 
        {
        } 
 
        private void formatTypeListBox_SelectedIndexChanged(object sender, EventArgs e)
        { 
            FormatTypeClass formatType = this.formatTypeListBox.SelectedItem as FormatTypeClass;
            UpdateControlVisibility(formatType);
            this.sampleLabel.Text = formatType.SampleString;
            this.explanationLabel.Text = formatType.TopLabelString; 
            this.dirty = true;
        } 
 
        // given a formatString/formatInfo combination, this method suggest the most appropiate user control
        // the result of the function is one of the strings "Numeric", "Currency", "DateTime", "Percentage", "Scientific", "Custom" 
        public static string FormatTypeStringFromFormatString(string formatString)
        {
            if (String.IsNullOrEmpty(formatString))
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting);
            } 
            if (System.Windows.Forms.Design.FormatControl.NumericFormatType.ParseStatic(formatString)) 
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeNumeric); 
            }
            else if (System.Windows.Forms.Design.FormatControl.CurrencyFormatType.ParseStatic(formatString))
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeCurrency); 
            }
            else if (System.Windows.Forms.Design.FormatControl.DateTimeFormatType.ParseStatic(formatString)) 
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeDateTime);
            } 
            else if (System.Windows.Forms.Design.FormatControl.ScientificFormatType.ParseStatic(formatString))
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeScientific);
            } 
            else
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeCustom); 
            }
        } 

        protected override bool ProcessMnemonic(char charCode)
        {
            if (System.Windows.Forms.Control.IsMnemonic(charCode, this.formatTypeLabel.Text)) 
            {
                this.formatTypeListBox.Focus(); 
                return true; 
            }
 
            if (System.Windows.Forms.Control.IsMnemonic(charCode, this.nullValueLabel.Text))
            {
                this.nullValueTextBox.Focus();
                return true; 
            }
 
            int selIndex = this.formatTypeListBox.SelectedIndex; 
            switch (selIndex)
            { 
                case NoFormattingIndex:
                    return false;
                case ScientificIndex:
                    // FALL THRU 
                case CurrencyIndex:
                    // FALL THRU 
                case NumericIndex: 
                    System.Diagnostics.Debug.Assert(this.decimalPlacesUpDown.Visible);
                    if (System.Windows.Forms.Control.IsMnemonic(charCode, this.secondRowLabel.Text)) 
                    {
                        this.decimalPlacesUpDown.Focus();
                        return true;
                    } 
                    else
                    { 
                        return false; 
                    }
                case DateTimeIndex: 
                    System.Diagnostics.Debug.Assert(this.dateTimeFormatsListBox.Visible);
                    if (System.Windows.Forms.Control.IsMnemonic(charCode, this.secondRowLabel.Text))
                    {
                        this.dateTimeFormatsListBox.Focus(); 
                        return true;
                    } 
                    else 
                    {
                        return false; 
                    }
                case CustomIndex:
                    System.Diagnostics.Debug.Assert(this.customStringTextBox.Visible);
                    if (System.Windows.Forms.Control.IsMnemonic(charCode, this.secondRowLabel.Text)) 
                    {
                        this.customStringTextBox.Focus(); 
                        return true; 
                    }
                    else 
                    {
                        return false;
                    }
 
                default:
                    return false; 
            } 
        }
 
        public void ResetFormattingInfo()
        {
            this.decimalPlacesUpDown.ValueChanged -= new EventHandler(decimalPlacesUpDown_ValueChanged);
            this.customStringTextBox.TextChanged -= new EventHandler(customStringTextBox_TextChanged); 
            this.dateTimeFormatsListBox.SelectedIndexChanged -= new EventHandler(dateTimeFormatsListBox_SelectedIndexChanged);
            this.formatTypeListBox.SelectedIndexChanged -= new EventHandler(formatTypeListBox_SelectedIndexChanged); 
 
            this.decimalPlacesUpDown.Value = 2;
            this.nullValueTextBox.Text = String.Empty; 
            this.dateTimeFormatsListBox.SelectedIndex = -1;
            this.formatTypeListBox.SelectedIndex = -1;
            this.customStringTextBox.Text = String.Empty;
 
            this.decimalPlacesUpDown.ValueChanged += new EventHandler(decimalPlacesUpDown_ValueChanged);
            this.customStringTextBox.TextChanged += new EventHandler(customStringTextBox_TextChanged); 
            this.dateTimeFormatsListBox.SelectedIndexChanged += new EventHandler(dateTimeFormatsListBox_SelectedIndexChanged); 
            this.formatTypeListBox.SelectedIndexChanged += new EventHandler(formatTypeListBox_SelectedIndexChanged);
        } 

        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        private void UpdateControlVisibility(FormatTypeClass formatType)
        { 
            if (formatType == null)
            { 
                this.explanationLabel.Visible = false; 
                this.sampleLabel.Visible = false;
                this.nullValueLabel.Visible = false; 
                this.secondRowLabel.Visible = false;
                this.nullValueTextBox.Visible = false;
                this.thirdRowLabel.Visible = false;
                this.dateTimeFormatsListBox.Visible = false; 
                this.customStringTextBox.Visible = false;
                this.decimalPlacesUpDown.Visible = false; 
                return; 
            }
 
            this.tableLayoutPanel1.SuspendLayout();
            this.secondRowLabel.Text = "";

            // process the decimalPlacesLabelVisible 
            if (formatType.DropDownVisible)
            { 
                this.secondRowLabel.Text = SR.GetString(SR.BindingFormattingDialogDecimalPlaces); 
                this.decimalPlacesUpDown.Visible = true;
            } 
            else
            {
                this.decimalPlacesUpDown.Visible = false;
            } 

            // process customFormatLabelVisible 
            if (formatType.FormatStringTextBoxVisible) 
            {
                this.secondRowLabel.Text = SR.GetString(SR.BindingFormattingDialogCustomFormat); 
                this.thirdRowLabel.Visible = true;
                this.tableLayoutPanel1.SetColumn(this.thirdRowLabel, 0);
                this.tableLayoutPanel1.SetColumnSpan(this.thirdRowLabel, 2);
                this.customStringTextBox.Visible = true; 
                if (this.tableLayoutPanel1.Controls.Contains(this.dateTimeFormatsListBox))
                { 
                    this.tableLayoutPanel1.Controls.Remove(this.dateTimeFormatsListBox); 
                }
                this.tableLayoutPanel1.Controls.Add(this.customStringTextBox, 1, 1); 
            }
            else
            {
                this.thirdRowLabel.Visible = false; 
                this.customStringTextBox.Visible = false;
            } 
 
            if (formatType.ListBoxVisible)
            { 
                this.secondRowLabel.Text = SR.GetString(SR.BindingFormattingDialogType);

                if (this.tableLayoutPanel1.Controls.Contains(this.customStringTextBox))
                { 
                    this.tableLayoutPanel1.Controls.Remove(this.customStringTextBox);
                } 
 
                this.dateTimeFormatsListBox.Visible = true;
                this.tableLayoutPanel1.Controls.Add(this.dateTimeFormatsListBox, 0, 2); 
                this.tableLayoutPanel1.SetColumn(this.dateTimeFormatsListBox, 0);
                this.tableLayoutPanel1.SetColumnSpan(this.dateTimeFormatsListBox, 2);
            }
            else 
            {
                this.dateTimeFormatsListBox.Visible = false; 
            } 
            this.tableLayoutPanel1.ResumeLayout(true /*performLayout*/);
        } 

        private void UpdateCustomStringTextBox()
        {
            this.customStringTextBox = new TextBox(); 
            this.customStringTextBox.AccessibleDescription = SR.GetString(SR.BindingFormattingDialogCustomFormatAccessibleDescription);
            this.customStringTextBox.Margin = new Padding(0, 3, 0, 3); 
            this.customStringTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right; 
            this.customStringTextBox.TabIndex = 3;
            this.customStringTextBox.TextChanged += new EventHandler(customStringTextBox_TextChanged); 
        }

        private void UpdateFormatTypeListBoxHeight()
        { 
            // there seems to be a bug in layout because setting
            // the anchor on the list box does not work. 
            this.formatTypeListBox.Height = this.tableLayoutPanel1.Bottom - this.formatTypeListBox.Top; 
        }
 
        private void UpdateFormatTypeListBoxItems()
        {
            this.dateTimeFormatsListBox.SelectedIndexChanged -= new System.EventHandler(dateTimeFormatsListBox_SelectedIndexChanged);
            this.dateTimeFormatsListBox.Items.Clear(); 
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "d"));
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "D")); 
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "f")); 
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "F"));
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "g")); 
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "G"));
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "t"));
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "T"));
            this.dateTimeFormatsListBox.Items.Add(new DateTimeFormatsListBoxItem(dateTimeFormatValue, "M")); 
            this.dateTimeFormatsListBox.SelectedIndex = 0;
            this.dateTimeFormatsListBox.SelectedIndexChanged += new System.EventHandler(dateTimeFormatsListBox_SelectedIndexChanged); 
        } 

        private void UpdateTBLHeight() 
        {
            this.tableLayoutPanel1.SuspendLayout();

            // set up the customStringTextBox 
            this.tableLayoutPanel1.Controls.Add(this.customStringTextBox, 1, 1);
            this.customStringTextBox.Visible = false; 
 
            // set the thirdRowLabel
            this.thirdRowLabel.MaximumSize = new Size(this.tableLayoutPanel1.Width, 0); 
            this.dateTimeFormatsListBox.Visible = false;
            this.tableLayoutPanel1.SetColumn(this.thirdRowLabel, 0);
            this.tableLayoutPanel1.SetColumnSpan(this.thirdRowLabel, 2);
            this.thirdRowLabel.AutoSize = true; 
            this.tableLayoutPanel1.ResumeLayout(true /*performLayout*/);
 
            // Now that PerformLayout set the bounds for the tableLayoutPanel 
            // we can use these bounds to specify the tableLayoutPanel minimumSize.
            this.tableLayoutPanel1.MinimumSize = new System.Drawing.Size(this.tableLayoutPanel1.Width, this.tableLayoutPanel1.Height); 
        }

        private void FormatControl_Load(object sender, EventArgs e)
        { 
            if (this.loaded)
            { 
                // we already did the setup work 
                return;
            } 

            int minWidth, minHeight;
            this.nullValueLabel.Text = SR.GetString(SR.BindingFormattingDialogNullValue);
            minWidth = this.nullValueLabel.Width; 
            minHeight = this.nullValueLabel.Height;
 
            this.secondRowLabel.Text = SR.GetString(SR.BindingFormattingDialogDecimalPlaces); 
            minWidth = Math.Max(minWidth, this.secondRowLabel.Width);
            minHeight = Math.Max(minHeight, this.secondRowLabel.Height); 

            this.secondRowLabel.Text = SR.GetString(SR.BindingFormattingDialogCustomFormat);
            minWidth = Math.Max(minWidth, this.secondRowLabel.Width);
            minHeight = Math.Max(minHeight, this.secondRowLabel.Height); 

            this.nullValueLabel.MinimumSize = new Size(minWidth, minHeight); 
            this.secondRowLabel.MinimumSize = new Size(minWidth, minHeight); 

            // add items to the list box 
            this.formatTypeListBox.SelectedIndexChanged -= new EventHandler(this.formatTypeListBox_SelectedIndexChanged);
            this.formatTypeListBox.Items.Clear();
            this.formatTypeListBox.Items.Add(new NoFormattingFormatType());
            this.formatTypeListBox.Items.Add(new NumericFormatType(this)); 
            this.formatTypeListBox.Items.Add(new CurrencyFormatType(this));
            this.formatTypeListBox.Items.Add(new DateTimeFormatType(this)); 
            this.formatTypeListBox.Items.Add(new ScientificFormatType(this)); 
            this.formatTypeListBox.Items.Add(new CustomFormatType(this));
            this.formatTypeListBox.SelectedIndex = 0; 
            this.formatTypeListBox.SelectedIndexChanged += new EventHandler(this.formatTypeListBox_SelectedIndexChanged);

            UpdateCustomStringTextBox();
            UpdateTBLHeight(); 
            UpdateFormatTypeListBoxHeight();
            UpdateFormatTypeListBoxItems(); 
 
            UpdateControlVisibility(this.formatTypeListBox.SelectedItem as FormatTypeClass);
            this.sampleLabel.Text = ((this.formatTypeListBox.SelectedItem) as FormatTypeClass).SampleString; 
            this.explanationLabel.Size = new System.Drawing.Size(this.formatGroupBox.Width - 10, 30);
            this.explanationLabel.Text = ((this.formatTypeListBox.SelectedItem) as FormatTypeClass).TopLabelString;

            this.dirty = false; 

            FormatControlFinishedLoading(); 
 
            this.loaded = true;
        } 

        //
        // This method tells the BindingFormattingDialog and the FormatStringDialog
        // that the FormatControl is loaded and resized. 
        //
        // 
        private void FormatControlFinishedLoading() 
        {
            BindingFormattingDialog bfd = null; 
            FormatStringDialog fsd = null;

            //
            Control ctl = this.Parent; 
            while (ctl != null)
            { 
                bfd = ctl as BindingFormattingDialog; 
                fsd = ctl as FormatStringDialog;
                if (bfd != null || fsd != null) 
                {
                    break;
                }
                ctl = ctl.Parent; 
            }
 
            if (fsd != null) 
            {
                fsd.FormatControlFinishedLoading(); 
            }

        }
 
        private class DateTimeFormatsListBoxItem
        { 
            DateTime value; 
            string formatString;
            public DateTimeFormatsListBoxItem(DateTime value, string formatString) 
            {
                this.value = value;
                this.formatString = formatString;
            } 

            public string FormatString 
            { 
                get
                { 
                    return this.formatString;
                }
            }
 
            public override string ToString()
            { 
 	            return value.ToString(this.formatString, CultureInfo.CurrentCulture); 
            }
        } 

        internal abstract class FormatTypeClass
        {
            public abstract string TopLabelString { get;} 
            public abstract string SampleString { get;}
            public abstract bool DropDownVisible { get;} 
            public abstract bool ListBoxVisible { get;} 
            public abstract bool FormatStringTextBoxVisible { get;}
            public abstract bool FormatLabelVisible { get;} 
            public abstract string FormatString { get;}
            public abstract bool Parse(string formatString);
            public abstract void PushFormatStringIntoFormatType(string formatString);
        } 

        private class NoFormattingFormatType : FormatTypeClass 
        { 
            public override string TopLabelString
            { 
                get
                {
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormattingExplanation);
                } 
            }
            public override string SampleString 
            { 
                get
                { 
                    return "-1234.5";
                }
            }
            public override bool DropDownVisible 
            {
                get 
                { 
                    return false;
                } 
            }
            public override bool ListBoxVisible
            {
                get 
                {
                    return false; 
                } 
            }
 
            public override bool FormatLabelVisible
            {
                get
                { 
                    return false;
                } 
            } 

            public override string FormatString 
            {
                get
                {
                    return ""; 
                }
            } 
 
            public override bool FormatStringTextBoxVisible
            { 
                get
                {
                    return false;
                } 
            }
 
            public override bool Parse(string formatString) 
            {
                return false; 
            }

            public override void PushFormatStringIntoFormatType(string formatString)
            { 
                // nothing to do;
            } 
 
            public override string ToString()
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting);
            }
        }
 
        private class NumericFormatType : FormatTypeClass
        { 
            FormatControl owner; 
            public NumericFormatType(FormatControl owner)
            { 
                this.owner = owner;
            }

            public override string TopLabelString 
            {
                get 
                { 
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeNumericExplanation);
                } 
            }
            public override string SampleString
            {
                get 
                {
                    return (-1234.5678).ToString(this.FormatString, CultureInfo.CurrentCulture); 
                } 
            }
            public override bool DropDownVisible 
            {
                get
                {
                    return true; 
                }
            } 
            public override bool ListBoxVisible 
            {
                get 
                {
                    return false;
                }
            } 

            public override bool FormatLabelVisible 
            { 
                get
                { 
                    return false;
                }
            }
 
            public override string FormatString
            { 
                get 
                {
                    switch ((int)this.owner.decimalPlacesUpDown.Value) 
                    {
                        case 0:
                            return "N0";
                        case 1: 
                            return "N1";
                        case 2: 
                            return "N2"; 
                        case 3:
                            return "N3"; 
                        case 4:
                            return "N4";
                        case 5:
                            return "N5"; 
                        case 6:
                            return "N6"; 
                        default: 
                            System.Diagnostics.Debug.Fail("decimalPlacesUpDown should allow only up to 6 digits");
                            return ""; 
                    }
                }
            }
 
            public override bool FormatStringTextBoxVisible
            { 
                get 
                {
                    return false; 
                }
            }

            public static bool ParseStatic(string formatString) 
            {
                return formatString.Equals("N0") || 
                       formatString.Equals("N1") || 
                       formatString.Equals("N2") ||
                       formatString.Equals("N3") || 
                       formatString.Equals("N4") ||
                       formatString.Equals("N5") ||
                       formatString.Equals("N6");
            } 

            public override bool Parse(string formatString) 
            { 
                return ParseStatic(formatString);
            } 

            public override void PushFormatStringIntoFormatType(string formatString)
            {
#if DEBUG 
                System.Diagnostics.Debug.Assert(Parse(formatString), "we only push valid strings");
#endif // DEBUG 
                if (formatString.Equals("N0")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 0; 
                }
                else if (formatString.Equals("N1"))
                {
                    this.owner.decimalPlacesUpDown.Value = 1; 
                }
                else if (formatString.Equals("N2")) 
                { 
                    this.owner.decimalPlacesUpDown.Value = 2;
                } 
                else if (formatString.Equals("N3"))
                {
                    this.owner.decimalPlacesUpDown.Value = 3;
                } 
                else if (formatString.Equals("N4"))
                { 
                    this.owner.decimalPlacesUpDown.Value = 4; 
                }
                else if (formatString.Equals("N5")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 5;
                }
                else if (formatString.Equals("N6")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 6; 
                } 
            }
 
            public override string ToString()
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeNumeric);
            } 
        }
 
        private class CurrencyFormatType : FormatTypeClass 
        {
            FormatControl owner; 
            public CurrencyFormatType(FormatControl owner)
            {
                this.owner = owner;
            } 

            public override string TopLabelString 
            { 
                get
                { 
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeCurrencyExplanation);
                }
            }
            public override string SampleString 
            {
                get 
                { 
                    return (-1234.5678).ToString(this.FormatString, CultureInfo.CurrentCulture);
                } 
            }
            public override bool DropDownVisible
            {
                get 
                {
                    return true; 
                } 
            }
            public override bool ListBoxVisible 
            {
                get
                {
                    return false; 
                }
            } 
 
            public override bool FormatLabelVisible
            { 
                get
                {
                    return false;
                } 
            }
 
            public override string FormatString 
            {
                get 
                {
                    switch ((int)this.owner.decimalPlacesUpDown.Value)
                    {
                        case 0: 
                            return "C0";
                        case 1: 
                            return "C1"; 
                        case 2:
                            return "C2"; 
                        case 3:
                            return "C3";
                        case 4:
                            return "C4"; 
                        case 5:
                            return "C5"; 
                        case 6: 
                            return "C6";
                        default: 
                            System.Diagnostics.Debug.Fail("decimalPlacesUpDown should allow only up to 6 digits");
                            return "";
                    }
                } 
            }
 
            public override bool FormatStringTextBoxVisible 
            {
                get 
                {
                    return false;
                }
            } 

            public static bool ParseStatic(string formatString) 
            { 
                return formatString.Equals("C0") ||
                       formatString.Equals("C1") || 
                       formatString.Equals("C2") ||
                       formatString.Equals("C3") ||
                       formatString.Equals("C4") ||
                       formatString.Equals("C5") || 
                       formatString.Equals("C6");
            } 
 
            public override bool Parse(string formatString)
            { 
                return ParseStatic(formatString);
            }

            public override void PushFormatStringIntoFormatType(string formatString) 
            {
#if DEBUG 
                System.Diagnostics.Debug.Assert(Parse(formatString), "we only push valid strings"); 
#endif // DEBUG
                if (formatString.Equals("C0")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 0;
                }
                else if (formatString.Equals("C1")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 1; 
                } 
                else if (formatString.Equals("C2"))
                { 
                    this.owner.decimalPlacesUpDown.Value = 2;
                }
                else if (formatString.Equals("C3"))
                { 
                    this.owner.decimalPlacesUpDown.Value = 3;
                } 
                else if (formatString.Equals("C4")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 4; 
                }
                else if (formatString.Equals("C5"))
                {
                    this.owner.decimalPlacesUpDown.Value = 5; 
                }
                else if (formatString.Equals("C6")) 
                { 
                    this.owner.decimalPlacesUpDown.Value = 6;
                } 
            }


            public override string ToString() 
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeCurrency); 
            } 
        }
 
        private class DateTimeFormatType : FormatTypeClass
        {
            FormatControl owner;
            public DateTimeFormatType(FormatControl owner) 
            {
                this.owner = owner; 
            } 

            public override string TopLabelString 
            {
                get
                {
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeDateTimeExplanation); 
                }
            } 
            public override string SampleString 
            {
                get 
                {
                    if (this.owner.dateTimeFormatsListBox.SelectedItem == null)
                    {
                        return ""; 
                    }
 
                    return System.Windows.Forms.Design.FormatControl.dateTimeFormatValue.ToString(this.FormatString, CultureInfo.CurrentCulture); 
                }
            } 
            public override bool DropDownVisible
            {
                get
                { 
                    return false;
                } 
            } 
            public override bool ListBoxVisible
            { 
                get
                {
                    return true;
                } 
            }
 
            public override bool FormatLabelVisible 
            {
                get 
                {
                    return false;
                }
            } 

            public override string FormatString 
            { 
                get
                { 
                    DateTimeFormatsListBoxItem item = this.owner.dateTimeFormatsListBox.SelectedItem as DateTimeFormatsListBoxItem;
                    return item.FormatString;
                }
            } 

            public override bool FormatStringTextBoxVisible 
            { 
                get
                { 
                    return false;
                }
            }
 
            public static bool ParseStatic(string formatString)
            { 
                return formatString.Equals("d") || 
                       formatString.Equals("D") ||
                       formatString.Equals("f") || 
                       formatString.Equals("F") ||
                       formatString.Equals("g") ||
                       formatString.Equals("G") ||
                       formatString.Equals("t") || 
                       formatString.Equals("T") ||
                       formatString.Equals("M"); 
            } 

            public override bool Parse(string formatString) 
            {
                return ParseStatic(formatString);
            }
 
            public override void PushFormatStringIntoFormatType(string formatString)
            { 
#if DEBUG 
                System.Diagnostics.Debug.Assert(Parse(formatString), "we only push valid strings");
#endif // DEBUG 
                int selectedIndex = -1;
                if (formatString.Equals("d"))
                {
                    selectedIndex = 0; 
                }
                else if (formatString.Equals("D")) 
                { 
                    selectedIndex = 1;
                } 
                else if (formatString.Equals("f"))
                {
                    selectedIndex = 2;
                } 
                else if (formatString.Equals("F"))
                { 
                    selectedIndex = 3; 
                }
                else if (formatString.Equals("g")) 
                {
                    selectedIndex = 4;
                }
                else if (formatString.Equals("G")) 
                {
                    selectedIndex = 5; 
                } 
                else if (formatString.Equals("t"))
                { 
                    selectedIndex = 6;
                }
                else if (formatString.Equals("T"))
                { 
                    selectedIndex = 7;
                } 
                else if (formatString.Equals("M")) 
                {
                    selectedIndex = 8; 
                }

                this.owner.dateTimeFormatsListBox.SelectedIndex = selectedIndex;
            } 
            public override string ToString()
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeDateTime); 
            }
        } 

        private class ScientificFormatType : FormatTypeClass
        {
            FormatControl owner; 
            public ScientificFormatType(FormatControl owner)
            { 
                this.owner = owner; 
            }
 
            public override string TopLabelString
            {
                get
                { 
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeScientificExplanation);
                } 
            } 
            public override string SampleString
            { 
                get
                {
                    return (-1234.5678).ToString(this.FormatString, CultureInfo.CurrentCulture);
                } 
            }
            public override bool DropDownVisible 
            { 
                get
                { 
                    return true;
                }
            }
            public override bool ListBoxVisible 
            {
                get 
                { 
                    return false;
                } 
            }

            public override bool FormatLabelVisible
            { 
                get
                { 
                    return false; 
                }
            } 

            public override string FormatString
            {
                get 
                {
                    switch ((int)this.owner.decimalPlacesUpDown.Value) 
                    { 
                        case 0:
                            return "E0"; 
                        case 1:
                            return "E1";
                        case 2:
                            return "E2"; 
                        case 3:
                            return "E3"; 
                        case 4: 
                            return "E4";
                        case 5: 
                            return "E5";
                        case 6:
                            return "E6";
                        default: 
                            System.Diagnostics.Debug.Fail("decimalPlacesUpDown should allow only up to 6 digits");
                            return ""; 
                    } 
                }
            } 

            public override bool FormatStringTextBoxVisible
            {
                get 
                {
                    return false; 
                } 
            }
 
            public static bool ParseStatic(string formatString)
            {
                return formatString.Equals("E0") ||
                       formatString.Equals("E1") || 
                       formatString.Equals("E2") ||
                       formatString.Equals("E3") || 
                       formatString.Equals("E4") || 
                       formatString.Equals("E5") ||
                       formatString.Equals("E6"); 
            }

            public override bool Parse(string formatString)
            { 
                return ParseStatic(formatString);
            } 
 
            public override void PushFormatStringIntoFormatType(string formatString)
            { 
#if DEBUG
                System.Diagnostics.Debug.Assert(Parse(formatString), "we only push valid strings");
#endif // DEBUG
                if (formatString.Equals("E0")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 0; 
                } 
                else if (formatString.Equals("E1"))
                { 
                    this.owner.decimalPlacesUpDown.Value = 1;
                }
                else if (formatString.Equals("E2"))
                { 
                    this.owner.decimalPlacesUpDown.Value = 2;
                } 
                else if (formatString.Equals("E3")) 
                {
                    this.owner.decimalPlacesUpDown.Value = 3; 
                }
                else if (formatString.Equals("E4"))
                {
                    this.owner.decimalPlacesUpDown.Value = 4; 
                }
                else if (formatString.Equals("E5")) 
                { 
                    this.owner.decimalPlacesUpDown.Value = 5;
                } 
                else if (formatString.Equals("E6"))
                {
                    this.owner.decimalPlacesUpDown.Value = 6;
                } 
            }
 
 
            public override string ToString()
            { 
                return SR.GetString(SR.BindingFormattingDialogFormatTypeScientific);
            }
        }
 
        private class CustomFormatType : FormatTypeClass
        { 
            FormatControl owner; 
            public CustomFormatType(FormatControl owner)
            { 
                this.owner = owner;
            }
            public override string TopLabelString
            { 
                get
                { 
                    return SR.GetString(SR.BindingFormattingDialogFormatTypeCustomExplanation); 
                }
            } 
            public override string SampleString
            {
                get
                { 
                    string formatString = this.FormatString;
                    if (String.IsNullOrEmpty(formatString)) 
                    { 
                        return "";
                    } 

                    string label = "";

                    // first see if the formatString is one of DateTime's format strings 
                    if (System.Windows.Forms.Design.FormatControl.DateTimeFormatType.ParseStatic(formatString))
                    { 
                        label = System.Windows.Forms.Design.FormatControl.dateTimeFormatValue.ToString(formatString, CultureInfo.CurrentCulture); 
                    }
 
                    // the formatString was not one of DateTime's strings
                    // Try a double
                    if (label.Equals(""))
                    { 
                        try
                        { 
                            label = (-1234.5678).ToString(formatString, CultureInfo.CurrentCulture); 
                        }
                        catch (FormatException) 
                        {
                            label = "";
                        }
                    } 

                    // double failed. 
                    // Try an Int 
                    if (label.Equals(""))
                    { 
                        try
                        {
                            label = (-1234).ToString(formatString, CultureInfo.CurrentCulture);
                        } 
                        catch (FormatException)
                        { 
                            label = ""; 
                        }
                    } 

                    // int failed.
                    // apply the formatString to the dateTime value
                    if (label.Equals("")) 
                    {
                        try 
                        { 
                            label = System.Windows.Forms.Design.FormatControl.dateTimeFormatValue.ToString(formatString, CultureInfo.CurrentCulture);
                        } 
                        catch (FormatException)
                        {
                            label = "";
                        } 
                    }
 
                    if (label.Equals("")) 
                    {
                        label = SR.GetString(SR.BindingFormattingDialogFormatTypeCustomInvalidFormat); 
                    }

                    return label;
                } 
            }
            public override bool DropDownVisible 
            { 
                get
                { 
                    return false;
                }
            }
            public override bool ListBoxVisible 
            {
                get 
                { 
                    return false;
                } 
            }
            public override bool FormatStringTextBoxVisible
            {
                get 
                {
                    return true; 
                } 
            }
            public override bool FormatLabelVisible 
            {
                get
                {
                    return false; 
                }
            } 
            public override string FormatString 
            {
                get 
                {
                    return this.owner.customStringTextBox.Text;
                }
            } 

            public static bool ParseStatic(string formatString) 
            { 
                // anything goes...
                return true; 
            }

            public override bool Parse(string formatString)
            { 
                return ParseStatic(formatString);
            } 
 
            public override void PushFormatStringIntoFormatType(string formatString)
            { 
                this.owner.customStringTextBox.Text = formatString;
            }

            public override string ToString() 
            {
                return SR.GetString(SR.BindingFormattingDialogFormatTypeCustom); 
            } 

        } 

        private void nullValueTextBox_TextChanged(object sender, EventArgs e)
        {
            this.dirty = true; 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
