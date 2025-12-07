//------------------------------------------------------------------------------ 
// <copyright file="BuildResultCache.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

 
 
/*********************************
 
BuildResultCache
    MemoryBuildResultCache
    DiskBuildResultCache
        StandardDiskBuildResultCache 
        PrecompBaseDiskBuildResultCache
            PrecompilerDiskBuildResultCache 
            PrecompiledSiteDiskBuildResultCache 

**********************************/ 

namespace System.Web.Compilation {

using System; 
using System.IO;
using System.Collections; 
using System.Globalization; 
using System.Text;
using System.Threading; 
using System.Reflection;
using System.Web.Hosting;
using System.Web.Util;
using System.Web.Caching; 
using System.Web.UI;
 
internal abstract class BuildResultCache { 
    internal BuildResult GetBuildResult(string cacheKey) {
        return GetBuildResult(cacheKey, null /*virtualPath*/, 0 /*hashCode*/); 
    }

    internal abstract BuildResult GetBuildResult(string cacheKey, VirtualPath virtualPath, long hashCode);
 
    internal void CacheBuildResult(string cacheKey, BuildResult result, DateTime utcStart) {
        CacheBuildResult(cacheKey, result, 0 /*hashCode*/, utcStart); 
    } 

    internal abstract void CacheBuildResult(string cacheKey, BuildResult result, 
        long hashCode, DateTime utcStart);

    internal static string GetAssemblyCacheKey(string assemblyPath) {
        string assemblyName = Util.GetAssemblyNameFromFileName(Path.GetFileName(assemblyPath)); 
        return GetAssemblyCacheKeyFromName(assemblyName);
    } 
 
    internal static string GetAssemblyCacheKey(Assembly assembly) {
        Debug.Assert(!assembly.GlobalAssemblyCache); 
        return GetAssemblyCacheKeyFromName(assembly.GetName().Name);
    }

    internal static string GetAssemblyCacheKeyFromName(string assemblyName) { 
        Debug.Assert(StringUtil.StringStartsWith(assemblyName, BuildManager.AssemblyNamePrefix));
        return CacheInternal.PrefixAssemblyPath + assemblyName.ToLowerInvariant(); 
    } 

} 

internal class MemoryBuildResultCache: BuildResultCache {

    private CacheInternal _cache; 
    private CacheItemRemovedCallback _onRemoveCallback;
 
    // The keys are simple assembly names 
    // The values are ArrayLists containing the simple names of assemblies that depend on it
    private Hashtable _dependentAssemblies = new Hashtable(); 

    internal MemoryBuildResultCache(CacheInternal cache) {
        _cache = cache;
 
        // Register an AssemblyLoad event
        AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(OnAssemblyLoad); 
    } 

    private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args) { 
        Assembly a = args.LoadedAssembly;

        // Ignore GAC assemblies
        if (a.GlobalAssemblyCache) 
            return;
 
        // Ignore assemblies that don't start with our prefix 
        string name = a.GetName().Name;
        if (!StringUtil.StringStartsWith(name, BuildManager.AssemblyNamePrefix)) 
            return;

        // Go through all the assemblies it references
        foreach (AssemblyName assemblyName in a.GetReferencedAssemblies()) { 

            // Ignore references that don't start with our prefix 
            if (!StringUtil.StringStartsWith(assemblyName.Name, BuildManager.AssemblyNamePrefix)) 
                continue;
 
            lock (_dependentAssemblies) {
                // Check whether we already have an ArrayList for this reference
                ArrayList dependentList = _dependentAssemblies[assemblyName.Name] as ArrayList;
                if (dependentList == null) { 
                    // If not, create one and add it to the hashtable
                    dependentList = new ArrayList(); 
                    _dependentAssemblies[assemblyName.Name] = dependentList; 
                }
 
                // Add the assembly that just got loaded as a dependent
                Debug.Assert(!dependentList.Contains(name));
                dependentList.Add(name);
            } 
        }
    } 
 
    internal override BuildResult GetBuildResult(string cacheKey, VirtualPath virtualPath, long hashCode) {
        Debug.Trace("BuildResultCache", "Looking for '" + cacheKey + "' in the memory cache"); 

        string key = GetMemoryCacheKey(cacheKey);
        BuildResult result = (BuildResult) _cache.Get(key);
 
        // Not found in the cache
        if (result == null) { 
            Debug.Trace("BuildResultCache", "'" + cacheKey + "' was not found in the memory cache"); 
            return null;
        } 

        // We found it in the cache, but is it up to date.  First, if it uses a CacheDependency,
        // it must be up to date (this is the default case when using MapPathBasedVirtualPathProvider).
        // If not, then we need to explicitely check that it's up to date (more expensive) 
        if (!result.UsesCacheDependency && !result.IsUpToDate(virtualPath)) {
 
            Debug.Trace("BuildResultCache", "'" + cacheKey + "' was found but is out of date"); 

            // Remove it from the cache 
            _cache.Remove(key);

            Debug.Assert(_cache.Get(key) == null);
 
            return null;
        } 
 
        Debug.Trace("BuildResultCache", "'" + cacheKey + "' was found in the memory cache");
 
        // It's up to date: return it
        return result;
    }
 
    internal override void CacheBuildResult(string cacheKey, BuildResult result,
        long hashCode, DateTime utcStart) { 
 
        ICollection virtualDependencies = result.VirtualPathDependencies;
 
        Debug.Trace("BuildResultCache", "Adding cache " + cacheKey + " in the memory cache");

        CacheDependency cacheDependency = null;
 
        if (virtualDependencies != null) {
            cacheDependency = result.VirtualPath.GetCacheDependency(virtualDependencies, utcStart); 
 
            // If we got a cache dependency, remember that in the BuildResult
            if (cacheDependency != null) 
                result.UsesCacheDependency = true;
        }

        // If it should not be cached to memory, leave it alone 
        if (!result.CacheToMemory) {
            return; 
        } 

        BuildResultCompiledAssemblyBase compiledResult = result as BuildResultCompiledAssemblyBase; 
        if (compiledResult != null && compiledResult.ResultAssembly != null && !compiledResult.UsesExistingAssembly) {

            // Insert a new cache entry using the assembly path as the key
            string assemblyKey = GetAssemblyCacheKey(compiledResult.ResultAssembly); 
            Assembly a = (Assembly)_cache.Get(assemblyKey);
            if (a == null) { 
                Debug.Trace("BuildResultCache", "Adding marker cache entry " + compiledResult.ResultAssembly); 
                // VSWhidbey 500049 - add as NotRemovable to prevent the assembly from being prematurely deleted
                _cache.UtcInsert(assemblyKey, compiledResult.ResultAssembly, 
                                 null,
                                 Cache.NoAbsoluteExpiration,
                                 Cache.NoSlidingExpiration,
                                 CacheItemPriority.NotRemovable, 
                                 null);
            } 
            else { 
                Debug.Assert(a == compiledResult.ResultAssembly);
            } 

            // Now create a dependency based on that key. This way, by removing that key, we are able to
            // remove all the pages that live in that assembly from the cache.
            CacheDependency assemblyCacheDependency = new CacheDependency(0, null, new string[] { assemblyKey }); 

            if (cacheDependency != null) { 
                // We can't share the same CacheDependency, since we don't want the UtcStart 
                // behavior for the assembly.  Use an Aggregate to put the two together.
                AggregateCacheDependency tmpDependency = new AggregateCacheDependency(); 
                tmpDependency.Add(new CacheDependency[] { cacheDependency, assemblyCacheDependency });
                cacheDependency = tmpDependency;
            }
            else { 
                cacheDependency = assemblyCacheDependency;
            } 
        } 

        string key = GetMemoryCacheKey(cacheKey); 

        // Only allow the cache item to expire if the result can be unloaded.  Otherwise,
        // we may as well cache it forever (e.g. for Assemblies and Types).
        CacheItemPriority cachePriority; 
        if (result.IsUnloadable)
            cachePriority = CacheItemPriority.Default; 
        else 
            cachePriority = CacheItemPriority.NotRemovable;
 
        CacheItemRemovedCallback onRemoveCallback = null;

        // If the appdomain needs to be shut down when the item becomes invalid, register
        // a callback to do the shutdown. 
        if (result.ShutdownAppDomainOnChange || result is BuildResultCompiledAssemblyBase) {
 
            // Create the delegate on demand 
            if (_onRemoveCallback == null)
                _onRemoveCallback = new CacheItemRemovedCallback(OnCacheItemRemoved); 

            onRemoveCallback = _onRemoveCallback;
        }
 
        _cache.UtcInsert(key, result, cacheDependency,
            result.MemoryCacheExpiration, 
            result.MemoryCacheSlidingExpiration, 
            cachePriority,
            onRemoveCallback); 
    }

