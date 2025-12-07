// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

namespace System.Threading 
{ 
    using System.IO;
    using System.IO.Ports; 
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.InteropServices;
    using System.Threading; 
    using System.Security.Permissions;
#if !FEATURE_PAL 
    using System.Security.AccessControl; 
#endif
    using System.Runtime.Versioning; 
    using System.Runtime.ConstrainedExecution;


    [HostProtection(Synchronization=true, ExternalThreading=true)] 
    [ComVisibleAttribute(false)]
    public sealed class Semaphore: WaitHandle 
    { 
        private static int MAX_PATH = 260;
        // creates a nameless semaphore object 
        // Win32 only takes maximum count of Int32.MaxValue
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Semaphore(int initialCount, int maximumCount) : this(initialCount,maximumCount,null){} 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)] 
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public Semaphore(int initialCount, int maximumCount, string name) 
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount", SR.GetString(SR.ArgumentOutOfRange_NeedNonNegNumRequired)); 
            }
 
            if (maximumCount < 1) 
            {
                throw new ArgumentOutOfRangeException("maximumCount", SR.GetString(SR.ArgumentOutOfRange_NeedNonNegNumRequired)); 
            }

            if (initialCount > maximumCount)
            { 
                throw new ArgumentException(SR.GetString(SR.Argument_SemaphoreInitialMaximum));
            } 
 
            if(null != name && MAX_PATH < name.Length)
            { 
                throw new ArgumentException(SR.GetString(SR.Argument_WaitHandleNameTooLong));
            }
            SafeWaitHandle   myHandle = SafeNativeMethods.CreateSemaphore(null, initialCount, maximumCount, name);
 
            if (myHandle.IsInvalid)
            { 
                int errorCode = Marshal.GetLastWin32Error(); 

                if(null != name && 0 != name.Length && NativeMethods.ERROR_INVALID_HANDLE == errorCode) 
                    throw new WaitHandleCannotBeOpenedException(SR.GetString(SR.WaitHandleCannotBeOpenedException_InvalidHandle,name));

                InternalResources.WinIOError();
            } 
            this.SafeWaitHandle = myHandle;
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public Semaphore(int initialCount, int maximumCount, string name, out bool createdNew)
#if !FEATURE_PAL
            : this(initialCount, maximumCount, name, out createdNew, null) 
        {
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public unsafe Semaphore(int initialCount, int maximumCount, string name, out bool createdNew, SemaphoreSecurity semaphoreSecurity)
#endif
        { 
            if (initialCount < 0)
            { 
                throw new ArgumentOutOfRangeException("initialCount", SR.GetString(SR.ArgumentOutOfRange_NeedNonNegNumRequired)); 
            }
 
            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException("maximumCount", SR.GetString(SR.ArgumentOutOfRange_NeedNonNegNumRequired));
            } 

            if (initialCount > maximumCount) 
            { 
                throw new ArgumentException(SR.GetString(SR.Argument_SemaphoreInitialMaximum));
            } 

            if(null != name && MAX_PATH < name.Length)
            {
                throw new ArgumentException(SR.GetString(SR.Argument_WaitHandleNameTooLong)); 
            }
            SafeWaitHandle   myHandle; 
#if !FEATURE_PAL 
            // For ACL's, get the security descriptor from the SemaphoreSecurity.
            if (semaphoreSecurity != null) { 
                NativeMethods.SECURITY_ATTRIBUTES secAttrs = null;
                secAttrs = new NativeMethods.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);
                byte[] sd = semaphoreSecurity.GetSecurityDescriptorBinaryForm(); 
                fixed(byte* pSecDescriptor = sd) {
                    secAttrs.lpSecurityDescriptor = new SafeLocalMemHandle((IntPtr) pSecDescriptor, false); 
                    myHandle = SafeNativeMethods.CreateSemaphore(secAttrs, initialCount, maximumCount, name); 
                }
            } 
            else {
#endif
                myHandle = SafeNativeMethods.CreateSemaphore(null, initialCount, maximumCount, name);
#if !FEATURE_PAL 
            }
