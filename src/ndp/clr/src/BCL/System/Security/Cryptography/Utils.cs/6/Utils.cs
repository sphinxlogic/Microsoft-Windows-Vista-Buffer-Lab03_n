// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// Utils.cs 
//
// 05/01/2002 
//

namespace System.Security.Cryptography
{ 
    using Microsoft.Win32;
    using System.IO; 
    using System.Globalization; 
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices; 
    using System.Security.AccessControl;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Permissions;
    using System.Security.Principal; 
    using System.Text;
    using System.Threading; 
    using System.Runtime.Versioning; 

    [Serializable] 
    internal enum CspAlgorithmType {
        Rsa = 0,
        Dss = 1
    } 

    internal static class Constants { 
        internal const int S_OK                   = 0; 
        internal const int NTE_NO_KEY             = unchecked((int) 0x8009000D); // Key does not exist.
        internal const int NTE_BAD_KEYSET         = unchecked((int) 0x80090016); // Keyset does not exist. 
        internal const int NTE_KEYSET_NOT_DEF     = unchecked((int) 0x80090019); // The keyset is not defined.

        internal const int KP_IV                  = 1;
        internal const int KP_MODE                = 4; 
        internal const int KP_MODE_BITS           = 5;
        internal const int KP_EFFECTIVE_KEYLEN    = 19; 
 
        internal const int ALG_CLASS_SIGNATURE    = (1 << 13);
        internal const int ALG_CLASS_DATA_ENCRYPT = (3 << 13); 
        internal const int ALG_CLASS_HASH         = (4 << 13);
        internal const int ALG_CLASS_KEY_EXCHANGE = (5 << 13);

        internal const int ALG_TYPE_DSS           = (1 << 9); 
        internal const int ALG_TYPE_RSA           = (2 << 9);
        internal const int ALG_TYPE_BLOCK         = (3 << 9); 
        internal const int ALG_TYPE_STREAM        = (4 << 9); 
        internal const int ALG_TYPE_ANY           = (0);
 
