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

    /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph"]/*' />
    /// <devdoc>
    ///     The ComponentGlyph class simply contains a pointer back 
    ///     to it's related Component.  This can be used to trace
    ///     Glyphs (during drag operations or otherwise) back to 
    ///     their component. 
    /// </devdoc>
    public class ComponentGlyph : Glyph { 

        private IComponent relatedComponent;//ptr back to the component

        /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph.ComponentGlyph"]/*' /> 
        /// <devdoc>
        ///     Standard constructor. 
        /// </devdoc> 
        public ComponentGlyph(IComponent relatedComponent, Behavior behavior) : base(behavior) {
            this.relatedComponent = relatedComponent; 
        }

        public ComponentGlyph(IComponent relatedComponent) : base(null) {
            this.relatedComponent = relatedComponent; 
        }
 
        /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph.RelatedComponent"]/*' /> 
        /// <devdoc>
        ///     Returns the Component this Glyph is related to. 
        /// </devdoc>
        public IComponent RelatedComponent {
            get {
                return relatedComponent; 
            }
        } 
 
        /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Overrides GetHitTest - this implementation does nothing.
        /// </devdoc>
        public override Cursor GetHitTest(Point p) {
            return null; 
        }
 
        /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph.Paint"]/*' /> 
        /// <devdoc>
        ///     Overrides Glyph::Paint - this implementation does nothing. 
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

    /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph"]/*' />
    /// <devdoc>
    ///     The ComponentGlyph class simply contains a pointer back 
    ///     to it's related Component.  This can be used to trace
    ///     Glyphs (during drag operations or otherwise) back to 
    ///     their component. 
    /// </devdoc>
    public class ComponentGlyph : Glyph { 

        private IComponent relatedComponent;//ptr back to the component

        /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph.ComponentGlyph"]/*' /> 
        /// <devdoc>
        ///     Standard constructor. 
        /// </devdoc> 
        public ComponentGlyph(IComponent relatedComponent, Behavior behavior) : base(behavior) {
            this.relatedComponent = relatedComponent; 
        }

        public ComponentGlyph(IComponent relatedComponent) : base(null) {
            this.relatedComponent = relatedComponent; 
        }
 
        /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph.RelatedComponent"]/*' /> 
        /// <devdoc>
        ///     Returns the Component this Glyph is related to. 
        /// </devdoc>
        public IComponent RelatedComponent {
            get {
                return relatedComponent; 
            }
        } 
 
        /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Overrides GetHitTest - this implementation does nothing.
        /// </devdoc>
        public override Cursor GetHitTest(Point p) {
            return null; 
        }
 
        /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph.Paint"]/*' /> 
        /// <devdoc>
        ///     Overrides Glyph::Paint - this implementation does nothing. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) {
        }
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
