//------------------------------------------------------------------------------ 
// <copyright fieldInfole="_AutoWebProxyScriptEngine.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net 
{ 
    using System.IO;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Threading;
    using System.Text;
    using System.Net.Cache; 
#if !FEATURE_PAL
    using System.Net.NetworkInformation; 
    using System.Security.Principal; 
#endif
    using System.Globalization; 
    using System.Net.Configuration;
    using System.Security.Permissions;

    enum AutoWebProxyState { 
        Uninitialized = 0,
        DiscoveryFailure = 1, 
        DiscoverySuccess = 2, 
        DownloadFailure = 3,
        DownloadSuccess = 4, 
        CompilationFailure = 5,
        CompilationSuccess = 6,
        ExecutionFailure = 7,
        ExecutionSuccess = 8, 
    }
 
    /// <summary> 
    ///    Simple EXE host for the AutoWebProxyScriptEngine. Pushes the contents of a script file
    ///    into the engine and executes it.  Exposes the JScript model used in IE 3.2 - 6, 
    ///    for resolving which proxy to use.
    /// </summary>
    internal class AutoWebProxyScriptEngine {
        private static readonly char[] splitChars = new char[]{';'}; 

        private static TimerThread.Queue s_TimerQueue; 
        private static readonly TimerThread.Callback s_TimerCallback = new TimerThread.Callback(RequestTimeoutCallback); 
        private static readonly WaitCallback s_AbortWrapper = new WaitCallback(AbortWrapper);
 
        private bool automaticallyDetectSettings;
        private Uri automaticConfigurationScript;

        private AutoWebProxyScriptWrapper scriptInstance; 
        internal AutoWebProxyState state;
        private Uri engineScriptLocation; 
        private WebProxy webProxy; 

        private RequestCache backupCache; 

        // Used by abortable lock.
        private bool m_LockHeld;
        private WebRequest m_LockedRequest; 

        private bool m_UseRegistry; 
 
#if !FEATURE_PAL
        // Used to get notifications of network changes and do AutoDetection (which are global). 
        private int m_NetworkChangeStatus;
        private AutoDetector m_AutoDetector;

        // This has to hold on to the creating user's registry hive and impersonation context. 
        private SafeRegistryHandle hkcu;
        private WindowsIdentity m_Identity; 
#endif // !FEATURE_PAL 

        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.ControlPrincipal)] 
        internal AutoWebProxyScriptEngine(WebProxy proxy, bool useRegistry)
        {
            webProxy = proxy;
            m_UseRegistry = useRegistry; 

#if !FEATURE_PAL 
            m_AutoDetector = AutoDetector.CurrentAutoDetector; 
            m_NetworkChangeStatus = m_AutoDetector.NetworkChangeStatus;
 
            SafeRegistryHandle.RegOpenCurrentUser(UnsafeNclNativeMethods.RegistryHelper.KEY_READ, out hkcu);
            if (m_UseRegistry)
            {
                ListenForRegistry(); 

                // Keep track of the identity we used to read the registry, in case we need to read it again later. 
                m_Identity = WindowsIdentity.GetCurrent(); 
            }
 
#endif // !FEATURE_PAL
            backupCache = new SingleItemRequestCache(RequestCacheManager.IsCachingEnabled);
        }
 

        // AutoWebProxyScriptEngine has special abortable locking.  No one should ever lock (this) except the locking helper methods below. 
        private static class SyncStatus 
        {
            internal const int Unlocked      = 0; 
            internal const int Locking       = 1;
            internal const int LockOwner     = 2;
            internal const int RequestOwner  = 3;
            internal const int AbortedLocked = 4; 
            internal const int Aborted       = 5;
        } 
 
        private void EnterLock(ref int syncStatus)
        { 
            if (syncStatus == SyncStatus.Unlocked)
            {
                lock (this)
                { 
                    if (syncStatus != SyncStatus.Aborted)
                    { 
                        syncStatus = SyncStatus.Locking; 
                        while (true)
                        { 
                            if (!m_LockHeld)
                            {
                                syncStatus = SyncStatus.LockOwner;
                                m_LockHeld = true; 
                                return;
                            } 
                            Monitor.Wait(this); 
                            if (syncStatus == SyncStatus.Aborted)
                            { 
                                Monitor.Pulse(this);  // This is to ensure that a Pulse meant to let someone take the lock isn't lost.
                                return;
                            }
                        } 
                    }
                } 
            } 
        }
 
        private void ExitLock(ref int syncStatus)
        {
            if (syncStatus != SyncStatus.Unlocked && syncStatus != SyncStatus.Aborted)
            { 
                lock (this)
                { 
                    if (syncStatus == SyncStatus.RequestOwner) 
                    {
                        m_LockedRequest = null; 
                    }
                    m_LockHeld = false;
                    if (syncStatus == SyncStatus.AbortedLocked)
                    { 
                        state = AutoWebProxyState.Uninitialized;
                        syncStatus = SyncStatus.Aborted; 
                    } 
                    else
                    { 
                        syncStatus = SyncStatus.Unlocked;
                    }
                    Monitor.Pulse(this);
                } 
            }
        } 
 
        private void LockRequest(WebRequest request, ref int syncStatus)
        { 
            lock (this)
            {
                switch (syncStatus)
                { 
                    case SyncStatus.LockOwner:
                        m_LockedRequest = request; 
                        syncStatus = SyncStatus.RequestOwner; 
                        break;
 
                    case SyncStatus.RequestOwner:
                        m_LockedRequest = request;
                        break;
                } 
            }
        } 
 
        internal void Abort(ref int syncStatus)
        { 
            lock (this)
            {
                switch (syncStatus)
                { 
                    case SyncStatus.Unlocked:
                        syncStatus = SyncStatus.Aborted; 
                        break; 

                    case SyncStatus.Locking: 
                        syncStatus = SyncStatus.Aborted;
                        Monitor.PulseAll(this);
                        break;
 
                    case SyncStatus.LockOwner:
                        syncStatus = SyncStatus.AbortedLocked; 
                        break; 

                    case SyncStatus.RequestOwner: 
                        ThreadPool.UnsafeQueueUserWorkItem(s_AbortWrapper, m_LockedRequest);
                        syncStatus = SyncStatus.AbortedLocked;
                        m_LockedRequest = null;
                        break; 
                }
            } 
        } 
        // End of locking helper methods.
 

#if !FEATURE_PAL
        internal SafeRegistryHandle CurrentUserKey
        { 
            get
            { 
                return hkcu; 
            }
        } 

        internal string Connectoid {
            get {
                return m_AutoDetector.Connectoid; 
            }
        } 
#endif // !FEATURE_PAL 

        // The lock is always held while these three are modified. 
        internal bool AutomaticallyDetectSettings
        {
            set
            { 
                if (automaticallyDetectSettings != value)
                { 
                    state = AutoWebProxyState.Uninitialized; 
                    automaticallyDetectSettings = value;
                } 
            }
        }

        internal Uri AutomaticConfigurationScript 
        {
            set 
            { 
                if (!object.Equals(automaticConfigurationScript, value))
                { 
                    automaticConfigurationScript = value;

#if !FEATURE_PAL
                    if (!automaticallyDetectSettings || m_AutoDetector.DetectionFailed) 
#endif
                    { 
                        state = AutoWebProxyState.Uninitialized; 
                    }
                } 
            }
        }

 
        // from wininet.h
        // 
        //  #define INTERNET_MAX_PATH_LENGTH        2048 
        //  #define INTERNET_MAX_PROTOCOL_NAME      "gopher"    // longest protocol name
        //  #define INTERNET_MAX_URL_LENGTH         ((sizeof(INTERNET_MAX_PROTOCOL_NAME) - 1) \ 
        //                                          + sizeof("://") \
        //                                          + INTERNET_MAX_PATH_LENGTH)
        //
        private const int MaximumProxyStringLength = 2058; 

        /// <devdoc> 
        ///     <para> 
        ///         Called to discover script location. This performs
        ///         autodetection using the method specified in the detectFlags. 
        ///     </para>
        /// </devdoc>
        private static unsafe Uri SafeDetectAutoProxyUrl(uint discoveryMethod)
        { 
            Uri autoProxy = null;
 
#if !FEATURE_PAL 
            string url = null;
            if (ComNetOS.IsWinHttp51) 
            {
                GlobalLog.Print("AutoWebProxyScriptEngine::SafeDetectAutoProxyUrl() Using WinHttp.");
                SafeGlobalFree autoProxyUrl;
                bool success = UnsafeNclNativeMethods.WinHttp.WinHttpDetectAutoProxyConfigUrl(discoveryMethod, out autoProxyUrl); 
                if (!success)
                { 
                    if (autoProxyUrl != null) 
                    {
                        autoProxyUrl.SetHandleAsInvalid(); 
                    }
                }
                else
                { 
                    url = new string((char*) autoProxyUrl.DangerousGetHandle());
                    autoProxyUrl.Close(); 
                } 
            }
            else 
            {
                GlobalLog.Print("AutoWebProxyScriptEngine::SafeDetectAutoProxyUrl() Using WinInet.");
                StringBuilder autoProxyUrl = new StringBuilder(MaximumProxyStringLength);
                bool success = UnsafeNclNativeMethods.WinInet.DetectAutoProxyUrl( 
                    autoProxyUrl,
                    MaximumProxyStringLength, 
                    (int) discoveryMethod); 

                if (success) 
                {
                    url = autoProxyUrl.ToString();
                }
            } 

            if (url != null) 
            { 
                bool parsed = Uri.TryCreate(url, UriKind.Absolute, out autoProxy);
                if (!parsed) { 
                    if(Logging.On)Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_autodetect_script_location_parse_error, ValidationHelper.ToString(url)));
                    GlobalLog.Print("AutoWebProxyScriptEngine::SafeDetectAutoProxyUrl() Uri.TryParse() failed url:" + ValidationHelper.ToString(url));
                }
            } 
            else {
                if(Logging.On)Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_autodetect_failed)); 
                GlobalLog.Print("AutoWebProxyScriptEngine::SafeDetectAutoProxyUrl() DetectAutoProxyUrl() returned false"); 
            }
