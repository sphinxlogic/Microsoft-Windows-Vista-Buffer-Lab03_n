//------------------------------------------------------------------------------ 
// <copyright file="SplitterDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.SplitterDesigner..ctor()")]
 
namespace System.Windows.Forms.Design {
    using System.Design;
    using System.ComponentModel;
 
    using System.Diagnostics;
    using System.Drawing.Drawing2D; 
    using System; 
    using System.Drawing;
    using System.Windows.Forms; 
    using Microsoft.Win32;

    /// <include file='doc\SplitterDesigner.uex' path='docs/doc[@for="SplitterDesigner"]/*' />
    /// <devdoc> 
    ///      This class handles all design time behavior for the splitter class.  This
    ///      draws a visible border on the splitter if it doesn't have a border so the 
    ///      user knows where the boundaries of the splitter lie. 
    /// </devdoc>
    internal class SplitterDesigner : ControlDesigner { 

        public SplitterDesigner() {
            AutoResizeHandles = true;
        } 

        /// <include file='doc\SplitterDesigner.uex' path='docs/doc[@for="SplitterDesigner.DrawBorder"]/*' /> 
        /// <devdoc> 
        ///      This draws a nice border around our panel.  We need
        ///      this because the panel can have no border and you can't 
        ///      tell where it is.
        /// </devdoc>
        /// <internalonly/>
        private void DrawBorder(Graphics graphics) { 
            Control ctl = Control;
            Rectangle rc = ctl.ClientRectangle; 
            Color penColor; 

            // Black or white pen?  Depends on the color of the control. 
            //
            if (ctl.BackColor.GetBrightness() < .5) {
                penColor = Color.White;
            } 
            else {
                penColor = Color.Black; 
            } 

            using (Pen pen = new Pen(penColor)) { 
                pen.DashStyle = DashStyle.Dash;

                rc.Width --;
                rc.Height--; 
                graphics.DrawRectangle(pen, rc);
            } 
        } 

        /// <include file='doc\SplitterDesigner.uex' path='docs/doc[@for="SplitterDesigner.OnPaintAdornments"]/*' /> 
        /// <devdoc>
        ///      Overrides our base class.  Here we check to see if there
        ///      is no border on the panel.  If not, we draw one so that
        ///      the panel shape is visible at design time. 
        /// </devdoc>
        protected override void OnPaintAdornments(PaintEventArgs pe) { 
            Splitter splitter = (Splitter)Component; 

            base.OnPaintAdornments(pe); 

            if (splitter.BorderStyle == BorderStyle.None) {
                DrawBorder(pe.Graphics);
            } 
        }
 
        protected override void WndProc(ref Message m) { 
            switch (m.Msg) {
                case NativeMethods.WM_WINDOWPOSCHANGED: 
                    // Really only care about window size changing
                    Control source = (Control)Control;
                    source.Invalidate();
                    break; 
            }
            base.WndProc(ref m); 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SplitterDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.SplitterDesigner..ctor()")]
 
namespace System.Windows.Forms.Design {
    using System.Design;
    using System.ComponentModel;
 
    using System.Diagnostics;
    using System.Drawing.Drawing2D; 
    using System; 
    using System.Drawing;
    using System.Windows.Forms; 
    using Microsoft.Win32;

    /// <include file='doc\SplitterDesigner.uex' path='docs/doc[@for="SplitterDesigner"]/*' />
    /// <devdoc> 
    ///      This class handles all design time behavior for the splitter class.  This
    ///      draws a visible border on the splitter if it doesn't have a border so the 
    ///      user knows where the boundaries of the splitter lie. 
    /// </devdoc>
    internal class SplitterDesigner : ControlDesigner { 

        public SplitterDesigner() {
            AutoResizeHandles = true;
        } 

        /// <include file='doc\SplitterDesigner.uex' path='docs/doc[@for="SplitterDesigner.DrawBorder"]/*' /> 
        /// <devdoc> 
        ///      This draws a nice border around our panel.  We need
        ///      this because the panel can have no border and you can't 
        ///      tell where it is.
        /// </devdoc>
        /// <internalonly/>
        private void DrawBorder(Graphics graphics) { 
            Control ctl = Control;
            Rectangle rc = ctl.ClientRectangle; 
            Color penColor; 

            // Black or white pen?  Depends on the color of the control. 
            //
            if (ctl.BackColor.GetBrightness() < .5) {
                penColor = Color.White;
            } 
            else {
                penColor = Color.Black; 
            } 

            using (Pen pen = new Pen(penColor)) { 
                pen.DashStyle = DashStyle.Dash;

                rc.Width --;
                rc.Height--; 
                graphics.DrawRectangle(pen, rc);
            } 
        } 

        /// <include file='doc\SplitterDesigner.uex' path='docs/doc[@for="SplitterDesigner.OnPaintAdornments"]/*' /> 
        /// <devdoc>
        ///      Overrides our base class.  Here we check to see if there
        ///      is no border on the panel.  If not, we draw one so that
        ///      the panel shape is visible at design time. 
        /// </devdoc>
        protected override void OnPaintAdornments(PaintEventArgs pe) { 
            Splitter splitter = (Splitter)Component; 

            base.OnPaintAdornments(pe); 

            if (splitter.BorderStyle == BorderStyle.None) {
                DrawBorder(pe.Graphics);
            } 
        }
 
        protected override void WndProc(ref Message m) { 
            switch (m.Msg) {
                case NativeMethods.WM_WINDOWPOSCHANGED: 
                    // Really only care about window size changing
                    Control source = (Control)Control;
                    source.Invalidate();
                    break; 
            }
            base.WndProc(ref m); 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
