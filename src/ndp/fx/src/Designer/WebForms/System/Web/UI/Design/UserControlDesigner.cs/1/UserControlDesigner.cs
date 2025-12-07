//------------------------------------------------------------------------------ 
// <copyright file="UserControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Configuration; 
    using System.Design; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions; 
    using System.Web.Configuration;
    using System.Web.UI; 
    using System.Web.UI.HtmlControls; 
    using System.Web.UI.WebControls;
 
    /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="UserControlDesigner"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides design-time support for the usercontrols (controls declared in 
    ///       .ascx files).
    ///    </para> 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class UserControlDesigner : ControlDesigner { 
        private const string UserControlCacheKey = "__aspnetUserControlCache";

        // This is used when the IDictionaryService does not exist
        private static IDictionary _antiRecursionDictionary = new HybridDictionary(); 

        private bool _userControlFound; 
 
        /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="UserControlDesigner.UserControlDesigner"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Initializes a new instance of the UserControlDesigner class.
        ///    </para>
        /// </devdoc> 
        public UserControlDesigner() {
        } 
 
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new UserControlDesignerActionList(this));
 
                return actionLists;
            } 
        } 

        /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="UserControlDesigner.AllowResize"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether all user controls are resizeable.
        ///    </para> 
        /// </devdoc>
        public override bool AllowResize { 
            get { 
                return false;
            } 
        }

        /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="UserControlDesigner.ShouldCodeSerializeInternal"]/*' />
        internal override bool ShouldCodeSerializeInternal { 
            get {
                if (Component.GetType() == typeof(UserControl)) { 
                    // should always return false - we don't want to code spit out 
                    // a variable of type UserControl
                    return false; 
                }
                return base.ShouldCodeSerializeInternal;
            }
            set { 
                base.ShouldCodeSerializeInternal = value;
            } 
        } 

        private string GenerateUserControlCacheKey(string userControlPath, IThemeResolutionService themeService) { 
            string userControlCacheKey = userControlPath;
            if (themeService != null) {
                ThemeProvider themeProvider = themeService.GetStylesheetThemeProvider();
                if ((themeProvider != null) && !String.IsNullOrEmpty(themeProvider.ThemeName)) { 
                    userControlCacheKey += "|" + themeProvider.ThemeName;
                } 
            } 

            return userControlCacheKey; 
        }

        private string GenerateUserControlHashCode(string contents, IThemeResolutionService themeService) {
            string newHashCode = contents.GetHashCode().ToString(CultureInfo.InvariantCulture); 
            if (themeService != null) {
                ThemeProvider themeProvider = themeService.GetStylesheetThemeProvider(); 
                if (themeProvider != null) { 
                    newHashCode += "|" + themeProvider.ContentHashCode.ToString(CultureInfo.InvariantCulture);
                } 
            }

            return newHashCode;
        } 

        private const string _dummyProtocolAndServer = "file://foo"; 
        private string MakeAppRelativePath(string path) { 
            if (String.IsNullOrEmpty(path) || path.StartsWith("~", StringComparison.Ordinal)) {
                return path; 
            }

            string prefix = Path.GetDirectoryName(RootDesigner.DocumentUrl);
            if (String.IsNullOrEmpty(prefix)) { 
                prefix = "~";
            } 
 
            prefix = prefix.Replace('\\', '/');
            prefix = prefix.Replace("~", _dummyProtocolAndServer); 
            path = path.Replace('\\', '/');
            //
            Uri docUri = new Uri(prefix + "/" + path);
            return docUri.ToString().Replace(_dummyProtocolAndServer, "~"); 
        }
 
 
        /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="UserControlDesigner.GetDesignTimeHtml"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the HTML to be used for the design time representation of the control runtime.
        ///    </para>
        /// </devdoc> 
        public override string GetDesignTimeHtml() {
            // Build up design-time HTML for the user control 
            if (Component.Site != null) { 
                IWebApplication webApp = (IWebApplication)Component.Site.GetService(typeof(IWebApplication));
                IDesignerHost host = (IDesignerHost) Component.Site.GetService(typeof(IDesignerHost)); 
                if ((webApp != null) && (host != null)) {
                    // Try to get the path of the file
                    if (RootDesigner.ReferenceManager != null) {
                        // Split up the tag prefix and tag name 
                        IUserControlDesignerAccessor userControl = (IUserControlDesignerAccessor)Component;
                        string[] tagNameParts = userControl.TagName.Split(':'); 
 
                        string userControlPath = RootDesigner.ReferenceManager.GetUserControlPath(tagNameParts[0], tagNameParts[1]);
                        userControlPath = MakeAppRelativePath(userControlPath); 

                        // Also create a cache key that includes the current theme name
                        IThemeResolutionService themeService = (IThemeResolutionService)Component.Site.GetService(typeof(IThemeResolutionService));
                        string userControlCacheKey = GenerateUserControlCacheKey(userControlPath, themeService); 

                        if (!String.IsNullOrEmpty(userControlPath)) { 
                            string hashCode = null; 
                            string designTimeHtml = String.Empty;
                            bool regenerate = false; 

                            // Default the cache to the anti-recursion dictionary
                            // so we have some way to stop circular refs inside user controls
                            IDictionary userControlCache = _antiRecursionDictionary; 

                            // Try to use the IDictionaryService as the design-time html cache 
                            IDictionaryService dictionaryService = (IDictionaryService)webApp.GetService(typeof(IDictionaryService)); 
                            if (dictionaryService != null) {
                                userControlCache = (IDictionary)dictionaryService.GetValue(UserControlCacheKey); 
                                if (userControlCache == null) {
                                    userControlCache = new HybridDictionary();
                                    dictionaryService.SetValue(UserControlCacheKey, userControlCache);
                                } 

                                Pair pair = (Pair)userControlCache[userControlCacheKey]; 
                                if (pair != null) { 
                                    hashCode = (string)pair.First;
                                    designTimeHtml = (string)pair.Second; 

                                    // VSWhidbey 305364 We have to regenerate if we are using resources from venus
                                    regenerate = designTimeHtml.Contains("mvwres:");
                                } 
                                else {
                                    regenerate = true; 
                                } 
                            }
 
                            // Read the contents of the file
                            IDocumentProjectItem userControlItem = webApp.GetProjectItemFromUrl(userControlPath) as IDocumentProjectItem;
                            if (userControlItem != null) {
                                _userControlFound = true; 

                                StreamReader reader = new StreamReader(userControlItem.GetContents()); 
                                string contents = reader.ReadToEnd(); 

                                string newHashCode = null; 
                                // Check if the hashcode still matches up
                                if (!regenerate) {
                                    newHashCode = GenerateUserControlHashCode(contents, themeService);
                                    regenerate = !String.Equals(newHashCode, hashCode, StringComparison.OrdinalIgnoreCase) 
                                        || contents.Contains(".ascx");
                                } 
 
                                if (regenerate) {
                                    // Detect cycles and return a suitable error 
                                    if (_antiRecursionDictionary.Contains(userControlCacheKey)) {
                                        return CreateErrorDesignTimeHtml(SR.GetString(SR.UserControlDesigner_CyclicError));
                                    }
                                    else { 
                                        _antiRecursionDictionary[userControlCacheKey] = CreateErrorDesignTimeHtml(SR.GetString(SR.UserControlDesigner_CyclicError));
                                    } 
                                    designTimeHtml = String.Empty; 

                                    // Put an empty entry into the cache so we don't 
                                    // get into a recursive user control loop
                                    Pair newPair = new Pair();
                                    if (newHashCode == null) {
                                        newHashCode = GenerateUserControlHashCode(contents, themeService); 
                                    }
                                    newPair.First = newHashCode; 
                                    newPair.Second = designTimeHtml; 
                                    userControlCache[userControlCacheKey] = newPair;
 
                                    // Need to create a dummy page and add the usercontrol to the page
                                    // This is similar to what we do in WebFormDesigner
                                    UserControl componentUserControl = (UserControl)Component;
                                    Page userControlPage = new Page(); 

                                    try { 
                                        userControlPage.Controls.Add(componentUserControl); 

                                        IDesignerHost childHost = new UserControlDesignerHost(host, userControlPage, userControlPath); 
                                        if (!String.IsNullOrEmpty(contents)) {
                                            List<Triplet> userControlRegisterEntries = new List<Triplet>();
                                            // Parse and add all non-literal control to the designer host
                                            Control[] childControls = ControlSerializer.DeserializeControlsInternal(contents, childHost, userControlRegisterEntries); 
                                            foreach (Control childControl in childControls) {
                                                if (!(childControl is LiteralControl) && 
                                                    !(childControl is DesignerDataBoundLiteralControl) && 
                                                    !(childControl is DataBoundLiteralControl)) {
                                                    if (String.IsNullOrEmpty(childControl.ID)) { 
                                                        throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.UserControlDesigner_MissingID), childControl.GetType().Name));
                                                    }

                                                    childHost.Container.Add(childControl); 
                                                }
 
                                                // Need to add child controls to the usercontrol 
                                                componentUserControl.Controls.Add(childControl);
                                            } 

                                            // Add all the registration entries to our internal lists so that our
                                            // dummy Reference Manager can use them.
                                            foreach (Triplet entry in userControlRegisterEntries) { 
                                                string tagPrefix = (string)entry.First;
                                                Pair userControlRegisterEntryData = (Pair)entry.Second; 
                                                Pair tagNamespaceRegisterEntryData = (Pair)entry.Third; 
                                                if (userControlRegisterEntryData != null) {
                                                    string tagName = (string)userControlRegisterEntryData.First; 
                                                    string src = (string)userControlRegisterEntryData.Second;
                                                    ((UserControlDesignerHost)childHost).RegisterUserControl(tagPrefix, tagName, src);
                                                    Debug.Assert(tagNamespaceRegisterEntryData == null, "Registration entry should have either a user control entry or a tag namespace entry.");
                                                } 
                                                else {
                                                    if (tagNamespaceRegisterEntryData != null) { 
                                                        string tagNamespace = (string)tagNamespaceRegisterEntryData.First; 
                                                        string assemblyName = (string)tagNamespaceRegisterEntryData.Second;
                                                        ((UserControlDesignerHost)childHost).RegisterTagNamespace(tagPrefix, tagNamespace, assemblyName); 
                                                    }
                                                    else {
                                                        Debug.Fail("Registration entry should have either a user control entry or a tag namespace entry.");
                                                    } 
                                                }
                                            } 
 
                                            // Now get design-time html for each
                                            StringBuilder designTimeHtmlBuilder = new StringBuilder(); 
                                            foreach (Control childControl in childControls) {
                                                string newHtml = String.Empty;
                                                if (childControl is LiteralControl) {
                                                    designTimeHtmlBuilder.Append(((LiteralControl)childControl).Text); 
                                                }
                                                else if (childControl is DesignerDataBoundLiteralControl) { 
                                                    designTimeHtmlBuilder.Append(((DesignerDataBoundLiteralControl)childControl).Text); 
                                                }
                                                else if (childControl is DataBoundLiteralControl) { 
                                                    designTimeHtmlBuilder.Append(((DataBoundLiteralControl)childControl).Text);
                                                }
                                                else if (childControl is HtmlControl) {
                                                    // Directly render out html controls 
                                                    StringWriter swriter = new StringWriter(CultureInfo.CurrentCulture);
                                                    DesignTimeHtmlTextWriter writer = new DesignTimeHtmlTextWriter(swriter); 
                                                    childControl.RenderControl(writer); 
                                                    designTimeHtmlBuilder.Append(swriter.GetStringBuilder().ToString());
                                                } 
                                                else {
                                                    // Otherwise use their control designers
                                                    ControlDesigner designer = (ControlDesigner)childHost.GetDesigner(childControl);
                                                    ViewRendering viewRendering = designer.GetViewRendering(); 
                                                    designTimeHtmlBuilder.Append(viewRendering.Content);
                                                } 
                                            } 
                                            designTimeHtml = designTimeHtmlBuilder.ToString();
                                        } 

                                        newPair.Second = designTimeHtml;
                                    }
                                    finally { 
                                        // Clear out the entry we just added in the anti-recursion dictionary
                                        // so we don't leave garbage around 
                                        _antiRecursionDictionary.Remove(userControlCacheKey); 

                                        // Remove the child contol from usercontrol 
                                        componentUserControl.Controls.Clear();
                                        // Remove the usercontrol from the page.
                                        userControlPage.Controls.Remove(componentUserControl);
                                    } 
                                }
                            } 
                            else { 
                                designTimeHtml = CreateErrorDesignTimeHtml(SR.GetString(SR.UserControlDesigner_NotFound, userControlPath));
                            } 

                            if (designTimeHtml.Trim().Length > 0) {
                                return designTimeHtml;
                            } 
                        }
                    } 
                } 
            }
 
            // If we didn't generate real design-time html, just show the grey block
            return CreatePlaceHolderDesignTimeHtml();
        }
 
        private void EditUserControl() {
            IWebApplication webApp = (IWebApplication)Component.Site.GetService(typeof(IWebApplication)); 
            if (webApp != null) { 
                // Split up the tag prefix and tag anem
                IUserControlDesignerAccessor userControl = (IUserControlDesignerAccessor)Component; 
                string[] tagNameParts = userControl.TagName.Split(':');
                string userControlPath = RootDesigner.ReferenceManager.GetUserControlPath(tagNameParts[0], tagNameParts[1]);
                if (!String.IsNullOrEmpty(userControlPath)) {
                    userControlPath = MakeAppRelativePath(userControlPath); 
                    IDocumentProjectItem userControlItem = webApp.GetProjectItemFromUrl(userControlPath) as IDocumentProjectItem;
                    if (userControlItem != null) { 
                        userControlItem.Open(); 
                    }
                } 
            }
        }

        private void Refresh() { 
            UpdateDesignTimeHtml();
        } 
 
        /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="PageletDesigner.GetPersistenceContent"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the persistable inner HTML.
        ///    </para>
        /// </devdoc> 
        internal override string GetPersistInnerHtmlInternal() {
            if (Component.GetType() == typeof(UserControl)) { 
                // always return null, so that the contents of the user control get round-tripped 
                // as is, since we're not in a position to do the actual persistence
                return null; 
            }
            return base.GetPersistInnerHtmlInternal();
        }
 
        private class UserControlDesignerActionList : DesignerActionList {
            private UserControlDesigner _parent; 
 
            public UserControlDesignerActionList(UserControlDesigner parent) : base(parent.Component) {
                _parent = parent; 
            }

            public override bool AutoShow {
                get { 
                    return true;
                } 
                set { 
                }
            } 

            public void EditUserControl() {
                _parent.EditUserControl();
            } 

            public void Refresh() { 
                _parent.Refresh(); 
            }
 
            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                if (_parent._userControlFound) {
                    items.Add(new DesignerActionMethodItem(this, "EditUserControl", SR.GetString(SR.UserControlDesigner_EditUserControl), String.Empty, String.Empty, true)); 
                }
                items.Add(new DesignerActionMethodItem(this, "Refresh", SR.GetString(SR.UserControlDesigner_Refresh), String.Empty, String.Empty, true)); 
 
                return items;
            } 
        }


        // Represents a tag namespace register entry, e.g. 
        // <%@ Register TagPrefix="..." Namespace="..." Assembly="..." %>
        private sealed class TagNamespaceRegisterEntry { 
            public string TagPrefix; 
            public string TagNamespace;
            public string AssemblyName; 

            public TagNamespaceRegisterEntry(string tagPrefix, string tagNamespace, string assemblyName) {
                TagPrefix = tagPrefix;
                TagNamespace = tagNamespace; 
                AssemblyName = assemblyName;
            } 
        } 

        private sealed class UserControlDesignerHost : IContainer, IDesignerHost, IDisposable, IUrlResolutionService { 

            // member variables
            private Hashtable _componentTable;        // Component collection
            private Hashtable _designerTable;         // Designer Collection 
            private IDesignerHost _host;              // Webform Designer host
            private bool _disposed = false;           // flag to indicate the object is disposed 
            private IComponent _rootComponent; 
            private int _nameCounter;
            private string _userControlPath; 

            // Lists of <%@ Register %> directives in the UserControl
            private IDictionary<string, string> _userControlRegisterEntries;
            private IList<TagNamespaceRegisterEntry> _tagNamespaceRegisterEntries; 

            /// <summary> 
            /// Initializes an instance of the UserControlDesignerHost class 
            /// </summary>
            public UserControlDesignerHost(IDesignerHost host, IComponent rootComponent, string userControlPath) { 
                _host = host;
                _componentTable = new Hashtable();
                _designerTable = new Hashtable();
                _rootComponent = rootComponent; 
                _userControlPath = userControlPath;
 
                _rootComponent.Site = new DummySite(_rootComponent, this); 
            }
 
            /// <summary>
            /// Destructor method
            /// </summary>
            ~UserControlDesignerHost() { 
                Dispose(false);
            } 
 
            // Property implementations
            private Hashtable ComponentTable { 
                get {
                    return _componentTable;
                }
            } 
            private Hashtable DesignerTable {
                get { 
                    return _designerTable; 
                }
            } 


            /// <summary>
            /// This method clears the components and the designers from 
            /// the respective hash table if the HasClearableComponents
            /// flag is set to true 
            /// </summary> 
            public void ClearComponents() {
                for (int i = 0; i < DesignerTable.Count; i++) { 
                    if (DesignerTable[i] != null) {
                        IDesigner designer = (IDesigner)DesignerTable[i];
                        try {
                            designer.Dispose(); 
                        }
                        catch { 
                            Debug.Fail("Designer " + designer.GetType().Name + " threw an exception during Dispose."); 
                        }
                    } 
                }
                DesignerTable.Clear();

                for (int i = 0; i < ComponentTable.Count; i++) { 
                    if (ComponentTable[i] != null) {
                        IComponent component = (IComponent)ComponentTable[i]; 
                        ISite site = component.Site; 
                        try {
                            component.Dispose(); 
                        }
                        catch {
                            Debug.Fail("Component " + site.Name + " threw during dispose.  Bad component!!");
                        } 
                        if (component.Site != null) {
                            Debug.Fail("Component " + site.Name + " did not remove itself from its container"); 
                            ((IContainer)this).Remove(component); 
                        }
                    } 
                }
                ComponentTable.Clear();
            }
 
            /// <summary>
            /// This method will be called to clean up any managed objects we had created 
            /// </summary> 
            public void Dispose() {
                Dispose(true); 
                // This object will be cleaned up by the Dispose method.
                // Therefore, you should call GC.SupressFinalize to
                // take this object off the finalization queue
                // and prevent finalization code for this object 
                // from executing a second time.
                GC.SuppressFinalize(this); 
            } 

            // <Summary> 
            // Dispose(bool disposing) executes in two distinct scenarios.
            // If disposing equals true, the method has been called directly
            // or indirectly by a user's code. Managed and unmanaged resources
            // can be disposed. 
            // If disposing equals false, the method has been called by the
            // runtime from inside the finalizer and you should not reference 
            // other objects. Only unmanaged resources can be disposed. 
            // </Summary>
            public void Dispose(bool disposing) { 
                // Check to see if Dispose has already been called.
                if(!this._disposed) {
                    // If disposing equals true, dispose all managed
                    // and unmanaged resources. 
                    if(disposing) {
                        ClearComponents(); 
                        _host = null; 
                        _componentTable = null;
                        _designerTable = null; 

                    }
                    // No unmanaged object to clean up
                } 
                _disposed = true;
            } 
 
            /// <summary>
            /// This method creates a collection of IComponents from the existing 
            /// list of components in the component has table and returns it to the
            /// caller.
            /// </summary>
            /// <returns> 
            /// returns the collection of IComponents
            /// </returns> 
            private IComponent[] GetComponents() { 
                int componentCount = ComponentTable.Count;
                IComponent[] components = new IComponent[componentCount]; 

                if (componentCount != 0) {
                    int i = 0;
                    foreach (IComponent component in ComponentTable.Values) { 
                        components[i++] = component;
                    } 
                } 
                return components;
            } 

            public void RegisterTagNamespace(string tagPrefix, string tagNamespace, string assemblyName) {
                if (_tagNamespaceRegisterEntries == null) {
                    _tagNamespaceRegisterEntries = new List<TagNamespaceRegisterEntry>(); 
                }
                _tagNamespaceRegisterEntries.Add(new TagNamespaceRegisterEntry(tagPrefix, tagNamespace, assemblyName)); 
            } 

            public void RegisterUserControl(string tagPrefix, string tagName, string src) { 
                if (_userControlRegisterEntries == null) {
                    _userControlRegisterEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                _userControlRegisterEntries[tagPrefix + ":" + tagName] = src; 
            }
 
            #region Implementation of IContainer 
            /// <summary>
            /// This method creates a collection of IComponents and returns it to 
            /// the caller.
            /// </summary>
            ComponentCollection IContainer.Components {
                get { 
                    return new ComponentCollection(GetComponents());
                } 
            } 

            /// <summary> 
            /// Adds a given component to the container.
            /// </summary>
            void IContainer.Add(IComponent component) {
                ((IContainer)this).Add(component, null); 
            }
 
            /// <summary> 
            /// Adds a given component to the container. The component is added to
            /// the component collection, also a designer for the component is created 
            /// and added to the designer collection.
            /// </summary>
            void IContainer.Add(IComponent component, string name) {
 
                // Check if the component is not null
                if (component == null) { 
                    throw new ArgumentNullException("component"); 
                }
 
                if (component.Site == null) {
                    component.Site = new DummySite(component, this);
                    if (component is Control) {
                        component.Site.Name = ((Control)component).ID; 
                    }
                    else { 
                        component.Site.Name = "Temp" + (_nameCounter++); 
                    }
                } 

                // make sure we have a name if one was not provided
                if (name == null) {
                    name = component.Site.Name; 
                }
 
                // make sure the name isn't already in use 
                if (ComponentTable[name] != null) {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.UserControlDesignerHost_ComponentAlreadyExists), name)); 
                }

                ComponentTable[name] = component;
                IDesigner designer = TypeDescriptor.CreateDesigner(component, typeof(IDesigner)); 
                designer.Initialize(component);
                DesignerTable[component] = designer; 
 
                if (component is Control) {
                    ((Control)component).Page = (Page)_rootComponent; 
                }
            }

 
            /// <summary>
            /// Removes a given component from the component collection. This method also 
            /// retrieves the corresponding designer for the component and removes it from 
            /// the designer collection.
            /// </summary> 
            void IContainer.Remove(IComponent component) {
                if (component == null) {
                    throw new ArgumentNullException("component");
                } 

                if (component.Site == null) { 
                    return; 
                }
 
                string name = component.Site.Name;
                if ((name != null) && (ComponentTable[name] == component)) {

                    // dispose and remove the designer 
                    if (DesignerTable != null) {
                        IDesigner designer = (IDesigner)DesignerTable[component]; 
                        if (designer != null) { 
                            DesignerTable.Remove(component);
                            designer.Dispose(); 
                        }
                    }

                    // remove the component 
                    ComponentTable.Remove(name);
                    component.Dispose(); 
 
                    // finally disassociate with the site
                    component.Site = null; 
                }
            }
            #endregion
 
            #region Implementation of IDisposable
            void IDisposable.Dispose() { 
                Dispose(); 
            }
            #endregion 

            #region Implementation of IServiceProvider
            /// <summary>
            ///     Override of Container's GetService method.  Other than that of the IDesignerhost 
            ///     and the IContainer service, this just delegates to the webform designer hosts
            ///     service provider 
            /// </summary> 
            /// <param name="service">
            ///     The type of service to retrieve 
            /// </param>
            /// <returns>
            ///     An instance of the service.
            /// </returns> 
            object IServiceProvider.GetService(Type serviceType) {
                if ((serviceType == typeof(IDesignerHost)) || 
                    (serviceType == typeof(IContainer)) || 
                    (serviceType == typeof(IUrlResolutionService))) {
                    return this; 
                }
                else {
                    return _host.GetService(serviceType);
                } 
            }
            #endregion 
 
            #region Implementation of IServiceContainer
            /// <summary> 
            /// Dummy implementation of IServiceContainer. These method are not intended to be used in any
            /// shape or form.
            /// </summary>
            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote) { 
            }
 
            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback) { 
            }
 
            void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote) {
            }

            void IServiceContainer.AddService(Type serviceType, object serviceInstance) { 
            }
 
            void IServiceContainer.RemoveService(Type serviceType, bool promote) { 
            }
 
            void IServiceContainer.RemoveService(Type serviceType) {
            }
            #endregion
 

            #region Implementation of IDesignerHost 
            /// <summary> 
            /// This is the implementation of IDesigner host. Except for a few methods most of the calls
            /// are forwarded to the webforms designer host or not implemented. The notable implementations 
            /// are: Container, Destroy component, and GetDesigner
            /// </summary>
            IContainer IDesignerHost.Container {
                get { 
                    return ((IContainer)this);
                } 
            } 

            bool IDesignerHost.InTransaction { 
                get {
                    return _host.InTransaction;
                }
            } 

            bool IDesignerHost.Loading { 
                get { 
                    return _host.Loading;
                } 
            }

            string IDesignerHost.TransactionDescription {
                get { 
                    return _host.TransactionDescription;
                } 
            } 

            IComponent IDesignerHost.RootComponent { 
                get {
                    return _rootComponent;
                }
            } 

            string IDesignerHost.RootComponentClassName { 
                get { 
                    return _rootComponent.GetType().Name;
                } 
            }

            event EventHandler IDesignerHost.Activated {
                add { 
                }
                remove { 
                } 
            }
 
            event EventHandler IDesignerHost.Deactivated {
                add {
                }
                remove { 
                }
            } 
 
            event EventHandler IDesignerHost.LoadComplete {
                add { 
                    _host.LoadComplete += value;
                }
                remove {
                    _host.LoadComplete -= value; 
                }
            } 
 
            event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosed {
                add { 
                }
                remove {
                }
            } 

            event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosing { 
                add { 
                }
                remove { 
                }
            }

            event EventHandler IDesignerHost.TransactionOpened { 
                add {
                } 
                remove { 
                }
            } 

            event EventHandler IDesignerHost.TransactionOpening {
                add {
                } 
                remove {
                } 
            } 

            void IDesignerHost.Activate() { 
            }

            IComponent IDesignerHost.CreateComponent(Type componentType) {
                return null; 
            }
 
            IComponent IDesignerHost.CreateComponent(Type componentType, string name) { 
                return null;
            } 

            DesignerTransaction IDesignerHost.CreateTransaction() {
                return (_host.CreateTransaction());
            } 

            DesignerTransaction IDesignerHost.CreateTransaction(string description) { 
                return (_host.CreateTransaction(description)); 
            }
 
            void IDesignerHost.DestroyComponent(IComponent component) {
                ((IContainer)this).Remove(component);
            }
 
            Type IDesignerHost.GetType(string typeName) {
                return _host.GetType(typeName); 
            } 

            IDesigner IDesignerHost.GetDesigner(IComponent component) { 
                IDesigner designer = null;
                if (component == _host.RootComponent) {
                    designer = _host.GetDesigner(component);
                } 
                else if (component == _rootComponent) {
                    designer = new DummyRootDesigner((WebFormsRootDesigner)_host.GetDesigner(_host.RootComponent), _userControlRegisterEntries, _tagNamespaceRegisterEntries, _userControlPath); 
                } 
                else {
                    designer = (IDesigner)DesignerTable[component]; 
                }
                return designer;
            }
 
            private const string dummyProtocolAndServer = "file://foo";
            string IUrlResolutionService.ResolveClientUrl(string relativeUrl) { 
                if (relativeUrl == null) { 
                    throw new ArgumentNullException("relativeUrl");
                } 

                if (IsRooted(relativeUrl) || relativeUrl.Contains("mvwres:")) {
                    return relativeUrl;
                } 

                IUrlResolutionService baseResolutionService = (IUrlResolutionService)_host.GetService(typeof(IUrlResolutionService)); 
                if (baseResolutionService != null) { 
                    if (IsAppRelativePath(relativeUrl)) {
                        relativeUrl = baseResolutionService.ResolveClientUrl(relativeUrl); 
                    }
                    else {
                        string documentUrl = _userControlPath;
                        // If the the specified url is a relative path, make it app-relative based on the user control's path 
                        if ((documentUrl != null) && (documentUrl.Length != 0)) {
                            // If the user control path is app-relative make the url app-relative 
                            // and use the normal resolver to get the correct url 
                            if (IsAppRelativePath(documentUrl)) {
                                documentUrl = documentUrl.Replace("~", dummyProtocolAndServer); 
                                //
                                Uri docUri = new Uri(documentUrl);
                                string[] segments = docUri.Segments;
                                StringBuilder appRelativeUrlBuilder = new StringBuilder("~"); 
                                for (int i = 0; i < segments.Length - 1; i++) {
                                    appRelativeUrlBuilder.Append(segments[i]); 
                                } 

                                relativeUrl = baseResolutionService.ResolveClientUrl(appRelativeUrlBuilder.ToString() + relativeUrl); 
                            }
                            // If the document url is rooted or doc-relative, just append it together
                            else {
                                string fileName = Path.GetFileName(documentUrl); 
                                int index = documentUrl.LastIndexOf(fileName, StringComparison.Ordinal);
                                relativeUrl = Path.Combine(documentUrl.Substring(0, index), relativeUrl); 
                            } 
                        }
                    } 
                }

                return relativeUrl;
            } 
            #endregion
 
            #region Copied from UrlPath.cs 
            private const char appRelativeCharacter = '~';
 
            private static bool IsRooted(String basepath) {
                return(basepath == null || basepath.Length == 0 || basepath[0] == '/' || basepath[0] == '\\');
            }
 
            private static bool IsAppRelativePath(string path) {
                return (path.Length >= 2 && path[0] == appRelativeCharacter && (path[1] == '/' || path[1] == '\\')); 
            } 
            #endregion
        } 

        private sealed class DummyRootDesigner : WebFormsRootDesigner {
            internal WebFormsRootDesigner _rootDesigner;
            private IDictionary<string, string> _userControlRegisterEntries; 
            private IList<TagNamespaceRegisterEntry> _tagNamespaceRegisterEntries;
            private string _documentUrl; 
 
            public DummyRootDesigner(WebFormsRootDesigner rootDesigner, IDictionary<string, string> userControlRegisterEntries, IList<TagNamespaceRegisterEntry> tagNamespaceRegisterEntries, string documentUrl) {
                _rootDesigner = rootDesigner; 
                _userControlRegisterEntries = userControlRegisterEntries;
                _tagNamespaceRegisterEntries = tagNamespaceRegisterEntries;
                _documentUrl = documentUrl;
            } 

            public override string DocumentUrl { 
                get { 
                    return _documentUrl;
                } 
            }

            public override bool IsLoading {
                get { 
                    return _rootDesigner.IsLoading;
                } 
            } 

            public override bool IsDesignerViewLocked { 
                get {
                    // This root designer always effectively has its view "locked" since
                    // the page developer cannot edit any of the contents, and also
                    // GetDesignTimeHtml is only called once on each control designer. 
                    return true;
                } 
            } 

            public override WebFormsReferenceManager ReferenceManager { 
                get {
                    return new DummyWebFormsReferenceManager(this, _rootDesigner.ReferenceManager, _userControlRegisterEntries, _tagNamespaceRegisterEntries);
                }
            } 

            internal IWebApplication WebApplication { 
                get { 
                    if (_rootDesigner != null) {
                        return (IWebApplication)(_rootDesigner.GetService(typeof(IWebApplication))); 
                    }
                    return null;
                }
            } 

            public override void AddClientScriptToDocument(ClientScriptItem scriptItem) { 
                throw new NotSupportedException(); 
            }
 
            public override string AddControlToDocument(Control newControl, Control referenceControl, ControlLocation location) {
                throw new NotSupportedException();
            }
 
            public override ClientScriptItemCollection GetClientScriptsInDocument() {
                throw new NotSupportedException(); 
            } 

            /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.GetControlViewAndTag"]/*' /> 
            /// <devdoc>
            /// </devdoc>
            protected internal override void GetControlViewAndTag(Control control, out IControlDesignerView view, out IControlDesignerTag tag) {
                view = null; 
                tag = null;
            } 
 
            public override void RemoveClientScriptFromDocument(string clientScriptId) {
                throw new NotSupportedException(); 
            }

            public override void RemoveControlFromDocument(Control control) {
                throw new NotSupportedException(); 
            }
 
 
            private sealed class DummyWebFormsReferenceManager : WebFormsReferenceManager {
                private DummyRootDesigner _owner; 
                private WebFormsReferenceManager _baseReferenceManager;
                private Collection<string> _registerDirectives;
                private IDictionary<string, string> _baseUserControlRegisterEntries;
                private IList<TagNamespaceRegisterEntry> _tagNamespaceRegisterEntries; 

                public DummyWebFormsReferenceManager(DummyRootDesigner owner, 
                    WebFormsReferenceManager baseReferenceManager, 
                    IDictionary<string, string> baseUserControlRegisterEntries,
                    IList<TagNamespaceRegisterEntry> tagNamespaceRegisterEntries) { 

                    _owner = owner;
                    _baseReferenceManager = baseReferenceManager;
                    _baseUserControlRegisterEntries = baseUserControlRegisterEntries; 
                    _tagNamespaceRegisterEntries = tagNamespaceRegisterEntries;
                } 
 
                // This code is borrowed from venus\mvw\WebForms\RegisterDirectiveManager.cs
                private bool GetNamespaceAndAssemblyFromType(Type objectType, out string ns, out string asmName) { 
                    if (objectType != null) {
                        Assembly assembly = objectType.Module.Assembly;

                        if (assembly.GlobalAssemblyCache) { 
                            asmName = assembly.FullName;
                        } 
                        else { 
                            asmName = assembly.GetName().Name;
                        } 

                        ns = objectType.Namespace;
                        if (ns == null) {
                            ns = string.Empty; 
                        }
 
                        // Strange case VSWhidbey:329962 
                        // This is a work-around and could be removed if VSWhidbey:372063 gets fixed
                        ns = ns.TrimEnd('.'); 

                        if (ns != null && asmName != null && asmName.Length > 0) {
                            return true;
                        } 
                    }
 
                    ns = null; 
                    asmName = null;
 
                    return false;
                }

                public override Type GetType(string tagPrefix, string tagName) { 
                    return _baseReferenceManager.GetType(tagPrefix, tagName);
                } 
 
                // This code is borrowed from venus\mvw\WebForms\RegisterDirectiveManager.cs
                public override String GetTagPrefix(Type objectType) { 
                    // First we scan through our own list of register directives that we found in the UserControl
                    string tagNamespace;
                    string assembly;
                    if (GetNamespaceAndAssemblyFromType(objectType, out tagNamespace, out assembly)) { 
                        string tagPrefix = null;
                        string assemblylessTagPrefix = null; 
 
                        if (tagNamespace != null && assembly != null) {
                            foreach (TagNamespaceRegisterEntry entry in _tagNamespaceRegisterEntries) { 
                                if (String.Equals(tagNamespace, entry.TagNamespace, StringComparison.OrdinalIgnoreCase)) {
                                    string registerDirectiveAssembly = entry.AssemblyName;
                                    if (!String.IsNullOrEmpty(registerDirectiveAssembly)) {
                                        if (String.Equals(assembly, registerDirectiveAssembly, StringComparison.OrdinalIgnoreCase)) { 
                                            tagPrefix = entry.TagPrefix;
                                            break; 
                                        } 
                                    }
                                    else { 
                                        if (assemblylessTagPrefix == null) {
                                            assemblylessTagPrefix = entry.TagPrefix;
                                        }
                                    } 
                                }
                            } 
 
                            if (tagPrefix == null) {
                                if (assemblylessTagPrefix != null) { 
                                    tagPrefix = assemblylessTagPrefix;
                                }
                                else {
                                    tagPrefix = string.Empty; 
                                }
                            } 
 
                            return tagPrefix;
                        } 
                    }

                    // If we didn't find a mapping, just fall back to the real reference manager's implementation
                    return _baseReferenceManager.GetTagPrefix(objectType); 
                }
 
                public override String RegisterTagPrefix(Type objectType) { 
                    throw new NotSupportedException();
                } 

                private static bool IsRooted(String basepath) {
                    return (basepath == null || basepath.Length == 0 ||
                            basepath[0] == '/' || basepath[0] == '\\' || 
                            Path.IsPathRooted(basepath) ||
                            basepath.IndexOf(Path.VolumeSeparatorChar) >= 0); 
                } 

                private static bool IsAppRelativePath(string path) { 
                    return (path.Length >= 2 && path[0] == '~' && (path[1] == '/' || path[1] == '\\'));
                }

                private static string ResolveFileUrl(string baseURL, string relativeFileUrl) { 
                    if (!IsRooted(relativeFileUrl)) {
                        if (!IsAppRelativePath(relativeFileUrl)) { 
                            // trim off any file name on baseURL 
                            string baseURLFileName = Path.GetFileName(baseURL);
                            int index = baseURL.LastIndexOf(baseURLFileName, StringComparison.Ordinal); 
                            string baseURLWithoutFileName = baseURL.Substring(0, index);
                            relativeFileUrl = Path.Combine(baseURLWithoutFileName, relativeFileUrl);
                        }
                    } 
                    return relativeFileUrl;
                } 
 
                public override ICollection GetRegisterDirectives() {
                    if (_registerDirectives == null) { 
                        try {
                            _registerDirectives = new Collection<string>();
                            IWebApplication webApp = _owner.WebApplication;
                            if (webApp != null) { 
                                Configuration config = webApp.OpenWebConfiguration(true /*readonly*/);
                                if (config != null) { 
                                    PagesSection section = (PagesSection)config.GetSection("system.web/pages"); 
                                    if (section != null) {
                                        string configFilePath = config.FilePath; 
                                        IProjectItem rootProjectItem = webApp.RootProjectItem;
                                        string rootPhysPath = rootProjectItem.PhysicalPath;
                                        string configFileAppPath = "~/" + configFilePath.Substring(rootPhysPath.Length, configFilePath.Length - rootPhysPath.Length);
 
                                        foreach (TagPrefixInfo tagPrefix in section.Controls) {
                                            Dictionary<string, string> tagPrefixStrings = new Dictionary<string, string>(); 
 
                                            tagPrefix.Source = ResolveFileUrl(configFileAppPath, tagPrefix.Source);
 
                                            // Copied from RegisterDirectiveManager
                                            ElementInformation elemInfo = tagPrefix.ElementInformation;
                                            foreach(PropertyInformation propInfo in elemInfo.Properties) {
                                                if (propInfo.Type == typeof(string)) { 
                                                    tagPrefixStrings[propInfo.Name] =
                                                        (propInfo.ValueOrigin != PropertyValueOrigin.Default) ? 
                                                        (string) propInfo.Value : null; 
                                                }
                                            } 
                                            // End copy
                                            _registerDirectives.Add(GenerateRegisterDirective(
                                                tagPrefixStrings["tagPrefix"],
                                                tagPrefixStrings["tagName"], 
                                                tagPrefixStrings["namespace"],
                                                tagPrefixStrings["assembly"], 
                                                tagPrefixStrings["src"])); 
                                        }
                                    } 
                                }
                            }
                        }
                        catch (Exception ex) { 
                            Debug.Fail("Failure fetching register directives from config: \r\n" + ex.ToString());
                        } 
                        if (_baseUserControlRegisterEntries != null) { 
                            foreach (KeyValuePair<string, string> entry in _baseUserControlRegisterEntries) {
                                string registerDirective = GenerateRegisterDirective(entry.Key, entry.Value); 
                                if (!_registerDirectives.Contains(registerDirective)) {
                                    _registerDirectives.Add(registerDirective);
                                }
                            } 
                        }
                        if (_tagNamespaceRegisterEntries != null) { 
                            foreach (TagNamespaceRegisterEntry entry in _tagNamespaceRegisterEntries) { 
                                string registerDirective = GenerateRegisterDirective(entry.TagPrefix, null, entry.TagNamespace, entry.AssemblyName, null);
                                if (!_registerDirectives.Contains(registerDirective)) { 
                                    _registerDirectives.Add(registerDirective);
                                }
                            }
                        } 
                    }
                    return _registerDirectives; 
                } 

                public override string GetUserControlPath(string tagPrefix, string tagName) { 
                    return _owner._userControlRegisterEntries[tagPrefix + ":" + tagName];
                }

                private string GenerateRegisterDirective(string tagPrefix, string tagName, string ns, string assembly, string src) { 
                    StringBuilder sw = new StringBuilder();
 
                    sw.Append("<%@ Register"); 
                    if (tagPrefix != null && tagPrefix.Length > 0) {
                        sw.Append(" TagPrefix=\""); 
                        sw.Append(tagPrefix);
                        sw.Append("\"");
                    }
 
                    if (!String.IsNullOrEmpty(tagName)) {
                        sw.Append(" TagName=\""); 
                        sw.Append(tagName); 
                        sw.Append("\"");
                    } 

                    if (ns != null) {
                        sw.Append(" Namespace=\"");
                        sw.Append(ns); 
                        sw.Append("\"");
                    } 
 
                    if (!String.IsNullOrEmpty(assembly)) {
                        sw.Append(" Assembly=\""); 
                        sw.Append(assembly);
                        sw.Append("\"");
                    }
 
                    if (!String.IsNullOrEmpty(src)) {
                        sw.Append(" Src=\""); 
                        sw.Append(src); 
                        sw.Append("\"");
                    } 
                    sw.Append("%>");

                    return sw.ToString();
                } 

                private string GenerateRegisterDirective(string tagPrefixAndName, string src) { 
                    StringBuilder sw = new StringBuilder(); 

                    sw.Append("<%@ Register"); 
                    if (!String.IsNullOrEmpty(tagPrefixAndName)) {
                        string[] parts = tagPrefixAndName.Split(':');
                        if (parts.Length == 2) {
                            sw.Append(" TagPrefix=\""); 
                            sw.Append(parts[0]);
                            sw.Append("\""); 
                            sw.Append(" TagName=\""); 
                            sw.Append(parts[1]);
                            sw.Append("\""); 
                        }
                    }

                    if (!String.IsNullOrEmpty(src)) { 
                        sw.Append(" Src=\"");
                        sw.Append(src); 
                        sw.Append("\""); 
                    }
                    sw.Append("%>"); 

                    return sw.ToString();
                }
            } 
        }
 
        private sealed class DummySite : ISite { 
            private IComponent _component;
            private IDesignerHost _designerHost; 
            private IContainer _container;
            private string _name;

            public DummySite(IComponent component, UserControlDesignerHost designerHost) { 
                _component = component;
                _container = (IContainer)designerHost; 
                _designerHost = (IDesignerHost)designerHost; 
            }
 
            IComponent ISite.Component {
                get {
                    return _component;
                } 
            }
 
            IContainer ISite.Container { 
                get {
                    return _container; 
                }
            }

            bool ISite.DesignMode { 
                get {
                    return true; 
                } 
            }
 
            string ISite.Name {
                get {
                    return _name;
                } 
                set {
                    _name = value; 
                } 
            }
 
            object IServiceProvider.GetService(Type type) {
                return _designerHost.GetService(type);
            }
        } 

    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="UserControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Configuration; 
    using System.Design; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions; 
    using System.Web.Configuration;
    using System.Web.UI; 
    using System.Web.UI.HtmlControls; 
    using System.Web.UI.WebControls;
 
    /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="UserControlDesigner"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides design-time support for the usercontrols (controls declared in 
    ///       .ascx files).
    ///    </para> 
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class UserControlDesigner : ControlDesigner { 
        private const string UserControlCacheKey = "__aspnetUserControlCache";

        // This is used when the IDictionaryService does not exist
        private static IDictionary _antiRecursionDictionary = new HybridDictionary(); 

        private bool _userControlFound; 
 
        /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="UserControlDesigner.UserControlDesigner"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Initializes a new instance of the UserControlDesigner class.
        ///    </para>
        /// </devdoc> 
        public UserControlDesigner() {
        } 
 
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new UserControlDesignerActionList(this));
 
                return actionLists;
            } 
        } 

        /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="UserControlDesigner.AllowResize"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether all user controls are resizeable.
        ///    </para> 
        /// </devdoc>
        public override bool AllowResize { 
            get { 
                return false;
            } 
        }

        /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="UserControlDesigner.ShouldCodeSerializeInternal"]/*' />
        internal override bool ShouldCodeSerializeInternal { 
            get {
                if (Component.GetType() == typeof(UserControl)) { 
                    // should always return false - we don't want to code spit out 
                    // a variable of type UserControl
                    return false; 
                }
                return base.ShouldCodeSerializeInternal;
            }
            set { 
                base.ShouldCodeSerializeInternal = value;
            } 
        } 

        private string GenerateUserControlCacheKey(string userControlPath, IThemeResolutionService themeService) { 
            string userControlCacheKey = userControlPath;
            if (themeService != null) {
                ThemeProvider themeProvider = themeService.GetStylesheetThemeProvider();
                if ((themeProvider != null) && !String.IsNullOrEmpty(themeProvider.ThemeName)) { 
                    userControlCacheKey += "|" + themeProvider.ThemeName;
                } 
            } 

            return userControlCacheKey; 
        }

        private string GenerateUserControlHashCode(string contents, IThemeResolutionService themeService) {
            string newHashCode = contents.GetHashCode().ToString(CultureInfo.InvariantCulture); 
            if (themeService != null) {
                ThemeProvider themeProvider = themeService.GetStylesheetThemeProvider(); 
                if (themeProvider != null) { 
                    newHashCode += "|" + themeProvider.ContentHashCode.ToString(CultureInfo.InvariantCulture);
                } 
            }

            return newHashCode;
        } 

        private const string _dummyProtocolAndServer = "file://foo"; 
        private string MakeAppRelativePath(string path) { 
            if (String.IsNullOrEmpty(path) || path.StartsWith("~", StringComparison.Ordinal)) {
                return path; 
            }

            string prefix = Path.GetDirectoryName(RootDesigner.DocumentUrl);
            if (String.IsNullOrEmpty(prefix)) { 
                prefix = "~";
            } 
 
            prefix = prefix.Replace('\\', '/');
            prefix = prefix.Replace("~", _dummyProtocolAndServer); 
            path = path.Replace('\\', '/');
            //
            Uri docUri = new Uri(prefix + "/" + path);
            return docUri.ToString().Replace(_dummyProtocolAndServer, "~"); 
        }
 
 
        /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="UserControlDesigner.GetDesignTimeHtml"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the HTML to be used for the design time representation of the control runtime.
        ///    </para>
        /// </devdoc> 
        public override string GetDesignTimeHtml() {
            // Build up design-time HTML for the user control 
            if (Component.Site != null) { 
                IWebApplication webApp = (IWebApplication)Component.Site.GetService(typeof(IWebApplication));
                IDesignerHost host = (IDesignerHost) Component.Site.GetService(typeof(IDesignerHost)); 
                if ((webApp != null) && (host != null)) {
                    // Try to get the path of the file
                    if (RootDesigner.ReferenceManager != null) {
                        // Split up the tag prefix and tag name 
                        IUserControlDesignerAccessor userControl = (IUserControlDesignerAccessor)Component;
                        string[] tagNameParts = userControl.TagName.Split(':'); 
 
                        string userControlPath = RootDesigner.ReferenceManager.GetUserControlPath(tagNameParts[0], tagNameParts[1]);
                        userControlPath = MakeAppRelativePath(userControlPath); 

                        // Also create a cache key that includes the current theme name
                        IThemeResolutionService themeService = (IThemeResolutionService)Component.Site.GetService(typeof(IThemeResolutionService));
                        string userControlCacheKey = GenerateUserControlCacheKey(userControlPath, themeService); 

                        if (!String.IsNullOrEmpty(userControlPath)) { 
                            string hashCode = null; 
                            string designTimeHtml = String.Empty;
                            bool regenerate = false; 

                            // Default the cache to the anti-recursion dictionary
                            // so we have some way to stop circular refs inside user controls
                            IDictionary userControlCache = _antiRecursionDictionary; 

                            // Try to use the IDictionaryService as the design-time html cache 
                            IDictionaryService dictionaryService = (IDictionaryService)webApp.GetService(typeof(IDictionaryService)); 
                            if (dictionaryService != null) {
                                userControlCache = (IDictionary)dictionaryService.GetValue(UserControlCacheKey); 
                                if (userControlCache == null) {
                                    userControlCache = new HybridDictionary();
                                    dictionaryService.SetValue(UserControlCacheKey, userControlCache);
                                } 

                                Pair pair = (Pair)userControlCache[userControlCacheKey]; 
                                if (pair != null) { 
                                    hashCode = (string)pair.First;
                                    designTimeHtml = (string)pair.Second; 

                                    // VSWhidbey 305364 We have to regenerate if we are using resources from venus
                                    regenerate = designTimeHtml.Contains("mvwres:");
                                } 
                                else {
                                    regenerate = true; 
                                } 
                            }
 
                            // Read the contents of the file
                            IDocumentProjectItem userControlItem = webApp.GetProjectItemFromUrl(userControlPath) as IDocumentProjectItem;
                            if (userControlItem != null) {
                                _userControlFound = true; 

                                StreamReader reader = new StreamReader(userControlItem.GetContents()); 
                                string contents = reader.ReadToEnd(); 

                                string newHashCode = null; 
                                // Check if the hashcode still matches up
                                if (!regenerate) {
                                    newHashCode = GenerateUserControlHashCode(contents, themeService);
                                    regenerate = !String.Equals(newHashCode, hashCode, StringComparison.OrdinalIgnoreCase) 
                                        || contents.Contains(".ascx");
                                } 
 
                                if (regenerate) {
                                    // Detect cycles and return a suitable error 
                                    if (_antiRecursionDictionary.Contains(userControlCacheKey)) {
                                        return CreateErrorDesignTimeHtml(SR.GetString(SR.UserControlDesigner_CyclicError));
                                    }
                                    else { 
                                        _antiRecursionDictionary[userControlCacheKey] = CreateErrorDesignTimeHtml(SR.GetString(SR.UserControlDesigner_CyclicError));
                                    } 
                                    designTimeHtml = String.Empty; 

                                    // Put an empty entry into the cache so we don't 
                                    // get into a recursive user control loop
                                    Pair newPair = new Pair();
                                    if (newHashCode == null) {
                                        newHashCode = GenerateUserControlHashCode(contents, themeService); 
                                    }
                                    newPair.First = newHashCode; 
                                    newPair.Second = designTimeHtml; 
                                    userControlCache[userControlCacheKey] = newPair;
 
                                    // Need to create a dummy page and add the usercontrol to the page
                                    // This is similar to what we do in WebFormDesigner
                                    UserControl componentUserControl = (UserControl)Component;
                                    Page userControlPage = new Page(); 

                                    try { 
                                        userControlPage.Controls.Add(componentUserControl); 

                                        IDesignerHost childHost = new UserControlDesignerHost(host, userControlPage, userControlPath); 
                                        if (!String.IsNullOrEmpty(contents)) {
                                            List<Triplet> userControlRegisterEntries = new List<Triplet>();
                                            // Parse and add all non-literal control to the designer host
                                            Control[] childControls = ControlSerializer.DeserializeControlsInternal(contents, childHost, userControlRegisterEntries); 
                                            foreach (Control childControl in childControls) {
                                                if (!(childControl is LiteralControl) && 
                                                    !(childControl is DesignerDataBoundLiteralControl) && 
                                                    !(childControl is DataBoundLiteralControl)) {
                                                    if (String.IsNullOrEmpty(childControl.ID)) { 
                                                        throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.UserControlDesigner_MissingID), childControl.GetType().Name));
                                                    }

                                                    childHost.Container.Add(childControl); 
                                                }
 
                                                // Need to add child controls to the usercontrol 
                                                componentUserControl.Controls.Add(childControl);
                                            } 

                                            // Add all the registration entries to our internal lists so that our
                                            // dummy Reference Manager can use them.
                                            foreach (Triplet entry in userControlRegisterEntries) { 
                                                string tagPrefix = (string)entry.First;
                                                Pair userControlRegisterEntryData = (Pair)entry.Second; 
                                                Pair tagNamespaceRegisterEntryData = (Pair)entry.Third; 
                                                if (userControlRegisterEntryData != null) {
                                                    string tagName = (string)userControlRegisterEntryData.First; 
                                                    string src = (string)userControlRegisterEntryData.Second;
                                                    ((UserControlDesignerHost)childHost).RegisterUserControl(tagPrefix, tagName, src);
                                                    Debug.Assert(tagNamespaceRegisterEntryData == null, "Registration entry should have either a user control entry or a tag namespace entry.");
                                                } 
                                                else {
                                                    if (tagNamespaceRegisterEntryData != null) { 
                                                        string tagNamespace = (string)tagNamespaceRegisterEntryData.First; 
                                                        string assemblyName = (string)tagNamespaceRegisterEntryData.Second;
                                                        ((UserControlDesignerHost)childHost).RegisterTagNamespace(tagPrefix, tagNamespace, assemblyName); 
                                                    }
                                                    else {
                                                        Debug.Fail("Registration entry should have either a user control entry or a tag namespace entry.");
                                                    } 
                                                }
                                            } 
 
                                            // Now get design-time html for each
                                            StringBuilder designTimeHtmlBuilder = new StringBuilder(); 
                                            foreach (Control childControl in childControls) {
                                                string newHtml = String.Empty;
                                                if (childControl is LiteralControl) {
                                                    designTimeHtmlBuilder.Append(((LiteralControl)childControl).Text); 
                                                }
                                                else if (childControl is DesignerDataBoundLiteralControl) { 
                                                    designTimeHtmlBuilder.Append(((DesignerDataBoundLiteralControl)childControl).Text); 
                                                }
                                                else if (childControl is DataBoundLiteralControl) { 
                                                    designTimeHtmlBuilder.Append(((DataBoundLiteralControl)childControl).Text);
                                                }
                                                else if (childControl is HtmlControl) {
                                                    // Directly render out html controls 
                                                    StringWriter swriter = new StringWriter(CultureInfo.CurrentCulture);
                                                    DesignTimeHtmlTextWriter writer = new DesignTimeHtmlTextWriter(swriter); 
                                                    childControl.RenderControl(writer); 
                                                    designTimeHtmlBuilder.Append(swriter.GetStringBuilder().ToString());
                                                } 
                                                else {
                                                    // Otherwise use their control designers
                                                    ControlDesigner designer = (ControlDesigner)childHost.GetDesigner(childControl);
                                                    ViewRendering viewRendering = designer.GetViewRendering(); 
                                                    designTimeHtmlBuilder.Append(viewRendering.Content);
                                                } 
                                            } 
                                            designTimeHtml = designTimeHtmlBuilder.ToString();
                                        } 

                                        newPair.Second = designTimeHtml;
                                    }
                                    finally { 
                                        // Clear out the entry we just added in the anti-recursion dictionary
                                        // so we don't leave garbage around 
                                        _antiRecursionDictionary.Remove(userControlCacheKey); 

                                        // Remove the child contol from usercontrol 
                                        componentUserControl.Controls.Clear();
                                        // Remove the usercontrol from the page.
                                        userControlPage.Controls.Remove(componentUserControl);
                                    } 
                                }
                            } 
                            else { 
                                designTimeHtml = CreateErrorDesignTimeHtml(SR.GetString(SR.UserControlDesigner_NotFound, userControlPath));
                            } 

                            if (designTimeHtml.Trim().Length > 0) {
                                return designTimeHtml;
                            } 
                        }
                    } 
                } 
            }
 
            // If we didn't generate real design-time html, just show the grey block
            return CreatePlaceHolderDesignTimeHtml();
        }
 
        private void EditUserControl() {
            IWebApplication webApp = (IWebApplication)Component.Site.GetService(typeof(IWebApplication)); 
            if (webApp != null) { 
                // Split up the tag prefix and tag anem
                IUserControlDesignerAccessor userControl = (IUserControlDesignerAccessor)Component; 
                string[] tagNameParts = userControl.TagName.Split(':');
                string userControlPath = RootDesigner.ReferenceManager.GetUserControlPath(tagNameParts[0], tagNameParts[1]);
                if (!String.IsNullOrEmpty(userControlPath)) {
                    userControlPath = MakeAppRelativePath(userControlPath); 
                    IDocumentProjectItem userControlItem = webApp.GetProjectItemFromUrl(userControlPath) as IDocumentProjectItem;
                    if (userControlItem != null) { 
                        userControlItem.Open(); 
                    }
                } 
            }
        }

        private void Refresh() { 
            UpdateDesignTimeHtml();
        } 
 
        /// <include file='doc\PageletDesigner.uex' path='docs/doc[@for="PageletDesigner.GetPersistenceContent"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the persistable inner HTML.
        ///    </para>
        /// </devdoc> 
        internal override string GetPersistInnerHtmlInternal() {
            if (Component.GetType() == typeof(UserControl)) { 
                // always return null, so that the contents of the user control get round-tripped 
                // as is, since we're not in a position to do the actual persistence
                return null; 
            }
            return base.GetPersistInnerHtmlInternal();
        }
 
        private class UserControlDesignerActionList : DesignerActionList {
            private UserControlDesigner _parent; 
 
            public UserControlDesignerActionList(UserControlDesigner parent) : base(parent.Component) {
                _parent = parent; 
            }

            public override bool AutoShow {
                get { 
                    return true;
                } 
                set { 
                }
            } 

            public void EditUserControl() {
                _parent.EditUserControl();
            } 

            public void Refresh() { 
                _parent.Refresh(); 
            }
 
            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                if (_parent._userControlFound) {
                    items.Add(new DesignerActionMethodItem(this, "EditUserControl", SR.GetString(SR.UserControlDesigner_EditUserControl), String.Empty, String.Empty, true)); 
                }
                items.Add(new DesignerActionMethodItem(this, "Refresh", SR.GetString(SR.UserControlDesigner_Refresh), String.Empty, String.Empty, true)); 
 
                return items;
            } 
        }


        // Represents a tag namespace register entry, e.g. 
        // <%@ Register TagPrefix="..." Namespace="..." Assembly="..." %>
        private sealed class TagNamespaceRegisterEntry { 
            public string TagPrefix; 
            public string TagNamespace;
            public string AssemblyName; 

            public TagNamespaceRegisterEntry(string tagPrefix, string tagNamespace, string assemblyName) {
                TagPrefix = tagPrefix;
                TagNamespace = tagNamespace; 
                AssemblyName = assemblyName;
            } 
        } 

        private sealed class UserControlDesignerHost : IContainer, IDesignerHost, IDisposable, IUrlResolutionService { 

            // member variables
            private Hashtable _componentTable;        // Component collection
            private Hashtable _designerTable;         // Designer Collection 
            private IDesignerHost _host;              // Webform Designer host
            private bool _disposed = false;           // flag to indicate the object is disposed 
            private IComponent _rootComponent; 
            private int _nameCounter;
            private string _userControlPath; 

            // Lists of <%@ Register %> directives in the UserControl
            private IDictionary<string, string> _userControlRegisterEntries;
            private IList<TagNamespaceRegisterEntry> _tagNamespaceRegisterEntries; 

            /// <summary> 
            /// Initializes an instance of the UserControlDesignerHost class 
            /// </summary>
            public UserControlDesignerHost(IDesignerHost host, IComponent rootComponent, string userControlPath) { 
                _host = host;
                _componentTable = new Hashtable();
                _designerTable = new Hashtable();
                _rootComponent = rootComponent; 
                _userControlPath = userControlPath;
 
                _rootComponent.Site = new DummySite(_rootComponent, this); 
            }
 
            /// <summary>
            /// Destructor method
            /// </summary>
            ~UserControlDesignerHost() { 
                Dispose(false);
            } 
 
            // Property implementations
            private Hashtable ComponentTable { 
                get {
                    return _componentTable;
                }
            } 
            private Hashtable DesignerTable {
                get { 
                    return _designerTable; 
                }
            } 


            /// <summary>
            /// This method clears the components and the designers from 
            /// the respective hash table if the HasClearableComponents
            /// flag is set to true 
            /// </summary> 
            public void ClearComponents() {
                for (int i = 0; i < DesignerTable.Count; i++) { 
                    if (DesignerTable[i] != null) {
                        IDesigner designer = (IDesigner)DesignerTable[i];
                        try {
                            designer.Dispose(); 
                        }
                        catch { 
                            Debug.Fail("Designer " + designer.GetType().Name + " threw an exception during Dispose."); 
                        }
                    } 
                }
                DesignerTable.Clear();

                for (int i = 0; i < ComponentTable.Count; i++) { 
                    if (ComponentTable[i] != null) {
                        IComponent component = (IComponent)ComponentTable[i]; 
                        ISite site = component.Site; 
                        try {
                            component.Dispose(); 
                        }
                        catch {
                            Debug.Fail("Component " + site.Name + " threw during dispose.  Bad component!!");
                        } 
                        if (component.Site != null) {
                            Debug.Fail("Component " + site.Name + " did not remove itself from its container"); 
                            ((IContainer)this).Remove(component); 
                        }
                    } 
                }
                ComponentTable.Clear();
            }
 
            /// <summary>
            /// This method will be called to clean up any managed objects we had created 
            /// </summary> 
            public void Dispose() {
                Dispose(true); 
                // This object will be cleaned up by the Dispose method.
                // Therefore, you should call GC.SupressFinalize to
                // take this object off the finalization queue
                // and prevent finalization code for this object 
                // from executing a second time.
                GC.SuppressFinalize(this); 
            } 

            // <Summary> 
            // Dispose(bool disposing) executes in two distinct scenarios.
            // If disposing equals true, the method has been called directly
            // or indirectly by a user's code. Managed and unmanaged resources
            // can be disposed. 
            // If disposing equals false, the method has been called by the
            // runtime from inside the finalizer and you should not reference 
            // other objects. Only unmanaged resources can be disposed. 
            // </Summary>
            public void Dispose(bool disposing) { 
                // Check to see if Dispose has already been called.
                if(!this._disposed) {
                    // If disposing equals true, dispose all managed
                    // and unmanaged resources. 
                    if(disposing) {
                        ClearComponents(); 
                        _host = null; 
                        _componentTable = null;
                        _designerTable = null; 

                    }
                    // No unmanaged object to clean up
                } 
                _disposed = true;
            } 
 
            /// <summary>
            /// This method creates a collection of IComponents from the existing 
            /// list of components in the component has table and returns it to the
            /// caller.
            /// </summary>
            /// <returns> 
            /// returns the collection of IComponents
            /// </returns> 
            private IComponent[] GetComponents() { 
                int componentCount = ComponentTable.Count;
                IComponent[] components = new IComponent[componentCount]; 

                if (componentCount != 0) {
                    int i = 0;
                    foreach (IComponent component in ComponentTable.Values) { 
                        components[i++] = component;
                    } 
                } 
                return components;
            } 

            public void RegisterTagNamespace(string tagPrefix, string tagNamespace, string assemblyName) {
                if (_tagNamespaceRegisterEntries == null) {
                    _tagNamespaceRegisterEntries = new List<TagNamespaceRegisterEntry>(); 
                }
                _tagNamespaceRegisterEntries.Add(new TagNamespaceRegisterEntry(tagPrefix, tagNamespace, assemblyName)); 
            } 

            public void RegisterUserControl(string tagPrefix, string tagName, string src) { 
                if (_userControlRegisterEntries == null) {
                    _userControlRegisterEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                _userControlRegisterEntries[tagPrefix + ":" + tagName] = src; 
            }
 
            #region Implementation of IContainer 
            /// <summary>
            /// This method creates a collection of IComponents and returns it to 
            /// the caller.
            /// </summary>
            ComponentCollection IContainer.Components {
                get { 
                    return new ComponentCollection(GetComponents());
                } 
            } 

            /// <summary> 
            /// Adds a given component to the container.
            /// </summary>
            void IContainer.Add(IComponent component) {
                ((IContainer)this).Add(component, null); 
            }
 
            /// <summary> 
            /// Adds a given component to the container. The component is added to
            /// the component collection, also a designer for the component is created 
            /// and added to the designer collection.
            /// </summary>
            void IContainer.Add(IComponent component, string name) {
 
                // Check if the component is not null
                if (component == null) { 
                    throw new ArgumentNullException("component"); 
                }
 
                if (component.Site == null) {
                    component.Site = new DummySite(component, this);
                    if (component is Control) {
                        component.Site.Name = ((Control)component).ID; 
                    }
                    else { 
                        component.Site.Name = "Temp" + (_nameCounter++); 
                    }
                } 

                // make sure we have a name if one was not provided
                if (name == null) {
                    name = component.Site.Name; 
                }
 
                // make sure the name isn't already in use 
                if (ComponentTable[name] != null) {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, SR.GetString(SR.UserControlDesignerHost_ComponentAlreadyExists), name)); 
                }

                ComponentTable[name] = component;
                IDesigner designer = TypeDescriptor.CreateDesigner(component, typeof(IDesigner)); 
                designer.Initialize(component);
                DesignerTable[component] = designer; 
 
                if (component is Control) {
                    ((Control)component).Page = (Page)_rootComponent; 
                }
            }

 
            /// <summary>
            /// Removes a given component from the component collection. This method also 
            /// retrieves the corresponding designer for the component and removes it from 
            /// the designer collection.
            /// </summary> 
            void IContainer.Remove(IComponent component) {
                if (component == null) {
                    throw new ArgumentNullException("component");
                } 

                if (component.Site == null) { 
                    return; 
                }
 
                string name = component.Site.Name;
                if ((name != null) && (ComponentTable[name] == component)) {

                    // dispose and remove the designer 
                    if (DesignerTable != null) {
                        IDesigner designer = (IDesigner)DesignerTable[component]; 
                        if (designer != null) { 
                            DesignerTable.Remove(component);
                            designer.Dispose(); 
                        }
                    }

                    // remove the component 
                    ComponentTable.Remove(name);
                    component.Dispose(); 
 
                    // finally disassociate with the site
                    component.Site = null; 
                }
            }
            #endregion
 
            #region Implementation of IDisposable
            void IDisposable.Dispose() { 
                Dispose(); 
            }
            #endregion 

            #region Implementation of IServiceProvider
            /// <summary>
            ///     Override of Container's GetService method.  Other than that of the IDesignerhost 
            ///     and the IContainer service, this just delegates to the webform designer hosts
            ///     service provider 
            /// </summary> 
            /// <param name="service">
            ///     The type of service to retrieve 
            /// </param>
            /// <returns>
            ///     An instance of the service.
            /// </returns> 
            object IServiceProvider.GetService(Type serviceType) {
                if ((serviceType == typeof(IDesignerHost)) || 
                    (serviceType == typeof(IContainer)) || 
                    (serviceType == typeof(IUrlResolutionService))) {
                    return this; 
                }
                else {
                    return _host.GetService(serviceType);
                } 
            }
            #endregion 
 
            #region Implementation of IServiceContainer
            /// <summary> 
            /// Dummy implementation of IServiceContainer. These method are not intended to be used in any
            /// shape or form.
            /// </summary>
            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote) { 
            }
 
            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback) { 
            }
 
            void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote) {
            }

            void IServiceContainer.AddService(Type serviceType, object serviceInstance) { 
            }
 
            void IServiceContainer.RemoveService(Type serviceType, bool promote) { 
            }
 
            void IServiceContainer.RemoveService(Type serviceType) {
            }
            #endregion
 

            #region Implementation of IDesignerHost 
            /// <summary> 
            /// This is the implementation of IDesigner host. Except for a few methods most of the calls
            /// are forwarded to the webforms designer host or not implemented. The notable implementations 
            /// are: Container, Destroy component, and GetDesigner
            /// </summary>
            IContainer IDesignerHost.Container {
                get { 
                    return ((IContainer)this);
                } 
            } 

            bool IDesignerHost.InTransaction { 
                get {
                    return _host.InTransaction;
                }
            } 

            bool IDesignerHost.Loading { 
                get { 
                    return _host.Loading;
                } 
            }

            string IDesignerHost.TransactionDescription {
                get { 
                    return _host.TransactionDescription;
                } 
            } 

            IComponent IDesignerHost.RootComponent { 
                get {
                    return _rootComponent;
                }
            } 

            string IDesignerHost.RootComponentClassName { 
                get { 
                    return _rootComponent.GetType().Name;
                } 
            }

            event EventHandler IDesignerHost.Activated {
                add { 
                }
                remove { 
                } 
            }
 
            event EventHandler IDesignerHost.Deactivated {
                add {
                }
                remove { 
                }
            } 
 
            event EventHandler IDesignerHost.LoadComplete {
                add { 
                    _host.LoadComplete += value;
                }
                remove {
                    _host.LoadComplete -= value; 
                }
            } 
 
            event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosed {
                add { 
                }
                remove {
                }
            } 

            event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosing { 
                add { 
                }
                remove { 
                }
            }

            event EventHandler IDesignerHost.TransactionOpened { 
                add {
                } 
                remove { 
                }
            } 

            event EventHandler IDesignerHost.TransactionOpening {
                add {
                } 
                remove {
                } 
            } 

            void IDesignerHost.Activate() { 
            }

            IComponent IDesignerHost.CreateComponent(Type componentType) {
                return null; 
            }
 
            IComponent IDesignerHost.CreateComponent(Type componentType, string name) { 
                return null;
            } 

            DesignerTransaction IDesignerHost.CreateTransaction() {
                return (_host.CreateTransaction());
            } 

            DesignerTransaction IDesignerHost.CreateTransaction(string description) { 
                return (_host.CreateTransaction(description)); 
            }
 
            void IDesignerHost.DestroyComponent(IComponent component) {
                ((IContainer)this).Remove(component);
            }
 
            Type IDesignerHost.GetType(string typeName) {
                return _host.GetType(typeName); 
            } 

            IDesigner IDesignerHost.GetDesigner(IComponent component) { 
                IDesigner designer = null;
                if (component == _host.RootComponent) {
                    designer = _host.GetDesigner(component);
                } 
                else if (component == _rootComponent) {
                    designer = new DummyRootDesigner((WebFormsRootDesigner)_host.GetDesigner(_host.RootComponent), _userControlRegisterEntries, _tagNamespaceRegisterEntries, _userControlPath); 
                } 
                else {
                    designer = (IDesigner)DesignerTable[component]; 
                }
                return designer;
            }
 
            private const string dummyProtocolAndServer = "file://foo";
            string IUrlResolutionService.ResolveClientUrl(string relativeUrl) { 
                if (relativeUrl == null) { 
                    throw new ArgumentNullException("relativeUrl");
                } 

                if (IsRooted(relativeUrl) || relativeUrl.Contains("mvwres:")) {
                    return relativeUrl;
                } 

                IUrlResolutionService baseResolutionService = (IUrlResolutionService)_host.GetService(typeof(IUrlResolutionService)); 
                if (baseResolutionService != null) { 
                    if (IsAppRelativePath(relativeUrl)) {
                        relativeUrl = baseResolutionService.ResolveClientUrl(relativeUrl); 
                    }
                    else {
                        string documentUrl = _userControlPath;
                        // If the the specified url is a relative path, make it app-relative based on the user control's path 
                        if ((documentUrl != null) && (documentUrl.Length != 0)) {
                            // If the user control path is app-relative make the url app-relative 
                            // and use the normal resolver to get the correct url 
                            if (IsAppRelativePath(documentUrl)) {
                                documentUrl = documentUrl.Replace("~", dummyProtocolAndServer); 
                                //
                                Uri docUri = new Uri(documentUrl);
                                string[] segments = docUri.Segments;
                                StringBuilder appRelativeUrlBuilder = new StringBuilder("~"); 
                                for (int i = 0; i < segments.Length - 1; i++) {
                                    appRelativeUrlBuilder.Append(segments[i]); 
                                } 

                                relativeUrl = baseResolutionService.ResolveClientUrl(appRelativeUrlBuilder.ToString() + relativeUrl); 
                            }
                            // If the document url is rooted or doc-relative, just append it together
                            else {
                                string fileName = Path.GetFileName(documentUrl); 
                                int index = documentUrl.LastIndexOf(fileName, StringComparison.Ordinal);
                                relativeUrl = Path.Combine(documentUrl.Substring(0, index), relativeUrl); 
                            } 
                        }
                    } 
                }

                return relativeUrl;
            } 
            #endregion
 
            #region Copied from UrlPath.cs 
            private const char appRelativeCharacter = '~';
 
            private static bool IsRooted(String basepath) {
                return(basepath == null || basepath.Length == 0 || basepath[0] == '/' || basepath[0] == '\\');
            }
 
            private static bool IsAppRelativePath(string path) {
                return (path.Length >= 2 && path[0] == appRelativeCharacter && (path[1] == '/' || path[1] == '\\')); 
            } 
            #endregion
        } 

        private sealed class DummyRootDesigner : WebFormsRootDesigner {
            internal WebFormsRootDesigner _rootDesigner;
            private IDictionary<string, string> _userControlRegisterEntries; 
            private IList<TagNamespaceRegisterEntry> _tagNamespaceRegisterEntries;
            private string _documentUrl; 
 
            public DummyRootDesigner(WebFormsRootDesigner rootDesigner, IDictionary<string, string> userControlRegisterEntries, IList<TagNamespaceRegisterEntry> tagNamespaceRegisterEntries, string documentUrl) {
                _rootDesigner = rootDesigner; 
                _userControlRegisterEntries = userControlRegisterEntries;
                _tagNamespaceRegisterEntries = tagNamespaceRegisterEntries;
                _documentUrl = documentUrl;
            } 

            public override string DocumentUrl { 
                get { 
                    return _documentUrl;
                } 
            }

            public override bool IsLoading {
                get { 
                    return _rootDesigner.IsLoading;
                } 
            } 

            public override bool IsDesignerViewLocked { 
                get {
                    // This root designer always effectively has its view "locked" since
                    // the page developer cannot edit any of the contents, and also
                    // GetDesignTimeHtml is only called once on each control designer. 
                    return true;
                } 
            } 

            public override WebFormsReferenceManager ReferenceManager { 
                get {
                    return new DummyWebFormsReferenceManager(this, _rootDesigner.ReferenceManager, _userControlRegisterEntries, _tagNamespaceRegisterEntries);
                }
            } 

            internal IWebApplication WebApplication { 
                get { 
                    if (_rootDesigner != null) {
                        return (IWebApplication)(_rootDesigner.GetService(typeof(IWebApplication))); 
                    }
                    return null;
                }
            } 

            public override void AddClientScriptToDocument(ClientScriptItem scriptItem) { 
                throw new NotSupportedException(); 
            }
 
            public override string AddControlToDocument(Control newControl, Control referenceControl, ControlLocation location) {
                throw new NotSupportedException();
            }
 
            public override ClientScriptItemCollection GetClientScriptsInDocument() {
                throw new NotSupportedException(); 
            } 

            /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.GetControlViewAndTag"]/*' /> 
            /// <devdoc>
            /// </devdoc>
            protected internal override void GetControlViewAndTag(Control control, out IControlDesignerView view, out IControlDesignerTag tag) {
                view = null; 
                tag = null;
            } 
 
            public override void RemoveClientScriptFromDocument(string clientScriptId) {
                throw new NotSupportedException(); 
            }

            public override void RemoveControlFromDocument(Control control) {
                throw new NotSupportedException(); 
            }
 
 
            private sealed class DummyWebFormsReferenceManager : WebFormsReferenceManager {
                private DummyRootDesigner _owner; 
                private WebFormsReferenceManager _baseReferenceManager;
                private Collection<string> _registerDirectives;
                private IDictionary<string, string> _baseUserControlRegisterEntries;
                private IList<TagNamespaceRegisterEntry> _tagNamespaceRegisterEntries; 

                public DummyWebFormsReferenceManager(DummyRootDesigner owner, 
                    WebFormsReferenceManager baseReferenceManager, 
                    IDictionary<string, string> baseUserControlRegisterEntries,
                    IList<TagNamespaceRegisterEntry> tagNamespaceRegisterEntries) { 

                    _owner = owner;
                    _baseReferenceManager = baseReferenceManager;
                    _baseUserControlRegisterEntries = baseUserControlRegisterEntries; 
                    _tagNamespaceRegisterEntries = tagNamespaceRegisterEntries;
                } 
 
                // This code is borrowed from venus\mvw\WebForms\RegisterDirectiveManager.cs
                private bool GetNamespaceAndAssemblyFromType(Type objectType, out string ns, out string asmName) { 
                    if (objectType != null) {
                        Assembly assembly = objectType.Module.Assembly;

                        if (assembly.GlobalAssemblyCache) { 
                            asmName = assembly.FullName;
                        } 
                        else { 
                            asmName = assembly.GetName().Name;
                        } 

                        ns = objectType.Namespace;
                        if (ns == null) {
                            ns = string.Empty; 
                        }
 
                        // Strange case VSWhidbey:329962 
                        // This is a work-around and could be removed if VSWhidbey:372063 gets fixed
                        ns = ns.TrimEnd('.'); 

                        if (ns != null && asmName != null && asmName.Length > 0) {
                            return true;
                        } 
                    }
 
                    ns = null; 
                    asmName = null;
 
                    return false;
                }

                public override Type GetType(string tagPrefix, string tagName) { 
                    return _baseReferenceManager.GetType(tagPrefix, tagName);
                } 
 
                // This code is borrowed from venus\mvw\WebForms\RegisterDirectiveManager.cs
                public override String GetTagPrefix(Type objectType) { 
                    // First we scan through our own list of register directives that we found in the UserControl
                    string tagNamespace;
                    string assembly;
                    if (GetNamespaceAndAssemblyFromType(objectType, out tagNamespace, out assembly)) { 
                        string tagPrefix = null;
                        string assemblylessTagPrefix = null; 
 
                        if (tagNamespace != null && assembly != null) {
                            foreach (TagNamespaceRegisterEntry entry in _tagNamespaceRegisterEntries) { 
                                if (String.Equals(tagNamespace, entry.TagNamespace, StringComparison.OrdinalIgnoreCase)) {
                                    string registerDirectiveAssembly = entry.AssemblyName;
                                    if (!String.IsNullOrEmpty(registerDirectiveAssembly)) {
                                        if (String.Equals(assembly, registerDirectiveAssembly, StringComparison.OrdinalIgnoreCase)) { 
                                            tagPrefix = entry.TagPrefix;
                                            break; 
                                        } 
                                    }
                                    else { 
                                        if (assemblylessTagPrefix == null) {
                                            assemblylessTagPrefix = entry.TagPrefix;
                                        }
                                    } 
                                }
                            } 
 
                            if (tagPrefix == null) {
                                if (assemblylessTagPrefix != null) { 
                                    tagPrefix = assemblylessTagPrefix;
                                }
                                else {
                                    tagPrefix = string.Empty; 
                                }
                            } 
 
                            return tagPrefix;
                        } 
                    }

                    // If we didn't find a mapping, just fall back to the real reference manager's implementation
                    return _baseReferenceManager.GetTagPrefix(objectType); 
                }
 
                public override String RegisterTagPrefix(Type objectType) { 
                    throw new NotSupportedException();
                } 

                private static bool IsRooted(String basepath) {
                    return (basepath == null || basepath.Length == 0 ||
                            basepath[0] == '/' || basepath[0] == '\\' || 
                            Path.IsPathRooted(basepath) ||
                            basepath.IndexOf(Path.VolumeSeparatorChar) >= 0); 
                } 

                private static bool IsAppRelativePath(string path) { 
                    return (path.Length >= 2 && path[0] == '~' && (path[1] == '/' || path[1] == '\\'));
                }

                private static string ResolveFileUrl(string baseURL, string relativeFileUrl) { 
                    if (!IsRooted(relativeFileUrl)) {
                        if (!IsAppRelativePath(relativeFileUrl)) { 
                            // trim off any file name on baseURL 
                            string baseURLFileName = Path.GetFileName(baseURL);
                            int index = baseURL.LastIndexOf(baseURLFileName, StringComparison.Ordinal); 
                            string baseURLWithoutFileName = baseURL.Substring(0, index);
                            relativeFileUrl = Path.Combine(baseURLWithoutFileName, relativeFileUrl);
                        }
                    } 
                    return relativeFileUrl;
                } 
 
                public override ICollection GetRegisterDirectives() {
                    if (_registerDirectives == null) { 
                        try {
                            _registerDirectives = new Collection<string>();
                            IWebApplication webApp = _owner.WebApplication;
                            if (webApp != null) { 
                                Configuration config = webApp.OpenWebConfiguration(true /*readonly*/);
                                if (config != null) { 
                                    PagesSection section = (PagesSection)config.GetSection("system.web/pages"); 
                                    if (section != null) {
                                        string configFilePath = config.FilePath; 
                                        IProjectItem rootProjectItem = webApp.RootProjectItem;
                                        string rootPhysPath = rootProjectItem.PhysicalPath;
                                        string configFileAppPath = "~/" + configFilePath.Substring(rootPhysPath.Length, configFilePath.Length - rootPhysPath.Length);
 
                                        foreach (TagPrefixInfo tagPrefix in section.Controls) {
                                            Dictionary<string, string> tagPrefixStrings = new Dictionary<string, string>(); 
 
                                            tagPrefix.Source = ResolveFileUrl(configFileAppPath, tagPrefix.Source);
 
                                            // Copied from RegisterDirectiveManager
                                            ElementInformation elemInfo = tagPrefix.ElementInformation;
                                            foreach(PropertyInformation propInfo in elemInfo.Properties) {
                                                if (propInfo.Type == typeof(string)) { 
                                                    tagPrefixStrings[propInfo.Name] =
                                                        (propInfo.ValueOrigin != PropertyValueOrigin.Default) ? 
                                                        (string) propInfo.Value : null; 
                                                }
                                            } 
                                            // End copy
                                            _registerDirectives.Add(GenerateRegisterDirective(
                                                tagPrefixStrings["tagPrefix"],
                                                tagPrefixStrings["tagName"], 
                                                tagPrefixStrings["namespace"],
                                                tagPrefixStrings["assembly"], 
                                                tagPrefixStrings["src"])); 
                                        }
                                    } 
                                }
                            }
                        }
                        catch (Exception ex) { 
                            Debug.Fail("Failure fetching register directives from config: \r\n" + ex.ToString());
                        } 
                        if (_baseUserControlRegisterEntries != null) { 
                            foreach (KeyValuePair<string, string> entry in _baseUserControlRegisterEntries) {
                                string registerDirective = GenerateRegisterDirective(entry.Key, entry.Value); 
                                if (!_registerDirectives.Contains(registerDirective)) {
                                    _registerDirectives.Add(registerDirective);
                                }
                            } 
                        }
                        if (_tagNamespaceRegisterEntries != null) { 
                            foreach (TagNamespaceRegisterEntry entry in _tagNamespaceRegisterEntries) { 
                                string registerDirective = GenerateRegisterDirective(entry.TagPrefix, null, entry.TagNamespace, entry.AssemblyName, null);
                                if (!_registerDirectives.Contains(registerDirective)) { 
                                    _registerDirectives.Add(registerDirective);
                                }
                            }
                        } 
                    }
                    return _registerDirectives; 
                } 

                public override string GetUserControlPath(string tagPrefix, string tagName) { 
                    return _owner._userControlRegisterEntries[tagPrefix + ":" + tagName];
                }

                private string GenerateRegisterDirective(string tagPrefix, string tagName, string ns, string assembly, string src) { 
                    StringBuilder sw = new StringBuilder();
 
                    sw.Append("<%@ Register"); 
                    if (tagPrefix != null && tagPrefix.Length > 0) {
                        sw.Append(" TagPrefix=\""); 
                        sw.Append(tagPrefix);
                        sw.Append("\"");
                    }
 
                    if (!String.IsNullOrEmpty(tagName)) {
                        sw.Append(" TagName=\""); 
                        sw.Append(tagName); 
                        sw.Append("\"");
                    } 

                    if (ns != null) {
                        sw.Append(" Namespace=\"");
                        sw.Append(ns); 
                        sw.Append("\"");
                    } 
 
                    if (!String.IsNullOrEmpty(assembly)) {
                        sw.Append(" Assembly=\""); 
                        sw.Append(assembly);
                        sw.Append("\"");
                    }
 
                    if (!String.IsNullOrEmpty(src)) {
                        sw.Append(" Src=\""); 
                        sw.Append(src); 
                        sw.Append("\"");
                    } 
                    sw.Append("%>");

                    return sw.ToString();
                } 

                private string GenerateRegisterDirective(string tagPrefixAndName, string src) { 
                    StringBuilder sw = new StringBuilder(); 

                    sw.Append("<%@ Register"); 
                    if (!String.IsNullOrEmpty(tagPrefixAndName)) {
                        string[] parts = tagPrefixAndName.Split(':');
                        if (parts.Length == 2) {
                            sw.Append(" TagPrefix=\""); 
                            sw.Append(parts[0]);
                            sw.Append("\""); 
                            sw.Append(" TagName=\""); 
                            sw.Append(parts[1]);
                            sw.Append("\""); 
                        }
                    }

                    if (!String.IsNullOrEmpty(src)) { 
                        sw.Append(" Src=\"");
                        sw.Append(src); 
                        sw.Append("\""); 
                    }
                    sw.Append("%>"); 

                    return sw.ToString();
                }
            } 
        }
 
        private sealed class DummySite : ISite { 
            private IComponent _component;
            private IDesignerHost _designerHost; 
            private IContainer _container;
            private string _name;

            public DummySite(IComponent component, UserControlDesignerHost designerHost) { 
                _component = component;
                _container = (IContainer)designerHost; 
                _designerHost = (IDesignerHost)designerHost; 
            }
 
            IComponent ISite.Component {
                get {
                    return _component;
                } 
            }
 
            IContainer ISite.Container { 
                get {
                    return _container; 
                }
            }

            bool ISite.DesignMode { 
                get {
                    return true; 
                } 
            }
 
            string ISite.Name {
                get {
                    return _name;
                } 
                set {
                    _name = value; 
                } 
            }
 
            object IServiceProvider.GetService(Type type) {
                return _designerHost.GetService(type);
            }
        } 

    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
