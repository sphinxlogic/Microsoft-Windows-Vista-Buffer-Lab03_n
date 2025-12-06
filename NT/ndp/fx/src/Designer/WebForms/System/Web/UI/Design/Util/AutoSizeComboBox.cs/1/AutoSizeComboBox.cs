//------------------------------------------------------------------------------ 
// <copyright file="ComboBoxHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.Util { 
 
    using System;
    using System.Design; 
    using System.Drawing;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
 
    /// <devdoc>
    /// Helper class to automatically resize the dropdown part of a ComboBox 
    /// to fit the widest item. 
    /// Note that if you modify the Items collection of the control, you will
    /// have to call InvalidateDropDownWidth so that it will be auto-resized 
    /// the next time the combobox is dropped down.
    /// </devdoc>
    internal sealed class AutoSizeComboBox : ComboBox {
        private const int MaxDropDownWidth = 600; 

        private bool _dropDownWidthValid; 
 
        private void AutoSizeComboBoxDropDown() {
            int maxWidth = 0; 
            using (Graphics g = Graphics.FromImage(new Bitmap(1, 1))) {
                foreach (object o in Items) {
                    if (o != null) {
                        Size size = g.MeasureString(o.ToString(), Font, 0, new StringFormat(StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox)).ToSize(); 
                        maxWidth = Math.Max(maxWidth, size.Width);
                        if (maxWidth >= MaxDropDownWidth) { 
                            maxWidth = MaxDropDownWidth; 
                            break;
                        } 
                    }
                }
            }
            int newWidth = maxWidth + SystemInformation.VerticalScrollBarWidth + 2 * SystemInformation.BorderSize.Width; 
            // This is a cheap hack to workaround the fact that the WinForms ComboBox
            // doesn't send a CB_SETDROPPEDWIDTH message to the control unless the value 
            // has changed. We have to always send the message since the effective value 
            // of the drop down width may have changed due to the width of the combobox
            // itself changing, and we want the new value. 
            DropDownWidth = newWidth + 1;
            DropDownWidth = newWidth;
        }
 
        public void InvalidateDropDownWidth() {
            _dropDownWidthValid = false; 
        } 

        protected override void OnDropDown(EventArgs e) { 
            if (!_dropDownWidthValid) {
                AutoSizeComboBoxDropDown();
                _dropDownWidthValid = true;
            } 
            base.OnDropDown(e);
        } 
 
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e); 

            _dropDownWidthValid = false;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ComboBoxHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.Util { 
 
    using System;
    using System.Design; 
    using System.Drawing;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
 
    /// <devdoc>
    /// Helper class to automatically resize the dropdown part of a ComboBox 
    /// to fit the widest item. 
    /// Note that if you modify the Items collection of the control, you will
    /// have to call InvalidateDropDownWidth so that it will be auto-resized 
    /// the next time the combobox is dropped down.
    /// </devdoc>
    internal sealed class AutoSizeComboBox : ComboBox {
        private const int MaxDropDownWidth = 600; 

        private bool _dropDownWidthValid; 
 
        private void AutoSizeComboBoxDropDown() {
            int maxWidth = 0; 
            using (Graphics g = Graphics.FromImage(new Bitmap(1, 1))) {
                foreach (object o in Items) {
                    if (o != null) {
                        Size size = g.MeasureString(o.ToString(), Font, 0, new StringFormat(StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox)).ToSize(); 
                        maxWidth = Math.Max(maxWidth, size.Width);
                        if (maxWidth >= MaxDropDownWidth) { 
                            maxWidth = MaxDropDownWidth; 
                            break;
                        } 
                    }
                }
            }
            int newWidth = maxWidth + SystemInformation.VerticalScrollBarWidth + 2 * SystemInformation.BorderSize.Width; 
            // This is a cheap hack to workaround the fact that the WinForms ComboBox
            // doesn't send a CB_SETDROPPEDWIDTH message to the control unless the value 
            // has changed. We have to always send the message since the effective value 
            // of the drop down width may have changed due to the width of the combobox
            // itself changing, and we want the new value. 
            DropDownWidth = newWidth + 1;
            DropDownWidth = newWidth;
        }
 
        public void InvalidateDropDownWidth() {
            _dropDownWidthValid = false; 
        } 

        protected override void OnDropDown(EventArgs e) { 
            if (!_dropDownWidthValid) {
                AutoSizeComboBoxDropDown();
                _dropDownWidthValid = true;
            } 
            base.OnDropDown(e);
        } 
 
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e); 

            _dropDownWidthValid = false;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
