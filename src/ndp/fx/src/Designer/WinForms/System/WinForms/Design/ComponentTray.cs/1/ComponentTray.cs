//------------------------------------------------------------------------------ 
// <copyright file="ComponentTray.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Runtime.Serialization.Formatters; 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Serialization; 
    using Microsoft.Win32;
    using System.Drawing; 
    using System.Design;
    using System.Drawing.Design;
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel; 
    using System.Windows.Forms.Design.Behavior;
    using System.Diagnostics.CodeAnalysis; 
 
    /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides the component tray UI for the form designer.</para>
    /// </devdoc>
    [ 
    ToolboxItem(false),
    DesignTimeVisible(false), 
    ProvideProperty("Location", typeof(IComponent)), 
    ProvideProperty("TrayLocation", typeof(IComponent)),   // VSWhidbey# 420631
    ] 
    public class ComponentTray : ScrollableControl, IExtenderProvider, ISelectionUIHandler, IOleDragClient {

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.InvalidPoint"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc> 
        private static readonly Point InvalidPoint = new Point(int.MinValue, int.MinValue); 

        private  IServiceProvider   serviceProvider;    // Where services come from. 

        private Point                   whiteSpace = Point.Empty;         // space to leave between components.
        private Size                    grabHandle = Size.Empty; // Size of the grab handles.
 
        private ArrayList               controls;           // List of items in the tray in the order of their layout.
 
        private SelectionUIHandler      dragHandler;        // the thing responsible for handling mouse drags 
        private ISelectionUIService     selectionUISvc;     // selectiuon UI; we use this a lot
        private IToolboxService         toolboxService;     // cached for drag/drop 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.oleDragDropHandler"]/*' />
        /// <devdoc>
        ///    <para>Provides drag and drop functionality through OLE.</para> 
        /// </devdoc>
        internal OleDragDropHandler     oleDragDropHandler; // handler class for ole drag drop operations. 
 
        private IDesigner               mainDesigner;       // the designer that is associated with this tray
        private IEventHandlerService    eventHandlerService = null; // Event Handler service to handle keyboard and focus. 
        private bool                    queriedTabOrder;
        private MenuCommand             tabOrderCommand;
        private ICollection             selectedObjects;
 
        // Services that we use on a high enough frequency to merit caching.
        // 
        private IMenuCommandService     menuCommandService; 
        private CommandSet              privateCommandSet=null;
        private InheritanceUI           inheritanceUI; 

        private Point       mouseDragStart = InvalidPoint;       // the starting location of a drag
        private Point       mouseDragEnd = InvalidPoint;         // the ending location of a drag
        private Rectangle   mouseDragWorkspace = Rectangle.Empty;   // a temp work rectangle we cache for perf 
        private ToolboxItem mouseDragTool;        // the tool that's being dragged; only for drag/drop
        private Point       mouseDropLocation = InvalidPoint;    // where the tool was dropped 
        private bool        showLargeIcons = false;// Show Large icons or not. 
        private bool        autoArrange = false;   // allows for auto arranging icons.
        private Point       autoScrollPosBeforeDragging = Point.Empty;//Used to return the correct scroll pos. after a drag 

        // Component Tray Context menu items...
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.menucmdArrangeIcons"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        private MenuCommand menucmdArrangeIcons = null; 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.menucmdLineupIcons"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        private MenuCommand menucmdLineupIcons = null;
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.menucmdLargeIcons"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        private MenuCommand menucmdLargeIcons = null;
 
        private bool fResetAmbient = false;

        private ComponentTrayGlyphManager glyphManager;//used to manage any glyphs added to the tray
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ComponentTray"]/*' />
        /// <devdoc> 
        ///      Creates a new component tray.  The component tray 
        ///      will monitor component additions and removals and create
        ///      appropriate UI objects in its space. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        public ComponentTray(IDesigner mainDesigner, IServiceProvider serviceProvider) {
            this.AutoScroll = true; 
            this.mainDesigner = mainDesigner;
            this.serviceProvider = serviceProvider; 
            this.AllowDrop = true; 
            Text = "ComponentTray"; // makes debugging easier
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true); 

            controls = new ArrayList();

            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            IExtenderProviderService es = (IExtenderProviderService)GetService(typeof(IExtenderProviderService));
            Debug.Assert(es != null, "Component tray wants an extender provider service, but there isn't one."); 
            if (es != null) { 
                es.AddExtenderProvider(this);
            } 

            if (GetService(typeof(IEventHandlerService)) == null) {
                if (host != null) {
                    eventHandlerService = new EventHandlerService(this); 
                    host.AddService(typeof(IEventHandlerService), eventHandlerService);
                } 
            } 

            IMenuCommandService mcs = MenuService; 
            if (mcs != null) {
                Debug.Assert(menucmdArrangeIcons == null, "Non-Null Menu Command for ArrangeIcons");
                Debug.Assert(menucmdLineupIcons  == null, "Non-Null Menu Command for LineupIcons");
                Debug.Assert(menucmdLargeIcons   == null, "Non-Null Menu Command for LargeIcons"); 

                menucmdArrangeIcons = new MenuCommand(new EventHandler(OnMenuArrangeIcons), StandardCommands.ArrangeIcons); 
                menucmdLineupIcons = new MenuCommand(new EventHandler(OnMenuLineupIcons), StandardCommands.LineupIcons); 
                menucmdLargeIcons = new MenuCommand(new EventHandler(OnMenuShowLargeIcons), StandardCommands.ShowLargeIcons);
 
                menucmdArrangeIcons.Checked = AutoArrange;
                menucmdLargeIcons.Checked   = ShowLargeIcons;
                mcs.AddCommand(menucmdArrangeIcons);
                mcs.AddCommand(menucmdLineupIcons); 
                mcs.AddCommand(menucmdLargeIcons);
            } 
 
            IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
 
            if (componentChangeService != null) {
                componentChangeService.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved);
            }
 
            IUIService uiService = (IUIService)GetService(typeof(IUIService));
            if (uiService != null) { 
                Color styleColor; 

                //Can't use 'as' here since Color is a value type 
                if (uiService.Styles["VsColorDesignerTray"] is Color) {
                    styleColor = (Color) uiService.Styles["VsColorDesignerTray"];
                }
                else if (uiService.Styles["HighlightColor"] is Color) { 
                    // Since v1, we have had code here that checks for HighlightColor, so some hosts (like WinRes)
                    // have been setting it. If VsColorDesignerTray isn't present, we look for HighlightColor 
                    // for backward compat. 
                    styleColor = (Color) uiService.Styles["HighlightColor"];
                } 
                else {
                    //No style color provided? Let's pick a default.
                    styleColor = SystemColors.Info;
                } 

 
                BackColor = styleColor; 
                Font = (Font)uiService.Styles["DialogFont"];
            } 

            ISelectionService selSvc = (ISelectionService)GetService(typeof(ISelectionService));
            if (selSvc != null) {
                selSvc.SelectionChanged += new EventHandler(OnSelectionChanged); 
            }
 
            // Listen to the SystemEvents so that we can resync selection based on display settings etc. 
            SystemEvents.DisplaySettingsChanged += new EventHandler(this.OnSystemSettingChanged);
            SystemEvents.InstalledFontsChanged += new EventHandler(this.OnSystemSettingChanged); 
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(this.OnUserPreferenceChanged);

            // Listen to refresh events from TypeDescriptor.  If a component gets refreshed, we re-query
            // and will hide/show the view based on the DesignerView attribute. 
            //
            TypeDescriptor.Refreshed += new RefreshEventHandler(OnComponentRefresh); 
 
            BehaviorService behSvc = GetService(typeof(BehaviorService)) as BehaviorService;
            if(behSvc != null) { 
                //this object will manage any glyphs that get added to our tray
                glyphManager = new ComponentTrayGlyphManager(selSvc, behSvc);
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.AutoArrange"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public bool AutoArrange {
            get {
                return autoArrange;
            } 

            set { 
                if (autoArrange != value) { 
                    autoArrange = value;
                    menucmdArrangeIcons.Checked = value; 

                    if (autoArrange) {
                        DoAutoArrange(true);
                    } 
                }
            } 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ComponentCount"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets the number of compnents contained within this tray.
        ///    </para> 
        /// </devdoc>
        public int ComponentCount { 
            get { 
                return Controls.Count;
            } 
        }

        internal virtual SelectionUIHandler DragHandler {
            get { 
                if (dragHandler == null) {
                    dragHandler = new TraySelectionUIHandler(this); 
                } 
                return dragHandler;
            } 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.Glyphs"]/*' />
        /// <devdoc> 
        ///     Internally exposes a way for sited components to add
        ///     glyphs to the component tray. 
        /// </devdoc> 
        internal GlyphCollection SelectionGlyphs {
            get { 
                if(glyphManager != null) {
                    return glyphManager.SelectionGlyphs;
                } else {
                    return null; 
                }
            } 
        } 

        private InheritanceUI InheritanceUI { 
            get {
                if (inheritanceUI == null) {
                    inheritanceUI = new InheritanceUI();
                } 
                return inheritanceUI;
            } 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.MenuService"]/*' /> 
        /// <devdoc>
        ///     Retrieves the menu editor service, which we cache for speed.
        /// </devdoc>
        private IMenuCommandService MenuService { 
            get {
                if (menuCommandService == null) { 
                    menuCommandService = (IMenuCommandService)GetService(typeof(IMenuCommandService)); 
                }
                return menuCommandService; 
            }
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ShowLargeIcons"]/*' /> 
        /// <devdoc>
        ///     Determines whether the tray will show large icon view or not. 
        /// </devdoc> 
        public bool ShowLargeIcons {
            get { 
                return showLargeIcons;
            }

            set { 
                if (showLargeIcons != value) {
                    showLargeIcons = value; 
                    menucmdLargeIcons.Checked = ShowLargeIcons; 

                    ResetTrayControls(); 
                    Invalidate(true);
                }
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TabOrderActive"]/*' /> 
        /// <devdoc> 
        ///      Determines if the tab order UI is active.  When tab order is active, the tray is locked in
        ///      a "read only" mode. 
        /// </devdoc>
        private bool TabOrderActive {
            get {
                if (!queriedTabOrder) { 
                    queriedTabOrder = true;
                    IMenuCommandService mcs = MenuService; 
                    if (mcs != null) { 
                        tabOrderCommand = mcs.FindCommand(MenuCommands.TabOrder);
                    } 
                }

                if (tabOrderCommand != null) {
                    return tabOrderCommand.Checked; 
                }
 
                return false; 
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IsWindowVisible"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        ///    <para>Indicates whether the window is visible.</para>
        /// </devdoc> 
        internal bool IsWindowVisible { 
            get {
                if (this.IsHandleCreated) { 
                    return NativeMethods.IsWindowVisible(this.Handle);
                }
                return false;
            } 
        }
 
        internal Size ParentGridSize { 
            get {
                ParentControlDesigner designer = mainDesigner as ParentControlDesigner; 
                if (designer != null) {
                    return designer.ParentGridSize;
                }
 
                return new Size(8, 8);
            } 
        } 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.AddComponent"]/*' />
        /// <devdoc> 
        ///    <para>Adds a component to the tray.</para>
        /// </devdoc>
        public virtual void AddComponent(IComponent component) {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 

            // Ignore components that cannot be added to the tray 
            if (!CanDisplayComponent(component)) { 
                return;
            } 

            // And designate us as the selection UI handler for the
            // control.
            // 
            if (selectionUISvc == null) {
                selectionUISvc = (ISelectionUIService)GetService(typeof(ISelectionUIService)); 
 
                // If there is no selection service, then we will provide our own.
                // 
                if (selectionUISvc == null) {
                    selectionUISvc = new SelectionUIService(host);
                    host.AddService(typeof(ISelectionUIService), selectionUISvc);
                    //privateCommandSet = new CommandSet(mainDesigner.Component.Site); 
                }
 
                grabHandle = selectionUISvc.GetAdornmentDimensions(AdornmentType.GrabHandle); 
            }
 
            // Create a new instance of a tray control.
            //
            TrayControl trayctl = new TrayControl(this, component);
 
            SuspendLayout();
            try { 
                // Add it to us. 
                //
                Controls.Add(trayctl); 
                controls.Add(trayctl);

                // CanExtend can actually be called BEFORE the component is added to the ComponentTray.
                // ToolStrip is such as scenario: 
                // 1. Add a timer to the Tray.
                // 2. Add a ToolStrip. 
                // 3. ToolStripDesigner.Initialize will be called before ComponentTray.AddComponent, 
                //      so the ToolStrip is not yet added to the tray.
                // 4. TooStripDesigner.Initialize calls GetProperties, which causes our CanExtend to be called. 
                // 5. CanExtend will return false, since the component has not yet been added.
                // 6. This causes all sorts of badness/
                // Fix is to refresh.
                TypeDescriptor.Refresh(component); 

                if (host != null && !host.Loading) { 
                    PositionControl(trayctl); 
                }
                if (selectionUISvc != null) { 
                    selectionUISvc.AssignSelectionUIHandler(component, this);
                }

 
                InheritanceAttribute attr = trayctl.InheritanceAttribute;
                if (attr.InheritanceLevel != InheritanceLevel.NotInherited) { 
                    InheritanceUI iui = InheritanceUI; 
                    if (iui != null) {
                        iui.AddInheritedControl(trayctl, attr.InheritanceLevel); 
                    }
                }
            }
            finally { 
                ResumeLayout();
            } 
 
            if (host != null && !host.Loading) {
                ScrollControlIntoView(trayctl); 
            }
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IExtenderProvider.CanExtend"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// <para> 
        /// Gets whether or not this extender provider can extend the given
        /// component. We only extend components that have been added 
        /// to our UI.
        /// </para>
        /// </devdoc>
        bool IExtenderProvider.CanExtend(object component) { 
            IComponent comp = component as IComponent;
            return (comp != null) && (TrayControl.FromComponent(comp) != null); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="CanCreateComponentFromTool"]/*' /> 
        protected virtual bool CanCreateComponentFromTool(ToolboxItem tool) {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(host != null, "Service object could not provide us with a designer host.");
 
            // Disallow controls to be added to the component tray.
            Type compType = host.GetType(tool.TypeName); 
 
            if (compType == null)
                return true; 

            if (!compType.IsSubclassOf(typeof(Control))) {
                return true;
            } 

            Type designerType = GetDesignerType(compType, typeof(IDesigner)); 
 
            if (typeof(ControlDesigner).IsAssignableFrom(designerType)) {
                return false; 
            }

            return true;
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.CanDisplayComponent"]/*' /> 
        /// <devdoc> 
        ///     This method determines if a UI representation for the given component should be provided.
        ///     If it returns true, then the component will get a glyph in the tray area.  If it returns 
        ///     false, then the component will not actually be added to the tray.  The default
        ///     implementation looks for DesignTimeVisibleAttribute.Yes on the component's class.
        /// </devdoc>
        protected virtual bool CanDisplayComponent(IComponent component) { 
            return TypeDescriptor.GetAttributes(component).Contains(DesignTimeVisibleAttribute.Yes);
        } 
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.CreateComponentFromTool"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CreateComponentFromTool(ToolboxItem tool) {
            if (!CanCreateComponentFromTool(tool)) { 
                return;
            } 
 
            // We invoke the drag drop handler for this.  This implementation is shared between all designers that
            // create components. 
            //
            GetOleDragHandler().CreateTool(tool, null, 0, 0, 0, 0, false, false);
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.DisplayError"]/*' />
        /// <devdoc> 
        ///      Displays the given exception to the user. 
        /// </devdoc>
        protected void DisplayError(Exception e) { 
            IUIService uis = (IUIService)GetService(typeof(IUIService));
            if (uis != null) {
                uis.ShowError(e);
            } 
            else {
                string message = e.Message; 
                if (message == null || message.Length == 0) { 
                    message = e.ToString();
                } 
                RTLAwareMessageBox.Show(null, message, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                            MessageBoxDefaultButton.Button1, 0);
            }
        } 

        // 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.Dispose"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Disposes of the resources (other than memory) used by the component tray object.
        ///    </para>
        /// </devdoc>
        protected override void Dispose(bool disposing) { 
            if (disposing && controls != null) {
                IExtenderProviderService es = (IExtenderProviderService)GetService(typeof(IExtenderProviderService)); 
                if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(es != null, "IExtenderProviderService not found"); 
                if (es != null) {
                    es.RemoveExtenderProvider(this); 
                }

                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                if (eventHandlerService != null) { 
                    if (host != null) {
                        host.RemoveService(typeof(IEventHandlerService)); 
                        eventHandlerService = null; 
                    }
                } 

                IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));

                if (componentChangeService != null) { 
                    componentChangeService.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved);
                } 
 
                TypeDescriptor.Refreshed -= new RefreshEventHandler(OnComponentRefresh);
                SystemEvents.DisplaySettingsChanged -= new EventHandler(this.OnSystemSettingChanged); 
                SystemEvents.InstalledFontsChanged -= new EventHandler(this.OnSystemSettingChanged);
                SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(this.OnUserPreferenceChanged);

                IMenuCommandService mcs = MenuService; 
                if (mcs != null) {
                    Debug.Assert(menucmdArrangeIcons != null, "Null Menu Command for ArrangeIcons"); 
                    Debug.Assert(menucmdLineupIcons  != null, "Null Menu Command for LineupIcons"); 
                    Debug.Assert(menucmdLargeIcons   != null, "Null Menu Command for LargeIcons");
                    mcs.RemoveCommand(menucmdArrangeIcons); 
                    mcs.RemoveCommand(menucmdLineupIcons);
                    mcs.RemoveCommand(menucmdLargeIcons);
                }
 
                if (privateCommandSet != null) {
                    privateCommandSet.Dispose(); 
 
                    // If we created a private command set, we also added a selection ui service to the host
                    if (host != null) { 
                        host.RemoveService(typeof(ISelectionUIService));
                    }

                } 
                selectionUISvc = null;
 
                if (inheritanceUI != null) { 
                    inheritanceUI.Dispose();
                    inheritanceUI = null; 
                }

                serviceProvider = null;
                controls.Clear(); 
                controls = null;
 
                if (glyphManager != null) { 
                    glyphManager.Dispose();
                    glyphManager = null; 
                }
            }
            base.Dispose(disposing);
        } 

        private void DoAutoArrange(bool dirtyDesigner) { 
            if (controls == null || controls.Count <= 0) { 
                return;
            } 

            controls.Sort(new AutoArrangeComparer());

            SuspendLayout(); 

            //Reset the autoscroll position before auto arranging. 
            //This way, when OnLayout gets fired after this, we won't 
            //have to move every component again.  Note that sync'ing
            //the selection will automatically select & scroll into view 
            //the right components
            this.AutoScrollPosition = new Point(0,0);

            try { 
                Control prevCtl = null;
                bool positionedGlobal = true; 
                foreach(Control ctl in controls) { 
                    if (!ctl.Visible)
                        continue; 

                    // If we're auto arranging, always move the control.  If not,
                    // move the control only if it was never given a position.  This
                    // auto arranges it until the user messes with it, or until its 
                    // position is saved into the resx.
                    // (if one control is no longer positioned, move all the other one as 
                    // we don't want them to go under one another) 
                    if (autoArrange) {
                        PositionInNextAutoSlot(ctl as TrayControl, prevCtl, dirtyDesigner); 
                    }
                    else if (!((TrayControl)ctl).Positioned || !positionedGlobal) {
                        PositionInNextAutoSlot(ctl as TrayControl, prevCtl, false);
                        positionedGlobal = false; 
                    }
                    prevCtl = ctl; 
                } 

                if (selectionUISvc != null) { 
                    selectionUISvc.SyncSelection();
                }
            }
            finally { 
                ResumeLayout();
            } 
        } 

        private void DoLineupIcons() { 
            if (autoArrange)
                return;

            bool oldValue = autoArrange; 
            autoArrange = true;
 
            try { 
                DoAutoArrange(true);
            } 
            finally {
                autoArrange = oldValue;
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.DrawRubber"]/*' /> 
        /// <devdoc> 
        ///      Draws a rubber band at the given coordinates.  The coordinates
        ///      can be transposed. 
        /// </devdoc>
        private void DrawRubber(Point start, Point end) {
            mouseDragWorkspace.X = Math.Min(start.X, end.X);
            mouseDragWorkspace.Y = Math.Min(start.Y, end.Y); 
            mouseDragWorkspace.Width = Math.Abs(end.X - start.X);
            mouseDragWorkspace.Height = Math.Abs(end.Y - start.Y); 
 
            mouseDragWorkspace = RectangleToScreen(mouseDragWorkspace);
 
            ControlPaint.DrawReversibleFrame(mouseDragWorkspace, BackColor, FrameStyle.Dashed);
        }

        internal void FocusDesigner() { 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host != null && host.RootComponent != null) { 
                IRootDesigner rd = host.GetDesigner(host.RootComponent) as IRootDesigner; 
                if (rd != null) {
                    ViewTechnology[] techs = rd.SupportedTechnologies; 
                    if (techs.Length > 0) {
                        Control view = rd.GetView(techs[0]) as Control;
                        if (view != null) {
                            view.Focus(); 
                        }
                    } 
                } 
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetComponentsInRect"]/*' />
        /// <devdoc>
        ///     Finds the array of components within the given rectangle.  This uses the rectangle to 
        ///     find controls within our frame, and then uses those controls to find the actual
        ///     components.  It returns an object array so the output can be directly fed into 
        ///     the selection service. 
        /// </devdoc>
        private object[] GetComponentsInRect(Rectangle rect) { 
            ArrayList list = new ArrayList();

            int controlCount = Controls.Count;
 
            for (int i = 0; i < controlCount; i++) {
                Control child = Controls[i]; 
                Rectangle bounds = child.Bounds; 
                TrayControl tc = child as TrayControl;
                if (tc != null && bounds.IntersectsWith(rect)) { 
                    list.Add(tc.Component);
                }
            }
 
            return list.ToArray();
        } 
 
        private Type GetDesignerType(Type t, Type designerBaseType)
        { 
            Type designerType = null;

            // Get the set of attributes for this type
            // 
            AttributeCollection attributes = TypeDescriptor.GetAttributes(t);
 
            for (int i = 0; i < attributes.Count; i++) 
            {
                DesignerAttribute da = attributes[i] as DesignerAttribute; 
                if (da != null)
                {
                    Type attributeBaseType = Type.GetType(da.DesignerBaseTypeName);
                    if (attributeBaseType != null && attributeBaseType == designerBaseType) 
                    {
                        bool foundService = false; 
 
                        ITypeResolutionService tr = (ITypeResolutionService)GetService(typeof(ITypeResolutionService));
                        if (tr != null) 
                        {
                            foundService = true;
                            designerType = tr.GetType(da.DesignerTypeName);
                        } 

                        if (!foundService) 
                        { 
                            designerType = Type.GetType(da.DesignerTypeName);
                        } 

                        if (designerType != null)
                        {
                            break; 
                        }
                    } 
                } 
            }
 
            return designerType;
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetDragDimensions"]/*' /> 
        /// <devdoc>
        ///     Returns the drag dimensions needed to move the currently selected 
        ///     component one way or the other. 
        /// </devdoc>
        internal Size GetDragDimensions() { 

            // This is a really gross approximation of the correct diemensions.
            //
            if (AutoArrange) { 
                ISelectionService ss = (ISelectionService)GetService(typeof(ISelectionService));
                IComponent comp = null; 
 
                if (ss != null) {
                    comp = (IComponent)ss.PrimarySelection; 
                }

                Control control = null;
 
                if (comp != null) {
                    control = ((IOleDragClient)this).GetControlForComponent(comp); 
                } 

                if (control == null && controls.Count > 0) { 
                    control = (Control)controls[0];
                }

                if (control != null) { 
                    Size s = control.Size;
                    s.Width += 2 * whiteSpace.X; 
                    s.Height += 2 * whiteSpace.Y; 
                    return s;
                } 
            }

            return new Size(10, 10);
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetNextComponent"]/*' /> 
        /// <devdoc> 
        ///     Similar to GetNextControl on Control, this method returns the next
        ///     component in the tray, given a starting component.  It will return 
        ///     null if the end (or beginning, if forward is false) of the list
        ///     is encountered.
        /// </devdoc>
        public IComponent GetNextComponent(IComponent component, bool forward) { 

            for (int i = 0; i < controls.Count; i++) { 
                TrayControl control = (TrayControl)controls[i]; 
                if (control.Component == component) {
 
                    int targetIndex = (forward ? i + 1 : i - 1);

                    if (targetIndex >= 0 && targetIndex < controls.Count) {
                        return((TrayControl)controls[targetIndex]).Component; 
                    }
 
                    // Reached the end of the road. 
                    return null;
                } 
            }

            // If we got here then the component isn't in our list.  Prime the
            // caller with either the first or the last. 

            if (controls.Count > 0) { 
                int targetIndex = (forward ? 0 : controls.Count -1); 
                return((TrayControl)controls[targetIndex]).Component;
            } 

            return null;
        }
 
        internal virtual OleDragDropHandler GetOleDragHandler() {
            if (oleDragDropHandler == null) { 
                oleDragDropHandler = new TrayOleDragDropHandler(this.DragHandler, this.serviceProvider, this); 
            }
            return oleDragDropHandler; 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetLocation"]/*' />
        /// <devdoc> 
        ///     Accessor method for the location extender property.  We offer this extender
        ///     to all non-visual components. 
        /// </devdoc> 
        [
            Category("Layout"), 
            Localizable(false),
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            SRDescription("ControlLocationDescr"), 
            DesignOnly(true),
        ] 
 	    [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")] 
        public Point GetLocation(IComponent receiver) {
            // We shouldn't really end up here, but if we do.... 

            PropertyDescriptor loc = TypeDescriptor.GetProperties(receiver.GetType())["Location"];
            if (loc != null) {
                // In this case the component already had a Location property, and what the caller 
                // wants is the underlying components Location, not the tray location. Why?
                // Because we now use TrayLocation. 
                return (Point)(loc.GetValue(receiver)); 
            }
            else { 
                // If the component didn't already have a Location property, then the caller
                // really wants the tray location. Could be a 3rd party vendor.
                return GetTrayLocation(receiver);
            } 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetLocation"]/*' /> 
        /// <devdoc>
        ///     Accessor method for the location extender property.  We offer this extender 
        ///     to all non-visual components.
        /// </devdoc>
        [
            Category("Layout"), 
            Localizable(false),
            Browsable(false), 
            SRDescription("ControlLocationDescr"), 
            DesignOnly(true),
        ] 
        public Point GetTrayLocation(IComponent receiver) {
            Control c = TrayControl.FromComponent(receiver);

            if (c == null) { 
                Debug.Fail("Anything we're extending should have a component view.");
                return new Point(); 
            } 

            Point loc = c.Location; 
            Point autoScrollLoc = this.AutoScrollPosition;

            return new Point(loc.X - autoScrollLoc.X, loc.Y - autoScrollLoc.Y);
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetService"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Gets the requsted service type. 
        ///    </para>
        /// </devdoc>
        protected override object GetService(Type serviceType) {
            object service = null; 

            Debug.Assert(serviceProvider != null, "Trying to access services too late or too early."); 
            if (serviceProvider != null) { 
                service = serviceProvider.GetService(serviceType);
            } 

            return service;
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetTrayControlFromComponent"]/*' />
        /// <devdoc> 
        ///     Returns the traycontrol representing the IComponent.  If no 
        ///     traycontrol is found, this returns null.  This is used identify
        ///     bounds for the DesignerAction UI. 
        /// </devdoc>
        internal TrayControl GetTrayControlFromComponent(IComponent comp) {
            return TrayControl.FromComponent(comp);
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IsTrayComponent"]/*' /> 
        /// <devdoc> 
        /// Returns true if the given componenent is being shown on the tray.
        /// </devdoc> 
        public bool IsTrayComponent(IComponent comp) {

            if (TrayControl.FromComponent(comp) == null) {
                return false; 
            }
 
            foreach (Control control in this.Controls) { 
                TrayControl tc = control as TrayControl;
                if (tc != null && tc.Component == comp) { 
                    return true;
                }
            }
 
            return false;
        } 
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnComponentRefresh"]/*' />
        /// <devdoc> 
        ///     Called when a component's metadata is invalidated.  We re-query here and will show/hide
        ///     the control's tray control based on the new metadata.
        /// </devdoc>
        private void OnComponentRefresh(RefreshEventArgs e) { 
            IComponent component = e.ComponentChanged as IComponent;
 
            if (component != null) { 
                TrayControl control = TrayControl.FromComponent(component);
 
                if (control != null) {
                    bool shouldDisplay = CanDisplayComponent(component);
                    if (shouldDisplay != control.Visible || !shouldDisplay) {
                        control.Visible = shouldDisplay; 
                        Rectangle bounds = control.Bounds;
                        bounds.Inflate(grabHandle); 
                        bounds.Inflate(grabHandle); 
                        Invalidate(bounds);
                        PerformLayout(); 
                    }
                }
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnComponentRemoved"]/*' /> 
        /// <devdoc> 
        ///      Called when a component is removed from the container.
        /// </devdoc> 
        private void OnComponentRemoved(object sender, ComponentEventArgs cevent) {
            RemoveComponent(cevent.Component);
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnComponentTrayPaste"]/*' />
        /// <devdoc> 
        ///      Called from CommandSet's OnMenuPaste method.  This will allow us to properly adjust the location 
        ///      of the components in the tray after we've incorreclty set them by deserializing the design time
        ///      properties (and hence called SetValue(c, myBadLocation) on the location property). 
        /// </devdoc>
        internal void UpdatePastePositions(ArrayList components) {
            foreach (TrayControl c in components) {
                if (!CanDisplayComponent(c.Component)) { 
                    return;
                } 
 
                if (mouseDropLocation == InvalidPoint) {
 
                    Control prevCtl = null;
                    if (controls.Count > 1) {
                        prevCtl = (Control)controls[controls.Count-1];
                    } 
                    PositionInNextAutoSlot(c, prevCtl, true);
                } 
                else { 
                    PositionControl(c);
                } 
                c.BringToFront();
            }
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnContextMenu"]/*' />
        /// <devdoc> 
        ///     Called when we are to display our context menu for this component. 
        /// </devdoc>
        private void OnContextMenu(int x, int y, bool useSelection) { 

            if (!TabOrderActive) {
                Capture = false;
 
                IMenuCommandService mcs = MenuService;
                if (mcs != null) { 
                    Capture = false; 
                    Cursor.Clip = Rectangle.Empty;
 
                    ISelectionService s = (ISelectionService)GetService(typeof(ISelectionService));


 
                    if (useSelection && s != null && !(1 == s.SelectionCount && s.PrimarySelection == mainDesigner.Component)) {
                        mcs.ShowContextMenu(MenuCommands.TraySelectionMenu, x, y); 
                    } 
                    else {
                        mcs.ShowContextMenu(MenuCommands.ComponentTrayMenu, x, y); 
                    }
                }
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnDoubleClick"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected override void OnMouseDoubleClick(MouseEventArgs e) {
            //give our glyphs first chance at this
            if (glyphManager != null && glyphManager.OnMouseDoubleClick(e)) {
                //handled by a glyph - so don't send to the comp tray 
                return;
            } 
 
            base.OnDoubleClick(e);
 
            if (!TabOrderActive) {
                OnLostCapture();
                IEventBindingService eps = (IEventBindingService)GetService(typeof(IEventBindingService));
                if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(eps != null, "IEventBindingService not found"); 
                if (eps != null) {
                    eps.ShowCode(); 
                } 
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnGiveFeedback"]/*' />
        /// <devdoc>
        ///     Inheriting classes should override this method to handle this event. 
        ///     Call base.onGiveFeedback to send this event to any registered event listeners.
        /// </devdoc> 
        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfevent) { 
            base.OnGiveFeedback(gfevent);
            GetOleDragHandler().DoOleGiveFeedback(gfevent); 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnDragDrop"]/*' />
        /// <devdoc> 
        ///      Called in response to a drag drop for OLE drag and drop.  Here we
        ///      drop a toolbox component on our parent control. 
        /// </devdoc> 
        protected override void OnDragDrop(DragEventArgs de) {
            // This will be used once during PositionComponent to place the component 
            // at the drop point.  It is automatically set to null afterwards, so further
            // components appear after the first one dropped.
            //
            mouseDropLocation = PointToClient(new Point(de.X, de.Y)); 
            autoScrollPosBeforeDragging = this.AutoScrollPosition;//save the scroll position
 
            if (mouseDragTool != null) { 
                ToolboxItem tool = mouseDragTool;
                mouseDragTool = null; 

                if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(GetService(typeof(IDesignerHost)) != null, "IDesignerHost not found");

                try { 
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                    IDesigner designer = host.GetDesigner(host.RootComponent); 
 
                    IToolboxUser itu = designer as IToolboxUser;
                    if (itu != null) { 
                        itu.ToolPicked(tool);
                    }
                    else {
                        CreateComponentFromTool(tool); 
                    }
                } 
                catch (Exception e) { 
                    DisplayError(e);
                    if (ClientUtils.IsCriticalException(e)) { 
                        throw;
                    }
                }
                catch { 
                }
 
                de.Effect = DragDropEffects.Copy; 

            } 
            else {
                GetOleDragHandler().DoOleDragDrop(de);
            }
 
            mouseDropLocation = InvalidPoint;
            ResumeLayout(); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnDragEnter"]/*' /> 
        /// <devdoc>
        ///      Called in response to a drag enter for OLE drag and drop.
        /// </devdoc>
        protected override void OnDragEnter(DragEventArgs de) { 
            if (!TabOrderActive) {
                SuspendLayout(); 
                if (toolboxService == null) { 
                    toolboxService = (IToolboxService)GetService(typeof(IToolboxService));
                } 

                OleDragDropHandler dragDropHandler = GetOleDragHandler();
                Object[] dragComps = dragDropHandler.GetDraggingObjects(de);
 
                // Only assume the items came from the ToolBox if dragComps == null
                // 
                if (toolboxService != null && dragComps == null) { 
                    mouseDragTool = toolboxService.DeserializeToolboxItem(de.Data, (IDesignerHost)GetService(typeof(IDesignerHost)));
                } 

                if (mouseDragTool != null) {
                    Debug.Assert(0 != (int)(de.AllowedEffect & (DragDropEffects.Move | DragDropEffects.Copy)), "DragDropEffect.Move | .Copy isn't allowed?");
                    if ((int)(de.AllowedEffect & DragDropEffects.Move) != 0) { 
                        de.Effect = DragDropEffects.Move;
                    } 
                    else { 
                        de.Effect = DragDropEffects.Copy;
                    } 
                }
                else {
                    dragDropHandler.DoOleDragEnter(de);
                } 
            }
        } 
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnDragLeave"]/*' />
        /// <devdoc> 
        ///     Called when a drag-drop operation leaves the control designer view
        ///
        /// </devdoc>
        protected override void OnDragLeave(EventArgs e) { 
            mouseDragTool = null;
            GetOleDragHandler().DoOleDragLeave(); 
            ResumeLayout(); 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnDragOver"]/*' />
        /// <devdoc>
        ///     Called when a drag drop object is dragged over the control designer view
        /// </devdoc> 
        protected override void OnDragOver(DragEventArgs de) {
            if (mouseDragTool != null) { 
                Debug.Assert(0!=(int)(de.AllowedEffect & DragDropEffects.Copy), "DragDropEffect.Move isn't allowed?"); 
                de.Effect = DragDropEffects.Copy;
            } 
            else {
                GetOleDragHandler().DoOleDragOver(de);
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnLayout"]/*' /> 
        /// <internalonly/> 
        /// <devdoc>
        ///    Forces the layout of any docked or anchored child controls. 
        /// </devdoc>
        protected override void OnLayout(LayoutEventArgs levent) {
            DoAutoArrange(false);
            // make sure selection service redraws 
            Invalidate(true);
            base.OnLayout(levent); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnLostCapture"]/*' /> 
        /// <devdoc>
        ///      This is called when we lose capture.  Here we get rid of any
        ///      rubber band we were drawing.  You should put any cleanup
        ///      code in here. 
        /// </devdoc>
        protected virtual void OnLostCapture() { 
            if (mouseDragStart != InvalidPoint) { 
                Cursor.Clip = Rectangle.Empty;
                if (mouseDragEnd != InvalidPoint) { 
                    DrawRubber(mouseDragStart, mouseDragEnd);
                    mouseDragEnd = InvalidPoint;
                }
                mouseDragStart = InvalidPoint; 
            }
        } 
 
        private void OnMenuArrangeIcons(object sender, EventArgs e) {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            DesignerTransaction t = null;

            try {
                t = host.CreateTransaction(SR.GetString(SR.TrayAutoArrange)); 

                PropertyDescriptor trayAAProp = TypeDescriptor.GetProperties(mainDesigner.Component)["TrayAutoArrange"]; 
                if (trayAAProp != null) { 
                    trayAAProp.SetValue(mainDesigner.Component, !AutoArrange);
                } 
            }
            finally {
                if (t != null)
                    t.Commit(); 
            }
        } 
 
        private void OnMenuShowLargeIcons(object sender, EventArgs e) {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            DesignerTransaction t = null;

            try {
                t = host.CreateTransaction(SR.GetString(SR.TrayShowLargeIcons)); 
                PropertyDescriptor trayIconProp = TypeDescriptor.GetProperties(mainDesigner.Component)["TrayLargeIcon"];
                if (trayIconProp != null) { 
                    trayIconProp.SetValue(mainDesigner.Component, !ShowLargeIcons); 
                }
            } 
            finally {
                if (t != null)
                    t.Commit();
            } 
        }
 
        private void OnMenuLineupIcons(object sender, EventArgs e) { 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            DesignerTransaction t = null; 
            try {
                t = host.CreateTransaction(SR.GetString(SR.TrayLineUpIcons));
                DoLineupIcons();
            } 
            finally {
                if (t != null) 
                    t.Commit(); 
            }
        } 

        /// <devdoc>
        ///     Used to forward messages from the related ComponentTray Glyph
        ///     to this ComponentTray class. 
        /// </devdoc>
        internal void OnMessage(ref Message m) { 
            this.WndProc(ref m); 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnMouseDown"]/*' />
        /// <devdoc>
        ///     Inheriting classes should override this method to handle this event.
        ///     Call base.onMouseDown to send this event to any registered event listeners. 
        /// </devdoc>
        protected override void OnMouseDown(MouseEventArgs e) { 
 
            //give our glyphs first chance at this
            if (glyphManager != null && glyphManager.OnMouseDown(e)) { 
                //handled by a glyph - so don't send to the comp tray
                return;
            }
 
            base.OnMouseDown(e);
 
            if (!TabOrderActive) { 
                if (toolboxService == null) {
                    toolboxService = (IToolboxService)GetService(typeof(IToolboxService)); 
                }


                FocusDesigner(); 

                if (e.Button == MouseButtons.Left && toolboxService != null) { 
                    ToolboxItem tool = toolboxService.GetSelectedToolboxItem((IDesignerHost)GetService(typeof(IDesignerHost))); 
                    if (tool != null) {
                        // mouseDropLocation is checked in PositionControl, which should get called as a result of adding a new 
                        // component.  This allows us to set the position without flickering, while still providing support for auto
                        // layout if the control was double clicked or added through extensibility.
                        //
                        mouseDropLocation = new Point(e.X, e.Y); 
                        try {
                            CreateComponentFromTool(tool); 
                            toolboxService.SelectedToolboxItemUsed(); 
                        }
                        catch (Exception ex) { 
                            DisplayError(ex);
                            if (ClientUtils.IsCriticalException(ex)) {
                                throw;
                            } 
                        }
                        catch { 
                        } 
                        mouseDropLocation = InvalidPoint;
                        return; 
                    }
                }

                // If it is the left button, start a rubber band drag to laso 
                // controls.
                // 
                if (e.Button == MouseButtons.Left) { 
                    mouseDragStart = new Point(e.X, e.Y);
                    Capture = true; 
                    Cursor.Clip = RectangleToScreen(ClientRectangle);

                }
                else { 
                    try {
                        ISelectionService ss = (ISelectionService)GetService(typeof(ISelectionService)); 
                        if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(ss != null, "ISelectionService not found"); 
                        if (ss != null) {
                            ss.SetSelectedComponents(new object[] {mainDesigner.Component}); 
                        }
                    }
                    catch (Exception ex) {
                        // nothing we can really do here; just eat it. 
                        if (ClientUtils.IsCriticalException(ex)) {
                            throw; 
                        } 
                    }
 
                    catch {
                    }
                }
            } 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnMouseMove"]/*' /> 
        /// <devdoc>
        ///     Inheriting classes should override this method to handle this event. 
        ///     Call base.onMouseMove to send this event to any registered event listeners.
        /// </devdoc>
        protected override void OnMouseMove(MouseEventArgs e) {
 
            //give our glyphs first chance at this
            if (glyphManager != null && glyphManager.OnMouseMove(e)) { 
                //handled by a glyph - so don't send to the comp tray 
                return;
            } 

            base.OnMouseMove(e);

            // If we are dragging, then draw our little rubber band. 
            //
            if (mouseDragStart != InvalidPoint) { 
                if (mouseDragEnd != InvalidPoint) { 
                    DrawRubber(mouseDragStart, mouseDragEnd);
                } 
                else {
                    mouseDragEnd = new Point(0, 0);
                }
 
                mouseDragEnd.X = e.X;
                mouseDragEnd.Y = e.Y; 
                DrawRubber(mouseDragStart, mouseDragEnd); 
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnMouseUp"]/*' />
        /// <devdoc>
        ///     Inheriting classes should override this method to handle this event. 
        ///     Call base.onMouseUp to send this event to any registered event listeners.
        /// </devdoc> 
        protected override void OnMouseUp(MouseEventArgs e) { 

            //give our glyphs first chance at this 
            if (glyphManager != null && glyphManager.OnMouseUp(e)) {
                //handled by a glyph - so don't send to the comp tray
                return;
            } 

            if (mouseDragStart != InvalidPoint && e.Button == MouseButtons.Left) { 
                object[] comps = null; 

                Capture = false; 
                Cursor.Clip = Rectangle.Empty;

                if (mouseDragEnd != InvalidPoint) {
                    DrawRubber(mouseDragStart, mouseDragEnd); 

                    Rectangle rect = new Rectangle(); 
                    rect.X = Math.Min(mouseDragStart.X, e.X); 
                    rect.Y = Math.Min(mouseDragStart.Y, e.Y);
                    rect.Width = Math.Abs(e.X - mouseDragStart.X); 
                    rect.Height = Math.Abs(e.Y - mouseDragStart.Y);
                    comps = GetComponentsInRect(rect);
                    mouseDragEnd = InvalidPoint;
                } 
                else {
                    comps = new object[0]; 
                } 

                if (comps.Length == 0) { 
                    comps = new object[] {mainDesigner.Component};
                }

                try { 
                    ISelectionService ss = (ISelectionService)GetService(typeof(ISelectionService));
                    if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(ss != null, "ISelectionService not found"); 
                    if (ss != null) { 
                        ss.SetSelectedComponents(comps);
                    } 
                }
                catch (Exception ex) {
                    // nothing we can really do here; just eat it.
                    if (ClientUtils.IsCriticalException(ex)) { 
                        throw;
                    } 
                } 
                catch {
                } 

                mouseDragStart = InvalidPoint;
            }
 

            base.OnMouseUp(e); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnPaint"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnPaint(PaintEventArgs pe) { 
            if (fResetAmbient) {
                fResetAmbient = false; 
 
                IUIService uiService = (IUIService)GetService(typeof(IUIService));
                if (uiService != null) { 
                    Color styleColor;
                    //Can't use 'as' here since Color is a value type
                    if (uiService.Styles["VsColorDesignerTray"] is Color) {
                        styleColor = (Color) uiService.Styles["VsColorDesignerTray"]; 
                    }
                    else if (uiService.Styles["HighlightColor"] is Color) { 
                        // Since v1, we have had code here that checks for HighlightColor, so some hosts (like WinRes) 
                        // have been setting it. If VsColorDesignerTray isn't present, we look for HighlightColor
                        // for backward compat. 
                        styleColor = (Color) uiService.Styles["HighlightColor"];
                    }
                    else {
                        //No style color provided? Let's pick a default. 
                        styleColor = SystemColors.Info;
                    } 
 
                    BackColor = styleColor;
                    Font = (Font)uiService.Styles["DialogFont"]; 
                }
            }

            base.OnPaint(pe); 

            Graphics gr = pe.Graphics; 
 
            // Now, if we have a selection, paint it
            // 
            if (selectedObjects != null) {
                bool first = true;//indicates the first iteration of our foreach loop
                foreach(object o in selectedObjects) {
                    Control c = ((IOleDragClient)this).GetControlForComponent(o); 
                    if (c != null && c.Visible) {
 
                        Rectangle innerRect = c.Bounds; 

                        NoResizeHandleGlyph glyph = new NoResizeHandleGlyph(innerRect, SelectionRules.None, first, null); 

                        DesignerUtils.DrawSelectionBorder(gr, DesignerUtils.GetBoundsForNoResizeSelectionType(innerRect, SelectionBorderGlyphType.Top));
                        DesignerUtils.DrawSelectionBorder(gr, DesignerUtils.GetBoundsForNoResizeSelectionType(innerRect, SelectionBorderGlyphType.Bottom));
                        DesignerUtils.DrawSelectionBorder(gr, DesignerUtils.GetBoundsForNoResizeSelectionType(innerRect, SelectionBorderGlyphType.Left)); 
                        DesignerUtils.DrawSelectionBorder(gr, DesignerUtils.GetBoundsForNoResizeSelectionType(innerRect, SelectionBorderGlyphType.Right));
                        // Need to draw this one last 
                        DesignerUtils.DrawNoResizeHandle(gr, glyph.Bounds, first, glyph); 
                    }
                    first = false; 
                }
            }
            //paint any glyphs
            if(glyphManager != null) { 
                glyphManager.OnPaintGlyphs(pe);
            } 
        } 

        private void OnSelectionChanged(object sender, EventArgs e) { 
            selectedObjects = ((ISelectionService)sender).GetSelectedComponents();
            object primary = ((ISelectionService)sender).PrimarySelection;
            Invalidate();
 
            // Accessibility information
            // 
 
            foreach(object selObj in selectedObjects) {
                IComponent component = selObj as IComponent; 
                if (component != null) {
                    Control c = TrayControl.FromComponent(component);
                    if (c != null) {
                        Debug.WriteLineIf(CompModSwitches.MSAA.TraceInfo, "MSAA: SelectionAdd, traycontrol = " + c.ToString()); 
                        UnsafeNativeMethods.NotifyWinEvent((int)AccessibleEvents.SelectionAdd, new HandleRef(c, c.Handle), NativeMethods.OBJID_CLIENT, 0);
                    } 
                } 
            }
 
            IComponent comp = primary as IComponent;
            if (comp != null) {
                Control c = TrayControl.FromComponent(comp);
                if (c != null && IsHandleCreated) { 
                    this.ScrollControlIntoView(c);
                    UnsafeNativeMethods.NotifyWinEvent((int)AccessibleEvents.Focus, new HandleRef(c, c.Handle), NativeMethods.OBJID_CLIENT, 0); 
                } 
                if(glyphManager != null) {
                    glyphManager.SelectionGlyphs.Clear(); 
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                    foreach(object selObj in selectedObjects) {
                        IComponent selectedComponent = selObj as IComponent;
                        if(selectedComponent!= null && !(host.GetDesigner(selectedComponent) is ControlDesigner)) { // don't want to do it for controls that are also in the tray 
                            GlyphCollection glyphs = glyphManager.GetGlyphsForComponent(selectedComponent);
                            if (glyphs != null && glyphs.Count > 0) { 
                                SelectionGlyphs.AddRange(glyphs); 
                            }
                        } 
                    }
                }
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnSetCursor"]/*' /> 
        /// <devdoc> 
        ///      Sets the cursor.  You may override this to set your own
        ///      cursor. 
        /// </devdoc>
        protected virtual void OnSetCursor() {
            if (toolboxService == null) {
                toolboxService = (IToolboxService)GetService(typeof(IToolboxService)); 
            }
 
            if (toolboxService == null || !toolboxService.SetCursor()) { 
                Cursor.Current = Cursors.Default;
            } 
        }

        private delegate void AsyncInvokeHandler(bool children);
 
        private void OnSystemSettingChanged(object sender, EventArgs e) {
            fResetAmbient = true; 
            ResetTrayControls(); 
            BeginInvoke(new AsyncInvokeHandler(Invalidate), new object[] {true});
        } 

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
            fResetAmbient = true;
            ResetTrayControls(); 
            BeginInvoke(new AsyncInvokeHandler(Invalidate), new object[] {true});
        } 
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.PositionControl"]/*' />
        /// <devdoc> 
        ///      Sets the given control to the correct position on our
        ///      surface.  You may override this to perform your own
        ///      positioning.
        /// </devdoc> 
        private void PositionControl(TrayControl c) {
            Debug.Assert(c.Visible, "TrayControl for " + c.Component + " should not be positioned"); 
 
            if (!autoArrange) {
                if (mouseDropLocation != InvalidPoint) { 
                    if (!c.Location.Equals(mouseDropLocation))
                        c.Location = mouseDropLocation;
                }
                else { 
                    Control prevCtl = null;
                    if (controls.Count > 1) { 
                        // PositionControl can be called when all the controls have been added 
                        // (from IOleDragClient.AddComponent), so we can't use the old
                        // way of looking up the previous control (prevCtl = controls[controls.Count - 2] 
                        int index = controls.IndexOf(c);
                        Debug.Assert(index >= 1, "Got the wrong index, how could that be?");
                        if (index >= 1) {
                            prevCtl = (Control)controls[index - 1]; 
                        }
                    } 
                    PositionInNextAutoSlot(c, prevCtl, true); 
                }
            } 
            else {
                if (mouseDropLocation != InvalidPoint) {
                    RearrangeInAutoSlots(c, mouseDropLocation);
                } 
                else {
                    Control prevCtl = null; 
                    if (controls.Count > 1) { 
                        int index = controls.IndexOf(c);
                        Debug.Assert(index >= 1, "Got the wrong index, how could that be?"); 
                        if (index >= 1) {
                            prevCtl = (Control)controls[index - 1];
                        }
                    } 
                    PositionInNextAutoSlot(c, prevCtl, true);
                } 
            } 

        } 

        private bool PositionInNextAutoSlot(TrayControl c, Control prevCtl, bool dirtyDesigner) {
            Debug.Assert(c.Visible, "TrayControl for " + c.Component + " should not be positioned");
 
            if (whiteSpace.IsEmpty) {
                Debug.Assert(selectionUISvc != null, "No SelectionUIService available for tray."); 
                whiteSpace = new Point(selectionUISvc.GetAdornmentDimensions(AdornmentType.GrabHandle)); 
                whiteSpace.X = whiteSpace.X * 2 + 3;
                whiteSpace.Y = whiteSpace.Y * 2 + 3; 
            }

            if (prevCtl == null) {
                Rectangle display = DisplayRectangle; 
                Point newLoc = new Point(display.X + whiteSpace.X, display.Y + whiteSpace.Y);
                if (!c.Location.Equals(newLoc)) { 
                    c.Location = newLoc; 
                    if (dirtyDesigner) {
                        IComponent comp = c.Component; 
                        Debug.Assert(comp != null, "Component for the TrayControl is null");

                        PropertyDescriptor ctlLocation = TypeDescriptor.GetProperties(comp)["TrayLocation"];
                        if (ctlLocation != null) { 
                            Point autoScrollLoc = this.AutoScrollPosition;
                            newLoc = new Point(newLoc.X - autoScrollLoc.X, newLoc.Y - autoScrollLoc.Y); 
                            ctlLocation.SetValue(comp, newLoc); 
                        }
                    } 
                    else {
                        c.Location = newLoc;
                    }
                    return true; 
                }
            } 
            else { 
                // Calcuate the next location for this control.
                // 
                Rectangle bounds = prevCtl.Bounds;
                Point newLoc = new Point(bounds.X + bounds.Width + whiteSpace.X, bounds.Y);

                // Check to see if it goes over the edge of our window.  If it does, 
                // then wrap it.
                // 
                if (newLoc.X + c.Size.Width > Size.Width) { 
                    newLoc.X = whiteSpace.X;
                    newLoc.Y += bounds.Height + whiteSpace.Y; 
                }

                if (!c.Location.Equals(newLoc)) {
                    if (dirtyDesigner) { 
                        IComponent comp = c.Component;
                        Debug.Assert(comp != null, "Component for the TrayControl is null"); 
 
                        PropertyDescriptor ctlLocation = TypeDescriptor.GetProperties(comp)["TrayLocation"];
                        if (ctlLocation != null) { 
                            Point autoScrollLoc = this.AutoScrollPosition;
                            newLoc = new Point(newLoc.X - autoScrollLoc.X, newLoc.Y - autoScrollLoc.Y);
                            ctlLocation.SetValue(comp, newLoc);
                        } 
                    }
                    else { 
                        c.Location = newLoc; 
                    }
                    return true; 
                }
            }

            return false; 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.RemoveComponent"]/*' /> 
        /// <devdoc>
        ///      Removes a component from the tray. 
        /// </devdoc>
        public virtual void RemoveComponent(IComponent component) {
            TrayControl c = TrayControl.FromComponent(component);
            if (c != null) { 
                try {
                    InheritanceAttribute attr = c.InheritanceAttribute; 
                    if (attr.InheritanceLevel != InheritanceLevel.NotInherited && inheritanceUI != null) { 
                        inheritanceUI.RemoveInheritedControl(c);
                    } 

                    if (controls != null) {
                        int index = controls.IndexOf(c);
                        if (index != -1) 
                            controls.RemoveAt(index);
                    } 
                } 
                finally {
                    c.Dispose(); 
                }
            }
        }
 
        private void ResetTrayControls() {
            ControlCollection children = (ControlCollection)this.Controls; 
            if (children == null) 
                return;
 
            for (int i = 0; i < children.Count; ++i) {
                TrayControl tc = children[i] as TrayControl;
                if (tc != null) {
                    tc.fRecompute = true; 
                }
            } 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.SetLocation"]/*' /> 
        /// <devdoc>
        ///     Accessor method for the location extender property.  We offer this extender
        ///     to all non-visual components.
        /// </devdoc> 
        public void SetLocation(IComponent receiver, Point location) {
            // This really should only be called when we are loading. 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            if (host != null && host.Loading) {
                // If we are loading, and we get called here, that's because we have provided 
                // the extended Location property. In this case we are loading an old project,
                // and what we are really setting is the tray location.
                SetTrayLocation(receiver, location);
            } 
            else {
                // we are not loading 
                PropertyDescriptor loc = TypeDescriptor.GetProperties(receiver.GetType())["Location"]; 
                if (loc != null) {
                    // so if the component already had the Location property, what the caller wants 
                    // is really the underlying component's Location property.
                    loc.SetValue(receiver, location);
                }
                else { 
                    // if the component didn't have a Location property, then the caller
                    // really wanted the tray location. 
                    SetTrayLocation(receiver, location); 
                }
            } 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.SetLocation"]/*' />
        /// <devdoc> 
        ///     Accessor method for the location extender property.  We offer this extender
        ///     to all non-visual components. 
        /// </devdoc> 
        public void SetTrayLocation(IComponent receiver, Point location) {
            TrayControl c = TrayControl.FromComponent(receiver); 

            if (c == null) {
                Debug.Fail("Anything we're extending should have a component view.");
                return; 
            }
 
            if (c.Parent == this) { 
                Point autoScrollLoc = this.AutoScrollPosition;
                location = new Point(location.X + autoScrollLoc.X, location.Y + autoScrollLoc.Y); 

                if (c.Visible) {
                    RearrangeInAutoSlots(c, location);
                } 
            }
            else if (!c.Location.Equals(location)) { 
                c.Location = location; 
                c.Positioned = true;
            } 
        }


        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.WndProc"]/*' /> 
        /// <devdoc>
        ///     We override our base class's WndProc to monitor certain messages. 
        /// </devdoc> 
        protected override void WndProc(ref Message m) {
            switch (m.Msg) { 
                case NativeMethods.WM_CANCELMODE:

                    // When we get cancelmode (i.e. you tabbed away to another window)
                    // then we want to cancel any pending drag operation! 
                    //
                    OnLostCapture(); 
                    break; 

                case NativeMethods.WM_SETCURSOR: 
                    OnSetCursor();
                    return;

                case NativeMethods.WM_HSCROLL: 
                case NativeMethods.WM_VSCROLL:
 
                    // When we scroll, we reposition a control without causing a 
                    // property change event.  Therefore, we must tell the
                    // selection UI service to sync itself. 
                    //
                    base.WndProc(ref m);
                    if (selectionUISvc != null) {
                        selectionUISvc.SyncSelection(); 
                    }
                    return; 
 
                case NativeMethods.WM_STYLECHANGED:
 
                    // When the scroll bars first appear, we need to
                    // invalidate so we properly paint our grid.
                    //
                    Invalidate(); 
                    break;
 
                case NativeMethods.WM_CONTEXTMENU: 

                    // Pop a context menu for the composition designer. 
                    //
                    int x = NativeMethods.Util.SignedLOWORD((int)m.LParam);
                    int y = NativeMethods.Util.SignedHIWORD((int)m.LParam);
                    if (x == -1 && y == -1) { 
                        // for shift-F10
                        Point mouse = Control.MousePosition; 
                        x = mouse.X; 
                        y = mouse.Y;
                    } 
                    OnContextMenu(x, y, true);
                    break;

                case NativeMethods.WM_NCHITTEST: 
                    if(glyphManager != null) {
                        // Get a hit test on any glyhs that we are managing 
                        // this way - we know where to route appropriate 
                        // messages
                        Point pt = new Point((short)NativeMethods.Util.LOWORD((int)m.LParam), 
                                             (short)NativeMethods.Util.HIWORD((int)m.LParam));
                        NativeMethods.POINT pt1 = new NativeMethods.POINT();
                        pt1.x = 0;
                        pt1.y = 0; 
                        NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, pt1, 1);
                        pt.Offset(pt1.x, pt1.y); 
                        glyphManager.GetHitTest(pt); 
                    }
 
                    base.WndProc(ref m);
                    break;

                default: 
                    base.WndProc(ref m);
                    break; 
            } 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.CanModifyComponents"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// Checks if the client is read only.  That is, if components can 
        /// be added or removed from the designer.
        /// </devdoc> 
        bool IOleDragClient.CanModifyComponents { 
            get {
                return true; 
            }
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.Component"]/*' /> 
        /// <internalonly/>
        IComponent IOleDragClient.Component { 
            get{ 
                return mainDesigner.Component;
            } 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.AddComponent"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// Adds a component to the tray. 
        /// </devdoc> 
        bool IOleDragClient.AddComponent(IComponent component, string name, bool firstAdd) {
 
            IOleDragClient oleDragClient = mainDesigner as IOleDragClient;
            // the designer for controls decides what to do here
            if (oleDragClient != null) {
 
                try {
                    oleDragClient.AddComponent(component, name, firstAdd); 
 
                    PositionControl(TrayControl.FromComponent(component));
                    mouseDropLocation = InvalidPoint; 

                    return true;
                }
                catch { 
                }
            } 
            else { 
                // for webforms (98109) just add the component directly to the host
                // 
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));

                try {
                    if (host != null && host.Container != null) { 
                        if (host.Container.Components[name] != null) {
                            name = null; 
                        } 
                        host.Container.Add(component, name);
                        return true; 
                    }
                }
                catch {
                } 

            } 
            Debug.Fail("Don't know how to add component!"); 
            return false;
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.GetControlForComponent"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// <para>
        /// Gets the control view instance for the given component. 
        /// For Win32 designer, this will often be the component itself. 
        /// </para>
        /// </devdoc> 
        Control IOleDragClient.GetControlForComponent(object component) {
            IComponent comp = component as IComponent;
            if (comp != null) {
                return TrayControl.FromComponent(comp); 
            }
            Debug.Fail("component is not IComponent"); 
            return null; 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.GetDesignerControl"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// <para> 
        /// Gets the control view instance for the designer that
        /// is hosting the drag. 
        /// </para> 
        /// </devdoc>
        Control IOleDragClient.GetDesignerControl() { 
            return this;
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.IsDropOk"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// Checks if it is valid to drop this type of a component on this client. 
        /// </devdoc>
        bool IOleDragClient.IsDropOk(IComponent component) { 
            return true;
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.BeginDrag"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// Begins a drag operation.  A designer should examine the list of components 
        /// to see if it wants to support the drag.  If it does, it should return
        /// true.  If it returns true, the designer should provide 
        /// UI feedback about the drag at this time.  Typically, this feedback consists
        /// of an inverted rectangle for each component, or a caret if the component
        /// is text.
        /// </devdoc> 
        bool ISelectionUIHandler.BeginDrag(object[] components, SelectionRules rules, int initialX, int initialY) {
            if (TabOrderActive) { 
                return false; 
            }
 
            bool result = DragHandler.BeginDrag(components, rules, initialX, initialY);
            if (result) {
                if (!GetOleDragHandler().DoBeginDrag(components, rules, initialX, initialY)) {
                    return false; 
                }
            } 
            return result; 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.DragMoved"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// Called when the user has moved the mouse.  This will only be called on 
        /// the designer that returned true from beginDrag.  The designer
        /// should update its UI feedback here. 
        /// </devdoc> 
        void ISelectionUIHandler.DragMoved(object[] components, Rectangle offset) {
            DragHandler.DragMoved(components, offset); 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.EndDrag"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// Called when the user has completed the drag.  The designer should 
        /// remove any UI feedback it may be providing. 
        /// </devdoc>
        void ISelectionUIHandler.EndDrag(object[] components, bool cancel) { 
            DragHandler.EndDrag(components, cancel);

            GetOleDragHandler().DoEndDrag(components, cancel);
 
            //Here, after the drag is finished and after we have resumed layout,
            //adjust the location of the components we dragged by the scroll offset 
            // 
            if (!this.autoScrollPosBeforeDragging.IsEmpty) {
                foreach (IComponent comp in components) { 
                    TrayControl tc = TrayControl.FromComponent(comp);
                    if (tc != null) {
                        this.SetTrayLocation(comp, new Point(tc.Location.X - this.autoScrollPosBeforeDragging.X, tc.Location.Y - this.autoScrollPosBeforeDragging.Y));
                    } 
                }
                this.AutoScrollPosition = new Point(-this.autoScrollPosBeforeDragging.X, -this.autoScrollPosBeforeDragging.Y); 
            } 

        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.GetComponentBounds"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// <para>
        /// Gets the shape of the component. The component's shape should be in 
        /// absolute coordinates and in pixels, where 0,0 is the upper left corner of 
        /// the screen.
        /// </para> 
        /// </devdoc>
        Rectangle ISelectionUIHandler.GetComponentBounds(object component) {
            // We render the selection UI glyph ourselves.
            return Rectangle.Empty; 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.GetComponentRules"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// <para>
        /// Gets a set of rules concerning the movement capabilities of a component.
        /// This should be one or more flags from the SelectionRules class. If no designer
        /// provides rules for a component, the component will not get any UI services. 
        /// </para>
        /// </devdoc> 
        SelectionRules ISelectionUIHandler.GetComponentRules(object component) { 
            return SelectionRules.Visible | SelectionRules.Moveable;
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.GetSelectionClipRect"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// <para>
        /// Gets the rectangle that any selection adornments should be clipped 
        /// to. This is normally the client area (in screen coordinates) of the 
        /// container.
        /// </para> 
        /// </devdoc>
        Rectangle ISelectionUIHandler.GetSelectionClipRect(object component) {
            if (IsHandleCreated) {
                return RectangleToScreen(ClientRectangle); 
            }
            return Rectangle.Empty; 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.OleDragEnter"]/*' /> 
        /// <internalonly/>
        void ISelectionUIHandler.OleDragEnter(DragEventArgs de) {
            GetOleDragHandler().DoOleDragEnter(de);
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.OleDragDrop"]/*' /> 
        /// <internalonly/> 
        void ISelectionUIHandler.OleDragDrop(DragEventArgs de) {
            GetOleDragHandler().DoOleDragDrop(de); 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.OleDragOver"]/*' />
        /// <internalonly/> 
        void ISelectionUIHandler.OleDragOver(DragEventArgs de) {
            GetOleDragHandler().DoOleDragOver(de); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.OleDragLeave"]/*' /> 
        /// <internalonly/>
        void ISelectionUIHandler.OleDragLeave() {
            GetOleDragHandler().DoOleDragLeave();
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.OnSelectionDoubleClick"]/*' /> 
        /// <internalonly/> 
        /// <devdoc>
        /// Handle a double-click on the selection rectangle 
        /// of the given component.
        /// </devdoc>
        void ISelectionUIHandler.OnSelectionDoubleClick(IComponent component) {
            if (!TabOrderActive) { 
                TrayControl tc = ((IOleDragClient)this).GetControlForComponent(component) as TrayControl;
                if (tc != null) { 
                    tc.ViewDefaultEvent(component); 
                }
            } 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.QueryBeginDrag"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// Queries to see if a drag operation 
        /// is valid on this handler for the given set of components. 
        /// If it returns true, BeginDrag will be called immediately after.
        /// </devdoc> 
        bool ISelectionUIHandler.QueryBeginDrag(object[] components, SelectionRules rules, int initialX, int initialY) {
            return DragHandler.QueryBeginDrag(components, rules, initialX, initialY);
        }
 
        internal void RearrangeInAutoSlots(Control c, Point pos) {
#if DEBUG 
            int index = controls.IndexOf(c); 
            Debug.Assert(index != -1, "Add control to the list of controls before autoarranging.!!!");
            Debug.Assert(this.Visible == c.Visible, "TrayControl for " + ((TrayControl)c).Component + " should not be positioned"); 
#endif // DEBUG

            TrayControl tc = (TrayControl)c;
            tc.Positioned = true; 
            tc.Location = pos;
        } 
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.ShowContextMenu"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// Shows the context menu for the given component.
        /// </devdoc>
        void ISelectionUIHandler.ShowContextMenu(IComponent component) { 
            Point cur = Control.MousePosition;
            OnContextMenu(cur.X, cur.Y, true); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ComponentTrayGlyphManager"]/*' /> 
        /// <devdoc>
        ///     This class privately manages all componenttray-related
        ///     glyphs in a simlar fashion to the BehaviorService.
        /// </devdoc> 
        private class ComponentTrayGlyphManager {
 
            private Adorner traySelectionAdorner;//we'll use a single adorner to manage the glyphs 
            private Glyph hitTestedGlyph;//the last glyph we hit tested (can be null)
 
            private ISelectionService       selSvc;//we need the selection service fo r the hover behavior
            private BehaviorService         behaviorSvc;

            /// <devdoc> 
            ///     Constructor that simply creates an empty adorner.
            /// </devdoc> 
            public ComponentTrayGlyphManager(ISelectionService selSvc, BehaviorService behaviorSvc) { 

                this.selSvc = selSvc; 
                this.behaviorSvc = behaviorSvc;

                traySelectionAdorner = new Adorner();
            } 

            /// <devdoc> 
            ///    This is how we publically expose our glyph collection 
            ///    so that other designer services can 'add value'.
            /// </devdoc> 
            public GlyphCollection SelectionGlyphs {
                get {
                    return traySelectionAdorner.Glyphs;
                } 
            }
 
            /// <devdoc> 
            ///    Clears teh adorner of glyphs.
            /// </devdoc> 
            public void Dispose() {
                if (traySelectionAdorner != null) {
                    traySelectionAdorner.Glyphs.Clear();
                    traySelectionAdorner = null; 
                }
            } 
 
            /// <devdoc>
            ///    Retrieves a list of glyphs associated with the component. 
            /// </devdoc>
            public GlyphCollection GetGlyphsForComponent(IComponent comp) {
                GlyphCollection glyphs = new GlyphCollection();
                if(behaviorSvc != null && comp != null) { 
                    if(behaviorSvc.DesignerActionUI != null) {
                        Glyph g = behaviorSvc.DesignerActionUI.GetDesignerActionGlyph(comp); 
                        if(g!=null) { 
                            glyphs.Add(g);
                        } 
                    }
                }
                return glyphs;
            } 

            /// <devdoc> 
            ///    Called from the tray's NCHITTEST message in the WndProc. 
            ///    We use this to loop through our glyphs and identify which
            ///    one is successfully hit tested.  From here, we know where 
            ///    to send our messages.
            /// </devdoc>
            public Cursor GetHitTest(Point p) {
                for (int i = 0; i < traySelectionAdorner.Glyphs.Count; i++) { 
                    Cursor hitTestCursor = traySelectionAdorner.Glyphs[i].GetHitTest(p);
                    if (hitTestCursor != null) { 
                        hitTestedGlyph = traySelectionAdorner.Glyphs[i]; 
                        return hitTestCursor;
                    } 
                }

                hitTestedGlyph = null;
                return null; 
            }
 
 
            /// <devdoc>
            ///    Called when the tray receives this mouse message.  Here, 
            ///    we'll give our glyphs the first chance to repsond to the message
            //     before the tray even sees it.
            /// </devdoc>
            public bool OnMouseDoubleClick(MouseEventArgs e) { 
                if (hitTestedGlyph != null && hitTestedGlyph.Behavior != null) {
                    return hitTestedGlyph.Behavior.OnMouseDoubleClick(hitTestedGlyph, e.Button, new Point(e.X, e.Y)); 
                } 
                return false;
            } 

            /// <devdoc>
            ///    Called when the tray receives this mouse message.  Here,
            ///    we'll give our glyphs the first chance to repsond to the message 
            //     before the tray even sees it.
            /// </devdoc> 
            public bool OnMouseDown(MouseEventArgs e) { 
                if (hitTestedGlyph != null && hitTestedGlyph.Behavior != null) {
                    return hitTestedGlyph.Behavior.OnMouseDown(hitTestedGlyph, e.Button, new Point(e.X, e.Y)); 
                }
                return false;
            }
 

            /// <devdoc> 
            ///    Called when the tray receives this mouse message.  Here, 
            ///    we'll give our glyphs the first chance to repsond to the message
            //     before the tray even sees it. 
            /// </devdoc>
            public bool OnMouseMove(MouseEventArgs e) {
                if (hitTestedGlyph != null && hitTestedGlyph.Behavior != null) {
                    return hitTestedGlyph.Behavior.OnMouseMove(hitTestedGlyph, e.Button, new Point(e.X, e.Y)); 
                }
                return false; 
            } 

            /// <devdoc> 
            ///    Called when the tray receives this mouse message.  Here,
            ///    we'll give our glyphs the first chance to repsond to the message
            //     before the tray even sees it.
            /// </devdoc> 
            public bool OnMouseUp(MouseEventArgs e) {
                if (hitTestedGlyph != null && hitTestedGlyph.Behavior != null) { 
                    return hitTestedGlyph.Behavior.OnMouseUp(hitTestedGlyph, e.Button); 
                }
                return false; 
            }

            /// <devdoc>
            ///    Called when the comp tray or any tray control paints. 
            ///    This will simply enumerate through the glyphs in our
            ///    Adorner and ask them to paint 
            /// </devdoc> 
            public void OnPaintGlyphs(PaintEventArgs pe) {
                //Paint any glyphs our tray adorner has 
                foreach (Glyph g in traySelectionAdorner.Glyphs) {
                    g.Paint(pe);
                }
            } 

            /// <devdoc> 
            ///    Called when a tray control's location has changed. 
            ///    We'll loop through our glyphs and invalidate any
            ///    that are associated with the component. 
            /// </devdoc>
            public void UpdateLocation(TrayControl trayControl) {

                foreach (Glyph g in traySelectionAdorner.Glyphs) { 
                    //only look at glyphs that derive from designerglyph base (actions)
                    DesignerActionGlyph desGlyph = g as DesignerActionGlyph; 
                    if (desGlyph != null && ((DesignerActionBehavior)(desGlyph.Behavior)).RelatedComponent.Equals(trayControl.Component)) { 
                        desGlyph.UpdateAlternativeBounds(trayControl.Bounds);
                    } 
                }
            }
        }
 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayOleDragDropHandler"]/*' /> 
        /// <internalonly/> 
        /// <devdoc>
        ///    TrayOleDragDropHandler provides the Ole Drag-drop handler for the 
        ///    component tray.
        /// </devdoc>
        private class TrayOleDragDropHandler : OleDragDropHandler {
 
            public TrayOleDragDropHandler(SelectionUIHandler selectionHandler,  IServiceProvider  serviceProvider, IOleDragClient client) :
            base(selectionHandler, serviceProvider, client) { 
            } 

            protected override bool CanDropDataObject(IDataObject dataObj) { 
                ICollection comps = null;
                if (dataObj != null) {
                    ComponentDataObjectWrapper cdow = dataObj as ComponentDataObjectWrapper;
                    if (cdow != null) { 
                        ComponentDataObject cdo = (ComponentDataObject) cdow.InnerData;
                        comps = cdo.Components; 
                    } 
                    else {
                        try { 
                            object serializationData = dataObj.GetData(OleDragDropHandler.DataFormat, true);

                            if (serializationData == null) {
                                return false; 
                            }
 
                            IDesignerSerializationService ds = (IDesignerSerializationService)GetService(typeof(IDesignerSerializationService)); 
                            if (ds == null) {
                                return false; 
                            }
                            comps = ds.Deserialize(serializationData);
                        }
                        catch (Exception e) { 
                            if (ClientUtils.IsCriticalException(e)) {
                                throw; 
                            } 
                            // we return false on any exception
                        } 

                        catch {
                        }
                    } 
                }
 
                if (comps != null && comps.Count > 0) { 
                    foreach(object comp in comps) {
                        if (comp is Point) { 
                            continue;
                        }
                        if (comp is Control || !(comp is IComponent)) {
                            return false; 
                        }
                    } 
                    return true; 
                }
 
                return false;
            }
        }
 
        internal class AutoArrangeComparer : IComparer {
            int IComparer.Compare(object o1, object o2) { 
                Debug.Assert(o1 != null && o2 != null, "Null objects sent for comparison!!!"); 

                Point tcLoc1 = ((Control)o1).Location; 
                Point tcLoc2 = ((Control)o2).Location;
                int width = ((Control)o1).Width / 2;
                int height = ((Control)o1).Height / 2;
 
                // If they are at the same location, they are equal.
                if (tcLoc1.X == tcLoc2.X && tcLoc1.Y == tcLoc2.Y) { 
                    return 0; 
                }
 
                // Is the first control lower than the 2nd...
                if (tcLoc1.Y + height <= tcLoc2.Y)
                    return -1;
 
                // Is the 2nd control lower than the first...
                if (tcLoc2.Y + height <= tcLoc1.Y) 
                    return 1; 

                // Which control is left of the other... 
                return((tcLoc1.X <= tcLoc2.X) ? -1 : 1);
            }
        }
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        ///    The tray control is the UI we show for each component in the tray. 
        /// </devdoc>
        internal class TrayControl : Control { 

            // Values that define this tray control
            //
            private IComponent  component;       // the component this control is representing 
            private Image       toolboxBitmap;   // the bitmap used to represent the component
            private int         cxIcon;          // the dimensions of the bitmap 
            private int         cyIcon;          // the dimensions of the bitmap 

            private InheritanceAttribute inheritanceAttribute; 

            // Services that we use often enough to cache.
            //
            private ComponentTray        tray; 

            // transient values that are used during mouse drags 
            // 
            private Point mouseDragLast = InvalidPoint;  // the last position of the mouse during a drag.
            private bool  mouseDragMoved;       // has the mouse been moved during this drag? 
            private bool  ctrlSelect = false;   // was the ctrl key down on the mouse down?
            private bool  positioned = false;   // Have we given this control an explicit location yet?

            private const int whiteSpace  = 5; 
            private int borderWidth;
 
            internal bool fRecompute = false; // This flag tells the TrayControl that it needs to retrieve 
                                              // the font and the background color before painting.
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.TrayControl"]/*' />
            /// <devdoc>
            ///      Creates a new TrayControl based on the component.
            /// </devdoc> 
            public TrayControl(ComponentTray tray, IComponent component) {
                this.tray = tray; 
                this.component = component; 

                SetStyle(ControlStyles.OptimizedDoubleBuffer, true); 
                SetStyle(ControlStyles.Selectable, false);
                borderWidth = SystemInformation.BorderSize.Width;

                UpdateIconInfo(); 

                IComponentChangeService cs = (IComponentChangeService)tray.GetService(typeof(IComponentChangeService)); 
                if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(cs != null, "IComponentChangeService not found"); 
                if (cs != null) {
                    cs.ComponentRename += new ComponentRenameEventHandler(this.OnComponentRename); 
                }

                ISite site = component.Site;
                string name = null; 

                if (site != null) { 
                    name = site.Name; 

                    IDictionaryService ds = (IDictionaryService)site.GetService(typeof(IDictionaryService)); 
                    Debug.Assert(ds != null, "ComponentTray relies on IDictionaryService, which is not available.");
                    if (ds != null) {
                        ds.SetValue(GetType(), this);
                    } 
                }
 
                if (name == null) { 
                    // We always want name to have something in it, so we default to
                    // the class name.  This way the design instance contains something 
                    // semi-intuitive if we don't have a site.
                    //
                    name = component.GetType().Name;
                } 

                Text = name; 
                inheritanceAttribute = (InheritanceAttribute)TypeDescriptor.GetAttributes(component)[typeof(InheritanceAttribute)]; 
                TabStop = false;
 

            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.Component"]/*' /> 
            /// <devdoc>
            ///      Retrieves the compnent this control is representing. 
            /// </devdoc> 
            public IComponent Component {
                get { 
                    return component;
                }
            }
 
            public override Font Font {
                get { 
                    /* 
                    IDesignerHost host = (IDesignerHost)tray.GetService(typeof(IDesignerHost));
                    if (host != null && host.GetRootComponent() is Control) { 
                        Control c = (Control)host.GetRootComponent();
                        return c.Font;
                    }
                    */ 
                    return tray.Font;
                } 
            } 

            public InheritanceAttribute InheritanceAttribute { 
                get {
                    return inheritanceAttribute;
                }
            } 

            public bool Positioned { 
                get { 
                    return positioned;
                } 
                set {
                    positioned = value;
                }
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.AdjustSize"]/*' /> 
            /// <devdoc> 
            ///     Adjusts the size of the control based on the contents.
            /// </devdoc> 
            //


 
            private void AdjustSize(bool autoArrange) {
                // 
                Graphics gr = CreateGraphics(); 

                try { 
                    Size sz = Size.Ceiling(gr.MeasureString(Text, Font));

                    Rectangle rc = Bounds;
 
                    if (tray.ShowLargeIcons) {
                        rc.Width = Math.Max(cxIcon, sz.Width) + 4 * borderWidth + 2 * whiteSpace; 
                        rc.Height = cyIcon + 2 * whiteSpace + sz.Height + 4 * borderWidth; 
                    }
                    else { 
                        rc.Width = cxIcon + sz.Width + 4 * borderWidth + 2 * whiteSpace;
                        rc.Height = Math.Max(cyIcon, sz.Height) + 4 * borderWidth;
                    }
 
                    Bounds = rc;
                    Invalidate(); 
                } 

                finally { 
                    if (gr != null) {
                        gr.Dispose();
                    }
                } 

                if(tray.glyphManager != null) { 
                    tray.glyphManager.UpdateLocation(this); 
                }
            } 

            protected override AccessibleObject CreateAccessibilityInstance() {
                return new TrayControlAccessibleObject(this, tray);
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.Dispose"]/*' /> 
            /// <devdoc> 
            ///     Destroys this control.  Views automatically destroy themselves when they
            ///     are removed from the design container. 
            /// </devdoc>
            protected override void Dispose(bool disposing) {
                if (disposing) {
                    ISite site = component.Site; 
                    if (site != null) {
                        IComponentChangeService cs = (IComponentChangeService)site.GetService(typeof(IComponentChangeService)); 
                        if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(cs != null, "IComponentChangeService not found"); 
                        if (cs != null) {
                            cs.ComponentRename -= new ComponentRenameEventHandler(this.OnComponentRename); 
                        }

                        IDictionaryService ds = (IDictionaryService)site.GetService(typeof(IDictionaryService));
                        if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(ds != null, "IDictionaryService not found"); 
                        if (ds != null) {
                            ds.SetValue(typeof(TrayControl), null); 
                        } 
                    }
                } 

                base.Dispose(disposing);
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.FromComponent"]/*' />
            /// <devdoc> 
            ///      Retrieves the tray control object for the given component. 
            /// </devdoc>
            public static TrayControl FromComponent(IComponent component) { 
                TrayControl c = null;

                if (component == null) {
                    return null; 
                }
 
                ISite site = component.Site; 
                if (site != null) {
                    IDictionaryService ds = (IDictionaryService)site.GetService(typeof(IDictionaryService)); 
                    if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(ds != null, "IDictionaryService not found");
                    if (ds != null) {
                        c = (TrayControl)ds.GetValue(typeof(TrayControl));
                    } 
                }
 
                return c; 
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnComponentRename"]/*' />
            /// <devdoc>
            ///     Delegate that is called in response to a name change.  Here we update our own
            ///     stashed version of the name, recalcuate our size and repaint. 
            /// </devdoc>
            private void OnComponentRename(object sender, ComponentRenameEventArgs e) { 
                if (e.Component == this.component) { 
                    Text = e.NewName;
                    AdjustSize(true); 
                }
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnHandleCreated"]/*' /> 
            /// <devdoc>
            ///     Overrides handle creation notification for a control.  Here we just ensure 
            ///     that we're the proper size. 
            /// </devdoc>
            protected override void OnHandleCreated(EventArgs e) { 
                base.OnHandleCreated(e);
                AdjustSize(false);
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnDoubleClick"]/*' />
            /// <devdoc> 
            ///     Called in response to a double-click of the left mouse button.  The 
            ///     default behavior here calls onDoubleClick on IMouseHandler
            /// </devdoc> 
            protected override void OnDoubleClick(EventArgs e) {
                base.OnDoubleClick(e);

                if (!tray.TabOrderActive) { 
                    IDesignerHost host = (IDesignerHost)tray.GetService(typeof(IDesignerHost));
                    Debug.Assert(host != null, "Component tray does not have access to designer host."); 
                    if (host != null) { 
                        mouseDragLast = InvalidPoint;
 
                        Capture = false;

                        // We try to get a designer for the component and let it view the
                        // event.  If this fails, then we'll try to do it ourselves. 
                        //
                        IDesigner designer = host.GetDesigner(component); 
 
                        if (designer == null) {
                            ViewDefaultEvent(component); 
                        }
                        else {
                            designer.DoDefaultAction();
                        } 
                    }
                } 
            } 

            /// <devdoc> 
            ///     Terminates our drag operation.
            /// </devdoc>
            private void OnEndDrag(bool cancel) {
                mouseDragLast = InvalidPoint; 

                if (!mouseDragMoved) { 
                    if (ctrlSelect) { 
                        ISelectionService sel = (ISelectionService)tray.GetService(typeof(ISelectionService));
                        if (sel != null) { 
                            sel.SetSelectedComponents(new object[] {this.Component}, SelectionTypes.Primary);
                        }
                        ctrlSelect = false;
                    } 
                    return;
                } 
                mouseDragMoved = false; 
                ctrlSelect = false;
 
                Capture = false;
                OnSetCursor();

                // And now finish the drag. 
                //
                Debug.Assert(tray.selectionUISvc != null, "We shouldn't be able to begin a drag without this"); 
                if (tray.selectionUISvc != null && tray.selectionUISvc.Dragging) { 
                    tray.selectionUISvc.EndDrag(cancel);
                } 
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnMouseDown"]/*' />
            /// <devdoc> 
            ///     Called when the mouse button is pressed down.  Here, we provide drag
            ///     support for the component. 
            /// </devdoc> 
            protected override void OnMouseDown(MouseEventArgs me) {
                base.OnMouseDown(me); 

                if (!tray.TabOrderActive) {

                    tray.FocusDesigner(); 

                    // If this is the left mouse button, then begin a drag. 
                    // 
                    if (me.Button == MouseButtons.Left) {
                        Capture = true; 
                        mouseDragLast = PointToScreen(new Point(me.X, me.Y));

                        // If the CTRL key isn't down, select this component,
                        // otherwise, we wait until the mouse up 
                        //
                        // Make sure the component is selected 
                        // 

                        ctrlSelect = NativeMethods.GetKeyState((int)Keys.ControlKey) != 0; 

                        if (!ctrlSelect) {
                            ISelectionService sel = (ISelectionService)tray.GetService(typeof(ISelectionService));
 
                            // Make sure the component is selected
                            // 
                            if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(sel != null, "ISelectionService not found"); 

                            if (sel != null) { 
                                sel.SetSelectedComponents(new object[] {this.Component}, SelectionTypes.Primary);
                            }
                        }
                    } 
                }
            } 
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnMouseMove"]/*' />
            /// <devdoc> 
            ///     Called when the mouse is moved over the component.  We update our drag
            ///     information here if we're dragging the component around.
            /// </devdoc>
            protected override void OnMouseMove(MouseEventArgs me) { 
                base.OnMouseMove(me);
 
                if (mouseDragLast == InvalidPoint) { 
                    return;
                } 

                if (!mouseDragMoved) {

                    Size minDrag = SystemInformation.DragSize; 
                    Size minDblClick = SystemInformation.DoubleClickSize;
 
                    minDrag.Width = Math.Max(minDrag.Width, minDblClick.Width); 
                    minDrag.Height = Math.Max(minDrag.Height, minDblClick.Height);
 
                    // we have to make sure the mouse moved farther than
                    // the minimum drag distance before we actually start
                    // the drag
                    // 
                    Point newPt = PointToScreen(new Point(me.X, me.Y));
                    if (mouseDragLast == InvalidPoint || 
                        (Math.Abs(mouseDragLast.X - newPt.X) < minDrag.Width && 
                         Math.Abs(mouseDragLast.Y - newPt.Y) < minDrag.Height)) {
                        return; 
                    }
                    else {
                        mouseDragMoved = true;
 
                        // we're on the move, so we're not in a ctrlSelect
                        // 
                        ctrlSelect = false; 
                    }
                } 

                try {
                    // Make sure the component is selected
                    // 
                    ISelectionService sel = (ISelectionService)tray.GetService(typeof(ISelectionService));
                    if (sel != null) { 
                        sel.SetSelectedComponents(new object[] {this.Component}, SelectionTypes.Primary); 
                    }
 
                    // Notify the selection service that all the components are in the "mouse down" mode.
                    //
                    if (tray.selectionUISvc != null && tray.selectionUISvc.BeginDrag(SelectionRules.Visible | SelectionRules.Moveable, mouseDragLast.X, mouseDragLast.Y)) {
                        OnSetCursor(); 
                    }
                } 
                finally { 
                    mouseDragMoved = false;
                    mouseDragLast = InvalidPoint; 
                }
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnMouseUp"]/*' /> 
            /// <devdoc>
            ///     Called when the mouse button is released.  Here, we finish our drag 
            ///     if one was started. 
            /// </devdoc>
            protected override void OnMouseUp(MouseEventArgs me) { 
                base.OnMouseUp(me);
                OnEndDrag(false);
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnContextMenu"]/*' />
            /// <devdoc> 
            ///     Called when we are to display our context menu for this component. 
            /// </devdoc>
            private void OnContextMenu(int x, int y) { 

                if (!tray.TabOrderActive) {
                    Capture = false;
 
                    // Ensure that this component is selected.
                    // 
                    ISelectionService s = (ISelectionService)tray.GetService(typeof(ISelectionService)); 
                    if (s != null && !s.GetComponentSelected(component)) {
                        s.SetSelectedComponents(new object[] {component}, SelectionTypes.Replace); 
                    }

                    IMenuCommandService mcs = tray.MenuService;
                    if (mcs != null) { 
                        Capture = false;
                        Cursor.Clip = Rectangle.Empty; 
                        mcs.ShowContextMenu(MenuCommands.TraySelectionMenu, x, y); 
                    }
                } 
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnPaint"]/*' />
            /// <devdoc> 
            ///     Painting for our control.
            /// </devdoc> 
            protected override void OnPaint(PaintEventArgs e) { 
                if (fRecompute) {
                    fRecompute = false; 
                    UpdateIconInfo();
                }

                base.OnPaint(e); 
                Rectangle rc = ClientRectangle;
 
                rc.X += whiteSpace + borderWidth; 
                rc.Y += borderWidth;
                rc.Width -= (2 * borderWidth + whiteSpace); 
                rc.Height -= 2 * borderWidth;

                StringFormat format = new StringFormat();
                Brush foreBrush = new SolidBrush(ForeColor); 

                try { 
                    format.Alignment = StringAlignment.Center; 
                    if (tray.ShowLargeIcons) {
                        if (null != toolboxBitmap) { 
                            int x = rc.X + (rc.Width - cxIcon)/2;
                            int y = rc.Y + whiteSpace;
                            e.Graphics.DrawImage(toolboxBitmap, new Rectangle(x, y, cxIcon, cyIcon));
                        } 

                        rc.Y += (cyIcon + whiteSpace); 
                        rc.Height -= cyIcon; 
                        e.Graphics.DrawString(Text, Font, foreBrush, rc, format);
                    } 
                    else {
                        if (null != toolboxBitmap) {
                            int y = rc.Y + (rc.Height - cyIcon)/2;
                            e.Graphics.DrawImage(toolboxBitmap, new Rectangle(rc.X, y, cxIcon, cyIcon)); 
                        }
 
                        rc.X += (cxIcon + borderWidth); 
                        rc.Width -= cxIcon;
                        rc.Y += 3; 
                        e.Graphics.DrawString(Text, Font, foreBrush, rc);
                    }
                }
 
                finally {
                    if (format != null) { 
                        format.Dispose(); 
                    }
                    if (foreBrush != null) { 
                        foreBrush.Dispose();
                    }
                }
 
                // If this component is being inherited, paint it as such
                // 
                if (!InheritanceAttribute.NotInherited.Equals(inheritanceAttribute)) { 
                    InheritanceUI iui = tray.InheritanceUI;
                    if (iui != null) { 
                        e.Graphics.DrawImage(iui.InheritanceGlyph, 0, 0);
                    }
                }
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnFontChanged"]/*' /> 
            /// <devdoc> 
            ///     Overrides control's FontChanged.  Here we re-adjust our size if the font changes.
            /// </devdoc> 
            protected override void OnFontChanged(EventArgs e) {
                AdjustSize(true);
                base.OnFontChanged(e);
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnLocationChanged"]/*' /> 
            /// <devdoc> 
            ///     Overrides control's LocationChanged.  Here, we make sure that any glyphs associated
            ///     with us are also relocated. 
            /// </devdoc>
            protected override void OnLocationChanged(EventArgs e) {
                if(tray.glyphManager != null) {
                    tray.glyphManager.UpdateLocation(this); 
                }
            } 
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnTextChanged"]/*' />
            /// <devdoc> 
            ///     Overrides control's TextChanged.  Here we re-adjust our size if the font changes.
            /// </devdoc>
            protected override void OnTextChanged(EventArgs e) {
                AdjustSize(true); 
                base.OnTextChanged(e);
            } 
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnSetCursor"]/*' />
            /// <devdoc> 
            ///     Called each time the cursor needs to be set.  The ControlDesigner behavior here
            ///     will set the cursor to one of three things:
            ///     1.  If the selection UI service shows a locked selection, or if there is no location
            ///     property on the control, then the default arrow will be set. 
            ///     2.  Otherwise, the four headed arrow will be set to indicate that the component can
            ///     be clicked and moved. 
            ///     3.  If the user is currently dragging a component, the crosshair cursor will be used 
            ///     instead of the four headed arrow.
            /// </devdoc> 
            private void OnSetCursor() {

                // Check that the component is not locked.
                // 
                PropertyDescriptor prop = TypeDescriptor.GetProperties(component)["Locked"];
                if (prop != null  && ((bool)prop.GetValue(component)) == true) { 
                    Cursor.Current = Cursors.Default; 
                    return;
                } 

                // Ask the tray to see if the tab order UI is not running.
                //
                if (tray.TabOrderActive) { 
                    Cursor.Current = Cursors.Default;
                    return; 
                } 

                if (mouseDragMoved) { 
                    Cursor.Current = Cursors.Default;
                }
                else if (mouseDragLast != InvalidPoint) {
                    Cursor.Current = Cursors.Cross; 
                }
                else { 
                    Cursor.Current = Cursors.SizeAll; 
                }
            } 

            protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified) {
                if (!tray.AutoArrange ||
                    (specified & BoundsSpecified.Width) == BoundsSpecified.Width || 
                    (specified & BoundsSpecified.Height) == BoundsSpecified.Height) {
 
                    base.SetBoundsCore(x, y, width, height, specified); 
                }
 
                Rectangle bounds = Bounds;
                Size parentGridSize = tray.ParentGridSize;
                if (Math.Abs(bounds.X - x) > parentGridSize.Width || Math.Abs(bounds.Y - y) > parentGridSize.Height) {
                    base.SetBoundsCore(x, y, width, height, specified); 
                }
 
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.SetVisibleCore"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            protected override void SetVisibleCore(bool value) { 
                if (value && !tray.CanDisplayComponent(this.component))
                    return; 
 
                base.SetVisibleCore(value);
            } 

            public override string ToString() {
                return "ComponentTray: " + component.ToString();
            } 

            internal void UpdateIconInfo() { 
                ToolboxBitmapAttribute attr = (ToolboxBitmapAttribute)TypeDescriptor.GetAttributes(component)[typeof(ToolboxBitmapAttribute)]; 
                if (attr != null) {
                    toolboxBitmap = attr.GetImage(component, tray.ShowLargeIcons); 
                }

                // Get the size of the bitmap so we can size our
                // component correctly. 
                //
                if (null == toolboxBitmap) { 
                    cxIcon = 0; 
                    cyIcon = SystemInformation.IconSize.Height;
                } 
                else {
                    Size sz = toolboxBitmap.Size;
                    cxIcon = sz.Width;
                    cyIcon = sz.Height; 
                }
 
                AdjustSize(true); 
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.ViewDefaultEvent"]/*' />
            /// <devdoc>
            ///      This creates a method signature in the source code file for the
            ///      default event on the component and navigates the user's cursor 
            ///      to that location.
            /// </devdoc> 
            public virtual void ViewDefaultEvent(IComponent component) { 
                EventDescriptor defaultEvent = TypeDescriptor.GetDefaultEvent(component);
                PropertyDescriptor defaultPropEvent = null; 
                string handler = null;
                bool eventChanged = false;

                IEventBindingService eps = (IEventBindingService)GetService(typeof(IEventBindingService)); 
                if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(eps != null, "IEventBindingService not found");
                if (eps != null) { 
                    defaultPropEvent = eps.GetEventProperty(defaultEvent); 
                }
 
                // If we couldn't find a property for this event, or if the property is read only, then
                // abort and just show the code.
                //
                if (defaultPropEvent == null || defaultPropEvent.IsReadOnly) { 
                    if (eps != null) {
                        eps.ShowCode(); 
                    } 
                    return;
                } 

                handler = (string)defaultPropEvent.GetValue(component);

                // If there is no handler set, set one now. 
                //
                if (handler == null) { 
                    eventChanged = true; 
                    handler = eps.CreateUniqueMethodName(component, defaultEvent);
                } 

                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                DesignerTransaction trans = null;
 
                try {
                    if (host != null) { 
                        trans = host.CreateTransaction(SR.GetString(SR.WindowsFormsAddEvent, defaultEvent.Name)); 
                    }
 
                    // Save the new value... BEFORE navigating to it!
                    //
                    if (eventChanged && defaultPropEvent != null) {
 
                        defaultPropEvent.SetValue(component, handler);
 
                        // make sure set succeded (may fail if under SCC) 
                        // if (defaultPropEvent.GetValue(component) != handler) {
                        //     return; 
                        // }
                    }

                    eps.ShowCode(component, defaultEvent); 
                }
                finally { 
                    if (trans != null) { 
                        trans.Commit();
                    } 
                }
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.WndProc"]/*' /> 
            /// <devdoc>
            ///     This method should be called by the extending designer for each message 
            ///     the control would normally receive.  This allows the designer to pre-process 
            ///     messages before allowing them to be routed to the control.
            /// </devdoc> 
            protected override void WndProc(ref Message m) {
                switch (m.Msg) {
                    case NativeMethods.WM_SETCURSOR:
                        // We always handle setting the cursor ourselves. 
                        //
                        OnSetCursor(); 
                        break; 

                    case NativeMethods.WM_CONTEXTMENU: 
                        // We must handle this ourselves.  Control only allows
                        // regular Windows Forms context menus, which doesn't do us much
                        // good.  Also, control's button up processing calls DefwndProc
                        // first, which causes a right mouse up to be routed as a 
                        // WM_CONTEXTMENU.  If we don't respond to it here, this
                        // message will be bubbled up to our parent, which would 
                        // pop up a container context menu instead of our own. 
                        //
                        int x = NativeMethods.Util.SignedLOWORD((int)m.LParam); 
                        int y = NativeMethods.Util.SignedHIWORD((int)m.LParam);
                        if (x == -1 && y == -1) {
                            // for shift-F10
                            Point mouse = Control.MousePosition; 
                            x = mouse.X;
                            y = mouse.Y; 
                        } 
                        OnContextMenu(x, y);
                        break; 
                    case NativeMethods.WM_NCHITTEST:
                        if(tray.glyphManager != null) {
                            //Make sure tha we send our glyphs hit test messages
                            //over the TrayControls too 
                            Point pt = new Point((short)NativeMethods.Util.LOWORD((int)m.LParam),
                                                 (short)NativeMethods.Util.HIWORD((int)m.LParam)); 
                            NativeMethods.POINT pt1 = new NativeMethods.POINT(); 
                            pt1.x = 0;
                            pt1.y = 0; 
                            NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, pt1, 1);
                            pt.Offset(pt1.x, pt1.y);
                            pt.Offset(this.Location.X, this.Location.Y);//offset the loc of the traycontrol -so now we're in comptray coords
                            tray.glyphManager.GetHitTest(pt); 
                        }
 
                        base.WndProc(ref m); 
                        break;
 
                    default:
                        base.WndProc(ref m);
                        break;
                } 
            }
 
            private class TrayControlAccessibleObject : ControlAccessibleObject 
            {
                ComponentTray tray; 

                public TrayControlAccessibleObject(TrayControl owner, ComponentTray tray) : base(owner) {
                    this.tray = tray;
                } 

                private IComponent Component { 
                    get 
                    {
                        return ((TrayControl)Owner).Component; 
                    }
                }

                public override AccessibleStates State { 
                    get
                    { 
                        AccessibleStates state = base.State; 

                        ISelectionService s = (ISelectionService)tray.GetService(typeof(ISelectionService)); 
                        if (s != null) {
                            if (s.GetComponentSelected(Component)) {
                                state |= AccessibleStates.Selected;
                            } 
                            if (s.PrimarySelection == Component) {
                                state |= AccessibleStates.Focused; 
                            } 
                        }
 
                        return state;
                    }
                }
            } 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler"]/*' /> 
        /// <devdoc>
        ///      This class inherits from the abstract SelectionUIHandler 
        ///      class to provide a selection UI implementation for the
        ///      component tray.
        /// </devdoc>
        private class TraySelectionUIHandler : SelectionUIHandler { 

            private ComponentTray tray; 
            private Size snapSize = Size.Empty; 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.TraySelectionUIHandler"]/*' /> 
            /// <devdoc>
            ///      Creates a new selection UI handler for the given
            ///      component tray.
            /// </devdoc> 
            public TraySelectionUIHandler(ComponentTray tray) {
                this.tray = tray; 
                snapSize = new Size(); 
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.BeginDrag"]/*' />
            /// <devdoc>
            ///     Called when the user has started the drag.
            /// </devdoc> 
            public override bool BeginDrag(object[] components, SelectionRules rules, int initialX, int initialY) {
                bool value = base.BeginDrag(components, rules, initialX, initialY); 
                tray.SuspendLayout(); 
                return value;
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.EndDrag"]/*' />
            /// <devdoc>
            ///     Called when the user has completed the drag.  The designer should 
            ///     remove any UI feedback it may be providing.
            /// </devdoc> 
            public override void EndDrag(object[] components, bool cancel) { 
                base.EndDrag(components, cancel);
                tray.ResumeLayout(); 
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetComponent"]/*' />
            /// <devdoc> 
            ///      Retrieves the base component for the selection handler.
            /// </devdoc> 
            protected override IComponent GetComponent() { 
                return tray;
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetControl"]/*' />
            /// <devdoc>
            ///      Retrieves the base component's UI control for the selection handler. 
            /// </devdoc>
            protected override Control GetControl() { 
                return tray; 
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetControl1"]/*' />
            /// <devdoc>
            ///      Retrieves the UI control for the given component.
            /// </devdoc> 
            protected override Control GetControl(IComponent component) {
                return TrayControl.FromComponent(component); 
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetCurrentSnapSize"]/*' /> 
            /// <devdoc>
            ///      Retrieves the current grid snap size we should snap objects
            ///      to.
            /// </devdoc> 
            protected override Size GetCurrentSnapSize() {
                return snapSize; 
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetService"]/*' /> 
            /// <devdoc>
            ///      We use this to request often-used services.
            /// </devdoc>
            protected override object GetService(Type serviceType) { 
                return tray.GetService(serviceType);
            } 
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetShouldSnapToGrid"]/*' />
            /// <devdoc> 
            ///      Determines if the selection UI handler should attempt to snap
            ///      objects to a grid.
            /// </devdoc>
            protected override bool GetShouldSnapToGrid() { 
                return false;
            } 
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetUpdatedRect"]/*' />
            /// <devdoc> 
            ///      Given a rectangle, this updates the dimensions of it
            ///      with any grid snaps and returns a new rectangle.  If
            ///      no changes to the rectangle's size were needed, this
            ///      may return the same rectangle. 
            /// </devdoc>
            public override Rectangle GetUpdatedRect(Rectangle originalRect, Rectangle dragRect, bool updateSize) { 
                return dragRect; 
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.SetCursor"]/*' />
            /// <devdoc>
            ///     Asks the handler to set the appropriate cursor
            /// </devdoc> 
            public override void SetCursor() {
                tray.OnSetCursor(); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ComponentTray.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Runtime.Serialization.Formatters; 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Serialization; 
    using Microsoft.Win32;
    using System.Drawing; 
    using System.Design;
    using System.Drawing.Design;
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel; 
    using System.Windows.Forms.Design.Behavior;
    using System.Diagnostics.CodeAnalysis; 
 
    /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides the component tray UI for the form designer.</para>
    /// </devdoc>
    [ 
    ToolboxItem(false),
    DesignTimeVisible(false), 
    ProvideProperty("Location", typeof(IComponent)), 
    ProvideProperty("TrayLocation", typeof(IComponent)),   // VSWhidbey# 420631
    ] 
    public class ComponentTray : ScrollableControl, IExtenderProvider, ISelectionUIHandler, IOleDragClient {

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.InvalidPoint"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// </devdoc> 
        private static readonly Point InvalidPoint = new Point(int.MinValue, int.MinValue); 

        private  IServiceProvider   serviceProvider;    // Where services come from. 

        private Point                   whiteSpace = Point.Empty;         // space to leave between components.
        private Size                    grabHandle = Size.Empty; // Size of the grab handles.
 
        private ArrayList               controls;           // List of items in the tray in the order of their layout.
 
        private SelectionUIHandler      dragHandler;        // the thing responsible for handling mouse drags 
        private ISelectionUIService     selectionUISvc;     // selectiuon UI; we use this a lot
        private IToolboxService         toolboxService;     // cached for drag/drop 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.oleDragDropHandler"]/*' />
        /// <devdoc>
        ///    <para>Provides drag and drop functionality through OLE.</para> 
        /// </devdoc>
        internal OleDragDropHandler     oleDragDropHandler; // handler class for ole drag drop operations. 
 
        private IDesigner               mainDesigner;       // the designer that is associated with this tray
        private IEventHandlerService    eventHandlerService = null; // Event Handler service to handle keyboard and focus. 
        private bool                    queriedTabOrder;
        private MenuCommand             tabOrderCommand;
        private ICollection             selectedObjects;
 
        // Services that we use on a high enough frequency to merit caching.
        // 
        private IMenuCommandService     menuCommandService; 
        private CommandSet              privateCommandSet=null;
        private InheritanceUI           inheritanceUI; 

        private Point       mouseDragStart = InvalidPoint;       // the starting location of a drag
        private Point       mouseDragEnd = InvalidPoint;         // the ending location of a drag
        private Rectangle   mouseDragWorkspace = Rectangle.Empty;   // a temp work rectangle we cache for perf 
        private ToolboxItem mouseDragTool;        // the tool that's being dragged; only for drag/drop
        private Point       mouseDropLocation = InvalidPoint;    // where the tool was dropped 
        private bool        showLargeIcons = false;// Show Large icons or not. 
        private bool        autoArrange = false;   // allows for auto arranging icons.
        private Point       autoScrollPosBeforeDragging = Point.Empty;//Used to return the correct scroll pos. after a drag 

        // Component Tray Context menu items...
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.menucmdArrangeIcons"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        private MenuCommand menucmdArrangeIcons = null; 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.menucmdLineupIcons"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        private MenuCommand menucmdLineupIcons = null;
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.menucmdLargeIcons"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        private MenuCommand menucmdLargeIcons = null;
 
        private bool fResetAmbient = false;

        private ComponentTrayGlyphManager glyphManager;//used to manage any glyphs added to the tray
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ComponentTray"]/*' />
        /// <devdoc> 
        ///      Creates a new component tray.  The component tray 
        ///      will monitor component additions and removals and create
        ///      appropriate UI objects in its space. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        public ComponentTray(IDesigner mainDesigner, IServiceProvider serviceProvider) {
            this.AutoScroll = true; 
            this.mainDesigner = mainDesigner;
            this.serviceProvider = serviceProvider; 
            this.AllowDrop = true; 
            Text = "ComponentTray"; // makes debugging easier
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true); 

            controls = new ArrayList();

            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            IExtenderProviderService es = (IExtenderProviderService)GetService(typeof(IExtenderProviderService));
            Debug.Assert(es != null, "Component tray wants an extender provider service, but there isn't one."); 
            if (es != null) { 
                es.AddExtenderProvider(this);
            } 

            if (GetService(typeof(IEventHandlerService)) == null) {
                if (host != null) {
                    eventHandlerService = new EventHandlerService(this); 
                    host.AddService(typeof(IEventHandlerService), eventHandlerService);
                } 
            } 

            IMenuCommandService mcs = MenuService; 
            if (mcs != null) {
                Debug.Assert(menucmdArrangeIcons == null, "Non-Null Menu Command for ArrangeIcons");
                Debug.Assert(menucmdLineupIcons  == null, "Non-Null Menu Command for LineupIcons");
                Debug.Assert(menucmdLargeIcons   == null, "Non-Null Menu Command for LargeIcons"); 

                menucmdArrangeIcons = new MenuCommand(new EventHandler(OnMenuArrangeIcons), StandardCommands.ArrangeIcons); 
                menucmdLineupIcons = new MenuCommand(new EventHandler(OnMenuLineupIcons), StandardCommands.LineupIcons); 
                menucmdLargeIcons = new MenuCommand(new EventHandler(OnMenuShowLargeIcons), StandardCommands.ShowLargeIcons);
 
                menucmdArrangeIcons.Checked = AutoArrange;
                menucmdLargeIcons.Checked   = ShowLargeIcons;
                mcs.AddCommand(menucmdArrangeIcons);
                mcs.AddCommand(menucmdLineupIcons); 
                mcs.AddCommand(menucmdLargeIcons);
            } 
 
            IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
 
            if (componentChangeService != null) {
                componentChangeService.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved);
            }
 
            IUIService uiService = (IUIService)GetService(typeof(IUIService));
            if (uiService != null) { 
                Color styleColor; 

                //Can't use 'as' here since Color is a value type 
                if (uiService.Styles["VsColorDesignerTray"] is Color) {
                    styleColor = (Color) uiService.Styles["VsColorDesignerTray"];
                }
                else if (uiService.Styles["HighlightColor"] is Color) { 
                    // Since v1, we have had code here that checks for HighlightColor, so some hosts (like WinRes)
                    // have been setting it. If VsColorDesignerTray isn't present, we look for HighlightColor 
                    // for backward compat. 
                    styleColor = (Color) uiService.Styles["HighlightColor"];
                } 
                else {
                    //No style color provided? Let's pick a default.
                    styleColor = SystemColors.Info;
                } 

 
                BackColor = styleColor; 
                Font = (Font)uiService.Styles["DialogFont"];
            } 

            ISelectionService selSvc = (ISelectionService)GetService(typeof(ISelectionService));
            if (selSvc != null) {
                selSvc.SelectionChanged += new EventHandler(OnSelectionChanged); 
            }
 
            // Listen to the SystemEvents so that we can resync selection based on display settings etc. 
            SystemEvents.DisplaySettingsChanged += new EventHandler(this.OnSystemSettingChanged);
            SystemEvents.InstalledFontsChanged += new EventHandler(this.OnSystemSettingChanged); 
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(this.OnUserPreferenceChanged);

            // Listen to refresh events from TypeDescriptor.  If a component gets refreshed, we re-query
            // and will hide/show the view based on the DesignerView attribute. 
            //
            TypeDescriptor.Refreshed += new RefreshEventHandler(OnComponentRefresh); 
 
            BehaviorService behSvc = GetService(typeof(BehaviorService)) as BehaviorService;
            if(behSvc != null) { 
                //this object will manage any glyphs that get added to our tray
                glyphManager = new ComponentTrayGlyphManager(selSvc, behSvc);
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.AutoArrange"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public bool AutoArrange {
            get {
                return autoArrange;
            } 

            set { 
                if (autoArrange != value) { 
                    autoArrange = value;
                    menucmdArrangeIcons.Checked = value; 

                    if (autoArrange) {
                        DoAutoArrange(true);
                    } 
                }
            } 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ComponentCount"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets the number of compnents contained within this tray.
        ///    </para> 
        /// </devdoc>
        public int ComponentCount { 
            get { 
                return Controls.Count;
            } 
        }

        internal virtual SelectionUIHandler DragHandler {
            get { 
                if (dragHandler == null) {
                    dragHandler = new TraySelectionUIHandler(this); 
                } 
                return dragHandler;
            } 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.Glyphs"]/*' />
        /// <devdoc> 
        ///     Internally exposes a way for sited components to add
        ///     glyphs to the component tray. 
        /// </devdoc> 
        internal GlyphCollection SelectionGlyphs {
            get { 
                if(glyphManager != null) {
                    return glyphManager.SelectionGlyphs;
                } else {
                    return null; 
                }
            } 
        } 

        private InheritanceUI InheritanceUI { 
            get {
                if (inheritanceUI == null) {
                    inheritanceUI = new InheritanceUI();
                } 
                return inheritanceUI;
            } 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.MenuService"]/*' /> 
        /// <devdoc>
        ///     Retrieves the menu editor service, which we cache for speed.
        /// </devdoc>
        private IMenuCommandService MenuService { 
            get {
                if (menuCommandService == null) { 
                    menuCommandService = (IMenuCommandService)GetService(typeof(IMenuCommandService)); 
                }
                return menuCommandService; 
            }
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ShowLargeIcons"]/*' /> 
        /// <devdoc>
        ///     Determines whether the tray will show large icon view or not. 
        /// </devdoc> 
        public bool ShowLargeIcons {
            get { 
                return showLargeIcons;
            }

            set { 
                if (showLargeIcons != value) {
                    showLargeIcons = value; 
                    menucmdLargeIcons.Checked = ShowLargeIcons; 

                    ResetTrayControls(); 
                    Invalidate(true);
                }
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TabOrderActive"]/*' /> 
        /// <devdoc> 
        ///      Determines if the tab order UI is active.  When tab order is active, the tray is locked in
        ///      a "read only" mode. 
        /// </devdoc>
        private bool TabOrderActive {
            get {
                if (!queriedTabOrder) { 
                    queriedTabOrder = true;
                    IMenuCommandService mcs = MenuService; 
                    if (mcs != null) { 
                        tabOrderCommand = mcs.FindCommand(MenuCommands.TabOrder);
                    } 
                }

                if (tabOrderCommand != null) {
                    return tabOrderCommand.Checked; 
                }
 
                return false; 
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IsWindowVisible"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        ///    <para>Indicates whether the window is visible.</para>
        /// </devdoc> 
        internal bool IsWindowVisible { 
            get {
                if (this.IsHandleCreated) { 
                    return NativeMethods.IsWindowVisible(this.Handle);
                }
                return false;
            } 
        }
 
        internal Size ParentGridSize { 
            get {
                ParentControlDesigner designer = mainDesigner as ParentControlDesigner; 
                if (designer != null) {
                    return designer.ParentGridSize;
                }
 
                return new Size(8, 8);
            } 
        } 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.AddComponent"]/*' />
        /// <devdoc> 
        ///    <para>Adds a component to the tray.</para>
        /// </devdoc>
        public virtual void AddComponent(IComponent component) {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 

            // Ignore components that cannot be added to the tray 
            if (!CanDisplayComponent(component)) { 
                return;
            } 

            // And designate us as the selection UI handler for the
            // control.
            // 
            if (selectionUISvc == null) {
                selectionUISvc = (ISelectionUIService)GetService(typeof(ISelectionUIService)); 
 
                // If there is no selection service, then we will provide our own.
                // 
                if (selectionUISvc == null) {
                    selectionUISvc = new SelectionUIService(host);
                    host.AddService(typeof(ISelectionUIService), selectionUISvc);
                    //privateCommandSet = new CommandSet(mainDesigner.Component.Site); 
                }
 
                grabHandle = selectionUISvc.GetAdornmentDimensions(AdornmentType.GrabHandle); 
            }
 
            // Create a new instance of a tray control.
            //
            TrayControl trayctl = new TrayControl(this, component);
 
            SuspendLayout();
            try { 
                // Add it to us. 
                //
                Controls.Add(trayctl); 
                controls.Add(trayctl);

                // CanExtend can actually be called BEFORE the component is added to the ComponentTray.
                // ToolStrip is such as scenario: 
                // 1. Add a timer to the Tray.
                // 2. Add a ToolStrip. 
                // 3. ToolStripDesigner.Initialize will be called before ComponentTray.AddComponent, 
                //      so the ToolStrip is not yet added to the tray.
                // 4. TooStripDesigner.Initialize calls GetProperties, which causes our CanExtend to be called. 
                // 5. CanExtend will return false, since the component has not yet been added.
                // 6. This causes all sorts of badness/
                // Fix is to refresh.
                TypeDescriptor.Refresh(component); 

                if (host != null && !host.Loading) { 
                    PositionControl(trayctl); 
                }
                if (selectionUISvc != null) { 
                    selectionUISvc.AssignSelectionUIHandler(component, this);
                }

 
                InheritanceAttribute attr = trayctl.InheritanceAttribute;
                if (attr.InheritanceLevel != InheritanceLevel.NotInherited) { 
                    InheritanceUI iui = InheritanceUI; 
                    if (iui != null) {
                        iui.AddInheritedControl(trayctl, attr.InheritanceLevel); 
                    }
                }
            }
            finally { 
                ResumeLayout();
            } 
 
            if (host != null && !host.Loading) {
                ScrollControlIntoView(trayctl); 
            }
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IExtenderProvider.CanExtend"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// <para> 
        /// Gets whether or not this extender provider can extend the given
        /// component. We only extend components that have been added 
        /// to our UI.
        /// </para>
        /// </devdoc>
        bool IExtenderProvider.CanExtend(object component) { 
            IComponent comp = component as IComponent;
            return (comp != null) && (TrayControl.FromComponent(comp) != null); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="CanCreateComponentFromTool"]/*' /> 
        protected virtual bool CanCreateComponentFromTool(ToolboxItem tool) {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(host != null, "Service object could not provide us with a designer host.");
 
            // Disallow controls to be added to the component tray.
            Type compType = host.GetType(tool.TypeName); 
 
            if (compType == null)
                return true; 

            if (!compType.IsSubclassOf(typeof(Control))) {
                return true;
            } 

            Type designerType = GetDesignerType(compType, typeof(IDesigner)); 
 
            if (typeof(ControlDesigner).IsAssignableFrom(designerType)) {
                return false; 
            }

            return true;
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.CanDisplayComponent"]/*' /> 
        /// <devdoc> 
        ///     This method determines if a UI representation for the given component should be provided.
        ///     If it returns true, then the component will get a glyph in the tray area.  If it returns 
        ///     false, then the component will not actually be added to the tray.  The default
        ///     implementation looks for DesignTimeVisibleAttribute.Yes on the component's class.
        /// </devdoc>
        protected virtual bool CanDisplayComponent(IComponent component) { 
            return TypeDescriptor.GetAttributes(component).Contains(DesignTimeVisibleAttribute.Yes);
        } 
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.CreateComponentFromTool"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CreateComponentFromTool(ToolboxItem tool) {
            if (!CanCreateComponentFromTool(tool)) { 
                return;
            } 
 
            // We invoke the drag drop handler for this.  This implementation is shared between all designers that
            // create components. 
            //
            GetOleDragHandler().CreateTool(tool, null, 0, 0, 0, 0, false, false);
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.DisplayError"]/*' />
        /// <devdoc> 
        ///      Displays the given exception to the user. 
        /// </devdoc>
        protected void DisplayError(Exception e) { 
            IUIService uis = (IUIService)GetService(typeof(IUIService));
            if (uis != null) {
                uis.ShowError(e);
            } 
            else {
                string message = e.Message; 
                if (message == null || message.Length == 0) { 
                    message = e.ToString();
                } 
                RTLAwareMessageBox.Show(null, message, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                            MessageBoxDefaultButton.Button1, 0);
            }
        } 

        // 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.Dispose"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Disposes of the resources (other than memory) used by the component tray object.
        ///    </para>
        /// </devdoc>
        protected override void Dispose(bool disposing) { 
            if (disposing && controls != null) {
                IExtenderProviderService es = (IExtenderProviderService)GetService(typeof(IExtenderProviderService)); 
                if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(es != null, "IExtenderProviderService not found"); 
                if (es != null) {
                    es.RemoveExtenderProvider(this); 
                }

                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                if (eventHandlerService != null) { 
                    if (host != null) {
                        host.RemoveService(typeof(IEventHandlerService)); 
                        eventHandlerService = null; 
                    }
                } 

                IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));

                if (componentChangeService != null) { 
                    componentChangeService.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved);
                } 
 
                TypeDescriptor.Refreshed -= new RefreshEventHandler(OnComponentRefresh);
                SystemEvents.DisplaySettingsChanged -= new EventHandler(this.OnSystemSettingChanged); 
                SystemEvents.InstalledFontsChanged -= new EventHandler(this.OnSystemSettingChanged);
                SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(this.OnUserPreferenceChanged);

                IMenuCommandService mcs = MenuService; 
                if (mcs != null) {
                    Debug.Assert(menucmdArrangeIcons != null, "Null Menu Command for ArrangeIcons"); 
                    Debug.Assert(menucmdLineupIcons  != null, "Null Menu Command for LineupIcons"); 
                    Debug.Assert(menucmdLargeIcons   != null, "Null Menu Command for LargeIcons");
                    mcs.RemoveCommand(menucmdArrangeIcons); 
                    mcs.RemoveCommand(menucmdLineupIcons);
                    mcs.RemoveCommand(menucmdLargeIcons);
                }
 
                if (privateCommandSet != null) {
                    privateCommandSet.Dispose(); 
 
                    // If we created a private command set, we also added a selection ui service to the host
                    if (host != null) { 
                        host.RemoveService(typeof(ISelectionUIService));
                    }

                } 
                selectionUISvc = null;
 
                if (inheritanceUI != null) { 
                    inheritanceUI.Dispose();
                    inheritanceUI = null; 
                }

                serviceProvider = null;
                controls.Clear(); 
                controls = null;
 
                if (glyphManager != null) { 
                    glyphManager.Dispose();
                    glyphManager = null; 
                }
            }
            base.Dispose(disposing);
        } 

        private void DoAutoArrange(bool dirtyDesigner) { 
            if (controls == null || controls.Count <= 0) { 
                return;
            } 

            controls.Sort(new AutoArrangeComparer());

            SuspendLayout(); 

            //Reset the autoscroll position before auto arranging. 
            //This way, when OnLayout gets fired after this, we won't 
            //have to move every component again.  Note that sync'ing
            //the selection will automatically select & scroll into view 
            //the right components
            this.AutoScrollPosition = new Point(0,0);

            try { 
                Control prevCtl = null;
                bool positionedGlobal = true; 
                foreach(Control ctl in controls) { 
                    if (!ctl.Visible)
                        continue; 

                    // If we're auto arranging, always move the control.  If not,
                    // move the control only if it was never given a position.  This
                    // auto arranges it until the user messes with it, or until its 
                    // position is saved into the resx.
                    // (if one control is no longer positioned, move all the other one as 
                    // we don't want them to go under one another) 
                    if (autoArrange) {
                        PositionInNextAutoSlot(ctl as TrayControl, prevCtl, dirtyDesigner); 
                    }
                    else if (!((TrayControl)ctl).Positioned || !positionedGlobal) {
                        PositionInNextAutoSlot(ctl as TrayControl, prevCtl, false);
                        positionedGlobal = false; 
                    }
                    prevCtl = ctl; 
                } 

                if (selectionUISvc != null) { 
                    selectionUISvc.SyncSelection();
                }
            }
            finally { 
                ResumeLayout();
            } 
        } 

        private void DoLineupIcons() { 
            if (autoArrange)
                return;

            bool oldValue = autoArrange; 
            autoArrange = true;
 
            try { 
                DoAutoArrange(true);
            } 
            finally {
                autoArrange = oldValue;
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.DrawRubber"]/*' /> 
        /// <devdoc> 
        ///      Draws a rubber band at the given coordinates.  The coordinates
        ///      can be transposed. 
        /// </devdoc>
        private void DrawRubber(Point start, Point end) {
            mouseDragWorkspace.X = Math.Min(start.X, end.X);
            mouseDragWorkspace.Y = Math.Min(start.Y, end.Y); 
            mouseDragWorkspace.Width = Math.Abs(end.X - start.X);
            mouseDragWorkspace.Height = Math.Abs(end.Y - start.Y); 
 
            mouseDragWorkspace = RectangleToScreen(mouseDragWorkspace);
 
            ControlPaint.DrawReversibleFrame(mouseDragWorkspace, BackColor, FrameStyle.Dashed);
        }

        internal void FocusDesigner() { 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host != null && host.RootComponent != null) { 
                IRootDesigner rd = host.GetDesigner(host.RootComponent) as IRootDesigner; 
                if (rd != null) {
                    ViewTechnology[] techs = rd.SupportedTechnologies; 
                    if (techs.Length > 0) {
                        Control view = rd.GetView(techs[0]) as Control;
                        if (view != null) {
                            view.Focus(); 
                        }
                    } 
                } 
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetComponentsInRect"]/*' />
        /// <devdoc>
        ///     Finds the array of components within the given rectangle.  This uses the rectangle to 
        ///     find controls within our frame, and then uses those controls to find the actual
        ///     components.  It returns an object array so the output can be directly fed into 
        ///     the selection service. 
        /// </devdoc>
        private object[] GetComponentsInRect(Rectangle rect) { 
            ArrayList list = new ArrayList();

            int controlCount = Controls.Count;
 
            for (int i = 0; i < controlCount; i++) {
                Control child = Controls[i]; 
                Rectangle bounds = child.Bounds; 
                TrayControl tc = child as TrayControl;
                if (tc != null && bounds.IntersectsWith(rect)) { 
                    list.Add(tc.Component);
                }
            }
 
            return list.ToArray();
        } 
 
        private Type GetDesignerType(Type t, Type designerBaseType)
        { 
            Type designerType = null;

            // Get the set of attributes for this type
            // 
            AttributeCollection attributes = TypeDescriptor.GetAttributes(t);
 
            for (int i = 0; i < attributes.Count; i++) 
            {
                DesignerAttribute da = attributes[i] as DesignerAttribute; 
                if (da != null)
                {
                    Type attributeBaseType = Type.GetType(da.DesignerBaseTypeName);
                    if (attributeBaseType != null && attributeBaseType == designerBaseType) 
                    {
                        bool foundService = false; 
 
                        ITypeResolutionService tr = (ITypeResolutionService)GetService(typeof(ITypeResolutionService));
                        if (tr != null) 
                        {
                            foundService = true;
                            designerType = tr.GetType(da.DesignerTypeName);
                        } 

                        if (!foundService) 
                        { 
                            designerType = Type.GetType(da.DesignerTypeName);
                        } 

                        if (designerType != null)
                        {
                            break; 
                        }
                    } 
                } 
            }
 
            return designerType;
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetDragDimensions"]/*' /> 
        /// <devdoc>
        ///     Returns the drag dimensions needed to move the currently selected 
        ///     component one way or the other. 
        /// </devdoc>
        internal Size GetDragDimensions() { 

            // This is a really gross approximation of the correct diemensions.
            //
            if (AutoArrange) { 
                ISelectionService ss = (ISelectionService)GetService(typeof(ISelectionService));
                IComponent comp = null; 
 
                if (ss != null) {
                    comp = (IComponent)ss.PrimarySelection; 
                }

                Control control = null;
 
                if (comp != null) {
                    control = ((IOleDragClient)this).GetControlForComponent(comp); 
                } 

                if (control == null && controls.Count > 0) { 
                    control = (Control)controls[0];
                }

                if (control != null) { 
                    Size s = control.Size;
                    s.Width += 2 * whiteSpace.X; 
                    s.Height += 2 * whiteSpace.Y; 
                    return s;
                } 
            }

            return new Size(10, 10);
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetNextComponent"]/*' /> 
        /// <devdoc> 
        ///     Similar to GetNextControl on Control, this method returns the next
        ///     component in the tray, given a starting component.  It will return 
        ///     null if the end (or beginning, if forward is false) of the list
        ///     is encountered.
        /// </devdoc>
        public IComponent GetNextComponent(IComponent component, bool forward) { 

            for (int i = 0; i < controls.Count; i++) { 
                TrayControl control = (TrayControl)controls[i]; 
                if (control.Component == component) {
 
                    int targetIndex = (forward ? i + 1 : i - 1);

                    if (targetIndex >= 0 && targetIndex < controls.Count) {
                        return((TrayControl)controls[targetIndex]).Component; 
                    }
 
                    // Reached the end of the road. 
                    return null;
                } 
            }

            // If we got here then the component isn't in our list.  Prime the
            // caller with either the first or the last. 

            if (controls.Count > 0) { 
                int targetIndex = (forward ? 0 : controls.Count -1); 
                return((TrayControl)controls[targetIndex]).Component;
            } 

            return null;
        }
 
        internal virtual OleDragDropHandler GetOleDragHandler() {
            if (oleDragDropHandler == null) { 
                oleDragDropHandler = new TrayOleDragDropHandler(this.DragHandler, this.serviceProvider, this); 
            }
            return oleDragDropHandler; 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetLocation"]/*' />
        /// <devdoc> 
        ///     Accessor method for the location extender property.  We offer this extender
        ///     to all non-visual components. 
        /// </devdoc> 
        [
            Category("Layout"), 
            Localizable(false),
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            SRDescription("ControlLocationDescr"), 
            DesignOnly(true),
        ] 
 	    [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")] 
        public Point GetLocation(IComponent receiver) {
            // We shouldn't really end up here, but if we do.... 

            PropertyDescriptor loc = TypeDescriptor.GetProperties(receiver.GetType())["Location"];
            if (loc != null) {
                // In this case the component already had a Location property, and what the caller 
                // wants is the underlying components Location, not the tray location. Why?
                // Because we now use TrayLocation. 
                return (Point)(loc.GetValue(receiver)); 
            }
            else { 
                // If the component didn't already have a Location property, then the caller
                // really wants the tray location. Could be a 3rd party vendor.
                return GetTrayLocation(receiver);
            } 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetLocation"]/*' /> 
        /// <devdoc>
        ///     Accessor method for the location extender property.  We offer this extender 
        ///     to all non-visual components.
        /// </devdoc>
        [
            Category("Layout"), 
            Localizable(false),
            Browsable(false), 
            SRDescription("ControlLocationDescr"), 
            DesignOnly(true),
        ] 
        public Point GetTrayLocation(IComponent receiver) {
            Control c = TrayControl.FromComponent(receiver);

            if (c == null) { 
                Debug.Fail("Anything we're extending should have a component view.");
                return new Point(); 
            } 

            Point loc = c.Location; 
            Point autoScrollLoc = this.AutoScrollPosition;

            return new Point(loc.X - autoScrollLoc.X, loc.Y - autoScrollLoc.Y);
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetService"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Gets the requsted service type. 
        ///    </para>
        /// </devdoc>
        protected override object GetService(Type serviceType) {
            object service = null; 

            Debug.Assert(serviceProvider != null, "Trying to access services too late or too early."); 
            if (serviceProvider != null) { 
                service = serviceProvider.GetService(serviceType);
            } 

            return service;
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.GetTrayControlFromComponent"]/*' />
        /// <devdoc> 
        ///     Returns the traycontrol representing the IComponent.  If no 
        ///     traycontrol is found, this returns null.  This is used identify
        ///     bounds for the DesignerAction UI. 
        /// </devdoc>
        internal TrayControl GetTrayControlFromComponent(IComponent comp) {
            return TrayControl.FromComponent(comp);
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IsTrayComponent"]/*' /> 
        /// <devdoc> 
        /// Returns true if the given componenent is being shown on the tray.
        /// </devdoc> 
        public bool IsTrayComponent(IComponent comp) {

            if (TrayControl.FromComponent(comp) == null) {
                return false; 
            }
 
            foreach (Control control in this.Controls) { 
                TrayControl tc = control as TrayControl;
                if (tc != null && tc.Component == comp) { 
                    return true;
                }
            }
 
            return false;
        } 
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnComponentRefresh"]/*' />
        /// <devdoc> 
        ///     Called when a component's metadata is invalidated.  We re-query here and will show/hide
        ///     the control's tray control based on the new metadata.
        /// </devdoc>
        private void OnComponentRefresh(RefreshEventArgs e) { 
            IComponent component = e.ComponentChanged as IComponent;
 
            if (component != null) { 
                TrayControl control = TrayControl.FromComponent(component);
 
                if (control != null) {
                    bool shouldDisplay = CanDisplayComponent(component);
                    if (shouldDisplay != control.Visible || !shouldDisplay) {
                        control.Visible = shouldDisplay; 
                        Rectangle bounds = control.Bounds;
                        bounds.Inflate(grabHandle); 
                        bounds.Inflate(grabHandle); 
                        Invalidate(bounds);
                        PerformLayout(); 
                    }
                }
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnComponentRemoved"]/*' /> 
        /// <devdoc> 
        ///      Called when a component is removed from the container.
        /// </devdoc> 
        private void OnComponentRemoved(object sender, ComponentEventArgs cevent) {
            RemoveComponent(cevent.Component);
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnComponentTrayPaste"]/*' />
        /// <devdoc> 
        ///      Called from CommandSet's OnMenuPaste method.  This will allow us to properly adjust the location 
        ///      of the components in the tray after we've incorreclty set them by deserializing the design time
        ///      properties (and hence called SetValue(c, myBadLocation) on the location property). 
        /// </devdoc>
        internal void UpdatePastePositions(ArrayList components) {
            foreach (TrayControl c in components) {
                if (!CanDisplayComponent(c.Component)) { 
                    return;
                } 
 
                if (mouseDropLocation == InvalidPoint) {
 
                    Control prevCtl = null;
                    if (controls.Count > 1) {
                        prevCtl = (Control)controls[controls.Count-1];
                    } 
                    PositionInNextAutoSlot(c, prevCtl, true);
                } 
                else { 
                    PositionControl(c);
                } 
                c.BringToFront();
            }
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnContextMenu"]/*' />
        /// <devdoc> 
        ///     Called when we are to display our context menu for this component. 
        /// </devdoc>
        private void OnContextMenu(int x, int y, bool useSelection) { 

            if (!TabOrderActive) {
                Capture = false;
 
                IMenuCommandService mcs = MenuService;
                if (mcs != null) { 
                    Capture = false; 
                    Cursor.Clip = Rectangle.Empty;
 
                    ISelectionService s = (ISelectionService)GetService(typeof(ISelectionService));


 
                    if (useSelection && s != null && !(1 == s.SelectionCount && s.PrimarySelection == mainDesigner.Component)) {
                        mcs.ShowContextMenu(MenuCommands.TraySelectionMenu, x, y); 
                    } 
                    else {
                        mcs.ShowContextMenu(MenuCommands.ComponentTrayMenu, x, y); 
                    }
                }
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnDoubleClick"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        protected override void OnMouseDoubleClick(MouseEventArgs e) {
            //give our glyphs first chance at this
            if (glyphManager != null && glyphManager.OnMouseDoubleClick(e)) {
                //handled by a glyph - so don't send to the comp tray 
                return;
            } 
 
            base.OnDoubleClick(e);
 
            if (!TabOrderActive) {
                OnLostCapture();
                IEventBindingService eps = (IEventBindingService)GetService(typeof(IEventBindingService));
                if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(eps != null, "IEventBindingService not found"); 
                if (eps != null) {
                    eps.ShowCode(); 
                } 
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnGiveFeedback"]/*' />
        /// <devdoc>
        ///     Inheriting classes should override this method to handle this event. 
        ///     Call base.onGiveFeedback to send this event to any registered event listeners.
        /// </devdoc> 
        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfevent) { 
            base.OnGiveFeedback(gfevent);
            GetOleDragHandler().DoOleGiveFeedback(gfevent); 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnDragDrop"]/*' />
        /// <devdoc> 
        ///      Called in response to a drag drop for OLE drag and drop.  Here we
        ///      drop a toolbox component on our parent control. 
        /// </devdoc> 
        protected override void OnDragDrop(DragEventArgs de) {
            // This will be used once during PositionComponent to place the component 
            // at the drop point.  It is automatically set to null afterwards, so further
            // components appear after the first one dropped.
            //
            mouseDropLocation = PointToClient(new Point(de.X, de.Y)); 
            autoScrollPosBeforeDragging = this.AutoScrollPosition;//save the scroll position
 
            if (mouseDragTool != null) { 
                ToolboxItem tool = mouseDragTool;
                mouseDragTool = null; 

                if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(GetService(typeof(IDesignerHost)) != null, "IDesignerHost not found");

                try { 
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                    IDesigner designer = host.GetDesigner(host.RootComponent); 
 
                    IToolboxUser itu = designer as IToolboxUser;
                    if (itu != null) { 
                        itu.ToolPicked(tool);
                    }
                    else {
                        CreateComponentFromTool(tool); 
                    }
                } 
                catch (Exception e) { 
                    DisplayError(e);
                    if (ClientUtils.IsCriticalException(e)) { 
                        throw;
                    }
                }
                catch { 
                }
 
                de.Effect = DragDropEffects.Copy; 

            } 
            else {
                GetOleDragHandler().DoOleDragDrop(de);
            }
 
            mouseDropLocation = InvalidPoint;
            ResumeLayout(); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnDragEnter"]/*' /> 
        /// <devdoc>
        ///      Called in response to a drag enter for OLE drag and drop.
        /// </devdoc>
        protected override void OnDragEnter(DragEventArgs de) { 
            if (!TabOrderActive) {
                SuspendLayout(); 
                if (toolboxService == null) { 
                    toolboxService = (IToolboxService)GetService(typeof(IToolboxService));
                } 

                OleDragDropHandler dragDropHandler = GetOleDragHandler();
                Object[] dragComps = dragDropHandler.GetDraggingObjects(de);
 
                // Only assume the items came from the ToolBox if dragComps == null
                // 
                if (toolboxService != null && dragComps == null) { 
                    mouseDragTool = toolboxService.DeserializeToolboxItem(de.Data, (IDesignerHost)GetService(typeof(IDesignerHost)));
                } 

                if (mouseDragTool != null) {
                    Debug.Assert(0 != (int)(de.AllowedEffect & (DragDropEffects.Move | DragDropEffects.Copy)), "DragDropEffect.Move | .Copy isn't allowed?");
                    if ((int)(de.AllowedEffect & DragDropEffects.Move) != 0) { 
                        de.Effect = DragDropEffects.Move;
                    } 
                    else { 
                        de.Effect = DragDropEffects.Copy;
                    } 
                }
                else {
                    dragDropHandler.DoOleDragEnter(de);
                } 
            }
        } 
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnDragLeave"]/*' />
        /// <devdoc> 
        ///     Called when a drag-drop operation leaves the control designer view
        ///
        /// </devdoc>
        protected override void OnDragLeave(EventArgs e) { 
            mouseDragTool = null;
            GetOleDragHandler().DoOleDragLeave(); 
            ResumeLayout(); 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnDragOver"]/*' />
        /// <devdoc>
        ///     Called when a drag drop object is dragged over the control designer view
        /// </devdoc> 
        protected override void OnDragOver(DragEventArgs de) {
            if (mouseDragTool != null) { 
                Debug.Assert(0!=(int)(de.AllowedEffect & DragDropEffects.Copy), "DragDropEffect.Move isn't allowed?"); 
                de.Effect = DragDropEffects.Copy;
            } 
            else {
                GetOleDragHandler().DoOleDragOver(de);
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnLayout"]/*' /> 
        /// <internalonly/> 
        /// <devdoc>
        ///    Forces the layout of any docked or anchored child controls. 
        /// </devdoc>
        protected override void OnLayout(LayoutEventArgs levent) {
            DoAutoArrange(false);
            // make sure selection service redraws 
            Invalidate(true);
            base.OnLayout(levent); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnLostCapture"]/*' /> 
        /// <devdoc>
        ///      This is called when we lose capture.  Here we get rid of any
        ///      rubber band we were drawing.  You should put any cleanup
        ///      code in here. 
        /// </devdoc>
        protected virtual void OnLostCapture() { 
            if (mouseDragStart != InvalidPoint) { 
                Cursor.Clip = Rectangle.Empty;
                if (mouseDragEnd != InvalidPoint) { 
                    DrawRubber(mouseDragStart, mouseDragEnd);
                    mouseDragEnd = InvalidPoint;
                }
                mouseDragStart = InvalidPoint; 
            }
        } 
 
        private void OnMenuArrangeIcons(object sender, EventArgs e) {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            DesignerTransaction t = null;

            try {
                t = host.CreateTransaction(SR.GetString(SR.TrayAutoArrange)); 

                PropertyDescriptor trayAAProp = TypeDescriptor.GetProperties(mainDesigner.Component)["TrayAutoArrange"]; 
                if (trayAAProp != null) { 
                    trayAAProp.SetValue(mainDesigner.Component, !AutoArrange);
                } 
            }
            finally {
                if (t != null)
                    t.Commit(); 
            }
        } 
 
        private void OnMenuShowLargeIcons(object sender, EventArgs e) {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            DesignerTransaction t = null;

            try {
                t = host.CreateTransaction(SR.GetString(SR.TrayShowLargeIcons)); 
                PropertyDescriptor trayIconProp = TypeDescriptor.GetProperties(mainDesigner.Component)["TrayLargeIcon"];
                if (trayIconProp != null) { 
                    trayIconProp.SetValue(mainDesigner.Component, !ShowLargeIcons); 
                }
            } 
            finally {
                if (t != null)
                    t.Commit();
            } 
        }
 
        private void OnMenuLineupIcons(object sender, EventArgs e) { 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            DesignerTransaction t = null; 
            try {
                t = host.CreateTransaction(SR.GetString(SR.TrayLineUpIcons));
                DoLineupIcons();
            } 
            finally {
                if (t != null) 
                    t.Commit(); 
            }
        } 

        /// <devdoc>
        ///     Used to forward messages from the related ComponentTray Glyph
        ///     to this ComponentTray class. 
        /// </devdoc>
        internal void OnMessage(ref Message m) { 
            this.WndProc(ref m); 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnMouseDown"]/*' />
        /// <devdoc>
        ///     Inheriting classes should override this method to handle this event.
        ///     Call base.onMouseDown to send this event to any registered event listeners. 
        /// </devdoc>
        protected override void OnMouseDown(MouseEventArgs e) { 
 
            //give our glyphs first chance at this
            if (glyphManager != null && glyphManager.OnMouseDown(e)) { 
                //handled by a glyph - so don't send to the comp tray
                return;
            }
 
            base.OnMouseDown(e);
 
            if (!TabOrderActive) { 
                if (toolboxService == null) {
                    toolboxService = (IToolboxService)GetService(typeof(IToolboxService)); 
                }


                FocusDesigner(); 

                if (e.Button == MouseButtons.Left && toolboxService != null) { 
                    ToolboxItem tool = toolboxService.GetSelectedToolboxItem((IDesignerHost)GetService(typeof(IDesignerHost))); 
                    if (tool != null) {
                        // mouseDropLocation is checked in PositionControl, which should get called as a result of adding a new 
                        // component.  This allows us to set the position without flickering, while still providing support for auto
                        // layout if the control was double clicked or added through extensibility.
                        //
                        mouseDropLocation = new Point(e.X, e.Y); 
                        try {
                            CreateComponentFromTool(tool); 
                            toolboxService.SelectedToolboxItemUsed(); 
                        }
                        catch (Exception ex) { 
                            DisplayError(ex);
                            if (ClientUtils.IsCriticalException(ex)) {
                                throw;
                            } 
                        }
                        catch { 
                        } 
                        mouseDropLocation = InvalidPoint;
                        return; 
                    }
                }

                // If it is the left button, start a rubber band drag to laso 
                // controls.
                // 
                if (e.Button == MouseButtons.Left) { 
                    mouseDragStart = new Point(e.X, e.Y);
                    Capture = true; 
                    Cursor.Clip = RectangleToScreen(ClientRectangle);

                }
                else { 
                    try {
                        ISelectionService ss = (ISelectionService)GetService(typeof(ISelectionService)); 
                        if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(ss != null, "ISelectionService not found"); 
                        if (ss != null) {
                            ss.SetSelectedComponents(new object[] {mainDesigner.Component}); 
                        }
                    }
                    catch (Exception ex) {
                        // nothing we can really do here; just eat it. 
                        if (ClientUtils.IsCriticalException(ex)) {
                            throw; 
                        } 
                    }
 
                    catch {
                    }
                }
            } 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnMouseMove"]/*' /> 
        /// <devdoc>
        ///     Inheriting classes should override this method to handle this event. 
        ///     Call base.onMouseMove to send this event to any registered event listeners.
        /// </devdoc>
        protected override void OnMouseMove(MouseEventArgs e) {
 
            //give our glyphs first chance at this
            if (glyphManager != null && glyphManager.OnMouseMove(e)) { 
                //handled by a glyph - so don't send to the comp tray 
                return;
            } 

            base.OnMouseMove(e);

            // If we are dragging, then draw our little rubber band. 
            //
            if (mouseDragStart != InvalidPoint) { 
                if (mouseDragEnd != InvalidPoint) { 
                    DrawRubber(mouseDragStart, mouseDragEnd);
                } 
                else {
                    mouseDragEnd = new Point(0, 0);
                }
 
                mouseDragEnd.X = e.X;
                mouseDragEnd.Y = e.Y; 
                DrawRubber(mouseDragStart, mouseDragEnd); 
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnMouseUp"]/*' />
        /// <devdoc>
        ///     Inheriting classes should override this method to handle this event. 
        ///     Call base.onMouseUp to send this event to any registered event listeners.
        /// </devdoc> 
        protected override void OnMouseUp(MouseEventArgs e) { 

            //give our glyphs first chance at this 
            if (glyphManager != null && glyphManager.OnMouseUp(e)) {
                //handled by a glyph - so don't send to the comp tray
                return;
            } 

            if (mouseDragStart != InvalidPoint && e.Button == MouseButtons.Left) { 
                object[] comps = null; 

                Capture = false; 
                Cursor.Clip = Rectangle.Empty;

                if (mouseDragEnd != InvalidPoint) {
                    DrawRubber(mouseDragStart, mouseDragEnd); 

                    Rectangle rect = new Rectangle(); 
                    rect.X = Math.Min(mouseDragStart.X, e.X); 
                    rect.Y = Math.Min(mouseDragStart.Y, e.Y);
                    rect.Width = Math.Abs(e.X - mouseDragStart.X); 
                    rect.Height = Math.Abs(e.Y - mouseDragStart.Y);
                    comps = GetComponentsInRect(rect);
                    mouseDragEnd = InvalidPoint;
                } 
                else {
                    comps = new object[0]; 
                } 

                if (comps.Length == 0) { 
                    comps = new object[] {mainDesigner.Component};
                }

                try { 
                    ISelectionService ss = (ISelectionService)GetService(typeof(ISelectionService));
                    if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(ss != null, "ISelectionService not found"); 
                    if (ss != null) { 
                        ss.SetSelectedComponents(comps);
                    } 
                }
                catch (Exception ex) {
                    // nothing we can really do here; just eat it.
                    if (ClientUtils.IsCriticalException(ex)) { 
                        throw;
                    } 
                } 
                catch {
                } 

                mouseDragStart = InvalidPoint;
            }
 

            base.OnMouseUp(e); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnPaint"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnPaint(PaintEventArgs pe) { 
            if (fResetAmbient) {
                fResetAmbient = false; 
 
                IUIService uiService = (IUIService)GetService(typeof(IUIService));
                if (uiService != null) { 
                    Color styleColor;
                    //Can't use 'as' here since Color is a value type
                    if (uiService.Styles["VsColorDesignerTray"] is Color) {
                        styleColor = (Color) uiService.Styles["VsColorDesignerTray"]; 
                    }
                    else if (uiService.Styles["HighlightColor"] is Color) { 
                        // Since v1, we have had code here that checks for HighlightColor, so some hosts (like WinRes) 
                        // have been setting it. If VsColorDesignerTray isn't present, we look for HighlightColor
                        // for backward compat. 
                        styleColor = (Color) uiService.Styles["HighlightColor"];
                    }
                    else {
                        //No style color provided? Let's pick a default. 
                        styleColor = SystemColors.Info;
                    } 
 
                    BackColor = styleColor;
                    Font = (Font)uiService.Styles["DialogFont"]; 
                }
            }

            base.OnPaint(pe); 

            Graphics gr = pe.Graphics; 
 
            // Now, if we have a selection, paint it
            // 
            if (selectedObjects != null) {
                bool first = true;//indicates the first iteration of our foreach loop
                foreach(object o in selectedObjects) {
                    Control c = ((IOleDragClient)this).GetControlForComponent(o); 
                    if (c != null && c.Visible) {
 
                        Rectangle innerRect = c.Bounds; 

                        NoResizeHandleGlyph glyph = new NoResizeHandleGlyph(innerRect, SelectionRules.None, first, null); 

                        DesignerUtils.DrawSelectionBorder(gr, DesignerUtils.GetBoundsForNoResizeSelectionType(innerRect, SelectionBorderGlyphType.Top));
                        DesignerUtils.DrawSelectionBorder(gr, DesignerUtils.GetBoundsForNoResizeSelectionType(innerRect, SelectionBorderGlyphType.Bottom));
                        DesignerUtils.DrawSelectionBorder(gr, DesignerUtils.GetBoundsForNoResizeSelectionType(innerRect, SelectionBorderGlyphType.Left)); 
                        DesignerUtils.DrawSelectionBorder(gr, DesignerUtils.GetBoundsForNoResizeSelectionType(innerRect, SelectionBorderGlyphType.Right));
                        // Need to draw this one last 
                        DesignerUtils.DrawNoResizeHandle(gr, glyph.Bounds, first, glyph); 
                    }
                    first = false; 
                }
            }
            //paint any glyphs
            if(glyphManager != null) { 
                glyphManager.OnPaintGlyphs(pe);
            } 
        } 

        private void OnSelectionChanged(object sender, EventArgs e) { 
            selectedObjects = ((ISelectionService)sender).GetSelectedComponents();
            object primary = ((ISelectionService)sender).PrimarySelection;
            Invalidate();
 
            // Accessibility information
            // 
 
            foreach(object selObj in selectedObjects) {
                IComponent component = selObj as IComponent; 
                if (component != null) {
                    Control c = TrayControl.FromComponent(component);
                    if (c != null) {
                        Debug.WriteLineIf(CompModSwitches.MSAA.TraceInfo, "MSAA: SelectionAdd, traycontrol = " + c.ToString()); 
                        UnsafeNativeMethods.NotifyWinEvent((int)AccessibleEvents.SelectionAdd, new HandleRef(c, c.Handle), NativeMethods.OBJID_CLIENT, 0);
                    } 
                } 
            }
 
            IComponent comp = primary as IComponent;
            if (comp != null) {
                Control c = TrayControl.FromComponent(comp);
                if (c != null && IsHandleCreated) { 
                    this.ScrollControlIntoView(c);
                    UnsafeNativeMethods.NotifyWinEvent((int)AccessibleEvents.Focus, new HandleRef(c, c.Handle), NativeMethods.OBJID_CLIENT, 0); 
                } 
                if(glyphManager != null) {
                    glyphManager.SelectionGlyphs.Clear(); 
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                    foreach(object selObj in selectedObjects) {
                        IComponent selectedComponent = selObj as IComponent;
                        if(selectedComponent!= null && !(host.GetDesigner(selectedComponent) is ControlDesigner)) { // don't want to do it for controls that are also in the tray 
                            GlyphCollection glyphs = glyphManager.GetGlyphsForComponent(selectedComponent);
                            if (glyphs != null && glyphs.Count > 0) { 
                                SelectionGlyphs.AddRange(glyphs); 
                            }
                        } 
                    }
                }
            }
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.OnSetCursor"]/*' /> 
        /// <devdoc> 
        ///      Sets the cursor.  You may override this to set your own
        ///      cursor. 
        /// </devdoc>
        protected virtual void OnSetCursor() {
            if (toolboxService == null) {
                toolboxService = (IToolboxService)GetService(typeof(IToolboxService)); 
            }
 
            if (toolboxService == null || !toolboxService.SetCursor()) { 
                Cursor.Current = Cursors.Default;
            } 
        }

        private delegate void AsyncInvokeHandler(bool children);
 
        private void OnSystemSettingChanged(object sender, EventArgs e) {
            fResetAmbient = true; 
            ResetTrayControls(); 
            BeginInvoke(new AsyncInvokeHandler(Invalidate), new object[] {true});
        } 

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
            fResetAmbient = true;
            ResetTrayControls(); 
            BeginInvoke(new AsyncInvokeHandler(Invalidate), new object[] {true});
        } 
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.PositionControl"]/*' />
        /// <devdoc> 
        ///      Sets the given control to the correct position on our
        ///      surface.  You may override this to perform your own
        ///      positioning.
        /// </devdoc> 
        private void PositionControl(TrayControl c) {
            Debug.Assert(c.Visible, "TrayControl for " + c.Component + " should not be positioned"); 
 
            if (!autoArrange) {
                if (mouseDropLocation != InvalidPoint) { 
                    if (!c.Location.Equals(mouseDropLocation))
                        c.Location = mouseDropLocation;
                }
                else { 
                    Control prevCtl = null;
                    if (controls.Count > 1) { 
                        // PositionControl can be called when all the controls have been added 
                        // (from IOleDragClient.AddComponent), so we can't use the old
                        // way of looking up the previous control (prevCtl = controls[controls.Count - 2] 
                        int index = controls.IndexOf(c);
                        Debug.Assert(index >= 1, "Got the wrong index, how could that be?");
                        if (index >= 1) {
                            prevCtl = (Control)controls[index - 1]; 
                        }
                    } 
                    PositionInNextAutoSlot(c, prevCtl, true); 
                }
            } 
            else {
                if (mouseDropLocation != InvalidPoint) {
                    RearrangeInAutoSlots(c, mouseDropLocation);
                } 
                else {
                    Control prevCtl = null; 
                    if (controls.Count > 1) { 
                        int index = controls.IndexOf(c);
                        Debug.Assert(index >= 1, "Got the wrong index, how could that be?"); 
                        if (index >= 1) {
                            prevCtl = (Control)controls[index - 1];
                        }
                    } 
                    PositionInNextAutoSlot(c, prevCtl, true);
                } 
            } 

        } 

        private bool PositionInNextAutoSlot(TrayControl c, Control prevCtl, bool dirtyDesigner) {
            Debug.Assert(c.Visible, "TrayControl for " + c.Component + " should not be positioned");
 
            if (whiteSpace.IsEmpty) {
                Debug.Assert(selectionUISvc != null, "No SelectionUIService available for tray."); 
                whiteSpace = new Point(selectionUISvc.GetAdornmentDimensions(AdornmentType.GrabHandle)); 
                whiteSpace.X = whiteSpace.X * 2 + 3;
                whiteSpace.Y = whiteSpace.Y * 2 + 3; 
            }

            if (prevCtl == null) {
                Rectangle display = DisplayRectangle; 
                Point newLoc = new Point(display.X + whiteSpace.X, display.Y + whiteSpace.Y);
                if (!c.Location.Equals(newLoc)) { 
                    c.Location = newLoc; 
                    if (dirtyDesigner) {
                        IComponent comp = c.Component; 
                        Debug.Assert(comp != null, "Component for the TrayControl is null");

                        PropertyDescriptor ctlLocation = TypeDescriptor.GetProperties(comp)["TrayLocation"];
                        if (ctlLocation != null) { 
                            Point autoScrollLoc = this.AutoScrollPosition;
                            newLoc = new Point(newLoc.X - autoScrollLoc.X, newLoc.Y - autoScrollLoc.Y); 
                            ctlLocation.SetValue(comp, newLoc); 
                        }
                    } 
                    else {
                        c.Location = newLoc;
                    }
                    return true; 
                }
            } 
            else { 
                // Calcuate the next location for this control.
                // 
                Rectangle bounds = prevCtl.Bounds;
                Point newLoc = new Point(bounds.X + bounds.Width + whiteSpace.X, bounds.Y);

                // Check to see if it goes over the edge of our window.  If it does, 
                // then wrap it.
                // 
                if (newLoc.X + c.Size.Width > Size.Width) { 
                    newLoc.X = whiteSpace.X;
                    newLoc.Y += bounds.Height + whiteSpace.Y; 
                }

                if (!c.Location.Equals(newLoc)) {
                    if (dirtyDesigner) { 
                        IComponent comp = c.Component;
                        Debug.Assert(comp != null, "Component for the TrayControl is null"); 
 
                        PropertyDescriptor ctlLocation = TypeDescriptor.GetProperties(comp)["TrayLocation"];
                        if (ctlLocation != null) { 
                            Point autoScrollLoc = this.AutoScrollPosition;
                            newLoc = new Point(newLoc.X - autoScrollLoc.X, newLoc.Y - autoScrollLoc.Y);
                            ctlLocation.SetValue(comp, newLoc);
                        } 
                    }
                    else { 
                        c.Location = newLoc; 
                    }
                    return true; 
                }
            }

            return false; 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.RemoveComponent"]/*' /> 
        /// <devdoc>
        ///      Removes a component from the tray. 
        /// </devdoc>
        public virtual void RemoveComponent(IComponent component) {
            TrayControl c = TrayControl.FromComponent(component);
            if (c != null) { 
                try {
                    InheritanceAttribute attr = c.InheritanceAttribute; 
                    if (attr.InheritanceLevel != InheritanceLevel.NotInherited && inheritanceUI != null) { 
                        inheritanceUI.RemoveInheritedControl(c);
                    } 

                    if (controls != null) {
                        int index = controls.IndexOf(c);
                        if (index != -1) 
                            controls.RemoveAt(index);
                    } 
                } 
                finally {
                    c.Dispose(); 
                }
            }
        }
 
        private void ResetTrayControls() {
            ControlCollection children = (ControlCollection)this.Controls; 
            if (children == null) 
                return;
 
            for (int i = 0; i < children.Count; ++i) {
                TrayControl tc = children[i] as TrayControl;
                if (tc != null) {
                    tc.fRecompute = true; 
                }
            } 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.SetLocation"]/*' /> 
        /// <devdoc>
        ///     Accessor method for the location extender property.  We offer this extender
        ///     to all non-visual components.
        /// </devdoc> 
        public void SetLocation(IComponent receiver, Point location) {
            // This really should only be called when we are loading. 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            if (host != null && host.Loading) {
                // If we are loading, and we get called here, that's because we have provided 
                // the extended Location property. In this case we are loading an old project,
                // and what we are really setting is the tray location.
                SetTrayLocation(receiver, location);
            } 
            else {
                // we are not loading 
                PropertyDescriptor loc = TypeDescriptor.GetProperties(receiver.GetType())["Location"]; 
                if (loc != null) {
                    // so if the component already had the Location property, what the caller wants 
                    // is really the underlying component's Location property.
                    loc.SetValue(receiver, location);
                }
                else { 
                    // if the component didn't have a Location property, then the caller
                    // really wanted the tray location. 
                    SetTrayLocation(receiver, location); 
                }
            } 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.SetLocation"]/*' />
        /// <devdoc> 
        ///     Accessor method for the location extender property.  We offer this extender
        ///     to all non-visual components. 
        /// </devdoc> 
        public void SetTrayLocation(IComponent receiver, Point location) {
            TrayControl c = TrayControl.FromComponent(receiver); 

            if (c == null) {
                Debug.Fail("Anything we're extending should have a component view.");
                return; 
            }
 
            if (c.Parent == this) { 
                Point autoScrollLoc = this.AutoScrollPosition;
                location = new Point(location.X + autoScrollLoc.X, location.Y + autoScrollLoc.Y); 

                if (c.Visible) {
                    RearrangeInAutoSlots(c, location);
                } 
            }
            else if (!c.Location.Equals(location)) { 
                c.Location = location; 
                c.Positioned = true;
            } 
        }


        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.WndProc"]/*' /> 
        /// <devdoc>
        ///     We override our base class's WndProc to monitor certain messages. 
        /// </devdoc> 
        protected override void WndProc(ref Message m) {
            switch (m.Msg) { 
                case NativeMethods.WM_CANCELMODE:

                    // When we get cancelmode (i.e. you tabbed away to another window)
                    // then we want to cancel any pending drag operation! 
                    //
                    OnLostCapture(); 
                    break; 

                case NativeMethods.WM_SETCURSOR: 
                    OnSetCursor();
                    return;

                case NativeMethods.WM_HSCROLL: 
                case NativeMethods.WM_VSCROLL:
 
                    // When we scroll, we reposition a control without causing a 
                    // property change event.  Therefore, we must tell the
                    // selection UI service to sync itself. 
                    //
                    base.WndProc(ref m);
                    if (selectionUISvc != null) {
                        selectionUISvc.SyncSelection(); 
                    }
                    return; 
 
                case NativeMethods.WM_STYLECHANGED:
 
                    // When the scroll bars first appear, we need to
                    // invalidate so we properly paint our grid.
                    //
                    Invalidate(); 
                    break;
 
                case NativeMethods.WM_CONTEXTMENU: 

                    // Pop a context menu for the composition designer. 
                    //
                    int x = NativeMethods.Util.SignedLOWORD((int)m.LParam);
                    int y = NativeMethods.Util.SignedHIWORD((int)m.LParam);
                    if (x == -1 && y == -1) { 
                        // for shift-F10
                        Point mouse = Control.MousePosition; 
                        x = mouse.X; 
                        y = mouse.Y;
                    } 
                    OnContextMenu(x, y, true);
                    break;

                case NativeMethods.WM_NCHITTEST: 
                    if(glyphManager != null) {
                        // Get a hit test on any glyhs that we are managing 
                        // this way - we know where to route appropriate 
                        // messages
                        Point pt = new Point((short)NativeMethods.Util.LOWORD((int)m.LParam), 
                                             (short)NativeMethods.Util.HIWORD((int)m.LParam));
                        NativeMethods.POINT pt1 = new NativeMethods.POINT();
                        pt1.x = 0;
                        pt1.y = 0; 
                        NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, pt1, 1);
                        pt.Offset(pt1.x, pt1.y); 
                        glyphManager.GetHitTest(pt); 
                    }
 
                    base.WndProc(ref m);
                    break;

                default: 
                    base.WndProc(ref m);
                    break; 
            } 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.CanModifyComponents"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// Checks if the client is read only.  That is, if components can 
        /// be added or removed from the designer.
        /// </devdoc> 
        bool IOleDragClient.CanModifyComponents { 
            get {
                return true; 
            }
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.Component"]/*' /> 
        /// <internalonly/>
        IComponent IOleDragClient.Component { 
            get{ 
                return mainDesigner.Component;
            } 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.AddComponent"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// Adds a component to the tray. 
        /// </devdoc> 
        bool IOleDragClient.AddComponent(IComponent component, string name, bool firstAdd) {
 
            IOleDragClient oleDragClient = mainDesigner as IOleDragClient;
            // the designer for controls decides what to do here
            if (oleDragClient != null) {
 
                try {
                    oleDragClient.AddComponent(component, name, firstAdd); 
 
                    PositionControl(TrayControl.FromComponent(component));
                    mouseDropLocation = InvalidPoint; 

                    return true;
                }
                catch { 
                }
            } 
            else { 
                // for webforms (98109) just add the component directly to the host
                // 
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));

                try {
                    if (host != null && host.Container != null) { 
                        if (host.Container.Components[name] != null) {
                            name = null; 
                        } 
                        host.Container.Add(component, name);
                        return true; 
                    }
                }
                catch {
                } 

            } 
            Debug.Fail("Don't know how to add component!"); 
            return false;
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.GetControlForComponent"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// <para>
        /// Gets the control view instance for the given component. 
        /// For Win32 designer, this will often be the component itself. 
        /// </para>
        /// </devdoc> 
        Control IOleDragClient.GetControlForComponent(object component) {
            IComponent comp = component as IComponent;
            if (comp != null) {
                return TrayControl.FromComponent(comp); 
            }
            Debug.Fail("component is not IComponent"); 
            return null; 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.GetDesignerControl"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// <para> 
        /// Gets the control view instance for the designer that
        /// is hosting the drag. 
        /// </para> 
        /// </devdoc>
        Control IOleDragClient.GetDesignerControl() { 
            return this;
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.IOleDragClient.IsDropOk"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// Checks if it is valid to drop this type of a component on this client. 
        /// </devdoc>
        bool IOleDragClient.IsDropOk(IComponent component) { 
            return true;
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.BeginDrag"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// Begins a drag operation.  A designer should examine the list of components 
        /// to see if it wants to support the drag.  If it does, it should return
        /// true.  If it returns true, the designer should provide 
        /// UI feedback about the drag at this time.  Typically, this feedback consists
        /// of an inverted rectangle for each component, or a caret if the component
        /// is text.
        /// </devdoc> 
        bool ISelectionUIHandler.BeginDrag(object[] components, SelectionRules rules, int initialX, int initialY) {
            if (TabOrderActive) { 
                return false; 
            }
 
            bool result = DragHandler.BeginDrag(components, rules, initialX, initialY);
            if (result) {
                if (!GetOleDragHandler().DoBeginDrag(components, rules, initialX, initialY)) {
                    return false; 
                }
            } 
            return result; 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.DragMoved"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// Called when the user has moved the mouse.  This will only be called on 
        /// the designer that returned true from beginDrag.  The designer
        /// should update its UI feedback here. 
        /// </devdoc> 
        void ISelectionUIHandler.DragMoved(object[] components, Rectangle offset) {
            DragHandler.DragMoved(components, offset); 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.EndDrag"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// Called when the user has completed the drag.  The designer should 
        /// remove any UI feedback it may be providing. 
        /// </devdoc>
        void ISelectionUIHandler.EndDrag(object[] components, bool cancel) { 
            DragHandler.EndDrag(components, cancel);

            GetOleDragHandler().DoEndDrag(components, cancel);
 
            //Here, after the drag is finished and after we have resumed layout,
            //adjust the location of the components we dragged by the scroll offset 
            // 
            if (!this.autoScrollPosBeforeDragging.IsEmpty) {
                foreach (IComponent comp in components) { 
                    TrayControl tc = TrayControl.FromComponent(comp);
                    if (tc != null) {
                        this.SetTrayLocation(comp, new Point(tc.Location.X - this.autoScrollPosBeforeDragging.X, tc.Location.Y - this.autoScrollPosBeforeDragging.Y));
                    } 
                }
                this.AutoScrollPosition = new Point(-this.autoScrollPosBeforeDragging.X, -this.autoScrollPosBeforeDragging.Y); 
            } 

        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.GetComponentBounds"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// <para>
        /// Gets the shape of the component. The component's shape should be in 
        /// absolute coordinates and in pixels, where 0,0 is the upper left corner of 
        /// the screen.
        /// </para> 
        /// </devdoc>
        Rectangle ISelectionUIHandler.GetComponentBounds(object component) {
            // We render the selection UI glyph ourselves.
            return Rectangle.Empty; 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.GetComponentRules"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// <para>
        /// Gets a set of rules concerning the movement capabilities of a component.
        /// This should be one or more flags from the SelectionRules class. If no designer
        /// provides rules for a component, the component will not get any UI services. 
        /// </para>
        /// </devdoc> 
        SelectionRules ISelectionUIHandler.GetComponentRules(object component) { 
            return SelectionRules.Visible | SelectionRules.Moveable;
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.GetSelectionClipRect"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// <para>
        /// Gets the rectangle that any selection adornments should be clipped 
        /// to. This is normally the client area (in screen coordinates) of the 
        /// container.
        /// </para> 
        /// </devdoc>
        Rectangle ISelectionUIHandler.GetSelectionClipRect(object component) {
            if (IsHandleCreated) {
                return RectangleToScreen(ClientRectangle); 
            }
            return Rectangle.Empty; 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.OleDragEnter"]/*' /> 
        /// <internalonly/>
        void ISelectionUIHandler.OleDragEnter(DragEventArgs de) {
            GetOleDragHandler().DoOleDragEnter(de);
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.OleDragDrop"]/*' /> 
        /// <internalonly/> 
        void ISelectionUIHandler.OleDragDrop(DragEventArgs de) {
            GetOleDragHandler().DoOleDragDrop(de); 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.OleDragOver"]/*' />
        /// <internalonly/> 
        void ISelectionUIHandler.OleDragOver(DragEventArgs de) {
            GetOleDragHandler().DoOleDragOver(de); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.OleDragLeave"]/*' /> 
        /// <internalonly/>
        void ISelectionUIHandler.OleDragLeave() {
            GetOleDragHandler().DoOleDragLeave();
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.OnSelectionDoubleClick"]/*' /> 
        /// <internalonly/> 
        /// <devdoc>
        /// Handle a double-click on the selection rectangle 
        /// of the given component.
        /// </devdoc>
        void ISelectionUIHandler.OnSelectionDoubleClick(IComponent component) {
            if (!TabOrderActive) { 
                TrayControl tc = ((IOleDragClient)this).GetControlForComponent(component) as TrayControl;
                if (tc != null) { 
                    tc.ViewDefaultEvent(component); 
                }
            } 
        }

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.QueryBeginDrag"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// Queries to see if a drag operation 
        /// is valid on this handler for the given set of components. 
        /// If it returns true, BeginDrag will be called immediately after.
        /// </devdoc> 
        bool ISelectionUIHandler.QueryBeginDrag(object[] components, SelectionRules rules, int initialX, int initialY) {
            return DragHandler.QueryBeginDrag(components, rules, initialX, initialY);
        }
 
        internal void RearrangeInAutoSlots(Control c, Point pos) {
#if DEBUG 
            int index = controls.IndexOf(c); 
            Debug.Assert(index != -1, "Add control to the list of controls before autoarranging.!!!");
            Debug.Assert(this.Visible == c.Visible, "TrayControl for " + ((TrayControl)c).Component + " should not be positioned"); 
#endif // DEBUG

            TrayControl tc = (TrayControl)c;
            tc.Positioned = true; 
            tc.Location = pos;
        } 
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ISelectionUIHandler.ShowContextMenu"]/*' />
        /// <internalonly/> 
        /// <devdoc>
        /// Shows the context menu for the given component.
        /// </devdoc>
        void ISelectionUIHandler.ShowContextMenu(IComponent component) { 
            Point cur = Control.MousePosition;
            OnContextMenu(cur.X, cur.Y, true); 
        } 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.ComponentTrayGlyphManager"]/*' /> 
        /// <devdoc>
        ///     This class privately manages all componenttray-related
        ///     glyphs in a simlar fashion to the BehaviorService.
        /// </devdoc> 
        private class ComponentTrayGlyphManager {
 
            private Adorner traySelectionAdorner;//we'll use a single adorner to manage the glyphs 
            private Glyph hitTestedGlyph;//the last glyph we hit tested (can be null)
 
            private ISelectionService       selSvc;//we need the selection service fo r the hover behavior
            private BehaviorService         behaviorSvc;

            /// <devdoc> 
            ///     Constructor that simply creates an empty adorner.
            /// </devdoc> 
            public ComponentTrayGlyphManager(ISelectionService selSvc, BehaviorService behaviorSvc) { 

                this.selSvc = selSvc; 
                this.behaviorSvc = behaviorSvc;

                traySelectionAdorner = new Adorner();
            } 

            /// <devdoc> 
            ///    This is how we publically expose our glyph collection 
            ///    so that other designer services can 'add value'.
            /// </devdoc> 
            public GlyphCollection SelectionGlyphs {
                get {
                    return traySelectionAdorner.Glyphs;
                } 
            }
 
            /// <devdoc> 
            ///    Clears teh adorner of glyphs.
            /// </devdoc> 
            public void Dispose() {
                if (traySelectionAdorner != null) {
                    traySelectionAdorner.Glyphs.Clear();
                    traySelectionAdorner = null; 
                }
            } 
 
            /// <devdoc>
            ///    Retrieves a list of glyphs associated with the component. 
            /// </devdoc>
            public GlyphCollection GetGlyphsForComponent(IComponent comp) {
                GlyphCollection glyphs = new GlyphCollection();
                if(behaviorSvc != null && comp != null) { 
                    if(behaviorSvc.DesignerActionUI != null) {
                        Glyph g = behaviorSvc.DesignerActionUI.GetDesignerActionGlyph(comp); 
                        if(g!=null) { 
                            glyphs.Add(g);
                        } 
                    }
                }
                return glyphs;
            } 

            /// <devdoc> 
            ///    Called from the tray's NCHITTEST message in the WndProc. 
            ///    We use this to loop through our glyphs and identify which
            ///    one is successfully hit tested.  From here, we know where 
            ///    to send our messages.
            /// </devdoc>
            public Cursor GetHitTest(Point p) {
                for (int i = 0; i < traySelectionAdorner.Glyphs.Count; i++) { 
                    Cursor hitTestCursor = traySelectionAdorner.Glyphs[i].GetHitTest(p);
                    if (hitTestCursor != null) { 
                        hitTestedGlyph = traySelectionAdorner.Glyphs[i]; 
                        return hitTestCursor;
                    } 
                }

                hitTestedGlyph = null;
                return null; 
            }
 
 
            /// <devdoc>
            ///    Called when the tray receives this mouse message.  Here, 
            ///    we'll give our glyphs the first chance to repsond to the message
            //     before the tray even sees it.
            /// </devdoc>
            public bool OnMouseDoubleClick(MouseEventArgs e) { 
                if (hitTestedGlyph != null && hitTestedGlyph.Behavior != null) {
                    return hitTestedGlyph.Behavior.OnMouseDoubleClick(hitTestedGlyph, e.Button, new Point(e.X, e.Y)); 
                } 
                return false;
            } 

            /// <devdoc>
            ///    Called when the tray receives this mouse message.  Here,
            ///    we'll give our glyphs the first chance to repsond to the message 
            //     before the tray even sees it.
            /// </devdoc> 
            public bool OnMouseDown(MouseEventArgs e) { 
                if (hitTestedGlyph != null && hitTestedGlyph.Behavior != null) {
                    return hitTestedGlyph.Behavior.OnMouseDown(hitTestedGlyph, e.Button, new Point(e.X, e.Y)); 
                }
                return false;
            }
 

            /// <devdoc> 
            ///    Called when the tray receives this mouse message.  Here, 
            ///    we'll give our glyphs the first chance to repsond to the message
            //     before the tray even sees it. 
            /// </devdoc>
            public bool OnMouseMove(MouseEventArgs e) {
                if (hitTestedGlyph != null && hitTestedGlyph.Behavior != null) {
                    return hitTestedGlyph.Behavior.OnMouseMove(hitTestedGlyph, e.Button, new Point(e.X, e.Y)); 
                }
                return false; 
            } 

            /// <devdoc> 
            ///    Called when the tray receives this mouse message.  Here,
            ///    we'll give our glyphs the first chance to repsond to the message
            //     before the tray even sees it.
            /// </devdoc> 
            public bool OnMouseUp(MouseEventArgs e) {
                if (hitTestedGlyph != null && hitTestedGlyph.Behavior != null) { 
                    return hitTestedGlyph.Behavior.OnMouseUp(hitTestedGlyph, e.Button); 
                }
                return false; 
            }

            /// <devdoc>
            ///    Called when the comp tray or any tray control paints. 
            ///    This will simply enumerate through the glyphs in our
            ///    Adorner and ask them to paint 
            /// </devdoc> 
            public void OnPaintGlyphs(PaintEventArgs pe) {
                //Paint any glyphs our tray adorner has 
                foreach (Glyph g in traySelectionAdorner.Glyphs) {
                    g.Paint(pe);
                }
            } 

            /// <devdoc> 
            ///    Called when a tray control's location has changed. 
            ///    We'll loop through our glyphs and invalidate any
            ///    that are associated with the component. 
            /// </devdoc>
            public void UpdateLocation(TrayControl trayControl) {

                foreach (Glyph g in traySelectionAdorner.Glyphs) { 
                    //only look at glyphs that derive from designerglyph base (actions)
                    DesignerActionGlyph desGlyph = g as DesignerActionGlyph; 
                    if (desGlyph != null && ((DesignerActionBehavior)(desGlyph.Behavior)).RelatedComponent.Equals(trayControl.Component)) { 
                        desGlyph.UpdateAlternativeBounds(trayControl.Bounds);
                    } 
                }
            }
        }
 

        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayOleDragDropHandler"]/*' /> 
        /// <internalonly/> 
        /// <devdoc>
        ///    TrayOleDragDropHandler provides the Ole Drag-drop handler for the 
        ///    component tray.
        /// </devdoc>
        private class TrayOleDragDropHandler : OleDragDropHandler {
 
            public TrayOleDragDropHandler(SelectionUIHandler selectionHandler,  IServiceProvider  serviceProvider, IOleDragClient client) :
            base(selectionHandler, serviceProvider, client) { 
            } 

            protected override bool CanDropDataObject(IDataObject dataObj) { 
                ICollection comps = null;
                if (dataObj != null) {
                    ComponentDataObjectWrapper cdow = dataObj as ComponentDataObjectWrapper;
                    if (cdow != null) { 
                        ComponentDataObject cdo = (ComponentDataObject) cdow.InnerData;
                        comps = cdo.Components; 
                    } 
                    else {
                        try { 
                            object serializationData = dataObj.GetData(OleDragDropHandler.DataFormat, true);

                            if (serializationData == null) {
                                return false; 
                            }
 
                            IDesignerSerializationService ds = (IDesignerSerializationService)GetService(typeof(IDesignerSerializationService)); 
                            if (ds == null) {
                                return false; 
                            }
                            comps = ds.Deserialize(serializationData);
                        }
                        catch (Exception e) { 
                            if (ClientUtils.IsCriticalException(e)) {
                                throw; 
                            } 
                            // we return false on any exception
                        } 

                        catch {
                        }
                    } 
                }
 
                if (comps != null && comps.Count > 0) { 
                    foreach(object comp in comps) {
                        if (comp is Point) { 
                            continue;
                        }
                        if (comp is Control || !(comp is IComponent)) {
                            return false; 
                        }
                    } 
                    return true; 
                }
 
                return false;
            }
        }
 
        internal class AutoArrangeComparer : IComparer {
            int IComparer.Compare(object o1, object o2) { 
                Debug.Assert(o1 != null && o2 != null, "Null objects sent for comparison!!!"); 

                Point tcLoc1 = ((Control)o1).Location; 
                Point tcLoc2 = ((Control)o2).Location;
                int width = ((Control)o1).Width / 2;
                int height = ((Control)o1).Height / 2;
 
                // If they are at the same location, they are equal.
                if (tcLoc1.X == tcLoc2.X && tcLoc1.Y == tcLoc2.Y) { 
                    return 0; 
                }
 
                // Is the first control lower than the 2nd...
                if (tcLoc1.Y + height <= tcLoc2.Y)
                    return -1;
 
                // Is the 2nd control lower than the first...
                if (tcLoc2.Y + height <= tcLoc1.Y) 
                    return 1; 

                // Which control is left of the other... 
                return((tcLoc1.X <= tcLoc2.X) ? -1 : 1);
            }
        }
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        ///    The tray control is the UI we show for each component in the tray. 
        /// </devdoc>
        internal class TrayControl : Control { 

            // Values that define this tray control
            //
            private IComponent  component;       // the component this control is representing 
            private Image       toolboxBitmap;   // the bitmap used to represent the component
            private int         cxIcon;          // the dimensions of the bitmap 
            private int         cyIcon;          // the dimensions of the bitmap 

            private InheritanceAttribute inheritanceAttribute; 

            // Services that we use often enough to cache.
            //
            private ComponentTray        tray; 

            // transient values that are used during mouse drags 
            // 
            private Point mouseDragLast = InvalidPoint;  // the last position of the mouse during a drag.
            private bool  mouseDragMoved;       // has the mouse been moved during this drag? 
            private bool  ctrlSelect = false;   // was the ctrl key down on the mouse down?
            private bool  positioned = false;   // Have we given this control an explicit location yet?

            private const int whiteSpace  = 5; 
            private int borderWidth;
 
            internal bool fRecompute = false; // This flag tells the TrayControl that it needs to retrieve 
                                              // the font and the background color before painting.
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.TrayControl"]/*' />
            /// <devdoc>
            ///      Creates a new TrayControl based on the component.
            /// </devdoc> 
            public TrayControl(ComponentTray tray, IComponent component) {
                this.tray = tray; 
                this.component = component; 

                SetStyle(ControlStyles.OptimizedDoubleBuffer, true); 
                SetStyle(ControlStyles.Selectable, false);
                borderWidth = SystemInformation.BorderSize.Width;

                UpdateIconInfo(); 

                IComponentChangeService cs = (IComponentChangeService)tray.GetService(typeof(IComponentChangeService)); 
                if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(cs != null, "IComponentChangeService not found"); 
                if (cs != null) {
                    cs.ComponentRename += new ComponentRenameEventHandler(this.OnComponentRename); 
                }

                ISite site = component.Site;
                string name = null; 

                if (site != null) { 
                    name = site.Name; 

                    IDictionaryService ds = (IDictionaryService)site.GetService(typeof(IDictionaryService)); 
                    Debug.Assert(ds != null, "ComponentTray relies on IDictionaryService, which is not available.");
                    if (ds != null) {
                        ds.SetValue(GetType(), this);
                    } 
                }
 
                if (name == null) { 
                    // We always want name to have something in it, so we default to
                    // the class name.  This way the design instance contains something 
                    // semi-intuitive if we don't have a site.
                    //
                    name = component.GetType().Name;
                } 

                Text = name; 
                inheritanceAttribute = (InheritanceAttribute)TypeDescriptor.GetAttributes(component)[typeof(InheritanceAttribute)]; 
                TabStop = false;
 

            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.Component"]/*' /> 
            /// <devdoc>
            ///      Retrieves the compnent this control is representing. 
            /// </devdoc> 
            public IComponent Component {
                get { 
                    return component;
                }
            }
 
            public override Font Font {
                get { 
                    /* 
                    IDesignerHost host = (IDesignerHost)tray.GetService(typeof(IDesignerHost));
                    if (host != null && host.GetRootComponent() is Control) { 
                        Control c = (Control)host.GetRootComponent();
                        return c.Font;
                    }
                    */ 
                    return tray.Font;
                } 
            } 

            public InheritanceAttribute InheritanceAttribute { 
                get {
                    return inheritanceAttribute;
                }
            } 

            public bool Positioned { 
                get { 
                    return positioned;
                } 
                set {
                    positioned = value;
                }
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.AdjustSize"]/*' /> 
            /// <devdoc> 
            ///     Adjusts the size of the control based on the contents.
            /// </devdoc> 
            //


 
            private void AdjustSize(bool autoArrange) {
                // 
                Graphics gr = CreateGraphics(); 

                try { 
                    Size sz = Size.Ceiling(gr.MeasureString(Text, Font));

                    Rectangle rc = Bounds;
 
                    if (tray.ShowLargeIcons) {
                        rc.Width = Math.Max(cxIcon, sz.Width) + 4 * borderWidth + 2 * whiteSpace; 
                        rc.Height = cyIcon + 2 * whiteSpace + sz.Height + 4 * borderWidth; 
                    }
                    else { 
                        rc.Width = cxIcon + sz.Width + 4 * borderWidth + 2 * whiteSpace;
                        rc.Height = Math.Max(cyIcon, sz.Height) + 4 * borderWidth;
                    }
 
                    Bounds = rc;
                    Invalidate(); 
                } 

                finally { 
                    if (gr != null) {
                        gr.Dispose();
                    }
                } 

                if(tray.glyphManager != null) { 
                    tray.glyphManager.UpdateLocation(this); 
                }
            } 

            protected override AccessibleObject CreateAccessibilityInstance() {
                return new TrayControlAccessibleObject(this, tray);
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.Dispose"]/*' /> 
            /// <devdoc> 
            ///     Destroys this control.  Views automatically destroy themselves when they
            ///     are removed from the design container. 
            /// </devdoc>
            protected override void Dispose(bool disposing) {
                if (disposing) {
                    ISite site = component.Site; 
                    if (site != null) {
                        IComponentChangeService cs = (IComponentChangeService)site.GetService(typeof(IComponentChangeService)); 
                        if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(cs != null, "IComponentChangeService not found"); 
                        if (cs != null) {
                            cs.ComponentRename -= new ComponentRenameEventHandler(this.OnComponentRename); 
                        }

                        IDictionaryService ds = (IDictionaryService)site.GetService(typeof(IDictionaryService));
                        if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(ds != null, "IDictionaryService not found"); 
                        if (ds != null) {
                            ds.SetValue(typeof(TrayControl), null); 
                        } 
                    }
                } 

                base.Dispose(disposing);
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.FromComponent"]/*' />
            /// <devdoc> 
            ///      Retrieves the tray control object for the given component. 
            /// </devdoc>
            public static TrayControl FromComponent(IComponent component) { 
                TrayControl c = null;

                if (component == null) {
                    return null; 
                }
 
                ISite site = component.Site; 
                if (site != null) {
                    IDictionaryService ds = (IDictionaryService)site.GetService(typeof(IDictionaryService)); 
                    if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(ds != null, "IDictionaryService not found");
                    if (ds != null) {
                        c = (TrayControl)ds.GetValue(typeof(TrayControl));
                    } 
                }
 
                return c; 
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnComponentRename"]/*' />
            /// <devdoc>
            ///     Delegate that is called in response to a name change.  Here we update our own
            ///     stashed version of the name, recalcuate our size and repaint. 
            /// </devdoc>
            private void OnComponentRename(object sender, ComponentRenameEventArgs e) { 
                if (e.Component == this.component) { 
                    Text = e.NewName;
                    AdjustSize(true); 
                }
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnHandleCreated"]/*' /> 
            /// <devdoc>
            ///     Overrides handle creation notification for a control.  Here we just ensure 
            ///     that we're the proper size. 
            /// </devdoc>
            protected override void OnHandleCreated(EventArgs e) { 
                base.OnHandleCreated(e);
                AdjustSize(false);
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnDoubleClick"]/*' />
            /// <devdoc> 
            ///     Called in response to a double-click of the left mouse button.  The 
            ///     default behavior here calls onDoubleClick on IMouseHandler
            /// </devdoc> 
            protected override void OnDoubleClick(EventArgs e) {
                base.OnDoubleClick(e);

                if (!tray.TabOrderActive) { 
                    IDesignerHost host = (IDesignerHost)tray.GetService(typeof(IDesignerHost));
                    Debug.Assert(host != null, "Component tray does not have access to designer host."); 
                    if (host != null) { 
                        mouseDragLast = InvalidPoint;
 
                        Capture = false;

                        // We try to get a designer for the component and let it view the
                        // event.  If this fails, then we'll try to do it ourselves. 
                        //
                        IDesigner designer = host.GetDesigner(component); 
 
                        if (designer == null) {
                            ViewDefaultEvent(component); 
                        }
                        else {
                            designer.DoDefaultAction();
                        } 
                    }
                } 
            } 

            /// <devdoc> 
            ///     Terminates our drag operation.
            /// </devdoc>
            private void OnEndDrag(bool cancel) {
                mouseDragLast = InvalidPoint; 

                if (!mouseDragMoved) { 
                    if (ctrlSelect) { 
                        ISelectionService sel = (ISelectionService)tray.GetService(typeof(ISelectionService));
                        if (sel != null) { 
                            sel.SetSelectedComponents(new object[] {this.Component}, SelectionTypes.Primary);
                        }
                        ctrlSelect = false;
                    } 
                    return;
                } 
                mouseDragMoved = false; 
                ctrlSelect = false;
 
                Capture = false;
                OnSetCursor();

                // And now finish the drag. 
                //
                Debug.Assert(tray.selectionUISvc != null, "We shouldn't be able to begin a drag without this"); 
                if (tray.selectionUISvc != null && tray.selectionUISvc.Dragging) { 
                    tray.selectionUISvc.EndDrag(cancel);
                } 
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnMouseDown"]/*' />
            /// <devdoc> 
            ///     Called when the mouse button is pressed down.  Here, we provide drag
            ///     support for the component. 
            /// </devdoc> 
            protected override void OnMouseDown(MouseEventArgs me) {
                base.OnMouseDown(me); 

                if (!tray.TabOrderActive) {

                    tray.FocusDesigner(); 

                    // If this is the left mouse button, then begin a drag. 
                    // 
                    if (me.Button == MouseButtons.Left) {
                        Capture = true; 
                        mouseDragLast = PointToScreen(new Point(me.X, me.Y));

                        // If the CTRL key isn't down, select this component,
                        // otherwise, we wait until the mouse up 
                        //
                        // Make sure the component is selected 
                        // 

                        ctrlSelect = NativeMethods.GetKeyState((int)Keys.ControlKey) != 0; 

                        if (!ctrlSelect) {
                            ISelectionService sel = (ISelectionService)tray.GetService(typeof(ISelectionService));
 
                            // Make sure the component is selected
                            // 
                            if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(sel != null, "ISelectionService not found"); 

                            if (sel != null) { 
                                sel.SetSelectedComponents(new object[] {this.Component}, SelectionTypes.Primary);
                            }
                        }
                    } 
                }
            } 
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnMouseMove"]/*' />
            /// <devdoc> 
            ///     Called when the mouse is moved over the component.  We update our drag
            ///     information here if we're dragging the component around.
            /// </devdoc>
            protected override void OnMouseMove(MouseEventArgs me) { 
                base.OnMouseMove(me);
 
                if (mouseDragLast == InvalidPoint) { 
                    return;
                } 

                if (!mouseDragMoved) {

                    Size minDrag = SystemInformation.DragSize; 
                    Size minDblClick = SystemInformation.DoubleClickSize;
 
                    minDrag.Width = Math.Max(minDrag.Width, minDblClick.Width); 
                    minDrag.Height = Math.Max(minDrag.Height, minDblClick.Height);
 
                    // we have to make sure the mouse moved farther than
                    // the minimum drag distance before we actually start
                    // the drag
                    // 
                    Point newPt = PointToScreen(new Point(me.X, me.Y));
                    if (mouseDragLast == InvalidPoint || 
                        (Math.Abs(mouseDragLast.X - newPt.X) < minDrag.Width && 
                         Math.Abs(mouseDragLast.Y - newPt.Y) < minDrag.Height)) {
                        return; 
                    }
                    else {
                        mouseDragMoved = true;
 
                        // we're on the move, so we're not in a ctrlSelect
                        // 
                        ctrlSelect = false; 
                    }
                } 

                try {
                    // Make sure the component is selected
                    // 
                    ISelectionService sel = (ISelectionService)tray.GetService(typeof(ISelectionService));
                    if (sel != null) { 
                        sel.SetSelectedComponents(new object[] {this.Component}, SelectionTypes.Primary); 
                    }
 
                    // Notify the selection service that all the components are in the "mouse down" mode.
                    //
                    if (tray.selectionUISvc != null && tray.selectionUISvc.BeginDrag(SelectionRules.Visible | SelectionRules.Moveable, mouseDragLast.X, mouseDragLast.Y)) {
                        OnSetCursor(); 
                    }
                } 
                finally { 
                    mouseDragMoved = false;
                    mouseDragLast = InvalidPoint; 
                }
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnMouseUp"]/*' /> 
            /// <devdoc>
            ///     Called when the mouse button is released.  Here, we finish our drag 
            ///     if one was started. 
            /// </devdoc>
            protected override void OnMouseUp(MouseEventArgs me) { 
                base.OnMouseUp(me);
                OnEndDrag(false);
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnContextMenu"]/*' />
            /// <devdoc> 
            ///     Called when we are to display our context menu for this component. 
            /// </devdoc>
            private void OnContextMenu(int x, int y) { 

                if (!tray.TabOrderActive) {
                    Capture = false;
 
                    // Ensure that this component is selected.
                    // 
                    ISelectionService s = (ISelectionService)tray.GetService(typeof(ISelectionService)); 
                    if (s != null && !s.GetComponentSelected(component)) {
                        s.SetSelectedComponents(new object[] {component}, SelectionTypes.Replace); 
                    }

                    IMenuCommandService mcs = tray.MenuService;
                    if (mcs != null) { 
                        Capture = false;
                        Cursor.Clip = Rectangle.Empty; 
                        mcs.ShowContextMenu(MenuCommands.TraySelectionMenu, x, y); 
                    }
                } 
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnPaint"]/*' />
            /// <devdoc> 
            ///     Painting for our control.
            /// </devdoc> 
            protected override void OnPaint(PaintEventArgs e) { 
                if (fRecompute) {
                    fRecompute = false; 
                    UpdateIconInfo();
                }

                base.OnPaint(e); 
                Rectangle rc = ClientRectangle;
 
                rc.X += whiteSpace + borderWidth; 
                rc.Y += borderWidth;
                rc.Width -= (2 * borderWidth + whiteSpace); 
                rc.Height -= 2 * borderWidth;

                StringFormat format = new StringFormat();
                Brush foreBrush = new SolidBrush(ForeColor); 

                try { 
                    format.Alignment = StringAlignment.Center; 
                    if (tray.ShowLargeIcons) {
                        if (null != toolboxBitmap) { 
                            int x = rc.X + (rc.Width - cxIcon)/2;
                            int y = rc.Y + whiteSpace;
                            e.Graphics.DrawImage(toolboxBitmap, new Rectangle(x, y, cxIcon, cyIcon));
                        } 

                        rc.Y += (cyIcon + whiteSpace); 
                        rc.Height -= cyIcon; 
                        e.Graphics.DrawString(Text, Font, foreBrush, rc, format);
                    } 
                    else {
                        if (null != toolboxBitmap) {
                            int y = rc.Y + (rc.Height - cyIcon)/2;
                            e.Graphics.DrawImage(toolboxBitmap, new Rectangle(rc.X, y, cxIcon, cyIcon)); 
                        }
 
                        rc.X += (cxIcon + borderWidth); 
                        rc.Width -= cxIcon;
                        rc.Y += 3; 
                        e.Graphics.DrawString(Text, Font, foreBrush, rc);
                    }
                }
 
                finally {
                    if (format != null) { 
                        format.Dispose(); 
                    }
                    if (foreBrush != null) { 
                        foreBrush.Dispose();
                    }
                }
 
                // If this component is being inherited, paint it as such
                // 
                if (!InheritanceAttribute.NotInherited.Equals(inheritanceAttribute)) { 
                    InheritanceUI iui = tray.InheritanceUI;
                    if (iui != null) { 
                        e.Graphics.DrawImage(iui.InheritanceGlyph, 0, 0);
                    }
                }
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnFontChanged"]/*' /> 
            /// <devdoc> 
            ///     Overrides control's FontChanged.  Here we re-adjust our size if the font changes.
            /// </devdoc> 
            protected override void OnFontChanged(EventArgs e) {
                AdjustSize(true);
                base.OnFontChanged(e);
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnLocationChanged"]/*' /> 
            /// <devdoc> 
            ///     Overrides control's LocationChanged.  Here, we make sure that any glyphs associated
            ///     with us are also relocated. 
            /// </devdoc>
            protected override void OnLocationChanged(EventArgs e) {
                if(tray.glyphManager != null) {
                    tray.glyphManager.UpdateLocation(this); 
                }
            } 
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnTextChanged"]/*' />
            /// <devdoc> 
            ///     Overrides control's TextChanged.  Here we re-adjust our size if the font changes.
            /// </devdoc>
            protected override void OnTextChanged(EventArgs e) {
                AdjustSize(true); 
                base.OnTextChanged(e);
            } 
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.OnSetCursor"]/*' />
            /// <devdoc> 
            ///     Called each time the cursor needs to be set.  The ControlDesigner behavior here
            ///     will set the cursor to one of three things:
            ///     1.  If the selection UI service shows a locked selection, or if there is no location
            ///     property on the control, then the default arrow will be set. 
            ///     2.  Otherwise, the four headed arrow will be set to indicate that the component can
            ///     be clicked and moved. 
            ///     3.  If the user is currently dragging a component, the crosshair cursor will be used 
            ///     instead of the four headed arrow.
            /// </devdoc> 
            private void OnSetCursor() {

                // Check that the component is not locked.
                // 
                PropertyDescriptor prop = TypeDescriptor.GetProperties(component)["Locked"];
                if (prop != null  && ((bool)prop.GetValue(component)) == true) { 
                    Cursor.Current = Cursors.Default; 
                    return;
                } 

                // Ask the tray to see if the tab order UI is not running.
                //
                if (tray.TabOrderActive) { 
                    Cursor.Current = Cursors.Default;
                    return; 
                } 

                if (mouseDragMoved) { 
                    Cursor.Current = Cursors.Default;
                }
                else if (mouseDragLast != InvalidPoint) {
                    Cursor.Current = Cursors.Cross; 
                }
                else { 
                    Cursor.Current = Cursors.SizeAll; 
                }
            } 

            protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified) {
                if (!tray.AutoArrange ||
                    (specified & BoundsSpecified.Width) == BoundsSpecified.Width || 
                    (specified & BoundsSpecified.Height) == BoundsSpecified.Height) {
 
                    base.SetBoundsCore(x, y, width, height, specified); 
                }
 
                Rectangle bounds = Bounds;
                Size parentGridSize = tray.ParentGridSize;
                if (Math.Abs(bounds.X - x) > parentGridSize.Width || Math.Abs(bounds.Y - y) > parentGridSize.Height) {
                    base.SetBoundsCore(x, y, width, height, specified); 
                }
 
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.SetVisibleCore"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            protected override void SetVisibleCore(bool value) { 
                if (value && !tray.CanDisplayComponent(this.component))
                    return; 
 
                base.SetVisibleCore(value);
            } 

            public override string ToString() {
                return "ComponentTray: " + component.ToString();
            } 

            internal void UpdateIconInfo() { 
                ToolboxBitmapAttribute attr = (ToolboxBitmapAttribute)TypeDescriptor.GetAttributes(component)[typeof(ToolboxBitmapAttribute)]; 
                if (attr != null) {
                    toolboxBitmap = attr.GetImage(component, tray.ShowLargeIcons); 
                }

                // Get the size of the bitmap so we can size our
                // component correctly. 
                //
                if (null == toolboxBitmap) { 
                    cxIcon = 0; 
                    cyIcon = SystemInformation.IconSize.Height;
                } 
                else {
                    Size sz = toolboxBitmap.Size;
                    cxIcon = sz.Width;
                    cyIcon = sz.Height; 
                }
 
                AdjustSize(true); 
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.ViewDefaultEvent"]/*' />
            /// <devdoc>
            ///      This creates a method signature in the source code file for the
            ///      default event on the component and navigates the user's cursor 
            ///      to that location.
            /// </devdoc> 
            public virtual void ViewDefaultEvent(IComponent component) { 
                EventDescriptor defaultEvent = TypeDescriptor.GetDefaultEvent(component);
                PropertyDescriptor defaultPropEvent = null; 
                string handler = null;
                bool eventChanged = false;

                IEventBindingService eps = (IEventBindingService)GetService(typeof(IEventBindingService)); 
                if (CompModSwitches.CommonDesignerServices.Enabled) Debug.Assert(eps != null, "IEventBindingService not found");
                if (eps != null) { 
                    defaultPropEvent = eps.GetEventProperty(defaultEvent); 
                }
 
                // If we couldn't find a property for this event, or if the property is read only, then
                // abort and just show the code.
                //
                if (defaultPropEvent == null || defaultPropEvent.IsReadOnly) { 
                    if (eps != null) {
                        eps.ShowCode(); 
                    } 
                    return;
                } 

                handler = (string)defaultPropEvent.GetValue(component);

                // If there is no handler set, set one now. 
                //
                if (handler == null) { 
                    eventChanged = true; 
                    handler = eps.CreateUniqueMethodName(component, defaultEvent);
                } 

                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                DesignerTransaction trans = null;
 
                try {
                    if (host != null) { 
                        trans = host.CreateTransaction(SR.GetString(SR.WindowsFormsAddEvent, defaultEvent.Name)); 
                    }
 
                    // Save the new value... BEFORE navigating to it!
                    //
                    if (eventChanged && defaultPropEvent != null) {
 
                        defaultPropEvent.SetValue(component, handler);
 
                        // make sure set succeded (may fail if under SCC) 
                        // if (defaultPropEvent.GetValue(component) != handler) {
                        //     return; 
                        // }
                    }

                    eps.ShowCode(component, defaultEvent); 
                }
                finally { 
                    if (trans != null) { 
                        trans.Commit();
                    } 
                }
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TrayControl.WndProc"]/*' /> 
            /// <devdoc>
            ///     This method should be called by the extending designer for each message 
            ///     the control would normally receive.  This allows the designer to pre-process 
            ///     messages before allowing them to be routed to the control.
            /// </devdoc> 
            protected override void WndProc(ref Message m) {
                switch (m.Msg) {
                    case NativeMethods.WM_SETCURSOR:
                        // We always handle setting the cursor ourselves. 
                        //
                        OnSetCursor(); 
                        break; 

                    case NativeMethods.WM_CONTEXTMENU: 
                        // We must handle this ourselves.  Control only allows
                        // regular Windows Forms context menus, which doesn't do us much
                        // good.  Also, control's button up processing calls DefwndProc
                        // first, which causes a right mouse up to be routed as a 
                        // WM_CONTEXTMENU.  If we don't respond to it here, this
                        // message will be bubbled up to our parent, which would 
                        // pop up a container context menu instead of our own. 
                        //
                        int x = NativeMethods.Util.SignedLOWORD((int)m.LParam); 
                        int y = NativeMethods.Util.SignedHIWORD((int)m.LParam);
                        if (x == -1 && y == -1) {
                            // for shift-F10
                            Point mouse = Control.MousePosition; 
                            x = mouse.X;
                            y = mouse.Y; 
                        } 
                        OnContextMenu(x, y);
                        break; 
                    case NativeMethods.WM_NCHITTEST:
                        if(tray.glyphManager != null) {
                            //Make sure tha we send our glyphs hit test messages
                            //over the TrayControls too 
                            Point pt = new Point((short)NativeMethods.Util.LOWORD((int)m.LParam),
                                                 (short)NativeMethods.Util.HIWORD((int)m.LParam)); 
                            NativeMethods.POINT pt1 = new NativeMethods.POINT(); 
                            pt1.x = 0;
                            pt1.y = 0; 
                            NativeMethods.MapWindowPoints(IntPtr.Zero, Handle, pt1, 1);
                            pt.Offset(pt1.x, pt1.y);
                            pt.Offset(this.Location.X, this.Location.Y);//offset the loc of the traycontrol -so now we're in comptray coords
                            tray.glyphManager.GetHitTest(pt); 
                        }
 
                        base.WndProc(ref m); 
                        break;
 
                    default:
                        base.WndProc(ref m);
                        break;
                } 
            }
 
            private class TrayControlAccessibleObject : ControlAccessibleObject 
            {
                ComponentTray tray; 

                public TrayControlAccessibleObject(TrayControl owner, ComponentTray tray) : base(owner) {
                    this.tray = tray;
                } 

                private IComponent Component { 
                    get 
                    {
                        return ((TrayControl)Owner).Component; 
                    }
                }

                public override AccessibleStates State { 
                    get
                    { 
                        AccessibleStates state = base.State; 

                        ISelectionService s = (ISelectionService)tray.GetService(typeof(ISelectionService)); 
                        if (s != null) {
                            if (s.GetComponentSelected(Component)) {
                                state |= AccessibleStates.Selected;
                            } 
                            if (s.PrimarySelection == Component) {
                                state |= AccessibleStates.Focused; 
                            } 
                        }
 
                        return state;
                    }
                }
            } 
        }
 
        /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler"]/*' /> 
        /// <devdoc>
        ///      This class inherits from the abstract SelectionUIHandler 
        ///      class to provide a selection UI implementation for the
        ///      component tray.
        /// </devdoc>
        private class TraySelectionUIHandler : SelectionUIHandler { 

            private ComponentTray tray; 
            private Size snapSize = Size.Empty; 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.TraySelectionUIHandler"]/*' /> 
            /// <devdoc>
            ///      Creates a new selection UI handler for the given
            ///      component tray.
            /// </devdoc> 
            public TraySelectionUIHandler(ComponentTray tray) {
                this.tray = tray; 
                snapSize = new Size(); 
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.BeginDrag"]/*' />
            /// <devdoc>
            ///     Called when the user has started the drag.
            /// </devdoc> 
            public override bool BeginDrag(object[] components, SelectionRules rules, int initialX, int initialY) {
                bool value = base.BeginDrag(components, rules, initialX, initialY); 
                tray.SuspendLayout(); 
                return value;
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.EndDrag"]/*' />
            /// <devdoc>
            ///     Called when the user has completed the drag.  The designer should 
            ///     remove any UI feedback it may be providing.
            /// </devdoc> 
            public override void EndDrag(object[] components, bool cancel) { 
                base.EndDrag(components, cancel);
                tray.ResumeLayout(); 
            }

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetComponent"]/*' />
            /// <devdoc> 
            ///      Retrieves the base component for the selection handler.
            /// </devdoc> 
            protected override IComponent GetComponent() { 
                return tray;
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetControl"]/*' />
            /// <devdoc>
            ///      Retrieves the base component's UI control for the selection handler. 
            /// </devdoc>
            protected override Control GetControl() { 
                return tray; 
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetControl1"]/*' />
            /// <devdoc>
            ///      Retrieves the UI control for the given component.
            /// </devdoc> 
            protected override Control GetControl(IComponent component) {
                return TrayControl.FromComponent(component); 
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetCurrentSnapSize"]/*' /> 
            /// <devdoc>
            ///      Retrieves the current grid snap size we should snap objects
            ///      to.
            /// </devdoc> 
            protected override Size GetCurrentSnapSize() {
                return snapSize; 
            } 

            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetService"]/*' /> 
            /// <devdoc>
            ///      We use this to request often-used services.
            /// </devdoc>
            protected override object GetService(Type serviceType) { 
                return tray.GetService(serviceType);
            } 
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetShouldSnapToGrid"]/*' />
            /// <devdoc> 
            ///      Determines if the selection UI handler should attempt to snap
            ///      objects to a grid.
            /// </devdoc>
            protected override bool GetShouldSnapToGrid() { 
                return false;
            } 
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.GetUpdatedRect"]/*' />
            /// <devdoc> 
            ///      Given a rectangle, this updates the dimensions of it
            ///      with any grid snaps and returns a new rectangle.  If
            ///      no changes to the rectangle's size were needed, this
            ///      may return the same rectangle. 
            /// </devdoc>
            public override Rectangle GetUpdatedRect(Rectangle originalRect, Rectangle dragRect, bool updateSize) { 
                return dragRect; 
            }
 
            /// <include file='doc\ComponentTray.uex' path='docs/doc[@for="ComponentTray.TraySelectionUIHandler.SetCursor"]/*' />
            /// <devdoc>
            ///     Asks the handler to set the appropriate cursor
            /// </devdoc> 
            public override void SetCursor() {
                tray.OnSetCursor(); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
