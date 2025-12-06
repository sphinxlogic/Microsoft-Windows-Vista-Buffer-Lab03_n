namespace System.Web { 
    using System.Web;
    using System.Web.Util;
    using System.Security;
    using System.Security.Permissions; 

    internal static class InternalSecurityPermissions { 
 
        private static IStackWalk   _unrestricted;
        private static IStackWalk   _unmanagedCode; 

#if UNUSED_CODE
        private static IStackWalk   _sensitiveInformation;
#endif 

        private static IStackWalk   _controlPrincipal; 
 
#if UNUSED_CODE
        private static IStackWalk   _controlEvidence; 
#endif

        private static IStackWalk   _reflection;
        private static IStackWalk   _appPathDiscovery; 
        private static IStackWalk   _controlThread;
 
#if UNUSED_CODE 
        private static IStackWalk   _levelMinimal;
#endif 

        private static IStackWalk   _levelLow;
        private static IStackWalk   _levelMedium;
        private static IStackWalk   _levelHigh; 

#if UNUSED_CODE 
        private static IStackWalk   _levelUnrestricted; 
#endif
 
        //
        // Static permissions as properties, created on demand
        //
 
        internal static IStackWalk Unrestricted {
            get { 
                if (_unrestricted == null) 
                    _unrestricted = new PermissionSet(PermissionState.Unrestricted);
 
                Debug.Trace("Permissions", "Unrestricted Set");
                return _unrestricted;
            }
        } 

        internal static IStackWalk UnmanagedCode { 
            get { 
                if (_unmanagedCode == null)
                    _unmanagedCode = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode); 

                Debug.Trace("Permissions", "UnmanagedCode");
                return _unmanagedCode;
            } 
        }
 
#if UNUSED_CODE 
        internal static IStackWalk SensitiveInformation {
            get { 
                if (_sensitiveInformation == null)
                    _sensitiveInformation = new EnvironmentPermission(PermissionState.Unrestricted);

                Debug.Trace("Permissions", "SensitiveInformation"); 
                return _sensitiveInformation;
            } 
        } 
#endif
 
        internal static IStackWalk ControlPrincipal {
            get {
                if (_controlPrincipal == null)
                    _controlPrincipal = new SecurityPermission(SecurityPermissionFlag.ControlPrincipal); 

                Debug.Trace("Permissions", "ControlPrincipal"); 
                return _controlPrincipal; 
            }
        } 

#if UNUSED_CODE
        internal static IStackWalk ControlEvidence {
            get { 
                if (_controlEvidence == null)
                    _controlEvidence = new SecurityPermission(SecurityPermissionFlag.ControlEvidence); 
 
                Debug.Trace("Permissions", "ControlEvidence");
                return _controlEvidence; 
            }
        }
#endif
 
        internal static IStackWalk Reflection {
            get { 
                if (_reflection == null) 
                    _reflection = new ReflectionPermission(ReflectionPermissionFlag.MemberAccess);
 
                Debug.Trace("Permissions", "Reflection");
                return _reflection;
            }
        } 

        internal static IStackWalk AppPathDiscovery { 
            get { 
                if (_appPathDiscovery == null)
                    _appPathDiscovery = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, HttpRuntime.AppDomainAppPathInternal); 

                Debug.Trace("Permissions", "AppPathDiscovery");
                return _appPathDiscovery;
            } 
        }
 
        internal static IStackWalk ControlThread { 
            get {
                if (_controlThread == null) 
                    _controlThread = new SecurityPermission(SecurityPermissionFlag.ControlThread);

                Debug.Trace("Permissions", "ControlThread");
                return _controlThread; 
            }
        } 
 
#if UNUSED_CODE
        internal static IStackWalk AspNetHostingPermissionLevelMinimal { 
            get {
                if (_levelMinimal == null)
                    _levelMinimal = new AspNetHostingPermission(AspNetHostingPermissionLevel.Minimal);
 
                Debug.Trace("Permissions", "AspNetHostingPermissionLevelMinimal");
                return _levelMinimal; 
            } 
        }
