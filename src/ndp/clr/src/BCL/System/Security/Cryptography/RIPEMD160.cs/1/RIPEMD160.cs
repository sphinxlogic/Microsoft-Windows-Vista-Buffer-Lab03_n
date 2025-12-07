// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RIPEMD160.cs 
//
 
namespace System.Security.Cryptography {
    using System;
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class RIPEMD160 : HashAlgorithm 
    {
        // 
        // public constructors 
        //
 
        protected RIPEMD160() {
        }

        // 
        // public methods
        // 
 
        new static public RIPEMD160 Create() {
            return Create("System.Security.Cryptography.RIPEMD160"); 
        }

        new static public RIPEMD160 Create(String hashName) {
            return (RIPEMD160) CryptoConfig.CreateFromName(hashName); 
        }
    } 
} 

// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RIPEMD160.cs 
//
 
namespace System.Security.Cryptography {
    using System;
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class RIPEMD160 : HashAlgorithm 
    {
        // 
        // public constructors 
        //
 
        protected RIPEMD160() {
        }

        // 
        // public methods
        // 
 
        new static public RIPEMD160 Create() {
            return Create("System.Security.Cryptography.RIPEMD160"); 
        }

        new static public RIPEMD160 Create(String hashName) {
            return (RIPEMD160) CryptoConfig.CreateFromName(hashName); 
        }
    } 
} 

