// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// AsymmetricAlgorithm.cs 
//
 
namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class AsymmetricAlgorithm : IDisposable {
        protected int KeySizeValue; 
        protected KeySizes[] LegalKeySizesValue;
 
        // 
        // public constructors
        // 

        protected AsymmetricAlgorithm() {}

        // AsymmetricAlgorithm implements IDisposable 

        /// <internalonly/> 
        void IDisposable.Dispose() { 
            Dispose(true);
            GC.SuppressFinalize(this); 
        }

        public void Clear() {
            ((IDisposable) this).Dispose(); 
        }
 
        protected abstract void Dispose(bool disposing); 

        // 
        // public properties
        //

        public virtual int KeySize { 
            get { return KeySizeValue; }
            set { 
                int   i; 
                int   j;
 
                for (i=0; i<LegalKeySizesValue.Length; i++) {
                    if (LegalKeySizesValue[i].SkipSize == 0) {
                        if (LegalKeySizesValue[i].MinSize == value) { // assume MinSize = MaxSize
                            KeySizeValue = value; 
                            return;
                        } 
                    } else { 
                        for (j = LegalKeySizesValue[i].MinSize; j<=LegalKeySizesValue[i].MaxSize;
                             j += LegalKeySizesValue[i].SkipSize) { 
                            if (j == value) {
                                KeySizeValue = value;
                                return;
                            } 
                        }
                    } 
                } 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));
            } 
        }

        public virtual KeySizes[] LegalKeySizes {
            get { return (KeySizes[]) LegalKeySizesValue.Clone(); } 
        }
 
        public abstract String SignatureAlgorithm { 
            get;
        } 

        public abstract String KeyExchangeAlgorithm {
            get;
        } 

        // 
        // public methods 
        //
 
        static public AsymmetricAlgorithm Create() {
            // Use the crypto config system to return an instance of
            // the default AsymmetricAlgorithm on this machine
            return Create("System.Security.Cryptography.AsymmetricAlgorithm"); 
        }
 
        static public AsymmetricAlgorithm Create(String algName) { 
            return (AsymmetricAlgorithm) CryptoConfig.CreateFromName(algName);
        } 

        public abstract void FromXmlString(String xmlString);
        public abstract String ToXmlString(bool includePrivateParameters);
    } 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// AsymmetricAlgorithm.cs 
//
 
namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class AsymmetricAlgorithm : IDisposable {
        protected int KeySizeValue; 
        protected KeySizes[] LegalKeySizesValue;
 
        // 
        // public constructors
        // 

        protected AsymmetricAlgorithm() {}

        // AsymmetricAlgorithm implements IDisposable 

        /// <internalonly/> 
        void IDisposable.Dispose() { 
            Dispose(true);
            GC.SuppressFinalize(this); 
        }

        public void Clear() {
            ((IDisposable) this).Dispose(); 
        }
 
        protected abstract void Dispose(bool disposing); 

        // 
        // public properties
        //

        public virtual int KeySize { 
            get { return KeySizeValue; }
            set { 
                int   i; 
                int   j;
 
                for (i=0; i<LegalKeySizesValue.Length; i++) {
                    if (LegalKeySizesValue[i].SkipSize == 0) {
                        if (LegalKeySizesValue[i].MinSize == value) { // assume MinSize = MaxSize
                            KeySizeValue = value; 
                            return;
                        } 
                    } else { 
                        for (j = LegalKeySizesValue[i].MinSize; j<=LegalKeySizesValue[i].MaxSize;
                             j += LegalKeySizesValue[i].SkipSize) { 
                            if (j == value) {
                                KeySizeValue = value;
                                return;
                            } 
                        }
                    } 
                } 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));
            } 
        }

        public virtual KeySizes[] LegalKeySizes {
            get { return (KeySizes[]) LegalKeySizesValue.Clone(); } 
        }
 
        public abstract String SignatureAlgorithm { 
            get;
        } 

        public abstract String KeyExchangeAlgorithm {
            get;
        } 

        // 
        // public methods 
        //
 
        static public AsymmetricAlgorithm Create() {
            // Use the crypto config system to return an instance of
            // the default AsymmetricAlgorithm on this machine
            return Create("System.Security.Cryptography.AsymmetricAlgorithm"); 
        }
 
        static public AsymmetricAlgorithm Create(String algName) { 
            return (AsymmetricAlgorithm) CryptoConfig.CreateFromName(algName);
        } 

        public abstract void FromXmlString(String xmlString);
        public abstract String ToXmlString(bool includePrivateParameters);
    } 
}
