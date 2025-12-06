//------------------------------------------------------------------------------ 
// <copyright file="CompilationConfiguration.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Code related to the <assemblies> config section 
 *
 * Copyright (c) 1999 Microsoft Corporation 
 */
namespace System.Web.Compilation {

    using System; 
    using System.Web;
    using System.Configuration; 
    using System.Web.UI; 
    using System.Web.Configuration;
    using System.Web.Hosting; 
    using System.Web.Util;
    using System.Globalization;
    using System.Collections;
    using System.CodeDom.Compiler; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Reflection; 

 
    internal static class CompilationUtil {

        internal const string CodeDomProviderOptionPath = "system.codedom/compilers/compiler/ProviderOption/";
 
        internal static bool IsDebuggingEnabled(HttpContext context) {
            CompilationSection compConfig = RuntimeConfig.GetConfig(context).Compilation; 
            return compConfig.Debug; 
        }
 
        internal static bool IsBatchingEnabled(string configPath) {
            CompilationSection config = RuntimeConfig.GetConfig(configPath).Compilation;
            return config.Batch;
        } 

        internal static int GetRecompilationsBeforeAppRestarts() { 
            CompilationSection config = RuntimeConfig.GetAppConfig().Compilation; 
            return config.NumRecompilesBeforeAppRestart;
        } 

        internal static CompilerType GetCodeDefaultLanguageCompilerInfo() {
            return new CompilerType(typeof(Microsoft.VisualBasic.VBCodeProvider), null);
        } 

        internal static CompilerType GetDefaultLanguageCompilerInfo(CompilationSection compConfig, VirtualPath configPath) { 
            if (compConfig == null) { 
                // Get the <compilation> config object
                compConfig = RuntimeConfig.GetConfig(configPath).Compilation; 
            }

            // If no default language was specified in config, use VB
            if (compConfig.DefaultLanguage == null) { 
                return GetCodeDefaultLanguageCompilerInfo();
            } 
            else { 
                return compConfig.GetCompilerInfoFromLanguage(compConfig.DefaultLanguage);
            } 
        }

        /*
         * Return a CompilerType that a file name's extension maps to. 
         */
        internal static CompilerType GetCompilerInfoFromVirtualPath(VirtualPath virtualPath) { 
 
            // Get the extension of the source file to compile
            string extension = virtualPath.Extension; 

            // Make sure there is an extension
            if (extension.Length == 0) {
                throw new HttpException( 
                    SR.GetString(SR.Empty_extension, virtualPath));
            } 
 
            return GetCompilerInfoFromExtension(virtualPath, extension);
        } 

        /*
         * Return a CompilerType that a extension maps to.
         */ 
        private static CompilerType GetCompilerInfoFromExtension(VirtualPath configPath, string extension) {
            // Get the <compilation> config object 
            CompilationSection config = RuntimeConfig.GetConfig(configPath).Compilation; 

            return config.GetCompilerInfoFromExtension(extension, true /*throwOnFail*/); 
        }

        /*
         * Return a CompilerType that a language maps to. 
         */
        internal static CompilerType GetCompilerInfoFromLanguage(VirtualPath configPath, string language) { 
            // Get the <compilation> config object 
            CompilationSection config = RuntimeConfig.GetConfig(configPath).Compilation;
 
            return config.GetCompilerInfoFromLanguage(language);
        }

        internal static CompilerType GetCSharpCompilerInfo( 
            CompilationSection compConfig, VirtualPath configPath) {
 
            if (compConfig == null) { 
                // Get the <compilation> config object
                compConfig = RuntimeConfig.GetConfig(configPath).Compilation; 
            }

            if (compConfig.DefaultLanguage == null)
                return new CompilerType(typeof(Microsoft.CSharp.CSharpCodeProvider), null); 

            return compConfig.GetCompilerInfoFromLanguage("c#"); 
        } 

        internal static CodeSubDirectoriesCollection GetCodeSubDirectories() { 
            // Get the <compilation> config object
            CompilationSection config = RuntimeConfig.GetAppConfig().Compilation;

            CodeSubDirectoriesCollection codeSubDirectories = config.CodeSubDirectories; 

            // Make sure the config data is valid 
            if (codeSubDirectories != null) { 
                codeSubDirectories.EnsureRuntimeValidation();
            } 

            return codeSubDirectories;
        }
 
