 
namespace System.Windows.Forms.Design.Behavior {
    using System;
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing; 
    using System.Windows.Forms.Design;

    /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph"]/*' />
    /// <devdoc> 
    ///     A Glyph represents a single UI entity managed by an Adorner.  A Glyph
    ///     does not have an HWnd - and is rendered on the BehaviorService's 
    ///     AdornerWindow control.  Each Glyph can have a Behavior associated with 
    ///     it - the idea here is that a successfully Hit-Tested Glyph has the
    ///     opportunity to 'push' a new/different Behavior onto the BehaviorService's 
    ///     BehaviorStack.  Note that all Glyphs really do is paint and hit test.
    /// </devdoc>
    public abstract class Glyph {
 
        private Behavior behavior;//the Behaivor associated with the Glyph - can be null.
 
        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.Glyph"]/*' /> 
        /// <devdoc>
        ///     Glyph's default constructor takes a Behavior. 
        /// </devdoc>
        protected Glyph(Behavior behavior) {
            this.behavior = behavior;
        } 

        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.Behavior"]/*' /> 
        /// <devdoc> 
        ///     This read-only property will return the Behavior associated with
        ///     this Glyph.  The Behavior can be null. 
        /// </devdoc>
        public virtual Behavior Behavior{
            get {
                return behavior; 
            }
        } 
 
        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.Bounds"]/*' />
        /// <devdoc> 
        ///     This read-only property will return the Bounds associated with
        ///     this Glyph.  The Bounds can be empty.
        /// </devdoc>
        public virtual Rectangle Bounds  { 
            get  {
                return Rectangle.Empty; 
            } 
        }
 
        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.GetHitTest"]/*' />
        /// <devdoc>
        ///     Abstract method that forces Glyph implementations to provide
        ///     hit test logic.  Given any point - if the Glyph has decided to 
        ///     be involved with that location, the Glyph will need to return
        ///     a valid Cursor.  Otherwise, returning null will cause the 
        ///     the BehaviorService to simply ignore it. 
        /// </devdoc>
        public abstract Cursor GetHitTest(Point p); 

        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.Paint"]/*' />
        /// <devdoc>
        ///     Abstract method that forces Glyph implementations to provide 
        ///     paint logic.  The PaintEventArgs object passed into this method
        ///     contains the Graphics object related to the BehaviorService's 
        ///     AdornerWindow. 
        /// </devdoc>
        public abstract void Paint(PaintEventArgs pe); 

        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.SetBehavior"]/*' />
        /// <devdoc>
        ///     This method is called by inheriting classes to change the 
        ///     Behavior object associated with the Glyph.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")] 
        protected void SetBehavior(Behavior behavior) {
            this.behavior = behavior; 
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
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing; 
    using System.Windows.Forms.Design;

    /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph"]/*' />
    /// <devdoc> 
    ///     A Glyph represents a single UI entity managed by an Adorner.  A Glyph
    ///     does not have an HWnd - and is rendered on the BehaviorService's 
    ///     AdornerWindow control.  Each Glyph can have a Behavior associated with 
    ///     it - the idea here is that a successfully Hit-Tested Glyph has the
    ///     opportunity to 'push' a new/different Behavior onto the BehaviorService's 
    ///     BehaviorStack.  Note that all Glyphs really do is paint and hit test.
    /// </devdoc>
    public abstract class Glyph {
 
        private Behavior behavior;//the Behaivor associated with the Glyph - can be null.
 
        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.Glyph"]/*' /> 
        /// <devdoc>
        ///     Glyph's default constructor takes a Behavior. 
        /// </devdoc>
        protected Glyph(Behavior behavior) {
            this.behavior = behavior;
        } 

        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.Behavior"]/*' /> 
        /// <devdoc> 
        ///     This read-only property will return the Behavior associated with
        ///     this Glyph.  The Behavior can be null. 
        /// </devdoc>
        public virtual Behavior Behavior{
            get {
                return behavior; 
            }
        } 
 
        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.Bounds"]/*' />
        /// <devdoc> 
        ///     This read-only property will return the Bounds associated with
        ///     this Glyph.  The Bounds can be empty.
        /// </devdoc>
        public virtual Rectangle Bounds  { 
            get  {
                return Rectangle.Empty; 
            } 
        }
 
        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.GetHitTest"]/*' />
        /// <devdoc>
        ///     Abstract method that forces Glyph implementations to provide
        ///     hit test logic.  Given any point - if the Glyph has decided to 
        ///     be involved with that location, the Glyph will need to return
        ///     a valid Cursor.  Otherwise, returning null will cause the 
        ///     the BehaviorService to simply ignore it. 
        /// </devdoc>
        public abstract Cursor GetHitTest(Point p); 

        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.Paint"]/*' />
        /// <devdoc>
        ///     Abstract method that forces Glyph implementations to provide 
        ///     paint logic.  The PaintEventArgs object passed into this method
        ///     contains the Graphics object related to the BehaviorService's 
        ///     AdornerWindow. 
        /// </devdoc>
        public abstract void Paint(PaintEventArgs pe); 

        /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.SetBehavior"]/*' />
        /// <devdoc>
        ///     This method is called by inheriting classes to change the 
        ///     Behavior object associated with the Glyph.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")] 
        protected void SetBehavior(Behavior behavior) {
            this.behavior = behavior; 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