#endif 
            int errorCode = Marshal.GetLastWin32Error(); 
            if (myHandle.IsInvalid)
            { 
                if(null != name && 0 != name.Length && NativeMethods.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(SR.GetString(SR.WaitHandleCannotBeOpenedException_InvalidHandle,name));
                InternalResources.WinIOError();
            } 
            createdNew = errorCode != NativeMethods.ERROR_ALREADY_EXISTS;
            this.SafeWaitHandle = myHandle; 
        } 

        private Semaphore(SafeWaitHandle handle) 
        {
            this.SafeWaitHandle = handle;
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)] 
        public static Semaphore OpenExisting(string name)
        { 
#if !FEATURE_PAL
            return OpenExisting(name, SemaphoreRights.Modify | SemaphoreRights.Synchronize);
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)] 
        public static Semaphore OpenExisting(string name, SemaphoreRights rights)
        { 
#endif // !FEATURE_PAL
            if (name == null)
            {
                throw new ArgumentNullException("name"); 
            }
            if(name.Length  == 0) 
            { 
                throw new ArgumentException(SR.GetString(SR.InvalidNullEmptyArgument, "name"), "name");
            } 
            if(null != name && MAX_PATH < name.Length)
            {
                throw new ArgumentException(SR.GetString(SR.Argument_WaitHandleNameTooLong));
            } 
            //Pass false to OpenSemaphore to prevent inheritedHandles
#if FEATURE_PAL 
            SafeWaitHandle myHandle = SafeNativeMethods.OpenSemaphore(Win32Native.SEMAPHORE_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name); 
#else
            SafeWaitHandle myHandle = SafeNativeMethods.OpenSemaphore((int) rights, false, name); 
#endif

            if (myHandle.IsInvalid)
            { 
                int errorCode = Marshal.GetLastWin32Error();
 
                if (NativeMethods.ERROR_FILE_NOT_FOUND == errorCode || NativeMethods.ERROR_INVALID_NAME == errorCode) 
                    throw new WaitHandleCannotBeOpenedException();
                if (null != name && 0 != name.Length && NativeMethods.ERROR_INVALID_HANDLE == errorCode) 
                    throw new WaitHandleCannotBeOpenedException(SR.GetString(SR.WaitHandleCannotBeOpenedException_InvalidHandle,name));
                //this is for passed through NativeMethods Errors
                InternalResources.WinIOError();
            } 
            return new Semaphore(myHandle);
        } 
 

        // increase the count on a semaphore, returns previous count 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [PrePrepareMethod]
        public int Release()
        { 
            return Release(1);
        } 
 
        // increase the count on a semaphore, returns previous count
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public int Release(int releaseCount)
        { 
            if (releaseCount < 1)
            { 
                throw new ArgumentOutOfRangeException("releaseCount", SR.GetString(SR.ArgumentOutOfRange_NeedNonNegNumRequired)); 
            }
            int previousCount; 

            //If ReleaseSempahore returns false when the specified value would cause
            //   the semaphore's count to exceed the maximum count set when Semaphore was created
            //Non-Zero return 

            if (!SafeNativeMethods.ReleaseSemaphore(SafeWaitHandle, releaseCount, out previousCount)) 
            { 
                throw new SemaphoreFullException();
            } 

            return previousCount;
        }
 
#if !FEATURE_PAL
        public SemaphoreSecurity GetAccessControl() { 
            return new SemaphoreSecurity(SafeWaitHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group); 
        }
 
        public void SetAccessControl(SemaphoreSecurity semaphoreSecurity) {
            if (semaphoreSecurity == null)
                throw new ArgumentNullException("semaphoreSecurity");
 
            semaphoreSecurity.Persist(SafeWaitHandle);
        } 
#endif 
    }
} 


// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

namespace System.Threading 
{ 
    using System.IO;
    using System.IO.Ports; 
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.InteropServices;
    using System.Threading; 
    using System.Security.Permissions;
#if !FEATURE_PAL 
    using System.Security.AccessControl; 
#endif
    using System.Runtime.Versioning; 
    using System.Runtime.ConstrainedExecution;


    [HostProtection(Synchronization=true, ExternalThreading=true)] 
    [ComVisibleAttribute(false)]
    public sealed class Semaphore: WaitHandle 
    { 
        private static int MAX_PATH = 260;
        // creates a nameless semaphore object 
        // Win32 only takes maximum count of Int32.MaxValue
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public Semaphore(int initialCount, int maximumCount) : this(initialCount,maximumCount,null){} 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)] 
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public Semaphore(int initialCount, int maximumCount, string name) 
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount", SR.GetString(SR.ArgumentOutOfRange_NeedNonNegNumRequired)); 
            }
 
            if (maximumCount < 1) 
            {
                throw new ArgumentOutOfRangeException("maximumCount", SR.GetString(SR.ArgumentOutOfRange_NeedNonNegNumRequired)); 
            }

            if (initialCount > maximumCount)
            { 
                throw new ArgumentException(SR.GetString(SR.Argument_SemaphoreInitialMaximum));
            } 
 
            if(null != name && MAX_PATH < name.Length)
            { 
                throw new ArgumentException(SR.GetString(SR.Argument_WaitHandleNameTooLong));
            }
            SafeWaitHandle   myHandle = SafeNativeMethods.CreateSemaphore(null, initialCount, maximumCount, name);
 
            if (myHandle.IsInvalid)
            { 
                int errorCode = Marshal.GetLastWin32Error(); 

                if(null != name && 0 != name.Length && NativeMethods.ERROR_INVALID_HANDLE == errorCode) 
                    throw new WaitHandleCannotBeOpenedException(SR.GetString(SR.WaitHandleCannotBeOpenedException_InvalidHandle,name));

                InternalResources.WinIOError();
            } 
            this.SafeWaitHandle = myHandle;
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public Semaphore(int initialCount, int maximumCount, string name, out bool createdNew)
#if !FEATURE_PAL
            : this(initialCount, maximumCount, name, out createdNew, null) 
        {
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public unsafe Semaphore(int initialCount, int maximumCount, string name, out bool createdNew, SemaphoreSecurity semaphoreSecurity)
#endif
        { 
            if (initialCount < 0)
            { 
                throw new ArgumentOutOfRangeException("initialCount", SR.GetString(SR.ArgumentOutOfRange_NeedNonNegNumRequired)); 
            }
 
            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException("maximumCount", SR.GetString(SR.ArgumentOutOfRange_NeedNonNegNumRequired));
            } 

            if (initialCount > maximumCount) 
            { 
                throw new ArgumentException(SR.GetString(SR.Argument_SemaphoreInitialMaximum));
            } 

            if(null != name && MAX_PATH < name.Length)
            {
                throw new ArgumentException(SR.GetString(SR.Argument_WaitHandleNameTooLong)); 
            }
            SafeWaitHandle   myHandle; 
#if !FEATURE_PAL 
            // For ACL's, get the security descriptor from the SemaphoreSecurity.
            if (semaphoreSecurity != null) { 
                NativeMethods.SECURITY_ATTRIBUTES secAttrs = null;
                secAttrs = new NativeMethods.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);
                byte[] sd = semaphoreSecurity.GetSecurityDescriptorBinaryForm(); 
                fixed(byte* pSecDescriptor = sd) {
                    secAttrs.lpSecurityDescriptor = new SafeLocalMemHandle((IntPtr) pSecDescriptor, false); 
                    myHandle = SafeNativeMethods.CreateSemaphore(secAttrs, initialCount, maximumCount, name); 
                }
            } 
            else {
#endif
                myHandle = SafeNativeMethods.CreateSemaphore(null, initialCount, maximumCount, name);
#if !FEATURE_PAL 
            }