        internal static long GetRecompilationHash(CompilationSection ps)
        { 
            HashCodeCombiner recompilationHash = new HashCodeCombiner(); 
            AssemblyCollection assemblies;
            BuildProviderCollection builders; 
            CodeSubDirectoriesCollection codeSubDirs;

            // Combine items from Compilation section
            recompilationHash.AddObject(ps.Debug); 
            recompilationHash.AddObject(ps.Strict);
            recompilationHash.AddObject(ps.Explicit); 
            recompilationHash.AddObject(ps.Batch); 
            recompilationHash.AddObject(ps.BatchTimeout);
            recompilationHash.AddObject(ps.MaxBatchGeneratedFileSize); 
            recompilationHash.AddObject(ps.MaxBatchSize);
            recompilationHash.AddObject(ps.NumRecompilesBeforeAppRestart);
            recompilationHash.AddObject(ps.DefaultLanguage);
            recompilationHash.AddObject(ps.UrlLinePragmas); 
            if (ps.AssemblyPostProcessorTypeInternal != null) {
                recompilationHash.AddObject(ps.AssemblyPostProcessorTypeInternal.FullName); 
            } 

            // Combine items from Compilers collection 
            foreach (Compiler compiler in ps.Compilers) {
                recompilationHash.AddObject(compiler.Language);
                recompilationHash.AddObject(compiler.Extension);
                recompilationHash.AddObject(compiler.Type); 
                recompilationHash.AddObject(compiler.WarningLevel);
                recompilationHash.AddObject(compiler.CompilerOptions); 
            } 

            // Combine items from <expressionBuilders> section 
            foreach (System.Web.Configuration.ExpressionBuilder eb in ps.ExpressionBuilders) {
                recompilationHash.AddObject(eb.ExpressionPrefix);
                recompilationHash.AddObject(eb.Type);
            } 

            // Combine items from the Assembly collection 
            assemblies = ps.Assemblies; 

            if (assemblies.Count == 0) { 
                recompilationHash.AddObject("__clearassemblies");
            }
            else {
                foreach (AssemblyInfo ai in assemblies) { 
                    recompilationHash.AddObject(ai.Assembly);
                } 
            } 

            // Combine items from the Builders Collection 
            builders = ps.BuildProviders;

            if (builders.Count == 0) {
                recompilationHash.AddObject("__clearbuildproviders"); 
            }
            else { 
                foreach (System.Web.Configuration.BuildProvider bp in builders) { 
                    recompilationHash.AddObject(bp.Type);
                    recompilationHash.AddObject(bp.Extension); 
                }
            }

            codeSubDirs = ps.CodeSubDirectories; 
            if (codeSubDirs.Count == 0) {
                recompilationHash.AddObject("__clearcodesubdirs"); 
            } 
            else {
                foreach (CodeSubDirectory csd in codeSubDirs) { 
                    recompilationHash.AddObject(csd.DirectoryName);
                }
            }
 
            // Make sure the <system.CodeDom> section is hashed properly.
            CompilerInfo[] compilerInfoArray = CodeDomProvider.GetAllCompilerInfo(); 
            if (compilerInfoArray != null) { 
                foreach (CompilerInfo info in compilerInfoArray) {
                    // Ignore it if the type is not valid. 
                    if (!info.IsCodeDomProviderTypeValid) {
                        continue;
                    }
 
                    CompilerParameters parameters = info.CreateDefaultCompilerParameters();
                    string option = parameters.CompilerOptions; 
                    if (!String.IsNullOrEmpty(option)) { 
                        Type type = info.CodeDomProviderType;
                        if (type != null) { 
                            recompilationHash.AddObject(type.FullName);
                        }
                        // compilerOptions need to be hashed.
                        recompilationHash.AddObject(option); 
                    }
 
                    // DevDiv 62998 
                    // The tag providerOption needs to be added to the hash,
                    // as the user could switch between v2 and v3.5. 
                    if (info.CodeDomProviderType == null)
                        continue;

                    // Add a hash for each providerOption added, specific for each codeDomProvider, so that 
                    // if some codedom setting has changed, we know we have to recompile.
                    System.Collections.Generic.IDictionary<string, string> providerOptions = GetProviderOptions(info); 
                    if (providerOptions != null && providerOptions.Count > 0) { 
                        string codeDomProviderType = info.CodeDomProviderType.FullName;
                        foreach (string key in providerOptions.Keys) { 
                            string value = providerOptions[key];
                            recompilationHash.AddObject(codeDomProviderType + ":" +  key + "=" + value);
                        }
                    } 
                }
            } 
 
            return recompilationHash.CombinedHash;
        } 


        /*
         * Simple wrapper to get the Assemblies 
         */
        internal static AssemblyCollection GetAssembliesForAppLevel() { 
            // Get the CompilationSection object for the passed in config path 
            CompilationSection compilationConfiguration = RuntimeConfig.GetAppConfig().Compilation;
            return compilationConfiguration.Assemblies; 
        }

        /*
         * Look for a type by name in a collection of assemblies.  If it exists in multiple assemblies, 
         * throw an error.
         */ 
        // Assert reflection in order to call assembly.GetType() 
        [ReflectionPermission(SecurityAction.Assert, Flags=ReflectionPermissionFlag.MemberAccess)]
        internal static Type GetTypeFromAssemblies(AssemblyCollection assembliesCollection, string typeName, bool ignoreCase) { 
            if (assembliesCollection == null)
                return null;

            Type type = null; 

            foreach (AssemblyInfo info in assembliesCollection) { 
                Assembly[] assemblies = info.AssemblyInternal; 

                for (int i = 0; i < assemblies.Length; i++) { 
                    Assembly assembly = assemblies[i];
                    Type t = assembly.GetType(typeName, false /*throwOnError*/, ignoreCase);

                    if (t == null) 
                        continue;
 
                    // If we had already found a different one, it's an ambiguous type reference 
                    if (type != null && t != type) {
                        throw new HttpException(SR.GetString( 
                            SR.Ambiguous_type, typeName,
                            Util.GetAssemblySafePathFromType(type), Util.GetAssemblySafePathFromType(t)));
                    }
 
                    // Keep track of it
                    type = t; 
                } 
            }
 
            return type;
        }

 
        /*
         * Return a file provider Type that an extension maps to. 
         */ 
        internal static Type GetBuildProviderTypeFromExtension(VirtualPath configPath, string extension,
            BuildProviderAppliesTo neededFor, bool failIfUnknown) { 

            // Get the <compilation> config object
            CompilationSection config = RuntimeConfig.GetConfig(configPath).Compilation;
 
            return GetBuildProviderTypeFromExtension(config, extension, neededFor, failIfUnknown);
        } 
 
