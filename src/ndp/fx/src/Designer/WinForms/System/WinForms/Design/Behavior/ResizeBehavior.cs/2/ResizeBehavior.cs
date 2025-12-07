 
namespace System.Windows.Forms.Design.Behavior {
    using System;
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Windows.Forms.Design; 
    using System.Runtime.InteropServices;
    using System.Drawing.Drawing2D;

    /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="ResizeBehavior"]/*' /> 
    /// <devdoc>
    ///     The ResizeBehavior is pushed onto the BehaviorStack in response to a 
    ///     positively hit tested SelectionGlyph.  The ResizeBehavior simply 
    ///     tracks the MouseMove messages and updates the bounds of the relatd
    ///     control based on the new mouse location and the resize Rules. 
    /// </devdoc>
    internal class ResizeBehavior : Behavior {

        private struct ResizeComponent { 
            public object resizeControl;
            public Rectangle resizeBounds; 
            public SelectionRules resizeRules; 
        };
 
        private ResizeComponent[]       resizeComponents;
        private IServiceProvider        serviceProvider;
        private BehaviorService         behaviorService;
        private SelectionRules          targetResizeRules;//rules dictating which sizes we can change 
        private Point                   initialPoint;//the initial point of the mouse down
        private bool                    dragging;//indicates that the behavior is currently 'dragging' 
        private bool                    pushedBehavior; 
        private bool                    initialResize;//true for the first resize of the control, false after that.
        private DesignerTransaction     resizeTransaction;//the transaction we create for the resize 
        private const int               MINSIZE = 10;
        private const int               borderSize = 2;
        private DragAssistanceManager   dragManager;//this object will integrate SnapLines into the resize
        private Point                   lastMouseLoc;//helps us avoid re-entering code if the mouse hasn't moved 
        private Point                   parentLocation;//used to snap resize ops to the grid
        private Size                    parentGridSize;//used to snap resize ops to the grid 
        private NativeMethods.POINT     lastMouseAbs; // last absolute mouse position 
        private Point                   lastSnapOffset;//the last snapoffset we used.
        private bool                    didSnap; //did we actually snap. 
        private Control                 primaryControl;//the primary control the status bar will queue off of

        private Cursor                  cursor = Cursors.Default; //used to set the correct cursor during resizing
        private StatusCommandUI         statusCommandUI; // used to update the StatusBar Information. 

        private Graphics                graphics;//graphics object of the adornerwindow (via BehaviorService) 
        private Region                  lastResizeRegion; 

        /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="ResizeBehavior.ResizeBehavior"]/*' /> 
        /// <devdoc>
        ///     Constructor that caches all values for perf. reasons.
        /// </devdoc>
        internal ResizeBehavior(IServiceProvider serviceProvider) { 
            this.serviceProvider = serviceProvider;
            dragging = false; 
            pushedBehavior = false; 
            lastSnapOffset = Point.Empty;
            didSnap = false; 
            statusCommandUI = new StatusCommandUI(serviceProvider);
        }

        /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="SelectionBehavior.BehaviorService"]/*' /> 
        /// <devdoc>
        ///     Demand creates the BehaviorService. 
        /// </devdoc> 
        private BehaviorService BehaviorService {
            get { 
                if (behaviorService == null) {
                    behaviorService = (BehaviorService)serviceProvider.GetService(typeof(BehaviorService));
                }
                return behaviorService; 
            }
        } 
 
        public override Cursor Cursor {
            get { 
                return cursor;
            }
        }
 
        /// <devdoc>
        ///     Called during the resize operation, we'll try to determine 
        ///     an offset so that the controls snap to the grid settings 
        ///     of the parent.
        /// </devdoc> 
        private Rectangle AdjustToGrid(Rectangle controlBounds, SelectionRules rules) {

            Rectangle rect = controlBounds;
 
            if ((rules & SelectionRules.RightSizeable) != 0) {
                int xDelta = controlBounds.Right % parentGridSize.Width; 
                if (xDelta > parentGridSize.Width /2) { 
                    rect.Width += parentGridSize.Width - xDelta;
                } 
                else {
                    rect.Width -= xDelta;
                }
            } 
            else if ((rules & SelectionRules.LeftSizeable) != 0) {
                int xDelta = controlBounds.Left % parentGridSize.Width; 
                if (xDelta > parentGridSize.Width /2) { 
                    rect.X += parentGridSize.Width - xDelta;
                    rect.Width -= parentGridSize.Width - xDelta; 
                }
                else {
                    rect.X-= xDelta;
                    rect.Width += xDelta; 
                }
            } 
 
            if ((rules & SelectionRules.BottomSizeable) != 0) {
                int yDelta = controlBounds.Bottom % parentGridSize.Height; 
                if (yDelta > parentGridSize.Height /2) {
                    rect.Height += parentGridSize.Height - yDelta;
                }
                else { 
                    rect.Height -= yDelta;
                } 
            } 
            else if ((rules & SelectionRules.TopSizeable) != 0) {
                int yDelta = controlBounds.Top % parentGridSize.Height; 
                if (yDelta > parentGridSize.Height /2) {
                    rect.Y += parentGridSize.Height - yDelta;
                    rect.Height -= parentGridSize.Height - yDelta;
                } 
                else {
                    rect.Y -= yDelta; 
                    rect.Height += yDelta; 
                }
            } 

            //validate our dimensions
            rect.Width = Math.Max(rect.Width, parentGridSize.Width);
            rect.Height = Math.Max(rect.Height, parentGridSize.Height); 

            return rect; 
        } 

        /// <include file='doc\SelectionBehavior.uex' path='docs/doc[@for="SelectionBehavior.GenerateSnapLines"]/*' /> 
        /// <devdoc>
        ///     Builds up an array of snaplines used during resize to adjust/snap
        ///     the controls bounds.
        /// </devdoc> 
        private SnapLine[] GenerateSnapLines(SelectionRules rules, Point loc) {
            ArrayList lines = new ArrayList(2); 
 
            //the four margins and edges of our control
            // 
            if ((rules & SelectionRules.BottomSizeable) != 0) {
                lines.Add(new SnapLine(SnapLineType.Bottom, loc.Y-1));
                if (primaryControl != null) {
                    lines.Add( new SnapLine(SnapLineType.Horizontal, loc.Y + primaryControl.Margin.Bottom, SnapLine.MarginBottom, SnapLinePriority.Always)); 
                }
            } 
            else if ((rules & SelectionRules.TopSizeable) != 0) { 
                lines.Add(new SnapLine(SnapLineType.Top, loc.Y));
                if (primaryControl != null) { 
                    lines.Add( new SnapLine(SnapLineType.Horizontal, loc.Y -primaryControl.Margin.Top, SnapLine.MarginTop, SnapLinePriority.Always));
                }
            }
 
            if ((rules & SelectionRules.RightSizeable) != 0) {
                lines.Add(new SnapLine(SnapLineType.Right, loc.X-1)); 
                if (primaryControl != null) { 
                    lines.Add( new SnapLine(SnapLineType.Vertical, loc.X + primaryControl.Margin.Right, SnapLine.MarginRight, SnapLinePriority.Always));
                } 
            }
            else if ((rules & SelectionRules.LeftSizeable) != 0) {
                lines.Add(new SnapLine(SnapLineType.Left, loc.X));
                if (primaryControl != null) { 
                    lines.Add( new SnapLine(SnapLineType.Vertical, loc.X -primaryControl.Margin.Left, SnapLine.MarginLeft, SnapLinePriority.Always));
                } 
            } 

            SnapLine[] l = new SnapLine[lines.Count]; 
            lines.CopyTo(l);

            return l;
        } 

        /// <devdoc> 
        ///     This is called in response to the mouse moving far enough 
        ///     away from its initial point.  Basically, we calculate the
        ///     bounds for each control we're resizing and disable any 
        ///     adorners.
        /// </devdoc>
        private void InitiateResize() {
 
            bool useSnapLines = BehaviorService.UseSnapLines;
            ArrayList components = new ArrayList(); 
 
            //check to see if the current designer participate with SnapLines
            IDesignerHost designerHost = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost; 

            //cache the control bounds
            for (int i = 0; i < resizeComponents.Length; i++) {
                resizeComponents[i].resizeBounds = ((Control)(resizeComponents[i].resizeControl)).Bounds; 
                if (useSnapLines) {
                    components.Add(resizeComponents[i].resizeControl); 
                } 
                if (designerHost != null) {
                    ControlDesigner designer = designerHost.GetDesigner(resizeComponents[i].resizeControl as Component) as ControlDesigner; 
                    if (designer != null) {
                        resizeComponents[i].resizeRules = designer.SelectionRules;
                    }
                    else { 
                        Debug.Fail("Initiating resize. Could not get the designer for " + resizeComponents[i].resizeControl.ToString());
                        resizeComponents[i].resizeRules = SelectionRules.None; 
                    } 
                }
            } 

            //disable all glyphs in all adorners
            foreach (Adorner a in BehaviorService.Adorners) {
                a.Enabled = false; 
            }
 
            //build up our resize transaction 
            IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
            if (host != null) { 
                string locString;
                if (resizeComponents.Length == 1) {
                    string name = TypeDescriptor.GetComponentName(resizeComponents[0].resizeControl);
                    if (name == null || name.Length == 0) { 
                        name = resizeComponents[0].resizeControl.GetType().Name;
                    } 
                    locString = SR.GetString(SR.BehaviorServiceResizeControl, name); 
                }
                else { 
                    locString = SR.GetString(SR.BehaviorServiceResizeControls, resizeComponents.Length);

                }
                resizeTransaction = host.CreateTransaction(locString); 
            }
 
            initialResize = true; 

            if (useSnapLines) { 
                //instantiate our class to manage snap/margin lines...
                dragManager = new DragAssistanceManager(serviceProvider, components, true);
            }
            else if (resizeComponents.Length > 0) { 
                //try to get the parents grid and snap settings
                Control control = resizeComponents[0].resizeControl as Control; 
                if (control != null && control.Parent != null) { 
                    PropertyDescriptor snapProp = TypeDescriptor.GetProperties(control.Parent)["SnapToGrid"];
                    if (snapProp != null && (bool)snapProp.GetValue(control.Parent)) { 
                        PropertyDescriptor gridProp = TypeDescriptor.GetProperties(control.Parent)["GridSize"];
                        if (gridProp != null) {
                            //cache of the gridsize and the location of the parent on the adornerwindow
                            this.parentGridSize = (Size)gridProp.GetValue(control.Parent); 
                            this.parentLocation = behaviorService.ControlToAdornerWindow(control);
                            this.parentLocation.X -= control.Location.X; 
                            this.parentLocation.Y -= control.Location.Y; 
                        }
                    } 
                }
            }

            graphics = BehaviorService.AdornerWindowGraphics; 

        } 
 
        /// <include file='doc\SelectionBehavior.uex' path='docs/doc[@for="SelectionBehavior.OnMouseDown"]/*' />
        /// <devdoc> 
        ///     In response to a MouseDown, the SelectionBehavior will push
        ///     (initiate) a dragBehavior by alerting the SelectionMananger
        ///     that a new control has been selected and the mouse is down.
        ///     Note that this is only if we find the related control's Dock 
        ///     property == none.
        /// </devdoc> 
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) { 

            //we only care about the right mouse button for resizing 
            if (button != MouseButtons.Left) {
                //pass any other mouse click along - unless we've already started our resize
                //in which case we'll ignore it
                return pushedBehavior; 
            }
 
            //start with no selection rules and try to obtain this info from the glyph 
            targetResizeRules = SelectionRules.None;
 
            SelectionGlyphBase sgb = g as SelectionGlyphBase;

            if (sgb != null) {
                targetResizeRules = sgb.SelectionRules; 
                cursor = sgb.HitTestCursor;
            } 
 
            if (targetResizeRules == SelectionRules.None) {
                return false; 
            }

            ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
 
            if (selSvc == null) {
                return false; 
            } 

            initialPoint = mouseLoc; 
            lastMouseLoc = mouseLoc;

            //build up a list of our selected controls
            // 
            primaryControl = selSvc.PrimarySelection as Control;
 
            // Since we don't know exactly how many valid objects we are going to have 
            // we use this temp
            ArrayList components = new ArrayList(); 

            foreach (object o in selSvc.GetSelectedComponents()) {
                if (o is Control) {
 
                    //don't drag locked controls
                    PropertyDescriptor prop = TypeDescriptor.GetProperties(o)["Locked"]; 
                    if (prop != null) { 
                        if ((bool)prop.GetValue(o))
                            continue; 
                    }

                    components.Add(o);
                } 
            }
 
            if (components.Count == 0) { 
                return false;
            } 

            resizeComponents = new ResizeComponent[components.Count];
            for (int i = 0; i < components.Count; i++) {
                resizeComponents[i].resizeControl = components[i]; 
            }
 
            //push this resizebehavior 
            pushedBehavior = true;
            BehaviorService.PushCaptureBehavior(this); 

            return false;
        }
 
        /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="ResizeBehavior.OnLoseCapture"]/*' />
        /// <devdoc> 
        ///     This method is called when we lose capture, which can occur when another window 
        ///     requests capture or the user presses ESC during a drag.  We check to see if
        ///     we are currently dragging, and if we are we abort the transaction.  We pop 
        ///     our behavior off the stack at this time.
        /// </devdoc>
        public override void OnLoseCapture(Glyph g, EventArgs e) {
 
            if (pushedBehavior) {
                pushedBehavior = false; 
                Debug.Assert(BehaviorService != null, "We should have a behavior service."); 

                if (BehaviorService != null) { 
                    if (dragging) {
                        dragging = false;

                        //make sure we get rid of the selection rectangle 
                        for (int i = 0; graphics != null && i < resizeComponents.Length; i++) {
                            Control control = resizeComponents[i].resizeControl as Control; 
                            Rectangle borderRect = BehaviorService.ControlRectInAdornerWindow(control); 
                            if (!borderRect.IsEmpty) {
                                graphics.SetClip(borderRect); 
                                using (Region newRegion = new Region(borderRect)) {
                                    newRegion.Exclude(Rectangle.Inflate(borderRect, -borderSize, -borderSize));
                                    BehaviorService.Invalidate(newRegion);
                                } 
                                graphics.ResetClip();
                            } 
                        } 

                        //re-enable all glyphs in all adorners 
                        foreach (Adorner a in BehaviorService.Adorners) {
                            a.Enabled = true;
                        }
                    } 
                    BehaviorService.PopBehavior(this);
 
                    if (lastResizeRegion != null) { 
                        BehaviorService.Invalidate(lastResizeRegion);//might be the same, might not.
                        lastResizeRegion.Dispose(); 
                        lastResizeRegion = null;
                    }
                }
 

 
                if (graphics != null) { 
                    graphics.Dispose();
                    graphics = null; 
                }
            }

            Debug.Assert(!dragging, "How can we be dragging without pushing a behavior?"); 

            // If we still have a transaction, roll it back. 
            // 
            if (resizeTransaction != null) {
                DesignerTransaction t = resizeTransaction; 
                resizeTransaction = null;
                using (t) {
                    t.Cancel();
                } 
            }
 
        } 

        /// <devdoc> 
        ///     VSWhidbey #226999 and #429306
        /// </devdoc>
 	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        internal static int AdjustPixelsForIntegralHeight(Control control, int pixelsMoved) { 
            PropertyDescriptor propIntegralHeight = TypeDescriptor.GetProperties(control)["IntegralHeight"];
            if (propIntegralHeight != null) { 
                Object value = propIntegralHeight.GetValue(control); 
                if (value is bool && (bool)value == true) {
                    PropertyDescriptor propItemHeight = TypeDescriptor.GetProperties(control)["ItemHeight"]; 
                    if (propItemHeight != null) {
                        if (pixelsMoved >= 0) {
                            return pixelsMoved - (pixelsMoved % (int)propItemHeight.GetValue(control));
                        } 
                        else {
                            int integralHeight = (int)propItemHeight.GetValue(control); 
                            return pixelsMoved - (integralHeight - (Math.Abs(pixelsMoved) % integralHeight)); 
                        }
                    } 
                }
            }

            //if the control does not have the IntegralHeight property, then the pixels moved are fine 
            return pixelsMoved;
        } 
 
        /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="ResizeBehavior.OnMouseMove"]/*' />
        /// <devdoc> 
        ///     This method will either initiate a new resize operation or continue with
        ///     an existing one.  If we're currently dragging (i.e. resizing) then we
        ///     look at the resize rules and set the bounds of each control to the new
        ///     location of the mouse pointer. 
        /// </devdoc>
        public override bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc) { 
 
            if (!pushedBehavior) {
                return false; 
            }

            bool altKeyPressed = Control.ModifierKeys == Keys.Alt;
 
            if (altKeyPressed && dragManager != null) {
                //erase any snaplines (if we had any) 
                dragManager.EraseSnapLines(); 
            }
 
            if (!altKeyPressed && mouseLoc.Equals(lastMouseLoc)) {
                return true;
            }
 
            // VSWhidbey #213385
            // When DesignerWindowPane has scrollbars and we resize, shrinking the the DesignerWindowPane 
            // makes it look like the mouse has moved to the BS.  To compensate for that we keep track of the 
            // mouse's previous position in screen coordinates, and use that to compare if the mouse has really moved.
            if (lastMouseAbs != null) { 
                NativeMethods.POINT mouseLocAbs = new NativeMethods.POINT(mouseLoc.X, mouseLoc.Y);
                UnsafeNativeMethods.ClientToScreen(new HandleRef(this, behaviorService.AdornerWindowControl.Handle), mouseLocAbs);
                if (mouseLocAbs.x == lastMouseAbs.x && mouseLocAbs.y == lastMouseAbs.y) {
                    return true; 
                }
            } 
 

            if (!dragging) { 
                if (Math.Abs(initialPoint.X - mouseLoc.X) > DesignerUtils.MinDragSize.Width/2 || Math.Abs(initialPoint.Y - mouseLoc.Y) > DesignerUtils.MinDragSize.Height/2) {
                    InitiateResize();
                    dragging = true;
                } 
                else {
                    return false; 
                } 
            }
 
            if (resizeComponents == null || resizeComponents.Length == 0) {
                return false;
            }
 

 
            // we do these separately so as not to disturb the cached sizes for values we're not actually 
            // changing.  For example, if a control is docked top and we modify the height, the width shouldn't
            // be modified. 
            //
            PropertyDescriptor propWidth = null;
            PropertyDescriptor propHeight = null;
            PropertyDescriptor propTop = null; 
            PropertyDescriptor propLeft = null;
 
            // We do this to make sure that Undo works correctly. 
            if (initialResize) {
                propWidth = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Width"]; 
                propHeight = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Height"];
                propTop = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Top"];
                propLeft = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Left"];
 
                // validate each of the property descriptors.
                // 
                if (propWidth != null && !typeof(int).IsAssignableFrom(propWidth.PropertyType)) { 
                    propWidth = null;
                } 

                if (propHeight != null && !typeof(int).IsAssignableFrom(propHeight.PropertyType)) {
                    propHeight = null;
                } 

                if (propTop != null && !typeof(int).IsAssignableFrom(propTop.PropertyType)) { 
                    propTop = null; 
                }
 
                if (propLeft != null && !typeof(int).IsAssignableFrom(propLeft.PropertyType)) {
                    propLeft = null;
                }
            } 

 
            Control targetControl = resizeComponents[0].resizeControl as Control; 

            lastMouseLoc = mouseLoc; 
            lastMouseAbs = new NativeMethods.POINT(mouseLoc.X, mouseLoc.Y);
            UnsafeNativeMethods.ClientToScreen(new HandleRef(this, behaviorService.AdornerWindowControl.Handle), lastMouseAbs);

            int minHeight = Math.Max(targetControl.MinimumSize.Height, MINSIZE); 
            int minWidth = Math.Max(targetControl.MinimumSize.Width, MINSIZE);
 
            if (dragManager != null) { 

                bool shouldSnap = true; 
                bool shouldSnapHorizontally = true;
                //if the targetcontrol is at min-size then we do not want to offer up snaplines
                if ((((targetResizeRules & SelectionRules.BottomSizeable) != 0) || ((targetResizeRules & SelectionRules.TopSizeable) != 0)) &&
                    (targetControl.Height == minHeight)) { 
                    shouldSnap = false;
                } 
                else if ((((targetResizeRules & SelectionRules.RightSizeable) != 0) || ((targetResizeRules & SelectionRules.LeftSizeable) != 0)) && 
                    (targetControl.Width == minWidth)) {
                    shouldSnap = false; 
                }

                //if the targetControl has IntegralHeight turned on, then don't snap if the control can be resized vertically
                PropertyDescriptor propIntegralHeight = TypeDescriptor.GetProperties(targetControl)["IntegralHeight"]; 
                if (propIntegralHeight != null) {
                    Object value = propIntegralHeight.GetValue(targetControl); 
                    if (value is bool && (bool)value == true) { 
                        shouldSnapHorizontally = false;
                    } 
                }


                if (!altKeyPressed && shouldSnap) { 
                    //here, ask the snapline engine to suggest an offset during our resize
 
                    // Remembering the last snapoffset allows us to correctly erase snaplines, 
                    // if the user subsequently holds down the Alt-Key. Remember that we don't physically move the mouse,
                    // we move the control. So if we didn't remember the last snapoffset 
                    // and the user then hit the Alt-Key, we would actually redraw the control at the actual mouse location,
                    // which would make the control "jump" which is not what the user would expect. Why does the control "jump"?
                    // Because when a control is snapped, we have offset the control relative to where the mouse is, but we
                    // have not update the physical mouse position. 
                    // When the user hits the Alt-Key they expect the control to be where it was (whether snapped or not).
 
                    // we can't rely on lastSnapOffset to check whether we snapped. We used to check if it was empty, 
                    // but it can be empty and we still snapped (say the control was snapped, as you continue to move the
                    // mouse, it will stay snapped for a while. During that while the snapoffset will got from x to -x (or vice versa) 
                    // and a one point hit 0.
                    //
                    // Since we have to calculate the new size/location differently based on whether we snapped or not,
                    // we have to know for sure if we snapped. 
                    //
                    // We do different math because of bug 264996: 
                    //  - if you snap, we want to move the control edge. 
                    //  - otherwise, we just want to change the size by the number of pixels moved.
                    // 
                    // See VSWhidbey #340708

                    lastSnapOffset  = dragManager.OnMouseMove(targetControl, GenerateSnapLines(targetResizeRules, mouseLoc), ref didSnap, shouldSnapHorizontally);
                } 
                else {
                    dragManager.OnMouseMove(new Rectangle(-100,-100,0,0));/*just an invalid rect - so we won't snap*///); 
                } 

                // If there's a line to snap to, the offset will come back non-zero. In that case we should adjust the 
                // mouse position with the offset such that the size calculation below takes that offset into account.

                // If there's no line, then the offset is 0, and there's no harm in adding the offset.
                mouseLoc.X += lastSnapOffset.X; 
                mouseLoc.Y += lastSnapOffset.Y;
            } 
 

            // IF WE ARE SNAPPING TO A CONTROL, then we also 
            // need to adjust for the offset between the initialPoint (where the MouseDown happened)
            // and the edge of the control otherwise we would be those pixels off when resizing the control.
            //
            // Remember that snaplines are based on the targetControl, so we need to use 
            // the targetControl to figure out the offset.
            // 
            // VSWhidbey 264996. 
            Rectangle controlBounds = new Rectangle(resizeComponents[0].resizeBounds.X, resizeComponents[0].resizeBounds.Y,
                                                      resizeComponents[0].resizeBounds.Width, resizeComponents[0].resizeBounds.Height); 
            if ((didSnap) && (targetControl.Parent != null)) {
                controlBounds.Location = behaviorService.MapAdornerWindowPoint(targetControl.Parent.Handle, controlBounds.Location);
                if (targetControl.Parent.IsMirrored) {
                    controlBounds.Offset(-controlBounds.Width,0); 
                }
            } 
 
            Rectangle newBorderRect = Rectangle.Empty;
            Rectangle targetBorderRect = Rectangle.Empty; 
            bool drawSnapline = true;
            Color backColor = targetControl.Parent != null ? targetControl.Parent.BackColor : Color.Empty;

            for (int i = 0; i < resizeComponents.Length; i++) { 
                Control control = resizeComponents[i].resizeControl as Control;
                Rectangle bounds = control.Bounds; 
                Rectangle oldBounds = bounds; 
                // We need to compute the offset beased on the original cached Bounds ...
                // ListBox doesnt allow drag on the top boundary if this is not done 
                // when it is "IntegralHeight"
                Rectangle baseBounds = resizeComponents[i].resizeBounds;

                Rectangle oldBorderRect = BehaviorService.ControlRectInAdornerWindow(control); 
                bool needToUpdate = true;
 
                // The ResizeBehavior can easily get into a situation where we are fighting with a layout engine. 
                // E.g., We resize control to 50px, LayoutEngine lays out and finds 50px was too small and
                // resized back to 100px.  This is what should happen, but it looks bad in the designer.  To avoid 
                // the flicker we temporarily turn off painting while we do the resize.
                //
                UnsafeNativeMethods.SendMessage(control.Handle, NativeMethods.WM_SETREDRAW, false, /* unused = */ 0);
                try { 

 
                    bool fRTL = false; 
                    // If the container is mirrored the control origin is in upper-right, so we need to
                    // adjust our math for that. Remember that mouse coords have origin in upper left. 
                    if (control.Parent != null && control.Parent.IsMirrored) {
                        fRTL = true;
                    }
 

                    // figure out which ones we're actually changing so we don't blow away the 
                    // controls cached sizing state.  This is important if things are docked we don't 
                    // want to destroy their "pre-dock" size.
 
                    BoundsSpecified specified = BoundsSpecified.None;

                    // When we check if we should change height, width, location,
                    // we first have to check if the targetControl allows resizing, 
                    // and then if the control we are currently resizing allows it as well.
                    // VSWhidbey #396519 
 
                    SelectionRules resizeRules = resizeComponents[i].resizeRules;
 
                    if (((targetResizeRules & SelectionRules.BottomSizeable) != 0) &&
                        ((resizeRules & SelectionRules.BottomSizeable) != 0)) {
                        int pixelHeight;
                        if (didSnap) { 
                            pixelHeight = mouseLoc.Y - controlBounds.Bottom;
                        } 
                        else { 
                            pixelHeight = AdjustPixelsForIntegralHeight(control, mouseLoc.Y - initialPoint.Y);
                        } 

                        bounds.Height = Math.Max(minHeight, baseBounds.Height + pixelHeight);
                        specified |= BoundsSpecified.Height;
                    } 

                    if (((targetResizeRules & SelectionRules.TopSizeable) != 0) && 
                        ((resizeRules & SelectionRules.TopSizeable) != 0)) { 
                        int yOffset;
                        if (didSnap) { 
                            yOffset = controlBounds.Y- mouseLoc.Y;
                        }
                        else {
                            yOffset = AdjustPixelsForIntegralHeight(control, initialPoint.Y - mouseLoc.Y); 
                        }
 
                        specified |= BoundsSpecified.Height; 
                        bounds.Height = Math.Max(minHeight, baseBounds.Height + yOffset);
                        if ((bounds.Height != minHeight) || 
                             ((bounds.Height == minHeight) && (oldBounds.Height != minHeight))) { //VSWhidbey 179862
                            specified |= BoundsSpecified.Y;
                            //VSWhidbey 179862 - if you do it fast enough, we actually could end up placing the control
                            //off the parent (say off the form), so enforce a "minimum" location 
                            bounds.Y = Math.Min(baseBounds.Bottom - minHeight, baseBounds.Y - yOffset);
                        } 
 
                    }
 
                    if (((((targetResizeRules & SelectionRules.RightSizeable) != 0) &&
                        ((resizeRules & SelectionRules.RightSizeable) != 0)) && (!fRTL)) ||
                       ((((targetResizeRules & SelectionRules.LeftSizeable) != 0) &&
                        ((resizeRules & SelectionRules.LeftSizeable) != 0)) && (fRTL))) { 

                        specified |= BoundsSpecified.Width; 
                        int xOffset = initialPoint.X; 
                        if (didSnap) {
                            xOffset = !fRTL ? controlBounds.Right : controlBounds.Left; 
                        }

                        bounds.Width = Math.Max(minWidth, baseBounds.Width + (!fRTL ? (mouseLoc.X - xOffset) :
                                                                                        (xOffset - mouseLoc.X))); 
                    }
 
                    if (((((targetResizeRules & SelectionRules.RightSizeable) != 0) && 
                        ((resizeRules & SelectionRules.RightSizeable) != 0)) && (fRTL)) ||
                       ((((targetResizeRules & SelectionRules.LeftSizeable) != 0) && 
                        ((resizeRules & SelectionRules.LeftSizeable) != 0)) && (!fRTL))) {
                        specified |= BoundsSpecified.Width;
                        int xPos = initialPoint.X;
                        if (didSnap) { 
                            xPos = !fRTL ? controlBounds.Left : controlBounds.Right;
                        } 
 
                        int xOffset = !fRTL ? (xPos - mouseLoc.X) : (mouseLoc.X - xPos);
                        bounds.Width = Math.Max(minWidth, baseBounds.Width + xOffset); 
                        if ((bounds.Width != minWidth) ||
                             ((bounds.Width == minWidth) && (oldBounds.Width != minWidth))) { //VSWhidbey 179862
                            specified |= BoundsSpecified.X;
                            //VSWhidbey 179862 - if you do it fast enough, we actually could end up placing the control 
                            //off the parent (say off the form), so enforce a "minimum" location
                            bounds.X = Math.Min(baseBounds.Right - minWidth, baseBounds.X - xOffset); 
                        } 
                    }
 

                    if (!parentGridSize.IsEmpty) {
                        bounds = AdjustToGrid(bounds, targetResizeRules);
                    } 

 
                    // Checking specified (check the diff) rather than bounds.<foo> != resizeBounds[i].<foo> 
                    // also handles the following corner cases:
                    // 
                    // 1. Create a form and add 2 buttons. Make sure that they are snapped to the left edge.
                    // Now grab the left edge of button 1, and start resizing to the left, past the
                    // snapline you will initially get, and then back to the right. What you would expect
                    // is to get the left edge snapline again. But without the specified check you wouldn't. 
                    // This is because the bounds.<foo> != resizeBounds[i].<foo> checks would fail, since the
                    // new size would now be the original size. We could probably live with that, except that 
                    // we draw the snapline below, since we correctly identified one. We could hack it so that 
                    // we didn't draw the snapline, but that would confuse the user even more.
                    // 
                    // 2. Create a form and add a single button. Place it at 100,100. Now start resizing it
                    // to the left and then back to the right. Note that with the original check (see diff),
                    // you would never be able to resize it back to position 100,100. You would get to 99,100
                    // and then to 101,100. 
                    //
                    if (((specified & BoundsSpecified.Width) == BoundsSpecified.Width) && 
                        dragging && initialResize && propWidth != null) { 
                            propWidth.SetValue(resizeComponents[i].resizeControl, bounds.Width);
                    } 

                    if (((specified & BoundsSpecified.Height) == BoundsSpecified.Height) &&
                        dragging && initialResize && propHeight != null) {
                            propHeight.SetValue(resizeComponents[i].resizeControl, bounds.Height); 
                        }
 
                    if (((specified & BoundsSpecified.X) == BoundsSpecified.X) && 
                        dragging && initialResize && propLeft != null) {
                            propLeft.SetValue(resizeComponents[i].resizeControl, bounds.X); 
                        }

                    if (((specified & BoundsSpecified.Y) == BoundsSpecified.Y) &&
                        dragging && initialResize && propTop != null) { 
                            propTop.SetValue(resizeComponents[i].resizeControl, bounds.Y);
                        } 
 
                    // We check the dragging bit here at every turn, because if there was a popup
                    // we may have lost capture and we are terminated.  At that point we shouldn't 
                    // make any changes.
                    //

                    if (dragging) { 

                        control.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height, specified); 
 
                        //Get the new resize border
                        newBorderRect = BehaviorService.ControlRectInAdornerWindow(control); 
                        if (control.Equals(targetControl)) {
                            Debug.Assert(i == 0, "The first control in the Selection should be the target control");
                            targetBorderRect = newBorderRect;
                        } 

                        //Check that the control really did resize itself. Some controls (like ListBox, MonthCalendar) 
                        //might adjust to a slightly different size than the one we pass in SetBounds. If if didn't 
                        //size, then there's no need to invalidate anything
                        if (control.Bounds == oldBounds) { 
                            needToUpdate = false;
                        }

                        // We would expect the bounds now to be what we set it to above, but this might not be the case. 
                        // If the control is hosted with e.g. a FLP, then setting the bounds above actually might force
                        // a re-layout, and the control will get moved to another spot. In this case, we don't really 
                        // want to draw a snapline. Even if we snapped to a snapline, if the control got moved, the snapline 
                        // would be in the wrong place. VSWhidbey #497636
                        if (control.Bounds != bounds) { 
                            drawSnapline = false;
                        }

                    } 

                    if (control == primaryControl && statusCommandUI != null) { 
                        statusCommandUI.SetStatusInformation(control as Component); 
                    }
                } 
                finally {
                    // While we were resizing we discarded painting messages to reduce flicker.  We now
                    // turn painting back on and manually refresh the controls.
                    // 
                    UnsafeNativeMethods.SendMessage(control.Handle, NativeMethods.WM_SETREDRAW, true, /* unused = */ 0);
 
                    //update the control 
                    if (needToUpdate) {
                        Control parent = control.Parent; 
                        if (parent != null) {
                            control.Invalidate(/* invalidateChildren = */ true);
                            parent.Invalidate(oldBounds, /* invalidateChildren = */ true);
                            parent.Update(); 
                        }
                        else { 
                            control.Refresh(); 
                        }
                    } 

                    //render the resize border
                    if (!newBorderRect.IsEmpty) {
                        using (Region newRegion = new Region(newBorderRect)) { 
                            newRegion.Exclude(Rectangle.Inflate(newBorderRect, -borderSize, -borderSize));
 
                            //No reason to get smart about only invalidating part of the border. Thought we could be but no. 
                            //The reason is the order:
                            //  ... the new border is drawn (last resize) 
                            //  On next mousemove, the control is resized which redraws the control AND ERASES THE BORDER
                            //  Then we draw the new border - flash baby.
                            // Thus this will always flicker.
                            if (needToUpdate) { 
                                using (Region oldRegion = new Region(oldBorderRect)) {
                                    oldRegion.Exclude(Rectangle.Inflate(oldBorderRect, -borderSize, -borderSize)); 
                                    BehaviorService.Invalidate(oldRegion); 
                                }
                            } 

                            //draw the new border
                            //graphics could be null if a popup came up and caused a lose focus
                            if (graphics != null) { 
                                if (lastResizeRegion != null) {
                                    if (!lastResizeRegion.Equals(newRegion, graphics)) { 
                                        lastResizeRegion.Exclude(newRegion); //we don't want to invalidate this region. 
                                        BehaviorService.Invalidate(lastResizeRegion);//might be the same, might not.
                                        lastResizeRegion.Dispose(); 
                                        lastResizeRegion = null;
                                    }
                                }
                                DesignerUtils.DrawResizeBorder(graphics, newRegion, backColor); 
                                if (lastResizeRegion == null) {
                                    lastResizeRegion = newRegion.Clone(); //we will need to dispose it later. 
                                } 
                            }
                        } 
                    }

                }
            } 

            if ((drawSnapline) && (!altKeyPressed) && (dragManager != null)) { 
                dragManager.RenderSnapLinesInternal(targetBorderRect); 
            }
 
            initialResize = false;
            return true;
        }
 
        /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="ResizeBehavior.OnMouseUp"]/*' />
        /// <devdoc> 
        ///     This ends the Behavior by popping itself from the BehaviorStack.  Also, 
        ///     all Adorners are re-enabled at the end of a successful drag.
        /// </devdoc> 
        public override bool OnMouseUp(Glyph g, MouseButtons button) {
            try {
                if (dragging) {
 
                    if (dragManager != null) {
                        dragManager.OnMouseUp(); 
                        dragManager = null; 
                        lastSnapOffset = Point.Empty;
                        didSnap = false; 
                    }


                    if (resizeComponents != null && resizeComponents.Length > 0) { 

                        // we do these separately so as not to disturb the cached sizes for values we're not actually 
                        // changing.  For example, if a control is docked top and we modify the height, the width shouldn't 
                        // be modified.
                        // 
                        PropertyDescriptor propWidth = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Width"];
                        PropertyDescriptor propHeight = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Height"];
                        PropertyDescriptor propTop = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Top"];;
                        PropertyDescriptor propLeft = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Left"];; 

                        for (int i = 0; i < resizeComponents.Length; i++) { 
                            if (propWidth != null && ((Control)resizeComponents[i].resizeControl).Width != resizeComponents[i].resizeBounds.Width) { 
                                propWidth.SetValue(resizeComponents[i].resizeControl, ((Control)resizeComponents[i].resizeControl).Width);
                            } 
                            if (propHeight != null && ((Control)resizeComponents[i].resizeControl).Height != resizeComponents[i].resizeBounds.Height) {
                                propHeight.SetValue(resizeComponents[i].resizeControl, ((Control)resizeComponents[i].resizeControl).Height);
                            }
 
                            if (propTop != null && ((Control)resizeComponents[i].resizeControl).Top != resizeComponents[i].resizeBounds.Y) {
                                propTop.SetValue(resizeComponents[i].resizeControl, ((Control)resizeComponents[i].resizeControl).Top); 
                            } 
                            if (propLeft != null && ((Control)resizeComponents[i].resizeControl).Left != resizeComponents[i].resizeBounds.X) {
                                propLeft.SetValue(resizeComponents[i].resizeControl, ((Control)resizeComponents[i].resizeControl).Left); 
                            }

                            if (resizeComponents[i].resizeControl == primaryControl && statusCommandUI != null) {
                                statusCommandUI.SetStatusInformation(primaryControl as Component); 
                            }
                        } 
                    } 
                }
 
                if (resizeTransaction != null) {
                    DesignerTransaction t = resizeTransaction;
                    resizeTransaction = null;
                    using (t) { 
                        t.Commit();
                    } 
                } 
            }
            finally { 
                // This pops us off the stack, re-enables adorners and clears the "dragging" flag.
                OnLoseCapture(g, EventArgs.Empty);
            }
 
            return false;
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
    using System.Windows.Forms.Design; 
    using System.Runtime.InteropServices;
    using System.Drawing.Drawing2D;

    /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="ResizeBehavior"]/*' /> 
    /// <devdoc>
    ///     The ResizeBehavior is pushed onto the BehaviorStack in response to a 
    ///     positively hit tested SelectionGlyph.  The ResizeBehavior simply 
    ///     tracks the MouseMove messages and updates the bounds of the relatd
    ///     control based on the new mouse location and the resize Rules. 
    /// </devdoc>
    internal class ResizeBehavior : Behavior {

        private struct ResizeComponent { 
            public object resizeControl;
            public Rectangle resizeBounds; 
            public SelectionRules resizeRules; 
        };
 
        private ResizeComponent[]       resizeComponents;
        private IServiceProvider        serviceProvider;
        private BehaviorService         behaviorService;
        private SelectionRules          targetResizeRules;//rules dictating which sizes we can change 
        private Point                   initialPoint;//the initial point of the mouse down
        private bool                    dragging;//indicates that the behavior is currently 'dragging' 
        private bool                    pushedBehavior; 
        private bool                    initialResize;//true for the first resize of the control, false after that.
        private DesignerTransaction     resizeTransaction;//the transaction we create for the resize 
        private const int               MINSIZE = 10;
        private const int               borderSize = 2;
        private DragAssistanceManager   dragManager;//this object will integrate SnapLines into the resize
        private Point                   lastMouseLoc;//helps us avoid re-entering code if the mouse hasn't moved 
        private Point                   parentLocation;//used to snap resize ops to the grid
        private Size                    parentGridSize;//used to snap resize ops to the grid 
        private NativeMethods.POINT     lastMouseAbs; // last absolute mouse position 
        private Point                   lastSnapOffset;//the last snapoffset we used.
        private bool                    didSnap; //did we actually snap. 
        private Control                 primaryControl;//the primary control the status bar will queue off of

        private Cursor                  cursor = Cursors.Default; //used to set the correct cursor during resizing
        private StatusCommandUI         statusCommandUI; // used to update the StatusBar Information. 

        private Graphics                graphics;//graphics object of the adornerwindow (via BehaviorService) 
        private Region                  lastResizeRegion; 

        /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="ResizeBehavior.ResizeBehavior"]/*' /> 
        /// <devdoc>
        ///     Constructor that caches all values for perf. reasons.
        /// </devdoc>
        internal ResizeBehavior(IServiceProvider serviceProvider) { 
            this.serviceProvider = serviceProvider;
            dragging = false; 
            pushedBehavior = false; 
            lastSnapOffset = Point.Empty;
            didSnap = false; 
            statusCommandUI = new StatusCommandUI(serviceProvider);
        }

        /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="SelectionBehavior.BehaviorService"]/*' /> 
        /// <devdoc>
        ///     Demand creates the BehaviorService. 
        /// </devdoc> 
        private BehaviorService BehaviorService {
            get { 
                if (behaviorService == null) {
                    behaviorService = (BehaviorService)serviceProvider.GetService(typeof(BehaviorService));
                }
                return behaviorService; 
            }
        } 
 
        public override Cursor Cursor {
            get { 
                return cursor;
            }
        }
 
        /// <devdoc>
        ///     Called during the resize operation, we'll try to determine 
        ///     an offset so that the controls snap to the grid settings 
        ///     of the parent.
        /// </devdoc> 
        private Rectangle AdjustToGrid(Rectangle controlBounds, SelectionRules rules) {

            Rectangle rect = controlBounds;
 
            if ((rules & SelectionRules.RightSizeable) != 0) {
                int xDelta = controlBounds.Right % parentGridSize.Width; 
                if (xDelta > parentGridSize.Width /2) { 
                    rect.Width += parentGridSize.Width - xDelta;
                } 
                else {
                    rect.Width -= xDelta;
                }
            } 
            else if ((rules & SelectionRules.LeftSizeable) != 0) {
                int xDelta = controlBounds.Left % parentGridSize.Width; 
                if (xDelta > parentGridSize.Width /2) { 
                    rect.X += parentGridSize.Width - xDelta;
                    rect.Width -= parentGridSize.Width - xDelta; 
                }
                else {
                    rect.X-= xDelta;
                    rect.Width += xDelta; 
                }
            } 
 
            if ((rules & SelectionRules.BottomSizeable) != 0) {
                int yDelta = controlBounds.Bottom % parentGridSize.Height; 
                if (yDelta > parentGridSize.Height /2) {
                    rect.Height += parentGridSize.Height - yDelta;
                }
                else { 
                    rect.Height -= yDelta;
                } 
            } 
            else if ((rules & SelectionRules.TopSizeable) != 0) {
                int yDelta = controlBounds.Top % parentGridSize.Height; 
                if (yDelta > parentGridSize.Height /2) {
                    rect.Y += parentGridSize.Height - yDelta;
                    rect.Height -= parentGridSize.Height - yDelta;
                } 
                else {
                    rect.Y -= yDelta; 
                    rect.Height += yDelta; 
                }
            } 

            //validate our dimensions
            rect.Width = Math.Max(rect.Width, parentGridSize.Width);
            rect.Height = Math.Max(rect.Height, parentGridSize.Height); 

            return rect; 
        } 

        /// <include file='doc\SelectionBehavior.uex' path='docs/doc[@for="SelectionBehavior.GenerateSnapLines"]/*' /> 
        /// <devdoc>
        ///     Builds up an array of snaplines used during resize to adjust/snap
        ///     the controls bounds.
        /// </devdoc> 
        private SnapLine[] GenerateSnapLines(SelectionRules rules, Point loc) {
            ArrayList lines = new ArrayList(2); 
 
            //the four margins and edges of our control
            // 
            if ((rules & SelectionRules.BottomSizeable) != 0) {
                lines.Add(new SnapLine(SnapLineType.Bottom, loc.Y-1));
                if (primaryControl != null) {
                    lines.Add( new SnapLine(SnapLineType.Horizontal, loc.Y + primaryControl.Margin.Bottom, SnapLine.MarginBottom, SnapLinePriority.Always)); 
                }
            } 
            else if ((rules & SelectionRules.TopSizeable) != 0) { 
                lines.Add(new SnapLine(SnapLineType.Top, loc.Y));
                if (primaryControl != null) { 
                    lines.Add( new SnapLine(SnapLineType.Horizontal, loc.Y -primaryControl.Margin.Top, SnapLine.MarginTop, SnapLinePriority.Always));
                }
            }
 
            if ((rules & SelectionRules.RightSizeable) != 0) {
                lines.Add(new SnapLine(SnapLineType.Right, loc.X-1)); 
                if (primaryControl != null) { 
                    lines.Add( new SnapLine(SnapLineType.Vertical, loc.X + primaryControl.Margin.Right, SnapLine.MarginRight, SnapLinePriority.Always));
                } 
            }
            else if ((rules & SelectionRules.LeftSizeable) != 0) {
                lines.Add(new SnapLine(SnapLineType.Left, loc.X));
                if (primaryControl != null) { 
                    lines.Add( new SnapLine(SnapLineType.Vertical, loc.X -primaryControl.Margin.Left, SnapLine.MarginLeft, SnapLinePriority.Always));
                } 
            } 

            SnapLine[] l = new SnapLine[lines.Count]; 
            lines.CopyTo(l);

            return l;
        } 

        /// <devdoc> 
        ///     This is called in response to the mouse moving far enough 
        ///     away from its initial point.  Basically, we calculate the
        ///     bounds for each control we're resizing and disable any 
        ///     adorners.
        /// </devdoc>
        private void InitiateResize() {
 
            bool useSnapLines = BehaviorService.UseSnapLines;
            ArrayList components = new ArrayList(); 
 
            //check to see if the current designer participate with SnapLines
            IDesignerHost designerHost = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost; 

            //cache the control bounds
            for (int i = 0; i < resizeComponents.Length; i++) {
                resizeComponents[i].resizeBounds = ((Control)(resizeComponents[i].resizeControl)).Bounds; 
                if (useSnapLines) {
                    components.Add(resizeComponents[i].resizeControl); 
                } 
                if (designerHost != null) {
                    ControlDesigner designer = designerHost.GetDesigner(resizeComponents[i].resizeControl as Component) as ControlDesigner; 
                    if (designer != null) {
                        resizeComponents[i].resizeRules = designer.SelectionRules;
                    }
                    else { 
                        Debug.Fail("Initiating resize. Could not get the designer for " + resizeComponents[i].resizeControl.ToString());
                        resizeComponents[i].resizeRules = SelectionRules.None; 
                    } 
                }
            } 

            //disable all glyphs in all adorners
            foreach (Adorner a in BehaviorService.Adorners) {
                a.Enabled = false; 
            }
 
            //build up our resize transaction 
            IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
            if (host != null) { 
                string locString;
                if (resizeComponents.Length == 1) {
                    string name = TypeDescriptor.GetComponentName(resizeComponents[0].resizeControl);
                    if (name == null || name.Length == 0) { 
                        name = resizeComponents[0].resizeControl.GetType().Name;
                    } 
                    locString = SR.GetString(SR.BehaviorServiceResizeControl, name); 
                }
                else { 
                    locString = SR.GetString(SR.BehaviorServiceResizeControls, resizeComponents.Length);

                }
                resizeTransaction = host.CreateTransaction(locString); 
            }
 
            initialResize = true; 

            if (useSnapLines) { 
                //instantiate our class to manage snap/margin lines...
                dragManager = new DragAssistanceManager(serviceProvider, components, true);
            }
            else if (resizeComponents.Length > 0) { 
                //try to get the parents grid and snap settings
                Control control = resizeComponents[0].resizeControl as Control; 
                if (control != null && control.Parent != null) { 
                    PropertyDescriptor snapProp = TypeDescriptor.GetProperties(control.Parent)["SnapToGrid"];
                    if (snapProp != null && (bool)snapProp.GetValue(control.Parent)) { 
                        PropertyDescriptor gridProp = TypeDescriptor.GetProperties(control.Parent)["GridSize"];
                        if (gridProp != null) {
                            //cache of the gridsize and the location of the parent on the adornerwindow
                            this.parentGridSize = (Size)gridProp.GetValue(control.Parent); 
                            this.parentLocation = behaviorService.ControlToAdornerWindow(control);
                            this.parentLocation.X -= control.Location.X; 
                            this.parentLocation.Y -= control.Location.Y; 
                        }
                    } 
                }
            }

            graphics = BehaviorService.AdornerWindowGraphics; 

        } 
 
        /// <include file='doc\SelectionBehavior.uex' path='docs/doc[@for="SelectionBehavior.OnMouseDown"]/*' />
        /// <devdoc> 
        ///     In response to a MouseDown, the SelectionBehavior will push
        ///     (initiate) a dragBehavior by alerting the SelectionMananger
        ///     that a new control has been selected and the mouse is down.
        ///     Note that this is only if we find the related control's Dock 
        ///     property == none.
        /// </devdoc> 
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) { 

            //we only care about the right mouse button for resizing 
            if (button != MouseButtons.Left) {
                //pass any other mouse click along - unless we've already started our resize
                //in which case we'll ignore it
                return pushedBehavior; 
            }
 
            //start with no selection rules and try to obtain this info from the glyph 
            targetResizeRules = SelectionRules.None;
 
            SelectionGlyphBase sgb = g as SelectionGlyphBase;

            if (sgb != null) {
                targetResizeRules = sgb.SelectionRules; 
                cursor = sgb.HitTestCursor;
            } 
 
            if (targetResizeRules == SelectionRules.None) {
                return false; 
            }

            ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
 
            if (selSvc == null) {
                return false; 
            } 

            initialPoint = mouseLoc; 
            lastMouseLoc = mouseLoc;

            //build up a list of our selected controls
            // 
            primaryControl = selSvc.PrimarySelection as Control;
 
            // Since we don't know exactly how many valid objects we are going to have 
            // we use this temp
            ArrayList components = new ArrayList(); 

            foreach (object o in selSvc.GetSelectedComponents()) {
                if (o is Control) {
 
                    //don't drag locked controls
                    PropertyDescriptor prop = TypeDescriptor.GetProperties(o)["Locked"]; 
                    if (prop != null) { 
                        if ((bool)prop.GetValue(o))
                            continue; 
                    }

                    components.Add(o);
                } 
            }
 
            if (components.Count == 0) { 
                return false;
            } 

            resizeComponents = new ResizeComponent[components.Count];
            for (int i = 0; i < components.Count; i++) {
                resizeComponents[i].resizeControl = components[i]; 
            }
 
            //push this resizebehavior 
            pushedBehavior = true;
            BehaviorService.PushCaptureBehavior(this); 

            return false;
        }
 
        /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="ResizeBehavior.OnLoseCapture"]/*' />
        /// <devdoc> 
        ///     This method is called when we lose capture, which can occur when another window 
        ///     requests capture or the user presses ESC during a drag.  We check to see if
        ///     we are currently dragging, and if we are we abort the transaction.  We pop 
        ///     our behavior off the stack at this time.
        /// </devdoc>
        public override void OnLoseCapture(Glyph g, EventArgs e) {
 
            if (pushedBehavior) {
                pushedBehavior = false; 
                Debug.Assert(BehaviorService != null, "We should have a behavior service."); 

                if (BehaviorService != null) { 
                    if (dragging) {
                        dragging = false;

                        //make sure we get rid of the selection rectangle 
                        for (int i = 0; graphics != null && i < resizeComponents.Length; i++) {
                            Control control = resizeComponents[i].resizeControl as Control; 
                            Rectangle borderRect = BehaviorService.ControlRectInAdornerWindow(control); 
                            if (!borderRect.IsEmpty) {
                                graphics.SetClip(borderRect); 
                                using (Region newRegion = new Region(borderRect)) {
                                    newRegion.Exclude(Rectangle.Inflate(borderRect, -borderSize, -borderSize));
                                    BehaviorService.Invalidate(newRegion);
                                } 
                                graphics.ResetClip();
                            } 
                        } 

                        //re-enable all glyphs in all adorners 
                        foreach (Adorner a in BehaviorService.Adorners) {
                            a.Enabled = true;
                        }
                    } 
                    BehaviorService.PopBehavior(this);
 
                    if (lastResizeRegion != null) { 
                        BehaviorService.Invalidate(lastResizeRegion);//might be the same, might not.
                        lastResizeRegion.Dispose(); 
                        lastResizeRegion = null;
                    }
                }
 

 
                if (graphics != null) { 
                    graphics.Dispose();
                    graphics = null; 
                }
            }

            Debug.Assert(!dragging, "How can we be dragging without pushing a behavior?"); 

            // If we still have a transaction, roll it back. 
            // 
            if (resizeTransaction != null) {
                DesignerTransaction t = resizeTransaction; 
                resizeTransaction = null;
                using (t) {
                    t.Cancel();
                } 
            }
 
        } 

        /// <devdoc> 
        ///     VSWhidbey #226999 and #429306
        /// </devdoc>
 	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        internal static int AdjustPixelsForIntegralHeight(Control control, int pixelsMoved) { 
            PropertyDescriptor propIntegralHeight = TypeDescriptor.GetProperties(control)["IntegralHeight"];
            if (propIntegralHeight != null) { 
                Object value = propIntegralHeight.GetValue(control); 
                if (value is bool && (bool)value == true) {
                    PropertyDescriptor propItemHeight = TypeDescriptor.GetProperties(control)["ItemHeight"]; 
                    if (propItemHeight != null) {
                        if (pixelsMoved >= 0) {
                            return pixelsMoved - (pixelsMoved % (int)propItemHeight.GetValue(control));
                        } 
                        else {
                            int integralHeight = (int)propItemHeight.GetValue(control); 
                            return pixelsMoved - (integralHeight - (Math.Abs(pixelsMoved) % integralHeight)); 
                        }
                    } 
                }
            }

            //if the control does not have the IntegralHeight property, then the pixels moved are fine 
            return pixelsMoved;
        } 
 
        /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="ResizeBehavior.OnMouseMove"]/*' />
        /// <devdoc> 
        ///     This method will either initiate a new resize operation or continue with
        ///     an existing one.  If we're currently dragging (i.e. resizing) then we
        ///     look at the resize rules and set the bounds of each control to the new
        ///     location of the mouse pointer. 
        /// </devdoc>
        public override bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc) { 
 
            if (!pushedBehavior) {
                return false; 
            }

            bool altKeyPressed = Control.ModifierKeys == Keys.Alt;
 
            if (altKeyPressed && dragManager != null) {
                //erase any snaplines (if we had any) 
                dragManager.EraseSnapLines(); 
            }
 
            if (!altKeyPressed && mouseLoc.Equals(lastMouseLoc)) {
                return true;
            }
 
            // VSWhidbey #213385
            // When DesignerWindowPane has scrollbars and we resize, shrinking the the DesignerWindowPane 
            // makes it look like the mouse has moved to the BS.  To compensate for that we keep track of the 
            // mouse's previous position in screen coordinates, and use that to compare if the mouse has really moved.
            if (lastMouseAbs != null) { 
                NativeMethods.POINT mouseLocAbs = new NativeMethods.POINT(mouseLoc.X, mouseLoc.Y);
                UnsafeNativeMethods.ClientToScreen(new HandleRef(this, behaviorService.AdornerWindowControl.Handle), mouseLocAbs);
                if (mouseLocAbs.x == lastMouseAbs.x && mouseLocAbs.y == lastMouseAbs.y) {
                    return true; 
                }
            } 
 

            if (!dragging) { 
                if (Math.Abs(initialPoint.X - mouseLoc.X) > DesignerUtils.MinDragSize.Width/2 || Math.Abs(initialPoint.Y - mouseLoc.Y) > DesignerUtils.MinDragSize.Height/2) {
                    InitiateResize();
                    dragging = true;
                } 
                else {
                    return false; 
                } 
            }
 
            if (resizeComponents == null || resizeComponents.Length == 0) {
                return false;
            }
 

 
            // we do these separately so as not to disturb the cached sizes for values we're not actually 
            // changing.  For example, if a control is docked top and we modify the height, the width shouldn't
            // be modified. 
            //
            PropertyDescriptor propWidth = null;
            PropertyDescriptor propHeight = null;
            PropertyDescriptor propTop = null; 
            PropertyDescriptor propLeft = null;
 
            // We do this to make sure that Undo works correctly. 
            if (initialResize) {
                propWidth = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Width"]; 
                propHeight = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Height"];
                propTop = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Top"];
                propLeft = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Left"];
 
                // validate each of the property descriptors.
                // 
                if (propWidth != null && !typeof(int).IsAssignableFrom(propWidth.PropertyType)) { 
                    propWidth = null;
                } 

                if (propHeight != null && !typeof(int).IsAssignableFrom(propHeight.PropertyType)) {
                    propHeight = null;
                } 

                if (propTop != null && !typeof(int).IsAssignableFrom(propTop.PropertyType)) { 
                    propTop = null; 
                }
 
                if (propLeft != null && !typeof(int).IsAssignableFrom(propLeft.PropertyType)) {
                    propLeft = null;
                }
            } 

 
            Control targetControl = resizeComponents[0].resizeControl as Control; 

            lastMouseLoc = mouseLoc; 
            lastMouseAbs = new NativeMethods.POINT(mouseLoc.X, mouseLoc.Y);
            UnsafeNativeMethods.ClientToScreen(new HandleRef(this, behaviorService.AdornerWindowControl.Handle), lastMouseAbs);

            int minHeight = Math.Max(targetControl.MinimumSize.Height, MINSIZE); 
            int minWidth = Math.Max(targetControl.MinimumSize.Width, MINSIZE);
 
            if (dragManager != null) { 

                bool shouldSnap = true; 
                bool shouldSnapHorizontally = true;
                //if the targetcontrol is at min-size then we do not want to offer up snaplines
                if ((((targetResizeRules & SelectionRules.BottomSizeable) != 0) || ((targetResizeRules & SelectionRules.TopSizeable) != 0)) &&
                    (targetControl.Height == minHeight)) { 
                    shouldSnap = false;
                } 
                else if ((((targetResizeRules & SelectionRules.RightSizeable) != 0) || ((targetResizeRules & SelectionRules.LeftSizeable) != 0)) && 
                    (targetControl.Width == minWidth)) {
                    shouldSnap = false; 
                }

                //if the targetControl has IntegralHeight turned on, then don't snap if the control can be resized vertically
                PropertyDescriptor propIntegralHeight = TypeDescriptor.GetProperties(targetControl)["IntegralHeight"]; 
                if (propIntegralHeight != null) {
                    Object value = propIntegralHeight.GetValue(targetControl); 
                    if (value is bool && (bool)value == true) { 
                        shouldSnapHorizontally = false;
                    } 
                }


                if (!altKeyPressed && shouldSnap) { 
                    //here, ask the snapline engine to suggest an offset during our resize
 
                    // Remembering the last snapoffset allows us to correctly erase snaplines, 
                    // if the user subsequently holds down the Alt-Key. Remember that we don't physically move the mouse,
                    // we move the control. So if we didn't remember the last snapoffset 
                    // and the user then hit the Alt-Key, we would actually redraw the control at the actual mouse location,
                    // which would make the control "jump" which is not what the user would expect. Why does the control "jump"?
                    // Because when a control is snapped, we have offset the control relative to where the mouse is, but we
                    // have not update the physical mouse position. 
                    // When the user hits the Alt-Key they expect the control to be where it was (whether snapped or not).
 
                    // we can't rely on lastSnapOffset to check whether we snapped. We used to check if it was empty, 
                    // but it can be empty and we still snapped (say the control was snapped, as you continue to move the
                    // mouse, it will stay snapped for a while. During that while the snapoffset will got from x to -x (or vice versa) 
                    // and a one point hit 0.
                    //
                    // Since we have to calculate the new size/location differently based on whether we snapped or not,
                    // we have to know for sure if we snapped. 
                    //
                    // We do different math because of bug 264996: 
                    //  - if you snap, we want to move the control edge. 
                    //  - otherwise, we just want to change the size by the number of pixels moved.
                    // 
                    // See VSWhidbey #340708

                    lastSnapOffset  = dragManager.OnMouseMove(targetControl, GenerateSnapLines(targetResizeRules, mouseLoc), ref didSnap, shouldSnapHorizontally);
                } 
                else {
                    dragManager.OnMouseMove(new Rectangle(-100,-100,0,0));/*just an invalid rect - so we won't snap*///); 
                } 

                // If there's a line to snap to, the offset will come back non-zero. In that case we should adjust the 
                // mouse position with the offset such that the size calculation below takes that offset into account.

                // If there's no line, then the offset is 0, and there's no harm in adding the offset.
                mouseLoc.X += lastSnapOffset.X; 
                mouseLoc.Y += lastSnapOffset.Y;
            } 
 

            // IF WE ARE SNAPPING TO A CONTROL, then we also 
            // need to adjust for the offset between the initialPoint (where the MouseDown happened)
            // and the edge of the control otherwise we would be those pixels off when resizing the control.
            //
            // Remember that snaplines are based on the targetControl, so we need to use 
            // the targetControl to figure out the offset.
            // 
            // VSWhidbey 264996. 
            Rectangle controlBounds = new Rectangle(resizeComponents[0].resizeBounds.X, resizeComponents[0].resizeBounds.Y,
                                                      resizeComponents[0].resizeBounds.Width, resizeComponents[0].resizeBounds.Height); 
            if ((didSnap) && (targetControl.Parent != null)) {
                controlBounds.Location = behaviorService.MapAdornerWindowPoint(targetControl.Parent.Handle, controlBounds.Location);
                if (targetControl.Parent.IsMirrored) {
                    controlBounds.Offset(-controlBounds.Width,0); 
                }
            } 
 
            Rectangle newBorderRect = Rectangle.Empty;
            Rectangle targetBorderRect = Rectangle.Empty; 
            bool drawSnapline = true;
            Color backColor = targetControl.Parent != null ? targetControl.Parent.BackColor : Color.Empty;

            for (int i = 0; i < resizeComponents.Length; i++) { 
                Control control = resizeComponents[i].resizeControl as Control;
                Rectangle bounds = control.Bounds; 
                Rectangle oldBounds = bounds; 
                // We need to compute the offset beased on the original cached Bounds ...
                // ListBox doesnt allow drag on the top boundary if this is not done 
                // when it is "IntegralHeight"
                Rectangle baseBounds = resizeComponents[i].resizeBounds;

                Rectangle oldBorderRect = BehaviorService.ControlRectInAdornerWindow(control); 
                bool needToUpdate = true;
 
                // The ResizeBehavior can easily get into a situation where we are fighting with a layout engine. 
                // E.g., We resize control to 50px, LayoutEngine lays out and finds 50px was too small and
                // resized back to 100px.  This is what should happen, but it looks bad in the designer.  To avoid 
                // the flicker we temporarily turn off painting while we do the resize.
                //
                UnsafeNativeMethods.SendMessage(control.Handle, NativeMethods.WM_SETREDRAW, false, /* unused = */ 0);
                try { 

 
                    bool fRTL = false; 
                    // If the container is mirrored the control origin is in upper-right, so we need to
                    // adjust our math for that. Remember that mouse coords have origin in upper left. 
                    if (control.Parent != null && control.Parent.IsMirrored) {
                        fRTL = true;
                    }
 

                    // figure out which ones we're actually changing so we don't blow away the 
                    // controls cached sizing state.  This is important if things are docked we don't 
                    // want to destroy their "pre-dock" size.
 
                    BoundsSpecified specified = BoundsSpecified.None;

                    // When we check if we should change height, width, location,
                    // we first have to check if the targetControl allows resizing, 
                    // and then if the control we are currently resizing allows it as well.
                    // VSWhidbey #396519 
 
                    SelectionRules resizeRules = resizeComponents[i].resizeRules;
 
                    if (((targetResizeRules & SelectionRules.BottomSizeable) != 0) &&
                        ((resizeRules & SelectionRules.BottomSizeable) != 0)) {
                        int pixelHeight;
                        if (didSnap) { 
                            pixelHeight = mouseLoc.Y - controlBounds.Bottom;
                        } 
                        else { 
                            pixelHeight = AdjustPixelsForIntegralHeight(control, mouseLoc.Y - initialPoint.Y);
                        } 

                        bounds.Height = Math.Max(minHeight, baseBounds.Height + pixelHeight);
                        specified |= BoundsSpecified.Height;
                    } 

                    if (((targetResizeRules & SelectionRules.TopSizeable) != 0) && 
                        ((resizeRules & SelectionRules.TopSizeable) != 0)) { 
                        int yOffset;
                        if (didSnap) { 
                            yOffset = controlBounds.Y- mouseLoc.Y;
                        }
                        else {
                            yOffset = AdjustPixelsForIntegralHeight(control, initialPoint.Y - mouseLoc.Y); 
                        }
 
                        specified |= BoundsSpecified.Height; 
                        bounds.Height = Math.Max(minHeight, baseBounds.Height + yOffset);
                        if ((bounds.Height != minHeight) || 
                             ((bounds.Height == minHeight) && (oldBounds.Height != minHeight))) { //VSWhidbey 179862
                            specified |= BoundsSpecified.Y;
                            //VSWhidbey 179862 - if you do it fast enough, we actually could end up placing the control
                            //off the parent (say off the form), so enforce a "minimum" location 
                            bounds.Y = Math.Min(baseBounds.Bottom - minHeight, baseBounds.Y - yOffset);
                        } 
 
                    }
 
                    if (((((targetResizeRules & SelectionRules.RightSizeable) != 0) &&
                        ((resizeRules & SelectionRules.RightSizeable) != 0)) && (!fRTL)) ||
                       ((((targetResizeRules & SelectionRules.LeftSizeable) != 0) &&
                        ((resizeRules & SelectionRules.LeftSizeable) != 0)) && (fRTL))) { 

                        specified |= BoundsSpecified.Width; 
                        int xOffset = initialPoint.X; 
                        if (didSnap) {
                            xOffset = !fRTL ? controlBounds.Right : controlBounds.Left; 
                        }

                        bounds.Width = Math.Max(minWidth, baseBounds.Width + (!fRTL ? (mouseLoc.X - xOffset) :
                                                                                        (xOffset - mouseLoc.X))); 
                    }
 
                    if (((((targetResizeRules & SelectionRules.RightSizeable) != 0) && 
                        ((resizeRules & SelectionRules.RightSizeable) != 0)) && (fRTL)) ||
                       ((((targetResizeRules & SelectionRules.LeftSizeable) != 0) && 
                        ((resizeRules & SelectionRules.LeftSizeable) != 0)) && (!fRTL))) {
                        specified |= BoundsSpecified.Width;
                        int xPos = initialPoint.X;
                        if (didSnap) { 
                            xPos = !fRTL ? controlBounds.Left : controlBounds.Right;
                        } 
 
                        int xOffset = !fRTL ? (xPos - mouseLoc.X) : (mouseLoc.X - xPos);
                        bounds.Width = Math.Max(minWidth, baseBounds.Width + xOffset); 
                        if ((bounds.Width != minWidth) ||
                             ((bounds.Width == minWidth) && (oldBounds.Width != minWidth))) { //VSWhidbey 179862
                            specified |= BoundsSpecified.X;
                            //VSWhidbey 179862 - if you do it fast enough, we actually could end up placing the control 
                            //off the parent (say off the form), so enforce a "minimum" location
                            bounds.X = Math.Min(baseBounds.Right - minWidth, baseBounds.X - xOffset); 
                        } 
                    }
 

                    if (!parentGridSize.IsEmpty) {
                        bounds = AdjustToGrid(bounds, targetResizeRules);
                    } 

 
                    // Checking specified (check the diff) rather than bounds.<foo> != resizeBounds[i].<foo> 
                    // also handles the following corner cases:
                    // 
                    // 1. Create a form and add 2 buttons. Make sure that they are snapped to the left edge.
                    // Now grab the left edge of button 1, and start resizing to the left, past the
                    // snapline you will initially get, and then back to the right. What you would expect
                    // is to get the left edge snapline again. But without the specified check you wouldn't. 
                    // This is because the bounds.<foo> != resizeBounds[i].<foo> checks would fail, since the
                    // new size would now be the original size. We could probably live with that, except that 
                    // we draw the snapline below, since we correctly identified one. We could hack it so that 
                    // we didn't draw the snapline, but that would confuse the user even more.
                    // 
                    // 2. Create a form and add a single button. Place it at 100,100. Now start resizing it
                    // to the left and then back to the right. Note that with the original check (see diff),
                    // you would never be able to resize it back to position 100,100. You would get to 99,100
                    // and then to 101,100. 
                    //
                    if (((specified & BoundsSpecified.Width) == BoundsSpecified.Width) && 
                        dragging && initialResize && propWidth != null) { 
                            propWidth.SetValue(resizeComponents[i].resizeControl, bounds.Width);
                    } 

                    if (((specified & BoundsSpecified.Height) == BoundsSpecified.Height) &&
                        dragging && initialResize && propHeight != null) {
                            propHeight.SetValue(resizeComponents[i].resizeControl, bounds.Height); 
                        }
 
                    if (((specified & BoundsSpecified.X) == BoundsSpecified.X) && 
                        dragging && initialResize && propLeft != null) {
                            propLeft.SetValue(resizeComponents[i].resizeControl, bounds.X); 
                        }

                    if (((specified & BoundsSpecified.Y) == BoundsSpecified.Y) &&
                        dragging && initialResize && propTop != null) { 
                            propTop.SetValue(resizeComponents[i].resizeControl, bounds.Y);
                        } 
 
                    // We check the dragging bit here at every turn, because if there was a popup
                    // we may have lost capture and we are terminated.  At that point we shouldn't 
                    // make any changes.
                    //

                    if (dragging) { 

                        control.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height, specified); 
 
                        //Get the new resize border
                        newBorderRect = BehaviorService.ControlRectInAdornerWindow(control); 
                        if (control.Equals(targetControl)) {
                            Debug.Assert(i == 0, "The first control in the Selection should be the target control");
                            targetBorderRect = newBorderRect;
                        } 

                        //Check that the control really did resize itself. Some controls (like ListBox, MonthCalendar) 
                        //might adjust to a slightly different size than the one we pass in SetBounds. If if didn't 
                        //size, then there's no need to invalidate anything
                        if (control.Bounds == oldBounds) { 
                            needToUpdate = false;
                        }

                        // We would expect the bounds now to be what we set it to above, but this might not be the case. 
                        // If the control is hosted with e.g. a FLP, then setting the bounds above actually might force
                        // a re-layout, and the control will get moved to another spot. In this case, we don't really 
                        // want to draw a snapline. Even if we snapped to a snapline, if the control got moved, the snapline 
                        // would be in the wrong place. VSWhidbey #497636
                        if (control.Bounds != bounds) { 
                            drawSnapline = false;
                        }

                    } 

                    if (control == primaryControl && statusCommandUI != null) { 
                        statusCommandUI.SetStatusInformation(control as Component); 
                    }
                } 
                finally {
                    // While we were resizing we discarded painting messages to reduce flicker.  We now
                    // turn painting back on and manually refresh the controls.
                    // 
                    UnsafeNativeMethods.SendMessage(control.Handle, NativeMethods.WM_SETREDRAW, true, /* unused = */ 0);
 
                    //update the control 
                    if (needToUpdate) {
                        Control parent = control.Parent; 
                        if (parent != null) {
                            control.Invalidate(/* invalidateChildren = */ true);
                            parent.Invalidate(oldBounds, /* invalidateChildren = */ true);
                            parent.Update(); 
                        }
                        else { 
                            control.Refresh(); 
                        }
                    } 

                    //render the resize border
                    if (!newBorderRect.IsEmpty) {
                        using (Region newRegion = new Region(newBorderRect)) { 
                            newRegion.Exclude(Rectangle.Inflate(newBorderRect, -borderSize, -borderSize));
 
                            //No reason to get smart about only invalidating part of the border. Thought we could be but no. 
                            //The reason is the order:
                            //  ... the new border is drawn (last resize) 
                            //  On next mousemove, the control is resized which redraws the control AND ERASES THE BORDER
                            //  Then we draw the new border - flash baby.
                            // Thus this will always flicker.
                            if (needToUpdate) { 
                                using (Region oldRegion = new Region(oldBorderRect)) {
                                    oldRegion.Exclude(Rectangle.Inflate(oldBorderRect, -borderSize, -borderSize)); 
                                    BehaviorService.Invalidate(oldRegion); 
                                }
                            } 

                            //draw the new border
                            //graphics could be null if a popup came up and caused a lose focus
                            if (graphics != null) { 
                                if (lastResizeRegion != null) {
                                    if (!lastResizeRegion.Equals(newRegion, graphics)) { 
                                        lastResizeRegion.Exclude(newRegion); //we don't want to invalidate this region. 
                                        BehaviorService.Invalidate(lastResizeRegion);//might be the same, might not.
                                        lastResizeRegion.Dispose(); 
                                        lastResizeRegion = null;
                                    }
                                }
                                DesignerUtils.DrawResizeBorder(graphics, newRegion, backColor); 
                                if (lastResizeRegion == null) {
                                    lastResizeRegion = newRegion.Clone(); //we will need to dispose it later. 
                                } 
                            }
                        } 
                    }

                }
            } 

            if ((drawSnapline) && (!altKeyPressed) && (dragManager != null)) { 
                dragManager.RenderSnapLinesInternal(targetBorderRect); 
            }
 
            initialResize = false;
            return true;
        }
 
        /// <include file='doc\ResizeBehavior.uex' path='docs/doc[@for="ResizeBehavior.OnMouseUp"]/*' />
        /// <devdoc> 
        ///     This ends the Behavior by popping itself from the BehaviorStack.  Also, 
        ///     all Adorners are re-enabled at the end of a successful drag.
        /// </devdoc> 
        public override bool OnMouseUp(Glyph g, MouseButtons button) {
            try {
                if (dragging) {
 
                    if (dragManager != null) {
                        dragManager.OnMouseUp(); 
                        dragManager = null; 
                        lastSnapOffset = Point.Empty;
                        didSnap = false; 
                    }


                    if (resizeComponents != null && resizeComponents.Length > 0) { 

                        // we do these separately so as not to disturb the cached sizes for values we're not actually 
                        // changing.  For example, if a control is docked top and we modify the height, the width shouldn't 
                        // be modified.
                        // 
                        PropertyDescriptor propWidth = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Width"];
                        PropertyDescriptor propHeight = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Height"];
                        PropertyDescriptor propTop = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Top"];;
                        PropertyDescriptor propLeft = TypeDescriptor.GetProperties(resizeComponents[0].resizeControl)["Left"];; 

                        for (int i = 0; i < resizeComponents.Length; i++) { 
                            if (propWidth != null && ((Control)resizeComponents[i].resizeControl).Width != resizeComponents[i].resizeBounds.Width) { 
                                propWidth.SetValue(resizeComponents[i].resizeControl, ((Control)resizeComponents[i].resizeControl).Width);
                            } 
                            if (propHeight != null && ((Control)resizeComponents[i].resizeControl).Height != resizeComponents[i].resizeBounds.Height) {
                                propHeight.SetValue(resizeComponents[i].resizeControl, ((Control)resizeComponents[i].resizeControl).Height);
                            }
 
                            if (propTop != null && ((Control)resizeComponents[i].resizeControl).Top != resizeComponents[i].resizeBounds.Y) {
                                propTop.SetValue(resizeComponents[i].resizeControl, ((Control)resizeComponents[i].resizeControl).Top); 
                            } 
                            if (propLeft != null && ((Control)resizeComponents[i].resizeControl).Left != resizeComponents[i].resizeBounds.X) {
                                propLeft.SetValue(resizeComponents[i].resizeControl, ((Control)resizeComponents[i].resizeControl).Left); 
                            }

                            if (resizeComponents[i].resizeControl == primaryControl && statusCommandUI != null) {
                                statusCommandUI.SetStatusInformation(primaryControl as Component); 
                            }
                        } 
                    } 
                }
 
                if (resizeTransaction != null) {
                    DesignerTransaction t = resizeTransaction;
                    resizeTransaction = null;
                    using (t) { 
                        t.Commit();
                    } 
                } 
            }
            finally { 
                // This pops us off the stack, re-enables adorners and clears the "dragging" flag.
                OnLoseCapture(g, EventArgs.Empty);
            }
 
            return false;
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