#endif // !FEATURE_PAL 

            return autoProxy;
        }
 
        internal StringCollection GetProxies(Uri destination, bool returnFirstOnly, out AutoWebProxyState autoWebProxyState)
        { 
            int syncStatus = SyncStatus.Unlocked; 
            return GetProxies(destination, returnFirstOnly, out autoWebProxyState, ref syncStatus);
        } 

        internal StringCollection GetProxies(Uri destination, bool returnFirstOnly, out AutoWebProxyState autoWebProxyState, ref int syncStatus)
        {
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::GetProxies() state:" + ValidationHelper.ToString(state)); 

#if !FEATURE_PAL 
            // See if we need to reinitialize based on registry or other changes. 
#if !AUTOPROXY_SKIP_CHECK
            CheckForChanges(ref syncStatus); 
#endif
#endif // !FEATURE_PAL

            if (state==AutoWebProxyState.DiscoveryFailure) { 
                // No engine will be available anyway, shortcut the call.
                autoWebProxyState = state; 
                return null; 
            }
 
            // This whole thing has to be locked, both to prevent simultaneous downloading / compilation, and
            // because the script isn't threadsafe.
            string scriptReturn = null;
            try 
            {
                EnterLock(ref syncStatus); 
                if (syncStatus != SyncStatus.LockOwner) 
                {
                    // This is typically because a download got aborted. 
                    autoWebProxyState = AutoWebProxyState.DownloadFailure;
                    return null;
                }
 
                autoWebProxyState = EnsureEngineAvailable(ref syncStatus);
                if (autoWebProxyState != AutoWebProxyState.CompilationSuccess) 
                { 
                    // the script can't run, say we're not ready and bypass
                    return null; 
                }
                autoWebProxyState = AutoWebProxyState.ExecutionFailure;
                try {
                    scriptReturn = scriptInstance.FindProxyForURL(destination.ToString(), destination.Host); 
                    autoWebProxyState = AutoWebProxyState.ExecutionSuccess;
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::GetProxies() calling ExecuteFindProxyForURL() for destination:" + ValidationHelper.ToString(destination) + " returned scriptReturn:" + ValidationHelper.ToString(scriptReturn)); 
                } 
                catch (Exception exception) {
                    if (NclUtilities.IsFatal(exception)) throw; 
                    if(Logging.On)Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_script_execution_error, exception));
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::GetProxies() calling ExecuteFindProxyForURL() for destination:" + ValidationHelper.ToString(destination) + " threw:" + ValidationHelper.ToString(exception));
                }
            } 
            finally
            { 
                ExitLock(ref syncStatus); 
            }
 
            if (autoWebProxyState==AutoWebProxyState.ExecutionFailure) {
                // the script failed at runtime, say we're not ready and bypass
                return null;
            } 
            StringCollection proxies = ParseScriptReturn(scriptReturn, destination, returnFirstOnly);
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::GetProxies() proxies:" + ValidationHelper.ToString(proxies)); 
            return proxies; 
        }
 
        /// <devdoc>
        ///     <para>
        ///         Ensures that (if state is AutoWebProxyState.CompilationSuccess) there is an engine available to execute script.
        ///         Figures out the script location (might discover if needed). 
        ///         Calls DownloadAndCompile().
        ///     </para> 
        /// </devdoc> 
        private AutoWebProxyState EnsureEngineAvailable(ref int syncStatus)
        { 
            GlobalLog.Enter("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable");
            AutoWebProxyScriptWrapper newScriptInstance;

            if (state == AutoWebProxyState.Uninitialized || engineScriptLocation == null) 
            {
#if !FEATURE_PAL 
                if (automaticallyDetectSettings) 
                {
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() Attempting auto-detection."); 
                    Uri scriptLocation = m_AutoDetector.DetectScriptLocation();
                    if (scriptLocation != null)
                    {
                        // 
                        // Successfully detected or user has flipped the automaticallyDetectSettings bit.
                        // Attempt a non conclusive DownloadAndCompile() so we can fallback 
                        // 
                        GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() discovered:" + ValidationHelper.ToString(scriptLocation) + " engineScriptLocation:" + ValidationHelper.ToString(engineScriptLocation));
                        state = AutoWebProxyState.DiscoverySuccess; 
                        if (scriptLocation.Equals(engineScriptLocation))
                        {
                            state = AutoWebProxyState.CompilationSuccess;
                            GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state)); 
                            return state;
                        } 
                        AutoWebProxyState newState = DownloadAndCompile(scriptLocation, out newScriptInstance, ref syncStatus); 
                        if (newState == AutoWebProxyState.CompilationSuccess)
                        { 
                            state = AutoWebProxyState.CompilationSuccess;
                            UpdateScriptInstance(newScriptInstance);
                            engineScriptLocation = scriptLocation;
                            GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state)); 
                            return state;
                        } 
                    } 
                }
