//------------------------------------------------------------------------------ 
// <copyright file="DesignTimeSiteMapProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System.Diagnostics; 

    using System; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Globalization; 
    using System.IO; 
    using System.Web;
    using System.Web.UI; 
    using System.Web.UI.Design;
    using System.Xml;

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, 
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal class DesignTimeSiteMapProviderBase : StaticSiteMapProvider { 
        private SiteMapNode _rootNode; 
        private SiteMapNode _currentNode;
 
        private static readonly string _rootNodeText = SR.GetString(SR.DesignTimeSiteMapProvider_RootNodeText);
        private static readonly string _parentNodeText = SR.GetString(SR.DesignTimeSiteMapProvider_ParentNodeText);
        private static readonly string _siblingNodeText = SR.GetString(SR.DesignTimeSiteMapProvider_SiblingNodeText);
        private static readonly string _currentNodeText = SR.GetString(SR.DesignTimeSiteMapProvider_CurrentNodeText); 
        private static readonly string _childNodeText = SR.GetString(SR.DesignTimeSiteMapProvider_ChildNodeText);
 
        private static readonly string _siblingNodeText1 = _siblingNodeText + " 1"; 
        private static readonly string _siblingNodeText2 = _siblingNodeText + " 2";
        private static readonly string _siblingNodeText3 = _siblingNodeText + " 3"; 

        private static readonly string _childNodeText1 = _childNodeText + " 1";
        private static readonly string _childNodeText2 = _childNodeText + " 2";
        private static readonly string _childNodeText3 = _childNodeText + " 3"; 

        protected IDesignerHost _host; 
 
        internal DesignTimeSiteMapProviderBase(IDesignerHost host) {
            if (host == null) 
                throw new ArgumentNullException("host");

            _host = host;
        } 

        public override SiteMapNode CurrentNode { 
            get { 
                BuildDesignTimeSiteMapInternal();
 
                return _currentNode;
            }
        }
 
        internal string DocumentAppRelativeUrl {
            get { 
                if (_host != null) { 
                    IComponent rootComponent = _host.RootComponent;
 
                    if (rootComponent != null) {
                        WebFormsRootDesigner rootDesigner = _host.GetDesigner(rootComponent) as WebFormsRootDesigner;
                        if (rootDesigner != null) {
                            return rootDesigner.DocumentUrl; 
                        }
                    } 
                } 

                return String.Empty; 
            }
        }

        protected override SiteMapNode GetRootNodeCore() { 
            BuildDesignTimeSiteMapInternal();
 
            return _rootNode; 
        }
 
        private SiteMapNode BuildDesignTimeSiteMapInternal() {
            if (_rootNode != null)
                return _rootNode;
 
            _rootNode = new SiteMapNode(this, _rootNodeText + " url", _rootNodeText + " url", _rootNodeText, _rootNodeText);
            _currentNode = new SiteMapNode(this, _currentNodeText + " url", _currentNodeText + " url", _currentNodeText, _currentNodeText); 
            SiteMapNode parentNode = CreateNewSiteMapNode(_parentNodeText); 
            SiteMapNode siblingNode1 = CreateNewSiteMapNode(_siblingNodeText1);
            SiteMapNode siblingNode2 = CreateNewSiteMapNode(_siblingNodeText2); 
            SiteMapNode siblingNode3 = CreateNewSiteMapNode(_siblingNodeText3);
            SiteMapNode childNode1 = CreateNewSiteMapNode(_childNodeText1);
            SiteMapNode childNode2 = CreateNewSiteMapNode(_childNodeText2);
            SiteMapNode childNode3 = CreateNewSiteMapNode(_childNodeText3); 

            AddNode(_rootNode); 
            AddNode(parentNode, _rootNode); 
            AddNode(siblingNode1, parentNode);
            AddNode(_currentNode, parentNode); 
            AddNode(siblingNode2, parentNode);
            AddNode(siblingNode3, parentNode);
            AddNode(childNode1, _currentNode);
            AddNode(childNode2, _currentNode); 
            AddNode(childNode3, _currentNode);
 
            return _rootNode; 
        }
 
        public override SiteMapNode BuildSiteMap() {
            return BuildDesignTimeSiteMapInternal();
        }
 
        private SiteMapNode CreateNewSiteMapNode(string text) {
            string url = text + "url"; 
            return new SiteMapNode(this, url, url , text, text); 
        }
    } 

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand,
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class DesignTimeSiteMapProvider : DesignTimeSiteMapProviderBase  { 

        private const string _providerAttribute = "provider"; 
        private const string _siteMapFileAttribute = "siteMapFile"; 
        private const string _siteMapNodeName = "siteMapNode";
        private const string _resourcePrefix = "$resources:"; 
        private const char _appRelativeCharacter = '~';
        private const int _resourcePrefixLength = 10;

        private static readonly char[] _seperators = new char[] { ';', ',' }; 
        private SiteMapNode _rootNode;
 
        internal DesignTimeSiteMapProvider(IDesignerHost host) : base(host) { 
        }
 
        public override SiteMapNode CurrentNode {
            get {
                SiteMapNode rootNode;
                SiteMapNode node = GetCurrentNodeFromLiveData(out rootNode); 
                if (node != null)
                    return node; 
 
                return base.CurrentNode;
            } 
        }

        public override SiteMapNode RootNode {
            get { 
                SiteMapNode rootNode;
                SiteMapNode node = GetCurrentNodeFromLiveData(out rootNode); 
                if (rootNode != null) 
                    return rootNode;
 
                return base.RootNode;
            }
        }
 
        private Stream GetSiteMapFileStream(out string physicalPath) {
            physicalPath = String.Empty; 
 
            if (_host != null) {
                // Use the WebApplication server to get the app.sitemap physical path 
                IWebApplication webApplicationService = (IWebApplication)_host.GetService(typeof(IWebApplication));
                if (webApplicationService != null) {
                    IProjectItem dataFileProjectItem = webApplicationService.GetProjectItemFromUrl("~/web.sitemap");
                    if (dataFileProjectItem != null) { 
                        physicalPath = dataFileProjectItem.PhysicalPath;
                        IDocumentProjectItem documentProjectItem = dataFileProjectItem as IDocumentProjectItem; 
 
                        if (documentProjectItem != null) {
                            return documentProjectItem.GetContents(); 
                        }
                    }
                }
            } 

            return null; 
        } 

        private Hashtable _urlTable; 
        internal IDictionary UrlTable {
            get {
                if (_urlTable == null) {
                    lock (this) { 
                        if (_urlTable == null) {
                            _urlTable = new Hashtable(StringComparer.OrdinalIgnoreCase); 
                        } 
                    }
                } 

                return _urlTable;
            }
        } 

        public override SiteMapNode BuildSiteMap() { 
 
            if (_rootNode != null) {
                return _rootNode; 
            }

            String applicationSiteMapFilePath = null;
            Stream stream = GetSiteMapFileStream(out applicationSiteMapFilePath); 

            XmlDocument document = new XmlDocument(); 
 
            if (stream == null) {
                // Use the static designtime data if stream or physical path is not found. 
                if (applicationSiteMapFilePath.Length == 0) {
                    _rootNode = base.BuildSiteMap();
                    return _rootNode;
                } 
                // Load it from the sitemap file physical path if stream is not found.
                else { 
                    document.Load(applicationSiteMapFilePath); 
                }
            } 
            else {
                // Load the stream into a reader if found.
                using (StreamReader sr = new StreamReader(stream)) {
                    document.LoadXml(sr.ReadToEnd()); 
                }
            } 
 
            XmlNode rootXmlNode = null;
            foreach (XmlNode siteMapMode in document.ChildNodes) { 
                if (String.Equals(siteMapMode.Name, "siteMap", StringComparison.Ordinal)) {
                    rootXmlNode = siteMapMode;
                    break;
                } 
            }
 
            if (rootXmlNode == null) { 
                _rootNode = base.BuildSiteMap();
                return _rootNode; 
            }

            try {
                _rootNode = ConvertFromXmlNode(rootXmlNode.FirstChild); 
            }
            catch (Exception ex) { 
                Debug.Fail(ex.ToString()); 

                // Clear any existing nodes. 
                Clear();

                // Use the static content instead.
                _rootNode = base.BuildSiteMap(); 
            }
 
            return _rootNode; 
        }
 
        private string GetAttributeFromXmlNode(XmlNode xmlNode, string attributeName) {
             XmlNode node = xmlNode.Attributes.GetNamedItem(attributeName);
             if (node != null)
                return node.Value; 

             return null; 
        } 

        private SiteMapNode ConvertFromXmlNode(XmlNode xmlNode) { 
            if (xmlNode.Attributes.GetNamedItem(_providerAttribute) != null ||
                xmlNode.Attributes.GetNamedItem(_siteMapFileAttribute) != null) {
                return null;
            } 

            string title = null, url = null, description = null, roles = null; 
 
            title = GetAttributeFromXmlNode(xmlNode, "title");
            description = GetAttributeFromXmlNode(xmlNode, "description"); 
            url = GetAttributeFromXmlNode(xmlNode, "url");
            roles = GetAttributeFromXmlNode(xmlNode, "roles");

            title = HandleResourceAttribute(title); 
            description = HandleResourceAttribute(description);
 
            ArrayList roleList = new ArrayList(); 
            if (roles != null) {
                foreach(string role in roles.Split(_seperators)) { 
                    string trimmedRole = role.Trim();
                    if (trimmedRole.Length > 0) {
                        roleList.Add(trimmedRole);
                    } 
                }
            } 
            roleList = ArrayList.ReadOnly(roleList); 

            if (url == null) { 
                url = String.Empty;
            }

            if (url.Length != 0 && !IsAppRelativePath(url)) { 
                url = "~/" + url;
            } 
 
            String key = url;
            if (key.Length == 0) { 
                key = Guid.NewGuid().ToString();
            }
            SiteMapNode node = new SiteMapNode(this, key, url, title, description, roleList, null, null, null);
 
            SiteMapNodeCollection list = new SiteMapNodeCollection();
            foreach(XmlNode subNode in xmlNode.ChildNodes) { 
                if (subNode.NodeType != XmlNodeType.Element) 
                    continue;
 
                SiteMapNode newNode = ConvertFromXmlNode(subNode);
                if (newNode == null)
                    continue;
 
                list.Add(newNode);
                AddNode(newNode, node); 
            } 

            if (url.Length != 0) { 
                if (UrlTable.Contains(url)) {
                    throw new InvalidOperationException(SR.GetString(SR.DesignTimeSiteMapProvider_Duplicate_Url, url));
                }
 
                UrlTable[url] = node;
            } 
            return node; 
        }
 
        private SiteMapNode GetCurrentNodeFromLiveData(out SiteMapNode rootNode) {
            rootNode = BuildSiteMap();
            if (rootNode != null && DocumentAppRelativeUrl != null) {
                return (SiteMapNode)UrlTable[DocumentAppRelativeUrl]; 
            }
 
            return null; 
        }
 
        private string HandleResourceAttribute(string text) {
            if (!String.IsNullOrEmpty(text)) {
                string temp = text.TrimStart(new char[] { ' ' });
                if (temp.Length > _resourcePrefixLength && 
                    temp.ToLower(CultureInfo.InvariantCulture).StartsWith(_resourcePrefix, StringComparison.Ordinal)) {
 
                    // Retrieve default text from attribute 
                    int defaultIndex = temp.IndexOf(',');
                    if (defaultIndex != -1) { 
                        defaultIndex = temp.IndexOf(',', defaultIndex + 1);
                        if (defaultIndex != -1) {
                            return temp.Substring(defaultIndex + 1);
                        } 
                    }
 
                    return String.Empty; 
                }
            } 

            return text;
        }
 
        private static bool IsAppRelativePath(string path) {
            return (path.Length >= 2 && path[0] == _appRelativeCharacter && (path[1] == '/' || path[1] == '\\')); 
        } 

        // Override and always return true at designtime 
        // Runtime base class will throw if the context is null.
        public override bool IsAccessibleToUser(HttpContext context, SiteMapNode node) {
            return true;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignTimeSiteMapProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System.Diagnostics; 

    using System; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Globalization; 
    using System.IO; 
    using System.Web;
    using System.Web.UI; 
    using System.Web.UI.Design;
    using System.Xml;

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, 
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal class DesignTimeSiteMapProviderBase : StaticSiteMapProvider { 
        private SiteMapNode _rootNode; 
        private SiteMapNode _currentNode;
 
        private static readonly string _rootNodeText = SR.GetString(SR.DesignTimeSiteMapProvider_RootNodeText);
        private static readonly string _parentNodeText = SR.GetString(SR.DesignTimeSiteMapProvider_ParentNodeText);
        private static readonly string _siblingNodeText = SR.GetString(SR.DesignTimeSiteMapProvider_SiblingNodeText);
        private static readonly string _currentNodeText = SR.GetString(SR.DesignTimeSiteMapProvider_CurrentNodeText); 
        private static readonly string _childNodeText = SR.GetString(SR.DesignTimeSiteMapProvider_ChildNodeText);
 
        private static readonly string _siblingNodeText1 = _siblingNodeText + " 1"; 
        private static readonly string _siblingNodeText2 = _siblingNodeText + " 2";
        private static readonly string _siblingNodeText3 = _siblingNodeText + " 3"; 

        private static readonly string _childNodeText1 = _childNodeText + " 1";
        private static readonly string _childNodeText2 = _childNodeText + " 2";
        private static readonly string _childNodeText3 = _childNodeText + " 3"; 

        protected IDesignerHost _host; 
 
        internal DesignTimeSiteMapProviderBase(IDesignerHost host) {
            if (host == null) 
                throw new ArgumentNullException("host");

            _host = host;
        } 

        public override SiteMapNode CurrentNode { 
            get { 
                BuildDesignTimeSiteMapInternal();
 
                return _currentNode;
            }
        }
 
        internal string DocumentAppRelativeUrl {
            get { 
                if (_host != null) { 
                    IComponent rootComponent = _host.RootComponent;
 
                    if (rootComponent != null) {
                        WebFormsRootDesigner rootDesigner = _host.GetDesigner(rootComponent) as WebFormsRootDesigner;
                        if (rootDesigner != null) {
                            return rootDesigner.DocumentUrl; 
                        }
                    } 
                } 

                return String.Empty; 
            }
        }

        protected override SiteMapNode GetRootNodeCore() { 
            BuildDesignTimeSiteMapInternal();
 
            return _rootNode; 
        }
 
        private SiteMapNode BuildDesignTimeSiteMapInternal() {
            if (_rootNode != null)
                return _rootNode;
 
            _rootNode = new SiteMapNode(this, _rootNodeText + " url", _rootNodeText + " url", _rootNodeText, _rootNodeText);
            _currentNode = new SiteMapNode(this, _currentNodeText + " url", _currentNodeText + " url", _currentNodeText, _currentNodeText); 
            SiteMapNode parentNode = CreateNewSiteMapNode(_parentNodeText); 
            SiteMapNode siblingNode1 = CreateNewSiteMapNode(_siblingNodeText1);
            SiteMapNode siblingNode2 = CreateNewSiteMapNode(_siblingNodeText2); 
            SiteMapNode siblingNode3 = CreateNewSiteMapNode(_siblingNodeText3);
            SiteMapNode childNode1 = CreateNewSiteMapNode(_childNodeText1);
            SiteMapNode childNode2 = CreateNewSiteMapNode(_childNodeText2);
            SiteMapNode childNode3 = CreateNewSiteMapNode(_childNodeText3); 

            AddNode(_rootNode); 
            AddNode(parentNode, _rootNode); 
            AddNode(siblingNode1, parentNode);
            AddNode(_currentNode, parentNode); 
            AddNode(siblingNode2, parentNode);
            AddNode(siblingNode3, parentNode);
            AddNode(childNode1, _currentNode);
            AddNode(childNode2, _currentNode); 
            AddNode(childNode3, _currentNode);
 
            return _rootNode; 
        }
 
        public override SiteMapNode BuildSiteMap() {
            return BuildDesignTimeSiteMapInternal();
        }
 
        private SiteMapNode CreateNewSiteMapNode(string text) {
            string url = text + "url"; 
            return new SiteMapNode(this, url, url , text, text); 
        }
    } 

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand,
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    internal sealed class DesignTimeSiteMapProvider : DesignTimeSiteMapProviderBase  { 

        private const string _providerAttribute = "provider"; 
        private const string _siteMapFileAttribute = "siteMapFile"; 
        private const string _siteMapNodeName = "siteMapNode";
        private const string _resourcePrefix = "$resources:"; 
        private const char _appRelativeCharacter = '~';
        private const int _resourcePrefixLength = 10;

        private static readonly char[] _seperators = new char[] { ';', ',' }; 
        private SiteMapNode _rootNode;
 
        internal DesignTimeSiteMapProvider(IDesignerHost host) : base(host) { 
        }
 
        public override SiteMapNode CurrentNode {
            get {
                SiteMapNode rootNode;
                SiteMapNode node = GetCurrentNodeFromLiveData(out rootNode); 
                if (node != null)
                    return node; 
 
                return base.CurrentNode;
            } 
        }

        public override SiteMapNode RootNode {
            get { 
                SiteMapNode rootNode;
                SiteMapNode node = GetCurrentNodeFromLiveData(out rootNode); 
                if (rootNode != null) 
                    return rootNode;
 
                return base.RootNode;
            }
        }
 
        private Stream GetSiteMapFileStream(out string physicalPath) {
            physicalPath = String.Empty; 
 
            if (_host != null) {
                // Use the WebApplication server to get the app.sitemap physical path 
                IWebApplication webApplicationService = (IWebApplication)_host.GetService(typeof(IWebApplication));
                if (webApplicationService != null) {
                    IProjectItem dataFileProjectItem = webApplicationService.GetProjectItemFromUrl("~/web.sitemap");
                    if (dataFileProjectItem != null) { 
                        physicalPath = dataFileProjectItem.PhysicalPath;
                        IDocumentProjectItem documentProjectItem = dataFileProjectItem as IDocumentProjectItem; 
 
                        if (documentProjectItem != null) {
                            return documentProjectItem.GetContents(); 
                        }
                    }
                }
            } 

            return null; 
        } 

        private Hashtable _urlTable; 
        internal IDictionary UrlTable {
            get {
                if (_urlTable == null) {
                    lock (this) { 
                        if (_urlTable == null) {
                            _urlTable = new Hashtable(StringComparer.OrdinalIgnoreCase); 
                        } 
                    }
                } 

                return _urlTable;
            }
        } 

        public override SiteMapNode BuildSiteMap() { 
 
            if (_rootNode != null) {
                return _rootNode; 
            }

            String applicationSiteMapFilePath = null;
            Stream stream = GetSiteMapFileStream(out applicationSiteMapFilePath); 

            XmlDocument document = new XmlDocument(); 
 
            if (stream == null) {
                // Use the static designtime data if stream or physical path is not found. 
                if (applicationSiteMapFilePath.Length == 0) {
                    _rootNode = base.BuildSiteMap();
                    return _rootNode;
                } 
                // Load it from the sitemap file physical path if stream is not found.
                else { 
                    document.Load(applicationSiteMapFilePath); 
                }
            } 
            else {
                // Load the stream into a reader if found.
                using (StreamReader sr = new StreamReader(stream)) {
                    document.LoadXml(sr.ReadToEnd()); 
                }
            } 
 
            XmlNode rootXmlNode = null;
            foreach (XmlNode siteMapMode in document.ChildNodes) { 
                if (String.Equals(siteMapMode.Name, "siteMap", StringComparison.Ordinal)) {
                    rootXmlNode = siteMapMode;
                    break;
                } 
            }
 
            if (rootXmlNode == null) { 
                _rootNode = base.BuildSiteMap();
                return _rootNode; 
            }

            try {
                _rootNode = ConvertFromXmlNode(rootXmlNode.FirstChild); 
            }
            catch (Exception ex) { 
                Debug.Fail(ex.ToString()); 

                // Clear any existing nodes. 
                Clear();

                // Use the static content instead.
                _rootNode = base.BuildSiteMap(); 
            }
 
            return _rootNode; 
        }
 
        private string GetAttributeFromXmlNode(XmlNode xmlNode, string attributeName) {
             XmlNode node = xmlNode.Attributes.GetNamedItem(attributeName);
             if (node != null)
                return node.Value; 

             return null; 
        } 

        private SiteMapNode ConvertFromXmlNode(XmlNode xmlNode) { 
            if (xmlNode.Attributes.GetNamedItem(_providerAttribute) != null ||
                xmlNode.Attributes.GetNamedItem(_siteMapFileAttribute) != null) {
                return null;
            } 

            string title = null, url = null, description = null, roles = null; 
 
            title = GetAttributeFromXmlNode(xmlNode, "title");
            description = GetAttributeFromXmlNode(xmlNode, "description"); 
            url = GetAttributeFromXmlNode(xmlNode, "url");
            roles = GetAttributeFromXmlNode(xmlNode, "roles");

            title = HandleResourceAttribute(title); 
            description = HandleResourceAttribute(description);
 
            ArrayList roleList = new ArrayList(); 
            if (roles != null) {
                foreach(string role in roles.Split(_seperators)) { 
                    string trimmedRole = role.Trim();
                    if (trimmedRole.Length > 0) {
                        roleList.Add(trimmedRole);
                    } 
                }
            } 
            roleList = ArrayList.ReadOnly(roleList); 

            if (url == null) { 
                url = String.Empty;
            }

            if (url.Length != 0 && !IsAppRelativePath(url)) { 
                url = "~/" + url;
            } 
 
            String key = url;
            if (key.Length == 0) { 
                key = Guid.NewGuid().ToString();
            }
            SiteMapNode node = new SiteMapNode(this, key, url, title, description, roleList, null, null, null);
 
            SiteMapNodeCollection list = new SiteMapNodeCollection();
            foreach(XmlNode subNode in xmlNode.ChildNodes) { 
                if (subNode.NodeType != XmlNodeType.Element) 
                    continue;
 
                SiteMapNode newNode = ConvertFromXmlNode(subNode);
                if (newNode == null)
                    continue;
 
                list.Add(newNode);
                AddNode(newNode, node); 
            } 

            if (url.Length != 0) { 
                if (UrlTable.Contains(url)) {
                    throw new InvalidOperationException(SR.GetString(SR.DesignTimeSiteMapProvider_Duplicate_Url, url));
                }
 
                UrlTable[url] = node;
            } 
            return node; 
        }
 
        private SiteMapNode GetCurrentNodeFromLiveData(out SiteMapNode rootNode) {
            rootNode = BuildSiteMap();
            if (rootNode != null && DocumentAppRelativeUrl != null) {
                return (SiteMapNode)UrlTable[DocumentAppRelativeUrl]; 
            }
 
            return null; 
        }
 
        private string HandleResourceAttribute(string text) {
            if (!String.IsNullOrEmpty(text)) {
                string temp = text.TrimStart(new char[] { ' ' });
                if (temp.Length > _resourcePrefixLength && 
                    temp.ToLower(CultureInfo.InvariantCulture).StartsWith(_resourcePrefix, StringComparison.Ordinal)) {
 
                    // Retrieve default text from attribute 
                    int defaultIndex = temp.IndexOf(',');
                    if (defaultIndex != -1) { 
                        defaultIndex = temp.IndexOf(',', defaultIndex + 1);
                        if (defaultIndex != -1) {
                            return temp.Substring(defaultIndex + 1);
                        } 
                    }
 
                    return String.Empty; 
                }
            } 

            return text;
        }
 
        private static bool IsAppRelativePath(string path) {
            return (path.Length >= 2 && path[0] == _appRelativeCharacter && (path[1] == '/' || path[1] == '\\')); 
        } 

        // Override and always return true at designtime 
        // Runtime base class will throw if the context is null.
        public override bool IsAccessibleToUser(HttpContext context, SiteMapNode node) {
            return true;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
