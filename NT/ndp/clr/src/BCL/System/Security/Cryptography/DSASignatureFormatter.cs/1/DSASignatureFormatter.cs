// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// DSASignatureFormatter.cs 
//
 
namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class DSASignatureFormatter : AsymmetricSignatureFormatter {
        DSA    _dsaKey; 
        String _oid;
 
        // 
        // public constructors
        // 

        public DSASignatureFormatter() {
            // The hash algorithm is always SHA1
            _oid = CryptoConfig.MapNameToOID("SHA1"); 
        }
 
        public DSASignatureFormatter(AsymmetricAlgorithm key) : this() { 
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
 
        public override void SetHashAlgorithm(String strName) {
            if (CryptoConfig.MapNameToOID(strName) != _oid) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOperation")); 
        }
 
        public override byte[] CreateSignature(byte[] rgbHash) {
            if (_oid == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingOID"));
            if (_dsaKey == null) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            if (rgbHash == null) 
                throw new ArgumentNullException("rgbHash"); 

            return _dsaKey.CreateSignature(rgbHash); 
        }
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// DSASignatureFormatter.cs 
//
 
namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class DSASignatureFormatter : AsymmetricSignatureFormatter {
        DSA    _dsaKey; 
        String _oid;
 
        // 
        // public constructors
        // 

        public DSASignatureFormatter() {
            // The hash algorithm is always SHA1
            _oid = CryptoConfig.MapNameToOID("SHA1"); 
        }
 
        public DSASignatureFormatter(AsymmetricAlgorithm key) : this() { 
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
 
        public override void SetHashAlgorithm(String strName) {
            if (CryptoConfig.MapNameToOID(strName) != _oid) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOperation")); 
        }
 
        public override byte[] CreateSignature(byte[] rgbHash) {
            if (_oid == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingOID"));
            if (_dsaKey == null) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            if (rgbHash == null) 
                throw new ArgumentNullException("rgbHash"); 

            return _dsaKey.CreateSignature(rgbHash); 
        }
    }
}
