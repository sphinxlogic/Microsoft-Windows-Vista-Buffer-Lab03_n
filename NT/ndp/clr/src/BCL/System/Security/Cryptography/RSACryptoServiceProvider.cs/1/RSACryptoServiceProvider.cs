// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RSACryptoServiceProvider.cs 
//
// CSP-based implementation of RSA 
//

namespace System.Security.Cryptography {
    using System; 
    using System.IO;
    using System.Security.Cryptography.X509Certificates; 
    using System.Security.Permissions; 
    using System.Globalization;
 
    // Object layout of the RSAParameters structure
    internal class RSACspObject {
        internal byte[] Exponent;
        internal byte[] Modulus; 
        internal byte[] P;
        internal byte[] Q; 
        internal byte[] DP; 
        internal byte[] DQ;
        internal byte[] InverseQ; 
        internal byte[] D;
    }

    [System.Runtime.InteropServices.ComVisible(true)] 
    public sealed class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm {
        private int _dwKeySize; 
        private CspParameters  _parameters; 
        private bool _randomKeyContainer;
        private SafeProvHandle _safeProvHandle; 
        private SafeKeyHandle _safeKeyHandle;

        private static CspProviderFlags s_UseMachineKeyStore = 0;
 
        //
        // public constructors 
        // 

        public RSACryptoServiceProvider() 
            : this(0, new CspParameters(Constants.PROV_RSA_FULL, null, null, s_UseMachineKeyStore), true) {
        }

        public RSACryptoServiceProvider(int dwKeySize) 
            : this(dwKeySize, new CspParameters(Constants.PROV_RSA_FULL, null, null, s_UseMachineKeyStore), false) {
        } 
 
        public RSACryptoServiceProvider(CspParameters parameters)
            : this(0, parameters, true) { 
        }

        public RSACryptoServiceProvider(int dwKeySize, CspParameters parameters)
            : this(dwKeySize, parameters, false) { 
        }
 
        // 
        // private methods
        // 

        private const uint RandomKeyContainerFlag = 0x80000000;

        private RSACryptoServiceProvider(int dwKeySize, CspParameters parameters, bool useDefaultKeySize) { 
            if (dwKeySize < 0)
                throw new ArgumentOutOfRangeException("dwKeySize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum")); 
 
            // see if the random key container flag was set, and clear the bit since CAPI will not understand it
            bool externallyGeneratedRandomKeyContainer = (((uint)parameters.Flags) & RandomKeyContainerFlag) != 0; 
            parameters.Flags = (CspProviderFlags)(((uint)parameters.Flags) & (~RandomKeyContainerFlag));

            _parameters = Utils.SaveCspParameters(CspAlgorithmType.Rsa, parameters, s_UseMachineKeyStore, ref _randomKeyContainer);
 
            if (_parameters.KeyNumber == Constants.AT_SIGNATURE || Utils.HasEnhProv == 1) {
                LegalKeySizesValue = new KeySizes[] { new KeySizes(384, 16384, 8) }; 
                if (useDefaultKeySize) 
                    _dwKeySize = 1024;
            } else { 
                // All we have is the base provider
                LegalKeySizesValue = new KeySizes[] { new KeySizes(384, 512, 8) };
                if (useDefaultKeySize)
                    _dwKeySize = 512; 
            }
 
            if (!useDefaultKeySize) 
                _dwKeySize = dwKeySize;
 
            // If this is not a random container we generate, create it eagerly
            // in the constructor so we can report any errors now.
            if (!_randomKeyContainer || Environment.GetCompatibilityFlag(CompatibilityFlag.EagerlyGenerateRandomAsymmKeys))
                GetKeyPair(); 

            // if the random key container flag was set, set the random flag so that KeyContainerPermission 
            // demands are bypassed 
            _randomKeyContainer |= externallyGeneratedRandomKeyContainer;
            return; 
        }

        private void GetKeyPair () {
            if (_safeKeyHandle == null) { 
                lock (this) {
                    if (_safeKeyHandle == null) 
                        Utils.GetKeyPairHelper(CspAlgorithmType.Rsa, _parameters, _randomKeyContainer, _dwKeySize, ref _safeProvHandle, ref _safeKeyHandle); 
                }
            } 
        }

