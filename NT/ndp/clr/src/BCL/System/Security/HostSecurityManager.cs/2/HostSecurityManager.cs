// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// A HostSecurityManager gives a hosting application the chance to 
// participate in the security decisions in the AppDomain.
// 

namespace System.Security {
    using System.Collections;
    using System.Deployment.Internal.Isolation; 
    using System.Deployment.Internal.Isolation.Manifest;
    using System.Reflection; 
    using System.Security; 
    using System.Security.Permissions;
    using System.Security.Policy; 
    using System.Runtime.Hosting;

    [Flags, Serializable()]
    [System.Runtime.InteropServices.ComVisible(true)] 
    public enum HostSecurityManagerOptions {
        None                            = 0x0000, 
        HostAppDomainEvidence           = 0x0001, 
        HostPolicyLevel                 = 0x0002,
        HostAssemblyEvidence            = 0x0004, 
        HostDetermineApplicationTrust   = 0x0008,
        HostResolvePolicy               = 0x0010,
        AllFlags                        = 0x001F
    } 

    [Serializable] 
    [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
    [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.Infrastructure)]
    [System.Runtime.InteropServices.ComVisible(true)] 
    public class HostSecurityManager {
        public HostSecurityManager () {}

        // The host can choose which events he wants to participate in. This property can be set when 
        // the host only cares about a subset of the capabilities exposed through the HostSecurityManager.
        public virtual HostSecurityManagerOptions Flags { 
            get { 
                // We use AllFlags as the default.
                return HostSecurityManagerOptions.AllFlags; 
            }
        }

        // provide policy for the AppDomain. 
        public virtual PolicyLevel DomainPolicy {
            get { 
                return null; 
            }
        } 

        public virtual Evidence ProvideAppDomainEvidence (Evidence inputEvidence) {
            // The default implementation does not modify the input evidence.
            return inputEvidence; 
        }
 
        public virtual Evidence ProvideAssemblyEvidence (Assembly loadedAssembly, Evidence inputEvidence) { 
            // The default implementation does not modify the input evidence.
            return inputEvidence; 
        }

#if !FEATURE_PAL
        [SecurityPermissionAttribute(SecurityAction.Assert, Unrestricted=true)] 
        public virtual ApplicationTrust DetermineApplicationTrust (Evidence applicationEvidence, Evidence activatorEvidence, TrustManagerContext context) {
            if (applicationEvidence == null) 
                throw new ArgumentNullException("applicationEvidence"); 

            // This method looks for a trust decision for the ActivationContext in three locations, in order 
            // of preference:
            //
            // 1. Supplied by the host in the AppDomainSetup. If the host supplied a decision this way, it
            //    will be in the applicationEvidence. 
            // 2. Reuse the ApplicationTrust from the current AppDomain
            // 3. Ask the TrustManager for a trust decision 
 
            // get the activation context from the application evidence.
            // The default HostSecurityManager does not examine the activatorEvidence 
            // but other security managers could use it to figure out the
            // evidence of the domain attempting to activate the application.

            IEnumerator enumerator = applicationEvidence.GetHostEnumerator(); 
            ActivationArguments activationArgs = null;
            ApplicationTrust appTrust = null; 
            while (enumerator.MoveNext()) 
            {
                if (activationArgs == null) 
                    activationArgs = enumerator.Current as ActivationArguments;
                if (appTrust == null)
                    appTrust = enumerator.Current as ApplicationTrust;
 
                if (activationArgs != null && appTrust != null)
                    break; 
            } 
            if (activationArgs == null)
                throw new ArgumentException(Environment.GetResourceString("Policy_MissingActivationContextInAppEvidence")); 

            ActivationContext actCtx = activationArgs.ActivationContext;
            if (actCtx == null)
                throw new ArgumentException(Environment.GetResourceString("Policy_MissingActivationContextInAppEvidence")); 

            // Make sure that any ApplicationTrust we find applies to the ActivationContext we're 
            // creating the new AppDomain for. 
            if (appTrust != null &&
                !CmsUtils.CompareIdentities(appTrust.ApplicationIdentity, activationArgs.ApplicationIdentity, ApplicationVersionMatch.MatchExactVersion)) 
            {
                appTrust = null;
            }
 
            // If there was not a trust decision supplied in the Evidence, we can reuse the existing trust
            // decision from this domain if its identity matches the ActivationContext of the new domain. 
            // Otherwise consult the TrustManager for a trust decision 
            if (appTrust == null)
            { 
                if (AppDomain.CurrentDomain.ApplicationTrust != null &&
                    CmsUtils.CompareIdentities(AppDomain.CurrentDomain.ApplicationTrust.ApplicationIdentity, activationArgs.ApplicationIdentity, ApplicationVersionMatch.MatchExactVersion))
                {
                    appTrust = AppDomain.CurrentDomain.ApplicationTrust; 
                }
                else 
                { 
                    appTrust = ApplicationSecurityManager.DetermineApplicationTrustInternal(actCtx, context);
                } 
            }

            // If the trust decision allows the application to run, then it should also have a permission set
            // which is at least the permission set the application requested. 
            ApplicationSecurityInfo appRequest = new ApplicationSecurityInfo(actCtx);
            if (appTrust != null && 
                appTrust.IsApplicationTrustedToRun && 
                !appRequest.DefaultRequestSet.IsSubsetOf(appTrust.DefaultGrantSet.PermissionSet))
            { 
                throw new InvalidOperationException(Environment.GetResourceString("Policy_AppTrustMustGrantAppRequest"));
            }

            return appTrust; 
        }
#endif //!FEATURE_PAL 
 
