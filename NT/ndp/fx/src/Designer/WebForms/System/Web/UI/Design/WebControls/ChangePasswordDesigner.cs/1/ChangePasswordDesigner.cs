//------------------------------------------------------------------------------ 
// <copyright file="ChangePasswordDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization; 
    using System.IO;
    using System.Web.UI.WebControls; 
    using Cursor = System.Windows.Forms.Cursor;
    using Cursors = System.Windows.Forms.Cursors;

    /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner"]/*' /> 
    /// <devdoc>
    /// The designer for the ChangePassword control.  Adds verbs for "Auto Format", "Convert To Template", and "Reset". 
    /// When the control is templated, removes properties that do not apply when templated. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ChangePasswordDesigner : ControlDesigner {
        private static DesignerAutoFormatCollection _autoFormats;
        private ChangePassword _changePassword;
 
        private static readonly string[] _templateNames = new string[] {
            "ChangePasswordTemplate", 
            "SuccessTemplate", 
        };
 
        private static readonly string[] _changePasswordViewRegionToPropertyMap = new string[] {
            "ChangePasswordTitleText",
            "UserNameLabelText",
            "PasswordLabelText", 
            "InstructionText",
            "PasswordHintText", 
            "NewPasswordLabelText", 
            "ConfirmNewPasswordLabelText",
        }; 

        private static readonly string[] _successViewRegionToPropertyMap = new string[] {
            "SuccessText",
            "SuccessTitleText", 
        };
 
        private const string _failureTextID = "FailureText"; 

        // Properties that do not apply to the control when it is templated 
        // Removed from the property grid when there is a user template
        private static readonly string[] _nonTemplateProperties = new string[] {
            "BorderPadding",
            "CancelButtonImageUrl", 
            "CancelButtonStyle",
            "CancelButtonText", 
            "CancelButtonType", 
            "ChangePasswordButtonImageUrl",
            "ChangePasswordButtonStyle", 
            "ChangePasswordButtonText",
            "ChangePasswordButtonType",
            "ChangePasswordTitleText",
            "ConfirmNewPasswordLabelText", 
            "ConfirmPasswordCompareErrorMessage",
            "ConfirmPasswordRequiredErrorMessage", 
            "ContinueButtonImageUrl", 
            "ContinueButtonStyle",
            "ContinueButtonText", 
            "ContinueButtonType",
            "CreateUserIconUrl",
            "CreateUserText",
            "CreateUserUrl", 
            "DisplayUserName",
            "EditProfileText", 
            "EditProfileIconUrl", 
            "EditProfileUrl",
            "FailureTextStyle", 
            "HelpPageIconUrl",
            "HelpPageText",
            "HelpPageUrl",
            "HyperLinkStyle", 
            "InstructionText",
            "InstructionTextStyle", 
            "LabelStyle", 
            "NewPasswordLabelText",
            "NewPasswordRequiredErrorMessage", 
            "NewPasswordRegularExpression",
            "NewPasswordRegularExpressionErrorMessage",
            "PasswordHintText",
            "PasswordHintStyle", 
            "PasswordLabelText",
            "PasswordRecoveryText", 
            "PasswordRecoveryUrl", 
            "PasswordRecoveryIconUrl",
            "PasswordRequiredErrorMessage", 
            "SuccessTitleText",
            "SuccessText",
            "SuccessTextStyle",
            "TextBoxStyle", 
            "TitleTextStyle",
            "UserNameLabelText", 
            "UserNameRequiredErrorMessage", 
            "ValidatorTextStyle",
        }; 

        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new ChangePasswordDesignerActionList(this)); 

                return actionLists; 
            }
        }

        public override DesignerAutoFormatCollection AutoFormats { 
            get {
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.CHANGEPASSWORD_SCHEMES, 
                        delegate(DataRow schemeData) { return new ChangePasswordAutoFormat(schemeData); });
                } 
                return _autoFormats;
            }
        }
 
        private ViewType CurrentView {
            get { 
                object view = DesignerState["CurrentView"]; 
                return (view == null) ? ViewType.ChangePassword : (ViewType)view;
            } 
            set {
                DesignerState["CurrentView"] = value;
            }
        } 

        private bool Templated { 
            get { 
                return (GetTemplate(_changePassword) != null);
            } 
        }

        private PropertyDescriptor TemplateDescriptor {
            get { 
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(Component);
                string templateName = _templateNames[(int)CurrentView]; 
                PropertyDescriptor templateDescriptor = (PropertyDescriptor)properties.Find(templateName, false); 
                return templateDescriptor;
            } 
        }

        private TemplateDefinition TemplateDefinition {
            get { 
                string templateName = _templateNames[(int)CurrentView];
                return new TemplateDefinition(this, templateName, _changePassword, templateName, ((WebControl)ViewControl).ControlStyle); 
            } 
        }
 
        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups {
            get {
                TemplateGroupCollection groups = base.TemplateGroups; 
                TemplateGroupCollection templateGroups = new TemplateGroupCollection();
                for (int i=0; i < _templateNames.Length; i++) { 
                    string templateName = _templateNames[i]; 
                    TemplateGroup templateGroup = new TemplateGroup(templateName, ((WebControl)ViewControl).ControlStyle);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, templateName, _changePassword, templateName, ((WebControl)ViewControl).ControlStyle)); 
                    templateGroups.Add(templateGroup);
                }
                groups.AddRange(templateGroups);
                return groups; 
            }
        } 
 
        protected override bool UsePreviewControl {
            get { 
                return true;
            }
        }
 
        /// <devdoc>
        /// Transacted change callback to invoke the ConvertToTemplate operation. 
        /// 
        /// Creates a template that looks identical to the current rendering of the control, and tells the control
        /// to use this template.  Allows a page developer to customize the control using its style properties, then 
        /// convert to a template and modify the template for further customization.  The template contains the
        /// necessary server controls with the correct IDs.
        /// </devdoc>
        private bool ConvertToTemplateChangeCallback(object context) { 
            try {
                IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                ConvertToTemplateHelper convertToTemplateHelper = new ConvertToTemplateHelper(this, designerHost); 
                ITemplate template = convertToTemplateHelper.ConvertToTemplate();
                TemplateDescriptor.SetValue(_changePassword, template); 
                return true;
            }
            catch (Exception e) {
                Debug.Fail(e.Message); 
                return false;
            } 
        } 

        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.GetDesignTimeHtml"]/*' /> 
        public override string GetDesignTimeHtml() {
            return GetDesignTimeHtml(null);
        }
 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) {
            IDictionary parameters = new HybridDictionary(2); 
            parameters["CurrentView"] = CurrentView; 

            bool useRegions = UseRegions(regions, GetTemplate(_changePassword)); 
            if (useRegions) {
                // VSWhidbey 382801 Always enable the controls in the designer so we can drag and drop controls
                ((WebControl)ViewControl).Enabled = true;
 
                parameters.Add("RegionEditing", true);
 
                EditableDesignerRegion region = new TemplatedEditableDesignerRegion(TemplateDefinition); 
                region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
                regions.Add(region); 
            }

            string designTimeHtml = String.Empty;
 
            try {
                ((IControlDesignerAccessor)ViewControl).SetDesignModeState(parameters); 
 
                // Make sure the child controls are recreated
                ((ICompositeControlDesignerAccessor)ViewControl).RecreateChildControls(); 

                designTimeHtml = base.GetDesignTimeHtml();
            } catch (Exception e) {
                designTimeHtml = GetErrorDesignTimeHtml(e); 
            }
 
            return designTimeHtml; 
        }
 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            ITemplate template = GetTemplate(_changePassword);
            if (template == null) {
                return GetEmptyDesignTimeHtml(); 
            }
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            return ControlPersister.PersistTemplate(template, host);
        } 

        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.GetErrorDesignTimeHtml"]/*' />
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br />" + e.Message); 
        }
 
        private ITemplate GetTemplate(ChangePassword changePassword) { 
            ITemplate template = null;
            switch (CurrentView) { 
                case ViewType.ChangePassword:
                    template = changePassword.ChangePasswordTemplate;
                    break;
                case ViewType.Success: 
                    template = changePassword.SuccessTemplate;
                    break; 
            } 
            return template;
        } 

        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.Initialize"]/*' />
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(ChangePassword)); 
            _changePassword = (ChangePassword) component;
            base.Initialize(component); 
        } 

        private void LaunchWebAdmin() { 
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (designerHost != null) {
                IWebAdministrationService webadmin = (IWebAdministrationService)designerHost.GetService(typeof(IWebAdministrationService));
                if (webadmin != null) { 
                    webadmin.Start(null);
                } 
            } 
        }
 
        private void ConvertToTemplate() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToTemplateChangeCallback), null,
                                   SR.GetString(SR.WebControls_ConvertToTemplate), TemplateDescriptor);
        } 

        private void Reset() { 
            // 

            UpdateDesignTimeHtml(); 

            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetChangeCallback), null,
                                   SR.GetString(SR.WebControls_Reset), TemplateDescriptor);
        } 

        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc> 
        /// If the control is templated, remove properties that do not apply when templated.
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
            if (Templated) {
                foreach (string propertyName in _nonTemplateProperties) { 
                    PropertyDescriptor property = (PropertyDescriptor) properties[propertyName];
                    Debug.Assert(property != null, "Property is null: " + propertyName); 
                    if (property != null) { 
                        properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                    } 
                }
            }
        }
 
        /// <devdoc>
        /// Transacted change callback to invoke the Reset operation. 
        /// 
        /// Removes the user template from the control, causing it to use the default template.
        /// Applies only to the current view. 
        /// </devdoc>
        private bool ResetChangeCallback(object context) {
            TemplateDescriptor.SetValue(Component, null);
            return true; 
        }
 
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)[region.Name];
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(host != null, "IDesignerHost is null.");
            ITemplate template = ControlParser.ParseTemplate(host, content);
            using (DesignerTransaction transaction = host.CreateTransaction("SetEditableDesignerRegionContent")) {
                descriptor.SetValue(Component, template); 
                transaction.Commit();
            } 
        } 

        private enum ViewType { 
            ChangePassword = 0,
            Success = 1
        }
 
        private class ChangePasswordDesignerActionList : DesignerActionList {
            private ChangePasswordDesigner _designer; 
 
            public ChangePasswordDesignerActionList(ChangePasswordDesigner designer) : base(designer.Component) {
                _designer = designer; 
            }

            public override bool AutoShow {
                get { 
                    return true;
                } 
                set { 
                }
            } 

            [TypeConverter(typeof(ChangePasswordViewTypeConverter))]
            public string View {
                get { 
                    if (_designer.CurrentView == ViewType.ChangePassword) {
                        return SR.GetString(SR.ChangePassword_ChangePasswordView); 
                    } 
                    else {
                        return SR.GetString(SR.ChangePassword_SuccessView); 
                    }
                }
                set {
                    if (String.Compare(value, SR.GetString(SR.ChangePassword_ChangePasswordView), StringComparison.Ordinal) == 0) { 
                        _designer.CurrentView = ViewType.ChangePassword;
                    } 
                    else if (String.Compare(value, SR.GetString(SR.ChangePassword_SuccessView), StringComparison.Ordinal) == 0) { 
                        _designer.CurrentView = ViewType.Success;
                    } 
                    else {
                        Debug.Fail("Unexpected view value");
                    }
 
                    // Update the property grid, since the visible properties may have changed if
                    // the view changed between a templated and non-templated view. 
                    TypeDescriptor.Refresh(_designer.Component); 

                    _designer.UpdateDesignTimeHtml(); 
                }

            }
 
            public void ConvertToTemplate() {
                Cursor originalCursor = Cursor.Current; 
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    _designer.ConvertToTemplate(); 
                }
                finally {
                    Cursor.Current = originalCursor;
                } 
            }
 
            public void LaunchWebAdmin() { 
                _designer.LaunchWebAdmin();
            } 

            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
 
                items.Add(new DesignerActionPropertyItem("View", SR.GetString(SR.WebControls_Views),
                    String.Empty, SR.GetString(SR.WebControls_ViewsDescription))); 
                if (_designer.Templated) { 
                    items.Add(new DesignerActionMethodItem(this, "Reset", SR.GetString(SR.WebControls_Reset), String.Empty,
                        SR.GetString(SR.WebControls_ResetDescriptionViews), true)); 
                }
                else {
                    items.Add(new DesignerActionMethodItem(this, "ConvertToTemplate",
                        SR.GetString(SR.WebControls_ConvertToTemplate), 
                        String.Empty, SR.GetString(SR.WebControls_ConvertToTemplateDescriptionViews), true));
                } 
 
                items.Add(new DesignerActionMethodItem(this, "LaunchWebAdmin", SR.GetString(SR.Login_LaunchWebAdmin),
                    String.Empty, SR.GetString(SR.Login_LaunchWebAdminDescription), true)); 
                return items;
            }

            public void Reset() { 
                _designer.Reset();
            } 
 
            private class ChangePasswordViewTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
                    string[] names = new string[2];

                    names[0] = SR.GetString(SR.ChangePassword_ChangePasswordView);
                    names[1] = SR.GetString(SR.ChangePassword_SuccessView); 

                    return new StandardValuesCollection(names); 
                } 

                public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { 
                    return true;
                }

                public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
                    return true;
                } 
            } 
        }
 
        private sealed class ConvertToTemplateHelper :
                LoginDesignerUtil.GenericConvertToTemplateHelper<ChangePassword, ChangePasswordDesigner> {

            // Controls that are persisted when converting to template 
            private static readonly string[] _persistedControlIDs = new string[] {
                "UserName", 
                "UserNameRequired", 
                "CurrentPassword",
                "CurrentPasswordRequired", 
                "NewPassword",
                "NewPasswordRequired",
                "NewPasswordRegExp",
                "ConfirmNewPassword", 
                "ConfirmNewPasswordRequired",
                "NewPasswordCompare", 
                "ChangePasswordPushButton", 
                "ChangePasswordImageButton",
                "ChangePasswordLinkButton", 
                "CancelPushButton",
                "CancelImageButton",
                "CancelLinkButton",
                "ContinuePushButton", 
                "ContinueImageButton",
                "ContinueLinkButton", 
                "FailureText", 
                "HelpLink",
                "CreateUserLink", 
                "PasswordRecoveryLink",
                "EditProfileLink",
                "EditProfileLinkSuccess",
            }; 

            // Controls that are persisted even if they are not visible when the control is rendered 
            // They are not visible at design-time because the values are computed at runtime 
            private static readonly string[] _persistedIfNotVisibleControlIDs = new string[] {
                "FailureText" 
            };

            public ConvertToTemplateHelper(ChangePasswordDesigner designer, IDesignerHost designerHost) :
                base(designer, designerHost) { 
            }
 
            protected override string[] PersistedControlIDs { 
                get {
                    return _persistedControlIDs; 
                }
            }

            protected override string[] PersistedIfNotVisibleControlIDs { 
                get {
                    return _persistedIfNotVisibleControlIDs; 
                } 
            }
 
            protected override Style GetFailureTextStyle(ChangePassword control) {
                return control.FailureTextStyle;
            }
 
            protected override Control GetDefaultTemplateContents() {
                Control container = null; 
                switch (Designer.CurrentView) { 
                    case ViewType.ChangePassword:
                        container = Designer.ViewControl.Controls[0]; 
                        break;
                    case ViewType.Success:
                        container = Designer.ViewControl.Controls[1];
                        break; 
                }
 
                Table table = (Table)(container.Controls[0]); 
                return table;
            } 

            protected override ITemplate GetTemplate(ChangePassword control) {
                return Designer.GetTemplate(control);
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ChangePasswordDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization; 
    using System.IO;
    using System.Web.UI.WebControls; 
    using Cursor = System.Windows.Forms.Cursor;
    using Cursors = System.Windows.Forms.Cursors;

    /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner"]/*' /> 
    /// <devdoc>
    /// The designer for the ChangePassword control.  Adds verbs for "Auto Format", "Convert To Template", and "Reset". 
    /// When the control is templated, removes properties that do not apply when templated. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ChangePasswordDesigner : ControlDesigner {
        private static DesignerAutoFormatCollection _autoFormats;
        private ChangePassword _changePassword;
 
        private static readonly string[] _templateNames = new string[] {
            "ChangePasswordTemplate", 
            "SuccessTemplate", 
        };
 
        private static readonly string[] _changePasswordViewRegionToPropertyMap = new string[] {
            "ChangePasswordTitleText",
            "UserNameLabelText",
            "PasswordLabelText", 
            "InstructionText",
            "PasswordHintText", 
            "NewPasswordLabelText", 
            "ConfirmNewPasswordLabelText",
        }; 

        private static readonly string[] _successViewRegionToPropertyMap = new string[] {
            "SuccessText",
            "SuccessTitleText", 
        };
 
        private const string _failureTextID = "FailureText"; 

        // Properties that do not apply to the control when it is templated 
        // Removed from the property grid when there is a user template
        private static readonly string[] _nonTemplateProperties = new string[] {
            "BorderPadding",
            "CancelButtonImageUrl", 
            "CancelButtonStyle",
            "CancelButtonText", 
            "CancelButtonType", 
            "ChangePasswordButtonImageUrl",
            "ChangePasswordButtonStyle", 
            "ChangePasswordButtonText",
            "ChangePasswordButtonType",
            "ChangePasswordTitleText",
            "ConfirmNewPasswordLabelText", 
            "ConfirmPasswordCompareErrorMessage",
            "ConfirmPasswordRequiredErrorMessage", 
            "ContinueButtonImageUrl", 
            "ContinueButtonStyle",
            "ContinueButtonText", 
            "ContinueButtonType",
            "CreateUserIconUrl",
            "CreateUserText",
            "CreateUserUrl", 
            "DisplayUserName",
            "EditProfileText", 
            "EditProfileIconUrl", 
            "EditProfileUrl",
            "FailureTextStyle", 
            "HelpPageIconUrl",
            "HelpPageText",
            "HelpPageUrl",
            "HyperLinkStyle", 
            "InstructionText",
            "InstructionTextStyle", 
            "LabelStyle", 
            "NewPasswordLabelText",
            "NewPasswordRequiredErrorMessage", 
            "NewPasswordRegularExpression",
            "NewPasswordRegularExpressionErrorMessage",
            "PasswordHintText",
            "PasswordHintStyle", 
            "PasswordLabelText",
            "PasswordRecoveryText", 
            "PasswordRecoveryUrl", 
            "PasswordRecoveryIconUrl",
            "PasswordRequiredErrorMessage", 
            "SuccessTitleText",
            "SuccessText",
            "SuccessTextStyle",
            "TextBoxStyle", 
            "TitleTextStyle",
            "UserNameLabelText", 
            "UserNameRequiredErrorMessage", 
            "ValidatorTextStyle",
        }; 

        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new ChangePasswordDesignerActionList(this)); 

                return actionLists; 
            }
        }

        public override DesignerAutoFormatCollection AutoFormats { 
            get {
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.CHANGEPASSWORD_SCHEMES, 
                        delegate(DataRow schemeData) { return new ChangePasswordAutoFormat(schemeData); });
                } 
                return _autoFormats;
            }
        }
 
        private ViewType CurrentView {
            get { 
                object view = DesignerState["CurrentView"]; 
                return (view == null) ? ViewType.ChangePassword : (ViewType)view;
            } 
            set {
                DesignerState["CurrentView"] = value;
            }
        } 

        private bool Templated { 
            get { 
                return (GetTemplate(_changePassword) != null);
            } 
        }

        private PropertyDescriptor TemplateDescriptor {
            get { 
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(Component);
                string templateName = _templateNames[(int)CurrentView]; 
                PropertyDescriptor templateDescriptor = (PropertyDescriptor)properties.Find(templateName, false); 
                return templateDescriptor;
            } 
        }

        private TemplateDefinition TemplateDefinition {
            get { 
                string templateName = _templateNames[(int)CurrentView];
                return new TemplateDefinition(this, templateName, _changePassword, templateName, ((WebControl)ViewControl).ControlStyle); 
            } 
        }
 
        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups {
            get {
                TemplateGroupCollection groups = base.TemplateGroups; 
                TemplateGroupCollection templateGroups = new TemplateGroupCollection();
                for (int i=0; i < _templateNames.Length; i++) { 
                    string templateName = _templateNames[i]; 
                    TemplateGroup templateGroup = new TemplateGroup(templateName, ((WebControl)ViewControl).ControlStyle);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, templateName, _changePassword, templateName, ((WebControl)ViewControl).ControlStyle)); 
                    templateGroups.Add(templateGroup);
                }
                groups.AddRange(templateGroups);
                return groups; 
            }
        } 
 
        protected override bool UsePreviewControl {
            get { 
                return true;
            }
        }
 
        /// <devdoc>
        /// Transacted change callback to invoke the ConvertToTemplate operation. 
        /// 
        /// Creates a template that looks identical to the current rendering of the control, and tells the control
        /// to use this template.  Allows a page developer to customize the control using its style properties, then 
        /// convert to a template and modify the template for further customization.  The template contains the
        /// necessary server controls with the correct IDs.
        /// </devdoc>
        private bool ConvertToTemplateChangeCallback(object context) { 
            try {
                IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                ConvertToTemplateHelper convertToTemplateHelper = new ConvertToTemplateHelper(this, designerHost); 
                ITemplate template = convertToTemplateHelper.ConvertToTemplate();
                TemplateDescriptor.SetValue(_changePassword, template); 
                return true;
            }
            catch (Exception e) {
                Debug.Fail(e.Message); 
                return false;
            } 
        } 

        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.GetDesignTimeHtml"]/*' /> 
        public override string GetDesignTimeHtml() {
            return GetDesignTimeHtml(null);
        }
 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) {
            IDictionary parameters = new HybridDictionary(2); 
            parameters["CurrentView"] = CurrentView; 

            bool useRegions = UseRegions(regions, GetTemplate(_changePassword)); 
            if (useRegions) {
                // VSWhidbey 382801 Always enable the controls in the designer so we can drag and drop controls
                ((WebControl)ViewControl).Enabled = true;
 
                parameters.Add("RegionEditing", true);
 
                EditableDesignerRegion region = new TemplatedEditableDesignerRegion(TemplateDefinition); 
                region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
                regions.Add(region); 
            }

            string designTimeHtml = String.Empty;
 
            try {
                ((IControlDesignerAccessor)ViewControl).SetDesignModeState(parameters); 
 
                // Make sure the child controls are recreated
                ((ICompositeControlDesignerAccessor)ViewControl).RecreateChildControls(); 

                designTimeHtml = base.GetDesignTimeHtml();
            } catch (Exception e) {
                designTimeHtml = GetErrorDesignTimeHtml(e); 
            }
 
            return designTimeHtml; 
        }
 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            ITemplate template = GetTemplate(_changePassword);
            if (template == null) {
                return GetEmptyDesignTimeHtml(); 
            }
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            return ControlPersister.PersistTemplate(template, host);
        } 

        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.GetErrorDesignTimeHtml"]/*' />
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br />" + e.Message); 
        }
 
        private ITemplate GetTemplate(ChangePassword changePassword) { 
            ITemplate template = null;
            switch (CurrentView) { 
                case ViewType.ChangePassword:
                    template = changePassword.ChangePasswordTemplate;
                    break;
                case ViewType.Success: 
                    template = changePassword.SuccessTemplate;
                    break; 
            } 
            return template;
        } 

        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.Initialize"]/*' />
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(ChangePassword)); 
            _changePassword = (ChangePassword) component;
            base.Initialize(component); 
        } 

        private void LaunchWebAdmin() { 
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (designerHost != null) {
                IWebAdministrationService webadmin = (IWebAdministrationService)designerHost.GetService(typeof(IWebAdministrationService));
                if (webadmin != null) { 
                    webadmin.Start(null);
                } 
            } 
        }
 
        private void ConvertToTemplate() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToTemplateChangeCallback), null,
                                   SR.GetString(SR.WebControls_ConvertToTemplate), TemplateDescriptor);
        } 

        private void Reset() { 
            // 

            UpdateDesignTimeHtml(); 

            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetChangeCallback), null,
                                   SR.GetString(SR.WebControls_Reset), TemplateDescriptor);
        } 

        /// <include file='doc\ChangePasswordDesigner.uex' path='docs/doc[@for="ChangePasswordDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc> 
        /// If the control is templated, remove properties that do not apply when templated.
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
            if (Templated) {
                foreach (string propertyName in _nonTemplateProperties) { 
                    PropertyDescriptor property = (PropertyDescriptor) properties[propertyName];
                    Debug.Assert(property != null, "Property is null: " + propertyName); 
                    if (property != null) { 
                        properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                    } 
                }
            }
        }
 
        /// <devdoc>
        /// Transacted change callback to invoke the Reset operation. 
        /// 
        /// Removes the user template from the control, causing it to use the default template.
        /// Applies only to the current view. 
        /// </devdoc>
        private bool ResetChangeCallback(object context) {
            TemplateDescriptor.SetValue(Component, null);
            return true; 
        }
 
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)[region.Name];
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(host != null, "IDesignerHost is null.");
            ITemplate template = ControlParser.ParseTemplate(host, content);
            using (DesignerTransaction transaction = host.CreateTransaction("SetEditableDesignerRegionContent")) {
                descriptor.SetValue(Component, template); 
                transaction.Commit();
            } 
        } 

        private enum ViewType { 
            ChangePassword = 0,
            Success = 1
        }
 
        private class ChangePasswordDesignerActionList : DesignerActionList {
            private ChangePasswordDesigner _designer; 
 
            public ChangePasswordDesignerActionList(ChangePasswordDesigner designer) : base(designer.Component) {
                _designer = designer; 
            }

            public override bool AutoShow {
                get { 
                    return true;
                } 
                set { 
                }
            } 

            [TypeConverter(typeof(ChangePasswordViewTypeConverter))]
            public string View {
                get { 
                    if (_designer.CurrentView == ViewType.ChangePassword) {
                        return SR.GetString(SR.ChangePassword_ChangePasswordView); 
                    } 
                    else {
                        return SR.GetString(SR.ChangePassword_SuccessView); 
                    }
                }
                set {
                    if (String.Compare(value, SR.GetString(SR.ChangePassword_ChangePasswordView), StringComparison.Ordinal) == 0) { 
                        _designer.CurrentView = ViewType.ChangePassword;
                    } 
                    else if (String.Compare(value, SR.GetString(SR.ChangePassword_SuccessView), StringComparison.Ordinal) == 0) { 
                        _designer.CurrentView = ViewType.Success;
                    } 
                    else {
                        Debug.Fail("Unexpected view value");
                    }
 
                    // Update the property grid, since the visible properties may have changed if
                    // the view changed between a templated and non-templated view. 
                    TypeDescriptor.Refresh(_designer.Component); 

                    _designer.UpdateDesignTimeHtml(); 
                }

            }
 
            public void ConvertToTemplate() {
                Cursor originalCursor = Cursor.Current; 
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    _designer.ConvertToTemplate(); 
                }
                finally {
                    Cursor.Current = originalCursor;
                } 
            }
 
            public void LaunchWebAdmin() { 
                _designer.LaunchWebAdmin();
            } 

            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
 
                items.Add(new DesignerActionPropertyItem("View", SR.GetString(SR.WebControls_Views),
                    String.Empty, SR.GetString(SR.WebControls_ViewsDescription))); 
                if (_designer.Templated) { 
                    items.Add(new DesignerActionMethodItem(this, "Reset", SR.GetString(SR.WebControls_Reset), String.Empty,
                        SR.GetString(SR.WebControls_ResetDescriptionViews), true)); 
                }
                else {
                    items.Add(new DesignerActionMethodItem(this, "ConvertToTemplate",
                        SR.GetString(SR.WebControls_ConvertToTemplate), 
                        String.Empty, SR.GetString(SR.WebControls_ConvertToTemplateDescriptionViews), true));
                } 
 
                items.Add(new DesignerActionMethodItem(this, "LaunchWebAdmin", SR.GetString(SR.Login_LaunchWebAdmin),
                    String.Empty, SR.GetString(SR.Login_LaunchWebAdminDescription), true)); 
                return items;
            }

            public void Reset() { 
                _designer.Reset();
            } 
 
            private class ChangePasswordViewTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
                    string[] names = new string[2];

                    names[0] = SR.GetString(SR.ChangePassword_ChangePasswordView);
                    names[1] = SR.GetString(SR.ChangePassword_SuccessView); 

                    return new StandardValuesCollection(names); 
                } 

                public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { 
                    return true;
                }

                public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { 
                    return true;
                } 
            } 
        }
 
        private sealed class ConvertToTemplateHelper :
                LoginDesignerUtil.GenericConvertToTemplateHelper<ChangePassword, ChangePasswordDesigner> {

            // Controls that are persisted when converting to template 
            private static readonly string[] _persistedControlIDs = new string[] {
                "UserName", 
                "UserNameRequired", 
                "CurrentPassword",
                "CurrentPasswordRequired", 
                "NewPassword",
                "NewPasswordRequired",
                "NewPasswordRegExp",
                "ConfirmNewPassword", 
                "ConfirmNewPasswordRequired",
                "NewPasswordCompare", 
                "ChangePasswordPushButton", 
                "ChangePasswordImageButton",
                "ChangePasswordLinkButton", 
                "CancelPushButton",
                "CancelImageButton",
                "CancelLinkButton",
                "ContinuePushButton", 
                "ContinueImageButton",
                "ContinueLinkButton", 
                "FailureText", 
                "HelpLink",
                "CreateUserLink", 
                "PasswordRecoveryLink",
                "EditProfileLink",
                "EditProfileLinkSuccess",
            }; 

            // Controls that are persisted even if they are not visible when the control is rendered 
            // They are not visible at design-time because the values are computed at runtime 
            private static readonly string[] _persistedIfNotVisibleControlIDs = new string[] {
                "FailureText" 
            };

            public ConvertToTemplateHelper(ChangePasswordDesigner designer, IDesignerHost designerHost) :
                base(designer, designerHost) { 
            }
 
            protected override string[] PersistedControlIDs { 
                get {
                    return _persistedControlIDs; 
                }
            }

            protected override string[] PersistedIfNotVisibleControlIDs { 
                get {
                    return _persistedIfNotVisibleControlIDs; 
                } 
            }
 
            protected override Style GetFailureTextStyle(ChangePassword control) {
                return control.FailureTextStyle;
            }
 
            protected override Control GetDefaultTemplateContents() {
                Control container = null; 
                switch (Designer.CurrentView) { 
                    case ViewType.ChangePassword:
                        container = Designer.ViewControl.Controls[0]; 
                        break;
                    case ViewType.Success:
                        container = Designer.ViewControl.Controls[1];
                        break; 
                }
 
                Table table = (Table)(container.Controls[0]); 
                return table;
            } 

            protected override ITemplate GetTemplate(ChangePassword control) {
                return Designer.GetTemplate(control);
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