        protected override void Dispose(bool disposing) {
            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed) 
                _safeKeyHandle.Dispose();
            if (_safeProvHandle != null && !_safeProvHandle.IsClosed) 
                _safeProvHandle.Dispose(); 
        }
 
        //
        // public properties
        //
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public bool PublicOnly { 
            get { 
                GetKeyPair();
                byte[] publicKey = (byte[]) Utils._GetKeyParameter(_safeKeyHandle, Constants.CLR_PUBLICKEYONLY); 
                return (publicKey[0] == 1);
            }
        }
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public CspKeyContainerInfo CspKeyContainerInfo { 
            get { 
                GetKeyPair();
                return new CspKeyContainerInfo(_parameters, _randomKeyContainer); 
            }
        }

        public override int KeySize { 
            get {
                GetKeyPair(); 
                byte[] keySize = (byte[]) Utils._GetKeyParameter(_safeKeyHandle, Constants.CLR_KEYLEN); 
                _dwKeySize = (keySize[0] | (keySize[1] << 8) | (keySize[2] << 16) | (keySize[3] << 24));
                return _dwKeySize; 
            }
        }

        public override string KeyExchangeAlgorithm { 
            get {
                if (_parameters.KeyNumber == Constants.AT_KEYEXCHANGE) 
                    return "RSA-PKCS1-KeyEx"; 
                return null;
            } 
        }

        public override string SignatureAlgorithm {
            get { return "http://www.w3.org/2000/09/xmldsig#rsa-sha1"; } 
        }
 
        public static bool UseMachineKeyStore { 
            get { return (s_UseMachineKeyStore == CspProviderFlags.UseMachineKeyStore); }
            set { s_UseMachineKeyStore = (value ? CspProviderFlags.UseMachineKeyStore : 0); } 
        }

        public bool PersistKeyInCsp {
            get { 
                if (_safeProvHandle == null) {
                    lock (this) { 
                        if (_safeProvHandle == null) 
                            _safeProvHandle = Utils.CreateProvHandle(_parameters, _randomKeyContainer);
                    } 
                }
                return Utils._GetPersistKeyInCsp(_safeProvHandle);
            }
            set { 
                bool oldPersistKeyInCsp = this.PersistKeyInCsp;
                if (value == oldPersistKeyInCsp) 
                    return; 

                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags); 
                if (!value) {
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Delete);
                    kp.AccessEntries.Add(entry);
                } else { 
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Create);
                    kp.AccessEntries.Add(entry); 
                } 
                kp.Demand();
 
                Utils._SetPersistKeyInCsp(_safeProvHandle, value);
            }
        }
 
        //
        // public methods 
        // 

        public override RSAParameters ExportParameters (bool includePrivateParameters) { 
            GetKeyPair();
            if (includePrivateParameters) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Export); 
                kp.AccessEntries.Add(entry);
                kp.Demand(); 
            } 
            RSACspObject rsaCspObject = new RSACspObject();
            int blobType = includePrivateParameters ? Constants.PRIVATEKEYBLOB : Constants.PUBLICKEYBLOB; 
            // _ExportKey will check for failures and throw an exception
            Utils._ExportKey(_safeKeyHandle, blobType, rsaCspObject);
            return RSAObjectToStruct(rsaCspObject);
        } 

        [System.Runtime.InteropServices.ComVisible(false)] 
        public byte[] ExportCspBlob (bool includePrivateParameters) { 
            GetKeyPair();
            return Utils.ExportCspBlobHelper(includePrivateParameters, _parameters, _safeKeyHandle); 
        }

        public override void ImportParameters(RSAParameters parameters) {
            RSACspObject rsaCspObject = RSAStructToObject(parameters); 
            // Free the current key handle
            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed) 
                _safeKeyHandle.Dispose(); 
            _safeKeyHandle = SafeKeyHandle.InvalidHandle;
 
            if (IsPublic(parameters)) {
                // Use our CRYPT_VERIFYCONTEXT handle, CRYPT_EXPORTABLE is not applicable to public only keys, so pass false
                Utils._ImportKey(Utils.StaticProvHandle, Constants.CALG_RSA_KEYX, (CspProviderFlags) 0, rsaCspObject, ref _safeKeyHandle);
            } else { 
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Import); 
                kp.AccessEntries.Add(entry); 
                kp.Demand();
                if (_safeProvHandle == null) 
                    _safeProvHandle = Utils.CreateProvHandle(_parameters, _randomKeyContainer);
                // Now, import the key into the CSP; _ImportKey will check for failures.
                Utils._ImportKey(_safeProvHandle, Constants.CALG_RSA_KEYX, _parameters.Flags, rsaCspObject, ref _safeKeyHandle);
            } 
        }
 
        [System.Runtime.InteropServices.ComVisible(false)] 
        public void ImportCspBlob (byte[] keyBlob) {
            Utils.ImportCspBlobHelper(CspAlgorithmType.Rsa, keyBlob, IsPublic(keyBlob), ref _parameters, _randomKeyContainer, ref _safeProvHandle, ref _safeKeyHandle); 
        }

        public byte[] SignData(Stream inputStream, Object halg) {
            string oid = Utils.ObjToOidValue(halg); 
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(inputStream); 
            return SignHash(hashVal, oid); 
        }
 
        public byte[] SignData(byte[] buffer, Object halg) {
            string oid = Utils.ObjToOidValue(halg);
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer); 
            return SignHash(hashVal, oid);
        } 
 
        public byte[] SignData(byte[] buffer, int offset, int count, Object halg) {
            string oid = Utils.ObjToOidValue(halg); 
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer, offset, count);
            return SignHash(hashVal, oid);
        } 

        public bool VerifyData(byte[] buffer, Object halg, byte[] signature) { 
            string oid = Utils.ObjToOidValue(halg); 
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer); 
            return VerifyHash(hashVal, oid, signature);
        }

        public byte[] SignHash(byte[] rgbHash, string str) { 
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash"); 
            if (PublicOnly) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_NoPrivateKey"));
 
            int calgHash = X509Utils.OidToAlgId(str);
            GetKeyPair();
            if (!_randomKeyContainer) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags); 
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Sign);
                kp.AccessEntries.Add(entry); 
                kp.Demand(); 
            }
            return Utils._SignValue(_safeKeyHandle, _parameters.KeyNumber, Constants.CALG_RSA_SIGN, calgHash, rgbHash, 0); 
        }

        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature) {
            if (rgbHash == null) 
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null) 
                throw new ArgumentNullException("rgbSignature"); 

            int calgHash = X509Utils.OidToAlgId(str); 
            GetKeyPair();
            return Utils._VerifySign(_safeKeyHandle, Constants.CALG_RSA_SIGN, calgHash, rgbHash, rgbSignature, 0);
        }
 
        //
        // if fOAEP is true, PKCS#1 v2.0 (OAEP) padding is used for both encryption and decryption, 
        // otherwise PKCS#1 v1.5 padding is used. In both cases, we will first try to use symmetric 
        // key import/export through the exponent-of-one trick. If this fails, we will try to use direct
        // encryption and decryption (only available in Win2K platforms and above). This is to make 
        // sure key transport of symmetric keys always works regardless of which CSP is used or installed.
        //

        public byte[] Encrypt(byte[] rgb, bool fOAEP) { 
            if (rgb == null)
                throw new ArgumentNullException("rgb"); 
 
            GetKeyPair();
            byte[] result = null; 
            int hr = Constants.S_OK;
            if (fOAEP) {
                // this is only available if we have the enhanced provider AND we're on Win2K
                if (Utils.HasEnhProv != 1 || Utils.Win2KCrypto != 1) 
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_Padding_Win2KEnhOnly"));
                result = Utils._EncryptPKWin2KEnh(_safeKeyHandle, rgb, true, out hr); // true means use CRYPT_OAEP flag 
                if (hr != Constants.S_OK) 
                    throw new CryptographicException(hr);
            } else { 
                // Use PKCS1 v1 type 2 padding here
                result = Utils._EncryptPKWin2KEnh(_safeKeyHandle, rgb, false, out hr);
                if (hr != Constants.S_OK)
                    result = Utils._EncryptKey(_safeKeyHandle, rgb); 
            }
            return result; 
        } 

        public byte [] Decrypt(byte[] rgb, bool fOAEP) { 
            if (rgb == null)
                throw new ArgumentNullException("rgb");

            GetKeyPair(); 
            // size check -- must be at most the modulus size
            if (rgb.Length > (KeySize / 8)) 
                throw new CryptographicException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Cryptography_Padding_DecDataTooBig"), KeySize / 8)); 

            if (!_randomKeyContainer) { 
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Decrypt);
                kp.AccessEntries.Add(entry);
                kp.Demand(); 
            }
 
            byte[] result = null; 
            int hr = Constants.S_OK;
            if (fOAEP) { 
                // this is only available if we have the enhanced provider AND we're on Win2K
                if (Utils.HasEnhProv != 1 || Utils.Win2KCrypto != 1)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_Padding_Win2KEnhOnly"));
                result = Utils._DecryptPKWin2KEnh(_safeKeyHandle, rgb, true, out hr); 
                if (hr != Constants.S_OK)
                    throw new CryptographicException(hr); 
            } else { 
                // Use PKCS1 v1 type 2 padding here
                result = Utils._DecryptPKWin2KEnh(_safeKeyHandle, rgb, false, out hr); 
                if (hr != Constants.S_OK)
                    result = Utils._DecryptKey(_safeKeyHandle, rgb, 0);
            }
            return result; 
        }
 
        public override byte[] DecryptValue(byte[] rgb) { 
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        } 

        public override byte[] EncryptValue(byte[] rgb) {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        } 

        // 
        // private static methods 
        //
 
        private static RSAParameters RSAObjectToStruct (RSACspObject rsaCspObject) {
            RSAParameters rsaParams = new RSAParameters();
            rsaParams.Exponent = rsaCspObject.Exponent;
            rsaParams.Modulus = rsaCspObject.Modulus; 
            rsaParams.P = rsaCspObject.P;
            rsaParams.Q = rsaCspObject.Q; 
            rsaParams.DP = rsaCspObject.DP; 
            rsaParams.DQ = rsaCspObject.DQ;
            rsaParams.InverseQ = rsaCspObject.InverseQ; 
            rsaParams.D = rsaCspObject.D;
            return rsaParams;
        }
 
        private static RSACspObject RSAStructToObject (RSAParameters rsaParams) {
            RSACspObject rsaCspObject = new RSACspObject(); 
            rsaCspObject.Exponent = rsaParams.Exponent; 
            rsaCspObject.Modulus = rsaParams.Modulus;
            rsaCspObject.P = rsaParams.P; 
            rsaCspObject.Q = rsaParams.Q;
            rsaCspObject.DP = rsaParams.DP;
            rsaCspObject.DQ = rsaParams.DQ;
            rsaCspObject.InverseQ = rsaParams.InverseQ; 
            rsaCspObject.D = rsaParams.D;
            return rsaCspObject; 
        } 

        // Since P is required, we will assume its presence is synonymous to a private key. 
        private static bool IsPublic (RSAParameters rsaParams) {
            return (rsaParams.P == null);
        }
 
        // find whether an RSA key blob is public.
        private static bool IsPublic (byte[] keyBlob) { 
            if (keyBlob == null) 
                throw new ArgumentNullException("keyBlob");
 
            // The CAPI RSA public key representation consists of the following sequence:
            //  - BLOBHEADER
            //  - RSAPUBKEY
 
            // The first should be PUBLICKEYBLOB and magic should be RSA_PUB_MAGIC "RSA1"
            if (keyBlob[0] != Constants.PUBLICKEYBLOB) 
                return false; 

            if (keyBlob[11] != 0x31 || keyBlob[10] != 0x41 || keyBlob[9] != 0x53 || keyBlob[8] != 0x52) 
                return false;

            return true;
        } 
    }
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RSACryptoServiceProvider.cs 
//
// CSP-based implementation of RSA 
//

