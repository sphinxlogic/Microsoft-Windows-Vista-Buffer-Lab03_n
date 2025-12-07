// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// AsymmetricSignatureFormatter.cs 
//
 
namespace System.Security.Cryptography {
    using System;

[System.Runtime.InteropServices.ComVisible(true)] 
    public abstract class AsymmetricSignatureFormatter {
        // 
        // protected constructors 
        //
 
        protected AsymmetricSignatureFormatter() {
        }

        // 
        // public methods
        // 
 
        abstract public void SetKey(AsymmetricAlgorithm key);
        abstract public void SetHashAlgorithm(String strName); 

        public virtual byte[] CreateSignature(HashAlgorithm hash) {
            if (hash == null) throw new ArgumentNullException("hash");
            SetHashAlgorithm(hash.ToString()); 
            return CreateSignature(hash.Hash);
        } 
 
        abstract public byte[] CreateSignature(byte[] rgbHash);
    } 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// AsymmetricSignatureFormatter.cs 
//
 
namespace System.Security.Cryptography {
    using System;

[System.Runtime.InteropServices.ComVisible(true)] 
    public abstract class AsymmetricSignatureFormatter {
        // 
        // protected constructors 
        //
 
        protected AsymmetricSignatureFormatter() {
        }

        // 
        // public methods
        // 
 
        abstract public void SetKey(AsymmetricAlgorithm key);
        abstract public void SetHashAlgorithm(String strName); 

        public virtual byte[] CreateSignature(HashAlgorithm hash) {
            if (hash == null) throw new ArgumentNullException("hash");
            SetHashAlgorithm(hash.ToString()); 
            return CreateSignature(hash.Hash);
        } 
 
        abstract public byte[] CreateSignature(byte[] rgbHash);
    } 
}
