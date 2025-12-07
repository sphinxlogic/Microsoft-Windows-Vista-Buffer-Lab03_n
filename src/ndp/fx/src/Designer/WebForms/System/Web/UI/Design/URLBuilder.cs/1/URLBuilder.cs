//------------------------------------------------------------------------------ 
// <copyright file="URLBuilder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System.Runtime.Serialization.Formatters; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics;

    using System;
    using System.Web.UI.Design; 
    using Microsoft.Win32;
 
    /// <include file='doc\URLBuilder.uex' path='docs/doc[@for="UrlBuilder"]/*' /> 
    /// <devdoc>
    ///   Helper class used by designers to 'build' Url properties by 
    ///   launching a Url picker.
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class UrlBuilder { 

        private UrlBuilder() { 
        } 

        /// <include file='doc\URLBuilder.uex' path='docs/doc[@for="UrlBuilder.BuildUrl"]/*' /> 
        /// <devdoc>
        ///   Launches the Url Picker to build a color.
        /// </devdoc>
        public static string BuildUrl(IComponent component, System.Windows.Forms.Control owner, string initialUrl, string caption, string filter) { 
            return BuildUrl(component, owner, initialUrl, caption, filter, UrlBuilderOptions.None);
        } 
 
        /// <include file='doc\URLBuilder.uex' path='docs/doc[@for="UrlBuilder.BuildUrl2"]/*' />
        /// <devdoc> 
        ///   Launches the Url Picker to build a color.
        /// </devdoc>
        public static string BuildUrl(IComponent component, System.Windows.Forms.Control owner, string initialUrl, string caption, string filter, UrlBuilderOptions options) {
            ISite componentSite = component.Site; 
            Debug.Assert(componentSite != null, "Component does not have a valid site.");
 
            if (componentSite == null) { 
                Debug.Fail("Component does not have a valid site.");
                return null; 
            }

            return BuildUrl(componentSite, owner, initialUrl, caption, filter, options);
        } 

        /// <include file='doc\URLBuilder.uex' path='docs/doc[@for="UrlBuilder.BuildUrl1"]/*' /> 
        public static string BuildUrl(IServiceProvider serviceProvider, System.Windows.Forms.Control owner, string initialUrl, string caption, string filter, UrlBuilderOptions options) { 
            string baseUrl = String.Empty;
            string result = null; 

            // Work out the base Url.
            IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
 
            if (host != null) {
                WebFormsRootDesigner rootDesigner = host.GetDesigner(host.RootComponent) as WebFormsRootDesigner; 
                if (rootDesigner != null) { 
                    baseUrl = rootDesigner.DocumentUrl;
                } 
            }
            if (baseUrl.Length == 0) {
#pragma warning disable 618
                IWebFormsDocumentService wfdServices = (IWebFormsDocumentService)serviceProvider.GetService(typeof(IWebFormsDocumentService)); 

                if (wfdServices != null) { 
                    baseUrl = wfdServices.DocumentUrl; 
                }
#pragma warning restore 618 
            }

            IWebFormsBuilderUIService builderService =
                (IWebFormsBuilderUIService)serviceProvider.GetService(typeof(IWebFormsBuilderUIService)); 
            if (builderService != null) {
                result = builderService.BuildUrl(owner, initialUrl, baseUrl, caption, filter, options); 
            } 

            return result; 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="URLBuilder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System.Runtime.Serialization.Formatters; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics;

    using System;
    using System.Web.UI.Design; 
    using Microsoft.Win32;
 
    /// <include file='doc\URLBuilder.uex' path='docs/doc[@for="UrlBuilder"]/*' /> 
    /// <devdoc>
    ///   Helper class used by designers to 'build' Url properties by 
    ///   launching a Url picker.
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class UrlBuilder { 

        private UrlBuilder() { 
        } 

        /// <include file='doc\URLBuilder.uex' path='docs/doc[@for="UrlBuilder.BuildUrl"]/*' /> 
        /// <devdoc>
        ///   Launches the Url Picker to build a color.
        /// </devdoc>
        public static string BuildUrl(IComponent component, System.Windows.Forms.Control owner, string initialUrl, string caption, string filter) { 
            return BuildUrl(component, owner, initialUrl, caption, filter, UrlBuilderOptions.None);
        } 
 
        /// <include file='doc\URLBuilder.uex' path='docs/doc[@for="UrlBuilder.BuildUrl2"]/*' />
        /// <devdoc> 
        ///   Launches the Url Picker to build a color.
        /// </devdoc>
        public static string BuildUrl(IComponent component, System.Windows.Forms.Control owner, string initialUrl, string caption, string filter, UrlBuilderOptions options) {
            ISite componentSite = component.Site; 
            Debug.Assert(componentSite != null, "Component does not have a valid site.");
 
            if (componentSite == null) { 
                Debug.Fail("Component does not have a valid site.");
                return null; 
            }

            return BuildUrl(componentSite, owner, initialUrl, caption, filter, options);
        } 

        /// <include file='doc\URLBuilder.uex' path='docs/doc[@for="UrlBuilder.BuildUrl1"]/*' /> 
        public static string BuildUrl(IServiceProvider serviceProvider, System.Windows.Forms.Control owner, string initialUrl, string caption, string filter, UrlBuilderOptions options) { 
            string baseUrl = String.Empty;
            string result = null; 

            // Work out the base Url.
            IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
 
            if (host != null) {
                WebFormsRootDesigner rootDesigner = host.GetDesigner(host.RootComponent) as WebFormsRootDesigner; 
                if (rootDesigner != null) { 
                    baseUrl = rootDesigner.DocumentUrl;
                } 
            }
            if (baseUrl.Length == 0) {
#pragma warning disable 618
                IWebFormsDocumentService wfdServices = (IWebFormsDocumentService)serviceProvider.GetService(typeof(IWebFormsDocumentService)); 

                if (wfdServices != null) { 
                    baseUrl = wfdServices.DocumentUrl; 
                }
#pragma warning restore 618 
            }

            IWebFormsBuilderUIService builderService =
                (IWebFormsBuilderUIService)serviceProvider.GetService(typeof(IWebFormsBuilderUIService)); 
            if (builderService != null) {
                result = builderService.BuildUrl(owner, initialUrl, baseUrl, caption, filter, options); 
            } 

            return result; 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