        public virtual PermissionSet ResolvePolicy (Evidence evidence) {
            return SecurityManager.PolicyManager.ResolveHelper(evidence); 
        }
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// A HostSecurityManager gives a hosting application the chance to 
// participate in the security decisions in the AppDomain.
// 

namespace System.Security {
    using System.Collections;
    using System.Deployment.Internal.Isolation; 
    using System.Deployment.Internal.Isolation.Manifest;
    using System.Reflection; 
    using System.Security; 
    using System.Security.Permissions;
    using System.Security.Policy; 
    using System.Runtime.Hosting;

    [Flags, Serializable()]
    [System.Runtime.InteropServices.ComVisible(true)] 
    public enum HostSecurityManagerOptions {
        None                            = 0x0000, 
        HostAppDomainEvidence           = 0x0001, 
        HostPolicyLevel                 = 0x0002,
        HostAssemblyEvidence            = 0x0004, 
        HostDetermineApplicationTrust   = 0x0008,
        HostResolvePolicy               = 0x0010,
        AllFlags                        = 0x001F
    } 

    [Serializable] 
    [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
    [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.Infrastructure)]
    [System.Runtime.InteropServices.ComVisible(true)] 
    public class HostSecurityManager {
        public HostSecurityManager () {}

        // The host can choose which events he wants to participate in. This property can be set when 
        // the host only cares about a subset of the capabilities exposed through the HostSecurityManager.
        public virtual HostSecurityManagerOptions Flags { 
            get { 
                // We use AllFlags as the default.
                return HostSecurityManagerOptions.AllFlags; 
            }
        }

        // provide policy for the AppDomain. 
        public virtual PolicyLevel DomainPolicy {
            get { 
                return null; 
            }
        } 

        public virtual Evidence ProvideAppDomainEvidence (Evidence inputEvidence) {
            // The default implementation does not modify the input evidence.
            return inputEvidence; 
        }
 
        public virtual Evidence ProvideAssemblyEvidence (Assembly loadedAssembly, Evidence inputEvidence) { 
            // The default implementation does not modify the input evidence.
            return inputEvidence; 
        }

#if !FEATURE_PAL
        [SecurityPermissionAttribute(SecurityAction.Assert, Unrestricted=true)] 
        public virtual ApplicationTrust DetermineApplicationTrust (Evidence applicationEvidence, Evidence activatorEvidence, TrustManagerContext context) {
            if (applicationEvidence == null) 
                throw new ArgumentNullException("applicationEvidence"); 

            // This method looks for a trust decision for the ActivationContext in three locations, in order 
            // of preference:
            //
            // 1. Supplied by the host in the AppDomainSetup. If the host supplied a decision this way, it
            //    will be in the applicationEvidence. 
            // 2. Reuse the ApplicationTrust from the current AppDomain
            // 3. Ask the TrustManager for a trust decision 
 
            // get the activation context from the application evidence.
            // The default HostSecurityManager does not examine the activatorEvidence 
            // but other security managers could use it to figure out the
            // evidence of the domain attempting to activate the application.

            IEnumerator enumerator = applicationEvidence.GetHostEnumerator(); 
            ActivationArguments activationArgs = null;
            ApplicationTrust appTrust = null; 
            while (enumerator.MoveNext()) 
            {
                if (activationArgs == null) 
                    activationArgs = enumerator.Current as ActivationArguments;
                if (appTrust == null)
                    appTrust = enumerator.Current as ApplicationTrust;
 
                if (activationArgs != null && appTrust != null)
                    break; 
            } 
            if (activationArgs == null)
                throw new ArgumentException(Environment.GetResourceString("Policy_MissingActivationContextInAppEvidence")); 

            ActivationContext actCtx = activationArgs.ActivationContext;
            if (actCtx == null)
                throw new ArgumentException(Environment.GetResourceString("Policy_MissingActivationContextInAppEvidence")); 

            // Make sure that any ApplicationTrust we find applies to the ActivationContext we're 
            // creating the new AppDomain for. 
            if (appTrust != null &&
                !CmsUtils.CompareIdentities(appTrust.ApplicationIdentity, activationArgs.ApplicationIdentity, ApplicationVersionMatch.MatchExactVersion)) 
            {
                appTrust = null;
            }
 
            // If there was not a trust decision supplied in the Evidence, we can reuse the existing trust
            // decision from this domain if its identity matches the ActivationContext of the new domain. 
            // Otherwise consult the TrustManager for a trust decision 
            if (appTrust == null)
            { 
                if (AppDomain.CurrentDomain.ApplicationTrust != null &&
                    CmsUtils.CompareIdentities(AppDomain.CurrentDomain.ApplicationTrust.ApplicationIdentity, activationArgs.ApplicationIdentity, ApplicationVersionMatch.MatchExactVersion))
                {
                    appTrust = AppDomain.CurrentDomain.ApplicationTrust; 
                }
                else 
                { 
                    appTrust = ApplicationSecurityManager.DetermineApplicationTrustInternal(actCtx, context);
                } 
            }

            // If the trust decision allows the application to run, then it should also have a permission set
            // which is at least the permission set the application requested. 
            ApplicationSecurityInfo appRequest = new ApplicationSecurityInfo(actCtx);
            if (appTrust != null && 
                appTrust.IsApplicationTrustedToRun && 
                !appRequest.DefaultRequestSet.IsSubsetOf(appTrust.DefaultGrantSet.PermissionSet))
            { 
                throw new InvalidOperationException(Environment.GetResourceString("Policy_AppTrustMustGrantAppRequest"));
            }

            return appTrust; 
        }
#endif //!FEATURE_PAL 
 
        public virtual PermissionSet ResolvePolicy (Evidence evidence) {
            return SecurityManager.PolicyManager.ResolveHelper(evidence); 
        }
    }
}
