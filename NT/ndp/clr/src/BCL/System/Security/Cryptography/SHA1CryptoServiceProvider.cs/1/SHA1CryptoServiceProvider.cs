// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// SHA1CryptoServiceProvider.cs 
//
 
namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class SHA1CryptoServiceProvider : SHA1
    { 
        private SafeHashHandle _safeHashHandle = null;
 
        // 
        // public constructors
        // 

        public SHA1CryptoServiceProvider() {
            SafeHashHandle safeHashHandle = SafeHashHandle.InvalidHandle;
            // _CreateHash will check for failures and throw the appropriate exception 
            Utils._CreateHash(Utils.StaticProvHandle, Constants.CALG_SHA1, ref safeHashHandle);
           _safeHashHandle = safeHashHandle; 
        } 

        protected override void Dispose(bool disposing) 
        {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed)
                _safeHashHandle.Dispose();
            // call the base class's Dispose 
            base.Dispose(disposing);
        } 
 
        //
        // public methods 
        //

        public override void Initialize() {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed) 
                _safeHashHandle.Dispose();
            SafeHashHandle safeHashHandle = SafeHashHandle.InvalidHandle; 
            // _CreateHash will check for failures and throw the appropriate exception 
            Utils._CreateHash(Utils.StaticProvHandle, Constants.CALG_SHA1, ref safeHashHandle);
           _safeHashHandle = safeHashHandle; 
        }

        protected override void HashCore(byte[] rgb, int ibStart, int cbSize) {
            Utils._HashData(_safeHashHandle, rgb, ibStart, cbSize); 
        }
 
        protected override byte[] HashFinal() { 
            return Utils._EndHash(_safeHashHandle);
        } 
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// SHA1CryptoServiceProvider.cs 
//
 
namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class SHA1CryptoServiceProvider : SHA1
    { 
        private SafeHashHandle _safeHashHandle = null;
 
        // 
        // public constructors
        // 

        public SHA1CryptoServiceProvider() {
            SafeHashHandle safeHashHandle = SafeHashHandle.InvalidHandle;
            // _CreateHash will check for failures and throw the appropriate exception 
            Utils._CreateHash(Utils.StaticProvHandle, Constants.CALG_SHA1, ref safeHashHandle);
           _safeHashHandle = safeHashHandle; 
        } 

        protected override void Dispose(bool disposing) 
        {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed)
                _safeHashHandle.Dispose();
            // call the base class's Dispose 
            base.Dispose(disposing);
        } 
 
        //
        // public methods 
        //

        public override void Initialize() {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed) 
                _safeHashHandle.Dispose();
            SafeHashHandle safeHashHandle = SafeHashHandle.InvalidHandle; 
            // _CreateHash will check for failures and throw the appropriate exception 
            Utils._CreateHash(Utils.StaticProvHandle, Constants.CALG_SHA1, ref safeHashHandle);
           _safeHashHandle = safeHashHandle; 
        }

        protected override void HashCore(byte[] rgb, int ibStart, int cbSize) {
            Utils._HashData(_safeHashHandle, rgb, ibStart, cbSize); 
        }
 
        protected override byte[] HashFinal() { 
            return Utils._EndHash(_safeHashHandle);
        } 
    }
}