        internal static Type GetBuildProviderTypeFromExtension(CompilationSection config, string extension,
            BuildProviderAppliesTo neededFor, bool failIfUnknown) { 

            System.Web.Configuration.BuildProvider entry = (System.Web.Configuration.BuildProvider) config.BuildProviders[extension];

            Type buildProviderType = null; 
            // Never return an IgnoreFileBuildProvider/ForceCopyBuildProvider, since it's just a marker
            if (entry != null && 
                entry.TypeInternal != typeof(IgnoreFileBuildProvider) && 
                entry.TypeInternal != typeof(ForceCopyBuildProvider)) {
                buildProviderType = entry.TypeInternal; 
            }

            // In updatable precomp mode, only aspx/ascx/master web files need processing.  Ignore the rest.
            if (neededFor == BuildProviderAppliesTo.Web && 
                BuildManager.PrecompilingForUpdatableDeployment &&
                !typeof(BaseTemplateBuildProvider).IsAssignableFrom(buildProviderType)) { 
                buildProviderType = null; 
            }
 
            if (buildProviderType != null) {
                // Only return it if it applies to what it's needed for
                if ((neededFor & entry.AppliesToInternal) != 0)
                    return buildProviderType; 
            }
            // If the extension is registered as a compiler extension, use 
            // a SourceFileBuildProvider to handle it (not supported in Resources directory) 
            else if (neededFor != BuildProviderAppliesTo.Resources &&
                config.GetCompilerInfoFromExtension(extension, false /*throwOnFail*/) != null) { 
                return typeof(SourceFileBuildProvider);
            }

            if (failIfUnknown) { 
                throw new HttpException( SR.GetString(
                    SR.Unknown_buildprovider_extension, extension, neededFor.ToString())); 
            } 

            return null; 
        }

        internal static void CheckCompilerOptionsAllowed(string compilerOptions, bool config, string file, int line) {
 
            // If it's empty, we never block it
            if (String.IsNullOrEmpty(compilerOptions)) 
                return; 

            // Only allow the use of compilerOptions when we have UnmanagedCode access (ASURT 73678) 
            if (!HttpRuntime.HasUnmanagedPermission()) {
                string errorString = SR.GetString(SR.Insufficient_trust_for_attribute, "compilerOptions");

                if (config) 
                    throw new ConfigurationErrorsException(errorString, file, line);
                else 
                    throw new HttpException(errorString); 
            }
        } 

        // This is used to determine what files need to be copied, and what stub files
        // need to be created during deployment precompilation.
        // Note: createStub only applies if the method returns false. 
        internal static bool NeedToCopyFile(VirtualPath virtualPath, bool updatable, out bool createStub) {
 
            createStub = false; 

            // Get the <compilation> config object 
            CompilationSection config = RuntimeConfig.GetConfig(virtualPath).Compilation;

            string extension = virtualPath.Extension;
 
            System.Web.Configuration.BuildProvider entry = (System.Web.Configuration.BuildProvider) config.BuildProviders[extension];
 
            if (entry != null) { 
                // We only care about 'web' providers.  Everything else we treat as static
                if ((BuildProviderAppliesTo.Web & entry.AppliesToInternal) == 0) 
                    return true;

                // If the provider is a ForceCopyBuildProvider, treat as static
                if (entry.TypeInternal == typeof(ForceCopyBuildProvider)) 
                    return true;
 
                // During updatable precomp, everything needs to be copied over.  However, 
                // aspx files that use code beside will later be overwritten by modified
                // versions (see TemplateParser.CreateModifiedMainDirectiveFileIfNeeded) 
                if (entry.TypeInternal != typeof(IgnoreFileBuildProvider) &&
                    BuildManager.PrecompilingForUpdatableDeployment) {
                    return true;
                } 

                // There is a real provider, so don't copy the file.  We also need to determine whether 
                // a stub file needs to be created. 

                createStub = true; 

                // Skip the stub file for some non-requestable types
                if (entry.TypeInternal == typeof(UserControlBuildProvider) ||
                    entry.TypeInternal == typeof(MasterPageBuildProvider) || 
                    entry.TypeInternal == typeof(IgnoreFileBuildProvider)) {
                    createStub = false; 
                } 

                return false; 
            }

            // If the extension is registered as a compiler extension, don't copy
            if (config.GetCompilerInfoFromExtension(extension, false /*throwOnFail*/) != null) { 
                return false;
            } 
 
            // Skip the copying for asax and skin files, which are not static even though they
            // don't have a registered BuildProvider (but don't skip .skin files during 
            // updatable precomp).
            //
            if (StringUtil.EqualsIgnoreCase(extension, ".asax"))
                return false; 
            if (!updatable && StringUtil.EqualsIgnoreCase(extension, ThemeDirectoryCompiler.skinExtension))
                return false; 
 
            //
            // If there is no BuildProvider registered, it's a static file, and should be copied 
            //

            return true;
        } 

