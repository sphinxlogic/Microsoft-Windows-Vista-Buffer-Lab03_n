// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RNGCryptoServiceProvider.cs 
//
 
namespace System.Security.Cryptography {
    using Microsoft.Win32;
    using System.Runtime.InteropServices;
 
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class RNGCryptoServiceProvider : RandomNumberGenerator { 
#if !FEATURE_PAL 
        SafeProvHandle m_safeProvHandle;
#endif 

        //
        // public constructors
        // 

#if !FEATURE_PAL 
        public RNGCryptoServiceProvider() : this((CspParameters) null) {} 
#else
        public RNGCryptoServiceProvider() { } 
#endif

#if !FEATURE_PAL
        public RNGCryptoServiceProvider(string str) : this((CspParameters) null) {} 

        public RNGCryptoServiceProvider(byte[] rgb) : this((CspParameters) null) {} 
 
        public RNGCryptoServiceProvider(CspParameters cspParams) {
            if (cspParams != null) 
                m_safeProvHandle = Utils.AcquireProvHandle(cspParams);
            else
                m_safeProvHandle = Utils.StaticProvHandle;
        } 
#endif
 
        // 
        // public methods
        // 

        public override void GetBytes(byte[] data) {
            if (data == null) throw new ArgumentNullException("data");
#if !FEATURE_PAL 
            Utils._GetBytes(m_safeProvHandle, data);
#else 
            if (!Win32Native.Random(true, data, data.Length)) 
                throw new CryptographicException(Marshal.GetLastWin32Error());
#endif 
        }

        public override void GetNonZeroBytes(byte[] data) {
            if (data == null) 
                throw new ArgumentNullException("data");
 
#if !FEATURE_PAL 
            Utils._GetNonZeroBytes(m_safeProvHandle, data);
#else 
            GetBytes(data);

            int indexOfFirst0Byte = data.Length;
            for (int i = 0; i < data.Length; i++) { 
                if (data[i] == 0) {
                    indexOfFirst0Byte = i; 
                    break; 
                }
            } 
            for (int i = indexOfFirst0Byte; i < data.Length; i++) {
                if (data[i] != 0) {
                    data[indexOfFirst0Byte++] = data[i];
                } 
            }
 
            while (indexOfFirst0Byte < data.Length) { 
                // this should be more than enough to fill the rest in one iteration
                byte[] tmp = new byte[2 * (data.Length - indexOfFirst0Byte)]; 
                GetBytes(tmp);

                for (int i = 0; i < tmp.Length; i++) {
                    if (tmp[i] != 0) { 
                        data[indexOfFirst0Byte++] = tmp[i];
                        if (indexOfFirst0Byte >= data.Length) break; 
                    } 
                }
            } 
#endif
        }
    }
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// RNGCryptoServiceProvider.cs 
//
 
namespace System.Security.Cryptography {
    using Microsoft.Win32;
    using System.Runtime.InteropServices;
 
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class RNGCryptoServiceProvider : RandomNumberGenerator { 
#if !FEATURE_PAL 
        SafeProvHandle m_safeProvHandle;
#endif 

        //
        // public constructors
        // 

#if !FEATURE_PAL 
        public RNGCryptoServiceProvider() : this((CspParameters) null) {} 
#else
        public RNGCryptoServiceProvider() { } 
#endif

#if !FEATURE_PAL
        public RNGCryptoServiceProvider(string str) : this((CspParameters) null) {} 

        public RNGCryptoServiceProvider(byte[] rgb) : this((CspParameters) null) {} 
 
        public RNGCryptoServiceProvider(CspParameters cspParams) {
            if (cspParams != null) 
                m_safeProvHandle = Utils.AcquireProvHandle(cspParams);
            else
                m_safeProvHandle = Utils.StaticProvHandle;
        } 
#endif
 
        // 
        // public methods
        // 

        public override void GetBytes(byte[] data) {
            if (data == null) throw new ArgumentNullException("data");
#if !FEATURE_PAL 
            Utils._GetBytes(m_safeProvHandle, data);
#else 
            if (!Win32Native.Random(true, data, data.Length)) 
                throw new CryptographicException(Marshal.GetLastWin32Error());
#endif 
        }

        public override void GetNonZeroBytes(byte[] data) {
            if (data == null) 
                throw new ArgumentNullException("data");
 
#if !FEATURE_PAL 
            Utils._GetNonZeroBytes(m_safeProvHandle, data);
#else 
            GetBytes(data);

            int indexOfFirst0Byte = data.Length;
            for (int i = 0; i < data.Length; i++) { 
                if (data[i] == 0) {
                    indexOfFirst0Byte = i; 
                    break; 
                }
            } 
            for (int i = indexOfFirst0Byte; i < data.Length; i++) {
                if (data[i] != 0) {
                    data[indexOfFirst0Byte++] = data[i];
                } 
            }
 
            while (indexOfFirst0Byte < data.Length) { 
                // this should be more than enough to fill the rest in one iteration
                byte[] tmp = new byte[2 * (data.Length - indexOfFirst0Byte)]; 
                GetBytes(tmp);

                for (int i = 0; i < tmp.Length; i++) {
                    if (tmp[i] != 0) { 
                        data[indexOfFirst0Byte++] = tmp[i];
                        if (indexOfFirst0Byte >= data.Length) break; 
                    } 
                }
            } 
#endif
        }
    }
} 
