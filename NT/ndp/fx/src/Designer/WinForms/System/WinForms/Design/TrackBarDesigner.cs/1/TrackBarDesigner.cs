//------------------------------------------------------------------------------ 
// <copyright file="TrackBarDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.TrackBarDesigner..ctor()")] 
namespace System.Windows.Forms.Design {
    using System;
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
 
    /// <include file='doc\TrackBarDesigner.uex' path='docs/doc[@for="TrackBarDesigner"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides a designer that can design components
    ///       that extend TrackBar.</para>
    /// </devdoc>
    internal class TrackBarDesigner : ControlDesigner { 

        public TrackBarDesigner() { 
            AutoResizeHandles = true; 
        }
 
        /// <include file='doc\TrackBarDesigner.uex' path='docs/doc[@for="TrackBarDesigner.SelectionRules"]/*' />
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public override SelectionRules SelectionRules { 
            get {
                SelectionRules rules = base.SelectionRules; 
                object component = Component;

                //VSWhidbey # 369288
                rules |= SelectionRules.AllSizeable; 

                PropertyDescriptor propAutoSize = TypeDescriptor.GetProperties(component)["AutoSize"]; 
                if (propAutoSize != null) { 
                    bool autoSize = (bool)propAutoSize.GetValue(component);
 
                    PropertyDescriptor propOrientation = TypeDescriptor.GetProperties(component)["Orientation"];
                    Orientation or = Orientation.Horizontal;
                    if (propOrientation != null) {
                        or = (Orientation)propOrientation.GetValue(component); 
                    }
 
                    if (autoSize) { 
                        if (or == Orientation.Horizontal) {
                            rules &= ~(SelectionRules.TopSizeable | SelectionRules.BottomSizeable); 
                        }
                        else if (or == Orientation.Vertical) {
                            rules &= ~(SelectionRules.LeftSizeable | SelectionRules.RightSizeable);
                        } 
                    }
                } 
 
                return rules;
            } 
        }
    }
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TrackBarDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.TrackBarDesigner..ctor()")] 
namespace System.Windows.Forms.Design {
    using System;
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
 
    /// <include file='doc\TrackBarDesigner.uex' path='docs/doc[@for="TrackBarDesigner"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides a designer that can design components
    ///       that extend TrackBar.</para>
    /// </devdoc>
    internal class TrackBarDesigner : ControlDesigner { 

        public TrackBarDesigner() { 
            AutoResizeHandles = true; 
        }
 
        /// <include file='doc\TrackBarDesigner.uex' path='docs/doc[@for="TrackBarDesigner.SelectionRules"]/*' />
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public override SelectionRules SelectionRules { 
            get {
                SelectionRules rules = base.SelectionRules; 
                object component = Component;

                //VSWhidbey # 369288
                rules |= SelectionRules.AllSizeable; 

                PropertyDescriptor propAutoSize = TypeDescriptor.GetProperties(component)["AutoSize"]; 
                if (propAutoSize != null) { 
                    bool autoSize = (bool)propAutoSize.GetValue(component);
 
                    PropertyDescriptor propOrientation = TypeDescriptor.GetProperties(component)["Orientation"];
                    Orientation or = Orientation.Horizontal;
                    if (propOrientation != null) {
                        or = (Orientation)propOrientation.GetValue(component); 
                    }
 
                    if (autoSize) { 
                        if (or == Orientation.Horizontal) {
                            rules &= ~(SelectionRules.TopSizeable | SelectionRules.BottomSizeable); 
                        }
                        else if (or == Orientation.Vertical) {
                            rules &= ~(SelectionRules.LeftSizeable | SelectionRules.RightSizeable);
                        } 
                    }
                } 
 
                return rules;
            } 
        }
    }
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
