namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Drawing2D;
    using System.Windows.Forms.Design; 

    /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase"]/*' />
    /// <devdoc>
    ///     This is the base class for all the selection Glyphs: GrabHandle, 
    ///     Hidden, Locked, Selection, and Tray Glyphs.  This class includes
    ///     all like-operations for the Selection glyphs. 
    /// </devdoc> 
    internal abstract class SelectionGlyphBase : Glyph {
 
        protected Rectangle bounds;//defines the bounds of the selection glyph
        protected Rectangle hitBounds;//defines the bounds used for hittest - it could be different than the bounds of the glyph itself
        protected Cursor hitTestCursor;//the cursor returned if hit test is positive
        protected SelectionRules rules;//the selection rules - defining how the control can change 

        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.SelectionGlyphBase"]/*' /> 
        /// <devdoc> 
        ///     Standard constructor.
        /// </devdoc> 
        internal SelectionGlyphBase(Behavior behavior) : base(behavior) {
        }

        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.SelectionRules"]/*' /> 
        /// <devdoc>
        ///     Read-only property describing the SelecitonRules for these Glyphs. 
        /// </devdoc> 
        public SelectionRules SelectionRules {
            get { 
                return rules;
            }
        }
 
        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Simple hit test rule: if the point is contained within the bounds - 
        ///     then it is a positive hit test.
        /// </devdoc> 
        public override Cursor GetHitTest(Point p) {
            if (hitBounds.Contains(p)) {
                return hitTestCursor;
            } 
            return null;
        } 
 
        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.HitTestCursor"]/*' />
        /// <devdoc> 
        ///     Returns the HitTestCursor for this glyph.
        /// </devdoc>
        public Cursor HitTestCursor {
            get { 
                return hitTestCursor;
            } 
        } 

        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.Bounds"]/*' /> 
        /// <devdoc>
        ///     The Bounds of this glyph.
        /// </devdoc>
 
        public override Rectangle Bounds  {
            get  { 
                return bounds; 
            }
        } 

        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.Paint"]/*' />
        /// <devdoc>
        ///     There's no paint logic on this base class. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) { 
        } 

    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Drawing2D;
    using System.Windows.Forms.Design; 

    /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase"]/*' />
    /// <devdoc>
    ///     This is the base class for all the selection Glyphs: GrabHandle, 
    ///     Hidden, Locked, Selection, and Tray Glyphs.  This class includes
    ///     all like-operations for the Selection glyphs. 
    /// </devdoc> 
    internal abstract class SelectionGlyphBase : Glyph {
 
        protected Rectangle bounds;//defines the bounds of the selection glyph
        protected Rectangle hitBounds;//defines the bounds used for hittest - it could be different than the bounds of the glyph itself
        protected Cursor hitTestCursor;//the cursor returned if hit test is positive
        protected SelectionRules rules;//the selection rules - defining how the control can change 

        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.SelectionGlyphBase"]/*' /> 
        /// <devdoc> 
        ///     Standard constructor.
        /// </devdoc> 
        internal SelectionGlyphBase(Behavior behavior) : base(behavior) {
        }

        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.SelectionRules"]/*' /> 
        /// <devdoc>
        ///     Read-only property describing the SelecitonRules for these Glyphs. 
        /// </devdoc> 
        public SelectionRules SelectionRules {
            get { 
                return rules;
            }
        }
 
        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Simple hit test rule: if the point is contained within the bounds - 
        ///     then it is a positive hit test.
        /// </devdoc> 
        public override Cursor GetHitTest(Point p) {
            if (hitBounds.Contains(p)) {
                return hitTestCursor;
            } 
            return null;
        } 
 
        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.HitTestCursor"]/*' />
        /// <devdoc> 
        ///     Returns the HitTestCursor for this glyph.
        /// </devdoc>
        public Cursor HitTestCursor {
            get { 
                return hitTestCursor;
            } 
        } 

        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.Bounds"]/*' /> 
        /// <devdoc>
        ///     The Bounds of this glyph.
        /// </devdoc>
 
        public override Rectangle Bounds  {
            get  { 
                return bounds; 
            }
        } 

        /// <include file='doc\SelectionGlyphBase.uex' path='docs/doc[@for="SelectionGlyphBase.Paint"]/*' />
        /// <devdoc>
        ///     There's no paint logic on this base class. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) { 
        } 

    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
