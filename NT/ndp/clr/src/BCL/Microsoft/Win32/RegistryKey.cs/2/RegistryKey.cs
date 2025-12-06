// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

/* 
  Note on transaction support: 
  Eventually we will want to add support for NT's transactions to our
  RegistryKey API's (possibly Whidbey M3?).  When we do this, here's 
  the list of API's we need to make transaction-aware:

  RegCreateKeyEx
  RegDeleteKey 
  RegDeleteValue
  RegEnumKeyEx 
  RegEnumValue 
  RegOpenKeyEx
  RegQueryInfoKey 
  RegQueryValueEx
  RegSetValueEx

  We can ignore RegConnectRegistry (remote registry access doesn't yet have 
  transaction support) and RegFlushKey.  RegCloseKey doesn't require any
  additional work.  . 
 */ 

/* 
  Note on ACL support:
  The key thing to note about ACL's is you set them on a kernel object like a
  registry key, then the ACL only gets checked when you construct handles to
  them.  So if you set an ACL to deny read access to yourself, you'll still be 
  able to read with that handle, but not with new handles.
 
  Another peculiarity is a Terminal Server app compatibility hack.  The OS 
  will second guess your attempt to open a handle sometimes.  If a certain
  combination of Terminal Server app compat registry keys are set, then the 
  OS will try to reopen your handle with lesser permissions if you couldn't
  open it in the specified mode.  So on some machines, we will see handles that
  may not be able to read or write to a registry key.  It's very strange.  But
  the real test of these handles is attempting to read or set a value in an 
  affected registry key.
 
  For reference, at least two registry keys must be set to particular values 
  for this behavior:
  HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\RegistryExtensionFlags, the least significant bit must be 1. 
  HKLM\SYSTEM\CurrentControlSet\Control\TerminalServer\TSAppCompat must be 1
  There might possibly be an interaction with yet a third registry key as well.

*/ 

 
namespace Microsoft.Win32 { 

    using System; 
    using System.Collections;
    using System.Security;
#if !FEATURE_PAL
    using System.Security.AccessControl; 
#endif
    using System.Security.Permissions; 
    using System.Text; 
    using System.IO;
    using System.Runtime.Remoting; 
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.Versioning;
    using System.Globalization; 

    /** 
     * Registry hive values.  Useful only for GetRemoteBaseKey 
     */
    [Serializable] 
[System.Runtime.InteropServices.ComVisible(true)]
    public enum RegistryHive
    {
        ClassesRoot = unchecked((int)0x80000000), 
        CurrentUser = unchecked((int)0x80000001),
        LocalMachine = unchecked((int)0x80000002), 
        Users = unchecked((int)0x80000003), 
        PerformanceData = unchecked((int)0x80000004),
        CurrentConfig = unchecked((int)0x80000005), 
        DynData = unchecked((int)0x80000006),
    }

 

    /** 
     * Registry encapsulation. To get an instance of a RegistryKey use the 
     * Registry class's static members then call OpenSubKey.
     * 
     * @see Registry
     * @security(checkDllCalls=off)
     * @security(checkClassLinking=on)
     */ 
    [ComVisible(true)]
    public sealed class RegistryKey : MarshalByRefObject, IDisposable { 
 
        // We could use const here, if C# supported ELEMENT_TYPE_I fully.
        internal static readonly IntPtr HKEY_CLASSES_ROOT         = new IntPtr(unchecked((int)0x80000000)); 
        internal static readonly IntPtr HKEY_CURRENT_USER         = new IntPtr(unchecked((int)0x80000001));
        internal static readonly IntPtr HKEY_LOCAL_MACHINE        = new IntPtr(unchecked((int)0x80000002));
        internal static readonly IntPtr HKEY_USERS                = new IntPtr(unchecked((int)0x80000003));
        internal static readonly IntPtr HKEY_PERFORMANCE_DATA     = new IntPtr(unchecked((int)0x80000004)); 
        internal static readonly IntPtr HKEY_CURRENT_CONFIG       = new IntPtr(unchecked((int)0x80000005));
        internal static readonly IntPtr HKEY_DYN_DATA             = new IntPtr(unchecked((int)0x80000006)); 
 
        // Dirty indicates that we have munged data that should be potentially
        // written to disk. 
        //
        private const int STATE_DIRTY        = 0x0001;

        // SystemKey indicates that this is a "SYSTEMKEY" and shouldn't be "opened" 
        // or "closed".
        // 
        private const int STATE_SYSTEMKEY    = 0x0002; 

        // Access 
        //
        private const int STATE_WRITEACCESS  = 0x0004;

        // Indicates if this key is for HKEY_PERFORMANCE_DATA 
        private const int STATE_PERF_DATA    = 0x0008;
 
        // Names of keys.  This array must be in the same order as the HKEY values listed above. 
        //
        private static readonly String[] hkeyNames = new String[] { 
                "HKEY_CLASSES_ROOT",
                "HKEY_CURRENT_USER",
                "HKEY_LOCAL_MACHINE",
                "HKEY_USERS", 
                "HKEY_PERFORMANCE_DATA",
                "HKEY_CURRENT_CONFIG", 
                "HKEY_DYN_DATA" 
                };
 
        // Maximum key name length is 255 on win9X.
        private const int MaxKeyLength = 255;

        private SafeRegistryHandle hkey = null; 
        private int state = 0;
        private String keyName; 
        private bool remoteKey = false; 
        private RegistryKeyPermissionCheck checkMode;
        private static readonly int _SystemDefaultCharSize = 3 - Win32Native.lstrlen(new sbyte [] {0x41, 0x41, 0, 0}); 



        /** 
         * Creates a RegistryKey.
         * 
         * This key is bound to hkey, if writable is <b>false</b> then no write operations 
         * will be allowed.
         */ 
        private RegistryKey(SafeRegistryHandle  hkey, bool writable)
            : this(hkey, writable, false, false, false) {
        }
 

        /** 
         * Creates a RegistryKey. 
         *
         * This key is bound to hkey, if writable is <b>false</b> then no write operations 
         * will be allowed. If systemkey is set then the hkey won't be released
         * when the object is GC'ed.
         * The remoteKey flag when set to true indicates that we are dealing with registry entries
         * on a remote machine and requires the program making these calls to have full trust. 
         */
        private RegistryKey(SafeRegistryHandle hkey, bool writable, bool systemkey, bool remoteKey, bool isPerfData) { 
            this.hkey = hkey; 
            this.keyName = "";
            this.remoteKey = remoteKey; 
            if (systemkey) {
                this.state |= STATE_SYSTEMKEY;
            }
            if (writable) { 
                this.state |= STATE_WRITEACCESS;
            } 
            if (isPerfData) 
                this.state |= STATE_PERF_DATA;
        } 

        /**
         * Closes this key, flushes it to disk if the contents have been modified.
         */ 
        public void Close() {
            Dispose(true); 
        } 

        private void Dispose(bool disposing) { 
            if (hkey != null) {
                bool isPerf = IsPerfDataKey();
                // System keys should never be closed.  However, we want to call RegCloseKey
                // on HKEY_PERFORMANCE_DATA without actually invalidating the RegistryKey. 
                if (!IsSystemKey() || isPerf ) {
                    try { 
                        hkey.Dispose(); 
                    }
                    catch (IOException){ 
                        // we don't really care if the handle is invalid at this point
                    }

                    if (isPerf) 
                        hkey = new SafeRegistryHandle(RegistryKey.HKEY_PERFORMANCE_DATA, !IsWin9x());
                    else 
                        hkey = null; 
                }
            } 
        }

        public void Flush() {
            if (hkey != null) { 
                 if (IsDirty()) {
                     Win32Native.RegFlushKey(hkey); 
                } 
            }
        } 

        /// <internalonly/>
        void IDisposable.Dispose() {
            Dispose(true); 
        }
 
        /** 
         * Creates a new subkey, or opens an existing one.
         * 
         * @param subkey Name or path to subkey to create or open.
         *
         * @return the subkey, or <b>null</b> if the operation failed.
         */ 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public RegistryKey CreateSubKey(String subkey) { 
            return CreateSubKey(subkey, checkMode);
        } 

        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public RegistryKey CreateSubKey(String subkey, RegistryKeyPermissionCheck permissionCheck) {
#if !FEATURE_PAL 
            return CreateSubKey(subkey, permissionCheck, null); 
        }
 
        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public unsafe RegistryKey CreateSubKey(String subkey, RegistryKeyPermissionCheck permissionCheck,  RegistrySecurity registrySecurity) { 
#endif
            ValidateKeyName(subkey); 
            ValidateKeyMode(permissionCheck); 
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash 

            // only keys opened under read mode is not writable
            if (!remoteKey) {
                RegistryKey key = InternalOpenSubKey(subkey, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree)); 
                if (key != null)  { // Key already exits
                    CheckSubKeyWritePermission(subkey); 
                    CheckSubTreePermission(subkey, permissionCheck); 
                    key.checkMode = permissionCheck;
                    return key; 
                }
            }

            CheckSubKeyCreatePermission(subkey); 

            Win32Native.SECURITY_ATTRIBUTES secAttrs = null; 
#if !FEATURE_PAL 
            // For ACL's, get the security descriptor from the RegistrySecurity.
            if (registrySecurity != null) { 
                secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);

                byte[] sd = registrySecurity.GetSecurityDescriptorBinaryForm(); 
                // We allocate memory on the stack to improve the speed.
                // So this part of code can't be refactored into a method. 
                byte* pSecDescriptor = stackalloc byte[sd.Length]; 
                Buffer.memcpy(sd, 0, pSecDescriptor, 0, sd.Length);
                secAttrs.pSecurityDescriptor = pSecDescriptor; 
            }
#endif
            int disposition = 0;
 
            // By default, the new key will be writable.
            SafeRegistryHandle result = null; 
            int ret = Win32Native.RegCreateKeyEx(hkey, 
                subkey,
                0, 
                null,
                0,
                GetRegistryKeyAccess(permissionCheck != RegistryKeyPermissionCheck.ReadSubTree),
                secAttrs, 
                out result,
                out disposition); 
 
            if (ret == 0 && !result.IsInvalid) {
                RegistryKey key = new RegistryKey(result, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree), false, remoteKey, false); 
                CheckSubTreePermission(subkey, permissionCheck);
                key.checkMode = permissionCheck;

