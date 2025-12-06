// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: Marshal 
**
** 
** Purpose: This class contains methods that are mainly used to marshal
**          between unmanaged and managed types.
**
** 
=============================================================================*/
 
namespace System.Runtime.InteropServices 
{
    using System; 
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Security; 
    using System.Security.Permissions;
    using System.Text; 
    using System.Threading; 
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Activation; 
    using System.Runtime.CompilerServices;
    using System.Runtime.Remoting.Proxies;
    using System.Globalization;
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.Versioning;
    using Win32Native = Microsoft.Win32.Win32Native; 
    using Microsoft.Win32.SafeHandles; 
#if FEATURE_COMINTEROP
    using System.Runtime.InteropServices.ComTypes; 
#endif

    //=======================================================================
    // All public methods, including PInvoke, are protected with linkchecks. 
    // Remove the default demands for all PInvoke methods with this global
    // declaration on the class. 
    //======================================================================= 

    [SuppressUnmanagedCodeSecurityAttribute()] 
    public static class Marshal
    {
        //===================================================================
        // Defines used inside the Marshal class. 
        //====================================================================
        private const int LMEM_FIXED = 0; 
#if !FEATURE_PAL 
        private const int LMEM_MOVEABLE = 2;
        private static readonly IntPtr HIWORDMASK = unchecked(new IntPtr((long)0xffffffffffff0000L)); 
#endif //!FEATURE_PAL
#if FEATURE_COMINTEROP
        private static Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
#endif //FEATURE_COMINTEROP 

