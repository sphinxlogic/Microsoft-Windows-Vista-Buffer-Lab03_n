 
namespace System.Runtime.Hosting {
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ActivationArguments { 
        private ActivationArguments () {}
 
        // This boolean is used to smuggle the information about whether 
        // AppDomainSetup was constructed from an ActivationContext.
        private bool m_useFusionActivationContext = false; 
        internal bool UseFusionActivationContext {
            get {
                return m_useFusionActivationContext;
            } 
        }
 
        // This is used to indicate whether the instance is to be activated 
        // during the new domain's initialization. CreateInstanceHelper sets
        // this flag to true; CreateDomainHelper never activates the application. 
        private bool m_activateInstance = false;
        internal bool ActivateInstance {
            get {
                return m_activateInstance; 
            }
            set { 
                m_activateInstance = value; 
            }
        } 

        private string m_appFullName;
        internal string ApplicationFullName {
            get { 
                return m_appFullName;
            } 
        } 

        private string[] m_appManifestPaths; 
        internal string[] ApplicationManifestPaths {
            get {
                return m_appManifestPaths;
            } 
        }
 
#if !FEATURE_PAL 
        public ActivationArguments (ApplicationIdentity applicationIdentity) : this (applicationIdentity, null) {}
        public ActivationArguments (ApplicationIdentity applicationIdentity, string[] activationData) { 
            if (applicationIdentity == null)
                throw new ArgumentNullException("applicationIdentity");

            m_appFullName = applicationIdentity.FullName; 
            m_activationData = activationData;
        } 
 
        public ActivationArguments (ActivationContext activationData) : this (activationData, null) {}
        public ActivationArguments (ActivationContext activationContext, string[] activationData) { 
            if (activationContext == null)
                throw new ArgumentNullException("activationContext");

            m_appFullName = activationContext.Identity.FullName; 
            m_appManifestPaths = activationContext.ManifestPaths;
            m_activationData = activationData; 
            m_useFusionActivationContext = true; 
        }
 
        internal ActivationArguments (string appFullName, string[] appManifestPaths, string[] activationData) {
            if (appFullName == null)
                throw new ArgumentNullException("appFullName");
 
            m_appFullName = appFullName;
            m_appManifestPaths = appManifestPaths; 
            m_activationData = activationData; 
            m_useFusionActivationContext = true;
        } 

        public ApplicationIdentity ApplicationIdentity {
            get {
                return new ApplicationIdentity(m_appFullName); 
            }
        } 
 
        public ActivationContext ActivationContext {
            get { 
                if (!UseFusionActivationContext)
                    return null;

                if (m_appManifestPaths == null) 
                    return new ActivationContext(new ApplicationIdentity(m_appFullName));
                else 
                    return new ActivationContext(new ApplicationIdentity(m_appFullName), m_appManifestPaths); 
            }
        } 
#endif // !FEATURE_PAL

        private string[] m_activationData;
        public string[] ActivationData { 
            get {
                return m_activationData; 
            } 
        }
    } 
}
 
namespace System.Runtime.Hosting {
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ActivationArguments { 
        private ActivationArguments () {}
 
        // This boolean is used to smuggle the information about whether 
        // AppDomainSetup was constructed from an ActivationContext.
        private bool m_useFusionActivationContext = false; 
        internal bool UseFusionActivationContext {
            get {
                return m_useFusionActivationContext;
            } 
        }
 
        // This is used to indicate whether the instance is to be activated 
        // during the new domain's initialization. CreateInstanceHelper sets
        // this flag to true; CreateDomainHelper never activates the application. 
        private bool m_activateInstance = false;
        internal bool ActivateInstance {
            get {
                return m_activateInstance; 
            }
            set { 
                m_activateInstance = value; 
            }
        } 

        private string m_appFullName;
        internal string ApplicationFullName {
            get { 
                return m_appFullName;
            } 
        } 

        private string[] m_appManifestPaths; 
        internal string[] ApplicationManifestPaths {
            get {
                return m_appManifestPaths;
            } 
        }
 
#if !FEATURE_PAL 
        public ActivationArguments (ApplicationIdentity applicationIdentity) : this (applicationIdentity, null) {}
        public ActivationArguments (ApplicationIdentity applicationIdentity, string[] activationData) { 
            if (applicationIdentity == null)
                throw new ArgumentNullException("applicationIdentity");

            m_appFullName = applicationIdentity.FullName; 
            m_activationData = activationData;
        } 
 
        public ActivationArguments (ActivationContext activationData) : this (activationData, null) {}
        public ActivationArguments (ActivationContext activationContext, string[] activationData) { 
            if (activationContext == null)
                throw new ArgumentNullException("activationContext");

            m_appFullName = activationContext.Identity.FullName; 
            m_appManifestPaths = activationContext.ManifestPaths;
            m_activationData = activationData; 
            m_useFusionActivationContext = true; 
        }
 
        internal ActivationArguments (string appFullName, string[] appManifestPaths, string[] activationData) {
            if (appFullName == null)
                throw new ArgumentNullException("appFullName");
 
            m_appFullName = appFullName;
            m_appManifestPaths = appManifestPaths; 
            m_activationData = activationData; 
            m_useFusionActivationContext = true;
        } 

        public ApplicationIdentity ApplicationIdentity {
            get {
                return new ApplicationIdentity(m_appFullName); 
            }
        } 
 
        public ActivationContext ActivationContext {
            get { 
                if (!UseFusionActivationContext)
                    return null;

                if (m_appManifestPaths == null) 
                    return new ActivationContext(new ApplicationIdentity(m_appFullName));
                else 
                    return new ActivationContext(new ApplicationIdentity(m_appFullName), m_appManifestPaths); 
            }
        } 
#endif // !FEATURE_PAL

        private string[] m_activationData;
        public string[] ActivationData { 
            get {
                return m_activationData; 
            } 
        }
    } 
}
