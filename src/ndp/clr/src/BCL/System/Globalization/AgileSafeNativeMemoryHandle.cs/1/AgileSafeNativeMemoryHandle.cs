// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  AgileSafeNativeMemoryHandle 
**
** This class is to hold native memory handle and make sure it 
** gets released when the object get collected by GC.
**
** This class can hold one of two types of memory handle
**      first it can hold a handle for memory created using 
**      Marshal.AllocHGlobal. this can happen when creating
**      the object using constructor taking IntPtr as handle. 
**      in this case mode = false. 
**
**      second it can hold handle for memory created from mapped 
**      file. this can happen when creating the object using the
**      file name then the constructor will open and map this file
**      and hold the mapped memory section handle
**      in this case mode = true. 
**
** IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT 
**      this class is used only with CultureInfo or its 
**      field classes.
**      this class is special case that is agile and has finalizer 
**      and it is a special case like Thread class.
**      this class is agile to make sure it survive the app domain
**      unloading. otherwise we can get AV when culture info cross
**      the app domain boundary. 
**      so don't use it in any other purpose.
** 
===========================================================*/ 
using System;
using System.IO; 
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices; 
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32; 
using Microsoft.Win32.SafeHandles; 
using System.Runtime.Versioning;
 
namespace System.Globalization
{
    internal sealed class AgileSafeNativeMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    { 
        private const int PAGE_READONLY       = 0x02;
        private const int SECTION_MAP_READ    = 0x0004; 
 
        private unsafe byte* bytes;
 
        //
        // The only handle we keep it open is the mapped memory section and we close
        // both the stream and mapped file handle as the OS keep the memory section
        // mapped even when closing the files. 
        // The benefit for closing the file and stream handle is have flexability
        // to rename the file while it is in use. 
        // 
        private long                 fileSize  = 0;
 
        // mode is true if the memory created from mapped file.
        // and false if the memory created from Marshal.AllocHGlobal.
        private bool                 mode      = false;
 

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)] 
        internal AgileSafeNativeMemoryHandle() : base(true) {} 

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)] 
        internal AgileSafeNativeMemoryHandle(IntPtr handle, bool ownsHandle) : base (ownsHandle)
        {
            SetHandle(handle);
        } 

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)] 
        internal unsafe AgileSafeNativeMemoryHandle(String fileName) : this(fileName, null) {}
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal unsafe AgileSafeNativeMemoryHandle(String fileName, String fileMappingName) : base(true)
        { 
            mode = true;
            // 
            // Use native API to create the file directly. 
            //
            SafeFileHandle fileHandle = Win32Native.UnsafeCreateFile(fileName, FileStream.GENERIC_READ, FileShare.Read, null, FileMode.Open, 0, IntPtr.Zero); 
            int lastError = Marshal.GetLastWin32Error();
            if (fileHandle.IsInvalid)
            {
                BCLDebug.Assert(false, "Failed to create file " + fileName + ", GetLastError = " + lastError); 
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_UnexpectedWin32Error"), lastError));
            } 
 
            int highSize;
            int lowSize = Win32Native.GetFileSize(fileHandle, out highSize); 
            if (lowSize == Win32Native.INVALID_FILE_SIZE)
            {
                fileHandle.Close();
                BCLDebug.Assert(false, "Failed to get the file size of " + fileName + ", GetLastError = " + lastError); 
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_UnexpectedWin32Error"), lastError));
            } 
 
            fileSize = (((long) highSize) << 32) | ((uint) lowSize);
 
            if (fileSize == 0)
            {
                // we cannot map zero size file. the caller should check for the file size.
                fileHandle.Close(); 
                return;
            } 
 
            SafeFileMappingHandle fileMapHandle = Win32Native.CreateFileMapping(fileHandle, IntPtr.Zero, PAGE_READONLY, 0, 0, fileMappingName);
            lastError = Marshal.GetLastWin32Error(); 
            fileHandle.Close();
            if (fileMapHandle.IsInvalid)
            {
                BCLDebug.Assert(false, "Failed to create file mapping for file " + fileName + ", GetLastError = " + lastError); 
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_UnexpectedWin32Error"), lastError));
            } 
 
            // Use a CER to ensure that we store the handle within this SafeHandle without allowing async exceptions.
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try
            {
            }
            finally 
            {
                handle = Win32Native.MapViewOfFile(fileMapHandle, SECTION_MAP_READ, 0, 0, UIntPtr.Zero); 
            } 

            lastError = Marshal.GetLastWin32Error(); 
            if (handle == IntPtr.Zero)
            {
                fileMapHandle.Close();
                BCLDebug.Assert(false, "Failed to map a view of file " + fileName + ", GetLastError = " + lastError); 
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_UnexpectedWin32Error"), lastError));
            } 
 
            bytes = (byte*) this.DangerousGetHandle();
 
            fileMapHandle.Close();
        }

        internal long FileSize 
        {
            get 
            { 
                BCLDebug.Assert(mode == true, "The memory is not created from mapped file to request the size");
                return fileSize; 
            }
        }

        internal unsafe byte* GetBytePtr() 
        {
            BCLDebug.Assert(bytes != null && mode == true, "bytes can requested only if the memory created from mapped file"); 
            return (bytes); 
        }
 
        override protected bool ReleaseHandle()
        {
            if (!IsInvalid)
            { 
                if (mode == true)
                { 
                    if (Win32Native.UnmapViewOfFile(handle)) 
                    {
                        handle = IntPtr.Zero; 
                        return true;
                    }
                }
                else 
                {
                    Marshal.FreeHGlobal(handle); 
                    handle = IntPtr.Zero; 
                    return true;
                } 
            }
            return false;
        }
    } 
}
 
 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  AgileSafeNativeMemoryHandle 
