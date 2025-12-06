//------------------------------------------------------------------------------ 
// <copyright file="ControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using Microsoft.Win32;
    using System; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Configuration;
    using System.Data; 
    using System.Design; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.IO;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.InteropServices; 
    using System.Text;
    using System.Web.Configuration; 
    using System.Web.Compilation; 
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.Design.WebControls;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
    using System.Xml;
 
    using WebUIControl = System.Web.UI.Control; 

    /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner"]/*' /> 
    /// <devdoc>
    ///    <para>
    ///       Provides the base class for all namespaced or custom server control designers.
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ControlDesigner : HtmlControlDesigner { 

        internal static readonly string ErrorDesignTimeHtmlTemplate = 
            @"<table cellpadding=""4"" cellspacing=""0"" style=""font: messagebox; color: buttontext; background-color: buttonface; border: solid 1px; border-top-color: buttonhighlight; border-left-color: buttonhighlight; border-bottom-color: buttonshadow; border-right-color: buttonshadow"">
                <tr><td nowrap><span style=""font-weight: bold; color: red"">{0}</span> - {1}</td></tr>
                <tr><td>{2}</td></tr>
              </table>"; 

        private static readonly string PlaceHolderDesignTimeHtmlTemplate = 
            @"<table cellpadding=4 cellspacing=0 style=""font:messagebox;color:buttontext;background-color:buttonface;border: solid 1px;border-top-color:buttonhighlight;border-left-color:buttonhighlight;border-bottom-color:buttonshadow;border-right-color:buttonshadow""> 
              <tr><td nowrap><span style=""font-weight:bold"">{0}</span> - {1}</td></tr>
              <tr><td>{2}</td></tr> 
            </table>";

        private bool isWebControl;           // true if the associated component is a WebControl
        private bool readOnly = true;        // read-only/read-write state of the control design surface. 
        private bool fDirty = false;         // indicates the dirty state of the control (used during inner content saving).
        private int _ignoreComponentChangesCount; 
 
        private bool _inTemplateMode;
 
        private WebUIControl _viewControl;
        private bool _viewControlCreated = false;

        private IControlDesignerTag _tag; 
        private IControlDesignerView _view;
 
        private ControlDesignerState _designerState; 

        private bool _expressionsChanged; 

        // Contains localized inner contents of the control after Localize() is called.
        private string _localizedInnerContent;
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists { 
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new ControlDesignerActionList(this));

                return actionLists;
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.AllowResize"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets or sets a value indicating
        ///       whether or not the control can be resized.
        ///    </para>
        /// </devdoc> 
        public virtual bool AllowResize {
            get { 
                return IsWebControl; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.AutoFormats"]/*' />
        public virtual DesignerAutoFormatCollection AutoFormats {
            get { 
                return new DesignerAutoFormatCollection();
            } 
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DataBindingsEnabled"]/*' /> 
        protected virtual bool DataBindingsEnabled {
            get {
                IControlDesignerView view = View;
                while (view != null) { 
                    EditableDesignerRegion region = (EditableDesignerRegion)view.ContainingRegion;
                    if (region != null) { 
                        if (region.SupportsDataBinding) { 
                            return true;
                        } 
                        else {
                            ControlDesigner containingControlDesigner = region.Designer;
                            if (containingControlDesigner != null) {
                                view = containingControlDesigner.View; 
                            }
                            else { 
                                return false; 
                            }
                        } 
                    }
                    else {
                        return false;
                    } 
                }
 
                return false; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerState"]/*' />
        protected ControlDesignerState DesignerState {
            get { 
                if (_designerState == null) {
                    _designerState = new ControlDesignerState(Component); 
                } 
                return _designerState;
            } 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignTimeHtmlRequiresLoadComplete"]/*' />
        [Obsolete("The recommended alternative is SetViewFlags(ViewFlags.DesignTimeHtmlRequiresLoadComplete, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public virtual bool DesignTimeHtmlRequiresLoadComplete {
            get { 
                return false; 
            }
        } 

        /// <devdoc>
        ///    <para>
        ///      Whether or not the properties of the control will be hidden when the control 
        ///      is placed into template editing mode. The 'ID' property is never hidden.
        ///      The default implementation returns 'true.' 
        ///    </para> 
        /// </devdoc>
        protected internal virtual bool HidePropertiesInTemplateMode { 
            get {
                return true;
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ID"]/*' /> 
        /// <devdoc> 
        ///
        /// </devdoc> 
        public virtual string ID {
            get {
                return ((WebUIControl)Component).ID;
            } 

            set { 
                if (RootDesigner != null) { 
                    RootDesigner.SetControlID((WebUIControl) Component, value);
                } 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InTemplateMode"]/*' /> 
        protected bool InTemplateMode {
            get { 
                return _inTemplateMode; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.IsDirty"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets or sets a value indicating
        ///       whether or not the boolean dirty state of the web control is currently set. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("The recommended alternative is to use Tag.SetDirty() and Tag.IsDirty. http://go.microsoft.com/fwlink/?linkid=14202")] 
        public bool IsDirty {
            get {
                return IsDirtyInternal;
            } 
            set {
                IsDirtyInternal = value; 
            } 
        }
 
        internal bool IsDirtyInternal {
            get {
                if (Tag != null) {
                    return Tag.IsDirty; 
                }
 
                return fDirty; 
            }
            set { 
                if (Tag != null) {
                    Tag.SetDirty(value);
                }
                else { 
                    fDirty = value;
                } 
            } 
        }
 
        internal bool IsIgnoringComponentChanges {
            get {
                return _ignoreComponentChangesCount > 0;
            } 
        }
 
        internal bool IsWebControl { 
            get {
                return isWebControl; 
            }
        }

        // This is internal so that ContainerControlDesigner can have access to it 
        // because of its overridden implementation of GetPersistenceContent().
        internal string LocalizedInnerContent { 
            get { 
                return _localizedInnerContent;
            } 
        }

        public virtual bool ViewControlCreated {
            get { 
                return _viewControlCreated;
            } 
            set { 
                _viewControlCreated = value;
            } 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ReadOnly"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets or sets a value indicating 
        ///       whether or not the control's associated design surface is set to read-only or not. 
        ///    </para>
        /// </devdoc> 
        [Obsolete("The recommended alternative is to inherit from ContainerControlDesigner instead and to use an EditableDesignerRegion. Regions allow for better control of the content in the designer. http://go.microsoft.com/fwlink/?linkid=14202")]
        public bool ReadOnly {
            get {
                return ReadOnlyInternal; 
            }
            set { 
                ReadOnlyInternal = value; 
            }
        } 

        internal bool ReadOnlyInternal {
            get {
                return readOnly; 
            }
            set { 
                readOnly = value; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.RootDesigner"]/*' />
        protected WebFormsRootDesigner RootDesigner {
            get { 
                WebFormsRootDesigner rootDesigner = null;
 
                ISite site = Component.Site; 
                if (site != null) {
                    IDesignerHost designerHost = (IDesignerHost)site.GetService(typeof(IDesignerHost)); 
                    if ((designerHost != null) && (designerHost.RootComponent != null)) {
                        rootDesigner = designerHost.GetDesigner(designerHost.RootComponent) as WebFormsRootDesigner;
                    }
                } 

                return rootDesigner; 
            } 
        }
 
        private bool SupportsDataBindings {
            get {
                BindableAttribute ba = (BindableAttribute)TypeDescriptor.GetAttributes(Component)[typeof(BindableAttribute)];
                return ((ba != null) && ba.Bindable); 
            }
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Tag"]/*' />
        /// <devdoc> 
        /// </devdoc>
        protected IControlDesignerTag Tag {
            get {
                return _tag; 
            }
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TemplateGroups"]/*' />
        public virtual TemplateGroupCollection TemplateGroups { 
            get {
                return new TemplateGroupCollection();
            }
        } 

        // 
        protected virtual bool UsePreviewControl { 
            get {
                object[] attrs = this.GetType().GetCustomAttributes(typeof(SupportsPreviewControlAttribute), false); 
                if (attrs.Length > 0) {
                    SupportsPreviewControlAttribute spca = (SupportsPreviewControlAttribute)attrs[0];
                    return spca.SupportsPreviewControl;
                } 

                return false; 
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.View"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal IControlDesignerView View { 
            get {
                return _view; 
            } 
        }
 
        public WebUIControl ViewControl {
            get {
                if (!ViewControlCreated) {
                    _viewControl = UsePreviewControl ? CreateViewControlInternal() : (WebUIControl)Component; 
                    ViewControlCreated = true;
                } 
 
                return _viewControl;
            } 
            set {
                _viewControl = value;
                ViewControlCreated = true;
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Visible"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual bool Visible {
            get {
                return true;
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignTimeElementView"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        ///   <para>The object on the design surface used to display the visual representation of the control associated with this designer.</para>
        /// </devdoc>
        [Obsolete("Error: This property can no longer be referenced, and is included to support existing compiled applications. The design-time element view architecture is no longer used. http://go.microsoft.com/fwlink/?linkid=14202", true)]
        protected object DesignTimeElementView { 
            get {
                IHtmlControlDesignerBehavior behavior = BehaviorInternal; 
                if (behavior != null) { 
                    Debug.Assert(behavior is IControlDesignerBehavior, "Wrong type of behavior");
 
                    return ((IControlDesignerBehavior)behavior).DesignTimeElementView;
                }
                return null;
            } 
        }
 
        internal static DesignerAutoFormatCollection CreateAutoFormats(string schemes, 
                                                                       CreateAutoFormatDelegate createAutoFormatDelegate) {
            DesignerAutoFormatCollection autoFormats = new DesignerAutoFormatCollection(); 
            try {
                DataSet ds = new DataSet();
                ds.Locale = CultureInfo.InvariantCulture;
                ds.ReadXml(new XmlTextReader(new StringReader(schemes))); 

                Debug.Assert((ds.Tables.Count == 1) && (ds.Tables[0].TableName.Equals("Scheme")), 
                            "Unexpected tables in schemes dataset"); 

                DataTable schemesTable = ds.Tables[0]; 
                schemesTable.PrimaryKey = new DataColumn[] {schemesTable.Columns["SchemeName"]};

                for (int i = 0; i < schemesTable.Rows.Count; i++) {
                    autoFormats.Add(createAutoFormatDelegate(schemesTable.Rows[i])); 
                }
            } 
            catch (Exception e) { 
                Debug.Fail(e.ToString());
            } 
            return autoFormats;
        }

        /// <devdoc> 
        /// Creates a clone of a control based on its persistence. It either gets the outer
        /// content persistence straight from the tool (best bet), or it tries to simply 
        /// serialize the control itself (not always reliable). 
        /// </devdoc>
        internal WebUIControl CreateClonedControl(IDesignerHost parseTimeDesignerHost, bool applyTheme) { 
            string persisted = null;
            if (Tag != null) {
                // As a perf optimization we ask the tag for the outer content
                persisted = Tag.GetOuterContent(); 
            }
            // If we didn't get back outer content, we fall back to the ControlSerializer 
            if (String.IsNullOrEmpty(persisted)) { 
                persisted = ControlPersister.PersistControl((WebUIControl)Component);
            } 
            WebUIControl clonedControl = ControlParser.ParseControl(parseTimeDesignerHost, persisted, applyTheme);
            return clonedControl;
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.CreatePlaceHolderDesignTimeHtml"]/*' />
        protected string CreatePlaceHolderDesignTimeHtml() { 
            return CreatePlaceHolderDesignTimeHtml(null); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.CreatePlaceHolderDesignTimeHtml1"]/*' />
        protected string CreatePlaceHolderDesignTimeHtml(string instruction) {
            string typeName = Component.GetType().Name;
            string name = Component.Site.Name; 

            if (instruction == null) { 
                instruction = String.Empty; 
            }
 
            return String.Format(CultureInfo.InvariantCulture, PlaceHolderDesignTimeHtmlTemplate, typeName, name, instruction);
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.CreateErrorDesignTimeHtml"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected string CreateErrorDesignTimeHtml(string errorMessage) { 
            return CreateErrorDesignTimeHtml(errorMessage, null);
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.CreateErrorDesignTimeHtml1"]/*' />
        /// <devdoc>
        /// </devdoc> 
        protected string CreateErrorDesignTimeHtml(string errorMessage, Exception e) {
            return CreateErrorDesignTimeHtml(errorMessage, e, Component); 
        } 

        internal static string CreateErrorDesignTimeHtml(string errorMessage, Exception e, IComponent component) { 
            Debug.Assert(component != null);

            string name = component.Site.Name;
 
            if (errorMessage == null) {
                errorMessage = String.Empty; 
            } 
            else {
                errorMessage = HttpUtility.HtmlEncode(errorMessage); 
            }

            if (e != null) {
                errorMessage += "<br />" + HttpUtility.HtmlEncode(e.Message); 
            }
 
            return String.Format(CultureInfo.InvariantCulture, ErrorDesignTimeHtmlTemplate, SR.GetString(SR.ControlDesigner_DesignTimeHtmlError), HttpUtility.HtmlEncode(name), errorMessage); 
        }
 
        internal string CreateInvalidParentDesignTimeHtml(Type controlType, Type requiredParentType) {
            return CreateErrorDesignTimeHtml(SR.GetString(SR.Control_CanOnlyBePlacedInside,
                controlType.Name, requiredParentType.Name));
        } 

        private WebUIControl CreateViewControlInternal() { 
            Debug.Assert(Component is WebUIControl); 
            WebUIControl originalControl = (WebUIControl)Component;
            WebUIControl viewControl = CreateViewControl(); 
            ((IControlDesignerAccessor)viewControl).SetOwnerControl(originalControl);
            UpdateExpressionValues(viewControl);

            return viewControl; 
        }
 
        protected virtual WebUIControl CreateViewControl() { 
            return CreateClonedControl((IDesignerHost)GetService(typeof(IDesignerHost)), true);
        } 

        /// <devdoc>
        /// Ensures that an expression is fully parsed. In some cases expression data is left
        /// in its original form, but the contract with ExpressionEditors is that they only get 
        /// parsed data.
        /// </devdoc> 
        private object EnsureParsedExpression(TemplateControl templateControl, ExpressionBinding eb, object parsedData) { 
            if (parsedData == null) {
                // No parsed data, try to re-parse 
                if (templateControl != null) {
                    string trueExpressionPrefix;
                    Type expressionBuilderType = ExpressionEditor.GetExpressionBuilderType(eb.ExpressionPrefix, Component.Site, out trueExpressionPrefix);
                    if (expressionBuilderType != null) { 
                        try {
                            System.Web.Compilation.ExpressionBuilder expressionBuilder = (System.Web.Compilation.ExpressionBuilder)Activator.CreateInstance(expressionBuilderType); 
                            ExpressionBuilderContext ec = new ExpressionBuilderContext(templateControl); 
                            parsedData = expressionBuilder.ParseExpression(eb.Expression, eb.PropertyType, ec);
                        } 
                        catch (Exception ex) {
                            // We basically ignore exceptions coming from the ExpressionBuilder
                            // since the page developer is not likely going to be able to do
                            // anything about it. 
                            Debug.Fail(String.Format(CultureInfo.InvariantCulture, "Exception when instantiating ExpressionBuilder or parsing expression\r\n\r\n{0}", ex.ToString()));
                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)GetService(typeof(IComponentDesignerDebugService)); 
                            if (debugService != null) { 
                                debugService.Fail(SR.GetString(SR.ControlDesigner_CouldNotGetExpressionBuilder, eb.ExpressionPrefix, ex.Message));
                            } 
                        }
                    }
                }
            } 
            return parsedData;
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetBounds"]/*' />
        public Rectangle GetBounds() { 
            if (View != null) {
                return View.GetBounds(null);
            }
            return Rectangle.Empty; 
        }
 
        /// <devdoc> 
        /// Parses a complex property expression and retrieves the correct PropertyDescriptor
        /// and also returns the object that the PropertyDescriptor appears on. 
        /// </devdoc>
        internal static PropertyDescriptor GetComplexProperty(object target, string propName, out object realTarget) {
            realTarget = null;
            string[] propNameParts = propName.Split('.'); 
            PropertyDescriptor currentPropDesc = null;
 
            foreach (string part in propNameParts) { 
                if (String.IsNullOrEmpty(part)) {
                    return null; 
                }
                currentPropDesc = TypeDescriptor.GetProperties(target)[part];
                if (currentPropDesc == null) {
                    return null; 
                }
                realTarget = target; 
                target = currentPropDesc.GetValue(target); 
            }
            return currentPropDesc; 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetDesignTimeHtml"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the HTML to be used for the design time representation of the control runtime. 
        ///    </para> 
        /// </devdoc>
        public virtual string GetDesignTimeHtml() { 
            StringWriter strWriter = new StringWriter(CultureInfo.InvariantCulture);
            DesignTimeHtmlTextWriter htmlWriter = new DesignTimeHtmlTextWriter(strWriter);
            string designTimeHTML = null;
 
            bool restoreVisible = false;
            bool oldVisible = true; 
 
            WebUIControl control = null;
            try { 
                control = ViewControl;
                oldVisible = control.Visible;
                if (oldVisible == false) {
                    control.Visible = true; 
                    restoreVisible = !UsePreviewControl;
                } 
 
                control.RenderControl(htmlWriter);
                designTimeHTML = strWriter.ToString(); 
            }
            catch (Exception ex) {
                designTimeHTML = GetErrorDesignTimeHtml(ex);
            } 
            finally {
                if (restoreVisible) { 
                    control.Visible = oldVisible; 
                }
            } 

            if ((designTimeHTML == null) || (designTimeHTML.Length == 0)) {
                designTimeHTML = GetEmptyDesignTimeHtml();
            } 

            return designTimeHTML; 
        } 

        public virtual string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            return GetDesignTimeHtml();
        }

        /// <devdoc> 
        /// Gets a factory for creating design-time resource providers and writers.
        /// This looks in config for the runtime factory type, and from that gets 
        /// the design time factory type. If none is found, a service is used to get 
        /// the tool's design time resource provider.
        /// </devdoc> 
        public static DesignTimeResourceProviderFactory GetDesignTimeResourceProviderFactory(IServiceProvider serviceProvider) {
            const string GlobalizationSectionName = "system.web/globalization";

            DesignTimeResourceProviderFactory resourceProviderFactory = null; 

            IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication)); 
            Configuration config = null; 
            if (webApp != null) {
                config = webApp.OpenWebConfiguration(true); 
                if (config != null) {
                    GlobalizationSection globConfig = config.GetSection(GlobalizationSectionName) as GlobalizationSection;
                    if (globConfig != null) {
                        string providerTypeName = globConfig.ResourceProviderFactoryType; 
                        if (!String.IsNullOrEmpty(providerTypeName)) {
                            ITypeResolutionService typeResolver = (ITypeResolutionService)serviceProvider.GetService(typeof(ITypeResolutionService)); 
                            if (typeResolver != null) { 
                                Type providerType = typeResolver.GetType(providerTypeName, true, true);
                                if (providerType != null) { 
                                    object[] attrs = providerType.GetCustomAttributes(typeof(DesignTimeResourceProviderFactoryAttribute), true);
                                    if (attrs != null && attrs.Length > 0) {
                                        DesignTimeResourceProviderFactoryAttribute factoryAttr = attrs[0] as DesignTimeResourceProviderFactoryAttribute;
                                        string designTimeProviderTypeName = factoryAttr.FactoryTypeName; 
                                        if (!String.IsNullOrEmpty(designTimeProviderTypeName)) {
                                            Type designTimeProviderType = typeResolver.GetType(designTimeProviderTypeName, true, true); 
                                            if (designTimeProviderType != null && typeof(DesignTimeResourceProviderFactory).IsAssignableFrom(designTimeProviderType)) { 
                                                try {
                                                    resourceProviderFactory = (DesignTimeResourceProviderFactory)Activator.CreateInstance(designTimeProviderType); 
                                                }
                                                catch (Exception ex) {
                                                    // We basically ignore exceptions coming from the DesignTimeResourceProviderFactory
                                                    // since the page developer is not likely going to be able to do anything about it. 
                                                    Debug.Fail(String.Format(CultureInfo.InvariantCulture, "Exception when instantiating DesignTimeResourceProviderFactory\r\n\r\n{0}", ex.ToString()));
                                                    if (serviceProvider != null) { 
                                                        IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)serviceProvider.GetService(typeof(IComponentDesignerDebugService)); 
                                                        if (debugService != null) {
                                                            debugService.Fail(SR.GetString(SR.ControlDesigner_CouldNotGetDesignTimeResourceProviderFactory, designTimeProviderTypeName, ex.Message)); 
                                                        }
                                                    }
                                                }
                                            } 
                                        }
                                    } 
                                } 
                            }
                        } 
                    }
                }
            }
 
            if (resourceProviderFactory == null) {
                IDesignTimeResourceProviderFactoryService resService = (IDesignTimeResourceProviderFactoryService)serviceProvider.GetService(typeof(IDesignTimeResourceProviderFactoryService)); 
                if (resService != null) { 
                    resourceProviderFactory = resService.GetFactory();
                } 
            }

            return resourceProviderFactory;
        } 

        public virtual string GetEditableDesignerRegionContent(EditableDesignerRegion region) { 
            return String.Empty; 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetEmptyDesignTimeHtml"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the HTML to be used at design time as the representation of the 
        ///       control when the control runtime does not return any rendered
        ///       HTML. The default behavior is to return a string containing the name of the 
        ///       component. 
        ///    </para>
        /// </devdoc> 
        protected virtual string GetEmptyDesignTimeHtml() {
            string typeName = Component.GetType().Name;
            string name = Component.Site.Name;
 
            if ((name != null) && (name.Length > 0)) {
                return "[ " + typeName + " \"" + name + "\" ]"; 
            } 
            else {
                return "[ " + typeName + " ]"; 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetErrorDesignTimeHtml"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual string GetErrorDesignTimeHtml(Exception e) { 
            return CreateErrorDesignTimeHtml(SR.GetString(SR.ControlDesigner_UnhandledException), e);
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetPersistInnerHtml"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets the persistable inner HTML.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("The recommended alternative is GetPersistenceContent(). http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetPersistInnerHtml() { 
            return GetPersistInnerHtmlInternal();
        }

        internal virtual string GetPersistInnerHtmlInternal() { 
            if (_localizedInnerContent != null) {
                return _localizedInnerContent; 
            } 

            if (!IsDirtyInternal) { 
                // NOTE: Returning a null string will prevent the actual save.
                return null;
            }
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(host != null, "Did not get a valid IDesignerHost reference"); 
 
            IsDirtyInternal = false;
 
            return ControlSerializer.SerializeInnerContents((WebUIControl)Component, host);
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetPersistenceContent"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public virtual string GetPersistenceContent() { 
#pragma warning disable 618
            return GetPersistInnerHtml(); 
#pragma warning restore 618
        }

        internal void HideAllPropertiesExceptID(IDictionary properties) { 
            ICollection coll = properties.Values;
            if (coll != null) { 
                object[] values = new object[coll.Count]; 
                coll.CopyTo(values, 0);
 
                for (int i = 0; i < values.Length; i++) {
                    PropertyDescriptor prop = (PropertyDescriptor)values[i];
                    if (prop != null) {
                        if (!String.Equals(prop.Name, "ID", StringComparison.OrdinalIgnoreCase)) { 
                            properties[prop.Name] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, BrowsableAttribute.No);
                        } 
                    } 
                }
            } 
        }

        public void Localize(IDesignTimeResourceWriter resourceWriter) {
            OnComponentChanging(Component, new ComponentChangingEventArgs(Component, null)); 

            string newInnerContent; 
 
            string resourceKey = ControlLocalizer.LocalizeControl((WebUIControl)Component, resourceWriter, out newInnerContent);
 
            if (!String.IsNullOrEmpty(resourceKey)) {
                // Add the resource key attribute to the top-level object, if present
                SetTagAttribute("meta:resourcekey", resourceKey, true);
            } 
            if (!String.IsNullOrEmpty(newInnerContent)) {
                // Regardless of whether we had a resource key for the top-level object, we 
                // might still have new inner content. For example, a MultiView doesn't have 
                // any localizable properties, so it doesn't get a meta:resourcekey tag.
                // However, controls inside it may have been localized, so we need to get 
                // new inner content for them.
                _localizedInnerContent = newInnerContent;
            }
 
            OnComponentChanged(Component, new ComponentChangedEventArgs(Component, null, null, null));
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetViewRendering"]/*' />
        public static ViewRendering GetViewRendering(System.Web.UI.Control control) { 
            ControlDesigner designer = null;

            ISite site = control.Site;
            if (site != null) { 
                IDesignerHost host = (IDesignerHost)site.GetService(typeof(IDesignerHost));
                Debug.Assert(host != null, "Did not get a valid IDesignerHost reference"); 
 
                designer = host.GetDesigner(control) as ControlDesigner;
            } 

            return GetViewRendering(designer);
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetViewRendering1"]/*' />
        public static ViewRendering GetViewRendering(ControlDesigner designer) { 
            string designTimeHtml = String.Empty; 
            DesignerRegionCollection regions = new DesignerRegionCollection();
            bool visible = true; 

            if (designer != null) {
                bool supportsRegions = false;
                if (designer.View != null) { 
                    supportsRegions = designer.View.SupportsRegions;
                } 
 
                try {
                    // clear the preview control so it will be current when we call GetDesignTimeHtml 
                    designer.ViewControlCreated = false;

                    // Otherwise, just get the view rendering for this control
                    if (supportsRegions) { 
                        designTimeHtml = designer.GetDesignTimeHtml(regions);
                    } 
                    else { 
                        designTimeHtml = designer.GetDesignTimeHtml();
                    } 

                    // Get Visible property after calling GetDesignTimeHtml(), so visible will still be true
                    // if GetDesignTimeHtml() throws an exception.
                    visible = designer.Visible; 
                }
                catch (Exception ex) { 
                    // If an exception was thrown, create an error block for it 
                    regions.Clear();
                    try { 
                        designTimeHtml = designer.GetErrorDesignTimeHtml(ex);
                    }
                    catch (Exception ex2) {
                        // If generating the designer's custom error block threw, 
                        // create a default error block
                        designTimeHtml = designer.CreateErrorDesignTimeHtml(ex2.Message); 
                    } 

                    // Ensure View is always visible if there was an error. 
                    visible = true;
                }
            }
 
            return new ViewRendering(designTimeHtml, regions, visible);
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetViewRendering2"]/*' />
        public ViewRendering GetViewRendering() { 
            ViewRendering viewRendering = null;


            EditableDesignerRegion containingRegion = null; 
            if (View != null) {
                containingRegion = View.ContainingRegion as EditableDesignerRegion; 
            } 

            if (containingRegion != null) { 
                // Call the containing editable region to get the view rendering
                viewRendering = ((EditableDesignerRegion)containingRegion).GetChildViewRendering((WebUIControl)this.Component);
            }
            else { 
                viewRendering = ControlDesigner.GetViewRendering(this);
            } 
 
            return viewRendering;
        } 

        private void IgnoreComponentChanges(bool ignore) {
            _ignoreComponentChangesCount += (ignore ? 1 : -1);
            Debug.Assert(_ignoreComponentChangesCount >= 0, "Ignore count should always be non-negative"); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Initialize"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes the designer using
        ///       the specified component.
        ///    </para>
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(WebUIControl)); 
            base.Initialize(component); 

            if (RootDesigner != null) { 
                RootDesigner.GetControlViewAndTag((WebUIControl)Component, out _view, out _tag);
                if (_view != null) {
                    _view.ViewEvent += new ViewEventHandler(OnViewEvent);
                } 
            }
 
            Expressions.Changed += new EventHandler(OnExpressionsChanged); 

            isWebControl = (component is WebControl); 

            UpdateExpressionValues(component);
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Invalidate"]/*' />
        public void Invalidate() { 
            if (View != null) { 
                Invalidate(View.GetBounds(null));
            } 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Invalidate1"]/*' />
        public void Invalidate(Rectangle rectangle) { 
            if (View != null) {
                View.Invalidate(rectangle); 
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InvokeTransactedChange"]/*' />
        public static void InvokeTransactedChange(IComponent component, TransactedChangeCallback callback, object context, string description) {
            InvokeTransactedChange(component, callback, context, description, null);
        } 

        public static void InvokeTransactedChange(IComponent component, TransactedChangeCallback callback, object context, string description, MemberDescriptor member) { 
            if (component == null) { 
                throw new ArgumentNullException("component");
            } 
            InvokeTransactedChange(component.Site, component, callback, context, description, member);
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InvokeTransactedChange1"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public static void InvokeTransactedChange(IServiceProvider serviceProvider, IComponent component, TransactedChangeCallback callback, object context, string description, MemberDescriptor member) { 
            if (component == null) {
                throw new ArgumentNullException("component"); 
            }
            if (callback == null) {
                throw new ArgumentNullException("callback");
            } 
            if (serviceProvider == null) {
                throw new ArgumentException(SR.GetString(SR.ControlDesigner_TransactedChangeRequiresServiceProvider), "serviceProvider"); 
            } 

            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null);

            using (DesignerTransaction transaction = designerHost.CreateTransaction(description)) {
                IComponentChangeService changeService = (IComponentChangeService)serviceProvider.GetService(typeof(IComponentChangeService)); 

                if (changeService != null) { 
                    try { 
                        changeService.OnComponentChanging(component, member);
                    } 
                    catch (CheckoutException ce) {
                        if (ce == CheckoutException.Canceled) {
                            // This will exit the "using" statement and the transaction will be cancelled
                            return; 
                        }
                        throw ce; 
                    } 
                }
 
                ControlDesigner controlDesigner = designerHost.GetDesigner(component) as ControlDesigner;
                bool unIgnored = false; // This makes sure we unignore only once
                try {
                    if (controlDesigner != null) { 
                        controlDesigner.IgnoreComponentChanges(true);
                    } 
                    if (callback(context)) { 
                        if (controlDesigner != null) {
                            unIgnored = true; 
                            controlDesigner.IgnoreComponentChanges(false);
                        }
                        if (changeService != null) {
                            changeService.OnComponentChanged(component, member, null, null); 
                        }
 
                        TypeDescriptor.Refresh(component); 

                        transaction.Commit(); 
                    }
                }
                finally {
                    if (controlDesigner != null && !unIgnored) { 
                        controlDesigner.IgnoreComponentChanges(false);
                    } 
                } 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.IsPropertyBound"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets a value indicating whether a particular property (identified by its name) is data bound.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("The recommended alternative is DataBindings.Contains(string). The DataBindings collection allows more control of the databindings associated with the control. http://go.microsoft.com/fwlink/?linkid=14202")]
        public bool IsPropertyBound(string propName) { 
            return (DataBindings[propName] != null);
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnAutoFormatApplied"]/*' /> 
        /// <devdoc>
        /// Called when an autoformat has been applied to the control. 
        /// A designer may override this to inspect the control's properties, or 
        /// to take further action.
        /// </devdoc> 
        public virtual void OnAutoFormatApplied(DesignerAutoFormat appliedAutoFormat) {
        }

        private static readonly Attribute[] emptyAttrs = new Attribute[0]; 
        private static readonly Attribute[] nonBrowsableAttrs = new Attribute[] { BrowsableAttribute.No };
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);

            PropertyDescriptor prop; 

            // Handle shadowed properties 
            prop = (PropertyDescriptor)properties["ID"]; 
            if (prop != null) {
                properties["ID"] = TypeDescriptor.CreateProperty(GetType(), prop, emptyAttrs); 
            }

            prop = (PropertyDescriptor)properties["SkinID"];
            if (prop != null) { 
                properties["SkinID"] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, new TypeConverterAttribute(typeof(SkinIDTypeConverter)));
            } 
 
            if (InTemplateMode) {
                // If we are in template editing mode, we optionally hide all 
                // properties except for the ID property. We always make the ID
                // property readonly in template editing mode.

                if (HidePropertiesInTemplateMode) { 
                    HideAllPropertiesExceptID(properties);
                } 
 
                prop = (PropertyDescriptor)properties["ID"];
                if (prop != null) { 
                    properties[prop.Name] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, ReadOnlyAttribute.Yes);
                }
            }
 
#if ORCAS
            IFilterResolutionService filterResolutionService = (IFilterResolutionService)Component.Site.GetService(typeof(IFilterResolutionService)); 
            bool filterSet = false; 
            if (filterResolutionService != null) {
                filterSet = (filterResolutionService.CurrentFilter.Length > 0) && !String.Equals(filterResolutionService.CurrentFilter, "default", StringComparison.InvariantCultureIgnoreCase); 
            }

            IDictionary replaceDictionary = new HybridDictionary(true);
            // Hide some properties and unfilterable properties if a filter is set 
            if (filterSet) {
                foreach (PropertyDescriptor pd in properties.Values) { 
                    if (!FilterableAttribute.IsPropertyFilterable(pd) || 
                        String.Equals(pd.Name, "DynamicProperties", StringComparison.InvariantCultureIgnoreCase)) {
                        replaceDictionary[pd.Name] = TypeDescriptor.CreateProperty(pd.ComponentType, pd, nonBrowsableAttrs); 
                    }
                }
            }
 
            foreach (DictionaryEntry entry in replaceDictionary) {
                properties[entry.Key] = entry.Value; 
            } 
#endif
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnBindingsCollectionChanged"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Delegate to
        ///       handle bindings collection changed event. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("The recommended alternative is to handle the Changed event on the DataBindings collection. The DataBindings collection allows more control of the databindings associated with the control. http://go.microsoft.com/fwlink/?linkid=14202")] 
        protected override void OnBindingsCollectionChanged(string propName) {
            // NOTE: This code is here strictly for backwards compatibility.
            // Some control designers did some funky things by adding and
            // removing DataBindings from within property setters. On top of 
            // this, those properties were marked with DesignerSerializationVisibility
            // set to Hidden. This code is from v1.x and just does the right thing 
            // for this rare and obscure case. 

            if (Tag == null) 
                return;

            DataBindingCollection bindings = DataBindings;
 
            if (propName != null) {
                DataBinding db = bindings[propName]; 
 
                string persistPropName = propName.Replace('.', '-');
 
                if (db == null) {
                    Tag.RemoveAttribute(persistPropName);
                }
                else { 
                    string bindingExpr = "<%# " + db.Expression + " %>";
                    Tag.SetAttribute(persistPropName, bindingExpr); 
 
                    if (persistPropName.IndexOf('-') < 0) {
                        // We only reset top-level properties to be consistent with 
                        // what we do the other way around, i.e., when a databound
                        // property value is set to some value
                        ResetPropertyValue(persistPropName, false);
                    } 
                }
            } 
            else { 
                string[] removedBindings = bindings.RemovedBindings;
                foreach (string s in removedBindings) { 
                    string persistPropName = s.Replace('.', '-');
                    Tag.RemoveAttribute(persistPropName);
                }
 
                foreach (DataBinding db in bindings) {
                    string bindingExpr = "<%# " + db.Expression + " %>"; 
                    string persistPropName = db.PropertyName.Replace('.', '-'); 

                    Tag.SetAttribute(persistPropName, bindingExpr); 
                    if (persistPropName.IndexOf('-') < 0) {
                        // We only reset top-level properties to be consistent with
                        // what we do the other way around, i.e., when a databound
                        // property value is set to some value 
                        ResetPropertyValue(persistPropName, false);
                    } 
                } 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnClick"]/*' />
        /// <devdoc>
        /// </devdoc> 
        protected virtual void OnClick(DesignerRegionMouseEventArgs e) {
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnComponentChanged"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Delegate to handle component changed event.
        ///    </para>
        /// </devdoc> 
        public virtual void OnComponentChanged(object sender, ComponentChangedEventArgs ce) {
            if (IsIgnoringComponentChanges) { 
                return; 
            }
 
            IComponent component = Component;
            Debug.Assert(ce.Component == component, "ControlDesigner::OnComponentChanged - Called from an unknown/invalid source object");

            if (DesignTimeElementInternal == null) { 
                return;
            } 
 
            MemberDescriptor member = ce.Member;
 
            if (member != null) {
                //
                Type t = Type.GetType("System.ComponentModel.ReflectPropertyDescriptor, " + AssemblyRef.System);
 
                if ((member.GetType() != t) ||
                    (ce.NewValue != null && ce.NewValue == ce.OldValue)) { 
                    // HACK: ideally, we would prevent the property descriptor from firing this change. 
                    // This would tear large holes in the WFC architecture. Instead, we do the
                    // filtering ourselves in this evil fashion. 

                    Debug.WriteLineIf(CompModSwitches.UserControlDesigner.TraceInfo, "    ...ignoring property descriptor of type: " + member.GetType().Name);
                    return;
                } 

                if (((PropertyDescriptor)member).SerializationVisibility != DesignerSerializationVisibility.Hidden) { 
                    // Set the dirty state upon changing persistable properties. 
                    IsDirtyInternal = true;
 
                    PersistenceModeAttribute persistenceType = (PersistenceModeAttribute)member.Attributes[typeof(PersistenceModeAttribute)];
                    PersistenceMode mode = persistenceType.Mode;

                    if ((mode == PersistenceMode.Attribute) || 
                        (mode == PersistenceMode.InnerDefaultProperty) ||
                        (mode == PersistenceMode.EncodedInnerDefaultProperty)) { 
                        string propName = member.Name; 

                        // Check to see whether the property that was changed is data bound. 
                        // If it is we need to remove it...
                        // For this rev, we're only doing this for the properties on the Component itself
                        // as we can't distinguish which subproperty of a complex type changed.
                        if (ce.Component == Component) { 
                            if (DataBindings.Contains(propName)) {
                                DataBindings.Remove(propName, false); 
                                RemoveTagAttribute(propName, true); 
                            }
 
                            if (Expressions.Contains(propName)) {
                                ExpressionBinding eb = Expressions[propName];
                                if (!eb.Generated) {
                                    Expressions.Remove(propName, false); 
                                    RemoveTagAttribute(propName, true);
                                } 
 
                                // Always mark expressions as changed so that UpdateExpressionValues will be called.
                                // This is necessary because when a property's value is set, it gets set on 
                                // the component itself, and that overwrites the expression's design-time value,
                                // and we need to restore the expression's design-time value.
                                _expressionsChanged = true;
                            } 
                        }
 
                        // For tag level properties ... 
                        WebUIControl control = (WebUIControl)ce.Component;
 
                        IDesignerHost host = null;
                        if (control.Site != null) {
                            host = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost));
                        } 

                        Debug.Assert(host != null, "Need an IDesignerHost to persist properties"); 
                        if (host != null) { 
                            ArrayList attribs = ControlSerializer.GetControlPersistedAttribute(control, (PropertyDescriptor)member, host);
 
                            PersistAttributes(attribs);
                        }
                    }
                } 
            }
            else { 
                // member is null, meaning that the whole component 
                // could have changed and not just a single member.
                // This happens when a component is edited through a ComponentEditor. 

                // Set the dirty state if more than one property is changed.
                IsDirtyInternal = true;
 
                WebUIControl control = (WebUIControl)ce.Component;
 
                IDesignerHost host = null; 
                if (control.Site != null) {
                    host = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost)); 
                }

                // Reset all properties which used to have expression, but don't anymore
                // Do this before persisting attributes so the old expression-evaluated values 
                // don't get into the persistence
                foreach (string propName in Expressions.RemovedBindings) { 
                    object realTarget; 
                    PropertyDescriptor propDesc = GetComplexProperty(Component, propName, out realTarget);
                    if (propDesc != null) { 
                        IgnoreComponentChanges(true);
                        try {
                            propDesc.ResetValue(realTarget);
                        } 
                        finally {
                            IgnoreComponentChanges(false); 
                        } 
                    }
                } 

                Debug.Assert(host != null, "Need an IDesignerHost to persist properties");
                if (host != null) {
                    ArrayList attribs = ControlSerializer.GetControlPersistedAttributes(control, host); 

                    PersistAttributes(attribs); 
                } 

                foreach (DataBinding db in DataBindings) { 
                    if (db.PropertyName.IndexOf('.') < 0) {
                        // We only reset top-level properties to be consistent with
                        // what we do the other way around, i.e., when a databound
                        // property value is set to some value 
                        ResetPropertyValue(db.PropertyName, false);
                    } 
                } 

                OnBindingsCollectionChangedInternal(null); 

                // Since we don't know which property changed, we force re-evaluation of all expressions.
                // This is necessary because when a property's value is set, it gets set on
                // the component itself, and that overwrites the expression's design-time value, 
                // and we need to restore the expression's design-time value.
                _expressionsChanged = true; 
            } 

            if (_expressionsChanged) { 
                UpdateExpressionValues(Component);
            }

            // Update the HTML and verbs. 
            UpdateDesignTimeHtml();
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnComponentChanging"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public virtual void OnComponentChanging(object sender, ComponentChangingEventArgs ce) {
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnControlResize"]/*' />
        /// <devdoc> 
        ///     Notification from the identity behavior upon resizing the control in the designer. 
        ///     This is only called when a user action causes the control to be resized.
        ///     Note that this method may be called several times during a resize process so as 
        ///     to enable live-resize of the contents of the control.
        /// </devdoc>
        [Obsolete("The recommended alternative is OnComponentChanged(). OnComponentChanged is called when any property of the control is changed. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual void OnControlResize() { 
        }
 
        private void OnExpressionsChanged(object sender, EventArgs e) { 
            // Remember that the collection as changed so we can re-persist in OnComponentChanged
            _expressionsChanged = true; 
        }

        /// <devdov>
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnPaint"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual void OnPaint(PaintEventArgs e) { 
        }
 
        private void OnViewEvent(object sender, ViewEventArgs e) {
            if (e.EventType == ViewEvent.Click) {
                OnClick((DesignerRegionMouseEventArgs)e.EventArgs);
            } 
            else if (e.EventType == ViewEvent.Paint) {
                OnPaint((PaintEventArgs)e.EventArgs); 
            } 
            else if (e.EventType == ViewEvent.TemplateModeChanged) {
                TemplateModeChangedEventArgs tmea = (TemplateModeChangedEventArgs)e.EventArgs; 
                _inTemplateMode = (tmea.NewTemplateGroup != null);
                // Invalidate the type descriptor so that proper filtering of properties
                // is done when entering and exiting template mode.
                TypeDescriptor.Refresh(Component); 
            }
        } 
 
        private void PersistAttributes(ArrayList attributes) {
            foreach (Triplet triplet in attributes) { 
                string attribName = Convert.ToString(triplet.Second, CultureInfo.InvariantCulture);
                string filter = triplet.First.ToString();
                if ((filter == null) || (filter.Length > 0)) {
                    attribName = filter + ':' + attribName; 
                }
                if (triplet.Third == null) { 
                    RemoveTagAttribute(attribName, true); 
                }
                else { 
                    string persistValue = Convert.ToString(triplet.Third, CultureInfo.InvariantCulture);
                    SetTagAttribute(attribName, persistValue, true);
                }
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.RaiseResizeEvent"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because resizing is handled by the OnComponentChanged() method. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void RaiseResizeEvent() {
            OnControlResize();
        } 

        /// <devdoc> 
        /// Registers internal data in a cloned item. Whenever an item that is to be 
        /// persisted is cloned, there are some internal data structures that have to
        /// be cloned as well. This can only be done by the ControlDesigner. 
        /// </devdoc>
        public void RegisterClone(object original, object clone) {
            if (original == null) {
                throw new ArgumentNullException("original"); 
            }
            if (clone == null) { 
                throw new ArgumentNullException("clone"); 
            }
            ControlBuilder cb = ((IControlBuilderAccessor)Component).ControlBuilder; 
            if (cb != null) {
                ObjectPersistData persistData = cb.GetObjectPersistData();
                persistData.BuiltObjects[clone] = persistData.BuiltObjects[original];
            } 
        }
 
        private void ResetPropertyValue(string property, bool useInstance) { 
            PropertyDescriptor propDesc = null;
 
            if (useInstance) {
                propDesc = TypeDescriptor.GetProperties(Component)[property];
            }
            else { 
                propDesc = TypeDescriptor.GetProperties(Component.GetType())[property];
            } 
 
            if (propDesc != null) {
                IgnoreComponentChanges(true); 
                try {
                    propDesc.ResetValue(Component);
                }
                finally { 
                    IgnoreComponentChanges(false);
                } 
            } 
        }
 
        private void RemoveTagAttribute(string name, bool ignoreCase) {
            if (Tag != null) {
                Tag.RemoveAttribute(name);
            } 
            else {
                BehaviorInternal.RemoveAttribute(name, ignoreCase); 
            } 
        }
 
        public virtual void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) {
        }

        protected void SetRegionContent(EditableDesignerRegion region, string content) { 
            if (View != null) {
                View.SetRegionContent(region, content); 
            } 
        }
 
        private void SetTagAttribute(string name, object value, bool ignoreCase) {
            if (Tag != null) {
                Tag.SetAttribute(name, value.ToString());
            } 
            else {
                BehaviorInternal.SetAttribute(name, value, ignoreCase); 
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.SetViewFlags"]/*' />
        protected void SetViewFlags(ViewFlags viewFlags, bool setFlag) {
            if (View != null) {
                View.SetFlags(viewFlags, setFlag); 
            }
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.UpdateDesignTimeHtml"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Updates the design time HTML.
        ///    </para>
        /// </devdoc> 
        public virtual void UpdateDesignTimeHtml() {
 
            if (View != null) { 
                View.Update();
            } 
            else {
                if (ReadOnlyInternal) {
#pragma warning disable 618
                    IHtmlControlDesignerBehavior behavior = BehaviorInternal; 
                    if (behavior != null) {
                        Debug.Assert(behavior is IControlDesignerBehavior, "Unexpected type of behavior for custom control"); 
                        ((IControlDesignerBehavior)behavior).DesignTimeHtml = GetDesignTimeHtml(); 
                    }
#pragma warning restore 618 
                }
            }
        }
 
        private void UpdateExpressionValues(IComponent target) {
            IExpressionsAccessor expressionsAccessor = target as IExpressionsAccessor; 
 
            TemplateControl templateControl = null;
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            if (host != null) {
                templateControl = host.RootComponent as TemplateControl;
                Debug.Assert(host.RootComponent == null || templateControl != null, "Expected IDesignerHost.RootComponent to be either null or a valid TemplateControl");
            } 

            // Evaulate all expressions that are currently set 
            foreach (ExpressionBinding eb in expressionsAccessor.Expressions) { 
                if (!eb.Generated) {
                    string propName = eb.PropertyName; 
                    object realTarget;
                    PropertyDescriptor propDesc = GetComplexProperty(target, propName, out realTarget);
                    if (propDesc != null) {
                        IgnoreComponentChanges(true); 
                        try {
                            ExpressionEditor editor = ExpressionEditor.GetExpressionEditor(eb.ExpressionPrefix, target.Site); 
                            if (editor != null) { 
                                // Ensure that parsed expression data is available to the ExpressionEditor
                                object parsedData = EnsureParsedExpression(templateControl, eb, eb.ParsedExpressionData); 

                                object value = editor.EvaluateExpression(eb.Expression, parsedData, propDesc.PropertyType, target.Site);
                                if (value != null) {
                                    if (value is string) { 
                                        TypeConverter converter = propDesc.Converter;
                                        if (converter != null && converter.CanConvertFrom(typeof(string))) { 
                                            value = converter.ConvertFromInvariantString((string)value); 
                                        }
                                    } 
                                    // If we actually got a value from the expression editor, try to apply it to the property
                                    propDesc.SetValue(realTarget, value);
                                }
                                else { 
                                    // If we didn't get a value for the expression, just show the expression
                                    propDesc.SetValue(realTarget, SR.GetString(SR.ExpressionEditor_ExpressionBound, eb.Expression)); 
                                } 
                            }
                            else { 
                                // If we couldn't even find an expression editor, also just show the expression
                                propDesc.SetValue(realTarget, SR.GetString(SR.ExpressionEditor_ExpressionBound, eb.Expression));
                            }
                        } 
                        catch {
                            // There are some legitimate cases where an expression failed to evaluate at design time, or 
                            // is otherwise invalid for the property type, so we don't want to blow up when that happens. 
                        }
                        finally { 
                            IgnoreComponentChanges(false);
                        }
                    }
                } 
            }
            _expressionsChanged = false; 
        } 

        // Called by controls that only show an editable region when the template on the Component 
        // is non-null.
        internal bool UseRegions(DesignerRegionCollection regions, ITemplate componentTemplate) {
            bool useRegions = UseRegionsCore(regions) && (componentTemplate != null);
            return useRegions; 
        }
 
        // Called by controls that always show an editable region, even in the template on the Component 
        // is null.
        internal bool UseRegions(DesignerRegionCollection regions, ITemplate componentTemplate, 
                                 ITemplate viewControlTemplate) {
            // If the template on the Component is null, but the template on the ViewControl is
            // not null, then the template must be coming from the skin.
            bool templateDefinedInSkin = (componentTemplate == null && viewControlTemplate != null); 

            // Do not use an editable region if the template is defined in the skin (VSWhidbey 468562) 
            bool useRegions = UseRegionsCore(regions) && !templateDefinedInSkin; 

            return useRegions; 
        }

        private bool UseRegionsCore(DesignerRegionCollection regions) {
            bool useRegionsCore = (regions != null && View != null && View.SupportsRegions); 
            return useRegionsCore;
        } 
 
        internal static void VerifyInitializeArgument(IComponent component, Type expectedType) {
            if (!expectedType.IsInstanceOfType(component)) { 
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                    SR.GetString(SR.ControlDesigner_ArgumentMustBeOfType), expectedType.FullName), "component");
            }
        } 

        internal class ControlDesignerActionList : DesignerActionList { 
            private ControlDesigner _parent; 

            public ControlDesignerActionList(ControlDesigner parent) : base(parent.Component) { 
                _parent = parent;
            }

            public override bool AutoShow { 
                get {
                    return true; 
                } 
                set {
                } 
            }

            /// <devdoc>
            /// Transacted change callback to invoke the DataBindings dialog. 
            /// </devdoc>
            private bool DataBindingsCallback(object context) { 
                WebUIControl control = (WebUIControl)_parent.Component; 
                ISite site = control.Site;
                DataBindingsDialog dlg = new DataBindingsDialog(site, control); 
                DialogResult result = UIServiceHelper.ShowDialog(site, dlg);
                return (result == DialogResult.OK);
            }
 
            public void EditDataBindings() {
                InvokeTransactedChange(_parent.Component, new TransactedChangeCallback(DataBindingsCallback), null, SR.GetString(SR.Designer_DataBindingsVerb)); 
                _parent.UpdateDesignTimeHtml(); 
            }
 
            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                if (_parent.SupportsDataBindings && _parent.DataBindingsEnabled) {
                    items.Add(new DesignerActionMethodItem(this, "EditDataBindings", SR.GetString(SR.Designer_DataBindingsVerb), String.Empty, SR.GetString(SR.Designer_DataBindingsVerbDesc), true)); 
                }
 
                return items; 
            }
        } 

        internal delegate DesignerAutoFormat CreateAutoFormatDelegate(DataRow schemeData);
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using Microsoft.Win32;
    using System; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Configuration;
    using System.Data; 
    using System.Design; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.IO;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.InteropServices; 
    using System.Text;
    using System.Web.Configuration; 
    using System.Web.Compilation; 
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.Design.WebControls;
    using System.Web.UI.WebControls;
    using System.Windows.Forms; 
    using System.Xml;
 
    using WebUIControl = System.Web.UI.Control; 

    /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner"]/*' /> 
    /// <devdoc>
    ///    <para>
    ///       Provides the base class for all namespaced or custom server control designers.
    ///    </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class ControlDesigner : HtmlControlDesigner { 

        internal static readonly string ErrorDesignTimeHtmlTemplate = 
            @"<table cellpadding=""4"" cellspacing=""0"" style=""font: messagebox; color: buttontext; background-color: buttonface; border: solid 1px; border-top-color: buttonhighlight; border-left-color: buttonhighlight; border-bottom-color: buttonshadow; border-right-color: buttonshadow"">
                <tr><td nowrap><span style=""font-weight: bold; color: red"">{0}</span> - {1}</td></tr>
                <tr><td>{2}</td></tr>
              </table>"; 

        private static readonly string PlaceHolderDesignTimeHtmlTemplate = 
            @"<table cellpadding=4 cellspacing=0 style=""font:messagebox;color:buttontext;background-color:buttonface;border: solid 1px;border-top-color:buttonhighlight;border-left-color:buttonhighlight;border-bottom-color:buttonshadow;border-right-color:buttonshadow""> 
              <tr><td nowrap><span style=""font-weight:bold"">{0}</span> - {1}</td></tr>
              <tr><td>{2}</td></tr> 
            </table>";

        private bool isWebControl;           // true if the associated component is a WebControl
        private bool readOnly = true;        // read-only/read-write state of the control design surface. 
        private bool fDirty = false;         // indicates the dirty state of the control (used during inner content saving).
        private int _ignoreComponentChangesCount; 
 
        private bool _inTemplateMode;
 
        private WebUIControl _viewControl;
        private bool _viewControlCreated = false;

        private IControlDesignerTag _tag; 
        private IControlDesignerView _view;
 
        private ControlDesignerState _designerState; 

        private bool _expressionsChanged; 

        // Contains localized inner contents of the control after Localize() is called.
        private string _localizedInnerContent;
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists { 
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new ControlDesignerActionList(this));

                return actionLists;
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.AllowResize"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets or sets a value indicating
        ///       whether or not the control can be resized.
        ///    </para>
        /// </devdoc> 
        public virtual bool AllowResize {
            get { 
                return IsWebControl; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.AutoFormats"]/*' />
        public virtual DesignerAutoFormatCollection AutoFormats {
            get { 
                return new DesignerAutoFormatCollection();
            } 
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DataBindingsEnabled"]/*' /> 
        protected virtual bool DataBindingsEnabled {
            get {
                IControlDesignerView view = View;
                while (view != null) { 
                    EditableDesignerRegion region = (EditableDesignerRegion)view.ContainingRegion;
                    if (region != null) { 
                        if (region.SupportsDataBinding) { 
                            return true;
                        } 
                        else {
                            ControlDesigner containingControlDesigner = region.Designer;
                            if (containingControlDesigner != null) {
                                view = containingControlDesigner.View; 
                            }
                            else { 
                                return false; 
                            }
                        } 
                    }
                    else {
                        return false;
                    } 
                }
 
                return false; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignerState"]/*' />
        protected ControlDesignerState DesignerState {
            get { 
                if (_designerState == null) {
                    _designerState = new ControlDesignerState(Component); 
                } 
                return _designerState;
            } 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignTimeHtmlRequiresLoadComplete"]/*' />
        [Obsolete("The recommended alternative is SetViewFlags(ViewFlags.DesignTimeHtmlRequiresLoadComplete, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public virtual bool DesignTimeHtmlRequiresLoadComplete {
            get { 
                return false; 
            }
        } 

        /// <devdoc>
        ///    <para>
        ///      Whether or not the properties of the control will be hidden when the control 
        ///      is placed into template editing mode. The 'ID' property is never hidden.
        ///      The default implementation returns 'true.' 
        ///    </para> 
        /// </devdoc>
        protected internal virtual bool HidePropertiesInTemplateMode { 
            get {
                return true;
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ID"]/*' /> 
        /// <devdoc> 
        ///
        /// </devdoc> 
        public virtual string ID {
            get {
                return ((WebUIControl)Component).ID;
            } 

            set { 
                if (RootDesigner != null) { 
                    RootDesigner.SetControlID((WebUIControl) Component, value);
                } 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InTemplateMode"]/*' /> 
        protected bool InTemplateMode {
            get { 
                return _inTemplateMode; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.IsDirty"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets or sets a value indicating
        ///       whether or not the boolean dirty state of the web control is currently set. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("The recommended alternative is to use Tag.SetDirty() and Tag.IsDirty. http://go.microsoft.com/fwlink/?linkid=14202")] 
        public bool IsDirty {
            get {
                return IsDirtyInternal;
            } 
            set {
                IsDirtyInternal = value; 
            } 
        }
 
        internal bool IsDirtyInternal {
            get {
                if (Tag != null) {
                    return Tag.IsDirty; 
                }
 
                return fDirty; 
            }
            set { 
                if (Tag != null) {
                    Tag.SetDirty(value);
                }
                else { 
                    fDirty = value;
                } 
            } 
        }
 
        internal bool IsIgnoringComponentChanges {
            get {
                return _ignoreComponentChangesCount > 0;
            } 
        }
 
        internal bool IsWebControl { 
            get {
                return isWebControl; 
            }
        }

        // This is internal so that ContainerControlDesigner can have access to it 
        // because of its overridden implementation of GetPersistenceContent().
        internal string LocalizedInnerContent { 
            get { 
                return _localizedInnerContent;
            } 
        }

        public virtual bool ViewControlCreated {
            get { 
                return _viewControlCreated;
            } 
            set { 
                _viewControlCreated = value;
            } 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.ReadOnly"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets or sets a value indicating 
        ///       whether or not the control's associated design surface is set to read-only or not. 
        ///    </para>
        /// </devdoc> 
        [Obsolete("The recommended alternative is to inherit from ContainerControlDesigner instead and to use an EditableDesignerRegion. Regions allow for better control of the content in the designer. http://go.microsoft.com/fwlink/?linkid=14202")]
        public bool ReadOnly {
            get {
                return ReadOnlyInternal; 
            }
            set { 
                ReadOnlyInternal = value; 
            }
        } 

        internal bool ReadOnlyInternal {
            get {
                return readOnly; 
            }
            set { 
                readOnly = value; 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.RootDesigner"]/*' />
        protected WebFormsRootDesigner RootDesigner {
            get { 
                WebFormsRootDesigner rootDesigner = null;
 
                ISite site = Component.Site; 
                if (site != null) {
                    IDesignerHost designerHost = (IDesignerHost)site.GetService(typeof(IDesignerHost)); 
                    if ((designerHost != null) && (designerHost.RootComponent != null)) {
                        rootDesigner = designerHost.GetDesigner(designerHost.RootComponent) as WebFormsRootDesigner;
                    }
                } 

                return rootDesigner; 
            } 
        }
 
        private bool SupportsDataBindings {
            get {
                BindableAttribute ba = (BindableAttribute)TypeDescriptor.GetAttributes(Component)[typeof(BindableAttribute)];
                return ((ba != null) && ba.Bindable); 
            }
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Tag"]/*' />
        /// <devdoc> 
        /// </devdoc>
        protected IControlDesignerTag Tag {
            get {
                return _tag; 
            }
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.TemplateGroups"]/*' />
        public virtual TemplateGroupCollection TemplateGroups { 
            get {
                return new TemplateGroupCollection();
            }
        } 

        // 
        protected virtual bool UsePreviewControl { 
            get {
                object[] attrs = this.GetType().GetCustomAttributes(typeof(SupportsPreviewControlAttribute), false); 
                if (attrs.Length > 0) {
                    SupportsPreviewControlAttribute spca = (SupportsPreviewControlAttribute)attrs[0];
                    return spca.SupportsPreviewControl;
                } 

                return false; 
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.View"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal IControlDesignerView View { 
            get {
                return _view; 
            } 
        }
 
        public WebUIControl ViewControl {
            get {
                if (!ViewControlCreated) {
                    _viewControl = UsePreviewControl ? CreateViewControlInternal() : (WebUIControl)Component; 
                    ViewControlCreated = true;
                } 
 
                return _viewControl;
            } 
            set {
                _viewControl = value;
                ViewControlCreated = true;
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Visible"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual bool Visible {
            get {
                return true;
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.DesignTimeElementView"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        ///   <para>The object on the design surface used to display the visual representation of the control associated with this designer.</para>
        /// </devdoc>
        [Obsolete("Error: This property can no longer be referenced, and is included to support existing compiled applications. The design-time element view architecture is no longer used. http://go.microsoft.com/fwlink/?linkid=14202", true)]
        protected object DesignTimeElementView { 
            get {
                IHtmlControlDesignerBehavior behavior = BehaviorInternal; 
                if (behavior != null) { 
                    Debug.Assert(behavior is IControlDesignerBehavior, "Wrong type of behavior");
 
                    return ((IControlDesignerBehavior)behavior).DesignTimeElementView;
                }
                return null;
            } 
        }
 
        internal static DesignerAutoFormatCollection CreateAutoFormats(string schemes, 
                                                                       CreateAutoFormatDelegate createAutoFormatDelegate) {
            DesignerAutoFormatCollection autoFormats = new DesignerAutoFormatCollection(); 
            try {
                DataSet ds = new DataSet();
                ds.Locale = CultureInfo.InvariantCulture;
                ds.ReadXml(new XmlTextReader(new StringReader(schemes))); 

                Debug.Assert((ds.Tables.Count == 1) && (ds.Tables[0].TableName.Equals("Scheme")), 
                            "Unexpected tables in schemes dataset"); 

                DataTable schemesTable = ds.Tables[0]; 
                schemesTable.PrimaryKey = new DataColumn[] {schemesTable.Columns["SchemeName"]};

                for (int i = 0; i < schemesTable.Rows.Count; i++) {
                    autoFormats.Add(createAutoFormatDelegate(schemesTable.Rows[i])); 
                }
            } 
            catch (Exception e) { 
                Debug.Fail(e.ToString());
            } 
            return autoFormats;
        }

        /// <devdoc> 
        /// Creates a clone of a control based on its persistence. It either gets the outer
        /// content persistence straight from the tool (best bet), or it tries to simply 
        /// serialize the control itself (not always reliable). 
        /// </devdoc>
        internal WebUIControl CreateClonedControl(IDesignerHost parseTimeDesignerHost, bool applyTheme) { 
            string persisted = null;
            if (Tag != null) {
                // As a perf optimization we ask the tag for the outer content
                persisted = Tag.GetOuterContent(); 
            }
            // If we didn't get back outer content, we fall back to the ControlSerializer 
            if (String.IsNullOrEmpty(persisted)) { 
                persisted = ControlPersister.PersistControl((WebUIControl)Component);
            } 
            WebUIControl clonedControl = ControlParser.ParseControl(parseTimeDesignerHost, persisted, applyTheme);
            return clonedControl;
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.CreatePlaceHolderDesignTimeHtml"]/*' />
        protected string CreatePlaceHolderDesignTimeHtml() { 
            return CreatePlaceHolderDesignTimeHtml(null); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.CreatePlaceHolderDesignTimeHtml1"]/*' />
        protected string CreatePlaceHolderDesignTimeHtml(string instruction) {
            string typeName = Component.GetType().Name;
            string name = Component.Site.Name; 

            if (instruction == null) { 
                instruction = String.Empty; 
            }
 
            return String.Format(CultureInfo.InvariantCulture, PlaceHolderDesignTimeHtmlTemplate, typeName, name, instruction);
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.CreateErrorDesignTimeHtml"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected string CreateErrorDesignTimeHtml(string errorMessage) { 
            return CreateErrorDesignTimeHtml(errorMessage, null);
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.CreateErrorDesignTimeHtml1"]/*' />
        /// <devdoc>
        /// </devdoc> 
        protected string CreateErrorDesignTimeHtml(string errorMessage, Exception e) {
            return CreateErrorDesignTimeHtml(errorMessage, e, Component); 
        } 

        internal static string CreateErrorDesignTimeHtml(string errorMessage, Exception e, IComponent component) { 
            Debug.Assert(component != null);

            string name = component.Site.Name;
 
            if (errorMessage == null) {
                errorMessage = String.Empty; 
            } 
            else {
                errorMessage = HttpUtility.HtmlEncode(errorMessage); 
            }

            if (e != null) {
                errorMessage += "<br />" + HttpUtility.HtmlEncode(e.Message); 
            }
 
            return String.Format(CultureInfo.InvariantCulture, ErrorDesignTimeHtmlTemplate, SR.GetString(SR.ControlDesigner_DesignTimeHtmlError), HttpUtility.HtmlEncode(name), errorMessage); 
        }
 
        internal string CreateInvalidParentDesignTimeHtml(Type controlType, Type requiredParentType) {
            return CreateErrorDesignTimeHtml(SR.GetString(SR.Control_CanOnlyBePlacedInside,
                controlType.Name, requiredParentType.Name));
        } 

        private WebUIControl CreateViewControlInternal() { 
            Debug.Assert(Component is WebUIControl); 
            WebUIControl originalControl = (WebUIControl)Component;
            WebUIControl viewControl = CreateViewControl(); 
            ((IControlDesignerAccessor)viewControl).SetOwnerControl(originalControl);
            UpdateExpressionValues(viewControl);

            return viewControl; 
        }
 
        protected virtual WebUIControl CreateViewControl() { 
            return CreateClonedControl((IDesignerHost)GetService(typeof(IDesignerHost)), true);
        } 

        /// <devdoc>
        /// Ensures that an expression is fully parsed. In some cases expression data is left
        /// in its original form, but the contract with ExpressionEditors is that they only get 
        /// parsed data.
        /// </devdoc> 
        private object EnsureParsedExpression(TemplateControl templateControl, ExpressionBinding eb, object parsedData) { 
            if (parsedData == null) {
                // No parsed data, try to re-parse 
                if (templateControl != null) {
                    string trueExpressionPrefix;
                    Type expressionBuilderType = ExpressionEditor.GetExpressionBuilderType(eb.ExpressionPrefix, Component.Site, out trueExpressionPrefix);
                    if (expressionBuilderType != null) { 
                        try {
                            System.Web.Compilation.ExpressionBuilder expressionBuilder = (System.Web.Compilation.ExpressionBuilder)Activator.CreateInstance(expressionBuilderType); 
                            ExpressionBuilderContext ec = new ExpressionBuilderContext(templateControl); 
                            parsedData = expressionBuilder.ParseExpression(eb.Expression, eb.PropertyType, ec);
                        } 
                        catch (Exception ex) {
                            // We basically ignore exceptions coming from the ExpressionBuilder
                            // since the page developer is not likely going to be able to do
                            // anything about it. 
                            Debug.Fail(String.Format(CultureInfo.InvariantCulture, "Exception when instantiating ExpressionBuilder or parsing expression\r\n\r\n{0}", ex.ToString()));
                            IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)GetService(typeof(IComponentDesignerDebugService)); 
                            if (debugService != null) { 
                                debugService.Fail(SR.GetString(SR.ControlDesigner_CouldNotGetExpressionBuilder, eb.ExpressionPrefix, ex.Message));
                            } 
                        }
                    }
                }
            } 
            return parsedData;
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetBounds"]/*' />
        public Rectangle GetBounds() { 
            if (View != null) {
                return View.GetBounds(null);
            }
            return Rectangle.Empty; 
        }
 
        /// <devdoc> 
        /// Parses a complex property expression and retrieves the correct PropertyDescriptor
        /// and also returns the object that the PropertyDescriptor appears on. 
        /// </devdoc>
        internal static PropertyDescriptor GetComplexProperty(object target, string propName, out object realTarget) {
            realTarget = null;
            string[] propNameParts = propName.Split('.'); 
            PropertyDescriptor currentPropDesc = null;
 
            foreach (string part in propNameParts) { 
                if (String.IsNullOrEmpty(part)) {
                    return null; 
                }
                currentPropDesc = TypeDescriptor.GetProperties(target)[part];
                if (currentPropDesc == null) {
                    return null; 
                }
                realTarget = target; 
                target = currentPropDesc.GetValue(target); 
            }
            return currentPropDesc; 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetDesignTimeHtml"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the HTML to be used for the design time representation of the control runtime. 
        ///    </para> 
        /// </devdoc>
        public virtual string GetDesignTimeHtml() { 
            StringWriter strWriter = new StringWriter(CultureInfo.InvariantCulture);
            DesignTimeHtmlTextWriter htmlWriter = new DesignTimeHtmlTextWriter(strWriter);
            string designTimeHTML = null;
 
            bool restoreVisible = false;
            bool oldVisible = true; 
 
            WebUIControl control = null;
            try { 
                control = ViewControl;
                oldVisible = control.Visible;
                if (oldVisible == false) {
                    control.Visible = true; 
                    restoreVisible = !UsePreviewControl;
                } 
 
                control.RenderControl(htmlWriter);
                designTimeHTML = strWriter.ToString(); 
            }
            catch (Exception ex) {
                designTimeHTML = GetErrorDesignTimeHtml(ex);
            } 
            finally {
                if (restoreVisible) { 
                    control.Visible = oldVisible; 
                }
            } 

            if ((designTimeHTML == null) || (designTimeHTML.Length == 0)) {
                designTimeHTML = GetEmptyDesignTimeHtml();
            } 

            return designTimeHTML; 
        } 

        public virtual string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            return GetDesignTimeHtml();
        }

        /// <devdoc> 
        /// Gets a factory for creating design-time resource providers and writers.
        /// This looks in config for the runtime factory type, and from that gets 
        /// the design time factory type. If none is found, a service is used to get 
        /// the tool's design time resource provider.
        /// </devdoc> 
        public static DesignTimeResourceProviderFactory GetDesignTimeResourceProviderFactory(IServiceProvider serviceProvider) {
            const string GlobalizationSectionName = "system.web/globalization";

            DesignTimeResourceProviderFactory resourceProviderFactory = null; 

            IWebApplication webApp = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication)); 
            Configuration config = null; 
            if (webApp != null) {
                config = webApp.OpenWebConfiguration(true); 
                if (config != null) {
                    GlobalizationSection globConfig = config.GetSection(GlobalizationSectionName) as GlobalizationSection;
                    if (globConfig != null) {
                        string providerTypeName = globConfig.ResourceProviderFactoryType; 
                        if (!String.IsNullOrEmpty(providerTypeName)) {
                            ITypeResolutionService typeResolver = (ITypeResolutionService)serviceProvider.GetService(typeof(ITypeResolutionService)); 
                            if (typeResolver != null) { 
                                Type providerType = typeResolver.GetType(providerTypeName, true, true);
                                if (providerType != null) { 
                                    object[] attrs = providerType.GetCustomAttributes(typeof(DesignTimeResourceProviderFactoryAttribute), true);
                                    if (attrs != null && attrs.Length > 0) {
                                        DesignTimeResourceProviderFactoryAttribute factoryAttr = attrs[0] as DesignTimeResourceProviderFactoryAttribute;
                                        string designTimeProviderTypeName = factoryAttr.FactoryTypeName; 
                                        if (!String.IsNullOrEmpty(designTimeProviderTypeName)) {
                                            Type designTimeProviderType = typeResolver.GetType(designTimeProviderTypeName, true, true); 
                                            if (designTimeProviderType != null && typeof(DesignTimeResourceProviderFactory).IsAssignableFrom(designTimeProviderType)) { 
                                                try {
                                                    resourceProviderFactory = (DesignTimeResourceProviderFactory)Activator.CreateInstance(designTimeProviderType); 
                                                }
                                                catch (Exception ex) {
                                                    // We basically ignore exceptions coming from the DesignTimeResourceProviderFactory
                                                    // since the page developer is not likely going to be able to do anything about it. 
                                                    Debug.Fail(String.Format(CultureInfo.InvariantCulture, "Exception when instantiating DesignTimeResourceProviderFactory\r\n\r\n{0}", ex.ToString()));
                                                    if (serviceProvider != null) { 
                                                        IComponentDesignerDebugService debugService = (IComponentDesignerDebugService)serviceProvider.GetService(typeof(IComponentDesignerDebugService)); 
                                                        if (debugService != null) {
                                                            debugService.Fail(SR.GetString(SR.ControlDesigner_CouldNotGetDesignTimeResourceProviderFactory, designTimeProviderTypeName, ex.Message)); 
                                                        }
                                                    }
                                                }
                                            } 
                                        }
                                    } 
                                } 
                            }
                        } 
                    }
                }
            }
 
            if (resourceProviderFactory == null) {
                IDesignTimeResourceProviderFactoryService resService = (IDesignTimeResourceProviderFactoryService)serviceProvider.GetService(typeof(IDesignTimeResourceProviderFactoryService)); 
                if (resService != null) { 
                    resourceProviderFactory = resService.GetFactory();
                } 
            }

            return resourceProviderFactory;
        } 

        public virtual string GetEditableDesignerRegionContent(EditableDesignerRegion region) { 
            return String.Empty; 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetEmptyDesignTimeHtml"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the HTML to be used at design time as the representation of the 
        ///       control when the control runtime does not return any rendered
        ///       HTML. The default behavior is to return a string containing the name of the 
        ///       component. 
        ///    </para>
        /// </devdoc> 
        protected virtual string GetEmptyDesignTimeHtml() {
            string typeName = Component.GetType().Name;
            string name = Component.Site.Name;
 
            if ((name != null) && (name.Length > 0)) {
                return "[ " + typeName + " \"" + name + "\" ]"; 
            } 
            else {
                return "[ " + typeName + " ]"; 
            }
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetErrorDesignTimeHtml"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual string GetErrorDesignTimeHtml(Exception e) { 
            return CreateErrorDesignTimeHtml(SR.GetString(SR.ControlDesigner_UnhandledException), e);
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetPersistInnerHtml"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets the persistable inner HTML.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("The recommended alternative is GetPersistenceContent(). http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetPersistInnerHtml() { 
            return GetPersistInnerHtmlInternal();
        }

        internal virtual string GetPersistInnerHtmlInternal() { 
            if (_localizedInnerContent != null) {
                return _localizedInnerContent; 
            } 

            if (!IsDirtyInternal) { 
                // NOTE: Returning a null string will prevent the actual save.
                return null;
            }
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(host != null, "Did not get a valid IDesignerHost reference"); 
 
            IsDirtyInternal = false;
 
            return ControlSerializer.SerializeInnerContents((WebUIControl)Component, host);
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetPersistenceContent"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public virtual string GetPersistenceContent() { 
#pragma warning disable 618
            return GetPersistInnerHtml(); 
#pragma warning restore 618
        }

        internal void HideAllPropertiesExceptID(IDictionary properties) { 
            ICollection coll = properties.Values;
            if (coll != null) { 
                object[] values = new object[coll.Count]; 
                coll.CopyTo(values, 0);
 
                for (int i = 0; i < values.Length; i++) {
                    PropertyDescriptor prop = (PropertyDescriptor)values[i];
                    if (prop != null) {
                        if (!String.Equals(prop.Name, "ID", StringComparison.OrdinalIgnoreCase)) { 
                            properties[prop.Name] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, BrowsableAttribute.No);
                        } 
                    } 
                }
            } 
        }

        public void Localize(IDesignTimeResourceWriter resourceWriter) {
            OnComponentChanging(Component, new ComponentChangingEventArgs(Component, null)); 

            string newInnerContent; 
 
            string resourceKey = ControlLocalizer.LocalizeControl((WebUIControl)Component, resourceWriter, out newInnerContent);
 
            if (!String.IsNullOrEmpty(resourceKey)) {
                // Add the resource key attribute to the top-level object, if present
                SetTagAttribute("meta:resourcekey", resourceKey, true);
            } 
            if (!String.IsNullOrEmpty(newInnerContent)) {
                // Regardless of whether we had a resource key for the top-level object, we 
                // might still have new inner content. For example, a MultiView doesn't have 
                // any localizable properties, so it doesn't get a meta:resourcekey tag.
                // However, controls inside it may have been localized, so we need to get 
                // new inner content for them.
                _localizedInnerContent = newInnerContent;
            }
 
            OnComponentChanged(Component, new ComponentChangedEventArgs(Component, null, null, null));
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetViewRendering"]/*' />
        public static ViewRendering GetViewRendering(System.Web.UI.Control control) { 
            ControlDesigner designer = null;

            ISite site = control.Site;
            if (site != null) { 
                IDesignerHost host = (IDesignerHost)site.GetService(typeof(IDesignerHost));
                Debug.Assert(host != null, "Did not get a valid IDesignerHost reference"); 
 
                designer = host.GetDesigner(control) as ControlDesigner;
            } 

            return GetViewRendering(designer);
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetViewRendering1"]/*' />
        public static ViewRendering GetViewRendering(ControlDesigner designer) { 
            string designTimeHtml = String.Empty; 
            DesignerRegionCollection regions = new DesignerRegionCollection();
            bool visible = true; 

            if (designer != null) {
                bool supportsRegions = false;
                if (designer.View != null) { 
                    supportsRegions = designer.View.SupportsRegions;
                } 
 
                try {
                    // clear the preview control so it will be current when we call GetDesignTimeHtml 
                    designer.ViewControlCreated = false;

                    // Otherwise, just get the view rendering for this control
                    if (supportsRegions) { 
                        designTimeHtml = designer.GetDesignTimeHtml(regions);
                    } 
                    else { 
                        designTimeHtml = designer.GetDesignTimeHtml();
                    } 

                    // Get Visible property after calling GetDesignTimeHtml(), so visible will still be true
                    // if GetDesignTimeHtml() throws an exception.
                    visible = designer.Visible; 
                }
                catch (Exception ex) { 
                    // If an exception was thrown, create an error block for it 
                    regions.Clear();
                    try { 
                        designTimeHtml = designer.GetErrorDesignTimeHtml(ex);
                    }
                    catch (Exception ex2) {
                        // If generating the designer's custom error block threw, 
                        // create a default error block
                        designTimeHtml = designer.CreateErrorDesignTimeHtml(ex2.Message); 
                    } 

                    // Ensure View is always visible if there was an error. 
                    visible = true;
                }
            }
 
            return new ViewRendering(designTimeHtml, regions, visible);
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetViewRendering2"]/*' />
        public ViewRendering GetViewRendering() { 
            ViewRendering viewRendering = null;


            EditableDesignerRegion containingRegion = null; 
            if (View != null) {
                containingRegion = View.ContainingRegion as EditableDesignerRegion; 
            } 

            if (containingRegion != null) { 
                // Call the containing editable region to get the view rendering
                viewRendering = ((EditableDesignerRegion)containingRegion).GetChildViewRendering((WebUIControl)this.Component);
            }
            else { 
                viewRendering = ControlDesigner.GetViewRendering(this);
            } 
 
            return viewRendering;
        } 

        private void IgnoreComponentChanges(bool ignore) {
            _ignoreComponentChangesCount += (ignore ? 1 : -1);
            Debug.Assert(_ignoreComponentChangesCount >= 0, "Ignore count should always be non-negative"); 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Initialize"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes the designer using
        ///       the specified component.
        ///    </para>
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(WebUIControl)); 
            base.Initialize(component); 

            if (RootDesigner != null) { 
                RootDesigner.GetControlViewAndTag((WebUIControl)Component, out _view, out _tag);
                if (_view != null) {
                    _view.ViewEvent += new ViewEventHandler(OnViewEvent);
                } 
            }
 
            Expressions.Changed += new EventHandler(OnExpressionsChanged); 

            isWebControl = (component is WebControl); 

            UpdateExpressionValues(component);
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Invalidate"]/*' />
        public void Invalidate() { 
            if (View != null) { 
                Invalidate(View.GetBounds(null));
            } 
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.Invalidate1"]/*' />
        public void Invalidate(Rectangle rectangle) { 
            if (View != null) {
                View.Invalidate(rectangle); 
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InvokeTransactedChange"]/*' />
        public static void InvokeTransactedChange(IComponent component, TransactedChangeCallback callback, object context, string description) {
            InvokeTransactedChange(component, callback, context, description, null);
        } 

        public static void InvokeTransactedChange(IComponent component, TransactedChangeCallback callback, object context, string description, MemberDescriptor member) { 
            if (component == null) { 
                throw new ArgumentNullException("component");
            } 
            InvokeTransactedChange(component.Site, component, callback, context, description, member);
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.InvokeTransactedChange1"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public static void InvokeTransactedChange(IServiceProvider serviceProvider, IComponent component, TransactedChangeCallback callback, object context, string description, MemberDescriptor member) { 
            if (component == null) {
                throw new ArgumentNullException("component"); 
            }
            if (callback == null) {
                throw new ArgumentNullException("callback");
            } 
            if (serviceProvider == null) {
                throw new ArgumentException(SR.GetString(SR.ControlDesigner_TransactedChangeRequiresServiceProvider), "serviceProvider"); 
            } 

            IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null);

            using (DesignerTransaction transaction = designerHost.CreateTransaction(description)) {
                IComponentChangeService changeService = (IComponentChangeService)serviceProvider.GetService(typeof(IComponentChangeService)); 

                if (changeService != null) { 
                    try { 
                        changeService.OnComponentChanging(component, member);
                    } 
                    catch (CheckoutException ce) {
                        if (ce == CheckoutException.Canceled) {
                            // This will exit the "using" statement and the transaction will be cancelled
                            return; 
                        }
                        throw ce; 
                    } 
                }
 
                ControlDesigner controlDesigner = designerHost.GetDesigner(component) as ControlDesigner;
                bool unIgnored = false; // This makes sure we unignore only once
                try {
                    if (controlDesigner != null) { 
                        controlDesigner.IgnoreComponentChanges(true);
                    } 
                    if (callback(context)) { 
                        if (controlDesigner != null) {
                            unIgnored = true; 
                            controlDesigner.IgnoreComponentChanges(false);
                        }
                        if (changeService != null) {
                            changeService.OnComponentChanged(component, member, null, null); 
                        }
 
                        TypeDescriptor.Refresh(component); 

                        transaction.Commit(); 
                    }
                }
                finally {
                    if (controlDesigner != null && !unIgnored) { 
                        controlDesigner.IgnoreComponentChanges(false);
                    } 
                } 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.IsPropertyBound"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets a value indicating whether a particular property (identified by its name) is data bound.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("The recommended alternative is DataBindings.Contains(string). The DataBindings collection allows more control of the databindings associated with the control. http://go.microsoft.com/fwlink/?linkid=14202")]
        public bool IsPropertyBound(string propName) { 
            return (DataBindings[propName] != null);
        }

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnAutoFormatApplied"]/*' /> 
        /// <devdoc>
        /// Called when an autoformat has been applied to the control. 
        /// A designer may override this to inspect the control's properties, or 
        /// to take further action.
        /// </devdoc> 
        public virtual void OnAutoFormatApplied(DesignerAutoFormat appliedAutoFormat) {
        }

        private static readonly Attribute[] emptyAttrs = new Attribute[0]; 
        private static readonly Attribute[] nonBrowsableAttrs = new Attribute[] { BrowsableAttribute.No };
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);

            PropertyDescriptor prop; 

            // Handle shadowed properties 
            prop = (PropertyDescriptor)properties["ID"]; 
            if (prop != null) {
                properties["ID"] = TypeDescriptor.CreateProperty(GetType(), prop, emptyAttrs); 
            }

            prop = (PropertyDescriptor)properties["SkinID"];
            if (prop != null) { 
                properties["SkinID"] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, new TypeConverterAttribute(typeof(SkinIDTypeConverter)));
            } 
 
            if (InTemplateMode) {
                // If we are in template editing mode, we optionally hide all 
                // properties except for the ID property. We always make the ID
                // property readonly in template editing mode.

                if (HidePropertiesInTemplateMode) { 
                    HideAllPropertiesExceptID(properties);
                } 
 
                prop = (PropertyDescriptor)properties["ID"];
                if (prop != null) { 
                    properties[prop.Name] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, ReadOnlyAttribute.Yes);
                }
            }
 
#if ORCAS
            IFilterResolutionService filterResolutionService = (IFilterResolutionService)Component.Site.GetService(typeof(IFilterResolutionService)); 
            bool filterSet = false; 
            if (filterResolutionService != null) {
                filterSet = (filterResolutionService.CurrentFilter.Length > 0) && !String.Equals(filterResolutionService.CurrentFilter, "default", StringComparison.InvariantCultureIgnoreCase); 
            }

            IDictionary replaceDictionary = new HybridDictionary(true);
            // Hide some properties and unfilterable properties if a filter is set 
            if (filterSet) {
                foreach (PropertyDescriptor pd in properties.Values) { 
                    if (!FilterableAttribute.IsPropertyFilterable(pd) || 
                        String.Equals(pd.Name, "DynamicProperties", StringComparison.InvariantCultureIgnoreCase)) {
                        replaceDictionary[pd.Name] = TypeDescriptor.CreateProperty(pd.ComponentType, pd, nonBrowsableAttrs); 
                    }
                }
            }
 
            foreach (DictionaryEntry entry in replaceDictionary) {
                properties[entry.Key] = entry.Value; 
            } 
#endif
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnBindingsCollectionChanged"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Delegate to
        ///       handle bindings collection changed event. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("The recommended alternative is to handle the Changed event on the DataBindings collection. The DataBindings collection allows more control of the databindings associated with the control. http://go.microsoft.com/fwlink/?linkid=14202")] 
        protected override void OnBindingsCollectionChanged(string propName) {
            // NOTE: This code is here strictly for backwards compatibility.
            // Some control designers did some funky things by adding and
            // removing DataBindings from within property setters. On top of 
            // this, those properties were marked with DesignerSerializationVisibility
            // set to Hidden. This code is from v1.x and just does the right thing 
            // for this rare and obscure case. 

            if (Tag == null) 
                return;

            DataBindingCollection bindings = DataBindings;
 
            if (propName != null) {
                DataBinding db = bindings[propName]; 
 
                string persistPropName = propName.Replace('.', '-');
 
                if (db == null) {
                    Tag.RemoveAttribute(persistPropName);
                }
                else { 
                    string bindingExpr = "<%# " + db.Expression + " %>";
                    Tag.SetAttribute(persistPropName, bindingExpr); 
 
                    if (persistPropName.IndexOf('-') < 0) {
                        // We only reset top-level properties to be consistent with 
                        // what we do the other way around, i.e., when a databound
                        // property value is set to some value
                        ResetPropertyValue(persistPropName, false);
                    } 
                }
            } 
            else { 
                string[] removedBindings = bindings.RemovedBindings;
                foreach (string s in removedBindings) { 
                    string persistPropName = s.Replace('.', '-');
                    Tag.RemoveAttribute(persistPropName);
                }
 
                foreach (DataBinding db in bindings) {
                    string bindingExpr = "<%# " + db.Expression + " %>"; 
                    string persistPropName = db.PropertyName.Replace('.', '-'); 

                    Tag.SetAttribute(persistPropName, bindingExpr); 
                    if (persistPropName.IndexOf('-') < 0) {
                        // We only reset top-level properties to be consistent with
                        // what we do the other way around, i.e., when a databound
                        // property value is set to some value 
                        ResetPropertyValue(persistPropName, false);
                    } 
                } 
            }
        } 

        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnClick"]/*' />
        /// <devdoc>
        /// </devdoc> 
        protected virtual void OnClick(DesignerRegionMouseEventArgs e) {
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnComponentChanged"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Delegate to handle component changed event.
        ///    </para>
        /// </devdoc> 
        public virtual void OnComponentChanged(object sender, ComponentChangedEventArgs ce) {
            if (IsIgnoringComponentChanges) { 
                return; 
            }
 
            IComponent component = Component;
            Debug.Assert(ce.Component == component, "ControlDesigner::OnComponentChanged - Called from an unknown/invalid source object");

            if (DesignTimeElementInternal == null) { 
                return;
            } 
 
            MemberDescriptor member = ce.Member;
 
            if (member != null) {
                //
                Type t = Type.GetType("System.ComponentModel.ReflectPropertyDescriptor, " + AssemblyRef.System);
 
                if ((member.GetType() != t) ||
                    (ce.NewValue != null && ce.NewValue == ce.OldValue)) { 
                    // HACK: ideally, we would prevent the property descriptor from firing this change. 
                    // This would tear large holes in the WFC architecture. Instead, we do the
                    // filtering ourselves in this evil fashion. 

                    Debug.WriteLineIf(CompModSwitches.UserControlDesigner.TraceInfo, "    ...ignoring property descriptor of type: " + member.GetType().Name);
                    return;
                } 

                if (((PropertyDescriptor)member).SerializationVisibility != DesignerSerializationVisibility.Hidden) { 
                    // Set the dirty state upon changing persistable properties. 
                    IsDirtyInternal = true;
 
                    PersistenceModeAttribute persistenceType = (PersistenceModeAttribute)member.Attributes[typeof(PersistenceModeAttribute)];
                    PersistenceMode mode = persistenceType.Mode;

                    if ((mode == PersistenceMode.Attribute) || 
                        (mode == PersistenceMode.InnerDefaultProperty) ||
                        (mode == PersistenceMode.EncodedInnerDefaultProperty)) { 
                        string propName = member.Name; 

                        // Check to see whether the property that was changed is data bound. 
                        // If it is we need to remove it...
                        // For this rev, we're only doing this for the properties on the Component itself
                        // as we can't distinguish which subproperty of a complex type changed.
                        if (ce.Component == Component) { 
                            if (DataBindings.Contains(propName)) {
                                DataBindings.Remove(propName, false); 
                                RemoveTagAttribute(propName, true); 
                            }
 
                            if (Expressions.Contains(propName)) {
                                ExpressionBinding eb = Expressions[propName];
                                if (!eb.Generated) {
                                    Expressions.Remove(propName, false); 
                                    RemoveTagAttribute(propName, true);
                                } 
 
                                // Always mark expressions as changed so that UpdateExpressionValues will be called.
                                // This is necessary because when a property's value is set, it gets set on 
                                // the component itself, and that overwrites the expression's design-time value,
                                // and we need to restore the expression's design-time value.
                                _expressionsChanged = true;
                            } 
                        }
 
                        // For tag level properties ... 
                        WebUIControl control = (WebUIControl)ce.Component;
 
                        IDesignerHost host = null;
                        if (control.Site != null) {
                            host = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost));
                        } 

                        Debug.Assert(host != null, "Need an IDesignerHost to persist properties"); 
                        if (host != null) { 
                            ArrayList attribs = ControlSerializer.GetControlPersistedAttribute(control, (PropertyDescriptor)member, host);
 
                            PersistAttributes(attribs);
                        }
                    }
                } 
            }
            else { 
                // member is null, meaning that the whole component 
                // could have changed and not just a single member.
                // This happens when a component is edited through a ComponentEditor. 

                // Set the dirty state if more than one property is changed.
                IsDirtyInternal = true;
 
                WebUIControl control = (WebUIControl)ce.Component;
 
                IDesignerHost host = null; 
                if (control.Site != null) {
                    host = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost)); 
                }

                // Reset all properties which used to have expression, but don't anymore
                // Do this before persisting attributes so the old expression-evaluated values 
                // don't get into the persistence
                foreach (string propName in Expressions.RemovedBindings) { 
                    object realTarget; 
                    PropertyDescriptor propDesc = GetComplexProperty(Component, propName, out realTarget);
                    if (propDesc != null) { 
                        IgnoreComponentChanges(true);
                        try {
                            propDesc.ResetValue(realTarget);
                        } 
                        finally {
                            IgnoreComponentChanges(false); 
                        } 
                    }
                } 

                Debug.Assert(host != null, "Need an IDesignerHost to persist properties");
                if (host != null) {
                    ArrayList attribs = ControlSerializer.GetControlPersistedAttributes(control, host); 

                    PersistAttributes(attribs); 
                } 

                foreach (DataBinding db in DataBindings) { 
                    if (db.PropertyName.IndexOf('.') < 0) {
                        // We only reset top-level properties to be consistent with
                        // what we do the other way around, i.e., when a databound
                        // property value is set to some value 
                        ResetPropertyValue(db.PropertyName, false);
                    } 
                } 

                OnBindingsCollectionChangedInternal(null); 

                // Since we don't know which property changed, we force re-evaluation of all expressions.
                // This is necessary because when a property's value is set, it gets set on
                // the component itself, and that overwrites the expression's design-time value, 
                // and we need to restore the expression's design-time value.
                _expressionsChanged = true; 
            } 

            if (_expressionsChanged) { 
                UpdateExpressionValues(Component);
            }

            // Update the HTML and verbs. 
            UpdateDesignTimeHtml();
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnComponentChanging"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public virtual void OnComponentChanging(object sender, ComponentChangingEventArgs ce) {
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnControlResize"]/*' />
        /// <devdoc> 
        ///     Notification from the identity behavior upon resizing the control in the designer. 
        ///     This is only called when a user action causes the control to be resized.
        ///     Note that this method may be called several times during a resize process so as 
        ///     to enable live-resize of the contents of the control.
        /// </devdoc>
        [Obsolete("The recommended alternative is OnComponentChanged(). OnComponentChanged is called when any property of the control is changed. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual void OnControlResize() { 
        }
 
        private void OnExpressionsChanged(object sender, EventArgs e) { 
            // Remember that the collection as changed so we can re-persist in OnComponentChanged
            _expressionsChanged = true; 
        }

        /// <devdov>
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.OnPaint"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected virtual void OnPaint(PaintEventArgs e) { 
        }
 
        private void OnViewEvent(object sender, ViewEventArgs e) {
            if (e.EventType == ViewEvent.Click) {
                OnClick((DesignerRegionMouseEventArgs)e.EventArgs);
            } 
            else if (e.EventType == ViewEvent.Paint) {
                OnPaint((PaintEventArgs)e.EventArgs); 
            } 
            else if (e.EventType == ViewEvent.TemplateModeChanged) {
                TemplateModeChangedEventArgs tmea = (TemplateModeChangedEventArgs)e.EventArgs; 
                _inTemplateMode = (tmea.NewTemplateGroup != null);
                // Invalidate the type descriptor so that proper filtering of properties
                // is done when entering and exiting template mode.
                TypeDescriptor.Refresh(Component); 
            }
        } 
 
        private void PersistAttributes(ArrayList attributes) {
            foreach (Triplet triplet in attributes) { 
                string attribName = Convert.ToString(triplet.Second, CultureInfo.InvariantCulture);
                string filter = triplet.First.ToString();
                if ((filter == null) || (filter.Length > 0)) {
                    attribName = filter + ':' + attribName; 
                }
                if (triplet.Third == null) { 
                    RemoveTagAttribute(attribName, true); 
                }
                else { 
                    string persistValue = Convert.ToString(triplet.Third, CultureInfo.InvariantCulture);
                    SetTagAttribute(attribName, persistValue, true);
                }
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.RaiseResizeEvent"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because resizing is handled by the OnComponentChanged() method. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void RaiseResizeEvent() {
            OnControlResize();
        } 

        /// <devdoc> 
        /// Registers internal data in a cloned item. Whenever an item that is to be 
        /// persisted is cloned, there are some internal data structures that have to
        /// be cloned as well. This can only be done by the ControlDesigner. 
        /// </devdoc>
        public void RegisterClone(object original, object clone) {
            if (original == null) {
                throw new ArgumentNullException("original"); 
            }
            if (clone == null) { 
                throw new ArgumentNullException("clone"); 
            }
            ControlBuilder cb = ((IControlBuilderAccessor)Component).ControlBuilder; 
            if (cb != null) {
                ObjectPersistData persistData = cb.GetObjectPersistData();
                persistData.BuiltObjects[clone] = persistData.BuiltObjects[original];
            } 
        }
 
        private void ResetPropertyValue(string property, bool useInstance) { 
            PropertyDescriptor propDesc = null;
 
            if (useInstance) {
                propDesc = TypeDescriptor.GetProperties(Component)[property];
            }
            else { 
                propDesc = TypeDescriptor.GetProperties(Component.GetType())[property];
            } 
 
            if (propDesc != null) {
                IgnoreComponentChanges(true); 
                try {
                    propDesc.ResetValue(Component);
                }
                finally { 
                    IgnoreComponentChanges(false);
                } 
            } 
        }
 
        private void RemoveTagAttribute(string name, bool ignoreCase) {
            if (Tag != null) {
                Tag.RemoveAttribute(name);
            } 
            else {
                BehaviorInternal.RemoveAttribute(name, ignoreCase); 
            } 
        }
 
        public virtual void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) {
        }

        protected void SetRegionContent(EditableDesignerRegion region, string content) { 
            if (View != null) {
                View.SetRegionContent(region, content); 
            } 
        }
 
        private void SetTagAttribute(string name, object value, bool ignoreCase) {
            if (Tag != null) {
                Tag.SetAttribute(name, value.ToString());
            } 
            else {
                BehaviorInternal.SetAttribute(name, value, ignoreCase); 
            } 
        }
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.SetViewFlags"]/*' />
        protected void SetViewFlags(ViewFlags viewFlags, bool setFlag) {
            if (View != null) {
                View.SetFlags(viewFlags, setFlag); 
            }
        } 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.UpdateDesignTimeHtml"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Updates the design time HTML.
        ///    </para>
        /// </devdoc> 
        public virtual void UpdateDesignTimeHtml() {
 
            if (View != null) { 
                View.Update();
            } 
            else {
                if (ReadOnlyInternal) {
#pragma warning disable 618
                    IHtmlControlDesignerBehavior behavior = BehaviorInternal; 
                    if (behavior != null) {
                        Debug.Assert(behavior is IControlDesignerBehavior, "Unexpected type of behavior for custom control"); 
                        ((IControlDesignerBehavior)behavior).DesignTimeHtml = GetDesignTimeHtml(); 
                    }
#pragma warning restore 618 
                }
            }
        }
 
        private void UpdateExpressionValues(IComponent target) {
            IExpressionsAccessor expressionsAccessor = target as IExpressionsAccessor; 
 
            TemplateControl templateControl = null;
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            if (host != null) {
                templateControl = host.RootComponent as TemplateControl;
                Debug.Assert(host.RootComponent == null || templateControl != null, "Expected IDesignerHost.RootComponent to be either null or a valid TemplateControl");
            } 

            // Evaulate all expressions that are currently set 
            foreach (ExpressionBinding eb in expressionsAccessor.Expressions) { 
                if (!eb.Generated) {
                    string propName = eb.PropertyName; 
                    object realTarget;
                    PropertyDescriptor propDesc = GetComplexProperty(target, propName, out realTarget);
                    if (propDesc != null) {
                        IgnoreComponentChanges(true); 
                        try {
                            ExpressionEditor editor = ExpressionEditor.GetExpressionEditor(eb.ExpressionPrefix, target.Site); 
                            if (editor != null) { 
                                // Ensure that parsed expression data is available to the ExpressionEditor
                                object parsedData = EnsureParsedExpression(templateControl, eb, eb.ParsedExpressionData); 

                                object value = editor.EvaluateExpression(eb.Expression, parsedData, propDesc.PropertyType, target.Site);
                                if (value != null) {
                                    if (value is string) { 
                                        TypeConverter converter = propDesc.Converter;
                                        if (converter != null && converter.CanConvertFrom(typeof(string))) { 
                                            value = converter.ConvertFromInvariantString((string)value); 
                                        }
                                    } 
                                    // If we actually got a value from the expression editor, try to apply it to the property
                                    propDesc.SetValue(realTarget, value);
                                }
                                else { 
                                    // If we didn't get a value for the expression, just show the expression
                                    propDesc.SetValue(realTarget, SR.GetString(SR.ExpressionEditor_ExpressionBound, eb.Expression)); 
                                } 
                            }
                            else { 
                                // If we couldn't even find an expression editor, also just show the expression
                                propDesc.SetValue(realTarget, SR.GetString(SR.ExpressionEditor_ExpressionBound, eb.Expression));
                            }
                        } 
                        catch {
                            // There are some legitimate cases where an expression failed to evaluate at design time, or 
                            // is otherwise invalid for the property type, so we don't want to blow up when that happens. 
                        }
                        finally { 
                            IgnoreComponentChanges(false);
                        }
                    }
                } 
            }
            _expressionsChanged = false; 
        } 

        // Called by controls that only show an editable region when the template on the Component 
        // is non-null.
        internal bool UseRegions(DesignerRegionCollection regions, ITemplate componentTemplate) {
            bool useRegions = UseRegionsCore(regions) && (componentTemplate != null);
            return useRegions; 
        }
 
        // Called by controls that always show an editable region, even in the template on the Component 
        // is null.
        internal bool UseRegions(DesignerRegionCollection regions, ITemplate componentTemplate, 
                                 ITemplate viewControlTemplate) {
            // If the template on the Component is null, but the template on the ViewControl is
            // not null, then the template must be coming from the skin.
            bool templateDefinedInSkin = (componentTemplate == null && viewControlTemplate != null); 

            // Do not use an editable region if the template is defined in the skin (VSWhidbey 468562) 
            bool useRegions = UseRegionsCore(regions) && !templateDefinedInSkin; 

            return useRegions; 
        }

        private bool UseRegionsCore(DesignerRegionCollection regions) {
            bool useRegionsCore = (regions != null && View != null && View.SupportsRegions); 
            return useRegionsCore;
        } 
 
        internal static void VerifyInitializeArgument(IComponent component, Type expectedType) {
            if (!expectedType.IsInstanceOfType(component)) { 
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                    SR.GetString(SR.ControlDesigner_ArgumentMustBeOfType), expectedType.FullName), "component");
            }
        } 

        internal class ControlDesignerActionList : DesignerActionList { 
            private ControlDesigner _parent; 

            public ControlDesignerActionList(ControlDesigner parent) : base(parent.Component) { 
                _parent = parent;
            }

            public override bool AutoShow { 
                get {
                    return true; 
                } 
                set {
                } 
            }

            /// <devdoc>
            /// Transacted change callback to invoke the DataBindings dialog. 
            /// </devdoc>
            private bool DataBindingsCallback(object context) { 
                WebUIControl control = (WebUIControl)_parent.Component; 
                ISite site = control.Site;
                DataBindingsDialog dlg = new DataBindingsDialog(site, control); 
                DialogResult result = UIServiceHelper.ShowDialog(site, dlg);
                return (result == DialogResult.OK);
            }
 
            public void EditDataBindings() {
                InvokeTransactedChange(_parent.Component, new TransactedChangeCallback(DataBindingsCallback), null, SR.GetString(SR.Designer_DataBindingsVerb)); 
                _parent.UpdateDesignTimeHtml(); 
            }
 
            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                if (_parent.SupportsDataBindings && _parent.DataBindingsEnabled) {
                    items.Add(new DesignerActionMethodItem(this, "EditDataBindings", SR.GetString(SR.Designer_DataBindingsVerb), String.Empty, SR.GetString(SR.Designer_DataBindingsVerbDesc), true)); 
                }
 
                return items; 
            }
        } 

        internal delegate DesignerAutoFormat CreateAutoFormatDelegate(DataRow schemeData);
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
