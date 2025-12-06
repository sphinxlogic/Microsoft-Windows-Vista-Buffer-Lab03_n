//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.Collections;
    using System.Drawing.Design;
    using System.Windows.Forms;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Globalization; 

 
    /// <devdoc>
    ///     Designer class for the MaskedTextBox control.
    /// </devdoc>
    internal class MaskedTextBoxDesigner : TextBoxBaseDesigner 
    {
        private DesignerVerbCollection verbs; 
        private DesignerActionListCollection actions; 

        /// <devdoc> 
        ///     MaskedTextBox designer action list property.  Gets the design-time supported actions on the control.
        /// </devdoc>
        public override DesignerActionListCollection ActionLists
        { 
            get
            { 
                if (this.actions == null) 
                {
                    this.actions = new DesignerActionListCollection(); 
                    this.actions.Add(new MaskedTextBoxDesignerActionList(this) );
                }

                return actions; 
            }
        } 
 
        /// <devdoc>
        ///     A utility method to get a design time masked text box based on the masked text box being designed. 
        /// </devdoc>
        internal static MaskedTextBox GetDesignMaskedTextBox( MaskedTextBox mtb )
        {
            MaskedTextBox designMtb = null; 

            if( mtb == null ) 
            { 
                // return a default control.
                designMtb = new System.Windows.Forms.MaskedTextBox(); 
            }
            else
            {
                MaskedTextProvider mtp = mtb.MaskedTextProvider; 

                if( mtp == null ) 
                { 
                    designMtb = new System.Windows.Forms.MaskedTextBox();
                    designMtb.Text = mtb.Text; 
                }
                else
                {
                    designMtb = new System.Windows.Forms.MaskedTextBox(mtb.MaskedTextProvider); 
                }
 
                // Clone MTB properties. 
                designMtb.ValidatingType = mtb.ValidatingType;
                designMtb.BeepOnError = mtb.BeepOnError; 
                designMtb.InsertKeyMode = mtb.InsertKeyMode;
                designMtb.RejectInputOnFirstFailure = mtb.RejectInputOnFirstFailure;
                designMtb.CutCopyMaskFormat = mtb.CutCopyMaskFormat;
                designMtb.Culture = mtb.Culture; 
                // designMtb.TextMaskFormat = mtb.TextMaskFormat; - Not relevant since it is to be used programatically only.
            } 
 
            // Some constant properties at design time.
            designMtb.UseSystemPasswordChar = false; 
            designMtb.PasswordChar = '\0';
            designMtb.ReadOnly = false;
            designMtb.HidePromptOnLeave = false;
 
            return designMtb;
        } 
 
        internal static string GetMaskInputRejectedErrorMessage( MaskInputRejectedEventArgs e )
        { 
            string rejectionHint;

            switch( e.RejectionHint )
            { 
                case MaskedTextResultHint.AsciiCharacterExpected:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintAsciiCharacterExpected ); 
                    break; 
                case MaskedTextResultHint.AlphanumericCharacterExpected:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintAlphanumericCharacterExpected ); 
                    break;
                case MaskedTextResultHint.DigitExpected:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintDigitExpected );
                    break; 
                case MaskedTextResultHint.LetterExpected:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintLetterExpected ); 
                    break; 
                case MaskedTextResultHint.SignedDigitExpected:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintSignedDigitExpected ); 
                    break;
                case MaskedTextResultHint.PromptCharNotAllowed:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintPromptCharNotAllowed );
                    break; 
                case MaskedTextResultHint.UnavailableEditPosition:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintUnavailableEditPosition ); 
                    break; 
                case MaskedTextResultHint.NonEditPosition:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintNonEditPosition ); 
                    break;
                case MaskedTextResultHint.PositionOutOfRange:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintPositionOutOfRange );
                    break; 
                case MaskedTextResultHint.InvalidInput:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintInvalidInput ); 
                    break; 
                case MaskedTextResultHint.Unknown:
                default: 
                    Debug.Fail( "Unknown RejectionHint, defaulting to InvalidInput..." );
                    goto case MaskedTextResultHint.InvalidInput;

            } 

            return string.Format(CultureInfo.CurrentCulture, SR.GetString(SR.MaskedTextBoxTextEditorErrorFormatString), e.Position, rejectionHint); 
        } 

        /// <devdoc> 
        ///    Obsolete ComponentDesigner method which sets component default properties.  Overriden to avoid setting
        ///    the Mask improperly.
        /// </devdoc>
        [Obsolete("This method has been deprecated. Use InitializeNewComponent instead.  http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override void OnSetComponentDefaults()
        { 
            // do nothing. 
        }
 
        /// <devdoc>
        ///     Event handler for the set mask verb.
        /// </devdoc>
        private void OnVerbSetMask(object sender, EventArgs e) 
        {
            MaskedTextBoxDesignerActionList actionList = new MaskedTextBoxDesignerActionList(this); 
            actionList.SetMask(); 
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
        protected override void PreFilterProperties(IDictionary properties)
        {
            base.PreFilterProperties(properties);
 
            PropertyDescriptor prop;
 
            string[] shadowProps = new string[] 
            {
                "Text", 
                "PasswordChar"
            };

            Attribute[] empty = new Attribute[0]; 

            for (int i = 0; i < shadowProps.Length; i++) 
            { 
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) 
                {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(MaskedTextBoxDesigner), prop, empty);
                }
            } 
        }
 
        /// <devdoc> 
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc>
        public override SelectionRules SelectionRules
        { 
            get
            { 
                SelectionRules rules = base.SelectionRules; 
                rules &= ~(SelectionRules.TopSizeable | SelectionRules.BottomSizeable); // Height is fixed.
                return rules; 
            }
        }

        /// <devdoc> 
        ///     Designe time support PasswordChar code serialization.
        /// </devdoc> 
        //private bool ShouldSerializePasswordChar() 
        //{
        //    return PasswordChar != '\0'; 
        //}

        /// <devdoc>
        ///     Shadows the PasswordChar.  UseSystemPasswordChar overrides PasswordChar so independent on the value 
        ///     of PasswordChar it will return the systemp password char.  However, the value of PasswordChar is
        ///     cached so if UseSystemPasswordChar is reset at design time the PasswordChar value can be restored. 
        ///     So in the case both properties are set, we need to serialize the real PasswordChar value as well. 
        ///
        ///     Note: This code was copied to TextBoxDesigner, if fixing a bug here it is probable that the same 
        ///     bug needs to be fixed there.
        /// </devdoc>
        private char PasswordChar
        { 
            get
            { 
                MaskedTextBox mtb = this.Control as MaskedTextBox; 
                Debug.Assert(mtb != null, "Designed control is not a MaskedTextBox.");
 
                if (mtb.UseSystemPasswordChar)
                {
                    mtb.UseSystemPasswordChar = false;
                    char pwdChar = mtb.PasswordChar; 
                    mtb.UseSystemPasswordChar = true;
 
                    return pwdChar; 
                }
                else 
                {
                    return mtb.PasswordChar;
                }
            } 
            set
            { 
                MaskedTextBox mtb = this.Control as MaskedTextBox; 
                Debug.Assert(mtb != null, "Designed control is not a MaskedTextBox.");
 
                mtb.PasswordChar = value;
            }
        }
 
        /// <devdoc>
        ///      Since we're shadowing Text property, we get called here to determine whether or not to serialize it. 
        /// <devdoc/> 
        //private bool ShouldSerializeText()
        //{ 
        //    return !string.Empty.Equals(this.Control.Text);
        //}

        /// <devdoc> 
        ///     Shadow the Text property to do two things:
        ///        1. Always show the text without prompt or literals. 
        ///        2. The text from the UITypeEditor is assigned escaping literals, prompt and spaces, this is to allow for partial inputs. 
        ///        Observe that if the MTB is hooked to a PropertyBrowser at design time, shadowing of the property won't work unless the
        ///        application is a well written control designer (implements corresponding interfaces). 
        /// </devdoc>
        private string Text
        {
            get 
            {
                // Return text w/o literals or prompt. 
                MaskedTextBox mtb = this.Control as MaskedTextBox; 
                Debug.Assert( mtb != null, "Designed control is not a MaskedTextBox." );
 
                // Text w/o prompt or literals.
                if( string.IsNullOrEmpty(mtb.Mask) )
                {
                    return mtb.Text; 
                }
                return mtb.MaskedTextProvider.ToString( false, false ); 
            } 
            set
            { 
                MaskedTextBox mtb = this.Control as MaskedTextBox;
                Debug.Assert( mtb != null, "Designed control is not a MaskedTextBox." );

                if( string.IsNullOrEmpty(mtb.Mask) ) 
                {
                    mtb.Text = value; 
                } 
                else
                { 
                    bool ResetOnSpace  = mtb.ResetOnSpace;
                    bool ResetOnPrompt = mtb.ResetOnPrompt;
                    bool SkipLiterals  = mtb.SkipLiterals;
 
                    mtb.ResetOnSpace  = true;
                    mtb.ResetOnPrompt = true; 
                    mtb.SkipLiterals  = true; 

                    // Value is expected to contain literals and prompt. 
                    mtb.Text = value;

                    mtb.ResetOnSpace  = ResetOnSpace;
                    mtb.ResetOnPrompt = ResetOnPrompt; 
                    mtb.SkipLiterals  = SkipLiterals;
                } 
            } 
        }
 

        /// <devdoc>
        ///     MaskedTextBox designer verb collection property.  Gets the design-time supported verbs of the control.
        /// </devdoc> 
        public override DesignerVerbCollection Verbs
        { 
            get 
            {
                if( this.verbs == null ) 
                {
                    this.verbs = new DesignerVerbCollection();
                    this.verbs.Add(new DesignerVerb( SR.GetString(SR.MaskedTextBoxDesignerVerbsSetMaskDesc), new EventHandler(OnVerbSetMask) ));
                } 

                return this.verbs; 
            } 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.Collections;
    using System.Drawing.Design;
    using System.Windows.Forms;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Globalization; 

 
    /// <devdoc>
    ///     Designer class for the MaskedTextBox control.
    /// </devdoc>
    internal class MaskedTextBoxDesigner : TextBoxBaseDesigner 
    {
        private DesignerVerbCollection verbs; 
        private DesignerActionListCollection actions; 

        /// <devdoc> 
        ///     MaskedTextBox designer action list property.  Gets the design-time supported actions on the control.
        /// </devdoc>
        public override DesignerActionListCollection ActionLists
        { 
            get
            { 
                if (this.actions == null) 
                {
                    this.actions = new DesignerActionListCollection(); 
                    this.actions.Add(new MaskedTextBoxDesignerActionList(this) );
                }

                return actions; 
            }
        } 
 
        /// <devdoc>
        ///     A utility method to get a design time masked text box based on the masked text box being designed. 
        /// </devdoc>
        internal static MaskedTextBox GetDesignMaskedTextBox( MaskedTextBox mtb )
        {
            MaskedTextBox designMtb = null; 

            if( mtb == null ) 
            { 
                // return a default control.
                designMtb = new System.Windows.Forms.MaskedTextBox(); 
            }
            else
            {
                MaskedTextProvider mtp = mtb.MaskedTextProvider; 

                if( mtp == null ) 
                { 
                    designMtb = new System.Windows.Forms.MaskedTextBox();
                    designMtb.Text = mtb.Text; 
                }
                else
                {
                    designMtb = new System.Windows.Forms.MaskedTextBox(mtb.MaskedTextProvider); 
                }
 
                // Clone MTB properties. 
                designMtb.ValidatingType = mtb.ValidatingType;
                designMtb.BeepOnError = mtb.BeepOnError; 
                designMtb.InsertKeyMode = mtb.InsertKeyMode;
                designMtb.RejectInputOnFirstFailure = mtb.RejectInputOnFirstFailure;
                designMtb.CutCopyMaskFormat = mtb.CutCopyMaskFormat;
                designMtb.Culture = mtb.Culture; 
                // designMtb.TextMaskFormat = mtb.TextMaskFormat; - Not relevant since it is to be used programatically only.
            } 
 
            // Some constant properties at design time.
            designMtb.UseSystemPasswordChar = false; 
            designMtb.PasswordChar = '\0';
            designMtb.ReadOnly = false;
            designMtb.HidePromptOnLeave = false;
 
            return designMtb;
        } 
 
        internal static string GetMaskInputRejectedErrorMessage( MaskInputRejectedEventArgs e )
        { 
            string rejectionHint;

            switch( e.RejectionHint )
            { 
                case MaskedTextResultHint.AsciiCharacterExpected:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintAsciiCharacterExpected ); 
                    break; 
                case MaskedTextResultHint.AlphanumericCharacterExpected:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintAlphanumericCharacterExpected ); 
                    break;
                case MaskedTextResultHint.DigitExpected:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintDigitExpected );
                    break; 
                case MaskedTextResultHint.LetterExpected:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintLetterExpected ); 
                    break; 
                case MaskedTextResultHint.SignedDigitExpected:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintSignedDigitExpected ); 
                    break;
                case MaskedTextResultHint.PromptCharNotAllowed:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintPromptCharNotAllowed );
                    break; 
                case MaskedTextResultHint.UnavailableEditPosition:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintUnavailableEditPosition ); 
                    break; 
                case MaskedTextResultHint.NonEditPosition:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintNonEditPosition ); 
                    break;
                case MaskedTextResultHint.PositionOutOfRange:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintPositionOutOfRange );
                    break; 
                case MaskedTextResultHint.InvalidInput:
                    rejectionHint = SR.GetString( SR.MaskedTextBoxHintInvalidInput ); 
                    break; 
                case MaskedTextResultHint.Unknown:
                default: 
                    Debug.Fail( "Unknown RejectionHint, defaulting to InvalidInput..." );
                    goto case MaskedTextResultHint.InvalidInput;

            } 

            return string.Format(CultureInfo.CurrentCulture, SR.GetString(SR.MaskedTextBoxTextEditorErrorFormatString), e.Position, rejectionHint); 
        } 

        /// <devdoc> 
        ///    Obsolete ComponentDesigner method which sets component default properties.  Overriden to avoid setting
        ///    the Mask improperly.
        /// </devdoc>
        [Obsolete("This method has been deprecated. Use InitializeNewComponent instead.  http://go.microsoft.com/fwlink/?linkid=14202")] 
        public override void OnSetComponentDefaults()
        { 
            // do nothing. 
        }
 
        /// <devdoc>
        ///     Event handler for the set mask verb.
        /// </devdoc>
        private void OnVerbSetMask(object sender, EventArgs e) 
        {
            MaskedTextBoxDesignerActionList actionList = new MaskedTextBoxDesignerActionList(this); 
            actionList.SetMask(); 
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
        protected override void PreFilterProperties(IDictionary properties)
        {
            base.PreFilterProperties(properties);
 
            PropertyDescriptor prop;
 
            string[] shadowProps = new string[] 
            {
                "Text", 
                "PasswordChar"
            };

            Attribute[] empty = new Attribute[0]; 

            for (int i = 0; i < shadowProps.Length; i++) 
            { 
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) 
                {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(MaskedTextBoxDesigner), prop, empty);
                }
            } 
        }
 
        /// <devdoc> 
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer 
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc>
        public override SelectionRules SelectionRules
        { 
            get
            { 
                SelectionRules rules = base.SelectionRules; 
                rules &= ~(SelectionRules.TopSizeable | SelectionRules.BottomSizeable); // Height is fixed.
                return rules; 
            }
        }

        /// <devdoc> 
        ///     Designe time support PasswordChar code serialization.
        /// </devdoc> 
        //private bool ShouldSerializePasswordChar() 
        //{
        //    return PasswordChar != '\0'; 
        //}

        /// <devdoc>
        ///     Shadows the PasswordChar.  UseSystemPasswordChar overrides PasswordChar so independent on the value 
        ///     of PasswordChar it will return the systemp password char.  However, the value of PasswordChar is
        ///     cached so if UseSystemPasswordChar is reset at design time the PasswordChar value can be restored. 
        ///     So in the case both properties are set, we need to serialize the real PasswordChar value as well. 
        ///
        ///     Note: This code was copied to TextBoxDesigner, if fixing a bug here it is probable that the same 
        ///     bug needs to be fixed there.
        /// </devdoc>
        private char PasswordChar
        { 
            get
            { 
                MaskedTextBox mtb = this.Control as MaskedTextBox; 
                Debug.Assert(mtb != null, "Designed control is not a MaskedTextBox.");
 
                if (mtb.UseSystemPasswordChar)
                {
                    mtb.UseSystemPasswordChar = false;
                    char pwdChar = mtb.PasswordChar; 
                    mtb.UseSystemPasswordChar = true;
 
                    return pwdChar; 
                }
                else 
                {
                    return mtb.PasswordChar;
                }
            } 
            set
            { 
                MaskedTextBox mtb = this.Control as MaskedTextBox; 
                Debug.Assert(mtb != null, "Designed control is not a MaskedTextBox.");
 
                mtb.PasswordChar = value;
            }
        }
 
        /// <devdoc>
        ///      Since we're shadowing Text property, we get called here to determine whether or not to serialize it. 
        /// <devdoc/> 
        //private bool ShouldSerializeText()
        //{ 
        //    return !string.Empty.Equals(this.Control.Text);
        //}

        /// <devdoc> 
        ///     Shadow the Text property to do two things:
        ///        1. Always show the text without prompt or literals. 
        ///        2. The text from the UITypeEditor is assigned escaping literals, prompt and spaces, this is to allow for partial inputs. 
        ///        Observe that if the MTB is hooked to a PropertyBrowser at design time, shadowing of the property won't work unless the
        ///        application is a well written control designer (implements corresponding interfaces). 
        /// </devdoc>
        private string Text
        {
            get 
            {
                // Return text w/o literals or prompt. 
                MaskedTextBox mtb = this.Control as MaskedTextBox; 
                Debug.Assert( mtb != null, "Designed control is not a MaskedTextBox." );
 
                // Text w/o prompt or literals.
                if( string.IsNullOrEmpty(mtb.Mask) )
                {
                    return mtb.Text; 
                }
                return mtb.MaskedTextProvider.ToString( false, false ); 
            } 
            set
            { 
                MaskedTextBox mtb = this.Control as MaskedTextBox;
                Debug.Assert( mtb != null, "Designed control is not a MaskedTextBox." );

                if( string.IsNullOrEmpty(mtb.Mask) ) 
                {
                    mtb.Text = value; 
                } 
                else
                { 
                    bool ResetOnSpace  = mtb.ResetOnSpace;
                    bool ResetOnPrompt = mtb.ResetOnPrompt;
                    bool SkipLiterals  = mtb.SkipLiterals;
 
                    mtb.ResetOnSpace  = true;
                    mtb.ResetOnPrompt = true; 
                    mtb.SkipLiterals  = true; 

                    // Value is expected to contain literals and prompt. 
                    mtb.Text = value;

                    mtb.ResetOnSpace  = ResetOnSpace;
                    mtb.ResetOnPrompt = ResetOnPrompt; 
                    mtb.SkipLiterals  = SkipLiterals;
                } 
            } 
        }
 

        /// <devdoc>
        ///     MaskedTextBox designer verb collection property.  Gets the design-time supported verbs of the control.
        /// </devdoc> 
        public override DesignerVerbCollection Verbs
        { 
            get 
            {
                if( this.verbs == null ) 
                {
                    this.verbs = new DesignerVerbCollection();
                    this.verbs.Add(new DesignerVerb( SR.GetString(SR.MaskedTextBoxDesignerVerbsSetMaskDesc), new EventHandler(OnVerbSetMask) ));
                } 

                return this.verbs; 
            } 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
