//------------------------------------------------------------------------------ 
// <copyright file="PanelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.ComponentModel; 

    using System.Diagnostics;

    using System; 
    using System.Drawing;
    using System.Drawing.Drawing2D; 
    using System.Windows.Forms; 
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior; 


    /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner"]/*' />
    /// <devdoc> 
    ///      This class handles all design time behavior for the panel class.  This
    ///      draws a visible border on the panel if it doesn't have a border so the 
    ///      user knows where the boundaries of the panel lie. 
    /// </devdoc>
    internal class PanelDesigner : ScrollableControlDesigner { 


        public PanelDesigner() {
            AutoResizeHandles = true; 
        }
 
        /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner.DrawBorder"]/*' /> 
        /// <devdoc>
        ///      This draws a nice border around our panel.  We need 
        ///      this because the panel can have no border and you can't
        ///      tell where it is.
        /// </devdoc>
        /// <internalonly/> 
        protected virtual void DrawBorder(Graphics graphics) {
            Panel panel = (Panel)Component; // if the panel is invisible, bail now 
            if(panel == null || !panel.Visible) { 
                return;
            } 

            Pen pen = BorderPen;
            Rectangle rc = Control.ClientRectangle;
 
            rc.Width --;
            rc.Height--; 
 

            graphics.DrawRectangle(pen, rc); 
            pen.Dispose();
        }

 
       /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner.OnPaintAdornments"]/*' />
       /// <devdoc> 
       ///      Overrides our base class.  Here we check to see if there 
       ///      is no border on the panel.  If not, we draw one so that
       ///      the panel shape is visible at design time. 
       /// </devdoc>
       protected override void OnPaintAdornments(PaintEventArgs pe) {
           Panel panel = (Panel)Component;
 
           if (panel.BorderStyle == BorderStyle.None) {
               DrawBorder(pe.Graphics); 
           } 

           base.OnPaintAdornments(pe); 
       }


        /// <devdoc> 
        ///      Creates a Dashed-Pen of appropriate color.
        /// </devdoc> 
        protected Pen BorderPen { 
            get {
                Color penColor = Control.BackColor.GetBrightness() < .5 ? 
                              ControlPaint.Light(Control.BackColor) :
                              ControlPaint.Dark(Control.BackColor);

                Pen pen = new Pen(penColor); 
                pen.DashStyle = DashStyle.Dash;
 
                return pen; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="PanelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.ComponentModel; 

    using System.Diagnostics;

    using System; 
    using System.Drawing;
    using System.Drawing.Drawing2D; 
    using System.Windows.Forms; 
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior; 


    /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner"]/*' />
    /// <devdoc> 
    ///      This class handles all design time behavior for the panel class.  This
    ///      draws a visible border on the panel if it doesn't have a border so the 
    ///      user knows where the boundaries of the panel lie. 
    /// </devdoc>
    internal class PanelDesigner : ScrollableControlDesigner { 


        public PanelDesigner() {
            AutoResizeHandles = true; 
        }
 
        /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner.DrawBorder"]/*' /> 
        /// <devdoc>
        ///      This draws a nice border around our panel.  We need 
        ///      this because the panel can have no border and you can't
        ///      tell where it is.
        /// </devdoc>
        /// <internalonly/> 
        protected virtual void DrawBorder(Graphics graphics) {
            Panel panel = (Panel)Component; // if the panel is invisible, bail now 
            if(panel == null || !panel.Visible) { 
                return;
            } 

            Pen pen = BorderPen;
            Rectangle rc = Control.ClientRectangle;
 
            rc.Width --;
            rc.Height--; 
 

            graphics.DrawRectangle(pen, rc); 
            pen.Dispose();
        }

 
       /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner.OnPaintAdornments"]/*' />
       /// <devdoc> 
       ///      Overrides our base class.  Here we check to see if there 
       ///      is no border on the panel.  If not, we draw one so that
       ///      the panel shape is visible at design time. 
       /// </devdoc>
       protected override void OnPaintAdornments(PaintEventArgs pe) {
           Panel panel = (Panel)Component;
 
           if (panel.BorderStyle == BorderStyle.None) {
               DrawBorder(pe.Graphics); 
           } 

           base.OnPaintAdornments(pe); 
       }


        /// <devdoc> 
        ///      Creates a Dashed-Pen of appropriate color.
        /// </devdoc> 
        protected Pen BorderPen { 
            get {
                Color penColor = Control.BackColor.GetBrightness() < .5 ? 
                              ControlPaint.Light(Control.BackColor) :
                              ControlPaint.Dark(Control.BackColor);

                Pen pen = new Pen(penColor); 
                pen.DashStyle = DashStyle.Dash;
 
                return pen; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