namespace System.Security.Cryptography {
    using System; 
    using System.IO;
    using System.Security.Cryptography.X509Certificates; 
    using System.Security.Permissions; 
    using System.Globalization;
 
    // Object layout of the RSAParameters structure
    internal class RSACspObject {
        internal byte[] Exponent;
        internal byte[] Modulus; 
        internal byte[] P;
        internal byte[] Q; 
        internal byte[] DP; 
        internal byte[] DQ;
        internal byte[] InverseQ; 
        internal byte[] D;
    }

    [System.Runtime.InteropServices.ComVisible(true)] 
    public sealed class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm {
        private int _dwKeySize; 
        private CspParameters  _parameters; 
        private bool _randomKeyContainer;
        private SafeProvHandle _safeProvHandle; 
        private SafeKeyHandle _safeKeyHandle;

        private static CspProviderFlags s_UseMachineKeyStore = 0;
 
        //
        // public constructors 
        // 

        public RSACryptoServiceProvider() 
            : this(0, new CspParameters(Constants.PROV_RSA_FULL, null, null, s_UseMachineKeyStore), true) {
        }

        public RSACryptoServiceProvider(int dwKeySize) 
            : this(dwKeySize, new CspParameters(Constants.PROV_RSA_FULL, null, null, s_UseMachineKeyStore), false) {
        } 
 
