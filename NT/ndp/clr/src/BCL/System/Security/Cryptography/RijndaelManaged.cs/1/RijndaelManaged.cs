// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RijndaelManaged.cs 
//
 
namespace System.Security.Cryptography
{
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class RijndaelManaged : Rijndael { 
        public RijndaelManaged () {
            if (Utils.FipsAlgorithmPolicy == 1) 
                throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm")); 
        }
 
        public override ICryptoTransform CreateEncryptor (byte[] rgbKey, byte[] rgbIV) {
            return NewEncryptor (rgbKey, ModeValue, rgbIV, FeedbackSizeValue, RijndaelManagedTransformMode.Encrypt);
        }
 
        public override ICryptoTransform CreateDecryptor (byte[] rgbKey, byte[] rgbIV) {
            return NewEncryptor (rgbKey, ModeValue, rgbIV, FeedbackSizeValue, RijndaelManagedTransformMode.Decrypt); 
        } 

        public override void GenerateKey () { 
            KeyValue = new byte[KeySizeValue/8];
            Utils.StaticRandomNumberGenerator.GetBytes(KeyValue);
        }
 
        public override void GenerateIV () {
            IVValue = new byte[BlockSizeValue/8]; 
            Utils.StaticRandomNumberGenerator.GetBytes(IVValue); 
        }
 
        private ICryptoTransform NewEncryptor (byte[] rgbKey,
                                               CipherMode mode,
                                               byte[] rgbIV,
                                               int feedbackSize, 
                                               RijndaelManagedTransformMode encryptMode) {
            // Build the key if one does not already exist 
            if (rgbKey == null) { 
                rgbKey = new byte[KeySizeValue/8];
                Utils.StaticRandomNumberGenerator.GetBytes(rgbKey); 
            }

            // If not ECB mode, make sure we have an IV
            if (mode != CipherMode.ECB) { 
                if (rgbIV == null) {
                    rgbIV = new byte[BlockSizeValue/8]; 
                    Utils.StaticRandomNumberGenerator.GetBytes(rgbIV); 
                }
            } 

            // Create the encryptor/decryptor object
            return new RijndaelManagedTransform (rgbKey,
                                                 mode, 
                                                 rgbIV,
                                                 BlockSizeValue, 
                                                 feedbackSize, 
                                                 PaddingValue,
                                                 encryptMode); 
        }
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RijndaelManaged.cs 
//
 
namespace System.Security.Cryptography
{
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class RijndaelManaged : Rijndael { 
        public RijndaelManaged () {
            if (Utils.FipsAlgorithmPolicy == 1) 
                throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm")); 
        }
 
        public override ICryptoTransform CreateEncryptor (byte[] rgbKey, byte[] rgbIV) {
            return NewEncryptor (rgbKey, ModeValue, rgbIV, FeedbackSizeValue, RijndaelManagedTransformMode.Encrypt);
        }
 
        public override ICryptoTransform CreateDecryptor (byte[] rgbKey, byte[] rgbIV) {
            return NewEncryptor (rgbKey, ModeValue, rgbIV, FeedbackSizeValue, RijndaelManagedTransformMode.Decrypt); 
        } 

        public override void GenerateKey () { 
            KeyValue = new byte[KeySizeValue/8];
            Utils.StaticRandomNumberGenerator.GetBytes(KeyValue);
        }
 
        public override void GenerateIV () {
            IVValue = new byte[BlockSizeValue/8]; 
            Utils.StaticRandomNumberGenerator.GetBytes(IVValue); 
        }
 
        private ICryptoTransform NewEncryptor (byte[] rgbKey,
                                               CipherMode mode,
                                               byte[] rgbIV,
                                               int feedbackSize, 
                                               RijndaelManagedTransformMode encryptMode) {
            // Build the key if one does not already exist 
            if (rgbKey == null) { 
                rgbKey = new byte[KeySizeValue/8];
                Utils.StaticRandomNumberGenerator.GetBytes(rgbKey); 
            }

            // If not ECB mode, make sure we have an IV
            if (mode != CipherMode.ECB) { 
                if (rgbIV == null) {
                    rgbIV = new byte[BlockSizeValue/8]; 
                    Utils.StaticRandomNumberGenerator.GetBytes(rgbIV); 
                }
            } 

            // Create the encryptor/decryptor object
            return new RijndaelManagedTransform (rgbKey,
                                                 mode, 
                                                 rgbIV,
                                                 BlockSizeValue, 
                                                 feedbackSize, 
                                                 PaddingValue,
                                                 encryptMode); 
        }
    }
}