    private void OnCacheItemRemoved(string key, object value, CacheItemRemovedReason reason) {
 
        // Only handle case when the dependency is removed.
        if (reason == CacheItemRemovedReason.DependencyChanged) { 
            Debug.Trace("BuildResultCache", "OnCacheItemRemoved Key=" + key); 

            // Remove the assembly if a buildresult becomes obsolete 
            if (HostingEnvironment.ShutdownInitiated) {
                // VSWhidbey 564168
                // We still need to mark the affected files and dependencies for later deletion so that we do not build up unused assemblies.
                RemoveAssemblyAndCleanupDependenciesShuttingDown(value as BuildResultCompiledAssembly); 
            }
            else { 
 
                RemoveAssemblyAndCleanupDependencies(value as BuildResultCompiledAssemblyBase);
 
                // Shutdown the appdomain if the buildresult requires it.
                if (((BuildResult)value).ShutdownAppDomainOnChange) {
                    HttpRuntime.SetShutdownReason(ApplicationShutdownReason.BuildManagerChange, "BuildResult change, cache key=" + key);
                    HostingEnvironment.InitiateShutdown(); 
                }
            } 
        } 
    }
 
    // Since we are shutting down, we will just create the .delete files to mark the files for deletion,
    // and not try to get the compilation lock.
    internal void RemoveAssemblyAndCleanupDependenciesShuttingDown(BuildResultCompiledAssemblyBase compiledResult) {
        if (compiledResult == null) 
            return;
 
        if (compiledResult != null && compiledResult.ResultAssembly != null && !compiledResult.UsesExistingAssembly) { 
            string assemblyName = compiledResult.ResultAssembly.GetName().Name;
            lock (_dependentAssemblies) { 
                RemoveAssemblyAndCleanupDependenciesNoLock(assemblyName);
            }
        }
    } 

 
    internal void RemoveAssemblyAndCleanupDependencies(BuildResultCompiledAssemblyBase compiledResult) { 
        if (compiledResult == null)
            return; 

        if (compiledResult != null && compiledResult.ResultAssembly != null && !compiledResult.UsesExistingAssembly) {
            RemoveAssemblyAndCleanupDependencies(compiledResult.ResultAssembly.GetName().Name);
        } 
    }
 
    private void RemoveAssemblyAndCleanupDependencies(string assemblyName) { 
        bool gotLock = false;
 
        try {
            // Grab the compilation mutex, since we will remove cached build result
            CompilationLock.GetLock(ref gotLock);
 
            // Protect the dependent assemblies table, as it's accessed/modified in the recursion
            lock (_dependentAssemblies) { 
                RemoveAssemblyAndCleanupDependenciesNoLock(assemblyName); 
            }
        } 
        finally {
            // Always release the mutex if we had taken it
            if (gotLock) {
                CompilationLock.ReleaseLock(); 
            }
            DiskBuildResultCache.ShutDownAppDomainIfRequired(); 
        } 
    }
 
    private void RemoveAssemblyAndCleanupDependenciesNoLock(string assemblyName) {

        // If we have no cache entry for this assembly, there is nothing to do
        string cacheKey = GetAssemblyCacheKeyFromName(assemblyName); 
        Assembly assembly = (Assembly)_cache[cacheKey];
        if (assembly == null) 
            return; 

        // Get the physical path to the assembly 
        String assemblyPath = Util.GetAssemblyCodeBase(assembly);

        Debug.Trace("BuildResultCache", "removing cacheKey for assembly " + assemblyPath + " because of dependency change");
 
        // Remove the cache entry in order to kick out all the pages that are in that batch
        _cache.Remove(cacheKey); 
 
        // Now call recursively on all the dependent assemblies (VSWhidbey 577593)
        ICollection dependentAssemblies = _dependentAssemblies[assemblyName] as ICollection; 
        if (dependentAssemblies != null) {
            foreach (string dependentAssemblyName in dependentAssemblies) {
                RemoveAssemblyAndCleanupDependenciesNoLock(dependentAssemblyName);
            } 

            // We can now remove this assembly from the hashtable 
            _dependentAssemblies.Remove(cacheKey); 
        }
 
        // Remove (or rename) the DLL
        DiskBuildResultCache.RemoveAssembly(new FileInfo(assemblyPath));
    }
 
    private static string GetMemoryCacheKey(string cacheKey) {
 
        // Prepend something to it to avoid conflicts with other cache users 
        return CacheInternal.PrefixMemoryBuildResult + cacheKey;
    } 
}

internal abstract class DiskBuildResultCache: BuildResultCache {
 
    protected const string preservationFileExtension = ".compiled";
 
    protected string _cacheDir; 

    private static int s_recompilations; 
    private static int s_maxRecompilations = -1;

    private static bool s_inUseAssemblyWasDeleted;
 
    protected const string dotDelete = ".delete";
 
    private static bool s_shutdownRequired; 

    internal DiskBuildResultCache(string cacheDir) { 
        _cacheDir = cacheDir;

        // Find out how many recompilations we allow before restarting the appdomain
        if (s_maxRecompilations < 0) 
            s_maxRecompilations = CompilationUtil.GetRecompilationsBeforeAppRestarts();
    } 
 
    protected void EnsureDiskCacheDirectoryCreated() {
 
        // Create the disk cache directory if it's not already there
        if (!FileUtil.DirectoryExists(_cacheDir)) {
            try {
                Directory.CreateDirectory(_cacheDir); 
            }
            catch (IOException e) { 
                throw new HttpException(SR.GetString( 
                    SR.Failed_to_create_temp_dir, HttpRuntime.GetSafePath(_cacheDir)), e);
            } 
        }
    }

    internal override BuildResult GetBuildResult(string cacheKey, VirtualPath virtualPath, long hashCode) { 

        Debug.Trace("BuildResultCache", "Looking for '" + cacheKey + "' in the disk cache"); 
 
        string preservationFile = GetPreservedDataFileName(cacheKey);
 
        PreservationFileReader pfr = new PreservationFileReader(this, PrecompilationMode);

        // Create the BuildResult from the preservation file
        BuildResult result = pfr.ReadBuildResultFromFile(virtualPath, preservationFile, hashCode); 

        if (result != null) 
            Debug.Trace("BuildResultCache", "'" + cacheKey + "' was found in the disk cache"); 
        else
            Debug.Trace("BuildResultCache", "'" + cacheKey + "' was not found in the disk cache"); 

        return result;
    }
 
    internal override void CacheBuildResult(string cacheKey, BuildResult result,
        long hashCode, DateTime utcStart) { 
 
        // If it should not be cached to disk, leave it alone
        if (!result.CacheToDisk) 
            return;

        // VSWhidbey 564168 don't save to disk if already shutting down, otherwise we might
        // be persisting assembly that was compiled with obsolete references. 
        // Since we are shutting down and not creating any cache, delete the compiled result
        // as it will not be used in future. 
        if (HostingEnvironment.ShutdownInitiated) { 
            BuildResultCompiledAssemblyBase compiledResult = result as BuildResultCompiledAssemblyBase;
            if (compiledResult != null) 
                MarkAssemblyAndRelatedFilesForDeletion(compiledResult.ResultAssembly.GetName().Name);
            return;
        }
 
        string preservationFile = GetPreservedDataFileName(cacheKey);
        PreservationFileWriter pfw = new PreservationFileWriter(PrecompilationMode); 
 
        pfw.SaveBuildResultToFile(preservationFile, result, hashCode);
    } 

    private void MarkAssemblyAndRelatedFilesForDeletion(string assemblyName) {
        DirectoryInfo directory = new DirectoryInfo(_cacheDir);
        // Get rid of the prefix "App_web", since related files don't have it 
        string baseName = assemblyName.Substring(BuildManager.WebAssemblyNamePrefix.Length);
        FileInfo[] files = directory.GetFiles("*" + baseName + ".*"); 
        foreach (FileInfo f in files) 
            CreateDotDeleteFile(f);
    } 

    /*
     * Return the physical full path to the preservation data file
     */ 
    private string GetPreservedDataFileName(string cacheKey) {
 
        // Make sure the key doesn't contain any invalid file name chars (VSWhidbey 263142) 
        cacheKey = Util.MakeValidFileName(cacheKey);
 
        cacheKey = Path.Combine(_cacheDir, cacheKey);

        cacheKey = FileUtil.TruncatePathIfNeeded(cacheKey, 9 /*length of ".compiled"*/);
 
        // Use a ".compiled" extension for the preservation file
        return cacheKey + preservationFileExtension; 
    } 