                if (subkey.Length == 0) 
                    key.keyName = keyName;
                else 
                    key.keyName = keyName + "\\" + subkey; 
                return key;
            } 
            else if (ret != 0) // syscall failed, ret is an error code.
                Win32Error(ret, keyName + "\\" + subkey);  // Access denied?

            BCLDebug.Assert(false, "Unexpected code path in RegistryKey::CreateSubKey"); 
            return null;
        } 
 
        /**
         * Deletes the specified subkey. Will throw an exception if the subkey has 
         * subkeys. To delete a tree of subkeys use, DeleteSubKeyTree.
         *
         * @param subkey SubKey to delete.
         * 
         * @exception InvalidOperationException thrown if the subkey has child subkeys.
         */ 
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public void DeleteSubKey(String subkey) { 
            DeleteSubKey(subkey, true);
        }

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public void DeleteSubKey(String subkey, bool throwOnMissingSubKey) { 
            ValidateKeyName(subkey); 
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash 
            CheckSubKeyWritePermission(subkey);

            // Open the key we are deleting and check for children. Be sure to
            // explicitly call close to avoid keeping an extra HKEY open. 
            //
            RegistryKey key = InternalOpenSubKey(subkey,false); 
            if (key != null) { 
                try {
                    if (key.InternalSubKeyCount() > 0) { 
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_RegRemoveSubKey);
                    }
                }
                finally { 
                    key.Close();
                } 
 
                int ret = Win32Native.RegDeleteKey(hkey, subkey);
                if (ret!=0) { 
                    if (ret == Win32Native.ERROR_FILE_NOT_FOUND) {
                        if (throwOnMissingSubKey)
                            ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
                    } 
                    else
                        Win32Error(ret, null); 
                } 
            }
            else { // there is no key which also means there is no subkey 
                if (throwOnMissingSubKey)
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        } 

        /** 
         * Recursively deletes a subkey and any child subkeys. 
         *
         * @param subkey SubKey to delete. 
         */
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public void DeleteSubKeyTree(String subkey) { 
            ValidateKeyName(subkey);
 
            // Security concern: Deleting a hive's "" subkey would delete all 
            // of that hive's contents.  Don't allow "".
            if (subkey.Length==0 && IsSystemKey()) { 
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyDelHive);
            }

            EnsureWriteable(); 

            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash 
            CheckSubTreeWritePermission(subkey); 

            RegistryKey key = InternalOpenSubKey(subkey, true); 
            if (key != null) {
                try {
                    if (key.InternalSubKeyCount() > 0) {
                        String[] keys = key.InternalGetSubKeyNames(); 

                        for (int i=0; i<keys.Length; i++) { 
                            key.DeleteSubKeyTreeInternal(keys[i]); 
                        }
                    } 
                }
                finally {
                    key.Close();
                } 

                int ret = Win32Native.RegDeleteKey(hkey, subkey); 
                if (ret!=0) Win32Error(ret, null); 
            }
            else { 
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        }
 
        // An internal version which does no security checks or argument checking.  Skipping the
        // security checks should give us a slight perf gain on large trees. 
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        private void DeleteSubKeyTreeInternal(string subkey) { 
            RegistryKey key = InternalOpenSubKey(subkey, true);
            if (key != null) {
                try {
                    if (key.InternalSubKeyCount() > 0) { 
                        String[] keys = key.InternalGetSubKeyNames();
 
                        for (int i=0; i<keys.Length; i++) { 
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        } 
                    }
                }
                finally {
                    key.Close(); 
                }
 
                int ret = Win32Native.RegDeleteKey(hkey, subkey); 
                if (ret!=0) Win32Error(ret, null);
            } 
            else {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        } 

        /** 
         * Deletes the specified value from this key. 
         *
         * @param name Name of value to delete. 
         */
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public void DeleteValue(String name) { 
            DeleteValue(name, true);
        } 
 
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)] 
        public void DeleteValue(String name, bool throwOnMissingValue) {
            if (name==null) {
               ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name);
            } 

            EnsureWriteable(); 
            CheckValueWritePermission(name); 
            int errorCode = Win32Native.RegDeleteValue(hkey, name);
 
            //
            // From windows 2003 server, if the name is too long we will get error code ERROR_FILENAME_EXCED_RANGE
            // This still means the name doesn't exist. We need to be consistent with previous OS.
            // 
            if (errorCode == Win32Native.ERROR_FILE_NOT_FOUND || errorCode == Win32Native.ERROR_FILENAME_EXCED_RANGE) {
                if (throwOnMissingValue) { 
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyValueAbsent); 
                }
                // Otherwise, just return giving no indication to the user. 
                // (For compatibility)
            }
            // We really should throw an exception here if errorCode was bad,
            // but we can't for compatibility reasons. 
            BCLDebug.Correctness(errorCode == 0, "RegDeleteValue failed.  Here's your error code: "+errorCode);
        } 
 
        /**
         * Retrieves a new RegistryKey that represents the requested key. Valid 
         * values are:
         *
         * HKEY_CLASSES_ROOT,
         * HKEY_CURRENT_USER, 
         * HKEY_LOCAL_MACHINE,
         * HKEY_USERS, 
         * HKEY_PERFORMANCE_DATA, 
         * HKEY_CURRENT_CONFIG,
         * HKEY_DYN_DATA. 
         *
         * @param hKey HKEY_* to open.
         *
         * @return the RegistryKey requested. 
         */
        internal static RegistryKey GetBaseKey(IntPtr hKey) { 
            int index = ((int)hKey) & 0x0FFFFFFF; 
            BCLDebug.Assert(index >= 0  && index < hkeyNames.Length, "index is out of range!");
            BCLDebug.Assert((((int)hKey) & 0xFFFFFFF0) == 0x80000000, "Invalid hkey value!"); 

            bool isPerf = hKey == HKEY_PERFORMANCE_DATA;
            // only mark the SafeHandle as ownsHandle if the key is HKEY_PERFORMANCE_DATA and we're not on win9x.
            SafeRegistryHandle srh = new SafeRegistryHandle(hKey, isPerf && !IsWin9x()); 

            RegistryKey key = new RegistryKey(srh, true, true,false, isPerf); 
            key.checkMode = RegistryKeyPermissionCheck.Default; 
            key.keyName = hkeyNames[index];
            return key; 
        }

        /**
         * Retrieves a new RegistryKey that represents the requested key on a foreign 
         * machine.  Valid values for hKey are members of the RegistryHive enum, or
         * Win32 integers such as: 
         * 
         * HKEY_CLASSES_ROOT,
         * HKEY_CURRENT_USER, 
         * HKEY_LOCAL_MACHINE,
         * HKEY_USERS,
         * HKEY_PERFORMANCE_DATA,
         * HKEY_CURRENT_CONFIG, 
         * HKEY_DYN_DATA.
         * 
         * @param hKey HKEY_* to open. 
         * @param machineName the machine to connect to
         * 
         * @return the RegistryKey requested.
         */
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, String machineName) {
            if (machineName==null) 
                throw new ArgumentNullException("machineName"); 
            int index = (int)hKey & 0x0FFFFFFF;
            if (index < 0 || index >= hkeyNames.Length || ((int)hKey & 0xFFFFFFF0) != 0x80000000) { 
                throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyOutOfRange"));
            }

            CheckUnmanagedCodePermission(); 
            // connect to the specified remote registry
            SafeRegistryHandle foreignHKey = null; 
            int ret = Win32Native.RegConnectRegistry(machineName, new SafeRegistryHandle(new IntPtr((int)hKey), false), out foreignHKey); 

            if (ret == Win32Native.ERROR_DLL_INIT_FAILED) 
                // return value indicates an error occurred
                throw new ArgumentException(Environment.GetResourceString("Arg_DllInitFailure"));

            if (ret != 0) 
                Win32ErrorStatic(ret, null);
 
            if (foreignHKey.IsInvalid) 
                // return value indicates an error occurred
                throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyNoRemoteConnect", machineName)); 

            RegistryKey key = new RegistryKey(foreignHKey, true, false, true, ((IntPtr) hKey) == HKEY_PERFORMANCE_DATA);
            key.checkMode = RegistryKeyPermissionCheck.Default;
            key.keyName = hkeyNames[index]; 
            return key;
        } 
 
        /**
         * Retrieves a subkey. If readonly is <b>true</b>, then the subkey is opened with 
         * read-only access.
         *
         * @param name Name or path of subkey to open.
         * @param readonly Set to <b>true</b> if you only need readonly access. 
         *
         * @return the Subkey requested, or <b>null</b> if the operation failed. 
         */ 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public RegistryKey OpenSubKey(string name, bool writable ) {
            ValidateKeyName(name);
            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash 

            CheckOpenSubKeyPermission(name, writable); 
            SafeRegistryHandle result = null; 
            int ret = Win32Native.RegOpenKeyEx(hkey, name, 0, GetRegistryKeyAccess(writable), out result);
 
            if (ret == 0 && !result.IsInvalid) {
                RegistryKey key = new RegistryKey(result, writable, false, remoteKey, false);
                key.checkMode = GetSubKeyPermissonCheck(writable);
                key.keyName = keyName + "\\" + name; 
                return key;
            } 
 
            // Return null if we didn't find the key.
            if (ret == Win32Native.ERROR_ACCESS_DENIED || ret == Win32Native.ERROR_BAD_IMPERSONATION_LEVEL) { 
                // We need to throw SecurityException here for compatibility reasons,
                // although UnauthorizedAccessException will make more sense.
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
            } 

            return null; 
        } 

        [ComVisible(false)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public RegistryKey OpenSubKey(String name, RegistryKeyPermissionCheck permissionCheck) {
            ValidateKeyMode(permissionCheck); 
            return InternalOpenSubKey(name, permissionCheck, GetRegistryKeyAccess(permissionCheck));
        } 
 
        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public RegistryKey OpenSubKey(String name, RegistryKeyPermissionCheck permissionCheck, RegistryRights rights) {
            return InternalOpenSubKey(name, permissionCheck, (int)rights);
        } 

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)] 
        private RegistryKey InternalOpenSubKey(String name, RegistryKeyPermissionCheck permissionCheck, int rights) {
            ValidateKeyName(name); 
            ValidateKeyMode(permissionCheck);
            ValidateKeyRights(rights);
            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash 

            CheckOpenSubKeyPermission(name, permissionCheck); 
            SafeRegistryHandle result = null; 
            int ret = Win32Native.RegOpenKeyEx(hkey, name, 0, rights, out result);
 
            if (ret == 0 && !result.IsInvalid) {
                RegistryKey key = new RegistryKey(result, (permissionCheck == RegistryKeyPermissionCheck.ReadWriteSubTree), false, remoteKey, false);
                key.keyName = keyName + "\\" + name;
                key.checkMode = permissionCheck; 
                return key;
            } 
 
            // Return null if we didn't find the key.
            if (ret == Win32Native.ERROR_ACCESS_DENIED || ret == Win32Native.ERROR_BAD_IMPERSONATION_LEVEL) { 
                // We need to throw SecurityException here for compatiblity reason,
                // although UnauthorizedAccessException will make more sense.
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
            } 

            return null; 
        } 

 
        // This required no security checks. This is to get around the Deleting SubKeys which only require
        // write permission. They call OpenSubKey which required read. Now instead call this function w/o security checks
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        internal RegistryKey InternalOpenSubKey(String name, bool writable) {
            ValidateKeyName(name); 
            EnsureNotDisposed(); 

            int winAccess = GetRegistryKeyAccess(writable); 
            SafeRegistryHandle result = null;
            int ret = Win32Native.RegOpenKeyEx(hkey, name, 0, winAccess, out result);

            if (ret == 0 && !result.IsInvalid) { 
                RegistryKey key = new RegistryKey(result, writable, false, remoteKey, false);
                key.keyName = keyName + "\\" + name; 
                return key; 
            }
            return null; 
        }

        /**
         * Returns a subkey with read only permissions. 
         *
         * @param name Name or path of subkey to open. 
         * 
         * @return the Subkey requested, or <b>null</b> if the operation failed.
         */ 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public RegistryKey OpenSubKey(String name) {
            return OpenSubKey(name, false); 
        }
 
        /** 
         * Retrieves the count of subkeys.
         * 
         * @return a count of subkeys.
         */
        public int SubKeyCount {
            get { 
                CheckKeyReadPermission();
                return InternalSubKeyCount(); 
            } 
        }
 
        internal int InternalSubKeyCount() {
                EnsureNotDisposed();

                int subkeys = 0; 
                int junk = 0;
                int ret = Win32Native.RegQueryInfoKey(hkey, 
                                          null, 
                                          null,
                                          Win32Native.NULL, 
                                          ref subkeys,  // subkeys
                                          null,
                                          null,
                                          ref junk,     // values 
                                          null,
                                          null, 
                                          null, 
                                          null);
 
                if (ret != 0)
                    Win32Error(ret, null);
                return subkeys;
        } 

        /** 
         * Retrieves an array of strings containing all the subkey names. 
         *
         * @return all subkey names. 
         */
        public String[] GetSubKeyNames() {
            CheckKeyReadPermission();
            return InternalGetSubKeyNames(); 
        }
 
        internal String[] InternalGetSubKeyNames() { 
            EnsureNotDisposed();
            int subkeys = InternalSubKeyCount(); 
            String[] names = new String[subkeys];  // Returns 0-length array if empty.

            if (subkeys > 0) {
                StringBuilder name = new StringBuilder(256); 
                int namelen;
 
                for (int i=0; i<subkeys; i++) { 
                    namelen = name.Capacity; // Don't remove this. The API's doesn't work if this is not properly initialised.
                    int ret = Win32Native.RegEnumKeyEx(hkey, 
                        i,
                        name,
                        out namelen,
                        null, 
                        null,
                        null, 
                        null); 
                    if (ret != 0)
                        Win32Error(ret, null); 
                    names[i] = name.ToString();
                }
            }
 
            return names;
        } 
 
        /**
         * Retrieves the count of values. 
         *
         * @return a count of values.
         */
        public int ValueCount { 
            get {
                CheckKeyReadPermission(); 
                return InternalValueCount(); 
            }
        } 

        internal int InternalValueCount() {
            EnsureNotDisposed();
            int values = 0; 
            int junk = 0;
            int ret = Win32Native.RegQueryInfoKey(hkey, 
                                      null, 
                                      null,
                                      Win32Native.NULL, 
                                      ref junk,     // subkeys
                                      null,
                                      null,
                                      ref values,   // values 
                                      null,
                                      null, 
                                      null, 
                                      null);
            if (ret != 0) 
               Win32Error(ret, null);
            return values;
        }
 
        /**
         * Retrieves an array of strings containing all the value names. 
         * 
         * @return all value names.
         */ 
        public String[] GetValueNames() {
            CheckKeyReadPermission();
            EnsureNotDisposed();
 
            int values = InternalValueCount();
            String[] names = new String[values]; 
 
            if (values > 0) {
                StringBuilder name = new StringBuilder(256); 
                int namelen;

                for (int i=0; i<values; i++) {
                    namelen = name.Capacity; 

                    int ret = Win32Native.RegEnumValue(hkey, 
                        i, 
                        name,
                        ref namelen, 
                        Win32Native.NULL,
                        null,
                        null,
                        null); 

                    // Remote Win9x machines always return ERROR_MORE_DATA if you don't pass in 
                    // a buffer.  In addition, they won't fill in the name properly unless certain 
                    // other conditions are met.  We can't know what the OS of the remote
                    // machine is, so we'll do this for all remote machines that return 
                    // ERROR_MORE_DATA for a key other than HKEY_PERFORMANCE_DATA.
                    if (ret == Win32Native.ERROR_MORE_DATA && !IsPerfDataKey() && remoteKey){
                        int [] datalen = new int[1];
                        byte [] data = new byte[5]; 
                        datalen[0] = 5;
                        ret = Win32Native.RegEnumValueA(hkey, 
                                                        i, 
                                                        name,
                                                        ref namelen, 
                                                        Win32Native.NULL,
                                                        null,
                                                        data,
                                                        datalen); 
                            if (ret == Win32Native.ERROR_MORE_DATA){
                                datalen[0] = 0; 
                                ret = Win32Native.RegEnumValueA(hkey, 
                                                                 i,
                                                                 name, 
                                                                 ref namelen,
                                                                 Win32Native.NULL,
                                                                 null,
                                                                 null, 
                                                                 datalen);
                        } 
                    } 

                    if (ret != 0) { 
                        // ignore ERROR_MORE_DATA if we're querying HKEY_PERFORMANCE_DATA
                        if (!(IsPerfDataKey() && ret == Win32Native.ERROR_MORE_DATA))
                            Win32Error(ret, null);
                    } 
                    names[i] = name.ToString();
                } 
            } 

            return names; 
        }

        /**
         * Retrieves the specified value. <b>null</b> is returned if the value 
         * doesn't exist.
         * 
         * <p>Note that <var>name</var> can be null or "", at which point the 
         * unnamed or default value of this Registry key is returned, if any.
         * 
         * @param name Name of value to retrieve.
         *
         * @return the data associated with the value.
         */ 
        public Object GetValue(String name) {
            CheckValueReadPermission(name); 
            return InternalGetValue(name, null, false, true); 
        }
 
        /**
         * Retrieves the specified value. <i>defaultValue</i> is returned if the value doesn't exist.
         *
         * <p>Note that <var>name</var> can be null or "", at which point the 
         * unnamed or default value of this Registry key is returned, if any.
         * The default values for RegistryKeys are OS-dependent.  NT doesn't 
         * have them by default, but they can exist and be of any type.  On 
         * Win95, the default value is always an empty key of type REG_SZ.
         * Win98 supports default values of any type, but defaults to REG_SZ. 
         *
         * @param name Name of value to retrieve.
         * @param defaultValue Value to return if <i>name</i> doesn't exist.
         * 
         * @return the data associated with the value.
         */ 
        public Object GetValue(String name, Object defaultValue) { 
            CheckValueReadPermission(name);
            return InternalGetValue(name, defaultValue, false, true); 
        }

        [ComVisible(false)]
        public Object GetValue(String name, Object defaultValue, RegistryValueOptions options) { 
            if( options < RegistryValueOptions.None || options > RegistryValueOptions.DoNotExpandEnvironmentNames) {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)options), "options"); 
            } 
            bool doNotExpand = (options == RegistryValueOptions.DoNotExpandEnvironmentNames);
            CheckValueReadPermission(name); 
            return InternalGetValue(name, defaultValue, doNotExpand, true);
        }

        internal Object InternalGetValue(String name, Object defaultValue, bool doNotExpand, bool checkSecurity) { 
            if (checkSecurity) {
                // Name can be null!  It's the most common use of RegQueryValueEx 
                EnsureNotDisposed(); 
            }
 
            Object data = defaultValue;
            int type = 0;
            int datasize = 0;
 
            int ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
 
            if (ret != 0) { 
                if (IsPerfDataKey()) {
                    int size = 65000; 
                    int sizeInput = size;

                    int r;
                    byte[] blob = new byte[size]; 
                    while (Win32Native.ERROR_MORE_DATA == (r = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref sizeInput))) {
                        size *= 2; 
                        sizeInput = size; 
                        blob = new byte[size];
                    } 
                    if (r != 0)
                        Win32Error(r, name);
                    return blob;
                } 
                else {
                    // Win9x will return ERROR_MORE_DATA even in success cases, so we want to continue on through 
                    // the function. 
                    if (ret != Win32Native.ERROR_MORE_DATA)
                        return data; 
                }
            }

            switch (type) { 
            case Win32Native.REG_DWORD_BIG_ENDIAN:
            case Win32Native.REG_BINARY: { 
                                 byte[] blob = new byte[datasize]; 
                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                                 data = blob; 
                             }
                             break;
            case Win32Native.REG_QWORD:
                             {    // also REG_QWORD_LITTLE_ENDIAN 
                                 long blob = 0;
                                 BCLDebug.Assert(datasize==8, "datasize==8"); 
                                 // Here, datasize must be 8 when calling this 
                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize);
 
                                 data = blob;
                             }
                             break;
            case Win32Native.REG_DWORD: 
                             {    // also REG_DWORD_LITTLE_ENDIAN
                                 int blob = 0; 
                                 BCLDebug.Assert(datasize==4, "datasize==4"); 
                                 // Here, datasize must be four when calling this
                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize); 

                                 data = blob;
                             }
                             break; 

            case Win32Native.REG_SZ: 
                             { 
                                 if (_SystemDefaultCharSize != 1) {
                                     BCLDebug.Assert(_SystemDefaultCharSize==2, "_SystemDefaultCharSize==2"); 
                                     StringBuilder blob = new StringBuilder(datasize/2);
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                                     data = blob.ToString();
                                 } 
                                 else {
                                     byte[] blob = new byte[datasize]; 
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize); 
                                     data = Encoding.Default.GetString(blob, 0, blob.Length-1);
                                 } 
                             }
                             break;

            case Win32Native.REG_EXPAND_SZ: 
                              {
                                 if (_SystemDefaultCharSize != 1) { 
                                     StringBuilder blob = new StringBuilder(datasize/2); 
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                                     if (doNotExpand) 
                                        data = blob.ToString();
                                     else
                                        data = Environment.ExpandEnvironmentVariables(blob.ToString());
                                 } 
                                 else {
                                     byte[] blob = new byte[datasize]; 
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize); 
                                     String unexpandedData = Encoding.Default.GetString(blob, 0, blob.Length-1);
                                     if (doNotExpand) 
                                        data = unexpandedData;
                                     else
                                        data = Environment.ExpandEnvironmentVariables(unexpandedData);
                                 } 
                             }
                             break; 
            case Win32Native.REG_MULTI_SZ: 
                             {
                                 bool unicode = (_SystemDefaultCharSize != 1); 

                                 IList strings = new ArrayList();

                                 if (unicode) { 
                                     char[] blob = new char[datasize/2];
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize); 
 
                                     int cur = 0;
                                     int len = blob.Length; 

                                     while (ret == 0 && cur < len) {

                                         int nextNull = cur; 
                                         while (nextNull < len && blob[nextNull] != (char)0) {
                                             nextNull++; 
                                         } 

                                         if (nextNull < len) { 
                                             BCLDebug.Assert(blob[nextNull] == (char)0, "blob[nextNull] should be 0");
                                             if (nextNull-cur > 0) {
                                                 strings.Add(new String(blob, cur, nextNull-cur));
                                             } 
                                             else {
                                                // we found an empty string.  But if we're at the end of the data, 
                                                // it's just the extra null terminator. 
                                                if (nextNull != len-1)
                                                    strings.Add(String.Empty); 
                                             }
                                         }
                                         else {
                                             strings.Add(new String(blob, cur, len-cur)); 
                                         }
                                         cur = nextNull+1; 
                                     } 

                                 } 
                                 else {
                                     byte[] blob = new byte[datasize];
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
 
                                     int cur = 0;
                                     int len = blob.Length; 
 
                                     while (ret == 0 && cur < len) {
 
                                         int nextNull = cur;
                                         while (nextNull < len && blob[nextNull] != (byte)0) {
                                             nextNull++;
                                         } 

                                         if (nextNull < len) { 
                                             BCLDebug.Assert(blob[nextNull] == (byte)0, "blob[nextNull] should be 0"); 
                                             if (nextNull-cur > 0) {
                                                strings.Add(Encoding.Default.GetString(blob, cur, nextNull-cur)); 
                                             }
                                             else {
                                                // we found an empty string.  But if we're at the end of the data,
                                                // it's just the extra null terminator. 
                                                if (nextNull != len-1)
                                                    strings.Add(String.Empty); 
                                             } 
                                         }
                                         else { 
                                            strings.Add(Encoding.Default.GetString(blob, cur, len-cur));
                                         }
                                         cur = nextNull + 1;
                                     } 
                                 }
 
                                 data = new String[strings.Count]; 
                                 strings.CopyTo((Array)data, 0);
                                 //data = strings.GetAllItems(String.class); 
                             }
                             break;
            case Win32Native.REG_NONE:
            case Win32Native.REG_LINK: 
            default:
                break; 
            } 

            return data; 
        }


        [ComVisible(false)] 
        public RegistryValueKind GetValueKind(string name) {
            CheckValueReadPermission(name); 
            EnsureNotDisposed(); 

            int type = 0; 
            int datasize = 0;
            int ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
            if (ret != 0)
                Win32Error(ret, null); 

            if (!Enum.IsDefined(typeof(RegistryValueKind), type)) 
                return RegistryValueKind.Unknown; 
            else
                return (RegistryValueKind) type; 
        }

        /**
         * Retrieves the current state of the dirty property. 
         *
         * A key is marked as dirty if any operation has occured that modifies the 
         * contents of the key. 
         *
         * @return <b>true</b> if the key has been modified. 
         */
        private bool IsDirty() {
            return (this.state & STATE_DIRTY) != 0;
        } 

        private bool IsSystemKey() { 
            return (this.state & STATE_SYSTEMKEY) != 0; 
        }
 
        private bool IsWritable() {
            return (this.state & STATE_WRITEACCESS) != 0;
        }
 
        private bool IsPerfDataKey() {
            return (this.state & STATE_PERF_DATA) != 0; 
        } 

        private static bool IsWin9x() { 
            return ((Environment.OSInfo & Environment.OSName.Win9x) != 0);
        }

        public String Name { 
            get {
                EnsureNotDisposed(); 
                return keyName; 
            }
        } 

        private void SetDirty() {
            this.state |= STATE_DIRTY;
        } 

        /** 
         * Sets the specified value. 
         *
         * @param name Name of value to store data in. 
         * @param value Data to store.
         */
        public void SetValue(String name, Object value) {
            SetValue(name, value, RegistryValueKind.Unknown); 
        }
 
        [ComVisible(false)] 
        public unsafe void SetValue(String name, Object value, RegistryValueKind valueKind) {
            if (value==null) 
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);

            if (name != null && name.Length > MaxKeyLength) {
                throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyStrLenBug")); 
            }
 
            if (!Enum.IsDefined(typeof(RegistryValueKind), valueKind)) 
                throw new ArgumentException(Environment.GetResourceString("Arg_RegBadKeyKind"), "valueKind");
 
            EnsureWriteable();

            if (!remoteKey && ContainsRegistryValue(name)) { // Existing key
                CheckValueWritePermission(name); 
            }
             else { // Creating a new value 
                CheckValueCreatePermission(name); 
            }
 
            if (valueKind == RegistryValueKind.Unknown) {
                // this is to maintain compatibility with the old way of autodetecting the type.
                // SetValue(string, object) will come through this codepath.
                valueKind = CalculateValueKind(value); 
            }
 
            int ret = 0; 
            try {
                switch (valueKind) { 
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.String:
                        {
                            String data = value.ToString(); 
                            if (_SystemDefaultCharSize == 1) {
                                byte[] blob = Encoding.Default.GetBytes(data); 
                                byte[] rawdata = new byte[blob.Length+1]; 
                                Array.Copy(blob, 0, rawdata, 0, blob.Length);
 
                                ret = Win32Native.RegSetValueEx(hkey,
                                    name,
                                    0,
                                    valueKind, 
                                    rawdata,
                                    rawdata.Length); 
                            } 
                            else {
                                ret = Win32Native.RegSetValueEx(hkey, 
                                    name,
                                    0,
                                    valueKind,
                                    data, 
                                    data.Length * 2 + 2);
                            } 
                            break; 
                        }
 
                    case RegistryValueKind.MultiString:
                        {
                            // Other thread might modify the input array after we calculate the buffer length.
                            // Make a copy of the input array to be safe. 
                            string[] dataStrings = (string[])(((string[])value).Clone());
                            bool unicode = (_SystemDefaultCharSize != 1); 
 
                            int sizeInBytes = 0;
 
                            // First determine the size of the array
                            //
                            if (unicode) {
                                for (int i=0; i<dataStrings.Length; i++) { 
                                    if (dataStrings[i] == null) {
                                        ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetStrArrNull); 
                                    } 
                                    sizeInBytes += (dataStrings[i].Length+1) * 2;
                                } 
                                sizeInBytes += 2;
                            }
                            else {
                                for (int i=0; i<dataStrings.Length; i++) { 
                                    if (dataStrings[i] == null) {
                                        ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetStrArrNull); 
                                    } 
                                    sizeInBytes += (Encoding.Default.GetByteCount(dataStrings[i]) + 1);
                                } 
                                sizeInBytes ++;
                            }

                            byte[] basePtr = new byte[sizeInBytes]; 
                            fixed(byte* b = basePtr) {
                                IntPtr currentPtr = new IntPtr( (void *) b); 
 
                                // Write out the strings...
                                // 
                                for (int i=0; i<dataStrings.Length; i++) {
                                     if (unicode) { // Assumes that the Strings are always null terminated.
                                        String.InternalCopy(dataStrings[i],currentPtr,(dataStrings[i].Length*2));
                                        currentPtr = new IntPtr((long)currentPtr + (dataStrings[i].Length*2)); 
                                        *(char*)(currentPtr.ToPointer()) = '\0';
                                        currentPtr = new IntPtr((long)currentPtr + 2); 
                                    } 
                                    else {
                                        byte[] data = Encoding.Default.GetBytes(dataStrings[i]); 
                                        Buffer.memcpy(data, 0, (byte*)currentPtr.ToPointer(), 0, data.Length) ;
                                        currentPtr = new IntPtr((long)currentPtr + data.Length);
                                        *(byte*)(currentPtr.ToPointer()) = 0;
                                        currentPtr = new IntPtr((long)currentPtr + 1 ); 
                                    }
                                } 
 
                                if (unicode) {
                                    *(char*)(currentPtr.ToPointer()) = '\0'; 
                                    currentPtr = new IntPtr((long)currentPtr + 2);
                                }
                                else {
                                    *(byte*)(currentPtr.ToPointer()) = 0; 
                                    currentPtr = new IntPtr((long)currentPtr + 1);
                                } 
 
                                ret = Win32Native.RegSetValueEx(hkey,
                                    name, 
                                    0,
                                    RegistryValueKind.MultiString,
                                    basePtr,
                                    sizeInBytes); 
                            }
                            break; 
                        } 

                    case RegistryValueKind.Binary: 
                        byte[] dataBytes = (byte[]) value;
                        ret = Win32Native.RegSetValueEx(hkey,
                            name,
                            0, 
                            RegistryValueKind.Binary,
                            dataBytes, 
                            dataBytes.Length); 
                        break;
 
                    case RegistryValueKind.DWord:
                        {
                            // We need to use Convert here because we could have a boxed type cannot be
                            // unboxed and cast at the same time.  I.e. ((int)(object)(short) 5) will fail. 
                            int data = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
 
                            ret = Win32Native.RegSetValueEx(hkey, 
                                name,
                                0, 
                                RegistryValueKind.DWord,
                                ref data,
                                4);
                            break; 
                        }
 
                    case RegistryValueKind.QWord: 
                        {
                            long data = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture); 

                            ret = Win32Native.RegSetValueEx(hkey,
                                name,
                                0, 
                                RegistryValueKind.QWord,
                                ref data, 
                                8); 
                            break;
                        } 
                }
            }
            catch (OverflowException) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind); 
            }
            catch (InvalidOperationException) { 
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind); 
            }
            catch (FormatException) { 
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }
            catch (InvalidCastException) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind); 
            }
 
            if (ret == 0) { 
                SetDirty();
            } 
            else
                Win32Error(ret, null);

        } 

        private RegistryValueKind CalculateValueKind(Object value) { 
            // This logic matches what used to be in SetValue(string name, object value) in the v1.0 and v1.1 days. 
            // Even though we could add detection for an int64 in here, we want to maintain compatibility with the
            // old behavior. 
            if (value is Int32)
                return RegistryValueKind.DWord;
            else if (value is Array) {
                if (value is byte[]) 
                    return RegistryValueKind.Binary;
                else if (value is String[]) 
                    return RegistryValueKind.MultiString; 
                else
                    throw new ArgumentException(Environment.GetResourceString("Arg_RegSetBadArrType", value.GetType().Name)); 
            }
            else
                return RegistryValueKind.String;
        } 

        /** 
         * Retrieves a string representation of this key. 
         *
         * @return a string representing the key. 
         */
        public override String ToString() {
            EnsureNotDisposed();
            return keyName; 
        }
 
