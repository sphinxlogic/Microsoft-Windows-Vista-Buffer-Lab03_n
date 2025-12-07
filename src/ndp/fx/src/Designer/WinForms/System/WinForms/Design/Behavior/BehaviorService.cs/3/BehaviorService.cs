namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing; 
    using System.Drawing.Design;
    using System.ComponentModel.Design.Serialization;
    using System.Security;
    using System.Security.Permissions; 
    using System.Windows.Forms.Design;
    using System.Runtime.InteropServices; 
    using System.Globalization; 
    using Microsoft.Win32;
 
    /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService"]/*' />
    /// <devdoc>
    ///     The BehaviorService essentially manages all things UI in the designer.
    ///     When the BehaviorService is created it adds a transparent window over the 
    ///     designer frame.  The BehaviorService can then use this window to render UI
    ///     elements (called Glyphs) as well as catch all mouse messages.  By doing 
    ///     so - the BehaviorService can control designer behavior.  The BehaviorService 
    ///     supports a BehaviorStack.  'Behavior' objects can be pushed onto this stack.
    ///     When a message is intercepted via the transparent window, the BehaviorService 
    ///     can send the message to the Behavior at the top of the stack.  This allows
    ///     for different UI modes depending on the currently pushed Behavior.  The
    ///     BehaviorService is used to render all 'Glyphs': selection borders, grab handles,
    ///     smart tags etc... as well as control many of the design-time behaviors: dragging, 
    ///     selection, snap lines, etc...
    /// </devdoc> 
    public sealed class BehaviorService : IDisposable { 

        private IServiceProvider                serviceProvider;        //standard service provider 
        private AdornerWindow                   adornerWindow;          //the transparent window all glyphs are drawn to
        private BehaviorServiceAdornerCollection               adorners;               //we manage all adorners (glyph-containers) here
        private ArrayList                       behaviorStack;          //the stack behavior objects can be pushed to and popped from
        private Behavior                        captureBehavior;        //the behavior that currently has capture; may be null 
        private Glyph                           hitTestedGlyph;         //the last valid glyph that was hit tested
        private IToolboxService                 toolboxSvc;             //allows us to have the toolbox choose a cursor 
        private Control                         dropSource;             //actual control used to call .dodragdrop 
        private DragEventArgs                   validDragArgs;          //if valid - this is used to fabricate drag enter/leave envents
        private BehaviorDragDropEventHandler    beginDragHandler;       //fired directly before we call .DoDragDrop() 
        private BehaviorDragDropEventHandler    endDragHandler;         //fired directly after we call .DoDragDrop()
        private EventHandler                    synchronizeEventHandler;    //fired when we want to synchronize the selection
        private NativeMethods.TRACKMOUSEEVENT   trackMouseEvent;        //demand created (once) used to track the mouse hover event
        private bool                            trackingMouseEvent;     //state identifying current mouse tracking 
        private string[]                        testHook_RecentSnapLines; //we keep track of the last snaplines we found - for testing purposes
        private MenuCommandHandler              menuCommandHandler;     //private object that handles all menu commands 
        private bool                            useSnapLines;           //indicates if this designer session is using snaplines or snapping to a grid 
        private bool                            queriedSnapLines;       //only query for this once since we require the user restart design sessions when this changes
 
        private Hashtable                       dragEnterReplies;       // we keep track of whether glyph has already responded to a DragEnter this D&D.

 		private static TraceSwitch              dragDropSwitch = new TraceSwitch("BSDRAGDROP", "Behavior service drag & drop messages");
 
        private bool                            dragging = false;        // are we in a drag
        private bool                            cancelDrag = false;     //  should we cancel the drag on the next QueryContinueDrag 
 
        private int adornerWindowIndex = -1;
 
        //test hooks for SnapLines
        private static int WM_GETALLSNAPLINES;
        private static int WM_GETRECENTSNAPLINES;
 
        private DesignerActionUI                  actionPointer;//pointer to the designer action service so we can supply mouse over notifications
 
        private const string ToolboxFormat = ".NET Toolbox Item"; // used to detect if a drag is coming from the toolbox. 

        /// <devdoc> 
        ///     This constructor is called from DocumentDesigner's Initialize method.
        /// </devdoc>
        [SuppressMessage("Microsoft.Performance", "CA1805:DoNotInitializeUnnecessarily")]
        internal BehaviorService(IServiceProvider serviceProvider, Control windowFrame) { 
            this.serviceProvider = serviceProvider;
 
            //create the AdornerWindow 
            adornerWindow = new AdornerWindow(this, windowFrame);
 
            //use the adornerWindow as an overlay
            IOverlayService os = (IOverlayService)serviceProvider.GetService(typeof(IOverlayService));
            if (os != null) {
                adornerWindowIndex = os.PushOverlay(adornerWindow); 
            }
 
            dragEnterReplies = new Hashtable(); 

            //start with an empty adorner collection & no behavior on the stack 
            adorners = new BehaviorServiceAdornerCollection(this);
            behaviorStack = new ArrayList();

            hitTestedGlyph = null; 
            validDragArgs = null;
            actionPointer = null; 
            trackMouseEvent = null; 
            trackingMouseEvent = false;
 
            //create out object that will handle all menucommands
            IMenuCommandService menuCommandService = serviceProvider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
 
            if (menuCommandService != null && host != null) {
 
                menuCommandHandler = new MenuCommandHandler(this, menuCommandService); 

                host.RemoveService(typeof(IMenuCommandService)); 
                host.AddService(typeof(IMenuCommandService), menuCommandHandler);
            }

            //default layoutmode is SnapToGrid. 
            useSnapLines = false;
            queriedSnapLines = false; 
 
            //test hooks
            WM_GETALLSNAPLINES = SafeNativeMethods.RegisterWindowMessage("WM_GETALLSNAPLINES"); 
            WM_GETRECENTSNAPLINES = SafeNativeMethods.RegisterWindowMessage("WM_GETRECENTSNAPLINES");

            // Listen to the SystemEvents so that we can resync selection based on display settings etc.
            SystemEvents.DisplaySettingsChanged += new EventHandler(this.OnSystemSettingChanged); 
            SystemEvents.InstalledFontsChanged += new EventHandler(this.OnSystemSettingChanged);
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(this.OnUserPreferenceChanged); 
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Adorners"]/*' /> 
        /// <devdoc>
        ///     Read-only property that returns the AdornerCollection that the BehaivorService manages.
        /// </devdoc>
        public BehaviorServiceAdornerCollection Adorners { 
            get {
                return adorners; 
            } 
        }
 
        /// <devdoc>
        ///     Returns the index of  the transparent AdornerWindow.
        /// </devdoc>
        internal int AdornerWindowIndex { 
            get
            { 
                return adornerWindowIndex; 
            }
        } 

        /// <devdoc>
        ///     Returns the actual Control that represents the transparent AdornerWindow.
        /// </devdoc> 
        internal Control AdornerWindowControl {
            get { 
                return adornerWindow; 
            }
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.AdornerWindowGraphics"]/*' />
        /// <devdoc>
        ///     Creates and returns a Graphics object for the AdornerWindow 
        /// </devdoc>
        public Graphics AdornerWindowGraphics { 
            get { 
                Graphics result = adornerWindow.CreateGraphics();
                result.Clip = new Region(adornerWindow.DesignerFrameDisplayRectangle); 
                return result;
            }
        }
 
        public Behavior CurrentBehavior {
            get { 
                if(behaviorStack != null && behaviorStack.Count > 0) { 
                    return (behaviorStack[0] as Behavior);
                } 
                else {
                    return null;
                }
            } 
        }
 
        /// <devdoc> 
        ///     If the drag operation should be cancelled (say we get a WM_CANCELMODE), set
        ///     CancelDrag to true. The next time QueryContinueDrag is called, the drag operation 
        ///     will be cancelled.
        /// </devdoc>
        internal bool CancelDrag {
            get { 
                return cancelDrag;
            } 
 
            set {
                cancelDrag = value; 
            }
        }

        /// <devdoc> 
        //  This value will be set by the DocumentDesigner.  'actionpointer' will be called
        //  when we get a mouse enter of a new component glyph.  This is so the Designer 
        //  Actions can 'hover-active' if needed. 
        /// </devdoc>
        internal DesignerActionUI DesignerActionUI { 
            get {
                return actionPointer;
            }
            set { 
                actionPointer = value;
            } 
        } 

        internal bool Dragging { 
            get{
                return dragging;
            }
        } 

 
        internal bool HasCapture { 
            get {
                return captureBehavior != null; 
            }
        }

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.LayoutMode"]/*' /> 
        /// <devdoc>
        ///     Returns the LayoutMode setting of the current designer session.  Either 
        ///     SnapLines or SnapToGrid. 
        /// </devdoc>
        internal bool UseSnapLines { 
            get {
                //we only check for this service/value once since we require the
                //user to re-open the designer session after these types of option
                //have been modified 
                if (!queriedSnapLines) {
                    queriedSnapLines = true; 
                    useSnapLines = DesignerUtils.UseSnapLines(serviceProvider); 
                }
 
                return useSnapLines;
            }
        }
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.AdornerWindowPointToScreen"]/*' />
        /// <devdoc> 
        ///     Translates a point in the AdornerWindow to screen coords. 
        /// </devdoc>
        public Point AdornerWindowPointToScreen(Point p) { 
            NativeMethods.POINT offset = new NativeMethods.POINT(p.X, p.Y);
            NativeMethods.MapWindowPoints(adornerWindow.Handle, IntPtr.Zero, offset, 1);
            return new Point(offset.x, offset.y);
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.AdornerWindowToScreen"]/*' /> 
        /// <devdoc> 
        ///     Gets the location (upper-left corner) of the AdornerWindow in screen coords.
        /// </devdoc> 
        public Point AdornerWindowToScreen() {
            Point origin = new Point(0, 0);
            return AdornerWindowPointToScreen(origin);
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.ControlToAdornerWindow"]/*' /> 
        /// <devdoc> 
        /// Returns the location of a Control translated to AdornerWindow coords.
        /// </devdoc> 
        public Point ControlToAdornerWindow(Control c) {
            if (c.Parent == null) {
                return Point.Empty;
            } 

            NativeMethods.POINT pt = new NativeMethods.POINT(); 
            pt.x = c.Left; 
            pt.y = c.Top;
            NativeMethods.MapWindowPoints(c.Parent.Handle, adornerWindow.Handle, pt, 1); 
            if(c.Parent.IsMirrored) {
                pt.x -= c.Width;
            }
            return new Point(pt.x, pt.y); 
        }
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.MapAdornerWindowPoint"]/*' /> 
        /// <devdoc>
        /// Converts a point in handle's coordinate system to AdornerWindow coords. 
        /// </devdoc>
        public Point MapAdornerWindowPoint(IntPtr handle, Point pt) {
            NativeMethods.POINT nativePoint = new NativeMethods.POINT();
            nativePoint.x = pt.X; 
            nativePoint.y = pt.Y;
            NativeMethods.MapWindowPoints(handle, adornerWindow.Handle, nativePoint, 1); 
            return new Point(nativePoint.x, nativePoint.y); 
        }
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.ControlRectInAdornerWindow"]/*' />
        /// <devdoc>
        /// Returns the bounding rectangle of a Control translated to AdornerWindow coords.
        /// </devdoc> 
        public Rectangle ControlRectInAdornerWindow(Control c){
            if(c.Parent == null) { 
                return Rectangle.Empty; 
            }
            Point loc = ControlToAdornerWindow(c); 

            return new Rectangle(loc, c.Size);
        }
 
    	internal bool IsDisposed
    	{ 
    		get 
    		{
    			return adornerWindow == null || adornerWindow.IsDisposed; 
    		}
    	}

        /// <devdoc> 
        ///     We demand create a Control to call .DoDragDrop() on.  With
        ///     this control, we'll hook the drag events: querycontinue and 
        //      givefeedback and forward them along to the DropSourceBehavior. 
        /// </devdoc>
        private Control DropSource { 
            get {
                if (dropSource == null) {
                    dropSource = new Control();
                } 
                return dropSource;
            } 
        } 

        /// <devdoc> 
        ///     Called by the DragAssistanceManager after a snapline/drag op
        ///     has completed - we store this data for testing purposes.  See
        ///     TestHook_GetRecentSnapLines method.
        /// </devdoc> 
        internal string[] RecentSnapLines {
            set { 
                this.testHook_RecentSnapLines = value; 
            }
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.BeginDrag"]/*' />
        /// <devdoc>
        ///     The BehaviorService fires the BeginDrag event immediately 
        ///     before it starts a drop/drop operation via DoBeginDragDrop.
        /// </devdoc> 
        public event BehaviorDragDropEventHandler BeginDrag { 
            add {
                beginDragHandler += value; 
            }
            remove {
                beginDragHandler -= value;
            } 
        }
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.EndDrag"]/*' /> 
        /// <devdoc>
        ///     The BehaviorService fires the EndDrag event immediately 
        ///     after the drag operation has completed.
        /// </devdoc>
        public event BehaviorDragDropEventHandler EndDrag {
            add { 
                endDragHandler += value;
            } 
            remove { 
                endDragHandler -= value;
            } 
        }

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Synchronize"]/*' />
        /// <devdoc> 
        ///     The BehaviorService fires the Synchronize event when the current selection should be synchronized (refreshed).
        /// </devdoc> 
        public event EventHandler Synchronize { 
            add {
                synchronizeEventHandler += value; 
            }

            remove {
                synchronizeEventHandler -= value; 
            }
        } 
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Dispose"]/*' />
        /// <devdoc> 
        ///     Disposes the behavior service.
        /// </devdoc>
        public void Dispose() {
            // remove adorner window from overlay service 
            IOverlayService os = (IOverlayService)serviceProvider.GetService(typeof(IOverlayService));
            if (os != null) { 
                os.RemoveOverlay(adornerWindow); 
            }
 
            if (dropSource != null) {
                dropSource.Dispose();
            }
 
            IMenuCommandService menuCommandService = serviceProvider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost; 
 
            MenuCommandHandler menuCommandHandler = null;
            if (menuCommandService != null) 
                menuCommandHandler = menuCommandService as MenuCommandHandler;

            if (menuCommandHandler != null && host != null) {
                IMenuCommandService oldMenuCommandService = menuCommandHandler.MenuService; 
                host.RemoveService(typeof(IMenuCommandService));
                host.AddService(typeof(IMenuCommandService), oldMenuCommandService); 
            } 

            adornerWindow.Dispose(); 

            SystemEvents.DisplaySettingsChanged -= new EventHandler(this.OnSystemSettingChanged);
            SystemEvents.InstalledFontsChanged -= new EventHandler(this.OnSystemSettingChanged);
            SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(this.OnUserPreferenceChanged); 
	
 
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.DoDragDrop"]/*' /> 
        /// <devdoc>
        ///     Enables Behaviors to call DoDragDrop.
        /// </devdoc>
        internal DragDropEffects DoDragDrop(DropSourceBehavior dropSourceBehavior) { 
            //hook events
            DropSource.QueryContinueDrag += new QueryContinueDragEventHandler(dropSourceBehavior.QueryContinueDrag); 
            DropSource.GiveFeedback += new GiveFeedbackEventHandler(dropSourceBehavior.GiveFeedback); 

            DragDropEffects res = DragDropEffects.None; 

            //build up the eventargs for firing our dragbegin/end events
            ICollection dragComponents = ((DropSourceBehavior.BehaviorDataObject)dropSourceBehavior.DataObject).DragComponents;
            BehaviorDragDropEventArgs eventArgs = new BehaviorDragDropEventArgs(dragComponents); 

            try { 
                try { 
                    OnBeginDrag(eventArgs);
                    dragging = true; 
                    cancelDrag = false;
                    // This is normally cleared on OnMouseUp, but we might not get an OnMouseUp to clear it. VSWhidbey #474259
                    // So let's make sure it is really cleared when we start the drag.
                    dragEnterReplies.Clear(); 
                    res = DropSource.DoDragDrop(dropSourceBehavior.DataObject, dropSourceBehavior.AllowedEffects);
                } 
                finally { 
                    DropSource.QueryContinueDrag -= new QueryContinueDragEventHandler(dropSourceBehavior.QueryContinueDrag);
                    DropSource.GiveFeedback -= new GiveFeedbackEventHandler(dropSourceBehavior.GiveFeedback); 
                    //If the drop gets cancelled, we won't get a OnDragDrop, so let's make sure that we stop
                    //processing drag notifications. Also VSWhidbey #354552 and 133339.
                    EndDragNotification();
                    validDragArgs = null; 
                    dragging = false;
                    cancelDrag = false; 
                    OnEndDrag(eventArgs); 
                }
            } 
            catch(CheckoutException cex) {
                if (cex == CheckoutException.Canceled) {
                    res = DragDropEffects.None;
                } 
                else {
                    throw; 
                } 
            }
            finally { 
                // VSWhidbey 306626 and 281813
                // It's possible we did not receive an EndDrag, and therefore
                // we weren't able to cleanup the drag.  We will do that here.
                // Scenarios where this happens: dragging from designer to recycle-bin, 
                // or over the taskbar.
                if (dropSourceBehavior != null) { 
                    dropSourceBehavior.CleanupDrag(); 
                }
            } 

            return res;
        }
 
        /// <devdoc>
        ///     Here, we reflect on all of our components to get all SnapLines 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        private void TestHook_GetAllSnapLines(ref Message m) { 

            string snapLineInfo = "";

            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost; 
            if (host == null) {
                return; 
            } 

            foreach (Component comp in host.Container.Components) { 
                if (!(comp is Control)) {
                    continue;
                }
 
                ControlDesigner designer = host.GetDesigner(comp) as ControlDesigner;
                if (designer != null) { 
                    foreach (SnapLine line in designer.SnapLines) { 
                        snapLineInfo += line.ToString() + "\tAssociated Control = " + designer.Control.Name + ":::";
                    } 

                }
            }
 
            TestHook_SetText(ref m, snapLineInfo);
        } 
 
        /// <devdoc>
        ///     Called by ControlDesigner when it receives a DragDrop 
        ///     message - we'll let the AdornerWindow know so it can
        ///     exit from 'drag mode'.
        /// </devdoc>
        internal void EndDragNotification() { 
            adornerWindow.EndDragNotification();
        } 
 
        /// <devdoc>
        ///     Called by our meucommand handling object, we will attempt to see 
        ///     if the appropriate hittested glyph's behavior wants to intercept and/or
        ///     modify this command in any way.
        /// </devdoc>
 
        private MenuCommand FindCommand(CommandID commandID, IMenuCommandService menuService) {
 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph); 

            if (behavior != null) { 
                //if the behavior wants all commands disabled..
                if (behavior.DisableAllCommands) {
                    MenuCommand menuCommand = menuService.FindCommand(commandID);
                    if (menuCommand != null) { 
                        menuCommand.Enabled = false;
                    } 
                    return menuCommand; 
                }
                //check to see if the behavior wants to interrupt this command 
                else {
                    MenuCommand menuCommand = behavior.FindCommand(commandID);
                    if (menuCommand != null) {
                        //the behavior chose to interrupt - so return the new command 
                        return menuCommand;
                    } 
                } 
            }
 
            return menuService.FindCommand(commandID);
        }

 
        /// <devdoc>
        ///     Pushes recent snaplines into the message structure. 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Performance", "CA1818:DoNotConcatenateStringsInsideLoops")]
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] 
        private void TestHook_GetRecentSnapLines(ref Message m) {

            string snapLineInfo = "";
 
            if (this.testHook_RecentSnapLines != null) {
                foreach(string line in this.testHook_RecentSnapLines) { 
                    snapLineInfo += line + "\n"; 
                }
            } 

            TestHook_SetText(ref m, snapLineInfo);
        }
 
        /// <devdoc>
        ///     Pushes the testhook string into the message structure 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")]
        private void TestHook_SetText(ref Message m, string text) { 

            if (m.LParam == IntPtr.Zero) {
                m.Result = (IntPtr)((text.Length + 1) * Marshal.SystemDefaultCharSize);
                return; 
            }
 
            if (unchecked((int)(long)m.WParam) < text.Length + 1) { 
                m.Result = (IntPtr)(-1);
                return; 
            }

            // Copy the name into the given IntPtr
            // 
            char[] nullChar = new char[] {(char)0};
            byte[] nullBytes; 
            byte[] bytes; 

            if (Marshal.SystemDefaultCharSize == 1) { 
                bytes = System.Text.Encoding.Default.GetBytes(text);
                nullBytes = System.Text.Encoding.Default.GetBytes(nullChar);
            }
            else { 
                bytes = System.Text.Encoding.Unicode.GetBytes(text);
                nullBytes = System.Text.Encoding.Unicode.GetBytes(nullChar); 
            } 

            Marshal.Copy(bytes, 0, m.LParam, bytes.Length); 
            Marshal.Copy(nullBytes, 0, unchecked((IntPtr)((long)m.LParam + (long)bytes.Length)), nullBytes.Length);
            m.Result = (IntPtr)((bytes.Length + nullBytes.Length)/Marshal.SystemDefaultCharSize);
        }
 

        /// <devdoc> 
        ///     This method defines the hueristic used to determine where a 
        ///     message is sent once the AdornerWindow intercepts it.  First, we'll
        ///     try to send it to the top-most Behavior on the stack.  Next, well 
        ///     send the message along to the supplied Glyph.  Finally, we'll
        ///     return null.
        /// </devdoc>
        private Behavior GetAppropriateBehavior(Glyph g) { 
            if (behaviorStack != null && behaviorStack.Count > 0) {
                return behaviorStack[0] as Behavior; 
            } 

            if (g != null && g.Behavior != null) { 
                return g.Behavior;
            }

            return null; 
        }
 
        /// <devdoc> 
        /// Given a behavior returns the behavior immediately
        /// after the behavior in the behaviorstack. 
        /// Can return null.
        /// </devdoc>
        public Behavior GetNextBehavior(Behavior behavior) {
            if (behaviorStack != null && behaviorStack.Count > 0) { 
                int index = behaviorStack.IndexOf(behavior);
                if ((index != -1) && (index < behaviorStack.Count - 1)) { 
                    return behaviorStack[index + 1] as Behavior; 
                }
            } 

            return null;
        }
 
        /// <devdoc>
        ///     Used by other designers (toolstrip designer for ex) this method will return 
        ///     the array of glyphs whose bounds intersect with the 'primaryGlyph' passed in. 
        /// </devdoc>
        internal Glyph[] GetIntersectingGlyphs(Glyph primaryGlyph) { 

            if (primaryGlyph == null) {
                Debug.Fail("The primary glyph cannot be null!");
                return new Glyph[0]; 
            }
 
            Rectangle primaryBounds = primaryGlyph.Bounds; 
            ArrayList intersectingGlyphs = new ArrayList();
 
            //loop through the glyphs in the same order as hit testing...
            for (int i = adorners.Count - 1; i >= 0; i--) {

                if (!adorners[i].Enabled) { 
                    continue;
                } 
 
                for (int j = 0; j < adorners[i].Glyphs.Count; j++) {
 
                    Glyph g = adorners[i].Glyphs[j];

                    if (primaryBounds.IntersectsWith(g.Bounds)) {
                        intersectingGlyphs.Add(g); 
                    }
                } 
 
            }
 
            if (intersectingGlyphs.Count == 0) {
                return new Glyph[0];
            }
 
            return (Glyph[])intersectingGlyphs.ToArray(typeof(Glyph));
        } 
 

        /// <devdoc> 
        ///     Called in response to a MouseMove message, we'll call TrackMouseEvents
        ///     to make sure we continue to get WM_HOVER messages so we can send them
        ///     to any relevant glyphs.
        /// </devdoc> 
        private void HookMouseEvent() {
            if (!trackingMouseEvent) { 
                trackingMouseEvent = true; 
                if (trackMouseEvent == null) {
                    trackMouseEvent = new NativeMethods.TRACKMOUSEEVENT(); 
                    trackMouseEvent.dwFlags = NativeMethods.TME_HOVER;
                    trackMouseEvent.hwndTrack = adornerWindow.Handle;
                }
                SafeNativeMethods.TrackMouseEvent(trackMouseEvent); 
            }
        } 
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Invalidate"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs.
        /// </devdoc>
        public void Invalidate() { 
            adornerWindow.InvalidateAdornerWindow();
        } 
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Invalidate2"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs.
        /// </devdoc>
        public void Invalidate(Rectangle rect) { 
            adornerWindow.InvalidateAdornerWindow(rect);
        } 
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Invalidate3"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs.
        /// </devdoc>
        public void Invalidate(Region r) { 
            adornerWindow.InvalidateAdornerWindow(r);
        } 
 
        /// <devdoc>
        ///     Called during hittesting, this method will send artificial 
        ///     enter/leave mouse events to the appropriate behavior.
        /// </devdoc>
        private void InvokeMouseEnterLeave(Glyph leaveGlyph, Glyph enterGlyph) {
            if (leaveGlyph != null) { 
                if (enterGlyph != null && leaveGlyph.Equals(enterGlyph)) {
                    //same glyph - no change 
                    return; 
                }
                if (validDragArgs != null) { 
                    OnDragLeave(leaveGlyph, EventArgs.Empty);
                }
                else {
                    OnMouseLeave(leaveGlyph); 
                }
            } 
 
            if (enterGlyph != null) {
                if (validDragArgs != null) { 
                    OnDragEnter(enterGlyph, validDragArgs);
                }
                else {
                    OnMouseEnter(enterGlyph); 
                }
            } 
 
        }
 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.SyncSelection"]/*' />
        /// <devdoc>
        ///     Synchronizes all selection glyphs. 
        /// </devdoc>
        public void SyncSelection() { 
            if (synchronizeEventHandler != null) { 
                synchronizeEventHandler(this, EventArgs.Empty);
            } 
        }

        private void OnSystemSettingChanged(object sender, EventArgs e) {
            SyncSelection(); 
            DesignerUtils.SyncBrushes();
        } 
 
        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
            SyncSelection(); 
            DesignerUtils.SyncBrushes();
        }

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.PopBehavior"]/*' /> 
        /// <devdoc>
        ///     Removes the behavior from the behavior stack 
        /// </devdoc> 
        public Behavior PopBehavior(Behavior behavior) {
            if (behaviorStack.Count == 0) { 
                throw new InvalidOperationException();
            }

            int index = behaviorStack.IndexOf(behavior); 
            if (index == -1) {
                Debug.Assert(false, "Could not find the behavior to pop - did it already get popped off? " + behavior.ToString()); 
                return null; 
            }
 
            behaviorStack.RemoveAt(index);

            if (behavior == captureBehavior) {
                adornerWindow.Capture = false; 

                // Defensive:  adornerWindow should get a WM_CAPTURECHANGED, 
                //             but do this by hand if it didn't. 
                if (captureBehavior != null) {
                    OnLoseCapture(); 
                    Debug.Assert(captureBehavior == null, "OnLostCapture should have cleared captureBehavior");
                }
            }
 
            return behavior;
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
            adornerWindow.Invalidate(paintRect); 
        }

        /// <devdoc>
        ///     Called in response to WM_NCHITTEST message intercepted by the transparent 
        ///     AdornerWindow.  The BehaviorService will cycle through all enabled
        ///     Adorners and pass this hitTest info along. 
        ///     Hit testing goes backwards through the list of adorners and forward 
        ///     through the list of glyphs... think about it...
        /// 
        ///     This returns true if it should let the default handling for the designer,
        ///     and will fill in which control was hit tested.
        /// </devdoc>
            private bool PropagateHitTest(Point pt) { 
            for (int i = adorners.Count - 1; i >= 0; i--) {
 
                if (!adorners[i].Enabled) { 
                    continue;
                } 

                for (int j = 0; j < adorners[i].Glyphs.Count; j++) {
                    Cursor hitTestCursor = adorners[i].Glyphs[j].GetHitTest(pt);
                    if (hitTestCursor != null) { 

                        // InvokeMouseEnterGlyph will cause the selection to change, which 
                        // might change the number of glyphs, so we need to remember the new glyph 
                        // before calling InvokeMouseEnterLeave. VSWhidbey #396611
                        Glyph newGlyph = adorners[i].Glyphs[j]; 

                        //with a valid hit test, fire enter/leave events
                        //
                        InvokeMouseEnterLeave(hitTestedGlyph, newGlyph); 
                        if (validDragArgs == null) {
                            //if we're not dragging, set the appropriate cursor 
                            // 
                            SetAppropriateCursor(hitTestCursor);
                        } 

                        hitTestedGlyph = newGlyph;

                        //return true if we hit on a transparentBehavior, otherwise false 
                        return (hitTestedGlyph.Behavior is ControlDesigner.TransparentBehavior);
                    } 
                } 

            } 

            InvokeMouseEnterLeave(hitTestedGlyph, null);
            if (validDragArgs == null) {
                Cursor cursor = Cursors.Default; 
                if ((behaviorStack != null) && (behaviorStack.Count > 0)) {
                    Behavior behavior = behaviorStack[0] as Behavior; 
                    if (behavior != null) { 
                        cursor = behavior.Cursor;
                    } 
                }
                SetAppropriateCursor(cursor);
            }
            hitTestedGlyph = null; 
            return true;//Returning false will cause the transparent window to return HTCLIENT when handling WM_NCHITTEST, thus blocking underline window to receive mouse events.
 
        } 

        /// <devdoc> 
        ///     Called in response to WM_PAINT message intercepted by the transparent
        ///     AdornerWindow.  The BehaviorService will cycle through all enabled
        ///     Adorners and pass this paint event along.
        ///     Painting should go forward through the list of adorners and 
        ///     backwards through each glyph... think about it...
        /// </devdoc> 
        private void PropagatePaint(PaintEventArgs pe) { 
            for (int i = 0; i < adorners.Count; i ++) {
                if (!adorners[i].Enabled) { 
                    continue;
                }
                for (int j = adorners[i].Glyphs.Count -1; j >= 0; j--) {
                    adorners[i].Glyphs[j].Paint(pe); 
                }
            } 
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.PushBehavior"]/*' /> 
        /// <devdoc>
        ///     Pushes a Behavior object onto the BehaviorStack.  This is often done through hit-tested
        ///     Glyph.
        /// </devdoc> 
        public void PushBehavior(Behavior behavior) {
            if (behavior == null) { 
                throw new ArgumentNullException("behavior"); 
            }
 

            // Should we catch this
            behaviorStack.Insert(0, behavior);
 
            // If there is a capture behavior, and it isn't this behavior,
            // notify it that it no longer has capture. 
            if (captureBehavior != null && captureBehavior != behavior) { 
                OnLoseCapture();
            } 

        }

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.PushCaptureBehavior"]/*' /> 
        /// <devdoc>
        ///     Pushes a Behavior object onto the BehaviorStack and assigns mouse capture to the behavior. 
        ///     This is often done through hit-tested Glyph.  If a behavior calls this the behavior's OnLoseCapture 
        ///     will be called if mouse capture is lost.
        /// </devdoc> 
        public void PushCaptureBehavior(Behavior behavior) {
            PushBehavior(behavior);
            captureBehavior = behavior;
            adornerWindow.Capture = true; 

            //VSWhidbey #373836. Since we are now capturing all mouse messages, we might miss some 
            //WM_MOUSEACTIVATE which would have activated the app. 
            //So if the DialogOwnerWindow (e.g. VS) is not the active window, let's activate it here.
            IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
            if(uiService != null) {
                IWin32Window hwnd = uiService.GetDialogOwnerWindow();
                if (hwnd != null && hwnd.Handle != IntPtr.Zero && hwnd.Handle != UnsafeNativeMethods.GetActiveWindow()) {
                    UnsafeNativeMethods.SetActiveWindow(new HandleRef(this, hwnd.Handle)); 
                }
            } 
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.ScreenToAdornerWindow"]/*' /> 
        /// <devdoc>
        ///     Translates a screen coord into a coord relative to the BehaviorService's AdornerWindow.
        /// </devdoc>
        public Point ScreenToAdornerWindow(Point p) { 
            NativeMethods.POINT offset = new NativeMethods.POINT();
            offset.x = p.X; 
            offset.y = p.Y; 
            NativeMethods.MapWindowPoints(IntPtr.Zero, adornerWindow.Handle, offset, 1);
            return new Point(offset.x, offset.y); 
        }

        /// <devdoc>
        ///     Called after a successful Glyph hittest.  This method will first 
        ///     request the Cursor from the toolbox.  If this fails, then the
        ///     Glyph's hittested cursor will be used. 
        /// </devdoc> 
        private void SetAppropriateCursor(Cursor cursor) {
 
            //default cursors will let the toolbox svc set a cursor if needed
            if (cursor == Cursors.Default) {
                if (toolboxSvc == null) {
                    toolboxSvc = (IToolboxService)serviceProvider.GetService(typeof(IToolboxService)); 
                }
 
                if (toolboxSvc != null && toolboxSvc.SetCursor()) { 
                    cursor = new Cursor(NativeMethods.GetCursor());
                } 
            }

            adornerWindow.Cursor = cursor;
        } 

        /// <devdoc> 
        ///     Displays the given exception to the user. 
        /// </devdoc>
        private void ShowError(Exception ex) { 
            IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService;
            if (uis != null) {
                uis.ShowError(ex);
            } 
        }
 
        /// <devdoc> 
        ///     Called by ControlDesigner when it receives a DragEnter
        ///     message - we'll let the AdornerWindow know so it can 
        ///     listen to all Mouse Messages for sending drag
        ///     notifcations.
        /// </devdoc>
        internal void StartDragNotification() { 
            adornerWindow.StartDragNotification();
        } 
 
        /// <devdoc>
        ///     Called in response to a MouseLeave message, we'll 
        ///     set state signalling that we are no longer tracking the
        ///     mouse event.  This is used for capturing MouseHover
        ///     notifications.
        /// </devdoc> 
        private void UnHookMouseEvent() {
            trackingMouseEvent = false; 
        } 

        /// <devdoc> 
        ///     Invokes the BeginDrag event for any listeners.
        /// </devdoc>
        private void OnBeginDrag(BehaviorDragDropEventArgs e) {
            if (beginDragHandler != null) { 
                beginDragHandler(this, e);
            } 
        } 

        /// <devdoc> 
        ///     Invokes the EndDrag event for any listeners.
        /// </devdoc>
        private void OnEndDrag(BehaviorDragDropEventArgs e) {
            if (endDragHandler != null) { 
                endDragHandler(this, e);
            } 
        } 

        /// <devdoc> 
        ///     Called by the adorner window when it loses capture.
        /// </devdoc>

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        internal void OnLoseCapture() {
            if (captureBehavior != null) { 
                Behavior b = captureBehavior; 
                captureBehavior = null;
                try { 
                    b.OnLoseCapture(hitTestedGlyph, EventArgs.Empty);
                }
                catch {
                } 
            }
        } 
 
        /// <devdoc>
        ///     Called when any MouseDown message enters the BehaviorService's 
        ///     AdornerWindow::WndProc().  From here, the message is sent to
        ///     the appropriate behavior.
        /// </devdoc>
        private bool OnMouseDoubleClick(MouseButtons button, Point mouseLoc) { 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph);
            if (behavior == null) { 
                return false; 
            }
 
            return behavior.OnMouseDoubleClick(hitTestedGlyph, button, mouseLoc);
        }

        /// <devdoc> 
        ///     Called when any MouseDown message enters the BehaviorService's
        ///     AdornerWindow::WndProc().  From here, the message is sent to 
        ///     the appropriate behavior. 
        /// </devdoc>
        private bool OnMouseDown(MouseButtons button, Point mouseLoc) { 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph);
            if (behavior == null) {
                return false;
            } 

            return behavior.OnMouseDown(hitTestedGlyph, button, mouseLoc); 
        } 

        /// <devdoc> 
        ///     Called when any MouseDown message enters the BehaviorService's
        ///     AdornerWindow::WndProc().  From here, the message is sent to
        ///     the appropriate behavior.
        /// </devdoc> 
        private bool OnMouseEnter(Glyph g) {
            Behavior behavior = GetAppropriateBehavior(g); 
            if (behavior == null) { 
                return false;
            } 
            return behavior.OnMouseEnter(g);
        }

        /// <devdoc> 
        ///     Called when the MouseHover message enters the BehaviorService's
        ///     AdornerWindow::WndProc().  From here, the message is sent to 
        ///     the appropriate behavior. 
        /// </devdoc>
        private bool OnMouseHover(Point mouseLoc) { 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph);
            if (behavior == null) {
                return false;
            } 

            return behavior.OnMouseHover(hitTestedGlyph, mouseLoc); 
        } 

        /// <devdoc> 
        ///     Called when any MouseDown message enters the BehaviorService's
        ///     AdornerWindow::WndProc().  From here, the message is sent to
        ///     the appropriate behavior.
        /// </devdoc> 
        private bool OnMouseLeave(Glyph g) {
            //stop tracking mouse events for MouseHover 
            UnHookMouseEvent(); 

            Behavior behavior = GetAppropriateBehavior(g); 
            if (behavior == null) {
                return false;
            }
            return behavior.OnMouseLeave(g); 
        }
 
        /// <devdoc> 
        ///     Called when any MouseMove message enters the BehaviorService's
        ///     AdornerWindow::WndProc().  From here, the message is sent to 
        ///     the appropriate behavior.
        /// </devdoc>
        private bool OnMouseMove(MouseButtons button, Point mouseLoc) {
            //hook mouse events (if we haven't already) for MouseHover 
            HookMouseEvent();
 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph); 
            if (behavior == null) {
                return false; 
            }

            return behavior.OnMouseMove(hitTestedGlyph, button, mouseLoc);
        } 

        /// <devdoc> 
        ///     Called when any MouseUp message enters the BehaviorService's 
        ///     AdornerWindow::WndProc().  From here, the message is sent to
        ///     the appropriate behavior. 
        /// </devdoc>
        private bool OnMouseUp(MouseButtons button) {
            dragEnterReplies.Clear();
            validDragArgs = null; 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph);
            if (behavior == null) { 
                return false; 
            }
 
            return behavior.OnMouseUp(hitTestedGlyph, button);
        }

        //OLEDragDrop messages... 

        /// <devdoc> 
        ///     Used to send this Drag/Drop event from the AdornerWindow 
        ///     to the appropriate Behavior (or hit tested Glyph).
        /// </devdoc> 
        private void OnDragDrop(DragEventArgs e) {
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "BS::OnDragDrop");
            validDragArgs = null;//be sure to null out our cached drag args
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph); 
            if (behavior == null) {
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tNo behavior. returning"); 
                return; 
            }
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tForwarding to behavior"); 
            behavior.OnDragDrop(hitTestedGlyph, e);
        }
        /// <devdoc>
        ///     Used to send this Drag/Drop event from the AdornerWindow 
        ///     to the appropriate Behavior (or hit tested Glyph).
        /// </devdoc> 
        private void OnDragEnter(Glyph g, DragEventArgs e) { 
            //if the AdornerWindow receives a drag message, this fn()
            //will be called w/o a glyph - so we'll assign the last 
            //hit tested one
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "BS::OnDragEnter");
            if (g == null) {
                g = hitTestedGlyph; 
            }
 
            Behavior behavior = GetAppropriateBehavior(g); 
            if (behavior == null) {
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tNo behavior, returning"); 
                return;
            }
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tForwarding to behavior");
            behavior.OnDragEnter(g, e); 

            if (g != null && g is ControlBodyGlyph && e.Effect == DragDropEffects.None) { 
                dragEnterReplies[g] = this; // dummy value, we just need to set something. 
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tCalled DragEnter on this glyph. Caching");
            } 
        }

        /// <devdoc>
        ///     Used to send this Drag/Drop event from the AdornerWindow 
        ///     to the appropriate Behavior (or hit tested Glyph).
        /// </devdoc> 
        private void OnDragLeave(Glyph g, EventArgs e) { 
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "BS::DragLeave");
 
            // This is normally cleared on OnMouseUp, but we might not get an OnMouseUp to clear it. VSWhidbey #474259
            // So let's make sure it is really cleared when we start the drag.
            dragEnterReplies.Clear();
 
            //if the AdornerWindow receives a drag message, this fn()
            //will be called w/o a glyph - so we'll assign the last 
            //hit tested one 
            if (g == null) {
                g = hitTestedGlyph; 
            }

            Behavior behavior = GetAppropriateBehavior(g);
            if (behavior == null) { 
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\t No behavior returning ");
                return; 
            } 
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tBehavior found calling OnDragLeave");
            behavior.OnDragLeave(g, e); 
        }

        /// <devdoc>
        ///     Used to send this Drag/Drop event from the AdornerWindow 
        ///     to the appropriate Behavior (or hit tested Glyph).
        /// </devdoc> 
        private void OnDragOver(DragEventArgs e) { 
            //cache off our validDragArgs so we can
            //re-fabricate enter/leave drag events 
            validDragArgs = e;

            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "BS::DragOver");
 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph);
            if (behavior == null) { 
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tNo behavior, exiting with DragDropEffects.None"); 
                e.Effect = DragDropEffects.None;
                return; 
            }
            if (hitTestedGlyph == null ||
               (hitTestedGlyph != null && !dragEnterReplies.ContainsKey(hitTestedGlyph))) {
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tFound glyph, forwarding to behavior"); 
                behavior.OnDragOver(hitTestedGlyph, e);
            } 
            else { 
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tFell through");
                e.Effect = DragDropEffects.None; 
            }
        }

        /// <devdoc> 
        ///     Used to send this Drag/Drop event from the AdornerWindow
        ///     to the appropriate Behavior (or hit tested Glyph). 
        /// </devdoc> 
        private void OnGiveFeedback(GiveFeedbackEventArgs e) {
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph); 
            if (behavior == null) {
                return;
            }
 
            behavior.OnGiveFeedback(hitTestedGlyph, e);
        } 
 
        /// <devdoc>
        ///     Used to send this Drag/Drop event from the AdornerWindow 
        ///     to the appropriate Behavior (or hit tested Glyph).
        /// </devdoc>
        private void OnQueryContinueDrag(QueryContinueDragEventArgs e) {
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph); 
            if (behavior == null) {
                return; 
            } 

            behavior.OnQueryContinueDrag(hitTestedGlyph, e); 
        }


        /// <devdoc> 
        ///     The AdornerWindow is a transparent window that resides ontop of the
        ///     Designer's Frame.  This window is used by the BehaviorService to 
        ///     intercept all messages.  It also serves as a unified canvas on which 
        ///     to paint Glyphs.
        /// </devdoc> 
        private class AdornerWindow : Control {

            private BehaviorService         behaviorService;//ptr back to BehaviorService
            private Control                 designerFrame;//the designer's frame 
            private MouseHook               mouseHook;
 
            /// <devdoc> 
            ///     Constructor that parents itself to the Designer Frame and hooks all
            ///     necessary events. 
            /// </devdoc>
            [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
            internal AdornerWindow(BehaviorService behaviorService, Control designerFrame) {
                this.behaviorService = behaviorService; 
                this.designerFrame = designerFrame;
                this.Dock = DockStyle.Fill; 
                this.AllowDrop = true; 
                this.Text = "AdornerWindow";
 
                SetStyle(ControlStyles.Opaque,true);

                designerFrame.HandleDestroyed += new EventHandler(OnDesignerFrameHandleDestroyed);
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
                if (mouseHook == null) {
                    mouseHook = new MouseHook(this, designerFrame); 
                }
                mouseHook.HookMouseMessages = true;
            }
 
            /// <devdoc>
            ///     Unhook and null out our mouseHook. 
            /// </devdoc> 
            protected override void OnHandleDestroyed(EventArgs e) {
                if (mouseHook != null) { 
                    mouseHook.HookMouseMessages = false;
                    mouseHook = null;
                }
                base.OnHandleDestroyed(e); 
            }
 
            /// <devdoc> 
            ///     Null out our mouseHook and unhook any events.
            /// </devdoc> 
            protected override void Dispose(bool disposing) {
                if (disposing) {
                    if (mouseHook != null) {
                        mouseHook.Dispose(); 
                        mouseHook = null;
                    } 
 
                    if (designerFrame != null) {
                        designerFrame.HandleDestroyed -= new EventHandler(OnDesignerFrameHandleDestroyed); 
                        designerFrame = null;
                    }

                } 
                base.Dispose(disposing);
            } 
 
            /// <devdoc>
            ///     Returns the display rectangle for the adorner window 
            /// </devdoc>
            internal  Rectangle DesignerFrameDisplayRectangle {
                get {
                    if(DesignerFrameValid) { 
                        return ((DesignerFrame)designerFrame).DisplayRectangle;
                    } else { 
                        return Rectangle.Empty; 
                    }
                } 
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
            ///     Ultimately called by ControlDesigner when it receives a DragDrop 
            ///     message - here, we'll exit from 'drag mode'.
            /// </devdoc> 
            internal void EndDragNotification() {
                this.mouseHook.ProcessingDrag = false;
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
                    //translate for non-zero scroll positions 
                    Point scrollPosition = ((DesignerFrame)designerFrame).AutoScrollPosition; 
                    region.Translate(scrollPosition.X, scrollPosition.Y);
 
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
                    //translate for non-zero scroll positions 
                    Point scrollPosition = ((DesignerFrame)designerFrame).AutoScrollPosition;
                    rectangle.Offset(scrollPosition.X, scrollPosition.Y); 
 
                    designerFrame.Invalidate(rectangle, true);
                    designerFrame.Update(); 
                }

            }
 
            private void OnDesignerFrameHandleDestroyed(object s, EventArgs e) {
                if (mouseHook != null) { 
                    mouseHook.Dispose(); 
                    mouseHook = null;
                } 
            }

            /// <devdoc>
            ///     The AdornerWindow hooks all Drag/Drop notification so 
            ///     they can be forwarded to the appropriate Behavior via
            ///     the BehaviorService. 
            /// </devdoc> 
            protected override void OnDragDrop(DragEventArgs e) {
                try { 
                    behaviorService.OnDragDrop(e);
                }
                finally {
                    this.mouseHook.ProcessingDrag = false; 
                }
            } 
 
            private static bool IsLocalDrag(DragEventArgs e) {
                if (e.Data is DropSourceBehavior.BehaviorDataObject) { 
                    return true;
                }
                else {
                    // Gets all the data formats and data conversion formats in the data object. 
                    String[] allFormats = e.Data.GetFormats();
 
                    for (int i = 0; i < allFormats.Length; i++) { 
                        if (allFormats[i].Length == ToolboxFormat.Length &&
                            string.Equals(ToolboxFormat, allFormats[i])) { 
                            return true;
                        }
                    }
                } 
                return false;
            } 
 

            /// <devdoc> 
            ///     The AdornerWindow hooks all Drag/Drop notification so
            ///     they can be forwarded to the appropriate Behavior via
            ///     the BehaviorService.
            /// </devdoc> 
            protected override void OnDragEnter(DragEventArgs e) {
                this.mouseHook.ProcessingDrag = true; 
 
                // determine if this is a local drag, if it is, do normal processing
                // otherwise, force a PropagateHitTest.  We need to force 
                // this because the OLE D&D service suspends mouse messages
                // when the drag is not local so the mouse hook never sees them.
                if (!IsLocalDrag(e)) {
                    behaviorService.validDragArgs = e; 
                    NativeMethods.POINT pt = new NativeMethods.POINT();
                    NativeMethods.GetCursorPos(pt); 
                    NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, pt, 1); 
                    Point mousePos = new Point(pt.x, pt.y);
                    behaviorService.PropagateHitTest(mousePos); 

                }
                behaviorService.OnDragEnter(null, e);
            } 

            /// <devdoc> 
            ///     The AdornerWindow hooks all Drag/Drop notification so 
            ///     they can be forwarded to the appropriate Behavior via
            ///     the BehaviorService. 
            /// </devdoc>
            protected override void OnDragLeave(EventArgs e) {
                //set our dragArgs to null so we know not to send
                //drag enter/leave events when we re-enter the 
                //dragging area
                behaviorService.validDragArgs = null; 
 
                try {
                    behaviorService.OnDragLeave(null, e); 
                }
                finally {
                    this.mouseHook.ProcessingDrag = false;
                } 
            }
 
            /// <devdoc> 
            ///     The AdornerWindow hooks all Drag/Drop notification so
            ///     they can be forwarded to the appropriate Behavior via 
            ///     the BehaviorService.
            /// </devdoc>
            protected override void OnDragOver(DragEventArgs e) {
                this.mouseHook.ProcessingDrag = true; 
                if (!IsLocalDrag(e)) {
                    behaviorService.validDragArgs = e; 
                    NativeMethods.POINT pt = new NativeMethods.POINT(); 
                    NativeMethods.GetCursorPos(pt);
                    NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, pt, 1); 
                    Point mousePos = new Point(pt.x, pt.y);
                    behaviorService.PropagateHitTest(mousePos);
                }
 
                behaviorService.OnDragOver(e);
            } 
 
            /// <devdoc>
            ///     The AdornerWindow hooks all Drag/Drop notification so 
            ///     they can be forwarded to the appropriate Behavior via
            ///     the BehaviorService.
            /// </devdoc>
            protected override void OnGiveFeedback(GiveFeedbackEventArgs e) { 
                behaviorService.OnGiveFeedback(e);
            } 
 
            /// <devdoc>
            ///     The AdornerWindow hooks all Drag/Drop notification so 
            ///     they can be forwarded to the appropriate Behavior via
            ///     the BehaviorService.
            /// </devdoc>
            protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e) { 
                behaviorService.OnQueryContinueDrag(e);
            } 
 
            /// <devdoc>
            ///     Called by ControlDesigner when it receives a DragEnter 
            ///     message - we'll let listen to all Mouse Messages
            ///     so we can send drag notifcations.
            /// </devdoc>
            internal void StartDragNotification() { 
                this.mouseHook.ProcessingDrag = true;
            } 
 
            /// <devdoc>
            ///     The AdornerWindow intercepts all designer-related messages and forwards them 
            ///     to the BehaviorService for appropriate actions.  Note that Paint and HitTest
            ///     messages are correctly parsed and translated to AdornerWindow coords.
            /// </devdoc>
            protected override void WndProc(ref Message m) 
            {
                //special test hooks 
                if (m.Msg == BehaviorService.WM_GETALLSNAPLINES) { 
                    behaviorService.TestHook_GetAllSnapLines(ref m);
                } 
                else if(m.Msg == BehaviorService.WM_GETRECENTSNAPLINES) {
                    behaviorService.TestHook_GetRecentSnapLines(ref m);
                }
 
                switch(m.Msg)
                { 
                    case NativeMethods.WM_PAINT: 
                        //
                        // Stash off the region we have to update 
                        //
                        IntPtr hrgn = NativeMethods.CreateRectRgn(0, 0, 0, 0);
                        NativeMethods.GetUpdateRgn(m.HWnd, hrgn, true);
 
                        //
                        // The region we have to update in terms of the smallest rectangle 
                        // that completely encloses the update region of the window gives 
                        // us the clip rectangle
                        // 
                        NativeMethods.RECT clip = new NativeMethods.RECT();
                        NativeMethods.GetUpdateRect(m.HWnd, ref clip, true);
                        Rectangle paintRect = new Rectangle(clip.left, clip.top, clip.right - clip.left, clip.bottom - clip.top);
 
                        try {
                            using (Region r = Region.FromHrgn(hrgn)) { 
                                // 
                                // Call the base class to do its painting.
                                // 
                                DefWndProc(ref m);

                                //
                                // Now do our own painting. 
                                //
                                using (Graphics g = Graphics.FromHwnd(m.HWnd)) { 
                                    using (PaintEventArgs pevent = new PaintEventArgs(g, paintRect)) { 
                                        g.Clip = r;
                                        behaviorService.PropagatePaint(pevent); 
                                    }
                                }
                            }
                        } 
                        finally {
                            NativeMethods.DeleteObject(hrgn); 
                        } 
                        break;
 
                    case NativeMethods.WM_NCHITTEST:
                        Point pt = new Point((short)NativeMethods.Util.LOWORD((int)m.LParam),
                                             (short)NativeMethods.Util.HIWORD((int)m.LParam));
                        NativeMethods.POINT pt1 = new NativeMethods.POINT(); 
                        pt1.x = 0;
                        pt1.y = 0; 
                        NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, pt1, 1); 
                        pt.Offset(pt1.x, pt1.y);
                        if (behaviorService.PropagateHitTest(pt) && !mouseHook.ProcessingDrag) { 
                            m.Result = (IntPtr)(NativeMethods.HTTRANSPARENT);
                        }
                        else {
                            m.Result = (IntPtr)(NativeMethods.HTCLIENT); 
                        }
                        break; 
 
                    case NativeMethods.WM_CAPTURECHANGED:
                        base.WndProc(ref m); 
                        behaviorService.OnLoseCapture();
                        break;

                    default: 
                        base.WndProc(ref m);
                        break; 
                } 
            }
 
            /// <devdoc>
            ///     Called by our mouseHook when it spies a mouse message that the adornerWindow
            ///     would be interested in.  Returning 'true' signifies that the message was processed
            ///     and should not continue to child windows. 
            /// </devdoc>
            private bool WndProcProxy(ref Message m, int x, int y) { 
 
                Point mouseLoc = new Point(x,y);
                behaviorService.PropagateHitTest(mouseLoc); 

                switch (m.Msg) {
                    case NativeMethods.WM_LBUTTONDOWN:
                        if (behaviorService.OnMouseDown(MouseButtons.Left, mouseLoc)) { 
                            return false;
                        } 
                        break; 

                    case NativeMethods.WM_RBUTTONDOWN: 
                        if (behaviorService.OnMouseDown(MouseButtons.Right, mouseLoc)) {
                            return false;
                        }
                        break; 

                    case NativeMethods.WM_MOUSEMOVE: 
                        if (behaviorService.OnMouseMove(Control.MouseButtons, mouseLoc)) { 
                            return false;
                        } 
                        break;

                    case NativeMethods.WM_LBUTTONUP:
                        if (behaviorService.OnMouseUp(MouseButtons.Left)) { 
                            return false;
                        } 
                        break; 

                    case NativeMethods.WM_RBUTTONUP: 
                        if (behaviorService.OnMouseUp(MouseButtons.Right)) {
                            return false;
                        }
                        break; 

                    case NativeMethods.WM_MOUSEHOVER: 
                        if (behaviorService.OnMouseHover(mouseLoc)) { 
                            return false;
                        } 
                        break;

                    case NativeMethods.WM_LBUTTONDBLCLK:
                        if (behaviorService.OnMouseDoubleClick(MouseButtons.Left, mouseLoc)) { 
                            return false;
                        } 
                        break; 

                    case NativeMethods.WM_RBUTTONDBLCLK: 
                        if (behaviorService.OnMouseDoubleClick(MouseButtons.Right, mouseLoc)) {
                            return false;
                        }
                        break; 
                }
                return true; 
            } 

            /// <include file='doc\BehaviorService.AdornerWidnow.MouseHook.uex' path='docs/doc[@for="BehaviorService.AdornerWidnow.MouseHook"]/*' /> 
            /// <devdoc>
            ///     This class knows how to hook all the messages to a given process/thread.  On any mouse clicks, it asks the designer what to do with the message,
            ///     that is to eat it or propogate it to the control it was meant for.   This allows us to synchrounously process mouse messages when
            ///     the AdornerWindow itself may be pumping messages. 
            /// </devdoc>
            /// <internalonly/> 
            [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")] 
            private class MouseHook {
 
                private AdornerWindow   adornerWindow;
                private Control         designerFrame;
                private int             thisProcessID = 0;
                private GCHandle        mouseHookRoot; 
                [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
                private IntPtr          mouseHookHandle = IntPtr.Zero; 
                private bool            processingMessage; 
                private bool            processingDrag;
 
                private bool            isHooked = false; //VSWHIDBEY # 474112
                private int             lastLButtonDownTimeStamp;

                public MouseHook(AdornerWindow aw, Control df) { 
                   this.designerFrame = df;
                   this.adornerWindow = aw; 
                   #if DEBUG 
                   callingStack = Environment.StackTrace;
                   #endif 
                }

                #if DEBUG
                string callingStack; 
                ~MouseHook() {
                    Debug.Assert(mouseHookHandle == IntPtr.Zero, "Finalizing an active mouse hook.  This will crash the process.  Calling stack: " + callingStack); 
                } 
                #endif
 
                public virtual bool HookMouseMessages {
                    get{
                        return mouseHookHandle != IntPtr.Zero;
                    } 
                    set{
                        if (value) { 
                            HookMouse(); 
                        }
                        else { 
                            UnhookMouse();
                        }
                    }
                } 

                public bool ProcessingDrag { 
                    get { 
                        return processingDrag;
                    } 
                    set {
                        processingDrag = value;
                    }
                } 

                public void Dispose() { 
                   UnhookMouse(); 
                }
 
                private void HookMouse() {
                    lock(this) {
                        if (mouseHookHandle != IntPtr.Zero) {
                            return; 
                        }
 
                        if (thisProcessID == 0) { 
                            UnsafeNativeMethods.GetWindowThreadProcessId(new HandleRef(adornerWindow, adornerWindow.Handle), out thisProcessID);
                        } 


                        UnsafeNativeMethods.HookProc hook = new UnsafeNativeMethods.HookProc(this.MouseHookProc);
                        mouseHookRoot = GCHandle.Alloc(hook); 

#pragma warning disable 618 
                        mouseHookHandle = UnsafeNativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE, 
                                                                   hook,
                                                                   new HandleRef(null, IntPtr.Zero), 
                                                                   AppDomain.GetCurrentThreadId());
#pragma warning restore 618
                        if (mouseHookHandle != IntPtr.Zero) {
                            isHooked = true; 
                        }
                        Debug.Assert(mouseHookHandle != IntPtr.Zero, "Failed to install mouse hook"); 
                    } 
                }
 
                [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
                [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
                private unsafe IntPtr MouseHookProc(int nCode, IntPtr wparam, IntPtr lparam) {
                    if (isHooked && nCode == NativeMethods.HC_ACTION) { 
                        NativeMethods.MOUSEHOOKSTRUCT* mhs = (NativeMethods.MOUSEHOOKSTRUCT*)lparam;
                        if (mhs != null) { 
                            try { 
                                if (ProcessMouseMessage(mhs->hWnd, (int)wparam, mhs->pt_x, mhs->pt_y)) {
                                    return (IntPtr)1; 
                                }
                            }
                            catch (Exception ex) {
                                adornerWindow.Capture = false; 
                                if (ex != CheckoutException.Canceled) {
                                    adornerWindow.behaviorService.ShowError(ex); 
                                } 
                                if (ClientUtils.IsCriticalException(ex)) {
                                    throw; 
                                }
                            }
                        }
                    } 

                    Debug.Assert(isHooked, "How did we get here when we are diposed?"); 
 
                    return UnsafeNativeMethods.CallNextHookEx(new HandleRef(this, mouseHookHandle), nCode, wparam, lparam);
                } 

                private void UnhookMouse() {
                    lock(this) {
                        if (mouseHookHandle != IntPtr.Zero) { 
                            UnsafeNativeMethods.UnhookWindowsHookEx(new HandleRef(this, mouseHookHandle));
                            mouseHookRoot.Free(); 
                            mouseHookHandle = IntPtr.Zero; 
                            isHooked = false;
                        } 
                    }
                }

                 /* 
                * Here is where we force validation on any clicks outside the
                */ 
                private bool ProcessMouseMessage(IntPtr hWnd, int msg, int x, int y) { 

                    if (processingMessage) { 
                      return false;
                    }

                    // We could have hooked a control in a semitrust web page.  This would put 
                    // semitrust frames above us, which could cause this to fail.
                    // SECREVIEW, 
 

                    new NamedPermissionSet("FullTrust").Assert(); 

                    IntPtr handle = designerFrame.Handle;

                    // if it's us or one of our children, just process as normal 
                    //
                    if (processingDrag || (hWnd != handle && SafeNativeMethods.IsChild(new HandleRef(this, handle), new HandleRef(this, hWnd)))) { 
                        Debug.Assert(thisProcessID != 0, "Didn't get our process id!"); 

                        // make sure the window is in our process 
                        int pid;
                        UnsafeNativeMethods.GetWindowThreadProcessId(new HandleRef(null, hWnd), out pid);

                        // if this isn't our process, bail 
                        if (pid != thisProcessID) {
                            return false; 
                        } 

                        try { 
                           processingMessage = true;

                           NativeMethods.POINT pt = new NativeMethods.POINT();
                           pt.x = x; 
                           pt.y = y;
                           NativeMethods.MapWindowPoints(IntPtr.Zero, adornerWindow.Handle, pt, 1); 
                           Message m = Message.Create(hWnd, msg, (IntPtr)0, (IntPtr)MAKELONG(pt.y, pt.x)); 

                           // DevDiv Bugs 79616, No one knows why we get an extra click here from VS. 
                           // As a workaround, we check the TimeStamp and discard it.
                           if (m.Msg == NativeMethods.WM_LBUTTONDOWN)
                           {
                               lastLButtonDownTimeStamp = UnsafeNativeMethods.GetMessageTime(); 
                           }
                           else if (m.Msg == NativeMethods.WM_LBUTTONDBLCLK) 
                           { 
                               int lButtonDoubleClickTimeStamp = UnsafeNativeMethods.GetMessageTime();
                               if (lButtonDoubleClickTimeStamp == lastLButtonDownTimeStamp) 
                               {
                                   return true;
                               }
                           } 

                           if (!adornerWindow.WndProcProxy(ref m, pt.x, pt.y)) { 
                               // we did the work, stop the message propogation 
                               return true;
                           } 

                        }
                        finally {
                           processingMessage = false; 
                        }
                    } 
 
                    return false;
                } 


                public static int MAKELONG(int low, int high) {
                    return (high << 16) | (low & 0xffff); 
                }
            } 
        } 

        /// <devdoc> 
        ///     This class is used to notifiy the BehaviorService when a 'FindCommand'
        ///     call on the ImenuCommandService has fired.  When this happens the
        ///     BehaviorService will ask the appropriate glyph's behavior if it intends
        ///     to interrupt the processing of the command. 
        /// </devdoc>
        private class MenuCommandHandler : IMenuCommandService { 
 
            private BehaviorService         owner;//ptr back to the behavior service
            private IMenuCommandService     menuService;//core service used for most implementations of the IMCS interface 
            private Stack<CommandID>        currentCommands = new Stack<CommandID>();

            /// <devdoc>
            ///     Cache off the behsvc and the menucommand service... 
            /// </devdoc>
            public MenuCommandHandler(BehaviorService owner, IMenuCommandService menuService) { 
                this.owner = owner; 
                this.menuService = menuService;
            } 
	
            /// <devdoc>
            ///     get menucommand service
            /// </devdoc> 
            public IMenuCommandService MenuService {
		        get{ 
                    return menuService; 
                }
            } 

 	
            /// <devdoc>
            ///     Just call straight through to the IMCS 
            /// </devdoc>
            void IMenuCommandService.AddCommand(MenuCommand command) 
            { 
                menuService.AddCommand(command);
            } 

            /// <devdoc>
            ///     Just call straight through to the IMCS
            /// </devdoc> 
            void IMenuCommandService.RemoveVerb(DesignerVerb verb)
            { 
                menuService.RemoveVerb(verb); 
            }
 
            /// <devdoc>
            ///     Just call straight through to the IMCS
            /// </devdoc>
            void IMenuCommandService.RemoveCommand(MenuCommand command) 
            {
                menuService.RemoveCommand(command); 
            } 

            /// <devdoc> 
            ///     Give the behavior service (specifically: the hittestedglyph's behavior)
            ///     a chance to interrupt this command.
            /// </devdoc>
            MenuCommand IMenuCommandService.FindCommand(CommandID commandID) 
            {
 
                try { 

                    if (currentCommands.Contains(commandID)) { 
                        return null;
                    }

                    currentCommands.Push(commandID); 

                    return owner.FindCommand(commandID, menuService); 
                } 
                finally {
                    currentCommands.Pop(); 
                }
            }

            /// <devdoc> 
            ///     Just call straight through to the IMCS
            /// </devdoc> 
            bool IMenuCommandService.GlobalInvoke(CommandID commandID) 
            {
                return menuService.GlobalInvoke(commandID); 
            }

            /// <devdoc>
            ///     Just call straight through to the IMCS 
            /// </devdoc>
            void IMenuCommandService.ShowContextMenu(CommandID menuID, int x, int y) 
            { 
                menuService.ShowContextMenu(menuID,x, y);
            } 

            /// <devdoc>
            ///     Just call straight through to the IMCS
            /// </devdoc> 
            void IMenuCommandService.AddVerb(DesignerVerb verb)
            { 
                menuService.AddVerb(verb); 
            }
 
            /// <devdoc>
            ///     Just call straight through to the IMCS
            /// </devdoc>
            DesignerVerbCollection IMenuCommandService.Verbs 
            {
                get { 
                    return menuService.Verbs; 
                }
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing; 
    using System.Drawing.Design;
    using System.ComponentModel.Design.Serialization;
    using System.Security;
    using System.Security.Permissions; 
    using System.Windows.Forms.Design;
    using System.Runtime.InteropServices; 
    using System.Globalization; 
    using Microsoft.Win32;
 
    /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService"]/*' />
    /// <devdoc>
    ///     The BehaviorService essentially manages all things UI in the designer.
    ///     When the BehaviorService is created it adds a transparent window over the 
    ///     designer frame.  The BehaviorService can then use this window to render UI
    ///     elements (called Glyphs) as well as catch all mouse messages.  By doing 
    ///     so - the BehaviorService can control designer behavior.  The BehaviorService 
    ///     supports a BehaviorStack.  'Behavior' objects can be pushed onto this stack.
    ///     When a message is intercepted via the transparent window, the BehaviorService 
    ///     can send the message to the Behavior at the top of the stack.  This allows
    ///     for different UI modes depending on the currently pushed Behavior.  The
    ///     BehaviorService is used to render all 'Glyphs': selection borders, grab handles,
    ///     smart tags etc... as well as control many of the design-time behaviors: dragging, 
    ///     selection, snap lines, etc...
    /// </devdoc> 
    public sealed class BehaviorService : IDisposable { 

        private IServiceProvider                serviceProvider;        //standard service provider 
        private AdornerWindow                   adornerWindow;          //the transparent window all glyphs are drawn to
        private BehaviorServiceAdornerCollection               adorners;               //we manage all adorners (glyph-containers) here
        private ArrayList                       behaviorStack;          //the stack behavior objects can be pushed to and popped from
        private Behavior                        captureBehavior;        //the behavior that currently has capture; may be null 
        private Glyph                           hitTestedGlyph;         //the last valid glyph that was hit tested
        private IToolboxService                 toolboxSvc;             //allows us to have the toolbox choose a cursor 
        private Control                         dropSource;             //actual control used to call .dodragdrop 
        private DragEventArgs                   validDragArgs;          //if valid - this is used to fabricate drag enter/leave envents
        private BehaviorDragDropEventHandler    beginDragHandler;       //fired directly before we call .DoDragDrop() 
        private BehaviorDragDropEventHandler    endDragHandler;         //fired directly after we call .DoDragDrop()
        private EventHandler                    synchronizeEventHandler;    //fired when we want to synchronize the selection
        private NativeMethods.TRACKMOUSEEVENT   trackMouseEvent;        //demand created (once) used to track the mouse hover event
        private bool                            trackingMouseEvent;     //state identifying current mouse tracking 
        private string[]                        testHook_RecentSnapLines; //we keep track of the last snaplines we found - for testing purposes
        private MenuCommandHandler              menuCommandHandler;     //private object that handles all menu commands 
        private bool                            useSnapLines;           //indicates if this designer session is using snaplines or snapping to a grid 
        private bool                            queriedSnapLines;       //only query for this once since we require the user restart design sessions when this changes
 
        private Hashtable                       dragEnterReplies;       // we keep track of whether glyph has already responded to a DragEnter this D&D.

 		private static TraceSwitch              dragDropSwitch = new TraceSwitch("BSDRAGDROP", "Behavior service drag & drop messages");
 
        private bool                            dragging = false;        // are we in a drag
        private bool                            cancelDrag = false;     //  should we cancel the drag on the next QueryContinueDrag 
 
        private int adornerWindowIndex = -1;
 
        //test hooks for SnapLines
        private static int WM_GETALLSNAPLINES;
        private static int WM_GETRECENTSNAPLINES;
 
        private DesignerActionUI                  actionPointer;//pointer to the designer action service so we can supply mouse over notifications
 
        private const string ToolboxFormat = ".NET Toolbox Item"; // used to detect if a drag is coming from the toolbox. 

        /// <devdoc> 
        ///     This constructor is called from DocumentDesigner's Initialize method.
        /// </devdoc>
        [SuppressMessage("Microsoft.Performance", "CA1805:DoNotInitializeUnnecessarily")]
        internal BehaviorService(IServiceProvider serviceProvider, Control windowFrame) { 
            this.serviceProvider = serviceProvider;
 
            //create the AdornerWindow 
            adornerWindow = new AdornerWindow(this, windowFrame);
 
            //use the adornerWindow as an overlay
            IOverlayService os = (IOverlayService)serviceProvider.GetService(typeof(IOverlayService));
            if (os != null) {
                adornerWindowIndex = os.PushOverlay(adornerWindow); 
            }
 
            dragEnterReplies = new Hashtable(); 

            //start with an empty adorner collection & no behavior on the stack 
            adorners = new BehaviorServiceAdornerCollection(this);
            behaviorStack = new ArrayList();

            hitTestedGlyph = null; 
            validDragArgs = null;
            actionPointer = null; 
            trackMouseEvent = null; 
            trackingMouseEvent = false;
 
            //create out object that will handle all menucommands
            IMenuCommandService menuCommandService = serviceProvider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
 
            if (menuCommandService != null && host != null) {
 
                menuCommandHandler = new MenuCommandHandler(this, menuCommandService); 

                host.RemoveService(typeof(IMenuCommandService)); 
                host.AddService(typeof(IMenuCommandService), menuCommandHandler);
            }

            //default layoutmode is SnapToGrid. 
            useSnapLines = false;
            queriedSnapLines = false; 
 
            //test hooks
            WM_GETALLSNAPLINES = SafeNativeMethods.RegisterWindowMessage("WM_GETALLSNAPLINES"); 
            WM_GETRECENTSNAPLINES = SafeNativeMethods.RegisterWindowMessage("WM_GETRECENTSNAPLINES");

            // Listen to the SystemEvents so that we can resync selection based on display settings etc.
            SystemEvents.DisplaySettingsChanged += new EventHandler(this.OnSystemSettingChanged); 
            SystemEvents.InstalledFontsChanged += new EventHandler(this.OnSystemSettingChanged);
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(this.OnUserPreferenceChanged); 
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Adorners"]/*' /> 
        /// <devdoc>
        ///     Read-only property that returns the AdornerCollection that the BehaivorService manages.
        /// </devdoc>
        public BehaviorServiceAdornerCollection Adorners { 
            get {
                return adorners; 
            } 
        }
 
        /// <devdoc>
        ///     Returns the index of  the transparent AdornerWindow.
        /// </devdoc>
        internal int AdornerWindowIndex { 
            get
            { 
                return adornerWindowIndex; 
            }
        } 

        /// <devdoc>
        ///     Returns the actual Control that represents the transparent AdornerWindow.
        /// </devdoc> 
        internal Control AdornerWindowControl {
            get { 
                return adornerWindow; 
            }
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.AdornerWindowGraphics"]/*' />
        /// <devdoc>
        ///     Creates and returns a Graphics object for the AdornerWindow 
        /// </devdoc>
        public Graphics AdornerWindowGraphics { 
            get { 
                Graphics result = adornerWindow.CreateGraphics();
                result.Clip = new Region(adornerWindow.DesignerFrameDisplayRectangle); 
                return result;
            }
        }
 
        public Behavior CurrentBehavior {
            get { 
                if(behaviorStack != null && behaviorStack.Count > 0) { 
                    return (behaviorStack[0] as Behavior);
                } 
                else {
                    return null;
                }
            } 
        }
 
        /// <devdoc> 
        ///     If the drag operation should be cancelled (say we get a WM_CANCELMODE), set
        ///     CancelDrag to true. The next time QueryContinueDrag is called, the drag operation 
        ///     will be cancelled.
        /// </devdoc>
        internal bool CancelDrag {
            get { 
                return cancelDrag;
            } 
 
            set {
                cancelDrag = value; 
            }
        }

        /// <devdoc> 
        //  This value will be set by the DocumentDesigner.  'actionpointer' will be called
        //  when we get a mouse enter of a new component glyph.  This is so the Designer 
        //  Actions can 'hover-active' if needed. 
        /// </devdoc>
        internal DesignerActionUI DesignerActionUI { 
            get {
                return actionPointer;
            }
            set { 
                actionPointer = value;
            } 
        } 

        internal bool Dragging { 
            get{
                return dragging;
            }
        } 

 
        internal bool HasCapture { 
            get {
                return captureBehavior != null; 
            }
        }

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.LayoutMode"]/*' /> 
        /// <devdoc>
        ///     Returns the LayoutMode setting of the current designer session.  Either 
        ///     SnapLines or SnapToGrid. 
        /// </devdoc>
        internal bool UseSnapLines { 
            get {
                //we only check for this service/value once since we require the
                //user to re-open the designer session after these types of option
                //have been modified 
                if (!queriedSnapLines) {
                    queriedSnapLines = true; 
                    useSnapLines = DesignerUtils.UseSnapLines(serviceProvider); 
                }
 
                return useSnapLines;
            }
        }
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.AdornerWindowPointToScreen"]/*' />
        /// <devdoc> 
        ///     Translates a point in the AdornerWindow to screen coords. 
        /// </devdoc>
        public Point AdornerWindowPointToScreen(Point p) { 
            NativeMethods.POINT offset = new NativeMethods.POINT(p.X, p.Y);
            NativeMethods.MapWindowPoints(adornerWindow.Handle, IntPtr.Zero, offset, 1);
            return new Point(offset.x, offset.y);
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.AdornerWindowToScreen"]/*' /> 
        /// <devdoc> 
        ///     Gets the location (upper-left corner) of the AdornerWindow in screen coords.
        /// </devdoc> 
        public Point AdornerWindowToScreen() {
            Point origin = new Point(0, 0);
            return AdornerWindowPointToScreen(origin);
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.ControlToAdornerWindow"]/*' /> 
        /// <devdoc> 
        /// Returns the location of a Control translated to AdornerWindow coords.
        /// </devdoc> 
        public Point ControlToAdornerWindow(Control c) {
            if (c.Parent == null) {
                return Point.Empty;
            } 

            NativeMethods.POINT pt = new NativeMethods.POINT(); 
            pt.x = c.Left; 
            pt.y = c.Top;
            NativeMethods.MapWindowPoints(c.Parent.Handle, adornerWindow.Handle, pt, 1); 
            if(c.Parent.IsMirrored) {
                pt.x -= c.Width;
            }
            return new Point(pt.x, pt.y); 
        }
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.MapAdornerWindowPoint"]/*' /> 
        /// <devdoc>
        /// Converts a point in handle's coordinate system to AdornerWindow coords. 
        /// </devdoc>
        public Point MapAdornerWindowPoint(IntPtr handle, Point pt) {
            NativeMethods.POINT nativePoint = new NativeMethods.POINT();
            nativePoint.x = pt.X; 
            nativePoint.y = pt.Y;
            NativeMethods.MapWindowPoints(handle, adornerWindow.Handle, nativePoint, 1); 
            return new Point(nativePoint.x, nativePoint.y); 
        }
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.ControlRectInAdornerWindow"]/*' />
        /// <devdoc>
        /// Returns the bounding rectangle of a Control translated to AdornerWindow coords.
        /// </devdoc> 
        public Rectangle ControlRectInAdornerWindow(Control c){
            if(c.Parent == null) { 
                return Rectangle.Empty; 
            }
            Point loc = ControlToAdornerWindow(c); 

            return new Rectangle(loc, c.Size);
        }
 
    	internal bool IsDisposed
    	{ 
    		get 
    		{
    			return adornerWindow == null || adornerWindow.IsDisposed; 
    		}
    	}

        /// <devdoc> 
        ///     We demand create a Control to call .DoDragDrop() on.  With
        ///     this control, we'll hook the drag events: querycontinue and 
        //      givefeedback and forward them along to the DropSourceBehavior. 
        /// </devdoc>
        private Control DropSource { 
            get {
                if (dropSource == null) {
                    dropSource = new Control();
                } 
                return dropSource;
            } 
        } 

        /// <devdoc> 
        ///     Called by the DragAssistanceManager after a snapline/drag op
        ///     has completed - we store this data for testing purposes.  See
        ///     TestHook_GetRecentSnapLines method.
        /// </devdoc> 
        internal string[] RecentSnapLines {
            set { 
                this.testHook_RecentSnapLines = value; 
            }
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.BeginDrag"]/*' />
        /// <devdoc>
        ///     The BehaviorService fires the BeginDrag event immediately 
        ///     before it starts a drop/drop operation via DoBeginDragDrop.
        /// </devdoc> 
        public event BehaviorDragDropEventHandler BeginDrag { 
            add {
                beginDragHandler += value; 
            }
            remove {
                beginDragHandler -= value;
            } 
        }
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.EndDrag"]/*' /> 
        /// <devdoc>
        ///     The BehaviorService fires the EndDrag event immediately 
        ///     after the drag operation has completed.
        /// </devdoc>
        public event BehaviorDragDropEventHandler EndDrag {
            add { 
                endDragHandler += value;
            } 
            remove { 
                endDragHandler -= value;
            } 
        }

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Synchronize"]/*' />
        /// <devdoc> 
        ///     The BehaviorService fires the Synchronize event when the current selection should be synchronized (refreshed).
        /// </devdoc> 
        public event EventHandler Synchronize { 
            add {
                synchronizeEventHandler += value; 
            }

            remove {
                synchronizeEventHandler -= value; 
            }
        } 
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Dispose"]/*' />
        /// <devdoc> 
        ///     Disposes the behavior service.
        /// </devdoc>
        public void Dispose() {
            // remove adorner window from overlay service 
            IOverlayService os = (IOverlayService)serviceProvider.GetService(typeof(IOverlayService));
            if (os != null) { 
                os.RemoveOverlay(adornerWindow); 
            }
 
            if (dropSource != null) {
                dropSource.Dispose();
            }
 
            IMenuCommandService menuCommandService = serviceProvider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost; 
 
            MenuCommandHandler menuCommandHandler = null;
            if (menuCommandService != null) 
                menuCommandHandler = menuCommandService as MenuCommandHandler;

            if (menuCommandHandler != null && host != null) {
                IMenuCommandService oldMenuCommandService = menuCommandHandler.MenuService; 
                host.RemoveService(typeof(IMenuCommandService));
                host.AddService(typeof(IMenuCommandService), oldMenuCommandService); 
            } 

            adornerWindow.Dispose(); 

            SystemEvents.DisplaySettingsChanged -= new EventHandler(this.OnSystemSettingChanged);
            SystemEvents.InstalledFontsChanged -= new EventHandler(this.OnSystemSettingChanged);
            SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(this.OnUserPreferenceChanged); 
	
 
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.DoDragDrop"]/*' /> 
        /// <devdoc>
        ///     Enables Behaviors to call DoDragDrop.
        /// </devdoc>
        internal DragDropEffects DoDragDrop(DropSourceBehavior dropSourceBehavior) { 
            //hook events
            DropSource.QueryContinueDrag += new QueryContinueDragEventHandler(dropSourceBehavior.QueryContinueDrag); 
            DropSource.GiveFeedback += new GiveFeedbackEventHandler(dropSourceBehavior.GiveFeedback); 

            DragDropEffects res = DragDropEffects.None; 

            //build up the eventargs for firing our dragbegin/end events
            ICollection dragComponents = ((DropSourceBehavior.BehaviorDataObject)dropSourceBehavior.DataObject).DragComponents;
            BehaviorDragDropEventArgs eventArgs = new BehaviorDragDropEventArgs(dragComponents); 

            try { 
                try { 
                    OnBeginDrag(eventArgs);
                    dragging = true; 
                    cancelDrag = false;
                    // This is normally cleared on OnMouseUp, but we might not get an OnMouseUp to clear it. VSWhidbey #474259
                    // So let's make sure it is really cleared when we start the drag.
                    dragEnterReplies.Clear(); 
                    res = DropSource.DoDragDrop(dropSourceBehavior.DataObject, dropSourceBehavior.AllowedEffects);
                } 
                finally { 
                    DropSource.QueryContinueDrag -= new QueryContinueDragEventHandler(dropSourceBehavior.QueryContinueDrag);
                    DropSource.GiveFeedback -= new GiveFeedbackEventHandler(dropSourceBehavior.GiveFeedback); 
                    //If the drop gets cancelled, we won't get a OnDragDrop, so let's make sure that we stop
                    //processing drag notifications. Also VSWhidbey #354552 and 133339.
                    EndDragNotification();
                    validDragArgs = null; 
                    dragging = false;
                    cancelDrag = false; 
                    OnEndDrag(eventArgs); 
                }
            } 
            catch(CheckoutException cex) {
                if (cex == CheckoutException.Canceled) {
                    res = DragDropEffects.None;
                } 
                else {
                    throw; 
                } 
            }
            finally { 
                // VSWhidbey 306626 and 281813
                // It's possible we did not receive an EndDrag, and therefore
                // we weren't able to cleanup the drag.  We will do that here.
                // Scenarios where this happens: dragging from designer to recycle-bin, 
                // or over the taskbar.
                if (dropSourceBehavior != null) { 
                    dropSourceBehavior.CleanupDrag(); 
                }
            } 

            return res;
        }
 
        /// <devdoc>
        ///     Here, we reflect on all of our components to get all SnapLines 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        private void TestHook_GetAllSnapLines(ref Message m) { 

            string snapLineInfo = "";

            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost; 
            if (host == null) {
                return; 
            } 

            foreach (Component comp in host.Container.Components) { 
                if (!(comp is Control)) {
                    continue;
                }
 
                ControlDesigner designer = host.GetDesigner(comp) as ControlDesigner;
                if (designer != null) { 
                    foreach (SnapLine line in designer.SnapLines) { 
                        snapLineInfo += line.ToString() + "\tAssociated Control = " + designer.Control.Name + ":::";
                    } 

                }
            }
 
            TestHook_SetText(ref m, snapLineInfo);
        } 
 
        /// <devdoc>
        ///     Called by ControlDesigner when it receives a DragDrop 
        ///     message - we'll let the AdornerWindow know so it can
        ///     exit from 'drag mode'.
        /// </devdoc>
        internal void EndDragNotification() { 
            adornerWindow.EndDragNotification();
        } 
 
        /// <devdoc>
        ///     Called by our meucommand handling object, we will attempt to see 
        ///     if the appropriate hittested glyph's behavior wants to intercept and/or
        ///     modify this command in any way.
        /// </devdoc>
 
        private MenuCommand FindCommand(CommandID commandID, IMenuCommandService menuService) {
 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph); 

            if (behavior != null) { 
                //if the behavior wants all commands disabled..
                if (behavior.DisableAllCommands) {
                    MenuCommand menuCommand = menuService.FindCommand(commandID);
                    if (menuCommand != null) { 
                        menuCommand.Enabled = false;
                    } 
                    return menuCommand; 
                }
                //check to see if the behavior wants to interrupt this command 
                else {
                    MenuCommand menuCommand = behavior.FindCommand(commandID);
                    if (menuCommand != null) {
                        //the behavior chose to interrupt - so return the new command 
                        return menuCommand;
                    } 
                } 
            }
 
            return menuService.FindCommand(commandID);
        }

 
        /// <devdoc>
        ///     Pushes recent snaplines into the message structure. 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Performance", "CA1818:DoNotConcatenateStringsInsideLoops")]
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")] 
        private void TestHook_GetRecentSnapLines(ref Message m) {

            string snapLineInfo = "";
 
            if (this.testHook_RecentSnapLines != null) {
                foreach(string line in this.testHook_RecentSnapLines) { 
                    snapLineInfo += line + "\n"; 
                }
            } 

            TestHook_SetText(ref m, snapLineInfo);
        }
 
        /// <devdoc>
        ///     Pushes the testhook string into the message structure 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")]
        private void TestHook_SetText(ref Message m, string text) { 

            if (m.LParam == IntPtr.Zero) {
                m.Result = (IntPtr)((text.Length + 1) * Marshal.SystemDefaultCharSize);
                return; 
            }
 
            if (unchecked((int)(long)m.WParam) < text.Length + 1) { 
                m.Result = (IntPtr)(-1);
                return; 
            }

            // Copy the name into the given IntPtr
            // 
            char[] nullChar = new char[] {(char)0};
            byte[] nullBytes; 
            byte[] bytes; 

            if (Marshal.SystemDefaultCharSize == 1) { 
                bytes = System.Text.Encoding.Default.GetBytes(text);
                nullBytes = System.Text.Encoding.Default.GetBytes(nullChar);
            }
            else { 
                bytes = System.Text.Encoding.Unicode.GetBytes(text);
                nullBytes = System.Text.Encoding.Unicode.GetBytes(nullChar); 
            } 

            Marshal.Copy(bytes, 0, m.LParam, bytes.Length); 
            Marshal.Copy(nullBytes, 0, unchecked((IntPtr)((long)m.LParam + (long)bytes.Length)), nullBytes.Length);
            m.Result = (IntPtr)((bytes.Length + nullBytes.Length)/Marshal.SystemDefaultCharSize);
        }
 

        /// <devdoc> 
        ///     This method defines the hueristic used to determine where a 
        ///     message is sent once the AdornerWindow intercepts it.  First, we'll
        ///     try to send it to the top-most Behavior on the stack.  Next, well 
        ///     send the message along to the supplied Glyph.  Finally, we'll
        ///     return null.
        /// </devdoc>
        private Behavior GetAppropriateBehavior(Glyph g) { 
            if (behaviorStack != null && behaviorStack.Count > 0) {
                return behaviorStack[0] as Behavior; 
            } 

            if (g != null && g.Behavior != null) { 
                return g.Behavior;
            }

            return null; 
        }
 
        /// <devdoc> 
        /// Given a behavior returns the behavior immediately
        /// after the behavior in the behaviorstack. 
        /// Can return null.
        /// </devdoc>
        public Behavior GetNextBehavior(Behavior behavior) {
            if (behaviorStack != null && behaviorStack.Count > 0) { 
                int index = behaviorStack.IndexOf(behavior);
                if ((index != -1) && (index < behaviorStack.Count - 1)) { 
                    return behaviorStack[index + 1] as Behavior; 
                }
            } 

            return null;
        }
 
        /// <devdoc>
        ///     Used by other designers (toolstrip designer for ex) this method will return 
        ///     the array of glyphs whose bounds intersect with the 'primaryGlyph' passed in. 
        /// </devdoc>
        internal Glyph[] GetIntersectingGlyphs(Glyph primaryGlyph) { 

            if (primaryGlyph == null) {
                Debug.Fail("The primary glyph cannot be null!");
                return new Glyph[0]; 
            }
 
            Rectangle primaryBounds = primaryGlyph.Bounds; 
            ArrayList intersectingGlyphs = new ArrayList();
 
            //loop through the glyphs in the same order as hit testing...
            for (int i = adorners.Count - 1; i >= 0; i--) {

                if (!adorners[i].Enabled) { 
                    continue;
                } 
 
                for (int j = 0; j < adorners[i].Glyphs.Count; j++) {
 
                    Glyph g = adorners[i].Glyphs[j];

                    if (primaryBounds.IntersectsWith(g.Bounds)) {
                        intersectingGlyphs.Add(g); 
                    }
                } 
 
            }
 
            if (intersectingGlyphs.Count == 0) {
                return new Glyph[0];
            }
 
            return (Glyph[])intersectingGlyphs.ToArray(typeof(Glyph));
        } 
 

        /// <devdoc> 
        ///     Called in response to a MouseMove message, we'll call TrackMouseEvents
        ///     to make sure we continue to get WM_HOVER messages so we can send them
        ///     to any relevant glyphs.
        /// </devdoc> 
        private void HookMouseEvent() {
            if (!trackingMouseEvent) { 
                trackingMouseEvent = true; 
                if (trackMouseEvent == null) {
                    trackMouseEvent = new NativeMethods.TRACKMOUSEEVENT(); 
                    trackMouseEvent.dwFlags = NativeMethods.TME_HOVER;
                    trackMouseEvent.hwndTrack = adornerWindow.Handle;
                }
                SafeNativeMethods.TrackMouseEvent(trackMouseEvent); 
            }
        } 
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Invalidate"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs.
        /// </devdoc>
        public void Invalidate() { 
            adornerWindow.InvalidateAdornerWindow();
        } 
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Invalidate2"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs.
        /// </devdoc>
        public void Invalidate(Rectangle rect) { 
            adornerWindow.InvalidateAdornerWindow(rect);
        } 
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.Invalidate3"]/*' />
        /// <devdoc> 
        ///     Invalidates the BehaviorService's AdornerWindow.  This will force a refesh of all Adorners
        ///     and, in turn, all Glyphs.
        /// </devdoc>
        public void Invalidate(Region r) { 
            adornerWindow.InvalidateAdornerWindow(r);
        } 
 
        /// <devdoc>
        ///     Called during hittesting, this method will send artificial 
        ///     enter/leave mouse events to the appropriate behavior.
        /// </devdoc>
        private void InvokeMouseEnterLeave(Glyph leaveGlyph, Glyph enterGlyph) {
            if (leaveGlyph != null) { 
                if (enterGlyph != null && leaveGlyph.Equals(enterGlyph)) {
                    //same glyph - no change 
                    return; 
                }
                if (validDragArgs != null) { 
                    OnDragLeave(leaveGlyph, EventArgs.Empty);
                }
                else {
                    OnMouseLeave(leaveGlyph); 
                }
            } 
 
            if (enterGlyph != null) {
                if (validDragArgs != null) { 
                    OnDragEnter(enterGlyph, validDragArgs);
                }
                else {
                    OnMouseEnter(enterGlyph); 
                }
            } 
 
        }
 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.SyncSelection"]/*' />
        /// <devdoc>
        ///     Synchronizes all selection glyphs. 
        /// </devdoc>
        public void SyncSelection() { 
            if (synchronizeEventHandler != null) { 
                synchronizeEventHandler(this, EventArgs.Empty);
            } 
        }

        private void OnSystemSettingChanged(object sender, EventArgs e) {
            SyncSelection(); 
            DesignerUtils.SyncBrushes();
        } 
 
        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
            SyncSelection(); 
            DesignerUtils.SyncBrushes();
        }

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.PopBehavior"]/*' /> 
        /// <devdoc>
        ///     Removes the behavior from the behavior stack 
        /// </devdoc> 
        public Behavior PopBehavior(Behavior behavior) {
            if (behaviorStack.Count == 0) { 
                throw new InvalidOperationException();
            }

            int index = behaviorStack.IndexOf(behavior); 
            if (index == -1) {
                Debug.Assert(false, "Could not find the behavior to pop - did it already get popped off? " + behavior.ToString()); 
                return null; 
            }
 
            behaviorStack.RemoveAt(index);

            if (behavior == captureBehavior) {
                adornerWindow.Capture = false; 

                // Defensive:  adornerWindow should get a WM_CAPTURECHANGED, 
                //             but do this by hand if it didn't. 
                if (captureBehavior != null) {
                    OnLoseCapture(); 
                    Debug.Assert(captureBehavior == null, "OnLostCapture should have cleared captureBehavior");
                }
            }
 
            return behavior;
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
            adornerWindow.Invalidate(paintRect); 
        }

        /// <devdoc>
        ///     Called in response to WM_NCHITTEST message intercepted by the transparent 
        ///     AdornerWindow.  The BehaviorService will cycle through all enabled
        ///     Adorners and pass this hitTest info along. 
        ///     Hit testing goes backwards through the list of adorners and forward 
        ///     through the list of glyphs... think about it...
        /// 
        ///     This returns true if it should let the default handling for the designer,
        ///     and will fill in which control was hit tested.
        /// </devdoc>
            private bool PropagateHitTest(Point pt) { 
            for (int i = adorners.Count - 1; i >= 0; i--) {
 
                if (!adorners[i].Enabled) { 
                    continue;
                } 

                for (int j = 0; j < adorners[i].Glyphs.Count; j++) {
                    Cursor hitTestCursor = adorners[i].Glyphs[j].GetHitTest(pt);
                    if (hitTestCursor != null) { 

                        // InvokeMouseEnterGlyph will cause the selection to change, which 
                        // might change the number of glyphs, so we need to remember the new glyph 
                        // before calling InvokeMouseEnterLeave. VSWhidbey #396611
                        Glyph newGlyph = adorners[i].Glyphs[j]; 

                        //with a valid hit test, fire enter/leave events
                        //
                        InvokeMouseEnterLeave(hitTestedGlyph, newGlyph); 
                        if (validDragArgs == null) {
                            //if we're not dragging, set the appropriate cursor 
                            // 
                            SetAppropriateCursor(hitTestCursor);
                        } 

                        hitTestedGlyph = newGlyph;

                        //return true if we hit on a transparentBehavior, otherwise false 
                        return (hitTestedGlyph.Behavior is ControlDesigner.TransparentBehavior);
                    } 
                } 

            } 

            InvokeMouseEnterLeave(hitTestedGlyph, null);
            if (validDragArgs == null) {
                Cursor cursor = Cursors.Default; 
                if ((behaviorStack != null) && (behaviorStack.Count > 0)) {
                    Behavior behavior = behaviorStack[0] as Behavior; 
                    if (behavior != null) { 
                        cursor = behavior.Cursor;
                    } 
                }
                SetAppropriateCursor(cursor);
            }
            hitTestedGlyph = null; 
            return true;//Returning false will cause the transparent window to return HTCLIENT when handling WM_NCHITTEST, thus blocking underline window to receive mouse events.
 
        } 

        /// <devdoc> 
        ///     Called in response to WM_PAINT message intercepted by the transparent
        ///     AdornerWindow.  The BehaviorService will cycle through all enabled
        ///     Adorners and pass this paint event along.
        ///     Painting should go forward through the list of adorners and 
        ///     backwards through each glyph... think about it...
        /// </devdoc> 
        private void PropagatePaint(PaintEventArgs pe) { 
            for (int i = 0; i < adorners.Count; i ++) {
                if (!adorners[i].Enabled) { 
                    continue;
                }
                for (int j = adorners[i].Glyphs.Count -1; j >= 0; j--) {
                    adorners[i].Glyphs[j].Paint(pe); 
                }
            } 
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.PushBehavior"]/*' /> 
        /// <devdoc>
        ///     Pushes a Behavior object onto the BehaviorStack.  This is often done through hit-tested
        ///     Glyph.
        /// </devdoc> 
        public void PushBehavior(Behavior behavior) {
            if (behavior == null) { 
                throw new ArgumentNullException("behavior"); 
            }
 

            // Should we catch this
            behaviorStack.Insert(0, behavior);
 
            // If there is a capture behavior, and it isn't this behavior,
            // notify it that it no longer has capture. 
            if (captureBehavior != null && captureBehavior != behavior) { 
                OnLoseCapture();
            } 

        }

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.PushCaptureBehavior"]/*' /> 
        /// <devdoc>
        ///     Pushes a Behavior object onto the BehaviorStack and assigns mouse capture to the behavior. 
        ///     This is often done through hit-tested Glyph.  If a behavior calls this the behavior's OnLoseCapture 
        ///     will be called if mouse capture is lost.
        /// </devdoc> 
        public void PushCaptureBehavior(Behavior behavior) {
            PushBehavior(behavior);
            captureBehavior = behavior;
            adornerWindow.Capture = true; 

            //VSWhidbey #373836. Since we are now capturing all mouse messages, we might miss some 
            //WM_MOUSEACTIVATE which would have activated the app. 
            //So if the DialogOwnerWindow (e.g. VS) is not the active window, let's activate it here.
            IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
            if(uiService != null) {
                IWin32Window hwnd = uiService.GetDialogOwnerWindow();
                if (hwnd != null && hwnd.Handle != IntPtr.Zero && hwnd.Handle != UnsafeNativeMethods.GetActiveWindow()) {
                    UnsafeNativeMethods.SetActiveWindow(new HandleRef(this, hwnd.Handle)); 
                }
            } 
        } 

        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorService.ScreenToAdornerWindow"]/*' /> 
        /// <devdoc>
        ///     Translates a screen coord into a coord relative to the BehaviorService's AdornerWindow.
        /// </devdoc>
        public Point ScreenToAdornerWindow(Point p) { 
            NativeMethods.POINT offset = new NativeMethods.POINT();
            offset.x = p.X; 
            offset.y = p.Y; 
            NativeMethods.MapWindowPoints(IntPtr.Zero, adornerWindow.Handle, offset, 1);
            return new Point(offset.x, offset.y); 
        }

        /// <devdoc>
        ///     Called after a successful Glyph hittest.  This method will first 
        ///     request the Cursor from the toolbox.  If this fails, then the
        ///     Glyph's hittested cursor will be used. 
        /// </devdoc> 
        private void SetAppropriateCursor(Cursor cursor) {
 
            //default cursors will let the toolbox svc set a cursor if needed
            if (cursor == Cursors.Default) {
                if (toolboxSvc == null) {
                    toolboxSvc = (IToolboxService)serviceProvider.GetService(typeof(IToolboxService)); 
                }
 
                if (toolboxSvc != null && toolboxSvc.SetCursor()) { 
                    cursor = new Cursor(NativeMethods.GetCursor());
                } 
            }

            adornerWindow.Cursor = cursor;
        } 

        /// <devdoc> 
        ///     Displays the given exception to the user. 
        /// </devdoc>
        private void ShowError(Exception ex) { 
            IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService;
            if (uis != null) {
                uis.ShowError(ex);
            } 
        }
 
        /// <devdoc> 
        ///     Called by ControlDesigner when it receives a DragEnter
        ///     message - we'll let the AdornerWindow know so it can 
        ///     listen to all Mouse Messages for sending drag
        ///     notifcations.
        /// </devdoc>
        internal void StartDragNotification() { 
            adornerWindow.StartDragNotification();
        } 
 
        /// <devdoc>
        ///     Called in response to a MouseLeave message, we'll 
        ///     set state signalling that we are no longer tracking the
        ///     mouse event.  This is used for capturing MouseHover
        ///     notifications.
        /// </devdoc> 
        private void UnHookMouseEvent() {
            trackingMouseEvent = false; 
        } 

        /// <devdoc> 
        ///     Invokes the BeginDrag event for any listeners.
        /// </devdoc>
        private void OnBeginDrag(BehaviorDragDropEventArgs e) {
            if (beginDragHandler != null) { 
                beginDragHandler(this, e);
            } 
        } 

        /// <devdoc> 
        ///     Invokes the EndDrag event for any listeners.
        /// </devdoc>
        private void OnEndDrag(BehaviorDragDropEventArgs e) {
            if (endDragHandler != null) { 
                endDragHandler(this, e);
            } 
        } 

        /// <devdoc> 
        ///     Called by the adorner window when it loses capture.
        /// </devdoc>

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        internal void OnLoseCapture() {
            if (captureBehavior != null) { 
                Behavior b = captureBehavior; 
                captureBehavior = null;
                try { 
                    b.OnLoseCapture(hitTestedGlyph, EventArgs.Empty);
                }
                catch {
                } 
            }
        } 
 
        /// <devdoc>
        ///     Called when any MouseDown message enters the BehaviorService's 
        ///     AdornerWindow::WndProc().  From here, the message is sent to
        ///     the appropriate behavior.
        /// </devdoc>
        private bool OnMouseDoubleClick(MouseButtons button, Point mouseLoc) { 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph);
            if (behavior == null) { 
                return false; 
            }
 
            return behavior.OnMouseDoubleClick(hitTestedGlyph, button, mouseLoc);
        }

        /// <devdoc> 
        ///     Called when any MouseDown message enters the BehaviorService's
        ///     AdornerWindow::WndProc().  From here, the message is sent to 
        ///     the appropriate behavior. 
        /// </devdoc>
        private bool OnMouseDown(MouseButtons button, Point mouseLoc) { 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph);
            if (behavior == null) {
                return false;
            } 

            return behavior.OnMouseDown(hitTestedGlyph, button, mouseLoc); 
        } 

        /// <devdoc> 
        ///     Called when any MouseDown message enters the BehaviorService's
        ///     AdornerWindow::WndProc().  From here, the message is sent to
        ///     the appropriate behavior.
        /// </devdoc> 
        private bool OnMouseEnter(Glyph g) {
            Behavior behavior = GetAppropriateBehavior(g); 
            if (behavior == null) { 
                return false;
            } 
            return behavior.OnMouseEnter(g);
        }

        /// <devdoc> 
        ///     Called when the MouseHover message enters the BehaviorService's
        ///     AdornerWindow::WndProc().  From here, the message is sent to 
        ///     the appropriate behavior. 
        /// </devdoc>
        private bool OnMouseHover(Point mouseLoc) { 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph);
            if (behavior == null) {
                return false;
            } 

            return behavior.OnMouseHover(hitTestedGlyph, mouseLoc); 
        } 

        /// <devdoc> 
        ///     Called when any MouseDown message enters the BehaviorService's
        ///     AdornerWindow::WndProc().  From here, the message is sent to
        ///     the appropriate behavior.
        /// </devdoc> 
        private bool OnMouseLeave(Glyph g) {
            //stop tracking mouse events for MouseHover 
            UnHookMouseEvent(); 

            Behavior behavior = GetAppropriateBehavior(g); 
            if (behavior == null) {
                return false;
            }
            return behavior.OnMouseLeave(g); 
        }
 
        /// <devdoc> 
        ///     Called when any MouseMove message enters the BehaviorService's
        ///     AdornerWindow::WndProc().  From here, the message is sent to 
        ///     the appropriate behavior.
        /// </devdoc>
        private bool OnMouseMove(MouseButtons button, Point mouseLoc) {
            //hook mouse events (if we haven't already) for MouseHover 
            HookMouseEvent();
 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph); 
            if (behavior == null) {
                return false; 
            }

            return behavior.OnMouseMove(hitTestedGlyph, button, mouseLoc);
        } 

        /// <devdoc> 
        ///     Called when any MouseUp message enters the BehaviorService's 
        ///     AdornerWindow::WndProc().  From here, the message is sent to
        ///     the appropriate behavior. 
        /// </devdoc>
        private bool OnMouseUp(MouseButtons button) {
            dragEnterReplies.Clear();
            validDragArgs = null; 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph);
            if (behavior == null) { 
                return false; 
            }
 
            return behavior.OnMouseUp(hitTestedGlyph, button);
        }

        //OLEDragDrop messages... 

        /// <devdoc> 
        ///     Used to send this Drag/Drop event from the AdornerWindow 
        ///     to the appropriate Behavior (or hit tested Glyph).
        /// </devdoc> 
        private void OnDragDrop(DragEventArgs e) {
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "BS::OnDragDrop");
            validDragArgs = null;//be sure to null out our cached drag args
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph); 
            if (behavior == null) {
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tNo behavior. returning"); 
                return; 
            }
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tForwarding to behavior"); 
            behavior.OnDragDrop(hitTestedGlyph, e);
        }
        /// <devdoc>
        ///     Used to send this Drag/Drop event from the AdornerWindow 
        ///     to the appropriate Behavior (or hit tested Glyph).
        /// </devdoc> 
        private void OnDragEnter(Glyph g, DragEventArgs e) { 
            //if the AdornerWindow receives a drag message, this fn()
            //will be called w/o a glyph - so we'll assign the last 
            //hit tested one
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "BS::OnDragEnter");
            if (g == null) {
                g = hitTestedGlyph; 
            }
 
            Behavior behavior = GetAppropriateBehavior(g); 
            if (behavior == null) {
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tNo behavior, returning"); 
                return;
            }
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tForwarding to behavior");
            behavior.OnDragEnter(g, e); 

            if (g != null && g is ControlBodyGlyph && e.Effect == DragDropEffects.None) { 
                dragEnterReplies[g] = this; // dummy value, we just need to set something. 
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tCalled DragEnter on this glyph. Caching");
            } 
        }

        /// <devdoc>
        ///     Used to send this Drag/Drop event from the AdornerWindow 
        ///     to the appropriate Behavior (or hit tested Glyph).
        /// </devdoc> 
        private void OnDragLeave(Glyph g, EventArgs e) { 
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "BS::DragLeave");
 
            // This is normally cleared on OnMouseUp, but we might not get an OnMouseUp to clear it. VSWhidbey #474259
            // So let's make sure it is really cleared when we start the drag.
            dragEnterReplies.Clear();
 
            //if the AdornerWindow receives a drag message, this fn()
            //will be called w/o a glyph - so we'll assign the last 
            //hit tested one 
            if (g == null) {
                g = hitTestedGlyph; 
            }

            Behavior behavior = GetAppropriateBehavior(g);
            if (behavior == null) { 
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\t No behavior returning ");
                return; 
            } 
            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tBehavior found calling OnDragLeave");
            behavior.OnDragLeave(g, e); 
        }

        /// <devdoc>
        ///     Used to send this Drag/Drop event from the AdornerWindow 
        ///     to the appropriate Behavior (or hit tested Glyph).
        /// </devdoc> 
        private void OnDragOver(DragEventArgs e) { 
            //cache off our validDragArgs so we can
            //re-fabricate enter/leave drag events 
            validDragArgs = e;

            Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "BS::DragOver");
 
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph);
            if (behavior == null) { 
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tNo behavior, exiting with DragDropEffects.None"); 
                e.Effect = DragDropEffects.None;
                return; 
            }
            if (hitTestedGlyph == null ||
               (hitTestedGlyph != null && !dragEnterReplies.ContainsKey(hitTestedGlyph))) {
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tFound glyph, forwarding to behavior"); 
                behavior.OnDragOver(hitTestedGlyph, e);
            } 
            else { 
                Debug.WriteLineIf(dragDropSwitch.TraceVerbose, "\tFell through");
                e.Effect = DragDropEffects.None; 
            }
        }

        /// <devdoc> 
        ///     Used to send this Drag/Drop event from the AdornerWindow
        ///     to the appropriate Behavior (or hit tested Glyph). 
        /// </devdoc> 
        private void OnGiveFeedback(GiveFeedbackEventArgs e) {
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph); 
            if (behavior == null) {
                return;
            }
 
            behavior.OnGiveFeedback(hitTestedGlyph, e);
        } 
 
        /// <devdoc>
        ///     Used to send this Drag/Drop event from the AdornerWindow 
        ///     to the appropriate Behavior (or hit tested Glyph).
        /// </devdoc>
        private void OnQueryContinueDrag(QueryContinueDragEventArgs e) {
            Behavior behavior = GetAppropriateBehavior(hitTestedGlyph); 
            if (behavior == null) {
                return; 
            } 

            behavior.OnQueryContinueDrag(hitTestedGlyph, e); 
        }


        /// <devdoc> 
        ///     The AdornerWindow is a transparent window that resides ontop of the
        ///     Designer's Frame.  This window is used by the BehaviorService to 
        ///     intercept all messages.  It also serves as a unified canvas on which 
        ///     to paint Glyphs.
        /// </devdoc> 
        private class AdornerWindow : Control {

            private BehaviorService         behaviorService;//ptr back to BehaviorService
            private Control                 designerFrame;//the designer's frame 
            private MouseHook               mouseHook;
 
            /// <devdoc> 
            ///     Constructor that parents itself to the Designer Frame and hooks all
            ///     necessary events. 
            /// </devdoc>
            [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
            internal AdornerWindow(BehaviorService behaviorService, Control designerFrame) {
                this.behaviorService = behaviorService; 
                this.designerFrame = designerFrame;
                this.Dock = DockStyle.Fill; 
                this.AllowDrop = true; 
                this.Text = "AdornerWindow";
 
                SetStyle(ControlStyles.Opaque,true);

                designerFrame.HandleDestroyed += new EventHandler(OnDesignerFrameHandleDestroyed);
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
                if (mouseHook == null) {
                    mouseHook = new MouseHook(this, designerFrame); 
                }
                mouseHook.HookMouseMessages = true;
            }
 
            /// <devdoc>
            ///     Unhook and null out our mouseHook. 
            /// </devdoc> 
            protected override void OnHandleDestroyed(EventArgs e) {
                if (mouseHook != null) { 
                    mouseHook.HookMouseMessages = false;
                    mouseHook = null;
                }
                base.OnHandleDestroyed(e); 
            }
 
            /// <devdoc> 
            ///     Null out our mouseHook and unhook any events.
            /// </devdoc> 
            protected override void Dispose(bool disposing) {
                if (disposing) {
                    if (mouseHook != null) {
                        mouseHook.Dispose(); 
                        mouseHook = null;
                    } 
 
                    if (designerFrame != null) {
                        designerFrame.HandleDestroyed -= new EventHandler(OnDesignerFrameHandleDestroyed); 
                        designerFrame = null;
                    }

                } 
                base.Dispose(disposing);
            } 
 
            /// <devdoc>
            ///     Returns the display rectangle for the adorner window 
            /// </devdoc>
            internal  Rectangle DesignerFrameDisplayRectangle {
                get {
                    if(DesignerFrameValid) { 
                        return ((DesignerFrame)designerFrame).DisplayRectangle;
                    } else { 
                        return Rectangle.Empty; 
                    }
                } 
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
            ///     Ultimately called by ControlDesigner when it receives a DragDrop 
            ///     message - here, we'll exit from 'drag mode'.
            /// </devdoc> 
            internal void EndDragNotification() {
                this.mouseHook.ProcessingDrag = false;
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
                    //translate for non-zero scroll positions 
                    Point scrollPosition = ((DesignerFrame)designerFrame).AutoScrollPosition; 
                    region.Translate(scrollPosition.X, scrollPosition.Y);
 
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
                    //translate for non-zero scroll positions 
                    Point scrollPosition = ((DesignerFrame)designerFrame).AutoScrollPosition;
                    rectangle.Offset(scrollPosition.X, scrollPosition.Y); 
 
                    designerFrame.Invalidate(rectangle, true);
                    designerFrame.Update(); 
                }

            }
 
            private void OnDesignerFrameHandleDestroyed(object s, EventArgs e) {
                if (mouseHook != null) { 
                    mouseHook.Dispose(); 
                    mouseHook = null;
                } 
            }

            /// <devdoc>
            ///     The AdornerWindow hooks all Drag/Drop notification so 
            ///     they can be forwarded to the appropriate Behavior via
            ///     the BehaviorService. 
            /// </devdoc> 
            protected override void OnDragDrop(DragEventArgs e) {
                try { 
                    behaviorService.OnDragDrop(e);
                }
                finally {
                    this.mouseHook.ProcessingDrag = false; 
                }
            } 
 
            private static bool IsLocalDrag(DragEventArgs e) {
                if (e.Data is DropSourceBehavior.BehaviorDataObject) { 
                    return true;
                }
                else {
                    // Gets all the data formats and data conversion formats in the data object. 
                    String[] allFormats = e.Data.GetFormats();
 
                    for (int i = 0; i < allFormats.Length; i++) { 
                        if (allFormats[i].Length == ToolboxFormat.Length &&
                            string.Equals(ToolboxFormat, allFormats[i])) { 
                            return true;
                        }
                    }
                } 
                return false;
            } 
 

            /// <devdoc> 
            ///     The AdornerWindow hooks all Drag/Drop notification so
            ///     they can be forwarded to the appropriate Behavior via
            ///     the BehaviorService.
            /// </devdoc> 
            protected override void OnDragEnter(DragEventArgs e) {
                this.mouseHook.ProcessingDrag = true; 
 
                // determine if this is a local drag, if it is, do normal processing
                // otherwise, force a PropagateHitTest.  We need to force 
                // this because the OLE D&D service suspends mouse messages
                // when the drag is not local so the mouse hook never sees them.
                if (!IsLocalDrag(e)) {
                    behaviorService.validDragArgs = e; 
                    NativeMethods.POINT pt = new NativeMethods.POINT();
                    NativeMethods.GetCursorPos(pt); 
                    NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, pt, 1); 
                    Point mousePos = new Point(pt.x, pt.y);
                    behaviorService.PropagateHitTest(mousePos); 

                }
                behaviorService.OnDragEnter(null, e);
            } 

            /// <devdoc> 
            ///     The AdornerWindow hooks all Drag/Drop notification so 
            ///     they can be forwarded to the appropriate Behavior via
            ///     the BehaviorService. 
            /// </devdoc>
            protected override void OnDragLeave(EventArgs e) {
                //set our dragArgs to null so we know not to send
                //drag enter/leave events when we re-enter the 
                //dragging area
                behaviorService.validDragArgs = null; 
 
                try {
                    behaviorService.OnDragLeave(null, e); 
                }
                finally {
                    this.mouseHook.ProcessingDrag = false;
                } 
            }
 
            /// <devdoc> 
            ///     The AdornerWindow hooks all Drag/Drop notification so
            ///     they can be forwarded to the appropriate Behavior via 
            ///     the BehaviorService.
            /// </devdoc>
            protected override void OnDragOver(DragEventArgs e) {
                this.mouseHook.ProcessingDrag = true; 
                if (!IsLocalDrag(e)) {
                    behaviorService.validDragArgs = e; 
                    NativeMethods.POINT pt = new NativeMethods.POINT(); 
                    NativeMethods.GetCursorPos(pt);
                    NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, pt, 1); 
                    Point mousePos = new Point(pt.x, pt.y);
                    behaviorService.PropagateHitTest(mousePos);
                }
 
                behaviorService.OnDragOver(e);
            } 
 
            /// <devdoc>
            ///     The AdornerWindow hooks all Drag/Drop notification so 
            ///     they can be forwarded to the appropriate Behavior via
            ///     the BehaviorService.
            /// </devdoc>
            protected override void OnGiveFeedback(GiveFeedbackEventArgs e) { 
                behaviorService.OnGiveFeedback(e);
            } 
 
            /// <devdoc>
            ///     The AdornerWindow hooks all Drag/Drop notification so 
            ///     they can be forwarded to the appropriate Behavior via
            ///     the BehaviorService.
            /// </devdoc>
            protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e) { 
                behaviorService.OnQueryContinueDrag(e);
            } 
 
            /// <devdoc>
            ///     Called by ControlDesigner when it receives a DragEnter 
            ///     message - we'll let listen to all Mouse Messages
            ///     so we can send drag notifcations.
            /// </devdoc>
            internal void StartDragNotification() { 
                this.mouseHook.ProcessingDrag = true;
            } 
 
            /// <devdoc>
            ///     The AdornerWindow intercepts all designer-related messages and forwards them 
            ///     to the BehaviorService for appropriate actions.  Note that Paint and HitTest
            ///     messages are correctly parsed and translated to AdornerWindow coords.
            /// </devdoc>
            protected override void WndProc(ref Message m) 
            {
                //special test hooks 
                if (m.Msg == BehaviorService.WM_GETALLSNAPLINES) { 
                    behaviorService.TestHook_GetAllSnapLines(ref m);
                } 
                else if(m.Msg == BehaviorService.WM_GETRECENTSNAPLINES) {
                    behaviorService.TestHook_GetRecentSnapLines(ref m);
                }
 
                switch(m.Msg)
                { 
                    case NativeMethods.WM_PAINT: 
                        //
                        // Stash off the region we have to update 
                        //
                        IntPtr hrgn = NativeMethods.CreateRectRgn(0, 0, 0, 0);
                        NativeMethods.GetUpdateRgn(m.HWnd, hrgn, true);
 
                        //
                        // The region we have to update in terms of the smallest rectangle 
                        // that completely encloses the update region of the window gives 
                        // us the clip rectangle
                        // 
                        NativeMethods.RECT clip = new NativeMethods.RECT();
                        NativeMethods.GetUpdateRect(m.HWnd, ref clip, true);
                        Rectangle paintRect = new Rectangle(clip.left, clip.top, clip.right - clip.left, clip.bottom - clip.top);
 
                        try {
                            using (Region r = Region.FromHrgn(hrgn)) { 
                                // 
                                // Call the base class to do its painting.
                                // 
                                DefWndProc(ref m);

                                //
                                // Now do our own painting. 
                                //
                                using (Graphics g = Graphics.FromHwnd(m.HWnd)) { 
                                    using (PaintEventArgs pevent = new PaintEventArgs(g, paintRect)) { 
                                        g.Clip = r;
                                        behaviorService.PropagatePaint(pevent); 
                                    }
                                }
                            }
                        } 
                        finally {
                            NativeMethods.DeleteObject(hrgn); 
                        } 
                        break;
 
                    case NativeMethods.WM_NCHITTEST:
                        Point pt = new Point((short)NativeMethods.Util.LOWORD((int)m.LParam),
                                             (short)NativeMethods.Util.HIWORD((int)m.LParam));
                        NativeMethods.POINT pt1 = new NativeMethods.POINT(); 
                        pt1.x = 0;
                        pt1.y = 0; 
                        NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, pt1, 1); 
                        pt.Offset(pt1.x, pt1.y);
                        if (behaviorService.PropagateHitTest(pt) && !mouseHook.ProcessingDrag) { 
                            m.Result = (IntPtr)(NativeMethods.HTTRANSPARENT);
                        }
                        else {
                            m.Result = (IntPtr)(NativeMethods.HTCLIENT); 
                        }
                        break; 
 
                    case NativeMethods.WM_CAPTURECHANGED:
                        base.WndProc(ref m); 
                        behaviorService.OnLoseCapture();
                        break;

                    default: 
                        base.WndProc(ref m);
                        break; 
                } 
            }
 
            /// <devdoc>
            ///     Called by our mouseHook when it spies a mouse message that the adornerWindow
            ///     would be interested in.  Returning 'true' signifies that the message was processed
            ///     and should not continue to child windows. 
            /// </devdoc>
            private bool WndProcProxy(ref Message m, int x, int y) { 
 
                Point mouseLoc = new Point(x,y);
                behaviorService.PropagateHitTest(mouseLoc); 

                switch (m.Msg) {
                    case NativeMethods.WM_LBUTTONDOWN:
                        if (behaviorService.OnMouseDown(MouseButtons.Left, mouseLoc)) { 
                            return false;
                        } 
                        break; 

                    case NativeMethods.WM_RBUTTONDOWN: 
                        if (behaviorService.OnMouseDown(MouseButtons.Right, mouseLoc)) {
                            return false;
                        }
                        break; 

                    case NativeMethods.WM_MOUSEMOVE: 
                        if (behaviorService.OnMouseMove(Control.MouseButtons, mouseLoc)) { 
                            return false;
                        } 
                        break;

                    case NativeMethods.WM_LBUTTONUP:
                        if (behaviorService.OnMouseUp(MouseButtons.Left)) { 
                            return false;
                        } 
                        break; 

                    case NativeMethods.WM_RBUTTONUP: 
                        if (behaviorService.OnMouseUp(MouseButtons.Right)) {
                            return false;
                        }
                        break; 

                    case NativeMethods.WM_MOUSEHOVER: 
                        if (behaviorService.OnMouseHover(mouseLoc)) { 
                            return false;
                        } 
                        break;

                    case NativeMethods.WM_LBUTTONDBLCLK:
                        if (behaviorService.OnMouseDoubleClick(MouseButtons.Left, mouseLoc)) { 
                            return false;
                        } 
                        break; 

                    case NativeMethods.WM_RBUTTONDBLCLK: 
                        if (behaviorService.OnMouseDoubleClick(MouseButtons.Right, mouseLoc)) {
                            return false;
                        }
                        break; 
                }
                return true; 
            } 

            /// <include file='doc\BehaviorService.AdornerWidnow.MouseHook.uex' path='docs/doc[@for="BehaviorService.AdornerWidnow.MouseHook"]/*' /> 
            /// <devdoc>
            ///     This class knows how to hook all the messages to a given process/thread.  On any mouse clicks, it asks the designer what to do with the message,
            ///     that is to eat it or propogate it to the control it was meant for.   This allows us to synchrounously process mouse messages when
            ///     the AdornerWindow itself may be pumping messages. 
            /// </devdoc>
            /// <internalonly/> 
            [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")] 
            private class MouseHook {
 
                private AdornerWindow   adornerWindow;
                private Control         designerFrame;
                private int             thisProcessID = 0;
                private GCHandle        mouseHookRoot; 
                [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
                private IntPtr          mouseHookHandle = IntPtr.Zero; 
                private bool            processingMessage; 
                private bool            processingDrag;
 
                private bool            isHooked = false; //VSWHIDBEY # 474112
                private int             lastLButtonDownTimeStamp;

                public MouseHook(AdornerWindow aw, Control df) { 
                   this.designerFrame = df;
                   this.adornerWindow = aw; 
                   #if DEBUG 
                   callingStack = Environment.StackTrace;
                   #endif 
                }

                #if DEBUG
                string callingStack; 
                ~MouseHook() {
                    Debug.Assert(mouseHookHandle == IntPtr.Zero, "Finalizing an active mouse hook.  This will crash the process.  Calling stack: " + callingStack); 
                } 
                #endif
 
                public virtual bool HookMouseMessages {
                    get{
                        return mouseHookHandle != IntPtr.Zero;
                    } 
                    set{
                        if (value) { 
                            HookMouse(); 
                        }
                        else { 
                            UnhookMouse();
                        }
                    }
                } 

                public bool ProcessingDrag { 
                    get { 
                        return processingDrag;
                    } 
                    set {
                        processingDrag = value;
                    }
                } 

                public void Dispose() { 
                   UnhookMouse(); 
                }
 
                private void HookMouse() {
                    lock(this) {
                        if (mouseHookHandle != IntPtr.Zero) {
                            return; 
                        }
 
                        if (thisProcessID == 0) { 
                            UnsafeNativeMethods.GetWindowThreadProcessId(new HandleRef(adornerWindow, adornerWindow.Handle), out thisProcessID);
                        } 


                        UnsafeNativeMethods.HookProc hook = new UnsafeNativeMethods.HookProc(this.MouseHookProc);
                        mouseHookRoot = GCHandle.Alloc(hook); 

#pragma warning disable 618 
                        mouseHookHandle = UnsafeNativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE, 
                                                                   hook,
                                                                   new HandleRef(null, IntPtr.Zero), 
                                                                   AppDomain.GetCurrentThreadId());
#pragma warning restore 618
                        if (mouseHookHandle != IntPtr.Zero) {
                            isHooked = true; 
                        }
                        Debug.Assert(mouseHookHandle != IntPtr.Zero, "Failed to install mouse hook"); 
                    } 
                }
 
                [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
                [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
                private unsafe IntPtr MouseHookProc(int nCode, IntPtr wparam, IntPtr lparam) {
                    if (isHooked && nCode == NativeMethods.HC_ACTION) { 
                        NativeMethods.MOUSEHOOKSTRUCT* mhs = (NativeMethods.MOUSEHOOKSTRUCT*)lparam;
                        if (mhs != null) { 
                            try { 
                                if (ProcessMouseMessage(mhs->hWnd, (int)wparam, mhs->pt_x, mhs->pt_y)) {
                                    return (IntPtr)1; 
                                }
                            }
                            catch (Exception ex) {
                                adornerWindow.Capture = false; 
                                if (ex != CheckoutException.Canceled) {
                                    adornerWindow.behaviorService.ShowError(ex); 
                                } 
                                if (ClientUtils.IsCriticalException(ex)) {
                                    throw; 
                                }
                            }
                        }
                    } 

                    Debug.Assert(isHooked, "How did we get here when we are diposed?"); 
 
                    return UnsafeNativeMethods.CallNextHookEx(new HandleRef(this, mouseHookHandle), nCode, wparam, lparam);
                } 

                private void UnhookMouse() {
                    lock(this) {
                        if (mouseHookHandle != IntPtr.Zero) { 
                            UnsafeNativeMethods.UnhookWindowsHookEx(new HandleRef(this, mouseHookHandle));
                            mouseHookRoot.Free(); 
                            mouseHookHandle = IntPtr.Zero; 
                            isHooked = false;
                        } 
                    }
                }

                 /* 
                * Here is where we force validation on any clicks outside the
                */ 
                private bool ProcessMouseMessage(IntPtr hWnd, int msg, int x, int y) { 

                    if (processingMessage) { 
                      return false;
                    }

                    // We could have hooked a control in a semitrust web page.  This would put 
                    // semitrust frames above us, which could cause this to fail.
                    // SECREVIEW, 
 

                    new NamedPermissionSet("FullTrust").Assert(); 

                    IntPtr handle = designerFrame.Handle;

                    // if it's us or one of our children, just process as normal 
                    //
                    if (processingDrag || (hWnd != handle && SafeNativeMethods.IsChild(new HandleRef(this, handle), new HandleRef(this, hWnd)))) { 
                        Debug.Assert(thisProcessID != 0, "Didn't get our process id!"); 

                        // make sure the window is in our process 
                        int pid;
                        UnsafeNativeMethods.GetWindowThreadProcessId(new HandleRef(null, hWnd), out pid);

                        // if this isn't our process, bail 
                        if (pid != thisProcessID) {
                            return false; 
                        } 

                        try { 
                           processingMessage = true;

                           NativeMethods.POINT pt = new NativeMethods.POINT();
                           pt.x = x; 
                           pt.y = y;
                           NativeMethods.MapWindowPoints(IntPtr.Zero, adornerWindow.Handle, pt, 1); 
                           Message m = Message.Create(hWnd, msg, (IntPtr)0, (IntPtr)MAKELONG(pt.y, pt.x)); 

                           // DevDiv Bugs 79616, No one knows why we get an extra click here from VS. 
                           // As a workaround, we check the TimeStamp and discard it.
                           if (m.Msg == NativeMethods.WM_LBUTTONDOWN)
                           {
                               lastLButtonDownTimeStamp = UnsafeNativeMethods.GetMessageTime(); 
                           }
                           else if (m.Msg == NativeMethods.WM_LBUTTONDBLCLK) 
                           { 
                               int lButtonDoubleClickTimeStamp = UnsafeNativeMethods.GetMessageTime();
                               if (lButtonDoubleClickTimeStamp == lastLButtonDownTimeStamp) 
                               {
                                   return true;
                               }
                           } 

                           if (!adornerWindow.WndProcProxy(ref m, pt.x, pt.y)) { 
                               // we did the work, stop the message propogation 
                               return true;
                           } 

                        }
                        finally {
                           processingMessage = false; 
                        }
                    } 
 
                    return false;
                } 


                public static int MAKELONG(int low, int high) {
                    return (high << 16) | (low & 0xffff); 
                }
            } 
        } 

        /// <devdoc> 
        ///     This class is used to notifiy the BehaviorService when a 'FindCommand'
        ///     call on the ImenuCommandService has fired.  When this happens the
        ///     BehaviorService will ask the appropriate glyph's behavior if it intends
        ///     to interrupt the processing of the command. 
        /// </devdoc>
        private class MenuCommandHandler : IMenuCommandService { 
 
            private BehaviorService         owner;//ptr back to the behavior service
            private IMenuCommandService     menuService;//core service used for most implementations of the IMCS interface 
            private Stack<CommandID>        currentCommands = new Stack<CommandID>();

            /// <devdoc>
            ///     Cache off the behsvc and the menucommand service... 
            /// </devdoc>
            public MenuCommandHandler(BehaviorService owner, IMenuCommandService menuService) { 
                this.owner = owner; 
                this.menuService = menuService;
            } 
	
            /// <devdoc>
            ///     get menucommand service
            /// </devdoc> 
            public IMenuCommandService MenuService {
		        get{ 
                    return menuService; 
                }
            } 

 	
            /// <devdoc>
            ///     Just call straight through to the IMCS 
            /// </devdoc>
            void IMenuCommandService.AddCommand(MenuCommand command) 
            { 
                menuService.AddCommand(command);
            } 

            /// <devdoc>
            ///     Just call straight through to the IMCS
            /// </devdoc> 
            void IMenuCommandService.RemoveVerb(DesignerVerb verb)
            { 
                menuService.RemoveVerb(verb); 
            }
 
            /// <devdoc>
            ///     Just call straight through to the IMCS
            /// </devdoc>
            void IMenuCommandService.RemoveCommand(MenuCommand command) 
            {
                menuService.RemoveCommand(command); 
            } 

            /// <devdoc> 
            ///     Give the behavior service (specifically: the hittestedglyph's behavior)
            ///     a chance to interrupt this command.
            /// </devdoc>
            MenuCommand IMenuCommandService.FindCommand(CommandID commandID) 
            {
 
                try { 

                    if (currentCommands.Contains(commandID)) { 
                        return null;
                    }

                    currentCommands.Push(commandID); 

                    return owner.FindCommand(commandID, menuService); 
                } 
                finally {
                    currentCommands.Pop(); 
                }
            }

            /// <devdoc> 
            ///     Just call straight through to the IMCS
            /// </devdoc> 
            bool IMenuCommandService.GlobalInvoke(CommandID commandID) 
            {
                return menuService.GlobalInvoke(commandID); 
            }

            /// <devdoc>
            ///     Just call straight through to the IMCS 
            /// </devdoc>
            void IMenuCommandService.ShowContextMenu(CommandID menuID, int x, int y) 
            { 
                menuService.ShowContextMenu(menuID,x, y);
            } 

            /// <devdoc>
            ///     Just call straight through to the IMCS
            /// </devdoc> 
            void IMenuCommandService.AddVerb(DesignerVerb verb)
            { 
                menuService.AddVerb(verb); 
            }
 
            /// <devdoc>
            ///     Just call straight through to the IMCS
            /// </devdoc>
            DesignerVerbCollection IMenuCommandService.Verbs 
            {
                get { 
                    return menuService.Verbs; 
                }
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
