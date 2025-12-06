//------------------------------------------------------------------------------ 
// <copyright file="HttpContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * HttpContext class 
 *
 * Copyright (c) 1999 Microsoft Corporation 
 */

namespace System.Web {
    using System.Collections; 
    using System.ComponentModel;
    using System.Configuration; 
    using System.Configuration.Internal; 
    using System.Globalization;
    using System.Reflection; 
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security.Principal; 
    using System.Threading;
    using System.Web.Security; 
    using System.Web.SessionState; 
    using System.Web.Configuration;
    using System.Web.Caching; 
    using System.Web.Hosting;
    using System.Web.Util;
    using System.Web.UI;
    using System.Runtime.Remoting.Messaging; 
    using System.Security.Permissions;
    using System.Web.Profile; 
    using System.EnterpriseServices; 
    using System.Web.Management;
    using System.Web.Compilation; 


    /// <devdoc>
    ///    <para>Encapsulates 
    ///       all HTTP-specific
    ///       context used by the HTTP server to process Web requests.</para> 
    /// <para>System.Web.IHttpModules and System.Web.IHttpHandler instances are provided a 
    ///    reference to an appropriate HttpContext object. For example
    ///    the Request and Response 
    ///    objects.</para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class HttpContext : IServiceProvider { 

        internal static readonly Assembly SystemWebAssembly = typeof(HttpContext).Assembly; 
 
        private IHttpAsyncHandler  _asyncAppHandler;   // application as handler (not always HttpApplication)
        private HttpApplication    _appInstance; 
        private IHttpHandler       _handler;
        private HttpRequest        _request;
        private HttpResponse       _response;
        private HttpServerUtility  _server; 
        private Stack              _traceContextStack;
        private TraceContext       _topTraceContext; 
        private Hashtable          _items; 
        private ArrayList          _errors;
        private Exception          _tempError; 
        private bool               _errorCleared;
        private IPrincipal         _user;
        private IntPtr             _pManagedPrincipal;
        internal ProfileBase   _Profile; 
        private DateTime           _utcTimestamp;
        private HttpWorkerRequest  _wr; 
        private GCHandle           _root; 
        private IntPtr             _ctxPtr;
        private VirtualPath        _configurationPath; 
        internal bool               _skipAuthorization;
        private CultureInfo        _dynamicCulture;
        private CultureInfo        _dynamicUICulture;
        private int                _serverExecuteDepth; 
        private Stack              _handlerStack;
        private bool               _preventPostback; 
        private bool               _runtimeErrorReported; 
        private bool               _firstNotificationInitCalled;
 
        // timeout support
        private DateTime   _timeoutStartTime = DateTime.MinValue;
        private bool       _timeoutSet;
        private TimeSpan   _timeout; 
        private int        _timeoutState;   // 0=non-cancelable, 1=cancelable, -1=canceled
        private DoubleLink _timeoutLink;    // link in the timeout's manager list 
        private Thread     _thread; 

        // cached configuration 
        private CachedPathData _configurationPathData; // Cached data if _configurationPath != null
        private CachedPathData _filePathData;   // Cached data of the file being requested

        // Sql Cache Dependency 
        private string _sqlDependencyCookie;
 
        // Delayed session state item 
        SessionStateModule  _sessionStateModule;    // if non-null, it means we have a delayed session state item
 
        // non-compiled pages
        private TemplateControl _templateControl;

        // integrated pipeline state 

        // keep synchronized with mgdhandler.hxx 
        private const int FLAG_NONE                          =   0x0; 
        private const int FLAG_CHANGE_IN_SERVER_VARIABLES    =   0x1;
        private const int FLAG_CHANGE_IN_REQUEST_HEADERS     =   0x2; 
        private const int FLAG_CHANGE_IN_RESPONSE_HEADERS    =   0x4;
        private const int FLAG_CHANGE_IN_USER_OBJECT         =   0x8;
        private const int FLAG_SEND_RESPONSE_HEADERS         =  0x10;
        private const int FLAG_RESPONSE_HEADERS_SENT         =  0x20; 
        internal const int FLAG_ETW_PROVIDER_ENABLED         =  0x40;
        private const int FLAG_CHANGE_IN_RESPONSE_STATUS     =  0x80; 
 
        private NotificationContext _notificationContext;
        private bool _isAppInitialized; 
        private bool _isIntegratedPipeline;
        private bool _finishPipelineRequestCalled;
        private bool _impersonationEnabled;
 
        internal bool HideRequestResponse;
        internal volatile bool InIndicateCompletion; 
        internal volatile HttpApplication.ThreadContext IndicateCompletionContext = null; 
        // synchronization context (for the newasync pattern)
        private AspNetSynchronizationContext _syncContext; 

        // session state support
        internal bool RequiresSessionState;
        internal bool ReadOnlySessionState; 
        internal bool InAspCompatMode;
 
