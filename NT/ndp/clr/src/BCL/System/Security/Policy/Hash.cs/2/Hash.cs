// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// Hash 
//
// Evidence corresponding to a hash of the assembly bits. 
//

namespace System.Security.Policy {
    using Microsoft.Win32.SafeHandles; 
    using System.Reflection;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.Serialization;
    using System.Security.Cryptography; 
    using System.Security.Util;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    public sealed class Hash : ISerializable, IBuiltInEvidence {
        private SafePEFileHandle m_peFile = SafePEFileHandle.InvalidHandle; 
 
        internal Hash() {}
        internal Hash(SerializationInfo info, StreamingContext context) { 
            m_md5 = (byte[]) info.GetValueNoThrow("Md5", typeof(byte[]));
            m_sha1 = (byte[]) info.GetValueNoThrow("Sha1", typeof(byte[]));
            m_peFile = SafePEFileHandle.InvalidHandle;
            m_rawData = (byte[]) info.GetValue("RawData", typeof(byte[])); 
            if (m_rawData == null) {
                IntPtr peFile = (IntPtr) info.GetValue("PEFile", typeof(IntPtr)); 
                if (peFile != IntPtr.Zero) 
                    _SetPEFileHandle(peFile, ref m_peFile);
            } 
        }

        public Hash(Assembly assembly) {
            if (assembly == null) 
                throw new ArgumentNullException("assembly");
 
            _GetPEFileFromAssembly(assembly.InternalAssembly, ref m_peFile); 
        }
 
        public static Hash CreateSHA1(byte[] sha1) {
            if (sha1 == null)
                throw new ArgumentNullException("sha1");
 
            Hash hash = new Hash();
            hash.m_sha1 = new byte[sha1.Length]; 
            Array.Copy(sha1, hash.m_sha1, sha1.Length); 

            return hash; 
        }

        public static Hash CreateMD5(byte[] md5) {
            if (md5 == null) 
                throw new ArgumentNullException("md5");
 
            Hash hash = new Hash(); 
            hash.m_md5 = new byte[md5.Length];
            Array.Copy(md5, hash.m_md5, md5.Length); 

            return hash;
        }
 
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            new System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode).Demand(); 
            info.AddValue("Md5", m_md5); 
            info.AddValue("Sha1", m_sha1);
            if (context.State == StreamingContextStates.Clone || 
                context.State == StreamingContextStates.CrossAppDomain) {
                info.AddValue("PEFile", m_peFile.DangerousGetHandle());
                if (m_peFile.IsInvalid)
                    info.AddValue("RawData", m_rawData); 
                else
                    info.AddValue("RawData", null); 
            } else { 
                if (!m_peFile.IsInvalid)
                    m_rawData = this.RawData; 
                info.AddValue("PEFile", IntPtr.Zero);
                info.AddValue("RawData", m_rawData);
            }
        } 

        private byte[] m_rawData = null; 
        internal byte[] RawData { 
            get {
                if (m_rawData == null) { 
                    if (m_peFile.IsInvalid)
                        throw new SecurityException(Environment.GetResourceString("Security_CannotGetRawData"));
                    byte[] rawData = _GetRawData(m_peFile);
                    if (rawData == null) 
                        throw new SecurityException(Environment.GetResourceString("Security_CannotGenerateHash"));
                    m_rawData = rawData; 
                } 
                return m_rawData;
            } 
        }

        private byte[] m_sha1 = null;
        public byte[] SHA1 { 
            get {
                if (m_sha1 == null) { 
                    System.Security.Cryptography.SHA1 hashAlg = new System.Security.Cryptography.SHA1Managed(); 
                    m_sha1 = hashAlg.ComputeHash(this.RawData);
                } 
                byte[] retval = new byte[m_sha1.Length];
                Array.Copy(m_sha1, retval, m_sha1.Length);
                return retval;
            } 
        }
 
        private byte[] m_md5 = null; 
        public byte[] MD5 {
            get { 
                if (m_md5 == null) {
                    System.Security.Cryptography.MD5 hashAlg = new MD5CryptoServiceProvider();
                    m_md5 = hashAlg.ComputeHash(this.RawData);
                } 
                byte[] retval = new byte[m_md5.Length];
                Array.Copy(m_md5, retval, m_md5.Length); 
                return retval; 
            }
        } 

        public byte[] GenerateHash(HashAlgorithm hashAlg) {
            if (hashAlg == null)
                throw new ArgumentNullException("hashAlg"); 

            if (hashAlg is SHA1) 
                return this.SHA1; 
            else if (hashAlg is MD5)
                return this.MD5; 

            return hashAlg.ComputeHash(this.RawData);
        }
 
        /// <internalonly/>
        int IBuiltInEvidence.OutputToBuffer(char[] buffer, int position, bool verbose) { 
            if (!verbose) 
                return position;
            buffer[position++] = BuiltInEvidenceHelper.idHash; 
            IntPtr ptrPEFile = IntPtr.Zero;
            if (!m_peFile.IsInvalid)
                ptrPEFile = m_peFile.DangerousGetHandle() ;
            BuiltInEvidenceHelper.CopyLongToCharArray((long)ptrPEFile, buffer, position); 
            return position + 4;
        } 
 
        /// <internalonly/>
        int IBuiltInEvidence.GetRequiredSize(bool verbose) { 
            if (verbose)
                return (1 + 4); // identifier + IntPtr
            else
                return 0; 
        }
 
        /// <internalonly/> 
        int IBuiltInEvidence.InitFromBuffer(char[] buffer, int position) {
            m_peFile = SafePEFileHandle.InvalidHandle; 
            IntPtr ptrPEFile = (IntPtr) BuiltInEvidenceHelper.GetLongFromCharArray(buffer, position);
            _SetPEFileHandle(ptrPEFile, ref this.m_peFile);
            return position + 4;
        } 

        private SecurityElement ToXml() { 
            SecurityElement root = new SecurityElement("System.Security.Policy.Hash"); 
            // If you hit this assert then most likely you are trying to change the name of this class.
            // This is ok as long as you change the hard coded string above and change the assert below. 
            BCLDebug.Assert(this.GetType().FullName.Equals("System.Security.Policy.Hash"), "Class name changed!");

            root.AddAttribute("version", "1");
            root.AddChild(new SecurityElement("RawData", Hex.EncodeHexString(this.RawData))); 

            return root; 
        } 

        public override String ToString() { 
            return ToXml().ToString();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern byte[] _GetRawData(SafePEFileHandle handle);
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern void _GetPEFileFromAssembly(Assembly assembly, ref SafePEFileHandle handle);
 
        [MethodImplAttribute(MethodImplOptions.InternalCall),
         ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern void _ReleasePEFile(IntPtr handle);
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _SetPEFileHandle(IntPtr inHandle, ref SafePEFileHandle outHandle); 
    } 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// Hash 
//
// Evidence corresponding to a hash of the assembly bits. 
//

namespace System.Security.Policy {
    using Microsoft.Win32.SafeHandles; 
    using System.Reflection;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution; 
    using System.Runtime.Serialization;
    using System.Security.Cryptography; 
    using System.Security.Util;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    public sealed class Hash : ISerializable, IBuiltInEvidence {
        private SafePEFileHandle m_peFile = SafePEFileHandle.InvalidHandle; 
 
        internal Hash() {}
        internal Hash(SerializationInfo info, StreamingContext context) { 
            m_md5 = (byte[]) info.GetValueNoThrow("Md5", typeof(byte[]));
            m_sha1 = (byte[]) info.GetValueNoThrow("Sha1", typeof(byte[]));
            m_peFile = SafePEFileHandle.InvalidHandle;
            m_rawData = (byte[]) info.GetValue("RawData", typeof(byte[])); 
            if (m_rawData == null) {
                IntPtr peFile = (IntPtr) info.GetValue("PEFile", typeof(IntPtr)); 
                if (peFile != IntPtr.Zero) 
                    _SetPEFileHandle(peFile, ref m_peFile);
            } 
        }

        public Hash(Assembly assembly) {
            if (assembly == null) 
                throw new ArgumentNullException("assembly");
 
            _GetPEFileFromAssembly(assembly.InternalAssembly, ref m_peFile); 
        }
 
        public static Hash CreateSHA1(byte[] sha1) {
            if (sha1 == null)
                throw new ArgumentNullException("sha1");
 
            Hash hash = new Hash();
            hash.m_sha1 = new byte[sha1.Length]; 
            Array.Copy(sha1, hash.m_sha1, sha1.Length); 

            return hash; 
        }

        public static Hash CreateMD5(byte[] md5) {
            if (md5 == null) 
                throw new ArgumentNullException("md5");
 
            Hash hash = new Hash(); 
            hash.m_md5 = new byte[md5.Length];
            Array.Copy(md5, hash.m_md5, md5.Length); 

            return hash;
        }
 
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            new System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode).Demand(); 
            info.AddValue("Md5", m_md5); 
            info.AddValue("Sha1", m_sha1);
            if (context.State == StreamingContextStates.Clone || 
                context.State == StreamingContextStates.CrossAppDomain) {
                info.AddValue("PEFile", m_peFile.DangerousGetHandle());
                if (m_peFile.IsInvalid)
                    info.AddValue("RawData", m_rawData); 
                else
                    info.AddValue("RawData", null); 
            } else { 
                if (!m_peFile.IsInvalid)
                    m_rawData = this.RawData; 
                info.AddValue("PEFile", IntPtr.Zero);
                info.AddValue("RawData", m_rawData);
            }
        } 

        private byte[] m_rawData = null; 
        internal byte[] RawData { 
            get {
                if (m_rawData == null) { 
                    if (m_peFile.IsInvalid)
                        throw new SecurityException(Environment.GetResourceString("Security_CannotGetRawData"));
                    byte[] rawData = _GetRawData(m_peFile);
                    if (rawData == null) 
                        throw new SecurityException(Environment.GetResourceString("Security_CannotGenerateHash"));
                    m_rawData = rawData; 
                } 
                return m_rawData;
            } 
        }

        private byte[] m_sha1 = null;
        public byte[] SHA1 { 
            get {
                if (m_sha1 == null) { 
                    System.Security.Cryptography.SHA1 hashAlg = new System.Security.Cryptography.SHA1Managed(); 
                    m_sha1 = hashAlg.ComputeHash(this.RawData);
                } 
                byte[] retval = new byte[m_sha1.Length];
                Array.Copy(m_sha1, retval, m_sha1.Length);
                return retval;
            } 
        }
 
        private byte[] m_md5 = null; 
        public byte[] MD5 {
            get { 
                if (m_md5 == null) {
                    System.Security.Cryptography.MD5 hashAlg = new MD5CryptoServiceProvider();
                    m_md5 = hashAlg.ComputeHash(this.RawData);
                } 
                byte[] retval = new byte[m_md5.Length];
                Array.Copy(m_md5, retval, m_md5.Length); 
                return retval; 
            }
        } 

        public byte[] GenerateHash(HashAlgorithm hashAlg) {
            if (hashAlg == null)
                throw new ArgumentNullException("hashAlg"); 

            if (hashAlg is SHA1) 
                return this.SHA1; 
            else if (hashAlg is MD5)
                return this.MD5; 

            return hashAlg.ComputeHash(this.RawData);
        }
 
        /// <internalonly/>
        int IBuiltInEvidence.OutputToBuffer(char[] buffer, int position, bool verbose) { 
            if (!verbose) 
                return position;
            buffer[position++] = BuiltInEvidenceHelper.idHash; 
            IntPtr ptrPEFile = IntPtr.Zero;
            if (!m_peFile.IsInvalid)
                ptrPEFile = m_peFile.DangerousGetHandle() ;
            BuiltInEvidenceHelper.CopyLongToCharArray((long)ptrPEFile, buffer, position); 
            return position + 4;
        } 
 
        /// <internalonly/>
        int IBuiltInEvidence.GetRequiredSize(bool verbose) { 
            if (verbose)
                return (1 + 4); // identifier + IntPtr
            else
                return 0; 
        }
 
        /// <internalonly/> 
        int IBuiltInEvidence.InitFromBuffer(char[] buffer, int position) {
            m_peFile = SafePEFileHandle.InvalidHandle; 
            IntPtr ptrPEFile = (IntPtr) BuiltInEvidenceHelper.GetLongFromCharArray(buffer, position);
            _SetPEFileHandle(ptrPEFile, ref this.m_peFile);
            return position + 4;
        } 

        private SecurityElement ToXml() { 
            SecurityElement root = new SecurityElement("System.Security.Policy.Hash"); 
            // If you hit this assert then most likely you are trying to change the name of this class.
            // This is ok as long as you change the hard coded string above and change the assert below. 
            BCLDebug.Assert(this.GetType().FullName.Equals("System.Security.Policy.Hash"), "Class name changed!");

            root.AddAttribute("version", "1");
            root.AddChild(new SecurityElement("RawData", Hex.EncodeHexString(this.RawData))); 

            return root; 
        } 

        public override String ToString() { 
            return ToXml().ToString();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern byte[] _GetRawData(SafePEFileHandle handle);
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern void _GetPEFileFromAssembly(Assembly assembly, ref SafePEFileHandle handle);
 
        [MethodImplAttribute(MethodImplOptions.InternalCall),
         ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern void _ReleasePEFile(IntPtr handle);
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void _SetPEFileHandle(IntPtr inHandle, ref SafePEFileHandle outHandle); 
    } 
}
