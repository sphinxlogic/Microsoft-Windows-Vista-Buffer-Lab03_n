namespace System.Windows.Forms.Design { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Security; 
    using System.Security.Permissions;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Forms.Design;
    using System.Runtime.InteropServices; 
    using System.Windows.Forms.Design.Behavior;
 
    /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService"]/*' /> 
    /// <devdoc>
    ///     Transparent Window to parent the DropDowns. 
    /// </devdoc>
    internal sealed class ToolStripAdornerWindowService : IDisposable {

        private IServiceProvider                serviceProvider;        //standard service provider 
        private ToolStripAdornerWindow          toolStripAdornerWindow; //the transparent window all glyphs are drawn to
        private BehaviorService                 bs; 
        private Adorner                         dropDownAdorner; 

        private ArrayList                       dropDownCollection; 

        private IOverlayService os;

        /// <devdoc> 
        ///     This constructor is called from DocumentDesigner's Initialize method.
        /// </devdoc> 
        internal ToolStripAdornerWindowService(IServiceProvider serviceProvider, Control windowFrame) { 
            this.serviceProvider = serviceProvider;
 
            //create the AdornerWindow
            toolStripAdornerWindow = new ToolStripAdornerWindow(windowFrame);
            bs = (BehaviorService)serviceProvider.GetService(typeof(BehaviorService));
            int indexToInsert = bs.AdornerWindowIndex; 

            //use the adornerWindow as an overlay 
            os = (IOverlayService)serviceProvider.GetService(typeof(IOverlayService)); 
            if (os != null) {
                os.InsertOverlay(toolStripAdornerWindow, indexToInsert); 
            }

            dropDownAdorner = new Adorner();
            int count = bs.Adorners.Count; 

            // Why this is NEEDED ? 
            // To Add the Adorner at proper index in the AdornerCollection for the BehaviorService 
            // So that the DesignerActionGlyph always stays on the Top.
            if (count > 1) 
            {
                bs.Adorners.Insert(count - 1, dropDownAdorner);
            }
 
        }
 
        /// <devdoc> 
        ///     Returns the actual Control that represents the transparent AdornerWindow.
        /// </devdoc> 
        internal Control ToolStripAdornerWindowControl {
            get {
                return toolStripAdornerWindow;
            } 
        }
 
        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.AdornerWindowGraphics"]/*' /> 
        /// <devdoc>
        ///     Creates and returns a Graphics object for the AdornerWindow 
        /// </devdoc>
        public Graphics ToolStripAdornerWindowGraphics {
            get {
                return toolStripAdornerWindow.CreateGraphics(); 
            }
        } 
 

        internal Adorner DropDownAdorner { 
            get {
                return dropDownAdorner;
            }
        } 

        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.Dispose"]/*' /> 
        /// <devdoc> 
        ///     Disposes the behavior service.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
        public void Dispose() {
            if (os != null) {
                os.RemoveOverlay(toolStripAdornerWindow); 
            }
 
            toolStripAdornerWindow.Dispose(); 

            if (bs != null) { 
                bs.Adorners.Remove(dropDownAdorner);
                bs = null;
            }
 
            if (dropDownAdorner != null)
            { 
            	dropDownAdorner.Glyphs.Clear(); 
            	dropDownAdorner = null;
            } 
        }


        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.AdornerWindowPointToScreen"]/*' /> 
        /// <devdoc>
        ///     Translates a point in the AdornerWindow to screen coords. 
        /// </devdoc> 
        public Point AdornerWindowPointToScreen(Point p) {
            NativeMethods.POINT offset = new NativeMethods.POINT(p.X, p.Y); 
            NativeMethods.MapWindowPoints(toolStripAdornerWindow.Handle, IntPtr.Zero, offset, 1);
            return new Point(offset.x, offset.y);
        }
 
        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.AdornerWindowToScreen"]/*' />
        /// <devdoc> 
        ///     Gets the location (upper-left corner) of the AdornerWindow in screen coords. 
        /// </devdoc>
        public Point AdornerWindowToScreen() { 
            Point origin = new Point(0, 0);
            return AdornerWindowPointToScreen(origin);
        }
 
        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.ControlToAdornerWindow"]/*' />
        /// <devdoc> 
        /// Returns the location of a Control translated to AdornerWidnow coords. 
        /// </devdoc>
        public Point ControlToAdornerWindow(Control c) { 
            if (c.Parent == null) {
                return Point.Empty;
            }
 
            NativeMethods.POINT pt = new NativeMethods.POINT();
            pt.x = c.Left; 
            pt.y = c.Top; 
            NativeMethods.MapWindowPoints(c.Parent.Handle, toolStripAdornerWindow.Handle, pt, 1);
            return new Point(pt.x, pt.y); 
        }

        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.Invalidate"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs. 
        /// </devdoc> 
        public void Invalidate() {
            toolStripAdornerWindow.InvalidateAdornerWindow(); 
        }

        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.Invalidate2"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs. 
        /// </devdoc> 
        public void Invalidate(Rectangle rect) {
            toolStripAdornerWindow.InvalidateAdornerWindow(rect); 
        }

        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.Invalidate3"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs. 
        /// </devdoc> 
        public void Invalidate(Region r) {
            toolStripAdornerWindow.InvalidateAdornerWindow(r); 
        }

        internal ArrayList DropDowns
        { 
            get {
                return dropDownCollection; 
            } 
            set {
                if (dropDownCollection == null) { 
                    dropDownCollection = new ArrayList();
                }
            }
 
        }
 
 

        /// <devdoc> 
        ///     ControlDesigner calls this internal method in response to a WmPaint.
        ///     We need to know when a ControlDesigner paints - 'cause we will need
        ///     to re-paint any glyphs above of this Control.
        /// </devdoc> 
        internal void ProcessPaintMessage(Rectangle paintRect) {
            //Note, we don't call BehSvc.Invalidate because 
            //this will just cause the messages to recurse. 
            //Instead, invalidating this adornerWindow will
            //just cause a "propagatePaint" and draw the glyphs. 
            toolStripAdornerWindow.Invalidate(paintRect);
        }

        /// <devdoc> 
        ///     The AdornerWindow is a transparent window that resides ontop of the
        ///     Designer's Frame.  This window is used by the ToolStripAdornerWindowService to 
        ///     parent the MenuItem DropDowns. 
        /// </devdoc>
        private class ToolStripAdornerWindow : Control { 

            private Control                 designerFrame;//the designer's frame

            /// <devdoc> 
            ///     Constructor that parents itself to the Designer Frame and hooks all
            ///     necessary events. 
            /// </devdoc> 
            [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
            internal ToolStripAdornerWindow(Control designerFrame) { 
                this.designerFrame = designerFrame;
                this.Dock = DockStyle.Fill;
                this.AllowDrop = true;
                this.Text = "ToolStripAdornerWindow"; 

                SetStyle(ControlStyles.Opaque,true); 
 
            }
 
            /// <devdoc>
            ///     The key here is to set the appropriate TransparetWindow style.
            /// </devdoc>
            protected override CreateParams CreateParams 
            {
                get 
                { 
                    CreateParams cp = base.CreateParams;
                    cp.Style &= ~(NativeMethods.WS_CLIPCHILDREN | NativeMethods.WS_CLIPSIBLINGS); 
                    cp.ExStyle |= 0x00000020/*WS_EX_TRANSPARENT*/;
                    return cp;
                }
            } 

            /// <devdoc> 
            ///     We'll use CreateHandle as our notification for creating 
            //      our mouse hooker.
            /// </devdoc> 
            protected override void OnHandleCreated(EventArgs e) {
                base.OnHandleCreated(e);
            }
 
            /// <devdoc>
            ///     Unhook and null out our mouseHook. 
            /// </devdoc> 
            protected override void OnHandleDestroyed(EventArgs e) {
                base.OnHandleDestroyed(e); 
            }

            /// <devdoc>
            ///     Null out our mouseHook and unhook any events. 
            /// </devdoc>
            protected override void Dispose(bool disposing) { 
                if (disposing) { 
                    if (designerFrame != null) {
                        designerFrame = null; 
                    }

                }
                base.Dispose(disposing); 
            }
 
 

            /// <devdoc> 
            ///     Returns true if the DesignerFrame is created & not being disposed.
            /// </devdoc>
            private bool DesignerFrameValid {
                get { 
                    if (designerFrame == null || designerFrame.IsDisposed || !designerFrame.IsHandleCreated) {
                        return false; 
                    } 
                    return true;
                } 
            }

            /// <devdoc>
            ///     Invalidates the transparent AdornerWindow by asking 
            ///     the Designer Frame beneath it to invalidate.  Note the
            ///     they use of the .Update() call for perf. purposes. 
            /// </devdoc> 
            internal void InvalidateAdornerWindow() {
                if (DesignerFrameValid) { 
                    designerFrame.Invalidate(true);
                    designerFrame.Update();
                }
            } 

            /// <devdoc> 
            ///     Invalidates the transparent AdornerWindow by asking 
            ///     the Designer Frame beneath it to invalidate.  Note the
            ///     they use of the .Update() call for perf. purposes. 
            /// </devdoc>
            internal void InvalidateAdornerWindow(Region region) {
                if (DesignerFrameValid) {
                    designerFrame.Invalidate(region, true); 
                    designerFrame.Update();
                } 
            } 

            /// <devdoc> 
            ///     Invalidates the transparent AdornerWindow by asking
            ///     the Designer Frame beneath it to invalidate.  Note the
            ///     they use of the .Update() call for perf. purposes.
            /// </devdoc> 
            internal void InvalidateAdornerWindow(Rectangle rectangle) {
                if (DesignerFrameValid) { 
                    designerFrame.Invalidate(rectangle, true); 
                    designerFrame.Update();
                } 

            }

 
            /// <devdoc>
            ///     The AdornerWindow intercepts all designer-related messages and forwards them 
            ///     to the BehaviorService for appropriate actions.  Note that Paint and HitTest 
            ///     messages are correctly parsed and translated to AdornerWindow coords.
            /// </devdoc> 
            protected override void WndProc(ref Message m)
            {
                switch(m.Msg)
                { 
                    case NativeMethods.WM_NCHITTEST:
                        m.Result = (IntPtr)(NativeMethods.HTTRANSPARENT); 
                        break; 

                    default: 
                        base.WndProc(ref m);
                        break;
                }
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Security; 
    using System.Security.Permissions;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Forms.Design;
    using System.Runtime.InteropServices; 
    using System.Windows.Forms.Design.Behavior;
 
    /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService"]/*' /> 
    /// <devdoc>
    ///     Transparent Window to parent the DropDowns. 
    /// </devdoc>
    internal sealed class ToolStripAdornerWindowService : IDisposable {

        private IServiceProvider                serviceProvider;        //standard service provider 
        private ToolStripAdornerWindow          toolStripAdornerWindow; //the transparent window all glyphs are drawn to
        private BehaviorService                 bs; 
        private Adorner                         dropDownAdorner; 

        private ArrayList                       dropDownCollection; 

        private IOverlayService os;

        /// <devdoc> 
        ///     This constructor is called from DocumentDesigner's Initialize method.
        /// </devdoc> 
        internal ToolStripAdornerWindowService(IServiceProvider serviceProvider, Control windowFrame) { 
            this.serviceProvider = serviceProvider;
 
            //create the AdornerWindow
            toolStripAdornerWindow = new ToolStripAdornerWindow(windowFrame);
            bs = (BehaviorService)serviceProvider.GetService(typeof(BehaviorService));
            int indexToInsert = bs.AdornerWindowIndex; 

            //use the adornerWindow as an overlay 
            os = (IOverlayService)serviceProvider.GetService(typeof(IOverlayService)); 
            if (os != null) {
                os.InsertOverlay(toolStripAdornerWindow, indexToInsert); 
            }

            dropDownAdorner = new Adorner();
            int count = bs.Adorners.Count; 

            // Why this is NEEDED ? 
            // To Add the Adorner at proper index in the AdornerCollection for the BehaviorService 
            // So that the DesignerActionGlyph always stays on the Top.
            if (count > 1) 
            {
                bs.Adorners.Insert(count - 1, dropDownAdorner);
            }
 
        }
 
        /// <devdoc> 
        ///     Returns the actual Control that represents the transparent AdornerWindow.
        /// </devdoc> 
        internal Control ToolStripAdornerWindowControl {
            get {
                return toolStripAdornerWindow;
            } 
        }
 
        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.AdornerWindowGraphics"]/*' /> 
        /// <devdoc>
        ///     Creates and returns a Graphics object for the AdornerWindow 
        /// </devdoc>
        public Graphics ToolStripAdornerWindowGraphics {
            get {
                return toolStripAdornerWindow.CreateGraphics(); 
            }
        } 
 

        internal Adorner DropDownAdorner { 
            get {
                return dropDownAdorner;
            }
        } 

        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.Dispose"]/*' /> 
        /// <devdoc> 
        ///     Disposes the behavior service.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
        public void Dispose() {
            if (os != null) {
                os.RemoveOverlay(toolStripAdornerWindow); 
            }
 
            toolStripAdornerWindow.Dispose(); 

            if (bs != null) { 
                bs.Adorners.Remove(dropDownAdorner);
                bs = null;
            }
 
            if (dropDownAdorner != null)
            { 
            	dropDownAdorner.Glyphs.Clear(); 
            	dropDownAdorner = null;
            } 
        }


        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.AdornerWindowPointToScreen"]/*' /> 
        /// <devdoc>
        ///     Translates a point in the AdornerWindow to screen coords. 
        /// </devdoc> 
        public Point AdornerWindowPointToScreen(Point p) {
            NativeMethods.POINT offset = new NativeMethods.POINT(p.X, p.Y); 
            NativeMethods.MapWindowPoints(toolStripAdornerWindow.Handle, IntPtr.Zero, offset, 1);
            return new Point(offset.x, offset.y);
        }
 
        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.AdornerWindowToScreen"]/*' />
        /// <devdoc> 
        ///     Gets the location (upper-left corner) of the AdornerWindow in screen coords. 
        /// </devdoc>
        public Point AdornerWindowToScreen() { 
            Point origin = new Point(0, 0);
            return AdornerWindowPointToScreen(origin);
        }
 
        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.ControlToAdornerWindow"]/*' />
        /// <devdoc> 
        /// Returns the location of a Control translated to AdornerWidnow coords. 
        /// </devdoc>
        public Point ControlToAdornerWindow(Control c) { 
            if (c.Parent == null) {
                return Point.Empty;
            }
 
            NativeMethods.POINT pt = new NativeMethods.POINT();
            pt.x = c.Left; 
            pt.y = c.Top; 
            NativeMethods.MapWindowPoints(c.Parent.Handle, toolStripAdornerWindow.Handle, pt, 1);
            return new Point(pt.x, pt.y); 
        }

        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.Invalidate"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs. 
        /// </devdoc> 
        public void Invalidate() {
            toolStripAdornerWindow.InvalidateAdornerWindow(); 
        }

        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.Invalidate2"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs. 
        /// </devdoc> 
        public void Invalidate(Rectangle rect) {
            toolStripAdornerWindow.InvalidateAdornerWindow(rect); 
        }

        /// <include file='doc\ToolStripAdornerWindowService.uex' path='docs/doc[@for="ToolStripAdornerWindowService.Invalidate3"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs. 
        /// </devdoc> 
        public void Invalidate(Region r) {
            toolStripAdornerWindow.InvalidateAdornerWindow(r); 
        }

        internal ArrayList DropDowns
        { 
            get {
                return dropDownCollection; 
            } 
            set {
                if (dropDownCollection == null) { 
                    dropDownCollection = new ArrayList();
                }
            }
 
        }
 
 

        /// <devdoc> 
        ///     ControlDesigner calls this internal method in response to a WmPaint.
        ///     We need to know when a ControlDesigner paints - 'cause we will need
        ///     to re-paint any glyphs above of this Control.
        /// </devdoc> 
        internal void ProcessPaintMessage(Rectangle paintRect) {
            //Note, we don't call BehSvc.Invalidate because 
            //this will just cause the messages to recurse. 
            //Instead, invalidating this adornerWindow will
            //just cause a "propagatePaint" and draw the glyphs. 
            toolStripAdornerWindow.Invalidate(paintRect);
        }

        /// <devdoc> 
        ///     The AdornerWindow is a transparent window that resides ontop of the
        ///     Designer's Frame.  This window is used by the ToolStripAdornerWindowService to 
        ///     parent the MenuItem DropDowns. 
        /// </devdoc>
        private class ToolStripAdornerWindow : Control { 

            private Control                 designerFrame;//the designer's frame

            /// <devdoc> 
            ///     Constructor that parents itself to the Designer Frame and hooks all
            ///     necessary events. 
            /// </devdoc> 
            [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
            internal ToolStripAdornerWindow(Control designerFrame) { 
                this.designerFrame = designerFrame;
                this.Dock = DockStyle.Fill;
                this.AllowDrop = true;
                this.Text = "ToolStripAdornerWindow"; 

                SetStyle(ControlStyles.Opaque,true); 
 
            }
 
            /// <devdoc>
            ///     The key here is to set the appropriate TransparetWindow style.
            /// </devdoc>
            protected override CreateParams CreateParams 
            {
                get 
                { 
                    CreateParams cp = base.CreateParams;
                    cp.Style &= ~(NativeMethods.WS_CLIPCHILDREN | NativeMethods.WS_CLIPSIBLINGS); 
                    cp.ExStyle |= 0x00000020/*WS_EX_TRANSPARENT*/;
                    return cp;
                }
            } 

            /// <devdoc> 
            ///     We'll use CreateHandle as our notification for creating 
            //      our mouse hooker.
            /// </devdoc> 
            protected override void OnHandleCreated(EventArgs e) {
                base.OnHandleCreated(e);
            }
 
            /// <devdoc>
            ///     Unhook and null out our mouseHook. 
            /// </devdoc> 
            protected override void OnHandleDestroyed(EventArgs e) {
                base.OnHandleDestroyed(e); 
            }

            /// <devdoc>
            ///     Null out our mouseHook and unhook any events. 
            /// </devdoc>
            protected override void Dispose(bool disposing) { 
                if (disposing) { 
                    if (designerFrame != null) {
                        designerFrame = null; 
                    }

                }
                base.Dispose(disposing); 
            }
 
 

            /// <devdoc> 
            ///     Returns true if the DesignerFrame is created & not being disposed.
            /// </devdoc>
            private bool DesignerFrameValid {
                get { 
                    if (designerFrame == null || designerFrame.IsDisposed || !designerFrame.IsHandleCreated) {
                        return false; 
                    } 
                    return true;
                } 
            }

            /// <devdoc>
            ///     Invalidates the transparent AdornerWindow by asking 
            ///     the Designer Frame beneath it to invalidate.  Note the
            ///     they use of the .Update() call for perf. purposes. 
            /// </devdoc> 
            internal void InvalidateAdornerWindow() {
                if (DesignerFrameValid) { 
                    designerFrame.Invalidate(true);
                    designerFrame.Update();
                }
            } 

            /// <devdoc> 
            ///     Invalidates the transparent AdornerWindow by asking 
            ///     the Designer Frame beneath it to invalidate.  Note the
            ///     they use of the .Update() call for perf. purposes. 
            /// </devdoc>
            internal void InvalidateAdornerWindow(Region region) {
                if (DesignerFrameValid) {
                    designerFrame.Invalidate(region, true); 
                    designerFrame.Update();
                } 
            } 

            /// <devdoc> 
            ///     Invalidates the transparent AdornerWindow by asking
            ///     the Designer Frame beneath it to invalidate.  Note the
            ///     they use of the .Update() call for perf. purposes.
            /// </devdoc> 
            internal void InvalidateAdornerWindow(Rectangle rectangle) {
                if (DesignerFrameValid) { 
                    designerFrame.Invalidate(rectangle, true); 
                    designerFrame.Update();
                } 

            }

 
            /// <devdoc>
            ///     The AdornerWindow intercepts all designer-related messages and forwards them 
            ///     to the BehaviorService for appropriate actions.  Note that Paint and HitTest 
            ///     messages are correctly parsed and translated to AdornerWindow coords.
            /// </devdoc> 
            protected override void WndProc(ref Message m)
            {
                switch(m.Msg)
                { 
                    case NativeMethods.WM_NCHITTEST:
                        m.Result = (IntPtr)(NativeMethods.HTTRANSPARENT); 
                        break; 

                    default: 
                        base.WndProc(ref m);
                        break;
                }
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
