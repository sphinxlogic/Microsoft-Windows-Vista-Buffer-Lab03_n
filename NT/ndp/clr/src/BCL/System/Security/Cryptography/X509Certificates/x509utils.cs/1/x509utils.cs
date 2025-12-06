// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// X509Utils.cs 
//
 
namespace System.Security.Cryptography.X509Certificates
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices; 
    using Microsoft.Win32;
 
    internal static class X509Constants { 
        internal const uint CRYPT_EXPORTABLE     = 0x00000001;
        internal const uint CRYPT_USER_PROTECTED = 0x00000002; 
        internal const uint CRYPT_MACHINE_KEYSET = 0x00000020;
        internal const uint CRYPT_USER_KEYSET    = 0x00001000;

        internal const uint CERT_QUERY_CONTENT_CERT               = 1; 
        internal const uint CERT_QUERY_CONTENT_CTL                = 2;
        internal const uint CERT_QUERY_CONTENT_CRL                = 3; 
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_STORE   = 4; 
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CERT    = 5;
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CTL     = 6; 
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CRL     = 7;
        internal const uint CERT_QUERY_CONTENT_PKCS7_SIGNED       = 8;
        internal const uint CERT_QUERY_CONTENT_PKCS7_UNSIGNED     = 9;
        internal const uint CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED = 10; 
        internal const uint CERT_QUERY_CONTENT_PKCS10             = 11;
        internal const uint CERT_QUERY_CONTENT_PFX                = 12; 
        internal const uint CERT_QUERY_CONTENT_CERT_PAIR          = 13; 

        internal const uint CERT_STORE_PROV_MEMORY   = 2; 
        internal const uint CERT_STORE_PROV_SYSTEM   = 10;

        // cert store flags
        internal const uint CERT_STORE_NO_CRYPT_RELEASE_FLAG            = 0x00000001; 
        internal const uint CERT_STORE_SET_LOCALIZED_NAME_FLAG          = 0x00000002;
        internal const uint CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG = 0x00000004; 
        internal const uint CERT_STORE_DELETE_FLAG                      = 0x00000010; 
        internal const uint CERT_STORE_SHARE_STORE_FLAG                 = 0x00000040;
        internal const uint CERT_STORE_SHARE_CONTEXT_FLAG               = 0x00000080; 
        internal const uint CERT_STORE_MANIFOLD_FLAG                    = 0x00000100;
        internal const uint CERT_STORE_ENUM_ARCHIVED_FLAG               = 0x00000200;
        internal const uint CERT_STORE_UPDATE_KEYID_FLAG                = 0x00000400;
        internal const uint CERT_STORE_BACKUP_RESTORE_FLAG              = 0x00000800; 
        internal const uint CERT_STORE_READONLY_FLAG                    = 0x00008000;
        internal const uint CERT_STORE_OPEN_EXISTING_FLAG               = 0x00004000; 
        internal const uint CERT_STORE_CREATE_NEW_FLAG                  = 0x00002000; 
        internal const uint CERT_STORE_MAXIMUM_ALLOWED_FLAG             = 0x00001000;
 
        internal const uint CERT_NAME_EMAIL_TYPE            = 1;
        internal const uint CERT_NAME_RDN_TYPE              = 2;
        internal const uint CERT_NAME_SIMPLE_DISPLAY_TYPE   = 4;
        internal const uint CERT_NAME_FRIENDLY_DISPLAY_TYPE = 5; 
        internal const uint CERT_NAME_DNS_TYPE              = 6;
        internal const uint CERT_NAME_URL_TYPE              = 7; 
        internal const uint CERT_NAME_UPN_TYPE              = 8; 
    }
 
    internal static class X509Utils {
        internal static int OidToAlgId (string oid) {
            // Default Algorithm Id is CALG_SHA1
            if (oid == null) 
                return Constants.CALG_SHA1;
            string oidValue = CryptoConfig.MapNameToOID(oid); 
            if (oidValue == null) 
                oidValue = oid; // we were probably passed an OID value directly
            return _GetAlgIdFromOid(oidValue); 
        }

        // this method maps a cert content type returned from CryptQueryObject
        // to a value in the managed X509ContentType enum 
        internal static X509ContentType MapContentType (uint contentType) {
            switch (contentType) { 
            case X509Constants.CERT_QUERY_CONTENT_CERT: 
                return X509ContentType.Cert;
            case X509Constants.CERT_QUERY_CONTENT_SERIALIZED_STORE: 
                return X509ContentType.SerializedStore;
            case X509Constants.CERT_QUERY_CONTENT_SERIALIZED_CERT:
                return X509ContentType.SerializedCert;
            case X509Constants.CERT_QUERY_CONTENT_PKCS7_SIGNED: 
            case X509Constants.CERT_QUERY_CONTENT_PKCS7_UNSIGNED:
                return X509ContentType.Pkcs7; 
            case X509Constants.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED: 
                return X509ContentType.Authenticode;
            case X509Constants.CERT_QUERY_CONTENT_PFX: 
                return X509ContentType.Pkcs12;
            default:
                return X509ContentType.Unknown;
            } 
        }
 
        // this method maps a X509KeyStorageFlags enum to a combination of crypto API flags 
        internal static uint MapKeyStorageFlags (X509KeyStorageFlags keyStorageFlags) {
            uint dwFlags = 0; 
            if ((keyStorageFlags & X509KeyStorageFlags.UserKeySet) == X509KeyStorageFlags.UserKeySet)
                dwFlags |= X509Constants.CRYPT_USER_KEYSET;
            else if ((keyStorageFlags & X509KeyStorageFlags.MachineKeySet) == X509KeyStorageFlags.MachineKeySet)
                dwFlags |= X509Constants.CRYPT_MACHINE_KEYSET; 

            if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable) 
                dwFlags |= X509Constants.CRYPT_EXPORTABLE; 
            if ((keyStorageFlags & X509KeyStorageFlags.UserProtected) == X509KeyStorageFlags.UserProtected)
                dwFlags |= X509Constants.CRYPT_USER_PROTECTED; 

            return dwFlags;
        }
 
        // this method creates a memory store from a certificate
        internal static SafeCertStoreHandle ExportCertToMemoryStore (X509Certificate certificate) { 
            SafeCertStoreHandle safeCertStoreHandle = SafeCertStoreHandle.InvalidHandle; 
            X509Utils._OpenX509Store(X509Constants.CERT_STORE_PROV_MEMORY,
                                     X509Constants.CERT_STORE_ENUM_ARCHIVED_FLAG | X509Constants.CERT_STORE_CREATE_NEW_FLAG, 
                                     null,
                                     ref safeCertStoreHandle);
            X509Utils._AddCertificateToStore(safeCertStoreHandle, certificate.CertContext);
            return safeCertStoreHandle; 
        }
 
        internal static IntPtr PasswordToCoTaskMemUni (object password) { 
            if (password != null) {
                string pwd = password as string; 
                if (pwd != null)
                    return Marshal.StringToCoTaskMemUni(pwd);
                SecureString securePwd = password as SecureString;
                if (securePwd != null) 
                    return Marshal.SecureStringToCoTaskMemUnicode(securePwd);
            } 
            return IntPtr.Zero; 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _AddCertificateToStore(SafeCertStoreHandle safeCertStoreHandle, SafeCertContextHandle safeCertContext);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _DuplicateCertContext(IntPtr handle, ref SafeCertContextHandle safeCertContext); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _ExportCertificatesToBlob(SafeCertStoreHandle safeCertStoreHandle, X509ContentType contentType, IntPtr password); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern int _GetAlgIdFromOid(string oid);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _GetCertRawData(SafeCertContextHandle safeCertContext);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _GetDateNotAfter(SafeCertContextHandle safeCertContext, ref Win32Native.FILE_TIME fileTime);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _GetDateNotBefore(SafeCertContextHandle safeCertContext, ref Win32Native.FILE_TIME fileTime);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern string _GetFriendlyNameFromOid(string oid); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetIssuerName(SafeCertContextHandle safeCertContext, bool legacyV1Mode); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetOidFromFriendlyName(string oid);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetPublicKeyOid(SafeCertContextHandle safeCertContext); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetPublicKeyParameters(SafeCertContextHandle safeCertContext); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _GetPublicKeyValue(SafeCertContextHandle safeCertContext);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern string _GetSubjectInfo(SafeCertContextHandle safeCertContext, uint displayType, bool legacyV1Mode);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetSerialNumber(SafeCertContextHandle safeCertContext);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _GetThumbprint(SafeCertContextHandle safeCertContext);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _LoadCertFromBlob(byte[] rawData, IntPtr password, uint dwFlags, bool persistKeySet, ref SafeCertContextHandle pCertCtx); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _LoadCertFromFile(string fileName, IntPtr password, uint dwFlags, bool persistKeySet, ref SafeCertContextHandle pCertCtx); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _OpenX509Store(uint storeType, uint flags, string storeName, ref SafeCertStoreHandle safeCertStoreHandle);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern uint _QueryCertBlobType(byte[] rawData); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern uint _QueryCertFileType(string fileName); 
    } 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// X509Utils.cs 
