//------------------------------------------------------------------------------ 
// <copyright file="AxHostDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Collections; 
    using System.Design;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using Hashtable = System.Collections.Hashtable;
    using IDictionaryEnumerator = System.Collections.IDictionaryEnumerator; 
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.Windows.Forms; 
    using System.Drawing;
    using System.ComponentModel.Design;
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior; 
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis; 
 
    /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner"]/*' />
    /// <devdoc> 
    ///    <para> Provides design time behavior for the AxHost class. AxHost
    ///       is used to host ActiveX controls.</para>
    /// </devdoc>
    internal class AxHostDesigner : ControlDesigner { 
        private AxHost axHost;
        private EventHandler handler; 
        private bool foundEdit = false; 
        private bool foundAbout = false;
        private bool foundProperties = false; 
        private bool dragdropRevoked = false;
        private Size defaultSize = Size.Empty;

        private const int OLEIVERB_UIACTIVATE = -4; 
        private const int HOSTVERB_ABOUT = 2;
        private const int HOSTVERB_PROPERTIES = 1; 
        private const int HOSTVERB_EDIT = 3; 

        private static readonly HostVerbData EditVerbData = new HostVerbData(SR.GetString(SR.AXEdit), HOSTVERB_EDIT); 
        private static readonly HostVerbData PropertiesVerbData = new HostVerbData(SR.GetString(SR.AXProperties), HOSTVERB_PROPERTIES);
        private static readonly HostVerbData AboutVerbData = new HostVerbData(SR.GetString(SR.AXAbout), HOSTVERB_ABOUT);

        private static TraceSwitch AxHostDesignerSwitch     = new TraceSwitch("AxHostDesigner", "ActiveX Designer Trace"); 

        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.AxHostDesigner"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Windows.Forms.Design.AxHostDesigner'/> class. 
        ///    </para>
        /// </devdoc>
        public AxHostDesigner() {
            handler = new EventHandler(this.OnVerb); 
            AutoResizeHandles = true;
        } 
 
        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.SelectionStyle"]/*' />
        /// <devdoc> 
        ///     This property allows the AxHost class to modify our selection style.  It provides three levels
        ///     of selection:  0 (not selected), 1 (selected) and 2 (selected UI active).
        /// </devdoc>
        private int SelectionStyle { 
            get {
                // we don't implement GET 
                return 0; 
            }
            set { 
                Debug.Fail("How did we get here?");
            }
        }
 
        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.GetOleVerbs"]/*' />
        public override DesignerVerbCollection Verbs { 
            get { 
                DesignerVerbCollection l = new DesignerVerbCollection();
                GetOleVerbs(l); 
                /*
                if (!foundEdit && (((AxHost)axHost).OCXFlags & AxHost.AxFlags.PREVENT_EDIT_MODE) == 0) {
                    l.Add(new HostVerb(EditVerbData, handler));
                } 
                if ((((AxHost)axHost).OCXFlags & AxHost.AxFlags.INCLUDE_PROPERTIES_VERB) != 0 && ((AxHost)axHost).HasPropertyPages()) {
                    l.Add(new HostVerb(PropertiesVerbData, handler)); 
                } 
                */
                if (!foundAbout && ((AxHost)axHost).HasAboutBox) { 
                    l.Add(new HostVerb(AboutVerbData, handler));
                }
                return l;
            } 
        }
 
        /// <devdoc> 
        ///     Retrieves the default dimensions for the given component class.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        private static Size GetDefaultSize(IComponent component) {

            Size size = Size.Empty; 
            DefaultValueAttribute sizeAttr = null;
 
            //Check to see if the control is AutoSized. VSWhidbey #416721 
            PropertyDescriptor prop = TypeDescriptor.GetProperties(component)["AutoSize"];
 
            if (prop != null &&
                !(prop.Attributes.Contains(DesignerSerializationVisibilityAttribute.Hidden) ||
                  prop.Attributes.Contains(BrowsableAttribute.No))) {
                bool autoSize = (bool)prop.GetValue(component); 
                if (autoSize) {
                    prop = TypeDescriptor.GetProperties(component)["PreferredSize"]; 
                    if (prop != null) { 
                        size = (Size)prop.GetValue(component);
                        if (size != Size.Empty) { 
                            return size;
                        }
                    }
                } 
            }
 
            // attempt to get the size property of our component 
            //
            prop = TypeDescriptor.GetProperties(component)["Size"]; 

            if (prop != null) {

                // first, let's see if we can get a valid size... 
                size = (Size)prop.GetValue(component);
 
                // ...if not, we'll see if there's a default size attribute... 
                if (size.Width <= 0 || size.Height <= 0) {
                    sizeAttr = (DefaultValueAttribute)prop.Attributes[typeof(DefaultValueAttribute)]; 
                    if (sizeAttr != null) {
                        return((Size)sizeAttr.Value);
                    }
                } 
                else {
                    return size; 
                } 
            }
 
            // Couldn't get the size or a def size attrib, returning 75,23...
            //
            return(new Size(75, 23));
        } 

        public virtual void GetOleVerbs(DesignerVerbCollection rval) { 
            NativeMethods.IEnumOLEVERB verbEnum = null; 
            NativeMethods.IOleObject obj = axHost.GetOcx() as NativeMethods.IOleObject;
            if (obj == null || NativeMethods.Failed(obj.EnumVerbs(out verbEnum))) { 
                return;
            }

            Debug.Assert(verbEnum != null, "must have return value"); 
            if (verbEnum == null) return;
            int[] fetched = new int[1]; 
            NativeMethods.tagOLEVERB oleVerb = new NativeMethods.tagOLEVERB(); 

            foundEdit = false; 
            foundAbout = false;
            foundProperties = false;

            while (true) { 
                fetched[0] = 0;
                oleVerb.lpszVerbName = null; 
                int hr = verbEnum.Next(1, oleVerb, fetched); 
                if (hr == NativeMethods.S_FALSE) {
                    break; 
                }
                else if (NativeMethods.Failed(hr)) {
                    Debug.Fail("Failed to enumerate through enums: " + hr.ToString(CultureInfo.InvariantCulture));
                    break; 
                }
 
                // Believe it or not, some controls, notably the shdocview control, dont' return 
                // S_FALSE and neither do they set fetched to 1.  So, we need to comment out what's
                // below to maintain compatibility with Visual Basic. 
                //                 if (fetched[0] != 1) {
                //                     Debug.fail("gotta have our 1 verb...");
                //                     break;
                //                 } 
                if ((oleVerb.grfAttribs & NativeMethods.ActiveX.OLEVERBATTRIB_ONCONTAINERMENU) != 0) {
                    foundEdit = foundEdit || oleVerb.lVerb == OLEIVERB_UIACTIVATE; 
                    foundAbout = foundAbout || oleVerb.lVerb == HOSTVERB_ABOUT; 
                    foundProperties = foundProperties || oleVerb.lVerb == HOSTVERB_PROPERTIES;
 
                    rval.Add(new HostVerb(new OleVerbData(oleVerb), handler));
                }
            }
        } 

        protected override bool GetHitTest(Point p) { 
            return axHost.EditMode; 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetBodyGlyph"]/*' />
        /// <devdoc>
        ///     Returns a 'BodyGlyph' representing the bounds of this control.
        ///     The BodyGlyph is responsible for hit testing the related CtrlDes 
        ///     and forwarding messages directly to the designer.
        /// </devdoc> 
        protected override ControlBodyGlyph GetControlGlyph(GlyphSelectionType selType) { 

            Cursor cursor = Cursors.Default; 

            //only selected moveable controls get the sizeall cursor
            if (selType != GlyphSelectionType.NotSelected &&
               (SelectionRules & SelectionRules.Moveable) != 0) { 
                cursor = Cursors.SizeAll;
            } 
 
            //get the correctly translated bounds
            Point loc = BehaviorService.ControlToAdornerWindow((Control)Component); 
            Rectangle translatedBounds = new Rectangle(loc, ((Control)Component).Size);

            //create our glyph, and set its cursor appropriately
            ControlBodyGlyph g = new ControlBodyGlyph(translatedBounds, cursor, Control, this); 
            return g;
        } 
 
        public override void Initialize(IComponent component) {
            base.Initialize(component); 
            axHost = (AxHost)component;
        }

 

        private void OnControlAdded(object sender, System.Windows.Forms.ControlEventArgs e) 
 		{ 
            if (e.Control == axHost)
            { 
                // Get the Size again as it would have changed when the control gets added to the parent (ActiveX controls)
                defaultSize = GetDefaultSize(axHost);
            }
		} 

 
        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.OnCreateHandle"]/*' /> 
        /// <devdoc>
        ///      This is called immediately after the control handle has been created. 
        /// </devdoc>
        protected override void OnCreateHandle() {
            base.OnCreateHandle();
 
            //Application.OLERequired();
            //int n = NativeMethods.RevokeDragDrop(Control.Handle); 
        } 

        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.InitializeNewComponent"]/*' /> 
        /// <devdoc>
        ///     Called when the designer is intialized.  This allows the designer to provide some
        ///     meaningful default values in the component.  The default implementation of this
        ///     sets the components's default property to it's name, if that property is a string. 
        /// </devdoc>
        public override void InitializeNewComponent(IDictionary defaultValues) { 
            try { 
                Control parent = defaultValues["Parent"] as Control;
                if (parent != null) 
                {
                    parent.ControlAdded += new System.Windows.Forms.ControlEventHandler(OnControlAdded);
                }
 
                base.InitializeNewComponent(defaultValues);
 
                if (parent != null) 
                {
                    parent.ControlAdded -= new System.Windows.Forms.ControlEventHandler(OnControlAdded); 
                }

                // We have to put the newSize here after the control is being added. This is because of a change in
                // ParentControlDesigner. We need to recalculate the Size and if it has changed then put in the new Size 
                bool hasSize = (defaultValues != null && defaultValues.Contains("Size"));
                // VsWhidbey : 470669 
                // ActiveX controls change there bounds when the control gets created through the above call which adds the controls to the 
                // parent. Hence re-check the bounds and use the new one.
                // If we are using default.. 
                if (!hasSize && defaultSize != Size.Empty)
                {

                    PropertyDescriptorCollection props = TypeDescriptor.GetProperties(axHost); 
                    if (props != null) {
                         PropertyDescriptor prop = props["Size"]; 
                         if (prop != null) { 
                             prop.SetValue(axHost, new Size(defaultSize.Width, defaultSize.Height));
                         } 
                    }
                }

            } 
            catch {
                // The ControlDesigner tries to set the Text property of the control when 
                // it creates the site. ActiveX controls generally don't like that, causing an 
                // exception to be thrown. We now catch these exceptions in the AxHostDesigner
                // and continue on. 
            }
        }

        public virtual void OnVerb(object sender, EventArgs evevent) { 
            if (sender != null && sender is HostVerb) {
                HostVerb vd = (HostVerb)sender; 
                vd.Invoke((AxHost)axHost); 
            }
            else { 
                Debug.Fail("Bad verb invocation.");
            }
        }
 
        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.PreFilterProperties"]/*' />
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
            object enabledProp = properties["Enabled"]; 

            base.PreFilterProperties(properties); 

            if (enabledProp != null) {
                properties["Enabled"] = enabledProp;
            } 

            // Add a property to handle selection from ActiveX 
            // 
            properties["SelectionStyle"] = TypeDescriptor.CreateProperty(typeof(AxHostDesigner), "SelectionStyle",
                typeof(int), 
                BrowsableAttribute.No,
                DesignerSerializationVisibilityAttribute.Hidden,
                DesignOnlyAttribute.Yes);
        } 

        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.WndProc"]/*' /> 
        /// <devdoc> 
        ///     This method should be called by the extending designer for each message
        ///     the control would normally receive.  This allows the designer to pre-process 
        ///     messages before allowing them to be routed to the control.
        /// </devdoc>
        protected override void WndProc(ref Message m) {
            switch (m.Msg) { 
                case NativeMethods.WM_PARENTNOTIFY:
                    if ((int)m.WParam == NativeMethods.WM_CREATE) { 
                        HookChildHandles(m.LParam); 
                    }
 
                    base.WndProc(ref m);
                    break;

                case NativeMethods.WM_NCHITTEST: 
                    // ASURT 66102 The ShDocVw control registers itself as a drop-target even in design mode.
                    // We take the first chance to unregister that, so we can perform our design-time behavior 
                    // irrespective of what the control wants to do. 
                    //
                    if (!dragdropRevoked) { 
                        int n = NativeMethods.RevokeDragDrop(Control.Handle);
                        dragdropRevoked = (n == NativeMethods.S_OK);
                    }
 
                    // Some ActiveX controls return non-HTCLIENT return values for NC_HITTEST, which
                    // causes the message to go to our parent.  We want the control's designer to get 
                    // these messages so we change the result to HTCLIENT. 
                    //
                    base.WndProc(ref m); 
                    if (((int)m.Result == NativeMethods.HTTRANSPARENT) || ((int)m.Result > NativeMethods.HTCLIENT)) {
                        Debug.WriteLineIf(AxHostDesignerSwitch.TraceVerbose, "Converting NCHITTEST result from : " + (int)m.Result + " to HTCLIENT");
                        m.Result = (IntPtr)NativeMethods.HTCLIENT;
                    } 
                    break;
 
                default: 
                    base.WndProc(ref m);
                    break; 
            }
        }

        /** 
          * @security(checkClassLinking=on)
          */ 
        private class HostVerb : DesignerVerb { 
            private HostVerbData data;
 
            public HostVerb(HostVerbData data, EventHandler handler) : base(data.ToString(), handler) {
                this.data = data;
            }
 
            public void Invoke(AxHost host) {
                data.Execute(host); 
            } 
        }
 
        /**
         * @security(checkClassLinking=on)
         */
        private class HostVerbData { 
            internal readonly string name;
            internal readonly int id; 
 
            internal HostVerbData(string name, int id) {
                this.name = name; 
                this.id = id;
            }

            public override string ToString() { 
                return name;
            } 
 
            internal virtual void Execute(AxHost ctl) {
                switch (id) { 
                    case HOSTVERB_PROPERTIES:
                        ctl.ShowPropertyPages();
                        break;
                    case HOSTVERB_EDIT: 
                        ctl.InvokeEditMode();
                        break; 
                    case HOSTVERB_ABOUT: 
                        ctl.ShowAboutBox();
                        break; 
                    default:
                        Debug.Fail("bad verb id in HostVerb");
                        break;
                } 
            }
        } 
 
        /**
         * @security(checkClassLinking=on) 
         */
        private class OleVerbData : HostVerbData {
            private readonly bool dirties;
 
            internal OleVerbData(NativeMethods.tagOLEVERB oleVerb)
            : base(SR.GetString(SR.AXVerbPrefix) + oleVerb.lpszVerbName, oleVerb.lVerb) { 
                this.dirties = (oleVerb.grfAttribs & NativeMethods.ActiveX.OLEVERBATTRIB_NEVERDIRTIES) == 0; 
            }
 
            internal override void Execute(AxHost ctl) {
                if (dirties) ctl.MakeDirty();
                ctl.DoVerb(id);
            } 
        }
 
        /// <devdoc> 
        ///     This TransparentBehavior is associated with the BodyGlyph for
        ///     this ControlDesigner.  When the BehaviorService hittests a glyph 
        ///     w/a TransparentBehavior, all messages will be passed through
        ///     the BehaviorService directly to the ControlDesigner.
        ///     During a Drag operation, when the BehaviorService hittests
        /// </devdoc> 
        internal class AxHostDesignerBehavior : System.Windows.Forms.Design.Behavior.Behavior {
 
            AxHostDesigner designer;//the related ControlDesigner 
            BehaviorService bs;
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.TransparentBehavior"]/*' />
            /// <devdoc>
            ///     Constructor that accepts the related ControlDesigner.
            /// </devdoc> 
            internal AxHostDesignerBehavior(AxHostDesigner designer) {
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

            /// <devdoc>
            ///     Translates an adorner coordinate to a control coordinate.
            /// </devdoc> 
            private Point AdornerToControl(Point ptAdorner) {
                if (bs == null) { 
                    bs = (BehaviorService)designer.GetService(typeof(BehaviorService)); 
                }
 
                if (bs != null) {
                    Point pt = bs.AdornerWindowToScreen();
                    pt.X += ptAdorner.X;
                    pt.Y += ptAdorner.Y; 
                    pt = designer.Control.PointToClient(pt);
                    return pt; 
                } 

                Debug.Fail("No behavior service."); 
                return ptAdorner;
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragDrop"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner. 
            /// </devdoc>
            public override void OnDragDrop(Glyph g, DragEventArgs e) { 
                designer.OnDragDrop(e);
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragEnter"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner. 
            /// </devdoc>
            public override void OnDragEnter(Glyph g, DragEventArgs e) { 
                designer.OnDragEnter(e);
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragLeave"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner. 
            /// </devdoc>
            public override void OnDragLeave(Glyph g, EventArgs e) { 
                designer.OnDragLeave(e);
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragOver"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner. 
            /// </devdoc>
            public override void OnDragOver(Glyph g, DragEventArgs e) { 
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

            /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseDown"]/*' /> 
            /// <devdoc>
            ///     When any MouseDown message enters the BehaviorService's AdornerWindow 
            ///     (nclbuttondown, lbuttondown, rbuttondown, nclrbuttondown) it is first 
            ///     passed here, to the top-most Behavior in the BehaviorStack.  Returning
            ///     'true' from this function signifies that the Message was 'handled' by 
            ///     the Behavior and should not continue to be processed.
            /// </devdoc>
            public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) {
                // Need to convert this to a message to pass to the designer 
                int msg = 0;
                if (button == MouseButtons.Left) { 
                    msg = NativeMethods.WM_LBUTTONDOWN; 
                }
                else if (button == MouseButtons.Right) { 
                    msg = NativeMethods.WM_RBUTTONDOWN;
                }

                if (msg != 0) { 
                    Point pt = AdornerToControl(mouseLoc);
                    Message m = new Message(); 
                    m.HWnd = designer.Control.Handle; 
                    m.Msg = msg;
                    m.WParam = IntPtr.Zero; 
                    m.LParam = (IntPtr)(pt.Y << 16 | pt.X);
                    designer.WndProc(ref m);
                    return true;
                } 
                return false;
            } 
 
            /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseUp"]/*' />
            /// <devdoc> 
            ///     When any MouseUp message enters the BehaviorService's AdornerWindow
            ///     (nclbuttonupown, lbuttonup, rbuttonup, nclrbuttonup) it is first
            ///     passed here, to the top-most Behavior in the BehaviorStack.  Returning
            ///     'true' from this function signifies that the Message was 'handled' by 
            ///     the Behavior and should not continue to be processed.
            /// </devdoc> 
            public override bool OnMouseUp(Glyph g, MouseButtons button) { 
                int msg = 0;
                if (button == MouseButtons.Left) { 
                    msg = NativeMethods.WM_LBUTTONUP;
                }
                else if (button == MouseButtons.Right) {
                    msg = NativeMethods.WM_RBUTTONUP; 
                }
 
                if (msg != 0) { 
                    Point pt = designer.Control.PointToClient(Control.MousePosition);
                    Message m = new Message(); 
                    m.HWnd = designer.Control.Handle;
                    m.Msg = msg;
                    m.WParam = IntPtr.Zero;
                    m.LParam = (IntPtr)(pt.Y << 16 | pt.X); 
                    designer.WndProc(ref m);
                    return true; 
                } 
                return false;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="AxHostDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Collections; 
    using System.Design;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using Hashtable = System.Collections.Hashtable;
    using IDictionaryEnumerator = System.Collections.IDictionaryEnumerator; 
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.Windows.Forms; 
    using System.Drawing;
    using System.ComponentModel.Design;
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior; 
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis; 
 
    /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner"]/*' />
    /// <devdoc> 
    ///    <para> Provides design time behavior for the AxHost class. AxHost
    ///       is used to host ActiveX controls.</para>
    /// </devdoc>
    internal class AxHostDesigner : ControlDesigner { 
        private AxHost axHost;
        private EventHandler handler; 
        private bool foundEdit = false; 
        private bool foundAbout = false;
        private bool foundProperties = false; 
        private bool dragdropRevoked = false;
        private Size defaultSize = Size.Empty;

        private const int OLEIVERB_UIACTIVATE = -4; 
        private const int HOSTVERB_ABOUT = 2;
        private const int HOSTVERB_PROPERTIES = 1; 
        private const int HOSTVERB_EDIT = 3; 

        private static readonly HostVerbData EditVerbData = new HostVerbData(SR.GetString(SR.AXEdit), HOSTVERB_EDIT); 
        private static readonly HostVerbData PropertiesVerbData = new HostVerbData(SR.GetString(SR.AXProperties), HOSTVERB_PROPERTIES);
        private static readonly HostVerbData AboutVerbData = new HostVerbData(SR.GetString(SR.AXAbout), HOSTVERB_ABOUT);

        private static TraceSwitch AxHostDesignerSwitch     = new TraceSwitch("AxHostDesigner", "ActiveX Designer Trace"); 

        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.AxHostDesigner"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Windows.Forms.Design.AxHostDesigner'/> class. 
        ///    </para>
        /// </devdoc>
        public AxHostDesigner() {
            handler = new EventHandler(this.OnVerb); 
            AutoResizeHandles = true;
        } 
 
        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.SelectionStyle"]/*' />
        /// <devdoc> 
        ///     This property allows the AxHost class to modify our selection style.  It provides three levels
        ///     of selection:  0 (not selected), 1 (selected) and 2 (selected UI active).
        /// </devdoc>
        private int SelectionStyle { 
            get {
                // we don't implement GET 
                return 0; 
            }
            set { 
                Debug.Fail("How did we get here?");
            }
        }
 
        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.GetOleVerbs"]/*' />
        public override DesignerVerbCollection Verbs { 
            get { 
                DesignerVerbCollection l = new DesignerVerbCollection();
                GetOleVerbs(l); 
                /*
                if (!foundEdit && (((AxHost)axHost).OCXFlags & AxHost.AxFlags.PREVENT_EDIT_MODE) == 0) {
                    l.Add(new HostVerb(EditVerbData, handler));
                } 
                if ((((AxHost)axHost).OCXFlags & AxHost.AxFlags.INCLUDE_PROPERTIES_VERB) != 0 && ((AxHost)axHost).HasPropertyPages()) {
                    l.Add(new HostVerb(PropertiesVerbData, handler)); 
                } 
                */
                if (!foundAbout && ((AxHost)axHost).HasAboutBox) { 
                    l.Add(new HostVerb(AboutVerbData, handler));
                }
                return l;
            } 
        }
 
        /// <devdoc> 
        ///     Retrieves the default dimensions for the given component class.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        private static Size GetDefaultSize(IComponent component) {

            Size size = Size.Empty; 
            DefaultValueAttribute sizeAttr = null;
 
            //Check to see if the control is AutoSized. VSWhidbey #416721 
            PropertyDescriptor prop = TypeDescriptor.GetProperties(component)["AutoSize"];
 
            if (prop != null &&
                !(prop.Attributes.Contains(DesignerSerializationVisibilityAttribute.Hidden) ||
                  prop.Attributes.Contains(BrowsableAttribute.No))) {
                bool autoSize = (bool)prop.GetValue(component); 
                if (autoSize) {
                    prop = TypeDescriptor.GetProperties(component)["PreferredSize"]; 
                    if (prop != null) { 
                        size = (Size)prop.GetValue(component);
                        if (size != Size.Empty) { 
                            return size;
                        }
                    }
                } 
            }
 
            // attempt to get the size property of our component 
            //
            prop = TypeDescriptor.GetProperties(component)["Size"]; 

            if (prop != null) {

                // first, let's see if we can get a valid size... 
                size = (Size)prop.GetValue(component);
 
                // ...if not, we'll see if there's a default size attribute... 
                if (size.Width <= 0 || size.Height <= 0) {
                    sizeAttr = (DefaultValueAttribute)prop.Attributes[typeof(DefaultValueAttribute)]; 
                    if (sizeAttr != null) {
                        return((Size)sizeAttr.Value);
                    }
                } 
                else {
                    return size; 
                } 
            }
 
            // Couldn't get the size or a def size attrib, returning 75,23...
            //
            return(new Size(75, 23));
        } 

        public virtual void GetOleVerbs(DesignerVerbCollection rval) { 
            NativeMethods.IEnumOLEVERB verbEnum = null; 
            NativeMethods.IOleObject obj = axHost.GetOcx() as NativeMethods.IOleObject;
            if (obj == null || NativeMethods.Failed(obj.EnumVerbs(out verbEnum))) { 
                return;
            }

            Debug.Assert(verbEnum != null, "must have return value"); 
            if (verbEnum == null) return;
            int[] fetched = new int[1]; 
            NativeMethods.tagOLEVERB oleVerb = new NativeMethods.tagOLEVERB(); 

            foundEdit = false; 
            foundAbout = false;
            foundProperties = false;

            while (true) { 
                fetched[0] = 0;
                oleVerb.lpszVerbName = null; 
                int hr = verbEnum.Next(1, oleVerb, fetched); 
                if (hr == NativeMethods.S_FALSE) {
                    break; 
                }
                else if (NativeMethods.Failed(hr)) {
                    Debug.Fail("Failed to enumerate through enums: " + hr.ToString(CultureInfo.InvariantCulture));
                    break; 
                }
 
                // Believe it or not, some controls, notably the shdocview control, dont' return 
                // S_FALSE and neither do they set fetched to 1.  So, we need to comment out what's
                // below to maintain compatibility with Visual Basic. 
                //                 if (fetched[0] != 1) {
                //                     Debug.fail("gotta have our 1 verb...");
                //                     break;
                //                 } 
                if ((oleVerb.grfAttribs & NativeMethods.ActiveX.OLEVERBATTRIB_ONCONTAINERMENU) != 0) {
                    foundEdit = foundEdit || oleVerb.lVerb == OLEIVERB_UIACTIVATE; 
                    foundAbout = foundAbout || oleVerb.lVerb == HOSTVERB_ABOUT; 
                    foundProperties = foundProperties || oleVerb.lVerb == HOSTVERB_PROPERTIES;
 
                    rval.Add(new HostVerb(new OleVerbData(oleVerb), handler));
                }
            }
        } 

        protected override bool GetHitTest(Point p) { 
            return axHost.EditMode; 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetBodyGlyph"]/*' />
        /// <devdoc>
        ///     Returns a 'BodyGlyph' representing the bounds of this control.
        ///     The BodyGlyph is responsible for hit testing the related CtrlDes 
        ///     and forwarding messages directly to the designer.
        /// </devdoc> 
        protected override ControlBodyGlyph GetControlGlyph(GlyphSelectionType selType) { 

            Cursor cursor = Cursors.Default; 

            //only selected moveable controls get the sizeall cursor
            if (selType != GlyphSelectionType.NotSelected &&
               (SelectionRules & SelectionRules.Moveable) != 0) { 
                cursor = Cursors.SizeAll;
            } 
 
            //get the correctly translated bounds
            Point loc = BehaviorService.ControlToAdornerWindow((Control)Component); 
            Rectangle translatedBounds = new Rectangle(loc, ((Control)Component).Size);

            //create our glyph, and set its cursor appropriately
            ControlBodyGlyph g = new ControlBodyGlyph(translatedBounds, cursor, Control, this); 
            return g;
        } 
 
        public override void Initialize(IComponent component) {
            base.Initialize(component); 
            axHost = (AxHost)component;
        }

 

        private void OnControlAdded(object sender, System.Windows.Forms.ControlEventArgs e) 
 		{ 
            if (e.Control == axHost)
            { 
                // Get the Size again as it would have changed when the control gets added to the parent (ActiveX controls)
                defaultSize = GetDefaultSize(axHost);
            }
		} 

 
        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.OnCreateHandle"]/*' /> 
        /// <devdoc>
        ///      This is called immediately after the control handle has been created. 
        /// </devdoc>
        protected override void OnCreateHandle() {
            base.OnCreateHandle();
 
            //Application.OLERequired();
            //int n = NativeMethods.RevokeDragDrop(Control.Handle); 
        } 

        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.InitializeNewComponent"]/*' /> 
        /// <devdoc>
        ///     Called when the designer is intialized.  This allows the designer to provide some
        ///     meaningful default values in the component.  The default implementation of this
        ///     sets the components's default property to it's name, if that property is a string. 
        /// </devdoc>
        public override void InitializeNewComponent(IDictionary defaultValues) { 
            try { 
                Control parent = defaultValues["Parent"] as Control;
                if (parent != null) 
                {
                    parent.ControlAdded += new System.Windows.Forms.ControlEventHandler(OnControlAdded);
                }
 
                base.InitializeNewComponent(defaultValues);
 
                if (parent != null) 
                {
                    parent.ControlAdded -= new System.Windows.Forms.ControlEventHandler(OnControlAdded); 
                }

                // We have to put the newSize here after the control is being added. This is because of a change in
                // ParentControlDesigner. We need to recalculate the Size and if it has changed then put in the new Size 
                bool hasSize = (defaultValues != null && defaultValues.Contains("Size"));
                // VsWhidbey : 470669 
                // ActiveX controls change there bounds when the control gets created through the above call which adds the controls to the 
                // parent. Hence re-check the bounds and use the new one.
                // If we are using default.. 
                if (!hasSize && defaultSize != Size.Empty)
                {

                    PropertyDescriptorCollection props = TypeDescriptor.GetProperties(axHost); 
                    if (props != null) {
                         PropertyDescriptor prop = props["Size"]; 
                         if (prop != null) { 
                             prop.SetValue(axHost, new Size(defaultSize.Width, defaultSize.Height));
                         } 
                    }
                }

            } 
            catch {
                // The ControlDesigner tries to set the Text property of the control when 
                // it creates the site. ActiveX controls generally don't like that, causing an 
                // exception to be thrown. We now catch these exceptions in the AxHostDesigner
                // and continue on. 
            }
        }

        public virtual void OnVerb(object sender, EventArgs evevent) { 
            if (sender != null && sender is HostVerb) {
                HostVerb vd = (HostVerb)sender; 
                vd.Invoke((AxHost)axHost); 
            }
            else { 
                Debug.Fail("Bad verb invocation.");
            }
        }
 
        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.PreFilterProperties"]/*' />
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
            object enabledProp = properties["Enabled"]; 

            base.PreFilterProperties(properties); 

            if (enabledProp != null) {
                properties["Enabled"] = enabledProp;
            } 

            // Add a property to handle selection from ActiveX 
            // 
            properties["SelectionStyle"] = TypeDescriptor.CreateProperty(typeof(AxHostDesigner), "SelectionStyle",
                typeof(int), 
                BrowsableAttribute.No,
                DesignerSerializationVisibilityAttribute.Hidden,
                DesignOnlyAttribute.Yes);
        } 

        /// <include file='doc\AxHostDesigner.uex' path='docs/doc[@for="AxHostDesigner.WndProc"]/*' /> 
        /// <devdoc> 
        ///     This method should be called by the extending designer for each message
        ///     the control would normally receive.  This allows the designer to pre-process 
        ///     messages before allowing them to be routed to the control.
        /// </devdoc>
        protected override void WndProc(ref Message m) {
            switch (m.Msg) { 
                case NativeMethods.WM_PARENTNOTIFY:
                    if ((int)m.WParam == NativeMethods.WM_CREATE) { 
                        HookChildHandles(m.LParam); 
                    }
 
                    base.WndProc(ref m);
                    break;

                case NativeMethods.WM_NCHITTEST: 
                    // ASURT 66102 The ShDocVw control registers itself as a drop-target even in design mode.
                    // We take the first chance to unregister that, so we can perform our design-time behavior 
                    // irrespective of what the control wants to do. 
                    //
                    if (!dragdropRevoked) { 
                        int n = NativeMethods.RevokeDragDrop(Control.Handle);
                        dragdropRevoked = (n == NativeMethods.S_OK);
                    }
 
                    // Some ActiveX controls return non-HTCLIENT return values for NC_HITTEST, which
                    // causes the message to go to our parent.  We want the control's designer to get 
                    // these messages so we change the result to HTCLIENT. 
                    //
                    base.WndProc(ref m); 
                    if (((int)m.Result == NativeMethods.HTTRANSPARENT) || ((int)m.Result > NativeMethods.HTCLIENT)) {
                        Debug.WriteLineIf(AxHostDesignerSwitch.TraceVerbose, "Converting NCHITTEST result from : " + (int)m.Result + " to HTCLIENT");
                        m.Result = (IntPtr)NativeMethods.HTCLIENT;
                    } 
                    break;
 
                default: 
                    base.WndProc(ref m);
                    break; 
            }
        }

        /** 
          * @security(checkClassLinking=on)
          */ 
        private class HostVerb : DesignerVerb { 
            private HostVerbData data;
 
            public HostVerb(HostVerbData data, EventHandler handler) : base(data.ToString(), handler) {
                this.data = data;
            }
 
            public void Invoke(AxHost host) {
                data.Execute(host); 
            } 
        }
 
        /**
         * @security(checkClassLinking=on)
         */
        private class HostVerbData { 
            internal readonly string name;
            internal readonly int id; 
 
            internal HostVerbData(string name, int id) {
                this.name = name; 
                this.id = id;
            }

            public override string ToString() { 
                return name;
            } 
 
            internal virtual void Execute(AxHost ctl) {
                switch (id) { 
                    case HOSTVERB_PROPERTIES:
                        ctl.ShowPropertyPages();
                        break;
                    case HOSTVERB_EDIT: 
                        ctl.InvokeEditMode();
                        break; 
                    case HOSTVERB_ABOUT: 
                        ctl.ShowAboutBox();
                        break; 
                    default:
                        Debug.Fail("bad verb id in HostVerb");
                        break;
                } 
            }
        } 
 
        /**
         * @security(checkClassLinking=on) 
         */
        private class OleVerbData : HostVerbData {
            private readonly bool dirties;
 
            internal OleVerbData(NativeMethods.tagOLEVERB oleVerb)
            : base(SR.GetString(SR.AXVerbPrefix) + oleVerb.lpszVerbName, oleVerb.lVerb) { 
                this.dirties = (oleVerb.grfAttribs & NativeMethods.ActiveX.OLEVERBATTRIB_NEVERDIRTIES) == 0; 
            }
 
            internal override void Execute(AxHost ctl) {
                if (dirties) ctl.MakeDirty();
                ctl.DoVerb(id);
            } 
        }
 
        /// <devdoc> 
        ///     This TransparentBehavior is associated with the BodyGlyph for
        ///     this ControlDesigner.  When the BehaviorService hittests a glyph 
        ///     w/a TransparentBehavior, all messages will be passed through
        ///     the BehaviorService directly to the ControlDesigner.
        ///     During a Drag operation, when the BehaviorService hittests
        /// </devdoc> 
        internal class AxHostDesignerBehavior : System.Windows.Forms.Design.Behavior.Behavior {
 
            AxHostDesigner designer;//the related ControlDesigner 
            BehaviorService bs;
 
            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.TransparentBehavior"]/*' />
            /// <devdoc>
            ///     Constructor that accepts the related ControlDesigner.
            /// </devdoc> 
            internal AxHostDesignerBehavior(AxHostDesigner designer) {
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

            /// <devdoc>
            ///     Translates an adorner coordinate to a control coordinate.
            /// </devdoc> 
            private Point AdornerToControl(Point ptAdorner) {
                if (bs == null) { 
                    bs = (BehaviorService)designer.GetService(typeof(BehaviorService)); 
                }
 
                if (bs != null) {
                    Point pt = bs.AdornerWindowToScreen();
                    pt.X += ptAdorner.X;
                    pt.Y += ptAdorner.Y; 
                    pt = designer.Control.PointToClient(pt);
                    return pt; 
                } 

                Debug.Fail("No behavior service."); 
                return ptAdorner;
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragDrop"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner. 
            /// </devdoc>
            public override void OnDragDrop(Glyph g, DragEventArgs e) { 
                designer.OnDragDrop(e);
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragEnter"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner. 
            /// </devdoc>
            public override void OnDragEnter(Glyph g, DragEventArgs e) { 
                designer.OnDragEnter(e);
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragLeave"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner. 
            /// </devdoc>
            public override void OnDragLeave(Glyph g, EventArgs e) { 
                designer.OnDragLeave(e);
            }

            /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TransparentBehavior.OnDragOver"]/*' /> 
            /// <devdoc>
            ///     Forwards DragDrop notification from the BehaviorService to 
            ///     the related ControlDesigner. 
            /// </devdoc>
            public override void OnDragOver(Glyph g, DragEventArgs e) { 
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

            /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseDown"]/*' /> 
            /// <devdoc>
            ///     When any MouseDown message enters the BehaviorService's AdornerWindow 
            ///     (nclbuttondown, lbuttondown, rbuttondown, nclrbuttondown) it is first 
            ///     passed here, to the top-most Behavior in the BehaviorStack.  Returning
            ///     'true' from this function signifies that the Message was 'handled' by 
            ///     the Behavior and should not continue to be processed.
            /// </devdoc>
            public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) {
                // Need to convert this to a message to pass to the designer 
                int msg = 0;
                if (button == MouseButtons.Left) { 
                    msg = NativeMethods.WM_LBUTTONDOWN; 
                }
                else if (button == MouseButtons.Right) { 
                    msg = NativeMethods.WM_RBUTTONDOWN;
                }

                if (msg != 0) { 
                    Point pt = AdornerToControl(mouseLoc);
                    Message m = new Message(); 
                    m.HWnd = designer.Control.Handle; 
                    m.Msg = msg;
                    m.WParam = IntPtr.Zero; 
                    m.LParam = (IntPtr)(pt.Y << 16 | pt.X);
                    designer.WndProc(ref m);
                    return true;
                } 
                return false;
            } 
 
            /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseUp"]/*' />
            /// <devdoc> 
            ///     When any MouseUp message enters the BehaviorService's AdornerWindow
            ///     (nclbuttonupown, lbuttonup, rbuttonup, nclrbuttonup) it is first
            ///     passed here, to the top-most Behavior in the BehaviorStack.  Returning
            ///     'true' from this function signifies that the Message was 'handled' by 
            ///     the Behavior and should not continue to be processed.
            /// </devdoc> 
            public override bool OnMouseUp(Glyph g, MouseButtons button) { 
                int msg = 0;
                if (button == MouseButtons.Left) { 
                    msg = NativeMethods.WM_LBUTTONUP;
                }
                else if (button == MouseButtons.Right) {
                    msg = NativeMethods.WM_RBUTTONUP; 
                }
 
                if (msg != 0) { 
                    Point pt = designer.Control.PointToClient(Control.MousePosition);
                    Message m = new Message(); 
                    m.HWnd = designer.Control.Handle;
                    m.Msg = msg;
                    m.WParam = IntPtr.Zero;
                    m.LParam = (IntPtr)(pt.Y << 16 | pt.X); 
                    designer.WndProc(ref m);
                    return true; 
                } 
                return false;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