#endif // !FEATURE_PAL 

                // Either Auto-Detect wasn't enabled or something failed with it.  Try the manual script location.
                if (automaticConfigurationScript != null)
                { 
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() using automaticConfigurationScript:" + ValidationHelper.ToString(automaticConfigurationScript) + " engineScriptLocation:" + ValidationHelper.ToString(engineScriptLocation));
                    state = AutoWebProxyState.DiscoverySuccess; 
                    if (automaticConfigurationScript.Equals(engineScriptLocation)) 
                    {
                        state = AutoWebProxyState.CompilationSuccess; 
                        GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state));
                        return state;
                    }
                    state = DownloadAndCompile(automaticConfigurationScript, out newScriptInstance, ref syncStatus); 
                    if (state == AutoWebProxyState.CompilationSuccess)
                    { 
                        UpdateScriptInstance(newScriptInstance); 
                        engineScriptLocation = automaticConfigurationScript;
                        GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state)); 
                        return state;
                    }
                }
            } 
            else
            { 
                // We always want to call DownloadAndCompile to check the expiration. 
                GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() state:" + state + " engineScriptLocation:" + ValidationHelper.ToString(engineScriptLocation));
                state = AutoWebProxyState.DiscoverySuccess; 
                state = DownloadAndCompile(engineScriptLocation, out newScriptInstance, ref syncStatus);
                if (state == AutoWebProxyState.CompilationSuccess)
                {
                    UpdateScriptInstance(newScriptInstance); 
                    GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state));
                    return state; 
                } 

                // There's still an opportunity to fail over to the automaticConfigurationScript. 
                if (!engineScriptLocation.Equals(automaticConfigurationScript))
                {
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() Update failed.  Falling back to automaticConfigurationScript:" + ValidationHelper.ToString(automaticConfigurationScript));
                    state = AutoWebProxyState.DiscoverySuccess; 
                    state = DownloadAndCompile(automaticConfigurationScript, out newScriptInstance, ref syncStatus);
                    if (state == AutoWebProxyState.CompilationSuccess) 
                    { 
                        UpdateScriptInstance(newScriptInstance);
                        engineScriptLocation = automaticConfigurationScript; 
                        GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state));
                        return state;
                    }
                } 
            }
 
            // Everything failed.  Set this instance to mostly-dead.  It will wake up again if there's a reg/connectoid change. 
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() All failed.");
            state = AutoWebProxyState.DiscoveryFailure; 
            UpdateScriptInstance(null);
            engineScriptLocation = null;

            GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state)); 
            return state;
        } 
 
        void UpdateScriptInstance(AutoWebProxyScriptWrapper newScriptInstance) {
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::UpdateScriptInstance() updating scriptInstance#" + ValidationHelper.HashString(scriptInstance) + " to newScriptInstance#" + ValidationHelper.HashString(newScriptInstance)); 

            if (scriptInstance == newScriptInstance)
            {
                return; 
            }
 
            if (scriptInstance!=null) { 
                GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::UpdateScriptInstance() Closing engine.");
                scriptInstance.Close(); 
            }
            scriptInstance = newScriptInstance;
        }
 
        /// <devdoc>
        ///     <para> 
        ///         Downloads and compiles the script from a given Uri. 
        ///         This code can be called by config for a downloaded control, we need to assert.
        ///         This code is called holding the lock. 
        ///     </para>
        /// </devdoc>
        private AutoWebProxyState DownloadAndCompile(Uri location, out AutoWebProxyScriptWrapper newScriptInstance, ref int syncStatus)
        { 
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() location:" + ValidationHelper.ToString(location));
            AutoWebProxyState newState = AutoWebProxyState.DownloadFailure; 
            WebResponse response = null; 
            TimerThread.Timer timer = null;
            newScriptInstance = null; 

            // Can't assert this in declarative form (DCR?). This Assert() is needed to be able to create the request to download the proxy script.
            ExceptionHelper.WebPermissionUnrestricted.Assert();
            try { 
                // here we have a reentrance issue due to config load.
                WebRequest request = WebRequest.Create(location); 
                request.Timeout = Timeout.Infinite; 
                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
                request.ConnectionGroupName = "__WebProxyScript"; 

                // We have an opportunity here, if caching is disabled AppDomain-wide, to override it with a
                // custom, trivial cache-provider to get a similar semantic.
                // 
                // We also want to have a backup caching key in the case when IE has locked an expired script response
                // 
                if (request.CacheProtocol != null) 
                {
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() Using backup caching."); 
                    request.CacheProtocol = new RequestCacheProtocol(backupCache, request.CacheProtocol.Validator);
                }

                HttpWebRequest httpWebRequest = request as HttpWebRequest; 
                if (httpWebRequest!=null)
                { 
                    httpWebRequest.Accept = "*/*"; 
                    httpWebRequest.UserAgent = this.GetType().FullName + "/" + Environment.Version;
                    httpWebRequest.KeepAlive = false; 
                    httpWebRequest.Pipelined = false;
                    httpWebRequest.InternalConnectionGroup = true;
                }
                else 
                {
                    FtpWebRequest ftpWebRequest = request as FtpWebRequest; 
                    if (ftpWebRequest!=null) 
                    {
                        ftpWebRequest.KeepAlive = false; 
                    }
                }

                // Use no proxy, default cache - initiate the download. 
                request.Proxy = null;
                request.Credentials = webProxy.Credentials; 
 
                // Set this up with the abortable lock to abort this too.
                LockRequest(request, ref syncStatus); 
                if (syncStatus != SyncStatus.RequestOwner)
                {
                    throw new WebException(NetRes.GetWebStatusString("net_requestaborted", WebExceptionStatus.RequestCanceled), WebExceptionStatus.RequestCanceled);
                } 

                // Use our own timeout timer so that it can encompass the whole request, not just the headers. 
                if (s_TimerQueue == null) 
                {
                    s_TimerQueue = TimerThread.GetOrCreateQueue(SettingsSectionInternal.Section.DownloadTimeout); 
                }
                timer = s_TimerQueue.CreateTimer(s_TimerCallback, request);
                response = request.GetResponse();
 
                // Check Last Modified.
                DateTime lastModified = DateTime.MinValue; 
                HttpWebResponse httpResponse = response as HttpWebResponse; 
                if (httpResponse != null)
                { 
                    lastModified = httpResponse.LastModified;
                }
                else
                { 
                    FtpWebResponse ftpResponse = response as FtpWebResponse;
                    if (ftpResponse != null) 
                    { 
                        lastModified = ftpResponse.LastModified;
                    } 
                }
                GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() lastModified:" + lastModified.ToString() + " (script):" + (scriptInstance == null ? "(null)" : scriptInstance.LastModified.ToString()));
                if (scriptInstance != null && lastModified != DateTime.MinValue && scriptInstance.LastModified == lastModified)
                { 
                    newScriptInstance = scriptInstance;
                    newState = AutoWebProxyState.CompilationSuccess; 
                } 
                else
                { 
                    string scriptBody = null;
                    byte[] scriptBuffer = null;
                    using (Stream responseStream = response.GetResponseStream())
                    { 
                        SingleItemRequestCache.ReadOnlyStream ros = responseStream as SingleItemRequestCache.ReadOnlyStream;
                        if (ros != null) 
                        { 
                            scriptBuffer = ros.Buffer;
                        } 
                        if (scriptInstance != null && scriptBuffer != null && scriptBuffer == scriptInstance.Buffer)
                        {
                            scriptInstance.LastModified = lastModified;
                            newScriptInstance = scriptInstance; 
                            newState = AutoWebProxyState.CompilationSuccess;
                            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() Buffer matched - reusing engine."); 
                        } 
                        else
                        { 
                            using (StreamReader streamReader = new StreamReader(responseStream))
                            {
                                scriptBody = streamReader.ReadToEnd();
                            } 
                        }
                    } 
 
                    WebResponse tempResponse = response;
                    response = null; 
                    tempResponse.Close();
                    timer.Cancel();
                    timer = null;
 
                    if (newState != AutoWebProxyState.CompilationSuccess)
                    { 
                        newState = AutoWebProxyState.DownloadSuccess; 

                        GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() IsFromCache:" + tempResponse.IsFromCache.ToString() + " scriptInstance:" + ValidationHelper.HashString(scriptInstance)); 
                        if (scriptInstance != null && scriptBody == scriptInstance.ScriptBody)
                        {
                            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() Script matched - using existing engine.");
                            scriptInstance.LastModified = lastModified; 
                            if (scriptBuffer != null)
                            { 
                                scriptInstance.Buffer = scriptBuffer; 
                            }
                            newScriptInstance = scriptInstance; 
                            newState = AutoWebProxyState.CompilationSuccess;
                        }
                        else
                        { 
                            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() Creating AutoWebProxyScriptWrapper.");
                            newScriptInstance = new AutoWebProxyScriptWrapper(); 
                            newScriptInstance.LastModified = lastModified; 
                            newState = newScriptInstance.Compile(location, scriptBody, scriptBuffer);
                        } 
                    }
                }
            }
            catch (Exception exception) 
            {
                if (NclUtilities.IsFatal(exception)) throw; 
                if(Logging.On)Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_script_download_compile_error, exception)); 
                GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() Download() threw:" + ValidationHelper.ToString(exception));
            } 
            finally
            {
                if (timer != null)
                { 
                    timer.Cancel();
                } 
 
                //
                try 
                {
                    if (response != null)
                    {
                        response.Close(); 
                    }
                } 
                finally 
                {
                    WebPermission.RevertAssert(); 
                }
            }
            if (newState!=AutoWebProxyState.CompilationSuccess) {
                newScriptInstance = null; 
            }
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() retuning newState:" + ValidationHelper.ToString(newState)); 
            return newState; 
        }
 
        // RequestTimeoutCallback - Called by the TimerThread to abort a request.  This just posts ThreadPool work item - Abort() does too
        // much to be done on the timer thread (timer thread should never block or call user code).
        private static void RequestTimeoutCallback(TimerThread.Timer timer, int timeNoticed, object context)
        { 
            ThreadPool.UnsafeQueueUserWorkItem(s_AbortWrapper, context);
        } 
 
        private static void AbortWrapper(object context)
        { 
#if DEBUG
            GlobalLog.SetThreadSource(ThreadKinds.Worker);
            using (GlobalLog.SetThreadKind(ThreadKinds.System)) {
#endif 
            ((WebRequest) context).Abort();
#if DEBUG 
            } 
#endif
        } 

        private StringCollection ParseScriptReturn(string scriptReturn, Uri destination, bool returnFirstOnly) {
            if (scriptReturn == null)
            { 
                return new StringCollection();
            } 
            StringCollection proxies = new StringCollection(); 
            string[] proxyListStrings = scriptReturn.Split(splitChars);
            string proxyAuthority; 
            foreach (string s in proxyListStrings)
            {
                string proxyString = s.Trim(' ');
                if (!proxyString.StartsWith("PROXY ", StringComparison.InvariantCultureIgnoreCase)) 
                {
                    if (string.Compare("DIRECT", proxyString, StringComparison.InvariantCultureIgnoreCase) == 0) 
                    { 
                        proxyAuthority = null;
                    } 
                    else
                    {
                        continue;
                    } 
                }
                else 
                { 
                    proxyAuthority = proxyString.Substring(6).TrimStart(' ');
                    Uri uri = null; 
                    bool tryParse = Uri.TryCreate("http://" + proxyAuthority, UriKind.Absolute, out uri);
                    if (!tryParse || uri.UserInfo.Length>0 || uri.HostNameType==UriHostNameType.Basic || uri.AbsolutePath.Length!=1 || proxyAuthority[proxyAuthority.Length-1]=='/' || proxyAuthority[proxyAuthority.Length-1]=='#' || proxyAuthority[proxyAuthority.Length-1]=='?') {
                        continue;
                    } 
                }
                proxies.Add(proxyAuthority); 
                if (returnFirstOnly) { 
                    break;
                } 
            }
            return proxies;
        }
 
        internal void Close() {
#if !FEATURE_PAL 
            // m_AutoDetector is always set up in the constructor, use it to lock 
            if (m_AutoDetector != null)
            { 
                int syncStatus = SyncStatus.Unlocked;
                try
                {
                    EnterLock(ref syncStatus); 
                    GlobalLog.Assert(syncStatus == SyncStatus.LockOwner, "AutoWebProxyScriptEngine#{0}::Close()|Failed to acquire lock.", ValidationHelper.HashString(this));
 
                    if (m_AutoDetector != null) 
                    {
                        registrySuppress = true; 
                        if (registryChangeEventPolicy != null)
                        {
                            registryChangeEventPolicy.Close();
                            registryChangeEventPolicy = null; 
                        }
                        if (registryChangeEventLM != null) 
                        { 
                            registryChangeEventLM.Close();
                            registryChangeEventLM = null; 
                        }
                        if (registryChangeEvent != null)
                        {
                            registryChangeEvent.Close(); 
                            registryChangeEvent = null;
                        } 
 
                        if (regKeyPolicy != null && !regKeyPolicy.IsInvalid)
                        { 
                            regKeyPolicy.Close();
                        }
                        if (regKeyLM != null && !regKeyLM.IsInvalid)
                        { 
                            regKeyLM.Close();
                        } 
                        if (regKey!=null && !regKey.IsInvalid) { 
                            regKey.Close();
                        } 

                        if (hkcu != null)
                        {
                            hkcu.RegCloseKey(); 
                            hkcu = null;
                        } 
 
                        if (m_Identity != null)
                        { 
                            m_Identity.Dispose();
                            m_Identity = null;
                        }
 
                        if (scriptInstance!=null) {
                            scriptInstance.Close(); 
                        } 

                        m_AutoDetector = null; 
                    }
                }
                finally
                { 
                    ExitLock(ref syncStatus);
                } 
            } 
#endif // !FEATURE_PAL
        } 

