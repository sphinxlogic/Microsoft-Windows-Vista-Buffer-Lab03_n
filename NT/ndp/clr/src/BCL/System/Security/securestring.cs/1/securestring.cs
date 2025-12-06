namespace System.Security { 
    using System.Security.Cryptography;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Win32; 
    using System.Runtime.CompilerServices;
    using System.Security.Permissions; 
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.Versioning;
    using Microsoft.Win32.SafeHandles;	 


    public sealed class SecureString: IDisposable {
        private SafeBSTRHandle m_buffer; 
        private int       m_length;
        private bool     m_readOnly; 
        private bool     m_enrypted; 

        static bool supportedOnCurrentPlatform = EncryptionSupported(); 

        const int BlockSize = (int)Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE /2;  // a char is two bytes
        const int MaxLength = 65536;
        const uint ProtectionScope = Win32Native.CRYPTPROTECTMEMORY_SAME_PROCESS; 

        unsafe static bool EncryptionSupported() { 
            // check if the enrypt/decrypt function is supported on current OS 
            bool supported = true;
            try { 
                Win32Native.SystemFunction041(
 			        SafeBSTRHandle.Allocate(null , (int)Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE),
			        Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE,
			        Win32Native.CRYPTPROTECTMEMORY_SAME_PROCESS); 
            }
            catch (EntryPointNotFoundException) { 
                supported = false; 
            }
            return supported; 
        }

        internal SecureString(SecureString str) {
            AllocateBuffer(str.BufferLength); 
            SafeBSTRHandle.Copy(str.m_buffer, this.m_buffer);
            m_length = str.m_length; 
            m_enrypted = str.m_enrypted; 
        }
 

        public SecureString() {
            CheckSupportedOnCurrentPlatform();
 
            // allocate the minimum block size for calling protectMemory
            AllocateBuffer(BlockSize); 
            m_length = 0; 
        }
 

        [CLSCompliant(false)]
        public unsafe SecureString(char* value, int length) {
            if( value == null) { 
                throw new ArgumentNullException("value");
            } 
 
            if( length < 0) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum")); 
            }

            if( length > MaxLength) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Length")); 
            }
 
            CheckSupportedOnCurrentPlatform(); 

            AllocateBuffer(length); 

            byte* bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try 
            {
                m_buffer.AcquirePointer(ref bufferPtr); 
                Buffer.memcpyimpl((byte*)value, bufferPtr, length * 2); 
            }
            finally 
            {
                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            } 

            m_length = length; 
            ProtectMemory(); 
        }
 
        public int Length {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            get  {
                EnsureNotDisposed(); 
                return m_length;
            } 
        } 

        [MethodImplAttribute(MethodImplOptions.Synchronized)] 
        public void AppendChar(char c) {
            EnsureNotDisposed();
            EnsureNotReadOnly();
 
            EnsureCapacity(m_length + 1);
 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                UnProtectMemory(); 
                m_buffer.Write<char>((uint)m_length * sizeof(char), c);
                m_length++;
            }
            finally { 
                ProtectMemory();
            } 
        } 

        // clears the current contents. Only available if writable 
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void Clear() {
            EnsureNotDisposed();
            EnsureNotReadOnly(); 

            m_length = 0; 
            m_buffer.ClearBuffer(); 
            m_enrypted = false;
        } 

        // Do a deep-copy of the SecureString
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public SecureString Copy() { 
            EnsureNotDisposed();
            return new SecureString(this); 
        } 

        [MethodImplAttribute(MethodImplOptions.Synchronized)] 
        public void Dispose() {
            if(m_buffer != null && !m_buffer.IsInvalid) {
                m_buffer.Close();
                m_buffer = null; 
            }
        } 
 
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void InsertAt( int index, char c ) { 
            EnsureNotDisposed();
            EnsureNotReadOnly();

            if( index < 0 || index > m_length) { 
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            } 
 
            EnsureCapacity(m_length + 1);
 
            unsafe {
                byte* bufferPtr = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    UnProtectMemory();
                    m_buffer.AcquirePointer(ref bufferPtr); 
                    char* pBuffer = (char*)bufferPtr; 

                    for (int i = m_length; i > index; i--) { 
                        pBuffer[i] = pBuffer[i - 1];
                    }
                    pBuffer[index] = c;
                    ++m_length; 
                }
                finally { 
                    ProtectMemory(); 
                    if (bufferPtr != null)
                        m_buffer.ReleasePointer(); 
                }
            }
        }
 
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public bool IsReadOnly() { 
            EnsureNotDisposed(); 
            return m_readOnly;
        } 

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void MakeReadOnly() {
            EnsureNotDisposed(); 
            m_readOnly = true;
        } 
 
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void RemoveAt( int index ) { 
            EnsureNotDisposed();
            EnsureNotReadOnly();

            if( index < 0 || index >= m_length) { 
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            } 
 
            unsafe
            { 
                byte* bufferPtr = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { 
                    UnProtectMemory();
                    m_buffer.AcquirePointer(ref bufferPtr); 
                    char* pBuffer = (char*)bufferPtr; 

                    for (int i = index; i < m_length - 1; i++) 
                    {
                        pBuffer[i] = pBuffer[i + 1];
                    }
                    pBuffer[--m_length] = (char)0; 
                }
                finally 
                { 
                    ProtectMemory();
                    if (bufferPtr != null) 
                        m_buffer.ReleasePointer();
                }
            }
        } 

        [MethodImplAttribute(MethodImplOptions.Synchronized)] 
        public void SetAt( int index, char c ) { 
            EnsureNotDisposed();
            EnsureNotReadOnly(); 

            if( index < 0 || index >= m_length) {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            } 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
                UnProtectMemory();
                m_buffer.Write<char>((uint)index * sizeof(char), c); 
            }
            finally {
                ProtectMemory();
            } 
        }
 
        private int BufferLength { 
            get {
                BCLDebug.Assert(m_buffer != null, "Buffer is not initialized!"); 
                return m_buffer.Length;
            }
        }
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private void AllocateBuffer(int size) { 
            uint alignedSize = GetAlignedSize(size); 

            m_buffer = SafeBSTRHandle.Allocate(null, alignedSize); 
            if (m_buffer.IsInvalid) {
                throw new OutOfMemoryException();
            }
        } 

        private void CheckSupportedOnCurrentPlatform() { 
            if( !supportedOnCurrentPlatform) { 
                throw new NotSupportedException(Environment.GetResourceString("Arg_PlatformSecureString"));
            } 
        }

        private void EnsureCapacity(int capacity) {
            if( capacity <= m_buffer.Length) { 
                return;
            } 
            if( capacity > MaxLength) { 
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
            } 

            SafeBSTRHandle newBuffer = SafeBSTRHandle.Allocate(null, GetAlignedSize(capacity));

            if (newBuffer.IsInvalid) { 
                throw new OutOfMemoryException();
            } 
 
            SafeBSTRHandle.Copy(m_buffer, newBuffer);
            m_buffer.Close(); 
            m_buffer = newBuffer;
        }

        private void EnsureNotDisposed() { 
            if( m_buffer == null) {
                throw new ObjectDisposedException(null); 
            } 
        }
 
        private void EnsureNotReadOnly() {
            if( m_readOnly) {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            } 
        }
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        private static uint GetAlignedSize( int size) {
            BCLDebug.Assert(size >= 0, "size must be non-negative"); 

            uint alignedSize = ((uint)size / BlockSize) * BlockSize;
            if( (size % BlockSize != 0) || size == 0) {  // if size is 0, set allocated size to blocksize
                alignedSize += BlockSize; 
            }
            return alignedSize; 
        } 

        private unsafe int GetAnsiByteCount() { 
            const uint CP_ACP               = 0;
            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;

            uint flgs = WC_NO_BEST_FIT_CHARS; 
            uint DefaultCharUsed = (uint)'?';
 
            byte* bufferPtr = null; 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                m_buffer.AcquirePointer(ref bufferPtr);

                return Win32Native.WideCharToMultiByte(
                    CP_ACP, 
                    flgs,
                    (char*) bufferPtr, 
                    m_length, 
                    null,
                    0, 
                    IntPtr.Zero,
                    new IntPtr((void*)&DefaultCharUsed));
            }
            finally { 
                if (bufferPtr != null)
                    m_buffer.ReleasePointer(); 
            } 
        }
 
        private unsafe void GetAnsiBytes( byte * ansiStrPtr, int byteCount) {
            const uint CP_ACP               = 0;
            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;
 
            uint flgs = WC_NO_BEST_FIT_CHARS;
            uint DefaultCharUsed = (uint)'?'; 
 
            byte* bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                m_buffer.AcquirePointer(ref bufferPtr);

                Win32Native.WideCharToMultiByte( 
                    CP_ACP,
                    flgs, 
                    (char*) bufferPtr, 
                    m_length,
                    ansiStrPtr, 
                    byteCount - 1,
                    IntPtr.Zero,
                    new IntPtr((void*)&DefaultCharUsed));
 
                *(ansiStrPtr + byteCount - 1) = (byte)0;
            } 
            finally { 
                if (bufferPtr != null)
                    m_buffer.ReleasePointer(); 
            }
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)] 
        private void ProtectMemory() {
            BCLDebug.Assert(!m_buffer.IsInvalid && m_buffer.Length != 0, "Invalid buffer!"); 
            BCLDebug.Assert(m_buffer.Length % BlockSize == 0, "buffer length must be multiple of blocksize!"); 

            if( m_length == 0 || m_enrypted) { 
                return;
            }

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
            } 
            finally { 
                // RtlEncryptMemory return an NTSTATUS
                int status = Win32Native.SystemFunction040(m_buffer, (uint)m_buffer.Length * 2, ProtectionScope); 
                if (status < 0)  { // non-negative numbers indicate success
                    throw new CryptographicException(Win32Native.LsaNtStatusToWinError(status));
                }
                m_enrypted = true; 
            }
        } 
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [MethodImplAttribute(MethodImplOptions.Synchronized)] 
        internal unsafe IntPtr ToBSTR() {
            EnsureNotDisposed();
            int length = m_length;
            IntPtr ptr = IntPtr.Zero; 
            IntPtr result = IntPtr.Zero;
            byte* bufferPtr = null; 
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                }
                finally { 
                    ptr = Win32Native.SysAllocStringLen(null, length);
                } 
 
                if (ptr == IntPtr.Zero) {
                    throw new OutOfMemoryException(); 
                }

                UnProtectMemory();
                m_buffer.AcquirePointer(ref bufferPtr); 
                Buffer.memcpyimpl(bufferPtr, (byte*) ptr.ToPointer(), length *2);
                result = ptr; 
            } 
            finally {
                ProtectMemory(); 
                if( result == IntPtr.Zero) {
                    // If we failed for any reason, free the new buffer
                    if (ptr != IntPtr.Zero) {
                        Win32Native.ZeroMemory(ptr, (uint)(length * 2)); 
                        Win32Native.SysFreeString(ptr);
                    } 
                } 
                if (bufferPtr != null)
                    m_buffer.ReleasePointer(); 
            }
            return result;
        }
 
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal unsafe IntPtr ToUniStr(bool allocateFromHeap) { 
            EnsureNotDisposed(); 
            int length = m_length;
            IntPtr ptr = IntPtr.Zero; 
            IntPtr result = IntPtr.Zero;
            byte* bufferPtr = null;

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try { 
                }
                finally { 
                    if( allocateFromHeap) {
                        ptr = Marshal.AllocHGlobal((length + 1) * 2);
                    }
                    else { 
                        ptr = Marshal.AllocCoTaskMem((length + 1) * 2);
                    } 
                } 

                if (ptr == IntPtr.Zero) { 
                    throw new OutOfMemoryException();
                }

                UnProtectMemory(); 
                m_buffer.AcquirePointer(ref bufferPtr);
                Buffer.memcpyimpl(bufferPtr, (byte*) ptr.ToPointer(), length *2); 
                char * endptr = (char *) ptr.ToPointer(); 
                *(endptr + length) = '\0';
                result = ptr; 
            }
            finally {
                ProtectMemory();
 
                if( result == IntPtr.Zero) {
                    // If we failed for any reason, free the new buffer 
                    if (ptr != IntPtr.Zero) { 
                        Win32Native.ZeroMemory(ptr, (uint)(length * 2));
                        if( allocateFromHeap) { 
                            Marshal.FreeHGlobal(ptr);
                        }
                        else {
                            Marshal.FreeCoTaskMem(ptr); 
                        }
                    } 
                } 

                if (bufferPtr != null) 
                    m_buffer.ReleasePointer();
            }
            return result;
        } 

        [MethodImplAttribute(MethodImplOptions.Synchronized)] 
        internal unsafe IntPtr ToAnsiStr(bool allocateFromHeap) { 
            EnsureNotDisposed();
 
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            int byteCount = 0;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                // GetAnsiByteCount uses the string data, so the calculation must happen after we are decrypted. 
                UnProtectMemory(); 

                // allocating an extra char for terminating zero 
                byteCount = GetAnsiByteCount() + 1;

                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                }
                finally { 
                    if( allocateFromHeap) { 
                        ptr = Marshal.AllocHGlobal(byteCount);
                    } 
                    else {
                        ptr = Marshal.AllocCoTaskMem(byteCount);
                    }
                } 

                if (ptr == IntPtr.Zero) { 
                    throw new OutOfMemoryException(); 
                }
 
                GetAnsiBytes((byte *)ptr.ToPointer(), byteCount);
                result = ptr;
            }
            finally { 
                ProtectMemory();
                if( result == IntPtr.Zero) { 
                    // If we failed for any reason, free the new buffer 
                    if (ptr != IntPtr.Zero) {
                        Win32Native.ZeroMemory(ptr, (uint)byteCount); 
                        if( allocateFromHeap) {
                            Marshal.FreeHGlobal(ptr);
                        }
                        else { 
                            Marshal.FreeCoTaskMem(ptr);
                        } 
                    } 
                }
 
            }
            return result;
        }
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private void UnProtectMemory() { 
            BCLDebug.Assert(!m_buffer.IsInvalid && m_buffer.Length != 0, "Invalid buffer!"); 
            BCLDebug.Assert(m_buffer.Length % BlockSize == 0, "buffer length must be multiple of blocksize!");
 
            if( m_length == 0) {
                return;
            }
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
            } 
            finally {
                if (m_enrypted) { 
                    // RtlEncryptMemory return an NTSTATUS
                    int status = Win32Native.SystemFunction041(m_buffer, (uint)m_buffer.Length * 2, ProtectionScope);
                    if (status < 0)
                    { // non-negative numbers indicate success 
                        throw new CryptographicException(Win32Native.LsaNtStatusToWinError(status));
                    } 
                    m_enrypted = false; 
                }
            } 
        }
    }

    [SuppressUnmanagedCodeSecurityAttribute()] 
    internal sealed class SafeBSTRHandle : SafePointer {
        internal SafeBSTRHandle () : base(true) {} 
 
        internal static SafeBSTRHandle Allocate(String src, uint len)
        { 
            SafeBSTRHandle bstr = SysAllocStringLen(src, len);
            bstr.Initialize(len * 2);
            return bstr;
        } 

        [ResourceExposure(ResourceScope.None)] 
        [DllImport(Win32Native.OLEAUT32, CharSet = CharSet.Unicode)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern SafeBSTRHandle SysAllocStringLen(String src, uint len);  // BSTR 

        override protected bool ReleaseHandle() {
            Win32Native.ZeroMemory(handle, (uint)Win32Native.SysStringLen(handle) * 2);
            Win32Native.SysFreeString(handle); 
            return true;
        } 
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe void ClearBuffer() { 
            byte* bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { 
                AcquirePointer(ref bufferPtr);
                Win32Native.ZeroMemory((IntPtr)bufferPtr, (uint)Win32Native.SysStringLen((IntPtr)bufferPtr) * 2); 
            } 
            finally
            { 
                if (bufferPtr != null)
                    ReleasePointer();
            }
        } 

 
        internal unsafe int Length { 
            get {
                byte* bufferPtr = null; 
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    AcquirePointer(ref bufferPtr); 
                    return Win32Native.SysStringLen((IntPtr)bufferPtr);
                } 
                finally 
                {
                    if (bufferPtr != null) 
                        ReleasePointer();
                }
            }
        } 

        internal unsafe static void Copy(SafeBSTRHandle source, SafeBSTRHandle target) { 
            byte* sourcePtr = null, targetPtr = null; 
            RuntimeHelpers.PrepareConstrainedRegions();
            try 
            {
                source.AcquirePointer(ref sourcePtr);
                target.AcquirePointer(ref targetPtr);
 
                BCLDebug.Assert(Win32Native.SysStringLen((IntPtr)targetPtr) >= Win32Native.SysStringLen((IntPtr)sourcePtr), "Target buffer is not large enough!");
 
                Buffer.memcpyimpl(sourcePtr, targetPtr, Win32Native.SysStringLen((IntPtr)sourcePtr) * 2); 
            }
            finally 
            {
                if (sourcePtr != null)
                    source.ReleasePointer();
                if (targetPtr != null) 
                    target.ReleasePointer();
            } 
        } 
    }
} 


