//------------------------------------------------------------------------------ 
// <copyright file="PasswordRecoveryDesigner.cs" company="Microsoft">
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

    /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner"]/*' /> 
    /// <devdoc>
    /// The designer for the PasswordRecovery control.  Adds verbs for "Auto Format", "Switch View", "Convert To Template", and "Reset". 
    /// When the control is templated, removes properties that do not apply when templated. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class PasswordRecoveryDesigner : ControlDesigner {

        private PasswordRecovery _passwordRecovery;
        private static DesignerAutoFormatCollection _autoFormats; 
        private const string _failureTextID = "FailureText";
 
        private static readonly string[] _userNameViewRegionToPropertyMap = new string[] { 
            "UserNameLabelText",
            "UserNameTitleText", 
            "UserNameInstructionText",
        };

        private static readonly string[] _questionViewRegionToPropertyMap = new string[] { 
            "UserNameLabelText",
            "QuestionTitleText", 
            "QuestionLabelText", 
            "QuestionInstructionText",
            "AnswerLabelText", 
        };

        private static readonly string[] _successViewRegionToPropertyMap = new string[] {
            "SuccessText", 
        };
 
 
        private static readonly string[] _templateNames = new string[] {
            "UserNameTemplate", 
            "QuestionTemplate",
            "SuccessTemplate",
        };
 
        // Properties that do not apply to the control when it is templated
        // Removed from the property grid when there is a user template 
        private static readonly string[] _nonTemplateProperties = new string[] { 
            "AnswerLabelText",
            "AnswerRequiredErrorMessage", 
            "BorderPadding",
            "HelpPageIconUrl",
            "FailureTextStyle",
            "HelpPageText", 
            "HelpPageUrl",
            "HyperLinkStyle", 
            "InstructionTextStyle", 
            "LabelStyle",
            "QuestionInstructionText", 
            "QuestionLabelText",
            "QuestionTitleText",
            "SubmitButtonImageUrl",
            "SubmitButtonStyle", 
            "SubmitButtonText",
            "SubmitButtonType", 
            "SuccessText", 
            "SuccessTextStyle",
            "TextBoxStyle", 
            "TextLayout",
            "TitleTextStyle",
            "UserNameInstructionText",
            "UserNameLabelText", 
            "UserNameRequiredErrorMessage",
            "UserNameTitleText", 
            "ValidatorTextStyle", 
        };
 
        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new PasswordRecoveryDesignerActionList(this)); 
 
                return actionLists;
            } 
        }

        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.PASSWORDRECOVERY_SCHEMES, 
                        delegate(DataRow schemeData) { return new PasswordRecoveryAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            }
        }

        private ViewType CurrentView { 
            get {
                object view = DesignerState["CurrentView"]; 
                return (view == null) ? ViewType.UserName : (ViewType)view; 
            }
            set { 
                DesignerState["CurrentView"] = value;
            }
        }
 
        /// <devdoc>
        /// Returns true if the current view is templated. 
        /// </devdoc> 
        private bool Templated {
            get { 
                return (GetTemplate(_passwordRecovery) != null);
            }
        }
 
        private TemplateDefinition TemplateDefinition {
            get { 
                string templateName = _templateNames[(int)CurrentView]; 
                return new TemplateDefinition(this, templateName, _passwordRecovery, templateName, ((WebControl)ViewControl).ControlStyle);
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

        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups { 
            get {
                TemplateGroupCollection groups = base.TemplateGroups; 
                TemplateGroupCollection templateGroups = new TemplateGroupCollection(); 
                for (int i = 0; i < _templateNames.Length; i++) {
                    string templateName = _templateNames[i]; 
                    TemplateGroup templateGroup = new TemplateGroup(templateName, ((WebControl)ViewControl).ControlStyle);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, templateName, _passwordRecovery, templateName, ((WebControl)ViewControl).ControlStyle));
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
 
        private bool ConvertToTemplateChangeCallback(object context) {
            try { 
                IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
                ConvertToTemplateHelper convertToTemplateHelper = new ConvertToTemplateHelper(this, designerHost);
                ITemplate template = convertToTemplateHelper.ConvertToTemplate();
                TemplateDescriptor.SetValue(_passwordRecovery, template); 
                return true;
            } 
            catch (Exception e) { 
                Debug.Fail(e.Message);
                return false; 
            }
        }

        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.GetDesignTimeHtml"]/*' /> 
        public override string GetDesignTimeHtml() {
            string designTimeHtml; 
            try { 
                IDictionary parameters = new HybridDictionary(1);
                parameters["CurrentView"] = CurrentView; 
                ((IControlDesignerAccessor)ViewControl).SetDesignModeState(parameters);

                // Make sure the child controls are recreated
                ICompositeControlDesignerAccessor designerAccessor = (ICompositeControlDesignerAccessor)ViewControl; 
                designerAccessor.RecreateChildControls();
 
                designTimeHtml = base.GetDesignTimeHtml(); 
            } catch (Exception e) {
                designTimeHtml = GetErrorDesignTimeHtml(e); 
            }
            return designTimeHtml;
        }
 
        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.GetErrorDesignTimeHtml"]/*' />
        protected override string GetErrorDesignTimeHtml(Exception e) { 
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br />" + e.Message); 
        }
 
        private ITemplate GetTemplate(PasswordRecovery passwordRecovery) {
            ITemplate template = null;
            switch (CurrentView) {
                case ViewType.UserName: 
                    template = passwordRecovery.UserNameTemplate;
                    break; 
                case ViewType.Question: 
                    template = passwordRecovery.QuestionTemplate;
                    break; 
                case ViewType.Success:
                    template = passwordRecovery.SuccessTemplate;
                    break;
            } 
            return template;
        } 
 
        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.Initialize"]/*' />
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(PasswordRecovery));
            _passwordRecovery = (PasswordRecovery) component;
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

        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        /// If the current view is templated, remove properties that do not apply when templated.
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
            try { 
                TemplateDescriptor.SetValue(_passwordRecovery, null);
                return true;
            }
            catch (Exception ex) { 
                Debug.Fail(ex.Message);
                return false; 
            } 
        }
 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) {
            bool useRegions = UseRegions(regions, GetTemplate(_passwordRecovery));
            if (useRegions) {
                EditableDesignerRegion region = new TemplatedEditableDesignerRegion(TemplateDefinition); 
                region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
                regions.Add(region); 
 
                // VSWhidbey 382801 Always enable the controls in the designer so we can drag and drop controls
                ((WebControl)ViewControl).Enabled = true; 

                IDictionary parameters = new HybridDictionary(1);
                parameters.Add("RegionEditing", true);
                ((IControlDesignerAccessor)ViewControl).SetDesignModeState(parameters); 
            }
 
            return GetDesignTimeHtml(); 
        }
 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            ITemplate template = GetTemplate(_passwordRecovery);
            if (template == null) {
                return String.Empty; 
            }
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            return ControlPersister.PersistTemplate(template, host);
        } 

        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) {
            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost));
            Debug.Assert(host != null, "IDesignerHost is null."); 

            ITemplate template = ControlParser.ParseTemplate(host, content); 
            // Region name maps to the template property 
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)[region.Name];
            using (DesignerTransaction transaction = host.CreateTransaction("SetEditableDesignerRegionContent")) { 
                descriptor.SetValue(Component, template);
                transaction.Commit();
            }
        } 

        private enum ViewType { 
            UserName = 0, 
            Question = 1,
            Success = 2 
        }

        private class PasswordRecoveryDesignerActionList : DesignerActionList {
            private PasswordRecoveryDesigner _designer; 

            public PasswordRecoveryDesignerActionList(PasswordRecoveryDesigner designer) : base(designer.Component) { 
                _designer = designer; 
            }
 
            public override bool AutoShow {
                get {
                    return true;
                } 
                set {
                } 
            } 

            [TypeConverter(typeof(PasswordRecoveryViewTypeConverter))] 
            public string View {
                get {
                    if (_designer.CurrentView == ViewType.UserName) {
                        return SR.GetString(SR.PasswordRecovery_UserNameView); 
                    }
                    else if (_designer.CurrentView == ViewType.Question) { 
                        return SR.GetString(SR.PasswordRecovery_QuestionView); 
                    }
                    else if (_designer.CurrentView == ViewType.Success) { 
                        return SR.GetString(SR.PasswordRecovery_SuccessView);
                    }
                    else {
                        Debug.Fail("Unexpected view value"); 
                    }
 
                    return String.Empty; 
                }
                set { 
                    if (String.Compare(value, SR.GetString(SR.PasswordRecovery_UserNameView), StringComparison.Ordinal) == 0) {
                        _designer.CurrentView = ViewType.UserName;
                    }
                    else if (String.Compare(value, SR.GetString(SR.PasswordRecovery_QuestionView), StringComparison.Ordinal) == 0) { 
                        _designer.CurrentView = ViewType.Question;
                    } 
                    else if (String.Compare(value, SR.GetString(SR.PasswordRecovery_SuccessView), StringComparison.Ordinal) == 0) { 
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
                if (!_designer.InTemplateMode) { 
                    if (_designer.Templated) {
                        items.Add(new DesignerActionMethodItem(this, "Reset", SR.GetString(SR.WebControls_Reset), 
                            String.Empty, SR.GetString(SR.WebControls_ResetDescriptionViews), true));
                    }
                    else {
                        items.Add(new DesignerActionMethodItem(this, "ConvertToTemplate", 
                            SR.GetString(SR.WebControls_ConvertToTemplate), String.Empty,
                            SR.GetString(SR.WebControls_ConvertToTemplateDescriptionViews), true)); 
                    } 
                }
 
                items.Add(new DesignerActionMethodItem(this, "LaunchWebAdmin", SR.GetString(SR.Login_LaunchWebAdmin),
                    String.Empty, SR.GetString(SR.Login_LaunchWebAdminDescription), true));
                return items;
            } 

            public void Reset() { 
                _designer.Reset(); 
            }
 
            private class PasswordRecoveryViewTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    string[] names = new string[3];
 
                    names[0] = SR.GetString(SR.PasswordRecovery_UserNameView);
                    names[1] = SR.GetString(SR.PasswordRecovery_QuestionView); 
                    names[2] = SR.GetString(SR.PasswordRecovery_SuccessView); 

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
                LoginDesignerUtil.GenericConvertToTemplateHelper<PasswordRecovery, PasswordRecoveryDesigner> { 
 
            // Controls that are persisted when converting to template
            private static readonly string[] _persistedControlIDs = new string[] { 
                "UserName",
                "UserNameRequired",
                "Question",
                "Answer", 
                "AnswerRequired",
                "SubmitButton", 
                "SubmitImageButton", 
                "SubmitLinkButton",
                "FailureText", 
                "HelpLink",
            };

            // Controls that are persisted even if they are not visible when the control is rendered 
            // They are not visible at design-time because the values are computed at runtime
            private static readonly string[] _persistedIfNotVisibleControlIDs = new string[] { 
                "UserName", 
                "Question",
                "FailureText" 
            };

            public ConvertToTemplateHelper(PasswordRecoveryDesigner designer, IDesignerHost designerHost) :
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
 
            protected override Style GetFailureTextStyle(PasswordRecovery control) {
                return control.FailureTextStyle;
            }
 
            protected override Control GetDefaultTemplateContents() {
                Control container = null; 
                switch (Designer.CurrentView) { 
                    case ViewType.UserName:
                        container = Designer.ViewControl.Controls[0]; 
                        break;
                    case ViewType.Question:
                        container = Designer.ViewControl.Controls[1];
                        break; 
                    case ViewType.Success:
                        container = Designer.ViewControl.Controls[2]; 
                        break; 
                }
 
                Table table = (Table)(container.Controls[0]);
                return table;
            }
 
            protected override ITemplate GetTemplate(PasswordRecovery control) {
                return Designer.GetTemplate(control); 
            } 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="PasswordRecoveryDesigner.cs" company="Microsoft">
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

    /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner"]/*' /> 
    /// <devdoc>
    /// The designer for the PasswordRecovery control.  Adds verbs for "Auto Format", "Switch View", "Convert To Template", and "Reset". 
    /// When the control is templated, removes properties that do not apply when templated. 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class PasswordRecoveryDesigner : ControlDesigner {

        private PasswordRecovery _passwordRecovery;
        private static DesignerAutoFormatCollection _autoFormats; 
        private const string _failureTextID = "FailureText";
 
        private static readonly string[] _userNameViewRegionToPropertyMap = new string[] { 
            "UserNameLabelText",
            "UserNameTitleText", 
            "UserNameInstructionText",
        };

        private static readonly string[] _questionViewRegionToPropertyMap = new string[] { 
            "UserNameLabelText",
            "QuestionTitleText", 
            "QuestionLabelText", 
            "QuestionInstructionText",
            "AnswerLabelText", 
        };

        private static readonly string[] _successViewRegionToPropertyMap = new string[] {
            "SuccessText", 
        };
 
 
        private static readonly string[] _templateNames = new string[] {
            "UserNameTemplate", 
            "QuestionTemplate",
            "SuccessTemplate",
        };
 
        // Properties that do not apply to the control when it is templated
        // Removed from the property grid when there is a user template 
        private static readonly string[] _nonTemplateProperties = new string[] { 
            "AnswerLabelText",
            "AnswerRequiredErrorMessage", 
            "BorderPadding",
            "HelpPageIconUrl",
            "FailureTextStyle",
            "HelpPageText", 
            "HelpPageUrl",
            "HyperLinkStyle", 
            "InstructionTextStyle", 
            "LabelStyle",
            "QuestionInstructionText", 
            "QuestionLabelText",
            "QuestionTitleText",
            "SubmitButtonImageUrl",
            "SubmitButtonStyle", 
            "SubmitButtonText",
            "SubmitButtonType", 
            "SuccessText", 
            "SuccessTextStyle",
            "TextBoxStyle", 
            "TextLayout",
            "TitleTextStyle",
            "UserNameInstructionText",
            "UserNameLabelText", 
            "UserNameRequiredErrorMessage",
            "UserNameTitleText", 
            "ValidatorTextStyle", 
        };
 
        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new PasswordRecoveryDesignerActionList(this)); 
 
                return actionLists;
            } 
        }

        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.PASSWORDRECOVERY_SCHEMES, 
                        delegate(DataRow schemeData) { return new PasswordRecoveryAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            }
        }

        private ViewType CurrentView { 
            get {
                object view = DesignerState["CurrentView"]; 
                return (view == null) ? ViewType.UserName : (ViewType)view; 
            }
            set { 
                DesignerState["CurrentView"] = value;
            }
        }
 
        /// <devdoc>
        /// Returns true if the current view is templated. 
        /// </devdoc> 
        private bool Templated {
            get { 
                return (GetTemplate(_passwordRecovery) != null);
            }
        }
 
        private TemplateDefinition TemplateDefinition {
            get { 
                string templateName = _templateNames[(int)CurrentView]; 
                return new TemplateDefinition(this, templateName, _passwordRecovery, templateName, ((WebControl)ViewControl).ControlStyle);
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

        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups { 
            get {
                TemplateGroupCollection groups = base.TemplateGroups; 
                TemplateGroupCollection templateGroups = new TemplateGroupCollection(); 
                for (int i = 0; i < _templateNames.Length; i++) {
                    string templateName = _templateNames[i]; 
                    TemplateGroup templateGroup = new TemplateGroup(templateName, ((WebControl)ViewControl).ControlStyle);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, templateName, _passwordRecovery, templateName, ((WebControl)ViewControl).ControlStyle));
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
 
        private bool ConvertToTemplateChangeCallback(object context) {
            try { 
                IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
                ConvertToTemplateHelper convertToTemplateHelper = new ConvertToTemplateHelper(this, designerHost);
                ITemplate template = convertToTemplateHelper.ConvertToTemplate();
                TemplateDescriptor.SetValue(_passwordRecovery, template); 
                return true;
            } 
            catch (Exception e) { 
                Debug.Fail(e.Message);
                return false; 
            }
        }

        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.GetDesignTimeHtml"]/*' /> 
        public override string GetDesignTimeHtml() {
            string designTimeHtml; 
            try { 
                IDictionary parameters = new HybridDictionary(1);
                parameters["CurrentView"] = CurrentView; 
                ((IControlDesignerAccessor)ViewControl).SetDesignModeState(parameters);

                // Make sure the child controls are recreated
                ICompositeControlDesignerAccessor designerAccessor = (ICompositeControlDesignerAccessor)ViewControl; 
                designerAccessor.RecreateChildControls();
 
                designTimeHtml = base.GetDesignTimeHtml(); 
            } catch (Exception e) {
                designTimeHtml = GetErrorDesignTimeHtml(e); 
            }
            return designTimeHtml;
        }
 
        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.GetErrorDesignTimeHtml"]/*' />
        protected override string GetErrorDesignTimeHtml(Exception e) { 
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br />" + e.Message); 
        }
 
        private ITemplate GetTemplate(PasswordRecovery passwordRecovery) {
            ITemplate template = null;
            switch (CurrentView) {
                case ViewType.UserName: 
                    template = passwordRecovery.UserNameTemplate;
                    break; 
                case ViewType.Question: 
                    template = passwordRecovery.QuestionTemplate;
                    break; 
                case ViewType.Success:
                    template = passwordRecovery.SuccessTemplate;
                    break;
            } 
            return template;
        } 
 
        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.Initialize"]/*' />
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(PasswordRecovery));
            _passwordRecovery = (PasswordRecovery) component;
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

        /// <include file='doc\PasswordRecoveryDesigner.uex' path='docs/doc[@for="PasswordRecoveryDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        /// If the current view is templated, remove properties that do not apply when templated.
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
            try { 
                TemplateDescriptor.SetValue(_passwordRecovery, null);
                return true;
            }
            catch (Exception ex) { 
                Debug.Fail(ex.Message);
                return false; 
            } 
        }
 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) {
            bool useRegions = UseRegions(regions, GetTemplate(_passwordRecovery));
            if (useRegions) {
                EditableDesignerRegion region = new TemplatedEditableDesignerRegion(TemplateDefinition); 
                region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
                regions.Add(region); 
 
                // VSWhidbey 382801 Always enable the controls in the designer so we can drag and drop controls
                ((WebControl)ViewControl).Enabled = true; 

                IDictionary parameters = new HybridDictionary(1);
                parameters.Add("RegionEditing", true);
                ((IControlDesignerAccessor)ViewControl).SetDesignModeState(parameters); 
            }
 
            return GetDesignTimeHtml(); 
        }
 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            ITemplate template = GetTemplate(_passwordRecovery);
            if (template == null) {
                return String.Empty; 
            }
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            return ControlPersister.PersistTemplate(template, host);
        } 

        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) {
            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost));
            Debug.Assert(host != null, "IDesignerHost is null."); 

            ITemplate template = ControlParser.ParseTemplate(host, content); 
            // Region name maps to the template property 
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)[region.Name];
            using (DesignerTransaction transaction = host.CreateTransaction("SetEditableDesignerRegionContent")) { 
                descriptor.SetValue(Component, template);
                transaction.Commit();
            }
        } 

        private enum ViewType { 
            UserName = 0, 
            Question = 1,
            Success = 2 
        }

        private class PasswordRecoveryDesignerActionList : DesignerActionList {
            private PasswordRecoveryDesigner _designer; 

            public PasswordRecoveryDesignerActionList(PasswordRecoveryDesigner designer) : base(designer.Component) { 
                _designer = designer; 
            }
 
            public override bool AutoShow {
                get {
                    return true;
                } 
                set {
                } 
            } 

            [TypeConverter(typeof(PasswordRecoveryViewTypeConverter))] 
            public string View {
                get {
                    if (_designer.CurrentView == ViewType.UserName) {
                        return SR.GetString(SR.PasswordRecovery_UserNameView); 
                    }
                    else if (_designer.CurrentView == ViewType.Question) { 
                        return SR.GetString(SR.PasswordRecovery_QuestionView); 
                    }
                    else if (_designer.CurrentView == ViewType.Success) { 
                        return SR.GetString(SR.PasswordRecovery_SuccessView);
                    }
                    else {
                        Debug.Fail("Unexpected view value"); 
                    }
 
                    return String.Empty; 
                }
                set { 
                    if (String.Compare(value, SR.GetString(SR.PasswordRecovery_UserNameView), StringComparison.Ordinal) == 0) {
                        _designer.CurrentView = ViewType.UserName;
                    }
                    else if (String.Compare(value, SR.GetString(SR.PasswordRecovery_QuestionView), StringComparison.Ordinal) == 0) { 
                        _designer.CurrentView = ViewType.Question;
                    } 
                    else if (String.Compare(value, SR.GetString(SR.PasswordRecovery_SuccessView), StringComparison.Ordinal) == 0) { 
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
                if (!_designer.InTemplateMode) { 
                    if (_designer.Templated) {
                        items.Add(new DesignerActionMethodItem(this, "Reset", SR.GetString(SR.WebControls_Reset), 
                            String.Empty, SR.GetString(SR.WebControls_ResetDescriptionViews), true));
                    }
                    else {
                        items.Add(new DesignerActionMethodItem(this, "ConvertToTemplate", 
                            SR.GetString(SR.WebControls_ConvertToTemplate), String.Empty,
                            SR.GetString(SR.WebControls_ConvertToTemplateDescriptionViews), true)); 
                    } 
                }
 
                items.Add(new DesignerActionMethodItem(this, "LaunchWebAdmin", SR.GetString(SR.Login_LaunchWebAdmin),
                    String.Empty, SR.GetString(SR.Login_LaunchWebAdminDescription), true));
                return items;
            } 

            public void Reset() { 
                _designer.Reset(); 
            }
 
            private class PasswordRecoveryViewTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    string[] names = new string[3];
 
                    names[0] = SR.GetString(SR.PasswordRecovery_UserNameView);
                    names[1] = SR.GetString(SR.PasswordRecovery_QuestionView); 
                    names[2] = SR.GetString(SR.PasswordRecovery_SuccessView); 

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
                LoginDesignerUtil.GenericConvertToTemplateHelper<PasswordRecovery, PasswordRecoveryDesigner> { 
 
            // Controls that are persisted when converting to template
            private static readonly string[] _persistedControlIDs = new string[] { 
                "UserName",
                "UserNameRequired",
                "Question",
                "Answer", 
                "AnswerRequired",
                "SubmitButton", 
                "SubmitImageButton", 
                "SubmitLinkButton",
                "FailureText", 
                "HelpLink",
            };

            // Controls that are persisted even if they are not visible when the control is rendered 
            // They are not visible at design-time because the values are computed at runtime
            private static readonly string[] _persistedIfNotVisibleControlIDs = new string[] { 
                "UserName", 
                "Question",
                "FailureText" 
            };

            public ConvertToTemplateHelper(PasswordRecoveryDesigner designer, IDesignerHost designerHost) :
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
 
            protected override Style GetFailureTextStyle(PasswordRecovery control) {
                return control.FailureTextStyle;
            }
 
            protected override Control GetDefaultTemplateContents() {
                Control container = null; 
                switch (Designer.CurrentView) { 
                    case ViewType.UserName:
                        container = Designer.ViewControl.Controls[0]; 
                        break;
                    case ViewType.Question:
                        container = Designer.ViewControl.Controls[1];
                        break; 
                    case ViewType.Success:
                        container = Designer.ViewControl.Controls[2]; 
                        break; 
                }
 
                Table table = (Table)(container.Controls[0]);
                return table;
            }
 
            protected override ITemplate GetTemplate(PasswordRecovery control) {
                return Designer.GetTemplate(control); 
            } 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
