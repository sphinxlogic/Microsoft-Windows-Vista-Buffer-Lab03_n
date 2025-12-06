//------------------------------------------------------------------------------ 
// <copyright file="httpserverutility.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Server intrinsic used to match ASP's object model 
 *
 * Copyright (c) 1999 Microsoft Corporation 
 */

// Don't entity encode high chars (160 to 256), to fix bugs VSWhidbey 85857/111927
// 
#define ENTITY_ENCODE_HIGH_ASCII_CHARS
 
namespace System.Web { 
    using System.Text;
    using System.Runtime.Serialization.Formatters; 
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.IO;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Globalization; 
    using System.Web.Hosting; 
    using System.Web.Util;
    using System.Web.UI; 
    using System.Web.Configuration;
    using System.Security;
    using System.Security.Permissions;
    using System.Web.Management;     
    using System.Web.SessionState;

    internal abstract class ErrorFormatterGenerator { 
        internal abstract ErrorFormatter GetErrorFormatter(Exception e); 
    }
 

    /// <devdoc>
    ///    <para>
    ///       Provides several 
    ///       helper methods that can be used in the processing of Web requests.
    ///    </para> 
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class HttpServerUtility { 
        private HttpContext _context;
        private HttpApplication _application;

        private static IDictionary _cultureCache = Hashtable.Synchronized(new Hashtable()); 

        internal HttpServerUtility(HttpContext context) { 
            _context = context; 
        }
 
        internal HttpServerUtility(HttpApplication application) {
            _application = application;
        }
 
        //
        // Misc ASP compatibility methods 
        // 

 
        /// <devdoc>
        ///    <para>
        ///       Instantiates a COM object identified via a progid.
        ///    </para> 
        /// </devdoc>
        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public object CreateObject(string progID) { 
            Type type = null;
            object obj = null; 

            try {
#if !FEATURE_PAL // FEATURE_PAL does not enable COM
                type = Type.GetTypeFromProgID(progID); 
#else // !FEATURE_PAL
                throw new NotImplementedException("ROTORTODO"); 
#endif // !FEATURE_PAL 
            }
            catch { 
            }

            if (type == null) {
                throw new HttpException(SR.GetString(SR.Could_not_create_object_of_type, progID)); 
            }
 
            // Disallow Apartment components in non-compat mode 
            AspCompatApplicationStep.CheckThreadingModel(progID, type.GUID);
 
            // Instantiate the object
            obj = Activator.CreateInstance(type);

            // For ASP compat: take care of OnPageStart/OnPageEnd 
            AspCompatApplicationStep.OnPageStart(obj);
 
            return obj; 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Instantiates a COM object identified via a Type. 
        ///    </para>
        /// </devdoc> 
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)] 
        public object CreateObject(Type type) {
            // Disallow Apartment components in non-compat mode 
            AspCompatApplicationStep.CheckThreadingModel(type.FullName, type.GUID);

            // Instantiate the object
            Object obj = Activator.CreateInstance(type); 

            // For ASP compat: take care of OnPageStart/OnPageEnd 
            AspCompatApplicationStep.OnPageStart(obj); 