        internal const int CALG_MD5               = (ALG_CLASS_HASH | ALG_TYPE_ANY | 3);
        internal const int CALG_SHA1              = (ALG_CLASS_HASH | ALG_TYPE_ANY | 4);
        internal const int CALG_RSA_KEYX          = (ALG_CLASS_KEY_EXCHANGE | ALG_TYPE_RSA | 0);
        internal const int CALG_RSA_SIGN          = (ALG_CLASS_SIGNATURE | ALG_TYPE_RSA | 0); 
        internal const int CALG_DSS_SIGN          = (ALG_CLASS_SIGNATURE | ALG_TYPE_DSS | 0);
        internal const int CALG_DES               = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 1); 
        internal const int CALG_RC2               = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 2); 
        internal const int CALG_3DES              = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 3);
        internal const int CALG_3DES_112          = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 9); 
        internal const int CALG_AES_128           = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 14);
        internal const int CALG_AES_192           = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 15);
        internal const int CALG_AES_256           = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 16);
        internal const int CALG_RC4               = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_STREAM | 1); 

        internal const int PROV_RSA_FULL          = 1; 
        internal const int PROV_DSS_DH            = 13; 

        internal const int AT_KEYEXCHANGE         = 1; 
        internal const int AT_SIGNATURE           = 2;
        internal const int PUBLICKEYBLOB          = 0x6;
        internal const int PRIVATEKEYBLOB         = 0x7;
        internal const int CRYPT_OAEP             = 0x40; 

 
        internal const uint CRYPT_VERIFYCONTEXT    = 0xF0000000; 
        internal const uint CRYPT_NEWKEYSET        = 0x00000008;
        internal const uint CRYPT_DELETEKEYSET     = 0x00000010; 
        internal const uint CRYPT_MACHINE_KEYSET   = 0x00000020;
        internal const uint CRYPT_SILENT           = 0x00000040;
        internal const uint CRYPT_EXPORTABLE       = 0x00000001;
 
        internal const uint CLR_KEYLEN            = 1;
        internal const uint CLR_PUBLICKEYONLY     = 2; 
        internal const uint CLR_EXPORTABLE        = 3; 
        internal const uint CLR_REMOVABLE         = 4;
        internal const uint CLR_HARDWARE          = 5; 
        internal const uint CLR_ACCESSIBLE        = 6;
        internal const uint CLR_PROTECTED         = 7;
        internal const uint CLR_UNIQUE_CONTAINER  = 8;
        internal const uint CLR_ALGID             = 9; 
        internal const uint CLR_PP_CLIENT_HWND    = 10;
        internal const uint CLR_PP_PIN            = 11; 
 
        internal const string OID_RSA_SMIMEalgCMS3DESwrap   = "1.2.840.113549.1.9.16.3.6";
        internal const string OID_RSA_MD5                   = "1.2.840.113549.2.5"; 
        internal const string OID_RSA_RC2CBC                = "1.2.840.113549.3.2";
        internal const string OID_RSA_DES_EDE3_CBC          = "1.2.840.113549.3.7";
        internal const string OID_OIWSEC_desCBC             = "1.3.14.3.2.7";
        internal const string OID_OIWSEC_SHA1               = "1.3.14.3.2.26"; 
        internal const string OID_OIWSEC_SHA256             = "2.16.840.1.101.3.4.2.1";
        internal const string OID_OIWSEC_SHA384             = "2.16.840.1.101.3.4.2.2"; 
        internal const string OID_OIWSEC_SHA512             = "2.16.840.1.101.3.4.2.3"; 
        internal const string OID_OIWSEC_RIPEMD160          = "1.3.36.3.2.1";
    } 

    internal static class Utils {

        // Private object for locking instead of locking on a public type for SQL reliability work. 
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
 
        private static SafeProvHandle _safeProvHandle = null; 
        internal static SafeProvHandle StaticProvHandle {
            get { 
                if (_safeProvHandle == null) {
                    lock (InternalSyncObject) {
                        if (_safeProvHandle == null) {
                            SafeProvHandle safeProvHandle = AcquireProvHandle(new CspParameters(Constants.PROV_RSA_FULL)); 
                            Thread.MemoryBarrier();
                            _safeProvHandle = safeProvHandle; 
                        } 
                    }
                } 
                return _safeProvHandle;
            }
        }
 
        private static SafeProvHandle _safeDssProvHandle = null;
        internal static SafeProvHandle StaticDssProvHandle { 
            get { 
                if (_safeDssProvHandle == null) {
                    lock (InternalSyncObject) { 
                        if (_safeDssProvHandle == null) {
                            SafeProvHandle safeProvHandle = AcquireProvHandle(new CspParameters(Constants.PROV_DSS_DH));
                            Thread.MemoryBarrier();
                            _safeDssProvHandle = safeProvHandle; 
                        }
                    } 
                } 
                return _safeDssProvHandle;
            } 
        }

        internal static SafeProvHandle AcquireProvHandle (CspParameters parameters) {
            if (parameters == null) 
                parameters = new CspParameters(Constants.PROV_RSA_FULL);
 
            SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle; 
            if (Utils.Win2KCrypto == 1) {
                Utils._AcquireCSP(parameters, ref safeProvHandle); // CRYPT_VERIFYCONTEXT is only supported in Win2K 
            } else {
                // Using the default key container is a bad idea since this might lead to races
                // if multiple threads are trying to access, delete or generate keys inside the container.
                if (parameters.KeyContainerName == null && 
                    (parameters.Flags & CspProviderFlags.UseDefaultKeyContainer) == 0)
                    parameters.KeyContainerName = _GetRandomKeyContainer(); 
                safeProvHandle = CreateProvHandle(parameters, true); 
            }
 
            return safeProvHandle;
        }

        internal static SafeProvHandle CreateProvHandle (CspParameters parameters, bool randomKeyContainer) { 
            SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
            int hr = Utils._OpenCSP(parameters, 0, ref safeProvHandle); 
            KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags); 
            if (hr != Constants.S_OK) {
                // If UseExistingKey flag is used and the key container does not exist 
                // throw an exception without attempting to create the container.
                if ((parameters.Flags & CspProviderFlags.UseExistingKey) != 0 || (hr != Constants.NTE_KEYSET_NOT_DEF && hr != Constants.NTE_BAD_KEYSET))
                    throw new CryptographicException(hr);
                if (!randomKeyContainer) { 
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Create);
                    kp.AccessEntries.Add(entry); 
                    kp.Demand(); 
                }
                Utils._CreateCSP(parameters, randomKeyContainer, ref safeProvHandle); 
            } else {
                if (!randomKeyContainer) {
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Open);
                    kp.AccessEntries.Add(entry); 
                    kp.Demand();
                } 
            } 
            return safeProvHandle;
        } 

        internal static CryptoKeySecurity GetKeySetSecurityInfo (SafeProvHandle hProv, AccessControlSections accessControlSections) {
            if (Utils.Win2KCrypto != 1)
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_RequiresNT")); 

            SecurityInfos securityInfo = 0; 
            Privilege privilege = null; 

            if ((accessControlSections & AccessControlSections.Owner) != 0) 
                securityInfo |= SecurityInfos.Owner;
            if ((accessControlSections & AccessControlSections.Group) != 0)
                securityInfo |= SecurityInfos.Group;
            if ((accessControlSections & AccessControlSections.Access) != 0) 
                securityInfo |= SecurityInfos.DiscretionaryAcl;
 
            byte[] rawSecurityDescriptor = null; 
            int error;
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                if ((accessControlSections & AccessControlSections.Audit) != 0) {
                    securityInfo |= SecurityInfos.SystemAcl; 
                    privilege = new Privilege("SeSecurityPrivilege");
                    privilege.Enable(); 
                } 
                rawSecurityDescriptor = _GetKeySetSecurityInfo(hProv, securityInfo, out error);
            } 
            finally {
                if (privilege != null)
                    privilege.Revert();
            } 

            // This means that the object doesn't have a security descriptor. And thus we throw 
            // a specific exception for the caller to catch and handle properly. 
            if (error == Win32Native.ERROR_SUCCESS && (rawSecurityDescriptor == null || rawSecurityDescriptor.Length == 0))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoSecurityDescriptor")); 
            if (error == Win32Native.ERROR_NOT_ENOUGH_MEMORY)
                throw new OutOfMemoryException();
            if (error == Win32Native.ERROR_ACCESS_DENIED)
                throw new UnauthorizedAccessException(); 
            if (error == Win32Native.ERROR_PRIVILEGE_NOT_HELD)
                throw new PrivilegeNotHeldException( "SeSecurityPrivilege" ); 
            if (error != Win32Native.ERROR_SUCCESS) 
                throw new CryptographicException(error);
 
            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false /* isContainer */,
                                                                       false /* isDS */,
                                                                       new RawSecurityDescriptor(rawSecurityDescriptor, 0),
                                                                       true); 
            return new CryptoKeySecurity(sd);
        } 
 
        internal static void SetKeySetSecurityInfo (SafeProvHandle hProv, CryptoKeySecurity cryptoKeySecurity, AccessControlSections accessControlSections) {
            if (Utils.Win2KCrypto != 1) 
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_RequiresNT"));

            SecurityInfos securityInfo = 0;
            Privilege privilege = null; 

            if ((accessControlSections & AccessControlSections.Owner) != 0 && cryptoKeySecurity._securityDescriptor.Owner != null) 
                securityInfo |= SecurityInfos.Owner; 
            if ((accessControlSections & AccessControlSections.Group) != 0 && cryptoKeySecurity._securityDescriptor.Group != null)
                securityInfo |= SecurityInfos.Group; 
            if ((accessControlSections & AccessControlSections.Audit) != 0)
                securityInfo |= SecurityInfos.SystemAcl;
            if ((accessControlSections & AccessControlSections.Access) != 0 && cryptoKeySecurity._securityDescriptor.IsDiscretionaryAclPresent)
                securityInfo |= SecurityInfos.DiscretionaryAcl; 

            if (securityInfo == 0) { 
                // Nothing to persist 
                return;
            } 

            int error = 0;

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                if ((securityInfo & SecurityInfos.SystemAcl) != 0) { 
                    privilege = new Privilege("SeSecurityPrivilege"); 
                    privilege.Enable();
                } 

                byte[] sd = cryptoKeySecurity.GetSecurityDescriptorBinaryForm();
                if (sd != null && sd.Length > 0)
                    error = _SetKeySetSecurityInfo (hProv, securityInfo, sd); 
            }
            finally { 
                if (privilege != null) 
                    privilege.Revert();
            } 

            if (error == Win32Native.ERROR_ACCESS_DENIED || error == Win32Native.ERROR_INVALID_OWNER || error == Win32Native.ERROR_INVALID_PRIMARY_GROUP)
                throw new UnauthorizedAccessException();
            else if (error == Win32Native.ERROR_PRIVILEGE_NOT_HELD) 
                throw new PrivilegeNotHeldException("SeSecurityPrivilege");
            else if (error == Win32Native.ERROR_INVALID_HANDLE) 
                throw new NotSupportedException(Environment.GetResourceString("AccessControl_InvalidHandle")); 
            else if (error != Win32Native.ERROR_SUCCESS)
                throw new CryptographicException(error); 
        }

        internal static byte[] ExportCspBlobHelper (bool includePrivateParameters, CspParameters parameters, SafeKeyHandle safeKeyHandle) {
            if (includePrivateParameters) { 
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Export); 
                kp.AccessEntries.Add(entry); 
                kp.Demand();
            } 
            return Utils._ExportCspBlob(safeKeyHandle, includePrivateParameters ? Constants.PRIVATEKEYBLOB : Constants.PUBLICKEYBLOB);
        }

        internal static void GetKeyPairHelper (CspAlgorithmType keyType, CspParameters parameters, bool randomKeyContainer, int dwKeySize, ref SafeProvHandle safeProvHandle, ref SafeKeyHandle safeKeyHandle) { 
            SafeProvHandle TempFetchedProvHandle = Utils.CreateProvHandle(parameters, randomKeyContainer);
 
            // If the user wanted to set the security descriptor on the provider context, apply it now. 
            if (parameters.CryptoKeySecurity != null) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags); 
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.ChangeAcl);
                kp.AccessEntries.Add(entry);
                kp.Demand();
                SetKeySetSecurityInfo(TempFetchedProvHandle, parameters.CryptoKeySecurity, AccessControlSections.All); 
            }
 
            // If the user wanted to specify a PIN or HWND for a smart card CSP, apply those settings now. 
            if (parameters.ParentWindowHandle != IntPtr.Zero)
                _SetProviderParameter(TempFetchedProvHandle, parameters.KeyNumber, Constants.CLR_PP_CLIENT_HWND, parameters.ParentWindowHandle); 
            else if (parameters.KeyPassword != null) {
                IntPtr szPassword = Marshal.SecureStringToCoTaskMemAnsi(parameters.KeyPassword);
                try {
                    _SetProviderParameter(TempFetchedProvHandle, parameters.KeyNumber, Constants.CLR_PP_PIN, szPassword); 
                }
                finally { 
                    if (szPassword != IntPtr.Zero) 
                        Marshal.ZeroFreeCoTaskMemAnsi(szPassword);
                } 
            }

            safeProvHandle = TempFetchedProvHandle;
 
            // If the key already exists, use it, else generate a new one
            SafeKeyHandle TempFetchedKeyHandle = SafeKeyHandle.InvalidHandle; 
            int hr = Utils._GetUserKey(safeProvHandle, parameters.KeyNumber, ref TempFetchedKeyHandle); 
            if (hr != Constants.S_OK) {
                if ((parameters.Flags & CspProviderFlags.UseExistingKey) != 0 || hr != Constants.NTE_NO_KEY) 
                    throw new CryptographicException(hr);
                // _GenerateKey will check for failures and throw an exception
                Utils._GenerateKey(safeProvHandle, parameters.KeyNumber, parameters.Flags, dwKeySize, ref TempFetchedKeyHandle);
            } 

            // check that this is indeed an RSA/DSS key. 
            byte[] algid = (byte[]) Utils._GetKeyParameter(TempFetchedKeyHandle, Constants.CLR_ALGID); 
            int dwAlgId = (algid[0] | (algid[1] << 8) | (algid[2] << 16) | (algid[3] << 24));
            if ((keyType == CspAlgorithmType.Rsa && dwAlgId != Constants.CALG_RSA_KEYX && dwAlgId != Constants.CALG_RSA_SIGN) || 
                    (keyType == CspAlgorithmType.Dss && dwAlgId != Constants.CALG_DSS_SIGN)) {
                TempFetchedKeyHandle.Dispose();
                throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_WrongKeySpec"));
            } 

            safeKeyHandle = TempFetchedKeyHandle; 
        } 

        internal static void ImportCspBlobHelper (CspAlgorithmType keyType, byte[] keyBlob, bool publicOnly, ref CspParameters parameters, bool randomKeyContainer, ref SafeProvHandle safeProvHandle, ref SafeKeyHandle safeKeyHandle) { 
            // Free the current key handle
            if (safeKeyHandle != null && !safeKeyHandle.IsClosed)
                safeKeyHandle.Dispose();
            safeKeyHandle = SafeKeyHandle.InvalidHandle; 

            if (publicOnly) { 
                parameters.KeyNumber = Utils._ImportCspBlob(keyBlob, keyType == CspAlgorithmType.Dss ? Utils.StaticDssProvHandle : Utils.StaticProvHandle, (CspProviderFlags) 0, ref safeKeyHandle); 
            } else {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags); 
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Import);
                kp.AccessEntries.Add(entry);
                kp.Demand();
                if (safeProvHandle == null) 
                    safeProvHandle = Utils.CreateProvHandle(parameters, randomKeyContainer);
                parameters.KeyNumber = Utils._ImportCspBlob(keyBlob, safeProvHandle, parameters.Flags, ref safeKeyHandle); 
            } 
        }
 
        internal static CspParameters SaveCspParameters (CspAlgorithmType keyType, CspParameters userParameters, CspProviderFlags defaultFlags, ref bool randomKeyContainer) {
            CspParameters parameters;
            if (userParameters == null) {
                parameters = new CspParameters(keyType == CspAlgorithmType.Dss ? Constants.PROV_DSS_DH : Constants.PROV_RSA_FULL, null, null, defaultFlags); 
            } else {
                ValidateCspFlags(userParameters.Flags); 
                parameters = new CspParameters(userParameters); 
            }
 
            if (parameters.KeyNumber == -1)
                parameters.KeyNumber = keyType == CspAlgorithmType.Dss ? Constants.AT_SIGNATURE : Constants.AT_KEYEXCHANGE;
            else if (parameters.KeyNumber == Constants.CALG_DSS_SIGN || parameters.KeyNumber == Constants.CALG_RSA_SIGN)
                parameters.KeyNumber = Constants.AT_SIGNATURE; 
            else if (parameters.KeyNumber == Constants.CALG_RSA_KEYX)
                parameters.KeyNumber = Constants.AT_KEYEXCHANGE; 
 
            // If no key container was specified and UseDefaultKeyContainer is not used,
            // generate a random key container name 
            randomKeyContainer = false;
            if (parameters.KeyContainerName == null &&
                (parameters.Flags & CspProviderFlags.UseDefaultKeyContainer) == 0) {
                parameters.KeyContainerName = Utils._GetRandomKeyContainer(); 
                randomKeyContainer = true;
            } 
 
            return parameters;
        } 

        private static void ValidateCspFlags (CspProviderFlags flags) {
            // check that the flags are consistent.
            if ((flags & CspProviderFlags.UseExistingKey) != 0) { 
                CspProviderFlags keyFlags = (CspProviderFlags.UseNonExportableKey | CspProviderFlags.UseArchivableKey | CspProviderFlags.UseUserProtectedKey);
                if ((flags & keyFlags) != CspProviderFlags.NoFlags) 
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag")); 
            }
 
            // make sure we are allowed to display the key protection UI if a user protected key is requested.
            if ((flags & CspProviderFlags.UseUserProtectedKey) != 0) {
                // UI only allowed in interactive session.
                if (!System.Environment.UserInteractive) 
                    throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NotInteractive"));
 
                // we need to demand UI permission here. 
                UIPermission uiPermission = new UIPermission(UIPermissionWindow.SafeTopLevelWindows);
                uiPermission.Demand(); 
            }
        }

        private static RNGCryptoServiceProvider _rng = null; 
        internal static RNGCryptoServiceProvider StaticRandomNumberGenerator {
            get { 
                if (_rng == null) 
                    _rng = new RNGCryptoServiceProvider();
                return _rng; 
            }
        }

        // 
        // internal static methods
        // 
 
        internal static byte[] GenerateRandom (int keySize) {
            byte[] key = new byte[keySize]; 
            StaticRandomNumberGenerator.GetBytes(key);
            return key;
        }
 
        // dwKeySize = 0 means the default key size
        internal static bool HasAlgorithm (int dwCalg, int dwKeySize) { 
            bool r = false; 
            // We need to take a lock here since we are querying the provider handle in a loop.
            // If multiple threads are racing in this code, not all algorithms/key sizes combinations 
            // will be examined; which may lead to a situation where false is wrongfully returned.
            lock (InternalSyncObject) {
                r = _SearchForAlgorithm(StaticProvHandle, dwCalg, dwKeySize);
            } 
            return r;
        } 
 
        private static int s_hasEnhProv = -1;
        internal static int HasEnhProv { 
            get {
                if (s_hasEnhProv == -1)
                    s_hasEnhProv = Utils.HasAlgorithm(Constants.CALG_RSA_KEYX, 2048) ? 1 : 0;
 
                return s_hasEnhProv;
            } 
        } 

        private static int s_fipsAlgorithmPolicy = -1; 
        internal static int FipsAlgorithmPolicy {
            [RegistryPermissionAttribute(SecurityAction.Assert, Unrestricted=true)]
            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)] 
            get {
                if (s_fipsAlgorithmPolicy == -1) { 
                    if (!_GetEnforceFipsPolicySetting()) { 
                        s_fipsAlgorithmPolicy = 0;
                    } 
                    else if (Environment.OSVersion.Version.Major >= 6) {
                        bool fipsEnabled;
                        uint policyReadStatus = Win32Native.BCryptGetFipsAlgorithmMode(out fipsEnabled);
 
                        bool readPolicy = policyReadStatus == Win32Native.STATUS_SUCCESS ||
                                          policyReadStatus == Win32Native.STATUS_OBJECT_NAME_NOT_FOUND; 
 
                        if (!readPolicy || fipsEnabled) {
                            s_fipsAlgorithmPolicy = 1; 
                        }
                        else {
                            s_fipsAlgorithmPolicy = 0;
                        } 
                    }
                    else 
                    { 
                        using (RegistryKey fipsAlgorithmPolicyKey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Lsa", false)) {
                            if (fipsAlgorithmPolicyKey != null) { 
                                object data = fipsAlgorithmPolicyKey.GetValue("FIPSAlgorithmPolicy");
                                if (data != null)
                                    s_fipsAlgorithmPolicy = (int) data;
                            } 
                        }
                    } 
                } 
                return s_fipsAlgorithmPolicy;
            } 
        }

        private static int s_win2KCrypto = -1;
        internal static int Win2KCrypto { 
            get {
                if (s_win2KCrypto == -1) { 
                    // cut-and-paste from Environment.OSVersion so we avoid the demand for EnvironmentPermission. 
                    // there's a side effect here that you can do a timing attack on how long it takes to spin up this
                    // function and figure out whether you're pre- or post-Win2K crypto.  But you could do that 
                    // anyway by looking at what CSPs are on the system.
                    Win32Native.OSVERSIONINFO osvi = new Win32Native.OSVERSIONINFO();
                    bool r = Win32Native.GetVersionEx(osvi);
                    s_win2KCrypto = (r && (osvi.PlatformId == Win32Native.VER_PLATFORM_WIN32_NT) && (osvi.MajorVersion >= 5)) ? 1 : 0; 
                }
                return s_win2KCrypto; 
            } 
        }
 
        internal static string ObjToOidValue (Object hashAlg) {
            if (hashAlg == null)
                throw new ArgumentNullException("hashAlg");
 
            string oidValue = null;
            if (hashAlg is String) { 
                oidValue = CryptoConfig.MapNameToOID((string) hashAlg); 
                if (oidValue == null)
                    oidValue = (string) hashAlg; // maybe we were passed the OID value 
            }
            else if (hashAlg is HashAlgorithm) {
                oidValue = CryptoConfig.MapNameToOID(hashAlg.GetType().ToString());
            } 
            else if (hashAlg is Type) {
                oidValue = CryptoConfig.MapNameToOID(hashAlg.ToString()); 
            } 

            if (oidValue == null) 
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));

            return oidValue;
        } 

        internal static HashAlgorithm ObjToHashAlgorithm (Object hashAlg) { 
            if (hashAlg == null) 
                throw new ArgumentNullException("hashAlg");
 
            HashAlgorithm hash = null;
            if (hashAlg is String) {
                hash = (HashAlgorithm) CryptoConfig.CreateFromName((string) hashAlg);
                if (hash == null) { 
                    string oidFriendlyName = X509Utils._GetFriendlyNameFromOid((string) hashAlg);
                    if (oidFriendlyName != null) 
                        hash = (HashAlgorithm) CryptoConfig.CreateFromName(oidFriendlyName); 
                }
            } 
            else if (hashAlg is HashAlgorithm) {
                hash = (HashAlgorithm) hashAlg;
            }
            else if (hashAlg is Type) { 
                hash = (HashAlgorithm) CryptoConfig.CreateFromName(hashAlg.ToString());
            } 
 
            if (hash == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue")); 

            return hash;
        }
 
        internal static String DiscardWhiteSpaces (string inputBuffer) {
            return DiscardWhiteSpaces(inputBuffer, 0, inputBuffer.Length); 
        } 

        internal static String DiscardWhiteSpaces (string inputBuffer, int inputOffset, int inputCount) { 
            int i, iCount = 0;
            for (i=0; i<inputCount; i++)
                if (Char.IsWhiteSpace(inputBuffer[inputOffset + i])) iCount++;
            char[] output = new char[inputCount - iCount]; 
            iCount = 0;
            for (i=0; i<inputCount; i++) { 
                if (!Char.IsWhiteSpace(inputBuffer[inputOffset + i])) 
                    output[iCount++] = inputBuffer[inputOffset + i];
            } 
            return new String(output);
        }

        internal static int ConvertByteArrayToInt (byte[] input) { 
            // Input to this routine is always big endian
            int dwOutput = 0; 
            for (int i = 0; i < input.Length; i++) { 
                dwOutput *= 256;
                dwOutput += input[i]; 
            }
            return(dwOutput);
        }
 
        // output of this routine is always big endian
        internal static byte[] ConvertIntToByteArray (int dwInput) { 
            byte[] temp = new byte[8]; // int can never be greater than Int64 
            int t1;  // t1 is remaining value to account for
            int t2;  // t2 is t1 % 256 
            int i = 0;

            if (dwInput == 0) return new byte[1];
            t1 = dwInput; 
            while (t1 > 0) {
                BCLDebug.Assert(i < 8, "Got too big an int here!"); 
                t2 = t1 % 256; 
                temp[i] = (byte) t2;
                t1 = (t1 - t2)/256; 
                i++;
            }

            // Now, copy only the non-zero part of temp and reverse 
            byte[] output = new byte[i];
            // copy and reverse in one pass 
            for (int j = 0; j < i; j++) { 
                output[j] = temp[i-j-1];
            } 
            return output;
        }

        // output is little endian 
        internal static void ConvertIntToByteArray (uint dwInput, ref byte[] counter) {
            uint t1 = dwInput;  // t1 is remaining value to account for 
            uint t2;  // t2 is t1 % 256 
            int i = 0;
 
            // clear the array first
            Array.Clear(counter, 0, counter.Length);
            if (dwInput == 0) return;
            while (t1 > 0) { 
                BCLDebug.Assert(i < 4, "Got too big an int here!");
                t2 = t1 % 256; 
                counter[3 - i] = (byte) t2; 
                t1 = (t1 - t2)/256;
                i++; 
            }
        }

        internal static byte[] FixupKeyParity (byte[] key) { 
            byte[] oddParityKey = new byte[key.Length];
            for (int index=0; index < key.Length; index++) { 
                // Get the bits we are interested in 
                oddParityKey[index] = (byte) (key[index] & 0xfe);
                // Get the parity of the sum of the previous bits 
                byte tmp1 = (byte)((oddParityKey[index] & 0xF) ^ (oddParityKey[index] >> 4));
                byte tmp2 = (byte)((tmp1 & 0x3) ^ (tmp1 >> 2));
                byte sumBitsMod2 = (byte)((tmp2 & 0x1) ^ (tmp2 >> 1));
                // We need to set the last bit in oddParityKey[index] to the negation 
                // of the last bit in sumBitsMod2
                if (sumBitsMod2 == 0) 
                    oddParityKey[index] |= 1; 
            }
            return oddParityKey; 
        }

        // digits == number of DWORDs
        internal unsafe static void DWORDFromLittleEndian (uint* x, int digits, byte* block) { 
            int i;
            int j; 
 
            for (i = 0, j = 0; i < digits; i++, j += 4)
                x[i] =  (uint) (block[j] | (block[j+1] << 8) | (block[j+2] << 16) | (block[j+3] << 24)); 
        }

        // encodes x (DWORD) into block (unsigned char), least significant byte first.
        // digits == number of DWORDs 
        internal static void DWORDToLittleEndian (byte[] block, uint[] x, int digits) {
            int i; 
            int j; 

            for (i = 0, j = 0; i < digits; i++, j += 4) { 
                block[j]   = (byte)(x[i] & 0xff);
                block[j+1] = (byte)((x[i] >> 8) & 0xff);
                block[j+2] = (byte)((x[i] >> 16) & 0xff);
                block[j+3] = (byte)((x[i] >> 24) & 0xff); 
            }
        } 
 
        // digits == number of DWORDs
        internal unsafe static void DWORDFromBigEndian (uint* x, int digits, byte* block) { 
            int i;
            int j;

            for (i = 0, j = 0; i < digits; i++, j += 4) 
                x[i] = (uint)((block[j] << 24) | (block[j + 1] << 16) | (block[j + 2] << 8) | block[j + 3]);
        } 
 
        // encodes x (DWORD) into block (unsigned char), most significant byte first.
        // digits == number of DWORDs 
        internal static void DWORDToBigEndian (byte[] block, uint[] x, int digits) {
            int i;
            int j;
 
            for (i = 0, j = 0; i < digits; i++, j += 4) {
                block[j] = (byte)((x[i] >> 24) & 0xff); 
                block[j+1] = (byte)((x[i] >> 16) & 0xff); 
                block[j+2] = (byte)((x[i] >> 8) & 0xff);
                block[j+3] = (byte)(x[i] & 0xff); 
            }
        }

        // digits == number of QWORDs 
        internal unsafe static void QuadWordFromBigEndian (UInt64* x, int digits, byte* block) {
            int i; 
            int j; 

            for (i = 0, j = 0; i < digits; i++, j += 8) 
                x[i] =  (
                         (((UInt64)block[j]) << 56) | (((UInt64)block[j+1]) << 48) |
                         (((UInt64)block[j+2]) << 40) | (((UInt64)block[j+3]) << 32) |
                         (((UInt64)block[j+4]) << 24) | (((UInt64)block[j+5]) << 16) | 
                         (((UInt64)block[j+6]) << 8) | ((UInt64)block[j+7])
                        ); 
        } 

        // encodes x (DWORD) into block (unsigned char), most significant byte first. 
        // digits = number of QWORDS
        internal static void QuadWordToBigEndian (byte[] block, UInt64[] x, int digits) {
            int i;
            int j; 

            for (i = 0, j = 0; i < digits; i++, j += 8) { 
                block[j] = (byte)((x[i] >> 56) & 0xff); 
                block[j+1] = (byte)((x[i] >> 48) & 0xff);
                block[j+2] = (byte)((x[i] >> 40) & 0xff); 
                block[j+3] = (byte)((x[i] >> 32) & 0xff);
                block[j+4] = (byte)((x[i] >> 24) & 0xff);
                block[j+5] = (byte)((x[i] >> 16) & 0xff);
                block[j+6] = (byte)((x[i] >> 8) & 0xff); 
                block[j+7] = (byte)(x[i] & 0xff);
            } 
        } 

        // encodes the integer i into a 4-byte array, in big endian. 
        internal static byte[] Int(uint i) {
            byte[] b = BitConverter.GetBytes(i);
            byte[] littleEndianBytes = {b[3], b[2], b[1], b[0]};
            return BitConverter.IsLittleEndian ? littleEndianBytes : b; 
        }
 
        internal unsafe static void BlockCopy (byte* src, int srcOffset, byte* dst, int dstOffset, int count) { 
            for (int i = 0; i < count; i++) {
                dst[dstOffset + i] = src[srcOffset + i]; 
            }
        }

        internal unsafe static void BlockCopy (byte[] src, int srcOffset, int* dst, int dstOffset, int count) { 
            fixed (byte* _src = src) {
                BlockCopy(_src, srcOffset, (byte*)dst, dstOffset, count); 
            } 
        }
 
        internal unsafe static void BlockCopy (int* src, int srcOffset, int[] dst, int dstOffset, int count) {
            fixed (int* _dst = &dst[dstOffset]) {
                BlockCopy((byte*)&src[srcOffset], srcOffset, (byte*)_dst, 0, count);
            } 
        }
 
        internal static byte[] RsaOaepEncrypt (RSA rsa, HashAlgorithm hash, PKCS1MaskGenerationMethod mgf, RandomNumberGenerator rng, byte[] data) { 
            int cb = rsa.KeySize / 8;
 
            //  1. Hash the parameters to get PHash
            int cbHash = hash.HashSize / 8;
            if ((data.Length + 2 + 2*cbHash) > cb)
                throw new CryptographicException(String.Format(null, Environment.GetResourceString("Cryptography_Padding_EncDataTooBig"), cb-2-2*cbHash)); 
            hash.ComputeHash(new byte[0]); // Use an empty octet string
 
            //  2.  Create DB object 
            byte[] DB = new byte[cb - cbHash];
 
            //  Structure is as follows:
            //      pHash || PS || 01 || M
            //      PS consists of all zeros
 
            Buffer.InternalBlockCopy(hash.Hash, 0, DB, 0, cbHash);
            DB[DB.Length - data.Length - 1] = 1; 
            Buffer.InternalBlockCopy(data, 0, DB, DB.Length-data.Length, data.Length); 

            // 3. Create a random value of size hLen 
            byte[] seed = new byte[cbHash];
            rng.GetBytes(seed);

            // 4.  Compute the mask value 
            byte[] mask = mgf.GenerateMask(seed, DB.Length);
 
            // 5.  Xor maskDB into DB 
            for (int i=0; i < DB.Length; i++) {
                DB[i] = (byte) (DB[i] ^ mask[i]); 
            }

            // 6.  Compute seed mask value
            mask = mgf.GenerateMask(DB, cbHash); 

            // 7.  Xor mask into seed 
            for (int i=0; i < seed.Length; i++) { 
                seed[i] ^= mask[i];
            } 

            // 8. Concatenate seed and DB to form value to encrypt
            byte[] pad = new byte[cb];
            Buffer.InternalBlockCopy(seed, 0, pad, 0, seed.Length); 
            Buffer.InternalBlockCopy(DB, 0, pad, seed.Length, DB.Length);
 
            return rsa.EncryptValue(pad); 
        }
 
        internal static byte[] RsaOaepDecrypt (RSA rsa, HashAlgorithm hash, PKCS1MaskGenerationMethod mgf, byte[] encryptedData) {
            int cb = rsa.KeySize / 8;

            // 1. Decode the input data 
            // It is important that the Integer to Octet String conversion errors be indistinguishable from the other decoding
            // errors to protect against chosen cipher text attacks 
            // A lecture given by James Manger during Crypto 2001 explains the issue in details 
            byte[] data = null;
            try { 
                data = rsa.DecryptValue(encryptedData);
            }
            catch (CryptographicException) {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding")); 
            }
 
            // 2. Create the hash object so we can get its size info. 
            int cbHash = hash.HashSize / 8;
 
            //  3.  Let maskedSeed be the first hLen octects and maskedDB
            //      be the remaining bytes.
            int zeros = cb - data.Length;
            if (zeros < 0 || zeros >= cbHash) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
 
            byte[] seed = new byte[cbHash]; 
            Buffer.InternalBlockCopy(data, 0, seed, zeros, seed.Length - zeros);
 
            byte[] DB = new byte[data.Length - seed.Length + zeros];
            Buffer.InternalBlockCopy(data, seed.Length - zeros, DB, 0, DB.Length);

            //  4.  seedMask = MGF(maskedDB, hLen); 
            byte[] mask = mgf.GenerateMask(DB, seed.Length);
 
            //  5.  seed = seedMask XOR maskedSeed 
            int i = 0;
            for (i=0; i < seed.Length; i++) { 
                seed[i] ^= mask[i];
            }

            //  6.  dbMask = MGF(seed, |EM| - hLen); 
            mask = mgf.GenerateMask(seed, DB.Length);
 
            //  7.  DB = maskedDB xor dbMask 
            for (i=0; i < DB.Length; i++) {
                DB[i] = (byte) (DB[i] ^ mask[i]); 
            }

            //  8.  pHash = HASH(P)
            hash.ComputeHash(new byte[0]); 

            //  9.  DB = pHash' || PS || 01 || M 
            //  10.  Check that pHash = pHash' 

            byte[] hashValue = hash.Hash; 
            for (i=0; i < cbHash; i++) {
                if (DB[i] != hashValue[i])
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
            } 

            //  Check that PS is all zeros 
            for (; i<DB.Length; i++) { 
                if (DB[i] == 1)
                    break; 
                else if (DB[i] != 0)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
            }
 
            if (i == DB.Length)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding")); 
 
            i++; // skip over the one
 
            //  11. Output M.
            byte[] output = new byte[DB.Length - i];
            Buffer.InternalBlockCopy(DB, i, output, 0, output.Length);
            return output; 
        }
 
        internal static byte[] RsaPkcs1Padding (RSA rsa, byte[] oid, byte[] hash) { 
            int cb = rsa.KeySize/8;
            byte[] pad = new byte[cb]; 

            //
            //  We want to pad this to the following format:
            // 
            //  00 || 01 || FF ... FF || 00 || prefix || Data
            // 
            // We want basically to ASN 1 encode the OID + hash: 
            // STRUCTURE {
            //  STRUCTURE { 
            //    OID <hash algorithm OID>
            //    NULL (0x05 0x00)  // this is actually an ANY and contains the parameters of the algorithm specified by the OID, I think
            //  }
            //  OCTET STRING <hashvalue> 
            // }
            // 
 
            // Get the correct prefix
            byte[] data = new byte[oid.Length + 8 + hash.Length]; 
            data[0] = 0x30; // a structure follows
            int tmp = data.Length - 2;
            data[1] = (byte) tmp;
            data[2] = 0x30; 
            tmp = oid.Length + 2;
            data[3] = (byte) tmp; 
            Buffer.InternalBlockCopy(oid, 0, data, 4, oid.Length); 
            data[4 + oid.Length] = 0x05;
            data[4 + oid.Length + 1] = 0x00; 
            data[4 + oid.Length + 2] = 0x04; // an octet string follows
            data[4 + oid.Length + 3] = (byte) hash.Length;
            Buffer.InternalBlockCopy(hash, 0, data, oid.Length + 8, hash.Length);
 
            // Construct the whole array
            int cb1 = cb - data.Length; 
            if (cb1 <= 2) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOID"));
 
            pad[0] = 0;
            pad[1] = 1;
            for (int i=2; i<cb1-1; i++) {
                pad[i] = 0xff; 
            }
            pad[cb1-1] = 0; 
            Buffer.InternalBlockCopy(data, 0, pad, cb1, data.Length); 
            return pad;
        } 

        // This routine compares 2 big ints; ignoring any leading zeros
        internal static bool CompareBigIntArrays (byte[] lhs, byte[] rhs) {
            if (lhs == null) 
                return (rhs == null);
 
            int i = 0, j = 0; 
            while (i < lhs.Length && lhs[i] == 0) i++;
            while (j < rhs.Length && rhs[j] == 0) j++; 

            int count = (lhs.Length - i);
            if ((rhs.Length - j) != count)
                return false; 

            for (int k = 0; k < count; k++) { 
                if (lhs[i + k] != rhs[j + k]) 
                    return false;
            } 
            return true;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _AcquireCSP(CspParameters param, ref SafeProvHandle hProv);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _CreateCSP(CspParameters param, bool randomKeyContainer, ref SafeProvHandle hProv); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _CreateHash(SafeProvHandle hProv, int algid, ref SafeHashHandle hKey); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _CryptDeriveKey(SafeProvHandle hProv, int algid, int algidHash, byte[] password, int dwFlags, byte[] IV);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int _DecryptData(SafeKeyHandle hKey, byte[] data, int ib, int cb, ref byte[] outputBuffer, int outputOffset, PaddingMode PaddingMode, bool fDone); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _DecryptKey(SafeKeyHandle hPubKey, byte[] key, int dwFlags); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _DecryptPKWin2KEnh(SafeKeyHandle hPubKey, byte[] key, bool fOAEP, out int hr);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern int _EncryptData(SafeKeyHandle hKey, byte[] data, int ib, int cb, ref byte[] outputBuffer, int outputOffset, PaddingMode PaddingMode, bool fDone);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _EncryptKey(SafeKeyHandle hPubKey, byte[] key);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _EncryptPKWin2KEnh(SafeKeyHandle hPubKey, byte[] key, bool fOAEP, out int hr);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _EndHash(SafeHashHandle hHash); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _ExportCspBlob(SafeKeyHandle hKey, int blobType); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _ExportKey(SafeKeyHandle hKey, int blobType, object cspObject);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _GenerateKey(SafeProvHandle hProv, int algid, CspProviderFlags flags, int keySize, ref SafeKeyHandle hKey); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _GetBytes(SafeProvHandle hProv, byte[] randomBytes); 
        [MethodImpl(MethodImplOptions.InternalCall)] 
        private static extern bool _GetEnforceFipsPolicySetting();
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _GetKeyParameter(SafeKeyHandle hKey, uint paramID);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetKeySetSecurityInfo(SafeProvHandle hProv, SecurityInfos securityInfo, out int error);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _GetNonZeroBytes(SafeProvHandle hProv, byte[] randomBytes);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern bool _GetPersistKeyInCsp(SafeProvHandle hProv); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object _GetProviderParameter(SafeProvHandle hProv, int keyNumber, uint paramID); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetRandomKeyContainer();
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int _GetUserKey(SafeProvHandle hProv, int keyNumber, ref SafeKeyHandle hKey); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _HashData(SafeHashHandle hHash, byte[] data, int ibStart, int cbSize); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _ImportBulkKey(SafeProvHandle hProv, int algid, bool useSalt, byte[] key, ref SafeKeyHandle hKey);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern int _ImportCspBlob(byte[] keyBlob, SafeProvHandle hProv, CspProviderFlags flags, ref SafeKeyHandle hKey);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _ImportKey(SafeProvHandle hCSP, int keyNumber, CspProviderFlags flags, object cspObject, ref SafeKeyHandle hKey);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern int _OpenCSP(CspParameters param, uint flags, ref SafeProvHandle hProv);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern bool _ProduceLegacyHmacValues(); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool _SearchForAlgorithm(SafeProvHandle hProv, int algID, int keyLength); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _SetKeyParamDw(SafeKeyHandle hKey, int param, int dwValue);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _SetKeyParamRgb(SafeKeyHandle hKey, int param, byte[] value); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int _SetKeySetSecurityInfo (SafeProvHandle hProv, SecurityInfos securityInfo, byte[] sd); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _SetPersistKeyInCsp(SafeProvHandle hProv, bool fPersistKeyInCsp);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _SetProviderParameter(SafeProvHandle hProv, int keyNumber, uint paramID, IntPtr pbData);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _ShowLegacyHmacWarning();
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _SignValue(SafeKeyHandle hKey, int keyNumber, int calgKey, int calgHash, byte[] hash, int dwFlags);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern bool _VerifySign(SafeKeyHandle hKey, int calgKey, int calgHash, byte[] hash, byte[] signature, int dwFlags); 
    }
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// Utils.cs 
//
// 05/01/2002 
//

namespace System.Security.Cryptography
{ 
    using Microsoft.Win32;
    using System.IO; 
    using System.Globalization; 
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices; 
    using System.Security.AccessControl;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Permissions;
    using System.Security.Principal; 
    using System.Text;
    using System.Threading; 
    using System.Runtime.Versioning; 

    [Serializable] 
    internal enum CspAlgorithmType {
        Rsa = 0,
        Dss = 1
    } 

    internal static class Constants { 
        internal const int S_OK                   = 0; 
        internal const int NTE_NO_KEY             = unchecked((int) 0x8009000D); // Key does not exist.
        internal const int NTE_BAD_KEYSET         = unchecked((int) 0x80090016); // Keyset does not exist. 
        internal const int NTE_KEYSET_NOT_DEF     = unchecked((int) 0x80090019); // The keyset is not defined.

        internal const int KP_IV                  = 1;
        internal const int KP_MODE                = 4; 
        internal const int KP_MODE_BITS           = 5;
        internal const int KP_EFFECTIVE_KEYLEN    = 19; 
 
        internal const int ALG_CLASS_SIGNATURE    = (1 << 13);
        internal const int ALG_CLASS_DATA_ENCRYPT = (3 << 13); 
        internal const int ALG_CLASS_HASH         = (4 << 13);
        internal const int ALG_CLASS_KEY_EXCHANGE = (5 << 13);

        internal const int ALG_TYPE_DSS           = (1 << 9); 
        internal const int ALG_TYPE_RSA           = (2 << 9);
        internal const int ALG_TYPE_BLOCK         = (3 << 9); 
        internal const int ALG_TYPE_STREAM        = (4 << 9); 
        internal const int ALG_TYPE_ANY           = (0);
 
        internal const int CALG_MD5               = (ALG_CLASS_HASH | ALG_TYPE_ANY | 3);
        internal const int CALG_SHA1              = (ALG_CLASS_HASH | ALG_TYPE_ANY | 4);
        internal const int CALG_RSA_KEYX          = (ALG_CLASS_KEY_EXCHANGE | ALG_TYPE_RSA | 0);
        internal const int CALG_RSA_SIGN          = (ALG_CLASS_SIGNATURE | ALG_TYPE_RSA | 0); 
        internal const int CALG_DSS_SIGN          = (ALG_CLASS_SIGNATURE | ALG_TYPE_DSS | 0);
        internal const int CALG_DES               = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 1); 
        internal const int CALG_RC2               = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 2); 
        internal const int CALG_3DES              = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 3);
        internal const int CALG_3DES_112          = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 9); 
        internal const int CALG_AES_128           = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 14);
        internal const int CALG_AES_192           = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 15);
        internal const int CALG_AES_256           = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 16);
        internal const int CALG_RC4               = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_STREAM | 1); 

        internal const int PROV_RSA_FULL          = 1; 
        internal const int PROV_DSS_DH            = 13; 

        internal const int AT_KEYEXCHANGE         = 1; 
        internal const int AT_SIGNATURE           = 2;
        internal const int PUBLICKEYBLOB          = 0x6;
        internal const int PRIVATEKEYBLOB         = 0x7;
        internal const int CRYPT_OAEP             = 0x40; 

 
        internal const uint CRYPT_VERIFYCONTEXT    = 0xF0000000; 
        internal const uint CRYPT_NEWKEYSET        = 0x00000008;
        internal const uint CRYPT_DELETEKEYSET     = 0x00000010; 
        internal const uint CRYPT_MACHINE_KEYSET   = 0x00000020;
        internal const uint CRYPT_SILENT           = 0x00000040;
        internal const uint CRYPT_EXPORTABLE       = 0x00000001;
 
        internal const uint CLR_KEYLEN            = 1;
        internal const uint CLR_PUBLICKEYONLY     = 2; 
        internal const uint CLR_EXPORTABLE        = 3; 
        internal const uint CLR_REMOVABLE         = 4;
        internal const uint CLR_HARDWARE          = 5; 
        internal const uint CLR_ACCESSIBLE        = 6;
        internal const uint CLR_PROTECTED         = 7;
        internal const uint CLR_UNIQUE_CONTAINER  = 8;
        internal const uint CLR_ALGID             = 9; 
        internal const uint CLR_PP_CLIENT_HWND    = 10;
        internal const uint CLR_PP_PIN            = 11; 
 
        internal const string OID_RSA_SMIMEalgCMS3DESwrap   = "1.2.840.113549.1.9.16.3.6";
        internal const string OID_RSA_MD5                   = "1.2.840.113549.2.5"; 
        internal const string OID_RSA_RC2CBC                = "1.2.840.113549.3.2";
        internal const string OID_RSA_DES_EDE3_CBC          = "1.2.840.113549.3.7";
        internal const string OID_OIWSEC_desCBC             = "1.3.14.3.2.7";
        internal const string OID_OIWSEC_SHA1               = "1.3.14.3.2.26"; 
        internal const string OID_OIWSEC_SHA256             = "2.16.840.1.101.3.4.2.1";
        internal const string OID_OIWSEC_SHA384             = "2.16.840.1.101.3.4.2.2"; 
        internal const string OID_OIWSEC_SHA512             = "2.16.840.1.101.3.4.2.3"; 
        internal const string OID_OIWSEC_RIPEMD160          = "1.3.36.3.2.1";
    } 

    internal static class Utils {

        // Private object for locking instead of locking on a public type for SQL reliability work. 
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
 
        private static SafeProvHandle _safeProvHandle = null; 
        internal static SafeProvHandle StaticProvHandle {
            get { 
                if (_safeProvHandle == null) {
                    lock (InternalSyncObject) {
                        if (_safeProvHandle == null) {
                            SafeProvHandle safeProvHandle = AcquireProvHandle(new CspParameters(Constants.PROV_RSA_FULL)); 
                            Thread.MemoryBarrier();
                            _safeProvHandle = safeProvHandle; 
                        } 
                    }
                } 
                return _safeProvHandle;
            }
        }
 
        private static SafeProvHandle _safeDssProvHandle = null;
        internal static SafeProvHandle StaticDssProvHandle { 
            get { 
                if (_safeDssProvHandle == null) {
                    lock (InternalSyncObject) { 
                        if (_safeDssProvHandle == null) {
                            SafeProvHandle safeProvHandle = AcquireProvHandle(new CspParameters(Constants.PROV_DSS_DH));
                            Thread.MemoryBarrier();
                            _safeDssProvHandle = safeProvHandle; 
                        }
                    } 
                } 
                return _safeDssProvHandle;
            } 
        }

        internal static SafeProvHandle AcquireProvHandle (CspParameters parameters) {
            if (parameters == null) 
                parameters = new CspParameters(Constants.PROV_RSA_FULL);
 
            SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle; 
            if (Utils.Win2KCrypto == 1) {
                Utils._AcquireCSP(parameters, ref safeProvHandle); // CRYPT_VERIFYCONTEXT is only supported in Win2K 
            } else {
                // Using the default key container is a bad idea since this might lead to races
                // if multiple threads are trying to access, delete or generate keys inside the container.
                if (parameters.KeyContainerName == null && 
                    (parameters.Flags & CspProviderFlags.UseDefaultKeyContainer) == 0)
                    parameters.KeyContainerName = _GetRandomKeyContainer(); 
                safeProvHandle = CreateProvHandle(parameters, true); 
            }
 
            return safeProvHandle;
        }

        internal static SafeProvHandle CreateProvHandle (CspParameters parameters, bool randomKeyContainer) { 
            SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
            int hr = Utils._OpenCSP(parameters, 0, ref safeProvHandle); 
            KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags); 
            if (hr != Constants.S_OK) {
                // If UseExistingKey flag is used and the key container does not exist 
                // throw an exception without attempting to create the container.
                if ((parameters.Flags & CspProviderFlags.UseExistingKey) != 0 || (hr != Constants.NTE_KEYSET_NOT_DEF && hr != Constants.NTE_BAD_KEYSET))
                    throw new CryptographicException(hr);
                if (!randomKeyContainer) { 
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Create);
                    kp.AccessEntries.Add(entry); 
                    kp.Demand(); 
                }
                Utils._CreateCSP(parameters, randomKeyContainer, ref safeProvHandle); 
            } else {
                if (!randomKeyContainer) {
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Open);
                    kp.AccessEntries.Add(entry); 
                    kp.Demand();
                } 
            } 
            return safeProvHandle;
        } 

        internal static CryptoKeySecurity GetKeySetSecurityInfo (SafeProvHandle hProv, AccessControlSections accessControlSections) {
            if (Utils.Win2KCrypto != 1)
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_RequiresNT")); 

            SecurityInfos securityInfo = 0; 
            Privilege privilege = null; 

            if ((accessControlSections & AccessControlSections.Owner) != 0) 
                securityInfo |= SecurityInfos.Owner;
            if ((accessControlSections & AccessControlSections.Group) != 0)
                securityInfo |= SecurityInfos.Group;
            if ((accessControlSections & AccessControlSections.Access) != 0) 
                securityInfo |= SecurityInfos.DiscretionaryAcl;
 
            byte[] rawSecurityDescriptor = null; 
            int error;
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                if ((accessControlSections & AccessControlSections.Audit) != 0) {
                    securityInfo |= SecurityInfos.SystemAcl; 
                    privilege = new Privilege("SeSecurityPrivilege");
                    privilege.Enable(); 
                } 
                rawSecurityDescriptor = _GetKeySetSecurityInfo(hProv, securityInfo, out error);
            } 
            finally {
                if (privilege != null)
                    privilege.Revert();
            } 

            // This means that the object doesn't have a security descriptor. And thus we throw 
            // a specific exception for the caller to catch and handle properly. 
            if (error == Win32Native.ERROR_SUCCESS && (rawSecurityDescriptor == null || rawSecurityDescriptor.Length == 0))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoSecurityDescriptor")); 
            if (error == Win32Native.ERROR_NOT_ENOUGH_MEMORY)
                throw new OutOfMemoryException();
            if (error == Win32Native.ERROR_ACCESS_DENIED)
                throw new UnauthorizedAccessException(); 
            if (error == Win32Native.ERROR_PRIVILEGE_NOT_HELD)
                throw new PrivilegeNotHeldException( "SeSecurityPrivilege" ); 
            if (error != Win32Native.ERROR_SUCCESS) 
                throw new CryptographicException(error);
 
            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false /* isContainer */,
                                                                       false /* isDS */,
                                                                       new RawSecurityDescriptor(rawSecurityDescriptor, 0),
                                                                       true); 
            return new CryptoKeySecurity(sd);
        } 
 
        internal static void SetKeySetSecurityInfo (SafeProvHandle hProv, CryptoKeySecurity cryptoKeySecurity, AccessControlSections accessControlSections) {
            if (Utils.Win2KCrypto != 1) 
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_RequiresNT"));

            SecurityInfos securityInfo = 0;
            Privilege privilege = null; 

            if ((accessControlSections & AccessControlSections.Owner) != 0 && cryptoKeySecurity._securityDescriptor.Owner != null) 
                securityInfo |= SecurityInfos.Owner; 
            if ((accessControlSections & AccessControlSections.Group) != 0 && cryptoKeySecurity._securityDescriptor.Group != null)
                securityInfo |= SecurityInfos.Group; 
            if ((accessControlSections & AccessControlSections.Audit) != 0)
                securityInfo |= SecurityInfos.SystemAcl;
            if ((accessControlSections & AccessControlSections.Access) != 0 && cryptoKeySecurity._securityDescriptor.IsDiscretionaryAclPresent)
                securityInfo |= SecurityInfos.DiscretionaryAcl; 

            if (securityInfo == 0) { 
                // Nothing to persist 
                return;
            } 

            int error = 0;

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                if ((securityInfo & SecurityInfos.SystemAcl) != 0) { 
                    privilege = new Privilege("SeSecurityPrivilege"); 
                    privilege.Enable();
                } 

                byte[] sd = cryptoKeySecurity.GetSecurityDescriptorBinaryForm();
                if (sd != null && sd.Length > 0)
                    error = _SetKeySetSecurityInfo (hProv, securityInfo, sd); 
            }
            finally { 
                if (privilege != null) 
                    privilege.Revert();
            } 

            if (error == Win32Native.ERROR_ACCESS_DENIED || error == Win32Native.ERROR_INVALID_OWNER || error == Win32Native.ERROR_INVALID_PRIMARY_GROUP)
                throw new UnauthorizedAccessException();
            else if (error == Win32Native.ERROR_PRIVILEGE_NOT_HELD) 
                throw new PrivilegeNotHeldException("SeSecurityPrivilege");
            else if (error == Win32Native.ERROR_INVALID_HANDLE) 
                throw new NotSupportedException(Environment.GetResourceString("AccessControl_InvalidHandle")); 
            else if (error != Win32Native.ERROR_SUCCESS)
                throw new CryptographicException(error); 
        }

        internal static byte[] ExportCspBlobHelper (bool includePrivateParameters, CspParameters parameters, SafeKeyHandle safeKeyHandle) {
            if (includePrivateParameters) { 
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Export); 
                kp.AccessEntries.Add(entry); 
                kp.Demand();
            } 
            return Utils._ExportCspBlob(safeKeyHandle, includePrivateParameters ? Constants.PRIVATEKEYBLOB : Constants.PUBLICKEYBLOB);
        }

        internal static void GetKeyPairHelper (CspAlgorithmType keyType, CspParameters parameters, bool randomKeyContainer, int dwKeySize, ref SafeProvHandle safeProvHandle, ref SafeKeyHandle safeKeyHandle) { 
            SafeProvHandle TempFetchedProvHandle = Utils.CreateProvHandle(parameters, randomKeyContainer);
 
            // If the user wanted to set the security descriptor on the provider context, apply it now. 
            if (parameters.CryptoKeySecurity != null) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags); 
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.ChangeAcl);
                kp.AccessEntries.Add(entry);
                kp.Demand();
                SetKeySetSecurityInfo(TempFetchedProvHandle, parameters.CryptoKeySecurity, AccessControlSections.All); 
            }
 
            // If the user wanted to specify a PIN or HWND for a smart card CSP, apply those settings now. 
            if (parameters.ParentWindowHandle != IntPtr.Zero)
                _SetProviderParameter(TempFetchedProvHandle, parameters.KeyNumber, Constants.CLR_PP_CLIENT_HWND, parameters.ParentWindowHandle); 
            else if (parameters.KeyPassword != null) {
                IntPtr szPassword = Marshal.SecureStringToCoTaskMemAnsi(parameters.KeyPassword);
                try {
                    _SetProviderParameter(TempFetchedProvHandle, parameters.KeyNumber, Constants.CLR_PP_PIN, szPassword); 
                }
                finally { 
                    if (szPassword != IntPtr.Zero) 
                        Marshal.ZeroFreeCoTaskMemAnsi(szPassword);
                } 
            }

            safeProvHandle = TempFetchedProvHandle;
 
            // If the key already exists, use it, else generate a new one
            SafeKeyHandle TempFetchedKeyHandle = SafeKeyHandle.InvalidHandle; 
            int hr = Utils._GetUserKey(safeProvHandle, parameters.KeyNumber, ref TempFetchedKeyHandle); 
            if (hr != Constants.S_OK) {
                if ((parameters.Flags & CspProviderFlags.UseExistingKey) != 0 || hr != Constants.NTE_NO_KEY) 
                    throw new CryptographicException(hr);
                // _GenerateKey will check for failures and throw an exception
                Utils._GenerateKey(safeProvHandle, parameters.KeyNumber, parameters.Flags, dwKeySize, ref TempFetchedKeyHandle);
            } 

            // check that this is indeed an RSA/DSS key. 
            byte[] algid = (byte[]) Utils._GetKeyParameter(TempFetchedKeyHandle, Constants.CLR_ALGID); 
            int dwAlgId = (algid[0] | (algid[1] << 8) | (algid[2] << 16) | (algid[3] << 24));
            if ((keyType == CspAlgorithmType.Rsa && dwAlgId != Constants.CALG_RSA_KEYX && dwAlgId != Constants.CALG_RSA_SIGN) || 
                    (keyType == CspAlgorithmType.Dss && dwAlgId != Constants.CALG_DSS_SIGN)) {
                TempFetchedKeyHandle.Dispose();
                throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_WrongKeySpec"));
            } 

            safeKeyHandle = TempFetchedKeyHandle; 
        } 

        internal static void ImportCspBlobHelper (CspAlgorithmType keyType, byte[] keyBlob, bool publicOnly, ref CspParameters parameters, bool randomKeyContainer, ref SafeProvHandle safeProvHandle, ref SafeKeyHandle safeKeyHandle) { 
            // Free the current key handle
            if (safeKeyHandle != null && !safeKeyHandle.IsClosed)
                safeKeyHandle.Dispose();
            safeKeyHandle = SafeKeyHandle.InvalidHandle; 

            if (publicOnly) { 
                parameters.KeyNumber = Utils._ImportCspBlob(keyBlob, keyType == CspAlgorithmType.Dss ? Utils.StaticDssProvHandle : Utils.StaticProvHandle, (CspProviderFlags) 0, ref safeKeyHandle); 
            } else {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags); 
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Import);
                kp.AccessEntries.Add(entry);
                kp.Demand();
                if (safeProvHandle == null) 
                    safeProvHandle = Utils.CreateProvHandle(parameters, randomKeyContainer);
                parameters.KeyNumber = Utils._ImportCspBlob(keyBlob, safeProvHandle, parameters.Flags, ref safeKeyHandle); 
            } 
        }
 
        internal static CspParameters SaveCspParameters (CspAlgorithmType keyType, CspParameters userParameters, CspProviderFlags defaultFlags, ref bool randomKeyContainer) {
            CspParameters parameters;
            if (userParameters == null) {
                parameters = new CspParameters(keyType == CspAlgorithmType.Dss ? Constants.PROV_DSS_DH : Constants.PROV_RSA_FULL, null, null, defaultFlags); 
            } else {
                ValidateCspFlags(userParameters.Flags); 
                parameters = new CspParameters(userParameters); 
            }
 
            if (parameters.KeyNumber == -1)
                parameters.KeyNumber = keyType == CspAlgorithmType.Dss ? Constants.AT_SIGNATURE : Constants.AT_KEYEXCHANGE;
            else if (parameters.KeyNumber == Constants.CALG_DSS_SIGN || parameters.KeyNumber == Constants.CALG_RSA_SIGN)
                parameters.KeyNumber = Constants.AT_SIGNATURE; 
            else if (parameters.KeyNumber == Constants.CALG_RSA_KEYX)
                parameters.KeyNumber = Constants.AT_KEYEXCHANGE; 
 
            // If no key container was specified and UseDefaultKeyContainer is not used,
            // generate a random key container name 
            randomKeyContainer = false;
            if (parameters.KeyContainerName == null &&
                (parameters.Flags & CspProviderFlags.UseDefaultKeyContainer) == 0) {
                parameters.KeyContainerName = Utils._GetRandomKeyContainer(); 
                randomKeyContainer = true;
            } 
 
            return parameters;
        } 

        private static void ValidateCspFlags (CspProviderFlags flags) {
            // check that the flags are consistent.
            if ((flags & CspProviderFlags.UseExistingKey) != 0) { 
                CspProviderFlags keyFlags = (CspProviderFlags.UseNonExportableKey | CspProviderFlags.UseArchivableKey | CspProviderFlags.UseUserProtectedKey);
                if ((flags & keyFlags) != CspProviderFlags.NoFlags) 
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag")); 
            }
 
            // make sure we are allowed to display the key protection UI if a user protected key is requested.
            if ((flags & CspProviderFlags.UseUserProtectedKey) != 0) {
                // UI only allowed in interactive session.
                if (!System.Environment.UserInteractive) 
                    throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NotInteractive"));
 
                // we need to demand UI permission here. 
                UIPermission uiPermission = new UIPermission(UIPermissionWindow.SafeTopLevelWindows);
                uiPermission.Demand(); 
            }
        }

        private static RNGCryptoServiceProvider _rng = null; 
        internal static RNGCryptoServiceProvider StaticRandomNumberGenerator {
            get { 
                if (_rng == null) 
                    _rng = new RNGCryptoServiceProvider();
                return _rng; 
            }
        }

        // 
        // internal static methods
        // 
 
        internal static byte[] GenerateRandom (int keySize) {
            byte[] key = new byte[keySize]; 
            StaticRandomNumberGenerator.GetBytes(key);
            return key;
        }
 
        // dwKeySize = 0 means the default key size
        internal static bool HasAlgorithm (int dwCalg, int dwKeySize) { 
            bool r = false; 
            // We need to take a lock here since we are querying the provider handle in a loop.
            // If multiple threads are racing in this code, not all algorithms/key sizes combinations 
            // will be examined; which may lead to a situation where false is wrongfully returned.
            lock (InternalSyncObject) {
                r = _SearchForAlgorithm(StaticProvHandle, dwCalg, dwKeySize);
            } 
            return r;
        } 
 
        private static int s_hasEnhProv = -1;
        internal static int HasEnhProv { 
            get {
                if (s_hasEnhProv == -1)
                    s_hasEnhProv = Utils.HasAlgorithm(Constants.CALG_RSA_KEYX, 2048) ? 1 : 0;
 
                return s_hasEnhProv;
            } 
        } 

        private static int s_fipsAlgorithmPolicy = -1; 
        internal static int FipsAlgorithmPolicy {
            [RegistryPermissionAttribute(SecurityAction.Assert, Unrestricted=true)]
            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)] 
            get {
                if (s_fipsAlgorithmPolicy == -1) { 
                    if (!_GetEnforceFipsPolicySetting()) { 
                        s_fipsAlgorithmPolicy = 0;
                    } 
                    else if (Environment.OSVersion.Version.Major >= 6) {
                        bool fipsEnabled;
                        uint policyReadStatus = Win32Native.BCryptGetFipsAlgorithmMode(out fipsEnabled);
 
                        bool readPolicy = policyReadStatus == Win32Native.STATUS_SUCCESS ||
                                          policyReadStatus == Win32Native.STATUS_OBJECT_NAME_NOT_FOUND; 
 
                        if (!readPolicy || fipsEnabled) {
                            s_fipsAlgorithmPolicy = 1; 
                        }
                        else {
                            s_fipsAlgorithmPolicy = 0;
                        } 
                    }
                    else 
                    { 
                        using (RegistryKey fipsAlgorithmPolicyKey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Lsa", false)) {
                            if (fipsAlgorithmPolicyKey != null) { 
                                object data = fipsAlgorithmPolicyKey.GetValue("FIPSAlgorithmPolicy");
                                if (data != null)
                                    s_fipsAlgorithmPolicy = (int) data;
                            } 
                        }
                    } 
                } 
                return s_fipsAlgorithmPolicy;
            } 
        }

        private static int s_win2KCrypto = -1;
        internal static int Win2KCrypto { 
            get {
                if (s_win2KCrypto == -1) { 
                    // cut-and-paste from Environment.OSVersion so we avoid the demand for EnvironmentPermission. 
                    // there's a side effect here that you can do a timing attack on how long it takes to spin up this
                    // function and figure out whether you're pre- or post-Win2K crypto.  But you could do that 
                    // anyway by looking at what CSPs are on the system.
                    Win32Native.OSVERSIONINFO osvi = new Win32Native.OSVERSIONINFO();
                    bool r = Win32Native.GetVersionEx(osvi);
                    s_win2KCrypto = (r && (osvi.PlatformId == Win32Native.VER_PLATFORM_WIN32_NT) && (osvi.MajorVersion >= 5)) ? 1 : 0; 
                }
                return s_win2KCrypto; 
            } 
        }
 
        internal static string ObjToOidValue (Object hashAlg) {
            if (hashAlg == null)
                throw new ArgumentNullException("hashAlg");
 
            string oidValue = null;
            if (hashAlg is String) { 
                oidValue = CryptoConfig.MapNameToOID((string) hashAlg); 
                if (oidValue == null)
                    oidValue = (string) hashAlg; // maybe we were passed the OID value 
            }
            else if (hashAlg is HashAlgorithm) {
                oidValue = CryptoConfig.MapNameToOID(hashAlg.GetType().ToString());
            } 
            else if (hashAlg is Type) {
                oidValue = CryptoConfig.MapNameToOID(hashAlg.ToString()); 
            } 

            if (oidValue == null) 
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));

            return oidValue;
        } 

        internal static HashAlgorithm ObjToHashAlgorithm (Object hashAlg) { 
            if (hashAlg == null) 
                throw new ArgumentNullException("hashAlg");
 
            HashAlgorithm hash = null;
            if (hashAlg is String) {
                hash = (HashAlgorithm) CryptoConfig.CreateFromName((string) hashAlg);
                if (hash == null) { 
                    string oidFriendlyName = X509Utils._GetFriendlyNameFromOid((string) hashAlg);
                    if (oidFriendlyName != null) 
                        hash = (HashAlgorithm) CryptoConfig.CreateFromName(oidFriendlyName); 
                }
            } 
            else if (hashAlg is HashAlgorithm) {
                hash = (HashAlgorithm) hashAlg;
            }
            else if (hashAlg is Type) { 
                hash = (HashAlgorithm) CryptoConfig.CreateFromName(hashAlg.ToString());
            } 
 
            if (hash == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue")); 

            return hash;
        }
 
        internal static String DiscardWhiteSpaces (string inputBuffer) {
            return DiscardWhiteSpaces(inputBuffer, 0, inputBuffer.Length); 
        } 

        internal static String DiscardWhiteSpaces (string inputBuffer, int inputOffset, int inputCount) { 
            int i, iCount = 0;
            for (i=0; i<inputCount; i++)
                if (Char.IsWhiteSpace(inputBuffer[inputOffset + i])) iCount++;
            char[] output = new char[inputCount - iCount]; 
            iCount = 0;
            for (i=0; i<inputCount; i++) { 
                if (!Char.IsWhiteSpace(inputBuffer[inputOffset + i])) 
                    output[iCount++] = inputBuffer[inputOffset + i];
            } 
            return new String(output);
        }

        internal static int ConvertByteArrayToInt (byte[] input) { 
            // Input to this routine is always big endian
            int dwOutput = 0; 
            for (int i = 0; i < input.Length; i++) { 
                dwOutput *= 256;
                dwOutput += input[i]; 
            }
            return(dwOutput);
        }
 
        // output of this routine is always big endian
        internal static byte[] ConvertIntToByteArray (int dwInput) { 
            byte[] temp = new byte[8]; // int can never be greater than Int64 
            int t1;  // t1 is remaining value to account for
            int t2;  // t2 is t1 % 256 
            int i = 0;

            if (dwInput == 0) return new byte[1];
            t1 = dwInput; 
            while (t1 > 0) {
                BCLDebug.Assert(i < 8, "Got too big an int here!"); 
                t2 = t1 % 256; 
                temp[i] = (byte) t2;
                t1 = (t1 - t2)/256; 
                i++;
            }

            // Now, copy only the non-zero part of temp and reverse 
            byte[] output = new byte[i];
            // copy and reverse in one pass 
            for (int j = 0; j < i; j++) { 
                output[j] = temp[i-j-1];
            } 
            return output;
        }

        // output is little endian 
        internal static void ConvertIntToByteArray (uint dwInput, ref byte[] counter) {
            uint t1 = dwInput;  // t1 is remaining value to account for 
            uint t2;  // t2 is t1 % 256 
            int i = 0;
 
            // clear the array first
            Array.Clear(counter, 0, counter.Length);
            if (dwInput == 0) return;
            while (t1 > 0) { 
                BCLDebug.Assert(i < 4, "Got too big an int here!");
                t2 = t1 % 256; 
                counter[3 - i] = (byte) t2; 
                t1 = (t1 - t2)/256;
                i++; 
            }
        }

        internal static byte[] FixupKeyParity (byte[] key) { 
            byte[] oddParityKey = new byte[key.Length];
            for (int index=0; index < key.Length; index++) { 
                // Get the bits we are interested in 
                oddParityKey[index] = (byte) (key[index] & 0xfe);
                // Get the parity of the sum of the previous bits 
                byte tmp1 = (byte)((oddParityKey[index] & 0xF) ^ (oddParityKey[index] >> 4));
                byte tmp2 = (byte)((tmp1 & 0x3) ^ (tmp1 >> 2));
                byte sumBitsMod2 = (byte)((tmp2 & 0x1) ^ (tmp2 >> 1));
                // We need to set the last bit in oddParityKey[index] to the negation 
                // of the last bit in sumBitsMod2
                if (sumBitsMod2 == 0) 
                    oddParityKey[index] |= 1; 
            }
            return oddParityKey; 
        }

        // digits == number of DWORDs
        internal unsafe static void DWORDFromLittleEndian (uint* x, int digits, byte* block) { 
            int i;
            int j; 
 
            for (i = 0, j = 0; i < digits; i++, j += 4)
                x[i] =  (uint) (block[j] | (block[j+1] << 8) | (block[j+2] << 16) | (block[j+3] << 24)); 
        }

        // encodes x (DWORD) into block (unsigned char), least significant byte first.
        // digits == number of DWORDs 
        internal static void DWORDToLittleEndian (byte[] block, uint[] x, int digits) {
            int i; 
            int j; 

            for (i = 0, j = 0; i < digits; i++, j += 4) { 
                block[j]   = (byte)(x[i] & 0xff);
                block[j+1] = (byte)((x[i] >> 8) & 0xff);
                block[j+2] = (byte)((x[i] >> 16) & 0xff);
                block[j+3] = (byte)((x[i] >> 24) & 0xff); 
            }
        } 
 
        // digits == number of DWORDs
        internal unsafe static void DWORDFromBigEndian (uint* x, int digits, byte* block) { 
            int i;
            int j;

            for (i = 0, j = 0; i < digits; i++, j += 4) 
                x[i] = (uint)((block[j] << 24) | (block[j + 1] << 16) | (block[j + 2] << 8) | block[j + 3]);
        } 
 
        // encodes x (DWORD) into block (unsigned char), most significant byte first.
        // digits == number of DWORDs 
        internal static void DWORDToBigEndian (byte[] block, uint[] x, int digits) {
            int i;
            int j;
 
            for (i = 0, j = 0; i < digits; i++, j += 4) {
                block[j] = (byte)((x[i] >> 24) & 0xff); 
                block[j+1] = (byte)((x[i] >> 16) & 0xff); 
                block[j+2] = (byte)((x[i] >> 8) & 0xff);
                block[j+3] = (byte)(x[i] & 0xff); 
            }
        }

        // digits == number of QWORDs 
        internal unsafe static void QuadWordFromBigEndian (UInt64* x, int digits, byte* block) {
            int i; 
            int j; 

            for (i = 0, j = 0; i < digits; i++, j += 8) 
                x[i] =  (
                         (((UInt64)block[j]) << 56) | (((UInt64)block[j+1]) << 48) |
                         (((UInt64)block[j+2]) << 40) | (((UInt64)block[j+3]) << 32) |
                         (((UInt64)block[j+4]) << 24) | (((UInt64)block[j+5]) << 16) | 
                         (((UInt64)block[j+6]) << 8) | ((UInt64)block[j+7])
                        ); 
        } 

        // encodes x (DWORD) into block (unsigned char), most significant byte first. 
        // digits = number of QWORDS
        internal static void QuadWordToBigEndian (byte[] block, UInt64[] x, int digits) {
            int i;
            int j; 

            for (i = 0, j = 0; i < digits; i++, j += 8) { 
                block[j] = (byte)((x[i] >> 56) & 0xff); 
                block[j+1] = (byte)((x[i] >> 48) & 0xff);
                block[j+2] = (byte)((x[i] >> 40) & 0xff); 
                block[j+3] = (byte)((x[i] >> 32) & 0xff);
                block[j+4] = (byte)((x[i] >> 24) & 0xff);
                block[j+5] = (byte)((x[i] >> 16) & 0xff);
                block[j+6] = (byte)((x[i] >> 8) & 0xff); 
                block[j+7] = (byte)(x[i] & 0xff);
            } 
        } 

        // encodes the integer i into a 4-byte array, in big endian. 
        internal static byte[] Int(uint i) {
            byte[] b = BitConverter.GetBytes(i);
            byte[] littleEndianBytes = {b[3], b[2], b[1], b[0]};
            return BitConverter.IsLittleEndian ? littleEndianBytes : b; 
        }
 
        internal unsafe static void BlockCopy (byte* src, int srcOffset, byte* dst, int dstOffset, int count) { 
            for (int i = 0; i < count; i++) {
                dst[dstOffset + i] = src[srcOffset + i]; 
            }
        }

        internal unsafe static void BlockCopy (byte[] src, int srcOffset, int* dst, int dstOffset, int count) { 
            fixed (byte* _src = src) {
                BlockCopy(_src, srcOffset, (byte*)dst, dstOffset, count); 
            } 
        }
 
        internal unsafe static void BlockCopy (int* src, int srcOffset, int[] dst, int dstOffset, int count) {
            fixed (int* _dst = &dst[dstOffset]) {
                BlockCopy((byte*)&src[srcOffset], srcOffset, (byte*)_dst, 0, count);
            } 
        }
 
        internal static byte[] RsaOaepEncrypt (RSA rsa, HashAlgorithm hash, PKCS1MaskGenerationMethod mgf, RandomNumberGenerator rng, byte[] data) { 
            int cb = rsa.KeySize / 8;
 
            //  1. Hash the parameters to get PHash
            int cbHash = hash.HashSize / 8;
            if ((data.Length + 2 + 2*cbHash) > cb)
                throw new CryptographicException(String.Format(null, Environment.GetResourceString("Cryptography_Padding_EncDataTooBig"), cb-2-2*cbHash)); 
            hash.ComputeHash(new byte[0]); // Use an empty octet string
 
            //  2.  Create DB object 
            byte[] DB = new byte[cb - cbHash];
 
            //  Structure is as follows:
            //      pHash || PS || 01 || M
            //      PS consists of all zeros
 
            Buffer.InternalBlockCopy(hash.Hash, 0, DB, 0, cbHash);
            DB[DB.Length - data.Length - 1] = 1; 
            Buffer.InternalBlockCopy(data, 0, DB, DB.Length-data.Length, data.Length); 

            // 3. Create a random value of size hLen 
            byte[] seed = new byte[cbHash];
            rng.GetBytes(seed);

            // 4.  Compute the mask value 
            byte[] mask = mgf.GenerateMask(seed, DB.Length);
 
            // 5.  Xor maskDB into DB 
            for (int i=0; i < DB.Length; i++) {
                DB[i] = (byte) (DB[i] ^ mask[i]); 
            }

            // 6.  Compute seed mask value
            mask = mgf.GenerateMask(DB, cbHash); 

            // 7.  Xor mask into seed 
            for (int i=0; i < seed.Length; i++) { 
                seed[i] ^= mask[i];
            } 

            // 8. Concatenate seed and DB to form value to encrypt
            byte[] pad = new byte[cb];
            Buffer.InternalBlockCopy(seed, 0, pad, 0, seed.Length); 
            Buffer.InternalBlockCopy(DB, 0, pad, seed.Length, DB.Length);
 
            return rsa.EncryptValue(pad); 
        }
 
        internal static byte[] RsaOaepDecrypt (RSA rsa, HashAlgorithm hash, PKCS1MaskGenerationMethod mgf, byte[] encryptedData) {
            int cb = rsa.KeySize / 8;

            // 1. Decode the input data 
            // It is important that the Integer to Octet String conversion errors be indistinguishable from the other decoding
            // errors to protect against chosen cipher text attacks 
            // A lecture given by James Manger during Crypto 2001 explains the issue in details 
            byte[] data = null;
            try { 
                data = rsa.DecryptValue(encryptedData);
            }
            catch (CryptographicException) {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding")); 
            }
 
            // 2. Create the hash object so we can get its size info. 
            int cbHash = hash.HashSize / 8;
 
            //  3.  Let maskedSeed be the first hLen octects and maskedDB
            //      be the remaining bytes.
            int zeros = cb - data.Length;
            if (zeros < 0 || zeros >= cbHash) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
 
            byte[] seed = new byte[cbHash]; 
            Buffer.InternalBlockCopy(data, 0, seed, zeros, seed.Length - zeros);
 
            byte[] DB = new byte[data.Length - seed.Length + zeros];
            Buffer.InternalBlockCopy(data, seed.Length - zeros, DB, 0, DB.Length);

            //  4.  seedMask = MGF(maskedDB, hLen); 
            byte[] mask = mgf.GenerateMask(DB, seed.Length);
 
            //  5.  seed = seedMask XOR maskedSeed 
            int i = 0;
            for (i=0; i < seed.Length; i++) { 
                seed[i] ^= mask[i];
            }

            //  6.  dbMask = MGF(seed, |EM| - hLen); 
            mask = mgf.GenerateMask(seed, DB.Length);
 
            //  7.  DB = maskedDB xor dbMask 
            for (i=0; i < DB.Length; i++) {
                DB[i] = (byte) (DB[i] ^ mask[i]); 
            }

            //  8.  pHash = HASH(P)
            hash.ComputeHash(new byte[0]); 

            //  9.  DB = pHash' || PS || 01 || M 
            //  10.  Check that pHash = pHash' 

            byte[] hashValue = hash.Hash; 
            for (i=0; i < cbHash; i++) {
                if (DB[i] != hashValue[i])
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
            } 

            //  Check that PS is all zeros 
            for (; i<DB.Length; i++) { 
                if (DB[i] == 1)
                    break; 
                else if (DB[i] != 0)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
            }
 
            if (i == DB.Length)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding")); 
 
            i++; // skip over the one
 
            //  11. Output M.
            byte[] output = new byte[DB.Length - i];
            Buffer.InternalBlockCopy(DB, i, output, 0, output.Length);
            return output; 
        }
 
        internal static byte[] RsaPkcs1Padding (RSA rsa, byte[] oid, byte[] hash) { 
            int cb = rsa.KeySize/8;
            byte[] pad = new byte[cb]; 

            //
            //  We want to pad this to the following format:
            // 
            //  00 || 01 || FF ... FF || 00 || prefix || Data
            // 
            // We want basically to ASN 1 encode the OID + hash: 
            // STRUCTURE {
            //  STRUCTURE { 
            //    OID <hash algorithm OID>
            //    NULL (0x05 0x00)  // this is actually an ANY and contains the parameters of the algorithm specified by the OID, I think
            //  }
            //  OCTET STRING <hashvalue> 
            // }
            // 
 
            // Get the correct prefix
            byte[] data = new byte[oid.Length + 8 + hash.Length]; 
            data[0] = 0x30; // a structure follows
            int tmp = data.Length - 2;
            data[1] = (byte) tmp;
            data[2] = 0x30; 
            tmp = oid.Length + 2;
            data[3] = (byte) tmp; 
            Buffer.InternalBlockCopy(oid, 0, data, 4, oid.Length); 
            data[4 + oid.Length] = 0x05;
            data[4 + oid.Length + 1] = 0x00; 
            data[4 + oid.Length + 2] = 0x04; // an octet string follows
            data[4 + oid.Length + 3] = (byte) hash.Length;
            Buffer.InternalBlockCopy(hash, 0, data, oid.Length + 8, hash.Length);
 
            // Construct the whole array
            int cb1 = cb - data.Length; 
            if (cb1 <= 2) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOID"));
 
            pad[0] = 0;
            pad[1] = 1;
            for (int i=2; i<cb1-1; i++) {
                pad[i] = 0xff; 
            }
            pad[cb1-1] = 0; 
            Buffer.InternalBlockCopy(data, 0, pad, cb1, data.Length); 
            return pad;
        } 

        // This routine compares 2 big ints; ignoring any leading zeros
        internal static bool CompareBigIntArrays (byte[] lhs, byte[] rhs) {
            if (lhs == null) 
                return (rhs == null);
 
            int i = 0, j = 0; 
            while (i < lhs.Length && lhs[i] == 0) i++;
            while (j < rhs.Length && rhs[j] == 0) j++; 

            int count = (lhs.Length - i);
            if ((rhs.Length - j) != count)
                return false; 

            for (int k = 0; k < count; k++) { 
                if (lhs[i + k] != rhs[j + k]) 
                    return false;
            } 
            return true;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _AcquireCSP(CspParameters param, ref SafeProvHandle hProv);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _CreateCSP(CspParameters param, bool randomKeyContainer, ref SafeProvHandle hProv); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _CreateHash(SafeProvHandle hProv, int algid, ref SafeHashHandle hKey); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _CryptDeriveKey(SafeProvHandle hProv, int algid, int algidHash, byte[] password, int dwFlags, byte[] IV);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int _DecryptData(SafeKeyHandle hKey, byte[] data, int ib, int cb, ref byte[] outputBuffer, int outputOffset, PaddingMode PaddingMode, bool fDone); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _DecryptKey(SafeKeyHandle hPubKey, byte[] key, int dwFlags); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _DecryptPKWin2KEnh(SafeKeyHandle hPubKey, byte[] key, bool fOAEP, out int hr);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern int _EncryptData(SafeKeyHandle hKey, byte[] data, int ib, int cb, ref byte[] outputBuffer, int outputOffset, PaddingMode PaddingMode, bool fDone);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _EncryptKey(SafeKeyHandle hPubKey, byte[] key);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _EncryptPKWin2KEnh(SafeKeyHandle hPubKey, byte[] key, bool fOAEP, out int hr);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _EndHash(SafeHashHandle hHash); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _ExportCspBlob(SafeKeyHandle hKey, int blobType); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _ExportKey(SafeKeyHandle hKey, int blobType, object cspObject);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _GenerateKey(SafeProvHandle hProv, int algid, CspProviderFlags flags, int keySize, ref SafeKeyHandle hKey); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _GetBytes(SafeProvHandle hProv, byte[] randomBytes); 
        [MethodImpl(MethodImplOptions.InternalCall)] 
        private static extern bool _GetEnforceFipsPolicySetting();
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _GetKeyParameter(SafeKeyHandle hKey, uint paramID);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetKeySetSecurityInfo(SafeProvHandle hProv, SecurityInfos securityInfo, out int error);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _GetNonZeroBytes(SafeProvHandle hProv, byte[] randomBytes);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern bool _GetPersistKeyInCsp(SafeProvHandle hProv); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object _GetProviderParameter(SafeProvHandle hProv, int keyNumber, uint paramID); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetRandomKeyContainer();
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int _GetUserKey(SafeProvHandle hProv, int keyNumber, ref SafeKeyHandle hKey); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _HashData(SafeHashHandle hHash, byte[] data, int ibStart, int cbSize); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _ImportBulkKey(SafeProvHandle hProv, int algid, bool useSalt, byte[] key, ref SafeKeyHandle hKey);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern int _ImportCspBlob(byte[] keyBlob, SafeProvHandle hProv, CspProviderFlags flags, ref SafeKeyHandle hKey);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _ImportKey(SafeProvHandle hCSP, int keyNumber, CspProviderFlags flags, object cspObject, ref SafeKeyHandle hKey);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern int _OpenCSP(CspParameters param, uint flags, ref SafeProvHandle hProv);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern bool _ProduceLegacyHmacValues(); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool _SearchForAlgorithm(SafeProvHandle hProv, int algID, int keyLength); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _SetKeyParamDw(SafeKeyHandle hKey, int param, int dwValue);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _SetKeyParamRgb(SafeKeyHandle hKey, int param, byte[] value); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int _SetKeySetSecurityInfo (SafeProvHandle hProv, SecurityInfos securityInfo, byte[] sd); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _SetPersistKeyInCsp(SafeProvHandle hProv, bool fPersistKeyInCsp);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _SetProviderParameter(SafeProvHandle hProv, int keyNumber, uint paramID, IntPtr pbData);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _ShowLegacyHmacWarning();
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _SignValue(SafeKeyHandle hKey, int keyNumber, int calgKey, int calgHash, byte[] hash, int dwFlags);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern bool _VerifySign(SafeKeyHandle hKey, int calgKey, int calgHash, byte[] hash, byte[] signature, int dwFlags); 
    }
} 