//
 
namespace System.Security.Cryptography.X509Certificates
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices; 
    using Microsoft.Win32;
 
    internal static class X509Constants { 
        internal const uint CRYPT_EXPORTABLE     = 0x00000001;
        internal const uint CRYPT_USER_PROTECTED = 0x00000002; 
        internal const uint CRYPT_MACHINE_KEYSET = 0x00000020;
        internal const uint CRYPT_USER_KEYSET    = 0x00001000;

        internal const uint CERT_QUERY_CONTENT_CERT               = 1; 
        internal const uint CERT_QUERY_CONTENT_CTL                = 2;
        internal const uint CERT_QUERY_CONTENT_CRL                = 3; 
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_STORE   = 4; 
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CERT    = 5;
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CTL     = 6; 
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CRL     = 7;
        internal const uint CERT_QUERY_CONTENT_PKCS7_SIGNED       = 8;
        internal const uint CERT_QUERY_CONTENT_PKCS7_UNSIGNED     = 9;
        internal const uint CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED = 10; 
        internal const uint CERT_QUERY_CONTENT_PKCS10             = 11;
        internal const uint CERT_QUERY_CONTENT_PFX                = 12; 
        internal const uint CERT_QUERY_CONTENT_CERT_PAIR          = 13; 

        internal const uint CERT_STORE_PROV_MEMORY   = 2; 
        internal const uint CERT_STORE_PROV_SYSTEM   = 10;

        // cert store flags
        internal const uint CERT_STORE_NO_CRYPT_RELEASE_FLAG            = 0x00000001; 
        internal const uint CERT_STORE_SET_LOCALIZED_NAME_FLAG          = 0x00000002;
        internal const uint CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG = 0x00000004; 
        internal const uint CERT_STORE_DELETE_FLAG                      = 0x00000010; 
        internal const uint CERT_STORE_SHARE_STORE_FLAG                 = 0x00000040;
        internal const uint CERT_STORE_SHARE_CONTEXT_FLAG               = 0x00000080; 
        internal const uint CERT_STORE_MANIFOLD_FLAG                    = 0x00000100;
        internal const uint CERT_STORE_ENUM_ARCHIVED_FLAG               = 0x00000200;
        internal const uint CERT_STORE_UPDATE_KEYID_FLAG                = 0x00000400;
        internal const uint CERT_STORE_BACKUP_RESTORE_FLAG              = 0x00000800; 
        internal const uint CERT_STORE_READONLY_FLAG                    = 0x00008000;
        internal const uint CERT_STORE_OPEN_EXISTING_FLAG               = 0x00004000; 
        internal const uint CERT_STORE_CREATE_NEW_FLAG                  = 0x00002000; 
        internal const uint CERT_STORE_MAXIMUM_ALLOWED_FLAG             = 0x00001000;
 
        internal const uint CERT_NAME_EMAIL_TYPE            = 1;
        internal const uint CERT_NAME_RDN_TYPE              = 2;
        internal const uint CERT_NAME_SIMPLE_DISPLAY_TYPE   = 4;
        internal const uint CERT_NAME_FRIENDLY_DISPLAY_TYPE = 5; 
        internal const uint CERT_NAME_DNS_TYPE              = 6;
        internal const uint CERT_NAME_URL_TYPE              = 7; 
        internal const uint CERT_NAME_UPN_TYPE              = 8; 
    }
 
    internal static class X509Utils {
        internal static int OidToAlgId (string oid) {
            // Default Algorithm Id is CALG_SHA1
            if (oid == null) 
                return Constants.CALG_SHA1;
            string oidValue = CryptoConfig.MapNameToOID(oid); 
            if (oidValue == null) 
                oidValue = oid; // we were probably passed an OID value directly
            return _GetAlgIdFromOid(oidValue); 
        }

        // this method maps a cert content type returned from CryptQueryObject
        // to a value in the managed X509ContentType enum 
        internal static X509ContentType MapContentType (uint contentType) {
            switch (contentType) { 
            case X509Constants.CERT_QUERY_CONTENT_CERT: 
                return X509ContentType.Cert;
            case X509Constants.CERT_QUERY_CONTENT_SERIALIZED_STORE: 
                return X509ContentType.SerializedStore;
            case X509Constants.CERT_QUERY_CONTENT_SERIALIZED_CERT:
                return X509ContentType.SerializedCert;
            case X509Constants.CERT_QUERY_CONTENT_PKCS7_SIGNED: 
            case X509Constants.CERT_QUERY_CONTENT_PKCS7_UNSIGNED:
                return X509ContentType.Pkcs7; 
            case X509Constants.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED: 
                return X509ContentType.Authenticode;
            case X509Constants.CERT_QUERY_CONTENT_PFX: 
                return X509ContentType.Pkcs12;
            default:
                return X509ContentType.Unknown;
            } 
        }
 
        // this method maps a X509KeyStorageFlags enum to a combination of crypto API flags 
        internal static uint MapKeyStorageFlags (X509KeyStorageFlags keyStorageFlags) {
            uint dwFlags = 0; 
            if ((keyStorageFlags & X509KeyStorageFlags.UserKeySet) == X509KeyStorageFlags.UserKeySet)
                dwFlags |= X509Constants.CRYPT_USER_KEYSET;
            else if ((keyStorageFlags & X509KeyStorageFlags.MachineKeySet) == X509KeyStorageFlags.MachineKeySet)
                dwFlags |= X509Constants.CRYPT_MACHINE_KEYSET; 

            if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable) 
                dwFlags |= X509Constants.CRYPT_EXPORTABLE; 
            if ((keyStorageFlags & X509KeyStorageFlags.UserProtected) == X509KeyStorageFlags.UserProtected)
                dwFlags |= X509Constants.CRYPT_USER_PROTECTED; 

            return dwFlags;
        }
 
        // this method creates a memory store from a certificate
        internal static SafeCertStoreHandle ExportCertToMemoryStore (X509Certificate certificate) { 
            SafeCertStoreHandle safeCertStoreHandle = SafeCertStoreHandle.InvalidHandle; 
            X509Utils._OpenX509Store(X509Constants.CERT_STORE_PROV_MEMORY,
                                     X509Constants.CERT_STORE_ENUM_ARCHIVED_FLAG | X509Constants.CERT_STORE_CREATE_NEW_FLAG, 
                                     null,
                                     ref safeCertStoreHandle);
            X509Utils._AddCertificateToStore(safeCertStoreHandle, certificate.CertContext);
            return safeCertStoreHandle; 
        }
 
        internal static IntPtr PasswordToCoTaskMemUni (object password) { 
            if (password != null) {
                string pwd = password as string; 
                if (pwd != null)
                    return Marshal.StringToCoTaskMemUni(pwd);
                SecureString securePwd = password as SecureString;
                if (securePwd != null) 
                    return Marshal.SecureStringToCoTaskMemUnicode(securePwd);
            } 
            return IntPtr.Zero; 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _AddCertificateToStore(SafeCertStoreHandle safeCertStoreHandle, SafeCertContextHandle safeCertContext);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _DuplicateCertContext(IntPtr handle, ref SafeCertContextHandle safeCertContext); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _ExportCertificatesToBlob(SafeCertStoreHandle safeCertStoreHandle, X509ContentType contentType, IntPtr password); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern int _GetAlgIdFromOid(string oid);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _GetCertRawData(SafeCertContextHandle safeCertContext);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _GetDateNotAfter(SafeCertContextHandle safeCertContext, ref Win32Native.FILE_TIME fileTime);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _GetDateNotBefore(SafeCertContextHandle safeCertContext, ref Win32Native.FILE_TIME fileTime);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern string _GetFriendlyNameFromOid(string oid); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetIssuerName(SafeCertContextHandle safeCertContext, bool legacyV1Mode); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetOidFromFriendlyName(string oid);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string _GetPublicKeyOid(SafeCertContextHandle safeCertContext); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetPublicKeyParameters(SafeCertContextHandle safeCertContext); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _GetPublicKeyValue(SafeCertContextHandle safeCertContext);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern string _GetSubjectInfo(SafeCertContextHandle safeCertContext, uint displayType, bool legacyV1Mode);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte[] _GetSerialNumber(SafeCertContextHandle safeCertContext);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern byte[] _GetThumbprint(SafeCertContextHandle safeCertContext);
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void _LoadCertFromBlob(byte[] rawData, IntPtr password, uint dwFlags, bool persistKeySet, ref SafeCertContextHandle pCertCtx); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _LoadCertFromFile(string fileName, IntPtr password, uint dwFlags, bool persistKeySet, ref SafeCertContextHandle pCertCtx); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _OpenX509Store(uint storeType, uint flags, string storeName, ref SafeCertStoreHandle safeCertStoreHandle);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern uint _QueryCertBlobType(byte[] rawData); 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern uint _QueryCertFileType(string fileName); 
    } 
}
