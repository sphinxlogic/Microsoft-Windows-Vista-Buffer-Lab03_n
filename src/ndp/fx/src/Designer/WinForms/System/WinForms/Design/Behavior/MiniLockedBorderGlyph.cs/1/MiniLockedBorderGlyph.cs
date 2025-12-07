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

    /// <include file='doc\MiniLockedBorderGlyph.uex' path='docs/doc[@for="MiniLockedBorderGlyph"]/*' />
    /// <devdoc>
    ///     The LockedBorderGlyph draws one side (depending on type) of a SelectionBorder 
    ///     in the 'Locked' mode.  The constructor will initialize and cache the pen
    ///     and brush objects to avoid uneccessary recreations. 
    /// </devdoc> 
    internal class MiniLockedBorderGlyph : SelectionGlyphBase {
 
        private SelectionBorderGlyphType type;

        /// <include file='doc\LockedBorderGlyph.uex' path='docs/doc[@for="LockedBorderGlyh.LockedBorderGlyph"]/*' />
        /// <devdoc> 
        ///     This constructor extends from the standard SelectionGlyphBase constructor.  Note that
        ///     a primarySelection flag is passed in - this will be used when determining the colors 
        ///     of the borders. 
        /// </devdoc>
        internal MiniLockedBorderGlyph(Rectangle controlBounds, SelectionBorderGlyphType type, Behavior behavior, bool primarySelection) : base(behavior) { 
            InitializeGlyph(controlBounds, type, primarySelection);
        }

        /// <devdoc> 
        ///     Helper function that initializes the Glyph based on bounds, type, primary sel, and bordersize.
        /// </devdoc> 
        private void InitializeGlyph(Rectangle controlBounds, SelectionBorderGlyphType type, bool primarySelection) { 

            hitTestCursor = Cursors.Default;//always default cursor for locked 
            rules = SelectionRules.None;//never change sel rules for locked

            int borderSize = 1;
            this.type = type; 

            //this will return the rect representing the bounds of the glyph 
            bounds = DesignerUtils.GetBoundsForSelectionType(controlBounds, type, borderSize); 
            hitBounds = bounds;
        } 

        /// <include file='doc\LockedBorderGlyph.uex' path='docs/doc[@for="LockedBorderGlyh.Paint"]/*' />
        /// <devdoc>
        ///     Simple painting logic for locked Glyphs. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) { 
           //DesignerUtils.DrawSelectionBorder(pe.Graphics, bounds); 
           pe.Graphics.FillRectangle(Brushes.Black, bounds);
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

    /// <include file='doc\MiniLockedBorderGlyph.uex' path='docs/doc[@for="MiniLockedBorderGlyph"]/*' />
    /// <devdoc>
    ///     The LockedBorderGlyph draws one side (depending on type) of a SelectionBorder 
    ///     in the 'Locked' mode.  The constructor will initialize and cache the pen
    ///     and brush objects to avoid uneccessary recreations. 
    /// </devdoc> 
    internal class MiniLockedBorderGlyph : SelectionGlyphBase {
 
        private SelectionBorderGlyphType type;

        /// <include file='doc\LockedBorderGlyph.uex' path='docs/doc[@for="LockedBorderGlyh.LockedBorderGlyph"]/*' />
        /// <devdoc> 
        ///     This constructor extends from the standard SelectionGlyphBase constructor.  Note that
        ///     a primarySelection flag is passed in - this will be used when determining the colors 
        ///     of the borders. 
        /// </devdoc>
        internal MiniLockedBorderGlyph(Rectangle controlBounds, SelectionBorderGlyphType type, Behavior behavior, bool primarySelection) : base(behavior) { 
            InitializeGlyph(controlBounds, type, primarySelection);
        }

        /// <devdoc> 
        ///     Helper function that initializes the Glyph based on bounds, type, primary sel, and bordersize.
        /// </devdoc> 
        private void InitializeGlyph(Rectangle controlBounds, SelectionBorderGlyphType type, bool primarySelection) { 

            hitTestCursor = Cursors.Default;//always default cursor for locked 
            rules = SelectionRules.None;//never change sel rules for locked

            int borderSize = 1;
            this.type = type; 

            //this will return the rect representing the bounds of the glyph 
            bounds = DesignerUtils.GetBoundsForSelectionType(controlBounds, type, borderSize); 
            hitBounds = bounds;
        } 

        /// <include file='doc\LockedBorderGlyph.uex' path='docs/doc[@for="LockedBorderGlyh.Paint"]/*' />
        /// <devdoc>
        ///     Simple painting logic for locked Glyphs. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) { 
           //DesignerUtils.DrawSelectionBorder(pe.Graphics, bounds); 
           pe.Graphics.FillRectangle(Brushes.Black, bounds);
        } 

    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
