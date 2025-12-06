// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// DSASignatureDeformatter.cs 
//
 
namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class DSASignatureDeformatter : AsymmetricSignatureDeformatter {
        DSA    _dsaKey; // DSA Key value to do decrypt operation 
        string _oid;
 
        // 
        // public constructors
        // 

        public DSASignatureDeformatter() {
            // The hash algorithm is always SHA1
            _oid = CryptoConfig.MapNameToOID("SHA1"); 
        }
 
        public DSASignatureDeformatter(AsymmetricAlgorithm key) : this() { 
            if (key == null)
                throw new ArgumentNullException("key"); 
            _dsaKey = (DSA) key;
        }

        // 
        // public methods
        // 
 
        public override void SetKey(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            _dsaKey = (DSA) key;
        }
 
        public override void SetHashAlgorithm(string strName) {
            if (CryptoConfig.MapNameToOID(strName) != _oid) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOperation")); 
        }
 
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) {
            if (_dsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            if (rgbHash == null) 
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null) 
                throw new ArgumentNullException("rgbSignature"); 

            return _dsaKey.VerifySignature(rgbHash, rgbSignature); 
        }
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// DSASignatureDeformatter.cs 
//
 
namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class DSASignatureDeformatter : AsymmetricSignatureDeformatter {
        DSA    _dsaKey; // DSA Key value to do decrypt operation 
        string _oid;
 
        // 
        // public constructors
        // 

        public DSASignatureDeformatter() {
            // The hash algorithm is always SHA1
            _oid = CryptoConfig.MapNameToOID("SHA1"); 
        }
 
        public DSASignatureDeformatter(AsymmetricAlgorithm key) : this() { 
            if (key == null)
                throw new ArgumentNullException("key"); 
            _dsaKey = (DSA) key;
        }

        // 
        // public methods
        // 
 
        public override void SetKey(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            _dsaKey = (DSA) key;
        }
 
        public override void SetHashAlgorithm(string strName) {
            if (CryptoConfig.MapNameToOID(strName) != _oid) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOperation")); 
        }
 
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) {
            if (_dsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            if (rgbHash == null) 
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null) 
                throw new ArgumentNullException("rgbSignature"); 

            return _dsaKey.VerifySignature(rgbHash, rgbSignature); 
        }
    }
}
