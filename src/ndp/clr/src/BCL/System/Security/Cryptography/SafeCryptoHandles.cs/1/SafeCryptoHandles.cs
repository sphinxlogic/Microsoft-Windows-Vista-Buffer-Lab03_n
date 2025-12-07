// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// SafeCryptoHandles.cs 
//
 
namespace System.Security.Cryptography {
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution; 
    using Microsoft.Win32.SafeHandles;
 
    // Since we need sometimes to delete the key container created in the 
    // context of the CSP, the handle used in this class is actually a pointer
    // to a CRYPT_PROV_CTX unmanaged structure defined in COMCryptography.h 

    internal sealed class SafeProvHandle : SafeHandleZeroOrMinusOneIsInvalid {
        // 0 is an Invalid Handle
        private SafeProvHandle(IntPtr handle) : base (true) { 
            SetHandle(handle);
        } 
 
        internal static SafeProvHandle InvalidHandle {
            get { return new SafeProvHandle(IntPtr.Zero); } 
        }

        // This method handles the case where pProvCtx == NULL
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        // The handle here is actually a pointer to a CRYPT_PROV_CTX unmanaged structure 
        private static extern void _FreeCSP(IntPtr pProvCtx); 

        override protected bool ReleaseHandle() 
        {
            _FreeCSP(handle);
            return true;
        } 
    }
 
    // Since we need to delete the key handle before the provider is released 
    // we need to actually hold a pointer to a CRYPT_KEY_CTX unmanaged structure
    // whose destructor decrements a refCount. Only when the provider refCount is 0 
    // it is deleted. This way, we loose a race in the critical finalization of the key
    // handle and provider handle. This also applies to hash handles, which point to a
    // CRYPT_HASH_CTX. Those strucutres are defined in COMCryptography.h
 
    internal sealed class SafeKeyHandle : SafeHandleZeroOrMinusOneIsInvalid {
        // 0 is an Invalid Handle 
        private SafeKeyHandle(IntPtr handle) : base (true) { 
            SetHandle(handle);
        } 

        internal static SafeKeyHandle InvalidHandle {
            get { return new SafeKeyHandle(IntPtr.Zero); }
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        // The handle here is actually a pointer to a CRYPT_KEY_CTX unmanaged structure
        private static extern void _FreeHKey(IntPtr pKeyCtx); 

        override protected bool ReleaseHandle()
        {
            _FreeHKey(handle); 
            return true;
        } 
    } 

    internal sealed class SafeHashHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        // 0 is an Invalid Handle
        private SafeHashHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        internal static SafeHashHandle InvalidHandle { 
            get { return new SafeHashHandle(IntPtr.Zero); } 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        // The handle here is actually a pointer to a CRYPT_HASH_CTX unmanaged structure
        private static extern void _FreeHash(IntPtr pHashCtx); 

        override protected bool ReleaseHandle() 
        { 
            _FreeHash(handle);
            return true; 
        }
    }
}
 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// SafeCryptoHandles.cs 
//
 
namespace System.Security.Cryptography {
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution; 
    using Microsoft.Win32.SafeHandles;
 
    // Since we need sometimes to delete the key container created in the 
    // context of the CSP, the handle used in this class is actually a pointer
    // to a CRYPT_PROV_CTX unmanaged structure defined in COMCryptography.h 

    internal sealed class SafeProvHandle : SafeHandleZeroOrMinusOneIsInvalid {
        // 0 is an Invalid Handle
        private SafeProvHandle(IntPtr handle) : base (true) { 
            SetHandle(handle);
        } 
 
        internal static SafeProvHandle InvalidHandle {
            get { return new SafeProvHandle(IntPtr.Zero); } 
        }

        // This method handles the case where pProvCtx == NULL
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        // The handle here is actually a pointer to a CRYPT_PROV_CTX unmanaged structure 
        private static extern void _FreeCSP(IntPtr pProvCtx); 

        override protected bool ReleaseHandle() 
        {
            _FreeCSP(handle);
            return true;
        } 
    }
 
    // Since we need to delete the key handle before the provider is released 
    // we need to actually hold a pointer to a CRYPT_KEY_CTX unmanaged structure
    // whose destructor decrements a refCount. Only when the provider refCount is 0 
    // it is deleted. This way, we loose a race in the critical finalization of the key
    // handle and provider handle. This also applies to hash handles, which point to a
    // CRYPT_HASH_CTX. Those strucutres are defined in COMCryptography.h
 
    internal sealed class SafeKeyHandle : SafeHandleZeroOrMinusOneIsInvalid {
        // 0 is an Invalid Handle 
        private SafeKeyHandle(IntPtr handle) : base (true) { 
            SetHandle(handle);
        } 

        internal static SafeKeyHandle InvalidHandle {
            get { return new SafeKeyHandle(IntPtr.Zero); }
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        // The handle here is actually a pointer to a CRYPT_KEY_CTX unmanaged structure
        private static extern void _FreeHKey(IntPtr pKeyCtx); 

        override protected bool ReleaseHandle()
        {
            _FreeHKey(handle); 
            return true;
        } 
    } 

    internal sealed class SafeHashHandle : SafeHandleZeroOrMinusOneIsInvalid { 
        // 0 is an Invalid Handle
        private SafeHashHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        } 

        internal static SafeHashHandle InvalidHandle { 
            get { return new SafeHashHandle(IntPtr.Zero); } 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        // The handle here is actually a pointer to a CRYPT_HASH_CTX unmanaged structure
        private static extern void _FreeHash(IntPtr pHashCtx); 

        override protected bool ReleaseHandle() 
        { 
            _FreeHash(handle);
            return true; 
        }
    }
}
 