    protected virtual bool PrecompilationMode { get { return false; } } 

    internal static bool InUseAssemblyWasDeleted { get { return s_inUseAssemblyWasDeleted; } }
    internal static void ResetAssemblyDeleted() { s_inUseAssemblyWasDeleted = false; }
 
    /*
     * Delete an assembly and all its related files.  The assembly is typically named 
     * something like ASPNET.jnw_y10n.dll, while related files are simply jnw_y10n.*. 
     */
    internal virtual void RemoveAssemblyAndRelatedFiles(string assemblyName) { 

        Debug.Trace("DiskBuildResultCache", "RemoveAssemblyAndRelatedFiles(" + assemblyName + ")");

        // If the name doesn't start with the prefix, the cleanup code doesn't apply 
        if (!assemblyName.StartsWith(BuildManager.AssemblyNamePrefix, StringComparison.Ordinal)) {
            // 
            Debug.Assert(false); 
            return;
        } 

        // Get rid of the prefix, since related files don't have it
        string baseName = assemblyName.Substring(BuildManager.WebAssemblyNamePrefix.Length);
 
        bool gotLock = false;
        try { 
            // Grab the compilation mutex, since we will remove generated assembly 
            CompilationLock.GetLock(ref gotLock);
 
            DirectoryInfo directory = new DirectoryInfo(_cacheDir);

            // Find all the files that contain the base name
            FileInfo[] files = directory.GetFiles("*" + baseName + ".*"); 
            foreach (FileInfo f in files) {
 
                if (f.Extension == ".dll") { 
                    // Notify existing buildresults that result assembly will be removed.
                    // This is required otherwise new components can be compiled 
                    // with obsolete build results whose assembly has been removed.
                    string assemblyKey = GetAssemblyCacheKey(f.FullName);
                    HttpRuntime.CacheInternal.Remove(assemblyKey);
 
                    // Remove the assembly
                    RemoveAssembly(f); 
 
                    // Also, remove satellite assemblies that may be associated with it
                    StandardDiskBuildResultCache.RemoveSatelliteAssemblies(assemblyName); 
                }
                else if (f.Extension == dotDelete) {
                    CheckAndRemoveDotDeleteFile(f);
                } 
                else {
                    // Remove the file, or if not possible, rename it, so it'll get 
                    // cleaned up next time by RemoveOldTempFiles() 

                    TryDeleteFile(f); 
                }
            }
        }
        finally { 
            // Always release the mutex if we had taken it
            if (gotLock) { 
                CompilationLock.ReleaseLock(); 
            }
            DiskBuildResultCache.ShutDownAppDomainIfRequired(); 
        }
    }

    internal static void RemoveAssembly(FileInfo f) { 

        // If we are shutting down, just create the .delete file and exit quickly. 
        if (HostingEnvironment.ShutdownInitiated) { 
            CreateDotDeleteFile(f);
            return; 
        }

        // VSWhidbey 564168 / Visual Studio QFE 4710
        // The assembly could still be referenced and needed for compilation in some cases. 
        // Thus, if we cannot delete it, we create an empty .delete file,
        // so that both will be later removed by RemoveOldTempFiles. 
 
        // If the file is already marked for deletion, we simply return, so that
        // we do not double count it in s_recompilations. 
        if (HasDotDeleteFile(f.FullName))
            return;

        if (TryDeleteFile(f)) 
            return;
 
        // It had to be renamed, so increment the recompilations count, 
        // and restart the appdomain if it reaches the limit
 
        Debug.Trace("DiskBuildResultCache", "RemoveAssembly: " + f.Name + " was renamed");

        if (++s_recompilations == s_maxRecompilations) {
            s_shutdownRequired = true; 
        }
 
        // Remember the fact that we just invalidated an assembly, which can cause 
        // other BuildResults to become invalid as a side effect (VSWhidbey 269297)
        s_inUseAssemblyWasDeleted = true; 
    }

    static internal void ShutDownAppDomainIfRequired() {
        // VSWhidbey 610631 Stress Failure: Worker process throws exceptions while unloading app domain and re-tries over and over 
        // It is possible for a deadlock to happen when locks on ApplicationManager and the CompilationMutex
        // are acquired in different orders in multiple threads. 
        // Thus, since ShutdownAppDomain acquires a lock on ApplicationManager, we always release the CompilationMutex 
        // before calling ShutdownAppDomain, in case another thread has acquired the lock on ApplicationManager and
        // is waiting on the CompilationMutex. 

        if (s_shutdownRequired)
            HttpRuntime.ShutdownAppDomain(ApplicationShutdownReason.MaxRecompilationsReached,
                "Recompilation limit of " + s_maxRecompilations + " reached"); 
    }
 
 
    internal static bool TryDeleteFile(string s) {
        return TryDeleteFile(new FileInfo(s)); 
    }

    // Returns true if we are able to delete the file. Otherwise, creates a .delete file and returns false.
    internal static bool TryDeleteFile(FileInfo f) { 
        if (f.Extension == dotDelete)
            return CheckAndRemoveDotDeleteFile(f); 
 
        try {
            f.Delete(); 
            Debug.Trace("DiskBuildResultCache", "TryDeleteFile removed " + f.Name);
            return true;
        }
        catch { } 

        CreateDotDeleteFile(f); 
        return false; 
    }
 
    // Checks if the file is .delete. If it is, check if the associated base file is still around.
    // If associated base file is around, try to delete it. If success, delete the .delete.
    // Returns true only if both base file and .delete are removed.
    internal static bool CheckAndRemoveDotDeleteFile(FileInfo f) { 
        if (f.Extension != dotDelete)
            return false; 
 
        string baseName = Path.GetDirectoryName(f.FullName) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(f.FullName);
        if (FileUtil.FileExists(baseName)) { 
            try {
                File.Delete(baseName);
                Debug.Trace("DiskBuildResultCache", "CheckAndRemoveDotDeleteFile deleted " + baseName);
            } 
            catch {
                return false; 
            } 
        }
 
        try {
            f.Delete();
            Debug.Trace("DiskBuildResultCache", "CheckAndRemoveDotDeleteFile deleted " + f.Name);
        } 
        catch { }
 
        return true; 
    }
 
    internal static bool HasDotDeleteFile(string s) {
        return File.Exists(s + dotDelete);
    }
 
    private static void CreateDotDeleteFile(FileInfo f) {
        if (f.Extension == dotDelete) 
            return; 
        string newName = f.FullName + dotDelete;
        if (!File.Exists(newName)) { 
            try {
                (new StreamWriter(newName)).Close();
                Debug.Trace("DiskBuildResultCache", "CreateDotDeleteFile succeeded: " + newName);
            } 
            catch {
                Debug.Trace("DiskBuildResultCache", "CreateDotDeleteFile failed: " + newName); 
            } // If we fail the .delete probably just got created by another process. 
        }
    } 

}

internal class StandardDiskBuildResultCache: DiskBuildResultCache { 

    private const string fusionCacheDirectoryName = "assembly"; 
    private const string webHashDirectoryName = "hash"; 

    private static ArrayList _satelliteDirectories; 

    internal StandardDiskBuildResultCache(string cacheDir)
        : base(cacheDir) {
 
        Debug.Assert(cacheDir == HttpRuntime.CodegenDirInternal);
 
        EnsureDiskCacheDirectoryCreated(); 

        FindSatelliteDirectories(); 
    }

    private string GetSpecialFilesCombinedHashFileName() {
        return BuildManager.WebHashFilePath; 
    }
 
    /* 
     * Return the combined hash that was preserved to file.  Return 0 if not valid.
     */ 
    internal long GetPreservedSpecialFilesCombinedHash() {

        string fileName = GetSpecialFilesCombinedHashFileName();
 
        if (!FileUtil.FileExists(fileName))
            return 0; 
 
        try {
            string s = Util.StringFromFile(fileName); 
            return Int64.Parse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        }
        catch {
            // If anything went wrong (file not found, or bad format), return 0 
            return 0;
        } 
    } 

    /* 
     * Preserve the combined hash of the special files to a file.
     */
    internal void SavePreservedSpecialFilesCombinedHash(long hash) {
 
        Debug.Assert(hash != 0, "SavePreservedSpecialFilesCombinedHash: hash != 0");
        StreamWriter writer = null; 
 
        try {
            String hashFilePath = GetSpecialFilesCombinedHashFileName(); 
            String hashDirPath = Path.GetDirectoryName(hashFilePath);

            // Create the hashweb directory if needed
            if (!FileUtil.DirectoryExists(hashDirPath)) { 
                Directory.CreateDirectory(hashDirPath);
            } 
 
            writer = new StreamWriter(hashFilePath, false, Encoding.UTF8);
            writer.Write(hash.ToString("x", CultureInfo.InvariantCulture)); 
        }
        finally {
            if (writer != null)
                writer.Close(); 
        }
    } 
 
