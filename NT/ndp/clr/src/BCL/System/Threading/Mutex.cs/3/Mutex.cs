// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: Mutex	 
**
** 
** Purpose: synchronization primitive that can also be used for interprocess synchronization
**
**
=============================================================================*/ 
namespace System.Threading
{ 
    using System; 
    using System.Threading;
    using System.Runtime.CompilerServices; 
    using System.Security.Permissions;
    using System.IO;
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles; 
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.Versioning; 
    using System.Security.Principal;
    using System.Security; 

#if !FEATURE_PAL
    using System.Security.AccessControl;
#endif 

    [HostProtection(Synchronization=true, ExternalThreading=true)] 
    [ComVisible(true)] 
    public sealed class Mutex : WaitHandle
    { 
        static bool dummyBool;

        private const int WAIT_OBJECT_0 = 0;
        private const int WAIT_ABANDONED_0 = 0x80; 
        private const uint WAIT_FAILED = 0xFFFFFFFF;
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Mutex(bool initiallyOwned, String name, out bool createdNew)
#if !FEATURE_PAL
            : this(initiallyOwned, name, out createdNew, null) 
        {
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public unsafe Mutex(bool initiallyOwned, String name, out bool createdNew, MutexSecurity mutexSecurity)
#endif 
        {
            if(null != name && System.IO.Path.MAX_PATH < name.Length) 
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong",name));
            } 
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
#if !FEATURE_PAL
            // For ACL's, get the security descriptor from the MutexSecurity.
            if (mutexSecurity != null) { 
                secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs); 
 
                byte[] sd = mutexSecurity.GetSecurityDescriptorBinaryForm();
                byte* pSecDescriptor = stackalloc byte[sd.Length]; 
                Buffer.memcpy(sd, 0, pSecDescriptor, 0, sd.Length);
                secAttrs.pSecurityDescriptor = pSecDescriptor;
            }
#endif 
            SafeWaitHandle mutexHandle = null;
            bool newMutex = false; 
            RuntimeHelpers.CleanupCode cleanupCode = new RuntimeHelpers.CleanupCode(MutexCleanupCode); 
            MutexCleanupInfo cleanupInfo = new MutexCleanupInfo(mutexHandle, false);
            RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup( 
                delegate(object userData)  {  // try block
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try {
                    } 
                    finally {
                        if (initiallyOwned) { 
                            cleanupInfo.inCriticalRegion = true; 
                            Thread.BeginThreadAffinity();
                            Thread.BeginCriticalRegion(); 
                        }
                    }

                    int errorCode = 0; 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { 
                    } 
                    finally {
                        errorCode = CreateMutexHandle(initiallyOwned, name, secAttrs, out mutexHandle); 
                    }

                    if (mutexHandle.IsInvalid) {
                        mutexHandle.SetHandleAsInvalid(); 
                        if(null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                            throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name)); 
                        __Error.WinIOError(errorCode, name); 
                    }
                    newMutex = errorCode != Win32Native.ERROR_ALREADY_EXISTS; 
                    SetHandleInternal(mutexHandle);

                    hasThreadAffinity = true;
 
                },
                cleanupCode, 
                cleanupInfo); 
                createdNew = newMutex;
 
}

        [PrePrepareMethod]
        private void MutexCleanupCode(Object userData, bool exceptionThrown) 
        {
            MutexCleanupInfo cleanupInfo = (MutexCleanupInfo) userData; 
 
            // If hasThreadAffinity isn’t true, we’ve thrown an exception in the above try, and we must free the mutex
            // on this OS thread before ending our thread affninity. 
            if(!hasThreadAffinity) {
                if (cleanupInfo.mutexHandle != null && !cleanupInfo.mutexHandle.IsInvalid) {
                    if( cleanupInfo.inCriticalRegion) {
                        Win32Native.ReleaseMutex(cleanupInfo.mutexHandle); 
                    }
                    cleanupInfo.mutexHandle.Dispose(); 
 
                }
 
                if( cleanupInfo.inCriticalRegion) {
                    Thread.EndCriticalRegion();
                    Thread.EndThreadAffinity();
                } 
            }
        } 
 
        internal class MutexCleanupInfo
        { 
            internal SafeWaitHandle mutexHandle;
            internal bool inCriticalRegion;
            internal MutexCleanupInfo(SafeWaitHandle mutexHandle, bool inCriticalRegion)
            { 
                this.mutexHandle = mutexHandle;
                this.inCriticalRegion = inCriticalRegion; 
            } 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public Mutex(bool initiallyOwned, String name) : this(initiallyOwned, name, out dummyBool) {
        } 
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Mutex(bool initiallyOwned) : this(initiallyOwned, null, out dummyBool)
        {
        } 

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Mutex() : this(false, null, out dummyBool) 
        {
        }
 		
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        private Mutex(SafeWaitHandle handle)
        { 
            SetHandleInternal(handle); 
            hasThreadAffinity = true;
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand,Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public static Mutex OpenExisting(string name)
        { 
#if !FEATURE_PAL 
            return OpenExisting(name, MutexRights.Modify | MutexRights.Synchronize);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand,Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public static Mutex OpenExisting(string name, MutexRights rights)
        { 
#endif // !FEATURE_PAL 
            if (name == null)
            { 
                throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_WithParamName"));
            }

            if(name.Length  == 0) 
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name"); 
            } 
            if(System.IO.Path.MAX_PATH < name.Length)
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong",name));
            }

 
            // To allow users to view & edit the ACL's, call OpenMutex
            // with parameters to allow us to view & edit the ACL.  This will 
            // fail if we don't have permission to view or edit the ACL's. 
            // If that happens, ask for less permissions.
#if FEATURE_PAL 
            SafeWaitHandle myHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);
#else
            SafeWaitHandle myHandle = Win32Native.OpenMutex((int) rights, false, name);
#endif 

            int errorCode = 0; 
            if (myHandle.IsInvalid) 
            {
                errorCode = Marshal.GetLastWin32Error(); 

                if(Win32Native.ERROR_FILE_NOT_FOUND == errorCode || Win32Native.ERROR_INVALID_NAME == errorCode)
                {
                    throw new WaitHandleCannotBeOpenedException(); 
                }
 
                if(null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode) 
                {
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name)); 
                }

                // this is for passed through Win32Native Errors
                __Error.WinIOError(errorCode,name); 
            }
 
            return new Mutex(myHandle); 
        }
 
        // Note: To call ReleaseMutex, you must have an ACL granting you
        // MUTEX_MODIFY_STATE rights (0x0001).  The other interesting value
        // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        public void ReleaseMutex()
        { 
            if (Win32Native.ReleaseMutex(safeWaitHandle)) 
            {
                Thread.EndCriticalRegion(); 
                Thread.EndThreadAffinity();
            }
            else
            { 
                throw new ApplicationException(Environment.GetResourceString("Arg_SynchronizationLockException"));
            } 
        } 

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        static int CreateMutexHandle(bool initiallyOwned, String name, Win32Native.SECURITY_ATTRIBUTES securityAttribute, out SafeWaitHandle mutexHandle) {
            int errorCode;
            bool fAffinity = false;
            bool fHitRace = false; 
            bool fRetry = false;
			 
            while(true) { 
                fHitRace = false;
                fRetry = false; 

                mutexHandle = Win32Native.CreateMutex(securityAttribute, initiallyOwned, name);
                errorCode = Marshal.GetLastWin32Error();
                if( !mutexHandle.IsInvalid) { 
                    break;
		} 
 

                if( errorCode == Win32Native.ERROR_ACCESS_DENIED) { 
                    // If a mutex with the name already exists, OS will try to open it with FullAccess.
                    // It might fail if we don't have enough access. In that case, we try to open the mutex will modify and synchronize access.
                    //
 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try 
                    { 
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try 
                        {
                        }
                        finally
                        { 
                            Thread.BeginThreadAffinity();
                            fAffinity = true; 
                        } 
                        mutexHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);
                        if(!mutexHandle.IsInvalid) 
                        {
                            errorCode = Win32Native.ERROR_ALREADY_EXISTS;

                            #if !FEATURE_PAL 
                            if (Environment.IsW2k3)
                            { 
                                //Enables workaround for known OS bug at 
                                //http://support.microsoft.com/default.aspx?scid=kb;en-us;889318	
                                //Workaround suggested by NeillC (The previous workaround of a acquiring a global mutex was not secure): 
                                //1)Open the mutex twice.
                                //2)Call WaitForMultipleObjects  with bWaitAll=TRUE and a zero timeout
                                //3)If this call returns with ERROR_INVALID_PARAMETER then the two objects are the same and you can close one and use the other
                                //4)If it returns timeout then you hit the race 
                                //5)If it returns success then you hit the race but you have to release before close.
	 
 	    	                SafeWaitHandle tempMutexHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name); 

                                if(!tempMutexHandle.IsInvalid) 
                                {
                                    RuntimeHelpers.PrepareConstrainedRegions();
                                    try
                                    { 
                                        uint originalRetCode = 0;
 
                                        IntPtr mutexPtr = mutexHandle.DangerousGetHandle(); 
                                        IntPtr tempMutexPtr = tempMutexHandle.DangerousGetHandle();
 
                                        IntPtr[] waitHandles = new IntPtr[] {mutexPtr, tempMutexPtr};
					
 					//We directly Pinvoke to WaitForMultipleObjects for two reasons:
                                        //1. The VM method WaitForMultiple does not support doing WaitForMultipleObjects 
                                        //   for COM STA threads.
                                        //2. We call the method in such a way that it's guaranteed to not block, so 
                                        //   it should not lead to any COM pumping/rentrancy issues. 
                                        originalRetCode = Win32Native.WaitForMultipleObjects(2,waitHandles,true, 0);
                                        GC.KeepAlive(waitHandles); 

                                        if(originalRetCode == WAIT_FAILED)
                                        {
                                            uint retCode = (uint) Marshal.GetLastWin32Error(); 
                                            BCLDebug.Assert(retCode ==  Win32Native.ERROR_INVALID_PARAMETER, "Expected Invalid Parameter return code");
 
                                            //Some weird transient error? 
                                            if(retCode != Win32Native.ERROR_INVALID_PARAMETER)
                                            { 
                                                 mutexHandle.Dispose();
                                                 fRetry = true;
                                            }
                                        } 
                                        else
                                        { 
                                            fHitRace = true; 
                                            if((originalRetCode >= WAIT_OBJECT_0) && (originalRetCode < (WAIT_OBJECT_0+2)))
                                            { 
                                                Win32Native.ReleaseMutex(mutexHandle);
                                                Win32Native.ReleaseMutex(tempMutexHandle);
                                            }
                                            else if((originalRetCode >= WAIT_ABANDONED_0) && (originalRetCode < (WAIT_ABANDONED_0+2))) 
                                            {
                                                Win32Native.ReleaseMutex(mutexHandle); 
                                                Win32Native.ReleaseMutex(tempMutexHandle); 
                                            }
 
                                            mutexHandle.Dispose();
                                        }
                                    }
                                    finally 
                                    {
                                        tempMutexHandle.Dispose(); 
                                    } 
                                }
                                else 
                                {
                                    mutexHandle.Dispose();
                                    fRetry = true;
                                } 
                            }
                            #endif //!FEATURE_PAL 
                        } 
                        else
                        { 
                            errorCode = Marshal.GetLastWin32Error();
                        }
                    }
                    finally 
                    {
                        if (fAffinity)						 
                            Thread.EndThreadAffinity();						 
                    }
 
                    if(fHitRace || fRetry)
                        continue;

                    // There could be a race here, the other owner of the mutex can free the mutex, 
                    // We need to retry creation in that case.
                    if( errorCode != Win32Native.ERROR_FILE_NOT_FOUND) { 
                        if( errorCode == Win32Native.ERROR_SUCCESS) { 
                            errorCode =  Win32Native.ERROR_ALREADY_EXISTS;
                        } 
                        break;
                    }
                }
                else { 
                    break;
                } 
            } 
            return errorCode;
        } 

