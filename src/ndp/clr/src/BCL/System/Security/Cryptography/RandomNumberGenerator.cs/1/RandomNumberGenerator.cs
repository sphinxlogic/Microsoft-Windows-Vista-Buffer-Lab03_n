// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RandomNumberGenerator.cs 
//
 
namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class RandomNumberGenerator {
        protected RandomNumberGenerator() { 
        }
 
        // 
        // public methods
        // 

#if !FEATURE_PAL
        static public RandomNumberGenerator Create() {
            return Create("System.Security.Cryptography.RandomNumberGenerator"); 
        }
 
        static public RandomNumberGenerator Create(String rngName) { 
            return (RandomNumberGenerator) CryptoConfig.CreateFromName(rngName);
        } 
#endif

        public abstract void GetBytes(byte[] data);
 
        public abstract void GetNonZeroBytes(byte[] data);
    } 
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RandomNumberGenerator.cs 
//
 
namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class RandomNumberGenerator {
        protected RandomNumberGenerator() { 
        }
 
        // 
        // public methods
        // 

#if !FEATURE_PAL
        static public RandomNumberGenerator Create() {
            return Create("System.Security.Cryptography.RandomNumberGenerator"); 
        }
 
        static public RandomNumberGenerator Create(String rngName) { 
            return (RandomNumberGenerator) CryptoConfig.CreateFromName(rngName);
        } 
#endif

        public abstract void GetBytes(byte[] data);
 
        public abstract void GetNonZeroBytes(byte[] data);
    } 
} 