    private void FindSatelliteDirectories() {
 
        Debug.Assert(_satelliteDirectories == null);

        //
        // Look for all the subdirectories of the codegen dir that look like 
        // satellite assemblies dirs, and keep track of them
        // 
 
        string[] subDirs = Directory.GetDirectories(_cacheDir);
 
        foreach (string subDir in subDirs) {
            string subDirName = Path.GetFileNameWithoutExtension(subDir);

            // Skip the fusion cache, since it's definitely not a culture (VSWhidbey 327716) 
            if (subDirName == fusionCacheDirectoryName)
                continue; 
 
            // Skip the "hash" folder
            if (subDirName == webHashDirectoryName) 
                continue;

            if (Util.IsCultureName(subDirName)) {
                if (_satelliteDirectories == null) 
                    _satelliteDirectories = new ArrayList();
 
                _satelliteDirectories.Add(Path.Combine(_cacheDir, subDir)); 
            }
        } 
    }

    internal static void RemoveSatelliteAssemblies(string baseAssemblyName) {
 
        if (_satelliteDirectories == null)
            return; 
 
        //
        // If any satellite directory contains a satellite assembly that's 
        // for the passed in assembly name, delete it.
        //

        string satelliteAssemblyName = baseAssemblyName + ".resources"; 

        foreach (string satelliteDir in _satelliteDirectories) { 
            string fullAssemblyPath = Path.Combine(satelliteDir, satelliteAssemblyName); 

            // Delete the DLL and PDB 
            Util.DeleteFileIfExistsNoException(fullAssemblyPath + ".dll");
            Util.DeleteFileIfExistsNoException(fullAssemblyPath + ".pdb");
        }
    } 

    /* 
     * Delete all temporary files from the codegen directory (e.g. source files, ...) 
     */
    internal void RemoveOldTempFiles() { 
        Debug.Trace("BuildResultCache", "Deleting old temporary files from " + _cacheDir);

        string codegen = _cacheDir + "\\";
 
        // Go through all the files in the codegen dir
        foreach (FileData fileData in FileEnumerator.Create(codegen)) { 
 
            // Skip directories
            if (fileData.IsDirectory) continue; 

            // If it has a known extension, skip it
            string ext = Path.GetExtension(fileData.Name);
            if (ext == ".dll" || ext == ".pdb" || ext == ".web" || ext == ".ccu" || ext == preservationFileExtension) { 
                continue;
            } 
 
            // .delete files need to be removed.
            if (ext != dotDelete) { 
                // Don't delete the temp file if it's named after a dll that's still around
                // since it could still be useful for debugging.
                // Note that we can't use GetFileNameWithoutExtension here because
                // some of the files are named 5hvoxl6v.0.cs, and it would return 
                // 5hvoxl6v.0 instead of just 5hvoxl6v
                int periodIndex = fileData.Name.LastIndexOf('.'); 
                if (periodIndex > 0) { 
                    string baseName = fileData.Name.Substring(0, periodIndex);
 
                    int secondPeriodIndex = baseName.LastIndexOf('.');
                    if (secondPeriodIndex > 0) {
                        baseName = baseName.Substring(0, secondPeriodIndex);
                    } 

                    // Generated source files uses assemblyname as prefix so we should keep them. 
                    if (FileUtil.FileExists(codegen + baseName + ".dll")) 
                        continue;
 
                    // other generated files, such as .cmdline, .err and .out need to add the
                    // WebAssemblyNamePrefix, since they do not use the assembly name as prefix.
                    if (FileUtil.FileExists(codegen + BuildManager.WebAssemblyNamePrefix + baseName + ".dll"))
                        continue; 
                }
            } 
            else { 
                // Additional logic for VSWhidbey 564168 / Visual Studio QFE 4710.
                // Delete both original .dll and .delete if possible 
                DiskBuildResultCache.CheckAndRemoveDotDeleteFile(new FileInfo(fileData.FullName));
                continue;
            }
 
            Debug.Trace("BuildResultCache", "Deleting old temporary files: " + fileData.FullName);
            Util.DeleteFileNoException(fileData.FullName); 
        } 
    }
 
    /*
     * Delete all the files in the codegen directory
     */
    internal void RemoveAllCodegenFiles() { 
        Debug.Trace("BuildResultCache", "Deleting all files from " + _cacheDir);
 
        // Remove everything in the codegen directory, as well as all the subdirectories 
        // used for culture assemblies
 
        // Go through all the files in the codegen dir.  Delete everything, except
        // for the fusion cache, which is in the "assembly" subdirectory
        foreach (FileData fileData in FileEnumerator.Create(_cacheDir)) {
 
            // If it's a directories
            if (fileData.IsDirectory) { 
 
                // Skip the fusion cache
                if (fileData.Name == fusionCacheDirectoryName) 
                    continue;

                // Skip the "hash" folder
                if (fileData.Name == webHashDirectoryName) 
                    continue;
 
                // Skip the source files generated for the designer (VSWhidbey 138194) 
                if (StringUtil.StringStartsWith(fileData.Name, CodeDirectoryCompiler.sourcesDirectoryPrefix))
                    continue; 

                try {
                    // If it is a directory, only remove the files inside and not the directory itself
                    // VSWhidbey 596757 
                    DeleteFilesInDirectory(fileData.FullName);
                } 
                catch { } // Ignore all exceptions 

                continue; 
            }

            // VSWhidbey 564168 Do not delete files that cannot be deleted, these files are still
            // referenced by other appdomains that are in the process of shutting down. 
            // We also do not rename as renaming can cause an assembly not to be found if another
            // appdomain tries to compile against it. 
            DiskBuildResultCache.TryDeleteFile(fileData.FullName); 

        } 


        // Clean up the fusion shadow copy cache
 
        AppDomainSetup appDomainSetup = Thread.GetDomain().SetupInformation;
        int hr = UnsafeNativeMethods.DeleteShadowCache(appDomainSetup.CachePath, 
            appDomainSetup.ApplicationName); 
        Debug.Assert(hr >= 0, "DeleteShadowCache failed with error " + hr);
    } 

    // Deletes all files in the directory, but leaves the directory there
    internal void DeleteFilesInDirectory(string path) {
        foreach (FileData fileData in FileEnumerator.Create(path)) { 
            if (fileData.IsDirectory) {
                Directory.Delete(fileData.FullName, true /*recursive*/); 
                continue; 
            }
            Util.RemoveOrRenameFile(fileData.FullName); 
        }
    }
}
 
internal abstract class PrecompBaseDiskBuildResultCache: DiskBuildResultCache {
 
    // In precompilation, the preservation files go in the bin directory 
    internal PrecompBaseDiskBuildResultCache(string cacheDir) : base(cacheDir) { }
} 

// Used when precompiling a site
internal class PrecompilerDiskBuildResultCache: PrecompBaseDiskBuildResultCache {
 
    internal PrecompilerDiskBuildResultCache(string cacheDir) : base(cacheDir) {
 
        EnsureDiskCacheDirectoryCreated(); 
    }
} 

// Used when precompiling a site using updatable precompilation
internal class UpdatablePrecompilerDiskBuildResultCache: PrecompilerDiskBuildResultCache {
 
    internal UpdatablePrecompilerDiskBuildResultCache(string cacheDir) : base(cacheDir) { }
 
    internal override void CacheBuildResult(string cacheKey, BuildResult result, 
        long hashCode, DateTime utcStart) {
 
        // Don't create preservation files in bin for pages in the updatable model,
        // because we turn them into a v1 style code behind, which works as a result of
        // having the aspx file point to the bin class via an inherits attribute.
        if (result is BuildResultCompiledTemplateType) 
            return;
 
        base.CacheBuildResult(cacheKey, result, hashCode, utcStart); 
    }
 
}

// Used when a site is already precompiled
internal class PrecompiledSiteDiskBuildResultCache: PrecompBaseDiskBuildResultCache { 

    internal PrecompiledSiteDiskBuildResultCache(string cacheDir) : base(cacheDir) {} 
 
    protected override bool PrecompilationMode { get { return true; } }
 
    internal override void CacheBuildResult(string cacheKey, BuildResult result,
        long hashCode, DateTime utcStart) {

        // Nothing to cache to disk if the app is already precompiled 
    }
 
    internal override void RemoveAssemblyAndRelatedFiles(string baseName) { 
        // Never remove precompiled files (we couldn't anyways since they're
        // in the app dir) 
    }
}

} 
//------------------------------------------------------------------------------ 
// <copyright file="BuildResultCache.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

 
 