#if !FEATURE_PAL
        private SafeRegistryHandle regKey;
        private SafeRegistryHandle regKeyLM; 
        private SafeRegistryHandle regKeyPolicy;
        private AutoResetEvent registryChangeEvent; 
        private AutoResetEvent registryChangeEventLM; 
        private AutoResetEvent registryChangeEventPolicy;
        private bool registryChangeDeferred; 
        private bool registryChangeLMDeferred;
        private bool registryChangePolicyDeferred;
        private bool needRegistryUpdate;
        private bool needConnectoidUpdate; 
        private bool registrySuppress;
 
        internal void ListenForRegistry() { 
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry()");
            if (!registrySuppress) 
            {
                if (registryChangeEvent == null)
                {
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() hooking HKCU."); 
                    ListenForRegistryHelper(ref regKey, ref registryChangeEvent, IntPtr.Zero, ProxyRegBlob.ProxyKey);
                } 
                if (registryChangeEventLM == null) 
                {
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() hooking HKLM."); 
                    ListenForRegistryHelper(ref regKeyLM, ref registryChangeEventLM, UnsafeNclNativeMethods.RegistryHelper.HKEY_LOCAL_MACHINE, ProxyRegBlob.ProxyKey);
                }
                if (registryChangeEventPolicy == null)
                { 
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() hooking HKLM/Policies.");
                    ListenForRegistryHelper(ref regKeyPolicy, ref registryChangeEventPolicy, UnsafeNclNativeMethods.RegistryHelper.HKEY_LOCAL_MACHINE, ProxyRegBlob.PolicyKey); 
                } 

                // If any succeeded, we should monitor it. 
                if (registryChangeEvent == null && registryChangeEventLM == null && registryChangeEventPolicy == null)
                {
                    registrySuppress = true;
                } 
            }
        } 
 
        private void ListenForRegistryHelper(ref SafeRegistryHandle key, ref AutoResetEvent changeEvent, IntPtr baseKey, string subKey)
        { 
            uint errorCode = 0;

            // First time through?
            if (key == null || key.IsInvalid) 
            {
                if (baseKey == IntPtr.Zero) 
                { 
                    // Impersonation requires extra effort.
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() RegOpenCurrentUser() using hkcu:" + hkcu.DangerousGetHandle().ToString("x")); 
                    if (hkcu != null)
                    {
                        errorCode = hkcu.RegOpenKeyEx(subKey, 0, UnsafeNclNativeMethods.RegistryHelper.KEY_READ, out key);
                        GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() RegOpenKeyEx() returned errorCode:" + errorCode + " key:" + key.DangerousGetHandle().ToString("x")); 
                    }
                    else 
                    { 
                        errorCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_NOT_FOUND;
                    } 
                }
                else
                {
                    errorCode = SafeRegistryHandle.RegOpenKeyEx(baseKey, subKey, 0, UnsafeNclNativeMethods.RegistryHelper.KEY_READ, out key); 
                    //GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() RegOpenKeyEx() returned errorCode:" + errorCode + " key:" + key.DangerousGetHandle().ToString("x"));
                } 
                if (errorCode == 0) 
                {
                    changeEvent = new AutoResetEvent(false); 
                }
            }
            if (errorCode == 0)
            { 
                // accessing Handle is protected by a link demand, OK for System.dll
                errorCode = key.RegNotifyChangeKeyValue(true, UnsafeNclNativeMethods.RegistryHelper.REG_NOTIFY_CHANGE_LAST_SET, changeEvent.SafeWaitHandle, true); 
                GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() RegNotifyChangeKeyValue() returned errorCode:" + errorCode); 
            }
            if (errorCode != 0) 
            {
                if (key != null && !key.IsInvalid)
                {
                    try 
                    {
                        errorCode = key.RegCloseKey(); 
                    } 
                    catch (Exception exception)
                    { 
                        if (NclUtilities.IsFatal(exception)) throw;
                    }
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() RegCloseKey() returned errorCode:" + errorCode);
                } 
                key = null;
                if (changeEvent != null) 
                { 
                    changeEvent.Close();
                    changeEvent = null; 
                }
            }
        }
 
        private void RegistryChanged()
        { 
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::RegistryChanged()"); 
            if(Logging.On) Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_system_setting_update));
 
            // always refresh settings because they might have changed
            WebProxyData webProxyData;
            try
            { 
                using (m_Identity.Impersonate())
                { 
                    webProxyData = ProxyRegBlob.GetWebProxyData(Connectoid, hkcu); 
                }
            } 
            catch
            {
                throw;
            } 
            webProxy.Update(webProxyData);
        } 
 
        private void ConnectoidChanged()
        { 
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ConnectoidChanged()");
            if(Logging.On) Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_update_due_to_ip_config_change));

            // Get the new connectoid/detector.  Only do this after detecting a change, to avoid races with other people detecting changes. 
            // (We don't want to end up using a detector/connectoid that doesn't match what we read from the registry.)
            m_AutoDetector = AutoDetector.CurrentAutoDetector; 
 
            if (m_UseRegistry)
            { 
                // update the engine and proxy
                WebProxyData webProxyData;
                try
                { 
                    using (m_Identity.Impersonate())
                    { 
                        webProxyData = ProxyRegBlob.GetWebProxyData(Connectoid, hkcu); 
                    }
                } 
                catch
                {
                    throw;
                } 
                webProxy.Update(webProxyData);
            } 
 
            // Always uninitialized if the connectoid/address changed and we are autodetecting.
            if (automaticallyDetectSettings) 
                state = AutoWebProxyState.Uninitialized;
        }

        internal void CheckForChanges() 
        {
            int syncStatus = SyncStatus.Unlocked; 
            CheckForChanges(ref syncStatus); 
        }
 
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.ControlPrincipal)]
        private void CheckForChanges(ref int syncStatus)
        {
            // Catch ObjectDisposedException instead of synchronizing with Close(). 
            try
            { 
                bool changed = AutoDetector.CheckForNetworkChanges(ref m_NetworkChangeStatus); 
                bool ignoreRegistryChange = false;
                if (changed || needConnectoidUpdate) 
                {
                    try
                    {
                        EnterLock(ref syncStatus); 
                        if (changed || needConnectoidUpdate)   // Make sure no one else took care of it before we got the lock.
                        { 
                            needConnectoidUpdate = syncStatus != SyncStatus.LockOwner; 
                            if (!needConnectoidUpdate)
                            { 
                                ConnectoidChanged();

                                // We usually get a registry change at the same time.  Since the connectoid change does more,
                                // we can skip reading the registry info twice. 
                                ignoreRegistryChange = true;
                            } 
                        } 
                    }
                    finally 
                    {
                        ExitLock(ref syncStatus);
                    }
                } 

                if (!m_UseRegistry) 
                { 
                    return;
                } 

                bool forReal = false;
                AutoResetEvent tempEvent = registryChangeEvent;
                if (registryChangeDeferred || (forReal = (tempEvent != null && tempEvent.WaitOne(0, false)))) 
                {
                    try 
                    { 
                        EnterLock(ref syncStatus);
                        if (forReal || registryChangeDeferred)  // Check if someone else handled it before I got the lock. 
                        {
                            registryChangeDeferred = syncStatus != SyncStatus.LockOwner;
                            if (!registryChangeDeferred && registryChangeEvent != null)
                            { 
                                try
                                { 
                                    using (m_Identity.Impersonate()) 
                                    {
                                        ListenForRegistryHelper(ref regKey, ref registryChangeEvent, IntPtr.Zero, ProxyRegBlob.ProxyKey); 
                                    }
                                }
                                catch
                                { 
                                    throw;
                                } 
                                needRegistryUpdate = true; 
                            }
                        } 
                    }
                    finally
                    {
                        ExitLock(ref syncStatus); 
                    }
                } 
 
                forReal = false;
                tempEvent = registryChangeEventLM; 
                if (registryChangeLMDeferred || (forReal = (tempEvent != null && tempEvent.WaitOne(0, false))))
                {
                    try
                    { 
                        EnterLock(ref syncStatus);
                        if (forReal || registryChangeLMDeferred)  // Check if someone else handled it before I got the lock. 
                        { 
                            registryChangeLMDeferred = syncStatus != SyncStatus.LockOwner;
                            if (!registryChangeLMDeferred && registryChangeEventLM != null) 
                            {
                                try
                                {
                                    using (m_Identity.Impersonate()) 
                                    {
                                        ListenForRegistryHelper(ref regKeyLM, ref registryChangeEventLM, UnsafeNclNativeMethods.RegistryHelper.HKEY_LOCAL_MACHINE, ProxyRegBlob.ProxyKey); 
                                    } 
                                }
                                catch 
                                {
                                    throw;
                                }
                                needRegistryUpdate = true; 
                            }
                        } 
                    } 
                    finally
                    { 
                        ExitLock(ref syncStatus);
                    }
                }
 
                forReal = false;
                tempEvent = registryChangeEventPolicy; 
                if (registryChangePolicyDeferred || (forReal = (tempEvent != null && tempEvent.WaitOne(0, false)))) 
                {
                    try 
                    {
                        EnterLock(ref syncStatus);
                        if (forReal || registryChangePolicyDeferred)  // Check if someone else handled it before I got the lock.
                        { 
                            registryChangePolicyDeferred = syncStatus != SyncStatus.LockOwner;
                            if (!registryChangePolicyDeferred && registryChangeEventPolicy != null) 
                            { 
                                try
                                { 
                                    using (m_Identity.Impersonate())
                                    {
                                        ListenForRegistryHelper(ref regKeyPolicy, ref registryChangeEventPolicy, UnsafeNclNativeMethods.RegistryHelper.HKEY_LOCAL_MACHINE, ProxyRegBlob.PolicyKey);
                                    } 
                                }
                                catch 
                                { 
                                    throw;
                                } 
                                needRegistryUpdate = true;
                            }
                        }
                    } 
                    finally
                    { 
                        ExitLock(ref syncStatus); 
                    }
                } 

                if (needRegistryUpdate)
                {
                    try 
                    {
                        EnterLock(ref syncStatus); 
                        if (needRegistryUpdate && syncStatus == SyncStatus.LockOwner) 
                        {
                            needRegistryUpdate = false; 

                            // We don't need to process this now if we just did it for the connectoid.
                            if (!ignoreRegistryChange)
                            { 
                                RegistryChanged();
                            } 
                        } 
                    }
                    finally 
                    {
                        ExitLock(ref syncStatus);
                    }
                } 
            }
            catch (ObjectDisposedException) { } 
        } 

        private class AutoDetector 
        {
            private static object s_InternalLock;

            private static NetworkAddressChangePolled s_AddressChange; 
            private static UnsafeNclNativeMethods.RasHelper s_RasHelper;
 
            private static int s_CurrentVersion; 
            private static AutoDetector s_CurrentAutoDetector;
 
            private static void Initialize()
            {
                if (s_InternalLock == null)
                { 
                    Interlocked.CompareExchange(ref s_InternalLock, new object(), null);
                } 
                if (s_RasHelper == null) 
                {
                    lock (s_InternalLock) 
                    {
                        if (s_RasHelper == null)
                        {
                            s_CurrentVersion = 1; 
                            s_CurrentAutoDetector = new AutoDetector(UnsafeNclNativeMethods.RasHelper.GetCurrentConnectoid(), 1);
                            if (NetworkChange.CanListenForNetworkChanges) 
                            { 
                                s_AddressChange = new NetworkAddressChangePolled();
                            } 
                            s_RasHelper = new UnsafeNclNativeMethods.RasHelper();
                        }
                    }
                } 
            }
 
            internal static bool CheckForNetworkChanges(ref int changeStatus) 
            {
                Initialize(); 
                CheckForChanges();
                int oldStatus = changeStatus;
                changeStatus = s_CurrentVersion;
                return oldStatus != changeStatus; 
            }
 
            private static void CheckForChanges() 
            {
                bool changed = false; 
                if (s_RasHelper.HasChanged)
                {
                    s_RasHelper.Reset();
                    changed = true; 
                }
                if (s_AddressChange != null && s_AddressChange.CheckAndReset()) 
                { 
                    changed = true;
                } 
                if (changed)
                {
                    // It's ok if there's a race here which only increments the version by one instead of two.  It only needs
                    // to change, not strictly count. 
                    s_CurrentVersion += 1;
                    s_CurrentAutoDetector = new AutoDetector(UnsafeNclNativeMethods.RasHelper.GetCurrentConnectoid(), s_CurrentVersion); 
                } 
            }
 
            internal static AutoDetector CurrentAutoDetector
            {
                get
                { 
                    Initialize();
                    return s_CurrentAutoDetector; 
                } 
            }
 

            private readonly string m_Connectoid;
            private readonly int m_CurrentVersion;
 
            private Uri m_ScriptLocation;
            private bool m_DetectionFailed; 
 
            private AutoDetector(string connectoid, int currentVersion)
            { 
                m_Connectoid = connectoid;
                m_CurrentVersion = currentVersion;
            }
 
            internal Uri DetectScriptLocation()
            { 
                Uri scriptLocation = m_ScriptLocation; 
                if (m_DetectionFailed || scriptLocation != null)
                { 
                    return scriptLocation;
                }

                lock (this) 
                {
                    if (m_DetectionFailed || m_ScriptLocation != null) 
                    { 
                        return m_ScriptLocation;
                    } 

                    GlobalLog.Print("AutoDetector::DetectScriptLocation() Attempting discovery PROXY_AUTO_DETECT_TYPE_DHCP.");
                    m_ScriptLocation = SafeDetectAutoProxyUrl(UnsafeNclNativeMethods.AUTO_DETECT_TYPE_DHCP);
 
                    if (m_ScriptLocation == null)
                    { 
                        GlobalLog.Print("AutoDetector::DetectScriptLocation() Attempting discovery AUTO_DETECT_TYPE_DNS_A."); 
                        m_ScriptLocation = SafeDetectAutoProxyUrl(UnsafeNclNativeMethods.AUTO_DETECT_TYPE_DNS_A);
                    } 

                    if (m_ScriptLocation == null)
                    {
                        GlobalLog.Print("AutoDetector::DetectScriptLocation() Discovery failed."); 
                        m_DetectionFailed = true;
                    } 
 
                    return m_ScriptLocation;
                } 
            }

            internal bool DetectionFailed
            { 
                get
                { 
                    return m_DetectionFailed; 
                }
            } 

            internal string Connectoid
            {
                get 
                {
                    return m_Connectoid; 
                } 
            }
 
            internal int NetworkChangeStatus
            {
                get
                { 
                    return m_CurrentVersion;
                } 
            } 
        }
