//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBaseDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Windows.Forms.Design.Behavior; 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
 
    /// <include file='doc\TextBoxBaseDesigner.uex' path='docs/doc[@for="TextBoxBaseDesigner"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides a designer that can design components
    ///       that extend TextBoxBase.</para>
    /// </devdoc> 
    internal class TextBoxBaseDesigner : ControlDesigner {
 
        public TextBoxBaseDesigner() { 
            AutoResizeHandles = true;
        } 

        /// <include file='doc\TextBoxBaseDesigner.uex' path='docs/doc[@for="TextBoxBaseDesigner.SnapLines"]/*' />
        /// <devdoc>
        ///     Adds a baseline SnapLine to the list of SnapLines related 
        ///     to this control.
        /// </devdoc> 
        public override IList SnapLines { 
            get {
                ArrayList snapLines = base.SnapLines as ArrayList; 

                int baseline = DesignerUtils.GetTextBaseline(Control, System.Drawing.ContentAlignment.TopLeft);

                BorderStyle borderStyle = BorderStyle.Fixed3D; 
                PropertyDescriptor prop = TypeDescriptor.GetProperties(Component)["BorderStyle"];
                if (prop != null) { 
                    borderStyle = (BorderStyle)prop.GetValue(Component); 
                }
 
                if (borderStyle == BorderStyle.None) {
                    baseline += 0;
                }
                else if (borderStyle == BorderStyle.FixedSingle) { 
                    baseline += 2;
                } 
                else if (borderStyle == BorderStyle.Fixed3D) { 
                    baseline += 3;
                } 
                else {
                    Debug.Fail("Unknown borderstyle");
                    baseline += 0;
                } 

                snapLines.Add(new SnapLine(SnapLineType.Baseline, baseline, SnapLinePriority.Medium)); 
 
                return snapLines;
            } 
        }

        private string Text {
            get { 
                return Control.Text;
            } 
            set { 
                Control.Text = value;
 
                // This fixes bug #48462. If the text box is not wide enough to display all of the text,
                // then we want to display the first portion at design-time. We can ensure this by
                // setting the selection to (0, 0).
                // 
                ((TextBoxBase)Control).Select(0, 0);
            } 
        } 

        private bool ShouldSerializeText() { 
           return TypeDescriptor.GetProperties(typeof(TextBoxBase))["Text"].ShouldSerializeValue(Component);
        }

 
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        private void ResetText() { 
            Control.Text = ""; 
        }
 
        /// <include file='doc\TextBoxBaseDesigner.uex' path='docs/doc[@for="TextBoxBaseDesigner.InitializeNewComponent"]/*' />
        /// <devdoc>
        ///   We override this so we can clear the text field set by controldesigner.
        /// </devdoc> 
        public override void InitializeNewComponent(IDictionary defaultValues) {
 
            base.InitializeNewComponent(defaultValues); 

            PropertyDescriptor textProp = TypeDescriptor.GetProperties(Component)["Text"]; 
            if (textProp != null && textProp.PropertyType == typeof(string) && !textProp.IsReadOnly && textProp.IsBrowsable) {
                textProp.SetValue(Component, "");
            }
        } 

        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties); 

            PropertyDescriptor prop; 


            // Handle shadowed properties
            // 
            string[] shadowProps = new string[] {
                "Text", 
            }; 

            Attribute[] empty = new Attribute[0]; 

            for (int i = 0; i < shadowProps.Length; i++) {
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) { 
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(TextBoxBaseDesigner), prop, empty);
                } 
            } 
        }
 
        /// <include file='doc\TextBoxBaseDesigner.uex' path='docs/doc[@for="TextBoxBaseDesigner.SelectionRules"]/*' />
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public override SelectionRules SelectionRules { 
            get {
                SelectionRules rules = base.SelectionRules; 
                object component = Component;

                rules |= SelectionRules.AllSizeable;
 
                PropertyDescriptor prop = TypeDescriptor.GetProperties(component)["Multiline"];
                if (prop != null) { 
                    Object value = prop.GetValue(component); 
                    if (value is bool && (bool)value == false) {
                        PropertyDescriptor propAuto = TypeDescriptor.GetProperties(component)["AutoSize"]; 
                        if (propAuto != null) {
                            Object auto = propAuto.GetValue(component);
                            //VSWhidbey #369288
                            if (auto is bool && (bool)auto == true) { 
                                rules &= ~(SelectionRules.TopSizeable | SelectionRules.BottomSizeable);
                            } 
                        } 
                    }
                } 

                return rules;
            }
        } 

    } 
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBaseDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Windows.Forms.Design.Behavior; 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
 
    /// <include file='doc\TextBoxBaseDesigner.uex' path='docs/doc[@for="TextBoxBaseDesigner"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Provides a designer that can design components
    ///       that extend TextBoxBase.</para>
    /// </devdoc> 
    internal class TextBoxBaseDesigner : ControlDesigner {
 
        public TextBoxBaseDesigner() { 
            AutoResizeHandles = true;
        } 

        /// <include file='doc\TextBoxBaseDesigner.uex' path='docs/doc[@for="TextBoxBaseDesigner.SnapLines"]/*' />
        /// <devdoc>
        ///     Adds a baseline SnapLine to the list of SnapLines related 
        ///     to this control.
        /// </devdoc> 
        public override IList SnapLines { 
            get {
                ArrayList snapLines = base.SnapLines as ArrayList; 

                int baseline = DesignerUtils.GetTextBaseline(Control, System.Drawing.ContentAlignment.TopLeft);

                BorderStyle borderStyle = BorderStyle.Fixed3D; 
                PropertyDescriptor prop = TypeDescriptor.GetProperties(Component)["BorderStyle"];
                if (prop != null) { 
                    borderStyle = (BorderStyle)prop.GetValue(Component); 
                }
 
                if (borderStyle == BorderStyle.None) {
                    baseline += 0;
                }
                else if (borderStyle == BorderStyle.FixedSingle) { 
                    baseline += 2;
                } 
                else if (borderStyle == BorderStyle.Fixed3D) { 
                    baseline += 3;
                } 
                else {
                    Debug.Fail("Unknown borderstyle");
                    baseline += 0;
                } 

                snapLines.Add(new SnapLine(SnapLineType.Baseline, baseline, SnapLinePriority.Medium)); 
 
                return snapLines;
            } 
        }

        private string Text {
            get { 
                return Control.Text;
            } 
            set { 
                Control.Text = value;
 
                // This fixes bug #48462. If the text box is not wide enough to display all of the text,
                // then we want to display the first portion at design-time. We can ensure this by
                // setting the selection to (0, 0).
                // 
                ((TextBoxBase)Control).Select(0, 0);
            } 
        } 

        private bool ShouldSerializeText() { 
           return TypeDescriptor.GetProperties(typeof(TextBoxBase))["Text"].ShouldSerializeValue(Component);
        }

 
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        private void ResetText() { 
            Control.Text = ""; 
        }
 
        /// <include file='doc\TextBoxBaseDesigner.uex' path='docs/doc[@for="TextBoxBaseDesigner.InitializeNewComponent"]/*' />
        /// <devdoc>
        ///   We override this so we can clear the text field set by controldesigner.
        /// </devdoc> 
        public override void InitializeNewComponent(IDictionary defaultValues) {
 
            base.InitializeNewComponent(defaultValues); 

            PropertyDescriptor textProp = TypeDescriptor.GetProperties(Component)["Text"]; 
            if (textProp != null && textProp.PropertyType == typeof(string) && !textProp.IsReadOnly && textProp.IsBrowsable) {
                textProp.SetValue(Component, "");
            }
        } 

        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties); 

            PropertyDescriptor prop; 


            // Handle shadowed properties
            // 
            string[] shadowProps = new string[] {
                "Text", 
            }; 

            Attribute[] empty = new Attribute[0]; 

            for (int i = 0; i < shadowProps.Length; i++) {
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) { 
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(TextBoxBaseDesigner), prop, empty);
                } 
            } 
        }
 
        /// <include file='doc\TextBoxBaseDesigner.uex' path='docs/doc[@for="TextBoxBaseDesigner.SelectionRules"]/*' />
        /// <devdoc>
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public override SelectionRules SelectionRules { 
            get {
                SelectionRules rules = base.SelectionRules; 
                object component = Component;

                rules |= SelectionRules.AllSizeable;
 
                PropertyDescriptor prop = TypeDescriptor.GetProperties(component)["Multiline"];
                if (prop != null) { 
                    Object value = prop.GetValue(component); 
                    if (value is bool && (bool)value == false) {
                        PropertyDescriptor propAuto = TypeDescriptor.GetProperties(component)["AutoSize"]; 
                        if (propAuto != null) {
                            Object auto = propAuto.GetValue(component);
                            //VSWhidbey #369288
                            if (auto is bool && (bool)auto == true) { 
                                rules &= ~(SelectionRules.TopSizeable | SelectionRules.BottomSizeable);
                            } 
                        } 
                    }
                } 

                return rules;
            }
        } 

    } 
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
