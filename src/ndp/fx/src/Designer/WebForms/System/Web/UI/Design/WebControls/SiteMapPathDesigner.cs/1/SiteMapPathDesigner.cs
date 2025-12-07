//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2001' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Web.UI.Design.WebControls { 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Globalization;
    using System.IO; 
    using System.Text;
    using System.Web;
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 

    /// <include file='doc\SiteMapPathDesigner.uex' path='docs/doc[@for="SiteMapPathDesigner"]/*' /> 
    /// <devdoc> 
    /// <para>
    /// Provides design-time support for the 
    /// <see cref='System.Web.UI.WebControls.SiteMapPath'/> web control.
    /// </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, 
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class SiteMapPathDesigner : ControlDesigner { 
 
        private SiteMapPath _navigationPath;
        private SiteMapProvider _siteMapProvider; 
        private DesignerAutoFormatCollection _autoFormats;

        private static string[] _controlTemplateNames;
        private static Style[] _templateStyleArray; 

        static SiteMapPathDesigner() { 
            _controlTemplateNames = new string[] { 
                "NodeTemplate",
                "CurrentNodeTemplate", 
                "RootNodeTemplate",
                "PathSeparatorTemplate" };
        }
 
        /// <include file='doc\SiteMapPathDesigner.uex' path='docs/doc[@for="SiteMapPathDesigner.SiteMapPathDesigner"]/*' />
        public SiteMapPathDesigner() { 
        } 

        public override DesignerAutoFormatCollection AutoFormats { 
            get {
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.SITEMAPPATH_SCHEMES,
                        delegate(DataRow schemeData) { return new SiteMapPathAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            } 
        }
 
        private SiteMapProvider DesignTimeSiteMapProvider {
            get {
                if (_siteMapProvider == null) {
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                    _siteMapProvider = new DesignTimeSiteMapProvider(host);
                } 
 
                return _siteMapProvider;
            } 
        }

        public override TemplateGroupCollection TemplateGroups {
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
 
                // don't cache the template groups because the styles might have changed. 
                for (int i=0; i < _controlTemplateNames.Length; i++) {
                    string templateName = _controlTemplateNames[i]; 
                    TemplateGroup templateGroup = new TemplateGroup(templateName);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, templateName, Component, templateName, TemplateStyleArray[i]));

                    groups.Add(templateGroup); 
                }
 
                return groups; 
            }
        } 

        private Style[] TemplateStyleArray {
            get {
                if (_templateStyleArray == null) { 

                    Debug.Assert(_navigationPath != null, "Designer not yet initialized."); 
                    _templateStyleArray = new Style[] { 
                        ((SiteMapPath)ViewControl).NodeStyle,
                        ((SiteMapPath)ViewControl).CurrentNodeStyle, 
                        ((SiteMapPath)ViewControl).RootNodeStyle,
                        ((SiteMapPath)ViewControl).PathSeparatorStyle,
                    };
                } 

                return _templateStyleArray; 
            } 
        }
 
        protected override bool UsePreviewControl {
            get {
                return true;
            } 
        }
 
        public override string GetDesignTimeHtml() { 
            string designTimeHtml = null;
 
            SiteMapPath renderControl = (SiteMapPath)ViewControl;
            try {
                // We need to set this on both the component and renderControl since
                // it uses the provider when saving the content 
                renderControl.Provider = DesignTimeSiteMapProvider;
 
                // Make sure the child controls are recreated 
                ICompositeControlDesignerAccessor designerAccessor = (ICompositeControlDesignerAccessor)renderControl;
                designerAccessor.RecreateChildControls(); 

                designTimeHtml = base.GetDesignTimeHtml();
            }
            catch (Exception e) { 
                designTimeHtml = GetErrorDesignTimeHtml(e);
            } 
 
            return designTimeHtml;
        } 

        /// <include file='doc\SiteMapPathDesigner.uex' path='docs/doc[@for="SiteMapPathDesigner.GetErrorDesignTimeHtml"]/*' />
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRendering) + e.Message); 
        }
 
        /// <include file='doc\SiteMapPathDesigner.uex' path='docs/doc[@for="SiteMapPathDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(SiteMapPath)); 

            base.Initialize(component);

            _navigationPath = (SiteMapPath)component; 

            if (View != null) { 
                View.SetFlags(ViewFlags.TemplateEditing, true); 
            }
        } 
    }

    internal sealed class SiteMapPathAutoFormat : DesignerAutoFormat {
 
        private string      _fontName;
        private FontUnit    _fontSize; 
        private string      _pathSeparator; 

        private bool    _nodeStyleFontBold; 
        private Color   _nodeStyleForeColor;

        private bool    _rootNodeStyleFontBold;
        private Color   _rootNodeStyleForeColor; 

        private Color   _currentNodeStyleForeColor; 
 
        private bool    _pathSeparatorStyleFontBold;
        private Color    _pathSeparatorStyleForeColor; 

        public SiteMapPathAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData);
 
            Style.Width = 400;
            Style.Height = 100; 
        } 

        public override void Apply(Control control) { 
            Debug.Assert(control is SiteMapPath, "SiteMapPathAutoFormat:ApplyScheme- control is not SiteMapPath");
            if (control is SiteMapPath) {
                Apply(control as SiteMapPath);
            } 
        }
 
        private void Apply(SiteMapPath siteMapPath) { 
            siteMapPath.Font.Name = _fontName;
            siteMapPath.Font.Size = _fontSize; 
            siteMapPath.Font.ClearDefaults();

            siteMapPath.NodeStyle.Font.Bold = _nodeStyleFontBold;
            siteMapPath.NodeStyle.ForeColor = _nodeStyleForeColor; 
            siteMapPath.NodeStyle.Font.ClearDefaults();
 
            siteMapPath.RootNodeStyle.Font.Bold = _rootNodeStyleFontBold; 
            siteMapPath.RootNodeStyle.ForeColor = _rootNodeStyleForeColor;
            siteMapPath.RootNodeStyle.Font.ClearDefaults(); 

            siteMapPath.CurrentNodeStyle.ForeColor = _currentNodeStyleForeColor;

            siteMapPath.PathSeparatorStyle.Font.Bold = _pathSeparatorStyleFontBold; 
            siteMapPath.PathSeparatorStyle.ForeColor = _pathSeparatorStyleForeColor;
            siteMapPath.PathSeparatorStyle.Font.ClearDefaults(); 
 
            // VSWhidbey 321248. Reset the pathseparator if it's empty string.
            if (_pathSeparator != null && _pathSeparator.Length == 0) { 
                _pathSeparator = null;
            }

            siteMapPath.PathSeparator = _pathSeparator; 
        }
 
        private bool GetBoolProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value)) 
                return bool.Parse(data.ToString());
            else
                return false;
        } 

        private string GetStringProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag]; 
            if ((data != null) && !data.Equals(DBNull.Value))
                return data.ToString(); 
            else
                return String.Empty;
        }
 
        private void Load(DataRow schemeData) {
            if (schemeData == null) { 
                Debug.Write("SiteMapPathAutoFormatUtil:LoadScheme- scheme not found"); 
                return;
            } 

            _fontName = GetStringProperty("FontName", schemeData);
            _fontSize = new FontUnit(GetStringProperty("FontSize", schemeData), CultureInfo.InvariantCulture);
            _pathSeparator = GetStringProperty("PathSeparator", schemeData); 
            _nodeStyleFontBold = GetBoolProperty("NodeStyleFontBold", schemeData);
            _nodeStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("NodeStyleForeColor", schemeData)); 
            _rootNodeStyleFontBold = GetBoolProperty("RootNodeStyleFontBold", schemeData); 
            _rootNodeStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("RootNodeStyleForeColor", schemeData));
            _currentNodeStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("CurrentNodeStyleForeColor", schemeData)); 
            _pathSeparatorStyleFontBold = GetBoolProperty("PathSeparatorStyleFontBold", schemeData);
            _pathSeparatorStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("PathSeparatorStyleForeColor", schemeData));
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2001' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Web.UI.Design.WebControls { 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Globalization;
    using System.IO; 
    using System.Text;
    using System.Web;
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 

    /// <include file='doc\SiteMapPathDesigner.uex' path='docs/doc[@for="SiteMapPathDesigner"]/*' /> 
    /// <devdoc> 
    /// <para>
    /// Provides design-time support for the 
    /// <see cref='System.Web.UI.WebControls.SiteMapPath'/> web control.
    /// </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, 
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class SiteMapPathDesigner : ControlDesigner { 
 
        private SiteMapPath _navigationPath;
        private SiteMapProvider _siteMapProvider; 
        private DesignerAutoFormatCollection _autoFormats;

        private static string[] _controlTemplateNames;
        private static Style[] _templateStyleArray; 

        static SiteMapPathDesigner() { 
            _controlTemplateNames = new string[] { 
                "NodeTemplate",
                "CurrentNodeTemplate", 
                "RootNodeTemplate",
                "PathSeparatorTemplate" };
        }
 
        /// <include file='doc\SiteMapPathDesigner.uex' path='docs/doc[@for="SiteMapPathDesigner.SiteMapPathDesigner"]/*' />
        public SiteMapPathDesigner() { 
        } 

        public override DesignerAutoFormatCollection AutoFormats { 
            get {
                if (_autoFormats == null) {
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.SITEMAPPATH_SCHEMES,
                        delegate(DataRow schemeData) { return new SiteMapPathAutoFormat(schemeData); }); 
                }
                return _autoFormats; 
            } 
        }
 
        private SiteMapProvider DesignTimeSiteMapProvider {
            get {
                if (_siteMapProvider == null) {
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                    _siteMapProvider = new DesignTimeSiteMapProvider(host);
                } 
 
                return _siteMapProvider;
            } 
        }

        public override TemplateGroupCollection TemplateGroups {
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
 
                // don't cache the template groups because the styles might have changed. 
                for (int i=0; i < _controlTemplateNames.Length; i++) {
                    string templateName = _controlTemplateNames[i]; 
                    TemplateGroup templateGroup = new TemplateGroup(templateName);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, templateName, Component, templateName, TemplateStyleArray[i]));

                    groups.Add(templateGroup); 
                }
 
                return groups; 
            }
        } 

        private Style[] TemplateStyleArray {
            get {
                if (_templateStyleArray == null) { 

                    Debug.Assert(_navigationPath != null, "Designer not yet initialized."); 
                    _templateStyleArray = new Style[] { 
                        ((SiteMapPath)ViewControl).NodeStyle,
                        ((SiteMapPath)ViewControl).CurrentNodeStyle, 
                        ((SiteMapPath)ViewControl).RootNodeStyle,
                        ((SiteMapPath)ViewControl).PathSeparatorStyle,
                    };
                } 

                return _templateStyleArray; 
            } 
        }
 
        protected override bool UsePreviewControl {
            get {
                return true;
            } 
        }
 
        public override string GetDesignTimeHtml() { 
            string designTimeHtml = null;
 
            SiteMapPath renderControl = (SiteMapPath)ViewControl;
            try {
                // We need to set this on both the component and renderControl since
                // it uses the provider when saving the content 
                renderControl.Provider = DesignTimeSiteMapProvider;
 
                // Make sure the child controls are recreated 
                ICompositeControlDesignerAccessor designerAccessor = (ICompositeControlDesignerAccessor)renderControl;
                designerAccessor.RecreateChildControls(); 

                designTimeHtml = base.GetDesignTimeHtml();
            }
            catch (Exception e) { 
                designTimeHtml = GetErrorDesignTimeHtml(e);
            } 
 
            return designTimeHtml;
        } 

        /// <include file='doc\SiteMapPathDesigner.uex' path='docs/doc[@for="SiteMapPathDesigner.GetErrorDesignTimeHtml"]/*' />
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRendering) + e.Message); 
        }
 
        /// <include file='doc\SiteMapPathDesigner.uex' path='docs/doc[@for="SiteMapPathDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(SiteMapPath)); 

            base.Initialize(component);

            _navigationPath = (SiteMapPath)component; 

            if (View != null) { 
                View.SetFlags(ViewFlags.TemplateEditing, true); 
            }
        } 
    }

    internal sealed class SiteMapPathAutoFormat : DesignerAutoFormat {
 
        private string      _fontName;
        private FontUnit    _fontSize; 
        private string      _pathSeparator; 

        private bool    _nodeStyleFontBold; 
        private Color   _nodeStyleForeColor;

        private bool    _rootNodeStyleFontBold;
        private Color   _rootNodeStyleForeColor; 

        private Color   _currentNodeStyleForeColor; 
 
        private bool    _pathSeparatorStyleFontBold;
        private Color    _pathSeparatorStyleForeColor; 

        public SiteMapPathAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData);
 
            Style.Width = 400;
            Style.Height = 100; 
        } 

        public override void Apply(Control control) { 
            Debug.Assert(control is SiteMapPath, "SiteMapPathAutoFormat:ApplyScheme- control is not SiteMapPath");
            if (control is SiteMapPath) {
                Apply(control as SiteMapPath);
            } 
        }
 
        private void Apply(SiteMapPath siteMapPath) { 
            siteMapPath.Font.Name = _fontName;
            siteMapPath.Font.Size = _fontSize; 
            siteMapPath.Font.ClearDefaults();

            siteMapPath.NodeStyle.Font.Bold = _nodeStyleFontBold;
            siteMapPath.NodeStyle.ForeColor = _nodeStyleForeColor; 
            siteMapPath.NodeStyle.Font.ClearDefaults();
 
            siteMapPath.RootNodeStyle.Font.Bold = _rootNodeStyleFontBold; 
            siteMapPath.RootNodeStyle.ForeColor = _rootNodeStyleForeColor;
            siteMapPath.RootNodeStyle.Font.ClearDefaults(); 

            siteMapPath.CurrentNodeStyle.ForeColor = _currentNodeStyleForeColor;

            siteMapPath.PathSeparatorStyle.Font.Bold = _pathSeparatorStyleFontBold; 
            siteMapPath.PathSeparatorStyle.ForeColor = _pathSeparatorStyleForeColor;
            siteMapPath.PathSeparatorStyle.Font.ClearDefaults(); 
 
            // VSWhidbey 321248. Reset the pathseparator if it's empty string.
            if (_pathSeparator != null && _pathSeparator.Length == 0) { 
                _pathSeparator = null;
            }

            siteMapPath.PathSeparator = _pathSeparator; 
        }
 
        private bool GetBoolProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value)) 
                return bool.Parse(data.ToString());
            else
                return false;
        } 

        private string GetStringProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag]; 
            if ((data != null) && !data.Equals(DBNull.Value))
                return data.ToString(); 
            else
                return String.Empty;
        }
 
        private void Load(DataRow schemeData) {
            if (schemeData == null) { 
                Debug.Write("SiteMapPathAutoFormatUtil:LoadScheme- scheme not found"); 
                return;
            } 

            _fontName = GetStringProperty("FontName", schemeData);
            _fontSize = new FontUnit(GetStringProperty("FontSize", schemeData), CultureInfo.InvariantCulture);
            _pathSeparator = GetStringProperty("PathSeparator", schemeData); 
            _nodeStyleFontBold = GetBoolProperty("NodeStyleFontBold", schemeData);
            _nodeStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("NodeStyleForeColor", schemeData)); 
            _rootNodeStyleFontBold = GetBoolProperty("RootNodeStyleFontBold", schemeData); 
            _rootNodeStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("RootNodeStyleForeColor", schemeData));
            _currentNodeStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("CurrentNodeStyleForeColor", schemeData)); 
            _pathSeparatorStyleFontBold = GetBoolProperty("PathSeparatorStyleFontBold", schemeData);
            _pathSeparatorStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("PathSeparatorStyleForeColor", schemeData));
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
