//------------------------------------------------------------------------------ 
// <copyright file="TdsParserSafeHandles.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
 
    using System;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Runtime.CompilerServices; 
    using System.Runtime.InteropServices;
    using System.Security; 
    using System.Security.Permissions; 
    using System.Threading;
    using System.Runtime.ConstrainedExecution; 

    internal sealed class SNILoadHandle : SafeHandle {
        internal static readonly SNILoadHandle SingletonInstance = new SNILoadHandle();
 
        internal readonly SNINativeMethodWrapper.SqlAsyncCallbackDelegate ReadAsyncCallbackDispatcher  = new SNINativeMethodWrapper.SqlAsyncCallbackDelegate(ReadDispatcher);
        internal readonly SNINativeMethodWrapper.SqlAsyncCallbackDelegate WriteAsyncCallbackDispatcher = new SNINativeMethodWrapper.SqlAsyncCallbackDelegate(WriteDispatcher); 
 
        private readonly UInt32            _sniStatus        = TdsEnums.SNI_UNINITIALIZED;
        private readonly EncryptionOptions _encryptionOption; 

        private SNILoadHandle() : base(IntPtr.Zero, true) {
            // SQL BU DT 346588 - from security review - SafeHandle guarantees this is only called once.
            // The reason for the safehandle is guaranteed initialization and termination of SNI to 
            // ensure SNI terminates and cleans up properly.
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {} finally { 

                _sniStatus = SNINativeMethodWrapper.SNIInitialize(); 
                UInt32 value = 0;

                // Query OS to find out whether encryption is supported.
                SNINativeMethodWrapper.SNIQueryInfo(SNINativeMethodWrapper.QTypes.SNI_QUERY_CLIENT_ENCRYPT_POSSIBLE, ref value); 

                _encryptionOption = (value == 0) ? EncryptionOptions.NOT_SUP : EncryptionOptions.OFF; 
 
                base.handle = (IntPtr) 1; // Initialize to non-zero dummy variable.
            } 
        }

        public override bool IsInvalid {
            get { 
                return (IntPtr.Zero == base.handle);
            } 
        } 

        override protected bool ReleaseHandle() { 
            if (base.handle != IntPtr.Zero) {
                if (TdsEnums.SNI_SUCCESS == _sniStatus) {
                    SNINativeMethodWrapper.SNITerminate();
                } 
                base.handle = IntPtr.Zero;
            } 
 
            return true;
        } 

        public UInt32 SNIStatus {
            get {
                return _sniStatus; 
            }
        } 
 
        public EncryptionOptions Options {
            get { 
                return _encryptionOption;
            }
        }
 
        static private void ReadDispatcher(IntPtr key, IntPtr packet, UInt32 error) {
            // This is the app-domain dispatcher for all async read callbacks, It 
            // simply gets the state object from the key that it is passed, and 
            // calls the state object's read callback.
            Debug.Assert(IntPtr.Zero != key, "no key passed to read callback dispatcher?"); 
            if (IntPtr.Zero != key) {
                // NOTE: we will get a null ref here if we don't get a key that
                //       contains a GCHandle to TDSParserStateObject; that is
                //       very bad, and we want that to occur so we can catch it. 
                GCHandle gcHandle = (GCHandle)key;
                TdsParserStateObject stateObj = (TdsParserStateObject)gcHandle.Target; 
 
                if (null != stateObj) {
                    stateObj.ReadAsyncCallback(IntPtr.Zero, packet, error); 
                }
            }
        }
 
        static private void WriteDispatcher(IntPtr key, IntPtr packet, UInt32 error) {
            // This is the app-domain dispatcher for all async write callbacks, 
            // It simply gets the state object from the key that it is passed, 
            // and calls the state object's write callback.
            Debug.Assert(IntPtr.Zero != key, "no key passed to write callback dispatcher?"); 
            if (IntPtr.Zero != key) {
                // NOTE: we will get a null ref here if we don't get a key that
                //       contains a GCHandle to TDSParserStateObject; that is
                //       very bad, and we want that to occur so we can catch it. 
                GCHandle gcHandle = (GCHandle)key;
                TdsParserStateObject stateObj = (TdsParserStateObject)gcHandle.Target; 
 
                if (null != stateObj) {
                    stateObj.WriteAsyncCallback(IntPtr.Zero, packet, error); 
                }
            }
        }
 
    }
 
    internal sealed class SNIHandle : SafeHandle { 
        private readonly UInt32 _status = TdsEnums.SNI_UNINITIALIZED;
        private readonly bool   _fSync = false; 

        internal SNIHandle(SNINativeMethodWrapper.ConsumerInfo myInfo, string serverName, bool integratedSecurity,
                        byte[] serverUserName, bool ignoreSniOpenTimeout, int timeout, out byte[] instanceName,
                        bool flushCache, bool fSync) 
                : base(IntPtr.Zero, true) {
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {} finally { 
                _fSync = fSync;
                instanceName = new byte[256]; // Size as specified by netlibs. 
                if (ignoreSniOpenTimeout) {
                    //

 
                    _status = SNINativeMethodWrapper.SNIOpenEx (myInfo, serverName, ref base.handle,
                                integratedSecurity, serverUserName, instanceName, flushCache, fSync); 
                } 
                else {
                    _status = SNINativeMethodWrapper.SNIOpenSyncEx (myInfo, serverName, ref base.handle, integratedSecurity, 
                                serverUserName, instanceName, flushCache, fSync, timeout);
                }
            }
        } 

        internal SNIHandle(SNINativeMethodWrapper.ConsumerInfo myInfo, string serverName, SNIHandle parent) : base(IntPtr.Zero, true) { 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {} finally {
                _status = SNINativeMethodWrapper.SNIOpen(myInfo, serverName, parent, ref base.handle, parent._fSync); 
            }
        }

        public override bool IsInvalid { 
            get {
                return (IntPtr.Zero == base.handle); 
            } 
        }
 
        override protected bool ReleaseHandle() {
            // NOTE: The SafeHandle class guarantees this will be called exactly once.
            IntPtr ptr = base.handle;
            base.handle = IntPtr.Zero; 
            if (IntPtr.Zero != ptr) {
                if (0 != SNINativeMethodWrapper.SNIClose(ptr)) { 
                    return false;   // SNIClose should never fail. 
                }
            } 
            return true;
        }

        internal UInt32 Status { 
            get {
                return _status; 
            } 
        }
    } 

    internal sealed class SNIPacket : SafeHandle {

        internal SNIPacket(SafeHandle sniHandle) : base(IntPtr.Zero, true) { 
            SNINativeMethodWrapper.SNIPacketAllocate(sniHandle, SNINativeMethodWrapper.IOType.WRITE, ref base.handle);
            if (IntPtr.Zero == base.handle) { 
                throw SQL.SNIPacketAllocationFailure(); 
            }
        } 

        public override bool IsInvalid {
            get {
                return (IntPtr.Zero == base.handle); 
            }
        } 
 
        override protected bool ReleaseHandle() {
            // NOTE: The SafeHandle class guarantees this will be called exactly once. 
            IntPtr ptr = base.handle;
            base.handle = IntPtr.Zero;
            if (IntPtr.Zero != ptr) {
               SNINativeMethodWrapper.SNIPacketRelease(ptr); 
            }
            return true; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TdsParserSafeHandles.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.SqlClient {
 
    using System;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Runtime.CompilerServices; 
    using System.Runtime.InteropServices;
    using System.Security; 
    using System.Security.Permissions; 
    using System.Threading;
    using System.Runtime.ConstrainedExecution; 

    internal sealed class SNILoadHandle : SafeHandle {
        internal static readonly SNILoadHandle SingletonInstance = new SNILoadHandle();
 
        internal readonly SNINativeMethodWrapper.SqlAsyncCallbackDelegate ReadAsyncCallbackDispatcher  = new SNINativeMethodWrapper.SqlAsyncCallbackDelegate(ReadDispatcher);
        internal readonly SNINativeMethodWrapper.SqlAsyncCallbackDelegate WriteAsyncCallbackDispatcher = new SNINativeMethodWrapper.SqlAsyncCallbackDelegate(WriteDispatcher); 
 
        private readonly UInt32            _sniStatus        = TdsEnums.SNI_UNINITIALIZED;
        private readonly EncryptionOptions _encryptionOption; 

        private SNILoadHandle() : base(IntPtr.Zero, true) {
            // SQL BU DT 346588 - from security review - SafeHandle guarantees this is only called once.
            // The reason for the safehandle is guaranteed initialization and termination of SNI to 
            // ensure SNI terminates and cleans up properly.
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {} finally { 

                _sniStatus = SNINativeMethodWrapper.SNIInitialize(); 
                UInt32 value = 0;

                // Query OS to find out whether encryption is supported.
                SNINativeMethodWrapper.SNIQueryInfo(SNINativeMethodWrapper.QTypes.SNI_QUERY_CLIENT_ENCRYPT_POSSIBLE, ref value); 

                _encryptionOption = (value == 0) ? EncryptionOptions.NOT_SUP : EncryptionOptions.OFF; 
 
                base.handle = (IntPtr) 1; // Initialize to non-zero dummy variable.
            } 
        }

        public override bool IsInvalid {
            get { 
                return (IntPtr.Zero == base.handle);
            } 
        } 

        override protected bool ReleaseHandle() { 
            if (base.handle != IntPtr.Zero) {
                if (TdsEnums.SNI_SUCCESS == _sniStatus) {
                    SNINativeMethodWrapper.SNITerminate();
                } 
                base.handle = IntPtr.Zero;
            } 
 
            return true;
        } 

        public UInt32 SNIStatus {
            get {
                return _sniStatus; 
            }
        } 
 
        public EncryptionOptions Options {
            get { 
                return _encryptionOption;
            }
        }
 
        static private void ReadDispatcher(IntPtr key, IntPtr packet, UInt32 error) {
            // This is the app-domain dispatcher for all async read callbacks, It 
            // simply gets the state object from the key that it is passed, and 
            // calls the state object's read callback.
            Debug.Assert(IntPtr.Zero != key, "no key passed to read callback dispatcher?"); 
            if (IntPtr.Zero != key) {
                // NOTE: we will get a null ref here if we don't get a key that
                //       contains a GCHandle to TDSParserStateObject; that is
                //       very bad, and we want that to occur so we can catch it. 
                GCHandle gcHandle = (GCHandle)key;
                TdsParserStateObject stateObj = (TdsParserStateObject)gcHandle.Target; 
 
                if (null != stateObj) {
                    stateObj.ReadAsyncCallback(IntPtr.Zero, packet, error); 
                }
            }
        }
 
        static private void WriteDispatcher(IntPtr key, IntPtr packet, UInt32 error) {
            // This is the app-domain dispatcher for all async write callbacks, 
            // It simply gets the state object from the key that it is passed, 
            // and calls the state object's write callback.
            Debug.Assert(IntPtr.Zero != key, "no key passed to write callback dispatcher?"); 
            if (IntPtr.Zero != key) {
                // NOTE: we will get a null ref here if we don't get a key that
                //       contains a GCHandle to TDSParserStateObject; that is
                //       very bad, and we want that to occur so we can catch it. 
                GCHandle gcHandle = (GCHandle)key;
                TdsParserStateObject stateObj = (TdsParserStateObject)gcHandle.Target; 
 
                if (null != stateObj) {
                    stateObj.WriteAsyncCallback(IntPtr.Zero, packet, error); 
                }
            }
        }
 
    }
 
    internal sealed class SNIHandle : SafeHandle { 
        private readonly UInt32 _status = TdsEnums.SNI_UNINITIALIZED;
        private readonly bool   _fSync = false; 

        internal SNIHandle(SNINativeMethodWrapper.ConsumerInfo myInfo, string serverName, bool integratedSecurity,
                        byte[] serverUserName, bool ignoreSniOpenTimeout, int timeout, out byte[] instanceName,
                        bool flushCache, bool fSync) 
                : base(IntPtr.Zero, true) {
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {} finally { 
                _fSync = fSync;
                instanceName = new byte[256]; // Size as specified by netlibs. 
                if (ignoreSniOpenTimeout) {
                    //

 
                    _status = SNINativeMethodWrapper.SNIOpenEx (myInfo, serverName, ref base.handle,
                                integratedSecurity, serverUserName, instanceName, flushCache, fSync); 
                } 
                else {
                    _status = SNINativeMethodWrapper.SNIOpenSyncEx (myInfo, serverName, ref base.handle, integratedSecurity, 
                                serverUserName, instanceName, flushCache, fSync, timeout);
                }
            }
        } 

        internal SNIHandle(SNINativeMethodWrapper.ConsumerInfo myInfo, string serverName, SNIHandle parent) : base(IntPtr.Zero, true) { 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {} finally {
                _status = SNINativeMethodWrapper.SNIOpen(myInfo, serverName, parent, ref base.handle, parent._fSync); 
            }
        }

        public override bool IsInvalid { 
            get {
                return (IntPtr.Zero == base.handle); 
            } 
        }
 
        override protected bool ReleaseHandle() {
            // NOTE: The SafeHandle class guarantees this will be called exactly once.
            IntPtr ptr = base.handle;
            base.handle = IntPtr.Zero; 
            if (IntPtr.Zero != ptr) {
                if (0 != SNINativeMethodWrapper.SNIClose(ptr)) { 
                    return false;   // SNIClose should never fail. 
                }
            } 
            return true;
        }

        internal UInt32 Status { 
            get {
                return _status; 
            } 
        }
    } 

    internal sealed class SNIPacket : SafeHandle {

        internal SNIPacket(SafeHandle sniHandle) : base(IntPtr.Zero, true) { 
            SNINativeMethodWrapper.SNIPacketAllocate(sniHandle, SNINativeMethodWrapper.IOType.WRITE, ref base.handle);
            if (IntPtr.Zero == base.handle) { 
                throw SQL.SNIPacketAllocationFailure(); 
            }
        } 

        public override bool IsInvalid {
            get {
                return (IntPtr.Zero == base.handle); 
            }
        } 
 
        override protected bool ReleaseHandle() {
            // NOTE: The SafeHandle class guarantees this will be called exactly once. 
            IntPtr ptr = base.handle;
            base.handle = IntPtr.Zero;
            if (IntPtr.Zero != ptr) {
               SNINativeMethodWrapper.SNIPacketRelease(ptr); 
            }
            return true; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