**
** This class is to hold native memory handle and make sure it 
** gets released when the object get collected by GC.
**
** This class can hold one of two types of memory handle
**      first it can hold a handle for memory created using 
**      Marshal.AllocHGlobal. this can happen when creating
**      the object using constructor taking IntPtr as handle. 
**      in this case mode = false. 
**
**      second it can hold handle for memory created from mapped 
**      file. this can happen when creating the object using the
**      file name then the constructor will open and map this file
**      and hold the mapped memory section handle
**      in this case mode = true. 
**
** IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT 
**      this class is used only with CultureInfo or its 
**      field classes.
**      this class is special case that is agile and has finalizer 
**      and it is a special case like Thread class.
**      this class is agile to make sure it survive the app domain
**      unloading. otherwise we can get AV when culture info cross
**      the app domain boundary. 
**      so don't use it in any other purpose.
** 
===========================================================*/ 
using System;
using System.IO; 
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices; 
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32; 
using Microsoft.Win32.SafeHandles; 
using System.Runtime.Versioning;
 
namespace System.Globalization
{
    internal sealed class AgileSafeNativeMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    { 
        private const int PAGE_READONLY       = 0x02;
        private const int SECTION_MAP_READ    = 0x0004; 
 
        private unsafe byte* bytes;
 
        //
        // The only handle we keep it open is the mapped memory section and we close
        // both the stream and mapped file handle as the OS keep the memory section
        // mapped even when closing the files. 
        // The benefit for closing the file and stream handle is have flexability
        // to rename the file while it is in use. 
        // 
        private long                 fileSize  = 0;
 
        // mode is true if the memory created from mapped file.
        // and false if the memory created from Marshal.AllocHGlobal.
        private bool                 mode      = false;
 

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)] 
        internal AgileSafeNativeMemoryHandle() : base(true) {} 

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)] 
        internal AgileSafeNativeMemoryHandle(IntPtr handle, bool ownsHandle) : base (ownsHandle)
        {
            SetHandle(handle);
        } 

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)] 
        internal unsafe AgileSafeNativeMemoryHandle(String fileName) : this(fileName, null) {}
 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal unsafe AgileSafeNativeMemoryHandle(String fileName, String fileMappingName) : base(true)
        { 
            mode = true;
            // 
            // Use native API to create the file directly. 
            //
            SafeFileHandle fileHandle = Win32Native.UnsafeCreateFile(fileName, FileStream.GENERIC_READ, FileShare.Read, null, FileMode.Open, 0, IntPtr.Zero); 
            int lastError = Marshal.GetLastWin32Error();
            if (fileHandle.IsInvalid)
            {
                BCLDebug.Assert(false, "Failed to create file " + fileName + ", GetLastError = " + lastError); 
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_UnexpectedWin32Error"), lastError));
            } 
 
            int highSize;
            int lowSize = Win32Native.GetFileSize(fileHandle, out highSize); 
            if (lowSize == Win32Native.INVALID_FILE_SIZE)
            {
                fileHandle.Close();
                BCLDebug.Assert(false, "Failed to get the file size of " + fileName + ", GetLastError = " + lastError); 
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_UnexpectedWin32Error"), lastError));
            } 
 
            fileSize = (((long) highSize) << 32) | ((uint) lowSize);
 
            if (fileSize == 0)
            {
                // we cannot map zero size file. the caller should check for the file size.
                fileHandle.Close(); 
                return;
            } 
 
            SafeFileMappingHandle fileMapHandle = Win32Native.CreateFileMapping(fileHandle, IntPtr.Zero, PAGE_READONLY, 0, 0, fileMappingName);
            lastError = Marshal.GetLastWin32Error(); 
            fileHandle.Close();
            if (fileMapHandle.IsInvalid)
            {
                BCLDebug.Assert(false, "Failed to create file mapping for file " + fileName + ", GetLastError = " + lastError); 
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_UnexpectedWin32Error"), lastError));
            } 
 
            // Use a CER to ensure that we store the handle within this SafeHandle without allowing async exceptions.
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try
            {
            }
            finally 
            {
                handle = Win32Native.MapViewOfFile(fileMapHandle, SECTION_MAP_READ, 0, 0, UIntPtr.Zero); 
            } 

            lastError = Marshal.GetLastWin32Error(); 
            if (handle == IntPtr.Zero)
            {
                fileMapHandle.Close();
                BCLDebug.Assert(false, "Failed to map a view of file " + fileName + ", GetLastError = " + lastError); 
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_UnexpectedWin32Error"), lastError));
            } 
 
            bytes = (byte*) this.DangerousGetHandle();
 
            fileMapHandle.Close();
        }

        internal long FileSize 
        {
            get 
            { 
                BCLDebug.Assert(mode == true, "The memory is not created from mapped file to request the size");
                return fileSize; 
            }
        }

        internal unsafe byte* GetBytePtr() 
        {
            BCLDebug.Assert(bytes != null && mode == true, "bytes can requested only if the memory created from mapped file"); 
            return (bytes); 
        }
 
        override protected bool ReleaseHandle()
        {
            if (!IsInvalid)
            { 
                if (mode == true)
                { 
                    if (Win32Native.UnmapViewOfFile(handle)) 
                    {
                        handle = IntPtr.Zero; 
                        return true;
                    }
                }
                else 
                {
                    Marshal.FreeHGlobal(handle); 
                    handle = IntPtr.Zero; 
                    return true;
                } 
            }
            return false;
        }
    } 
}
 
 
