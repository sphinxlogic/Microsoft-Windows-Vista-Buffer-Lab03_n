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

    /// <include file='doc\BodyGlyph.uex' path='docs/doc[@for="BodyGlyph"]/*' />
    /// <devdoc>
    ///     This Glyph is placed on every control sized to the exact bounds of 
    ///     the control.
    /// </devdoc> 
    public class ControlBodyGlyph : ComponentGlyph { 

        private Rectangle bounds; //bounds of the related control 
        private Cursor hitTestCursor; //cursor used to hit test
        private IComponent component;

 
        /// <include file='doc\BodyGlyph.uex' path='docs/doc[@for="BodyGlyph.BodyGlyph"]/*' />
        /// <devdoc> 
        ///     Standard Constructor. 
        /// </devdoc>
        public ControlBodyGlyph(Rectangle bounds, Cursor cursor, IComponent relatedComponent, ControlDesigner designer) : base(relatedComponent, new ControlDesigner.TransparentBehavior(designer)) { 
            this.bounds = bounds;
            this.hitTestCursor = cursor;
            this.component = relatedComponent;
        } 

        public ControlBodyGlyph(Rectangle bounds, Cursor cursor, IComponent relatedComponent, Behavior behavior) : base(relatedComponent, behavior) { 
            this.bounds = bounds; 
            this.hitTestCursor = cursor;
            this.component = relatedComponent; 
        }

        /// <include file='doc\BodyGlyph.uex' path='docs/doc[@for="BodyGlyph.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Simple hit test rule: if the point is contained within the bounds
        ///     AND the component is Visible (controls on some tab pages may 
        ///     not be, for ex) then it is a positive hit test. 
        /// </devdoc>
        public override Cursor GetHitTest(Point p) { 

            bool isVisible = (component is Control) ? ((Control)component).Visible : true; /*non-controls are always visible here*/

            if (isVisible && bounds.Contains(p)) { 
                return hitTestCursor;
            } 
            return null; 
        }
 
        /// <include file='doc\BodyGlyph.uex' path='docs/doc[@for="BodyGlyph.Bounds"]/*' />
        /// <devdoc>
        ///     The bounds of this glyph.
        /// </devdoc> 

        public override Rectangle Bounds  { 
            get  { 
                return bounds;
            } 
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

    /// <include file='doc\BodyGlyph.uex' path='docs/doc[@for="BodyGlyph"]/*' />
    /// <devdoc>
    ///     This Glyph is placed on every control sized to the exact bounds of 
    ///     the control.
    /// </devdoc> 
    public class ControlBodyGlyph : ComponentGlyph { 

        private Rectangle bounds; //bounds of the related control 
        private Cursor hitTestCursor; //cursor used to hit test
        private IComponent component;

 
        /// <include file='doc\BodyGlyph.uex' path='docs/doc[@for="BodyGlyph.BodyGlyph"]/*' />
        /// <devdoc> 
        ///     Standard Constructor. 
        /// </devdoc>
        public ControlBodyGlyph(Rectangle bounds, Cursor cursor, IComponent relatedComponent, ControlDesigner designer) : base(relatedComponent, new ControlDesigner.TransparentBehavior(designer)) { 
            this.bounds = bounds;
            this.hitTestCursor = cursor;
            this.component = relatedComponent;
        } 

        public ControlBodyGlyph(Rectangle bounds, Cursor cursor, IComponent relatedComponent, Behavior behavior) : base(relatedComponent, behavior) { 
            this.bounds = bounds; 
            this.hitTestCursor = cursor;
            this.component = relatedComponent; 
        }

        /// <include file='doc\BodyGlyph.uex' path='docs/doc[@for="BodyGlyph.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Simple hit test rule: if the point is contained within the bounds
        ///     AND the component is Visible (controls on some tab pages may 
        ///     not be, for ex) then it is a positive hit test. 
        /// </devdoc>
        public override Cursor GetHitTest(Point p) { 

            bool isVisible = (component is Control) ? ((Control)component).Visible : true; /*non-controls are always visible here*/

            if (isVisible && bounds.Contains(p)) { 
                return hitTestCursor;
            } 
            return null; 
        }
 
        /// <include file='doc\BodyGlyph.uex' path='docs/doc[@for="BodyGlyph.Bounds"]/*' />
        /// <devdoc>
        ///     The bounds of this glyph.
        /// </devdoc> 

        public override Rectangle Bounds  { 
            get  { 
                return bounds;
            } 
        }
    }

} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