#endif 

        internal static IStackWalk AspNetHostingPermissionLevelLow {
            get {
                if (_levelLow == null) 
                    _levelLow = new AspNetHostingPermission(AspNetHostingPermissionLevel.Low);
 
                Debug.Trace("Permissions", "AspNetHostingPermissionLevelLow"); 
                return _levelLow;
            } 
        }

        internal static IStackWalk AspNetHostingPermissionLevelMedium {
            get { 
                if (_levelMedium == null)
                    _levelMedium = new AspNetHostingPermission(AspNetHostingPermissionLevel.Medium); 
 
                Debug.Trace("Permissions", "AspNetHostingPermissionLevelMedium");
                return _levelMedium; 
            }
        }

        internal static IStackWalk AspNetHostingPermissionLevelHigh { 
            get {
                if (_levelHigh == null) 
                    _levelHigh = new AspNetHostingPermission(AspNetHostingPermissionLevel.High); 

                Debug.Trace("Permissions", "AspNetHostingPermissionLevelHigh"); 
                return _levelHigh;
            }
        }
 

#if UNUSED_CODE 
        internal static IStackWalk AspNetHostingPermissionLevelUnrestricted { 
            get {
                if (_levelUnrestricted == null) 
                    _levelUnrestricted = new AspNetHostingPermission(AspNetHostingPermissionLevel.Unrestricted);

                Debug.Trace("Permissions", "AspNetHostingPermissionLevelUnrestricted");
                return _levelUnrestricted; 
            }
        } 
#endif 

 
        // Parameterized permissions

        internal static IStackWalk FileReadAccess(String filename) {
            Debug.Trace("Permissions", "FileReadAccess(" + filename + ")"); 
            return new FileIOPermission(FileIOPermissionAccess.Read, filename);
        } 
 
        internal static IStackWalk PathDiscovery(String path) {
            Debug.Trace("Permissions", "PathDiscovery(" + path + ")"); 
            return new FileIOPermission(FileIOPermissionAccess.PathDiscovery, path);
        }

    } 
}
namespace System.Web { 
    using System.Web;
    using System.Web.Util;
    using System.Security;
    using System.Security.Permissions; 

    internal static class InternalSecurityPermissions { 
 
        private static IStackWalk   _unrestricted;
        private static IStackWalk   _unmanagedCode; 

#if UNUSED_CODE
        private static IStackWalk   _sensitiveInformation;
#endif 

        private static IStackWalk   _controlPrincipal; 
 
#if UNUSED_CODE
        private static IStackWalk   _controlEvidence; 
#endif

        private static IStackWalk   _reflection;
        private static IStackWalk   _appPathDiscovery; 
        private static IStackWalk   _controlThread;
 
#if UNUSED_CODE 
        private static IStackWalk   _levelMinimal;
#endif 

        private static IStackWalk   _levelLow;
        private static IStackWalk   _levelMedium;
        private static IStackWalk   _levelHigh; 

#if UNUSED_CODE 
        private static IStackWalk   _levelUnrestricted; 
#endif
 
        //
        // Static permissions as properties, created on demand
        //
 
        internal static IStackWalk Unrestricted {
            get { 
                if (_unrestricted == null) 
                    _unrestricted = new PermissionSet(PermissionState.Unrestricted);
 
                Debug.Trace("Permissions", "Unrestricted Set");
                return _unrestricted;
            }
        } 

        internal static IStackWalk UnmanagedCode { 
            get { 
                if (_unmanagedCode == null)
                    _unmanagedCode = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode); 

                Debug.Trace("Permissions", "UnmanagedCode");
                return _unmanagedCode;
            } 
        }
 
#if UNUSED_CODE 
        internal static IStackWalk SensitiveInformation {
            get { 
                if (_sensitiveInformation == null)
                    _sensitiveInformation = new EnvironmentPermission(PermissionState.Unrestricted);

                Debug.Trace("Permissions", "SensitiveInformation"); 
                return _sensitiveInformation;
            } 
        } 
#endif
 
