//------------------------------------------------------------------------------ 
// <copyright file="EventLog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

//#define RETRY_ON_ALL_ERRORS 
 
namespace System.Diagnostics {
    using System.Text; 
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics;
    using System; 
    using Microsoft.Win32; 
    using Microsoft.Win32.SafeHandles;
    using System.IO; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.ComponentModel.Design; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Reflection; 
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices; 

    /// <devdoc>
    ///    <para>
    ///       Provides interaction with Windows 2000 event logs. 
    ///    </para>
    /// </devdoc> 
    [ 
    DefaultEvent("EntryWritten"),
    InstallerType("System.Diagnostics.EventLogInstaller, " + AssemblyRef.SystemConfigurationInstall), 
    MonitoringDescription(SR.EventLogDesc)
    ]
    public class EventLog : Component, ISupportInitialize {
        // a collection over all our entries. Since the class holds no state, we 
        // can just hand the same instance out every time.
        private EventLogEntryCollection entriesCollection; 
        // the name of the log we're reading from or writing to 
        private string logName;
        // used in monitoring for event postings. 
        private int lastSeenCount;
        // holds the machine we're on, or null if it's the local machine
        private string machineName;
 
        // the delegate to call when an event arrives
        private EntryWrittenEventHandler onEntryWrittenHandler; 
        // holds onto the handle for reading 
        private SafeEventLogReadHandle  readHandle;
        // the source name - used only when writing 
        private string sourceName;
        // holds onto the handle for writing
        private SafeEventLogWriteHandle writeHandle;
 
        private string logDisplayName;
 
        // cache system state variables 
        // the initial size of the buffer (it can be made larger if necessary)
        private const int BUF_SIZE = 40000; 
        // the number of bytes in the cache that belong to entries (not necessarily
        // the same as BUF_SIZE, because the cache only holds whole entries)
        private int bytesCached;
        // the actual cache buffer 
        private byte[] cache;
        // the number of the entry at the beginning of the cache 
        private int firstCachedEntry = -1; 
        // the number of the entry that we got out of the cache most recently
        private int lastSeenEntry; 
        // where that entry was
        private int lastSeenPos;
        //support for threadpool based deferred execution
        private ISynchronizeInvoke synchronizingObject; 

        private const string EventLogKey = "SYSTEM\\CurrentControlSet\\Services\\EventLog"; 
        internal const string DllName = "EventLogMessages.dll"; 
        private const string eventLogMutexName = "netfxeventlog.1.0";
        private const int SecondsPerDay = 60 * 60 * 24; 
        private const int DefaultMaxSize = 512*1024;
        private const int DefaultRetention = 7*SecondsPerDay;

        private const int Flag_notifying     = 0x1;           // keeps track of whether we're notifying our listeners - to prevent double notifications 
        private const int Flag_forwards      = 0x2;     // whether the cache contains entries in forwards order (true) or backwards (false)
        private const int Flag_initializing  = 0x4; 
        private const int Flag_monitoring    = 0x8; 
        private const int Flag_registeredAsListener  = 0x10;
        private const int Flag_writeGranted     = 0x20; 
        private const int Flag_disposed      = 0x100;
        private const int Flag_sourceVerified= 0x200;

        private BitVector32 boolFlags = new BitVector32(); 

        private Hashtable messageLibraries; 
        private static Hashtable listenerInfos = new Hashtable(StringComparer.OrdinalIgnoreCase); 

        private static Object s_InternalSyncObject; 
        private static Object InternalSyncObject {
            get {
                if (s_InternalSyncObject == null) {
                    Object o = new Object(); 
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                } 
                return s_InternalSyncObject; 
            }
        } 

        // Whether we need backward compatible OS patch work or not
        private static bool s_CheckedOsVersion;
        private static bool s_SkipRegPatch; 

        private static bool SkipRegPatch { 
            get { 
                if (!s_CheckedOsVersion) {
                    OperatingSystem os = Environment.OSVersion; 
                    s_SkipRegPatch = (os.Platform == PlatformID.Win32NT) && (os.Version.Major > 5);
                    s_CheckedOsVersion = true;
                }
                return s_SkipRegPatch; 
            }
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.Diagnostics.EventLog'/>
        ///       class.
        ///    </para>
        /// </devdoc> 
        public EventLog() : this("", ".", "") {
        } 
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public EventLog(string logName) : this(logName, ".", "") {
        }
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public EventLog(string logName, string machineName) : this(logName, machineName, "") {
        } 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public EventLog(string logName, string machineName, string source) {
            //look out for invalid log names 
            if (logName == null) 
                throw new ArgumentNullException("logName");
            if (!ValidLogName(logName, true)) 
                throw new ArgumentException(SR.GetString(SR.BadLogName));

            if (!SyntaxCheck.CheckMachineName(machineName))
                throw new ArgumentException(SR.GetString(SR.InvalidParameter, "machineName", machineName)); 

            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, machineName); 
            permission.Demand(); 

            this.machineName = machineName; 

            this.logName = logName;
            this.sourceName = source;
            readHandle = null; 
            writeHandle = null;
            boolFlags[Flag_forwards] = true; 
        } 

        /// <devdoc> 
        ///    <para>
        ///       Gets the contents of the event log.
        ///    </para>
        /// </devdoc> 
        [
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        MonitoringDescription(SR.LogEntries)
        ] 
        public EventLogEntryCollection Entries {
            get {
                string currentMachineName = this.machineName;
 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
                permission.Demand(); 
 
                if (entriesCollection == null)
                    entriesCollection = new EventLogEntryCollection(this); 
                return entriesCollection;
            }
        }
 
        /// <devdoc>
        ///     Gets the number of entries in the log 
        /// </devdoc> 
        internal int EntryCount {
            get { 
                if (!IsOpenForRead)
                    OpenForRead(this.machineName);
                int count;
                bool success = UnsafeNativeMethods.GetNumberOfEventLogRecords(readHandle, out count); 
                if (!success)
                    throw SharedUtils.CreateSafeWin32Exception(); 
                return count; 
            }
        } 

        /// <devdoc>
        ///     Determines whether the event log is open in either read or write access
        /// </devdoc> 
        private bool IsOpen {
            get { 
                return readHandle != null || writeHandle != null; 
            }
        } 

        /// <devdoc>
        ///     Determines whether the event log is open with read access
        /// </devdoc> 
        private bool IsOpenForRead {
            get { 
                return readHandle != null; 
            }
        } 

        /// <devdoc>
        ///     Determines whether the event log is open with write access.
        /// </devdoc> 
        private bool IsOpenForWrite {
            get { 
                return writeHandle != null; 
            }
        } 

        private static PermissionSet _GetAssertPermSet() {

            PermissionSet permissionSet = new PermissionSet(PermissionState.None); 

            // We need RegistryPermission 
            RegistryPermission registryPermission = new RegistryPermission(PermissionState.Unrestricted); 
            permissionSet.AddPermission(registryPermission);
 
            // It is not enough to just assert RegistryPermission, for some regkeys
            // we need to assert EnvironmentPermission too
            EnvironmentPermission environmentPermission = new EnvironmentPermission(PermissionState.Unrestricted);
            permissionSet.AddPermission(environmentPermission); 

            return permissionSet; 
        } 

        /// <devdoc> 
        ///    <para>
        ///    </para>
        /// </devdoc>
        [Browsable(false)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public string LogDisplayName { 
            get {
                if (logDisplayName == null) { 

                    string currentMachineName = this.machineName;
                    if (GetLogName(currentMachineName) != null) {
 
                        EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
                        permission.Demand(); 
 
                        //Check environment before looking at the registry
                        SharedUtils.CheckEnvironment(); 

                        //SECREVIEW: Note that EventLogPermission is just demmanded above
                        PermissionSet permissionSet = _GetAssertPermSet();
                        permissionSet.Assert(); 

                        RegistryKey logkey = null; 
 
                        try {
                            // we figure out what logs are on the machine by looking in the registry. 
                            logkey = GetLogRegKey(currentMachineName, false);
                            if (logkey == null)
                                throw new InvalidOperationException(SR.GetString(SR.MissingLog, GetLogName(currentMachineName), currentMachineName));
 
                            string resourceDll = (string)logkey.GetValue("DisplayNameFile");
                            if (resourceDll == null) 
                                logDisplayName = GetLogName(currentMachineName); 
                            else {
                                int resourceId = (int)logkey.GetValue("DisplayNameID"); 
                                logDisplayName = FormatMessageWrapper(resourceDll, (uint) resourceId, null);
                                if (logDisplayName == null)
                                    logDisplayName = GetLogName(currentMachineName);
                            } 
                        }
                        finally { 
                            if (logkey != null) logkey.Close(); 

                            // Revert registry and environment permission asserts 
                            CodeAccessPermission.RevertAssert();
                        }
                    }
                } 

                return logDisplayName; 
            } 
        }
 
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the name of the log to read from and write to.
        ///    </para> 
        /// </devdoc>
        [ 
        TypeConverter("System.Diagnostics.Design.LogConverter, " + AssemblyRef.SystemDesign), 
        ReadOnly(true),
        MonitoringDescription(SR.LogLog), 
        DefaultValue(""),
        RecommendedAsConfigurable(true)
        ]
        public string Log { 
            get {
                return GetLogName(this.machineName); 
            } 
            set {
                SetLogName(this.machineName, value); 
            }
        }

        private string GetLogName(string currentMachineName) 
        {
            if ((logName == null || logName.Length == 0) && sourceName != null && sourceName.Length!=0) 
                // they've told us a source, but they haven't told us a log name. 
                // try to deduce the log name from the source name.
                logName = LogNameFromSourceName(sourceName, currentMachineName); 
            return logName;
        }

        private void SetLogName(string currentMachineName, string value) 
        {
            //look out for invalid log names 
            if (value == null) 
                throw new ArgumentNullException("value");
            if (!ValidLogName(value, true)) 
                throw new ArgumentException(SR.GetString(SR.BadLogName));

            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
            permission.Demand(); 

            if (value == null) 
                value = string.Empty; 
            if (logName == null)
                logName = value; 
            else {
                if (String.Compare(logName, value, StringComparison.OrdinalIgnoreCase) == 0)
                    return;
 
                logDisplayName = null;
                logName = value; 
                if (IsOpen) { 
                    bool setEnableRaisingEvents = this.EnableRaisingEvents;
                    Close(currentMachineName); 
                    this.EnableRaisingEvents = setEnableRaisingEvents;
                }
            }
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Gets or sets the name of the computer on which to read or write events.
        ///    </para> 
        /// </devdoc>
        [
        ReadOnly(true),
        MonitoringDescription(SR.LogMachineName), 
        DefaultValue("."),
        RecommendedAsConfigurable(true) 
        ] 
        public string MachineName {
            get { 
                string currentMachineName = this.machineName;

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand(); 

                return currentMachineName; 
            } 
            set {
                if (!SyntaxCheck.CheckMachineName(value)) { 
                    throw new ArgumentException(SR.GetString(SR.InvalidProperty, "MachineName", value));
                }

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, value); 
                permission.Demand();
 
                string currentMachineName = this.machineName; 

                if (currentMachineName != null) { 
                    if (String.Compare(currentMachineName, value, StringComparison.OrdinalIgnoreCase) == 0)
                        return;

                    boolFlags[Flag_writeGranted] = false; 

                    if (IsOpen) 
                        Close(currentMachineName); 
                }
                machineName = value; 
            }
        }

        [ 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        Browsable(false), 
        ComVisible(false) 
        ]
        public long MaximumKilobytes { 
            get {
                string currentMachineName = this.machineName;

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName); 
                permission.Demand();
 
                object val = GetLogRegValue(currentMachineName, "MaxSize"); 
                if (val != null) {
                    int intval = (int) val;         // cast to an int first to unbox 
                    return ((uint)intval) / 1024;   // then convert to kilobytes
                }

                // 512k is the default value 
                return 0x200;
            } 
 
            set {
                string currentMachineName = this.machineName; 

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
                permission.Demand();
 
                // valid range is 64 KB to 4 GB
                if (value < 64 || value > 0x3FFFC0 || value % 64 != 0) 
                    throw new ArgumentOutOfRangeException("MaximumKilobytes", SR.GetString(SR.MaximumKilobytesOutOfRange)); 

                PermissionSet permissionSet = _GetAssertPermSet(); 
                permissionSet.Assert();

                long regvalue = value * 1024; // convert to bytes
                int i = unchecked((int)regvalue); 

                using (RegistryKey logkey = GetLogRegKey(currentMachineName, true)) 
                    logkey.SetValue("MaxSize", i, RegistryValueKind.DWord); 
            }
        } 

        internal Hashtable MessageLibraries {
            get {
                if (messageLibraries == null) 
                    messageLibraries = new Hashtable(StringComparer.OrdinalIgnoreCase);
                return messageLibraries; 
            } 
        }
 
        [
        Browsable(false),
        ComVisible(false)
        ] 
        public OverflowAction OverflowAction {
            get { 
                string currentMachineName = this.machineName; 

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName); 
                permission.Demand();

                object retentionobj  = GetLogRegValue(currentMachineName, "Retention");
                if (retentionobj  != null) { 
                    int retention = (int) retentionobj;
                    if (retention == 0) 
                        return OverflowAction.OverwriteAsNeeded; 
                    else if (retention == -1)
                        return OverflowAction.DoNotOverwrite; 
                    else
                        return OverflowAction.OverwriteOlder;
                }
 
                // default value as listed in MSDN
                return OverflowAction.OverwriteOlder; 
            } 
        }
 
