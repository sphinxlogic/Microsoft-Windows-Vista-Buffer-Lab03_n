[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.PictureBoxDesigner..ctor()")] 
namespace System.Windows.Forms.Design {


    using System.Diagnostics; 

    using System; 
    using System.Design; 
    using System.Windows.Forms;
    using System.Drawing; 
    using Microsoft.Win32;
    using System.Drawing.Drawing2D;

    using System.ComponentModel; 
    using System.Drawing.Design;
    using System.ComponentModel.Design; 
    using System.Collections; 

    /// <include file='doc\PictureBoxDesigner.uex' path='docs/doc[@for="PictureBoxDesigner"]/*' /> 
    /// <devdoc>
    ///     This class handles all design time behavior for the group box class.  Group
    ///     boxes may contain sub-components and therefore use the frame designer.
    /// </devdoc> 
    internal class PictureBoxDesigner : ControlDesigner {
        private DesignerActionListCollection _actionLists; 
 
        public PictureBoxDesigner() {
            AutoResizeHandles = true; 
        }


        /// <include file='doc\PictureBoxDesigner.uex' path='docs/doc[@for="PictureBoxDesigner.DrawBorder"]/*' /> 
        /// <devdoc>
        ///      This draws a nice border around our pictureBox.  We need 
        ///      this because the pictureBox can have no border and you can't 
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
                penColor = ControlPaint.Light(ctl.BackColor);
            }
            else {
                penColor = ControlPaint.Dark(ctl.BackColor);; 
            }
 
            Pen pen = new Pen(penColor); 
            pen.DashStyle = DashStyle.Dash;
 
            rc.Width --;
            rc.Height--;
            graphics.DrawRectangle(pen, rc);
 
            pen.Dispose();
        } 
 
        /// <include file='doc\PictureBoxDesigner.uex' path='docs/doc[@for="PictureBoxDesigner.OnPaintAdornments"]/*' />
        /// <devdoc> 
        ///      Overrides our base class.  Here we check to see if there
        ///      is no border on the pictureBox.  If not, we draw one so that
        ///      the pictureBox shape is visible at design time.
        /// </devdoc> 
        protected override void OnPaintAdornments(PaintEventArgs pe) {
            PictureBox pictureBox = (PictureBox)Component; 
 
            if (pictureBox.BorderStyle == BorderStyle.None) {
                DrawBorder(pe.Graphics); 
            }

            base.OnPaintAdornments(pe);
        } 

        /// <include file='doc\PictureBoxDesigner.uex' path='docs/doc[@for="PictureBoxDesigner.SelectionRules"]/*' /> 
        /// <devdoc> 
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc>
        public override SelectionRules SelectionRules {
            get { 
                SelectionRules rules = base.SelectionRules;
                object component = Component; 
 
                PropertyDescriptor propSizeMode = TypeDescriptor.GetProperties(Component)["SizeMode"];
                if (propSizeMode != null) { 
                    PictureBoxSizeMode sizeMode = (PictureBoxSizeMode)propSizeMode.GetValue(component);

                    if (sizeMode == PictureBoxSizeMode.AutoSize) {
                        rules &= ~SelectionRules.AllSizeable; 
                    }
                } 
 
                return rules;
            } 
        }


        public override DesignerActionListCollection ActionLists { 
            get {
                if (_actionLists == null) { 
                    _actionLists = new DesignerActionListCollection(); 
                    _actionLists.Add(new PictureBoxActionList(this));
                } 
                return _actionLists;
            }
        }
    } 

    internal class PictureBoxActionList : DesignerActionList { 
        private PictureBoxDesigner  _designer; 
        public PictureBoxActionList(PictureBoxDesigner designer) : base(designer.Component) {
            _designer = designer; 
        }

        public PictureBoxSizeMode SizeMode {
            get { 
                return ((PictureBox)Component).SizeMode;
            } 
            set { 
                TypeDescriptor.GetProperties(Component)["SizeMode"].SetValue(Component, value);
            } 
        }

        public void ChooseImage() {
            EditorServiceContext.EditValue(_designer, Component, "Image"); 
        }
 
        public override DesignerActionItemCollection GetSortedActionItems() { 
            DesignerActionItemCollection items = new DesignerActionItemCollection();
            items.Add(new DesignerActionMethodItem(this, "ChooseImage", SR.GetString(SR.ChooseImageDisplayName), SR.GetString(SR.PropertiesCategoryName), SR.GetString(SR.ChooseImageDescription), true)); 
            items.Add(new DesignerActionPropertyItem("SizeMode", SR.GetString(SR.SizeModeDisplayName), SR.GetString(SR.PropertiesCategoryName), SR.GetString(SR.SizeModeDescription)));
            return items;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.PictureBoxDesigner..ctor()")] 
namespace System.Windows.Forms.Design {


    using System.Diagnostics; 

    using System; 
    using System.Design; 
    using System.Windows.Forms;
    using System.Drawing; 
    using Microsoft.Win32;
    using System.Drawing.Drawing2D;

    using System.ComponentModel; 
    using System.Drawing.Design;
    using System.ComponentModel.Design; 
    using System.Collections; 

    /// <include file='doc\PictureBoxDesigner.uex' path='docs/doc[@for="PictureBoxDesigner"]/*' /> 
    /// <devdoc>
    ///     This class handles all design time behavior for the group box class.  Group
    ///     boxes may contain sub-components and therefore use the frame designer.
    /// </devdoc> 
    internal class PictureBoxDesigner : ControlDesigner {
        private DesignerActionListCollection _actionLists; 
 
        public PictureBoxDesigner() {
            AutoResizeHandles = true; 
        }


        /// <include file='doc\PictureBoxDesigner.uex' path='docs/doc[@for="PictureBoxDesigner.DrawBorder"]/*' /> 
        /// <devdoc>
        ///      This draws a nice border around our pictureBox.  We need 
        ///      this because the pictureBox can have no border and you can't 
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
                penColor = ControlPaint.Light(ctl.BackColor);
            }
            else {
                penColor = ControlPaint.Dark(ctl.BackColor);; 
            }
 
            Pen pen = new Pen(penColor); 
            pen.DashStyle = DashStyle.Dash;
 
            rc.Width --;
            rc.Height--;
            graphics.DrawRectangle(pen, rc);
 
            pen.Dispose();
        } 
 
        /// <include file='doc\PictureBoxDesigner.uex' path='docs/doc[@for="PictureBoxDesigner.OnPaintAdornments"]/*' />
        /// <devdoc> 
        ///      Overrides our base class.  Here we check to see if there
        ///      is no border on the pictureBox.  If not, we draw one so that
        ///      the pictureBox shape is visible at design time.
        /// </devdoc> 
        protected override void OnPaintAdornments(PaintEventArgs pe) {
            PictureBox pictureBox = (PictureBox)Component; 
 
            if (pictureBox.BorderStyle == BorderStyle.None) {
                DrawBorder(pe.Graphics); 
            }

            base.OnPaintAdornments(pe);
        } 

        /// <include file='doc\PictureBoxDesigner.uex' path='docs/doc[@for="PictureBoxDesigner.SelectionRules"]/*' /> 
        /// <devdoc> 
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc>
        public override SelectionRules SelectionRules {
            get { 
                SelectionRules rules = base.SelectionRules;
                object component = Component; 
 
                PropertyDescriptor propSizeMode = TypeDescriptor.GetProperties(Component)["SizeMode"];
                if (propSizeMode != null) { 
                    PictureBoxSizeMode sizeMode = (PictureBoxSizeMode)propSizeMode.GetValue(component);

                    if (sizeMode == PictureBoxSizeMode.AutoSize) {
                        rules &= ~SelectionRules.AllSizeable; 
                    }
                } 
 
                return rules;
            } 
        }


        public override DesignerActionListCollection ActionLists { 
            get {
                if (_actionLists == null) { 
                    _actionLists = new DesignerActionListCollection(); 
                    _actionLists.Add(new PictureBoxActionList(this));
                } 
                return _actionLists;
            }
        }
    } 

    internal class PictureBoxActionList : DesignerActionList { 
        private PictureBoxDesigner  _designer; 
        public PictureBoxActionList(PictureBoxDesigner designer) : base(designer.Component) {
            _designer = designer; 
        }

        public PictureBoxSizeMode SizeMode {
            get { 
                return ((PictureBox)Component).SizeMode;
            } 
            set { 
                TypeDescriptor.GetProperties(Component)["SizeMode"].SetValue(Component, value);
            } 
        }

        public void ChooseImage() {
            EditorServiceContext.EditValue(_designer, Component, "Image"); 
        }
 
        public override DesignerActionItemCollection GetSortedActionItems() { 
            DesignerActionItemCollection items = new DesignerActionItemCollection();
            items.Add(new DesignerActionMethodItem(this, "ChooseImage", SR.GetString(SR.ChooseImageDisplayName), SR.GetString(SR.PropertiesCategoryName), SR.GetString(SR.ChooseImageDescription), true)); 
            items.Add(new DesignerActionPropertyItem("SizeMode", SR.GetString(SR.SizeModeDisplayName), SR.GetString(SR.PropertiesCategoryName), SR.GetString(SR.SizeModeDescription)));
            return items;
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