        /// <include file='doc\HttpContext.uex' path='docs/doc[@for="HttpContext.HttpContext"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the HttpContext class.
        ///    </para>
        /// </devdoc>
        public HttpContext(HttpRequest request, HttpResponse response) { 
            Init(request, response);
            request.Context = this; 
            response.Context = this; 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the HttpContext class. 
        ///    </para>
        /// </devdoc> 
        public HttpContext(HttpWorkerRequest wr) { 
            _wr = wr;
            Init(new HttpRequest(wr, this), new HttpResponse(wr, this)); 
            _response.InitResponseWriter();
        }

        // ctor used in HttpRuntime 
        internal HttpContext(HttpWorkerRequest wr, bool initResponseWriter) {
            _wr = wr; 
            Init(new HttpRequest(wr, this), new HttpResponse(wr, this)); 

            if (initResponseWriter) 
                _response.InitResponseWriter();

            PerfCounters.IncrementCounter(AppPerfCounter.REQUESTS_EXECUTING);
        } 

        private void Init(HttpRequest request, HttpResponse response) { 
            _request = request; 
            _response = response;
            _utcTimestamp = DateTime.UtcNow; 

            if (_wr is IIS7WorkerRequest) {
                _isIntegratedPipeline = true;
            } 

            if (!(_wr is System.Web.SessionState.StateHttpWorkerRequest)) 
                CookielessHelper.RemoveCookielessValuesFromPath(); // This ensures that the cookieless-helper is initialized and 
            // rewrites the path if the URI contains cookieless form-auth ticket, session-id, etc.
 
            Profiler p = HttpRuntime.Profile;
            if (p != null && p.IsEnabled)
                _topTraceContext = new TraceContext(this);
        } 

        // Current HttpContext off the call context 
#if DBG 
        internal static void SetDebugAssertOnAccessToCurrent(bool doAssert) {
            if (doAssert) { 
                CallContext.SetData("__ContextAssert", String.Empty);
            }
            else {
                CallContext.SetData("__ContextAssert", null); 
            }
        } 
 
        private static bool NeedDebugAssertOnAccessToCurrent {
            get { 
                return (CallContext.GetData("__ContextAssert") != null);
            }
        }
#endif 

        /// <devdoc> 
        ///    <para>Returns the current HttpContext object.</para> 
        /// </devdoc>
        public static HttpContext Current { 
            get {
#if DBG
                if (NeedDebugAssertOnAccessToCurrent) {
                    Debug.Assert(ContextBase.Current != null); 
                }
#endif 
                return ContextBase.Current as HttpContext; 
            }
 
            set {
                ContextBase.Current = value;
            }
        } 

        // 
        //  Root / unroot for the duration of async operation 
        //
 
        internal void Root() {
            _root = GCHandle.Alloc(this);
            _ctxPtr = GCHandle.ToIntPtr(_root);
        } 

        internal void Unroot() { 
            if(_root.IsAllocated) { 
                _root.Free();
                _ctxPtr = IntPtr.Zero; 
            }
        }

        internal void FinishPipelineRequest() { 
            if (!_finishPipelineRequestCalled) {
                _finishPipelineRequestCalled = true; 
                HttpRuntime.FinishPipelineRequest(this); 
            }
        } 

        internal IntPtr ContextPtr { get { return _ctxPtr; } }

        internal void ValidatePath() { 
            string physicalPath = _request.PhysicalPathInternal;
 
            // Get the cached path data. If the path is suspicious, GetConfigurationPathData will throw. 
            // If it doesn't throw and returns a CachedPathData, then the path is safe.  However, to
            // be extra cautious, only use this optimization if the path that CachedPathData 
            // used (which it got from MapPath) is the same as what we got from IIS.
            CachedPathData pathData = GetConfigurationPathData();

            // assert that config system successfully obtained a physical path 
            Debug.Assert(pathData.PhysicalPath != null);
 
            if (StringUtil.EqualsIgnoreCase(pathData.PhysicalPath, physicalPath)) { 
                return;
            } 

            // If we're here, the paths were different, which we don't think should ever happen.
            // But if it does, be safe and check the IIS physical path explicitely
            Debug.Assert(false, "ValidationPath couldn't apply optimization, Request.PhysicalPath=" + physicalPath + "; pathData.PhysicalPath=" + pathData.PhysicalPath); 

            FileUtil.CheckSuspiciousPhysicalPath(physicalPath); 
        } 

 
        // IServiceProvider implementation

        /// <internalonly/>
        Object IServiceProvider.GetService(Type service) { 
            Object obj;
 
            if (service == typeof(HttpWorkerRequest)) { 
                InternalSecurityPermissions.UnmanagedCode.Demand();
                obj = _wr; 
            }
            else if (service == typeof(HttpRequest))
                obj = Request;
            else if (service == typeof(HttpResponse)) 
                obj = Response;
            else if (service == typeof(HttpApplication)) 
                obj = ApplicationInstance; 
            else if (service == typeof(HttpApplicationState))
                obj = Application; 
            else if (service == typeof(HttpSessionState))
                obj = Session;
            else if (service == typeof(HttpServerUtility))
                obj = Server; 
            else
                obj = null; 
 
            return obj;
        } 

        //
        // Async app handler is remembered for the duration of execution of the
        // request when application happens to be IHttpAsyncHandler. It is needed 
        // for HttpRuntime to remember the object on which to call OnEndRequest.
        // 
        // The assumption is that application is a IHttpAsyncHandler, not always 
        // HttpApplication.
        // 
        internal IHttpAsyncHandler AsyncAppHandler {
            get { return _asyncAppHandler; }
            set { _asyncAppHandler = value; }
        } 

 
 
        /// <devdoc>
        ///    <para>Retrieves a reference to the application object for the current Http request.</para> 
        /// </devdoc>
        public HttpApplication ApplicationInstance {
            get { return _appInstance;}
            set { 
                // For integrated pipeline, once this is set to a non-null value, it can only be set to null.
                // The setter should never have been made public.  It probably happened in 1.0, before it was possible 
                // to have getter and setter with different accessibility. 
                if (_isIntegratedPipeline && _appInstance != null && value != null) {
                    throw new InvalidOperationException(SR.GetString(SR.Application_instance_cannot_be_changed)); 
                }
                else {
                    _appInstance = value;
                } 
            }
        } 
 

        /// <devdoc> 
        ///    <para>
        ///       Retrieves a reference to the application object for the current
        ///       Http request.
        ///    </para> 
        /// </devdoc>
        public HttpApplicationState Application { 
            get { return HttpApplicationFactory.ApplicationState; } 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Retrieves or assigns a reference to the <see cref='System.Web.IHttpHandler'/> 
        ///       object for the current request.
        ///    </para> 
        /// </devdoc> 
        public IHttpHandler Handler {
            get { return _handler;} 
            set {
                _handler = value;
                RequiresSessionState = false;
                ReadOnlySessionState = false; 
                InAspCompatMode = false;
                if (_handler != null) { 
                    if (_handler is IRequiresSessionState) { 
                        RequiresSessionState = true;
                    } 
                    if (_handler is IReadOnlySessionState) {
                        ReadOnlySessionState = true;
                    }
                    Page page = _handler as Page; 
                    if (page != null && page.IsInAspCompatMode) {
                        InAspCompatMode = true; 
                    } 
                }
            } 
        }


        /// <devdoc> 
        ///    <para>
        ///       Retrieves or assigns a reference to the <see cref='System.Web.IHttpHandler'/> 
        ///       object for the previous handler; 
        ///    </para>
        /// </devdoc> 

        public IHttpHandler PreviousHandler {
            get {
                if (_handlerStack == null || _handlerStack.Count == 0) 
                    return null;
 
                return (IHttpHandler)_handlerStack.Peek(); 
            }
        } 


        /// <devdoc>
        ///    <para> 
        ///       Retrieves or assigns a reference to the <see cref='System.Web.IHttpHandler'/>
        ///       object for the current executing handler; 
        ///    </para> 
        /// </devdoc>
        private IHttpHandler _currentHandler = null; 

        public IHttpHandler CurrentHandler {
            get {
                if (_currentHandler == null) 
                    _currentHandler = _handler;
 
                return _currentHandler; 
            }
        } 

        internal void RestoreCurrentHandler() {
            _currentHandler = (IHttpHandler)_handlerStack.Pop();
        } 

        internal void SetCurrentHandler(IHttpHandler newtHandler) { 
            if (_handlerStack == null) { 
                _handlerStack = new Stack();
            } 
            _handlerStack.Push(CurrentHandler);

            _currentHandler = newtHandler;
        } 

 
        /// <devdoc> 
        ///    <para>
        ///       Retrieves a reference to the target <see cref='System.Web.HttpRequest'/> 
        ///       object for the current request.
        ///    </para>
        /// </devdoc>
        public HttpRequest Request { 
            get {
                 if (HideRequestResponse) 
                    throw new HttpException(SR.GetString(SR.Request_not_available)); 
                return _request;
            } 
        }


        /// <devdoc> 
        ///    <para>
        ///       Retrieves a reference to the <see cref='System.Web.HttpResponse'/> 
        ///       object for the current response. 
        ///    </para>
        /// </devdoc> 
        public HttpResponse Response {
            get {
                if (HideRequestResponse)
                    throw new HttpException(SR.GetString(SR.Response_not_available)); 
                return _response;
            } 
        } 

        internal IHttpHandler TopHandler { 
            get {
                if (_handlerStack == null) {
                    return _handler;
                } 
                object[] handlers = _handlerStack.ToArray();
                if (handlers == null || handlers.Length == 0) { 
                    return _handler; 
                }
                return (IHttpHandler)handlers[handlers.Length - 1]; 
            }
        }

 
        /// <devdoc>
        /// <para>Retrieves a reference to the <see cref='System.Web.TraceContext'/> object for the current 
        ///    response.</para> 
        /// </devdoc>
        public TraceContext Trace { 
            get {
                if (_topTraceContext == null)
                    _topTraceContext = new TraceContext(this);
                return _topTraceContext; 
            }
        } 
 
        internal bool TraceIsEnabled {
            get { 
                if (_topTraceContext == null)
                    return false;

                return _topTraceContext.IsEnabled; 
            }
            set { 
                if (value) 
                    _topTraceContext = new TraceContext(this);
            } 

        }

 

        /// <devdoc> 
        ///    <para> 
        ///       Retrieves a key-value collection that can be used to
        ///       build up and share data between an <see cref='System.Web.IHttpModule'/> and an <see cref='System.Web.IHttpHandler'/> 
        ///       during a
        ///       request.
        ///    </para>
        /// </devdoc> 
        public IDictionary Items {
            get { 
                if (_items == null) 
                    _items = new Hashtable();
 
                return _items;
            }
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       Gets a reference to the <see cref='System.Web.SessionState'/> instance for the current request.
        ///    </para> 
        /// </devdoc>
        public HttpSessionState Session {
            get {
                if (_sessionStateModule != null) { 
                    lock (this) {
                        if (_sessionStateModule != null) { 
                            // If it's not null, it means we have a delayed session state item 
                            _sessionStateModule.InitStateStoreItem(true);
                            _sessionStateModule = null; 
                        }
                    }
                }
 
                return(HttpSessionState)Items[SessionStateUtility.SESSION_KEY];
            } 
        } 

        internal void AddDelayedHttpSessionState(SessionStateModule module) { 
            if (_sessionStateModule != null) {
                throw new HttpException(SR.GetString(SR.Cant_have_multiple_session_module));
            }
            _sessionStateModule = module; 
        }
 
        internal void RemoveDelayedHttpSessionState() { 
            Debug.Assert(_sessionStateModule != null, "_sessionStateModule != null");
            _sessionStateModule = null; 
        }


        /// <devdoc> 
        ///    <para>
        ///       Gets a reference to the <see cref='System.Web.HttpServerUtility'/> 
        ///       for the current 
        ///       request.
        ///    </para> 
        /// </devdoc>
        public HttpServerUtility Server {
            get {
                // create only on demand 
                if (_server == null)
                    _server = new HttpServerUtility(this); 
                return _server; 
            }
        } 

        // if the context has an error, report it, but only one time
        internal void ReportRuntimeErrorIfExists(ref RequestNotificationStatus status) {
            Exception e = Error; 

            if (e == null || _runtimeErrorReported) { 
                return; 
            }
 
            // WOS 1921799: custom errors don't work in integrated mode if there's an initialization exception
            if (_notificationContext != null && CurrentModuleIndex == -1) {
                try {
                    IIS7WorkerRequest wr = _wr as IIS7WorkerRequest; 
                    if (Request.QueryString["aspxerrorpath"] != null
                        && wr != null 
                        && String.IsNullOrEmpty(wr.GetManagedHandlerType()) 
                        && wr.GetCurrentModuleName() == PipelineRuntime.InitExceptionModuleName) {
                        status = RequestNotificationStatus.Continue;   // allow non-managed handler to execute request 
                        return;
                    }
                }
                catch { 
                }
            } 
 
            _runtimeErrorReported = true;
 
            if (HttpRuntime.AppOfflineMessage != null) {
                try {
                    // report app offline error
                    Response.StatusCode = 404; 
                    Response.TrySkipIisCustomErrors = true;
                    Response.OutputStream.Write(HttpRuntime.AppOfflineMessage, 0, HttpRuntime.AppOfflineMessage.Length); 
                } 
                catch {
                } 
            }
            else {
                // report error exception
                using (new HttpContextWrapper(this)) { 
                    // when application is on UNC share the code below must
                    // be run while impersonating the token given by IIS 
                    using (new ApplicationImpersonationContext()) { 

                        try { 
                            try {
                                // try to report error in a way that could possibly throw (a config exception)
                                Response.ReportRuntimeError(e, true /*canThrow*/, false);
                            } 
                            catch (Exception eReport) {
                                // report the config error in a way that would not throw 
                                Response.ReportRuntimeError(eReport, false /*canThrow*/, false); 
                            }
                        } 
                        catch (Exception) {
                        }
                    }
                } 
            }
 
            status = RequestNotificationStatus.FinishRequest; 
            return;
        } 

        /// <devdoc>
        ///    <para>
        ///       Gets the 
        ///       first error (if any) accumulated during request processing.
        ///    </para> 
        /// </devdoc> 
        public Exception Error {
            get { 
                if (_tempError != null)
                    return _tempError;
                if (_errors == null || _errors.Count == 0 || _errorCleared)
                    return null; 
                return (Exception)_errors[0];
            } 
        } 

        // 
        // Temp error (yet to be caught on app level)
        // to be reported as Server.GetLastError() but could be cleared later
        //
        internal Exception TempError { 
            get { return _tempError; }
            set { _tempError = value; } 
        } 

 
        /// <devdoc>
        ///    <para>
        ///       An array (collection) of errors accumulated while processing a
        ///       request. 
        ///    </para>
        /// </devdoc> 
        public Exception[] AllErrors { 
            get {
                int n = (_errors != null) ? _errors.Count : 0; 

                if (n == 0)
                    return null;
 
                Exception[] errors = new Exception[n];
                _errors.CopyTo(0, errors, 0, n); 
                return errors; 
            }
        } 


        /// <devdoc>
        ///    <para> 
        ///       Registers an error for the current request.
        ///    </para> 
        /// </devdoc> 
        public void AddError(Exception errorInfo) {
            if (_errors == null) 
                _errors = new ArrayList();

            _errors.Add(errorInfo);
 
            if (_isIntegratedPipeline && _notificationContext != null) {
                // set the error on the current notification context 
                _notificationContext.Error = errorInfo; 
            }
        } 


        /// <devdoc>
        ///    <para> 
        ///       Clears all errors for the current request.
        ///    </para> 
        /// </devdoc> 
        public void ClearError() {
            if (_tempError != null) 
                _tempError = null;
            else
                _errorCleared = true;
 
            if (_isIntegratedPipeline && _notificationContext != null) {
                // clear the error on the current notification context 
                _notificationContext.Error = null; 
            }
        } 


        /// <devdoc>
        ///    <para> 
        ///       IPrincipal security information.
        ///    </para> 
        /// </devdoc> 
        public IPrincipal User {
            get { return _user; } 

            [SecurityPermission(SecurityAction.Demand, ControlPrincipal=true)]
            set {
                SetPrincipalNoDemand(value); 
            }
        } 
 
        // route all internals call to the principal (that don't have luring attacks)
        // through this method so we can centralize reporting 
        // Before this, some auth modules were assigning directly to _user
        internal void SetPrincipalNoDemand(IPrincipal principal, bool needToSetNativePrincipal) {
            _user = principal;
 
            // push changes through to native side
            if (needToSetNativePrincipal 
                && _isIntegratedPipeline 
                && _notificationContext.CurrentNotification == RequestNotification.AuthenticateRequest) {
 
                IntPtr pManagedPrincipal = IntPtr.Zero;
                try {
                    IIS7WorkerRequest wr = _wr as IIS7WorkerRequest;
                    if (principal != null) { 
                        GCHandle h = GCHandle.Alloc(principal);
                        try { 
                            pManagedPrincipal = GCHandle.ToIntPtr(h); 
                            wr.SetPrincipal(principal, pManagedPrincipal);
                        } 
                        catch {
                            pManagedPrincipal = IntPtr.Zero;
                            if (h.IsAllocated) {
                                h.Free(); 
                            }
                            throw; 
                        } 
                    }
                    else { 
                        wr.SetPrincipal(null, IntPtr.Zero);
                    }
                }
                finally { 
                    if (_pManagedPrincipal != IntPtr.Zero) {
                        GCHandle h = GCHandle.FromIntPtr(_pManagedPrincipal); 
                        if (h.IsAllocated) { 
                            h.Free();
                        } 
                    }
                    _pManagedPrincipal = pManagedPrincipal;
                }
            } 
        }
 
        internal void SetPrincipalNoDemand(IPrincipal principal) { 
            SetPrincipalNoDemand(principal, true /*needToSetNativePrincipal*/);
        } 

        internal bool _ProfileDelayLoad = false;

        public ProfileBase  Profile { 
            get {
                if (_Profile == null && _ProfileDelayLoad) 
                    _Profile = ProfileBase.Create(Request.IsAuthenticated ? User.Identity.Name : Request.AnonymousID, Request.IsAuthenticated); 
                return _Profile;
            } 
        }


        public bool SkipAuthorization { 
            get { return _skipAuthorization;}
 
            [SecurityPermission(SecurityAction.Demand, ControlPrincipal=true)] 
            set {
                SetSkipAuthorizationNoDemand(value, false); 
            }
        }

        internal void SetSkipAuthorizationNoDemand(bool value, bool managedOnly) 
        {
            if (HttpRuntime.UseIntegratedPipeline 
                && !managedOnly 
                && value != _skipAuthorization) {
 
                // For integrated mode, persist changes to SkipAuthorization
                // in the IS_LOGIN_PAGE server variable.  When this server variable exists
                // and the value is not "0", IIS skips authorization.
 
                _request.SetSkipAuthorization(value);
            } 
 
            _skipAuthorization = value;
        } 

        /// <devdoc>
        ///    <para>
        ///       Is this request in debug mode? 
        ///    </para>
        /// </devdoc> 
        public bool IsDebuggingEnabled { 
            get {
                try { 
                    return CompilationUtil.IsDebuggingEnabled(this);
                }
                catch {
                    // in case of config errors don't throw 
                    return false;
                } 
            } 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Is this custom error enabled for this request? 
        ///    </para>
        /// </devdoc> 
        public bool IsCustomErrorEnabled { 
            get {
                return CustomErrorsSection.GetSettings(this).CustomErrorsEnabled(_request); 
            }
        }

        internal TemplateControl TemplateControl { 
            get {
                return _templateControl; 
            } 
            set {
                _templateControl = value; 
            }
        }

 
        /// <devdoc>
        ///    <para>Gets the initial timestamp of the current request.</para> 
        /// </devdoc> 
        public DateTime Timestamp {
            get { return _utcTimestamp.ToLocalTime();} 
        }

        internal DateTime UtcTimestamp {
            get { return _utcTimestamp;} 
        }
 
        internal HttpWorkerRequest WorkerRequest { 
            get { return _wr;}
        } 


        /// <devdoc>
        ///    <para> 
        ///       Gets a reference to the System.Web.Cache.Cache object for the current request.
        ///    </para> 
        /// </devdoc> 
        public Cache Cache {
            get { return HttpRuntime.Cache;} 
        }
#if SITECOUNTERS

        /// <devdoc> 
        ///    <para>
        ///       Gets a reference to the System.Web.SiteCounters object. 
        ///    </para> 
        /// </devdoc>
        public SiteCounters SiteCounters { 
            get { return HttpRuntime.SiteCounters;}
        }
#endif
        /* 
         * The virtual path used to get config settings.  This allows the user
         * to specify a non default config path, without having to pass it to every 
         * configuration call. 
         */
        internal VirtualPath ConfigurationPath { 
            get {
                if (_configurationPath == null)
                    _configurationPath = _request.FilePathObject;
 
                return _configurationPath;
            } 
 
            set {
                _configurationPath = value; 
                if (_configurationPathData != null) {
                    if (!_configurationPathData.CompletedFirstRequest) {
                        CachedPathData.RemoveBadPathData(_configurationPathData);
                    } 

                    _configurationPathData = null; 
                } 

                if (_filePathData != null) { 
                    if (!_filePathData.CompletedFirstRequest) {
                        CachedPathData.RemoveBadPathData(_filePathData);
                    }
 
                    _filePathData = null;
                } 
            } 
        }
 
        internal CachedPathData GetFilePathData() {
            if (_filePathData == null) {
                _filePathData = CachedPathData.GetVirtualPathData(_request.FilePathObject, false);
            } 

            return _filePathData; 
        } 

        internal CachedPathData GetConfigurationPathData() { 
            if (_configurationPath == null) {
                return GetFilePathData();
            }
 
            //
            if (_configurationPathData == null) { 
                _configurationPathData = CachedPathData.GetVirtualPathData(_configurationPath, true); 
            }
 
            return _configurationPathData;
        }

        internal CachedPathData GetPathData(VirtualPath path) { 
            if (path != null) {
                if (path.Equals(_request.FilePathObject)) { 
                    return GetFilePathData(); 
                }
 
                if (_configurationPath != null && path.Equals(_configurationPath)) {
                    return GetConfigurationPathData();
                }
            } 

            return CachedPathData.GetVirtualPathData(path, false); 
        } 

        internal void FirstNotificationInit() { 
            // called once during the first notification for a request
            if (!_firstNotificationInitCalled) {
                _firstNotificationInitCalled = true;
                ValidatePath(); 
            }
        } 
 
        internal void FinishRequestForCachedPathData(int statusCode) {
            // Remove the cached path data for a file path if the first request for it 
            // does not succeed due to a bad request. Otherwise we could be vulnerable
            // to a DOS attack.
            if (_filePathData != null && !_filePathData.CompletedFirstRequest) {
                if (400 <= statusCode && statusCode < 500) { 
                    CachedPathData.RemoveBadPathData(_filePathData);
                } 
                else { 
                    CachedPathData.MarkCompleted(_filePathData);
                } 
            }
        }

        /* 
         * Uses the Config system to get the specified configuraiton
         */ 
        [Obsolete("The recommended alternative is System.Web.Configuration.WebConfigurationManager.GetWebApplicationSection in System.Web.dll. http://go.microsoft.com/fwlink/?linkid=14202")] 
        public static object GetAppConfig(String name) {
            return WebConfigurationManager.GetWebApplicationSection(name); 
        }

        [Obsolete("The recommended alternative is System.Web.HttpContext.GetSection in System.Web.dll. http://go.microsoft.com/fwlink/?linkid=14202")]
        public object GetConfig(String name) { 
            return GetSection(name);
        } 
 
        public object GetSection(String sectionName) {
            if (HttpConfigurationSystem.UseHttpConfigurationSystem) { 
                return GetConfigurationPathData().ConfigRecord.GetSection(sectionName);
            }
            else {
                return ConfigurationManager.GetSection(sectionName); 
            }
        } 
 
        internal RuntimeConfig GetRuntimeConfig() {
            return GetConfigurationPathData().RuntimeConfig; 
        }

        internal RuntimeConfig GetRuntimeConfig(VirtualPath path) {
            return GetPathData(path).RuntimeConfig; 
        }
 
        public void RewritePath(String path) { 
            RewritePath(path, true);
        } 

        /*
         * Called by the URL rewrite module to modify the path for downstream modules
         */ 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public void RewritePath(String path, bool rebaseClientPath) { 
            if (path == null)
                throw new ArgumentNullException("path");

            // extract query string 
            String qs = null;
            int iqs = path.IndexOf('?'); 
            if (iqs >= 0) { 
                qs = (iqs < path.Length-1) ? path.Substring(iqs+1) : String.Empty;
                path = path.Substring(0, iqs); 
            }

            // resolve relative path
            VirtualPath virtualPath = VirtualPath.Create(path); 
            virtualPath = Request.FilePathObject.Combine(virtualPath);
 
            // disallow paths outside of app 
            virtualPath.FailIfNotWithinAppRoot();
 
            // clear things that depend on path
            ConfigurationPath = null;

            // rewrite path on request 
            Request.InternalRewritePath(virtualPath, qs, rebaseClientPath);
        } 
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void RewritePath(String filePath, String pathInfo, String queryString) {
            RewritePath(VirtualPath.CreateAllowNull(filePath), VirtualPath.CreateAllowNull(pathInfo), 
                queryString, false /*setClientFilePath*/);
        } 
        public void RewritePath(string filePath, string pathInfo, String queryString, bool setClientFilePath) 
        {
            RewritePath(VirtualPath.CreateAllowNull(filePath), VirtualPath.CreateAllowNull(pathInfo), queryString, setClientFilePath); 
        }
        internal void RewritePath(VirtualPath filePath, VirtualPath pathInfo, String queryString, bool setClientFilePath) {
            if (filePath == null)
                throw new ArgumentNullException("filePath"); 

            // resolve relative path 
            filePath = Request.FilePathObject.Combine(filePath); 

            // disallow paths outside of app 
            filePath.FailIfNotWithinAppRoot();

            // clear things that depend on path
            ConfigurationPath = null; 

            // rewrite path on request 
            Request.InternalRewritePath(filePath, pathInfo, queryString, setClientFilePath); 
        }
 
        internal CultureInfo DynamicCulture {
            get { return _dynamicCulture; }
            set { _dynamicCulture = value; }
        } 

        internal CultureInfo DynamicUICulture { 
            get { return _dynamicUICulture; } 
            set { _dynamicUICulture = value; }
        } 

        public static object GetGlobalResourceObject(string classKey, string resourceKey) {
            return GetGlobalResourceObject(classKey, resourceKey, null);
        } 

        public static object GetGlobalResourceObject(string classKey, string resourceKey, CultureInfo culture) { 
            return ResourceExpressionBuilder.GetGlobalResourceObject(classKey, resourceKey, null, null, culture); 
        }
 
        public static object GetLocalResourceObject(string virtualPath, string resourceKey) {
            return GetLocalResourceObject(virtualPath, resourceKey, null);
        }
 
        public static object GetLocalResourceObject(string virtualPath, string resourceKey, CultureInfo culture) {
            IResourceProvider pageProvider = ResourceExpressionBuilder.GetLocalResourceProvider( 
                VirtualPath.Create(virtualPath)); 
            return ResourceExpressionBuilder.GetResourceObject(pageProvider, resourceKey, culture);
        } 

        internal int ServerExecuteDepth {
            get { return _serverExecuteDepth; }
            set { _serverExecuteDepth = value; } 
        }
 
        internal bool PreventPostback { 
            get { return _preventPostback; }
            set { _preventPostback = value; } 
        }

        //
        // Timeout support 
        //
 
        internal Thread CurrentThread { 
            get {
                return _thread; 
            }
            set {
                _thread = value;
            } 
        }
 
        internal TimeSpan Timeout { 
            get {
                EnsureTimeout(); 
                return _timeout;
            }

            set { 
                _timeout = value;
                _timeoutSet = true; 
            } 
        }
 
        internal void EnsureTimeout() {
            // Ensure that calls to Timeout property will not go to config after this call
            if (!_timeoutSet) {
                HttpRuntimeSection cfg = RuntimeConfig.GetConfig(this).HttpRuntime; 
                int s = (int) cfg.ExecutionTimeout.TotalSeconds;
                _timeout = new TimeSpan(0, 0, s); 
                _timeoutSet = true; 
            }
        } 

        internal DoubleLink TimeoutLink {
            get { return _timeoutLink;}
            set { _timeoutLink = value;} 
        }
 
        /* 

        Notes on the following 5 functions: 

        Execution can be cancelled only during certain periods, when inside the catch
        block for ThreadAbortException.  These periods are marked with the value of
        _timeoutState of 1. 

        There is potential [rare] race condition when the timeout thread would call 
        thread.abort but the execution logic in the meantime escapes the catch block. 
        To avoid such race conditions _timeoutState of -1 (cancelled) is introduced.
        The timeout thread sets _timeoutState to -1 before thread abort and the 
        unwinding logic just waits for the exception in this case. The wait cannot
        be done in EndCancellablePeriod because the function is call from inside of
        a finally block and thus would wait indefinetely. That's why another function
        WaitForExceptionIfCancelled had been added. 

        Originally _timeoutStartTime was set in BeginCancellablePeriod. However, that means 
        we'll call UtcNow everytime we call ExecuteStep, which is too expensive. So to save 
        CPU time we created a new method SetStartTime() which is called by the caller of
        ExecuteStep. 

        */

        internal void BeginCancellablePeriod() { 
            // It could be caused by an exception in OnThreadStart
            if (_timeoutStartTime == DateTime.MinValue) { 
                SetStartTime(); 
            }
 
            _timeoutState = 1;
        }

        internal void SetStartTime() { 
            _timeoutStartTime = DateTime.UtcNow;
        } 
 
        internal void EndCancellablePeriod() {
            Interlocked.CompareExchange(ref _timeoutState, 0, 1); 
        }

        internal void WaitForExceptionIfCancelled() {
            while (_timeoutState == -1) 
                Thread.Sleep(100);
        } 
 
        internal bool IsInCancellablePeriod {
            get { return (_timeoutState == 1); } 
        }

        internal Thread MustTimeout(DateTime utcNow) {
            if (_timeoutState == 1) {  // fast check 
                if (TimeSpan.Compare(utcNow.Subtract(_timeoutStartTime), Timeout) >= 0) {
                    // don't abort in debug mode 
                    try { 
                        if (CompilationUtil.IsDebuggingEnabled(this) || System.Diagnostics.Debugger.IsAttached)
                            return null; 
                    }
                    catch {
                        // ignore config errors
                        return null; 
                    }
 
                    // abort the thread only if in cancelable state, avoiding race conditions 
                    // the caller MUST timeout if the return is true
                    if (Interlocked.CompareExchange(ref _timeoutState, -1, 1) == 1) 
                        return _thread;
                }
            }
 
            return null;
        } 
 
        // call a delegate within cancellable period (possibly throwing timeout exception)
        internal void InvokeCancellableCallback(WaitCallback callback, Object state) { 
            if (IsInCancellablePeriod) {
                // call directly
                callback(state);
                return; 
            }
 
            try { 
                BeginCancellablePeriod();  // request can be cancelled from this point
 
                try {
                    callback(state);
                }
                finally { 
                    EndCancellablePeriod();  // request can be cancelled until this point
                } 
 
                WaitForExceptionIfCancelled();  // wait outside of finally
            } 
            catch (ThreadAbortException e) {
                if (e.ExceptionState != null &&
                    e.ExceptionState is HttpApplication.CancelModuleException &&
                    ((HttpApplication.CancelModuleException)e.ExceptionState).Timeout) { 

                    Thread.ResetAbort(); 
                    PerfCounters.IncrementCounter(AppPerfCounter.REQUESTS_TIMED_OUT); 

                    throw new HttpException(SR.GetString(SR.Request_timed_out), 
                                        null, WebEventCodes.RuntimeErrorRequestAbort);
                }
            }
        } 

        internal void PushTraceContext() { 
            if (_traceContextStack == null) { 
                _traceContextStack = new Stack();
            } 

            // push current TraceContext on stack
            _traceContextStack.Push(_topTraceContext);
 
            // now make a new one for the top if necessary
            if (_topTraceContext != null) { 
                TraceContext tc = new TraceContext(this); 
                _topTraceContext.CopySettingsTo(tc);
                _topTraceContext = tc; 
            }
        }

        internal void PopTraceContext() { 
            Debug.Assert(_traceContextStack != null);
            _topTraceContext = (TraceContext) _traceContextStack.Pop(); 
        } 

        internal bool RequestRequiresAuthorization()  { 
#if !FEATURE_PAL // FEATURE_PAL does not enable IIS-based hosting features
            // if current user is anonymous, then trivially, this page does not require authorization
            if (!User.Identity.IsAuthenticated)
                return false; 

            // Ask each of the authorization modules 
            return 
                ( FileAuthorizationModule.RequestRequiresAuthorization(this) ||
                  UrlAuthorizationModule.RequestRequiresAuthorization(this)   ); 
#else // !FEATURE_PAL
                return false; // ROTORTODO
#endif // !FEATURE_PAL
        } 

        internal int CallISAPI(UnsafeNativeMethods.CallISAPIFunc iFunction, byte [] bufIn, byte [] bufOut) { 
 
            if (_wr == null || !(_wr is System.Web.Hosting.ISAPIWorkerRequest))
                throw new HttpException(SR.GetString(SR.Cannot_call_ISAPI_functions)); 
#if !FEATURE_PAL // FEATURE_PAL does not enable IIS-based hosting features
            return ((System.Web.Hosting.ISAPIWorkerRequest) _wr).CallISAPI(iFunction, bufIn, bufOut);
#else // !FEATURE_PAL
                throw new NotImplementedException ("ROTORTODO"); 
#endif // !FEATURE_PAL
        } 
 
        internal void SendEmptyResponse() {
#if !FEATURE_PAL // FEATURE_PAL does not enable IIS-based hosting features 
            if (_wr != null  && (_wr is System.Web.Hosting.ISAPIWorkerRequest))
                ((System.Web.Hosting.ISAPIWorkerRequest) _wr).SendEmptyResponse();
#endif // !FEATURE_PAL
        } 

        private  CookielessHelperClass _CookielessHelper; 
        internal CookielessHelperClass  CookielessHelper { 
            get {
                if (_CookielessHelper == null) 
                    _CookielessHelper = new CookielessHelperClass(this);
                return _CookielessHelper;
            }
        } 

 
        // When a thread enters the pipeline, we may need to set the cookie in the CallContext. 
        internal void ResetSqlDependencyCookie() {
            if (_sqlDependencyCookie != null) { 
                System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(SqlCacheDependency.SQL9_OUTPUT_CACHE_DEPENDENCY_COOKIE, _sqlDependencyCookie);
            }
        }
 
        // When a thread leaves the pipeline, we may need to remove the cookie from the CallContext.
        internal void RemoveSqlDependencyCookie() { 
            if (_sqlDependencyCookie != null) { 
                System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(SqlCacheDependency.SQL9_OUTPUT_CACHE_DEPENDENCY_COOKIE, null);
            } 
        }

        internal string SqlDependencyCookie {
            get { 
                return _sqlDependencyCookie;
            } 
 
            set {
                _sqlDependencyCookie = value; 
                System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(SqlCacheDependency.SQL9_OUTPUT_CACHE_DEPENDENCY_COOKIE, value);
            }
        }
 
        //
        // integrated pipeline related 
        // 
        internal NotificationContext NotificationContext {
            get { return _notificationContext; } 
            set { _notificationContext = value; }
        }

        public RequestNotification CurrentNotification { 
            get {
                if (!HttpRuntime.UseIntegratedPipeline) { 
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode)); 
                }
 
                return _notificationContext.CurrentNotification;
            }
            internal set {
                if (!HttpRuntime.UseIntegratedPipeline) { 
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode));
                } 
 
                _notificationContext.CurrentNotification = value;
            } 
        }

