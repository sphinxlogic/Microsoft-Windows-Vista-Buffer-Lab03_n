// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  Microsoft.Win32.Win32Native 
**
** 
** Purpose: The CLR wrapper for all Win32 (Win2000, NT4, Win9x,
**          as well as ROTOR-style Unix PAL, etc.) native operations
**
** 
===========================================================*/
/** 
 * Notes to PInvoke users:  Getting the syntax exactly correct is crucial, and 
 * more than a little confusing.  Here's some guidelines.
 * 
 * For handles, you should use a SafeHandle subclass specific to your handle
 * type.  For files, we have the following set of interesting definitions:
 *
 *  [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
 *  private static extern SafeFileHandle CreateFile(...);
 * 
 *  [DllImport(KERNEL32, SetLastError=true)] 
 *  unsafe internal static extern int ReadFile(SafeFileHandle handle, ...);
 * 
 *  [DllImport(KERNEL32, SetLastError=true)]
 *  internal static extern bool CloseHandle(IntPtr handle);
 *
 * P/Invoke will create the SafeFileHandle instance for you and assign the 
 * return value from CreateFile into the handle atomically.  When we call
 * ReadFile, P/Invoke will increment a ref count, make the call, then decrement 
 * it (preventing handle recycling vulnerabilities).  Then SafeFileHandle's 
 * ReleaseHandle method will call CloseHandle, passing in the handle field
 * as an IntPtr. 
 *
 * If for some reason you cannot use a SafeHandle subclass for your handles,
 * then use IntPtr as the handle type (or possibly HandleRef - understand when
 * to use GC.KeepAlive).  If your code will run in SQL Server (or any other 
 * long-running process that can't be recycled easily), use a constrained
 * execution region to prevent thread aborts while allocating your 
 * handle, and consider making your handle wrapper subclass 
 * CriticalFinalizerObject to ensure you can free the handle.  As you can
 * probably guess, SafeHandle  will save you a lot of headaches if your code 
 * needs to be robust to thread aborts and OOM.
 *
 *
 * If you have a method that takes a native struct, you have two options for 
 * declaring that struct.  You can make it a value type ('struct' in CSharp),
 * or a reference type ('class').  This choice doesn't seem very interesting, 
 * but your function prototype must use different syntax depending on your 
 * choice.  For example, if your native method is prototyped as such:
 * 
 *    bool GetVersionEx(OSVERSIONINFO & lposvi);
 *
 *
 * you must use EITHER THIS OR THE NEXT syntax: 
 *
 *    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)] 
 *    internal struct OSVERSIONINFO {  ...  } 
 *
 *    [DllImport(KERNEL32, CharSet=CharSet.Auto)] 
 *    internal static extern bool GetVersionEx(ref OSVERSIONINFO lposvi);
 *
 * OR:
 * 
 *    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
 *    internal class OSVERSIONINFO {  ...  } 
 * 
 *    [DllImport(KERNEL32, CharSet=CharSet.Auto)]
 *    internal static extern bool GetVersionEx([In, Out] OSVERSIONINFO lposvi); 
 *
 * Note that classes require being marked as [In, Out] while value types must
 * be passed as ref parameters.
 * 
 * Also note the CharSet.Auto on GetVersionEx - while it does not take a String
 * as a parameter, the OSVERSIONINFO contains an embedded array of TCHARs, so 
 * the size of the struct varies on different platforms, and there's a 
 * GetVersionExA & a GetVersionExW.  Also, the OSVERSIONINFO struct has a sizeof
 * field so the OS can ensure you've passed in the correctly-sized copy of an 
 * OSVERSIONINFO.  You must explicitly set this using Marshal.SizeOf(Object);
 *
 * For security reasons, if you're making a P/Invoke method to a Win32 method
 * that takes an ANSI String (or will be ANSI on Win9x) and that String is the 
 * name of some resource you've done a security check on (such as a file name),
 * you want to disable best fit mapping in WideCharToMultiByte.  Do this by 
 * setting BestFitMapping=false in your DllImportAttribute. 
 */
 
namespace Microsoft.Win32 {
    using System;
    using System.Security;
    using System.Security.Principal; 
    using System.Text;
    using System.Configuration.Assemblies; 
    using System.Runtime.Remoting; 
    using System.Runtime.InteropServices;
    using System.Threading; 
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
 
    using BOOL = System.Int32;
    using DWORD = System.UInt32; 
    using ULONG = System.UInt32; 

    /** 
     * Win32 encapsulation for MSCORLIB.
     */
    // Remove the default demands for all N/Direct methods with this
    // global declaration on the class. 
    //
    [SuppressUnmanagedCodeSecurityAttribute()] 
    internal static class Win32Native { 

#if !FEATURE_PAL 
        internal const int KEY_QUERY_VALUE        = 0x0001;
        internal const int KEY_SET_VALUE          = 0x0002;
        internal const int KEY_CREATE_SUB_KEY     = 0x0004;
        internal const int KEY_ENUMERATE_SUB_KEYS = 0x0008; 
        internal const int KEY_NOTIFY             = 0x0010;
        internal const int KEY_CREATE_LINK        = 0x0020; 
        internal const int KEY_READ               =((STANDARD_RIGHTS_READ       | 
                                                           KEY_QUERY_VALUE            |
                                                           KEY_ENUMERATE_SUB_KEYS     | 
                                                           KEY_NOTIFY)
                                                          &
                                                          (~SYNCHRONIZE));
 
        internal const int KEY_WRITE              =((STANDARD_RIGHTS_WRITE      |
                                                           KEY_SET_VALUE              | 
                                                           KEY_CREATE_SUB_KEY) 
                                                          &
                                                          (~SYNCHRONIZE)); 
        internal const int REG_NONE                    = 0;     // No value type
        internal const int REG_SZ                      = 1;     // Unicode nul terminated string
        internal const int REG_EXPAND_SZ               = 2;     // Unicode nul terminated string
        // (with environment variable references) 
        internal const int REG_BINARY                  = 3;     // Free form binary
        internal const int REG_DWORD                   = 4;     // 32-bit number 
        internal const int REG_DWORD_LITTLE_ENDIAN     = 4;     // 32-bit number (same as REG_DWORD) 
        internal const int REG_DWORD_BIG_ENDIAN        = 5;     // 32-bit number
        internal const int REG_LINK                    = 6;     // Symbolic Link (unicode) 
        internal const int REG_MULTI_SZ                = 7;     // Multiple Unicode strings
        internal const int REG_RESOURCE_LIST           = 8;     // Resource list in the resource map
        internal const int REG_FULL_RESOURCE_DESCRIPTOR  = 9;   // Resource list in the hardware description
        internal const int REG_RESOURCE_REQUIREMENTS_LIST = 10; 
        internal const int REG_QWORD                   = 11;    // 64-bit number
 
        internal const int HWND_BROADCAST              = 0xffff; 
        internal const int WM_SETTINGCHANGE            = 0x001A;
 
        // CryptProtectMemory and CryptUnprotectMemory.
        internal const uint CRYPTPROTECTMEMORY_BLOCK_SIZE    = 16;
        internal const uint CRYPTPROTECTMEMORY_SAME_PROCESS  = 0x00;
        internal const uint CRYPTPROTECTMEMORY_CROSS_PROCESS = 0x01; 
        internal const uint CRYPTPROTECTMEMORY_SAME_LOGON    = 0x02;
 
        // Security Quality of Service flags 
        internal const int SECURITY_ANONYMOUS       = ((int)SECURITY_IMPERSONATION_LEVEL.Anonymous << 16);
        internal const int SECURITY_SQOS_PRESENT    = 0x00100000; 

        // Access Control library.
        internal const string MICROSOFT_KERBEROS_NAME = "Kerberos";
        internal const uint ANONYMOUS_LOGON_LUID = 0x3e6; 

        internal const int SECURITY_ANONYMOUS_LOGON_RID    = 0x00000007; 
        internal const int SECURITY_AUTHENTICATED_USER_RID = 0x0000000B; 
        internal const int SECURITY_LOCAL_SYSTEM_RID       = 0x00000012;
        internal const int SECURITY_BUILTIN_DOMAIN_RID     = 0x00000020; 
        internal const int DOMAIN_USER_RID_GUEST           = 0x000001F5;

        internal const uint SE_PRIVILEGE_DISABLED           = 0x00000000;
        internal const uint SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001; 
        internal const uint SE_PRIVILEGE_ENABLED            = 0x00000002;
        internal const uint SE_PRIVILEGE_USED_FOR_ACCESS    = 0x80000000; 
 
        internal const uint SE_GROUP_MANDATORY          = 0x00000001;
        internal const uint SE_GROUP_ENABLED_BY_DEFAULT = 0x00000002; 
        internal const uint SE_GROUP_ENABLED            = 0x00000004;
        internal const uint SE_GROUP_OWNER              = 0x00000008;
        internal const uint SE_GROUP_USE_FOR_DENY_ONLY  = 0x00000010;
        internal const uint SE_GROUP_LOGON_ID           = 0xC0000000; 
        internal const uint SE_GROUP_RESOURCE           = 0x20000000;
 
        internal const uint DUPLICATE_CLOSE_SOURCE      = 0x00000001; 
        internal const uint DUPLICATE_SAME_ACCESS       = 0x00000002;
        internal const uint DUPLICATE_SAME_ATTRIBUTES   = 0x00000004; 
#endif

        // Win32 ACL-related constants:
        internal const int READ_CONTROL                    = 0x00020000; 
        internal const int SYNCHRONIZE                     = 0x00100000;
 
        internal const int STANDARD_RIGHTS_READ            = READ_CONTROL; 
        internal const int STANDARD_RIGHTS_WRITE           = READ_CONTROL;
 
        // STANDARD_RIGHTS_REQUIRED  (0x000F0000L)
        // SEMAPHORE_ALL_ACCESS          (STANDARD_RIGHTS_REQUIRED|SYNCHRONIZE|0x3)

        // SEMAPHORE and Event both use 0x0002 
        // MUTEX uses 0x001 (MUTANT_QUERY_STATE)
 
        // Note that you may need to specify the SYNCHRONIZE bit as well 
        // to be able to open a synchronization primitive.
        internal const int SEMAPHORE_MODIFY_STATE = 0x00000002; 
        internal const int EVENT_MODIFY_STATE     = 0x00000002;
        internal const int MUTEX_MODIFY_STATE     = 0x00000001;
        internal const int MUTEX_ALL_ACCESS       = 0x001F0001;
 

        internal const int LMEM_FIXED    = 0x0000; 
        internal const int LMEM_ZEROINIT = 0x0040; 
        internal const int LPTR          = (LMEM_FIXED | LMEM_ZEROINIT);
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        internal class OSVERSIONINFO {
            internal OSVERSIONINFO() {
                OSVersionInfoSize = (int)Marshal.SizeOf(this); 
            }
 
            // The OSVersionInfoSize field must be set to Marshal.SizeOf(this) 
            internal int OSVersionInfoSize = 0;
            internal int MajorVersion = 0; 
            internal int MinorVersion = 0;
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] 
            internal String CSDVersion = null;
        } 
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        internal class OSVERSIONINFOEX { 

            public OSVERSIONINFOEX() {
                OSVersionInfoSize = (int)Marshal.SizeOf(this);
            } 

            // The OSVersionInfoSize field must be set to Marshal.SizeOf(this) 
            internal int OSVersionInfoSize = 0; 
            internal int MajorVersion = 0;
            internal int MinorVersion = 0; 
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)]
            internal string CSDVersion = null; 
            internal ushort ServicePackMajor = 0;
            internal ushort ServicePackMinor = 0; 
            internal short SuiteMask = 0; 
            internal byte ProductType = 0;
            internal byte Reserved = 0; 
        }

        [StructLayout(LayoutKind.Sequential)]
            internal struct SYSTEM_INFO { 
            internal int dwOemId;    // This is a union of a DWORD and a struct containing 2 WORDs.
            internal int dwPageSize; 
            internal IntPtr lpMinimumApplicationAddress; 
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask; 
            internal int dwNumberOfProcessors;
            internal int dwProcessorType;
            internal int dwAllocationGranularity;
            internal short wProcessorLevel; 
            internal short wProcessorRevision;
        } 
 
        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES { 
            internal int nLength = 0;
            internal unsafe byte * pSecurityDescriptor = null;
            internal int bInheritHandle = 0;
        } 

        [StructLayout(LayoutKind.Sequential), Serializable] 
        internal struct WIN32_FILE_ATTRIBUTE_DATA { 
            internal int fileAttributes;
            internal uint ftCreationTimeLow; 
            internal uint ftCreationTimeHigh;
            internal uint ftLastAccessTimeLow;
            internal uint ftLastAccessTimeHigh;
            internal uint ftLastWriteTimeLow; 
            internal uint ftLastWriteTimeHigh;
            internal int fileSizeHigh; 
            internal int fileSizeLow; 
        }
 
        [StructLayout(LayoutKind.Sequential)]
        internal struct FILE_TIME {
            public FILE_TIME(long fileTime) {
                ftTimeLow = (uint) fileTime; 
                ftTimeHigh = (uint) (fileTime >> 32);
            } 
 
            public long ToTicks() {
                return ((long) ftTimeHigh << 32) + ftTimeLow; 
            }

            internal uint ftTimeLow;
            internal uint ftTimeHigh; 
        }
 