#if !FEATURE_PAL 
        public RegistrySecurity GetAccessControl() {
            return GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group); 
        }

        public RegistrySecurity GetAccessControl(AccessControlSections includeSections) {
            EnsureNotDisposed(); 
            return new RegistrySecurity(hkey, keyName, includeSections);
        } 
 
        public void SetAccessControl(RegistrySecurity registrySecurity) {
            EnsureWriteable(); 
            if (registrySecurity == null)
                throw new ArgumentNullException("registrySecurity");

            registrySecurity.Persist(hkey, keyName); 
        }
#endif 
 
        /**
         * After calling GetLastWin32Error(), it clears the last error field, 
         * so you must save the HResult and pass it to this method.  This method
         * will determine the appropriate exception to throw dependent on your
         * error, and depending on the error, insert a string into the message
         * gotten from the ResourceManager. 
         */
        internal void Win32Error(int errorCode, String str) { 
            switch (errorCode) { 
                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str != null) 
                        throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key", str));
                    else
                        throw new UnauthorizedAccessException();
 
                case Win32Native.ERROR_INVALID_HANDLE:
                    this.hkey.SetHandleAsInvalid(); 
                    this.hkey = null; 
                    goto default;
 
                case Win32Native.ERROR_MORE_DATA:
                    // ignore ERROR_MORE_DATA from remote machines.  Win9x machines return this when we try to get the size or
                    // type of a value without the data.
                    if (remoteKey) 
                        return;
                    else 
                        goto default; 

                case Win32Native.ERROR_FILE_NOT_FOUND: 
                    throw new IOException(Environment.GetResourceString("Arg_RegKeyNotFound"), errorCode);

                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode); 
            }
        } 
        internal static void Win32ErrorStatic(int errorCode, String str) { 
            switch (errorCode) {
                case Win32Native.ERROR_ACCESS_DENIED: 
                    if (str != null)
                        throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key", str));
                    else
                        throw new UnauthorizedAccessException(); 

                default: 
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode); 
            }
        } 

        internal static String FixupName(String name)
        {
            BCLDebug.Assert(name!=null,"[FixupName]name!=null"); 
            if (name.IndexOf('\\') == -1)
                return name; 
 
            StringBuilder sb = new StringBuilder(name);
            FixupPath(sb); 
            int temp = sb.Length - 1;
            if (sb[temp] == '\\') // Remove trailing slash
                sb.Length = temp;
            return sb.ToString(); 
        }
 
 
        private static void FixupPath(StringBuilder path)
        { 
            int length  = path.Length;
            bool fixup = false;
            char markerChar = (char)0xFFFF;
 
            int i = 1;
            while (i < length - 1) 
            { 
                if (path[i] == '\\')
                { 
                    i++;
                    while (i < length)
                    {
                        if (path[i] == '\\') 
                        {
                           path[i] = markerChar; 
                           i++; 
                           fixup = true;
                        } 
                        else
                           break;
                    }
 
                }
                i++; 
            } 

            if (fixup) 
            {
                i = 0;
                int j = 0;
                while (i < length) 
                {
                    if(path[i] == markerChar) 
                    { 
                        i++;
                        continue; 
                    }
                    path[j] = path[i];
                    i++;
                    j++; 
                }
                path.Length += j - i; 
            } 

        } 

        private void CheckOpenSubKeyPermission(string subkeyName, bool subKeyWritable) {
            // If the parent key is not opened under default mode, we have access already.
            // If the parent key is opened under default mode, we need to check for permission. 
            if(checkMode == RegistryKeyPermissionCheck.Default) {
                CheckSubKeyReadPermission(subkeyName); 
            } 

            if( subKeyWritable && (checkMode == RegistryKeyPermissionCheck.ReadSubTree)) { 
                CheckSubTreeReadWritePermission(subkeyName);
            }
        }
 
        private void CheckOpenSubKeyPermission(string subkeyName, RegistryKeyPermissionCheck subKeyCheck) {
            if(subKeyCheck == RegistryKeyPermissionCheck.Default) { 
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    CheckSubKeyReadPermission(subkeyName);
                } 
            }
            CheckSubTreePermission(subkeyName, subKeyCheck);
        }
 
        private void CheckSubTreePermission(string subkeyName, RegistryKeyPermissionCheck subKeyCheck) {
            if( subKeyCheck == RegistryKeyPermissionCheck.ReadSubTree) { 
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    CheckSubTreeReadPermission(subkeyName);
                } 
            }
            else if(subKeyCheck == RegistryKeyPermissionCheck.ReadWriteSubTree) {
                if( checkMode != RegistryKeyPermissionCheck.ReadWriteSubTree) {
                    CheckSubTreeReadWritePermission(subkeyName); 
                }
            } 
        } 

        private void  CheckSubKeyWritePermission(string subkeyName) { 
            if( remoteKey) {
                CheckUnmanagedCodePermission();
            }
            else { 
                BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating sub key under read-only key!");
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    // If we want to open a subkey of a read-only key as writeable, we need to do the check. 
                    new RegistryPermission(RegistryPermissionAccess.Write, keyName + "\\" + subkeyName + "\\.").Demand();
                } 
            }
        }

        private void  CheckSubKeyReadPermission(string subkeyName) { 
            BCLDebug.Assert(checkMode == RegistryKeyPermissionCheck.Default, "Should be called from a key opened under default mode only!");
            if( remoteKey) { 
                CheckUnmanagedCodePermission(); 
            }
            else { 
                new RegistryPermission(RegistryPermissionAccess.Read, keyName + "\\" + subkeyName + "\\.").Demand();
            }
        }
 
        private void  CheckSubKeyCreatePermission(string subkeyName) {
            if( remoteKey) { 
                CheckUnmanagedCodePermission(); 
            }
            else { 
                BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating sub key under read-only key!");
                if( checkMode == RegistryKeyPermissionCheck.Default) {
                    new RegistryPermission(RegistryPermissionAccess.Create, keyName + "\\" + subkeyName + "\\.").Demand();
                } 
            }
        } 
 
        private void  CheckSubTreeReadPermission(string subkeyName) {
            if( remoteKey) { 
                CheckUnmanagedCodePermission();
            }
            else {
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    new RegistryPermission(RegistryPermissionAccess.Read, keyName + "\\" + subkeyName + "\\").Demand();
                } 
            } 
        }
 
        private void  CheckSubTreeWritePermission(string subkeyName) {
            if( remoteKey) {
                CheckUnmanagedCodePermission();
            } 
            else {
                BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow writing value to read-only key!"); 
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    new RegistryPermission(RegistryPermissionAccess.Write, keyName + "\\" + subkeyName + "\\").Demand();
                } 
            }
        }

        private void  CheckSubTreeReadWritePermission(string subkeyName) { 
            if( remoteKey) {
                CheckUnmanagedCodePermission(); 
            } 
            else {
                // If we want to open a subkey of a read-only key as writeable, we need to do the check. 
                new RegistryPermission(RegistryPermissionAccess.Write |RegistryPermissionAccess.Read,
                        keyName + "\\" + subkeyName ).Demand();
            }
        } 

        static private void  CheckUnmanagedCodePermission() { 
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand(); 
        }
 
        private void CheckValueWritePermission(string valueName) {
            if (remoteKey) {
                // unmanaged code trust required for remote access
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand(); 
            }
            else { 
                BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow writing value to read-only key!"); 
                // skip the security check if the key is opened under write mode
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    new RegistryPermission(RegistryPermissionAccess.Write, keyName+"\\"+valueName).Demand();
                }
            }
        } 

        private void CheckValueCreatePermission(string valueName) { 
            if (remoteKey) { 
                // unmanaged code trust required for remote access
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand(); 
            }
            else {
                BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating value under read-only key!");
                // skip the security check if the key is opened under write mode 
                if( checkMode == RegistryKeyPermissionCheck.Default) {
                    new RegistryPermission(RegistryPermissionAccess.Create, keyName+"\\"+valueName).Demand(); 
                } 
            }
        } 

        private void CheckValueReadPermission(string valueName) {
            if( checkMode == RegistryKeyPermissionCheck.Default) {
                // only need to check for default mode (dynamice check) 
                new RegistryPermission(RegistryPermissionAccess.Read, keyName+"\\"+valueName).Demand();
            } 
        } 

        private void CheckKeyReadPermission(){ 
            if( checkMode == RegistryKeyPermissionCheck.Default) {
                // only need to check for default mode (dynamice check)
                new RegistryPermission(RegistryPermissionAccess.Read, keyName + "\\.").Demand();
            } 
        }
 
        private bool ContainsRegistryValue(string name) { 
                int type = 0;
                int datasize = 0; 
                int retval = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
                return retval == 0;
        }
 
        private void EnsureNotDisposed(){
            if (hkey == null) { 
                ThrowHelper.ThrowObjectDisposedException(keyName, ExceptionResource.ObjectDisposed_RegKeyClosed); 
            }
        } 

        private void EnsureWriteable() {
            EnsureNotDisposed();
            if (!IsWritable()) { 
                ThrowHelper.ThrowUnauthorizedAccessException(ExceptionResource.UnauthorizedAccess_RegistryNoWrite);
            } 
        } 

        static int GetRegistryKeyAccess(bool isWritable) { 
            int winAccess;
            if (!isWritable) {
                winAccess = Win32Native.KEY_READ;
            } 
            else {
                winAccess = Win32Native.KEY_READ | Win32Native.KEY_WRITE; 
            } 
            return winAccess;
        } 

        static int GetRegistryKeyAccess(RegistryKeyPermissionCheck mode) {
            int winAccess = 0;
            switch(mode) { 
                case RegistryKeyPermissionCheck.ReadSubTree:
                case RegistryKeyPermissionCheck.Default: 
                    winAccess =  Win32Native.KEY_READ; 
                    break;
 
                case RegistryKeyPermissionCheck.ReadWriteSubTree:
                    winAccess = Win32Native.KEY_READ| Win32Native.KEY_WRITE;
                    break;
 
               default:
                    BCLDebug.Assert(false, "unexpected code path"); 
                    break; 
            }
            return winAccess; 
        }

        private RegistryKeyPermissionCheck GetSubKeyPermissonCheck(bool subkeyWritable) {
            if( checkMode == RegistryKeyPermissionCheck.Default) { 
                return checkMode;
            } 
 
            if(subkeyWritable) {
                return RegistryKeyPermissionCheck.ReadWriteSubTree; 
            }
            else {
                return RegistryKeyPermissionCheck.ReadSubTree;
            } 
        }
 
        static private void ValidateKeyName(string name) { 
            if (name == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name); 
            }

            int nextSlash = name.IndexOf("\\", StringComparison.OrdinalIgnoreCase);
            int current = 0; 
            while (nextSlash != -1) {
                if ((nextSlash - current) > MaxKeyLength) 
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug); 

                current = nextSlash + 1; 
                nextSlash = name.IndexOf("\\", current, StringComparison.OrdinalIgnoreCase);
            }

            if ((name.Length - current) > MaxKeyLength) 
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug);
 
        } 

        static private void ValidateKeyMode(RegistryKeyPermissionCheck mode) { 
            if( mode < RegistryKeyPermissionCheck.Default || mode > RegistryKeyPermissionCheck.ReadWriteSubTree) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryKeyPermissionCheck, ExceptionArgument.mode);
            }
        } 

        static private void ValidateKeyRights(int rights) { 
            if(0 != (rights & ~((int) RegistryRights.FullControl))) { 
                // We need to throw SecurityException here for compatiblity reason,
                // although UnauthorizedAccessException will make more sense. 
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
            }
        }
 	 
        // Win32 constants for error handling
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200; 
        private const int FORMAT_MESSAGE_FROM_SYSTEM    = 0x00001000; 
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
    } 

    [Flags]
    public enum RegistryValueOptions {
        None = 0, 
        DoNotExpandEnvironmentNames = 1
    } 
 
    // the name for this API is meant to mimic FileMode, which has similar values
 
    public enum RegistryKeyPermissionCheck {
        Default = 0,
        ReadSubTree = 1,
        ReadWriteSubTree = 2 
    }
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

