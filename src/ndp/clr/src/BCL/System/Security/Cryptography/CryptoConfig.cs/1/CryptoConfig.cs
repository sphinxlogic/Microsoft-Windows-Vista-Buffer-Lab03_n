// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// CryptoConfig.cs 
//
 
namespace System.Security.Cryptography {
    using System;
    using System.Collections;
    using System.IO; 
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates; 
    using System.Security.Permissions; 
    using System.Threading;
    using System.Globalization; 
    using System.Runtime.Versioning;

    [System.Runtime.InteropServices.ComVisible(true)]
    public class CryptoConfig { 
        private static Hashtable defaultOidHT = null;
        private static Hashtable defaultNameHT = null; 
 
        private static string machineConfigDir = System.Security.Util.Config.MachineDirectory;
        private static Hashtable machineOidHT = null; 
        private static Hashtable machineNameHT = null;
        private static string machineConfigFilename = "machine.config";
        private static bool isInitialized = false;
 
        private static string _Version = null;
 
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
 
        private static Hashtable DefaultOidHT {
            get {
                if (defaultOidHT == null) {
                    Hashtable ht = new Hashtable(StringComparer.OrdinalIgnoreCase); 
                    ht.Add("SHA", Constants.OID_OIWSEC_SHA1);
                    ht.Add("SHA1", Constants.OID_OIWSEC_SHA1); 
                    ht.Add("System.Security.Cryptography.SHA1", Constants.OID_OIWSEC_SHA1); 
                    ht.Add("System.Security.Cryptography.SHA1CryptoServiceProvider", Constants.OID_OIWSEC_SHA1);
                    ht.Add("System.Security.Cryptography.SHA1Managed", Constants.OID_OIWSEC_SHA1); 
                    ht.Add("SHA256", Constants.OID_OIWSEC_SHA256);
                    ht.Add("System.Security.Cryptography.SHA256", Constants.OID_OIWSEC_SHA256);
                    ht.Add("System.Security.Cryptography.SHA256Managed", Constants.OID_OIWSEC_SHA256);
                    ht.Add("SHA384", Constants.OID_OIWSEC_SHA384); 
                    ht.Add("System.Security.Cryptography.SHA384", Constants.OID_OIWSEC_SHA384);
                    ht.Add("System.Security.Cryptography.SHA384Managed", Constants.OID_OIWSEC_SHA384); 
                    ht.Add("SHA512", Constants.OID_OIWSEC_SHA512); 
                    ht.Add("System.Security.Cryptography.SHA512", Constants.OID_OIWSEC_SHA512);
                    ht.Add("System.Security.Cryptography.SHA512Managed", Constants.OID_OIWSEC_SHA512); 
                    ht.Add("RIPEMD160", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("System.Security.Cryptography.RIPEMD160", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("System.Security.Cryptography.RIPEMD160Managed", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("MD5", Constants.OID_RSA_MD5); 
                    ht.Add("System.Security.Cryptography.MD5", Constants.OID_RSA_MD5);
                    ht.Add("System.Security.Cryptography.MD5CryptoServiceProvider", Constants.OID_RSA_MD5); 
                    ht.Add("System.Security.Cryptography.MD5Managed", Constants.OID_RSA_MD5); 
                    ht.Add("TripleDESKeyWrap", Constants.OID_RSA_SMIMEalgCMS3DESwrap);
                    ht.Add("RC2", Constants.OID_RSA_RC2CBC); 
                    ht.Add("System.Security.Cryptography.RC2CryptoServiceProvider", Constants.OID_RSA_RC2CBC);
                    ht.Add("DES", Constants.OID_OIWSEC_desCBC);
                    ht.Add("System.Security.Cryptography.DESCryptoServiceProvider", Constants.OID_OIWSEC_desCBC);
                    ht.Add("TripleDES", Constants.OID_RSA_DES_EDE3_CBC); 
                    ht.Add("System.Security.Cryptography.TripleDESCryptoServiceProvider", Constants.OID_RSA_DES_EDE3_CBC);
                    defaultOidHT = ht; 
                } 
                return defaultOidHT;
            } 
        }

        private static Hashtable DefaultNameHT {
            get { 
                if (defaultNameHT == null) {
                    Hashtable ht = new Hashtable(StringComparer.OrdinalIgnoreCase); 
                    Type SHA1CryptoServiceProviderType = typeof(System.Security.Cryptography.SHA1CryptoServiceProvider); 
                    Type MD5CryptoServiceProviderType = typeof(System.Security.Cryptography.MD5CryptoServiceProvider);
                    Type SHA256ManagedType = typeof(SHA256Managed); 
                    Type SHA384ManagedType = typeof(SHA384Managed);
                    Type SHA512ManagedType = typeof(SHA512Managed);
                    Type RIPEMD160ManagedType  = typeof(System.Security.Cryptography.RIPEMD160Managed);
                    Type HMACMD5Type       = typeof(System.Security.Cryptography.HMACMD5); 
                    Type HMACRIPEMD160Type = typeof(System.Security.Cryptography.HMACRIPEMD160);
                    Type HMACSHA1Type      = typeof(System.Security.Cryptography.HMACSHA1); 
                    Type HMACSHA256Type    = typeof(System.Security.Cryptography.HMACSHA256); 
                    Type HMACSHA384Type    = typeof(System.Security.Cryptography.HMACSHA384);
                    Type HMACSHA512Type    = typeof(System.Security.Cryptography.HMACSHA512); 
                    Type MAC3DESType       = typeof(System.Security.Cryptography.MACTripleDES);
                    Type RSACryptoServiceProviderType = typeof(System.Security.Cryptography.RSACryptoServiceProvider);
                    Type DSACryptoServiceProviderType = typeof(System.Security.Cryptography.DSACryptoServiceProvider);
                    Type DESCryptoServiceProviderType = typeof(System.Security.Cryptography.DESCryptoServiceProvider); 
                    Type TripleDESCryptoServiceProviderType = typeof(System.Security.Cryptography.TripleDESCryptoServiceProvider);
                    Type RC2CryptoServiceProviderType = typeof(System.Security.Cryptography.RC2CryptoServiceProvider); 
                    Type RijndaelManagedType = typeof(System.Security.Cryptography.RijndaelManaged); 
                    Type DSASignatureDescriptionType = typeof(System.Security.Cryptography.DSASignatureDescription);
                    Type RSAPKCS1SHA1SignatureDescriptionType = typeof(System.Security.Cryptography.RSAPKCS1SHA1SignatureDescription); 
                    Type RNGCryptoServiceProviderType = typeof(System.Security.Cryptography.RNGCryptoServiceProvider);

                    // Random number generator
                    ht.Add("RandomNumberGenerator", RNGCryptoServiceProviderType); 
                    ht.Add("System.Security.Cryptography.RandomNumberGenerator", RNGCryptoServiceProviderType);
 
                    // Hash functions 
                    ht.Add("SHA", SHA1CryptoServiceProviderType);
                    ht.Add("SHA1", SHA1CryptoServiceProviderType); 
                    ht.Add("System.Security.Cryptography.SHA1", SHA1CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.HashAlgorithm", SHA1CryptoServiceProviderType);
                    ht.Add("MD5", MD5CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.MD5", MD5CryptoServiceProviderType); 
                    ht.Add("SHA256", SHA256ManagedType);
                    ht.Add("SHA-256", SHA256ManagedType); 
                    ht.Add("System.Security.Cryptography.SHA256", SHA256ManagedType); 
                    ht.Add("SHA384", SHA384ManagedType);
                    ht.Add("SHA-384", SHA384ManagedType); 
                    ht.Add("System.Security.Cryptography.SHA384", SHA384ManagedType);
                    ht.Add("SHA512", SHA512ManagedType);
                    ht.Add("SHA-512", SHA512ManagedType);
                    ht.Add("System.Security.Cryptography.SHA512", SHA512ManagedType); 
                    ht.Add("RIPEMD160", RIPEMD160ManagedType);
                    ht.Add("RIPEMD-160", RIPEMD160ManagedType); 
                    ht.Add("System.Security.Cryptography.RIPEMD160", RIPEMD160ManagedType); 
                    ht.Add("System.Security.Cryptography.RIPEMD160Managed", RIPEMD160ManagedType);
 
                    // Keyed Hash Algorithms
                    ht.Add("System.Security.Cryptography.HMAC", HMACSHA1Type);
                    ht.Add("System.Security.Cryptography.KeyedHashAlgorithm", HMACSHA1Type);
                    ht.Add("HMACMD5", HMACMD5Type); 
                    ht.Add("System.Security.Cryptography.HMACMD5", HMACMD5Type);
                    ht.Add("HMACRIPEMD160", HMACRIPEMD160Type); 
                    ht.Add("System.Security.Cryptography.HMACRIPEMD160", HMACRIPEMD160Type); 
                    ht.Add("HMACSHA1", HMACSHA1Type);
                    ht.Add("System.Security.Cryptography.HMACSHA1", HMACSHA1Type); 
                    ht.Add("HMACSHA256", HMACSHA256Type);
                    ht.Add("System.Security.Cryptography.HMACSHA256", HMACSHA256Type);
                    ht.Add("HMACSHA384", HMACSHA384Type);
                    ht.Add("System.Security.Cryptography.HMACSHA384", HMACSHA384Type); 
                    ht.Add("HMACSHA512", HMACSHA512Type);
                    ht.Add("System.Security.Cryptography.HMACSHA512", HMACSHA512Type); 
                    ht.Add("MACTripleDES", MAC3DESType); 
                    ht.Add("System.Security.Cryptography.MACTripleDES", MAC3DESType);
 
                    // Asymmetric algorithms
                    ht.Add("RSA", RSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.RSA", RSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.AsymmetricAlgorithm", RSACryptoServiceProviderType); 
                    ht.Add("DSA", DSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.DSA", DSACryptoServiceProviderType); 
 
                    // Symmetric algorithms
                    ht.Add("DES", DESCryptoServiceProviderType); 
                    ht.Add("System.Security.Cryptography.DES", DESCryptoServiceProviderType);
                    ht.Add("3DES", TripleDESCryptoServiceProviderType);
                    ht.Add("TripleDES", TripleDESCryptoServiceProviderType);
                    ht.Add("Triple DES", TripleDESCryptoServiceProviderType); 
                    ht.Add("System.Security.Cryptography.TripleDES", TripleDESCryptoServiceProviderType);
                    ht.Add("RC2", RC2CryptoServiceProviderType); 
                    ht.Add("System.Security.Cryptography.RC2", RC2CryptoServiceProviderType); 
                    ht.Add("Rijndael", RijndaelManagedType);
                    ht.Add("System.Security.Cryptography.Rijndael", RijndaelManagedType); 
                    // Rijndael is the default symmetric cipher because (a) it's the strongest and (b) we know we have an implementation everywhere
                    ht.Add("System.Security.Cryptography.SymmetricAlgorithm", RijndaelManagedType);

                    // Asymmetric signature descriptions 
                    ht.Add("http://www.w3.org/2000/09/xmldsig#dsa-sha1", DSASignatureDescriptionType);
                    ht.Add("System.Security.Cryptography.DSASignatureDescription", DSASignatureDescriptionType); 
                    ht.Add("http://www.w3.org/2000/09/xmldsig#rsa-sha1", RSAPKCS1SHA1SignatureDescriptionType); 
                    ht.Add("System.Security.Cryptography.RSASignatureDescription", RSAPKCS1SHA1SignatureDescriptionType);
 
                    // Xml Dsig/Enc Hash algorithms
                    ht.Add("http://www.w3.org/2000/09/xmldsig#sha1", SHA1CryptoServiceProviderType);
                    // Add the other hash algorithms introduced with XML Encryption
                    ht.Add("http://www.w3.org/2001/04/xmlenc#sha256", SHA256ManagedType); 
                    ht.Add("http://www.w3.org/2001/04/xmlenc#sha512", SHA512ManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#ripemd160", RIPEMD160ManagedType); 
 
                    // Xml Encryption symmetric keys
                    ht.Add("http://www.w3.org/2001/04/xmlenc#des-cbc", DESCryptoServiceProviderType); 
                    ht.Add("http://www.w3.org/2001/04/xmlenc#tripledes-cbc", TripleDESCryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-tripledes", TripleDESCryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes128-cbc", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes128", RijndaelManagedType); 
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes192-cbc", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes192", RijndaelManagedType); 
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes256-cbc", RijndaelManagedType); 
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes256", RijndaelManagedType);
 
                    // Xml Dsig Transforms
                    // First arg must match the constants defined in System.Security.Cryptography.Xml.SignedXml
                    ht.Add("http://www.w3.org/TR/2001/REC-xml-c14n-20010315", "System.Security.Cryptography.Xml.XmlDsigC14NTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments", "System.Security.Cryptography.Xml.XmlDsigC14NWithCommentsTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("http://www.w3.org/2001/10/xml-exc-c14n#", "System.Security.Cryptography.Xml.XmlDsigExcC14NTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", "System.Security.Cryptography.Xml.XmlDsigExcC14NWithCommentsTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("http://www.w3.org/2000/09/xmldsig#base64","System.Security.Cryptography.Xml.XmlDsigBase64Transform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("http://www.w3.org/TR/1999/REC-xpath-19991116","System.Security.Cryptography.Xml.XmlDsigXPathTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/TR/1999/REC-xslt-19991116", "System.Security.Cryptography.Xml.XmlDsigXsltTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("http://www.w3.org/2000/09/xmldsig#enveloped-signature", "System.Security.Cryptography.Xml.XmlDsigEnvelopedSignatureTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);

                    // the decryption transform
                    ht.Add("http://www.w3.org/2002/07/decrypt#XML", "System.Security.Cryptography.Xml.XmlDecryptionTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 

                    // Xml licence transform. 
                    ht.Add("urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform", "System.Security.Cryptography.Xml.XmlLicenseTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 

                    // Xml Dsig KeyInfo 
                    // First arg (the key) is formed as elem.NamespaceURI + " " + elem.LocalName
                    ht.Add("http://www.w3.org/2000/09/xmldsig# X509Data", "System.Security.Cryptography.Xml.KeyInfoX509Data, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyName", "System.Security.Cryptography.Xml.KeyInfoName, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyValue/DSAKeyValue", "System.Security.Cryptography.Xml.DSAKeyValue, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyValue/RSAKeyValue", "System.Security.Cryptography.Xml.RSAKeyValue, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# RetrievalMethod", "System.Security.Cryptography.Xml.KeyInfoRetrievalMethod, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
 
                    // Xml EncryptedKey
                    ht.Add("http://www.w3.org/2001/04/xmlenc# EncryptedKey", "System.Security.Cryptography.Xml.KeyInfoEncryptedKey, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 

                    // Xml Dsig-more Uri's as defined in http://www.ietf.org/internet-drafts/draft-eastlake-xmldsig-uri-02.txt
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#md5", MD5CryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#sha384", SHA384ManagedType); 
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-ripemd160", HMACRIPEMD160Type);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", HMACSHA256Type); 
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha384", HMACSHA384Type); 
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha512", HMACSHA512Type);
 
                    // X509 Extensions (custom decoders)
                    // Basic Constraints OID value
                    ht.Add("2.5.29.10", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version);
                    ht.Add("2.5.29.19", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version); 
                    // Subject Key Identifier OID value
                    ht.Add("2.5.29.14", "System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version); 
                    // Key Usage OID value 
                    ht.Add("2.5.29.15", "System.Security.Cryptography.X509Certificates.X509KeyUsageExtension, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version);
                    // Enhanced Key Usage OID value 
                    ht.Add("2.5.29.37", "System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version);

                    // X509Chain class can be overridden to use a different chain engine.
                    ht.Add("X509Chain", "System.Security.Cryptography.X509Certificates.X509Chain, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version); 

                    // PKCS9 attributes 
                    ht.Add("1.2.840.113549.1.9.3", "System.Security.Cryptography.Pkcs.Pkcs9ContentType, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("1.2.840.113549.1.9.4", "System.Security.Cryptography.Pkcs.Pkcs9MessageDigest, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("1.2.840.113549.1.9.5", "System.Security.Cryptography.Pkcs.Pkcs9SigningTime, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("1.3.6.1.4.1.311.88.2.1", "System.Security.Cryptography.Pkcs.Pkcs9DocumentName, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("1.3.6.1.4.1.311.88.2.2", "System.Security.Cryptography.Pkcs.Pkcs9DocumentDescription, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);

                    defaultNameHT = ht; 
                }
                return defaultNameHT; 
            } 
        }
 
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        private static void InitializeConfigInfo() {
            // set up the version string 
            Type myType = typeof(CryptoConfig);
            _Version = myType.Assembly.GetVersion().ToString(); 
            if ((machineNameHT == null) && (machineOidHT == null)) { 
                lock(InternalSyncObject) {
                    String machineConfigFile = machineConfigDir + machineConfigFilename; 
                    // we need to assert here the right to read the machineConfigFile, since
                    // the parser now checks for this right.
                    (new FileIOPermission(FileIOPermissionAccess.Read, machineConfigFile)).Assert();
                    if (File.Exists(machineConfigFile)) { 
                        ConfigTreeParser parser = new ConfigTreeParser();
                        ConfigNode rootNode = parser.Parse(machineConfigFile, "configuration"); 
                        if (rootNode == null) goto endInitialization; 
                        // now, find the mscorlib tag with our version
                        ArrayList rootChildren = rootNode.Children; 
                        ConfigNode mscorlibNode = null;
                        foreach (ConfigNode node in rootChildren) {
                            if (node.Name.Equals("mscorlib")) {
                                ArrayList attribs = node.Attributes; 
                                if (attribs.Count > 0) {
                                    DictionaryEntry attribute = (DictionaryEntry) node.Attributes[0]; 
                                    if (attribute.Key.Equals("version")) { 
                                        if (attribute.Value.Equals(_Version)) {
                                            mscorlibNode = node; 
                                            break;
                                        }
                                    }
                                } 
                                else mscorlibNode = node;
                            } 
                        } 
                        if (mscorlibNode == null) goto endInitialization;
                        // look for cryptosettings 
                        ArrayList mscorlibChildren = mscorlibNode.Children;
                        ConfigNode cryptoSettings = null;
                        foreach (ConfigNode node in mscorlibChildren) {
                            // take the first one that matches 
                            if (node.Name.Equals("cryptographySettings")) {
                                cryptoSettings = node; 
                                break; 
                            }
                        } 
                        if (cryptoSettings == null) goto endInitialization;
                        // See if there's a CryptoNameMapping section (at most one)
                        ConfigNode cryptoNameMapping = null;
                        foreach (ConfigNode node in cryptoSettings.Children) { 
                            if (node.Name.Equals("cryptoNameMapping")) {
                                cryptoNameMapping = node; 
                                break; 
                            }
                        } 
                        if (cryptoNameMapping == null) goto initializeOIDMap;
                        // We have a name mapping section, so now we have to build up the type aliases
                        // in CryptoClass elements and the mappings.
                        ArrayList cryptoNameMappingChildren = cryptoNameMapping.Children; 
                        ConfigNode cryptoClasses = null;
                        // find the cryptoClases element 
                        foreach (ConfigNode node in cryptoNameMappingChildren) { 
                            if (node.Name.Equals("cryptoClasses")) {
                                cryptoClasses = node; 
                                break;
                            }
                        }
                        // if we didn't find it, no mappings 
                        if (cryptoClasses == null) goto initializeOIDMap;
                        Hashtable typeAliases = new Hashtable(); 
                        Hashtable nameMappings = new Hashtable(); 
                        foreach (ConfigNode node in cryptoClasses.Children) {
                            if (node.Name.Equals("cryptoClass")) { 
                                if (node.Attributes.Count > 0) {
                                    DictionaryEntry attribute = (DictionaryEntry) node.Attributes[0];
                                    typeAliases.Add(attribute.Key,attribute.Value);
                                } 
                            }
                        } 
                        // Now process the name mappings 
                        foreach (ConfigNode node in cryptoNameMappingChildren) {
                            if (node.Name.Equals("nameEntry")) { 
                                String friendlyName = null;
                                String className = null;
                                foreach (DictionaryEntry attribNode in node.Attributes) {
                                    if (((String) attribNode.Key).Equals("name")) { 
                                        friendlyName = (String) attribNode.Value;
                                        continue; 
                                    } 
                                    if (((String) attribNode.Key).Equals("class")) {
                                        className = (String) attribNode.Value; 
                                        continue;
                                    }
                                }
                                if ((friendlyName != null) && (className != null)) { 
                                    String typeName = (String) typeAliases[className];
                                    if (typeName != null) { 
                                        nameMappings.Add(friendlyName,typeName); 
                                    }
                                } 
                            }
                        }
                        machineNameHT = nameMappings;
                    initializeOIDMap: 
                        // Now, process the OID mappings
                        // See if there's an oidMap section (at most one) 
                        ConfigNode oidMapNode = null; 
                        foreach (ConfigNode node in cryptoSettings.Children) {
                            if (node.Name.Equals("oidMap")) { 
                                oidMapNode = node;
                                break;
                            }
                        } 
                        if (oidMapNode == null) goto endInitialization;
                        Hashtable oidMapHT = new Hashtable(); 
                        foreach (ConfigNode node in oidMapNode.Children) { 
                            if (node.Name.Equals("oidEntry")) {
                                String oidString = null; 
                                String friendlyName = null;
                                foreach (DictionaryEntry attribNode in node.Attributes) {
                                    if (((String) attribNode.Key).Equals("OID")) {
                                        oidString = (String) attribNode.Value; 
                                        continue;
                                    } 
                                    if (((String) attribNode.Key).Equals("name")) { 
                                        friendlyName = (String) attribNode.Value;
                                        continue; 
                                    }
                                }
                                if ((friendlyName != null) && (oidString != null)) {
                                    oidMapHT.Add(friendlyName,oidString); 
                                }
                            } 
                        } 
                        machineOidHT = oidMapHT;
                    } 
                }
            }
            endInitialization:
                isInitialized = true; 
        }
 
        public static object CreateFromName (string name, params object[] args) { 
            if (name == null)
                throw new ArgumentNullException("name"); 

            Type retvalType = null;
            Object retval;
 
            // First we'll do the machine-wide stuff, initializing if necessary
            if (!isInitialized) { 
                InitializeConfigInfo(); 
            }
 
            // Search the machine table
            if (machineNameHT != null) {
                String retvalTypeString = (String) machineNameHT[name];
                if (retvalTypeString != null) { 
                    retvalType = Type.GetType(retvalTypeString, false, false);
                    if (retvalType != null && !retvalType.IsVisible) 
                        retvalType = null; 
                }
            } 

            // If we didn't find it in the machine-wide table,  look in the default table
            if (retvalType == null) {
                // We allow the default table to Types and Strings 
                // Types get used for other stuff in mscorlib.dll
                // strings get used for delay-loaded stuff like System.Security.dll 
                Object retvalObj  = DefaultNameHT[name]; 
                if (retvalObj != null) {
                    if (retvalObj is Type) { 
                        retvalType = (Type) retvalObj;
                    } else if (retvalObj is String) {
                        retvalType = Type.GetType((String) retvalObj, false, false);
                        if (retvalType != null && !retvalType.IsVisible) 
                            retvalType = null;
                    } 
                } 
            }
 
            // Maybe they gave us a classname.
            if (retvalType == null) {
                retvalType = Type.GetType(name, false, false);
                if (retvalType != null && !retvalType.IsVisible) 
                    retvalType = null;
            } 
 
            // Still null? Then we didn't find it
            if (retvalType == null) 
                return null;

            // Perform a CreateInstance by hand so we can check that the
            // constructor doesn't have a linktime demand attached (which would 
            // be incorrrectly applied against mscorlib otherwise).
            RuntimeType rtType = retvalType as RuntimeType; 
            if (rtType == null) 
                return null;
            if (args == null) 
                args = new Object[]{};

            // Locate all constructors.
            MethodBase[] cons = rtType.GetConstructors(Activator.ConstructorDefault); 

            if (cons == null) 
                return null; 

            ArrayList candidates = new ArrayList(); 
            for (int i = 0; i < cons.Length; i ++) {
                MethodBase con = cons[i];
                if (con.GetParameters().Length == args.Length) {
                    candidates.Add(con); 
                }
            } 
 
            if (candidates.Count == 0)
                return null; 

            cons = candidates.ToArray(typeof(MethodBase)) as MethodBase[];

            // Bind to matching ctor. 
            Object state;
            RuntimeConstructorInfo rci = Type.DefaultBinder.BindToMethod(Activator.ConstructorDefault, 
                                                                         cons, 
                                                                         ref args,
                                                                         null, 
                                                                         null,
                                                                         null,
                                                                         out state) as RuntimeConstructorInfo;
 
            // Check for ctor we don't like (non-existant, delegate or decorated
            // with declarative linktime demand). 
            if (rci == null || typeof(Delegate).IsAssignableFrom(rci.DeclaringType)) 
                return null;
 
            // Ctor invoke (actually causes the allocation as well).
            retval = rci.Invoke(Activator.ConstructorDefault, Type.DefaultBinder, args, null);

            // Reset any parameter re-ordering performed by the binder. 
            if (state != null)
                Type.DefaultBinder.ReorderArgumentArray(ref args, state); 
 
            return retval;
        } 

        public static object CreateFromName (string name) {
            return CreateFromName(name, null);
        } 

        public static string MapNameToOID (string name) { 
            if (name == null) 
                throw new ArgumentNullException("name");
 
            // First we'll do the machine-wide stuff, initializing if necessary
            if (!isInitialized)
                InitializeConfigInfo();
 
            string oid = null;
            // Search the machine table 
            if (machineOidHT != null) 
                oid = machineOidHT[name] as string;
 
            // If we didn't find it in the machine-wide table, look in the default table
            if (oid == null)
                oid = DefaultOidHT[name] as string;
 
            // Try the CAPI table association
            if (oid == null) 
                oid = X509Utils._GetOidFromFriendlyName(name); 

            return oid; 
        }

        static public byte[] EncodeOID (string str) {
            if (str == null) { 
                throw new ArgumentNullException("str");
            } 
            char[] sepArray = { '.' }; // valid ASN.1 separators 
            String[] oidString = str.Split(sepArray);
            uint[] oidNums = new uint[oidString.Length]; 
            for (int i = 0; i < oidString.Length; i++) {
                oidNums[i] = (uint) Int32.Parse(oidString[i], CultureInfo.InvariantCulture);
            }
 
            // Allocate the array to receive encoded oidNums
            byte[] encodedOidNums = new byte[oidNums.Length * 5]; // this is guaranteed to be longer than necessary 
            int encodedOidNumsIndex = 0; 
            // Handle the first two oidNums special
            if (oidNums.Length < 2) { 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOID"));
            }
            uint firstTwoOidNums = (oidNums[0] * 40) + oidNums[1];
            byte[] retval = EncodeSingleOIDNum(firstTwoOidNums); 
            Array.Copy(retval, 0, encodedOidNums, encodedOidNumsIndex, retval.Length);
            encodedOidNumsIndex += retval.Length; 
            for (int i = 2; i < oidNums.Length; i++) { 
                retval = EncodeSingleOIDNum(oidNums[i]);
                Buffer.InternalBlockCopy(retval, 0, encodedOidNums, encodedOidNumsIndex, retval.Length); 
                encodedOidNumsIndex += retval.Length;
            }

            // final return value is 06 <length> || encodedOidNums 
            if (encodedOidNumsIndex > 0x7f) {
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_Config_EncodedOIDError")); 
            } 
            retval = new byte[ encodedOidNumsIndex + 2];
            retval[0] = (byte) 0x06; 
            retval[1] = (byte) encodedOidNumsIndex;
            Buffer.InternalBlockCopy(encodedOidNums, 0, retval, 2, encodedOidNumsIndex);
            return retval;
        } 

        static private byte[] EncodeSingleOIDNum(uint dwValue) { 
            byte[] retval; 

            if ((int)dwValue < 0x80) { 
                retval = new byte[1];
                retval[0] = (byte) dwValue;
                return retval;
            } 
            else if (dwValue < 0x4000) {
                retval = new byte[2]; 
                retval[0]   = (byte) ((dwValue >> 7) | 0x80); 
                retval[1] = (byte) (dwValue & 0x7f);
                return retval; 
            }
            else if (dwValue < 0x200000) {
                retval = new byte[3];
                retval[0] = (byte) ((dwValue >> 14) | 0x80); 
                retval[1] = (byte) ((dwValue >> 7) | 0x80);
                retval[2] = (byte) (dwValue & 0x7f); 
                return retval; 
            }
            else if (dwValue < 0x10000000) { 
                retval = new byte[4];
                retval[0] = (byte) ((dwValue >> 21) | 0x80);
                retval[1] = (byte) ((dwValue >> 14) | 0x80);
                retval[2] = (byte) ((dwValue >> 7) | 0x80); 
                retval[3] = (byte) (dwValue & 0x7f);
                return retval; 
            } 
            else {
                retval = new byte[5]; 
                retval[0] = (byte) ((dwValue >> 28) | 0x80);
                retval[1] = (byte) ((dwValue >> 21) | 0x80);
                retval[2] = (byte) ((dwValue >> 14) | 0x80);
                retval[3] = (byte) ((dwValue >> 7) | 0x80); 
                retval[4] = (byte) (dwValue & 0x7f);
                return retval; 
            } 
        }
    } 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// CryptoConfig.cs 
//
 
namespace System.Security.Cryptography {
    using System;
    using System.Collections;
    using System.IO; 
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates; 
    using System.Security.Permissions; 
    using System.Threading;
    using System.Globalization; 
    using System.Runtime.Versioning;

    [System.Runtime.InteropServices.ComVisible(true)]
    public class CryptoConfig { 
        private static Hashtable defaultOidHT = null;
        private static Hashtable defaultNameHT = null; 
 
        private static string machineConfigDir = System.Security.Util.Config.MachineDirectory;
        private static Hashtable machineOidHT = null; 
        private static Hashtable machineNameHT = null;
        private static string machineConfigFilename = "machine.config";
        private static bool isInitialized = false;
 
        private static string _Version = null;
 
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
 
        private static Hashtable DefaultOidHT {
            get {
                if (defaultOidHT == null) {
                    Hashtable ht = new Hashtable(StringComparer.OrdinalIgnoreCase); 
                    ht.Add("SHA", Constants.OID_OIWSEC_SHA1);
                    ht.Add("SHA1", Constants.OID_OIWSEC_SHA1); 
                    ht.Add("System.Security.Cryptography.SHA1", Constants.OID_OIWSEC_SHA1); 
                    ht.Add("System.Security.Cryptography.SHA1CryptoServiceProvider", Constants.OID_OIWSEC_SHA1);
                    ht.Add("System.Security.Cryptography.SHA1Managed", Constants.OID_OIWSEC_SHA1); 
                    ht.Add("SHA256", Constants.OID_OIWSEC_SHA256);
                    ht.Add("System.Security.Cryptography.SHA256", Constants.OID_OIWSEC_SHA256);
                    ht.Add("System.Security.Cryptography.SHA256Managed", Constants.OID_OIWSEC_SHA256);
                    ht.Add("SHA384", Constants.OID_OIWSEC_SHA384); 
                    ht.Add("System.Security.Cryptography.SHA384", Constants.OID_OIWSEC_SHA384);
                    ht.Add("System.Security.Cryptography.SHA384Managed", Constants.OID_OIWSEC_SHA384); 
                    ht.Add("SHA512", Constants.OID_OIWSEC_SHA512); 
                    ht.Add("System.Security.Cryptography.SHA512", Constants.OID_OIWSEC_SHA512);
                    ht.Add("System.Security.Cryptography.SHA512Managed", Constants.OID_OIWSEC_SHA512); 
                    ht.Add("RIPEMD160", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("System.Security.Cryptography.RIPEMD160", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("System.Security.Cryptography.RIPEMD160Managed", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("MD5", Constants.OID_RSA_MD5); 
                    ht.Add("System.Security.Cryptography.MD5", Constants.OID_RSA_MD5);
                    ht.Add("System.Security.Cryptography.MD5CryptoServiceProvider", Constants.OID_RSA_MD5); 
                    ht.Add("System.Security.Cryptography.MD5Managed", Constants.OID_RSA_MD5); 
                    ht.Add("TripleDESKeyWrap", Constants.OID_RSA_SMIMEalgCMS3DESwrap);
                    ht.Add("RC2", Constants.OID_RSA_RC2CBC); 
                    ht.Add("System.Security.Cryptography.RC2CryptoServiceProvider", Constants.OID_RSA_RC2CBC);
                    ht.Add("DES", Constants.OID_OIWSEC_desCBC);
                    ht.Add("System.Security.Cryptography.DESCryptoServiceProvider", Constants.OID_OIWSEC_desCBC);
                    ht.Add("TripleDES", Constants.OID_RSA_DES_EDE3_CBC); 
                    ht.Add("System.Security.Cryptography.TripleDESCryptoServiceProvider", Constants.OID_RSA_DES_EDE3_CBC);
                    defaultOidHT = ht; 
                } 
                return defaultOidHT;
            } 
        }

        private static Hashtable DefaultNameHT {
            get { 
                if (defaultNameHT == null) {
                    Hashtable ht = new Hashtable(StringComparer.OrdinalIgnoreCase); 
                    Type SHA1CryptoServiceProviderType = typeof(System.Security.Cryptography.SHA1CryptoServiceProvider); 
                    Type MD5CryptoServiceProviderType = typeof(System.Security.Cryptography.MD5CryptoServiceProvider);
                    Type SHA256ManagedType = typeof(SHA256Managed); 
                    Type SHA384ManagedType = typeof(SHA384Managed);
                    Type SHA512ManagedType = typeof(SHA512Managed);
                    Type RIPEMD160ManagedType  = typeof(System.Security.Cryptography.RIPEMD160Managed);
                    Type HMACMD5Type       = typeof(System.Security.Cryptography.HMACMD5); 
                    Type HMACRIPEMD160Type = typeof(System.Security.Cryptography.HMACRIPEMD160);
                    Type HMACSHA1Type      = typeof(System.Security.Cryptography.HMACSHA1); 
                    Type HMACSHA256Type    = typeof(System.Security.Cryptography.HMACSHA256); 
                    Type HMACSHA384Type    = typeof(System.Security.Cryptography.HMACSHA384);
                    Type HMACSHA512Type    = typeof(System.Security.Cryptography.HMACSHA512); 
                    Type MAC3DESType       = typeof(System.Security.Cryptography.MACTripleDES);
                    Type RSACryptoServiceProviderType = typeof(System.Security.Cryptography.RSACryptoServiceProvider);
                    Type DSACryptoServiceProviderType = typeof(System.Security.Cryptography.DSACryptoServiceProvider);
                    Type DESCryptoServiceProviderType = typeof(System.Security.Cryptography.DESCryptoServiceProvider); 
                    Type TripleDESCryptoServiceProviderType = typeof(System.Security.Cryptography.TripleDESCryptoServiceProvider);
                    Type RC2CryptoServiceProviderType = typeof(System.Security.Cryptography.RC2CryptoServiceProvider); 
                    Type RijndaelManagedType = typeof(System.Security.Cryptography.RijndaelManaged); 
                    Type DSASignatureDescriptionType = typeof(System.Security.Cryptography.DSASignatureDescription);
                    Type RSAPKCS1SHA1SignatureDescriptionType = typeof(System.Security.Cryptography.RSAPKCS1SHA1SignatureDescription); 
                    Type RNGCryptoServiceProviderType = typeof(System.Security.Cryptography.RNGCryptoServiceProvider);

                    // Random number generator
                    ht.Add("RandomNumberGenerator", RNGCryptoServiceProviderType); 
                    ht.Add("System.Security.Cryptography.RandomNumberGenerator", RNGCryptoServiceProviderType);
 
                    // Hash functions 
                    ht.Add("SHA", SHA1CryptoServiceProviderType);
                    ht.Add("SHA1", SHA1CryptoServiceProviderType); 
                    ht.Add("System.Security.Cryptography.SHA1", SHA1CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.HashAlgorithm", SHA1CryptoServiceProviderType);
                    ht.Add("MD5", MD5CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.MD5", MD5CryptoServiceProviderType); 
                    ht.Add("SHA256", SHA256ManagedType);
                    ht.Add("SHA-256", SHA256ManagedType); 
                    ht.Add("System.Security.Cryptography.SHA256", SHA256ManagedType); 
                    ht.Add("SHA384", SHA384ManagedType);
                    ht.Add("SHA-384", SHA384ManagedType); 
                    ht.Add("System.Security.Cryptography.SHA384", SHA384ManagedType);
                    ht.Add("SHA512", SHA512ManagedType);
                    ht.Add("SHA-512", SHA512ManagedType);
                    ht.Add("System.Security.Cryptography.SHA512", SHA512ManagedType); 
                    ht.Add("RIPEMD160", RIPEMD160ManagedType);
                    ht.Add("RIPEMD-160", RIPEMD160ManagedType); 
                    ht.Add("System.Security.Cryptography.RIPEMD160", RIPEMD160ManagedType); 
                    ht.Add("System.Security.Cryptography.RIPEMD160Managed", RIPEMD160ManagedType);
 
                    // Keyed Hash Algorithms
                    ht.Add("System.Security.Cryptography.HMAC", HMACSHA1Type);
                    ht.Add("System.Security.Cryptography.KeyedHashAlgorithm", HMACSHA1Type);
                    ht.Add("HMACMD5", HMACMD5Type); 
                    ht.Add("System.Security.Cryptography.HMACMD5", HMACMD5Type);
                    ht.Add("HMACRIPEMD160", HMACRIPEMD160Type); 
                    ht.Add("System.Security.Cryptography.HMACRIPEMD160", HMACRIPEMD160Type); 
                    ht.Add("HMACSHA1", HMACSHA1Type);
                    ht.Add("System.Security.Cryptography.HMACSHA1", HMACSHA1Type); 
                    ht.Add("HMACSHA256", HMACSHA256Type);
                    ht.Add("System.Security.Cryptography.HMACSHA256", HMACSHA256Type);
                    ht.Add("HMACSHA384", HMACSHA384Type);
                    ht.Add("System.Security.Cryptography.HMACSHA384", HMACSHA384Type); 
                    ht.Add("HMACSHA512", HMACSHA512Type);
                    ht.Add("System.Security.Cryptography.HMACSHA512", HMACSHA512Type); 
                    ht.Add("MACTripleDES", MAC3DESType); 
                    ht.Add("System.Security.Cryptography.MACTripleDES", MAC3DESType);
 
                    // Asymmetric algorithms
                    ht.Add("RSA", RSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.RSA", RSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.AsymmetricAlgorithm", RSACryptoServiceProviderType); 
                    ht.Add("DSA", DSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.DSA", DSACryptoServiceProviderType); 
 
                    // Symmetric algorithms
                    ht.Add("DES", DESCryptoServiceProviderType); 
                    ht.Add("System.Security.Cryptography.DES", DESCryptoServiceProviderType);
                    ht.Add("3DES", TripleDESCryptoServiceProviderType);
                    ht.Add("TripleDES", TripleDESCryptoServiceProviderType);
                    ht.Add("Triple DES", TripleDESCryptoServiceProviderType); 
                    ht.Add("System.Security.Cryptography.TripleDES", TripleDESCryptoServiceProviderType);
                    ht.Add("RC2", RC2CryptoServiceProviderType); 
                    ht.Add("System.Security.Cryptography.RC2", RC2CryptoServiceProviderType); 
                    ht.Add("Rijndael", RijndaelManagedType);
                    ht.Add("System.Security.Cryptography.Rijndael", RijndaelManagedType); 
                    // Rijndael is the default symmetric cipher because (a) it's the strongest and (b) we know we have an implementation everywhere
                    ht.Add("System.Security.Cryptography.SymmetricAlgorithm", RijndaelManagedType);

                    // Asymmetric signature descriptions 
                    ht.Add("http://www.w3.org/2000/09/xmldsig#dsa-sha1", DSASignatureDescriptionType);
                    ht.Add("System.Security.Cryptography.DSASignatureDescription", DSASignatureDescriptionType); 
                    ht.Add("http://www.w3.org/2000/09/xmldsig#rsa-sha1", RSAPKCS1SHA1SignatureDescriptionType); 
                    ht.Add("System.Security.Cryptography.RSASignatureDescription", RSAPKCS1SHA1SignatureDescriptionType);
 
                    // Xml Dsig/Enc Hash algorithms
                    ht.Add("http://www.w3.org/2000/09/xmldsig#sha1", SHA1CryptoServiceProviderType);
                    // Add the other hash algorithms introduced with XML Encryption
                    ht.Add("http://www.w3.org/2001/04/xmlenc#sha256", SHA256ManagedType); 
                    ht.Add("http://www.w3.org/2001/04/xmlenc#sha512", SHA512ManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#ripemd160", RIPEMD160ManagedType); 
 
                    // Xml Encryption symmetric keys
                    ht.Add("http://www.w3.org/2001/04/xmlenc#des-cbc", DESCryptoServiceProviderType); 
                    ht.Add("http://www.w3.org/2001/04/xmlenc#tripledes-cbc", TripleDESCryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-tripledes", TripleDESCryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes128-cbc", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes128", RijndaelManagedType); 
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes192-cbc", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes192", RijndaelManagedType); 
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes256-cbc", RijndaelManagedType); 
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes256", RijndaelManagedType);
 
                    // Xml Dsig Transforms
                    // First arg must match the constants defined in System.Security.Cryptography.Xml.SignedXml
                    ht.Add("http://www.w3.org/TR/2001/REC-xml-c14n-20010315", "System.Security.Cryptography.Xml.XmlDsigC14NTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments", "System.Security.Cryptography.Xml.XmlDsigC14NWithCommentsTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("http://www.w3.org/2001/10/xml-exc-c14n#", "System.Security.Cryptography.Xml.XmlDsigExcC14NTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", "System.Security.Cryptography.Xml.XmlDsigExcC14NWithCommentsTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("http://www.w3.org/2000/09/xmldsig#base64","System.Security.Cryptography.Xml.XmlDsigBase64Transform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("http://www.w3.org/TR/1999/REC-xpath-19991116","System.Security.Cryptography.Xml.XmlDsigXPathTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/TR/1999/REC-xslt-19991116", "System.Security.Cryptography.Xml.XmlDsigXsltTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("http://www.w3.org/2000/09/xmldsig#enveloped-signature", "System.Security.Cryptography.Xml.XmlDsigEnvelopedSignatureTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);

                    // the decryption transform
                    ht.Add("http://www.w3.org/2002/07/decrypt#XML", "System.Security.Cryptography.Xml.XmlDecryptionTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 

                    // Xml licence transform. 
                    ht.Add("urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform", "System.Security.Cryptography.Xml.XmlLicenseTransform, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 

                    // Xml Dsig KeyInfo 
                    // First arg (the key) is formed as elem.NamespaceURI + " " + elem.LocalName
                    ht.Add("http://www.w3.org/2000/09/xmldsig# X509Data", "System.Security.Cryptography.Xml.KeyInfoX509Data, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyName", "System.Security.Cryptography.Xml.KeyInfoName, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyValue/DSAKeyValue", "System.Security.Cryptography.Xml.DSAKeyValue, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyValue/RSAKeyValue", "System.Security.Cryptography.Xml.RSAKeyValue, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# RetrievalMethod", "System.Security.Cryptography.Xml.KeyInfoRetrievalMethod, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
 
                    // Xml EncryptedKey
                    ht.Add("http://www.w3.org/2001/04/xmlenc# EncryptedKey", "System.Security.Cryptography.Xml.KeyInfoEncryptedKey, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 

                    // Xml Dsig-more Uri's as defined in http://www.ietf.org/internet-drafts/draft-eastlake-xmldsig-uri-02.txt
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#md5", MD5CryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#sha384", SHA384ManagedType); 
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-ripemd160", HMACRIPEMD160Type);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", HMACSHA256Type); 
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha384", HMACSHA384Type); 
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha512", HMACSHA512Type);
 
                    // X509 Extensions (custom decoders)
                    // Basic Constraints OID value
                    ht.Add("2.5.29.10", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version);
                    ht.Add("2.5.29.19", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version); 
                    // Subject Key Identifier OID value
                    ht.Add("2.5.29.14", "System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version); 
                    // Key Usage OID value 
                    ht.Add("2.5.29.15", "System.Security.Cryptography.X509Certificates.X509KeyUsageExtension, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version);
                    // Enhanced Key Usage OID value 
                    ht.Add("2.5.29.37", "System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version);

                    // X509Chain class can be overridden to use a different chain engine.
                    ht.Add("X509Chain", "System.Security.Cryptography.X509Certificates.X509Chain, System, Culture=neutral, PublicKeyToken=b77a5c561934e089, Version=" + _Version); 

                    // PKCS9 attributes 
                    ht.Add("1.2.840.113549.1.9.3", "System.Security.Cryptography.Pkcs.Pkcs9ContentType, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("1.2.840.113549.1.9.4", "System.Security.Cryptography.Pkcs.Pkcs9MessageDigest, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("1.2.840.113549.1.9.5", "System.Security.Cryptography.Pkcs.Pkcs9SigningTime, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version); 
                    ht.Add("1.3.6.1.4.1.311.88.2.1", "System.Security.Cryptography.Pkcs.Pkcs9DocumentName, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);
                    ht.Add("1.3.6.1.4.1.311.88.2.2", "System.Security.Cryptography.Pkcs.Pkcs9DocumentDescription, System.Security, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, Version=" + _Version);

                    defaultNameHT = ht; 
                }
                return defaultNameHT; 
            } 
        }
 
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        private static void InitializeConfigInfo() {
            // set up the version string 
            Type myType = typeof(CryptoConfig);
            _Version = myType.Assembly.GetVersion().ToString(); 
            if ((machineNameHT == null) && (machineOidHT == null)) { 
                lock(InternalSyncObject) {
                    String machineConfigFile = machineConfigDir + machineConfigFilename; 
                    // we need to assert here the right to read the machineConfigFile, since
                    // the parser now checks for this right.
                    (new FileIOPermission(FileIOPermissionAccess.Read, machineConfigFile)).Assert();
                    if (File.Exists(machineConfigFile)) { 
                        ConfigTreeParser parser = new ConfigTreeParser();
                        ConfigNode rootNode = parser.Parse(machineConfigFile, "configuration"); 
                        if (rootNode == null) goto endInitialization; 
                        // now, find the mscorlib tag with our version
                        ArrayList rootChildren = rootNode.Children; 
                        ConfigNode mscorlibNode = null;
                        foreach (ConfigNode node in rootChildren) {
                            if (node.Name.Equals("mscorlib")) {
                                ArrayList attribs = node.Attributes; 
                                if (attribs.Count > 0) {
                                    DictionaryEntry attribute = (DictionaryEntry) node.Attributes[0]; 
                                    if (attribute.Key.Equals("version")) { 
                                        if (attribute.Value.Equals(_Version)) {
                                            mscorlibNode = node; 
                                            break;
                                        }
                                    }
                                } 
                                else mscorlibNode = node;
                            } 
                        } 
                        if (mscorlibNode == null) goto endInitialization;
                        // look for cryptosettings 
                        ArrayList mscorlibChildren = mscorlibNode.Children;
                        ConfigNode cryptoSettings = null;
                        foreach (ConfigNode node in mscorlibChildren) {
                            // take the first one that matches 
                            if (node.Name.Equals("cryptographySettings")) {
                                cryptoSettings = node; 
                                break; 
                            }
                        } 
                        if (cryptoSettings == null) goto endInitialization;
                        // See if there's a CryptoNameMapping section (at most one)
                        ConfigNode cryptoNameMapping = null;
                        foreach (ConfigNode node in cryptoSettings.Children) { 
                            if (node.Name.Equals("cryptoNameMapping")) {
                                cryptoNameMapping = node; 
                                break; 
                            }
                        } 
                        if (cryptoNameMapping == null) goto initializeOIDMap;
                        // We have a name mapping section, so now we have to build up the type aliases
                        // in CryptoClass elements and the mappings.
                        ArrayList cryptoNameMappingChildren = cryptoNameMapping.Children; 
                        ConfigNode cryptoClasses = null;
                        // find the cryptoClases element 
                        foreach (ConfigNode node in cryptoNameMappingChildren) { 
                            if (node.Name.Equals("cryptoClasses")) {
                                cryptoClasses = node; 
                                break;
                            }
                        }
                        // if we didn't find it, no mappings 
                        if (cryptoClasses == null) goto initializeOIDMap;
                        Hashtable typeAliases = new Hashtable(); 
                        Hashtable nameMappings = new Hashtable(); 
                        foreach (ConfigNode node in cryptoClasses.Children) {
                            if (node.Name.Equals("cryptoClass")) { 
                                if (node.Attributes.Count > 0) {
                                    DictionaryEntry attribute = (DictionaryEntry) node.Attributes[0];
                                    typeAliases.Add(attribute.Key,attribute.Value);
                                } 
                            }
                        } 
                        // Now process the name mappings 
                        foreach (ConfigNode node in cryptoNameMappingChildren) {
                            if (node.Name.Equals("nameEntry")) { 
                                String friendlyName = null;
                                String className = null;
                                foreach (DictionaryEntry attribNode in node.Attributes) {
                                    if (((String) attribNode.Key).Equals("name")) { 
                                        friendlyName = (String) attribNode.Value;
                                        continue; 
                                    } 
                                    if (((String) attribNode.Key).Equals("class")) {
                                        className = (String) attribNode.Value; 
                                        continue;
                                    }
                                }
                                if ((friendlyName != null) && (className != null)) { 
                                    String typeName = (String) typeAliases[className];
                                    if (typeName != null) { 
                                        nameMappings.Add(friendlyName,typeName); 
                                    }
                                } 
                            }
                        }
                        machineNameHT = nameMappings;
                    initializeOIDMap: 
                        // Now, process the OID mappings
                        // See if there's an oidMap section (at most one) 
                        ConfigNode oidMapNode = null; 
                        foreach (ConfigNode node in cryptoSettings.Children) {
                            if (node.Name.Equals("oidMap")) { 
                                oidMapNode = node;
                                break;
                            }
                        } 
                        if (oidMapNode == null) goto endInitialization;
                        Hashtable oidMapHT = new Hashtable(); 
                        foreach (ConfigNode node in oidMapNode.Children) { 
                            if (node.Name.Equals("oidEntry")) {
                                String oidString = null; 
                                String friendlyName = null;
                                foreach (DictionaryEntry attribNode in node.Attributes) {
                                    if (((String) attribNode.Key).Equals("OID")) {
                                        oidString = (String) attribNode.Value; 
                                        continue;
                                    } 
                                    if (((String) attribNode.Key).Equals("name")) { 
                                        friendlyName = (String) attribNode.Value;
                                        continue; 
                                    }
                                }
                                if ((friendlyName != null) && (oidString != null)) {
                                    oidMapHT.Add(friendlyName,oidString); 
                                }
                            } 
                        } 
                        machineOidHT = oidMapHT;
                    } 
                }
            }
            endInitialization:
                isInitialized = true; 
        }
 
        public static object CreateFromName (string name, params object[] args) { 
            if (name == null)
                throw new ArgumentNullException("name"); 

            Type retvalType = null;
            Object retval;
 
            // First we'll do the machine-wide stuff, initializing if necessary
            if (!isInitialized) { 
                InitializeConfigInfo(); 
            }
 
            // Search the machine table
            if (machineNameHT != null) {
                String retvalTypeString = (String) machineNameHT[name];
                if (retvalTypeString != null) { 
                    retvalType = Type.GetType(retvalTypeString, false, false);
                    if (retvalType != null && !retvalType.IsVisible) 
                        retvalType = null; 
                }
            } 

            // If we didn't find it in the machine-wide table,  look in the default table
            if (retvalType == null) {
                // We allow the default table to Types and Strings 
                // Types get used for other stuff in mscorlib.dll
                // strings get used for delay-loaded stuff like System.Security.dll 
                Object retvalObj  = DefaultNameHT[name]; 
                if (retvalObj != null) {
                    if (retvalObj is Type) { 
                        retvalType = (Type) retvalObj;
                    } else if (retvalObj is String) {
                        retvalType = Type.GetType((String) retvalObj, false, false);
                        if (retvalType != null && !retvalType.IsVisible) 
                            retvalType = null;
                    } 
                } 
            }
 
            // Maybe they gave us a classname.
            if (retvalType == null) {
                retvalType = Type.GetType(name, false, false);
                if (retvalType != null && !retvalType.IsVisible) 
                    retvalType = null;
            } 
 
            // Still null? Then we didn't find it
            if (retvalType == null) 
                return null;

            // Perform a CreateInstance by hand so we can check that the
            // constructor doesn't have a linktime demand attached (which would 
            // be incorrrectly applied against mscorlib otherwise).
            RuntimeType rtType = retvalType as RuntimeType; 
            if (rtType == null) 
                return null;
            if (args == null) 
                args = new Object[]{};

            // Locate all constructors.
            MethodBase[] cons = rtType.GetConstructors(Activator.ConstructorDefault); 

            if (cons == null) 
                return null; 

            ArrayList candidates = new ArrayList(); 
            for (int i = 0; i < cons.Length; i ++) {
                MethodBase con = cons[i];
                if (con.GetParameters().Length == args.Length) {
                    candidates.Add(con); 
                }
            } 
 
            if (candidates.Count == 0)
                return null; 

            cons = candidates.ToArray(typeof(MethodBase)) as MethodBase[];

            // Bind to matching ctor. 
            Object state;
            RuntimeConstructorInfo rci = Type.DefaultBinder.BindToMethod(Activator.ConstructorDefault, 
                                                                         cons, 
                                                                         ref args,
                                                                         null, 
                                                                         null,
                                                                         null,
                                                                         out state) as RuntimeConstructorInfo;
 
            // Check for ctor we don't like (non-existant, delegate or decorated
            // with declarative linktime demand). 
            if (rci == null || typeof(Delegate).IsAssignableFrom(rci.DeclaringType)) 
                return null;
 
            // Ctor invoke (actually causes the allocation as well).
            retval = rci.Invoke(Activator.ConstructorDefault, Type.DefaultBinder, args, null);

            // Reset any parameter re-ordering performed by the binder. 
            if (state != null)
                Type.DefaultBinder.ReorderArgumentArray(ref args, state); 
 
            return retval;
        } 

        public static object CreateFromName (string name) {
            return CreateFromName(name, null);
        } 

        public static string MapNameToOID (string name) { 
            if (name == null) 
                throw new ArgumentNullException("name");
 
            // First we'll do the machine-wide stuff, initializing if necessary
            if (!isInitialized)
                InitializeConfigInfo();
 
            string oid = null;
            // Search the machine table 
            if (machineOidHT != null) 
                oid = machineOidHT[name] as string;
 
            // If we didn't find it in the machine-wide table, look in the default table
            if (oid == null)
                oid = DefaultOidHT[name] as string;
 
            // Try the CAPI table association
            if (oid == null) 
                oid = X509Utils._GetOidFromFriendlyName(name); 

            return oid; 
        }

        static public byte[] EncodeOID (string str) {
            if (str == null) { 
                throw new ArgumentNullException("str");
            } 
            char[] sepArray = { '.' }; // valid ASN.1 separators 
            String[] oidString = str.Split(sepArray);
            uint[] oidNums = new uint[oidString.Length]; 
            for (int i = 0; i < oidString.Length; i++) {
                oidNums[i] = (uint) Int32.Parse(oidString[i], CultureInfo.InvariantCulture);
            }
 
            // Allocate the array to receive encoded oidNums
            byte[] encodedOidNums = new byte[oidNums.Length * 5]; // this is guaranteed to be longer than necessary 
            int encodedOidNumsIndex = 0; 
            // Handle the first two oidNums special
            if (oidNums.Length < 2) { 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOID"));
            }
            uint firstTwoOidNums = (oidNums[0] * 40) + oidNums[1];
            byte[] retval = EncodeSingleOIDNum(firstTwoOidNums); 
            Array.Copy(retval, 0, encodedOidNums, encodedOidNumsIndex, retval.Length);
            encodedOidNumsIndex += retval.Length; 
            for (int i = 2; i < oidNums.Length; i++) { 
                retval = EncodeSingleOIDNum(oidNums[i]);
                Buffer.InternalBlockCopy(retval, 0, encodedOidNums, encodedOidNumsIndex, retval.Length); 
                encodedOidNumsIndex += retval.Length;
            }

            // final return value is 06 <length> || encodedOidNums 
            if (encodedOidNumsIndex > 0x7f) {
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_Config_EncodedOIDError")); 
            } 
            retval = new byte[ encodedOidNumsIndex + 2];
            retval[0] = (byte) 0x06; 
            retval[1] = (byte) encodedOidNumsIndex;
            Buffer.InternalBlockCopy(encodedOidNums, 0, retval, 2, encodedOidNumsIndex);
            return retval;
        } 

        static private byte[] EncodeSingleOIDNum(uint dwValue) { 
            byte[] retval; 

            if ((int)dwValue < 0x80) { 
                retval = new byte[1];
                retval[0] = (byte) dwValue;
                return retval;
            } 
            else if (dwValue < 0x4000) {
                retval = new byte[2]; 
                retval[0]   = (byte) ((dwValue >> 7) | 0x80); 
                retval[1] = (byte) (dwValue & 0x7f);
                return retval; 
            }
            else if (dwValue < 0x200000) {
                retval = new byte[3];
                retval[0] = (byte) ((dwValue >> 14) | 0x80); 
                retval[1] = (byte) ((dwValue >> 7) | 0x80);
                retval[2] = (byte) (dwValue & 0x7f); 
                return retval; 
            }
            else if (dwValue < 0x10000000) { 
                retval = new byte[4];
                retval[0] = (byte) ((dwValue >> 21) | 0x80);
                retval[1] = (byte) ((dwValue >> 14) | 0x80);
                retval[2] = (byte) ((dwValue >> 7) | 0x80); 
                retval[3] = (byte) (dwValue & 0x7f);
                return retval; 
            } 
            else {
                retval = new byte[5]; 
                retval[0] = (byte) ((dwValue >> 28) | 0x80);
                retval[1] = (byte) ((dwValue >> 21) | 0x80);
                retval[2] = (byte) ((dwValue >> 14) | 0x80);
                retval[3] = (byte) ((dwValue >> 7) | 0x80); 
                retval[4] = (byte) (dwValue & 0x7f);
                return retval; 
            } 
        }
    } 
}
