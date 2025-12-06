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
    using System.Runtime.InteropServices;

    /// <include file='doc\NoResizeHandleGlyph.uex' path='docs/doc[@for="NoResizeHandleGlyph"]/*' />
    /// <devdoc> 
    ///     The NoResizeHandleGlyph represents the handle for a non-resizeable control in our new seleciton
    ///     model.  Note that the pen and brush are created once per instance of this class 
    ///     and re-used in our painting logic for perf. reasonse. 
    /// </devdoc>
    internal class NoResizeHandleGlyph : SelectionGlyphBase { 

        private bool isPrimary = false;

        /// <include file='doc\NoResizeHandleGlyph.uex' path='docs/doc[@for="NoResizeHandleGlyph.NoResizeHandleGlyph"]/*' /> 
        /// <devdoc>
        ///     NoResizeHandleGlyph's constructor takes additional parameters: 'type' and 'primary selection'. 
        ///     Also, we create/cache our pen & brush here to avoid this action with every paint message. 
        /// </devdoc>
        internal NoResizeHandleGlyph(Rectangle controlBounds, SelectionRules selRules, bool primarySelection, Behavior behavior) : base(behavior) { 

            isPrimary = primarySelection;
            hitTestCursor = Cursors.Default;
            rules = SelectionRules.None; 
            if ((selRules & SelectionRules.Moveable) != 0) {
                rules = SelectionRules.Moveable; 
                hitTestCursor = Cursors.SizeAll; 
            }
 
            // The handle is always upperleft
            bounds = new Rectangle(controlBounds.X - DesignerUtils.NORESIZEHANDLESIZE, controlBounds.Y - DesignerUtils.NORESIZEHANDLESIZE, DesignerUtils.NORESIZEHANDLESIZE, DesignerUtils.NORESIZEHANDLESIZE);
            hitBounds = bounds;
 
        }
 
        /// <include file='doc\NoResizeHandleGlyph.uex' path='docs/doc[@for="NoResizeHandleGlyph.Paint"]/*' /> 
        /// <devdoc>
        ///     Very simple paint logic. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) {
            DesignerUtils.DrawNoResizeHandle(pe.Graphics, bounds, isPrimary, this);
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
    using System.Runtime.InteropServices;

    /// <include file='doc\NoResizeHandleGlyph.uex' path='docs/doc[@for="NoResizeHandleGlyph"]/*' />
    /// <devdoc> 
    ///     The NoResizeHandleGlyph represents the handle for a non-resizeable control in our new seleciton
    ///     model.  Note that the pen and brush are created once per instance of this class 
    ///     and re-used in our painting logic for perf. reasonse. 
    /// </devdoc>
    internal class NoResizeHandleGlyph : SelectionGlyphBase { 

        private bool isPrimary = false;

        /// <include file='doc\NoResizeHandleGlyph.uex' path='docs/doc[@for="NoResizeHandleGlyph.NoResizeHandleGlyph"]/*' /> 
        /// <devdoc>
        ///     NoResizeHandleGlyph's constructor takes additional parameters: 'type' and 'primary selection'. 
        ///     Also, we create/cache our pen & brush here to avoid this action with every paint message. 
        /// </devdoc>
        internal NoResizeHandleGlyph(Rectangle controlBounds, SelectionRules selRules, bool primarySelection, Behavior behavior) : base(behavior) { 

            isPrimary = primarySelection;
            hitTestCursor = Cursors.Default;
            rules = SelectionRules.None; 
            if ((selRules & SelectionRules.Moveable) != 0) {
                rules = SelectionRules.Moveable; 
                hitTestCursor = Cursors.SizeAll; 
            }
 
            // The handle is always upperleft
            bounds = new Rectangle(controlBounds.X - DesignerUtils.NORESIZEHANDLESIZE, controlBounds.Y - DesignerUtils.NORESIZEHANDLESIZE, DesignerUtils.NORESIZEHANDLESIZE, DesignerUtils.NORESIZEHANDLESIZE);
            hitBounds = bounds;
 
        }
 
        /// <include file='doc\NoResizeHandleGlyph.uex' path='docs/doc[@for="NoResizeHandleGlyph.Paint"]/*' /> 
        /// <devdoc>
        ///     Very simple paint logic. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) {
            DesignerUtils.DrawNoResizeHandle(pe.Graphics, bounds, isPrimary, this);
        } 

    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
