//------------------------------------------------------------------------------ 
// <copyright file="SplitterPanelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.SplitterPanelDesigner..ctor()")]
 
namespace System.Windows.Forms.Design {
    using System.Windows.Forms;
    using System.Design;
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Collections; 
    using System.Diagnostics; 
    using System;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Design;
    using System.Drawing.Drawing2D;
    using Microsoft.Win32; 

 
    /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner"]/*' /> 
    /// <devdoc>
    ///      This class handles all design time behavior for the panel class.  This 
    ///      draws a visible border on the panel if it doesn't have a border so the
    ///      user knows where the boundaries of the panel lie.
    /// </devdoc>
    internal class SplitterPanelDesigner : PanelDesigner { 

        private IDesignerHost designerHost; 
        private SplitContainerDesigner splitContainerDesigner; 
        SplitterPanel splitterPanel;
 
        private bool selected;

        public override bool CanBeParentedTo(IDesigner parentDesigner) {
           return (parentDesigner is SplitContainerDesigner); 
        }
 
 
        protected override InheritanceAttribute InheritanceAttribute {
            get { 
                if (splitterPanel != null && splitterPanel.Parent != null)
                {
                    return (InheritanceAttribute)TypeDescriptor.GetAttributes(splitterPanel.Parent)[typeof(InheritanceAttribute)];
                } 
                return base.InheritanceAttribute;
            } 
        } 