        internal static Type LoadTypeWithChecks(string typeName, Type requiredBaseType, Type requiredBaseType2, ConfigurationElement elem, string propertyName) { 
            Type t = ConfigUtil.GetType(typeName, propertyName, elem); 

            if (requiredBaseType2 == null) { 
                ConfigUtil.CheckAssignableType(requiredBaseType, t, elem, propertyName);
            }
            else {
                ConfigUtil.CheckAssignableType(requiredBaseType, requiredBaseType2, t, elem, propertyName); 
            }
 
            return t; 
        }
 
        // Devdiv Bug 57600
        // We need to use the constructor with ProviderOptions to get the v3.5 compiler that was possibly set in config.
        // We first check if there is any providerOptions and invoke the constructor if so.
        // Otherwise, we fall back to the default constructor. 
        internal static CodeDomProvider CreateCodeDomProvider(Type codeDomProviderType) {
            CodeDomProvider codeDomProvider = CreateCodeDomProviderWithPropertyOptions(codeDomProviderType); 
            if (codeDomProvider != null) 
                return codeDomProvider;
            return (CodeDomProvider)Activator.CreateInstance(codeDomProviderType); 
        }

        internal static CodeDomProvider CreateCodeDomProviderNonPublic(Type codeDomProviderType) {
            CodeDomProvider codeDomProvider = CreateCodeDomProviderWithPropertyOptions(codeDomProviderType); 
            if (codeDomProvider != null)
                return codeDomProvider; 
            return (CodeDomProvider)HttpRuntime.CreateNonPublicInstance(codeDomProviderType); 
        }
 
        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        private static CodeDomProvider CreateCodeDomProviderWithPropertyOptions(Type codeDomProviderType) {
            // The following resembles the code in System.CodeDom.CompilerInfo.CreateProvider
            System.Collections.Generic.IDictionary<string, string> providerOptions = GetProviderOptions(codeDomProviderType); 
            if (providerOptions != null && providerOptions.Count > 0) {
                Debug.Assert(codeDomProviderType != null, "codeDomProviderType should not be null"); 
                ConstructorInfo ci = codeDomProviderType.GetConstructor(new Type[] { typeof(System.Collections.Generic.IDictionary<string, string>) }); 
                if (ci != null) {
                    return (CodeDomProvider)ci.Invoke(new object[] { providerOptions }); 
                }
            }
            return null;
        } 

        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)] 
        internal static System.Collections.Generic.IDictionary<string, string> GetProviderOptions(Type codeDomProviderType) { 
            // Using reflection to get the property for the time being.
            // This could simply return CompilerInfo.PropertyOptions if it goes public in future. 
            CodeDomProvider provider = (CodeDomProvider) Activator.CreateInstance(codeDomProviderType);
            if (CodeDomProvider.IsDefinedExtension(provider.FileExtension)) {
                CompilerInfo ci = CodeDomProvider.GetCompilerInfo(CodeDomProvider.GetLanguageFromExtension(provider.FileExtension));
                return GetProviderOptions(ci); 
            }
            return null; 
        } 

        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)] 
        private static System.Collections.Generic.IDictionary<string, string> GetProviderOptions(CompilerInfo ci) {
            Debug.Assert(ci != null, "CompilerInfo ci should not be null");
            PropertyInfo pi = ci.GetType().GetProperty("ProviderOptions",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance); 
            if (pi != null)
                return (System.Collections.Generic.IDictionary<string, string>)pi.GetValue(ci, null); 
            return null; 
        }
 
        // This returns true only if the version is v3.5
        internal static bool IsCompilerVersion35(Type codeDomProviderType) {
            System.Collections.Generic.IDictionary<string, string> providerOptions = CompilationUtil.GetProviderOptions(codeDomProviderType);
 
            // If providerOptions is available, try processing it.
            if (providerOptions != null) { 
                string version; 
                if (providerOptions.TryGetValue("CompilerVersion", out version)) {
                    // If providerOptions contains the CompilerVersion key, it has to be either "v2.0" or "v3.5" 
                    if (version == "v2.0")
                        return false;
                    else if (version == "v3.5")
                        return true; 

                    // If it is not v2.0 or v3.5, we throw an exception. 
                    throw new ConfigurationException(SR.GetString(SR.Invalid_attribute_value, version, CodeDomProviderOptionPath + "CompilerVersion")); 
                }
            } 

            // If providerOptions is null, default to v2.0.
            // Or if providerOptions does not contain CompilerVersion, default to v2.0.
            return false; 
        }
    } 
} 