#endif 

    }
}
//------------------------------------------------------------------------------ 
// <copyright fieldInfole="_AutoWebProxyScriptEngine.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net 
{ 
    using System.IO;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Threading;
    using System.Text;
    using System.Net.Cache; 
#if !FEATURE_PAL
    using System.Net.NetworkInformation; 
    using System.Security.Principal; 
#endif
    using System.Globalization; 
    using System.Net.Configuration;
    using System.Security.Permissions;

    enum AutoWebProxyState { 
        Uninitialized = 0,
        DiscoveryFailure = 1, 
        DiscoverySuccess = 2, 
        DownloadFailure = 3,
        DownloadSuccess = 4, 
        CompilationFailure = 5,
        CompilationSuccess = 6,
        ExecutionFailure = 7,
        ExecutionSuccess = 8, 
    }
 
    /// <summary> 
    ///    Simple EXE host for the AutoWebProxyScriptEngine. Pushes the contents of a script file
    ///    into the engine and executes it.  Exposes the JScript model used in IE 3.2 - 6, 
    ///    for resolving which proxy to use.
    /// </summary>
    internal class AutoWebProxyScriptEngine {
        private static readonly char[] splitChars = new char[]{';'}; 

        private static TimerThread.Queue s_TimerQueue; 
        private static readonly TimerThread.Callback s_TimerCallback = new TimerThread.Callback(RequestTimeoutCallback); 
        private static readonly WaitCallback s_AbortWrapper = new WaitCallback(AbortWrapper);
 
        private bool automaticallyDetectSettings;
        private Uri automaticConfigurationScript;

        private AutoWebProxyScriptWrapper scriptInstance; 
        internal AutoWebProxyState state;
        private Uri engineScriptLocation; 
        private WebProxy webProxy; 

        private RequestCache backupCache; 

        // Used by abortable lock.
        private bool m_LockHeld;
        private WebRequest m_LockedRequest; 

        private bool m_UseRegistry; 
 
#if !FEATURE_PAL
        // Used to get notifications of network changes and do AutoDetection (which are global). 
        private int m_NetworkChangeStatus;
        private AutoDetector m_AutoDetector;

        // This has to hold on to the creating user's registry hive and impersonation context. 
        private SafeRegistryHandle hkcu;
        private WindowsIdentity m_Identity; 
#endif // !FEATURE_PAL 

        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.ControlPrincipal)] 
        internal AutoWebProxyScriptEngine(WebProxy proxy, bool useRegistry)
        {
            webProxy = proxy;
            m_UseRegistry = useRegistry; 

#if !FEATURE_PAL 
            m_AutoDetector = AutoDetector.CurrentAutoDetector; 
            m_NetworkChangeStatus = m_AutoDetector.NetworkChangeStatus;
 
            SafeRegistryHandle.RegOpenCurrentUser(UnsafeNclNativeMethods.RegistryHelper.KEY_READ, out hkcu);
            if (m_UseRegistry)
            {
                ListenForRegistry(); 

                // Keep track of the identity we used to read the registry, in case we need to read it again later. 
                m_Identity = WindowsIdentity.GetCurrent(); 
            }
 
#endif // !FEATURE_PAL
            backupCache = new SingleItemRequestCache(RequestCacheManager.IsCachingEnabled);
        }
 

        // AutoWebProxyScriptEngine has special abortable locking.  No one should ever lock (this) except the locking helper methods below. 
        private static class SyncStatus 
        {
            internal const int Unlocked      = 0; 
            internal const int Locking       = 1;
            internal const int LockOwner     = 2;
            internal const int RequestOwner  = 3;
            internal const int AbortedLocked = 4; 
            internal const int Aborted       = 5;
        } 
 
        private void EnterLock(ref int syncStatus)
        { 
            if (syncStatus == SyncStatus.Unlocked)
            {
                lock (this)
                { 
                    if (syncStatus != SyncStatus.Aborted)
                    { 
                        syncStatus = SyncStatus.Locking; 
                        while (true)
                        { 
                            if (!m_LockHeld)
                            {
                                syncStatus = SyncStatus.LockOwner;
                                m_LockHeld = true; 
                                return;
                            } 
                            Monitor.Wait(this); 
                            if (syncStatus == SyncStatus.Aborted)
                            { 
                                Monitor.Pulse(this);  // This is to ensure that a Pulse meant to let someone take the lock isn't lost.
                                return;
                            }
                        } 
                    }
                } 
            } 
        }
 
        private void ExitLock(ref int syncStatus)
        {
            if (syncStatus != SyncStatus.Unlocked && syncStatus != SyncStatus.Aborted)
            { 
                lock (this)
                { 
                    if (syncStatus == SyncStatus.RequestOwner) 
                    {
                        m_LockedRequest = null; 
                    }
                    m_LockHeld = false;
                    if (syncStatus == SyncStatus.AbortedLocked)
                    { 
                        state = AutoWebProxyState.Uninitialized;
                        syncStatus = SyncStatus.Aborted; 
                    } 
                    else
                    { 
                        syncStatus = SyncStatus.Unlocked;
                    }
                    Monitor.Pulse(this);
                } 
            }
        } 
 
        private void LockRequest(WebRequest request, ref int syncStatus)
        { 
            lock (this)
            {
                switch (syncStatus)
                { 
                    case SyncStatus.LockOwner:
                        m_LockedRequest = request; 
                        syncStatus = SyncStatus.RequestOwner; 
                        break;
 
                    case SyncStatus.RequestOwner:
                        m_LockedRequest = request;
                        break;
                } 
            }
        } 
 
        internal void Abort(ref int syncStatus)
        { 
            lock (this)
            {
                switch (syncStatus)
                { 
                    case SyncStatus.Unlocked:
                        syncStatus = SyncStatus.Aborted; 
                        break; 

                    case SyncStatus.Locking: 
                        syncStatus = SyncStatus.Aborted;
                        Monitor.PulseAll(this);
                        break;
 
                    case SyncStatus.LockOwner:
                        syncStatus = SyncStatus.AbortedLocked; 
                        break; 

                    case SyncStatus.RequestOwner: 
                        ThreadPool.UnsafeQueueUserWorkItem(s_AbortWrapper, m_LockedRequest);
                        syncStatus = SyncStatus.AbortedLocked;
                        m_LockedRequest = null;
                        break; 
                }
            } 
        } 
        // End of locking helper methods.
 

#if !FEATURE_PAL
        internal SafeRegistryHandle CurrentUserKey
        { 
            get
            { 
                return hkcu; 
            }
        } 

        internal string Connectoid {
            get {
                return m_AutoDetector.Connectoid; 
            }
        } 
#endif // !FEATURE_PAL 

        // The lock is always held while these three are modified. 
        internal bool AutomaticallyDetectSettings
        {
            set
            { 
                if (automaticallyDetectSettings != value)
                { 
                    state = AutoWebProxyState.Uninitialized; 
                    automaticallyDetectSettings = value;
                } 
            }
        }

        internal Uri AutomaticConfigurationScript 
        {
            set 
            { 
                if (!object.Equals(automaticConfigurationScript, value))
                { 
                    automaticConfigurationScript = value;

#if !FEATURE_PAL
                    if (!automaticallyDetectSettings || m_AutoDetector.DetectionFailed) 
#endif
                    { 
                        state = AutoWebProxyState.Uninitialized; 
                    }
                } 
            }
        }

 
        // from wininet.h
        // 
        //  #define INTERNET_MAX_PATH_LENGTH        2048 
        //  #define INTERNET_MAX_PROTOCOL_NAME      "gopher"    // longest protocol name
        //  #define INTERNET_MAX_URL_LENGTH         ((sizeof(INTERNET_MAX_PROTOCOL_NAME) - 1) \ 
        //                                          + sizeof("://") \
        //                                          + INTERNET_MAX_PATH_LENGTH)
        //
        private const int MaximumProxyStringLength = 2058; 

        /// <devdoc> 
        ///     <para> 
        ///         Called to discover script location. This performs
        ///         autodetection using the method specified in the detectFlags. 
        ///     </para>
        /// </devdoc>
        private static unsafe Uri SafeDetectAutoProxyUrl(uint discoveryMethod)
        { 
            Uri autoProxy = null;
 
#if !FEATURE_PAL 
            string url = null;
            if (ComNetOS.IsWinHttp51) 
            {
                GlobalLog.Print("AutoWebProxyScriptEngine::SafeDetectAutoProxyUrl() Using WinHttp.");
                SafeGlobalFree autoProxyUrl;
                bool success = UnsafeNclNativeMethods.WinHttp.WinHttpDetectAutoProxyConfigUrl(discoveryMethod, out autoProxyUrl); 
                if (!success)
                { 
                    if (autoProxyUrl != null) 
                    {
                        autoProxyUrl.SetHandleAsInvalid(); 
                    }
                }
                else
                { 
                    url = new string((char*) autoProxyUrl.DangerousGetHandle());
                    autoProxyUrl.Close(); 
                } 
            }
            else 
            {
                GlobalLog.Print("AutoWebProxyScriptEngine::SafeDetectAutoProxyUrl() Using WinInet.");
                StringBuilder autoProxyUrl = new StringBuilder(MaximumProxyStringLength);
                bool success = UnsafeNclNativeMethods.WinInet.DetectAutoProxyUrl( 
                    autoProxyUrl,
                    MaximumProxyStringLength, 
                    (int) discoveryMethod); 

                if (success) 
                {
                    url = autoProxyUrl.ToString();
                }
            } 

            if (url != null) 
            { 
                bool parsed = Uri.TryCreate(url, UriKind.Absolute, out autoProxy);
                if (!parsed) { 
                    if(Logging.On)Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_autodetect_script_location_parse_error, ValidationHelper.ToString(url)));
                    GlobalLog.Print("AutoWebProxyScriptEngine::SafeDetectAutoProxyUrl() Uri.TryParse() failed url:" + ValidationHelper.ToString(url));
                }
            } 
            else {
                if(Logging.On)Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_autodetect_failed)); 
                GlobalLog.Print("AutoWebProxyScriptEngine::SafeDetectAutoProxyUrl() DetectAutoProxyUrl() returned false"); 
            }
