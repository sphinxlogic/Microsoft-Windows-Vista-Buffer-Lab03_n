 
//------------------------------------------------------------------------------
// <copyright file="UpDownBaseDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
 
 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.UpDownBaseDesigner..ctor()")]

namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Windows.Forms.Design.Behavior;
 
    /// <include file='doc\UpDownBaseDesigner.uex' path='docs/doc[@for="UpDownBaseDesigner"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides a designer that can design components 
    ///       that extend UpDownBase.</para>
    /// </devdoc> 
    internal class UpDownBaseDesigner : ControlDesigner { 

        public UpDownBaseDesigner() { 
            AutoResizeHandles = true;
        }

        /// <include file='doc\UpDownBaseDesigner.uex' path='docs/doc[@for="UpDownBaseDesigner.SelectionRules"]/*' /> 
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

        /// <include file='doc\UpDownBaseDesigner.uex' path='docs/doc[@for="UpDownBaseDesigner.SnapLines"]/*' /> 
        /// <devdoc>
        ///     Adds a baseline SnapLine to the list of SnapLines related to this control.
        /// </devdoc>
        public override IList SnapLines { 
            get {
                ArrayList snapLines = base.SnapLines as ArrayList; 
 
                int baseline = DesignerUtils.GetTextBaseline(Control, System.Drawing.ContentAlignment.TopLeft);
 
                BorderStyle borderStyle = BorderStyle.Fixed3D;
                PropertyDescriptor prop = TypeDescriptor.GetProperties(Component)["BorderStyle"];
                if (prop != null) {
                    borderStyle = (BorderStyle)prop.GetValue(Component); 
                }
 
                if (borderStyle == BorderStyle.None) { 
                    baseline -= 1;
                } 
                else {
                    baseline += 2;
                }
 

                snapLines.Add(new SnapLine(SnapLineType.Baseline, baseline, SnapLinePriority.Medium)); 
 
                return snapLines;
            } 
        }

    }
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="UpDownBaseDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
 
 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.UpDownBaseDesigner..ctor()")]

namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Windows.Forms.Design.Behavior;
 
    /// <include file='doc\UpDownBaseDesigner.uex' path='docs/doc[@for="UpDownBaseDesigner"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides a designer that can design components 
    ///       that extend UpDownBase.</para>
    /// </devdoc> 
    internal class UpDownBaseDesigner : ControlDesigner { 

        public UpDownBaseDesigner() { 
            AutoResizeHandles = true;
        }

        /// <include file='doc\UpDownBaseDesigner.uex' path='docs/doc[@for="UpDownBaseDesigner.SelectionRules"]/*' /> 
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

        /// <include file='doc\UpDownBaseDesigner.uex' path='docs/doc[@for="UpDownBaseDesigner.SnapLines"]/*' /> 
        /// <devdoc>
        ///     Adds a baseline SnapLine to the list of SnapLines related to this control.
        /// </devdoc>
        public override IList SnapLines { 
            get {
                ArrayList snapLines = base.SnapLines as ArrayList; 
 
                int baseline = DesignerUtils.GetTextBaseline(Control, System.Drawing.ContentAlignment.TopLeft);
 
                BorderStyle borderStyle = BorderStyle.Fixed3D;
                PropertyDescriptor prop = TypeDescriptor.GetProperties(Component)["BorderStyle"];
                if (prop != null) {
                    borderStyle = (BorderStyle)prop.GetValue(Component); 
                }
 
                if (borderStyle == BorderStyle.None) { 
                    baseline -= 1;
                } 
                else {
                    baseline += 2;
                }
 

                snapLines.Add(new SnapLine(SnapLineType.Baseline, baseline, SnapLinePriority.Medium)); 
 
                return snapLines;
            } 
        }

    }
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
