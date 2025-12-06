//------------------------------------------------------------------------------ 
// <copyright file="ApplicationManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Hosting { 
    using System; 
    using System.Collections;
    using System.Configuration; 
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.Security.Permissions;
    using System.Web; 
    using System.Web.Configuration;
    using System.Web.Util; 
 

    [ComImport, Guid("0ccd465e-3114-4ca3-ad50-cea561307e93"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IProcessHost {
 
        void StartApplication(
                [In, MarshalAs(UnmanagedType.LPWStr)] 
                String appId, 
                [In, MarshalAs(UnmanagedType.LPWStr)]
                String appPath, 
                [MarshalAs(UnmanagedType.Interface)] out Object runtimeInterface);

        void ShutdownApplication([In, MarshalAs(UnmanagedType.LPWStr)] String appId);
 
        void Shutdown();
 
        void EnumerateAppDomains( [MarshalAs(UnmanagedType.Interface)] out IAppDomainInfoEnum appDomainInfoEnum); 

    } 

    //
    // App domain protocol manager
    // Note that this doesn't provide COM interop 
    //
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IAdphManager { 

        void StartAppDomainProtocolListenerChannel(
            [In, MarshalAs(UnmanagedType.LPWStr)] String appId,
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId, 
            IListenerChannelCallback listenerChannelCallback);
 
        void StopAppDomainProtocolListenerChannel( 
            [In, MarshalAs(UnmanagedType.LPWStr)] String appId,
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId, 
            int listenerChannelId,
            bool immediate);

        void StopAppDomainProtocol( 
            [In, MarshalAs(UnmanagedType.LPWStr)] String appId,
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId, 
            bool immediate); 
    }
 
    [ComImport, Guid("1cc9099d-0a8d-41cb-87d6-845e4f8c4e91"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IPphManager { 

        void StartProcessProtocolListenerChannel( 
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId, 
            IListenerChannelCallback listenerChannelCallback);
 
        void StopProcessProtocolListenerChannel(
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId,
            int listenerChannelId,
            bool immediate); 

        void StopProcessProtocol( 
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId, 
            bool immediate);
    } 


    [ComImport, Guid("9d98b251-453e-44f6-9cec-8b5aed970129"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IProcessHostIdleAndHealthCheck { 
 
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsIdle(); 

        void Ping(IProcessPingCallback callback);
    }
 

    [ComImport, Guid("5BC9C234-6CD7-49bf-A07A-6FDB7F22DFFF"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IAppDomainInfo { 
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetId();

        [return: MarshalAs(UnmanagedType.BStr)] 
        string GetVirtualPath();
 
        [return: MarshalAs(UnmanagedType.BStr)] 
        string GetPhysicalPath();
 
        [return: MarshalAs(UnmanagedType.I4)]
        int GetSiteId();

        [return: MarshalAs(UnmanagedType.Bool)] 
        bool IsIdle();
    } 
 
    [ComImport, Guid("F79648FB-558B-4a09-88F1-1E3BCB30E34F"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IAppDomainInfoEnum {
        [return: MarshalAs(UnmanagedType.Interface)]
        IAppDomainInfo GetData(); 

        [return: MarshalAs(UnmanagedType.I4)] 
        int Count(); 

        [return: MarshalAs(UnmanagedType.Bool)] 
        bool MoveNext();

        void Reset();
    } 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class AppDomainInfoEnum : IAppDomainInfoEnum
    { 
        private AppDomainInfo[] _appDomainInfos;
        private int _curPos;

        internal AppDomainInfoEnum(AppDomainInfo[] appDomainInfos) 
        {
            _appDomainInfos = appDomainInfos; 
            _curPos = -1; 
        }
 
        public int Count()
        {
            return _appDomainInfos.Length;
        } 

        public IAppDomainInfo GetData() 
        { 
            return _appDomainInfos[_curPos];
        } 

        public bool MoveNext()
        {
            _curPos++; 

            if (_curPos >= _appDomainInfos.Length) 
            { 
                return false;
            } 

            return true;
        }
 
        public void Reset()
        { 
            _curPos = -1; 
        }
    } 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class AppDomainInfo : IAppDomainInfo 
    {
        private string _id; 
        private string _virtualPath; 
        private string _physicalPath;
        private int _siteId; 
        private bool _isIdle;

        internal AppDomainInfo(string id, string vpath, string physPath, int siteId, bool isIdle)
        { 
            _id = id;
            _virtualPath = vpath; 
            _physicalPath = physPath; 
            _siteId = siteId;
            _isIdle = isIdle; 
        }

        public string GetId()
        { 
            return _id;
        } 
 
        public string GetVirtualPath()
        { 
            return _virtualPath;
        }

        public string GetPhysicalPath() 
        {
            return _physicalPath; 
        } 

        public int GetSiteId() 
        {
            return _siteId;
        }
 
        public bool IsIdle()
        { 
            return _isIdle; 
        }
    } 

    /// <include file='doc\ProcessHost.uex' path='docs/doc[@for="ProcessHost"]/*' />
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class ProcessHost : MarshalByRefObject, 
                                      IProcessHost,
                                      IAdphManager, // process protocol handlers manager 
                                      IPphManager,  // appdomain protocol handlers manager 
                                      IProcessHostIdleAndHealthCheck {
        private static Object _processHostStaticLock = new Object(); 
        private static ProcessHost _theProcessHost;

        private IProcessHostSupportFunctions _functions;
        private ApplicationManager _appManager; 
        private ProtocolsSection _protocolsConfig;
 
        // process protocol handlers by prot id 
        private Hashtable _protocolHandlers = new Hashtable();
 
        private ProtocolsSection ProtocolsConfig {
            get {
                if (_protocolsConfig == null) {
                    lock (this) { 
                        if (_protocolsConfig == null) {

                            if (HttpConfigurationSystem.IsSet) {
	                        _protocolsConfig = RuntimeConfig.GetRootWebConfig().Protocols; 
                            } else {
                                Configuration c = WebConfigurationManager.OpenWebConfiguration(null);
                                _protocolsConfig = (ProtocolsSection) c.GetSection("system.web/protocols");
                            }

                        } 
                    }
                } 
                return _protocolsConfig;
            }
        }
 
        // ctor only called via GetProcessHost
        [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Minimal)] 
        private ProcessHost(IProcessHostSupportFunctions functions) { 
            try {
                // remember support functions 
                _functions = functions;

                // pass them along to the HostingEnvironment in the default domain
                HostingEnvironment.SupportFunctions = functions; 

                // create singleton app manager 
                _appManager = ApplicationManager.GetApplicationManager(); 

            } 
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] 
                                                  { SR.GetString(SR.Cant_Create_Process_Host)});
                    Debug.Trace("internal", "ProcessHost::ctor failed with " + e.GetType().FullName + ": " + e.Message + "\r\n" + e.StackTrace); 
                }
                throw;
            }
        } 

 
        // ValidateType 
        //
        // Validate and Get the Type that is sent in 
        //
        // Note: Because ProtocolElement is outside of our assembly we need to do
        //       that here, and because of that we need to hardcode the property
        //       names!! 
        //
        private Type ValidateAndGetType( ProtocolElement element, 
                                         string          typeName, 
                                         Type            assignableType,
                                         string          elementPropertyName ) { 
            Type handlerType;

            try {
                 handlerType = Type.GetType(typeName, true /*throwOnError*/); 
            }
            catch (Exception e) { 
 
                PropertyInformation propInfo = null;
                string source = String.Empty; 
                int lineNum = 0;

                if (element != null  && null != element.ElementInformation) {
                    propInfo = element.ElementInformation.Properties[elementPropertyName]; 

                    if (null != propInfo) { 
                        source = propInfo.Source; 
                        lineNum = propInfo.LineNumber;
                    } 

                }

                throw new ConfigurationErrorsException( 
                            e.Message,
                            e, 
                            source, 
                            lineNum);
            } 

            ConfigUtil.CheckAssignableType( assignableType, handlerType, element, elementPropertyName);

            return handlerType; 
        }
 
        private Type GetAppDomainProtocolHandlerType(String protocolId) { 
            Type t = null;
 
            try {
                // get app domaoin protocol handler type from config
                ProtocolElement configEntry = ProtocolsConfig.Protocols[protocolId];
                if (configEntry == null) 
                    throw new ArgumentException(SR.GetString(SR.Unknown_protocol_id, protocolId));
 
                    t = ValidateAndGetType( configEntry, 
                                       configEntry.AppDomainHandlerType,
                                       typeof(AppDomainProtocolHandler), 
                                       "AppDomainHandlerType" );
            }
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Invalid_AppDomain_Prot_Type)} ); 
                } 
            }
 
            return t;
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
        public override Object InitializeLifetimeService() {
            return null; // never expire lease 
        } 

        // called from ProcessHostFactoryHelper to get ProcessHost 
        internal static ProcessHost GetProcessHost(IProcessHostSupportFunctions functions) {
            if (_theProcessHost == null) {
                lock (_processHostStaticLock) {
                    if (_theProcessHost == null) { 
                        _theProcessHost = new ProcessHost(functions);
                    } 
                } 
            }
 
            return _theProcessHost;
        }

        internal static ProcessHost DefaultHost { 
            get {
                return _theProcessHost; // may be null 
            } 
        }
 
        internal IProcessHostSupportFunctions SupportFunctions {
            get {
                return _functions;
            } 
        }
 
        // 
        // IProcessHostProcessProtocolManager interface implementation
        // 

        // starts process protocol handler on demand
        public void StartProcessProtocolListenerChannel(String protocolId, IListenerChannelCallback listenerChannelCallback) {
            try { 
                if (protocolId == null)
                    throw new ArgumentNullException("protocolId"); 
 
                // validate protocol id
                ProtocolElement configEntry = ProtocolsConfig.Protocols[protocolId]; 
                if (configEntry == null)
                    throw new ArgumentException(SR.GetString(SR.Unknown_protocol_id, protocolId));

                ProcessProtocolHandler protocolHandler = null; 
                Type                   protocolHandlerType = null;
 
                protocolHandlerType = ValidateAndGetType( configEntry, 
                                                          configEntry.ProcessHandlerType,
                                                          typeof(ProcessProtocolHandler), 
                                                          "ProcessHandlerType" );

                lock (this) {
                    // lookup or create protocol handler 
                    protocolHandler = _protocolHandlers[protocolId] as ProcessProtocolHandler;
 
                    if (protocolHandler == null) { 
                        protocolHandler = (ProcessProtocolHandler)Activator.CreateInstance(protocolHandlerType);
                        _protocolHandlers[protocolId] = protocolHandler; 
                    }

                }
 
                // call the handler to start listenerChannel
                if (protocolHandler != null) { 
                    protocolHandler.StartListenerChannel(listenerChannelCallback, this); 
                }
            } 
            catch (Exception e) {
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Invalid_Process_Prot_Type)} ); 
                }
                throw; 
            } 
        }
 
        public void StopProcessProtocolListenerChannel(String protocolId, int listenerChannelId, bool immediate) {
            try {
                if (protocolId == null)
                    throw new ArgumentNullException("protocolId"); 

                ProcessProtocolHandler protocolHandler = null; 
 
                lock (this) {
                    // lookup protocol handler 
                    protocolHandler = _protocolHandlers[protocolId] as ProcessProtocolHandler;
                }

                // call the handler to stop listenerChannel 
                if (protocolHandler != null) {
                    protocolHandler.StopListenerChannel(listenerChannelId, immediate); 
                } 
            }
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Failure_Stop_Listener_Channel)} );
                } 
                throw;
            } 
        } 

 
        public void StopProcessProtocol(String protocolId, bool immediate) {
            try {
                if (protocolId == null)
                    throw new ArgumentNullException("protocolId"); 

                ProcessProtocolHandler protocolHandler = null; 
 
                lock (this) {
                    // lookup and remove protocol handler 
                    protocolHandler = _protocolHandlers[protocolId] as ProcessProtocolHandler;

                    if (protocolHandler != null) {
                        _protocolHandlers.Remove(protocolId); 
                    }
                } 
 
                if (protocolHandler != null) {
                    protocolHandler.StopProtocol(immediate); 
                }
            }
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Failure_Stop_Process_Prot)} ); 
                } 
                throw;
            } 
        }

        //
        // IAppDomainProtocolManager 
        //
 
        // starts app domain protocol handler on demand (called by process protocol handler 

        public void StartAppDomainProtocolListenerChannel(String appId, String protocolId, IListenerChannelCallback listenerChannelCallback) { 
            try {
                if (appId == null)
                    throw new ArgumentNullException("appId");
                if (protocolId == null) 
                    throw new ArgumentNullException("protocolId");
 
                ISAPIApplicationHost appHost = CreateAppHost(appId, null); 

                // get app domaoin protocol handler type from config 
                Type handlerType = GetAppDomainProtocolHandlerType(protocolId);

                AppDomainProtocolHandler handler = null;
 
                lock (_appManager) {
                    HostingEnvironmentParameters hostingParameters = new HostingEnvironmentParameters(); 
                    hostingParameters.HostingFlags = HostingEnvironmentFlags.ThrowHostingInitErrors; 

                    // call app manager to create the handler 
                    handler = (AppDomainProtocolHandler)_appManager.CreateObjectInternal(
                        appId, handlerType, appHost, false /*failIfExists*/,
                        hostingParameters);
 
                    // create a shim object that we can use for proxy unwrapping
                    ListenerAdapterDispatchShim shim = (ListenerAdapterDispatchShim) 
                        _appManager.CreateObjectInternal( 
                            appId, typeof(ListenerAdapterDispatchShim), appHost, false /*failIfExists*/,
                            hostingParameters); 

                    if (null != shim) {
                        shim.StartListenerChannel(handler, listenerChannelCallback);
 
                        // remove the shim
                        ((IRegisteredObject)shim).Stop(true); 
                    } 
                    else {
                        throw new HttpException(SR.GetString(SR.Failure_Create_Listener_Shim)); 
                    }
                }
            }
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_Start_AppDomain_Listener)} ); 
                }
                throw; 
            }
        }

        public void StopAppDomainProtocolListenerChannel(String appId, String protocolId, int listenerChannelId, bool immediate) { 
            try {
                if (appId == null) 
                    throw new ArgumentNullException("appId"); 
                if (protocolId == null)
                    throw new ArgumentNullException("protocolId"); 

                // get app domaoin protocol handler type from config
                Type handlerType = GetAppDomainProtocolHandlerType(protocolId);
 
                AppDomainProtocolHandler handler = null;
 
                lock (_appManager) { 
                    // call app manager to create the handler
                    handler = (AppDomainProtocolHandler)_appManager.GetObject(appId, handlerType); 
                }

                // stop the listenerChannel
                if (handler != null) { 
                    handler.StopListenerChannel(listenerChannelId, immediate);
                } 
            } 
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Failure_Stop_AppDomain_Listener)} );
                }
                throw; 
            }
        } 
 

        public void StopAppDomainProtocol(String appId, String protocolId, bool immediate) { 
            try {
                if (appId == null)
                    throw new ArgumentNullException("appId");
                if (protocolId == null) 
                    throw new ArgumentNullException("protocolId");
 
                // get app domaoin protocol handler type from config 
                Type handlerType = GetAppDomainProtocolHandlerType(protocolId);
 
                AppDomainProtocolHandler handler = null;

                lock (_appManager) {
                    // call app manager to create the handler 
                    handler = (AppDomainProtocolHandler)_appManager.GetObject(appId, handlerType);
                } 
 
                // stop protocol
                if (handler != null) { 
                    handler.StopProtocol(immediate);
                }
            }
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_Stop_AppDomain_Protocol)} ); 
                }
                throw; 
            }
        }

        public void StartApplication(String appId, String appPath, out Object runtimeInterface) 
        {
            try { 
                if (appId == null) 
                    throw new ArgumentNullException("appId");
                if (appPath == null) 
                    throw new ArgumentNullException("appPath");

                Debug.Assert(_functions != null, "_functions != null");
 
                runtimeInterface = null;
 
                PipelineRuntime runtime = null; 

                // 
                //  Fill app a Dictionary with 'binding rules' -- name value string pairs
                //  for app domain creation
                //
 
                //
 
 
                if (appPath[0] == '.') {
                    System.IO.FileInfo file = new System.IO.FileInfo(appPath); 
                    appPath = file.FullName;
                }

                if (!StringUtil.StringEndsWith(appPath, '\\')) { 
                    appPath = appPath + "\\";
                } 
 
                // Create new app host of a consistent type
                IApplicationHost appHost = CreateAppHost(appId, appPath); 

                //
                // under lock, create the AppDomain and a registered object in it
                // 
                lock (_appManager) {
 
                    // #1 WOS 1690249: ASP.Net v2.0: ASP.NET stress: 2nd chance exception: Attempted to access an unloaded AppDomain. 
                    // if an old AppDomain exists with a PipelineRuntime, remove it from
                    // AppManager._appDomains so that a new AppDomain will be created 
                    // #2 WOS 1977425: ASP.NET apps continue recycling after touching machine.config once - this used to initiate shutdown,
                    // but that can cause us to recycle the app repeatedly if we initiate shutdown before IIS initiates shutdown of the
                    // previous app.
                    _appManager.RemoveFromTableIfRuntimeExists(appId, typeof(PipelineRuntime)); 

                    try { 
                        runtime = (PipelineRuntime)_appManager.CreateObjectInternal( 
                            appId,
                            typeof(PipelineRuntime), 
                            appHost,
                            true /* failIfExists */,
                            null /* default */ );
                    } 
                    catch(AppDomainUnloadedException) {
                        // munch it so we can retry again 
                    } 

                    if (null != runtime) { 
                        runtime.SetThisAppDomainsIsapiAppId(appId);
                        runtime.StartProcessing();
                        runtimeInterface = new ObjectHandle(runtime);
                    } 
                }
            } 
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_Start_Integrated_App)} );
                }
                throw;
            } 
        }
 
 
        public void ShutdownApplication(String appId) {
            try { 
                // call into app manager
                _appManager.ShutdownApplication(appId);
            }
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_Stop_Integrated_App)} ); 
                }
                throw; 
            }
        }

        public void Shutdown() { 
            try {
                // collect all protocols under lock 
                ArrayList protocolList = new ArrayList(); 
                int       refCount = 0;
 
                lock (this) {
                    // lookup protocol handler
                    foreach (DictionaryEntry e in _protocolHandlers) {
                        protocolList.Add(e.Value); 
                    }
 
                    _protocolHandlers = new Hashtable(); 
                }
 
                // stop all process protocols outside of lock
                foreach (ProcessProtocolHandler p in protocolList) {
                    p.StopProtocol(true);
                } 

                // call into app manager to shutdown 
                _appManager.ShutdownAll(); 

 
                // SupportFunctions interface provided by native layer
                // must be released now.
                // Otherwise the release of the COM object will have
                // to wait for GC. Native layer assumes that after 
                // returning from Shutdown there is no reference
                // to the native objects from ProcessHost. 
                // 
                do {
                    refCount = Marshal.ReleaseComObject( _functions ); 
                } while( refCount != 0 );

            }
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_Shutdown_ProcessHost), e.ToString()} ); 
                }
                throw; 
            }
        }

        public void EnumerateAppDomains( out IAppDomainInfoEnum appDomainInfoEnum ) 
        {
            try { 
                ApplicationManager appManager = ApplicationManager.GetApplicationManager(); 
                AppDomainInfo [] infos;
 
                infos = appManager.GetAppDomainInfos();

                appDomainInfoEnum = new AppDomainInfoEnum(infos);
            } 
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_AppDomain_Enum)} );
                } 
                throw;
            }
        }
 
        // IProcessHostIdleAndHealthCheck interface implementation
        public bool IsIdle() { 
            bool result = false; 

            try { 
                result = _appManager.IsIdle();
            }
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Failure_PMH_Idle)} ); 
                } 
                throw;
            } 

            return result;
        }
 

        public void Ping(IProcessPingCallback callback) { 
            try { 
                if (callback != null)
                    _appManager.Ping(callback); 
            }
            catch (Exception e) {
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_PMH_Ping)} );
                } 
                throw; 
            }
        } 

        private ISAPIApplicationHost CreateAppHost(string appId, string appPath) {

            // 
            // if we have a null physical path, we need
            // to use the PMH to resolve it 
            // 
            if (String.IsNullOrEmpty(appPath)) {
                string virtualPath; 
                string physicalPath;
                string siteName;
                string siteID;
 
                _functions.GetApplicationProperties(
                        appId, 
                        out virtualPath, 
                        out physicalPath,
                        out siteName, 
                        out siteID);

                //
                // make sure physical app path ends with '\\' and virtual does not 
                //
                if (!StringUtil.StringEndsWith(physicalPath, '\\')) { 
                    physicalPath = physicalPath + "\\"; 
                }
 
                Debug.Assert( !String.IsNullOrEmpty(physicalPath), "!String.IsNullOrEmpty(physicalPath)");
                appPath = physicalPath;
            }
 
            //
            // Create a new application host 
            // This needs to be a coherent type across all 
            // protocol types so that we get a consistent
            // environment regardless of which protocol initializes first 
            //
            ISAPIApplicationHost appHost = new
                ISAPIApplicationHost(
                        appId, 
                        appPath,
                        false, /* validatePhysicalPath */ 
                        _functions 
                        );
 

            return appHost;
        }
    } 

    internal sealed class ListenerAdapterDispatchShim : MarshalByRefObject, IRegisteredObject { 
 
        void IRegisteredObject.Stop(bool immediate) {
            HostingEnvironment.UnregisterObject(this); 
        }

        // this should run in an Hosted app domain (not in the default domain)
        internal void StartListenerChannel( AppDomainProtocolHandler handler, IListenerChannelCallback listenerCallback ) { 
            Debug.Assert( HostingEnvironment.IsHosted, "HostingEnvironment.IsHosted" );
            Debug.Assert( null != handler, "null != handler" ); 
 
            IListenerChannelCallback unwrappedProxy = MarshalComProxy(listenerCallback);
 
            Debug.Assert(null != unwrappedProxy, "null != unwrappedProxy");
            if (null != unwrappedProxy && null != handler) {
                handler.StartListenerChannel(unwrappedProxy);
            } 
        }
 
        internal IListenerChannelCallback MarshalComProxy(IListenerChannelCallback defaultDomainCallback) { 
            IListenerChannelCallback localProxy = null;
 
            // get the underlying COM object
            IntPtr pUnk = Marshal.GetIUnknownForObject(defaultDomainCallback);

            // this object isn't a COM object 
            if (IntPtr.Zero == pUnk) {
                return null; 
            } 

            IntPtr ppv = IntPtr.Zero; 
            try {
                // QI it for the interface
                Guid g = typeof(IListenerChannelCallback).GUID;
 
                int hresult = Marshal.QueryInterface(pUnk, ref g, out ppv);
                if (hresult < 0)  { 
                    Marshal.ThrowExceptionForHR(hresult); 
                }
 
                // create a RCW we can hold onto in this domain
                // this bumps the ref count so we can drop our refs on the raw interfaces
                localProxy = (IListenerChannelCallback)Marshal.GetObjectForIUnknown(ppv);
            } 
            finally {
                // drop our explicit refs and keep the managed instance 
                if (IntPtr.Zero != ppv) { 
                    Marshal.Release(ppv);
                } 
                if (IntPtr.Zero != pUnk) {
                    Marshal.Release(pUnk);
                }
            } 

            return localProxy; 
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
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.Security.Permissions;
    using System.Web; 
    using System.Web.Configuration;
    using System.Web.Util; 
 

    [ComImport, Guid("0ccd465e-3114-4ca3-ad50-cea561307e93"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IProcessHost {
 
        void StartApplication(
                [In, MarshalAs(UnmanagedType.LPWStr)] 
                String appId, 
                [In, MarshalAs(UnmanagedType.LPWStr)]
                String appPath, 
                [MarshalAs(UnmanagedType.Interface)] out Object runtimeInterface);

        void ShutdownApplication([In, MarshalAs(UnmanagedType.LPWStr)] String appId);
 
        void Shutdown();
 
        void EnumerateAppDomains( [MarshalAs(UnmanagedType.Interface)] out IAppDomainInfoEnum appDomainInfoEnum); 

    } 

    //
    // App domain protocol manager
    // Note that this doesn't provide COM interop 
    //
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IAdphManager { 

        void StartAppDomainProtocolListenerChannel(
            [In, MarshalAs(UnmanagedType.LPWStr)] String appId,
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId, 
            IListenerChannelCallback listenerChannelCallback);
 
        void StopAppDomainProtocolListenerChannel( 
            [In, MarshalAs(UnmanagedType.LPWStr)] String appId,
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId, 
            int listenerChannelId,
            bool immediate);

        void StopAppDomainProtocol( 
            [In, MarshalAs(UnmanagedType.LPWStr)] String appId,
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId, 
            bool immediate); 
    }
 
    [ComImport, Guid("1cc9099d-0a8d-41cb-87d6-845e4f8c4e91"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IPphManager { 

        void StartProcessProtocolListenerChannel( 
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId, 
            IListenerChannelCallback listenerChannelCallback);
 
        void StopProcessProtocolListenerChannel(
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId,
            int listenerChannelId,
            bool immediate); 

        void StopProcessProtocol( 
            [In, MarshalAs(UnmanagedType.LPWStr)] String protocolId, 
            bool immediate);
    } 


    [ComImport, Guid("9d98b251-453e-44f6-9cec-8b5aed970129"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IProcessHostIdleAndHealthCheck { 
 
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsIdle(); 

        void Ping(IProcessPingCallback callback);
    }
 

    [ComImport, Guid("5BC9C234-6CD7-49bf-A07A-6FDB7F22DFFF"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IAppDomainInfo { 
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetId();

        [return: MarshalAs(UnmanagedType.BStr)] 
        string GetVirtualPath();
 
        [return: MarshalAs(UnmanagedType.BStr)] 
        string GetPhysicalPath();
 
        [return: MarshalAs(UnmanagedType.I4)]
        int GetSiteId();

        [return: MarshalAs(UnmanagedType.Bool)] 
        bool IsIdle();
    } 
 
    [ComImport, Guid("F79648FB-558B-4a09-88F1-1E3BCB30E34F"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public interface IAppDomainInfoEnum {
        [return: MarshalAs(UnmanagedType.Interface)]
        IAppDomainInfo GetData(); 

        [return: MarshalAs(UnmanagedType.I4)] 
        int Count(); 

        [return: MarshalAs(UnmanagedType.Bool)] 
        bool MoveNext();

        void Reset();
    } 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class AppDomainInfoEnum : IAppDomainInfoEnum
    { 
        private AppDomainInfo[] _appDomainInfos;
        private int _curPos;

        internal AppDomainInfoEnum(AppDomainInfo[] appDomainInfos) 
        {
            _appDomainInfos = appDomainInfos; 
            _curPos = -1; 
        }
 
        public int Count()
        {
            return _appDomainInfos.Length;
        } 

        public IAppDomainInfo GetData() 
        { 
            return _appDomainInfos[_curPos];
        } 

        public bool MoveNext()
        {
            _curPos++; 

            if (_curPos >= _appDomainInfos.Length) 
            { 
                return false;
            } 

            return true;
        }
 
        public void Reset()
        { 
            _curPos = -1; 
        }
    } 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class AppDomainInfo : IAppDomainInfo 
    {
        private string _id; 
        private string _virtualPath; 
        private string _physicalPath;
        private int _siteId; 
        private bool _isIdle;

        internal AppDomainInfo(string id, string vpath, string physPath, int siteId, bool isIdle)
        { 
            _id = id;
            _virtualPath = vpath; 
            _physicalPath = physPath; 
            _siteId = siteId;
            _isIdle = isIdle; 
        }

        public string GetId()
        { 
            return _id;
        } 
 
        public string GetVirtualPath()
        { 
            return _virtualPath;
        }

        public string GetPhysicalPath() 
        {
            return _physicalPath; 
        } 

        public int GetSiteId() 
        {
            return _siteId;
        }
 
        public bool IsIdle()
        { 
            return _isIdle; 
        }
    } 

    /// <include file='doc\ProcessHost.uex' path='docs/doc[@for="ProcessHost"]/*' />
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class ProcessHost : MarshalByRefObject, 
                                      IProcessHost,
                                      IAdphManager, // process protocol handlers manager 
                                      IPphManager,  // appdomain protocol handlers manager 
                                      IProcessHostIdleAndHealthCheck {
        private static Object _processHostStaticLock = new Object(); 
        private static ProcessHost _theProcessHost;

        private IProcessHostSupportFunctions _functions;
        private ApplicationManager _appManager; 
        private ProtocolsSection _protocolsConfig;
 
        // process protocol handlers by prot id 
        private Hashtable _protocolHandlers = new Hashtable();
 
        private ProtocolsSection ProtocolsConfig {
            get {
                if (_protocolsConfig == null) {
                    lock (this) { 
                        if (_protocolsConfig == null) {

                            if (HttpConfigurationSystem.IsSet) {
	                        _protocolsConfig = RuntimeConfig.GetRootWebConfig().Protocols; 
                            } else {
                                Configuration c = WebConfigurationManager.OpenWebConfiguration(null);
                                _protocolsConfig = (ProtocolsSection) c.GetSection("system.web/protocols");
                            }

                        } 
                    }
                } 
                return _protocolsConfig;
            }
        }
 
        // ctor only called via GetProcessHost
        [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Minimal)] 
        private ProcessHost(IProcessHostSupportFunctions functions) { 
            try {
                // remember support functions 
                _functions = functions;

                // pass them along to the HostingEnvironment in the default domain
                HostingEnvironment.SupportFunctions = functions; 

                // create singleton app manager 
                _appManager = ApplicationManager.GetApplicationManager(); 

            } 
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] 
                                                  { SR.GetString(SR.Cant_Create_Process_Host)});
                    Debug.Trace("internal", "ProcessHost::ctor failed with " + e.GetType().FullName + ": " + e.Message + "\r\n" + e.StackTrace); 
                }
                throw;
            }
        } 

 
        // ValidateType 
        //
        // Validate and Get the Type that is sent in 
        //
        // Note: Because ProtocolElement is outside of our assembly we need to do
        //       that here, and because of that we need to hardcode the property
        //       names!! 
        //
        private Type ValidateAndGetType( ProtocolElement element, 
                                         string          typeName, 
                                         Type            assignableType,
                                         string          elementPropertyName ) { 
            Type handlerType;

            try {
                 handlerType = Type.GetType(typeName, true /*throwOnError*/); 
            }
            catch (Exception e) { 
 
                PropertyInformation propInfo = null;
                string source = String.Empty; 
                int lineNum = 0;

                if (element != null  && null != element.ElementInformation) {
                    propInfo = element.ElementInformation.Properties[elementPropertyName]; 

                    if (null != propInfo) { 
                        source = propInfo.Source; 
                        lineNum = propInfo.LineNumber;
                    } 

                }

                throw new ConfigurationErrorsException( 
                            e.Message,
                            e, 
                            source, 
                            lineNum);
            } 

            ConfigUtil.CheckAssignableType( assignableType, handlerType, element, elementPropertyName);

            return handlerType; 
        }
 
        private Type GetAppDomainProtocolHandlerType(String protocolId) { 
            Type t = null;
 
            try {
                // get app domaoin protocol handler type from config
                ProtocolElement configEntry = ProtocolsConfig.Protocols[protocolId];
                if (configEntry == null) 
                    throw new ArgumentException(SR.GetString(SR.Unknown_protocol_id, protocolId));
 
                    t = ValidateAndGetType( configEntry, 
                                       configEntry.AppDomainHandlerType,
                                       typeof(AppDomainProtocolHandler), 
                                       "AppDomainHandlerType" );
            }
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Invalid_AppDomain_Prot_Type)} ); 
                } 
            }
 
            return t;
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
        public override Object InitializeLifetimeService() {
            return null; // never expire lease 
        } 

        // called from ProcessHostFactoryHelper to get ProcessHost 
        internal static ProcessHost GetProcessHost(IProcessHostSupportFunctions functions) {
            if (_theProcessHost == null) {
                lock (_processHostStaticLock) {
                    if (_theProcessHost == null) { 
                        _theProcessHost = new ProcessHost(functions);
                    } 
                } 
            }
 
            return _theProcessHost;
        }

        internal static ProcessHost DefaultHost { 
            get {
                return _theProcessHost; // may be null 
            } 
        }
 
        internal IProcessHostSupportFunctions SupportFunctions {
            get {
                return _functions;
            } 
        }
 
        // 
        // IProcessHostProcessProtocolManager interface implementation
        // 

        // starts process protocol handler on demand
        public void StartProcessProtocolListenerChannel(String protocolId, IListenerChannelCallback listenerChannelCallback) {
            try { 
                if (protocolId == null)
                    throw new ArgumentNullException("protocolId"); 
 
                // validate protocol id
                ProtocolElement configEntry = ProtocolsConfig.Protocols[protocolId]; 
                if (configEntry == null)
                    throw new ArgumentException(SR.GetString(SR.Unknown_protocol_id, protocolId));

                ProcessProtocolHandler protocolHandler = null; 
                Type                   protocolHandlerType = null;
 
                protocolHandlerType = ValidateAndGetType( configEntry, 
                                                          configEntry.ProcessHandlerType,
                                                          typeof(ProcessProtocolHandler), 
                                                          "ProcessHandlerType" );

                lock (this) {
                    // lookup or create protocol handler 
                    protocolHandler = _protocolHandlers[protocolId] as ProcessProtocolHandler;
 
                    if (protocolHandler == null) { 
                        protocolHandler = (ProcessProtocolHandler)Activator.CreateInstance(protocolHandlerType);
                        _protocolHandlers[protocolId] = protocolHandler; 
                    }

                }
 
                // call the handler to start listenerChannel
                if (protocolHandler != null) { 
                    protocolHandler.StartListenerChannel(listenerChannelCallback, this); 
                }
            } 
            catch (Exception e) {
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Invalid_Process_Prot_Type)} ); 
                }
                throw; 
            } 
        }
 
        public void StopProcessProtocolListenerChannel(String protocolId, int listenerChannelId, bool immediate) {
            try {
                if (protocolId == null)
                    throw new ArgumentNullException("protocolId"); 

                ProcessProtocolHandler protocolHandler = null; 
 
                lock (this) {
                    // lookup protocol handler 
                    protocolHandler = _protocolHandlers[protocolId] as ProcessProtocolHandler;
                }

                // call the handler to stop listenerChannel 
                if (protocolHandler != null) {
                    protocolHandler.StopListenerChannel(listenerChannelId, immediate); 
                } 
            }
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Failure_Stop_Listener_Channel)} );
                } 
                throw;
            } 
        } 

 
        public void StopProcessProtocol(String protocolId, bool immediate) {
            try {
                if (protocolId == null)
                    throw new ArgumentNullException("protocolId"); 

                ProcessProtocolHandler protocolHandler = null; 
 
                lock (this) {
                    // lookup and remove protocol handler 
                    protocolHandler = _protocolHandlers[protocolId] as ProcessProtocolHandler;

                    if (protocolHandler != null) {
                        _protocolHandlers.Remove(protocolId); 
                    }
                } 
 
                if (protocolHandler != null) {
                    protocolHandler.StopProtocol(immediate); 
                }
            }
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Failure_Stop_Process_Prot)} ); 
                } 
                throw;
            } 
        }

        //
        // IAppDomainProtocolManager 
        //
 
        // starts app domain protocol handler on demand (called by process protocol handler 

        public void StartAppDomainProtocolListenerChannel(String appId, String protocolId, IListenerChannelCallback listenerChannelCallback) { 
            try {
                if (appId == null)
                    throw new ArgumentNullException("appId");
                if (protocolId == null) 
                    throw new ArgumentNullException("protocolId");
 
                ISAPIApplicationHost appHost = CreateAppHost(appId, null); 

                // get app domaoin protocol handler type from config 
                Type handlerType = GetAppDomainProtocolHandlerType(protocolId);

                AppDomainProtocolHandler handler = null;
 
                lock (_appManager) {
                    HostingEnvironmentParameters hostingParameters = new HostingEnvironmentParameters(); 
                    hostingParameters.HostingFlags = HostingEnvironmentFlags.ThrowHostingInitErrors; 

                    // call app manager to create the handler 
                    handler = (AppDomainProtocolHandler)_appManager.CreateObjectInternal(
                        appId, handlerType, appHost, false /*failIfExists*/,
                        hostingParameters);
 
                    // create a shim object that we can use for proxy unwrapping
                    ListenerAdapterDispatchShim shim = (ListenerAdapterDispatchShim) 
                        _appManager.CreateObjectInternal( 
                            appId, typeof(ListenerAdapterDispatchShim), appHost, false /*failIfExists*/,
                            hostingParameters); 

                    if (null != shim) {
                        shim.StartListenerChannel(handler, listenerChannelCallback);
 
                        // remove the shim
                        ((IRegisteredObject)shim).Stop(true); 
                    } 
                    else {
                        throw new HttpException(SR.GetString(SR.Failure_Create_Listener_Shim)); 
                    }
                }
            }
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_Start_AppDomain_Listener)} ); 
                }
                throw; 
            }
        }

        public void StopAppDomainProtocolListenerChannel(String appId, String protocolId, int listenerChannelId, bool immediate) { 
            try {
                if (appId == null) 
                    throw new ArgumentNullException("appId"); 
                if (protocolId == null)
                    throw new ArgumentNullException("protocolId"); 

                // get app domaoin protocol handler type from config
                Type handlerType = GetAppDomainProtocolHandlerType(protocolId);
 
                AppDomainProtocolHandler handler = null;
 
                lock (_appManager) { 
                    // call app manager to create the handler
                    handler = (AppDomainProtocolHandler)_appManager.GetObject(appId, handlerType); 
                }

                // stop the listenerChannel
                if (handler != null) { 
                    handler.StopListenerChannel(listenerChannelId, immediate);
                } 
            } 
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Failure_Stop_AppDomain_Listener)} );
                }
                throw; 
            }
        } 
 

        public void StopAppDomainProtocol(String appId, String protocolId, bool immediate) { 
            try {
                if (appId == null)
                    throw new ArgumentNullException("appId");
                if (protocolId == null) 
                    throw new ArgumentNullException("protocolId");
 
                // get app domaoin protocol handler type from config 
                Type handlerType = GetAppDomainProtocolHandlerType(protocolId);
 
                AppDomainProtocolHandler handler = null;

                lock (_appManager) {
                    // call app manager to create the handler 
                    handler = (AppDomainProtocolHandler)_appManager.GetObject(appId, handlerType);
                } 
 
                // stop protocol
                if (handler != null) { 
                    handler.StopProtocol(immediate);
                }
            }
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_Stop_AppDomain_Protocol)} ); 
                }
                throw; 
            }
        }

        public void StartApplication(String appId, String appPath, out Object runtimeInterface) 
        {
            try { 
                if (appId == null) 
                    throw new ArgumentNullException("appId");
                if (appPath == null) 
                    throw new ArgumentNullException("appPath");

                Debug.Assert(_functions != null, "_functions != null");
 
                runtimeInterface = null;
 
                PipelineRuntime runtime = null; 

                // 
                //  Fill app a Dictionary with 'binding rules' -- name value string pairs
                //  for app domain creation
                //
 
                //
 
 
                if (appPath[0] == '.') {
                    System.IO.FileInfo file = new System.IO.FileInfo(appPath); 
                    appPath = file.FullName;
                }

                if (!StringUtil.StringEndsWith(appPath, '\\')) { 
                    appPath = appPath + "\\";
                } 
 
                // Create new app host of a consistent type
                IApplicationHost appHost = CreateAppHost(appId, appPath); 

                //
                // under lock, create the AppDomain and a registered object in it
                // 
                lock (_appManager) {
 
                    // #1 WOS 1690249: ASP.Net v2.0: ASP.NET stress: 2nd chance exception: Attempted to access an unloaded AppDomain. 
                    // if an old AppDomain exists with a PipelineRuntime, remove it from
                    // AppManager._appDomains so that a new AppDomain will be created 
                    // #2 WOS 1977425: ASP.NET apps continue recycling after touching machine.config once - this used to initiate shutdown,
                    // but that can cause us to recycle the app repeatedly if we initiate shutdown before IIS initiates shutdown of the
                    // previous app.
                    _appManager.RemoveFromTableIfRuntimeExists(appId, typeof(PipelineRuntime)); 

                    try { 
                        runtime = (PipelineRuntime)_appManager.CreateObjectInternal( 
                            appId,
                            typeof(PipelineRuntime), 
                            appHost,
                            true /* failIfExists */,
                            null /* default */ );
                    } 
                    catch(AppDomainUnloadedException) {
                        // munch it so we can retry again 
                    } 

                    if (null != runtime) { 
                        runtime.SetThisAppDomainsIsapiAppId(appId);
                        runtime.StartProcessing();
                        runtimeInterface = new ObjectHandle(runtime);
                    } 
                }
            } 
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_Start_Integrated_App)} );
                }
                throw;
            } 
        }
 
 
        public void ShutdownApplication(String appId) {
            try { 
                // call into app manager
                _appManager.ShutdownApplication(appId);
            }
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_Stop_Integrated_App)} ); 
                }
                throw; 
            }
        }

        public void Shutdown() { 
            try {
                // collect all protocols under lock 
                ArrayList protocolList = new ArrayList(); 
                int       refCount = 0;
 
                lock (this) {
                    // lookup protocol handler
                    foreach (DictionaryEntry e in _protocolHandlers) {
                        protocolList.Add(e.Value); 
                    }
 
                    _protocolHandlers = new Hashtable(); 
                }
 
                // stop all process protocols outside of lock
                foreach (ProcessProtocolHandler p in protocolList) {
                    p.StopProtocol(true);
                } 

                // call into app manager to shutdown 
                _appManager.ShutdownAll(); 

 
                // SupportFunctions interface provided by native layer
                // must be released now.
                // Otherwise the release of the COM object will have
                // to wait for GC. Native layer assumes that after 
                // returning from Shutdown there is no reference
                // to the native objects from ProcessHost. 
                // 
                do {
                    refCount = Marshal.ReleaseComObject( _functions ); 
                } while( refCount != 0 );

            }
            catch (Exception e) { 
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_Shutdown_ProcessHost), e.ToString()} ); 
                }
                throw; 
            }
        }

        public void EnumerateAppDomains( out IAppDomainInfoEnum appDomainInfoEnum ) 
        {
            try { 
                ApplicationManager appManager = ApplicationManager.GetApplicationManager(); 
                AppDomainInfo [] infos;
 
                infos = appManager.GetAppDomainInfos();

                appDomainInfoEnum = new AppDomainInfoEnum(infos);
            } 
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_AppDomain_Enum)} );
                } 
                throw;
            }
        }
 
        // IProcessHostIdleAndHealthCheck interface implementation
        public bool IsIdle() { 
            bool result = false; 

            try { 
                result = _appManager.IsIdle();
            }
            catch (Exception e) {
                using (new ProcessImpersonationContext()) { 
                    Misc.ReportUnhandledException(e, new string[] {
                                              SR.GetString(SR.Failure_PMH_Idle)} ); 
                } 
                throw;
            } 

            return result;
        }
 

        public void Ping(IProcessPingCallback callback) { 
            try { 
                if (callback != null)
                    _appManager.Ping(callback); 
            }
            catch (Exception e) {
                using (new ProcessImpersonationContext()) {
                    Misc.ReportUnhandledException(e, new string[] { 
                                              SR.GetString(SR.Failure_PMH_Ping)} );
                } 
                throw; 
            }
        } 

        private ISAPIApplicationHost CreateAppHost(string appId, string appPath) {

            // 
            // if we have a null physical path, we need
            // to use the PMH to resolve it 
            // 
            if (String.IsNullOrEmpty(appPath)) {
                string virtualPath; 
                string physicalPath;
                string siteName;
                string siteID;
 
                _functions.GetApplicationProperties(
                        appId, 
                        out virtualPath, 
                        out physicalPath,
                        out siteName, 
                        out siteID);

                //
                // make sure physical app path ends with '\\' and virtual does not 
                //
                if (!StringUtil.StringEndsWith(physicalPath, '\\')) { 
                    physicalPath = physicalPath + "\\"; 
                }
 
                Debug.Assert( !String.IsNullOrEmpty(physicalPath), "!String.IsNullOrEmpty(physicalPath)");
                appPath = physicalPath;
            }
 
            //
            // Create a new application host 
            // This needs to be a coherent type across all 
            // protocol types so that we get a consistent
            // environment regardless of which protocol initializes first 
            //
            ISAPIApplicationHost appHost = new
                ISAPIApplicationHost(
                        appId, 
                        appPath,
                        false, /* validatePhysicalPath */ 
                        _functions 
                        );
 

            return appHost;
        }
    } 

    internal sealed class ListenerAdapterDispatchShim : MarshalByRefObject, IRegisteredObject { 
 
        void IRegisteredObject.Stop(bool immediate) {
            HostingEnvironment.UnregisterObject(this); 
        }

        // this should run in an Hosted app domain (not in the default domain)
        internal void StartListenerChannel( AppDomainProtocolHandler handler, IListenerChannelCallback listenerCallback ) { 
            Debug.Assert( HostingEnvironment.IsHosted, "HostingEnvironment.IsHosted" );
            Debug.Assert( null != handler, "null != handler" ); 
 
            IListenerChannelCallback unwrappedProxy = MarshalComProxy(listenerCallback);
 
            Debug.Assert(null != unwrappedProxy, "null != unwrappedProxy");
            if (null != unwrappedProxy && null != handler) {
                handler.StartListenerChannel(unwrappedProxy);
            } 
        }
 
        internal IListenerChannelCallback MarshalComProxy(IListenerChannelCallback defaultDomainCallback) { 
            IListenerChannelCallback localProxy = null;
 
            // get the underlying COM object
            IntPtr pUnk = Marshal.GetIUnknownForObject(defaultDomainCallback);

            // this object isn't a COM object 
            if (IntPtr.Zero == pUnk) {
                return null; 
            } 

            IntPtr ppv = IntPtr.Zero; 
            try {
                // QI it for the interface
                Guid g = typeof(IListenerChannelCallback).GUID;
 
                int hresult = Marshal.QueryInterface(pUnk, ref g, out ppv);
                if (hresult < 0)  { 
                    Marshal.ThrowExceptionForHR(hresult); 
                }
 
                // create a RCW we can hold onto in this domain
                // this bumps the ref count so we can drop our refs on the raw interfaces
                localProxy = (IListenerChannelCallback)Marshal.GetObjectForIUnknown(ppv);
            } 
            finally {
                // drop our explicit refs and keep the managed instance 
                if (IntPtr.Zero != ppv) { 
                    Marshal.Release(ppv);
                } 
                if (IntPtr.Zero != pUnk) {
                    Marshal.Release(pUnk);
                }
            } 

            return localProxy; 
        } 

    } 
}