/*********************************
 
BuildResultCache
    MemoryBuildResultCache
    DiskBuildResultCache
        StandardDiskBuildResultCache 
        PrecompBaseDiskBuildResultCache
            PrecompilerDiskBuildResultCache 
            PrecompiledSiteDiskBuildResultCache 

**********************************/ 

namespace System.Web.Compilation {

using System; 
using System.IO;
using System.Collections; 
using System.Globalization; 
using System.Text;
using System.Threading; 
using System.Reflection;
using System.Web.Hosting;
using System.Web.Util;
using System.Web.Caching; 
using System.Web.UI;
 
internal abstract class BuildResultCache { 
    internal BuildResult GetBuildResult(string cacheKey) {
        return GetBuildResult(cacheKey, null /*virtualPath*/, 0 /*hashCode*/); 
    }

    internal abstract BuildResult GetBuildResult(string cacheKey, VirtualPath virtualPath, long hashCode);
 
    internal void CacheBuildResult(string cacheKey, BuildResult result, DateTime utcStart) {
        CacheBuildResult(cacheKey, result, 0 /*hashCode*/, utcStart); 
    } 

    internal abstract void CacheBuildResult(string cacheKey, BuildResult result, 
        long hashCode, DateTime utcStart);

    internal static string GetAssemblyCacheKey(string assemblyPath) {
        string assemblyName = Util.GetAssemblyNameFromFileName(Path.GetFileName(assemblyPath)); 
        return GetAssemblyCacheKeyFromName(assemblyName);
    } 
 
    internal static string GetAssemblyCacheKey(Assembly assembly) {
        Debug.Assert(!assembly.GlobalAssemblyCache); 
        return GetAssemblyCacheKeyFromName(assembly.GetName().Name);
    }

    internal static string GetAssemblyCacheKeyFromName(string assemblyName) { 
        Debug.Assert(StringUtil.StringStartsWith(assemblyName, BuildManager.AssemblyNamePrefix));
        return CacheInternal.PrefixAssemblyPath + assemblyName.ToLowerInvariant(); 
    } 

} 

internal class MemoryBuildResultCache: BuildResultCache {

    private CacheInternal _cache; 
    private CacheItemRemovedCallback _onRemoveCallback;
 
    // The keys are simple assembly names 
    // The values are ArrayLists containing the simple names of assemblies that depend on it
    private Hashtable _dependentAssemblies = new Hashtable(); 

    internal MemoryBuildResultCache(CacheInternal cache) {
        _cache = cache;
 
        // Register an AssemblyLoad event
        AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(OnAssemblyLoad); 
    } 

    private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args) { 
        Assembly a = args.LoadedAssembly;

        // Ignore GAC assemblies
        if (a.GlobalAssemblyCache) 
            return;
 
        // Ignore assemblies that don't start with our prefix 
        string name = a.GetName().Name;
        if (!StringUtil.StringStartsWith(name, BuildManager.AssemblyNamePrefix)) 
            return;

        // Go through all the assemblies it references
        foreach (AssemblyName assemblyName in a.GetReferencedAssemblies()) { 

            // Ignore references that don't start with our prefix 
            if (!StringUtil.StringStartsWith(assemblyName.Name, BuildManager.AssemblyNamePrefix)) 
                continue;
 
            lock (_dependentAssemblies) {
                // Check whether we already have an ArrayList for this reference
                ArrayList dependentList = _dependentAssemblies[assemblyName.Name] as ArrayList;
                if (dependentList == null) { 
                    // If not, create one and add it to the hashtable
                    dependentList = new ArrayList(); 
                    _dependentAssemblies[assemblyName.Name] = dependentList; 
                }
 
                // Add the assembly that just got loaded as a dependent
                Debug.Assert(!dependentList.Contains(name));
                dependentList.Add(name);
            } 
        }
    } 
 
    internal override BuildResult GetBuildResult(string cacheKey, VirtualPath virtualPath, long hashCode) {
        Debug.Trace("BuildResultCache", "Looking for '" + cacheKey + "' in the memory cache"); 

        string key = GetMemoryCacheKey(cacheKey);
        BuildResult result = (BuildResult) _cache.Get(key);
 
        // Not found in the cache
        if (result == null) { 
            Debug.Trace("BuildResultCache", "'" + cacheKey + "' was not found in the memory cache"); 
            return null;
        } 

        // We found it in the cache, but is it up to date.  First, if it uses a CacheDependency,
        // it must be up to date (this is the default case when using MapPathBasedVirtualPathProvider).
        // If not, then we need to explicitely check that it's up to date (more expensive) 
        if (!result.UsesCacheDependency && !result.IsUpToDate(virtualPath)) {
 
            Debug.Trace("BuildResultCache", "'" + cacheKey + "' was found but is out of date"); 

            // Remove it from the cache 
            _cache.Remove(key);

            Debug.Assert(_cache.Get(key) == null);
 
            return null;
        } 
 
        Debug.Trace("BuildResultCache", "'" + cacheKey + "' was found in the memory cache");
 
        // It's up to date: return it
        return result;
    }
 
    internal override void CacheBuildResult(string cacheKey, BuildResult result,
        long hashCode, DateTime utcStart) { 
 
        ICollection virtualDependencies = result.VirtualPathDependencies;
 
        Debug.Trace("BuildResultCache", "Adding cache " + cacheKey + " in the memory cache");

        CacheDependency cacheDependency = null;
 
        if (virtualDependencies != null) {
            cacheDependency = result.VirtualPath.GetCacheDependency(virtualDependencies, utcStart); 
 
            // If we got a cache dependency, remember that in the BuildResult
            if (cacheDependency != null) 
                result.UsesCacheDependency = true;
        }

        // If it should not be cached to memory, leave it alone 
        if (!result.CacheToMemory) {
            return; 
        } 

        BuildResultCompiledAssemblyBase compiledResult = result as BuildResultCompiledAssemblyBase; 
        if (compiledResult != null && compiledResult.ResultAssembly != null && !compiledResult.UsesExistingAssembly) {

            // Insert a new cache entry using the assembly path as the key
            string assemblyKey = GetAssemblyCacheKey(compiledResult.ResultAssembly); 
            Assembly a = (Assembly)_cache.Get(assemblyKey);
            if (a == null) { 
                Debug.Trace("BuildResultCache", "Adding marker cache entry " + compiledResult.ResultAssembly); 
                // VSWhidbey 500049 - add as NotRemovable to prevent the assembly from being prematurely deleted
                _cache.UtcInsert(assemblyKey, compiledResult.ResultAssembly, 
                                 null,
                                 Cache.NoAbsoluteExpiration,
                                 Cache.NoSlidingExpiration,
                                 CacheItemPriority.NotRemovable, 
                                 null);
            } 
            else { 
                Debug.Assert(a == compiledResult.ResultAssembly);
            } 

            // Now create a dependency based on that key. This way, by removing that key, we are able to
            // remove all the pages that live in that assembly from the cache.
            CacheDependency assemblyCacheDependency = new CacheDependency(0, null, new string[] { assemblyKey }); 

            if (cacheDependency != null) { 
                // We can't share the same CacheDependency, since we don't want the UtcStart 
                // behavior for the assembly.  Use an Aggregate to put the two together.
                AggregateCacheDependency tmpDependency = new AggregateCacheDependency(); 
                tmpDependency.Add(new CacheDependency[] { cacheDependency, assemblyCacheDependency });
                cacheDependency = tmpDependency;
            }
            else { 
                cacheDependency = assemblyCacheDependency;
            } 
        } 

        string key = GetMemoryCacheKey(cacheKey); 

        // Only allow the cache item to expire if the result can be unloaded.  Otherwise,
        // we may as well cache it forever (e.g. for Assemblies and Types).
        CacheItemPriority cachePriority; 
        if (result.IsUnloadable)
            cachePriority = CacheItemPriority.Default; 
        else 
            cachePriority = CacheItemPriority.NotRemovable;
 
        CacheItemRemovedCallback onRemoveCallback = null;

        // If the appdomain needs to be shut down when the item becomes invalid, register
        // a callback to do the shutdown. 
        if (result.ShutdownAppDomainOnChange || result is BuildResultCompiledAssemblyBase) {
 
            // Create the delegate on demand 
            if (_onRemoveCallback == null)
                _onRemoveCallback = new CacheItemRemovedCallback(OnCacheItemRemoved); 

            onRemoveCallback = _onRemoveCallback;
        }
 
        _cache.UtcInsert(key, result, cacheDependency,
            result.MemoryCacheExpiration, 
            result.MemoryCacheSlidingExpiration, 
            cachePriority,
            onRemoveCallback); 
    }

