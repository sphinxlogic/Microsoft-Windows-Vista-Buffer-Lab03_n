//------------------------------------------------------------------------------ 
// <copyright file="LoginDesigner.cs" company="Microsoft">
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
    using System.Web.UI.Design;
    using System.Web.UI;
    using Cursor = System.Windows.Forms.Cursor;
    using Cursors = System.Windows.Forms.Cursors; 

    /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner"]/*' /> 
    /// <devdoc> 
    /// The designer for the Login control.  Adds verbs for "Auto Format", "Convert To Template", and "Reset".
    /// When the control is templated, removes properties that do not apply when templated. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class LoginDesigner : CompositeControlDesigner {
 
        private Login _login;
 
        private static DesignerAutoFormatCollection _autoFormats; 

        private const string _templateName = "LayoutTemplate"; 
        private const string _failureTextID = "FailureText";

        // Properties that do not apply to the control when it is templated
        // Removed from the property grid when there is a user template 
        private static readonly string[] _nonTemplateProperties = new string[] {
            "BorderPadding", 
            "CheckBoxStyle", 
            "CreateUserIconUrl",
            "CreateUserText", 
            "CreateUserUrl",
            "DisplayRememberMe",
            "FailureTextStyle",
            "HelpPageIconUrl", 
            "HelpPageText",
            "HelpPageUrl", 
            "HyperLinkStyle", 
            "InstructionText",
            "InstructionTextStyle", 
            "LabelStyle",
            "Orientation",
            "PasswordLabelText",
            "PasswordRecoveryIconUrl", 
            "PasswordRecoveryText",
            "PasswordRecoveryUrl", 
            "PasswordRequiredErrorMessage", 
            "RememberMeText",
            "LoginButtonImageUrl", 
            "LoginButtonStyle",
            "LoginButtonText",
            "LoginButtonType",
            "TextBoxStyle", 
            "TextLayout",
            "TitleText", 
            "TitleTextStyle", 
            "UserNameLabelText",
            "UserNameRequiredErrorMessage", 
            "ValidatorTextStyle",
        };

        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new LoginDesignerActionList(this)); 
                return actionLists;
            }
        }
 
        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.LOGIN_SCHEMES,
                        delegate(DataRow schemeData) { return new LoginAutoFormat(schemeData); }); 
                }
                return _autoFormats;
            }
        } 

        private bool Templated { 
            get { 
                return (_login.LayoutTemplate != null);
            } 
        }

        private TemplateDefinition TemplateDefinition {
            get { 
                return new TemplateDefinition(this, _templateName, _login, _templateName, ((WebControl)ViewControl).ControlStyle);
            } 
        } 

        private PropertyDescriptor TemplateDescriptor { 
            get {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(Component);
                PropertyDescriptor templateDescriptor = (PropertyDescriptor)properties.Find(_templateName, false);
                return templateDescriptor; 
            }
        } 
 
        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups { 
            get {
                TemplateGroupCollection groups = base.TemplateGroups;
                TemplateGroup templateGroup = new TemplateGroup(_templateName, ((WebControl)ViewControl).ControlStyle);
                templateGroup.AddTemplateDefinition(new TemplateDefinition(this, _templateName, _login, _templateName, ((WebControl)ViewControl).ControlStyle)); 
                groups.Add(templateGroup);
                return groups; 
            } 
        }
 
        protected override bool UsePreviewControl {
            get {
                return true;
            } 
        }
 
        private void ConvertToTemplate() { 
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToTemplateChangeCallback), null,
                                   SR.GetString(SR.WebControls_ConvertToTemplate), TemplateDescriptor); 
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
                TemplateDescriptor.SetValue(_login, template); 
                return true;
            } 
            catch (Exception e) {
                Debug.Fail(e.Message);
                return false;
            } 
        }
 
        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.GetErrorDesignTimeHtml"]/*' /> 
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br />" + e.Message); 
        }

        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.Initialize"]/*' />
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(Login));
            _login = (Login) component; 
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

        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.PreFilterProperties"]/*' />
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
 
        private void Reset() {
            //

            UpdateDesignTimeHtml(); 

            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetChangeCallback), null, 
                                   SR.GetString(SR.WebControls_Reset), TemplateDescriptor); 
        }
 
        /// <devdoc>
        /// Transacted change callback to invoke the Reset operation.
        ///
        /// Removes the user template from the control, causing it to use the default template. 
        /// </devdoc>
        private bool ResetChangeCallback(object context) { 
            TemplateDescriptor.SetValue(_login, null); 
            return true;
        } 

        public override string GetDesignTimeHtml(DesignerRegionCollection regions) {
            bool useRegions = UseRegions(regions, _login.LayoutTemplate);
            if (useRegions) { 
                // VSWhidbey 382801 Always enable the controls in the designer so we can drag and drop controls
                ((WebControl)ViewControl).Enabled = true; 
 
                IDictionary parameters = new HybridDictionary(1);
                parameters.Add("RegionEditing", true); 
                ((IControlDesignerAccessor)ViewControl).SetDesignModeState(parameters);

                EditableDesignerRegion region = new TemplatedEditableDesignerRegion(TemplateDefinition);
                region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark); 
                regions.Add(region);
            } 
            return GetDesignTimeHtml(); 
        }
 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            return ControlPersister.PersistTemplate(_login.LayoutTemplate, host);
        } 

        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(host != null, "IDesignerHost is null.");
 
            ITemplate template = ControlParser.ParseTemplate(host, content);
            // Region name maps to the template property
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)[region.Name];
            using (DesignerTransaction transaction = host.CreateTransaction("SetEditableDesignerRegionContent")) { 
                descriptor.SetValue(Component, template);
                transaction.Commit(); 
            } 
        }
 
        private class LoginDesignerActionList : DesignerActionList {
            private LoginDesigner _parent;

            public LoginDesignerActionList(LoginDesigner parent) : base(parent.Component) { 
                _parent = parent;
            } 
 
            public override bool AutoShow {
                get { 
                    return true;
                }
                set {
                } 
            }
 
            public void ConvertToTemplate() { 
                Cursor originalCursor = Cursor.Current;
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    _parent.ConvertToTemplate();
                }
                finally { 
                    Cursor.Current = originalCursor;
                } 
            } 

            public void LaunchWebAdmin() { 
                _parent.LaunchWebAdmin();
            }

            public void Reset() { 
                _parent.Reset();
            } 
 
            public override DesignerActionItemCollection GetSortedActionItems() {
                if (_parent.InTemplateMode) { 
                    return new DesignerActionItemCollection();
                }

                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                if (!_parent.Templated) {
                    items.Add(new DesignerActionMethodItem(this, "ConvertToTemplate", 
                        SR.GetString(SR.WebControls_ConvertToTemplate), String.Empty, 
                        SR.GetString(SR.WebControls_ConvertToTemplateDescription), true));
                } 
                else {
                    items.Add(new DesignerActionMethodItem(this, "Reset", SR.GetString(SR.WebControls_Reset),
                         String.Empty, SR.GetString(SR.WebControls_ResetDescription), true));
                } 

                items.Add(new DesignerActionMethodItem(this, "LaunchWebAdmin", SR.GetString(SR.Login_LaunchWebAdmin), 
                    String.Empty, SR.GetString(SR.Login_LaunchWebAdminDescription), true)); 
                return items;
            } 
        }

        private sealed class ConvertToTemplateHelper :
                LoginDesignerUtil.GenericConvertToTemplateHelper<Login, LoginDesigner> { 

            // Controls that are persisted when converting to template 
            private static readonly string[] _persistedControlIDs = new string[] { 
                "UserName",
                "UserNameRequired", 
                "Password",
                "PasswordRequired",
                "RememberMe",
                "LoginButton", 
                "LoginImageButton",
                "LoginLinkButton", 
                "FailureText", 
                "CreateUserLink",
                "PasswordRecoveryLink", 
                "HelpLink",
            };

            // Controls that are persisted even if they are not visible when the control is rendered 
            // They are not visible at design-time because the values are computed at runtime
            private static readonly string[] _persistedIfNotVisibleControlIDs = new string[] { 
                "FailureText" 
            };
 
            public ConvertToTemplateHelper(LoginDesigner designer, IDesignerHost designerHost) :
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
 
            protected override Style GetFailureTextStyle(Login control) {
                return control.FailureTextStyle; 
            }

            protected override Control GetDefaultTemplateContents() {
                Control loginContainer = Designer.ViewControl.Controls[0]; 
                Table table = (Table)(loginContainer.Controls[0]);
                return table; 
            } 

            protected override ITemplate GetTemplate(Login control) { 
                return control.LayoutTemplate;
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="LoginDesigner.cs" company="Microsoft">
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
    using System.Web.UI.Design;
    using System.Web.UI;
    using Cursor = System.Windows.Forms.Cursor;
    using Cursors = System.Windows.Forms.Cursors; 

    /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner"]/*' /> 
    /// <devdoc> 
    /// The designer for the Login control.  Adds verbs for "Auto Format", "Convert To Template", and "Reset".
    /// When the control is templated, removes properties that do not apply when templated. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class LoginDesigner : CompositeControlDesigner {
 
        private Login _login;
 
        private static DesignerAutoFormatCollection _autoFormats; 

        private const string _templateName = "LayoutTemplate"; 
        private const string _failureTextID = "FailureText";

        // Properties that do not apply to the control when it is templated
        // Removed from the property grid when there is a user template 
        private static readonly string[] _nonTemplateProperties = new string[] {
            "BorderPadding", 
            "CheckBoxStyle", 
            "CreateUserIconUrl",
            "CreateUserText", 
            "CreateUserUrl",
            "DisplayRememberMe",
            "FailureTextStyle",
            "HelpPageIconUrl", 
            "HelpPageText",
            "HelpPageUrl", 
            "HyperLinkStyle", 
            "InstructionText",
            "InstructionTextStyle", 
            "LabelStyle",
            "Orientation",
            "PasswordLabelText",
            "PasswordRecoveryIconUrl", 
            "PasswordRecoveryText",
            "PasswordRecoveryUrl", 
            "PasswordRequiredErrorMessage", 
            "RememberMeText",
            "LoginButtonImageUrl", 
            "LoginButtonStyle",
            "LoginButtonText",
            "LoginButtonType",
            "TextBoxStyle", 
            "TextLayout",
            "TitleText", 
            "TitleTextStyle", 
            "UserNameLabelText",
            "UserNameRequiredErrorMessage", 
            "ValidatorTextStyle",
        };

        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new LoginDesignerActionList(this)); 
                return actionLists;
            }
        }
 
        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.LOGIN_SCHEMES,
                        delegate(DataRow schemeData) { return new LoginAutoFormat(schemeData); }); 
                }
                return _autoFormats;
            }
        } 

        private bool Templated { 
            get { 
                return (_login.LayoutTemplate != null);
            } 
        }

        private TemplateDefinition TemplateDefinition {
            get { 
                return new TemplateDefinition(this, _templateName, _login, _templateName, ((WebControl)ViewControl).ControlStyle);
            } 
        } 

        private PropertyDescriptor TemplateDescriptor { 
            get {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(Component);
                PropertyDescriptor templateDescriptor = (PropertyDescriptor)properties.Find(_templateName, false);
                return templateDescriptor; 
            }
        } 
 
        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups { 
            get {
                TemplateGroupCollection groups = base.TemplateGroups;
                TemplateGroup templateGroup = new TemplateGroup(_templateName, ((WebControl)ViewControl).ControlStyle);
                templateGroup.AddTemplateDefinition(new TemplateDefinition(this, _templateName, _login, _templateName, ((WebControl)ViewControl).ControlStyle)); 
                groups.Add(templateGroup);
                return groups; 
            } 
        }
 
        protected override bool UsePreviewControl {
            get {
                return true;
            } 
        }
 
        private void ConvertToTemplate() { 
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToTemplateChangeCallback), null,
                                   SR.GetString(SR.WebControls_ConvertToTemplate), TemplateDescriptor); 
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
                TemplateDescriptor.SetValue(_login, template); 
                return true;
            } 
            catch (Exception e) {
                Debug.Fail(e.Message);
                return false;
            } 
        }
 
        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.GetErrorDesignTimeHtml"]/*' /> 
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br />" + e.Message); 
        }

        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.Initialize"]/*' />
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(Login));
            _login = (Login) component; 
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

        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.PreFilterProperties"]/*' />
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
 
        private void Reset() {
            //

            UpdateDesignTimeHtml(); 

            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetChangeCallback), null, 
                                   SR.GetString(SR.WebControls_Reset), TemplateDescriptor); 
        }
 
        /// <devdoc>
        /// Transacted change callback to invoke the Reset operation.
        ///
        /// Removes the user template from the control, causing it to use the default template. 
        /// </devdoc>
        private bool ResetChangeCallback(object context) { 
            TemplateDescriptor.SetValue(_login, null); 
            return true;
        } 

        public override string GetDesignTimeHtml(DesignerRegionCollection regions) {
            bool useRegions = UseRegions(regions, _login.LayoutTemplate);
            if (useRegions) { 
                // VSWhidbey 382801 Always enable the controls in the designer so we can drag and drop controls
                ((WebControl)ViewControl).Enabled = true; 
 
                IDictionary parameters = new HybridDictionary(1);
                parameters.Add("RegionEditing", true); 
                ((IControlDesignerAccessor)ViewControl).SetDesignModeState(parameters);

                EditableDesignerRegion region = new TemplatedEditableDesignerRegion(TemplateDefinition);
                region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark); 
                regions.Add(region);
            } 
            return GetDesignTimeHtml(); 
        }
 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            return ControlPersister.PersistTemplate(_login.LayoutTemplate, host);
        } 

        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(host != null, "IDesignerHost is null.");
 
            ITemplate template = ControlParser.ParseTemplate(host, content);
            // Region name maps to the template property
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)[region.Name];
            using (DesignerTransaction transaction = host.CreateTransaction("SetEditableDesignerRegionContent")) { 
                descriptor.SetValue(Component, template);
                transaction.Commit(); 
            } 
        }
 
        private class LoginDesignerActionList : DesignerActionList {
            private LoginDesigner _parent;

            public LoginDesignerActionList(LoginDesigner parent) : base(parent.Component) { 
                _parent = parent;
            } 
 
            public override bool AutoShow {
                get { 
                    return true;
                }
                set {
                } 
            }
 
            public void ConvertToTemplate() { 
                Cursor originalCursor = Cursor.Current;
                try { 
                    Cursor.Current = Cursors.WaitCursor;
                    _parent.ConvertToTemplate();
                }
                finally { 
                    Cursor.Current = originalCursor;
                } 
            } 

            public void LaunchWebAdmin() { 
                _parent.LaunchWebAdmin();
            }

            public void Reset() { 
                _parent.Reset();
            } 
 
            public override DesignerActionItemCollection GetSortedActionItems() {
                if (_parent.InTemplateMode) { 
                    return new DesignerActionItemCollection();
                }

                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                if (!_parent.Templated) {
                    items.Add(new DesignerActionMethodItem(this, "ConvertToTemplate", 
                        SR.GetString(SR.WebControls_ConvertToTemplate), String.Empty, 
                        SR.GetString(SR.WebControls_ConvertToTemplateDescription), true));
                } 
                else {
                    items.Add(new DesignerActionMethodItem(this, "Reset", SR.GetString(SR.WebControls_Reset),
                         String.Empty, SR.GetString(SR.WebControls_ResetDescription), true));
                } 

                items.Add(new DesignerActionMethodItem(this, "LaunchWebAdmin", SR.GetString(SR.Login_LaunchWebAdmin), 
                    String.Empty, SR.GetString(SR.Login_LaunchWebAdminDescription), true)); 
                return items;
            } 
        }

        private sealed class ConvertToTemplateHelper :
                LoginDesignerUtil.GenericConvertToTemplateHelper<Login, LoginDesigner> { 

            // Controls that are persisted when converting to template 
            private static readonly string[] _persistedControlIDs = new string[] { 
                "UserName",
                "UserNameRequired", 
                "Password",
                "PasswordRequired",
                "RememberMe",
                "LoginButton", 
                "LoginImageButton",
                "LoginLinkButton", 
                "FailureText", 
                "CreateUserLink",
                "PasswordRecoveryLink", 
                "HelpLink",
            };

            // Controls that are persisted even if they are not visible when the control is rendered 
            // They are not visible at design-time because the values are computed at runtime
            private static readonly string[] _persistedIfNotVisibleControlIDs = new string[] { 
                "FailureText" 
            };
 
            public ConvertToTemplateHelper(LoginDesigner designer, IDesignerHost designerHost) :
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
 
            protected override Style GetFailureTextStyle(Login control) {
                return control.FailureTextStyle; 
            }

            protected override Control GetDefaultTemplateContents() {
                Control loginContainer = Designer.ViewControl.Controls[0]; 
                Table table = (Table)(loginContainer.Controls[0]);
                return table; 
            } 

            protected override ITemplate GetTemplate(Login control) { 
                return control.LayoutTemplate;
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
