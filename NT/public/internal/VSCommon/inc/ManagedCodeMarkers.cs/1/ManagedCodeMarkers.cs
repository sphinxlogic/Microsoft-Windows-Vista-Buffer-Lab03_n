using System; 
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Microsoft.Internal.Performance 
{
 	internal sealed class CodeMarkers 
	{ 
        // Singleton access
        public static readonly CodeMarkers Instance = new CodeMarkers(); 

        internal class NativeMethods
        {
            ///// Code markers' functions (imported from the code markers dll) 
#if Codemarkers_IncludeAppEnum
            [DllImport(DllName, EntryPoint = "InitPerf")] 
            public static extern void DllInitPerf(System.Int32 iApp); 

            [DllImport(DllName, EntryPoint = "UnInitPerf")] 
            public static extern void DllUnInitPerf(System.Int32 iApp);
#endif //Codemarkers_IncludeAppEnum

            [DllImport(DllName, EntryPoint = "PerfCodeMarker")] 
            public static extern void DllPerfCodeMarker(System.Int32 nTimerID, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)] byte [] aUserParams, System.Int32 cbParams);
 
            [DllImport("kernel32.dll")] 
            public static extern System.UInt16 FindAtom(string lpString);
 
            [DllImport("kernel32.dll")]
            public static extern System.UInt16 AddAtom(string lpString);

            [DllImport("kernel32.dll")] 
            public static extern System.UInt16 DeleteAtom(System.UInt16 atom);
        } 
 
        // Atom name. This ATOM will be set by the host application when code markers are enabled
        // in the registry. 
        const string AtomName = "VSCodeMarkersEnabled";

        // CodeMarkers DLL name
        const string DllName = "Microsoft.Internal.Performance.CodeMarkers.dll"; 

        // Do we want to use code markers? 
        bool fUseCodeMarkers; 

        // Constructor. Do not call directly. Use CodeMarkers.Instance to access the singleton 
        // Checks to see if code markers are enabled by looking for a named ATOM
        private CodeMarkers()
        {
            // This ATOM will be set by the native Code Markers host 
            fUseCodeMarkers = (NativeMethods.FindAtom(AtomName) != 0);
        } 
 
        // Implements sending the code marker value nTimerID.
        public void CodeMarker(CodeMarkerEvent nTimerID) 
        {
            if (!fUseCodeMarkers)
                return;
 
            try
            { 
                NativeMethods.DllPerfCodeMarker((int)nTimerID, null, 0); 
            }
            catch( DllNotFoundException ) 
            {
                // If the DLL doesn't load or the entry point doesn't exist, then
                // abandon all further attempts to send codemarkers.
                fUseCodeMarkers = false; 
            }
        } 
 
        // Implements sending the code marker value nTimerID with additional user data
        public void CodeMarkerEx(CodeMarkerEvent nTimerID, byte[] aBuff) 
        {
            if (aBuff == null)
                throw new ArgumentNullException("aBuff");
 
            if (!fUseCodeMarkers)
                return; 
 
            try
            { 
                NativeMethods.DllPerfCodeMarker((int)nTimerID, aBuff, aBuff.Length);
            }
            catch (DllNotFoundException)
            { 
                // If the DLL doesn't load or the entry point doesn't exist, then
                // abandon all further attempts to send codemarkers. 
                fUseCodeMarkers = false; 
            }
        } 

