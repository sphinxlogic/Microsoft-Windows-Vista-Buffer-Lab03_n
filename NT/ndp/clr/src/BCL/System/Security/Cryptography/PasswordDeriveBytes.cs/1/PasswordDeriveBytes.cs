// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// PasswordDerivedBytes.cs 
//
 
namespace System.Security.Cryptography {
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography.X509Certificates; 
    using System.Text;
    using System.Globalization; 
 
    [System.Runtime.InteropServices.ComVisible(true)]
    public class PasswordDeriveBytes : DeriveBytes { 
        private int             _extraCount;
        private int             _prefix;
        private int             _iterations;
        private byte[]          _baseValue; 
        private byte[]          _extra;
        private byte[]          _salt; 
        private string          _hashName; 
        private byte[]          _password;
        private HashAlgorithm   _hash; 
        private CspParameters   _cspParams;

        private SafeProvHandle _safeProvHandle = null;
        private SafeProvHandle ProvHandle { 
            get {
                if (_safeProvHandle == null) { 
                    lock (this) { 
                        if (_safeProvHandle == null) {
                            SafeProvHandle safeProvHandle = Utils.AcquireProvHandle(_cspParams); 
                            System.Threading.Thread.MemoryBarrier();
                            _safeProvHandle = safeProvHandle;
                        }
                    } 
                }
                return _safeProvHandle; 
            } 
        }
 
        //
        // public constructors
        //
 
        public PasswordDeriveBytes (String strPassword, byte[] rgbSalt) : this (strPassword, rgbSalt, new CspParameters()) {}
 
        public PasswordDeriveBytes (byte[] password, byte[] salt) : this (password, salt, new CspParameters()) {} 

        public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, string strHashName, int iterations) : 
            this (strPassword, rgbSalt, strHashName, iterations, new CspParameters()) {}

