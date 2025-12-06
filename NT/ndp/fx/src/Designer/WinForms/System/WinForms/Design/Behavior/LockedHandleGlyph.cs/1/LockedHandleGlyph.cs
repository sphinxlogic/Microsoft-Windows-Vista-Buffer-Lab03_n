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

    /// <include file='doc\LockedHandleGlyph.uex' path='docs/doc[@for="LockedHandleGlyph"]/*' />
    /// <devdoc> 
    ///     The LockedHandleGlyph represents the handle for a non-resizeable control in our new seleciton
    ///     model.  Note that the pen and brush are created once per instance of this class 
    ///     and re-used in our painting logic for perf. reasonse. 
    /// </devdoc>
    internal class LockedHandleGlyph : SelectionGlyphBase { 

        private bool isPrimary = false;

        /// <include file='doc\LockedHandleGlyph.uex' path='docs/doc[@for="LockedHandleGlyph.LockedHandleGlyph"]/*' /> 
        /// <devdoc>
        ///     LockedHandleGlyph's constructor takes additional parameters: 'type' and 'primary selection'. 
        ///     Also, we create/cache our pen & brush here to avoid this action with every paint message. 
        /// </devdoc>
        internal LockedHandleGlyph(Rectangle controlBounds, bool primarySelection) : base(null) { 

            isPrimary = primarySelection;
            hitTestCursor = Cursors.Default;
            rules = SelectionRules.None; 

            bounds = new Rectangle((controlBounds.X + DesignerUtils.LOCKHANDLEOVERLAP) - DesignerUtils.LOCKHANDLEWIDTH, 
                                    (controlBounds.Y + DesignerUtils.LOCKHANDLEOVERLAP) - DesignerUtils.LOCKHANDLEHEIGHT, 
                                    DesignerUtils.LOCKHANDLEWIDTH, DesignerUtils.LOCKHANDLEHEIGHT);
            hitBounds = bounds; 
        }

        /// <include file='doc\LockedHandleGlyph.uex' path='docs/doc[@for="LockedHandleGlyph.Paint"]/*' />
        /// <devdoc> 
        ///     Very simple paint logic.
        /// </devdoc> 
        public override void Paint(PaintEventArgs pe) { 
            DesignerUtils.DrawLockedHandle(pe.Graphics, bounds, isPrimary, this);
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

    /// <include file='doc\LockedHandleGlyph.uex' path='docs/doc[@for="LockedHandleGlyph"]/*' />
    /// <devdoc> 
    ///     The LockedHandleGlyph represents the handle for a non-resizeable control in our new seleciton
    ///     model.  Note that the pen and brush are created once per instance of this class 
    ///     and re-used in our painting logic for perf. reasonse. 
    /// </devdoc>
    internal class LockedHandleGlyph : SelectionGlyphBase { 

        private bool isPrimary = false;

        /// <include file='doc\LockedHandleGlyph.uex' path='docs/doc[@for="LockedHandleGlyph.LockedHandleGlyph"]/*' /> 
        /// <devdoc>
        ///     LockedHandleGlyph's constructor takes additional parameters: 'type' and 'primary selection'. 
        ///     Also, we create/cache our pen & brush here to avoid this action with every paint message. 
        /// </devdoc>
        internal LockedHandleGlyph(Rectangle controlBounds, bool primarySelection) : base(null) { 

            isPrimary = primarySelection;
            hitTestCursor = Cursors.Default;
            rules = SelectionRules.None; 

            bounds = new Rectangle((controlBounds.X + DesignerUtils.LOCKHANDLEOVERLAP) - DesignerUtils.LOCKHANDLEWIDTH, 
                                    (controlBounds.Y + DesignerUtils.LOCKHANDLEOVERLAP) - DesignerUtils.LOCKHANDLEHEIGHT, 
                                    DesignerUtils.LOCKHANDLEWIDTH, DesignerUtils.LOCKHANDLEHEIGHT);
            hitBounds = bounds; 
        }

        /// <include file='doc\LockedHandleGlyph.uex' path='docs/doc[@for="LockedHandleGlyph.Paint"]/*' />
        /// <devdoc> 
        ///     Very simple paint logic.
        /// </devdoc> 
        public override void Paint(PaintEventArgs pe) { 
            DesignerUtils.DrawLockedHandle(pe.Graphics, bounds, isPrimary, this);
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
