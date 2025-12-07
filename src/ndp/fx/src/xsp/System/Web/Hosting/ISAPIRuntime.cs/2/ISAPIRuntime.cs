//------------------------------------------------------------------------------ 
// <copyright file="ISAPIRuntime.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * The ASP.NET runtime services 
 *
 * Copyright (c) 1998 Microsoft Corporation 
 */

namespace System.Web.Hosting {
    using System.Runtime.InteropServices; 
    using System.Collections;
    using System.Reflection; 
    using System.Threading; 
    using System.Web;
    using System.Web.Management; 
    using System.Web.Util;
    using System.Globalization;
    using System.Security.Permissions;
 

    /// <devdoc> 
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    /// <internalonly/> 
    // NOTE: There is no link demand here because it causes a 7% perf regression.
    // there is a Demand on the .ctor for ISAPIRuntime, which provides the same level of
    // security, except that we only take a one time perf hit when an instance is created.
    [ComImport, Guid("08a2c56f-7c16-41c1-a8be-432917a1a2d1"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)] 
    public interface IISAPIRuntime {
 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 

        void StartProcessing();

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
 

        void StopProcessing(); 


        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        [return: MarshalAs(UnmanagedType.I4)] 
        int ProcessRequest( 
                          [In]
                          IntPtr ecb, 
                          [In, MarshalAs(UnmanagedType.I4)]
                          int useProcessModel);

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
 

        void DoGCCollect(); 
    }


    /// <devdoc> 
    ///    <para>[To be supplied.]</para>
    /// </devdoc> 
    /// <internalonly/> 
    // NOTE: There is no link demand here because it causes a 7% perf regression.
    // there is a Demand on the .ctor for ISAPIRuntime, which provides the same level of 
    // security, except that we only take a one time perf hit when an instance is created.
    public sealed class ISAPIRuntime : MarshalByRefObject, IISAPIRuntime, IRegisteredObject {

        // WARNING: do not modify without making corresponding changes in appdomains.h 
        private const int WORKER_REQUEST_TYPE_IN_PROC            = 0x0;
        private const int WORKER_REQUEST_TYPE_OOP                = 0x1; 
        private const int WORKER_REQUEST_TYPE_IN_PROC_VERSION_2  = 0x2; 

        // to control removal from unmanaged table (to it only once) 
        private static int _isThisAppDomainRemovedFromUnmanagedTable;

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
        [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Minimal)] 
        public ISAPIRuntime() {
            HostingEnvironment.RegisterObject(this); 
        } 

 
        public override Object InitializeLifetimeService() {
            return null; // never expire lease
        }
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public void StartProcessing() { 
            Debug.Trace("ISAPIRuntime", "StartProcessing");
        }

 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public void StopProcessing() {
            Debug.Trace("ISAPIRuntime", "StopProcessing"); 
            HostingEnvironment.UnregisterObject(this);
        }

        /* 
         * Process one ISAPI request
         * 
         * @param ecb ECB 
         * @param useProcessModel flag set to true when out-of-process
         */ 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public int ProcessRequest(IntPtr ecb, int iWRType) {
            IntPtr pHttpCompletion = IntPtr.Zero; 
            if (iWRType == WORKER_REQUEST_TYPE_IN_PROC_VERSION_2) { 
                pHttpCompletion = ecb;
                ecb = UnsafeNativeMethods.GetEcb(pHttpCompletion); 
            }
            ISAPIWorkerRequest wr = null;
            try {
                bool useOOP = (iWRType == WORKER_REQUEST_TYPE_OOP); 
                wr = ISAPIWorkerRequest.CreateWorkerRequest(ecb, useOOP);
                wr.Initialize(); 
 
                // check if app path matches (need to restart app domain?)
                String wrPath = wr.GetAppPathTranslated(); 
                String adPath = HttpRuntime.AppDomainAppPathInternal;

                if (adPath == null ||
                    StringUtil.EqualsIgnoreCase(wrPath, adPath)) { 

                    HttpRuntime.ProcessRequestNoDemand(wr); 
                    return 0; 
                }
                else { 
                    // need to restart app domain
                    HttpRuntime.ShutdownAppDomain(ApplicationShutdownReason.PhysicalApplicationPathChanged,
                                                  SR.GetString(SR.Hosting_Phys_Path_Changed,
                                                                                   adPath, 
                                                                                   wrPath));
                    return 1; 
                } 
            }
            catch(Exception e) { 
                try {
                    WebBaseEvent.RaiseRuntimeError(e, this);
                } catch {}
 
                // Have we called HSE_REQ_DONE_WITH_SESSION?  If so, don't re-throw.
                if (wr != null && wr.Ecb == IntPtr.Zero) { 
                    if (pHttpCompletion != IntPtr.Zero) { 
                        UnsafeNativeMethods.SetDoneWithSessionCalled(pHttpCompletion);
                    } 
                    // if this is a thread abort exception, cancel the abort
                    if (e is ThreadAbortException) {
                        Thread.ResetAbort();
                    } 
                    // IMPORTANT: if this thread is being aborted because of an AppDomain.Unload,
                    // the CLR will still throw an AppDomainUnloadedException. The native caller 
                    // must special case COR_E_APPDOMAINUNLOADED(0x80131014) and not 
                    // call HSE_REQ_DONE_WITH_SESSION more than once.
                    return 0; 
                }

                // re-throw if we have not called HSE_REQ_DONE_WITH_SESSION
                throw; 
            }
        } 
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void DoGCCollect() {
            for (int c = 10; c > 0; c--) { 
                System.GC.Collect();
            } 
        } 

 
        /// <internalonly/>
        void IRegisteredObject.Stop(bool immediate) {
            RemoveThisAppDomainFromUnmanagedTable();
            HostingEnvironment.UnregisterObject(this); 
        }
 