//------------------------------------------------------------------------------ 
// <copyright file="CompilationConfiguration.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Code related to the <assemblies> config section 
 *
 * Copyright (c) 1999 Microsoft Corporation 
 */
namespace System.Web.Compilation {

    using System; 
    using System.Web;
    using System.Configuration; 
    using System.Web.UI; 
    using System.Web.Configuration;
    using System.Web.Hosting; 
    using System.Web.Util;
    using System.Globalization;
    using System.Collections;
    using System.CodeDom.Compiler; 
    using System.Security;
    using System.Security.Permissions; 
    using System.Reflection; 

 
    internal static class CompilationUtil {

        internal const string CodeDomProviderOptionPath = "system.codedom/compilers/compiler/ProviderOption/";
 
        internal static bool IsDebuggingEnabled(HttpContext context) {
            CompilationSection compConfig = RuntimeConfig.GetConfig(context).Compilation; 
            return compConfig.Debug; 
        }
 
        internal static bool IsBatchingEnabled(string configPath) {
            CompilationSection config = RuntimeConfig.GetConfig(configPath).Compilation;
            return config.Batch;
        } 

        internal static int GetRecompilationsBeforeAppRestarts() { 
            CompilationSection config = RuntimeConfig.GetAppConfig().Compilation; 
            return config.NumRecompilesBeforeAppRestart;
        } 

        internal static CompilerType GetCodeDefaultLanguageCompilerInfo() {
            return new CompilerType(typeof(Microsoft.VisualBasic.VBCodeProvider), null);
        } 

        internal static CompilerType GetDefaultLanguageCompilerInfo(CompilationSection compConfig, VirtualPath configPath) { 
            if (compConfig == null) { 
                // Get the <compilation> config object
                compConfig = RuntimeConfig.GetConfig(configPath).Compilation; 
            }

            // If no default language was specified in config, use VB
            if (compConfig.DefaultLanguage == null) { 
                return GetCodeDefaultLanguageCompilerInfo();
            } 
            else { 
                return compConfig.GetCompilerInfoFromLanguage(compConfig.DefaultLanguage);
            } 
        }

        /*
         * Return a CompilerType that a file name's extension maps to. 
         */
        internal static CompilerType GetCompilerInfoFromVirtualPath(VirtualPath virtualPath) { 
 
            // Get the extension of the source file to compile
            string extension = virtualPath.Extension; 

            // Make sure there is an extension
            if (extension.Length == 0) {
                throw new HttpException( 
                    SR.GetString(SR.Empty_extension, virtualPath));
            } 
 
            return GetCompilerInfoFromExtension(virtualPath, extension);
        } 

        /*
         * Return a CompilerType that a extension maps to.
         */ 
        private static CompilerType GetCompilerInfoFromExtension(VirtualPath configPath, string extension) {
            // Get the <compilation> config object 
            CompilationSection config = RuntimeConfig.GetConfig(configPath).Compilation; 

            return config.GetCompilerInfoFromExtension(extension, true /*throwOnFail*/); 
        }

        /*
         * Return a CompilerType that a language maps to. 
         */
        internal static CompilerType GetCompilerInfoFromLanguage(VirtualPath configPath, string language) { 
            // Get the <compilation> config object 
            CompilationSection config = RuntimeConfig.GetConfig(configPath).Compilation;
 
            return config.GetCompilerInfoFromLanguage(language);
        }

        internal static CompilerType GetCSharpCompilerInfo( 
            CompilationSection compConfig, VirtualPath configPath) {
 
            if (compConfig == null) { 
                // Get the <compilation> config object
                compConfig = RuntimeConfig.GetConfig(configPath).Compilation; 
            }

            if (compConfig.DefaultLanguage == null)
                return new CompilerType(typeof(Microsoft.CSharp.CSharpCodeProvider), null); 

            return compConfig.GetCompilerInfoFromLanguage("c#"); 
        } 

        internal static CodeSubDirectoriesCollection GetCodeSubDirectories() { 
            // Get the <compilation> config object
            CompilationSection config = RuntimeConfig.GetAppConfig().Compilation;

            CodeSubDirectoriesCollection codeSubDirectories = config.CodeSubDirectories; 

            // Make sure the config data is valid 
            if (codeSubDirectories != null) { 
                codeSubDirectories.EnsureRuntimeValidation();
            } 

            return codeSubDirectories;
        }
 
