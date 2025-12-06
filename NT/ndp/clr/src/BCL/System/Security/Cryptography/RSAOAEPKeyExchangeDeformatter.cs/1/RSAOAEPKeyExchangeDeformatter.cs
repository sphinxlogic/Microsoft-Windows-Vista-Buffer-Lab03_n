// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

namespace System.Security.Cryptography { 
    [System.Runtime.InteropServices.ComVisible(true)] 
    public class RSAOAEPKeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter {
        private RSA _rsaKey; // RSA Key value to do decrypt operation 

        //
        // public constructors
        // 

        public RSAOAEPKeyExchangeDeformatter() {} 
        public RSAOAEPKeyExchangeDeformatter(AsymmetricAlgorithm key) { 
            if (key == null)
                throw new ArgumentNullException("key"); 
            _rsaKey = (RSA) key;
        }

        // 
        // public properties
        // 
 
        public override String Parameters {
            get { return null; } 
            set { ; }
        }

        // 
        // public methods
        // 
 
        public override byte[] DecryptKeyExchange(byte[] rgbData) {
            if (_rsaKey == null) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));

            if (_rsaKey is RSACryptoServiceProvider) {
                return ((RSACryptoServiceProvider) _rsaKey).Decrypt(rgbData, true); 
            } else {
                return Utils.RsaOaepDecrypt(_rsaKey, SHA1.Create(), new PKCS1MaskGenerationMethod(), rgbData); 
            } 
        }
 
        public override void SetKey(AsymmetricAlgorithm key) {
            if (key == null)
                throw new ArgumentNullException("key");
            _rsaKey = (RSA) key; 
        }
    } 
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

namespace System.Security.Cryptography { 
    [System.Runtime.InteropServices.ComVisible(true)] 
    public class RSAOAEPKeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter {
        private RSA _rsaKey; // RSA Key value to do decrypt operation 

        //
        // public constructors
        // 

        public RSAOAEPKeyExchangeDeformatter() {} 
        public RSAOAEPKeyExchangeDeformatter(AsymmetricAlgorithm key) { 
            if (key == null)
                throw new ArgumentNullException("key"); 
            _rsaKey = (RSA) key;
        }

        // 
        // public properties
        // 
 
        public override String Parameters {
            get { return null; } 
            set { ; }
        }

        // 
        // public methods
        // 
 
        public override byte[] DecryptKeyExchange(byte[] rgbData) {
            if (_rsaKey == null) 
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));

            if (_rsaKey is RSACryptoServiceProvider) {
                return ((RSACryptoServiceProvider) _rsaKey).Decrypt(rgbData, true); 
            } else {
                return Utils.RsaOaepDecrypt(_rsaKey, SHA1.Create(), new PKCS1MaskGenerationMethod(), rgbData); 
            } 
        }
 
        public override void SetKey(AsymmetricAlgorithm key) {
            if (key == null)
                throw new ArgumentNullException("key");
            _rsaKey = (RSA) key; 
        }
    } 
} 