        private static String s_thisAppDomainsIsapiAppId; 

        internal void SetThisAppDomainsIsapiAppId(String appId) { 
            Debug.Trace("ISAPIRuntime", "SetThisAppDomainsIsapiAppId appId=" + appId);
            s_thisAppDomainsIsapiAppId = appId;
        }
 
        internal static void RemoveThisAppDomainFromUnmanagedTable() {
            if (Interlocked.Exchange(ref _isThisAppDomainRemovedFromUnmanagedTable, 1) != 0) { 
                return; 
            }
 
            try {
                if (s_thisAppDomainsIsapiAppId != null ) {
                    Debug.Trace("ISAPIRuntime", "Calling UnsafeNativeMethods.AppDomainRestart appId=" +
                        s_thisAppDomainsIsapiAppId + " (AppDomainAppId=" + HttpRuntime.AppDomainAppIdInternal + ")"); 

                    UnsafeNativeMethods.AppDomainRestart(s_thisAppDomainsIsapiAppId); 
                } 

                HttpRuntime.AddAppDomainTraceMessage(SR.GetString(SR.App_Domain_Restart)); 
            }
            catch {
            }
        } 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="ISAPIRuntime.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * The ASP.NET runtime services 
 *
 * Copyright (c) 1998 Microsoft Corporation 
 */

namespace System.Web.Hosting {
    using System.Runtime.InteropServices; 
    using System.Collections;
    using System.Reflection; 
    using System.Threading; 
    using System.Web;
    using System.Web.Management; 
    using System.Web.Util;
    using System.Globalization;
    using System.Security.Permissions;
 

    /// <devdoc> 
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    /// <internalonly/> 
    // NOTE: There is no link demand here because it causes a 7% perf regression.
    // there is a Demand on the .ctor for ISAPIRuntime, which provides the same level of
    // security, except that we only take a one time perf hit when an instance is created.
    [ComImport, Guid("08a2c56f-7c16-41c1-a8be-432917a1a2d1"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)] 
    public interface IISAPIRuntime {
 
        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 

        void StartProcessing();

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
 

        void StopProcessing(); 


        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        [return: MarshalAs(UnmanagedType.I4)] 
        int ProcessRequest( 
                          [In]
                          IntPtr ecb, 
                          [In, MarshalAs(UnmanagedType.I4)]
                          int useProcessModel);

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
 

        void DoGCCollect(); 
    }


    /// <devdoc> 
    ///    <para>[To be supplied.]</para>
    /// </devdoc> 
    /// <internalonly/> 
    // NOTE: There is no link demand here because it causes a 7% perf regression.
    // there is a Demand on the .ctor for ISAPIRuntime, which provides the same level of 
    // security, except that we only take a one time perf hit when an instance is created.
    public sealed class ISAPIRuntime : MarshalByRefObject, IISAPIRuntime, IRegisteredObject {

        // WARNING: do not modify without making corresponding changes in appdomains.h 
        private const int WORKER_REQUEST_TYPE_IN_PROC            = 0x0;
        private const int WORKER_REQUEST_TYPE_OOP                = 0x1; 
        private const int WORKER_REQUEST_TYPE_IN_PROC_VERSION_2  = 0x2; 

