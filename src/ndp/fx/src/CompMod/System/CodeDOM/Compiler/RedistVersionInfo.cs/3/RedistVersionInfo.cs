//------------------------------------------------------------------------------ 
// <copyright file="RedistVersionInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.CodeDom.Compiler { 
    using System; 
    using System.Diagnostics;
    using System.IO; 
    using System.CodeDom.Compiler;
    using System.Configuration;
    using System.Collections.Generic;
 
    using Microsoft.Win32;
 
    internal static class RedistVersionInfo { 
        internal const String NameTag = "CompilerVersion";    // name of the tag for specifying the version
 
        internal const String DefaultVersion = InPlaceVersion;      // should match one of the versions below

        //internal const String LatestVersion = "Latest";       // always bind to the latest version
        internal const String InPlaceVersion = "v2.0";        // always bind to Whidbey version 
        internal const String RedistVersion = "v3.5";          // always bind to the Orcas version
 
        private const string dotNetFrameworkSdkInstallKeyValueV35 = "MSBuildToolsPath"; 
        private const string dotNetFrameworkRegistryPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\MSBuild\\ToolsVersions\\3.5";
 
        public static string GetCompilerPath(IDictionary<string, string> provOptions, string compilerExecutable) {
            string compPath = Executor.GetRuntimeInstallDirectory();

            // if provOptions is provided check to see if it alters what version we should bind to. 
            // provOptions can be null if someone does new VB/CSCodeProvider(), in which case
            // they get the Whidbey behavior. 
            if (provOptions != null) { 
                string versionVal;//, newPath;
                if (provOptions.TryGetValue(RedistVersionInfo.NameTag, out versionVal)) { 
                    switch (versionVal) {
                        //case RedistVersionInfo.LatestVersion:
                            // always run against the latest version of the compiler
 
                        //    newPath = GetOrcasPath();
                        //    if (newPath != null && File.Exists(Path.Combine(newPath, compilerExecutable))) 
                        //        compPath = newPath; 
                        //    break;
                        case RedistVersionInfo.RedistVersion: 
                            // lock-forward to the Orcas version, if it's not available throw (we'll throw at compile time)

                            compPath = GetOrcasPath();
                            break; 
                        case RedistVersionInfo.InPlaceVersion:
                            // lock-back to the Whidbey version, no-op 
                            break; 
                        default:
                            compPath = null; 
                            break;
                    }
                }
            } 

            if (compPath == null) 
                throw new InvalidOperationException(SR.GetString(SR.CompilerNotFound, compilerExecutable)); 

            return compPath; 
        }

        private static string GetOrcasPath() {
            // Temporary until the decision is made: 
            //
            // \Windows\Microsoft.NET\Framework\v3.5.xxxxx\ or 
            // \Windows\WinFX\v3.5\ 

            string dir = Registry.GetValue(dotNetFrameworkRegistryPath, dotNetFrameworkSdkInstallKeyValueV35, null) as string; 
            if (dir != null && Directory.Exists(dir)) {
                return dir;
            }
            return null; 
        }
    } 
} 

//------------------------------------------------------------------------------ 
// <copyright file="RedistVersionInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.CodeDom.Compiler { 
    using System; 
    using System.Diagnostics;
    using System.IO; 
    using System.CodeDom.Compiler;
    using System.Configuration;
    using System.Collections.Generic;
 
    using Microsoft.Win32;
 
    internal static class RedistVersionInfo { 
        internal const String NameTag = "CompilerVersion";    // name of the tag for specifying the version
 
        internal const String DefaultVersion = InPlaceVersion;      // should match one of the versions below

        //internal const String LatestVersion = "Latest";       // always bind to the latest version
        internal const String InPlaceVersion = "v2.0";        // always bind to Whidbey version 
        internal const String RedistVersion = "v3.5";          // always bind to the Orcas version
 
        private const string dotNetFrameworkSdkInstallKeyValueV35 = "MSBuildToolsPath"; 
        private const string dotNetFrameworkRegistryPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\MSBuild\\ToolsVersions\\3.5";
 
        public static string GetCompilerPath(IDictionary<string, string> provOptions, string compilerExecutable) {
            string compPath = Executor.GetRuntimeInstallDirectory();

            // if provOptions is provided check to see if it alters what version we should bind to. 
            // provOptions can be null if someone does new VB/CSCodeProvider(), in which case
            // they get the Whidbey behavior. 
            if (provOptions != null) { 
                string versionVal;//, newPath;
                if (provOptions.TryGetValue(RedistVersionInfo.NameTag, out versionVal)) { 
                    switch (versionVal) {
                        //case RedistVersionInfo.LatestVersion:
                            // always run against the latest version of the compiler
 
                        //    newPath = GetOrcasPath();
                        //    if (newPath != null && File.Exists(Path.Combine(newPath, compilerExecutable))) 
                        //        compPath = newPath; 
                        //    break;
                        case RedistVersionInfo.RedistVersion: 
                            // lock-forward to the Orcas version, if it's not available throw (we'll throw at compile time)

                            compPath = GetOrcasPath();
                            break; 
                        case RedistVersionInfo.InPlaceVersion:
                            // lock-back to the Whidbey version, no-op 
                            break; 
                        default:
                            compPath = null; 
                            break;
                    }
                }
            } 

            if (compPath == null) 
                throw new InvalidOperationException(SR.GetString(SR.CompilerNotFound, compilerExecutable)); 

            return compPath; 
        }

        private static string GetOrcasPath() {
            // Temporary until the decision is made: 
            //
            // \Windows\Microsoft.NET\Framework\v3.5.xxxxx\ or 
            // \Windows\WinFX\v3.5\ 

            string dir = Registry.GetValue(dotNetFrameworkRegistryPath, dotNetFrameworkSdkInstallKeyValueV35, null) as string; 
            if (dir != null && Directory.Exists(dir)) {
                return dir;
            }
            return null; 
        }
    } 
} 

