//------------------------------------------------------------------------------ 
// <copyright file="ControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using Accessibility;
    using System.Runtime.Serialization.Formatters;
    using System.Threading;
    using System.Runtime.InteropServices; 
    using System.CodeDom;
    using System.ComponentModel; 
    using System.Diagnostics; 
    using System;
    using System.Security; 
    using System.Security.Permissions;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Serialization;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design.Behavior; 
    using System.Drawing;
    using System.Drawing.Design; 
    using Microsoft.Win32;
    using System.Configuration;
    using Timer = System.Windows.Forms.Timer;
    using System.Globalization; 
    using System.Diagnostics.CodeAnalysis;
 
    /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides a designer that can design components
    ///       that extend Control.</para>
    /// </devdoc>
    public class ControlDesigner : ComponentDesigner { 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InvalidPoint"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 


        protected static readonly Point InvalidPoint = new Point(int.MinValue, int.MinValue);
        private static int currentProcessId; 

        private   IDesignerHost         host;           // the host for our designer 
        private   IDesignerTarget       designerTarget; // the target window proc for the control. 

        private   bool                  liveRegion;     // is the mouse is over a live region of the control? 
        private   bool                  inHitTest;      // A popular way to implement GetHitTest is by WM_NCHITTEST...which would cause a cycle.
        private   bool                  hasLocation;    // Do we have a location property?
        private   bool                  locationChecked;// And did we check it
        private   bool                  locked;         // signifies if this control is locked or not 
        private   bool                  initializing;
        private   bool                  enabledchangerecursionguard; 
 

        //Behavior work 
        private BehaviorService     behaviorService; //we cache this 'cause we use it so often
        private ResizeBehavior      resizeBehavior; //the standard behavior for our selection glyphs - demand created
        private ContainerSelectorBehavior moveBehavior; //the behavior for non-resize glyphs - demand created
 
        // Services that we use enough to cache
        // 
        private ISelectionUIService     selectionUISvc; 
        private IEventHandlerService    eventSvc;
        private IToolboxService         toolboxSvc; 
        private InheritanceUI           inheritanceUI;
        private IOverlayService         overlayService;

 

        // transient values that are used during mouse drags 
        // 
        private Point               mouseDragLast = InvalidPoint;   // the last position of the mouse during a drag.
        private bool                mouseDragMoved;                 // has the mouse been moved during this drag? 
        private int                 lastMoveScreenX;
        private int                 lastMoveScreenY;

        // Values used to simulate double clicks for controls that don't support them. 
        //
        private int lastClickMessageTime; 
        private int lastClickMessagePositionX; 
        private int lastClickMessagePositionY;
 
        private Point                         downPos = Point.Empty;     // point used to track first down of a double click
        private event EventHandler            disposingHandler;
        private CollectionChangeEventHandler  dataBindingsCollectionChanged;
        private Exception                     thrownException; 
        private bool                          ctrlSelect;                // if the CTRL key was down at the mouse down
        private bool                          toolPassThrough;           // a tool is selected, allow the parent to draw a rect for it. 
        private bool                          removalNotificationHooked = false; 
        private bool                          revokeDragDrop = true;
        private bool                          hadDragDrop; 

        private DesignerControlCollection     controls;

        private static bool                   inContextMenu = false; 

        private DockingActionList             dockingAction; 
        private StatusCommandUI               statusCommandUI;   // UI for setting the StatusBar Information.. 

        private bool                          forceVisible = true; 

        private bool                          autoResizeHandles = false; // used for disabling AutoSize effect on resize modes. Needed for compat.

        private Dictionary<IntPtr, bool>      subclassedChildren; 

 
        /// <devdoc> 
        ///     Accessor for AllowDrop.  Since we often turn this on, we shadow it
        ///     so it doesn't show up to the user. 
        /// </devdoc>
        private bool AllowDrop {
            get {
                return (bool)ShadowProperties["AllowDrop"]; 
            }
            set { 
                ShadowProperties["AllowDrop"] = value; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.BehaviorService"]/*' />
        protected BehaviorService BehaviorService {
            get { 
                if (behaviorService == null) {
                    behaviorService = (BehaviorService)GetService(typeof(BehaviorService)); 
                } 
                return behaviorService;
            } 
        }

        internal bool ForceVisible {
            get { 
                return forceVisible;
            } 
 
            set {
                forceVisible = value; 
            }
        }

        private Dictionary<IntPtr, bool> SubclassedChildWindows { 
            get {
                if (subclassedChildren == null) { 
                    subclassedChildren = new Dictionary<IntPtr, bool>(); 
                }
 
                return subclassedChildren;
            }
        }
 
        private IOverlayService OverlayService {
            get { 
                if (overlayService == null) { 
                     overlayService = (IOverlayService)GetService(typeof(IOverlayService));
                } 
                return overlayService;
            }
        }
 
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        private DesignerControlCollection Controls { 
            get { 
                if (controls == null) {
                    controls = new DesignerControlCollection(Control); 
                }
                return controls;
            }
        } 

        private Point Location { 
            get { 
                Point loc = Control.Location;
 
                ScrollableControl p = Control.Parent as ScrollableControl;
                if (p != null) {
                    Point pt = p.AutoScrollPosition;
                    loc.Offset(-pt.X, -pt.Y); 
                }
                return loc; 
            } 
            set {
                ScrollableControl p = Control.Parent as ScrollableControl; 
                if (p != null) {
                    Point pt = p.AutoScrollPosition;
                    value.Offset(pt.X, pt.Y);
                } 
                Control.Location = value;
            } 
        } 

 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.AssociatedComponents"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Retrieves a list of associated components.  These are components that should be incluced in a cut or copy operation on this component.
        ///    </para> 
        /// </devdoc> 
        public override ICollection AssociatedComponents {
            get { 
                ArrayList sitedChildren = null;

                foreach (Control c in Control.Controls) {
                    if (c.Site != null) { 
                        if (sitedChildren == null) {
                            sitedChildren = new ArrayList(); 
                        } 
                        sitedChildren.Add(c);
                    } 
                }

                if (sitedChildren != null) {
                    return sitedChildren; 
                }
                return base.AssociatedComponents; 
            } 
        }
 
        /// <devdoc>
        ///     Accessor method for the context menu property on control.  We shadow
        ///     this property at design time.
        /// </devdoc> 
        private ContextMenu ContextMenu {
            get { 
                return (ContextMenu)ShadowProperties["ContextMenu"]; 
            }
            set { 
                ContextMenu oldValue = (ContextMenu)ShadowProperties["ContextMenu"];

                if (oldValue != value) {
                    EventHandler disposedHandler = new EventHandler(DetachContextMenu); 

                    if (oldValue != null) { 
                        oldValue.Disposed -= disposedHandler; 
                    }
 
                    ShadowProperties["ContextMenu"] = value;

                    if (value != null) {
                        value.Disposed += disposedHandler; 
                    }
                } 
 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.accessibilityObj"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        protected AccessibleObject accessibilityObj = null; 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.AccessibilityObject"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual AccessibleObject AccessibilityObject {
            get { 
                if (accessibilityObj == null) {
                    accessibilityObj = new ControlDesignerAccessibleObject(this, Control); 
                } 
                return accessibilityObj;
            } 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Control"]/*' />
        /// <devdoc> 
        ///     Retrieves the control we're designing.
        /// </devdoc> 
        public virtual Control Control { 
            get {
                return(Control)Component; 
            }
        }

        private IDesignerTarget DesignerTarget { 
            get {
                return designerTarget; 
            } 
            set {
                this.designerTarget = value; 
            }
        }

        /// <devdoc> 
        ///     Accessor method for the enabled property on control.  We shadow
        ///     this property at design time. 
        /// </devdoc> 
        private bool Enabled {
            get { 
                return (bool)ShadowProperties["Enabled"];
            }
            set {
                ShadowProperties["Enabled"] = value; 
            }
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.EnableDragRect"]/*' />
        /// <devdoc> 
        ///     Determines whether drag rects can be drawn on this designer.
        /// </devdoc>
        protected virtual bool EnableDragRect {
            get { 
                return false;
            } 
        } 

        /// <devdoc> 
        ///     Gets / Sets this controls locked property
        ///
        /// </devdoc>
        private bool Locked { 
            get {
                return locked; 
            } 

            set{ 
                if (locked != value) {
                    locked = value;
                }
            } 
        }
 
        private string Name { 
            get {
                return Component.Site.Name; 
            }
            set {
                // don't do anything here during loading, if a refactor changed it we don't want to do anything
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
                if(host == null || (host != null && !host.Loading)) {
                    Component.Site.Name = value; 
                } 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ParentComponent"]/*' />
        /// <devdoc>
        ///     Returns the parent component for this control designer. 
        ///     The default implementation just checks to see if the
        ///     component being designed is a control, and if it is it 
        ///     returns its parent.  This property can return null if there 
        ///     is no parent component.
        /// </devdoc> 
        protected override IComponent ParentComponent {
            get {
                Control c = Component as Control;
                if (c != null && c.Parent != null) { 
                    return c.Parent;
                } 
                return base.ParentComponent; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ParticipatesWidthSnapLines"]/*' />
        /// <devdoc>
        ///     Determines whether or not the ControlDesigner will allow SnapLine alignment during a 
        ///     drag operation when the primary drag control is over this designer, or when a control
        ///     is being dragged from the toolbox, or when a control is being drawn through click-drag. 
        /// 
        /// </devdoc>
        public virtual bool ParticipatesWithSnapLines { 
            get {
                return true;
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.NumberOfInternalControlDesigners"]/*' /> 
        /// <devdoc> 
        ///     Returns the number of internal control designers in the ControlDesigner. An internal control
        ///     is a control that is not in the IDesignerHost.Container.Components collection. 
        ///     SplitterPanel is an example of one such control. We use this to get SnapLines for the internal
        ///     control designers.
        /// </devdoc>
        public virtual int NumberOfInternalControlDesigners() { 
            return 0;
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InternalControlDesigner"]/*' />
        /// <devdoc> 
        ///     Returns the internal control designer with the specified index in the ControlDesigner. An internal control
        ///     is a control that is not in the IDesignerHost.Container.Components collection.
        ///     SplitterPanel is an example of one such control.
        /// 
        ///     internalControlIndex is zero-based.
        /// </devdoc> 
        public virtual ControlDesigner InternalControlDesigner(int internalControlIndex) { 
            return null;
        } 

        /// <devdoc>
        ///  Per AutoSize spec, determines if a control is resizable.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        private bool IsResizableConsiderAutoSize(PropertyDescriptor autoSizeProp, PropertyDescriptor autoSizeModeProp) { 
            object component = Component; 

            bool resizable = true; 
            bool autoSize = false;
            bool growOnly = false;

            if (autoSizeProp != null && 
                !(autoSizeProp.Attributes.Contains(DesignerSerializationVisibilityAttribute.Hidden) ||
                  autoSizeProp.Attributes.Contains(BrowsableAttribute.No))) { 
                autoSize = (bool) autoSizeProp.GetValue(component); 
            }
 
            if (autoSizeModeProp != null) {
                AutoSizeMode mode = (AutoSizeMode) autoSizeModeProp.GetValue(component);
                growOnly = mode == AutoSizeMode.GrowOnly;
            } 

            if (autoSize) { 
                resizable = growOnly; 
            }
 
            return resizable;

        }
 
        /// <devdoc>
        /// 
        /// 
        /// </devdoc>
        public bool AutoResizeHandles { 
            get {
                return autoResizeHandles;
            }
            set { 
                autoResizeHandles = value;
            } 
        } 

 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.SelectionRules"]/*' />
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public virtual SelectionRules SelectionRules { 
            get {
                SelectionRules rules = SelectionRules.Visible; 
                object component = Component;

                rules = SelectionRules.Visible;
 
                PropertyDescriptor prop;
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(component); 
 
                PropertyDescriptor autoSizeProp = props["AutoSize"];
                PropertyDescriptor autoSizeModeProp = props["AutoSizeMode"]; 

                if ((prop = props["Location"]) != null &&
                    !prop.IsReadOnly) {
                    rules |= SelectionRules.Moveable; 
                }
 
                if ((prop = props["Size"]) != null && !prop.IsReadOnly) { 

                    if (AutoResizeHandles && this.Component != host.RootComponent) { 
                        rules = IsResizableConsiderAutoSize(autoSizeProp, autoSizeModeProp) ? rules | SelectionRules.AllSizeable : rules;
                    }
                    else {
                        rules |= SelectionRules.AllSizeable; 
                    }
                } 
 
                PropertyDescriptor propDock = props["Dock"];
                if (propDock != null) { 
                    DockStyle dock = (DockStyle)(int)propDock.GetValue(component);
                    //gotta adjust if the control's parent is mirrored...
                    //this is just such that we add the right resize handles.
                    //We need to do it this way, since resize glyphs are added in 
                    //AdornerWindow coords, and the AdornerWindow is never mirrored.
                    if (Control.Parent != null && Control.Parent.IsMirrored) { 
                        if (dock == DockStyle.Left) { 
                            dock = DockStyle.Right;
                        } 
                        else if (dock == DockStyle.Right) {
                            dock = DockStyle.Left;
                        }
                    } 
                    switch (dock) {
                        case DockStyle.Top: 
                            rules &= ~(SelectionRules.Moveable | SelectionRules.TopSizeable | SelectionRules.LeftSizeable | SelectionRules.RightSizeable); 
                            break;
                        case DockStyle.Left: 
                            rules &= ~(SelectionRules.Moveable | SelectionRules.TopSizeable | SelectionRules.LeftSizeable | SelectionRules.BottomSizeable);
                            break;
                        case DockStyle.Right:
                            rules &= ~(SelectionRules.Moveable | SelectionRules.TopSizeable | SelectionRules.BottomSizeable | SelectionRules.RightSizeable); 
                            break;
                        case DockStyle.Bottom: 
                            rules &= ~(SelectionRules.Moveable | SelectionRules.LeftSizeable | SelectionRules.BottomSizeable | SelectionRules.RightSizeable); 
                            break;
                        case DockStyle.Fill: 
                            rules &= ~(SelectionRules.Moveable | SelectionRules.TopSizeable | SelectionRules.LeftSizeable | SelectionRules.RightSizeable | SelectionRules.BottomSizeable);
                            break;
                    }
                } 

                PropertyDescriptor pd = props["Locked"]; 
                if (pd != null) { 
                    Object value = pd.GetValue(component);
 
                    // make sure that value is a boolean, in case someone else added this property
                    //
                    if (value is bool && (bool)value == true) {
                        rules = SelectionRules.Locked | SelectionRules.Visible; 
                    }
                } 
 
                return rules;
            } 
        }

        // This boolean indicates whether the Control will allow SnapLines to be shown when any other targetControl is dragged on the design surface.
        // This is true by default. 
        internal virtual bool ControlSupportsSnaplines
        { 
            get 
            {
                return true; 
            }
        }

 

        /// <devdoc> 
        /// 
        /// Used when adding snaplines
        /// 
        /// In order to add padding, we need to get the offset from the usable client area of our control
        /// and the actual origin of our control.  In other words: how big is the non-client area here?
        /// Ex: we want to add padding on a form to the insides of the borders and below the titlebar.
        /// 
        internal Point GetOffsetToClientArea() {
            NativeMethods.POINT nativeOffset = new NativeMethods.POINT(0,0); 
            NativeMethods.MapWindowPoints(Control.Handle, Control.Parent.Handle, nativeOffset, 1); 

            Point offset = Control.Location; 
            // If the 2 controls do not have the same orientation, then force one
            // to make sure we calculate the correct offset
            if (Control.IsMirrored != Control.Parent.IsMirrored) {
                offset.Offset(Control.Width,0); 
            }
            return (new Point(Math.Abs(nativeOffset.x - offset.X), nativeOffset.y - offset.Y)); 
        } 

 
        internal IList SnapLinesInternal() {
            return SnapLinesInternal(Control.Margin);
        }
 
        /// <devdoc>
        ///     We separate this from the SnapLines property so that other designers 
        ///     can call this directly. E.g. SplitContainerDesigner would inherit 
        ///     SnapLines from ParentControlDesigner which overrides SnapLines.
        ///     But ParentControlDesigner.SnapLines also adds padding SnapLines, 
        ///     which we don't want for the SplitContainerDesigner.
        /// </devdoc>
        internal IList SnapLinesInternal(Padding margin) {
            ArrayList snapLines = new ArrayList(4); 
            int width = Control.Width; // better perf
            int height = Control.Height; // better perf 
 

 
            //the four edges of our control
            snapLines.Add( new SnapLine(SnapLineType.Top, 0, SnapLinePriority.Low));
            snapLines.Add( new SnapLine(SnapLineType.Bottom, height-1, SnapLinePriority.Low));
            snapLines.Add( new SnapLine(SnapLineType.Left, 0, SnapLinePriority.Low)); 
            snapLines.Add( new SnapLine(SnapLineType.Right, width-1, SnapLinePriority.Low));
 
            //the four margins of our control 

 
            // Even if a control does not have margins, we still want to add Margin snaplines.
            // This is because we only try to match to matching snaplines. Makes the code a little easier...
            snapLines.Add( new SnapLine(SnapLineType.Horizontal, -margin.Top, SnapLine.MarginTop, SnapLinePriority.Always));
            snapLines.Add( new SnapLine(SnapLineType.Horizontal, margin.Bottom + height, SnapLine.MarginBottom, SnapLinePriority.Always)); 
            snapLines.Add( new SnapLine(SnapLineType.Vertical, -margin.Left, SnapLine.MarginLeft, SnapLinePriority.Always));
            snapLines.Add( new SnapLine(SnapLineType.Vertical, margin.Right + width, SnapLine.MarginRight, SnapLinePriority.Always)); 
 

            return snapLines; 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.SnapLines"]/*' />
        /// <devdoc> 
        ///     Returns a list of SnapLine objects representing interesting
        ///     alignment points for this control.  These SnapLines are used 
        ///     to assist in the positioning of the control on a parent's 
        ///     surface.
        /// </devdoc> 
        public virtual IList SnapLines {
            get {
                return SnapLinesInternal();
            } 
        }
 
        /// <devdoc> 
        ///     Demand creates the StandardBehavior related to this
        ///     ControlDesigner.  This is used to associate the designer's 
        ///     selection glyphs to a common Behavior (resize in this case).
        /// </devdoc>
        internal virtual System.Windows.Forms.Design.Behavior.Behavior StandardBehavior {
            get { 
                if (resizeBehavior == null) {
                    resizeBehavior = new ResizeBehavior(Component.Site); 
                } 
                return resizeBehavior;
            } 
        }


        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.SerializePerformLayout"]/*' /> 
        /// <devdoc>
        ///     Refer VsWhidbey : 487804. There are certain containers (like ToolStrip) that require PerformLayout to be serialized in the code gen. 
        /// </devdoc> 
        internal virtual bool SerializePerformLayout {
            get { 
                return false;
            }
        }
 
        internal System.Windows.Forms.Design.Behavior.Behavior MoveBehavior {
            get { 
                if (moveBehavior == null) { 
                    moveBehavior = new ContainerSelectorBehavior(Control, Component.Site);
                } 
                return moveBehavior;
            }
        }
 
        /// <devdoc>
        ///     Accessor method for the visible property on control.  We shadow 
        ///     this property at design time. 
        /// </devdoc>
        private bool Visible { 
            get {
                return (bool)ShadowProperties["Visible"];
            }
            set { 
                ShadowProperties["Visible"] = value;
            } 
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InheritanceAttribute"]/*' /> 
        protected override InheritanceAttribute InheritanceAttribute {
            get {
                if(IsRootDesigner) {
                    return InheritanceAttribute.Inherited; 
                }
                return base.InheritanceAttribute; 
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.BaseWndProc"]/*' />
        /// <devdoc>
        ///     Default processing for messages.  This method causes the message to
        ///     get processed by windows, skipping the control.  This is useful if 
        ///     you want to block this message from getting to the control, but
        ///     you do not want to block it from getting to Windows itself because 
        ///     it causes other messages to be generated. 
        /// </devdoc>
        protected void BaseWndProc(ref Message m) { 
            m.Result = NativeMethods.DefWindowProc(m.HWnd, m.Msg, m.WParam, m.LParam);
        }

        internal override bool CanBeAssociatedWith(IDesigner parentDesigner) 
        {
            return CanBeParentedTo(parentDesigner); 
        } 

         /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.CanBeParentedTo"]/*' /> 
         /// <devdoc>
        ///     Determines if the this designer can be parented to the specified desinger --
        ///     generally this means if the control for this designer can be parented into the
        ///     given ParentControlDesigner's designer. 
        /// </devdoc>
        public virtual bool CanBeParentedTo(IDesigner parentDesigner) { 
           ParentControlDesigner p = parentDesigner as ParentControlDesigner; 
           return (p != null && !Control.Contains(p.Control));
        } 

        private void DataBindingsCollectionChanged(object sender, CollectionChangeEventArgs e) {

            // It is possible to use the control designer with NON CONTROl types. 
            //
            Control ctl = Component as Control; 
 
            if (ctl != null) {
                if (ctl.DataBindings.Count == 0 && removalNotificationHooked) { 
                    // remove the notification for the ComponentRemoved event
                    IComponentChangeService csc = (IComponentChangeService) GetService(typeof(IComponentChangeService));
                    if (csc != null) {
                        csc.ComponentRemoved -= new ComponentEventHandler(DataSource_ComponentRemoved); 
                    }
                    removalNotificationHooked = false; 
                } 
                else if (ctl.DataBindings.Count > 0 && !removalNotificationHooked) {
                    // add he notification for the ComponentRemoved event 
                    IComponentChangeService csc = (IComponentChangeService) GetService(typeof(IComponentChangeService));
                    if (csc != null) {
                        csc.ComponentRemoved += new ComponentEventHandler(DataSource_ComponentRemoved);
                    } 
                    removalNotificationHooked = true;
                } 
            } 
        }
 
        private void DataSource_ComponentRemoved(object sender, ComponentEventArgs e) {

            // It is possible to use the control designer with NON CONTROl types.
            // 
            Control ctl = Component as Control;
 
            if (ctl != null) { 
                Debug.Assert(ctl.DataBindings.Count > 0, "we should not be notified if the control has no dataBindings");
 
                ctl.DataBindings.CollectionChanged -= dataBindingsCollectionChanged;
                for (int i = 0; i < ctl.DataBindings.Count; i ++) {
                    Binding binding = ctl.DataBindings[i];
                    if (binding.DataSource == e.Component) { 
                        // remove the binding from the control's collection
                        // this will also remove the binding from the bindingManagerBase's bindingscollection 
                        // NOTE: we can't remove the bindingManager from the bindingContext, cause there may 
                        // be some complex bound controls ( such as the dataGrid, or the ComboBox, or the ListBox )
                        // that still use that bindingManager 
                        ctl.DataBindings.Remove(binding);
                    }
                }
 
                // if after removing those bindings the collection is empty, then
                // unhook the changeNotificationService 
                // 
                if (ctl.DataBindings.Count == 0) {
                    IComponentChangeService csc = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
                    if (csc != null) {
                        csc.ComponentRemoved -= new ComponentEventHandler(DataSource_ComponentRemoved);
                    }
                    removalNotificationHooked = false; 
                }
                ctl.DataBindings.CollectionChanged += dataBindingsCollectionChanged; 
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DefWndProc"]/*' />
        /// <devdoc>
        ///     Default processing for messages.  This method causes the message to
        ///     get processed by the control, rather than the designer. 
        /// </devdoc>
        protected void DefWndProc(ref Message m) { 
            designerTarget.DefWndProc(ref m); 
        }
 
        private void DetachContextMenu(object sender, EventArgs e) {
            ContextMenu = null;
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DisplayError"]/*' />
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
                RTLAwareMessageBox.Show(Control, message, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1, 0);
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Dispose"]/*' /> 
        /// <devdoc> 
        ///      Disposes of this object.
        /// </devdoc> 
        protected override void Dispose(bool disposing) {

            if (disposing) {
 
                if (Control != null) {
 
                    if (dataBindingsCollectionChanged != null) { 
                        Control.DataBindings.CollectionChanged -= dataBindingsCollectionChanged;
                    } 

                    if (Inherited && inheritanceUI != null) {
                        inheritanceUI.RemoveInheritedControl(Control);
                    } 

                    if (removalNotificationHooked) { 
                        IComponentChangeService csc = (IComponentChangeService) GetService(typeof(IComponentChangeService)); 
                        if (csc != null) {
                            csc.ComponentRemoved -= new ComponentEventHandler(DataSource_ComponentRemoved); 
                        }
                        removalNotificationHooked = false;
                    }
 
                    if (disposingHandler != null) {
                        disposingHandler(this, EventArgs.Empty); 
                    } 

                    UnhookChildControls(Control); 
                }

                if (ContextMenu != null) {
                    ContextMenu.Disposed -= new EventHandler(this.DetachContextMenu); 
                }
 
                if (designerTarget != null) { 
                    designerTarget.Dispose();
                } 

                downPos = Point.Empty;

                Control.ControlAdded -= new ControlEventHandler(OnControlAdded); 
                Control.ControlRemoved -= new ControlEventHandler(OnControlRemoved);
                Control.ParentChanged -= new EventHandler(OnParentChanged); 
                Control.SizeChanged -= new EventHandler(OnSizeChanged); 
                Control.LocationChanged -= new EventHandler(OnLocationChanged);
                Control.EnabledChanged -= new EventHandler(OnEnabledChanged); 
            }

            base.Dispose(disposing);
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.EnableDesignTime"]/*' /> 
        /// <devdoc> 
        ///     Enables design time functionality for a child control.  The child control is a child
        ///     of this control designer's control.  The child does not directly participate in 
        ///     persistence, but it will if it is exposed as a property of the main control.  Consider
        ///     a control like the SplitContainer:  it has two panels, Panel1 and Panel2.  These panels
        ///     are exposed through read only Panel1 and Panel2 properties on the SplitContainer class.
        ///     SplitContainer's designer calls EnableDesignTime for each panel, which allows other 
        ///     components to be dropped on them.  But, in order for the contents of Panel1 and Panel2
        ///     to be saved, SplitContainer itself needed to expose the panels as public properties. 
        /// 
        ///     The child paramter is the control to enable.  The name paramter is the name of this
        ///     control as exposed to the end user.  Names need to be unique within a control designer, 
        ///     but do not have to be unique to other control designer's children.
        ///
        ///     This method returns true if the child control could be enabled for design time, or
        ///     false if the hosting infrastructure does not support it.  To support this feature, the 
        ///     hosting infrastructure must expose the INestedContainer class as a service off of the site.
        /// </devdoc> 
        protected bool EnableDesignMode(Control child, string name) { 
            if (child == null) {
                throw new ArgumentNullException("child"); 
            }

            if (name == null) {
                throw new ArgumentNullException("name"); 
            }
 
            INestedContainer nc = GetService(typeof(INestedContainer)) as INestedContainer; 
            if (nc == null) {
                return false; 
            }

            // Only add the child if it doesn't already exist. VSWhidbey #408041.
            for (int i = 0; i < nc.Components.Count; i++) { 
                if (nc.Components[i].Equals(child)) {
                    return true; 
                } 
            }
 
            nc.Add(child, name);

            return true;
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.EnableDragDrop"]/*' /> 
        /// <devdoc> 
        ///      Enables or disables drag/drop support.  This
        ///      hooks drag event handlers to the control. 
        /// </devdoc>
        protected void EnableDragDrop(bool value) {
            Control rc = Control;
 
            if (rc == null) {
                return; 
            } 

            if (value) { 
                rc.DragDrop += new DragEventHandler(this.OnDragDrop);
                rc.DragOver += new DragEventHandler(this.OnDragOver);
                rc.DragEnter += new DragEventHandler(this.OnDragEnter);
                rc.DragLeave += new EventHandler(this.OnDragLeave); 
                rc.GiveFeedback += new GiveFeedbackEventHandler(this.OnGiveFeedback);
                hadDragDrop = rc.AllowDrop; 
                if (!hadDragDrop) { 
                    rc.AllowDrop = true;
                } 
                revokeDragDrop = false;
            }
            else {
                rc.DragDrop -= new DragEventHandler(this.OnDragDrop); 
                rc.DragOver -= new DragEventHandler(this.OnDragOver);
                rc.DragEnter -= new DragEventHandler(this.OnDragEnter); 
                rc.DragLeave -= new EventHandler(this.OnDragLeave); 
                rc.GiveFeedback -= new GiveFeedbackEventHandler(this.OnGiveFeedback);
                if (!hadDragDrop) { 
                    rc.AllowDrop = false;
                }
                revokeDragDrop = true;
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetComponentGlyph"]/*' /> 
        /// <devdoc>
        ///     Returns a 'BodyGlyph' representing the bounds of this control. 
        ///     The BodyGlyph is responsible for hit testing the related CtrlDes
        ///     and forwarding messages directly to the designer.
        /// </devdoc>
        protected virtual ControlBodyGlyph GetControlGlyph(GlyphSelectionType selectionType) { 

            //get the right cursor for this component 
            OnSetCursor(); 
            Cursor cursor = Cursor.Current;
 
            //get the correctly translated bounds
            Rectangle translatedBounds = BehaviorService.ControlRectInAdornerWindow(Control);

            //create our glyph, and set its cursor appropriately 

            ControlBodyGlyph g = null; 
 
            Control parent = Control.Parent;
 
            if (parent != null && host != null && host.RootComponent != Component) {

                Rectangle parentRect = parent.RectangleToScreen(parent.ClientRectangle);
                Rectangle controlRect = Control.RectangleToScreen(Control.ClientRectangle); 

                if (!parentRect.Contains(controlRect) && !parentRect.IntersectsWith(controlRect)) { 
                    //since the parent is completely clipping the control, the control cannot be a 
                    //drop target, and it will not get mouse messages. So we don't have to give
                    //the glyph a transparentbehavior (default for ControlBodyGlyph). But we still 
                    //would like to be able to move the control, so push a MoveBehavior. If we didn't
                    //we wouldn't be able to move the control, since it won't get any mouse messages.
                    //VS Whidbey# 344888
 
                    //VS Whidbey# 399801 - but only do so if the control is selected.
 
                    ISelectionService sel = (ISelectionService)GetService(typeof(ISelectionService)); 
                    if (sel != null && sel.GetComponentSelected(Control)) {
                        g = new ControlBodyGlyph(translatedBounds, cursor, Control, MoveBehavior); 
                    }
                    else if (cursor == Cursors.SizeAll) {
                        //If we get here, OnSetCursor could have set the cursor to SizeAll. But if we fall
                        //into this category, we don't have a MoveBehavior, so we don't want to show the 
                        //SizeAll cursor. Let's make sure the cursor is set to the default cursor.
                        cursor = Cursors.Default; 
                    } 

                } 
            }

            if (g == null) {
                //we are not totally clipped by the parent 
                g = new ControlBodyGlyph(translatedBounds, cursor, Control, this);
            } 
 
            return g;
        } 

        internal ControlBodyGlyph GetControlGlyphInternal(GlyphSelectionType selectionType) {
            return GetControlGlyph(selectionType);
        } 

 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetGlyphs"]/*' /> 
        /// <devdoc>
        ///     Returns a collection of Glyph objects representing the selection 
        ///     borders and grab handles for a standard control.  Note that
        ///     based on 'selectionType' the Glyphs returned will either: represent
        ///     a fully resizeable selection border with grab handles, a locked
        ///     selection border, or a single 'hidden' selection Glyph. 
        /// </devdoc>
        public virtual GlyphCollection GetGlyphs(GlyphSelectionType selectionType) { 
 
            GlyphCollection glyphs = new GlyphCollection();
 
            if (selectionType != GlyphSelectionType.NotSelected) {

                Rectangle translatedBounds = BehaviorService.ControlRectInAdornerWindow(Control);
 
                bool primarySelection = (selectionType == GlyphSelectionType.SelectedPrimary);
 
                SelectionRules rules = SelectionRules; 

                if ((Locked) || (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly)) { 
                    // the lock glyph
                    glyphs.Add(new LockedHandleGlyph(translatedBounds, primarySelection));

                    //the four locked border glyphs 
                    glyphs.Add(new LockedBorderGlyph(translatedBounds, SelectionBorderGlyphType.Top));
                    glyphs.Add(new LockedBorderGlyph(translatedBounds, SelectionBorderGlyphType.Bottom)); 
                    glyphs.Add(new LockedBorderGlyph(translatedBounds, SelectionBorderGlyphType.Left)); 
                    glyphs.Add(new LockedBorderGlyph(translatedBounds, SelectionBorderGlyphType.Right));
                } 
                else if ((rules & SelectionRules.AllSizeable) == SelectionRules.None) {
                    //the non-resizeable grab handle
                    glyphs.Add(new NoResizeHandleGlyph(translatedBounds, rules, primarySelection, MoveBehavior));
 
                    //the four resizeable border glyphs
                    glyphs.Add(new NoResizeSelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Top, MoveBehavior)); 
                    glyphs.Add(new NoResizeSelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Bottom, MoveBehavior)); 
                    glyphs.Add(new NoResizeSelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Left, MoveBehavior));
                    glyphs.Add(new NoResizeSelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Right, MoveBehavior)); 
                    // enable the designeractionpanel for this control if it needs one
                    if (TypeDescriptor.GetAttributes(Component).Contains(DesignTimeVisibleAttribute.Yes) && behaviorService.DesignerActionUI != null)  {
                        Glyph dapGlyph = behaviorService.DesignerActionUI.GetDesignerActionGlyph(Component);
                        if(dapGlyph!=null) { 
                            glyphs.Insert(0, dapGlyph); //we WANT to be in front of the other UI
                        } 
                    } 
                }
                else { 
                    //grab handles
                    if ((rules & SelectionRules.TopSizeable) != 0) {
                        glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.MiddleTop, StandardBehavior, primarySelection));
                        if ((rules & SelectionRules.LeftSizeable) != 0) { 
                            glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.UpperLeft, StandardBehavior, primarySelection));
                        } 
                        if ((rules & SelectionRules.RightSizeable) != 0) { 
                            glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.UpperRight, StandardBehavior, primarySelection));
                        } 
                    }

                    if ((rules & SelectionRules.BottomSizeable) != 0) {
                        glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.MiddleBottom, StandardBehavior, primarySelection)); 
                        if ((rules & SelectionRules.LeftSizeable) != 0) {
                            glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.LowerLeft, StandardBehavior, primarySelection)); 
                        } 
                        if ((rules & SelectionRules.RightSizeable) != 0) {
                            glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.LowerRight, StandardBehavior, primarySelection)); 
                        }
                    }

                    if ((rules & SelectionRules.LeftSizeable) != 0) { 
                        glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.MiddleLeft, StandardBehavior, primarySelection));
                    } 
 
                    if ((rules & SelectionRules.RightSizeable) != 0) {
                        glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.MiddleRight, StandardBehavior, primarySelection)); 
                    }

                    //the four resizeable border glyphs
                    glyphs.Add(new SelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Top, StandardBehavior)); 
                    glyphs.Add(new SelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Bottom, StandardBehavior));
                    glyphs.Add(new SelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Left, StandardBehavior)); 
                    glyphs.Add(new SelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Right, StandardBehavior)); 

                    // enable the designeractionpanel for this control if it needs one 
                    if (TypeDescriptor.GetAttributes(Component).Contains(DesignTimeVisibleAttribute.Yes) && behaviorService.DesignerActionUI != null)  {
                        Glyph dapGlyph = behaviorService.DesignerActionUI.GetDesignerActionGlyph(Component);
                        if(dapGlyph!=null) {
                            glyphs.Insert(0,dapGlyph); //we WANT to be in front of the other UI 
                        }
                    } 
                } 
            }
            return glyphs; 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Allows your component to support a design time user interface.  A TabStrip
        ///     control, for example, has a design time user interface that allows the user 
        ///     to click the tabs to change tabs.  To implement this, TabStrip returns 
        ///     true whenever the given point is within its tabs.
        /// </devdoc> 
        protected virtual bool GetHitTest(Point point) {
            return false;
        }
 
        /// <devdoc>
        ///     Given an LParam as a parameter, this extracts a point in parent 
        ///     coordinates. 
        /// </devdoc>
        private int GetParentPointFromLparam(IntPtr lParam) { 
            Point pt = new Point(NativeMethods.Util.SignedLOWORD((int)lParam), NativeMethods.Util.SignedHIWORD((int)lParam));
            pt = Control.PointToScreen(pt);
            pt = Control.Parent.PointToClient(pt);
            return NativeMethods.Util.MAKELONG(pt.X, pt.Y); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.HookChildControls"]/*' /> 
        /// <devdoc>
        ///     Hooks the children of the given control.  We need to do this for 
        ///     child controls that are not in design mode, which is the case
        ///     for composite controls.
        /// </devdoc>
        protected void HookChildControls(Control firstChild) { 

            foreach(Control child in firstChild.Controls) { 
                if (child != null && host != null) { 
                    if (!(host.GetDesigner(child) is ControlDesigner)) {
 
                        // No, no designer means we must replace the window target in this
                        // control.
                        //
                        IWindowTarget oldTarget = child.WindowTarget; 

                        if (!(oldTarget is ChildWindowTarget)) { 
                            child.WindowTarget = new ChildWindowTarget(this, child, oldTarget); 
                        }
 
                        // ASURT 45655: Some controls (primarily RichEdit) will register themselves as
                        // drag-drop source/targets when they are instantiated. We have to RevokeDragDrop()
                        // for them so that the ParentControlDesigner()'s drag-drop support can work
                        // correctly. Normally, the hwnd for the child control is not created at this time, 
                        // and we will use the WM_CREATE message in ChildWindowTarget's WndProc() to revoke
                        // drag-drop. But, if the handle was already created for some reason, we will need 
                        // to revoke drag-drop right away. 
                        //
                        if (child.IsHandleCreated) { 
                            Application.OleRequired();
                            NativeMethods.RevokeDragDrop(child.Handle);
                            HookChildHandles(child.Handle);
                        } 
                        else {
                            child.HandleCreated += delegate(object sender, EventArgs e) { 
                                HookChildHandles(child.Handle); 
                            };
                        } 

                        // We only hook the children's children if there was no designer.
                        // We leave it up to the designer to hook its own children.
                        // 
                        HookChildControls(child);
                    } 
                } 
            }
        } 

        private int CurrentProcessId {
            get {
                if (currentProcessId == 0) { 
                    currentProcessId = SafeNativeMethods.GetCurrentProcessId();
                } 
                return currentProcessId; 
            }
        } 

        private bool IsWindowInCurrentProcess(IntPtr hwnd) {
            int pid;
            UnsafeNativeMethods.GetWindowThreadProcessId(new HandleRef(null, hwnd), out pid); 

            return pid == CurrentProcessId; 
        } 

        /// <devdoc> 
        ///     Hooks the peer handles of the given child control.  We need
        ///     to do this to handle windows that are not associated with
        ///     a control (such as the combo box edit), and for controls
        ///     that are not in design mode (such as child controls on a 
        ///     user control).
        /// </devdoc> 
 
        [SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults")]
        internal void HookChildHandles(IntPtr firstChild) { 
            IntPtr hwndChild = firstChild;

            while (hwndChild != IntPtr.Zero) {
 
                if (!IsWindowInCurrentProcess(hwndChild)) {
                    break; 
                } 

                // Is it a control? 
                //
                Control child = Control.FromHandle(hwndChild);
                if (child == null) {
                    // No control.  We must subclass this control. 
                    //
                    if (!SubclassedChildWindows.ContainsKey(hwndChild)) { 
                        // ASURT 45655: Some controls (primarily RichEdit) will register themselves as 
                        // drag-drop source/targets when they are instantiated. Since these hwnds do not
                        // have a Windows Forms control associated with them, we have to RevokeDragDrop() 
                        // for them so that the ParentControlDesigner()'s drag-drop support can work
                        // correctly.
                        //
                        NativeMethods.RevokeDragDrop(hwndChild); 

                        new ChildSubClass(this, hwndChild); 
                        SubclassedChildWindows[hwndChild] = true; 
                    }
 
                }

                // UserControl is a special ContainerControl which should "hook to all the WindowHandles"
                // Since it doesnt allow the Mouse to pass through any of its contained controls. 
                // Please refer to VsWhidbey : 293117
                if (child == null || Control is UserControl) 
                { 
                    // Now do the children of this window.
                    // 
                    HookChildHandles(NativeMethods.GetWindow(hwndChild, NativeMethods.GW_CHILD));
                }

                hwndChild = NativeMethods.GetWindow(hwndChild, NativeMethods.GW_HWNDNEXT); 
            }
        } 
 
        internal void RemoveSubclassedWindow(IntPtr hwnd) {
            if (SubclassedChildWindows.ContainsKey(hwnd)) { 
                SubclassedChildWindows.Remove(hwnd);
            }
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///     Called by the host when we're first initialized. 
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            // Visibility works as follows:  If the control's property is not actually set, then
            // set our shadow to true.  Otherwise, grab the shadow value from the control directly and
            // then set the control to be visible if it is not the root component.  Root components
            // will be set to visible = true in their own time by the view. 
            //
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(component.GetType()); 
            PropertyDescriptor visibleProp = props["Visible"]; 
            if (visibleProp == null || visibleProp.PropertyType != typeof(bool) || !visibleProp.ShouldSerializeValue(component))
            { 
                Visible = true;
            }
            else
            { 
                Visible = (bool)visibleProp.GetValue(component);
            } 
 
            PropertyDescriptor enabledProp = props["Enabled"];
            if (enabledProp == null || enabledProp.PropertyType != typeof(bool) || !enabledProp.ShouldSerializeValue(component)) 
            {
                Enabled = true;
            }
            else 
            {
                Enabled = (bool)enabledProp.GetValue(component); 
            } 

 
            initializing = true;
            base.Initialize(component);
            initializing = false;
 
            // And get other commonly used services.
            // 
            host = (IDesignerHost)GetService(typeof(IDesignerHost)); 

            // this is to create the action in the DAP for this component if it requires docking/undocking logic 
            AttributeCollection attributes = TypeDescriptor.GetAttributes(Component);
            DockingAttribute dockingAttribute = (DockingAttribute)attributes[typeof(DockingAttribute)];
            if (dockingAttribute != null && dockingAttribute.DockingBehavior != DockingBehavior.Never) {
                // create the action for this control 
                dockingAction = new DockingActionList(this);
                //add our 'dock in parent' or 'undock in parent' action 
                DesignerActionService das = GetService(typeof(DesignerActionService)) as DesignerActionService; 
                if (das != null) {
                    das.Add(Component, dockingAction); 
                }
            }
            // Hook up the property change notifications we need to track. One for data binding.
            // More for control add / remove notifications 
            //
            dataBindingsCollectionChanged = new CollectionChangeEventHandler(DataBindingsCollectionChanged); 
            Control.DataBindings.CollectionChanged += dataBindingsCollectionChanged; 

            Control.ControlAdded += new ControlEventHandler(OnControlAdded); 
            Control.ControlRemoved += new ControlEventHandler(OnControlRemoved);
            Control.ParentChanged += new EventHandler(OnParentChanged);

            Control.SizeChanged += new EventHandler(OnSizeChanged); 
            Control.LocationChanged += new EventHandler(OnLocationChanged);
 
            // Replace the control's window target with our own.  This 
            // allows us to hook messages.
            // 
            this.DesignerTarget = new DesignerWindowTarget(this);

            // If the handle has already been created for this control, invoke OnCreateHandle so we
            // can hookup our child control subclass. 
            //
            if (Control.IsHandleCreated) { 
                OnCreateHandle(); 
            }
 
            // If we are an inherited control, notify our inheritance UI
            //
            if (Inherited && host != null && host.RootComponent != component) {
                inheritanceUI = (InheritanceUI)GetService(typeof(InheritanceUI)); 
                if (inheritanceUI != null) {
                    inheritanceUI.AddInheritedControl(Control, InheritanceAttribute.InheritanceLevel); 
                } 
            }
 
            // When we drag one control from one form to another, we will end up here.
            // In this case we do not want to set the control to visible, so check ForceVisible.
            if ((host == null || host.RootComponent != component) && ForceVisible) {
                Control.Visible = true; 
            }
 
            // Always make controls enabled, event inherited ones.  Otherwise we won't be able 
            // to select them.
            // 
            Control.Enabled = true;

            //we move enabledchanged below the set to avoid any possible stack overflows.
            //this can occur if the parent is not enabled when we set enabled to true. 
            //see VS#538084
            Control.EnabledChanged += new EventHandler(OnEnabledChanged); 
 
            // And force some shadow properties that we change in the course of
            // initializing the form. 
            //
            AllowDrop = Control.AllowDrop;

            // update the Status Command 
            statusCommandUI = new StatusCommandUI(component.Site);
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InitializeExistingComponent"]/*' />
        /// <devdoc> 
        ///    ControlDesigner overrides this method to handle after-drop cases.
        /// </devdoc>
        public override void InitializeExistingComponent(IDictionary defaultValues) {
            base.InitializeExistingComponent(defaultValues); 

            // unhook any sited children that got ChildWindowTargets 
            foreach (Control c in Control.Controls) { 
                  if (c != null) {
                     ISite site = c.Site; 
                     ChildWindowTarget target = c.WindowTarget as ChildWindowTarget;
                     if (site != null && target != null) {
                        c.WindowTarget = target.OldWindowTarget;
                     } 
                  }
            } 
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InitializeNewComponent"]/*' /> 
        /// <devdoc>
        ///   ControlDesigner overrides this method.  It will look at the default property for the control and,
        ///   if it is of type string, it will set this property's value to the name of the component.  It only
        ///   does this if the designer has been configured with this option in the options service.  This method 
        ///   also connects the control to its parent and positions it.  If you override this method, you should
        ///   always call base. 
        /// </devdoc> 
        public override void InitializeNewComponent(IDictionary defaultValues) {
 
            ISite site = Component.Site;

            if (site != null) {
                PropertyDescriptor textProp = TypeDescriptor.GetProperties(Component)["Text"]; 
                if (textProp != null && textProp.PropertyType == typeof(string) && !textProp.IsReadOnly && textProp.IsBrowsable) {
                    textProp.SetValue(Component, site.Name); 
                } 
            }
 
            if (defaultValues != null) {
                IComponent parent = defaultValues["Parent"] as IComponent;
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
 
                if (parent != null && host != null) {
 
                    ParentControlDesigner parentDesigner = host.GetDesigner(parent) as ParentControlDesigner; 
                    if (parentDesigner != null) {
                        parentDesigner.AddControl(Control, defaultValues); 
                    }

                    Control parentControl = parent as Control;
 
                    if (parentControl != null) {
                        // 
                        // Some containers are docked differently (instead of DockStyle.None) when 
                        // they are added through the designer
                        // 
                        AttributeCollection attributes = TypeDescriptor.GetAttributes(Component);
                        DockingAttribute dockingAttribute = (DockingAttribute)attributes[typeof(DockingAttribute)];

                        if (dockingAttribute != null && dockingAttribute.DockingBehavior != DockingBehavior.Never) { 

                            if (dockingAttribute.DockingBehavior == DockingBehavior.AutoDock) { 
                                bool onlyNonDockedChild = true; 
                                foreach (Control c in parentControl.Controls) {
                                    if (c != Control && c.Dock == DockStyle.None) { 
                                        onlyNonDockedChild = false;
                                        break;
                                    }
                                } 

                                if (onlyNonDockedChild) { 
                                    PropertyDescriptor dockProp = TypeDescriptor.GetProperties(Component)["Dock"]; 
                                    if (dockProp != null && dockProp.IsBrowsable) {
                                        dockProp.SetValue(Component, DockStyle.Fill); 
                                    }
                                }

                            } 
                        }
                    } 
                } 
            }
            // Finally, call base.  Base simply calls OnSetComponentDefaults to preserve old behavior. 
            base.InitializeNewComponent(defaultValues);
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnSetComponentDefaults"]/*' /> 
        /// <devdoc>
        ///     Called when the designer is intialized.  This allows the designer to provide some 
        ///     meaningful default values in the component.  The default implementation of this 
        ///     sets the components's default property to it's name, if that property is a string.
        /// </devdoc> 
        [Obsolete("This method has been deprecated. Use InitializeNewComponent instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public override void OnSetComponentDefaults() {
            // COMPAT: The following code shipped in Everett, so we need to continue to do this.
            // See VSWhidbey #467460 for details. 

            // Don't call base. 
            // 
            ISite site = Component.Site;
            if (site != null) { 
                PropertyDescriptor textProp = TypeDescriptor.GetProperties(Component)["Text"];
                if (textProp != null && textProp.IsBrowsable) {
                    textProp.SetValue(Component, site.Name);
                } 
            }
        } 
 
        /// <devdoc>
        ///     Determines if the given mouse click is a double click or not.  We must 
        ///     handle this ourselves for controls that don't have the CS_DOUBLECLICKS style
        ///     set.
        /// </devdoc>
        private bool IsDoubleClick(int x, int y) { 
            bool doubleClick = false;
 
            int wait = SystemInformation.DoubleClickTime; 
            int elapsed = SafeNativeMethods.GetTickCount() -  lastClickMessageTime;
 
            if (elapsed <= wait) {
                Size dblClick = SystemInformation.DoubleClickSize;

                if (x >= lastClickMessagePositionX - dblClick.Width 
                    && x <= lastClickMessagePositionX + dblClick.Width
                    && y >= lastClickMessagePositionY - dblClick.Height 
                    && y <= lastClickMessagePositionY + dblClick.Height) { 

                    doubleClick = true; 
                }
            }

            if (!doubleClick) { 
                lastClickMessagePositionX = x;
                lastClickMessagePositionY = y; 
                lastClickMessageTime = SafeNativeMethods.GetTickCount(); 
            }
            else { 
                lastClickMessagePositionX = lastClickMessagePositionY = 0;
                lastClickMessageTime = 0;
            }
 
            return doubleClick;
        } 
 
        private bool IsMouseMessage(int msg) {
 
            if (msg >= NativeMethods.WM_MOUSEFIRST && msg <= NativeMethods.WM_MOUSELAST) {
                return true;
            }
 
            switch (msg) {
                // WM messages not covered by the above block 
                case NativeMethods.WM_MOUSEHOVER: 
                case NativeMethods.WM_MOUSELEAVE:
 
                // WM_NC messages
                case NativeMethods.WM_NCMOUSEMOVE:
                case NativeMethods.WM_NCLBUTTONDOWN:
                case NativeMethods.WM_NCLBUTTONUP: 
                case NativeMethods.WM_NCLBUTTONDBLCLK:
                case NativeMethods.WM_NCRBUTTONDOWN: 
                case NativeMethods.WM_NCRBUTTONUP: 
                case NativeMethods.WM_NCRBUTTONDBLCLK:
                case NativeMethods.WM_NCMBUTTONDOWN: 
                case NativeMethods.WM_NCMBUTTONUP:
                case NativeMethods.WM_NCMBUTTONDBLCLK:
                case NativeMethods.WM_NCMOUSEHOVER:
                case NativeMethods.WM_NCMOUSELEAVE: 
                case NativeMethods.WM_NCXBUTTONDOWN:
                case NativeMethods.WM_NCXBUTTONUP: 
                case NativeMethods.WM_NCXBUTTONDBLCLK: 

                    return true; 
                default:
                    return false;
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnContextMenu"]/*' /> 
        /// <devdoc> 
        ///     Called when the context menu should be displayed
        /// </devdoc> 
        protected virtual void OnContextMenu(int x, int y) {
            ShowContextMenu(x, y);
        }
 
        /// <devdoc>
        ///     Called in response to a new control being added to this designer's control. 
        ///     We check to see if the control has an associated ControlDesigner.  If it 
        ///     doesn't, we hook its window target so we can sniff messages and make
        ///     it ui inactive. 
        /// </devdoc>
        private void OnControlAdded(object sender, ControlEventArgs e) {
            if (e.Control != null && host != null) {
                if (!(host.GetDesigner(e.Control) is ControlDesigner)) { 

                    // No, no designer means we must replace the window target in this 
                    // control. 
                    //
                    IWindowTarget oldTarget = e.Control.WindowTarget; 

                    if (!(oldTarget is ChildWindowTarget)) {
                        e.Control.WindowTarget = new ChildWindowTarget(this, e.Control, oldTarget);
                    } 

                    // ASURT 45655: Some controls (primarily RichEdit) will register themselves as 
                    // drag-drop source/targets when they are instantiated. We have to RevokeDragDrop() 
                    // for them so that the ParentControlDesigner()'s drag-drop support can work
                    // correctly. Normally, the hwnd for the child control is not created at this time, 
                    // and we will use the WM_CREATE message in ChildWindowTarget's WndProc() to revoke
                    // drag-drop. But, if the handle was already created for some reason, we will need
                    // to revoke drag-drop right away.
                    // 
                    if (e.Control.IsHandleCreated) {
                        Application.OleRequired(); 
                        NativeMethods.RevokeDragDrop(e.Control.Handle); 

 
                        // We only hook the control's children if there was no designer.
                        // We leave it up to the designer to hook its own children.
                        //
                        HookChildControls(e.Control); 
                    }
                } 
            } 
        }
 
        /// <devdoc>
        ///     Called in response to a control being removed from this designer's control.
        ///     If we previously changed out this control's window target, we undo that
        ///     work here. 
        /// </devdoc>
        private void OnControlRemoved(object sender, ControlEventArgs e) { 
            if (e.Control != null) { 

                // No, no designer means we must replace the window target in this 
                // control.
                //
                ChildWindowTarget oldTarget = e.Control.WindowTarget as ChildWindowTarget;
 
                if (oldTarget != null) {
                    e.Control.WindowTarget = oldTarget.OldWindowTarget; 
                } 

                UnhookChildControls(e.Control); 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnCreateHandle"]/*' /> 
        /// <devdoc>
        ///      This is called immediately after the control handle has been created. 
        /// </devdoc> 
        protected virtual void OnCreateHandle() {
            OnHandleChange(); 

            if (revokeDragDrop) {
                int n = NativeMethods.RevokeDragDrop(Control.Handle);
            } 
        }
 
 
        /// <devdoc>
        ///      Event handler for our drag enter event.  The host will call us with 
        ///      this when an OLE drag event happens.
        /// </devdoc>
        private void OnDragEnter(object s, DragEventArgs e) {
            if (BehaviorService != null) { 
                //Tell the BehaviorService to monitor mouse messages
                //so it can send appropriate drag notifications. 
                // 
                BehaviorService.StartDragNotification();
            } 

            OnDragEnter(e);
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnDragEnter1"]/*' />
        /// <devdoc> 
        ///     Called when a drag-drop operation enters the control designer view 
        ///
        /// </devdoc> 
        protected virtual void OnDragEnter(DragEventArgs de) {
            // unhook our events - we don't want to create an infinite loop.
            Control control = Control;
            DragEventHandler handler = new DragEventHandler(this.OnDragEnter); 
            control.DragEnter -= handler;
            ((IDropTarget)Control).OnDragEnter(de); 
            control.DragEnter += handler; 
        }
 
        /// <devdoc>
        ///      Event handler for our drag drop event.  The host will call us with
        ///      this when an OLE drag event happens.
        /// </devdoc> 
        private void OnDragDrop(object s, DragEventArgs e) {
 
            if (BehaviorService != null) { 
                //this will cause the BehSvc to return from 'drag mode'
                // 
                BehaviorService.EndDragNotification();
            }

            OnDragDrop(e); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnDragComplete"]/*' /> 
        /// <devdoc>
        ///     Called to cleanup a D&D operation 
        /// </devdoc>
        protected virtual void OnDragComplete(DragEventArgs de) {
            // default implementation - does nothing.
        } 

 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnDragDrop1"]/*' /> 
        /// <devdoc>
        ///     Called when a drag drop object is dropped onto the control designer view 
        /// </devdoc>
        protected virtual void OnDragDrop(DragEventArgs de) {
            // unhook our events - we don't want to create an infinite loop.
            Control control = Control; 
            DragEventHandler handler = new DragEventHandler(this.OnDragDrop);
            control.DragDrop -= handler; 
            ((IDropTarget)Control).OnDragDrop(de); 
            control.DragDrop += handler;
 
            OnDragComplete(de);
        }

        /// <devdoc> 
        ///      Event handler for our drag leave event.  The host will call us with
        ///      this when an OLE drag event happens. 
        /// </devdoc> 
        private void OnDragLeave(object s, EventArgs e) {
            OnDragLeave(e); 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnDragLeave1"]/*' />
        /// <devdoc> 
        ///     Called when a drag-drop operation leaves the control designer view
        /// 
        /// </devdoc> 
        protected virtual void OnDragLeave(EventArgs e) {
            // unhook our events - we don't want to create an infinite loop. 
            Control control = Control;
            EventHandler handler = new EventHandler(this.OnDragLeave);
            control.DragLeave -= handler;
            ((IDropTarget)Control).OnDragLeave(e); 
            control.DragLeave += handler;
        } 
 
        /// <devdoc>
        ///      Event handler for our drag over event.  The host will call us with 
        ///      this when an OLE drag event happens.
        /// </devdoc>
        private void OnDragOver(object s, DragEventArgs e) {
            OnDragOver(e); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnDragOver1"]/*' /> 
        /// <devdoc>
        ///     Called when a drag drop object is dragged over the control designer view 
        /// </devdoc>
        protected virtual void OnDragOver(DragEventArgs de) {
            // unhook our events - we don't want to create an infinite loop.
            Control control = Control; 
            DragEventHandler handler = new DragEventHandler(this.OnDragOver);
            control.DragOver -= handler; 
            ((IDropTarget)Control).OnDragOver(de); 
            control.DragOver += handler;
        } 

        /// <devdoc>
        ///      Event handler for our GiveFeedback event, which is called when a drag operation
        ///      is in progress.  The host will call us with 
        ///      this when an OLE drag event happens.
        /// </devdoc> 
        private void OnGiveFeedback(object s, GiveFeedbackEventArgs e) { 
            OnGiveFeedback(e);
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnGiveFeedback1"]/*' />
        /// <devdoc>
        ///      Event handler for our GiveFeedback event, which is called when a drag operation 
        ///      is in progress.  The host will call us with
        ///      this when an OLE drag event happens. 
        /// </devdoc> 
        protected virtual void OnGiveFeedback(GiveFeedbackEventArgs e) {
        } 

        /// <devdoc>
        ///      This is called whenever the control handle changes.
        /// </devdoc> 
        private void OnHandleChange() {
            // We must now traverse child handles for this control.  There are 
            // three types of child handles and we are interested in two of 
            // them:
            // 
            //  1.  Child handles that do not have a Control associated
            //      with them.  We must subclass these and prevent them
            //      from getting design-time events.
            // 
            // 2.   Child handles that do have a Control associated
            //      with them, but the control does not have a designer. 
            //      We must hook the WindowTarget on these controls and 
            //      prevent them from getting design-time events.
            // 
            // 3.   Child handles that do have a Control associated
            //      with them, and the control has a designer.  We
            //      ignore these and let the designer handle their
            //      messages. 
            //
            HookChildHandles(NativeMethods.GetWindow(Control.Handle, NativeMethods.GW_CHILD)); 
            HookChildControls(Control); 
        }
 
        /// <devdoc>
        ///     Called in response to a double-click of the left mouse button.  We
        ///     Just call this on the event service
        /// </devdoc> 
        private void OnMouseDoubleClick() {
 
            try { 
               DoDefaultAction();
            } 
            catch (Exception e) {
                DisplayError(e);
                if (ClientUtils.IsCriticalException(e)) {
                    throw; 
                }
            } 
            catch { 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseDragBegin"]/*' />
        /// <devdoc>
        ///     Called in response to the left mouse button being pressed on a 
        ///     component. It ensures that the component is selected.
        /// </devdoc> 
        protected virtual void OnMouseDragBegin(int x, int y) { 
            // Ignore another mouse down if we are already in a drag.
            // 
            if (BehaviorService == null && mouseDragLast != InvalidPoint) {
                return;
            }
 
            mouseDragLast = new Point(x, y);
            ctrlSelect = (Control.ModifierKeys & Keys.Control) != 0; 
            ISelectionService sel = (ISelectionService)GetService(typeof(ISelectionService)); 

            // If the CTRL key isn't down, select this component, 
            // otherwise, we wait until the mouse up
            //
            // Make sure the component is selected
            // 
            if (!ctrlSelect && sel != null) {
                sel.SetSelectedComponents(new object[] {Component}, SelectionTypes.Primary); 
            } 

            Control.Capture = true; 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseDragEnd"]/*' />
        /// <devdoc> 
        ///     Called at the end of a drag operation.  This either commits or rolls back the
        ///     drag. 
        /// </devdoc> 
        protected virtual void OnMouseDragEnd(bool cancel) {
            mouseDragLast = InvalidPoint; 
            Control.Capture = false;

            if (!mouseDragMoved) {
                if (!cancel) { 
                    ISelectionService sel = (ISelectionService)GetService(typeof(ISelectionService));
                    bool shiftSelect = (Control.ModifierKeys & Keys.Shift) != 0; 
                    if (!shiftSelect && (ctrlSelect || (sel != null && !sel.GetComponentSelected(Component)))) { 
                        if (sel != null) {
                            sel.SetSelectedComponents(new object[] {Component}, SelectionTypes.Primary); 
                        }
                        ctrlSelect = false;
                    }
                } 
                return;
            } 
            mouseDragMoved = false; 
            ctrlSelect = false;
 
            // And now finish the drag.

            if (BehaviorService != null && BehaviorService.Dragging && cancel) {
                BehaviorService.CancelDrag = true; 
            }
 
            // Leave this here in case we are doing a ComponentTray drag 
            if (selectionUISvc == null) {
                selectionUISvc = (ISelectionUIService)GetService(typeof(ISelectionUIService)); 
            }

            if (selectionUISvc == null) {
                return; 
            }
 
            // We must check to ensure that UI service is still in drag mode.  It is 
            // possible that the user hit escape, which will cancel drag mode.
            // 
            if (selectionUISvc.Dragging) {
                selectionUISvc.EndDrag(cancel);
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseDragMove"]/*' /> 
        /// <devdoc> 
        ///     Called for each movement of the mouse.  This will check to see if a drag operation
        ///     is in progress.  If so, it will pass the updated drag dimensions on to the selection 
        ///     UI service.
        /// </devdoc>
        protected virtual void OnMouseDragMove(int x, int y) {
            if (!mouseDragMoved) { 
                Size minDrag = SystemInformation.DragSize;
                Size minDblClick = SystemInformation.DoubleClickSize; 
 
                minDrag.Width = Math.Max(minDrag.Width, minDblClick.Width);
                minDrag.Height = Math.Max(minDrag.Height, minDblClick.Height); 

                // we have to make sure the mouse moved farther than
                // the minimum drag distance before we actually start
                // the drag 
                //
                if (mouseDragLast == InvalidPoint || 
                    (Math.Abs(mouseDragLast.X - x) < minDrag.Width && 
                     Math.Abs(mouseDragLast.Y - y) < minDrag.Height)) {
                    return; 
                }
                else {
                    mouseDragMoved = true;
                    // we're on the move, so we're not in a ctrlSelect 
                    //
                    ctrlSelect = false; 
                } 
            }
 
            // Make sure the component is selected
            //
            // VSWhidbey #461078
            // But only select it if it is not already the primary selection, and we want to toggle 
            // the current primary selection.
            ISelectionService sel = (ISelectionService)GetService(typeof(ISelectionService)); 
            if (sel != null && !Component.Equals(sel.PrimarySelection)) { 
                sel.SetSelectedComponents(new object[] {Component}, SelectionTypes.Primary | SelectionTypes.Toggle);
            } 

            if (BehaviorService != null && sel != null) {

                //create our list of controls-to-drag 
                ArrayList dragControls = new ArrayList();
                ICollection selComps = sel.GetSelectedComponents(); 
                //must identify a required parent to avoid dragging mixes of children 
                Control requiredParent = null;
 
                foreach (IComponent comp in selComps) {
                    Control control = comp as Control;
                    if (control != null) {
                        if (requiredParent == null) { 
                            requiredParent = control.Parent;
                        } 
                        else if (!requiredParent.Equals(control.Parent)) { 
                            continue;//mixed selection of different parents - don't add this
                        } 

                        ControlDesigner des = host.GetDesigner(comp) as ControlDesigner;
                        if (des != null && (des.SelectionRules & SelectionRules.Moveable) != 0) {
                            dragControls.Add(comp); 
                        }
                    } 
                } 

                //if we have controls-to-drag, create our new behavior 
                //and start the drag/drop operation
                if (dragControls.Count > 0) {
                    using (Graphics adornerGraphics = BehaviorService.AdornerWindowGraphics) {
                        DropSourceBehavior dsb = new DropSourceBehavior(dragControls, Control.Parent, mouseDragLast); 
                        BehaviorService.DoDragDrop(dsb);
                    } 
                } 
            }
 
            mouseDragLast = InvalidPoint;
            mouseDragMoved = false;
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseEnter"]/*' />
        /// <devdoc> 
        ///     Called when the mouse first enters the control. This is forwarded to the parent 
        ///     designer to enable the container selector.
        /// </devdoc> 
        protected virtual void OnMouseEnter() {
            Control ctl = Control;
            Control parent = ctl;
 
            object parentDesigner = null;
            while (parentDesigner == null && parent != null) { 
                parent = parent.Parent; 
                if (parent != null) {
                    object d = host.GetDesigner(parent); 
                    if (d != this) {
                        parentDesigner = d;
                    }
                } 
            }
 
            ControlDesigner cd = parentDesigner as ControlDesigner; 
            if (cd != null) {
                cd.OnMouseEnter(); 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseHover"]/*' /> 
        /// <devdoc>
        ///     Called after the mouse hovers over the control. This is forwarded to the parent 
        ///     designer to enabled the container selector. 
        /// </devdoc>
        protected virtual void OnMouseHover() { 
            Control ctl = Control;
            Control parent = ctl;

            object parentDesigner = null; 
            while (parentDesigner == null && parent != null) {
                parent = parent.Parent; 
                if (parent != null) { 
                    object d = host.GetDesigner(parent);
                    if (d != this) { 
                        parentDesigner = d;
                    }
                }
            } 

            ControlDesigner cd = parentDesigner as ControlDesigner; 
            if (cd != null) { 
                cd.OnMouseHover();
            } 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseLeave"]/*' />
        /// <devdoc> 
        ///     Called when the mouse first enters the control. This is forwarded to the parent
        ///     designer to enable the container selector. 
        /// </devdoc> 
        protected virtual void OnMouseLeave() {
            Control ctl = Control; 
            Control parent = ctl;

            object parentDesigner = null;
            while (parentDesigner == null && parent != null) { 
                parent = parent.Parent;
                if (parent != null) { 
                    object d = host.GetDesigner(parent); 
                    if (d != this) {
                        parentDesigner = d; 
                    }
                }
            }
 
            ControlDesigner cd = parentDesigner as ControlDesigner;
            if (cd != null) { 
                cd.OnMouseLeave(); 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnPaintAdornments"]/*' />
        /// <devdoc>
        ///     Called when the control we're designing has finished painting.  This method 
        ///     gives the designer a chance to paint any additional adornments on top of the
        ///     control. 
        /// </devdoc> 
        protected virtual void OnPaintAdornments(PaintEventArgs pe) {
 
            // If this control is being inherited, paint it
            //
            if (inheritanceUI != null && pe.ClipRectangle.IntersectsWith(inheritanceUI.InheritanceGlyphRectangle)) {
                pe.Graphics.DrawImage(inheritanceUI.InheritanceGlyph, 0, 0); 
            }
        } 
 
        private void OnParentChanged(object sender, EventArgs e) {
            if (Control.IsHandleCreated) { 
                OnHandleChange();
            }
        }
 
        // VSWhidbey #245901
        // HACK HACK 
        // This is a workaround to some problems with the ComponentCache that we should fix. 
        // When this is removed remember to change ComponentCache's RemoveEntry method back to private (from internal).
        private void OnSizeChanged(object sender, EventArgs e) { 
            System.ComponentModel.Design.Serialization.ComponentCache cache =
                (System.ComponentModel.Design.Serialization.ComponentCache) GetService(typeof(System.ComponentModel.Design.Serialization.ComponentCache));
            object component = Component;
            if (cache != null && component != null) { 
                cache.RemoveEntry(component);
            } 
        } 

         private void OnLocationChanged(object sender, EventArgs e) { 
            System.ComponentModel.Design.Serialization.ComponentCache cache =
                (System.ComponentModel.Design.Serialization.ComponentCache) GetService(typeof(System.ComponentModel.Design.Serialization.ComponentCache));
            object component = Component;
            if (cache != null && component != null) { 
                cache.RemoveEntry(component);
            } 
        } 

        private void OnEnabledChanged(object sender, EventArgs e) { 
            // VSWhidbey #154310 - Never allow controls to be disabled at design time.
            if (!enabledchangerecursionguard) {
                 enabledchangerecursionguard = true;
                 try { 
                      Control.Enabled = true;
                 } 
                 finally { 
                     enabledchangerecursionguard = false;
                 } 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnSetCursor"]/*' /> 
        /// <devdoc>
        ///     Called each time the cursor needs to be set.  The ControlDesigner behavior here 
        ///     will set the cursor to one of three things: 
        ///     1.  If the toolbox service has a tool selected, it will allow the toolbox service to
        ///     set the cursor. 
        ///     2.  If the selection UI service shows a locked selection, or if there is no location
        ///     property on the control, then the default arrow will be set.
        ///     3.  Otherwise, the four headed arrow will be set to indicate that the component can
        ///     be clicked and moved. 
        ///     4.  If the user is currently dragging a component, the crosshair cursor will be used
        ///     instead of the four headed arrow. 
        /// </devdoc> 
        protected virtual void OnSetCursor() {
 
            if (Control.Dock != DockStyle.None) {
                Cursor.Current = Cursors.Default;
            }
            else { 

                if (toolboxSvc == null) { 
                    toolboxSvc = (IToolboxService)GetService(typeof(IToolboxService)); 
                }
 
                if (toolboxSvc != null && toolboxSvc.SetCursor()) {
                    return;
                }
 
                if (!locationChecked) {
                    locationChecked = true; 
 
                    try {
                        hasLocation = TypeDescriptor.GetProperties(Component)["Location"] != null; 
                    }
                    catch {
                    }
                } 

                if (!hasLocation) { 
                    Cursor.Current = Cursors.Default; 
                    return;
                } 

                if (Locked) {
                    Cursor.Current = Cursors.Default;
                    return; 
                }
 
                Cursor.Current = Cursors.SizeAll; 
            }
        } 

        /// <devdoc>
        ///     Paints a red rectangle with a red X, painted on a white background.  Used
        ///     when the control has thrown an exception. 
        /// </devdoc>
        private void PaintException(PaintEventArgs e, Exception ex) { 
            StringFormat stringFormat = new StringFormat(); 
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near; 

            string exceptionText = ex.ToString();
            stringFormat.SetMeasurableCharacterRanges(new CharacterRange[] {new CharacterRange(0, exceptionText.Length)});
 
            // rendering calculations...
            // 
            int penThickness = 2; 
            Size glyphSize = SystemInformation.IconSize;
            int marginX = penThickness * 2; 
            int marginY = penThickness * 2;

            Rectangle clientRectangle = Control.ClientRectangle;
 
            Rectangle borderRectangle = clientRectangle;
            borderRectangle.X++; 
            borderRectangle.Y++; 
            borderRectangle.Width -=2;
            borderRectangle.Height-=2; 

            Rectangle imageRect = new Rectangle(marginX, marginY, glyphSize.Width, glyphSize.Height);

            Rectangle textRect = clientRectangle; 
            textRect.X = imageRect.X + imageRect.Width + 2 * marginX;
            textRect.Y = imageRect.Y; 
            textRect.Width -= (textRect.X + marginX + penThickness); 
            textRect.Height -= (textRect.Y + marginY + penThickness);
 
            using (Font errorFont = new Font(Control.Font.FontFamily, Math.Max(SystemInformation.ToolWindowCaptionHeight - SystemInformation.BorderSize.Height - 2, Control.Font.Height), GraphicsUnit.Pixel)) {

                using(Region textRegion = e.Graphics.MeasureCharacterRanges(exceptionText, errorFont, textRect, stringFormat)[0]) {
                    // paint contents... clipping optimizations for less flicker... 
                    //
                    Region originalClip = e.Graphics.Clip; 
 
                    e.Graphics.ExcludeClip(textRegion);
                    e.Graphics.ExcludeClip(imageRect); 
                    try {
                        e.Graphics.FillRectangle(Brushes.White, clientRectangle);
                    }
                    finally { 
                        e.Graphics.Clip = originalClip;
                    } 
 
                    using (Pen pen = new Pen(Color.Red, penThickness)) {
                        e.Graphics.DrawRectangle(pen, borderRectangle); 
                    }

                    Icon err = SystemIcons.Error;
 
                    e.Graphics.FillRectangle(Brushes.White, imageRect);
                    e.Graphics.DrawIcon(err, imageRect.X, imageRect.Y); 
 
                    textRect.X++;
                    e.Graphics.IntersectClip(textRegion); 
                    try {
                        e.Graphics.FillRectangle(Brushes.White, textRect);
                        e.Graphics.DrawString(exceptionText, errorFont, new SolidBrush(Control.ForeColor), textRect, stringFormat);
                    } 
                    finally {
                        e.Graphics.Clip = originalClip; 
                    } 
                }
            } 
        }


 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        ///      Allows a designer to filter the set of properties 
        ///      the component it is designing will expose through the
        ///      TypeDescriptor object.  This method is called 
        ///      immediately before its corresponding "Post" method.
        ///      If you are overriding this method you should call
        ///      the base implementation before you perform your own
        ///      filtering. 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties); 

            PropertyDescriptor prop; 

            // Handle shadowed properties
            //
            string[] shadowProps = new string[] { 
                "Visible",
                "Enabled", 
                "ContextMenu", 
                "AllowDrop",
                "Location", 
                "Name"
            };

            Attribute[] empty = new Attribute[0]; 

            for (int i = 0; i < shadowProps.Length; i++) { 
                prop = (PropertyDescriptor)properties[shadowProps[i]]; 
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(ControlDesigner), prop, empty); 
                }
            }

            // replace this one seperately because it is of a different type (DesignerControlCollection) than 
            // the original property (ControlCollection)
            // 
            PropertyDescriptor controlsProp = (PropertyDescriptor)properties["Controls"]; 

            if (controlsProp != null) { 
                Attribute[] attrs = new Attribute[controlsProp.Attributes.Count];
                controlsProp.Attributes.CopyTo(attrs, 0);
                properties["Controls"] = TypeDescriptor.CreateProperty(typeof(ControlDesigner), "Controls", typeof(DesignerControlCollection), attrs);
            } 

            PropertyDescriptor sizeProp = (PropertyDescriptor)properties["Size"]; 
            if (sizeProp != null) { 
                properties["Size"] = new CanResetSizePropertyDescriptor(sizeProp);
            } 

            // Now we add our own design time properties.
            //
            properties["Locked"] = TypeDescriptor.CreateProperty(typeof(ControlDesigner), "Locked", typeof(bool), 
                                                        new DefaultValueAttribute(false),
                                                        BrowsableAttribute.Yes, 
                                                        CategoryAttribute.Design, 
                                                        DesignOnlyAttribute.Yes,
                                                        new SRDescriptionAttribute(SR.lockedDescr)); 



 
        }
 
        /// <devdoc> 
        ///     Returns true if the visible property should be persisted in code gen.
        /// </devdoc> 
        private void ResetVisible() {
            Visible = true;
        }
 
        /// <devdoc>
        ///     Returns true if the Enabled property should be persisted in code gen. 
        /// </devdoc> 
        private void ResetEnabled() {
            Enabled = true; 
        }

        /// <devdoc>
        ///     Sets an unhandled exception that is raised from a control or child control wndproc. 
        /// </devdoc>
        internal void SetUnhandledException(Control owner, Exception exception) { 
            if (thrownException == null) { 
                thrownException = exception;
                if (owner == null) { 
                    owner = Control;
                }
                string stack = string.Empty;
                string[] exceptionLines = exception.StackTrace.Split('\r', '\n'); 
                string typeName = owner.GetType().FullName;
                foreach(string line in exceptionLines) { 
                    if (line.IndexOf(typeName) != -1) { 
                        stack = string.Format(CultureInfo.CurrentCulture, "{0}\r\n{1}", stack, line);
                    } 
                }

                Exception wrapper = new Exception(SR.GetString(SR.ControlDesigner_WndProcException, typeName, exception.Message, stack), exception);
                DisplayError(wrapper); 

                // hide all the child controls. 
                // 
                foreach (Control c in Control.Controls) {
                    c.Visible = false; 
                }

                Control.Invalidate(true);
            } 
        }
 
        private bool ShouldSerializeAllowDrop() { 
            return AllowDrop != hadDragDrop;
        } 


        /// <devdoc>
        ///     Returns true if the enabled property should be persisted in code gen. 
        /// </devdoc>
        private bool ShouldSerializeEnabled() { 
            return ShadowProperties.ShouldSerializeValue("Enabled", true); 
        }
 
        /// <devdoc>
        ///     Returns true if the visible property should be persisted in code gen.
        /// </devdoc>
        private bool ShouldSerializeVisible() { 
            return ShadowProperties.ShouldSerializeValue("Visible", true);
        } 
 
        private bool ShouldSerializeName() {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(host != null);

            return initializing ? (Component != host.RootComponent)  //for non root components, respect the name that the base Control serialized unless changed
                : ShadowProperties.ShouldSerializeValue("Name", null); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.UnhookChildControls"]/*' /> 
        /// <devdoc>
        ///     Hooks the children of the given control.  We need to do this for 
        ///     child controls that are not in design mode, which is the case
        ///     for composite controls.
        /// </devdoc>
        protected void UnhookChildControls(Control firstChild) { 

            if (host == null) { 
                host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            }
 
            foreach(Control child in firstChild.Controls) {
                IWindowTarget oldTarget = null;
                if (child != null) {
 
                    // No, no designer means we must replace the window target in this
                    // control. 
                    // 
                    oldTarget= child.WindowTarget;
 
                    ChildWindowTarget target = oldTarget as ChildWindowTarget;
                    if (target != null) {
                        child.WindowTarget = target.OldWindowTarget;
                    } 
                }
                if (!(oldTarget is DesignerWindowTarget)) { 
                    UnhookChildControls(child); 
                }
 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.WndProc"]/*' /> 
        /// <devdoc>
        ///     This method should be called by the extending designer for each message 
        ///     the control would normally receive.  This allows the designer to pre-process 
        ///     messages before allowing them to be routed to the control.
        /// </devdoc> 
        protected virtual void WndProc(ref Message m) {
            IMouseHandler mouseHandler = null;

            // We look at WM_NCHITTEST to determine if the mouse 
            // is in a live region of the control
            // 
            if (m.Msg == NativeMethods.WM_NCHITTEST) { 
                if (!inHitTest) {
                    inHitTest = true; 
                    Point pt = new Point((short)NativeMethods.Util.LOWORD((int)m.LParam),
                                         (short)NativeMethods.Util.HIWORD((int)m.LParam));
                    try {
                        liveRegion = GetHitTest(pt); 
                    }
                    catch (Exception e) { 
                        liveRegion = false; 
                        if (ClientUtils.IsCriticalException(e)) {
                            throw; 
                        }
                    }
                    catch {
                        liveRegion = false; 
                    }
                    inHitTest = false; 
                } 
            }
 
            // Check to see if the mouse
            // is in a live region of the control
            // and that the context key is not being fired
            // 
            bool isContextKey = (m.Msg == NativeMethods.WM_CONTEXTMENU);
 
            if (liveRegion && (IsMouseMessage(m.Msg) || isContextKey)) { 
                // ASURT 70725: The ActiveX DataGrid control brings up a context menu on right mouse down when
                // it is in edit mode. 
                // And, when we generate a WM_CONTEXTMENU message later, it calls DefWndProc() which by default
                // calls the parent (formdesigner). The FormDesigner then brings up the AxHost context menu.
                // This code causes recursive WM_CONTEXTMENU messages to be ignored till we return from the
                // live region message. 
                //
                if (m.Msg == NativeMethods.WM_CONTEXTMENU) { 
                    Debug.Assert(!inContextMenu, "Recursively hitting live region for context menu!!!"); 
                    inContextMenu = true;
                } 

                try {
                    DefWndProc(ref m);
                } 
                finally {
                    if (m.Msg == NativeMethods.WM_CONTEXTMENU) { 
                        inContextMenu = false; 
                    }
                    if (m.Msg == NativeMethods.WM_LBUTTONUP) 
                    {
                        // terminate the drag.
                        //
                        // Vs Whidbey : 355250 
                        // DTS SRX040824604234 TabControl loses shortcut menu options after adding ActiveX control.
                        OnMouseDragEnd(true); 
                    } 

                } 
                return;
            }

            // Get the x and y coordniates of the mouse message 
            //
            int x = 0, y = 0; 
 
            // Look for a mouse handler.
            // 
            //


 
            if (m.Msg >= NativeMethods.WM_MOUSEFIRST && m.Msg <= NativeMethods.WM_MOUSELAST
                || m.Msg >= NativeMethods.WM_NCMOUSEMOVE && m.Msg <= NativeMethods.WM_NCMBUTTONDBLCLK 
                || m.Msg == NativeMethods.WM_SETCURSOR) { 

                if (eventSvc == null) { 
                    eventSvc = (IEventHandlerService)GetService(typeof(IEventHandlerService));
                }
                if (eventSvc != null) {
                    mouseHandler = (IMouseHandler)eventSvc.GetHandler(typeof(IMouseHandler)); 
                }
            } 
 
            if (m.Msg >= NativeMethods.WM_MOUSEFIRST && m.Msg <= NativeMethods.WM_MOUSELAST) {
 
                NativeMethods.POINT pt = new NativeMethods.POINT();
                pt.x = NativeMethods.Util.SignedLOWORD((int)m.LParam);
                pt.y = NativeMethods.Util.SignedHIWORD((int)m.LParam);
                NativeMethods.MapWindowPoints(m.HWnd, IntPtr.Zero, pt, 1); 
                x = pt.x;
                y = pt.y; 
            } 
            else if (m.Msg >= NativeMethods.WM_NCMOUSEMOVE && m.Msg <= NativeMethods.WM_NCMBUTTONDBLCLK) {
                x = NativeMethods.Util.SignedLOWORD((int)m.LParam); 
                y = NativeMethods.Util.SignedHIWORD((int)m.LParam);
            }

            // This is implemented on the base designer for 
            // UI activation support.  We call it so that
            // we can support UI activation. 
            // 
            MouseButtons button = MouseButtons.None;
 
            switch (m.Msg) {
                case NativeMethods.WM_CREATE:
                    DefWndProc(ref m);
 
                    // Only call OnCreateHandle if this is our OWN
                    // window handle -- the designer window procs are 
                    // re-entered for child controls. 
                    //
                    if (m.HWnd == Control.Handle) { 
                        OnCreateHandle();
                    }
                    break;
 
                case NativeMethods.WM_GETOBJECT:
                    // See "How to Handle WM_GETOBJECT" in MSDN 
                    if (NativeMethods.OBJID_CLIENT == (int)m.LParam) { 

                        // Get the IAccessible GUID 
                        //
                        Guid IID_IAccessible = new Guid(NativeMethods.uuid_IAccessible);

                        // Get an Lresult for the accessibility Object for this control 
                        //
                        IntPtr punkAcc; 
                        try { 
                            IAccessible iacc = (IAccessible)this.AccessibilityObject;
 
                            if (iacc == null) {
                                // Accessibility is not supported on this control
                                //
                                m.Result = (IntPtr)0; 
                            }
                            else { 
                                // Obtain the Lresult 
                                //
                                punkAcc = Marshal.GetIUnknownForObject(iacc); 

                                try {
                                    m.Result = UnsafeNativeMethods.LresultFromObject(ref IID_IAccessible, m.WParam, punkAcc);
                                } 
                                finally {
                                    Marshal.Release(punkAcc); 
                                } 
                            }
                        } 
                        catch (Exception e) {
                            throw e;
                        }
                        catch { 
                            throw;
                        } 
                    } 
                    else {  // m.lparam != OBJID_CLIENT, so do default message processing
                        DefWndProc(ref m); 
                    }
                    break;

                case NativeMethods.WM_MBUTTONDOWN: 
                case NativeMethods.WM_MBUTTONUP:
                case NativeMethods.WM_MBUTTONDBLCLK: 
                case NativeMethods.WM_NCMOUSEHOVER: 
                case NativeMethods.WM_NCMOUSELEAVE:
                case NativeMethods.WM_MOUSEWHEEL: 
                case NativeMethods.WM_NCMBUTTONDOWN:
                case NativeMethods.WM_NCMBUTTONUP:
                case NativeMethods.WM_NCMBUTTONDBLCLK:
                    // We intentionally eat these messages. 
                    //
                    break; 
 
                case NativeMethods.WM_MOUSEHOVER:
                    if (mouseHandler != null) { 
                        mouseHandler.OnMouseHover(Component);
                    }
                    else {
                        OnMouseHover(); 
                    }
                    break; 
 
                case NativeMethods.WM_MOUSELEAVE:
                    OnMouseLeave(); 
                    BaseWndProc(ref m);
                    break;

                case NativeMethods.WM_NCLBUTTONDBLCLK: 
                case NativeMethods.WM_LBUTTONDBLCLK:
                case NativeMethods.WM_NCRBUTTONDBLCLK: 
                case NativeMethods.WM_RBUTTONDBLCLK: 

                    if ((m.Msg == NativeMethods.WM_NCRBUTTONDBLCLK || m.Msg == NativeMethods.WM_RBUTTONDBLCLK)) { 
                        button = MouseButtons.Right;
                    }
                    else {
                        button = MouseButtons.Left; 
                    }
 
                    if (button == MouseButtons.Left) { 

                        // We handle doubleclick messages, and we also process 
                        // our own simulated double clicks for controls that don't
                        // specify CS_WANTDBLCLKS.
                        //
                        if (mouseHandler != null) { 
                            mouseHandler.OnMouseDoubleClick(Component);
                        } 
                        else { 
                            OnMouseDoubleClick();
                        } 
                    }
                    break;

                case NativeMethods.WM_NCLBUTTONDOWN: 
                case NativeMethods.WM_LBUTTONDOWN:
                case NativeMethods.WM_NCRBUTTONDOWN: 
                case NativeMethods.WM_RBUTTONDOWN: 

                    if ((m.Msg == NativeMethods.WM_NCRBUTTONDOWN || m.Msg == NativeMethods.WM_RBUTTONDOWN)) { 
                        button = MouseButtons.Right;
                    }
                    else {
                        button = MouseButtons.Left; 
                    }
 
                    // We don't really want the focus, but we want to focus the designer. 
                    // Below we handle WM_SETFOCUS and do the right thing.
                    // 
                    NativeMethods.SendMessage(Control.Handle, NativeMethods.WM_SETFOCUS, 0, 0);

                    // We simulate doubleclick for things that don't...
                    // 
                    if (button == MouseButtons.Left && IsDoubleClick(x, y)) {
                        if (mouseHandler != null) { 
                            mouseHandler.OnMouseDoubleClick(Component); 
                        }
                        else { 
                            OnMouseDoubleClick();
                        }
                    }
                    else { 

                        toolPassThrough = false; 
 
                        if (!this.EnableDragRect && button == MouseButtons.Left) {
 
                            if (toolboxSvc == null) {
                                toolboxSvc = (IToolboxService)GetService(typeof(IToolboxService));
                            }
 
                            if (toolboxSvc != null && toolboxSvc.GetSelectedToolboxItem((IDesignerHost)GetService(typeof(IDesignerHost))) != null) {
                                // there is a tool to be dragged, so set passthrough and pass to the parent. 
                                toolPassThrough = true; 
                            }
                        } 
                        else {
                            toolPassThrough = false;
                        }
 

                        if (toolPassThrough) { 
                            NativeMethods.SendMessage(Control.Parent.Handle, m.Msg, m.WParam, (IntPtr)GetParentPointFromLparam(m.LParam)); 
                            return;
                        } 

                        if (mouseHandler != null) {
                            mouseHandler.OnMouseDown(Component, button, x, y);
                        } 
                        else if (button == MouseButtons.Left) {
                            OnMouseDragBegin(x,y); 
 
                        }
                        else if (button == MouseButtons.Right) { 
                            ISelectionService selSvc = (ISelectionService)GetService(typeof(ISelectionService));
                            if (selSvc != null) {
                                selSvc.SetSelectedComponents(new object[] {Component}, SelectionTypes.Primary);
                            } 
                        }
 
                        lastMoveScreenX = x; 
                        lastMoveScreenY = y;
                    } 
                    break;

                case NativeMethods.WM_NCMOUSEMOVE:
                case NativeMethods.WM_MOUSEMOVE: 
                    if (((int)m.WParam & NativeMethods.MK_LBUTTON) != 0) {
                        button = MouseButtons.Left; 
                    } 
                    else if (((int)m.WParam & NativeMethods.MK_RBUTTON) != 0) {
                        button = MouseButtons.Right; 
                        toolPassThrough = false;
                    }
                    else {
                        toolPassThrough = false; 
                    }
 
                    if (lastMoveScreenX != x || lastMoveScreenY != y) { 
                        if (toolPassThrough) {
                            NativeMethods.SendMessage(Control.Parent.Handle, m.Msg, m.WParam, (IntPtr)GetParentPointFromLparam(m.LParam)); 
                            return;
                        }

                        if (mouseHandler != null) { 
                            mouseHandler.OnMouseMove(Component, x, y);
                        } 
                        else if (button == MouseButtons.Left) { 
                            OnMouseDragMove(x, y);
                        } 
                    }
                    lastMoveScreenX = x;
                    lastMoveScreenY = y;
 
                    // VSWhidbey #487865. We eat WM_NCMOUSEMOVE messages, since we don't want the non-client area
                    // of design time controls to repaint on mouse move. 
                    if (m.Msg == NativeMethods.WM_MOUSEMOVE) { 
                        BaseWndProc(ref m);
                    } 

                    break;

                case NativeMethods.WM_NCLBUTTONUP: 
                case NativeMethods.WM_LBUTTONUP:
                case NativeMethods.WM_NCRBUTTONUP: 
                case NativeMethods.WM_RBUTTONUP: 

                    // This is implemented on the base designer for 
                    // UI activation support.
                    //
                    if ((m.Msg == NativeMethods.WM_NCRBUTTONUP || m.Msg == NativeMethods.WM_RBUTTONUP)) {
                        button = MouseButtons.Right; 
                    }
                    else { 
                        button = MouseButtons.Left; 
                    }
 
                    bool moved = mouseDragMoved;

                    // And terminate the drag.
                    // 
                    if (mouseHandler != null) {
                        mouseHandler.OnMouseUp(Component, button); 
                    } 
                    else {
                        if (toolPassThrough) { 
                            NativeMethods.SendMessage(Control.Parent.Handle, m.Msg, m.WParam, (IntPtr)GetParentPointFromLparam(m.LParam));
                            toolPassThrough = false;
                            return;
                        } 

                        if (button == MouseButtons.Left) { 
                            OnMouseDragEnd(false); 
                        }
                    } 

                    // clear any pass through.
                    toolPassThrough = false;
 
                    BaseWndProc(ref m);
                    break; 
 
                case NativeMethods.WM_PRINTCLIENT:
                    { 
                        using (Graphics g = Graphics.FromHdc(m.WParam)) {
                            using (PaintEventArgs e = new PaintEventArgs(g, Control.ClientRectangle)) {
                                DefWndProc(ref m);
                                OnPaintAdornments(e); 
                            }
                        } 
                    } 
                    break;
 
                case NativeMethods.WM_PAINT:
                    // First, save off the update region and
                    // call our base class.
                    // 
                    if (OleDragDropHandler.FreezePainting) {
                        NativeMethods.ValidateRect(m.HWnd, IntPtr.Zero); 
                        break; 
                    }
 
                    if (Control == null) {
                        break;
                    }
 
                    NativeMethods.RECT clip = new NativeMethods.RECT();
                    IntPtr hrgn = NativeMethods.CreateRectRgn(0, 0, 0, 0); 
                    NativeMethods.GetUpdateRgn(m.HWnd, hrgn, false); 
                    NativeMethods.GetUpdateRect(m.HWnd, ref clip, false);
                    Region r = Region.FromHrgn(hrgn); 
                    Rectangle paintRect = Rectangle.Empty;

                    try {
                        // Call the base class to do its own painting. 
                        //
                        if (thrownException == null) { 
                            DefWndProc(ref m); 
                        }
 
                        // Now do our own painting.
                        //
                        Graphics gr = Graphics.FromHwnd(m.HWnd);
                        if (m.HWnd != Control.Handle) { 
                            // Re-map the clip rect we pass to the paint event args
                            // to our child coordinates. 
                            // 
                            NativeMethods.POINT pt = new NativeMethods.POINT();
                            pt.x = 0; 
                            pt.y = 0;
                            NativeMethods.MapWindowPoints(m.HWnd, Control.Handle, pt, 1);
                            gr.TranslateTransform(-pt.x, -pt.y);
 
                            NativeMethods.MapWindowPoints(m.HWnd, Control.Handle, ref clip, 2);
                        } 
 
                        paintRect = new Rectangle(clip.left, clip.top, clip.right-clip.left, clip.bottom-clip.top);
                        PaintEventArgs pevent = new PaintEventArgs(gr, paintRect); 

                        try {
                            gr.Clip = r;
                            if (thrownException == null) { 
                                OnPaintAdornments(pevent);
                            } 
                            else { 
                                UnsafeNativeMethods.PAINTSTRUCT ps = new UnsafeNativeMethods.PAINTSTRUCT();
                                IntPtr dc = UnsafeNativeMethods.BeginPaint(m.HWnd, ref ps); 
                                PaintException(pevent, thrownException);
                                UnsafeNativeMethods.EndPaint(m.HWnd, ref ps);
                            }
                        } 
                        finally {
                            // pevent will dispose the graphics object... no need to do that separately... 
                            // 
                            if (pevent != null) {
                                pevent.Dispose(); 
                            }
                            else {
                                gr.Dispose();
                            } 
                        }
                    } 
                    finally { 
                       r.Dispose();
                       NativeMethods.DeleteObject(hrgn); 
                    }

                    if (OverlayService != null) {
                        //this will allow any Glyphs to re-paint 
                        //after this control and its designer has painted
                        paintRect.Location = Control.PointToScreen(paintRect.Location); 
                        OverlayService.InvalidateOverlays(paintRect); 
                    }
 
                    break;

                case NativeMethods.WM_NCPAINT:
                case NativeMethods.WM_NCACTIVATE: 
                    if (m.Msg == NativeMethods.WM_NCACTIVATE) {
                        DefWndProc(ref m); 
                    } 
                    else if (thrownException == null) {
                        DefWndProc(ref m); 
                    }


                   // For some reason we dont always get an NCPAINT with the WM_NCACTIVATE 
                   // usually this repros with themes on.... this can happen when someone calls RedrawWindow without
                   // the flags to send an NCPAINT.  So that we dont double process this event, our calls 
                   // to redraw window should not have RDW_ERASENOW | RDW_UPDATENOW. 

                    if (OverlayService != null) { 

                        if (Control != null && Control.Size != Control.ClientSize && Control.Parent != null) {
                            // we have a non-client region to invalidate
                            Rectangle controlScreenBounds = new Rectangle(Control.Parent.PointToScreen(Control.Location), Control.Size); 
                            Rectangle clientAreaScreenBounds = new Rectangle(Control.PointToScreen(Point.Empty), Control.ClientSize);
 
                            using (Region nonClient = new Region(controlScreenBounds)) { 
                                nonClient.Exclude(clientAreaScreenBounds);
                                OverlayService.InvalidateOverlays(nonClient); 
                            }
                        }
                    }
                    break; 

                case NativeMethods.WM_SETCURSOR: 
                    // We always handle setting the cursor ourselves. 
                    //
 
                    if (liveRegion) {
                        DefWndProc(ref m);
                        break;
                    } 

                    if (mouseHandler != null) { 
                        mouseHandler.OnSetCursor(Component); 
                    }
                    else { 
                        OnSetCursor();
                    }
                    break;
 
                case NativeMethods.WM_SIZE:
                    if (this.thrownException != null) { 
                        Control.Invalidate(); 
                    }
                    DefWndProc(ref m); 
                    break;
                case NativeMethods.WM_CANCELMODE:
                    // When we get cancelmode (i.e. you tabbed away to another window)
                    // then we want to cancel any pending drag operation! 
                    //
                    OnMouseDragEnd(true); 
                    DefWndProc(ref m); 
                    break;
 
                case NativeMethods.WM_SETFOCUS:
                    // We always eat the focus.
                    //
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
                    break; 

            case NativeMethods.WM_CONTEXTMENU: 
                    if (inContextMenu) {
                        break;
                    }
 
                    // We handle this in addition to a right mouse button.
                    // Why?  Because we often eat the right mouse button, so 
                    // it may never generate a WM_CONTEXTMENU.  However, the 
                    // system may generate one in response to an F-10.
                    // 
                    x = NativeMethods.Util.SignedLOWORD((int)m.LParam);
                    y = NativeMethods.Util.SignedHIWORD((int)m.LParam);

                    ToolStripKeyboardHandlingService keySvc = (ToolStripKeyboardHandlingService)GetService(typeof(ToolStripKeyboardHandlingService)); 
                    bool handled = false;
 
                    if (keySvc != null) 
                    {
                        handled = keySvc.OnContextMenu(x, y); 
                    }

                    if (!handled)
                    { 
                        if (x == -1 && y == -1) {
                            // for shift-F10 
                            Point p = Cursor.Position; 
                            x = p.X;
                            y = p.Y; 
                        }
                        OnContextMenu(x, y);
                    }
                    break; 

                default: 
 
                    if (m.Msg == NativeMethods.WM_MOUSEENTER) {
                        OnMouseEnter(); 
                        BaseWndProc(ref m);
                    }
                    // We eat all key handling to the control.  Controls generally
                    // should not be getting focus anyway, so this shouldn't happen. 
                    // However, we want to prevent this as much as possible.
                    // 
                    else if (m.Msg < NativeMethods.WM_KEYFIRST || m.Msg > NativeMethods.WM_KEYLAST) { 
                        DefWndProc(ref m);
                    } 
                    break;
            }
        }
 
        /// <devdoc>
        ///     This is a subclass window that we attach to all child windows. 
        ///     We use this to disable a child hwnd's UI during design time. 
        /// </devdoc>
 
        private class ChildSubClass : NativeWindow, IDesignerTarget {
            private ControlDesigner designer;

            /// <devdoc> 
            ///     Creates a new ChildSubClass object.  This subclasses
            ///     the given hwnd immediately. 
            /// </devdoc> 

            // AssignHandle calls NativeWindow::OnHandleChanged, but we do not override it so we should be okay 
            [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
            public ChildSubClass(ControlDesigner designer, IntPtr hwnd) {
                this.designer = designer;
                if (designer != null) { 
                    designer.disposingHandler += new EventHandler(this.OnDesignerDisposing);
                } 
                AssignHandle(hwnd); 
            }
 
            void IDesignerTarget.DefWndProc(ref Message m) {
                base.DefWndProc(ref m);
            }
 
            public void Dispose() {
                designer = null; 
            } 

            private void OnDesignerDisposing(object sender, EventArgs e) { 
                Dispose();
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ChildSubClass.WndProc"]/*' /> 
            /// <devdoc>
            ///     Overrides Window's WndProc to process messages. 
            /// </devdoc> 
            protected override void WndProc(ref Message m) {
                if (designer == null) { 
                    DefWndProc(ref m);
                    return;
                }
 
                if (m.Msg == NativeMethods.WM_DESTROY) {
                    designer.RemoveSubclassedWindow(m.HWnd); 
                } 
                if (m.Msg == NativeMethods.WM_PARENTNOTIFY &&
                        NativeMethods.Util.LOWORD((int)m.WParam) == (short)NativeMethods.WM_CREATE) { 
                        designer.HookChildHandles(m.LParam);    // they will get removed from the collection just above
                }

                // We want these messages to go through the designer's WndProc 
                // method, and we want people to be able to do default processing
                // with the designer's DefWndProc.  So, we stuff ourselves into 
                // the designers window target and call their WndProc. 
                //
                IDesignerTarget designerTarget = designer.DesignerTarget; 

                designer.DesignerTarget = this;

                Debug.Assert(m.HWnd == this.Handle, "Message handle differs from target handle"); 

                try { 
                   designer.WndProc(ref m); 
                }
                catch (Exception ex){ 
                    designer.SetUnhandledException(Control.FromChildHandle(m.HWnd), ex);
                }
                catch {
                    Debug.Fail("non CLS-compliant exception"); 
                }
                finally { 
                   // make sure the designer wasn't destroyed 
                   //
                   if (designer != null && designer.Component != null) { 
                       designer.DesignerTarget = designerTarget;
                   }
                }
            } 
        }
 
        /// <devdoc> 
        ///     This is a subclass class that attaches to a control instance.
        ///     Controls can be subclasses by hooking their IWindowTarget 
        ///     interface.  We use this to disable a child hwnd's UI during
        ///     design time.
        /// </devdoc>
        private class ChildWindowTarget : IWindowTarget, IDesignerTarget { 
            private ControlDesigner designer;
            private Control childControl; 
            private IWindowTarget oldWindowTarget; 
            private IntPtr handle = IntPtr.Zero;
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ChildWindowTarget.ChildWindowTarget"]/*' />
            /// <devdoc>
            ///     Creates a new ChildWindowTarget object.  This hooks the
            ///     given control's window target. 
            /// </devdoc>
            public ChildWindowTarget(ControlDesigner designer, Control childControl, IWindowTarget oldWindowTarget) { 
                this.designer = designer; 
                this.childControl = childControl;
                this.oldWindowTarget = oldWindowTarget; 
            }

            public IWindowTarget OldWindowTarget {
                get { 
                    return oldWindowTarget;
                } 
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ChildWindowTarget.DefWndProc"]/*' /> 
            /// <devdoc>
            ///     Causes default window processing for the given message.  We
            ///     just forward this on to the old control target.
            /// </devdoc> 
            public void DefWndProc(ref Message m) {
                oldWindowTarget.OnMessage(ref m); 
            } 

            [SuppressMessage("Microsoft.Usage", "CA2216:DisposableTypesShouldDeclareFinalizer")] 
            public void Dispose() {
                // Do nothing.  We will pick this up through a null DesignerTarget property
                // when we come out of the message loop.
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ChildWindowTarget.OnHandleChange"]/*' /> 
            /// <devdoc> 
            ///      Called when the window handle of the control has changed.
            /// </devdoc> 
            public void OnHandleChange(IntPtr newHandle) {
                handle = newHandle;
                oldWindowTarget.OnHandleChange(newHandle);
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ChildWindowTarget.OnMessage"]/*' /> 
            /// <devdoc> 
            ///      Called to do control-specific processing for this window.
            /// </devdoc> 
            public void OnMessage(ref Message m) {
                // If the designer has jumped ship, the continue
                // partying on messages, but send them back to the original control.
                if (designer.Component == null) { 
                    oldWindowTarget.OnMessage(ref m);
                    return; 
                } 

                // We want these messages to go through the designer's WndProc 
                // method, and we want people to be able to do default processing
                // with the designer's DefWndProc.  So, we stuff the old window
                // target into the designer's target and then call their
                // WndProc. 
                //
                IDesignerTarget designerTarget = designer.DesignerTarget; 
 
                designer.DesignerTarget = this;
 
                try {
                    designer.WndProc(ref m);
                }
                catch (Exception ex) { 
                    designer.SetUnhandledException(childControl, ex);
                } 
                catch { 
                }
                finally { 

                    // If the designer disposed us, then we should follow suit.
                    //
                    if (designer.DesignerTarget == null) { 
                        designerTarget.Dispose();
                    } 
                    else { 
                        designer.DesignerTarget = designerTarget;
                    } 

                    // ASURT 45655: Controls (primarily RichEdit) will register themselves as
                    // drag-drop source/targets when they are instantiated. Normally, when they
                    // are being designed, we will RevokeDragDrop() in their designers. The problem 
                    // occurs when these controls are inside a UserControl. At that time, we do not
                    // have a designer for these controls, and they prevent the ParentControlDesigner's 
                    // drag-drop from working. What we do is to loop through all child controls that 
                    // do not have a designer (in HookChildControls()), and RevokeDragDrop() after
                    // their handles have been created. 
                    //
                    if (m.Msg == NativeMethods.WM_CREATE) {
                        Debug.Assert(handle != IntPtr.Zero, "Handle for control not created");
                        NativeMethods.RevokeDragDrop(handle); 
                    }
                } 
            } 
        }
 
        /// <devdoc>
        ///     This class is the interface the designer will use to funnel messages
        ///     back to the control.
        /// </devdoc> 
        private interface IDesignerTarget : IDisposable {
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.IDesignerTarget.DefWndProc"]/*' /> 
            /// <devdoc> 
            ///     Causes default window processing for the given message.  We
            ///     just forward this on to the old control target. 
            /// </devdoc>
            void DefWndProc(ref Message m);
        }
 
        /// <devdoc>
        ///     This class replaces Control's window target, which effectively subclasses 
        ///     the control in a handle-independent way. 
        /// </devdoc>
        private class DesignerWindowTarget : IWindowTarget, IDesignerTarget, IDisposable { 
            internal ControlDesigner designer;
            internal IWindowTarget oldTarget;

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerWindowTarget.DesignerWindowTarget"]/*' /> 
            /// <devdoc>
            ///     Creates a new DesignerTarget object. 
            /// </devdoc> 
            public DesignerWindowTarget(ControlDesigner designer) {
 
                Control control = designer.Control;

                this.designer = designer;
                this.oldTarget = control.WindowTarget; 
                control.WindowTarget = this;
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerWindowTarget.DefWndProc"]/*' />
            /// <devdoc> 
            ///     Causes default window processing for the given message.  We
            ///     just forward this on to the old control target.
            /// </devdoc>
            public void DefWndProc(ref Message m) { 
                oldTarget.OnMessage(ref m);
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerWindowTarget.Dispose"]/*' />
            /// <devdoc> 
            ///      Disposes this window target.  This re-establishes the
            ///      prior window target.
            /// </devdoc>
            public void Dispose() { 
                if (designer != null) {
                    designer.Control.WindowTarget = oldTarget; 
                    designer = null; 
                }
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerWindowTarget.OnHandleChange"]/*' />
            /// <devdoc>
            ///      Called when the window handle of the control has changed. 
            /// </devdoc>
            public void OnHandleChange(IntPtr newHandle) { 
                oldTarget.OnHandleChange(newHandle); 
                if (newHandle != IntPtr.Zero) {
                    designer.OnHandleChange(); 
                }
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerWindowTarget.OnMessage"]/*' /> 
            /// <devdoc>
            ///      Called to do control-specific processing for this window. 
            /// </devdoc> 
            public void OnMessage(ref Message m) {
                // We want these messages to go through the designer's WndProc 
                // method, and we want people to be able to do default processing
                // with the designer's DefWndProc.  So, we stuff ourselves into
                // the designers window target and call their WndProc.
                // 
                ControlDesigner currentDesigner = designer;
 
                if (currentDesigner != null) { 
                    IDesignerTarget designerTarget = currentDesigner.DesignerTarget;
                    currentDesigner.DesignerTarget = this; 

                   try {
                       currentDesigner.WndProc(ref m);
                   } 
                   catch (Exception ex) {
                       designer.SetUnhandledException(designer.Control,  ex); 
                   } 
                   catch {
                        Debug.Fail("non-CLS compliant exception"); 
                   }
                   finally {
                       if (currentDesigner != null) {
                            currentDesigner.DesignerTarget = designerTarget; 
                       }
                   } 
                } 
                else {
                    DefWndProc(ref m); 
                }
            }
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesignerAccessibleObject"]/*' />
        [ComVisible(true)] 
        public class ControlDesignerAccessibleObject : AccessibleObject { 

            private ControlDesigner designer = null; 
            private Control control = null;
            private IDesignerHost host = null;
            private ISelectionService selSvc = null;
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.ControlDesignerAccessibleObject"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public ControlDesignerAccessibleObject(ControlDesigner designer, Control control) { 
                this.designer = designer;
                this.control = control;
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Bounds"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public override Rectangle Bounds { 
                get {
                    return control.AccessibilityObject.Bounds;
                }
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Description"]/*' /> 
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public override string Description {
                get {
                    return control.AccessibilityObject.Description;
                } 
            }
 
            private IDesignerHost DesignerHost { 
                get {
                    if (host == null) { 
                        host = (IDesignerHost)designer.GetService(typeof(IDesignerHost));
                    }
                    return host;
                } 
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.DefaultAction"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public override string DefaultAction {
                get {
                    return ""; 
                }
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Name"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public override string Name {
                get { 
                    return control.Name;
                } 
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Parent"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public override AccessibleObject Parent { 
                get {
                    return control.AccessibilityObject.Parent; 
                } 
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Role"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public override AccessibleRole Role {
                get { 
                    return control.AccessibilityObject.Role; 
                }
            } 

            private ISelectionService SelectionService {
                get {
                    if (selSvc == null) { 
                        selSvc = (ISelectionService)designer.GetService(typeof(ISelectionService));
                    } 
 
                    return selSvc;
                } 
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.State"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public override AccessibleStates State { 
                get {
                    AccessibleStates state = control.AccessibilityObject.State; 

                    ISelectionService s = SelectionService;
                    if (s != null) {
                        if (s.GetComponentSelected(this.control)) { 
                            state |= AccessibleStates.Selected;
                        } 
                        if (s.PrimarySelection == this.control) { 
                            state |= AccessibleStates.Focused;
                        } 
                    }

                    return state;
                } 
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Value"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public override string Value {
                get {
                    return control.AccessibilityObject.Value; 
                }
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.GetChild"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public override AccessibleObject GetChild(int index) {
                Debug.WriteLineIf(CompModSwitches.MSAA.TraceInfo, "ControlDesignerAccessibleObject.GetChild(" + index.ToString(CultureInfo.InvariantCulture) + ")"); 

                Control.ControlAccessibleObject childAccObj = control.AccessibilityObject.GetChild(index) as Control.ControlAccessibleObject; 
                if (childAccObj != null) { 
                    AccessibleObject cao = GetDesignerAccessibleObject(childAccObj);
                    if (cao != null) { 
                        return cao;
                    }
                }
 
                return control.AccessibilityObject.GetChild(index);
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesignerAccessibleObject.GetChildCount"]/*' />
            public override int GetChildCount() { 
                return control.AccessibilityObject.GetChildCount();
            }
            private AccessibleObject GetDesignerAccessibleObject(Control.ControlAccessibleObject cao) {
                if (cao == null) { 
                    return null;
                } 
                ControlDesigner ctlDesigner = DesignerHost.GetDesigner(cao.Owner) as ControlDesigner; 
                if (ctlDesigner != null) {
                    return ctlDesigner.AccessibilityObject; 
                }
                return null;
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesignerAccessibleObject.GetFocused"]/*' />
            public override AccessibleObject GetFocused() { 
                if ((this.State & AccessibleStates.Focused) != 0) { 
                    return this;
                } 
                return base.GetFocused();
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesignerAccessibleObject.GetSelected"]/*' /> 
            public override AccessibleObject GetSelected() {
                if ((this.State & AccessibleStates.Selected) != 0) { 
                    return this; 
                }
                return base.GetFocused(); 
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesignerAccessibleObject.HitTest"]/*' />
            public override AccessibleObject HitTest(int x, int y) { 
                return control.AccessibilityObject.HitTest(x, y);
            } 
        } 

        [ListBindable(false)] 
        [DesignerSerializer(typeof(DesignerControlCollectionCodeDomSerializer), typeof(CodeDomSerializer))]
        internal class DesignerControlCollection : Control.ControlCollection, IList {

            Control.ControlCollection realCollection; 

            public DesignerControlCollection(Control owner) : base(owner) { 
                this.realCollection = owner.Controls; 
            }
 
             /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.Count"]/*' />
            /// <devdoc>
            ///     Retrieves the number of child controls.
            /// </devdoc> 
            public override int Count {
                get { 
                    return realCollection.Count; 
                }
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.ICollection.SyncRoot"]/*' />
            /// <internalonly/>
            object ICollection.SyncRoot { 
                get {
                    return this; 
                } 
            }
 
            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.ICollection.IsSynchronized"]/*' />
            /// <internalonly/>
            bool ICollection.IsSynchronized {
                get { 
                    return false;
                } 
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.IsFixedSize"]/*' /> 
            /// <internalonly/>
            bool IList.IsFixedSize {
                get {
                    return false; 
                }
            } 
 
            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.IsReadOnly"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public new bool IsReadOnly {
                get { 
                    return realCollection.IsReadOnly;
                } 
            } 

 
            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.Add"]/*' />
            /// <internalonly/>
            int IList.Add(object control) {
                return ((IList)realCollection).Add(control); 
            }
 
            public override void Add(Control c) { 
                realCollection.Add(c);
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.AddRange"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public override void AddRange(Control[] controls) { 
                realCollection.AddRange(controls); 
            }
 
            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.Contains"]/*' />
            /// <internalonly/>
            bool IList.Contains(object control) {
                return ((IList)realCollection).Contains(control); 
            }
 
            public new void CopyTo(Array dest, int index) { 
                realCollection.CopyTo(dest, index);
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.Equals"]/*' />
            /// <internalonly/>
            public override bool Equals(object other) { 
                return realCollection.Equals(other);
            } 
 
            public new IEnumerator GetEnumerator() {
               return realCollection.GetEnumerator(); 
            }

            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.GetHashCode"]/*' />
            /// <internalonly/> 
            public override int GetHashCode() {
                return realCollection.GetHashCode(); 
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.IndexOf"]/*' /> 
            /// <internalonly/>
            int IList.IndexOf(object control) {
                return ((IList)realCollection).IndexOf(control);
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.Insert"]/*' /> 
            /// <internalonly/> 
            void IList.Insert(int index, object value) {
                ((IList)realCollection).Insert(index, value); 
            }

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.Remove"]/*' />
            /// <internalonly/> 
            void IList.Remove(object control) {
                ((IList)realCollection).Remove(control); 
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.Remove"]/*' /> 
            /// <internalonly/>
            void IList.RemoveAt(int index) {
                ((IList)realCollection).RemoveAt(index);
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.this"]/*' /> 
            /// <internalonly/> 
            object IList.this[int index] {
                get { 
                    return ((IList)realCollection)[index];
                }
                set {
                    throw new NotSupportedException(); 
                }
            } 
 
            public override int GetChildIndex(Control child, bool throwException) {
                return realCollection.GetChildIndex(child,throwException); 
            }

            // we also need to redirect this guy
            public override void SetChildIndex(Control child, int newIndex) { 
                realCollection.SetChildIndex(child, newIndex);
            } 
 
            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.Clear"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public override void Clear() {
 
                // only remove the sited non-inherited components
                // 
                for (int i = realCollection.Count - 1; i >= 0; i--) { 
                    if (realCollection[i] != null &&
                        realCollection[i].Site != null && 
                        TypeDescriptor.GetAttributes(realCollection[i]).Contains(InheritanceAttribute.NotInherited)) {
                        realCollection.RemoveAt(i);
                    }
                } 
            }
        } 
 
        // Custom code dom serializer for the DesignerControlCollection. We need this so we can filter out controls
        // that aren't sited in the host's container. 
        internal class DesignerControlCollectionCodeDomSerializer : CollectionCodeDomSerializer {
            protected override object SerializeCollection(IDesignerSerializationManager manager, CodeExpression targetExpression, Type targetType, ICollection originalCollection, ICollection valuesToSerialize) {
                ArrayList subset = new ArrayList();
 
                if (valuesToSerialize != null && valuesToSerialize.Count > 0) {
                    foreach (object val in valuesToSerialize) { 
                        IComponent comp = val as IComponent; 

                        if (comp != null && comp.Site != null && !(comp.Site is INestedSite)) { 
                            subset.Add(comp);
                        }
                    }
                } 

                return base.SerializeCollection(manager, targetExpression, targetType, originalCollection, subset); 
            } 
        }
 
        /// <devdoc>
        ///     This class is used to provide the 'dock in parent' or 'undock in parent'
        ///     designer action item.
        /// </devdoc> 
        private class DockingActionList : DesignerActionList {
            private ControlDesigner _designer; 
            private IDesignerHost   _host; 
            /// <devdoc>
            ///     Caches off the localized name of our action 
            /// </devdoc>
            public DockingActionList(ControlDesigner owner) : base(owner.Component) {
                _designer = owner;
                _host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
            }
 
            private string GetActionName() { 
                PropertyDescriptor dockProp = TypeDescriptor.GetProperties(Component)["Dock"];
                if (dockProp != null) { 
                    DockStyle dockStyle = (DockStyle)dockProp.GetValue(Component);
                    if(dockStyle == DockStyle.Fill) {
                        return SR.GetString(SR.DesignerShortcutUndockInParent);
                    } else { 
                        return SR.GetString(SR.DesignerShortcutDockInParent);
                    } 
                } 
                return null;
            } 

            /// <devdoc>
            ///     Returns our undock or dock item
            /// </devdoc> 
            public override DesignerActionItemCollection GetSortedActionItems()     {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                string actionName = GetActionName(); 
                if(actionName != null) {
                    items.Add(new DesignerActionVerbItem(new DesignerVerb(GetActionName(), OnDockActionClick))); 
                }
                return items;
            }
 

            /// <devdoc> 
            ///      Called when this designer's 'DockInParent' or 'Undock' designer action 
            ///      has been clicked.
            /// </devdoc> 
            private void OnDockActionClick(object sender, EventArgs e) {
                DesignerVerb designerVerb = sender as DesignerVerb;
                if (designerVerb != null && _host != null) {
                    using (DesignerTransaction t = _host.CreateTransaction(designerVerb.Text)) { 
                        //set the dock prop to DockStyle.Fill
                        PropertyDescriptor dockProp = TypeDescriptor.GetProperties(Component)["Dock"]; 
                        DockStyle dockStyle = (DockStyle)dockProp.GetValue(Component); 
                        if(dockStyle == DockStyle.Fill) {
                            dockProp.SetValue(Component, DockStyle.None); 
                        } else {
                            dockProp.SetValue(Component, DockStyle.Fill);
                        }
                        t.Commit(); 
                    }
                } 
            } 

        } 

        /// <devdoc>
        ///     This TransparentBehavior is associated with the BodyGlyph for
        ///     this ControlDesigner.  When the BehaviorService hittests a glyph 
        ///     w/a TransparentBehavior, all messages will be passed through
        ///     the BehaviorService directly to the ControlDesigner. 
        ///     During a Drag operation, when the BehaviorService hittests 
        /// </devdoc>
        internal class TransparentBehavior : System.Windows.Forms.Design.Behavior.Behavior { 

            ControlDesigner designer;//the related ControlDesigner

            Rectangle controlRect = Rectangle.Empty;//the client rectangle of the related control in screen coordinates. Used to check if we can drop. 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.TransparentBehavior"]/*' /> 
            /// <devdoc> 
            ///     Constructor that accepts the related ControlDesigner.
            /// </devdoc> 
            internal TransparentBehavior(ControlDesigner designer) {
                this.designer = designer;
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.IsTransparent"]/*' />
            /// <devdoc> 
            ///     This property performs a hit test on the ControlDesigner 
            ///     to determine if the BodyGlyph should return '-1' for
            ///     hit testing (letting all messages pass directly to the 
            ///     the control).
            /// </devdoc>
            internal bool IsTransparent(Point p) {
                return designer.GetHitTest(p); 
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragDrop"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner.
            /// </devdoc>
            public override void OnDragDrop(Glyph g, DragEventArgs e) {
                controlRect = Rectangle.Empty; 
                designer.OnDragDrop(e);
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragEnter"]/*' />
            /// <devdoc> 
            ///     Forwards DragDrop notification from the BehaviorService to
            ///     the related ControlDesigner.
            /// </devdoc>
            public override void OnDragEnter(Glyph g, DragEventArgs e) { 
                if (designer != null && designer.Control != null) {
                    controlRect = designer.Control.RectangleToScreen(designer.Control.ClientRectangle); 
                } 

                designer.OnDragEnter(e); 
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragLeave"]/*' />
            /// <devdoc> 
            ///     Forwards DragDrop notification from the BehaviorService to
            ///     the related ControlDesigner. 
            /// </devdoc> 
            public override void OnDragLeave(Glyph g, EventArgs e) {
                controlRect = Rectangle.Empty; 
                designer.OnDragLeave(e);
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragOver"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner. 
            /// </devdoc>
            public override void OnDragOver(Glyph g, DragEventArgs e) { 
                // If we are not over a valid drop area, then do not allow the drag/drop

                //VSWhidbey# 364083. Now that all dragging/dropping is done via
                //the behavior service and adorner window, we have to do our own 
                //validation, and cannot rely on the OS to do it for us.
                if (e != null && controlRect != Rectangle.Empty && !controlRect.Contains(new Point(e.X, e.Y))) { 
                    e.Effect = DragDropEffects.None; 
                    return;
                } 

                designer.OnDragOver(e);
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragOver"]/*' />
            /// <devdoc> 
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner.
            /// </devdoc> 
            public override void OnGiveFeedback(Glyph g, GiveFeedbackEventArgs e) {
                designer.OnGiveFeedback(e);
            }
        } 

        private class CanResetSizePropertyDescriptor : PropertyDescriptor 
        { 

            private PropertyDescriptor    _basePropDesc; 

            public CanResetSizePropertyDescriptor (PropertyDescriptor pd) : base(pd)
            {
                this._basePropDesc = pd; 
            }
 
            public override Type ComponentType 
            {
                get 
                {
                    return _basePropDesc.ComponentType;
                }
            } 

            public override string DisplayName 
            { 
                get {
                    return _basePropDesc.DisplayName; 
                }
            }

            public override bool IsReadOnly 
            {
                get 
                { 
                    return _basePropDesc.IsReadOnly;
                } 
            }

            public override Type PropertyType
            { 
                get
                { 
                    return _basePropDesc.PropertyType; 
                }
            } 


            public override bool CanResetValue(object component)
            { 
                // VSWhidbey 379297 -- since we can't get to the DefaultSize property, we use the existing
                // ShouldSerialize logic. 
                return _basePropDesc.ShouldSerializeValue(component); 
            }
 
            public override object GetValue(object component)
            {
                return _basePropDesc.GetValue(component);
            } 

            public override void ResetValue(object component) 
            { 
                _basePropDesc.ResetValue(component);
            } 


            public override void SetValue(object component, object value)
            { 
                _basePropDesc.SetValue(component, value);
            } 
 
            public override bool ShouldSerializeValue(object component)
            { 
                // we always want to serialize values.
                return true;
            }
        } 

    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using Accessibility;
    using System.Runtime.Serialization.Formatters;
    using System.Threading;
    using System.Runtime.InteropServices; 
    using System.CodeDom;
    using System.ComponentModel; 
    using System.Diagnostics; 
    using System;
    using System.Security; 
    using System.Security.Permissions;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Serialization;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design.Behavior; 
    using System.Drawing;
    using System.Drawing.Design; 
    using Microsoft.Win32;
    using System.Configuration;
    using Timer = System.Windows.Forms.Timer;
    using System.Globalization; 
    using System.Diagnostics.CodeAnalysis;
 
    /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides a designer that can design components
    ///       that extend Control.</para>
    /// </devdoc>
    public class ControlDesigner : ComponentDesigner { 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InvalidPoint"]/*' /> 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 


        protected static readonly Point InvalidPoint = new Point(int.MinValue, int.MinValue);
        private static int currentProcessId; 

        private   IDesignerHost         host;           // the host for our designer 
        private   IDesignerTarget       designerTarget; // the target window proc for the control. 

        private   bool                  liveRegion;     // is the mouse is over a live region of the control? 
        private   bool                  inHitTest;      // A popular way to implement GetHitTest is by WM_NCHITTEST...which would cause a cycle.
        private   bool                  hasLocation;    // Do we have a location property?
        private   bool                  locationChecked;// And did we check it
        private   bool                  locked;         // signifies if this control is locked or not 
        private   bool                  initializing;
        private   bool                  enabledchangerecursionguard; 
 

        //Behavior work 
        private BehaviorService     behaviorService; //we cache this 'cause we use it so often
        private ResizeBehavior      resizeBehavior; //the standard behavior for our selection glyphs - demand created
        private ContainerSelectorBehavior moveBehavior; //the behavior for non-resize glyphs - demand created
 
        // Services that we use enough to cache
        // 
        private ISelectionUIService     selectionUISvc; 
        private IEventHandlerService    eventSvc;
        private IToolboxService         toolboxSvc; 
        private InheritanceUI           inheritanceUI;
        private IOverlayService         overlayService;

 

        // transient values that are used during mouse drags 
        // 
        private Point               mouseDragLast = InvalidPoint;   // the last position of the mouse during a drag.
        private bool                mouseDragMoved;                 // has the mouse been moved during this drag? 
        private int                 lastMoveScreenX;
        private int                 lastMoveScreenY;

        // Values used to simulate double clicks for controls that don't support them. 
        //
        private int lastClickMessageTime; 
        private int lastClickMessagePositionX; 
        private int lastClickMessagePositionY;
 
        private Point                         downPos = Point.Empty;     // point used to track first down of a double click
        private event EventHandler            disposingHandler;
        private CollectionChangeEventHandler  dataBindingsCollectionChanged;
        private Exception                     thrownException; 
        private bool                          ctrlSelect;                // if the CTRL key was down at the mouse down
        private bool                          toolPassThrough;           // a tool is selected, allow the parent to draw a rect for it. 
        private bool                          removalNotificationHooked = false; 
        private bool                          revokeDragDrop = true;
        private bool                          hadDragDrop; 

        private DesignerControlCollection     controls;

        private static bool                   inContextMenu = false; 

        private DockingActionList             dockingAction; 
        private StatusCommandUI               statusCommandUI;   // UI for setting the StatusBar Information.. 

        private bool                          forceVisible = true; 

        private bool                          autoResizeHandles = false; // used for disabling AutoSize effect on resize modes. Needed for compat.

        private Dictionary<IntPtr, bool>      subclassedChildren; 

 
        /// <devdoc> 
        ///     Accessor for AllowDrop.  Since we often turn this on, we shadow it
        ///     so it doesn't show up to the user. 
        /// </devdoc>
        private bool AllowDrop {
            get {
                return (bool)ShadowProperties["AllowDrop"]; 
            }
            set { 
                ShadowProperties["AllowDrop"] = value; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.BehaviorService"]/*' />
        protected BehaviorService BehaviorService {
            get { 
                if (behaviorService == null) {
                    behaviorService = (BehaviorService)GetService(typeof(BehaviorService)); 
                } 
                return behaviorService;
            } 
        }

        internal bool ForceVisible {
            get { 
                return forceVisible;
            } 
 
            set {
                forceVisible = value; 
            }
        }

        private Dictionary<IntPtr, bool> SubclassedChildWindows { 
            get {
                if (subclassedChildren == null) { 
                    subclassedChildren = new Dictionary<IntPtr, bool>(); 
                }
 
                return subclassedChildren;
            }
        }
 
        private IOverlayService OverlayService {
            get { 
                if (overlayService == null) { 
                     overlayService = (IOverlayService)GetService(typeof(IOverlayService));
                } 
                return overlayService;
            }
        }
 
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        private DesignerControlCollection Controls { 
            get { 
                if (controls == null) {
                    controls = new DesignerControlCollection(Control); 
                }
                return controls;
            }
        } 

        private Point Location { 
            get { 
                Point loc = Control.Location;
 
                ScrollableControl p = Control.Parent as ScrollableControl;
                if (p != null) {
                    Point pt = p.AutoScrollPosition;
                    loc.Offset(-pt.X, -pt.Y); 
                }
                return loc; 
            } 
            set {
                ScrollableControl p = Control.Parent as ScrollableControl; 
                if (p != null) {
                    Point pt = p.AutoScrollPosition;
                    value.Offset(pt.X, pt.Y);
                } 
                Control.Location = value;
            } 
        } 

 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.AssociatedComponents"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Retrieves a list of associated components.  These are components that should be incluced in a cut or copy operation on this component.
        ///    </para> 
        /// </devdoc> 
        public override ICollection AssociatedComponents {
            get { 
                ArrayList sitedChildren = null;

                foreach (Control c in Control.Controls) {
                    if (c.Site != null) { 
                        if (sitedChildren == null) {
                            sitedChildren = new ArrayList(); 
                        } 
                        sitedChildren.Add(c);
                    } 
                }

                if (sitedChildren != null) {
                    return sitedChildren; 
                }
                return base.AssociatedComponents; 
            } 
        }
 
        /// <devdoc>
        ///     Accessor method for the context menu property on control.  We shadow
        ///     this property at design time.
        /// </devdoc> 
        private ContextMenu ContextMenu {
            get { 
                return (ContextMenu)ShadowProperties["ContextMenu"]; 
            }
            set { 
                ContextMenu oldValue = (ContextMenu)ShadowProperties["ContextMenu"];

                if (oldValue != value) {
                    EventHandler disposedHandler = new EventHandler(DetachContextMenu); 

                    if (oldValue != null) { 
                        oldValue.Disposed -= disposedHandler; 
                    }
 
                    ShadowProperties["ContextMenu"] = value;

                    if (value != null) {
                        value.Disposed += disposedHandler; 
                    }
                } 
 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.accessibilityObj"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        protected AccessibleObject accessibilityObj = null; 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.AccessibilityObject"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual AccessibleObject AccessibilityObject {
            get { 
                if (accessibilityObj == null) {
                    accessibilityObj = new ControlDesignerAccessibleObject(this, Control); 
                } 
                return accessibilityObj;
            } 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Control"]/*' />
        /// <devdoc> 
        ///     Retrieves the control we're designing.
        /// </devdoc> 
        public virtual Control Control { 
            get {
                return(Control)Component; 
            }
        }

        private IDesignerTarget DesignerTarget { 
            get {
                return designerTarget; 
            } 
            set {
                this.designerTarget = value; 
            }
        }

        /// <devdoc> 
        ///     Accessor method for the enabled property on control.  We shadow
        ///     this property at design time. 
        /// </devdoc> 
        private bool Enabled {
            get { 
                return (bool)ShadowProperties["Enabled"];
            }
            set {
                ShadowProperties["Enabled"] = value; 
            }
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.EnableDragRect"]/*' />
        /// <devdoc> 
        ///     Determines whether drag rects can be drawn on this designer.
        /// </devdoc>
        protected virtual bool EnableDragRect {
            get { 
                return false;
            } 
        } 

        /// <devdoc> 
        ///     Gets / Sets this controls locked property
        ///
        /// </devdoc>
        private bool Locked { 
            get {
                return locked; 
            } 

            set{ 
                if (locked != value) {
                    locked = value;
                }
            } 
        }
 
        private string Name { 
            get {
                return Component.Site.Name; 
            }
            set {
                // don't do anything here during loading, if a refactor changed it we don't want to do anything
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
                if(host == null || (host != null && !host.Loading)) {
                    Component.Site.Name = value; 
                } 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ParentComponent"]/*' />
        /// <devdoc>
        ///     Returns the parent component for this control designer. 
        ///     The default implementation just checks to see if the
        ///     component being designed is a control, and if it is it 
        ///     returns its parent.  This property can return null if there 
        ///     is no parent component.
        /// </devdoc> 
        protected override IComponent ParentComponent {
            get {
                Control c = Component as Control;
                if (c != null && c.Parent != null) { 
                    return c.Parent;
                } 
                return base.ParentComponent; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ParticipatesWidthSnapLines"]/*' />
        /// <devdoc>
        ///     Determines whether or not the ControlDesigner will allow SnapLine alignment during a 
        ///     drag operation when the primary drag control is over this designer, or when a control
        ///     is being dragged from the toolbox, or when a control is being drawn through click-drag. 
        /// 
        /// </devdoc>
        public virtual bool ParticipatesWithSnapLines { 
            get {
                return true;
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.NumberOfInternalControlDesigners"]/*' /> 
        /// <devdoc> 
        ///     Returns the number of internal control designers in the ControlDesigner. An internal control
        ///     is a control that is not in the IDesignerHost.Container.Components collection. 
        ///     SplitterPanel is an example of one such control. We use this to get SnapLines for the internal
        ///     control designers.
        /// </devdoc>
        public virtual int NumberOfInternalControlDesigners() { 
            return 0;
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InternalControlDesigner"]/*' />
        /// <devdoc> 
        ///     Returns the internal control designer with the specified index in the ControlDesigner. An internal control
        ///     is a control that is not in the IDesignerHost.Container.Components collection.
        ///     SplitterPanel is an example of one such control.
        /// 
        ///     internalControlIndex is zero-based.
        /// </devdoc> 
        public virtual ControlDesigner InternalControlDesigner(int internalControlIndex) { 
            return null;
        } 

        /// <devdoc>
        ///  Per AutoSize spec, determines if a control is resizable.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        private bool IsResizableConsiderAutoSize(PropertyDescriptor autoSizeProp, PropertyDescriptor autoSizeModeProp) { 
            object component = Component; 

            bool resizable = true; 
            bool autoSize = false;
            bool growOnly = false;

            if (autoSizeProp != null && 
                !(autoSizeProp.Attributes.Contains(DesignerSerializationVisibilityAttribute.Hidden) ||
                  autoSizeProp.Attributes.Contains(BrowsableAttribute.No))) { 
                autoSize = (bool) autoSizeProp.GetValue(component); 
            }
 
            if (autoSizeModeProp != null) {
                AutoSizeMode mode = (AutoSizeMode) autoSizeModeProp.GetValue(component);
                growOnly = mode == AutoSizeMode.GrowOnly;
            } 

            if (autoSize) { 
                resizable = growOnly; 
            }
 
            return resizable;

        }
 
        /// <devdoc>
        /// 
        /// 
        /// </devdoc>
        public bool AutoResizeHandles { 
            get {
                return autoResizeHandles;
            }
            set { 
                autoResizeHandles = value;
            } 
        } 

 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.SelectionRules"]/*' />
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public virtual SelectionRules SelectionRules { 
            get {
                SelectionRules rules = SelectionRules.Visible; 
                object component = Component;

                rules = SelectionRules.Visible;
 
                PropertyDescriptor prop;
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(component); 
 
                PropertyDescriptor autoSizeProp = props["AutoSize"];
                PropertyDescriptor autoSizeModeProp = props["AutoSizeMode"]; 

                if ((prop = props["Location"]) != null &&
                    !prop.IsReadOnly) {
                    rules |= SelectionRules.Moveable; 
                }
 
                if ((prop = props["Size"]) != null && !prop.IsReadOnly) { 

                    if (AutoResizeHandles && this.Component != host.RootComponent) { 
                        rules = IsResizableConsiderAutoSize(autoSizeProp, autoSizeModeProp) ? rules | SelectionRules.AllSizeable : rules;
                    }
                    else {
                        rules |= SelectionRules.AllSizeable; 
                    }
                } 
 
                PropertyDescriptor propDock = props["Dock"];
                if (propDock != null) { 
                    DockStyle dock = (DockStyle)(int)propDock.GetValue(component);
                    //gotta adjust if the control's parent is mirrored...
                    //this is just such that we add the right resize handles.
                    //We need to do it this way, since resize glyphs are added in 
                    //AdornerWindow coords, and the AdornerWindow is never mirrored.
                    if (Control.Parent != null && Control.Parent.IsMirrored) { 
                        if (dock == DockStyle.Left) { 
                            dock = DockStyle.Right;
                        } 
                        else if (dock == DockStyle.Right) {
                            dock = DockStyle.Left;
                        }
                    } 
                    switch (dock) {
                        case DockStyle.Top: 
                            rules &= ~(SelectionRules.Moveable | SelectionRules.TopSizeable | SelectionRules.LeftSizeable | SelectionRules.RightSizeable); 
                            break;
                        case DockStyle.Left: 
                            rules &= ~(SelectionRules.Moveable | SelectionRules.TopSizeable | SelectionRules.LeftSizeable | SelectionRules.BottomSizeable);
                            break;
                        case DockStyle.Right:
                            rules &= ~(SelectionRules.Moveable | SelectionRules.TopSizeable | SelectionRules.BottomSizeable | SelectionRules.RightSizeable); 
                            break;
                        case DockStyle.Bottom: 
                            rules &= ~(SelectionRules.Moveable | SelectionRules.LeftSizeable | SelectionRules.BottomSizeable | SelectionRules.RightSizeable); 
                            break;
                        case DockStyle.Fill: 
                            rules &= ~(SelectionRules.Moveable | SelectionRules.TopSizeable | SelectionRules.LeftSizeable | SelectionRules.RightSizeable | SelectionRules.BottomSizeable);
                            break;
                    }
                } 

                PropertyDescriptor pd = props["Locked"]; 
                if (pd != null) { 
                    Object value = pd.GetValue(component);
 
                    // make sure that value is a boolean, in case someone else added this property
                    //
                    if (value is bool && (bool)value == true) {
                        rules = SelectionRules.Locked | SelectionRules.Visible; 
                    }
                } 
 
                return rules;
            } 
        }

        // This boolean indicates whether the Control will allow SnapLines to be shown when any other targetControl is dragged on the design surface.
        // This is true by default. 
        internal virtual bool ControlSupportsSnaplines
        { 
            get 
            {
                return true; 
            }
        }

 

        /// <devdoc> 
        /// 
        /// Used when adding snaplines
        /// 
        /// In order to add padding, we need to get the offset from the usable client area of our control
        /// and the actual origin of our control.  In other words: how big is the non-client area here?
        /// Ex: we want to add padding on a form to the insides of the borders and below the titlebar.
        /// 
        internal Point GetOffsetToClientArea() {
            NativeMethods.POINT nativeOffset = new NativeMethods.POINT(0,0); 
            NativeMethods.MapWindowPoints(Control.Handle, Control.Parent.Handle, nativeOffset, 1); 

            Point offset = Control.Location; 
            // If the 2 controls do not have the same orientation, then force one
            // to make sure we calculate the correct offset
            if (Control.IsMirrored != Control.Parent.IsMirrored) {
                offset.Offset(Control.Width,0); 
            }
            return (new Point(Math.Abs(nativeOffset.x - offset.X), nativeOffset.y - offset.Y)); 
        } 

 
        internal IList SnapLinesInternal() {
            return SnapLinesInternal(Control.Margin);
        }
 
        /// <devdoc>
        ///     We separate this from the SnapLines property so that other designers 
        ///     can call this directly. E.g. SplitContainerDesigner would inherit 
        ///     SnapLines from ParentControlDesigner which overrides SnapLines.
        ///     But ParentControlDesigner.SnapLines also adds padding SnapLines, 
        ///     which we don't want for the SplitContainerDesigner.
        /// </devdoc>
        internal IList SnapLinesInternal(Padding margin) {
            ArrayList snapLines = new ArrayList(4); 
            int width = Control.Width; // better perf
            int height = Control.Height; // better perf 
 

 
            //the four edges of our control
            snapLines.Add( new SnapLine(SnapLineType.Top, 0, SnapLinePriority.Low));
            snapLines.Add( new SnapLine(SnapLineType.Bottom, height-1, SnapLinePriority.Low));
            snapLines.Add( new SnapLine(SnapLineType.Left, 0, SnapLinePriority.Low)); 
            snapLines.Add( new SnapLine(SnapLineType.Right, width-1, SnapLinePriority.Low));
 
            //the four margins of our control 

 
            // Even if a control does not have margins, we still want to add Margin snaplines.
            // This is because we only try to match to matching snaplines. Makes the code a little easier...
            snapLines.Add( new SnapLine(SnapLineType.Horizontal, -margin.Top, SnapLine.MarginTop, SnapLinePriority.Always));
            snapLines.Add( new SnapLine(SnapLineType.Horizontal, margin.Bottom + height, SnapLine.MarginBottom, SnapLinePriority.Always)); 
            snapLines.Add( new SnapLine(SnapLineType.Vertical, -margin.Left, SnapLine.MarginLeft, SnapLinePriority.Always));
            snapLines.Add( new SnapLine(SnapLineType.Vertical, margin.Right + width, SnapLine.MarginRight, SnapLinePriority.Always)); 
 

            return snapLines; 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.SnapLines"]/*' />
        /// <devdoc> 
        ///     Returns a list of SnapLine objects representing interesting
        ///     alignment points for this control.  These SnapLines are used 
        ///     to assist in the positioning of the control on a parent's 
        ///     surface.
        /// </devdoc> 
        public virtual IList SnapLines {
            get {
                return SnapLinesInternal();
            } 
        }
 
        /// <devdoc> 
        ///     Demand creates the StandardBehavior related to this
        ///     ControlDesigner.  This is used to associate the designer's 
        ///     selection glyphs to a common Behavior (resize in this case).
        /// </devdoc>
        internal virtual System.Windows.Forms.Design.Behavior.Behavior StandardBehavior {
            get { 
                if (resizeBehavior == null) {
                    resizeBehavior = new ResizeBehavior(Component.Site); 
                } 
                return resizeBehavior;
            } 
        }


        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.SerializePerformLayout"]/*' /> 
        /// <devdoc>
        ///     Refer VsWhidbey : 487804. There are certain containers (like ToolStrip) that require PerformLayout to be serialized in the code gen. 
        /// </devdoc> 
        internal virtual bool SerializePerformLayout {
            get { 
                return false;
            }
        }
 
        internal System.Windows.Forms.Design.Behavior.Behavior MoveBehavior {
            get { 
                if (moveBehavior == null) { 
                    moveBehavior = new ContainerSelectorBehavior(Control, Component.Site);
                } 
                return moveBehavior;
            }
        }
 
        /// <devdoc>
        ///     Accessor method for the visible property on control.  We shadow 
        ///     this property at design time. 
        /// </devdoc>
        private bool Visible { 
            get {
                return (bool)ShadowProperties["Visible"];
            }
            set { 
                ShadowProperties["Visible"] = value;
            } 
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InheritanceAttribute"]/*' /> 
        protected override InheritanceAttribute InheritanceAttribute {
            get {
                if(IsRootDesigner) {
                    return InheritanceAttribute.Inherited; 
                }
                return base.InheritanceAttribute; 
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.BaseWndProc"]/*' />
        /// <devdoc>
        ///     Default processing for messages.  This method causes the message to
        ///     get processed by windows, skipping the control.  This is useful if 
        ///     you want to block this message from getting to the control, but
        ///     you do not want to block it from getting to Windows itself because 
        ///     it causes other messages to be generated. 
        /// </devdoc>
        protected void BaseWndProc(ref Message m) { 
            m.Result = NativeMethods.DefWindowProc(m.HWnd, m.Msg, m.WParam, m.LParam);
        }

        internal override bool CanBeAssociatedWith(IDesigner parentDesigner) 
        {
            return CanBeParentedTo(parentDesigner); 
        } 

         /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.CanBeParentedTo"]/*' /> 
         /// <devdoc>
        ///     Determines if the this designer can be parented to the specified desinger --
        ///     generally this means if the control for this designer can be parented into the
        ///     given ParentControlDesigner's designer. 
        /// </devdoc>
        public virtual bool CanBeParentedTo(IDesigner parentDesigner) { 
           ParentControlDesigner p = parentDesigner as ParentControlDesigner; 
           return (p != null && !Control.Contains(p.Control));
        } 

        private void DataBindingsCollectionChanged(object sender, CollectionChangeEventArgs e) {

            // It is possible to use the control designer with NON CONTROl types. 
            //
            Control ctl = Component as Control; 
 
            if (ctl != null) {
                if (ctl.DataBindings.Count == 0 && removalNotificationHooked) { 
                    // remove the notification for the ComponentRemoved event
                    IComponentChangeService csc = (IComponentChangeService) GetService(typeof(IComponentChangeService));
                    if (csc != null) {
                        csc.ComponentRemoved -= new ComponentEventHandler(DataSource_ComponentRemoved); 
                    }
                    removalNotificationHooked = false; 
                } 
                else if (ctl.DataBindings.Count > 0 && !removalNotificationHooked) {
                    // add he notification for the ComponentRemoved event 
                    IComponentChangeService csc = (IComponentChangeService) GetService(typeof(IComponentChangeService));
                    if (csc != null) {
                        csc.ComponentRemoved += new ComponentEventHandler(DataSource_ComponentRemoved);
                    } 
                    removalNotificationHooked = true;
                } 
            } 
        }
 
        private void DataSource_ComponentRemoved(object sender, ComponentEventArgs e) {

            // It is possible to use the control designer with NON CONTROl types.
            // 
            Control ctl = Component as Control;
 
            if (ctl != null) { 
                Debug.Assert(ctl.DataBindings.Count > 0, "we should not be notified if the control has no dataBindings");
 
                ctl.DataBindings.CollectionChanged -= dataBindingsCollectionChanged;
                for (int i = 0; i < ctl.DataBindings.Count; i ++) {
                    Binding binding = ctl.DataBindings[i];
                    if (binding.DataSource == e.Component) { 
                        // remove the binding from the control's collection
                        // this will also remove the binding from the bindingManagerBase's bindingscollection 
                        // NOTE: we can't remove the bindingManager from the bindingContext, cause there may 
                        // be some complex bound controls ( such as the dataGrid, or the ComboBox, or the ListBox )
                        // that still use that bindingManager 
                        ctl.DataBindings.Remove(binding);
                    }
                }
 
                // if after removing those bindings the collection is empty, then
                // unhook the changeNotificationService 
                // 
                if (ctl.DataBindings.Count == 0) {
                    IComponentChangeService csc = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
                    if (csc != null) {
                        csc.ComponentRemoved -= new ComponentEventHandler(DataSource_ComponentRemoved);
                    }
                    removalNotificationHooked = false; 
                }
                ctl.DataBindings.CollectionChanged += dataBindingsCollectionChanged; 
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DefWndProc"]/*' />
        /// <devdoc>
        ///     Default processing for messages.  This method causes the message to
        ///     get processed by the control, rather than the designer. 
        /// </devdoc>
        protected void DefWndProc(ref Message m) { 
            designerTarget.DefWndProc(ref m); 
        }
 
        private void DetachContextMenu(object sender, EventArgs e) {
            ContextMenu = null;
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DisplayError"]/*' />
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
                RTLAwareMessageBox.Show(Control, message, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1, 0);
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Dispose"]/*' /> 
        /// <devdoc> 
        ///      Disposes of this object.
        /// </devdoc> 
        protected override void Dispose(bool disposing) {

            if (disposing) {
 
                if (Control != null) {
 
                    if (dataBindingsCollectionChanged != null) { 
                        Control.DataBindings.CollectionChanged -= dataBindingsCollectionChanged;
                    } 

                    if (Inherited && inheritanceUI != null) {
                        inheritanceUI.RemoveInheritedControl(Control);
                    } 

                    if (removalNotificationHooked) { 
                        IComponentChangeService csc = (IComponentChangeService) GetService(typeof(IComponentChangeService)); 
                        if (csc != null) {
                            csc.ComponentRemoved -= new ComponentEventHandler(DataSource_ComponentRemoved); 
                        }
                        removalNotificationHooked = false;
                    }
 
                    if (disposingHandler != null) {
                        disposingHandler(this, EventArgs.Empty); 
                    } 

                    UnhookChildControls(Control); 
                }

                if (ContextMenu != null) {
                    ContextMenu.Disposed -= new EventHandler(this.DetachContextMenu); 
                }
 
                if (designerTarget != null) { 
                    designerTarget.Dispose();
                } 

                downPos = Point.Empty;

                Control.ControlAdded -= new ControlEventHandler(OnControlAdded); 
                Control.ControlRemoved -= new ControlEventHandler(OnControlRemoved);
                Control.ParentChanged -= new EventHandler(OnParentChanged); 
                Control.SizeChanged -= new EventHandler(OnSizeChanged); 
                Control.LocationChanged -= new EventHandler(OnLocationChanged);
                Control.EnabledChanged -= new EventHandler(OnEnabledChanged); 
            }

            base.Dispose(disposing);
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.EnableDesignTime"]/*' /> 
        /// <devdoc> 
        ///     Enables design time functionality for a child control.  The child control is a child
        ///     of this control designer's control.  The child does not directly participate in 
        ///     persistence, but it will if it is exposed as a property of the main control.  Consider
        ///     a control like the SplitContainer:  it has two panels, Panel1 and Panel2.  These panels
        ///     are exposed through read only Panel1 and Panel2 properties on the SplitContainer class.
        ///     SplitContainer's designer calls EnableDesignTime for each panel, which allows other 
        ///     components to be dropped on them.  But, in order for the contents of Panel1 and Panel2
        ///     to be saved, SplitContainer itself needed to expose the panels as public properties. 
        /// 
        ///     The child paramter is the control to enable.  The name paramter is the name of this
        ///     control as exposed to the end user.  Names need to be unique within a control designer, 
        ///     but do not have to be unique to other control designer's children.
        ///
        ///     This method returns true if the child control could be enabled for design time, or
        ///     false if the hosting infrastructure does not support it.  To support this feature, the 
        ///     hosting infrastructure must expose the INestedContainer class as a service off of the site.
        /// </devdoc> 
        protected bool EnableDesignMode(Control child, string name) { 
            if (child == null) {
                throw new ArgumentNullException("child"); 
            }

            if (name == null) {
                throw new ArgumentNullException("name"); 
            }
 
            INestedContainer nc = GetService(typeof(INestedContainer)) as INestedContainer; 
            if (nc == null) {
                return false; 
            }

            // Only add the child if it doesn't already exist. VSWhidbey #408041.
            for (int i = 0; i < nc.Components.Count; i++) { 
                if (nc.Components[i].Equals(child)) {
                    return true; 
                } 
            }
 
            nc.Add(child, name);

            return true;
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.EnableDragDrop"]/*' /> 
        /// <devdoc> 
        ///      Enables or disables drag/drop support.  This
        ///      hooks drag event handlers to the control. 
        /// </devdoc>
        protected void EnableDragDrop(bool value) {
            Control rc = Control;
 
            if (rc == null) {
                return; 
            } 

            if (value) { 
                rc.DragDrop += new DragEventHandler(this.OnDragDrop);
                rc.DragOver += new DragEventHandler(this.OnDragOver);
                rc.DragEnter += new DragEventHandler(this.OnDragEnter);
                rc.DragLeave += new EventHandler(this.OnDragLeave); 
                rc.GiveFeedback += new GiveFeedbackEventHandler(this.OnGiveFeedback);
                hadDragDrop = rc.AllowDrop; 
                if (!hadDragDrop) { 
                    rc.AllowDrop = true;
                } 
                revokeDragDrop = false;
            }
            else {
                rc.DragDrop -= new DragEventHandler(this.OnDragDrop); 
                rc.DragOver -= new DragEventHandler(this.OnDragOver);
                rc.DragEnter -= new DragEventHandler(this.OnDragEnter); 
                rc.DragLeave -= new EventHandler(this.OnDragLeave); 
                rc.GiveFeedback -= new GiveFeedbackEventHandler(this.OnGiveFeedback);
                if (!hadDragDrop) { 
                    rc.AllowDrop = false;
                }
                revokeDragDrop = true;
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetComponentGlyph"]/*' /> 
        /// <devdoc>
        ///     Returns a 'BodyGlyph' representing the bounds of this control. 
        ///     The BodyGlyph is responsible for hit testing the related CtrlDes
        ///     and forwarding messages directly to the designer.
        /// </devdoc>
        protected virtual ControlBodyGlyph GetControlGlyph(GlyphSelectionType selectionType) { 

            //get the right cursor for this component 
            OnSetCursor(); 
            Cursor cursor = Cursor.Current;
 
            //get the correctly translated bounds
            Rectangle translatedBounds = BehaviorService.ControlRectInAdornerWindow(Control);

            //create our glyph, and set its cursor appropriately 

            ControlBodyGlyph g = null; 
 
            Control parent = Control.Parent;
 
            if (parent != null && host != null && host.RootComponent != Component) {

                Rectangle parentRect = parent.RectangleToScreen(parent.ClientRectangle);
                Rectangle controlRect = Control.RectangleToScreen(Control.ClientRectangle); 

                if (!parentRect.Contains(controlRect) && !parentRect.IntersectsWith(controlRect)) { 
                    //since the parent is completely clipping the control, the control cannot be a 
                    //drop target, and it will not get mouse messages. So we don't have to give
                    //the glyph a transparentbehavior (default for ControlBodyGlyph). But we still 
                    //would like to be able to move the control, so push a MoveBehavior. If we didn't
                    //we wouldn't be able to move the control, since it won't get any mouse messages.
                    //VS Whidbey# 344888
 
                    //VS Whidbey# 399801 - but only do so if the control is selected.
 
                    ISelectionService sel = (ISelectionService)GetService(typeof(ISelectionService)); 
                    if (sel != null && sel.GetComponentSelected(Control)) {
                        g = new ControlBodyGlyph(translatedBounds, cursor, Control, MoveBehavior); 
                    }
                    else if (cursor == Cursors.SizeAll) {
                        //If we get here, OnSetCursor could have set the cursor to SizeAll. But if we fall
                        //into this category, we don't have a MoveBehavior, so we don't want to show the 
                        //SizeAll cursor. Let's make sure the cursor is set to the default cursor.
                        cursor = Cursors.Default; 
                    } 

                } 
            }

            if (g == null) {
                //we are not totally clipped by the parent 
                g = new ControlBodyGlyph(translatedBounds, cursor, Control, this);
            } 
 
            return g;
        } 

        internal ControlBodyGlyph GetControlGlyphInternal(GlyphSelectionType selectionType) {
            return GetControlGlyph(selectionType);
        } 

 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetGlyphs"]/*' /> 
        /// <devdoc>
        ///     Returns a collection of Glyph objects representing the selection 
        ///     borders and grab handles for a standard control.  Note that
        ///     based on 'selectionType' the Glyphs returned will either: represent
        ///     a fully resizeable selection border with grab handles, a locked
        ///     selection border, or a single 'hidden' selection Glyph. 
        /// </devdoc>
        public virtual GlyphCollection GetGlyphs(GlyphSelectionType selectionType) { 
 
            GlyphCollection glyphs = new GlyphCollection();
 
            if (selectionType != GlyphSelectionType.NotSelected) {

                Rectangle translatedBounds = BehaviorService.ControlRectInAdornerWindow(Control);
 
                bool primarySelection = (selectionType == GlyphSelectionType.SelectedPrimary);
 
                SelectionRules rules = SelectionRules; 

                if ((Locked) || (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly)) { 
                    // the lock glyph
                    glyphs.Add(new LockedHandleGlyph(translatedBounds, primarySelection));

                    //the four locked border glyphs 
                    glyphs.Add(new LockedBorderGlyph(translatedBounds, SelectionBorderGlyphType.Top));
                    glyphs.Add(new LockedBorderGlyph(translatedBounds, SelectionBorderGlyphType.Bottom)); 
                    glyphs.Add(new LockedBorderGlyph(translatedBounds, SelectionBorderGlyphType.Left)); 
                    glyphs.Add(new LockedBorderGlyph(translatedBounds, SelectionBorderGlyphType.Right));
                } 
                else if ((rules & SelectionRules.AllSizeable) == SelectionRules.None) {
                    //the non-resizeable grab handle
                    glyphs.Add(new NoResizeHandleGlyph(translatedBounds, rules, primarySelection, MoveBehavior));
 
                    //the four resizeable border glyphs
                    glyphs.Add(new NoResizeSelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Top, MoveBehavior)); 
                    glyphs.Add(new NoResizeSelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Bottom, MoveBehavior)); 
                    glyphs.Add(new NoResizeSelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Left, MoveBehavior));
                    glyphs.Add(new NoResizeSelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Right, MoveBehavior)); 
                    // enable the designeractionpanel for this control if it needs one
                    if (TypeDescriptor.GetAttributes(Component).Contains(DesignTimeVisibleAttribute.Yes) && behaviorService.DesignerActionUI != null)  {
                        Glyph dapGlyph = behaviorService.DesignerActionUI.GetDesignerActionGlyph(Component);
                        if(dapGlyph!=null) { 
                            glyphs.Insert(0, dapGlyph); //we WANT to be in front of the other UI
                        } 
                    } 
                }
                else { 
                    //grab handles
                    if ((rules & SelectionRules.TopSizeable) != 0) {
                        glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.MiddleTop, StandardBehavior, primarySelection));
                        if ((rules & SelectionRules.LeftSizeable) != 0) { 
                            glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.UpperLeft, StandardBehavior, primarySelection));
                        } 
                        if ((rules & SelectionRules.RightSizeable) != 0) { 
                            glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.UpperRight, StandardBehavior, primarySelection));
                        } 
                    }

                    if ((rules & SelectionRules.BottomSizeable) != 0) {
                        glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.MiddleBottom, StandardBehavior, primarySelection)); 
                        if ((rules & SelectionRules.LeftSizeable) != 0) {
                            glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.LowerLeft, StandardBehavior, primarySelection)); 
                        } 
                        if ((rules & SelectionRules.RightSizeable) != 0) {
                            glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.LowerRight, StandardBehavior, primarySelection)); 
                        }
                    }

                    if ((rules & SelectionRules.LeftSizeable) != 0) { 
                        glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.MiddleLeft, StandardBehavior, primarySelection));
                    } 
 
                    if ((rules & SelectionRules.RightSizeable) != 0) {
                        glyphs.Add(new GrabHandleGlyph(translatedBounds, GrabHandleGlyphType.MiddleRight, StandardBehavior, primarySelection)); 
                    }

                    //the four resizeable border glyphs
                    glyphs.Add(new SelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Top, StandardBehavior)); 
                    glyphs.Add(new SelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Bottom, StandardBehavior));
                    glyphs.Add(new SelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Left, StandardBehavior)); 
                    glyphs.Add(new SelectionBorderGlyph(translatedBounds, rules, SelectionBorderGlyphType.Right, StandardBehavior)); 

                    // enable the designeractionpanel for this control if it needs one 
                    if (TypeDescriptor.GetAttributes(Component).Contains(DesignTimeVisibleAttribute.Yes) && behaviorService.DesignerActionUI != null)  {
                        Glyph dapGlyph = behaviorService.DesignerActionUI.GetDesignerActionGlyph(Component);
                        if(dapGlyph!=null) {
                            glyphs.Insert(0,dapGlyph); //we WANT to be in front of the other UI 
                        }
                    } 
                } 
            }
            return glyphs; 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Allows your component to support a design time user interface.  A TabStrip
        ///     control, for example, has a design time user interface that allows the user 
        ///     to click the tabs to change tabs.  To implement this, TabStrip returns 
        ///     true whenever the given point is within its tabs.
        /// </devdoc> 
        protected virtual bool GetHitTest(Point point) {
            return false;
        }
 
        /// <devdoc>
        ///     Given an LParam as a parameter, this extracts a point in parent 
        ///     coordinates. 
        /// </devdoc>
        private int GetParentPointFromLparam(IntPtr lParam) { 
            Point pt = new Point(NativeMethods.Util.SignedLOWORD((int)lParam), NativeMethods.Util.SignedHIWORD((int)lParam));
            pt = Control.PointToScreen(pt);
            pt = Control.Parent.PointToClient(pt);
            return NativeMethods.Util.MAKELONG(pt.X, pt.Y); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.HookChildControls"]/*' /> 
        /// <devdoc>
        ///     Hooks the children of the given control.  We need to do this for 
        ///     child controls that are not in design mode, which is the case
        ///     for composite controls.
        /// </devdoc>
        protected void HookChildControls(Control firstChild) { 

            foreach(Control child in firstChild.Controls) { 
                if (child != null && host != null) { 
                    if (!(host.GetDesigner(child) is ControlDesigner)) {
 
                        // No, no designer means we must replace the window target in this
                        // control.
                        //
                        IWindowTarget oldTarget = child.WindowTarget; 

                        if (!(oldTarget is ChildWindowTarget)) { 
                            child.WindowTarget = new ChildWindowTarget(this, child, oldTarget); 
                        }
 
                        // ASURT 45655: Some controls (primarily RichEdit) will register themselves as
                        // drag-drop source/targets when they are instantiated. We have to RevokeDragDrop()
                        // for them so that the ParentControlDesigner()'s drag-drop support can work
                        // correctly. Normally, the hwnd for the child control is not created at this time, 
                        // and we will use the WM_CREATE message in ChildWindowTarget's WndProc() to revoke
                        // drag-drop. But, if the handle was already created for some reason, we will need 
                        // to revoke drag-drop right away. 
                        //
                        if (child.IsHandleCreated) { 
                            Application.OleRequired();
                            NativeMethods.RevokeDragDrop(child.Handle);
                            HookChildHandles(child.Handle);
                        } 
                        else {
                            child.HandleCreated += delegate(object sender, EventArgs e) { 
                                HookChildHandles(child.Handle); 
                            };
                        } 

                        // We only hook the children's children if there was no designer.
                        // We leave it up to the designer to hook its own children.
                        // 
                        HookChildControls(child);
                    } 
                } 
            }
        } 

        private int CurrentProcessId {
            get {
                if (currentProcessId == 0) { 
                    currentProcessId = SafeNativeMethods.GetCurrentProcessId();
                } 
                return currentProcessId; 
            }
        } 

        private bool IsWindowInCurrentProcess(IntPtr hwnd) {
            int pid;
            UnsafeNativeMethods.GetWindowThreadProcessId(new HandleRef(null, hwnd), out pid); 

            return pid == CurrentProcessId; 
        } 

        /// <devdoc> 
        ///     Hooks the peer handles of the given child control.  We need
        ///     to do this to handle windows that are not associated with
        ///     a control (such as the combo box edit), and for controls
        ///     that are not in design mode (such as child controls on a 
        ///     user control).
        /// </devdoc> 
 
        [SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults")]
        internal void HookChildHandles(IntPtr firstChild) { 
            IntPtr hwndChild = firstChild;

            while (hwndChild != IntPtr.Zero) {
 
                if (!IsWindowInCurrentProcess(hwndChild)) {
                    break; 
                } 

                // Is it a control? 
                //
                Control child = Control.FromHandle(hwndChild);
                if (child == null) {
                    // No control.  We must subclass this control. 
                    //
                    if (!SubclassedChildWindows.ContainsKey(hwndChild)) { 
                        // ASURT 45655: Some controls (primarily RichEdit) will register themselves as 
                        // drag-drop source/targets when they are instantiated. Since these hwnds do not
                        // have a Windows Forms control associated with them, we have to RevokeDragDrop() 
                        // for them so that the ParentControlDesigner()'s drag-drop support can work
                        // correctly.
                        //
                        NativeMethods.RevokeDragDrop(hwndChild); 

                        new ChildSubClass(this, hwndChild); 
                        SubclassedChildWindows[hwndChild] = true; 
                    }
 
                }

                // UserControl is a special ContainerControl which should "hook to all the WindowHandles"
                // Since it doesnt allow the Mouse to pass through any of its contained controls. 
                // Please refer to VsWhidbey : 293117
                if (child == null || Control is UserControl) 
                { 
                    // Now do the children of this window.
                    // 
                    HookChildHandles(NativeMethods.GetWindow(hwndChild, NativeMethods.GW_CHILD));
                }

                hwndChild = NativeMethods.GetWindow(hwndChild, NativeMethods.GW_HWNDNEXT); 
            }
        } 
 
        internal void RemoveSubclassedWindow(IntPtr hwnd) {
            if (SubclassedChildWindows.ContainsKey(hwnd)) { 
                SubclassedChildWindows.Remove(hwnd);
            }
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///     Called by the host when we're first initialized. 
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            // Visibility works as follows:  If the control's property is not actually set, then
            // set our shadow to true.  Otherwise, grab the shadow value from the control directly and
            // then set the control to be visible if it is not the root component.  Root components
            // will be set to visible = true in their own time by the view. 
            //
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(component.GetType()); 
            PropertyDescriptor visibleProp = props["Visible"]; 
            if (visibleProp == null || visibleProp.PropertyType != typeof(bool) || !visibleProp.ShouldSerializeValue(component))
            { 
                Visible = true;
            }
            else
            { 
                Visible = (bool)visibleProp.GetValue(component);
            } 
 
            PropertyDescriptor enabledProp = props["Enabled"];
            if (enabledProp == null || enabledProp.PropertyType != typeof(bool) || !enabledProp.ShouldSerializeValue(component)) 
            {
                Enabled = true;
            }
            else 
            {
                Enabled = (bool)enabledProp.GetValue(component); 
            } 

 
            initializing = true;
            base.Initialize(component);
            initializing = false;
 
            // And get other commonly used services.
            // 
            host = (IDesignerHost)GetService(typeof(IDesignerHost)); 

            // this is to create the action in the DAP for this component if it requires docking/undocking logic 
            AttributeCollection attributes = TypeDescriptor.GetAttributes(Component);
            DockingAttribute dockingAttribute = (DockingAttribute)attributes[typeof(DockingAttribute)];
            if (dockingAttribute != null && dockingAttribute.DockingBehavior != DockingBehavior.Never) {
                // create the action for this control 
                dockingAction = new DockingActionList(this);
                //add our 'dock in parent' or 'undock in parent' action 
                DesignerActionService das = GetService(typeof(DesignerActionService)) as DesignerActionService; 
                if (das != null) {
                    das.Add(Component, dockingAction); 
                }
            }
            // Hook up the property change notifications we need to track. One for data binding.
            // More for control add / remove notifications 
            //
            dataBindingsCollectionChanged = new CollectionChangeEventHandler(DataBindingsCollectionChanged); 
            Control.DataBindings.CollectionChanged += dataBindingsCollectionChanged; 

            Control.ControlAdded += new ControlEventHandler(OnControlAdded); 
            Control.ControlRemoved += new ControlEventHandler(OnControlRemoved);
            Control.ParentChanged += new EventHandler(OnParentChanged);

            Control.SizeChanged += new EventHandler(OnSizeChanged); 
            Control.LocationChanged += new EventHandler(OnLocationChanged);
 
            // Replace the control's window target with our own.  This 
            // allows us to hook messages.
            // 
            this.DesignerTarget = new DesignerWindowTarget(this);

            // If the handle has already been created for this control, invoke OnCreateHandle so we
            // can hookup our child control subclass. 
            //
            if (Control.IsHandleCreated) { 
                OnCreateHandle(); 
            }
 
            // If we are an inherited control, notify our inheritance UI
            //
            if (Inherited && host != null && host.RootComponent != component) {
                inheritanceUI = (InheritanceUI)GetService(typeof(InheritanceUI)); 
                if (inheritanceUI != null) {
                    inheritanceUI.AddInheritedControl(Control, InheritanceAttribute.InheritanceLevel); 
                } 
            }
 
            // When we drag one control from one form to another, we will end up here.
            // In this case we do not want to set the control to visible, so check ForceVisible.
            if ((host == null || host.RootComponent != component) && ForceVisible) {
                Control.Visible = true; 
            }
 
            // Always make controls enabled, event inherited ones.  Otherwise we won't be able 
            // to select them.
            // 
            Control.Enabled = true;

            //we move enabledchanged below the set to avoid any possible stack overflows.
            //this can occur if the parent is not enabled when we set enabled to true. 
            //see VS#538084
            Control.EnabledChanged += new EventHandler(OnEnabledChanged); 
 
            // And force some shadow properties that we change in the course of
            // initializing the form. 
            //
            AllowDrop = Control.AllowDrop;

            // update the Status Command 
            statusCommandUI = new StatusCommandUI(component.Site);
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InitializeExistingComponent"]/*' />
        /// <devdoc> 
        ///    ControlDesigner overrides this method to handle after-drop cases.
        /// </devdoc>
        public override void InitializeExistingComponent(IDictionary defaultValues) {
            base.InitializeExistingComponent(defaultValues); 

            // unhook any sited children that got ChildWindowTargets 
            foreach (Control c in Control.Controls) { 
                  if (c != null) {
                     ISite site = c.Site; 
                     ChildWindowTarget target = c.WindowTarget as ChildWindowTarget;
                     if (site != null && target != null) {
                        c.WindowTarget = target.OldWindowTarget;
                     } 
                  }
            } 
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InitializeNewComponent"]/*' /> 
        /// <devdoc>
        ///   ControlDesigner overrides this method.  It will look at the default property for the control and,
        ///   if it is of type string, it will set this property's value to the name of the component.  It only
        ///   does this if the designer has been configured with this option in the options service.  This method 
        ///   also connects the control to its parent and positions it.  If you override this method, you should
        ///   always call base. 
        /// </devdoc> 
        public override void InitializeNewComponent(IDictionary defaultValues) {
 
            ISite site = Component.Site;

            if (site != null) {
                PropertyDescriptor textProp = TypeDescriptor.GetProperties(Component)["Text"]; 
                if (textProp != null && textProp.PropertyType == typeof(string) && !textProp.IsReadOnly && textProp.IsBrowsable) {
                    textProp.SetValue(Component, site.Name); 
                } 
            }
 
            if (defaultValues != null) {
                IComponent parent = defaultValues["Parent"] as IComponent;
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
 
                if (parent != null && host != null) {
 
                    ParentControlDesigner parentDesigner = host.GetDesigner(parent) as ParentControlDesigner; 
                    if (parentDesigner != null) {
                        parentDesigner.AddControl(Control, defaultValues); 
                    }

                    Control parentControl = parent as Control;
 
                    if (parentControl != null) {
                        // 
                        // Some containers are docked differently (instead of DockStyle.None) when 
                        // they are added through the designer
                        // 
                        AttributeCollection attributes = TypeDescriptor.GetAttributes(Component);
                        DockingAttribute dockingAttribute = (DockingAttribute)attributes[typeof(DockingAttribute)];

                        if (dockingAttribute != null && dockingAttribute.DockingBehavior != DockingBehavior.Never) { 

                            if (dockingAttribute.DockingBehavior == DockingBehavior.AutoDock) { 
                                bool onlyNonDockedChild = true; 
                                foreach (Control c in parentControl.Controls) {
                                    if (c != Control && c.Dock == DockStyle.None) { 
                                        onlyNonDockedChild = false;
                                        break;
                                    }
                                } 

                                if (onlyNonDockedChild) { 
                                    PropertyDescriptor dockProp = TypeDescriptor.GetProperties(Component)["Dock"]; 
                                    if (dockProp != null && dockProp.IsBrowsable) {
                                        dockProp.SetValue(Component, DockStyle.Fill); 
                                    }
                                }

                            } 
                        }
                    } 
                } 
            }
            // Finally, call base.  Base simply calls OnSetComponentDefaults to preserve old behavior. 
            base.InitializeNewComponent(defaultValues);
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnSetComponentDefaults"]/*' /> 
        /// <devdoc>
        ///     Called when the designer is intialized.  This allows the designer to provide some 
        ///     meaningful default values in the component.  The default implementation of this 
        ///     sets the components's default property to it's name, if that property is a string.
        /// </devdoc> 
        [Obsolete("This method has been deprecated. Use InitializeNewComponent instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public override void OnSetComponentDefaults() {
            // COMPAT: The following code shipped in Everett, so we need to continue to do this.
            // See VSWhidbey #467460 for details. 

            // Don't call base. 
            // 
            ISite site = Component.Site;
            if (site != null) { 
                PropertyDescriptor textProp = TypeDescriptor.GetProperties(Component)["Text"];
                if (textProp != null && textProp.IsBrowsable) {
                    textProp.SetValue(Component, site.Name);
                } 
            }
        } 
 
        /// <devdoc>
        ///     Determines if the given mouse click is a double click or not.  We must 
        ///     handle this ourselves for controls that don't have the CS_DOUBLECLICKS style
        ///     set.
        /// </devdoc>
        private bool IsDoubleClick(int x, int y) { 
            bool doubleClick = false;
 
            int wait = SystemInformation.DoubleClickTime; 
            int elapsed = SafeNativeMethods.GetTickCount() -  lastClickMessageTime;
 
            if (elapsed <= wait) {
                Size dblClick = SystemInformation.DoubleClickSize;

                if (x >= lastClickMessagePositionX - dblClick.Width 
                    && x <= lastClickMessagePositionX + dblClick.Width
                    && y >= lastClickMessagePositionY - dblClick.Height 
                    && y <= lastClickMessagePositionY + dblClick.Height) { 

                    doubleClick = true; 
                }
            }

            if (!doubleClick) { 
                lastClickMessagePositionX = x;
                lastClickMessagePositionY = y; 
                lastClickMessageTime = SafeNativeMethods.GetTickCount(); 
            }
            else { 
                lastClickMessagePositionX = lastClickMessagePositionY = 0;
                lastClickMessageTime = 0;
            }
 
            return doubleClick;
        } 
 
        private bool IsMouseMessage(int msg) {
 
            if (msg >= NativeMethods.WM_MOUSEFIRST && msg <= NativeMethods.WM_MOUSELAST) {
                return true;
            }
 
            switch (msg) {
                // WM messages not covered by the above block 
                case NativeMethods.WM_MOUSEHOVER: 
                case NativeMethods.WM_MOUSELEAVE:
 
                // WM_NC messages
                case NativeMethods.WM_NCMOUSEMOVE:
                case NativeMethods.WM_NCLBUTTONDOWN:
                case NativeMethods.WM_NCLBUTTONUP: 
                case NativeMethods.WM_NCLBUTTONDBLCLK:
                case NativeMethods.WM_NCRBUTTONDOWN: 
                case NativeMethods.WM_NCRBUTTONUP: 
                case NativeMethods.WM_NCRBUTTONDBLCLK:
                case NativeMethods.WM_NCMBUTTONDOWN: 
                case NativeMethods.WM_NCMBUTTONUP:
                case NativeMethods.WM_NCMBUTTONDBLCLK:
                case NativeMethods.WM_NCMOUSEHOVER:
                case NativeMethods.WM_NCMOUSELEAVE: 
                case NativeMethods.WM_NCXBUTTONDOWN:
                case NativeMethods.WM_NCXBUTTONUP: 
                case NativeMethods.WM_NCXBUTTONDBLCLK: 

                    return true; 
                default:
                    return false;
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnContextMenu"]/*' /> 
        /// <devdoc> 
        ///     Called when the context menu should be displayed
        /// </devdoc> 
        protected virtual void OnContextMenu(int x, int y) {
            ShowContextMenu(x, y);
        }
 
        /// <devdoc>
        ///     Called in response to a new control being added to this designer's control. 
        ///     We check to see if the control has an associated ControlDesigner.  If it 
        ///     doesn't, we hook its window target so we can sniff messages and make
        ///     it ui inactive. 
        /// </devdoc>
        private void OnControlAdded(object sender, ControlEventArgs e) {
            if (e.Control != null && host != null) {
                if (!(host.GetDesigner(e.Control) is ControlDesigner)) { 

                    // No, no designer means we must replace the window target in this 
                    // control. 
                    //
                    IWindowTarget oldTarget = e.Control.WindowTarget; 

                    if (!(oldTarget is ChildWindowTarget)) {
                        e.Control.WindowTarget = new ChildWindowTarget(this, e.Control, oldTarget);
                    } 

                    // ASURT 45655: Some controls (primarily RichEdit) will register themselves as 
                    // drag-drop source/targets when they are instantiated. We have to RevokeDragDrop() 
                    // for them so that the ParentControlDesigner()'s drag-drop support can work
                    // correctly. Normally, the hwnd for the child control is not created at this time, 
                    // and we will use the WM_CREATE message in ChildWindowTarget's WndProc() to revoke
                    // drag-drop. But, if the handle was already created for some reason, we will need
                    // to revoke drag-drop right away.
                    // 
                    if (e.Control.IsHandleCreated) {
                        Application.OleRequired(); 
                        NativeMethods.RevokeDragDrop(e.Control.Handle); 

 
                        // We only hook the control's children if there was no designer.
                        // We leave it up to the designer to hook its own children.
                        //
                        HookChildControls(e.Control); 
                    }
                } 
            } 
        }
 
        /// <devdoc>
        ///     Called in response to a control being removed from this designer's control.
        ///     If we previously changed out this control's window target, we undo that
        ///     work here. 
        /// </devdoc>
        private void OnControlRemoved(object sender, ControlEventArgs e) { 
            if (e.Control != null) { 

                // No, no designer means we must replace the window target in this 
                // control.
                //
                ChildWindowTarget oldTarget = e.Control.WindowTarget as ChildWindowTarget;
 
                if (oldTarget != null) {
                    e.Control.WindowTarget = oldTarget.OldWindowTarget; 
                } 

                UnhookChildControls(e.Control); 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnCreateHandle"]/*' /> 
        /// <devdoc>
        ///      This is called immediately after the control handle has been created. 
        /// </devdoc> 
        protected virtual void OnCreateHandle() {
            OnHandleChange(); 

            if (revokeDragDrop) {
                int n = NativeMethods.RevokeDragDrop(Control.Handle);
            } 
        }
 
 
        /// <devdoc>
        ///      Event handler for our drag enter event.  The host will call us with 
        ///      this when an OLE drag event happens.
        /// </devdoc>
        private void OnDragEnter(object s, DragEventArgs e) {
            if (BehaviorService != null) { 
                //Tell the BehaviorService to monitor mouse messages
                //so it can send appropriate drag notifications. 
                // 
                BehaviorService.StartDragNotification();
            } 

            OnDragEnter(e);
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnDragEnter1"]/*' />
        /// <devdoc> 
        ///     Called when a drag-drop operation enters the control designer view 
        ///
        /// </devdoc> 
        protected virtual void OnDragEnter(DragEventArgs de) {
            // unhook our events - we don't want to create an infinite loop.
            Control control = Control;
            DragEventHandler handler = new DragEventHandler(this.OnDragEnter); 
            control.DragEnter -= handler;
            ((IDropTarget)Control).OnDragEnter(de); 
            control.DragEnter += handler; 
        }
 
        /// <devdoc>
        ///      Event handler for our drag drop event.  The host will call us with
        ///      this when an OLE drag event happens.
        /// </devdoc> 
        private void OnDragDrop(object s, DragEventArgs e) {
 
            if (BehaviorService != null) { 
                //this will cause the BehSvc to return from 'drag mode'
                // 
                BehaviorService.EndDragNotification();
            }

            OnDragDrop(e); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnDragComplete"]/*' /> 
        /// <devdoc>
        ///     Called to cleanup a D&D operation 
        /// </devdoc>
        protected virtual void OnDragComplete(DragEventArgs de) {
            // default implementation - does nothing.
        } 

 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnDragDrop1"]/*' /> 
        /// <devdoc>
        ///     Called when a drag drop object is dropped onto the control designer view 
        /// </devdoc>
        protected virtual void OnDragDrop(DragEventArgs de) {
            // unhook our events - we don't want to create an infinite loop.
            Control control = Control; 
            DragEventHandler handler = new DragEventHandler(this.OnDragDrop);
            control.DragDrop -= handler; 
            ((IDropTarget)Control).OnDragDrop(de); 
            control.DragDrop += handler;
 
            OnDragComplete(de);
        }

        /// <devdoc> 
        ///      Event handler for our drag leave event.  The host will call us with
        ///      this when an OLE drag event happens. 
        /// </devdoc> 
        private void OnDragLeave(object s, EventArgs e) {
            OnDragLeave(e); 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnDragLeave1"]/*' />
        /// <devdoc> 
        ///     Called when a drag-drop operation leaves the control designer view
        /// 
        /// </devdoc> 
        protected virtual void OnDragLeave(EventArgs e) {
            // unhook our events - we don't want to create an infinite loop. 
            Control control = Control;
            EventHandler handler = new EventHandler(this.OnDragLeave);
            control.DragLeave -= handler;
            ((IDropTarget)Control).OnDragLeave(e); 
            control.DragLeave += handler;
        } 
 
        /// <devdoc>
        ///      Event handler for our drag over event.  The host will call us with 
        ///      this when an OLE drag event happens.
        /// </devdoc>
        private void OnDragOver(object s, DragEventArgs e) {
            OnDragOver(e); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnDragOver1"]/*' /> 
        /// <devdoc>
        ///     Called when a drag drop object is dragged over the control designer view 
        /// </devdoc>
        protected virtual void OnDragOver(DragEventArgs de) {
            // unhook our events - we don't want to create an infinite loop.
            Control control = Control; 
            DragEventHandler handler = new DragEventHandler(this.OnDragOver);
            control.DragOver -= handler; 
            ((IDropTarget)Control).OnDragOver(de); 
            control.DragOver += handler;
        } 

        /// <devdoc>
        ///      Event handler for our GiveFeedback event, which is called when a drag operation
        ///      is in progress.  The host will call us with 
        ///      this when an OLE drag event happens.
        /// </devdoc> 
        private void OnGiveFeedback(object s, GiveFeedbackEventArgs e) { 
            OnGiveFeedback(e);
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnGiveFeedback1"]/*' />
        /// <devdoc>
        ///      Event handler for our GiveFeedback event, which is called when a drag operation 
        ///      is in progress.  The host will call us with
        ///      this when an OLE drag event happens. 
        /// </devdoc> 
        protected virtual void OnGiveFeedback(GiveFeedbackEventArgs e) {
        } 

        /// <devdoc>
        ///      This is called whenever the control handle changes.
        /// </devdoc> 
        private void OnHandleChange() {
            // We must now traverse child handles for this control.  There are 
            // three types of child handles and we are interested in two of 
            // them:
            // 
            //  1.  Child handles that do not have a Control associated
            //      with them.  We must subclass these and prevent them
            //      from getting design-time events.
            // 
            // 2.   Child handles that do have a Control associated
            //      with them, but the control does not have a designer. 
            //      We must hook the WindowTarget on these controls and 
            //      prevent them from getting design-time events.
            // 
            // 3.   Child handles that do have a Control associated
            //      with them, and the control has a designer.  We
            //      ignore these and let the designer handle their
            //      messages. 
            //
            HookChildHandles(NativeMethods.GetWindow(Control.Handle, NativeMethods.GW_CHILD)); 
            HookChildControls(Control); 
        }
 
        /// <devdoc>
        ///     Called in response to a double-click of the left mouse button.  We
        ///     Just call this on the event service
        /// </devdoc> 
        private void OnMouseDoubleClick() {
 
            try { 
               DoDefaultAction();
            } 
            catch (Exception e) {
                DisplayError(e);
                if (ClientUtils.IsCriticalException(e)) {
                    throw; 
                }
            } 
            catch { 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseDragBegin"]/*' />
        /// <devdoc>
        ///     Called in response to the left mouse button being pressed on a 
        ///     component. It ensures that the component is selected.
        /// </devdoc> 
        protected virtual void OnMouseDragBegin(int x, int y) { 
            // Ignore another mouse down if we are already in a drag.
            // 
            if (BehaviorService == null && mouseDragLast != InvalidPoint) {
                return;
            }
 
            mouseDragLast = new Point(x, y);
            ctrlSelect = (Control.ModifierKeys & Keys.Control) != 0; 
            ISelectionService sel = (ISelectionService)GetService(typeof(ISelectionService)); 

            // If the CTRL key isn't down, select this component, 
            // otherwise, we wait until the mouse up
            //
            // Make sure the component is selected
            // 
            if (!ctrlSelect && sel != null) {
                sel.SetSelectedComponents(new object[] {Component}, SelectionTypes.Primary); 
            } 

            Control.Capture = true; 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseDragEnd"]/*' />
        /// <devdoc> 
        ///     Called at the end of a drag operation.  This either commits or rolls back the
        ///     drag. 
        /// </devdoc> 
        protected virtual void OnMouseDragEnd(bool cancel) {
            mouseDragLast = InvalidPoint; 
            Control.Capture = false;

            if (!mouseDragMoved) {
                if (!cancel) { 
                    ISelectionService sel = (ISelectionService)GetService(typeof(ISelectionService));
                    bool shiftSelect = (Control.ModifierKeys & Keys.Shift) != 0; 
                    if (!shiftSelect && (ctrlSelect || (sel != null && !sel.GetComponentSelected(Component)))) { 
                        if (sel != null) {
                            sel.SetSelectedComponents(new object[] {Component}, SelectionTypes.Primary); 
                        }
                        ctrlSelect = false;
                    }
                } 
                return;
            } 
            mouseDragMoved = false; 
            ctrlSelect = false;
 
            // And now finish the drag.

            if (BehaviorService != null && BehaviorService.Dragging && cancel) {
                BehaviorService.CancelDrag = true; 
            }
 
            // Leave this here in case we are doing a ComponentTray drag 
            if (selectionUISvc == null) {
                selectionUISvc = (ISelectionUIService)GetService(typeof(ISelectionUIService)); 
            }

            if (selectionUISvc == null) {
                return; 
            }
 
            // We must check to ensure that UI service is still in drag mode.  It is 
            // possible that the user hit escape, which will cancel drag mode.
            // 
            if (selectionUISvc.Dragging) {
                selectionUISvc.EndDrag(cancel);
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseDragMove"]/*' /> 
        /// <devdoc> 
        ///     Called for each movement of the mouse.  This will check to see if a drag operation
        ///     is in progress.  If so, it will pass the updated drag dimensions on to the selection 
        ///     UI service.
        /// </devdoc>
        protected virtual void OnMouseDragMove(int x, int y) {
            if (!mouseDragMoved) { 
                Size minDrag = SystemInformation.DragSize;
                Size minDblClick = SystemInformation.DoubleClickSize; 
 
                minDrag.Width = Math.Max(minDrag.Width, minDblClick.Width);
                minDrag.Height = Math.Max(minDrag.Height, minDblClick.Height); 

                // we have to make sure the mouse moved farther than
                // the minimum drag distance before we actually start
                // the drag 
                //
                if (mouseDragLast == InvalidPoint || 
                    (Math.Abs(mouseDragLast.X - x) < minDrag.Width && 
                     Math.Abs(mouseDragLast.Y - y) < minDrag.Height)) {
                    return; 
                }
                else {
                    mouseDragMoved = true;
                    // we're on the move, so we're not in a ctrlSelect 
                    //
                    ctrlSelect = false; 
                } 
            }
 
            // Make sure the component is selected
            //
            // VSWhidbey #461078
            // But only select it if it is not already the primary selection, and we want to toggle 
            // the current primary selection.
            ISelectionService sel = (ISelectionService)GetService(typeof(ISelectionService)); 
            if (sel != null && !Component.Equals(sel.PrimarySelection)) { 
                sel.SetSelectedComponents(new object[] {Component}, SelectionTypes.Primary | SelectionTypes.Toggle);
            } 

            if (BehaviorService != null && sel != null) {

                //create our list of controls-to-drag 
                ArrayList dragControls = new ArrayList();
                ICollection selComps = sel.GetSelectedComponents(); 
                //must identify a required parent to avoid dragging mixes of children 
                Control requiredParent = null;
 
                foreach (IComponent comp in selComps) {
                    Control control = comp as Control;
                    if (control != null) {
                        if (requiredParent == null) { 
                            requiredParent = control.Parent;
                        } 
                        else if (!requiredParent.Equals(control.Parent)) { 
                            continue;//mixed selection of different parents - don't add this
                        } 

                        ControlDesigner des = host.GetDesigner(comp) as ControlDesigner;
                        if (des != null && (des.SelectionRules & SelectionRules.Moveable) != 0) {
                            dragControls.Add(comp); 
                        }
                    } 
                } 

                //if we have controls-to-drag, create our new behavior 
                //and start the drag/drop operation
                if (dragControls.Count > 0) {
                    using (Graphics adornerGraphics = BehaviorService.AdornerWindowGraphics) {
                        DropSourceBehavior dsb = new DropSourceBehavior(dragControls, Control.Parent, mouseDragLast); 
                        BehaviorService.DoDragDrop(dsb);
                    } 
                } 
            }
 
            mouseDragLast = InvalidPoint;
            mouseDragMoved = false;
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseEnter"]/*' />
        /// <devdoc> 
        ///     Called when the mouse first enters the control. This is forwarded to the parent 
        ///     designer to enable the container selector.
        /// </devdoc> 
        protected virtual void OnMouseEnter() {
            Control ctl = Control;
            Control parent = ctl;
 
            object parentDesigner = null;
            while (parentDesigner == null && parent != null) { 
                parent = parent.Parent; 
                if (parent != null) {
                    object d = host.GetDesigner(parent); 
                    if (d != this) {
                        parentDesigner = d;
                    }
                } 
            }
 
            ControlDesigner cd = parentDesigner as ControlDesigner; 
            if (cd != null) {
                cd.OnMouseEnter(); 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseHover"]/*' /> 
        /// <devdoc>
        ///     Called after the mouse hovers over the control. This is forwarded to the parent 
        ///     designer to enabled the container selector. 
        /// </devdoc>
        protected virtual void OnMouseHover() { 
            Control ctl = Control;
            Control parent = ctl;

            object parentDesigner = null; 
            while (parentDesigner == null && parent != null) {
                parent = parent.Parent; 
                if (parent != null) { 
                    object d = host.GetDesigner(parent);
                    if (d != this) { 
                        parentDesigner = d;
                    }
                }
            } 

            ControlDesigner cd = parentDesigner as ControlDesigner; 
            if (cd != null) { 
                cd.OnMouseHover();
            } 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnMouseLeave"]/*' />
        /// <devdoc> 
        ///     Called when the mouse first enters the control. This is forwarded to the parent
        ///     designer to enable the container selector. 
        /// </devdoc> 
        protected virtual void OnMouseLeave() {
            Control ctl = Control; 
            Control parent = ctl;

            object parentDesigner = null;
            while (parentDesigner == null && parent != null) { 
                parent = parent.Parent;
                if (parent != null) { 
                    object d = host.GetDesigner(parent); 
                    if (d != this) {
                        parentDesigner = d; 
                    }
                }
            }
 
            ControlDesigner cd = parentDesigner as ControlDesigner;
            if (cd != null) { 
                cd.OnMouseLeave(); 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnPaintAdornments"]/*' />
        /// <devdoc>
        ///     Called when the control we're designing has finished painting.  This method 
        ///     gives the designer a chance to paint any additional adornments on top of the
        ///     control. 
        /// </devdoc> 
        protected virtual void OnPaintAdornments(PaintEventArgs pe) {
 
            // If this control is being inherited, paint it
            //
            if (inheritanceUI != null && pe.ClipRectangle.IntersectsWith(inheritanceUI.InheritanceGlyphRectangle)) {
                pe.Graphics.DrawImage(inheritanceUI.InheritanceGlyph, 0, 0); 
            }
        } 
 
        private void OnParentChanged(object sender, EventArgs e) {
            if (Control.IsHandleCreated) { 
                OnHandleChange();
            }
        }
 
        // VSWhidbey #245901
        // HACK HACK 
        // This is a workaround to some problems with the ComponentCache that we should fix. 
        // When this is removed remember to change ComponentCache's RemoveEntry method back to private (from internal).
        private void OnSizeChanged(object sender, EventArgs e) { 
            System.ComponentModel.Design.Serialization.ComponentCache cache =
                (System.ComponentModel.Design.Serialization.ComponentCache) GetService(typeof(System.ComponentModel.Design.Serialization.ComponentCache));
            object component = Component;
            if (cache != null && component != null) { 
                cache.RemoveEntry(component);
            } 
        } 

         private void OnLocationChanged(object sender, EventArgs e) { 
            System.ComponentModel.Design.Serialization.ComponentCache cache =
                (System.ComponentModel.Design.Serialization.ComponentCache) GetService(typeof(System.ComponentModel.Design.Serialization.ComponentCache));
            object component = Component;
            if (cache != null && component != null) { 
                cache.RemoveEntry(component);
            } 
        } 

        private void OnEnabledChanged(object sender, EventArgs e) { 
            // VSWhidbey #154310 - Never allow controls to be disabled at design time.
            if (!enabledchangerecursionguard) {
                 enabledchangerecursionguard = true;
                 try { 
                      Control.Enabled = true;
                 } 
                 finally { 
                     enabledchangerecursionguard = false;
                 } 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnSetCursor"]/*' /> 
        /// <devdoc>
        ///     Called each time the cursor needs to be set.  The ControlDesigner behavior here 
        ///     will set the cursor to one of three things: 
        ///     1.  If the toolbox service has a tool selected, it will allow the toolbox service to
        ///     set the cursor. 
        ///     2.  If the selection UI service shows a locked selection, or if there is no location
        ///     property on the control, then the default arrow will be set.
        ///     3.  Otherwise, the four headed arrow will be set to indicate that the component can
        ///     be clicked and moved. 
        ///     4.  If the user is currently dragging a component, the crosshair cursor will be used
        ///     instead of the four headed arrow. 
        /// </devdoc> 
        protected virtual void OnSetCursor() {
 
            if (Control.Dock != DockStyle.None) {
                Cursor.Current = Cursors.Default;
            }
            else { 

                if (toolboxSvc == null) { 
                    toolboxSvc = (IToolboxService)GetService(typeof(IToolboxService)); 
                }
 
                if (toolboxSvc != null && toolboxSvc.SetCursor()) {
                    return;
                }
 
                if (!locationChecked) {
                    locationChecked = true; 
 
                    try {
                        hasLocation = TypeDescriptor.GetProperties(Component)["Location"] != null; 
                    }
                    catch {
                    }
                } 

                if (!hasLocation) { 
                    Cursor.Current = Cursors.Default; 
                    return;
                } 

                if (Locked) {
                    Cursor.Current = Cursors.Default;
                    return; 
                }
 
                Cursor.Current = Cursors.SizeAll; 
            }
        } 

        /// <devdoc>
        ///     Paints a red rectangle with a red X, painted on a white background.  Used
        ///     when the control has thrown an exception. 
        /// </devdoc>
        private void PaintException(PaintEventArgs e, Exception ex) { 
            StringFormat stringFormat = new StringFormat(); 
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near; 

            string exceptionText = ex.ToString();
            stringFormat.SetMeasurableCharacterRanges(new CharacterRange[] {new CharacterRange(0, exceptionText.Length)});
 
            // rendering calculations...
            // 
            int penThickness = 2; 
            Size glyphSize = SystemInformation.IconSize;
            int marginX = penThickness * 2; 
            int marginY = penThickness * 2;

            Rectangle clientRectangle = Control.ClientRectangle;
 
            Rectangle borderRectangle = clientRectangle;
            borderRectangle.X++; 
            borderRectangle.Y++; 
            borderRectangle.Width -=2;
            borderRectangle.Height-=2; 

            Rectangle imageRect = new Rectangle(marginX, marginY, glyphSize.Width, glyphSize.Height);

            Rectangle textRect = clientRectangle; 
            textRect.X = imageRect.X + imageRect.Width + 2 * marginX;
            textRect.Y = imageRect.Y; 
            textRect.Width -= (textRect.X + marginX + penThickness); 
            textRect.Height -= (textRect.Y + marginY + penThickness);
 
            using (Font errorFont = new Font(Control.Font.FontFamily, Math.Max(SystemInformation.ToolWindowCaptionHeight - SystemInformation.BorderSize.Height - 2, Control.Font.Height), GraphicsUnit.Pixel)) {

                using(Region textRegion = e.Graphics.MeasureCharacterRanges(exceptionText, errorFont, textRect, stringFormat)[0]) {
                    // paint contents... clipping optimizations for less flicker... 
                    //
                    Region originalClip = e.Graphics.Clip; 
 
                    e.Graphics.ExcludeClip(textRegion);
                    e.Graphics.ExcludeClip(imageRect); 
                    try {
                        e.Graphics.FillRectangle(Brushes.White, clientRectangle);
                    }
                    finally { 
                        e.Graphics.Clip = originalClip;
                    } 
 
                    using (Pen pen = new Pen(Color.Red, penThickness)) {
                        e.Graphics.DrawRectangle(pen, borderRectangle); 
                    }

                    Icon err = SystemIcons.Error;
 
                    e.Graphics.FillRectangle(Brushes.White, imageRect);
                    e.Graphics.DrawIcon(err, imageRect.X, imageRect.Y); 
 
                    textRect.X++;
                    e.Graphics.IntersectClip(textRegion); 
                    try {
                        e.Graphics.FillRectangle(Brushes.White, textRect);
                        e.Graphics.DrawString(exceptionText, errorFont, new SolidBrush(Control.ForeColor), textRect, stringFormat);
                    } 
                    finally {
                        e.Graphics.Clip = originalClip; 
                    } 
                }
            } 
        }


 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        ///      Allows a designer to filter the set of properties 
        ///      the component it is designing will expose through the
        ///      TypeDescriptor object.  This method is called 
        ///      immediately before its corresponding "Post" method.
        ///      If you are overriding this method you should call
        ///      the base implementation before you perform your own
        ///      filtering. 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties); 

            PropertyDescriptor prop; 

            // Handle shadowed properties
            //
            string[] shadowProps = new string[] { 
                "Visible",
                "Enabled", 
                "ContextMenu", 
                "AllowDrop",
                "Location", 
                "Name"
            };

            Attribute[] empty = new Attribute[0]; 

            for (int i = 0; i < shadowProps.Length; i++) { 
                prop = (PropertyDescriptor)properties[shadowProps[i]]; 
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(ControlDesigner), prop, empty); 
                }
            }

            // replace this one seperately because it is of a different type (DesignerControlCollection) than 
            // the original property (ControlCollection)
            // 
            PropertyDescriptor controlsProp = (PropertyDescriptor)properties["Controls"]; 

            if (controlsProp != null) { 
                Attribute[] attrs = new Attribute[controlsProp.Attributes.Count];
                controlsProp.Attributes.CopyTo(attrs, 0);
                properties["Controls"] = TypeDescriptor.CreateProperty(typeof(ControlDesigner), "Controls", typeof(DesignerControlCollection), attrs);
            } 

            PropertyDescriptor sizeProp = (PropertyDescriptor)properties["Size"]; 
            if (sizeProp != null) { 
                properties["Size"] = new CanResetSizePropertyDescriptor(sizeProp);
            } 

            // Now we add our own design time properties.
            //
            properties["Locked"] = TypeDescriptor.CreateProperty(typeof(ControlDesigner), "Locked", typeof(bool), 
                                                        new DefaultValueAttribute(false),
                                                        BrowsableAttribute.Yes, 
                                                        CategoryAttribute.Design, 
                                                        DesignOnlyAttribute.Yes,
                                                        new SRDescriptionAttribute(SR.lockedDescr)); 



 
        }
 
        /// <devdoc> 
        ///     Returns true if the visible property should be persisted in code gen.
        /// </devdoc> 
        private void ResetVisible() {
            Visible = true;
        }
 
        /// <devdoc>
        ///     Returns true if the Enabled property should be persisted in code gen. 
        /// </devdoc> 
        private void ResetEnabled() {
            Enabled = true; 
        }

        /// <devdoc>
        ///     Sets an unhandled exception that is raised from a control or child control wndproc. 
        /// </devdoc>
        internal void SetUnhandledException(Control owner, Exception exception) { 
            if (thrownException == null) { 
                thrownException = exception;
                if (owner == null) { 
                    owner = Control;
                }
                string stack = string.Empty;
                string[] exceptionLines = exception.StackTrace.Split('\r', '\n'); 
                string typeName = owner.GetType().FullName;
                foreach(string line in exceptionLines) { 
                    if (line.IndexOf(typeName) != -1) { 
                        stack = string.Format(CultureInfo.CurrentCulture, "{0}\r\n{1}", stack, line);
                    } 
                }

                Exception wrapper = new Exception(SR.GetString(SR.ControlDesigner_WndProcException, typeName, exception.Message, stack), exception);
                DisplayError(wrapper); 

                // hide all the child controls. 
                // 
                foreach (Control c in Control.Controls) {
                    c.Visible = false; 
                }

                Control.Invalidate(true);
            } 
        }
 
        private bool ShouldSerializeAllowDrop() { 
            return AllowDrop != hadDragDrop;
        } 


        /// <devdoc>
        ///     Returns true if the enabled property should be persisted in code gen. 
        /// </devdoc>
        private bool ShouldSerializeEnabled() { 
            return ShadowProperties.ShouldSerializeValue("Enabled", true); 
        }
 
        /// <devdoc>
        ///     Returns true if the visible property should be persisted in code gen.
        /// </devdoc>
        private bool ShouldSerializeVisible() { 
            return ShadowProperties.ShouldSerializeValue("Visible", true);
        } 
 
        private bool ShouldSerializeName() {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(host != null);

            return initializing ? (Component != host.RootComponent)  //for non root components, respect the name that the base Control serialized unless changed
                : ShadowProperties.ShouldSerializeValue("Name", null); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.UnhookChildControls"]/*' /> 
        /// <devdoc>
        ///     Hooks the children of the given control.  We need to do this for 
        ///     child controls that are not in design mode, which is the case
        ///     for composite controls.
        /// </devdoc>
        protected void UnhookChildControls(Control firstChild) { 

            if (host == null) { 
                host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            }
 
            foreach(Control child in firstChild.Controls) {
                IWindowTarget oldTarget = null;
                if (child != null) {
 
                    // No, no designer means we must replace the window target in this
                    // control. 
                    // 
                    oldTarget= child.WindowTarget;
 
                    ChildWindowTarget target = oldTarget as ChildWindowTarget;
                    if (target != null) {
                        child.WindowTarget = target.OldWindowTarget;
                    } 
                }
                if (!(oldTarget is DesignerWindowTarget)) { 
                    UnhookChildControls(child); 
                }
 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.WndProc"]/*' /> 
        /// <devdoc>
        ///     This method should be called by the extending designer for each message 
        ///     the control would normally receive.  This allows the designer to pre-process 
        ///     messages before allowing them to be routed to the control.
        /// </devdoc> 
        protected virtual void WndProc(ref Message m) {
            IMouseHandler mouseHandler = null;

            // We look at WM_NCHITTEST to determine if the mouse 
            // is in a live region of the control
            // 
            if (m.Msg == NativeMethods.WM_NCHITTEST) { 
                if (!inHitTest) {
                    inHitTest = true; 
                    Point pt = new Point((short)NativeMethods.Util.LOWORD((int)m.LParam),
                                         (short)NativeMethods.Util.HIWORD((int)m.LParam));
                    try {
                        liveRegion = GetHitTest(pt); 
                    }
                    catch (Exception e) { 
                        liveRegion = false; 
                        if (ClientUtils.IsCriticalException(e)) {
                            throw; 
                        }
                    }
                    catch {
                        liveRegion = false; 
                    }
                    inHitTest = false; 
                } 
            }
 
            // Check to see if the mouse
            // is in a live region of the control
            // and that the context key is not being fired
            // 
            bool isContextKey = (m.Msg == NativeMethods.WM_CONTEXTMENU);
 
            if (liveRegion && (IsMouseMessage(m.Msg) || isContextKey)) { 
                // ASURT 70725: The ActiveX DataGrid control brings up a context menu on right mouse down when
                // it is in edit mode. 
                // And, when we generate a WM_CONTEXTMENU message later, it calls DefWndProc() which by default
                // calls the parent (formdesigner). The FormDesigner then brings up the AxHost context menu.
                // This code causes recursive WM_CONTEXTMENU messages to be ignored till we return from the
                // live region message. 
                //
                if (m.Msg == NativeMethods.WM_CONTEXTMENU) { 
                    Debug.Assert(!inContextMenu, "Recursively hitting live region for context menu!!!"); 
                    inContextMenu = true;
                } 

                try {
                    DefWndProc(ref m);
                } 
                finally {
                    if (m.Msg == NativeMethods.WM_CONTEXTMENU) { 
                        inContextMenu = false; 
                    }
                    if (m.Msg == NativeMethods.WM_LBUTTONUP) 
                    {
                        // terminate the drag.
                        //
                        // Vs Whidbey : 355250 
                        // DTS SRX040824604234 TabControl loses shortcut menu options after adding ActiveX control.
                        OnMouseDragEnd(true); 
                    } 

                } 
                return;
            }

            // Get the x and y coordniates of the mouse message 
            //
            int x = 0, y = 0; 
 
            // Look for a mouse handler.
            // 
            //


 
            if (m.Msg >= NativeMethods.WM_MOUSEFIRST && m.Msg <= NativeMethods.WM_MOUSELAST
                || m.Msg >= NativeMethods.WM_NCMOUSEMOVE && m.Msg <= NativeMethods.WM_NCMBUTTONDBLCLK 
                || m.Msg == NativeMethods.WM_SETCURSOR) { 

                if (eventSvc == null) { 
                    eventSvc = (IEventHandlerService)GetService(typeof(IEventHandlerService));
                }
                if (eventSvc != null) {
                    mouseHandler = (IMouseHandler)eventSvc.GetHandler(typeof(IMouseHandler)); 
                }
            } 
 
            if (m.Msg >= NativeMethods.WM_MOUSEFIRST && m.Msg <= NativeMethods.WM_MOUSELAST) {
 
                NativeMethods.POINT pt = new NativeMethods.POINT();
                pt.x = NativeMethods.Util.SignedLOWORD((int)m.LParam);
                pt.y = NativeMethods.Util.SignedHIWORD((int)m.LParam);
                NativeMethods.MapWindowPoints(m.HWnd, IntPtr.Zero, pt, 1); 
                x = pt.x;
                y = pt.y; 
            } 
            else if (m.Msg >= NativeMethods.WM_NCMOUSEMOVE && m.Msg <= NativeMethods.WM_NCMBUTTONDBLCLK) {
                x = NativeMethods.Util.SignedLOWORD((int)m.LParam); 
                y = NativeMethods.Util.SignedHIWORD((int)m.LParam);
            }

            // This is implemented on the base designer for 
            // UI activation support.  We call it so that
            // we can support UI activation. 
            // 
            MouseButtons button = MouseButtons.None;
 
            switch (m.Msg) {
                case NativeMethods.WM_CREATE:
                    DefWndProc(ref m);
 
                    // Only call OnCreateHandle if this is our OWN
                    // window handle -- the designer window procs are 
                    // re-entered for child controls. 
                    //
                    if (m.HWnd == Control.Handle) { 
                        OnCreateHandle();
                    }
                    break;
 
                case NativeMethods.WM_GETOBJECT:
                    // See "How to Handle WM_GETOBJECT" in MSDN 
                    if (NativeMethods.OBJID_CLIENT == (int)m.LParam) { 

                        // Get the IAccessible GUID 
                        //
                        Guid IID_IAccessible = new Guid(NativeMethods.uuid_IAccessible);

                        // Get an Lresult for the accessibility Object for this control 
                        //
                        IntPtr punkAcc; 
                        try { 
                            IAccessible iacc = (IAccessible)this.AccessibilityObject;
 
                            if (iacc == null) {
                                // Accessibility is not supported on this control
                                //
                                m.Result = (IntPtr)0; 
                            }
                            else { 
                                // Obtain the Lresult 
                                //
                                punkAcc = Marshal.GetIUnknownForObject(iacc); 

                                try {
                                    m.Result = UnsafeNativeMethods.LresultFromObject(ref IID_IAccessible, m.WParam, punkAcc);
                                } 
                                finally {
                                    Marshal.Release(punkAcc); 
                                } 
                            }
                        } 
                        catch (Exception e) {
                            throw e;
                        }
                        catch { 
                            throw;
                        } 
                    } 
                    else {  // m.lparam != OBJID_CLIENT, so do default message processing
                        DefWndProc(ref m); 
                    }
                    break;

                case NativeMethods.WM_MBUTTONDOWN: 
                case NativeMethods.WM_MBUTTONUP:
                case NativeMethods.WM_MBUTTONDBLCLK: 
                case NativeMethods.WM_NCMOUSEHOVER: 
                case NativeMethods.WM_NCMOUSELEAVE:
                case NativeMethods.WM_MOUSEWHEEL: 
                case NativeMethods.WM_NCMBUTTONDOWN:
                case NativeMethods.WM_NCMBUTTONUP:
                case NativeMethods.WM_NCMBUTTONDBLCLK:
                    // We intentionally eat these messages. 
                    //
                    break; 
 
                case NativeMethods.WM_MOUSEHOVER:
                    if (mouseHandler != null) { 
                        mouseHandler.OnMouseHover(Component);
                    }
                    else {
                        OnMouseHover(); 
                    }
                    break; 
 
                case NativeMethods.WM_MOUSELEAVE:
                    OnMouseLeave(); 
                    BaseWndProc(ref m);
                    break;

                case NativeMethods.WM_NCLBUTTONDBLCLK: 
                case NativeMethods.WM_LBUTTONDBLCLK:
                case NativeMethods.WM_NCRBUTTONDBLCLK: 
                case NativeMethods.WM_RBUTTONDBLCLK: 

                    if ((m.Msg == NativeMethods.WM_NCRBUTTONDBLCLK || m.Msg == NativeMethods.WM_RBUTTONDBLCLK)) { 
                        button = MouseButtons.Right;
                    }
                    else {
                        button = MouseButtons.Left; 
                    }
 
                    if (button == MouseButtons.Left) { 

                        // We handle doubleclick messages, and we also process 
                        // our own simulated double clicks for controls that don't
                        // specify CS_WANTDBLCLKS.
                        //
                        if (mouseHandler != null) { 
                            mouseHandler.OnMouseDoubleClick(Component);
                        } 
                        else { 
                            OnMouseDoubleClick();
                        } 
                    }
                    break;

                case NativeMethods.WM_NCLBUTTONDOWN: 
                case NativeMethods.WM_LBUTTONDOWN:
                case NativeMethods.WM_NCRBUTTONDOWN: 
                case NativeMethods.WM_RBUTTONDOWN: 

                    if ((m.Msg == NativeMethods.WM_NCRBUTTONDOWN || m.Msg == NativeMethods.WM_RBUTTONDOWN)) { 
                        button = MouseButtons.Right;
                    }
                    else {
                        button = MouseButtons.Left; 
                    }
 
                    // We don't really want the focus, but we want to focus the designer. 
                    // Below we handle WM_SETFOCUS and do the right thing.
                    // 
                    NativeMethods.SendMessage(Control.Handle, NativeMethods.WM_SETFOCUS, 0, 0);

                    // We simulate doubleclick for things that don't...
                    // 
                    if (button == MouseButtons.Left && IsDoubleClick(x, y)) {
                        if (mouseHandler != null) { 
                            mouseHandler.OnMouseDoubleClick(Component); 
                        }
                        else { 
                            OnMouseDoubleClick();
                        }
                    }
                    else { 

                        toolPassThrough = false; 
 
                        if (!this.EnableDragRect && button == MouseButtons.Left) {
 
                            if (toolboxSvc == null) {
                                toolboxSvc = (IToolboxService)GetService(typeof(IToolboxService));
                            }
 
                            if (toolboxSvc != null && toolboxSvc.GetSelectedToolboxItem((IDesignerHost)GetService(typeof(IDesignerHost))) != null) {
                                // there is a tool to be dragged, so set passthrough and pass to the parent. 
                                toolPassThrough = true; 
                            }
                        } 
                        else {
                            toolPassThrough = false;
                        }
 

                        if (toolPassThrough) { 
                            NativeMethods.SendMessage(Control.Parent.Handle, m.Msg, m.WParam, (IntPtr)GetParentPointFromLparam(m.LParam)); 
                            return;
                        } 

                        if (mouseHandler != null) {
                            mouseHandler.OnMouseDown(Component, button, x, y);
                        } 
                        else if (button == MouseButtons.Left) {
                            OnMouseDragBegin(x,y); 
 
                        }
                        else if (button == MouseButtons.Right) { 
                            ISelectionService selSvc = (ISelectionService)GetService(typeof(ISelectionService));
                            if (selSvc != null) {
                                selSvc.SetSelectedComponents(new object[] {Component}, SelectionTypes.Primary);
                            } 
                        }
 
                        lastMoveScreenX = x; 
                        lastMoveScreenY = y;
                    } 
                    break;

                case NativeMethods.WM_NCMOUSEMOVE:
                case NativeMethods.WM_MOUSEMOVE: 
                    if (((int)m.WParam & NativeMethods.MK_LBUTTON) != 0) {
                        button = MouseButtons.Left; 
                    } 
                    else if (((int)m.WParam & NativeMethods.MK_RBUTTON) != 0) {
                        button = MouseButtons.Right; 
                        toolPassThrough = false;
                    }
                    else {
                        toolPassThrough = false; 
                    }
 
                    if (lastMoveScreenX != x || lastMoveScreenY != y) { 
                        if (toolPassThrough) {
                            NativeMethods.SendMessage(Control.Parent.Handle, m.Msg, m.WParam, (IntPtr)GetParentPointFromLparam(m.LParam)); 
                            return;
                        }

                        if (mouseHandler != null) { 
                            mouseHandler.OnMouseMove(Component, x, y);
                        } 
                        else if (button == MouseButtons.Left) { 
                            OnMouseDragMove(x, y);
                        } 
                    }
                    lastMoveScreenX = x;
                    lastMoveScreenY = y;
 
                    // VSWhidbey #487865. We eat WM_NCMOUSEMOVE messages, since we don't want the non-client area
                    // of design time controls to repaint on mouse move. 
                    if (m.Msg == NativeMethods.WM_MOUSEMOVE) { 
                        BaseWndProc(ref m);
                    } 

                    break;

                case NativeMethods.WM_NCLBUTTONUP: 
                case NativeMethods.WM_LBUTTONUP:
                case NativeMethods.WM_NCRBUTTONUP: 
                case NativeMethods.WM_RBUTTONUP: 

                    // This is implemented on the base designer for 
                    // UI activation support.
                    //
                    if ((m.Msg == NativeMethods.WM_NCRBUTTONUP || m.Msg == NativeMethods.WM_RBUTTONUP)) {
                        button = MouseButtons.Right; 
                    }
                    else { 
                        button = MouseButtons.Left; 
                    }
 
                    bool moved = mouseDragMoved;

                    // And terminate the drag.
                    // 
                    if (mouseHandler != null) {
                        mouseHandler.OnMouseUp(Component, button); 
                    } 
                    else {
                        if (toolPassThrough) { 
                            NativeMethods.SendMessage(Control.Parent.Handle, m.Msg, m.WParam, (IntPtr)GetParentPointFromLparam(m.LParam));
                            toolPassThrough = false;
                            return;
                        } 

                        if (button == MouseButtons.Left) { 
                            OnMouseDragEnd(false); 
                        }
                    } 

                    // clear any pass through.
                    toolPassThrough = false;
 
                    BaseWndProc(ref m);
                    break; 
 
                case NativeMethods.WM_PRINTCLIENT:
                    { 
                        using (Graphics g = Graphics.FromHdc(m.WParam)) {
                            using (PaintEventArgs e = new PaintEventArgs(g, Control.ClientRectangle)) {
                                DefWndProc(ref m);
                                OnPaintAdornments(e); 
                            }
                        } 
                    } 
                    break;
 
                case NativeMethods.WM_PAINT:
                    // First, save off the update region and
                    // call our base class.
                    // 
                    if (OleDragDropHandler.FreezePainting) {
                        NativeMethods.ValidateRect(m.HWnd, IntPtr.Zero); 
                        break; 
                    }
 
                    if (Control == null) {
                        break;
                    }
 
                    NativeMethods.RECT clip = new NativeMethods.RECT();
                    IntPtr hrgn = NativeMethods.CreateRectRgn(0, 0, 0, 0); 
                    NativeMethods.GetUpdateRgn(m.HWnd, hrgn, false); 
                    NativeMethods.GetUpdateRect(m.HWnd, ref clip, false);
                    Region r = Region.FromHrgn(hrgn); 
                    Rectangle paintRect = Rectangle.Empty;

                    try {
                        // Call the base class to do its own painting. 
                        //
                        if (thrownException == null) { 
                            DefWndProc(ref m); 
                        }
 
                        // Now do our own painting.
                        //
                        Graphics gr = Graphics.FromHwnd(m.HWnd);
                        if (m.HWnd != Control.Handle) { 
                            // Re-map the clip rect we pass to the paint event args
                            // to our child coordinates. 
                            // 
                            NativeMethods.POINT pt = new NativeMethods.POINT();
                            pt.x = 0; 
                            pt.y = 0;
                            NativeMethods.MapWindowPoints(m.HWnd, Control.Handle, pt, 1);
                            gr.TranslateTransform(-pt.x, -pt.y);
 
                            NativeMethods.MapWindowPoints(m.HWnd, Control.Handle, ref clip, 2);
                        } 
 
                        paintRect = new Rectangle(clip.left, clip.top, clip.right-clip.left, clip.bottom-clip.top);
                        PaintEventArgs pevent = new PaintEventArgs(gr, paintRect); 

                        try {
                            gr.Clip = r;
                            if (thrownException == null) { 
                                OnPaintAdornments(pevent);
                            } 
                            else { 
                                UnsafeNativeMethods.PAINTSTRUCT ps = new UnsafeNativeMethods.PAINTSTRUCT();
                                IntPtr dc = UnsafeNativeMethods.BeginPaint(m.HWnd, ref ps); 
                                PaintException(pevent, thrownException);
                                UnsafeNativeMethods.EndPaint(m.HWnd, ref ps);
                            }
                        } 
                        finally {
                            // pevent will dispose the graphics object... no need to do that separately... 
                            // 
                            if (pevent != null) {
                                pevent.Dispose(); 
                            }
                            else {
                                gr.Dispose();
                            } 
                        }
                    } 
                    finally { 
                       r.Dispose();
                       NativeMethods.DeleteObject(hrgn); 
                    }

                    if (OverlayService != null) {
                        //this will allow any Glyphs to re-paint 
                        //after this control and its designer has painted
                        paintRect.Location = Control.PointToScreen(paintRect.Location); 
                        OverlayService.InvalidateOverlays(paintRect); 
                    }
 
                    break;

                case NativeMethods.WM_NCPAINT:
                case NativeMethods.WM_NCACTIVATE: 
                    if (m.Msg == NativeMethods.WM_NCACTIVATE) {
                        DefWndProc(ref m); 
                    } 
                    else if (thrownException == null) {
                        DefWndProc(ref m); 
                    }


                   // For some reason we dont always get an NCPAINT with the WM_NCACTIVATE 
                   // usually this repros with themes on.... this can happen when someone calls RedrawWindow without
                   // the flags to send an NCPAINT.  So that we dont double process this event, our calls 
                   // to redraw window should not have RDW_ERASENOW | RDW_UPDATENOW. 

                    if (OverlayService != null) { 

                        if (Control != null && Control.Size != Control.ClientSize && Control.Parent != null) {
                            // we have a non-client region to invalidate
                            Rectangle controlScreenBounds = new Rectangle(Control.Parent.PointToScreen(Control.Location), Control.Size); 
                            Rectangle clientAreaScreenBounds = new Rectangle(Control.PointToScreen(Point.Empty), Control.ClientSize);
 
                            using (Region nonClient = new Region(controlScreenBounds)) { 
                                nonClient.Exclude(clientAreaScreenBounds);
                                OverlayService.InvalidateOverlays(nonClient); 
                            }
                        }
                    }
                    break; 

                case NativeMethods.WM_SETCURSOR: 
                    // We always handle setting the cursor ourselves. 
                    //
 
                    if (liveRegion) {
                        DefWndProc(ref m);
                        break;
                    } 

                    if (mouseHandler != null) { 
                        mouseHandler.OnSetCursor(Component); 
                    }
                    else { 
                        OnSetCursor();
                    }
                    break;
 
                case NativeMethods.WM_SIZE:
                    if (this.thrownException != null) { 
                        Control.Invalidate(); 
                    }
                    DefWndProc(ref m); 
                    break;
                case NativeMethods.WM_CANCELMODE:
                    // When we get cancelmode (i.e. you tabbed away to another window)
                    // then we want to cancel any pending drag operation! 
                    //
                    OnMouseDragEnd(true); 
                    DefWndProc(ref m); 
                    break;
 
                case NativeMethods.WM_SETFOCUS:
                    // We always eat the focus.
                    //
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
                    break; 

            case NativeMethods.WM_CONTEXTMENU: 
                    if (inContextMenu) {
                        break;
                    }
 
                    // We handle this in addition to a right mouse button.
                    // Why?  Because we often eat the right mouse button, so 
                    // it may never generate a WM_CONTEXTMENU.  However, the 
                    // system may generate one in response to an F-10.
                    // 
                    x = NativeMethods.Util.SignedLOWORD((int)m.LParam);
                    y = NativeMethods.Util.SignedHIWORD((int)m.LParam);

                    ToolStripKeyboardHandlingService keySvc = (ToolStripKeyboardHandlingService)GetService(typeof(ToolStripKeyboardHandlingService)); 
                    bool handled = false;
 
                    if (keySvc != null) 
                    {
                        handled = keySvc.OnContextMenu(x, y); 
                    }

                    if (!handled)
                    { 
                        if (x == -1 && y == -1) {
                            // for shift-F10 
                            Point p = Cursor.Position; 
                            x = p.X;
                            y = p.Y; 
                        }
                        OnContextMenu(x, y);
                    }
                    break; 

                default: 
 
                    if (m.Msg == NativeMethods.WM_MOUSEENTER) {
                        OnMouseEnter(); 
                        BaseWndProc(ref m);
                    }
                    // We eat all key handling to the control.  Controls generally
                    // should not be getting focus anyway, so this shouldn't happen. 
                    // However, we want to prevent this as much as possible.
                    // 
                    else if (m.Msg < NativeMethods.WM_KEYFIRST || m.Msg > NativeMethods.WM_KEYLAST) { 
                        DefWndProc(ref m);
                    } 
                    break;
            }
        }
 
        /// <devdoc>
        ///     This is a subclass window that we attach to all child windows. 
        ///     We use this to disable a child hwnd's UI during design time. 
        /// </devdoc>
 
        private class ChildSubClass : NativeWindow, IDesignerTarget {
            private ControlDesigner designer;

            /// <devdoc> 
            ///     Creates a new ChildSubClass object.  This subclasses
            ///     the given hwnd immediately. 
            /// </devdoc> 

            // AssignHandle calls NativeWindow::OnHandleChanged, but we do not override it so we should be okay 
            [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
            public ChildSubClass(ControlDesigner designer, IntPtr hwnd) {
                this.designer = designer;
                if (designer != null) { 
                    designer.disposingHandler += new EventHandler(this.OnDesignerDisposing);
                } 
                AssignHandle(hwnd); 
            }
 
            void IDesignerTarget.DefWndProc(ref Message m) {
                base.DefWndProc(ref m);
            }
 
            public void Dispose() {
                designer = null; 
            } 

            private void OnDesignerDisposing(object sender, EventArgs e) { 
                Dispose();
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ChildSubClass.WndProc"]/*' /> 
            /// <devdoc>
            ///     Overrides Window's WndProc to process messages. 
            /// </devdoc> 
            protected override void WndProc(ref Message m) {
                if (designer == null) { 
                    DefWndProc(ref m);
                    return;
                }
 
                if (m.Msg == NativeMethods.WM_DESTROY) {
                    designer.RemoveSubclassedWindow(m.HWnd); 
                } 
                if (m.Msg == NativeMethods.WM_PARENTNOTIFY &&
                        NativeMethods.Util.LOWORD((int)m.WParam) == (short)NativeMethods.WM_CREATE) { 
                        designer.HookChildHandles(m.LParam);    // they will get removed from the collection just above
                }

                // We want these messages to go through the designer's WndProc 
                // method, and we want people to be able to do default processing
                // with the designer's DefWndProc.  So, we stuff ourselves into 
                // the designers window target and call their WndProc. 
                //
                IDesignerTarget designerTarget = designer.DesignerTarget; 

                designer.DesignerTarget = this;

                Debug.Assert(m.HWnd == this.Handle, "Message handle differs from target handle"); 

                try { 
                   designer.WndProc(ref m); 
                }
                catch (Exception ex){ 
                    designer.SetUnhandledException(Control.FromChildHandle(m.HWnd), ex);
                }
                catch {
                    Debug.Fail("non CLS-compliant exception"); 
                }
                finally { 
                   // make sure the designer wasn't destroyed 
                   //
                   if (designer != null && designer.Component != null) { 
                       designer.DesignerTarget = designerTarget;
                   }
                }
            } 
        }
 
        /// <devdoc> 
        ///     This is a subclass class that attaches to a control instance.
        ///     Controls can be subclasses by hooking their IWindowTarget 
        ///     interface.  We use this to disable a child hwnd's UI during
        ///     design time.
        /// </devdoc>
        private class ChildWindowTarget : IWindowTarget, IDesignerTarget { 
            private ControlDesigner designer;
            private Control childControl; 
            private IWindowTarget oldWindowTarget; 
            private IntPtr handle = IntPtr.Zero;
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ChildWindowTarget.ChildWindowTarget"]/*' />
            /// <devdoc>
            ///     Creates a new ChildWindowTarget object.  This hooks the
            ///     given control's window target. 
            /// </devdoc>
            public ChildWindowTarget(ControlDesigner designer, Control childControl, IWindowTarget oldWindowTarget) { 
                this.designer = designer; 
                this.childControl = childControl;
                this.oldWindowTarget = oldWindowTarget; 
            }

            public IWindowTarget OldWindowTarget {
                get { 
                    return oldWindowTarget;
                } 
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ChildWindowTarget.DefWndProc"]/*' /> 
            /// <devdoc>
            ///     Causes default window processing for the given message.  We
            ///     just forward this on to the old control target.
            /// </devdoc> 
            public void DefWndProc(ref Message m) {
                oldWindowTarget.OnMessage(ref m); 
            } 

            [SuppressMessage("Microsoft.Usage", "CA2216:DisposableTypesShouldDeclareFinalizer")] 
            public void Dispose() {
                // Do nothing.  We will pick this up through a null DesignerTarget property
                // when we come out of the message loop.
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ChildWindowTarget.OnHandleChange"]/*' /> 
            /// <devdoc> 
            ///      Called when the window handle of the control has changed.
            /// </devdoc> 
            public void OnHandleChange(IntPtr newHandle) {
                handle = newHandle;
                oldWindowTarget.OnHandleChange(newHandle);
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ChildWindowTarget.OnMessage"]/*' /> 
            /// <devdoc> 
            ///      Called to do control-specific processing for this window.
            /// </devdoc> 
            public void OnMessage(ref Message m) {
                // If the designer has jumped ship, the continue
                // partying on messages, but send them back to the original control.
                if (designer.Component == null) { 
                    oldWindowTarget.OnMessage(ref m);
                    return; 
                } 

                // We want these messages to go through the designer's WndProc 
                // method, and we want people to be able to do default processing
                // with the designer's DefWndProc.  So, we stuff the old window
                // target into the designer's target and then call their
                // WndProc. 
                //
                IDesignerTarget designerTarget = designer.DesignerTarget; 
 
                designer.DesignerTarget = this;
 
                try {
                    designer.WndProc(ref m);
                }
                catch (Exception ex) { 
                    designer.SetUnhandledException(childControl, ex);
                } 
                catch { 
                }
                finally { 

                    // If the designer disposed us, then we should follow suit.
                    //
                    if (designer.DesignerTarget == null) { 
                        designerTarget.Dispose();
                    } 
                    else { 
                        designer.DesignerTarget = designerTarget;
                    } 

                    // ASURT 45655: Controls (primarily RichEdit) will register themselves as
                    // drag-drop source/targets when they are instantiated. Normally, when they
                    // are being designed, we will RevokeDragDrop() in their designers. The problem 
                    // occurs when these controls are inside a UserControl. At that time, we do not
                    // have a designer for these controls, and they prevent the ParentControlDesigner's 
                    // drag-drop from working. What we do is to loop through all child controls that 
                    // do not have a designer (in HookChildControls()), and RevokeDragDrop() after
                    // their handles have been created. 
                    //
                    if (m.Msg == NativeMethods.WM_CREATE) {
                        Debug.Assert(handle != IntPtr.Zero, "Handle for control not created");
                        NativeMethods.RevokeDragDrop(handle); 
                    }
                } 
            } 
        }
 
        /// <devdoc>
        ///     This class is the interface the designer will use to funnel messages
        ///     back to the control.
        /// </devdoc> 
        private interface IDesignerTarget : IDisposable {
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.IDesignerTarget.DefWndProc"]/*' /> 
            /// <devdoc> 
            ///     Causes default window processing for the given message.  We
            ///     just forward this on to the old control target. 
            /// </devdoc>
            void DefWndProc(ref Message m);
        }
 
        /// <devdoc>
        ///     This class replaces Control's window target, which effectively subclasses 
        ///     the control in a handle-independent way. 
        /// </devdoc>
        private class DesignerWindowTarget : IWindowTarget, IDesignerTarget, IDisposable { 
            internal ControlDesigner designer;
            internal IWindowTarget oldTarget;

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerWindowTarget.DesignerWindowTarget"]/*' /> 
            /// <devdoc>
            ///     Creates a new DesignerTarget object. 
            /// </devdoc> 
            public DesignerWindowTarget(ControlDesigner designer) {
 
                Control control = designer.Control;

                this.designer = designer;
                this.oldTarget = control.WindowTarget; 
                control.WindowTarget = this;
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerWindowTarget.DefWndProc"]/*' />
            /// <devdoc> 
            ///     Causes default window processing for the given message.  We
            ///     just forward this on to the old control target.
            /// </devdoc>
            public void DefWndProc(ref Message m) { 
                oldTarget.OnMessage(ref m);
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerWindowTarget.Dispose"]/*' />
            /// <devdoc> 
            ///      Disposes this window target.  This re-establishes the
            ///      prior window target.
            /// </devdoc>
            public void Dispose() { 
                if (designer != null) {
                    designer.Control.WindowTarget = oldTarget; 
                    designer = null; 
                }
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerWindowTarget.OnHandleChange"]/*' />
            /// <devdoc>
            ///      Called when the window handle of the control has changed. 
            /// </devdoc>
            public void OnHandleChange(IntPtr newHandle) { 
                oldTarget.OnHandleChange(newHandle); 
                if (newHandle != IntPtr.Zero) {
                    designer.OnHandleChange(); 
                }
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerWindowTarget.OnMessage"]/*' /> 
            /// <devdoc>
            ///      Called to do control-specific processing for this window. 
            /// </devdoc> 
            public void OnMessage(ref Message m) {
                // We want these messages to go through the designer's WndProc 
                // method, and we want people to be able to do default processing
                // with the designer's DefWndProc.  So, we stuff ourselves into
                // the designers window target and call their WndProc.
                // 
                ControlDesigner currentDesigner = designer;
 
                if (currentDesigner != null) { 
                    IDesignerTarget designerTarget = currentDesigner.DesignerTarget;
                    currentDesigner.DesignerTarget = this; 

                   try {
                       currentDesigner.WndProc(ref m);
                   } 
                   catch (Exception ex) {
                       designer.SetUnhandledException(designer.Control,  ex); 
                   } 
                   catch {
                        Debug.Fail("non-CLS compliant exception"); 
                   }
                   finally {
                       if (currentDesigner != null) {
                            currentDesigner.DesignerTarget = designerTarget; 
                       }
                   } 
                } 
                else {
                    DefWndProc(ref m); 
                }
            }
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesignerAccessibleObject"]/*' />
        [ComVisible(true)] 
        public class ControlDesignerAccessibleObject : AccessibleObject { 

            private ControlDesigner designer = null; 
            private Control control = null;
            private IDesignerHost host = null;
            private ISelectionService selSvc = null;
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.ControlDesignerAccessibleObject"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public ControlDesignerAccessibleObject(ControlDesigner designer, Control control) { 
                this.designer = designer;
                this.control = control;
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Bounds"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public override Rectangle Bounds { 
                get {
                    return control.AccessibilityObject.Bounds;
                }
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Description"]/*' /> 
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public override string Description {
                get {
                    return control.AccessibilityObject.Description;
                } 
            }
 
            private IDesignerHost DesignerHost { 
                get {
                    if (host == null) { 
                        host = (IDesignerHost)designer.GetService(typeof(IDesignerHost));
                    }
                    return host;
                } 
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.DefaultAction"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public override string DefaultAction {
                get {
                    return ""; 
                }
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Name"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public override string Name {
                get { 
                    return control.Name;
                } 
            } 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Parent"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public override AccessibleObject Parent { 
                get {
                    return control.AccessibilityObject.Parent; 
                } 
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Role"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public override AccessibleRole Role {
                get { 
                    return control.AccessibilityObject.Role; 
                }
            } 

            private ISelectionService SelectionService {
                get {
                    if (selSvc == null) { 
                        selSvc = (ISelectionService)designer.GetService(typeof(ISelectionService));
                    } 
 
                    return selSvc;
                } 
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.State"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc> 
            public override AccessibleStates State { 
                get {
                    AccessibleStates state = control.AccessibilityObject.State; 

                    ISelectionService s = SelectionService;
                    if (s != null) {
                        if (s.GetComponentSelected(this.control)) { 
                            state |= AccessibleStates.Selected;
                        } 
                        if (s.PrimarySelection == this.control) { 
                            state |= AccessibleStates.Focused;
                        } 
                    }

                    return state;
                } 
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.Value"]/*' /> 
            /// <devdoc>
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public override string Value {
                get {
                    return control.AccessibilityObject.Value; 
                }
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ControlDesignerAccessibleObject.GetChild"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public override AccessibleObject GetChild(int index) {
                Debug.WriteLineIf(CompModSwitches.MSAA.TraceInfo, "ControlDesignerAccessibleObject.GetChild(" + index.ToString(CultureInfo.InvariantCulture) + ")"); 

                Control.ControlAccessibleObject childAccObj = control.AccessibilityObject.GetChild(index) as Control.ControlAccessibleObject; 
                if (childAccObj != null) { 
                    AccessibleObject cao = GetDesignerAccessibleObject(childAccObj);
                    if (cao != null) { 
                        return cao;
                    }
                }
 
                return control.AccessibilityObject.GetChild(index);
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesignerAccessibleObject.GetChildCount"]/*' />
            public override int GetChildCount() { 
                return control.AccessibilityObject.GetChildCount();
            }
            private AccessibleObject GetDesignerAccessibleObject(Control.ControlAccessibleObject cao) {
                if (cao == null) { 
                    return null;
                } 
                ControlDesigner ctlDesigner = DesignerHost.GetDesigner(cao.Owner) as ControlDesigner; 
                if (ctlDesigner != null) {
                    return ctlDesigner.AccessibilityObject; 
                }
                return null;
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesignerAccessibleObject.GetFocused"]/*' />
            public override AccessibleObject GetFocused() { 
                if ((this.State & AccessibleStates.Focused) != 0) { 
                    return this;
                } 
                return base.GetFocused();
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesignerAccessibleObject.GetSelected"]/*' /> 
            public override AccessibleObject GetSelected() {
                if ((this.State & AccessibleStates.Selected) != 0) { 
                    return this; 
                }
                return base.GetFocused(); 
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesignerAccessibleObject.HitTest"]/*' />
            public override AccessibleObject HitTest(int x, int y) { 
                return control.AccessibilityObject.HitTest(x, y);
            } 
        } 

        [ListBindable(false)] 
        [DesignerSerializer(typeof(DesignerControlCollectionCodeDomSerializer), typeof(CodeDomSerializer))]
        internal class DesignerControlCollection : Control.ControlCollection, IList {

            Control.ControlCollection realCollection; 

            public DesignerControlCollection(Control owner) : base(owner) { 
                this.realCollection = owner.Controls; 
            }
 
             /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.Count"]/*' />
            /// <devdoc>
            ///     Retrieves the number of child controls.
            /// </devdoc> 
            public override int Count {
                get { 
                    return realCollection.Count; 
                }
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.ICollection.SyncRoot"]/*' />
            /// <internalonly/>
            object ICollection.SyncRoot { 
                get {
                    return this; 
                } 
            }
 
            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.ICollection.IsSynchronized"]/*' />
            /// <internalonly/>
            bool ICollection.IsSynchronized {
                get { 
                    return false;
                } 
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.IsFixedSize"]/*' /> 
            /// <internalonly/>
            bool IList.IsFixedSize {
                get {
                    return false; 
                }
            } 
 
            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.IsReadOnly"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public new bool IsReadOnly {
                get { 
                    return realCollection.IsReadOnly;
                } 
            } 

 
            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.Add"]/*' />
            /// <internalonly/>
            int IList.Add(object control) {
                return ((IList)realCollection).Add(control); 
            }
 
            public override void Add(Control c) { 
                realCollection.Add(c);
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.AddRange"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para> 
            /// </devdoc>
            public override void AddRange(Control[] controls) { 
                realCollection.AddRange(controls); 
            }
 
            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.Contains"]/*' />
            /// <internalonly/>
            bool IList.Contains(object control) {
                return ((IList)realCollection).Contains(control); 
            }
 
            public new void CopyTo(Array dest, int index) { 
                realCollection.CopyTo(dest, index);
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.Equals"]/*' />
            /// <internalonly/>
            public override bool Equals(object other) { 
                return realCollection.Equals(other);
            } 
 
            public new IEnumerator GetEnumerator() {
               return realCollection.GetEnumerator(); 
            }

            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.GetHashCode"]/*' />
            /// <internalonly/> 
            public override int GetHashCode() {
                return realCollection.GetHashCode(); 
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.IndexOf"]/*' /> 
            /// <internalonly/>
            int IList.IndexOf(object control) {
                return ((IList)realCollection).IndexOf(control);
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.Insert"]/*' /> 
            /// <internalonly/> 
            void IList.Insert(int index, object value) {
                ((IList)realCollection).Insert(index, value); 
            }

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.Remove"]/*' />
            /// <internalonly/> 
            void IList.Remove(object control) {
                ((IList)realCollection).Remove(control); 
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.Remove"]/*' /> 
            /// <internalonly/>
            void IList.RemoveAt(int index) {
                ((IList)realCollection).RemoveAt(index);
            } 

            /// <include file='doc\Control.uex' path='docs/doc[@for="ControlCollection.IList.this"]/*' /> 
            /// <internalonly/> 
            object IList.this[int index] {
                get { 
                    return ((IList)realCollection)[index];
                }
                set {
                    throw new NotSupportedException(); 
                }
            } 
 
            public override int GetChildIndex(Control child, bool throwException) {
                return realCollection.GetChildIndex(child,throwException); 
            }

            // we also need to redirect this guy
            public override void SetChildIndex(Control child, int newIndex) { 
                realCollection.SetChildIndex(child, newIndex);
            } 
 
            /// <include file='doc\Control.uex' path='docs/doc[@for="Control.ControlCollection.Clear"]/*' />
            /// <devdoc> 
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public override void Clear() {
 
                // only remove the sited non-inherited components
                // 
                for (int i = realCollection.Count - 1; i >= 0; i--) { 
                    if (realCollection[i] != null &&
                        realCollection[i].Site != null && 
                        TypeDescriptor.GetAttributes(realCollection[i]).Contains(InheritanceAttribute.NotInherited)) {
                        realCollection.RemoveAt(i);
                    }
                } 
            }
        } 
 
        // Custom code dom serializer for the DesignerControlCollection. We need this so we can filter out controls
        // that aren't sited in the host's container. 
        internal class DesignerControlCollectionCodeDomSerializer : CollectionCodeDomSerializer {
            protected override object SerializeCollection(IDesignerSerializationManager manager, CodeExpression targetExpression, Type targetType, ICollection originalCollection, ICollection valuesToSerialize) {
                ArrayList subset = new ArrayList();
 
                if (valuesToSerialize != null && valuesToSerialize.Count > 0) {
                    foreach (object val in valuesToSerialize) { 
                        IComponent comp = val as IComponent; 

                        if (comp != null && comp.Site != null && !(comp.Site is INestedSite)) { 
                            subset.Add(comp);
                        }
                    }
                } 

                return base.SerializeCollection(manager, targetExpression, targetType, originalCollection, subset); 
            } 
        }
 
        /// <devdoc>
        ///     This class is used to provide the 'dock in parent' or 'undock in parent'
        ///     designer action item.
        /// </devdoc> 
        private class DockingActionList : DesignerActionList {
            private ControlDesigner _designer; 
            private IDesignerHost   _host; 
            /// <devdoc>
            ///     Caches off the localized name of our action 
            /// </devdoc>
            public DockingActionList(ControlDesigner owner) : base(owner.Component) {
                _designer = owner;
                _host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
            }
 
            private string GetActionName() { 
                PropertyDescriptor dockProp = TypeDescriptor.GetProperties(Component)["Dock"];
                if (dockProp != null) { 
                    DockStyle dockStyle = (DockStyle)dockProp.GetValue(Component);
                    if(dockStyle == DockStyle.Fill) {
                        return SR.GetString(SR.DesignerShortcutUndockInParent);
                    } else { 
                        return SR.GetString(SR.DesignerShortcutDockInParent);
                    } 
                } 
                return null;
            } 

            /// <devdoc>
            ///     Returns our undock or dock item
            /// </devdoc> 
            public override DesignerActionItemCollection GetSortedActionItems()     {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                string actionName = GetActionName(); 
                if(actionName != null) {
                    items.Add(new DesignerActionVerbItem(new DesignerVerb(GetActionName(), OnDockActionClick))); 
                }
                return items;
            }
 

            /// <devdoc> 
            ///      Called when this designer's 'DockInParent' or 'Undock' designer action 
            ///      has been clicked.
            /// </devdoc> 
            private void OnDockActionClick(object sender, EventArgs e) {
                DesignerVerb designerVerb = sender as DesignerVerb;
                if (designerVerb != null && _host != null) {
                    using (DesignerTransaction t = _host.CreateTransaction(designerVerb.Text)) { 
                        //set the dock prop to DockStyle.Fill
                        PropertyDescriptor dockProp = TypeDescriptor.GetProperties(Component)["Dock"]; 
                        DockStyle dockStyle = (DockStyle)dockProp.GetValue(Component); 
                        if(dockStyle == DockStyle.Fill) {
                            dockProp.SetValue(Component, DockStyle.None); 
                        } else {
                            dockProp.SetValue(Component, DockStyle.Fill);
                        }
                        t.Commit(); 
                    }
                } 
            } 

        } 

        /// <devdoc>
        ///     This TransparentBehavior is associated with the BodyGlyph for
        ///     this ControlDesigner.  When the BehaviorService hittests a glyph 
        ///     w/a TransparentBehavior, all messages will be passed through
        ///     the BehaviorService directly to the ControlDesigner. 
        ///     During a Drag operation, when the BehaviorService hittests 
        /// </devdoc>
        internal class TransparentBehavior : System.Windows.Forms.Design.Behavior.Behavior { 

            ControlDesigner designer;//the related ControlDesigner

            Rectangle controlRect = Rectangle.Empty;//the client rectangle of the related control in screen coordinates. Used to check if we can drop. 

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.TransparentBehavior"]/*' /> 
            /// <devdoc> 
            ///     Constructor that accepts the related ControlDesigner.
            /// </devdoc> 
            internal TransparentBehavior(ControlDesigner designer) {
                this.designer = designer;
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.IsTransparent"]/*' />
            /// <devdoc> 
            ///     This property performs a hit test on the ControlDesigner 
            ///     to determine if the BodyGlyph should return '-1' for
            ///     hit testing (letting all messages pass directly to the 
            ///     the control).
            /// </devdoc>
            internal bool IsTransparent(Point p) {
                return designer.GetHitTest(p); 
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragDrop"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner.
            /// </devdoc>
            public override void OnDragDrop(Glyph g, DragEventArgs e) {
                controlRect = Rectangle.Empty; 
                designer.OnDragDrop(e);
            } 
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragEnter"]/*' />
            /// <devdoc> 
            ///     Forwards DragDrop notification from the BehaviorService to
            ///     the related ControlDesigner.
            /// </devdoc>
            public override void OnDragEnter(Glyph g, DragEventArgs e) { 
                if (designer != null && designer.Control != null) {
                    controlRect = designer.Control.RectangleToScreen(designer.Control.ClientRectangle); 
                } 

                designer.OnDragEnter(e); 
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragLeave"]/*' />
            /// <devdoc> 
            ///     Forwards DragDrop notification from the BehaviorService to
            ///     the related ControlDesigner. 
            /// </devdoc> 
            public override void OnDragLeave(Glyph g, EventArgs e) {
                controlRect = Rectangle.Empty; 
                designer.OnDragLeave(e);
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragOver"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner. 
            /// </devdoc>
            public override void OnDragOver(Glyph g, DragEventArgs e) { 
                // If we are not over a valid drop area, then do not allow the drag/drop

                //VSWhidbey# 364083. Now that all dragging/dropping is done via
                //the behavior service and adorner window, we have to do our own 
                //validation, and cannot rely on the OS to do it for us.
                if (e != null && controlRect != Rectangle.Empty && !controlRect.Contains(new Point(e.X, e.Y))) { 
                    e.Effect = DragDropEffects.None; 
                    return;
                } 

                designer.OnDragOver(e);
            }
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragOver"]/*' />
            /// <devdoc> 
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner.
            /// </devdoc> 
            public override void OnGiveFeedback(Glyph g, GiveFeedbackEventArgs e) {
                designer.OnGiveFeedback(e);
            }
        } 

        private class CanResetSizePropertyDescriptor : PropertyDescriptor 
        { 

            private PropertyDescriptor    _basePropDesc; 

            public CanResetSizePropertyDescriptor (PropertyDescriptor pd) : base(pd)
            {
                this._basePropDesc = pd; 
            }
 
            public override Type ComponentType 
            {
                get 
                {
                    return _basePropDesc.ComponentType;
                }
            } 

            public override string DisplayName 
            { 
                get {
                    return _basePropDesc.DisplayName; 
                }
            }

            public override bool IsReadOnly 
            {
                get 
                { 
                    return _basePropDesc.IsReadOnly;
                } 
            }

            public override Type PropertyType
            { 
                get
                { 
                    return _basePropDesc.PropertyType; 
                }
            } 


            public override bool CanResetValue(object component)
            { 
                // VSWhidbey 379297 -- since we can't get to the DefaultSize property, we use the existing
                // ShouldSerialize logic. 
                return _basePropDesc.ShouldSerializeValue(component); 
            }
 
            public override object GetValue(object component)
            {
                return _basePropDesc.GetValue(component);
            } 

            public override void ResetValue(object component) 
            { 
                _basePropDesc.ResetValue(component);
            } 


            public override void SetValue(object component, object value)
            { 
                _basePropDesc.SetValue(component, value);
            } 
 
            public override bool ShouldSerializeValue(object component)
            { 
                // we always want to serialize values.
                return true;
            }
        } 

    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