        internal static long GetRecompilationHash(CompilationSection ps)
        { 
            HashCodeCombiner recompilationHash = new HashCodeCombiner(); 
            AssemblyCollection assemblies;
            BuildProviderCollection builders; 
            CodeSubDirectoriesCollection codeSubDirs;

            // Combine items from Compilation section
            recompilationHash.AddObject(ps.Debug); 
            recompilationHash.AddObject(ps.Strict);
            recompilationHash.AddObject(ps.Explicit); 
            recompilationHash.AddObject(ps.Batch); 
            recompilationHash.AddObject(ps.BatchTimeout);
            recompilationHash.AddObject(ps.MaxBatchGeneratedFileSize); 
            recompilationHash.AddObject(ps.MaxBatchSize);
            recompilationHash.AddObject(ps.NumRecompilesBeforeAppRestart);
            recompilationHash.AddObject(ps.DefaultLanguage);
            recompilationHash.AddObject(ps.UrlLinePragmas); 
            if (ps.AssemblyPostProcessorTypeInternal != null) {
                recompilationHash.AddObject(ps.AssemblyPostProcessorTypeInternal.FullName); 
            } 

            // Combine items from Compilers collection 
            foreach (Compiler compiler in ps.Compilers) {
                recompilationHash.AddObject(compiler.Language);
                recompilationHash.AddObject(compiler.Extension);
                recompilationHash.AddObject(compiler.Type); 
                recompilationHash.AddObject(compiler.WarningLevel);
                recompilationHash.AddObject(compiler.CompilerOptions); 
            } 

            // Combine items from <expressionBuilders> section 
            foreach (System.Web.Configuration.ExpressionBuilder eb in ps.ExpressionBuilders) {
                recompilationHash.AddObject(eb.ExpressionPrefix);
                recompilationHash.AddObject(eb.Type);
            } 

            // Combine items from the Assembly collection 
            assemblies = ps.Assemblies; 

            if (assemblies.Count == 0) { 
                recompilationHash.AddObject("__clearassemblies");
            }
            else {
                foreach (AssemblyInfo ai in assemblies) { 
                    recompilationHash.AddObject(ai.Assembly);
                } 
            } 

            // Combine items from the Builders Collection 
            builders = ps.BuildProviders;

            if (builders.Count == 0) {
                recompilationHash.AddObject("__clearbuildproviders"); 
            }
            else { 
                foreach (System.Web.Configuration.BuildProvider bp in builders) { 
                    recompilationHash.AddObject(bp.Type);
                    recompilationHash.AddObject(bp.Extension); 
                }
            }

            codeSubDirs = ps.CodeSubDirectories; 
            if (codeSubDirs.Count == 0) {
                recompilationHash.AddObject("__clearcodesubdirs"); 
            } 
            else {
                foreach (CodeSubDirectory csd in codeSubDirs) { 
                    recompilationHash.AddObject(csd.DirectoryName);
                }
            }
 
            // Make sure the <system.CodeDom> section is hashed properly.
            CompilerInfo[] compilerInfoArray = CodeDomProvider.GetAllCompilerInfo(); 
            if (compilerInfoArray != null) { 
                foreach (CompilerInfo info in compilerInfoArray) {
                    // Ignore it if the type is not valid. 
                    if (!info.IsCodeDomProviderTypeValid) {
                        continue;
                    }
 
                    CompilerParameters parameters = info.CreateDefaultCompilerParameters();
                    string option = parameters.CompilerOptions; 
                    if (!String.IsNullOrEmpty(option)) { 
                        Type type = info.CodeDomProviderType;
                        if (type != null) { 
                            recompilationHash.AddObject(type.FullName);
                        }
                        // compilerOptions need to be hashed.
                        recompilationHash.AddObject(option); 
                    }
 
                    // DevDiv 62998 
                    // The tag providerOption needs to be added to the hash,
                    // as the user could switch between v2 and v3.5. 
                    if (info.CodeDomProviderType == null)
                        continue;

                    // Add a hash for each providerOption added, specific for each codeDomProvider, so that 
                    // if some codedom setting has changed, we know we have to recompile.
                    System.Collections.Generic.IDictionary<string, string> providerOptions = GetProviderOptions(info); 
                    if (providerOptions != null && providerOptions.Count > 0) { 
                        string codeDomProviderType = info.CodeDomProviderType.FullName;
                        foreach (string key in providerOptions.Keys) { 
                            string value = providerOptions[key];
                            recompilationHash.AddObject(codeDomProviderType + ":" +  key + "=" + value);
                        }
                    } 
                }
            } 
 
            return recompilationHash.CombinedHash;
        } 


        /*
         * Simple wrapper to get the Assemblies 
         */
        internal static AssemblyCollection GetAssembliesForAppLevel() { 
            // Get the CompilationSection object for the passed in config path 
            CompilationSection compilationConfiguration = RuntimeConfig.GetAppConfig().Compilation;
            return compilationConfiguration.Assemblies; 
        }

        /*
         * Look for a type by name in a collection of assemblies.  If it exists in multiple assemblies, 
         * throw an error.
         */ 
        // Assert reflection in order to call assembly.GetType() 
        [ReflectionPermission(SecurityAction.Assert, Flags=ReflectionPermissionFlag.MemberAccess)]
        internal static Type GetTypeFromAssemblies(AssemblyCollection assembliesCollection, string typeName, bool ignoreCase) { 
            if (assembliesCollection == null)
                return null;

            Type type = null; 

            foreach (AssemblyInfo info in assembliesCollection) { 
                Assembly[] assemblies = info.AssemblyInternal; 

                for (int i = 0; i < assemblies.Length; i++) { 
                    Assembly assembly = assemblies[i];
                    Type t = assembly.GetType(typeName, false /*throwOnError*/, ignoreCase);

                    if (t == null) 
                        continue;
 
                    // If we had already found a different one, it's an ambiguous type reference 
                    if (type != null && t != type) {
                        throw new HttpException(SR.GetString( 
                            SR.Ambiguous_type, typeName,
                            Util.GetAssemblySafePathFromType(type), Util.GetAssemblySafePathFromType(t)));
                    }
 
                    // Keep track of it
                    type = t; 
                } 
            }
 
            return type;
        }

 
        /*
         * Return a file provider Type that an extension maps to. 
         */ 
        internal static Type GetBuildProviderTypeFromExtension(VirtualPath configPath, string extension,
            BuildProviderAppliesTo neededFor, bool failIfUnknown) { 

            // Get the <compilation> config object
            CompilationSection config = RuntimeConfig.GetConfig(configPath).Compilation;
 
            return GetBuildProviderTypeFromExtension(config, extension, neededFor, failIfUnknown);
        } 
 