        public PasswordDeriveBytes (byte[] password, byte[] salt, string hashName, int iterations) :
            this (password, salt, hashName, iterations, new CspParameters()) {} 

 
        public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, CspParameters cspParams) : 
            this (strPassword, rgbSalt, "SHA1", 100, cspParams) {}
 
        public PasswordDeriveBytes (byte[] password, byte[] salt, CspParameters cspParams) :
            this (password, salt, "SHA1", 100, cspParams) {}

        public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, String strHashName, int iterations, CspParameters cspParams) : 
            this ((new UTF8Encoding(false)).GetBytes(strPassword), rgbSalt, strHashName, iterations, cspParams) {}
 
        public PasswordDeriveBytes (byte[] password, byte[] salt, String hashName, int iterations, CspParameters cspParams) { 
            this.IterationCount = iterations;
            this.Salt = salt; 
            this.HashName = hashName;
            _password = password;
            _cspParams = cspParams;
        } 

        // 
        // public properties 
        //
 
        public String HashName {
            get { return _hashName; }
            set {
                if (_baseValue != null) 
                    throw new CryptographicException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed"), "HashName"));
                _hashName = value; 
                _hash = (HashAlgorithm) CryptoConfig.CreateFromName(_hashName); 
            }
        } 

        public int IterationCount {
            get { return _iterations; }
            set { 
                if (_baseValue != null)
                    throw new CryptographicException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed"), "IterationCount")); 
                if (value <= 0) 
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                _iterations = value; 
            }
        }

        public byte[] Salt { 
            get {
                if (_salt == null) 
                    return null; 
                return (byte[]) _salt.Clone();
            } 
            set {
                if (_baseValue != null)
                    throw new CryptographicException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed"), "Salt"));
                if (value == null) 
                    _salt = null;
                else 
                    _salt = (byte[]) value.Clone(); 
            }
        } 

        //
        // public methods
        // 

        [Obsolete("Rfc2898DeriveBytes replaces PasswordDeriveBytes for deriving key material from a password and is preferred in new applications.")] 
        public override byte[] GetBytes(int cb) { 
            int         ib = 0;
            byte[]      rgb; 
            byte[]      rgbOut = new byte[cb];

            if (_baseValue == null) {
                ComputeBaseValue(); 
            }
            else if (_extra != null) { 
                ib = _extra.Length - _extraCount; 
                if (ib >= cb) {
                    Buffer.InternalBlockCopy(_extra, _extraCount, rgbOut, 0, cb); 
                    if (ib > cb)
                        _extraCount += cb;
                    else
                        _extra = null; 

                    return rgbOut; 
                } 
                else {
                    // 
                    // Note: The second parameter should really be _extraCount instead
                    // However, changing this would constitute a breaking change compared
                    // to what has shipped in V1.x.
                    // 

                    Buffer.InternalBlockCopy(_extra, ib, rgbOut, 0, ib); 
                    _extra = null; 
                }
            } 

            rgb = ComputeBytes(cb-ib);
            Buffer.InternalBlockCopy(rgb, 0, rgbOut, ib, cb-ib);
            if (rgb.Length + ib > cb) { 
                _extra = rgb;
                _extraCount = cb-ib; 
            } 
            return rgbOut;
        } 

        public override void Reset() {
            _prefix = 0;
            _extra = null; 
            _baseValue = null;
        } 
 
        public byte[] CryptDeriveKey (string algname, string alghashname, int keySize, byte[] rgbIV) {
            if (keySize < 0) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));

            int algidhash = X509Utils.OidToAlgId(alghashname);
            if (algidhash == 0) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidAlgorithm"));
            int algid = X509Utils.OidToAlgId(algname); 
            if (algid == 0) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidAlgorithm"));
 
            // Validate the rgbIV array
            if (rgbIV == null)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidIV"));
            return Utils._CryptDeriveKey(ProvHandle, algid, algidhash, _password, keySize << 16, rgbIV); 
        }
 
        // 
        // private methods
        // 

        private byte[] ComputeBaseValue() {
            _hash.Initialize();
            _hash.TransformBlock(_password, 0, _password.Length, _password, 0); 
            if (_salt != null)
                _hash.TransformBlock(_salt, 0, _salt.Length, _salt, 0); 
            _hash.TransformFinalBlock(new byte[0], 0, 0); 
            _baseValue = _hash.Hash;
            _hash.Initialize(); 

            for (int i=1; i<(_iterations-1); i++) {
                _hash.ComputeHash(_baseValue);
                _baseValue = _hash.Hash; 
            }
            return _baseValue; 
        } 

        private byte[] ComputeBytes(int cb) { 
            int                 cbHash;
            int                 ib = 0;
            byte[]              rgb;
 
            _hash.Initialize();
            cbHash = _hash.HashSize / 8; 
            rgb = new byte[((cb+cbHash-1)/cbHash)*cbHash]; 

            CryptoStream cs = new CryptoStream(Stream.Null, _hash, CryptoStreamMode.Write); 
            HashPrefix(cs);
            cs.Write(_baseValue, 0, _baseValue.Length);
            cs.Close();
            Buffer.InternalBlockCopy(_hash.Hash, 0, rgb, ib, cbHash); 
            ib += cbHash;
 
            while (cb > ib) { 
                _hash.Initialize();
                cs = new CryptoStream(Stream.Null, _hash, CryptoStreamMode.Write); 
                HashPrefix(cs);
                cs.Write(_baseValue, 0, _baseValue.Length);
                cs.Close();
                Buffer.InternalBlockCopy(_hash.Hash, 0, rgb, ib, cbHash); 
                ib += cbHash;
            } 
 
            return rgb;
        } 

        void HashPrefix(CryptoStream cs) {
            int    cb = 0;
            byte[] rgb = {(byte)'0', (byte)'0', (byte)'0'}; 

            if (_prefix > 999) 
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_TooManyBytes")); 

            if (_prefix >= 100) { 
                rgb[0] += (byte) (_prefix /100);
                cb += 1;
            }
            if (_prefix >= 10) { 
                rgb[cb] += (byte) ((_prefix % 100) / 10);
                cb += 1; 
            } 
            if (_prefix > 0) {
                rgb[cb] += (byte) (_prefix % 10); 
                cb += 1;
                cs.Write(rgb, 0, cb);
            }
            _prefix += 1; 
        }
    } 
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// PasswordDerivedBytes.cs 
//
 