/* 
  Note on transaction support: 
  Eventually we will want to add support for NT's transactions to our
  RegistryKey API's (possibly Whidbey M3?).  When we do this, here's 
  the list of API's we need to make transaction-aware:

  RegCreateKeyEx
  RegDeleteKey 
  RegDeleteValue
  RegEnumKeyEx 
  RegEnumValue 
  RegOpenKeyEx
  RegQueryInfoKey 
  RegQueryValueEx
  RegSetValueEx

  We can ignore RegConnectRegistry (remote registry access doesn't yet have 
  transaction support) and RegFlushKey.  RegCloseKey doesn't require any
  additional work.  . 
 */ 

/* 
  Note on ACL support:
  The key thing to note about ACL's is you set them on a kernel object like a
  registry key, then the ACL only gets checked when you construct handles to
  them.  So if you set an ACL to deny read access to yourself, you'll still be 
  able to read with that handle, but not with new handles.
 
  Another peculiarity is a Terminal Server app compatibility hack.  The OS 
  will second guess your attempt to open a handle sometimes.  If a certain
  combination of Terminal Server app compat registry keys are set, then the 
  OS will try to reopen your handle with lesser permissions if you couldn't
  open it in the specified mode.  So on some machines, we will see handles that
  may not be able to read or write to a registry key.  It's very strange.  But
  the real test of these handles is attempting to read or set a value in an 
  affected registry key.
 
  For reference, at least two registry keys must be set to particular values 
  for this behavior:
  HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\RegistryExtensionFlags, the least significant bit must be 1. 
  HKLM\SYSTEM\CurrentControlSet\Control\TerminalServer\TSAppCompat must be 1
  There might possibly be an interaction with yet a third registry key as well.

*/ 

 
namespace Microsoft.Win32 { 

