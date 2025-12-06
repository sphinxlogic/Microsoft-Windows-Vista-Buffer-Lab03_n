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
    using System.Windows.Forms;

    /// <include file='doc\ContainerSelectorGlyph.uex' path='docs/doc[@for="ContainerSelectorGlyph"]/*' />
    /// <devdoc> 
    ///     This is the glyph used to drag container controls around the designer.
    ///     This glyph (and associated behavior) is created by the ParentControlDesigner. 
    /// </devdoc> 
    internal sealed class ContainerSelectorGlyph : Glyph {
 
        private Rectangle glyphBounds;
        private ContainerSelectorBehavior relatedBehavior;

        /// <include file='doc\ContainerSelectorGlyph.uex' path='docs/doc[@for="ContainerSelectorGlyph.ContainerSelectorGlyph"]/*' /> 
        /// <devdoc>
        ///     ContainerSelectorGlyph constructor. 
        /// </devdoc> 
        internal ContainerSelectorGlyph(Rectangle containerBounds, int glyphSize, int glyphOffset, ContainerSelectorBehavior behavior) : base(behavior) {
            relatedBehavior = (ContainerSelectorBehavior)behavior; 
            glyphBounds = new Rectangle(containerBounds.X + glyphOffset, containerBounds.Y - (int)(glyphSize * .5), glyphSize, glyphSize);
        }

 
        /// <include file='doc\ContainerSelectorGlyph.uex' path='docs/doc[@for="ContainerSelectorGlyph.Bounds"]/*' />
        /// <devdoc> 
        ///     The bounds of this Glyph. 
        /// </devdoc>
        public override Rectangle Bounds  { 
            get  {
                return glyphBounds;
            }
        } 

        public Behavior RelatedBehavior { 
            get { 
                return relatedBehavior;
            } 
        }


        /// <include file='doc\ContainerSelectorGlyph.uex' path='docs/doc[@for="ContainerSelectorGlyph.GetHitTest"]/*' /> 
        /// <devdoc>
        ///     Simple hit test rule: if the point is contained within the bounds 
        ///     - then it is a positive hit test. 
        /// </devdoc>
        public override Cursor GetHitTest(Point p) { 
            if (glyphBounds.Contains(p) || relatedBehavior.OkToMove) {
                return Cursors.SizeAll;
            }
            return null; 
        }
 
        private Bitmap glyph = null; 
        private Bitmap MoveGlyph  {
            get { 
                if (glyph  == null) {
                    glyph = new Bitmap(typeof(ContainerSelectorGlyph), "MoverGlyph.bmp");
                    glyph.MakeTransparent();
                } 

                return glyph; 
            } 
        }
 

        /// <include file='doc\GrabHandleGlyph.uex' path='docs/doc[@for="GrabHandleGlyph.Paint"]/*' />
        /// <devdoc>
        ///     Very simple paint logic. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) { 
            pe.Graphics.DrawImage(MoveGlyph, glyphBounds); 
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
    using System.Windows.Forms;

    /// <include file='doc\ContainerSelectorGlyph.uex' path='docs/doc[@for="ContainerSelectorGlyph"]/*' />
    /// <devdoc> 
    ///     This is the glyph used to drag container controls around the designer.
    ///     This glyph (and associated behavior) is created by the ParentControlDesigner. 
    /// </devdoc> 
    internal sealed class ContainerSelectorGlyph : Glyph {
 
        private Rectangle glyphBounds;
        private ContainerSelectorBehavior relatedBehavior;

        /// <include file='doc\ContainerSelectorGlyph.uex' path='docs/doc[@for="ContainerSelectorGlyph.ContainerSelectorGlyph"]/*' /> 
        /// <devdoc>
        ///     ContainerSelectorGlyph constructor. 
        /// </devdoc> 
        internal ContainerSelectorGlyph(Rectangle containerBounds, int glyphSize, int glyphOffset, ContainerSelectorBehavior behavior) : base(behavior) {
            relatedBehavior = (ContainerSelectorBehavior)behavior; 
            glyphBounds = new Rectangle(containerBounds.X + glyphOffset, containerBounds.Y - (int)(glyphSize * .5), glyphSize, glyphSize);
        }

 
        /// <include file='doc\ContainerSelectorGlyph.uex' path='docs/doc[@for="ContainerSelectorGlyph.Bounds"]/*' />
        /// <devdoc> 
        ///     The bounds of this Glyph. 
        /// </devdoc>
        public override Rectangle Bounds  { 
            get  {
                return glyphBounds;
            }
        } 

        public Behavior RelatedBehavior { 
            get { 
                return relatedBehavior;
            } 
        }


        /// <include file='doc\ContainerSelectorGlyph.uex' path='docs/doc[@for="ContainerSelectorGlyph.GetHitTest"]/*' /> 
        /// <devdoc>
        ///     Simple hit test rule: if the point is contained within the bounds 
        ///     - then it is a positive hit test. 
        /// </devdoc>
        public override Cursor GetHitTest(Point p) { 
            if (glyphBounds.Contains(p) || relatedBehavior.OkToMove) {
                return Cursors.SizeAll;
            }
            return null; 
        }
 
        private Bitmap glyph = null; 
        private Bitmap MoveGlyph  {
            get { 
                if (glyph  == null) {
                    glyph = new Bitmap(typeof(ContainerSelectorGlyph), "MoverGlyph.bmp");
                    glyph.MakeTransparent();
                } 

                return glyph; 
            } 
        }
 

        /// <include file='doc\GrabHandleGlyph.uex' path='docs/doc[@for="GrabHandleGlyph.Paint"]/*' />
        /// <devdoc>
        ///     Very simple paint logic. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) { 
            pe.Graphics.DrawImage(MoveGlyph, glyphBounds); 
        }
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