namespace System.Security { 
    using System.Security.Cryptography;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Win32; 
    using System.Runtime.CompilerServices;
    using System.Security.Permissions; 
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.Versioning;
    using Microsoft.Win32.SafeHandles;	 


    public sealed class SecureString: IDisposable {
        private SafeBSTRHandle m_buffer; 
        private int       m_length;
        private bool     m_readOnly; 
        private bool     m_enrypted; 

        static bool supportedOnCurrentPlatform = EncryptionSupported(); 

        const int BlockSize = (int)Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE /2;  // a char is two bytes
        const int MaxLength = 65536;
        const uint ProtectionScope = Win32Native.CRYPTPROTECTMEMORY_SAME_PROCESS; 

        unsafe static bool EncryptionSupported() { 
            // check if the enrypt/decrypt function is supported on current OS 
            bool supported = true;
            try { 
                Win32Native.SystemFunction041(
 			        SafeBSTRHandle.Allocate(null , (int)Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE),
			        Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE,
			        Win32Native.CRYPTPROTECTMEMORY_SAME_PROCESS); 
            }
            catch (EntryPointNotFoundException) { 
                supported = false; 
            }
            return supported; 
        }

        internal SecureString(SecureString str) {
            AllocateBuffer(str.BufferLength); 
            SafeBSTRHandle.Copy(str.m_buffer, this.m_buffer);
            m_length = str.m_length; 
            m_enrypted = str.m_enrypted; 
        }
 