    using System; 
    using System.Collections;
    using System.Security;
#if !FEATURE_PAL
    using System.Security.AccessControl; 
#endif
    using System.Security.Permissions; 
    using System.Text; 
    using System.IO;
    using System.Runtime.Remoting; 
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.Versioning;
    using System.Globalization; 

    /** 
     * Registry hive values.  Useful only for GetRemoteBaseKey 
     */
    [Serializable] 
[System.Runtime.InteropServices.ComVisible(true)]
    public enum RegistryHive
    {
        ClassesRoot = unchecked((int)0x80000000), 
        CurrentUser = unchecked((int)0x80000001),
        LocalMachine = unchecked((int)0x80000002), 
        Users = unchecked((int)0x80000003), 
        PerformanceData = unchecked((int)0x80000004),
        CurrentConfig = unchecked((int)0x80000005), 
        DynData = unchecked((int)0x80000006),
    }

 

    /** 
     * Registry encapsulation. To get an instance of a RegistryKey use the 
     * Registry class's static members then call OpenSubKey.
     * 
     * @see Registry
     * @security(checkDllCalls=off)
     * @security(checkClassLinking=on)
     */ 
    [ComVisible(true)]
    public sealed class RegistryKey : MarshalByRefObject, IDisposable { 
 
        // We could use const here, if C# supported ELEMENT_TYPE_I fully.
        internal static readonly IntPtr HKEY_CLASSES_ROOT         = new IntPtr(unchecked((int)0x80000000)); 
        internal static readonly IntPtr HKEY_CURRENT_USER         = new IntPtr(unchecked((int)0x80000001));
        internal static readonly IntPtr HKEY_LOCAL_MACHINE        = new IntPtr(unchecked((int)0x80000002));
        internal static readonly IntPtr HKEY_USERS                = new IntPtr(unchecked((int)0x80000003));
        internal static readonly IntPtr HKEY_PERFORMANCE_DATA     = new IntPtr(unchecked((int)0x80000004)); 
        internal static readonly IntPtr HKEY_CURRENT_CONFIG       = new IntPtr(unchecked((int)0x80000005));
        internal static readonly IntPtr HKEY_DYN_DATA             = new IntPtr(unchecked((int)0x80000006)); 
 
        // Dirty indicates that we have munged data that should be potentially
        // written to disk. 
        //
        private const int STATE_DIRTY        = 0x0001;

        // SystemKey indicates that this is a "SYSTEMKEY" and shouldn't be "opened" 
        // or "closed".
        // 
        private const int STATE_SYSTEMKEY    = 0x0002; 

        // Access 
        //
        private const int STATE_WRITEACCESS  = 0x0004;

        // Indicates if this key is for HKEY_PERFORMANCE_DATA 
        private const int STATE_PERF_DATA    = 0x0008;
 
        // Names of keys.  This array must be in the same order as the HKEY values listed above. 
        //
        private static readonly String[] hkeyNames = new String[] { 
                "HKEY_CLASSES_ROOT",
                "HKEY_CURRENT_USER",
                "HKEY_LOCAL_MACHINE",
                "HKEY_USERS", 
                "HKEY_PERFORMANCE_DATA",
                "HKEY_CURRENT_CONFIG", 
                "HKEY_DYN_DATA" 
                };
 
        // Maximum key name length is 255 on win9X.
        private const int MaxKeyLength = 255;

        private SafeRegistryHandle hkey = null; 
        private int state = 0;
        private String keyName; 
        private bool remoteKey = false; 
        private RegistryKeyPermissionCheck checkMode;
        private static readonly int _SystemDefaultCharSize = 3 - Win32Native.lstrlen(new sbyte [] {0x41, 0x41, 0, 0}); 



        /** 
         * Creates a RegistryKey.
         * 
         * This key is bound to hkey, if writable is <b>false</b> then no write operations 
         * will be allowed.
         */ 
        private RegistryKey(SafeRegistryHandle  hkey, bool writable)
            : this(hkey, writable, false, false, false) {
        }
 

        /** 
         * Creates a RegistryKey. 
         *
         * This key is bound to hkey, if writable is <b>false</b> then no write operations 
         * will be allowed. If systemkey is set then the hkey won't be released
         * when the object is GC'ed.
         * The remoteKey flag when set to true indicates that we are dealing with registry entries
         * on a remote machine and requires the program making these calls to have full trust. 
         */
        private RegistryKey(SafeRegistryHandle hkey, bool writable, bool systemkey, bool remoteKey, bool isPerfData) { 
            this.hkey = hkey; 
            this.keyName = "";
            this.remoteKey = remoteKey; 
            if (systemkey) {
                this.state |= STATE_SYSTEMKEY;
            }
            if (writable) { 
                this.state |= STATE_WRITEACCESS;
            } 
            if (isPerfData) 
                this.state |= STATE_PERF_DATA;
        } 

        /**
         * Closes this key, flushes it to disk if the contents have been modified.
         */ 
        public void Close() {
            Dispose(true); 
        } 

        private void Dispose(bool disposing) { 
            if (hkey != null) {
                bool isPerf = IsPerfDataKey();
                // System keys should never be closed.  However, we want to call RegCloseKey
                // on HKEY_PERFORMANCE_DATA without actually invalidating the RegistryKey. 
                if (!IsSystemKey() || isPerf ) {
                    try { 
                        hkey.Dispose(); 
                    }
                    catch (IOException){ 
                        // we don't really care if the handle is invalid at this point
                    }

                    if (isPerf) 
                        hkey = new SafeRegistryHandle(RegistryKey.HKEY_PERFORMANCE_DATA, !IsWin9x());
                    else 
                        hkey = null; 
                }
            } 
        }

        public void Flush() {
            if (hkey != null) { 
                 if (IsDirty()) {
                     Win32Native.RegFlushKey(hkey); 
                } 
            }
        } 

        /// <internalonly/>
        void IDisposable.Dispose() {
            Dispose(true); 
        }
 
        /** 
         * Creates a new subkey, or opens an existing one.
         * 
         * @param subkey Name or path to subkey to create or open.
         *
         * @return the subkey, or <b>null</b> if the operation failed.
         */ 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public RegistryKey CreateSubKey(String subkey) { 
            return CreateSubKey(subkey, checkMode);
        } 

        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public RegistryKey CreateSubKey(String subkey, RegistryKeyPermissionCheck permissionCheck) {
#if !FEATURE_PAL 
            return CreateSubKey(subkey, permissionCheck, null); 
        }
 
        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public unsafe RegistryKey CreateSubKey(String subkey, RegistryKeyPermissionCheck permissionCheck,  RegistrySecurity registrySecurity) { 
#endif
            ValidateKeyName(subkey); 
            ValidateKeyMode(permissionCheck); 
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash 

            // only keys opened under read mode is not writable
            if (!remoteKey) {
                RegistryKey key = InternalOpenSubKey(subkey, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree)); 
                if (key != null)  { // Key already exits
                    CheckSubKeyWritePermission(subkey); 
                    CheckSubTreePermission(subkey, permissionCheck); 
                    key.checkMode = permissionCheck;
                    return key; 
                }
            }

            CheckSubKeyCreatePermission(subkey); 

            Win32Native.SECURITY_ATTRIBUTES secAttrs = null; 
#if !FEATURE_PAL 
            // For ACL's, get the security descriptor from the RegistrySecurity.
            if (registrySecurity != null) { 
                secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);

                byte[] sd = registrySecurity.GetSecurityDescriptorBinaryForm(); 
                // We allocate memory on the stack to improve the speed.
                // So this part of code can't be refactored into a method. 
                byte* pSecDescriptor = stackalloc byte[sd.Length]; 
                Buffer.memcpy(sd, 0, pSecDescriptor, 0, sd.Length);
                secAttrs.pSecurityDescriptor = pSecDescriptor; 
            }
#endif
            int disposition = 0;
 
            // By default, the new key will be writable.
            SafeRegistryHandle result = null; 
            int ret = Win32Native.RegCreateKeyEx(hkey, 
                subkey,
                0, 
                null,
                0,
                GetRegistryKeyAccess(permissionCheck != RegistryKeyPermissionCheck.ReadSubTree),
                secAttrs, 
                out result,
                out disposition); 
 
            if (ret == 0 && !result.IsInvalid) {
                RegistryKey key = new RegistryKey(result, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree), false, remoteKey, false); 
                CheckSubTreePermission(subkey, permissionCheck);
                key.checkMode = permissionCheck;

