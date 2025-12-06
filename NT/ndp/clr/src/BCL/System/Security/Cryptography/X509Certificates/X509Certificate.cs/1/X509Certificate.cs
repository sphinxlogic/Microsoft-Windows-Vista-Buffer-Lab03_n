// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// X509Certificate.cs 
//
 
namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Win32;
    using System;
    using System.IO; 
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices; 
    using System.Runtime.Serialization; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Security.Util;
    using System.Text;
    using System.Runtime.Versioning;
 
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum X509ContentType { 
        Unknown         = 0x00, 
        Cert            = 0x01,
        SerializedCert  = 0x02, 
        Pfx             = 0x03,
        Pkcs12          = Pfx,
        SerializedStore = 0x04,
        Pkcs7           = 0x05, 
        Authenticode    = 0x06
    } 
 
    // DefaultKeySet, UserKeySet and MachineKeySet are mutually exclusive
    [Flags, Serializable()] 
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum X509KeyStorageFlags {
        DefaultKeySet = 0x00,
        UserKeySet    = 0x01, 
        MachineKeySet = 0x02,
        Exportable    = 0x04, 
        UserProtected = 0x08, 
        PersistKeySet = 0x10
    } 

#if !FEATURE_PAL

    [Serializable()] 
    [System.Runtime.InteropServices.ComVisible(true)]
    public class X509Certificate : IDeserializationCallback, ISerializable { 
        private const string m_format = "X509"; 
        private string m_subjectName;
        private string m_issuerName; 
        private byte[] m_serialNumber;
        private byte[] m_publicKeyParameters;
        private byte[] m_publicKeyValue;
        private string m_publicKeyOid; 
        private byte[] m_rawData;
        private byte[] m_thumbprint; 
        private DateTime m_notBefore; 
        private DateTime m_notAfter;
        private SafeCertContextHandle m_safeCertContext = SafeCertContextHandle.InvalidHandle; 

        //
        // public constructors
        // 

        public X509Certificate () {} 
 
        public X509Certificate (byte[] data) {
            if ((data != null) && (data.Length != 0)) 
                LoadCertificateFromBlob(data, null, X509KeyStorageFlags.DefaultKeySet);
        }

        public X509Certificate (byte[] rawData, string password) { 
            LoadCertificateFromBlob(rawData, password, X509KeyStorageFlags.DefaultKeySet);
        } 
 
        public X509Certificate (byte[] rawData, SecureString password) {
            LoadCertificateFromBlob(rawData, password, X509KeyStorageFlags.DefaultKeySet); 
        }

        public X509Certificate (byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags) {
            LoadCertificateFromBlob(rawData, password, keyStorageFlags); 
        }
 
        public X509Certificate (byte[] rawData, SecureString password, X509KeyStorageFlags keyStorageFlags) { 
            LoadCertificateFromBlob(rawData, password, keyStorageFlags);
        } 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public X509Certificate (string fileName) { 
            LoadCertificateFromFile(fileName, null, X509KeyStorageFlags.DefaultKeySet);
        } 
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public X509Certificate (string fileName, string password) {
            LoadCertificateFromFile(fileName, password, X509KeyStorageFlags.DefaultKeySet);
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public X509Certificate (string fileName, SecureString password) { 
            LoadCertificateFromFile(fileName, password, X509KeyStorageFlags.DefaultKeySet);
        } 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public X509Certificate (string fileName, string password, X509KeyStorageFlags keyStorageFlags) { 
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        } 
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public X509Certificate (string fileName, SecureString password, X509KeyStorageFlags keyStorageFlags) {
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        }
 
        // Package protected constructor for creating a certificate from a PCCERT_CONTEXT
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public X509Certificate (IntPtr handle) {
            if (handle == IntPtr.Zero) 
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidHandle"), "handle");

            X509Utils._DuplicateCertContext(handle, ref m_safeCertContext);
        } 

        public X509Certificate (X509Certificate cert) { 
            if (cert == null) 
                throw new ArgumentNullException("cert");
 
            if (cert.m_safeCertContext.pCertContext != IntPtr.Zero)
                X509Utils._DuplicateCertContext(cert.m_safeCertContext.pCertContext, ref this.m_safeCertContext);

            // we need to keep the certificate context alive until we are done with duplicating the handle. 
            GC.KeepAlive(cert.m_safeCertContext);
        } 
 
        public X509Certificate (SerializationInfo info, StreamingContext context) {
            byte[] rawData = (byte[]) info.GetValue("RawData", typeof(byte[])); 
            if (rawData != null)
                LoadCertificateFromBlob(rawData, null, X509KeyStorageFlags.DefaultKeySet);
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public static X509Certificate CreateFromCertFile (string filename) { 
            return new X509Certificate(filename);
        } 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static X509Certificate CreateFromSignedFile (string filename) { 
            return new X509Certificate(filename);
        } 
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public IntPtr Handle { 
            [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            get {
                return m_safeCertContext.pCertContext; 
            }
        } 
 
        [Obsolete("This method has been deprecated.  Please use the Subject property instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetName() { 
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");

            return X509Utils._GetSubjectInfo(m_safeCertContext, X509Constants.CERT_NAME_RDN_TYPE, true); 
        }
 
        [Obsolete("This method has been deprecated.  Please use the Issuer property instead.  http://go.microsoft.com/fwlink/?linkid=14202")] 
        public virtual string GetIssuerName() {
            if (m_safeCertContext.IsInvalid) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");

            return X509Utils._GetIssuerName(m_safeCertContext, true);
        } 

        public virtual byte[] GetSerialNumber() { 
            if (m_safeCertContext.IsInvalid) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
 
            if (m_serialNumber == null)
                m_serialNumber = X509Utils._GetSerialNumber(m_safeCertContext);
            return (byte[]) m_serialNumber.Clone();
        } 

        public virtual string GetSerialNumberString() { 
            return SerialNumber; 
        }
 
        public virtual byte[] GetKeyAlgorithmParameters() {
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
 
            if (m_publicKeyParameters == null)
                m_publicKeyParameters = X509Utils._GetPublicKeyParameters(m_safeCertContext); 
 
            return (byte[]) m_publicKeyParameters.Clone();
        } 

        public virtual string GetKeyAlgorithmParametersString() {
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 

            return Hex.EncodeHexString(GetKeyAlgorithmParameters()); 
        } 

        public virtual string GetKeyAlgorithm() { 
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");

            if (m_publicKeyOid == null) 
                m_publicKeyOid = X509Utils._GetPublicKeyOid(m_safeCertContext);
 
            return m_publicKeyOid; 
        }
 
        public virtual byte[] GetPublicKey() {
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
 
            if (m_publicKeyValue == null)
                m_publicKeyValue = X509Utils._GetPublicKeyValue(m_safeCertContext); 
 
            return (byte[]) m_publicKeyValue.Clone();
        } 

        public virtual string GetPublicKeyString() {
            return Hex.EncodeHexString(GetPublicKey());
        } 

        public virtual byte[] GetRawCertData() { 
            return RawData; 
        }
 
        public virtual string GetRawCertDataString() {
            return Hex.EncodeHexString(GetRawCertData());
        }
 
        public virtual byte[] GetCertHash() {
            SetThumbprint(); 
            return (byte[]) m_thumbprint.Clone(); 
        }
 
        public virtual string GetCertHashString() {
            SetThumbprint();
            return Hex.EncodeHexString(m_thumbprint);
        } 

        public virtual string GetEffectiveDateString() { 
            return NotBefore.ToString(); 
        }
 
        public virtual string GetExpirationDateString() {
            return NotAfter.ToString();
        }
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool Equals (Object obj) { 
            if (!(obj is X509Certificate)) return false; 
            X509Certificate other = (X509Certificate) obj;
            return this.Equals(other); 
        }

        public virtual bool Equals (X509Certificate other) {
            if (other == null) 
                return false;
 
            if (m_safeCertContext.IsInvalid) 
                return other.m_safeCertContext.IsInvalid;
 
            if (!this.Issuer.Equals(other.Issuer))
                return false;

            if (!this.SerialNumber.Equals(other.SerialNumber)) 
                return false;
 
            return true; 
        }
 
        public override int GetHashCode() {
            if (m_safeCertContext.IsInvalid)
                return 0;
 
            SetThumbprint();
            int value = 0; 
            for (int i = 0; i < m_thumbprint.Length && i < 4; ++i) { 
                value = value << 8 | m_thumbprint[i];
            } 
            return value;
        }

        public override string ToString() { 
            return ToString(false);
        } 
 
        public virtual string ToString (bool fVerbose) {
            if (fVerbose == false || m_safeCertContext.IsInvalid) 
                return GetType().FullName;

            StringBuilder sb = new StringBuilder();
 
            // Subject
            sb.Append("[Subject]" + Environment.NewLine + "  "); 
            sb.Append(this.Subject); 

            // Issuer 
            sb.Append(Environment.NewLine + Environment.NewLine + "[Issuer]" + Environment.NewLine + "  ");
            sb.Append(this.Issuer);

            // Serial Number 
            sb.Append(Environment.NewLine + Environment.NewLine + "[Serial Number]" + Environment.NewLine + "  ");
            sb.Append(this.SerialNumber); 
 
            // NotBefore
            sb.Append(Environment.NewLine + Environment.NewLine + "[Not Before]" + Environment.NewLine + "  "); 
            sb.Append(this.NotBefore);

            // NotAfter
            sb.Append(Environment.NewLine + Environment.NewLine + "[Not After]" + Environment.NewLine + "  "); 
            sb.Append(this.NotAfter);
 
            // Thumbprint 
            sb.Append(Environment.NewLine + Environment.NewLine + "[Thumbprint]" + Environment.NewLine + "  ");
            sb.Append(this.GetCertHashString()); 

            sb.Append(Environment.NewLine);
            return sb.ToString();
        } 

        public virtual string GetFormat() { 
            return m_format; 
        }
 
        public string Issuer {
            get {
                if (m_safeCertContext.IsInvalid)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 

                if (m_issuerName == null) 
                    m_issuerName = X509Utils._GetIssuerName(m_safeCertContext, false); 
                return m_issuerName;
            } 
        }

        public string Subject {
            get { 
                if (m_safeCertContext.IsInvalid)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 
 
                if (m_subjectName == null)
                    m_subjectName = X509Utils._GetSubjectInfo(m_safeCertContext, X509Constants.CERT_NAME_RDN_TYPE, false); 
                return m_subjectName;
            }
        }
 
        [System.Runtime.InteropServices.ComVisible(false)]
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)] 
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)] 
        public virtual void Import(byte[] rawData) {
            Reset(); 
            LoadCertificateFromBlob(rawData, null, X509KeyStorageFlags.DefaultKeySet);
        }

        [System.Runtime.InteropServices.ComVisible(false)] 
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)] 
        public virtual void Import(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags) { 
            Reset();
            LoadCertificateFromBlob(rawData, password, keyStorageFlags); 
        }

        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)] 
        public virtual void Import(byte[] rawData, SecureString password, X509KeyStorageFlags keyStorageFlags) {
            Reset(); 
            LoadCertificateFromBlob(rawData, password, keyStorageFlags); 
        }
 
        [System.Runtime.InteropServices.ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)] 
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
        public virtual void Import(string fileName) { 
            Reset(); 
            LoadCertificateFromFile(fileName, null, X509KeyStorageFlags.DefaultKeySet);
        } 

        [System.Runtime.InteropServices.ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)] 
        public virtual void Import(string fileName, string password, X509KeyStorageFlags keyStorageFlags) { 
            Reset();
            LoadCertificateFromFile(fileName, password, keyStorageFlags); 
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)] 
        public virtual void Import(string fileName, SecureString password, X509KeyStorageFlags keyStorageFlags) { 
            Reset();
            LoadCertificateFromFile(fileName, password, keyStorageFlags); 
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual byte[] Export(X509ContentType contentType) { 
            return ExportHelper(contentType, null);
        } 
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual byte[] Export(X509ContentType contentType, string password) { 
            return ExportHelper(contentType, password);
        }

        public virtual byte[] Export(X509ContentType contentType, SecureString password) { 
            return ExportHelper(contentType, password);
        } 
 
        [System.Runtime.InteropServices.ComVisible(false)]
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)] 
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
        public virtual void Reset () {
            m_subjectName = null;
            m_issuerName = null; 
            m_serialNumber = null;
            m_publicKeyParameters = null; 
            m_publicKeyValue = null; 
            m_publicKeyOid = null;
            m_rawData = null; 
            m_thumbprint = null;
            m_notBefore = DateTime.MinValue;
            m_notAfter = DateTime.MinValue;
            if (!m_safeCertContext.IsInvalid) { 
                // Free the current certificate handle
                m_safeCertContext.Dispose(); 
                m_safeCertContext = SafeCertContextHandle.InvalidHandle; 
            }
        } 

        /// <internalonly/>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context) { 
            if (m_safeCertContext.IsInvalid)
                info.AddValue("RawData", null); 
            else 
                info.AddValue("RawData", this.RawData);
        } 

        /// <internalonly/>
        void IDeserializationCallback.OnDeserialization(Object sender) {}
 
        //
        // internal. 
        // 

        internal SafeCertContextHandle CertContext { 
            get {
                return m_safeCertContext;
            }
        } 

        // 
        // private methods. 
        //
 
        private DateTime NotAfter {
            get {
                if (m_safeCertContext.IsInvalid)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 

                if (m_notAfter == DateTime.MinValue) { 
                    Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(); 
                    X509Utils._GetDateNotAfter(m_safeCertContext, ref fileTime);
                    m_notAfter = DateTime.FromFileTime(fileTime.ToTicks()); 
                }
                return m_notAfter;
            }
        } 

        private DateTime NotBefore { 
            get { 
                if (m_safeCertContext.IsInvalid)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 

                if (m_notBefore == DateTime.MinValue) {
                    Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME();
                    X509Utils._GetDateNotBefore(m_safeCertContext, ref fileTime); 
                    m_notBefore = DateTime.FromFileTime(fileTime.ToTicks());
                } 
                return m_notBefore; 
            }
        } 

        private byte[] RawData {
            get {
                if (m_safeCertContext.IsInvalid) 
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
 
                if (m_rawData == null) 
                    m_rawData = X509Utils._GetCertRawData(m_safeCertContext);
                return (byte[]) m_rawData.Clone(); 
            }
        }

        private string SerialNumber { 
            get {
                if (m_safeCertContext.IsInvalid) 
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 

                if (m_serialNumber == null) 
                    m_serialNumber = X509Utils._GetSerialNumber(m_safeCertContext);
                return Hex.EncodeHexStringFromInt(m_serialNumber);
            }
        } 

        private void SetThumbprint () { 
            if (m_safeCertContext.IsInvalid) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
 
            if (m_thumbprint == null)
                m_thumbprint = X509Utils._GetThumbprint(m_safeCertContext);
        }
 
        private byte[] ExportHelper (X509ContentType contentType, object password) {
            switch(contentType) { 
            case X509ContentType.Cert: 
            case X509ContentType.SerializedCert:
                break; 
            case X509ContentType.Pkcs12:
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.Open | KeyContainerPermissionFlags.Export);
                kp.Demand();
                break; 
            default:
                throw new CryptographicException(Environment.GetResourceString("Cryptography_X509_InvalidContentType")); 
            } 

            IntPtr szPassword = IntPtr.Zero; 
            byte[] encodedRawData = null;
            SafeCertStoreHandle safeCertStoreHandle = X509Utils.ExportCertToMemoryStore(this);

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                szPassword = X509Utils.PasswordToCoTaskMemUni(password); 
                encodedRawData = X509Utils._ExportCertificatesToBlob(safeCertStoreHandle, contentType, szPassword); 
            }
            finally { 
                if (szPassword != IntPtr.Zero)
                    Marshal.ZeroFreeCoTaskMemUnicode(szPassword);
                safeCertStoreHandle.Dispose();
            } 
            if (encodedRawData == null)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_X509_ExportFailed")); 
            return encodedRawData; 
        }
 
        private void LoadCertificateFromBlob (byte[] rawData, object password, X509KeyStorageFlags keyStorageFlags) {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EmptyOrNullArray"), "rawData");
 
            X509ContentType contentType = X509Utils.MapContentType(X509Utils._QueryCertBlobType(rawData));
            if (contentType == X509ContentType.Pkcs12 && 
                (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet) { 
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.Create);
                kp.Demand(); 
            }
            uint dwFlags = X509Utils.MapKeyStorageFlags(keyStorageFlags);
            IntPtr szPassword = IntPtr.Zero;
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                szPassword = X509Utils.PasswordToCoTaskMemUni(password); 
                X509Utils._LoadCertFromBlob(rawData,
                                            szPassword, 
                                            dwFlags,
                                            (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == 0 ? false : true,
                                            ref m_safeCertContext);
            } 
            finally {
                if (szPassword != IntPtr.Zero) 
                    Marshal.ZeroFreeCoTaskMemUnicode(szPassword); 
            }
        } 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private void LoadCertificateFromFile (string fileName, object password, X509KeyStorageFlags keyStorageFlags) { 
            if (fileName == null)
                throw new ArgumentNullException("fileName"); 
 
            string fullPath = Path.GetFullPathInternal(fileName);
            new FileIOPermission (FileIOPermissionAccess.Read, fullPath).Demand(); 
            X509ContentType contentType = X509Utils.MapContentType(X509Utils._QueryCertFileType(fileName));
            if (contentType == X509ContentType.Pkcs12 &&
                (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.Create); 
                kp.Demand();
            } 
            uint dwFlags = X509Utils.MapKeyStorageFlags(keyStorageFlags); 
            IntPtr szPassword = IntPtr.Zero;
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                szPassword = X509Utils.PasswordToCoTaskMemUni(password);
                X509Utils._LoadCertFromFile(fileName, 
                                            szPassword,
                                            dwFlags, 
                                            (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == 0 ? false : true, 
                                            ref m_safeCertContext);
            } 
            finally {
                if (szPassword != IntPtr.Zero)
                    Marshal.ZeroFreeCoTaskMemUnicode(szPassword);
            } 
        }
    } 
 
