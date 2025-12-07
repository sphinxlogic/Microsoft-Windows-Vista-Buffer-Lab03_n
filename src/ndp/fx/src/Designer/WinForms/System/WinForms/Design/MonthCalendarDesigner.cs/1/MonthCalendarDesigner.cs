 
//------------------------------------------------------------------------------
// <copyright file="MonthCalendarDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
 
 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.MonthCalendarDesigner..ctor()")]

namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Diagnostics;
 
    /// <include file='doc\MonthCalendarDesigner.uex' path='docs/doc[@for="MonthCalendarDesigner"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides a designer that can design components 
    ///       that extend MonthCalendar.</para>
    /// </devdoc> 
 

    internal class MonthCalendarDesigner : ControlDesigner { 

        public MonthCalendarDesigner() {
            AutoResizeHandles = true;
        } 

        /// <include file='doc\MonthCalendarDesigner.uex' path='docs/doc[@for="MonthCalendarDesigner.SelectionRules"]/*' /> 
        /// <devdoc> 
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc>
        public override SelectionRules SelectionRules {
            get { 
                SelectionRules rules = base.SelectionRules;
                if ((Control.Parent == null) || (Control.Parent != null && !Control.Parent.IsMirrored)) { 
                    rules &= ~(SelectionRules.TopSizeable | SelectionRules.LeftSizeable); 
                }
                else { 
                    Debug.Assert(Control.Parent != null && Control.Parent.IsMirrored);
                    rules &= ~(SelectionRules.TopSizeable | SelectionRules.RightSizeable);
                }
                return rules; 
            }
        } 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="MonthCalendarDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
 
 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.MonthCalendarDesigner..ctor()")]

namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Diagnostics;
 
    /// <include file='doc\MonthCalendarDesigner.uex' path='docs/doc[@for="MonthCalendarDesigner"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides a designer that can design components 
    ///       that extend MonthCalendar.</para>
    /// </devdoc> 
 

    internal class MonthCalendarDesigner : ControlDesigner { 

        public MonthCalendarDesigner() {
            AutoResizeHandles = true;
        } 

        /// <include file='doc\MonthCalendarDesigner.uex' path='docs/doc[@for="MonthCalendarDesigner.SelectionRules"]/*' /> 
        /// <devdoc> 
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc>
        public override SelectionRules SelectionRules {
            get { 
                SelectionRules rules = base.SelectionRules;
                if ((Control.Parent == null) || (Control.Parent != null && !Control.Parent.IsMirrored)) { 
                    rules &= ~(SelectionRules.TopSizeable | SelectionRules.LeftSizeable); 
                }
                else { 
                    Debug.Assert(Control.Parent != null && Control.Parent.IsMirrored);
                    rules &= ~(SelectionRules.TopSizeable | SelectionRules.RightSizeable);
                }
                return rules; 
            }
        } 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
