//------------------------------------------------------------------------------ 
// <copyright file="AssemblyResourceLoader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.Handlers {
    using System; 
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Globalization; 
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions; 
    using System.Security.Permissions;
    using System.Web; 
    using System.Web.Caching; 
    using System.Web.Compilation;
    using System.Web.Configuration; 
    using System.Web.Hosting;
    using System.Web.RegularExpressions;
    using System.Web.UI;
    using System.Web.Util; 
    using System.Collections.Generic;
 
 
    /// <devdoc>
    /// Provides a way to load client-side resources from assemblies 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public sealed class AssemblyResourceLoader : IHttpHandler {
        private const string _webResourceUrl = "WebResource.axd"; 

        private readonly static Regex webResourceRegex = new WebResourceRegex(); 
 
        private static IDictionary _urlCache = Hashtable.Synchronized(new Hashtable());
        private static IDictionary _assemblyInfoCache = Hashtable.Synchronized(new Hashtable()); 
        private static IDictionary _webResourceCache = Hashtable.Synchronized(new Hashtable());
        private static IDictionary _typeAssemblyCache = Hashtable.Synchronized(new Hashtable());

        // This group of fields is used for backwards compatibility. In v1.x you could 
        // technically customize the files in the /aspnet_client/ folder whereas in v2.x
        // we serve those files using WebResource.axd. These fields are used to check 
        // if there is a customized version of the file and use that instead of the resource. 
        private static bool _webFormsScriptChecked;
        private static VirtualPath _webFormsScriptLocation; 
        private static bool _webUIValidationScriptChecked;
        private static VirtualPath _webUIValidationScriptLocation;
        private static bool _smartNavScriptChecked;
        private static VirtualPath _smartNavScriptLocation; 
        private static bool _smartNavPageChecked;
        private static VirtualPath _smartNavPageLocation; 
 
        private static bool _handlerExistenceChecked;
        private static bool _handlerExists; 

        private static bool DebugMode {
            get {
                return HttpContext.Current.IsDebuggingEnabled; 
            }
        } 
 
        /// <devdoc>
        ///     Create a cache key for the UrlCache. 
        ///
        ///     requirement:  If assembly1 and assembly2 represent the same assembly,
        ///     then they must be the same object; otherwise this method will fail to generate
        ///     a unique cache key. 
        /// </devdoc>
        private static int CreateWebResourceUrlCacheKey(Assembly assembly, string resourceName, bool htmlEncoded) { 
            int hash = HashCodeCombiner.CombineHashCodes(assembly.GetHashCode(), resourceName.GetHashCode()); 
            return HashCodeCombiner.CombineHashCodes(hash, htmlEncoded.GetHashCode());
        } 

        /// <devdoc>
        /// Validates that the WebResource.axd handler is registered in config and actually
        /// points to the correct handler type. 
        /// </devdoc>
        private static void EnsureHandlerExistenceChecked() { 
            // First we have to check that the handler is registered: 
            // <add path="WebResource.axd" verb="GET" type="System.Web.Handlers.AssemblyResourceLoader" validate="True" />
            if (!_handlerExistenceChecked) { 
                HttpContext context = HttpContext.Current;
                IIS7WorkerRequest iis7WorkerRequest = (context != null) ? context.WorkerRequest as IIS7WorkerRequest : null;
                if (iis7WorkerRequest != null) {
                    // check the IIS <handlers> section by mapping the handler 
                    string handlerTypeString = iis7WorkerRequest.MapHandlerAndGetHandlerTypeString("GET", UrlPath.Combine(HttpRuntime.AppDomainAppVirtualPathString, _webResourceUrl), false /*convertNativeStaticFileModule*/);
                    if (!String.IsNullOrEmpty(handlerTypeString)) { 
                        _handlerExists = (typeof(AssemblyResourceLoader) == BuildManager.GetType(handlerTypeString, true /*throwOnFail*/, false /*ignoreCase*/)); 
                    }
                } 
                else {
                    // check the <httpHandlers> section
                    HttpHandlerAction httpHandler = RuntimeConfig.GetConfig().HttpHandlers.FindMapping("GET", VirtualPath.Create(_webResourceUrl));
                    _handlerExists = (httpHandler != null) && (httpHandler.TypeInternal == typeof(AssemblyResourceLoader)); 
                }
                _handlerExistenceChecked = true; 
            } 
        }
 
        /// <devdoc>
        ///     Performs the actual putting together of the resource reference URL.
        /// </devdoc>
        private static string FormatWebResourceUrl(string assemblyName, string resourceName, long assemblyDate, bool htmlEncoded) { 
            string encryptedData = Page.EncryptString(assemblyName + "|" + resourceName);
            if (htmlEncoded) { 
                return String.Format(CultureInfo.InvariantCulture, _webResourceUrl + "?d={0}&amp;t={1}", 
                                    encryptedData,
                                    assemblyDate); 
            }
            else {
                return String.Format(CultureInfo.InvariantCulture, _webResourceUrl + "?d={0}&t={1}",
                                    encryptedData, 
                                    assemblyDate);
            } 
        } 

        private static Pair GetAssemblyInfo(Assembly assembly) { 
            Pair assemblyInfo = _assemblyInfoCache[assembly] as Pair;
            if (assemblyInfo == null) {
                assemblyInfo = GetAssemblyInfoWithAssertInternal(assembly);
                _assemblyInfoCache[assembly] = assemblyInfo; 
            }
            Debug.Assert(assemblyInfo != null, "Assembly info should not be null"); 
            return assemblyInfo; 
        }
 
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        private static Pair GetAssemblyInfoWithAssertInternal(Assembly assembly) {
            AssemblyName assemblyName = assembly.GetName();
            long assemblyDate = File.GetLastWriteTime(new Uri(assemblyName.CodeBase).LocalPath).Ticks; 
            Pair assemblyInfo = new Pair(assemblyName, assemblyDate);
            return assemblyInfo; 
        } 

        /// <devdoc> 
        /// Gets the virtual path of a physical resource file. Null is
        /// returned if the resource does not exist.
        /// We assert full FileIOPermission so that we can map paths.
        /// </devdoc> 
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        private static VirtualPath GetDiskResourcePath(string resourceName) { 
            VirtualPath clientScriptsLocation = Util.GetScriptLocation(); 
            VirtualPath resourceVirtualPath = clientScriptsLocation.SimpleCombine(resourceName);
            string resourcePhysicalPath = resourceVirtualPath.MapPath(); 
            if (File.Exists(resourcePhysicalPath)) {
                return resourceVirtualPath;
            }
            else { 
                return null;
            } 
        } 

        internal static string GetWebResourceUrl(Type type, string resourceName) { 
            return GetWebResourceUrl(type, resourceName, false);
        }

        /// <devdoc> 
        ///     Gets a URL resource reference to a client-side resource
        /// </devdoc> 
        internal static string GetWebResourceUrl(Type type, string resourceName, bool htmlEncoded) { 
            Assembly assembly = (Assembly)_typeAssemblyCache[type];
            if (assembly == null) { 
                assembly = type.Assembly;
                _typeAssemblyCache[type] = assembly;
            }
 
            // If the resource request is for System.Web.dll and more specifically
            // it is for a file that we shipped in v1.x, we have to check if a 
            // customized copy of the file exists. See notes at the top of the file 
            // regarding this.
            if (assembly == typeof(AssemblyResourceLoader).Assembly) { 
                if (String.Equals(resourceName, "WebForms.js", StringComparison.Ordinal)) {
                    if (!_webFormsScriptChecked) {
                        _webFormsScriptLocation = GetDiskResourcePath(resourceName);
                        _webFormsScriptChecked = true; 
                    }
                    if (_webFormsScriptLocation != null) { 
                        return _webFormsScriptLocation.VirtualPathString; 
                    }
                } 
                else if (String.Equals(resourceName, "WebUIValidation.js", StringComparison.Ordinal)) {
                    if (!_webUIValidationScriptChecked) {
                        _webUIValidationScriptLocation = GetDiskResourcePath(resourceName);
                        _webUIValidationScriptChecked = true; 
                    }
                    if (_webUIValidationScriptLocation != null) { 
                        return _webUIValidationScriptLocation.VirtualPathString; 
                    }
                } 
                else if (String.Equals(resourceName, "SmartNav.htm", StringComparison.Ordinal)) {
                    if (!_smartNavPageChecked) {
                        _smartNavPageLocation = GetDiskResourcePath(resourceName);
                        _smartNavPageChecked = true; 
                    }
                    if (_smartNavPageLocation != null) { 
                        return _smartNavPageLocation.VirtualPathString; 
                    }
                } 
                else if (String.Equals(resourceName, "SmartNav.js", StringComparison.Ordinal)) {
                    if (!_smartNavScriptChecked) {
                        _smartNavScriptLocation = GetDiskResourcePath(resourceName);
                        _smartNavScriptChecked = true; 
                    }
                    if (_smartNavScriptLocation != null) { 
                        return _smartNavScriptLocation.VirtualPathString; 
                    }
                } 
            }

            return UrlPath.Combine(HttpRuntime.AppDomainAppVirtualPathString, GetWebResourceUrlInternal(assembly, resourceName, htmlEncoded));
        } 

        private static string GetWebResourceUrlInternal(Assembly assembly, string resourceName, bool htmlEncoded) { 
            EnsureHandlerExistenceChecked(); 
            if (!_handlerExists) {
                throw new InvalidOperationException(SR.GetString(SR.AssemblyResourceLoader_HandlerNotRegistered)); 
            }

            Pair assemblyInfo = GetAssemblyInfo(assembly);
 
            AssemblyName assemblyName = (AssemblyName)assemblyInfo.First;
            long assemblyDate = (long)assemblyInfo.Second; 
            string assemblyVersion = assemblyName.Version.ToString(); 

            int urlCacheKey = CreateWebResourceUrlCacheKey(assembly, resourceName, htmlEncoded); 

            string url = (string)_urlCache[urlCacheKey];

            if (url == null) { 
                string urlAssemblyName;
 
                if (assembly.GlobalAssemblyCache) { 
                    // If the assembly is in the GAC, we need to store a full name to load the assembly later
                    if (assembly == HttpContext.SystemWebAssembly) { 
                        urlAssemblyName = "s";
                    }
                    else {
                        // Pack the necessary values into a more compact format than FullName 
                        StringBuilder builder = new StringBuilder();
                        builder.Append('f'); 
                        builder.Append(assemblyName.Name); 
                        builder.Append(',');
                        builder.Append(assemblyVersion); 
                        builder.Append(',');
                        if (assemblyName.CultureInfo != null) {
                            builder.Append(assemblyName.CultureInfo.ToString());
                        } 
                        builder.Append(',');
                        byte[] token = assemblyName.GetPublicKeyToken(); 
                        for (int i = 0; i < token.Length; i++) { 
                            builder.Append(token[i].ToString("x2", CultureInfo.InvariantCulture));
                        } 
                        urlAssemblyName = builder.ToString();
                    }
                }
                else { 
                    // Otherwise, we can just use a partial name
                    urlAssemblyName = "p" + assemblyName.Name; 
                } 

                url = FormatWebResourceUrl(urlAssemblyName, resourceName, assemblyDate, htmlEncoded); 

                _urlCache[urlCacheKey] = url;
            }
 
            return url;
        } 
 
        internal static bool IsValidWebResourceRequest(HttpContext context) {
            EnsureHandlerExistenceChecked(); 
            if (!_handlerExists) {
                // If the handler isn't properly registered, it can't
                // possibly be a valid web resource request.
                return false; 
            }
 
            string webResourceHandlerUrl = UrlPath.Combine(HttpRuntime.AppDomainAppVirtualPathString, _webResourceUrl); 
            string requestPath = context.Request.Path;
            if (String.Equals(requestPath, webResourceHandlerUrl, StringComparison.OrdinalIgnoreCase)) { 
                return true;
            }

            return false; 
        }
 
        /// <internalonly/> 
        bool IHttpHandler.IsReusable {
            get { 
                return true;
            }
        }
 

        /// <internalonly/> 
        void IHttpHandler.ProcessRequest(HttpContext context) { 
            // Make sure we don't get any extra content in this handler (like Application.BeginRequest stuff);
            context.Response.Clear(); 

            NameValueCollection queryString = context.Request.QueryString;

            string encryptedData = queryString["d"]; 
            if (String.IsNullOrEmpty(encryptedData)) {
                throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_InvalidRequest)); 
            } 

            string decryptedData = Page.DecryptString(encryptedData); 

            int separatorIndex = decryptedData.IndexOf('|');
            Debug.Assert(separatorIndex != -1, "The decrypted data must contain a separator.");
 
            string assemblyName = decryptedData.Substring(0, separatorIndex);
            if (String.IsNullOrEmpty(assemblyName)) { 
                throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_AssemblyNotFound, assemblyName)); 
            }
 
            string resourceName = decryptedData.Substring(separatorIndex + 1);
            if (String.IsNullOrEmpty(resourceName)) {
                throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_ResourceNotFound, resourceName));
            } 

            char nameType = assemblyName[0]; 
            assemblyName = assemblyName.Substring(1); 

            Assembly assembly = null; 

            // If it was a full name, create an AssemblyName and load from that
            if (nameType == 'f') {
                string[] parts = assemblyName.Split(','); 

                if (parts.Length != 4) { 
                    throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_InvalidRequest)); 
                }
 
                AssemblyName realName = new AssemblyName();
                realName.Name = parts[0];
                realName.Version = new Version(parts[1]);
                string cultureString = parts[2]; 

                // Try to determine the culture, using the invariant culture if there wasn't one (doesn't work without it) 
                if (cultureString.Length > 0) { 
                    realName.CultureInfo = new CultureInfo(cultureString);
                } 
                else {
                    realName.CultureInfo = CultureInfo.InvariantCulture;
                }
 
                // Parse up the public key token which is represented as hex bytes in a string
                string token = parts[3]; 
                byte[] tokenBytes = new byte[token.Length / 2]; 
                for (int i = 0; i < tokenBytes.Length; i++) {
                    tokenBytes[i] = Byte.Parse(token.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture); 
                }
                realName.SetPublicKeyToken(tokenBytes);

                assembly = Assembly.Load(realName); 
            }
            // System.Web special case 
            else if (nameType == 's') { 
                assembly = typeof(AssemblyResourceLoader).Assembly;
            } 
            // If was a partial name, just try to load it
            else if (nameType == 'p') {
                assembly = Assembly.Load(assemblyName);
            } 
            else {
                throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_InvalidRequest)); 
            } 

            bool performSubstitution = false; 
            bool validResource = false;
            string contentType = String.Empty;

            if (assembly != null) { 
                // Check the validation cache to see if the resource has already been validated
                int cacheKey = HashCodeCombiner.CombineHashCodes(assembly.GetHashCode(), resourceName.GetHashCode()); 
                Triplet resourceTriplet = (Triplet)_webResourceCache[cacheKey]; 
                if (resourceTriplet != null) {
                    validResource = (bool)resourceTriplet.First; 
                    contentType = (string)resourceTriplet.Second;
                    performSubstitution = (bool)resourceTriplet.Third;
                }
                else { 
                    // Validation cache is empty, find out if it's valid and add it to the cache
                    object[] attrs = assembly.GetCustomAttributes(false); 
                    for (int i = 0; i < attrs.Length; i++) { 
                        WebResourceAttribute wra = attrs[i] as WebResourceAttribute;
                        if (wra != null) { 
                            if (string.Compare(wra.WebResource, resourceName, StringComparison.Ordinal) == 0) {
                                // For case insensitivity, we want the real name from the assembly.
                                resourceName = wra.WebResource;
                                validResource = true; 
                                contentType = wra.ContentType;
                                performSubstitution = wra.PerformSubstitution; 
                                break; 
                            }
                        } 
                    }

                    // Cache the results so we don't have to do this again
                    Triplet triplet = new Triplet(); 
                    triplet.First = validResource;
                    triplet.Second = contentType; 
                    triplet.Third = performSubstitution; 
                    _webResourceCache[cacheKey] = triplet;
                } 

                if (validResource) {
                    // Cache the resource so we don't keep processing the same requests
                    HttpCachePolicy cachePolicy = context.Response.Cache; 
                    cachePolicy.SetCacheability(HttpCacheability.Public);
                    cachePolicy.VaryByParams["d"] = true; 
                    cachePolicy.SetOmitVaryStar(true); 
                    cachePolicy.SetExpires(DateTime.Now + TimeSpan.FromDays(365));
                    cachePolicy.SetValidUntilExpires(true); 
                    Pair assemblyInfo = GetAssemblyInfo(assembly);
                    cachePolicy.SetLastModified(new DateTime((long)assemblyInfo.Second));

 
                    Stream resourceStream = null;
                    StreamReader reader = null; 
                    try { 
                        // Get the resource stream from the assembly and stream it out to the response
                        resourceStream = assembly.GetManifestResourceStream(resourceName); 
                        if (resourceStream != null) {
                            context.Response.ContentType = contentType;

                            if (performSubstitution) { 
                                //
                                reader = new StreamReader(resourceStream, true); 
 
                                string content = reader.ReadToEnd();
 
                                // Looking for something of the form: WebResource("resourcename")
                                MatchCollection matches = webResourceRegex.Matches(content);
                                int startIndex = 0;
                                StringBuilder newContent = new StringBuilder(); 
                                foreach (Match match in matches) {
                                    newContent.Append(content.Substring(startIndex, match.Index - startIndex)); 
 
                                    Group group = match.Groups["resourceName"];
                                    if (group != null) { 
                                        string embeddedResourceName = group.ToString();
                                        if (embeddedResourceName.Length > 0) {
                                            //
                                            if (String.Equals(embeddedResourceName, resourceName, StringComparison.Ordinal)) { 
                                                throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_NoCircularReferences, resourceName));
                                            } 
                                            newContent.Append(GetWebResourceUrlInternal(assembly, embeddedResourceName, false)); 
                                        }
                                    } 

                                    startIndex = match.Index + match.Length;
                                }
 
                                newContent.Append(content.Substring(startIndex, content.Length - startIndex));
 
                                StreamWriter writer = new StreamWriter(context.Response.OutputStream, reader.CurrentEncoding); 
                                writer.Write(newContent.ToString());
                                writer.Flush(); 
                            }
                            else {
                                byte[] buffer = new byte[1024];
                                Stream outputStream = context.Response.OutputStream; 
                                int count = 1;
                                while (count > 0) { 
                                    count = resourceStream.Read(buffer, 0, 1024); 
                                    outputStream.Write(buffer, 0, count);
                                } 
                                outputStream.Flush();
                            }
                        }
                    } 
                    finally {
                        if (reader != null) 
                            reader.Close(); 
                        if (resourceStream != null)
                            resourceStream.Close(); 
                    }
                }
                else {
                    throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_InvalidRequest, assemblyName)); 
                }
            } 
 
            context.Response.IgnoreFurtherWrites();
        } 

    }
}
//------------------------------------------------------------------------------ 
// <copyright file="AssemblyResourceLoader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.Handlers {
    using System; 
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Globalization; 
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions; 
    using System.Security.Permissions;
    using System.Web; 
    using System.Web.Caching; 
    using System.Web.Compilation;
    using System.Web.Configuration; 
    using System.Web.Hosting;
    using System.Web.RegularExpressions;
    using System.Web.UI;
    using System.Web.Util; 
    using System.Collections.Generic;
 
 
    /// <devdoc>
    /// Provides a way to load client-side resources from assemblies 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public sealed class AssemblyResourceLoader : IHttpHandler {
        private const string _webResourceUrl = "WebResource.axd"; 

        private readonly static Regex webResourceRegex = new WebResourceRegex(); 
 
        private static IDictionary _urlCache = Hashtable.Synchronized(new Hashtable());
        private static IDictionary _assemblyInfoCache = Hashtable.Synchronized(new Hashtable()); 
        private static IDictionary _webResourceCache = Hashtable.Synchronized(new Hashtable());
        private static IDictionary _typeAssemblyCache = Hashtable.Synchronized(new Hashtable());

        // This group of fields is used for backwards compatibility. In v1.x you could 
        // technically customize the files in the /aspnet_client/ folder whereas in v2.x
        // we serve those files using WebResource.axd. These fields are used to check 
        // if there is a customized version of the file and use that instead of the resource. 
        private static bool _webFormsScriptChecked;
        private static VirtualPath _webFormsScriptLocation; 
        private static bool _webUIValidationScriptChecked;
        private static VirtualPath _webUIValidationScriptLocation;
        private static bool _smartNavScriptChecked;
        private static VirtualPath _smartNavScriptLocation; 
        private static bool _smartNavPageChecked;
        private static VirtualPath _smartNavPageLocation; 
 
        private static bool _handlerExistenceChecked;
        private static bool _handlerExists; 

        private static bool DebugMode {
            get {
                return HttpContext.Current.IsDebuggingEnabled; 
            }
        } 
 
        /// <devdoc>
        ///     Create a cache key for the UrlCache. 
        ///
        ///     requirement:  If assembly1 and assembly2 represent the same assembly,
        ///     then they must be the same object; otherwise this method will fail to generate
        ///     a unique cache key. 
        /// </devdoc>
        private static int CreateWebResourceUrlCacheKey(Assembly assembly, string resourceName, bool htmlEncoded) { 
            int hash = HashCodeCombiner.CombineHashCodes(assembly.GetHashCode(), resourceName.GetHashCode()); 
            return HashCodeCombiner.CombineHashCodes(hash, htmlEncoded.GetHashCode());
        } 

        /// <devdoc>
        /// Validates that the WebResource.axd handler is registered in config and actually
        /// points to the correct handler type. 
        /// </devdoc>
        private static void EnsureHandlerExistenceChecked() { 
            // First we have to check that the handler is registered: 
            // <add path="WebResource.axd" verb="GET" type="System.Web.Handlers.AssemblyResourceLoader" validate="True" />
            if (!_handlerExistenceChecked) { 
                HttpContext context = HttpContext.Current;
                IIS7WorkerRequest iis7WorkerRequest = (context != null) ? context.WorkerRequest as IIS7WorkerRequest : null;
                if (iis7WorkerRequest != null) {
                    // check the IIS <handlers> section by mapping the handler 
                    string handlerTypeString = iis7WorkerRequest.MapHandlerAndGetHandlerTypeString("GET", UrlPath.Combine(HttpRuntime.AppDomainAppVirtualPathString, _webResourceUrl), false /*convertNativeStaticFileModule*/);
                    if (!String.IsNullOrEmpty(handlerTypeString)) { 
                        _handlerExists = (typeof(AssemblyResourceLoader) == BuildManager.GetType(handlerTypeString, true /*throwOnFail*/, false /*ignoreCase*/)); 
                    }
                } 
                else {
                    // check the <httpHandlers> section
                    HttpHandlerAction httpHandler = RuntimeConfig.GetConfig().HttpHandlers.FindMapping("GET", VirtualPath.Create(_webResourceUrl));
                    _handlerExists = (httpHandler != null) && (httpHandler.TypeInternal == typeof(AssemblyResourceLoader)); 
                }
                _handlerExistenceChecked = true; 
            } 
        }
 
        /// <devdoc>
        ///     Performs the actual putting together of the resource reference URL.
        /// </devdoc>
        private static string FormatWebResourceUrl(string assemblyName, string resourceName, long assemblyDate, bool htmlEncoded) { 
            string encryptedData = Page.EncryptString(assemblyName + "|" + resourceName);
            if (htmlEncoded) { 
                return String.Format(CultureInfo.InvariantCulture, _webResourceUrl + "?d={0}&amp;t={1}", 
                                    encryptedData,
                                    assemblyDate); 
            }
            else {
                return String.Format(CultureInfo.InvariantCulture, _webResourceUrl + "?d={0}&t={1}",
                                    encryptedData, 
                                    assemblyDate);
            } 
        } 

        private static Pair GetAssemblyInfo(Assembly assembly) { 
            Pair assemblyInfo = _assemblyInfoCache[assembly] as Pair;
            if (assemblyInfo == null) {
                assemblyInfo = GetAssemblyInfoWithAssertInternal(assembly);
                _assemblyInfoCache[assembly] = assemblyInfo; 
            }
            Debug.Assert(assemblyInfo != null, "Assembly info should not be null"); 
            return assemblyInfo; 
        }
 
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        private static Pair GetAssemblyInfoWithAssertInternal(Assembly assembly) {
            AssemblyName assemblyName = assembly.GetName();
            long assemblyDate = File.GetLastWriteTime(new Uri(assemblyName.CodeBase).LocalPath).Ticks; 
            Pair assemblyInfo = new Pair(assemblyName, assemblyDate);
            return assemblyInfo; 
        } 

        /// <devdoc> 
        /// Gets the virtual path of a physical resource file. Null is
        /// returned if the resource does not exist.
        /// We assert full FileIOPermission so that we can map paths.
        /// </devdoc> 
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        private static VirtualPath GetDiskResourcePath(string resourceName) { 
            VirtualPath clientScriptsLocation = Util.GetScriptLocation(); 
            VirtualPath resourceVirtualPath = clientScriptsLocation.SimpleCombine(resourceName);
            string resourcePhysicalPath = resourceVirtualPath.MapPath(); 
            if (File.Exists(resourcePhysicalPath)) {
                return resourceVirtualPath;
            }
            else { 
                return null;
            } 
        } 

        internal static string GetWebResourceUrl(Type type, string resourceName) { 
            return GetWebResourceUrl(type, resourceName, false);
        }

        /// <devdoc> 
        ///     Gets a URL resource reference to a client-side resource
        /// </devdoc> 
        internal static string GetWebResourceUrl(Type type, string resourceName, bool htmlEncoded) { 
            Assembly assembly = (Assembly)_typeAssemblyCache[type];
            if (assembly == null) { 
                assembly = type.Assembly;
                _typeAssemblyCache[type] = assembly;
            }
 
            // If the resource request is for System.Web.dll and more specifically
            // it is for a file that we shipped in v1.x, we have to check if a 
            // customized copy of the file exists. See notes at the top of the file 
            // regarding this.
            if (assembly == typeof(AssemblyResourceLoader).Assembly) { 
                if (String.Equals(resourceName, "WebForms.js", StringComparison.Ordinal)) {
                    if (!_webFormsScriptChecked) {
                        _webFormsScriptLocation = GetDiskResourcePath(resourceName);
                        _webFormsScriptChecked = true; 
                    }
                    if (_webFormsScriptLocation != null) { 
                        return _webFormsScriptLocation.VirtualPathString; 
                    }
                } 
                else if (String.Equals(resourceName, "WebUIValidation.js", StringComparison.Ordinal)) {
                    if (!_webUIValidationScriptChecked) {
                        _webUIValidationScriptLocation = GetDiskResourcePath(resourceName);
                        _webUIValidationScriptChecked = true; 
                    }
                    if (_webUIValidationScriptLocation != null) { 
                        return _webUIValidationScriptLocation.VirtualPathString; 
                    }
                } 
                else if (String.Equals(resourceName, "SmartNav.htm", StringComparison.Ordinal)) {
                    if (!_smartNavPageChecked) {
                        _smartNavPageLocation = GetDiskResourcePath(resourceName);
                        _smartNavPageChecked = true; 
                    }
                    if (_smartNavPageLocation != null) { 
                        return _smartNavPageLocation.VirtualPathString; 
                    }
                } 
                else if (String.Equals(resourceName, "SmartNav.js", StringComparison.Ordinal)) {
                    if (!_smartNavScriptChecked) {
                        _smartNavScriptLocation = GetDiskResourcePath(resourceName);
                        _smartNavScriptChecked = true; 
                    }
                    if (_smartNavScriptLocation != null) { 
                        return _smartNavScriptLocation.VirtualPathString; 
                    }
                } 
            }

            return UrlPath.Combine(HttpRuntime.AppDomainAppVirtualPathString, GetWebResourceUrlInternal(assembly, resourceName, htmlEncoded));
        } 

        private static string GetWebResourceUrlInternal(Assembly assembly, string resourceName, bool htmlEncoded) { 
            EnsureHandlerExistenceChecked(); 
            if (!_handlerExists) {
                throw new InvalidOperationException(SR.GetString(SR.AssemblyResourceLoader_HandlerNotRegistered)); 
            }

            Pair assemblyInfo = GetAssemblyInfo(assembly);
 
            AssemblyName assemblyName = (AssemblyName)assemblyInfo.First;
            long assemblyDate = (long)assemblyInfo.Second; 
            string assemblyVersion = assemblyName.Version.ToString(); 

            int urlCacheKey = CreateWebResourceUrlCacheKey(assembly, resourceName, htmlEncoded); 

            string url = (string)_urlCache[urlCacheKey];

            if (url == null) { 
                string urlAssemblyName;
 
                if (assembly.GlobalAssemblyCache) { 
                    // If the assembly is in the GAC, we need to store a full name to load the assembly later
                    if (assembly == HttpContext.SystemWebAssembly) { 
                        urlAssemblyName = "s";
                    }
                    else {
                        // Pack the necessary values into a more compact format than FullName 
                        StringBuilder builder = new StringBuilder();
                        builder.Append('f'); 
                        builder.Append(assemblyName.Name); 
                        builder.Append(',');
                        builder.Append(assemblyVersion); 
                        builder.Append(',');
                        if (assemblyName.CultureInfo != null) {
                            builder.Append(assemblyName.CultureInfo.ToString());
                        } 
                        builder.Append(',');
                        byte[] token = assemblyName.GetPublicKeyToken(); 
                        for (int i = 0; i < token.Length; i++) { 
                            builder.Append(token[i].ToString("x2", CultureInfo.InvariantCulture));
                        } 
                        urlAssemblyName = builder.ToString();
                    }
                }
                else { 
                    // Otherwise, we can just use a partial name
                    urlAssemblyName = "p" + assemblyName.Name; 
                } 

                url = FormatWebResourceUrl(urlAssemblyName, resourceName, assemblyDate, htmlEncoded); 

                _urlCache[urlCacheKey] = url;
            }
 
            return url;
        } 
 
        internal static bool IsValidWebResourceRequest(HttpContext context) {
            EnsureHandlerExistenceChecked(); 
            if (!_handlerExists) {
                // If the handler isn't properly registered, it can't
                // possibly be a valid web resource request.
                return false; 
            }
 
            string webResourceHandlerUrl = UrlPath.Combine(HttpRuntime.AppDomainAppVirtualPathString, _webResourceUrl); 
            string requestPath = context.Request.Path;
            if (String.Equals(requestPath, webResourceHandlerUrl, StringComparison.OrdinalIgnoreCase)) { 
                return true;
            }

            return false; 
        }
 
        /// <internalonly/> 
        bool IHttpHandler.IsReusable {
            get { 
                return true;
            }
        }
 

        /// <internalonly/> 
        void IHttpHandler.ProcessRequest(HttpContext context) { 
            // Make sure we don't get any extra content in this handler (like Application.BeginRequest stuff);
            context.Response.Clear(); 

            NameValueCollection queryString = context.Request.QueryString;

            string encryptedData = queryString["d"]; 
            if (String.IsNullOrEmpty(encryptedData)) {
                throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_InvalidRequest)); 
            } 

            string decryptedData = Page.DecryptString(encryptedData); 

            int separatorIndex = decryptedData.IndexOf('|');
            Debug.Assert(separatorIndex != -1, "The decrypted data must contain a separator.");
 
            string assemblyName = decryptedData.Substring(0, separatorIndex);
            if (String.IsNullOrEmpty(assemblyName)) { 
                throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_AssemblyNotFound, assemblyName)); 
            }
 
            string resourceName = decryptedData.Substring(separatorIndex + 1);
            if (String.IsNullOrEmpty(resourceName)) {
                throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_ResourceNotFound, resourceName));
            } 

            char nameType = assemblyName[0]; 
            assemblyName = assemblyName.Substring(1); 

            Assembly assembly = null; 

            // If it was a full name, create an AssemblyName and load from that
            if (nameType == 'f') {
                string[] parts = assemblyName.Split(','); 

                if (parts.Length != 4) { 
                    throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_InvalidRequest)); 
                }
 
                AssemblyName realName = new AssemblyName();
                realName.Name = parts[0];
                realName.Version = new Version(parts[1]);
                string cultureString = parts[2]; 

                // Try to determine the culture, using the invariant culture if there wasn't one (doesn't work without it) 
                if (cultureString.Length > 0) { 
                    realName.CultureInfo = new CultureInfo(cultureString);
                } 
                else {
                    realName.CultureInfo = CultureInfo.InvariantCulture;
                }
 
                // Parse up the public key token which is represented as hex bytes in a string
                string token = parts[3]; 
                byte[] tokenBytes = new byte[token.Length / 2]; 
                for (int i = 0; i < tokenBytes.Length; i++) {
                    tokenBytes[i] = Byte.Parse(token.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture); 
                }
                realName.SetPublicKeyToken(tokenBytes);

                assembly = Assembly.Load(realName); 
            }
            // System.Web special case 
            else if (nameType == 's') { 
                assembly = typeof(AssemblyResourceLoader).Assembly;
            } 
            // If was a partial name, just try to load it
            else if (nameType == 'p') {
                assembly = Assembly.Load(assemblyName);
            } 
            else {
                throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_InvalidRequest)); 
            } 

            bool performSubstitution = false; 
            bool validResource = false;
            string contentType = String.Empty;

            if (assembly != null) { 
                // Check the validation cache to see if the resource has already been validated
                int cacheKey = HashCodeCombiner.CombineHashCodes(assembly.GetHashCode(), resourceName.GetHashCode()); 
                Triplet resourceTriplet = (Triplet)_webResourceCache[cacheKey]; 
                if (resourceTriplet != null) {
                    validResource = (bool)resourceTriplet.First; 
                    contentType = (string)resourceTriplet.Second;
                    performSubstitution = (bool)resourceTriplet.Third;
                }
                else { 
                    // Validation cache is empty, find out if it's valid and add it to the cache
                    object[] attrs = assembly.GetCustomAttributes(false); 
                    for (int i = 0; i < attrs.Length; i++) { 
                        WebResourceAttribute wra = attrs[i] as WebResourceAttribute;
                        if (wra != null) { 
                            if (string.Compare(wra.WebResource, resourceName, StringComparison.Ordinal) == 0) {
                                // For case insensitivity, we want the real name from the assembly.
                                resourceName = wra.WebResource;
                                validResource = true; 
                                contentType = wra.ContentType;
                                performSubstitution = wra.PerformSubstitution; 
                                break; 
                            }
                        } 
                    }

                    // Cache the results so we don't have to do this again
                    Triplet triplet = new Triplet(); 
                    triplet.First = validResource;
                    triplet.Second = contentType; 
                    triplet.Third = performSubstitution; 
                    _webResourceCache[cacheKey] = triplet;
                } 

                if (validResource) {
                    // Cache the resource so we don't keep processing the same requests
                    HttpCachePolicy cachePolicy = context.Response.Cache; 
                    cachePolicy.SetCacheability(HttpCacheability.Public);
                    cachePolicy.VaryByParams["d"] = true; 
                    cachePolicy.SetOmitVaryStar(true); 
                    cachePolicy.SetExpires(DateTime.Now + TimeSpan.FromDays(365));
                    cachePolicy.SetValidUntilExpires(true); 
                    Pair assemblyInfo = GetAssemblyInfo(assembly);
                    cachePolicy.SetLastModified(new DateTime((long)assemblyInfo.Second));

 
                    Stream resourceStream = null;
                    StreamReader reader = null; 
                    try { 
                        // Get the resource stream from the assembly and stream it out to the response
                        resourceStream = assembly.GetManifestResourceStream(resourceName); 
                        if (resourceStream != null) {
                            context.Response.ContentType = contentType;

                            if (performSubstitution) { 
                                //
                                reader = new StreamReader(resourceStream, true); 
 
                                string content = reader.ReadToEnd();
 
                                // Looking for something of the form: WebResource("resourcename")
                                MatchCollection matches = webResourceRegex.Matches(content);
                                int startIndex = 0;
                                StringBuilder newContent = new StringBuilder(); 
                                foreach (Match match in matches) {
                                    newContent.Append(content.Substring(startIndex, match.Index - startIndex)); 
 
                                    Group group = match.Groups["resourceName"];
                                    if (group != null) { 
                                        string embeddedResourceName = group.ToString();
                                        if (embeddedResourceName.Length > 0) {
                                            //
                                            if (String.Equals(embeddedResourceName, resourceName, StringComparison.Ordinal)) { 
                                                throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_NoCircularReferences, resourceName));
                                            } 
                                            newContent.Append(GetWebResourceUrlInternal(assembly, embeddedResourceName, false)); 
                                        }
                                    } 

                                    startIndex = match.Index + match.Length;
                                }
 
                                newContent.Append(content.Substring(startIndex, content.Length - startIndex));
 
                                StreamWriter writer = new StreamWriter(context.Response.OutputStream, reader.CurrentEncoding); 
                                writer.Write(newContent.ToString());
                                writer.Flush(); 
                            }
                            else {
                                byte[] buffer = new byte[1024];
                                Stream outputStream = context.Response.OutputStream; 
                                int count = 1;
                                while (count > 0) { 
                                    count = resourceStream.Read(buffer, 0, 1024); 
                                    outputStream.Write(buffer, 0, count);
                                } 
                                outputStream.Flush();
                            }
                        }
                    } 
                    finally {
                        if (reader != null) 
                            reader.Close(); 
                        if (resourceStream != null)
                            resourceStream.Close(); 
                    }
                }
                else {
                    throw new HttpException(404, SR.GetString(SR.AssemblyResourceLoader_InvalidRequest, assemblyName)); 
                }
            } 
 
            context.Response.IgnoreFurtherWrites();
        } 

    }
}
