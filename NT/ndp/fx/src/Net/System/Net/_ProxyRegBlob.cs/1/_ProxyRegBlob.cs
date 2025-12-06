//------------------------------------------------------------------------------ 
// <copyright file="_ProxyRegBlob.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System; 
    using System.Security.Permissions;
    using System.Globalization; 
    using System.Text;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Net.Sockets; 
    using System.Threading;
    using System.Runtime.InteropServices; 
#if USE_WINIET_AUTODETECT_CACHE 
#if !FEATURE_PAL
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME; 
#endif // !FEATURE_PAL
#endif
    using Microsoft.Win32;
    using System.Runtime.Versioning; 

    internal class ProxyRegBlob { 
 
#if !FEATURE_PAL
        // 
        // Allows us to grob through the registry and read the
        //  IE binary format, note that this should be replaced,
        //  by code that calls Wininet directly, but it can be
        //  expensive to load wininet, in order to do this. 
        //
 
        [Flags] 
        private enum ProxyTypeFlags
        { 
            PROXY_TYPE_DIRECT          = 0x00000001,   // direct to net
            PROXY_TYPE_PROXY           = 0x00000002,   // via named proxy
            PROXY_TYPE_AUTO_PROXY_URL  = 0x00000004,   // autoproxy URL
            PROXY_TYPE_AUTO_DETECT     = 0x00000008,   // use autoproxy detection 
        }
 
        internal const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\Internet Settings"; 
        internal const string ProxyKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\Connections";
        private const string DefaultConnectionSettings = "DefaultConnectionSettings"; 
        private const string ProxySettingsPerUser = "ProxySettingsPerUser";

#if USE_WINIET_AUTODETECT_CACHE
        // Get the number of MilliSeconds in 7 days and then multiply by 10 because 
        // FILETIME stores data stores time in 100-nanosecond intervals.
        // 
        internal static UInt64 s_lkgScriptValidTime = (UInt64)(new TimeSpan(7, 0, 0, 0).Ticks); // 7 days 
#endif
 
        const int IE50StrucSize = 60;

        private byte[] m_RegistryBytes;
        private int m_ByteOffset; 

        // returns true - on successful read of proxy registry settings 
        [RegistryPermission(SecurityAction.Assert, Read=@"HKEY_LOCAL_MACHINE\" + PolicyKey)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        private bool ReadRegSettings(string connectoid, SafeRegistryHandle hkcu)
        {
            SafeRegistryHandle key = null;
            RegistryKey lmKey = null; 
            try {
                bool isPerUser = true; 
                lmKey = Registry.LocalMachine.OpenSubKey(PolicyKey); 
                if (lmKey != null)
                { 
                    object perUser = lmKey.GetValue(ProxySettingsPerUser);
                    if (perUser != null && perUser.GetType() == typeof(int) && 0 == (int) perUser)
                    {
                        isPerUser = false; 
                    }
                } 
 
                uint errorCode;
                if (isPerUser) 
                {
                    if (hkcu != null)
                    {
                        errorCode = hkcu.RegOpenKeyEx(ProxyKey, 0, UnsafeNclNativeMethods.RegistryHelper.KEY_READ, out key); 
                    }
                    else 
                    { 
                        errorCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_NOT_FOUND;
                    } 
                }
                else
                {
                    errorCode = SafeRegistryHandle.RegOpenKeyEx(UnsafeNclNativeMethods.RegistryHelper.HKEY_LOCAL_MACHINE, ProxyKey, 0, UnsafeNclNativeMethods.RegistryHelper.KEY_READ, out key); 
                }
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS) 
                { 
                    key = null;
                } 
                if (key != null)
                {
                    // When reading settings from the registry, if connectoid key is missing, the connectoid
                    // was never configured. In this case we have no settings (this is equivalent to always go direct). 
                    object data;
                    errorCode = key.QueryValue(connectoid != null ? connectoid : DefaultConnectionSettings, out data); 
                    if (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS) 
                    {
                        m_RegistryBytes = (byte[]) data; 
                    }
                }
            }
            catch (Exception exception) { 
                if (NclUtilities.IsFatal(exception)) throw;
            } 
            finally 
            {
                if (lmKey != null) 
                    lmKey.Close();

                if(key != null)
                    key.RegCloseKey(); 
            }
            return m_RegistryBytes != null; 
        } 

#if USE_WINIET_AUTODETECT_CACHE 
        public FILETIME ReadFileTime() {
            FILETIME ft = new FILETIME();
            ft.dwLowDateTime = ReadInt32();
            ft.dwHighDateTime = ReadInt32(); 
            return ft;
        } 
#endif 

        // 
        // Reads a string from the byte buffer, cached
        //  inside this object, and then updates the
        //  offset, NOTE: Must be in the correct offset
        //  before reading, or will error 
        //
        public string ReadString() { 
            string stringOut = null; 
            int stringSize = ReadInt32();
            if (stringSize>0) { 
                // prevent reading too much
                int actualSize = m_RegistryBytes.Length - m_ByteOffset;
                if (stringSize >= actualSize) {
                    stringSize = actualSize; 
                }
                stringOut = Encoding.UTF8.GetString(m_RegistryBytes, m_ByteOffset, stringSize); 
                m_ByteOffset += stringSize; 
            }
            return stringOut; 
        }


        // 
        // Reads a DWORD into a Int32, used to read
        //  a int from the byte buffer. 
        // 
        internal unsafe int ReadInt32() {
            int intValue = 0; 
            int actualSize = m_RegistryBytes.Length - m_ByteOffset;
            // copy bytes and increment offset
            if (actualSize>=sizeof(int)) {
                fixed (byte* pBuffer = m_RegistryBytes) { 
                    if (sizeof(IntPtr)==4) {
                        intValue = *((int*)(pBuffer + m_ByteOffset)); 
                    } 
                    else {
                        intValue = Marshal.ReadInt32((IntPtr)pBuffer, m_ByteOffset); 
                    }
                }
                m_ByteOffset += sizeof(int);
            } 
            // tell caller what we actually read
            return intValue; 
        } 
#else // !FEATURE_PAL
        private static string ReadConfigString(string ConfigName) { 
            const int parameterValueLength = 255;
            StringBuilder parameterValue = new StringBuilder(parameterValueLength);
            bool rc = UnsafeNclNativeMethods.FetchConfigurationString(true, ConfigName, parameterValue, parameterValueLength);
            if (rc) { 
                return parameterValue.ToString();
            } 
            return ""; 
        }
#endif // !FEATURE_PAL 

        //
        // Parses out a string from IE and turns it into a URI
        // 
        private static Uri ParseProxyUri(string proxyString, bool validate) {
            if (validate) { 
                if (proxyString.Length == 0) { 
                    return null;
                } 
                if (proxyString.IndexOf('=') != -1) {
                    return null;
                }
            } 
            if (proxyString.IndexOf("://") == -1) {
                proxyString = "http://" + proxyString; 
            } 
            return new Uri(proxyString);
        } 

        //
        // Builds a hashtable containing the protocol and proxy URI to use for it.
        // 
        private static Hashtable ParseProtocolProxies(string proxyListString) {
            if (proxyListString.Length == 0) { 
                return null; 
            }
            // parse something like "http=http://http-proxy;https=http://https-proxy;ftp=http://ftp-proxy" 
            char[] splitChars = new char[] { ';', '=' };
            string[] proxyListStrings = proxyListString.Split(splitChars);
            bool protocolPass = true;
            string protocolString = null; 
            Hashtable proxyListHashTable = new Hashtable(CaseInsensitiveAscii.StaticInstance);
            foreach (string elementString in proxyListStrings) { 
                string elementString2 = elementString.Trim().ToLower(CultureInfo.InvariantCulture); 
                if (protocolPass) {
                    protocolString = elementString2; 
                }
                else {
                    proxyListHashTable[protocolString] = ParseProxyUri(elementString2, false);
                } 
                protocolPass = !protocolPass;
            } 
            if (proxyListHashTable.Count == 0) { 
                return null;
            } 
            return proxyListHashTable;
        }

        // 
        // Converts a simple IE regular expresion string into one
        //  that is compatible with Regex escape sequences. 
        // 
        private static string BypassStringEscape(string bypassString) {
            StringBuilder escapedBypass = new StringBuilder(); 
            // (\, *, +, ?, |, {, [, (,), ^, $, ., #, and whitespace) are reserved
            foreach (char c in bypassString){
                if (c == '\\' || c == '.' || c == '?' || c == '(' || c == ')' || c == '|' || c == '^' || c == '+' ||
                    c == '{' || c == '[' || c == '$' || c == '#') 
                {
                    escapedBypass.Append('\\'); 
                } 
                else if (c == '*') {
                    escapedBypass.Append('.'); 
                }
                escapedBypass.Append(c);
            }
            escapedBypass.Append('$'); 
            return escapedBypass.ToString();
        } 
 

        // 
        // Parses out a string of bypass list entries and coverts it to Regex's that can be used
        //   to match against.
        //
        private static ArrayList ParseBypassList(string bypassListString, out bool bypassOnLocal) { 
            char[] splitChars = new char[] {';'};
            string[] bypassListStrings = bypassListString.Split(splitChars); 
            bypassOnLocal = false; 
            if (bypassListStrings.Length == 0) {
                return null; 
            }
            ArrayList bypassList = null;
            foreach (string bypassString in bypassListStrings) {
                if (bypassString!=null) { 
                    string bypassString2 = bypassString.Trim();
                    if (bypassString2.Length>0) { 
                        if (string.Compare(bypassString2, "<local>", StringComparison.OrdinalIgnoreCase)==0) { 
                            bypassOnLocal = true;
                        } 
                        else {
                            bypassString2 = BypassStringEscape(bypassString2);
                            if (bypassList==null) {
                                bypassList = new ArrayList(); 
                            }
                            GlobalLog.Print("ProxyRegBlob::ParseBypassList() bypassList.Count:" + bypassList.Count + " adding:" + ValidationHelper.ToString(bypassString2)); 
                            if (!bypassList.Contains(bypassString2)) { 
                                bypassList.Add(bypassString2);
                                GlobalLog.Print("ProxyRegBlob::ParseBypassList() bypassList.Count:" + bypassList.Count + " added:" + ValidationHelper.ToString(bypassString2)); 
                            }
                        }
                    }
                } 
            }
            return bypassList; 
        } 

        // 
        // Updates an instance of WbeProxy with the proxy settings from IE for:
        // the current user and a given connectoid.
        //
        [ResourceExposure(ResourceScope.Machine)]  // Check scoping on this SafeRegistryHandle 
        [ResourceConsumption(ResourceScope.Machine)]
#if !FEATURE_PAL 
        internal static WebProxyData GetWebProxyData(string connectoid, SafeRegistryHandle hkcu) 
#else // !FEATURE_PAL
        internal static WebProxyData GetWebProxyData(string connectoid) 
#endif // !FEATURE_PAL
        {
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() connectoid:" + ValidationHelper.ToString(connectoid));
            WebProxyData webProxyData = new WebProxyData(); 
            Hashtable proxyHashTable = null;
            Uri address = null; 
 
#if !FEATURE_PAL
            ProxyRegBlob proxyIE5Settings = new ProxyRegBlob(); 
            // DON'T TOUCH THE ORDERING OF THE CALLS TO THE INSTANCE OF ProxyRegBlob
            bool success = proxyIE5Settings.ReadRegSettings(connectoid, hkcu);
            if (success) {
                success = proxyIE5Settings.ReadInt32()>=ProxyRegBlob.IE50StrucSize; 
            }
            if (!success) { 
                // if registry access fails rely on automatic detection 
                webProxyData.automaticallyDetectSettings = true;
                return webProxyData; 
            }
            // read the rest of the items out
            proxyIE5Settings.ReadInt32(); // incremental version# of current settings (ignored)
            ProxyTypeFlags proxyFlags = (ProxyTypeFlags)proxyIE5Settings.ReadInt32(); // flags 
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() proxyFlags:" + ValidationHelper.ToString(proxyFlags));
 
            string addressString = proxyIE5Settings.ReadString(); // proxy name 
            string proxyBypassString = proxyIE5Settings.ReadString(); // proxy bypass
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() proxyAddressString:" + ValidationHelper.ToString(addressString) + " proxyBypassString:" + ValidationHelper.ToString(proxyBypassString)); 

            //
            // Once we verify that the flag for proxy is enabled,
            // Parse UriString that is stored, may be in the form, 
            //  of "http=http://http-proxy;ftp="ftp=http://..." must
            //  handle this case along with just a URI. 
            // 
            if ((proxyFlags & ProxyTypeFlags.PROXY_TYPE_PROXY)!=0) {
                try { 

 			//VSWHIDBEY: 583058
			//When the "manual" proxy check box is checked and there is no proxy string
			//present, the ReadString above returns null. 
			//Which means thet when we call the ParseProxyUri, there will be an
 			//Aceess Violation since we try to compute the length of the string 
			//The fix is to not do any more processing if the addresString is null 
 			if(addressString != null)
 			{ 
	                    address = ParseProxyUri(addressString, true);
 	                    if ( address == null ) {
	                        proxyHashTable = ParseProtocolProxies(addressString);
	                    } 
	                    if ((address != null || proxyHashTable != null) && proxyBypassString != null) {
 	                        // reuse success for bypassOnLocal 
	                        webProxyData.bypassList = ParseBypassList(proxyBypassString, out success); 
 	                        webProxyData.bypassOnLocal = success;
 	                    } 
			}
                }
                catch (Exception exception) {
                    if (NclUtilities.IsFatal(exception)) throw; 
                }
            } 
#else // !FEATURE_PAL 
            string proxyAddressString = ReadConfigString("ProxyUri");
            string proxyBypassString = ReadConfigString("ProxyBypass"); 
            try {
 		if(proxyAddressString != null)			
		{
	                address = ParseProxyUri(proxyAddressString, true); 
	                if ( address == null ) {
 	                    proxyHashTable = ParseProtocolProxies(proxyAddressString); 
	                } 
 	                if ((address != null || proxyHashTable != null) && proxyBypassString != null ) {
 	                    webProxyData.bypassList = ParseBypassList(proxyBypassString, out webProxyData.bypassOnLocal); 
	                }
 		}
                // success if we reach here
            } 
            catch {
            } 
#endif // !FEATURE_PAL 

            if (proxyHashTable!=null) { 
                address = proxyHashTable["http"] as Uri;
            }
            webProxyData.proxyAddress = address;
 
#if !FEATURE_PAL
            webProxyData.automaticallyDetectSettings = (proxyFlags & ProxyTypeFlags.PROXY_TYPE_AUTO_DETECT)!=0; 
 
            // reuse addressString for scriptLocationString
            addressString = proxyIE5Settings.ReadString(); // autoconfig url 
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() scriptLocation:" + ValidationHelper.ToString(addressString));
            if ((proxyFlags & ProxyTypeFlags.PROXY_TYPE_AUTO_PROXY_URL)!=0) {
                // reuse addressString for scriptLocation
                if (Uri.TryCreate(addressString, UriKind.Absolute, out address)) { 
                    webProxyData.scriptLocation = address;
                } 
            } 

// The final straw against attempting to use the WinInet LKG script location was, it's invalid when IPs have changed even if the 
// connectoid hadn't.  Doing that validation didn't seem worth it (error-prone, expensive, unsupported).
#if USE_WINIET_AUTODETECT_CACHE
            proxyIE5Settings.ReadInt32(); // autodetect flags (ignored)
 
            // reuse addressString for lkgScriptLocationString
            addressString = proxyIE5Settings.ReadString(); // last known good auto-proxy url 
 
            // read ftLastKnownDetectTime
            FILETIME ftLastKnownDetectTime = proxyIE5Settings.ReadFileTime(); 

            // Verify if this lkgScriptLocationString has timed out
            //
            if (IsValidTimeForLkgScriptLocation(ftLastKnownDetectTime)) { 
                // reuse address for lkgScriptLocation
                GlobalLog.Print("ProxyRegBlob::GetWebProxyData() lkgScriptLocation:" + ValidationHelper.ToString(addressString)); 
                if (Uri.TryCreate(addressString, UriKind.Absolute, out address)) { 
                    webProxyData.lkgScriptLocation = address;
                } 
            }
            else {
#if TRAVE
                SYSTEMTIME st = new SYSTEMTIME(); 
                bool f = SafeNclNativeMethods.FileTimeToSystemTime(ref ftLastKnownDetectTime, ref st);
                if (f) 
                    GlobalLog.Print("ProxyRegBlob::GetWebProxyData() ftLastKnownDetectTime:" + ValidationHelper.ToString(st)); 
#endif // TRAVE
                GlobalLog.Print("ProxyRegBlob::GetWebProxyData() Ignoring Timed out lkgScriptLocation:" + ValidationHelper.ToString(addressString)); 

                // Now rely on automatic detection settings set above
                // based on the proxy flags (webProxyData.automaticallyDetectSettings).
                // 
            }
#endif 
            /* 
            // This is some of the rest of the proxy reg key blob parsing.
            // 
            // Read Interace IPs
            int iftCount = proxyIE5Settings.ReadInt32();
            for (int ift = 0; ift < iftCount; ++ift) {
                proxyIE5Settings.ReadInt32(); 
            }
 
            // Read lpszAutoconfigSecondaryUrl 
            string autoconfigSecondaryUrl = proxyIE5Settings.ReadString();
 
            // Read dwAutoconfigReloadDelayMins
            int autoconfigReloadDelayMins = proxyIE5Settings.ReadInt32();
            */
#endif 
            return webProxyData;
        } 
 
#if USE_WINIET_AUTODETECT_CACHE
#if !FEATURE_PAL 
        internal unsafe static bool IsValidTimeForLkgScriptLocation(FILETIME ftLastKnownDetectTime) {
            // Get Current System Time.
            FILETIME ftCurrentTime = new FILETIME();
            SafeNclNativeMethods.GetSystemTimeAsFileTime(ref ftCurrentTime); 

            UInt64 ftDetect = (UInt64)ftLastKnownDetectTime.dwHighDateTime; 
            ftDetect <<= (sizeof(int) * 8); 
            ftDetect |= (UInt64)(uint)ftLastKnownDetectTime.dwLowDateTime;
 
            UInt64 ftCurrent = (UInt64)ftCurrentTime.dwHighDateTime;
            ftCurrent <<= (sizeof(int) * 8);
            ftCurrent |= (UInt64)(uint)ftCurrentTime.dwLowDateTime;
 
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() Detect Time:" + ValidationHelper.ToString(ftDetect));
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() Current Time:" + ValidationHelper.ToString(ftCurrent)); 
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() 7 days:" + ValidationHelper.ToString(s_lkgScriptValidTime)); 
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() Delta Time:" + ValidationHelper.ToString((UInt64)(ftCurrent - ftDetect)));
 
            return (ftCurrent - ftDetect) < s_lkgScriptValidTime;
        }
#endif // !FEATURE_PAL
#endif 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="_ProxyRegBlob.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System; 
    using System.Security.Permissions;
    using System.Globalization; 
    using System.Text;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Net.Sockets; 
    using System.Threading;
    using System.Runtime.InteropServices; 
#if USE_WINIET_AUTODETECT_CACHE 
#if !FEATURE_PAL
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME; 
#endif // !FEATURE_PAL
#endif
    using Microsoft.Win32;
    using System.Runtime.Versioning; 

    internal class ProxyRegBlob { 
 
#if !FEATURE_PAL
        // 
        // Allows us to grob through the registry and read the
        //  IE binary format, note that this should be replaced,
        //  by code that calls Wininet directly, but it can be
        //  expensive to load wininet, in order to do this. 
        //
 
        [Flags] 
        private enum ProxyTypeFlags
        { 
            PROXY_TYPE_DIRECT          = 0x00000001,   // direct to net
            PROXY_TYPE_PROXY           = 0x00000002,   // via named proxy
            PROXY_TYPE_AUTO_PROXY_URL  = 0x00000004,   // autoproxy URL
            PROXY_TYPE_AUTO_DETECT     = 0x00000008,   // use autoproxy detection 
        }
 
        internal const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\Internet Settings"; 
        internal const string ProxyKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\Connections";
        private const string DefaultConnectionSettings = "DefaultConnectionSettings"; 
        private const string ProxySettingsPerUser = "ProxySettingsPerUser";

#if USE_WINIET_AUTODETECT_CACHE
        // Get the number of MilliSeconds in 7 days and then multiply by 10 because 
        // FILETIME stores data stores time in 100-nanosecond intervals.
        // 
        internal static UInt64 s_lkgScriptValidTime = (UInt64)(new TimeSpan(7, 0, 0, 0).Ticks); // 7 days 
#endif
 
        const int IE50StrucSize = 60;

        private byte[] m_RegistryBytes;
        private int m_ByteOffset; 

        // returns true - on successful read of proxy registry settings 
        [RegistryPermission(SecurityAction.Assert, Read=@"HKEY_LOCAL_MACHINE\" + PolicyKey)] 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)] 
        private bool ReadRegSettings(string connectoid, SafeRegistryHandle hkcu)
        {
            SafeRegistryHandle key = null;
            RegistryKey lmKey = null; 
            try {
                bool isPerUser = true; 
                lmKey = Registry.LocalMachine.OpenSubKey(PolicyKey); 
                if (lmKey != null)
                { 
                    object perUser = lmKey.GetValue(ProxySettingsPerUser);
                    if (perUser != null && perUser.GetType() == typeof(int) && 0 == (int) perUser)
                    {
                        isPerUser = false; 
                    }
                } 
 
                uint errorCode;
                if (isPerUser) 
                {
                    if (hkcu != null)
                    {
                        errorCode = hkcu.RegOpenKeyEx(ProxyKey, 0, UnsafeNclNativeMethods.RegistryHelper.KEY_READ, out key); 
                    }
                    else 
                    { 
                        errorCode = UnsafeNclNativeMethods.ErrorCodes.ERROR_NOT_FOUND;
                    } 
                }
                else
                {
                    errorCode = SafeRegistryHandle.RegOpenKeyEx(UnsafeNclNativeMethods.RegistryHelper.HKEY_LOCAL_MACHINE, ProxyKey, 0, UnsafeNclNativeMethods.RegistryHelper.KEY_READ, out key); 
                }
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS) 
                { 
                    key = null;
                } 
                if (key != null)
                {
                    // When reading settings from the registry, if connectoid key is missing, the connectoid
                    // was never configured. In this case we have no settings (this is equivalent to always go direct). 
                    object data;
                    errorCode = key.QueryValue(connectoid != null ? connectoid : DefaultConnectionSettings, out data); 
                    if (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS) 
                    {
                        m_RegistryBytes = (byte[]) data; 
                    }
                }
            }
            catch (Exception exception) { 
                if (NclUtilities.IsFatal(exception)) throw;
            } 
            finally 
            {
                if (lmKey != null) 
                    lmKey.Close();

                if(key != null)
                    key.RegCloseKey(); 
            }
            return m_RegistryBytes != null; 
        } 

#if USE_WINIET_AUTODETECT_CACHE 
        public FILETIME ReadFileTime() {
            FILETIME ft = new FILETIME();
            ft.dwLowDateTime = ReadInt32();
            ft.dwHighDateTime = ReadInt32(); 
            return ft;
        } 
#endif 

        // 
        // Reads a string from the byte buffer, cached
        //  inside this object, and then updates the
        //  offset, NOTE: Must be in the correct offset
        //  before reading, or will error 
        //
        public string ReadString() { 
            string stringOut = null; 
            int stringSize = ReadInt32();
            if (stringSize>0) { 
                // prevent reading too much
                int actualSize = m_RegistryBytes.Length - m_ByteOffset;
                if (stringSize >= actualSize) {
                    stringSize = actualSize; 
                }
                stringOut = Encoding.UTF8.GetString(m_RegistryBytes, m_ByteOffset, stringSize); 
                m_ByteOffset += stringSize; 
            }
            return stringOut; 
        }


        // 
        // Reads a DWORD into a Int32, used to read
        //  a int from the byte buffer. 
        // 
        internal unsafe int ReadInt32() {
            int intValue = 0; 
            int actualSize = m_RegistryBytes.Length - m_ByteOffset;
            // copy bytes and increment offset
            if (actualSize>=sizeof(int)) {
                fixed (byte* pBuffer = m_RegistryBytes) { 
                    if (sizeof(IntPtr)==4) {
                        intValue = *((int*)(pBuffer + m_ByteOffset)); 
                    } 
                    else {
                        intValue = Marshal.ReadInt32((IntPtr)pBuffer, m_ByteOffset); 
                    }
                }
                m_ByteOffset += sizeof(int);
            } 
            // tell caller what we actually read
            return intValue; 
        } 
#else // !FEATURE_PAL
        private static string ReadConfigString(string ConfigName) { 
            const int parameterValueLength = 255;
            StringBuilder parameterValue = new StringBuilder(parameterValueLength);
            bool rc = UnsafeNclNativeMethods.FetchConfigurationString(true, ConfigName, parameterValue, parameterValueLength);
            if (rc) { 
                return parameterValue.ToString();
            } 
            return ""; 
        }
#endif // !FEATURE_PAL 

        //
        // Parses out a string from IE and turns it into a URI
        // 
        private static Uri ParseProxyUri(string proxyString, bool validate) {
            if (validate) { 
                if (proxyString.Length == 0) { 
                    return null;
                } 
                if (proxyString.IndexOf('=') != -1) {
                    return null;
                }
            } 
            if (proxyString.IndexOf("://") == -1) {
                proxyString = "http://" + proxyString; 
            } 
            return new Uri(proxyString);
        } 

        //
        // Builds a hashtable containing the protocol and proxy URI to use for it.
        // 
        private static Hashtable ParseProtocolProxies(string proxyListString) {
            if (proxyListString.Length == 0) { 
                return null; 
            }
            // parse something like "http=http://http-proxy;https=http://https-proxy;ftp=http://ftp-proxy" 
            char[] splitChars = new char[] { ';', '=' };
            string[] proxyListStrings = proxyListString.Split(splitChars);
            bool protocolPass = true;
            string protocolString = null; 
            Hashtable proxyListHashTable = new Hashtable(CaseInsensitiveAscii.StaticInstance);
            foreach (string elementString in proxyListStrings) { 
                string elementString2 = elementString.Trim().ToLower(CultureInfo.InvariantCulture); 
                if (protocolPass) {
                    protocolString = elementString2; 
                }
                else {
                    proxyListHashTable[protocolString] = ParseProxyUri(elementString2, false);
                } 
                protocolPass = !protocolPass;
            } 
            if (proxyListHashTable.Count == 0) { 
                return null;
            } 
            return proxyListHashTable;
        }

        // 
        // Converts a simple IE regular expresion string into one
        //  that is compatible with Regex escape sequences. 
        // 
        private static string BypassStringEscape(string bypassString) {
            StringBuilder escapedBypass = new StringBuilder(); 
            // (\, *, +, ?, |, {, [, (,), ^, $, ., #, and whitespace) are reserved
            foreach (char c in bypassString){
                if (c == '\\' || c == '.' || c == '?' || c == '(' || c == ')' || c == '|' || c == '^' || c == '+' ||
                    c == '{' || c == '[' || c == '$' || c == '#') 
                {
                    escapedBypass.Append('\\'); 
                } 
                else if (c == '*') {
                    escapedBypass.Append('.'); 
                }
                escapedBypass.Append(c);
            }
            escapedBypass.Append('$'); 
            return escapedBypass.ToString();
        } 
 

        // 
        // Parses out a string of bypass list entries and coverts it to Regex's that can be used
        //   to match against.
        //
        private static ArrayList ParseBypassList(string bypassListString, out bool bypassOnLocal) { 
            char[] splitChars = new char[] {';'};
            string[] bypassListStrings = bypassListString.Split(splitChars); 
            bypassOnLocal = false; 
            if (bypassListStrings.Length == 0) {
                return null; 
            }
            ArrayList bypassList = null;
            foreach (string bypassString in bypassListStrings) {
                if (bypassString!=null) { 
                    string bypassString2 = bypassString.Trim();
                    if (bypassString2.Length>0) { 
                        if (string.Compare(bypassString2, "<local>", StringComparison.OrdinalIgnoreCase)==0) { 
                            bypassOnLocal = true;
                        } 
                        else {
                            bypassString2 = BypassStringEscape(bypassString2);
                            if (bypassList==null) {
                                bypassList = new ArrayList(); 
                            }
                            GlobalLog.Print("ProxyRegBlob::ParseBypassList() bypassList.Count:" + bypassList.Count + " adding:" + ValidationHelper.ToString(bypassString2)); 
                            if (!bypassList.Contains(bypassString2)) { 
                                bypassList.Add(bypassString2);
                                GlobalLog.Print("ProxyRegBlob::ParseBypassList() bypassList.Count:" + bypassList.Count + " added:" + ValidationHelper.ToString(bypassString2)); 
                            }
                        }
                    }
                } 
            }
            return bypassList; 
        } 

        // 
        // Updates an instance of WbeProxy with the proxy settings from IE for:
        // the current user and a given connectoid.
        //
        [ResourceExposure(ResourceScope.Machine)]  // Check scoping on this SafeRegistryHandle 
        [ResourceConsumption(ResourceScope.Machine)]
#if !FEATURE_PAL 
        internal static WebProxyData GetWebProxyData(string connectoid, SafeRegistryHandle hkcu) 
#else // !FEATURE_PAL
        internal static WebProxyData GetWebProxyData(string connectoid) 
#endif // !FEATURE_PAL
        {
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() connectoid:" + ValidationHelper.ToString(connectoid));
            WebProxyData webProxyData = new WebProxyData(); 
            Hashtable proxyHashTable = null;
            Uri address = null; 
 
#if !FEATURE_PAL
            ProxyRegBlob proxyIE5Settings = new ProxyRegBlob(); 
            // DON'T TOUCH THE ORDERING OF THE CALLS TO THE INSTANCE OF ProxyRegBlob
            bool success = proxyIE5Settings.ReadRegSettings(connectoid, hkcu);
            if (success) {
                success = proxyIE5Settings.ReadInt32()>=ProxyRegBlob.IE50StrucSize; 
            }
            if (!success) { 
                // if registry access fails rely on automatic detection 
                webProxyData.automaticallyDetectSettings = true;
                return webProxyData; 
            }
            // read the rest of the items out
            proxyIE5Settings.ReadInt32(); // incremental version# of current settings (ignored)
            ProxyTypeFlags proxyFlags = (ProxyTypeFlags)proxyIE5Settings.ReadInt32(); // flags 
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() proxyFlags:" + ValidationHelper.ToString(proxyFlags));
 
            string addressString = proxyIE5Settings.ReadString(); // proxy name 
            string proxyBypassString = proxyIE5Settings.ReadString(); // proxy bypass
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() proxyAddressString:" + ValidationHelper.ToString(addressString) + " proxyBypassString:" + ValidationHelper.ToString(proxyBypassString)); 

            //
            // Once we verify that the flag for proxy is enabled,
            // Parse UriString that is stored, may be in the form, 
            //  of "http=http://http-proxy;ftp="ftp=http://..." must
            //  handle this case along with just a URI. 
            // 
            if ((proxyFlags & ProxyTypeFlags.PROXY_TYPE_PROXY)!=0) {
                try { 

 			//VSWHIDBEY: 583058
			//When the "manual" proxy check box is checked and there is no proxy string
			//present, the ReadString above returns null. 
			//Which means thet when we call the ParseProxyUri, there will be an
 			//Aceess Violation since we try to compute the length of the string 
			//The fix is to not do any more processing if the addresString is null 
 			if(addressString != null)
 			{ 
	                    address = ParseProxyUri(addressString, true);
 	                    if ( address == null ) {
	                        proxyHashTable = ParseProtocolProxies(addressString);
	                    } 
	                    if ((address != null || proxyHashTable != null) && proxyBypassString != null) {
 	                        // reuse success for bypassOnLocal 
	                        webProxyData.bypassList = ParseBypassList(proxyBypassString, out success); 
 	                        webProxyData.bypassOnLocal = success;
 	                    } 
			}
                }
                catch (Exception exception) {
                    if (NclUtilities.IsFatal(exception)) throw; 
                }
            } 
#else // !FEATURE_PAL 
            string proxyAddressString = ReadConfigString("ProxyUri");
            string proxyBypassString = ReadConfigString("ProxyBypass"); 
            try {
 		if(proxyAddressString != null)			
		{
	                address = ParseProxyUri(proxyAddressString, true); 
	                if ( address == null ) {
 	                    proxyHashTable = ParseProtocolProxies(proxyAddressString); 
	                } 
 	                if ((address != null || proxyHashTable != null) && proxyBypassString != null ) {
 	                    webProxyData.bypassList = ParseBypassList(proxyBypassString, out webProxyData.bypassOnLocal); 
	                }
 		}
                // success if we reach here
            } 
            catch {
            } 
#endif // !FEATURE_PAL 

            if (proxyHashTable!=null) { 
                address = proxyHashTable["http"] as Uri;
            }
            webProxyData.proxyAddress = address;
 
#if !FEATURE_PAL
            webProxyData.automaticallyDetectSettings = (proxyFlags & ProxyTypeFlags.PROXY_TYPE_AUTO_DETECT)!=0; 
 
            // reuse addressString for scriptLocationString
            addressString = proxyIE5Settings.ReadString(); // autoconfig url 
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() scriptLocation:" + ValidationHelper.ToString(addressString));
            if ((proxyFlags & ProxyTypeFlags.PROXY_TYPE_AUTO_PROXY_URL)!=0) {
                // reuse addressString for scriptLocation
                if (Uri.TryCreate(addressString, UriKind.Absolute, out address)) { 
                    webProxyData.scriptLocation = address;
                } 
            } 

// The final straw against attempting to use the WinInet LKG script location was, it's invalid when IPs have changed even if the 
// connectoid hadn't.  Doing that validation didn't seem worth it (error-prone, expensive, unsupported).
#if USE_WINIET_AUTODETECT_CACHE
            proxyIE5Settings.ReadInt32(); // autodetect flags (ignored)
 
            // reuse addressString for lkgScriptLocationString
            addressString = proxyIE5Settings.ReadString(); // last known good auto-proxy url 
 
            // read ftLastKnownDetectTime
            FILETIME ftLastKnownDetectTime = proxyIE5Settings.ReadFileTime(); 

            // Verify if this lkgScriptLocationString has timed out
            //
            if (IsValidTimeForLkgScriptLocation(ftLastKnownDetectTime)) { 
                // reuse address for lkgScriptLocation
                GlobalLog.Print("ProxyRegBlob::GetWebProxyData() lkgScriptLocation:" + ValidationHelper.ToString(addressString)); 
                if (Uri.TryCreate(addressString, UriKind.Absolute, out address)) { 
                    webProxyData.lkgScriptLocation = address;
                } 
            }
            else {
#if TRAVE
                SYSTEMTIME st = new SYSTEMTIME(); 
                bool f = SafeNclNativeMethods.FileTimeToSystemTime(ref ftLastKnownDetectTime, ref st);
                if (f) 
                    GlobalLog.Print("ProxyRegBlob::GetWebProxyData() ftLastKnownDetectTime:" + ValidationHelper.ToString(st)); 
#endif // TRAVE
                GlobalLog.Print("ProxyRegBlob::GetWebProxyData() Ignoring Timed out lkgScriptLocation:" + ValidationHelper.ToString(addressString)); 

                // Now rely on automatic detection settings set above
                // based on the proxy flags (webProxyData.automaticallyDetectSettings).
                // 
            }
#endif 
            /* 
            // This is some of the rest of the proxy reg key blob parsing.
            // 
            // Read Interace IPs
            int iftCount = proxyIE5Settings.ReadInt32();
            for (int ift = 0; ift < iftCount; ++ift) {
                proxyIE5Settings.ReadInt32(); 
            }
 
            // Read lpszAutoconfigSecondaryUrl 
            string autoconfigSecondaryUrl = proxyIE5Settings.ReadString();
 
            // Read dwAutoconfigReloadDelayMins
            int autoconfigReloadDelayMins = proxyIE5Settings.ReadInt32();
            */
#endif 
            return webProxyData;
        } 
 
#if USE_WINIET_AUTODETECT_CACHE
#if !FEATURE_PAL 
        internal unsafe static bool IsValidTimeForLkgScriptLocation(FILETIME ftLastKnownDetectTime) {
            // Get Current System Time.
            FILETIME ftCurrentTime = new FILETIME();
            SafeNclNativeMethods.GetSystemTimeAsFileTime(ref ftCurrentTime); 

            UInt64 ftDetect = (UInt64)ftLastKnownDetectTime.dwHighDateTime; 
            ftDetect <<= (sizeof(int) * 8); 
            ftDetect |= (UInt64)(uint)ftLastKnownDetectTime.dwLowDateTime;
 
            UInt64 ftCurrent = (UInt64)ftCurrentTime.dwHighDateTime;
            ftCurrent <<= (sizeof(int) * 8);
            ftCurrent |= (UInt64)(uint)ftCurrentTime.dwLowDateTime;
 
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() Detect Time:" + ValidationHelper.ToString(ftDetect));
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() Current Time:" + ValidationHelper.ToString(ftCurrent)); 
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() 7 days:" + ValidationHelper.ToString(s_lkgScriptValidTime)); 
            GlobalLog.Print("ProxyRegBlob::GetWebProxyData() Delta Time:" + ValidationHelper.ToString((UInt64)(ftCurrent - ftDetect)));
 
            return (ftCurrent - ftDetect) < s_lkgScriptValidTime;
        }
#endif // !FEATURE_PAL
#endif 
    }
} 