#endif // !FEATURE_PAL
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// X509Certificate.cs 
//
 
namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Win32;
    using System;
    using System.IO; 
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices; 
    using System.Runtime.Serialization; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Security.Util;
    using System.Text;
    using System.Runtime.Versioning;
 
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum X509ContentType { 
        Unknown         = 0x00, 
        Cert            = 0x01,
        SerializedCert  = 0x02, 
        Pfx             = 0x03,
        Pkcs12          = Pfx,
        SerializedStore = 0x04,
        Pkcs7           = 0x05, 
        Authenticode    = 0x06
    } 
 
    // DefaultKeySet, UserKeySet and MachineKeySet are mutually exclusive
    [Flags, Serializable()] 
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum X509KeyStorageFlags {
        DefaultKeySet = 0x00,
        UserKeySet    = 0x01, 
        MachineKeySet = 0x02,
        Exportable    = 0x04, 
        UserProtected = 0x08, 
        PersistKeySet = 0x10
    } 

#if !FEATURE_PAL

    [Serializable()] 
    [System.Runtime.InteropServices.ComVisible(true)]
    public class X509Certificate : IDeserializationCallback, ISerializable { 
        private const string m_format = "X509"; 
        private string m_subjectName;
        private string m_issuerName; 
        private byte[] m_serialNumber;
        private byte[] m_publicKeyParameters;
        private byte[] m_publicKeyValue;
        private string m_publicKeyOid; 
        private byte[] m_rawData;
        private byte[] m_thumbprint; 
        private DateTime m_notBefore; 
        private DateTime m_notAfter;
        private SafeCertContextHandle m_safeCertContext = SafeCertContextHandle.InvalidHandle; 

        //
        // public constructors
        // 

        public X509Certificate () {} 
 
        public X509Certificate (byte[] data) {
            if ((data != null) && (data.Length != 0)) 
                LoadCertificateFromBlob(data, null, X509KeyStorageFlags.DefaultKeySet);
        }

        public X509Certificate (byte[] rawData, string password) { 
            LoadCertificateFromBlob(rawData, password, X509KeyStorageFlags.DefaultKeySet);
        } 
 
        public X509Certificate (byte[] rawData, SecureString password) {
            LoadCertificateFromBlob(rawData, password, X509KeyStorageFlags.DefaultKeySet); 
        }

        public X509Certificate (byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags) {
            LoadCertificateFromBlob(rawData, password, keyStorageFlags); 
        }
 
        public X509Certificate (byte[] rawData, SecureString password, X509KeyStorageFlags keyStorageFlags) { 
            LoadCertificateFromBlob(rawData, password, keyStorageFlags);
        } 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public X509Certificate (string fileName) { 
            LoadCertificateFromFile(fileName, null, X509KeyStorageFlags.DefaultKeySet);
        } 
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public X509Certificate (string fileName, string password) {
            LoadCertificateFromFile(fileName, password, X509KeyStorageFlags.DefaultKeySet);
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public X509Certificate (string fileName, SecureString password) { 
            LoadCertificateFromFile(fileName, password, X509KeyStorageFlags.DefaultKeySet);
        } 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public X509Certificate (string fileName, string password, X509KeyStorageFlags keyStorageFlags) { 
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        } 
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public X509Certificate (string fileName, SecureString password, X509KeyStorageFlags keyStorageFlags) {
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        }
 
        // Package protected constructor for creating a certificate from a PCCERT_CONTEXT
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public X509Certificate (IntPtr handle) {
            if (handle == IntPtr.Zero) 
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidHandle"), "handle");

            X509Utils._DuplicateCertContext(handle, ref m_safeCertContext);
        } 

        public X509Certificate (X509Certificate cert) { 
            if (cert == null) 
                throw new ArgumentNullException("cert");
 
            if (cert.m_safeCertContext.pCertContext != IntPtr.Zero)
                X509Utils._DuplicateCertContext(cert.m_safeCertContext.pCertContext, ref this.m_safeCertContext);

            // we need to keep the certificate context alive until we are done with duplicating the handle. 
            GC.KeepAlive(cert.m_safeCertContext);
        } 
 
        public X509Certificate (SerializationInfo info, StreamingContext context) {
            byte[] rawData = (byte[]) info.GetValue("RawData", typeof(byte[])); 
            if (rawData != null)
                LoadCertificateFromBlob(rawData, null, X509KeyStorageFlags.DefaultKeySet);
        }
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public static X509Certificate CreateFromCertFile (string filename) { 
            return new X509Certificate(filename);
        } 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static X509Certificate CreateFromSignedFile (string filename) { 
            return new X509Certificate(filename);
        } 
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public IntPtr Handle { 
            [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            get {
                return m_safeCertContext.pCertContext; 
            }
        } 
 
        [Obsolete("This method has been deprecated.  Please use the Subject property instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetName() { 
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");

            return X509Utils._GetSubjectInfo(m_safeCertContext, X509Constants.CERT_NAME_RDN_TYPE, true); 
        }
 
        [Obsolete("This method has been deprecated.  Please use the Issuer property instead.  http://go.microsoft.com/fwlink/?linkid=14202")] 
        public virtual string GetIssuerName() {
            if (m_safeCertContext.IsInvalid) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");

            return X509Utils._GetIssuerName(m_safeCertContext, true);
        } 

        public virtual byte[] GetSerialNumber() { 
            if (m_safeCertContext.IsInvalid) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
 
            if (m_serialNumber == null)
                m_serialNumber = X509Utils._GetSerialNumber(m_safeCertContext);
            return (byte[]) m_serialNumber.Clone();
        } 

        public virtual string GetSerialNumberString() { 
            return SerialNumber; 
        }
 
        public virtual byte[] GetKeyAlgorithmParameters() {
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
 
            if (m_publicKeyParameters == null)
                m_publicKeyParameters = X509Utils._GetPublicKeyParameters(m_safeCertContext); 
 
            return (byte[]) m_publicKeyParameters.Clone();
        } 

        public virtual string GetKeyAlgorithmParametersString() {
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 

            return Hex.EncodeHexString(GetKeyAlgorithmParameters()); 
        } 

        public virtual string GetKeyAlgorithm() { 
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");

            if (m_publicKeyOid == null) 
                m_publicKeyOid = X509Utils._GetPublicKeyOid(m_safeCertContext);
 
            return m_publicKeyOid; 
        }
 
        public virtual byte[] GetPublicKey() {
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
 
            if (m_publicKeyValue == null)
                m_publicKeyValue = X509Utils._GetPublicKeyValue(m_safeCertContext); 
 
            return (byte[]) m_publicKeyValue.Clone();
        } 

        public virtual string GetPublicKeyString() {
            return Hex.EncodeHexString(GetPublicKey());
        } 

        public virtual byte[] GetRawCertData() { 
            return RawData; 
        }
 
        public virtual string GetRawCertDataString() {
            return Hex.EncodeHexString(GetRawCertData());
        }
 
        public virtual byte[] GetCertHash() {
            SetThumbprint(); 
            return (byte[]) m_thumbprint.Clone(); 
        }
 
        public virtual string GetCertHashString() {
            SetThumbprint();
            return Hex.EncodeHexString(m_thumbprint);
        } 

        public virtual string GetEffectiveDateString() { 
            return NotBefore.ToString(); 
        }
 
        public virtual string GetExpirationDateString() {
            return NotAfter.ToString();
        }
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool Equals (Object obj) { 
            if (!(obj is X509Certificate)) return false; 
            X509Certificate other = (X509Certificate) obj;
            return this.Equals(other); 
        }

        public virtual bool Equals (X509Certificate other) {
            if (other == null) 
                return false;
 
            if (m_safeCertContext.IsInvalid) 
                return other.m_safeCertContext.IsInvalid;
 
            if (!this.Issuer.Equals(other.Issuer))
                return false;

            if (!this.SerialNumber.Equals(other.SerialNumber)) 
                return false;
 
            return true; 
        }
 
        public override int GetHashCode() {
            if (m_safeCertContext.IsInvalid)
                return 0;
 
            SetThumbprint();
            int value = 0; 
            for (int i = 0; i < m_thumbprint.Length && i < 4; ++i) { 
                value = value << 8 | m_thumbprint[i];
            } 
            return value;
        }

        public override string ToString() { 
            return ToString(false);
        } 
 
        public virtual string ToString (bool fVerbose) {
            if (fVerbose == false || m_safeCertContext.IsInvalid) 
                return GetType().FullName;

            StringBuilder sb = new StringBuilder();
 
            // Subject
            sb.Append("[Subject]" + Environment.NewLine + "  "); 
            sb.Append(this.Subject); 

            // Issuer 
            sb.Append(Environment.NewLine + Environment.NewLine + "[Issuer]" + Environment.NewLine + "  ");
            sb.Append(this.Issuer);

            // Serial Number 
            sb.Append(Environment.NewLine + Environment.NewLine + "[Serial Number]" + Environment.NewLine + "  ");
            sb.Append(this.SerialNumber); 
 
            // NotBefore
            sb.Append(Environment.NewLine + Environment.NewLine + "[Not Before]" + Environment.NewLine + "  "); 
            sb.Append(this.NotBefore);

            // NotAfter
            sb.Append(Environment.NewLine + Environment.NewLine + "[Not After]" + Environment.NewLine + "  "); 
            sb.Append(this.NotAfter);
 
            // Thumbprint 
            sb.Append(Environment.NewLine + Environment.NewLine + "[Thumbprint]" + Environment.NewLine + "  ");
            sb.Append(this.GetCertHashString()); 

            sb.Append(Environment.NewLine);
            return sb.ToString();
        } 

        public virtual string GetFormat() { 
            return m_format; 
        }
 
        public string Issuer {
            get {
                if (m_safeCertContext.IsInvalid)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 

                if (m_issuerName == null) 
                    m_issuerName = X509Utils._GetIssuerName(m_safeCertContext, false); 
                return m_issuerName;
            } 
        }

        public string Subject {
            get { 
                if (m_safeCertContext.IsInvalid)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 
 
                if (m_subjectName == null)
                    m_subjectName = X509Utils._GetSubjectInfo(m_safeCertContext, X509Constants.CERT_NAME_RDN_TYPE, false); 
                return m_subjectName;
            }
        }
 
        [System.Runtime.InteropServices.ComVisible(false)]
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)] 
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)] 
        public virtual void Import(byte[] rawData) {
            Reset(); 
            LoadCertificateFromBlob(rawData, null, X509KeyStorageFlags.DefaultKeySet);
        }

        [System.Runtime.InteropServices.ComVisible(false)] 
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)] 
        public virtual void Import(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags) { 
            Reset();
            LoadCertificateFromBlob(rawData, password, keyStorageFlags); 
        }

        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)] 
        public virtual void Import(byte[] rawData, SecureString password, X509KeyStorageFlags keyStorageFlags) {
            Reset(); 
            LoadCertificateFromBlob(rawData, password, keyStorageFlags); 
        }
 
        [System.Runtime.InteropServices.ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)] 
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
        public virtual void Import(string fileName) { 
            Reset(); 
            LoadCertificateFromFile(fileName, null, X509KeyStorageFlags.DefaultKeySet);
        } 

        [System.Runtime.InteropServices.ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)] 
        public virtual void Import(string fileName, string password, X509KeyStorageFlags keyStorageFlags) { 
            Reset();
            LoadCertificateFromFile(fileName, password, keyStorageFlags); 
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)]
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)] 
        public virtual void Import(string fileName, SecureString password, X509KeyStorageFlags keyStorageFlags) { 
            Reset();
            LoadCertificateFromFile(fileName, password, keyStorageFlags); 
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual byte[] Export(X509ContentType contentType) { 
            return ExportHelper(contentType, null);
        } 
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual byte[] Export(X509ContentType contentType, string password) { 
            return ExportHelper(contentType, password);
        }

        public virtual byte[] Export(X509ContentType contentType, SecureString password) { 
            return ExportHelper(contentType, password);
        } 
 
        [System.Runtime.InteropServices.ComVisible(false)]
        [PermissionSetAttribute(SecurityAction.LinkDemand, Unrestricted=true)] 
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
        public virtual void Reset () {
            m_subjectName = null;
            m_issuerName = null; 
            m_serialNumber = null;
            m_publicKeyParameters = null; 
            m_publicKeyValue = null; 
            m_publicKeyOid = null;
            m_rawData = null; 
            m_thumbprint = null;
            m_notBefore = DateTime.MinValue;
            m_notAfter = DateTime.MinValue;
            if (!m_safeCertContext.IsInvalid) { 
                // Free the current certificate handle
                m_safeCertContext.Dispose(); 
                m_safeCertContext = SafeCertContextHandle.InvalidHandle; 
            }
        } 

        /// <internalonly/>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context) { 
            if (m_safeCertContext.IsInvalid)
                info.AddValue("RawData", null); 
            else 
                info.AddValue("RawData", this.RawData);
        } 

        /// <internalonly/>
        void IDeserializationCallback.OnDeserialization(Object sender) {}
 
        //
        // internal. 
        // 

        internal SafeCertContextHandle CertContext { 
            get {
                return m_safeCertContext;
            }
        } 

        // 
        // private methods. 
        //
 
        private DateTime NotAfter {
            get {
                if (m_safeCertContext.IsInvalid)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 

                if (m_notAfter == DateTime.MinValue) { 
                    Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(); 
                    X509Utils._GetDateNotAfter(m_safeCertContext, ref fileTime);
                    m_notAfter = DateTime.FromFileTime(fileTime.ToTicks()); 
                }
                return m_notAfter;
            }
        } 

        private DateTime NotBefore { 
            get { 
                if (m_safeCertContext.IsInvalid)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 

                if (m_notBefore == DateTime.MinValue) {
                    Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME();
                    X509Utils._GetDateNotBefore(m_safeCertContext, ref fileTime); 
                    m_notBefore = DateTime.FromFileTime(fileTime.ToTicks());
                } 
                return m_notBefore; 
            }
        } 

        private byte[] RawData {
            get {
                if (m_safeCertContext.IsInvalid) 
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
 
                if (m_rawData == null) 
                    m_rawData = X509Utils._GetCertRawData(m_safeCertContext);
                return (byte[]) m_rawData.Clone(); 
            }
        }

        private string SerialNumber { 
            get {
                if (m_safeCertContext.IsInvalid) 
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext"); 

                if (m_serialNumber == null) 
                    m_serialNumber = X509Utils._GetSerialNumber(m_safeCertContext);
                return Hex.EncodeHexStringFromInt(m_serialNumber);
            }
        } 

        private void SetThumbprint () { 
            if (m_safeCertContext.IsInvalid) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
 
            if (m_thumbprint == null)
                m_thumbprint = X509Utils._GetThumbprint(m_safeCertContext);
        }
 
        private byte[] ExportHelper (X509ContentType contentType, object password) {
            switch(contentType) { 
            case X509ContentType.Cert: 
            case X509ContentType.SerializedCert:
                break; 
            case X509ContentType.Pkcs12:
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.Open | KeyContainerPermissionFlags.Export);
                kp.Demand();
                break; 
            default:
                throw new CryptographicException(Environment.GetResourceString("Cryptography_X509_InvalidContentType")); 
            } 

            IntPtr szPassword = IntPtr.Zero; 
            byte[] encodedRawData = null;
            SafeCertStoreHandle safeCertStoreHandle = X509Utils.ExportCertToMemoryStore(this);

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                szPassword = X509Utils.PasswordToCoTaskMemUni(password); 
                encodedRawData = X509Utils._ExportCertificatesToBlob(safeCertStoreHandle, contentType, szPassword); 
            }
            finally { 
                if (szPassword != IntPtr.Zero)
                    Marshal.ZeroFreeCoTaskMemUnicode(szPassword);
                safeCertStoreHandle.Dispose();
            } 
            if (encodedRawData == null)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_X509_ExportFailed")); 
            return encodedRawData; 
        }
 
        private void LoadCertificateFromBlob (byte[] rawData, object password, X509KeyStorageFlags keyStorageFlags) {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EmptyOrNullArray"), "rawData");
 
            X509ContentType contentType = X509Utils.MapContentType(X509Utils._QueryCertBlobType(rawData));
            if (contentType == X509ContentType.Pkcs12 && 
                (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet) { 
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.Create);
                kp.Demand(); 
            }
            uint dwFlags = X509Utils.MapKeyStorageFlags(keyStorageFlags);
            IntPtr szPassword = IntPtr.Zero;
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                szPassword = X509Utils.PasswordToCoTaskMemUni(password); 
                X509Utils._LoadCertFromBlob(rawData,
                                            szPassword, 
                                            dwFlags,
                                            (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == 0 ? false : true,
                                            ref m_safeCertContext);
            } 
            finally {
                if (szPassword != IntPtr.Zero) 
                    Marshal.ZeroFreeCoTaskMemUnicode(szPassword); 
            }
        } 

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private void LoadCertificateFromFile (string fileName, object password, X509KeyStorageFlags keyStorageFlags) { 
            if (fileName == null)
                throw new ArgumentNullException("fileName"); 
 
            string fullPath = Path.GetFullPathInternal(fileName);
            new FileIOPermission (FileIOPermissionAccess.Read, fullPath).Demand(); 
            X509ContentType contentType = X509Utils.MapContentType(X509Utils._QueryCertFileType(fileName));
            if (contentType == X509ContentType.Pkcs12 &&
                (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.Create); 
                kp.Demand();
            } 
            uint dwFlags = X509Utils.MapKeyStorageFlags(keyStorageFlags); 
            IntPtr szPassword = IntPtr.Zero;
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                szPassword = X509Utils.PasswordToCoTaskMemUni(password);
                X509Utils._LoadCertFromFile(fileName, 
                                            szPassword,
                                            dwFlags, 
                                            (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == 0 ? false : true, 
                                            ref m_safeCertContext);
            } 
            finally {
                if (szPassword != IntPtr.Zero)
                    Marshal.ZeroFreeCoTaskMemUnicode(szPassword);
            } 
        }
    } 
 
#endif // !FEATURE_PAL
} 
