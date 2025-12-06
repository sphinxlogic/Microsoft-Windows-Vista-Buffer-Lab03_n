// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// MD5CryptoServiceProvider.cs 
//
 
namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class MD5CryptoServiceProvider : MD5 {
        private SafeHashHandle _safeHashHandle = null; 

        // 
        // public constructors 
        //
 
        public MD5CryptoServiceProvider() {
            if (Utils.FipsAlgorithmPolicy == 1)
                throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
 
            SafeHashHandle safeHashHandle = SafeHashHandle.InvalidHandle;
            // _CreateHash will check for failures and throw the appropriate exception 
            Utils._CreateHash(Utils.StaticProvHandle, Constants.CALG_MD5, ref safeHashHandle); 
           _safeHashHandle = safeHashHandle;
        } 

        protected override void Dispose(bool disposing) {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed)
                _safeHashHandle.Dispose(); 
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
            Utils._CreateHash(Utils.StaticProvHandle, Constants.CALG_MD5, ref safeHashHandle);
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
// MD5CryptoServiceProvider.cs 
//
 
namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class MD5CryptoServiceProvider : MD5 {
        private SafeHashHandle _safeHashHandle = null; 

        // 
        // public constructors 
        //
 
        public MD5CryptoServiceProvider() {
            if (Utils.FipsAlgorithmPolicy == 1)
                throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
 
            SafeHashHandle safeHashHandle = SafeHashHandle.InvalidHandle;
            // _CreateHash will check for failures and throw the appropriate exception 
            Utils._CreateHash(Utils.StaticProvHandle, Constants.CALG_MD5, ref safeHashHandle); 
           _safeHashHandle = safeHashHandle;
        } 

        protected override void Dispose(bool disposing) {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed)
                _safeHashHandle.Dispose(); 
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
            Utils._CreateHash(Utils.StaticProvHandle, Constants.CALG_MD5, ref safeHashHandle);
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
