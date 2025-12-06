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

    /// <include file='doc\DesignerActionGlyph.uex' path='docs/doc[@for="DesignerActionGlyph"]/*' />
    /// <devdoc>
    ///     This Glyph represents the UI appended to a control when DesignerActions are 
    ///     available. Each image that represents these
    ///     states are demand created.  This is done because it is entirely possible 
    ///     that a DesignerActionGlyph will only ever be in one of these states during 
    ///     its lifetime... kind of sad really.
    /// </devdoc> 
    internal sealed class DesignerActionGlyph : Glyph {

        internal const int CONTROLOVERLAP_X = 5;//number of pixels the anchor should be offset to the left of the control's upper-right
        internal const int CONTROLOVERLAP_Y = 2;//number of pixels the anchor overlaps the control in the y-direction 

        private Rectangle       bounds;//the bounds of our glyph 
        private Adorner         adorner;//A ptr back to our adorner - so when we decide to change state, we can invalidate 
        private bool            mouseOver;//on mouse over, we shade our image differently, this is used to track that state
        private Rectangle       alternativeBounds = Rectangle.Empty;//if !empty, this represents the bounds of the tray control this gyph is related to 
        private Control         alternativeParent;//if this is valid - then the glyph will invalidate itself here instead of on the adorner


        private DockStyle dockStyle; 
        private Bitmap glyphImageClosed;
        private Bitmap glyphImageOpened; 
 

        /// <include file='doc\DesignerActionGlyph.uex' path='docs/doc[@for="DesignerActionGlyph.DesignerActionGlyph1"]/*' /> 
        /// <devdoc>
        ///     Constructor that passes empty alternative bounds and parents.
        ///     Typically this is done for control on the designer's surface since
        ///     component tray glyphs will have these alternative values. 
        /// </devdoc>
        public DesignerActionGlyph(DesignerActionBehavior behavior, Adorner adorner) : 
                              this(behavior, adorner, Rectangle.Empty, null) {} 
        public DesignerActionGlyph(DesignerActionBehavior behavior,    Rectangle alternativeBounds, Control alternativeParent) :
                              this(behavior, null, alternativeBounds, alternativeParent) {} 

        /// <include file='doc\DesignerActionGlyph.uex' path='docs/doc[@for="DesignerActionGlyph.DesignerActionGlyph2"]/*' />
        /// <devdoc>
        ///     Constructor that sets the dropdownbox size, creates a our hottrack brush 
        ///     and invalidates the glyph (to configure location).
        /// </devdoc> 
        private DesignerActionGlyph(DesignerActionBehavior behavior, Adorner adorner,    Rectangle alternativeBounds, Control alternativeParent) : 
                              base(behavior) {
            this.adorner = adorner; 
            this.alternativeBounds = alternativeBounds;
            this.alternativeParent = alternativeParent;
            Invalidate();
        } 

        /// <include file='doc\DesignerGlyphBase.uex' path='docs/doc[@for="DesignerGlyphBase.Bounds"]/*' /> 
        /// <devdoc> 
        ///     Returns the bounds of our glyph.  This is used by the related Behavior
        ///     to determine where to show the contextmenu (list of actions). 
        /// </devdoc>
        public override Rectangle Bounds {
            get {
                return bounds; 
            }
        } 
 

        public DockStyle DockEdge { 
            get {
                return dockStyle;
            }
            set { 
                if(dockStyle != value) {
                    dockStyle = value; 
                } 
            }
        } 

        public bool IsInComponentTray {
            get {
                return (adorner == null); // adorner and alternative bounds are exclusive 
            }
        } 
 
        /// <include file='doc\DesignerGlyphBase.uex' path='docs/doc[@for="DesignerGlyphBase.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Standard hit test logic that returns true if the point is contained within our bounds.
        ///     This is also used to manage out mouse over state.
        /// </devdoc>
        public override Cursor GetHitTest(Point p) { 
            if (bounds.Contains(p)) {
                MouseOver = true; 
                return Cursors.Default; 
            }
 
            MouseOver = false;
            return null;
        }
 
        /// <devdoc>
        ///     Returns an image representing the 
        /// </devdoc> 
        private  Image GlyphImageClosed {
            get { 
                if(glyphImageClosed == null) {
                    glyphImageClosed  = new Bitmap(typeof(DesignerActionGlyph), "Close_left.bmp");
                    glyphImageClosed.MakeTransparent(Color.Magenta);
                } 

                return glyphImageClosed; 
            } 
        }
 
        private Image GlyphImageOpened {
            get {
                if(glyphImageOpened == null) {
                    glyphImageOpened  = new Bitmap(typeof(DesignerActionGlyph), "Open_left.bmp"); 
                    glyphImageOpened.MakeTransparent(Color.Magenta);
                } 
 
                return glyphImageOpened;
            } 
        }

        internal void InvalidateOwnerLocation() {
            if (alternativeParent != null) { // alternative parent and adoner are exclusive... 
                alternativeParent.Invalidate(bounds);
            } 
            else { 
                adorner.Invalidate(bounds);
            } 
        }

        /// <include file='doc\DesignerActionGlyph.uex' path='docs/doc[@for="DesignerActionGlyph.Invalidate"]/*' />
        /// <devdoc> 
        ///     Called when the state for this DesignerActionGlyph changes.  Or when the related
        ///     component's size or location change.  Here, we re-calculate the Glyph's bounds 
        ///     and change our image. 
        /// </devdoc>
        internal void Invalidate() { 
            IComponent relatedComponent = ((DesignerActionBehavior)Behavior).RelatedComponent;

            Point topRight = Point.Empty;
 
            //handle the case that our comp is a control
            Control relatedControl = relatedComponent as Control; 
            if (relatedControl != null && !(relatedComponent is ToolStripDropDown) && adorner != null) { 
                topRight = adorner.BehaviorService.ControlToAdornerWindow(relatedControl);
                Control parentControl = relatedControl.Parent; 
                topRight.X += relatedControl.Width;
            }
            //ISSUE: we can't have this special cased here - we should find a more
            //generic approach to solving this problem 
            //special logic here for our comp being a toolstrip item
            else  { 
                // update alternative bounds if possible... 
                ComponentTray compTray = alternativeParent as ComponentTray;
                if (compTray != null) { 
                    ComponentTray.TrayControl trayControl = compTray.GetTrayControlFromComponent(relatedComponent);
                    if (trayControl != null) {
                        alternativeBounds = trayControl.Bounds;
                    } 
                }
                Rectangle newRect = DesignerUtils.GetBoundsForNoResizeSelectionType(alternativeBounds, SelectionBorderGlyphType.Top); 
                topRight.X = newRect.Right; 
                topRight.Y = newRect.Top;
            } 

            topRight.X -= (GlyphImageOpened.Width + CONTROLOVERLAP_X);
            topRight.Y -= (GlyphImageOpened.Height - CONTROLOVERLAP_Y);
            bounds = (new Rectangle(topRight.X, topRight.Y, GlyphImageOpened.Width,GlyphImageOpened.Height)); 

 
        } 

 
        /// <devdoc>
        ///     Used to manage the mouse-pointer-is-over-glyph state.  If this is true,
        ///     then we will shade our BoxImage in the Paint logic.
        /// </devdoc> 
        private bool MouseOver {
            get { 
                return mouseOver; 
            }
            set { 
                if (mouseOver != value) {
                    mouseOver = value;

                    InvalidateOwnerLocation(); 
                }
            } 
        } 

 
        /// <include file='doc\DesignerGlyphBase.uex' path='docs/doc[@for="DesignerGlyphBase.Paint"]/*' />
        /// <devdoc>
        ///     Responds to a paint event.  This Glyph will paint its current image and, if
        ///     MouseHover is true, we'll paint over the image with the 'hoverBrush'. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) { 
            Image image; 
            if(Behavior is DesignerActionBehavior) {
                IComponent panelComponent = ((DesignerActionUI)((DesignerActionBehavior)Behavior).ParentUI).LastPanelComponent; 
                IComponent relatedComponent = ((DesignerActionBehavior)Behavior).RelatedComponent;

                if (panelComponent != null && panelComponent == relatedComponent) {
                    image = GlyphImageOpened; 
                } else {
                    image = GlyphImageClosed; 
                } 
                pe.Graphics.DrawImage(image, bounds.Left, bounds.Top);
                if (MouseOver || (panelComponent != null && panelComponent == relatedComponent)) { 
                    pe.Graphics.FillRectangle(DesignerUtils.HoverBrush, Rectangle.Inflate(bounds, -1, -1));
                }
            }
        } 

        /// <include file='doc\DesignerGlyphBase.uex' path='docs/doc[@for="DesignerGlyphBase.UpdateAlternativeBounds"]/*' /> 
        /// <devdoc> 
        ///     Called by the ComponentTray when a tray control changes location.
        /// </devdoc> 
        internal void UpdateAlternativeBounds(Rectangle newBounds) {
            alternativeBounds = newBounds;
            Invalidate();
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

    /// <include file='doc\DesignerActionGlyph.uex' path='docs/doc[@for="DesignerActionGlyph"]/*' />
    /// <devdoc>
    ///     This Glyph represents the UI appended to a control when DesignerActions are 
    ///     available. Each image that represents these
    ///     states are demand created.  This is done because it is entirely possible 
    ///     that a DesignerActionGlyph will only ever be in one of these states during 
    ///     its lifetime... kind of sad really.
    /// </devdoc> 
    internal sealed class DesignerActionGlyph : Glyph {

        internal const int CONTROLOVERLAP_X = 5;//number of pixels the anchor should be offset to the left of the control's upper-right
        internal const int CONTROLOVERLAP_Y = 2;//number of pixels the anchor overlaps the control in the y-direction 

        private Rectangle       bounds;//the bounds of our glyph 
        private Adorner         adorner;//A ptr back to our adorner - so when we decide to change state, we can invalidate 
        private bool            mouseOver;//on mouse over, we shade our image differently, this is used to track that state
        private Rectangle       alternativeBounds = Rectangle.Empty;//if !empty, this represents the bounds of the tray control this gyph is related to 
        private Control         alternativeParent;//if this is valid - then the glyph will invalidate itself here instead of on the adorner


        private DockStyle dockStyle; 
        private Bitmap glyphImageClosed;
        private Bitmap glyphImageOpened; 
 

        /// <include file='doc\DesignerActionGlyph.uex' path='docs/doc[@for="DesignerActionGlyph.DesignerActionGlyph1"]/*' /> 
        /// <devdoc>
        ///     Constructor that passes empty alternative bounds and parents.
        ///     Typically this is done for control on the designer's surface since
        ///     component tray glyphs will have these alternative values. 
        /// </devdoc>
        public DesignerActionGlyph(DesignerActionBehavior behavior, Adorner adorner) : 
                              this(behavior, adorner, Rectangle.Empty, null) {} 
        public DesignerActionGlyph(DesignerActionBehavior behavior,    Rectangle alternativeBounds, Control alternativeParent) :
                              this(behavior, null, alternativeBounds, alternativeParent) {} 

        /// <include file='doc\DesignerActionGlyph.uex' path='docs/doc[@for="DesignerActionGlyph.DesignerActionGlyph2"]/*' />
        /// <devdoc>
        ///     Constructor that sets the dropdownbox size, creates a our hottrack brush 
        ///     and invalidates the glyph (to configure location).
        /// </devdoc> 
        private DesignerActionGlyph(DesignerActionBehavior behavior, Adorner adorner,    Rectangle alternativeBounds, Control alternativeParent) : 
                              base(behavior) {
            this.adorner = adorner; 
            this.alternativeBounds = alternativeBounds;
            this.alternativeParent = alternativeParent;
            Invalidate();
        } 

        /// <include file='doc\DesignerGlyphBase.uex' path='docs/doc[@for="DesignerGlyphBase.Bounds"]/*' /> 
        /// <devdoc> 
        ///     Returns the bounds of our glyph.  This is used by the related Behavior
        ///     to determine where to show the contextmenu (list of actions). 
        /// </devdoc>
        public override Rectangle Bounds {
            get {
                return bounds; 
            }
        } 
 

        public DockStyle DockEdge { 
            get {
                return dockStyle;
            }
            set { 
                if(dockStyle != value) {
                    dockStyle = value; 
                } 
            }
        } 

        public bool IsInComponentTray {
            get {
                return (adorner == null); // adorner and alternative bounds are exclusive 
            }
        } 
 
        /// <include file='doc\DesignerGlyphBase.uex' path='docs/doc[@for="DesignerGlyphBase.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Standard hit test logic that returns true if the point is contained within our bounds.
        ///     This is also used to manage out mouse over state.
        /// </devdoc>
        public override Cursor GetHitTest(Point p) { 
            if (bounds.Contains(p)) {
                MouseOver = true; 
                return Cursors.Default; 
            }
 
            MouseOver = false;
            return null;
        }
 
        /// <devdoc>
        ///     Returns an image representing the 
        /// </devdoc> 
        private  Image GlyphImageClosed {
            get { 
                if(glyphImageClosed == null) {
                    glyphImageClosed  = new Bitmap(typeof(DesignerActionGlyph), "Close_left.bmp");
                    glyphImageClosed.MakeTransparent(Color.Magenta);
                } 

                return glyphImageClosed; 
            } 
        }
 
        private Image GlyphImageOpened {
            get {
                if(glyphImageOpened == null) {
                    glyphImageOpened  = new Bitmap(typeof(DesignerActionGlyph), "Open_left.bmp"); 
                    glyphImageOpened.MakeTransparent(Color.Magenta);
                } 
 
                return glyphImageOpened;
            } 
        }

        internal void InvalidateOwnerLocation() {
            if (alternativeParent != null) { // alternative parent and adoner are exclusive... 
                alternativeParent.Invalidate(bounds);
            } 
            else { 
                adorner.Invalidate(bounds);
            } 
        }

        /// <include file='doc\DesignerActionGlyph.uex' path='docs/doc[@for="DesignerActionGlyph.Invalidate"]/*' />
        /// <devdoc> 
        ///     Called when the state for this DesignerActionGlyph changes.  Or when the related
        ///     component's size or location change.  Here, we re-calculate the Glyph's bounds 
        ///     and change our image. 
        /// </devdoc>
        internal void Invalidate() { 
            IComponent relatedComponent = ((DesignerActionBehavior)Behavior).RelatedComponent;

            Point topRight = Point.Empty;
 
            //handle the case that our comp is a control
            Control relatedControl = relatedComponent as Control; 
            if (relatedControl != null && !(relatedComponent is ToolStripDropDown) && adorner != null) { 
                topRight = adorner.BehaviorService.ControlToAdornerWindow(relatedControl);
                Control parentControl = relatedControl.Parent; 
                topRight.X += relatedControl.Width;
            }
            //ISSUE: we can't have this special cased here - we should find a more
            //generic approach to solving this problem 
            //special logic here for our comp being a toolstrip item
            else  { 
                // update alternative bounds if possible... 
                ComponentTray compTray = alternativeParent as ComponentTray;
                if (compTray != null) { 
                    ComponentTray.TrayControl trayControl = compTray.GetTrayControlFromComponent(relatedComponent);
                    if (trayControl != null) {
                        alternativeBounds = trayControl.Bounds;
                    } 
                }
                Rectangle newRect = DesignerUtils.GetBoundsForNoResizeSelectionType(alternativeBounds, SelectionBorderGlyphType.Top); 
                topRight.X = newRect.Right; 
                topRight.Y = newRect.Top;
            } 

            topRight.X -= (GlyphImageOpened.Width + CONTROLOVERLAP_X);
            topRight.Y -= (GlyphImageOpened.Height - CONTROLOVERLAP_Y);
            bounds = (new Rectangle(topRight.X, topRight.Y, GlyphImageOpened.Width,GlyphImageOpened.Height)); 

 
        } 

 
        /// <devdoc>
        ///     Used to manage the mouse-pointer-is-over-glyph state.  If this is true,
        ///     then we will shade our BoxImage in the Paint logic.
        /// </devdoc> 
        private bool MouseOver {
            get { 
                return mouseOver; 
            }
            set { 
                if (mouseOver != value) {
                    mouseOver = value;

                    InvalidateOwnerLocation(); 
                }
            } 
        } 

 
        /// <include file='doc\DesignerGlyphBase.uex' path='docs/doc[@for="DesignerGlyphBase.Paint"]/*' />
        /// <devdoc>
        ///     Responds to a paint event.  This Glyph will paint its current image and, if
        ///     MouseHover is true, we'll paint over the image with the 'hoverBrush'. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) { 
            Image image; 
            if(Behavior is DesignerActionBehavior) {
                IComponent panelComponent = ((DesignerActionUI)((DesignerActionBehavior)Behavior).ParentUI).LastPanelComponent; 
                IComponent relatedComponent = ((DesignerActionBehavior)Behavior).RelatedComponent;

                if (panelComponent != null && panelComponent == relatedComponent) {
                    image = GlyphImageOpened; 
                } else {
                    image = GlyphImageClosed; 
                } 
                pe.Graphics.DrawImage(image, bounds.Left, bounds.Top);
                if (MouseOver || (panelComponent != null && panelComponent == relatedComponent)) { 
                    pe.Graphics.FillRectangle(DesignerUtils.HoverBrush, Rectangle.Inflate(bounds, -1, -1));
                }
            }
        } 

        /// <include file='doc\DesignerGlyphBase.uex' path='docs/doc[@for="DesignerGlyphBase.UpdateAlternativeBounds"]/*' /> 
        /// <devdoc> 
        ///     Called by the ComponentTray when a tray control changes location.
        /// </devdoc> 
        internal void UpdateAlternativeBounds(Rectangle newBounds) {
            alternativeBounds = newBounds;
            Invalidate();
        } 

    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
