//------------------------------------------------------------------------------ 
// <copyright file="LoginViewDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections.Specialized;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Collections;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Text; 
    using System.Web.UI.WebControls; 
    using System.Drawing;
    using System.Drawing.Imaging; 
    using System.Drawing.Design;
    using System.Web.UI.Design.Util;

    /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner"]/*' /> 
    /// <devdoc>
    /// Designer for the LoginView class.  Adds property to property grid to select which view renders in the designer. 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class LoginViewDesigner : ControlDesigner { 
        private const string _designtimeHTML =
        @"<table cellspacing=0 cellpadding=0 border=0 style=""display:inline-block"">
                <tr>
                    <td nowrap align=center valign=middle style=""color:{0}; background-color:{1}; "">{2}</td> 
                </tr>
                <tr> 
                    <td style=""vertical-align:top;"" {3}='0'>{4}</td> 
                </tr>
          </table>"; 

        private LoginView _loginView;
        private TemplateGroupCollection _templateGroups;
        private const int _anonymousTemplateIndex = 0; 
        private const int _loggedInTemplateIndex = 1;
        // index of the first RoleGroup template 
        private const int _roleGroupStartingIndex = 2; 
        private const string _anonymousTemplateName = "AnonymousTemplate";
        private const string _loggedInTemplateName = "LoggedInTemplate"; 
        private const string _contentTemplateName = "ContentTemplate";
        private const string _roleGroupsPropertyName = "RoleGroups";

        private static readonly string[] _templateNames = new string[] { 
            "AnonymousTemplate",
            "LoggedInTemplate" 
        }; 

        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new LoginViewDesignerActionList(this));
 
                return actionLists; 
            }
        } 

        private object CurrentObject {
            get {
                if ((CurrentView == _anonymousTemplateIndex)) { 
                    return Component;
                } 
                else if ((CurrentView == _loggedInTemplateIndex)) { 
                    return Component;
                } 
                else {
                    RoleGroup selectedRoleGroup = _loginView.RoleGroups[CurrentView - _roleGroupStartingIndex];
                    return selectedRoleGroup;
                } 
            }
        } 
 
        private ITemplate CurrentTemplate {
            get { 
                if ((CurrentView == _anonymousTemplateIndex)) {
                    return _loginView.AnonymousTemplate;
                }
                else if ((CurrentView == _loggedInTemplateIndex)) { 
                    return _loginView.LoggedInTemplate;
                } 
                else { 
                    RoleGroup selectedRoleGroup = _loginView.RoleGroups[CurrentView - _roleGroupStartingIndex];
                    return selectedRoleGroup.ContentTemplate; 
                }
            }
        }
 
        private PropertyDescriptor CurrentTemplateDescriptor {
            get { 
                if ((CurrentView == _anonymousTemplateIndex)) { 
                    return TypeDescriptor.GetProperties(Component)[_anonymousTemplateName];
                } 
                else if ((CurrentView == _loggedInTemplateIndex)) {
                    return TypeDescriptor.GetProperties(Component)[_loggedInTemplateName];
                }
                else { 
                    RoleGroup selectedRoleGroup = _loginView.RoleGroups[CurrentView - _roleGroupStartingIndex];
                    return TypeDescriptor.GetProperties(selectedRoleGroup)[_contentTemplateName]; 
                } 
            }
        } 

        // index of the view currently visible in the designer
        private int CurrentView {
            get { 
                object view = DesignerState["CurrentView"];
                int index = (view == null) ? _anonymousTemplateIndex : (int)view; 
                // If the CurrentView ever gets too big (i.e. deleted rolegroups) revert to a default 
                return (index > _roleGroupStartingIndex + _loginView.RoleGroups.Count - 1) ? _anonymousTemplateIndex : index;
            } 
            set {
                DesignerState["CurrentView"] = value;
            }
        } 

        private ITemplate CurrentViewControlTemplate { 
            get { 
                if ((CurrentView == _anonymousTemplateIndex)) {
                    return ((LoginView)ViewControl).AnonymousTemplate; 
                }
                else if ((CurrentView == _loggedInTemplateIndex)) {
                    return ((LoginView)ViewControl).LoggedInTemplate;
                } 
                else {
                    RoleGroup selectedRoleGroup = ((LoginView)ViewControl).RoleGroups[CurrentView - _roleGroupStartingIndex]; 
                    return selectedRoleGroup.ContentTemplate; 
                }
            } 
        }

        private TemplateDefinition TemplateDefinition {
            get { 
                int view = CurrentView;
                if ((view == _anonymousTemplateIndex)) { 
                    return new TemplateDefinition(this, _anonymousTemplateName, _loginView, _anonymousTemplateName); 
                }
                else if ((CurrentView == _loggedInTemplateIndex)) { 
                    return new TemplateDefinition(this, _loggedInTemplateName, _loginView, _loggedInTemplateName);
                }
                else {
                    return new TemplateDefinition(this, _contentTemplateName, _loginView.RoleGroups[view - _roleGroupStartingIndex], _contentTemplateName); 
                }
            } 
        } 

        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.TemplateGroups"]/*' /> 
        public override TemplateGroupCollection TemplateGroups {
            get {
                TemplateGroupCollection groups = base.TemplateGroups;
 
                if (_templateGroups == null) {
                    _templateGroups = new TemplateGroupCollection(); 
 
                    TemplateGroup templateGroup = new TemplateGroup(_anonymousTemplateName);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, _anonymousTemplateName, _loginView, _anonymousTemplateName)); 
                    _templateGroups.Add(templateGroup);

                    templateGroup = new TemplateGroup(_loggedInTemplateName);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, _loggedInTemplateName, _loginView, _loggedInTemplateName)); 
                    _templateGroups.Add(templateGroup);
 
                    RoleGroupCollection roleGroups = _loginView.RoleGroups; 
                    for (int i = 0; i < roleGroups.Count; i++) {
                        string caption = CreateRoleGroupCaption(i, roleGroups); 

                        templateGroup = new TemplateGroup(caption);
                        templateGroup.AddTemplateDefinition(new TemplateDefinition(this, caption, _loginView.RoleGroups[i], _contentTemplateName));
 
                        _templateGroups.Add(templateGroup);
                    } 
                } 

                groups.AddRange(_templateGroups); 

                return groups;
            }
        } 

        protected override bool UsePreviewControl { 
            get { 
                return true;
            } 
        }

        private EditableDesignerRegion BuildRegion() {
            EditableDesignerRegion region = new LoginViewDesignerRegion(this, CurrentObject, CurrentTemplate, CurrentTemplateDescriptor, TemplateDefinition); 
            region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
            return region; 
        } 

        /// <devdoc> 
        /// Returns the caption for a RoleGroup, i.e. "RoleGroup[1] - admin, user".
        /// Used as a template editing verb, and as text if a template is empty.
        /// </devdoc>
        private static string CreateRoleGroupCaption(int roleGroupIndex, RoleGroupCollection roleGroups) { 
            string roleGroupText = roleGroups[roleGroupIndex].ToString();
            string caption = "RoleGroup[" + roleGroupIndex.ToString(CultureInfo.InvariantCulture) + "]"; 
            if ((roleGroupText != null) && (roleGroupText.Length > 0)) { 
                caption += " - " + roleGroupText;
            } 
            return caption;
        }

        private void EditRoleGroups() { 
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)[_roleGroupsPropertyName];
            InvokeTransactedChange(Component, new TransactedChangeCallback(EditRoleGroupsChangeCallback), descriptor, 
                                   SR.GetString(SR.LoginView_EditRoleGroupsTransactionDescription), descriptor); 

            int numViews = _loginView.RoleGroups.Count+_roleGroupStartingIndex; 
            if (CurrentView >= numViews) {
                CurrentView = numViews - 1;
            }
            if (CurrentView < 0) { 
                CurrentView = 0;
            } 
 
            // Make sure the template groups are recreated
            _templateGroups = null; 
        }

        private bool EditRoleGroupsChangeCallback(object context) {
            PropertyDescriptor descriptor = (PropertyDescriptor)context; 

            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null); 

            UITypeEditor editor = (UITypeEditor)descriptor.GetEditor(typeof(UITypeEditor)); 
            object newValue = editor.EditValue(new TypeDescriptorContext(designerHost, descriptor, Component),
                                               new WindowsFormsEditorServiceHelper(this), descriptor.GetValue(Component));

            return (newValue != null); 
        }
 
        public override string GetDesignTimeHtml() { 

            string designTimeHtml = String.Empty; 
            if (CurrentViewControlTemplate != null) {
                LoginView loginView = (LoginView)ViewControl;

                IDictionary param = new HybridDictionary(1); 
                param["TemplateIndex"] = CurrentView;
                ((IControlDesignerAccessor)loginView).SetDesignModeState(param); 
 
                loginView.DataBind();
 
                designTimeHtml = base.GetDesignTimeHtml();
            }

            return designTimeHtml; 
        }
 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            string content = String.Empty;
            bool useRegions = UseRegions(regions, CurrentTemplate, CurrentViewControlTemplate); 
            if (useRegions) {
                regions.Add(BuildRegion());
            }
            else { 
                content = GetDesignTimeHtml();
            } 
 
            StringBuilder sb = new StringBuilder(1024);
            sb.Append(String.Format(CultureInfo.InvariantCulture, 
                                    _designtimeHTML,
                                    ColorTranslator.ToHtml(SystemColors.ControlText),
                                    ColorTranslator.ToHtml(SystemColors.Control),
                                    _loginView.ID, 
                                    DesignerRegion.DesignerRegionAttributeName,
                                    content)); 
            return sb.ToString(); 
        }
 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            if (region is LoginViewDesignerRegion) {
                ITemplate template = ((LoginViewDesignerRegion)region).Template;
                if (template != null) { 
                    IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost));
                    return ControlPersister.PersistTemplate(template, host); 
                } 
            }
            return base.GetEditableDesignerRegionContent(region); 
        }

        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.GetEmptyDesignTimeHtml"]/*' />
        protected override string GetEmptyDesignTimeHtml() { 
            string templateEmpty = String.Empty;
            switch (CurrentView) { 
                case _anonymousTemplateIndex: 
                    templateEmpty = SR.GetString(SR.LoginView_AnonymousTemplateEmpty);
                    break; 
                case _loggedInTemplateIndex:
                    templateEmpty = SR.GetString(SR.LoginView_LoggedInTemplateEmpty);
                    break;
                default: 
                    int roleGroupIndex = CurrentView - _roleGroupStartingIndex;
                    string roleGroupTemplateCaption = CreateRoleGroupCaption(roleGroupIndex, _loginView.RoleGroups); 
                    templateEmpty = SR.GetString(SR.LoginView_RoleGroupTemplateEmpty, roleGroupTemplateCaption); 
                    break;
            } 
            return CreatePlaceHolderDesignTimeHtml(templateEmpty + "<br>" + SR.GetString(SR.LoginView_NoTemplateInst));
        }

        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.GetErrorDesignTimeHtml"]/*' /> 
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.LoginView_ErrorRendering) + "<br />" + e.Message); 
        } 

        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(LoginView));
            _loginView = (LoginView)component;
            base.Initialize(component); 
        }
 
        private void LaunchWebAdmin() { 
            if (Component.Site != null) {
                IDesignerHost designerHost = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost)); 
                if (designerHost != null) {
                    IWebAdministrationService webadmin = (IWebAdministrationService)designerHost.GetService(typeof(IWebAdministrationService));
                    if (webadmin != null) {
                        webadmin.Start(null); 
                    }
                } 
            } 
        }
 
        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.OnComponentChanged"]/*' />
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs e) {
            if ((e.Member == null) || (e.Member.Name.Equals("RoleGroups"))) {
                // If we removed a RoleGroup, the current view may be greater than the 
                // number of views.
                int numViews = _loginView.RoleGroups.Count + _roleGroupStartingIndex; 
                if (CurrentView >= numViews) { 
                    CurrentView = numViews-1;
                } 

                _templateGroups = null;
            }
 
            base.OnComponentChanged(sender, e);
        } 
 
        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.PreFilterProperties"]/*' />
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);
            if (InTemplateMode) {
                PropertyDescriptor property = (PropertyDescriptor)properties["RoleGroups"];
                properties["RoleGroups"] = 
                    TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
            } 
        } 

        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            LoginViewDesignerRegion lvRegion = region as LoginViewDesignerRegion;
            if (lvRegion == null) return;

            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost)); 
            Debug.Assert(host != null, "IDesignerHost is null.");
 
            ITemplate template = ControlParser.ParseTemplate(host, content); 
            using (DesignerTransaction transaction = host.CreateTransaction("SetEditableDesignerRegionContent")) {
                lvRegion.PropertyDescriptor.SetValue(lvRegion.Object, template); 
                transaction.Commit();
            }
            lvRegion.Template = template;
        } 

        private class LoginViewDesignerRegion : TemplatedEditableDesignerRegion { 
            ITemplate _template; 
            public ITemplate Template {
                get { return _template; } 
                set { _template = value; }
            }

            object _object; 
            public object Object { get { return _object; } }
 
            PropertyDescriptor _prop; 
            public PropertyDescriptor PropertyDescriptor { get { return _prop; } }
 
            public LoginViewDesignerRegion(ControlDesigner owner, object obj, ITemplate template, PropertyDescriptor descriptor, TemplateDefinition definition) :  base(definition) {
                _template = template;
                _object = obj;
                _prop = descriptor; 
                EnsureSize = true;
            } 
        } 

        private class LoginViewDesignerActionList : DesignerActionList { 
            private LoginViewDesigner _designer;

            public LoginViewDesignerActionList(LoginViewDesigner designer) : base(designer.Component) {
                _designer = designer; 
            }
 
            public override bool AutoShow { 
                get {
                    return true; 
                }
                set {
                }
            } 

            [TypeConverter(typeof(LoginViewViewTypeConverter))] 
            public string View { 
                get {
                    int currentView = _designer.CurrentView; 
                    if (currentView-2 >= _designer._loginView.RoleGroups.Count) {
                        currentView = _designer._loginView.RoleGroups.Count+1;
                        _designer.CurrentView = currentView;
                    } 
                    if (currentView == 0) {
                        return LoginViewDesigner._anonymousTemplateName; 
                    } 
                    else if (currentView == 1) {
                        return LoginViewDesigner._loggedInTemplateName; 
                    }
                    else {
                        return LoginViewDesigner.CreateRoleGroupCaption(currentView - 2, _designer._loginView.RoleGroups);
                    } 
                }
                set { 
                    if (String.Compare(value, LoginViewDesigner._anonymousTemplateName, StringComparison.Ordinal) == 0) { 
                        _designer.CurrentView = 0;
                    } 
                    else if (String.Compare(value, LoginViewDesigner._loggedInTemplateName, StringComparison.Ordinal) == 0) {
                        _designer.CurrentView = 1;
                    }
                    else { 
                        RoleGroupCollection roleGroups = _designer._loginView.RoleGroups;
                        for (int i = 0; i < roleGroups.Count; i++) { 
                            string roleGroupName = LoginViewDesigner.CreateRoleGroupCaption(i, roleGroups); 
                            if (String.Compare(value, roleGroupName, StringComparison.Ordinal) == 0) {
                                _designer.CurrentView = i + 2; 
                            }
                        }
                    }
 
                    _designer.UpdateDesignTimeHtml();
                } 
            } 

            public void EditRoleGroups() { 
                _designer.EditRoleGroups();
            }

            public void LaunchWebAdmin() { 
                _designer.LaunchWebAdmin();
            } 
 
            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                items.Add(new DesignerActionMethodItem(this, "EditRoleGroups", SR.GetString(SR.LoginView_EditRoleGroups),
                    String.Empty, SR.GetString(SR.LoginView_EditRoleGroupsDescription), true));
                items.Add(new DesignerActionPropertyItem("View", SR.GetString(SR.WebControls_Views),
                    String.Empty, SR.GetString(SR.WebControls_ViewsDescription))); 
                items.Add(new DesignerActionMethodItem(this, "LaunchWebAdmin", SR.GetString(SR.Login_LaunchWebAdmin),
                    String.Empty, SR.GetString(SR.Login_LaunchWebAdminDescription), true)); 
                return items; 
            }
 
            private class LoginViewViewTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    LoginViewDesignerActionList list = (LoginViewDesignerActionList)context.Instance;
                    LoginView loginView = list._designer._loginView; 

                    RoleGroupCollection roleGroups = loginView.RoleGroups; 
 
                    string[] names = new string[roleGroups.Count + 2];
 
                    names[0] = LoginViewDesigner._anonymousTemplateName;
                    names[1] = LoginViewDesigner._loggedInTemplateName;
                    for (int i = 0; i < roleGroups.Count; i++) {
                        names[i + 2] = LoginViewDesigner.CreateRoleGroupCaption(i, roleGroups); 
                    }
 
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
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="LoginViewDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections.Specialized;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Collections;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Text; 
    using System.Web.UI.WebControls; 
    using System.Drawing;
    using System.Drawing.Imaging; 
    using System.Drawing.Design;
    using System.Web.UI.Design.Util;

    /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner"]/*' /> 
    /// <devdoc>
    /// Designer for the LoginView class.  Adds property to property grid to select which view renders in the designer. 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class LoginViewDesigner : ControlDesigner { 
        private const string _designtimeHTML =
        @"<table cellspacing=0 cellpadding=0 border=0 style=""display:inline-block"">
                <tr>
                    <td nowrap align=center valign=middle style=""color:{0}; background-color:{1}; "">{2}</td> 
                </tr>
                <tr> 
                    <td style=""vertical-align:top;"" {3}='0'>{4}</td> 
                </tr>
          </table>"; 

        private LoginView _loginView;
        private TemplateGroupCollection _templateGroups;
        private const int _anonymousTemplateIndex = 0; 
        private const int _loggedInTemplateIndex = 1;
        // index of the first RoleGroup template 
        private const int _roleGroupStartingIndex = 2; 
        private const string _anonymousTemplateName = "AnonymousTemplate";
        private const string _loggedInTemplateName = "LoggedInTemplate"; 
        private const string _contentTemplateName = "ContentTemplate";
        private const string _roleGroupsPropertyName = "RoleGroups";

        private static readonly string[] _templateNames = new string[] { 
            "AnonymousTemplate",
            "LoggedInTemplate" 
        }; 

        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new LoginViewDesignerActionList(this));
 
                return actionLists; 
            }
        } 

        private object CurrentObject {
            get {
                if ((CurrentView == _anonymousTemplateIndex)) { 
                    return Component;
                } 
                else if ((CurrentView == _loggedInTemplateIndex)) { 
                    return Component;
                } 
                else {
                    RoleGroup selectedRoleGroup = _loginView.RoleGroups[CurrentView - _roleGroupStartingIndex];
                    return selectedRoleGroup;
                } 
            }
        } 
 
        private ITemplate CurrentTemplate {
            get { 
                if ((CurrentView == _anonymousTemplateIndex)) {
                    return _loginView.AnonymousTemplate;
                }
                else if ((CurrentView == _loggedInTemplateIndex)) { 
                    return _loginView.LoggedInTemplate;
                } 
                else { 
                    RoleGroup selectedRoleGroup = _loginView.RoleGroups[CurrentView - _roleGroupStartingIndex];
                    return selectedRoleGroup.ContentTemplate; 
                }
            }
        }
 
        private PropertyDescriptor CurrentTemplateDescriptor {
            get { 
                if ((CurrentView == _anonymousTemplateIndex)) { 
                    return TypeDescriptor.GetProperties(Component)[_anonymousTemplateName];
                } 
                else if ((CurrentView == _loggedInTemplateIndex)) {
                    return TypeDescriptor.GetProperties(Component)[_loggedInTemplateName];
                }
                else { 
                    RoleGroup selectedRoleGroup = _loginView.RoleGroups[CurrentView - _roleGroupStartingIndex];
                    return TypeDescriptor.GetProperties(selectedRoleGroup)[_contentTemplateName]; 
                } 
            }
        } 

        // index of the view currently visible in the designer
        private int CurrentView {
            get { 
                object view = DesignerState["CurrentView"];
                int index = (view == null) ? _anonymousTemplateIndex : (int)view; 
                // If the CurrentView ever gets too big (i.e. deleted rolegroups) revert to a default 
                return (index > _roleGroupStartingIndex + _loginView.RoleGroups.Count - 1) ? _anonymousTemplateIndex : index;
            } 
            set {
                DesignerState["CurrentView"] = value;
            }
        } 

        private ITemplate CurrentViewControlTemplate { 
            get { 
                if ((CurrentView == _anonymousTemplateIndex)) {
                    return ((LoginView)ViewControl).AnonymousTemplate; 
                }
                else if ((CurrentView == _loggedInTemplateIndex)) {
                    return ((LoginView)ViewControl).LoggedInTemplate;
                } 
                else {
                    RoleGroup selectedRoleGroup = ((LoginView)ViewControl).RoleGroups[CurrentView - _roleGroupStartingIndex]; 
                    return selectedRoleGroup.ContentTemplate; 
                }
            } 
        }

        private TemplateDefinition TemplateDefinition {
            get { 
                int view = CurrentView;
                if ((view == _anonymousTemplateIndex)) { 
                    return new TemplateDefinition(this, _anonymousTemplateName, _loginView, _anonymousTemplateName); 
                }
                else if ((CurrentView == _loggedInTemplateIndex)) { 
                    return new TemplateDefinition(this, _loggedInTemplateName, _loginView, _loggedInTemplateName);
                }
                else {
                    return new TemplateDefinition(this, _contentTemplateName, _loginView.RoleGroups[view - _roleGroupStartingIndex], _contentTemplateName); 
                }
            } 
        } 

        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.TemplateGroups"]/*' /> 
        public override TemplateGroupCollection TemplateGroups {
            get {
                TemplateGroupCollection groups = base.TemplateGroups;
 
                if (_templateGroups == null) {
                    _templateGroups = new TemplateGroupCollection(); 
 
                    TemplateGroup templateGroup = new TemplateGroup(_anonymousTemplateName);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, _anonymousTemplateName, _loginView, _anonymousTemplateName)); 
                    _templateGroups.Add(templateGroup);

                    templateGroup = new TemplateGroup(_loggedInTemplateName);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, _loggedInTemplateName, _loginView, _loggedInTemplateName)); 
                    _templateGroups.Add(templateGroup);
 
                    RoleGroupCollection roleGroups = _loginView.RoleGroups; 
                    for (int i = 0; i < roleGroups.Count; i++) {
                        string caption = CreateRoleGroupCaption(i, roleGroups); 

                        templateGroup = new TemplateGroup(caption);
                        templateGroup.AddTemplateDefinition(new TemplateDefinition(this, caption, _loginView.RoleGroups[i], _contentTemplateName));
 
                        _templateGroups.Add(templateGroup);
                    } 
                } 

                groups.AddRange(_templateGroups); 

                return groups;
            }
        } 

        protected override bool UsePreviewControl { 
            get { 
                return true;
            } 
        }

        private EditableDesignerRegion BuildRegion() {
            EditableDesignerRegion region = new LoginViewDesignerRegion(this, CurrentObject, CurrentTemplate, CurrentTemplateDescriptor, TemplateDefinition); 
            region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
            return region; 
        } 

        /// <devdoc> 
        /// Returns the caption for a RoleGroup, i.e. "RoleGroup[1] - admin, user".
        /// Used as a template editing verb, and as text if a template is empty.
        /// </devdoc>
        private static string CreateRoleGroupCaption(int roleGroupIndex, RoleGroupCollection roleGroups) { 
            string roleGroupText = roleGroups[roleGroupIndex].ToString();
            string caption = "RoleGroup[" + roleGroupIndex.ToString(CultureInfo.InvariantCulture) + "]"; 
            if ((roleGroupText != null) && (roleGroupText.Length > 0)) { 
                caption += " - " + roleGroupText;
            } 
            return caption;
        }

        private void EditRoleGroups() { 
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)[_roleGroupsPropertyName];
            InvokeTransactedChange(Component, new TransactedChangeCallback(EditRoleGroupsChangeCallback), descriptor, 
                                   SR.GetString(SR.LoginView_EditRoleGroupsTransactionDescription), descriptor); 

            int numViews = _loginView.RoleGroups.Count+_roleGroupStartingIndex; 
            if (CurrentView >= numViews) {
                CurrentView = numViews - 1;
            }
            if (CurrentView < 0) { 
                CurrentView = 0;
            } 
 
            // Make sure the template groups are recreated
            _templateGroups = null; 
        }

        private bool EditRoleGroupsChangeCallback(object context) {
            PropertyDescriptor descriptor = (PropertyDescriptor)context; 

            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null); 

            UITypeEditor editor = (UITypeEditor)descriptor.GetEditor(typeof(UITypeEditor)); 
            object newValue = editor.EditValue(new TypeDescriptorContext(designerHost, descriptor, Component),
                                               new WindowsFormsEditorServiceHelper(this), descriptor.GetValue(Component));

            return (newValue != null); 
        }
 
        public override string GetDesignTimeHtml() { 

            string designTimeHtml = String.Empty; 
            if (CurrentViewControlTemplate != null) {
                LoginView loginView = (LoginView)ViewControl;

                IDictionary param = new HybridDictionary(1); 
                param["TemplateIndex"] = CurrentView;
                ((IControlDesignerAccessor)loginView).SetDesignModeState(param); 
 
                loginView.DataBind();
 
                designTimeHtml = base.GetDesignTimeHtml();
            }

            return designTimeHtml; 
        }
 
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            string content = String.Empty;
            bool useRegions = UseRegions(regions, CurrentTemplate, CurrentViewControlTemplate); 
            if (useRegions) {
                regions.Add(BuildRegion());
            }
            else { 
                content = GetDesignTimeHtml();
            } 
 
            StringBuilder sb = new StringBuilder(1024);
            sb.Append(String.Format(CultureInfo.InvariantCulture, 
                                    _designtimeHTML,
                                    ColorTranslator.ToHtml(SystemColors.ControlText),
                                    ColorTranslator.ToHtml(SystemColors.Control),
                                    _loginView.ID, 
                                    DesignerRegion.DesignerRegionAttributeName,
                                    content)); 
            return sb.ToString(); 
        }
 
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) {
            if (region is LoginViewDesignerRegion) {
                ITemplate template = ((LoginViewDesignerRegion)region).Template;
                if (template != null) { 
                    IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost));
                    return ControlPersister.PersistTemplate(template, host); 
                } 
            }
            return base.GetEditableDesignerRegionContent(region); 
        }

        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.GetEmptyDesignTimeHtml"]/*' />
        protected override string GetEmptyDesignTimeHtml() { 
            string templateEmpty = String.Empty;
            switch (CurrentView) { 
                case _anonymousTemplateIndex: 
                    templateEmpty = SR.GetString(SR.LoginView_AnonymousTemplateEmpty);
                    break; 
                case _loggedInTemplateIndex:
                    templateEmpty = SR.GetString(SR.LoginView_LoggedInTemplateEmpty);
                    break;
                default: 
                    int roleGroupIndex = CurrentView - _roleGroupStartingIndex;
                    string roleGroupTemplateCaption = CreateRoleGroupCaption(roleGroupIndex, _loginView.RoleGroups); 
                    templateEmpty = SR.GetString(SR.LoginView_RoleGroupTemplateEmpty, roleGroupTemplateCaption); 
                    break;
            } 
            return CreatePlaceHolderDesignTimeHtml(templateEmpty + "<br>" + SR.GetString(SR.LoginView_NoTemplateInst));
        }

        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.GetErrorDesignTimeHtml"]/*' /> 
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.LoginView_ErrorRendering) + "<br />" + e.Message); 
        } 

        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(LoginView));
            _loginView = (LoginView)component;
            base.Initialize(component); 
        }
 
        private void LaunchWebAdmin() { 
            if (Component.Site != null) {
                IDesignerHost designerHost = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost)); 
                if (designerHost != null) {
                    IWebAdministrationService webadmin = (IWebAdministrationService)designerHost.GetService(typeof(IWebAdministrationService));
                    if (webadmin != null) {
                        webadmin.Start(null); 
                    }
                } 
            } 
        }
 
        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.OnComponentChanged"]/*' />
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs e) {
            if ((e.Member == null) || (e.Member.Name.Equals("RoleGroups"))) {
                // If we removed a RoleGroup, the current view may be greater than the 
                // number of views.
                int numViews = _loginView.RoleGroups.Count + _roleGroupStartingIndex; 
                if (CurrentView >= numViews) { 
                    CurrentView = numViews-1;
                } 

                _templateGroups = null;
            }
 
            base.OnComponentChanged(sender, e);
        } 
 
        /// <include file='doc\LoginViewDesigner.uex' path='docs/doc[@for="LoginViewDesigner.PreFilterProperties"]/*' />
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);
            if (InTemplateMode) {
                PropertyDescriptor property = (PropertyDescriptor)properties["RoleGroups"];
                properties["RoleGroups"] = 
                    TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
            } 
        } 

        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            LoginViewDesignerRegion lvRegion = region as LoginViewDesignerRegion;
            if (lvRegion == null) return;

            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost)); 
            Debug.Assert(host != null, "IDesignerHost is null.");
 
            ITemplate template = ControlParser.ParseTemplate(host, content); 
            using (DesignerTransaction transaction = host.CreateTransaction("SetEditableDesignerRegionContent")) {
                lvRegion.PropertyDescriptor.SetValue(lvRegion.Object, template); 
                transaction.Commit();
            }
            lvRegion.Template = template;
        } 

        private class LoginViewDesignerRegion : TemplatedEditableDesignerRegion { 
            ITemplate _template; 
            public ITemplate Template {
                get { return _template; } 
                set { _template = value; }
            }

            object _object; 
            public object Object { get { return _object; } }
 
            PropertyDescriptor _prop; 
            public PropertyDescriptor PropertyDescriptor { get { return _prop; } }
 
            public LoginViewDesignerRegion(ControlDesigner owner, object obj, ITemplate template, PropertyDescriptor descriptor, TemplateDefinition definition) :  base(definition) {
                _template = template;
                _object = obj;
                _prop = descriptor; 
                EnsureSize = true;
            } 
        } 

        private class LoginViewDesignerActionList : DesignerActionList { 
            private LoginViewDesigner _designer;

            public LoginViewDesignerActionList(LoginViewDesigner designer) : base(designer.Component) {
                _designer = designer; 
            }
 
            public override bool AutoShow { 
                get {
                    return true; 
                }
                set {
                }
            } 

            [TypeConverter(typeof(LoginViewViewTypeConverter))] 
            public string View { 
                get {
                    int currentView = _designer.CurrentView; 
                    if (currentView-2 >= _designer._loginView.RoleGroups.Count) {
                        currentView = _designer._loginView.RoleGroups.Count+1;
                        _designer.CurrentView = currentView;
                    } 
                    if (currentView == 0) {
                        return LoginViewDesigner._anonymousTemplateName; 
                    } 
                    else if (currentView == 1) {
                        return LoginViewDesigner._loggedInTemplateName; 
                    }
                    else {
                        return LoginViewDesigner.CreateRoleGroupCaption(currentView - 2, _designer._loginView.RoleGroups);
                    } 
                }
                set { 
                    if (String.Compare(value, LoginViewDesigner._anonymousTemplateName, StringComparison.Ordinal) == 0) { 
                        _designer.CurrentView = 0;
                    } 
                    else if (String.Compare(value, LoginViewDesigner._loggedInTemplateName, StringComparison.Ordinal) == 0) {
                        _designer.CurrentView = 1;
                    }
                    else { 
                        RoleGroupCollection roleGroups = _designer._loginView.RoleGroups;
                        for (int i = 0; i < roleGroups.Count; i++) { 
                            string roleGroupName = LoginViewDesigner.CreateRoleGroupCaption(i, roleGroups); 
                            if (String.Compare(value, roleGroupName, StringComparison.Ordinal) == 0) {
                                _designer.CurrentView = i + 2; 
                            }
                        }
                    }
 
                    _designer.UpdateDesignTimeHtml();
                } 
            } 

            public void EditRoleGroups() { 
                _designer.EditRoleGroups();
            }

            public void LaunchWebAdmin() { 
                _designer.LaunchWebAdmin();
            } 
 
            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                items.Add(new DesignerActionMethodItem(this, "EditRoleGroups", SR.GetString(SR.LoginView_EditRoleGroups),
                    String.Empty, SR.GetString(SR.LoginView_EditRoleGroupsDescription), true));
                items.Add(new DesignerActionPropertyItem("View", SR.GetString(SR.WebControls_Views),
                    String.Empty, SR.GetString(SR.WebControls_ViewsDescription))); 
                items.Add(new DesignerActionMethodItem(this, "LaunchWebAdmin", SR.GetString(SR.Login_LaunchWebAdmin),
                    String.Empty, SR.GetString(SR.Login_LaunchWebAdminDescription), true)); 
                return items; 
            }
 
            private class LoginViewViewTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    LoginViewDesignerActionList list = (LoginViewDesignerActionList)context.Instance;
                    LoginView loginView = list._designer._loginView; 

                    RoleGroupCollection roleGroups = loginView.RoleGroups; 
 
                    string[] names = new string[roleGroups.Count + 2];
 
                    names[0] = LoginViewDesigner._anonymousTemplateName;
                    names[1] = LoginViewDesigner._loggedInTemplateName;
                    for (int i = 0; i < roleGroups.Count; i++) {
                        names[i + 2] = LoginViewDesigner.CreateRoleGroupCaption(i, roleGroups); 
                    }
 
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
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
