namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
 
    /// <include file='doc\NoResizeSelectionBorderGlyph.uex' path='docs/doc[@for="NoResizeSelectionBorderGlyph"]/*' />
    /// <devdoc>
    ///     The NoResizeSelectionBorderGlyph draws one side (depending on type) of a SelectionBorder.
    /// </devdoc> 
    internal class NoResizeSelectionBorderGlyph : SelectionGlyphBase {
 
        /// <include file='doc\NoResizeSelectionBorderGlyph.uex' path='docs/doc[@for="NoResizeSelectionBorderGlyph.NoResizeSelectionBorderGlyph"]/*' /> 
        /// <devdoc>
        ///     This constructor extends from the standard SelectionGlyphBase constructor. 
        /// </devdoc>
        internal NoResizeSelectionBorderGlyph(Rectangle controlBounds, SelectionRules rules, SelectionBorderGlyphType type, Behavior behavior) : base(behavior) {
            InitializeGlyph(controlBounds, rules, type);
        } 

        /// <devdoc> 
        ///     Helper function that initializes the Glyph based on bounds, type, and bordersize. 
        /// </devdoc>
        private void InitializeGlyph(Rectangle controlBounds, SelectionRules selRules, SelectionBorderGlyphType type) { 

            rules = SelectionRules.None;
            hitTestCursor = Cursors.Default;
            if ((selRules & SelectionRules.Moveable) != 0) { 
                rules = SelectionRules.Moveable;
                hitTestCursor = Cursors.SizeAll; 
            } 

            //this will return the rect representing the bounds of the glyph 
            bounds = DesignerUtils.GetBoundsForNoResizeSelectionType(controlBounds, type);
            hitBounds = bounds;

            // The hitbounds for the border is actually a bit bigger than the glyph bounds 

            switch (type) { 
                case SelectionBorderGlyphType.Top: 
                    goto case SelectionBorderGlyphType.Bottom;
                case SelectionBorderGlyphType.Bottom: 
                    // We want to apply the SELECTIONBORDERHITAREA to the top and the bottom of the selection border glyph
                    hitBounds.Y -= (DesignerUtils.SELECTIONBORDERHITAREA - DesignerUtils.SELECTIONBORDERSIZE) / 2;
                    hitBounds.Height += DesignerUtils.SELECTIONBORDERHITAREA - DesignerUtils.SELECTIONBORDERSIZE;
                break; 
                case SelectionBorderGlyphType.Left:
                    goto case SelectionBorderGlyphType.Right; 
                case SelectionBorderGlyphType.Right: 
                    // We want to apply the SELECTIONBORDERHITAREA to the left and the right of the selection border glyph
                    hitBounds.X -= (DesignerUtils.SELECTIONBORDERHITAREA - DesignerUtils.SELECTIONBORDERSIZE) / 2; 
                    hitBounds.Width += DesignerUtils.SELECTIONBORDERHITAREA - DesignerUtils.SELECTIONBORDERSIZE;
                break;
            }
 

 
        } 

        /// <include file='doc\NoResizeSelectionBorderGlyph.uex' path='docs/doc[@for="NoResizeSelectionBorderGlyph.Paint"]/*' /> 
        /// <devdoc>
        ///     Simple painting logic for selection Glyphs.
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
    using System.Windows.Forms.Design;
 
    /// <include file='doc\NoResizeSelectionBorderGlyph.uex' path='docs/doc[@for="NoResizeSelectionBorderGlyph"]/*' />
    /// <devdoc>
    ///     The NoResizeSelectionBorderGlyph draws one side (depending on type) of a SelectionBorder.
    /// </devdoc> 
    internal class NoResizeSelectionBorderGlyph : SelectionGlyphBase {
 
        /// <include file='doc\NoResizeSelectionBorderGlyph.uex' path='docs/doc[@for="NoResizeSelectionBorderGlyph.NoResizeSelectionBorderGlyph"]/*' /> 
        /// <devdoc>
        ///     This constructor extends from the standard SelectionGlyphBase constructor. 
        /// </devdoc>
        internal NoResizeSelectionBorderGlyph(Rectangle controlBounds, SelectionRules rules, SelectionBorderGlyphType type, Behavior behavior) : base(behavior) {
            InitializeGlyph(controlBounds, rules, type);
        } 

        /// <devdoc> 
        ///     Helper function that initializes the Glyph based on bounds, type, and bordersize. 
        /// </devdoc>
        private void InitializeGlyph(Rectangle controlBounds, SelectionRules selRules, SelectionBorderGlyphType type) { 

            rules = SelectionRules.None;
            hitTestCursor = Cursors.Default;
            if ((selRules & SelectionRules.Moveable) != 0) { 
                rules = SelectionRules.Moveable;
                hitTestCursor = Cursors.SizeAll; 
            } 

            //this will return the rect representing the bounds of the glyph 
            bounds = DesignerUtils.GetBoundsForNoResizeSelectionType(controlBounds, type);
            hitBounds = bounds;

            // The hitbounds for the border is actually a bit bigger than the glyph bounds 

            switch (type) { 
                case SelectionBorderGlyphType.Top: 
                    goto case SelectionBorderGlyphType.Bottom;
                case SelectionBorderGlyphType.Bottom: 
                    // We want to apply the SELECTIONBORDERHITAREA to the top and the bottom of the selection border glyph
                    hitBounds.Y -= (DesignerUtils.SELECTIONBORDERHITAREA - DesignerUtils.SELECTIONBORDERSIZE) / 2;
                    hitBounds.Height += DesignerUtils.SELECTIONBORDERHITAREA - DesignerUtils.SELECTIONBORDERSIZE;
                break; 
                case SelectionBorderGlyphType.Left:
                    goto case SelectionBorderGlyphType.Right; 
                case SelectionBorderGlyphType.Right: 
                    // We want to apply the SELECTIONBORDERHITAREA to the left and the right of the selection border glyph
                    hitBounds.X -= (DesignerUtils.SELECTIONBORDERHITAREA - DesignerUtils.SELECTIONBORDERSIZE) / 2; 
                    hitBounds.Width += DesignerUtils.SELECTIONBORDERHITAREA - DesignerUtils.SELECTIONBORDERSIZE;
                break;
            }
 

 
        } 

        /// <include file='doc\NoResizeSelectionBorderGlyph.uex' path='docs/doc[@for="NoResizeSelectionBorderGlyph.Paint"]/*' /> 
        /// <devdoc>
        ///     Simple painting logic for selection Glyphs.
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) { 
            DesignerUtils.DrawSelectionBorder(pe.Graphics, bounds);
        } 
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