#endif 
            int errorCode = Marshal.GetLastWin32Error(); 
            if (myHandle.IsInvalid)
            { 
                if(null != name && 0 != name.Length && NativeMethods.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(SR.GetString(SR.WaitHandleCannotBeOpenedException_InvalidHandle,name));
                InternalResources.WinIOError();
            } 
            createdNew = errorCode != NativeMethods.ERROR_ALREADY_EXISTS;
            this.SafeWaitHandle = myHandle; 
        } 

        private Semaphore(SafeWaitHandle handle) 
        {
            this.SafeWaitHandle = handle;
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)] 
        public static Semaphore OpenExisting(string name)
        { 
#if !FEATURE_PAL
            return OpenExisting(name, SemaphoreRights.Modify | SemaphoreRights.Synchronize);
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)] 
        public static Semaphore OpenExisting(string name, SemaphoreRights rights)
        { 
#endif // !FEATURE_PAL
            if (name == null)
            {
                throw new ArgumentNullException("name"); 
            }
            if(name.Length  == 0) 
            { 
                throw new ArgumentException(SR.GetString(SR.InvalidNullEmptyArgument, "name"), "name");
            } 
            if(null != name && MAX_PATH < name.Length)
            {
                throw new ArgumentException(SR.GetString(SR.Argument_WaitHandleNameTooLong));
            } 
            //Pass false to OpenSemaphore to prevent inheritedHandles
#if FEATURE_PAL 
            SafeWaitHandle myHandle = SafeNativeMethods.OpenSemaphore(Win32Native.SEMAPHORE_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name); 
#else
            SafeWaitHandle myHandle = SafeNativeMethods.OpenSemaphore((int) rights, false, name); 
#endif

            if (myHandle.IsInvalid)
            { 
                int errorCode = Marshal.GetLastWin32Error();
 
                if (NativeMethods.ERROR_FILE_NOT_FOUND == errorCode || NativeMethods.ERROR_INVALID_NAME == errorCode) 
                    throw new WaitHandleCannotBeOpenedException();
                if (null != name && 0 != name.Length && NativeMethods.ERROR_INVALID_HANDLE == errorCode) 
                    throw new WaitHandleCannotBeOpenedException(SR.GetString(SR.WaitHandleCannotBeOpenedException_InvalidHandle,name));
                //this is for passed through NativeMethods Errors
                InternalResources.WinIOError();
            } 
            return new Semaphore(myHandle);
        } 
 

        // increase the count on a semaphore, returns previous count 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [PrePrepareMethod]
        public int Release()
        { 
            return Release(1);
        } 
 
        // increase the count on a semaphore, returns previous count
        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public int Release(int releaseCount)
        { 
            if (releaseCount < 1)
            { 
                throw new ArgumentOutOfRangeException("releaseCount", SR.GetString(SR.ArgumentOutOfRange_NeedNonNegNumRequired)); 
            }
            int previousCount; 

            //If ReleaseSempahore returns false when the specified value would cause
            //   the semaphore's count to exceed the maximum count set when Semaphore was created
            //Non-Zero return 

            if (!SafeNativeMethods.ReleaseSemaphore(SafeWaitHandle, releaseCount, out previousCount)) 
            { 
                throw new SemaphoreFullException();
            } 

            return previousCount;
        }
 
#if !FEATURE_PAL
        public SemaphoreSecurity GetAccessControl() { 
            return new SemaphoreSecurity(SafeWaitHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group); 
        }
 
        public void SetAccessControl(SemaphoreSecurity semaphoreSecurity) {
            if (semaphoreSecurity == null)
                throw new ArgumentNullException("semaphoreSecurity");
 
            semaphoreSecurity.Persist(SafeWaitHandle);
        } 
#endif 
    }
} 


