//------------------------------------------------------------------------------ 
// <copyright file="_NativeSSPI.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.Net.Sockets; 
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution;
    using System.Globalization;

    // need a global so we can pass the interfaces as variables, 
    // is there a better way?
    internal static class GlobalSSPI { 
        internal static SSPIInterface SSPIAuth = new SSPIAuthType(); 
        internal static SSPIInterface SSPISecureChannel = new SSPISecureChannelType();
    } 

    // used to define the interface for security to use.
    internal interface SSPIInterface {
        SecurityPackageInfoClass[] SecurityPackages { get; set; } 
        int EnumerateSecurityPackages(out int pkgnum, out SafeFreeContextBuffer pkgArray);
        int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref AuthIdentity authdata, out SafeFreeCredentials outCredential); 
        int AcquireDefaultCredential(string moduleName, CredentialUse usage, out SafeFreeCredentials outCredential); 
        int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref SecureCredential authdata, out SafeFreeCredentials outCredential);
        int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags); 
        int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags);
        int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref ContextFlags outFlags);
        int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref ContextFlags outFlags);
        int EncryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber); 
        int DecryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber);
        int MakeSignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber); 
        int VerifySignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber); 

        int QueryContextAttributes(SafeDeleteContext phContext, ContextAttribute  attribute, byte[] buffer, Type handleType, out SafeHandle refHandle); 
        int QuerySecurityContextToken(SafeDeleteContext phContext, out SafeCloseHandle phToken);
        int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers);
    }
 
    // For SSL connections:
    // on Win9x use schannel.dll, on NT we don't care, since its the same DLL 
    internal class SSPISecureChannelType : SSPIInterface { 
        private static readonly SecurDll Library = ComNetOS.IsWin9x ? SecurDll.SCHANNEL : SecurDll.SECURITY;
        private static SecurityPackageInfoClass[] m_SecurityPackages; 

        public SecurityPackageInfoClass[] SecurityPackages {
            get {
                return m_SecurityPackages; 
            }
            set { 
                m_SecurityPackages = value; 
            }
        } 

        public int EnumerateSecurityPackages(out int pkgnum, out SafeFreeContextBuffer pkgArray) {
            GlobalLog.Print("SSPISecureChannelType::EnumerateSecurityPackages()");
            return SafeFreeContextBuffer.EnumeratePackages(Library, out pkgnum, out pkgArray); 
        }
 
        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref AuthIdentity authdata, out SafeFreeCredentials outCredential) { 
            return SafeFreeCredentials.AcquireCredentialsHandle(Library, moduleName, usage, ref authdata, out outCredential);
        } 

        public int AcquireDefaultCredential(string moduleName, CredentialUse usage, out SafeFreeCredentials outCredential) {
            return SafeFreeCredentials.AcquireDefaultCredential(Library, moduleName, usage, out outCredential);
        } 

        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref SecureCredential authdata, out SafeFreeCredentials outCredential) { 
            return SafeFreeCredentials.AcquireCredentialsHandle(Library, moduleName, usage, ref authdata, out outCredential); 
        }
 
        public int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags) {
            return SafeDeleteContext.AcceptSecurityContext(Library, ref credential, ref context, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        }
 
        public int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags) {
            return SafeDeleteContext.AcceptSecurityContext(Library, ref credential, ref context, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags); 
        } 

        public int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref ContextFlags outFlags) { 
            return SafeDeleteContext.InitializeSecurityContext(Library, ref credential, ref context, targetName, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        }

        public int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref ContextFlags outFlags) { 
            return SafeDeleteContext.InitializeSecurityContext(Library, ref credential, ref context, targetName, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags);
        } 
 

 
        private int EncryptMessageHelper9x(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
                context.DangerousAddRef(ref b);
            } 
            catch(Exception e) {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException)) 
                    throw;
            } 
            catch {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw; 
            }
            finally { 

                if (b)
                {
                    status = UnsafeNclNativeMethods.NativeSSLWin9xSSPI.SealMessage(ref context._handle, 0, inputOutput, sequenceNumber); 
                    context.DangerousRelease();
                } 
            } 
            return status;
 
        }
        private int EncryptMessageHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            int status = (int)SecurityStatus.InvalidHandle; 
            bool b = false;
 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                context.DangerousAddRef(ref b); 
            }
            catch(Exception e) {
                if (b)
                { 
                    context.DangerousRelease();
                    b = false; 
                } 
                if (!(e is ObjectDisposedException))
                    throw; 
            }
            catch {
                if (b)
                { 
                    context.DangerousRelease();
                    b = false; 
                } 
                throw;
            } 
            finally {

                if (b)
                { 
                    status = UnsafeNclNativeMethods.NativeNTSSPI.EncryptMessage(ref context._handle, 0, inputOutput, sequenceNumber);
                    context.DangerousRelease(); 
                } 
            }
            return status; 
        }


        //get around the constrained region requirements 
        public int EncryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            if (ComNetOS.IsWin9x){ 
                return EncryptMessageHelper9x(context,inputOutput,sequenceNumber); 
            }
            else{ 
                return EncryptMessageHelper(context,inputOutput,sequenceNumber);
            }
        }
 
        private int DecryptMessageHelper9x(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            int status = (int)SecurityStatus.InvalidHandle; 
            bool b = false; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                context.DangerousAddRef(ref b);
            }
            catch(Exception e) { 
                if (b)
                { 
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException))
                    throw;
            }
            catch { 
                if (b)
                { 
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw;
            }
            finally {
 
                if (b)
                { 
                    status = UnsafeNclNativeMethods.NativeSSLWin9xSSPI.UnsealMessage(ref context._handle, inputOutput, IntPtr.Zero, sequenceNumber); 
                    context.DangerousRelease();
                } 
            }
            return status;
        }
 
        private unsafe int DecryptMessageHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            int status = (int)SecurityStatus.InvalidHandle; 
            bool b = false; 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                context.DangerousAddRef(ref b);
            }
            catch(Exception e) {
                if (b) 
                {
                    context.DangerousRelease(); 
                    b = false; 
                }
                if (!(e is ObjectDisposedException)) 
                    throw;
            }
            catch {
                if (b) 
                {
                    context.DangerousRelease(); 
                    b = false; 
                }
                throw; 
            }
            finally {

                if (b) 
                {
                    status = UnsafeNclNativeMethods.NativeNTSSPI.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, null); 
                    context.DangerousRelease(); 
                }
            } 
            return status;
        }

 
        public int DecryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            if (ComNetOS.IsWin9x){ 
                return DecryptMessageHelper9x(context,inputOutput,sequenceNumber); 
            }
            else{ 
                return DecryptMessageHelper(context,inputOutput,sequenceNumber);
            }
        }
 

        public int MakeSignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) { 
            throw ExceptionHelper.MethodNotSupportedException; 
        }
 
        public int VerifySignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            throw ExceptionHelper.MethodNotSupportedException;
        }
 
        public unsafe int QueryContextAttributes(SafeDeleteContext phContext, ContextAttribute  attribute, byte[] buffer, Type handleType, out SafeHandle refHandle) {
 
            refHandle = null; 
            if (handleType != null) {
                if (handleType == typeof(SafeFreeContextBuffer)) { 
                    refHandle = SafeFreeContextBuffer.CreateEmptyHandle(Library);
                }
                else if (handleType == typeof(SafeFreeCertContext)) {
                    refHandle = new SafeFreeCertContext(); 
                }
                else { 
                    throw new ArgumentException(SR.GetString(SR.SSPIInvalidHandleType, handleType.FullName), "handleType"); 
                }
            } 
            fixed (byte* bufferPtr = buffer) {
                return SafeFreeContextBuffer.QueryContextAttributes(Library, phContext, attribute, bufferPtr, refHandle);
            }
        } 

        public int QuerySecurityContextToken(SafeDeleteContext phContext, out SafeCloseHandle phToken) { 
            throw new NotSupportedException(); 
        }
 
        public int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers) {
            throw new NotSupportedException();
        }
    } 

 
    // For Authentication (Kerberos, NTLM, Negotiate and WDigest): 
    // on Win9x use schannel.dll, on NT we don't care, since its the same DLL
    internal class SSPIAuthType : SSPIInterface { 
        private static readonly SecurDll Library = ComNetOS.IsWin9x ? SecurDll.SECUR32 : SecurDll.SECURITY;
        private static SecurityPackageInfoClass[] m_SecurityPackages;

        public SecurityPackageInfoClass[] SecurityPackages { 
            get {
                return m_SecurityPackages; 
            } 
            set {
                m_SecurityPackages = value; 
            }
        }

        public int EnumerateSecurityPackages(out int pkgnum, out SafeFreeContextBuffer pkgArray) { 
            GlobalLog.Print("SSPIAuthType::EnumerateSecurityPackages()");
            return SafeFreeContextBuffer.EnumeratePackages(Library, out pkgnum, out pkgArray); 
        } 

        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref AuthIdentity authdata, out SafeFreeCredentials outCredential) { 
            return SafeFreeCredentials.AcquireCredentialsHandle(Library, moduleName, usage, ref authdata, out outCredential);
        }

        public int AcquireDefaultCredential(string moduleName, CredentialUse usage, out SafeFreeCredentials outCredential) { 
            return SafeFreeCredentials.AcquireDefaultCredential(Library, moduleName, usage, out outCredential);
        } 
 
        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref SecureCredential authdata, out SafeFreeCredentials outCredential) {
            return SafeFreeCredentials.AcquireCredentialsHandle(Library, moduleName, usage, ref authdata, out outCredential); 
        }

        public int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags) {
            return SafeDeleteContext.AcceptSecurityContext(Library, ref credential, ref context, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags); 
        }
 
        public int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags) { 
            return SafeDeleteContext.AcceptSecurityContext(Library, ref credential, ref context, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags);
        } 

        public int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref ContextFlags outFlags) {
            return SafeDeleteContext.InitializeSecurityContext(Library, ref credential, ref context, targetName, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        } 

        public int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref ContextFlags outFlags) { 
            return SafeDeleteContext.InitializeSecurityContext(Library, ref credential, ref context, targetName, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags); 
        }
 

        private int EncryptMessageHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
                context.DangerousAddRef(ref b);
            } 
            catch(Exception e) {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException)) 
                    throw;
            } 
            catch {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw; 
            }
            finally { 

                if (b)
                {
                    status = UnsafeNclNativeMethods.NativeNTSSPI.EncryptMessage(ref context._handle, 0, inputOutput, sequenceNumber); 
                    context.DangerousRelease();
                } 
            } 
            return status;
        } 

        public int EncryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            if (ComNetOS.IsWin9x){
                throw ExceptionHelper.MethodNotImplementedException; 
            }
            else{ 
                return EncryptMessageHelper(context,inputOutput,sequenceNumber); 
            }
        } 
        private unsafe int DecryptMessageHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false;
            uint qop = 0; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
                context.DangerousAddRef(ref b);
            } 
            catch(Exception e) {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException)) 
                    throw;
            } 
            catch {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw; 
            }
            finally { 

                if (b)
                {
                    status = UnsafeNclNativeMethods.NativeNTSSPI.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, &qop); 
                    context.DangerousRelease();
                } 
            } 

            const uint SECQOP_WRAP_NO_ENCRYPT = 0x80000001; 
            if (status == 0 && qop == SECQOP_WRAP_NO_ENCRYPT)
            {
                GlobalLog.Assert("NativeNTSSPI.DecryptMessage", "Expected qop = 0, returned value = " + qop.ToString("x", CultureInfo.InvariantCulture));
                throw new InvalidOperationException(SR.GetString(SR.net_auth_message_not_encrypted)); 
            }
 
 
            return status;
        } 

        public int DecryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            if (ComNetOS.IsWin9x){
                throw ExceptionHelper.MethodNotImplementedException; 
            }
            else{ 
                return DecryptMessageHelper(context,inputOutput,sequenceNumber); 
            }
        } 

        private int MakeSignatureHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
                context.DangerousAddRef(ref b);
            } 
            catch(Exception e) {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException)) 
                    throw;
            } 
            catch {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw; 
            }
            finally { 

                if (b)
                {
                    const uint SECQOP_WRAP_NO_ENCRYPT = 0x80000001; 
                    status = UnsafeNclNativeMethods.NativeNTSSPI.EncryptMessage(ref context._handle, SECQOP_WRAP_NO_ENCRYPT, inputOutput, sequenceNumber);
                    context.DangerousRelease(); 
                } 
            }
            return status; 
        }


        public int MakeSignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) { 
            if (ComNetOS.IsWin9x){
                throw ExceptionHelper.MethodNotImplementedException; 
            } 
            else{
                return MakeSignatureHelper(context,inputOutput,sequenceNumber); 
            }
        }

        private unsafe int VerifySignatureHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) { 
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false; 
 
            uint qop = 0;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                context.DangerousAddRef(ref b);
            }
            catch(Exception e) { 
                if (b)
                { 
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException))
                    throw;
            }
            catch { 
                if (b)
                { 
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw;
            }
            finally {
 
                if (b)
                { 
                    status = UnsafeNclNativeMethods.NativeNTSSPI.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, &qop); 
                    context.DangerousRelease();
                } 
            }

            return status;
 
        }
 
        public int VerifySignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) { 
            if (ComNetOS.IsWin9x){
                throw ExceptionHelper.MethodNotImplementedException; 
            }
            else{
                return VerifySignatureHelper(context,inputOutput,sequenceNumber);
            } 
        }
 
 
        public unsafe int QueryContextAttributes(SafeDeleteContext context, ContextAttribute  attribute, byte[] buffer, Type handleType, out SafeHandle refHandle) {
 
            refHandle = null;
            if (handleType != null) {
                if (handleType == typeof(SafeFreeContextBuffer)) {
                    refHandle = SafeFreeContextBuffer.CreateEmptyHandle(Library); 
                }
                else if (handleType == typeof(SafeFreeCertContext)) { 
                    refHandle = new SafeFreeCertContext(); 
                }
                else { 
                    throw new ArgumentException(SR.GetString(SR.SSPIInvalidHandleType, handleType.FullName), "handleType");
                }
            }
 
            fixed (byte* bufferPtr = buffer) {
                return SafeFreeContextBuffer.QueryContextAttributes(Library, context, attribute, bufferPtr, refHandle); 
            } 
        }
 
        public int QuerySecurityContextToken(SafeDeleteContext phContext, out SafeCloseHandle phToken) {
            if (ComNetOS.IsWin9x) {
                throw new NotSupportedException();
            } 
            else {
                return SafeCloseHandle.GetSecurityContextToken(phContext, out phToken); 
            } 
        }
 
        public int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers) {
            if (ComNetOS.IsWin9x) {
                throw new NotSupportedException();
            } 
            else {
                return SafeDeleteContext.CompleteAuthToken(Library, ref refContext, inputBuffers); 
            } 
        }
 
    }

}
//------------------------------------------------------------------------------ 
// <copyright file="_NativeSSPI.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.Net.Sockets; 
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices; 
    using System.Runtime.ConstrainedExecution;
    using System.Globalization;

    // need a global so we can pass the interfaces as variables, 
    // is there a better way?
    internal static class GlobalSSPI { 
        internal static SSPIInterface SSPIAuth = new SSPIAuthType(); 
        internal static SSPIInterface SSPISecureChannel = new SSPISecureChannelType();
    } 

    // used to define the interface for security to use.
    internal interface SSPIInterface {
        SecurityPackageInfoClass[] SecurityPackages { get; set; } 
        int EnumerateSecurityPackages(out int pkgnum, out SafeFreeContextBuffer pkgArray);
        int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref AuthIdentity authdata, out SafeFreeCredentials outCredential); 
        int AcquireDefaultCredential(string moduleName, CredentialUse usage, out SafeFreeCredentials outCredential); 
        int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref SecureCredential authdata, out SafeFreeCredentials outCredential);
        int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags); 
        int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags);
        int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref ContextFlags outFlags);
        int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref ContextFlags outFlags);
        int EncryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber); 
        int DecryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber);
        int MakeSignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber); 
        int VerifySignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber); 

        int QueryContextAttributes(SafeDeleteContext phContext, ContextAttribute  attribute, byte[] buffer, Type handleType, out SafeHandle refHandle); 
        int QuerySecurityContextToken(SafeDeleteContext phContext, out SafeCloseHandle phToken);
        int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers);
    }
 
    // For SSL connections:
    // on Win9x use schannel.dll, on NT we don't care, since its the same DLL 
    internal class SSPISecureChannelType : SSPIInterface { 
        private static readonly SecurDll Library = ComNetOS.IsWin9x ? SecurDll.SCHANNEL : SecurDll.SECURITY;
        private static SecurityPackageInfoClass[] m_SecurityPackages; 

        public SecurityPackageInfoClass[] SecurityPackages {
            get {
                return m_SecurityPackages; 
            }
            set { 
                m_SecurityPackages = value; 
            }
        } 

        public int EnumerateSecurityPackages(out int pkgnum, out SafeFreeContextBuffer pkgArray) {
            GlobalLog.Print("SSPISecureChannelType::EnumerateSecurityPackages()");
            return SafeFreeContextBuffer.EnumeratePackages(Library, out pkgnum, out pkgArray); 
        }
 
        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref AuthIdentity authdata, out SafeFreeCredentials outCredential) { 
            return SafeFreeCredentials.AcquireCredentialsHandle(Library, moduleName, usage, ref authdata, out outCredential);
        } 

        public int AcquireDefaultCredential(string moduleName, CredentialUse usage, out SafeFreeCredentials outCredential) {
            return SafeFreeCredentials.AcquireDefaultCredential(Library, moduleName, usage, out outCredential);
        } 

        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref SecureCredential authdata, out SafeFreeCredentials outCredential) { 
            return SafeFreeCredentials.AcquireCredentialsHandle(Library, moduleName, usage, ref authdata, out outCredential); 
        }
 
        public int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags) {
            return SafeDeleteContext.AcceptSecurityContext(Library, ref credential, ref context, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        }
 
        public int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags) {
            return SafeDeleteContext.AcceptSecurityContext(Library, ref credential, ref context, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags); 
        } 

        public int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref ContextFlags outFlags) { 
            return SafeDeleteContext.InitializeSecurityContext(Library, ref credential, ref context, targetName, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        }

        public int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref ContextFlags outFlags) { 
            return SafeDeleteContext.InitializeSecurityContext(Library, ref credential, ref context, targetName, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags);
        } 
 

 
        private int EncryptMessageHelper9x(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
                context.DangerousAddRef(ref b);
            } 
            catch(Exception e) {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException)) 
                    throw;
            } 
            catch {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw; 
            }
            finally { 

                if (b)
                {
                    status = UnsafeNclNativeMethods.NativeSSLWin9xSSPI.SealMessage(ref context._handle, 0, inputOutput, sequenceNumber); 
                    context.DangerousRelease();
                } 
            } 
            return status;
 
        }
        private int EncryptMessageHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            int status = (int)SecurityStatus.InvalidHandle; 
            bool b = false;
 
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                context.DangerousAddRef(ref b); 
            }
            catch(Exception e) {
                if (b)
                { 
                    context.DangerousRelease();
                    b = false; 
                } 
                if (!(e is ObjectDisposedException))
                    throw; 
            }
            catch {
                if (b)
                { 
                    context.DangerousRelease();
                    b = false; 
                } 
                throw;
            } 
            finally {

                if (b)
                { 
                    status = UnsafeNclNativeMethods.NativeNTSSPI.EncryptMessage(ref context._handle, 0, inputOutput, sequenceNumber);
                    context.DangerousRelease(); 
                } 
            }
            return status; 
        }


        //get around the constrained region requirements 
        public int EncryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            if (ComNetOS.IsWin9x){ 
                return EncryptMessageHelper9x(context,inputOutput,sequenceNumber); 
            }
            else{ 
                return EncryptMessageHelper(context,inputOutput,sequenceNumber);
            }
        }
 
        private int DecryptMessageHelper9x(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            int status = (int)SecurityStatus.InvalidHandle; 
            bool b = false; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                context.DangerousAddRef(ref b);
            }
            catch(Exception e) { 
                if (b)
                { 
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException))
                    throw;
            }
            catch { 
                if (b)
                { 
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw;
            }
            finally {
 
                if (b)
                { 
                    status = UnsafeNclNativeMethods.NativeSSLWin9xSSPI.UnsealMessage(ref context._handle, inputOutput, IntPtr.Zero, sequenceNumber); 
                    context.DangerousRelease();
                } 
            }
            return status;
        }
 
        private unsafe int DecryptMessageHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            int status = (int)SecurityStatus.InvalidHandle; 
            bool b = false; 
            RuntimeHelpers.PrepareConstrainedRegions();
            try { 
                context.DangerousAddRef(ref b);
            }
            catch(Exception e) {
                if (b) 
                {
                    context.DangerousRelease(); 
                    b = false; 
                }
                if (!(e is ObjectDisposedException)) 
                    throw;
            }
            catch {
                if (b) 
                {
                    context.DangerousRelease(); 
                    b = false; 
                }
                throw; 
            }
            finally {

                if (b) 
                {
                    status = UnsafeNclNativeMethods.NativeNTSSPI.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, null); 
                    context.DangerousRelease(); 
                }
            } 
            return status;
        }

 
        public int DecryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            if (ComNetOS.IsWin9x){ 
                return DecryptMessageHelper9x(context,inputOutput,sequenceNumber); 
            }
            else{ 
                return DecryptMessageHelper(context,inputOutput,sequenceNumber);
            }
        }
 

        public int MakeSignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) { 
            throw ExceptionHelper.MethodNotSupportedException; 
        }
 
        public int VerifySignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            throw ExceptionHelper.MethodNotSupportedException;
        }
 
        public unsafe int QueryContextAttributes(SafeDeleteContext phContext, ContextAttribute  attribute, byte[] buffer, Type handleType, out SafeHandle refHandle) {
 
            refHandle = null; 
            if (handleType != null) {
                if (handleType == typeof(SafeFreeContextBuffer)) { 
                    refHandle = SafeFreeContextBuffer.CreateEmptyHandle(Library);
                }
                else if (handleType == typeof(SafeFreeCertContext)) {
                    refHandle = new SafeFreeCertContext(); 
                }
                else { 
                    throw new ArgumentException(SR.GetString(SR.SSPIInvalidHandleType, handleType.FullName), "handleType"); 
                }
            } 
            fixed (byte* bufferPtr = buffer) {
                return SafeFreeContextBuffer.QueryContextAttributes(Library, phContext, attribute, bufferPtr, refHandle);
            }
        } 

        public int QuerySecurityContextToken(SafeDeleteContext phContext, out SafeCloseHandle phToken) { 
            throw new NotSupportedException(); 
        }
 
        public int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers) {
            throw new NotSupportedException();
        }
    } 

 
    // For Authentication (Kerberos, NTLM, Negotiate and WDigest): 
    // on Win9x use schannel.dll, on NT we don't care, since its the same DLL
    internal class SSPIAuthType : SSPIInterface { 
        private static readonly SecurDll Library = ComNetOS.IsWin9x ? SecurDll.SECUR32 : SecurDll.SECURITY;
        private static SecurityPackageInfoClass[] m_SecurityPackages;

        public SecurityPackageInfoClass[] SecurityPackages { 
            get {
                return m_SecurityPackages; 
            } 
            set {
                m_SecurityPackages = value; 
            }
        }

        public int EnumerateSecurityPackages(out int pkgnum, out SafeFreeContextBuffer pkgArray) { 
            GlobalLog.Print("SSPIAuthType::EnumerateSecurityPackages()");
            return SafeFreeContextBuffer.EnumeratePackages(Library, out pkgnum, out pkgArray); 
        } 

        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref AuthIdentity authdata, out SafeFreeCredentials outCredential) { 
            return SafeFreeCredentials.AcquireCredentialsHandle(Library, moduleName, usage, ref authdata, out outCredential);
        }

        public int AcquireDefaultCredential(string moduleName, CredentialUse usage, out SafeFreeCredentials outCredential) { 
            return SafeFreeCredentials.AcquireDefaultCredential(Library, moduleName, usage, out outCredential);
        } 
 
        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref SecureCredential authdata, out SafeFreeCredentials outCredential) {
            return SafeFreeCredentials.AcquireCredentialsHandle(Library, moduleName, usage, ref authdata, out outCredential); 
        }

        public int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags) {
            return SafeDeleteContext.AcceptSecurityContext(Library, ref credential, ref context, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags); 
        }
 
        public int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags) { 
            return SafeDeleteContext.AcceptSecurityContext(Library, ref credential, ref context, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags);
        } 

        public int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref ContextFlags outFlags) {
            return SafeDeleteContext.InitializeSecurityContext(Library, ref credential, ref context, targetName, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        } 

        public int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref ContextFlags outFlags) { 
            return SafeDeleteContext.InitializeSecurityContext(Library, ref credential, ref context, targetName, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags); 
        }
 

        private int EncryptMessageHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
                context.DangerousAddRef(ref b);
            } 
            catch(Exception e) {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException)) 
                    throw;
            } 
            catch {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw; 
            }
            finally { 

                if (b)
                {
                    status = UnsafeNclNativeMethods.NativeNTSSPI.EncryptMessage(ref context._handle, 0, inputOutput, sequenceNumber); 
                    context.DangerousRelease();
                } 
            } 
            return status;
        } 

        public int EncryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            if (ComNetOS.IsWin9x){
                throw ExceptionHelper.MethodNotImplementedException; 
            }
            else{ 
                return EncryptMessageHelper(context,inputOutput,sequenceNumber); 
            }
        } 
        private unsafe int DecryptMessageHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false;
            uint qop = 0; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
                context.DangerousAddRef(ref b);
            } 
            catch(Exception e) {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException)) 
                    throw;
            } 
            catch {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw; 
            }
            finally { 

                if (b)
                {
                    status = UnsafeNclNativeMethods.NativeNTSSPI.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, &qop); 
                    context.DangerousRelease();
                } 
            } 

            const uint SECQOP_WRAP_NO_ENCRYPT = 0x80000001; 
            if (status == 0 && qop == SECQOP_WRAP_NO_ENCRYPT)
            {
                GlobalLog.Assert("NativeNTSSPI.DecryptMessage", "Expected qop = 0, returned value = " + qop.ToString("x", CultureInfo.InvariantCulture));
                throw new InvalidOperationException(SR.GetString(SR.net_auth_message_not_encrypted)); 
            }
 
 
            return status;
        } 

        public int DecryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            if (ComNetOS.IsWin9x){
                throw ExceptionHelper.MethodNotImplementedException; 
            }
            else{ 
                return DecryptMessageHelper(context,inputOutput,sequenceNumber); 
            }
        } 

        private int MakeSignatureHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false; 

            RuntimeHelpers.PrepareConstrainedRegions(); 
            try { 
                context.DangerousAddRef(ref b);
            } 
            catch(Exception e) {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException)) 
                    throw;
            } 
            catch {
                if (b)
                {
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw; 
            }
            finally { 

                if (b)
                {
                    const uint SECQOP_WRAP_NO_ENCRYPT = 0x80000001; 
                    status = UnsafeNclNativeMethods.NativeNTSSPI.EncryptMessage(ref context._handle, SECQOP_WRAP_NO_ENCRYPT, inputOutput, sequenceNumber);
                    context.DangerousRelease(); 
                } 
            }
            return status; 
        }


        public int MakeSignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) { 
            if (ComNetOS.IsWin9x){
                throw ExceptionHelper.MethodNotImplementedException; 
            } 
            else{
                return MakeSignatureHelper(context,inputOutput,sequenceNumber); 
            }
        }

        private unsafe int VerifySignatureHelper(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) { 
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false; 
 
            uint qop = 0;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try {
                context.DangerousAddRef(ref b);
            }
            catch(Exception e) { 
                if (b)
                { 
                    context.DangerousRelease(); 
                    b = false;
                } 
                if (!(e is ObjectDisposedException))
                    throw;
            }
            catch { 
                if (b)
                { 
                    context.DangerousRelease(); 
                    b = false;
                } 
                throw;
            }
            finally {
 
                if (b)
                { 
                    status = UnsafeNclNativeMethods.NativeNTSSPI.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, &qop); 
                    context.DangerousRelease();
                } 
            }

            return status;
 
        }
 
        public int VerifySignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber) { 
            if (ComNetOS.IsWin9x){
                throw ExceptionHelper.MethodNotImplementedException; 
            }
            else{
                return VerifySignatureHelper(context,inputOutput,sequenceNumber);
            } 
        }
 
 
        public unsafe int QueryContextAttributes(SafeDeleteContext context, ContextAttribute  attribute, byte[] buffer, Type handleType, out SafeHandle refHandle) {
 
            refHandle = null;
            if (handleType != null) {
                if (handleType == typeof(SafeFreeContextBuffer)) {
                    refHandle = SafeFreeContextBuffer.CreateEmptyHandle(Library); 
                }
                else if (handleType == typeof(SafeFreeCertContext)) { 
                    refHandle = new SafeFreeCertContext(); 
                }
                else { 
                    throw new ArgumentException(SR.GetString(SR.SSPIInvalidHandleType, handleType.FullName), "handleType");
                }
            }
 
            fixed (byte* bufferPtr = buffer) {
                return SafeFreeContextBuffer.QueryContextAttributes(Library, context, attribute, bufferPtr, refHandle); 
            } 
        }
 
        public int QuerySecurityContextToken(SafeDeleteContext phContext, out SafeCloseHandle phToken) {
            if (ComNetOS.IsWin9x) {
                throw new NotSupportedException();
            } 
            else {
                return SafeCloseHandle.GetSecurityContextToken(phContext, out phToken); 
            } 
        }
 
        public int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers) {
            if (ComNetOS.IsWin9x) {
                throw new NotSupportedException();
            } 
            else {
                return SafeDeleteContext.CompleteAuthToken(Library, ref refContext, inputBuffers); 
            } 
        }
 
    }

}