#endif // !FEATURE_PAL 

            return autoProxy;
        }
 
        internal StringCollection GetProxies(Uri destination, bool returnFirstOnly, out AutoWebProxyState autoWebProxyState)
        { 
            int syncStatus = SyncStatus.Unlocked; 
            return GetProxies(destination, returnFirstOnly, out autoWebProxyState, ref syncStatus);
        } 

        internal StringCollection GetProxies(Uri destination, bool returnFirstOnly, out AutoWebProxyState autoWebProxyState, ref int syncStatus)
        {
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::GetProxies() state:" + ValidationHelper.ToString(state)); 

#if !FEATURE_PAL 
            // See if we need to reinitialize based on registry or other changes. 
#if !AUTOPROXY_SKIP_CHECK
            CheckForChanges(ref syncStatus); 
#endif
#endif // !FEATURE_PAL

            if (state==AutoWebProxyState.DiscoveryFailure) { 
                // No engine will be available anyway, shortcut the call.
                autoWebProxyState = state; 
                return null; 
            }
 
            // This whole thing has to be locked, both to prevent simultaneous downloading / compilation, and
            // because the script isn't threadsafe.
            string scriptReturn = null;
            try 
            {
                EnterLock(ref syncStatus); 
                if (syncStatus != SyncStatus.LockOwner) 
                {
                    // This is typically because a download got aborted. 
                    autoWebProxyState = AutoWebProxyState.DownloadFailure;
                    return null;
                }
 
                autoWebProxyState = EnsureEngineAvailable(ref syncStatus);
                if (autoWebProxyState != AutoWebProxyState.CompilationSuccess) 
                { 
                    // the script can't run, say we're not ready and bypass
                    return null; 
                }
                autoWebProxyState = AutoWebProxyState.ExecutionFailure;
                try {
                    scriptReturn = scriptInstance.FindProxyForURL(destination.ToString(), destination.Host); 
                    autoWebProxyState = AutoWebProxyState.ExecutionSuccess;
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::GetProxies() calling ExecuteFindProxyForURL() for destination:" + ValidationHelper.ToString(destination) + " returned scriptReturn:" + ValidationHelper.ToString(scriptReturn)); 
                } 
                catch (Exception exception) {
                    if (NclUtilities.IsFatal(exception)) throw; 
                    if(Logging.On)Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_script_execution_error, exception));
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::GetProxies() calling ExecuteFindProxyForURL() for destination:" + ValidationHelper.ToString(destination) + " threw:" + ValidationHelper.ToString(exception));
                }
            } 
            finally
            { 
                ExitLock(ref syncStatus); 
            }
 
            if (autoWebProxyState==AutoWebProxyState.ExecutionFailure) {
                // the script failed at runtime, say we're not ready and bypass
                return null;
            } 
            StringCollection proxies = ParseScriptReturn(scriptReturn, destination, returnFirstOnly);
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::GetProxies() proxies:" + ValidationHelper.ToString(proxies)); 
            return proxies; 
        }
 
        /// <devdoc>
        ///     <para>
        ///         Ensures that (if state is AutoWebProxyState.CompilationSuccess) there is an engine available to execute script.
        ///         Figures out the script location (might discover if needed). 
        ///         Calls DownloadAndCompile().
        ///     </para> 
        /// </devdoc> 
        private AutoWebProxyState EnsureEngineAvailable(ref int syncStatus)
        { 
            GlobalLog.Enter("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable");
            AutoWebProxyScriptWrapper newScriptInstance;

            if (state == AutoWebProxyState.Uninitialized || engineScriptLocation == null) 
            {
#if !FEATURE_PAL 
                if (automaticallyDetectSettings) 
                {
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() Attempting auto-detection."); 
                    Uri scriptLocation = m_AutoDetector.DetectScriptLocation();
                    if (scriptLocation != null)
                    {
                        // 
                        // Successfully detected or user has flipped the automaticallyDetectSettings bit.
                        // Attempt a non conclusive DownloadAndCompile() so we can fallback 
                        // 
                        GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() discovered:" + ValidationHelper.ToString(scriptLocation) + " engineScriptLocation:" + ValidationHelper.ToString(engineScriptLocation));
                        state = AutoWebProxyState.DiscoverySuccess; 
                        if (scriptLocation.Equals(engineScriptLocation))
                        {
                            state = AutoWebProxyState.CompilationSuccess;
                            GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state)); 
                            return state;
                        } 
                        AutoWebProxyState newState = DownloadAndCompile(scriptLocation, out newScriptInstance, ref syncStatus); 
                        if (newState == AutoWebProxyState.CompilationSuccess)
                        { 
                            state = AutoWebProxyState.CompilationSuccess;
                            UpdateScriptInstance(newScriptInstance);
                            engineScriptLocation = scriptLocation;
                            GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state)); 
                            return state;
                        } 
                    } 
                }
#endif // !FEATURE_PAL 

                // Either Auto-Detect wasn't enabled or something failed with it.  Try the manual script location.
                if (automaticConfigurationScript != null)
                { 
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() using automaticConfigurationScript:" + ValidationHelper.ToString(automaticConfigurationScript) + " engineScriptLocation:" + ValidationHelper.ToString(engineScriptLocation));
                    state = AutoWebProxyState.DiscoverySuccess; 
                    if (automaticConfigurationScript.Equals(engineScriptLocation)) 
                    {
                        state = AutoWebProxyState.CompilationSuccess; 
                        GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state));
                        return state;
                    }
                    state = DownloadAndCompile(automaticConfigurationScript, out newScriptInstance, ref syncStatus); 
                    if (state == AutoWebProxyState.CompilationSuccess)
                    { 
                        UpdateScriptInstance(newScriptInstance); 
                        engineScriptLocation = automaticConfigurationScript;
                        GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state)); 
                        return state;
                    }
                }
            } 
            else
            { 
                // We always want to call DownloadAndCompile to check the expiration. 
                GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() state:" + state + " engineScriptLocation:" + ValidationHelper.ToString(engineScriptLocation));
                state = AutoWebProxyState.DiscoverySuccess; 
                state = DownloadAndCompile(engineScriptLocation, out newScriptInstance, ref syncStatus);
                if (state == AutoWebProxyState.CompilationSuccess)
                {
                    UpdateScriptInstance(newScriptInstance); 
                    GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state));
                    return state; 
                } 

                // There's still an opportunity to fail over to the automaticConfigurationScript. 
                if (!engineScriptLocation.Equals(automaticConfigurationScript))
                {
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() Update failed.  Falling back to automaticConfigurationScript:" + ValidationHelper.ToString(automaticConfigurationScript));
                    state = AutoWebProxyState.DiscoverySuccess; 
                    state = DownloadAndCompile(automaticConfigurationScript, out newScriptInstance, ref syncStatus);
                    if (state == AutoWebProxyState.CompilationSuccess) 
                    { 
                        UpdateScriptInstance(newScriptInstance);
                        engineScriptLocation = automaticConfigurationScript; 
                        GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state));
                        return state;
                    }
                } 
            }
 
            // Everything failed.  Set this instance to mostly-dead.  It will wake up again if there's a reg/connectoid change. 
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable() All failed.");
            state = AutoWebProxyState.DiscoveryFailure; 
            UpdateScriptInstance(null);
            engineScriptLocation = null;

            GlobalLog.Leave("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::EnsureEngineAvailable", ValidationHelper.ToString(state)); 
            return state;
        } 
 
        void UpdateScriptInstance(AutoWebProxyScriptWrapper newScriptInstance) {
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::UpdateScriptInstance() updating scriptInstance#" + ValidationHelper.HashString(scriptInstance) + " to newScriptInstance#" + ValidationHelper.HashString(newScriptInstance)); 

            if (scriptInstance == newScriptInstance)
            {
                return; 
            }
 
            if (scriptInstance!=null) { 
                GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::UpdateScriptInstance() Closing engine.");
                scriptInstance.Close(); 
            }
            scriptInstance = newScriptInstance;
        }
 
        /// <devdoc>
        ///     <para> 
        ///         Downloads and compiles the script from a given Uri. 
        ///         This code can be called by config for a downloaded control, we need to assert.
        ///         This code is called holding the lock. 
        ///     </para>
        /// </devdoc>
        private AutoWebProxyState DownloadAndCompile(Uri location, out AutoWebProxyScriptWrapper newScriptInstance, ref int syncStatus)
        { 
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() location:" + ValidationHelper.ToString(location));
            AutoWebProxyState newState = AutoWebProxyState.DownloadFailure; 
            WebResponse response = null; 
            TimerThread.Timer timer = null;
            newScriptInstance = null; 

            // Can't assert this in declarative form (DCR?). This Assert() is needed to be able to create the request to download the proxy script.
            ExceptionHelper.WebPermissionUnrestricted.Assert();
            try { 
                // here we have a reentrance issue due to config load.
                WebRequest request = WebRequest.Create(location); 
                request.Timeout = Timeout.Infinite; 
                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
                request.ConnectionGroupName = "__WebProxyScript"; 

                // We have an opportunity here, if caching is disabled AppDomain-wide, to override it with a
                // custom, trivial cache-provider to get a similar semantic.
                // 
                // We also want to have a backup caching key in the case when IE has locked an expired script response
                // 
                if (request.CacheProtocol != null) 
                {
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() Using backup caching."); 
                    request.CacheProtocol = new RequestCacheProtocol(backupCache, request.CacheProtocol.Validator);
                }

                HttpWebRequest httpWebRequest = request as HttpWebRequest; 
                if (httpWebRequest!=null)
                { 
                    httpWebRequest.Accept = "*/*"; 
                    httpWebRequest.UserAgent = this.GetType().FullName + "/" + Environment.Version;
                    httpWebRequest.KeepAlive = false; 
                    httpWebRequest.Pipelined = false;
                    httpWebRequest.InternalConnectionGroup = true;
                }
                else 
                {
                    FtpWebRequest ftpWebRequest = request as FtpWebRequest; 
                    if (ftpWebRequest!=null) 
                    {
                        ftpWebRequest.KeepAlive = false; 
                    }
                }

                // Use no proxy, default cache - initiate the download. 
                request.Proxy = null;
                request.Credentials = webProxy.Credentials; 
 
                // Set this up with the abortable lock to abort this too.
                LockRequest(request, ref syncStatus); 
                if (syncStatus != SyncStatus.RequestOwner)
                {
                    throw new WebException(NetRes.GetWebStatusString("net_requestaborted", WebExceptionStatus.RequestCanceled), WebExceptionStatus.RequestCanceled);
                } 

                // Use our own timeout timer so that it can encompass the whole request, not just the headers. 
                if (s_TimerQueue == null) 
                {
                    s_TimerQueue = TimerThread.GetOrCreateQueue(SettingsSectionInternal.Section.DownloadTimeout); 
                }
                timer = s_TimerQueue.CreateTimer(s_TimerCallback, request);
                response = request.GetResponse();
 
                // Check Last Modified.
                DateTime lastModified = DateTime.MinValue; 
                HttpWebResponse httpResponse = response as HttpWebResponse; 
                if (httpResponse != null)
                { 
                    lastModified = httpResponse.LastModified;
                }
                else
                { 
                    FtpWebResponse ftpResponse = response as FtpWebResponse;
                    if (ftpResponse != null) 
                    { 
                        lastModified = ftpResponse.LastModified;
                    } 
                }
                GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() lastModified:" + lastModified.ToString() + " (script):" + (scriptInstance == null ? "(null)" : scriptInstance.LastModified.ToString()));
                if (scriptInstance != null && lastModified != DateTime.MinValue && scriptInstance.LastModified == lastModified)
                { 
                    newScriptInstance = scriptInstance;
                    newState = AutoWebProxyState.CompilationSuccess; 
                } 
                else
                { 
                    string scriptBody = null;
                    byte[] scriptBuffer = null;
                    using (Stream responseStream = response.GetResponseStream())
                    { 
                        SingleItemRequestCache.ReadOnlyStream ros = responseStream as SingleItemRequestCache.ReadOnlyStream;
                        if (ros != null) 
                        { 
                            scriptBuffer = ros.Buffer;
                        } 
                        if (scriptInstance != null && scriptBuffer != null && scriptBuffer == scriptInstance.Buffer)
                        {
                            scriptInstance.LastModified = lastModified;
                            newScriptInstance = scriptInstance; 
                            newState = AutoWebProxyState.CompilationSuccess;
                            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() Buffer matched - reusing engine."); 
                        } 
                        else
                        { 
                            using (StreamReader streamReader = new StreamReader(responseStream))
                            {
                                scriptBody = streamReader.ReadToEnd();
                            } 
                        }
                    } 
 
                    WebResponse tempResponse = response;
                    response = null; 
                    tempResponse.Close();
                    timer.Cancel();
                    timer = null;
 
                    if (newState != AutoWebProxyState.CompilationSuccess)
                    { 
                        newState = AutoWebProxyState.DownloadSuccess; 

                        GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() IsFromCache:" + tempResponse.IsFromCache.ToString() + " scriptInstance:" + ValidationHelper.HashString(scriptInstance)); 
                        if (scriptInstance != null && scriptBody == scriptInstance.ScriptBody)
                        {
                            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() Script matched - using existing engine.");
                            scriptInstance.LastModified = lastModified; 
                            if (scriptBuffer != null)
                            { 
                                scriptInstance.Buffer = scriptBuffer; 
                            }
                            newScriptInstance = scriptInstance; 
                            newState = AutoWebProxyState.CompilationSuccess;
                        }
                        else
                        { 
                            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() Creating AutoWebProxyScriptWrapper.");
                            newScriptInstance = new AutoWebProxyScriptWrapper(); 
                            newScriptInstance.LastModified = lastModified; 
                            newState = newScriptInstance.Compile(location, scriptBody, scriptBuffer);
                        } 
                    }
                }
            }
            catch (Exception exception) 
            {
                if (NclUtilities.IsFatal(exception)) throw; 
                if(Logging.On)Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_script_download_compile_error, exception)); 
                GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() Download() threw:" + ValidationHelper.ToString(exception));
            } 
            finally
            {
                if (timer != null)
                { 
                    timer.Cancel();
                } 
 
                //
                try 
                {
                    if (response != null)
                    {
                        response.Close(); 
                    }
                } 
                finally 
                {
                    WebPermission.RevertAssert(); 
                }
            }
            if (newState!=AutoWebProxyState.CompilationSuccess) {
                newScriptInstance = null; 
            }
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::DownloadAndCompile() retuning newState:" + ValidationHelper.ToString(newState)); 
            return newState; 
        }
 
        // RequestTimeoutCallback - Called by the TimerThread to abort a request.  This just posts ThreadPool work item - Abort() does too
        // much to be done on the timer thread (timer thread should never block or call user code).
        private static void RequestTimeoutCallback(TimerThread.Timer timer, int timeNoticed, object context)
        { 
            ThreadPool.UnsafeQueueUserWorkItem(s_AbortWrapper, context);
        } 
 
        private static void AbortWrapper(object context)
        { 
#if DEBUG
            GlobalLog.SetThreadSource(ThreadKinds.Worker);
            using (GlobalLog.SetThreadKind(ThreadKinds.System)) {
#endif 
            ((WebRequest) context).Abort();
#if DEBUG 
            } 
#endif
        } 

        private StringCollection ParseScriptReturn(string scriptReturn, Uri destination, bool returnFirstOnly) {
            if (scriptReturn == null)
            { 
                return new StringCollection();
            } 
            StringCollection proxies = new StringCollection(); 
            string[] proxyListStrings = scriptReturn.Split(splitChars);
            string proxyAuthority; 
            foreach (string s in proxyListStrings)
            {
                string proxyString = s.Trim(' ');
                if (!proxyString.StartsWith("PROXY ", StringComparison.InvariantCultureIgnoreCase)) 
                {
                    if (string.Compare("DIRECT", proxyString, StringComparison.InvariantCultureIgnoreCase) == 0) 
                    { 
                        proxyAuthority = null;
                    } 
                    else
                    {
                        continue;
                    } 
                }
                else 
                { 
                    proxyAuthority = proxyString.Substring(6).TrimStart(' ');
                    Uri uri = null; 
                    bool tryParse = Uri.TryCreate("http://" + proxyAuthority, UriKind.Absolute, out uri);
                    if (!tryParse || uri.UserInfo.Length>0 || uri.HostNameType==UriHostNameType.Basic || uri.AbsolutePath.Length!=1 || proxyAuthority[proxyAuthority.Length-1]=='/' || proxyAuthority[proxyAuthority.Length-1]=='#' || proxyAuthority[proxyAuthority.Length-1]=='?') {
                        continue;
                    } 
                }
                proxies.Add(proxyAuthority); 
                if (returnFirstOnly) { 
                    break;
                } 
            }
            return proxies;
        }
 
        internal void Close() {
#if !FEATURE_PAL 
            // m_AutoDetector is always set up in the constructor, use it to lock 
            if (m_AutoDetector != null)
            { 
                int syncStatus = SyncStatus.Unlocked;
                try
                {
                    EnterLock(ref syncStatus); 
                    GlobalLog.Assert(syncStatus == SyncStatus.LockOwner, "AutoWebProxyScriptEngine#{0}::Close()|Failed to acquire lock.", ValidationHelper.HashString(this));
 
                    if (m_AutoDetector != null) 
                    {
                        registrySuppress = true; 
                        if (registryChangeEventPolicy != null)
                        {
                            registryChangeEventPolicy.Close();
                            registryChangeEventPolicy = null; 
                        }
                        if (registryChangeEventLM != null) 
                        { 
                            registryChangeEventLM.Close();
                            registryChangeEventLM = null; 
                        }
                        if (registryChangeEvent != null)
                        {
                            registryChangeEvent.Close(); 
                            registryChangeEvent = null;
                        } 
 
                        if (regKeyPolicy != null && !regKeyPolicy.IsInvalid)
                        { 
                            regKeyPolicy.Close();
                        }
                        if (regKeyLM != null && !regKeyLM.IsInvalid)
                        { 
                            regKeyLM.Close();
                        } 
                        if (regKey!=null && !regKey.IsInvalid) { 
                            regKey.Close();
                        } 

                        if (hkcu != null)
                        {
                            hkcu.RegCloseKey(); 
                            hkcu = null;
                        } 
 
                        if (m_Identity != null)
                        { 
                            m_Identity.Dispose();
                            m_Identity = null;
                        }
 
                        if (scriptInstance!=null) {
                            scriptInstance.Close(); 
                        } 

                        m_AutoDetector = null; 
                    }
                }
                finally
                { 
                    ExitLock(ref syncStatus);
                } 
            } 
#endif // !FEATURE_PAL
        } 

