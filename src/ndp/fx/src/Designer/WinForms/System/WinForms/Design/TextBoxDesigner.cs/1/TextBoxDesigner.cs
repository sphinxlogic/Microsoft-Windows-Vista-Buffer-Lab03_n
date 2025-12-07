//------------------------------------------------------------------------------ 
// <copyright file="TextBoxDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.TextBoxDesigner..ctor()")] 
namespace System.Windows.Forms.Design {
    using System;
    using System.Design;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Windows.Forms.Design.Behavior; 
    using System.Diagnostics;
 
    /// <include file='doc\TextBoxDesigner.uex' path='docs/doc[@for="TextBoxDesigner"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides a designer for TextBox.</para> 
    /// </devdoc>
    internal class TextBoxDesigner : TextBoxBaseDesigner { 
 
        private DesignerActionListCollection _actionLists;
        public override DesignerActionListCollection ActionLists { 
            get {
                if (_actionLists == null) {
                    _actionLists = new DesignerActionListCollection();
                    _actionLists.Add(new TextBoxActionList(this)); 
                }
                return _actionLists; 
            } 
        }
 
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

            string[] shadowProps = new string[] { 
                "PasswordChar" 
            };
 
            Attribute[] empty = new Attribute[0];

            for (int i = 0; i < shadowProps.Length; i++) {
                prop = (PropertyDescriptor)properties[shadowProps[i]]; 
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(TextBoxDesigner), prop, empty); 
                } 
            }
        } 

        /// <devdoc>
        ///     Shadows the PasswordChar.  UseSystemPasswordChar overrides PasswordChar so independent on the value
        ///     of PasswordChar it will return the systemp password char.  However, the value of PasswordChar is 
        ///     cached so if UseSystemPasswordChar is reset at design time the PasswordChar value can be restored.
        ///     So in the case both properties are set, we need to serialize the real PasswordChar value as well. 
        /// 
        ///     Note: This code was copied from MaskedTextBoxDesigner, if fixing a bug here it is probable that the
        ///     same bug needs to be fixed there. 
        /// </devdoc>
        private char PasswordChar {
            get {
                TextBox tb = this.Control as TextBox; 
                Debug.Assert(tb != null, "Designed control is not a TextBox.");
 
                if (tb.UseSystemPasswordChar) { 
                    tb.UseSystemPasswordChar = false;
                    char pwdChar = tb.PasswordChar; 
                    tb.UseSystemPasswordChar = true;

                    return pwdChar;
                } 
                else {
                    return tb.PasswordChar; 
                } 
            }
            set { 
                TextBox tb = this.Control as TextBox;
                Debug.Assert(tb != null, "Designed control is not a TextBox.");

                tb.PasswordChar = value; 
            }
        } 
    } 

    internal class TextBoxActionList : DesignerActionList { 
        public TextBoxActionList(TextBoxDesigner designer) : base(designer.Component) {
        }

        public bool Multiline { 
            get {
                return ((TextBox)Component).Multiline; 
            } 
            set {
                TypeDescriptor.GetProperties(Component)["Multiline"].SetValue(Component, value); 
            }
        }

        public override DesignerActionItemCollection GetSortedActionItems() { 
            DesignerActionItemCollection items = new DesignerActionItemCollection();
            items.Add(new DesignerActionPropertyItem("Multiline", SR.GetString(SR.MultiLineDisplayName), SR.GetString(SR.PropertiesCategoryName), SR.GetString(SR.MultiLineDescription))); 
            return items; 
        }
    } 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TextBoxDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.TextBoxDesigner..ctor()")] 
namespace System.Windows.Forms.Design {
    using System;
    using System.Design;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Windows.Forms.Design.Behavior; 
    using System.Diagnostics;
 
    /// <include file='doc\TextBoxDesigner.uex' path='docs/doc[@for="TextBoxDesigner"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides a designer for TextBox.</para> 
    /// </devdoc>
    internal class TextBoxDesigner : TextBoxBaseDesigner { 
 
        private DesignerActionListCollection _actionLists;
        public override DesignerActionListCollection ActionLists { 
            get {
                if (_actionLists == null) {
                    _actionLists = new DesignerActionListCollection();
                    _actionLists.Add(new TextBoxActionList(this)); 
                }
                return _actionLists; 
            } 
        }
 
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

            string[] shadowProps = new string[] { 
                "PasswordChar" 
            };
 
            Attribute[] empty = new Attribute[0];

            for (int i = 0; i < shadowProps.Length; i++) {
                prop = (PropertyDescriptor)properties[shadowProps[i]]; 
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(TextBoxDesigner), prop, empty); 
                } 
            }
        } 

        /// <devdoc>
        ///     Shadows the PasswordChar.  UseSystemPasswordChar overrides PasswordChar so independent on the value
        ///     of PasswordChar it will return the systemp password char.  However, the value of PasswordChar is 
        ///     cached so if UseSystemPasswordChar is reset at design time the PasswordChar value can be restored.
        ///     So in the case both properties are set, we need to serialize the real PasswordChar value as well. 
        /// 
        ///     Note: This code was copied from MaskedTextBoxDesigner, if fixing a bug here it is probable that the
        ///     same bug needs to be fixed there. 
        /// </devdoc>
        private char PasswordChar {
            get {
                TextBox tb = this.Control as TextBox; 
                Debug.Assert(tb != null, "Designed control is not a TextBox.");
 
                if (tb.UseSystemPasswordChar) { 
                    tb.UseSystemPasswordChar = false;
                    char pwdChar = tb.PasswordChar; 
                    tb.UseSystemPasswordChar = true;

                    return pwdChar;
                } 
                else {
                    return tb.PasswordChar; 
                } 
            }
            set { 
                TextBox tb = this.Control as TextBox;
                Debug.Assert(tb != null, "Designed control is not a TextBox.");

                tb.PasswordChar = value; 
            }
        } 
    } 

    internal class TextBoxActionList : DesignerActionList { 
        public TextBoxActionList(TextBoxDesigner designer) : base(designer.Component) {
        }

        public bool Multiline { 
            get {
                return ((TextBox)Component).Multiline; 
            } 
            set {
                TypeDescriptor.GetProperties(Component)["Multiline"].SetValue(Component, value); 
            }
        }

        public override DesignerActionItemCollection GetSortedActionItems() { 
            DesignerActionItemCollection items = new DesignerActionItemCollection();
            items.Add(new DesignerActionPropertyItem("Multiline", SR.GetString(SR.MultiLineDisplayName), SR.GetString(SR.PropertiesCategoryName), SR.GetString(SR.MultiLineDescription))); 
            return items; 
        }
    } 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