            return obj; 
        }


 
        /// <devdoc>
        ///    <para> 
        ///       Instantiates a COM object identified via a clsid. 
        ///    </para>
        /// </devdoc> 
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)]
        public object CreateObjectFromClsid(string clsid) {
            Type type = null;
            object obj = null; 

            // Create a Guid out of it 
            Guid guid = new Guid(clsid); 

            // Disallow Apartment components in non-compat mode 
            AspCompatApplicationStep.CheckThreadingModel(clsid, guid);

            try {
#if !FEATURE_PAL // FEATURE_PAL does not enable COM 
                type = Type.GetTypeFromCLSID(guid, null, true /*throwOnError*/);
#else // !FEATURE_PAL 
                throw new NotImplementedException("ROTORTODO"); 
#endif // !FEATURE_PAL
 
                // Instantiate the object
                obj = Activator.CreateInstance(type);
            }
            catch { 
            }
 
            if (obj == null) { 
                throw new HttpException(
                    SR.GetString(SR.Could_not_create_object_from_clsid, clsid)); 
            }

            // For ASP compat: take care of OnPageStart/OnPageEnd
            AspCompatApplicationStep.OnPageStart(obj); 

            return obj; 
        } 

        // Internal static method that returns a read-only, non-user override accounted, CultureInfo object 
        internal static CultureInfo CreateReadOnlyCultureInfo(string name) {
            if (!_cultureCache.Contains(name)) {
                // To be threadsafe, get the lock before creating
                lock (_cultureCache) { 
                    if (_cultureCache[name] == null) {
                        _cultureCache[name] = CultureInfo.ReadOnly(new CultureInfo(name)); 
                    } 
                }
            } 
            return (CultureInfo)_cultureCache[name];
        }

        // Internal static method that returns a read-only, non-user override accounted, culture specific CultureInfo object 
        internal static CultureInfo CreateReadOnlySpecificCultureInfo(string name) {
            if(name.IndexOf('-') > 0) { 
                return CreateReadOnlyCultureInfo(name); 
            }
            CultureInfo ci = CultureInfo.CreateSpecificCulture(name); 
            if (!_cultureCache.Contains(ci.Name)) {
                //To be threadsafe, get the lock before creating
                lock (_cultureCache) {
                    if (_cultureCache[ci.Name] == null) { 
                        _cultureCache[ci.Name] = CultureInfo.ReadOnly(ci);
                    } 
                } 
            }
            return (CultureInfo)_cultureCache[ci.Name]; 
        }

        // Internal static method that returns a read-only, non-user override accounted, CultureInfo object
        internal static CultureInfo CreateReadOnlyCultureInfo(int culture) { 
            if (!_cultureCache.Contains(culture)) {
                // To be threadsafe, get the lock before creating 
                lock (_cultureCache) { 
                    if (_cultureCache[culture] == null) {
                        _cultureCache[culture] = CultureInfo.ReadOnly(new CultureInfo(culture)); 
                    }
                }
            }
            return (CultureInfo)_cultureCache[culture]; 
        }
 
        /// <devdoc> 
        ///    <para>
        ///       Maps a virtual path to a physical path. 
        ///    </para>
        /// </devdoc>
        public string MapPath(string path) {
            if (_context == null) 
                throw new HttpException(SR.GetString(SR.Server_not_available));
            // Disable hiding the request so that Server.MapPath works when called from 
            // Application_Start in integrated mode 
            bool unhideRequest = _context.HideRequestResponse;
            string realPath; 
            try {
                if (unhideRequest) {
                    _context.HideRequestResponse = false;
                } 
                realPath = _context.Request.MapPath(path);
            } 
            finally { 
                if (unhideRequest) {
                    _context.HideRequestResponse = true; 
                }
            }
            return realPath;
        } 

 
        /// <devdoc> 
        ///    <para>Returns the last recorded exception.</para>
        /// </devdoc> 
        public Exception GetLastError() {
            if (_context != null)
                return _context.Error;
            else if (_application != null) 
                return _application.LastError;
            else 
                return null; 
        }
 

        /// <devdoc>
        ///    <para>Clears the last error.</para>
        /// </devdoc> 
        public void ClearError() {
            if (_context != null) 
                _context.ClearError(); 
            else if (_application != null)
                _application.ClearError(); 
        }

        //
        // Server.Transfer/Server.Execute -- child requests 
        //
 
 
        /// <devdoc>
        ///    <para> 
        ///       Executes a new request (using the specified URL path as the target). Unlike
        ///       the Transfer method, execution of the original page continues after the executed
        ///       page completes.
        ///    </para> 
        /// </devdoc>
        public void Execute(string path) { 
            Execute(path, null, true /*preserveForm*/); 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Executes a new request (using the specified URL path as the target). Unlike 
        ///       the Transfer method, execution of the original page continues after the executed
        ///       page completes. 
        ///    </para> 
        /// </devdoc>
        public void Execute(string path, TextWriter writer) { 
            Execute(path, writer, true /*preserveForm*/);
        }

 
        /// <devdoc>
        ///    <para> 
        ///       Executes a new request (using the specified URL path as the target). Unlike 
        ///       the Transfer method, execution of the original page continues after the executed
        ///       page completes. 
        ///       If preserveForm is false, the QueryString and Form collections are cleared.
        ///    </para>
        /// </devdoc>
        public void Execute(string path, bool preserveForm) { 
            Execute(path, null, preserveForm);
        } 
 

        /// <devdoc> 
        ///    <para>
        ///       Executes a new request (using the specified URL path as the target). Unlike
        ///       the Transfer method, execution of the original page continues after the executed
        ///       page completes. 
        ///       If preserveForm is false, the QueryString and Form collections are cleared.
        ///    </para> 
        /// </devdoc> 
        public void Execute(string path, TextWriter writer, bool preserveForm) {
            if (_context == null) 
                throw new HttpException(SR.GetString(SR.Server_not_available));

            if (path == null)
                throw new ArgumentNullException("path"); 

            string queryStringOverride = null; 
            HttpRequest request = _context.Request; 
            HttpResponse response = _context.Response;
 
            // Remove potential cookie-less session id (ASURT 100558)
            path = response.RemoveAppPathModifier(path);

            // Allow query string override 
            int iqs = path.IndexOf('?');
            if (iqs >= 0) { 
                queryStringOverride = path.Substring(iqs+1); 
                path = path.Substring(0, iqs);
            } 

            if (!UrlPath.IsValidVirtualPathWithoutProtocol(path))
                throw new ArgumentException(SR.GetString(SR.Invalid_path_for_child_request, path));
 
            VirtualPath virtualPath = VirtualPath.Create(path);
 
            // Find the handler for the path 

            IHttpHandler handler = null; 

            string physPath = request.MapPath(virtualPath);        // get physical path
            VirtualPath filePath = request.FilePathObject.Combine(virtualPath);    // vpath
 
            // Demand read access to the physical path of the target handler
            InternalSecurityPermissions.FileReadAccess(physPath).Demand(); 
 
            // We need to Assert since there typically is user code on the stack (VSWhidbey 270965)
            InternalSecurityPermissions.Unrestricted.Assert(); 

            try {
                // paths that ends with . are disallowed as they are used to get around
                // extension mappings and server source as static file 
                if (StringUtil.StringEndsWith(virtualPath.VirtualPathString, '.'))
                    throw new HttpException(404, String.Empty); 
 
                bool useAppConfig = !filePath.IsWithinAppRoot;
 
                using (new HttpContextWrapper(_context)) {

                    try {
                        // We need to increase the depth when calling MapHttpHandler, 
                        // since PageHandlerFactory relies on it
                        _context.ServerExecuteDepth++; 
 
                        if (_context.WorkerRequest is IIS7WorkerRequest) {
                            handler = _context.ApplicationInstance.MapIntegratedHttpHandler( 
                                _context,
                                request.RequestType,
                                filePath,
                                physPath, 
                                useAppConfig,
                                true /*convertNativeStaticFileModule*/); 
                        } 
                        else {
                            handler = _context.ApplicationInstance.MapHttpHandler( 
                                _context,
                                request.RequestType,
                                filePath,
                                physPath, 
                                useAppConfig);
                        } 
                    } 
                    finally {
                        _context.ServerExecuteDepth--; 
                    }
                }
            }
            catch (Exception e) { 
                // 500 errors (compilation errors) get preserved
                if (e is HttpException) { 
                    int code = ((HttpException)e).GetHttpCode(); 

                    if (code != 500 && code != 404) { 
                        e = null;
                    }
                }
 
                throw new HttpException(SR.GetString(SR.Error_executing_child_request_for_path, path), e);
            } 
 
            ExecuteInternal(handler, writer, preserveForm, true /*setPreviousPage*/,
                virtualPath, filePath, physPath, null, queryStringOverride); 
        }


        public void Execute(IHttpHandler handler, TextWriter writer, bool preserveForm) { 
            if (_context == null)
                throw new HttpException(SR.GetString(SR.Server_not_available)); 
 
            Execute(handler, writer, preserveForm, true /*setPreviousPage*/);
        } 

        internal void Execute(IHttpHandler handler, TextWriter writer, bool preserveForm, bool setPreviousPage) {
            HttpRequest request = _context.Request;
            VirtualPath filePath = request.CurrentExecutionFilePathObject; 
            string physicalPath = request.MapPath(filePath);
 
            ExecuteInternal(handler, writer, preserveForm, setPreviousPage, 
                null, filePath, physicalPath, null, null);
        } 

        private void ExecuteInternal(IHttpHandler handler, TextWriter writer, bool preserveForm, bool setPreviousPage,
            VirtualPath path, VirtualPath filePath, string physPath, Exception error, string queryStringOverride) {
 
            if (handler == null)
                throw new ArgumentNullException("handler"); 
 
            HttpRequest request = _context.Request;
            HttpResponse response = _context.Response; 
            HttpApplication app = _context.ApplicationInstance;

            HttpValueCollection savedForm = null;
            VirtualPath savedCurrentExecutionFilePath = null; 
            string savedQueryString = null;
            TextWriter savedOutputWriter = null; 
            AspNetSynchronizationContext savedSyncContext = null; 

            // Transaction wouldn't flow into ASPCOMPAT mode -- need to report an error 
            VerifyTransactionFlow(handler);

            // create new trace context
            _context.PushTraceContext(); 

            // set the new handler as the current handler 
            _context.SetCurrentHandler(handler); 

            // because we call this synchrnously async operations must be disabled 
            bool originalSyncContextWasEnabled = _context.SyncContext.Enabled;
            _context.SyncContext.Disable();

            // Execute the handler 
            try {
                try { 
                    _context.ServerExecuteDepth++; 

                    savedCurrentExecutionFilePath = request.SwitchCurrentExecutionFilePath(filePath); 

                    if (!preserveForm) {
                        savedForm = request.SwitchForm(new HttpValueCollection());
 
                        // Clear out the query string, but honor overrides
                        if (queryStringOverride == null) 
                            queryStringOverride = String.Empty; 
                    }
 
                    // override query string if requested
                    if (queryStringOverride != null) {
                        savedQueryString = request.QueryStringText;
                        request.QueryStringText = queryStringOverride; 
                    }
 
                    // capture output if requested 
                    if (writer != null)
                        savedOutputWriter = response.SwitchWriter(writer); 

                    Page targetPage = handler as Page;
                    if (targetPage != null) {
                        if (setPreviousPage) { 
                            // Set the previousPage of the new Page as the previous Page
                            targetPage.SetPreviousPage(_context.PreviousHandler as Page); 
                        } 

                        Page sourcePage = _context.Handler as Page; 

#pragma warning disable 0618    // To avoid deprecation warning
                        // If the source page of the transfer has smart nav on,
                        // always do as if the destination has it too (ASURT 97732) 
                        if (sourcePage != null && sourcePage.SmartNavigation)
                            targetPage.SmartNavigation = true; 
#pragma warning restore 0618 

                        // If the target page is async need to save/restore sync context 
                        if (targetPage is IHttpAsyncHandler) {
                            savedSyncContext = _context.InstallNewAspNetSynchronizationContext();
                        }
                    } 

                    if ((handler is StaticFileHandler || handler is DefaultHttpHandler) && 
                       !DefaultHttpHandler.IsClassicAspRequest(filePath.VirtualPathString)) { 
                        // cannot apply static files handler directly
                        // -- it would dump the source of the current page 
                        // instead just dump the file content into response
                        try {
                            response.WriteFile(physPath);
                        } 
                        catch {
                            // hide the real error as it could be misleading 
                            // in case of mismapped requests like /foo.asmx/bar 
                            error = new HttpException(404, String.Empty);
                        } 
                    }
                    else if (!(handler is Page)) {
                        // disallow anything but pages
                        error = new HttpException(404, String.Empty); 
                    }
                    else if (handler is IHttpAsyncHandler) { 
                        // Asynchronous handler 

                        // suspend cancellable period (don't abort this thread while 
                        // we wait for another to finish)
                        bool isCancellable =  _context.IsInCancellablePeriod;
                        if (isCancellable)
                            _context.EndCancellablePeriod(); 

                        try { 
                            IHttpAsyncHandler asyncHandler = (IHttpAsyncHandler)handler; 

                            IAsyncResult ar = asyncHandler.BeginProcessRequest(_context, null, null); 

                            // wait for completion
                            if (!ar.IsCompleted) {
                                // suspend app lock while waiting 
                                bool needToRelock = false;
 
                                try { 
                                    try {} finally {
                                        Monitor.Exit(app); 
                                        needToRelock = true;
                                    }

                                    WaitHandle h = ar.AsyncWaitHandle; 

                                    if (h != null) { 
                                        h.WaitOne(); 
                                    }
                                    else { 
                                        while (!ar.IsCompleted)
                                            Thread.Sleep(1);
                                    }
                                } 
                                finally {
                                    if (needToRelock) { 
                                        Monitor.Enter(app); 
                                    }
                                } 
                            }

                            // end the async operation (get error if any)
 
                            try {
                                asyncHandler.EndProcessRequest(ar); 
                            } 
                            catch (Exception e) {
                                error = e; 
                            }
                        }
                        finally {
                            // resume cancelleable period 
                            if (isCancellable)
                                _context.BeginCancellablePeriod(); 
                        } 
                    }
                    else { 
                        // Synchronous handler

                        using (new HttpContextWrapper(_context)) {
                            try { 
                                handler.ProcessRequest(_context);
                            } 
                            catch (Exception e) { 
                                error = e;
                            } 
                        }
                    }
                }
                finally { 
                    _context.ServerExecuteDepth--;
 
                    // Restore the handlers; 
                    _context.RestoreCurrentHandler();
 
                    // restore output writer
                    if (savedOutputWriter != null)
                        response.SwitchWriter(savedOutputWriter);
 
                    // restore overriden query string
                    if (queryStringOverride != null && savedQueryString != null) 
                        request.QueryStringText = savedQueryString; 

                    if (savedForm != null) 
                        request.SwitchForm(savedForm);

                    request.SwitchCurrentExecutionFilePath(savedCurrentExecutionFilePath);
 
                    if (savedSyncContext != null) {
                        _context.RestoreSavedAspNetSynchronizationContext(savedSyncContext); 
                    } 

                    if (originalSyncContextWasEnabled) { 
                        _context.SyncContext.Enable();
                    }

                    // restore trace context 
                    _context.PopTraceContext();
                } 
            } 
            catch { // Protect against exception filters
                throw; 
            }

            // Report any error
            if (error != null) { 
                // suppress errors with HTTP codes (for child requests they mislead more than help)
                if (error is HttpException && ((HttpException)error).GetHttpCode() != 500) 
                    error = null; 

                if (path != null) 
                    throw new HttpException(SR.GetString(SR.Error_executing_child_request_for_path, path), error);

                throw new HttpException(SR.GetString(SR.Error_executing_child_request_for_handler, handler.GetType().ToString()), error);
            } 
        }
 
 
        /// <devdoc>
        ///    <para> 
        ///       Terminates execution of the current page and begins execution of a new
        ///       request using the supplied URL path.
        ///       If preserveForm is false, the QueryString and Form collections are cleared.
        ///    </para> 
        /// </devdoc>
        public void Transfer(string path, bool preserveForm) { 
            Page page = _context.Handler as Page; 
            if ((page != null) && page.IsCallback) {
                throw new ApplicationException(SR.GetString(SR.Transfer_not_allowed_in_callback)); 
            }

            // execute child request
 
            Execute(path, null, preserveForm);
 
            // suppress the remainder of the current one 

            _context.Response.End(); 
        }


        /// <devdoc> 
        ///    <para>
        ///       Terminates execution of the current page and begins execution of a new 
        ///       request using the supplied URL path. 
        ///    </para>
        /// </devdoc> 
        public void Transfer(string path) {
            // Make sure the transfer is not treated as a postback, which could cause a stack
            // overflow if the user doesn't expect it (VSWhidbey 181013).
            // If the use *does* want it treated as a postback, they can call Transfer(path, true). 
            bool savedPreventPostback = _context.PreventPostback;
            _context.PreventPostback = true; 
 
            Transfer(path, true /*preserveForm*/);
 
            _context.PreventPostback = savedPreventPostback;
        }

 
        public void Transfer(IHttpHandler handler, bool preserveForm) {
            Page page = handler as Page; 
            if ((page != null) && page.IsCallback) { 
                throw new ApplicationException(SR.GetString(SR.Transfer_not_allowed_in_callback));
            } 

            Execute(handler, null, preserveForm);

            // suppress the remainder of the current one 

            _context.Response.End(); 
        } 

        public void TransferRequest(string path) 
        {
            TransferRequest(path, false, null, null);
        }
 
        public void TransferRequest(string path, bool preserveForm)
        { 
            TransferRequest(path, preserveForm, null, null); 
        }
 
        public void TransferRequest(string path, bool preserveForm, string method, NameValueCollection headers) {
            if (!HttpRuntime.UseIntegratedPipeline) {
                throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode));
            } 

            if (_context == null) { 
                throw new HttpException(SR.GetString(SR.Server_not_available)); 
            }
 
            if (path == null) {
                throw new ArgumentNullException("path");
            }
 
            IIS7WorkerRequest wr = _context.WorkerRequest as IIS7WorkerRequest;
            HttpRequest request = _context.Request; 
            HttpResponse response = _context.Response; 

            if (wr == null) { 
                throw new HttpException(SR.GetString(SR.Server_not_available));
            }

            // Remove potential cookie-less session id (ASURT 100558) 
            path = response.RemoveAppPathModifier(path);
 
            // Extract query string if specified 
            String qs = null;
            int iqs = path.IndexOf('?'); 
            if (iqs >= 0) {
                qs = (iqs < path.Length-1) ? path.Substring(iqs+1) : String.Empty;
                path = path.Substring(0, iqs);
            } 

            if (!UrlPath.IsValidVirtualPathWithoutProtocol(path)) { 
                throw new ArgumentException(SR.GetString(SR.Invalid_path_for_child_request, path)); 
            }
 
            VirtualPath virtualPath = request.FilePathObject.Combine(VirtualPath.Create(path));
            string physPath = request.MapPath(path);

            // Demand read access to the physical path of the target handler 
            InternalSecurityPermissions.FileReadAccess(physPath).Demand();
 
            //  Schedule the child execution 
            wr.ScheduleExecuteUrl( virtualPath.VirtualPathString,
                                   qs, 
                                   method,
                                   preserveForm,
                                   preserveForm ? request.EntityBody : null,
                                   headers); 

            // force the completion of the current request so that the 
            // child execution can be performed immediately after unwind 
            _context.ApplicationInstance.EnsureReleaseState();
            _context.Response.End();
        } 

        private void VerifyTransactionFlow(IHttpHandler handler) {
            Page topPage = _context.Handler as Page;
            Page childPage = handler as Page; 

            if (childPage != null && childPage.IsInAspCompatMode && // child page aspcompat 
                topPage != null && !topPage.IsInAspCompatMode &&    // top page is not aspcompat 
                Transactions.Utils.IsInTransaction) {               // we are in transaction
 
                throw new HttpException(SR.GetString(SR.Transacted_page_calls_aspcompat));
            }
        }
 
        //
        // Static method to execute a request outside of HttpContext and capture the response 
        // 

        internal static void ExecuteLocalRequestAndCaptureResponse(String path, TextWriter writer, 
                                                                ErrorFormatterGenerator errorFormatterGenerator) {
            HttpRequest request = new HttpRequest(
                                          VirtualPath.CreateAbsolute(path),
                                          String.Empty); 

            HttpResponse response = new HttpResponse(writer); 
 
            HttpContext context = new HttpContext(request, response);
 
            HttpApplication app = HttpApplicationFactory.GetApplicationInstance(context) as HttpApplication;
            context.ApplicationInstance = app;

            try { 
                context.Server.Execute(path);
            } 
            catch (HttpException e) { 
                if (errorFormatterGenerator != null) {
                    context.Response.SetOverrideErrorFormatter(errorFormatterGenerator.GetErrorFormatter(e)); 
                }

                context.Response.ReportRuntimeError(e, false, true);
            } 
            finally {
                if (app != null) { 
                    context.ApplicationInstance = null; 
                    HttpApplicationFactory.RecycleApplicationInstance(app);
                } 
            }
        }

        // 
        // Computer name
        // 
 
        private static object _machineNameLock = new object();
        private static string _machineName; 
        private const int _maxMachineNameLength = 256;


        /// <devdoc> 
        ///    <para>
        ///       Gets 
        ///       the server machine name. 
        ///    </para>
        /// </devdoc> 
        public string MachineName {
            [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Medium)]
            get {
                return GetMachineNameInternal(); 
            }
        } 
 
        internal static string GetMachineNameInternal()
        { 
            if (_machineName != null)
                return _machineName;
            lock (_machineNameLock)
            { 
                if (_machineName != null)
                    return _machineName; 
 
                StringBuilder   buf = new StringBuilder (_maxMachineNameLength);
                int             len = _maxMachineNameLength; 

                if (UnsafeNativeMethods.GetComputerName (buf, ref len) == 0)
                    throw new HttpException (SR.GetString (SR.Get_computer_name_failed));
 
                _machineName = buf.ToString();
            } 
            return _machineName; 
        }
 
        //
        // Request Timeout
        //
 

        /// <devdoc> 
        ///    <para> 
        ///       Request timeout in seconds
        ///    </para> 
        /// </devdoc>
        public int ScriptTimeout {
            get {
                if (_context != null) { 
                    return Convert.ToInt32(_context.Timeout.TotalSeconds, CultureInfo.InvariantCulture);
                } 
                else { 
                    return HttpRuntimeSection.DefaultExecutionTimeout;
                } 
            }

            [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Medium)]
            set { 
                if (_context == null)
                    throw new HttpException(SR.GetString(SR.Server_not_available)); 
                if (value <= 0) 
                    throw new ArgumentOutOfRangeException("value");
                _context.Timeout = new TimeSpan(0, 0, value); 
            }
        }

        // 
        // Encoding / Decoding -- wrappers for HttpUtility
        // 
 

        /// <devdoc> 
        ///    <para>
        ///       HTML
        ///       decodes a given string and
        ///       returns the decoded string. 
        ///    </para>
        /// </devdoc> 
        public string HtmlDecode(string s) { 
            return HttpUtility.HtmlDecode(s);
        } 


        /// <devdoc>
        ///    <para> 
        ///       HTML
        ///       decode a string and send the result to a TextWriter output 
        ///       stream. 
        ///    </para>
        /// </devdoc> 
        public void HtmlDecode(string s, TextWriter output) {
            HttpUtility.HtmlDecode(s, output);
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       HTML
        ///       encodes a given string and 
        ///       returns the encoded string.
        ///    </para>
        /// </devdoc>
        public string HtmlEncode(string s) { 
            return HttpUtility.HtmlEncode(s);
        } 
 

        /// <devdoc> 
        ///    <para>
        ///       HTML
        ///       encodes
        ///       a string and returns the output to a TextWriter stream of output. 
        ///    </para>
        /// </devdoc> 
        public void HtmlEncode(string s, TextWriter output) { 
            HttpUtility.HtmlEncode(s, output);
        } 


        /// <devdoc>
        ///    <para> 
        ///       URL
        ///       encodes a given 
        ///       string and returns the encoded string. 
        ///    </para>
        /// </devdoc> 
        public string UrlEncode(string s) {
            Encoding e = (_context != null) ? _context.Response.ContentEncoding : Encoding.UTF8;
            return HttpUtility.UrlEncode(s, e);
        } 

 
        /// <devdoc> 
        ///    <para>
        ///       URL encodes a path portion of a URL string and returns the encoded string. 
        ///    </para>
        /// </devdoc>
        public string UrlPathEncode(string s) {
            return HttpUtility.UrlPathEncode(s); 
        }
 
 
        /// <devdoc>
        ///    <para> 
        ///       URL
        ///       encodes
        ///       a string and returns the output to a TextWriter output stream.
        ///    </para> 
        /// </devdoc>
        public void UrlEncode(string s, TextWriter output) { 
            if (s != null) 
                output.Write(UrlEncode(s));
        } 


        /// <devdoc>
        ///    <para> 
        ///       URL decodes a string and returns the output in a string.
        ///    </para> 
        /// </devdoc> 
        public string UrlDecode(string s) {
            Encoding e = (_context != null) ? _context.Request.ContentEncoding : Encoding.UTF8; 
            return HttpUtility.UrlDecode(s, e);
        }

 
        /// <devdoc>
        ///    <para> 
        ///       URL decodes a string and returns the output as a TextWriter output 
        ///       stream.
        ///    </para> 
        /// </devdoc>
        public void UrlDecode(string s, TextWriter output) {
            if (s != null)
                output.Write(UrlDecode(s)); 
        }
 
        ///////////////////////////////////////////////////////////////////////////// 
        /////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////// 

        static public string UrlTokenEncode(byte [] input)
        {
            if (input == null) 
                throw new ArgumentNullException("input");
            if (input.Length < 1) 
                return String.Empty; 

            string  base64Str   = null; 
            int     endPos      = 0;
            char[]  base64Chars = null;

            //////////////////////////////////////////////////////// 
            // Step 1: Do a Base64 encoding
            base64Str = Convert.ToBase64String(input); 
            if (base64Str == null) 
                return null;
 
            ////////////////////////////////////////////////////////
            // Step 2: Find how many padding chars are present in the end
            for (endPos = base64Str.Length; endPos > 0; endPos--)
            { 
                if (base64Str[endPos - 1] != '=') // Found a non-padding char!
                { 
                    break; // Stop here 
                }
            } 

            ////////////////////////////////////////////////////////
            // Step 3: Create char array to store all non-padding chars,
            //      plus a char to indicate how many padding chars are needed 
            base64Chars = new char[endPos + 1];
            base64Chars[endPos] = (char)((int)'0' + base64Str.Length - endPos); // Store a char at the end, to indicate how many padding chars are needed 
 
            ////////////////////////////////////////////////////////
            // Step 3: Copy in the other chars. Transform the "+" to "-", and "/" to "_" 
            for (int iter = 0; iter < endPos; iter++)
            {
                char c = base64Str[iter];
 
                switch (c)
                { 
                    case '+': 
                        base64Chars[iter] = '-';
                        break; 

                    case '/':
                        base64Chars[iter] = '_';
                        break; 

                    case '=': 
                        Debug.Assert (false); 
                        base64Chars[iter] = c;
                        break; 

                    default:
                        base64Chars[iter] = c;
                        break; 
                }
            } 
            return new string(base64Chars); 
        }
 
        /////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////
 
        static public byte [] UrlTokenDecode(string input) {
            if (input == null) 
                throw new ArgumentNullException("input"); 

            int len = input.Length; 
            if (len < 1)
                return new byte[0];

            /////////////////////////////////////////////////////////////////// 
            // Step 1: Calculate the number of padding chars to append to this string.
            //         The number of padding chars to append is stored in the last char of the string. 
            int numPadChars = (int)input[len - 1] - (int)'0'; 
            if (numPadChars < 0 || numPadChars > 10)
                return null; 


            ///////////////////////////////////////////////////////////////////
            // Step 2: Create array to store the chars (not including the last char) 
            //          and the padding chars
            char[] base64Chars = new char[len - 1 + numPadChars]; 
 

            //////////////////////////////////////////////////////// 
            // Step 3: Copy in the chars. Transform the "-" to "+", and "*" to "/"
            for (int iter = 0; iter < len - 1; iter++)
            {
                char c = input[iter]; 

                switch (c) 
                { 
                    case '-':
                        base64Chars[iter] = '+'; 
                        break;

                    case '_':
                        base64Chars[iter] = '/'; 
                        break;
 
                    default: 
                        base64Chars[iter] = c;
                        break; 
                }
            }

            //////////////////////////////////////////////////////// 
            // Step 4: Add padding chars
            for (int iter = len - 1; iter < base64Chars.Length; iter++) 
            { 
                base64Chars[iter] = '=';
            } 

            // Do the actual conversion
            return Convert.FromBase64CharArray (base64Chars, 0, base64Chars.Length);
        } 
    }
 
 
    /// <devdoc>
    /// </devdoc> 

    // VSWhidbey 473228 - removed link demand from HttpUtility for ClickOnce scenario
    public sealed class HttpUtility {
 
        public HttpUtility () {}
 		 
		////////////////////////////////////////////////////////////////////////// 
        //
        //  HTML Encoding / Decoding 
        //


        /// <devdoc> 
        ///    <para>
        ///       HTML decodes a string and returns the decoded string. 
        ///    </para> 
        /// </devdoc>
        public static string HtmlDecode(string s) { 
            if (s == null)
                return null;

            // See if this string needs to be decoded at all.  If no 
            // ampersands are found, then no special HTML-encoded chars
            // are in the string. 
            if (s.IndexOf('&') < 0) 
                return s;
 
            StringBuilder builder = new StringBuilder();
            StringWriter writer = new StringWriter(builder);

            HtmlDecode(s, writer); 

            return builder.ToString(); 
        } 

 
        /// <devdoc>
        ///    <para>
        ///       HTML decode a string and send the result to a TextWriter output stream.
        ///    </para> 
        /// </devdoc>
        private static char[] s_entityEndingChars = new char[] {';', '&'}; 
        public static void HtmlDecode(string s, TextWriter output) { 
            if (s == null)
                return; 

            if (s.IndexOf('&') < 0) {
                output.Write(s);        // good as is
                return; 
            }
 
            int l = s.Length; 
            for (int i = 0; i < l; i++) {
                char ch = s[i]; 

                if (ch == '&') {
                    // We found a '&'. Now look for the next ';' or '&'. The idea is that
                    // if we find another '&' before finding a ';', then this is not an entity, 
                    // and the next '&' might start a real entity (VSWhidbey 275184)
                    int index = s.IndexOfAny(s_entityEndingChars, i + 1); 
                    if (index > 0 && s[index] == ';') { 
                        string entity = s.Substring(i + 1, index - i - 1);
 
                        if (entity.Length > 1 && entity[0] == '#') {
                            try {
                                // The # syntax can be in decimal or hex, e.g.
                                //      &#229;  --> decimal 
                                //      &#xE5;  --> same char in hex
                                // See http://www.w3.org/TR/REC-html40/charset.html#entities 
                                if (entity[1] == 'x' || entity[1] == 'X') 
                                    ch = (char) Int32.Parse(entity.Substring(2), NumberStyles.AllowHexSpecifier);
                                else 
                                    ch = (char) Int32.Parse(entity.Substring(1));
                                i = index; // already looked at everything until semicolon
                            }
                            catch (System.FormatException) { 
                                i++;    //if the number isn't valid, ignore it
                            } 
                            catch (System.ArgumentException) { 
                                i++;    // if there is no number, ignore it.
                            } 
                        }
                        else {
                            i = index; // already looked at everything until semicolon
 
                            char entityChar = HtmlEntities.Lookup(entity);
                            if (entityChar != (char)0) { 
                                ch = entityChar; 
                            }
                            else { 
                                output.Write('&');
                                output.Write(entity);
                                output.Write(';');
                                continue; 
                            }
                        } 
 
                    }
                } 

                output.Write(ch);
            }
        } 

 
        /// <devdoc> 
        ///    <para>
        ///       HTML encodes a string and returns the encoded string. 
        ///    </para>
        /// </devdoc>
        public static String HtmlEncode(String s) {
            if (s == null) 
                return null;
 
            int pos = IndexOfHtmlEncodingChars(s, 0); 
            if (pos == -1)
                return s; 

            StringBuilder builder = new StringBuilder(s.Length + 5);
            int cch = s.Length;
            int startPos = 0; 
            for (;;) {
                if (pos > startPos) { 
                    builder.Append(s, startPos, pos - startPos); 
                }
 
                char ch = s[pos];
                if (ch <= '>') {
                    switch (ch) {
                        case '<': 
                            builder.Append("&lt;");
                            break; 
                        case '>': 
                            builder.Append("&gt;");
                            break; 
                        case '"':
                            builder.Append("&quot;");
                            break;
                        case '&': 
                            builder.Append("&amp;");
                            break; 
                        default: 
                            Debug.Assert(false, "Unexpected value for ch, ch=" + ch);
                            break; 
                    }
                }
                else {
#if ENTITY_ENCODE_HIGH_ASCII_CHARS 
                    Debug.Assert(ch >= 160 && ch < 256, "ch >= 160 && ch < 256");
 
                    // The seemingly arbitrary 160 comes from RFC 
                    builder.Append("&#");
                    builder.Append(((int)ch).ToString(NumberFormatInfo.InvariantInfo)); 
                    builder.Append(';');
#else
                    Debug.Assert(false);
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS 
                }
 
                startPos = pos + 1; 
                if (startPos >= cch)
                    break; 

                pos = IndexOfHtmlEncodingChars(s, startPos);
                if (pos == -1) {
                    builder.Append(s, startPos, cch - startPos); 
                    break;
                } 
            } 

            return builder.ToString(); 
        }


        /// <devdoc> 
        ///    <para>
        ///       HTML encodes a string and returns the output to a TextWriter stream of 
        ///       output. 
        ///    </para>
        /// </devdoc> 
        public unsafe static void HtmlEncode(String s, TextWriter output) {
            if (s == null)
                return;
 
            int index = IndexOfHtmlEncodingChars(s, 0);
            if (index == -1) { 
                output.Write(s); 
                return;
            } 

            Debug.Assert(0 <= index && index <= s.Length, "0 <= index && index <= s.Length");

            int cch = s.Length - index; 
            fixed (char * str = s) {
                char *pch = str; 
                while (index-- > 0) { 
                    output.Write(*pch++);
                } 

                while (cch-- > 0) {
                    char ch = *pch++;
                    if (ch <= '>') { 
                        switch (ch) {
                            case '<': 
                                output.Write("&lt;"); 
                                break;
                            case '>': 
                                output.Write("&gt;");
                                break;
                            case '"':
                                output.Write("&quot;"); 
                                break;
                            case '&': 
                                output.Write("&amp;"); 
                                break;
                            default: 
                                output.Write(ch);
                                break;
                        }
                    } 
#if ENTITY_ENCODE_HIGH_ASCII_CHARS
                    else if (ch >= 160 && ch < 256) { 
                        // The seemingly arbitrary 160 comes from RFC 
                        output.Write("&#");
                        output.Write(((int)ch).ToString(NumberFormatInfo.InvariantInfo)); 
                        output.Write(';');
                    }
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS
                    else { 
                        output.Write(ch);
                    } 
                } 
            }
        } 

        static unsafe int IndexOfHtmlEncodingChars(string s, int startPos) {
            Debug.Assert(0 <= startPos && startPos <= s.Length, "0 <= startPos && startPos <= s.Length");
 
            int cch = s.Length - startPos;
            fixed (char * str = s) { 
                for (char *pch = &str[startPos]; cch > 0; pch++, cch--) { 
                    char ch = *pch;
                    if (ch <= '>') { 
                        switch (ch) {
                            case '<':
                            case '>':
                            case '"': 
                            case '&':
                                return s.Length - cch; 
                        } 
                    }
#if ENTITY_ENCODE_HIGH_ASCII_CHARS 
                    else if (ch >= 160 && ch < 256) {
                        return s.Length - cch;
                    }
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS 
                }
            } 
 
            return -1;
        } 


        /// <devdoc>
        ///    <para> 
        ///       Encodes a string to make it a valid HTML attribute and returns the encoded string.
        ///    </para> 
        /// </devdoc> 
        public static String HtmlAttributeEncode(String s) {
            if (s == null) 
                return null;

            int pos = IndexOfHtmlAttributeEncodingChars(s, 0);
            if (pos == -1) 
                return s;
 
            StringBuilder builder = new StringBuilder(s.Length + 5); 
            int cch = s.Length;
            int startPos = 0; 
            for (;;) {
                if (pos > startPos) {
                    builder.Append(s, startPos, pos - startPos);
                } 

                char ch = s[pos]; 
                switch (ch) { 
                    case '"':
                        builder.Append("&quot;"); 
                        break;
                    case '&':
                        builder.Append("&amp;");
                        break; 
                    case '<':
                        // Whidbey 32404: The character '<' is not valid in an XML attribute value. 
                        // (See the W3C XML rec). 
                        builder.Append("&lt;");
                        break; 

                    default:
                        Debug.Assert(false, "Unexpected value for ch, ch=" + ch);
                        break; 
                }
 
                startPos = pos + 1; 
                if (startPos >= cch)
                    break; 

                pos = IndexOfHtmlAttributeEncodingChars(s, startPos);
                if (pos == -1) {
                    builder.Append(s, startPos, cch - startPos); 
                    break;
                } 
            } 

            return builder.ToString(); 
        }

        internal static void HtmlAttributeEncodeInternal(String s, HttpWriter writer) {
            int pos = IndexOfHtmlAttributeEncodingChars(s, 0); 
            if (pos == -1) {
                writer.Write(s); 
                return; 
            }
 
            int cch = s.Length;
            int startPos = 0;
            for (;;) {
                if (pos > startPos) { 
                    writer.WriteString(s, startPos, pos - startPos);
                } 
 
                char ch = s[pos];
                switch (ch) { 
                    case '"':
                        writer.Write("&quot;");
                        break;
                    case '&': 
                        writer.Write("&amp;");
                        break; 
                    case '<': 
                        // Whidbey 32404: The character '<' is not valid in an XML attribute value.
                        // (See the W3C XML rec). 
                        writer.Write("&lt;");
                        break;
                }
 
                startPos = pos + 1;
                if (startPos >= cch) 
                    break; 

                pos = IndexOfHtmlAttributeEncodingChars(s, startPos); 
                if (pos == -1) {
                    writer.WriteString(s, startPos, cch - startPos);
                    break;
                } 
            }
        } 
 

 
        /// <devdoc>
        ///    <para>
        ///       Encodes a string to make it a valid HTML attribute and returns the output
        ///       to a TextWriter stream of 
        ///       output.
        ///    </para> 
        /// </devdoc> 
        public unsafe static void HtmlAttributeEncode(String s, TextWriter output) {
            if (s == null) 
                return;

            int index = IndexOfHtmlAttributeEncodingChars(s, 0);
            if (index == -1) { 
                output.Write(s);
            } 
            else { 
                int cch = s.Length - index;
                fixed (char * str = s) { 
                    char *pch = str;
                    while (index-- > 0) {
                        output.Write(*pch++);
                    } 

                    while (cch-- > 0) { 
                        char ch = *pch++; 
                        if (ch <= '<') {
                            switch (ch) { 
                                case '<':
                                    output.Write("&lt;");
                                    break;
                                case '"': 
                                    output.Write("&quot;");
                                    break; 
                                case '&': 
                                    output.Write("&amp;");
                                    break; 
                                default:
                                    output.Write(ch);
                                    break;
                            } 
                        }
                        else { 
                            output.Write(ch); 
                        }
                    } 
                }
            }
        }
 
        static unsafe int IndexOfHtmlAttributeEncodingChars(string s, int startPos) {
            Debug.Assert(0 <= startPos && startPos <= s.Length, "0 <= startPos && startPos <= s.Length"); 
            int cch = s.Length - startPos; 
            fixed (char * str = s) {
                for (char *pch = &str[startPos]; cch > 0; pch++, cch--) { 
                    char ch = *pch;
                    if (ch <= '<') {
                        switch (ch) {
                            case '<': 
                            case '"':
                            case '&': 
                                return s.Length - cch; 
                        }
                    } 
                }
            }

            return -1; 
        }
 
 
        internal static string FormatPlainTextSpacesAsHtml(string s) {
            if (s == null) { 
                return null;
            }

            StringBuilder builder = new StringBuilder(); 
            StringWriter writer = new StringWriter(builder);
 
            int cb = s.Length; 

            for (int i = 0; i < cb; i++) { 
                char ch = s[i];
                if(ch == ' ') {
                    writer.Write("&nbsp;");
                } 
                else {
                    writer.Write(ch); 
                } 
            }
            return builder.ToString(); 
        }

        internal static String FormatPlainTextAsHtml(String s) {
            if (s == null) 
                return null;
 
            StringBuilder builder = new StringBuilder(); 
            StringWriter writer = new StringWriter(builder);
 
            FormatPlainTextAsHtml(s, writer);

            return builder.ToString();
        } 

        internal static void FormatPlainTextAsHtml(String s, TextWriter output) { 
            if (s == null) 
                return;
 
            int cb = s.Length;

            char prevCh = '\0';
 
            for (int i=0; i<cb; i++) {
                char ch = s[i]; 
                switch (ch) { 
                    case '<':
                        output.Write("&lt;"); 
                        break;
                    case '>':
                        output.Write("&gt;");
                        break; 
                    case '"':
                        output.Write("&quot;"); 
                        break; 
                    case '&':
                        output.Write("&amp;"); 
                        break;
                    case ' ':
                        if (prevCh == ' ')
                            output.Write("&nbsp;"); 
                        else
                            output.Write(ch); 
                        break; 
                    case '\r':
                        // Ignore \r, only handle \n 
                        break;
                    case '\n':
                        output.Write("<br>");
                        break; 

                    // 
                    default: 
#if ENTITY_ENCODE_HIGH_ASCII_CHARS
                        // The seemingly arbitrary 160 comes from RFC 
                        if (ch >= 160 && ch < 256) {
                            output.Write("&#");
                            output.Write(((int)ch).ToString(NumberFormatInfo.InvariantInfo));
                            output.Write(';'); 
                            break;
                        } 
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS 

                        output.Write(ch); 
                        break;
                }

                prevCh = ch; 
            }
        } 
 

        ////////////////////////////////////////////////////////////////////////// 
        //
        //  ASII encode - everything all non-7-bit to '?'
        //
 
        /*internal static String AsciiEncode(String s) {
            if (s == null) 
                return null; 

            StringBuilder sb = new StringBuilder(s.Length); 

            for (int i = 0; i < s.Length; i++) {
                char ch = s[i];
                if (((ch & 0xff80) != 0) || (ch < ' ' && ch != '\r' && ch != '\n' && ch != '\t')) 
                    ch = '?';
                sb.Append(ch); 
            } 

            return sb.ToString(); 
        }*/


        // 
        //  Query string parsing support
        // 
 
        public static NameValueCollection ParseQueryString(string query) {
            return ParseQueryString(query, Encoding.UTF8); 
        }

        public static NameValueCollection ParseQueryString(string query, Encoding encoding) {
            if (query == null) { 
                throw new ArgumentNullException("query");
            } 
 
            if (encoding == null) {
                throw new ArgumentNullException("encoding"); 
            }

            if (query.Length > 0 && query[0] == '?') {
                query = query.Substring(1); 
            }
 
            return new HttpValueCollection(query, false, true, encoding); 
        }
 
        //////////////////////////////////////////////////////////////////////////
        //
        //  URL decoding / encoding
        // 
        //////////////////////////////////////////////////////////////////////////
 
        // 
        //  Public static methods
        // 


        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static string UrlEncode(string str) { 
            if (str == null) 
                return null;
            return UrlEncode(str, Encoding.UTF8); 
        }


        /// <devdoc> 
        ///    <para>
        ///       URL encodes a path portion of a URL string and returns the encoded string. 
        ///    </para> 
        /// </devdoc>
        public static string UrlPathEncode(string str) { 
            if (str == null)
                return null;

            // recurse in case there is a query string 
            int i = str.IndexOf('?');
            if (i >= 0) 
                return UrlPathEncode(str.Substring(0, i)) + str.Substring(i); 

            // encode DBCS characters and spaces only 
            return UrlEncodeSpaces(UrlEncodeNonAscii(str, Encoding.UTF8));
        }

        internal static string AspCompatUrlEncode(string s) { 
            s = UrlEncode(s);
            s = s.Replace("!", "%21"); 
            s = s.Replace("*", "%2A"); 
            s = s.Replace("(", "%28");
            s = s.Replace(")", "%29"); 
            s = s.Replace("-", "%2D");
            s = s.Replace(".", "%2E");
            s = s.Replace("_", "%5F");
            s = s.Replace("\\", "%5C"); 
            return s;
        } 
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string UrlEncode(string str, Encoding e) {
            if (str == null) 
                return null;
            return Encoding.ASCII.GetString(UrlEncodeToBytes(str, e)); 
        } 

        //  Helper to encode the non-ASCII url characters only 
        internal static String UrlEncodeNonAscii(string str, Encoding e) {
            if (String.IsNullOrEmpty(str))
                return str;
            if (e == null) 
                e = Encoding.UTF8;
            byte[] bytes = e.GetBytes(str); 
            bytes = UrlEncodeBytesToBytesInternalNonAscii(bytes, 0, bytes.Length, false); 
            return Encoding.ASCII.GetString(bytes);
        } 

        //  Helper to encode spaces only
        internal static String UrlEncodeSpaces(string str) {
            if (str != null && str.IndexOf(' ') >= 0) 
                str = str.Replace(" ", "%20");
            return str; 
        } 

 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string UrlEncode(byte[] bytes) { 
            if (bytes == null)
                return null; 
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes)); 
        }
 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static string UrlEncode(byte[] bytes, int offset, int count) {
            if (bytes == null) 
                return null; 
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, offset, count));
        } 


        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static byte[] UrlEncodeToBytes(string str) { 
            if (str == null) 
                return null;
            return UrlEncodeToBytes(str, Encoding.UTF8); 
        }


        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static byte[] UrlEncodeToBytes(string str, Encoding e) { 
            if (str == null)
                return null; 
            byte[] bytes = e.GetBytes(str);
            return UrlEncodeBytesToBytesInternal(bytes, 0, bytes.Length, false);
        }
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static byte[] UrlEncodeToBytes(byte[] bytes) { 
            if (bytes == null)
                return null;
            return UrlEncodeToBytes(bytes, 0, bytes.Length);
        } 

 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count) {
            if (bytes == null && count == 0)
                return null;
            if (bytes == null) { 
                throw new ArgumentNullException("bytes");
            } 
            if (offset < 0 || offset > bytes.Length) { 
                throw new ArgumentOutOfRangeException("offset");
            } 
            if (count < 0 || offset+count > bytes.Length) {
                throw new ArgumentOutOfRangeException("count");
            }
            return UrlEncodeBytesToBytesInternal(bytes, offset, count, true); 
        }
 
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static string UrlEncodeUnicode(string str) {
            if (str == null)
                return null; 
            return UrlEncodeUnicodeStringToStringInternal(str, false);
        } 
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static byte[] UrlEncodeUnicodeToBytes(string str) {
            if (str == null) 
                return null;
            return Encoding.ASCII.GetBytes(UrlEncodeUnicode(str)); 
        } 

 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string UrlDecode(string str) { 
            if (str == null)
                return null; 
            return UrlDecode(str, Encoding.UTF8); 
        }
 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static string UrlDecode(string str, Encoding e) {
            if (str == null) 
                return null; 
            return UrlDecodeStringFromStringInternal(str, e);
        } 


        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static string UrlDecode(byte[] bytes, Encoding e) { 
            if (bytes == null) 
                return null;
            return UrlDecode(bytes, 0, bytes.Length, e); 
        }


        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static string UrlDecode(byte[] bytes, int offset, int count, Encoding e) { 
            if (bytes == null && count == 0)
                return null; 
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }
            if (offset < 0 || offset > bytes.Length) { 
                throw new ArgumentOutOfRangeException("offset");
            } 
            if (count < 0 || offset+count > bytes.Length) { 
                throw new ArgumentOutOfRangeException("count");
            } 
            return UrlDecodeStringFromBytesInternal(bytes, offset, count, e);
        }

 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public static byte[] UrlDecodeToBytes(string str) {
            if (str == null) 
                return null;
            return UrlDecodeToBytes(str, Encoding.UTF8);
        }
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static byte[] UrlDecodeToBytes(string str, Encoding e) { 
            if (str == null)
                return null;
            return UrlDecodeToBytes(e.GetBytes(str));
        } 

 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static byte[] UrlDecodeToBytes(byte[] bytes) {
            if (bytes == null)
                return null;
            return UrlDecodeToBytes(bytes, 0, (bytes != null) ? bytes.Length : 0); 
        }
 
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count) {
            if (bytes == null && count == 0)
                return null; 
            if (bytes == null) {
                throw new ArgumentNullException("bytes"); 
            } 
            if (offset < 0 || offset > bytes.Length) {
                throw new ArgumentOutOfRangeException("offset"); 
            }
            if (count < 0 || offset+count > bytes.Length) {
                throw new ArgumentOutOfRangeException("count");
            } 
            return UrlDecodeBytesFromBytesInternal(bytes, offset, count);
        } 
 
        //
        //  Implementation for encoding 
        //

        private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue) {
            int cSpaces = 0; 
            int cUnsafe = 0;
 
            // count them first 
            for (int i = 0; i < count; i++) {
                char ch = (char)bytes[offset+i]; 

                if (ch == ' ')
                    cSpaces++;
                else if (!IsSafe(ch)) 
                    cUnsafe++;
            } 
 
            // nothing to expand?
            if (!alwaysCreateReturnValue && cSpaces == 0 && cUnsafe == 0) 
                return bytes;

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cUnsafe*2]; 
            int pos = 0;
 
            for (int i = 0; i < count; i++) { 
                byte b = bytes[offset+i];
                char ch = (char)b; 

                if (IsSafe(ch)) {
                    expandedBytes[pos++] = b;
                } 
                else if (ch == ' ') {
                    expandedBytes[pos++] = (byte)'+'; 
                } 
                else {
                    expandedBytes[pos++] = (byte)'%'; 
                    expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte)IntToHex(b & 0x0f);
                }
            } 

            return expandedBytes; 
        } 

 
        private static bool IsNonAsciiByte(byte b) {
            return ( b >= 0x7F || b < 0x20 );
        }
 

        private static byte[] UrlEncodeBytesToBytesInternalNonAscii(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue) { 
            int cNonAscii = 0; 

            // count them first 
            for (int i = 0; i < count; i++) {
                if ( IsNonAsciiByte(bytes[offset+i]) )
                    cNonAscii++;
            } 

            // nothing to expand? 
            if (!alwaysCreateReturnValue && cNonAscii == 0) 
                return bytes;
 
            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cNonAscii*2];
            int pos = 0;
 
            for (int i = 0; i < count; i++)
            { 
                byte b = bytes[offset+i]; 

                if (IsNonAsciiByte(b)) { 
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte)IntToHex(b & 0x0f);
                } 
                else {
                    expandedBytes[pos++] = b; 
                } 
            }
 
            return expandedBytes;
        }

 
        private static string UrlEncodeUnicodeStringToStringInternal(string s, bool ignoreAscii) {
            int l = s.Length; 
            StringBuilder sb = new StringBuilder(l); 

            for (int i = 0; i < l; i++) { 
                char ch = s[i];

                if ((ch & 0xff80) == 0) {  // 7 bit?
                    if (ignoreAscii || IsSafe(ch)) { 
                        sb.Append(ch);
                    } 
                    else if (ch == ' ') { 
                        sb.Append('+');
                    } 
                    else {
                        sb.Append('%');
                        sb.Append(IntToHex((ch >>  4) & 0xf));
                        sb.Append(IntToHex((ch      ) & 0xf)); 
                    }
                } 
                else { // arbitrary Unicode? 
                    sb.Append("%u");
                    sb.Append(IntToHex((ch >> 12) & 0xf)); 
                    sb.Append(IntToHex((ch >>  8) & 0xf));
                    sb.Append(IntToHex((ch >>  4) & 0xf));
                    sb.Append(IntToHex((ch      ) & 0xf));
                } 
            }
 
            return sb.ToString(); 
        }
 
        //
        //  Implementation for decoding
        //
 
        // Internal class to facilitate URL decoding -- keeps char buffer and byte buffer, allows appending of either chars or bytes
        private class UrlDecoder { 
            private int _bufferSize; 

            // Accumulate characters in a special array 
            private int _numChars;
            private char[] _charBuffer;

            // Accumulate bytes for decoding into characters in a special array 
            private int _numBytes;
            private byte[] _byteBuffer; 
 
            // Encoding to convert chars to bytes
            private Encoding _encoding; 

            private void FlushBytes() {
                if (_numBytes > 0) {
                    _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars); 
                    _numBytes = 0;
                } 
            } 

            internal UrlDecoder(int bufferSize, Encoding encoding) { 
                _bufferSize = bufferSize;
                _encoding = encoding;

                _charBuffer = new char[bufferSize]; 
                // byte buffer created on demand
            } 
 
            internal void AddChar(char ch) {
                if (_numBytes > 0) 
                    FlushBytes();

                _charBuffer[_numChars++] = ch;
            } 

            internal void AddByte(byte b) { 
                // if there are no pending bytes treat 7 bit bytes as characters 
                // this optimization is temp disable as it doesn't work for some encodings
/* 
                if (_numBytes == 0 && ((b & 0x80) == 0)) {
                    AddChar((char)b);
                }
                else 
*/
                { 
                    if (_byteBuffer == null) 
                        _byteBuffer = new byte[_bufferSize];
 
                    _byteBuffer[_numBytes++] = b;
                }
            }
 
            internal String GetString() {
                if (_numBytes > 0) 
                    FlushBytes(); 

                if (_numChars > 0) 
                    return new String(_charBuffer, 0, _numChars);
                else
                    return String.Empty;
            } 
        }
 
        internal static string CollapsePercentUFromStringInternal(string s, Encoding e) { 
            int count = s.Length;
            UrlDecoder helper = new UrlDecoder(count, e); 

            // go thorugh the string's chars collapsing just %uXXXX and
            // appending each char as char
            int loc = s.IndexOf("%u", StringComparison.Ordinal); 
            if(loc == -1) {
                return s; 
            } 

            for (int pos = 0; pos < count; pos++) { 
                char ch = s[pos];

                if (ch == '%' && pos < count-5) {
                    if(s[pos+1] == 'u') { 
                        int h1 = HexToInt(s[pos+2]);
                        int h2 = HexToInt(s[pos+3]); 
                        int h3 = HexToInt(s[pos+4]); 
                        int h4 = HexToInt(s[pos+5]);
 
                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0) { //valid 4 hex chars
                            ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            pos += 5;
 
                            // add as char
                            helper.AddChar(ch); 
                            continue; 
                        }
                    } 
                }
                if ((ch & 0xFF80) == 0)
                    helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                else 
                    helper.AddChar(ch);
            } 
            return helper.GetString(); 
        }
 
        private static string UrlDecodeStringFromStringInternal(string s, Encoding e) {
            int count = s.Length;
            UrlDecoder helper = new UrlDecoder(count, e);
 
            // go through the string's chars collapsing %XX and %uXXXX and
            // appending each char as char, with exception of %XX constructs 
            // that are appended as bytes 

            for (int pos = 0; pos < count; pos++) { 
                char ch = s[pos];

                if (ch == '+') {
                    ch = ' '; 
                }
                else if (ch == '%' && pos < count-2) { 
                    if (s[pos+1] == 'u' && pos < count-5) { 
                        int h1 = HexToInt(s[pos+2]);
                        int h2 = HexToInt(s[pos+3]); 
                        int h3 = HexToInt(s[pos+4]);
                        int h4 = HexToInt(s[pos+5]);

                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0) {   // valid 4 hex chars 
                            ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            pos += 5; 
 
                            // only add as char
                            helper.AddChar(ch); 
                            continue;
                        }
                    }
                    else { 
                        int h1 = HexToInt(s[pos+1]);
                        int h2 = HexToInt(s[pos+2]); 
 
                        if (h1 >= 0 && h2 >= 0) {     // valid 2 hex chars
                            byte b = (byte)((h1 << 4) | h2); 
                            pos += 2;

                            // don't add as char
                            helper.AddByte(b); 
                            continue;
                        } 
                    } 
                }
 
                if ((ch & 0xFF80) == 0)
                    helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                else
                    helper.AddChar(ch); 
            }
 
            return helper.GetString(); 
        }
 
        private static string UrlDecodeStringFromBytesInternal(byte[] buf, int offset, int count, Encoding e) {
            UrlDecoder helper = new UrlDecoder(count, e);

            // go through the bytes collapsing %XX and %uXXXX and appending 
            // each byte as byte, with exception of %uXXXX constructs that
            // are appended as chars 
 
            for (int i = 0; i < count; i++) {
                int pos = offset + i; 
                byte b = buf[pos];

                // The code assumes that + and % cannot be in multibyte sequence
 
                if (b == '+') {
                    b = (byte)' '; 
                } 
                else if (b == '%' && i < count-2) {
                    if (buf[pos+1] == 'u' && i < count-5) { 
                        int h1 = HexToInt((char)buf[pos+2]);
                        int h2 = HexToInt((char)buf[pos+3]);
                        int h3 = HexToInt((char)buf[pos+4]);
                        int h4 = HexToInt((char)buf[pos+5]); 

                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0) {   // valid 4 hex chars 
                            char ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4); 
                            i += 5;
 
                            // don't add as byte
                            helper.AddChar(ch);
                            continue;
                        } 
                    }
                    else { 
                        int h1 = HexToInt((char)buf[pos+1]); 
                        int h2 = HexToInt((char)buf[pos+2]);
 
                        if (h1 >= 0 && h2 >= 0) {     // valid 2 hex chars
                            b = (byte)((h1 << 4) | h2);
                            i += 2;
                        } 
                    }
                } 
 
                helper.AddByte(b);
            } 

            return helper.GetString();
        }
 
        private static byte[] UrlDecodeBytesFromBytesInternal(byte[] buf, int offset, int count) {
            int decodedBytesCount = 0; 
            byte[] decodedBytes = new byte[count]; 

            for (int i = 0; i < count; i++) { 
                int pos = offset + i;
                byte b = buf[pos];

                if (b == '+') { 
                    b = (byte)' ';
                } 
                else if (b == '%' && i < count-2) { 
                    int h1 = HexToInt((char)buf[pos+1]);
                    int h2 = HexToInt((char)buf[pos+2]); 

                    if (h1 >= 0 && h2 >= 0) {     // valid 2 hex chars
                        b = (byte)((h1 << 4) | h2);
                        i += 2; 
                    }
                } 
 
                decodedBytes[decodedBytesCount++] = b;
            } 

            if (decodedBytesCount < decodedBytes.Length) {
                byte[] newDecodedBytes = new byte[decodedBytesCount];
                Array.Copy(decodedBytes, newDecodedBytes, decodedBytesCount); 
                decodedBytes = newDecodedBytes;
            } 
 
            return decodedBytes;
        } 

        //
        // Private helpers for URL encoding/decoding
        // 

        private static int HexToInt(char h) { 
            return( h >= '0' && h <= '9' ) ? h - '0' : 
            ( h >= 'a' && h <= 'f' ) ? h - 'a' + 10 :
            ( h >= 'A' && h <= 'F' ) ? h - 'A' + 10 : 
            -1;
        }

        internal static char IntToHex(int n) { 
            Debug.Assert(n < 0x10);
 
            if (n <= 9) 
                return(char)(n + (int)'0');
            else 
                return(char)(n - 10 + (int)'a');
        }

        // Set of safe chars, from RFC 1738.4 minus '+' 
        internal static bool IsSafe(char ch) {
            if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9') 
                return true; 

            switch (ch) { 
                case '-':
                case '_':
                case '.':
                case '!': 
                case '*':
                case '\'': 
                case '(': 
                case ')':
                    return true; 
            }

            return false;
        } 

        ////////////////////////////////////////////////////////////////////////// 
        // 
        //  Misc helpers
        // 
        //////////////////////////////////////////////////////////////////////////

        internal static String FormatHttpDateTime(DateTime dt) {
            if (dt < DateTime.MaxValue.AddDays(-1) && dt > DateTime.MinValue.AddDays(1)) 
                dt = dt.ToUniversalTime();
            return dt.ToString("R", DateTimeFormatInfo.InvariantInfo); 
        } 

        internal static String FormatHttpDateTimeUtc(DateTime dt) { 
            return dt.ToString("R", DateTimeFormatInfo.InvariantInfo);
        }

        internal static String FormatHttpCookieDateTime(DateTime dt) { 
            if (dt < DateTime.MaxValue.AddDays(-1) && dt > DateTime.MinValue.AddDays(1))
                dt = dt.ToUniversalTime(); 
            return dt.ToString("ddd, dd-MMM-yyyy HH':'mm':'ss 'GMT'", DateTimeFormatInfo.InvariantInfo); 
        }
    } 

    // helper class for lookup of HTML encoding entities
    internal class HtmlEntities {
        private static object _lookupLockObject = new object(); 

        // The list is from http://www.w3.org/TR/REC-html40/sgml/entities.html 
        private static String[] _entitiesList = new String[] { 
            "\x0022-quot",
            "\x0026-amp", 
            "\x003c-lt",
            "\x003e-gt",
            "\x00a0-nbsp",
            "\x00a1-iexcl", 
            "\x00a2-cent",
            "\x00a3-pound", 
            "\x00a4-curren", 
            "\x00a5-yen",
            "\x00a6-brvbar", 
            "\x00a7-sect",
            "\x00a8-uml",
            "\x00a9-copy",
            "\x00aa-ordf", 
            "\x00ab-laquo",
            "\x00ac-not", 
            "\x00ad-shy", 
            "\x00ae-reg",
            "\x00af-macr", 
            "\x00b0-deg",
            "\x00b1-plusmn",
            "\x00b2-sup2",
            "\x00b3-sup3", 
            "\x00b4-acute",
            "\x00b5-micro", 
            "\x00b6-para", 
            "\x00b7-middot",
            "\x00b8-cedil", 
            "\x00b9-sup1",
            "\x00ba-ordm",
            "\x00bb-raquo",
            "\x00bc-frac14", 
            "\x00bd-frac12",
            "\x00be-frac34", 
            "\x00bf-iquest", 
            "\x00c0-Agrave",
            "\x00c1-Aacute", 
            "\x00c2-Acirc",
            "\x00c3-Atilde",
            "\x00c4-Auml",
            "\x00c5-Aring", 
            "\x00c6-AElig",
            "\x00c7-Ccedil", 
            "\x00c8-Egrave", 
            "\x00c9-Eacute",
            "\x00ca-Ecirc", 
            "\x00cb-Euml",
            "\x00cc-Igrave",
            "\x00cd-Iacute",
            "\x00ce-Icirc", 
            "\x00cf-Iuml",
            "\x00d0-ETH", 
            "\x00d1-Ntilde", 
            "\x00d2-Ograve",
            "\x00d3-Oacute", 
            "\x00d4-Ocirc",
            "\x00d5-Otilde",
            "\x00d6-Ouml",
            "\x00d7-times", 
            "\x00d8-Oslash",
            "\x00d9-Ugrave", 
            "\x00da-Uacute", 
            "\x00db-Ucirc",
            "\x00dc-Uuml", 
            "\x00dd-Yacute",
            "\x00de-THORN",
            "\x00df-szlig",
            "\x00e0-agrave", 
            "\x00e1-aacute",
            "\x00e2-acirc", 
            "\x00e3-atilde", 
            "\x00e4-auml",
            "\x00e5-aring", 
            "\x00e6-aelig",
            "\x00e7-ccedil",
            "\x00e8-egrave",
            "\x00e9-eacute", 
            "\x00ea-ecirc",
            "\x00eb-euml", 
            "\x00ec-igrave", 
            "\x00ed-iacute",
            "\x00ee-icirc", 
            "\x00ef-iuml",
            "\x00f0-eth",
            "\x00f1-ntilde",
            "\x00f2-ograve", 
            "\x00f3-oacute",
            "\x00f4-ocirc", 
            "\x00f5-otilde", 
            "\x00f6-ouml",
            "\x00f7-divide", 
            "\x00f8-oslash",
            "\x00f9-ugrave",
            "\x00fa-uacute",
            "\x00fb-ucirc", 
            "\x00fc-uuml",
            "\x00fd-yacute", 
            "\x00fe-thorn", 
            "\x00ff-yuml",
            "\x0152-OElig", 
            "\x0153-oelig",
            "\x0160-Scaron",
            "\x0161-scaron",
            "\x0178-Yuml", 
            "\x0192-fnof",
            "\x02c6-circ", 
            "\x02dc-tilde", 
            "\x0391-Alpha",
            "\x0392-Beta", 
            "\x0393-Gamma",
            "\x0394-Delta",
            "\x0395-Epsilon",
            "\x0396-Zeta", 
            "\x0397-Eta",
            "\x0398-Theta", 
            "\x0399-Iota", 
            "\x039a-Kappa",
            "\x039b-Lambda", 
            "\x039c-Mu",
            "\x039d-Nu",
            "\x039e-Xi",
            "\x039f-Omicron", 
            "\x03a0-Pi",
            "\x03a1-Rho", 
            "\x03a3-Sigma", 
            "\x03a4-Tau",
            "\x03a5-Upsilon", 
            "\x03a6-Phi",
            "\x03a7-Chi",
            "\x03a8-Psi",
            "\x03a9-Omega", 
            "\x03b1-alpha",
            "\x03b2-beta", 
            "\x03b3-gamma", 
            "\x03b4-delta",
            "\x03b5-epsilon", 
            "\x03b6-zeta",
            "\x03b7-eta",
            "\x03b8-theta",
            "\x03b9-iota", 
            "\x03ba-kappa",
            "\x03bb-lambda", 
            "\x03bc-mu", 
            "\x03bd-nu",
            "\x03be-xi", 
            "\x03bf-omicron",
            "\x03c0-pi",
            "\x03c1-rho",
            "\x03c2-sigmaf", 
            "\x03c3-sigma",
            "\x03c4-tau", 
            "\x03c5-upsilon", 
            "\x03c6-phi",
            "\x03c7-chi", 
            "\x03c8-psi",
            "\x03c9-omega",
            "\x03d1-thetasym",
            "\x03d2-upsih", 
            "\x03d6-piv",
            "\x2002-ensp", 
            "\x2003-emsp", 
            "\x2009-thinsp",
            "\x200c-zwnj", 
            "\x200d-zwj",
            "\x200e-lrm",
            "\x200f-rlm",
            "\x2013-ndash", 
            "\x2014-mdash",
            "\x2018-lsquo", 
            "\x2019-rsquo", 
            "\x201a-sbquo",
            "\x201c-ldquo", 
            "\x201d-rdquo",
            "\x201e-bdquo",
            "\x2020-dagger",
            "\x2021-Dagger", 
            "\x2022-bull",
            "\x2026-hellip", 
            "\x2030-permil", 
            "\x2032-prime",
            "\x2033-Prime", 
            "\x2039-lsaquo",
            "\x203a-rsaquo",
            "\x203e-oline",
            "\x2044-frasl", 
            "\x20ac-euro",
            "\x2111-image", 
            "\x2118-weierp", 
            "\x211c-real",
            "\x2122-trade", 
            "\x2135-alefsym",
            "\x2190-larr",
            "\x2191-uarr",
            "\x2192-rarr", 
            "\x2193-darr",
            "\x2194-harr", 
            "\x21b5-crarr", 
            "\x21d0-lArr",
            "\x21d1-uArr", 
            "\x21d2-rArr",
            "\x21d3-dArr",
            "\x21d4-hArr",
            "\x2200-forall", 
            "\x2202-part",
            "\x2203-exist", 
            "\x2205-empty", 
            "\x2207-nabla",
            "\x2208-isin", 
            "\x2209-notin",
            "\x220b-ni",
            "\x220f-prod",
            "\x2211-sum", 
            "\x2212-minus",
            "\x2217-lowast", 
            "\x221a-radic", 
            "\x221d-prop",
            "\x221e-infin", 
            "\x2220-ang",
            "\x2227-and",
            "\x2228-or",
            "\x2229-cap", 
            "\x222a-cup",
            "\x222b-int", 
            "\x2234-there4", 
            "\x223c-sim",
            "\x2245-cong", 
            "\x2248-asymp",
            "\x2260-ne",
            "\x2261-equiv",
            "\x2264-le", 
            "\x2265-ge",
            "\x2282-sub", 
            "\x2283-sup", 
            "\x2284-nsub",
            "\x2286-sube", 
            "\x2287-supe",
            "\x2295-oplus",
            "\x2297-otimes",
            "\x22a5-perp", 
            "\x22c5-sdot",
            "\x2308-lceil", 
            "\x2309-rceil", 
            "\x230a-lfloor",
            "\x230b-rfloor", 
            "\x2329-lang",
            "\x232a-rang",
            "\x25ca-loz",
            "\x2660-spades", 
            "\x2663-clubs",
            "\x2665-hearts", 
            "\x2666-diams", 
        };
 
        private static Hashtable _entitiesLookupTable;

        private HtmlEntities() {
        } 

        internal /*public*/ static char Lookup(String entity) { 
            if (_entitiesLookupTable == null) { 
                // populate hashtable on demand
                lock (_lookupLockObject) { 
                    if (_entitiesLookupTable == null) {
                        Hashtable t = new Hashtable();

                        foreach (String s in _entitiesList) 
                            t[s.Substring(2)] = s[0];  // 1st char is the code, 2nd '-'
 
                        _entitiesLookupTable = t; 
                    }
                } 
            }

            Object obj = _entitiesLookupTable[entity];
 
            if (obj != null)
                return (char)obj; 
            else 
                return (char)0;
        } 
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="httpserverutility.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Server intrinsic used to match ASP's object model 
 *
 * Copyright (c) 1999 Microsoft Corporation 
 */

// Don't entity encode high chars (160 to 256), to fix bugs VSWhidbey 85857/111927
// 
#define ENTITY_ENCODE_HIGH_ASCII_CHARS
 
namespace System.Web { 
    using System.Text;
    using System.Runtime.Serialization.Formatters; 
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.IO;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Globalization; 
    using System.Web.Hosting; 
    using System.Web.Util;
    using System.Web.UI; 
    using System.Web.Configuration;
    using System.Security;
    using System.Security.Permissions;
    using System.Web.Management;     
    using System.Web.SessionState;

    internal abstract class ErrorFormatterGenerator { 
        internal abstract ErrorFormatter GetErrorFormatter(Exception e); 
    }
 

    /// <devdoc>
    ///    <para>
    ///       Provides several 
    ///       helper methods that can be used in the processing of Web requests.
    ///    </para> 
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class HttpServerUtility { 
        private HttpContext _context;
        private HttpApplication _application;

        private static IDictionary _cultureCache = Hashtable.Synchronized(new Hashtable()); 

        internal HttpServerUtility(HttpContext context) { 
            _context = context; 
        }
 
        internal HttpServerUtility(HttpApplication application) {
            _application = application;
        }
 
        //
        // Misc ASP compatibility methods 
        // 

 
        /// <devdoc>
        ///    <para>
        ///       Instantiates a COM object identified via a progid.
        ///    </para> 
        /// </devdoc>
        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public object CreateObject(string progID) { 
            Type type = null;
            object obj = null; 

            try {
#if !FEATURE_PAL // FEATURE_PAL does not enable COM
                type = Type.GetTypeFromProgID(progID); 
#else // !FEATURE_PAL
                throw new NotImplementedException("ROTORTODO"); 
#endif // !FEATURE_PAL 
            }
            catch { 
            }

            if (type == null) {
                throw new HttpException(SR.GetString(SR.Could_not_create_object_of_type, progID)); 
            }
 
            // Disallow Apartment components in non-compat mode 
            AspCompatApplicationStep.CheckThreadingModel(progID, type.GUID);
 
            // Instantiate the object
            obj = Activator.CreateInstance(type);

            // For ASP compat: take care of OnPageStart/OnPageEnd 
            AspCompatApplicationStep.OnPageStart(obj);
 
            return obj; 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Instantiates a COM object identified via a Type. 
        ///    </para>
        /// </devdoc> 
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)] 
        public object CreateObject(Type type) {
            // Disallow Apartment components in non-compat mode 
            AspCompatApplicationStep.CheckThreadingModel(type.FullName, type.GUID);

            // Instantiate the object
            Object obj = Activator.CreateInstance(type); 

            // For ASP compat: take care of OnPageStart/OnPageEnd 
            AspCompatApplicationStep.OnPageStart(obj); 

            return obj; 
        }


 
        /// <devdoc>
        ///    <para> 
        ///       Instantiates a COM object identified via a clsid. 
        ///    </para>
        /// </devdoc> 
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)]
        public object CreateObjectFromClsid(string clsid) {
            Type type = null;
            object obj = null; 

            // Create a Guid out of it 
            Guid guid = new Guid(clsid); 

            // Disallow Apartment components in non-compat mode 
            AspCompatApplicationStep.CheckThreadingModel(clsid, guid);

            try {
#if !FEATURE_PAL // FEATURE_PAL does not enable COM 
                type = Type.GetTypeFromCLSID(guid, null, true /*throwOnError*/);
#else // !FEATURE_PAL 
                throw new NotImplementedException("ROTORTODO"); 
#endif // !FEATURE_PAL
 
                // Instantiate the object
                obj = Activator.CreateInstance(type);
            }
            catch { 
            }
 
            if (obj == null) { 
                throw new HttpException(
                    SR.GetString(SR.Could_not_create_object_from_clsid, clsid)); 
            }

            // For ASP compat: take care of OnPageStart/OnPageEnd
            AspCompatApplicationStep.OnPageStart(obj); 

            return obj; 
        } 

        // Internal static method that returns a read-only, non-user override accounted, CultureInfo object 
        internal static CultureInfo CreateReadOnlyCultureInfo(string name) {
            if (!_cultureCache.Contains(name)) {
                // To be threadsafe, get the lock before creating
                lock (_cultureCache) { 
                    if (_cultureCache[name] == null) {
                        _cultureCache[name] = CultureInfo.ReadOnly(new CultureInfo(name)); 
                    } 
                }
            } 
            return (CultureInfo)_cultureCache[name];
        }

        // Internal static method that returns a read-only, non-user override accounted, culture specific CultureInfo object 
        internal static CultureInfo CreateReadOnlySpecificCultureInfo(string name) {
            if(name.IndexOf('-') > 0) { 
                return CreateReadOnlyCultureInfo(name); 
            }
            CultureInfo ci = CultureInfo.CreateSpecificCulture(name); 
            if (!_cultureCache.Contains(ci.Name)) {
                //To be threadsafe, get the lock before creating
                lock (_cultureCache) {
                    if (_cultureCache[ci.Name] == null) { 
                        _cultureCache[ci.Name] = CultureInfo.ReadOnly(ci);
                    } 
                } 
            }
            return (CultureInfo)_cultureCache[ci.Name]; 
        }

        // Internal static method that returns a read-only, non-user override accounted, CultureInfo object
        internal static CultureInfo CreateReadOnlyCultureInfo(int culture) { 
            if (!_cultureCache.Contains(culture)) {
                // To be threadsafe, get the lock before creating 
                lock (_cultureCache) { 
                    if (_cultureCache[culture] == null) {
                        _cultureCache[culture] = CultureInfo.ReadOnly(new CultureInfo(culture)); 
                    }
                }
            }
            return (CultureInfo)_cultureCache[culture]; 
        }
 
        /// <devdoc> 
        ///    <para>
        ///       Maps a virtual path to a physical path. 
        ///    </para>
        /// </devdoc>
        public string MapPath(string path) {
            if (_context == null) 
                throw new HttpException(SR.GetString(SR.Server_not_available));
            // Disable hiding the request so that Server.MapPath works when called from 
            // Application_Start in integrated mode 
            bool unhideRequest = _context.HideRequestResponse;
            string realPath; 
            try {
                if (unhideRequest) {
                    _context.HideRequestResponse = false;
                } 
                realPath = _context.Request.MapPath(path);
            } 
            finally { 
                if (unhideRequest) {
                    _context.HideRequestResponse = true; 
                }
            }
            return realPath;
        } 

 
        /// <devdoc> 
        ///    <para>Returns the last recorded exception.</para>
        /// </devdoc> 
        public Exception GetLastError() {
            if (_context != null)
                return _context.Error;
            else if (_application != null) 
                return _application.LastError;
            else 
                return null; 
        }
 

        /// <devdoc>
        ///    <para>Clears the last error.</para>
        /// </devdoc> 
        public void ClearError() {
            if (_context != null) 
                _context.ClearError(); 
            else if (_application != null)
                _application.ClearError(); 
        }

        //
        // Server.Transfer/Server.Execute -- child requests 
        //
 
 
        /// <devdoc>
        ///    <para> 
        ///       Executes a new request (using the specified URL path as the target). Unlike
        ///       the Transfer method, execution of the original page continues after the executed
        ///       page completes.
        ///    </para> 
        /// </devdoc>
        public void Execute(string path) { 
            Execute(path, null, true /*preserveForm*/); 
        }
 

        /// <devdoc>
        ///    <para>
        ///       Executes a new request (using the specified URL path as the target). Unlike 
        ///       the Transfer method, execution of the original page continues after the executed
        ///       page completes. 
        ///    </para> 
        /// </devdoc>
        public void Execute(string path, TextWriter writer) { 
            Execute(path, writer, true /*preserveForm*/);
        }

 
        /// <devdoc>
        ///    <para> 
        ///       Executes a new request (using the specified URL path as the target). Unlike 
        ///       the Transfer method, execution of the original page continues after the executed
        ///       page completes. 
        ///       If preserveForm is false, the QueryString and Form collections are cleared.
        ///    </para>
        /// </devdoc>
        public void Execute(string path, bool preserveForm) { 
            Execute(path, null, preserveForm);
        } 
 

        /// <devdoc> 
        ///    <para>
        ///       Executes a new request (using the specified URL path as the target). Unlike
        ///       the Transfer method, execution of the original page continues after the executed
        ///       page completes. 
        ///       If preserveForm is false, the QueryString and Form collections are cleared.
        ///    </para> 
        /// </devdoc> 
        public void Execute(string path, TextWriter writer, bool preserveForm) {
            if (_context == null) 
                throw new HttpException(SR.GetString(SR.Server_not_available));

            if (path == null)
                throw new ArgumentNullException("path"); 

            string queryStringOverride = null; 
            HttpRequest request = _context.Request; 
            HttpResponse response = _context.Response;
 
            // Remove potential cookie-less session id (ASURT 100558)
            path = response.RemoveAppPathModifier(path);

            // Allow query string override 
            int iqs = path.IndexOf('?');
            if (iqs >= 0) { 
                queryStringOverride = path.Substring(iqs+1); 
                path = path.Substring(0, iqs);
            } 

            if (!UrlPath.IsValidVirtualPathWithoutProtocol(path))
                throw new ArgumentException(SR.GetString(SR.Invalid_path_for_child_request, path));
 
            VirtualPath virtualPath = VirtualPath.Create(path);
 
            // Find the handler for the path 

            IHttpHandler handler = null; 

            string physPath = request.MapPath(virtualPath);        // get physical path
            VirtualPath filePath = request.FilePathObject.Combine(virtualPath);    // vpath
 
            // Demand read access to the physical path of the target handler
            InternalSecurityPermissions.FileReadAccess(physPath).Demand(); 
 
            // We need to Assert since there typically is user code on the stack (VSWhidbey 270965)
            InternalSecurityPermissions.Unrestricted.Assert(); 

            try {
                // paths that ends with . are disallowed as they are used to get around
                // extension mappings and server source as static file 
                if (StringUtil.StringEndsWith(virtualPath.VirtualPathString, '.'))
                    throw new HttpException(404, String.Empty); 
 
                bool useAppConfig = !filePath.IsWithinAppRoot;
 
                using (new HttpContextWrapper(_context)) {

                    try {
                        // We need to increase the depth when calling MapHttpHandler, 
                        // since PageHandlerFactory relies on it
                        _context.ServerExecuteDepth++; 
 
                        if (_context.WorkerRequest is IIS7WorkerRequest) {
                            handler = _context.ApplicationInstance.MapIntegratedHttpHandler( 
                                _context,
                                request.RequestType,
                                filePath,
                                physPath, 
                                useAppConfig,
                                true /*convertNativeStaticFileModule*/); 
                        } 
                        else {
                            handler = _context.ApplicationInstance.MapHttpHandler( 
                                _context,
                                request.RequestType,
                                filePath,
                                physPath, 
                                useAppConfig);
                        } 
                    } 
                    finally {
                        _context.ServerExecuteDepth--; 
                    }
                }
            }
            catch (Exception e) { 
                // 500 errors (compilation errors) get preserved
                if (e is HttpException) { 
                    int code = ((HttpException)e).GetHttpCode(); 

                    if (code != 500 && code != 404) { 
                        e = null;
                    }
                }
 
                throw new HttpException(SR.GetString(SR.Error_executing_child_request_for_path, path), e);
            } 
 
            ExecuteInternal(handler, writer, preserveForm, true /*setPreviousPage*/,
                virtualPath, filePath, physPath, null, queryStringOverride); 
        }


        public void Execute(IHttpHandler handler, TextWriter writer, bool preserveForm) { 
            if (_context == null)
                throw new HttpException(SR.GetString(SR.Server_not_available)); 
 
            Execute(handler, writer, preserveForm, true /*setPreviousPage*/);
        } 

        internal void Execute(IHttpHandler handler, TextWriter writer, bool preserveForm, bool setPreviousPage) {
            HttpRequest request = _context.Request;
            VirtualPath filePath = request.CurrentExecutionFilePathObject; 
            string physicalPath = request.MapPath(filePath);
 
            ExecuteInternal(handler, writer, preserveForm, setPreviousPage, 
                null, filePath, physicalPath, null, null);
        } 

        private void ExecuteInternal(IHttpHandler handler, TextWriter writer, bool preserveForm, bool setPreviousPage,
            VirtualPath path, VirtualPath filePath, string physPath, Exception error, string queryStringOverride) {
 
            if (handler == null)
                throw new ArgumentNullException("handler"); 
 
            HttpRequest request = _context.Request;
            HttpResponse response = _context.Response; 
            HttpApplication app = _context.ApplicationInstance;

            HttpValueCollection savedForm = null;
            VirtualPath savedCurrentExecutionFilePath = null; 
            string savedQueryString = null;
            TextWriter savedOutputWriter = null; 
            AspNetSynchronizationContext savedSyncContext = null; 

            // Transaction wouldn't flow into ASPCOMPAT mode -- need to report an error 
            VerifyTransactionFlow(handler);

            // create new trace context
            _context.PushTraceContext(); 

            // set the new handler as the current handler 
            _context.SetCurrentHandler(handler); 

            // because we call this synchrnously async operations must be disabled 
            bool originalSyncContextWasEnabled = _context.SyncContext.Enabled;
            _context.SyncContext.Disable();

            // Execute the handler 
            try {
                try { 
                    _context.ServerExecuteDepth++; 

                    savedCurrentExecutionFilePath = request.SwitchCurrentExecutionFilePath(filePath); 

                    if (!preserveForm) {
                        savedForm = request.SwitchForm(new HttpValueCollection());
 
                        // Clear out the query string, but honor overrides
                        if (queryStringOverride == null) 
                            queryStringOverride = String.Empty; 
                    }
 
                    // override query string if requested
                    if (queryStringOverride != null) {
                        savedQueryString = request.QueryStringText;
                        request.QueryStringText = queryStringOverride; 
                    }
 
                    // capture output if requested 
                    if (writer != null)
                        savedOutputWriter = response.SwitchWriter(writer); 

                    Page targetPage = handler as Page;
                    if (targetPage != null) {
                        if (setPreviousPage) { 
                            // Set the previousPage of the new Page as the previous Page
                            targetPage.SetPreviousPage(_context.PreviousHandler as Page); 
                        } 

                        Page sourcePage = _context.Handler as Page; 

#pragma warning disable 0618    // To avoid deprecation warning
                        // If the source page of the transfer has smart nav on,
                        // always do as if the destination has it too (ASURT 97732) 
                        if (sourcePage != null && sourcePage.SmartNavigation)
                            targetPage.SmartNavigation = true; 
#pragma warning restore 0618 

                        // If the target page is async need to save/restore sync context 
                        if (targetPage is IHttpAsyncHandler) {
                            savedSyncContext = _context.InstallNewAspNetSynchronizationContext();
                        }
                    } 

                    if ((handler is StaticFileHandler || handler is DefaultHttpHandler) && 
                       !DefaultHttpHandler.IsClassicAspRequest(filePath.VirtualPathString)) { 
                        // cannot apply static files handler directly
                        // -- it would dump the source of the current page 
                        // instead just dump the file content into response
                        try {
                            response.WriteFile(physPath);
                        } 
                        catch {
                            // hide the real error as it could be misleading 
                            // in case of mismapped requests like /foo.asmx/bar 
                            error = new HttpException(404, String.Empty);
                        } 
                    }
                    else if (!(handler is Page)) {
                        // disallow anything but pages
                        error = new HttpException(404, String.Empty); 
                    }
                    else if (handler is IHttpAsyncHandler) { 
                        // Asynchronous handler 

                        // suspend cancellable period (don't abort this thread while 
                        // we wait for another to finish)
                        bool isCancellable =  _context.IsInCancellablePeriod;
                        if (isCancellable)
                            _context.EndCancellablePeriod(); 

                        try { 
                            IHttpAsyncHandler asyncHandler = (IHttpAsyncHandler)handler; 

                            IAsyncResult ar = asyncHandler.BeginProcessRequest(_context, null, null); 

                            // wait for completion
                            if (!ar.IsCompleted) {
                                // suspend app lock while waiting 
                                bool needToRelock = false;
 
                                try { 
                                    try {} finally {
                                        Monitor.Exit(app); 
                                        needToRelock = true;
                                    }

                                    WaitHandle h = ar.AsyncWaitHandle; 

                                    if (h != null) { 
                                        h.WaitOne(); 
                                    }
                                    else { 
                                        while (!ar.IsCompleted)
                                            Thread.Sleep(1);
                                    }
                                } 
                                finally {
                                    if (needToRelock) { 
                                        Monitor.Enter(app); 
                                    }
                                } 
                            }

                            // end the async operation (get error if any)
 
                            try {
                                asyncHandler.EndProcessRequest(ar); 
                            } 
                            catch (Exception e) {
                                error = e; 
                            }
                        }
                        finally {
                            // resume cancelleable period 
                            if (isCancellable)
                                _context.BeginCancellablePeriod(); 
                        } 
                    }
                    else { 
                        // Synchronous handler

                        using (new HttpContextWrapper(_context)) {
                            try { 
                                handler.ProcessRequest(_context);
                            } 
                            catch (Exception e) { 
                                error = e;
                            } 
                        }
                    }
                }
                finally { 
                    _context.ServerExecuteDepth--;
 
                    // Restore the handlers; 
                    _context.RestoreCurrentHandler();
 
                    // restore output writer
                    if (savedOutputWriter != null)
                        response.SwitchWriter(savedOutputWriter);
 
                    // restore overriden query string
                    if (queryStringOverride != null && savedQueryString != null) 
                        request.QueryStringText = savedQueryString; 

                    if (savedForm != null) 
                        request.SwitchForm(savedForm);

                    request.SwitchCurrentExecutionFilePath(savedCurrentExecutionFilePath);
 
                    if (savedSyncContext != null) {
                        _context.RestoreSavedAspNetSynchronizationContext(savedSyncContext); 
                    } 

                    if (originalSyncContextWasEnabled) { 
                        _context.SyncContext.Enable();
                    }

                    // restore trace context 
                    _context.PopTraceContext();
                } 
            } 
            catch { // Protect against exception filters
                throw; 
            }

            // Report any error
            if (error != null) { 
                // suppress errors with HTTP codes (for child requests they mislead more than help)
                if (error is HttpException && ((HttpException)error).GetHttpCode() != 500) 
                    error = null; 

                if (path != null) 
                    throw new HttpException(SR.GetString(SR.Error_executing_child_request_for_path, path), error);

                throw new HttpException(SR.GetString(SR.Error_executing_child_request_for_handler, handler.GetType().ToString()), error);
            } 
        }
 
 
        /// <devdoc>
        ///    <para> 
        ///       Terminates execution of the current page and begins execution of a new
        ///       request using the supplied URL path.
        ///       If preserveForm is false, the QueryString and Form collections are cleared.
        ///    </para> 
        /// </devdoc>
        public void Transfer(string path, bool preserveForm) { 
            Page page = _context.Handler as Page; 
            if ((page != null) && page.IsCallback) {
                throw new ApplicationException(SR.GetString(SR.Transfer_not_allowed_in_callback)); 
            }

            // execute child request
 
            Execute(path, null, preserveForm);
 
            // suppress the remainder of the current one 

            _context.Response.End(); 
        }


        /// <devdoc> 
        ///    <para>
        ///       Terminates execution of the current page and begins execution of a new 
        ///       request using the supplied URL path. 
        ///    </para>
        /// </devdoc> 
        public void Transfer(string path) {
            // Make sure the transfer is not treated as a postback, which could cause a stack
            // overflow if the user doesn't expect it (VSWhidbey 181013).
            // If the use *does* want it treated as a postback, they can call Transfer(path, true). 
            bool savedPreventPostback = _context.PreventPostback;
            _context.PreventPostback = true; 
 
            Transfer(path, true /*preserveForm*/);
 
            _context.PreventPostback = savedPreventPostback;
        }

 
        public void Transfer(IHttpHandler handler, bool preserveForm) {
            Page page = handler as Page; 
            if ((page != null) && page.IsCallback) { 
                throw new ApplicationException(SR.GetString(SR.Transfer_not_allowed_in_callback));
            } 

            Execute(handler, null, preserveForm);

            // suppress the remainder of the current one 

            _context.Response.End(); 
        } 

        public void TransferRequest(string path) 
        {
            TransferRequest(path, false, null, null);
        }
 
        public void TransferRequest(string path, bool preserveForm)
        { 
            TransferRequest(path, preserveForm, null, null); 
        }
 
        public void TransferRequest(string path, bool preserveForm, string method, NameValueCollection headers) {
            if (!HttpRuntime.UseIntegratedPipeline) {
                throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode));
            } 

            if (_context == null) { 
                throw new HttpException(SR.GetString(SR.Server_not_available)); 
            }
 
            if (path == null) {
                throw new ArgumentNullException("path");
            }
 
            IIS7WorkerRequest wr = _context.WorkerRequest as IIS7WorkerRequest;
            HttpRequest request = _context.Request; 
            HttpResponse response = _context.Response; 

            if (wr == null) { 
                throw new HttpException(SR.GetString(SR.Server_not_available));
            }

            // Remove potential cookie-less session id (ASURT 100558) 
            path = response.RemoveAppPathModifier(path);
 
            // Extract query string if specified 
            String qs = null;
            int iqs = path.IndexOf('?'); 
            if (iqs >= 0) {
                qs = (iqs < path.Length-1) ? path.Substring(iqs+1) : String.Empty;
                path = path.Substring(0, iqs);
            } 

            if (!UrlPath.IsValidVirtualPathWithoutProtocol(path)) { 
                throw new ArgumentException(SR.GetString(SR.Invalid_path_for_child_request, path)); 
            }
 
            VirtualPath virtualPath = request.FilePathObject.Combine(VirtualPath.Create(path));
            string physPath = request.MapPath(path);

            // Demand read access to the physical path of the target handler 
            InternalSecurityPermissions.FileReadAccess(physPath).Demand();
 
            //  Schedule the child execution 
            wr.ScheduleExecuteUrl( virtualPath.VirtualPathString,
                                   qs, 
                                   method,
                                   preserveForm,
                                   preserveForm ? request.EntityBody : null,
                                   headers); 

            // force the completion of the current request so that the 
            // child execution can be performed immediately after unwind 
            _context.ApplicationInstance.EnsureReleaseState();
            _context.Response.End();
        } 

        private void VerifyTransactionFlow(IHttpHandler handler) {
            Page topPage = _context.Handler as Page;
            Page childPage = handler as Page; 

            if (childPage != null && childPage.IsInAspCompatMode && // child page aspcompat 
                topPage != null && !topPage.IsInAspCompatMode &&    // top page is not aspcompat 
                Transactions.Utils.IsInTransaction) {               // we are in transaction
 
                throw new HttpException(SR.GetString(SR.Transacted_page_calls_aspcompat));
            }
        }
 
        //
        // Static method to execute a request outside of HttpContext and capture the response 
        // 

        internal static void ExecuteLocalRequestAndCaptureResponse(String path, TextWriter writer, 
                                                                ErrorFormatterGenerator errorFormatterGenerator) {
            HttpRequest request = new HttpRequest(
                                          VirtualPath.CreateAbsolute(path),
                                          String.Empty); 

            HttpResponse response = new HttpResponse(writer); 
 
            HttpContext context = new HttpContext(request, response);
 
            HttpApplication app = HttpApplicationFactory.GetApplicationInstance(context) as HttpApplication;
            context.ApplicationInstance = app;

            try { 
                context.Server.Execute(path);
            } 
            catch (HttpException e) { 
                if (errorFormatterGenerator != null) {
                    context.Response.SetOverrideErrorFormatter(errorFormatterGenerator.GetErrorFormatter(e)); 
                }

                context.Response.ReportRuntimeError(e, false, true);
            } 
            finally {
                if (app != null) { 
                    context.ApplicationInstance = null; 
                    HttpApplicationFactory.RecycleApplicationInstance(app);
                } 
            }
        }

        // 
        // Computer name
        // 
 
        private static object _machineNameLock = new object();
        private static string _machineName; 
        private const int _maxMachineNameLength = 256;


        /// <devdoc> 
        ///    <para>
        ///       Gets 
        ///       the server machine name. 
        ///    </para>
        /// </devdoc> 
        public string MachineName {
            [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Medium)]
            get {
                return GetMachineNameInternal(); 
            }
        } 
 
        internal static string GetMachineNameInternal()
        { 
            if (_machineName != null)
                return _machineName;
            lock (_machineNameLock)
            { 
                if (_machineName != null)
                    return _machineName; 
 
                StringBuilder   buf = new StringBuilder (_maxMachineNameLength);
                int             len = _maxMachineNameLength; 

                if (UnsafeNativeMethods.GetComputerName (buf, ref len) == 0)
                    throw new HttpException (SR.GetString (SR.Get_computer_name_failed));
 
                _machineName = buf.ToString();
            } 
            return _machineName; 
        }
 
        //
        // Request Timeout
        //
 

        /// <devdoc> 
        ///    <para> 
        ///       Request timeout in seconds
        ///    </para> 
        /// </devdoc>
        public int ScriptTimeout {
            get {
                if (_context != null) { 
                    return Convert.ToInt32(_context.Timeout.TotalSeconds, CultureInfo.InvariantCulture);
                } 
                else { 
                    return HttpRuntimeSection.DefaultExecutionTimeout;
                } 
            }

            [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Medium)]
            set { 
                if (_context == null)
                    throw new HttpException(SR.GetString(SR.Server_not_available)); 
                if (value <= 0) 
                    throw new ArgumentOutOfRangeException("value");
                _context.Timeout = new TimeSpan(0, 0, value); 
            }
        }

        // 
        // Encoding / Decoding -- wrappers for HttpUtility
        // 
 

        /// <devdoc> 
        ///    <para>
        ///       HTML
        ///       decodes a given string and
        ///       returns the decoded string. 
        ///    </para>
        /// </devdoc> 
        public string HtmlDecode(string s) { 
            return HttpUtility.HtmlDecode(s);
        } 


        /// <devdoc>
        ///    <para> 
        ///       HTML
        ///       decode a string and send the result to a TextWriter output 
        ///       stream. 
        ///    </para>
        /// </devdoc> 
        public void HtmlDecode(string s, TextWriter output) {
            HttpUtility.HtmlDecode(s, output);
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       HTML
        ///       encodes a given string and 
        ///       returns the encoded string.
        ///    </para>
        /// </devdoc>
        public string HtmlEncode(string s) { 
            return HttpUtility.HtmlEncode(s);
        } 
 

        /// <devdoc> 
        ///    <para>
        ///       HTML
        ///       encodes
        ///       a string and returns the output to a TextWriter stream of output. 
        ///    </para>
        /// </devdoc> 
        public void HtmlEncode(string s, TextWriter output) { 
            HttpUtility.HtmlEncode(s, output);
        } 


        /// <devdoc>
        ///    <para> 
        ///       URL
        ///       encodes a given 
        ///       string and returns the encoded string. 
        ///    </para>
        /// </devdoc> 
        public string UrlEncode(string s) {
            Encoding e = (_context != null) ? _context.Response.ContentEncoding : Encoding.UTF8;
            return HttpUtility.UrlEncode(s, e);
        } 

 
        /// <devdoc> 
        ///    <para>
        ///       URL encodes a path portion of a URL string and returns the encoded string. 
        ///    </para>
        /// </devdoc>
        public string UrlPathEncode(string s) {
            return HttpUtility.UrlPathEncode(s); 
        }
 
 
        /// <devdoc>
        ///    <para> 
        ///       URL
        ///       encodes
        ///       a string and returns the output to a TextWriter output stream.
        ///    </para> 
        /// </devdoc>
        public void UrlEncode(string s, TextWriter output) { 
            if (s != null) 
                output.Write(UrlEncode(s));
        } 


        /// <devdoc>
        ///    <para> 
        ///       URL decodes a string and returns the output in a string.
        ///    </para> 
        /// </devdoc> 
        public string UrlDecode(string s) {
            Encoding e = (_context != null) ? _context.Request.ContentEncoding : Encoding.UTF8; 
            return HttpUtility.UrlDecode(s, e);
        }

 
        /// <devdoc>
        ///    <para> 
        ///       URL decodes a string and returns the output as a TextWriter output 
        ///       stream.
        ///    </para> 
        /// </devdoc>
        public void UrlDecode(string s, TextWriter output) {
            if (s != null)
                output.Write(UrlDecode(s)); 
        }
 
        ///////////////////////////////////////////////////////////////////////////// 
        /////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////// 

        static public string UrlTokenEncode(byte [] input)
        {
            if (input == null) 
                throw new ArgumentNullException("input");
            if (input.Length < 1) 
                return String.Empty; 

            string  base64Str   = null; 
            int     endPos      = 0;
            char[]  base64Chars = null;

            //////////////////////////////////////////////////////// 
            // Step 1: Do a Base64 encoding
            base64Str = Convert.ToBase64String(input); 
            if (base64Str == null) 
                return null;
 
            ////////////////////////////////////////////////////////
            // Step 2: Find how many padding chars are present in the end
            for (endPos = base64Str.Length; endPos > 0; endPos--)
            { 
                if (base64Str[endPos - 1] != '=') // Found a non-padding char!
                { 
                    break; // Stop here 
                }
            } 

            ////////////////////////////////////////////////////////
            // Step 3: Create char array to store all non-padding chars,
            //      plus a char to indicate how many padding chars are needed 
            base64Chars = new char[endPos + 1];
            base64Chars[endPos] = (char)((int)'0' + base64Str.Length - endPos); // Store a char at the end, to indicate how many padding chars are needed 
 
            ////////////////////////////////////////////////////////
            // Step 3: Copy in the other chars. Transform the "+" to "-", and "/" to "_" 
            for (int iter = 0; iter < endPos; iter++)
            {
                char c = base64Str[iter];
 
                switch (c)
                { 
                    case '+': 
                        base64Chars[iter] = '-';
                        break; 

                    case '/':
                        base64Chars[iter] = '_';
                        break; 

                    case '=': 
                        Debug.Assert (false); 
                        base64Chars[iter] = c;
                        break; 

                    default:
                        base64Chars[iter] = c;
                        break; 
                }
            } 
            return new string(base64Chars); 
        }
 
        /////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////
 
        static public byte [] UrlTokenDecode(string input) {
            if (input == null) 
                throw new ArgumentNullException("input"); 

            int len = input.Length; 
            if (len < 1)
                return new byte[0];

            /////////////////////////////////////////////////////////////////// 
            // Step 1: Calculate the number of padding chars to append to this string.
            //         The number of padding chars to append is stored in the last char of the string. 
            int numPadChars = (int)input[len - 1] - (int)'0'; 
            if (numPadChars < 0 || numPadChars > 10)
                return null; 


            ///////////////////////////////////////////////////////////////////
            // Step 2: Create array to store the chars (not including the last char) 
            //          and the padding chars
            char[] base64Chars = new char[len - 1 + numPadChars]; 
 

            //////////////////////////////////////////////////////// 
            // Step 3: Copy in the chars. Transform the "-" to "+", and "*" to "/"
            for (int iter = 0; iter < len - 1; iter++)
            {
                char c = input[iter]; 

                switch (c) 
                { 
                    case '-':
                        base64Chars[iter] = '+'; 
                        break;

                    case '_':
                        base64Chars[iter] = '/'; 
                        break;
 
                    default: 
                        base64Chars[iter] = c;
                        break; 
                }
            }

            //////////////////////////////////////////////////////// 
            // Step 4: Add padding chars
            for (int iter = len - 1; iter < base64Chars.Length; iter++) 
            { 
                base64Chars[iter] = '=';
            } 

            // Do the actual conversion
            return Convert.FromBase64CharArray (base64Chars, 0, base64Chars.Length);
        } 
    }
 
 
    /// <devdoc>
    /// </devdoc> 

    // VSWhidbey 473228 - removed link demand from HttpUtility for ClickOnce scenario
    public sealed class HttpUtility {
 
        public HttpUtility () {}
 		 
		////////////////////////////////////////////////////////////////////////// 
        //
        //  HTML Encoding / Decoding 
        //


        /// <devdoc> 
        ///    <para>
        ///       HTML decodes a string and returns the decoded string. 
        ///    </para> 
        /// </devdoc>
        public static string HtmlDecode(string s) { 
            if (s == null)
                return null;

            // See if this string needs to be decoded at all.  If no 
            // ampersands are found, then no special HTML-encoded chars
            // are in the string. 
            if (s.IndexOf('&') < 0) 
                return s;
 
            StringBuilder builder = new StringBuilder();
            StringWriter writer = new StringWriter(builder);

            HtmlDecode(s, writer); 

            return builder.ToString(); 
        } 

 
        /// <devdoc>
        ///    <para>
        ///       HTML decode a string and send the result to a TextWriter output stream.
        ///    </para> 
        /// </devdoc>
        private static char[] s_entityEndingChars = new char[] {';', '&'}; 
        public static void HtmlDecode(string s, TextWriter output) { 
            if (s == null)
                return; 

            if (s.IndexOf('&') < 0) {
                output.Write(s);        // good as is
                return; 
            }
 
            int l = s.Length; 
            for (int i = 0; i < l; i++) {
                char ch = s[i]; 

                if (ch == '&') {
                    // We found a '&'. Now look for the next ';' or '&'. The idea is that
                    // if we find another '&' before finding a ';', then this is not an entity, 
                    // and the next '&' might start a real entity (VSWhidbey 275184)
                    int index = s.IndexOfAny(s_entityEndingChars, i + 1); 
                    if (index > 0 && s[index] == ';') { 
                        string entity = s.Substring(i + 1, index - i - 1);
 
                        if (entity.Length > 1 && entity[0] == '#') {
                            try {
                                // The # syntax can be in decimal or hex, e.g.
                                //      &#229;  --> decimal 
                                //      &#xE5;  --> same char in hex
                                // See http://www.w3.org/TR/REC-html40/charset.html#entities 
                                if (entity[1] == 'x' || entity[1] == 'X') 
                                    ch = (char) Int32.Parse(entity.Substring(2), NumberStyles.AllowHexSpecifier);
                                else 
                                    ch = (char) Int32.Parse(entity.Substring(1));
                                i = index; // already looked at everything until semicolon
                            }
                            catch (System.FormatException) { 
                                i++;    //if the number isn't valid, ignore it
                            } 
                            catch (System.ArgumentException) { 
                                i++;    // if there is no number, ignore it.
                            } 
                        }
                        else {
                            i = index; // already looked at everything until semicolon
 
                            char entityChar = HtmlEntities.Lookup(entity);
                            if (entityChar != (char)0) { 
                                ch = entityChar; 
                            }
                            else { 
                                output.Write('&');
                                output.Write(entity);
                                output.Write(';');
                                continue; 
                            }
                        } 
 
                    }
                } 

                output.Write(ch);
            }
        } 

 
        /// <devdoc> 
        ///    <para>
        ///       HTML encodes a string and returns the encoded string. 
        ///    </para>
        /// </devdoc>
        public static String HtmlEncode(String s) {
            if (s == null) 
                return null;
 
            int pos = IndexOfHtmlEncodingChars(s, 0); 
            if (pos == -1)
                return s; 

            StringBuilder builder = new StringBuilder(s.Length + 5);
            int cch = s.Length;
            int startPos = 0; 
            for (;;) {
                if (pos > startPos) { 
                    builder.Append(s, startPos, pos - startPos); 
                }
 
                char ch = s[pos];
                if (ch <= '>') {
                    switch (ch) {
                        case '<': 
                            builder.Append("&lt;");
                            break; 
                        case '>': 
                            builder.Append("&gt;");
                            break; 
                        case '"':
                            builder.Append("&quot;");
                            break;
                        case '&': 
                            builder.Append("&amp;");
                            break; 
                        default: 
                            Debug.Assert(false, "Unexpected value for ch, ch=" + ch);
                            break; 
                    }
                }
                else {
#if ENTITY_ENCODE_HIGH_ASCII_CHARS 
                    Debug.Assert(ch >= 160 && ch < 256, "ch >= 160 && ch < 256");
 
                    // The seemingly arbitrary 160 comes from RFC 
                    builder.Append("&#");
                    builder.Append(((int)ch).ToString(NumberFormatInfo.InvariantInfo)); 
                    builder.Append(';');
#else
                    Debug.Assert(false);
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS 
                }
 
                startPos = pos + 1; 
                if (startPos >= cch)
                    break; 

                pos = IndexOfHtmlEncodingChars(s, startPos);
                if (pos == -1) {
                    builder.Append(s, startPos, cch - startPos); 
                    break;
                } 
            } 

            return builder.ToString(); 
        }


        /// <devdoc> 
        ///    <para>
        ///       HTML encodes a string and returns the output to a TextWriter stream of 
        ///       output. 
        ///    </para>
        /// </devdoc> 
        public unsafe static void HtmlEncode(String s, TextWriter output) {
            if (s == null)
                return;
 
            int index = IndexOfHtmlEncodingChars(s, 0);
            if (index == -1) { 
                output.Write(s); 
                return;
            } 

            Debug.Assert(0 <= index && index <= s.Length, "0 <= index && index <= s.Length");

            int cch = s.Length - index; 
            fixed (char * str = s) {
                char *pch = str; 
                while (index-- > 0) { 
                    output.Write(*pch++);
                } 

                while (cch-- > 0) {
                    char ch = *pch++;
                    if (ch <= '>') { 
                        switch (ch) {
                            case '<': 
                                output.Write("&lt;"); 
                                break;
                            case '>': 
                                output.Write("&gt;");
                                break;
                            case '"':
                                output.Write("&quot;"); 
                                break;
                            case '&': 
                                output.Write("&amp;"); 
                                break;
                            default: 
                                output.Write(ch);
                                break;
                        }
                    } 
#if ENTITY_ENCODE_HIGH_ASCII_CHARS
                    else if (ch >= 160 && ch < 256) { 
                        // The seemingly arbitrary 160 comes from RFC 
                        output.Write("&#");
                        output.Write(((int)ch).ToString(NumberFormatInfo.InvariantInfo)); 
                        output.Write(';');
                    }
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS
                    else { 
                        output.Write(ch);
                    } 
                } 
            }
        } 

        static unsafe int IndexOfHtmlEncodingChars(string s, int startPos) {
            Debug.Assert(0 <= startPos && startPos <= s.Length, "0 <= startPos && startPos <= s.Length");
 
            int cch = s.Length - startPos;
            fixed (char * str = s) { 
                for (char *pch = &str[startPos]; cch > 0; pch++, cch--) { 
                    char ch = *pch;
                    if (ch <= '>') { 
                        switch (ch) {
                            case '<':
                            case '>':
                            case '"': 
                            case '&':
                                return s.Length - cch; 
                        } 
                    }
#if ENTITY_ENCODE_HIGH_ASCII_CHARS 
                    else if (ch >= 160 && ch < 256) {
                        return s.Length - cch;
                    }
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS 
                }
            } 
 
            return -1;
        } 


        /// <devdoc>
        ///    <para> 
        ///       Encodes a string to make it a valid HTML attribute and returns the encoded string.
        ///    </para> 
        /// </devdoc> 
        public static String HtmlAttributeEncode(String s) {
            if (s == null) 
                return null;

            int pos = IndexOfHtmlAttributeEncodingChars(s, 0);
            if (pos == -1) 
                return s;
 
            StringBuilder builder = new StringBuilder(s.Length + 5); 
            int cch = s.Length;
            int startPos = 0; 
            for (;;) {
                if (pos > startPos) {
                    builder.Append(s, startPos, pos - startPos);
                } 

                char ch = s[pos]; 
                switch (ch) { 
                    case '"':
                        builder.Append("&quot;"); 
                        break;
                    case '&':
                        builder.Append("&amp;");
                        break; 
                    case '<':
                        // Whidbey 32404: The character '<' is not valid in an XML attribute value. 
                        // (See the W3C XML rec). 
                        builder.Append("&lt;");
                        break; 

                    default:
                        Debug.Assert(false, "Unexpected value for ch, ch=" + ch);
                        break; 
                }
 
                startPos = pos + 1; 
                if (startPos >= cch)
                    break; 

                pos = IndexOfHtmlAttributeEncodingChars(s, startPos);
                if (pos == -1) {
                    builder.Append(s, startPos, cch - startPos); 
                    break;
                } 
            } 

            return builder.ToString(); 
        }

        internal static void HtmlAttributeEncodeInternal(String s, HttpWriter writer) {
            int pos = IndexOfHtmlAttributeEncodingChars(s, 0); 
            if (pos == -1) {
                writer.Write(s); 
                return; 
            }
 
            int cch = s.Length;
            int startPos = 0;
            for (;;) {
                if (pos > startPos) { 
                    writer.WriteString(s, startPos, pos - startPos);
                } 
 
                char ch = s[pos];
                switch (ch) { 
                    case '"':
                        writer.Write("&quot;");
                        break;
                    case '&': 
                        writer.Write("&amp;");
                        break; 
                    case '<': 
                        // Whidbey 32404: The character '<' is not valid in an XML attribute value.
                        // (See the W3C XML rec). 
                        writer.Write("&lt;");
                        break;
                }
 
                startPos = pos + 1;
                if (startPos >= cch) 
                    break; 

                pos = IndexOfHtmlAttributeEncodingChars(s, startPos); 
                if (pos == -1) {
                    writer.WriteString(s, startPos, cch - startPos);
                    break;
                } 
            }
        } 
 

 
        /// <devdoc>
        ///    <para>
        ///       Encodes a string to make it a valid HTML attribute and returns the output
        ///       to a TextWriter stream of 
        ///       output.
        ///    </para> 
        /// </devdoc> 
        public unsafe static void HtmlAttributeEncode(String s, TextWriter output) {
            if (s == null) 
                return;

            int index = IndexOfHtmlAttributeEncodingChars(s, 0);
            if (index == -1) { 
                output.Write(s);
            } 
            else { 
                int cch = s.Length - index;
                fixed (char * str = s) { 
                    char *pch = str;
                    while (index-- > 0) {
                        output.Write(*pch++);
                    } 

                    while (cch-- > 0) { 
                        char ch = *pch++; 
                        if (ch <= '<') {
                            switch (ch) { 
                                case '<':
                                    output.Write("&lt;");
                                    break;
                                case '"': 
                                    output.Write("&quot;");
                                    break; 
                                case '&': 
                                    output.Write("&amp;");
                                    break; 
                                default:
                                    output.Write(ch);
                                    break;
                            } 
                        }
                        else { 
                            output.Write(ch); 
                        }
                    } 
                }
            }
        }
 
        static unsafe int IndexOfHtmlAttributeEncodingChars(string s, int startPos) {
            Debug.Assert(0 <= startPos && startPos <= s.Length, "0 <= startPos && startPos <= s.Length"); 
            int cch = s.Length - startPos; 
            fixed (char * str = s) {
                for (char *pch = &str[startPos]; cch > 0; pch++, cch--) { 
                    char ch = *pch;
                    if (ch <= '<') {
                        switch (ch) {
                            case '<': 
                            case '"':
                            case '&': 
                                return s.Length - cch; 
                        }
                    } 
                }
            }

            return -1; 
        }
 
 
        internal static string FormatPlainTextSpacesAsHtml(string s) {
            if (s == null) { 
                return null;
            }

            StringBuilder builder = new StringBuilder(); 
            StringWriter writer = new StringWriter(builder);
 
            int cb = s.Length; 

            for (int i = 0; i < cb; i++) { 
                char ch = s[i];
                if(ch == ' ') {
                    writer.Write("&nbsp;");
                } 
                else {
                    writer.Write(ch); 
                } 
            }
            return builder.ToString(); 
        }

        internal static String FormatPlainTextAsHtml(String s) {
            if (s == null) 
                return null;
 
            StringBuilder builder = new StringBuilder(); 
            StringWriter writer = new StringWriter(builder);
 
            FormatPlainTextAsHtml(s, writer);

            return builder.ToString();
        } 

        internal static void FormatPlainTextAsHtml(String s, TextWriter output) { 
            if (s == null) 
                return;
 
            int cb = s.Length;

            char prevCh = '\0';
 
            for (int i=0; i<cb; i++) {
                char ch = s[i]; 
                switch (ch) { 
                    case '<':
                        output.Write("&lt;"); 
                        break;
                    case '>':
                        output.Write("&gt;");
                        break; 
                    case '"':
                        output.Write("&quot;"); 
                        break; 
                    case '&':
                        output.Write("&amp;"); 
                        break;
                    case ' ':
                        if (prevCh == ' ')
                            output.Write("&nbsp;"); 
                        else
                            output.Write(ch); 
                        break; 
                    case '\r':
                        // Ignore \r, only handle \n 
                        break;
                    case '\n':
                        output.Write("<br>");
                        break; 

                    // 
                    default: 
#if ENTITY_ENCODE_HIGH_ASCII_CHARS
                        // The seemingly arbitrary 160 comes from RFC 
                        if (ch >= 160 && ch < 256) {
                            output.Write("&#");
                            output.Write(((int)ch).ToString(NumberFormatInfo.InvariantInfo));
                            output.Write(';'); 
                            break;
                        } 
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS 

                        output.Write(ch); 
                        break;
                }

                prevCh = ch; 
            }
        } 
 

        ////////////////////////////////////////////////////////////////////////// 
        //
        //  ASII encode - everything all non-7-bit to '?'
        //
 
        /*internal static String AsciiEncode(String s) {
            if (s == null) 
                return null; 

            StringBuilder sb = new StringBuilder(s.Length); 

            for (int i = 0; i < s.Length; i++) {
                char ch = s[i];
                if (((ch & 0xff80) != 0) || (ch < ' ' && ch != '\r' && ch != '\n' && ch != '\t')) 
                    ch = '?';
                sb.Append(ch); 
            } 

            return sb.ToString(); 
        }*/


        // 
        //  Query string parsing support
        // 
 
        public static NameValueCollection ParseQueryString(string query) {
            return ParseQueryString(query, Encoding.UTF8); 
        }

        public static NameValueCollection ParseQueryString(string query, Encoding encoding) {
            if (query == null) { 
                throw new ArgumentNullException("query");
            } 
 
            if (encoding == null) {
                throw new ArgumentNullException("encoding"); 
            }

            if (query.Length > 0 && query[0] == '?') {
                query = query.Substring(1); 
            }
 
            return new HttpValueCollection(query, false, true, encoding); 
        }
 
        //////////////////////////////////////////////////////////////////////////
        //
        //  URL decoding / encoding
        // 
        //////////////////////////////////////////////////////////////////////////
 
        // 
        //  Public static methods
        // 


        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static string UrlEncode(string str) { 
            if (str == null) 
                return null;
            return UrlEncode(str, Encoding.UTF8); 
        }


        /// <devdoc> 
        ///    <para>
        ///       URL encodes a path portion of a URL string and returns the encoded string. 
        ///    </para> 
        /// </devdoc>
        public static string UrlPathEncode(string str) { 
            if (str == null)
                return null;

            // recurse in case there is a query string 
            int i = str.IndexOf('?');
            if (i >= 0) 
                return UrlPathEncode(str.Substring(0, i)) + str.Substring(i); 

            // encode DBCS characters and spaces only 
            return UrlEncodeSpaces(UrlEncodeNonAscii(str, Encoding.UTF8));
        }

        internal static string AspCompatUrlEncode(string s) { 
            s = UrlEncode(s);
            s = s.Replace("!", "%21"); 
            s = s.Replace("*", "%2A"); 
            s = s.Replace("(", "%28");
            s = s.Replace(")", "%29"); 
            s = s.Replace("-", "%2D");
            s = s.Replace(".", "%2E");
            s = s.Replace("_", "%5F");
            s = s.Replace("\\", "%5C"); 
            return s;
        } 
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string UrlEncode(string str, Encoding e) {
            if (str == null) 
                return null;
            return Encoding.ASCII.GetString(UrlEncodeToBytes(str, e)); 
        } 

        //  Helper to encode the non-ASCII url characters only 
        internal static String UrlEncodeNonAscii(string str, Encoding e) {
            if (String.IsNullOrEmpty(str))
                return str;
            if (e == null) 
                e = Encoding.UTF8;
            byte[] bytes = e.GetBytes(str); 
            bytes = UrlEncodeBytesToBytesInternalNonAscii(bytes, 0, bytes.Length, false); 
            return Encoding.ASCII.GetString(bytes);
        } 

        //  Helper to encode spaces only
        internal static String UrlEncodeSpaces(string str) {
            if (str != null && str.IndexOf(' ') >= 0) 
                str = str.Replace(" ", "%20");
            return str; 
        } 

 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string UrlEncode(byte[] bytes) { 
            if (bytes == null)
                return null; 
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes)); 
        }
 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static string UrlEncode(byte[] bytes, int offset, int count) {
            if (bytes == null) 
                return null; 
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, offset, count));
        } 


        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static byte[] UrlEncodeToBytes(string str) { 
            if (str == null) 
                return null;
            return UrlEncodeToBytes(str, Encoding.UTF8); 
        }


        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static byte[] UrlEncodeToBytes(string str, Encoding e) { 
            if (str == null)
                return null; 
            byte[] bytes = e.GetBytes(str);
            return UrlEncodeBytesToBytesInternal(bytes, 0, bytes.Length, false);
        }
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static byte[] UrlEncodeToBytes(byte[] bytes) { 
            if (bytes == null)
                return null;
            return UrlEncodeToBytes(bytes, 0, bytes.Length);
        } 

 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count) {
            if (bytes == null && count == 0)
                return null;
            if (bytes == null) { 
                throw new ArgumentNullException("bytes");
            } 
            if (offset < 0 || offset > bytes.Length) { 
                throw new ArgumentOutOfRangeException("offset");
            } 
            if (count < 0 || offset+count > bytes.Length) {
                throw new ArgumentOutOfRangeException("count");
            }
            return UrlEncodeBytesToBytesInternal(bytes, offset, count, true); 
        }
 
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static string UrlEncodeUnicode(string str) {
            if (str == null)
                return null; 
            return UrlEncodeUnicodeStringToStringInternal(str, false);
        } 
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static byte[] UrlEncodeUnicodeToBytes(string str) {
            if (str == null) 
                return null;
            return Encoding.ASCII.GetBytes(UrlEncodeUnicode(str)); 
        } 

 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string UrlDecode(string str) { 
            if (str == null)
                return null; 
            return UrlDecode(str, Encoding.UTF8); 
        }
 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static string UrlDecode(string str, Encoding e) {
            if (str == null) 
                return null; 
            return UrlDecodeStringFromStringInternal(str, e);
        } 


        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static string UrlDecode(byte[] bytes, Encoding e) { 
            if (bytes == null) 
                return null;
            return UrlDecode(bytes, 0, bytes.Length, e); 
        }


        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static string UrlDecode(byte[] bytes, int offset, int count, Encoding e) { 
            if (bytes == null && count == 0)
                return null; 
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }
            if (offset < 0 || offset > bytes.Length) { 
                throw new ArgumentOutOfRangeException("offset");
            } 
            if (count < 0 || offset+count > bytes.Length) { 
                throw new ArgumentOutOfRangeException("count");
            } 
            return UrlDecodeStringFromBytesInternal(bytes, offset, count, e);
        }

 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public static byte[] UrlDecodeToBytes(string str) {
            if (str == null) 
                return null;
            return UrlDecodeToBytes(str, Encoding.UTF8);
        }
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static byte[] UrlDecodeToBytes(string str, Encoding e) { 
            if (str == null)
                return null;
            return UrlDecodeToBytes(e.GetBytes(str));
        } 

 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static byte[] UrlDecodeToBytes(byte[] bytes) {
            if (bytes == null)
                return null;
            return UrlDecodeToBytes(bytes, 0, (bytes != null) ? bytes.Length : 0); 
        }
 
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count) {
            if (bytes == null && count == 0)
                return null; 
            if (bytes == null) {
                throw new ArgumentNullException("bytes"); 
            } 
            if (offset < 0 || offset > bytes.Length) {
                throw new ArgumentOutOfRangeException("offset"); 
            }
            if (count < 0 || offset+count > bytes.Length) {
                throw new ArgumentOutOfRangeException("count");
            } 
            return UrlDecodeBytesFromBytesInternal(bytes, offset, count);
        } 
 
        //
        //  Implementation for encoding 
        //

        private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue) {
            int cSpaces = 0; 
            int cUnsafe = 0;
 
            // count them first 
            for (int i = 0; i < count; i++) {
                char ch = (char)bytes[offset+i]; 

                if (ch == ' ')
                    cSpaces++;
                else if (!IsSafe(ch)) 
                    cUnsafe++;
            } 
 
            // nothing to expand?
            if (!alwaysCreateReturnValue && cSpaces == 0 && cUnsafe == 0) 
                return bytes;

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cUnsafe*2]; 
            int pos = 0;
 
            for (int i = 0; i < count; i++) { 
                byte b = bytes[offset+i];
                char ch = (char)b; 

                if (IsSafe(ch)) {
                    expandedBytes[pos++] = b;
                } 
                else if (ch == ' ') {
                    expandedBytes[pos++] = (byte)'+'; 
                } 
                else {
                    expandedBytes[pos++] = (byte)'%'; 
                    expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte)IntToHex(b & 0x0f);
                }
            } 

            return expandedBytes; 
        } 

 
        private static bool IsNonAsciiByte(byte b) {
            return ( b >= 0x7F || b < 0x20 );
        }
 

        private static byte[] UrlEncodeBytesToBytesInternalNonAscii(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue) { 
            int cNonAscii = 0; 

            // count them first 
            for (int i = 0; i < count; i++) {
                if ( IsNonAsciiByte(bytes[offset+i]) )
                    cNonAscii++;
            } 

            // nothing to expand? 
            if (!alwaysCreateReturnValue && cNonAscii == 0) 
                return bytes;
 
            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cNonAscii*2];
            int pos = 0;
 
            for (int i = 0; i < count; i++)
            { 
                byte b = bytes[offset+i]; 

                if (IsNonAsciiByte(b)) { 
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte)IntToHex(b & 0x0f);
                } 
                else {
                    expandedBytes[pos++] = b; 
                } 
            }
 
            return expandedBytes;
        }

 
        private static string UrlEncodeUnicodeStringToStringInternal(string s, bool ignoreAscii) {
            int l = s.Length; 
            StringBuilder sb = new StringBuilder(l); 

            for (int i = 0; i < l; i++) { 
                char ch = s[i];

                if ((ch & 0xff80) == 0) {  // 7 bit?
                    if (ignoreAscii || IsSafe(ch)) { 
                        sb.Append(ch);
                    } 
                    else if (ch == ' ') { 
                        sb.Append('+');
                    } 
                    else {
                        sb.Append('%');
                        sb.Append(IntToHex((ch >>  4) & 0xf));
                        sb.Append(IntToHex((ch      ) & 0xf)); 
                    }
                } 
                else { // arbitrary Unicode? 
                    sb.Append("%u");
                    sb.Append(IntToHex((ch >> 12) & 0xf)); 
                    sb.Append(IntToHex((ch >>  8) & 0xf));
                    sb.Append(IntToHex((ch >>  4) & 0xf));
                    sb.Append(IntToHex((ch      ) & 0xf));
                } 
            }
 
            return sb.ToString(); 
        }
 
        //
        //  Implementation for decoding
        //
 
        // Internal class to facilitate URL decoding -- keeps char buffer and byte buffer, allows appending of either chars or bytes
        private class UrlDecoder { 
            private int _bufferSize; 

            // Accumulate characters in a special array 
            private int _numChars;
            private char[] _charBuffer;

            // Accumulate bytes for decoding into characters in a special array 
            private int _numBytes;
            private byte[] _byteBuffer; 
 
            // Encoding to convert chars to bytes
            private Encoding _encoding; 

            private void FlushBytes() {
                if (_numBytes > 0) {
                    _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars); 
                    _numBytes = 0;
                } 
            } 

            internal UrlDecoder(int bufferSize, Encoding encoding) { 
                _bufferSize = bufferSize;
                _encoding = encoding;

                _charBuffer = new char[bufferSize]; 
                // byte buffer created on demand
            } 
 
            internal void AddChar(char ch) {
                if (_numBytes > 0) 
                    FlushBytes();

                _charBuffer[_numChars++] = ch;
            } 

            internal void AddByte(byte b) { 
                // if there are no pending bytes treat 7 bit bytes as characters 
                // this optimization is temp disable as it doesn't work for some encodings
/* 
                if (_numBytes == 0 && ((b & 0x80) == 0)) {
                    AddChar((char)b);
                }
                else 
*/
                { 
                    if (_byteBuffer == null) 
                        _byteBuffer = new byte[_bufferSize];
 
                    _byteBuffer[_numBytes++] = b;
                }
            }
 
            internal String GetString() {
                if (_numBytes > 0) 
                    FlushBytes(); 

                if (_numChars > 0) 
                    return new String(_charBuffer, 0, _numChars);
                else
                    return String.Empty;
            } 
        }
 
        internal static string CollapsePercentUFromStringInternal(string s, Encoding e) { 
            int count = s.Length;
            UrlDecoder helper = new UrlDecoder(count, e); 

            // go thorugh the string's chars collapsing just %uXXXX and
            // appending each char as char
            int loc = s.IndexOf("%u", StringComparison.Ordinal); 
            if(loc == -1) {
                return s; 
            } 

            for (int pos = 0; pos < count; pos++) { 
                char ch = s[pos];

                if (ch == '%' && pos < count-5) {
                    if(s[pos+1] == 'u') { 
                        int h1 = HexToInt(s[pos+2]);
                        int h2 = HexToInt(s[pos+3]); 
                        int h3 = HexToInt(s[pos+4]); 
                        int h4 = HexToInt(s[pos+5]);
 
                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0) { //valid 4 hex chars
                            ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            pos += 5;
 
                            // add as char
                            helper.AddChar(ch); 
                            continue; 
                        }
                    } 
                }
                if ((ch & 0xFF80) == 0)
                    helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                else 
                    helper.AddChar(ch);
            } 
            return helper.GetString(); 
        }
 
        private static string UrlDecodeStringFromStringInternal(string s, Encoding e) {
            int count = s.Length;
            UrlDecoder helper = new UrlDecoder(count, e);
 
            // go through the string's chars collapsing %XX and %uXXXX and
            // appending each char as char, with exception of %XX constructs 
            // that are appended as bytes 

            for (int pos = 0; pos < count; pos++) { 
                char ch = s[pos];

                if (ch == '+') {
                    ch = ' '; 
                }
                else if (ch == '%' && pos < count-2) { 
                    if (s[pos+1] == 'u' && pos < count-5) { 
                        int h1 = HexToInt(s[pos+2]);
                        int h2 = HexToInt(s[pos+3]); 
                        int h3 = HexToInt(s[pos+4]);
                        int h4 = HexToInt(s[pos+5]);

                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0) {   // valid 4 hex chars 
                            ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            pos += 5; 
 
                            // only add as char
                            helper.AddChar(ch); 
                            continue;
                        }
                    }
                    else { 
                        int h1 = HexToInt(s[pos+1]);
                        int h2 = HexToInt(s[pos+2]); 
 
                        if (h1 >= 0 && h2 >= 0) {     // valid 2 hex chars
                            byte b = (byte)((h1 << 4) | h2); 
                            pos += 2;

                            // don't add as char
                            helper.AddByte(b); 
                            continue;
                        } 
                    } 
                }
 
                if ((ch & 0xFF80) == 0)
                    helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                else
                    helper.AddChar(ch); 
            }
 
            return helper.GetString(); 
        }
 
        private static string UrlDecodeStringFromBytesInternal(byte[] buf, int offset, int count, Encoding e) {
            UrlDecoder helper = new UrlDecoder(count, e);

            // go through the bytes collapsing %XX and %uXXXX and appending 
            // each byte as byte, with exception of %uXXXX constructs that
            // are appended as chars 
 
            for (int i = 0; i < count; i++) {
                int pos = offset + i; 
                byte b = buf[pos];

                // The code assumes that + and % cannot be in multibyte sequence
 
                if (b == '+') {
                    b = (byte)' '; 
                } 
                else if (b == '%' && i < count-2) {
                    if (buf[pos+1] == 'u' && i < count-5) { 
                        int h1 = HexToInt((char)buf[pos+2]);
                        int h2 = HexToInt((char)buf[pos+3]);
                        int h3 = HexToInt((char)buf[pos+4]);
                        int h4 = HexToInt((char)buf[pos+5]); 

                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0) {   // valid 4 hex chars 
                            char ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4); 
                            i += 5;
 
                            // don't add as byte
                            helper.AddChar(ch);
                            continue;
                        } 
                    }
                    else { 
                        int h1 = HexToInt((char)buf[pos+1]); 
                        int h2 = HexToInt((char)buf[pos+2]);
 
                        if (h1 >= 0 && h2 >= 0) {     // valid 2 hex chars
                            b = (byte)((h1 << 4) | h2);
                            i += 2;
                        } 
                    }
                } 
 
                helper.AddByte(b);
            } 

            return helper.GetString();
        }
 
        private static byte[] UrlDecodeBytesFromBytesInternal(byte[] buf, int offset, int count) {
            int decodedBytesCount = 0; 
            byte[] decodedBytes = new byte[count]; 

            for (int i = 0; i < count; i++) { 
                int pos = offset + i;
                byte b = buf[pos];

                if (b == '+') { 
                    b = (byte)' ';
                } 
                else if (b == '%' && i < count-2) { 
                    int h1 = HexToInt((char)buf[pos+1]);
                    int h2 = HexToInt((char)buf[pos+2]); 

                    if (h1 >= 0 && h2 >= 0) {     // valid 2 hex chars
                        b = (byte)((h1 << 4) | h2);
                        i += 2; 
                    }
                } 
 
                decodedBytes[decodedBytesCount++] = b;
            } 

            if (decodedBytesCount < decodedBytes.Length) {
                byte[] newDecodedBytes = new byte[decodedBytesCount];
                Array.Copy(decodedBytes, newDecodedBytes, decodedBytesCount); 
                decodedBytes = newDecodedBytes;
            } 
 
            return decodedBytes;
        } 

        //
        // Private helpers for URL encoding/decoding
        // 

        private static int HexToInt(char h) { 
            return( h >= '0' && h <= '9' ) ? h - '0' : 
            ( h >= 'a' && h <= 'f' ) ? h - 'a' + 10 :
            ( h >= 'A' && h <= 'F' ) ? h - 'A' + 10 : 
            -1;
        }

        internal static char IntToHex(int n) { 
            Debug.Assert(n < 0x10);
 
            if (n <= 9) 
                return(char)(n + (int)'0');
            else 
                return(char)(n - 10 + (int)'a');
        }

        // Set of safe chars, from RFC 1738.4 minus '+' 
        internal static bool IsSafe(char ch) {
            if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9') 
                return true; 

            switch (ch) { 
                case '-':
                case '_':
                case '.':
                case '!': 
                case '*':
                case '\'': 
                case '(': 
                case ')':
                    return true; 
            }

            return false;
        } 

        ////////////////////////////////////////////////////////////////////////// 
        // 
        //  Misc helpers
        // 
        //////////////////////////////////////////////////////////////////////////

        internal static String FormatHttpDateTime(DateTime dt) {
            if (dt < DateTime.MaxValue.AddDays(-1) && dt > DateTime.MinValue.AddDays(1)) 
                dt = dt.ToUniversalTime();
            return dt.ToString("R", DateTimeFormatInfo.InvariantInfo); 
        } 

        internal static String FormatHttpDateTimeUtc(DateTime dt) { 
            return dt.ToString("R", DateTimeFormatInfo.InvariantInfo);
        }

        internal static String FormatHttpCookieDateTime(DateTime dt) { 
            if (dt < DateTime.MaxValue.AddDays(-1) && dt > DateTime.MinValue.AddDays(1))
                dt = dt.ToUniversalTime(); 
            return dt.ToString("ddd, dd-MMM-yyyy HH':'mm':'ss 'GMT'", DateTimeFormatInfo.InvariantInfo); 
        }
    } 

    // helper class for lookup of HTML encoding entities
    internal class HtmlEntities {
        private static object _lookupLockObject = new object(); 

        // The list is from http://www.w3.org/TR/REC-html40/sgml/entities.html 
        private static String[] _entitiesList = new String[] { 
            "\x0022-quot",
            "\x0026-amp", 
            "\x003c-lt",
            "\x003e-gt",
            "\x00a0-nbsp",
            "\x00a1-iexcl", 
            "\x00a2-cent",
            "\x00a3-pound", 
            "\x00a4-curren", 
            "\x00a5-yen",
            "\x00a6-brvbar", 
            "\x00a7-sect",
            "\x00a8-uml",
            "\x00a9-copy",
            "\x00aa-ordf", 
            "\x00ab-laquo",
            "\x00ac-not", 
            "\x00ad-shy", 
            "\x00ae-reg",
            "\x00af-macr", 
            "\x00b0-deg",
            "\x00b1-plusmn",
            "\x00b2-sup2",
            "\x00b3-sup3", 
            "\x00b4-acute",
            "\x00b5-micro", 
            "\x00b6-para", 
            "\x00b7-middot",
            "\x00b8-cedil", 
            "\x00b9-sup1",
            "\x00ba-ordm",
            "\x00bb-raquo",
            "\x00bc-frac14", 
            "\x00bd-frac12",
            "\x00be-frac34", 
            "\x00bf-iquest", 
            "\x00c0-Agrave",
            "\x00c1-Aacute", 
            "\x00c2-Acirc",
            "\x00c3-Atilde",
            "\x00c4-Auml",
            "\x00c5-Aring", 
            "\x00c6-AElig",
            "\x00c7-Ccedil", 
            "\x00c8-Egrave", 
            "\x00c9-Eacute",
            "\x00ca-Ecirc", 
            "\x00cb-Euml",
            "\x00cc-Igrave",
            "\x00cd-Iacute",
            "\x00ce-Icirc", 
            "\x00cf-Iuml",
            "\x00d0-ETH", 
            "\x00d1-Ntilde", 
            "\x00d2-Ograve",
            "\x00d3-Oacute", 
            "\x00d4-Ocirc",
            "\x00d5-Otilde",
            "\x00d6-Ouml",
            "\x00d7-times", 
            "\x00d8-Oslash",
            "\x00d9-Ugrave", 
            "\x00da-Uacute", 
            "\x00db-Ucirc",
            "\x00dc-Uuml", 
            "\x00dd-Yacute",
            "\x00de-THORN",
            "\x00df-szlig",
            "\x00e0-agrave", 
            "\x00e1-aacute",
            "\x00e2-acirc", 
            "\x00e3-atilde", 
            "\x00e4-auml",
            "\x00e5-aring", 
            "\x00e6-aelig",
            "\x00e7-ccedil",
            "\x00e8-egrave",
            "\x00e9-eacute", 
            "\x00ea-ecirc",
            "\x00eb-euml", 
            "\x00ec-igrave", 
            "\x00ed-iacute",
            "\x00ee-icirc", 
            "\x00ef-iuml",
            "\x00f0-eth",
            "\x00f1-ntilde",
            "\x00f2-ograve", 
            "\x00f3-oacute",
            "\x00f4-ocirc", 
            "\x00f5-otilde", 
            "\x00f6-ouml",
            "\x00f7-divide", 
            "\x00f8-oslash",
            "\x00f9-ugrave",
            "\x00fa-uacute",
            "\x00fb-ucirc", 
            "\x00fc-uuml",
            "\x00fd-yacute", 
            "\x00fe-thorn", 
            "\x00ff-yuml",
            "\x0152-OElig", 
            "\x0153-oelig",
            "\x0160-Scaron",
            "\x0161-scaron",
            "\x0178-Yuml", 
            "\x0192-fnof",
            "\x02c6-circ", 
            "\x02dc-tilde", 
            "\x0391-Alpha",
            "\x0392-Beta", 
            "\x0393-Gamma",
            "\x0394-Delta",
            "\x0395-Epsilon",
            "\x0396-Zeta", 
            "\x0397-Eta",
            "\x0398-Theta", 
            "\x0399-Iota", 
            "\x039a-Kappa",
            "\x039b-Lambda", 
            "\x039c-Mu",
            "\x039d-Nu",
            "\x039e-Xi",
            "\x039f-Omicron", 
            "\x03a0-Pi",
            "\x03a1-Rho", 
            "\x03a3-Sigma", 
            "\x03a4-Tau",
            "\x03a5-Upsilon", 
            "\x03a6-Phi",
            "\x03a7-Chi",
            "\x03a8-Psi",
            "\x03a9-Omega", 
            "\x03b1-alpha",
            "\x03b2-beta", 
            "\x03b3-gamma", 
            "\x03b4-delta",
            "\x03b5-epsilon", 
            "\x03b6-zeta",
            "\x03b7-eta",
            "\x03b8-theta",
            "\x03b9-iota", 
            "\x03ba-kappa",
            "\x03bb-lambda", 
            "\x03bc-mu", 
            "\x03bd-nu",
            "\x03be-xi", 
            "\x03bf-omicron",
            "\x03c0-pi",
            "\x03c1-rho",
            "\x03c2-sigmaf", 
            "\x03c3-sigma",
            "\x03c4-tau", 
            "\x03c5-upsilon", 
            "\x03c6-phi",
            "\x03c7-chi", 
            "\x03c8-psi",
            "\x03c9-omega",
            "\x03d1-thetasym",
            "\x03d2-upsih", 
            "\x03d6-piv",
            "\x2002-ensp", 
            "\x2003-emsp", 
            "\x2009-thinsp",
            "\x200c-zwnj", 
            "\x200d-zwj",
            "\x200e-lrm",
            "\x200f-rlm",
            "\x2013-ndash", 
            "\x2014-mdash",
            "\x2018-lsquo", 
            "\x2019-rsquo", 
            "\x201a-sbquo",
            "\x201c-ldquo", 
            "\x201d-rdquo",
            "\x201e-bdquo",
            "\x2020-dagger",
            "\x2021-Dagger", 
            "\x2022-bull",
            "\x2026-hellip", 
            "\x2030-permil", 
            "\x2032-prime",
            "\x2033-Prime", 
            "\x2039-lsaquo",
            "\x203a-rsaquo",
            "\x203e-oline",
            "\x2044-frasl", 
            "\x20ac-euro",
            "\x2111-image", 
            "\x2118-weierp", 
            "\x211c-real",
            "\x2122-trade", 
            "\x2135-alefsym",
            "\x2190-larr",
            "\x2191-uarr",
            "\x2192-rarr", 
            "\x2193-darr",
            "\x2194-harr", 
            "\x21b5-crarr", 
            "\x21d0-lArr",
            "\x21d1-uArr", 
            "\x21d2-rArr",
            "\x21d3-dArr",
            "\x21d4-hArr",
            "\x2200-forall", 
            "\x2202-part",
            "\x2203-exist", 
            "\x2205-empty", 
            "\x2207-nabla",
            "\x2208-isin", 
            "\x2209-notin",
            "\x220b-ni",
            "\x220f-prod",
            "\x2211-sum", 
            "\x2212-minus",
            "\x2217-lowast", 
            "\x221a-radic", 
            "\x221d-prop",
            "\x221e-infin", 
            "\x2220-ang",
            "\x2227-and",
            "\x2228-or",
            "\x2229-cap", 
            "\x222a-cup",
            "\x222b-int", 
            "\x2234-there4", 
            "\x223c-sim",
            "\x2245-cong", 
            "\x2248-asymp",
            "\x2260-ne",
            "\x2261-equiv",
            "\x2264-le", 
            "\x2265-ge",
            "\x2282-sub", 
            "\x2283-sup", 
            "\x2284-nsub",
            "\x2286-sube", 
            "\x2287-supe",
            "\x2295-oplus",
            "\x2297-otimes",
            "\x22a5-perp", 
            "\x22c5-sdot",
            "\x2308-lceil", 
            "\x2309-rceil", 
            "\x230a-lfloor",
            "\x230b-rfloor", 
            "\x2329-lang",
            "\x232a-rang",
            "\x25ca-loz",
            "\x2660-spades", 
            "\x2663-clubs",
            "\x2665-hearts", 
            "\x2666-diams", 
        };
 
        private static Hashtable _entitiesLookupTable;

        private HtmlEntities() {
        } 

        internal /*public*/ static char Lookup(String entity) { 
            if (_entitiesLookupTable == null) { 
                // populate hashtable on demand
                lock (_lookupLockObject) { 
                    if (_entitiesLookupTable == null) {
                        Hashtable t = new Hashtable();

                        foreach (String s in _entitiesList) 
                            t[s.Substring(2)] = s[0];  // 1st char is the code, 2nd '-'
 
                        _entitiesLookupTable = t; 
                    }
                } 
            }

            Object obj = _entitiesLookupTable[entity];
 
            if (obj != null)
                return (char)obj; 
            else 
                return (char)0;
        } 
    }
}