#if !FEATURE_PAL
        private SafeRegistryHandle regKey;
        private SafeRegistryHandle regKeyLM; 
        private SafeRegistryHandle regKeyPolicy;
        private AutoResetEvent registryChangeEvent; 
        private AutoResetEvent registryChangeEventLM; 
        private AutoResetEvent registryChangeEventPolicy;
        private bool registryChangeDeferred; 
        private bool registryChangeLMDeferred;
        private bool registryChangePolicyDeferred;
        private bool needRegistryUpdate;
        private bool needConnectoidUpdate; 
        private bool registrySuppress;
 
        internal void ListenForRegistry() { 
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry()");
            if (!registrySuppress) 
            {
                if (registryChangeEvent == null)
                {
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() hooking HKCU."); 
                    ListenForRegistryHelper(ref regKey, ref registryChangeEvent, IntPtr.Zero, ProxyRegBlob.ProxyKey);
                } 
                if (registryChangeEventLM == null) 
                {
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() hooking HKLM."); 
                    ListenForRegistryHelper(ref regKeyLM, ref registryChangeEventLM, UnsafeNclNativeMethods.RegistryHelper.HKEY_LOCAL_MACHINE, ProxyRegBlob.ProxyKey);
                }
                if (registryChangeEventPolicy == null)
                { 
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() hooking HKLM/Policies.");
                    ListenForRegistryHelper(ref regKeyPolicy, ref registryChangeEventPolicy, UnsafeNclNativeMethods.RegistryHelper.HKEY_LOCAL_MACHINE, ProxyRegBlob.PolicyKey); 
                } 

                // If any succeeded, we should monitor it. 
                if (registryChangeEvent == null && registryChangeEventLM == null && registryChangeEventPolicy == null)
                {
                    registrySuppress = true;
                } 
            }
        } 
 
        private void ListenForRegistryHelper(ref SafeRegistryHandle key, ref AutoResetEvent changeEvent, IntPtr baseKey, string subKey)
        { 
            uint errorCode = 0;

            // First time through?
            if (key == null || key.IsInvalid) 
            {
                if (baseKey == IntPtr.Zero) 
                { 
                    // Impersonation requires extra effort.
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() RegOpenCurrentUser() using hkcu:" + hkcu.DangerousGetHandle().ToString("x")); 
                    if (hkcu != null)
                    {
                        errorCode = hkcu.RegOpenKeyEx(subKey, 0, UnsafeNclNativeMethods.RegistryHelper.KEY_READ, out key);
                        GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() RegOpenKeyEx() returned errorCode:" + errorCode + " key:" + key.DangerousGetHandle().ToString("x")); 
                    }
                    else 
                    { 
                        errorCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_NOT_FOUND;
                    } 
                }
                else
                {
                    errorCode = SafeRegistryHandle.RegOpenKeyEx(baseKey, subKey, 0, UnsafeNclNativeMethods.RegistryHelper.KEY_READ, out key); 
                    //GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() RegOpenKeyEx() returned errorCode:" + errorCode + " key:" + key.DangerousGetHandle().ToString("x"));
                } 
                if (errorCode == 0) 
                {
                    changeEvent = new AutoResetEvent(false); 
                }
            }
            if (errorCode == 0)
            { 
                // accessing Handle is protected by a link demand, OK for System.dll
                errorCode = key.RegNotifyChangeKeyValue(true, UnsafeNclNativeMethods.RegistryHelper.REG_NOTIFY_CHANGE_LAST_SET, changeEvent.SafeWaitHandle, true); 
                GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() RegNotifyChangeKeyValue() returned errorCode:" + errorCode); 
            }
            if (errorCode != 0) 
            {
                if (key != null && !key.IsInvalid)
                {
                    try 
                    {
                        errorCode = key.RegCloseKey(); 
                    } 
                    catch (Exception exception)
                    { 
                        if (NclUtilities.IsFatal(exception)) throw;
                    }
                    GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ListenForRegistry() RegCloseKey() returned errorCode:" + errorCode);
                } 
                key = null;
                if (changeEvent != null) 
                { 
                    changeEvent.Close();
                    changeEvent = null; 
                }
            }
        }
 
        private void RegistryChanged()
        { 
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::RegistryChanged()"); 
            if(Logging.On) Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_system_setting_update));
 
            // always refresh settings because they might have changed
            WebProxyData webProxyData;
            try
            { 
                using (m_Identity.Impersonate())
                { 
                    webProxyData = ProxyRegBlob.GetWebProxyData(Connectoid, hkcu); 
                }
            } 
            catch
            {
                throw;
            } 
            webProxy.Update(webProxyData);
        } 
 
        private void ConnectoidChanged()
        { 
            GlobalLog.Print("AutoWebProxyScriptEngine#" + ValidationHelper.HashString(this) + "::ConnectoidChanged()");
            if(Logging.On) Logging.PrintWarning(Logging.Web, SR.GetString(SR.net_log_proxy_update_due_to_ip_config_change));

            // Get the new connectoid/detector.  Only do this after detecting a change, to avoid races with other people detecting changes. 
            // (We don't want to end up using a detector/connectoid that doesn't match what we read from the registry.)
            m_AutoDetector = AutoDetector.CurrentAutoDetector; 
 
            if (m_UseRegistry)
            { 
                // update the engine and proxy
                WebProxyData webProxyData;
                try
                { 
                    using (m_Identity.Impersonate())
                    { 
                        webProxyData = ProxyRegBlob.GetWebProxyData(Connectoid, hkcu); 
                    }
                } 
                catch
                {
                    throw;
                } 
                webProxy.Update(webProxyData);
            } 
 
            // Always uninitialized if the connectoid/address changed and we are autodetecting.
            if (automaticallyDetectSettings) 
                state = AutoWebProxyState.Uninitialized;
        }

        internal void CheckForChanges() 
        {
            int syncStatus = SyncStatus.Unlocked; 
            CheckForChanges(ref syncStatus); 
        }
 
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.ControlPrincipal)]
        private void CheckForChanges(ref int syncStatus)
        {
            // Catch ObjectDisposedException instead of synchronizing with Close(). 
            try
            { 
                bool changed = AutoDetector.CheckForNetworkChanges(ref m_NetworkChangeStatus); 
                bool ignoreRegistryChange = false;
                if (changed || needConnectoidUpdate) 
                {
                    try
                    {
                        EnterLock(ref syncStatus); 
                        if (changed || needConnectoidUpdate)   // Make sure no one else took care of it before we got the lock.
                        { 
                            needConnectoidUpdate = syncStatus != SyncStatus.LockOwner; 
                            if (!needConnectoidUpdate)
                            { 
                                ConnectoidChanged();

                                // We usually get a registry change at the same time.  Since the connectoid change does more,
                                // we can skip reading the registry info twice. 
                                ignoreRegistryChange = true;
                            } 
                        } 
                    }
                    finally 
                    {
                        ExitLock(ref syncStatus);
                    }
                } 

                if (!m_UseRegistry) 
                { 
                    return;
                } 

                bool forReal = false;
                AutoResetEvent tempEvent = registryChangeEvent;
                if (registryChangeDeferred || (forReal = (tempEvent != null && tempEvent.WaitOne(0, false)))) 
                {
                    try 
                    { 
                        EnterLock(ref syncStatus);
                        if (forReal || registryChangeDeferred)  // Check if someone else handled it before I got the lock. 
                        {
                            registryChangeDeferred = syncStatus != SyncStatus.LockOwner;
                            if (!registryChangeDeferred && registryChangeEvent != null)
                            { 
                                try
                                { 
                                    using (m_Identity.Impersonate()) 
                                    {
                                        ListenForRegistryHelper(ref regKey, ref registryChangeEvent, IntPtr.Zero, ProxyRegBlob.ProxyKey); 
                                    }
                                }
                                catch
                                { 
                                    throw;
                                } 
                                needRegistryUpdate = true; 
                            }
                        } 
                    }
                    finally
                    {
                        ExitLock(ref syncStatus); 
                    }
                } 
 
                forReal = false;
                tempEvent = registryChangeEventLM; 
                if (registryChangeLMDeferred || (forReal = (tempEvent != null && tempEvent.WaitOne(0, false))))
                {
                    try
                    { 
                        EnterLock(ref syncStatus);
                        if (forReal || registryChangeLMDeferred)  // Check if someone else handled it before I got the lock. 
                        { 
                            registryChangeLMDeferred = syncStatus != SyncStatus.LockOwner;
                            if (!registryChangeLMDeferred && registryChangeEventLM != null) 
                            {
                                try
                                {
                                    using (m_Identity.Impersonate()) 
                                    {
                                        ListenForRegistryHelper(ref regKeyLM, ref registryChangeEventLM, UnsafeNclNativeMethods.RegistryHelper.HKEY_LOCAL_MACHINE, ProxyRegBlob.ProxyKey); 
                                    } 
                                }
                                catch 
                                {
                                    throw;
                                }
                                needRegistryUpdate = true; 
                            }
                        } 
                    } 
                    finally
                    { 
                        ExitLock(ref syncStatus);
                    }
                }
 
                forReal = false;
                tempEvent = registryChangeEventPolicy; 
                if (registryChangePolicyDeferred || (forReal = (tempEvent != null && tempEvent.WaitOne(0, false)))) 
                {
                    try 
                    {
                        EnterLock(ref syncStatus);
                        if (forReal || registryChangePolicyDeferred)  // Check if someone else handled it before I got the lock.
                        { 
                            registryChangePolicyDeferred = syncStatus != SyncStatus.LockOwner;
                            if (!registryChangePolicyDeferred && registryChangeEventPolicy != null) 
                            { 
                                try
                                { 
                                    using (m_Identity.Impersonate())
                                    {
                                        ListenForRegistryHelper(ref regKeyPolicy, ref registryChangeEventPolicy, UnsafeNclNativeMethods.RegistryHelper.HKEY_LOCAL_MACHINE, ProxyRegBlob.PolicyKey);
                                    } 
                                }
                                catch 
                                { 
                                    throw;
                                } 
                                needRegistryUpdate = true;
                            }
                        }
                    } 
                    finally
                    { 
                        ExitLock(ref syncStatus); 
                    }
                } 

                if (needRegistryUpdate)
                {
                    try 
                    {
                        EnterLock(ref syncStatus); 
                        if (needRegistryUpdate && syncStatus == SyncStatus.LockOwner) 
                        {
                            needRegistryUpdate = false; 

                            // We don't need to process this now if we just did it for the connectoid.
                            if (!ignoreRegistryChange)
                            { 
                                RegistryChanged();
                            } 
                        } 
                    }
                    finally 
                    {
                        ExitLock(ref syncStatus);
                    }
                } 
            }
            catch (ObjectDisposedException) { } 
        } 

        private class AutoDetector 
        {
            private static object s_InternalLock;

            private static NetworkAddressChangePolled s_AddressChange; 
            private static UnsafeNclNativeMethods.RasHelper s_RasHelper;
 
            private static int s_CurrentVersion; 
            private static AutoDetector s_CurrentAutoDetector;
 
            private static void Initialize()
            {
                if (s_InternalLock == null)
                { 
                    Interlocked.CompareExchange(ref s_InternalLock, new object(), null);
                } 
                if (s_RasHelper == null) 
                {
                    lock (s_InternalLock) 
                    {
                        if (s_RasHelper == null)
                        {
                            s_CurrentVersion = 1; 
                            s_CurrentAutoDetector = new AutoDetector(UnsafeNclNativeMethods.RasHelper.GetCurrentConnectoid(), 1);
                            if (NetworkChange.CanListenForNetworkChanges) 
                            { 
                                s_AddressChange = new NetworkAddressChangePolled();
                            } 
                            s_RasHelper = new UnsafeNclNativeMethods.RasHelper();
                        }
                    }
                } 
            }
 
            internal static bool CheckForNetworkChanges(ref int changeStatus) 
            {
                Initialize(); 
                CheckForChanges();
                int oldStatus = changeStatus;
                changeStatus = s_CurrentVersion;
                return oldStatus != changeStatus; 
            }
 
            private static void CheckForChanges() 
            {
                bool changed = false; 
                if (s_RasHelper.HasChanged)
                {
                    s_RasHelper.Reset();
                    changed = true; 
                }
                if (s_AddressChange != null && s_AddressChange.CheckAndReset()) 
                { 
                    changed = true;
                } 
                if (changed)
                {
                    // It's ok if there's a race here which only increments the version by one instead of two.  It only needs
                    // to change, not strictly count. 
                    s_CurrentVersion += 1;
                    s_CurrentAutoDetector = new AutoDetector(UnsafeNclNativeMethods.RasHelper.GetCurrentConnectoid(), s_CurrentVersion); 
                } 
            }
 
            internal static AutoDetector CurrentAutoDetector
            {
                get
                { 
                    Initialize();
                    return s_CurrentAutoDetector; 
                } 
            }
 

            private readonly string m_Connectoid;
            private readonly int m_CurrentVersion;
 
            private Uri m_ScriptLocation;
            private bool m_DetectionFailed; 
 
            private AutoDetector(string connectoid, int currentVersion)
            { 
                m_Connectoid = connectoid;
                m_CurrentVersion = currentVersion;
            }
 
            internal Uri DetectScriptLocation()
            { 
                Uri scriptLocation = m_ScriptLocation; 
                if (m_DetectionFailed || scriptLocation != null)
                { 
                    return scriptLocation;
                }

                lock (this) 
                {
                    if (m_DetectionFailed || m_ScriptLocation != null) 
                    { 
                        return m_ScriptLocation;
                    } 

                    GlobalLog.Print("AutoDetector::DetectScriptLocation() Attempting discovery PROXY_AUTO_DETECT_TYPE_DHCP.");
                    m_ScriptLocation = SafeDetectAutoProxyUrl(UnsafeNclNativeMethods.AUTO_DETECT_TYPE_DHCP);
 
                    if (m_ScriptLocation == null)
                    { 
                        GlobalLog.Print("AutoDetector::DetectScriptLocation() Attempting discovery AUTO_DETECT_TYPE_DNS_A."); 
                        m_ScriptLocation = SafeDetectAutoProxyUrl(UnsafeNclNativeMethods.AUTO_DETECT_TYPE_DNS_A);
                    } 

                    if (m_ScriptLocation == null)
                    {
                        GlobalLog.Print("AutoDetector::DetectScriptLocation() Discovery failed."); 
                        m_DetectionFailed = true;
                    } 
 
                    return m_ScriptLocation;
                } 
            }

            internal bool DetectionFailed
            { 
                get
                { 
                    return m_DetectionFailed; 
                }
            } 

            internal string Connectoid
            {
                get 
                {
                    return m_Connectoid; 
                } 
            }
 
            internal int NetworkChangeStatus
            {
                get
                { 
                    return m_CurrentVersion;
                } 
            } 
        }
#endif 

    }
}