        [
        Browsable(false),
        ComVisible(false)
        ] 
        public int MinimumRetentionDays {
            get { 
                string currentMachineName = this.machineName; 

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName); 
                permission.Demand();

                object retentionobj  = GetLogRegValue(currentMachineName, "Retention");
                if (retentionobj  != null) { 
                    int retention = (int) retentionobj;
                    if (retention == 0 || retention == -1) 
                        return retention; 
                    else
                        return (int) (((double) retention) / SecondsPerDay); 
                }
                return 7;
            }
        } 

        /// <devdoc> 
        /// </devdoc> 
        [
        Browsable(false), 
        MonitoringDescription(SR.LogMonitoring),
        DefaultValue(false)
        ]
        public bool EnableRaisingEvents { 
            get {
                string currentMachineName = this.machineName; 
 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand(); 

                return boolFlags[Flag_monitoring];
            }
            set { 
                string currentMachineName = this.machineName;
 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName); 
                permission.Demand();
 
                if (this.DesignMode)
                    this.boolFlags[Flag_monitoring] = value;
                else {
                    if (value) 
                        StartRaisingEvents(currentMachineName, GetLogName(currentMachineName));
                    else 
                        StopRaisingEvents(/*currentMachineName,*/ GetLogName(currentMachineName)); 
                }
            } 
        }

        private int OldestEntryNumber {
            get { 
                if (!IsOpenForRead)
                    OpenForRead(this.machineName); 
                int[] num = new int[1]; 
                bool success = UnsafeNativeMethods.GetOldestEventLogRecord(readHandle, num);
                if (!success) 
                    throw SharedUtils.CreateSafeWin32Exception();
                int oldest = num[0];

                // When the event log is empty, GetOldestEventLogRecord returns 0. 
                // But then after an entry is written, it returns 1. We need to go from
                // the last oldest to the current. 
                if (oldest == 0) 
                    oldest = 1;
 
                return oldest;
            }
        }
 
        internal SafeEventLogReadHandle  ReadHandle {
            get { 
                if (!IsOpenForRead) 
                    OpenForRead(this.machineName);
                return readHandle; 
            }
        }

        /// <devdoc> 
        ///    <para>
        ///       Represents the object used to marshal the event handler 
        ///       calls issued as a result of an <see cref='System.Diagnostics.EventLog'/> 
        ///       change.
        ///    </para> 
        /// </devdoc>
        [
        Browsable(false),
        DefaultValue(null), 
        MonitoringDescription(SR.LogSynchronizingObject)
        ] 
        public ISynchronizeInvoke SynchronizingObject { 
        [HostProtection(Synchronization=true)]
            get { 
                string currentMachineName = this.machineName;

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand(); 
                if (this.synchronizingObject == null && DesignMode) {
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                    if (host != null) { 
                        object baseComponent = host.RootComponent;
                        if (baseComponent != null && baseComponent is ISynchronizeInvoke) 
                            this.synchronizingObject = (ISynchronizeInvoke)baseComponent;
                    }
                }
 
                return this.synchronizingObject;
            } 
 
            set {
                this.synchronizingObject = value; 
            }
        }

        /// <devdoc> 
        ///    <para>
        ///       Gets or 
        ///       sets the application name (source name) to register and use when writing to the event log. 
        ///    </para>
        /// </devdoc> 
        [
        ReadOnly(true),
        TypeConverter("System.Diagnostics.Design.StringValueConverter, " + AssemblyRef.SystemDesign),
        MonitoringDescription(SR.LogSource), 
        DefaultValue(""),
        RecommendedAsConfigurable(true) 
        ] 
        public string Source {
            get { 
                string currentMachineName = this.machineName;

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand(); 
                return sourceName;
            } 
            set { 
                if (value == null)
                    value = string.Empty; 

                // this 254 limit is the max length of a registry key.
                if (value.Length + EventLogKey.Length > 254)
                    throw new ArgumentException(SR.GetString(SR.ParameterTooLong, "source", 254 - EventLogKey.Length)); 

                string currentMachineName = this.machineName; 
 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand(); 
                if (sourceName == null)
                    sourceName = value;
                else {
                    if (String.Compare(sourceName, value, StringComparison.OrdinalIgnoreCase) == 0) 
                        return;
 
                    sourceName = value; 
                    if (IsOpen) {
                        bool setEnableRaisingEvents = this.EnableRaisingEvents; 
                        Close(currentMachineName);
                        this.EnableRaisingEvents = setEnableRaisingEvents;
                    }
                } 
                //Trace("Set_Source", "Setting source to " + (sourceName == null ? "null" : sourceName));
            } 
        } 

        [HostProtection(Synchronization=true)] 
        private static void AddListenerComponent(EventLog component, string compMachineName, string compLogName) {
            lock (InternalSyncObject) {
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::AddListenerComponent(" + compLogName + ")");
 
                LogListeningInfo info = (LogListeningInfo) listenerInfos[compLogName];
                if (info != null) { 
                    Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::AddListenerComponent: listener already active."); 
                    info.listeningComponents.Add(component);
                    return; 
                }

                info = new LogListeningInfo();
                info.listeningComponents.Add(component); 

                info.handleOwner = new EventLog(); 
                info.handleOwner.MachineName = compMachineName; 
                info.handleOwner.Log = compLogName;
 
                // create a system event
                SafeEventHandle notifyEventHandle = SafeEventHandle.CreateEvent(NativeMethods.NullHandleRef, false, false, null);
                if (notifyEventHandle.IsInvalid) {
                    Win32Exception e = null; 
                    if (Marshal.GetLastWin32Error() != 0) {
                        e = SharedUtils.CreateSafeWin32Exception(); 
                    } 
                    throw new InvalidOperationException(SR.GetString(SR.NotifyCreateFailed), e);
                } 

                // tell the event log system about it
                bool success = UnsafeNativeMethods.NotifyChangeEventLog(info.handleOwner.ReadHandle, notifyEventHandle);
                if (!success) 
                    throw new InvalidOperationException(SR.GetString(SR.CantMonitorEventLog), SharedUtils.CreateSafeWin32Exception());
 
                info.waitHandle = new EventLogWaitHandle(notifyEventHandle); 
                info.registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(info.waitHandle, new WaitOrTimerCallback(StaticCompletionCallback), info, -1, false);
 
                listenerInfos[compLogName] = info;
            }
        }
 
        /// <devdoc>
        ///    <para> 
        ///       Occurs when an entry is written to the event log. 
        ///    </para>
        /// </devdoc> 
        [MonitoringDescription(SR.LogEntryWritten)]
        public event EntryWrittenEventHandler EntryWritten {
            add {
                string currentMachineName = this.machineName; 

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName); 
                permission.Demand(); 

                onEntryWrittenHandler += value; 
            }
            remove {
                string currentMachineName = this.machineName;
 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
                permission.Demand(); 
 
                onEntryWrittenHandler -= value;
            } 
        }

        /// <devdoc>
        /// </devdoc> 
        public void BeginInit() {
            string currentMachineName = this.machineName; 
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
            permission.Demand(); 

            if (boolFlags[Flag_initializing]) throw new InvalidOperationException(SR.GetString(SR.InitTwice));
            boolFlags[Flag_initializing] = true;
            if (boolFlags[Flag_monitoring]) 
                StopListening(GetLogName(currentMachineName));
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Clears
        ///       the event log by removing all entries from it.
        ///    </para>
        /// </devdoc> 
        public void Clear() {
            string currentMachineName = this.machineName; 
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
            permission.Demand(); 

            if (!IsOpenForRead)
                OpenForRead(currentMachineName);
            bool success = UnsafeNativeMethods.ClearEventLog(readHandle, NativeMethods.NullHandleRef); 
            if (!success) {
                // Ignore file not found errors.  ClearEventLog seems to try to delete the file where the event log is 
                // stored.  If it can't find it, it gives an error. 
                int error = Marshal.GetLastWin32Error();
                if (error != NativeMethods.ERROR_FILE_NOT_FOUND) 
                    throw SharedUtils.CreateSafeWin32Exception();
            }

            // now that we've cleared the event log, we need to re-open our handles, because 
            // the internal state of the event log has changed.
            Reset(currentMachineName); 
        } 

        /// <devdoc> 
        ///    <para>
        ///       Closes the event log and releases read and write handles.
        ///    </para>
        /// </devdoc> 
        public void Close() {
            Close(this.machineName); 
        } 

        private void Close(string currentMachineName) { 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
            permission.Demand();

            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::Close"); 
            //Trace("Close", "Closing the event log");
            if (readHandle != null) { 
                try { 
                    readHandle.Close();
                } 
                catch (IOException) {
                    throw SharedUtils.CreateSafeWin32Exception();
                }
                readHandle = null; 
                //Trace("Close", "Closed read handle");
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::Close: closed read handle"); 
            } 
            if (writeHandle != null) {
                try { 
                    writeHandle.Close();
                }
                catch (IOException) {
                    throw SharedUtils.CreateSafeWin32Exception(); 
                }
                writeHandle = null; 
                //Trace("Close", "Closed write handle"); 
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::Close: closed write handle");
            } 
            if (boolFlags[Flag_monitoring])
                StopRaisingEvents(/*currentMachineName,*/ GetLogName(currentMachineName));

            if (messageLibraries != null) { 
                foreach (SafeLibraryHandle handle in messageLibraries.Values)
                    handle.Close(); 
 
                messageLibraries = null;
            } 

            boolFlags[Flag_sourceVerified] = false;
        }
 

        /// <internalonly/> 
        /// <devdoc> 
        ///     Called when the threadpool is ready for us to handle a status change.
        /// </devdoc> 
        private void CompletionCallback(object context)  {
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: starting at " + lastSeenCount.ToString(CultureInfo.InvariantCulture));
            lock (this) {
                if (boolFlags[Flag_notifying]) { 
                    // don't do double notifications.
                    Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: aborting because we're already notifying."); 
                    return; 
                }
                boolFlags[Flag_notifying] = true; 
            }

            int i = lastSeenCount;
            try { 
                // NOTE, [....]: We have a double loop here so that we access the
                // EntryCount property as infrequently as possible. (It may be expensive 
                // to get the property.) Even though there are two loops, they will together 
                // only execute as many times as (final value of EntryCount) - lastSeenCount.
                int oldest = OldestEntryNumber; 
                int count = EntryCount + oldest;
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: OldestEntryNumber is " + OldestEntryNumber + ", EntryCount is " + EntryCount);
                while (i < count) {
                    while (i < count) { 
                        EventLogEntry entry = GetEntryWithOldest(i);
                        if (this.SynchronizingObject != null && this.SynchronizingObject.InvokeRequired) 
                            this.SynchronizingObject.BeginInvoke(this.onEntryWrittenHandler, new object[]{this, new EntryWrittenEventArgs(entry)}); 
                        else
                           onEntryWrittenHandler(this, new EntryWrittenEventArgs(entry)); 

                        i++;
                    }
                    oldest = OldestEntryNumber; 
                    count = EntryCount + oldest;
                } 
            } 
            catch (Exception e) {
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: Caught exception notifying event handlers: " + e.ToString()); 
            }
            catch {
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: Caught exception notifying event handlers.");
            } 

            // if the user cleared the log while we were receiving events, the call to GetEntryWithOldest above could have 
            // thrown an exception and i could be too large.  Make sure we don't set lastSeenCount to something bogus. 
            int newCount = EntryCount + OldestEntryNumber;
            if (i > newCount) 
                lastSeenCount = newCount;
            else
                lastSeenCount = i;
 
            lock (this) {
                boolFlags[Flag_notifying] = false; 
            } 
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: finishing at " + lastSeenCount.ToString(CultureInfo.InvariantCulture));
        } 

        /// <devdoc>
        ///    <para> Establishes an application, using the
        ///       specified <see cref='System.Diagnostics.EventLog.Source'/> , as a valid event source for 
        ///       writing entries
        ///       to a log on the local computer. This method 
        ///       can also be used to create 
        ///       a new custom log on the local computer.</para>
        /// </devdoc> 
        public static void CreateEventSource(string source, string logName) {
            CreateEventSource(new EventSourceCreationData(source, logName, "."));
        }
 
        /// <devdoc>
        ///    <para>Establishes an application, using the specified 
        ///    <see cref='System.Diagnostics.EventLog.Source'/> as a valid event source for writing 
        ///       entries to a log on the computer
        ///       specified by <paramref name="machineName"/>. This method can also be used to create a new 
        ///       custom log on the given computer.</para>
        /// </devdoc>
        [Obsolete("This method has been deprecated.  Please use System.Diagnostics.EventLog.CreateEventSource(EventSourceCreationData sourceData) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public static void CreateEventSource(string source, string logName, string machineName) { 
            CreateEventSource(new EventSourceCreationData(source, logName, machineName));
        } 
 
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)] 
        public static void CreateEventSource(EventSourceCreationData sourceData) {
            if (sourceData == null)
                throw new ArgumentNullException("sourceData");
 
            string logName = sourceData.LogName;
            string source = sourceData.Source; 
            string machineName = sourceData.MachineName; 

            // verify parameters 
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "CreateEventSource: Checking arguments");
            if (!SyntaxCheck.CheckMachineName(machineName)) {
                throw new ArgumentException(SR.GetString(SR.InvalidParameter, "machineName", machineName));
            } 
            if (logName == null || logName.Length==0)
                logName = "Application"; 
            if (!ValidLogName(logName, false)) 
                throw new ArgumentException(SR.GetString(SR.BadLogName));
            if (source == null || source.Length==0) 
                throw new ArgumentException(SR.GetString(SR.MissingParameter, "source"));
            if (source.Length + EventLogKey.Length > 254)
                throw new ArgumentException(SR.GetString(SR.ParameterTooLong, "source", 254 - EventLogKey.Length));
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName);
            permission.Demand(); 
 
            Mutex mutex = null;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                SharedUtils.EnterMutex(eventLogMutexName, ref mutex);
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "CreateEventSource: Calling SourceExists");
                if (SourceExists(source, machineName)) { 
                    Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "CreateEventSource: SourceExists returned true");
                    // don't let them register a source if it already exists 
                    // this makes more sense than just doing it anyway, because the source might 
                    // be registered under a different log name, and we don't want to create
                    // duplicates. 
                    if (".".Equals(machineName))
                        throw new ArgumentException(SR.GetString(SR.LocalSourceAlreadyExists, source));
                    else
                        throw new ArgumentException(SR.GetString(SR.SourceAlreadyExists, source, machineName)); 
                }
 
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "CreateEventSource: Getting DllPath"); 

                //SECREVIEW: Note that EventLog permission is demanded above. 
                PermissionSet permissionSet = _GetAssertPermSet();
                permissionSet.Assert();

                RegistryKey baseKey = null; 
                RegistryKey eventKey = null;
                RegistryKey logKey = null; 
                RegistryKey sourceLogKey = null; 
                RegistryKey sourceKey = null;
                try { 
                    Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "CreateEventSource: Getting local machine regkey");
                    if (machineName == ".")
                        baseKey = Registry.LocalMachine;
                    else 
                        baseKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machineName);
 
                    eventKey = baseKey.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\EventLog", true); 
                    if (eventKey == null) {
                        if (!".".Equals(machineName)) 
                            throw new InvalidOperationException(SR.GetString(SR.RegKeyMissing, "SYSTEM\\CurrentControlSet\\Services\\EventLog", logName, source, machineName));
                        else
                            throw new InvalidOperationException(SR.GetString(SR.LocalRegKeyMissing, "SYSTEM\\CurrentControlSet\\Services\\EventLog", logName, source));
                    } 

                    // The event log system only treats the first 8 characters of the log name as 
                    // significant. If they're creating a new log, but that new log has the same 
                    // first 8 characters as another log, the system will think they're the same.
                    // Throw an exception to let them know. 
                    logKey = eventKey.OpenSubKey(logName, true);
                    if (logKey == null && logName.Length >= 8) {

                        // check for Windows embedded logs file names 
                        string logNameFirst8 = logName.Substring(0,8);
                        if ( string.Compare(logNameFirst8,"AppEvent",StringComparison.OrdinalIgnoreCase) ==0  || 
                             string.Compare(logNameFirst8,"SecEvent",StringComparison.OrdinalIgnoreCase) ==0  || 
                             string.Compare(logNameFirst8,"SysEvent",StringComparison.OrdinalIgnoreCase) ==0 )
                            throw new ArgumentException(SR.GetString(SR.InvalidCustomerLogName, logName)); 

                        string sameLogName = FindSame8FirstCharsLog(eventKey, logName);
                        if ( sameLogName != null )
                            throw new ArgumentException(SR.GetString(SR.DuplicateLogName, logName, sameLogName)); 
                    }
 
                    bool createLogKey = (logKey == null); 
                    if (createLogKey) {
                        if (SourceExists(logName, machineName)) { 
                            // don't let them register a log name that already
                            // exists as source name, a source with the same
                            // name as the log will have to be created by default
                            if (".".Equals(machineName)) 
                                throw new ArgumentException(SR.GetString(SR.LocalLogAlreadyExistsAsSource, logName));
                            else 
                                throw new ArgumentException(SR.GetString(SR.LogAlreadyExistsAsSource, logName, machineName)); 
                        }
 
                        logKey = eventKey.CreateSubKey(logName);

                        // NOTE: We shouldn't set "Sources" explicitly, the OS will automatically set it.
                        // The EventLog service doesn't use it for anything it is just an helping hand for event viewer filters. 
                        // Writing this value explicitly might confuse the service as it might perceive it as a change and
                        // start initializing again 
 
                        if (!SkipRegPatch)
                            logKey.SetValue("Sources", new string[] {logName, source}, RegistryValueKind.MultiString); 

                        SetSpecialLogRegValues(logKey, logName);

                        // A source with the same name as the log has to be created 
                        // by default. It is the behavior expected by EventLog API.
                        sourceLogKey = logKey.CreateSubKey(logName); 
                        SetSpecialSourceRegValues(sourceLogKey, sourceData); 
                    }
 
                    if (logName != source) {
                        if (!createLogKey) {
                            SetSpecialLogRegValues(logKey, logName);
 
                            if (!SkipRegPatch) {
                                string[] sources = logKey.GetValue("Sources") as string[]; 
                                if (sources == null) 
                                    logKey.SetValue("Sources", new string[] {logName, source}, RegistryValueKind.MultiString);
                                else { 
                                    // We have a race with OS EventLog here.
                                    // OS might update Sources as well. We should avoid writing the
                                    // source name if OS beats us.
                                    if( Array.IndexOf(sources, source) == -1) { 
                                        string[] newsources = new string[sources.Length + 1];
                                        Array.Copy(sources, newsources, sources.Length); 
                                        newsources[sources.Length] = source; 
                                        logKey.SetValue("Sources", newsources, RegistryValueKind.MultiString);
                                    } 
                                }
                            }
                        }
 
                        sourceKey = logKey.CreateSubKey(source);
                        SetSpecialSourceRegValues(sourceKey, sourceData); 
                    } 
                }
                finally { 
                    if (baseKey != null)
                        baseKey.Close();

                    if (eventKey != null) 
                        eventKey.Close();
 
                    if (logKey != null) { 
                        logKey.Flush();
                        logKey.Close(); 
                    }

                    if (sourceLogKey != null) {
                        sourceLogKey.Flush(); 
                        sourceLogKey.Close();
                    } 
 
                    if (sourceKey != null) {
                        sourceKey.Flush(); 
                        sourceKey.Close();
                    }

                    // Revert registry and environment permission asserts 
                    CodeAccessPermission.RevertAssert();
                } 
            } 
            finally {
                if (mutex != null) { 
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
            } 
        }
 
        /// <devdoc> 
        ///    <para>
        ///       Removes 
        ///       an event
        ///       log from the local computer.
        ///    </para>
        /// </devdoc> 
        public static void Delete(string logName) {
            Delete(logName, "."); 
        } 

        /// <devdoc> 
        ///    <para>
        ///       Removes
        ///       an
        ///       event 
        ///       log from the specified computer.
        ///    </para> 
        /// </devdoc> 
        public static void Delete(string logName, string machineName) {
 
            if (!SyntaxCheck.CheckMachineName(machineName))
                throw new ArgumentException(SR.GetString(SR.InvalidParameterFormat, "machineName"));
            if (logName == null || logName.Length==0)
                throw new ArgumentException(SR.GetString(SR.NoLogName)); 
            if (!ValidLogName(logName, false))
                throw new InvalidOperationException(SR.GetString(SR.BadLogName)); 
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName);
            permission.Demand(); 

            //Check environment before even trying to play with the registry
            SharedUtils.CheckEnvironment();
 
            //SECREVIEW: Note that EventLog permission is demanded above.
            PermissionSet permissionSet = _GetAssertPermSet(); 
            permissionSet.Assert(); 

            RegistryKey eventlogkey = null; 

            Mutex mutex = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                SharedUtils.EnterMutex(eventLogMutexName, ref mutex);
 
                try { 
                    eventlogkey  = GetEventLogRegKey(machineName, true);
                    if (eventlogkey  == null) { 
                        // there's not even an event log service on the machine.
                        // or, more likely, we don't have the access to read the registry.
                        throw new InvalidOperationException(SR.GetString(SR.RegKeyNoAccess, "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\EventLog", machineName));
                    } 

                    using (RegistryKey logKey = eventlogkey.OpenSubKey(logName)) { 
                        if (logKey == null) 
                            throw new InvalidOperationException(SR.GetString(SR.MissingLog, logName, machineName));
 
                        //clear out log before trying to delete it
                        //that way, if we can't delete the log file, no entries will persist because it has been cleared
                        EventLog logToClear = new EventLog();
                        try { 
                            logToClear.Log = logName;
                            logToClear.MachineName = machineName; 
                            logToClear.Clear(); 
                        }
                        finally { 
                            logToClear.Close();
                        }

                        // 

 
                        string filename = null; 
                        try {
                            //most of the time, the "File" key does not exist, but we'll still give it a whirl 
                            filename = (string) logKey.GetValue("File");
                        }
                        catch { }
                        if (filename != null) { 
                            try {
                                File.Delete(filename); 
                            } 
                            catch { }
                        } 
                    }

                    // now delete the registry entry
                    eventlogkey.DeleteSubKeyTree(logName); 
                }
                finally { 
                    if (eventlogkey != null) eventlogkey.Close(); 

                    // Revert registry and environment permission asserts 
                    CodeAccessPermission.RevertAssert();
                }
            }
            finally { 
                if (mutex != null) mutex.ReleaseMutex();
            } 
        } 

        /// <devdoc> 
        ///    <para>
        ///       Removes the event source
        ///       registration from the event log of the local computer.
        ///    </para> 
        /// </devdoc>
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)] 
        public static void DeleteEventSource(string source) {
            DeleteEventSource(source, "."); 
        }

        /// <devdoc>
        ///    <para> 
        ///       Removes
        ///       the application's event source registration from the specified computer. 
        ///    </para> 
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public static void DeleteEventSource(string source, string machineName) {
            if (!SyntaxCheck.CheckMachineName(machineName)) {
                throw new ArgumentException(SR.GetString(SR.InvalidParameter, "machineName", machineName)); 
            }
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName); 
            permission.Demand();
 
            //Check environment before looking at the registry
            SharedUtils.CheckEnvironment();

            //SECREVIEW: Note that EventLog permission is demanded above. 
            PermissionSet permissionSet = _GetAssertPermSet();
            permissionSet.Assert(); 
 
            Mutex mutex = null;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                SharedUtils.EnterMutex(eventLogMutexName, ref mutex);
                RegistryKey key = null;
 
                // First open the key read only so we can do some checks.  This is important so we get the same
                // exceptions even if we don't have write access to the reg key. 
                using (key = FindSourceRegistration(source, machineName, true)) { 
                    if (key == null) {
                        if (machineName == null) 
                            throw new ArgumentException(SR.GetString(SR.LocalSourceNotRegistered, source));
                        else
                            throw new ArgumentException(SR.GetString(SR.SourceNotRegistered, source, machineName, "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\EventLog"));
                    } 

                    // Check parent registry key (Event Log Name) and if it's equal to source, then throw an exception. 
                    // The reason: each log registry key must always contain subkey (i.e. source) with the same name. 
                    string keyname = key.Name;
                    int index = keyname.LastIndexOf('\\'); 
                    if ( string.Compare(keyname, index+1, source, 0, keyname.Length - index, StringComparison.Ordinal) == 0 )
                        throw new InvalidOperationException(SR.GetString(SR.CannotDeleteEqualSource, source));
                }
 
                try {
                    // now open it read/write to try to do the actual delete 
                    key = FindSourceRegistration(source, machineName, false); 
                    key.DeleteSubKeyTree(source);
 
                    if (!SkipRegPatch) {
                        string[] sources = (string[]) key.GetValue("Sources");
                        ArrayList newsources = new ArrayList(sources.Length - 1);
 
                        for (int i=0; i<sources.Length; i++) {
                            if (sources[i] != source) { 
                                newsources.Add(sources[i]); 
                            }
                        } 
                        string[] newsourcesArray = new string[newsources.Count];
                        newsources.CopyTo(newsourcesArray);

                        key.SetValue("Sources", newsourcesArray, RegistryValueKind.MultiString); 
                    }
                } 
                finally { 
                    if (key != null) {
                        key.Flush(); 
                        key.Close();
                    }

                    // Revert registry and environment permission asserts 
                    CodeAccessPermission.RevertAssert();
                } 
            } 
            finally {
                if (mutex != null) 
                    mutex.ReleaseMutex();
            }
        }
 
        /// <devdoc>
        /// </devdoc> 
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                //Dispose unmanaged and managed resources 
                if (IsOpen)
                    Close();
            }
            else { 
                //Dispose unmanaged resources
                if (readHandle != null) 
                    readHandle.Close(); 

                if (writeHandle != null) 
                    writeHandle.Close();

                messageLibraries = null;
            } 

            this.boolFlags[Flag_disposed] = true; 
            base.Dispose(disposing); 
        }
 
        /// <devdoc>
        /// </devdoc>
        public void EndInit() {
            string currentMachineName = this.machineName; 

            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName); 
            permission.Demand(); 

            boolFlags[Flag_initializing] = false; 
            if (boolFlags[Flag_monitoring])
                StartListening(currentMachineName, GetLogName(currentMachineName));
        }
 
        /// <devdoc>
        ///    <para> 
        ///       Determines whether the log 
        ///       exists on the local computer.
        ///    </para> 
        /// </devdoc>
        public static bool Exists(string logName) {
            return Exists(logName, ".");
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Determines whether the
        ///       log exists on the specified computer. 
        ///    </para>
        /// </devdoc>
        public static bool Exists(string logName, string machineName) {
            if (!SyntaxCheck.CheckMachineName(machineName)) 
                throw new ArgumentException(SR.GetString(SR.InvalidParameterFormat, "machineName"));
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName); 
            permission.Demand();
 
            if (logName == null || logName.Length==0)
                return false;

            //Check environment before looking at the registry 
            SharedUtils.CheckEnvironment();
 
            //SECREVIEW: Note that EventLog permission is demanded above. 
            PermissionSet permissionSet = _GetAssertPermSet();
            permissionSet.Assert(); 

            RegistryKey eventkey = null;
            RegistryKey logKey = null;
 
            try {
                eventkey = GetEventLogRegKey(machineName, false); 
                if (eventkey == null) 
                    return false;
 
                logKey = eventkey.OpenSubKey(logName, false);         // try to find log file key immediately.
                return (logKey != null );
            }
            finally { 
                if (eventkey != null) eventkey.Close();
                if (logKey != null) logKey.Close(); 
 
                // Revert registry and environment permission asserts
                CodeAccessPermission.RevertAssert(); 
            }
        }

 
        // Try to find log file name with the same 8 first characters.
        // Returns 'null' if no "same first 8 chars" log is found.   logName.Length must be > 7 
        private static string FindSame8FirstCharsLog(RegistryKey keyParent, string logName) { 

            string logNameFirst8 = logName.Substring(0, 8); 
            string[] logNames = keyParent.GetSubKeyNames();

            for (int i = 0; i < logNames.Length; i++) {
                string currentLogName = logNames[i]; 
                if ( currentLogName.Length >= 8  &&
                     string.Compare(currentLogName.Substring(0, 8), logNameFirst8, StringComparison.OrdinalIgnoreCase) == 0) 
                    return currentLogName; 
            }
 
            return null;   // not found
        }

        /// <devdoc> 
        ///     Gets a RegistryKey that points to the LogName entry in the registry that is
        ///     the parent of the given source on the given machine, or null if none is found. 
        /// </devdoc> 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        private static RegistryKey FindSourceRegistration(string source, string machineName, bool readOnly) {
            if (source != null && source.Length != 0) {

                //Check environment before looking at the registry 
                SharedUtils.CheckEnvironment();
 
                //SECREVIEW: Any call to this function must have demmanded 
                //                         EventLogPermission before.
                PermissionSet permissionSet = _GetAssertPermSet(); 
                permissionSet.Assert();

                RegistryKey eventkey = null;
                try { 
                    eventkey = GetEventLogRegKey(machineName, !readOnly);
                    if (eventkey == null) { 
                        // there's not even an event log service on the machine. 
                        // or, more likely, we don't have the access to read the registry.
                        return null; 
                    }

                    StringBuilder inaccessibleLogs = null;
 
                    // Most machines will return only { "Application", "System", "Security" },
                    // but you can create your own if you want. 
                    string[] logNames = eventkey.GetSubKeyNames(); 
                    for (int i = 0; i < logNames.Length; i++) {
                        // see if the source is registered in this log. 
                        // NOTE: A source name must be unique across ALL LOGS!
                        RegistryKey sourceKey = null;
                        try {
                            RegistryKey logKey = eventkey.OpenSubKey(logNames[i], /*writable*/!readOnly); 
                            if (logKey != null) {
                                sourceKey = logKey.OpenSubKey(source, /*writable*/!readOnly); 
                                if (sourceKey != null) { 
                                    // found it
                                    return logKey; 
                                }
                            }
                            // else logKey is null, so we don't need to Close it
                        } 
                        catch (UnauthorizedAccessException) {
                            if (inaccessibleLogs == null) { 
                                inaccessibleLogs = new StringBuilder(logNames[i]); 
                            }
                            else { 
                                inaccessibleLogs.Append(", ");
                                inaccessibleLogs.Append(logNames[i]);
                            }
                        } 
                        catch (SecurityException) {
                            if (inaccessibleLogs == null) { 
                                inaccessibleLogs = new StringBuilder(logNames[i]); 
                            }
                            else { 
                                inaccessibleLogs.Append(", ");
                                inaccessibleLogs.Append(logNames[i]);
                            }
                        } 
                        finally {
                            if (sourceKey != null) sourceKey.Close(); 
                        } 
                    }
 
                    if (inaccessibleLogs != null)
                        throw new SecurityException(SR.GetString(SR.SomeLogsInaccessible, inaccessibleLogs.ToString()));

                } 
                finally {
                    if (eventkey != null) eventkey.Close(); 
 
                    // Revert registry and environment permission asserts
                    CodeAccessPermission.RevertAssert(); 
                }
                // didn't see it anywhere
            }
 
            return null;
        } 
 
        internal string FormatMessageWrapper(string dllNameList, uint messageNum, string[] insertionStrings) {
            if (dllNameList == null) 
                return null;

            if (insertionStrings == null)
                insertionStrings = new string[0]; 

            string[] listDll = dllNameList.Split(';'); 
 
            // Find first mesage in DLL list
            foreach ( string dllName in  listDll) { 
                if (dllName == null || dllName.Length == 0)
                    continue;

                SafeLibraryHandle hModule = null; 

                // if the EventLog is open, then we want to cache the library in our hashtable.  Otherwise 
                // we'll just load it and free it after we're done. 
                if (IsOpen) {
                    hModule = MessageLibraries[dllName] as SafeLibraryHandle; 

                    if (hModule == null || hModule.IsInvalid) {
                        hModule = SafeLibraryHandle.LoadLibraryEx(dllName, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
                        MessageLibraries[dllName] = hModule; 
                    }
                } 
                else { 
                    hModule = SafeLibraryHandle.LoadLibraryEx(dllName, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
                } 

                if (hModule.IsInvalid)
                    continue;
 
                string msg = null;
                try { 
                    msg = TryFormatMessage(hModule, messageNum, insertionStrings); 
                }
                finally { 
                    if (!IsOpen) {
                        hModule.Close();
                    }
                } 

                if ( msg != null ) { 
                    return msg; 
                }
 
            }
            return null;
        }
 
        /// <devdoc>
        ///     Gets an array of EventLogEntry's, one for each entry in the log. 
        /// </devdoc> 
        internal EventLogEntry[] GetAllEntries() {
            // we could just call getEntryAt() on all the entries, but it'll be faster 
            // if we grab multiple entries at once.
            string currentMachineName = this.machineName;

            if (!IsOpenForRead) 
                OpenForRead(currentMachineName);
 
            EventLogEntry[] entries = new EventLogEntry[EntryCount]; 
            int idx = 0;
            int oldestEntry = OldestEntryNumber; 

            int[] bytesRead = new int[1];
            int[] minBytesNeeded = new int[] { BUF_SIZE};
            int error = 0; 
            while (idx < entries.Length) {
                byte[] buf = new byte[BUF_SIZE]; 
                bool success = UnsafeNativeMethods.ReadEventLog(readHandle, NativeMethods.FORWARDS_READ | NativeMethods.SEEK_READ, 
                                                      oldestEntry+idx, buf, buf.Length, bytesRead, minBytesNeeded);
                if (!success) { 
                    error = Marshal.GetLastWin32Error();
                    // NOTE, [....]: ERROR_PROC_NOT_FOUND used to get returned, but I think that
                    // was because I was calling GetLastError directly instead of GetLastWin32Error.
                    // Making the buffer bigger and trying again seemed to work. I've removed the check 
                    // for ERROR_PROC_NOT_FOUND because I don't think it's necessary any more, but
                    // I can't prove it... 
                    Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "Error from ReadEventLog is " + error.ToString(CultureInfo.InvariantCulture)); 
#if !RETRY_ON_ALL_ERRORS
                    if (error == NativeMethods.ERROR_INSUFFICIENT_BUFFER || error == NativeMethods.ERROR_EVENTLOG_FILE_CHANGED) { 
#endif
                        if (error == NativeMethods.ERROR_EVENTLOG_FILE_CHANGED) {
                            // somewhere along the way the event log file changed - probably it
                            // got cleared while we were looping here. Reset the handle and 
                            // try again.
                            Reset(currentMachineName); 
                        } 
                        // try again with a bigger buffer if necessary
                        else if (minBytesNeeded[0] > buf.Length) { 
                            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "Increasing buffer size from " + buf.Length.ToString(CultureInfo.InvariantCulture) + " to " + minBytesNeeded[0].ToString(CultureInfo.InvariantCulture) + " bytes");
                            buf = new byte[minBytesNeeded[0]];
                        }
                        success = UnsafeNativeMethods.ReadEventLog(readHandle, NativeMethods.FORWARDS_READ | NativeMethods.SEEK_READ, 
                                                         oldestEntry+idx, buf, buf.Length, bytesRead, minBytesNeeded);
                        if (!success) 
                            // we'll just stop right here. 
                            break;
#if !RETRY_ON_ALL_ERRORS 
                    }
                    else {
                        break;
                    } 
#endif
                    error = 0; 
                } 
                entries[idx] = new EventLogEntry(buf, 0, this);
                int sum = IntFrom(buf, 0); 
                idx++;
                while (sum < bytesRead[0] && idx < entries.Length) {
                    entries[idx] = new EventLogEntry(buf, sum, this);
                    sum += IntFrom(buf, sum); 
                    idx++;
                } 
            } 
            if (idx != entries.Length) {
                if (error != 0) 
                    throw new InvalidOperationException(SR.GetString(SR.CantRetrieveEntries), SharedUtils.CreateSafeWin32Exception(error));
                else
                    throw new InvalidOperationException(SR.GetString(SR.CantRetrieveEntries));
            } 
            return entries;
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Searches for all event logs on the local computer and
        ///       creates an array of <see cref='System.Diagnostics.EventLog'/>
        ///       objects to contain the
        ///       list. 
        ///    </para>
        /// </devdoc> 
        public static EventLog[] GetEventLogs() { 
            return GetEventLogs(".");
        } 

        /// <devdoc>
        ///    <para>
        ///       Searches for all event logs on the given computer and 
        ///       creates an array of <see cref='System.Diagnostics.EventLog'/>
        ///       objects to contain the 
        ///       list. 
        ///    </para>
        /// </devdoc> 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static EventLog[] GetEventLogs(string machineName) {
            if (!SyntaxCheck.CheckMachineName(machineName)) { 
                throw new ArgumentException(SR.GetString(SR.InvalidParameter, "machineName", machineName));
            } 
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName);
            permission.Demand(); 

            //Check environment before looking at the registry
            SharedUtils.CheckEnvironment();
 
            string[] logNames = new string[0];
            //SECREVIEW: Note that EventLogPermission is just demmanded above 
            PermissionSet permissionSet = _GetAssertPermSet(); 
            permissionSet.Assert();
 
            RegistryKey eventkey = null;
            try {
                // we figure out what logs are on the machine by looking in the registry.
                eventkey = GetEventLogRegKey(machineName, false); 
                if (eventkey == null)
                    // there's not even an event log service on the machine. 
                    // or, more likely, we don't have the access to read the registry. 
                    throw new InvalidOperationException(SR.GetString(SR.RegKeyMissingShort, EventLogKey, machineName));
                // Most machines will return only { "Application", "System", "Security" }, 
                // but you can create your own if you want.
                logNames = eventkey.GetSubKeyNames();
            }
            finally { 
                if (eventkey != null) eventkey.Close();
                // Revert registry and environment permission asserts 
                CodeAccessPermission.RevertAssert(); 
            }
 
            // now create EventLog objects that point to those logs
            EventLog[] logs = new EventLog[logNames.Length];
            for (int i = 0; i < logNames.Length; i++) {
                EventLog log = new EventLog(); 
                log.Log = logNames[i];
                log.MachineName = machineName; 
                logs[i] = log; 
            }
 
            return logs;
        }

        /// <devdoc> 
        ///     Searches the cache for an entry with the given index
        /// </devdoc> 
        private int GetCachedEntryPos(int entryIndex) { 
            if (cache == null || (boolFlags[Flag_forwards] && entryIndex < firstCachedEntry) ||
                (!boolFlags[Flag_forwards] && entryIndex > firstCachedEntry) || firstCachedEntry == -1) { 
                // the index falls before anything we have in the cache, or the cache
                // is not yet valid
                return -1;
            } 
            // we only know where the beginning of the cache is, not the end, so even
            // if it's past the end of the cache, we'll have to search through the whole 
            // cache to find out. 

            // we're betting heavily that the one they want to see now is close 
            // to the one they asked for last time. We start looking where we
            // stopped last time.

            // We have two loops, one to go forwards and one to go backwards. Only one 
            // of them will ever be executed.
            while (lastSeenEntry < entryIndex) { 
                lastSeenEntry++; 
                if (boolFlags[Flag_forwards]) {
                    lastSeenPos = GetNextEntryPos(lastSeenPos); 
                    if (lastSeenPos >= bytesCached)
                        break;
                }
                else { 
                    lastSeenPos = GetPreviousEntryPos(lastSeenPos);
                    if (lastSeenPos < 0) 
                        break; 
                }
            } 
            while (lastSeenEntry > entryIndex) {
                lastSeenEntry--;
                if (boolFlags[Flag_forwards]) {
                    lastSeenPos = GetPreviousEntryPos(lastSeenPos); 
                    if (lastSeenPos < 0)
                        break; 
                } 
                else {
                    lastSeenPos = GetNextEntryPos(lastSeenPos); 
                    if (lastSeenPos >= bytesCached)
                        break;
                }
            } 
            if (lastSeenPos >= bytesCached) {
                // we ran past the end. move back to the last one and return -1 
                lastSeenPos = GetPreviousEntryPos(lastSeenPos); 
                if (boolFlags[Flag_forwards])
                    lastSeenEntry--; 
                else
                    lastSeenEntry++;
                return -1;
            } 
            else if (lastSeenPos < 0) {
                // we ran past the beginning. move back to the first one and return -1 
                lastSeenPos = 0; 
                if (boolFlags[Flag_forwards])
                    lastSeenEntry++; 
                else
                    lastSeenEntry--;
                return -1;
            } 
            else {
                // we found it. 
                return lastSeenPos; 
            }
        } 

        /// <devdoc>
        ///     Gets the entry at the given index
        /// </devdoc> 
        internal EventLogEntry GetEntryAt(int index) {
            EventLogEntry entry = GetEntryAtNoThrow(index); 
            if (entry == null) 
                throw new ArgumentException(SR.GetString(SR.IndexOutOfBounds, index.ToString(CultureInfo.CurrentCulture)));
            return entry; 
        }

        internal EventLogEntry GetEntryAtNoThrow(int index) {
            if (!IsOpenForRead) 
                OpenForRead(this.machineName);
 
            if (index < 0 || index >= EntryCount) 
                return null;
 
            index += OldestEntryNumber;

            return GetEntryWithOldest(index);
        } 

        private EventLogEntry GetEntryWithOldest(int index) { 
            EventLogEntry entry = null; 
            int entryPos = GetCachedEntryPos(index);
            if (entryPos >= 0) { 
                entry = new EventLogEntry(cache, entryPos, this);
                return entry;
            }
 
            string currentMachineName = this.machineName;
 
            // if we haven't seen the one after this, we were probably going 
            // forwards.
            int flags = 0; 
            if (GetCachedEntryPos(index+1) < 0) {
                flags = NativeMethods.FORWARDS_READ | NativeMethods.SEEK_READ;
                boolFlags[Flag_forwards] = true;
            } 
            else {
                flags = NativeMethods.BACKWARDS_READ | NativeMethods.SEEK_READ; 
                boolFlags[Flag_forwards] = false; 
            }
 
            cache = new byte[BUF_SIZE];
            int[] bytesRead = new int[1];
            int[] minBytesNeeded = new int[] { cache.Length};
            bool success = UnsafeNativeMethods.ReadEventLog(readHandle, flags, index, 
                                                  cache, cache.Length, bytesRead, minBytesNeeded);
            if (!success) { 
                int error = Marshal.GetLastWin32Error(); 
                // NOTE, [....]: ERROR_PROC_NOT_FOUND used to get returned, but I think that
                // was because I was calling GetLastError directly instead of GetLastWin32Error. 
                // Making the buffer bigger and trying again seemed to work. I've removed the check
                // for ERROR_PROC_NOT_FOUND because I don't think it's necessary any more, but
                // I can't prove it...
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "Error from ReadEventLog is " + error.ToString(CultureInfo.InvariantCulture)); 
                if (error == NativeMethods.ERROR_INSUFFICIENT_BUFFER || error == NativeMethods.ERROR_EVENTLOG_FILE_CHANGED) {
                    if (error == NativeMethods.ERROR_EVENTLOG_FILE_CHANGED) { 
                        // Reset() sets the cache null.  But since we're going to call ReadEventLog right after this, 
                        // we need the cache to be something valid.  We'll reuse the old byte array rather
                        // than creating a new one. 
                        byte[] tempcache = cache;
                        Reset(currentMachineName);
                        cache = tempcache;
                    } else { 
                        // try again with a bigger buffer.
                        if (minBytesNeeded[0] > cache.Length) { 
                            cache = new byte[minBytesNeeded[0]]; 
                        }
                    } 
                    success = UnsafeNativeMethods.ReadEventLog(readHandle, NativeMethods.FORWARDS_READ | NativeMethods.SEEK_READ, index,
                                                     cache, cache.Length, bytesRead, minBytesNeeded);
                }
 
                if (!success) {
                    throw new InvalidOperationException(SR.GetString(SR.CantReadLogEntryAt, index.ToString(CultureInfo.CurrentCulture)), SharedUtils.CreateSafeWin32Exception()); 
                } 
            }
            bytesCached = bytesRead[0]; 
            firstCachedEntry = index;
            lastSeenEntry = index;
            lastSeenPos = 0;
            return new EventLogEntry(cache, 0, this); 
        }
 
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        internal static RegistryKey GetEventLogRegKey(string machine, bool writable) { 
            RegistryKey lmkey = null;
            try {
                if (machine.Equals(".")) {
                    lmkey = Registry.LocalMachine; 
                }
                else { 
                    lmkey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machine); 
                }
                if (lmkey != null) 
                    return lmkey.OpenSubKey(EventLogKey, writable);
            }
            finally {
                if (lmkey != null) lmkey.Close(); 
            }
 
            return null; 
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private RegistryKey GetLogRegKey(string currentMachineName, bool writable) {
            string logname = GetLogName(currentMachineName); 
            // we need to verify the logname here again because we might have tried to look it up
            // based on the source and failed. 
            if (!ValidLogName(logname, false)) 
                throw new InvalidOperationException(SR.GetString(SR.BadLogName));
 
            RegistryKey eventkey = null;
            RegistryKey logkey = null;

            try { 
                eventkey = GetEventLogRegKey(currentMachineName, false);
                if (eventkey == null) 
                    throw new InvalidOperationException(SR.GetString(SR.RegKeyMissingShort, EventLogKey, currentMachineName)); 

                logkey = eventkey.OpenSubKey(logname, writable); 
                if (logkey == null)
                    throw new InvalidOperationException(SR.GetString(SR.MissingLog, logname, currentMachineName));
            }
            finally { 
                if (eventkey != null) eventkey.Close();
            } 
 
            return logkey;
        } 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private object GetLogRegValue(string currentMachineName, string valuename) { 
            PermissionSet permissionSet = _GetAssertPermSet();
            permissionSet.Assert(); 
 
            RegistryKey logkey = null;
 
            try {
                logkey = GetLogRegKey(currentMachineName, false);
                if (logkey == null)
                    throw new InvalidOperationException(SR.GetString(SR.MissingLog, GetLogName(currentMachineName), currentMachineName)); 

                object val = logkey.GetValue(valuename); 
                return val; 
            }
            finally { 
                if (logkey != null) logkey.Close();

                // Revert registry and environment permission asserts
                CodeAccessPermission.RevertAssert(); 
            }
        } 
 
        /// <devdoc>
        ///     Finds the index into the cache where the next entry starts 
        /// </devdoc>
        private int GetNextEntryPos(int pos) {
            return pos + IntFrom(cache, pos);
        } 

        /// <devdoc> 
        ///     Finds the index into the cache where the previous entry starts 
        /// </devdoc>
        private int GetPreviousEntryPos(int pos) { 
            // the entries in our buffer come back like this:
            // <length 1> ... <data> ...  <length 1> <length 2> ... <data> ... <length 2> ...
            // In other words, the length for each entry is repeated at the beginning and
            // at the end. This makes it easy to navigate forwards and backwards through 
            // the buffer.
            return pos - IntFrom(cache, pos - 4); 
        } 

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        internal static string GetDllPath(string machineName) {
            return SharedUtils.GetLatestBuildDllDirectory(machineName) + "\\" + DllName;
        } 

        /// <devdoc> 
        ///     Extracts a 32-bit integer from the ubyte buffer, beginning at the byte offset 
        ///     specified in offset.
        /// </devdoc> 
        private static int IntFrom(byte[] buf, int offset) {
            // assumes Little Endian byte order.
            return(unchecked((int)0xFF000000) & (buf[offset+3] << 24)) | (0xFF0000 & (buf[offset+2] << 16)) |
            (0xFF00 & (buf[offset+1] << 8)) | (0xFF & (buf[offset])); 
        }
 
        /// <devdoc> 
        ///    <para>
        ///       Determines whether an event source is registered on the local computer. 
        ///    </para>
        /// </devdoc>
        public static bool SourceExists(string source) {
            return SourceExists(source, "."); 
        }
 
        /// <devdoc> 
        ///    <para>
        ///       Determines whether an event 
        ///       source is registered on a specified computer.
        ///    </para>
        /// </devdoc>
        public static bool SourceExists(string source, string machineName) { 
            if (!SyntaxCheck.CheckMachineName(machineName)) {
                throw new ArgumentException(SR.GetString(SR.InvalidParameter, "machineName", machineName)); 
            } 

            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, machineName); 
            permission.Demand();

            using (RegistryKey keyFound = FindSourceRegistration(source, machineName, true)) {
                return (keyFound != null); 
            }
        } 
 
        /// <devdoc>
        ///     Gets the name of the log that the given source name is registered in. 
        /// </devdoc>
        public static string LogNameFromSourceName(string source, string machineName) {
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName);
            permission.Demand(); 

            using (RegistryKey key = FindSourceRegistration(source, machineName, true)) { 
                if (key == null) 
                    return "";
                else { 
                    string name = key.Name;
                    int whackPos = name.LastIndexOf('\\');
                    // this will work even if whackPos is -1
                    return name.Substring(whackPos+1); 
                }
            } 
        } 

        [ 
        ComVisible(false),
        ResourceExposure(ResourceScope.None),
        ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)
        ] 
        public void ModifyOverflowPolicy(OverflowAction action, int retentionDays) {
            string currentMachineName = this.machineName; 
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
            permission.Demand(); 

            if (action < OverflowAction.DoNotOverwrite || action > OverflowAction.OverwriteOlder)
                throw new InvalidEnumArgumentException("action", (int)action, typeof(OverflowAction));
 
            // this is a long because in the if statement we may need to store values as
            // large as UInt32.MaxValue - 1.  This would overflow an int. 
            long retentionvalue = (long) action; 
            if (action == OverflowAction.OverwriteOlder) {
                if (retentionDays < 1 || retentionDays > 365) 
                    throw new ArgumentOutOfRangeException(SR.GetString(SR.RentionDaysOutOfRange));

                retentionvalue = (long) retentionDays * SecondsPerDay;
            } 

            PermissionSet permissionSet = _GetAssertPermSet(); 
            permissionSet.Assert(); 

            using (RegistryKey logkey = GetLogRegKey(currentMachineName, true)) 
                logkey.SetValue("Retention", retentionvalue, RegistryValueKind.DWord);
        }

 
        /// <devdoc>
        ///     Opens the event log with read access 
        /// </devdoc> 
        private void OpenForRead(string currentMachineName) {
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::OpenForRead"); 

            //Cannot allocate the readHandle if the object has been disposed, since finalization has been suppressed.
            if (this.boolFlags[Flag_disposed])
                throw new ObjectDisposedException(GetType().Name); 

            string logname = GetLogName(currentMachineName); 
 
            if (logname == null || logname.Length==0)
                throw new ArgumentException(SR.GetString(SR.MissingLogProperty)); 

            if (! Exists(logname, currentMachineName) )        // do not open non-existing Log [[....]]
                throw new InvalidOperationException( SR.GetString(SR.LogDoesNotExists, logname, currentMachineName) );
            //Check environment before calling api 
            SharedUtils.CheckEnvironment();
 
            // Clean up cache variables. 
            // [[....]] The initilizing code is put here to guarantee, that first read of events
            //           from log file will start by filling up the cache buffer. 
            lastSeenEntry = 0;
            lastSeenPos = 0;
            bytesCached = 0;
            firstCachedEntry = -1; 

            readHandle = SafeEventLogReadHandle.OpenEventLog(currentMachineName, logname); 
            if (readHandle.IsInvalid) { 
                Win32Exception e = null;
                if (Marshal.GetLastWin32Error() != 0) { 
                    e = SharedUtils.CreateSafeWin32Exception();
                }

                throw new InvalidOperationException(SR.GetString(SR.CantOpenLog, logname.ToString(), currentMachineName), e); 
            }
        } 
 
        /// <devdoc>
        ///     Opens the event log with write access 
        /// </devdoc>
        private void OpenForWrite(string currentMachineName) {
            //Cannot allocate the writeHandle if the object has been disposed, since finalization has been suppressed.
            if (this.boolFlags[Flag_disposed]) 
                throw new ObjectDisposedException(GetType().Name);
 
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::OpenForWrite"); 
            if (sourceName == null || sourceName.Length==0)
                throw new ArgumentException(SR.GetString(SR.NeedSourceToOpen)); 

            //Check environment before calling api
            SharedUtils.CheckEnvironment();
 
            writeHandle = SafeEventLogWriteHandle.RegisterEventSource(currentMachineName, sourceName);
            if (writeHandle.IsInvalid) { 
                Win32Exception e = null; 
                if (Marshal.GetLastWin32Error() != 0) {
                    e = SharedUtils.CreateSafeWin32Exception(); 
                }
                throw new InvalidOperationException(SR.GetString(SR.CantOpenLogAccess, sourceName), e);
            }
        } 

        [ 
        ComVisible(false), 
        ResourceExposure(ResourceScope.Machine),
        ResourceConsumption(ResourceScope.Machine) 
        ]
        public void RegisterDisplayName(string resourceFile, long resourceId) {
            string currentMachineName = this.machineName;
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
            permission.Demand(); 
 
            PermissionSet permissionSet = _GetAssertPermSet();
            permissionSet.Assert(); 

            using (RegistryKey logkey = GetLogRegKey(currentMachineName, true)) {
                logkey.SetValue("DisplayNameFile", resourceFile, RegistryValueKind.ExpandString);
                logkey.SetValue("DisplayNameID", resourceId, RegistryValueKind.DWord); 
            }
        } 
 
        private void Reset(string currentMachineName) {
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::Reset"); 
            // save the state we're in now
            bool openRead = IsOpenForRead;
            bool openWrite = IsOpenForWrite;
            bool isMonitoring = boolFlags[Flag_monitoring]; 
            bool isListening = boolFlags[Flag_registeredAsListener];
 
            // close everything down 
            Close(currentMachineName);
            cache = null; 

            // and get us back into the same state as before
            if (openRead)
                OpenForRead(currentMachineName); 
            if (openWrite)
                OpenForWrite(currentMachineName); 
            if (isListening) 
                StartListening(currentMachineName, GetLogName(currentMachineName));
 
            boolFlags[Flag_monitoring] = isMonitoring;
        }

        [HostProtection(Synchronization=true)] 
        private static void RemoveListenerComponent(EventLog component, string compLogName) {
            lock (InternalSyncObject) { 
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::RemoveListenerComponent(" + compLogName + ")"); 

                LogListeningInfo info = (LogListeningInfo) listenerInfos[compLogName]; 
                Debug.Assert(info != null);

                // remove the requested component from the list.
                info.listeningComponents.Remove(component); 
                if (info.listeningComponents.Count != 0)
                    return; 
 
                // if that was the last interested compononent, destroy the handles and stop listening.
 
                info.handleOwner.Dispose();

                //Unregister the thread pool wait handle
                info.registeredWaitHandle.Unregister(info.waitHandle); 
                // close the handle
                info.waitHandle.Close(); 
 
                listenerInfos[compLogName] = null;
            } 
        }

        // The reasoning behind filling these values is historical. WS03 RTM had a race
        // between registry changes and EventLog service, which made the service wait 2 secs 
        // before retrying to see whether all regkey values are present. To avoid this
        // potential lag (worst case up to n*2 secs where n is the number of required regkeys) 
        // between creation and being able to write events, we started filling some of these 
        // values explicitly but for XP and latter OS releases like WS03 SP1 and Vista this
        // is not necessary and in some cases like the "File" key it's plain wrong to write. 
        private static void SetSpecialLogRegValues(RegistryKey logKey, string logName) {
            // Set all the default values for this log.  AutoBackupLogfiles only makes sense in
            // Win2000 SP4, WinXP SP1, and Win2003, but it should alright elsewhere.
 
            // Since we use this method on the existing system logs as well as our own,
            // we need to make sure we don't overwrite any existing values. 
            if (logKey.GetValue("MaxSize") == null) 
                logKey.SetValue("MaxSize", DefaultMaxSize, RegistryValueKind.DWord);
            if (logKey.GetValue("AutoBackupLogFiles") == null) 
                logKey.SetValue("AutoBackupLogFiles", 0, RegistryValueKind.DWord);

            if (!SkipRegPatch) {
                // In Vista, "retention of events for 'n' days" concept is removed 
                if (logKey.GetValue("Retention") == null)
                    logKey.SetValue("Retention", DefaultRetention, RegistryValueKind.DWord); 
 
                if (logKey.GetValue("File") == null) {
                    string filename; 
                    if (logName.Length > 8)
                        filename = @"%SystemRoot%\System32\config\" + logName.Substring(0,8) + ".evt";
                    else
                        filename = @"%SystemRoot%\System32\config\" + logName + ".evt"; 

                    logKey.SetValue("File", filename, RegistryValueKind.ExpandString); 
                } 
            }
        } 

        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        private static void SetSpecialSourceRegValues(RegistryKey sourceLogKey, EventSourceCreationData sourceData) { 
            if (String.IsNullOrEmpty(sourceData.MessageResourceFile))
                sourceLogKey.SetValue("EventMessageFile", GetDllPath(sourceData.MachineName), RegistryValueKind.ExpandString); 
            else 
                sourceLogKey.SetValue("EventMessageFile", FixupPath(sourceData.MessageResourceFile), RegistryValueKind.ExpandString);
 
            if (!String.IsNullOrEmpty(sourceData.ParameterResourceFile))
                sourceLogKey.SetValue("ParameterMessageFile", FixupPath(sourceData.ParameterResourceFile), RegistryValueKind.ExpandString);

            if (!String.IsNullOrEmpty(sourceData.CategoryResourceFile)) { 
                sourceLogKey.SetValue("CategoryMessageFile", FixupPath(sourceData.CategoryResourceFile), RegistryValueKind.ExpandString);
                sourceLogKey.SetValue("CategoryCount", sourceData.CategoryCount, RegistryValueKind.DWord); 
            } 
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static string FixupPath(string path) {
            if (path[0] == '%') 
                return path;
            else 
                return Path.GetFullPath(path); 
        }
 
        /// <devdoc>
        ///     Sets up the event monitoring mechanism.  We don't track event log changes
        ///     unless someone is interested, so we set this up on demand.
        /// </devdoc> 
        [HostProtection(Synchronization=true, ExternalThreading=true)]
        private void StartListening(string currentMachineName, string currentLogName) { 
            // make sure we don't fire events for entries that are already there 
            Debug.Assert(!boolFlags[Flag_registeredAsListener], "StartListening called with boolFlags[Flag_registeredAsListener] true.");
            lastSeenCount = EntryCount + OldestEntryNumber; 
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::StartListening: lastSeenCount = " + lastSeenCount);
            AddListenerComponent(this, currentMachineName, currentLogName);
            boolFlags[Flag_registeredAsListener] = true;
        } 

        private void StartRaisingEvents(string currentMachineName, string currentLogName) { 
            if (!boolFlags[Flag_initializing] && !boolFlags[Flag_monitoring] && !DesignMode) { 
                StartListening(currentMachineName, currentLogName);
            } 
            boolFlags[Flag_monitoring] = true;
        }

        private static void StaticCompletionCallback(object context, bool wasSignaled) { 
            LogListeningInfo info = (LogListeningInfo) context;
 
            // get a snapshot of the components to fire the event on 
            EventLog[] interestedComponents = (EventLog[]) info.listeningComponents.ToArray(typeof(EventLog));
 
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::StaticCompletionCallback: notifying " + interestedComponents.Length + " components.");

            for (int i = 0; i < interestedComponents.Length; i++)
                interestedComponents[i].CompletionCallback(null); 
        }
 
        /// <devdoc> 
        ///     Tears down the event listening mechanism.  This is called when the last
        ///     interested party removes their event handler. 
        /// </devdoc>
        [HostProtection(Synchronization=true, ExternalThreading=true)]
        private void StopListening(/*string currentMachineName,*/ string currentLogName) {
            Debug.Assert(boolFlags[Flag_registeredAsListener], "StopListening called without StartListening."); 
            RemoveListenerComponent(this, currentLogName);
            boolFlags[Flag_registeredAsListener] = false; 
        } 

        /// <devdoc> 
        /// </devdoc>
        private void StopRaisingEvents(/*string currentMachineName,*/ string currentLogName) {
            if (!boolFlags[Flag_initializing] && boolFlags[Flag_monitoring] && !DesignMode) {
                StopListening(currentLogName); 
            }
            boolFlags[Flag_monitoring] = false; 
        } 

        // Format message in specific DLL. Return <null> on failure. 
        internal static string TryFormatMessage(SafeLibraryHandle hModule, uint messageNum, string[] insertionStrings) {
            string msg = null;

            int msgLen = 0; 
            StringBuilder buf = new StringBuilder(1024);
            int flags = NativeMethods.FORMAT_MESSAGE_FROM_HMODULE | NativeMethods.FORMAT_MESSAGE_ARGUMENT_ARRAY; 
 
            IntPtr[] addresses = new IntPtr[insertionStrings.Length];
            GCHandle[] handles = new GCHandle[insertionStrings.Length]; 
            GCHandle stringsRoot = GCHandle.Alloc(addresses, GCHandleType.Pinned);

            // Make sure that we don't try to pass in a zero length array of addresses.  If there are no insertion strings,
            // we'll use the FORMAT_MESSAGE_IGNORE_INSERTS flag . 
            if (insertionStrings.Length == 0) {
                flags |= NativeMethods.FORMAT_MESSAGE_IGNORE_INSERTS; 
            } 

            try { 
                for (int i=0; i<handles.Length; i++) {
                    handles[i] = GCHandle.Alloc(insertionStrings[i], GCHandleType.Pinned);
                    addresses[i] = handles[i].AddrOfPinnedObject();
                } 
                int lastError = NativeMethods.ERROR_INSUFFICIENT_BUFFER;
                while (msgLen == 0 && lastError == NativeMethods.ERROR_INSUFFICIENT_BUFFER) { 
                    msgLen = SafeNativeMethods.FormatMessage( 
                        flags,
                        hModule, 
                        messageNum,
                        0,
                        buf,
                        buf.Capacity, 
                        addresses);
 
                    if (msgLen == 0) { 
                        lastError = Marshal.GetLastWin32Error();
                        if (lastError == NativeMethods.ERROR_INSUFFICIENT_BUFFER) 
                            buf.Capacity = buf.Capacity * 2;
                    }
                }
            } 
            catch {
                msgLen = 0;              // return empty on failure 
            } 
            finally  {
                for (int i=0; i<handles.Length; i++) { 
                    if (handles[i].IsAllocated) handles[i].Free();
                }
                stringsRoot.Free();
            } 

            if (msgLen > 0) { 
                msg = buf.ToString(); 
                // chop off a single CR/LF pair from the end if there is one. FormatMessage always appends one extra.
                if (msg.Length > 1 && msg[msg.Length-1] == '\n') 
                    msg = msg.Substring(0, msg.Length-2);
            }

            return msg; 
        }
 
        // CharIsPrintable used to be Char.IsPrintable, but Jay removed it and 
        // is forcing people to use the Unicode categories themselves.  Copied
        // the code here. 
        private static bool CharIsPrintable(char c) {
            UnicodeCategory uc = Char.GetUnicodeCategory(c);
            return (!(uc == UnicodeCategory.Control) || (uc == UnicodeCategory.Format) ||
                    (uc == UnicodeCategory.LineSeparator) || (uc == UnicodeCategory.ParagraphSeparator) || 
            (uc == UnicodeCategory.OtherNotAssigned));
        } 
 
        // SECREVIEW: Make sure this method catches all the strange cases.
        internal static bool ValidLogName(string logName, bool ignoreEmpty) { 
            // No need to trim here since the next check will verify that there are no spaces.
            // We need to ignore the empty string as an invalid log name sometimes because it can
            // be passed in from our default constructor.
            if (logName.Length == 0 && !ignoreEmpty) 
                return false;
 
            //any space, backslash, asterisk, or question mark is bad 
            //any non-printable characters are also bad
            foreach (char c in logName) 
                if (!CharIsPrintable(c) || (c == '\\') || (c == '*') || (c == '?'))
                    return false;

            return true; 
        }
 
        private void VerifyAndCreateSource(string sourceName, string currentMachineName) { 
            if (boolFlags[Flag_sourceVerified])
                return; 

            if (!SourceExists(sourceName, currentMachineName)) {
                Mutex mutex = null;
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    SharedUtils.EnterMutex(eventLogMutexName, ref mutex); 
                    if (!SourceExists(sourceName, currentMachineName)) { 
                        if (GetLogName(currentMachineName) == null)
                            SetLogName(currentMachineName, "Application"); 
                        // we automatically add an entry in the registry if there's not already
                        // one there for this source
                        CreateEventSource(new EventSourceCreationData(sourceName, GetLogName(currentMachineName), currentMachineName));
                        // The user may have set a custom log and tried to read it before trying to 
                        // write. Due to a quirk in the event log API, we would have opened the Application
                        // log to read (because the custom log wasn't there). Now that we've created 
                        // the custom log, we should close so that when we re-open, we get a read 
                        // handle on the _new_ log instead of the Application log.
                        Reset(currentMachineName); 
                    }
                    else {
                        string rightLogName = LogNameFromSourceName(sourceName, currentMachineName);
                        string currentLogName = GetLogName(currentMachineName); 
                        if (rightLogName != null && currentLogName != null && String.Compare(rightLogName, currentLogName, StringComparison.OrdinalIgnoreCase) != 0)
                            throw new ArgumentException(SR.GetString(SR.LogSourceMismatch, Source.ToString(), currentLogName, rightLogName)); 
                    } 

                } 
                finally {
                    if (mutex != null) {
                        mutex.ReleaseMutex();
                        mutex.Close(); 
                    }
                } 
            } 
            else {
                string rightLogName = LogNameFromSourceName(sourceName, currentMachineName); 
                string currentLogName = GetLogName(currentMachineName);
                if (rightLogName != null && currentLogName != null && String.Compare(rightLogName, currentLogName, StringComparison.OrdinalIgnoreCase) != 0)
                    throw new ArgumentException(SR.GetString(SR.LogSourceMismatch, Source.ToString(), currentLogName, rightLogName));
            } 

            boolFlags[Flag_sourceVerified] = true; 
        } 

        /// <devdoc> 
        ///    <para>
        ///       Writes an information type entry with the given message text to the event log.
        ///    </para>
        /// </devdoc> 
        public void WriteEntry(string message) {
            WriteEntry(message, EventLogEntryType.Information, (short) 0, 0, null); 
        } 

        /// <devdoc> 
        /// </devdoc>
        public static void WriteEntry(string source, string message) {
            WriteEntry(source, message, EventLogEntryType.Information, (short) 0, 0, null);
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Writes an entry of the specified <see cref='System.Diagnostics.EventLogEntryType'/> to the event log. Valid types are
        ///    <see langword='Error'/>, <see langword='Warning'/>, <see langword='Information'/>, 
        ///    <see langword='Success Audit'/>, and <see langword='Failure Audit'/>.
        ///    </para>
        /// </devdoc>
        public void WriteEntry(string message, EventLogEntryType type) { 
            WriteEntry(message, type, (short) 0, 0, null);
        } 
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static void WriteEntry(string source, string message, EventLogEntryType type) {
            WriteEntry(source, message, type, (short) 0, 0, null);
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Writes an entry of the specified <see cref='System.Diagnostics.EventLogEntryType'/>
        ///       and with the 
        ///       user-defined <paramref name="eventID"/>
        ///       to
        ///       the event log.
        ///    </para> 
        /// </devdoc>
        public void WriteEntry(string message, EventLogEntryType type, int eventID) { 
            WriteEntry(message, type, eventID, 0, null); 
        }
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID) { 
            WriteEntry(source, message, type, eventID, 0, null);
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Writes an entry of the specified type with the
        ///       user-defined <paramref name="eventID"/> and <paramref name="category"/>
        ///       to the event log. The <paramref name="category"/>
        ///       can be used by the event viewer to filter events in the log. 
        ///    </para>
        /// </devdoc> 
        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category) { 
            WriteEntry(message, type, eventID, category, null);
        } 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID, short category) {
            WriteEntry(source, message, type, eventID, category, null); 
        } 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID, short category,
                               byte[] rawData) { 
            EventLog log = new EventLog();
            try { 
                log.Source = source; 
                log.WriteEntry(message, type, eventID, category, rawData);
            } 
            finally {
                log.Dispose(true);
            }
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Writes an entry of the specified type with the
        ///       user-defined <paramref name="eventID"/> and <paramref name="category"/> to the event log, and appends binary data to 
        ///       the message. The Event Viewer does not interpret this data; it
        ///       displays raw data only in a combined hexadecimal and text format.
        ///    </para>
        /// </devdoc> 
        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category,
                               byte[] rawData) { 
 
            if (eventID < 0 || eventID > ushort.MaxValue)
                throw new ArgumentException(SR.GetString(SR.EventID, eventID, 0, (int)ushort.MaxValue)); 

            if (Source.Length == 0)
                throw new ArgumentException(SR.GetString(SR.NeedSourceToWrite));
 
            if (!Enum.IsDefined(typeof(EventLogEntryType), type))
                throw new InvalidEnumArgumentException("type", (int)type, typeof(EventLogEntryType)); 
 
            string currentMachineName = machineName;
            if (!boolFlags[Flag_writeGranted]) { 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand();
                boolFlags[Flag_writeGranted] = true;
            } 

            VerifyAndCreateSource(sourceName, currentMachineName); 
 
            // now that the source has been hooked up to our DLL, we can use "normal"
            // (message-file driven) logging techniques. 
            // Our DLL has 64K different entries; all of them just display the first
            // insertion string.
            InternalWriteEvent((uint)eventID, (ushort)category, type, new string[] { message}, rawData, currentMachineName);
        } 

        [ 
        ComVisible(false) 
        ]
        public void WriteEvent(EventInstance instance, params Object[] values) { 
            WriteEvent(instance, null, values);
        }

        [ 
        ComVisible(false)
        ] 
        public void WriteEvent(EventInstance instance, byte[] data, params Object[] values) { 
            if (instance == null)
                throw new ArgumentNullException("instance"); 
            if (Source.Length == 0)
                throw new ArgumentException(SR.GetString(SR.NeedSourceToWrite));

            string currentMachineName = machineName; 
            if (!boolFlags[Flag_writeGranted]) {
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName); 
                permission.Demand(); 
                boolFlags[Flag_writeGranted] = true;
            } 

            VerifyAndCreateSource(Source, currentMachineName);

            string[] strings = null; 

            if (values != null) { 
                strings = new string[values.Length]; 
                for (int i=0; i<values.Length; i++) {
                    if (values[i] != null) 
                        strings[i] = values[i].ToString();
                    else
                        strings[i] = String.Empty;
                } 
            }
 
            InternalWriteEvent((uint) instance.InstanceId, (ushort) instance.CategoryId, instance.EntryType, strings, data, currentMachineName); 
        }
 
        public static void WriteEvent(string source, EventInstance instance, params Object[] values) {
            using(EventLog log = new EventLog()) {
                log.Source = source;
                log.WriteEvent(instance, null, values); 
            }
        } 
 
        public static void WriteEvent(string source, EventInstance instance, byte[] data, params Object[] values) {
            using(EventLog log = new EventLog()) { 
                log.Source = source;
                log.WriteEvent(instance, data, values);
            }
        } 

        private void InternalWriteEvent(uint eventID, ushort category, EventLogEntryType type, string[] strings, 
                                byte[] rawData, string currentMachineName) { 

            // check arguments 
            if (strings == null)
                strings = new string[0];
            if (strings.Length >= 256)
                throw new ArgumentException(SR.GetString(SR.TooManyReplacementStrings)); 

            for (int i = 0; i < strings.Length; i++) { 
                if (strings[i] == null) 
                    strings[i] = String.Empty;
 
                // make sure the strings aren't too long.  MSDN says each string has a limit of 32k (32768) characters, but
                // experimentation shows that it doesn't like anything larger than 32766
                if (strings[i].Length > 32766)
                    throw new ArgumentException(SR.GetString(SR.LogEntryTooLong)); 
            }
            if (rawData == null) 
                rawData = new byte[0]; 

            if (Source.Length == 0) 
                throw new ArgumentException(SR.GetString(SR.NeedSourceToWrite));

            if (!IsOpenForWrite)
                OpenForWrite(currentMachineName); 

            // pin each of the strings in memory 
            IntPtr[] stringRoots = new IntPtr[strings.Length]; 
            GCHandle[] stringHandles = new GCHandle[strings.Length];
            GCHandle stringsRootHandle = GCHandle.Alloc(stringRoots, GCHandleType.Pinned); 
            try {
                for (int strIndex = 0; strIndex < strings.Length; strIndex++) {
                    stringHandles[strIndex] = GCHandle.Alloc(strings[strIndex], GCHandleType.Pinned);
                    stringRoots[strIndex] = stringHandles[strIndex].AddrOfPinnedObject(); 
                }
 
                byte[] sid = null; 
                // actually report the event
                bool success = UnsafeNativeMethods.ReportEvent(writeHandle, (short) type, category, eventID, 
                                                     sid, (short) strings.Length, rawData.Length, new HandleRef(this, stringsRootHandle.AddrOfPinnedObject()), rawData);
                if (!success) {
                    //Trace("WriteEvent", "Throwing Win32Exception");
                    throw SharedUtils.CreateSafeWin32Exception(); 
                }
            } 
            finally { 
                // now free the pinned strings
                for (int i = 0; i < strings.Length; i++) { 
                    if (stringHandles[i].IsAllocated)
                        stringHandles[i].Free();
                }
                stringsRootHandle.Free(); 
            }
        } 
 
        private class LogListeningInfo {
            public EventLog handleOwner; 
            public RegisteredWaitHandle registeredWaitHandle;
            public WaitHandle waitHandle;
            public ArrayList listeningComponents = new ArrayList();
        } 

        private class EventLogWaitHandle : WaitHandle { 
            public EventLogWaitHandle(SafeEventHandle eventLogNativeHandle) { 
                this.SafeWaitHandle = new SafeWaitHandle(eventLogNativeHandle.DangerousGetHandle(), true);
                eventLogNativeHandle.SetHandleAsInvalid(); 
            }
        }

    } 

} 
//------------------------------------------------------------------------------ 
// <copyright file="EventLog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

