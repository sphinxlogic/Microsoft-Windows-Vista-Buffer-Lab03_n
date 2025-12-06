//------------------------------------------------------------------------------ 
// <copyright file="UrlPath.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.Util { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Web.UI.Design;

    /// <devdoc> 
    /// Helper class for URLs.
    /// The only method in this class is borrowed directly from the runtime's 
    /// System.Web.Util.UrlPath class. 
    /// </devdoc>
    internal class UrlPath { 
        // Only static methods, so hide constructor.
        private UrlPath() {
        }
 
        /// <devdoc>
        /// Returns true if the path is an absolute physical path. 
        /// </devdoc> 
        private static bool IsAbsolutePhysicalPath(string path) {
            if (path == null || path.Length < 3) 
                return false;

            if (path.StartsWith("\\\\", StringComparison.Ordinal))
                return true; 

            return (Char.IsLetter(path[0]) && path[1] == ':' && path[2] == '\\'); 
        } 

        /// <devdoc> 
        /// Maps an arbitrary path (physical absolute, app-relative, relative) to
        /// a physical path using designer host services. If the path cannot be
        /// mapped because certain services are not present, null is returned.
        /// </devdoc> 
        internal static string MapPath(IServiceProvider serviceProvider, string path) {
            if (path.Length == 0) { 
                return null; 
            }
 
            if (IsAbsolutePhysicalPath(path)) {
                // Absolute path
                return path;
            } 
            else {
                // Root relative path - use designer host service to map the path 
                WebFormsRootDesigner rootDesigner = null; 

                if (serviceProvider != null) { 
                    IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                    if ((designerHost != null) && (designerHost.RootComponent != null)) {
                        rootDesigner = designerHost.GetDesigner(designerHost.RootComponent) as WebFormsRootDesigner;
 
                        if (rootDesigner != null) {
                            string resolvedUrl = rootDesigner.ResolveUrl(path); 
 
                            // Use the WebApplication server to get a physical path from the app-relative path
                            IWebApplication webApplicationService = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication)); 
                            if (webApplicationService != null) {
                                IProjectItem dataFileProjectItem = webApplicationService.GetProjectItemFromUrl(resolvedUrl);
                                if (dataFileProjectItem != null) {
                                    return dataFileProjectItem.PhysicalPath; 
                                }
                            } 
                        } 
                    }
                } 
            }
            // Could not get service to map path, return null
            return null;
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="UrlPath.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.Util { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Web.UI.Design;

    /// <devdoc> 
    /// Helper class for URLs.
    /// The only method in this class is borrowed directly from the runtime's 
    /// System.Web.Util.UrlPath class. 
    /// </devdoc>
    internal class UrlPath { 
        // Only static methods, so hide constructor.
        private UrlPath() {
        }
 
        /// <devdoc>
        /// Returns true if the path is an absolute physical path. 
        /// </devdoc> 
        private static bool IsAbsolutePhysicalPath(string path) {
            if (path == null || path.Length < 3) 
                return false;

            if (path.StartsWith("\\\\", StringComparison.Ordinal))
                return true; 

            return (Char.IsLetter(path[0]) && path[1] == ':' && path[2] == '\\'); 
        } 

        /// <devdoc> 
        /// Maps an arbitrary path (physical absolute, app-relative, relative) to
        /// a physical path using designer host services. If the path cannot be
        /// mapped because certain services are not present, null is returned.
        /// </devdoc> 
        internal static string MapPath(IServiceProvider serviceProvider, string path) {
            if (path.Length == 0) { 
                return null; 
            }
 
            if (IsAbsolutePhysicalPath(path)) {
                // Absolute path
                return path;
            } 
            else {
                // Root relative path - use designer host service to map the path 
                WebFormsRootDesigner rootDesigner = null; 

                if (serviceProvider != null) { 
                    IDesignerHost designerHost = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                    if ((designerHost != null) && (designerHost.RootComponent != null)) {
                        rootDesigner = designerHost.GetDesigner(designerHost.RootComponent) as WebFormsRootDesigner;
 
                        if (rootDesigner != null) {
                            string resolvedUrl = rootDesigner.ResolveUrl(path); 
 
                            // Use the WebApplication server to get a physical path from the app-relative path
                            IWebApplication webApplicationService = (IWebApplication)serviceProvider.GetService(typeof(IWebApplication)); 
                            if (webApplicationService != null) {
                                IProjectItem dataFileProjectItem = webApplicationService.GetProjectItemFromUrl(resolvedUrl);
                                if (dataFileProjectItem != null) {
                                    return dataFileProjectItem.PhysicalPath; 
                                }
                            } 
                        } 
                    }
                } 
            }
            // Could not get service to map path, return null
            return null;
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
