//------------------------------------------------------------------------------ 
// <copyright file="DbConnectionPoolCounters.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Data.ProviderBase { 

    using System; 
    using System.Collections;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Globalization; 
    using System.Reflection;
    using System.Runtime.ConstrainedExecution; 
    using System.Security; 
    using System.Security.Permissions;
    using System.Security.Principal; 

    internal abstract class DbConnectionPoolCounters {
        private static class CreationData {
#if CREATEONLY 
            internal const int NumberOfCounters = 14;
#endif 
 
            static internal readonly CounterCreationData HardConnectsPerSecond = new CounterCreationData(
                                                                        "HardConnectsPerSecond", 
                                                                        "The number of actual connections per second that are being made to servers",
                                                                        PerformanceCounterType.RateOfCountsPerSecond32);

            static internal readonly CounterCreationData HardDisconnectsPerSecond = new CounterCreationData( 
                                                                        "HardDisconnectsPerSecond",
                                                                        "The number of actual disconnects per second that are being made to servers", 
                                                                        PerformanceCounterType.RateOfCountsPerSecond32); 

            static internal readonly CounterCreationData SoftConnectsPerSecond = new CounterCreationData( 
                                                                        "SoftConnectsPerSecond",
                                                                        "The number of connections we get from the pool per second",
                                                                        PerformanceCounterType.RateOfCountsPerSecond32);
 
            static internal readonly CounterCreationData SoftDisconnectsPerSecond = new CounterCreationData(
                                                                        "SoftDisconnectsPerSecond", 
                                                                        "The number of connections we return to the pool per second", 
                                                                        PerformanceCounterType.RateOfCountsPerSecond32);
 
            static internal readonly CounterCreationData NumberOfNonPooledConnections = new CounterCreationData(
                                                                        "NumberOfNonPooledConnections",
                                                                        "The number of connections that are not using connection pooling",
                                                                        PerformanceCounterType.NumberOfItems32); 

            static internal readonly CounterCreationData NumberOfPooledConnections = new CounterCreationData( 
                                                                        "NumberOfPooledConnections", 
                                                                        "The number of connections that are managed by the connection pooler",
                                                                        PerformanceCounterType.NumberOfItems32); 

            static internal readonly CounterCreationData NumberOfActiveConnectionPoolGroups = new CounterCreationData(
                                                                        "NumberOfActiveConnectionPoolGroups",
                                                                        "The number of unique connection strings", 
                                                                        PerformanceCounterType.NumberOfItems32);
 
            static internal readonly CounterCreationData NumberOfInactiveConnectionPoolGroups = new CounterCreationData( 
                                                                        "NumberOfInactiveConnectionPoolGroups",
                                                                        "The number of unique connection strings waiting for pruning", 
                                                                        PerformanceCounterType.NumberOfItems32);

            static internal readonly CounterCreationData NumberOfActiveConnectionPools = new CounterCreationData(
                                                                        "NumberOfActiveConnectionPools", 
                                                                        "The number of connection pools",
                                                                        PerformanceCounterType.NumberOfItems32); 
 
            static internal readonly CounterCreationData NumberOfInactiveConnectionPools = new CounterCreationData(
                                                                        "NumberOfInactiveConnectionPools", 
                                                                        "The number of connection pools",
                                                                        PerformanceCounterType.NumberOfItems32);

            static internal readonly CounterCreationData NumberOfActiveConnections = new CounterCreationData( 
                                                                        "NumberOfActiveConnections",
                                                                        "The number of connections currently in-use", 
                                                                        PerformanceCounterType.NumberOfItems32); 

            static internal readonly CounterCreationData NumberOfFreeConnections = new CounterCreationData( 
                                                                        "NumberOfFreeConnections",
                                                                        "The number of connections currently available for use",
                                                                        PerformanceCounterType.NumberOfItems32);
 
            static internal readonly CounterCreationData NumberOfStasisConnections = new CounterCreationData(
                                                                        "NumberOfStasisConnections", 
                                                                        "The number of connections currently waiting to be made ready for use", 
                                                                        PerformanceCounterType.NumberOfItems32);
 
            static internal readonly CounterCreationData NumberOfReclaimedConnections = new CounterCreationData(
                                                                        "NumberOfReclaimedConnections",
                                                                        "The number of connections we reclaim from GC'd external connections",
                                                                        PerformanceCounterType.NumberOfItems32); 
        };
 
        sealed internal class Counter { 
            private PerformanceCounter _instance;
 
            internal Counter (string categoryName, string instanceName, string counterName, PerformanceCounterType counterType) {
                if (ADP.IsPlatformNT5) {
                    try {
                        if (!ADP.IsEmpty(categoryName) && !ADP.IsEmpty(instanceName)) { 
                            PerformanceCounter instance = new PerformanceCounter();
                            instance.CategoryName = categoryName; 
                            instance.CounterName = counterName; 
                            instance.InstanceName = instanceName;
                            instance.InstanceLifetime = PerformanceCounterInstanceLifetime.Process; 
                            instance.ReadOnly = false;
                            instance.RawValue = 0;  // make sure we start out at zero
                            _instance = instance;
                        } 
                    }
                    catch (InvalidOperationException e) { 
                        ADP.TraceExceptionWithoutRethrow(e); 
                        //
                    } 
                }
            }

 
            internal void Decrement() {
                PerformanceCounter instance = _instance; 
                if (null != instance) { 
                    instance.Decrement();
                } 
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal void Dispose () { // 
                PerformanceCounter instance = _instance;
                _instance = null; 
                if (null != instance) { 
                    instance.RemoveInstance();
                    // should we be calling instance.Close? 
                    // if we do will it exacerbate the Dispose vs. Decrement race condition
                    //instance.Close();
                }
            } 

            internal void Increment() { 
                PerformanceCounter instance = _instance; 
                if (null != instance) {
                    instance.Increment(); 
                }
            }
        };
 
        internal readonly Counter HardConnectsPerSecond;
        internal readonly Counter HardDisconnectsPerSecond; 
        internal readonly Counter SoftConnectsPerSecond; 
        internal readonly Counter SoftDisconnectsPerSecond;
        internal readonly Counter NumberOfNonPooledConnections; 
        internal readonly Counter NumberOfPooledConnections;
        internal readonly Counter NumberOfActiveConnectionPoolGroups;
        internal readonly Counter NumberOfInactiveConnectionPoolGroups;
        internal readonly Counter NumberOfActiveConnectionPools; 
        internal readonly Counter NumberOfInactiveConnectionPools;
        internal readonly Counter NumberOfActiveConnections; 
        internal readonly Counter NumberOfFreeConnections; 
        internal readonly Counter NumberOfStasisConnections;
        internal readonly Counter NumberOfReclaimedConnections; 

        protected DbConnectionPoolCounters() : this(null, null) {
        }
 
        protected DbConnectionPoolCounters(string categoryName, string categoryHelp) {
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(this.UnloadEventHandler); 
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(this.ExitEventHandler); 
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(this.ExceptionEventHandler);
 
            string instanceName = null;

            if (!ADP.IsEmpty(categoryName)) {
                if (ADP.IsPlatformNT5) { 
#if CREATEONLY
                    if (!PerformanceCounterCategory.Exists(categoryName)) { 
                        CreateCounters(categoryName, categoryHelp); 
                    }
#endif //CREATEONLY 
                    instanceName = GetInstanceName();
                }
            }
 
            // level 0-3: hard connects/disconnects, plus basic pool/pool entry statistics
            string basicCategoryName = categoryName; 
            HardConnectsPerSecond = new Counter(basicCategoryName, instanceName, CreationData.HardConnectsPerSecond.CounterName, CreationData.HardConnectsPerSecond.CounterType); 
            HardDisconnectsPerSecond = new Counter(basicCategoryName, instanceName, CreationData.HardDisconnectsPerSecond.CounterName, CreationData.HardDisconnectsPerSecond.CounterType);
            NumberOfNonPooledConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfNonPooledConnections.CounterName, CreationData.NumberOfNonPooledConnections.CounterType); 
            NumberOfPooledConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfPooledConnections.CounterName, CreationData.NumberOfPooledConnections.CounterType);
            NumberOfActiveConnectionPoolGroups = new Counter(basicCategoryName, instanceName, CreationData.NumberOfActiveConnectionPoolGroups.CounterName, CreationData.NumberOfActiveConnectionPoolGroups.CounterType);
            NumberOfInactiveConnectionPoolGroups = new Counter(basicCategoryName, instanceName, CreationData.NumberOfInactiveConnectionPoolGroups.CounterName, CreationData.NumberOfInactiveConnectionPoolGroups.CounterType);
            NumberOfActiveConnectionPools = new Counter(basicCategoryName, instanceName, CreationData.NumberOfActiveConnectionPools.CounterName, CreationData.NumberOfActiveConnectionPools.CounterType); 
            NumberOfInactiveConnectionPools = new Counter(basicCategoryName, instanceName, CreationData.NumberOfInactiveConnectionPools.CounterName, CreationData.NumberOfInactiveConnectionPools.CounterType);
            NumberOfStasisConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfStasisConnections.CounterName, CreationData.NumberOfStasisConnections.CounterType); 
            NumberOfReclaimedConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfReclaimedConnections.CounterName, CreationData.NumberOfReclaimedConnections.CounterType); 

            // level 4: expensive stuff 
            string verboseCategoryName = null;
            if (!ADP.IsEmpty(categoryName)) {
                // don't load TraceSwitch if no categoryName so that Odbc/OleDb have a chance of not loading TraceSwitch
                // which are also used by System.Diagnostics.PerformanceCounter.ctor & System.Transactions.get_Current 
                TraceSwitch perfCtrSwitch = new TraceSwitch("ConnectionPoolPerformanceCounterDetail", "level of detail to track with connection pool performance counters");
                if (TraceLevel.Verbose == perfCtrSwitch.Level) { 
                    verboseCategoryName = categoryName; 
                }
            } 
            SoftConnectsPerSecond = new Counter(verboseCategoryName, instanceName, CreationData.SoftConnectsPerSecond.CounterName, CreationData.SoftConnectsPerSecond.CounterType);
            SoftDisconnectsPerSecond = new Counter(verboseCategoryName, instanceName, CreationData.SoftDisconnectsPerSecond.CounterName, CreationData.SoftDisconnectsPerSecond.CounterType);
            NumberOfActiveConnections = new Counter(verboseCategoryName, instanceName, CreationData.NumberOfActiveConnections.CounterName, CreationData.NumberOfActiveConnections.CounterType);
            NumberOfFreeConnections = new Counter(verboseCategoryName, instanceName, CreationData.NumberOfFreeConnections.CounterName, CreationData.NumberOfFreeConnections.CounterType); 
        }
 
        [FileIOPermission(SecurityAction.Assert, Unrestricted=true)] 
        private string GetAssemblyName() {
            string result = null; 

            // First try GetEntryAssembly name, then AppDomain.FriendlyName.
            Assembly assembly = Assembly.GetEntryAssembly();
 
            if (null != assembly) {
                AssemblyName name = assembly.GetName(); 
                if (name != null) { 
                    result = name.Name; // MDAC 73469
                } 
            }
            return result;
        }
 
        private string GetInstanceName() {
            string result = null; 
 
            string instanceName = GetAssemblyName(); // instance perfcounter name
 
            if (ADP.IsEmpty(instanceName)) {
                AppDomain appDomain = AppDomain.CurrentDomain;
                if (null != appDomain) {
                    instanceName = appDomain.FriendlyName; 
                }
            } 
 
            int pid = SafeNativeMethods.GetCurrentProcessId();
 

            //

 

            result = String.Format((IFormatProvider)null, "{0}[{1}]", instanceName, pid); 
            result = result.Replace('(','[').Replace(')',']').Replace('#','_').Replace('/','_').Replace('\\','_'); 

            return result; 
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void Dispose() { 
            // ExceptionEventHandler with IsTerminiating may be called before
            // the Connection Close is called or the variables are initialized 
            SafeDispose(HardConnectsPerSecond); 
            SafeDispose(HardDisconnectsPerSecond);
            SafeDispose(SoftConnectsPerSecond); 
            SafeDispose(SoftDisconnectsPerSecond);
            SafeDispose(NumberOfNonPooledConnections);
            SafeDispose(NumberOfPooledConnections);
            SafeDispose(NumberOfActiveConnectionPoolGroups); 
            SafeDispose(NumberOfInactiveConnectionPoolGroups);
            SafeDispose(NumberOfActiveConnectionPools); 
            SafeDispose(NumberOfActiveConnections); 
            SafeDispose(NumberOfFreeConnections);
            SafeDispose(NumberOfStasisConnections); 
            SafeDispose(NumberOfReclaimedConnections);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        private void SafeDispose(Counter counter) { // WebData 103603
            if (null != counter) { 
                counter.Dispose(); 
            }
        } 

        [PrePrepareMethod]
        void ExceptionEventHandler (object sender, UnhandledExceptionEventArgs e) {
            if ((null != e) && e.IsTerminating) { 
                Dispose();
            } 
        } 

        [PrePrepareMethod] 
        void ExitEventHandler (object sender, EventArgs e) {
            Dispose();
        }
 
        [PrePrepareMethod]
        void UnloadEventHandler (object sender, EventArgs e) { 
            Dispose(); 
        }
 
#if CREATEONLY
        private void CreateCounters(string categoryName, string categoryHelp) {
            int i = 0;
 
            CounterCreationData[]  myCounterCreationData = new CounterCreationData[CreationData.NumberOfCounters];
 
            myCounterCreationData[i++] = CreationData.HardConnectsPerSecond; 
            myCounterCreationData[i++] = CreationData.HardDisconnectsPerSecond;
            myCounterCreationData[i++] = CreationData.SoftConnectsPerSecond; 
            myCounterCreationData[i++] = CreationData.SoftDisconnectsPerSecond;
            myCounterCreationData[i++] = CreationData.NumberOfNonPooledConnections;
            myCounterCreationData[i++] = CreationData.NumberOfPooledConnections;
            myCounterCreationData[i++] = CreationData.NumberOfActiveConnectionPoolGroups; 
            myCounterCreationData[i++] = CreationData.NumberOfInactiveConnectionPoolGroups;
            myCounterCreationData[i++] = CreationData.NumberOfActiveConnectionPools; 
            myCounterCreationData[i++] = CreationData.NumberOfInactiveConnectionPools; 
            myCounterCreationData[i++] = CreationData.NumberOfActiveConnections;
            myCounterCreationData[i++] = CreationData.NumberOfFreeConnections; 
            myCounterCreationData[i++] = CreationData.NumberOfStasisConnections;
            myCounterCreationData[i++] = CreationData.NumberOfReclaimedConnections;

            CounterCreationDataCollection myCounterCollection = new CounterCreationDataCollection(myCounterCreationData); 
            CreateCountersSafe(categoryName, categoryHelp, myCounterCollection);
 
            for(i = 0; i < myCounterCollection.Count; i++) { 
                myCounterCollection.Remove(myCounterCreationData[i]);
            } 
        }

        private void CreateCountersSafe(string categoryName, string categoryHelp, CounterCreationDataCollection myCounterCollection) {
            (new PerformanceCounterPermission(PerformanceCounterPermissionAccess.Instrument, ".", categoryName)).Assert(); 
            try {
                PerformanceCounterCategory.Create(categoryName, categoryHelp, myCounterCollection); 
            } 
            finally {
                PerformanceCounterPermission.RevertAssert(); 
            }
        }
#endif //CREATEONLY
    } 

    sealed internal class DbConnectionPoolCountersNoCounters : DbConnectionPoolCounters { 
 
        public static readonly DbConnectionPoolCountersNoCounters SingletonInstance = new DbConnectionPoolCountersNoCounters();
 
        private DbConnectionPoolCountersNoCounters() : base () {
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbConnectionPoolCounters.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Data.ProviderBase { 

    using System; 
    using System.Collections;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Globalization; 
    using System.Reflection;
    using System.Runtime.ConstrainedExecution; 
    using System.Security; 
    using System.Security.Permissions;
    using System.Security.Principal; 

    internal abstract class DbConnectionPoolCounters {
        private static class CreationData {
#if CREATEONLY 
            internal const int NumberOfCounters = 14;
#endif 
 
            static internal readonly CounterCreationData HardConnectsPerSecond = new CounterCreationData(
                                                                        "HardConnectsPerSecond", 
                                                                        "The number of actual connections per second that are being made to servers",
                                                                        PerformanceCounterType.RateOfCountsPerSecond32);

            static internal readonly CounterCreationData HardDisconnectsPerSecond = new CounterCreationData( 
                                                                        "HardDisconnectsPerSecond",
                                                                        "The number of actual disconnects per second that are being made to servers", 
                                                                        PerformanceCounterType.RateOfCountsPerSecond32); 

            static internal readonly CounterCreationData SoftConnectsPerSecond = new CounterCreationData( 
                                                                        "SoftConnectsPerSecond",
                                                                        "The number of connections we get from the pool per second",
                                                                        PerformanceCounterType.RateOfCountsPerSecond32);
 
            static internal readonly CounterCreationData SoftDisconnectsPerSecond = new CounterCreationData(
                                                                        "SoftDisconnectsPerSecond", 
                                                                        "The number of connections we return to the pool per second", 
                                                                        PerformanceCounterType.RateOfCountsPerSecond32);
 
            static internal readonly CounterCreationData NumberOfNonPooledConnections = new CounterCreationData(
                                                                        "NumberOfNonPooledConnections",
                                                                        "The number of connections that are not using connection pooling",
                                                                        PerformanceCounterType.NumberOfItems32); 

            static internal readonly CounterCreationData NumberOfPooledConnections = new CounterCreationData( 
                                                                        "NumberOfPooledConnections", 
                                                                        "The number of connections that are managed by the connection pooler",
                                                                        PerformanceCounterType.NumberOfItems32); 

            static internal readonly CounterCreationData NumberOfActiveConnectionPoolGroups = new CounterCreationData(
                                                                        "NumberOfActiveConnectionPoolGroups",
                                                                        "The number of unique connection strings", 
                                                                        PerformanceCounterType.NumberOfItems32);
 
            static internal readonly CounterCreationData NumberOfInactiveConnectionPoolGroups = new CounterCreationData( 
                                                                        "NumberOfInactiveConnectionPoolGroups",
                                                                        "The number of unique connection strings waiting for pruning", 
                                                                        PerformanceCounterType.NumberOfItems32);

            static internal readonly CounterCreationData NumberOfActiveConnectionPools = new CounterCreationData(
                                                                        "NumberOfActiveConnectionPools", 
                                                                        "The number of connection pools",
                                                                        PerformanceCounterType.NumberOfItems32); 
 
            static internal readonly CounterCreationData NumberOfInactiveConnectionPools = new CounterCreationData(
                                                                        "NumberOfInactiveConnectionPools", 
                                                                        "The number of connection pools",
                                                                        PerformanceCounterType.NumberOfItems32);

            static internal readonly CounterCreationData NumberOfActiveConnections = new CounterCreationData( 
                                                                        "NumberOfActiveConnections",
                                                                        "The number of connections currently in-use", 
                                                                        PerformanceCounterType.NumberOfItems32); 

            static internal readonly CounterCreationData NumberOfFreeConnections = new CounterCreationData( 
                                                                        "NumberOfFreeConnections",
                                                                        "The number of connections currently available for use",
                                                                        PerformanceCounterType.NumberOfItems32);
 
            static internal readonly CounterCreationData NumberOfStasisConnections = new CounterCreationData(
                                                                        "NumberOfStasisConnections", 
                                                                        "The number of connections currently waiting to be made ready for use", 
                                                                        PerformanceCounterType.NumberOfItems32);
 
            static internal readonly CounterCreationData NumberOfReclaimedConnections = new CounterCreationData(
                                                                        "NumberOfReclaimedConnections",
                                                                        "The number of connections we reclaim from GC'd external connections",
                                                                        PerformanceCounterType.NumberOfItems32); 
        };
 
        sealed internal class Counter { 
            private PerformanceCounter _instance;
 
            internal Counter (string categoryName, string instanceName, string counterName, PerformanceCounterType counterType) {
                if (ADP.IsPlatformNT5) {
                    try {
                        if (!ADP.IsEmpty(categoryName) && !ADP.IsEmpty(instanceName)) { 
                            PerformanceCounter instance = new PerformanceCounter();
                            instance.CategoryName = categoryName; 
                            instance.CounterName = counterName; 
                            instance.InstanceName = instanceName;
                            instance.InstanceLifetime = PerformanceCounterInstanceLifetime.Process; 
                            instance.ReadOnly = false;
                            instance.RawValue = 0;  // make sure we start out at zero
                            _instance = instance;
                        } 
                    }
                    catch (InvalidOperationException e) { 
                        ADP.TraceExceptionWithoutRethrow(e); 
                        //
                    } 
                }
            }

 
            internal void Decrement() {
                PerformanceCounter instance = _instance; 
                if (null != instance) { 
                    instance.Decrement();
                } 
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal void Dispose () { // 
                PerformanceCounter instance = _instance;
                _instance = null; 
                if (null != instance) { 
                    instance.RemoveInstance();
                    // should we be calling instance.Close? 
                    // if we do will it exacerbate the Dispose vs. Decrement race condition
                    //instance.Close();
                }
            } 

            internal void Increment() { 
                PerformanceCounter instance = _instance; 
                if (null != instance) {
                    instance.Increment(); 
                }
            }
        };
 
        internal readonly Counter HardConnectsPerSecond;
        internal readonly Counter HardDisconnectsPerSecond; 
        internal readonly Counter SoftConnectsPerSecond; 
        internal readonly Counter SoftDisconnectsPerSecond;
        internal readonly Counter NumberOfNonPooledConnections; 
        internal readonly Counter NumberOfPooledConnections;
        internal readonly Counter NumberOfActiveConnectionPoolGroups;
        internal readonly Counter NumberOfInactiveConnectionPoolGroups;
        internal readonly Counter NumberOfActiveConnectionPools; 
        internal readonly Counter NumberOfInactiveConnectionPools;
        internal readonly Counter NumberOfActiveConnections; 
        internal readonly Counter NumberOfFreeConnections; 
        internal readonly Counter NumberOfStasisConnections;
        internal readonly Counter NumberOfReclaimedConnections; 

        protected DbConnectionPoolCounters() : this(null, null) {
        }
 
        protected DbConnectionPoolCounters(string categoryName, string categoryHelp) {
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(this.UnloadEventHandler); 
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(this.ExitEventHandler); 
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(this.ExceptionEventHandler);
 
            string instanceName = null;

            if (!ADP.IsEmpty(categoryName)) {
                if (ADP.IsPlatformNT5) { 
#if CREATEONLY
                    if (!PerformanceCounterCategory.Exists(categoryName)) { 
                        CreateCounters(categoryName, categoryHelp); 
                    }
#endif //CREATEONLY 
                    instanceName = GetInstanceName();
                }
            }
 
            // level 0-3: hard connects/disconnects, plus basic pool/pool entry statistics
            string basicCategoryName = categoryName; 
            HardConnectsPerSecond = new Counter(basicCategoryName, instanceName, CreationData.HardConnectsPerSecond.CounterName, CreationData.HardConnectsPerSecond.CounterType); 
            HardDisconnectsPerSecond = new Counter(basicCategoryName, instanceName, CreationData.HardDisconnectsPerSecond.CounterName, CreationData.HardDisconnectsPerSecond.CounterType);
            NumberOfNonPooledConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfNonPooledConnections.CounterName, CreationData.NumberOfNonPooledConnections.CounterType); 
            NumberOfPooledConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfPooledConnections.CounterName, CreationData.NumberOfPooledConnections.CounterType);
            NumberOfActiveConnectionPoolGroups = new Counter(basicCategoryName, instanceName, CreationData.NumberOfActiveConnectionPoolGroups.CounterName, CreationData.NumberOfActiveConnectionPoolGroups.CounterType);
            NumberOfInactiveConnectionPoolGroups = new Counter(basicCategoryName, instanceName, CreationData.NumberOfInactiveConnectionPoolGroups.CounterName, CreationData.NumberOfInactiveConnectionPoolGroups.CounterType);
            NumberOfActiveConnectionPools = new Counter(basicCategoryName, instanceName, CreationData.NumberOfActiveConnectionPools.CounterName, CreationData.NumberOfActiveConnectionPools.CounterType); 
            NumberOfInactiveConnectionPools = new Counter(basicCategoryName, instanceName, CreationData.NumberOfInactiveConnectionPools.CounterName, CreationData.NumberOfInactiveConnectionPools.CounterType);
            NumberOfStasisConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfStasisConnections.CounterName, CreationData.NumberOfStasisConnections.CounterType); 
            NumberOfReclaimedConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfReclaimedConnections.CounterName, CreationData.NumberOfReclaimedConnections.CounterType); 

            // level 4: expensive stuff 
            string verboseCategoryName = null;
            if (!ADP.IsEmpty(categoryName)) {
                // don't load TraceSwitch if no categoryName so that Odbc/OleDb have a chance of not loading TraceSwitch
                // which are also used by System.Diagnostics.PerformanceCounter.ctor & System.Transactions.get_Current 
                TraceSwitch perfCtrSwitch = new TraceSwitch("ConnectionPoolPerformanceCounterDetail", "level of detail to track with connection pool performance counters");
                if (TraceLevel.Verbose == perfCtrSwitch.Level) { 
                    verboseCategoryName = categoryName; 
                }
            } 
            SoftConnectsPerSecond = new Counter(verboseCategoryName, instanceName, CreationData.SoftConnectsPerSecond.CounterName, CreationData.SoftConnectsPerSecond.CounterType);
            SoftDisconnectsPerSecond = new Counter(verboseCategoryName, instanceName, CreationData.SoftDisconnectsPerSecond.CounterName, CreationData.SoftDisconnectsPerSecond.CounterType);
            NumberOfActiveConnections = new Counter(verboseCategoryName, instanceName, CreationData.NumberOfActiveConnections.CounterName, CreationData.NumberOfActiveConnections.CounterType);
            NumberOfFreeConnections = new Counter(verboseCategoryName, instanceName, CreationData.NumberOfFreeConnections.CounterName, CreationData.NumberOfFreeConnections.CounterType); 
        }
 
        [FileIOPermission(SecurityAction.Assert, Unrestricted=true)] 
        private string GetAssemblyName() {
            string result = null; 

            // First try GetEntryAssembly name, then AppDomain.FriendlyName.
            Assembly assembly = Assembly.GetEntryAssembly();
 
            if (null != assembly) {
                AssemblyName name = assembly.GetName(); 
                if (name != null) { 
                    result = name.Name; // MDAC 73469
                } 
            }
            return result;
        }
 
        private string GetInstanceName() {
            string result = null; 
 
            string instanceName = GetAssemblyName(); // instance perfcounter name
 
            if (ADP.IsEmpty(instanceName)) {
                AppDomain appDomain = AppDomain.CurrentDomain;
                if (null != appDomain) {
                    instanceName = appDomain.FriendlyName; 
                }
            } 
 
            int pid = SafeNativeMethods.GetCurrentProcessId();
 

            //

 

            result = String.Format((IFormatProvider)null, "{0}[{1}]", instanceName, pid); 
            result = result.Replace('(','[').Replace(')',']').Replace('#','_').Replace('/','_').Replace('\\','_'); 

            return result; 
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void Dispose() { 
            // ExceptionEventHandler with IsTerminiating may be called before
            // the Connection Close is called or the variables are initialized 
            SafeDispose(HardConnectsPerSecond); 
            SafeDispose(HardDisconnectsPerSecond);
            SafeDispose(SoftConnectsPerSecond); 
            SafeDispose(SoftDisconnectsPerSecond);
            SafeDispose(NumberOfNonPooledConnections);
            SafeDispose(NumberOfPooledConnections);
            SafeDispose(NumberOfActiveConnectionPoolGroups); 
            SafeDispose(NumberOfInactiveConnectionPoolGroups);
            SafeDispose(NumberOfActiveConnectionPools); 
            SafeDispose(NumberOfActiveConnections); 
            SafeDispose(NumberOfFreeConnections);
            SafeDispose(NumberOfStasisConnections); 
            SafeDispose(NumberOfReclaimedConnections);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        private void SafeDispose(Counter counter) { // WebData 103603
            if (null != counter) { 
                counter.Dispose(); 
            }
        } 

        [PrePrepareMethod]
        void ExceptionEventHandler (object sender, UnhandledExceptionEventArgs e) {
            if ((null != e) && e.IsTerminating) { 
                Dispose();
            } 
        } 

        [PrePrepareMethod] 
        void ExitEventHandler (object sender, EventArgs e) {
            Dispose();
        }
 
        [PrePrepareMethod]
        void UnloadEventHandler (object sender, EventArgs e) { 
            Dispose(); 
        }
 
#if CREATEONLY
        private void CreateCounters(string categoryName, string categoryHelp) {
            int i = 0;
 
            CounterCreationData[]  myCounterCreationData = new CounterCreationData[CreationData.NumberOfCounters];
 
            myCounterCreationData[i++] = CreationData.HardConnectsPerSecond; 
            myCounterCreationData[i++] = CreationData.HardDisconnectsPerSecond;
            myCounterCreationData[i++] = CreationData.SoftConnectsPerSecond; 
            myCounterCreationData[i++] = CreationData.SoftDisconnectsPerSecond;
            myCounterCreationData[i++] = CreationData.NumberOfNonPooledConnections;
            myCounterCreationData[i++] = CreationData.NumberOfPooledConnections;
            myCounterCreationData[i++] = CreationData.NumberOfActiveConnectionPoolGroups; 
            myCounterCreationData[i++] = CreationData.NumberOfInactiveConnectionPoolGroups;
            myCounterCreationData[i++] = CreationData.NumberOfActiveConnectionPools; 
            myCounterCreationData[i++] = CreationData.NumberOfInactiveConnectionPools; 
            myCounterCreationData[i++] = CreationData.NumberOfActiveConnections;
            myCounterCreationData[i++] = CreationData.NumberOfFreeConnections; 
            myCounterCreationData[i++] = CreationData.NumberOfStasisConnections;
            myCounterCreationData[i++] = CreationData.NumberOfReclaimedConnections;

            CounterCreationDataCollection myCounterCollection = new CounterCreationDataCollection(myCounterCreationData); 
            CreateCountersSafe(categoryName, categoryHelp, myCounterCollection);
 
            for(i = 0; i < myCounterCollection.Count; i++) { 
                myCounterCollection.Remove(myCounterCreationData[i]);
            } 
        }

        private void CreateCountersSafe(string categoryName, string categoryHelp, CounterCreationDataCollection myCounterCollection) {
            (new PerformanceCounterPermission(PerformanceCounterPermissionAccess.Instrument, ".", categoryName)).Assert(); 
            try {
                PerformanceCounterCategory.Create(categoryName, categoryHelp, myCounterCollection); 
            } 
            finally {
                PerformanceCounterPermission.RevertAssert(); 
            }
        }
#endif //CREATEONLY
    } 

    sealed internal class DbConnectionPoolCountersNoCounters : DbConnectionPoolCounters { 
 
        public static readonly DbConnectionPoolCountersNoCounters SingletonInstance = new DbConnectionPoolCountersNoCounters();
 
        private DbConnectionPoolCountersNoCounters() : base () {
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
