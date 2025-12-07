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

    /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph "]/*' />
    /// <devdoc>
    ///     This glyph is associated with every row/column line rendered by the TableLayouPanelDesigner. 
    //      Each glyph simply tracks the bounds, type (row or column), and row/col style that it is associated
    //      with.  All glyphs on a TableLayoutPanelDesigner share one instance of the TableLayoutPanelBehavior. 
    /// </devdoc> 
    internal class TableLayoutPanelResizeGlyph : Glyph {
 
        private Rectangle                   bounds;//bounds of the column/row line
        private Cursor                      hitTestCursor;//cursor used for hittesting - vsplit/hsplit
        private TableLayoutStyle   style;//the style (row or column) associated
        private TableLayoutResizeType       type;//the "Type" used by the Behavior for resizing 

        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph .TableLayoutPanelResizeGlyph "]/*' /> 
        /// <devdoc> 
        ///     This constructor caches our necessary state and determine what 'type'
        ///     it is. 
        /// </devdoc>
        internal TableLayoutPanelResizeGlyph (Rectangle controlBounds, TableLayoutStyle style, Cursor hitTestCursor, Behavior behavior) : base(behavior) {
            this.bounds = controlBounds;
            this.hitTestCursor = hitTestCursor; 
            this.style = style;
 
            if (style is ColumnStyle) { 
                type = TableLayoutResizeType.Column;
            } 
            else {
                type = TableLayoutResizeType.Row;
            }
        } 

        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph .Bounds "]/*' /> 
        /// <devdoc> 
        ///     Represents the bounds of the row or column line being rendered
        ///     by the TableLayoutPanelDesigner. 
        /// </devdoc>
        public override Rectangle Bounds {
            get {
                return bounds; 
            }
        } 
 
        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph .Style"]/*' />
        /// <devdoc> 
        ///     Represents the Style associated with this glyph: Row or Column.
        ///     This is used by the behaviors resize methods to set the values.
        /// </devdoc>
        public TableLayoutStyle Style { 
            get {
                return style; 
            } 
        }
 
        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph .Type"]/*' />
        /// <devdoc>
        ///     Used as quick check by our behavior when dragging/resizing.
        /// </devdoc> 
        public TableLayoutResizeType Type {
            get { 
                return type; 
            }
        } 

        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph .GetHitTest"]/*' />
        /// <devdoc>
        ///     Simply returns the proper cursor if the mouse pointer is within 
        ///     our cached boudns.
        /// </devdoc> 
        public override Cursor GetHitTest(Point p) { 
            if (bounds.Contains(p)) {
                return hitTestCursor; 
            }

            return null;
        } 

        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph.Paint"]/*' /> 
        /// <devdoc> 
        ///     No painting necessary - this glyph is more of a 'hot spot'
        /// </devdoc> 
        public override void Paint(PaintEventArgs pe) {
        }

        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph.TableLayoutResizeType"]/*' /> 
        /// <devdoc>
        ///     Internal Enum defining the two different types of glyphs a TableLayoutPanel 
        ///     can have: column or row. 
        /// </devdoc>
        public enum TableLayoutResizeType { 
            Column,
            Row
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

    /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph "]/*' />
    /// <devdoc>
    ///     This glyph is associated with every row/column line rendered by the TableLayouPanelDesigner. 
    //      Each glyph simply tracks the bounds, type (row or column), and row/col style that it is associated
    //      with.  All glyphs on a TableLayoutPanelDesigner share one instance of the TableLayoutPanelBehavior. 
    /// </devdoc> 
    internal class TableLayoutPanelResizeGlyph : Glyph {
 
        private Rectangle                   bounds;//bounds of the column/row line
        private Cursor                      hitTestCursor;//cursor used for hittesting - vsplit/hsplit
        private TableLayoutStyle   style;//the style (row or column) associated
        private TableLayoutResizeType       type;//the "Type" used by the Behavior for resizing 

        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph .TableLayoutPanelResizeGlyph "]/*' /> 
        /// <devdoc> 
        ///     This constructor caches our necessary state and determine what 'type'
        ///     it is. 
        /// </devdoc>
        internal TableLayoutPanelResizeGlyph (Rectangle controlBounds, TableLayoutStyle style, Cursor hitTestCursor, Behavior behavior) : base(behavior) {
            this.bounds = controlBounds;
            this.hitTestCursor = hitTestCursor; 
            this.style = style;
 
            if (style is ColumnStyle) { 
                type = TableLayoutResizeType.Column;
            } 
            else {
                type = TableLayoutResizeType.Row;
            }
        } 

        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph .Bounds "]/*' /> 
        /// <devdoc> 
        ///     Represents the bounds of the row or column line being rendered
        ///     by the TableLayoutPanelDesigner. 
        /// </devdoc>
        public override Rectangle Bounds {
            get {
                return bounds; 
            }
        } 
 
        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph .Style"]/*' />
        /// <devdoc> 
        ///     Represents the Style associated with this glyph: Row or Column.
        ///     This is used by the behaviors resize methods to set the values.
        /// </devdoc>
        public TableLayoutStyle Style { 
            get {
                return style; 
            } 
        }
 
        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph .Type"]/*' />
        /// <devdoc>
        ///     Used as quick check by our behavior when dragging/resizing.
        /// </devdoc> 
        public TableLayoutResizeType Type {
            get { 
                return type; 
            }
        } 

        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph .GetHitTest"]/*' />
        /// <devdoc>
        ///     Simply returns the proper cursor if the mouse pointer is within 
        ///     our cached boudns.
        /// </devdoc> 
        public override Cursor GetHitTest(Point p) { 
            if (bounds.Contains(p)) {
                return hitTestCursor; 
            }

            return null;
        } 

        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph.Paint"]/*' /> 
        /// <devdoc> 
        ///     No painting necessary - this glyph is more of a 'hot spot'
        /// </devdoc> 
        public override void Paint(PaintEventArgs pe) {
        }

        /// <include file='doc\TableLayoutPanelResizeGlyph .uex' path='docs/doc[@for="TableLayoutPanelResizeGlyph.TableLayoutResizeType"]/*' /> 
        /// <devdoc>
        ///     Internal Enum defining the two different types of glyphs a TableLayoutPanel 
        ///     can have: column or row. 
        /// </devdoc>
        public enum TableLayoutResizeType { 
            Column,
            Row
        }
 
    }
 
 

} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