        internal bool IsChangeInServerVars {
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_CHANGE_IN_SERVER_VARIABLES) == FLAG_CHANGE_IN_SERVER_VARIABLES; } 
        }
 
        internal bool IsChangeInRequestHeaders { 
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_CHANGE_IN_REQUEST_HEADERS) == FLAG_CHANGE_IN_REQUEST_HEADERS; }
        } 

        internal bool IsChangeInResponseHeaders {
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_CHANGE_IN_RESPONSE_HEADERS) == FLAG_CHANGE_IN_RESPONSE_HEADERS; }
        } 

        internal bool IsChangeInResponseStatus { 
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_CHANGE_IN_RESPONSE_STATUS) == FLAG_CHANGE_IN_RESPONSE_STATUS; } 
        }
 
        internal bool IsChangeInUserPrincipal {
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_CHANGE_IN_USER_OBJECT) == FLAG_CHANGE_IN_USER_OBJECT; }
        }
 
        internal bool IsSendResponseHeaders {
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_SEND_RESPONSE_HEADERS) == FLAG_SEND_RESPONSE_HEADERS; } 
        } 

        internal void SetImpersonationEnabled() { 
            IdentitySection c = RuntimeConfig.GetConfig(this).Identity;
            _impersonationEnabled = (c != null && c.Impersonate);
        }
 
        internal bool UsesImpersonation {
            get { 
                // if we're on a UNC share and we have a UNC token, then use impersonation for all notifications 
                if (HttpRuntime.IsOnUNCShareInternal && HostingEnvironment.ApplicationIdentityToken != IntPtr.Zero) {
                    return true; 
                }
                // if <identity impersonate=/> is false, then don't use impersonation
                if (!_impersonationEnabled) {
                    return false; 
                }
                // if this notification is after AuthenticateRequest and not a SendResponse notification, use impersonation 
                return ( ( (_notificationContext.CurrentNotification == RequestNotification.AuthenticateRequest && _notificationContext.IsPostNotification) 
                           || _notificationContext.CurrentNotification > RequestNotification.AuthenticateRequest )
                         && _notificationContext.CurrentNotification != RequestNotification.SendResponse ); 
            }
        }

        internal bool AreResponseHeadersSent { 
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_RESPONSE_HEADERS_SENT) == FLAG_RESPONSE_HEADERS_SENT; }
        } 
 
        internal bool NeedToInitializeApp() {
            bool needToInit = !_isAppInitialized; 
            if (needToInit) {
                _isAppInitialized = true;
            }
            return needToInit; 
        }
 
        // flags passed in on the call to PipelineRuntime::ProcessRequestNotification 
        internal int CurrentNotificationFlags {
            get { 
                return _notificationContext.CurrentNotificationFlags;
            }
            set {
                _notificationContext.CurrentNotificationFlags = value; 
            }
        } 
 
        // index of the current "module" running the request
        // into the application module array 
        internal int CurrentModuleIndex {
            get {
                return _notificationContext.CurrentModuleIndex;
            } 
            set {
                _notificationContext.CurrentModuleIndex = value; 
            } 
        }
 
        // Each module has a PipelineModuleStepContainer
        // which stores/manages a list of event handlers
        // that correspond to each RequestNotification.
        // CurrentModuleEventIndex is the index (for the current 
        // module) of the current event handler.
        // This will be greater than one when a single 
        // module registers multiple delegates for a single event. 
        // e.g.
        // app.BeginRequest += Foo; 
        // app.BeginRequest += Bar;
        internal int CurrentModuleEventIndex {
            get {
                return _notificationContext.CurrentModuleEventIndex; 
            }
            set { 
                _notificationContext.CurrentModuleEventIndex = value; 
            }
        } 

        internal void DisableNotifications(RequestNotification notifications, RequestNotification postNotifications) {
            IIS7WorkerRequest wr = _wr as IIS7WorkerRequest;
            if (null != wr) { 
                wr.DisableNotifications(notifications, postNotifications);
            } 
        } 

        // if the principal is derived from WindowsIdentity 
        // there may be a dup'ed token here that we should dispose
        // of as quickly as possible
        internal void DisposePrincipal() {
            if (_pManagedPrincipal == IntPtr.Zero 
                && _user != null
                && _user != WindowsAuthenticationModule.AnonymousPrincipal) { 
                WindowsIdentity id = _user.Identity as WindowsIdentity; 
                if (id != null) {
                    _user = null; 
                    id.Dispose();
                }
            }
        } 

        public bool IsPostNotification { 
            get { 
                if (!HttpRuntime.UseIntegratedPipeline) {
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode)); 
                }
                return _notificationContext.IsPostNotification;
            }
            internal set { 
                if (!HttpRuntime.UseIntegratedPipeline) {
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode)); 
                } 
                _notificationContext.IsPostNotification = value;
            } 

        }

        // user token for the request 
        internal IntPtr ClientIdentityToken {
            get { 
                if (_wr != null) { 
                    return _wr.GetUserToken();
                } 
                else {
                    return IntPtr.Zero;
                }
            } 
        }
 
        // is configured to impersonate client? 
        internal bool IsClientImpersonationConfigured {
            get { 
                try {
                    IdentitySection c = RuntimeConfig.GetConfig(this).Identity;
                    return (c != null && c.Impersonate && c.ImpersonateToken == IntPtr.Zero);
                } 
                catch {
                    // this property should not throw as it is used in the error reporting pass 
                    // config errors will be reported elsewhere 
                    return false;
                } 
            }
        }

        internal IntPtr ImpersonationToken { 
            get {
                // by default use app identity 
                IntPtr token = HostingEnvironment.ApplicationIdentityToken; 
                IdentitySection c = RuntimeConfig.GetConfig(this).Identity;
                if (c != null) { 
                    if (c.Impersonate) {
                        token = (c.ImpersonateToken != IntPtr.Zero) ? c.ImpersonateToken : ClientIdentityToken;
                    }
                    else { 
                        // for non-UNC case impersonate="false" means "don't impersonate",
                        // but there is a special case for UNC shares - even if 
                        // impersonate="false" we still impersonate the UNC identity 
                        // (hosting identity). and this is how v1.x works as well
                        if (!HttpRuntime.IsOnUNCShareInternal) { 
                            token = IntPtr.Zero;
                        }
                    }
                } 
                return token;
            } 
        } 

        internal AspNetSynchronizationContext SyncContext { 
            get {
                if (_syncContext == null) {
                    _syncContext = new AspNetSynchronizationContext(ApplicationInstance);
                } 

                return _syncContext; 
            } 
        }
 
        internal AspNetSynchronizationContext InstallNewAspNetSynchronizationContext() {
            AspNetSynchronizationContext syncContext = _syncContext;

            if (syncContext != null && syncContext == AsyncOperationManager.SynchronizationContext) { 
                // using current ASP.NET synchronization context - switch it
                _syncContext = new AspNetSynchronizationContext(ApplicationInstance); 
                AsyncOperationManager.SynchronizationContext = _syncContext; 
                return syncContext;
            } 

            return null;
        }
 
        internal void RestoreSavedAspNetSynchronizationContext(AspNetSynchronizationContext syncContext) {
            AsyncOperationManager.SynchronizationContext = syncContext; 
            _syncContext = syncContext; 
        }
 
        internal string UserLanguageFromContext() {
            if(Request != null && Request.UserLanguages != null) {
                string userLanguageEntry = Request.UserLanguages[0];
                if(userLanguageEntry != null) { 
                    int loc = userLanguageEntry.IndexOf(';');
                    if(loc != -1) { 
                        return userLanguageEntry.Substring(0, loc); 
                    }
                    else { 
                        return userLanguageEntry;
                    }
                }
            } 
            return null;
        } 
 
        // References should be nulled a.s.a.p. to reduce working set
        internal void ClearReferences() { 
            _appInstance = null;
            _handler = null;
            _handlerStack = null;
            _currentHandler = null; 
            if (_isIntegratedPipeline) {
                _items = null; 
                _syncContext = null; 
            }
        } 

        internal CultureInfo CultureFromConfig(string configString, bool requireSpecific) {
            //auto
            if(StringUtil.EqualsIgnoreCase(configString, HttpApplication.AutoCulture)) { 
                string userLanguage = UserLanguageFromContext();
                if (userLanguage != null) { 
                    try { 
                        if (requireSpecific) {
                            return HttpServerUtility.CreateReadOnlySpecificCultureInfo(userLanguage); 
                        }
                        else {
                            return HttpServerUtility.CreateReadOnlyCultureInfo(userLanguage);
                        } 
                    }
                    catch { 
                        return null; 
                    }
                } 
                else {
                    return null;
                }
            } 
            else if(StringUtil.StringStartsWithIgnoreCase(configString, "auto:")) {
                string userLanguage = UserLanguageFromContext(); 
                if(userLanguage != null) { 
                    try {
                        if(requireSpecific) { 
                            return HttpServerUtility.CreateReadOnlySpecificCultureInfo(userLanguage);
                        }
                        else {
                            return HttpServerUtility.CreateReadOnlyCultureInfo(userLanguage); 
                        }
                    } 
                    catch { 
                        if(requireSpecific) {
                            return HttpServerUtility.CreateReadOnlySpecificCultureInfo(HttpApplication.GetFallbackCulture(configString)); 
                        }
                        else {
                            return HttpServerUtility.CreateReadOnlyCultureInfo(HttpApplication.GetFallbackCulture(configString));
                        } 
                    }
                } 
                else { 
                    if(requireSpecific) {
                        return HttpServerUtility.CreateReadOnlySpecificCultureInfo(configString.Substring(5)); 
                    }
                    else {
                        return HttpServerUtility.CreateReadOnlyCultureInfo(configString.Substring(5));
                    } 
                }
            } 
            if(requireSpecific) { 
                return HttpServerUtility.CreateReadOnlySpecificCultureInfo(configString);
            } 
            else {
                return HttpServerUtility.CreateReadOnlyCultureInfo(configString);
            }
        } 
    }
 
    // 
    // Helper class to add/remove HttpContext to/from CallContext
    // 
    // using (new HttpContextWrapper(context)) {
    //     // this code will have HttpContext.Current working
    // }
    // 

    internal class HttpContextWrapper : IDisposable { 
        private bool _needToUndo; 
        private HttpContext _savedContext;
 
        internal static HttpContext SwitchContext(HttpContext context) {
            return ContextBase.SwitchContext(context) as HttpContext;
        }
 
        internal HttpContextWrapper(HttpContext context) {
            if (context != null) { 
                _savedContext = SwitchContext(context); 
                _needToUndo = (_savedContext != context);
            } 
        }

        void IDisposable.Dispose() {
            if (_needToUndo) { 
                SwitchContext(_savedContext);
                _savedContext = null; 
                _needToUndo = false; 
            }
        } 
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="HttpContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * HttpContext class 
 *
 * Copyright (c) 1999 Microsoft Corporation 
 */

namespace System.Web {
    using System.Collections; 
    using System.ComponentModel;
    using System.Configuration; 
    using System.Configuration.Internal; 
    using System.Globalization;
    using System.Reflection; 
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security.Principal; 
    using System.Threading;
    using System.Web.Security; 
    using System.Web.SessionState; 
    using System.Web.Configuration;
    using System.Web.Caching; 
    using System.Web.Hosting;
    using System.Web.Util;
    using System.Web.UI;
    using System.Runtime.Remoting.Messaging; 
    using System.Security.Permissions;
    using System.Web.Profile; 
    using System.EnterpriseServices; 
    using System.Web.Management;
    using System.Web.Compilation; 


    /// <devdoc>
    ///    <para>Encapsulates 
    ///       all HTTP-specific
    ///       context used by the HTTP server to process Web requests.</para> 
    /// <para>System.Web.IHttpModules and System.Web.IHttpHandler instances are provided a 
    ///    reference to an appropriate HttpContext object. For example
    ///    the Request and Response 
    ///    objects.</para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class HttpContext : IServiceProvider { 

        internal static readonly Assembly SystemWebAssembly = typeof(HttpContext).Assembly; 
 
        private IHttpAsyncHandler  _asyncAppHandler;   // application as handler (not always HttpApplication)
        private HttpApplication    _appInstance; 
        private IHttpHandler       _handler;
        private HttpRequest        _request;
        private HttpResponse       _response;
        private HttpServerUtility  _server; 
        private Stack              _traceContextStack;
        private TraceContext       _topTraceContext; 
        private Hashtable          _items; 
        private ArrayList          _errors;
        private Exception          _tempError; 
        private bool               _errorCleared;
        private IPrincipal         _user;
        private IntPtr             _pManagedPrincipal;
        internal ProfileBase   _Profile; 
        private DateTime           _utcTimestamp;
        private HttpWorkerRequest  _wr; 
        private GCHandle           _root; 
        private IntPtr             _ctxPtr;
        private VirtualPath        _configurationPath; 
        internal bool               _skipAuthorization;
        private CultureInfo        _dynamicCulture;
        private CultureInfo        _dynamicUICulture;
        private int                _serverExecuteDepth; 
        private Stack              _handlerStack;
        private bool               _preventPostback; 
        private bool               _runtimeErrorReported; 
        private bool               _firstNotificationInitCalled;
 
        // timeout support
        private DateTime   _timeoutStartTime = DateTime.MinValue;
        private bool       _timeoutSet;
        private TimeSpan   _timeout; 
        private int        _timeoutState;   // 0=non-cancelable, 1=cancelable, -1=canceled
        private DoubleLink _timeoutLink;    // link in the timeout's manager list 
        private Thread     _thread; 

        // cached configuration 
        private CachedPathData _configurationPathData; // Cached data if _configurationPath != null
        private CachedPathData _filePathData;   // Cached data of the file being requested

        // Sql Cache Dependency 
        private string _sqlDependencyCookie;
 
        // Delayed session state item 
        SessionStateModule  _sessionStateModule;    // if non-null, it means we have a delayed session state item
 
        // non-compiled pages
        private TemplateControl _templateControl;

        // integrated pipeline state 

        // keep synchronized with mgdhandler.hxx 
        private const int FLAG_NONE                          =   0x0; 
        private const int FLAG_CHANGE_IN_SERVER_VARIABLES    =   0x1;
        private const int FLAG_CHANGE_IN_REQUEST_HEADERS     =   0x2; 
        private const int FLAG_CHANGE_IN_RESPONSE_HEADERS    =   0x4;
        private const int FLAG_CHANGE_IN_USER_OBJECT         =   0x8;
        private const int FLAG_SEND_RESPONSE_HEADERS         =  0x10;
        private const int FLAG_RESPONSE_HEADERS_SENT         =  0x20; 
        internal const int FLAG_ETW_PROVIDER_ENABLED         =  0x40;
        private const int FLAG_CHANGE_IN_RESPONSE_STATUS     =  0x80; 
 
        private NotificationContext _notificationContext;
        private bool _isAppInitialized; 
        private bool _isIntegratedPipeline;
        private bool _finishPipelineRequestCalled;
        private bool _impersonationEnabled;
 
        internal bool HideRequestResponse;
        internal volatile bool InIndicateCompletion; 
        internal volatile HttpApplication.ThreadContext IndicateCompletionContext = null; 
        // synchronization context (for the newasync pattern)
        private AspNetSynchronizationContext _syncContext; 

        // session state support
        internal bool RequiresSessionState;
        internal bool ReadOnlySessionState; 
        internal bool InAspCompatMode;
 
        /// <include file='doc\HttpContext.uex' path='docs/doc[@for="HttpContext.HttpContext"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the HttpContext class.
        ///    </para>
        /// </devdoc>
        public HttpContext(HttpRequest request, HttpResponse response) { 
            Init(request, response);
            request.Context = this; 
            response.Context = this; 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the HttpContext class. 
        ///    </para>
        /// </devdoc> 
        public HttpContext(HttpWorkerRequest wr) { 
            _wr = wr;
            Init(new HttpRequest(wr, this), new HttpResponse(wr, this)); 
            _response.InitResponseWriter();
        }

        // ctor used in HttpRuntime 
        internal HttpContext(HttpWorkerRequest wr, bool initResponseWriter) {
            _wr = wr; 
            Init(new HttpRequest(wr, this), new HttpResponse(wr, this)); 

            if (initResponseWriter) 
                _response.InitResponseWriter();

            PerfCounters.IncrementCounter(AppPerfCounter.REQUESTS_EXECUTING);
        } 

        private void Init(HttpRequest request, HttpResponse response) { 
            _request = request; 
            _response = response;
            _utcTimestamp = DateTime.UtcNow; 

            if (_wr is IIS7WorkerRequest) {
                _isIntegratedPipeline = true;
            } 

            if (!(_wr is System.Web.SessionState.StateHttpWorkerRequest)) 
                CookielessHelper.RemoveCookielessValuesFromPath(); // This ensures that the cookieless-helper is initialized and 
            // rewrites the path if the URI contains cookieless form-auth ticket, session-id, etc.
 
            Profiler p = HttpRuntime.Profile;
            if (p != null && p.IsEnabled)
                _topTraceContext = new TraceContext(this);
        } 

        // Current HttpContext off the call context 
#if DBG 
        internal static void SetDebugAssertOnAccessToCurrent(bool doAssert) {
            if (doAssert) { 
                CallContext.SetData("__ContextAssert", String.Empty);
            }
            else {
                CallContext.SetData("__ContextAssert", null); 
            }
        } 
 
        private static bool NeedDebugAssertOnAccessToCurrent {
            get { 
                return (CallContext.GetData("__ContextAssert") != null);
            }
        }
#endif 

        /// <devdoc> 
        ///    <para>Returns the current HttpContext object.</para> 
        /// </devdoc>
        public static HttpContext Current { 
            get {
#if DBG
                if (NeedDebugAssertOnAccessToCurrent) {
                    Debug.Assert(ContextBase.Current != null); 
                }
#endif 
                return ContextBase.Current as HttpContext; 
            }
 
            set {
                ContextBase.Current = value;
            }
        } 

        // 
        //  Root / unroot for the duration of async operation 
        //
 
        internal void Root() {
            _root = GCHandle.Alloc(this);
            _ctxPtr = GCHandle.ToIntPtr(_root);
        } 

        internal void Unroot() { 
            if(_root.IsAllocated) { 
                _root.Free();
                _ctxPtr = IntPtr.Zero; 
            }
        }

        internal void FinishPipelineRequest() { 
            if (!_finishPipelineRequestCalled) {
                _finishPipelineRequestCalled = true; 
                HttpRuntime.FinishPipelineRequest(this); 
            }
        } 

        internal IntPtr ContextPtr { get { return _ctxPtr; } }

        internal void ValidatePath() { 
            string physicalPath = _request.PhysicalPathInternal;
 
            // Get the cached path data. If the path is suspicious, GetConfigurationPathData will throw. 
            // If it doesn't throw and returns a CachedPathData, then the path is safe.  However, to
            // be extra cautious, only use this optimization if the path that CachedPathData 
            // used (which it got from MapPath) is the same as what we got from IIS.
            CachedPathData pathData = GetConfigurationPathData();

            // assert that config system successfully obtained a physical path 
            Debug.Assert(pathData.PhysicalPath != null);
 
            if (StringUtil.EqualsIgnoreCase(pathData.PhysicalPath, physicalPath)) { 
                return;
            } 

            // If we're here, the paths were different, which we don't think should ever happen.
            // But if it does, be safe and check the IIS physical path explicitely
            Debug.Assert(false, "ValidationPath couldn't apply optimization, Request.PhysicalPath=" + physicalPath + "; pathData.PhysicalPath=" + pathData.PhysicalPath); 

            FileUtil.CheckSuspiciousPhysicalPath(physicalPath); 
        } 

 
        // IServiceProvider implementation

        /// <internalonly/>
        Object IServiceProvider.GetService(Type service) { 
            Object obj;
 
            if (service == typeof(HttpWorkerRequest)) { 
                InternalSecurityPermissions.UnmanagedCode.Demand();
                obj = _wr; 
            }
            else if (service == typeof(HttpRequest))
                obj = Request;
            else if (service == typeof(HttpResponse)) 
                obj = Response;
            else if (service == typeof(HttpApplication)) 
                obj = ApplicationInstance; 
            else if (service == typeof(HttpApplicationState))
                obj = Application; 
            else if (service == typeof(HttpSessionState))
                obj = Session;
            else if (service == typeof(HttpServerUtility))
                obj = Server; 
            else
                obj = null; 
 
            return obj;
        } 

        //
        // Async app handler is remembered for the duration of execution of the
        // request when application happens to be IHttpAsyncHandler. It is needed 
        // for HttpRuntime to remember the object on which to call OnEndRequest.
        // 
        // The assumption is that application is a IHttpAsyncHandler, not always 
        // HttpApplication.
        // 
        internal IHttpAsyncHandler AsyncAppHandler {
            get { return _asyncAppHandler; }
            set { _asyncAppHandler = value; }
        } 

 
 
        /// <devdoc>
        ///    <para>Retrieves a reference to the application object for the current Http request.</para> 
        /// </devdoc>
        public HttpApplication ApplicationInstance {
            get { return _appInstance;}
            set { 
                // For integrated pipeline, once this is set to a non-null value, it can only be set to null.
                // The setter should never have been made public.  It probably happened in 1.0, before it was possible 
                // to have getter and setter with different accessibility. 
                if (_isIntegratedPipeline && _appInstance != null && value != null) {
                    throw new InvalidOperationException(SR.GetString(SR.Application_instance_cannot_be_changed)); 
                }
                else {
                    _appInstance = value;
                } 
            }
        } 
 

        /// <devdoc> 
        ///    <para>
        ///       Retrieves a reference to the application object for the current
        ///       Http request.
        ///    </para> 
        /// </devdoc>
        public HttpApplicationState Application { 
            get { return HttpApplicationFactory.ApplicationState; } 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Retrieves or assigns a reference to the <see cref='System.Web.IHttpHandler'/> 
        ///       object for the current request.
        ///    </para> 
        /// </devdoc> 
        public IHttpHandler Handler {
            get { return _handler;} 
            set {
                _handler = value;
                RequiresSessionState = false;
                ReadOnlySessionState = false; 
                InAspCompatMode = false;
                if (_handler != null) { 
                    if (_handler is IRequiresSessionState) { 
                        RequiresSessionState = true;
                    } 
                    if (_handler is IReadOnlySessionState) {
                        ReadOnlySessionState = true;
                    }
                    Page page = _handler as Page; 
                    if (page != null && page.IsInAspCompatMode) {
                        InAspCompatMode = true; 
                    } 
                }
            } 
        }


        /// <devdoc> 
        ///    <para>
        ///       Retrieves or assigns a reference to the <see cref='System.Web.IHttpHandler'/> 
        ///       object for the previous handler; 
        ///    </para>
        /// </devdoc> 

        public IHttpHandler PreviousHandler {
            get {
                if (_handlerStack == null || _handlerStack.Count == 0) 
                    return null;
 
                return (IHttpHandler)_handlerStack.Peek(); 
            }
        } 


        /// <devdoc>
        ///    <para> 
        ///       Retrieves or assigns a reference to the <see cref='System.Web.IHttpHandler'/>
        ///       object for the current executing handler; 
        ///    </para> 
        /// </devdoc>
        private IHttpHandler _currentHandler = null; 

        public IHttpHandler CurrentHandler {
            get {
                if (_currentHandler == null) 
                    _currentHandler = _handler;
 
                return _currentHandler; 
            }
        } 

        internal void RestoreCurrentHandler() {
            _currentHandler = (IHttpHandler)_handlerStack.Pop();
        } 

        internal void SetCurrentHandler(IHttpHandler newtHandler) { 
            if (_handlerStack == null) { 
                _handlerStack = new Stack();
            } 
            _handlerStack.Push(CurrentHandler);

            _currentHandler = newtHandler;
        } 

 
        /// <devdoc> 
        ///    <para>
        ///       Retrieves a reference to the target <see cref='System.Web.HttpRequest'/> 
        ///       object for the current request.
        ///    </para>
        /// </devdoc>
        public HttpRequest Request { 
            get {
                 if (HideRequestResponse) 
                    throw new HttpException(SR.GetString(SR.Request_not_available)); 
                return _request;
            } 
        }


        /// <devdoc> 
        ///    <para>
        ///       Retrieves a reference to the <see cref='System.Web.HttpResponse'/> 
        ///       object for the current response. 
        ///    </para>
        /// </devdoc> 
        public HttpResponse Response {
            get {
                if (HideRequestResponse)
                    throw new HttpException(SR.GetString(SR.Response_not_available)); 
                return _response;
            } 
        } 

        internal IHttpHandler TopHandler { 
            get {
                if (_handlerStack == null) {
                    return _handler;
                } 
                object[] handlers = _handlerStack.ToArray();
                if (handlers == null || handlers.Length == 0) { 
                    return _handler; 
                }
                return (IHttpHandler)handlers[handlers.Length - 1]; 
            }
        }

 
        /// <devdoc>
        /// <para>Retrieves a reference to the <see cref='System.Web.TraceContext'/> object for the current 
        ///    response.</para> 
        /// </devdoc>
        public TraceContext Trace { 
            get {
                if (_topTraceContext == null)
                    _topTraceContext = new TraceContext(this);
                return _topTraceContext; 
            }
        } 
 
        internal bool TraceIsEnabled {
            get { 
                if (_topTraceContext == null)
                    return false;

                return _topTraceContext.IsEnabled; 
            }
            set { 
                if (value) 
                    _topTraceContext = new TraceContext(this);
            } 

        }

 

        /// <devdoc> 
        ///    <para> 
        ///       Retrieves a key-value collection that can be used to
        ///       build up and share data between an <see cref='System.Web.IHttpModule'/> and an <see cref='System.Web.IHttpHandler'/> 
        ///       during a
        ///       request.
        ///    </para>
        /// </devdoc> 
        public IDictionary Items {
            get { 
                if (_items == null) 
                    _items = new Hashtable();
 
                return _items;
            }
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       Gets a reference to the <see cref='System.Web.SessionState'/> instance for the current request.
        ///    </para> 
        /// </devdoc>
        public HttpSessionState Session {
            get {
                if (_sessionStateModule != null) { 
                    lock (this) {
                        if (_sessionStateModule != null) { 
                            // If it's not null, it means we have a delayed session state item 
                            _sessionStateModule.InitStateStoreItem(true);
                            _sessionStateModule = null; 
                        }
                    }
                }
 
                return(HttpSessionState)Items[SessionStateUtility.SESSION_KEY];
            } 
        } 

        internal void AddDelayedHttpSessionState(SessionStateModule module) { 
            if (_sessionStateModule != null) {
                throw new HttpException(SR.GetString(SR.Cant_have_multiple_session_module));
            }
            _sessionStateModule = module; 
        }
 
        internal void RemoveDelayedHttpSessionState() { 
            Debug.Assert(_sessionStateModule != null, "_sessionStateModule != null");
            _sessionStateModule = null; 
        }


        /// <devdoc> 
        ///    <para>
        ///       Gets a reference to the <see cref='System.Web.HttpServerUtility'/> 
        ///       for the current 
        ///       request.
        ///    </para> 
        /// </devdoc>
        public HttpServerUtility Server {
            get {
                // create only on demand 
                if (_server == null)
                    _server = new HttpServerUtility(this); 
                return _server; 
            }
        } 

        // if the context has an error, report it, but only one time
        internal void ReportRuntimeErrorIfExists(ref RequestNotificationStatus status) {
            Exception e = Error; 

            if (e == null || _runtimeErrorReported) { 
                return; 
            }
 
            // WOS 1921799: custom errors don't work in integrated mode if there's an initialization exception
            if (_notificationContext != null && CurrentModuleIndex == -1) {
                try {
                    IIS7WorkerRequest wr = _wr as IIS7WorkerRequest; 
                    if (Request.QueryString["aspxerrorpath"] != null
                        && wr != null 
                        && String.IsNullOrEmpty(wr.GetManagedHandlerType()) 
                        && wr.GetCurrentModuleName() == PipelineRuntime.InitExceptionModuleName) {
                        status = RequestNotificationStatus.Continue;   // allow non-managed handler to execute request 
                        return;
                    }
                }
                catch { 
                }
            } 
 
            _runtimeErrorReported = true;
 
            if (HttpRuntime.AppOfflineMessage != null) {
                try {
                    // report app offline error
                    Response.StatusCode = 404; 
                    Response.TrySkipIisCustomErrors = true;
                    Response.OutputStream.Write(HttpRuntime.AppOfflineMessage, 0, HttpRuntime.AppOfflineMessage.Length); 
                } 
                catch {
                } 
            }
            else {
                // report error exception
                using (new HttpContextWrapper(this)) { 
                    // when application is on UNC share the code below must
                    // be run while impersonating the token given by IIS 
                    using (new ApplicationImpersonationContext()) { 

                        try { 
                            try {
                                // try to report error in a way that could possibly throw (a config exception)
                                Response.ReportRuntimeError(e, true /*canThrow*/, false);
                            } 
                            catch (Exception eReport) {
                                // report the config error in a way that would not throw 
                                Response.ReportRuntimeError(eReport, false /*canThrow*/, false); 
                            }
                        } 
                        catch (Exception) {
                        }
                    }
                } 
            }
 
            status = RequestNotificationStatus.FinishRequest; 
            return;
        } 

        /// <devdoc>
        ///    <para>
        ///       Gets the 
        ///       first error (if any) accumulated during request processing.
        ///    </para> 
        /// </devdoc> 
        public Exception Error {
            get { 
                if (_tempError != null)
                    return _tempError;
                if (_errors == null || _errors.Count == 0 || _errorCleared)
                    return null; 
                return (Exception)_errors[0];
            } 
        } 

        // 
        // Temp error (yet to be caught on app level)
        // to be reported as Server.GetLastError() but could be cleared later
        //
        internal Exception TempError { 
            get { return _tempError; }
            set { _tempError = value; } 
        } 

 
        /// <devdoc>
        ///    <para>
        ///       An array (collection) of errors accumulated while processing a
        ///       request. 
        ///    </para>
        /// </devdoc> 
        public Exception[] AllErrors { 
            get {
                int n = (_errors != null) ? _errors.Count : 0; 

                if (n == 0)
                    return null;
 
                Exception[] errors = new Exception[n];
                _errors.CopyTo(0, errors, 0, n); 
                return errors; 
            }
        } 


        /// <devdoc>
        ///    <para> 
        ///       Registers an error for the current request.
        ///    </para> 
        /// </devdoc> 
        public void AddError(Exception errorInfo) {
            if (_errors == null) 
                _errors = new ArrayList();

            _errors.Add(errorInfo);
 
            if (_isIntegratedPipeline && _notificationContext != null) {
                // set the error on the current notification context 
                _notificationContext.Error = errorInfo; 
            }
        } 


        /// <devdoc>
        ///    <para> 
        ///       Clears all errors for the current request.
        ///    </para> 
        /// </devdoc> 
        public void ClearError() {
            if (_tempError != null) 
                _tempError = null;
            else
                _errorCleared = true;
 
            if (_isIntegratedPipeline && _notificationContext != null) {
                // clear the error on the current notification context 
                _notificationContext.Error = null; 
            }
        } 


        /// <devdoc>
        ///    <para> 
        ///       IPrincipal security information.
        ///    </para> 
        /// </devdoc> 
        public IPrincipal User {
            get { return _user; } 

            [SecurityPermission(SecurityAction.Demand, ControlPrincipal=true)]
            set {
                SetPrincipalNoDemand(value); 
            }
        } 
 
        // route all internals call to the principal (that don't have luring attacks)
        // through this method so we can centralize reporting 
        // Before this, some auth modules were assigning directly to _user
        internal void SetPrincipalNoDemand(IPrincipal principal, bool needToSetNativePrincipal) {
            _user = principal;
 
            // push changes through to native side
            if (needToSetNativePrincipal 
                && _isIntegratedPipeline 
                && _notificationContext.CurrentNotification == RequestNotification.AuthenticateRequest) {
 
                IntPtr pManagedPrincipal = IntPtr.Zero;
                try {
                    IIS7WorkerRequest wr = _wr as IIS7WorkerRequest;
                    if (principal != null) { 
                        GCHandle h = GCHandle.Alloc(principal);
                        try { 
                            pManagedPrincipal = GCHandle.ToIntPtr(h); 
                            wr.SetPrincipal(principal, pManagedPrincipal);
                        } 
                        catch {
                            pManagedPrincipal = IntPtr.Zero;
                            if (h.IsAllocated) {
                                h.Free(); 
                            }
                            throw; 
                        } 
                    }
                    else { 
                        wr.SetPrincipal(null, IntPtr.Zero);
                    }
                }
                finally { 
                    if (_pManagedPrincipal != IntPtr.Zero) {
                        GCHandle h = GCHandle.FromIntPtr(_pManagedPrincipal); 
                        if (h.IsAllocated) { 
                            h.Free();
                        } 
                    }
                    _pManagedPrincipal = pManagedPrincipal;
                }
            } 
        }
 
        internal void SetPrincipalNoDemand(IPrincipal principal) { 
            SetPrincipalNoDemand(principal, true /*needToSetNativePrincipal*/);
        } 

        internal bool _ProfileDelayLoad = false;

        public ProfileBase  Profile { 
            get {
                if (_Profile == null && _ProfileDelayLoad) 
                    _Profile = ProfileBase.Create(Request.IsAuthenticated ? User.Identity.Name : Request.AnonymousID, Request.IsAuthenticated); 
                return _Profile;
            } 
        }


        public bool SkipAuthorization { 
            get { return _skipAuthorization;}
 
            [SecurityPermission(SecurityAction.Demand, ControlPrincipal=true)] 
            set {
                SetSkipAuthorizationNoDemand(value, false); 
            }
        }

        internal void SetSkipAuthorizationNoDemand(bool value, bool managedOnly) 
        {
            if (HttpRuntime.UseIntegratedPipeline 
                && !managedOnly 
                && value != _skipAuthorization) {
 
                // For integrated mode, persist changes to SkipAuthorization
                // in the IS_LOGIN_PAGE server variable.  When this server variable exists
                // and the value is not "0", IIS skips authorization.
 
                _request.SetSkipAuthorization(value);
            } 
 
            _skipAuthorization = value;
        } 

        /// <devdoc>
        ///    <para>
        ///       Is this request in debug mode? 
        ///    </para>
        /// </devdoc> 
        public bool IsDebuggingEnabled { 
            get {
                try { 
                    return CompilationUtil.IsDebuggingEnabled(this);
                }
                catch {
                    // in case of config errors don't throw 
                    return false;
                } 
            } 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Is this custom error enabled for this request? 
        ///    </para>
        /// </devdoc> 
        public bool IsCustomErrorEnabled { 
            get {
                return CustomErrorsSection.GetSettings(this).CustomErrorsEnabled(_request); 
            }
        }

        internal TemplateControl TemplateControl { 
            get {
                return _templateControl; 
            } 
            set {
                _templateControl = value; 
            }
        }

 
        /// <devdoc>
        ///    <para>Gets the initial timestamp of the current request.</para> 
        /// </devdoc> 
        public DateTime Timestamp {
            get { return _utcTimestamp.ToLocalTime();} 
        }

        internal DateTime UtcTimestamp {
            get { return _utcTimestamp;} 
        }
 
        internal HttpWorkerRequest WorkerRequest { 
            get { return _wr;}
        } 


        /// <devdoc>
        ///    <para> 
        ///       Gets a reference to the System.Web.Cache.Cache object for the current request.
        ///    </para> 
        /// </devdoc> 
        public Cache Cache {
            get { return HttpRuntime.Cache;} 
        }
#if SITECOUNTERS

        /// <devdoc> 
        ///    <para>
        ///       Gets a reference to the System.Web.SiteCounters object. 
        ///    </para> 
        /// </devdoc>
        public SiteCounters SiteCounters { 
            get { return HttpRuntime.SiteCounters;}
        }
#endif
        /* 
         * The virtual path used to get config settings.  This allows the user
         * to specify a non default config path, without having to pass it to every 
         * configuration call. 
         */
        internal VirtualPath ConfigurationPath { 
            get {
                if (_configurationPath == null)
                    _configurationPath = _request.FilePathObject;
 
                return _configurationPath;
            } 
 
            set {
                _configurationPath = value; 
                if (_configurationPathData != null) {
                    if (!_configurationPathData.CompletedFirstRequest) {
                        CachedPathData.RemoveBadPathData(_configurationPathData);
                    } 

                    _configurationPathData = null; 
                } 

                if (_filePathData != null) { 
                    if (!_filePathData.CompletedFirstRequest) {
                        CachedPathData.RemoveBadPathData(_filePathData);
                    }
 
                    _filePathData = null;
                } 
            } 
        }
 
        internal CachedPathData GetFilePathData() {
            if (_filePathData == null) {
                _filePathData = CachedPathData.GetVirtualPathData(_request.FilePathObject, false);
            } 

            return _filePathData; 
        } 

        internal CachedPathData GetConfigurationPathData() { 
            if (_configurationPath == null) {
                return GetFilePathData();
            }
 
            //
            if (_configurationPathData == null) { 
                _configurationPathData = CachedPathData.GetVirtualPathData(_configurationPath, true); 
            }
 
            return _configurationPathData;
        }

        internal CachedPathData GetPathData(VirtualPath path) { 
            if (path != null) {
                if (path.Equals(_request.FilePathObject)) { 
                    return GetFilePathData(); 
                }
 
                if (_configurationPath != null && path.Equals(_configurationPath)) {
                    return GetConfigurationPathData();
                }
            } 

            return CachedPathData.GetVirtualPathData(path, false); 
        } 

        internal void FirstNotificationInit() { 
            // called once during the first notification for a request
            if (!_firstNotificationInitCalled) {
                _firstNotificationInitCalled = true;
                ValidatePath(); 
            }
        } 
 
        internal void FinishRequestForCachedPathData(int statusCode) {
            // Remove the cached path data for a file path if the first request for it 
            // does not succeed due to a bad request. Otherwise we could be vulnerable
            // to a DOS attack.
            if (_filePathData != null && !_filePathData.CompletedFirstRequest) {
                if (400 <= statusCode && statusCode < 500) { 
                    CachedPathData.RemoveBadPathData(_filePathData);
                } 
                else { 
                    CachedPathData.MarkCompleted(_filePathData);
                } 
            }
        }

        /* 
         * Uses the Config system to get the specified configuraiton
         */ 
        [Obsolete("The recommended alternative is System.Web.Configuration.WebConfigurationManager.GetWebApplicationSection in System.Web.dll. http://go.microsoft.com/fwlink/?linkid=14202")] 
        public static object GetAppConfig(String name) {
            return WebConfigurationManager.GetWebApplicationSection(name); 
        }

        [Obsolete("The recommended alternative is System.Web.HttpContext.GetSection in System.Web.dll. http://go.microsoft.com/fwlink/?linkid=14202")]
        public object GetConfig(String name) { 
            return GetSection(name);
        } 
 
        public object GetSection(String sectionName) {
            if (HttpConfigurationSystem.UseHttpConfigurationSystem) { 
                return GetConfigurationPathData().ConfigRecord.GetSection(sectionName);
            }
            else {
                return ConfigurationManager.GetSection(sectionName); 
            }
        } 
 
        internal RuntimeConfig GetRuntimeConfig() {
            return GetConfigurationPathData().RuntimeConfig; 
        }

        internal RuntimeConfig GetRuntimeConfig(VirtualPath path) {
            return GetPathData(path).RuntimeConfig; 
        }
 
        public void RewritePath(String path) { 
            RewritePath(path, true);
        } 

        /*
         * Called by the URL rewrite module to modify the path for downstream modules
         */ 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public void RewritePath(String path, bool rebaseClientPath) { 
            if (path == null)
                throw new ArgumentNullException("path");

            // extract query string 
            String qs = null;
            int iqs = path.IndexOf('?'); 
            if (iqs >= 0) { 
                qs = (iqs < path.Length-1) ? path.Substring(iqs+1) : String.Empty;
                path = path.Substring(0, iqs); 
            }

            // resolve relative path
            VirtualPath virtualPath = VirtualPath.Create(path); 
            virtualPath = Request.FilePathObject.Combine(virtualPath);
 
            // disallow paths outside of app 
            virtualPath.FailIfNotWithinAppRoot();
 
            // clear things that depend on path
            ConfigurationPath = null;

            // rewrite path on request 
            Request.InternalRewritePath(virtualPath, qs, rebaseClientPath);
        } 
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void RewritePath(String filePath, String pathInfo, String queryString) {
            RewritePath(VirtualPath.CreateAllowNull(filePath), VirtualPath.CreateAllowNull(pathInfo), 
                queryString, false /*setClientFilePath*/);
        } 
        public void RewritePath(string filePath, string pathInfo, String queryString, bool setClientFilePath) 
        {
            RewritePath(VirtualPath.CreateAllowNull(filePath), VirtualPath.CreateAllowNull(pathInfo), queryString, setClientFilePath); 
        }
        internal void RewritePath(VirtualPath filePath, VirtualPath pathInfo, String queryString, bool setClientFilePath) {
            if (filePath == null)
                throw new ArgumentNullException("filePath"); 

            // resolve relative path 
            filePath = Request.FilePathObject.Combine(filePath); 

            // disallow paths outside of app 
            filePath.FailIfNotWithinAppRoot();

            // clear things that depend on path
            ConfigurationPath = null; 

            // rewrite path on request 
            Request.InternalRewritePath(filePath, pathInfo, queryString, setClientFilePath); 
        }
 
        internal CultureInfo DynamicCulture {
            get { return _dynamicCulture; }
            set { _dynamicCulture = value; }
        } 

        internal CultureInfo DynamicUICulture { 
            get { return _dynamicUICulture; } 
            set { _dynamicUICulture = value; }
        } 

        public static object GetGlobalResourceObject(string classKey, string resourceKey) {
            return GetGlobalResourceObject(classKey, resourceKey, null);
        } 

        public static object GetGlobalResourceObject(string classKey, string resourceKey, CultureInfo culture) { 
            return ResourceExpressionBuilder.GetGlobalResourceObject(classKey, resourceKey, null, null, culture); 
        }
 
        public static object GetLocalResourceObject(string virtualPath, string resourceKey) {
            return GetLocalResourceObject(virtualPath, resourceKey, null);
        }
 
        public static object GetLocalResourceObject(string virtualPath, string resourceKey, CultureInfo culture) {
            IResourceProvider pageProvider = ResourceExpressionBuilder.GetLocalResourceProvider( 
                VirtualPath.Create(virtualPath)); 
            return ResourceExpressionBuilder.GetResourceObject(pageProvider, resourceKey, culture);
        } 

        internal int ServerExecuteDepth {
            get { return _serverExecuteDepth; }
            set { _serverExecuteDepth = value; } 
        }
 
        internal bool PreventPostback { 
            get { return _preventPostback; }
            set { _preventPostback = value; } 
        }

        //
        // Timeout support 
        //
 
        internal Thread CurrentThread { 
            get {
                return _thread; 
            }
            set {
                _thread = value;
            } 
        }
 
        internal TimeSpan Timeout { 
            get {
                EnsureTimeout(); 
                return _timeout;
            }

            set { 
                _timeout = value;
                _timeoutSet = true; 
            } 
        }
 
        internal void EnsureTimeout() {
            // Ensure that calls to Timeout property will not go to config after this call
            if (!_timeoutSet) {
                HttpRuntimeSection cfg = RuntimeConfig.GetConfig(this).HttpRuntime; 
                int s = (int) cfg.ExecutionTimeout.TotalSeconds;
                _timeout = new TimeSpan(0, 0, s); 
                _timeoutSet = true; 
            }
        } 

        internal DoubleLink TimeoutLink {
            get { return _timeoutLink;}
            set { _timeoutLink = value;} 
        }
 
        /* 

        Notes on the following 5 functions: 

        Execution can be cancelled only during certain periods, when inside the catch
        block for ThreadAbortException.  These periods are marked with the value of
        _timeoutState of 1. 

        There is potential [rare] race condition when the timeout thread would call 
        thread.abort but the execution logic in the meantime escapes the catch block. 
        To avoid such race conditions _timeoutState of -1 (cancelled) is introduced.
        The timeout thread sets _timeoutState to -1 before thread abort and the 
        unwinding logic just waits for the exception in this case. The wait cannot
        be done in EndCancellablePeriod because the function is call from inside of
        a finally block and thus would wait indefinetely. That's why another function
        WaitForExceptionIfCancelled had been added. 

        Originally _timeoutStartTime was set in BeginCancellablePeriod. However, that means 
        we'll call UtcNow everytime we call ExecuteStep, which is too expensive. So to save 
        CPU time we created a new method SetStartTime() which is called by the caller of
        ExecuteStep. 

        */

        internal void BeginCancellablePeriod() { 
            // It could be caused by an exception in OnThreadStart
            if (_timeoutStartTime == DateTime.MinValue) { 
                SetStartTime(); 
            }
 
            _timeoutState = 1;
        }

        internal void SetStartTime() { 
            _timeoutStartTime = DateTime.UtcNow;
        } 
 
        internal void EndCancellablePeriod() {
            Interlocked.CompareExchange(ref _timeoutState, 0, 1); 
        }

        internal void WaitForExceptionIfCancelled() {
            while (_timeoutState == -1) 
                Thread.Sleep(100);
        } 
 
        internal bool IsInCancellablePeriod {
            get { return (_timeoutState == 1); } 
        }

        internal Thread MustTimeout(DateTime utcNow) {
            if (_timeoutState == 1) {  // fast check 
                if (TimeSpan.Compare(utcNow.Subtract(_timeoutStartTime), Timeout) >= 0) {
                    // don't abort in debug mode 
                    try { 
                        if (CompilationUtil.IsDebuggingEnabled(this) || System.Diagnostics.Debugger.IsAttached)
                            return null; 
                    }
                    catch {
                        // ignore config errors
                        return null; 
                    }
 
                    // abort the thread only if in cancelable state, avoiding race conditions 
                    // the caller MUST timeout if the return is true
                    if (Interlocked.CompareExchange(ref _timeoutState, -1, 1) == 1) 
                        return _thread;
                }
            }
 
            return null;
        } 
 
        // call a delegate within cancellable period (possibly throwing timeout exception)
        internal void InvokeCancellableCallback(WaitCallback callback, Object state) { 
            if (IsInCancellablePeriod) {
                // call directly
                callback(state);
                return; 
            }
 
            try { 
                BeginCancellablePeriod();  // request can be cancelled from this point
 
                try {
                    callback(state);
                }
                finally { 
                    EndCancellablePeriod();  // request can be cancelled until this point
                } 
 
                WaitForExceptionIfCancelled();  // wait outside of finally
            } 
            catch (ThreadAbortException e) {
                if (e.ExceptionState != null &&
                    e.ExceptionState is HttpApplication.CancelModuleException &&
                    ((HttpApplication.CancelModuleException)e.ExceptionState).Timeout) { 

                    Thread.ResetAbort(); 
                    PerfCounters.IncrementCounter(AppPerfCounter.REQUESTS_TIMED_OUT); 

                    throw new HttpException(SR.GetString(SR.Request_timed_out), 
                                        null, WebEventCodes.RuntimeErrorRequestAbort);
                }
            }
        } 

        internal void PushTraceContext() { 
            if (_traceContextStack == null) { 
                _traceContextStack = new Stack();
            } 

            // push current TraceContext on stack
            _traceContextStack.Push(_topTraceContext);
 
            // now make a new one for the top if necessary
            if (_topTraceContext != null) { 
                TraceContext tc = new TraceContext(this); 
                _topTraceContext.CopySettingsTo(tc);
                _topTraceContext = tc; 
            }
        }

        internal void PopTraceContext() { 
            Debug.Assert(_traceContextStack != null);
            _topTraceContext = (TraceContext) _traceContextStack.Pop(); 
        } 

        internal bool RequestRequiresAuthorization()  { 
#if !FEATURE_PAL // FEATURE_PAL does not enable IIS-based hosting features
            // if current user is anonymous, then trivially, this page does not require authorization
            if (!User.Identity.IsAuthenticated)
                return false; 

            // Ask each of the authorization modules 
            return 
                ( FileAuthorizationModule.RequestRequiresAuthorization(this) ||
                  UrlAuthorizationModule.RequestRequiresAuthorization(this)   ); 
#else // !FEATURE_PAL
                return false; // ROTORTODO
#endif // !FEATURE_PAL
        } 

        internal int CallISAPI(UnsafeNativeMethods.CallISAPIFunc iFunction, byte [] bufIn, byte [] bufOut) { 
 
            if (_wr == null || !(_wr is System.Web.Hosting.ISAPIWorkerRequest))
                throw new HttpException(SR.GetString(SR.Cannot_call_ISAPI_functions)); 
#if !FEATURE_PAL // FEATURE_PAL does not enable IIS-based hosting features
            return ((System.Web.Hosting.ISAPIWorkerRequest) _wr).CallISAPI(iFunction, bufIn, bufOut);
#else // !FEATURE_PAL
                throw new NotImplementedException ("ROTORTODO"); 
#endif // !FEATURE_PAL
        } 
 
        internal void SendEmptyResponse() {
#if !FEATURE_PAL // FEATURE_PAL does not enable IIS-based hosting features 
            if (_wr != null  && (_wr is System.Web.Hosting.ISAPIWorkerRequest))
                ((System.Web.Hosting.ISAPIWorkerRequest) _wr).SendEmptyResponse();
#endif // !FEATURE_PAL
        } 

        private  CookielessHelperClass _CookielessHelper; 
        internal CookielessHelperClass  CookielessHelper { 
            get {
                if (_CookielessHelper == null) 
                    _CookielessHelper = new CookielessHelperClass(this);
                return _CookielessHelper;
            }
        } 

 
        // When a thread enters the pipeline, we may need to set the cookie in the CallContext. 
        internal void ResetSqlDependencyCookie() {
            if (_sqlDependencyCookie != null) { 
                System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(SqlCacheDependency.SQL9_OUTPUT_CACHE_DEPENDENCY_COOKIE, _sqlDependencyCookie);
            }
        }
 
        // When a thread leaves the pipeline, we may need to remove the cookie from the CallContext.
        internal void RemoveSqlDependencyCookie() { 
            if (_sqlDependencyCookie != null) { 
                System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(SqlCacheDependency.SQL9_OUTPUT_CACHE_DEPENDENCY_COOKIE, null);
            } 
        }

        internal string SqlDependencyCookie {
            get { 
                return _sqlDependencyCookie;
            } 
 
            set {
                _sqlDependencyCookie = value; 
                System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(SqlCacheDependency.SQL9_OUTPUT_CACHE_DEPENDENCY_COOKIE, value);
            }
        }
 
        //
        // integrated pipeline related 
        // 
        internal NotificationContext NotificationContext {
            get { return _notificationContext; } 
            set { _notificationContext = value; }
        }

        public RequestNotification CurrentNotification { 
            get {
                if (!HttpRuntime.UseIntegratedPipeline) { 
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode)); 
                }
 
                return _notificationContext.CurrentNotification;
            }
            internal set {
                if (!HttpRuntime.UseIntegratedPipeline) { 
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode));
                } 
 
                _notificationContext.CurrentNotification = value;
            } 
        }

        internal bool IsChangeInServerVars {
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_CHANGE_IN_SERVER_VARIABLES) == FLAG_CHANGE_IN_SERVER_VARIABLES; } 
        }
 
        internal bool IsChangeInRequestHeaders { 
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_CHANGE_IN_REQUEST_HEADERS) == FLAG_CHANGE_IN_REQUEST_HEADERS; }
        } 

        internal bool IsChangeInResponseHeaders {
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_CHANGE_IN_RESPONSE_HEADERS) == FLAG_CHANGE_IN_RESPONSE_HEADERS; }
        } 

        internal bool IsChangeInResponseStatus { 
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_CHANGE_IN_RESPONSE_STATUS) == FLAG_CHANGE_IN_RESPONSE_STATUS; } 
        }
 
        internal bool IsChangeInUserPrincipal {
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_CHANGE_IN_USER_OBJECT) == FLAG_CHANGE_IN_USER_OBJECT; }
        }
 
        internal bool IsSendResponseHeaders {
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_SEND_RESPONSE_HEADERS) == FLAG_SEND_RESPONSE_HEADERS; } 
        } 

        internal void SetImpersonationEnabled() { 
            IdentitySection c = RuntimeConfig.GetConfig(this).Identity;
            _impersonationEnabled = (c != null && c.Impersonate);
        }
 
        internal bool UsesImpersonation {
            get { 
                // if we're on a UNC share and we have a UNC token, then use impersonation for all notifications 
                if (HttpRuntime.IsOnUNCShareInternal && HostingEnvironment.ApplicationIdentityToken != IntPtr.Zero) {
                    return true; 
                }
                // if <identity impersonate=/> is false, then don't use impersonation
                if (!_impersonationEnabled) {
                    return false; 
                }
                // if this notification is after AuthenticateRequest and not a SendResponse notification, use impersonation 
                return ( ( (_notificationContext.CurrentNotification == RequestNotification.AuthenticateRequest && _notificationContext.IsPostNotification) 
                           || _notificationContext.CurrentNotification > RequestNotification.AuthenticateRequest )
                         && _notificationContext.CurrentNotification != RequestNotification.SendResponse ); 
            }
        }

        internal bool AreResponseHeadersSent { 
            get { return (_notificationContext.CurrentNotificationFlags & FLAG_RESPONSE_HEADERS_SENT) == FLAG_RESPONSE_HEADERS_SENT; }
        } 
 
        internal bool NeedToInitializeApp() {
            bool needToInit = !_isAppInitialized; 
            if (needToInit) {
                _isAppInitialized = true;
            }
            return needToInit; 
        }
 
        // flags passed in on the call to PipelineRuntime::ProcessRequestNotification 
        internal int CurrentNotificationFlags {
            get { 
                return _notificationContext.CurrentNotificationFlags;
            }
            set {
                _notificationContext.CurrentNotificationFlags = value; 
            }
        } 
 
        // index of the current "module" running the request
        // into the application module array 
        internal int CurrentModuleIndex {
            get {
                return _notificationContext.CurrentModuleIndex;
            } 
            set {
                _notificationContext.CurrentModuleIndex = value; 
            } 
        }
 
        // Each module has a PipelineModuleStepContainer
        // which stores/manages a list of event handlers
        // that correspond to each RequestNotification.
        // CurrentModuleEventIndex is the index (for the current 
        // module) of the current event handler.
        // This will be greater than one when a single 
        // module registers multiple delegates for a single event. 
        // e.g.
        // app.BeginRequest += Foo; 
        // app.BeginRequest += Bar;
        internal int CurrentModuleEventIndex {
            get {
                return _notificationContext.CurrentModuleEventIndex; 
            }
            set { 
                _notificationContext.CurrentModuleEventIndex = value; 
            }
        } 

        internal void DisableNotifications(RequestNotification notifications, RequestNotification postNotifications) {
            IIS7WorkerRequest wr = _wr as IIS7WorkerRequest;
            if (null != wr) { 
                wr.DisableNotifications(notifications, postNotifications);
            } 
        } 

        // if the principal is derived from WindowsIdentity 
        // there may be a dup'ed token here that we should dispose
        // of as quickly as possible
        internal void DisposePrincipal() {
            if (_pManagedPrincipal == IntPtr.Zero 
                && _user != null
                && _user != WindowsAuthenticationModule.AnonymousPrincipal) { 
                WindowsIdentity id = _user.Identity as WindowsIdentity; 
                if (id != null) {
                    _user = null; 
                    id.Dispose();
                }
            }
        } 

        public bool IsPostNotification { 
            get { 
                if (!HttpRuntime.UseIntegratedPipeline) {
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode)); 
                }
                return _notificationContext.IsPostNotification;
            }
            internal set { 
                if (!HttpRuntime.UseIntegratedPipeline) {
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode)); 
                } 
                _notificationContext.IsPostNotification = value;
            } 

        }

        // user token for the request 
        internal IntPtr ClientIdentityToken {
            get { 
                if (_wr != null) { 
                    return _wr.GetUserToken();
                } 
                else {
                    return IntPtr.Zero;
                }
            } 
        }
 
        // is configured to impersonate client? 
        internal bool IsClientImpersonationConfigured {
            get { 
                try {
                    IdentitySection c = RuntimeConfig.GetConfig(this).Identity;
                    return (c != null && c.Impersonate && c.ImpersonateToken == IntPtr.Zero);
                } 
                catch {
                    // this property should not throw as it is used in the error reporting pass 
                    // config errors will be reported elsewhere 
                    return false;
                } 
            }
        }

        internal IntPtr ImpersonationToken { 
            get {
                // by default use app identity 
                IntPtr token = HostingEnvironment.ApplicationIdentityToken; 
                IdentitySection c = RuntimeConfig.GetConfig(this).Identity;
                if (c != null) { 
                    if (c.Impersonate) {
                        token = (c.ImpersonateToken != IntPtr.Zero) ? c.ImpersonateToken : ClientIdentityToken;
                    }
                    else { 
                        // for non-UNC case impersonate="false" means "don't impersonate",
                        // but there is a special case for UNC shares - even if 
                        // impersonate="false" we still impersonate the UNC identity 
                        // (hosting identity). and this is how v1.x works as well
                        if (!HttpRuntime.IsOnUNCShareInternal) { 
                            token = IntPtr.Zero;
                        }
                    }
                } 
                return token;
            } 
        } 

        internal AspNetSynchronizationContext SyncContext { 
            get {
                if (_syncContext == null) {
                    _syncContext = new AspNetSynchronizationContext(ApplicationInstance);
                } 

                return _syncContext; 
            } 
        }
 
        internal AspNetSynchronizationContext InstallNewAspNetSynchronizationContext() {
            AspNetSynchronizationContext syncContext = _syncContext;

            if (syncContext != null && syncContext == AsyncOperationManager.SynchronizationContext) { 
                // using current ASP.NET synchronization context - switch it
                _syncContext = new AspNetSynchronizationContext(ApplicationInstance); 
                AsyncOperationManager.SynchronizationContext = _syncContext; 
                return syncContext;
            } 

            return null;
        }
 
        internal void RestoreSavedAspNetSynchronizationContext(AspNetSynchronizationContext syncContext) {
            AsyncOperationManager.SynchronizationContext = syncContext; 
            _syncContext = syncContext; 
        }
 
        internal string UserLanguageFromContext() {
            if(Request != null && Request.UserLanguages != null) {
                string userLanguageEntry = Request.UserLanguages[0];
                if(userLanguageEntry != null) { 
                    int loc = userLanguageEntry.IndexOf(';');
                    if(loc != -1) { 
                        return userLanguageEntry.Substring(0, loc); 
                    }
                    else { 
                        return userLanguageEntry;
                    }
                }
            } 
            return null;
        } 
 
        // References should be nulled a.s.a.p. to reduce working set
        internal void ClearReferences() { 
            _appInstance = null;
            _handler = null;
            _handlerStack = null;
            _currentHandler = null; 
            if (_isIntegratedPipeline) {
                _items = null; 
                _syncContext = null; 
            }
        } 

        internal CultureInfo CultureFromConfig(string configString, bool requireSpecific) {
            //auto
            if(StringUtil.EqualsIgnoreCase(configString, HttpApplication.AutoCulture)) { 
                string userLanguage = UserLanguageFromContext();
                if (userLanguage != null) { 
                    try { 
                        if (requireSpecific) {
                            return HttpServerUtility.CreateReadOnlySpecificCultureInfo(userLanguage); 
                        }
                        else {
                            return HttpServerUtility.CreateReadOnlyCultureInfo(userLanguage);
                        } 
                    }
                    catch { 
                        return null; 
                    }
                } 
                else {
                    return null;
                }
            } 
            else if(StringUtil.StringStartsWithIgnoreCase(configString, "auto:")) {
                string userLanguage = UserLanguageFromContext(); 
                if(userLanguage != null) { 
                    try {
                        if(requireSpecific) { 
                            return HttpServerUtility.CreateReadOnlySpecificCultureInfo(userLanguage);
                        }
                        else {
                            return HttpServerUtility.CreateReadOnlyCultureInfo(userLanguage); 
                        }
                    } 
                    catch { 
                        if(requireSpecific) {
                            return HttpServerUtility.CreateReadOnlySpecificCultureInfo(HttpApplication.GetFallbackCulture(configString)); 
                        }
                        else {
                            return HttpServerUtility.CreateReadOnlyCultureInfo(HttpApplication.GetFallbackCulture(configString));
                        } 
                    }
                } 
                else { 
                    if(requireSpecific) {
                        return HttpServerUtility.CreateReadOnlySpecificCultureInfo(configString.Substring(5)); 
                    }
                    else {
                        return HttpServerUtility.CreateReadOnlyCultureInfo(configString.Substring(5));
                    } 
                }
            } 
            if(requireSpecific) { 
                return HttpServerUtility.CreateReadOnlySpecificCultureInfo(configString);
            } 
            else {
                return HttpServerUtility.CreateReadOnlyCultureInfo(configString);
            }
        } 
    }
 
    // 
    // Helper class to add/remove HttpContext to/from CallContext
    // 
    // using (new HttpContextWrapper(context)) {
    //     // this code will have HttpContext.Current working
    // }
    // 

    internal class HttpContextWrapper : IDisposable { 
        private bool _needToUndo; 
        private HttpContext _savedContext;
 
        internal static HttpContext SwitchContext(HttpContext context) {
            return ContextBase.SwitchContext(context) as HttpContext;
        }
 
        internal HttpContextWrapper(HttpContext context) {
            if (context != null) { 
                _savedContext = SwitchContext(context); 
                _needToUndo = (_savedContext != context);
            } 
        }

        void IDisposable.Dispose() {
            if (_needToUndo) { 
                SwitchContext(_savedContext);
                _savedContext = null; 
                _needToUndo = false; 
            }
        } 
    }
}