        internal static Type GetBuildProviderTypeFromExtension(CompilationSection config, string extension,
            BuildProviderAppliesTo neededFor, bool failIfUnknown) { 

            System.Web.Configuration.BuildProvider entry = (System.Web.Configuration.BuildProvider) config.BuildProviders[extension];

            Type buildProviderType = null; 
            // Never return an IgnoreFileBuildProvider/ForceCopyBuildProvider, since it's just a marker
            if (entry != null && 
                entry.TypeInternal != typeof(IgnoreFileBuildProvider) && 
                entry.TypeInternal != typeof(ForceCopyBuildProvider)) {
                buildProviderType = entry.TypeInternal; 
            }

            // In updatable precomp mode, only aspx/ascx/master web files need processing.  Ignore the rest.
            if (neededFor == BuildProviderAppliesTo.Web && 
                BuildManager.PrecompilingForUpdatableDeployment &&
                !typeof(BaseTemplateBuildProvider).IsAssignableFrom(buildProviderType)) { 
                buildProviderType = null; 
            }
 
            if (buildProviderType != null) {
                // Only return it if it applies to what it's needed for
                if ((neededFor & entry.AppliesToInternal) != 0)
                    return buildProviderType; 
            }
            // If the extension is registered as a compiler extension, use 
            // a SourceFileBuildProvider to handle it (not supported in Resources directory) 
            else if (neededFor != BuildProviderAppliesTo.Resources &&
                config.GetCompilerInfoFromExtension(extension, false /*throwOnFail*/) != null) { 
                return typeof(SourceFileBuildProvider);
            }

            if (failIfUnknown) { 
                throw new HttpException( SR.GetString(
                    SR.Unknown_buildprovider_extension, extension, neededFor.ToString())); 
            } 

            return null; 
        }

        internal static void CheckCompilerOptionsAllowed(string compilerOptions, bool config, string file, int line) {
 
            // If it's empty, we never block it
            if (String.IsNullOrEmpty(compilerOptions)) 
                return; 

            // Only allow the use of compilerOptions when we have UnmanagedCode access (ASURT 73678) 
            if (!HttpRuntime.HasUnmanagedPermission()) {
                string errorString = SR.GetString(SR.Insufficient_trust_for_attribute, "compilerOptions");

                if (config) 
                    throw new ConfigurationErrorsException(errorString, file, line);
                else 
                    throw new HttpException(errorString); 
            }
        } 

        // This is used to determine what files need to be copied, and what stub files
        // need to be created during deployment precompilation.
        // Note: createStub only applies if the method returns false. 
        internal static bool NeedToCopyFile(VirtualPath virtualPath, bool updatable, out bool createStub) {
 
            createStub = false; 

            // Get the <compilation> config object 
            CompilationSection config = RuntimeConfig.GetConfig(virtualPath).Compilation;

            string extension = virtualPath.Extension;
 
            System.Web.Configuration.BuildProvider entry = (System.Web.Configuration.BuildProvider) config.BuildProviders[extension];
 
            if (entry != null) { 
                // We only care about 'web' providers.  Everything else we treat as static
                if ((BuildProviderAppliesTo.Web & entry.AppliesToInternal) == 0) 
                    return true;

                // If the provider is a ForceCopyBuildProvider, treat as static
                if (entry.TypeInternal == typeof(ForceCopyBuildProvider)) 
                    return true;
 
                // During updatable precomp, everything needs to be copied over.  However, 
                // aspx files that use code beside will later be overwritten by modified
                // versions (see TemplateParser.CreateModifiedMainDirectiveFileIfNeeded) 
                if (entry.TypeInternal != typeof(IgnoreFileBuildProvider) &&
                    BuildManager.PrecompilingForUpdatableDeployment) {
                    return true;
                } 

                // There is a real provider, so don't copy the file.  We also need to determine whether 
                // a stub file needs to be created. 

                createStub = true; 

                // Skip the stub file for some non-requestable types
                if (entry.TypeInternal == typeof(UserControlBuildProvider) ||
                    entry.TypeInternal == typeof(MasterPageBuildProvider) || 
                    entry.TypeInternal == typeof(IgnoreFileBuildProvider)) {
                    createStub = false; 
                } 

                return false; 
            }

            // If the extension is registered as a compiler extension, don't copy
            if (config.GetCompilerInfoFromExtension(extension, false /*throwOnFail*/) != null) { 
                return false;
            } 
 
            // Skip the copying for asax and skin files, which are not static even though they
            // don't have a registered BuildProvider (but don't skip .skin files during 
            // updatable precomp).
            //
            if (StringUtil.EqualsIgnoreCase(extension, ".asax"))
                return false; 
            if (!updatable && StringUtil.EqualsIgnoreCase(extension, ThemeDirectoryCompiler.skinExtension))
                return false; 
 
            //
            // If there is no BuildProvider registered, it's a static file, and should be copied 
            //

            return true;
        } 