        public RSACryptoServiceProvider(CspParameters parameters)
            : this(0, parameters, true) { 
        }

        public RSACryptoServiceProvider(int dwKeySize, CspParameters parameters)
            : this(dwKeySize, parameters, false) { 
        }
 
        // 
        // private methods
        // 

        private const uint RandomKeyContainerFlag = 0x80000000;

        private RSACryptoServiceProvider(int dwKeySize, CspParameters parameters, bool useDefaultKeySize) { 
            if (dwKeySize < 0)
                throw new ArgumentOutOfRangeException("dwKeySize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum")); 
 
            // see if the random key container flag was set, and clear the bit since CAPI will not understand it
            bool externallyGeneratedRandomKeyContainer = (((uint)parameters.Flags) & RandomKeyContainerFlag) != 0; 
            parameters.Flags = (CspProviderFlags)(((uint)parameters.Flags) & (~RandomKeyContainerFlag));

            _parameters = Utils.SaveCspParameters(CspAlgorithmType.Rsa, parameters, s_UseMachineKeyStore, ref _randomKeyContainer);
 
            if (_parameters.KeyNumber == Constants.AT_SIGNATURE || Utils.HasEnhProv == 1) {
                LegalKeySizesValue = new KeySizes[] { new KeySizes(384, 16384, 8) }; 
                if (useDefaultKeySize) 
                    _dwKeySize = 1024;
            } else { 
                // All we have is the base provider
                LegalKeySizesValue = new KeySizes[] { new KeySizes(384, 512, 8) };
                if (useDefaultKeySize)
                    _dwKeySize = 512; 
            }
 
