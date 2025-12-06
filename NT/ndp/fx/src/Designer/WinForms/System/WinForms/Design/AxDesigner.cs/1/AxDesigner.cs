//------------------------------------------------------------------------------ 
// <copyright file="AxDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using Microsoft.Win32;
    using System;
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using System.Windows.Forms.Design.Behavior;
 
    internal class AxDesigner : ControlDesigner {
        private WebBrowserBase webBrowserBase; 
        private bool dragdropRevoked = false; 

        private static TraceSwitch AxDesignerSwitch     = new TraceSwitch("AxDesigner", "ActiveX Designer Trace"); 

        private int SelectionStyle {
            get {
                // we don't implement GET 
                return 0;
            } 
            set { 
                Debug.Fail("How did we get here?");
           } 
        }

        public override void Initialize(IComponent component) {
            base.Initialize(component); 
            AutoResizeHandles = true;
            webBrowserBase = (WebBrowserBase)component; 
        } 

        public override void InitializeNewComponent(IDictionary defaultValues) { 
            try {
                base.InitializeNewComponent(defaultValues);
            }
            catch  { 
                // The ControlDesigner tries to set the Text property of the control when
                // it creates the site. ActiveX controls generally don't like that, causing an 
                // exception to be thrown. We now catch these exceptions in the AxDesigner 
                // and continue on.
            } 
        }

        protected override void PreFilterProperties(IDictionary properties) {
            object enabledProp = properties["Enabled"]; 

            base.PreFilterProperties(properties); 
 
            if (enabledProp != null) {
                properties["Enabled"] = enabledProp; 
            }

            // Add a property to handle selection from ActiveX
            // 
            properties["SelectionStyle"] = TypeDescriptor.CreateProperty(typeof(AxDesigner), "SelectionStyle",
                typeof(int), 
                BrowsableAttribute.No, 
                DesignerSerializationVisibilityAttribute.Hidden,
                DesignOnlyAttribute.Yes); 
        }

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
 
                    // VSWhidbey 482533. The associated control might have child handles created
                    // asynchronously, so we need to do this for all children.
                    if (!dragdropRevoked) {
                        IntPtr child = Control.Handle; 
                        dragdropRevoked = true;
                        while(child != IntPtr.Zero && dragdropRevoked) { 
                            NativeMethods.RevokeDragDrop(child); 
                            child = NativeMethods.GetWindow(child, NativeMethods.GW_CHILD);
                        } 

                    }

                    // Some ActiveX controls return non-HTCLIENT return values for NC_HITTEST, which 
                    // causes the message to go to our parent.  We want the control's designer to get
                    // these messages so we change the result to HTCLIENT. 
                    // 
                    base.WndProc(ref m);
                    if (((int)m.Result == NativeMethods.HTTRANSPARENT) || ((int)m.Result > NativeMethods.HTCLIENT)) { 
                        Debug.WriteLineIf(AxDesignerSwitch.TraceVerbose, "Converting NCHITTEST result from : " + (int)m.Result + " to HTCLIENT");
                        m.Result = (IntPtr)NativeMethods.HTCLIENT;
                    }
                    break; 

                default: 
                    base.WndProc(ref m); 
                    break;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="AxDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using Microsoft.Win32;
    using System;
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using System.Windows.Forms.Design.Behavior;
 
    internal class AxDesigner : ControlDesigner {
        private WebBrowserBase webBrowserBase; 
        private bool dragdropRevoked = false; 

        private static TraceSwitch AxDesignerSwitch     = new TraceSwitch("AxDesigner", "ActiveX Designer Trace"); 

        private int SelectionStyle {
            get {
                // we don't implement GET 
                return 0;
            } 
            set { 
                Debug.Fail("How did we get here?");
           } 
        }

        public override void Initialize(IComponent component) {
            base.Initialize(component); 
            AutoResizeHandles = true;
            webBrowserBase = (WebBrowserBase)component; 
        } 

        public override void InitializeNewComponent(IDictionary defaultValues) { 
            try {
                base.InitializeNewComponent(defaultValues);
            }
            catch  { 
                // The ControlDesigner tries to set the Text property of the control when
                // it creates the site. ActiveX controls generally don't like that, causing an 
                // exception to be thrown. We now catch these exceptions in the AxDesigner 
                // and continue on.
            } 
        }

        protected override void PreFilterProperties(IDictionary properties) {
            object enabledProp = properties["Enabled"]; 

            base.PreFilterProperties(properties); 
 
            if (enabledProp != null) {
                properties["Enabled"] = enabledProp; 
            }

            // Add a property to handle selection from ActiveX
            // 
            properties["SelectionStyle"] = TypeDescriptor.CreateProperty(typeof(AxDesigner), "SelectionStyle",
                typeof(int), 
                BrowsableAttribute.No, 
                DesignerSerializationVisibilityAttribute.Hidden,
                DesignOnlyAttribute.Yes); 
        }

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
 
                    // VSWhidbey 482533. The associated control might have child handles created
                    // asynchronously, so we need to do this for all children.
                    if (!dragdropRevoked) {
                        IntPtr child = Control.Handle; 
                        dragdropRevoked = true;
                        while(child != IntPtr.Zero && dragdropRevoked) { 
                            NativeMethods.RevokeDragDrop(child); 
                            child = NativeMethods.GetWindow(child, NativeMethods.GW_CHILD);
                        } 

                    }

                    // Some ActiveX controls return non-HTCLIENT return values for NC_HITTEST, which 
                    // causes the message to go to our parent.  We want the control's designer to get
                    // these messages so we change the result to HTCLIENT. 
                    // 
                    base.WndProc(ref m);
                    if (((int)m.Result == NativeMethods.HTTRANSPARENT) || ((int)m.Result > NativeMethods.HTCLIENT)) { 
                        Debug.WriteLineIf(AxDesignerSwitch.TraceVerbose, "Converting NCHITTEST result from : " + (int)m.Result + " to HTCLIENT");
                        m.Result = (IntPtr)NativeMethods.HTCLIENT;
                    }
                    break; 

                default: 
                    base.WndProc(ref m); 
                    break;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
