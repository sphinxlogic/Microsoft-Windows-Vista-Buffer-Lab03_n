//------------------------------------------------------------------------------ 
// <copyright file="IIS7Runtime.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * The ASP.NET/IIS 7 integrated pipeline runtime service host 
 *
 * Copyright (c) 2004 Microsoft Corporation 
 */

namespace System.Web.Hosting {
    using System.Collections; 
    using System.Globalization;
    using System.Reflection; 
    using System.Runtime.InteropServices; 
    using System.Security.Permissions;
    using System.Security.Principal; 
    using System.Text;
    using System.Threading;
    using System.Web.Util;
    using System.Web; 
    using System.Web.Management;
    using System.IO; 
 
    using IIS = UnsafeIISMethods;
 

    // this delegate is called from native code
    // each time a native-managed
    // transition is made to process a request state 
    delegate int ExecuteFunctionDelegate(
            IntPtr managedHttpContext, 
            IntPtr nativeRequestContext, 
            IntPtr moduleData,
            int flags); 

    delegate bool RoleFunctionDelegate(
            IntPtr pManagedPrincipal,
            IntPtr pszRole, 
            int cchRole,
            bool disposing); 
 
    // this delegate is called from native code when the request is complete
    // to free any managed resources associated with the request 
    delegate void DisposeFunctionDelegate( [In] IntPtr managedHttpContext );