#if !FEATURE_PAL 

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct KERB_S4U_LOGON {
            internal uint                   MessageType;
            internal uint                   Flags;
            internal UNICODE_INTPTR_STRING  ClientUpn;   // REQUIRED: UPN for client 
            internal UNICODE_INTPTR_STRING  ClientRealm; // Optional: Client Realm, if known
        } 
 
        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct LSA_OBJECT_ATTRIBUTES { 
            internal int Length;
            internal IntPtr RootDirectory;
            internal IntPtr ObjectName;
            internal int Attributes; 
            internal IntPtr SecurityDescriptor;
            internal IntPtr SecurityQualityOfService; 
        } 

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct UNICODE_STRING {
            internal ushort Length;
            internal ushort MaximumLength;
            [MarshalAs(UnmanagedType.LPWStr)] internal string Buffer; 
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct UNICODE_INTPTR_STRING {
            internal UNICODE_INTPTR_STRING (int length, int maximumLength, IntPtr buffer) { 
                this.Length = (ushort) length;
                this.MaxLength = (ushort) maximumLength;
                this.Buffer = buffer;
            } 
            internal ushort Length;
            internal ushort MaxLength; 
            internal IntPtr Buffer; 
        }
 
        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_TRANSLATED_NAME {
            internal int Use;
            internal UNICODE_INTPTR_STRING Name; 
            internal int DomainIndex;
        } 
 
        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct LSA_TRANSLATED_SID { 
            internal int Use;
            internal uint Rid;
            internal int DomainIndex;
        } 

        [StructLayoutAttribute(LayoutKind.Sequential)] 
        internal struct LSA_TRANSLATED_SID2 { 
            internal int Use;
            internal IntPtr Sid; 
            internal int DomainIndex;
            uint Flags;
        }
 
        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_TRUST_INFORMATION { 
            internal UNICODE_INTPTR_STRING Name; 
            internal IntPtr Sid;
        } 

        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_REFERENCED_DOMAIN_LIST {
            internal int Entries; 
            internal IntPtr Domains;
        } 
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct LUID { 
            internal uint LowPart;
            internal uint HighPart;
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct LUID_AND_ATTRIBUTES { 
            internal LUID Luid; 
            internal uint Attributes;
        } 

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct QUOTA_LIMITS {
            internal IntPtr PagedPoolLimit; 
            internal IntPtr NonPagedPoolLimit;
            internal IntPtr MinimumWorkingSetSize; 
            internal IntPtr MaximumWorkingSetSize; 
            internal IntPtr PagefileLimit;
            internal IntPtr TimeLimit; 
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct SECURITY_LOGON_SESSION_DATA { 
            internal uint       Size;
            internal LUID       LogonId; 
            internal UNICODE_INTPTR_STRING UserName; 
            internal UNICODE_INTPTR_STRING LogonDomain;
            internal UNICODE_INTPTR_STRING AuthenticationPackage; 
            internal uint       LogonType;
            internal uint       Session;
            internal IntPtr     Sid;
            internal long       LogonTime; 
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct SID_AND_ATTRIBUTES {
            internal IntPtr Sid; 
            internal uint   Attributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct TOKEN_GROUPS {
            internal uint GroupCount; 
            internal SID_AND_ATTRIBUTES Groups; // SID_AND_ATTRIBUTES Groups[ANYSIZE_ARRAY]; 
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct TOKEN_PRIVILEGE {
            internal uint                PrivilegeCount;
            internal LUID_AND_ATTRIBUTES Privilege; 
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct TOKEN_SOURCE {
            private const int TOKEN_SOURCE_LENGTH = 8; 

            [MarshalAs(UnmanagedType.ByValArray, SizeConst=TOKEN_SOURCE_LENGTH)]
            internal char[] Name;
            internal LUID   SourceIdentifier; 
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct TOKEN_STATISTICS {
            internal LUID   TokenId; 
            internal LUID   AuthenticationId;
            internal long   ExpirationTime;
            internal uint   TokenType;
            internal uint   ImpersonationLevel; 
            internal uint   DynamicCharged;
            internal uint   DynamicAvailable; 
            internal uint   GroupCount; 
            internal uint   PrivilegeCount;
            internal LUID   ModifiedId; 
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct TOKEN_USER { 
            internal SID_AND_ATTRIBUTES User;
        } 
 
        [StructLayout(LayoutKind.Sequential)]
        internal class MEMORYSTATUSEX { 
            internal MEMORYSTATUSEX() {
                length = (int) Marshal.SizeOf(this);
            }
 
            // The length field must be set to the size of this data structure.
            internal int length; 
            internal int memoryLoad; 
            internal ulong totalPhys;
            internal ulong availPhys; 
            internal ulong totalPageFile;
            internal ulong availPageFile;
            internal ulong totalVirtual;
            internal ulong availVirtual; 
            internal ulong availExtendedVirtual;
        } 
 
        // Use only on Win9x
        [StructLayout(LayoutKind.Sequential)] 
        internal class MEMORYSTATUS {
            internal MEMORYSTATUS() {
                length = (int) Marshal.SizeOf(this);
            } 

            // The length field must be set to the size of this data structure. 
            internal int length; 
            internal int memoryLoad;
            internal uint totalPhys; 
            internal uint availPhys;
            internal uint totalPageFile;
            internal uint availPageFile;
            internal uint totalVirtual; 
            internal uint availVirtual;
        } 
 
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct MEMORY_BASIC_INFORMATION { 
            internal void* BaseAddress;
            internal void* AllocationBase;
            internal uint AllocationProtect;
            internal UIntPtr RegionSize; 
            internal uint State;
            internal uint Protect; 
            internal uint Type; 
        }
#endif  // !FEATURE_PAL 

#if !FEATURE_PAL
        internal const String KERNEL32 = "kernel32.dll";
        internal const String USER32   = "user32.dll"; 
        internal const String ADVAPI32 = "advapi32.dll";
        internal const String OLE32    = "ole32.dll"; 
        internal const String OLEAUT32 = "oleaut32.dll"; 
        internal const String SHFOLDER = "shfolder.dll";
        internal const String SHIM     = "mscoree.dll"; 
        internal const String CRYPT32  = "crypt32.dll";
        internal const String SECUR32  = "secur32.dll";
        internal const String MSCORWKS = "mscorwks.dll";
 
#else // !FEATURE_PAL
 
 #if !PLATFORM_UNIX 
        internal const String DLLPREFIX = "";
        internal const String DLLSUFFIX = ".dll"; 
 #else // !PLATFORM_UNIX
  #if __APPLE__
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".dylib"; 
  #elif _AIX
        internal const String DLLPREFIX = "lib"; 
        internal const String DLLSUFFIX = ".a"; 
  #elif __hppa__ || IA64
        internal const String DLLPREFIX = "lib"; 
        internal const String DLLSUFFIX = ".sl";
  #else
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".so"; 
  #endif
 #endif // !PLATFORM_UNIX 
 
        internal const String KERNEL32 = DLLPREFIX + "rotor_pal" + DLLSUFFIX;
        internal const String USER32   = DLLPREFIX + "rotor_pal" + DLLSUFFIX; 
        internal const String ADVAPI32 = DLLPREFIX + "rotor_pal" + DLLSUFFIX;
        internal const String OLE32    = DLLPREFIX + "rotor_pal" + DLLSUFFIX;
        internal const String OLEAUT32 = DLLPREFIX + "rotor_palrt" + DLLSUFFIX;
        internal const String SHIM     = DLLPREFIX + "sscoree" + DLLSUFFIX; 
        internal const String MSCORWKS = DLLPREFIX + "mscorwks" + DLLSUFFIX;
 
#endif // !FEATURE_PAL 

        internal const String LSTRCPY  = "lstrcpy"; 
        internal const String LSTRCPYN = "lstrcpyn";
        internal const String LSTRLEN  = "lstrlen";
        internal const String LSTRLENA = "lstrlenA";
        internal const String LSTRLENW = "lstrlenW"; 
        internal const String MOVEMEMORY = "RtlMoveMemory";
 
 
        // From WinBase.h
        internal const int SEM_FAILCRITICALERRORS = 1; 

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern void SetLastError(int errorCode); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool GetVersionEx([In, Out] OSVERSIONINFO ver);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetVersionEx([In, Out] OSVERSIONINFOEX ver);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int FormatMessage(int dwFlags, IntPtr lpSource,
                    int dwMessageId, int dwLanguageId, StringBuilder lpBuffer,
                    int nSize, IntPtr va_list_arguments); 

        // Gets an error message for a Win32 error code. 
        internal static String GetMessage(int errorCode) { 
            StringBuilder sb = new StringBuilder(512);
            int result = Win32Native.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS | 
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                Win32Native.NULL, errorCode, 0, sb, sb.Capacity, Win32Native.NULL);
            if (result != 0) {
                // result is the # of characters copied to the StringBuilder on NT, 
                // but on Win9x, it appears to be the number of MBCS bytes.
                // Just give up and return the String as-is... 
                String s = sb.ToString(); 
                return s;
            } 
            else {
                return Environment.GetResourceString("UnknownError_Num", errorCode);
            }
        } 

        [DllImport(KERNEL32, EntryPoint="LocalAlloc")] 
        [ResourceExposure(ResourceScope.None)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal static extern IntPtr LocalAlloc_NoSafeHandle(int uFlags, IntPtr sizetdwBytes); 

#if !FEATURE_PAL
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        SafeLocalAllocHandle LocalAlloc( 
            [In] int uFlags, 
            [In] IntPtr sizetdwBytes);
#endif // !FEATURE_PAL 

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern IntPtr LocalFree(IntPtr handle);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern void ZeroMemory(IntPtr handle, uint length);

#if !FEATURE_PAL
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer); 
 
        // Only call this on Win9x, because it doesn't handle more than 4 GB of
        // memory. 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GlobalMemoryStatus([In, Out] MEMORYSTATUS buffer);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        unsafe internal static extern IntPtr VirtualQuery(void* address, ref MEMORY_BASIC_INFORMATION buffer, IntPtr sizeOfBuffer); 

        // VirtualAlloc should generally be avoided, but is needed in 
        // the MemoryFailPoint implementation (within a CER) to increase the
        // size of the page file, ignoring any host memory allocators.
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        unsafe internal static extern void * VirtualAlloc(void* address, UIntPtr numBytes, int commitOrReserve, int pageProtectionMode); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        unsafe internal static extern bool VirtualFree(void* address, UIntPtr numBytes, int pageFreeMode);

#if IO_CANCELLATION_ENABLED 
        // Note - do NOT use this to call methods.  Use P/Invoke, which will
        // do much better things w.r.t. marshaling, pinning memory, security 
        // stuff, better interactions with thread aborts, etc.  I'm defining 
        // this solely to detect whether certain Longhorn builds define a
        // method, to detect whether a feature exists (no, I can't use version 
        // numbers easily yet - Longhorn code merge integration issues).
        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, String methodName); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false, SetLastError=true, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Process)]  // Is your module side-by-side? 
        internal static extern IntPtr GetModuleHandle(String moduleName);
#endif // IO_CANCELLATION_ENABLED 
#endif // !FEATURE_PAL

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern uint GetTempPath(int bufferLen, StringBuilder buffer);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, EntryPoint=LSTRCPY, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern IntPtr lstrcpy(IntPtr dst, String src); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, EntryPoint=LSTRCPY, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern IntPtr lstrcpy(StringBuilder dst, IntPtr src); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, EntryPoint=LSTRLEN)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int lstrlen(sbyte [] ptr);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, EntryPoint=LSTRLEN)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int lstrlen(IntPtr ptr);
 
        [DllImport(KERNEL32, CharSet=CharSet.Ansi, EntryPoint=LSTRLENA)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int lstrlenA(IntPtr ptr); 

        [DllImport(KERNEL32, CharSet=CharSet.Unicode, EntryPoint=LSTRLENW)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int lstrlenW(IntPtr ptr);

        [DllImport(Win32Native.OLEAUT32, CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        internal static extern IntPtr SysAllocStringLen(String src, int len);  // BSTR 

        [DllImport(Win32Native.OLEAUT32)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int SysStringLen(IntPtr bstr);
 
        [DllImport(Win32Native.OLEAUT32)]
        [ResourceExposure(ResourceScope.None)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern void SysFreeString(IntPtr bstr);
 
        [DllImport(KERNEL32, CharSet=CharSet.Unicode, EntryPoint=MOVEMEMORY)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern void CopyMemoryUni(IntPtr pdst, String psrc, IntPtr sizetcb);
 
        [DllImport(KERNEL32, CharSet=CharSet.Unicode, EntryPoint=MOVEMEMORY)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern void CopyMemoryUni(StringBuilder pdst, 
                    IntPtr psrc, IntPtr sizetcb);
 
        [DllImport(KERNEL32, CharSet=CharSet.Ansi, EntryPoint=MOVEMEMORY, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern void CopyMemoryAnsi(IntPtr pdst, String psrc, IntPtr sizetcb);
 
        [DllImport(KERNEL32, CharSet=CharSet.Ansi, EntryPoint=MOVEMEMORY, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern void CopyMemoryAnsi(StringBuilder pdst, 
                    IntPtr psrc, IntPtr sizetcb);
 

        [DllImport(KERNEL32)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int GetACP(); 

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool SetEvent(SafeWaitHandle handle);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool ResetEvent(SafeWaitHandle handle);
 
        //Do not use this method. Call the managed WaitHandle.WaitAny/WaitAll as you have to deal with
        //COM STA issues, thread aborts among many other things. This method was added to get around 
        //an OS issue regarding named mutexes, and we guarantee that we never block when calling this 
        //method directly.
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD WaitForMultipleObjects(DWORD nCount,IntPtr[] handles, bool bWaitAll, DWORD dwMilliseconds);

 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] // Machine or none based on the value of "name" 
        internal static extern SafeWaitHandle CreateEvent(SECURITY_ATTRIBUTES lpSecurityAttributes, bool isManualReset, bool initialState, String name); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern SafeWaitHandle OpenEvent(/* DWORD */ int desiredAccess, bool inheritHandle, String name);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        internal static extern SafeWaitHandle CreateMutex(SECURITY_ATTRIBUTES lpSecurityAttributes, bool initialOwner, String name); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern SafeWaitHandle OpenMutex(/* DWORD */ int desiredAccess, bool inheritHandle, String name);

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        internal static extern bool ReleaseMutex(SafeWaitHandle handle); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int GetFullPathName([In] char[] path, int numBufferChars, [Out] char[] buffer, IntPtr mustBeZero);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal unsafe static extern int GetFullPathName(char* path, int numBufferChars, char* buffer, IntPtr mustBeZero); 
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetLongPathName(String path, StringBuilder longPathBuffer, int bufferLength);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetLongPathName([In] char[] path, [Out] char [] longPathBuffer, int bufferLength);
 
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal unsafe static extern int GetLongPathName(char* path, char* longPathBuffer, int bufferLength);

        // Disallow access to all non-file devices from methods that take
        // a String.  This disallows DOS devices like "con:", "com1:", 
        // "lpt1:", etc.  Use this to avoid security problems, like allowing
        // a web client asking a server for "http://server/com1.aspx" and 
        // then causing a worker process to hang. 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        internal static SafeFileHandle SafeCreateFile(String lpFileName,
                    int dwDesiredAccess, System.IO.FileShare dwShareMode,
                    SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile) 
        {
            SafeFileHandle handle = CreateFile( lpFileName, dwDesiredAccess, dwShareMode, 
                                securityAttrs, dwCreationDisposition, 
                                dwFlagsAndAttributes, hTemplateFile );
 
            if (!handle.IsInvalid)
            {
                int fileType = Win32Native.GetFileType(handle);
                if (fileType != Win32Native.FILE_TYPE_DISK) { 
                    handle.Dispose();
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_FileStreamOnNonFiles")); 
                } 
            }
 
            return handle;
        }

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        internal static SafeFileHandle UnsafeCreateFile(String lpFileName, 
                    int dwDesiredAccess, System.IO.FileShare dwShareMode, 
                    SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile) 
        {
            SafeFileHandle handle = CreateFile( lpFileName, dwDesiredAccess, dwShareMode,
                                securityAttrs, dwCreationDisposition,
                                dwFlagsAndAttributes, hTemplateFile ); 

            return handle; 
        } 

        // Do not use these directly, use the safe or unsafe versions above. 
        // The safe version does not support devices (aka if will only open
        // files on disk), while the unsafe version give you the full semantic
        // of the native version.
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        private static extern SafeFileHandle CreateFile(String lpFileName, 
                    int dwDesiredAccess, System.IO.FileShare dwShareMode, 
                    SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern SafeFileMappingHandle CreateFileMapping(SafeFileHandle hFile, IntPtr lpAttributes, uint fProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, String lpName); 

        [DllImport(KERNEL32, SetLastError=true, ExactSpelling=true)] 
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern IntPtr MapViewOfFile(
            SafeFileMappingHandle handle, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumerOfBytesToMap); 

        [DllImport(KERNEL32, ExactSpelling=true)]
        [ResourceExposure(ResourceScope.Machine)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress );
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport(KERNEL32)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int GetFileType(SafeFileHandle handle);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool SetEndOfFile(SafeFileHandle hFile); 

        [DllImport(KERNEL32, SetLastError=true, EntryPoint="SetFilePointer")]
        [ResourceExposure(ResourceScope.None)]
        private unsafe static extern int SetFilePointerWin32(SafeFileHandle handle, int lo, int * hi, int origin); 

        [ResourceExposure(ResourceScope.None)] 
        internal unsafe static long SetFilePointer(SafeFileHandle handle, long offset, System.IO.SeekOrigin origin, out int hr) { 
            hr = 0;
            int lo = (int) offset; 
            int hi = (int) (offset >> 32);
            lo = SetFilePointerWin32(handle, lo, &hi, (int) origin);

            if (lo == -1 && ((hr = Marshal.GetLastWin32Error()) != 0)) 
                return -1;
            return (long) (((ulong) ((uint) hi)) << 32) | ((uint) lo); 
        } 

        // Note there are two different ReadFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in
        // Win32's async IO support.  You can't do the following three things
        // simultaneously: overlapped IO, free the memory for the overlapped
        // struct in a callback (or an EndRead method called by that callback), 
        // and pass in an address for the numBytesRead parameter.
        // <STRIP> See Windows Bug 105512 for details.  -- </STRIP> 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        unsafe internal static extern int ReadFile(SafeFileHandle handle, byte* bytes, int numBytesToRead, IntPtr numBytesRead_mustBeZero, NativeOverlapped* overlapped);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        unsafe internal static extern int ReadFile(SafeFileHandle handle, byte* bytes, int numBytesToRead, out int numBytesRead, IntPtr mustBeZero);
 
        // Note there are two different WriteFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in
        // Win32's async IO support.  You can't do the following three things 
        // simultaneously: overlapped IO, free the memory for the overlapped
        // struct in a callback (or an EndWrite method called by that callback),
        // and pass in an address for the numBytesRead parameter.
        // <STRIP> See Windows Bug 105512 for details.  -- </STRIP> 

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, IntPtr numBytesWritten_mustBeZero, NativeOverlapped* lpOverlapped);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);
 
#if IO_CANCELLATION_ENABLED
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool CancelSynchronousIo(IntPtr threadHandle);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)]
        internal static unsafe extern bool CancelIoEx(SafeFileHandle handle, NativeOverlapped* lpOverlapped);
#endif 

        // NOTE: The out parameters are PULARGE_INTEGERs and may require 
        // some byte munging magic. 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool GetDiskFreeSpaceEx(String drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int GetDriveType(String drive);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetVolumeInformation(String drive, StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, StringBuilder fileSystemName, int fileSystemNameBufLen); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool SetVolumeLabel(String driveLetter, String volumeName); 

#if !FEATURE_PAL 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int GetWindowsDirectory(StringBuilder sb, int length); 

        // In winnls.h
        internal const int LCMAP_SORTKEY    = 0x00000400;
 
        // Used for synthetic cultures, which are not currently supported in ROTOR.
        [DllImport(KERNEL32, CharSet=CharSet.Unicode, ExactSpelling=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static unsafe extern int LCMapStringW(int lcid, int flags, char *src, int cchSrc, char *target, int cchTarget);
 
        // Will be in winnls.h
        internal const int FIND_STARTSWITH  = 0x00100000; // see if value is at the beginning of source
        internal const int FIND_ENDSWITH    = 0x00200000; // see if value is at the end of source
        internal const int FIND_FROMSTART   = 0x00400000; // look for value in source, starting at the beginning 
        internal const int FIND_FROMEND     = 0x00800000; // look for value in source, starting at the end
 
 
        // Used for synthetic cultures, which are not currently supported in ROTOR (neither is this entry).
        // Last parameter is an LPINT which on success returns the length of the string found. Since that value 
        // is not used and NULL needs to be passed, it is defined as an IntPtr rather than an out int.
        [DllImport(KERNEL32, CharSet=CharSet.Unicode, ExactSpelling=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static unsafe extern int FindNLSString(int Locale, int dwFindFlags, char *lpStringSource, int cchSource, char *lpStringValue, int cchValue, IntPtr pcchFound); // , out int pcchFound); 
#endif
 
#if !FEATURE_PAL 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetSystemDirectory(StringBuilder sb, int length);
#else
        [DllImport(KERNEL32, CharSet=CharSet.Unicode, SetLastError=true, EntryPoint="PAL_GetPALDirectoryW", BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetSystemDirectory(StringBuilder sb, int length);
 
        [DllImport(OLEAUT32, CharSet=CharSet.Unicode, SetLastError=true, EntryPoint="PAL_FetchConfigurationStringW")] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool FetchConfigurationString(bool perMachine, String parameterName, StringBuilder parameterValue, int parameterValueLength); 
#endif // !FEATURE_PAL

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal unsafe static extern bool SetFileTime(SafeFileHandle hFile, FILE_TIME* creationTime,
                    FILE_TIME* lastAccessTime, FILE_TIME* lastWriteTime); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int GetFileSize(SafeFileHandle hFile, out int highSize);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool LockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool UnlockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh); 

        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h
        internal static readonly IntPtr NULL = IntPtr.Zero;
 
        // Note, these are #defines used to extract handles, and are NOT handles.
        internal const int STD_INPUT_HANDLE = -10; 
        internal const int STD_OUTPUT_HANDLE = -11; 
        internal const int STD_ERROR_HANDLE = -12;
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);  // param is NOT a handle, but it returns one!
 
        // From wincon.h
        internal const int CTRL_C_EVENT = 0; 
        internal const int CTRL_BREAK_EVENT = 1; 
        internal const int CTRL_CLOSE_EVENT = 2;
        internal const int CTRL_LOGOFF_EVENT = 5; 
        internal const int CTRL_SHUTDOWN_EVENT = 6;
        internal const short KEY_EVENT = 1;

        // From WinBase.h 
        internal const int FILE_TYPE_DISK = 0x0001;
        internal const int FILE_TYPE_CHAR = 0x0002; 
        internal const int FILE_TYPE_PIPE = 0x0003; 

        internal const int REPLACEFILE_WRITE_THROUGH = 0x1; 
        internal const int REPLACEFILE_IGNORE_MERGE_ERRORS = 0x2;

        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM    = 0x00001000; 
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
 
        // Constants from WinNT.h 
        internal const int FILE_ATTRIBUTE_READONLY      = 0x00000001;
        internal const int FILE_ATTRIBUTE_DIRECTORY     = 0x00000010; 
        internal const int FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;

        internal const int IO_REPARSE_TAG_MOUNT_POINT = unchecked((int)0xA0000003);
 
        internal const int PAGE_READWRITE = 0x04;
 
        internal const int MEM_COMMIT  =  0x1000; 
        internal const int MEM_RESERVE =  0x2000;
        internal const int MEM_RELEASE =  0x8000; 
        internal const int MEM_FREE    = 0x10000;

        // Error codes from WinError.h
        internal const int ERROR_SUCCESS = 0x0; 
        internal const int ERROR_INVALID_FUNCTION = 0x1;
        internal const int ERROR_FILE_NOT_FOUND = 0x2; 
        internal const int ERROR_PATH_NOT_FOUND = 0x3; 
        internal const int ERROR_ACCESS_DENIED  = 0x5;
        internal const int ERROR_INVALID_HANDLE = 0x6; 
        internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
        internal const int ERROR_INVALID_DATA = 0xd;
        internal const int ERROR_INVALID_DRIVE = 0xf;
        internal const int ERROR_NO_MORE_FILES = 0x12; 
        internal const int ERROR_NOT_READY = 0x15;
        internal const int ERROR_BAD_LENGTH = 0x18; 
        internal const int ERROR_SHARING_VIOLATION = 0x20; 
        internal const int ERROR_NOT_SUPPORTED = 0x32;
        internal const int ERROR_FILE_EXISTS = 0x50; 
        internal const int ERROR_INVALID_PARAMETER = 0x57;
        internal const int ERROR_CALL_NOT_IMPLEMENTED = 0x78;
        internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
        internal const int ERROR_INVALID_NAME = 0x7B; 
        internal const int ERROR_BAD_PATHNAME = 0xA1;
        internal const int ERROR_ALREADY_EXISTS = 0xB7; 
        internal const int ERROR_ENVVAR_NOT_FOUND = 0xCB; 
        internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
        internal const int ERROR_NO_DATA = 0xE8; 
        internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;
        internal const int ERROR_MORE_DATA = 0xEA;
        internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
        internal const int ERROR_NO_TOKEN = 0x3f0; 
        internal const int ERROR_DLL_INIT_FAILED = 0x45A;
        internal const int ERROR_NON_ACCOUNT_SID = 0x4E9; 
        internal const int ERROR_NOT_ALL_ASSIGNED = 0x514; 
        internal const int ERROR_UNKNOWN_REVISION = 0x519;
        internal const int ERROR_INVALID_OWNER = 0x51B; 
        internal const int ERROR_INVALID_PRIMARY_GROUP = 0x51C;
        internal const int ERROR_NO_SUCH_PRIVILEGE = 0x521;
        internal const int ERROR_PRIVILEGE_NOT_HELD = 0x522;
        internal const int ERROR_NONE_MAPPED = 0x534; 
        internal const int ERROR_INVALID_ACL = 0x538;
        internal const int ERROR_INVALID_SID = 0x539; 
        internal const int ERROR_INVALID_SECURITY_DESCR = 0x53A; 
        internal const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;
        internal const int ERROR_CANT_OPEN_ANONYMOUS = 0x543; 
        internal const int ERROR_NO_SECURITY_ON_OBJECT = 0x546;
        internal const int ERROR_TRUSTED_RELATIONSHIP_FAILURE = 0x6FD;

        // Error codes from ntstatus.h 
        internal const uint STATUS_SUCCESS = 0x00000000;
        internal const uint STATUS_SOME_NOT_MAPPED = 0x00000107; 
        internal const uint STATUS_NO_MEMORY = 0xC0000017; 
        internal const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;
        internal const uint STATUS_NONE_MAPPED = 0xC0000073; 
        internal const uint STATUS_INSUFFICIENT_RESOURCES = 0xC000009A;
        internal const uint STATUS_ACCESS_DENIED = 0xC0000022;

        internal const int INVALID_FILE_SIZE     = -1; 

        // From WinStatus.h 
        internal const int STATUS_ACCOUNT_RESTRICTION = unchecked((int) 0xC000006E); 

        // Use this to translate error codes like the above into HRESULTs like 
        // 0x80070006 for ERROR_INVALID_HANDLE
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            BCLDebug.Assert((0xFFFF0000 & errorCode) == 0, "This is an HRESULT, not an error code!"); 
            return unchecked(((int)0x80070000) | errorCode);
        } 
 
        // Win32 Structs in N/Direct style
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto), Serializable] 
        [BestFitMapping(false)]
        internal class WIN32_FIND_DATA {
            internal int  dwFileAttributes = 0;
            // ftCreationTime was a by-value FILETIME structure 
            internal int  ftCreationTime_dwLowDateTime = 0 ;
            internal int  ftCreationTime_dwHighDateTime = 0; 
            // ftLastAccessTime was a by-value FILETIME structure 
            internal int  ftLastAccessTime_dwLowDateTime = 0;
            internal int  ftLastAccessTime_dwHighDateTime = 0; 
            // ftLastWriteTime was a by-value FILETIME structure
            internal int  ftLastWriteTime_dwLowDateTime = 0;
            internal int  ftLastWriteTime_dwHighDateTime = 0;
            internal int  nFileSizeHigh = 0; 
            internal int  nFileSizeLow = 0;
            // If the file attributes' reparse point flag is set, then 
            // dwReserved0 is the file tag (aka reparse tag) for the 
            // reparse point.  Use this to figure out whether something is
            // a volume mount point or a symbolic link. 
            internal int  dwReserved0 = 0;
            internal int  dwReserved1 = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
            internal String   cFileName = null; 
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=14)]
            internal String   cAlternateFileName = null; 
        } 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool CopyFile(
                    String src, String dst, bool failIfExists);
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern bool CreateDirectory( 
                    String path, SECURITY_ATTRIBUTES lpSecurityAttributes);
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool DeleteFile(String path);
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern bool ReplaceFile(String replacedFileName, String replacementFileName, String backupFileName, int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved); 

        [DllImport(ADVAPI32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool DecryptFile(String path, int reservedMustBeZero);

        [DllImport(ADVAPI32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool EncryptFile(String path); 
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern SafeFindHandle FindFirstFile(String fileName, [In, Out] Win32Native.WIN32_FIND_DATA data);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool FindNextFile(
                    SafeFindHandle hndFindFile, 
                    [In, Out, MarshalAs(UnmanagedType.LPStruct)] 
                    WIN32_FIND_DATA lpFindFileData);
 
        [DllImport(KERNEL32)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern bool FindClose(IntPtr handle); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetCurrentDirectory(
                  int nBufferLength, 
                  StringBuilder lpBuffer);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool GetFileAttributesEx(String name, int fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool SetFileAttributes(String name, int attr); 

#if !PLATFORM_UNIX
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int GetLogicalDrives();
#endif // !PLATFORM_UNIX 
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern uint GetTempFileName(String tmpPath, String prefix, uint uniqueIdOrZero, StringBuilder tmpFileName);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern bool MoveFile(String src, String dst);
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool DeleteVolumeMountPoint(String mountPoint); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool RemoveDirectory(String path); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern bool SetCurrentDirectory(String path);
 
        [DllImport(KERNEL32, SetLastError=false)]
        [ResourceExposure(ResourceScope.Process)]
        internal static extern int SetErrorMode(int newMode);
 
        internal const int LCID_SUPPORTED = 0x00000002;  // supported locale ids
 
        [DllImport(KERNEL32)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern unsafe int WideCharToMultiByte(uint cp, uint flags, char* pwzSource, int cchSource, byte* pbDestBuffer, int cbDestBuffer, IntPtr null1, IntPtr null2); 

        // A Win32 HandlerRoutine
        internal delegate bool ConsoleCtrlHandlerRoutine(int controlType);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine handler, bool addOrRemove);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetEnvironmentVariable(string lpName, StringBuilder lpValue, int size); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static extern uint GetCurrentProcessId();

        [DllImport(ADVAPI32, CharSet=CharSet.Auto)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetUserName(StringBuilder lpBuffer, ref int nSize); 
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal extern static int GetComputerName(StringBuilder nameBuffer, ref int bufferSize); 

#if FEATURE_COMINTEROP
        [DllImport(Win32Native.OLE32)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern IntPtr CoTaskMemAlloc(int cb);
 
        [DllImport(Win32Native.OLE32)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern IntPtr CoTaskMemRealloc(IntPtr pv, int cb); 

        [DllImport(Win32Native.OLE32)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern void CoTaskMemFree(IntPtr ptr); 
#endif // FEATURE_COMINTEROP
 
#if !FEATURE_PAL 
        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct COORD 
        {
            internal short X;
            internal short Y;
        } 

        [StructLayoutAttribute(LayoutKind.Sequential)] 
        internal struct SMALL_RECT 
        {
            internal short Left; 
            internal short Top;
            internal short Right;
            internal short Bottom;
        } 

        [StructLayoutAttribute(LayoutKind.Sequential)] 
        internal struct CONSOLE_SCREEN_BUFFER_INFO 
        {
            internal COORD      dwSize; 
            internal COORD      dwCursorPosition;
            internal short      wAttributes;
            internal SMALL_RECT srWindow;
            internal COORD      dwMaximumWindowSize; 
        }
 
        [StructLayoutAttribute(LayoutKind.Sequential)] 
        internal struct CONSOLE_CURSOR_INFO
        { 
            internal int dwSize;
            internal bool bVisible;
        }
 
        // Win32's KEY_EVENT_RECORD
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)] 
        internal struct KeyEventRecord 
        {
            internal bool keyDown; 
            internal short repeatCount;
            internal short virtualKeyCode;
            internal short virtualScanCode;
            internal char uChar; 
            internal int controlKeyState;
        } 
 
        // Really, this is a union of KeyEventRecords and other types.
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)] 
        internal struct InputRecord
        {
            internal short eventType;
            // This is a union!  Must make sure INPUT_RECORD's size matches this. 
            // However, KEY_EVENT_RECORD is the largest part of the union.
            internal KeyEventRecord keyEvent; 
        } 

        [Flags, Serializable] 
        internal enum Color : short
        {
            Black = 0,
            ForegroundBlue = 0x1, 
            ForegroundGreen = 0x2,
            ForegroundRed = 0x4, 
            ForegroundYellow = 0x6, 
            ForegroundIntensity = 0x8,
            BackgroundBlue = 0x10, 
            BackgroundGreen = 0x20,
            BackgroundRed = 0x40,
            BackgroundYellow = 0x60,
            BackgroundIntensity = 0x80, 

            ForegroundMask = 0xf, 
            BackgroundMask = 0xf0, 
            ColorMask = 0xff
        } 

        [StructLayout(LayoutKind.Sequential)]
        internal struct CHAR_INFO
        { 
            ushort charData;  // Union between WCHAR and ASCII char
            short attributes; 
        } 

        internal const int ENABLE_PROCESSED_INPUT  = 0x0001; 
        internal const int ENABLE_LINE_INPUT  = 0x0002;
        internal const int ENABLE_ECHO_INPUT  = 0x0004;

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int mode);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool Beep(int frequency, int duration);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, 
            out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, COORD size);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern COORD GetLargestConsoleWindowSize(IntPtr hConsoleOutput); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool FillConsoleOutputCharacter(IntPtr hConsoleOutput, 
            char character, int nLength, COORD dwWriteCoord, out int pNumCharsWritten);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool FillConsoleOutputAttribute(IntPtr hConsoleOutput, 
            short wColorAttribute, int numCells, COORD startCoord, out int pNumBytesWritten);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static unsafe extern bool SetConsoleWindowInfo(IntPtr hConsoleOutput,
            bool absolute, SMALL_RECT* consoleWindow); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, short attributes);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput,
            COORD cursorPosition); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool GetConsoleCursorInfo(IntPtr hConsoleOutput,
            out CONSOLE_CURSOR_INFO cci);

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool SetConsoleCursorInfo(IntPtr hConsoleOutput, 
            ref CONSOLE_CURSOR_INFO cci); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int GetConsoleTitle(StringBuilder sb, int capacity);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool SetConsoleTitle(String title); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool ReadConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool PeekConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static unsafe extern bool ReadConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO* pBuffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT readRegion); 

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)]
        internal static unsafe extern bool WriteConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO* buffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT writeRegion); 

        [DllImport(USER32)]  // Appears to always succeed 
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern short GetKeyState(int virtualKeyCode);
#endif // !FEATURE_PAL 

        [DllImport(KERNEL32, SetLastError=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern uint GetConsoleCP(); 

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleCP(uint codePage);
 
        [DllImport(KERNEL32, SetLastError=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern uint GetConsoleOutputCP();
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleOutputCP(uint codePage); 

#if !FEATURE_PAL 
        internal const int VER_PLATFORM_WIN32s = 0;
        internal const int VER_PLATFORM_WIN32_WINDOWS = 1;
        internal const int VER_PLATFORM_WIN32_NT = 2;
        internal const int VER_PLATFORM_WINCE = 3; 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int RegConnectRegistry(String machineName,
                    SafeRegistryHandle key, out SafeRegistryHandle result); 

        // Note: RegCreateKeyEx won't set the last error on failure - it returns
        // an error code if it fails.
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int RegCreateKeyEx(SafeRegistryHandle hKey, String lpSubKey, 
                    int Reserved, String lpClass, int dwOptions, 
                    int samDesigner, SECURITY_ATTRIBUTES lpSecurityAttributes,
                    out SafeRegistryHandle hkResult, out int lpdwDisposition); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int RegDeleteKey(SafeRegistryHandle hKey, String lpSubKey); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int RegDeleteValue(SafeRegistryHandle hKey, String lpValueName);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegEnumKeyEx(SafeRegistryHandle hKey, int dwIndex,
                    StringBuilder lpName, out int lpcbName, int[] lpReserved, 
                    StringBuilder lpClass, int[] lpcbClass,
                    long[] lpftLastWriteTime); 
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegEnumValue(SafeRegistryHandle hKey, int dwIndex,
                    StringBuilder lpValueName, ref int lpcbValueName,
                    IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData,
                    int[] lpcbData); 

        [DllImport(ADVAPI32, CharSet=CharSet.Ansi, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegEnumValueA(SafeRegistryHandle hKey, int dwIndex,
                     StringBuilder lpValueName, ref int lpcbValueName, 
                     IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData,
                     int[] lpcbData);

 
        [DllImport(ADVAPI32)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegFlushKey(SafeRegistryHandle hKey); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int RegOpenKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegQueryInfoKey(SafeRegistryHandle hKey, StringBuilder lpClass, 
                    int[] lpcbClass, IntPtr lpReserved_MustBeZero, ref int lpcSubKeys,
                    int[] lpcbMaxSubKeyLen, int[] lpcbMaxClassLen, 
                    ref int lpcValues, int[] lpcbMaxValueNameLen,
                    int[] lpcbMaxValueLen, int[] lpcbSecurityDescriptor,
                    int[] lpftLastWriteTime);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, 
                    int[] lpReserved, ref int lpType, [Out] byte[] lpData,
                    ref int lpcbData); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, 
                    int[] lpReserved, ref int lpType, ref int lpData,
                    ref int lpcbData); 
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, ref long lpData,
                    ref int lpcbData);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, 
                     int[] lpReserved, ref int lpType, [Out] char[] lpData,
                     ref int lpcbData); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, 
                    int[] lpReserved, ref int lpType, StringBuilder lpData,
                    ref int lpcbData); 
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, byte[] lpData, int cbData);

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName, 
                    int Reserved, RegistryValueKind dwType, ref int lpData, int cbData); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, ref long lpData, int cbData);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName, 
                    int Reserved, RegistryValueKind dwType, String lpData, int cbData);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int ExpandEnvironmentStrings(String lpSrc, StringBuilder lpDst, int nSize);
 
        [DllImport(KERNEL32)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern IntPtr LocalReAlloc(IntPtr handle, IntPtr sizetcbBytes, int uFlags); 

        internal const int SHGFP_TYPE_CURRENT               = 0;             // the current (user) folder path setting 
        internal const int UOI_FLAGS                        = 1;
        internal const int WSF_VISIBLE                      = 1;
        internal const int CSIDL_APPDATA                    = 0x001a;
        internal const int CSIDL_COMMON_APPDATA             = 0x0023; 
        internal const int CSIDL_LOCAL_APPDATA              = 0x001c;
        internal const int CSIDL_COOKIES                    = 0x0021; 
        internal const int CSIDL_FAVORITES                  = 0x0006; 
        internal const int CSIDL_HISTORY                    = 0x0022;
        internal const int CSIDL_INTERNET_CACHE             = 0x0020; 
        internal const int CSIDL_PROGRAMS                   = 0x0002;
        internal const int CSIDL_RECENT                     = 0x0008;
        internal const int CSIDL_SENDTO                     = 0x0009;
        internal const int CSIDL_STARTMENU                  = 0x000b; 
        internal const int CSIDL_STARTUP                    = 0x0007;
        internal const int CSIDL_SYSTEM                     = 0x0025; 
        internal const int CSIDL_TEMPLATES                  = 0x0015; 
        internal const int CSIDL_DESKTOPDIRECTORY           = 0x0010;
        internal const int CSIDL_PERSONAL                   = 0x0005; 
        internal const int CSIDL_PROGRAM_FILES              = 0x0026;
        internal const int CSIDL_PROGRAM_FILES_COMMON       = 0x002b;
        internal const int CSIDL_DESKTOP                    = 0x0000;
        internal const int CSIDL_DRIVES                     = 0x0011; 
        internal const int CSIDL_MYMUSIC                    = 0x000d;
        internal const int CSIDL_MYPICTURES                 = 0x0027; 
 
        [DllImport(SHFOLDER, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, int dwFlags, StringBuilder lpszPath);

        internal const int NameSamCompatible = 2;
 
        [ResourceExposure(ResourceScope.None)]
        [DllImport(SECUR32, CharSet=CharSet.Unicode, SetLastError=true)] 
        // Win32 return type is BOOLEAN (which is 1 byte and not BOOL which is 4bytes) 
        internal static extern byte GetUserNameEx(int format, StringBuilder domainName, ref int domainNameLen);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool LookupAccountName(string machineName, string accountName, byte[] sid,
                                 ref int sidLen, StringBuilder domainName, ref int domainNameLen, out int peUse); 

        // Note: This returns a handle, but it shouldn't be closed.  The Avalon 
        // team says CloseWindowStation would ignore this handle.  So there 
        // isn't a lot of value to switching to SafeHandle here.
        [DllImport(USER32, ExactSpelling=true)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern IntPtr GetProcessWindowStation();

        [DllImport(USER32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex, 
            [MarshalAs(UnmanagedType.LPStruct)] USEROBJECTFLAGS pvBuffer, int nLength, ref int lpnLengthNeeded); 

        [DllImport(USER32, SetLastError=true, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, String lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult);

        [StructLayout(LayoutKind.Sequential)] 
        internal class USEROBJECTFLAGS {
            internal int fInherit = 0; 
            internal int fReserved = 0; 
            internal int dwFlags = 0;
        } 

        //
        // DPAPI
        // 

        // 
        // RtlEncryptMemory and RtlDecryptMemory are declared in the internal header file crypt.h. 
        // They were also recently declared in the public header file ntsecapi.h (in the Platform SDK as well as the current build of Server 2003).
        // We use them instead of CryptProtectMemory and CryptUnprotectMemory because 
        // they are available in both WinXP and in Windows Server 2003.
        //

        [DllImport(Win32Native.ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern 
        int SystemFunction040 (
            [In,Out] SafeBSTRHandle     pDataIn, 
            [In]     uint       cbDataIn,   // multiple of RTL_ENCRYPT_MEMORY_SIZE
            [In]     uint       dwFlags);

        [DllImport(Win32Native.ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern 
        int SystemFunction041 ( 
            [In,Out] SafeBSTRHandle     pDataIn,
            [In]     uint       cbDataIn,   // multiple of RTL_ENCRYPT_MEMORY_SIZE 
            [In]     uint       dwFlags);

        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        int LsaNtStatusToWinError ( 
            [In]    int         status); 

#if !FEATURE_PAL 
        // Get the current FIPS policy setting on Vista and above
        [DllImport("bcrypt.dll")]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern uint BCryptGetFipsAlgorithmMode( 
                [MarshalAs(UnmanagedType.U1), Out]out bool pfEnabled);
#endif 
 
        //
        // Managed ACLs 
        //

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern 
        bool AdjustTokenPrivileges ( 
            [In]     SafeTokenHandle       TokenHandle,
            [In]     bool                  DisableAllPrivileges, 
            [In]     ref TOKEN_PRIVILEGE   NewState,
            [In]     uint                  BufferLength,
            [In,Out] ref TOKEN_PRIVILEGE   PreviousState,
            [In,Out] ref uint              ReturnLength); 

        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        bool AllocateLocallyUniqueId( 
            [In,Out] ref LUID              Luid);

        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        bool CheckTokenMembership( 
            [In]     SafeTokenHandle TokenHandle, 
            [In]     byte[]          SidToCheck,
            [In,Out] ref bool        IsMember); 

        [DllImport(
             ADVAPI32,
             EntryPoint="ConvertSecurityDescriptorToStringSecurityDescriptorW", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern BOOL ConvertSdToStringSd( 
            byte[] securityDescriptor,
            DWORD requestedRevision,
            ULONG securityInformation,
            out IntPtr resultString, 
            ref ULONG resultStringLength );
 
        [DllImport( 
             ADVAPI32,
             EntryPoint="ConvertStringSecurityDescriptorToSecurityDescriptorW", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern BOOL ConvertStringSdToSd(
            string stringSd, 
            DWORD stringSdRevision, 
            out IntPtr resultSd,
            ref ULONG resultSdLength ); 

        [DllImport(
             ADVAPI32,
             EntryPoint="ConvertStringSidToSidW", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern BOOL ConvertStringSidToSid( 
            string stringSid,
            out IntPtr ByteArray
            );
 
        [DllImport(
             ADVAPI32, 
             EntryPoint="CreateWellKnownSid", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern BOOL CreateWellKnownSid(
            int sidType, 
            byte[] domainSid,
            [Out] byte[] resultSid, 
            ref DWORD resultSidLength ); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern
        bool DuplicateHandle (
            [In]     IntPtr                     hSourceProcessHandle, 
            [In]     IntPtr                     hSourceHandle,
            [In]     IntPtr                     hTargetProcessHandle, 
            [In,Out] ref SafeTokenHandle        lpTargetHandle, 
            [In]     uint                       dwDesiredAccess,
            [In]     bool                       bInheritHandle, 
            [In]     uint                       dwOptions);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern 
        bool DuplicateHandle ( 
            [In]     IntPtr                     hSourceProcessHandle,
            [In]     SafeTokenHandle            hSourceHandle, 
            [In]     IntPtr                     hTargetProcessHandle,
            [In,Out] ref SafeTokenHandle        lpTargetHandle,
            [In]     uint                       dwDesiredAccess,
            [In]     bool                       bInheritHandle, 
            [In]     uint                       dwOptions);
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        bool DuplicateTokenEx (
            [In]     SafeTokenHandle             ExistingTokenHandle,
            [In]     TokenAccessLevels           DesiredAccess, 
            [In]     IntPtr                      TokenAttributes,
            [In]     SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, 
            [In]     System.Security.Principal.TokenType TokenType, 
            [In,Out] ref SafeTokenHandle         DuplicateTokenHandle );
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern
        bool DuplicateTokenEx ( 
            [In]     SafeTokenHandle            hExistingToken,
            [In]     uint                       dwDesiredAccess, 
            [In]     IntPtr                     lpTokenAttributes,   // LPSECURITY_ATTRIBUTES 
            [In]     uint                       ImpersonationLevel,
            [In]     uint                       TokenType, 
            [In,Out] ref SafeTokenHandle        phNewToken);

        [DllImport(
             ADVAPI32, 
             EntryPoint="EqualDomainSid",
             CallingConvention=CallingConvention.Winapi, 
             SetLastError=true, 
             CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern BOOL IsEqualDomainSid(
            byte[] sid1,
            byte[] sid2,
            out bool result); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern IntPtr GetCurrentProcess();
 
        [DllImport(
             ADVAPI32,
             EntryPoint="GetSecurityDescriptorLength",
             CallingConvention=CallingConvention.Winapi, 
             SetLastError=true,
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern DWORD GetSecurityDescriptorLength(
            IntPtr byteArray ); 

        [DllImport(
             ADVAPI32,
             EntryPoint="GetSecurityInfo", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD GetSecurityInfoByHandle( 
            SafeHandle handle,
            DWORD objectType,
            DWORD securityInformation,
            out IntPtr sidOwner, 
            out IntPtr sidGroup,
            out IntPtr dacl, 
            out IntPtr sacl, 
            out IntPtr securityDescriptor );
 
        [DllImport(
             ADVAPI32,
             EntryPoint="GetNamedSecurityInfoW",
             CallingConvention=CallingConvention.Winapi, 
             SetLastError=true,
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern DWORD GetSecurityInfoByName(
            string name, 
            DWORD objectType,
            DWORD securityInformation,
            out IntPtr sidOwner,
            out IntPtr sidGroup, 
            out IntPtr dacl,
            out IntPtr sacl, 
            out IntPtr securityDescriptor ); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern
        bool GetTokenInformation (
            [In]  IntPtr                TokenHandle, 
            [In]  uint                  TokenInformationClass,
            [In]  SafeLocalAllocHandle  TokenInformation, 
            [In]  uint                  TokenInformationLength, 
            [Out] out uint              ReturnLength);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern
        bool GetTokenInformation ( 
            [In]  SafeTokenHandle       TokenHandle,
            [In]  uint                  TokenInformationClass, 
            [In]  SafeLocalAllocHandle  TokenInformation, 
            [In]  uint                  TokenInformationLength,
            [Out] out uint              ReturnLength); 

        [DllImport(
             ADVAPI32,
             EntryPoint="GetWindowsAccountDomainSid", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern BOOL GetWindowsAccountDomainSid( 
            byte[] sid,
            [Out] byte[] resultSid,
            ref DWORD resultSidLength );
 
        internal enum SECURITY_IMPERSONATION_LEVEL
        { 
            Anonymous = 0, 
            Identification = 1,
            Impersonation = 2, 
            Delegation = 3,
        }

        [DllImport( 
             ADVAPI32,
             EntryPoint="IsWellKnownSid", 
             CallingConvention=CallingConvention.Winapi, 
             SetLastError=true,
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern BOOL IsWellKnownSid(
            byte[] sid,
            int type ); 

        [DllImport( 
            ADVAPI32, 
            EntryPoint="LsaOpenPolicy",
            CallingConvention=CallingConvention.Winapi, 
            SetLastError=true,
            CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD LsaOpenPolicy( 
            string systemName,
            ref LSA_OBJECT_ATTRIBUTES attributes, 
            int accessMask, 
            out SafeLsaPolicyHandle handle
            ); 

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(
            ADVAPI32, 
            EntryPoint="LookupPrivilegeValueW",
            CharSet=CharSet.Auto, 
            SetLastError=true, 
            BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        bool LookupPrivilegeValue (
            [In]     string             lpSystemName,
            [In]     string             lpName, 
            [In,Out] ref LUID           Luid);
 
        [DllImport( 
            ADVAPI32,
            EntryPoint="LsaLookupSids", 
            CallingConvention=CallingConvention.Winapi,
            SetLastError=true,
            CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern DWORD LsaLookupSids(
            SafeLsaPolicyHandle handle, 
            int count, 
            IntPtr[] sids,
            ref SafeLsaMemoryHandle referencedDomains, 
            ref SafeLsaMemoryHandle names
            );

        [DllImport(ADVAPI32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern int LsaFreeMemory( IntPtr handle ); 

        [DllImport( 
            ADVAPI32,
            EntryPoint="LsaLookupNames",
            CallingConvention=CallingConvention.Winapi,
            SetLastError=true, 
            CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern DWORD LsaLookupNames( 
            SafeLsaPolicyHandle handle,
            int count, 
            UNICODE_STRING[] names,
            ref SafeLsaMemoryHandle referencedDomains,
            ref SafeLsaMemoryHandle sids
            ); 

        [DllImport( 
            ADVAPI32, 
            EntryPoint="LsaLookupNames2",
            CallingConvention=CallingConvention.Winapi, 
            SetLastError=true,
            CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD LsaLookupNames2( 
            SafeLsaPolicyHandle handle,
            int flags, 
            int count, 
            UNICODE_STRING[] names,
            ref SafeLsaMemoryHandle referencedDomains, 
            ref SafeLsaMemoryHandle sids
            );

        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern 
        int LsaConnectUntrusted ( 
            [In,Out] ref SafeLsaLogonProcessHandle LsaHandle);
 
        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern
        int LsaGetLogonSessionData ( 
            [In]     ref LUID                      LogonId,
            [In,Out] ref SafeLsaReturnBufferHandle ppLogonSessionData); 
 
        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        int LsaLogonUser (
            [In]     SafeLsaLogonProcessHandle      LsaHandle,
            [In]     ref UNICODE_INTPTR_STRING      OriginName, 
            [In]     uint                           LogonType,
            [In]     uint                           AuthenticationPackage, 
            [In]     IntPtr                         AuthenticationInformation, 
            [In]     uint                           AuthenticationInformationLength,
            [In]     IntPtr                         LocalGroups, 
            [In]     ref TOKEN_SOURCE               SourceContext,
            [In,Out] ref SafeLsaReturnBufferHandle  ProfileBuffer,
            [In,Out] ref uint                       ProfileBufferLength,
            [In,Out] ref LUID                       LogonId, 
            [In,Out] ref SafeTokenHandle            Token,
            [In,Out] ref QUOTA_LIMITS               Quotas, 
            [In,Out] ref int                        SubStatus); 

        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern
        int LsaLookupAuthenticationPackage (
            [In]     SafeLsaLogonProcessHandle LsaHandle, 
            [In]     ref UNICODE_INTPTR_STRING PackageName,
            [In,Out] ref uint                  AuthenticationPackage); 
 
        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        int LsaRegisterLogonProcess (
            [In]     ref UNICODE_INTPTR_STRING     LogonProcessName,
            [In,Out] ref SafeLsaLogonProcessHandle LsaHandle, 
            [In,Out] ref IntPtr                    SecurityMode);
 
        [DllImport(SECUR32, SetLastError=true)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int LsaDeregisterLogonProcess(IntPtr handle); 

        [DllImport(ADVAPI32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern int LsaClose( IntPtr handle );
 
        [DllImport(SECUR32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern int LsaFreeReturnBuffer(IntPtr handle);

        [DllImport (ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern
        bool OpenProcessToken ( 
            [In]     IntPtr              ProcessToken, 
            [In]     TokenAccessLevels   DesiredAccess,
            [In,Out] ref SafeTokenHandle TokenHandle); 

        [DllImport(
             ADVAPI32,
             EntryPoint="SetNamedSecurityInfoW", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern DWORD SetSecurityInfoByName( 
            string name,
            DWORD objectType,
            DWORD securityInformation,
            byte[] owner, 
            byte[] group,
            byte[] dacl, 
            byte[] sacl ); 

        [DllImport( 
             ADVAPI32,
             EntryPoint="SetSecurityInfo",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern DWORD SetSecurityInfoByHandle( 
            SafeHandle handle,
            DWORD objectType, 
            DWORD securityInformation,
            byte[] owner,
            byte[] group,
            byte[] dacl, 
            byte[] sacl );
 
#else // FEATURE_PAL 

        // managed cryptography wrapper around the PALRT cryptography api 
        internal const int PAL_HCRYPTPROV = 123;

        internal const int CALG_MD2         = ((4 << 13) | 1);
        internal const int CALG_MD4         = ((4 << 13) | 2); 
        internal const int CALG_MD5         = ((4 << 13) | 3);
        internal const int CALG_SHA         = ((4 << 13) | 4); 
        internal const int CALG_SHA1        = ((4 << 13) | 4); 
        internal const int CALG_MAC         = ((4 << 13) | 5);
        internal const int CALG_SSL3_SHAMD5 = ((4 << 13) | 8); 
        internal const int CALG_HMAC        = ((4 << 13) | 9);

        internal const int HP_ALGID         = 0x0001;
        internal const int HP_HASHVAL       = 0x0002; 
        internal const int HP_HASHSIZE      = 0x0004;
 
        [DllImport(OLEAUT32, CharSet=CharSet.Unicode, EntryPoint="CryptAcquireContextW")] 
        [ResourceExposure(ResourceScope.Machine)]
        internal extern static bool CryptAcquireContext(out IntPtr hProv, 
                           [MarshalAs(UnmanagedType.LPWStr)] string container,
                           [MarshalAs(UnmanagedType.LPWStr)] string provider,
                           int provType,
                           int flags); 

        [DllImport(OLEAUT32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal extern static bool CryptReleaseContext( IntPtr hProv, int flags);
 
        [DllImport(OLEAUT32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal extern static bool CryptCreateHash(IntPtr hProv, int Algid, IntPtr hKey, int flags, out IntPtr hHash);
 
        [DllImport(OLEAUT32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal extern static bool CryptDestroyHash(IntPtr hHash); 

        [DllImport(OLEAUT32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal extern static bool CryptHashData(IntPtr hHash,
                           [In, MarshalAs(UnmanagedType.LPArray)] byte[] data,
                           int length, 
                           int flags);
 
        [DllImport(OLEAUT32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal extern static bool CryptGetHashParam(IntPtr hHash, 
                           int param,
                           [Out, MarshalAs(UnmanagedType.LPArray)] byte[] digest,
                           ref int length,
                           int flags); 

        [DllImport(OLEAUT32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal extern static bool CryptGetHashParam(IntPtr hHash,
                           int param, 
                           out int data,
                           ref int length,
                           int flags);
 
        [DllImport(KERNEL32, EntryPoint="PAL_Random")]
        [ResourceExposure(ResourceScope.None)] 
        internal extern static bool Random(bool bStrong, 
                           [Out, MarshalAs(UnmanagedType.LPArray)] byte[] buffer, int length);
#endif // FEATURE_PAL 

    // Fusion APIs
#if FEATURE_COMINTEROP
 
        [DllImport(MSCORWKS, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int CreateAssemblyNameObject(out IAssemblyName ppEnum, String szAssemblyName, uint dwFlags, IntPtr pvReserved); 

        [DllImport(MSCORWKS, CharSet=CharSet.Auto)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int CreateAssemblyEnum(out IAssemblyEnum ppEnum, IApplicationContext pAppCtx, IAssemblyName pName, uint dwFlags, IntPtr pvReserved);
#else // FEATURE_COMINTEROP
 
        [DllImport(MSCORWKS, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int CreateAssemblyNameObject(out SafeFusionHandle ppEnum, String szAssemblyName, uint dwFlags, IntPtr pvReserved); 

        [DllImport(MSCORWKS, CharSet=CharSet.Auto)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int CreateAssemblyEnum(out SafeFusionHandle ppEnum, SafeFusionHandle pAppCtx, SafeFusionHandle pName, uint dwFlags, IntPtr pvReserved);
#endif // FEATURE_COMINTEROP
 
    // Globalization APIs
        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)] 
        internal extern static int GetCalendarInfo(
                                      int           Locale,     // locale 
                                      int           Calendar,   // calendar identifier
                                      int           CalType,    // calendar type
                                      StringBuilder lpCalData,  // information buffer
                                      int           cchData,    // information buffer size 
                                      IntPtr        lpValue     // data
                                    ); 
    } 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  Microsoft.Win32.Win32Native 
**
** 
** Purpose: The CLR wrapper for all Win32 (Win2000, NT4, Win9x,
**          as well as ROTOR-style Unix PAL, etc.) native operations
**
** 
===========================================================*/
/** 
 * Notes to PInvoke users:  Getting the syntax exactly correct is crucial, and 
 * more than a little confusing.  Here's some guidelines.
 * 
 * For handles, you should use a SafeHandle subclass specific to your handle
 * type.  For files, we have the following set of interesting definitions:
 *
 *  [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
 *  private static extern SafeFileHandle CreateFile(...);
 * 
 *  [DllImport(KERNEL32, SetLastError=true)] 
 *  unsafe internal static extern int ReadFile(SafeFileHandle handle, ...);
 * 
 *  [DllImport(KERNEL32, SetLastError=true)]
 *  internal static extern bool CloseHandle(IntPtr handle);
 *
 * P/Invoke will create the SafeFileHandle instance for you and assign the 
 * return value from CreateFile into the handle atomically.  When we call
 * ReadFile, P/Invoke will increment a ref count, make the call, then decrement 
 * it (preventing handle recycling vulnerabilities).  Then SafeFileHandle's 
 * ReleaseHandle method will call CloseHandle, passing in the handle field
 * as an IntPtr. 
 *
 * If for some reason you cannot use a SafeHandle subclass for your handles,
 * then use IntPtr as the handle type (or possibly HandleRef - understand when
 * to use GC.KeepAlive).  If your code will run in SQL Server (or any other 
 * long-running process that can't be recycled easily), use a constrained
 * execution region to prevent thread aborts while allocating your 
 * handle, and consider making your handle wrapper subclass 
 * CriticalFinalizerObject to ensure you can free the handle.  As you can
 * probably guess, SafeHandle  will save you a lot of headaches if your code 
 * needs to be robust to thread aborts and OOM.
 *
 *
 * If you have a method that takes a native struct, you have two options for 
 * declaring that struct.  You can make it a value type ('struct' in CSharp),
 * or a reference type ('class').  This choice doesn't seem very interesting, 
 * but your function prototype must use different syntax depending on your 
 * choice.  For example, if your native method is prototyped as such:
 * 
 *    bool GetVersionEx(OSVERSIONINFO & lposvi);
 *
 *
 * you must use EITHER THIS OR THE NEXT syntax: 
 *
 *    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)] 
 *    internal struct OSVERSIONINFO {  ...  } 
 *
 *    [DllImport(KERNEL32, CharSet=CharSet.Auto)] 
 *    internal static extern bool GetVersionEx(ref OSVERSIONINFO lposvi);
 *
 * OR:
 * 
 *    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
 *    internal class OSVERSIONINFO {  ...  } 
 * 
 *    [DllImport(KERNEL32, CharSet=CharSet.Auto)]
 *    internal static extern bool GetVersionEx([In, Out] OSVERSIONINFO lposvi); 
 *
 * Note that classes require being marked as [In, Out] while value types must
 * be passed as ref parameters.
 * 
 * Also note the CharSet.Auto on GetVersionEx - while it does not take a String
 * as a parameter, the OSVERSIONINFO contains an embedded array of TCHARs, so 
 * the size of the struct varies on different platforms, and there's a 
 * GetVersionExA & a GetVersionExW.  Also, the OSVERSIONINFO struct has a sizeof
 * field so the OS can ensure you've passed in the correctly-sized copy of an 
 * OSVERSIONINFO.  You must explicitly set this using Marshal.SizeOf(Object);
 *
 * For security reasons, if you're making a P/Invoke method to a Win32 method
 * that takes an ANSI String (or will be ANSI on Win9x) and that String is the 
 * name of some resource you've done a security check on (such as a file name),
 * you want to disable best fit mapping in WideCharToMultiByte.  Do this by 
 * setting BestFitMapping=false in your DllImportAttribute. 
 */
 
namespace Microsoft.Win32 {
    using System;
    using System.Security;
    using System.Security.Principal; 
    using System.Text;
    using System.Configuration.Assemblies; 
    using System.Runtime.Remoting; 
    using System.Runtime.InteropServices;
    using System.Threading; 
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
 
    using BOOL = System.Int32;
    using DWORD = System.UInt32; 
    using ULONG = System.UInt32; 

    /** 
     * Win32 encapsulation for MSCORLIB.
     */
    // Remove the default demands for all N/Direct methods with this
    // global declaration on the class. 
    //
    [SuppressUnmanagedCodeSecurityAttribute()] 
    internal static class Win32Native { 

#if !FEATURE_PAL 
        internal const int KEY_QUERY_VALUE        = 0x0001;
        internal const int KEY_SET_VALUE          = 0x0002;
        internal const int KEY_CREATE_SUB_KEY     = 0x0004;
        internal const int KEY_ENUMERATE_SUB_KEYS = 0x0008; 
        internal const int KEY_NOTIFY             = 0x0010;
        internal const int KEY_CREATE_LINK        = 0x0020; 
        internal const int KEY_READ               =((STANDARD_RIGHTS_READ       | 
                                                           KEY_QUERY_VALUE            |
                                                           KEY_ENUMERATE_SUB_KEYS     | 
                                                           KEY_NOTIFY)
                                                          &
                                                          (~SYNCHRONIZE));
 
        internal const int KEY_WRITE              =((STANDARD_RIGHTS_WRITE      |
                                                           KEY_SET_VALUE              | 
                                                           KEY_CREATE_SUB_KEY) 
                                                          &
                                                          (~SYNCHRONIZE)); 
        internal const int REG_NONE                    = 0;     // No value type
        internal const int REG_SZ                      = 1;     // Unicode nul terminated string
        internal const int REG_EXPAND_SZ               = 2;     // Unicode nul terminated string
        // (with environment variable references) 
        internal const int REG_BINARY                  = 3;     // Free form binary
        internal const int REG_DWORD                   = 4;     // 32-bit number 
        internal const int REG_DWORD_LITTLE_ENDIAN     = 4;     // 32-bit number (same as REG_DWORD) 
        internal const int REG_DWORD_BIG_ENDIAN        = 5;     // 32-bit number
        internal const int REG_LINK                    = 6;     // Symbolic Link (unicode) 
        internal const int REG_MULTI_SZ                = 7;     // Multiple Unicode strings
        internal const int REG_RESOURCE_LIST           = 8;     // Resource list in the resource map
        internal const int REG_FULL_RESOURCE_DESCRIPTOR  = 9;   // Resource list in the hardware description
        internal const int REG_RESOURCE_REQUIREMENTS_LIST = 10; 
        internal const int REG_QWORD                   = 11;    // 64-bit number
 
        internal const int HWND_BROADCAST              = 0xffff; 
        internal const int WM_SETTINGCHANGE            = 0x001A;
 
        // CryptProtectMemory and CryptUnprotectMemory.
        internal const uint CRYPTPROTECTMEMORY_BLOCK_SIZE    = 16;
        internal const uint CRYPTPROTECTMEMORY_SAME_PROCESS  = 0x00;
        internal const uint CRYPTPROTECTMEMORY_CROSS_PROCESS = 0x01; 
        internal const uint CRYPTPROTECTMEMORY_SAME_LOGON    = 0x02;
 
        // Security Quality of Service flags 
        internal const int SECURITY_ANONYMOUS       = ((int)SECURITY_IMPERSONATION_LEVEL.Anonymous << 16);
        internal const int SECURITY_SQOS_PRESENT    = 0x00100000; 

        // Access Control library.
        internal const string MICROSOFT_KERBEROS_NAME = "Kerberos";
        internal const uint ANONYMOUS_LOGON_LUID = 0x3e6; 

        internal const int SECURITY_ANONYMOUS_LOGON_RID    = 0x00000007; 
        internal const int SECURITY_AUTHENTICATED_USER_RID = 0x0000000B; 
        internal const int SECURITY_LOCAL_SYSTEM_RID       = 0x00000012;
        internal const int SECURITY_BUILTIN_DOMAIN_RID     = 0x00000020; 
        internal const int DOMAIN_USER_RID_GUEST           = 0x000001F5;

        internal const uint SE_PRIVILEGE_DISABLED           = 0x00000000;
        internal const uint SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001; 
        internal const uint SE_PRIVILEGE_ENABLED            = 0x00000002;
        internal const uint SE_PRIVILEGE_USED_FOR_ACCESS    = 0x80000000; 
 
        internal const uint SE_GROUP_MANDATORY          = 0x00000001;
        internal const uint SE_GROUP_ENABLED_BY_DEFAULT = 0x00000002; 
        internal const uint SE_GROUP_ENABLED            = 0x00000004;
        internal const uint SE_GROUP_OWNER              = 0x00000008;
        internal const uint SE_GROUP_USE_FOR_DENY_ONLY  = 0x00000010;
        internal const uint SE_GROUP_LOGON_ID           = 0xC0000000; 
        internal const uint SE_GROUP_RESOURCE           = 0x20000000;
 
        internal const uint DUPLICATE_CLOSE_SOURCE      = 0x00000001; 
        internal const uint DUPLICATE_SAME_ACCESS       = 0x00000002;
        internal const uint DUPLICATE_SAME_ATTRIBUTES   = 0x00000004; 
#endif

        // Win32 ACL-related constants:
        internal const int READ_CONTROL                    = 0x00020000; 
        internal const int SYNCHRONIZE                     = 0x00100000;
 
        internal const int STANDARD_RIGHTS_READ            = READ_CONTROL; 
        internal const int STANDARD_RIGHTS_WRITE           = READ_CONTROL;
 
        // STANDARD_RIGHTS_REQUIRED  (0x000F0000L)
        // SEMAPHORE_ALL_ACCESS          (STANDARD_RIGHTS_REQUIRED|SYNCHRONIZE|0x3)

        // SEMAPHORE and Event both use 0x0002 
        // MUTEX uses 0x001 (MUTANT_QUERY_STATE)
 
        // Note that you may need to specify the SYNCHRONIZE bit as well 
        // to be able to open a synchronization primitive.
        internal const int SEMAPHORE_MODIFY_STATE = 0x00000002; 
        internal const int EVENT_MODIFY_STATE     = 0x00000002;
        internal const int MUTEX_MODIFY_STATE     = 0x00000001;
        internal const int MUTEX_ALL_ACCESS       = 0x001F0001;
 

        internal const int LMEM_FIXED    = 0x0000; 
        internal const int LMEM_ZEROINIT = 0x0040; 
        internal const int LPTR          = (LMEM_FIXED | LMEM_ZEROINIT);
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        internal class OSVERSIONINFO {
            internal OSVERSIONINFO() {
                OSVersionInfoSize = (int)Marshal.SizeOf(this); 
            }
 
            // The OSVersionInfoSize field must be set to Marshal.SizeOf(this) 
            internal int OSVersionInfoSize = 0;
            internal int MajorVersion = 0; 
            internal int MinorVersion = 0;
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] 
            internal String CSDVersion = null;
        } 
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        internal class OSVERSIONINFOEX { 

            public OSVERSIONINFOEX() {
                OSVersionInfoSize = (int)Marshal.SizeOf(this);
            } 

            // The OSVersionInfoSize field must be set to Marshal.SizeOf(this) 
            internal int OSVersionInfoSize = 0; 
            internal int MajorVersion = 0;
            internal int MinorVersion = 0; 
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)]
            internal string CSDVersion = null; 
            internal ushort ServicePackMajor = 0;
            internal ushort ServicePackMinor = 0; 
            internal short SuiteMask = 0; 
            internal byte ProductType = 0;
            internal byte Reserved = 0; 
        }

        [StructLayout(LayoutKind.Sequential)]
            internal struct SYSTEM_INFO { 
            internal int dwOemId;    // This is a union of a DWORD and a struct containing 2 WORDs.
            internal int dwPageSize; 
            internal IntPtr lpMinimumApplicationAddress; 
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask; 
            internal int dwNumberOfProcessors;
            internal int dwProcessorType;
            internal int dwAllocationGranularity;
            internal short wProcessorLevel; 
            internal short wProcessorRevision;
        } 
 
        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES { 
            internal int nLength = 0;
            internal unsafe byte * pSecurityDescriptor = null;
            internal int bInheritHandle = 0;
        } 

        [StructLayout(LayoutKind.Sequential), Serializable] 
        internal struct WIN32_FILE_ATTRIBUTE_DATA { 
            internal int fileAttributes;
            internal uint ftCreationTimeLow; 
            internal uint ftCreationTimeHigh;
            internal uint ftLastAccessTimeLow;
            internal uint ftLastAccessTimeHigh;
            internal uint ftLastWriteTimeLow; 
            internal uint ftLastWriteTimeHigh;
            internal int fileSizeHigh; 
            internal int fileSizeLow; 
        }
 
        [StructLayout(LayoutKind.Sequential)]
        internal struct FILE_TIME {
            public FILE_TIME(long fileTime) {
                ftTimeLow = (uint) fileTime; 
                ftTimeHigh = (uint) (fileTime >> 32);
            } 
 
            public long ToTicks() {
                return ((long) ftTimeHigh << 32) + ftTimeLow; 
            }

            internal uint ftTimeLow;
            internal uint ftTimeHigh; 
        }
 
#if !FEATURE_PAL 

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct KERB_S4U_LOGON {
            internal uint                   MessageType;
            internal uint                   Flags;
            internal UNICODE_INTPTR_STRING  ClientUpn;   // REQUIRED: UPN for client 
            internal UNICODE_INTPTR_STRING  ClientRealm; // Optional: Client Realm, if known
        } 
 
        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct LSA_OBJECT_ATTRIBUTES { 
            internal int Length;
            internal IntPtr RootDirectory;
            internal IntPtr ObjectName;
            internal int Attributes; 
            internal IntPtr SecurityDescriptor;
            internal IntPtr SecurityQualityOfService; 
        } 

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct UNICODE_STRING {
            internal ushort Length;
            internal ushort MaximumLength;
            [MarshalAs(UnmanagedType.LPWStr)] internal string Buffer; 
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct UNICODE_INTPTR_STRING {
            internal UNICODE_INTPTR_STRING (int length, int maximumLength, IntPtr buffer) { 
                this.Length = (ushort) length;
                this.MaxLength = (ushort) maximumLength;
                this.Buffer = buffer;
            } 
            internal ushort Length;
            internal ushort MaxLength; 
            internal IntPtr Buffer; 
        }
 
        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_TRANSLATED_NAME {
            internal int Use;
            internal UNICODE_INTPTR_STRING Name; 
            internal int DomainIndex;
        } 
 
        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct LSA_TRANSLATED_SID { 
            internal int Use;
            internal uint Rid;
            internal int DomainIndex;
        } 

        [StructLayoutAttribute(LayoutKind.Sequential)] 
        internal struct LSA_TRANSLATED_SID2 { 
            internal int Use;
            internal IntPtr Sid; 
            internal int DomainIndex;
            uint Flags;
        }
 
        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_TRUST_INFORMATION { 
            internal UNICODE_INTPTR_STRING Name; 
            internal IntPtr Sid;
        } 

        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_REFERENCED_DOMAIN_LIST {
            internal int Entries; 
            internal IntPtr Domains;
        } 
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct LUID { 
            internal uint LowPart;
            internal uint HighPart;
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct LUID_AND_ATTRIBUTES { 
            internal LUID Luid; 
            internal uint Attributes;
        } 

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct QUOTA_LIMITS {
            internal IntPtr PagedPoolLimit; 
            internal IntPtr NonPagedPoolLimit;
            internal IntPtr MinimumWorkingSetSize; 
            internal IntPtr MaximumWorkingSetSize; 
            internal IntPtr PagefileLimit;
            internal IntPtr TimeLimit; 
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct SECURITY_LOGON_SESSION_DATA { 
            internal uint       Size;
            internal LUID       LogonId; 
            internal UNICODE_INTPTR_STRING UserName; 
            internal UNICODE_INTPTR_STRING LogonDomain;
            internal UNICODE_INTPTR_STRING AuthenticationPackage; 
            internal uint       LogonType;
            internal uint       Session;
            internal IntPtr     Sid;
            internal long       LogonTime; 
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct SID_AND_ATTRIBUTES {
            internal IntPtr Sid; 
            internal uint   Attributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct TOKEN_GROUPS {
            internal uint GroupCount; 
            internal SID_AND_ATTRIBUTES Groups; // SID_AND_ATTRIBUTES Groups[ANYSIZE_ARRAY]; 
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct TOKEN_PRIVILEGE {
            internal uint                PrivilegeCount;
            internal LUID_AND_ATTRIBUTES Privilege; 
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct TOKEN_SOURCE {
            private const int TOKEN_SOURCE_LENGTH = 8; 

            [MarshalAs(UnmanagedType.ByValArray, SizeConst=TOKEN_SOURCE_LENGTH)]
            internal char[] Name;
            internal LUID   SourceIdentifier; 
        }
 
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] 
        internal struct TOKEN_STATISTICS {
            internal LUID   TokenId; 
            internal LUID   AuthenticationId;
            internal long   ExpirationTime;
            internal uint   TokenType;
            internal uint   ImpersonationLevel; 
            internal uint   DynamicCharged;
            internal uint   DynamicAvailable; 
            internal uint   GroupCount; 
            internal uint   PrivilegeCount;
            internal LUID   ModifiedId; 
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct TOKEN_USER { 
            internal SID_AND_ATTRIBUTES User;
        } 
 
        [StructLayout(LayoutKind.Sequential)]
        internal class MEMORYSTATUSEX { 
            internal MEMORYSTATUSEX() {
                length = (int) Marshal.SizeOf(this);
            }
 
            // The length field must be set to the size of this data structure.
            internal int length; 
            internal int memoryLoad; 
            internal ulong totalPhys;
            internal ulong availPhys; 
            internal ulong totalPageFile;
            internal ulong availPageFile;
            internal ulong totalVirtual;
            internal ulong availVirtual; 
            internal ulong availExtendedVirtual;
        } 
 
        // Use only on Win9x
        [StructLayout(LayoutKind.Sequential)] 
        internal class MEMORYSTATUS {
            internal MEMORYSTATUS() {
                length = (int) Marshal.SizeOf(this);
            } 

            // The length field must be set to the size of this data structure. 
            internal int length; 
            internal int memoryLoad;
            internal uint totalPhys; 
            internal uint availPhys;
            internal uint totalPageFile;
            internal uint availPageFile;
            internal uint totalVirtual; 
            internal uint availVirtual;
        } 
 
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct MEMORY_BASIC_INFORMATION { 
            internal void* BaseAddress;
            internal void* AllocationBase;
            internal uint AllocationProtect;
            internal UIntPtr RegionSize; 
            internal uint State;
            internal uint Protect; 
            internal uint Type; 
        }
#endif  // !FEATURE_PAL 

#if !FEATURE_PAL
        internal const String KERNEL32 = "kernel32.dll";
        internal const String USER32   = "user32.dll"; 
        internal const String ADVAPI32 = "advapi32.dll";
        internal const String OLE32    = "ole32.dll"; 
        internal const String OLEAUT32 = "oleaut32.dll"; 
        internal const String SHFOLDER = "shfolder.dll";
        internal const String SHIM     = "mscoree.dll"; 
        internal const String CRYPT32  = "crypt32.dll";
        internal const String SECUR32  = "secur32.dll";
        internal const String MSCORWKS = "mscorwks.dll";
 
#else // !FEATURE_PAL
 
 #if !PLATFORM_UNIX 
        internal const String DLLPREFIX = "";
        internal const String DLLSUFFIX = ".dll"; 
 #else // !PLATFORM_UNIX
  #if __APPLE__
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".dylib"; 
  #elif _AIX
        internal const String DLLPREFIX = "lib"; 
        internal const String DLLSUFFIX = ".a"; 
  #elif __hppa__ || IA64
        internal const String DLLPREFIX = "lib"; 
        internal const String DLLSUFFIX = ".sl";
  #else
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".so"; 
  #endif
 #endif // !PLATFORM_UNIX 
 
        internal const String KERNEL32 = DLLPREFIX + "rotor_pal" + DLLSUFFIX;
        internal const String USER32   = DLLPREFIX + "rotor_pal" + DLLSUFFIX; 
        internal const String ADVAPI32 = DLLPREFIX + "rotor_pal" + DLLSUFFIX;
        internal const String OLE32    = DLLPREFIX + "rotor_pal" + DLLSUFFIX;
        internal const String OLEAUT32 = DLLPREFIX + "rotor_palrt" + DLLSUFFIX;
        internal const String SHIM     = DLLPREFIX + "sscoree" + DLLSUFFIX; 
        internal const String MSCORWKS = DLLPREFIX + "mscorwks" + DLLSUFFIX;
 
#endif // !FEATURE_PAL 

        internal const String LSTRCPY  = "lstrcpy"; 
        internal const String LSTRCPYN = "lstrcpyn";
        internal const String LSTRLEN  = "lstrlen";
        internal const String LSTRLENA = "lstrlenA";
        internal const String LSTRLENW = "lstrlenW"; 
        internal const String MOVEMEMORY = "RtlMoveMemory";
 
 
        // From WinBase.h
        internal const int SEM_FAILCRITICALERRORS = 1; 

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern void SetLastError(int errorCode); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool GetVersionEx([In, Out] OSVERSIONINFO ver);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetVersionEx([In, Out] OSVERSIONINFOEX ver);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int FormatMessage(int dwFlags, IntPtr lpSource,
                    int dwMessageId, int dwLanguageId, StringBuilder lpBuffer,
                    int nSize, IntPtr va_list_arguments); 

        // Gets an error message for a Win32 error code. 
        internal static String GetMessage(int errorCode) { 
            StringBuilder sb = new StringBuilder(512);
            int result = Win32Native.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS | 
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                Win32Native.NULL, errorCode, 0, sb, sb.Capacity, Win32Native.NULL);
            if (result != 0) {
                // result is the # of characters copied to the StringBuilder on NT, 
                // but on Win9x, it appears to be the number of MBCS bytes.
                // Just give up and return the String as-is... 
                String s = sb.ToString(); 
                return s;
            } 
            else {
                return Environment.GetResourceString("UnknownError_Num", errorCode);
            }
        } 

        [DllImport(KERNEL32, EntryPoint="LocalAlloc")] 
        [ResourceExposure(ResourceScope.None)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal static extern IntPtr LocalAlloc_NoSafeHandle(int uFlags, IntPtr sizetdwBytes); 

#if !FEATURE_PAL
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        SafeLocalAllocHandle LocalAlloc( 
            [In] int uFlags, 
            [In] IntPtr sizetdwBytes);
#endif // !FEATURE_PAL 

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern IntPtr LocalFree(IntPtr handle);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern void ZeroMemory(IntPtr handle, uint length);

#if !FEATURE_PAL
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer); 
 
        // Only call this on Win9x, because it doesn't handle more than 4 GB of
        // memory. 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GlobalMemoryStatus([In, Out] MEMORYSTATUS buffer);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        unsafe internal static extern IntPtr VirtualQuery(void* address, ref MEMORY_BASIC_INFORMATION buffer, IntPtr sizeOfBuffer); 

        // VirtualAlloc should generally be avoided, but is needed in 
        // the MemoryFailPoint implementation (within a CER) to increase the
        // size of the page file, ignoring any host memory allocators.
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        unsafe internal static extern void * VirtualAlloc(void* address, UIntPtr numBytes, int commitOrReserve, int pageProtectionMode); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        unsafe internal static extern bool VirtualFree(void* address, UIntPtr numBytes, int pageFreeMode);

#if IO_CANCELLATION_ENABLED 
        // Note - do NOT use this to call methods.  Use P/Invoke, which will
        // do much better things w.r.t. marshaling, pinning memory, security 
        // stuff, better interactions with thread aborts, etc.  I'm defining 
        // this solely to detect whether certain Longhorn builds define a
        // method, to detect whether a feature exists (no, I can't use version 
        // numbers easily yet - Longhorn code merge integration issues).
        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, String methodName); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false, SetLastError=true, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Process)]  // Is your module side-by-side? 
        internal static extern IntPtr GetModuleHandle(String moduleName);
#endif // IO_CANCELLATION_ENABLED 
#endif // !FEATURE_PAL

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern uint GetTempPath(int bufferLen, StringBuilder buffer);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, EntryPoint=LSTRCPY, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern IntPtr lstrcpy(IntPtr dst, String src); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, EntryPoint=LSTRCPY, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern IntPtr lstrcpy(StringBuilder dst, IntPtr src); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, EntryPoint=LSTRLEN)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int lstrlen(sbyte [] ptr);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, EntryPoint=LSTRLEN)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int lstrlen(IntPtr ptr);
 
        [DllImport(KERNEL32, CharSet=CharSet.Ansi, EntryPoint=LSTRLENA)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int lstrlenA(IntPtr ptr); 

        [DllImport(KERNEL32, CharSet=CharSet.Unicode, EntryPoint=LSTRLENW)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int lstrlenW(IntPtr ptr);

        [DllImport(Win32Native.OLEAUT32, CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        internal static extern IntPtr SysAllocStringLen(String src, int len);  // BSTR 

        [DllImport(Win32Native.OLEAUT32)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int SysStringLen(IntPtr bstr);
 
        [DllImport(Win32Native.OLEAUT32)]
        [ResourceExposure(ResourceScope.None)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern void SysFreeString(IntPtr bstr);
 
        [DllImport(KERNEL32, CharSet=CharSet.Unicode, EntryPoint=MOVEMEMORY)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern void CopyMemoryUni(IntPtr pdst, String psrc, IntPtr sizetcb);
 
        [DllImport(KERNEL32, CharSet=CharSet.Unicode, EntryPoint=MOVEMEMORY)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern void CopyMemoryUni(StringBuilder pdst, 
                    IntPtr psrc, IntPtr sizetcb);
 
        [DllImport(KERNEL32, CharSet=CharSet.Ansi, EntryPoint=MOVEMEMORY, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern void CopyMemoryAnsi(IntPtr pdst, String psrc, IntPtr sizetcb);
 
        [DllImport(KERNEL32, CharSet=CharSet.Ansi, EntryPoint=MOVEMEMORY, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern void CopyMemoryAnsi(StringBuilder pdst, 
                    IntPtr psrc, IntPtr sizetcb);
 

        [DllImport(KERNEL32)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int GetACP(); 

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool SetEvent(SafeWaitHandle handle);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool ResetEvent(SafeWaitHandle handle);
 
        //Do not use this method. Call the managed WaitHandle.WaitAny/WaitAll as you have to deal with
        //COM STA issues, thread aborts among many other things. This method was added to get around 
        //an OS issue regarding named mutexes, and we guarantee that we never block when calling this 
        //method directly.
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD WaitForMultipleObjects(DWORD nCount,IntPtr[] handles, bool bWaitAll, DWORD dwMilliseconds);

 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] // Machine or none based on the value of "name" 
        internal static extern SafeWaitHandle CreateEvent(SECURITY_ATTRIBUTES lpSecurityAttributes, bool isManualReset, bool initialState, String name); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern SafeWaitHandle OpenEvent(/* DWORD */ int desiredAccess, bool inheritHandle, String name);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        internal static extern SafeWaitHandle CreateMutex(SECURITY_ATTRIBUTES lpSecurityAttributes, bool initialOwner, String name); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern SafeWaitHandle OpenMutex(/* DWORD */ int desiredAccess, bool inheritHandle, String name);

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        internal static extern bool ReleaseMutex(SafeWaitHandle handle); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int GetFullPathName([In] char[] path, int numBufferChars, [Out] char[] buffer, IntPtr mustBeZero);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal unsafe static extern int GetFullPathName(char* path, int numBufferChars, char* buffer, IntPtr mustBeZero); 
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetLongPathName(String path, StringBuilder longPathBuffer, int bufferLength);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetLongPathName([In] char[] path, [Out] char [] longPathBuffer, int bufferLength);
 
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal unsafe static extern int GetLongPathName(char* path, char* longPathBuffer, int bufferLength);

        // Disallow access to all non-file devices from methods that take
        // a String.  This disallows DOS devices like "con:", "com1:", 
        // "lpt1:", etc.  Use this to avoid security problems, like allowing
        // a web client asking a server for "http://server/com1.aspx" and 
        // then causing a worker process to hang. 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        internal static SafeFileHandle SafeCreateFile(String lpFileName,
                    int dwDesiredAccess, System.IO.FileShare dwShareMode,
                    SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile) 
        {
            SafeFileHandle handle = CreateFile( lpFileName, dwDesiredAccess, dwShareMode, 
                                securityAttrs, dwCreationDisposition, 
                                dwFlagsAndAttributes, hTemplateFile );
 
            if (!handle.IsInvalid)
            {
                int fileType = Win32Native.GetFileType(handle);
                if (fileType != Win32Native.FILE_TYPE_DISK) { 
                    handle.Dispose();
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_FileStreamOnNonFiles")); 
                } 
            }
 
            return handle;
        }

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        internal static SafeFileHandle UnsafeCreateFile(String lpFileName, 
                    int dwDesiredAccess, System.IO.FileShare dwShareMode, 
                    SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile) 
        {
            SafeFileHandle handle = CreateFile( lpFileName, dwDesiredAccess, dwShareMode,
                                securityAttrs, dwCreationDisposition,
                                dwFlagsAndAttributes, hTemplateFile ); 

            return handle; 
        } 

        // Do not use these directly, use the safe or unsafe versions above. 
        // The safe version does not support devices (aka if will only open
        // files on disk), while the unsafe version give you the full semantic
        // of the native version.
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        private static extern SafeFileHandle CreateFile(String lpFileName, 
                    int dwDesiredAccess, System.IO.FileShare dwShareMode, 
                    SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern SafeFileMappingHandle CreateFileMapping(SafeFileHandle hFile, IntPtr lpAttributes, uint fProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, String lpName); 

        [DllImport(KERNEL32, SetLastError=true, ExactSpelling=true)] 
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern IntPtr MapViewOfFile(
            SafeFileMappingHandle handle, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumerOfBytesToMap); 

        [DllImport(KERNEL32, ExactSpelling=true)]
        [ResourceExposure(ResourceScope.Machine)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress );
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport(KERNEL32)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int GetFileType(SafeFileHandle handle);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool SetEndOfFile(SafeFileHandle hFile); 

        [DllImport(KERNEL32, SetLastError=true, EntryPoint="SetFilePointer")]
        [ResourceExposure(ResourceScope.None)]
        private unsafe static extern int SetFilePointerWin32(SafeFileHandle handle, int lo, int * hi, int origin); 

        [ResourceExposure(ResourceScope.None)] 
        internal unsafe static long SetFilePointer(SafeFileHandle handle, long offset, System.IO.SeekOrigin origin, out int hr) { 
            hr = 0;
            int lo = (int) offset; 
            int hi = (int) (offset >> 32);
            lo = SetFilePointerWin32(handle, lo, &hi, (int) origin);

            if (lo == -1 && ((hr = Marshal.GetLastWin32Error()) != 0)) 
                return -1;
            return (long) (((ulong) ((uint) hi)) << 32) | ((uint) lo); 
        } 

        // Note there are two different ReadFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in
        // Win32's async IO support.  You can't do the following three things
        // simultaneously: overlapped IO, free the memory for the overlapped
        // struct in a callback (or an EndRead method called by that callback), 
        // and pass in an address for the numBytesRead parameter.
        // <STRIP> See Windows Bug 105512 for details.  -- </STRIP> 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        unsafe internal static extern int ReadFile(SafeFileHandle handle, byte* bytes, int numBytesToRead, IntPtr numBytesRead_mustBeZero, NativeOverlapped* overlapped);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        unsafe internal static extern int ReadFile(SafeFileHandle handle, byte* bytes, int numBytesToRead, out int numBytesRead, IntPtr mustBeZero);
 
        // Note there are two different WriteFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in
        // Win32's async IO support.  You can't do the following three things 
        // simultaneously: overlapped IO, free the memory for the overlapped
        // struct in a callback (or an EndWrite method called by that callback),
        // and pass in an address for the numBytesRead parameter.
        // <STRIP> See Windows Bug 105512 for details.  -- </STRIP> 

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, IntPtr numBytesWritten_mustBeZero, NativeOverlapped* lpOverlapped);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);
 
#if IO_CANCELLATION_ENABLED
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool CancelSynchronousIo(IntPtr threadHandle);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)]
        internal static unsafe extern bool CancelIoEx(SafeFileHandle handle, NativeOverlapped* lpOverlapped);
#endif 

        // NOTE: The out parameters are PULARGE_INTEGERs and may require 
        // some byte munging magic. 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool GetDiskFreeSpaceEx(String drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int GetDriveType(String drive);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetVolumeInformation(String drive, StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, StringBuilder fileSystemName, int fileSystemNameBufLen); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool SetVolumeLabel(String driveLetter, String volumeName); 

#if !FEATURE_PAL 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int GetWindowsDirectory(StringBuilder sb, int length); 

        // In winnls.h
        internal const int LCMAP_SORTKEY    = 0x00000400;
 
        // Used for synthetic cultures, which are not currently supported in ROTOR.
        [DllImport(KERNEL32, CharSet=CharSet.Unicode, ExactSpelling=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static unsafe extern int LCMapStringW(int lcid, int flags, char *src, int cchSrc, char *target, int cchTarget);
 
        // Will be in winnls.h
        internal const int FIND_STARTSWITH  = 0x00100000; // see if value is at the beginning of source
        internal const int FIND_ENDSWITH    = 0x00200000; // see if value is at the end of source
        internal const int FIND_FROMSTART   = 0x00400000; // look for value in source, starting at the beginning 
        internal const int FIND_FROMEND     = 0x00800000; // look for value in source, starting at the end
 
 
        // Used for synthetic cultures, which are not currently supported in ROTOR (neither is this entry).
        // Last parameter is an LPINT which on success returns the length of the string found. Since that value 
        // is not used and NULL needs to be passed, it is defined as an IntPtr rather than an out int.
        [DllImport(KERNEL32, CharSet=CharSet.Unicode, ExactSpelling=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static unsafe extern int FindNLSString(int Locale, int dwFindFlags, char *lpStringSource, int cchSource, char *lpStringValue, int cchValue, IntPtr pcchFound); // , out int pcchFound); 
#endif
 
#if !FEATURE_PAL 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetSystemDirectory(StringBuilder sb, int length);
#else
        [DllImport(KERNEL32, CharSet=CharSet.Unicode, SetLastError=true, EntryPoint="PAL_GetPALDirectoryW", BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetSystemDirectory(StringBuilder sb, int length);
 
        [DllImport(OLEAUT32, CharSet=CharSet.Unicode, SetLastError=true, EntryPoint="PAL_FetchConfigurationStringW")] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool FetchConfigurationString(bool perMachine, String parameterName, StringBuilder parameterValue, int parameterValueLength); 
#endif // !FEATURE_PAL

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal unsafe static extern bool SetFileTime(SafeFileHandle hFile, FILE_TIME* creationTime,
                    FILE_TIME* lastAccessTime, FILE_TIME* lastWriteTime); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int GetFileSize(SafeFileHandle hFile, out int highSize);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool LockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool UnlockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh); 

        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h
        internal static readonly IntPtr NULL = IntPtr.Zero;
 
        // Note, these are #defines used to extract handles, and are NOT handles.
        internal const int STD_INPUT_HANDLE = -10; 
        internal const int STD_OUTPUT_HANDLE = -11; 
        internal const int STD_ERROR_HANDLE = -12;
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);  // param is NOT a handle, but it returns one!
 
        // From wincon.h
        internal const int CTRL_C_EVENT = 0; 
        internal const int CTRL_BREAK_EVENT = 1; 
        internal const int CTRL_CLOSE_EVENT = 2;
        internal const int CTRL_LOGOFF_EVENT = 5; 
        internal const int CTRL_SHUTDOWN_EVENT = 6;
        internal const short KEY_EVENT = 1;

        // From WinBase.h 
        internal const int FILE_TYPE_DISK = 0x0001;
        internal const int FILE_TYPE_CHAR = 0x0002; 
        internal const int FILE_TYPE_PIPE = 0x0003; 

        internal const int REPLACEFILE_WRITE_THROUGH = 0x1; 
        internal const int REPLACEFILE_IGNORE_MERGE_ERRORS = 0x2;

        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM    = 0x00001000; 
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
 
        // Constants from WinNT.h 
        internal const int FILE_ATTRIBUTE_READONLY      = 0x00000001;
        internal const int FILE_ATTRIBUTE_DIRECTORY     = 0x00000010; 
        internal const int FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;

        internal const int IO_REPARSE_TAG_MOUNT_POINT = unchecked((int)0xA0000003);
 
        internal const int PAGE_READWRITE = 0x04;
 
        internal const int MEM_COMMIT  =  0x1000; 
        internal const int MEM_RESERVE =  0x2000;
        internal const int MEM_RELEASE =  0x8000; 
        internal const int MEM_FREE    = 0x10000;

        // Error codes from WinError.h
        internal const int ERROR_SUCCESS = 0x0; 
        internal const int ERROR_INVALID_FUNCTION = 0x1;
        internal const int ERROR_FILE_NOT_FOUND = 0x2; 
        internal const int ERROR_PATH_NOT_FOUND = 0x3; 
        internal const int ERROR_ACCESS_DENIED  = 0x5;
        internal const int ERROR_INVALID_HANDLE = 0x6; 
        internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
        internal const int ERROR_INVALID_DATA = 0xd;
        internal const int ERROR_INVALID_DRIVE = 0xf;
        internal const int ERROR_NO_MORE_FILES = 0x12; 
        internal const int ERROR_NOT_READY = 0x15;
        internal const int ERROR_BAD_LENGTH = 0x18; 
        internal const int ERROR_SHARING_VIOLATION = 0x20; 
        internal const int ERROR_NOT_SUPPORTED = 0x32;
        internal const int ERROR_FILE_EXISTS = 0x50; 
        internal const int ERROR_INVALID_PARAMETER = 0x57;
        internal const int ERROR_CALL_NOT_IMPLEMENTED = 0x78;
        internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
        internal const int ERROR_INVALID_NAME = 0x7B; 
        internal const int ERROR_BAD_PATHNAME = 0xA1;
        internal const int ERROR_ALREADY_EXISTS = 0xB7; 
        internal const int ERROR_ENVVAR_NOT_FOUND = 0xCB; 
        internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
        internal const int ERROR_NO_DATA = 0xE8; 
        internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;
        internal const int ERROR_MORE_DATA = 0xEA;
        internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
        internal const int ERROR_NO_TOKEN = 0x3f0; 
        internal const int ERROR_DLL_INIT_FAILED = 0x45A;
        internal const int ERROR_NON_ACCOUNT_SID = 0x4E9; 
        internal const int ERROR_NOT_ALL_ASSIGNED = 0x514; 
        internal const int ERROR_UNKNOWN_REVISION = 0x519;
        internal const int ERROR_INVALID_OWNER = 0x51B; 
        internal const int ERROR_INVALID_PRIMARY_GROUP = 0x51C;
        internal const int ERROR_NO_SUCH_PRIVILEGE = 0x521;
        internal const int ERROR_PRIVILEGE_NOT_HELD = 0x522;
        internal const int ERROR_NONE_MAPPED = 0x534; 
        internal const int ERROR_INVALID_ACL = 0x538;
        internal const int ERROR_INVALID_SID = 0x539; 
        internal const int ERROR_INVALID_SECURITY_DESCR = 0x53A; 
        internal const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;
        internal const int ERROR_CANT_OPEN_ANONYMOUS = 0x543; 
        internal const int ERROR_NO_SECURITY_ON_OBJECT = 0x546;
        internal const int ERROR_TRUSTED_RELATIONSHIP_FAILURE = 0x6FD;

        // Error codes from ntstatus.h 
        internal const uint STATUS_SUCCESS = 0x00000000;
        internal const uint STATUS_SOME_NOT_MAPPED = 0x00000107; 
        internal const uint STATUS_NO_MEMORY = 0xC0000017; 
        internal const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;
        internal const uint STATUS_NONE_MAPPED = 0xC0000073; 
        internal const uint STATUS_INSUFFICIENT_RESOURCES = 0xC000009A;
        internal const uint STATUS_ACCESS_DENIED = 0xC0000022;

        internal const int INVALID_FILE_SIZE     = -1; 

        // From WinStatus.h 
        internal const int STATUS_ACCOUNT_RESTRICTION = unchecked((int) 0xC000006E); 

        // Use this to translate error codes like the above into HRESULTs like 
        // 0x80070006 for ERROR_INVALID_HANDLE
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            BCLDebug.Assert((0xFFFF0000 & errorCode) == 0, "This is an HRESULT, not an error code!"); 
            return unchecked(((int)0x80070000) | errorCode);
        } 
 
        // Win32 Structs in N/Direct style
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto), Serializable] 
        [BestFitMapping(false)]
        internal class WIN32_FIND_DATA {
            internal int  dwFileAttributes = 0;
            // ftCreationTime was a by-value FILETIME structure 
            internal int  ftCreationTime_dwLowDateTime = 0 ;
            internal int  ftCreationTime_dwHighDateTime = 0; 
            // ftLastAccessTime was a by-value FILETIME structure 
            internal int  ftLastAccessTime_dwLowDateTime = 0;
            internal int  ftLastAccessTime_dwHighDateTime = 0; 
            // ftLastWriteTime was a by-value FILETIME structure
            internal int  ftLastWriteTime_dwLowDateTime = 0;
            internal int  ftLastWriteTime_dwHighDateTime = 0;
            internal int  nFileSizeHigh = 0; 
            internal int  nFileSizeLow = 0;
            // If the file attributes' reparse point flag is set, then 
            // dwReserved0 is the file tag (aka reparse tag) for the 
            // reparse point.  Use this to figure out whether something is
            // a volume mount point or a symbolic link. 
            internal int  dwReserved0 = 0;
            internal int  dwReserved1 = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
            internal String   cFileName = null; 
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=14)]
            internal String   cAlternateFileName = null; 
        } 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool CopyFile(
                    String src, String dst, bool failIfExists);
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern bool CreateDirectory( 
                    String path, SECURITY_ATTRIBUTES lpSecurityAttributes);
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool DeleteFile(String path);
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern bool ReplaceFile(String replacedFileName, String replacementFileName, String backupFileName, int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved); 

        [DllImport(ADVAPI32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool DecryptFile(String path, int reservedMustBeZero);

        [DllImport(ADVAPI32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool EncryptFile(String path); 
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern SafeFindHandle FindFirstFile(String fileName, [In, Out] Win32Native.WIN32_FIND_DATA data);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool FindNextFile(
                    SafeFindHandle hndFindFile, 
                    [In, Out, MarshalAs(UnmanagedType.LPStruct)] 
                    WIN32_FIND_DATA lpFindFileData);
 
        [DllImport(KERNEL32)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern bool FindClose(IntPtr handle); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetCurrentDirectory(
                  int nBufferLength, 
                  StringBuilder lpBuffer);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool GetFileAttributesEx(String name, int fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool SetFileAttributes(String name, int attr); 

#if !PLATFORM_UNIX
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int GetLogicalDrives();
#endif // !PLATFORM_UNIX 
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern uint GetTempFileName(String tmpPath, String prefix, uint uniqueIdOrZero, StringBuilder tmpFileName);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern bool MoveFile(String src, String dst);
 
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool DeleteVolumeMountPoint(String mountPoint); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern bool RemoveDirectory(String path); 

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern bool SetCurrentDirectory(String path);
 
        [DllImport(KERNEL32, SetLastError=false)]
        [ResourceExposure(ResourceScope.Process)]
        internal static extern int SetErrorMode(int newMode);
 
        internal const int LCID_SUPPORTED = 0x00000002;  // supported locale ids
 
        [DllImport(KERNEL32)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern unsafe int WideCharToMultiByte(uint cp, uint flags, char* pwzSource, int cchSource, byte* pbDestBuffer, int cbDestBuffer, IntPtr null1, IntPtr null2); 

        // A Win32 HandlerRoutine
        internal delegate bool ConsoleCtrlHandlerRoutine(int controlType);
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine handler, bool addOrRemove);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int GetEnvironmentVariable(string lpName, StringBuilder lpValue, int size); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static extern uint GetCurrentProcessId();

        [DllImport(ADVAPI32, CharSet=CharSet.Auto)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetUserName(StringBuilder lpBuffer, ref int nSize); 
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal extern static int GetComputerName(StringBuilder nameBuffer, ref int bufferSize); 

#if FEATURE_COMINTEROP
        [DllImport(Win32Native.OLE32)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern IntPtr CoTaskMemAlloc(int cb);
 
        [DllImport(Win32Native.OLE32)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern IntPtr CoTaskMemRealloc(IntPtr pv, int cb); 

        [DllImport(Win32Native.OLE32)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern void CoTaskMemFree(IntPtr ptr); 
#endif // FEATURE_COMINTEROP
 
#if !FEATURE_PAL 
        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct COORD 
        {
            internal short X;
            internal short Y;
        } 

        [StructLayoutAttribute(LayoutKind.Sequential)] 
        internal struct SMALL_RECT 
        {
            internal short Left; 
            internal short Top;
            internal short Right;
            internal short Bottom;
        } 

        [StructLayoutAttribute(LayoutKind.Sequential)] 
        internal struct CONSOLE_SCREEN_BUFFER_INFO 
        {
            internal COORD      dwSize; 
            internal COORD      dwCursorPosition;
            internal short      wAttributes;
            internal SMALL_RECT srWindow;
            internal COORD      dwMaximumWindowSize; 
        }
 
        [StructLayoutAttribute(LayoutKind.Sequential)] 
        internal struct CONSOLE_CURSOR_INFO
        { 
            internal int dwSize;
            internal bool bVisible;
        }
 
        // Win32's KEY_EVENT_RECORD
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)] 
        internal struct KeyEventRecord 
        {
            internal bool keyDown; 
            internal short repeatCount;
            internal short virtualKeyCode;
            internal short virtualScanCode;
            internal char uChar; 
            internal int controlKeyState;
        } 
 
        // Really, this is a union of KeyEventRecords and other types.
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)] 
        internal struct InputRecord
        {
            internal short eventType;
            // This is a union!  Must make sure INPUT_RECORD's size matches this. 
            // However, KEY_EVENT_RECORD is the largest part of the union.
            internal KeyEventRecord keyEvent; 
        } 

        [Flags, Serializable] 
        internal enum Color : short
        {
            Black = 0,
            ForegroundBlue = 0x1, 
            ForegroundGreen = 0x2,
            ForegroundRed = 0x4, 
            ForegroundYellow = 0x6, 
            ForegroundIntensity = 0x8,
            BackgroundBlue = 0x10, 
            BackgroundGreen = 0x20,
            BackgroundRed = 0x40,
            BackgroundYellow = 0x60,
            BackgroundIntensity = 0x80, 

            ForegroundMask = 0xf, 
            BackgroundMask = 0xf0, 
            ColorMask = 0xff
        } 

        [StructLayout(LayoutKind.Sequential)]
        internal struct CHAR_INFO
        { 
            ushort charData;  // Union between WCHAR and ASCII char
            short attributes; 
        } 

        internal const int ENABLE_PROCESSED_INPUT  = 0x0001; 
        internal const int ENABLE_LINE_INPUT  = 0x0002;
        internal const int ENABLE_ECHO_INPUT  = 0x0004;

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int mode);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool Beep(int frequency, int duration);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, 
            out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, COORD size);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern COORD GetLargestConsoleWindowSize(IntPtr hConsoleOutput); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool FillConsoleOutputCharacter(IntPtr hConsoleOutput, 
            char character, int nLength, COORD dwWriteCoord, out int pNumCharsWritten);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool FillConsoleOutputAttribute(IntPtr hConsoleOutput, 
            short wColorAttribute, int numCells, COORD startCoord, out int pNumBytesWritten);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static unsafe extern bool SetConsoleWindowInfo(IntPtr hConsoleOutput,
            bool absolute, SMALL_RECT* consoleWindow); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, short attributes);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput,
            COORD cursorPosition); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern bool GetConsoleCursorInfo(IntPtr hConsoleOutput,
            out CONSOLE_CURSOR_INFO cci);

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool SetConsoleCursorInfo(IntPtr hConsoleOutput, 
            ref CONSOLE_CURSOR_INFO cci); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int GetConsoleTitle(StringBuilder sb, int capacity);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static extern bool SetConsoleTitle(String title); 
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool ReadConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool PeekConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);
 
        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)]
        internal static unsafe extern bool ReadConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO* pBuffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT readRegion); 

        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)]
        internal static unsafe extern bool WriteConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO* buffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT writeRegion); 

        [DllImport(USER32)]  // Appears to always succeed 
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern short GetKeyState(int virtualKeyCode);
#endif // !FEATURE_PAL 

        [DllImport(KERNEL32, SetLastError=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern uint GetConsoleCP(); 

        [DllImport(KERNEL32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleCP(uint codePage);
 
        [DllImport(KERNEL32, SetLastError=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern uint GetConsoleOutputCP();
 
        [DllImport(KERNEL32, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern bool SetConsoleOutputCP(uint codePage); 

#if !FEATURE_PAL 
        internal const int VER_PLATFORM_WIN32s = 0;
        internal const int VER_PLATFORM_WIN32_WINDOWS = 1;
        internal const int VER_PLATFORM_WIN32_NT = 2;
        internal const int VER_PLATFORM_WINCE = 3; 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int RegConnectRegistry(String machineName,
                    SafeRegistryHandle key, out SafeRegistryHandle result); 

        // Note: RegCreateKeyEx won't set the last error on failure - it returns
        // an error code if it fails.
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int RegCreateKeyEx(SafeRegistryHandle hKey, String lpSubKey, 
                    int Reserved, String lpClass, int dwOptions, 
                    int samDesigner, SECURITY_ATTRIBUTES lpSecurityAttributes,
                    out SafeRegistryHandle hkResult, out int lpdwDisposition); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int RegDeleteKey(SafeRegistryHandle hKey, String lpSubKey); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int RegDeleteValue(SafeRegistryHandle hKey, String lpValueName);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegEnumKeyEx(SafeRegistryHandle hKey, int dwIndex,
                    StringBuilder lpName, out int lpcbName, int[] lpReserved, 
                    StringBuilder lpClass, int[] lpcbClass,
                    long[] lpftLastWriteTime); 
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegEnumValue(SafeRegistryHandle hKey, int dwIndex,
                    StringBuilder lpValueName, ref int lpcbValueName,
                    IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData,
                    int[] lpcbData); 

        [DllImport(ADVAPI32, CharSet=CharSet.Ansi, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegEnumValueA(SafeRegistryHandle hKey, int dwIndex,
                     StringBuilder lpValueName, ref int lpcbValueName, 
                     IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData,
                     int[] lpcbData);

 
        [DllImport(ADVAPI32)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegFlushKey(SafeRegistryHandle hKey); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int RegOpenKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegQueryInfoKey(SafeRegistryHandle hKey, StringBuilder lpClass, 
                    int[] lpcbClass, IntPtr lpReserved_MustBeZero, ref int lpcSubKeys,
                    int[] lpcbMaxSubKeyLen, int[] lpcbMaxClassLen, 
                    ref int lpcValues, int[] lpcbMaxValueNameLen,
                    int[] lpcbMaxValueLen, int[] lpcbSecurityDescriptor,
                    int[] lpftLastWriteTime);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, 
                    int[] lpReserved, ref int lpType, [Out] byte[] lpData,
                    ref int lpcbData); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, 
                    int[] lpReserved, ref int lpType, ref int lpData,
                    ref int lpcbData); 
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, ref long lpData,
                    ref int lpcbData);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, 
                     int[] lpReserved, ref int lpType, [Out] char[] lpData,
                     ref int lpcbData); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, 
                    int[] lpReserved, ref int lpType, StringBuilder lpData,
                    ref int lpcbData); 
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, byte[] lpData, int cbData);

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName, 
                    int Reserved, RegistryValueKind dwType, ref int lpData, int cbData); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, ref long lpData, int cbData);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName, 
                    int Reserved, RegistryValueKind dwType, String lpData, int cbData);
 
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int ExpandEnvironmentStrings(String lpSrc, StringBuilder lpDst, int nSize);
 
        [DllImport(KERNEL32)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern IntPtr LocalReAlloc(IntPtr handle, IntPtr sizetcbBytes, int uFlags); 

        internal const int SHGFP_TYPE_CURRENT               = 0;             // the current (user) folder path setting 
        internal const int UOI_FLAGS                        = 1;
        internal const int WSF_VISIBLE                      = 1;
        internal const int CSIDL_APPDATA                    = 0x001a;
        internal const int CSIDL_COMMON_APPDATA             = 0x0023; 
        internal const int CSIDL_LOCAL_APPDATA              = 0x001c;
        internal const int CSIDL_COOKIES                    = 0x0021; 
        internal const int CSIDL_FAVORITES                  = 0x0006; 
        internal const int CSIDL_HISTORY                    = 0x0022;
        internal const int CSIDL_INTERNET_CACHE             = 0x0020; 
        internal const int CSIDL_PROGRAMS                   = 0x0002;
        internal const int CSIDL_RECENT                     = 0x0008;
        internal const int CSIDL_SENDTO                     = 0x0009;
        internal const int CSIDL_STARTMENU                  = 0x000b; 
        internal const int CSIDL_STARTUP                    = 0x0007;
        internal const int CSIDL_SYSTEM                     = 0x0025; 
        internal const int CSIDL_TEMPLATES                  = 0x0015; 
        internal const int CSIDL_DESKTOPDIRECTORY           = 0x0010;
        internal const int CSIDL_PERSONAL                   = 0x0005; 
        internal const int CSIDL_PROGRAM_FILES              = 0x0026;
        internal const int CSIDL_PROGRAM_FILES_COMMON       = 0x002b;
        internal const int CSIDL_DESKTOP                    = 0x0000;
        internal const int CSIDL_DRIVES                     = 0x0011; 
        internal const int CSIDL_MYMUSIC                    = 0x000d;
        internal const int CSIDL_MYPICTURES                 = 0x0027; 
 
        [DllImport(SHFOLDER, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.Machine)] 
        internal static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, int dwFlags, StringBuilder lpszPath);

        internal const int NameSamCompatible = 2;
 
        [ResourceExposure(ResourceScope.None)]
        [DllImport(SECUR32, CharSet=CharSet.Unicode, SetLastError=true)] 
        // Win32 return type is BOOLEAN (which is 1 byte and not BOOL which is 4bytes) 
        internal static extern byte GetUserNameEx(int format, StringBuilder domainName, ref int domainNameLen);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool LookupAccountName(string machineName, string accountName, byte[] sid,
                                 ref int sidLen, StringBuilder domainName, ref int domainNameLen, out int peUse); 

        // Note: This returns a handle, but it shouldn't be closed.  The Avalon 
        // team says CloseWindowStation would ignore this handle.  So there 
        // isn't a lot of value to switching to SafeHandle here.
        [DllImport(USER32, ExactSpelling=true)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern IntPtr GetProcessWindowStation();

        [DllImport(USER32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex, 
            [MarshalAs(UnmanagedType.LPStruct)] USEROBJECTFLAGS pvBuffer, int nLength, ref int lpnLengthNeeded); 

        [DllImport(USER32, SetLastError=true, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, String lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult);

        [StructLayout(LayoutKind.Sequential)] 
        internal class USEROBJECTFLAGS {
            internal int fInherit = 0; 
            internal int fReserved = 0; 
            internal int dwFlags = 0;
        } 

        //
        // DPAPI
        // 

        // 
        // RtlEncryptMemory and RtlDecryptMemory are declared in the internal header file crypt.h. 
        // They were also recently declared in the public header file ntsecapi.h (in the Platform SDK as well as the current build of Server 2003).
        // We use them instead of CryptProtectMemory and CryptUnprotectMemory because 
        // they are available in both WinXP and in Windows Server 2003.
        //

        [DllImport(Win32Native.ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern 
        int SystemFunction040 (
            [In,Out] SafeBSTRHandle     pDataIn, 
            [In]     uint       cbDataIn,   // multiple of RTL_ENCRYPT_MEMORY_SIZE
            [In]     uint       dwFlags);

        [DllImport(Win32Native.ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern 
        int SystemFunction041 ( 
            [In,Out] SafeBSTRHandle     pDataIn,
            [In]     uint       cbDataIn,   // multiple of RTL_ENCRYPT_MEMORY_SIZE 
            [In]     uint       dwFlags);

        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        int LsaNtStatusToWinError ( 
            [In]    int         status); 

#if !FEATURE_PAL 
        // Get the current FIPS policy setting on Vista and above
        [DllImport("bcrypt.dll")]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern uint BCryptGetFipsAlgorithmMode( 
                [MarshalAs(UnmanagedType.U1), Out]out bool pfEnabled);
#endif 
 
        //
        // Managed ACLs 
        //

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern 
        bool AdjustTokenPrivileges ( 
            [In]     SafeTokenHandle       TokenHandle,
            [In]     bool                  DisableAllPrivileges, 
            [In]     ref TOKEN_PRIVILEGE   NewState,
            [In]     uint                  BufferLength,
            [In,Out] ref TOKEN_PRIVILEGE   PreviousState,
            [In,Out] ref uint              ReturnLength); 

        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        bool AllocateLocallyUniqueId( 
            [In,Out] ref LUID              Luid);

        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        bool CheckTokenMembership( 
            [In]     SafeTokenHandle TokenHandle, 
            [In]     byte[]          SidToCheck,
            [In,Out] ref bool        IsMember); 

        [DllImport(
             ADVAPI32,
             EntryPoint="ConvertSecurityDescriptorToStringSecurityDescriptorW", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern BOOL ConvertSdToStringSd( 
            byte[] securityDescriptor,
            DWORD requestedRevision,
            ULONG securityInformation,
            out IntPtr resultString, 
            ref ULONG resultStringLength );
 
        [DllImport( 
             ADVAPI32,
             EntryPoint="ConvertStringSecurityDescriptorToSecurityDescriptorW", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern BOOL ConvertStringSdToSd(
            string stringSd, 
            DWORD stringSdRevision, 
            out IntPtr resultSd,
            ref ULONG resultSdLength ); 

        [DllImport(
             ADVAPI32,
             EntryPoint="ConvertStringSidToSidW", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern BOOL ConvertStringSidToSid( 
            string stringSid,
            out IntPtr ByteArray
            );
 
        [DllImport(
             ADVAPI32, 
             EntryPoint="CreateWellKnownSid", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern BOOL CreateWellKnownSid(
            int sidType, 
            byte[] domainSid,
            [Out] byte[] resultSid, 
            ref DWORD resultSidLength ); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern
        bool DuplicateHandle (
            [In]     IntPtr                     hSourceProcessHandle, 
            [In]     IntPtr                     hSourceHandle,
            [In]     IntPtr                     hTargetProcessHandle, 
            [In,Out] ref SafeTokenHandle        lpTargetHandle, 
            [In]     uint                       dwDesiredAccess,
            [In]     bool                       bInheritHandle, 
            [In]     uint                       dwOptions);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern 
        bool DuplicateHandle ( 
            [In]     IntPtr                     hSourceProcessHandle,
            [In]     SafeTokenHandle            hSourceHandle, 
            [In]     IntPtr                     hTargetProcessHandle,
            [In,Out] ref SafeTokenHandle        lpTargetHandle,
            [In]     uint                       dwDesiredAccess,
            [In]     bool                       bInheritHandle, 
            [In]     uint                       dwOptions);
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        bool DuplicateTokenEx (
            [In]     SafeTokenHandle             ExistingTokenHandle,
            [In]     TokenAccessLevels           DesiredAccess, 
            [In]     IntPtr                      TokenAttributes,
            [In]     SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, 
            [In]     System.Security.Principal.TokenType TokenType, 
            [In,Out] ref SafeTokenHandle         DuplicateTokenHandle );
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern
        bool DuplicateTokenEx ( 
            [In]     SafeTokenHandle            hExistingToken,
            [In]     uint                       dwDesiredAccess, 
            [In]     IntPtr                     lpTokenAttributes,   // LPSECURITY_ATTRIBUTES 
            [In]     uint                       ImpersonationLevel,
            [In]     uint                       TokenType, 
            [In,Out] ref SafeTokenHandle        phNewToken);

        [DllImport(
             ADVAPI32, 
             EntryPoint="EqualDomainSid",
             CallingConvention=CallingConvention.Winapi, 
             SetLastError=true, 
             CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern BOOL IsEqualDomainSid(
            byte[] sid1,
            byte[] sid2,
            out bool result); 

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern IntPtr GetCurrentProcess();
 
        [DllImport(
             ADVAPI32,
             EntryPoint="GetSecurityDescriptorLength",
             CallingConvention=CallingConvention.Winapi, 
             SetLastError=true,
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern DWORD GetSecurityDescriptorLength(
            IntPtr byteArray ); 

        [DllImport(
             ADVAPI32,
             EntryPoint="GetSecurityInfo", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD GetSecurityInfoByHandle( 
            SafeHandle handle,
            DWORD objectType,
            DWORD securityInformation,
            out IntPtr sidOwner, 
            out IntPtr sidGroup,
            out IntPtr dacl, 
            out IntPtr sacl, 
            out IntPtr securityDescriptor );
 
        [DllImport(
             ADVAPI32,
             EntryPoint="GetNamedSecurityInfoW",
             CallingConvention=CallingConvention.Winapi, 
             SetLastError=true,
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)] 
        internal static extern DWORD GetSecurityInfoByName(
            string name, 
            DWORD objectType,
            DWORD securityInformation,
            out IntPtr sidOwner,
            out IntPtr sidGroup, 
            out IntPtr dacl,
            out IntPtr sacl, 
            out IntPtr securityDescriptor ); 

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern
        bool GetTokenInformation (
            [In]  IntPtr                TokenHandle, 
            [In]  uint                  TokenInformationClass,
            [In]  SafeLocalAllocHandle  TokenInformation, 
            [In]  uint                  TokenInformationLength, 
            [Out] out uint              ReturnLength);
 
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern
        bool GetTokenInformation ( 
            [In]  SafeTokenHandle       TokenHandle,
            [In]  uint                  TokenInformationClass, 
            [In]  SafeLocalAllocHandle  TokenInformation, 
            [In]  uint                  TokenInformationLength,
            [Out] out uint              ReturnLength); 

        [DllImport(
             ADVAPI32,
             EntryPoint="GetWindowsAccountDomainSid", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern BOOL GetWindowsAccountDomainSid( 
            byte[] sid,
            [Out] byte[] resultSid,
            ref DWORD resultSidLength );
 
        internal enum SECURITY_IMPERSONATION_LEVEL
        { 
            Anonymous = 0, 
            Identification = 1,
            Impersonation = 2, 
            Delegation = 3,
        }

        [DllImport( 
             ADVAPI32,
             EntryPoint="IsWellKnownSid", 
             CallingConvention=CallingConvention.Winapi, 
             SetLastError=true,
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern BOOL IsWellKnownSid(
            byte[] sid,
            int type ); 

        [DllImport( 
            ADVAPI32, 
            EntryPoint="LsaOpenPolicy",
            CallingConvention=CallingConvention.Winapi, 
            SetLastError=true,
            CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD LsaOpenPolicy( 
            string systemName,
            ref LSA_OBJECT_ATTRIBUTES attributes, 
            int accessMask, 
            out SafeLsaPolicyHandle handle
            ); 

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(
            ADVAPI32, 
            EntryPoint="LookupPrivilegeValueW",
            CharSet=CharSet.Auto, 
            SetLastError=true, 
            BestFitMapping=false)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        bool LookupPrivilegeValue (
            [In]     string             lpSystemName,
            [In]     string             lpName, 
            [In,Out] ref LUID           Luid);
 
        [DllImport( 
            ADVAPI32,
            EntryPoint="LsaLookupSids", 
            CallingConvention=CallingConvention.Winapi,
            SetLastError=true,
            CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern DWORD LsaLookupSids(
            SafeLsaPolicyHandle handle, 
            int count, 
            IntPtr[] sids,
            ref SafeLsaMemoryHandle referencedDomains, 
            ref SafeLsaMemoryHandle names
            );

        [DllImport(ADVAPI32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern int LsaFreeMemory( IntPtr handle ); 

        [DllImport( 
            ADVAPI32,
            EntryPoint="LsaLookupNames",
            CallingConvention=CallingConvention.Winapi,
            SetLastError=true, 
            CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern DWORD LsaLookupNames( 
            SafeLsaPolicyHandle handle,
            int count, 
            UNICODE_STRING[] names,
            ref SafeLsaMemoryHandle referencedDomains,
            ref SafeLsaMemoryHandle sids
            ); 

        [DllImport( 
            ADVAPI32, 
            EntryPoint="LsaLookupNames2",
            CallingConvention=CallingConvention.Winapi, 
            SetLastError=true,
            CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD LsaLookupNames2( 
            SafeLsaPolicyHandle handle,
            int flags, 
            int count, 
            UNICODE_STRING[] names,
            ref SafeLsaMemoryHandle referencedDomains, 
            ref SafeLsaMemoryHandle sids
            );

        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern 
        int LsaConnectUntrusted ( 
            [In,Out] ref SafeLsaLogonProcessHandle LsaHandle);
 
        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern
        int LsaGetLogonSessionData ( 
            [In]     ref LUID                      LogonId,
            [In,Out] ref SafeLsaReturnBufferHandle ppLogonSessionData); 
 
        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        int LsaLogonUser (
            [In]     SafeLsaLogonProcessHandle      LsaHandle,
            [In]     ref UNICODE_INTPTR_STRING      OriginName, 
            [In]     uint                           LogonType,
            [In]     uint                           AuthenticationPackage, 
            [In]     IntPtr                         AuthenticationInformation, 
            [In]     uint                           AuthenticationInformationLength,
            [In]     IntPtr                         LocalGroups, 
            [In]     ref TOKEN_SOURCE               SourceContext,
            [In,Out] ref SafeLsaReturnBufferHandle  ProfileBuffer,
            [In,Out] ref uint                       ProfileBufferLength,
            [In,Out] ref LUID                       LogonId, 
            [In,Out] ref SafeTokenHandle            Token,
            [In,Out] ref QUOTA_LIMITS               Quotas, 
            [In,Out] ref int                        SubStatus); 

        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern
        int LsaLookupAuthenticationPackage (
            [In]     SafeLsaLogonProcessHandle LsaHandle, 
            [In]     ref UNICODE_INTPTR_STRING PackageName,
            [In,Out] ref uint                  AuthenticationPackage); 
 
        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern
        int LsaRegisterLogonProcess (
            [In]     ref UNICODE_INTPTR_STRING     LogonProcessName,
            [In,Out] ref SafeLsaLogonProcessHandle LsaHandle, 
            [In,Out] ref IntPtr                    SecurityMode);
 
        [DllImport(SECUR32, SetLastError=true)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int LsaDeregisterLogonProcess(IntPtr handle); 

        [DllImport(ADVAPI32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern int LsaClose( IntPtr handle );
 
        [DllImport(SECUR32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        internal static extern int LsaFreeReturnBuffer(IntPtr handle);

        [DllImport (ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        [ResourceExposure(ResourceScope.Process)] 
        internal static extern
        bool OpenProcessToken ( 
            [In]     IntPtr              ProcessToken, 
            [In]     TokenAccessLevels   DesiredAccess,
            [In,Out] ref SafeTokenHandle TokenHandle); 

        [DllImport(
             ADVAPI32,
             EntryPoint="SetNamedSecurityInfoW", 
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)] 
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern DWORD SetSecurityInfoByName( 
            string name,
            DWORD objectType,
            DWORD securityInformation,
            byte[] owner, 
            byte[] group,
            byte[] dacl, 
            byte[] sacl ); 

        [DllImport( 
             ADVAPI32,
             EntryPoint="SetSecurityInfo",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true, 
             CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern DWORD SetSecurityInfoByHandle( 
            SafeHandle handle,
            DWORD objectType, 
            DWORD securityInformation,
            byte[] owner,
            byte[] group,
            byte[] dacl, 
            byte[] sacl );
 
#else // FEATURE_PAL 

        // managed cryptography wrapper around the PALRT cryptography api 
        internal const int PAL_HCRYPTPROV = 123;

        internal const int CALG_MD2         = ((4 << 13) | 1);
        internal const int CALG_MD4         = ((4 << 13) | 2); 
        internal const int CALG_MD5         = ((4 << 13) | 3);
        internal const int CALG_SHA         = ((4 << 13) | 4); 
        internal const int CALG_SHA1        = ((4 << 13) | 4); 
        internal const int CALG_MAC         = ((4 << 13) | 5);
        internal const int CALG_SSL3_SHAMD5 = ((4 << 13) | 8); 
        internal const int CALG_HMAC        = ((4 << 13) | 9);

        internal const int HP_ALGID         = 0x0001;
        internal const int HP_HASHVAL       = 0x0002; 
        internal const int HP_HASHSIZE      = 0x0004;
 
        [DllImport(OLEAUT32, CharSet=CharSet.Unicode, EntryPoint="CryptAcquireContextW")] 
        [ResourceExposure(ResourceScope.Machine)]
        internal extern static bool CryptAcquireContext(out IntPtr hProv, 
                           [MarshalAs(UnmanagedType.LPWStr)] string container,
                           [MarshalAs(UnmanagedType.LPWStr)] string provider,
                           int provType,
                           int flags); 

        [DllImport(OLEAUT32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal extern static bool CryptReleaseContext( IntPtr hProv, int flags);
 
        [DllImport(OLEAUT32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)]
        internal extern static bool CryptCreateHash(IntPtr hProv, int Algid, IntPtr hKey, int flags, out IntPtr hHash);
 
        [DllImport(OLEAUT32, SetLastError=true)]
        [ResourceExposure(ResourceScope.None)] 
        internal extern static bool CryptDestroyHash(IntPtr hHash); 

        [DllImport(OLEAUT32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal extern static bool CryptHashData(IntPtr hHash,
                           [In, MarshalAs(UnmanagedType.LPArray)] byte[] data,
                           int length, 
                           int flags);
 
        [DllImport(OLEAUT32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)]
        internal extern static bool CryptGetHashParam(IntPtr hHash, 
                           int param,
                           [Out, MarshalAs(UnmanagedType.LPArray)] byte[] digest,
                           ref int length,
                           int flags); 

        [DllImport(OLEAUT32, SetLastError=true)] 
        [ResourceExposure(ResourceScope.None)] 
        internal extern static bool CryptGetHashParam(IntPtr hHash,
                           int param, 
                           out int data,
                           ref int length,
                           int flags);
 
        [DllImport(KERNEL32, EntryPoint="PAL_Random")]
        [ResourceExposure(ResourceScope.None)] 
        internal extern static bool Random(bool bStrong, 
                           [Out, MarshalAs(UnmanagedType.LPArray)] byte[] buffer, int length);
#endif // FEATURE_PAL 

    // Fusion APIs
#if FEATURE_COMINTEROP
 
        [DllImport(MSCORWKS, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int CreateAssemblyNameObject(out IAssemblyName ppEnum, String szAssemblyName, uint dwFlags, IntPtr pvReserved); 

        [DllImport(MSCORWKS, CharSet=CharSet.Auto)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int CreateAssemblyEnum(out IAssemblyEnum ppEnum, IApplicationContext pAppCtx, IAssemblyName pName, uint dwFlags, IntPtr pvReserved);
#else // FEATURE_COMINTEROP
 
        [DllImport(MSCORWKS, CharSet=CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)] 
        internal static extern int CreateAssemblyNameObject(out SafeFusionHandle ppEnum, String szAssemblyName, uint dwFlags, IntPtr pvReserved); 

        [DllImport(MSCORWKS, CharSet=CharSet.Auto)] 
        [ResourceExposure(ResourceScope.None)]
        internal static extern int CreateAssemblyEnum(out SafeFusionHandle ppEnum, SafeFusionHandle pAppCtx, SafeFusionHandle pName, uint dwFlags, IntPtr pvReserved);
#endif // FEATURE_COMINTEROP
 
    // Globalization APIs
        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        [ResourceExposure(ResourceScope.None)] 
        internal extern static int GetCalendarInfo(
                                      int           Locale,     // locale 
                                      int           Calendar,   // calendar identifier
                                      int           CalType,    // calendar type
                                      StringBuilder lpCalData,  // information buffer
                                      int           cchData,    // information buffer size 
                                      IntPtr        lpValue     // data
                                    ); 
    } 
}