        internal static IStackWalk ControlPrincipal {
            get {
                if (_controlPrincipal == null)
                    _controlPrincipal = new SecurityPermission(SecurityPermissionFlag.ControlPrincipal); 

                Debug.Trace("Permissions", "ControlPrincipal"); 
                return _controlPrincipal; 
            }
        } 

#if UNUSED_CODE
        internal static IStackWalk ControlEvidence {
            get { 
                if (_controlEvidence == null)
                    _controlEvidence = new SecurityPermission(SecurityPermissionFlag.ControlEvidence); 
 
                Debug.Trace("Permissions", "ControlEvidence");
                return _controlEvidence; 
            }
        }
#endif
 
        internal static IStackWalk Reflection {
            get { 
                if (_reflection == null) 
                    _reflection = new ReflectionPermission(ReflectionPermissionFlag.MemberAccess);
 
                Debug.Trace("Permissions", "Reflection");
                return _reflection;
            }
        } 

        internal static IStackWalk AppPathDiscovery { 
            get { 
                if (_appPathDiscovery == null)
                    _appPathDiscovery = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, HttpRuntime.AppDomainAppPathInternal); 

                Debug.Trace("Permissions", "AppPathDiscovery");
                return _appPathDiscovery;
            } 
        }
 
        internal static IStackWalk ControlThread { 
            get {
                if (_controlThread == null) 
                    _controlThread = new SecurityPermission(SecurityPermissionFlag.ControlThread);

                Debug.Trace("Permissions", "ControlThread");
                return _controlThread; 
            }
        } 
 
#if UNUSED_CODE
        internal static IStackWalk AspNetHostingPermissionLevelMinimal { 
            get {
                if (_levelMinimal == null)
                    _levelMinimal = new AspNetHostingPermission(AspNetHostingPermissionLevel.Minimal);
 
                Debug.Trace("Permissions", "AspNetHostingPermissionLevelMinimal");
                return _levelMinimal; 
            } 
        }
#endif 

        internal static IStackWalk AspNetHostingPermissionLevelLow {
            get {
                if (_levelLow == null) 
                    _levelLow = new AspNetHostingPermission(AspNetHostingPermissionLevel.Low);
 
                Debug.Trace("Permissions", "AspNetHostingPermissionLevelLow"); 
                return _levelLow;
            } 
        }

        internal static IStackWalk AspNetHostingPermissionLevelMedium {
            get { 
                if (_levelMedium == null)
                    _levelMedium = new AspNetHostingPermission(AspNetHostingPermissionLevel.Medium); 
 
                Debug.Trace("Permissions", "AspNetHostingPermissionLevelMedium");
                return _levelMedium; 
            }
        }

        internal static IStackWalk AspNetHostingPermissionLevelHigh { 
            get {
                if (_levelHigh == null) 
                    _levelHigh = new AspNetHostingPermission(AspNetHostingPermissionLevel.High); 

                Debug.Trace("Permissions", "AspNetHostingPermissionLevelHigh"); 
                return _levelHigh;
            }
        }
 

#if UNUSED_CODE 
        internal static IStackWalk AspNetHostingPermissionLevelUnrestricted { 
            get {
                if (_levelUnrestricted == null) 
                    _levelUnrestricted = new AspNetHostingPermission(AspNetHostingPermissionLevel.Unrestricted);

                Debug.Trace("Permissions", "AspNetHostingPermissionLevelUnrestricted");
                return _levelUnrestricted; 
            }
        } 
#endif 

 
        // Parameterized permissions

        internal static IStackWalk FileReadAccess(String filename) {
            Debug.Trace("Permissions", "FileReadAccess(" + filename + ")"); 
            return new FileIOPermission(FileIOPermissionAccess.Read, filename);
        } 
 
        internal static IStackWalk PathDiscovery(String path) {
            Debug.Trace("Permissions", "PathDiscovery(" + path + ")"); 
            return new FileIOPermission(FileIOPermissionAccess.PathDiscovery, path);
        }

    } 
}
