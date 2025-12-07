//------------------------------------------------------------------------------ 
// <copyright file="NumberEdit.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

// NumberEdit.cs 
// 
// 3/18/99: [....]: created
// 

namespace System.Web.UI.Design.Util {
    using System.ComponentModel;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization; 
    using System.Runtime.Serialization.Formatters;
    using System.Windows.Forms; 

    /// <include file='doc\NumberEdit.uex' path='docs/doc[@for="NumberEdit"]/*' />
    /// <devdoc>
    ///    Provides an edit control that only accepts numbers with addition 
    ///    restrictions such as whether negatives and decimals are allowed
    /// </devdoc> 
    /// <internalonly/> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class NumberEdit : TextBox { 
        private bool allowNegative = true;
        private bool allowDecimal = true;

        /// <include file='doc\NumberEdit.uex' path='docs/doc[@for="NumberEdit.AllowDecimal"]/*' /> 
        /// <devdoc>
        ///    Controls whether the edit control allows negative values 
        /// </devdoc> 
        public bool AllowDecimal {
            get { 
                return allowDecimal;
            }
            set {
                allowDecimal = value; 
            }
        } 
 
        /// <include file='doc\NumberEdit.uex' path='docs/doc[@for="NumberEdit.AllowNegative"]/*' />
        /// <devdoc> 
        ///    Controls whether the edit control allows negative values
        /// </devdoc>
        public bool AllowNegative {
            get { 
                return allowNegative;
            } 
            set { 
                allowNegative = value;
            } 
        }


        /// <include file='doc\NumberEdit.uex' path='docs/doc[@for="NumberEdit.WndProc"]/*' /> 
        /// <devdoc>
        ///    Override of wndProc to listen to WM_CHAR and filter out invalid 
        ///    key strokes. Valid keystrokes are: 
        ///    0...9,
        ///    '.' (if fractions allowed), 
        ///    '-' (if negative allowed),
        ///    BKSP.
        ///    A beep is generated for invalid keystrokes
        /// </devdoc> 
        protected override void WndProc(ref Message m) {
            if (m.Msg == NativeMethods.WM_CHAR) { 
                char ch = (char)m.WParam; 
                if (!(((ch >= '0') && (ch <= '9')) ||
                      (NumberFormatInfo.CurrentInfo.NumberDecimalSeparator.Contains(ch.ToString(CultureInfo.CurrentCulture)) && allowDecimal) || 
                      (NumberFormatInfo.CurrentInfo.NegativeSign.Contains(ch.ToString(CultureInfo.CurrentCulture)) && allowNegative) ||
                      (ch == (char)8))) {
                    System.Console.Beep();
                    return; 
                }
            } 
            base.WndProc(ref m); 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="NumberEdit.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

// NumberEdit.cs 
// 
// 3/18/99: [....]: created
// 

namespace System.Web.UI.Design.Util {
    using System.ComponentModel;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization; 
    using System.Runtime.Serialization.Formatters;
    using System.Windows.Forms; 

    /// <include file='doc\NumberEdit.uex' path='docs/doc[@for="NumberEdit"]/*' />
    /// <devdoc>
    ///    Provides an edit control that only accepts numbers with addition 
    ///    restrictions such as whether negatives and decimals are allowed
    /// </devdoc> 
    /// <internalonly/> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class NumberEdit : TextBox { 
        private bool allowNegative = true;
        private bool allowDecimal = true;

        /// <include file='doc\NumberEdit.uex' path='docs/doc[@for="NumberEdit.AllowDecimal"]/*' /> 
        /// <devdoc>
        ///    Controls whether the edit control allows negative values 
        /// </devdoc> 
        public bool AllowDecimal {
            get { 
                return allowDecimal;
            }
            set {
                allowDecimal = value; 
            }
        } 
 
        /// <include file='doc\NumberEdit.uex' path='docs/doc[@for="NumberEdit.AllowNegative"]/*' />
        /// <devdoc> 
        ///    Controls whether the edit control allows negative values
        /// </devdoc>
        public bool AllowNegative {
            get { 
                return allowNegative;
            } 
            set { 
                allowNegative = value;
            } 
        }


        /// <include file='doc\NumberEdit.uex' path='docs/doc[@for="NumberEdit.WndProc"]/*' /> 
        /// <devdoc>
        ///    Override of wndProc to listen to WM_CHAR and filter out invalid 
        ///    key strokes. Valid keystrokes are: 
        ///    0...9,
        ///    '.' (if fractions allowed), 
        ///    '-' (if negative allowed),
        ///    BKSP.
        ///    A beep is generated for invalid keystrokes
        /// </devdoc> 
        protected override void WndProc(ref Message m) {
            if (m.Msg == NativeMethods.WM_CHAR) { 
                char ch = (char)m.WParam; 
                if (!(((ch >= '0') && (ch <= '9')) ||
                      (NumberFormatInfo.CurrentInfo.NumberDecimalSeparator.Contains(ch.ToString(CultureInfo.CurrentCulture)) && allowDecimal) || 
                      (NumberFormatInfo.CurrentInfo.NegativeSign.Contains(ch.ToString(CultureInfo.CurrentCulture)) && allowNegative) ||
                      (ch == (char)8))) {
                    System.Console.Beep();
                    return; 
                }
            } 
            base.WndProc(ref m); 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