namespace System.Security.Cryptography {
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography.X509Certificates; 
    using System.Text;
    using System.Globalization; 
 
    [System.Runtime.InteropServices.ComVisible(true)]
    public class PasswordDeriveBytes : DeriveBytes { 
        private int             _extraCount;
        private int             _prefix;
        private int             _iterations;
        private byte[]          _baseValue; 
        private byte[]          _extra;
        private byte[]          _salt; 
        private string          _hashName; 
        private byte[]          _password;
        private HashAlgorithm   _hash; 
        private CspParameters   _cspParams;

        private SafeProvHandle _safeProvHandle = null;
        private SafeProvHandle ProvHandle { 
            get {
                if (_safeProvHandle == null) { 
                    lock (this) { 
                        if (_safeProvHandle == null) {
                            SafeProvHandle safeProvHandle = Utils.AcquireProvHandle(_cspParams); 
                            System.Threading.Thread.MemoryBarrier();
                            _safeProvHandle = safeProvHandle;
                        }
                    } 
                }
                return _safeProvHandle; 
            } 
        }
 
        //
        // public constructors
        //
 
        public PasswordDeriveBytes (String strPassword, byte[] rgbSalt) : this (strPassword, rgbSalt, new CspParameters()) {}
 
        public PasswordDeriveBytes (byte[] password, byte[] salt) : this (password, salt, new CspParameters()) {} 

        public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, string strHashName, int iterations) : 
            this (strPassword, rgbSalt, strHashName, iterations, new CspParameters()) {}

