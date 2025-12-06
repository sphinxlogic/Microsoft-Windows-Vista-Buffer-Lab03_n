//------------------------------------------------------------------------------ 
// <copyright file="StrongNameUtility.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
 
    using System.Diagnostics;
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.Security.Permissions;

    internal class StrongNameUtility { 

        // Help class shouldn't be instantiated. 
        private StrongNameUtility() { 
        }
 
        ///     Free the buffer allocated by strong name functions
        /// </summary>
        /// <param name="pbMemory">address of memory to free</param>
        [DllImport("mscoree.dll")] 
        internal extern static void StrongNameFreeBuffer(IntPtr pbMemory);
 
        /// <summary> 
        ///     Return the last error
        /// </summary> 
        /// <returns>error information for the last strong name call</returns>
        [DllImport("mscoree.dll")]
        internal extern static int StrongNameErrorInfo();
 
        /// <summary>
        ///     Generate a new key pair for strong name use 
        /// </summary> 
        /// <param name="wszKeyContainer">desired key container name</param>
        /// <param name="dwFlags">flags</param> 
        /// <param name="ppbKeyBlob">[out] generated public / private key blob</param>
        /// <param name="pcbKeyBlob">[out] size of the generated blob</param>
        /// <returns>true if the key was generated, false if there was an error</returns>
        [DllImport("mscoree.dll")] 
        internal extern static bool StrongNameKeyGen([MarshalAs(UnmanagedType.LPWStr)]string wszKeyContainer,
                uint dwFlags, [Out]out IntPtr ppbKeyBlob, [Out]out long pcbKeyBlob); 
 
        internal static bool GenerateStrongNameFile(string filename) {
            // variables that hold the unmanaged key 
            IntPtr keyBlob = IntPtr.Zero;
            long generatedSize = 0;

            // create the key 
            bool createdKey = StrongNameKeyGen(null,
                0 /*No flags. 1 is to save the key in the key container */, 
                out keyBlob, out generatedSize); 

            // if there was a problem, translate it and report it 
            if (!createdKey || keyBlob == IntPtr.Zero) {
                throw Marshal.GetExceptionForHR(StrongNameErrorInfo());
            }
 
            try {
                Debug.Assert(keyBlob != IntPtr.Zero); 
 
                // make sure the key size makes sense
                Debug.Assert(generatedSize > 0 && generatedSize <= Int32.MaxValue); 
                if (generatedSize <= 0 || generatedSize > Int32.MaxValue) {
                    throw new InvalidOperationException(SR.GetString(SR.Browser_InvalidStrongNameKey));
                }
 
                // get the key into managed memory
                byte[] key = new byte[generatedSize]; 
                Marshal.Copy(keyBlob, key, 0, (int)generatedSize); 

                // write the key to the specified file 
                using (FileStream snkStream = new FileStream(filename, FileMode.Create, FileAccess.Write)) {
                    using (BinaryWriter snkWriter = new BinaryWriter(snkStream)) {
                        snkWriter.Write(key);
                    } 
                }
            } 
            finally { 
                // release the unmanaged memory the key resides in
                if (keyBlob != IntPtr.Zero) { 
                    StrongNameFreeBuffer(keyBlob);
                }
            }
 
            return true;
        } 
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="StrongNameUtility.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
 
    using System.Diagnostics;
    using System.IO; 
    using System.Runtime.InteropServices;
    using System.Security.Permissions;

    internal class StrongNameUtility { 

        // Help class shouldn't be instantiated. 
        private StrongNameUtility() { 
        }
 
        ///     Free the buffer allocated by strong name functions
        /// </summary>
        /// <param name="pbMemory">address of memory to free</param>
        [DllImport("mscoree.dll")] 
        internal extern static void StrongNameFreeBuffer(IntPtr pbMemory);
 
        /// <summary> 
        ///     Return the last error
        /// </summary> 
        /// <returns>error information for the last strong name call</returns>
        [DllImport("mscoree.dll")]
        internal extern static int StrongNameErrorInfo();
 
        /// <summary>
        ///     Generate a new key pair for strong name use 
        /// </summary> 
        /// <param name="wszKeyContainer">desired key container name</param>
        /// <param name="dwFlags">flags</param> 
        /// <param name="ppbKeyBlob">[out] generated public / private key blob</param>
        /// <param name="pcbKeyBlob">[out] size of the generated blob</param>
        /// <returns>true if the key was generated, false if there was an error</returns>
        [DllImport("mscoree.dll")] 
        internal extern static bool StrongNameKeyGen([MarshalAs(UnmanagedType.LPWStr)]string wszKeyContainer,
                uint dwFlags, [Out]out IntPtr ppbKeyBlob, [Out]out long pcbKeyBlob); 
 
        internal static bool GenerateStrongNameFile(string filename) {
            // variables that hold the unmanaged key 
            IntPtr keyBlob = IntPtr.Zero;
            long generatedSize = 0;

            // create the key 
            bool createdKey = StrongNameKeyGen(null,
                0 /*No flags. 1 is to save the key in the key container */, 
                out keyBlob, out generatedSize); 

            // if there was a problem, translate it and report it 
            if (!createdKey || keyBlob == IntPtr.Zero) {
                throw Marshal.GetExceptionForHR(StrongNameErrorInfo());
            }
 
            try {
                Debug.Assert(keyBlob != IntPtr.Zero); 
 
                // make sure the key size makes sense
                Debug.Assert(generatedSize > 0 && generatedSize <= Int32.MaxValue); 
                if (generatedSize <= 0 || generatedSize > Int32.MaxValue) {
                    throw new InvalidOperationException(SR.GetString(SR.Browser_InvalidStrongNameKey));
                }
 
                // get the key into managed memory
                byte[] key = new byte[generatedSize]; 
                Marshal.Copy(keyBlob, key, 0, (int)generatedSize); 

                // write the key to the specified file 
                using (FileStream snkStream = new FileStream(filename, FileMode.Create, FileAccess.Write)) {
                    using (BinaryWriter snkWriter = new BinaryWriter(snkStream)) {
                        snkWriter.Write(key);
                    } 
                }
            } 
            finally { 
                // release the unmanaged memory the key resides in
                if (keyBlob != IntPtr.Zero) { 
                    StrongNameFreeBuffer(keyBlob);
                }
            }
 
            return true;
        } 
    } 
}
