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

    /// <include file='doc\GrabHandleGlyph.uex' path='docs/doc[@for="GrabHandleGlyph"]/*' />
    /// <devdoc> 
    ///     The GrabHandleGlyph represents the 8 handles of our new seleciton
    ///     model.  Note that the pen and brush are created once per instance of this class 
    ///     and re-used in our painting logic for perf. reasonse. 
    /// </devdoc>
    internal class GrabHandleGlyph : SelectionGlyphBase { 

        private bool isPrimary = false;

        /// <include file='doc\GrabHandleGlyph.uex' path='docs/doc[@for="GrabHandleGlyph.GrabHandleGlyph"]/*' /> 
        /// <devdoc>
        ///     GrabHandleGlyph's constructor takes additional parameters: 'type' and 'primary selection'. 
        ///     Also, we create/cache our pen & brush here to avoid this action with every paint message. 
        /// </devdoc>
        internal GrabHandleGlyph(Rectangle controlBounds, GrabHandleGlyphType type, Behavior behavior, bool primarySelection) : base(behavior) { 

            isPrimary = primarySelection;
            hitTestCursor = Cursors.Default;
            rules = SelectionRules.None; 

            // We +/- DesignerUtils.HANDLEOVERLAP because we want each GrabHandle to overlap the control by DesignerUtils.HANDLEOVERLAP pixels 
            switch (type) { 
                case GrabHandleGlyphType.UpperLeft:
                    bounds = new Rectangle((controlBounds.X+DesignerUtils.HANDLEOVERLAP) - DesignerUtils.HANDLESIZE, (controlBounds.Y+DesignerUtils.HANDLEOVERLAP) - DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE); 
                    hitTestCursor = Cursors.SizeNWSE;
                    rules = SelectionRules.TopSizeable | SelectionRules.LeftSizeable;
                    break;
                case GrabHandleGlyphType.UpperRight: 
                    bounds = new Rectangle(controlBounds.Right - DesignerUtils.HANDLEOVERLAP, (controlBounds.Y+DesignerUtils.HANDLEOVERLAP) - DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE);
                    hitTestCursor = Cursors.SizeNESW; 
                    rules = SelectionRules.TopSizeable | SelectionRules.RightSizeable; 
                    break;
                case GrabHandleGlyphType.LowerRight: 
                    bounds = new Rectangle(controlBounds.Right - DesignerUtils.HANDLEOVERLAP, controlBounds.Bottom-DesignerUtils.HANDLEOVERLAP, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE);
                    hitTestCursor = Cursors.SizeNWSE;
                    rules = SelectionRules.BottomSizeable | SelectionRules.RightSizeable;
                    break; 
                case GrabHandleGlyphType.LowerLeft:
                    bounds = new Rectangle((controlBounds.X+DesignerUtils.HANDLEOVERLAP) -DesignerUtils.HANDLESIZE, controlBounds.Bottom-DesignerUtils.HANDLEOVERLAP, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE); 
                    hitTestCursor = Cursors.SizeNESW; 
                    rules = SelectionRules.BottomSizeable | SelectionRules.LeftSizeable;
                    break; 
                case GrabHandleGlyphType.MiddleTop:
                    // Only add this one if there's room enough. Room is enough is as follows:
                    // 2*HANDLEOVERLAP for UpperLeft and UpperRight handles, 1 HANDLESIZE for the MiddleTop handle, 1 HANDLESIZE
                    // for padding 
                    if (controlBounds.Width >= (2 * DesignerUtils.HANDLEOVERLAP) + (2 * DesignerUtils.HANDLESIZE)) {
                        bounds = new Rectangle(controlBounds.X + (controlBounds.Width/2) - (DesignerUtils.HANDLESIZE/2), (controlBounds.Y+DesignerUtils.HANDLEOVERLAP) - DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE); 
                        hitTestCursor = Cursors.SizeNS; 
                        rules = SelectionRules.TopSizeable;
                    } 
                    break;
                case GrabHandleGlyphType.MiddleBottom:
                    // Only add this one if there's room enough. Room is enough is as follows:
                    // 2*HANDLEOVERLAP for LowerLeft and LowerRight handles, 1 HANDLESIZE for the MiddleBottom handle, 1 HANDLESIZE 
                    // for padding
                    if (controlBounds.Width >= (2 * DesignerUtils.HANDLEOVERLAP) + (2 * DesignerUtils.HANDLESIZE)) { 
                        bounds = new Rectangle(controlBounds.X + (controlBounds.Width/2) - (DesignerUtils.HANDLESIZE/2), controlBounds.Bottom-DesignerUtils.HANDLEOVERLAP, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE); 
                        hitTestCursor = Cursors.SizeNS;
                        rules = SelectionRules.BottomSizeable; 
                    }
                    break;
                case GrabHandleGlyphType.MiddleLeft:
                    // Only add this one if there's room enough. Room is enough is as follows: 
                    // 2*HANDLEOVERLAP for UpperLeft and LowerLeft handles, 1 HANDLESIZE for the MiddleLeft handle, 1 HANDLESIZE
                    // for padding 
                    if (controlBounds.Height >= (2 * DesignerUtils.HANDLEOVERLAP) + (2 * DesignerUtils.HANDLESIZE)) { 
                        bounds = new Rectangle((controlBounds.X+DesignerUtils.HANDLEOVERLAP) - DesignerUtils.HANDLESIZE, controlBounds.Y + (controlBounds.Height/2) - (DesignerUtils.HANDLESIZE/2), DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE);
                        hitTestCursor = Cursors.SizeWE; 
                        rules = SelectionRules.LeftSizeable;
                    }
                    break;
                case GrabHandleGlyphType.MiddleRight: 
                    // Only add this one if there's room enough. Room is enough is as follows:
                    // 2*HANDLEOVERLAP for UpperRight and LowerRight handles, 1 HANDLESIZE for the MiddleRight handle, 1 HANDLESIZE 
                    // for padding 
                    if (controlBounds.Height >= (2 * DesignerUtils.HANDLEOVERLAP) + (2 * DesignerUtils.HANDLESIZE)) {
                        bounds = new Rectangle(controlBounds.Right - DesignerUtils.HANDLEOVERLAP, controlBounds.Y + (controlBounds.Height/2) - (DesignerUtils.HANDLESIZE/2), DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE); 
                        hitTestCursor = Cursors.SizeWE;
                        rules = SelectionRules.RightSizeable;
                    }
                    break; 
                default:
                    Debug.Assert(false, "GrabHandleGlyph was called with a bad GrapHandleGlyphType."); 
                    break; 
            }
 
            hitBounds = bounds;
        }

        /// <include file='doc\GrabHandleGlyph.uex' path='docs/doc[@for="GrabHandleGlyph.Paint"]/*' /> 
        /// <devdoc>
        ///     Very simple paint logic. 
        /// </devdoc> 
        public override void Paint(PaintEventArgs pe) {
            DesignerUtils.DrawGrabHandle(pe.Graphics, bounds, isPrimary, this); 
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

    /// <include file='doc\GrabHandleGlyph.uex' path='docs/doc[@for="GrabHandleGlyph"]/*' />
    /// <devdoc> 
    ///     The GrabHandleGlyph represents the 8 handles of our new seleciton
    ///     model.  Note that the pen and brush are created once per instance of this class 
    ///     and re-used in our painting logic for perf. reasonse. 
    /// </devdoc>
    internal class GrabHandleGlyph : SelectionGlyphBase { 

        private bool isPrimary = false;

        /// <include file='doc\GrabHandleGlyph.uex' path='docs/doc[@for="GrabHandleGlyph.GrabHandleGlyph"]/*' /> 
        /// <devdoc>
        ///     GrabHandleGlyph's constructor takes additional parameters: 'type' and 'primary selection'. 
        ///     Also, we create/cache our pen & brush here to avoid this action with every paint message. 
        /// </devdoc>
        internal GrabHandleGlyph(Rectangle controlBounds, GrabHandleGlyphType type, Behavior behavior, bool primarySelection) : base(behavior) { 

            isPrimary = primarySelection;
            hitTestCursor = Cursors.Default;
            rules = SelectionRules.None; 

            // We +/- DesignerUtils.HANDLEOVERLAP because we want each GrabHandle to overlap the control by DesignerUtils.HANDLEOVERLAP pixels 
            switch (type) { 
                case GrabHandleGlyphType.UpperLeft:
                    bounds = new Rectangle((controlBounds.X+DesignerUtils.HANDLEOVERLAP) - DesignerUtils.HANDLESIZE, (controlBounds.Y+DesignerUtils.HANDLEOVERLAP) - DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE); 
                    hitTestCursor = Cursors.SizeNWSE;
                    rules = SelectionRules.TopSizeable | SelectionRules.LeftSizeable;
                    break;
                case GrabHandleGlyphType.UpperRight: 
                    bounds = new Rectangle(controlBounds.Right - DesignerUtils.HANDLEOVERLAP, (controlBounds.Y+DesignerUtils.HANDLEOVERLAP) - DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE);
                    hitTestCursor = Cursors.SizeNESW; 
                    rules = SelectionRules.TopSizeable | SelectionRules.RightSizeable; 
                    break;
                case GrabHandleGlyphType.LowerRight: 
                    bounds = new Rectangle(controlBounds.Right - DesignerUtils.HANDLEOVERLAP, controlBounds.Bottom-DesignerUtils.HANDLEOVERLAP, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE);
                    hitTestCursor = Cursors.SizeNWSE;
                    rules = SelectionRules.BottomSizeable | SelectionRules.RightSizeable;
                    break; 
                case GrabHandleGlyphType.LowerLeft:
                    bounds = new Rectangle((controlBounds.X+DesignerUtils.HANDLEOVERLAP) -DesignerUtils.HANDLESIZE, controlBounds.Bottom-DesignerUtils.HANDLEOVERLAP, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE); 
                    hitTestCursor = Cursors.SizeNESW; 
                    rules = SelectionRules.BottomSizeable | SelectionRules.LeftSizeable;
                    break; 
                case GrabHandleGlyphType.MiddleTop:
                    // Only add this one if there's room enough. Room is enough is as follows:
                    // 2*HANDLEOVERLAP for UpperLeft and UpperRight handles, 1 HANDLESIZE for the MiddleTop handle, 1 HANDLESIZE
                    // for padding 
                    if (controlBounds.Width >= (2 * DesignerUtils.HANDLEOVERLAP) + (2 * DesignerUtils.HANDLESIZE)) {
                        bounds = new Rectangle(controlBounds.X + (controlBounds.Width/2) - (DesignerUtils.HANDLESIZE/2), (controlBounds.Y+DesignerUtils.HANDLEOVERLAP) - DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE); 
                        hitTestCursor = Cursors.SizeNS; 
                        rules = SelectionRules.TopSizeable;
                    } 
                    break;
                case GrabHandleGlyphType.MiddleBottom:
                    // Only add this one if there's room enough. Room is enough is as follows:
                    // 2*HANDLEOVERLAP for LowerLeft and LowerRight handles, 1 HANDLESIZE for the MiddleBottom handle, 1 HANDLESIZE 
                    // for padding
                    if (controlBounds.Width >= (2 * DesignerUtils.HANDLEOVERLAP) + (2 * DesignerUtils.HANDLESIZE)) { 
                        bounds = new Rectangle(controlBounds.X + (controlBounds.Width/2) - (DesignerUtils.HANDLESIZE/2), controlBounds.Bottom-DesignerUtils.HANDLEOVERLAP, DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE); 
                        hitTestCursor = Cursors.SizeNS;
                        rules = SelectionRules.BottomSizeable; 
                    }
                    break;
                case GrabHandleGlyphType.MiddleLeft:
                    // Only add this one if there's room enough. Room is enough is as follows: 
                    // 2*HANDLEOVERLAP for UpperLeft and LowerLeft handles, 1 HANDLESIZE for the MiddleLeft handle, 1 HANDLESIZE
                    // for padding 
                    if (controlBounds.Height >= (2 * DesignerUtils.HANDLEOVERLAP) + (2 * DesignerUtils.HANDLESIZE)) { 
                        bounds = new Rectangle((controlBounds.X+DesignerUtils.HANDLEOVERLAP) - DesignerUtils.HANDLESIZE, controlBounds.Y + (controlBounds.Height/2) - (DesignerUtils.HANDLESIZE/2), DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE);
                        hitTestCursor = Cursors.SizeWE; 
                        rules = SelectionRules.LeftSizeable;
                    }
                    break;
                case GrabHandleGlyphType.MiddleRight: 
                    // Only add this one if there's room enough. Room is enough is as follows:
                    // 2*HANDLEOVERLAP for UpperRight and LowerRight handles, 1 HANDLESIZE for the MiddleRight handle, 1 HANDLESIZE 
                    // for padding 
                    if (controlBounds.Height >= (2 * DesignerUtils.HANDLEOVERLAP) + (2 * DesignerUtils.HANDLESIZE)) {
                        bounds = new Rectangle(controlBounds.Right - DesignerUtils.HANDLEOVERLAP, controlBounds.Y + (controlBounds.Height/2) - (DesignerUtils.HANDLESIZE/2), DesignerUtils.HANDLESIZE, DesignerUtils.HANDLESIZE); 
                        hitTestCursor = Cursors.SizeWE;
                        rules = SelectionRules.RightSizeable;
                    }
                    break; 
                default:
                    Debug.Assert(false, "GrabHandleGlyph was called with a bad GrapHandleGlyphType."); 
                    break; 
            }
 
            hitBounds = bounds;
        }

        /// <include file='doc\GrabHandleGlyph.uex' path='docs/doc[@for="GrabHandleGlyph.Paint"]/*' /> 
        /// <devdoc>
        ///     Very simple paint logic. 
        /// </devdoc> 
        public override void Paint(PaintEventArgs pe) {
            DesignerUtils.DrawGrabHandle(pe.Graphics, bounds, isPrimary, this); 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
