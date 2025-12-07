//------------------------------------------------------------------------------ 
// <copyright file="HttpCapabilitiesEvaluator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Security; 
    using System.Text;
    using System.Text.RegularExpressions; 
    using System.Threading; 
    using System.Web.Caching;
    using System.Web.Compilation; 
    using System.Web.Hosting;
    using System.Security.Permissions;

    // 
    // CapabilitiesEvaluator encapabilitiesulates a set of rules for deducing
    // a capabilities object from an HttpRequest 
    // 
    internal class HttpCapabilitiesEvaluator {
 
        internal CapabilitiesRule _rule;
        internal Hashtable _variables;
        internal Type _resultType;
        internal TimeSpan _cachetime; 
        internal string _cacheKeyPrefix;
 
        private int _userAgentCacheKeyLength; 
        private static int _idCounter;
        private const string _isMobileDeviceCapKey = "isMobileDevice"; 
        private static object _disableOptimisticCachingSingleton = new object();

        internal int UserAgentCacheKeyLength {
            get { 
                return _userAgentCacheKeyLength;
            } 
            set { 
                _userAgentCacheKeyLength = value;
            } 
        }

        //
        // internal constructor; inherit from parent 
        //
        internal HttpCapabilitiesEvaluator(HttpCapabilitiesEvaluator parent) { 
            int id = Interlocked.Increment(ref _idCounter); 
            // don't do id.ToString() on every request, do it here
            _cacheKeyPrefix = CacheInternal.PrefixHttpCapabilities + id.ToString(CultureInfo.InvariantCulture); 

            if (parent == null) {
                ClearParent();
            } 
            else {
                _rule = parent._rule; 
 
                if (parent._variables == null)
                    _variables = null; 
                else
                    _variables = new Hashtable(parent._variables);

                _cachetime = parent._cachetime; 
                _resultType = parent._resultType;
            } 
            // 
            AddDependency(String.Empty);
        } 

        internal BrowserCapabilitiesFactoryBase BrowserCapFactory {
            get {
                return BrowserCapabilitiesCompiler.BrowserCapabilitiesFactory; 
            }
        } 
 
        //
        // remove inheritance for <result inherit="false" /> 
        //
        internal virtual void ClearParent() {
            _rule = null;
            _cachetime = TimeSpan.FromSeconds(60); // one minute default expiry 
            _variables = new Hashtable();
            _resultType = typeof(HttpCapabilitiesBase); 
        } 

        // 
        // set <result cacheTime="ms" ... />
        //
        internal virtual void SetCacheTime(int sec) {
            _cachetime = TimeSpan.FromSeconds(sec); 
        }
 
        // 
        // add a dependency when we encounter a <use var="HTTP_ACCEPT_LANGUAGE" as="lang" />
        // 
        internal virtual void AddDependency(String variable) {
            if (variable.Equals("HTTP_USER_AGENT"))
                variable = String.Empty;
 
            _variables[variable] = true;
        } 
 
        //
        // sets the set of rules 
        //
        internal virtual void AddRuleList(ArrayList rulelist) {
            if (rulelist.Count == 0)
                return; 

            if (_rule != null) 
                rulelist.Insert(0, _rule); 

            _rule = new CapabilitiesSection(CapabilitiesRule.Filter, null, null, rulelist); 
        }

        internal static string GetUserAgent(HttpRequest request) {
            string userAgent; 

            if (request.ClientTarget.Length > 0) { 
                userAgent = GetUserAgentFromClientTarget( 
                    request.Context.ConfigurationPath, request.ClientTarget);
            } 
            else {
                userAgent = request.UserAgent;
            }
 
            // Protect against attacks with long User-Agent headers
            if (userAgent != null && userAgent.Length > 256) { 
                userAgent = userAgent.Substring(0, 256); 
            }
 
            return userAgent;
        }

        internal static string GetUserAgentFromClientTarget(VirtualPath configPath, string clientTarget) { 

            // Lookup ClientTarget section in config. 
            ClientTargetSection clientTargetConfig = RuntimeConfig.GetConfig(configPath).ClientTarget; 

            string userAgent = null; 

            if ( clientTargetConfig.ClientTargets[ clientTarget ] != null )
            {
                userAgent = clientTargetConfig.ClientTargets[ clientTarget ].UserAgent; 
            }
 
            if ( userAgent == null ) 
            {
                throw new HttpException(SR.GetString(SR.Invalid_client_target, clientTarget)); 
            }

            return userAgent;
        } 

        private void CacheBrowserCapResult(ref HttpCapabilitiesBase result) { 
            // Use the previously cached browserCap object if an identical 
            // browserCap is found.
            CacheInternal cacheInternal = System.Web.HttpRuntime.CacheInternal; 

            if (result.Capabilities == null) {
                return;
            } 

            string hashKey = CacheInternal.PrefixBrowserCapsHash; 
            StringBuilder builder = new StringBuilder(); 
            foreach (string attribute in result.Capabilities.Keys) {
                // Ignore useragent that is stored with empty key. 
                if (String.IsNullOrEmpty(attribute)) {
                    continue;
                }
 
                string value = (String)result.Capabilities[attribute];
                if (value != null) { 
                    builder.Append(attribute); 
                    builder.Append("$");
                    builder.Append(value); 
                    builder.Append("$");
                }
            }
            hashKey += builder.ToString().GetHashCode().ToString(CultureInfo.InvariantCulture); 

            HttpCapabilitiesBase newResult = cacheInternal.Get(hashKey) as HttpCapabilitiesBase; 
            if (newResult != null) { 
                result = newResult;
            } 
            else {
                cacheInternal.UtcInsert(hashKey, result);
            }
        } 

        // 
        // Actually computes the browser capabilities 
        //
        internal HttpCapabilitiesBase Evaluate(HttpRequest request) { 

            HttpCapabilitiesBase result;
            CacheInternal cacheInternal = System.Web.HttpRuntime.CacheInternal;
 
            //
            // 1) grab UA and do optimistic cache lookup (if UA is in dependency list) 
            // 
            string userAgent = GetUserAgent(request);
            string userAgentCacheKey = userAgent; 

            // Use the shorten userAgent as the cache key.
            Debug.Assert(UserAgentCacheKeyLength != 0);
            // Trim the useragent string based on <browserCaps> config 
            if (userAgentCacheKey != null && userAgentCacheKey.Length > UserAgentCacheKeyLength) {
                userAgentCacheKey = userAgentCacheKey.Substring(0, UserAgentCacheKeyLength); 
            } 

            bool doFullCacheKeyLookup = false; 
            string optimisticCacheKey = _cacheKeyPrefix + userAgentCacheKey;
            object optimisticCacheResult = cacheInternal.Get(optimisticCacheKey);

            // optimize for common case (desktop browser) 
            result = optimisticCacheResult as HttpCapabilitiesBase;
            if (result != null) { 
                return result; 
            }
 
            //
            // 1.1) optimistic cache entry could tell us to do full cache lookup
            //
            if (optimisticCacheResult == _disableOptimisticCachingSingleton) { 
                doFullCacheKeyLookup = true;
            } 
            else { 
                // cache it and respect _cachetime
                result = EvaluateFinal(request, true); 

                // Optimized cache key is disabled if the request matches any headers defined within identifications.
                if (result.UseOptimizedCacheKey) {
 
                    // Use the same browserCap result if one with the same capabilities can be found in the cache.
                    // This is to reduce the number of identical browserCap instances being cached. 
                    CacheBrowserCapResult(ref result); 

                    // Cache the result using the optimisicCacheKey 
                    cacheInternal.UtcInsert(optimisticCacheKey, result, null, Cache.NoAbsoluteExpiration, _cachetime);

                    return result;
                } 
            }
 
            // 
            // 2) either:
            // 
            //      We've never seen the UA before (parse all headers to
            //          determine if the new UA also carries modile device
            //          httpheaders).
            // 
            //      It's a mobile UA (so parse all headers) and do full
            //          cache lookup 
            // 
            //      UA isn't in dependency list (customer custom caps section)
            // 

            IDictionaryEnumerator de = _variables.GetEnumerator();
            StringBuilder sb = new StringBuilder(_cacheKeyPrefix);
 
            InternalSecurityPermissions.AspNetHostingPermissionLevelLow.Assert();
 
            while (de.MoveNext()) { 
                string key = (string)de.Key;
                string value; 

                if (key.Length == 0) {
                    value = userAgent;
                } 
                else {
                    value = request.ServerVariables[key]; 
                } 

                if (value != null) { 
                    sb.Append(value);
                }
            }
 
            CodeAccessPermission.RevertAssert();
 
            sb.Append(BrowserCapabilitiesFactoryBase.GetBrowserCapKey(BrowserCapFactory.InternalGetMatchedHeaders(), request)); 
            string fullCacheKey = sb.ToString();
 
            //
            // Only do full cache lookup if the optimistic cache
            // result was _disableOptimisticCachingSingleton or
            // if UserAgent wasn't in the cap var list. 
            //
            if (userAgent == null || doFullCacheKeyLookup) { 
 
                result = cacheInternal.Get(fullCacheKey) as HttpCapabilitiesBase;
 
                if (result != null)
                    return result;
            }
 
            result = EvaluateFinal(request, false);
 
            // Use the same browserCap result if one with the same matched nodes can be found in the cache. 
            // This is to reduce the number of identical browserCap instances being cached.
            CacheBrowserCapResult(ref result); 

             // cache it and respect _cachetime
            cacheInternal.UtcInsert(fullCacheKey, result, null, Cache.NoAbsoluteExpiration, _cachetime);
            if(optimisticCacheKey != null) { 
                cacheInternal.UtcInsert(optimisticCacheKey, _disableOptimisticCachingSingleton, null, Cache.NoAbsoluteExpiration, _cachetime);
            } 
 
            return result;
        } 

        internal HttpCapabilitiesBase EvaluateFinal(HttpRequest request, bool onlyEvaluateUserAgent) {
            HttpBrowserCapabilities browserCaps = BrowserCapFactory.GetHttpBrowserCapabilities(request);
            CapabilitiesState state = new CapabilitiesState(request, browserCaps.Capabilities); 
            if (onlyEvaluateUserAgent) {
                state.EvaluateOnlyUserAgent = true; 
            } 

            if(_rule != null) { 
                string oldIsMobileDevice = browserCaps[_isMobileDeviceCapKey];
                browserCaps.Capabilities[_isMobileDeviceCapKey] = null;

                _rule.Evaluate(state); 

                string newIsMobileDevice = browserCaps[_isMobileDeviceCapKey]; 
                if (newIsMobileDevice == null) { 
                    browserCaps.Capabilities[_isMobileDeviceCapKey] = oldIsMobileDevice;
                } 
                else if (newIsMobileDevice.Equals("true")) {
                    browserCaps.DisableOptimizedCacheKey();
                }
            } 

            // create the new type 
            // 
            HttpCapabilitiesBase result = (HttpCapabilitiesBase)HttpRuntime.CreateNonPublicInstance(_resultType);
            result.InitInternal(browserCaps); 

            return result;
        }
 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="HttpCapabilitiesEvaluator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Security; 
    using System.Text;
    using System.Text.RegularExpressions; 
    using System.Threading; 
    using System.Web.Caching;
    using System.Web.Compilation; 
    using System.Web.Hosting;
    using System.Security.Permissions;

    // 
    // CapabilitiesEvaluator encapabilitiesulates a set of rules for deducing
    // a capabilities object from an HttpRequest 
    // 
    internal class HttpCapabilitiesEvaluator {
 
        internal CapabilitiesRule _rule;
        internal Hashtable _variables;
        internal Type _resultType;
        internal TimeSpan _cachetime; 
        internal string _cacheKeyPrefix;
 
        private int _userAgentCacheKeyLength; 
        private static int _idCounter;
        private const string _isMobileDeviceCapKey = "isMobileDevice"; 
        private static object _disableOptimisticCachingSingleton = new object();

        internal int UserAgentCacheKeyLength {
            get { 
                return _userAgentCacheKeyLength;
            } 
            set { 
                _userAgentCacheKeyLength = value;
            } 
        }

        //
        // internal constructor; inherit from parent 
        //
        internal HttpCapabilitiesEvaluator(HttpCapabilitiesEvaluator parent) { 
            int id = Interlocked.Increment(ref _idCounter); 
            // don't do id.ToString() on every request, do it here
            _cacheKeyPrefix = CacheInternal.PrefixHttpCapabilities + id.ToString(CultureInfo.InvariantCulture); 

            if (parent == null) {
                ClearParent();
            } 
            else {
                _rule = parent._rule; 
 
                if (parent._variables == null)
                    _variables = null; 
                else
                    _variables = new Hashtable(parent._variables);

                _cachetime = parent._cachetime; 
                _resultType = parent._resultType;
            } 
            // 
            AddDependency(String.Empty);
        } 

        internal BrowserCapabilitiesFactoryBase BrowserCapFactory {
            get {
                return BrowserCapabilitiesCompiler.BrowserCapabilitiesFactory; 
            }
        } 
 
        //
        // remove inheritance for <result inherit="false" /> 
        //
        internal virtual void ClearParent() {
            _rule = null;
            _cachetime = TimeSpan.FromSeconds(60); // one minute default expiry 
            _variables = new Hashtable();
            _resultType = typeof(HttpCapabilitiesBase); 
        } 

        // 
        // set <result cacheTime="ms" ... />
        //
        internal virtual void SetCacheTime(int sec) {
            _cachetime = TimeSpan.FromSeconds(sec); 
        }
 
        // 
        // add a dependency when we encounter a <use var="HTTP_ACCEPT_LANGUAGE" as="lang" />
        // 
        internal virtual void AddDependency(String variable) {
            if (variable.Equals("HTTP_USER_AGENT"))
                variable = String.Empty;
 
            _variables[variable] = true;
        } 
 
        //
        // sets the set of rules 
        //
        internal virtual void AddRuleList(ArrayList rulelist) {
            if (rulelist.Count == 0)
                return; 

            if (_rule != null) 
                rulelist.Insert(0, _rule); 

            _rule = new CapabilitiesSection(CapabilitiesRule.Filter, null, null, rulelist); 
        }

        internal static string GetUserAgent(HttpRequest request) {
            string userAgent; 

            if (request.ClientTarget.Length > 0) { 
                userAgent = GetUserAgentFromClientTarget( 
                    request.Context.ConfigurationPath, request.ClientTarget);
            } 
            else {
                userAgent = request.UserAgent;
            }
 
            // Protect against attacks with long User-Agent headers
            if (userAgent != null && userAgent.Length > 256) { 
                userAgent = userAgent.Substring(0, 256); 
            }
 
            return userAgent;
        }

        internal static string GetUserAgentFromClientTarget(VirtualPath configPath, string clientTarget) { 

            // Lookup ClientTarget section in config. 
            ClientTargetSection clientTargetConfig = RuntimeConfig.GetConfig(configPath).ClientTarget; 

            string userAgent = null; 

            if ( clientTargetConfig.ClientTargets[ clientTarget ] != null )
            {
                userAgent = clientTargetConfig.ClientTargets[ clientTarget ].UserAgent; 
            }
 
            if ( userAgent == null ) 
            {
                throw new HttpException(SR.GetString(SR.Invalid_client_target, clientTarget)); 
            }

            return userAgent;
        } 

        private void CacheBrowserCapResult(ref HttpCapabilitiesBase result) { 
            // Use the previously cached browserCap object if an identical 
            // browserCap is found.
            CacheInternal cacheInternal = System.Web.HttpRuntime.CacheInternal; 

            if (result.Capabilities == null) {
                return;
            } 

            string hashKey = CacheInternal.PrefixBrowserCapsHash; 
            StringBuilder builder = new StringBuilder(); 
            foreach (string attribute in result.Capabilities.Keys) {
                // Ignore useragent that is stored with empty key. 
                if (String.IsNullOrEmpty(attribute)) {
                    continue;
                }
 
                string value = (String)result.Capabilities[attribute];
                if (value != null) { 
                    builder.Append(attribute); 
                    builder.Append("$");
                    builder.Append(value); 
                    builder.Append("$");
                }
            }
            hashKey += builder.ToString().GetHashCode().ToString(CultureInfo.InvariantCulture); 

            HttpCapabilitiesBase newResult = cacheInternal.Get(hashKey) as HttpCapabilitiesBase; 
            if (newResult != null) { 
                result = newResult;
            } 
            else {
                cacheInternal.UtcInsert(hashKey, result);
            }
        } 

        // 
        // Actually computes the browser capabilities 
        //
        internal HttpCapabilitiesBase Evaluate(HttpRequest request) { 

            HttpCapabilitiesBase result;
            CacheInternal cacheInternal = System.Web.HttpRuntime.CacheInternal;
 
            //
            // 1) grab UA and do optimistic cache lookup (if UA is in dependency list) 
            // 
            string userAgent = GetUserAgent(request);
            string userAgentCacheKey = userAgent; 

            // Use the shorten userAgent as the cache key.
            Debug.Assert(UserAgentCacheKeyLength != 0);
            // Trim the useragent string based on <browserCaps> config 
            if (userAgentCacheKey != null && userAgentCacheKey.Length > UserAgentCacheKeyLength) {
                userAgentCacheKey = userAgentCacheKey.Substring(0, UserAgentCacheKeyLength); 
            } 

            bool doFullCacheKeyLookup = false; 
            string optimisticCacheKey = _cacheKeyPrefix + userAgentCacheKey;
            object optimisticCacheResult = cacheInternal.Get(optimisticCacheKey);

            // optimize for common case (desktop browser) 
            result = optimisticCacheResult as HttpCapabilitiesBase;
            if (result != null) { 
                return result; 
            }
 
            //
            // 1.1) optimistic cache entry could tell us to do full cache lookup
            //
            if (optimisticCacheResult == _disableOptimisticCachingSingleton) { 
                doFullCacheKeyLookup = true;
            } 
            else { 
                // cache it and respect _cachetime
                result = EvaluateFinal(request, true); 

                // Optimized cache key is disabled if the request matches any headers defined within identifications.
                if (result.UseOptimizedCacheKey) {
 
                    // Use the same browserCap result if one with the same capabilities can be found in the cache.
                    // This is to reduce the number of identical browserCap instances being cached. 
                    CacheBrowserCapResult(ref result); 

                    // Cache the result using the optimisicCacheKey 
                    cacheInternal.UtcInsert(optimisticCacheKey, result, null, Cache.NoAbsoluteExpiration, _cachetime);

                    return result;
                } 
            }
 
            // 
            // 2) either:
            // 
            //      We've never seen the UA before (parse all headers to
            //          determine if the new UA also carries modile device
            //          httpheaders).
            // 
            //      It's a mobile UA (so parse all headers) and do full
            //          cache lookup 
            // 
            //      UA isn't in dependency list (customer custom caps section)
            // 

            IDictionaryEnumerator de = _variables.GetEnumerator();
            StringBuilder sb = new StringBuilder(_cacheKeyPrefix);
 
            InternalSecurityPermissions.AspNetHostingPermissionLevelLow.Assert();
 
            while (de.MoveNext()) { 
                string key = (string)de.Key;
                string value; 

                if (key.Length == 0) {
                    value = userAgent;
                } 
                else {
                    value = request.ServerVariables[key]; 
                } 

                if (value != null) { 
                    sb.Append(value);
                }
            }
 
            CodeAccessPermission.RevertAssert();
 
            sb.Append(BrowserCapabilitiesFactoryBase.GetBrowserCapKey(BrowserCapFactory.InternalGetMatchedHeaders(), request)); 
            string fullCacheKey = sb.ToString();
 
            //
            // Only do full cache lookup if the optimistic cache
            // result was _disableOptimisticCachingSingleton or
            // if UserAgent wasn't in the cap var list. 
            //
            if (userAgent == null || doFullCacheKeyLookup) { 
 
                result = cacheInternal.Get(fullCacheKey) as HttpCapabilitiesBase;
 
                if (result != null)
                    return result;
            }
 
            result = EvaluateFinal(request, false);
 
            // Use the same browserCap result if one with the same matched nodes can be found in the cache. 
            // This is to reduce the number of identical browserCap instances being cached.
            CacheBrowserCapResult(ref result); 

             // cache it and respect _cachetime
            cacheInternal.UtcInsert(fullCacheKey, result, null, Cache.NoAbsoluteExpiration, _cachetime);
            if(optimisticCacheKey != null) { 
                cacheInternal.UtcInsert(optimisticCacheKey, _disableOptimisticCachingSingleton, null, Cache.NoAbsoluteExpiration, _cachetime);
            } 
 
            return result;
        } 

        internal HttpCapabilitiesBase EvaluateFinal(HttpRequest request, bool onlyEvaluateUserAgent) {
            HttpBrowserCapabilities browserCaps = BrowserCapFactory.GetHttpBrowserCapabilities(request);
            CapabilitiesState state = new CapabilitiesState(request, browserCaps.Capabilities); 
            if (onlyEvaluateUserAgent) {
                state.EvaluateOnlyUserAgent = true; 
            } 

            if(_rule != null) { 
                string oldIsMobileDevice = browserCaps[_isMobileDeviceCapKey];
                browserCaps.Capabilities[_isMobileDeviceCapKey] = null;

                _rule.Evaluate(state); 

                string newIsMobileDevice = browserCaps[_isMobileDeviceCapKey]; 
                if (newIsMobileDevice == null) { 
                    browserCaps.Capabilities[_isMobileDeviceCapKey] = oldIsMobileDevice;
                } 
                else if (newIsMobileDevice.Equals("true")) {
                    browserCaps.DisableOptimizedCacheKey();
                }
            } 

            // create the new type 
            // 
            HttpCapabilitiesBase result = (HttpCapabilitiesBase)HttpRuntime.CreateNonPublicInstance(_resultType);
            result.InitInternal(browserCaps); 

            return result;
        }
 
    }
} 