            if (!useDefaultKeySize) 
                _dwKeySize = dwKeySize;
 
            // If this is not a random container we generate, create it eagerly
            // in the constructor so we can report any errors now.
            if (!_randomKeyContainer || Environment.GetCompatibilityFlag(CompatibilityFlag.EagerlyGenerateRandomAsymmKeys))
                GetKeyPair(); 

            // if the random key container flag was set, set the random flag so that KeyContainerPermission 
            // demands are bypassed 
            _randomKeyContainer |= externallyGeneratedRandomKeyContainer;
            return; 
        }

        private void GetKeyPair () {
            if (_safeKeyHandle == null) { 
                lock (this) {
                    if (_safeKeyHandle == null) 
                        Utils.GetKeyPairHelper(CspAlgorithmType.Rsa, _parameters, _randomKeyContainer, _dwKeySize, ref _safeProvHandle, ref _safeKeyHandle); 
                }
            } 
        }

        protected override void Dispose(bool disposing) {
            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed) 
                _safeKeyHandle.Dispose();
            if (_safeProvHandle != null && !_safeProvHandle.IsClosed) 
                _safeProvHandle.Dispose(); 
        }
 
        //
        // public properties
        //
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public bool PublicOnly { 
            get { 
                GetKeyPair();
                byte[] publicKey = (byte[]) Utils._GetKeyParameter(_safeKeyHandle, Constants.CLR_PUBLICKEYONLY); 
                return (publicKey[0] == 1);
            }
        }
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public CspKeyContainerInfo CspKeyContainerInfo { 
            get { 
                GetKeyPair();
                return new CspKeyContainerInfo(_parameters, _randomKeyContainer); 
            }
        }

        public override int KeySize { 
            get {
                GetKeyPair(); 
                byte[] keySize = (byte[]) Utils._GetKeyParameter(_safeKeyHandle, Constants.CLR_KEYLEN); 
                _dwKeySize = (keySize[0] | (keySize[1] << 8) | (keySize[2] << 16) | (keySize[3] << 24));
                return _dwKeySize; 
            }
        }

        public override string KeyExchangeAlgorithm { 
            get {
                if (_parameters.KeyNumber == Constants.AT_KEYEXCHANGE) 
                    return "RSA-PKCS1-KeyEx"; 
                return null;
            } 
        }

        public override string SignatureAlgorithm {
            get { return "http://www.w3.org/2000/09/xmldsig#rsa-sha1"; } 
        }
 
        public static bool UseMachineKeyStore { 
            get { return (s_UseMachineKeyStore == CspProviderFlags.UseMachineKeyStore); }
            set { s_UseMachineKeyStore = (value ? CspProviderFlags.UseMachineKeyStore : 0); } 
        }

        public bool PersistKeyInCsp {
            get { 
                if (_safeProvHandle == null) {
                    lock (this) { 
                        if (_safeProvHandle == null) 
                            _safeProvHandle = Utils.CreateProvHandle(_parameters, _randomKeyContainer);
                    } 
                }
                return Utils._GetPersistKeyInCsp(_safeProvHandle);
            }
            set { 
                bool oldPersistKeyInCsp = this.PersistKeyInCsp;
                if (value == oldPersistKeyInCsp) 
                    return; 

                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags); 
                if (!value) {
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Delete);
                    kp.AccessEntries.Add(entry);
                } else { 
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Create);
                    kp.AccessEntries.Add(entry); 
                } 
                kp.Demand();
 
                Utils._SetPersistKeyInCsp(_safeProvHandle, value);
            }
        }
 
        //
        // public methods 
        // 

        public override RSAParameters ExportParameters (bool includePrivateParameters) { 
            GetKeyPair();
            if (includePrivateParameters) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Export); 
                kp.AccessEntries.Add(entry);
                kp.Demand(); 
            } 
            RSACspObject rsaCspObject = new RSACspObject();
            int blobType = includePrivateParameters ? Constants.PRIVATEKEYBLOB : Constants.PUBLICKEYBLOB; 
            // _ExportKey will check for failures and throw an exception
            Utils._ExportKey(_safeKeyHandle, blobType, rsaCspObject);
            return RSAObjectToStruct(rsaCspObject);
        } 

        [System.Runtime.InteropServices.ComVisible(false)] 
        public byte[] ExportCspBlob (bool includePrivateParameters) { 
            GetKeyPair();
            return Utils.ExportCspBlobHelper(includePrivateParameters, _parameters, _safeKeyHandle); 
        }

        public override void ImportParameters(RSAParameters parameters) {
            RSACspObject rsaCspObject = RSAStructToObject(parameters); 
            // Free the current key handle
            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed) 
                _safeKeyHandle.Dispose(); 
            _safeKeyHandle = SafeKeyHandle.InvalidHandle;
 
            if (IsPublic(parameters)) {
                // Use our CRYPT_VERIFYCONTEXT handle, CRYPT_EXPORTABLE is not applicable to public only keys, so pass false
                Utils._ImportKey(Utils.StaticProvHandle, Constants.CALG_RSA_KEYX, (CspProviderFlags) 0, rsaCspObject, ref _safeKeyHandle);
            } else { 
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Import); 
                kp.AccessEntries.Add(entry); 
                kp.Demand();
                if (_safeProvHandle == null) 
                    _safeProvHandle = Utils.CreateProvHandle(_parameters, _randomKeyContainer);
                // Now, import the key into the CSP; _ImportKey will check for failures.
                Utils._ImportKey(_safeProvHandle, Constants.CALG_RSA_KEYX, _parameters.Flags, rsaCspObject, ref _safeKeyHandle);
            } 
        }
 
        [System.Runtime.InteropServices.ComVisible(false)] 
        public void ImportCspBlob (byte[] keyBlob) {
            Utils.ImportCspBlobHelper(CspAlgorithmType.Rsa, keyBlob, IsPublic(keyBlob), ref _parameters, _randomKeyContainer, ref _safeProvHandle, ref _safeKeyHandle); 
        }

        public byte[] SignData(Stream inputStream, Object halg) {
            string oid = Utils.ObjToOidValue(halg); 
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(inputStream); 
            return SignHash(hashVal, oid); 
        }
 
        public byte[] SignData(byte[] buffer, Object halg) {
            string oid = Utils.ObjToOidValue(halg);
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer); 
            return SignHash(hashVal, oid);
        } 
 
        public byte[] SignData(byte[] buffer, int offset, int count, Object halg) {
            string oid = Utils.ObjToOidValue(halg); 
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer, offset, count);
            return SignHash(hashVal, oid);
        } 

        public bool VerifyData(byte[] buffer, Object halg, byte[] signature) { 
            string oid = Utils.ObjToOidValue(halg); 
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer); 
            return VerifyHash(hashVal, oid, signature);
        }

        public byte[] SignHash(byte[] rgbHash, string str) { 
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash"); 
            if (PublicOnly) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_NoPrivateKey"));
 
            int calgHash = X509Utils.OidToAlgId(str);
            GetKeyPair();
            if (!_randomKeyContainer) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags); 
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Sign);
                kp.AccessEntries.Add(entry); 
                kp.Demand(); 
            }
            return Utils._SignValue(_safeKeyHandle, _parameters.KeyNumber, Constants.CALG_RSA_SIGN, calgHash, rgbHash, 0); 
        }

        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature) {
            if (rgbHash == null) 
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null) 
                throw new ArgumentNullException("rgbSignature"); 

            int calgHash = X509Utils.OidToAlgId(str); 
            GetKeyPair();
            return Utils._VerifySign(_safeKeyHandle, Constants.CALG_RSA_SIGN, calgHash, rgbHash, rgbSignature, 0);
        }
 
        //
        // if fOAEP is true, PKCS#1 v2.0 (OAEP) padding is used for both encryption and decryption, 
        // otherwise PKCS#1 v1.5 padding is used. In both cases, we will first try to use symmetric 
        // key import/export through the exponent-of-one trick. If this fails, we will try to use direct
        // encryption and decryption (only available in Win2K platforms and above). This is to make 
        // sure key transport of symmetric keys always works regardless of which CSP is used or installed.
        //

        public byte[] Encrypt(byte[] rgb, bool fOAEP) { 
            if (rgb == null)
                throw new ArgumentNullException("rgb"); 
 
            GetKeyPair();
            byte[] result = null; 
            int hr = Constants.S_OK;
            if (fOAEP) {
                // this is only available if we have the enhanced provider AND we're on Win2K
                if (Utils.HasEnhProv != 1 || Utils.Win2KCrypto != 1) 
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_Padding_Win2KEnhOnly"));
                result = Utils._EncryptPKWin2KEnh(_safeKeyHandle, rgb, true, out hr); // true means use CRYPT_OAEP flag 
                if (hr != Constants.S_OK) 
                    throw new CryptographicException(hr);
            } else { 
                // Use PKCS1 v1 type 2 padding here
                result = Utils._EncryptPKWin2KEnh(_safeKeyHandle, rgb, false, out hr);
                if (hr != Constants.S_OK)
                    result = Utils._EncryptKey(_safeKeyHandle, rgb); 
            }
            return result; 
        } 

        public byte [] Decrypt(byte[] rgb, bool fOAEP) { 
            if (rgb == null)
                throw new ArgumentNullException("rgb");

            GetKeyPair(); 
            // size check -- must be at most the modulus size
            if (rgb.Length > (KeySize / 8)) 
                throw new CryptographicException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Cryptography_Padding_DecDataTooBig"), KeySize / 8)); 

            if (!_randomKeyContainer) { 
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Decrypt);
                kp.AccessEntries.Add(entry);
                kp.Demand(); 
            }
 
            byte[] result = null; 
            int hr = Constants.S_OK;
            if (fOAEP) { 
                // this is only available if we have the enhanced provider AND we're on Win2K
                if (Utils.HasEnhProv != 1 || Utils.Win2KCrypto != 1)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_Padding_Win2KEnhOnly"));
                result = Utils._DecryptPKWin2KEnh(_safeKeyHandle, rgb, true, out hr); 
                if (hr != Constants.S_OK)
                    throw new CryptographicException(hr); 
            } else { 
                // Use PKCS1 v1 type 2 padding here
                result = Utils._DecryptPKWin2KEnh(_safeKeyHandle, rgb, false, out hr); 
                if (hr != Constants.S_OK)
                    result = Utils._DecryptKey(_safeKeyHandle, rgb, 0);
            }
            return result; 
        }
 
        public override byte[] DecryptValue(byte[] rgb) { 
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        } 

        public override byte[] EncryptValue(byte[] rgb) {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        } 

        // 
        // private static methods 
        //
 
        private static RSAParameters RSAObjectToStruct (RSACspObject rsaCspObject) {
            RSAParameters rsaParams = new RSAParameters();
            rsaParams.Exponent = rsaCspObject.Exponent;
            rsaParams.Modulus = rsaCspObject.Modulus; 
            rsaParams.P = rsaCspObject.P;
            rsaParams.Q = rsaCspObject.Q; 
            rsaParams.DP = rsaCspObject.DP; 
            rsaParams.DQ = rsaCspObject.DQ;
            rsaParams.InverseQ = rsaCspObject.InverseQ; 
            rsaParams.D = rsaCspObject.D;
            return rsaParams;
        }
 
        private static RSACspObject RSAStructToObject (RSAParameters rsaParams) {
            RSACspObject rsaCspObject = new RSACspObject(); 
            rsaCspObject.Exponent = rsaParams.Exponent; 
            rsaCspObject.Modulus = rsaParams.Modulus;
            rsaCspObject.P = rsaParams.P; 
            rsaCspObject.Q = rsaParams.Q;
            rsaCspObject.DP = rsaParams.DP;
            rsaCspObject.DQ = rsaParams.DQ;
            rsaCspObject.InverseQ = rsaParams.InverseQ; 
            rsaCspObject.D = rsaParams.D;
            return rsaCspObject; 
        } 

        // Since P is required, we will assume its presence is synonymous to a private key. 
        private static bool IsPublic (RSAParameters rsaParams) {
            return (rsaParams.P == null);
        }
 
        // find whether an RSA key blob is public.
        private static bool IsPublic (byte[] keyBlob) { 
            if (keyBlob == null) 
                throw new ArgumentNullException("keyBlob");
 
            // The CAPI RSA public key representation consists of the following sequence:
            //  - BLOBHEADER
            //  - RSAPUBKEY
 
            // The first should be PUBLICKEYBLOB and magic should be RSA_PUB_MAGIC "RSA1"
            if (keyBlob[0] != Constants.PUBLICKEYBLOB) 
                return false; 

            if (keyBlob[11] != 0x31 || keyBlob[10] != 0x41 || keyBlob[9] != 0x53 || keyBlob[8] != 0x52) 
                return false;

            return true;
        } 
    }
} 
