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

    /// <include file='doc\LockedBorderGlyph.uex' path='docs/doc[@for="LockedBorderGlyh"]/*' />
    /// <devdoc>
    ///     The LockedBorderGlyph draws one side (depending on type) of a SelectionBorder 
    ///     in the 'Locked' mode.  The constructor will initialize and cache the pen
    ///     and brush objects to avoid uneccessary recreations. 
    /// </devdoc> 
    internal class LockedBorderGlyph : SelectionGlyphBase {
 
        /// <include file='doc\LockedBorderGlyph.uex' path='docs/doc[@for="LockedBorderGlyh.LockedBorderGlyph"]/*' />
        /// <devdoc>
        ///     This constructor extends from the standard SelectionGlyphBase constructor.  Note that
        ///     a primarySelection flag is passed in - this will be used when determining the colors 
        ///     of the borders.
        /// </devdoc> 
        internal LockedBorderGlyph(Rectangle controlBounds, SelectionBorderGlyphType type) : base(null) { 
            InitializeGlyph(controlBounds, type);
        } 

        /// <devdoc>
        ///     Helper function that initializes the Glyph based on bounds, type, primary sel, and bordersize.
        /// </devdoc> 
        private void InitializeGlyph(Rectangle controlBounds, SelectionBorderGlyphType type) {
 
            hitTestCursor = Cursors.Default;//always default cursor for locked 
            rules = SelectionRules.None;//never change sel rules for locked
 
            //this will return the rect representing the bounds of the glyph
            bounds = DesignerUtils.GetBoundsForSelectionType(controlBounds, type);
            hitBounds = bounds;
        } 

        /// <include file='doc\LockedBorderGlyph.uex' path='docs/doc[@for="LockedBorderGlyh.Paint"]/*' /> 
        /// <devdoc> 
        ///     Simple painting logic for locked Glyphs.
        /// </devdoc> 
        public override void Paint(PaintEventArgs pe) {
            DesignerUtils.DrawSelectionBorder(pe.Graphics, bounds);
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

    /// <include file='doc\LockedBorderGlyph.uex' path='docs/doc[@for="LockedBorderGlyh"]/*' />
    /// <devdoc>
    ///     The LockedBorderGlyph draws one side (depending on type) of a SelectionBorder 
    ///     in the 'Locked' mode.  The constructor will initialize and cache the pen
    ///     and brush objects to avoid uneccessary recreations. 
    /// </devdoc> 
    internal class LockedBorderGlyph : SelectionGlyphBase {
 
        /// <include file='doc\LockedBorderGlyph.uex' path='docs/doc[@for="LockedBorderGlyh.LockedBorderGlyph"]/*' />
        /// <devdoc>
        ///     This constructor extends from the standard SelectionGlyphBase constructor.  Note that
        ///     a primarySelection flag is passed in - this will be used when determining the colors 
        ///     of the borders.
        /// </devdoc> 
        internal LockedBorderGlyph(Rectangle controlBounds, SelectionBorderGlyphType type) : base(null) { 
            InitializeGlyph(controlBounds, type);
        } 

        /// <devdoc>
        ///     Helper function that initializes the Glyph based on bounds, type, primary sel, and bordersize.
        /// </devdoc> 
        private void InitializeGlyph(Rectangle controlBounds, SelectionBorderGlyphType type) {
 
            hitTestCursor = Cursors.Default;//always default cursor for locked 
            rules = SelectionRules.None;//never change sel rules for locked
 
            //this will return the rect representing the bounds of the glyph
            bounds = DesignerUtils.GetBoundsForSelectionType(controlBounds, type);
            hitBounds = bounds;
        } 

        /// <include file='doc\LockedBorderGlyph.uex' path='docs/doc[@for="LockedBorderGlyh.Paint"]/*' /> 
        /// <devdoc> 
        ///     Simple painting logic for locked Glyphs.
        /// </devdoc> 
        public override void Paint(PaintEventArgs pe) {
            DesignerUtils.DrawSelectionBorder(pe.Graphics, bounds);
        }
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