#if !FEATURE_PAL
        public MutexSecurity GetAccessControl()
        { 
            return new MutexSecurity(safeWaitHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        } 
 
        public void SetAccessControl(MutexSecurity mutexSecurity)
        { 
            if (mutexSecurity == null)
                throw new ArgumentNullException("mutexSecurity");

            mutexSecurity.Persist(safeWaitHandle); 
        }
#endif 
 
    }
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: Mutex	 
**
** 
** Purpose: synchronization primitive that can also be used for interprocess synchronization
**
**
=============================================================================*/ 
namespace System.Threading
{ 
    using System; 
    using System.Threading;
    using System.Runtime.CompilerServices; 
    using System.Security.Permissions;
    using System.IO;
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles; 
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.Versioning; 
    using System.Security.Principal;
    using System.Security; 

#if !FEATURE_PAL
    using System.Security.AccessControl;
#endif 

    [HostProtection(Synchronization=true, ExternalThreading=true)] 
    [ComVisible(true)] 
    public sealed class Mutex : WaitHandle
    { 
        static bool dummyBool;

        private const int WAIT_OBJECT_0 = 0;
        private const int WAIT_ABANDONED_0 = 0x80; 
        private const uint WAIT_FAILED = 0xFFFFFFFF;
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Mutex(bool initiallyOwned, String name, out bool createdNew)
#if !FEATURE_PAL
            : this(initiallyOwned, name, out createdNew, null) 
        {
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public unsafe Mutex(bool initiallyOwned, String name, out bool createdNew, MutexSecurity mutexSecurity)
#endif 
        {
            if(null != name && System.IO.Path.MAX_PATH < name.Length) 
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong",name));
            } 
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
#if !FEATURE_PAL
            // For ACL's, get the security descriptor from the MutexSecurity.
            if (mutexSecurity != null) { 
                secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs); 
 
                byte[] sd = mutexSecurity.GetSecurityDescriptorBinaryForm();
                byte* pSecDescriptor = stackalloc byte[sd.Length]; 
                Buffer.memcpy(sd, 0, pSecDescriptor, 0, sd.Length);
                secAttrs.pSecurityDescriptor = pSecDescriptor;
            }
#endif 
            SafeWaitHandle mutexHandle = null;
            bool newMutex = false; 
            RuntimeHelpers.CleanupCode cleanupCode = new RuntimeHelpers.CleanupCode(MutexCleanupCode); 
            MutexCleanupInfo cleanupInfo = new MutexCleanupInfo(mutexHandle, false);
            RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup( 
                delegate(object userData)  {  // try block
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try {
                    } 
                    finally {
                        if (initiallyOwned) { 
                            cleanupInfo.inCriticalRegion = true; 
                            Thread.BeginThreadAffinity();
                            Thread.BeginCriticalRegion(); 
                        }
                    }

                    int errorCode = 0; 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { 
                    } 
                    finally {
                        errorCode = CreateMutexHandle(initiallyOwned, name, secAttrs, out mutexHandle); 
                    }

                    if (mutexHandle.IsInvalid) {
                        mutexHandle.SetHandleAsInvalid(); 
                        if(null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                            throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name)); 
                        __Error.WinIOError(errorCode, name); 
                    }
                    newMutex = errorCode != Win32Native.ERROR_ALREADY_EXISTS; 
                    SetHandleInternal(mutexHandle);

                    hasThreadAffinity = true;
 
                },
                cleanupCode, 
                cleanupInfo); 
                createdNew = newMutex;
 
}

        [PrePrepareMethod]
        private void MutexCleanupCode(Object userData, bool exceptionThrown) 
        {
            MutexCleanupInfo cleanupInfo = (MutexCleanupInfo) userData; 
 
            // If hasThreadAffinity isn’t true, we’ve thrown an exception in the above try, and we must free the mutex
            // on this OS thread before ending our thread affninity. 
            if(!hasThreadAffinity) {
                if (cleanupInfo.mutexHandle != null && !cleanupInfo.mutexHandle.IsInvalid) {
                    if( cleanupInfo.inCriticalRegion) {
                        Win32Native.ReleaseMutex(cleanupInfo.mutexHandle); 
                    }
                    cleanupInfo.mutexHandle.Dispose(); 
 
                }
 
                if( cleanupInfo.inCriticalRegion) {
                    Thread.EndCriticalRegion();
                    Thread.EndThreadAffinity();
                } 
            }
        } 
 
        internal class MutexCleanupInfo
        { 
            internal SafeWaitHandle mutexHandle;
            internal bool inCriticalRegion;
            internal MutexCleanupInfo(SafeWaitHandle mutexHandle, bool inCriticalRegion)
            { 
                this.mutexHandle = mutexHandle;
                this.inCriticalRegion = inCriticalRegion; 
            } 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public Mutex(bool initiallyOwned, String name) : this(initiallyOwned, name, out dummyBool) {
        } 
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Mutex(bool initiallyOwned) : this(initiallyOwned, null, out dummyBool)
        {
        } 

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Mutex() : this(false, null, out dummyBool) 
        {
        }
 		
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        private Mutex(SafeWaitHandle handle)
        { 
            SetHandleInternal(handle); 
            hasThreadAffinity = true;
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand,Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public static Mutex OpenExisting(string name)
        { 
#if !FEATURE_PAL 
            return OpenExisting(name, MutexRights.Modify | MutexRights.Synchronize);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand,Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public static Mutex OpenExisting(string name, MutexRights rights)
        { 
#endif // !FEATURE_PAL 
            if (name == null)
            { 
                throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_WithParamName"));
            }

            if(name.Length  == 0) 
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name"); 
            } 
            if(System.IO.Path.MAX_PATH < name.Length)
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong",name));
            }

 
            // To allow users to view & edit the ACL's, call OpenMutex
            // with parameters to allow us to view & edit the ACL.  This will 
            // fail if we don't have permission to view or edit the ACL's. 
            // If that happens, ask for less permissions.
#if FEATURE_PAL 
            SafeWaitHandle myHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);
#else
            SafeWaitHandle myHandle = Win32Native.OpenMutex((int) rights, false, name);
#endif 

            int errorCode = 0; 
            if (myHandle.IsInvalid) 
            {
                errorCode = Marshal.GetLastWin32Error(); 

                if(Win32Native.ERROR_FILE_NOT_FOUND == errorCode || Win32Native.ERROR_INVALID_NAME == errorCode)
                {
                    throw new WaitHandleCannotBeOpenedException(); 
                }
 
                if(null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode) 
                {
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name)); 
                }

                // this is for passed through Win32Native Errors
                __Error.WinIOError(errorCode,name); 
            }
 
            return new Mutex(myHandle); 
        }
 
        // Note: To call ReleaseMutex, you must have an ACL granting you
        // MUTEX_MODIFY_STATE rights (0x0001).  The other interesting value
        // in a Mutex's ACL is MUTEX_ALL_ACCESS (0x1F0001).
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        public void ReleaseMutex()
        { 
            if (Win32Native.ReleaseMutex(safeWaitHandle)) 
            {
                Thread.EndCriticalRegion(); 
                Thread.EndThreadAffinity();
            }
            else
            { 
                throw new ApplicationException(Environment.GetResourceString("Arg_SynchronizationLockException"));
            } 
        } 

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        static int CreateMutexHandle(bool initiallyOwned, String name, Win32Native.SECURITY_ATTRIBUTES securityAttribute, out SafeWaitHandle mutexHandle) {
            int errorCode;
            bool fAffinity = false;
            bool fHitRace = false; 
            bool fRetry = false;
			 
            while(true) { 
                fHitRace = false;
                fRetry = false; 

                mutexHandle = Win32Native.CreateMutex(securityAttribute, initiallyOwned, name);
                errorCode = Marshal.GetLastWin32Error();
                if( !mutexHandle.IsInvalid) { 
                    break;
		} 
 

                if( errorCode == Win32Native.ERROR_ACCESS_DENIED) { 
                    // If a mutex with the name already exists, OS will try to open it with FullAccess.
                    // It might fail if we don't have enough access. In that case, we try to open the mutex will modify and synchronize access.
                    //
 
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try 
                    { 
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try 
                        {
                        }
                        finally
                        { 
                            Thread.BeginThreadAffinity();
                            fAffinity = true; 
                        } 
                        mutexHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);
                        if(!mutexHandle.IsInvalid) 
                        {
                            errorCode = Win32Native.ERROR_ALREADY_EXISTS;

                            #if !FEATURE_PAL 
                            if (Environment.IsW2k3)
                            { 
                                //Enables workaround for known OS bug at 
                                //http://support.microsoft.com/default.aspx?scid=kb;en-us;889318	
                                //Workaround suggested by NeillC (The previous workaround of a acquiring a global mutex was not secure): 
                                //1)Open the mutex twice.
                                //2)Call WaitForMultipleObjects  with bWaitAll=TRUE and a zero timeout
                                //3)If this call returns with ERROR_INVALID_PARAMETER then the two objects are the same and you can close one and use the other
                                //4)If it returns timeout then you hit the race 
                                //5)If it returns success then you hit the race but you have to release before close.
	 
 	    	                SafeWaitHandle tempMutexHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name); 

                                if(!tempMutexHandle.IsInvalid) 
                                {
                                    RuntimeHelpers.PrepareConstrainedRegions();
                                    try
                                    { 
                                        uint originalRetCode = 0;
 
                                        IntPtr mutexPtr = mutexHandle.DangerousGetHandle(); 
                                        IntPtr tempMutexPtr = tempMutexHandle.DangerousGetHandle();
 
                                        IntPtr[] waitHandles = new IntPtr[] {mutexPtr, tempMutexPtr};
					
 					//We directly Pinvoke to WaitForMultipleObjects for two reasons:
                                        //1. The VM method WaitForMultiple does not support doing WaitForMultipleObjects 
                                        //   for COM STA threads.
                                        //2. We call the method in such a way that it's guaranteed to not block, so 
                                        //   it should not lead to any COM pumping/rentrancy issues. 
                                        originalRetCode = Win32Native.WaitForMultipleObjects(2,waitHandles,true, 0);
                                        GC.KeepAlive(waitHandles); 

                                        if(originalRetCode == WAIT_FAILED)
                                        {
                                            uint retCode = (uint) Marshal.GetLastWin32Error(); 
                                            BCLDebug.Assert(retCode ==  Win32Native.ERROR_INVALID_PARAMETER, "Expected Invalid Parameter return code");
 
                                            //Some weird transient error? 
                                            if(retCode != Win32Native.ERROR_INVALID_PARAMETER)
                                            { 
                                                 mutexHandle.Dispose();
                                                 fRetry = true;
                                            }
                                        } 
                                        else
                                        { 
                                            fHitRace = true; 
                                            if((originalRetCode >= WAIT_OBJECT_0) && (originalRetCode < (WAIT_OBJECT_0+2)))
                                            { 
                                                Win32Native.ReleaseMutex(mutexHandle);
                                                Win32Native.ReleaseMutex(tempMutexHandle);
                                            }
                                            else if((originalRetCode >= WAIT_ABANDONED_0) && (originalRetCode < (WAIT_ABANDONED_0+2))) 
                                            {
                                                Win32Native.ReleaseMutex(mutexHandle); 
                                                Win32Native.ReleaseMutex(tempMutexHandle); 
                                            }
 
                                            mutexHandle.Dispose();
                                        }
                                    }
                                    finally 
                                    {
                                        tempMutexHandle.Dispose(); 
                                    } 
                                }
                                else 
                                {
                                    mutexHandle.Dispose();
                                    fRetry = true;
                                } 
                            }
                            #endif //!FEATURE_PAL 
                        } 
                        else
                        { 
                            errorCode = Marshal.GetLastWin32Error();
                        }
                    }
                    finally 
                    {
                        if (fAffinity)						 
                            Thread.EndThreadAffinity();						 
                    }
 
                    if(fHitRace || fRetry)
                        continue;

                    // There could be a race here, the other owner of the mutex can free the mutex, 
                    // We need to retry creation in that case.
                    if( errorCode != Win32Native.ERROR_FILE_NOT_FOUND) { 
                        if( errorCode == Win32Native.ERROR_SUCCESS) { 
                            errorCode =  Win32Native.ERROR_ALREADY_EXISTS;
                        } 
                        break;
                    }
                }
                else { 
                    break;
                } 
            } 
            return errorCode;
        } 

#if !FEATURE_PAL
        public MutexSecurity GetAccessControl()
        { 
            return new MutexSecurity(safeWaitHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        } 
 
        public void SetAccessControl(MutexSecurity mutexSecurity)
        { 
            if (mutexSecurity == null)
                throw new ArgumentNullException("mutexSecurity");

            mutexSecurity.Persist(safeWaitHandle); 
        }
#endif 
 
    }
} 