#if Codemarkers_IncludeAppEnum
        // Check the registry and, if appropriate, loads and initializes the code markers dll.
        // Must be used only if your code is called from outside of VS. 
        [Obsolete("Please use InitPerformanceDll(CodeMarkerApp, string) instead to specify a registry root")]
        public void InitPerformanceDll(CodeMarkerApp iApp) 
        { 
            InitPerformanceDll(iApp, "Software\\Microsoft\\VisualStudio\\8.0" );
        } 

        public void InitPerformanceDll(CodeMarkerApp iApp, string strRegRoot )
        {
            fUseCodeMarkers = false; 

            if (!UseCodeMarkers( strRegRoot )) 
            { 
                return;
            } 

            try
            {
                // Add an ATOM so that other CodeMarker enabled code in this process 
                // knows that CodeMarkers are enabled
                NativeMethods.AddAtom(AtomName); 
 
                NativeMethods.DllInitPerf((int)iApp);
                fUseCodeMarkers = true; 
            }
            catch( DllNotFoundException )
            {
                ; // Ignore, but note that fUseCodeMarkers is false 
            }
        } 
 
        [Obsolete("Second parameter is ignored. Please use InitPerformanceDll(CodeMarkerApp, string) instead to specify a registry root")]
        public void InitPerformanceDll(CodeMarkerApp iApp, bool bEndBoot) 
        {
            // bEndBoot is ignored
            InitPerformanceDll( iApp );
        } 

        // Checks the registry to see if code markers are enabled 
        bool UseCodeMarkers( string strRegRoot ) 
        {
            // SECURITY: We no longer check HKCU because that might lead to a DLL spoofing attack via 
            // the code markers DLL. Check only HKLM since that has a strong ACL. You therefore need
            // admin rights to enable/disable code markers.

            // It doesn't matter what the value is, if it's present and not empty, code markers are enabled 
            return !String.IsNullOrEmpty( GetPerformanceSubKey(Registry.LocalMachine, strRegRoot) );
        } 
 
        // Reads the Performance subkey from the appropriate registry key
        // Returns: the Default value from the subkey (null if not found) 
        string GetPerformanceSubKey(RegistryKey hKey, string strRegRoot)
        {
            if (hKey == null)
                return null; 

            // does the subkey exist 
			string str = null; 
			using (RegistryKey key = hKey.OpenSubKey(strRegRoot + "\\Performance"))
 			{ 
				if (key != null)
 				{
 					// reads the default value
					str = key.GetValue("").ToString(); 
 				}
			} 
			return str; 
		}
 
        // Opposite of InitPerformanceDLL. Call it when your app does not need the code markers dll.
        public void UninitializePerformanceDLL(CodeMarkerApp iApp)
        {
            if (!fUseCodeMarkers) 
            {
                return; 
            } 

            fUseCodeMarkers = false; 

            // Delete the atom created during the initialization if it exists
            System.UInt16 atom = NativeMethods.FindAtom(AtomName);
            if (atom != 0) 
            {
                NativeMethods.DeleteAtom(atom); 
            } 

            try 
            {
                NativeMethods.DllUnInitPerf((int)iApp);
            }
            catch( DllNotFoundException ) 
            {
                // Eat exception 
            } 
        }
#endif //Codemarkers_IncludeAppEnum 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
using System; 
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Microsoft.Internal.Performance 
{
 	internal sealed class CodeMarkers 
	{ 
        // Singleton access
        public static readonly CodeMarkers Instance = new CodeMarkers(); 

        internal class NativeMethods
        {
            ///// Code markers' functions (imported from the code markers dll) 
#if Codemarkers_IncludeAppEnum
            [DllImport(DllName, EntryPoint = "InitPerf")] 
            public static extern void DllInitPerf(System.Int32 iApp); 

            [DllImport(DllName, EntryPoint = "UnInitPerf")] 
            public static extern void DllUnInitPerf(System.Int32 iApp);
#endif //Codemarkers_IncludeAppEnum

            [DllImport(DllName, EntryPoint = "PerfCodeMarker")] 
            public static extern void DllPerfCodeMarker(System.Int32 nTimerID, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)] byte [] aUserParams, System.Int32 cbParams);
 
            [DllImport("kernel32.dll")] 
            public static extern System.UInt16 FindAtom(string lpString);
 
            [DllImport("kernel32.dll")]
            public static extern System.UInt16 AddAtom(string lpString);

            [DllImport("kernel32.dll")] 
            public static extern System.UInt16 DeleteAtom(System.UInt16 atom);
        } 
 
        // Atom name. This ATOM will be set by the host application when code markers are enabled
        // in the registry. 
        const string AtomName = "VSCodeMarkersEnabled";

        // CodeMarkers DLL name
        const string DllName = "Microsoft.Internal.Performance.CodeMarkers.dll"; 

        // Do we want to use code markers? 
        bool fUseCodeMarkers; 

        // Constructor. Do not call directly. Use CodeMarkers.Instance to access the singleton 
        // Checks to see if code markers are enabled by looking for a named ATOM
        private CodeMarkers()
        {
            // This ATOM will be set by the native Code Markers host 
            fUseCodeMarkers = (NativeMethods.FindAtom(AtomName) != 0);
        } 
 
        // Implements sending the code marker value nTimerID.
        public void CodeMarker(CodeMarkerEvent nTimerID) 
        {
            if (!fUseCodeMarkers)
                return;
 
            try
            { 
                NativeMethods.DllPerfCodeMarker((int)nTimerID, null, 0); 
            }
            catch( DllNotFoundException ) 
            {
                // If the DLL doesn't load or the entry point doesn't exist, then
                // abandon all further attempts to send codemarkers.
                fUseCodeMarkers = false; 
            }
        } 
 
        // Implements sending the code marker value nTimerID with additional user data
        public void CodeMarkerEx(CodeMarkerEvent nTimerID, byte[] aBuff) 
        {
            if (aBuff == null)
                throw new ArgumentNullException("aBuff");
 
            if (!fUseCodeMarkers)
                return; 
 
            try
            { 
                NativeMethods.DllPerfCodeMarker((int)nTimerID, aBuff, aBuff.Length);
            }
            catch (DllNotFoundException)
            { 
                // If the DLL doesn't load or the entry point doesn't exist, then
                // abandon all further attempts to send codemarkers. 
                fUseCodeMarkers = false; 
            }
        } 

