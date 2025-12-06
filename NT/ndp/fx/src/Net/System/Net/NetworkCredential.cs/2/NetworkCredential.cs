//------------------------------------------------------------------------------ 
// <copyright file="NetworkCredential.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
 
    using System.IO;
    using System.Runtime.InteropServices; 
    using System.Security.Cryptography;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading; 
    using Microsoft.Win32;
 
 
    /// <devdoc>
    ///    <para>Provides credentials for password-based 
    ///       authentication schemes such as basic, digest, NTLM and Kerberos.</para>
    /// </devdoc>
    public class NetworkCredential : ICredentials,ICredentialsByHost {
 
        private static EnvironmentPermission m_environmentUserNamePermission;
        private static EnvironmentPermission m_environmentDomainNamePermission; 
        private static readonly object lockingObject = new object(); 
#if !FEATURE_PAL
        private static SymmetricAlgorithm s_symmetricAlgorithm; 
        private static RNGCryptoServiceProvider s_random;
        private static bool s_useTripleDES = false;

 
        private byte[] m_userName;
        private byte[] m_password; 
        private byte[] m_domain; 
        private byte[] m_encryptionIV;
        private bool m_encrypt = true; 
#else  //FEATURE_PAL
        private string m_userName;
        private string m_password;
        private string m_domain; 
#endif //FEATURE_PAL
 
 
        public NetworkCredential() {
        } 

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Net.NetworkCredential'/> 
        ///       class with name and password set as specified.
        ///    </para> 
        /// </devdoc> 
        public NetworkCredential(string userName, string password)
        : this(userName, password, string.Empty) { 
        }

        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.Net.NetworkCredential'/>
        ///       class with name and password set as specified. 
        ///    </para> 
        /// </devdoc>
        public NetworkCredential(string userName, string password, string domain) : this(userName, password, domain, true) { 
        }

        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.Net.NetworkCredential'/>
        ///       class with name and password set as specified. 
        ///    </para> 
        /// </devdoc>
 

        internal NetworkCredential(string userName, string password, string domain, bool encrypt) {
#if !FEATURE_PAL
            m_encrypt = encrypt; 
#endif
            UserName = userName; 
            Password = password; 
            Domain = domain;
        } 


        void InitializePart1() {
            if (m_environmentUserNamePermission == null) { 
                lock(lockingObject) {
                    if (m_environmentUserNamePermission == null) { 
                        m_environmentDomainNamePermission = new EnvironmentPermission(EnvironmentPermissionAccess.Read, "USERDOMAIN"); 
                        m_environmentUserNamePermission = new EnvironmentPermission(EnvironmentPermissionAccess.Read, "USERNAME");
                    } 
                }
            }
        }
 

        void InitializePart2(){ 
 
#if !FEATURE_PAL
            if (m_encrypt) { 
                if (s_symmetricAlgorithm == null) {
                    lock(lockingObject) {
                        if (s_symmetricAlgorithm == null)
                        { 
                            SymmetricAlgorithm algorithm;
 
                            s_useTripleDES = ReadRegFips(); 

                            if (s_useTripleDES) 
                            {
                                algorithm = new TripleDESCryptoServiceProvider();
                                algorithm.KeySize = 128;
                                algorithm.GenerateKey(); 
                            }
                            else 
                            { 
                                s_random = new RNGCryptoServiceProvider();
                                algorithm = Rijndael.Create(); 
                                byte[] encryptionKey = new byte[16];
                                s_random.GetBytes(encryptionKey);
                                algorithm.Key = encryptionKey;
                            } 
                            s_symmetricAlgorithm = algorithm;
                        } 
                    } 
                }
                if (m_encryptionIV == null) { 
                    if (s_useTripleDES) {
                        s_symmetricAlgorithm.GenerateIV();
                        byte[] tempIV = s_symmetricAlgorithm.IV;
                        Interlocked.CompareExchange(ref m_encryptionIV, tempIV, null); 
                    }
                    else{ 
                        byte[] tmp = new byte[16]; 
                        s_random.GetBytes(tmp);
                        Interlocked.CompareExchange(ref m_encryptionIV, tmp, null); 
                    }
                }
            }
 

#endif // !FEATURE_PAL 
        } 

        /// <devdoc> 
        ///    <para>
        ///       The user name associated with this credential.
        ///    </para>
        /// </devdoc> 
        public string UserName {
            get { 
                InitializePart1(); 
                m_environmentUserNamePermission.Demand();
                return InternalGetUserName(); 
            }
            set {
#if FEATURE_PAL
                m_userName = value; 
                // GlobalLog.Print("NetworkCredential::set_UserName: m_userName: \"" + m_userName + "\"" );
#else //!FEATURE_PAL 
                m_userName = Encrypt(value); 
                // GlobalLog.Print("NetworkCredential::set_UserName: value = " + value);
                // GlobalLog.Print("NetworkCredential::set_UserName: m_userName:"); 
                // GlobalLog.Dump(m_userName);
#endif //!FEATURE_PAL
            }
        } 

        /// <devdoc> 
        ///    <para> 
        ///       The password for the user name.
        ///    </para> 
        /// </devdoc>
        public string Password {
            get {
                ExceptionHelper.UnmanagedPermission.Demand(); 
                return InternalGetPassword();
            } 
            set { 
#if FEATURE_PAL
                m_password = value; 
//                GlobalLog.Print("NetworkCredential::set_Password: m_password: \"" + m_password + "\"" );
#else //!FEATURE_PAL
                m_password = Encrypt(value);
//                GlobalLog.Print("NetworkCredential::set_Password: value = " + value); 
//                GlobalLog.Print("NetworkCredential::set_Password: m_password:");
//                GlobalLog.Dump(m_password); 
#endif //!FEATURE_PAL 
            }
        } 

        /// <devdoc>
        ///    <para>
        ///       The machine name that verifies 
        ///       the credentials. Usually this is the host machine.
        ///    </para> 
        /// </devdoc> 
        public string Domain {
            get { 
                InitializePart1();
                m_environmentDomainNamePermission.Demand();
                return InternalGetDomain();
            } 
            set {
#if FEATURE_PAL 
                m_domain = value; 
//                GlobalLog.Print("NetworkCredential::set_Domain: m_domain: \"" + m_domain + "\"" );
#else //!FEATURE_PAL 
                m_domain = Encrypt(value);
//                GlobalLog.Print("NetworkCredential::set_Domain: value = " + value);
//                GlobalLog.Print("NetworkCredential::set_Domain: m_domain:");
//                GlobalLog.Dump(m_domain); 
#endif //!FEATURE_PAL
            } 
        } 

        internal string InternalGetUserName() { 
#if FEATURE_PAL
            // GlobalLog.Print("NetworkCredential::get_UserName: returning \"" + m_userName + "\"");
            return m_userName;
#else //!FEATURE_PAL 
            string decryptedString = Decrypt(m_userName);
 
//          GlobalLog.Print("NetworkCredential::get_UserName: returning \"" + decryptedString + "\""); 
            return decryptedString;
#endif //!FEATURE_PAL 
        }

        internal string InternalGetPassword() {
#if FEATURE_PAL 
            // GlobalLog.Print("NetworkCredential::get_Password: returning \"" + m_password + "\"");
            return m_password; 
#else //!FEATURE_PAL 
            string decryptedString = Decrypt(m_password);
 
            // GlobalLog.Print("NetworkCredential::get_Password: returning \"" + decryptedString + "\"");
            return decryptedString;
#endif //!FEATURE_PAL
        } 

        internal string InternalGetDomain() { 
#if FEATURE_PAL 

            // GlobalLog.Print("NetworkCredential::get_Domain: returning \"" + m_domain + "\""); 
            return m_domain;
#else //!FEATURE_PAL

            string decryptedString = Decrypt(m_domain); 

            // GlobalLog.Print("NetworkCredential::get_Domain: returning \"" + decryptedString + "\""); 
            return decryptedString; 
#endif //!FEATURE_PAL
        } 

        internal string InternalGetDomainUserName() {
            string domainUserName = InternalGetDomain();
            if (domainUserName.Length != 0) 
                domainUserName += "\\";
            domainUserName += InternalGetUserName(); 
            return domainUserName; 
        }
 
        /// <devdoc>
        ///    <para>
        ///       Returns an instance of the NetworkCredential class for a Uri and
        ///       authentication type. 
        ///    </para>
        /// </devdoc> 
        public NetworkCredential GetCredential(Uri uri, String authType) { 
            return this;
        } 

        public NetworkCredential GetCredential(string host, int port, String authenticationType) {
            return this;
        } 

        internal bool IsEqualTo(object compObject) { 
            if ((object)compObject == null) 
                return false;
            if ((object)this == (object)compObject) 
                return true;
            NetworkCredential compCred = compObject as NetworkCredential;
            if ((object)compCred == null)
                return false; 
            return(InternalGetUserName() == compCred.InternalGetUserName() &&
                   InternalGetPassword() == compCred.InternalGetPassword() && 
                   InternalGetDomain()  == compCred.InternalGetDomain()); 
        }
 

#if !FEATURE_PAL
        internal string Decrypt(byte[] ciphertext) {
            if (ciphertext == null) { 
                return String.Empty;
            } 
 
            if (!m_encrypt)
                return Encoding.UTF8.GetString(ciphertext); 

            InitializePart2();
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, s_symmetricAlgorithm.CreateDecryptor(s_symmetricAlgorithm.Key, m_encryptionIV), CryptoStreamMode.Write); 

            cs.Write(ciphertext, 0, ciphertext.Length); 
            cs.FlushFinalBlock(); 

            byte[] decryptedBytes = ms.ToArray(); 

            cs.Close();
            return Encoding.UTF8.GetString(decryptedBytes);
        } 

        internal byte[] Encrypt(string text) { 
            if ((text == null) || (text.Length == 0)) { 
                return null;
            } 

            if (!m_encrypt)
                return Encoding.UTF8.GetBytes(text);
 
            InitializePart2();
            MemoryStream ms = new MemoryStream(); 
            CryptoStream cs = new CryptoStream(ms, s_symmetricAlgorithm.CreateEncryptor(s_symmetricAlgorithm.Key, m_encryptionIV), CryptoStreamMode.Write); 

            byte[] stringBytes = Encoding.UTF8.GetBytes(text); 

            cs.Write(stringBytes, 0, stringBytes.Length);
            cs.FlushFinalBlock();
            stringBytes = ms.ToArray(); 
            cs.Close();
            return stringBytes; 
        } 

        [RegistryPermission(SecurityAction.Assert, Read="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Lsa")] 
        private bool ReadRegFips() {
            bool readPolicy = false;
            bool fipsEnabled = false;
            if (ComNetOS.IsVista) { 
                // For Vista and later OS - use bcrypt.dll function
                uint policyReadStatus = UnsafeNclNativeMethods.BCryptGetFipsAlgorithmMode(out fipsEnabled); 
 
                // Status other than SUCCESS or NOT_FOUND means policy store (registry) could not be accessed
                // fipsEnabled is output as false when NOT_FOUND 
                readPolicy = policyReadStatus == UnsafeNclNativeMethods.NTStatus.STATUS_SUCCESS ||
                             policyReadStatus == UnsafeNclNativeMethods.NTStatus.STATUS_OBJECT_NAME_NOT_FOUND;
             } else {
                // Not Vista or later OS - read legacy registry value 
                RegistryKey key = null;
                object value = null; 
                try { 
                    string subKey = "SYSTEM\\CurrentControlSet\\Control\\Lsa";
                    key = Registry.LocalMachine.OpenSubKey(subKey); 
                    if (null != key) {
                        value = key.GetValue("fipsalgorithmpolicy");
                    }
                    readPolicy = true; 
                    if (value != null && (int) value == 1) {
                        fipsEnabled = true; 
                    } 
                }
                catch { 
                    // consume exceptions and just leave readPolicy == false
                }
                finally{
                    // close reg key 
                    if (null != key) {
                        key.Close(); 
                    } 
                }
            } 

            // Default to true if registry couldn't be accessed
            if (!readPolicy || fipsEnabled) {
                return true; 
            } else {
                return false; 
            } 
        }
 