                if (subkey.Length == 0) 
                    key.keyName = keyName;
                else 
                    key.keyName = keyName + "\\" + subkey; 
                return key;
            } 
            else if (ret != 0) // syscall failed, ret is an error code.
                Win32Error(ret, keyName + "\\" + subkey);  // Access denied?

            BCLDebug.Assert(false, "Unexpected code path in RegistryKey::CreateSubKey"); 
            return null;
        } 
 
        /**
         * Deletes the specified subkey. Will throw an exception if the subkey has 
         * subkeys. To delete a tree of subkeys use, DeleteSubKeyTree.
         *
         * @param subkey SubKey to delete.
         * 
         * @exception InvalidOperationException thrown if the subkey has child subkeys.
         */ 
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public void DeleteSubKey(String subkey) { 
            DeleteSubKey(subkey, true);
        }

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public void DeleteSubKey(String subkey, bool throwOnMissingSubKey) { 
            ValidateKeyName(subkey); 
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash 
            CheckSubKeyWritePermission(subkey);

            // Open the key we are deleting and check for children. Be sure to
            // explicitly call close to avoid keeping an extra HKEY open. 
            //
            RegistryKey key = InternalOpenSubKey(subkey,false); 
            if (key != null) { 
                try {
                    if (key.InternalSubKeyCount() > 0) { 
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_RegRemoveSubKey);
                    }
                }
                finally { 
                    key.Close();
                } 
 
                int ret = Win32Native.RegDeleteKey(hkey, subkey);
                if (ret!=0) { 
                    if (ret == Win32Native.ERROR_FILE_NOT_FOUND) {
                        if (throwOnMissingSubKey)
                            ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
                    } 
                    else
                        Win32Error(ret, null); 
                } 
            }
            else { // there is no key which also means there is no subkey 
                if (throwOnMissingSubKey)
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        } 

        /** 
         * Recursively deletes a subkey and any child subkeys. 
         *
         * @param subkey SubKey to delete. 
         */
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public void DeleteSubKeyTree(String subkey) { 
            ValidateKeyName(subkey);
 
            // Security concern: Deleting a hive's "" subkey would delete all 
            // of that hive's contents.  Don't allow "".
            if (subkey.Length==0 && IsSystemKey()) { 
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyDelHive);
            }

            EnsureWriteable(); 

            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash 
            CheckSubTreeWritePermission(subkey); 

            RegistryKey key = InternalOpenSubKey(subkey, true); 
            if (key != null) {
                try {
                    if (key.InternalSubKeyCount() > 0) {
                        String[] keys = key.InternalGetSubKeyNames(); 

                        for (int i=0; i<keys.Length; i++) { 
                            key.DeleteSubKeyTreeInternal(keys[i]); 
                        }
                    } 
                }
                finally {
                    key.Close();
                } 

                int ret = Win32Native.RegDeleteKey(hkey, subkey); 
                if (ret!=0) Win32Error(ret, null); 
            }
            else { 
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        }
 
        // An internal version which does no security checks or argument checking.  Skipping the
        // security checks should give us a slight perf gain on large trees. 
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        private void DeleteSubKeyTreeInternal(string subkey) { 
            RegistryKey key = InternalOpenSubKey(subkey, true);
            if (key != null) {
                try {
                    if (key.InternalSubKeyCount() > 0) { 
                        String[] keys = key.InternalGetSubKeyNames();
 
                        for (int i=0; i<keys.Length; i++) { 
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        } 
                    }
                }
                finally {
                    key.Close(); 
                }
 
                int ret = Win32Native.RegDeleteKey(hkey, subkey); 
                if (ret!=0) Win32Error(ret, null);
            } 
            else {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        } 

        /** 
         * Deletes the specified value from this key. 
         *
         * @param name Name of value to delete. 
         */
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public void DeleteValue(String name) { 
            DeleteValue(name, true);
        } 
 
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)] 
        public void DeleteValue(String name, bool throwOnMissingValue) {
            if (name==null) {
               ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name);
            } 

            EnsureWriteable(); 
            CheckValueWritePermission(name); 
            int errorCode = Win32Native.RegDeleteValue(hkey, name);
 
            //
            // From windows 2003 server, if the name is too long we will get error code ERROR_FILENAME_EXCED_RANGE
            // This still means the name doesn't exist. We need to be consistent with previous OS.
            // 
            if (errorCode == Win32Native.ERROR_FILE_NOT_FOUND || errorCode == Win32Native.ERROR_FILENAME_EXCED_RANGE) {
                if (throwOnMissingValue) { 
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyValueAbsent); 
                }
                // Otherwise, just return giving no indication to the user. 
                // (For compatibility)
            }
            // We really should throw an exception here if errorCode was bad,
            // but we can't for compatibility reasons. 
            BCLDebug.Correctness(errorCode == 0, "RegDeleteValue failed.  Here's your error code: "+errorCode);
        } 
 
        /**
         * Retrieves a new RegistryKey that represents the requested key. Valid 
         * values are:
         *
         * HKEY_CLASSES_ROOT,
         * HKEY_CURRENT_USER, 
         * HKEY_LOCAL_MACHINE,
         * HKEY_USERS, 
         * HKEY_PERFORMANCE_DATA, 
         * HKEY_CURRENT_CONFIG,
         * HKEY_DYN_DATA. 
         *
         * @param hKey HKEY_* to open.
         *
         * @return the RegistryKey requested. 
         */
        internal static RegistryKey GetBaseKey(IntPtr hKey) { 
            int index = ((int)hKey) & 0x0FFFFFFF; 
            BCLDebug.Assert(index >= 0  && index < hkeyNames.Length, "index is out of range!");
            BCLDebug.Assert((((int)hKey) & 0xFFFFFFF0) == 0x80000000, "Invalid hkey value!"); 

            bool isPerf = hKey == HKEY_PERFORMANCE_DATA;
            // only mark the SafeHandle as ownsHandle if the key is HKEY_PERFORMANCE_DATA and we're not on win9x.
            SafeRegistryHandle srh = new SafeRegistryHandle(hKey, isPerf && !IsWin9x()); 

            RegistryKey key = new RegistryKey(srh, true, true,false, isPerf); 
            key.checkMode = RegistryKeyPermissionCheck.Default; 
            key.keyName = hkeyNames[index];
            return key; 
        }

        /**
         * Retrieves a new RegistryKey that represents the requested key on a foreign 
         * machine.  Valid values for hKey are members of the RegistryHive enum, or
         * Win32 integers such as: 
         * 
         * HKEY_CLASSES_ROOT,
         * HKEY_CURRENT_USER, 
         * HKEY_LOCAL_MACHINE,
         * HKEY_USERS,
         * HKEY_PERFORMANCE_DATA,
         * HKEY_CURRENT_CONFIG, 
         * HKEY_DYN_DATA.
         * 
         * @param hKey HKEY_* to open. 
         * @param machineName the machine to connect to
         * 
         * @return the RegistryKey requested.
         */
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, String machineName) {
            if (machineName==null) 
                throw new ArgumentNullException("machineName"); 
            int index = (int)hKey & 0x0FFFFFFF;
            if (index < 0 || index >= hkeyNames.Length || ((int)hKey & 0xFFFFFFF0) != 0x80000000) { 
                throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyOutOfRange"));
            }

            CheckUnmanagedCodePermission(); 
            // connect to the specified remote registry
            SafeRegistryHandle foreignHKey = null; 
            int ret = Win32Native.RegConnectRegistry(machineName, new SafeRegistryHandle(new IntPtr((int)hKey), false), out foreignHKey); 

            if (ret == Win32Native.ERROR_DLL_INIT_FAILED) 
                // return value indicates an error occurred
                throw new ArgumentException(Environment.GetResourceString("Arg_DllInitFailure"));

            if (ret != 0) 
                Win32ErrorStatic(ret, null);
 
            if (foreignHKey.IsInvalid) 
                // return value indicates an error occurred
                throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyNoRemoteConnect", machineName)); 

            RegistryKey key = new RegistryKey(foreignHKey, true, false, true, ((IntPtr) hKey) == HKEY_PERFORMANCE_DATA);
            key.checkMode = RegistryKeyPermissionCheck.Default;
            key.keyName = hkeyNames[index]; 
            return key;
        } 
 
        /**
         * Retrieves a subkey. If readonly is <b>true</b>, then the subkey is opened with 
         * read-only access.
         *
         * @param name Name or path of subkey to open.
         * @param readonly Set to <b>true</b> if you only need readonly access. 
         *
         * @return the Subkey requested, or <b>null</b> if the operation failed. 
         */ 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        public RegistryKey OpenSubKey(string name, bool writable ) {
            ValidateKeyName(name);
            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash 

            CheckOpenSubKeyPermission(name, writable); 
            SafeRegistryHandle result = null; 
            int ret = Win32Native.RegOpenKeyEx(hkey, name, 0, GetRegistryKeyAccess(writable), out result);
 
            if (ret == 0 && !result.IsInvalid) {
                RegistryKey key = new RegistryKey(result, writable, false, remoteKey, false);
                key.checkMode = GetSubKeyPermissonCheck(writable);
                key.keyName = keyName + "\\" + name; 
                return key;
            } 
 
            // Return null if we didn't find the key.
            if (ret == Win32Native.ERROR_ACCESS_DENIED || ret == Win32Native.ERROR_BAD_IMPERSONATION_LEVEL) { 
                // We need to throw SecurityException here for compatibility reasons,
                // although UnauthorizedAccessException will make more sense.
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
            } 

            return null; 
        } 

        [ComVisible(false)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public RegistryKey OpenSubKey(String name, RegistryKeyPermissionCheck permissionCheck) {
            ValidateKeyMode(permissionCheck); 
            return InternalOpenSubKey(name, permissionCheck, GetRegistryKeyAccess(permissionCheck));
        } 
 
        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)]
        public RegistryKey OpenSubKey(String name, RegistryKeyPermissionCheck permissionCheck, RegistryRights rights) {
            return InternalOpenSubKey(name, permissionCheck, (int)rights);
        } 

        [ResourceExposure(ResourceScope.Machine)] 
        [ResourceConsumption(ResourceScope.Machine)] 
        private RegistryKey InternalOpenSubKey(String name, RegistryKeyPermissionCheck permissionCheck, int rights) {
            ValidateKeyName(name); 
            ValidateKeyMode(permissionCheck);
            ValidateKeyRights(rights);
            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash 

            CheckOpenSubKeyPermission(name, permissionCheck); 
            SafeRegistryHandle result = null; 
            int ret = Win32Native.RegOpenKeyEx(hkey, name, 0, rights, out result);
 
            if (ret == 0 && !result.IsInvalid) {
                RegistryKey key = new RegistryKey(result, (permissionCheck == RegistryKeyPermissionCheck.ReadWriteSubTree), false, remoteKey, false);
                key.keyName = keyName + "\\" + name;
                key.checkMode = permissionCheck; 
                return key;
            } 
 
            // Return null if we didn't find the key.
            if (ret == Win32Native.ERROR_ACCESS_DENIED || ret == Win32Native.ERROR_BAD_IMPERSONATION_LEVEL) { 
                // We need to throw SecurityException here for compatiblity reason,
                // although UnauthorizedAccessException will make more sense.
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
            } 

            return null; 
        } 

 
        // This required no security checks. This is to get around the Deleting SubKeys which only require
        // write permission. They call OpenSubKey which required read. Now instead call this function w/o security checks
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        internal RegistryKey InternalOpenSubKey(String name, bool writable) {
            ValidateKeyName(name); 
            EnsureNotDisposed(); 

            int winAccess = GetRegistryKeyAccess(writable); 
            SafeRegistryHandle result = null;
            int ret = Win32Native.RegOpenKeyEx(hkey, name, 0, winAccess, out result);

            if (ret == 0 && !result.IsInvalid) { 
                RegistryKey key = new RegistryKey(result, writable, false, remoteKey, false);
                key.keyName = keyName + "\\" + name; 
                return key; 
            }
            return null; 
        }

        /**
         * Returns a subkey with read only permissions. 
         *
         * @param name Name or path of subkey to open. 
         * 
         * @return the Subkey requested, or <b>null</b> if the operation failed.
         */ 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public RegistryKey OpenSubKey(String name) {
            return OpenSubKey(name, false); 
        }
 
        /** 
         * Retrieves the count of subkeys.
         * 
         * @return a count of subkeys.
         */
        public int SubKeyCount {
            get { 
                CheckKeyReadPermission();
                return InternalSubKeyCount(); 
            } 
        }
 
        internal int InternalSubKeyCount() {
                EnsureNotDisposed();

                int subkeys = 0; 
                int junk = 0;
                int ret = Win32Native.RegQueryInfoKey(hkey, 
                                          null, 
                                          null,
                                          Win32Native.NULL, 
                                          ref subkeys,  // subkeys
                                          null,
                                          null,
                                          ref junk,     // values 
                                          null,
                                          null, 
                                          null, 
                                          null);
 
                if (ret != 0)
                    Win32Error(ret, null);
                return subkeys;
        } 

        /** 
         * Retrieves an array of strings containing all the subkey names. 
         *
         * @return all subkey names. 
         */
        public String[] GetSubKeyNames() {
            CheckKeyReadPermission();
            return InternalGetSubKeyNames(); 
        }
 
        internal String[] InternalGetSubKeyNames() { 
            EnsureNotDisposed();
            int subkeys = InternalSubKeyCount(); 
            String[] names = new String[subkeys];  // Returns 0-length array if empty.

            if (subkeys > 0) {
                StringBuilder name = new StringBuilder(256); 
                int namelen;
 
                for (int i=0; i<subkeys; i++) { 
                    namelen = name.Capacity; // Don't remove this. The API's doesn't work if this is not properly initialised.
                    int ret = Win32Native.RegEnumKeyEx(hkey, 
                        i,
                        name,
                        out namelen,
                        null, 
                        null,
                        null, 
                        null); 
                    if (ret != 0)
                        Win32Error(ret, null); 
                    names[i] = name.ToString();
                }
            }
 
            return names;
        } 
 
        /**
         * Retrieves the count of values. 
         *
         * @return a count of values.
         */
        public int ValueCount { 
            get {
                CheckKeyReadPermission(); 
                return InternalValueCount(); 
            }
        } 

        internal int InternalValueCount() {
            EnsureNotDisposed();
            int values = 0; 
            int junk = 0;
            int ret = Win32Native.RegQueryInfoKey(hkey, 
                                      null, 
                                      null,
                                      Win32Native.NULL, 
                                      ref junk,     // subkeys
                                      null,
                                      null,
                                      ref values,   // values 
                                      null,
                                      null, 
                                      null, 
                                      null);
            if (ret != 0) 
               Win32Error(ret, null);
            return values;
        }
 
        /**
         * Retrieves an array of strings containing all the value names. 
         * 
         * @return all value names.
         */ 
        public String[] GetValueNames() {
            CheckKeyReadPermission();
            EnsureNotDisposed();
 
            int values = InternalValueCount();
            String[] names = new String[values]; 
 
            if (values > 0) {
                StringBuilder name = new StringBuilder(256); 
                int namelen;

                for (int i=0; i<values; i++) {
                    namelen = name.Capacity; 

                    int ret = Win32Native.RegEnumValue(hkey, 
                        i, 
                        name,
                        ref namelen, 
                        Win32Native.NULL,
                        null,
                        null,
                        null); 

                    // Remote Win9x machines always return ERROR_MORE_DATA if you don't pass in 
                    // a buffer.  In addition, they won't fill in the name properly unless certain 
                    // other conditions are met.  We can't know what the OS of the remote
                    // machine is, so we'll do this for all remote machines that return 
                    // ERROR_MORE_DATA for a key other than HKEY_PERFORMANCE_DATA.
                    if (ret == Win32Native.ERROR_MORE_DATA && !IsPerfDataKey() && remoteKey){
                        int [] datalen = new int[1];
                        byte [] data = new byte[5]; 
                        datalen[0] = 5;
                        ret = Win32Native.RegEnumValueA(hkey, 
                                                        i, 
                                                        name,
                                                        ref namelen, 
                                                        Win32Native.NULL,
                                                        null,
                                                        data,
                                                        datalen); 
                            if (ret == Win32Native.ERROR_MORE_DATA){
                                datalen[0] = 0; 
                                ret = Win32Native.RegEnumValueA(hkey, 
                                                                 i,
                                                                 name, 
                                                                 ref namelen,
                                                                 Win32Native.NULL,
                                                                 null,
                                                                 null, 
                                                                 datalen);
                        } 
                    } 

                    if (ret != 0) { 
                        // ignore ERROR_MORE_DATA if we're querying HKEY_PERFORMANCE_DATA
                        if (!(IsPerfDataKey() && ret == Win32Native.ERROR_MORE_DATA))
                            Win32Error(ret, null);
                    } 
                    names[i] = name.ToString();
                } 
            } 

            return names; 
        }

        /**
         * Retrieves the specified value. <b>null</b> is returned if the value 
         * doesn't exist.
         * 
         * <p>Note that <var>name</var> can be null or "", at which point the 
         * unnamed or default value of this Registry key is returned, if any.
         * 
         * @param name Name of value to retrieve.
         *
         * @return the data associated with the value.
         */ 
        public Object GetValue(String name) {
            CheckValueReadPermission(name); 
            return InternalGetValue(name, null, false, true); 
        }
 
        /**
         * Retrieves the specified value. <i>defaultValue</i> is returned if the value doesn't exist.
         *
         * <p>Note that <var>name</var> can be null or "", at which point the 
         * unnamed or default value of this Registry key is returned, if any.
         * The default values for RegistryKeys are OS-dependent.  NT doesn't 
         * have them by default, but they can exist and be of any type.  On 
         * Win95, the default value is always an empty key of type REG_SZ.
         * Win98 supports default values of any type, but defaults to REG_SZ. 
         *
         * @param name Name of value to retrieve.
         * @param defaultValue Value to return if <i>name</i> doesn't exist.
         * 
         * @return the data associated with the value.
         */ 
        public Object GetValue(String name, Object defaultValue) { 
            CheckValueReadPermission(name);
            return InternalGetValue(name, defaultValue, false, true); 
        }

        [ComVisible(false)]
        public Object GetValue(String name, Object defaultValue, RegistryValueOptions options) { 
            if( options < RegistryValueOptions.None || options > RegistryValueOptions.DoNotExpandEnvironmentNames) {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)options), "options"); 
            } 
            bool doNotExpand = (options == RegistryValueOptions.DoNotExpandEnvironmentNames);
            CheckValueReadPermission(name); 
            return InternalGetValue(name, defaultValue, doNotExpand, true);
        }

        internal Object InternalGetValue(String name, Object defaultValue, bool doNotExpand, bool checkSecurity) { 
            if (checkSecurity) {
                // Name can be null!  It's the most common use of RegQueryValueEx 
                EnsureNotDisposed(); 
            }
 
            Object data = defaultValue;
            int type = 0;
            int datasize = 0;
 
            int ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
 
            if (ret != 0) { 
                if (IsPerfDataKey()) {
                    int size = 65000; 
                    int sizeInput = size;

                    int r;
                    byte[] blob = new byte[size]; 
                    while (Win32Native.ERROR_MORE_DATA == (r = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref sizeInput))) {
                        size *= 2; 
                        sizeInput = size; 
                        blob = new byte[size];
                    } 
                    if (r != 0)
                        Win32Error(r, name);
                    return blob;
                } 
                else {
                    // Win9x will return ERROR_MORE_DATA even in success cases, so we want to continue on through 
                    // the function. 
                    if (ret != Win32Native.ERROR_MORE_DATA)
                        return data; 
                }
            }

            switch (type) { 
            case Win32Native.REG_DWORD_BIG_ENDIAN:
            case Win32Native.REG_BINARY: { 
                                 byte[] blob = new byte[datasize]; 
                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                                 data = blob; 
                             }
                             break;
            case Win32Native.REG_QWORD:
                             {    // also REG_QWORD_LITTLE_ENDIAN 
                                 long blob = 0;
                                 BCLDebug.Assert(datasize==8, "datasize==8"); 
                                 // Here, datasize must be 8 when calling this 
                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize);
 
                                 data = blob;
                             }
                             break;
            case Win32Native.REG_DWORD: 
                             {    // also REG_DWORD_LITTLE_ENDIAN
                                 int blob = 0; 
                                 BCLDebug.Assert(datasize==4, "datasize==4"); 
                                 // Here, datasize must be four when calling this
                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize); 

                                 data = blob;
                             }
                             break; 

            case Win32Native.REG_SZ: 
                             { 
                                 if (_SystemDefaultCharSize != 1) {
                                     BCLDebug.Assert(_SystemDefaultCharSize==2, "_SystemDefaultCharSize==2"); 
                                     StringBuilder blob = new StringBuilder(datasize/2);
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                                     data = blob.ToString();
                                 } 
                                 else {
                                     byte[] blob = new byte[datasize]; 
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize); 
                                     data = Encoding.Default.GetString(blob, 0, blob.Length-1);
                                 } 
                             }
                             break;

            case Win32Native.REG_EXPAND_SZ: 
                              {
                                 if (_SystemDefaultCharSize != 1) { 
                                     StringBuilder blob = new StringBuilder(datasize/2); 
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                                     if (doNotExpand) 
                                        data = blob.ToString();
                                     else
                                        data = Environment.ExpandEnvironmentVariables(blob.ToString());
                                 } 
                                 else {
                                     byte[] blob = new byte[datasize]; 
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize); 
                                     String unexpandedData = Encoding.Default.GetString(blob, 0, blob.Length-1);
                                     if (doNotExpand) 
                                        data = unexpandedData;
                                     else
                                        data = Environment.ExpandEnvironmentVariables(unexpandedData);
                                 } 
                             }
                             break; 
            case Win32Native.REG_MULTI_SZ: 
                             {
                                 bool unicode = (_SystemDefaultCharSize != 1); 

                                 IList strings = new ArrayList();

                                 if (unicode) { 
                                     char[] blob = new char[datasize/2];
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize); 
 
                                     int cur = 0;
                                     int len = blob.Length; 

                                     while (ret == 0 && cur < len) {

                                         int nextNull = cur; 
                                         while (nextNull < len && blob[nextNull] != (char)0) {
                                             nextNull++; 
                                         } 

                                         if (nextNull < len) { 
                                             BCLDebug.Assert(blob[nextNull] == (char)0, "blob[nextNull] should be 0");
                                             if (nextNull-cur > 0) {
                                                 strings.Add(new String(blob, cur, nextNull-cur));
                                             } 
                                             else {
                                                // we found an empty string.  But if we're at the end of the data, 
                                                // it's just the extra null terminator. 
                                                if (nextNull != len-1)
                                                    strings.Add(String.Empty); 
                                             }
                                         }
                                         else {
                                             strings.Add(new String(blob, cur, len-cur)); 
                                         }
                                         cur = nextNull+1; 
                                     } 

                                 } 
                                 else {
                                     byte[] blob = new byte[datasize];
                                     ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
 
                                     int cur = 0;
                                     int len = blob.Length; 
 
                                     while (ret == 0 && cur < len) {
 
                                         int nextNull = cur;
                                         while (nextNull < len && blob[nextNull] != (byte)0) {
                                             nextNull++;
                                         } 

                                         if (nextNull < len) { 
                                             BCLDebug.Assert(blob[nextNull] == (byte)0, "blob[nextNull] should be 0"); 
                                             if (nextNull-cur > 0) {
                                                strings.Add(Encoding.Default.GetString(blob, cur, nextNull-cur)); 
                                             }
                                             else {
                                                // we found an empty string.  But if we're at the end of the data,
                                                // it's just the extra null terminator. 
                                                if (nextNull != len-1)
                                                    strings.Add(String.Empty); 
                                             } 
                                         }
                                         else { 
                                            strings.Add(Encoding.Default.GetString(blob, cur, len-cur));
                                         }
                                         cur = nextNull + 1;
                                     } 
                                 }
 
                                 data = new String[strings.Count]; 
                                 strings.CopyTo((Array)data, 0);
                                 //data = strings.GetAllItems(String.class); 
                             }
                             break;
            case Win32Native.REG_NONE:
            case Win32Native.REG_LINK: 
            default:
                break; 
            } 

            return data; 
        }


        [ComVisible(false)] 
        public RegistryValueKind GetValueKind(string name) {
            CheckValueReadPermission(name); 
            EnsureNotDisposed(); 

            int type = 0; 
            int datasize = 0;
            int ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
            if (ret != 0)
                Win32Error(ret, null); 

            if (!Enum.IsDefined(typeof(RegistryValueKind), type)) 
                return RegistryValueKind.Unknown; 
            else
                return (RegistryValueKind) type; 
        }

        /**
         * Retrieves the current state of the dirty property. 
         *
         * A key is marked as dirty if any operation has occured that modifies the 
         * contents of the key. 
         *
         * @return <b>true</b> if the key has been modified. 
         */
        private bool IsDirty() {
            return (this.state & STATE_DIRTY) != 0;
        } 

        private bool IsSystemKey() { 
            return (this.state & STATE_SYSTEMKEY) != 0; 
        }
 
        private bool IsWritable() {
            return (this.state & STATE_WRITEACCESS) != 0;
        }
 
        private bool IsPerfDataKey() {
            return (this.state & STATE_PERF_DATA) != 0; 
        } 

        private static bool IsWin9x() { 
            return ((Environment.OSInfo & Environment.OSName.Win9x) != 0);
        }

        public String Name { 
            get {
                EnsureNotDisposed(); 
                return keyName; 
            }
        } 

        private void SetDirty() {
            this.state |= STATE_DIRTY;
        } 

        /** 
         * Sets the specified value. 
         *
         * @param name Name of value to store data in. 
         * @param value Data to store.
         */
        public void SetValue(String name, Object value) {
            SetValue(name, value, RegistryValueKind.Unknown); 
        }
 
        [ComVisible(false)] 
        public unsafe void SetValue(String name, Object value, RegistryValueKind valueKind) {
            if (value==null) 
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);

            if (name != null && name.Length > MaxKeyLength) {
                throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyStrLenBug")); 
            }
 
            if (!Enum.IsDefined(typeof(RegistryValueKind), valueKind)) 
                throw new ArgumentException(Environment.GetResourceString("Arg_RegBadKeyKind"), "valueKind");
 
            EnsureWriteable();

            if (!remoteKey && ContainsRegistryValue(name)) { // Existing key
                CheckValueWritePermission(name); 
            }
             else { // Creating a new value 
                CheckValueCreatePermission(name); 
            }
 
            if (valueKind == RegistryValueKind.Unknown) {
                // this is to maintain compatibility with the old way of autodetecting the type.
                // SetValue(string, object) will come through this codepath.
                valueKind = CalculateValueKind(value); 
            }
 
            int ret = 0; 
            try {
                switch (valueKind) { 
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.String:
                        {
                            String data = value.ToString(); 
                            if (_SystemDefaultCharSize == 1) {
                                byte[] blob = Encoding.Default.GetBytes(data); 
                                byte[] rawdata = new byte[blob.Length+1]; 
                                Array.Copy(blob, 0, rawdata, 0, blob.Length);
 
                                ret = Win32Native.RegSetValueEx(hkey,
                                    name,
                                    0,
                                    valueKind, 
                                    rawdata,
                                    rawdata.Length); 
                            } 
                            else {
                                ret = Win32Native.RegSetValueEx(hkey, 
                                    name,
                                    0,
                                    valueKind,
                                    data, 
                                    data.Length * 2 + 2);
                            } 
                            break; 
                        }
 
                    case RegistryValueKind.MultiString:
                        {
                            // Other thread might modify the input array after we calculate the buffer length.
                            // Make a copy of the input array to be safe. 
                            string[] dataStrings = (string[])(((string[])value).Clone());
                            bool unicode = (_SystemDefaultCharSize != 1); 
 
                            int sizeInBytes = 0;
 
                            // First determine the size of the array
                            //
                            if (unicode) {
                                for (int i=0; i<dataStrings.Length; i++) { 
                                    if (dataStrings[i] == null) {
                                        ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetStrArrNull); 
                                    } 
                                    sizeInBytes += (dataStrings[i].Length+1) * 2;
                                } 
                                sizeInBytes += 2;
                            }
                            else {
                                for (int i=0; i<dataStrings.Length; i++) { 
                                    if (dataStrings[i] == null) {
                                        ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetStrArrNull); 
                                    } 
                                    sizeInBytes += (Encoding.Default.GetByteCount(dataStrings[i]) + 1);
                                } 
                                sizeInBytes ++;
                            }

                            byte[] basePtr = new byte[sizeInBytes]; 
                            fixed(byte* b = basePtr) {
                                IntPtr currentPtr = new IntPtr( (void *) b); 
 
                                // Write out the strings...
                                // 
                                for (int i=0; i<dataStrings.Length; i++) {
                                     if (unicode) { // Assumes that the Strings are always null terminated.
                                        String.InternalCopy(dataStrings[i],currentPtr,(dataStrings[i].Length*2));
                                        currentPtr = new IntPtr((long)currentPtr + (dataStrings[i].Length*2)); 
                                        *(char*)(currentPtr.ToPointer()) = '\0';
                                        currentPtr = new IntPtr((long)currentPtr + 2); 
                                    } 
                                    else {
                                        byte[] data = Encoding.Default.GetBytes(dataStrings[i]); 
                                        Buffer.memcpy(data, 0, (byte*)currentPtr.ToPointer(), 0, data.Length) ;
                                        currentPtr = new IntPtr((long)currentPtr + data.Length);
                                        *(byte*)(currentPtr.ToPointer()) = 0;
                                        currentPtr = new IntPtr((long)currentPtr + 1 ); 
                                    }
                                } 
 
                                if (unicode) {
                                    *(char*)(currentPtr.ToPointer()) = '\0'; 
                                    currentPtr = new IntPtr((long)currentPtr + 2);
                                }
                                else {
                                    *(byte*)(currentPtr.ToPointer()) = 0; 
                                    currentPtr = new IntPtr((long)currentPtr + 1);
                                } 
 
                                ret = Win32Native.RegSetValueEx(hkey,
                                    name, 
                                    0,
                                    RegistryValueKind.MultiString,
                                    basePtr,
                                    sizeInBytes); 
                            }
                            break; 
                        } 

                    case RegistryValueKind.Binary: 
                        byte[] dataBytes = (byte[]) value;
                        ret = Win32Native.RegSetValueEx(hkey,
                            name,
                            0, 
                            RegistryValueKind.Binary,
                            dataBytes, 
                            dataBytes.Length); 
                        break;
 
                    case RegistryValueKind.DWord:
                        {
                            // We need to use Convert here because we could have a boxed type cannot be
                            // unboxed and cast at the same time.  I.e. ((int)(object)(short) 5) will fail. 
                            int data = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
 
                            ret = Win32Native.RegSetValueEx(hkey, 
                                name,
                                0, 
                                RegistryValueKind.DWord,
                                ref data,
                                4);
                            break; 
                        }
 
                    case RegistryValueKind.QWord: 
                        {
                            long data = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture); 

                            ret = Win32Native.RegSetValueEx(hkey,
                                name,
                                0, 
                                RegistryValueKind.QWord,
                                ref data, 
                                8); 
                            break;
                        } 
                }
            }
            catch (OverflowException) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind); 
            }
            catch (InvalidOperationException) { 
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind); 
            }
            catch (FormatException) { 
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }
            catch (InvalidCastException) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind); 
            }
 
            if (ret == 0) { 
                SetDirty();
            } 
            else
                Win32Error(ret, null);

        } 

        private RegistryValueKind CalculateValueKind(Object value) { 
            // This logic matches what used to be in SetValue(string name, object value) in the v1.0 and v1.1 days. 
            // Even though we could add detection for an int64 in here, we want to maintain compatibility with the
            // old behavior. 
            if (value is Int32)
                return RegistryValueKind.DWord;
            else if (value is Array) {
                if (value is byte[]) 
                    return RegistryValueKind.Binary;
                else if (value is String[]) 
                    return RegistryValueKind.MultiString; 
                else
                    throw new ArgumentException(Environment.GetResourceString("Arg_RegSetBadArrType", value.GetType().Name)); 
            }
            else
                return RegistryValueKind.String;
        } 

        /** 
         * Retrieves a string representation of this key. 
         *
         * @return a string representing the key. 
         */
        public override String ToString() {
            EnsureNotDisposed();
            return keyName; 
        }
 
