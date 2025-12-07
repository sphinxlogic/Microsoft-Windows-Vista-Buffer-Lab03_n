 
//------------------------------------------------------------------------------
// <copyright file="TabPageDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.Windows.Forms.Design { 


    using System.Diagnostics;
    using System.Collections; 
    using System;
    using System.Drawing; 
    using System.Windows.Forms; 
    using Microsoft.Win32;
    using System.ComponentModel.Design; 
    using System.Windows.Forms.Design.Behavior;
    using System.ComponentModel;

    /// <include file='doc\TabPageDesigner.uex' path='docs/doc[@for="TabPageDesigner"]/*' /> 
    /// <devdoc>
    ///      This is the designer for tap page controls.  It inherits 
    ///      from the base control designer and adds live hit testing 
    ///      capabilites for the tree view control.
    /// </devdoc> 
    internal class TabPageDesigner : PanelDesigner {

         /// <include file='doc\TabPageDesigner.uex' path='docs/doc[@for="TabPageDesigner.CanBeParentedTo"]/*' />
         /// <devdoc> 
        ///     Determines if the this designer can be parented to the specified desinger --
        ///     generally this means if the control for this designer can be parented into the 
        ///     given ParentControlDesigner's designer. 
        /// </devdoc>
        public override bool CanBeParentedTo(IDesigner parentDesigner) { 
            return (parentDesigner != null && parentDesigner.Component is TabControl);
        }

        /// <include file='doc\TabPageDesigner.uex' path='docs/doc[@for="TabPageDesigner.SelectionRules"]/*' /> 
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component. 
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public override SelectionRules SelectionRules {
            get {
                SelectionRules rules = base.SelectionRules;
                Control ctl = Control; 

                if (ctl.Parent is TabControl) { 
                    rules &= ~SelectionRules.AllSizeable; 
                }
 
                return rules;
            }
        }
 
        internal void OnDragDropInternal(DragEventArgs de) {
            OnDragDrop(de); 
        } 

        internal void OnDragEnterInternal(DragEventArgs de) { 
            OnDragEnter(de);
        }

        internal void OnDragLeaveInternal(EventArgs e) { 
            OnDragLeave(e);
        } 
 
        internal void OnDragOverInternal(DragEventArgs e) {
            OnDragOver(e); 
        }

        internal void OnGiveFeedbackInternal(GiveFeedbackEventArgs e) {
            OnGiveFeedback(e); 
        }
 
        protected override ControlBodyGlyph GetControlGlyph(GlyphSelectionType selectionType)  { 
            // create a new body glyph with empty bounds.
            // this will keep incorrect tab pages from stealing drag/drop messages 
            // which are now handled by the TabControlDesigner

            //get the right cursor for this component
            OnSetCursor(); 

            Rectangle translatedBounds = Rectangle.Empty; 
 
            //create our glyph, and set its cursor appropriately
            ControlBodyGlyph g = new ControlBodyGlyph(translatedBounds, Cursor.Current, Control, this); 

            return g;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="TabPageDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.Windows.Forms.Design { 


    using System.Diagnostics;
    using System.Collections; 
    using System;
    using System.Drawing; 
    using System.Windows.Forms; 
    using Microsoft.Win32;
    using System.ComponentModel.Design; 
    using System.Windows.Forms.Design.Behavior;
    using System.ComponentModel;

    /// <include file='doc\TabPageDesigner.uex' path='docs/doc[@for="TabPageDesigner"]/*' /> 
    /// <devdoc>
    ///      This is the designer for tap page controls.  It inherits 
    ///      from the base control designer and adds live hit testing 
    ///      capabilites for the tree view control.
    /// </devdoc> 
    internal class TabPageDesigner : PanelDesigner {

         /// <include file='doc\TabPageDesigner.uex' path='docs/doc[@for="TabPageDesigner.CanBeParentedTo"]/*' />
         /// <devdoc> 
        ///     Determines if the this designer can be parented to the specified desinger --
        ///     generally this means if the control for this designer can be parented into the 
        ///     given ParentControlDesigner's designer. 
        /// </devdoc>
        public override bool CanBeParentedTo(IDesigner parentDesigner) { 
            return (parentDesigner != null && parentDesigner.Component is TabControl);
        }

        /// <include file='doc\TabPageDesigner.uex' path='docs/doc[@for="TabPageDesigner.SelectionRules"]/*' /> 
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component. 
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public override SelectionRules SelectionRules {
            get {
                SelectionRules rules = base.SelectionRules;
                Control ctl = Control; 

                if (ctl.Parent is TabControl) { 
                    rules &= ~SelectionRules.AllSizeable; 
                }
 
                return rules;
            }
        }
 
        internal void OnDragDropInternal(DragEventArgs de) {
            OnDragDrop(de); 
        } 

        internal void OnDragEnterInternal(DragEventArgs de) { 
            OnDragEnter(de);
        }

        internal void OnDragLeaveInternal(EventArgs e) { 
            OnDragLeave(e);
        } 
 
        internal void OnDragOverInternal(DragEventArgs e) {
            OnDragOver(e); 
        }

        internal void OnGiveFeedbackInternal(GiveFeedbackEventArgs e) {
            OnGiveFeedback(e); 
        }
 
        protected override ControlBodyGlyph GetControlGlyph(GlyphSelectionType selectionType)  { 
            // create a new body glyph with empty bounds.
            // this will keep incorrect tab pages from stealing drag/drop messages 
            // which are now handled by the TabControlDesigner

            //get the right cursor for this component
            OnSetCursor(); 

            Rectangle translatedBounds = Rectangle.Empty; 
 
            //create our glyph, and set its cursor appropriately
            ControlBodyGlyph g = new ControlBodyGlyph(translatedBounds, Cursor.Current, Control, this); 

            return g;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