        internal bool Selected { 
            get {
                return selected;
            }
            set { 
                selected = value;
                if (selected) { 
                    DrawSelectedBorder(); 
                }
                else { 
                    EraseBorder();
                }

            } 
        }
 
        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.OnDragEnter"]/*' /> 
        /// <devdoc>
        ///      Called in response to a drag enter for OLE drag and drop. 
        /// </devdoc>
        protected override void OnDragEnter(DragEventArgs de) {
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                de.Effect = DragDropEffects.None; 
                return;
            } 
            base.OnDragEnter(de); 
        }
        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.OnDragOver"]/*' /> 
        /// <devdoc>
        ///     Called when a drag drop object is dragged over the control designer view
        /// </devdoc>
        protected override void OnDragOver(DragEventArgs de) { 
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                de.Effect = DragDropEffects.None; 
                return; 
            }
            base.OnDragOver(de); 
        }


        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.OnDragLeave"]/*' /> 
        /// <devdoc>
        ///     Called when a drag-drop operation leaves the control designer view 
        /// 
        /// </devdoc>
        protected override void OnDragLeave(EventArgs e) { 
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                return;
            }
            base.OnDragLeave(e); 
        }
 
        protected override void OnDragDrop(DragEventArgs de) { 
           if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                de.Effect = DragDropEffects.None; 
                return;
           }
           base.OnDragDrop(de);
        } 

        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.OnMouseHover"]/*' /> 
        /// <devdoc> 
        ///     Called when the user hovers over the splitterpanel.  Here, we'll internally forward this message
        ///     to the SplitContainerDesigner so we have ContainerGrabHandle functionality for the entire component. 
        /// </devdoc>
        protected override void OnMouseHover() {
            if (splitContainerDesigner != null) {
                splitContainerDesigner.SplitterPanelHover(); 
            }
        } 
 

 
        protected override void Dispose(bool disposing) {

            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (cs != null) { 
                cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged);
            } 
 
            base.Dispose(disposing);
        } 

        public override void Initialize(IComponent component) {
            base.Initialize(component);
            splitterPanel = (SplitterPanel)component; 

            designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost)); 
            splitContainerDesigner = (SplitContainerDesigner)designerHost.GetDesigner(splitterPanel.Parent); 

            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
            if (cs != null) {
                cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
            }
 
            PropertyDescriptor lockedProp = TypeDescriptor.GetProperties(component)["Locked"];
            if (lockedProp != null && splitterPanel.Parent is SplitContainer) { 
                lockedProp.SetValue(component, true); 
            }
        } 

        private void OnComponentChanged(object sender, ComponentChangedEventArgs e) {
            if (splitterPanel.Parent == null) {
                return; 
            }
            if (splitterPanel.Controls.Count == 0) { 
                Graphics g = splitterPanel.CreateGraphics(); 
                DrawWaterMark(g);
                g.Dispose(); 
            }
            else {
                // Erase WaterMark ...
                splitterPanel.Invalidate(); 
            }
        } 
 

        internal void DrawSelectedBorder() { 
            Control ctl = Control;
            Rectangle rc = ctl.ClientRectangle;

            using (Graphics g = ctl.CreateGraphics()) { 
                Color penColor;
 
                // Black or white pen?  Depends on the color of the control. 
                //
                if (ctl.BackColor.GetBrightness() < .5) { 
                    penColor = ControlPaint.Light(ctl.BackColor);
                }
                else {
                    penColor = ControlPaint.Dark(ctl.BackColor); 
                }
 
                using (Pen pen = new Pen(penColor)) { 
                    pen.DashStyle = DashStyle.Dash;
                    rc.Inflate(-4,-4); 
                    g.DrawRectangle(pen, rc);
                }
            }
        } 

        internal void EraseBorder() { 
 
            Control ctl = Control;
            Rectangle rc = ctl.ClientRectangle; 

            Graphics g = ctl.CreateGraphics();
            Color penColor = ctl.BackColor;
 
            Pen pen = new Pen(penColor);
            pen.DashStyle = DashStyle.Dash; 
            rc.Inflate(-4,-4); 
            g.DrawRectangle(pen, rc);
 
            pen.Dispose();
            g.Dispose();

            ctl.Invalidate(); 
        }
 
        internal void DrawWaterMark(Graphics g) { 
            Control ctl = Control;
            Rectangle rc = ctl.ClientRectangle; 
            String name = ctl.Name;
            using (Font drawFont = new Font("Arial", 8)) {
                int watermarkX = rc.Width / 2 - (int)g.MeasureString(name,drawFont).Width / 2;
                int watermarkY = rc.Height / 2 ; 
                TextRenderer.DrawText(g, name, drawFont, new Point(watermarkX, watermarkY), Color.Black, TextFormatFlags.Default);
            } 
        } 

        protected override void OnPaintAdornments(PaintEventArgs pe) { 
            base.OnPaintAdornments(pe);

            if (splitterPanel.BorderStyle == BorderStyle.None) {
                DrawBorder(pe.Graphics); 
            }
 
            if (Selected) { 
                DrawSelectedBorder();
            } 
            if (splitterPanel.Controls.Count == 0) {
                DrawWaterMark(pe.Graphics);
            }
        } 

        /// <summary> 
        /// Remove some basic properties that are not supported by the 
        ///  SplitterPanel
        /// </summary> 
        ///
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
            properties.Remove("Modifiers"); 
            properties.Remove("Locked");
            properties.Remove("GenerateMember"); 
            //remove the "(Name)" property  from the property grid. 
            foreach(DictionaryEntry de in properties) {
                PropertyDescriptor p = (PropertyDescriptor)de.Value; 
                if (p.Name.Equals("Name") && p.DesignTimeOnly) {
                    properties[de.Key] =TypeDescriptor.CreateProperty(p.ComponentType, p, BrowsableAttribute.No, DesignerSerializationVisibilityAttribute.Hidden);
                    break;
                } 
            }
        } 
 
        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.SnapLines"]/*' />
        /// <devdoc> 
        ///     Returns a list of SnapLine objects representing interesting
        ///     alignment points for this control.  These SnapLines are used
        ///     to assist in the positioning of the control on a parent's
        ///     surface. 
        /// </devdoc>
        public override IList SnapLines { 
            get { 
                ArrayList snapLines = null;
 
                // We only want PaddingSnaplines for splitterpanels.
                AddPaddingSnapLines(ref snapLines);

                return snapLines; 
            }
        } 
 

        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.SelectionRules"]/*' /> 
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer
        ///     provides rules for a component, the component will not get any UI services. 
        /// </devdoc>
        public override SelectionRules SelectionRules { 
            get { 
                SelectionRules rules = SelectionRules.None;
                Control ctl = Control; 
                if (ctl.Parent is SplitContainer) {
                    rules = SelectionRules.Locked;
                }
                return rules; 
            }
        } 
 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SplitterPanelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.SplitterPanelDesigner..ctor()")]
 