#if !FEATURE_PAL 
        public RegistrySecurity GetAccessControl() {
            return GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group); 
        }

        public RegistrySecurity GetAccessControl(AccessControlSections includeSections) {
            EnsureNotDisposed(); 
            return new RegistrySecurity(hkey, keyName, includeSections);
        } 
 
        public void SetAccessControl(RegistrySecurity registrySecurity) {
            EnsureWriteable(); 
            if (registrySecurity == null)
                throw new ArgumentNullException("registrySecurity");

            registrySecurity.Persist(hkey, keyName); 
        }
#endif 
 
        /**
         * After calling GetLastWin32Error(), it clears the last error field, 
         * so you must save the HResult and pass it to this method.  This method
         * will determine the appropriate exception to throw dependent on your
         * error, and depending on the error, insert a string into the message
         * gotten from the ResourceManager. 
         */
        internal void Win32Error(int errorCode, String str) { 
            switch (errorCode) { 
                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str != null) 
                        throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key", str));
                    else
                        throw new UnauthorizedAccessException();
 
                case Win32Native.ERROR_INVALID_HANDLE:
                    this.hkey.SetHandleAsInvalid(); 
                    this.hkey = null; 
                    goto default;
 
                case Win32Native.ERROR_MORE_DATA:
                    // ignore ERROR_MORE_DATA from remote machines.  Win9x machines return this when we try to get the size or
                    // type of a value without the data.
                    if (remoteKey) 
                        return;
                    else 
                        goto default; 

                case Win32Native.ERROR_FILE_NOT_FOUND: 
                    throw new IOException(Environment.GetResourceString("Arg_RegKeyNotFound"), errorCode);

                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode); 
            }
        } 
        internal static void Win32ErrorStatic(int errorCode, String str) { 
            switch (errorCode) {
                case Win32Native.ERROR_ACCESS_DENIED: 
                    if (str != null)
                        throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key", str));
                    else
                        throw new UnauthorizedAccessException(); 

                default: 
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode); 
            }
        } 

        internal static String FixupName(String name)
        {
            BCLDebug.Assert(name!=null,"[FixupName]name!=null"); 
            if (name.IndexOf('\\') == -1)
                return name; 
 
            StringBuilder sb = new StringBuilder(name);
            FixupPath(sb); 
            int temp = sb.Length - 1;
            if (sb[temp] == '\\') // Remove trailing slash
                sb.Length = temp;
            return sb.ToString(); 
        }
 
 
        private static void FixupPath(StringBuilder path)
        { 
            int length  = path.Length;
            bool fixup = false;
            char markerChar = (char)0xFFFF;
 
            int i = 1;
            while (i < length - 1) 
            { 
                if (path[i] == '\\')
                { 
                    i++;
                    while (i < length)
                    {
                        if (path[i] == '\\') 
                        {
                           path[i] = markerChar; 
                           i++; 
                           fixup = true;
                        } 
                        else
                           break;
                    }
 
                }
                i++; 
            } 

            if (fixup) 
            {
                i = 0;
                int j = 0;
                while (i < length) 
                {
                    if(path[i] == markerChar) 
                    { 
                        i++;
                        continue; 
                    }
                    path[j] = path[i];
                    i++;
                    j++; 
                }
                path.Length += j - i; 
            } 

        } 

        private void CheckOpenSubKeyPermission(string subkeyName, bool subKeyWritable) {
            // If the parent key is not opened under default mode, we have access already.
            // If the parent key is opened under default mode, we need to check for permission. 
            if(checkMode == RegistryKeyPermissionCheck.Default) {
                CheckSubKeyReadPermission(subkeyName); 
            } 

            if( subKeyWritable && (checkMode == RegistryKeyPermissionCheck.ReadSubTree)) { 
                CheckSubTreeReadWritePermission(subkeyName);
            }
        }
 
        private void CheckOpenSubKeyPermission(string subkeyName, RegistryKeyPermissionCheck subKeyCheck) {
            if(subKeyCheck == RegistryKeyPermissionCheck.Default) { 
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    CheckSubKeyReadPermission(subkeyName);
                } 
            }
            CheckSubTreePermission(subkeyName, subKeyCheck);
        }
 
        private void CheckSubTreePermission(string subkeyName, RegistryKeyPermissionCheck subKeyCheck) {
            if( subKeyCheck == RegistryKeyPermissionCheck.ReadSubTree) { 
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    CheckSubTreeReadPermission(subkeyName);
                } 
            }
            else if(subKeyCheck == RegistryKeyPermissionCheck.ReadWriteSubTree) {
                if( checkMode != RegistryKeyPermissionCheck.ReadWriteSubTree) {
                    CheckSubTreeReadWritePermission(subkeyName); 
                }
            } 
        } 

        private void  CheckSubKeyWritePermission(string subkeyName) { 
            if( remoteKey) {
                CheckUnmanagedCodePermission();
            }
            else { 
                BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating sub key under read-only key!");
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    // If we want to open a subkey of a read-only key as writeable, we need to do the check. 
                    new RegistryPermission(RegistryPermissionAccess.Write, keyName + "\\" + subkeyName + "\\.").Demand();
                } 
            }
        }

        private void  CheckSubKeyReadPermission(string subkeyName) { 
            BCLDebug.Assert(checkMode == RegistryKeyPermissionCheck.Default, "Should be called from a key opened under default mode only!");
            if( remoteKey) { 
                CheckUnmanagedCodePermission(); 
            }
            else { 
                new RegistryPermission(RegistryPermissionAccess.Read, keyName + "\\" + subkeyName + "\\.").Demand();
            }
        }
 
        private void  CheckSubKeyCreatePermission(string subkeyName) {
            if( remoteKey) { 
                CheckUnmanagedCodePermission(); 
            }
            else { 
                BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating sub key under read-only key!");
                if( checkMode == RegistryKeyPermissionCheck.Default) {
                    new RegistryPermission(RegistryPermissionAccess.Create, keyName + "\\" + subkeyName + "\\.").Demand();
                } 
            }
        } 
 
        private void  CheckSubTreeReadPermission(string subkeyName) {
            if( remoteKey) { 
                CheckUnmanagedCodePermission();
            }
            else {
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    new RegistryPermission(RegistryPermissionAccess.Read, keyName + "\\" + subkeyName + "\\").Demand();
                } 
            } 
        }
 
        private void  CheckSubTreeWritePermission(string subkeyName) {
            if( remoteKey) {
                CheckUnmanagedCodePermission();
            } 
            else {
                BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow writing value to read-only key!"); 
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    new RegistryPermission(RegistryPermissionAccess.Write, keyName + "\\" + subkeyName + "\\").Demand();
                } 
            }
        }

        private void  CheckSubTreeReadWritePermission(string subkeyName) { 
            if( remoteKey) {
                CheckUnmanagedCodePermission(); 
            } 
            else {
                // If we want to open a subkey of a read-only key as writeable, we need to do the check. 
                new RegistryPermission(RegistryPermissionAccess.Write |RegistryPermissionAccess.Read,
                        keyName + "\\" + subkeyName ).Demand();
            }
        } 

        static private void  CheckUnmanagedCodePermission() { 
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand(); 
        }
 
        private void CheckValueWritePermission(string valueName) {
            if (remoteKey) {
                // unmanaged code trust required for remote access
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand(); 
            }
            else { 
                BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow writing value to read-only key!"); 
                // skip the security check if the key is opened under write mode
                if( checkMode == RegistryKeyPermissionCheck.Default) { 
                    new RegistryPermission(RegistryPermissionAccess.Write, keyName+"\\"+valueName).Demand();
                }
            }
        } 

        private void CheckValueCreatePermission(string valueName) { 
            if (remoteKey) { 
                // unmanaged code trust required for remote access
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand(); 
            }
            else {
                BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating value under read-only key!");
                // skip the security check if the key is opened under write mode 
                if( checkMode == RegistryKeyPermissionCheck.Default) {
                    new RegistryPermission(RegistryPermissionAccess.Create, keyName+"\\"+valueName).Demand(); 
                } 
            }
        } 

        private void CheckValueReadPermission(string valueName) {
            if( checkMode == RegistryKeyPermissionCheck.Default) {
                // only need to check for default mode (dynamice check) 
                new RegistryPermission(RegistryPermissionAccess.Read, keyName+"\\"+valueName).Demand();
            } 
        } 

        private void CheckKeyReadPermission(){ 
            if( checkMode == RegistryKeyPermissionCheck.Default) {
                // only need to check for default mode (dynamice check)
                new RegistryPermission(RegistryPermissionAccess.Read, keyName + "\\.").Demand();
            } 
        }
 
        private bool ContainsRegistryValue(string name) { 
                int type = 0;
                int datasize = 0; 
                int retval = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
                return retval == 0;
        }
 
        private void EnsureNotDisposed(){
            if (hkey == null) { 
                ThrowHelper.ThrowObjectDisposedException(keyName, ExceptionResource.ObjectDisposed_RegKeyClosed); 
            }
        } 

        private void EnsureWriteable() {
            EnsureNotDisposed();
            if (!IsWritable()) { 
                ThrowHelper.ThrowUnauthorizedAccessException(ExceptionResource.UnauthorizedAccess_RegistryNoWrite);
            } 
        } 

        static int GetRegistryKeyAccess(bool isWritable) { 
            int winAccess;
            if (!isWritable) {
                winAccess = Win32Native.KEY_READ;
            } 
            else {
                winAccess = Win32Native.KEY_READ | Win32Native.KEY_WRITE; 
            } 
            return winAccess;
        } 

        static int GetRegistryKeyAccess(RegistryKeyPermissionCheck mode) {
            int winAccess = 0;
            switch(mode) { 
                case RegistryKeyPermissionCheck.ReadSubTree:
                case RegistryKeyPermissionCheck.Default: 
                    winAccess =  Win32Native.KEY_READ; 
                    break;
 
                case RegistryKeyPermissionCheck.ReadWriteSubTree:
                    winAccess = Win32Native.KEY_READ| Win32Native.KEY_WRITE;
                    break;
 
               default:
                    BCLDebug.Assert(false, "unexpected code path"); 
                    break; 
            }
            return winAccess; 
        }

        private RegistryKeyPermissionCheck GetSubKeyPermissonCheck(bool subkeyWritable) {
            if( checkMode == RegistryKeyPermissionCheck.Default) { 
                return checkMode;
            } 
 
            if(subkeyWritable) {
                return RegistryKeyPermissionCheck.ReadWriteSubTree; 
            }
            else {
                return RegistryKeyPermissionCheck.ReadSubTree;
            } 
        }
 
        static private void ValidateKeyName(string name) { 
            if (name == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name); 
            }

            int nextSlash = name.IndexOf("\\", StringComparison.OrdinalIgnoreCase);
            int current = 0; 
            while (nextSlash != -1) {
                if ((nextSlash - current) > MaxKeyLength) 
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug); 

                current = nextSlash + 1; 
                nextSlash = name.IndexOf("\\", current, StringComparison.OrdinalIgnoreCase);
            }

            if ((name.Length - current) > MaxKeyLength) 
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug);
 
        } 

        static private void ValidateKeyMode(RegistryKeyPermissionCheck mode) { 
            if( mode < RegistryKeyPermissionCheck.Default || mode > RegistryKeyPermissionCheck.ReadWriteSubTree) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryKeyPermissionCheck, ExceptionArgument.mode);
            }
        } 

        static private void ValidateKeyRights(int rights) { 
            if(0 != (rights & ~((int) RegistryRights.FullControl))) { 
                // We need to throw SecurityException here for compatiblity reason,
                // although UnauthorizedAccessException will make more sense. 
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
            }
        }
 	 
        // Win32 constants for error handling
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200; 
        private const int FORMAT_MESSAGE_FROM_SYSTEM    = 0x00001000; 
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
    } 

    [Flags]
    public enum RegistryValueOptions {
        None = 0, 
        DoNotExpandEnvironmentNames = 1
    } 
 
    // the name for this API is meant to mimic FileMode, which has similar values
 
    public enum RegistryKeyPermissionCheck {
        Default = 0,
        ReadSubTree = 1,
        ReadWriteSubTree = 2 
    }
} 
