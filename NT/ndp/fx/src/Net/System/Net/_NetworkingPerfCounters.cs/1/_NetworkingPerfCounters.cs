//------------------------------------------------------------------------------ 
// <copyright file="_NetworkingPerfCounters.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System; 
    using System.Reflection;
    using System.Net.Sockets; 
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.ComponentModel;
    using System.Globalization; 
    using System.Text;
 
    // 
    // This implementation uses the PerformanceCounter object defined in
    // System.Diagnostics, too bad it doesn't work 'cause the runtime defines 
    // the counter and doesn't expose the API
    //
    internal static class NetworkingPerfCounters {
 
        private static PerformanceCounter
            ConnectionsEstablished, 
            BytesReceived, 
            BytesSent,
            DatagramsReceived, 
            DatagramsSent;

        private static PerformanceCounter
            globalConnectionsEstablished, 
            globalBytesReceived,
            globalBytesSent, 
            globalDatagramsReceived, 
            globalDatagramsSent;
 
        private const string CategoryName = ".NET CLR Networking";
        private const string ConnectionsEstablishedName = "Connections Established";
        private const string BytesReceivedName = "Bytes Received";
        private const string BytesSentName = "Bytes Sent"; 
        private const string DatagramsReceivedName = "Datagrams Received";
        private const string DatagramsSentName = "Datagrams Sent"; 
        private const string GlobalInstanceName = "_Global_"; 

        private static object syncObject = new object(); 
        private static bool initialized = false;

        internal static void Initialize() {
 
            if(!initialized){
                lock(syncObject){ 
                    if(!initialized){ 
                        //
                        // on Win9x you have no PerfCounters 
                        // on NT4 PerfCounters are not writable
                        //
                        if (ComNetOS.IsWin2K) {
                            // 
                            // this is an internal class, we need to update performance counters
                            // on behalf of the user to log perf data on network activity 
                            // 
                            // <
 
                            PerformanceCounterPermission perfCounterPermission = new PerformanceCounterPermission(PermissionState.Unrestricted);
                            perfCounterPermission.Assert();
                            try {
                                // 
                                // create the counters, this will check for the right permissions (false)
                                // means the counter is not readonly (it's read/write) and cache them while 
                                // we're under the Assert(), which will be reverted in the finally below. 
                                //
 
                                // Changed due to a recent Whidbey DCR to fix spinlock issues. To get the new behavior, we have to use the
                                // default constructor and set the properties inidividually
                                // #498527
                                string instanceName = GetInstanceName(); 

                                ConnectionsEstablished = new PerformanceCounter(); 
                                ConnectionsEstablished.CategoryName = CategoryName; 
                                ConnectionsEstablished.CounterName = ConnectionsEstablishedName;
                                ConnectionsEstablished.InstanceName = instanceName; 
                                ConnectionsEstablished.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                                ConnectionsEstablished.ReadOnly = false;
                                ConnectionsEstablished.RawValue = 0;
 
                                BytesReceived = new PerformanceCounter();
                                BytesReceived.CategoryName = CategoryName; 
                                BytesReceived.CounterName = BytesReceivedName; 
                                BytesReceived.InstanceName = instanceName;
                                BytesReceived.InstanceLifetime = PerformanceCounterInstanceLifetime.Process; 
                                BytesReceived.ReadOnly = false;
                                BytesReceived.RawValue = 0;

                                BytesSent = new PerformanceCounter(); 
                                BytesSent.CategoryName = CategoryName;
                                BytesSent.CounterName = BytesSentName; 
                                BytesSent.InstanceName = instanceName; 
                                BytesSent.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                                BytesSent.ReadOnly = false; 
                                BytesSent.RawValue = 0;

                                DatagramsReceived = new PerformanceCounter();
                                DatagramsReceived.CategoryName = CategoryName; 
                                DatagramsReceived.CounterName = DatagramsReceivedName;
                                DatagramsReceived.InstanceName = instanceName; 
                                DatagramsReceived.InstanceLifetime = PerformanceCounterInstanceLifetime.Process; 
                                DatagramsReceived.ReadOnly = false;
                                DatagramsReceived.RawValue = 0; 

                                DatagramsSent = new PerformanceCounter();
                                DatagramsSent.CategoryName = CategoryName;
                                DatagramsSent.CounterName = DatagramsSentName; 
                                DatagramsSent.InstanceName = instanceName;
                                DatagramsSent.InstanceLifetime = PerformanceCounterInstanceLifetime.Process; 
                                DatagramsSent.ReadOnly = false; 
                                DatagramsSent.RawValue = 0;
 

                                //
                                // Get a handle onto the global ones too.  Do not reset to zero.
                                // 
                                globalConnectionsEstablished = new PerformanceCounter(CategoryName, ConnectionsEstablishedName, GlobalInstanceName, false);
                                globalBytesReceived = new PerformanceCounter(CategoryName, BytesReceivedName, GlobalInstanceName, false); 
                                globalBytesSent = new PerformanceCounter(CategoryName, BytesSentName, GlobalInstanceName, false); 
                                globalDatagramsReceived = new PerformanceCounter(CategoryName, DatagramsReceivedName, GlobalInstanceName, false);
                                globalDatagramsSent = new PerformanceCounter(CategoryName, DatagramsSentName, GlobalInstanceName, false); 

                                AppDomain.CurrentDomain.DomainUnload += new EventHandler(ExitOrUnloadEventHandler);
                                AppDomain.CurrentDomain.ProcessExit += new EventHandler(ExitOrUnloadEventHandler);
                                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionEventHandler); 
                            }
                            catch (Win32Exception exception) { 
                                GlobalLog.Print("NetworkingPerfCounters::NetworkingPerfCounters() caught exception:" + exception); 
                            }
                            catch (InvalidOperationException exception) { 
                                GlobalLog.Print("NetworkingPerfCounters::NetworkingPerfCounters() caught exception:" + exception);
                            }
                            finally {
                                PerformanceCounterPermission.RevertAssert(); 
                            }
                        } 
                        initialized = true; 
                    }
                } 
            }
        }

        private static void ExceptionEventHandler (object sender, UnhandledExceptionEventArgs e) { 
            if (e.IsTerminating) {
                Cleanup(); 
            } 
        }
 
        private static void ExitOrUnloadEventHandler(object sender, EventArgs e) {
            Cleanup();
        }
 
        private static void Cleanup() {
            PerformanceCounter performanceCounter; 
            performanceCounter = ConnectionsEstablished; 
            if (performanceCounter!=null) {
                performanceCounter.RemoveInstance(); 
            }
            performanceCounter = BytesReceived;
            if (performanceCounter!=null) {
                performanceCounter.RemoveInstance(); 
            }
            performanceCounter = BytesSent; 
            if (performanceCounter!=null) { 
                performanceCounter.RemoveInstance();
            } 
            performanceCounter = DatagramsReceived;
            if (performanceCounter!=null) {
                performanceCounter.RemoveInstance();
            } 
            performanceCounter = DatagramsSent;
            if (performanceCounter!=null) { 
                performanceCounter.RemoveInstance(); 
            }
 
            // No need to clean up the global ones.
        }

 
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        private static string GetAssemblyName() { 
            string result = null; 
            Assembly assembly = Assembly.GetEntryAssembly();
            if (assembly!=null) { 
                AssemblyName name = assembly.GetName();
                if (name!=null) {
                    result = name.Name;
                } 
            }
            return result; 
        } 

        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)] 
        private static string GetInstanceName() {
            string result = null;
            string instanceName = GetAssemblyName();
            if (instanceName==null || instanceName.Length==0) { 
                instanceName = AppDomain.CurrentDomain.FriendlyName;
            } 
            StringBuilder instanceBuilder = new StringBuilder(instanceName); 
            for (int i = 0; i < instanceBuilder.Length; i++)
            { 
                switch (instanceBuilder[i])
                {
                    case '(':
                        instanceBuilder[i] = '['; 
                        break;
                    case ')': 
                        instanceBuilder[i] = ']'; 
                        break;
                    case '/': 
                    case '\\':
                    case '#':
                        instanceBuilder[i] = '_';
                        break; 
                }
            } 
            result = string.Format(CultureInfo.CurrentCulture, "{0}[{1}]", instanceBuilder.ToString(), Process.GetCurrentProcess().Id); 
            return result;
        } 

        internal static void IncrementConnectionsEstablished() {
            if (ConnectionsEstablished != null) {
                ConnectionsEstablished.Increment(); 
            }
            if (globalConnectionsEstablished != null) 
            { 
                globalConnectionsEstablished.Increment();
                GlobalLog.Print("NetworkingPerfCounters::IncrementConnectionsEstablished()"); 
            }
        }

        internal static void AddBytesReceived(int increment) { 
            if (BytesReceived != null) {
                BytesReceived.IncrementBy(increment); 
            } 
            if (globalBytesReceived != null)
            { 
                globalBytesReceived.IncrementBy(increment);
                GlobalLog.Print("NetworkingPerfCounters::AddBytesReceived(" + increment.ToString() + ")");
            }
        } 

        internal static void AddBytesSent(int increment) { 
            if (BytesSent != null) { 
                BytesSent.IncrementBy(increment);
            } 
            if (globalBytesSent != null)
            {
                globalBytesSent.IncrementBy(increment);
                GlobalLog.Print("NetworkingPerfCounters::AddBytesSent(" + increment.ToString() + ")"); 
            }
        } 
 
        internal static void IncrementDatagramsReceived() {
            if (DatagramsReceived != null) { 
                DatagramsReceived.Increment();
            }
            if (globalDatagramsReceived != null)
            { 
                globalDatagramsReceived.Increment();
                GlobalLog.Print("NetworkingPerfCounters::IncrementDatagramsReceived()"); 
            } 
        }
 
        internal static void IncrementDatagramsSent() {
            if (DatagramsSent != null) {
                DatagramsSent.Increment();
            } 
            if (globalDatagramsSent != null)
            { 
                globalDatagramsSent.Increment(); 
                GlobalLog.Print("NetworkingPerfCounters::IncrementDatagramsSent()");
            } 
        }
    }
}
 