        public PasswordDeriveBytes (byte[] password, byte[] salt, string hashName, int iterations) :
            this (password, salt, hashName, iterations, new CspParameters()) {} 

 
        public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, CspParameters cspParams) : 
            this (strPassword, rgbSalt, "SHA1", 100, cspParams) {}
 
        public PasswordDeriveBytes (byte[] password, byte[] salt, CspParameters cspParams) :
            this (password, salt, "SHA1", 100, cspParams) {}

        public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, String strHashName, int iterations, CspParameters cspParams) : 
            this ((new UTF8Encoding(false)).GetBytes(strPassword), rgbSalt, strHashName, iterations, cspParams) {}
 
        public PasswordDeriveBytes (byte[] password, byte[] salt, String hashName, int iterations, CspParameters cspParams) { 
            this.IterationCount = iterations;
            this.Salt = salt; 
            this.HashName = hashName;
            _password = password;
            _cspParams = cspParams;
        } 

        // 
        // public properties 
        //
 
        public String HashName {
            get { return _hashName; }
            set {
                if (_baseValue != null) 
                    throw new CryptographicException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed"), "HashName"));
                _hashName = value; 
                _hash = (HashAlgorithm) CryptoConfig.CreateFromName(_hashName); 
            }
        } 

        public int IterationCount {
            get { return _iterations; }
            set { 
                if (_baseValue != null)
                    throw new CryptographicException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed"), "IterationCount")); 
                if (value <= 0) 
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                _iterations = value; 
            }
        }

        public byte[] Salt { 
            get {
                if (_salt == null) 
                    return null; 
                return (byte[]) _salt.Clone();
            } 
            set {
                if (_baseValue != null)
                    throw new CryptographicException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed"), "Salt"));
                if (value == null) 
                    _salt = null;
                else 
                    _salt = (byte[]) value.Clone(); 
            }
        } 

        //
        // public methods
        // 

        [Obsolete("Rfc2898DeriveBytes replaces PasswordDeriveBytes for deriving key material from a password and is preferred in new applications.")] 
        public override byte[] GetBytes(int cb) { 
            int         ib = 0;
            byte[]      rgb; 
            byte[]      rgbOut = new byte[cb];

            if (_baseValue == null) {
                ComputeBaseValue(); 
            }
            else if (_extra != null) { 
                ib = _extra.Length - _extraCount; 
                if (ib >= cb) {
                    Buffer.InternalBlockCopy(_extra, _extraCount, rgbOut, 0, cb); 
                    if (ib > cb)
                        _extraCount += cb;
                    else
                        _extra = null; 

                    return rgbOut; 
                } 
                else {
                    // 
                    // Note: The second parameter should really be _extraCount instead
                    // However, changing this would constitute a breaking change compared
                    // to what has shipped in V1.x.
                    // 

                    Buffer.InternalBlockCopy(_extra, ib, rgbOut, 0, ib); 
                    _extra = null; 
                }
            } 

            rgb = ComputeBytes(cb-ib);
            Buffer.InternalBlockCopy(rgb, 0, rgbOut, ib, cb-ib);
            if (rgb.Length + ib > cb) { 
                _extra = rgb;
                _extraCount = cb-ib; 
            } 
            return rgbOut;
        } 

        public override void Reset() {
            _prefix = 0;
            _extra = null; 
            _baseValue = null;
        } 
 
        public byte[] CryptDeriveKey (string algname, string alghashname, int keySize, byte[] rgbIV) {
            if (keySize < 0) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));

            int algidhash = X509Utils.OidToAlgId(alghashname);
            if (algidhash == 0) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidAlgorithm"));
            int algid = X509Utils.OidToAlgId(algname); 
            if (algid == 0) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidAlgorithm"));
 
            // Validate the rgbIV array
            if (rgbIV == null)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidIV"));
            return Utils._CryptDeriveKey(ProvHandle, algid, algidhash, _password, keySize << 16, rgbIV); 
        }
 
        // 
        // private methods
        // 

        private byte[] ComputeBaseValue() {
            _hash.Initialize();
            _hash.TransformBlock(_password, 0, _password.Length, _password, 0); 
            if (_salt != null)
                _hash.TransformBlock(_salt, 0, _salt.Length, _salt, 0); 
            _hash.TransformFinalBlock(new byte[0], 0, 0); 
            _baseValue = _hash.Hash;
            _hash.Initialize(); 

            for (int i=1; i<(_iterations-1); i++) {
                _hash.ComputeHash(_baseValue);
                _baseValue = _hash.Hash; 
            }
            return _baseValue; 
        } 

        private byte[] ComputeBytes(int cb) { 
            int                 cbHash;
            int                 ib = 0;
            byte[]              rgb;
 
            _hash.Initialize();
            cbHash = _hash.HashSize / 8; 
            rgb = new byte[((cb+cbHash-1)/cbHash)*cbHash]; 

            CryptoStream cs = new CryptoStream(Stream.Null, _hash, CryptoStreamMode.Write); 
            HashPrefix(cs);
            cs.Write(_baseValue, 0, _baseValue.Length);
            cs.Close();
            Buffer.InternalBlockCopy(_hash.Hash, 0, rgb, ib, cbHash); 
            ib += cbHash;
 
            while (cb > ib) { 
                _hash.Initialize();
                cs = new CryptoStream(Stream.Null, _hash, CryptoStreamMode.Write); 
                HashPrefix(cs);
                cs.Write(_baseValue, 0, _baseValue.Length);
                cs.Close();
                Buffer.InternalBlockCopy(_hash.Hash, 0, rgb, ib, cbHash); 
                ib += cbHash;
            } 
 
            return rgb;
        } 

        void HashPrefix(CryptoStream cs) {
            int    cb = 0;
            byte[] rgb = {(byte)'0', (byte)'0', (byte)'0'}; 

            if (_prefix > 999) 
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_TooManyBytes")); 

            if (_prefix >= 100) { 
                rgb[0] += (byte) (_prefix /100);
                cb += 1;
            }
            if (_prefix >= 10) { 
                rgb[cb] += (byte) ((_prefix % 100) / 10);
                cb += 1; 
            } 
            if (_prefix > 0) {
                rgb[cb] += (byte) (_prefix % 10); 
                cb += 1;
                cs.Write(rgb, 0, cb);
            }
            _prefix += 1; 
        }
    } 
} 