//#define RETRY_ON_ALL_ERRORS 
 
namespace System.Diagnostics {
    using System.Text; 
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics;
    using System; 
    using Microsoft.Win32; 
    using Microsoft.Win32.SafeHandles;
    using System.IO; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.ComponentModel.Design; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Reflection; 
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices; 

    /// <devdoc>
    ///    <para>
    ///       Provides interaction with Windows 2000 event logs. 
    ///    </para>
    /// </devdoc> 
    [ 
    DefaultEvent("EntryWritten"),
    InstallerType("System.Diagnostics.EventLogInstaller, " + AssemblyRef.SystemConfigurationInstall), 
    MonitoringDescription(SR.EventLogDesc)
    ]
    public class EventLog : Component, ISupportInitialize {
        // a collection over all our entries. Since the class holds no state, we 
        // can just hand the same instance out every time.
        private EventLogEntryCollection entriesCollection; 
        // the name of the log we're reading from or writing to 
        private string logName;
        // used in monitoring for event postings. 
        private int lastSeenCount;
        // holds the machine we're on, or null if it's the local machine
        private string machineName;
 
        // the delegate to call when an event arrives
        private EntryWrittenEventHandler onEntryWrittenHandler; 
        // holds onto the handle for reading 
        private SafeEventLogReadHandle  readHandle;
        // the source name - used only when writing 
        private string sourceName;
        // holds onto the handle for writing
        private SafeEventLogWriteHandle writeHandle;
 