    private void OnCacheItemRemoved(string key, object value, CacheItemRemovedReason reason) {
 
        // Only handle case when the dependency is removed.
        if (reason == CacheItemRemovedReason.DependencyChanged) { 
            Debug.Trace("BuildResultCache", "OnCacheItemRemoved Key=" + key); 

            // Remove the assembly if a buildresult becomes obsolete 
            if (HostingEnvironment.ShutdownInitiated) {
                // VSWhidbey 564168
                // We still need to mark the affected files and dependencies for later deletion so that we do not build up unused assemblies.
                RemoveAssemblyAndCleanupDependenciesShuttingDown(value as BuildResultCompiledAssembly); 
            }
            else { 
 
                RemoveAssemblyAndCleanupDependencies(value as BuildResultCompiledAssemblyBase);
 
                // Shutdown the appdomain if the buildresult requires it.
                if (((BuildResult)value).ShutdownAppDomainOnChange) {
                    HttpRuntime.SetShutdownReason(ApplicationShutdownReason.BuildManagerChange, "BuildResult change, cache key=" + key);
                    HostingEnvironment.InitiateShutdown(); 
                }
            } 
        } 
    }
 
    // Since we are shutting down, we will just create the .delete files to mark the files for deletion,
    // and not try to get the compilation lock.
    internal void RemoveAssemblyAndCleanupDependenciesShuttingDown(BuildResultCompiledAssemblyBase compiledResult) {
        if (compiledResult == null) 
            return;
 
        if (compiledResult != null && compiledResult.ResultAssembly != null && !compiledResult.UsesExistingAssembly) { 
            string assemblyName = compiledResult.ResultAssembly.GetName().Name;
            lock (_dependentAssemblies) { 
                RemoveAssemblyAndCleanupDependenciesNoLock(assemblyName);
            }
        }
    } 

 
    internal void RemoveAssemblyAndCleanupDependencies(BuildResultCompiledAssemblyBase compiledResult) { 
        if (compiledResult == null)
            return; 

        if (compiledResult != null && compiledResult.ResultAssembly != null && !compiledResult.UsesExistingAssembly) {
            RemoveAssemblyAndCleanupDependencies(compiledResult.ResultAssembly.GetName().Name);
        } 
    }
 
    private void RemoveAssemblyAndCleanupDependencies(string assemblyName) { 
        bool gotLock = false;
 
        try {
            // Grab the compilation mutex, since we will remove cached build result
            CompilationLock.GetLock(ref gotLock);
 
            // Protect the dependent assemblies table, as it's accessed/modified in the recursion
            lock (_dependentAssemblies) { 
                RemoveAssemblyAndCleanupDependenciesNoLock(assemblyName); 
            }
        } 
        finally {
            // Always release the mutex if we had taken it
            if (gotLock) {
                CompilationLock.ReleaseLock(); 
            }
            DiskBuildResultCache.ShutDownAppDomainIfRequired(); 
        } 
    }
 
    private void RemoveAssemblyAndCleanupDependenciesNoLock(string assemblyName) {

        // If we have no cache entry for this assembly, there is nothing to do
        string cacheKey = GetAssemblyCacheKeyFromName(assemblyName); 
        Assembly assembly = (Assembly)_cache[cacheKey];
        if (assembly == null) 
            return; 

        // Get the physical path to the assembly 
        String assemblyPath = Util.GetAssemblyCodeBase(assembly);

        Debug.Trace("BuildResultCache", "removing cacheKey for assembly " + assemblyPath + " because of dependency change");
 
        // Remove the cache entry in order to kick out all the pages that are in that batch
        _cache.Remove(cacheKey); 
 
        // Now call recursively on all the dependent assemblies (VSWhidbey 577593)
        ICollection dependentAssemblies = _dependentAssemblies[assemblyName] as ICollection; 
        if (dependentAssemblies != null) {
            foreach (string dependentAssemblyName in dependentAssemblies) {
                RemoveAssemblyAndCleanupDependenciesNoLock(dependentAssemblyName);
            } 

            // We can now remove this assembly from the hashtable 
            _dependentAssemblies.Remove(cacheKey); 
        }
 
        // Remove (or rename) the DLL
        DiskBuildResultCache.RemoveAssembly(new FileInfo(assemblyPath));
    }
 
    private static string GetMemoryCacheKey(string cacheKey) {
 
        // Prepend something to it to avoid conflicts with other cache users 
        return CacheInternal.PrefixMemoryBuildResult + cacheKey;
    } 
}

internal abstract class DiskBuildResultCache: BuildResultCache {
 
    protected const string preservationFileExtension = ".compiled";
 
    protected string _cacheDir; 

    private static int s_recompilations; 
    private static int s_maxRecompilations = -1;

    private static bool s_inUseAssemblyWasDeleted;
 
    protected const string dotDelete = ".delete";
 
    private static bool s_shutdownRequired; 

    internal DiskBuildResultCache(string cacheDir) { 
        _cacheDir = cacheDir;

        // Find out how many recompilations we allow before restarting the appdomain
        if (s_maxRecompilations < 0) 
            s_maxRecompilations = CompilationUtil.GetRecompilationsBeforeAppRestarts();
    } 
 
    protected void EnsureDiskCacheDirectoryCreated() {
 
        // Create the disk cache directory if it's not already there
        if (!FileUtil.DirectoryExists(_cacheDir)) {
            try {
                Directory.CreateDirectory(_cacheDir); 
            }
            catch (IOException e) { 
                throw new HttpException(SR.GetString( 
                    SR.Failed_to_create_temp_dir, HttpRuntime.GetSafePath(_cacheDir)), e);
            } 
        }
    }

    internal override BuildResult GetBuildResult(string cacheKey, VirtualPath virtualPath, long hashCode) { 

        Debug.Trace("BuildResultCache", "Looking for '" + cacheKey + "' in the disk cache"); 
 
        string preservationFile = GetPreservedDataFileName(cacheKey);
 
        PreservationFileReader pfr = new PreservationFileReader(this, PrecompilationMode);

        // Create the BuildResult from the preservation file
        BuildResult result = pfr.ReadBuildResultFromFile(virtualPath, preservationFile, hashCode); 

        if (result != null) 
            Debug.Trace("BuildResultCache", "'" + cacheKey + "' was found in the disk cache"); 
        else
            Debug.Trace("BuildResultCache", "'" + cacheKey + "' was not found in the disk cache"); 

        return result;
    }
 
    internal override void CacheBuildResult(string cacheKey, BuildResult result,
        long hashCode, DateTime utcStart) { 
 
        // If it should not be cached to disk, leave it alone
        if (!result.CacheToDisk) 
            return;

        // VSWhidbey 564168 don't save to disk if already shutting down, otherwise we might
        // be persisting assembly that was compiled with obsolete references. 
        // Since we are shutting down and not creating any cache, delete the compiled result
        // as it will not be used in future. 
        if (HostingEnvironment.ShutdownInitiated) { 
            BuildResultCompiledAssemblyBase compiledResult = result as BuildResultCompiledAssemblyBase;
            if (compiledResult != null) 
                MarkAssemblyAndRelatedFilesForDeletion(compiledResult.ResultAssembly.GetName().Name);
            return;
        }
 
        string preservationFile = GetPreservedDataFileName(cacheKey);
        PreservationFileWriter pfw = new PreservationFileWriter(PrecompilationMode); 
 
        pfw.SaveBuildResultToFile(preservationFile, result, hashCode);
    } 

    private void MarkAssemblyAndRelatedFilesForDeletion(string assemblyName) {
        DirectoryInfo directory = new DirectoryInfo(_cacheDir);
        // Get rid of the prefix "App_web", since related files don't have it 
        string baseName = assemblyName.Substring(BuildManager.WebAssemblyNamePrefix.Length);
        FileInfo[] files = directory.GetFiles("*" + baseName + ".*"); 
        foreach (FileInfo f in files) 
            CreateDotDeleteFile(f);
    } 

    /*
     * Return the physical full path to the preservation data file
     */ 
    private string GetPreservedDataFileName(string cacheKey) {
 
        // Make sure the key doesn't contain any invalid file name chars (VSWhidbey 263142) 
        cacheKey = Util.MakeValidFileName(cacheKey);
 
        cacheKey = Path.Combine(_cacheDir, cacheKey);

        cacheKey = FileUtil.TruncatePathIfNeeded(cacheKey, 9 /*length of ".compiled"*/);
 
        // Use a ".compiled" extension for the preservation file
        return cacheKey + preservationFileExtension; 
    } 

