//------------------------------------------------------------------------------ 
// <copyright file="LoginStatusDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Web.UI.WebControls; 
 
    /// <include file='doc\LoginStatusDesigner.uex' path='docs/doc[@for="LoginStatusDesigner"]/*' />
    /// <devdoc> 
    /// Designer for the LoginStatus control.  Includes verb to switch whether the control renders as "login" or "logout".
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class LoginStatusDesigner : CompositeControlDesigner { 

        private bool _loggedIn; 
        private LoginStatus _loginStatus; 

        /// <include file='doc\LoginStatusDesigner.uex' path='docs/doc[@for="LoginStatusDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new LoginStatusDesignerActionList(this));
 
                return actionLists; 
            }
        } 

        protected override bool UsePreviewControl {
            get {
                return true; 
            }
        } 
 
        public override string GetDesignTimeHtml() {
            IDictionary parameters = new HybridDictionary(2); 
            parameters["LoggedIn"] = _loggedIn;
            LoginStatus loginStatus = (LoginStatus)ViewControl;
            ((IControlDesignerAccessor)loginStatus).SetDesignModeState(parameters);
 
            bool blank;
            string originalText; 
            if (_loggedIn) { 
                originalText = loginStatus.LogoutText;
                // Need to check if original text is empty OR a single space " ", since it is 
                // persisted as a single space in the designer due to a Trident bug
                blank = (originalText == null || originalText.Length == 0 || originalText == " ");
                if (blank) {
                    loginStatus.LogoutText = "[" + loginStatus.ID + "]"; 
                }
            } 
            else { 
                originalText = loginStatus.LoginText;
                blank = (originalText == null || originalText.Length == 0 || originalText == " "); 
                if (blank) {
                    loginStatus.LoginText = "[" + loginStatus.ID + "]";
                }
            } 

            return base.GetDesignTimeHtml(); 
        } 

        /// <include file='doc\LoginStatusDesigner.uex' path='docs/doc[@for="LoginStatusDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(LoginStatus));
            _loginStatus = (LoginStatus) component;
            base.Initialize(component); 
        }
 
        private class LoginStatusDesignerActionList : DesignerActionList { 
            private LoginStatusDesigner _designer;
 
            public LoginStatusDesignerActionList(LoginStatusDesigner designer) :base(designer.Component) {
                _designer = designer;
            }
 
            public override bool AutoShow {
                get { 
                    return true; 
                }
                set { 
                }
            }

            [TypeConverter(typeof(LoginStatusViewTypeConverter))] 
            public string View {
                get { 
                    if (_designer._loggedIn) { 
                        return SR.GetString(SR.LoginStatus_LoggedInView);
                    } 
                    else {
                        return SR.GetString(SR.LoginStatus_LoggedOutView);
                    }
                } 
                set {
                    if (String.Compare(value, SR.GetString(SR.LoginStatus_LoggedInView), StringComparison.Ordinal) == 0) { 
                        _designer._loggedIn = true; 
                    }
                    else if (String.Compare(value, SR.GetString(SR.LoginStatus_LoggedOutView), StringComparison.Ordinal) == 0) { 
                        _designer._loggedIn = false;
                    }
                    else {
                        Debug.Fail("Unexpected view value"); 
                    }
 
                    _designer.UpdateDesignTimeHtml(); 
                }
 
            }

            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                items.Add(new DesignerActionPropertyItem("View", SR.GetString(SR.WebControls_Views),
                     String.Empty, SR.GetString(SR.WebControls_ViewsDescription))); 
                return items; 
            }
 
            private class LoginStatusViewTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    string[] names = new string[2];
 
                    names[0] = SR.GetString(SR.LoginStatus_LoggedOutView);
                    names[1] = SR.GetString(SR.LoginStatus_LoggedInView); 
 
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
// <copyright file="LoginStatusDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Web.UI.WebControls; 
 
    /// <include file='doc\LoginStatusDesigner.uex' path='docs/doc[@for="LoginStatusDesigner"]/*' />
    /// <devdoc> 
    /// Designer for the LoginStatus control.  Includes verb to switch whether the control renders as "login" or "logout".
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class LoginStatusDesigner : CompositeControlDesigner { 

        private bool _loggedIn; 
        private LoginStatus _loginStatus; 

        /// <include file='doc\LoginStatusDesigner.uex' path='docs/doc[@for="LoginStatusDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists {
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new LoginStatusDesignerActionList(this));
 
                return actionLists; 
            }
        } 

        protected override bool UsePreviewControl {
            get {
                return true; 
            }
        } 
 
        public override string GetDesignTimeHtml() {
            IDictionary parameters = new HybridDictionary(2); 
            parameters["LoggedIn"] = _loggedIn;
            LoginStatus loginStatus = (LoginStatus)ViewControl;
            ((IControlDesignerAccessor)loginStatus).SetDesignModeState(parameters);
 
            bool blank;
            string originalText; 
            if (_loggedIn) { 
                originalText = loginStatus.LogoutText;
                // Need to check if original text is empty OR a single space " ", since it is 
                // persisted as a single space in the designer due to a Trident bug
                blank = (originalText == null || originalText.Length == 0 || originalText == " ");
                if (blank) {
                    loginStatus.LogoutText = "[" + loginStatus.ID + "]"; 
                }
            } 
            else { 
                originalText = loginStatus.LoginText;
                blank = (originalText == null || originalText.Length == 0 || originalText == " "); 
                if (blank) {
                    loginStatus.LoginText = "[" + loginStatus.ID + "]";
                }
            } 

            return base.GetDesignTimeHtml(); 
        } 

        /// <include file='doc\LoginStatusDesigner.uex' path='docs/doc[@for="LoginStatusDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(LoginStatus));
            _loginStatus = (LoginStatus) component;
            base.Initialize(component); 
        }
 
        private class LoginStatusDesignerActionList : DesignerActionList { 
            private LoginStatusDesigner _designer;
 
            public LoginStatusDesignerActionList(LoginStatusDesigner designer) :base(designer.Component) {
                _designer = designer;
            }
 
            public override bool AutoShow {
                get { 
                    return true; 
                }
                set { 
                }
            }

            [TypeConverter(typeof(LoginStatusViewTypeConverter))] 
            public string View {
                get { 
                    if (_designer._loggedIn) { 
                        return SR.GetString(SR.LoginStatus_LoggedInView);
                    } 
                    else {
                        return SR.GetString(SR.LoginStatus_LoggedOutView);
                    }
                } 
                set {
                    if (String.Compare(value, SR.GetString(SR.LoginStatus_LoggedInView), StringComparison.Ordinal) == 0) { 
                        _designer._loggedIn = true; 
                    }
                    else if (String.Compare(value, SR.GetString(SR.LoginStatus_LoggedOutView), StringComparison.Ordinal) == 0) { 
                        _designer._loggedIn = false;
                    }
                    else {
                        Debug.Fail("Unexpected view value"); 
                    }
 
                    _designer.UpdateDesignTimeHtml(); 
                }
 
            }

            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                items.Add(new DesignerActionPropertyItem("View", SR.GetString(SR.WebControls_Views),
                     String.Empty, SR.GetString(SR.WebControls_ViewsDescription))); 
                return items; 
            }
 
            private class LoginStatusViewTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    string[] names = new string[2];
 
                    names[0] = SR.GetString(SR.LoginStatus_LoggedOutView);
                    names[1] = SR.GetString(SR.LoginStatus_LoggedInView); 
 
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