        private string logDisplayName;
 
        // cache system state variables 
        // the initial size of the buffer (it can be made larger if necessary)
        private const int BUF_SIZE = 40000; 
        // the number of bytes in the cache that belong to entries (not necessarily
        // the same as BUF_SIZE, because the cache only holds whole entries)
        private int bytesCached;
        // the actual cache buffer 
        private byte[] cache;
        // the number of the entry at the beginning of the cache 
        private int firstCachedEntry = -1; 
        // the number of the entry that we got out of the cache most recently
        private int lastSeenEntry; 
        // where that entry was
        private int lastSeenPos;
        //support for threadpool based deferred execution
        private ISynchronizeInvoke synchronizingObject; 

        private const string EventLogKey = "SYSTEM\\CurrentControlSet\\Services\\EventLog"; 
        internal const string DllName = "EventLogMessages.dll"; 
        private const string eventLogMutexName = "netfxeventlog.1.0";
        private const int SecondsPerDay = 60 * 60 * 24; 
        private const int DefaultMaxSize = 512*1024;
        private const int DefaultRetention = 7*SecondsPerDay;

        private const int Flag_notifying     = 0x1;           // keeps track of whether we're notifying our listeners - to prevent double notifications 
        private const int Flag_forwards      = 0x2;     // whether the cache contains entries in forwards order (true) or backwards (false)
        private const int Flag_initializing  = 0x4; 
        private const int Flag_monitoring    = 0x8; 
        private const int Flag_registeredAsListener  = 0x10;
        private const int Flag_writeGranted     = 0x20; 
        private const int Flag_disposed      = 0x100;
        private const int Flag_sourceVerified= 0x200;