        internal static Type LoadTypeWithChecks(string typeName, Type requiredBaseType, Type requiredBaseType2, ConfigurationElement elem, string propertyName) { 
            Type t = ConfigUtil.GetType(typeName, propertyName, elem); 

            if (requiredBaseType2 == null) { 
                ConfigUtil.CheckAssignableType(requiredBaseType, t, elem, propertyName);
            }
            else {
                ConfigUtil.CheckAssignableType(requiredBaseType, requiredBaseType2, t, elem, propertyName); 
            }
 
            return t; 
        }
 
        // Devdiv Bug 57600
        // We need to use the constructor with ProviderOptions to get the v3.5 compiler that was possibly set in config.
        // We first check if there is any providerOptions and invoke the constructor if so.
        // Otherwise, we fall back to the default constructor. 
        internal static CodeDomProvider CreateCodeDomProvider(Type codeDomProviderType) {
            CodeDomProvider codeDomProvider = CreateCodeDomProviderWithPropertyOptions(codeDomProviderType); 
            if (codeDomProvider != null) 
                return codeDomProvider;
            return (CodeDomProvider)Activator.CreateInstance(codeDomProviderType); 
        }

        internal static CodeDomProvider CreateCodeDomProviderNonPublic(Type codeDomProviderType) {
            CodeDomProvider codeDomProvider = CreateCodeDomProviderWithPropertyOptions(codeDomProviderType); 
            if (codeDomProvider != null)
                return codeDomProvider; 
            return (CodeDomProvider)HttpRuntime.CreateNonPublicInstance(codeDomProviderType); 
        }
 
        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        private static CodeDomProvider CreateCodeDomProviderWithPropertyOptions(Type codeDomProviderType) {
            // The following resembles the code in System.CodeDom.CompilerInfo.CreateProvider
            System.Collections.Generic.IDictionary<string, string> providerOptions = GetProviderOptions(codeDomProviderType); 
            if (providerOptions != null && providerOptions.Count > 0) {
                Debug.Assert(codeDomProviderType != null, "codeDomProviderType should not be null"); 
                ConstructorInfo ci = codeDomProviderType.GetConstructor(new Type[] { typeof(System.Collections.Generic.IDictionary<string, string>) }); 
                if (ci != null) {
                    return (CodeDomProvider)ci.Invoke(new object[] { providerOptions }); 
                }
            }
            return null;
        } 

        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)] 
        internal static System.Collections.Generic.IDictionary<string, string> GetProviderOptions(Type codeDomProviderType) { 
            // Using reflection to get the property for the time being.
            // This could simply return CompilerInfo.PropertyOptions if it goes public in future. 
            CodeDomProvider provider = (CodeDomProvider) Activator.CreateInstance(codeDomProviderType);
            if (CodeDomProvider.IsDefinedExtension(provider.FileExtension)) {
                CompilerInfo ci = CodeDomProvider.GetCompilerInfo(CodeDomProvider.GetLanguageFromExtension(provider.FileExtension));
                return GetProviderOptions(ci); 
            }
            return null; 
        } 

        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)] 
        private static System.Collections.Generic.IDictionary<string, string> GetProviderOptions(CompilerInfo ci) {
            Debug.Assert(ci != null, "CompilerInfo ci should not be null");
            PropertyInfo pi = ci.GetType().GetProperty("ProviderOptions",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance); 
            if (pi != null)
                return (System.Collections.Generic.IDictionary<string, string>)pi.GetValue(ci, null); 
            return null; 
        }
 
        // This returns true only if the version is v3.5
        internal static bool IsCompilerVersion35(Type codeDomProviderType) {
            System.Collections.Generic.IDictionary<string, string> providerOptions = CompilationUtil.GetProviderOptions(codeDomProviderType);
 
            // If providerOptions is available, try processing it.
            if (providerOptions != null) { 
                string version; 
                if (providerOptions.TryGetValue("CompilerVersion", out version)) {
                    // If providerOptions contains the CompilerVersion key, it has to be either "v2.0" or "v3.5" 
                    if (version == "v2.0")
                        return false;
                    else if (version == "v3.5")
                        return true; 

                    // If it is not v2.0 or v3.5, we throw an exception. 
                    throw new ConfigurationException(SR.GetString(SR.Invalid_attribute_value, version, CodeDomProviderOptionPath + "CompilerVersion")); 
                }
            } 

            // If providerOptions is null, default to v2.0.
            // Or if providerOptions does not contain CompilerVersion, default to v2.0.
            return false; 
        }
    } 
} 