namespace System.Windows.Forms.Design {
    using System.Windows.Forms;
    using System.Design;
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Collections; 
    using System.Diagnostics; 
    using System;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Design;
    using System.Drawing.Drawing2D;
    using Microsoft.Win32; 

 
    /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner"]/*' /> 
    /// <devdoc>
    ///      This class handles all design time behavior for the panel class.  This 
    ///      draws a visible border on the panel if it doesn't have a border so the
    ///      user knows where the boundaries of the panel lie.
    /// </devdoc>
    internal class SplitterPanelDesigner : PanelDesigner { 

        private IDesignerHost designerHost; 
        private SplitContainerDesigner splitContainerDesigner; 
        SplitterPanel splitterPanel;
 
        private bool selected;

        public override bool CanBeParentedTo(IDesigner parentDesigner) {
           return (parentDesigner is SplitContainerDesigner); 
        }
 
 
        protected override InheritanceAttribute InheritanceAttribute {
            get { 
                if (splitterPanel != null && splitterPanel.Parent != null)
                {
                    return (InheritanceAttribute)TypeDescriptor.GetAttributes(splitterPanel.Parent)[typeof(InheritanceAttribute)];
                } 
                return base.InheritanceAttribute;
            } 
        } 

        internal bool Selected { 
            get {
                return selected;
            }
            set { 
                selected = value;
                if (selected) { 
                    DrawSelectedBorder(); 
                }
                else { 
                    EraseBorder();
                }

            } 
        }
 
        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.OnDragEnter"]/*' /> 
        /// <devdoc>
        ///      Called in response to a drag enter for OLE drag and drop. 
        /// </devdoc>
        protected override void OnDragEnter(DragEventArgs de) {
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                de.Effect = DragDropEffects.None; 
                return;
            } 
            base.OnDragEnter(de); 
        }
        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.OnDragOver"]/*' /> 
        /// <devdoc>
        ///     Called when a drag drop object is dragged over the control designer view
        /// </devdoc>
        protected override void OnDragOver(DragEventArgs de) { 
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                de.Effect = DragDropEffects.None; 
                return; 
            }
            base.OnDragOver(de); 
        }


        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.OnDragLeave"]/*' /> 
        /// <devdoc>
        ///     Called when a drag-drop operation leaves the control designer view 
        /// 
        /// </devdoc>
        protected override void OnDragLeave(EventArgs e) { 
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                return;
            }
            base.OnDragLeave(e); 
        }
 
        protected override void OnDragDrop(DragEventArgs de) { 
           if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                de.Effect = DragDropEffects.None; 
                return;
           }
           base.OnDragDrop(de);
        } 

        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.OnMouseHover"]/*' /> 
        /// <devdoc> 
        ///     Called when the user hovers over the splitterpanel.  Here, we'll internally forward this message
        ///     to the SplitContainerDesigner so we have ContainerGrabHandle functionality for the entire component. 
        /// </devdoc>
        protected override void OnMouseHover() {
            if (splitContainerDesigner != null) {
                splitContainerDesigner.SplitterPanelHover(); 
            }
        } 
 

 
        protected override void Dispose(bool disposing) {

            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (cs != null) { 
                cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged);
            } 
 
            base.Dispose(disposing);
        } 

        public override void Initialize(IComponent component) {
            base.Initialize(component);
            splitterPanel = (SplitterPanel)component; 

            designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost)); 
            splitContainerDesigner = (SplitContainerDesigner)designerHost.GetDesigner(splitterPanel.Parent); 

            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
            if (cs != null) {
                cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
            }
 
            PropertyDescriptor lockedProp = TypeDescriptor.GetProperties(component)["Locked"];
            if (lockedProp != null && splitterPanel.Parent is SplitContainer) { 
                lockedProp.SetValue(component, true); 
            }
        } 

        private void OnComponentChanged(object sender, ComponentChangedEventArgs e) {
            if (splitterPanel.Parent == null) {
                return; 
            }
            if (splitterPanel.Controls.Count == 0) { 
                Graphics g = splitterPanel.CreateGraphics(); 
                DrawWaterMark(g);
                g.Dispose(); 
            }
            else {
                // Erase WaterMark ...
                splitterPanel.Invalidate(); 
            }
        } 
 

        internal void DrawSelectedBorder() { 
            Control ctl = Control;
            Rectangle rc = ctl.ClientRectangle;

            using (Graphics g = ctl.CreateGraphics()) { 
                Color penColor;
 
                // Black or white pen?  Depends on the color of the control. 
                //
                if (ctl.BackColor.GetBrightness() < .5) { 
                    penColor = ControlPaint.Light(ctl.BackColor);
                }
                else {
                    penColor = ControlPaint.Dark(ctl.BackColor); 
                }
 
                using (Pen pen = new Pen(penColor)) { 
                    pen.DashStyle = DashStyle.Dash;
                    rc.Inflate(-4,-4); 
                    g.DrawRectangle(pen, rc);
                }
            }
        } 

        internal void EraseBorder() { 
 
            Control ctl = Control;
            Rectangle rc = ctl.ClientRectangle; 

            Graphics g = ctl.CreateGraphics();
            Color penColor = ctl.BackColor;
 
            Pen pen = new Pen(penColor);
            pen.DashStyle = DashStyle.Dash; 
            rc.Inflate(-4,-4); 
            g.DrawRectangle(pen, rc);
 
            pen.Dispose();
            g.Dispose();

            ctl.Invalidate(); 
        }
 
        internal void DrawWaterMark(Graphics g) { 
            Control ctl = Control;
            Rectangle rc = ctl.ClientRectangle; 
            String name = ctl.Name;
            using (Font drawFont = new Font("Arial", 8)) {
                int watermarkX = rc.Width / 2 - (int)g.MeasureString(name,drawFont).Width / 2;
                int watermarkY = rc.Height / 2 ; 
                TextRenderer.DrawText(g, name, drawFont, new Point(watermarkX, watermarkY), Color.Black, TextFormatFlags.Default);
            } 
        } 

        protected override void OnPaintAdornments(PaintEventArgs pe) { 
            base.OnPaintAdornments(pe);

            if (splitterPanel.BorderStyle == BorderStyle.None) {
                DrawBorder(pe.Graphics); 
            }
 
            if (Selected) { 
                DrawSelectedBorder();
            } 
            if (splitterPanel.Controls.Count == 0) {
                DrawWaterMark(pe.Graphics);
            }
        } 

        /// <summary> 
        /// Remove some basic properties that are not supported by the 
        ///  SplitterPanel
        /// </summary> 
        ///
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
            properties.Remove("Modifiers"); 
            properties.Remove("Locked");
            properties.Remove("GenerateMember"); 
            //remove the "(Name)" property  from the property grid. 
            foreach(DictionaryEntry de in properties) {
                PropertyDescriptor p = (PropertyDescriptor)de.Value; 
                if (p.Name.Equals("Name") && p.DesignTimeOnly) {
                    properties[de.Key] =TypeDescriptor.CreateProperty(p.ComponentType, p, BrowsableAttribute.No, DesignerSerializationVisibilityAttribute.Hidden);
                    break;
                } 
            }
        } 
 
        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.SnapLines"]/*' />
        /// <devdoc> 
        ///     Returns a list of SnapLine objects representing interesting
        ///     alignment points for this control.  These SnapLines are used
        ///     to assist in the positioning of the control on a parent's
        ///     surface. 
        /// </devdoc>
        public override IList SnapLines { 
            get { 
                ArrayList snapLines = null;
 
                // We only want PaddingSnaplines for splitterpanels.
                AddPaddingSnapLines(ref snapLines);

                return snapLines; 
            }
        } 
 

        /// <include file='doc\SplitterPanelDesigner.uex' path='docs/doc[@for="SplitterPanelDesigner.SelectionRules"]/*' /> 
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer
        ///     provides rules for a component, the component will not get any UI services. 
        /// </devdoc>
        public override SelectionRules SelectionRules { 
            get { 
                SelectionRules rules = SelectionRules.None;
                Control ctl = Control; 
                if (ctl.Parent is SplitContainer) {
                    rules = SelectionRules.Locked;
                }
                return rules; 
            }
        } 
 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