        private BitVector32 boolFlags = new BitVector32(); 

        private Hashtable messageLibraries; 
        private static Hashtable listenerInfos = new Hashtable(StringComparer.OrdinalIgnoreCase); 

        private static Object s_InternalSyncObject; 
        private static Object InternalSyncObject {
            get {
                if (s_InternalSyncObject == null) {
                    Object o = new Object(); 
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                } 
                return s_InternalSyncObject; 
            }
        } 

        // Whether we need backward compatible OS patch work or not
        private static bool s_CheckedOsVersion;
        private static bool s_SkipRegPatch; 

        private static bool SkipRegPatch { 
            get { 
                if (!s_CheckedOsVersion) {
                    OperatingSystem os = Environment.OSVersion; 
                    s_SkipRegPatch = (os.Platform == PlatformID.Win32NT) && (os.Version.Major > 5);
                    s_CheckedOsVersion = true;
                }
                return s_SkipRegPatch; 
            }
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.Diagnostics.EventLog'/>
        ///       class.
        ///    </para>
        /// </devdoc> 
        public EventLog() : this("", ".", "") {
        } 
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public EventLog(string logName) : this(logName, ".", "") {
        }
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public EventLog(string logName, string machineName) : this(logName, machineName, "") {
        } 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public EventLog(string logName, string machineName, string source) {
            //look out for invalid log names 
            if (logName == null) 
                throw new ArgumentNullException("logName");
            if (!ValidLogName(logName, true)) 
                throw new ArgumentException(SR.GetString(SR.BadLogName));

            if (!SyntaxCheck.CheckMachineName(machineName))
                throw new ArgumentException(SR.GetString(SR.InvalidParameter, "machineName", machineName)); 

            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, machineName); 
            permission.Demand(); 

            this.machineName = machineName; 

            this.logName = logName;
            this.sourceName = source;
            readHandle = null; 
            writeHandle = null;
            boolFlags[Flag_forwards] = true; 
        } 

        /// <devdoc> 
        ///    <para>
        ///       Gets the contents of the event log.
        ///    </para>
        /// </devdoc> 
        [
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        MonitoringDescription(SR.LogEntries)
        ] 
        public EventLogEntryCollection Entries {
            get {
                string currentMachineName = this.machineName;
 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
                permission.Demand(); 
 
                if (entriesCollection == null)
                    entriesCollection = new EventLogEntryCollection(this); 
                return entriesCollection;
            }
        }
 
        /// <devdoc>
        ///     Gets the number of entries in the log 
        /// </devdoc> 
        internal int EntryCount {
            get { 
                if (!IsOpenForRead)
                    OpenForRead(this.machineName);
                int count;
                bool success = UnsafeNativeMethods.GetNumberOfEventLogRecords(readHandle, out count); 
                if (!success)
                    throw SharedUtils.CreateSafeWin32Exception(); 
                return count; 
            }
        } 

        /// <devdoc>
        ///     Determines whether the event log is open in either read or write access
        /// </devdoc> 
        private bool IsOpen {
            get { 
                return readHandle != null || writeHandle != null; 
            }
        } 

        /// <devdoc>
        ///     Determines whether the event log is open with read access
        /// </devdoc> 
        private bool IsOpenForRead {
            get { 
                return readHandle != null; 
            }
        } 

        /// <devdoc>
        ///     Determines whether the event log is open with write access.
        /// </devdoc> 
        private bool IsOpenForWrite {
            get { 
                return writeHandle != null; 
            }
        } 

        private static PermissionSet _GetAssertPermSet() {

            PermissionSet permissionSet = new PermissionSet(PermissionState.None); 

            // We need RegistryPermission 
            RegistryPermission registryPermission = new RegistryPermission(PermissionState.Unrestricted); 
            permissionSet.AddPermission(registryPermission);
 
            // It is not enough to just assert RegistryPermission, for some regkeys
            // we need to assert EnvironmentPermission too
            EnvironmentPermission environmentPermission = new EnvironmentPermission(PermissionState.Unrestricted);
            permissionSet.AddPermission(environmentPermission); 

            return permissionSet; 
        } 

        /// <devdoc> 
        ///    <para>
        ///    </para>
        /// </devdoc>
        [Browsable(false)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public string LogDisplayName { 
            get {
                if (logDisplayName == null) { 

                    string currentMachineName = this.machineName;
                    if (GetLogName(currentMachineName) != null) {
 
                        EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
                        permission.Demand(); 
 
                        //Check environment before looking at the registry
                        SharedUtils.CheckEnvironment(); 

                        //SECREVIEW: Note that EventLogPermission is just demmanded above
                        PermissionSet permissionSet = _GetAssertPermSet();
                        permissionSet.Assert(); 

                        RegistryKey logkey = null; 
 
                        try {
                            // we figure out what logs are on the machine by looking in the registry. 
                            logkey = GetLogRegKey(currentMachineName, false);
                            if (logkey == null)
                                throw new InvalidOperationException(SR.GetString(SR.MissingLog, GetLogName(currentMachineName), currentMachineName));
 
                            string resourceDll = (string)logkey.GetValue("DisplayNameFile");
                            if (resourceDll == null) 
                                logDisplayName = GetLogName(currentMachineName); 
                            else {
                                int resourceId = (int)logkey.GetValue("DisplayNameID"); 
                                logDisplayName = FormatMessageWrapper(resourceDll, (uint) resourceId, null);
                                if (logDisplayName == null)
                                    logDisplayName = GetLogName(currentMachineName);
                            } 
                        }
                        finally { 
                            if (logkey != null) logkey.Close(); 

                            // Revert registry and environment permission asserts 
                            CodeAccessPermission.RevertAssert();
                        }
                    }
                } 

                return logDisplayName; 
            } 
        }
 
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the name of the log to read from and write to.
        ///    </para> 
        /// </devdoc>
        [ 
        TypeConverter("System.Diagnostics.Design.LogConverter, " + AssemblyRef.SystemDesign), 
        ReadOnly(true),
        MonitoringDescription(SR.LogLog), 
        DefaultValue(""),
        RecommendedAsConfigurable(true)
        ]
        public string Log { 
            get {
                return GetLogName(this.machineName); 
            } 
            set {
                SetLogName(this.machineName, value); 
            }
        }

        private string GetLogName(string currentMachineName) 
        {
            if ((logName == null || logName.Length == 0) && sourceName != null && sourceName.Length!=0) 
                // they've told us a source, but they haven't told us a log name. 
                // try to deduce the log name from the source name.
                logName = LogNameFromSourceName(sourceName, currentMachineName); 
            return logName;
        }

        private void SetLogName(string currentMachineName, string value) 
        {
            //look out for invalid log names 
            if (value == null) 
                throw new ArgumentNullException("value");
            if (!ValidLogName(value, true)) 
                throw new ArgumentException(SR.GetString(SR.BadLogName));

            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
            permission.Demand(); 

            if (value == null) 
                value = string.Empty; 
            if (logName == null)
                logName = value; 
            else {
                if (String.Compare(logName, value, StringComparison.OrdinalIgnoreCase) == 0)
                    return;
 
                logDisplayName = null;
                logName = value; 
                if (IsOpen) { 
                    bool setEnableRaisingEvents = this.EnableRaisingEvents;
                    Close(currentMachineName); 
                    this.EnableRaisingEvents = setEnableRaisingEvents;
                }
            }
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Gets or sets the name of the computer on which to read or write events.
        ///    </para> 
        /// </devdoc>
        [
        ReadOnly(true),
        MonitoringDescription(SR.LogMachineName), 
        DefaultValue("."),
        RecommendedAsConfigurable(true) 
        ] 
        public string MachineName {
            get { 
                string currentMachineName = this.machineName;

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand(); 

                return currentMachineName; 
            } 
            set {
                if (!SyntaxCheck.CheckMachineName(value)) { 
                    throw new ArgumentException(SR.GetString(SR.InvalidProperty, "MachineName", value));
                }

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, value); 
                permission.Demand();
 
                string currentMachineName = this.machineName; 

                if (currentMachineName != null) { 
                    if (String.Compare(currentMachineName, value, StringComparison.OrdinalIgnoreCase) == 0)
                        return;

                    boolFlags[Flag_writeGranted] = false; 

                    if (IsOpen) 
                        Close(currentMachineName); 
                }
                machineName = value; 
            }
        }

        [ 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        Browsable(false), 
        ComVisible(false) 
        ]
        public long MaximumKilobytes { 
            get {
                string currentMachineName = this.machineName;

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName); 
                permission.Demand();
 
                object val = GetLogRegValue(currentMachineName, "MaxSize"); 
                if (val != null) {
                    int intval = (int) val;         // cast to an int first to unbox 
                    return ((uint)intval) / 1024;   // then convert to kilobytes
                }

                // 512k is the default value 
                return 0x200;
            } 
 
            set {
                string currentMachineName = this.machineName; 

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
                permission.Demand();
 
                // valid range is 64 KB to 4 GB
                if (value < 64 || value > 0x3FFFC0 || value % 64 != 0) 
                    throw new ArgumentOutOfRangeException("MaximumKilobytes", SR.GetString(SR.MaximumKilobytesOutOfRange)); 

                PermissionSet permissionSet = _GetAssertPermSet(); 
                permissionSet.Assert();

                long regvalue = value * 1024; // convert to bytes
                int i = unchecked((int)regvalue); 

                using (RegistryKey logkey = GetLogRegKey(currentMachineName, true)) 
                    logkey.SetValue("MaxSize", i, RegistryValueKind.DWord); 
            }
        } 

        internal Hashtable MessageLibraries {
            get {
                if (messageLibraries == null) 
                    messageLibraries = new Hashtable(StringComparer.OrdinalIgnoreCase);
                return messageLibraries; 
            } 
        }
 
        [
        Browsable(false),
        ComVisible(false)
        ] 
        public OverflowAction OverflowAction {
            get { 
                string currentMachineName = this.machineName; 

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName); 
                permission.Demand();

                object retentionobj  = GetLogRegValue(currentMachineName, "Retention");
                if (retentionobj  != null) { 
                    int retention = (int) retentionobj;
                    if (retention == 0) 
                        return OverflowAction.OverwriteAsNeeded; 
                    else if (retention == -1)
                        return OverflowAction.DoNotOverwrite; 
                    else
                        return OverflowAction.OverwriteOlder;
                }
 
                // default value as listed in MSDN
                return OverflowAction.OverwriteOlder; 
            } 
        }
 