        // to control removal from unmanaged table (to it only once) 
        private static int _isThisAppDomainRemovedFromUnmanagedTable;

        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
        [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Minimal)] 
        public ISAPIRuntime() {
            HostingEnvironment.RegisterObject(this); 
        } 

 
        public override Object InitializeLifetimeService() {
            return null; // never expire lease
        }
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public void StartProcessing() { 
            Debug.Trace("ISAPIRuntime", "StartProcessing");
        }

 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc> 
        public void StopProcessing() {
            Debug.Trace("ISAPIRuntime", "StopProcessing"); 
            HostingEnvironment.UnregisterObject(this);
        }

        /* 
         * Process one ISAPI request
         * 
         * @param ecb ECB 
         * @param useProcessModel flag set to true when out-of-process
         */ 

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public int ProcessRequest(IntPtr ecb, int iWRType) {
            IntPtr pHttpCompletion = IntPtr.Zero; 
            if (iWRType == WORKER_REQUEST_TYPE_IN_PROC_VERSION_2) { 
                pHttpCompletion = ecb;
                ecb = UnsafeNativeMethods.GetEcb(pHttpCompletion); 
            }
            ISAPIWorkerRequest wr = null;
            try {
                bool useOOP = (iWRType == WORKER_REQUEST_TYPE_OOP); 
                wr = ISAPIWorkerRequest.CreateWorkerRequest(ecb, useOOP);
                wr.Initialize(); 
 
                // check if app path matches (need to restart app domain?)
                String wrPath = wr.GetAppPathTranslated(); 
                String adPath = HttpRuntime.AppDomainAppPathInternal;

                if (adPath == null ||
                    StringUtil.EqualsIgnoreCase(wrPath, adPath)) { 

                    HttpRuntime.ProcessRequestNoDemand(wr); 
                    return 0; 
                }
                else { 
                    // need to restart app domain
                    HttpRuntime.ShutdownAppDomain(ApplicationShutdownReason.PhysicalApplicationPathChanged,
                                                  SR.GetString(SR.Hosting_Phys_Path_Changed,
                                                                                   adPath, 
                                                                                   wrPath));
                    return 1; 
                } 
            }
            catch(Exception e) { 
                try {
                    WebBaseEvent.RaiseRuntimeError(e, this);
                } catch {}
 
                // Have we called HSE_REQ_DONE_WITH_SESSION?  If so, don't re-throw.
                if (wr != null && wr.Ecb == IntPtr.Zero) { 
                    if (pHttpCompletion != IntPtr.Zero) { 
                        UnsafeNativeMethods.SetDoneWithSessionCalled(pHttpCompletion);
                    } 
                    // if this is a thread abort exception, cancel the abort
                    if (e is ThreadAbortException) {
                        Thread.ResetAbort();
                    } 
                    // IMPORTANT: if this thread is being aborted because of an AppDomain.Unload,
                    // the CLR will still throw an AppDomainUnloadedException. The native caller 
                    // must special case COR_E_APPDOMAINUNLOADED(0x80131014) and not 
                    // call HSE_REQ_DONE_WITH_SESSION more than once.
                    return 0; 
                }

                // re-throw if we have not called HSE_REQ_DONE_WITH_SESSION
                throw; 
            }
        } 
 

        /// <devdoc> 
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void DoGCCollect() {
            for (int c = 10; c > 0; c--) { 
                System.GC.Collect();
            } 
        } 

 
        /// <internalonly/>
        void IRegisteredObject.Stop(bool immediate) {
            RemoveThisAppDomainFromUnmanagedTable();
            HostingEnvironment.UnregisterObject(this); 
        }
 
        private static String s_thisAppDomainsIsapiAppId; 

        internal void SetThisAppDomainsIsapiAppId(String appId) { 
            Debug.Trace("ISAPIRuntime", "SetThisAppDomainsIsapiAppId appId=" + appId);
            s_thisAppDomainsIsapiAppId = appId;
        }
 
        internal static void RemoveThisAppDomainFromUnmanagedTable() {
            if (Interlocked.Exchange(ref _isThisAppDomainRemovedFromUnmanagedTable, 1) != 0) { 
                return; 
            }
 
            try {
                if (s_thisAppDomainsIsapiAppId != null ) {
                    Debug.Trace("ISAPIRuntime", "Calling UnsafeNativeMethods.AppDomainRestart appId=" +
                        s_thisAppDomainsIsapiAppId + " (AppDomainAppId=" + HttpRuntime.AppDomainAppIdInternal + ")"); 

                    UnsafeNativeMethods.AppDomainRestart(s_thisAppDomainsIsapiAppId); 
                } 

                HttpRuntime.AddAppDomainTraceMessage(SR.GetString(SR.App_Domain_Restart)); 
            }
            catch {
            }
        } 
    }
} 