    [ComImport, Guid("c96cb854-aec2-4208-9ada-a86a96860cb6"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPipelineRuntime { 
        void StartProcessing();
        void StopProcessing(); 
        void InitializeApplication([In] IntPtr appContext); 
        IntPtr GetExecuteDelegate();
        IntPtr GetDisposeDelegate(); 
        IntPtr GetRoleDelegate();
    }

    /// <include file='doc\ISAPIRuntime.uex' path='docs/doc[@for="ISAPIRuntime"]/*' /> 
    /// <devdoc>
    ///    <para>[To be supplied.]</para> 
    /// </devdoc> 
    /// <internalonly/>
    internal sealed class PipelineRuntime : MarshalByRefObject, IPipelineRuntime, IRegisteredObject { 

        // initialization error handling
        internal const string InitExceptionModuleName = "AspNetInitializationExceptionModule";
        private const string s_InitExceptionModulePrecondition = ""; 

        // to control removal from unmanaged table (to it only once) 
        private static int s_isThisAppDomainRemovedFromUnmanagedTable; 
        private static IntPtr s_ApplicationContext;
        private static string s_thisAppDomainsIsapiAppId; 

        // when GL_APPLICATION_STOP fires, this is set to true to indicate that we can unload the AppDomain
        private static bool s_StopProcessingCalled;
        private static bool s_InitializationCompleted; 

        // keep rooted through the app domain lifetime 
        private static object _delegatelock = new object(); 

        private static int _inIndicateCompletionCount; 

        private static IntPtr _executeDelegatePointer = IntPtr.Zero;
        private static ExecuteFunctionDelegate _executeDelegate = null;
 
        private static IntPtr _disposeDelegatePointer = IntPtr.Zero;
        private static DisposeFunctionDelegate _disposeDelegate = null; 
 
        private static IntPtr _roleDelegatePointer = IntPtr.Zero;
        private static RoleFunctionDelegate _roleDelegate = null; 

        public IntPtr GetExecuteDelegate() {
            if (IntPtr.Zero == _executeDelegatePointer) {
                lock (_delegatelock) { 
                    if (IntPtr.Zero == _executeDelegatePointer) {
                        ExecuteFunctionDelegate d = new ExecuteFunctionDelegate(ProcessRequestNotification); 
                        if (null != d) { 
                            IntPtr p = Marshal.GetFunctionPointerForDelegate(d);
                            if (IntPtr.Zero != p) { 
                                Thread.MemoryBarrier();
                                _executeDelegate = d;
                                _executeDelegatePointer = p;
                            } 
                        }
                    } 
                } 
            }
 
            return _executeDelegatePointer;
        }

        public IntPtr GetDisposeDelegate() { 
            if (IntPtr.Zero == _disposeDelegatePointer) {
                lock (_delegatelock) { 
                    if (IntPtr.Zero == _disposeDelegatePointer) { 
                        DisposeFunctionDelegate d = new DisposeFunctionDelegate(DisposeHandler);
                        if (null != d) { 
                            IntPtr p = Marshal.GetFunctionPointerForDelegate(d);
                            if (IntPtr.Zero != p) {
                                Thread.MemoryBarrier();
                                _disposeDelegate = d; 
                                _disposeDelegatePointer = p;
                            } 
                        } 
                    }
                } 
            }

            return _disposeDelegatePointer;
        } 

        public IntPtr GetRoleDelegate() { 
            if (IntPtr.Zero == _roleDelegatePointer) { 
                lock (_delegatelock) {
                    if (IntPtr.Zero == _roleDelegatePointer) { 
                        RoleFunctionDelegate d = new RoleFunctionDelegate(RoleHandler);
                        if (null != d) {
                            IntPtr p = Marshal.GetFunctionPointerForDelegate(d);
                            if (IntPtr.Zero != p) { 
                                Thread.MemoryBarrier();
                                _roleDelegate = d; 
                                _roleDelegatePointer = p; 
                            }
                        } 
                    }
                }
            }
 
            return _roleDelegatePointer;
        } 
 

 
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)]
        [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Minimal)]
        public PipelineRuntime() {
            HostingEnvironment.RegisterObject(this); 
            Debug.Trace("PipelineDomain", "RegisterObject(this) called");
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        public override Object InitializeLifetimeService() { 
            return null; // never expire lease
        }

        public void StartProcessing() { 
            Debug.Trace("PipelineDomain", "StartProcessing AppId = " + s_thisAppDomainsIsapiAppId);
        } 
 
        public void StopProcessing() {
            Debug.Trace("PipelineDomain", "StopProcessing with stack = " + Environment.StackTrace 
                        + " for AppId= " +  s_thisAppDomainsIsapiAppId);

            if (IIS.MgdHasConfigChanged() && !HostingEnvironment.ShutdownInitiated) {
                HttpRuntime.SetShutdownReason(ApplicationShutdownReason.ConfigurationChange, "IIS configuration change"); 
            }
 
            s_StopProcessingCalled = true; 
            // inititate shutdown and
            // require the native callback for Stop 
            HostingEnvironment.InitiateShutdown();
        }

        internal static void WaitForRequestsToDrain() { 
            while (!s_StopProcessingCalled || _inIndicateCompletionCount > 0) {
                Thread.Sleep(250); 
            } 
        }
 
        private StringBuilder FormatExceptionMessage(Exception e, string[] strings) {
            StringBuilder sb = new StringBuilder(4096);

            if (null != strings) { 
                for (int i = 0; i < strings.Length; i++) {
                    sb.Append(strings[i]); 
                } 
            }
            for (Exception current = e; current != null; current = current.InnerException) { 
                if (current == e)
                    sb.Append("\r\n\r\nException: ");
                else
                    sb.Append("\r\n\r\nInnerException: "); 
                sb.Append(current.GetType().FullName);
                sb.Append("\r\nMessage: "); 
                sb.Append(current.Message); 
                sb.Append("\r\nStackTrace: ");
                sb.Append(current.StackTrace); 
            }

            return sb;
        } 

        public void InitializeApplication(IntPtr appContext) 
        { 
            s_ApplicationContext = appContext;
 
            HttpApplication app = null;

            try {
                HttpRuntime.UseIntegratedPipeline = true; 

                // if HttpRuntime.HostingInit failed, do not attempt to create the application (WOS #1653963) 
                if (!HttpRuntime.HostingInitFailed) { 
                    //
                    //  On IIS7, application initialization does not provide an http context.  Theoretically, 
                    //  no one should be using the context during application initialization, but people do.
                    //  Create a dummy context that is used during application initialization
                    //  to prevent breakage (ISAPI mode always provides a context)
                    // 
                    HttpWorkerRequest initWorkerRequest = new SimpleWorkerRequest("" /*page*/,
                                                                                  "" /*query*/, 
                                                                                  new StringWriter(CultureInfo.InvariantCulture)); 
                    HttpContext initHttpContext = new HttpContext(initWorkerRequest);
                    app = HttpApplicationFactory.GetPipelineApplicationInstance(appContext, initHttpContext); 
                }
            }
            catch(Exception e)
            { 
                if (HttpRuntime.InitializationException == null) {
                    HttpRuntime.InitializationException = e; 
                } 
            }
            finally { 
                s_InitializationCompleted = true;

                if (HttpRuntime.InitializationException != null) {
 
                    // at least one module must be registered so that we
                    // call ProcessRequestNotification later and send the formatted 
                    // InitializationException to the client. 
                    int hresult = UnsafeIISMethods.MgdRegisterEventSubscription(
                        appContext, 
                        InitExceptionModuleName,
                        RequestNotification.BeginRequest,
                        0 /*postRequestNotifications*/,
                        InitExceptionModuleName, 
                        s_InitExceptionModulePrecondition,
                        new IntPtr(-1), 
                        false /*useHighPriority*/); 

                    if (hresult < 0) { 
                        throw new COMException( SR.GetString(SR.Failed_Pipeline_Subscription, InitExceptionModuleName),
                                                hresult );
                    }
 
                    // Always register a managed handler:
                    // WOS 1990290: VS F5 Debugging: "AspNetInitializationExceptionModule" is registered for RQ_BEGIN_REQUEST, 
                    // but the DEBUG verb skips notifications until post RQ_AUTHENTICATE_REQUEST. 
                    hresult = UnsafeIISMethods.MgdRegisterEventSubscription(
                        appContext, 
                        HttpApplication.IMPLICIT_HANDLER,
                        RequestNotification.ExecuteRequestHandler /*requestNotifications*/,
                        0 /*postRequestNotifications*/,
                        String.Empty /*type*/, 
                        HttpApplication.MANAGED_PRECONDITION /*precondition*/,
                        new IntPtr(-1), 
                        false /*useHighPriority*/); 

                    if (hresult < 0) { 
                        throw new COMException( SR.GetString(SR.Failed_Pipeline_Subscription, HttpApplication.IMPLICIT_HANDLER),
                                                hresult );
                    }
                } 

                if (app != null) { 
                    HttpApplicationFactory.RecyclePipelineApplicationInstance(app); 
                }
            } 
        }

        private static HttpContext UnwrapContext(IntPtr contextPtr) {
            GCHandle h = GCHandle.FromIntPtr(contextPtr); 
            return (HttpContext) h.Target;
        } 
 
        internal bool HostingShutdownInitiated {
            get { 
                return HostingEnvironment.ShutdownInitiated;
            }
        }
 
        internal static bool RoleHandler(IntPtr pManagedPrincipal, IntPtr pszRole, int cchRole, bool disposing) {
            GCHandle h = GCHandle.FromIntPtr(pManagedPrincipal); 
            IPrincipal principal = (IPrincipal) h.Target; 
            if (principal != null) {
                if (disposing) { 
                    if (h.IsAllocated) {
                        h.Free();
                    }
                    WindowsIdentity id = principal.Identity as WindowsIdentity; 
                    if (id != null) {
                        id.Dispose(); 
                    } 
                    return false;
                } 

                return principal.IsInRole(StringUtil.StringFromWCharPtr(pszRole, cchRole));
            }
            return false; 
        }
 
        // called from native code when the IHttpContext is disposed 
        internal static void DisposeHandler(IntPtr managedHttpContext) {
            HttpContext context = UnwrapContext(managedHttpContext); 
            DisposeHandlerPrivate(context);
        }

        // called from managed code as a perf optimization to avoid calling back later 
        internal static void DisposeHandler(HttpContext context, IntPtr nativeRequestContext, RequestNotificationStatus status) {
            if (IIS.MgdCanDisposeManagedContext(nativeRequestContext, status)) { 
                DisposeHandlerPrivate(context); 
            }
        } 

        private static void DisposeHandlerPrivate(HttpContext context) {
            Debug.Trace("PipelineRuntime", "DisposeHandler");
            try { 
                context.FinishPipelineRequest();
 
                IIS7WorkerRequest wr = context.WorkerRequest as IIS7WorkerRequest; 
                if (wr != null) {
                    wr.Dispose(); 
                }

                PerfCounters.DecrementCounter(AppPerfCounter.REQUESTS_EXECUTING);
 
                // make sure that the principal is cleaned up to ensure
                // tokens get closed quickly 
                context.DisposePrincipal(); 
            }
            finally { 
                if(context != null) {
                    context.Unroot();
                }
                HttpRuntime.DecrementActivePipelineCount(); 
            }
        } 
 
        //
        // This is the managed entry point for processing request notifications. 
        // Although this method is wrapped in try/catch, it is not supposed to
        // cause an exception. If it does throw, the application, httpwriter, etc
        // may not be initialized, and it might not be possible to process the rest
        // of the request. I would prefer to let this method throw and crash the 
        // process, but for now we will consume the exception, report the error to
        // IIS, and continue. 
        // 
        // Code that might throw belongs in HttpRuntime::ProcessRequestNotificationPrivate.
        // 
        internal static int ProcessRequestNotification(
                IntPtr managedHttpContext,
                IntPtr nativeRequestContext,
                IntPtr moduleData, 
                int flags)
        { 
            try { 
                return ProcessRequestNotificationHelper(managedHttpContext, nativeRequestContext, moduleData, flags);
            } 
            catch(Exception e) {
                ApplicationManager.RecordFatalException(e);
                throw;
            } 
        }
 
        internal static int ProcessRequestNotificationHelper( 
                IntPtr managedHttpContext,
                IntPtr nativeRequestContext, 
                IntPtr moduleData,
                int flags)
        {
            IIS7WorkerRequest wr = null; 
            HttpContext context = null;
            RequestNotificationStatus status = RequestNotificationStatus.Continue; 
 
            if (managedHttpContext == IntPtr.Zero) {
                wr = IIS7WorkerRequest.CreateWorkerRequest(nativeRequestContext, ((flags & HttpContext.FLAG_ETW_PROVIDER_ENABLED) == HttpContext.FLAG_ETW_PROVIDER_ENABLED)); 
                context = CreateContext(wr, nativeRequestContext);
                if (context == null) {
                    return (int)RequestNotificationStatus.FinishRequest;
                } 
                context.Root();
                IIS.MgdSetManagedHttpContext(nativeRequestContext, context.ContextPtr); 
 
                // Increment active request count as soon as possible to prevent
                // shutdown of the appdomain while requests are in flight.  It 
                // is decremented in DisposeHandler
                HttpRuntime.IncrementActivePipelineCount();
            }
            else { 
                context = UnwrapContext(managedHttpContext);
                wr = context.WorkerRequest as IIS7WorkerRequest; 
            } 

            // It is possible for a notification to complete asynchronously while we're in 
            // a call to IndicateCompletion, in which case a new IIS thread might enter before
            // the call to IndicateCompletion returns.  If this happens, block the thread until
            // IndicateCompletion returns.
            if (context.InIndicateCompletion && context.CurrentThread != Thread.CurrentThread) { 
                while (context.InIndicateCompletion) {
                    Thread.Sleep(10); 
                } 
            }
 
            // RQ_SEND_RESPONSE fires out of band and completes synchronously only.
            // The pipeline must be reentrant to support this, so the notification
            // context for the previous notification must be saved and restored.
            NotificationContext savedNotificationContext = context.NotificationContext; 
            try {
                context.NotificationContext = new NotificationContext(flags /*CurrentNotificationFlags*/, 
                                                                      savedNotificationContext != null /*IsReEntry*/); 
                status = HttpRuntime.ProcessRequestNotification(wr, context);
            } 
            finally {
                if (status != RequestNotificationStatus.Pending) {
                    // if we completed the notification, pop the notification context stack
                    // if this is an asynchronous unwind, then the completion will clear the context 
                    context.NotificationContext = savedNotificationContext;
                } 
            } 

            if (status != RequestNotificationStatus.Pending) { 
                // WOS 1785741: (Perf) In profiles, 8% of HelloWorld is transitioning from native to managed.
                // The fix is to keep managed code on the stack so that the AppDomain context remains on the
                // thread, and we can re-enter managed code without setting up the AppDomain context.
                // If this optimization is possible, MgdIndicateCompletion will execute one or more notifications 
                // and return PENDING as the status.
                HttpApplication.ThreadContext threadContext = context.IndicateCompletionContext; 
                if (!context.InIndicateCompletion && threadContext != null) { 
                    if (status == RequestNotificationStatus.Continue) {
                        try { 
                            context.InIndicateCompletion = true;
                            Interlocked.Increment(ref _inIndicateCompletionCount);
                            IIS.MgdIndicateCompletion(nativeRequestContext, ref status);
                        } 
                        finally {
                            Interlocked.Decrement(ref _inIndicateCompletionCount); 
                                // Leave will have been called already if the last notification is returning pending 
                            if (!threadContext.HasLeaveBeenCalled) {
                                lock (threadContext) { 
                                    if (!threadContext.HasLeaveBeenCalled) {
                                        threadContext.Leave();
                                        context.IndicateCompletionContext = null;
                                        context.InIndicateCompletion = false; 
                                    }
                                } 
                            } 
                        }
                    } 
                    else {
                        if (!threadContext.HasLeaveBeenCalled) {
                            lock (threadContext) {
                                if (!threadContext.HasLeaveBeenCalled) { 
                                    threadContext.Leave();
                                    context.IndicateCompletionContext = null; 
                                    context.InIndicateCompletion = false; 
                                }
                            } 
                        }
                    }
                }
            } 

            return (int)status; 
        } 

        private static HttpContext CreateContext(IIS7WorkerRequest wr, IntPtr nativeRequestContext) { 
            HttpContext context = null;
            try {
                // this may throw, e.g. see WOS 1724573: ASP.Net v2.0: wrong error code returned when ? is used in the URL
                context = new HttpContext(wr, false); 
            }
            catch(Exception e) { 
                try { 
                    WebBaseEvent.RaiseRuntimeError(e, wr);
                } catch {} // ignore exceptions that happen while trying to report the error 

                // treat as "400 Bad Request" since that's the only reason the HttpContext.ctor should throw
                IIS.MgdSetBadRequestStatus(nativeRequestContext);
            } 
            return context;
        } 
 
        /// <include file='doc\ISAPIRuntime.uex' path='docs/doc[@for="ISAPIRuntime.IRegisteredObject.Stop"]/*' />
        /// <internalonly/> 
        [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
        void IRegisteredObject.Stop(bool immediate) {
            Debug.Trace("PipelineDomain", "IRegisteredObject.Stop appId = " +
                        s_thisAppDomainsIsapiAppId); 

            while (!s_InitializationCompleted && !s_StopProcessingCalled) { 
                // the native W3_MGD_APP_CONTEXT is not ready for us to unload 
                Thread.Sleep(250);
            } 

            RemoveThisAppDomainFromUnmanagedTable();
            HostingEnvironment.UnregisterObject(this);
        } 

        internal void SetThisAppDomainsIsapiAppId(String appId) { 
            Debug.Trace("PipelineDomain", "SetThisAppDomainsPipelineAppId appId=" + appId); 
            s_thisAppDomainsIsapiAppId = appId;
        } 

        internal static void RemoveThisAppDomainFromUnmanagedTable() {
            if (Interlocked.Exchange(ref s_isThisAppDomainRemovedFromUnmanagedTable, 1) != 0) {
                return; 
            }
 
            // 
            // only notify mgdeng of this shutdown if we went through
            // Initialize from the there 
            // We can also have PipelineRuntime in app domains with only
            // other protocols
            //
            try { 
                if (s_thisAppDomainsIsapiAppId != null  && s_ApplicationContext != IntPtr.Zero) {
                    Debug.Trace("PipelineDomain", "Calling MgdAppDomainShutdown appId=" + 
                        s_thisAppDomainsIsapiAppId + " (AppDomainAppId=" + HttpRuntime.AppDomainAppIdInternal + ")"); 

                    UnsafeIISMethods.MgdAppDomainShutdown(s_ApplicationContext); 
                }

                HttpRuntime.AddAppDomainTraceMessage(SR.GetString(SR.App_Domain_Restart));
            } 
            catch(Exception e) {
                if (ShouldRethrowException(e)) { 
                    throw; 
                }
            } 
        }

        internal static bool ShouldRethrowException(Exception ex) {
            return     ex is NullReferenceException 
                    || ex is AccessViolationException
                    || ex is StackOverflowException 
                    || ex is OutOfMemoryException 
                    || ex is System.Threading.ThreadAbortException;
        } 

    }
}
//------------------------------------------------------------------------------ 
// <copyright file="IIS7Runtime.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * The ASP.NET/IIS 7 integrated pipeline runtime service host 
 *
 * Copyright (c) 2004 Microsoft Corporation 
 */

namespace System.Web.Hosting {
    using System.Collections; 
    using System.Globalization;
    using System.Reflection; 
    using System.Runtime.InteropServices; 
    using System.Security.Permissions;
    using System.Security.Principal; 
    using System.Text;
    using System.Threading;
    using System.Web.Util;
    using System.Web; 
    using System.Web.Management;
    using System.IO; 
 