#if Codemarkers_IncludeAppEnum
        // Check the registry and, if appropriate, loads and initializes the code markers dll.
        // Must be used only if your code is called from outside of VS. 
        [Obsolete("Please use InitPerformanceDll(CodeMarkerApp, string) instead to specify a registry root")]
        public void InitPerformanceDll(CodeMarkerApp iApp) 
        { 
            InitPerformanceDll(iApp, "Software\\Microsoft\\VisualStudio\\8.0" );
        } 

        public void InitPerformanceDll(CodeMarkerApp iApp, string strRegRoot )
        {
            fUseCodeMarkers = false; 

            if (!UseCodeMarkers( strRegRoot )) 
            { 
                return;
            } 

            try
            {
                // Add an ATOM so that other CodeMarker enabled code in this process 
                // knows that CodeMarkers are enabled
                NativeMethods.AddAtom(AtomName); 
 
                NativeMethods.DllInitPerf((int)iApp);
                fUseCodeMarkers = true; 
            }
            catch( DllNotFoundException )
            {
                ; // Ignore, but note that fUseCodeMarkers is false 
            }
        } 
 
        [Obsolete("Second parameter is ignored. Please use InitPerformanceDll(CodeMarkerApp, string) instead to specify a registry root")]
        public void InitPerformanceDll(CodeMarkerApp iApp, bool bEndBoot) 
        {
            // bEndBoot is ignored
            InitPerformanceDll( iApp );
        } 

        // Checks the registry to see if code markers are enabled 
        bool UseCodeMarkers( string strRegRoot ) 
        {
            // SECURITY: We no longer check HKCU because that might lead to a DLL spoofing attack via 
            // the code markers DLL. Check only HKLM since that has a strong ACL. You therefore need
            // admin rights to enable/disable code markers.

            // It doesn't matter what the value is, if it's present and not empty, code markers are enabled 
            return !String.IsNullOrEmpty( GetPerformanceSubKey(Registry.LocalMachine, strRegRoot) );
        } 
 
        // Reads the Performance subkey from the appropriate registry key
        // Returns: the Default value from the subkey (null if not found) 
        string GetPerformanceSubKey(RegistryKey hKey, string strRegRoot)
        {
            if (hKey == null)
                return null; 

            // does the subkey exist 
			string str = null; 
			using (RegistryKey key = hKey.OpenSubKey(strRegRoot + "\\Performance"))
 			{ 
				if (key != null)
 				{
 					// reads the default value
					str = key.GetValue("").ToString(); 
 				}
			} 
			return str; 
		}
 
        // Opposite of InitPerformanceDLL. Call it when your app does not need the code markers dll.
        public void UninitializePerformanceDLL(CodeMarkerApp iApp)
        {
            if (!fUseCodeMarkers) 
            {
                return; 
            } 

            fUseCodeMarkers = false; 

            // Delete the atom created during the initialization if it exists
            System.UInt16 atom = NativeMethods.FindAtom(AtomName);
            if (atom != 0) 
            {
                NativeMethods.DeleteAtom(atom); 
            } 

            try 
            {
                NativeMethods.DllUnInitPerf((int)iApp);
            }
            catch( DllNotFoundException ) 
            {
                // Eat exception 
            } 
        }
#endif //Codemarkers_IncludeAppEnum 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
