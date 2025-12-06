//------------------------------------------------------------------------------ 
// <copyright file="ApplicationManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Hosting { 
    using System; 
    using System.Collections;
    using System.Configuration; 
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Security.Policy; 
    using System.Threading;
    using System.Web; 
    using System.Web.Configuration;
    using System.Web.Util;
    using System.Web.Compilation;
    using System.Text; 

 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class ApplicationManager : MarshalByRefObject {
        private static Object _applicationManagerStaticLock = new Object(); 

        // open count (when last close goes to 0 it shuts down everything)
        int _openCount = 0;
        bool _shutdownInProgress = false; 

        // table of app domains (hosting environment objects) by app id 
        private Hashtable _appDomains = new Hashtable(StringComparer.OrdinalIgnoreCase); 

        // could differ from hashtable count (host env is active some time after it is removed) 
        private int _activeHostingEnvCount;

        // pending callback to respond to ping (typed as Object to do Interlocked operations)
        private Object _pendingPingCallback; 
        // delegate OnRespondToPing
        private WaitCallback _onRespondToPingWaitCallback; 
 
        // single instance of app manager
        private static ApplicationManager _theAppManager; 

        private StringBuilder _appDomainsShutdowdIds = new StringBuilder();

        // store fatal exception to assist debugging 
        private static Exception _fatalException = null;
 
        internal ApplicationManager() { 
            _onRespondToPingWaitCallback = new WaitCallback(this.OnRespondToPingWaitCallback);
 
            // VSWhidbey 555767: Need better logging for unhandled exceptions (http://support.microsoft.com/?id=911816)
            // We only add a handler in the default domain because it will be notified when an unhandled exception
            // occurs in ANY domain.
            // WOS 1983175: (weird) only the handler in the default domain is notified when there is an AV in a native module 
            // while we're in a call to MgdIndicateCompletion.
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException); 
        } 

        internal static void RecordFatalException(Exception e) { 
            RecordFatalException(AppDomain.CurrentDomain, e);
        }

        internal static void RecordFatalException(AppDomain appDomain, Exception e) { 
            // store the exception from the first caller to assist debugging
            object originalValue = Interlocked.CompareExchange(ref _fatalException, e, null); 
 
            if (originalValue == null) {
                // create event log entry 
                Misc.WriteUnhandledExceptionToEventLog(appDomain, e);
            }
        }
 
        private static void OnUnhandledException(Object sender, UnhandledExceptionEventArgs eventArgs) {
            // if the CLR is not terminating, ignore the notification 
            if (!eventArgs.IsTerminating) { 
                return;
            } 

            Exception exception = eventArgs.ExceptionObject as Exception;
            if (exception == null) {
                return; 
            }
 
            AppDomain appDomain = sender as AppDomain; 
            if (appDomain == null) {
                return; 
            }

            RecordFatalException(appDomain, exception);
        } 

        public override Object InitializeLifetimeService() { 
            return null; // never expire lease 
        }
 
        //
        // public ApplicationManager methods
        //
 

        public static ApplicationManager GetApplicationManager() { 
            if (_theAppManager == null) { 
                lock (_applicationManagerStaticLock) {
                    if (_theAppManager == null) { 
                        if (HostingEnvironment.IsHosted)
                            _theAppManager = HostingEnvironment.GetApplicationManager();

                        if (_theAppManager == null) 
                            _theAppManager = new ApplicationManager();
                    } 
                } 
            }
 
            return _theAppManager;
        }

 
        [SecurityPermission(SecurityAction.Demand, Unrestricted=true)]
        public void Open() { 
            Interlocked.Increment(ref _openCount); 
        }
 

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
        public void Close() {
            if (Interlocked.Decrement(ref _openCount) > 0) 
                return;
 
            // need to shutdown everything 
            ShutdownAll();
        } 

        private string CreateSimpleAppID(VirtualPath virtualPath, string physicalPath, string siteName) {
            // Put together some unique app id
            string appId = String.Concat(virtualPath.VirtualPathString, physicalPath); 

            if (!String.IsNullOrEmpty(siteName)) { 
                appId = String.Concat(appId, siteName); 
            }
 
            return appId.GetHashCode().ToString("x", CultureInfo.InvariantCulture);
        }

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public IRegisteredObject CreateObject(IApplicationHost appHost, Type type) {
            if (appHost == null) { 
                throw new ArgumentNullException("appHost"); 
            }
            if (type == null) { 
                throw new ArgumentNullException("type");
            }

            string appID = CreateSimpleAppID(VirtualPath.Create(appHost.GetVirtualPath()), 
                                             appHost.GetPhysicalPath(), appHost.GetSiteName());
            return CreateObjectInternal(appID, type, appHost, false); 
        } 

        [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)] 
        public IRegisteredObject CreateObject(String appId, Type type, string virtualPath, string physicalPath, bool failIfExists) {
            return CreateObject(appId, type, virtualPath, physicalPath, failIfExists, false /*throwOnError*/);
        }
 
        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
        public IRegisteredObject CreateObject(String appId, Type type, string virtualPath, string physicalPath, 
                                              bool failIfExists, bool throwOnError) { 
            // check args
            if (appId == null) 
                throw new ArgumentNullException("appId");

            SimpleApplicationHost appHost = new SimpleApplicationHost(VirtualPath.CreateAbsolute(virtualPath), physicalPath);
 
            // if throw on error flag is set, create hosting parameters accordingly
            HostingEnvironmentParameters hostingParameters = null; 
 
            if (throwOnError) {
                hostingParameters = new HostingEnvironmentParameters(); 
                hostingParameters.HostingFlags = HostingEnvironmentFlags.ThrowHostingInitErrors;

            }
 
            // call the internal method
            return CreateObjectInternal(appId, type, appHost, failIfExists, hostingParameters); 
        } 

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        internal IRegisteredObject CreateObjectInternal(String appId, Type type, IApplicationHost appHost, bool failIfExists) {
            // check args
            if (appId == null)
                throw new ArgumentNullException("appId"); 

            if (type == null) 
                throw new ArgumentNullException("type"); 

            if (appHost == null) 
                throw new ArgumentNullException("appHost");

            // call the internal method
            return CreateObjectInternal(appId, type, appHost, failIfExists, null /*hostingParameters*/); 
        }
 
        internal IRegisteredObject CreateObjectInternal( 
                                        String appId,
                                        Type type, 
                                        IApplicationHost appHost,
                                        bool failIfExists,
                                        HostingEnvironmentParameters hostingParameters) {
 
            // check that type is as IRegisteredObject
            if (!typeof(IRegisteredObject).IsAssignableFrom(type)) 
                throw new ArgumentException(SR.GetString(SR.Not_IRegisteredObject, type.FullName), "type"); 

            // get hosting environment 
            HostingEnvironment env = GetAppDomainWithHostingEnvironment(appId, appHost, hostingParameters);

            // create the managed object in the worker app domain
            ObjectHandle h = env.CreateWellKnownObjectInstance(type, failIfExists); 
            return (h != null) ? h.Unwrap() as IRegisteredObject : null;
        } 
 
        internal IRegisteredObject CreateObjectWithDefaultAppHostAndAppId(
                                        String physicalPath, 
                                        string virtualPath,
                                        Type type,
                                        out String appId) {
            return CreateObjectWithDefaultAppHostAndAppId(physicalPath, 
                VirtualPath.CreateNonRelative(virtualPath), type, out appId);
        } 
 
        internal IRegisteredObject CreateObjectWithDefaultAppHostAndAppId(
                                        String physicalPath, 
                                        VirtualPath virtualPath,
                                        Type type,
                                        out String appId) {
 
            HostingEnvironmentParameters hostingParameters = new HostingEnvironmentParameters();
            hostingParameters.HostingFlags = HostingEnvironmentFlags.DontCallAppInitialize; 
 
            return CreateObjectWithDefaultAppHostAndAppId(
                        physicalPath, 
                        virtualPath,
                        type,
                        false,
                        hostingParameters, 
                        out appId);
        } 
 
        internal IRegisteredObject CreateObjectWithDefaultAppHostAndAppId(
                                        String physicalPath, 
                                        VirtualPath virtualPath,
                                        Type type,
                                        bool failIfExists,
                                        HostingEnvironmentParameters hostingParameters, 
                                        out String appId) {
 
            IApplicationHost appHost; 
#if !FEATURE_PAL // FEATURE_PAL does not enable IIS-based hosting features
            if (physicalPath == null) { 

                // If the physical path is null, we use an ISAPIApplicationHost based
                // on the virtual path (or metabase id).
 
                // Make sure the static HttpRuntime is created so isapi assembly can be loaded properly.
                HttpRuntime.ForceStaticInit(); 
 
                ISAPIApplicationHost isapiAppHost = new ISAPIApplicationHost(virtualPath.VirtualPathString, null, true);
 
                appHost = isapiAppHost;
                appId = isapiAppHost.AppId;
                virtualPath = VirtualPath.Create(appHost.GetVirtualPath());
                physicalPath = FileUtil.FixUpPhysicalDirectory(appHost.GetPhysicalPath()); 
            }
            else { 
#endif // !FEATURE_PAL 
                // If the physical path was passed in, don't use an Isapi host. Instead,
                // use a simple app host which does simple virtual to physical mappings 

                // Put together some unique app id
                appId = CreateSimpleAppID(virtualPath, physicalPath, null);
 
                appHost = new SimpleApplicationHost(virtualPath, physicalPath);
            } 
 
            string precompTargetPhysicalDir = hostingParameters.PrecompilationTargetPhysicalDirectory;
            if (precompTargetPhysicalDir != null) { 
                // Make sure the target physical dir has no relation with the source
                BuildManager.VerifyUnrelatedSourceAndDest(physicalPath, precompTargetPhysicalDir);

                // Change the appID so we use a different codegendir in precompile for deployment scenario, 
                // this ensures we don't use or pollute the regular codegen files.  Also, use different
                // ID's depending on whether the precompilation is Updatable (VSWhidbey 383239) 
                if ((hostingParameters.ClientBuildManagerParameter != null) && 
                    (hostingParameters.ClientBuildManagerParameter.PrecompilationFlags & PrecompilationFlags.Updatable) == 0)
                    appId = appId + "_precompile"; 
                else
                    appId = appId + "_precompile_u";
            }
 
            return CreateObjectInternal(appId, type, appHost, failIfExists, hostingParameters);
        } 
 

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public IRegisteredObject GetObject(String appId, Type type) {
            // check args
            if (appId == null)
                throw new ArgumentNullException("appId"); 
            if (type == null)
                throw new ArgumentNullException("type"); 
 
            // get hosting environment
            HostingEnvironment env = FindAppDomainWithHostingEnvironment(appId); 
            if (env == null)
                return null;

            // find the instance by type 
            ObjectHandle h =  env.FindWellKnownObject(type);
            return (h != null) ? h.Unwrap() as IRegisteredObject : null; 
        } 

        // if a "well-known" object of the specified type already exists in the application, 
        // remove the app from the managed application table.  This is
        // used in IIS7 integrated mode when IIS7 determines that it is necessary to create
        // a new application and shutdown the old one.
        internal void RemoveFromTableIfRuntimeExists(String appId, Type runtimeType) { 
            // check args
            if (appId == null) 
                throw new ArgumentNullException("appId"); 
            if (runtimeType == null)
                throw new ArgumentNullException("runtimeType"); 

            // get hosting environment
            HostingEnvironment env = FindAppDomainWithHostingEnvironment(appId);
            if (env == null) 
                return;
 
            // find the instance by type 
            ObjectHandle h =  env.FindWellKnownObject(runtimeType);
            if (h != null) { 
                // ensure that it is removed from _appDomains by calling
                // HostingEnvironmentShutdownInitiated directly.
                HostingEnvironmentShutdownInitiated(appId, env);
            } 
        }
 
        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public void StopObject(String appId, Type type) {
            // check args 
            if (appId == null)
                throw new ArgumentNullException("appId");
            if (type == null)
                throw new ArgumentNullException("type"); 

            HostingEnvironment env = FindAppDomainWithHostingEnvironment(appId); 
            if (env != null) { 
                env.StopWellKnownObject(type);
            } 
        }


        public bool IsIdle() { 
            lock (this) {
                foreach (DictionaryEntry e in _appDomains) { 
                    bool idle = ((HostingEnvironment)e.Value).IsIdle(); 

                    if (!idle) 
                        return false;
                }
            }
 
            return true;
        } 
 

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public void ShutdownApplication(String appId) {
            if (appId == null)
                throw new ArgumentNullException("appId");
 
            HostingEnvironment env = FindAppDomainWithHostingEnvironment(appId);
            if (env != null) { 
                env.InitiateShutdownInternal(); 
            }
        } 


        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
        public void ShutdownAll() { 
            _shutdownInProgress = true;
 
            lock (this) { 
                foreach (DictionaryEntry e in _appDomains) {
                    _appDomainsShutdowdIds.Append("SA:" + e.Key + ":" + DateTime.UtcNow.ToShortTimeString() + ";"); 
                    ((HostingEnvironment)e.Value).InitiateShutdownInternal();
                }

                // don't keep references to hosting environments anymore 
                _appDomains = new Hashtable();
            } 
 
            for (int iter=0; _activeHostingEnvCount > 0 && iter < 3000; iter++) // Wait at most 5 minutes
                Thread.Sleep(100); 
        }


        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public ApplicationInfo[] GetRunningApplications() {
            ArrayList appList = new ArrayList(); 
 
            lock (this) {
                foreach (DictionaryEntry e in _appDomains) { 
                    appList.Add(((HostingEnvironment)e.Value).GetApplicationInfo());
                }
            }
 
            int n = appList.Count;
            ApplicationInfo[] result = new ApplicationInfo[n]; 
 
            if (n > 0) {
                appList.CopyTo(result); 
            }

            return result;
        } 

        internal AppDomainInfo [] GetAppDomainInfos() 
        { 
            ArrayList appList = new ArrayList();
            AppDomainInfo appDomainInfo; 
            ApplicationInfo appInfo;
            HostingEnvironment hostEnv;
            IApplicationHost appHost;
            int siteId; 

            lock (this) { 
                foreach (DictionaryEntry e in _appDomains) { 
                    hostEnv = (HostingEnvironment) e.Value;
 
                    appHost = hostEnv.InternalApplicationHost;

                    appInfo = hostEnv.GetApplicationInfo();
 
                    if (appHost != null) {
                        try { 
                            siteId = Int32.Parse(appHost.GetSiteID(), CultureInfo.InvariantCulture); 
                        }
                        catch { 
                            siteId = 0;
                        }
                    }
                    else { 
                        siteId = 0;
                    } 
 
                    appDomainInfo = new AppDomainInfo(appInfo.ID,
                                                      appInfo.VirtualPath, 
                                                      appInfo.PhysicalPath,
                                                      siteId,
                                                      hostEnv.GetIdleValue());
 
                    appList.Add(appDomainInfo);
                } 
            } 

            return (AppDomainInfo[]) appList.ToArray(typeof(AppDomainInfo)); 
        }

        //
        // ping implementation 
        //
 
        // called from process host 
        internal void Ping(IProcessPingCallback callback) {
            if (callback == null || _pendingPingCallback != null) 
                return;

            // remember active callback but only if none is remembered already
            if (Interlocked.CompareExchange(ref _pendingPingCallback, callback, null) == null) { 
                // queue a work item to respond to ping
                ThreadPool.QueueUserWorkItem(_onRespondToPingWaitCallback); 
            } 
        }
 
        // threadpool callback (also called on some activity from hosting environment)
        internal void OnRespondToPingWaitCallback(Object state) {
            RespondToPingIfNeeded();
        } 

        // respond to ping on callback 
        internal void RespondToPingIfNeeded() { 
            IProcessPingCallback callback = _pendingPingCallback as IProcessPingCallback;
 
            // make sure we call the callback once
            if (callback != null) {
                if (Interlocked.CompareExchange(ref _pendingPingCallback, null, callback) == callback) {
                    callback.Respond(); 
                }
            } 
        } 

        // 
        // communication with hosting environments
        //

        internal void HostingEnvironmentActivated(String appId) { 
            Interlocked.Increment(ref _activeHostingEnvCount);
        } 
 
        internal void HostingEnvironmentShutdownComplete(String appId, IApplicationHost appHost) {
            try { 
                if (appHost != null) {
                    // make sure application host can be GC'd
                    MarshalByRefObject realApplicationHost = appHost as MarshalByRefObject;
                    if (realApplicationHost != null) { 
                        RemotingServices.Disconnect(realApplicationHost);
                    } 
                } 
            }
            finally { 
                Interlocked.Decrement(ref _activeHostingEnvCount);
            }
        }
 
        internal void HostingEnvironmentShutdownInitiated(String appId, HostingEnvironment env) {
            if (!_shutdownInProgress) { // don't bother during shutdown (while enumerating) 
                // remove from the table of app domains 
                lock (this) {
                    if (!env.HasBeenRemovedFromAppManagerTable) { 
                        env.HasBeenRemovedFromAppManagerTable = true;
                        _appDomainsShutdowdIds.Append("SI:" + appId + ":" + DateTime.UtcNow.ToShortTimeString() + ";");
                        _appDomains.Remove(appId);
                    } 
                }
            } 
        } 

        internal int AppDomainsCount { 
            get {
                int c = 0;
                lock (this) {
                    c = _appDomains.Count; 
                }
                return c; 
            } 
        }
 
        internal void ReduceAppDomainsCount(int limit) {
            while (_appDomains.Count >= limit && !_shutdownInProgress) {
                HostingEnvironment bestCandidateForShutdown = null;
 
                lock (this) {
                    foreach (DictionaryEntry e in _appDomains) { 
                        HostingEnvironment h = ((HostingEnvironment)e.Value); 

                        if (bestCandidateForShutdown == null || 
                                h.LruScore < bestCandidateForShutdown.LruScore) {
                            bestCandidateForShutdown = h;
                        }
                    } 
                }
 
                if (bestCandidateForShutdown == null) 
                    break;
 
                bestCandidateForShutdown.InitiateShutdownInternal();
            }
        }
 
        //
        // helper to support legacy APIs (AppHost.CreateAppHost) 
        // 

        internal ObjectHandle CreateInstanceInNewWorkerAppDomain( 
                                Type type,
                                String appId,
                                VirtualPath virtualPath,
                                String physicalPath) { 

            Debug.Trace("AppManager", "CreateObjectInNewWorkerAppDomain, type=" + type.FullName); 
 
            IApplicationHost appHost = new SimpleApplicationHost(virtualPath, physicalPath);
 
            HostingEnvironmentParameters hostingParameters = new HostingEnvironmentParameters();
            hostingParameters.HostingFlags = HostingEnvironmentFlags.HideFromAppManager;

            HostingEnvironment env = CreateAppDomainWithHostingEnvironmentAndReportErrors(appId, appHost, hostingParameters); 
            return env.CreateInstance(type);
        } 
 
        //
        // helpers to facilitate app domain creation 
        //

        private HostingEnvironment FindAppDomainWithHostingEnvironment(String appId) {
            HostingEnvironment env = null; 

            lock (this) { 
                env = _appDomains[appId] as HostingEnvironment; 
            }
 
            return env;
        }

        private HostingEnvironment GetAppDomainWithHostingEnvironment(String appId, IApplicationHost appHost, HostingEnvironmentParameters hostingParameters) { 
            HostingEnvironment env = null;
 
            lock (this) { 
                env = _appDomains[appId] as HostingEnvironment;
                if (env != null) { 
                    try {
                        env.IsUnloaded();
                    } catch(AppDomainUnloadedException) {
                        env = null; 
                        _appDomainsShutdowdIds.Append("Un:" + appId + ":" + DateTime.UtcNow.ToShortTimeString() + ";");
                    } 
                } 
                if (env == null) {
                    env = CreateAppDomainWithHostingEnvironmentAndReportErrors(appId, appHost, hostingParameters); 
                    _appDomains[appId] = env;
                }
            }
 
            return env;
        } 
 
        private HostingEnvironment CreateAppDomainWithHostingEnvironmentAndReportErrors(
                                        String appId, 
                                        IApplicationHost appHost,
                                        HostingEnvironmentParameters hostingParameters) {
            try {
                return CreateAppDomainWithHostingEnvironment(appId, appHost, hostingParameters); 
            }
            catch (Exception e) { 
                Misc.ReportUnhandledException(e, new string[] {SR.GetString(SR.Failed_to_initialize_AppDomain), appId}); 
                throw;
            } 
        }

        private HostingEnvironment CreateAppDomainWithHostingEnvironment(
                                        String appId, 
                                        IApplicationHost appHost,
                                        HostingEnvironmentParameters hostingParameters) { 
 
            String physicalPath = appHost.GetPhysicalPath();
            if (!StringUtil.StringEndsWith(physicalPath, Path.DirectorySeparatorChar)) 
                physicalPath = physicalPath + Path.DirectorySeparatorChar;

            String domainId = ConstructAppDomainId(appId);
            String appName = (StringUtil.GetStringHashCode(String.Concat(appId.ToLower(CultureInfo.InvariantCulture), 
                physicalPath.ToLower(CultureInfo.InvariantCulture)))).ToString("x", CultureInfo.InvariantCulture);
            VirtualPath virtualPath = VirtualPath.Create(appHost.GetVirtualPath()); 
 
            Debug.Trace("AppManager", "CreateAppDomainWithHostingEnvironment, path=" + physicalPath + "; appId=" + appId + "; domainId=" + domainId);
 
            IDictionary bindings = new Hashtable(20);
            AppDomainSetup setup = new AppDomainSetup();
            PopulateDomainBindings(domainId, appId, appName, physicalPath, virtualPath, setup, bindings);
 
            //  Create the app domain
 
            AppDomain appDomain = null; 
            Exception appDomainCreationException = null;
 
            try {
                appDomain = AppDomain.CreateDomain(domainId,
#if FEATURE_PAL // FEATURE_PAL: hack to avoid non-supported hosting features
                                                   null, 
#else // FEATURE_PAL
                                                   GetDefaultDomainIdentity(), 
#endif // FEATURE_PAL 
                                                   setup);
 
                foreach (DictionaryEntry e in bindings)
                    appDomain.SetData((String)e.Key, (String)e.Value);
            }
            catch (Exception e) { 
                Debug.Trace("AppManager", "AppDomain.CreateDomain failed", e);
                appDomainCreationException = e; 
            } 

            if (appDomain == null) { 
                throw new SystemException(SR.GetString(SR.Cannot_create_AppDomain), appDomainCreationException);
            }

            // Create hosting environment in the new app domain 

            Type hostType = typeof(HostingEnvironment); 
            String module = hostType.Module.Assembly.FullName; 
            String typeName = hostType.FullName;
            ObjectHandle h = null; 

            // impersonate UNC identity, if any
            ImpersonationContext ictx = null;
            IntPtr uncToken = IntPtr.Zero; 

            // 
            // fetching config can fail due to a race with the 
            // native config reader
            // if that has happened, force a flush 
            //
            int maxRetries = 10;
            int numRetries = 0;
 
            while (numRetries < maxRetries) {
                try { 
                    uncToken = appHost.GetConfigToken(); 
                    // no throw, so break
                    break; 
                }
                catch (InvalidOperationException) {
                    numRetries++;
                    System.Threading.Thread.Sleep(250); 
                }
            } 
 

            if (uncToken != IntPtr.Zero) { 
                try {
                    ictx = new ImpersonationContext(uncToken);
                }
                catch { 
                }
                finally { 
                    UnsafeNativeMethods.CloseHandle(uncToken); 
                }
            } 

            try {

                // Create the hosting environment in the app domain 
#if DBG
                try { 
                    h = appDomain.CreateInstance(module, typeName); 
                }
                catch (Exception e) { 
                    Debug.Trace("AppManager", "appDomain.CreateInstance failed; identity=" + System.Security.Principal.WindowsIdentity.GetCurrent().Name, e);
                    throw;
                }
#else 
                h = appDomain.CreateInstance(module, typeName);
#endif 
            } 
            finally {
                // revert impersonation 
                if (ictx != null)
                    ictx.Undo();

                if (h == null) { 
                    AppDomain.Unload(appDomain);
                } 
            } 

            HostingEnvironment env = (h != null) ? h.Unwrap() as HostingEnvironment : null; 

            if (env == null)
                throw new SystemException(SR.GetString(SR.Cannot_create_HostEnv));
 
            // iniitalize the hosting environment
            IConfigMapPathFactory configMapPathFactory = appHost.GetConfigMapPathFactory(); 
            env.Initialize(this, appHost, configMapPathFactory, hostingParameters); 
            return env;
        } 

        private static void PopulateDomainBindings(String domainId, String appId, String appName,
                                                    String appPath, VirtualPath appVPath,
                                                    AppDomainSetup setup, IDictionary dict) { 
            // assembly loading settings
 
            // We put both the old and new bin dir names on the private bin path 
            setup.PrivateBinPathProbe   = "*";  // disable loading from app base
            setup.ShadowCopyFiles       = "true"; 
            setup.ApplicationBase       = appPath;
            setup.ApplicationName       = appName;
            setup.ConfigurationFile     = HttpConfigurationSystem.WebConfigFileName;
 
            // Disallow code download, since it's unreliable in services (ASURT 123836/127606)
            setup.DisallowCodeDownload  = true; 
 
            // internal settings
            dict.Add(".appDomain",     "*"); 
            dict.Add(".appId",         appId);
            dict.Add(".appPath",       appPath);
            dict.Add(".appVPath",      appVPath.VirtualPathString);
            dict.Add(".domainId",      domainId); 
        }
 
        private static Evidence GetDefaultDomainIdentity() { 
            Evidence     evidence      = new Evidence();
            bool         hasZone       = false; 
            IEnumerator  enumerator;

            enumerator = AppDomain.CurrentDomain.Evidence.GetHostEnumerator();
            while (enumerator.MoveNext()) { 
                if (enumerator.Current is Zone)
                    hasZone = true; 
                evidence.AddHost( enumerator.Current ); 
            }
 
            enumerator = AppDomain.CurrentDomain.Evidence.GetAssemblyEnumerator();
            while (enumerator.MoveNext()) {
                evidence.AddAssembly( enumerator.Current );
            } 

            //evidence.AddHost( new Url( "http://localhost/ASP_Plus" ) ); // 
            if (!hasZone) 
                evidence.AddHost( new Zone( SecurityZone.MyComputer ) );
 
            return evidence;
        }

        private static int s_domainCount = 0; 
        private static Object s_domainCountLock = new Object();
 
        private static String ConstructAppDomainId(String id) { 
            int domainCount = 0;
            lock (s_domainCountLock) { 
                domainCount = ++s_domainCount;
            }
            return id + "-" + domainCount.ToString(NumberFormatInfo.InvariantInfo) + "-" + DateTime.UtcNow.ToFileTime().ToString();
        } 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="ApplicationManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Hosting { 
    using System; 
    using System.Collections;
    using System.Configuration; 
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Security.Policy; 
    using System.Threading;
    using System.Web; 
    using System.Web.Configuration;
    using System.Web.Util;
    using System.Web.Compilation;
    using System.Text; 

 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class ApplicationManager : MarshalByRefObject {
        private static Object _applicationManagerStaticLock = new Object(); 

        // open count (when last close goes to 0 it shuts down everything)
        int _openCount = 0;
        bool _shutdownInProgress = false; 

        // table of app domains (hosting environment objects) by app id 
        private Hashtable _appDomains = new Hashtable(StringComparer.OrdinalIgnoreCase); 

        // could differ from hashtable count (host env is active some time after it is removed) 
        private int _activeHostingEnvCount;

        // pending callback to respond to ping (typed as Object to do Interlocked operations)
        private Object _pendingPingCallback; 
        // delegate OnRespondToPing
        private WaitCallback _onRespondToPingWaitCallback; 
 
        // single instance of app manager
        private static ApplicationManager _theAppManager; 

        private StringBuilder _appDomainsShutdowdIds = new StringBuilder();

        // store fatal exception to assist debugging 
        private static Exception _fatalException = null;
 
        internal ApplicationManager() { 
            _onRespondToPingWaitCallback = new WaitCallback(this.OnRespondToPingWaitCallback);
 
            // VSWhidbey 555767: Need better logging for unhandled exceptions (http://support.microsoft.com/?id=911816)
            // We only add a handler in the default domain because it will be notified when an unhandled exception
            // occurs in ANY domain.
            // WOS 1983175: (weird) only the handler in the default domain is notified when there is an AV in a native module 
            // while we're in a call to MgdIndicateCompletion.
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException); 
        } 

        internal static void RecordFatalException(Exception e) { 
            RecordFatalException(AppDomain.CurrentDomain, e);
        }

        internal static void RecordFatalException(AppDomain appDomain, Exception e) { 
            // store the exception from the first caller to assist debugging
            object originalValue = Interlocked.CompareExchange(ref _fatalException, e, null); 
 
            if (originalValue == null) {
                // create event log entry 
                Misc.WriteUnhandledExceptionToEventLog(appDomain, e);
            }
        }
 
        private static void OnUnhandledException(Object sender, UnhandledExceptionEventArgs eventArgs) {
            // if the CLR is not terminating, ignore the notification 
            if (!eventArgs.IsTerminating) { 
                return;
            } 

            Exception exception = eventArgs.ExceptionObject as Exception;
            if (exception == null) {
                return; 
            }
 
            AppDomain appDomain = sender as AppDomain; 
            if (appDomain == null) {
                return; 
            }

            RecordFatalException(appDomain, exception);
        } 

        public override Object InitializeLifetimeService() { 
            return null; // never expire lease 
        }
 
        //
        // public ApplicationManager methods
        //
 

        public static ApplicationManager GetApplicationManager() { 
            if (_theAppManager == null) { 
                lock (_applicationManagerStaticLock) {
                    if (_theAppManager == null) { 
                        if (HostingEnvironment.IsHosted)
                            _theAppManager = HostingEnvironment.GetApplicationManager();

                        if (_theAppManager == null) 
                            _theAppManager = new ApplicationManager();
                    } 
                } 
            }
 
            return _theAppManager;
        }

 
        [SecurityPermission(SecurityAction.Demand, Unrestricted=true)]
        public void Open() { 
            Interlocked.Increment(ref _openCount); 
        }
 

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
        public void Close() {
            if (Interlocked.Decrement(ref _openCount) > 0) 
                return;
 
            // need to shutdown everything 
            ShutdownAll();
        } 

        private string CreateSimpleAppID(VirtualPath virtualPath, string physicalPath, string siteName) {
            // Put together some unique app id
            string appId = String.Concat(virtualPath.VirtualPathString, physicalPath); 

            if (!String.IsNullOrEmpty(siteName)) { 
                appId = String.Concat(appId, siteName); 
            }
 
            return appId.GetHashCode().ToString("x", CultureInfo.InvariantCulture);
        }

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public IRegisteredObject CreateObject(IApplicationHost appHost, Type type) {
            if (appHost == null) { 
                throw new ArgumentNullException("appHost"); 
            }
            if (type == null) { 
                throw new ArgumentNullException("type");
            }

            string appID = CreateSimpleAppID(VirtualPath.Create(appHost.GetVirtualPath()), 
                                             appHost.GetPhysicalPath(), appHost.GetSiteName());
            return CreateObjectInternal(appID, type, appHost, false); 
        } 

        [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)] 
        public IRegisteredObject CreateObject(String appId, Type type, string virtualPath, string physicalPath, bool failIfExists) {
            return CreateObject(appId, type, virtualPath, physicalPath, failIfExists, false /*throwOnError*/);
        }
 
        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
        public IRegisteredObject CreateObject(String appId, Type type, string virtualPath, string physicalPath, 
                                              bool failIfExists, bool throwOnError) { 
            // check args
            if (appId == null) 
                throw new ArgumentNullException("appId");

            SimpleApplicationHost appHost = new SimpleApplicationHost(VirtualPath.CreateAbsolute(virtualPath), physicalPath);
 
            // if throw on error flag is set, create hosting parameters accordingly
            HostingEnvironmentParameters hostingParameters = null; 
 
            if (throwOnError) {
                hostingParameters = new HostingEnvironmentParameters(); 
                hostingParameters.HostingFlags = HostingEnvironmentFlags.ThrowHostingInitErrors;

            }
 
            // call the internal method
            return CreateObjectInternal(appId, type, appHost, failIfExists, hostingParameters); 
        } 

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        internal IRegisteredObject CreateObjectInternal(String appId, Type type, IApplicationHost appHost, bool failIfExists) {
            // check args
            if (appId == null)
                throw new ArgumentNullException("appId"); 

            if (type == null) 
                throw new ArgumentNullException("type"); 

            if (appHost == null) 
                throw new ArgumentNullException("appHost");

            // call the internal method
            return CreateObjectInternal(appId, type, appHost, failIfExists, null /*hostingParameters*/); 
        }
 
        internal IRegisteredObject CreateObjectInternal( 
                                        String appId,
                                        Type type, 
                                        IApplicationHost appHost,
                                        bool failIfExists,
                                        HostingEnvironmentParameters hostingParameters) {
 
            // check that type is as IRegisteredObject
            if (!typeof(IRegisteredObject).IsAssignableFrom(type)) 
                throw new ArgumentException(SR.GetString(SR.Not_IRegisteredObject, type.FullName), "type"); 

            // get hosting environment 
            HostingEnvironment env = GetAppDomainWithHostingEnvironment(appId, appHost, hostingParameters);

            // create the managed object in the worker app domain
            ObjectHandle h = env.CreateWellKnownObjectInstance(type, failIfExists); 
            return (h != null) ? h.Unwrap() as IRegisteredObject : null;
        } 
 
        internal IRegisteredObject CreateObjectWithDefaultAppHostAndAppId(
                                        String physicalPath, 
                                        string virtualPath,
                                        Type type,
                                        out String appId) {
            return CreateObjectWithDefaultAppHostAndAppId(physicalPath, 
                VirtualPath.CreateNonRelative(virtualPath), type, out appId);
        } 
 
        internal IRegisteredObject CreateObjectWithDefaultAppHostAndAppId(
                                        String physicalPath, 
                                        VirtualPath virtualPath,
                                        Type type,
                                        out String appId) {
 
            HostingEnvironmentParameters hostingParameters = new HostingEnvironmentParameters();
            hostingParameters.HostingFlags = HostingEnvironmentFlags.DontCallAppInitialize; 
 
            return CreateObjectWithDefaultAppHostAndAppId(
                        physicalPath, 
                        virtualPath,
                        type,
                        false,
                        hostingParameters, 
                        out appId);
        } 
 
        internal IRegisteredObject CreateObjectWithDefaultAppHostAndAppId(
                                        String physicalPath, 
                                        VirtualPath virtualPath,
                                        Type type,
                                        bool failIfExists,
                                        HostingEnvironmentParameters hostingParameters, 
                                        out String appId) {
 
            IApplicationHost appHost; 
#if !FEATURE_PAL // FEATURE_PAL does not enable IIS-based hosting features
            if (physicalPath == null) { 

                // If the physical path is null, we use an ISAPIApplicationHost based
                // on the virtual path (or metabase id).
 
                // Make sure the static HttpRuntime is created so isapi assembly can be loaded properly.
                HttpRuntime.ForceStaticInit(); 
 
                ISAPIApplicationHost isapiAppHost = new ISAPIApplicationHost(virtualPath.VirtualPathString, null, true);
 
                appHost = isapiAppHost;
                appId = isapiAppHost.AppId;
                virtualPath = VirtualPath.Create(appHost.GetVirtualPath());
                physicalPath = FileUtil.FixUpPhysicalDirectory(appHost.GetPhysicalPath()); 
            }
            else { 
#endif // !FEATURE_PAL 
                // If the physical path was passed in, don't use an Isapi host. Instead,
                // use a simple app host which does simple virtual to physical mappings 

                // Put together some unique app id
                appId = CreateSimpleAppID(virtualPath, physicalPath, null);
 
                appHost = new SimpleApplicationHost(virtualPath, physicalPath);
            } 
 
            string precompTargetPhysicalDir = hostingParameters.PrecompilationTargetPhysicalDirectory;
            if (precompTargetPhysicalDir != null) { 
                // Make sure the target physical dir has no relation with the source
                BuildManager.VerifyUnrelatedSourceAndDest(physicalPath, precompTargetPhysicalDir);

                // Change the appID so we use a different codegendir in precompile for deployment scenario, 
                // this ensures we don't use or pollute the regular codegen files.  Also, use different
                // ID's depending on whether the precompilation is Updatable (VSWhidbey 383239) 
                if ((hostingParameters.ClientBuildManagerParameter != null) && 
                    (hostingParameters.ClientBuildManagerParameter.PrecompilationFlags & PrecompilationFlags.Updatable) == 0)
                    appId = appId + "_precompile"; 
                else
                    appId = appId + "_precompile_u";
            }
 
            return CreateObjectInternal(appId, type, appHost, failIfExists, hostingParameters);
        } 
 

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public IRegisteredObject GetObject(String appId, Type type) {
            // check args
            if (appId == null)
                throw new ArgumentNullException("appId"); 
            if (type == null)
                throw new ArgumentNullException("type"); 
 
            // get hosting environment
            HostingEnvironment env = FindAppDomainWithHostingEnvironment(appId); 
            if (env == null)
                return null;

            // find the instance by type 
            ObjectHandle h =  env.FindWellKnownObject(type);
            return (h != null) ? h.Unwrap() as IRegisteredObject : null; 
        } 

        // if a "well-known" object of the specified type already exists in the application, 
        // remove the app from the managed application table.  This is
        // used in IIS7 integrated mode when IIS7 determines that it is necessary to create
        // a new application and shutdown the old one.
        internal void RemoveFromTableIfRuntimeExists(String appId, Type runtimeType) { 
            // check args
            if (appId == null) 
                throw new ArgumentNullException("appId"); 
            if (runtimeType == null)
                throw new ArgumentNullException("runtimeType"); 

            // get hosting environment
            HostingEnvironment env = FindAppDomainWithHostingEnvironment(appId);
            if (env == null) 
                return;
 
            // find the instance by type 
            ObjectHandle h =  env.FindWellKnownObject(runtimeType);
            if (h != null) { 
                // ensure that it is removed from _appDomains by calling
                // HostingEnvironmentShutdownInitiated directly.
                HostingEnvironmentShutdownInitiated(appId, env);
            } 
        }
 
        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public void StopObject(String appId, Type type) {
            // check args 
            if (appId == null)
                throw new ArgumentNullException("appId");
            if (type == null)
                throw new ArgumentNullException("type"); 

            HostingEnvironment env = FindAppDomainWithHostingEnvironment(appId); 
            if (env != null) { 
                env.StopWellKnownObject(type);
            } 
        }


        public bool IsIdle() { 
            lock (this) {
                foreach (DictionaryEntry e in _appDomains) { 
                    bool idle = ((HostingEnvironment)e.Value).IsIdle(); 

                    if (!idle) 
                        return false;
                }
            }
 
            return true;
        } 
 

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public void ShutdownApplication(String appId) {
            if (appId == null)
                throw new ArgumentNullException("appId");
 
            HostingEnvironment env = FindAppDomainWithHostingEnvironment(appId);
            if (env != null) { 
                env.InitiateShutdownInternal(); 
            }
        } 


        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
        public void ShutdownAll() { 
            _shutdownInProgress = true;
 
            lock (this) { 
                foreach (DictionaryEntry e in _appDomains) {
                    _appDomainsShutdowdIds.Append("SA:" + e.Key + ":" + DateTime.UtcNow.ToShortTimeString() + ";"); 
                    ((HostingEnvironment)e.Value).InitiateShutdownInternal();
                }

                // don't keep references to hosting environments anymore 
                _appDomains = new Hashtable();
            } 
 
            for (int iter=0; _activeHostingEnvCount > 0 && iter < 3000; iter++) // Wait at most 5 minutes
                Thread.Sleep(100); 
        }


        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)] 
        public ApplicationInfo[] GetRunningApplications() {
            ArrayList appList = new ArrayList(); 
 
            lock (this) {
                foreach (DictionaryEntry e in _appDomains) { 
                    appList.Add(((HostingEnvironment)e.Value).GetApplicationInfo());
                }
            }
 
            int n = appList.Count;
            ApplicationInfo[] result = new ApplicationInfo[n]; 
 
            if (n > 0) {
                appList.CopyTo(result); 
            }

            return result;
        } 

        internal AppDomainInfo [] GetAppDomainInfos() 
        { 
            ArrayList appList = new ArrayList();
            AppDomainInfo appDomainInfo; 
            ApplicationInfo appInfo;
            HostingEnvironment hostEnv;
            IApplicationHost appHost;
            int siteId; 

            lock (this) { 
                foreach (DictionaryEntry e in _appDomains) { 
                    hostEnv = (HostingEnvironment) e.Value;
 
                    appHost = hostEnv.InternalApplicationHost;

                    appInfo = hostEnv.GetApplicationInfo();
 
                    if (appHost != null) {
                        try { 
                            siteId = Int32.Parse(appHost.GetSiteID(), CultureInfo.InvariantCulture); 
                        }
                        catch { 
                            siteId = 0;
                        }
                    }
                    else { 
                        siteId = 0;
                    } 
 
                    appDomainInfo = new AppDomainInfo(appInfo.ID,
                                                      appInfo.VirtualPath, 
                                                      appInfo.PhysicalPath,
                                                      siteId,
                                                      hostEnv.GetIdleValue());
 
                    appList.Add(appDomainInfo);
                } 
            } 

            return (AppDomainInfo[]) appList.ToArray(typeof(AppDomainInfo)); 
        }

        //
        // ping implementation 
        //
 
        // called from process host 
        internal void Ping(IProcessPingCallback callback) {
            if (callback == null || _pendingPingCallback != null) 
                return;

            // remember active callback but only if none is remembered already
            if (Interlocked.CompareExchange(ref _pendingPingCallback, callback, null) == null) { 
                // queue a work item to respond to ping
                ThreadPool.QueueUserWorkItem(_onRespondToPingWaitCallback); 
            } 
        }
 
        // threadpool callback (also called on some activity from hosting environment)
        internal void OnRespondToPingWaitCallback(Object state) {
            RespondToPingIfNeeded();
        } 

        // respond to ping on callback 
        internal void RespondToPingIfNeeded() { 
            IProcessPingCallback callback = _pendingPingCallback as IProcessPingCallback;
 
            // make sure we call the callback once
            if (callback != null) {
                if (Interlocked.CompareExchange(ref _pendingPingCallback, null, callback) == callback) {
                    callback.Respond(); 
                }
            } 
        } 

        // 
        // communication with hosting environments
        //

        internal void HostingEnvironmentActivated(String appId) { 
            Interlocked.Increment(ref _activeHostingEnvCount);
        } 
 
        internal void HostingEnvironmentShutdownComplete(String appId, IApplicationHost appHost) {
            try { 
                if (appHost != null) {
                    // make sure application host can be GC'd
                    MarshalByRefObject realApplicationHost = appHost as MarshalByRefObject;
                    if (realApplicationHost != null) { 
                        RemotingServices.Disconnect(realApplicationHost);
                    } 
                } 
            }
            finally { 
                Interlocked.Decrement(ref _activeHostingEnvCount);
            }
        }
 
        internal void HostingEnvironmentShutdownInitiated(String appId, HostingEnvironment env) {
            if (!_shutdownInProgress) { // don't bother during shutdown (while enumerating) 
                // remove from the table of app domains 
                lock (this) {
                    if (!env.HasBeenRemovedFromAppManagerTable) { 
                        env.HasBeenRemovedFromAppManagerTable = true;
                        _appDomainsShutdowdIds.Append("SI:" + appId + ":" + DateTime.UtcNow.ToShortTimeString() + ";");
                        _appDomains.Remove(appId);
                    } 
                }
            } 
        } 

        internal int AppDomainsCount { 
            get {
                int c = 0;
                lock (this) {
                    c = _appDomains.Count; 
                }
                return c; 
            } 
        }
 
        internal void ReduceAppDomainsCount(int limit) {
            while (_appDomains.Count >= limit && !_shutdownInProgress) {
                HostingEnvironment bestCandidateForShutdown = null;
 
                lock (this) {
                    foreach (DictionaryEntry e in _appDomains) { 
                        HostingEnvironment h = ((HostingEnvironment)e.Value); 

                        if (bestCandidateForShutdown == null || 
                                h.LruScore < bestCandidateForShutdown.LruScore) {
                            bestCandidateForShutdown = h;
                        }
                    } 
                }
 
                if (bestCandidateForShutdown == null) 
                    break;
 
                bestCandidateForShutdown.InitiateShutdownInternal();
            }
        }
 
        //
        // helper to support legacy APIs (AppHost.CreateAppHost) 
        // 

        internal ObjectHandle CreateInstanceInNewWorkerAppDomain( 
                                Type type,
                                String appId,
                                VirtualPath virtualPath,
                                String physicalPath) { 

            Debug.Trace("AppManager", "CreateObjectInNewWorkerAppDomain, type=" + type.FullName); 
 
            IApplicationHost appHost = new SimpleApplicationHost(virtualPath, physicalPath);
 
            HostingEnvironmentParameters hostingParameters = new HostingEnvironmentParameters();
            hostingParameters.HostingFlags = HostingEnvironmentFlags.HideFromAppManager;

            HostingEnvironment env = CreateAppDomainWithHostingEnvironmentAndReportErrors(appId, appHost, hostingParameters); 
            return env.CreateInstance(type);
        } 
 
        //
        // helpers to facilitate app domain creation 
        //

        private HostingEnvironment FindAppDomainWithHostingEnvironment(String appId) {
            HostingEnvironment env = null; 

            lock (this) { 
                env = _appDomains[appId] as HostingEnvironment; 
            }
 
            return env;
        }

        private HostingEnvironment GetAppDomainWithHostingEnvironment(String appId, IApplicationHost appHost, HostingEnvironmentParameters hostingParameters) { 
            HostingEnvironment env = null;
 
            lock (this) { 
                env = _appDomains[appId] as HostingEnvironment;
                if (env != null) { 
                    try {
                        env.IsUnloaded();
                    } catch(AppDomainUnloadedException) {
                        env = null; 
                        _appDomainsShutdowdIds.Append("Un:" + appId + ":" + DateTime.UtcNow.ToShortTimeString() + ";");
                    } 
                } 
                if (env == null) {
                    env = CreateAppDomainWithHostingEnvironmentAndReportErrors(appId, appHost, hostingParameters); 
                    _appDomains[appId] = env;
                }
            }
 
            return env;
        } 
 
        private HostingEnvironment CreateAppDomainWithHostingEnvironmentAndReportErrors(
                                        String appId, 
                                        IApplicationHost appHost,
                                        HostingEnvironmentParameters hostingParameters) {
            try {
                return CreateAppDomainWithHostingEnvironment(appId, appHost, hostingParameters); 
            }
            catch (Exception e) { 
                Misc.ReportUnhandledException(e, new string[] {SR.GetString(SR.Failed_to_initialize_AppDomain), appId}); 
                throw;
            } 
        }

        private HostingEnvironment CreateAppDomainWithHostingEnvironment(
                                        String appId, 
                                        IApplicationHost appHost,
                                        HostingEnvironmentParameters hostingParameters) { 
 
            String physicalPath = appHost.GetPhysicalPath();
            if (!StringUtil.StringEndsWith(physicalPath, Path.DirectorySeparatorChar)) 
                physicalPath = physicalPath + Path.DirectorySeparatorChar;

            String domainId = ConstructAppDomainId(appId);
            String appName = (StringUtil.GetStringHashCode(String.Concat(appId.ToLower(CultureInfo.InvariantCulture), 
                physicalPath.ToLower(CultureInfo.InvariantCulture)))).ToString("x", CultureInfo.InvariantCulture);
            VirtualPath virtualPath = VirtualPath.Create(appHost.GetVirtualPath()); 
 
            Debug.Trace("AppManager", "CreateAppDomainWithHostingEnvironment, path=" + physicalPath + "; appId=" + appId + "; domainId=" + domainId);
 
            IDictionary bindings = new Hashtable(20);
            AppDomainSetup setup = new AppDomainSetup();
            PopulateDomainBindings(domainId, appId, appName, physicalPath, virtualPath, setup, bindings);
 
            //  Create the app domain
 
            AppDomain appDomain = null; 
            Exception appDomainCreationException = null;
 
            try {
                appDomain = AppDomain.CreateDomain(domainId,
#if FEATURE_PAL // FEATURE_PAL: hack to avoid non-supported hosting features
                                                   null, 
#else // FEATURE_PAL
                                                   GetDefaultDomainIdentity(), 
#endif // FEATURE_PAL 
                                                   setup);
 
                foreach (DictionaryEntry e in bindings)
                    appDomain.SetData((String)e.Key, (String)e.Value);
            }
            catch (Exception e) { 
                Debug.Trace("AppManager", "AppDomain.CreateDomain failed", e);
                appDomainCreationException = e; 
            } 

            if (appDomain == null) { 
                throw new SystemException(SR.GetString(SR.Cannot_create_AppDomain), appDomainCreationException);
            }

            // Create hosting environment in the new app domain 

            Type hostType = typeof(HostingEnvironment); 
            String module = hostType.Module.Assembly.FullName; 
            String typeName = hostType.FullName;
            ObjectHandle h = null; 

            // impersonate UNC identity, if any
            ImpersonationContext ictx = null;
            IntPtr uncToken = IntPtr.Zero; 

            // 
            // fetching config can fail due to a race with the 
            // native config reader
            // if that has happened, force a flush 
            //
            int maxRetries = 10;
            int numRetries = 0;
 
            while (numRetries < maxRetries) {
                try { 
                    uncToken = appHost.GetConfigToken(); 
                    // no throw, so break
                    break; 
                }
                catch (InvalidOperationException) {
                    numRetries++;
                    System.Threading.Thread.Sleep(250); 
                }
            } 
 

            if (uncToken != IntPtr.Zero) { 
                try {
                    ictx = new ImpersonationContext(uncToken);
                }
                catch { 
                }
                finally { 
                    UnsafeNativeMethods.CloseHandle(uncToken); 
                }
            } 

            try {

                // Create the hosting environment in the app domain 
#if DBG
                try { 
                    h = appDomain.CreateInstance(module, typeName); 
                }
                catch (Exception e) { 
                    Debug.Trace("AppManager", "appDomain.CreateInstance failed; identity=" + System.Security.Principal.WindowsIdentity.GetCurrent().Name, e);
                    throw;
                }
#else 
                h = appDomain.CreateInstance(module, typeName);
#endif 
            } 
            finally {
                // revert impersonation 
                if (ictx != null)
                    ictx.Undo();

                if (h == null) { 
                    AppDomain.Unload(appDomain);
                } 
            } 

            HostingEnvironment env = (h != null) ? h.Unwrap() as HostingEnvironment : null; 

            if (env == null)
                throw new SystemException(SR.GetString(SR.Cannot_create_HostEnv));
 
            // iniitalize the hosting environment
            IConfigMapPathFactory configMapPathFactory = appHost.GetConfigMapPathFactory(); 
            env.Initialize(this, appHost, configMapPathFactory, hostingParameters); 
            return env;
        } 

        private static void PopulateDomainBindings(String domainId, String appId, String appName,
                                                    String appPath, VirtualPath appVPath,
                                                    AppDomainSetup setup, IDictionary dict) { 
            // assembly loading settings
 
            // We put both the old and new bin dir names on the private bin path 
            setup.PrivateBinPathProbe   = "*";  // disable loading from app base
            setup.ShadowCopyFiles       = "true"; 
            setup.ApplicationBase       = appPath;
            setup.ApplicationName       = appName;
            setup.ConfigurationFile     = HttpConfigurationSystem.WebConfigFileName;
 
            // Disallow code download, since it's unreliable in services (ASURT 123836/127606)
            setup.DisallowCodeDownload  = true; 
 
            // internal settings
            dict.Add(".appDomain",     "*"); 
            dict.Add(".appId",         appId);
            dict.Add(".appPath",       appPath);
            dict.Add(".appVPath",      appVPath.VirtualPathString);
            dict.Add(".domainId",      domainId); 
        }
 
        private static Evidence GetDefaultDomainIdentity() { 
            Evidence     evidence      = new Evidence();
            bool         hasZone       = false; 
            IEnumerator  enumerator;

            enumerator = AppDomain.CurrentDomain.Evidence.GetHostEnumerator();
            while (enumerator.MoveNext()) { 
                if (enumerator.Current is Zone)
                    hasZone = true; 
                evidence.AddHost( enumerator.Current ); 
            }
 
            enumerator = AppDomain.CurrentDomain.Evidence.GetAssemblyEnumerator();
            while (enumerator.MoveNext()) {
                evidence.AddAssembly( enumerator.Current );
            } 

            //evidence.AddHost( new Url( "http://localhost/ASP_Plus" ) ); // 
            if (!hasZone) 
                evidence.AddHost( new Zone( SecurityZone.MyComputer ) );
 
            return evidence;
        }

        private static int s_domainCount = 0; 
        private static Object s_domainCountLock = new Object();
 
        private static String ConstructAppDomainId(String id) { 
            int domainCount = 0;
            lock (s_domainCountLock) { 
                domainCount = ++s_domainCount;
            }
            return id + "-" + domainCount.ToString(NumberFormatInfo.InvariantInfo) + "-" + DateTime.UtcNow.ToFileTime().ToString();
        } 
    }
} 
