//------------------------------------------------------------------------------ 
// <copyright file="BuildDependencySet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/************************************************************************************************************/ 
 

 
namespace System.Web.Compilation {

    using System.Collections;
    using System.Security.Permissions; 

 
    /// <devdoc> 
    ///    <para>
    ///       Dependency set returned by BuildManager.GetCachedBuildDependencySet 
    ///    </para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class BuildDependencySet { 

        private BuildResult _result; 
 
        internal BuildDependencySet(BuildResult result) {
            _result = result; 
        }


        public string HashCode { get { return _result.VirtualPathDependenciesHash; } } 

        public IEnumerable VirtualPaths { get { return _result.VirtualPathDependencies; } } 
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="BuildDependencySet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/************************************************************************************************************/ 
 

 
namespace System.Web.Compilation {

    using System.Collections;
    using System.Security.Permissions; 

 
    /// <devdoc> 
    ///    <para>
    ///       Dependency set returned by BuildManager.GetCachedBuildDependencySet 
    ///    </para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class BuildDependencySet { 

        private BuildResult _result; 
 
        internal BuildDependencySet(BuildResult result) {
            _result = result; 
        }


        public string HashCode { get { return _result.VirtualPathDependenciesHash; } } 

        public IEnumerable VirtualPaths { get { return _result.VirtualPathDependencies; } } 
    } 
}
