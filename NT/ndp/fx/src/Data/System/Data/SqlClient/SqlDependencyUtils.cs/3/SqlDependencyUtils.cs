//------------------------------------------------------------------------------ 
// <copyright file="SqlDependencyUtils.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="true">[....]</owner>
// <owner current="false" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.SqlClient { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Common; 
    using System.Diagnostics;
    using System.Security.Principal; 
    using System.Security.AccessControl; 
    using System.Text;
    using System.Threading; 

    // This is a singleton instance per AppDomain that acts as the notification dispatcher for
    // that AppDomain.  It receives calls from the SqlDependencyProcessDispatcher with an ID or a server name
    // to invalidate matching dependencies in the given AppDomain. 

    internal class SqlDependencyPerAppDomainDispatcher : MarshalByRefObject { // MBR, since ref'ed by ProcessDispatcher. 
 
        // ----------------
        // Instance members 
        // ----------------

        internal static readonly SqlDependencyPerAppDomainDispatcher
                 SingletonInstance = new SqlDependencyPerAppDomainDispatcher(); // singleton object 

        // Dependency ID -> Dependency hashtable.  1 -> 1 mapping. 
        // 1) Used for ASP.Net to map from ID to dependency. 
        // 2) Used to enumerate dependencies to invalidate based on server.
        private Dictionary<string, SqlDependency>       _dependencyIdToDependencyHash; 

        // CommandText + Parameter Values hash value -> (Guid as string) command identifier.  1->1 mapping.
        // appdomain identifier + command identifier -> Dependencies hashtable.  1 -> N mapping.  More than one dependency
        // can be using the same command text and parameter values - resulting in a hash to the same value. 
        //
        // We use this to cache mapping between command to dependencies such that we may reduce the notification 
        // resource effect on SQL Server.  The Guid identifier is sent to the server during notification enlistment, 
        // and returned during the notification event.  Dependencies look up existing Guids, if one exists, to ensure
        // they are re-using notification ids. 
        private Dictionary<string, List<SqlDependency>> _notificationIdToDependenciesHash;
        private Dictionary<string, string> _commandToCommandId;

        // TIMEOUT LOGIC DESCRIPTION 
        //
        // Every time we add a dependency we compute the next, earlier timeout. 
        // 
        // We setup a timer to get a callback every 15 seconds. In the call back:
        // - If there are no active dependencies, we just return. 
        // - If there are dependencies but none of them timed-out (compared to the "next timeout"),
        //   we just return.
        // - Otherwise we Invalidate() those that timed-out.
        // 
        // So the client-generated timeouts have a granularity of 15 seconds. This allows
        // for a simple and low-resource-consumption implementation. 
        // 
        // LOCKS: don't update _nextTimeout outside of the _dependencyHash.SyncRoot lock.
 
 		private bool     _SqlDependencyTimeOutTimerStarted = false;
        // Next timeout for any of the dependencies in the dependency table.
        private DateTime _nextTimeout;
        // Timer to periodically check the dependencies in the table and see if anyone needs 
        // a timeout.  We'll enable this only on demand.
        private Timer    _timeoutTimer; 
 
        // -----------
        // BID members 
        // -----------

        private readonly int _objectID        = System.Threading.Interlocked.Increment(ref _objectTypeCount);
        private static   int _objectTypeCount; // Bid counter 
        internal int ObjectID {
            get { 
                return _objectID; 
            }
        } 

        private SqlDependencyPerAppDomainDispatcher() {
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher|DEP> %d#", ObjectID); 
            try {
                _dependencyIdToDependencyHash = new Dictionary<string, SqlDependency>(); 
                _notificationIdToDependenciesHash    = new Dictionary<string, List<SqlDependency>>(); 
                _commandToCommandId           = new Dictionary<string, string>();
 
                _timeoutTimer = new Timer(new TimerCallback(TimeoutTimerCallback), null, Timeout.Infinite, Timeout.Infinite);

                // If rude abort - we'll leak.  This is acceptable for now.
                AppDomain.CurrentDomain.DomainUnload += new EventHandler(this.UnloadEventHandler); 
            }
            finally { 
                Bid.ScopeLeave(ref hscp); 
            }
        } 

        // SQL Hotfix 236
        //  When remoted across appdomains, MarshalByRefObject links by default time out if there is no activity
        //  within a few minutes.  Add this override to prevent marshaled links from timing out. 
        public override object InitializeLifetimeService() {
            return null; 
        } 

        // ------ 
        // Events
        // ------

        private void UnloadEventHandler(object sender, EventArgs e) { 
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.UnloadEventHandler|DEP> %d#", ObjectID); 
            try { 
                // Make non-blocking call to ProcessDispatcher to ThreadPool.QueueUserWorkItem to complete
                // stopping of all start calls in this AppDomain.  For containers shared among various AppDomains, 
                // this will just be a ref-count subtract.  For non-shared containers, we will close the container
                // and clean-up.
                SqlDependencyProcessDispatcher dispatcher = SqlDependency.ProcessDispatcher;
                if (null != dispatcher) { 
                    dispatcher.QueueAppDomainUnloading(SqlDependency.AppDomainKey);
                } 
            } 
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        }

        // --------------------------------------------------- 
        // Methods for dependency hash manipulation and firing.
        // --------------------------------------------------- 
 
        // This method is called upon SqlDependency constructor.
        internal void AddDependencyEntry(SqlDependency dep) { 
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.AddDependencyEntry|DEP> %d#, SqlDependency: %d#", ObjectID, dep.ObjectID);
            try {
                lock (this) { 
                    _dependencyIdToDependencyHash.Add(dep.Id, dep);
                } 
            } 
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        }

        // This method is called upon Execute of a command associated with a SqlDependency object. 
        internal string AddCommandEntry(string commandHash, SqlDependency dep) {
            IntPtr hscp; 
            string idString = String.Empty; 
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> %d#, commandHash: '%ls', SqlDependency: %d#", ObjectID, commandHash, dep.ObjectID);
            try { 
                lock (this) {
                    if (!_dependencyIdToDependencyHash.ContainsKey(dep.Id)) { // Determine if depId->dep hashtable contains dependency.  If not, it's been invalidated.
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> Dependency not present in depId->dep hash, must have been invalidated.\n");
                    } 
                    else {
                        List<SqlDependency> dependencyList = null; 
 
                        string commandId;
                        if (!_commandToCommandId.TryGetValue(commandHash, out commandId)) { 
                            Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> No command id, creating.\n");
                            commandId = Guid.NewGuid().ToString();
                            _commandToCommandId[commandHash] = commandId;
                        } 

                        // get combined key for hash 
                        StringBuilder builder = new StringBuilder(); 
                        builder.Append(SqlDependency.AppDomainKey);
                        builder.Append(";"); 
                        builder.Append(commandId);
                        idString = builder.ToString();

                        if (!_notificationIdToDependenciesHash.TryGetValue(idString, out dependencyList)) { 
                            Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> Creating new ArrayList for commandHash.\n");
                            dependencyList = new List<SqlDependency>(); 
                            _notificationIdToDependenciesHash.Add(idString, dependencyList); 
                        }
 
                        if (!dependencyList.Contains(dep)) {
                            Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> Dependency not present for commandHash, adding.\n");
                            dependencyList.Add(dep);
                        } 
                        else {
                            Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> Dependency already present for commandHash.\n"); 
                        } 
                    }
                } 
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            } 

            return idString; 
        } 

        // This method is called by the ProcessDispatcher upon a notification for this AppDomain. 
        internal void InvalidateCommandID(SqlNotification sqlNotification) {
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.InvalidateCommandID|DEP> %d#, commandHash: '%ls'", ObjectID, sqlNotification.Key);
            try { 
                List<SqlDependency> dependencyList = null;
 
                lock (this) { 
                    dependencyList = LookupCommandEntryWithRemove(sqlNotification.Key);
 
                    if (null != dependencyList) {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.InvalidateCommandID|DEP> commandHash found in hashtable.\n");

                        foreach (SqlDependency dependency in dependencyList) { 
                            // Ensure we remove from process static app domain hash for dependency initiated invalidates.
                            LookupDependencyEntryWithRemove(dependency.Id); 
 
                            // Completely remove Dependency from commandToDependenciesHash.
                            RemoveDependencyFromCommandToDependenciesHash(dependency); 
                        }
                    }
                    else {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.InvalidateCommandID|DEP> commandHash NOT found in hashtable.\n"); 
                    }
                } 
 
                if (null != dependencyList) {
                    // After removal from hashtables, invalidate. 
                    foreach (SqlDependency dependency in dependencyList) {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.InvalidateCommandID|DEP> Dependency found in commandHash dependency ArrayList - calling invalidate.\n");

                        try { 
                            dependency.Invalidate(sqlNotification.Type, sqlNotification.Info, sqlNotification.Source);
                        } 
                        catch (Exception e) { 
                            // Since we are looping over dependencies, do not allow one Invalidate
                            // that results in a throw prevent us from invalidating all dependencies 
                            // related to this server.
                            if (!ADP.IsCatchableExceptionType(e)) {
                                throw;
                            } 
                            ADP.TraceExceptionWithoutRethrow(e);
                        } 
                    } 
                }
            } 
            finally {
                Bid.ScopeLeave(ref hscp);
            }
        } 

        // This method is called when a connection goes down or other unknown error occurs in the ProcessDispatcher. 
        internal void InvalidateServer(string server, SqlNotification sqlNotification) { 
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.Invalidate|DEP> %d#, server: '%ls'", ObjectID, server); 
            try {
                List<SqlDependency> dependencies = new List<SqlDependency>();

                lock (this) { // Copy inside of lock, but invalidate outside of lock. 
                    foreach (KeyValuePair<string, SqlDependency> entry in _dependencyIdToDependencyHash) {
                        SqlDependency dependency = entry.Value; 
                        if (dependency.ServerList.Contains(server)) { 
                            dependencies.Add(dependency);
                        } 
                    }

                    foreach (SqlDependency dependency in dependencies) { // Iterate over resulting list removing from our hashes.
                        // Ensure we remove from process static app domain hash for dependency initiated invalidates. 
                        LookupDependencyEntryWithRemove(dependency.Id);
 
                        // Completely remove Dependency from commandToDependenciesHash. 
                        RemoveDependencyFromCommandToDependenciesHash(dependency);
                    } 
                }

                foreach (SqlDependency dependency in dependencies) { // Iterate and invalidate.
                    try { 
                        dependency.Invalidate(sqlNotification.Type, sqlNotification.Info, sqlNotification.Source);
                    } 
                    catch (Exception e) { 
                        // Since we are looping over dependencies, do not allow one Invalidate
                        // that results in a throw prevent us from invalidating all dependencies 
                        // related to this server.
                        if (!ADP.IsCatchableExceptionType(e)) {
                            throw;
                        } 
                        ADP.TraceExceptionWithoutRethrow(e);
                    } 
                } 
            }
            finally { 
                Bid.ScopeLeave(ref hscp);
            }
        }
 
        // This method is called by SqlCommand to enable ASP.Net scenarios - map from ID to Dependency.
        internal SqlDependency LookupDependencyEntry(string id) { 
            IntPtr hscp; 
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntry|DEP> %d#, Key: '%ls'", ObjectID, id);
            try { 
                if (null == id) {
                    throw ADP.ArgumentNull("id");
                }
                if (ADP.IsEmpty(id)) { 
                    throw SQL.SqlDependencyIdMismatch();
                } 
 
                SqlDependency entry = null;
 
                lock (this) {
                    if (_dependencyIdToDependencyHash.ContainsKey(id)) {
                        entry = _dependencyIdToDependencyHash[id];
                    } 
                    else {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntry|DEP|ERR> ERROR - dependency ID mismatch - not throwing.\n"); 
                    } 
                }
 
                return entry;
            }
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
        // Remove the dependency from the hashtable with the passed id.
        private void LookupDependencyEntryWithRemove(string id) { 
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntryWithRemove|DEP> %d#, id: '%ls'", ObjectID, id);
            try {
                lock (this) { 
                    if (_dependencyIdToDependencyHash.ContainsKey(id)) {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntryWithRemove|DEP> Entry found in hashtable - removing.\n"); 
                        _dependencyIdToDependencyHash.Remove(id); 

                        // if there are no more dependencies then we can dispose the timer. 
                        if (0 == _dependencyIdToDependencyHash.Count) {
                            _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                            _SqlDependencyTimeOutTimerStarted = false;
                        } 
                    }
                    else { 
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntryWithRemove|DEP> Entry NOT found in hashtable.\n"); 
                    }
                } 
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            } 
        }
 
        // Find and return arraylist, and remove passed hash value. 
        private List<SqlDependency> LookupCommandEntryWithRemove(string commandHash) {
            IntPtr hscp; 
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.LookupCommandEntryWithRemove|DEP> %d#, commandHash: '%ls'", ObjectID, commandHash);
            try {
                List<SqlDependency> entry = null;
 
                lock (this) {
                    if (_notificationIdToDependenciesHash.ContainsKey(commandHash)) { 
                        entry = _notificationIdToDependenciesHash[commandHash]; 
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntriesWithRemove|DEP> Entries found in hashtable - removing.\n");
                        _notificationIdToDependenciesHash.Remove(commandHash); 
                    }
                    else {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntriesWithRemove|DEP> Entries NOT found in hashtable.\n");
                    } 
                }
 
                return entry; 
            }
            finally { 
                Bid.ScopeLeave(ref hscp);
            }
        }
 
        // Remove from commandToDependenciesHash all references to the passed dependency.
        private void RemoveDependencyFromCommandToDependenciesHash(SqlDependency dependency) { 
            IntPtr hscp; 
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.RemoveDependencyFromCommandToDependenciesHash|DEP> %d#, SqlDependency: %d#", ObjectID, dependency.ObjectID);
            try { 
                lock (this) {
                    foreach (KeyValuePair<string, List<SqlDependency>> entry in _notificationIdToDependenciesHash) {
                        List<SqlDependency> dependencies = entry.Value;
                        if (dependencies.Contains(dependency)) { 
                            Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.RemoveDependencyFromCommandToDependenciesHash|DEP> Removing SqlDependency: %d#, with ID: '%ls'.\n", dependency.ObjectID, dependency.Id);
                            dependencies.Remove(dependency); 
                        } 
                    }
                } 
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            } 
        }
 
        // ----------------------------------------- 
        // Methods for Timer maintenance and firing.
        // ----------------------------------------- 

        internal void StartTimer(SqlDependency dep) {
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.StartTimer|DEP> %d#, SqlDependency: %d#", ObjectID, dep.ObjectID); 
            try {
                // If this dependency expires sooner than the current next timeout, change 
                // the timeout and enable timer callback as needed.  Note that we change _nextTimeout 
                // only inside the hashtable syncroot.
                lock (this) { 
                    // Enable the timer if needed (disable when empty, enable on the first addition).
                    if (!_SqlDependencyTimeOutTimerStarted) {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.StartTimer|DEP> Timer not yet started, starting.\n");
 
                        _timeoutTimer.Change(15000 /* 15 secs */, 15000 /* 15 secs */);
 
                        // Save this as the earlier timeout to come. 
                        _nextTimeout = dep.ExpirationTime;
                        _SqlDependencyTimeOutTimerStarted = true; 
                    }
                    else if(_nextTimeout > dep.ExpirationTime) {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.StartTimer|DEP> Timer already started, resetting time.\n");
 
                        // Save this as the earlier timeout to come.
                        _nextTimeout = dep.ExpirationTime; 
                    } 
                }
            } 
            finally {
                Bid.ScopeLeave(ref hscp);
            }
        } 

        private static void TimeoutTimerCallback(object state) { 
            IntPtr hscp; 
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.TimeoutTimerCallback|DEP> AppDomainKey: '%ls'", SqlDependency.AppDomainKey);
            try { 
                SqlDependency[] dependencies;

                // Only take the lock for checking whether there is work to do
                // if we do have work, we'll copy the hashtable and scan it after releasing 
                // the lock.
                lock (SingletonInstance) { 
                    if (0 == SingletonInstance._dependencyIdToDependencyHash.Count) { 
                        // Nothing to check.
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.TimeoutTimerCallback|DEP> No dependencies, exiting.\n"); 
                        return;
                    }
                    if (SingletonInstance._nextTimeout > DateTime.UtcNow)  {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.TimeoutTimerCallback|DEP> No timeouts expired, exiting.\n"); 
                        // No dependency timed-out yet.
                        return; 
                    } 

                    // If at least one dependency timed-out do a scan of the table. 
                    // NOTE: we could keep a shadow table sorted by expiration time, but
                    // given the number of typical simultaneously alive dependencies it's
                    // probably not worth the optimization.
                    dependencies = new SqlDependency[SingletonInstance._dependencyIdToDependencyHash.Count]; 
                    SingletonInstance._dependencyIdToDependencyHash.Values.CopyTo(dependencies, 0);
                } 
 
                // Scan the active dependencies if needed.
                DateTime now            = DateTime.UtcNow; 
                DateTime newNextTimeout = DateTime.MaxValue;

                for (int i=0; i < dependencies.Length; i++) {
                    // If expired fire the change notification. 
                    if(dependencies[i].ExpirationTime <= now) {
                        try { 
                            // This invokes user-code which may throw exceptions. 
                            // NOTE: this is intentionally outside of the lock, we don't want
                            // to invoke user-code while holding an internal lock. 
                            dependencies[i].Invalidate(SqlNotificationType.Change, SqlNotificationInfo.Error, SqlNotificationSource.Timeout);
                        }
                        catch(Exception e) {
                            if (!ADP.IsCatchableExceptionType(e)) { 
                                throw;
                            } 
 
                            // This is an exception in user code, and we're in a thread-pool thread
                            // without user's code up in the stack, no much we can do other than 
                            // eating the exception.
                            ADP.TraceExceptionWithoutRethrow(e);
                        }
                    } 
                    else {
                        if (dependencies[i].ExpirationTime < newNextTimeout) { 
                            newNextTimeout = dependencies[i].ExpirationTime; // Track the next earlier timeout. 
                        }
                        dependencies[i] = null; // Null means "don't remove it from the hashtable" in the loop below. 
                    }
                }

                // Remove timed-out dependencies from the hashtable. 
                lock (SingletonInstance) {
                    for (int i=0; i < dependencies.Length; i++) { 
                        if (null != dependencies[i]) { 
                            SingletonInstance._dependencyIdToDependencyHash.Remove(dependencies[i].Id);
                        } 
                    }
                    if (newNextTimeout < SingletonInstance._nextTimeout) {
                        SingletonInstance._nextTimeout = newNextTimeout; // We're inside the lock so ok to update.
                    } 
                }
            } 
            finally { 
                Bid.ScopeLeave(ref hscp);
            } 
        }
    }

    // Simple class used to encapsulate all data in a notification. 
    internal class SqlNotification : MarshalByRefObject {
        // This class could be Serializable rather than MBR... 
 
        private readonly SqlNotificationInfo   _info;
        private readonly SqlNotificationSource _source; 
        private readonly SqlNotificationType   _type;
        private readonly string                _key;

        internal SqlNotification(SqlNotificationInfo info, SqlNotificationSource source, SqlNotificationType type, string key) { 
            _info    = info;
            _source  = source; 
            _type    = type; 
            _key     = key;
        } 

        internal SqlNotificationInfo Info {
            get {
                return _info; 
            }
        } 
 
        internal string Key {
            get { 
                return _key;
            }
        }
 
        internal SqlNotificationSource Source {
            get { 
                return _source; 
            }
        } 

        internal SqlNotificationType Type {
            get {
                return _type; 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDependencyUtils.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="true">[....]</owner>
// <owner current="false" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.SqlClient { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Common; 
    using System.Diagnostics;
    using System.Security.Principal; 
    using System.Security.AccessControl; 
    using System.Text;
    using System.Threading; 

    // This is a singleton instance per AppDomain that acts as the notification dispatcher for
    // that AppDomain.  It receives calls from the SqlDependencyProcessDispatcher with an ID or a server name
    // to invalidate matching dependencies in the given AppDomain. 

    internal class SqlDependencyPerAppDomainDispatcher : MarshalByRefObject { // MBR, since ref'ed by ProcessDispatcher. 
 
        // ----------------
        // Instance members 
        // ----------------

        internal static readonly SqlDependencyPerAppDomainDispatcher
                 SingletonInstance = new SqlDependencyPerAppDomainDispatcher(); // singleton object 

        // Dependency ID -> Dependency hashtable.  1 -> 1 mapping. 
        // 1) Used for ASP.Net to map from ID to dependency. 
        // 2) Used to enumerate dependencies to invalidate based on server.
        private Dictionary<string, SqlDependency>       _dependencyIdToDependencyHash; 

        // CommandText + Parameter Values hash value -> (Guid as string) command identifier.  1->1 mapping.
        // appdomain identifier + command identifier -> Dependencies hashtable.  1 -> N mapping.  More than one dependency
        // can be using the same command text and parameter values - resulting in a hash to the same value. 
        //
        // We use this to cache mapping between command to dependencies such that we may reduce the notification 
        // resource effect on SQL Server.  The Guid identifier is sent to the server during notification enlistment, 
        // and returned during the notification event.  Dependencies look up existing Guids, if one exists, to ensure
        // they are re-using notification ids. 
        private Dictionary<string, List<SqlDependency>> _notificationIdToDependenciesHash;
        private Dictionary<string, string> _commandToCommandId;

        // TIMEOUT LOGIC DESCRIPTION 
        //
        // Every time we add a dependency we compute the next, earlier timeout. 
        // 
        // We setup a timer to get a callback every 15 seconds. In the call back:
        // - If there are no active dependencies, we just return. 
        // - If there are dependencies but none of them timed-out (compared to the "next timeout"),
        //   we just return.
        // - Otherwise we Invalidate() those that timed-out.
        // 
        // So the client-generated timeouts have a granularity of 15 seconds. This allows
        // for a simple and low-resource-consumption implementation. 
        // 
        // LOCKS: don't update _nextTimeout outside of the _dependencyHash.SyncRoot lock.
 
 		private bool     _SqlDependencyTimeOutTimerStarted = false;
        // Next timeout for any of the dependencies in the dependency table.
        private DateTime _nextTimeout;
        // Timer to periodically check the dependencies in the table and see if anyone needs 
        // a timeout.  We'll enable this only on demand.
        private Timer    _timeoutTimer; 
 
        // -----------
        // BID members 
        // -----------

        private readonly int _objectID        = System.Threading.Interlocked.Increment(ref _objectTypeCount);
        private static   int _objectTypeCount; // Bid counter 
        internal int ObjectID {
            get { 
                return _objectID; 
            }
        } 

        private SqlDependencyPerAppDomainDispatcher() {
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher|DEP> %d#", ObjectID); 
            try {
                _dependencyIdToDependencyHash = new Dictionary<string, SqlDependency>(); 
                _notificationIdToDependenciesHash    = new Dictionary<string, List<SqlDependency>>(); 
                _commandToCommandId           = new Dictionary<string, string>();
 
                _timeoutTimer = new Timer(new TimerCallback(TimeoutTimerCallback), null, Timeout.Infinite, Timeout.Infinite);

                // If rude abort - we'll leak.  This is acceptable for now.
                AppDomain.CurrentDomain.DomainUnload += new EventHandler(this.UnloadEventHandler); 
            }
            finally { 
                Bid.ScopeLeave(ref hscp); 
            }
        } 

        // SQL Hotfix 236
        //  When remoted across appdomains, MarshalByRefObject links by default time out if there is no activity
        //  within a few minutes.  Add this override to prevent marshaled links from timing out. 
        public override object InitializeLifetimeService() {
            return null; 
        } 

        // ------ 
        // Events
        // ------

        private void UnloadEventHandler(object sender, EventArgs e) { 
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.UnloadEventHandler|DEP> %d#", ObjectID); 
            try { 
                // Make non-blocking call to ProcessDispatcher to ThreadPool.QueueUserWorkItem to complete
                // stopping of all start calls in this AppDomain.  For containers shared among various AppDomains, 
                // this will just be a ref-count subtract.  For non-shared containers, we will close the container
                // and clean-up.
                SqlDependencyProcessDispatcher dispatcher = SqlDependency.ProcessDispatcher;
                if (null != dispatcher) { 
                    dispatcher.QueueAppDomainUnloading(SqlDependency.AppDomainKey);
                } 
            } 
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        }

        // --------------------------------------------------- 
        // Methods for dependency hash manipulation and firing.
        // --------------------------------------------------- 
 
        // This method is called upon SqlDependency constructor.
        internal void AddDependencyEntry(SqlDependency dep) { 
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.AddDependencyEntry|DEP> %d#, SqlDependency: %d#", ObjectID, dep.ObjectID);
            try {
                lock (this) { 
                    _dependencyIdToDependencyHash.Add(dep.Id, dep);
                } 
            } 
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        }

        // This method is called upon Execute of a command associated with a SqlDependency object. 
        internal string AddCommandEntry(string commandHash, SqlDependency dep) {
            IntPtr hscp; 
            string idString = String.Empty; 
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> %d#, commandHash: '%ls', SqlDependency: %d#", ObjectID, commandHash, dep.ObjectID);
            try { 
                lock (this) {
                    if (!_dependencyIdToDependencyHash.ContainsKey(dep.Id)) { // Determine if depId->dep hashtable contains dependency.  If not, it's been invalidated.
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> Dependency not present in depId->dep hash, must have been invalidated.\n");
                    } 
                    else {
                        List<SqlDependency> dependencyList = null; 
 
                        string commandId;
                        if (!_commandToCommandId.TryGetValue(commandHash, out commandId)) { 
                            Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> No command id, creating.\n");
                            commandId = Guid.NewGuid().ToString();
                            _commandToCommandId[commandHash] = commandId;
                        } 

                        // get combined key for hash 
                        StringBuilder builder = new StringBuilder(); 
                        builder.Append(SqlDependency.AppDomainKey);
                        builder.Append(";"); 
                        builder.Append(commandId);
                        idString = builder.ToString();

                        if (!_notificationIdToDependenciesHash.TryGetValue(idString, out dependencyList)) { 
                            Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> Creating new ArrayList for commandHash.\n");
                            dependencyList = new List<SqlDependency>(); 
                            _notificationIdToDependenciesHash.Add(idString, dependencyList); 
                        }
 
                        if (!dependencyList.Contains(dep)) {
                            Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> Dependency not present for commandHash, adding.\n");
                            dependencyList.Add(dep);
                        } 
                        else {
                            Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.AddCommandEntry|DEP> Dependency already present for commandHash.\n"); 
                        } 
                    }
                } 
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            } 

            return idString; 
        } 

        // This method is called by the ProcessDispatcher upon a notification for this AppDomain. 
        internal void InvalidateCommandID(SqlNotification sqlNotification) {
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.InvalidateCommandID|DEP> %d#, commandHash: '%ls'", ObjectID, sqlNotification.Key);
            try { 
                List<SqlDependency> dependencyList = null;
 
                lock (this) { 
                    dependencyList = LookupCommandEntryWithRemove(sqlNotification.Key);
 
                    if (null != dependencyList) {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.InvalidateCommandID|DEP> commandHash found in hashtable.\n");

                        foreach (SqlDependency dependency in dependencyList) { 
                            // Ensure we remove from process static app domain hash for dependency initiated invalidates.
                            LookupDependencyEntryWithRemove(dependency.Id); 
 
                            // Completely remove Dependency from commandToDependenciesHash.
                            RemoveDependencyFromCommandToDependenciesHash(dependency); 
                        }
                    }
                    else {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.InvalidateCommandID|DEP> commandHash NOT found in hashtable.\n"); 
                    }
                } 
 
                if (null != dependencyList) {
                    // After removal from hashtables, invalidate. 
                    foreach (SqlDependency dependency in dependencyList) {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.InvalidateCommandID|DEP> Dependency found in commandHash dependency ArrayList - calling invalidate.\n");

                        try { 
                            dependency.Invalidate(sqlNotification.Type, sqlNotification.Info, sqlNotification.Source);
                        } 
                        catch (Exception e) { 
                            // Since we are looping over dependencies, do not allow one Invalidate
                            // that results in a throw prevent us from invalidating all dependencies 
                            // related to this server.
                            if (!ADP.IsCatchableExceptionType(e)) {
                                throw;
                            } 
                            ADP.TraceExceptionWithoutRethrow(e);
                        } 
                    } 
                }
            } 
            finally {
                Bid.ScopeLeave(ref hscp);
            }
        } 

        // This method is called when a connection goes down or other unknown error occurs in the ProcessDispatcher. 
        internal void InvalidateServer(string server, SqlNotification sqlNotification) { 
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.Invalidate|DEP> %d#, server: '%ls'", ObjectID, server); 
            try {
                List<SqlDependency> dependencies = new List<SqlDependency>();

                lock (this) { // Copy inside of lock, but invalidate outside of lock. 
                    foreach (KeyValuePair<string, SqlDependency> entry in _dependencyIdToDependencyHash) {
                        SqlDependency dependency = entry.Value; 
                        if (dependency.ServerList.Contains(server)) { 
                            dependencies.Add(dependency);
                        } 
                    }

                    foreach (SqlDependency dependency in dependencies) { // Iterate over resulting list removing from our hashes.
                        // Ensure we remove from process static app domain hash for dependency initiated invalidates. 
                        LookupDependencyEntryWithRemove(dependency.Id);
 
                        // Completely remove Dependency from commandToDependenciesHash. 
                        RemoveDependencyFromCommandToDependenciesHash(dependency);
                    } 
                }

                foreach (SqlDependency dependency in dependencies) { // Iterate and invalidate.
                    try { 
                        dependency.Invalidate(sqlNotification.Type, sqlNotification.Info, sqlNotification.Source);
                    } 
                    catch (Exception e) { 
                        // Since we are looping over dependencies, do not allow one Invalidate
                        // that results in a throw prevent us from invalidating all dependencies 
                        // related to this server.
                        if (!ADP.IsCatchableExceptionType(e)) {
                            throw;
                        } 
                        ADP.TraceExceptionWithoutRethrow(e);
                    } 
                } 
            }
            finally { 
                Bid.ScopeLeave(ref hscp);
            }
        }
 
        // This method is called by SqlCommand to enable ASP.Net scenarios - map from ID to Dependency.
        internal SqlDependency LookupDependencyEntry(string id) { 
            IntPtr hscp; 
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntry|DEP> %d#, Key: '%ls'", ObjectID, id);
            try { 
                if (null == id) {
                    throw ADP.ArgumentNull("id");
                }
                if (ADP.IsEmpty(id)) { 
                    throw SQL.SqlDependencyIdMismatch();
                } 
 
                SqlDependency entry = null;
 
                lock (this) {
                    if (_dependencyIdToDependencyHash.ContainsKey(id)) {
                        entry = _dependencyIdToDependencyHash[id];
                    } 
                    else {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntry|DEP|ERR> ERROR - dependency ID mismatch - not throwing.\n"); 
                    } 
                }
 
                return entry;
            }
            finally {
                Bid.ScopeLeave(ref hscp); 
            }
        } 
 
        // Remove the dependency from the hashtable with the passed id.
        private void LookupDependencyEntryWithRemove(string id) { 
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntryWithRemove|DEP> %d#, id: '%ls'", ObjectID, id);
            try {
                lock (this) { 
                    if (_dependencyIdToDependencyHash.ContainsKey(id)) {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntryWithRemove|DEP> Entry found in hashtable - removing.\n"); 
                        _dependencyIdToDependencyHash.Remove(id); 

                        // if there are no more dependencies then we can dispose the timer. 
                        if (0 == _dependencyIdToDependencyHash.Count) {
                            _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                            _SqlDependencyTimeOutTimerStarted = false;
                        } 
                    }
                    else { 
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntryWithRemove|DEP> Entry NOT found in hashtable.\n"); 
                    }
                } 
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            } 
        }
 
        // Find and return arraylist, and remove passed hash value. 
        private List<SqlDependency> LookupCommandEntryWithRemove(string commandHash) {
            IntPtr hscp; 
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.LookupCommandEntryWithRemove|DEP> %d#, commandHash: '%ls'", ObjectID, commandHash);
            try {
                List<SqlDependency> entry = null;
 
                lock (this) {
                    if (_notificationIdToDependenciesHash.ContainsKey(commandHash)) { 
                        entry = _notificationIdToDependenciesHash[commandHash]; 
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntriesWithRemove|DEP> Entries found in hashtable - removing.\n");
                        _notificationIdToDependenciesHash.Remove(commandHash); 
                    }
                    else {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.LookupDependencyEntriesWithRemove|DEP> Entries NOT found in hashtable.\n");
                    } 
                }
 
                return entry; 
            }
            finally { 
                Bid.ScopeLeave(ref hscp);
            }
        }
 
        // Remove from commandToDependenciesHash all references to the passed dependency.
        private void RemoveDependencyFromCommandToDependenciesHash(SqlDependency dependency) { 
            IntPtr hscp; 
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.RemoveDependencyFromCommandToDependenciesHash|DEP> %d#, SqlDependency: %d#", ObjectID, dependency.ObjectID);
            try { 
                lock (this) {
                    foreach (KeyValuePair<string, List<SqlDependency>> entry in _notificationIdToDependenciesHash) {
                        List<SqlDependency> dependencies = entry.Value;
                        if (dependencies.Contains(dependency)) { 
                            Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.RemoveDependencyFromCommandToDependenciesHash|DEP> Removing SqlDependency: %d#, with ID: '%ls'.\n", dependency.ObjectID, dependency.Id);
                            dependencies.Remove(dependency); 
                        } 
                    }
                } 
            }
            finally {
                Bid.ScopeLeave(ref hscp);
            } 
        }
 
        // ----------------------------------------- 
        // Methods for Timer maintenance and firing.
        // ----------------------------------------- 

        internal void StartTimer(SqlDependency dep) {
            IntPtr hscp;
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.StartTimer|DEP> %d#, SqlDependency: %d#", ObjectID, dep.ObjectID); 
            try {
                // If this dependency expires sooner than the current next timeout, change 
                // the timeout and enable timer callback as needed.  Note that we change _nextTimeout 
                // only inside the hashtable syncroot.
                lock (this) { 
                    // Enable the timer if needed (disable when empty, enable on the first addition).
                    if (!_SqlDependencyTimeOutTimerStarted) {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.StartTimer|DEP> Timer not yet started, starting.\n");
 
                        _timeoutTimer.Change(15000 /* 15 secs */, 15000 /* 15 secs */);
 
                        // Save this as the earlier timeout to come. 
                        _nextTimeout = dep.ExpirationTime;
                        _SqlDependencyTimeOutTimerStarted = true; 
                    }
                    else if(_nextTimeout > dep.ExpirationTime) {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.StartTimer|DEP> Timer already started, resetting time.\n");
 
                        // Save this as the earlier timeout to come.
                        _nextTimeout = dep.ExpirationTime; 
                    } 
                }
            } 
            finally {
                Bid.ScopeLeave(ref hscp);
            }
        } 

        private static void TimeoutTimerCallback(object state) { 
            IntPtr hscp; 
            Bid.NotificationsScopeEnter(out hscp, "<sc.SqlDependencyPerAppDomainDispatcher.TimeoutTimerCallback|DEP> AppDomainKey: '%ls'", SqlDependency.AppDomainKey);
            try { 
                SqlDependency[] dependencies;

                // Only take the lock for checking whether there is work to do
                // if we do have work, we'll copy the hashtable and scan it after releasing 
                // the lock.
                lock (SingletonInstance) { 
                    if (0 == SingletonInstance._dependencyIdToDependencyHash.Count) { 
                        // Nothing to check.
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.TimeoutTimerCallback|DEP> No dependencies, exiting.\n"); 
                        return;
                    }
                    if (SingletonInstance._nextTimeout > DateTime.UtcNow)  {
                        Bid.NotificationsTrace("<sc.SqlDependencyPerAppDomainDispatcher.TimeoutTimerCallback|DEP> No timeouts expired, exiting.\n"); 
                        // No dependency timed-out yet.
                        return; 
                    } 

                    // If at least one dependency timed-out do a scan of the table. 
                    // NOTE: we could keep a shadow table sorted by expiration time, but
                    // given the number of typical simultaneously alive dependencies it's
                    // probably not worth the optimization.
                    dependencies = new SqlDependency[SingletonInstance._dependencyIdToDependencyHash.Count]; 
                    SingletonInstance._dependencyIdToDependencyHash.Values.CopyTo(dependencies, 0);
                } 
 
                // Scan the active dependencies if needed.
                DateTime now            = DateTime.UtcNow; 
                DateTime newNextTimeout = DateTime.MaxValue;

                for (int i=0; i < dependencies.Length; i++) {
                    // If expired fire the change notification. 
                    if(dependencies[i].ExpirationTime <= now) {
                        try { 
                            // This invokes user-code which may throw exceptions. 
                            // NOTE: this is intentionally outside of the lock, we don't want
                            // to invoke user-code while holding an internal lock. 
                            dependencies[i].Invalidate(SqlNotificationType.Change, SqlNotificationInfo.Error, SqlNotificationSource.Timeout);
                        }
                        catch(Exception e) {
                            if (!ADP.IsCatchableExceptionType(e)) { 
                                throw;
                            } 
 
                            // This is an exception in user code, and we're in a thread-pool thread
                            // without user's code up in the stack, no much we can do other than 
                            // eating the exception.
                            ADP.TraceExceptionWithoutRethrow(e);
                        }
                    } 
                    else {
                        if (dependencies[i].ExpirationTime < newNextTimeout) { 
                            newNextTimeout = dependencies[i].ExpirationTime; // Track the next earlier timeout. 
                        }
                        dependencies[i] = null; // Null means "don't remove it from the hashtable" in the loop below. 
                    }
                }

                // Remove timed-out dependencies from the hashtable. 
                lock (SingletonInstance) {
                    for (int i=0; i < dependencies.Length; i++) { 
                        if (null != dependencies[i]) { 
                            SingletonInstance._dependencyIdToDependencyHash.Remove(dependencies[i].Id);
                        } 
                    }
                    if (newNextTimeout < SingletonInstance._nextTimeout) {
                        SingletonInstance._nextTimeout = newNextTimeout; // We're inside the lock so ok to update.
                    } 
                }
            } 
            finally { 
                Bid.ScopeLeave(ref hscp);
            } 
        }
    }

    // Simple class used to encapsulate all data in a notification. 
    internal class SqlNotification : MarshalByRefObject {
        // This class could be Serializable rather than MBR... 
 
        private readonly SqlNotificationInfo   _info;
        private readonly SqlNotificationSource _source; 
        private readonly SqlNotificationType   _type;
        private readonly string                _key;

        internal SqlNotification(SqlNotificationInfo info, SqlNotificationSource source, SqlNotificationType type, string key) { 
            _info    = info;
            _source  = source; 
            _type    = type; 
            _key     = key;
        } 

        internal SqlNotificationInfo Info {
            get {
                return _info; 
            }
        } 
 
        internal string Key {
            get { 
                return _key;
            }
        }
 
        internal SqlNotificationSource Source {
            get { 
                return _source; 
            }
        } 

        internal SqlNotificationType Type {
            get {
                return _type; 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
