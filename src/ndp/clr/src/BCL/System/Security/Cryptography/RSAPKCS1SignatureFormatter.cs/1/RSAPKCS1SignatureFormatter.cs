// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RSAPKCS1SignatureFormatter.cs 
//
 
namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class RSAPKCS1SignatureFormatter : AsymmetricSignatureFormatter {
        private RSA    _rsaKey; 
        private String _strOID;
 
        // 
        // public constructors
        // 

        public RSAPKCS1SignatureFormatter() {}

        public RSAPKCS1SignatureFormatter(AsymmetricAlgorithm key) { 
            if (key == null)
                throw new ArgumentNullException("key"); 
            _rsaKey = (RSA) key; 
        }
 
        //
        // public methods
        //
 
        public override void SetKey(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key"); 
            _rsaKey = (RSA) key;
        } 

        public override void SetHashAlgorithm(String strName) {
            _strOID = CryptoConfig.MapNameToOID(strName);
        } 

        public override byte[] CreateSignature(byte[] rgbHash) { 
            if (_strOID == null) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingOID"));
            if (_rsaKey == null) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
 
            // Two cases here -- if we are talking to the CSP version or if we are talking to some other RSA provider.
            if (_rsaKey is RSACryptoServiceProvider) { 
                return ((RSACryptoServiceProvider) _rsaKey).SignHash(rgbHash, _strOID); 
            }
            else { 
                byte[] pad = Utils.RsaPkcs1Padding(_rsaKey, CryptoConfig.EncodeOID(_strOID), rgbHash);
                // Create the signature by applying the private key to the padded buffer we just created.
                return _rsaKey.DecryptValue(pad);
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
// RSAPKCS1SignatureFormatter.cs 
//
 
namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class RSAPKCS1SignatureFormatter : AsymmetricSignatureFormatter {
        private RSA    _rsaKey; 
        private String _strOID;
 
        // 
        // public constructors
        // 

        public RSAPKCS1SignatureFormatter() {}

        public RSAPKCS1SignatureFormatter(AsymmetricAlgorithm key) { 
            if (key == null)
                throw new ArgumentNullException("key"); 
            _rsaKey = (RSA) key; 
        }
 
        //
        // public methods
        //
 
        public override void SetKey(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key"); 
            _rsaKey = (RSA) key;
        } 

        public override void SetHashAlgorithm(String strName) {
            _strOID = CryptoConfig.MapNameToOID(strName);
        } 

        public override byte[] CreateSignature(byte[] rgbHash) { 
            if (_strOID == null) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingOID"));
            if (_rsaKey == null) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
 
            // Two cases here -- if we are talking to the CSP version or if we are talking to some other RSA provider.
            if (_rsaKey is RSACryptoServiceProvider) { 
                return ((RSACryptoServiceProvider) _rsaKey).SignHash(rgbHash, _strOID); 
            }
            else { 
                byte[] pad = Utils.RsaPkcs1Padding(_rsaKey, CryptoConfig.EncodeOID(_strOID), rgbHash);
                // Create the signature by applying the private key to the padded buffer we just created.
                return _rsaKey.DecryptValue(pad);
            } 
        }
    } 
} 
