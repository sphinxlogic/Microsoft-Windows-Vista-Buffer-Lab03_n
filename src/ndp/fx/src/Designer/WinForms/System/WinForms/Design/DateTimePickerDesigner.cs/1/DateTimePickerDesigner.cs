//------------------------------------------------------------------------------ 
// <copyright file="DateTimePickerDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.DateTimePickerDesigner..ctor()")] 

namespace System.Windows.Forms.Design {
    using System.ComponentModel;
    using System.Diagnostics; 
    using System;
    using System.ComponentModel.Design; 
    using System.Windows.Forms; 
    using System.Drawing;
    using Microsoft.Win32; 
    using System.Windows.Forms.Design.Behavior;
    using System.Collections;

 
    /// <include file='doc\DateTimePickerDesigner.uex' path='docs/doc[@for="DateTimePickerDesigner"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides rich design time behavior for the
    ///       DateTimePicker control. 
    ///    </para>
    /// </devdoc>
    internal class DateTimePickerDesigner : ControlDesigner {
 

        public DateTimePickerDesigner() { 
            AutoResizeHandles = true; 
        }
 
        /// <include file='doc\DateTimePickerDesigner.uex' path='docs/doc[@for="DateTimePickerDesigner.SelectionRules"]/*' />
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public override SelectionRules SelectionRules { 
            get {
                SelectionRules rules = base.SelectionRules; 
                rules &= ~(SelectionRules.TopSizeable | SelectionRules.BottomSizeable);
                return rules;
            }
        } 

        /// <include file='doc\DateTimePickerDesigner.uex' path='docs/doc[@for="DateTimePickerDesigner.SnapLines"]/*' /> 
        /// <devdoc> 
        ///     Adds a baseline SnapLine to the list of SnapLines related
        ///     to this control. 
        /// </devdoc>
        public override IList SnapLines {
            get {
                ArrayList snapLines = base.SnapLines as ArrayList; 

                //a single text-baseline for the label (and linklabel) control 
                int baseline = DesignerUtils.GetTextBaseline(Control, System.Drawing.ContentAlignment.MiddleLeft); 
                // DateTimePicker doesn't have an alignment, so we use MiddleLeft and add a fudge-factor
                baseline += 2; 
                snapLines.Add(new SnapLine(SnapLineType.Baseline, baseline, SnapLinePriority.Medium));

                return snapLines;
            } 
        }
 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DateTimePickerDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.DateTimePickerDesigner..ctor()")] 

namespace System.Windows.Forms.Design {
    using System.ComponentModel;
    using System.Diagnostics; 
    using System;
    using System.ComponentModel.Design; 
    using System.Windows.Forms; 
    using System.Drawing;
    using Microsoft.Win32; 
    using System.Windows.Forms.Design.Behavior;
    using System.Collections;

 
    /// <include file='doc\DateTimePickerDesigner.uex' path='docs/doc[@for="DateTimePickerDesigner"]/*' />
    /// <devdoc> 
    ///    <para> 
    ///       Provides rich design time behavior for the
    ///       DateTimePicker control. 
    ///    </para>
    /// </devdoc>
    internal class DateTimePickerDesigner : ControlDesigner {
 

        public DateTimePickerDesigner() { 
            AutoResizeHandles = true; 
        }
 
        /// <include file='doc\DateTimePickerDesigner.uex' path='docs/doc[@for="DateTimePickerDesigner.SelectionRules"]/*' />
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public override SelectionRules SelectionRules { 
            get {
                SelectionRules rules = base.SelectionRules; 
                rules &= ~(SelectionRules.TopSizeable | SelectionRules.BottomSizeable);
                return rules;
            }
        } 

        /// <include file='doc\DateTimePickerDesigner.uex' path='docs/doc[@for="DateTimePickerDesigner.SnapLines"]/*' /> 
        /// <devdoc> 
        ///     Adds a baseline SnapLine to the list of SnapLines related
        ///     to this control. 
        /// </devdoc>
        public override IList SnapLines {
            get {
                ArrayList snapLines = base.SnapLines as ArrayList; 

                //a single text-baseline for the label (and linklabel) control 
                int baseline = DesignerUtils.GetTextBaseline(Control, System.Drawing.ContentAlignment.MiddleLeft); 
                // DateTimePicker doesn't have an alignment, so we use MiddleLeft and add a fudge-factor
                baseline += 2; 
                snapLines.Add(new SnapLine(SnapLineType.Baseline, baseline, SnapLinePriority.Medium));

                return snapLines;
            } 
        }
 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