        public SecureString() {
            CheckSupportedOnCurrentPlatform();
 
            // allocate the minimum block size for calling protectMemory
            AllocateBuffer(BlockSize); 
            m_length = 0; 
        }
 

        [CLSCompliant(false)]
        public unsafe SecureString(char* value, int length) {
            if( value == null) { 
                throw new ArgumentNullException("value");
            } 
 
            if( length < 0) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum")); 
            }

            if( length > MaxLength) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Length")); 
            }
 
            CheckSupportedOnCurrentPlatform(); 

            AllocateBuffer(length); 

            byte* bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try 
            {
                m_buffer.AcquirePointer(ref bufferPtr); 
                Buffer.memcpyimpl((byte*)value, bufferPtr, length * 2); 
            }
            finally 
            {
                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            } 

            m_length = length; 
            ProtectMemory(); 
        }
 
        public int Length {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            get  {
                EnsureNotDisposed(); 
                return m_length;
            } 
        } 

        [MethodImplAttribute(MethodImplOptions.Synchronized)] 
        public void AppendChar(char c) {
            EnsureNotDisposed();
            EnsureNotReadOnly();
 
            EnsureCapacity(m_length + 1);
 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                UnProtectMemory(); 
                m_buffer.Write<char>((uint)m_length * sizeof(char), c);
                m_length++;
            }
            finally { 
                ProtectMemory();
            } 
        } 

        // clears the current contents. Only available if writable 
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void Clear() {
            EnsureNotDisposed();
            EnsureNotReadOnly(); 

            m_length = 0; 
            m_buffer.ClearBuffer(); 
            m_enrypted = false;
        } 

        // Do a deep-copy of the SecureString
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public SecureString Copy() { 
            EnsureNotDisposed();
            return new SecureString(this); 
        } 

        [MethodImplAttribute(MethodImplOptions.Synchronized)] 
        public void Dispose() {
            if(m_buffer != null && !m_buffer.IsInvalid) {
                m_buffer.Close();
                m_buffer = null; 
            }
        } 
 
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void InsertAt( int index, char c ) { 
            EnsureNotDisposed();
            EnsureNotReadOnly();

            if( index < 0 || index > m_length) { 
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            } 
 
            EnsureCapacity(m_length + 1);
 
            unsafe {
                byte* bufferPtr = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                    UnProtectMemory();
                    m_buffer.AcquirePointer(ref bufferPtr); 
                    char* pBuffer = (char*)bufferPtr; 

                    for (int i = m_length; i > index; i--) { 
                        pBuffer[i] = pBuffer[i - 1];
                    }
                    pBuffer[index] = c;
                    ++m_length; 
                }
                finally { 
                    ProtectMemory(); 
                    if (bufferPtr != null)
                        m_buffer.ReleasePointer(); 
                }
            }
        }
 
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public bool IsReadOnly() { 
            EnsureNotDisposed(); 
            return m_readOnly;
        } 

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void MakeReadOnly() {
            EnsureNotDisposed(); 
            m_readOnly = true;
        } 
 
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        public void RemoveAt( int index ) { 
            EnsureNotDisposed();
            EnsureNotReadOnly();

            if( index < 0 || index >= m_length) { 
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            } 
 
            unsafe
            { 
                byte* bufferPtr = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { 
                    UnProtectMemory();
                    m_buffer.AcquirePointer(ref bufferPtr); 
                    char* pBuffer = (char*)bufferPtr; 

                    for (int i = index; i < m_length - 1; i++) 
                    {
                        pBuffer[i] = pBuffer[i + 1];
                    }
                    pBuffer[--m_length] = (char)0; 
                }
                finally 
                { 
                    ProtectMemory();
                    if (bufferPtr != null) 
                        m_buffer.ReleasePointer();
                }
            }
        } 

        [MethodImplAttribute(MethodImplOptions.Synchronized)] 
        public void SetAt( int index, char c ) { 
            EnsureNotDisposed();
            EnsureNotReadOnly(); 

            if( index < 0 || index >= m_length) {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            } 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
                UnProtectMemory();
                m_buffer.Write<char>((uint)index * sizeof(char), c); 
            }
            finally {
                ProtectMemory();
            } 
        }
 
        private int BufferLength { 
            get {
                BCLDebug.Assert(m_buffer != null, "Buffer is not initialized!"); 
                return m_buffer.Length;
            }
        }
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private void AllocateBuffer(int size) { 
            uint alignedSize = GetAlignedSize(size); 

            m_buffer = SafeBSTRHandle.Allocate(null, alignedSize); 
            if (m_buffer.IsInvalid) {
                throw new OutOfMemoryException();
            }
        } 

        private void CheckSupportedOnCurrentPlatform() { 
            if( !supportedOnCurrentPlatform) { 
                throw new NotSupportedException(Environment.GetResourceString("Arg_PlatformSecureString"));
            } 
        }

        private void EnsureCapacity(int capacity) {
            if( capacity <= m_buffer.Length) { 
                return;
            } 
            if( capacity > MaxLength) { 
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
            } 

            SafeBSTRHandle newBuffer = SafeBSTRHandle.Allocate(null, GetAlignedSize(capacity));

            if (newBuffer.IsInvalid) { 
                throw new OutOfMemoryException();
            } 
 
            SafeBSTRHandle.Copy(m_buffer, newBuffer);
            m_buffer.Close(); 
            m_buffer = newBuffer;
        }

        private void EnsureNotDisposed() { 
            if( m_buffer == null) {
                throw new ObjectDisposedException(null); 
            } 
        }
 
        private void EnsureNotReadOnly() {
            if( m_readOnly) {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            } 
        }
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] 
        private static uint GetAlignedSize( int size) {
            BCLDebug.Assert(size >= 0, "size must be non-negative"); 

            uint alignedSize = ((uint)size / BlockSize) * BlockSize;
            if( (size % BlockSize != 0) || size == 0) {  // if size is 0, set allocated size to blocksize
                alignedSize += BlockSize; 
            }
            return alignedSize; 
        } 

        private unsafe int GetAnsiByteCount() { 
            const uint CP_ACP               = 0;
            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;

            uint flgs = WC_NO_BEST_FIT_CHARS; 
            uint DefaultCharUsed = (uint)'?';
 
            byte* bufferPtr = null; 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                m_buffer.AcquirePointer(ref bufferPtr);

                return Win32Native.WideCharToMultiByte(
                    CP_ACP, 
                    flgs,
                    (char*) bufferPtr, 
                    m_length, 
                    null,
                    0, 
                    IntPtr.Zero,
                    new IntPtr((void*)&DefaultCharUsed));
            }
            finally { 
                if (bufferPtr != null)
                    m_buffer.ReleasePointer(); 
            } 
        }
 
        private unsafe void GetAnsiBytes( byte * ansiStrPtr, int byteCount) {
            const uint CP_ACP               = 0;
            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;
 
            uint flgs = WC_NO_BEST_FIT_CHARS;
            uint DefaultCharUsed = (uint)'?'; 
 
            byte* bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                m_buffer.AcquirePointer(ref bufferPtr);

                Win32Native.WideCharToMultiByte( 
                    CP_ACP,
                    flgs, 
                    (char*) bufferPtr, 
                    m_length,
                    ansiStrPtr, 
                    byteCount - 1,
                    IntPtr.Zero,
                    new IntPtr((void*)&DefaultCharUsed));
 
                *(ansiStrPtr + byteCount - 1) = (byte)0;
            } 
            finally { 
                if (bufferPtr != null)
                    m_buffer.ReleasePointer(); 
            }
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)] 
        private void ProtectMemory() {
            BCLDebug.Assert(!m_buffer.IsInvalid && m_buffer.Length != 0, "Invalid buffer!"); 
            BCLDebug.Assert(m_buffer.Length % BlockSize == 0, "buffer length must be multiple of blocksize!"); 

            if( m_length == 0 || m_enrypted) { 
                return;
            }

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
            } 
            finally { 
                // RtlEncryptMemory return an NTSTATUS
                int status = Win32Native.SystemFunction040(m_buffer, (uint)m_buffer.Length * 2, ProtectionScope); 
                if (status < 0)  { // non-negative numbers indicate success
                    throw new CryptographicException(Win32Native.LsaNtStatusToWinError(status));
                }
                m_enrypted = true; 
            }
        } 
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [MethodImplAttribute(MethodImplOptions.Synchronized)] 
        internal unsafe IntPtr ToBSTR() {
            EnsureNotDisposed();
            int length = m_length;
            IntPtr ptr = IntPtr.Zero; 
            IntPtr result = IntPtr.Zero;
            byte* bufferPtr = null; 
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                }
                finally { 
                    ptr = Win32Native.SysAllocStringLen(null, length);
                } 
 
                if (ptr == IntPtr.Zero) {
                    throw new OutOfMemoryException(); 
                }

                UnProtectMemory();
                m_buffer.AcquirePointer(ref bufferPtr); 
                Buffer.memcpyimpl(bufferPtr, (byte*) ptr.ToPointer(), length *2);
                result = ptr; 
            } 
            finally {
                ProtectMemory(); 
                if( result == IntPtr.Zero) {
                    // If we failed for any reason, free the new buffer
                    if (ptr != IntPtr.Zero) {
                        Win32Native.ZeroMemory(ptr, (uint)(length * 2)); 
                        Win32Native.SysFreeString(ptr);
                    } 
                } 
                if (bufferPtr != null)
                    m_buffer.ReleasePointer(); 
            }
            return result;
        }
 
        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        internal unsafe IntPtr ToUniStr(bool allocateFromHeap) { 
            EnsureNotDisposed(); 
            int length = m_length;
            IntPtr ptr = IntPtr.Zero; 
            IntPtr result = IntPtr.Zero;
            byte* bufferPtr = null;

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                RuntimeHelpers.PrepareConstrainedRegions(); 
                try { 
                }
                finally { 
                    if( allocateFromHeap) {
                        ptr = Marshal.AllocHGlobal((length + 1) * 2);
                    }
                    else { 
                        ptr = Marshal.AllocCoTaskMem((length + 1) * 2);
                    } 
                } 

                if (ptr == IntPtr.Zero) { 
                    throw new OutOfMemoryException();
                }

                UnProtectMemory(); 
                m_buffer.AcquirePointer(ref bufferPtr);
                Buffer.memcpyimpl(bufferPtr, (byte*) ptr.ToPointer(), length *2); 
                char * endptr = (char *) ptr.ToPointer(); 
                *(endptr + length) = '\0';
                result = ptr; 
            }
            finally {
                ProtectMemory();
 
                if( result == IntPtr.Zero) {
                    // If we failed for any reason, free the new buffer 
                    if (ptr != IntPtr.Zero) { 
                        Win32Native.ZeroMemory(ptr, (uint)(length * 2));
                        if( allocateFromHeap) { 
                            Marshal.FreeHGlobal(ptr);
                        }
                        else {
                            Marshal.FreeCoTaskMem(ptr); 
                        }
                    } 
                } 

                if (bufferPtr != null) 
                    m_buffer.ReleasePointer();
            }
            return result;
        } 

        [MethodImplAttribute(MethodImplOptions.Synchronized)] 
        internal unsafe IntPtr ToAnsiStr(bool allocateFromHeap) { 
            EnsureNotDisposed();
 
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            int byteCount = 0;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                // GetAnsiByteCount uses the string data, so the calculation must happen after we are decrypted. 
                UnProtectMemory(); 

                // allocating an extra char for terminating zero 
                byteCount = GetAnsiByteCount() + 1;

                RuntimeHelpers.PrepareConstrainedRegions();
                try { 
                }
                finally { 
                    if( allocateFromHeap) { 
                        ptr = Marshal.AllocHGlobal(byteCount);
                    } 
                    else {
                        ptr = Marshal.AllocCoTaskMem(byteCount);
                    }
                } 

                if (ptr == IntPtr.Zero) { 
                    throw new OutOfMemoryException(); 
                }
 
                GetAnsiBytes((byte *)ptr.ToPointer(), byteCount);
                result = ptr;
            }
            finally { 
                ProtectMemory();
                if( result == IntPtr.Zero) { 
                    // If we failed for any reason, free the new buffer 
                    if (ptr != IntPtr.Zero) {
                        Win32Native.ZeroMemory(ptr, (uint)byteCount); 
                        if( allocateFromHeap) {
                            Marshal.FreeHGlobal(ptr);
                        }
                        else { 
                            Marshal.FreeCoTaskMem(ptr);
                        } 
                    } 
                }
 
            }
            return result;
        }
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private void UnProtectMemory() { 
            BCLDebug.Assert(!m_buffer.IsInvalid && m_buffer.Length != 0, "Invalid buffer!"); 
            BCLDebug.Assert(m_buffer.Length % BlockSize == 0, "buffer length must be multiple of blocksize!");
 
            if( m_length == 0) {
                return;
            }
 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
            } 
            finally {
                if (m_enrypted) { 
                    // RtlEncryptMemory return an NTSTATUS
                    int status = Win32Native.SystemFunction041(m_buffer, (uint)m_buffer.Length * 2, ProtectionScope);
                    if (status < 0)
                    { // non-negative numbers indicate success 
                        throw new CryptographicException(Win32Native.LsaNtStatusToWinError(status));
                    } 
                    m_enrypted = false; 
                }
            } 
        }
    }

    [SuppressUnmanagedCodeSecurityAttribute()] 
    internal sealed class SafeBSTRHandle : SafePointer {
        internal SafeBSTRHandle () : base(true) {} 
 
        internal static SafeBSTRHandle Allocate(String src, uint len)
        { 
            SafeBSTRHandle bstr = SysAllocStringLen(src, len);
            bstr.Initialize(len * 2);
            return bstr;
        } 

        [ResourceExposure(ResourceScope.None)] 
        [DllImport(Win32Native.OLEAUT32, CharSet = CharSet.Unicode)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern SafeBSTRHandle SysAllocStringLen(String src, uint len);  // BSTR 

        override protected bool ReleaseHandle() {
            Win32Native.ZeroMemory(handle, (uint)Win32Native.SysStringLen(handle) * 2);
            Win32Native.SysFreeString(handle); 
            return true;
        } 
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe void ClearBuffer() { 
            byte* bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { 
                AcquirePointer(ref bufferPtr);
                Win32Native.ZeroMemory((IntPtr)bufferPtr, (uint)Win32Native.SysStringLen((IntPtr)bufferPtr) * 2); 
            } 
            finally
            { 
                if (bufferPtr != null)
                    ReleasePointer();
            }
        } 

 
        internal unsafe int Length { 
            get {
                byte* bufferPtr = null; 
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    AcquirePointer(ref bufferPtr); 
                    return Win32Native.SysStringLen((IntPtr)bufferPtr);
                } 
                finally 
                {
                    if (bufferPtr != null) 
                        ReleasePointer();
                }
            }
        } 

        internal unsafe static void Copy(SafeBSTRHandle source, SafeBSTRHandle target) { 
            byte* sourcePtr = null, targetPtr = null; 
            RuntimeHelpers.PrepareConstrainedRegions();
            try 
            {
                source.AcquirePointer(ref sourcePtr);
                target.AcquirePointer(ref targetPtr);
 
                BCLDebug.Assert(Win32Native.SysStringLen((IntPtr)targetPtr) >= Win32Native.SysStringLen((IntPtr)sourcePtr), "Target buffer is not large enough!");
 
                Buffer.memcpyimpl(sourcePtr, targetPtr, Win32Native.SysStringLen((IntPtr)sourcePtr) * 2); 
            }
            finally 
            {
                if (sourcePtr != null)
                    source.ReleasePointer();
                if (targetPtr != null) 
                    target.ReleasePointer();
            } 
        } 
    }
} 