    protected virtual bool PrecompilationMode { get { return false; } } 

    internal static bool InUseAssemblyWasDeleted { get { return s_inUseAssemblyWasDeleted; } }
    internal static void ResetAssemblyDeleted() { s_inUseAssemblyWasDeleted = false; }
 
    /*
     * Delete an assembly and all its related files.  The assembly is typically named 
     * something like ASPNET.jnw_y10n.dll, while related files are simply jnw_y10n.*. 
     */
    internal virtual void RemoveAssemblyAndRelatedFiles(string assemblyName) { 

        Debug.Trace("DiskBuildResultCache", "RemoveAssemblyAndRelatedFiles(" + assemblyName + ")");

        // If the name doesn't start with the prefix, the cleanup code doesn't apply 
        if (!assemblyName.StartsWith(BuildManager.AssemblyNamePrefix, StringComparison.Ordinal)) {
            // 
            Debug.Assert(false); 
            return;
        } 

        // Get rid of the prefix, since related files don't have it
        string baseName = assemblyName.Substring(BuildManager.WebAssemblyNamePrefix.Length);
 
        bool gotLock = false;
        try { 
            // Grab the compilation mutex, since we will remove generated assembly 
            CompilationLock.GetLock(ref gotLock);
 
            DirectoryInfo directory = new DirectoryInfo(_cacheDir);

            // Find all the files that contain the base name
            FileInfo[] files = directory.GetFiles("*" + baseName + ".*"); 
            foreach (FileInfo f in files) {
 
                if (f.Extension == ".dll") { 
                    // Notify existing buildresults that result assembly will be removed.
                    // This is required otherwise new components can be compiled 
                    // with obsolete build results whose assembly has been removed.
                    string assemblyKey = GetAssemblyCacheKey(f.FullName);
                    HttpRuntime.CacheInternal.Remove(assemblyKey);
 
                    // Remove the assembly
                    RemoveAssembly(f); 
 
                    // Also, remove satellite assemblies that may be associated with it
                    StandardDiskBuildResultCache.RemoveSatelliteAssemblies(assemblyName); 
                }
                else if (f.Extension == dotDelete) {
                    CheckAndRemoveDotDeleteFile(f);
                } 
                else {
                    // Remove the file, or if not possible, rename it, so it'll get 
                    // cleaned up next time by RemoveOldTempFiles() 

                    TryDeleteFile(f); 
                }
            }
        }
        finally { 
            // Always release the mutex if we had taken it
            if (gotLock) { 
                CompilationLock.ReleaseLock(); 
            }
            DiskBuildResultCache.ShutDownAppDomainIfRequired(); 
        }
    }

    internal static void RemoveAssembly(FileInfo f) { 

        // If we are shutting down, just create the .delete file and exit quickly. 
        if (HostingEnvironment.ShutdownInitiated) { 
            CreateDotDeleteFile(f);
            return; 
        }

        // VSWhidbey 564168 / Visual Studio QFE 4710
        // The assembly could still be referenced and needed for compilation in some cases. 
        // Thus, if we cannot delete it, we create an empty .delete file,
        // so that both will be later removed by RemoveOldTempFiles. 
 
        // If the file is already marked for deletion, we simply return, so that
        // we do not double count it in s_recompilations. 
        if (HasDotDeleteFile(f.FullName))
            return;

        if (TryDeleteFile(f)) 
            return;
 
        // It had to be renamed, so increment the recompilations count, 
        // and restart the appdomain if it reaches the limit
 
        Debug.Trace("DiskBuildResultCache", "RemoveAssembly: " + f.Name + " was renamed");

        if (++s_recompilations == s_maxRecompilations) {
            s_shutdownRequired = true; 
        }
 
        // Remember the fact that we just invalidated an assembly, which can cause 
        // other BuildResults to become invalid as a side effect (VSWhidbey 269297)
        s_inUseAssemblyWasDeleted = true; 
    }

    static internal void ShutDownAppDomainIfRequired() {
        // VSWhidbey 610631 Stress Failure: Worker process throws exceptions while unloading app domain and re-tries over and over 
        // It is possible for a deadlock to happen when locks on ApplicationManager and the CompilationMutex
        // are acquired in different orders in multiple threads. 
        // Thus, since ShutdownAppDomain acquires a lock on ApplicationManager, we always release the CompilationMutex 
        // before calling ShutdownAppDomain, in case another thread has acquired the lock on ApplicationManager and
        // is waiting on the CompilationMutex. 

        if (s_shutdownRequired)
            HttpRuntime.ShutdownAppDomain(ApplicationShutdownReason.MaxRecompilationsReached,
                "Recompilation limit of " + s_maxRecompilations + " reached"); 
    }
 
 
    internal static bool TryDeleteFile(string s) {
        return TryDeleteFile(new FileInfo(s)); 
    }

    // Returns true if we are able to delete the file. Otherwise, creates a .delete file and returns false.
    internal static bool TryDeleteFile(FileInfo f) { 
        if (f.Extension == dotDelete)
            return CheckAndRemoveDotDeleteFile(f); 
 
        try {
            f.Delete(); 
            Debug.Trace("DiskBuildResultCache", "TryDeleteFile removed " + f.Name);
            return true;
        }
        catch { } 

        CreateDotDeleteFile(f); 
        return false; 
    }
 
    // Checks if the file is .delete. If it is, check if the associated base file is still around.
    // If associated base file is around, try to delete it. If success, delete the .delete.
    // Returns true only if both base file and .delete are removed.
    internal static bool CheckAndRemoveDotDeleteFile(FileInfo f) { 
        if (f.Extension != dotDelete)
            return false; 
 
        string baseName = Path.GetDirectoryName(f.FullName) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(f.FullName);
        if (FileUtil.FileExists(baseName)) { 
            try {
                File.Delete(baseName);
                Debug.Trace("DiskBuildResultCache", "CheckAndRemoveDotDeleteFile deleted " + baseName);
            } 
            catch {
                return false; 
            } 
        }
 
        try {
            f.Delete();
            Debug.Trace("DiskBuildResultCache", "CheckAndRemoveDotDeleteFile deleted " + f.Name);
        } 
        catch { }
 
        return true; 
    }
 
    internal static bool HasDotDeleteFile(string s) {
        return File.Exists(s + dotDelete);
    }
 
    private static void CreateDotDeleteFile(FileInfo f) {
        if (f.Extension == dotDelete) 
            return; 
        string newName = f.FullName + dotDelete;
        if (!File.Exists(newName)) { 
            try {
                (new StreamWriter(newName)).Close();
                Debug.Trace("DiskBuildResultCache", "CreateDotDeleteFile succeeded: " + newName);
            } 
            catch {
                Debug.Trace("DiskBuildResultCache", "CreateDotDeleteFile failed: " + newName); 
            } // If we fail the .delete probably just got created by another process. 
        }
    } 

}

internal class StandardDiskBuildResultCache: DiskBuildResultCache { 

    private const string fusionCacheDirectoryName = "assembly"; 
    private const string webHashDirectoryName = "hash"; 

    private static ArrayList _satelliteDirectories; 

    internal StandardDiskBuildResultCache(string cacheDir)
        : base(cacheDir) {
 
        Debug.Assert(cacheDir == HttpRuntime.CodegenDirInternal);
 
        EnsureDiskCacheDirectoryCreated(); 

        FindSatelliteDirectories(); 
    }

    private string GetSpecialFilesCombinedHashFileName() {
        return BuildManager.WebHashFilePath; 
    }
 
    /* 
     * Return the combined hash that was preserved to file.  Return 0 if not valid.
     */ 
    internal long GetPreservedSpecialFilesCombinedHash() {

        string fileName = GetSpecialFilesCombinedHashFileName();
 
        if (!FileUtil.FileExists(fileName))
            return 0; 
 
        try {
            string s = Util.StringFromFile(fileName); 
            return Int64.Parse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        }
        catch {
            // If anything went wrong (file not found, or bad format), return 0 
            return 0;
        } 
    } 

    /* 
     * Preserve the combined hash of the special files to a file.
     */
    internal void SavePreservedSpecialFilesCombinedHash(long hash) {
 
        Debug.Assert(hash != 0, "SavePreservedSpecialFilesCombinedHash: hash != 0");
        StreamWriter writer = null; 
 
        try {
            String hashFilePath = GetSpecialFilesCombinedHashFileName(); 
            String hashDirPath = Path.GetDirectoryName(hashFilePath);

            // Create the hashweb directory if needed
            if (!FileUtil.DirectoryExists(hashDirPath)) { 
                Directory.CreateDirectory(hashDirPath);
            } 
 
            writer = new StreamWriter(hashFilePath, false, Encoding.UTF8);
            writer.Write(hash.ToString("x", CultureInfo.InvariantCulture)); 
        }
        finally {
            if (writer != null)
                writer.Close(); 
        }
    } 
 