    using IIS = UnsafeIISMethods;
 

    // this delegate is called from native code
    // each time a native-managed
    // transition is made to process a request state 
    delegate int ExecuteFunctionDelegate(
            IntPtr managedHttpContext, 
            IntPtr nativeRequestContext, 
            IntPtr moduleData,
            int flags); 

    delegate bool RoleFunctionDelegate(
            IntPtr pManagedPrincipal,
            IntPtr pszRole, 
            int cchRole,
            bool disposing); 
 
    // this delegate is called from native code when the request is complete
    // to free any managed resources associated with the request 
    delegate void DisposeFunctionDelegate( [In] IntPtr managedHttpContext );

    [ComImport, Guid("c96cb854-aec2-4208-9ada-a86a96860cb6"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPipelineRuntime { 
        void StartProcessing();
        void StopProcessing(); 
        void InitializeApplication([In] IntPtr appContext); 
        IntPtr GetExecuteDelegate();
        IntPtr GetDisposeDelegate(); 
        IntPtr GetRoleDelegate();
    }

    /// <include file='doc\ISAPIRuntime.uex' path='docs/doc[@for="ISAPIRuntime"]/*' /> 
    /// <devdoc>
    ///    <para>[To be supplied.]</para> 
    /// </devdoc> 
    /// <internalonly/>
    internal sealed class PipelineRuntime : MarshalByRefObject, IPipelineRuntime, IRegisteredObject { 

        // initialization error handling
        internal const string InitExceptionModuleName = "AspNetInitializationExceptionModule";
        private const string s_InitExceptionModulePrecondition = ""; 

        // to control removal from unmanaged table (to it only once) 
        private static int s_isThisAppDomainRemovedFromUnmanagedTable; 
        private static IntPtr s_ApplicationContext;
        private static string s_thisAppDomainsIsapiAppId; 

        // when GL_APPLICATION_STOP fires, this is set to true to indicate that we can unload the AppDomain
        private static bool s_StopProcessingCalled;
        private static bool s_InitializationCompleted; 

        // keep rooted through the app domain lifetime 
        private static object _delegatelock = new object(); 

        private static int _inIndicateCompletionCount; 

        private static IntPtr _executeDelegatePointer = IntPtr.Zero;
        private static ExecuteFunctionDelegate _executeDelegate = null;
 
        private static IntPtr _disposeDelegatePointer = IntPtr.Zero;
        private static DisposeFunctionDelegate _disposeDelegate = null; 
 
        private static IntPtr _roleDelegatePointer = IntPtr.Zero;
        private static RoleFunctionDelegate _roleDelegate = null; 

        public IntPtr GetExecuteDelegate() {
            if (IntPtr.Zero == _executeDelegatePointer) {
                lock (_delegatelock) { 
                    if (IntPtr.Zero == _executeDelegatePointer) {
                        ExecuteFunctionDelegate d = new ExecuteFunctionDelegate(ProcessRequestNotification); 
                        if (null != d) { 
                            IntPtr p = Marshal.GetFunctionPointerForDelegate(d);
                            if (IntPtr.Zero != p) { 
                                Thread.MemoryBarrier();
                                _executeDelegate = d;
                                _executeDelegatePointer = p;
                            } 
                        }
                    } 
                } 
            }
 
            return _executeDelegatePointer;
        }

        public IntPtr GetDisposeDelegate() { 
            if (IntPtr.Zero == _disposeDelegatePointer) {
                lock (_delegatelock) { 
                    if (IntPtr.Zero == _disposeDelegatePointer) { 
                        DisposeFunctionDelegate d = new DisposeFunctionDelegate(DisposeHandler);
                        if (null != d) { 
                            IntPtr p = Marshal.GetFunctionPointerForDelegate(d);
                            if (IntPtr.Zero != p) {
                                Thread.MemoryBarrier();
                                _disposeDelegate = d; 
                                _disposeDelegatePointer = p;
                            } 
                        } 
                    }
                } 
            }

            return _disposeDelegatePointer;
        } 

        public IntPtr GetRoleDelegate() { 
            if (IntPtr.Zero == _roleDelegatePointer) { 
                lock (_delegatelock) {
                    if (IntPtr.Zero == _roleDelegatePointer) { 
                        RoleFunctionDelegate d = new RoleFunctionDelegate(RoleHandler);
                        if (null != d) {
                            IntPtr p = Marshal.GetFunctionPointerForDelegate(d);
                            if (IntPtr.Zero != p) { 
                                Thread.MemoryBarrier();
                                _roleDelegate = d; 
                                _roleDelegatePointer = p; 
                            }
                        } 
                    }
                }
            }
 
            return _roleDelegatePointer;
        } 
 

 
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)]
        [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Minimal)]
        public PipelineRuntime() {
            HostingEnvironment.RegisterObject(this); 
            Debug.Trace("PipelineDomain", "RegisterObject(this) called");
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        public override Object InitializeLifetimeService() { 
            return null; // never expire lease
        }

        public void StartProcessing() { 
            Debug.Trace("PipelineDomain", "StartProcessing AppId = " + s_thisAppDomainsIsapiAppId);
        } 
 
        public void StopProcessing() {
            Debug.Trace("PipelineDomain", "StopProcessing with stack = " + Environment.StackTrace 
                        + " for AppId= " +  s_thisAppDomainsIsapiAppId);

            if (IIS.MgdHasConfigChanged() && !HostingEnvironment.ShutdownInitiated) {
                HttpRuntime.SetShutdownReason(ApplicationShutdownReason.ConfigurationChange, "IIS configuration change"); 
            }
 
            s_StopProcessingCalled = true; 
            // inititate shutdown and
            // require the native callback for Stop 
            HostingEnvironment.InitiateShutdown();
        }

        internal static void WaitForRequestsToDrain() { 
            while (!s_StopProcessingCalled || _inIndicateCompletionCount > 0) {
                Thread.Sleep(250); 
            } 
        }
 
        private StringBuilder FormatExceptionMessage(Exception e, string[] strings) {
            StringBuilder sb = new StringBuilder(4096);

            if (null != strings) { 
                for (int i = 0; i < strings.Length; i++) {
                    sb.Append(strings[i]); 
                } 
            }
            for (Exception current = e; current != null; current = current.InnerException) { 
                if (current == e)
                    sb.Append("\r\n\r\nException: ");
                else
                    sb.Append("\r\n\r\nInnerException: "); 
                sb.Append(current.GetType().FullName);
                sb.Append("\r\nMessage: "); 
                sb.Append(current.Message); 
                sb.Append("\r\nStackTrace: ");
                sb.Append(current.StackTrace); 
            }

            return sb;
        } 

        public void InitializeApplication(IntPtr appContext) 
        { 
            s_ApplicationContext = appContext;
 
            HttpApplication app = null;

            try {
                HttpRuntime.UseIntegratedPipeline = true; 

                // if HttpRuntime.HostingInit failed, do not attempt to create the application (WOS #1653963) 
                if (!HttpRuntime.HostingInitFailed) { 
                    //
                    //  On IIS7, application initialization does not provide an http context.  Theoretically, 
                    //  no one should be using the context during application initialization, but people do.
                    //  Create a dummy context that is used during application initialization
                    //  to prevent breakage (ISAPI mode always provides a context)
                    // 
                    HttpWorkerRequest initWorkerRequest = new SimpleWorkerRequest("" /*page*/,
                                                                                  "" /*query*/, 
                                                                                  new StringWriter(CultureInfo.InvariantCulture)); 
                    HttpContext initHttpContext = new HttpContext(initWorkerRequest);
                    app = HttpApplicationFactory.GetPipelineApplicationInstance(appContext, initHttpContext); 
                }
            }
            catch(Exception e)
            { 
                if (HttpRuntime.InitializationException == null) {
                    HttpRuntime.InitializationException = e; 
                } 
            }
            finally { 
                s_InitializationCompleted = true;

                if (HttpRuntime.InitializationException != null) {
 
                    // at least one module must be registered so that we
                    // call ProcessRequestNotification later and send the formatted 
                    // InitializationException to the client. 
                    int hresult = UnsafeIISMethods.MgdRegisterEventSubscription(
                        appContext, 
                        InitExceptionModuleName,
                        RequestNotification.BeginRequest,
                        0 /*postRequestNotifications*/,
                        InitExceptionModuleName, 
                        s_InitExceptionModulePrecondition,
                        new IntPtr(-1), 
                        false /*useHighPriority*/); 

                    if (hresult < 0) { 
                        throw new COMException( SR.GetString(SR.Failed_Pipeline_Subscription, InitExceptionModuleName),
                                                hresult );
                    }
 
                    // Always register a managed handler:
                    // WOS 1990290: VS F5 Debugging: "AspNetInitializationExceptionModule" is registered for RQ_BEGIN_REQUEST, 
                    // but the DEBUG verb skips notifications until post RQ_AUTHENTICATE_REQUEST. 
                    hresult = UnsafeIISMethods.MgdRegisterEventSubscription(
                        appContext, 
                        HttpApplication.IMPLICIT_HANDLER,
                        RequestNotification.ExecuteRequestHandler /*requestNotifications*/,
                        0 /*postRequestNotifications*/,
                        String.Empty /*type*/, 
                        HttpApplication.MANAGED_PRECONDITION /*precondition*/,
                        new IntPtr(-1), 
                        false /*useHighPriority*/); 

                    if (hresult < 0) { 
                        throw new COMException( SR.GetString(SR.Failed_Pipeline_Subscription, HttpApplication.IMPLICIT_HANDLER),
                                                hresult );
                    }
                } 

                if (app != null) { 
                    HttpApplicationFactory.RecyclePipelineApplicationInstance(app); 
                }
            } 
        }

        private static HttpContext UnwrapContext(IntPtr contextPtr) {
            GCHandle h = GCHandle.FromIntPtr(contextPtr); 
            return (HttpContext) h.Target;
        } 
 
        internal bool HostingShutdownInitiated {
            get { 
                return HostingEnvironment.ShutdownInitiated;
            }
        }
 
        internal static bool RoleHandler(IntPtr pManagedPrincipal, IntPtr pszRole, int cchRole, bool disposing) {
            GCHandle h = GCHandle.FromIntPtr(pManagedPrincipal); 
            IPrincipal principal = (IPrincipal) h.Target; 
            if (principal != null) {
                if (disposing) { 
                    if (h.IsAllocated) {
                        h.Free();
                    }
                    WindowsIdentity id = principal.Identity as WindowsIdentity; 
                    if (id != null) {
                        id.Dispose(); 
                    } 
                    return false;
                } 

                return principal.IsInRole(StringUtil.StringFromWCharPtr(pszRole, cchRole));
            }
            return false; 
        }
 
        // called from native code when the IHttpContext is disposed 
        internal static void DisposeHandler(IntPtr managedHttpContext) {
            HttpContext context = UnwrapContext(managedHttpContext); 
            DisposeHandlerPrivate(context);
        }

        // called from managed code as a perf optimization to avoid calling back later 
        internal static void DisposeHandler(HttpContext context, IntPtr nativeRequestContext, RequestNotificationStatus status) {
            if (IIS.MgdCanDisposeManagedContext(nativeRequestContext, status)) { 
                DisposeHandlerPrivate(context); 
            }
        } 

        private static void DisposeHandlerPrivate(HttpContext context) {
            Debug.Trace("PipelineRuntime", "DisposeHandler");
            try { 
                context.FinishPipelineRequest();
 
                IIS7WorkerRequest wr = context.WorkerRequest as IIS7WorkerRequest; 
                if (wr != null) {
                    wr.Dispose(); 
                }

                PerfCounters.DecrementCounter(AppPerfCounter.REQUESTS_EXECUTING);
 
                // make sure that the principal is cleaned up to ensure
                // tokens get closed quickly 
                context.DisposePrincipal(); 
            }
            finally { 
                if(context != null) {
                    context.Unroot();
                }
                HttpRuntime.DecrementActivePipelineCount(); 
            }
        } 
 
        //
        // This is the managed entry point for processing request notifications. 
        // Although this method is wrapped in try/catch, it is not supposed to
        // cause an exception. If it does throw, the application, httpwriter, etc
        // may not be initialized, and it might not be possible to process the rest
        // of the request. I would prefer to let this method throw and crash the 
        // process, but for now we will consume the exception, report the error to
        // IIS, and continue. 
        // 
        // Code that might throw belongs in HttpRuntime::ProcessRequestNotificationPrivate.
        // 
        internal static int ProcessRequestNotification(
                IntPtr managedHttpContext,
                IntPtr nativeRequestContext,
                IntPtr moduleData, 
                int flags)
        { 
            try { 
                return ProcessRequestNotificationHelper(managedHttpContext, nativeRequestContext, moduleData, flags);
            } 
            catch(Exception e) {
                ApplicationManager.RecordFatalException(e);
                throw;
            } 
        }
 
        internal static int ProcessRequestNotificationHelper( 
                IntPtr managedHttpContext,
                IntPtr nativeRequestContext, 
                IntPtr moduleData,
                int flags)
        {
            IIS7WorkerRequest wr = null; 
            HttpContext context = null;
            RequestNotificationStatus status = RequestNotificationStatus.Continue; 
 
            if (managedHttpContext == IntPtr.Zero) {
                wr = IIS7WorkerRequest.CreateWorkerRequest(nativeRequestContext, ((flags & HttpContext.FLAG_ETW_PROVIDER_ENABLED) == HttpContext.FLAG_ETW_PROVIDER_ENABLED)); 
                context = CreateContext(wr, nativeRequestContext);
                if (context == null) {
                    return (int)RequestNotificationStatus.FinishRequest;
                } 
                context.Root();
                IIS.MgdSetManagedHttpContext(nativeRequestContext, context.ContextPtr); 
 
                // Increment active request count as soon as possible to prevent
                // shutdown of the appdomain while requests are in flight.  It 
                // is decremented in DisposeHandler
                HttpRuntime.IncrementActivePipelineCount();
            }
            else { 
                context = UnwrapContext(managedHttpContext);
                wr = context.WorkerRequest as IIS7WorkerRequest; 
            } 

            // It is possible for a notification to complete asynchronously while we're in 
            // a call to IndicateCompletion, in which case a new IIS thread might enter before
            // the call to IndicateCompletion returns.  If this happens, block the thread until
            // IndicateCompletion returns.
            if (context.InIndicateCompletion && context.CurrentThread != Thread.CurrentThread) { 
                while (context.InIndicateCompletion) {
                    Thread.Sleep(10); 
                } 
            }
 
            // RQ_SEND_RESPONSE fires out of band and completes synchronously only.
            // The pipeline must be reentrant to support this, so the notification
            // context for the previous notification must be saved and restored.
            NotificationContext savedNotificationContext = context.NotificationContext; 
            try {
                context.NotificationContext = new NotificationContext(flags /*CurrentNotificationFlags*/, 
                                                                      savedNotificationContext != null /*IsReEntry*/); 
                status = HttpRuntime.ProcessRequestNotification(wr, context);
            } 
            finally {
                if (status != RequestNotificationStatus.Pending) {
                    // if we completed the notification, pop the notification context stack
                    // if this is an asynchronous unwind, then the completion will clear the context 
                    context.NotificationContext = savedNotificationContext;
                } 
            } 

            if (status != RequestNotificationStatus.Pending) { 
                // WOS 1785741: (Perf) In profiles, 8% of HelloWorld is transitioning from native to managed.
                // The fix is to keep managed code on the stack so that the AppDomain context remains on the
                // thread, and we can re-enter managed code without setting up the AppDomain context.
                // If this optimization is possible, MgdIndicateCompletion will execute one or more notifications 
                // and return PENDING as the status.
                HttpApplication.ThreadContext threadContext = context.IndicateCompletionContext; 
                if (!context.InIndicateCompletion && threadContext != null) { 
                    if (status == RequestNotificationStatus.Continue) {
                        try { 
                            context.InIndicateCompletion = true;
                            Interlocked.Increment(ref _inIndicateCompletionCount);
                            IIS.MgdIndicateCompletion(nativeRequestContext, ref status);
                        } 
                        finally {
                            Interlocked.Decrement(ref _inIndicateCompletionCount); 
                                // Leave will have been called already if the last notification is returning pending 
                            if (!threadContext.HasLeaveBeenCalled) {
                                lock (threadContext) { 
                                    if (!threadContext.HasLeaveBeenCalled) {
                                        threadContext.Leave();
                                        context.IndicateCompletionContext = null;
                                        context.InIndicateCompletion = false; 
                                    }
                                } 
                            } 
                        }
                    } 
                    else {
                        if (!threadContext.HasLeaveBeenCalled) {
                            lock (threadContext) {
                                if (!threadContext.HasLeaveBeenCalled) { 
                                    threadContext.Leave();
                                    context.IndicateCompletionContext = null; 
                                    context.InIndicateCompletion = false; 
                                }
                            } 
                        }
                    }
                }
            } 

            return (int)status; 
        } 

        private static HttpContext CreateContext(IIS7WorkerRequest wr, IntPtr nativeRequestContext) { 
            HttpContext context = null;
            try {
                // this may throw, e.g. see WOS 1724573: ASP.Net v2.0: wrong error code returned when ? is used in the URL
                context = new HttpContext(wr, false); 
            }
            catch(Exception e) { 
                try { 
                    WebBaseEvent.RaiseRuntimeError(e, wr);
                } catch {} // ignore exceptions that happen while trying to report the error 

                // treat as "400 Bad Request" since that's the only reason the HttpContext.ctor should throw
                IIS.MgdSetBadRequestStatus(nativeRequestContext);
            } 
            return context;
        } 
 
        /// <include file='doc\ISAPIRuntime.uex' path='docs/doc[@for="ISAPIRuntime.IRegisteredObject.Stop"]/*' />
        /// <internalonly/> 
        [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
        void IRegisteredObject.Stop(bool immediate) {
            Debug.Trace("PipelineDomain", "IRegisteredObject.Stop appId = " +
                        s_thisAppDomainsIsapiAppId); 

            while (!s_InitializationCompleted && !s_StopProcessingCalled) { 
                // the native W3_MGD_APP_CONTEXT is not ready for us to unload 
                Thread.Sleep(250);
            } 

            RemoveThisAppDomainFromUnmanagedTable();
            HostingEnvironment.UnregisterObject(this);
        } 

        internal void SetThisAppDomainsIsapiAppId(String appId) { 
            Debug.Trace("PipelineDomain", "SetThisAppDomainsPipelineAppId appId=" + appId); 
            s_thisAppDomainsIsapiAppId = appId;
        } 

        internal static void RemoveThisAppDomainFromUnmanagedTable() {
            if (Interlocked.Exchange(ref s_isThisAppDomainRemovedFromUnmanagedTable, 1) != 0) {
                return; 
            }
 
            // 
            // only notify mgdeng of this shutdown if we went through
            // Initialize from the there 
            // We can also have PipelineRuntime in app domains with only
            // other protocols
            //
            try { 
                if (s_thisAppDomainsIsapiAppId != null  && s_ApplicationContext != IntPtr.Zero) {
                    Debug.Trace("PipelineDomain", "Calling MgdAppDomainShutdown appId=" + 
                        s_thisAppDomainsIsapiAppId + " (AppDomainAppId=" + HttpRuntime.AppDomainAppIdInternal + ")"); 

                    UnsafeIISMethods.MgdAppDomainShutdown(s_ApplicationContext); 
                }

                HttpRuntime.AddAppDomainTraceMessage(SR.GetString(SR.App_Domain_Restart));
            } 
            catch(Exception e) {
                if (ShouldRethrowException(e)) { 
                    throw; 
                }
            } 
        }

        internal static bool ShouldRethrowException(Exception ex) {
            return     ex is NullReferenceException 
                    || ex is AccessViolationException
                    || ex is StackOverflowException 
                    || ex is OutOfMemoryException 
                    || ex is System.Threading.ThreadAbortException;
        } 

    }
}