        [
        Browsable(false),
        ComVisible(false)
        ] 
        public int MinimumRetentionDays {
            get { 
                string currentMachineName = this.machineName; 

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName); 
                permission.Demand();

                object retentionobj  = GetLogRegValue(currentMachineName, "Retention");
                if (retentionobj  != null) { 
                    int retention = (int) retentionobj;
                    if (retention == 0 || retention == -1) 
                        return retention; 
                    else
                        return (int) (((double) retention) / SecondsPerDay); 
                }
                return 7;
            }
        } 

        /// <devdoc> 
        /// </devdoc> 
        [
        Browsable(false), 
        MonitoringDescription(SR.LogMonitoring),
        DefaultValue(false)
        ]
        public bool EnableRaisingEvents { 
            get {
                string currentMachineName = this.machineName; 
 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand(); 

                return boolFlags[Flag_monitoring];
            }
            set { 
                string currentMachineName = this.machineName;
 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName); 
                permission.Demand();
 
                if (this.DesignMode)
                    this.boolFlags[Flag_monitoring] = value;
                else {
                    if (value) 
                        StartRaisingEvents(currentMachineName, GetLogName(currentMachineName));
                    else 
                        StopRaisingEvents(/*currentMachineName,*/ GetLogName(currentMachineName)); 
                }
            } 
        }

        private int OldestEntryNumber {
            get { 
                if (!IsOpenForRead)
                    OpenForRead(this.machineName); 
                int[] num = new int[1]; 
                bool success = UnsafeNativeMethods.GetOldestEventLogRecord(readHandle, num);
                if (!success) 
                    throw SharedUtils.CreateSafeWin32Exception();
                int oldest = num[0];

                // When the event log is empty, GetOldestEventLogRecord returns 0. 
                // But then after an entry is written, it returns 1. We need to go from
                // the last oldest to the current. 
                if (oldest == 0) 
                    oldest = 1;
 
                return oldest;
            }
        }
 
        internal SafeEventLogReadHandle  ReadHandle {
            get { 
                if (!IsOpenForRead) 
                    OpenForRead(this.machineName);
                return readHandle; 
            }
        }

        /// <devdoc> 
        ///    <para>
        ///       Represents the object used to marshal the event handler 
        ///       calls issued as a result of an <see cref='System.Diagnostics.EventLog'/> 
        ///       change.
        ///    </para> 
        /// </devdoc>
        [
        Browsable(false),
        DefaultValue(null), 
        MonitoringDescription(SR.LogSynchronizingObject)
        ] 
        public ISynchronizeInvoke SynchronizingObject { 
        [HostProtection(Synchronization=true)]
            get { 
                string currentMachineName = this.machineName;

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand(); 
                if (this.synchronizingObject == null && DesignMode) {
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                    if (host != null) { 
                        object baseComponent = host.RootComponent;
                        if (baseComponent != null && baseComponent is ISynchronizeInvoke) 
                            this.synchronizingObject = (ISynchronizeInvoke)baseComponent;
                    }
                }
 
                return this.synchronizingObject;
            } 
 
            set {
                this.synchronizingObject = value; 
            }
        }

        /// <devdoc> 
        ///    <para>
        ///       Gets or 
        ///       sets the application name (source name) to register and use when writing to the event log. 
        ///    </para>
        /// </devdoc> 
        [
        ReadOnly(true),
        TypeConverter("System.Diagnostics.Design.StringValueConverter, " + AssemblyRef.SystemDesign),
        MonitoringDescription(SR.LogSource), 
        DefaultValue(""),
        RecommendedAsConfigurable(true) 
        ] 
        public string Source {
            get { 
                string currentMachineName = this.machineName;

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand(); 
                return sourceName;
            } 
            set { 
                if (value == null)
                    value = string.Empty; 

                // this 254 limit is the max length of a registry key.
                if (value.Length + EventLogKey.Length > 254)
                    throw new ArgumentException(SR.GetString(SR.ParameterTooLong, "source", 254 - EventLogKey.Length)); 

                string currentMachineName = this.machineName; 
 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand(); 
                if (sourceName == null)
                    sourceName = value;
                else {
                    if (String.Compare(sourceName, value, StringComparison.OrdinalIgnoreCase) == 0) 
                        return;
 
                    sourceName = value; 
                    if (IsOpen) {
                        bool setEnableRaisingEvents = this.EnableRaisingEvents; 
                        Close(currentMachineName);
                        this.EnableRaisingEvents = setEnableRaisingEvents;
                    }
                } 
                //Trace("Set_Source", "Setting source to " + (sourceName == null ? "null" : sourceName));
            } 
        } 

        [HostProtection(Synchronization=true)] 
        private static void AddListenerComponent(EventLog component, string compMachineName, string compLogName) {
            lock (InternalSyncObject) {
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::AddListenerComponent(" + compLogName + ")");
 
                LogListeningInfo info = (LogListeningInfo) listenerInfos[compLogName];
                if (info != null) { 
                    Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::AddListenerComponent: listener already active."); 
                    info.listeningComponents.Add(component);
                    return; 
                }

                info = new LogListeningInfo();
                info.listeningComponents.Add(component); 

                info.handleOwner = new EventLog(); 
                info.handleOwner.MachineName = compMachineName; 
                info.handleOwner.Log = compLogName;
 
                // create a system event
                SafeEventHandle notifyEventHandle = SafeEventHandle.CreateEvent(NativeMethods.NullHandleRef, false, false, null);
                if (notifyEventHandle.IsInvalid) {
                    Win32Exception e = null; 
                    if (Marshal.GetLastWin32Error() != 0) {
                        e = SharedUtils.CreateSafeWin32Exception(); 
                    } 
                    throw new InvalidOperationException(SR.GetString(SR.NotifyCreateFailed), e);
                } 

                // tell the event log system about it
                bool success = UnsafeNativeMethods.NotifyChangeEventLog(info.handleOwner.ReadHandle, notifyEventHandle);
                if (!success) 
                    throw new InvalidOperationException(SR.GetString(SR.CantMonitorEventLog), SharedUtils.CreateSafeWin32Exception());
 
                info.waitHandle = new EventLogWaitHandle(notifyEventHandle); 
                info.registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(info.waitHandle, new WaitOrTimerCallback(StaticCompletionCallback), info, -1, false);
 
                listenerInfos[compLogName] = info;
            }
        }
 
        /// <devdoc>
        ///    <para> 
        ///       Occurs when an entry is written to the event log. 
        ///    </para>
        /// </devdoc> 
        [MonitoringDescription(SR.LogEntryWritten)]
        public event EntryWrittenEventHandler EntryWritten {
            add {
                string currentMachineName = this.machineName; 

                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName); 
                permission.Demand(); 

                onEntryWrittenHandler += value; 
            }
            remove {
                string currentMachineName = this.machineName;
 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
                permission.Demand(); 
 
                onEntryWrittenHandler -= value;
            } 
        }

        /// <devdoc>
        /// </devdoc> 
        public void BeginInit() {
            string currentMachineName = this.machineName; 
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
            permission.Demand(); 

            if (boolFlags[Flag_initializing]) throw new InvalidOperationException(SR.GetString(SR.InitTwice));
            boolFlags[Flag_initializing] = true;
            if (boolFlags[Flag_monitoring]) 
                StopListening(GetLogName(currentMachineName));
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Clears
        ///       the event log by removing all entries from it.
        ///    </para>
        /// </devdoc> 
        public void Clear() {
            string currentMachineName = this.machineName; 
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
            permission.Demand(); 

            if (!IsOpenForRead)
                OpenForRead(currentMachineName);
            bool success = UnsafeNativeMethods.ClearEventLog(readHandle, NativeMethods.NullHandleRef); 
            if (!success) {
                // Ignore file not found errors.  ClearEventLog seems to try to delete the file where the event log is 
                // stored.  If it can't find it, it gives an error. 
                int error = Marshal.GetLastWin32Error();
                if (error != NativeMethods.ERROR_FILE_NOT_FOUND) 
                    throw SharedUtils.CreateSafeWin32Exception();
            }

            // now that we've cleared the event log, we need to re-open our handles, because 
            // the internal state of the event log has changed.
            Reset(currentMachineName); 
        } 

        /// <devdoc> 
        ///    <para>
        ///       Closes the event log and releases read and write handles.
        ///    </para>
        /// </devdoc> 
        public void Close() {
            Close(this.machineName); 
        } 

        private void Close(string currentMachineName) { 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
            permission.Demand();

            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::Close"); 
            //Trace("Close", "Closing the event log");
            if (readHandle != null) { 
                try { 
                    readHandle.Close();
                } 
                catch (IOException) {
                    throw SharedUtils.CreateSafeWin32Exception();
                }
                readHandle = null; 
                //Trace("Close", "Closed read handle");
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::Close: closed read handle"); 
            } 
            if (writeHandle != null) {
                try { 
                    writeHandle.Close();
                }
                catch (IOException) {
                    throw SharedUtils.CreateSafeWin32Exception(); 
                }
                writeHandle = null; 
                //Trace("Close", "Closed write handle"); 
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::Close: closed write handle");
            } 
            if (boolFlags[Flag_monitoring])
                StopRaisingEvents(/*currentMachineName,*/ GetLogName(currentMachineName));

            if (messageLibraries != null) { 
                foreach (SafeLibraryHandle handle in messageLibraries.Values)
                    handle.Close(); 
 
                messageLibraries = null;
            } 

            boolFlags[Flag_sourceVerified] = false;
        }
 

        /// <internalonly/> 
        /// <devdoc> 
        ///     Called when the threadpool is ready for us to handle a status change.
        /// </devdoc> 
        private void CompletionCallback(object context)  {
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: starting at " + lastSeenCount.ToString(CultureInfo.InvariantCulture));
            lock (this) {
                if (boolFlags[Flag_notifying]) { 
                    // don't do double notifications.
                    Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: aborting because we're already notifying."); 
                    return; 
                }
                boolFlags[Flag_notifying] = true; 
            }

            int i = lastSeenCount;
            try { 
                // NOTE, [....]: We have a double loop here so that we access the
                // EntryCount property as infrequently as possible. (It may be expensive 
                // to get the property.) Even though there are two loops, they will together 
                // only execute as many times as (final value of EntryCount) - lastSeenCount.
                int oldest = OldestEntryNumber; 
                int count = EntryCount + oldest;
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: OldestEntryNumber is " + OldestEntryNumber + ", EntryCount is " + EntryCount);
                while (i < count) {
                    while (i < count) { 
                        EventLogEntry entry = GetEntryWithOldest(i);
                        if (this.SynchronizingObject != null && this.SynchronizingObject.InvokeRequired) 
                            this.SynchronizingObject.BeginInvoke(this.onEntryWrittenHandler, new object[]{this, new EntryWrittenEventArgs(entry)}); 
                        else
                           onEntryWrittenHandler(this, new EntryWrittenEventArgs(entry)); 

                        i++;
                    }
                    oldest = OldestEntryNumber; 
                    count = EntryCount + oldest;
                } 
            } 
            catch (Exception e) {
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: Caught exception notifying event handlers: " + e.ToString()); 
            }
            catch {
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: Caught exception notifying event handlers.");
            } 

            // if the user cleared the log while we were receiving events, the call to GetEntryWithOldest above could have 
            // thrown an exception and i could be too large.  Make sure we don't set lastSeenCount to something bogus. 
            int newCount = EntryCount + OldestEntryNumber;
            if (i > newCount) 
                lastSeenCount = newCount;
            else
                lastSeenCount = i;
 
            lock (this) {
                boolFlags[Flag_notifying] = false; 
            } 
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::CompletionStatusChanged: finishing at " + lastSeenCount.ToString(CultureInfo.InvariantCulture));
        } 

        /// <devdoc>
        ///    <para> Establishes an application, using the
        ///       specified <see cref='System.Diagnostics.EventLog.Source'/> , as a valid event source for 
        ///       writing entries
        ///       to a log on the local computer. This method 
        ///       can also be used to create 
        ///       a new custom log on the local computer.</para>
        /// </devdoc> 
        public static void CreateEventSource(string source, string logName) {
            CreateEventSource(new EventSourceCreationData(source, logName, "."));
        }
 
        /// <devdoc>
        ///    <para>Establishes an application, using the specified 
        ///    <see cref='System.Diagnostics.EventLog.Source'/> as a valid event source for writing 
        ///       entries to a log on the computer
        ///       specified by <paramref name="machineName"/>. This method can also be used to create a new 
        ///       custom log on the given computer.</para>
        /// </devdoc>
        [Obsolete("This method has been deprecated.  Please use System.Diagnostics.EventLog.CreateEventSource(EventSourceCreationData sourceData) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public static void CreateEventSource(string source, string logName, string machineName) { 
            CreateEventSource(new EventSourceCreationData(source, logName, machineName));
        } 
 
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)] 
        public static void CreateEventSource(EventSourceCreationData sourceData) {
            if (sourceData == null)
                throw new ArgumentNullException("sourceData");
 
            string logName = sourceData.LogName;
            string source = sourceData.Source; 
            string machineName = sourceData.MachineName; 

            // verify parameters 
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "CreateEventSource: Checking arguments");
            if (!SyntaxCheck.CheckMachineName(machineName)) {
                throw new ArgumentException(SR.GetString(SR.InvalidParameter, "machineName", machineName));
            } 
            if (logName == null || logName.Length==0)
                logName = "Application"; 
            if (!ValidLogName(logName, false)) 
                throw new ArgumentException(SR.GetString(SR.BadLogName));
            if (source == null || source.Length==0) 
                throw new ArgumentException(SR.GetString(SR.MissingParameter, "source"));
            if (source.Length + EventLogKey.Length > 254)
                throw new ArgumentException(SR.GetString(SR.ParameterTooLong, "source", 254 - EventLogKey.Length));
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName);
            permission.Demand(); 
 
            Mutex mutex = null;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                SharedUtils.EnterMutex(eventLogMutexName, ref mutex);
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "CreateEventSource: Calling SourceExists");
                if (SourceExists(source, machineName)) { 
                    Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "CreateEventSource: SourceExists returned true");
                    // don't let them register a source if it already exists 
                    // this makes more sense than just doing it anyway, because the source might 
                    // be registered under a different log name, and we don't want to create
                    // duplicates. 
                    if (".".Equals(machineName))
                        throw new ArgumentException(SR.GetString(SR.LocalSourceAlreadyExists, source));
                    else
                        throw new ArgumentException(SR.GetString(SR.SourceAlreadyExists, source, machineName)); 
                }
 
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "CreateEventSource: Getting DllPath"); 

                //SECREVIEW: Note that EventLog permission is demanded above. 
                PermissionSet permissionSet = _GetAssertPermSet();
                permissionSet.Assert();

                RegistryKey baseKey = null; 
                RegistryKey eventKey = null;
                RegistryKey logKey = null; 
                RegistryKey sourceLogKey = null; 
                RegistryKey sourceKey = null;
                try { 
                    Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "CreateEventSource: Getting local machine regkey");
                    if (machineName == ".")
                        baseKey = Registry.LocalMachine;
                    else 
                        baseKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machineName);
 
                    eventKey = baseKey.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\EventLog", true); 
                    if (eventKey == null) {
                        if (!".".Equals(machineName)) 
                            throw new InvalidOperationException(SR.GetString(SR.RegKeyMissing, "SYSTEM\\CurrentControlSet\\Services\\EventLog", logName, source, machineName));
                        else
                            throw new InvalidOperationException(SR.GetString(SR.LocalRegKeyMissing, "SYSTEM\\CurrentControlSet\\Services\\EventLog", logName, source));
                    } 

                    // The event log system only treats the first 8 characters of the log name as 
                    // significant. If they're creating a new log, but that new log has the same 
                    // first 8 characters as another log, the system will think they're the same.
                    // Throw an exception to let them know. 
                    logKey = eventKey.OpenSubKey(logName, true);
                    if (logKey == null && logName.Length >= 8) {

                        // check for Windows embedded logs file names 
                        string logNameFirst8 = logName.Substring(0,8);
                        if ( string.Compare(logNameFirst8,"AppEvent",StringComparison.OrdinalIgnoreCase) ==0  || 
                             string.Compare(logNameFirst8,"SecEvent",StringComparison.OrdinalIgnoreCase) ==0  || 
                             string.Compare(logNameFirst8,"SysEvent",StringComparison.OrdinalIgnoreCase) ==0 )
                            throw new ArgumentException(SR.GetString(SR.InvalidCustomerLogName, logName)); 

                        string sameLogName = FindSame8FirstCharsLog(eventKey, logName);
                        if ( sameLogName != null )
                            throw new ArgumentException(SR.GetString(SR.DuplicateLogName, logName, sameLogName)); 
                    }
 
                    bool createLogKey = (logKey == null); 
                    if (createLogKey) {
                        if (SourceExists(logName, machineName)) { 
                            // don't let them register a log name that already
                            // exists as source name, a source with the same
                            // name as the log will have to be created by default
                            if (".".Equals(machineName)) 
                                throw new ArgumentException(SR.GetString(SR.LocalLogAlreadyExistsAsSource, logName));
                            else 
                                throw new ArgumentException(SR.GetString(SR.LogAlreadyExistsAsSource, logName, machineName)); 
                        }
 
                        logKey = eventKey.CreateSubKey(logName);

                        // NOTE: We shouldn't set "Sources" explicitly, the OS will automatically set it.
                        // The EventLog service doesn't use it for anything it is just an helping hand for event viewer filters. 
                        // Writing this value explicitly might confuse the service as it might perceive it as a change and
                        // start initializing again 
 
                        if (!SkipRegPatch)
                            logKey.SetValue("Sources", new string[] {logName, source}, RegistryValueKind.MultiString); 

                        SetSpecialLogRegValues(logKey, logName);

                        // A source with the same name as the log has to be created 
                        // by default. It is the behavior expected by EventLog API.
                        sourceLogKey = logKey.CreateSubKey(logName); 
                        SetSpecialSourceRegValues(sourceLogKey, sourceData); 
                    }
 
                    if (logName != source) {
                        if (!createLogKey) {
                            SetSpecialLogRegValues(logKey, logName);
 
                            if (!SkipRegPatch) {
                                string[] sources = logKey.GetValue("Sources") as string[]; 
                                if (sources == null) 
                                    logKey.SetValue("Sources", new string[] {logName, source}, RegistryValueKind.MultiString);
                                else { 
                                    // We have a race with OS EventLog here.
                                    // OS might update Sources as well. We should avoid writing the
                                    // source name if OS beats us.
                                    if( Array.IndexOf(sources, source) == -1) { 
                                        string[] newsources = new string[sources.Length + 1];
                                        Array.Copy(sources, newsources, sources.Length); 
                                        newsources[sources.Length] = source; 
                                        logKey.SetValue("Sources", newsources, RegistryValueKind.MultiString);
                                    } 
                                }
                            }
                        }
 
                        sourceKey = logKey.CreateSubKey(source);
                        SetSpecialSourceRegValues(sourceKey, sourceData); 
                    } 
                }
                finally { 
                    if (baseKey != null)
                        baseKey.Close();

                    if (eventKey != null) 
                        eventKey.Close();
 
                    if (logKey != null) { 
                        logKey.Flush();
                        logKey.Close(); 
                    }

                    if (sourceLogKey != null) {
                        sourceLogKey.Flush(); 
                        sourceLogKey.Close();
                    } 
 
                    if (sourceKey != null) {
                        sourceKey.Flush(); 
                        sourceKey.Close();
                    }

                    // Revert registry and environment permission asserts 
                    CodeAccessPermission.RevertAssert();
                } 
            } 
            finally {
                if (mutex != null) { 
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
            } 
        }
 
        /// <devdoc> 
        ///    <para>
        ///       Removes 
        ///       an event
        ///       log from the local computer.
        ///    </para>
        /// </devdoc> 
        public static void Delete(string logName) {
            Delete(logName, "."); 
        } 

        /// <devdoc> 
        ///    <para>
        ///       Removes
        ///       an
        ///       event 
        ///       log from the specified computer.
        ///    </para> 
        /// </devdoc> 
        public static void Delete(string logName, string machineName) {
 
            if (!SyntaxCheck.CheckMachineName(machineName))
                throw new ArgumentException(SR.GetString(SR.InvalidParameterFormat, "machineName"));
            if (logName == null || logName.Length==0)
                throw new ArgumentException(SR.GetString(SR.NoLogName)); 
            if (!ValidLogName(logName, false))
                throw new InvalidOperationException(SR.GetString(SR.BadLogName)); 
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName);
            permission.Demand(); 

            //Check environment before even trying to play with the registry
            SharedUtils.CheckEnvironment();
 
            //SECREVIEW: Note that EventLog permission is demanded above.
            PermissionSet permissionSet = _GetAssertPermSet(); 
            permissionSet.Assert(); 

            RegistryKey eventlogkey = null; 

            Mutex mutex = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                SharedUtils.EnterMutex(eventLogMutexName, ref mutex);
 
                try { 
                    eventlogkey  = GetEventLogRegKey(machineName, true);
                    if (eventlogkey  == null) { 
                        // there's not even an event log service on the machine.
                        // or, more likely, we don't have the access to read the registry.
                        throw new InvalidOperationException(SR.GetString(SR.RegKeyNoAccess, "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\EventLog", machineName));
                    } 

                    using (RegistryKey logKey = eventlogkey.OpenSubKey(logName)) { 
                        if (logKey == null) 
                            throw new InvalidOperationException(SR.GetString(SR.MissingLog, logName, machineName));
 
                        //clear out log before trying to delete it
                        //that way, if we can't delete the log file, no entries will persist because it has been cleared
                        EventLog logToClear = new EventLog();
                        try { 
                            logToClear.Log = logName;
                            logToClear.MachineName = machineName; 
                            logToClear.Clear(); 
                        }
                        finally { 
                            logToClear.Close();
                        }

                        // 

 
                        string filename = null; 
                        try {
                            //most of the time, the "File" key does not exist, but we'll still give it a whirl 
                            filename = (string) logKey.GetValue("File");
                        }
                        catch { }
                        if (filename != null) { 
                            try {
                                File.Delete(filename); 
                            } 
                            catch { }
                        } 
                    }

                    // now delete the registry entry
                    eventlogkey.DeleteSubKeyTree(logName); 
                }
                finally { 
                    if (eventlogkey != null) eventlogkey.Close(); 

                    // Revert registry and environment permission asserts 
                    CodeAccessPermission.RevertAssert();
                }
            }
            finally { 
                if (mutex != null) mutex.ReleaseMutex();
            } 
        } 

        /// <devdoc> 
        ///    <para>
        ///       Removes the event source
        ///       registration from the event log of the local computer.
        ///    </para> 
        /// </devdoc>
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)] 
        public static void DeleteEventSource(string source) {
            DeleteEventSource(source, "."); 
        }

        /// <devdoc>
        ///    <para> 
        ///       Removes
        ///       the application's event source registration from the specified computer. 
        ///    </para> 
        /// </devdoc>
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public static void DeleteEventSource(string source, string machineName) {
            if (!SyntaxCheck.CheckMachineName(machineName)) {
                throw new ArgumentException(SR.GetString(SR.InvalidParameter, "machineName", machineName)); 
            }
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName); 
            permission.Demand();
 
            //Check environment before looking at the registry
            SharedUtils.CheckEnvironment();

            //SECREVIEW: Note that EventLog permission is demanded above. 
            PermissionSet permissionSet = _GetAssertPermSet();
            permissionSet.Assert(); 
 
            Mutex mutex = null;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                SharedUtils.EnterMutex(eventLogMutexName, ref mutex);
                RegistryKey key = null;
 
                // First open the key read only so we can do some checks.  This is important so we get the same
                // exceptions even if we don't have write access to the reg key. 
                using (key = FindSourceRegistration(source, machineName, true)) { 
                    if (key == null) {
                        if (machineName == null) 
                            throw new ArgumentException(SR.GetString(SR.LocalSourceNotRegistered, source));
                        else
                            throw new ArgumentException(SR.GetString(SR.SourceNotRegistered, source, machineName, "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\EventLog"));
                    } 

                    // Check parent registry key (Event Log Name) and if it's equal to source, then throw an exception. 
                    // The reason: each log registry key must always contain subkey (i.e. source) with the same name. 
                    string keyname = key.Name;
                    int index = keyname.LastIndexOf('\\'); 
                    if ( string.Compare(keyname, index+1, source, 0, keyname.Length - index, StringComparison.Ordinal) == 0 )
                        throw new InvalidOperationException(SR.GetString(SR.CannotDeleteEqualSource, source));
                }
 
                try {
                    // now open it read/write to try to do the actual delete 
                    key = FindSourceRegistration(source, machineName, false); 
                    key.DeleteSubKeyTree(source);
 
                    if (!SkipRegPatch) {
                        string[] sources = (string[]) key.GetValue("Sources");
                        ArrayList newsources = new ArrayList(sources.Length - 1);
 
                        for (int i=0; i<sources.Length; i++) {
                            if (sources[i] != source) { 
                                newsources.Add(sources[i]); 
                            }
                        } 
                        string[] newsourcesArray = new string[newsources.Count];
                        newsources.CopyTo(newsourcesArray);

                        key.SetValue("Sources", newsourcesArray, RegistryValueKind.MultiString); 
                    }
                } 
                finally { 
                    if (key != null) {
                        key.Flush(); 
                        key.Close();
                    }

                    // Revert registry and environment permission asserts 
                    CodeAccessPermission.RevertAssert();
                } 
            } 
            finally {
                if (mutex != null) 
                    mutex.ReleaseMutex();
            }
        }
 
        /// <devdoc>
        /// </devdoc> 
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                //Dispose unmanaged and managed resources 
                if (IsOpen)
                    Close();
            }
            else { 
                //Dispose unmanaged resources
                if (readHandle != null) 
                    readHandle.Close(); 

                if (writeHandle != null) 
                    writeHandle.Close();

                messageLibraries = null;
            } 

            this.boolFlags[Flag_disposed] = true; 
            base.Dispose(disposing); 
        }
 
        /// <devdoc>
        /// </devdoc>
        public void EndInit() {
            string currentMachineName = this.machineName; 

            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName); 
            permission.Demand(); 

            boolFlags[Flag_initializing] = false; 
            if (boolFlags[Flag_monitoring])
                StartListening(currentMachineName, GetLogName(currentMachineName));
        }
 
        /// <devdoc>
        ///    <para> 
        ///       Determines whether the log 
        ///       exists on the local computer.
        ///    </para> 
        /// </devdoc>
        public static bool Exists(string logName) {
            return Exists(logName, ".");
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Determines whether the
        ///       log exists on the specified computer. 
        ///    </para>
        /// </devdoc>
        public static bool Exists(string logName, string machineName) {
            if (!SyntaxCheck.CheckMachineName(machineName)) 
                throw new ArgumentException(SR.GetString(SR.InvalidParameterFormat, "machineName"));
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName); 
            permission.Demand();
 
            if (logName == null || logName.Length==0)
                return false;

            //Check environment before looking at the registry 
            SharedUtils.CheckEnvironment();
 
            //SECREVIEW: Note that EventLog permission is demanded above. 
            PermissionSet permissionSet = _GetAssertPermSet();
            permissionSet.Assert(); 

            RegistryKey eventkey = null;
            RegistryKey logKey = null;
 
            try {
                eventkey = GetEventLogRegKey(machineName, false); 
                if (eventkey == null) 
                    return false;
 
                logKey = eventkey.OpenSubKey(logName, false);         // try to find log file key immediately.
                return (logKey != null );
            }
            finally { 
                if (eventkey != null) eventkey.Close();
                if (logKey != null) logKey.Close(); 
 
                // Revert registry and environment permission asserts
                CodeAccessPermission.RevertAssert(); 
            }
        }

 
        // Try to find log file name with the same 8 first characters.
        // Returns 'null' if no "same first 8 chars" log is found.   logName.Length must be > 7 
        private static string FindSame8FirstCharsLog(RegistryKey keyParent, string logName) { 

            string logNameFirst8 = logName.Substring(0, 8); 
            string[] logNames = keyParent.GetSubKeyNames();

            for (int i = 0; i < logNames.Length; i++) {
                string currentLogName = logNames[i]; 
                if ( currentLogName.Length >= 8  &&
                     string.Compare(currentLogName.Substring(0, 8), logNameFirst8, StringComparison.OrdinalIgnoreCase) == 0) 
                    return currentLogName; 
            }
 
            return null;   // not found
        }

        /// <devdoc> 
        ///     Gets a RegistryKey that points to the LogName entry in the registry that is
        ///     the parent of the given source on the given machine, or null if none is found. 
        /// </devdoc> 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        private static RegistryKey FindSourceRegistration(string source, string machineName, bool readOnly) {
            if (source != null && source.Length != 0) {

                //Check environment before looking at the registry 
                SharedUtils.CheckEnvironment();
 
                //SECREVIEW: Any call to this function must have demmanded 
                //                         EventLogPermission before.
                PermissionSet permissionSet = _GetAssertPermSet(); 
                permissionSet.Assert();

                RegistryKey eventkey = null;
                try { 
                    eventkey = GetEventLogRegKey(machineName, !readOnly);
                    if (eventkey == null) { 
                        // there's not even an event log service on the machine. 
                        // or, more likely, we don't have the access to read the registry.
                        return null; 
                    }

                    StringBuilder inaccessibleLogs = null;
 
                    // Most machines will return only { "Application", "System", "Security" },
                    // but you can create your own if you want. 
                    string[] logNames = eventkey.GetSubKeyNames(); 
                    for (int i = 0; i < logNames.Length; i++) {
                        // see if the source is registered in this log. 
                        // NOTE: A source name must be unique across ALL LOGS!
                        RegistryKey sourceKey = null;
                        try {
                            RegistryKey logKey = eventkey.OpenSubKey(logNames[i], /*writable*/!readOnly); 
                            if (logKey != null) {
                                sourceKey = logKey.OpenSubKey(source, /*writable*/!readOnly); 
                                if (sourceKey != null) { 
                                    // found it
                                    return logKey; 
                                }
                            }
                            // else logKey is null, so we don't need to Close it
                        } 
                        catch (UnauthorizedAccessException) {
                            if (inaccessibleLogs == null) { 
                                inaccessibleLogs = new StringBuilder(logNames[i]); 
                            }
                            else { 
                                inaccessibleLogs.Append(", ");
                                inaccessibleLogs.Append(logNames[i]);
                            }
                        } 
                        catch (SecurityException) {
                            if (inaccessibleLogs == null) { 
                                inaccessibleLogs = new StringBuilder(logNames[i]); 
                            }
                            else { 
                                inaccessibleLogs.Append(", ");
                                inaccessibleLogs.Append(logNames[i]);
                            }
                        } 
                        finally {
                            if (sourceKey != null) sourceKey.Close(); 
                        } 
                    }
 
                    if (inaccessibleLogs != null)
                        throw new SecurityException(SR.GetString(SR.SomeLogsInaccessible, inaccessibleLogs.ToString()));

                } 
                finally {
                    if (eventkey != null) eventkey.Close(); 
 
                    // Revert registry and environment permission asserts
                    CodeAccessPermission.RevertAssert(); 
                }
                // didn't see it anywhere
            }
 
            return null;
        } 
 
        internal string FormatMessageWrapper(string dllNameList, uint messageNum, string[] insertionStrings) {
            if (dllNameList == null) 
                return null;

            if (insertionStrings == null)
                insertionStrings = new string[0]; 

            string[] listDll = dllNameList.Split(';'); 
 
            // Find first mesage in DLL list
            foreach ( string dllName in  listDll) { 
                if (dllName == null || dllName.Length == 0)
                    continue;

                SafeLibraryHandle hModule = null; 

                // if the EventLog is open, then we want to cache the library in our hashtable.  Otherwise 
                // we'll just load it and free it after we're done. 
                if (IsOpen) {
                    hModule = MessageLibraries[dllName] as SafeLibraryHandle; 

                    if (hModule == null || hModule.IsInvalid) {
                        hModule = SafeLibraryHandle.LoadLibraryEx(dllName, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
                        MessageLibraries[dllName] = hModule; 
                    }
                } 
                else { 
                    hModule = SafeLibraryHandle.LoadLibraryEx(dllName, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
                } 

                if (hModule.IsInvalid)
                    continue;
 
                string msg = null;
                try { 
                    msg = TryFormatMessage(hModule, messageNum, insertionStrings); 
                }
                finally { 
                    if (!IsOpen) {
                        hModule.Close();
                    }
                } 

                if ( msg != null ) { 
                    return msg; 
                }
 
            }
            return null;
        }
 
        /// <devdoc>
        ///     Gets an array of EventLogEntry's, one for each entry in the log. 
        /// </devdoc> 
        internal EventLogEntry[] GetAllEntries() {
            // we could just call getEntryAt() on all the entries, but it'll be faster 
            // if we grab multiple entries at once.
            string currentMachineName = this.machineName;

            if (!IsOpenForRead) 
                OpenForRead(currentMachineName);
 
            EventLogEntry[] entries = new EventLogEntry[EntryCount]; 
            int idx = 0;
            int oldestEntry = OldestEntryNumber; 

            int[] bytesRead = new int[1];
            int[] minBytesNeeded = new int[] { BUF_SIZE};
            int error = 0; 
            while (idx < entries.Length) {
                byte[] buf = new byte[BUF_SIZE]; 
                bool success = UnsafeNativeMethods.ReadEventLog(readHandle, NativeMethods.FORWARDS_READ | NativeMethods.SEEK_READ, 
                                                      oldestEntry+idx, buf, buf.Length, bytesRead, minBytesNeeded);
                if (!success) { 
                    error = Marshal.GetLastWin32Error();
                    // NOTE, [....]: ERROR_PROC_NOT_FOUND used to get returned, but I think that
                    // was because I was calling GetLastError directly instead of GetLastWin32Error.
                    // Making the buffer bigger and trying again seemed to work. I've removed the check 
                    // for ERROR_PROC_NOT_FOUND because I don't think it's necessary any more, but
                    // I can't prove it... 
                    Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "Error from ReadEventLog is " + error.ToString(CultureInfo.InvariantCulture)); 
#if !RETRY_ON_ALL_ERRORS
                    if (error == NativeMethods.ERROR_INSUFFICIENT_BUFFER || error == NativeMethods.ERROR_EVENTLOG_FILE_CHANGED) { 
#endif
                        if (error == NativeMethods.ERROR_EVENTLOG_FILE_CHANGED) {
                            // somewhere along the way the event log file changed - probably it
                            // got cleared while we were looping here. Reset the handle and 
                            // try again.
                            Reset(currentMachineName); 
                        } 
                        // try again with a bigger buffer if necessary
                        else if (minBytesNeeded[0] > buf.Length) { 
                            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "Increasing buffer size from " + buf.Length.ToString(CultureInfo.InvariantCulture) + " to " + minBytesNeeded[0].ToString(CultureInfo.InvariantCulture) + " bytes");
                            buf = new byte[minBytesNeeded[0]];
                        }
                        success = UnsafeNativeMethods.ReadEventLog(readHandle, NativeMethods.FORWARDS_READ | NativeMethods.SEEK_READ, 
                                                         oldestEntry+idx, buf, buf.Length, bytesRead, minBytesNeeded);
                        if (!success) 
                            // we'll just stop right here. 
                            break;
#if !RETRY_ON_ALL_ERRORS 
                    }
                    else {
                        break;
                    } 
#endif
                    error = 0; 
                } 
                entries[idx] = new EventLogEntry(buf, 0, this);
                int sum = IntFrom(buf, 0); 
                idx++;
                while (sum < bytesRead[0] && idx < entries.Length) {
                    entries[idx] = new EventLogEntry(buf, sum, this);
                    sum += IntFrom(buf, sum); 
                    idx++;
                } 
            } 
            if (idx != entries.Length) {
                if (error != 0) 
                    throw new InvalidOperationException(SR.GetString(SR.CantRetrieveEntries), SharedUtils.CreateSafeWin32Exception(error));
                else
                    throw new InvalidOperationException(SR.GetString(SR.CantRetrieveEntries));
            } 
            return entries;
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Searches for all event logs on the local computer and
        ///       creates an array of <see cref='System.Diagnostics.EventLog'/>
        ///       objects to contain the
        ///       list. 
        ///    </para>
        /// </devdoc> 
        public static EventLog[] GetEventLogs() { 
            return GetEventLogs(".");
        } 

        /// <devdoc>
        ///    <para>
        ///       Searches for all event logs on the given computer and 
        ///       creates an array of <see cref='System.Diagnostics.EventLog'/>
        ///       objects to contain the 
        ///       list. 
        ///    </para>
        /// </devdoc> 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static EventLog[] GetEventLogs(string machineName) {
            if (!SyntaxCheck.CheckMachineName(machineName)) { 
                throw new ArgumentException(SR.GetString(SR.InvalidParameter, "machineName", machineName));
            } 
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName);
            permission.Demand(); 

            //Check environment before looking at the registry
            SharedUtils.CheckEnvironment();
 
            string[] logNames = new string[0];
            //SECREVIEW: Note that EventLogPermission is just demmanded above 
            PermissionSet permissionSet = _GetAssertPermSet(); 
            permissionSet.Assert();
 
            RegistryKey eventkey = null;
            try {
                // we figure out what logs are on the machine by looking in the registry.
                eventkey = GetEventLogRegKey(machineName, false); 
                if (eventkey == null)
                    // there's not even an event log service on the machine. 
                    // or, more likely, we don't have the access to read the registry. 
                    throw new InvalidOperationException(SR.GetString(SR.RegKeyMissingShort, EventLogKey, machineName));
                // Most machines will return only { "Application", "System", "Security" }, 
                // but you can create your own if you want.
                logNames = eventkey.GetSubKeyNames();
            }
            finally { 
                if (eventkey != null) eventkey.Close();
                // Revert registry and environment permission asserts 
                CodeAccessPermission.RevertAssert(); 
            }
 
            // now create EventLog objects that point to those logs
            EventLog[] logs = new EventLog[logNames.Length];
            for (int i = 0; i < logNames.Length; i++) {
                EventLog log = new EventLog(); 
                log.Log = logNames[i];
                log.MachineName = machineName; 
                logs[i] = log; 
            }
 
            return logs;
        }

        /// <devdoc> 
        ///     Searches the cache for an entry with the given index
        /// </devdoc> 
        private int GetCachedEntryPos(int entryIndex) { 
            if (cache == null || (boolFlags[Flag_forwards] && entryIndex < firstCachedEntry) ||
                (!boolFlags[Flag_forwards] && entryIndex > firstCachedEntry) || firstCachedEntry == -1) { 
                // the index falls before anything we have in the cache, or the cache
                // is not yet valid
                return -1;
            } 
            // we only know where the beginning of the cache is, not the end, so even
            // if it's past the end of the cache, we'll have to search through the whole 
            // cache to find out. 

            // we're betting heavily that the one they want to see now is close 
            // to the one they asked for last time. We start looking where we
            // stopped last time.

            // We have two loops, one to go forwards and one to go backwards. Only one 
            // of them will ever be executed.
            while (lastSeenEntry < entryIndex) { 
                lastSeenEntry++; 
                if (boolFlags[Flag_forwards]) {
                    lastSeenPos = GetNextEntryPos(lastSeenPos); 
                    if (lastSeenPos >= bytesCached)
                        break;
                }
                else { 
                    lastSeenPos = GetPreviousEntryPos(lastSeenPos);
                    if (lastSeenPos < 0) 
                        break; 
                }
            } 
            while (lastSeenEntry > entryIndex) {
                lastSeenEntry--;
                if (boolFlags[Flag_forwards]) {
                    lastSeenPos = GetPreviousEntryPos(lastSeenPos); 
                    if (lastSeenPos < 0)
                        break; 
                } 
                else {
                    lastSeenPos = GetNextEntryPos(lastSeenPos); 
                    if (lastSeenPos >= bytesCached)
                        break;
                }
            } 
            if (lastSeenPos >= bytesCached) {
                // we ran past the end. move back to the last one and return -1 
                lastSeenPos = GetPreviousEntryPos(lastSeenPos); 
                if (boolFlags[Flag_forwards])
                    lastSeenEntry--; 
                else
                    lastSeenEntry++;
                return -1;
            } 
            else if (lastSeenPos < 0) {
                // we ran past the beginning. move back to the first one and return -1 
                lastSeenPos = 0; 
                if (boolFlags[Flag_forwards])
                    lastSeenEntry++; 
                else
                    lastSeenEntry--;
                return -1;
            } 
            else {
                // we found it. 
                return lastSeenPos; 
            }
        } 

        /// <devdoc>
        ///     Gets the entry at the given index
        /// </devdoc> 
        internal EventLogEntry GetEntryAt(int index) {
            EventLogEntry entry = GetEntryAtNoThrow(index); 
            if (entry == null) 
                throw new ArgumentException(SR.GetString(SR.IndexOutOfBounds, index.ToString(CultureInfo.CurrentCulture)));
            return entry; 
        }

        internal EventLogEntry GetEntryAtNoThrow(int index) {
            if (!IsOpenForRead) 
                OpenForRead(this.machineName);
 
            if (index < 0 || index >= EntryCount) 
                return null;
 
            index += OldestEntryNumber;

            return GetEntryWithOldest(index);
        } 

        private EventLogEntry GetEntryWithOldest(int index) { 
            EventLogEntry entry = null; 
            int entryPos = GetCachedEntryPos(index);
            if (entryPos >= 0) { 
                entry = new EventLogEntry(cache, entryPos, this);
                return entry;
            }
 
            string currentMachineName = this.machineName;
 
            // if we haven't seen the one after this, we were probably going 
            // forwards.
            int flags = 0; 
            if (GetCachedEntryPos(index+1) < 0) {
                flags = NativeMethods.FORWARDS_READ | NativeMethods.SEEK_READ;
                boolFlags[Flag_forwards] = true;
            } 
            else {
                flags = NativeMethods.BACKWARDS_READ | NativeMethods.SEEK_READ; 
                boolFlags[Flag_forwards] = false; 
            }
 
            cache = new byte[BUF_SIZE];
            int[] bytesRead = new int[1];
            int[] minBytesNeeded = new int[] { cache.Length};
            bool success = UnsafeNativeMethods.ReadEventLog(readHandle, flags, index, 
                                                  cache, cache.Length, bytesRead, minBytesNeeded);
            if (!success) { 
                int error = Marshal.GetLastWin32Error(); 
                // NOTE, [....]: ERROR_PROC_NOT_FOUND used to get returned, but I think that
                // was because I was calling GetLastError directly instead of GetLastWin32Error. 
                // Making the buffer bigger and trying again seemed to work. I've removed the check
                // for ERROR_PROC_NOT_FOUND because I don't think it's necessary any more, but
                // I can't prove it...
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "Error from ReadEventLog is " + error.ToString(CultureInfo.InvariantCulture)); 
                if (error == NativeMethods.ERROR_INSUFFICIENT_BUFFER || error == NativeMethods.ERROR_EVENTLOG_FILE_CHANGED) {
                    if (error == NativeMethods.ERROR_EVENTLOG_FILE_CHANGED) { 
                        // Reset() sets the cache null.  But since we're going to call ReadEventLog right after this, 
                        // we need the cache to be something valid.  We'll reuse the old byte array rather
                        // than creating a new one. 
                        byte[] tempcache = cache;
                        Reset(currentMachineName);
                        cache = tempcache;
                    } else { 
                        // try again with a bigger buffer.
                        if (minBytesNeeded[0] > cache.Length) { 
                            cache = new byte[minBytesNeeded[0]]; 
                        }
                    } 
                    success = UnsafeNativeMethods.ReadEventLog(readHandle, NativeMethods.FORWARDS_READ | NativeMethods.SEEK_READ, index,
                                                     cache, cache.Length, bytesRead, minBytesNeeded);
                }
 
                if (!success) {
                    throw new InvalidOperationException(SR.GetString(SR.CantReadLogEntryAt, index.ToString(CultureInfo.CurrentCulture)), SharedUtils.CreateSafeWin32Exception()); 
                } 
            }
            bytesCached = bytesRead[0]; 
            firstCachedEntry = index;
            lastSeenEntry = index;
            lastSeenPos = 0;
            return new EventLogEntry(cache, 0, this); 
        }
 
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        internal static RegistryKey GetEventLogRegKey(string machine, bool writable) { 
            RegistryKey lmkey = null;
            try {
                if (machine.Equals(".")) {
                    lmkey = Registry.LocalMachine; 
                }
                else { 
                    lmkey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machine); 
                }
                if (lmkey != null) 
                    return lmkey.OpenSubKey(EventLogKey, writable);
            }
            finally {
                if (lmkey != null) lmkey.Close(); 
            }
 
            return null; 
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private RegistryKey GetLogRegKey(string currentMachineName, bool writable) {
            string logname = GetLogName(currentMachineName); 
            // we need to verify the logname here again because we might have tried to look it up
            // based on the source and failed. 
            if (!ValidLogName(logname, false)) 
                throw new InvalidOperationException(SR.GetString(SR.BadLogName));
 
            RegistryKey eventkey = null;
            RegistryKey logkey = null;

            try { 
                eventkey = GetEventLogRegKey(currentMachineName, false);
                if (eventkey == null) 
                    throw new InvalidOperationException(SR.GetString(SR.RegKeyMissingShort, EventLogKey, currentMachineName)); 

                logkey = eventkey.OpenSubKey(logname, writable); 
                if (logkey == null)
                    throw new InvalidOperationException(SR.GetString(SR.MissingLog, logname, currentMachineName));
            }
            finally { 
                if (eventkey != null) eventkey.Close();
            } 
 
            return logkey;
        } 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private object GetLogRegValue(string currentMachineName, string valuename) { 
            PermissionSet permissionSet = _GetAssertPermSet();
            permissionSet.Assert(); 
 
            RegistryKey logkey = null;
 
            try {
                logkey = GetLogRegKey(currentMachineName, false);
                if (logkey == null)
                    throw new InvalidOperationException(SR.GetString(SR.MissingLog, GetLogName(currentMachineName), currentMachineName)); 

                object val = logkey.GetValue(valuename); 
                return val; 
            }
            finally { 
                if (logkey != null) logkey.Close();

                // Revert registry and environment permission asserts
                CodeAccessPermission.RevertAssert(); 
            }
        } 
 
        /// <devdoc>
        ///     Finds the index into the cache where the next entry starts 
        /// </devdoc>
        private int GetNextEntryPos(int pos) {
            return pos + IntFrom(cache, pos);
        } 

        /// <devdoc> 
        ///     Finds the index into the cache where the previous entry starts 
        /// </devdoc>
        private int GetPreviousEntryPos(int pos) { 
            // the entries in our buffer come back like this:
            // <length 1> ... <data> ...  <length 1> <length 2> ... <data> ... <length 2> ...
            // In other words, the length for each entry is repeated at the beginning and
            // at the end. This makes it easy to navigate forwards and backwards through 
            // the buffer.
            return pos - IntFrom(cache, pos - 4); 
        } 

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        internal static string GetDllPath(string machineName) {
            return SharedUtils.GetLatestBuildDllDirectory(machineName) + "\\" + DllName;
        } 

        /// <devdoc> 
        ///     Extracts a 32-bit integer from the ubyte buffer, beginning at the byte offset 
        ///     specified in offset.
        /// </devdoc> 
        private static int IntFrom(byte[] buf, int offset) {
            // assumes Little Endian byte order.
            return(unchecked((int)0xFF000000) & (buf[offset+3] << 24)) | (0xFF0000 & (buf[offset+2] << 16)) |
            (0xFF00 & (buf[offset+1] << 8)) | (0xFF & (buf[offset])); 
        }
 
        /// <devdoc> 
        ///    <para>
        ///       Determines whether an event source is registered on the local computer. 
        ///    </para>
        /// </devdoc>
        public static bool SourceExists(string source) {
            return SourceExists(source, "."); 
        }
 
        /// <devdoc> 
        ///    <para>
        ///       Determines whether an event 
        ///       source is registered on a specified computer.
        ///    </para>
        /// </devdoc>
        public static bool SourceExists(string source, string machineName) { 
            if (!SyntaxCheck.CheckMachineName(machineName)) {
                throw new ArgumentException(SR.GetString(SR.InvalidParameter, "machineName", machineName)); 
            } 

            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, machineName); 
            permission.Demand();

            using (RegistryKey keyFound = FindSourceRegistration(source, machineName, true)) {
                return (keyFound != null); 
            }
        } 
 
        /// <devdoc>
        ///     Gets the name of the log that the given source name is registered in. 
        /// </devdoc>
        public static string LogNameFromSourceName(string source, string machineName) {
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, machineName);
            permission.Demand(); 

            using (RegistryKey key = FindSourceRegistration(source, machineName, true)) { 
                if (key == null) 
                    return "";
                else { 
                    string name = key.Name;
                    int whackPos = name.LastIndexOf('\\');
                    // this will work even if whackPos is -1
                    return name.Substring(whackPos+1); 
                }
            } 
        } 

        [ 
        ComVisible(false),
        ResourceExposure(ResourceScope.None),
        ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)
        ] 
        public void ModifyOverflowPolicy(OverflowAction action, int retentionDays) {
            string currentMachineName = this.machineName; 
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
            permission.Demand(); 

            if (action < OverflowAction.DoNotOverwrite || action > OverflowAction.OverwriteOlder)
                throw new InvalidEnumArgumentException("action", (int)action, typeof(OverflowAction));
 
            // this is a long because in the if statement we may need to store values as
            // large as UInt32.MaxValue - 1.  This would overflow an int. 
            long retentionvalue = (long) action; 
            if (action == OverflowAction.OverwriteOlder) {
                if (retentionDays < 1 || retentionDays > 365) 
                    throw new ArgumentOutOfRangeException(SR.GetString(SR.RentionDaysOutOfRange));

                retentionvalue = (long) retentionDays * SecondsPerDay;
            } 

            PermissionSet permissionSet = _GetAssertPermSet(); 
            permissionSet.Assert(); 

            using (RegistryKey logkey = GetLogRegKey(currentMachineName, true)) 
                logkey.SetValue("Retention", retentionvalue, RegistryValueKind.DWord);
        }

 
        /// <devdoc>
        ///     Opens the event log with read access 
        /// </devdoc> 
        private void OpenForRead(string currentMachineName) {
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::OpenForRead"); 

            //Cannot allocate the readHandle if the object has been disposed, since finalization has been suppressed.
            if (this.boolFlags[Flag_disposed])
                throw new ObjectDisposedException(GetType().Name); 

            string logname = GetLogName(currentMachineName); 
 
            if (logname == null || logname.Length==0)
                throw new ArgumentException(SR.GetString(SR.MissingLogProperty)); 

            if (! Exists(logname, currentMachineName) )        // do not open non-existing Log [[....]]
                throw new InvalidOperationException( SR.GetString(SR.LogDoesNotExists, logname, currentMachineName) );
            //Check environment before calling api 
            SharedUtils.CheckEnvironment();
 
            // Clean up cache variables. 
            // [[....]] The initilizing code is put here to guarantee, that first read of events
            //           from log file will start by filling up the cache buffer. 
            lastSeenEntry = 0;
            lastSeenPos = 0;
            bytesCached = 0;
            firstCachedEntry = -1; 

            readHandle = SafeEventLogReadHandle.OpenEventLog(currentMachineName, logname); 
            if (readHandle.IsInvalid) { 
                Win32Exception e = null;
                if (Marshal.GetLastWin32Error() != 0) { 
                    e = SharedUtils.CreateSafeWin32Exception();
                }

                throw new InvalidOperationException(SR.GetString(SR.CantOpenLog, logname.ToString(), currentMachineName), e); 
            }
        } 
 
        /// <devdoc>
        ///     Opens the event log with write access 
        /// </devdoc>
        private void OpenForWrite(string currentMachineName) {
            //Cannot allocate the writeHandle if the object has been disposed, since finalization has been suppressed.
            if (this.boolFlags[Flag_disposed]) 
                throw new ObjectDisposedException(GetType().Name);
 
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::OpenForWrite"); 
            if (sourceName == null || sourceName.Length==0)
                throw new ArgumentException(SR.GetString(SR.NeedSourceToOpen)); 

            //Check environment before calling api
            SharedUtils.CheckEnvironment();
 
            writeHandle = SafeEventLogWriteHandle.RegisterEventSource(currentMachineName, sourceName);
            if (writeHandle.IsInvalid) { 
                Win32Exception e = null; 
                if (Marshal.GetLastWin32Error() != 0) {
                    e = SharedUtils.CreateSafeWin32Exception(); 
                }
                throw new InvalidOperationException(SR.GetString(SR.CantOpenLogAccess, sourceName), e);
            }
        } 

        [ 
        ComVisible(false), 
        ResourceExposure(ResourceScope.Machine),
        ResourceConsumption(ResourceScope.Machine) 
        ]
        public void RegisterDisplayName(string resourceFile, long resourceId) {
            string currentMachineName = this.machineName;
 
            EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Administer, currentMachineName);
            permission.Demand(); 
 
            PermissionSet permissionSet = _GetAssertPermSet();
            permissionSet.Assert(); 

            using (RegistryKey logkey = GetLogRegKey(currentMachineName, true)) {
                logkey.SetValue("DisplayNameFile", resourceFile, RegistryValueKind.ExpandString);
                logkey.SetValue("DisplayNameID", resourceId, RegistryValueKind.DWord); 
            }
        } 
 
        private void Reset(string currentMachineName) {
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::Reset"); 
            // save the state we're in now
            bool openRead = IsOpenForRead;
            bool openWrite = IsOpenForWrite;
            bool isMonitoring = boolFlags[Flag_monitoring]; 
            bool isListening = boolFlags[Flag_registeredAsListener];
 
            // close everything down 
            Close(currentMachineName);
            cache = null; 

            // and get us back into the same state as before
            if (openRead)
                OpenForRead(currentMachineName); 
            if (openWrite)
                OpenForWrite(currentMachineName); 
            if (isListening) 
                StartListening(currentMachineName, GetLogName(currentMachineName));
 
            boolFlags[Flag_monitoring] = isMonitoring;
        }

        [HostProtection(Synchronization=true)] 
        private static void RemoveListenerComponent(EventLog component, string compLogName) {
            lock (InternalSyncObject) { 
                Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::RemoveListenerComponent(" + compLogName + ")"); 

                LogListeningInfo info = (LogListeningInfo) listenerInfos[compLogName]; 
                Debug.Assert(info != null);

                // remove the requested component from the list.
                info.listeningComponents.Remove(component); 
                if (info.listeningComponents.Count != 0)
                    return; 
 
                // if that was the last interested compononent, destroy the handles and stop listening.
 
                info.handleOwner.Dispose();

                //Unregister the thread pool wait handle
                info.registeredWaitHandle.Unregister(info.waitHandle); 
                // close the handle
                info.waitHandle.Close(); 
 
                listenerInfos[compLogName] = null;
            } 
        }

        // The reasoning behind filling these values is historical. WS03 RTM had a race
        // between registry changes and EventLog service, which made the service wait 2 secs 
        // before retrying to see whether all regkey values are present. To avoid this
        // potential lag (worst case up to n*2 secs where n is the number of required regkeys) 
        // between creation and being able to write events, we started filling some of these 
        // values explicitly but for XP and latter OS releases like WS03 SP1 and Vista this
        // is not necessary and in some cases like the "File" key it's plain wrong to write. 
        private static void SetSpecialLogRegValues(RegistryKey logKey, string logName) {
            // Set all the default values for this log.  AutoBackupLogfiles only makes sense in
            // Win2000 SP4, WinXP SP1, and Win2003, but it should alright elsewhere.
 
            // Since we use this method on the existing system logs as well as our own,
            // we need to make sure we don't overwrite any existing values. 
            if (logKey.GetValue("MaxSize") == null) 
                logKey.SetValue("MaxSize", DefaultMaxSize, RegistryValueKind.DWord);
            if (logKey.GetValue("AutoBackupLogFiles") == null) 
                logKey.SetValue("AutoBackupLogFiles", 0, RegistryValueKind.DWord);

            if (!SkipRegPatch) {
                // In Vista, "retention of events for 'n' days" concept is removed 
                if (logKey.GetValue("Retention") == null)
                    logKey.SetValue("Retention", DefaultRetention, RegistryValueKind.DWord); 
 
                if (logKey.GetValue("File") == null) {
                    string filename; 
                    if (logName.Length > 8)
                        filename = @"%SystemRoot%\System32\config\" + logName.Substring(0,8) + ".evt";
                    else
                        filename = @"%SystemRoot%\System32\config\" + logName + ".evt"; 

                    logKey.SetValue("File", filename, RegistryValueKind.ExpandString); 
                } 
            }
        } 

        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        private static void SetSpecialSourceRegValues(RegistryKey sourceLogKey, EventSourceCreationData sourceData) { 
            if (String.IsNullOrEmpty(sourceData.MessageResourceFile))
                sourceLogKey.SetValue("EventMessageFile", GetDllPath(sourceData.MachineName), RegistryValueKind.ExpandString); 
            else 
                sourceLogKey.SetValue("EventMessageFile", FixupPath(sourceData.MessageResourceFile), RegistryValueKind.ExpandString);
 
            if (!String.IsNullOrEmpty(sourceData.ParameterResourceFile))
                sourceLogKey.SetValue("ParameterMessageFile", FixupPath(sourceData.ParameterResourceFile), RegistryValueKind.ExpandString);

            if (!String.IsNullOrEmpty(sourceData.CategoryResourceFile)) { 
                sourceLogKey.SetValue("CategoryMessageFile", FixupPath(sourceData.CategoryResourceFile), RegistryValueKind.ExpandString);
                sourceLogKey.SetValue("CategoryCount", sourceData.CategoryCount, RegistryValueKind.DWord); 
            } 
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static string FixupPath(string path) {
            if (path[0] == '%') 
                return path;
            else 
                return Path.GetFullPath(path); 
        }
 
        /// <devdoc>
        ///     Sets up the event monitoring mechanism.  We don't track event log changes
        ///     unless someone is interested, so we set this up on demand.
        /// </devdoc> 
        [HostProtection(Synchronization=true, ExternalThreading=true)]
        private void StartListening(string currentMachineName, string currentLogName) { 
            // make sure we don't fire events for entries that are already there 
            Debug.Assert(!boolFlags[Flag_registeredAsListener], "StartListening called with boolFlags[Flag_registeredAsListener] true.");
            lastSeenCount = EntryCount + OldestEntryNumber; 
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::StartListening: lastSeenCount = " + lastSeenCount);
            AddListenerComponent(this, currentMachineName, currentLogName);
            boolFlags[Flag_registeredAsListener] = true;
        } 

        private void StartRaisingEvents(string currentMachineName, string currentLogName) { 
            if (!boolFlags[Flag_initializing] && !boolFlags[Flag_monitoring] && !DesignMode) { 
                StartListening(currentMachineName, currentLogName);
            } 
            boolFlags[Flag_monitoring] = true;
        }

        private static void StaticCompletionCallback(object context, bool wasSignaled) { 
            LogListeningInfo info = (LogListeningInfo) context;
 
            // get a snapshot of the components to fire the event on 
            EventLog[] interestedComponents = (EventLog[]) info.listeningComponents.ToArray(typeof(EventLog));
 
            Debug.WriteLineIf(CompModSwitches.EventLog.TraceVerbose, "EventLog::StaticCompletionCallback: notifying " + interestedComponents.Length + " components.");

            for (int i = 0; i < interestedComponents.Length; i++)
                interestedComponents[i].CompletionCallback(null); 
        }
 
        /// <devdoc> 
        ///     Tears down the event listening mechanism.  This is called when the last
        ///     interested party removes their event handler. 
        /// </devdoc>
        [HostProtection(Synchronization=true, ExternalThreading=true)]
        private void StopListening(/*string currentMachineName,*/ string currentLogName) {
            Debug.Assert(boolFlags[Flag_registeredAsListener], "StopListening called without StartListening."); 
            RemoveListenerComponent(this, currentLogName);
            boolFlags[Flag_registeredAsListener] = false; 
        } 

        /// <devdoc> 
        /// </devdoc>
        private void StopRaisingEvents(/*string currentMachineName,*/ string currentLogName) {
            if (!boolFlags[Flag_initializing] && boolFlags[Flag_monitoring] && !DesignMode) {
                StopListening(currentLogName); 
            }
            boolFlags[Flag_monitoring] = false; 
        } 

        // Format message in specific DLL. Return <null> on failure. 
        internal static string TryFormatMessage(SafeLibraryHandle hModule, uint messageNum, string[] insertionStrings) {
            string msg = null;

            int msgLen = 0; 
            StringBuilder buf = new StringBuilder(1024);
            int flags = NativeMethods.FORMAT_MESSAGE_FROM_HMODULE | NativeMethods.FORMAT_MESSAGE_ARGUMENT_ARRAY; 
 
            IntPtr[] addresses = new IntPtr[insertionStrings.Length];
            GCHandle[] handles = new GCHandle[insertionStrings.Length]; 
            GCHandle stringsRoot = GCHandle.Alloc(addresses, GCHandleType.Pinned);

            // Make sure that we don't try to pass in a zero length array of addresses.  If there are no insertion strings,
            // we'll use the FORMAT_MESSAGE_IGNORE_INSERTS flag . 
            if (insertionStrings.Length == 0) {
                flags |= NativeMethods.FORMAT_MESSAGE_IGNORE_INSERTS; 
            } 

            try { 
                for (int i=0; i<handles.Length; i++) {
                    handles[i] = GCHandle.Alloc(insertionStrings[i], GCHandleType.Pinned);
                    addresses[i] = handles[i].AddrOfPinnedObject();
                } 
                int lastError = NativeMethods.ERROR_INSUFFICIENT_BUFFER;
                while (msgLen == 0 && lastError == NativeMethods.ERROR_INSUFFICIENT_BUFFER) { 
                    msgLen = SafeNativeMethods.FormatMessage( 
                        flags,
                        hModule, 
                        messageNum,
                        0,
                        buf,
                        buf.Capacity, 
                        addresses);
 
                    if (msgLen == 0) { 
                        lastError = Marshal.GetLastWin32Error();
                        if (lastError == NativeMethods.ERROR_INSUFFICIENT_BUFFER) 
                            buf.Capacity = buf.Capacity * 2;
                    }
                }
            } 
            catch {
                msgLen = 0;              // return empty on failure 
            } 
            finally  {
                for (int i=0; i<handles.Length; i++) { 
                    if (handles[i].IsAllocated) handles[i].Free();
                }
                stringsRoot.Free();
            } 

            if (msgLen > 0) { 
                msg = buf.ToString(); 
                // chop off a single CR/LF pair from the end if there is one. FormatMessage always appends one extra.
                if (msg.Length > 1 && msg[msg.Length-1] == '\n') 
                    msg = msg.Substring(0, msg.Length-2);
            }

            return msg; 
        }
 
        // CharIsPrintable used to be Char.IsPrintable, but Jay removed it and 
        // is forcing people to use the Unicode categories themselves.  Copied
        // the code here. 
        private static bool CharIsPrintable(char c) {
            UnicodeCategory uc = Char.GetUnicodeCategory(c);
            return (!(uc == UnicodeCategory.Control) || (uc == UnicodeCategory.Format) ||
                    (uc == UnicodeCategory.LineSeparator) || (uc == UnicodeCategory.ParagraphSeparator) || 
            (uc == UnicodeCategory.OtherNotAssigned));
        } 
 
        // SECREVIEW: Make sure this method catches all the strange cases.
        internal static bool ValidLogName(string logName, bool ignoreEmpty) { 
            // No need to trim here since the next check will verify that there are no spaces.
            // We need to ignore the empty string as an invalid log name sometimes because it can
            // be passed in from our default constructor.
            if (logName.Length == 0 && !ignoreEmpty) 
                return false;
 
            //any space, backslash, asterisk, or question mark is bad 
            //any non-printable characters are also bad
            foreach (char c in logName) 
                if (!CharIsPrintable(c) || (c == '\\') || (c == '*') || (c == '?'))
                    return false;

            return true; 
        }
 
        private void VerifyAndCreateSource(string sourceName, string currentMachineName) { 
            if (boolFlags[Flag_sourceVerified])
                return; 

            if (!SourceExists(sourceName, currentMachineName)) {
                Mutex mutex = null;
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try {
                    SharedUtils.EnterMutex(eventLogMutexName, ref mutex); 
                    if (!SourceExists(sourceName, currentMachineName)) { 
                        if (GetLogName(currentMachineName) == null)
                            SetLogName(currentMachineName, "Application"); 
                        // we automatically add an entry in the registry if there's not already
                        // one there for this source
                        CreateEventSource(new EventSourceCreationData(sourceName, GetLogName(currentMachineName), currentMachineName));
                        // The user may have set a custom log and tried to read it before trying to 
                        // write. Due to a quirk in the event log API, we would have opened the Application
                        // log to read (because the custom log wasn't there). Now that we've created 
                        // the custom log, we should close so that when we re-open, we get a read 
                        // handle on the _new_ log instead of the Application log.
                        Reset(currentMachineName); 
                    }
                    else {
                        string rightLogName = LogNameFromSourceName(sourceName, currentMachineName);
                        string currentLogName = GetLogName(currentMachineName); 
                        if (rightLogName != null && currentLogName != null && String.Compare(rightLogName, currentLogName, StringComparison.OrdinalIgnoreCase) != 0)
                            throw new ArgumentException(SR.GetString(SR.LogSourceMismatch, Source.ToString(), currentLogName, rightLogName)); 
                    } 

                } 
                finally {
                    if (mutex != null) {
                        mutex.ReleaseMutex();
                        mutex.Close(); 
                    }
                } 
            } 
            else {
                string rightLogName = LogNameFromSourceName(sourceName, currentMachineName); 
                string currentLogName = GetLogName(currentMachineName);
                if (rightLogName != null && currentLogName != null && String.Compare(rightLogName, currentLogName, StringComparison.OrdinalIgnoreCase) != 0)
                    throw new ArgumentException(SR.GetString(SR.LogSourceMismatch, Source.ToString(), currentLogName, rightLogName));
            } 

            boolFlags[Flag_sourceVerified] = true; 
        } 

        /// <devdoc> 
        ///    <para>
        ///       Writes an information type entry with the given message text to the event log.
        ///    </para>
        /// </devdoc> 
        public void WriteEntry(string message) {
            WriteEntry(message, EventLogEntryType.Information, (short) 0, 0, null); 
        } 

        /// <devdoc> 
        /// </devdoc>
        public static void WriteEntry(string source, string message) {
            WriteEntry(source, message, EventLogEntryType.Information, (short) 0, 0, null);
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Writes an entry of the specified <see cref='System.Diagnostics.EventLogEntryType'/> to the event log. Valid types are
        ///    <see langword='Error'/>, <see langword='Warning'/>, <see langword='Information'/>, 
        ///    <see langword='Success Audit'/>, and <see langword='Failure Audit'/>.
        ///    </para>
        /// </devdoc>
        public void WriteEntry(string message, EventLogEntryType type) { 
            WriteEntry(message, type, (short) 0, 0, null);
        } 
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public static void WriteEntry(string source, string message, EventLogEntryType type) {
            WriteEntry(source, message, type, (short) 0, 0, null);
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Writes an entry of the specified <see cref='System.Diagnostics.EventLogEntryType'/>
        ///       and with the 
        ///       user-defined <paramref name="eventID"/>
        ///       to
        ///       the event log.
        ///    </para> 
        /// </devdoc>
        public void WriteEntry(string message, EventLogEntryType type, int eventID) { 
            WriteEntry(message, type, eventID, 0, null); 
        }
 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID) { 
            WriteEntry(source, message, type, eventID, 0, null);
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Writes an entry of the specified type with the
        ///       user-defined <paramref name="eventID"/> and <paramref name="category"/>
        ///       to the event log. The <paramref name="category"/>
        ///       can be used by the event viewer to filter events in the log. 
        ///    </para>
        /// </devdoc> 
        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category) { 
            WriteEntry(message, type, eventID, category, null);
        } 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID, short category) {
            WriteEntry(source, message, type, eventID, category, null); 
        } 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID, short category,
                               byte[] rawData) { 
            EventLog log = new EventLog();
            try { 
                log.Source = source; 
                log.WriteEntry(message, type, eventID, category, rawData);
            } 
            finally {
                log.Dispose(true);
            }
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Writes an entry of the specified type with the
        ///       user-defined <paramref name="eventID"/> and <paramref name="category"/> to the event log, and appends binary data to 
        ///       the message. The Event Viewer does not interpret this data; it
        ///       displays raw data only in a combined hexadecimal and text format.
        ///    </para>
        /// </devdoc> 
        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category,
                               byte[] rawData) { 
 
            if (eventID < 0 || eventID > ushort.MaxValue)
                throw new ArgumentException(SR.GetString(SR.EventID, eventID, 0, (int)ushort.MaxValue)); 

            if (Source.Length == 0)
                throw new ArgumentException(SR.GetString(SR.NeedSourceToWrite));
 
            if (!Enum.IsDefined(typeof(EventLogEntryType), type))
                throw new InvalidEnumArgumentException("type", (int)type, typeof(EventLogEntryType)); 
 
            string currentMachineName = machineName;
            if (!boolFlags[Flag_writeGranted]) { 
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName);
                permission.Demand();
                boolFlags[Flag_writeGranted] = true;
            } 

            VerifyAndCreateSource(sourceName, currentMachineName); 
 
            // now that the source has been hooked up to our DLL, we can use "normal"
            // (message-file driven) logging techniques. 
            // Our DLL has 64K different entries; all of them just display the first
            // insertion string.
            InternalWriteEvent((uint)eventID, (ushort)category, type, new string[] { message}, rawData, currentMachineName);
        } 

        [ 
        ComVisible(false) 
        ]
        public void WriteEvent(EventInstance instance, params Object[] values) { 
            WriteEvent(instance, null, values);
        }

        [ 
        ComVisible(false)
        ] 
        public void WriteEvent(EventInstance instance, byte[] data, params Object[] values) { 
            if (instance == null)
                throw new ArgumentNullException("instance"); 
            if (Source.Length == 0)
                throw new ArgumentException(SR.GetString(SR.NeedSourceToWrite));

            string currentMachineName = machineName; 
            if (!boolFlags[Flag_writeGranted]) {
                EventLogPermission permission = new EventLogPermission(EventLogPermissionAccess.Write, currentMachineName); 
                permission.Demand(); 
                boolFlags[Flag_writeGranted] = true;
            } 

            VerifyAndCreateSource(Source, currentMachineName);

            string[] strings = null; 

            if (values != null) { 
                strings = new string[values.Length]; 
                for (int i=0; i<values.Length; i++) {
                    if (values[i] != null) 
                        strings[i] = values[i].ToString();
                    else
                        strings[i] = String.Empty;
                } 
            }
 
            InternalWriteEvent((uint) instance.InstanceId, (ushort) instance.CategoryId, instance.EntryType, strings, data, currentMachineName); 
        }
 
        public static void WriteEvent(string source, EventInstance instance, params Object[] values) {
            using(EventLog log = new EventLog()) {
                log.Source = source;
                log.WriteEvent(instance, null, values); 
            }
        } 
 
        public static void WriteEvent(string source, EventInstance instance, byte[] data, params Object[] values) {
            using(EventLog log = new EventLog()) { 
                log.Source = source;
                log.WriteEvent(instance, data, values);
            }
        } 

        private void InternalWriteEvent(uint eventID, ushort category, EventLogEntryType type, string[] strings, 
                                byte[] rawData, string currentMachineName) { 

            // check arguments 
            if (strings == null)
                strings = new string[0];
            if (strings.Length >= 256)
                throw new ArgumentException(SR.GetString(SR.TooManyReplacementStrings)); 

            for (int i = 0; i < strings.Length; i++) { 
                if (strings[i] == null) 
                    strings[i] = String.Empty;
 
                // make sure the strings aren't too long.  MSDN says each string has a limit of 32k (32768) characters, but
                // experimentation shows that it doesn't like anything larger than 32766
                if (strings[i].Length > 32766)
                    throw new ArgumentException(SR.GetString(SR.LogEntryTooLong)); 
            }
            if (rawData == null) 
                rawData = new byte[0]; 

            if (Source.Length == 0) 
                throw new ArgumentException(SR.GetString(SR.NeedSourceToWrite));

            if (!IsOpenForWrite)
                OpenForWrite(currentMachineName); 

            // pin each of the strings in memory 
            IntPtr[] stringRoots = new IntPtr[strings.Length]; 
            GCHandle[] stringHandles = new GCHandle[strings.Length];
            GCHandle stringsRootHandle = GCHandle.Alloc(stringRoots, GCHandleType.Pinned); 
            try {
                for (int strIndex = 0; strIndex < strings.Length; strIndex++) {
                    stringHandles[strIndex] = GCHandle.Alloc(strings[strIndex], GCHandleType.Pinned);
                    stringRoots[strIndex] = stringHandles[strIndex].AddrOfPinnedObject(); 
                }
 
                byte[] sid = null; 
                // actually report the event
                bool success = UnsafeNativeMethods.ReportEvent(writeHandle, (short) type, category, eventID, 
                                                     sid, (short) strings.Length, rawData.Length, new HandleRef(this, stringsRootHandle.AddrOfPinnedObject()), rawData);
                if (!success) {
                    //Trace("WriteEvent", "Throwing Win32Exception");
                    throw SharedUtils.CreateSafeWin32Exception(); 
                }
            } 
            finally { 
                // now free the pinned strings
                for (int i = 0; i < strings.Length; i++) { 
                    if (stringHandles[i].IsAllocated)
                        stringHandles[i].Free();
                }
                stringsRootHandle.Free(); 
            }
        } 
 
        private class LogListeningInfo {
            public EventLog handleOwner; 
            public RegisteredWaitHandle registeredWaitHandle;
            public WaitHandle waitHandle;
            public ArrayList listeningComponents = new ArrayList();
        } 

        private class EventLogWaitHandle : WaitHandle { 
            public EventLogWaitHandle(SafeEventHandle eventLogNativeHandle) { 
                this.SafeWaitHandle = new SafeWaitHandle(eventLogNativeHandle.DangerousGetHandle(), true);
                eventLogNativeHandle.SetHandleAsInvalid(); 
            }
        }

    } 

} 