        // Win32 has the concept of Atoms, where a pointer can either be a pointer 
        // or an int.  If it's less than 64K, this is guaranteed to NOT be a 
        // pointer since the bottom 64K bytes are reserved in a process' page table.
        // We should be careful about deallocating this stuff.  Extracted to 
        // a function to avoid C# problems with lack of support for IntPtr.
        // We have 2 of these methods for slightly different semantics for NULL.
        private static bool IsWin32Atom(IntPtr ptr)
        { 
#if FEATURE_PAL
        return false; 
#else 
            long lPtr = (long)ptr;
            return 0 == (lPtr & (long)HIWORDMASK); 
#endif
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        private static bool IsNotWin32Atom(IntPtr ptr)
        { 
#if FEATURE_PAL 
    return false;
#else 
            long lPtr = (long)ptr;
            return 0 != (lPtr & (long)HIWORDMASK);
#endif
        } 

        //=================================================================== 
        // The default character size for the system. 
        //====================================================================
        public static readonly int SystemDefaultCharSize = 3 - Win32Native.lstrlen(new sbyte [] {0x41, 0x41, 0, 0}); 

        //====================================================================
        // The max DBCS character size for the system.
        //=================================================================== 
        public static readonly int SystemMaxDBCSCharSize = GetSystemMaxDBCSCharSize();
 
 
        //====================================================================
        // The name, title and description of the assembly that will contain 
        // the dynamically generated interop types.
        //===================================================================
        private const String s_strConvertedTypeInfoAssemblyName   = "InteropDynamicTypes";
        private const String s_strConvertedTypeInfoAssemblyTitle  = "Interop Dynamic Types"; 
        private const String s_strConvertedTypeInfoAssemblyDesc   = "Type dynamically generated from ITypeInfo's";
        private const String s_strConvertedTypeInfoNameSpace      = "InteropDynamicTypes"; 
 

        //=================================================================== 
        // Helper method to retrieve the system's maximum DBCS character size.
        //===================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetSystemMaxDBCSCharSize(); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static String PtrToStringAnsi(IntPtr ptr) 
        {
            if (Win32Native.NULL == ptr) { 
                return null;
            }
            else if (IsWin32Atom(ptr)) {
                return null; 
            }
            else { 
                int nb = Win32Native.lstrlenA(ptr); 
                if( nb == 0) {
                    return string.Empty; 
                }
                else {
                    StringBuilder sb = new StringBuilder(nb);
                    Win32Native.CopyMemoryAnsi(sb, ptr, new IntPtr(1+nb)); 
                    return sb.ToString();
                } 
            } 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern String PtrToStringAnsi(IntPtr ptr, int len);
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern String PtrToStringUni(IntPtr ptr, int len); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static String PtrToStringAuto(IntPtr ptr, int len)
        {
            return (SystemDefaultCharSize == 1) ? PtrToStringAnsi(ptr, len) : PtrToStringUni(ptr, len);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static String PtrToStringUni(IntPtr ptr) 
        {
            if (Win32Native.NULL == ptr) { 
                return null;
            }
            else if (IsWin32Atom(ptr)) {
                return null; 
            }
            else { 
                int nc = Win32Native.lstrlenW(ptr); 
                StringBuilder sb = new StringBuilder(nc);
                Win32Native.CopyMemoryUni(sb, ptr, new IntPtr(2*(1+nc))); 
                return sb.ToString();
            }
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String PtrToStringAuto(IntPtr ptr) 
        { 
            if (Win32Native.NULL == ptr) {
                return null; 
            }
            else if (IsWin32Atom(ptr)) {
                return null;
            } 
            else {
                int nc = Win32Native.lstrlen(ptr); 
                StringBuilder sb = new StringBuilder(nc); 
                Win32Native.lstrcpy(sb, ptr);
                return sb.ToString(); 
            }
        }

        //==================================================================== 
        // SizeOf()
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
 
        [System.Runtime.InteropServices.ComVisible(true)]
        public static extern int SizeOf(Object structure);

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern int SizeOf(Type t); 
 

        //==================================================================== 
        // OffsetOf()
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr OffsetOf(Type t, String fieldName) 
        {
            if (t == null) 
                throw new ArgumentNullException("t"); 

            FieldInfo f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); 
            if (f == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetOfFieldNotFound", t.FullName), "fieldName");
            else if (!(f is RuntimeFieldInfo))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeFieldInfo"), "fieldName"); 

            return OffsetOfHelper(((RuntimeFieldInfo)f).GetFieldHandle().Value); 
        } 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr OffsetOfHelper(IntPtr f); 

        //===================================================================
        // UnsafeAddrOfPinnedArrayElement()
        // 
        // IMPORTANT NOTICE: This method does not do any verification on the
        // array. It must be used with EXTREME CAUTION since passing in 
        // an array that is not pinned or in the fixed heap can cause 
        // unexpected results !
        //==================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern IntPtr UnsafeAddrOfPinnedArrayElement(Array arr, int index);
 

        //=================================================================== 
        // Copy blocks from CLR arrays to native memory. 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(int[]     source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(char[]    source, int startIndex, IntPtr destination, int length) 
        { 
            CopyToNative(source, startIndex, destination, length);
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(short[]   source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length); 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(long[]    source, int startIndex, IntPtr destination, int length) 
        {
            CopyToNative(source, startIndex, destination, length); 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(float[]   source, int startIndex, IntPtr destination, int length)
        { 
            CopyToNative(source, startIndex, destination, length);
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(double[]  source, int startIndex, IntPtr destination, int length)
        { 
            CopyToNative(source, startIndex, destination, length);
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(byte[] source, int startIndex, IntPtr destination, int length) 
        {
            CopyToNative(source, startIndex, destination, length); 
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(IntPtr[] source, int startIndex, IntPtr destination, int length) 
        {
            CopyToNative(source, startIndex, destination, length);
        }
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern void CopyToNative(Object source, int startIndex, IntPtr destination, int length);
 
        //=================================================================== 
        // Copy blocks from native memory to CLR arrays
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(IntPtr source, int[]     destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length); 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(IntPtr source, char[]    destination, int startIndex, int length) 
        {
            CopyToManaged(source, destination, startIndex, length); 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(IntPtr source, short[]   destination, int startIndex, int length)
        { 
            CopyToManaged(source, destination, startIndex, length);
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(IntPtr source, long[]    destination, int startIndex, int length)
        { 
            CopyToManaged(source, destination, startIndex, length);
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(IntPtr source, float[]   destination, int startIndex, int length) 
        {
            CopyToManaged(source, destination, startIndex, length); 
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(IntPtr source, double[]  destination, int startIndex, int length) 
        {
            CopyToManaged(source, destination, startIndex, length);
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(IntPtr source, byte[] destination, int startIndex, int length)
        { 
            CopyToManaged(source, destination, startIndex, length); 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(IntPtr source, IntPtr[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        } 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void CopyToManaged(IntPtr source, Object destination, int startIndex, int length); 
 
        //===================================================================
        // Read from memory 
        //====================================================================
        [DllImport(Win32Native.SHIM, EntryPoint="ND_RU1")]
        [ResourceExposure(ResourceScope.None)]
        public static extern byte ReadByte([MarshalAs(UnmanagedType.AsAny), In] Object ptr, int ofs); 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_RU1")] 
        [ResourceExposure(ResourceScope.None)] 
        public static extern byte ReadByte(IntPtr ptr, int ofs);
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static byte ReadByte(IntPtr ptr)
        {
            return ReadByte(ptr,0); 
        }
 
        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI2")] 
        [ResourceExposure(ResourceScope.None)]
        public static extern short ReadInt16([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs); 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI2")]
        [ResourceExposure(ResourceScope.None)]
        public static extern short ReadInt16(IntPtr ptr, int ofs); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static short ReadInt16(IntPtr ptr) 
        {
            return ReadInt16(ptr, 0); 
        }

        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI4"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [ResourceExposure(ResourceScope.None)] 
        public static extern int ReadInt32([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs);
 
        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI4"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        [ResourceExposure(ResourceScope.None)]
        public static extern int ReadInt32(IntPtr ptr, int ofs); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int ReadInt32(IntPtr ptr)
        { 
            return ReadInt32(ptr,0);
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static IntPtr ReadIntPtr([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs) 
        {
            #if WIN32
                return (IntPtr) ReadInt32(ptr, ofs);
            #else 
                return (IntPtr) ReadInt64(ptr, ofs);
            #endif 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        public static IntPtr ReadIntPtr(IntPtr ptr, int ofs)
        {
            #if WIN32
                return (IntPtr) ReadInt32(ptr, ofs); 
            #else
                return (IntPtr) ReadInt64(ptr, ofs); 
            #endif 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static IntPtr ReadIntPtr(IntPtr ptr)
        {
            #if WIN32 
                return (IntPtr) ReadInt32(ptr, 0);
            #else 
                return (IntPtr) ReadInt64(ptr, 0); 
            #endif
        } 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI8"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern long ReadInt64([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs);
 
        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI8"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern long ReadInt64(IntPtr ptr, int ofs); 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static long ReadInt64(IntPtr ptr) 
        {
            return ReadInt64(ptr,0);
        }
 

        //==================================================================== 
        // Write to memory 
        //===================================================================
        [DllImport(Win32Native.SHIM, EntryPoint="ND_WU1")] 
        public static extern void WriteByte(IntPtr ptr, int ofs, byte val);

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WU1")]
        public static extern void WriteByte([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, byte val); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteByte(IntPtr ptr, byte val) 
        {
            WriteByte(ptr, 0, val); 
        }

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI2")]
        public static extern void WriteInt16(IntPtr ptr, int ofs, short val); 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI2")] 
        public static extern void WriteInt16([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, short val); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteInt16(IntPtr ptr, short val)
        {
            WriteInt16(ptr, 0, val);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteInt16(IntPtr ptr, int ofs, char val) 
        {
            WriteInt16(ptr, ofs, (short)val); 
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void WriteInt16([In,Out]Object ptr, int ofs, char val) 
        {
            WriteInt16(ptr, ofs, (short)val); 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteInt16(IntPtr ptr, char val)
        {
            WriteInt16(ptr, 0, (short)val);
        } 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI4")] 
        public static extern void WriteInt32(IntPtr ptr, int ofs, int val); 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI4")] 
        public static extern void WriteInt32([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, int val);

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void WriteInt32(IntPtr ptr, int val) 
        {
            WriteInt32(ptr,0,val); 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteIntPtr(IntPtr ptr, int ofs, IntPtr val)
        {
            #if WIN32
                WriteInt32(ptr, ofs, (int)val); 
            #else
                WriteInt64(ptr, ofs, (long)val); 
            #endif 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void WriteIntPtr([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, IntPtr val)
        {
            #if WIN32 
                WriteInt32(ptr, ofs, (int)val);
            #else 
                WriteInt64(ptr, ofs, (long)val); 
            #endif
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void WriteIntPtr(IntPtr ptr, IntPtr val)
        { 
            #if WIN32
                WriteInt32(ptr, 0, (int)val); 
            #else 
                WriteInt64(ptr, 0, (long)val);
            #endif 
        }

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI8")]
        public static extern void WriteInt64(IntPtr ptr, int ofs, long val); 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI8")] 
        public static extern void WriteInt64([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, long val); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteInt64(IntPtr ptr, long val)
        {
            WriteInt64(ptr, 0, val);
        } 

 
        //==================================================================== 
        // GetLastWin32Error
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern int GetLastWin32Error(); 

 
        //=================================================================== 
        // SetLastWin32Error
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern void SetLastWin32Error(int error);
 

        //==================================================================== 
        // GetHRForLastWin32Error 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int GetHRForLastWin32Error()
        {
            int dwLastError = GetLastWin32Error(); 
            if ((dwLastError & 0x80000000) == 0x80000000)
                return dwLastError; 
            else 
                return (dwLastError & 0x0000FFFF) | unchecked((int)0x80070000);
        } 


        //====================================================================
        // Prelink 
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Prelink(MethodInfo m) 
        {
            if (m == null) 
                throw new ArgumentNullException("m");

            if (!(m is RuntimeMethodInfo))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo")); 

            RuntimeMethodHandle method = m.MethodHandle; 
 
            InternalPrelink(method.Value);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void InternalPrelink(IntPtr m);
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void PrelinkAll(Type c) 
        { 
            if (c == null)
                throw new ArgumentNullException("c"); 

            MethodInfo[] mi = c.GetMethods();
            if (mi != null)
            { 
                for (int i = 0; i < mi.Length; i++)
                { 
                    Prelink(mi[i]); 
                }
            } 
        }

        //===================================================================
        // NumParamBytes 
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static int NumParamBytes(MethodInfo m) 
        {
            if (m == null) 
                throw new ArgumentNullException("m");

            if (!(m is RuntimeMethodInfo))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo")); 

            RuntimeMethodHandle method = m.GetMethodHandle(); 
 
            return InternalNumParamBytes(method.Value);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int InternalNumParamBytes(IntPtr m);
 
        //===================================================================
        // Win32 Exception stuff 
        // These are mostly interesting for Structured exception handling, 
        // but need to be exposed for all exceptions (not just SEHException).
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
[System.Runtime.InteropServices.ComVisible(true)]
        public static extern /* struct _EXCEPTION_POINTERS* */ IntPtr GetExceptionPointers(); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern int GetExceptionCode();
 

        //===================================================================
        // Marshals data from a structure class to a native memory block.
        // If the structure contains pointers to allocated blocks and 
        // "fDeleteOld" is true, this routine will call DestroyStructure() first.
        //==================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
[System.Runtime.InteropServices.ComVisible(true)] 
        public static extern void StructureToPtr(Object structure, IntPtr ptr, bool fDeleteOld);


        //=================================================================== 
        // Marshals data from a native memory block to a preallocated structure class.
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
[System.Runtime.InteropServices.ComVisible(true)]
        public static void PtrToStructure(IntPtr ptr, Object structure) 
        {
            PtrToStructureHelper(ptr, structure, false);
        }
 

        //==================================================================== 
        // Creates a new instance of "structuretype" and marshals data from a 
        // native memory block to it.
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [System.Runtime.InteropServices.ComVisible(true)]
        public static Object PtrToStructure(IntPtr ptr, Type structureType)
        { 
            if (ptr == Win32Native.NULL) return null;
 
            if (structureType == null) 
                throw new ArgumentNullException("structureType");
 
            if (structureType.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "structureType");

            Object structure = Activator.InternalCreateInstanceWithNoMemberAccessCheck(structureType, true); 
            PtrToStructureHelper(ptr, structure, true);
            return structure; 
        } 

 
        //====================================================================
        // Helper function to copy a pointer into a preallocated structure.
        //===================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern void PtrToStructureHelper(IntPtr ptr, Object structure, bool allowValueClasses);
 
 
        //===================================================================
        // Freeds all substructures pointed to by the native memory block. 
        // "structureclass" is used to provide layout information.
        //===================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
[System.Runtime.InteropServices.ComVisible(true)]
        public static extern void DestroyStructure(IntPtr ptr, Type structuretype); 
 

        //==================================================================== 
        // Returns the HInstance for this module.  Returns -1 if the module
        // doesn't have an HInstance.  In Memory (Dynamic) Modules won't have
        // an HInstance.
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr GetHINSTANCE(Module m) 
        { 
            if (m == null)
                throw new ArgumentNullException("m"); 
            return m.GetHINSTANCE();
        }

 
        //====================================================================
        // Throws a CLR exception based on the HRESULT. 
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void ThrowExceptionForHR(int errorCode) 
        {
            if (errorCode < 0)
                ThrowExceptionForHRInternal(errorCode, Win32Native.NULL);
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void ThrowExceptionForHR(int errorCode, IntPtr errorInfo) 
        { 
            if (errorCode < 0)
                ThrowExceptionForHRInternal(errorCode, errorInfo); 
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ThrowExceptionForHRInternal(int errorCode, IntPtr errorInfo); 

 
        //=================================================================== 
        // Converts the HRESULT to a CLR exception.
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Exception GetExceptionForHR(int errorCode)
        {
            if (errorCode < 0) 
                return GetExceptionForHRInternal(errorCode, Win32Native.NULL);
            else 
                return null; 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static Exception GetExceptionForHR(int errorCode, IntPtr errorInfo)
        {
            if (errorCode < 0)
                return GetExceptionForHRInternal(errorCode, errorInfo); 
            else
                return null; 
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern Exception GetExceptionForHRInternal(int errorCode, IntPtr errorInfo);


        //=================================================================== 
        // Converts the CLR exception to an HRESULT. This function also sets
        // up an IErrorInfo for the exception. 
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern int GetHRForException(Exception e);

#if !FEATURE_PAL
        //=================================================================== 
        // This method is intended for compiler code generators rather
        // than applications. 
        //==================================================================== 
        //
        [ObsoleteAttribute("The GetUnmanagedThunkForManagedMethodPtr method has been deprecated and will be removed in a future release.", false)] 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern IntPtr GetUnmanagedThunkForManagedMethodPtr(IntPtr pfnMethodToWrap, IntPtr pbSignature, int cbSignature);
 
        //===================================================================
        // This method is intended for compiler code generators rather 
        // than applications. 
        //====================================================================
        // 
        [ObsoleteAttribute("The GetManagedThunkForUnmanagedMethodPtr method has been deprecated and will be removed in a future release.", false)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern IntPtr GetManagedThunkForUnmanagedMethodPtr(IntPtr pfnMethodToWrap, IntPtr pbSignature, int cbSignature); 

        //==================================================================== 
        // The hosting APIs allow a sophisticated host to schedule fibers 
        // onto OS threads, so long as they notify the runtime of this
        // activity.  A fiber cookie can be redeemed for its managed Thread 
        // object by calling the following service.
        //===================================================================
        //
        [ObsoleteAttribute("The GetThreadFromFiberCookie method has been deprecated.  Use the hosting API to perform this operation.", false)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Thread GetThreadFromFiberCookie(int cookie) 
        { 
            if (cookie == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_ArgumentZero"), "cookie"); 

            return InternalGetThreadFromFiberCookie(cookie);
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Thread InternalGetThreadFromFiberCookie(int cookie); 
#endif //!FEATURE_PAL 

 
        //====================================================================
        // Memory allocation and dealocation.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static IntPtr AllocHGlobal(IntPtr cb) 
        { 
            IntPtr pNewMem = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, cb);
 
            if (pNewMem == Win32Native.NULL) {
                throw new OutOfMemoryException();
            }
            return pNewMem; 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static IntPtr AllocHGlobal(int cb) 
        {
            return AllocHGlobal((IntPtr)cb);
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        public static void FreeHGlobal(IntPtr hglobal) 
        {
            if (IsNotWin32Atom(hglobal)) { 
                if (Win32Native.NULL != Win32Native.LocalFree(hglobal)) {
                    ThrowExceptionForHR(GetHRForLastWin32Error());
                }
            } 
        }
 
#if !FEATURE_PAL 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr ReAllocHGlobal(IntPtr pv, IntPtr cb) 
        {
            IntPtr pNewMem = Win32Native.LocalReAlloc(pv, cb, LMEM_MOVEABLE);
            if (pNewMem == Win32Native.NULL) {
                throw new OutOfMemoryException(); 
            }
            return pNewMem; 
        } 

 
        //===================================================================
        // String convertions.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr StringToHGlobalAnsi(String s)
        { 
            if (s == null) 
            {
                return Win32Native.NULL; 
            }
            else
            {
                int nb = (s.Length + 1) * SystemMaxDBCSCharSize; 

                // Overflow checking 
                if (nb < s.Length) 
                    throw new ArgumentOutOfRangeException("s");
 
                IntPtr len = new IntPtr(nb);
                IntPtr hglobal = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, len);

                if (hglobal == Win32Native.NULL) 
                {
                    throw new OutOfMemoryException(); 
                } 
                else
                { 
                    Win32Native.CopyMemoryAnsi(hglobal, s, len);
                    return hglobal;
                }
            } 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr StringToCoTaskMemAnsi(String s)
        { 
            if (s == null)
            {
                return Win32Native.NULL;
            } 
            else
            { 
                int nb = (s.Length + 1) * SystemMaxDBCSCharSize; 

                // Overflow checking 
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException("s");

                IntPtr hglobal = Win32Native.CoTaskMemAlloc(nb); 

                if (hglobal == Win32Native.NULL) 
                { 
                    throw new OutOfMemoryException();
                } 
                else
                {
                    Win32Native.CopyMemoryAnsi(hglobal, s, new IntPtr(nb));
                    return hglobal; 
                }
            } 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr StringToHGlobalUni(String s)
        {
            if (s == null)
            { 
                return Win32Native.NULL;
            } 
            else 
            {
                int nb = (s.Length + 1) * 2; 

                // Overflow checking
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException("s"); 

                IntPtr len = new IntPtr(nb); 
                IntPtr hglobal = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, len); 

                if (hglobal == Win32Native.NULL) 
                {
                    throw new OutOfMemoryException();
                }
                else 
                {
                    Win32Native.CopyMemoryUni(hglobal, s, len); 
                    return hglobal; 
                }
            } 
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr StringToHGlobalAuto(String s) 
        {
            return (SystemDefaultCharSize == 1) ? StringToHGlobalAnsi(s) : StringToHGlobalUni(s); 
        } 
#endif //!FEATURE_PAL
 
#if FEATURE_COMINTEROP

        //====================================================================
        // Given a managed object that wraps a UCOMITypeLib, return its name 
        //===================================================================
        [Obsolete("Use System.Runtime.InteropServices.Marshal.GetTypeLibName(ITypeLib pTLB) instead. http://go.microsoft.com/fwlink/?linkid=14202&ID=0000011.", false)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static String GetTypeLibName(UCOMITypeLib pTLB)
        { 
            return GetTypeLibName((ITypeLib)pTLB);
        }

 
        //====================================================================
        // Given a managed object that wraps an ITypeLib, return its name 
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String GetTypeLibName(ITypeLib typelib) 
        {
            String strTypeLibName = null;
            String strDocString = null;
            int dwHelpContext = 0; 
            String strHelpFile = null;
 
            if (typelib == null) 
                throw new ArgumentNullException("typelib");
 
            typelib.GetDocumentation(-1, out strTypeLibName, out strDocString, out dwHelpContext, out strHelpFile);

            return strTypeLibName;
        } 

 
        //=================================================================== 
        // Given an managed object that wraps an UCOMITypeLib, return its guid
        //==================================================================== 
        [Obsolete("Use System.Runtime.InteropServices.Marshal.GetTypeLibGuid(ITypeLib pTLB) instead. http://go.microsoft.com/fwlink/?linkid=14202&ID=0000011.", false)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Guid GetTypeLibGuid(UCOMITypeLib pTLB)
        { 
            return GetTypeLibGuid((ITypeLib)pTLB);
        } 
 
        //===================================================================
        // Given an managed object that wraps an ITypeLib, return its guid 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Guid GetTypeLibGuid(ITypeLib typelib)
        { 
            Guid result = new Guid ();
            FCallGetTypeLibGuid (ref result, typelib); 
            return result; 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallGetTypeLibGuid(ref Guid result, ITypeLib pTLB);

        //=================================================================== 
        // Given a managed object that wraps a UCOMITypeLib, return its lcid
        //==================================================================== 
        [Obsolete("Use System.Runtime.InteropServices.Marshal.GetTypeLibLcid(ITypeLib pTLB) instead. http://go.microsoft.com/fwlink/?linkid=14202&ID=0000011.", false)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static int GetTypeLibLcid(UCOMITypeLib pTLB) 
        {
            return GetTypeLibLcid((ITypeLib)pTLB);
        }
 
        //===================================================================
        // Given a managed object that wraps an ITypeLib, return its lcid 
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        public static extern int GetTypeLibLcid(ITypeLib typelib);

        //====================================================================
        // Given a managed object that wraps an ITypeLib, return it's 
        // version information.
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void GetTypeLibVersion(ITypeLib typeLibrary, out int major, out int minor);
 
        //====================================================================
        // Given a managed object that wraps an ITypeInfo, return its guid.
        //===================================================================
        internal static Guid GetTypeInfoGuid(ITypeInfo typeInfo) 
        {
            Guid result = new Guid (); 
            FCallGetTypeInfoGuid (ref result, typeInfo); 
            return result;
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallGetTypeInfoGuid(ref Guid result, ITypeInfo typeInfo);
 
        //===================================================================
        // Given a assembly, return the TLBID that will be generated for the 
        // typelib exported from the assembly. 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static Guid GetTypeLibGuidForAssembly(Assembly asm)
        {
            Guid result = new Guid ();
            FCallGetTypeLibGuidForAssembly (ref result, asm == null ? null : asm.InternalAssembly); 
            return result;
        } 
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallGetTypeLibGuidForAssembly(ref Guid result, Assembly asm); 

        //====================================================================
        // Given a assembly, return the version number of the type library
        // that would be exported from the assembly. 
        //===================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern void _GetTypeLibVersionForAssembly(Assembly inputAssembly, out int majorVersion, out int minorVersion); 

        [ResourceExposure(ResourceScope.None)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void GetTypeLibVersionForAssembly(Assembly inputAssembly, out int majorVersion, out int minorVersion)
        {
            _GetTypeLibVersionForAssembly(inputAssembly == null ? null : inputAssembly.InternalAssembly, out majorVersion, out minorVersion); 
        }
 
        //==================================================================== 
        // Given a managed object that wraps an UCOMITypeInfo, return its name
        //==================================================================== 
        [Obsolete("Use System.Runtime.InteropServices.Marshal.GetTypeInfoName(ITypeInfo pTLB) instead. http://go.microsoft.com/fwlink/?linkid=14202&ID=0000011.", false)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String GetTypeInfoName(UCOMITypeInfo pTI)
        { 
            return GetTypeInfoName((ITypeInfo)pTI);
        } 
 
        //===================================================================
        // Given a managed object that wraps an ITypeInfo, return its name 
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String GetTypeInfoName(ITypeInfo typeInfo)
        { 
            String strTypeLibName = null;
            String strDocString = null; 
            int dwHelpContext = 0; 
            String strHelpFile = null;
 
            if (typeInfo == null)
                throw new ArgumentNullException("typeInfo");

            typeInfo.GetDocumentation(-1, out strTypeLibName, out strDocString, out dwHelpContext, out strHelpFile); 

            return strTypeLibName; 
        } 

 

        //===================================================================
        // If a type with the specified GUID is loaded, this method will
        // return the reflection type that represents it. Otherwise it returns 
        // NULL.
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern Type GetLoadedTypeForGUID(ref Guid guid);
 
        //===================================================================
        // map ITypeInfo* to Type
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static Type GetTypeForITypeInfo(IntPtr /* ITypeInfo* */ piTypeInfo)
        { 
            ITypeInfo pTI = null; 
            ITypeLib pTLB = null;
            Type TypeObj = null; 
            Assembly AsmBldr = null;
            TypeLibConverter TlbConverter = null;
            int Index = 0;
            Guid clsid; 

            // If the input ITypeInfo is NULL then return NULL. 
            if (piTypeInfo == Win32Native.NULL) 
                return null;
 
            // Wrap the ITypeInfo in a CLR object.
            pTI = (ITypeInfo)GetObjectForIUnknown(piTypeInfo);

            // Check to see if a class exists with the specified GUID. 

            clsid = GetTypeInfoGuid(pTI); 
            TypeObj = GetLoadedTypeForGUID(ref clsid); 

            // If we managed to find the type based on the GUID then return it. 
            if (TypeObj != null)
                return TypeObj;

            // There is no type with the specified GUID in the app domain so lets 
            // try and convert the containing typelib.
            try 
            { 
                pTI.GetContainingTypeLib(out pTLB, out Index);
            } 
            catch(COMException)
            {
                pTLB = null;
            } 

            // Check to see if we managed to get a containing typelib. 
            if (pTLB != null) 
            {
                // Get the assembly name from the typelib. 
                AssemblyName AsmName = TypeLibConverter.GetAssemblyNameFromTypelib(pTLB, null, null, null, null, AssemblyNameFlags.None);
                String AsmNameString = AsmName.FullName;

                // Check to see if the assembly that will contain the type already exists. 
                Assembly[] aAssemblies = Thread.GetDomain().GetAssemblies();
                int NumAssemblies = aAssemblies.Length; 
                for (int i = 0; i < NumAssemblies; i++) 
                {
                    if (String.Compare(aAssemblies[i].FullName, 
                                       AsmNameString,StringComparison.Ordinal) == 0)
                        AsmBldr = aAssemblies[i];
                }
 
                // If we haven't imported the assembly yet then import it.
                if (AsmBldr == null) 
                { 
                    TlbConverter = new TypeLibConverter();
                    AsmBldr = TlbConverter.ConvertTypeLibToAssembly(pTLB, 
                        GetTypeLibName(pTLB) + ".dll", 0, new ImporterCallback(), null, null, null, null);
                }

                // Load the type object from the imported typelib. 
                TypeObj = AsmBldr.GetType(GetTypeLibName(pTLB) + "." + GetTypeInfoName(pTI), true, false);
                if (TypeObj != null && !TypeObj.IsVisible) 
                    TypeObj = null; 
            }
            else 
            {
                // If the ITypeInfo does not have a containing typelib then simply
                // return Object as the type.
                TypeObj = typeof(Object); 
            }
 
            return TypeObj; 
        }
 
        //===================================================================
        // map Type to ITypeInfo*
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern IntPtr /* ITypeInfo* */ GetITypeInfoForType(Type t); 
 
        //====================================================================
        // return the IUnknown* for an Object if the current context 
        // is the one where the RCW was first seen. Will return null
        // otherwise.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr /* IUnknown* */ GetIUnknownForObject(Object o)
        { 
            return GetIUnknownForObjectNative(o, false); 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr /* IUnknown* */ GetIUnknownForObjectInContext(Object o)
        {
            return GetIUnknownForObjectNative(o, true); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern IntPtr /* IUnknown* */ GetIUnknownForObjectNative(Object o, bool onlyInContext);
 
        //====================================================================
        // return the IDispatch* for an Object
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr /* IDispatch */ GetIDispatchForObject(Object o)
        { 
            return GetIDispatchForObjectNative(o, false); 
        }
 
        //===================================================================
        // return the IDispatch* for an Object if the current context
        // is the one where the RCW was first seen. Will return null
        // otherwise. 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr /* IUnknown* */ GetIDispatchForObjectInContext(Object o) 
        {
            return GetIDispatchForObjectNative(o, true); 
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IUnknown* */ GetIDispatchForObjectNative(Object o, bool onlyInContext); 

        //==================================================================== 
        // return the IUnknown* representing the interface for the Object 
        // Object o should support Type T
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr /* IUnknown* */ GetComInterfaceForObject(Object o, Type T)
        {
            return GetComInterfaceForObjectNative(o, T, false); 
        }
 
        //==================================================================== 
        // return the IUnknown* representing the interface for the Object
        // Object o should support Type T if the current context 
        // is the one where the RCW was first seen. Will return null
        // otherwise.
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr /* IUnknown* */ GetComInterfaceForObjectInContext(Object o, Type t)
        { 
            return GetComInterfaceForObjectNative(o, t, true); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IUnknown* */ GetComInterfaceForObjectNative(Object o, Type t, bool onlyInContext);

        //=================================================================== 
        // return an Object for IUnknown
        //==================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern Object GetObjectForIUnknown(IntPtr /* IUnknown* */ pUnk); 

        //===================================================================
        // Return a unique Object given an IUnknown.  This ensures that you
        //  receive a fresh object (we will not look in the cache to match up this 
        //  IUnknown to an already existing object).  This is useful in cases
        //  where you want to be able to call ReleaseComObject on a RCW 
        //  and not worry about other active uses of said RCW. 
        //===================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern Object GetUniqueObjectForIUnknown(IntPtr unknown);

 
        //===================================================================
        // return an Object for IUnknown, using the Type T, 
        //  NOTE: 
        //  Type T should be either a COM imported Type or a sub-type of COM
        //  imported Type 
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern Object GetTypedObjectForIUnknown(IntPtr /* IUnknown* */ pUnk, Type t); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern IntPtr CreateAggregatedObject(IntPtr pOuter, Object o);
 
#endif // FEATURE_COMINTEROP


        //=================================================================== 
        // check if the object is classic COM component
        //==================================================================== 
#if FEATURE_COMINTEROP 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern bool IsComObject(Object o);
#else
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static bool IsComObject(Object o) 
        {
            return false; 
        } 
#endif // FEATURE_COMINTEROP
 

#if FEATURE_COMINTEROP
        //====================================================================
        // release the COM component and if the reference hits 0 zombie this object 
        // further usage of this Object might throw an exception
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static int ReleaseComObject(Object o)
        { 
            __ComObject co = null;

            // Make sure the obj is an __ComObject.
            try 
            {
                co = (__ComObject)o; 
            } 
            catch (InvalidCastException)
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "o");
            }

            return co.ReleaseSelf(); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern int InternalReleaseComObject(Object o);
 

        //====================================================================
        // release the COM component and zombie this object
        // further usage of this Object might throw an exception 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static Int32 FinalReleaseComObject(Object o) 
        {
            __ComObject co = null; 

            if (o == null)
                throw new ArgumentNullException("o");
 
            // Make sure the obj is an __ComObject.
            try 
            { 
                co = (__ComObject)o;
            } 
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "o");
            } 

            co.FinalReleaseSelf(); 
 
            return 0;
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InternalFinalReleaseComObject(Object o);
 
        //===================================================================
        // This method retrieves data from the COM object. 
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Object GetComObjectData(Object obj, Object key) 
        {
            __ComObject comObj = null;

            // Validate that the arguments aren't null. 
            if (obj == null)
                throw new ArgumentNullException("obj"); 
            if (key == null) 
                throw new ArgumentNullException("key");
 
            // Make sure the obj is an __ComObject.
            try
            {
                comObj = (__ComObject)obj; 
            }
            catch (InvalidCastException) 
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "obj");
            } 

            // Retrieve the data from the __ComObject.
            return comObj.GetData(key);
        } 

        //==================================================================== 
        // This method sets data on the COM object. The data can only be set 
        // once for a given key and cannot be removed. This function returns
        // true if the data has been added, false if the data could not be 
        // added because there already was data for the specified key.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static bool SetComObjectData(Object obj, Object key, Object data) 
        {
            __ComObject comObj = null; 
 
            // Validate that the arguments aren't null. The data can validly be null.
            if (obj == null) 
                throw new ArgumentNullException("obj");
            if (key == null)
                throw new ArgumentNullException("key");
 
            // Make sure the obj is an __ComObject.
            try 
            { 
                comObj = (__ComObject)obj;
            } 
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "obj");
            } 

            // Retrieve the data from the __ComObject. 
            return comObj.SetData(key, data); 
        }
 
        //====================================================================
        // This method takes the given COM object and wraps it in an object
        // of the specified type. The type must be derived from __ComObject.
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Object CreateWrapperOfType(Object o, Type t) 
        { 
            // Validate the arguments.
            if (t == null) 
                throw new ArgumentNullException("t");
            if (!t.IsCOMObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeNotComObject"), "t");
            if (t.IsGenericType) 
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "t");
 
            // Check for the null case. 
            if (o == null)
                return null; 

            // Make sure the object is a COM object.
            if (!o.GetType().IsCOMObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "o"); 

            // Check to see if the type of the object is the requested type. 
            if (o.GetType() == t) 
                return o;
 
            // Check to see if we already have a cached wrapper for this type.
            Object Wrapper = GetComObjectData(o, t);
            if (Wrapper == null)
            { 
                // Create the wrapper for the specified type.
                Wrapper = InternalCreateWrapperOfType(o, t); 
 
                // Attempt to cache the wrapper on the object.
                if (!SetComObjectData(o, t, Wrapper)) 
                {
                    // Another thead already cached the wrapper so use that one instead.
                    Wrapper = GetComObjectData(o, t);
                } 
            }
 
            return Wrapper; 
        }
 
        //===================================================================
        // Helper method called from CreateWrapperOfType.
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        private static extern Object InternalCreateWrapperOfType(Object o, Type t); 
 
        //===================================================================
        // There may be a thread-based cache of COM components.  This service can 
        // force the aggressive release of the current thread's cache.
        //===================================================================
        //
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [Obsolete("This API did not perform any operation and will be removed in future versions of the CLR.", false)]
        public static void ReleaseThreadCache() 
        { 
        }
 
        //===================================================================
        // check if the type is visible from COM.
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern bool IsTypeVisibleFromCom(Type t); 
 
        //===================================================================
        // IUnknown Helpers 
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern int /* HRESULT */ QueryInterface(IntPtr /* IUnknown */ pUnk, ref Guid iid, out IntPtr ppv); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern int /* ULONG */ AddRef(IntPtr /* IUnknown */ pUnk );
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern int /* ULONG */ Release(IntPtr /* IUnknown */ pUnk );
 
        //====================================================================
        // BSTR allocation and dealocation. 
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr AllocCoTaskMem(int cb) 
        {
            IntPtr pNewMem = Win32Native.CoTaskMemAlloc(cb);
            if (pNewMem == Win32Native.NULL) {
                throw new OutOfMemoryException(); 
            }
            return pNewMem; 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr ReAllocCoTaskMem(IntPtr pv, int cb)
        {
            IntPtr pNewMem = Win32Native.CoTaskMemRealloc(pv, cb);
            if (pNewMem == Win32Native.NULL && cb != 0) { 
                throw new OutOfMemoryException();
            } 
            return pNewMem; 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void FreeCoTaskMem(IntPtr ptr)
        {
            if (IsNotWin32Atom(ptr)) { 
                Win32Native.CoTaskMemFree(ptr);
            } 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void FreeBSTR(IntPtr ptr)
        {
            if (IsNotWin32Atom(ptr)) {
                Win32Native.SysFreeString(ptr); 
            }
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr StringToCoTaskMemUni(String s) 
        {
            if (s == null)
            {
                return Win32Native.NULL; 
            }
            else 
            { 
                int nb = (s.Length + 1) * 2;
 
                // Overflow checking
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException("s");
 
                IntPtr hglobal = Win32Native.CoTaskMemAlloc(nb);
 
                if (hglobal == Win32Native.NULL) 
                {
                    throw new OutOfMemoryException(); 
                }
                else
                {
                    Win32Native.CopyMemoryUni(hglobal, s, new IntPtr(nb)); 
                    return hglobal;
                } 
            } 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr StringToCoTaskMemAuto(String s)
        {
            if (s == null) 
            {
                return Win32Native.NULL; 
            } 
            else
            { 
                int nb = (s.Length + 1) * SystemDefaultCharSize;

                // Overflow checking
                if (nb < s.Length) 
                    throw new ArgumentOutOfRangeException("s");
 
                IntPtr hglobal = Win32Native.CoTaskMemAlloc(nb); 
                if (hglobal == Win32Native.NULL)
                { 
                    throw new OutOfMemoryException();
                }
                else
                { 
                    Win32Native.lstrcpy(hglobal, s);
                    return hglobal; 
                } 
            }
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr StringToBSTR(String s)
        { 
            if (s == null)
                return Win32Native.NULL; 
 
            // Overflow checking
            if (s.Length+1 < s.Length) 
                throw new ArgumentOutOfRangeException("s");

            IntPtr bstr = Win32Native.SysAllocStringLen(s, s.Length);
            if (bstr == Win32Native.NULL) 
                throw new OutOfMemoryException();
 
            return bstr; 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String PtrToStringBSTR(IntPtr ptr)
        {
            return PtrToStringUni(ptr, Win32Native.SysStringLen(ptr)); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern void GetNativeVariantForObject(Object obj, /* VARIANT * */ IntPtr pDstNativeVariant); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern Object GetObjectForNativeVariant(/* VARIANT * */ IntPtr pSrcNativeVariant ); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern Object[] GetObjectsForNativeVariants(/* VARIANT * */ IntPtr aSrcNativeVariant, int cVars );
 
        /// <summary>
        /// <para>Returns the first valid COM slot that GetMethodInfoForSlot will work on
        /// This will be 3 for IUnknown based interfaces and 7 for IDispatch based interfaces. </para>
        /// </summary> 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern int GetStartComSlot(Type t); 

        /// <summary> 
        /// <para>Returns the last valid COM slot that GetMethodInfoForSlot will work on. </para>
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern int GetEndComSlot(Type t);
 
        /// <summary> 
        /// <para>Returns the MemberInfo that COM callers calling through the exposed
        /// vtable on the given slot will be calling. The slot should take into account 
        /// if the exposed interface is IUnknown based or IDispatch based.
        /// For classes, the lookup is done on the default interface that will be
        /// exposed for the class. </para>
        /// </summary> 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern MemberInfo GetMethodInfoForComSlot(Type t, int slot, ref ComMemberType memberType); 

        /// <summary> 
        /// <para>Returns the COM slot for a memeber info, taking into account whether
        /// the exposed interface is IUnknown based or IDispatch based</para>
        /// </summary>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static int GetComSlotForMethodInfo(MemberInfo m)
        { 
            if (m== null) 
                throw new ArgumentNullException("m");
            if (!(m is RuntimeMethodInfo)) 
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "m");
            if (!m.DeclaringType.IsInterface)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeInterfaceMethod"), "m");
            if (m.DeclaringType.IsGenericType) 
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "m");
 
            RuntimeMethodHandle method = ((RuntimeMethodInfo)m).GetMethodHandle(); 
            BCLDebug.Assert(!method.IsNullHandle(), "!method.IsNullHandle()");
 
            return InternalGetComSlotForMethodInfo(method);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern int InternalGetComSlotForMethodInfo(RuntimeMethodHandle m);
 
        //==================================================================== 
        // This method generates a GUID for the specified type. If the type
        // has a GUID in the metadata then it is returned otherwise a stable 
        // guid GUID is generated based on the fully qualified name of the
        // type.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static Guid GenerateGuidForType(Type type)
        { 
            Guid result = new Guid (); 
            FCallGenerateGuidForType (ref result, type);
            return result; 
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallGenerateGuidForType(ref Guid result, Type type); 

        //=================================================================== 
        // This method generates a PROGID for the specified type. If the type 
        // has a PROGID in the metadata then it is returned otherwise a stable
        // PROGID is generated based on the fully qualified name of the 
        // type.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String GenerateProgIdForType(Type type) 
        {
            if (type == null) 
                throw new ArgumentNullException("type"); 
            if (!RegistrationServices.TypeRequiresRegistrationHelper(type))
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustBeComCreatable"), "type"); 
            if (type.IsImport)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustNotBeComImport"), "type");
            if (type.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "type"); 

 
            IList<CustomAttributeData> cas = CustomAttributeData.GetCustomAttributes(type); 
            for (int i = 0; i < cas.Count; i ++)
            { 
                if (cas[i].Constructor.DeclaringType == typeof(ProgIdAttribute))
                {
                    // Retrieve the PROGID string from the ProgIdAttribute.
                    IList<CustomAttributeTypedArgument> caConstructorArgs = cas[i].ConstructorArguments; 
                    BCLDebug.Assert(caConstructorArgs.Count == 1, "caConstructorArgs.Count == 1");
 
                    CustomAttributeTypedArgument progIdConstructorArg = caConstructorArgs[0]; 
                    BCLDebug.Assert(progIdConstructorArg.ArgumentType == typeof(String), "progIdConstructorArg.ArgumentType == typeof(String)");
 
                    String strProgId = (String)progIdConstructorArg.Value;

                    if (strProgId == null)
                        strProgId = String.Empty; 

                    return strProgId; 
                } 
            }
 
            // If there is no prog ID attribute then use the full name of the type as the prog id.
            return type.FullName;
        }
 
        //====================================================================
        // This method binds to the specified moniker. 
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Object BindToMoniker(String monikerName) 
        {
            Object obj = null;
            IBindCtx bindctx = null;
            CreateBindCtx(0, out bindctx); 

            UInt32 cbEaten; 
            IMoniker pmoniker = null; 
            MkParseDisplayName(bindctx, monikerName, out cbEaten, out pmoniker);
 
            BindMoniker(pmoniker, 0, ref IID_IUnknown, out obj);
            return obj;
        }
 
        //====================================================================
        // This method gets the currently running object. 
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Object GetActiveObject(String progID) 
        {
            Object obj = null;
            Guid clsid;
 
            // Call CLSIDFromProgIDEx first then fall back on CLSIDFromProgID if
            // CLSIDFromProgIDEx doesn't exist. 
            try 
            {
                CLSIDFromProgIDEx(progID, out clsid); 
            }
//            catch
            catch(Exception)
            { 
                CLSIDFromProgID(progID, out clsid);
            } 
 
            GetActiveObject(ref clsid, IntPtr.Zero, out obj);
            return obj; 
        }

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
        private static extern void CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] String progId, out Guid clsid); 

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)] 
        private static extern void CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] String progId, out Guid clsid); 

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)] 
        private static extern void CreateBindCtx(UInt32 reserved, out IBindCtx ppbc);

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
        private static extern void MkParseDisplayName(IBindCtx pbc, [MarshalAs(UnmanagedType.LPWStr)] String szUserName, out UInt32 pchEaten, out IMoniker ppmk); 

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)] 
        private static extern void BindMoniker(IMoniker pmk, UInt32 grfOpt, ref Guid iidResult, [MarshalAs(UnmanagedType.Interface)] out Object ppvResult); 

        [DllImport(Microsoft.Win32.Win32Native.OLEAUT32, PreserveSig = false)] 
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.Interface)] out Object ppunk);

        //=======================================================================
        // Private method called from remoting to support ServicedComponents. 
        //========================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern bool InternalSwitchCCW(Object oldtp, Object newtp); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern Object InternalWrapIUnknownWithComObject(IntPtr i);

        //=======================================================================
        // Private method called from EE upon use of license/ICF2 marshaling. 
        //=======================================================================
        private static RuntimeTypeHandle LoadLicenseManager() 
        { 
            Assembly sys = Assembly.Load("System, Version="+ ThisAssembly.Version +
                ", Culture=neutral, PublicKeyToken=" + AssemblyRef.EcmaPublicKeyToken); 
            Type t = sys.GetType("System.ComponentModel.LicenseManager");
            if (t == null || !t.IsVisible)
                return RuntimeTypeHandle.EmptyHandle;
            return t.TypeHandle; 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern void ChangeWrapperHandleStrength(Object otp, bool fIsWeak); 
#endif // FEATURE_COMINTEROP

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Delegate GetDelegateForFunctionPointer(IntPtr ptr, Type t) 
        {
            // Validate the parameters 
            if (ptr == IntPtr.Zero) 
                throw new ArgumentNullException("ptr");
 
            if (t == null)
                throw new ArgumentNullException("t");

            if ((t as RuntimeType) == null) 
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "t");
 
            if (t.IsGenericType) 
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "t");
 
            Type c = t.BaseType;
            if (c == null || (c != typeof(Delegate) && c != typeof(MulticastDelegate)))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "t");
 
            return GetDelegateForFunctionPointerInternal(ptr, t);
        } 
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, Type t); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr GetFunctionPointerForDelegate(Delegate d)
        { 
            if (d == null)
                throw new ArgumentNullException("d"); 
 
            return GetFunctionPointerForDelegateInternal(d);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetFunctionPointerForDelegateInternal(Delegate d);
 
#if !FEATURE_PAL
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr SecureStringToBSTR(SecureString s) { 
            if( s == null) {
                throw new ArgumentNullException("s"); 
            }

            return s.ToBSTR();
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s) { 
            if( s == null) {
                throw new ArgumentNullException("s"); 
            }

            return s.ToAnsiStr(false);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s) { 
            if( s == null) {
                throw new ArgumentNullException("s"); 
            }

            return s.ToAnsiStr(true);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s) { 
            if( s == null) {
                throw new ArgumentNullException("s"); 
            }

            return s.ToUniStr(false);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s) { 
            if( s == null) {
                throw new ArgumentNullException("s"); 
            }

            return s.ToUniStr(true);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void ZeroFreeBSTR(IntPtr s) { 
            Win32Native.ZeroMemory(s, (uint)(Win32Native.SysStringLen(s) * 2));
            FreeBSTR(s); 
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void ZeroFreeCoTaskMemAnsi(IntPtr s) { 
            Win32Native.ZeroMemory(s, (uint)(Win32Native.lstrlenA(s)));
            FreeCoTaskMem(s); 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void ZeroFreeGlobalAllocAnsi(IntPtr s) {
            Win32Native.ZeroMemory(s, (uint)(Win32Native.lstrlenA(s)));
            FreeHGlobal(s);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void ZeroFreeCoTaskMemUnicode(IntPtr s) { 
            Win32Native.ZeroMemory(s, (uint)(Win32Native.lstrlenW(s)*2));
            FreeCoTaskMem(s); 
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void ZeroFreeGlobalAllocUnicode(IntPtr s) { 
            Win32Native.ZeroMemory(s, (uint)(Win32Native.lstrlenW(s)*2));
            FreeHGlobal(s); 
        } 
#endif
    } 

#if FEATURE_COMINTEROP
    //=======================================================================
    // Typelib importer callback implementation. 
    //========================================================================
    internal class ImporterCallback : ITypeLibImporterNotifySink 
    { 
        public void ReportEvent(ImporterEventKind EventKind, int EventCode, String EventMsg)
        { 
        }

        public Assembly ResolveRef(Object TypeLib)
        { 
            try
            { 
                // Create the TypeLibConverter. 
                ITypeLibConverter TLBConv = new TypeLibConverter();
 
                // Convert the typelib.
                return TLBConv.ConvertTypeLibToAssembly(TypeLib,
                                                        Marshal.GetTypeLibName((ITypeLib)TypeLib) + ".dll",
                                                        0, 
                                                        new ImporterCallback(),
                                                        null, 
                                                        null, 
                                                        null,
                                                        null); 
            }
            catch(Exception)
//            catch
            { 
                return null;
            } 
        } 
    }
#endif // FEATURE_COMINTEROP 
}


 

 
 

// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: Marshal 
**
** 
** Purpose: This class contains methods that are mainly used to marshal
**          between unmanaged and managed types.
**
** 
=============================================================================*/
 
namespace System.Runtime.InteropServices 
{
    using System; 
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Security; 
    using System.Security.Permissions;
    using System.Text; 
    using System.Threading; 
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Activation; 
    using System.Runtime.CompilerServices;
    using System.Runtime.Remoting.Proxies;
    using System.Globalization;
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.Versioning;
    using Win32Native = Microsoft.Win32.Win32Native; 
    using Microsoft.Win32.SafeHandles; 
#if FEATURE_COMINTEROP
    using System.Runtime.InteropServices.ComTypes; 
#endif

    //=======================================================================
    // All public methods, including PInvoke, are protected with linkchecks. 
    // Remove the default demands for all PInvoke methods with this global
    // declaration on the class. 
    //======================================================================= 

    [SuppressUnmanagedCodeSecurityAttribute()] 
    public static class Marshal
    {
        //===================================================================
        // Defines used inside the Marshal class. 
        //====================================================================
        private const int LMEM_FIXED = 0; 
#if !FEATURE_PAL 
        private const int LMEM_MOVEABLE = 2;
        private static readonly IntPtr HIWORDMASK = unchecked(new IntPtr((long)0xffffffffffff0000L)); 
#endif //!FEATURE_PAL
#if FEATURE_COMINTEROP
        private static Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
#endif //FEATURE_COMINTEROP 

        // Win32 has the concept of Atoms, where a pointer can either be a pointer 
        // or an int.  If it's less than 64K, this is guaranteed to NOT be a 
        // pointer since the bottom 64K bytes are reserved in a process' page table.
        // We should be careful about deallocating this stuff.  Extracted to 
        // a function to avoid C# problems with lack of support for IntPtr.
        // We have 2 of these methods for slightly different semantics for NULL.
        private static bool IsWin32Atom(IntPtr ptr)
        { 
#if FEATURE_PAL
        return false; 
#else 
            long lPtr = (long)ptr;
            return 0 == (lPtr & (long)HIWORDMASK); 
#endif
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        private static bool IsNotWin32Atom(IntPtr ptr)
        { 
#if FEATURE_PAL 
    return false;
#else 
            long lPtr = (long)ptr;
            return 0 != (lPtr & (long)HIWORDMASK);
#endif
        } 

        //=================================================================== 
        // The default character size for the system. 
        //====================================================================
        public static readonly int SystemDefaultCharSize = 3 - Win32Native.lstrlen(new sbyte [] {0x41, 0x41, 0, 0}); 

        //====================================================================
        // The max DBCS character size for the system.
        //=================================================================== 
        public static readonly int SystemMaxDBCSCharSize = GetSystemMaxDBCSCharSize();
 
 
        //====================================================================
        // The name, title and description of the assembly that will contain 
        // the dynamically generated interop types.
        //===================================================================
        private const String s_strConvertedTypeInfoAssemblyName   = "InteropDynamicTypes";
        private const String s_strConvertedTypeInfoAssemblyTitle  = "Interop Dynamic Types"; 
        private const String s_strConvertedTypeInfoAssemblyDesc   = "Type dynamically generated from ITypeInfo's";
        private const String s_strConvertedTypeInfoNameSpace      = "InteropDynamicTypes"; 
 

        //=================================================================== 
        // Helper method to retrieve the system's maximum DBCS character size.
        //===================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetSystemMaxDBCSCharSize(); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static String PtrToStringAnsi(IntPtr ptr) 
        {
            if (Win32Native.NULL == ptr) { 
                return null;
            }
            else if (IsWin32Atom(ptr)) {
                return null; 
            }
            else { 
                int nb = Win32Native.lstrlenA(ptr); 
                if( nb == 0) {
                    return string.Empty; 
                }
                else {
                    StringBuilder sb = new StringBuilder(nb);
                    Win32Native.CopyMemoryAnsi(sb, ptr, new IntPtr(1+nb)); 
                    return sb.ToString();
                } 
            } 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern String PtrToStringAnsi(IntPtr ptr, int len);
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern String PtrToStringUni(IntPtr ptr, int len); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static String PtrToStringAuto(IntPtr ptr, int len)
        {
            return (SystemDefaultCharSize == 1) ? PtrToStringAnsi(ptr, len) : PtrToStringUni(ptr, len);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static String PtrToStringUni(IntPtr ptr) 
        {
            if (Win32Native.NULL == ptr) { 
                return null;
            }
            else if (IsWin32Atom(ptr)) {
                return null; 
            }
            else { 
                int nc = Win32Native.lstrlenW(ptr); 
                StringBuilder sb = new StringBuilder(nc);
                Win32Native.CopyMemoryUni(sb, ptr, new IntPtr(2*(1+nc))); 
                return sb.ToString();
            }
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String PtrToStringAuto(IntPtr ptr) 
        { 
            if (Win32Native.NULL == ptr) {
                return null; 
            }
            else if (IsWin32Atom(ptr)) {
                return null;
            } 
            else {
                int nc = Win32Native.lstrlen(ptr); 
                StringBuilder sb = new StringBuilder(nc); 
                Win32Native.lstrcpy(sb, ptr);
                return sb.ToString(); 
            }
        }

        //==================================================================== 
        // SizeOf()
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
 
        [System.Runtime.InteropServices.ComVisible(true)]
        public static extern int SizeOf(Object structure);

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern int SizeOf(Type t); 
 

        //==================================================================== 
        // OffsetOf()
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr OffsetOf(Type t, String fieldName) 
        {
            if (t == null) 
                throw new ArgumentNullException("t"); 

            FieldInfo f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); 
            if (f == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetOfFieldNotFound", t.FullName), "fieldName");
            else if (!(f is RuntimeFieldInfo))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeFieldInfo"), "fieldName"); 

            return OffsetOfHelper(((RuntimeFieldInfo)f).GetFieldHandle().Value); 
        } 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr OffsetOfHelper(IntPtr f); 

        //===================================================================
        // UnsafeAddrOfPinnedArrayElement()
        // 
        // IMPORTANT NOTICE: This method does not do any verification on the
        // array. It must be used with EXTREME CAUTION since passing in 
        // an array that is not pinned or in the fixed heap can cause 
        // unexpected results !
        //==================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern IntPtr UnsafeAddrOfPinnedArrayElement(Array arr, int index);
 

        //=================================================================== 
        // Copy blocks from CLR arrays to native memory. 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(int[]     source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(char[]    source, int startIndex, IntPtr destination, int length) 
        { 
            CopyToNative(source, startIndex, destination, length);
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(short[]   source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length); 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(long[]    source, int startIndex, IntPtr destination, int length) 
        {
            CopyToNative(source, startIndex, destination, length); 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(float[]   source, int startIndex, IntPtr destination, int length)
        { 
            CopyToNative(source, startIndex, destination, length);
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(double[]  source, int startIndex, IntPtr destination, int length)
        { 
            CopyToNative(source, startIndex, destination, length);
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(byte[] source, int startIndex, IntPtr destination, int length) 
        {
            CopyToNative(source, startIndex, destination, length); 
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(IntPtr[] source, int startIndex, IntPtr destination, int length) 
        {
            CopyToNative(source, startIndex, destination, length);
        }
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern void CopyToNative(Object source, int startIndex, IntPtr destination, int length);
 
        //=================================================================== 
        // Copy blocks from native memory to CLR arrays
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(IntPtr source, int[]     destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length); 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(IntPtr source, char[]    destination, int startIndex, int length) 
        {
            CopyToManaged(source, destination, startIndex, length); 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(IntPtr source, short[]   destination, int startIndex, int length)
        { 
            CopyToManaged(source, destination, startIndex, length);
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(IntPtr source, long[]    destination, int startIndex, int length)
        { 
            CopyToManaged(source, destination, startIndex, length);
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(IntPtr source, float[]   destination, int startIndex, int length) 
        {
            CopyToManaged(source, destination, startIndex, length); 
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void Copy(IntPtr source, double[]  destination, int startIndex, int length) 
        {
            CopyToManaged(source, destination, startIndex, length);
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(IntPtr source, byte[] destination, int startIndex, int length)
        { 
            CopyToManaged(source, destination, startIndex, length); 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Copy(IntPtr source, IntPtr[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        } 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void CopyToManaged(IntPtr source, Object destination, int startIndex, int length); 
 
        //===================================================================
        // Read from memory 
        //====================================================================
        [DllImport(Win32Native.SHIM, EntryPoint="ND_RU1")]
        [ResourceExposure(ResourceScope.None)]
        public static extern byte ReadByte([MarshalAs(UnmanagedType.AsAny), In] Object ptr, int ofs); 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_RU1")] 
        [ResourceExposure(ResourceScope.None)] 
        public static extern byte ReadByte(IntPtr ptr, int ofs);
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static byte ReadByte(IntPtr ptr)
        {
            return ReadByte(ptr,0); 
        }
 
        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI2")] 
        [ResourceExposure(ResourceScope.None)]
        public static extern short ReadInt16([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs); 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI2")]
        [ResourceExposure(ResourceScope.None)]
        public static extern short ReadInt16(IntPtr ptr, int ofs); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static short ReadInt16(IntPtr ptr) 
        {
            return ReadInt16(ptr, 0); 
        }

        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI4"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [ResourceExposure(ResourceScope.None)] 
        public static extern int ReadInt32([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs);
 
        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI4"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        [ResourceExposure(ResourceScope.None)]
        public static extern int ReadInt32(IntPtr ptr, int ofs); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int ReadInt32(IntPtr ptr)
        { 
            return ReadInt32(ptr,0);
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static IntPtr ReadIntPtr([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs) 
        {
            #if WIN32
                return (IntPtr) ReadInt32(ptr, ofs);
            #else 
                return (IntPtr) ReadInt64(ptr, ofs);
            #endif 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        public static IntPtr ReadIntPtr(IntPtr ptr, int ofs)
        {
            #if WIN32
                return (IntPtr) ReadInt32(ptr, ofs); 
            #else
                return (IntPtr) ReadInt64(ptr, ofs); 
            #endif 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static IntPtr ReadIntPtr(IntPtr ptr)
        {
            #if WIN32 
                return (IntPtr) ReadInt32(ptr, 0);
            #else 
                return (IntPtr) ReadInt64(ptr, 0); 
            #endif
        } 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI8"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern long ReadInt64([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs);
 
        [DllImport(Win32Native.SHIM, EntryPoint="ND_RI8"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern long ReadInt64(IntPtr ptr, int ofs); 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static long ReadInt64(IntPtr ptr) 
        {
            return ReadInt64(ptr,0);
        }
 

        //==================================================================== 
        // Write to memory 
        //===================================================================
        [DllImport(Win32Native.SHIM, EntryPoint="ND_WU1")] 
        public static extern void WriteByte(IntPtr ptr, int ofs, byte val);

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WU1")]
        public static extern void WriteByte([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, byte val); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteByte(IntPtr ptr, byte val) 
        {
            WriteByte(ptr, 0, val); 
        }

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI2")]
        public static extern void WriteInt16(IntPtr ptr, int ofs, short val); 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI2")] 
        public static extern void WriteInt16([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, short val); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteInt16(IntPtr ptr, short val)
        {
            WriteInt16(ptr, 0, val);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteInt16(IntPtr ptr, int ofs, char val) 
        {
            WriteInt16(ptr, ofs, (short)val); 
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void WriteInt16([In,Out]Object ptr, int ofs, char val) 
        {
            WriteInt16(ptr, ofs, (short)val); 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteInt16(IntPtr ptr, char val)
        {
            WriteInt16(ptr, 0, (short)val);
        } 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI4")] 
        public static extern void WriteInt32(IntPtr ptr, int ofs, int val); 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI4")] 
        public static extern void WriteInt32([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, int val);

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void WriteInt32(IntPtr ptr, int val) 
        {
            WriteInt32(ptr,0,val); 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteIntPtr(IntPtr ptr, int ofs, IntPtr val)
        {
            #if WIN32
                WriteInt32(ptr, ofs, (int)val); 
            #else
                WriteInt64(ptr, ofs, (long)val); 
            #endif 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void WriteIntPtr([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, IntPtr val)
        {
            #if WIN32 
                WriteInt32(ptr, ofs, (int)val);
            #else 
                WriteInt64(ptr, ofs, (long)val); 
            #endif
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void WriteIntPtr(IntPtr ptr, IntPtr val)
        { 
            #if WIN32
                WriteInt32(ptr, 0, (int)val); 
            #else 
                WriteInt64(ptr, 0, (long)val);
            #endif 
        }

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI8")]
        public static extern void WriteInt64(IntPtr ptr, int ofs, long val); 

        [DllImport(Win32Native.SHIM, EntryPoint="ND_WI8")] 
        public static extern void WriteInt64([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, long val); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void WriteInt64(IntPtr ptr, long val)
        {
            WriteInt64(ptr, 0, val);
        } 

 
        //==================================================================== 
        // GetLastWin32Error
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern int GetLastWin32Error(); 

 
        //=================================================================== 
        // SetLastWin32Error
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern void SetLastWin32Error(int error);
 

        //==================================================================== 
        // GetHRForLastWin32Error 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int GetHRForLastWin32Error()
        {
            int dwLastError = GetLastWin32Error(); 
            if ((dwLastError & 0x80000000) == 0x80000000)
                return dwLastError; 
            else 
                return (dwLastError & 0x0000FFFF) | unchecked((int)0x80070000);
        } 


        //====================================================================
        // Prelink 
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void Prelink(MethodInfo m) 
        {
            if (m == null) 
                throw new ArgumentNullException("m");

            if (!(m is RuntimeMethodInfo))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo")); 

            RuntimeMethodHandle method = m.MethodHandle; 
 
            InternalPrelink(method.Value);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void InternalPrelink(IntPtr m);
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void PrelinkAll(Type c) 
        { 
            if (c == null)
                throw new ArgumentNullException("c"); 

            MethodInfo[] mi = c.GetMethods();
            if (mi != null)
            { 
                for (int i = 0; i < mi.Length; i++)
                { 
                    Prelink(mi[i]); 
                }
            } 
        }

        //===================================================================
        // NumParamBytes 
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static int NumParamBytes(MethodInfo m) 
        {
            if (m == null) 
                throw new ArgumentNullException("m");

            if (!(m is RuntimeMethodInfo))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo")); 

            RuntimeMethodHandle method = m.GetMethodHandle(); 
 
            return InternalNumParamBytes(method.Value);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int InternalNumParamBytes(IntPtr m);
 
        //===================================================================
        // Win32 Exception stuff 
        // These are mostly interesting for Structured exception handling, 
        // but need to be exposed for all exceptions (not just SEHException).
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
[System.Runtime.InteropServices.ComVisible(true)]
        public static extern /* struct _EXCEPTION_POINTERS* */ IntPtr GetExceptionPointers(); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern int GetExceptionCode();
 

        //===================================================================
        // Marshals data from a structure class to a native memory block.
        // If the structure contains pointers to allocated blocks and 
        // "fDeleteOld" is true, this routine will call DestroyStructure() first.
        //==================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
[System.Runtime.InteropServices.ComVisible(true)] 
        public static extern void StructureToPtr(Object structure, IntPtr ptr, bool fDeleteOld);


        //=================================================================== 
        // Marshals data from a native memory block to a preallocated structure class.
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
[System.Runtime.InteropServices.ComVisible(true)]
        public static void PtrToStructure(IntPtr ptr, Object structure) 
        {
            PtrToStructureHelper(ptr, structure, false);
        }
 

        //==================================================================== 
        // Creates a new instance of "structuretype" and marshals data from a 
        // native memory block to it.
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [System.Runtime.InteropServices.ComVisible(true)]
        public static Object PtrToStructure(IntPtr ptr, Type structureType)
        { 
            if (ptr == Win32Native.NULL) return null;
 
            if (structureType == null) 
                throw new ArgumentNullException("structureType");
 
            if (structureType.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "structureType");

            Object structure = Activator.InternalCreateInstanceWithNoMemberAccessCheck(structureType, true); 
            PtrToStructureHelper(ptr, structure, true);
            return structure; 
        } 

 
        //====================================================================
        // Helper function to copy a pointer into a preallocated structure.
        //===================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern void PtrToStructureHelper(IntPtr ptr, Object structure, bool allowValueClasses);
 
 
        //===================================================================
        // Freeds all substructures pointed to by the native memory block. 
        // "structureclass" is used to provide layout information.
        //===================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
[System.Runtime.InteropServices.ComVisible(true)]
        public static extern void DestroyStructure(IntPtr ptr, Type structuretype); 
 

        //==================================================================== 
        // Returns the HInstance for this module.  Returns -1 if the module
        // doesn't have an HInstance.  In Memory (Dynamic) Modules won't have
        // an HInstance.
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr GetHINSTANCE(Module m) 
        { 
            if (m == null)
                throw new ArgumentNullException("m"); 
            return m.GetHINSTANCE();
        }

 
        //====================================================================
        // Throws a CLR exception based on the HRESULT. 
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void ThrowExceptionForHR(int errorCode) 
        {
            if (errorCode < 0)
                ThrowExceptionForHRInternal(errorCode, Win32Native.NULL);
        } 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void ThrowExceptionForHR(int errorCode, IntPtr errorInfo) 
        { 
            if (errorCode < 0)
                ThrowExceptionForHRInternal(errorCode, errorInfo); 
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ThrowExceptionForHRInternal(int errorCode, IntPtr errorInfo); 

 
        //=================================================================== 
        // Converts the HRESULT to a CLR exception.
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Exception GetExceptionForHR(int errorCode)
        {
            if (errorCode < 0) 
                return GetExceptionForHRInternal(errorCode, Win32Native.NULL);
            else 
                return null; 
        }
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static Exception GetExceptionForHR(int errorCode, IntPtr errorInfo)
        {
            if (errorCode < 0)
                return GetExceptionForHRInternal(errorCode, errorInfo); 
            else
                return null; 
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern Exception GetExceptionForHRInternal(int errorCode, IntPtr errorInfo);


        //=================================================================== 
        // Converts the CLR exception to an HRESULT. This function also sets
        // up an IErrorInfo for the exception. 
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern int GetHRForException(Exception e);

#if !FEATURE_PAL
        //=================================================================== 
        // This method is intended for compiler code generators rather
        // than applications. 
        //==================================================================== 
        //
        [ObsoleteAttribute("The GetUnmanagedThunkForManagedMethodPtr method has been deprecated and will be removed in a future release.", false)] 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern IntPtr GetUnmanagedThunkForManagedMethodPtr(IntPtr pfnMethodToWrap, IntPtr pbSignature, int cbSignature);
 
        //===================================================================
        // This method is intended for compiler code generators rather 
        // than applications. 
        //====================================================================
        // 
        [ObsoleteAttribute("The GetManagedThunkForUnmanagedMethodPtr method has been deprecated and will be removed in a future release.", false)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern IntPtr GetManagedThunkForUnmanagedMethodPtr(IntPtr pfnMethodToWrap, IntPtr pbSignature, int cbSignature); 

        //==================================================================== 
        // The hosting APIs allow a sophisticated host to schedule fibers 
        // onto OS threads, so long as they notify the runtime of this
        // activity.  A fiber cookie can be redeemed for its managed Thread 
        // object by calling the following service.
        //===================================================================
        //
        [ObsoleteAttribute("The GetThreadFromFiberCookie method has been deprecated.  Use the hosting API to perform this operation.", false)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Thread GetThreadFromFiberCookie(int cookie) 
        { 
            if (cookie == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_ArgumentZero"), "cookie"); 

            return InternalGetThreadFromFiberCookie(cookie);
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Thread InternalGetThreadFromFiberCookie(int cookie); 
#endif //!FEATURE_PAL 

 
        //====================================================================
        // Memory allocation and dealocation.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static IntPtr AllocHGlobal(IntPtr cb) 
        { 
            IntPtr pNewMem = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, cb);
 
            if (pNewMem == Win32Native.NULL) {
                throw new OutOfMemoryException();
            }
            return pNewMem; 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static IntPtr AllocHGlobal(int cb) 
        {
            return AllocHGlobal((IntPtr)cb);
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        public static void FreeHGlobal(IntPtr hglobal) 
        {
            if (IsNotWin32Atom(hglobal)) { 
                if (Win32Native.NULL != Win32Native.LocalFree(hglobal)) {
                    ThrowExceptionForHR(GetHRForLastWin32Error());
                }
            } 
        }
 
#if !FEATURE_PAL 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr ReAllocHGlobal(IntPtr pv, IntPtr cb) 
        {
            IntPtr pNewMem = Win32Native.LocalReAlloc(pv, cb, LMEM_MOVEABLE);
            if (pNewMem == Win32Native.NULL) {
                throw new OutOfMemoryException(); 
            }
            return pNewMem; 
        } 

 
        //===================================================================
        // String convertions.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr StringToHGlobalAnsi(String s)
        { 
            if (s == null) 
            {
                return Win32Native.NULL; 
            }
            else
            {
                int nb = (s.Length + 1) * SystemMaxDBCSCharSize; 

                // Overflow checking 
                if (nb < s.Length) 
                    throw new ArgumentOutOfRangeException("s");
 
                IntPtr len = new IntPtr(nb);
                IntPtr hglobal = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, len);

                if (hglobal == Win32Native.NULL) 
                {
                    throw new OutOfMemoryException(); 
                } 
                else
                { 
                    Win32Native.CopyMemoryAnsi(hglobal, s, len);
                    return hglobal;
                }
            } 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr StringToCoTaskMemAnsi(String s)
        { 
            if (s == null)
            {
                return Win32Native.NULL;
            } 
            else
            { 
                int nb = (s.Length + 1) * SystemMaxDBCSCharSize; 

                // Overflow checking 
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException("s");

                IntPtr hglobal = Win32Native.CoTaskMemAlloc(nb); 

                if (hglobal == Win32Native.NULL) 
                { 
                    throw new OutOfMemoryException();
                } 
                else
                {
                    Win32Native.CopyMemoryAnsi(hglobal, s, new IntPtr(nb));
                    return hglobal; 
                }
            } 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr StringToHGlobalUni(String s)
        {
            if (s == null)
            { 
                return Win32Native.NULL;
            } 
            else 
            {
                int nb = (s.Length + 1) * 2; 

                // Overflow checking
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException("s"); 

                IntPtr len = new IntPtr(nb); 
                IntPtr hglobal = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, len); 

                if (hglobal == Win32Native.NULL) 
                {
                    throw new OutOfMemoryException();
                }
                else 
                {
                    Win32Native.CopyMemoryUni(hglobal, s, len); 
                    return hglobal; 
                }
            } 
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr StringToHGlobalAuto(String s) 
        {
            return (SystemDefaultCharSize == 1) ? StringToHGlobalAnsi(s) : StringToHGlobalUni(s); 
        } 
#endif //!FEATURE_PAL
 
#if FEATURE_COMINTEROP

        //====================================================================
        // Given a managed object that wraps a UCOMITypeLib, return its name 
        //===================================================================
        [Obsolete("Use System.Runtime.InteropServices.Marshal.GetTypeLibName(ITypeLib pTLB) instead. http://go.microsoft.com/fwlink/?linkid=14202&ID=0000011.", false)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static String GetTypeLibName(UCOMITypeLib pTLB)
        { 
            return GetTypeLibName((ITypeLib)pTLB);
        }

 
        //====================================================================
        // Given a managed object that wraps an ITypeLib, return its name 
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String GetTypeLibName(ITypeLib typelib) 
        {
            String strTypeLibName = null;
            String strDocString = null;
            int dwHelpContext = 0; 
            String strHelpFile = null;
 
            if (typelib == null) 
                throw new ArgumentNullException("typelib");
 
            typelib.GetDocumentation(-1, out strTypeLibName, out strDocString, out dwHelpContext, out strHelpFile);

            return strTypeLibName;
        } 

 
        //=================================================================== 
        // Given an managed object that wraps an UCOMITypeLib, return its guid
        //==================================================================== 
        [Obsolete("Use System.Runtime.InteropServices.Marshal.GetTypeLibGuid(ITypeLib pTLB) instead. http://go.microsoft.com/fwlink/?linkid=14202&ID=0000011.", false)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Guid GetTypeLibGuid(UCOMITypeLib pTLB)
        { 
            return GetTypeLibGuid((ITypeLib)pTLB);
        } 
 
        //===================================================================
        // Given an managed object that wraps an ITypeLib, return its guid 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Guid GetTypeLibGuid(ITypeLib typelib)
        { 
            Guid result = new Guid ();
            FCallGetTypeLibGuid (ref result, typelib); 
            return result; 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallGetTypeLibGuid(ref Guid result, ITypeLib pTLB);

        //=================================================================== 
        // Given a managed object that wraps a UCOMITypeLib, return its lcid
        //==================================================================== 
        [Obsolete("Use System.Runtime.InteropServices.Marshal.GetTypeLibLcid(ITypeLib pTLB) instead. http://go.microsoft.com/fwlink/?linkid=14202&ID=0000011.", false)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static int GetTypeLibLcid(UCOMITypeLib pTLB) 
        {
            return GetTypeLibLcid((ITypeLib)pTLB);
        }
 
        //===================================================================
        // Given a managed object that wraps an ITypeLib, return its lcid 
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        public static extern int GetTypeLibLcid(ITypeLib typelib);

        //====================================================================
        // Given a managed object that wraps an ITypeLib, return it's 
        // version information.
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void GetTypeLibVersion(ITypeLib typeLibrary, out int major, out int minor);
 
        //====================================================================
        // Given a managed object that wraps an ITypeInfo, return its guid.
        //===================================================================
        internal static Guid GetTypeInfoGuid(ITypeInfo typeInfo) 
        {
            Guid result = new Guid (); 
            FCallGetTypeInfoGuid (ref result, typeInfo); 
            return result;
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallGetTypeInfoGuid(ref Guid result, ITypeInfo typeInfo);
 
        //===================================================================
        // Given a assembly, return the TLBID that will be generated for the 
        // typelib exported from the assembly. 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static Guid GetTypeLibGuidForAssembly(Assembly asm)
        {
            Guid result = new Guid ();
            FCallGetTypeLibGuidForAssembly (ref result, asm == null ? null : asm.InternalAssembly); 
            return result;
        } 
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallGetTypeLibGuidForAssembly(ref Guid result, Assembly asm); 

        //====================================================================
        // Given a assembly, return the version number of the type library
        // that would be exported from the assembly. 
        //===================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern void _GetTypeLibVersionForAssembly(Assembly inputAssembly, out int majorVersion, out int minorVersion); 

        [ResourceExposure(ResourceScope.None)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void GetTypeLibVersionForAssembly(Assembly inputAssembly, out int majorVersion, out int minorVersion)
        {
            _GetTypeLibVersionForAssembly(inputAssembly == null ? null : inputAssembly.InternalAssembly, out majorVersion, out minorVersion); 
        }
 
        //==================================================================== 
        // Given a managed object that wraps an UCOMITypeInfo, return its name
        //==================================================================== 
        [Obsolete("Use System.Runtime.InteropServices.Marshal.GetTypeInfoName(ITypeInfo pTLB) instead. http://go.microsoft.com/fwlink/?linkid=14202&ID=0000011.", false)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String GetTypeInfoName(UCOMITypeInfo pTI)
        { 
            return GetTypeInfoName((ITypeInfo)pTI);
        } 
 
        //===================================================================
        // Given a managed object that wraps an ITypeInfo, return its name 
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String GetTypeInfoName(ITypeInfo typeInfo)
        { 
            String strTypeLibName = null;
            String strDocString = null; 
            int dwHelpContext = 0; 
            String strHelpFile = null;
 
            if (typeInfo == null)
                throw new ArgumentNullException("typeInfo");

            typeInfo.GetDocumentation(-1, out strTypeLibName, out strDocString, out dwHelpContext, out strHelpFile); 

            return strTypeLibName; 
        } 

 

        //===================================================================
        // If a type with the specified GUID is loaded, this method will
        // return the reflection type that represents it. Otherwise it returns 
        // NULL.
        //=================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern Type GetLoadedTypeForGUID(ref Guid guid);
 
        //===================================================================
        // map ITypeInfo* to Type
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static Type GetTypeForITypeInfo(IntPtr /* ITypeInfo* */ piTypeInfo)
        { 
            ITypeInfo pTI = null; 
            ITypeLib pTLB = null;
            Type TypeObj = null; 
            Assembly AsmBldr = null;
            TypeLibConverter TlbConverter = null;
            int Index = 0;
            Guid clsid; 

            // If the input ITypeInfo is NULL then return NULL. 
            if (piTypeInfo == Win32Native.NULL) 
                return null;
 
            // Wrap the ITypeInfo in a CLR object.
            pTI = (ITypeInfo)GetObjectForIUnknown(piTypeInfo);

            // Check to see if a class exists with the specified GUID. 

            clsid = GetTypeInfoGuid(pTI); 
            TypeObj = GetLoadedTypeForGUID(ref clsid); 

            // If we managed to find the type based on the GUID then return it. 
            if (TypeObj != null)
                return TypeObj;

            // There is no type with the specified GUID in the app domain so lets 
            // try and convert the containing typelib.
            try 
            { 
                pTI.GetContainingTypeLib(out pTLB, out Index);
            } 
            catch(COMException)
            {
                pTLB = null;
            } 

            // Check to see if we managed to get a containing typelib. 
            if (pTLB != null) 
            {
                // Get the assembly name from the typelib. 
                AssemblyName AsmName = TypeLibConverter.GetAssemblyNameFromTypelib(pTLB, null, null, null, null, AssemblyNameFlags.None);
                String AsmNameString = AsmName.FullName;

                // Check to see if the assembly that will contain the type already exists. 
                Assembly[] aAssemblies = Thread.GetDomain().GetAssemblies();
                int NumAssemblies = aAssemblies.Length; 
                for (int i = 0; i < NumAssemblies; i++) 
                {
                    if (String.Compare(aAssemblies[i].FullName, 
                                       AsmNameString,StringComparison.Ordinal) == 0)
                        AsmBldr = aAssemblies[i];
                }
 
                // If we haven't imported the assembly yet then import it.
                if (AsmBldr == null) 
                { 
                    TlbConverter = new TypeLibConverter();
                    AsmBldr = TlbConverter.ConvertTypeLibToAssembly(pTLB, 
                        GetTypeLibName(pTLB) + ".dll", 0, new ImporterCallback(), null, null, null, null);
                }

                // Load the type object from the imported typelib. 
                TypeObj = AsmBldr.GetType(GetTypeLibName(pTLB) + "." + GetTypeInfoName(pTI), true, false);
                if (TypeObj != null && !TypeObj.IsVisible) 
                    TypeObj = null; 
            }
            else 
            {
                // If the ITypeInfo does not have a containing typelib then simply
                // return Object as the type.
                TypeObj = typeof(Object); 
            }
 
            return TypeObj; 
        }
 
        //===================================================================
        // map Type to ITypeInfo*
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern IntPtr /* ITypeInfo* */ GetITypeInfoForType(Type t); 
 
        //====================================================================
        // return the IUnknown* for an Object if the current context 
        // is the one where the RCW was first seen. Will return null
        // otherwise.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr /* IUnknown* */ GetIUnknownForObject(Object o)
        { 
            return GetIUnknownForObjectNative(o, false); 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr /* IUnknown* */ GetIUnknownForObjectInContext(Object o)
        {
            return GetIUnknownForObjectNative(o, true); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern IntPtr /* IUnknown* */ GetIUnknownForObjectNative(Object o, bool onlyInContext);
 
        //====================================================================
        // return the IDispatch* for an Object
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr /* IDispatch */ GetIDispatchForObject(Object o)
        { 
            return GetIDispatchForObjectNative(o, false); 
        }
 
        //===================================================================
        // return the IDispatch* for an Object if the current context
        // is the one where the RCW was first seen. Will return null
        // otherwise. 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr /* IUnknown* */ GetIDispatchForObjectInContext(Object o) 
        {
            return GetIDispatchForObjectNative(o, true); 
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IUnknown* */ GetIDispatchForObjectNative(Object o, bool onlyInContext); 

        //==================================================================== 
        // return the IUnknown* representing the interface for the Object 
        // Object o should support Type T
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr /* IUnknown* */ GetComInterfaceForObject(Object o, Type T)
        {
            return GetComInterfaceForObjectNative(o, T, false); 
        }
 
        //==================================================================== 
        // return the IUnknown* representing the interface for the Object
        // Object o should support Type T if the current context 
        // is the one where the RCW was first seen. Will return null
        // otherwise.
        //====================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr /* IUnknown* */ GetComInterfaceForObjectInContext(Object o, Type t)
        { 
            return GetComInterfaceForObjectNative(o, t, true); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IUnknown* */ GetComInterfaceForObjectNative(Object o, Type t, bool onlyInContext);

        //=================================================================== 
        // return an Object for IUnknown
        //==================================================================== 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern Object GetObjectForIUnknown(IntPtr /* IUnknown* */ pUnk); 

        //===================================================================
        // Return a unique Object given an IUnknown.  This ensures that you
        //  receive a fresh object (we will not look in the cache to match up this 
        //  IUnknown to an already existing object).  This is useful in cases
        //  where you want to be able to call ReleaseComObject on a RCW 
        //  and not worry about other active uses of said RCW. 
        //===================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern Object GetUniqueObjectForIUnknown(IntPtr unknown);

 
        //===================================================================
        // return an Object for IUnknown, using the Type T, 
        //  NOTE: 
        //  Type T should be either a COM imported Type or a sub-type of COM
        //  imported Type 
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern Object GetTypedObjectForIUnknown(IntPtr /* IUnknown* */ pUnk, Type t); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern IntPtr CreateAggregatedObject(IntPtr pOuter, Object o);
 
#endif // FEATURE_COMINTEROP


        //=================================================================== 
        // check if the object is classic COM component
        //==================================================================== 
#if FEATURE_COMINTEROP 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern bool IsComObject(Object o);
#else
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static bool IsComObject(Object o) 
        {
            return false; 
        } 
#endif // FEATURE_COMINTEROP
 

#if FEATURE_COMINTEROP
        //====================================================================
        // release the COM component and if the reference hits 0 zombie this object 
        // further usage of this Object might throw an exception
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static int ReleaseComObject(Object o)
        { 
            __ComObject co = null;

            // Make sure the obj is an __ComObject.
            try 
            {
                co = (__ComObject)o; 
            } 
            catch (InvalidCastException)
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "o");
            }

            return co.ReleaseSelf(); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern int InternalReleaseComObject(Object o);
 

        //====================================================================
        // release the COM component and zombie this object
        // further usage of this Object might throw an exception 
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static Int32 FinalReleaseComObject(Object o) 
        {
            __ComObject co = null; 

            if (o == null)
                throw new ArgumentNullException("o");
 
            // Make sure the obj is an __ComObject.
            try 
            { 
                co = (__ComObject)o;
            } 
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "o");
            } 

            co.FinalReleaseSelf(); 
 
            return 0;
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InternalFinalReleaseComObject(Object o);
 
        //===================================================================
        // This method retrieves data from the COM object. 
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Object GetComObjectData(Object obj, Object key) 
        {
            __ComObject comObj = null;

            // Validate that the arguments aren't null. 
            if (obj == null)
                throw new ArgumentNullException("obj"); 
            if (key == null) 
                throw new ArgumentNullException("key");
 
            // Make sure the obj is an __ComObject.
            try
            {
                comObj = (__ComObject)obj; 
            }
            catch (InvalidCastException) 
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "obj");
            } 

            // Retrieve the data from the __ComObject.
            return comObj.GetData(key);
        } 

        //==================================================================== 
        // This method sets data on the COM object. The data can only be set 
        // once for a given key and cannot be removed. This function returns
        // true if the data has been added, false if the data could not be 
        // added because there already was data for the specified key.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static bool SetComObjectData(Object obj, Object key, Object data) 
        {
            __ComObject comObj = null; 
 
            // Validate that the arguments aren't null. The data can validly be null.
            if (obj == null) 
                throw new ArgumentNullException("obj");
            if (key == null)
                throw new ArgumentNullException("key");
 
            // Make sure the obj is an __ComObject.
            try 
            { 
                comObj = (__ComObject)obj;
            } 
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "obj");
            } 

            // Retrieve the data from the __ComObject. 
            return comObj.SetData(key, data); 
        }
 
        //====================================================================
        // This method takes the given COM object and wraps it in an object
        // of the specified type. The type must be derived from __ComObject.
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Object CreateWrapperOfType(Object o, Type t) 
        { 
            // Validate the arguments.
            if (t == null) 
                throw new ArgumentNullException("t");
            if (!t.IsCOMObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeNotComObject"), "t");
            if (t.IsGenericType) 
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "t");
 
            // Check for the null case. 
            if (o == null)
                return null; 

            // Make sure the object is a COM object.
            if (!o.GetType().IsCOMObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "o"); 

            // Check to see if the type of the object is the requested type. 
            if (o.GetType() == t) 
                return o;
 
            // Check to see if we already have a cached wrapper for this type.
            Object Wrapper = GetComObjectData(o, t);
            if (Wrapper == null)
            { 
                // Create the wrapper for the specified type.
                Wrapper = InternalCreateWrapperOfType(o, t); 
 
                // Attempt to cache the wrapper on the object.
                if (!SetComObjectData(o, t, Wrapper)) 
                {
                    // Another thead already cached the wrapper so use that one instead.
                    Wrapper = GetComObjectData(o, t);
                } 
            }
 
            return Wrapper; 
        }
 
        //===================================================================
        // Helper method called from CreateWrapperOfType.
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        private static extern Object InternalCreateWrapperOfType(Object o, Type t); 
 
        //===================================================================
        // There may be a thread-based cache of COM components.  This service can 
        // force the aggressive release of the current thread's cache.
        //===================================================================
        //
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        [Obsolete("This API did not perform any operation and will be removed in future versions of the CLR.", false)]
        public static void ReleaseThreadCache() 
        { 
        }
 
        //===================================================================
        // check if the type is visible from COM.
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern bool IsTypeVisibleFromCom(Type t); 
 
        //===================================================================
        // IUnknown Helpers 
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern int /* HRESULT */ QueryInterface(IntPtr /* IUnknown */ pUnk, ref Guid iid, out IntPtr ppv); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern int /* ULONG */ AddRef(IntPtr /* IUnknown */ pUnk );
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern int /* ULONG */ Release(IntPtr /* IUnknown */ pUnk );
 
        //====================================================================
        // BSTR allocation and dealocation. 
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr AllocCoTaskMem(int cb) 
        {
            IntPtr pNewMem = Win32Native.CoTaskMemAlloc(cb);
            if (pNewMem == Win32Native.NULL) {
                throw new OutOfMemoryException(); 
            }
            return pNewMem; 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr ReAllocCoTaskMem(IntPtr pv, int cb)
        {
            IntPtr pNewMem = Win32Native.CoTaskMemRealloc(pv, cb);
            if (pNewMem == Win32Native.NULL && cb != 0) { 
                throw new OutOfMemoryException();
            } 
            return pNewMem; 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void FreeCoTaskMem(IntPtr ptr)
        {
            if (IsNotWin32Atom(ptr)) { 
                Win32Native.CoTaskMemFree(ptr);
            } 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void FreeBSTR(IntPtr ptr)
        {
            if (IsNotWin32Atom(ptr)) {
                Win32Native.SysFreeString(ptr); 
            }
        } 
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr StringToCoTaskMemUni(String s) 
        {
            if (s == null)
            {
                return Win32Native.NULL; 
            }
            else 
            { 
                int nb = (s.Length + 1) * 2;
 
                // Overflow checking
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException("s");
 
                IntPtr hglobal = Win32Native.CoTaskMemAlloc(nb);
 
                if (hglobal == Win32Native.NULL) 
                {
                    throw new OutOfMemoryException(); 
                }
                else
                {
                    Win32Native.CopyMemoryUni(hglobal, s, new IntPtr(nb)); 
                    return hglobal;
                } 
            } 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr StringToCoTaskMemAuto(String s)
        {
            if (s == null) 
            {
                return Win32Native.NULL; 
            } 
            else
            { 
                int nb = (s.Length + 1) * SystemDefaultCharSize;

                // Overflow checking
                if (nb < s.Length) 
                    throw new ArgumentOutOfRangeException("s");
 
                IntPtr hglobal = Win32Native.CoTaskMemAlloc(nb); 
                if (hglobal == Win32Native.NULL)
                { 
                    throw new OutOfMemoryException();
                }
                else
                { 
                    Win32Native.lstrcpy(hglobal, s);
                    return hglobal; 
                } 
            }
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr StringToBSTR(String s)
        { 
            if (s == null)
                return Win32Native.NULL; 
 
            // Overflow checking
            if (s.Length+1 < s.Length) 
                throw new ArgumentOutOfRangeException("s");

            IntPtr bstr = Win32Native.SysAllocStringLen(s, s.Length);
            if (bstr == Win32Native.NULL) 
                throw new OutOfMemoryException();
 
            return bstr; 
        }
 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String PtrToStringBSTR(IntPtr ptr)
        {
            return PtrToStringUni(ptr, Win32Native.SysStringLen(ptr)); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern void GetNativeVariantForObject(Object obj, /* VARIANT * */ IntPtr pDstNativeVariant); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern Object GetObjectForNativeVariant(/* VARIANT * */ IntPtr pSrcNativeVariant ); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern Object[] GetObjectsForNativeVariants(/* VARIANT * */ IntPtr aSrcNativeVariant, int cVars );
 
        /// <summary>
        /// <para>Returns the first valid COM slot that GetMethodInfoForSlot will work on
        /// This will be 3 for IUnknown based interfaces and 7 for IDispatch based interfaces. </para>
        /// </summary> 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern int GetStartComSlot(Type t); 

        /// <summary> 
        /// <para>Returns the last valid COM slot that GetMethodInfoForSlot will work on. </para>
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern int GetEndComSlot(Type t);
 
        /// <summary> 
        /// <para>Returns the MemberInfo that COM callers calling through the exposed
        /// vtable on the given slot will be calling. The slot should take into account 
        /// if the exposed interface is IUnknown based or IDispatch based.
        /// For classes, the lookup is done on the default interface that will be
        /// exposed for the class. </para>
        /// </summary> 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static extern MemberInfo GetMethodInfoForComSlot(Type t, int slot, ref ComMemberType memberType); 

        /// <summary> 
        /// <para>Returns the COM slot for a memeber info, taking into account whether
        /// the exposed interface is IUnknown based or IDispatch based</para>
        /// </summary>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static int GetComSlotForMethodInfo(MemberInfo m)
        { 
            if (m== null) 
                throw new ArgumentNullException("m");
            if (!(m is RuntimeMethodInfo)) 
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "m");
            if (!m.DeclaringType.IsInterface)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeInterfaceMethod"), "m");
            if (m.DeclaringType.IsGenericType) 
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "m");
 
            RuntimeMethodHandle method = ((RuntimeMethodInfo)m).GetMethodHandle(); 
            BCLDebug.Assert(!method.IsNullHandle(), "!method.IsNullHandle()");
 
            return InternalGetComSlotForMethodInfo(method);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern int InternalGetComSlotForMethodInfo(RuntimeMethodHandle m);
 
        //==================================================================== 
        // This method generates a GUID for the specified type. If the type
        // has a GUID in the metadata then it is returned otherwise a stable 
        // guid GUID is generated based on the fully qualified name of the
        // type.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static Guid GenerateGuidForType(Type type)
        { 
            Guid result = new Guid (); 
            FCallGenerateGuidForType (ref result, type);
            return result; 
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallGenerateGuidForType(ref Guid result, Type type); 

        //=================================================================== 
        // This method generates a PROGID for the specified type. If the type 
        // has a PROGID in the metadata then it is returned otherwise a stable
        // PROGID is generated based on the fully qualified name of the 
        // type.
        //===================================================================
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static String GenerateProgIdForType(Type type) 
        {
            if (type == null) 
                throw new ArgumentNullException("type"); 
            if (!RegistrationServices.TypeRequiresRegistrationHelper(type))
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustBeComCreatable"), "type"); 
            if (type.IsImport)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustNotBeComImport"), "type");
            if (type.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "type"); 

 
            IList<CustomAttributeData> cas = CustomAttributeData.GetCustomAttributes(type); 
            for (int i = 0; i < cas.Count; i ++)
            { 
                if (cas[i].Constructor.DeclaringType == typeof(ProgIdAttribute))
                {
                    // Retrieve the PROGID string from the ProgIdAttribute.
                    IList<CustomAttributeTypedArgument> caConstructorArgs = cas[i].ConstructorArguments; 
                    BCLDebug.Assert(caConstructorArgs.Count == 1, "caConstructorArgs.Count == 1");
 
                    CustomAttributeTypedArgument progIdConstructorArg = caConstructorArgs[0]; 
                    BCLDebug.Assert(progIdConstructorArg.ArgumentType == typeof(String), "progIdConstructorArg.ArgumentType == typeof(String)");
 
                    String strProgId = (String)progIdConstructorArg.Value;

                    if (strProgId == null)
                        strProgId = String.Empty; 

                    return strProgId; 
                } 
            }
 
            // If there is no prog ID attribute then use the full name of the type as the prog id.
            return type.FullName;
        }
 
        //====================================================================
        // This method binds to the specified moniker. 
        //=================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Object BindToMoniker(String monikerName) 
        {
            Object obj = null;
            IBindCtx bindctx = null;
            CreateBindCtx(0, out bindctx); 

            UInt32 cbEaten; 
            IMoniker pmoniker = null; 
            MkParseDisplayName(bindctx, monikerName, out cbEaten, out pmoniker);
 
            BindMoniker(pmoniker, 0, ref IID_IUnknown, out obj);
            return obj;
        }
 
        //====================================================================
        // This method gets the currently running object. 
        //==================================================================== 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Object GetActiveObject(String progID) 
        {
            Object obj = null;
            Guid clsid;
 
            // Call CLSIDFromProgIDEx first then fall back on CLSIDFromProgID if
            // CLSIDFromProgIDEx doesn't exist. 
            try 
            {
                CLSIDFromProgIDEx(progID, out clsid); 
            }
//            catch
            catch(Exception)
            { 
                CLSIDFromProgID(progID, out clsid);
            } 
 
            GetActiveObject(ref clsid, IntPtr.Zero, out obj);
            return obj; 
        }

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
        private static extern void CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] String progId, out Guid clsid); 

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)] 
        private static extern void CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] String progId, out Guid clsid); 

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)] 
        private static extern void CreateBindCtx(UInt32 reserved, out IBindCtx ppbc);

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
        private static extern void MkParseDisplayName(IBindCtx pbc, [MarshalAs(UnmanagedType.LPWStr)] String szUserName, out UInt32 pchEaten, out IMoniker ppmk); 

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)] 
        private static extern void BindMoniker(IMoniker pmk, UInt32 grfOpt, ref Guid iidResult, [MarshalAs(UnmanagedType.Interface)] out Object ppvResult); 

        [DllImport(Microsoft.Win32.Win32Native.OLEAUT32, PreserveSig = false)] 
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.Interface)] out Object ppunk);

        //=======================================================================
        // Private method called from remoting to support ServicedComponents. 
        //========================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern bool InternalSwitchCCW(Object oldtp, Object newtp); 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern Object InternalWrapIUnknownWithComObject(IntPtr i);

        //=======================================================================
        // Private method called from EE upon use of license/ICF2 marshaling. 
        //=======================================================================
        private static RuntimeTypeHandle LoadLicenseManager() 
        { 
            Assembly sys = Assembly.Load("System, Version="+ ThisAssembly.Version +
                ", Culture=neutral, PublicKeyToken=" + AssemblyRef.EcmaPublicKeyToken); 
            Type t = sys.GetType("System.ComponentModel.LicenseManager");
            if (t == null || !t.IsVisible)
                return RuntimeTypeHandle.EmptyHandle;
            return t.TypeHandle; 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static extern void ChangeWrapperHandleStrength(Object otp, bool fIsWeak); 
#endif // FEATURE_COMINTEROP

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static Delegate GetDelegateForFunctionPointer(IntPtr ptr, Type t) 
        {
            // Validate the parameters 
            if (ptr == IntPtr.Zero) 
                throw new ArgumentNullException("ptr");
 
            if (t == null)
                throw new ArgumentNullException("t");

            if ((t as RuntimeType) == null) 
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "t");
 
            if (t.IsGenericType) 
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "t");
 
            Type c = t.BaseType;
            if (c == null || (c != typeof(Delegate) && c != typeof(MulticastDelegate)))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "t");
 
            return GetDelegateForFunctionPointerInternal(ptr, t);
        } 
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, Type t); 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static IntPtr GetFunctionPointerForDelegate(Delegate d)
        { 
            if (d == null)
                throw new ArgumentNullException("d"); 
 
            return GetFunctionPointerForDelegateInternal(d);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetFunctionPointerForDelegateInternal(Delegate d);
 
#if !FEATURE_PAL
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr SecureStringToBSTR(SecureString s) { 
            if( s == null) {
                throw new ArgumentNullException("s"); 
            }

            return s.ToBSTR();
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s) { 
            if( s == null) {
                throw new ArgumentNullException("s"); 
            }

            return s.ToAnsiStr(false);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s) { 
            if( s == null) {
                throw new ArgumentNullException("s"); 
            }

            return s.ToAnsiStr(true);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s) { 
            if( s == null) {
                throw new ArgumentNullException("s"); 
            }

            return s.ToUniStr(false);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s) { 
            if( s == null) {
                throw new ArgumentNullException("s"); 
            }

            return s.ToUniStr(true);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void ZeroFreeBSTR(IntPtr s) { 
            Win32Native.ZeroMemory(s, (uint)(Win32Native.SysStringLen(s) * 2));
            FreeBSTR(s); 
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void ZeroFreeCoTaskMemAnsi(IntPtr s) { 
            Win32Native.ZeroMemory(s, (uint)(Win32Native.lstrlenA(s)));
            FreeCoTaskMem(s); 
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void ZeroFreeGlobalAllocAnsi(IntPtr s) {
            Win32Native.ZeroMemory(s, (uint)(Win32Native.lstrlenA(s)));
            FreeHGlobal(s);
        } 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        public static void ZeroFreeCoTaskMemUnicode(IntPtr s) { 
            Win32Native.ZeroMemory(s, (uint)(Win32Native.lstrlenW(s)*2));
            FreeCoTaskMem(s); 
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        public static void ZeroFreeGlobalAllocUnicode(IntPtr s) { 
            Win32Native.ZeroMemory(s, (uint)(Win32Native.lstrlenW(s)*2));
            FreeHGlobal(s); 
        } 
#endif
    } 

#if FEATURE_COMINTEROP
    //=======================================================================
    // Typelib importer callback implementation. 
    //========================================================================
    internal class ImporterCallback : ITypeLibImporterNotifySink 
    { 
        public void ReportEvent(ImporterEventKind EventKind, int EventCode, String EventMsg)
        { 
        }

        public Assembly ResolveRef(Object TypeLib)
        { 
            try
            { 
                // Create the TypeLibConverter. 
                ITypeLibConverter TLBConv = new TypeLibConverter();
 
                // Convert the typelib.
                return TLBConv.ConvertTypeLibToAssembly(TypeLib,
                                                        Marshal.GetTypeLibName((ITypeLib)TypeLib) + ".dll",
                                                        0, 
                                                        new ImporterCallback(),
                                                        null, 
                                                        null, 
                                                        null,
                                                        null); 
            }
            catch(Exception)
//            catch
            { 
                return null;
            } 
        } 
    }
#endif // FEATURE_COMINTEROP 
}


 

 
 

