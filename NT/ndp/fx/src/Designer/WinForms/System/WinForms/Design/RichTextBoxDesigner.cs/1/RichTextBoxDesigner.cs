//------------------------------------------------------------------------------ 
// <copyright file="RichTextBoxDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.RichTextBoxDesigner..ctor()")]
namespace System.Windows.Forms.Design { 
 
    using Microsoft.Win32;
    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Reflection; 
    using System.Windows.Forms;
 
    /// <include file='doc\RichTextBoxDesigner.uex' path='docs/doc[@for="RichTextBoxDesigner"]/*' />
    /// <devdoc>
    ///     The RichTextBoxDesigner provides rich designtime behavior for the
    ///     RichTextBox control. 
    /// </devdoc>
    internal class RichTextBoxDesigner :           TextBoxBaseDesigner { 
 
        private DesignerActionListCollection _actionLists;
 
        /// <devdoc>
        ///     Called when the designer is intialized.  This allows the designer to provide some
        ///     meaningful default values in the control.  The default implementation of this
        ///     sets the control's text to its name. 
        /// </devdoc>
        public override void InitializeNewComponent(IDictionary defaultValues) { 
            base.InitializeNewComponent(defaultValues); 

            // Disable DragDrop at design time. 
            //
            Control control = Control;

            if (control != null && control.Handle != IntPtr.Zero) { 
                NativeMethods.RevokeDragDrop(control.Handle);
                // DragAcceptFiles(control.Handle, false); 
            } 
        }
 
        public override DesignerActionListCollection ActionLists {
            get {
                if (_actionLists == null) {
                    _actionLists = new DesignerActionListCollection(); 
                    _actionLists.Add(new RichTextBoxActionList(this));
                } 
                return _actionLists; 
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
        protected override void PreFilterProperties(IDictionary properties)
        { 
            base.PreFilterProperties(properties);
 
            PropertyDescriptor prop; 

            // Handle shadowed properties 
            //
            string[] shadowProps = new string[] {
                "Text"
            }; 

            Attribute[] empty = new Attribute[0]; 
 
            for (int i = 0; i < shadowProps.Length; i++)
            { 
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null)
                {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(RichTextBoxDesigner), prop, empty); 
                }
            } 
        } 

        /// <devdoc> 
        ///     Accessor for Text. We need to replace "\r\n" with "\n" in the designer before deciding whether
        ///     the old value & new value match.
        /// </devdoc>
        private string Text 
        {
            get 
            { 
                return Control.Text;
            } 
            set
            {
                string oldText = Control.Text;
                if (value != null) 
                {
                    value = value.Replace("\r\n", "\n"); 
                } 
                if (oldText != value) {
                    Control.Text = value; 
                }
            }
        }
    } 

    internal class RichTextBoxActionList : DesignerActionList { 
 
        private RichTextBoxDesigner _designer;
        public RichTextBoxActionList(RichTextBoxDesigner designer) : base(designer.Component) { 
            _designer = designer;
        }

        public void EditLines() { 
            EditorServiceContext.EditValue(_designer, Component, "Lines");
        } 
 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            items.Add(new DesignerActionMethodItem(this, "EditLines", SR.GetString(SR.EditLinesDisplayName), SR.GetString(SR.LinksCategoryName), SR.GetString(SR.EditLinesDescription), true));
            return items;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="RichTextBoxDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.RichTextBoxDesigner..ctor()")]
namespace System.Windows.Forms.Design { 
 
    using Microsoft.Win32;
    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Reflection; 
    using System.Windows.Forms;
 
    /// <include file='doc\RichTextBoxDesigner.uex' path='docs/doc[@for="RichTextBoxDesigner"]/*' />
    /// <devdoc>
    ///     The RichTextBoxDesigner provides rich designtime behavior for the
    ///     RichTextBox control. 
    /// </devdoc>
    internal class RichTextBoxDesigner :           TextBoxBaseDesigner { 
 
        private DesignerActionListCollection _actionLists;
 
        /// <devdoc>
        ///     Called when the designer is intialized.  This allows the designer to provide some
        ///     meaningful default values in the control.  The default implementation of this
        ///     sets the control's text to its name. 
        /// </devdoc>
        public override void InitializeNewComponent(IDictionary defaultValues) { 
            base.InitializeNewComponent(defaultValues); 

            // Disable DragDrop at design time. 
            //
            Control control = Control;

            if (control != null && control.Handle != IntPtr.Zero) { 
                NativeMethods.RevokeDragDrop(control.Handle);
                // DragAcceptFiles(control.Handle, false); 
            } 
        }
 
        public override DesignerActionListCollection ActionLists {
            get {
                if (_actionLists == null) {
                    _actionLists = new DesignerActionListCollection(); 
                    _actionLists.Add(new RichTextBoxActionList(this));
                } 
                return _actionLists; 
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
        protected override void PreFilterProperties(IDictionary properties)
        { 
            base.PreFilterProperties(properties);
 
            PropertyDescriptor prop; 

            // Handle shadowed properties 
            //
            string[] shadowProps = new string[] {
                "Text"
            }; 

            Attribute[] empty = new Attribute[0]; 
 
            for (int i = 0; i < shadowProps.Length; i++)
            { 
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null)
                {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(RichTextBoxDesigner), prop, empty); 
                }
            } 
        } 

        /// <devdoc> 
        ///     Accessor for Text. We need to replace "\r\n" with "\n" in the designer before deciding whether
        ///     the old value & new value match.
        /// </devdoc>
        private string Text 
        {
            get 
            { 
                return Control.Text;
            } 
            set
            {
                string oldText = Control.Text;
                if (value != null) 
                {
                    value = value.Replace("\r\n", "\n"); 
                } 
                if (oldText != value) {
                    Control.Text = value; 
                }
            }
        }
    } 

    internal class RichTextBoxActionList : DesignerActionList { 
 
        private RichTextBoxDesigner _designer;
        public RichTextBoxActionList(RichTextBoxDesigner designer) : base(designer.Component) { 
            _designer = designer;
        }

        public void EditLines() { 
            EditorServiceContext.EditValue(_designer, Component, "Lines");
        } 
 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            items.Add(new DesignerActionMethodItem(this, "EditLines", SR.GetString(SR.EditLinesDisplayName), SR.GetString(SR.LinksCategoryName), SR.GetString(SR.EditLinesDescription), true));
            return items;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
