//------------------------------------------------------------------------------ 
// <copyright file="WebFormDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Globalization; 
    using System.Resources;
    using System.Web.Compilation; 
    using System.Web.UI;

    /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner"]/*' />
    /// <devdoc> 
    /// </devdoc>
    public abstract class WebFormsRootDesigner : IRootDesigner, IDesignerFilter { 
        private const string dummyProtocolAndServer = "file://foo"; 

        private IComponent _component; 

        private EventHandler _loadCompleteHandler;

        private IUrlResolutionService _urlResolutionService; 
        private DesignerActionService _designerActionService;
        private DesignerActionUIService _designerActionUIService; 
        private IImplicitResourceProvider _implicitResourceProvider; 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Component"]/*' /> 
        /// <devdoc>
        /// </devdoc>
        public virtual IComponent Component {
            get { 
                return _component;
            } 
            set { 
                _component = value;
            } 
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Finalize"]/*' />
        ~WebFormsRootDesigner() { 
            Dispose(false);
        } 
 
        public CultureInfo CurrentCulture {
            get { 
                return CultureInfo.CurrentCulture;
            }
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.DocumentUrl"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public abstract string DocumentUrl { get; }
 
        /// <devdoc>
        /// Returns whether the designer view is locked, keeping controls from being added.
        /// </devdoc>
        public abstract bool IsDesignerViewLocked { get; } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IsLoading"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public abstract bool IsLoading { get; } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.ReferenceManager"]/*' />
        public abstract WebFormsReferenceManager ReferenceManager {
            get; 
        }
 
        protected ViewTechnology[] SupportedTechnologies { 
            get {
                return new ViewTechnology[] { ViewTechnology.Default }; 
            }
        }

        protected DesignerVerbCollection Verbs { 
            get {
                return new DesignerVerbCollection(); 
            } 
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.GetService"]/*' />
        /// <devdoc>
        /// </devdoc>
        protected internal virtual object GetService(Type serviceType) { 
            if (_component != null) {
                ISite site = _component.Site; 
                if (site != null) { 
                    return site.GetService(serviceType);
                } 
            }
            return null;
        }
 
        protected object GetView(ViewTechnology viewTechnology) {
            return null; 
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.LoadComplete"]/*' /> 
        /// <devdoc>
        /// </devdoc>
        public event EventHandler LoadComplete {
            add { 
                _loadCompleteHandler = (EventHandler)Delegate.Combine(_loadCompleteHandler, value);
            } 
            remove { 
                _loadCompleteHandler = (EventHandler)Delegate.Remove(_loadCompleteHandler, value);
            } 
        }

        public abstract void AddClientScriptToDocument(ClientScriptItem scriptItem);
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.AddControlToDocument"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public abstract string AddControlToDocument(Control newControl, Control referenceControl, ControlLocation location);
 
        protected virtual DesignerActionService CreateDesignerActionService(IServiceProvider serviceProvider) {
            return new WebFormsDesignerActionService(serviceProvider);
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.CreateUrlResolutionService"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        protected virtual IUrlResolutionService CreateUrlResolutionService() {
            return new UrlResolutionService(this); 
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Dispose"]/*' />
        /// <devdoc> 
        /// </devdoc>
        protected virtual void Dispose(bool disposing) { 
            if (disposing) { 
                IPropertyValueUIService propUIService = (IPropertyValueUIService)GetService(typeof(IPropertyValueUIService));
                if (propUIService != null) { 
                    propUIService.RemovePropertyValueUIHandler(new PropertyValueUIHandler(OnGetUIValueItem));
                }

                IServiceContainer serviceContainer = (IServiceContainer)GetService(typeof(IServiceContainer)); 
                if (serviceContainer != null) {
                    if (_urlResolutionService != null) { 
                        serviceContainer.RemoveService(typeof(IUrlResolutionService)); 
                    }
 
                    serviceContainer.RemoveService(typeof(IImplicitResourceProvider));
                    if (_designerActionService != null) {
                        _designerActionService.Dispose();
                    } 
                    _designerActionUIService.Dispose();
                } 
 
                _urlResolutionService = null;
 
                _component = null;
            }
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.GenerateEmptyDesignTimeHtml"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public virtual string GenerateEmptyDesignTimeHtml(Control control) {
            return GenerateErrorDesignTimeHtml(control, null, String.Empty); 
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.GenerateErrorDesignTimeHtml"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public virtual string GenerateErrorDesignTimeHtml(Control control, Exception e, string errorMessage) { 
            string name = control.Site.Name; 

            if (errorMessage == null) { 
                errorMessage = String.Empty;
            }
            else {
                errorMessage = HttpUtility.HtmlEncode(errorMessage); 
            }
 
            if (e != null) { 
                errorMessage += "<br />" + HttpUtility.HtmlEncode(e.Message);
            } 

            return String.Format(CultureInfo.InvariantCulture, ControlDesigner.ErrorDesignTimeHtmlTemplate, SR.GetString(SR.ControlDesigner_DesignTimeHtmlError), HttpUtility.HtmlEncode(name), errorMessage);
        }
 
        public abstract ClientScriptItemCollection GetClientScriptsInDocument();
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.GetControlViewAndTag"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected internal abstract void GetControlViewAndTag(Control control, out IControlDesignerView view, out IControlDesignerTag tag);

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Initialize"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public virtual void Initialize(IComponent component) { 
            ControlDesigner.VerifyInitializeArgument(component, typeof(TemplateControl)); 

            _component = component; 

            IServiceContainer serviceContainer = (IServiceContainer)GetService(typeof(IServiceContainer));
            if (serviceContainer != null) {
                _urlResolutionService = CreateUrlResolutionService(); 
                if (_urlResolutionService != null) {
                    serviceContainer.AddService(typeof(IUrlResolutionService), _urlResolutionService); 
                } 

                _designerActionService = CreateDesignerActionService(_component.Site); 
                Debug.Assert(_designerActionService != null, "Did not expecte CreateDesignerActionService to return null.");
                _designerActionUIService = new DesignerActionUIService(_component.Site);

                // Demand create the IImplicitResourceProvider service. 
                ServiceCreatorCallback callback = new ServiceCreatorCallback(this.OnCreateService);
                serviceContainer.AddService(typeof(IImplicitResourceProvider), callback); 
            } 

            IPropertyValueUIService propUIService = (IPropertyValueUIService)GetService(typeof(IPropertyValueUIService)); 
            if (propUIService != null) {
                propUIService.AddPropertyValueUIHandler(new PropertyValueUIHandler(OnGetUIValueItem));
            }
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Initialize"]/*' /> 
        /// <devdoc> 
        ///     Demand creates some of the more infrequently used services we offer.
        /// </devdoc> 
        private object OnCreateService(IServiceContainer container, Type serviceType) {
            if (serviceType == typeof(IImplicitResourceProvider)) {
                if (_implicitResourceProvider == null) {
                    DesignTimeResourceProviderFactory designTimeProvider = ControlDesigner.GetDesignTimeResourceProviderFactory(Component.Site); 
                    IResourceProvider resProvider = designTimeProvider.CreateDesignTimeLocalResourceProvider(Component.Site);
                    _implicitResourceProvider = resProvider as IImplicitResourceProvider; 
                    if (_implicitResourceProvider == null) { 
                        _implicitResourceProvider = new ImplicitResourceProvider(this);
                    } 
                }
                return _implicitResourceProvider;
            }
 
            Debug.Fail("Service type " + serviceType.FullName + " requested but we don't support it");
            return null; 
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.OnGetUIValueItem"]/*' /> 
        /// <devdoc>
        /// </devdoc>
        private void OnGetUIValueItem(ITypeDescriptorContext context, PropertyDescriptor propDesc, ArrayList valueUIItemList) {
            // This only supports top-level properties. Properties such as Font.Bold 
            // are not supported because there is no way to detect the parent chain
            // of ownership of complex properties. 
            Control ctrl = context.Instance as Control; 
            if (ctrl != null) {
                IDataBindingsAccessor dbAcc = (IDataBindingsAccessor)ctrl; 

                if (dbAcc.HasDataBindings) {
                    DataBinding db = dbAcc.DataBindings[propDesc.Name];
 
                    if (db != null) {
                        valueUIItemList.Add(new DataBindingUIItem()); 
                    } 
                }
 
                IExpressionsAccessor expAcc = (IExpressionsAccessor)ctrl;

                if (expAcc.HasExpressions) {
                    ExpressionBinding eb = expAcc.Expressions[propDesc.Name]; 

                    if (eb != null) { 
                        if (eb.Generated) { 
                            valueUIItemList.Add(new ImplicitExpressionUIItem());
                        } 
                        else {
                            valueUIItemList.Add(new ExpressionBindingUIItem());
                        }
                    } 
                }
            } 
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.OnLoadComplete"]/*' /> 
        /// <devdoc>
        /// </devdoc>
        protected virtual void OnLoadComplete(EventArgs e) {
            if (_loadCompleteHandler != null) { 
                _loadCompleteHandler(this, e);
            } 
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PostFilterAttributes"]/*' /> 
        /// <devdoc>
        /// Allows a
        /// designer to filter the set of member attributes the
        /// component it is designing will expose through the 
        /// TypeDescriptor object.
        /// </devdoc> 
        protected virtual void PostFilterAttributes(IDictionary attributes) { 
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PostFilterEvents"]/*' />
        /// <devdoc>
        /// Allows
        /// a designer to filter the set of events the 
        /// component it is designing will expose through the
        /// TypeDescriptor object. 
        /// </devdoc> 
        protected virtual void PostFilterEvents(IDictionary events) {
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PostFilterProperties"]/*' />
        /// <devdoc>
        /// Allows 
        /// a designer to filter the set of properties the
        /// component it is designing will expose through the 
        /// TypeDescriptor object. 
        /// </devdoc>
        protected virtual void PostFilterProperties(IDictionary properties) { 
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PreFilterAttributes"]/*' />
        /// <devdoc> 
        /// Allows a designer
        /// to filter the set of member attributes the component 
        /// it is designing will expose through the TypeDescriptor 
        /// object.
        /// </devdoc> 
        protected virtual void PreFilterAttributes(IDictionary attributes) {
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PreFilterEvents"]/*' /> 
        /// <devdoc>
        /// Allows a 
        /// designer to filter the set of events the component 
        /// it is designing will expose through the TypeDescriptor
        /// object. 
        /// </devdoc>
        protected virtual void PreFilterEvents(IDictionary events) {
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        /// Allows a 
        /// designer to filter the set of properties the component
        /// it is designing will expose through the TypeDescriptor 
        /// object.
        /// </devdoc>
        protected virtual void PreFilterProperties(IDictionary properties) {
        } 

        public abstract void RemoveClientScriptFromDocument(string clientScriptId); 
 
        public abstract void RemoveControlFromDocument(Control control);
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.ResolveUrl"]/*' />
        /// <devdoc>
        /// </devdoc>
        public string ResolveUrl(string relativeUrl) { 
            if (relativeUrl == null) {
                throw new ArgumentNullException("relativeUrl"); 
            } 

            string documentUrl = DocumentUrl; 
            if ((documentUrl == null) || (documentUrl.Length == 0) ||
                IsAppRelativePath(relativeUrl) || IsRooted(relativeUrl) || !IsAppRelativePath(documentUrl)) {
                // If there's no documentUrl or the path is already appRelative or the path is rooted or
                // the document URL (invalid) isn't app-relative, just return what they gave us 
                return relativeUrl;
            } 
 
            // Give this a fake protocol and server so Uri can resolve it
            documentUrl = documentUrl.Replace("~", dummyProtocolAndServer); 
#pragma warning disable 618
            Uri docUri = new Uri(documentUrl, true);
#pragma warning restore 618
 
            Uri resolvedUri = new Uri(docUri, relativeUrl);
 
            string resolvedUrl = resolvedUri.ToString(); 
            resolvedUrl = resolvedUrl.Replace(dummyProtocolAndServer, "~");
 
            return resolvedUrl;
        }

        public virtual void SetControlID(Control control, string id) { 
            ISite site = control.Site;
 
            site.Name = id; 
            control.ID = id.Trim();
        } 

        #region Copied from UrlPath.cs
        private const char appRelativeCharacter = '~';
 
        private static bool IsRooted(String basepath) {
            return(basepath == null || basepath.Length == 0 || basepath[0] == '/' || basepath[0] == '\\'); 
        } 

        private static bool IsAppRelativePath(string path) { 
            return (path.Length >= 2 && path[0] == appRelativeCharacter && (path[1] == '/' || path[1] == '\\'));
        }
        #endregion
 
        #region IDesigner private implementation
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Verbs"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        DesignerVerbCollection IDesigner.Verbs { 
            get {
                return Verbs;
            }
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.DoDefaultAction"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        void IDesigner.DoDefaultAction() { 
        }

        #endregion
 
        #region IDesignerFilter implementation
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PostFilterAttributes"]/*' /> 
        /// <internalonly/> 
        /// <devdoc>
        /// Allows a designer to filter the set of 
        /// attributes the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> object.
        /// </devdoc>
        void IDesignerFilter.PostFilterAttributes(IDictionary attributes) {
            PostFilterAttributes(attributes); 
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PostFilterEvents"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// Allows a designer to filter the set of events
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        /// object.
        /// </devdoc> 
        void IDesignerFilter.PostFilterEvents(IDictionary events) {
            PostFilterEvents(events); 
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PostFilterProperties"]/*' /> 
        /// <internalonly/>
        /// <devdoc>
        /// Allows a designer to filter the set of properties
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object.
        /// </devdoc> 
        void IDesignerFilter.PostFilterProperties(IDictionary properties) { 
            PostFilterProperties(properties);
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PreFilterAttributes"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// Allows a designer to filter the set of
        /// attributes the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object. 
        /// </devdoc>
        void IDesignerFilter.PreFilterAttributes(IDictionary attributes) { 
            PreFilterAttributes(attributes);
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PreFilterEvents"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// Allows a designer to filter the set of events 
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        /// object. 
        /// </devdoc>
        void IDesignerFilter.PreFilterEvents(IDictionary events) {
            PreFilterEvents(events);
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PreFilterProperties"]/*' /> 
        /// <internalonly/> 
        /// <devdoc>
        /// Allows a designer to filter the set of properties 
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        /// object.
        /// </devdoc>
        void IDesignerFilter.PreFilterProperties(IDictionary properties) { 
            PreFilterProperties(properties);
        } 
        #endregion 

        #region IDisposable implementation 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDisposable.Dispose"]/*' />
        void IDisposable.Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this); 
        }
        #endregion 
 
        #region IRootDesigner implementation
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IRootDesigner.SupportedTechnologies"]/*' /> 
        /// <internalonly/>
        ViewTechnology[] IRootDesigner.SupportedTechnologies {
            get {
                return SupportedTechnologies; 
            }
        } 
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IRootDesigner.GetView"]/*' />
        /// <internalonly/> 
        object IRootDesigner.GetView(ViewTechnology viewTechnology) {
            return GetView(viewTechnology);
        }
        #endregion 

 
        /// <devdoc> 
        /// </devdoc>
        private sealed class DataBindingUIItem : PropertyValueUIItem { 
            private static Bitmap _dataBindingBitmap;
            private static string _dataBindingToolTip;

            public DataBindingUIItem() : base(DataBindingUIItem.DataBindingBitmap, new PropertyValueUIItemInvokeHandler(OnValueUIItemInvoke), DataBindingUIItem.DataBindingToolTip) { 
            }
 
            private static Bitmap DataBindingBitmap { 
                get {
                    if (_dataBindingBitmap == null) { 
                        _dataBindingBitmap = new Bitmap(typeof(WebFormsRootDesigner), "DataBindingGlyph.bmp");
                        _dataBindingBitmap.MakeTransparent(Color.Fuchsia);
                    }
                    return _dataBindingBitmap; 
                }
            } 
 
            private static string DataBindingToolTip {
                get { 
                    if (_dataBindingToolTip == null) {
                        _dataBindingToolTip = SR.GetString(SR.DataBindingGlyph_ToolTip);
                    }
                    return _dataBindingToolTip; 
                }
            } 
 
            private static void OnValueUIItemInvoke(ITypeDescriptorContext context, PropertyDescriptor propDesc, PropertyValueUIItem invokedItem) {
                // No action is necessary when the icon is clicked 
            }
        }

 
        /// <devdoc>
        /// </devdoc> 
        private sealed class ExpressionBindingUIItem : PropertyValueUIItem { 
            private static Bitmap _expressionBindingBitmap;
            private static string _expressionBindingToolTip; 

            public ExpressionBindingUIItem() : base(ExpressionBindingUIItem.ExpressionBindingBitmap, new PropertyValueUIItemInvokeHandler(OnValueUIItemInvoke), ExpressionBindingUIItem.ExpressionBindingToolTip) {
            }
 
            private static Bitmap ExpressionBindingBitmap {
                get { 
                    if (_expressionBindingBitmap == null) { 
                        _expressionBindingBitmap = new Bitmap(typeof(WebFormsRootDesigner), "ExpressionBindingGlyph.bmp");
                        _expressionBindingBitmap.MakeTransparent(Color.Fuchsia); 
                    }
                    return _expressionBindingBitmap;
                }
            } 

            private static string ExpressionBindingToolTip { 
                get { 
                    if (_expressionBindingToolTip == null) {
                        _expressionBindingToolTip = SR.GetString(SR.ExpressionBindingGlyph_ToolTip); 
                    }
                    return _expressionBindingToolTip;
                }
            } 

            private static void OnValueUIItemInvoke(ITypeDescriptorContext context, PropertyDescriptor propDesc, PropertyValueUIItem invokedItem) { 
                // No action is necessary when the icon is clicked 
            }
        } 

        /// <devdoc>
        /// </devdoc>
        private sealed class ImplicitExpressionUIItem : PropertyValueUIItem { 
            private static Bitmap _expressionBindingBitmap;
            private static string _expressionBindingToolTip; 
 
            public ImplicitExpressionUIItem() : base(ImplicitExpressionUIItem.ImplicitExpressionBindingBitmap, new PropertyValueUIItemInvokeHandler(OnValueUIItemInvoke), ImplicitExpressionUIItem.ImplicitExpressionBindingToolTip) {
            } 

            private static Bitmap ImplicitExpressionBindingBitmap {
                get {
                    if (_expressionBindingBitmap == null) { 
                        _expressionBindingBitmap = new Bitmap(typeof(WebFormsRootDesigner), "ImplicitExpressionBindingGlyph.bmp");
                        _expressionBindingBitmap.MakeTransparent(Color.Fuchsia); 
                    } 
                    return _expressionBindingBitmap;
                } 
            }

            private static string ImplicitExpressionBindingToolTip {
                get { 
                    if (_expressionBindingToolTip == null) {
                        _expressionBindingToolTip = SR.GetString(SR.ImplicitExpressionBindingGlyph_ToolTip); 
                    } 
                    return _expressionBindingToolTip;
                } 
            }

            private static void OnValueUIItemInvoke(ITypeDescriptorContext context, PropertyDescriptor propDesc, PropertyValueUIItem invokedItem) {
                // No action is necessary when the icon is clicked 
            }
        } 
 

        /// <devdoc> 
        /// </devdoc>
        private sealed class UrlResolutionService : IUrlResolutionService {
            private WebFormsRootDesigner _owner;
 
            public UrlResolutionService(WebFormsRootDesigner owner) {
                _owner = owner; 
            } 

            string IUrlResolutionService.ResolveClientUrl(string relativeUrl) { 
                if (relativeUrl == null) {
                    throw new ArgumentNullException("relativeUrl");
                }
 
                if (!IsAppRelativePath(relativeUrl)) {
                    return relativeUrl; 
                } 

                string documentUrl = _owner.DocumentUrl; 
                if ((documentUrl == null) || (documentUrl.Length == 0) || !IsAppRelativePath(documentUrl)) {
                    // If there is no documentUrl or it isn't app-relative,
                    // trim off the ~/ to make our best guess.
                    return relativeUrl.Substring(2); 
                }
 
                // Give this a fake protocol and server so Uri can resolve it 
                documentUrl = documentUrl.Replace("~", dummyProtocolAndServer);
#pragma warning disable 618 
                Uri docUri = new Uri(documentUrl, true);
#pragma warning restore 618

                // Give this a fake protocol and server so Uri can resolve it 
                relativeUrl = relativeUrl.Replace("~", dummyProtocolAndServer);
#pragma warning disable 618 
                Uri relativeUri = new Uri(relativeUrl, true); 
#pragma warning restore 618
 
                string resultUrl = docUri.MakeRelativeUri(relativeUri).ToString();

                // In the case that relativeUri and docUri can't be made relative
                // MakeRelative just returns relatveUri, so we need to remove the 
                // dummyProcotolAndServer
                return resultUrl.Replace(dummyProtocolAndServer, String.Empty); 
            } 
        }
 
        private sealed class ImplicitResourceProvider : IImplicitResourceProvider {
            private WebFormsRootDesigner _owner;

            public ImplicitResourceProvider(WebFormsRootDesigner owner) { 
                _owner = owner;
            } 
 
            object IImplicitResourceProvider.GetObject(ImplicitResourceKey key, CultureInfo culture) {
                throw new NotSupportedException(); 
            }

            ICollection IImplicitResourceProvider.GetImplicitResourceKeys(string keyPrefix) {
                IDictionary pageResources = GetPageResources(); 
                return pageResources[keyPrefix] as ICollection;
            } 
 
            private IDictionary GetPageResources() {
                if (_owner.Component == null) { 
                    return null;
                }

                IServiceProvider serviceProvider = _owner.Component.Site; 
                if (serviceProvider == null) {
                    return null; 
                } 

                DesignTimeResourceProviderFactory resourceProviderFactory = ControlDesigner.GetDesignTimeResourceProviderFactory(serviceProvider); 
                if (resourceProviderFactory == null) {
                    return null;
                }
 
                IResourceProvider resProvider = resourceProviderFactory.CreateDesignTimeLocalResourceProvider(serviceProvider);
                if (resProvider == null) { 
                    return null; 
                }
 
                IResourceReader resReader = resProvider.ResourceReader;
                if (resReader == null) {
                    return null;
                } 

                IDictionary pageResources = new HybridDictionary(true); 
                if (resReader != null) { 
                    //
                    foreach (DictionaryEntry entry in resReader) { 
                        string key = (string)entry.Key;
                        string filter = String.Empty;

                        // A page resource key looks like [myfilter:]MyResKey.MyProp[.MySubProp] 

                        // Check if there is a filter 
                        if (key.IndexOf(':') > 0) { 
                            string[] parts = key.Split(':');
 
                            // Shouldn't be multiple ':'.  If there is, ignore it
                            if (parts.Length > 2)
                                continue;
 
                            filter = parts[0];
                            key = parts[1]; 
                        } 

                        int periodIndex = key.IndexOf('.'); 

                        // There should be at least one period, for the meta:resourcekey part. If not, ignore.
                        if (periodIndex <= 0)
                            continue; 

                        string resourceKey = key.Substring(0, periodIndex); 
 
                        // The rest of the string is the property (e.g. MyProp.MySubProp)
                        string property = key.Substring(periodIndex + 1); 

                        // Check if we already have an entry for this resource key
                        ArrayList controlResources = (ArrayList)pageResources[resourceKey];
 
                        // If not, create one
                        if (controlResources == null) { 
                            controlResources = new ArrayList(); 
                            pageResources[resourceKey] = controlResources;
                        } 

                        // Add an entry in the ArrayList for this property
                        ImplicitResourceKey resKeyEntry = new ImplicitResourceKey();
                        resKeyEntry.Filter = filter; 
                        resKeyEntry.Property = property;
                        resKeyEntry.KeyPrefix = resourceKey; 
                        controlResources.Add(resKeyEntry); 
                    }
                } 

                return pageResources;
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WebFormDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Globalization; 
    using System.Resources;
    using System.Web.Compilation; 
    using System.Web.UI;

    /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner"]/*' />
    /// <devdoc> 
    /// </devdoc>
    public abstract class WebFormsRootDesigner : IRootDesigner, IDesignerFilter { 
        private const string dummyProtocolAndServer = "file://foo"; 

        private IComponent _component; 

        private EventHandler _loadCompleteHandler;

        private IUrlResolutionService _urlResolutionService; 
        private DesignerActionService _designerActionService;
        private DesignerActionUIService _designerActionUIService; 
        private IImplicitResourceProvider _implicitResourceProvider; 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Component"]/*' /> 
        /// <devdoc>
        /// </devdoc>
        public virtual IComponent Component {
            get { 
                return _component;
            } 
            set { 
                _component = value;
            } 
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Finalize"]/*' />
        ~WebFormsRootDesigner() { 
            Dispose(false);
        } 
 
        public CultureInfo CurrentCulture {
            get { 
                return CultureInfo.CurrentCulture;
            }
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.DocumentUrl"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public abstract string DocumentUrl { get; }
 
        /// <devdoc>
        /// Returns whether the designer view is locked, keeping controls from being added.
        /// </devdoc>
        public abstract bool IsDesignerViewLocked { get; } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IsLoading"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public abstract bool IsLoading { get; } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.ReferenceManager"]/*' />
        public abstract WebFormsReferenceManager ReferenceManager {
            get; 
        }
 
        protected ViewTechnology[] SupportedTechnologies { 
            get {
                return new ViewTechnology[] { ViewTechnology.Default }; 
            }
        }

        protected DesignerVerbCollection Verbs { 
            get {
                return new DesignerVerbCollection(); 
            } 
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.GetService"]/*' />
        /// <devdoc>
        /// </devdoc>
        protected internal virtual object GetService(Type serviceType) { 
            if (_component != null) {
                ISite site = _component.Site; 
                if (site != null) { 
                    return site.GetService(serviceType);
                } 
            }
            return null;
        }
 
        protected object GetView(ViewTechnology viewTechnology) {
            return null; 
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.LoadComplete"]/*' /> 
        /// <devdoc>
        /// </devdoc>
        public event EventHandler LoadComplete {
            add { 
                _loadCompleteHandler = (EventHandler)Delegate.Combine(_loadCompleteHandler, value);
            } 
            remove { 
                _loadCompleteHandler = (EventHandler)Delegate.Remove(_loadCompleteHandler, value);
            } 
        }

        public abstract void AddClientScriptToDocument(ClientScriptItem scriptItem);
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.AddControlToDocument"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public abstract string AddControlToDocument(Control newControl, Control referenceControl, ControlLocation location);
 
        protected virtual DesignerActionService CreateDesignerActionService(IServiceProvider serviceProvider) {
            return new WebFormsDesignerActionService(serviceProvider);
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.CreateUrlResolutionService"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        protected virtual IUrlResolutionService CreateUrlResolutionService() {
            return new UrlResolutionService(this); 
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Dispose"]/*' />
        /// <devdoc> 
        /// </devdoc>
        protected virtual void Dispose(bool disposing) { 
            if (disposing) { 
                IPropertyValueUIService propUIService = (IPropertyValueUIService)GetService(typeof(IPropertyValueUIService));
                if (propUIService != null) { 
                    propUIService.RemovePropertyValueUIHandler(new PropertyValueUIHandler(OnGetUIValueItem));
                }

                IServiceContainer serviceContainer = (IServiceContainer)GetService(typeof(IServiceContainer)); 
                if (serviceContainer != null) {
                    if (_urlResolutionService != null) { 
                        serviceContainer.RemoveService(typeof(IUrlResolutionService)); 
                    }
 
                    serviceContainer.RemoveService(typeof(IImplicitResourceProvider));
                    if (_designerActionService != null) {
                        _designerActionService.Dispose();
                    } 
                    _designerActionUIService.Dispose();
                } 
 
                _urlResolutionService = null;
 
                _component = null;
            }
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.GenerateEmptyDesignTimeHtml"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public virtual string GenerateEmptyDesignTimeHtml(Control control) {
            return GenerateErrorDesignTimeHtml(control, null, String.Empty); 
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.GenerateErrorDesignTimeHtml"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public virtual string GenerateErrorDesignTimeHtml(Control control, Exception e, string errorMessage) { 
            string name = control.Site.Name; 

            if (errorMessage == null) { 
                errorMessage = String.Empty;
            }
            else {
                errorMessage = HttpUtility.HtmlEncode(errorMessage); 
            }
 
            if (e != null) { 
                errorMessage += "<br />" + HttpUtility.HtmlEncode(e.Message);
            } 

            return String.Format(CultureInfo.InvariantCulture, ControlDesigner.ErrorDesignTimeHtmlTemplate, SR.GetString(SR.ControlDesigner_DesignTimeHtmlError), HttpUtility.HtmlEncode(name), errorMessage);
        }
 
        public abstract ClientScriptItemCollection GetClientScriptsInDocument();
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.GetControlViewAndTag"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        protected internal abstract void GetControlViewAndTag(Control control, out IControlDesignerView view, out IControlDesignerTag tag);

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Initialize"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public virtual void Initialize(IComponent component) { 
            ControlDesigner.VerifyInitializeArgument(component, typeof(TemplateControl)); 

            _component = component; 

            IServiceContainer serviceContainer = (IServiceContainer)GetService(typeof(IServiceContainer));
            if (serviceContainer != null) {
                _urlResolutionService = CreateUrlResolutionService(); 
                if (_urlResolutionService != null) {
                    serviceContainer.AddService(typeof(IUrlResolutionService), _urlResolutionService); 
                } 

                _designerActionService = CreateDesignerActionService(_component.Site); 
                Debug.Assert(_designerActionService != null, "Did not expecte CreateDesignerActionService to return null.");
                _designerActionUIService = new DesignerActionUIService(_component.Site);

                // Demand create the IImplicitResourceProvider service. 
                ServiceCreatorCallback callback = new ServiceCreatorCallback(this.OnCreateService);
                serviceContainer.AddService(typeof(IImplicitResourceProvider), callback); 
            } 

            IPropertyValueUIService propUIService = (IPropertyValueUIService)GetService(typeof(IPropertyValueUIService)); 
            if (propUIService != null) {
                propUIService.AddPropertyValueUIHandler(new PropertyValueUIHandler(OnGetUIValueItem));
            }
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Initialize"]/*' /> 
        /// <devdoc> 
        ///     Demand creates some of the more infrequently used services we offer.
        /// </devdoc> 
        private object OnCreateService(IServiceContainer container, Type serviceType) {
            if (serviceType == typeof(IImplicitResourceProvider)) {
                if (_implicitResourceProvider == null) {
                    DesignTimeResourceProviderFactory designTimeProvider = ControlDesigner.GetDesignTimeResourceProviderFactory(Component.Site); 
                    IResourceProvider resProvider = designTimeProvider.CreateDesignTimeLocalResourceProvider(Component.Site);
                    _implicitResourceProvider = resProvider as IImplicitResourceProvider; 
                    if (_implicitResourceProvider == null) { 
                        _implicitResourceProvider = new ImplicitResourceProvider(this);
                    } 
                }
                return _implicitResourceProvider;
            }
 
            Debug.Fail("Service type " + serviceType.FullName + " requested but we don't support it");
            return null; 
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.OnGetUIValueItem"]/*' /> 
        /// <devdoc>
        /// </devdoc>
        private void OnGetUIValueItem(ITypeDescriptorContext context, PropertyDescriptor propDesc, ArrayList valueUIItemList) {
            // This only supports top-level properties. Properties such as Font.Bold 
            // are not supported because there is no way to detect the parent chain
            // of ownership of complex properties. 
            Control ctrl = context.Instance as Control; 
            if (ctrl != null) {
                IDataBindingsAccessor dbAcc = (IDataBindingsAccessor)ctrl; 

                if (dbAcc.HasDataBindings) {
                    DataBinding db = dbAcc.DataBindings[propDesc.Name];
 
                    if (db != null) {
                        valueUIItemList.Add(new DataBindingUIItem()); 
                    } 
                }
 
                IExpressionsAccessor expAcc = (IExpressionsAccessor)ctrl;

                if (expAcc.HasExpressions) {
                    ExpressionBinding eb = expAcc.Expressions[propDesc.Name]; 

                    if (eb != null) { 
                        if (eb.Generated) { 
                            valueUIItemList.Add(new ImplicitExpressionUIItem());
                        } 
                        else {
                            valueUIItemList.Add(new ExpressionBindingUIItem());
                        }
                    } 
                }
            } 
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.OnLoadComplete"]/*' /> 
        /// <devdoc>
        /// </devdoc>
        protected virtual void OnLoadComplete(EventArgs e) {
            if (_loadCompleteHandler != null) { 
                _loadCompleteHandler(this, e);
            } 
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PostFilterAttributes"]/*' /> 
        /// <devdoc>
        /// Allows a
        /// designer to filter the set of member attributes the
        /// component it is designing will expose through the 
        /// TypeDescriptor object.
        /// </devdoc> 
        protected virtual void PostFilterAttributes(IDictionary attributes) { 
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PostFilterEvents"]/*' />
        /// <devdoc>
        /// Allows
        /// a designer to filter the set of events the 
        /// component it is designing will expose through the
        /// TypeDescriptor object. 
        /// </devdoc> 
        protected virtual void PostFilterEvents(IDictionary events) {
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PostFilterProperties"]/*' />
        /// <devdoc>
        /// Allows 
        /// a designer to filter the set of properties the
        /// component it is designing will expose through the 
        /// TypeDescriptor object. 
        /// </devdoc>
        protected virtual void PostFilterProperties(IDictionary properties) { 
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PreFilterAttributes"]/*' />
        /// <devdoc> 
        /// Allows a designer
        /// to filter the set of member attributes the component 
        /// it is designing will expose through the TypeDescriptor 
        /// object.
        /// </devdoc> 
        protected virtual void PreFilterAttributes(IDictionary attributes) {
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PreFilterEvents"]/*' /> 
        /// <devdoc>
        /// Allows a 
        /// designer to filter the set of events the component 
        /// it is designing will expose through the TypeDescriptor
        /// object. 
        /// </devdoc>
        protected virtual void PreFilterEvents(IDictionary events) {
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        /// Allows a 
        /// designer to filter the set of properties the component
        /// it is designing will expose through the TypeDescriptor 
        /// object.
        /// </devdoc>
        protected virtual void PreFilterProperties(IDictionary properties) {
        } 

        public abstract void RemoveClientScriptFromDocument(string clientScriptId); 
 
        public abstract void RemoveControlFromDocument(Control control);
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.ResolveUrl"]/*' />
        /// <devdoc>
        /// </devdoc>
        public string ResolveUrl(string relativeUrl) { 
            if (relativeUrl == null) {
                throw new ArgumentNullException("relativeUrl"); 
            } 

            string documentUrl = DocumentUrl; 
            if ((documentUrl == null) || (documentUrl.Length == 0) ||
                IsAppRelativePath(relativeUrl) || IsRooted(relativeUrl) || !IsAppRelativePath(documentUrl)) {
                // If there's no documentUrl or the path is already appRelative or the path is rooted or
                // the document URL (invalid) isn't app-relative, just return what they gave us 
                return relativeUrl;
            } 
 
            // Give this a fake protocol and server so Uri can resolve it
            documentUrl = documentUrl.Replace("~", dummyProtocolAndServer); 
#pragma warning disable 618
            Uri docUri = new Uri(documentUrl, true);
#pragma warning restore 618
 
            Uri resolvedUri = new Uri(docUri, relativeUrl);
 
            string resolvedUrl = resolvedUri.ToString(); 
            resolvedUrl = resolvedUrl.Replace(dummyProtocolAndServer, "~");
 
            return resolvedUrl;
        }

        public virtual void SetControlID(Control control, string id) { 
            ISite site = control.Site;
 
            site.Name = id; 
            control.ID = id.Trim();
        } 

        #region Copied from UrlPath.cs
        private const char appRelativeCharacter = '~';
 
        private static bool IsRooted(String basepath) {
            return(basepath == null || basepath.Length == 0 || basepath[0] == '/' || basepath[0] == '\\'); 
        } 

        private static bool IsAppRelativePath(string path) { 
            return (path.Length >= 2 && path[0] == appRelativeCharacter && (path[1] == '/' || path[1] == '\\'));
        }
        #endregion
 
        #region IDesigner private implementation
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.Verbs"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        DesignerVerbCollection IDesigner.Verbs { 
            get {
                return Verbs;
            }
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.DoDefaultAction"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        void IDesigner.DoDefaultAction() { 
        }

        #endregion
 
        #region IDesignerFilter implementation
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PostFilterAttributes"]/*' /> 
        /// <internalonly/> 
        /// <devdoc>
        /// Allows a designer to filter the set of 
        /// attributes the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> object.
        /// </devdoc>
        void IDesignerFilter.PostFilterAttributes(IDictionary attributes) {
            PostFilterAttributes(attributes); 
        }
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PostFilterEvents"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// Allows a designer to filter the set of events
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        /// object.
        /// </devdoc> 
        void IDesignerFilter.PostFilterEvents(IDictionary events) {
            PostFilterEvents(events); 
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PostFilterProperties"]/*' /> 
        /// <internalonly/>
        /// <devdoc>
        /// Allows a designer to filter the set of properties
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object.
        /// </devdoc> 
        void IDesignerFilter.PostFilterProperties(IDictionary properties) { 
            PostFilterProperties(properties);
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PreFilterAttributes"]/*' />
        /// <internalonly/>
        /// <devdoc> 
        /// Allows a designer to filter the set of
        /// attributes the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> 
        /// object. 
        /// </devdoc>
        void IDesignerFilter.PreFilterAttributes(IDictionary attributes) { 
            PreFilterAttributes(attributes);
        }

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PreFilterEvents"]/*' /> 
        /// <internalonly/>
        /// <devdoc> 
        /// Allows a designer to filter the set of events 
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        /// object. 
        /// </devdoc>
        void IDesignerFilter.PreFilterEvents(IDictionary events) {
            PreFilterEvents(events);
        } 

        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDesignerFilter.PreFilterProperties"]/*' /> 
        /// <internalonly/> 
        /// <devdoc>
        /// Allows a designer to filter the set of properties 
        /// the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        /// object.
        /// </devdoc>
        void IDesignerFilter.PreFilterProperties(IDictionary properties) { 
            PreFilterProperties(properties);
        } 
        #endregion 

        #region IDisposable implementation 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IDisposable.Dispose"]/*' />
        void IDisposable.Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this); 
        }
        #endregion 
 
        #region IRootDesigner implementation
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IRootDesigner.SupportedTechnologies"]/*' /> 
        /// <internalonly/>
        ViewTechnology[] IRootDesigner.SupportedTechnologies {
            get {
                return SupportedTechnologies; 
            }
        } 
 
        /// <include file='doc\WebFormsRootDesigner.uex' path='docs/doc[@for="WebFormsRootDesigner.IRootDesigner.GetView"]/*' />
        /// <internalonly/> 
        object IRootDesigner.GetView(ViewTechnology viewTechnology) {
            return GetView(viewTechnology);
        }
        #endregion 

 
        /// <devdoc> 
        /// </devdoc>
        private sealed class DataBindingUIItem : PropertyValueUIItem { 
            private static Bitmap _dataBindingBitmap;
            private static string _dataBindingToolTip;

            public DataBindingUIItem() : base(DataBindingUIItem.DataBindingBitmap, new PropertyValueUIItemInvokeHandler(OnValueUIItemInvoke), DataBindingUIItem.DataBindingToolTip) { 
            }
 
            private static Bitmap DataBindingBitmap { 
                get {
                    if (_dataBindingBitmap == null) { 
                        _dataBindingBitmap = new Bitmap(typeof(WebFormsRootDesigner), "DataBindingGlyph.bmp");
                        _dataBindingBitmap.MakeTransparent(Color.Fuchsia);
                    }
                    return _dataBindingBitmap; 
                }
            } 
 
            private static string DataBindingToolTip {
                get { 
                    if (_dataBindingToolTip == null) {
                        _dataBindingToolTip = SR.GetString(SR.DataBindingGlyph_ToolTip);
                    }
                    return _dataBindingToolTip; 
                }
            } 
 
            private static void OnValueUIItemInvoke(ITypeDescriptorContext context, PropertyDescriptor propDesc, PropertyValueUIItem invokedItem) {
                // No action is necessary when the icon is clicked 
            }
        }

 
        /// <devdoc>
        /// </devdoc> 
        private sealed class ExpressionBindingUIItem : PropertyValueUIItem { 
            private static Bitmap _expressionBindingBitmap;
            private static string _expressionBindingToolTip; 

            public ExpressionBindingUIItem() : base(ExpressionBindingUIItem.ExpressionBindingBitmap, new PropertyValueUIItemInvokeHandler(OnValueUIItemInvoke), ExpressionBindingUIItem.ExpressionBindingToolTip) {
            }
 
            private static Bitmap ExpressionBindingBitmap {
                get { 
                    if (_expressionBindingBitmap == null) { 
                        _expressionBindingBitmap = new Bitmap(typeof(WebFormsRootDesigner), "ExpressionBindingGlyph.bmp");
                        _expressionBindingBitmap.MakeTransparent(Color.Fuchsia); 
                    }
                    return _expressionBindingBitmap;
                }
            } 

            private static string ExpressionBindingToolTip { 
                get { 
                    if (_expressionBindingToolTip == null) {
                        _expressionBindingToolTip = SR.GetString(SR.ExpressionBindingGlyph_ToolTip); 
                    }
                    return _expressionBindingToolTip;
                }
            } 

            private static void OnValueUIItemInvoke(ITypeDescriptorContext context, PropertyDescriptor propDesc, PropertyValueUIItem invokedItem) { 
                // No action is necessary when the icon is clicked 
            }
        } 

        /// <devdoc>
        /// </devdoc>
        private sealed class ImplicitExpressionUIItem : PropertyValueUIItem { 
            private static Bitmap _expressionBindingBitmap;
            private static string _expressionBindingToolTip; 
 
            public ImplicitExpressionUIItem() : base(ImplicitExpressionUIItem.ImplicitExpressionBindingBitmap, new PropertyValueUIItemInvokeHandler(OnValueUIItemInvoke), ImplicitExpressionUIItem.ImplicitExpressionBindingToolTip) {
            } 

            private static Bitmap ImplicitExpressionBindingBitmap {
                get {
                    if (_expressionBindingBitmap == null) { 
                        _expressionBindingBitmap = new Bitmap(typeof(WebFormsRootDesigner), "ImplicitExpressionBindingGlyph.bmp");
                        _expressionBindingBitmap.MakeTransparent(Color.Fuchsia); 
                    } 
                    return _expressionBindingBitmap;
                } 
            }

            private static string ImplicitExpressionBindingToolTip {
                get { 
                    if (_expressionBindingToolTip == null) {
                        _expressionBindingToolTip = SR.GetString(SR.ImplicitExpressionBindingGlyph_ToolTip); 
                    } 
                    return _expressionBindingToolTip;
                } 
            }

            private static void OnValueUIItemInvoke(ITypeDescriptorContext context, PropertyDescriptor propDesc, PropertyValueUIItem invokedItem) {
                // No action is necessary when the icon is clicked 
            }
        } 
 

        /// <devdoc> 
        /// </devdoc>
        private sealed class UrlResolutionService : IUrlResolutionService {
            private WebFormsRootDesigner _owner;
 
            public UrlResolutionService(WebFormsRootDesigner owner) {
                _owner = owner; 
            } 

            string IUrlResolutionService.ResolveClientUrl(string relativeUrl) { 
                if (relativeUrl == null) {
                    throw new ArgumentNullException("relativeUrl");
                }
 
                if (!IsAppRelativePath(relativeUrl)) {
                    return relativeUrl; 
                } 

                string documentUrl = _owner.DocumentUrl; 
                if ((documentUrl == null) || (documentUrl.Length == 0) || !IsAppRelativePath(documentUrl)) {
                    // If there is no documentUrl or it isn't app-relative,
                    // trim off the ~/ to make our best guess.
                    return relativeUrl.Substring(2); 
                }
 
                // Give this a fake protocol and server so Uri can resolve it 
                documentUrl = documentUrl.Replace("~", dummyProtocolAndServer);
#pragma warning disable 618 
                Uri docUri = new Uri(documentUrl, true);
#pragma warning restore 618

                // Give this a fake protocol and server so Uri can resolve it 
                relativeUrl = relativeUrl.Replace("~", dummyProtocolAndServer);
#pragma warning disable 618 
                Uri relativeUri = new Uri(relativeUrl, true); 
#pragma warning restore 618
 
                string resultUrl = docUri.MakeRelativeUri(relativeUri).ToString();

                // In the case that relativeUri and docUri can't be made relative
                // MakeRelative just returns relatveUri, so we need to remove the 
                // dummyProcotolAndServer
                return resultUrl.Replace(dummyProtocolAndServer, String.Empty); 
            } 
        }
 
        private sealed class ImplicitResourceProvider : IImplicitResourceProvider {
            private WebFormsRootDesigner _owner;

            public ImplicitResourceProvider(WebFormsRootDesigner owner) { 
                _owner = owner;
            } 
 
            object IImplicitResourceProvider.GetObject(ImplicitResourceKey key, CultureInfo culture) {
                throw new NotSupportedException(); 
            }

            ICollection IImplicitResourceProvider.GetImplicitResourceKeys(string keyPrefix) {
                IDictionary pageResources = GetPageResources(); 
                return pageResources[keyPrefix] as ICollection;
            } 
 
            private IDictionary GetPageResources() {
                if (_owner.Component == null) { 
                    return null;
                }

                IServiceProvider serviceProvider = _owner.Component.Site; 
                if (serviceProvider == null) {
                    return null; 
                } 

                DesignTimeResourceProviderFactory resourceProviderFactory = ControlDesigner.GetDesignTimeResourceProviderFactory(serviceProvider); 
                if (resourceProviderFactory == null) {
                    return null;
                }
 
                IResourceProvider resProvider = resourceProviderFactory.CreateDesignTimeLocalResourceProvider(serviceProvider);
                if (resProvider == null) { 
                    return null; 
                }
 
                IResourceReader resReader = resProvider.ResourceReader;
                if (resReader == null) {
                    return null;
                } 

                IDictionary pageResources = new HybridDictionary(true); 
                if (resReader != null) { 
                    //
                    foreach (DictionaryEntry entry in resReader) { 
                        string key = (string)entry.Key;
                        string filter = String.Empty;

                        // A page resource key looks like [myfilter:]MyResKey.MyProp[.MySubProp] 

                        // Check if there is a filter 
                        if (key.IndexOf(':') > 0) { 
                            string[] parts = key.Split(':');
 
                            // Shouldn't be multiple ':'.  If there is, ignore it
                            if (parts.Length > 2)
                                continue;
 
                            filter = parts[0];
                            key = parts[1]; 
                        } 

                        int periodIndex = key.IndexOf('.'); 

                        // There should be at least one period, for the meta:resourcekey part. If not, ignore.
                        if (periodIndex <= 0)
                            continue; 

                        string resourceKey = key.Substring(0, periodIndex); 
 
                        // The rest of the string is the property (e.g. MyProp.MySubProp)
                        string property = key.Substring(periodIndex + 1); 

                        // Check if we already have an entry for this resource key
                        ArrayList controlResources = (ArrayList)pageResources[resourceKey];
 
                        // If not, create one
                        if (controlResources == null) { 
                            controlResources = new ArrayList(); 
                            pageResources[resourceKey] = controlResources;
                        } 

                        // Add an entry in the ArrayList for this property
                        ImplicitResourceKey resKeyEntry = new ImplicitResourceKey();
                        resKeyEntry.Filter = filter; 
                        resKeyEntry.Property = property;
                        resKeyEntry.KeyPrefix = resourceKey; 
                        controlResources.Add(resKeyEntry); 
                    }
                } 

                return pageResources;
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