#endif //!FEATURE_PAL
    } // class NetworkCredential
} // namespace System.Net 
//------------------------------------------------------------------------------ 
// <copyright file="NetworkCredential.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
 
    using System.IO;
    using System.Runtime.InteropServices; 
    using System.Security.Cryptography;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading; 
    using Microsoft.Win32;
 
 
    /// <devdoc>
    ///    <para>Provides credentials for password-based 
    ///       authentication schemes such as basic, digest, NTLM and Kerberos.</para>
    /// </devdoc>
    public class NetworkCredential : ICredentials,ICredentialsByHost {
 
        private static EnvironmentPermission m_environmentUserNamePermission;
        private static EnvironmentPermission m_environmentDomainNamePermission; 
        private static readonly object lockingObject = new object(); 
#if !FEATURE_PAL
        private static SymmetricAlgorithm s_symmetricAlgorithm; 
        private static RNGCryptoServiceProvider s_random;
        private static bool s_useTripleDES = false;

 
        private byte[] m_userName;
        private byte[] m_password; 
        private byte[] m_domain; 
        private byte[] m_encryptionIV;
        private bool m_encrypt = true; 
#else  //FEATURE_PAL
        private string m_userName;
        private string m_password;
        private string m_domain; 
#endif //FEATURE_PAL
 
 
        public NetworkCredential() {
        } 

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Net.NetworkCredential'/> 
        ///       class with name and password set as specified.
        ///    </para> 
        /// </devdoc> 
        public NetworkCredential(string userName, string password)
        : this(userName, password, string.Empty) { 
        }

        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.Net.NetworkCredential'/>
        ///       class with name and password set as specified. 
        ///    </para> 
        /// </devdoc>
        public NetworkCredential(string userName, string password, string domain) : this(userName, password, domain, true) { 
        }

        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.Net.NetworkCredential'/>
        ///       class with name and password set as specified. 
        ///    </para> 
        /// </devdoc>
 

        internal NetworkCredential(string userName, string password, string domain, bool encrypt) {
#if !FEATURE_PAL
            m_encrypt = encrypt; 
#endif
            UserName = userName; 
            Password = password; 
            Domain = domain;
        } 


        void InitializePart1() {
            if (m_environmentUserNamePermission == null) { 
                lock(lockingObject) {
                    if (m_environmentUserNamePermission == null) { 
                        m_environmentDomainNamePermission = new EnvironmentPermission(EnvironmentPermissionAccess.Read, "USERDOMAIN"); 
                        m_environmentUserNamePermission = new EnvironmentPermission(EnvironmentPermissionAccess.Read, "USERNAME");
                    } 
                }
            }
        }
 

        void InitializePart2(){ 
 
#if !FEATURE_PAL
            if (m_encrypt) { 
                if (s_symmetricAlgorithm == null) {
                    lock(lockingObject) {
                        if (s_symmetricAlgorithm == null)
                        { 
                            SymmetricAlgorithm algorithm;
 
                            s_useTripleDES = ReadRegFips(); 

                            if (s_useTripleDES) 
                            {
                                algorithm = new TripleDESCryptoServiceProvider();
                                algorithm.KeySize = 128;
                                algorithm.GenerateKey(); 
                            }
                            else 
                            { 
                                s_random = new RNGCryptoServiceProvider();
                                algorithm = Rijndael.Create(); 
                                byte[] encryptionKey = new byte[16];
                                s_random.GetBytes(encryptionKey);
                                algorithm.Key = encryptionKey;
                            } 
                            s_symmetricAlgorithm = algorithm;
                        } 
                    } 
                }
                if (m_encryptionIV == null) { 
                    if (s_useTripleDES) {
                        s_symmetricAlgorithm.GenerateIV();
                        byte[] tempIV = s_symmetricAlgorithm.IV;
                        Interlocked.CompareExchange(ref m_encryptionIV, tempIV, null); 
                    }
                    else{ 
                        byte[] tmp = new byte[16]; 
                        s_random.GetBytes(tmp);
                        Interlocked.CompareExchange(ref m_encryptionIV, tmp, null); 
                    }
                }
            }
 

#endif // !FEATURE_PAL 
        } 

        /// <devdoc> 
        ///    <para>
        ///       The user name associated with this credential.
        ///    </para>
        /// </devdoc> 
        public string UserName {
            get { 
                InitializePart1(); 
                m_environmentUserNamePermission.Demand();
                return InternalGetUserName(); 
            }
            set {
#if FEATURE_PAL
                m_userName = value; 
                // GlobalLog.Print("NetworkCredential::set_UserName: m_userName: \"" + m_userName + "\"" );
#else //!FEATURE_PAL 
                m_userName = Encrypt(value); 
                // GlobalLog.Print("NetworkCredential::set_UserName: value = " + value);
                // GlobalLog.Print("NetworkCredential::set_UserName: m_userName:"); 
                // GlobalLog.Dump(m_userName);
#endif //!FEATURE_PAL
            }
        } 

        /// <devdoc> 
        ///    <para> 
        ///       The password for the user name.
        ///    </para> 
        /// </devdoc>
        public string Password {
            get {
                ExceptionHelper.UnmanagedPermission.Demand(); 
                return InternalGetPassword();
            } 
            set { 
#if FEATURE_PAL
                m_password = value; 
//                GlobalLog.Print("NetworkCredential::set_Password: m_password: \"" + m_password + "\"" );
#else //!FEATURE_PAL
                m_password = Encrypt(value);
//                GlobalLog.Print("NetworkCredential::set_Password: value = " + value); 
//                GlobalLog.Print("NetworkCredential::set_Password: m_password:");
//                GlobalLog.Dump(m_password); 
#endif //!FEATURE_PAL 
            }
        } 

        /// <devdoc>
        ///    <para>
        ///       The machine name that verifies 
        ///       the credentials. Usually this is the host machine.
        ///    </para> 
        /// </devdoc> 
        public string Domain {
            get { 
                InitializePart1();
                m_environmentDomainNamePermission.Demand();
                return InternalGetDomain();
            } 
            set {
#if FEATURE_PAL 
                m_domain = value; 
//                GlobalLog.Print("NetworkCredential::set_Domain: m_domain: \"" + m_domain + "\"" );
#else //!FEATURE_PAL 
                m_domain = Encrypt(value);
//                GlobalLog.Print("NetworkCredential::set_Domain: value = " + value);
//                GlobalLog.Print("NetworkCredential::set_Domain: m_domain:");
//                GlobalLog.Dump(m_domain); 
#endif //!FEATURE_PAL
            } 
        } 

        internal string InternalGetUserName() { 
#if FEATURE_PAL
            // GlobalLog.Print("NetworkCredential::get_UserName: returning \"" + m_userName + "\"");
            return m_userName;
#else //!FEATURE_PAL 
            string decryptedString = Decrypt(m_userName);
 
//          GlobalLog.Print("NetworkCredential::get_UserName: returning \"" + decryptedString + "\""); 
            return decryptedString;
#endif //!FEATURE_PAL 
        }

        internal string InternalGetPassword() {
#if FEATURE_PAL 
            // GlobalLog.Print("NetworkCredential::get_Password: returning \"" + m_password + "\"");
            return m_password; 
#else //!FEATURE_PAL 
            string decryptedString = Decrypt(m_password);
 
            // GlobalLog.Print("NetworkCredential::get_Password: returning \"" + decryptedString + "\"");
            return decryptedString;
#endif //!FEATURE_PAL
        } 

        internal string InternalGetDomain() { 
#if FEATURE_PAL 

            // GlobalLog.Print("NetworkCredential::get_Domain: returning \"" + m_domain + "\""); 
            return m_domain;
#else //!FEATURE_PAL

            string decryptedString = Decrypt(m_domain); 

            // GlobalLog.Print("NetworkCredential::get_Domain: returning \"" + decryptedString + "\""); 
            return decryptedString; 
#endif //!FEATURE_PAL
        } 

        internal string InternalGetDomainUserName() {
            string domainUserName = InternalGetDomain();
            if (domainUserName.Length != 0) 
                domainUserName += "\\";
            domainUserName += InternalGetUserName(); 
            return domainUserName; 
        }
 
        /// <devdoc>
        ///    <para>
        ///       Returns an instance of the NetworkCredential class for a Uri and
        ///       authentication type. 
        ///    </para>
        /// </devdoc> 
        public NetworkCredential GetCredential(Uri uri, String authType) { 
            return this;
        } 

        public NetworkCredential GetCredential(string host, int port, String authenticationType) {
            return this;
        } 

        internal bool IsEqualTo(object compObject) { 
            if ((object)compObject == null) 
                return false;
            if ((object)this == (object)compObject) 
                return true;
            NetworkCredential compCred = compObject as NetworkCredential;
            if ((object)compCred == null)
                return false; 
            return(InternalGetUserName() == compCred.InternalGetUserName() &&
                   InternalGetPassword() == compCred.InternalGetPassword() && 
                   InternalGetDomain()  == compCred.InternalGetDomain()); 
        }
 

#if !FEATURE_PAL
        internal string Decrypt(byte[] ciphertext) {
            if (ciphertext == null) { 
                return String.Empty;
            } 
 
            if (!m_encrypt)
                return Encoding.UTF8.GetString(ciphertext); 

            InitializePart2();
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, s_symmetricAlgorithm.CreateDecryptor(s_symmetricAlgorithm.Key, m_encryptionIV), CryptoStreamMode.Write); 

            cs.Write(ciphertext, 0, ciphertext.Length); 
            cs.FlushFinalBlock(); 

            byte[] decryptedBytes = ms.ToArray(); 

            cs.Close();
            return Encoding.UTF8.GetString(decryptedBytes);
        } 

        internal byte[] Encrypt(string text) { 
            if ((text == null) || (text.Length == 0)) { 
                return null;
            } 

            if (!m_encrypt)
                return Encoding.UTF8.GetBytes(text);
 
            InitializePart2();
            MemoryStream ms = new MemoryStream(); 
            CryptoStream cs = new CryptoStream(ms, s_symmetricAlgorithm.CreateEncryptor(s_symmetricAlgorithm.Key, m_encryptionIV), CryptoStreamMode.Write); 

            byte[] stringBytes = Encoding.UTF8.GetBytes(text); 

            cs.Write(stringBytes, 0, stringBytes.Length);
            cs.FlushFinalBlock();
            stringBytes = ms.ToArray(); 
            cs.Close();
            return stringBytes; 
        } 

        [RegistryPermission(SecurityAction.Assert, Read="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Lsa")] 
        private bool ReadRegFips() {
            bool readPolicy = false;
            bool fipsEnabled = false;
            if (ComNetOS.IsVista) { 
                // For Vista and later OS - use bcrypt.dll function
                uint policyReadStatus = UnsafeNclNativeMethods.BCryptGetFipsAlgorithmMode(out fipsEnabled); 
 
                // Status other than SUCCESS or NOT_FOUND means policy store (registry) could not be accessed
                // fipsEnabled is output as false when NOT_FOUND 
                readPolicy = policyReadStatus == UnsafeNclNativeMethods.NTStatus.STATUS_SUCCESS ||
                             policyReadStatus == UnsafeNclNativeMethods.NTStatus.STATUS_OBJECT_NAME_NOT_FOUND;
             } else {
                // Not Vista or later OS - read legacy registry value 
                RegistryKey key = null;
                object value = null; 
                try { 
                    string subKey = "SYSTEM\\CurrentControlSet\\Control\\Lsa";
                    key = Registry.LocalMachine.OpenSubKey(subKey); 
                    if (null != key) {
                        value = key.GetValue("fipsalgorithmpolicy");
                    }
                    readPolicy = true; 
                    if (value != null && (int) value == 1) {
                        fipsEnabled = true; 
                    } 
                }
                catch { 
                    // consume exceptions and just leave readPolicy == false
                }
                finally{
                    // close reg key 
                    if (null != key) {
                        key.Close(); 
                    } 
                }
            } 

            // Default to true if registry couldn't be accessed
            if (!readPolicy || fipsEnabled) {
                return true; 
            } else {
                return false; 
            } 
        }
 

#endif //!FEATURE_PAL
    } // class NetworkCredential
} // namespace System.Net 