    private void FindSatelliteDirectories() {
 
        Debug.Assert(_satelliteDirectories == null);

        //
        // Look for all the subdirectories of the codegen dir that look like 
        // satellite assemblies dirs, and keep track of them
        // 
 
        string[] subDirs = Directory.GetDirectories(_cacheDir);
 
        foreach (string subDir in subDirs) {
            string subDirName = Path.GetFileNameWithoutExtension(subDir);

            // Skip the fusion cache, since it's definitely not a culture (VSWhidbey 327716) 
            if (subDirName == fusionCacheDirectoryName)
                continue; 
 
            // Skip the "hash" folder
            if (subDirName == webHashDirectoryName) 
                continue;

            if (Util.IsCultureName(subDirName)) {
                if (_satelliteDirectories == null) 
                    _satelliteDirectories = new ArrayList();
 
                _satelliteDirectories.Add(Path.Combine(_cacheDir, subDir)); 
            }
        } 
    }

    internal static void RemoveSatelliteAssemblies(string baseAssemblyName) {
 
        if (_satelliteDirectories == null)
            return; 
 
        //
        // If any satellite directory contains a satellite assembly that's 
        // for the passed in assembly name, delete it.
        //

        string satelliteAssemblyName = baseAssemblyName + ".resources"; 

        foreach (string satelliteDir in _satelliteDirectories) { 
            string fullAssemblyPath = Path.Combine(satelliteDir, satelliteAssemblyName); 

            // Delete the DLL and PDB 
            Util.DeleteFileIfExistsNoException(fullAssemblyPath + ".dll");
            Util.DeleteFileIfExistsNoException(fullAssemblyPath + ".pdb");
        }
    } 

    /* 
     * Delete all temporary files from the codegen directory (e.g. source files, ...) 
     */
    internal void RemoveOldTempFiles() { 
        Debug.Trace("BuildResultCache", "Deleting old temporary files from " + _cacheDir);

        string codegen = _cacheDir + "\\";
 
        // Go through all the files in the codegen dir
        foreach (FileData fileData in FileEnumerator.Create(codegen)) { 
 
            // Skip directories
            if (fileData.IsDirectory) continue; 

            // If it has a known extension, skip it
            string ext = Path.GetExtension(fileData.Name);
            if (ext == ".dll" || ext == ".pdb" || ext == ".web" || ext == ".ccu" || ext == preservationFileExtension) { 
                continue;
            } 
 
            // .delete files need to be removed.
            if (ext != dotDelete) { 
                // Don't delete the temp file if it's named after a dll that's still around
                // since it could still be useful for debugging.
                // Note that we can't use GetFileNameWithoutExtension here because
                // some of the files are named 5hvoxl6v.0.cs, and it would return 
                // 5hvoxl6v.0 instead of just 5hvoxl6v
                int periodIndex = fileData.Name.LastIndexOf('.'); 
                if (periodIndex > 0) { 
                    string baseName = fileData.Name.Substring(0, periodIndex);
 
                    int secondPeriodIndex = baseName.LastIndexOf('.');
                    if (secondPeriodIndex > 0) {
                        baseName = baseName.Substring(0, secondPeriodIndex);
                    } 

                    // Generated source files uses assemblyname as prefix so we should keep them. 
                    if (FileUtil.FileExists(codegen + baseName + ".dll")) 
                        continue;
 
                    // other generated files, such as .cmdline, .err and .out need to add the
                    // WebAssemblyNamePrefix, since they do not use the assembly name as prefix.
                    if (FileUtil.FileExists(codegen + BuildManager.WebAssemblyNamePrefix + baseName + ".dll"))
                        continue; 
                }
            } 
            else { 
                // Additional logic for VSWhidbey 564168 / Visual Studio QFE 4710.
                // Delete both original .dll and .delete if possible 
                DiskBuildResultCache.CheckAndRemoveDotDeleteFile(new FileInfo(fileData.FullName));
                continue;
            }
 
            Debug.Trace("BuildResultCache", "Deleting old temporary files: " + fileData.FullName);
            Util.DeleteFileNoException(fileData.FullName); 
        } 
    }
 
    /*
     * Delete all the files in the codegen directory
     */
    internal void RemoveAllCodegenFiles() { 
        Debug.Trace("BuildResultCache", "Deleting all files from " + _cacheDir);
 
        // Remove everything in the codegen directory, as well as all the subdirectories 
        // used for culture assemblies
 
        // Go through all the files in the codegen dir.  Delete everything, except
        // for the fusion cache, which is in the "assembly" subdirectory
        foreach (FileData fileData in FileEnumerator.Create(_cacheDir)) {
 
            // If it's a directories
            if (fileData.IsDirectory) { 
 
                // Skip the fusion cache
                if (fileData.Name == fusionCacheDirectoryName) 
                    continue;

                // Skip the "hash" folder
                if (fileData.Name == webHashDirectoryName) 
                    continue;
 
                // Skip the source files generated for the designer (VSWhidbey 138194) 
                if (StringUtil.StringStartsWith(fileData.Name, CodeDirectoryCompiler.sourcesDirectoryPrefix))
                    continue; 

                try {
                    // If it is a directory, only remove the files inside and not the directory itself
                    // VSWhidbey 596757 
                    DeleteFilesInDirectory(fileData.FullName);
                } 
                catch { } // Ignore all exceptions 

                continue; 
            }

            // VSWhidbey 564168 Do not delete files that cannot be deleted, these files are still
            // referenced by other appdomains that are in the process of shutting down. 
            // We also do not rename as renaming can cause an assembly not to be found if another
            // appdomain tries to compile against it. 
            DiskBuildResultCache.TryDeleteFile(fileData.FullName); 

        } 


        // Clean up the fusion shadow copy cache
 
        AppDomainSetup appDomainSetup = Thread.GetDomain().SetupInformation;
        int hr = UnsafeNativeMethods.DeleteShadowCache(appDomainSetup.CachePath, 
            appDomainSetup.ApplicationName); 
        Debug.Assert(hr >= 0, "DeleteShadowCache failed with error " + hr);
    } 

    // Deletes all files in the directory, but leaves the directory there
    internal void DeleteFilesInDirectory(string path) {
        foreach (FileData fileData in FileEnumerator.Create(path)) { 
            if (fileData.IsDirectory) {
                Directory.Delete(fileData.FullName, true /*recursive*/); 
                continue; 
            }
            Util.RemoveOrRenameFile(fileData.FullName); 
        }
    }
}
 
internal abstract class PrecompBaseDiskBuildResultCache: DiskBuildResultCache {
 
    // In precompilation, the preservation files go in the bin directory 
    internal PrecompBaseDiskBuildResultCache(string cacheDir) : base(cacheDir) { }
} 

// Used when precompiling a site
internal class PrecompilerDiskBuildResultCache: PrecompBaseDiskBuildResultCache {
 
    internal PrecompilerDiskBuildResultCache(string cacheDir) : base(cacheDir) {
 
        EnsureDiskCacheDirectoryCreated(); 
    }
} 

// Used when precompiling a site using updatable precompilation
internal class UpdatablePrecompilerDiskBuildResultCache: PrecompilerDiskBuildResultCache {
 
    internal UpdatablePrecompilerDiskBuildResultCache(string cacheDir) : base(cacheDir) { }
 
    internal override void CacheBuildResult(string cacheKey, BuildResult result, 
        long hashCode, DateTime utcStart) {
 
        // Don't create preservation files in bin for pages in the updatable model,
        // because we turn them into a v1 style code behind, which works as a result of
        // having the aspx file point to the bin class via an inherits attribute.
        if (result is BuildResultCompiledTemplateType) 
            return;
 
        base.CacheBuildResult(cacheKey, result, hashCode, utcStart); 
    }
 
}

// Used when a site is already precompiled
internal class PrecompiledSiteDiskBuildResultCache: PrecompBaseDiskBuildResultCache { 

    internal PrecompiledSiteDiskBuildResultCache(string cacheDir) : base(cacheDir) {} 
 
    protected override bool PrecompilationMode { get { return true; } }
 
    internal override void CacheBuildResult(string cacheKey, BuildResult result,
        long hashCode, DateTime utcStart) {

        // Nothing to cache to disk if the app is already precompiled 
    }
 
    internal override void RemoveAssemblyAndRelatedFiles(string baseName) { 
        // Never remove precompiled files (we couldn't anyways since they're
        // in the app dir) 
    }
}

} 