//------------------------------------------------------------------------------ 
// <copyright file="_NetworkingPerfCounters.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System; 
    using System.Reflection;
    using System.Net.Sockets; 
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.ComponentModel;
    using System.Globalization; 
    using System.Text;
 
    // 
    // This implementation uses the PerformanceCounter object defined in
    // System.Diagnostics, too bad it doesn't work 'cause the runtime defines 
    // the counter and doesn't expose the API
    //
    internal static class NetworkingPerfCounters {
 
        private static PerformanceCounter
            ConnectionsEstablished, 
            BytesReceived, 
            BytesSent,
            DatagramsReceived, 
            DatagramsSent;

        private static PerformanceCounter
            globalConnectionsEstablished, 
            globalBytesReceived,
            globalBytesSent, 
            globalDatagramsReceived, 
            globalDatagramsSent;
 
        private const string CategoryName = ".NET CLR Networking";
        private const string ConnectionsEstablishedName = "Connections Established";
        private const string BytesReceivedName = "Bytes Received";
        private const string BytesSentName = "Bytes Sent"; 
        private const string DatagramsReceivedName = "Datagrams Received";
        private const string DatagramsSentName = "Datagrams Sent"; 
        private const string GlobalInstanceName = "_Global_"; 

        private static object syncObject = new object(); 
        private static bool initialized = false;

        internal static void Initialize() {
 
            if(!initialized){
                lock(syncObject){ 
                    if(!initialized){ 
                        //
                        // on Win9x you have no PerfCounters 
                        // on NT4 PerfCounters are not writable
                        //
                        if (ComNetOS.IsWin2K) {
                            // 
                            // this is an internal class, we need to update performance counters
                            // on behalf of the user to log perf data on network activity 
                            // 
                            // <
 
                            PerformanceCounterPermission perfCounterPermission = new PerformanceCounterPermission(PermissionState.Unrestricted);
                            perfCounterPermission.Assert();
                            try {
                                // 
                                // create the counters, this will check for the right permissions (false)
                                // means the counter is not readonly (it's read/write) and cache them while 
                                // we're under the Assert(), which will be reverted in the finally below. 
                                //
 
                                // Changed due to a recent Whidbey DCR to fix spinlock issues. To get the new behavior, we have to use the
                                // default constructor and set the properties inidividually
                                // #498527
                                string instanceName = GetInstanceName(); 

                                ConnectionsEstablished = new PerformanceCounter(); 
                                ConnectionsEstablished.CategoryName = CategoryName; 
                                ConnectionsEstablished.CounterName = ConnectionsEstablishedName;
                                ConnectionsEstablished.InstanceName = instanceName; 
                                ConnectionsEstablished.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                                ConnectionsEstablished.ReadOnly = false;
                                ConnectionsEstablished.RawValue = 0;
 
                                BytesReceived = new PerformanceCounter();
                                BytesReceived.CategoryName = CategoryName; 
                                BytesReceived.CounterName = BytesReceivedName; 
                                BytesReceived.InstanceName = instanceName;
                                BytesReceived.InstanceLifetime = PerformanceCounterInstanceLifetime.Process; 
                                BytesReceived.ReadOnly = false;
                                BytesReceived.RawValue = 0;

                                BytesSent = new PerformanceCounter(); 
                                BytesSent.CategoryName = CategoryName;
                                BytesSent.CounterName = BytesSentName; 
                                BytesSent.InstanceName = instanceName; 
                                BytesSent.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                                BytesSent.ReadOnly = false; 
                                BytesSent.RawValue = 0;

                                DatagramsReceived = new PerformanceCounter();
                                DatagramsReceived.CategoryName = CategoryName; 
                                DatagramsReceived.CounterName = DatagramsReceivedName;
                                DatagramsReceived.InstanceName = instanceName; 
                                DatagramsReceived.InstanceLifetime = PerformanceCounterInstanceLifetime.Process; 
                                DatagramsReceived.ReadOnly = false;
                                DatagramsReceived.RawValue = 0; 

                                DatagramsSent = new PerformanceCounter();
                                DatagramsSent.CategoryName = CategoryName;
                                DatagramsSent.CounterName = DatagramsSentName; 
                                DatagramsSent.InstanceName = instanceName;
                                DatagramsSent.InstanceLifetime = PerformanceCounterInstanceLifetime.Process; 
                                DatagramsSent.ReadOnly = false; 
                                DatagramsSent.RawValue = 0;
 

                                //
                                // Get a handle onto the global ones too.  Do not reset to zero.
                                // 
                                globalConnectionsEstablished = new PerformanceCounter(CategoryName, ConnectionsEstablishedName, GlobalInstanceName, false);
                                globalBytesReceived = new PerformanceCounter(CategoryName, BytesReceivedName, GlobalInstanceName, false); 
                                globalBytesSent = new PerformanceCounter(CategoryName, BytesSentName, GlobalInstanceName, false); 
                                globalDatagramsReceived = new PerformanceCounter(CategoryName, DatagramsReceivedName, GlobalInstanceName, false);
                                globalDatagramsSent = new PerformanceCounter(CategoryName, DatagramsSentName, GlobalInstanceName, false); 

                                AppDomain.CurrentDomain.DomainUnload += new EventHandler(ExitOrUnloadEventHandler);
                                AppDomain.CurrentDomain.ProcessExit += new EventHandler(ExitOrUnloadEventHandler);
                                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionEventHandler); 
                            }
                            catch (Win32Exception exception) { 
                                GlobalLog.Print("NetworkingPerfCounters::NetworkingPerfCounters() caught exception:" + exception); 
                            }
                            catch (InvalidOperationException exception) { 
                                GlobalLog.Print("NetworkingPerfCounters::NetworkingPerfCounters() caught exception:" + exception);
                            }
                            finally {
                                PerformanceCounterPermission.RevertAssert(); 
                            }
                        } 
                        initialized = true; 
                    }
                } 
            }
        }

        private static void ExceptionEventHandler (object sender, UnhandledExceptionEventArgs e) { 
            if (e.IsTerminating) {
                Cleanup(); 
            } 
        }
 
        private static void ExitOrUnloadEventHandler(object sender, EventArgs e) {
            Cleanup();
        }
 
        private static void Cleanup() {
            PerformanceCounter performanceCounter; 
            performanceCounter = ConnectionsEstablished; 
            if (performanceCounter!=null) {
                performanceCounter.RemoveInstance(); 
            }
            performanceCounter = BytesReceived;
            if (performanceCounter!=null) {
                performanceCounter.RemoveInstance(); 
            }
            performanceCounter = BytesSent; 
            if (performanceCounter!=null) { 
                performanceCounter.RemoveInstance();
            } 
            performanceCounter = DatagramsReceived;
            if (performanceCounter!=null) {
                performanceCounter.RemoveInstance();
            } 
            performanceCounter = DatagramsSent;
            if (performanceCounter!=null) { 
                performanceCounter.RemoveInstance(); 
            }
 
            // No need to clean up the global ones.
        }

 
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        private static string GetAssemblyName() { 
            string result = null; 
            Assembly assembly = Assembly.GetEntryAssembly();
            if (assembly!=null) { 
                AssemblyName name = assembly.GetName();
                if (name!=null) {
                    result = name.Name;
                } 
            }
            return result; 
        } 

        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)] 
        private static string GetInstanceName() {
            string result = null;
            string instanceName = GetAssemblyName();
            if (instanceName==null || instanceName.Length==0) { 
                instanceName = AppDomain.CurrentDomain.FriendlyName;
            } 
            StringBuilder instanceBuilder = new StringBuilder(instanceName); 
            for (int i = 0; i < instanceBuilder.Length; i++)
            { 
                switch (instanceBuilder[i])
                {
                    case '(':
                        instanceBuilder[i] = '['; 
                        break;
                    case ')': 
                        instanceBuilder[i] = ']'; 
                        break;
                    case '/': 
                    case '\\':
                    case '#':
                        instanceBuilder[i] = '_';
                        break; 
                }
            } 
            result = string.Format(CultureInfo.CurrentCulture, "{0}[{1}]", instanceBuilder.ToString(), Process.GetCurrentProcess().Id); 
            return result;
        } 

        internal static void IncrementConnectionsEstablished() {
            if (ConnectionsEstablished != null) {
                ConnectionsEstablished.Increment(); 
            }
            if (globalConnectionsEstablished != null) 
            { 
                globalConnectionsEstablished.Increment();
                GlobalLog.Print("NetworkingPerfCounters::IncrementConnectionsEstablished()"); 
            }
        }

        internal static void AddBytesReceived(int increment) { 
            if (BytesReceived != null) {
                BytesReceived.IncrementBy(increment); 
            } 
            if (globalBytesReceived != null)
            { 
                globalBytesReceived.IncrementBy(increment);
                GlobalLog.Print("NetworkingPerfCounters::AddBytesReceived(" + increment.ToString() + ")");
            }
        } 

        internal static void AddBytesSent(int increment) { 
            if (BytesSent != null) { 
                BytesSent.IncrementBy(increment);
            } 
            if (globalBytesSent != null)
            {
                globalBytesSent.IncrementBy(increment);
                GlobalLog.Print("NetworkingPerfCounters::AddBytesSent(" + increment.ToString() + ")"); 
            }
        } 
 
        internal static void IncrementDatagramsReceived() {
            if (DatagramsReceived != null) { 
                DatagramsReceived.Increment();
            }
            if (globalDatagramsReceived != null)
            { 
                globalDatagramsReceived.Increment();
                GlobalLog.Print("NetworkingPerfCounters::IncrementDatagramsReceived()"); 
            } 
        }
 
        internal static void IncrementDatagramsSent() {
            if (DatagramsSent != null) {
                DatagramsSent.Increment();
            } 
            if (globalDatagramsSent != null)
            { 
                globalDatagramsSent.Increment(); 
                GlobalLog.Print("NetworkingPerfCounters::IncrementDatagramsSent()");
            } 
        }
    }
}
 
