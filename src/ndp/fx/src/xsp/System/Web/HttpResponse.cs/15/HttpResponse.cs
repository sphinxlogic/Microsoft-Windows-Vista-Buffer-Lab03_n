//------------------------------------------------------------------------------ 
// <copyright file="HttpResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Response intrinsic 
 */
namespace System.Web { 

    using System.Text;
    using System.Threading;
    using System.Runtime.Serialization; 
    using System.IO;
    using System.Collections; 
    using System.Collections.Specialized; 
    using System.Globalization;
    using System.Web.Util; 
    using System.Web.Hosting;
    using System.Web.Caching;
    using System.Web.Configuration;
    using System.Web.UI; 
    using System.Configuration;
    using System.Security.Permissions; 
    using System.Web.Management; 

 
    /// <devdoc>
    ///    <para>Used in HttpResponse.WriteSubstitution.</para>
    /// </devdoc>
    public delegate String HttpResponseSubstitutionCallback(HttpContext context); 

 
    /// <devdoc> 
    ///    <para> Enables type-safe server to browser communication. Used to
    ///       send Http output to a client.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class HttpResponse {
        private HttpWorkerRequest _wr;              // some response have HttpWorkerRequest 
        private HttpContext _context;               // context
        private HttpWriter _httpWriter;             // and HttpWriter 
        private TextWriter _writer;                 // others just have Writer 

        private HttpHeaderCollection _headers;      // response header collection (IIS7+) 

        private bool _headersWritten;
        private bool _completed;    // after final flush
        private bool _ended;        // after response.end or execute url 
        private bool _flushing;
        private bool _clientDisconnected; 
        private bool _filteringCompleted; 
        private bool _closeConnectionAfterError;
 
        // simple properties

        private int         _statusCode = 200;
        private String      _statusDescription; 
        private bool        _bufferOutput = true;
        private String      _contentType = "text/html"; 
        private String      _charSet; 
        private bool        _customCharSet;
        private bool        _contentLengthSet; 
        private String      _redirectLocation;
        private bool        _redirectLocationSet;
        private Encoding    _encoding;
        private Encoder     _encoder; // cached encoder for the encoding 
        private Encoding    _headerEncoding; // encoding for response headers, default utf-8
        private bool        _cacheControlHeaderAdded; 
        private HttpCachePolicy _cachePolicy; 
        private ArrayList   _cacheHeaders;
        private bool        _suppressHeaders; 
        private bool        _suppressContentSet;
        private bool        _suppressContent;
        private string      _appPathModifier;
        private bool        _isRequestBeingRedirected; 
        private bool        _useAdaptiveError;
        private bool        _handlerHeadersGenerated; 
 
        // complex properties
 
        private ArrayList               _customHeaders;
        private HttpCookieCollection    _cookies;
        #pragma warning disable 0649
        private ResponseDependencyList  _fileDependencyList; 
        private ResponseDependencyList  _virtualPathDependencyList;
        private ResponseDependencyList  _cacheItemDependencyList; 
        #pragma warning restore 0649 
        private AggregateCacheDependency _aggDependency;
        private ErrorFormatter          _overrideErrorFormatter; 

        // cache properties
        int         _expiresInMinutes;
        bool        _expiresInMinutesSet; 
        DateTime    _expiresAbsolute;
        bool        _expiresAbsoluteSet; 
        string      _cacheControl; 

        private bool        _statusSet; 
        private int         _subStatusCode;
        private bool        _versionHeaderSent;
        private bool        _contentTypeSet;
 
        // chunking
        bool        _transferEncodingSet; 
        bool        _chunked; 

        // mobile redirect properties 
        internal static readonly String RedirectQueryStringVariable = "__redir";
        internal static readonly String RedirectQueryStringValue = "1";
        internal static readonly String RedirectQueryStringAssignment = RedirectQueryStringVariable + "=" + RedirectQueryStringValue;
 
        private static readonly String _redirectQueryString = "?" + RedirectQueryStringAssignment;
        private static readonly String _redirectQueryStringInline = RedirectQueryStringAssignment + "&"; 
 
        internal HttpContext Context {
            get { return _context; } 
            set { _context = value; }
        }

        internal HttpRequest Request { 
            get {
                if (_context == null) 
                    return null; 
                return _context.Request;
            } 
        }

        /*
         * Internal package visible constructor to create responses that 
         * have HttpWorkerRequest
         * 
         * @param wr Worker Request 
         */
        internal HttpResponse(HttpWorkerRequest wr, HttpContext context) { 
            _wr = wr;
            _context = context;
            // HttpWriter is created in InitResponseWriter
        } 

        // Public constructor for responses that go to an arbitrary writer 
        // Initializes a new instance of the <see langword='HttpResponse'/> class.</para> 
        public HttpResponse(TextWriter writer) {
            _wr = null; 
            _httpWriter = null;
            _writer = writer;
        }
 
        private bool UsingHttpWriter {
            get { 
                return (_httpWriter != null && _writer == _httpWriter); 
            }
        } 

        /*
         *  Cleanup code
         */ 
        internal void Dispose() {
            // recycle buffers 
            if (_httpWriter != null) 
                _httpWriter.RecycleBuffers();
        } 

        internal void InitResponseWriter() {
            if (_httpWriter == null) {
                _httpWriter = new HttpWriter(this); 

                _writer = _httpWriter; 
            } 
        }
 
        //
        // Private helper methods
        //
 
        private void AppendHeader(HttpResponseHeader h) {
            if (_customHeaders == null) 
                _customHeaders = new ArrayList(); 
            _customHeaders.Add(h);
            if (_cachePolicy != null && StringUtil.EqualsIgnoreCase("Set-Cookie", h.Name)) { 
                _cachePolicy.SetHasSetCookieHeader();
            }
        }
 
        internal bool HeadersWritten {
            get { return _headersWritten; } 
            set { _headersWritten = value; } 
        }
 
        internal ArrayList GenerateResponseHeadersIntegrated(bool forCache) {
            ArrayList headers = new ArrayList();
            HttpHeaderCollection responseHeaders = Headers as HttpHeaderCollection;
            int headerId = 0; 

            // copy all current response headers 
            foreach(String key in responseHeaders) 
            {
                // skip certain headers that the cache does not cache 
                // this is based on the cache headers saved separately in AppendHeader
                // and not generated in GenerateResponseHeaders in ISAPI mode
                headerId = HttpWorkerRequest.GetKnownResponseHeaderIndex(key);
                if (headerId >= 0 && forCache && 
                     (headerId == HttpWorkerRequest.HeaderServer ||
                      headerId == HttpWorkerRequest.HeaderSetCookie || 
                      headerId == HttpWorkerRequest.HeaderCacheControl || 
                      headerId == HttpWorkerRequest.HeaderExpires ||
                      headerId == HttpWorkerRequest.HeaderLastModified || 
                      headerId == HttpWorkerRequest.HeaderEtag ||
                      headerId == HttpWorkerRequest.HeaderVary)) {
                    continue;
                } 

                if ( headerId >= 0 ) { 
                    headers.Add(new HttpResponseHeader(headerId, responseHeaders[key])); 
                }
                else { 
                    headers.Add(new HttpResponseHeader(key, responseHeaders[key]));
                }
            }
 
            return headers;
        } 
 
        internal void GenerateResponseHeadersForCookies()
        { 
            if (_cookies == null || (_cookies.Count == 0 && !_cookies.Changed))
                return; // no cookies exist

            HttpHeaderCollection headers = Headers as HttpHeaderCollection; 
            HttpResponseHeader cookieHeader = null;
            HttpCookie cookie = null; 
            bool needToReset = false; 

            // Go through all cookies, and check whether any have been added 
            // or changed.  If a cookie was added, we can simply generate a new
            // set cookie header for it.  If the cookie collection has been
            // changed (cleared or cookies removed), or an existing cookie was
            // changed, we have to regenerate all Set-Cookie headers due to an IIS 
            // limitation that prevents us from being able to delete specific
            // Set-Cookie headers for items that changed. 
            if (!_cookies.Changed) 
            {
                for(int c = 0; c < _cookies.Count; c++) 
                {
                    cookie = _cookies[c];
                    if (cookie.Added) {
                        // if a cookie was added, we generate a Set-Cookie header for it 
                        cookieHeader = cookie.GetSetCookieHeader(_context);
                        headers.SetHeader(cookieHeader.Name, cookieHeader.Value, false); 
                        cookie.Added = false; 
                        cookie.Changed = false;
                    } 
                    else if (cookie.Changed) {
                        // if a cookie has changed, we need to clear all cookie
                        // headers and re-write them all since we cant delete
                        // specific existing cookies 
                        needToReset = true;
                        break; 
                    } 
                }
            } 


            if (_cookies.Changed || needToReset)
            { 
                // delete all set cookie headers
                headers.Remove("Set-Cookie"); 
 
                // write all the cookies again
                for(int c = 0; c < _cookies.Count; c++) 
                {
                    // generate a Set-Cookie header for each cookie
                    cookie = _cookies[c];
                    cookieHeader = cookie.GetSetCookieHeader(_context); 
                    headers.SetHeader(cookieHeader.Name, cookieHeader.Value, false);
                    cookie.Added = false; 
                    cookie.Changed = false; 
                }
 
                _cookies.Changed = false;
            }
        }
 
        internal void GenerateResponseHeadersForHandler()
        { 
            if ( !(_wr is IIS7WorkerRequest) ) { 
                return;
            } 

            bool sendCacheControlHeader = false;
            String versionHeader = null;
 
            // Generate the default headers associated with an ASP.NET handler
            if (!_headersWritten && !_handlerHeadersGenerated) { 
                try { 
                    // The "sendCacheControlHeader" is default to true, but a false setting in either
                    // the <httpRuntime> section (legacy) or the <outputCache> section (current) will disable 
                    // sending of that header.
                    RuntimeConfig config = RuntimeConfig.GetLKGConfig(_context);

                    HttpRuntimeSection runtimeConfig = config.HttpRuntime; 
                    if (runtimeConfig != null) {
                        versionHeader = runtimeConfig.VersionHeader; 
                        sendCacheControlHeader = runtimeConfig.SendCacheControlHeader; 
                    }
 
                    OutputCacheSection outputCacheConfig = config.OutputCache;
                    if (outputCacheConfig != null) {
                        sendCacheControlHeader &= outputCacheConfig.SendCacheControlHeader;
                    } 

                    // Ensure that cacheability is set to cache-control: private 
                    // if it is not explicitly set 
                    if (sendCacheControlHeader && !_cacheControlHeaderAdded) {
                        Headers.Set("Cache-Control", "private"); 
                    }

                    // set the version header
                    if (!String.IsNullOrEmpty(versionHeader)) { 
                        Headers.Set("X-AspNet-Version", versionHeader);
                    } 
 
                    // Force content-type generation
                    _contentTypeSet = true; 
                }
                finally {
                    _handlerHeadersGenerated = true;
                } 
            }
        } 
 
        internal ArrayList GenerateResponseHeaders(bool forCache) {
            ArrayList   headers = new ArrayList(); 
            bool sendCacheControlHeader = HttpRuntimeSection.DefaultSendCacheControlHeader;

            // ASP.NET version header
            if (!forCache ) { 

                if (!_versionHeaderSent) { 
                    String versionHeader = null; 

                    // The "sendCacheControlHeader" is default to true, but a false setting in either 
                    // the <httpRuntime> section (legacy) or the <outputCache> section (current) will disable
                    // sending of that header.
                    RuntimeConfig config = RuntimeConfig.GetLKGConfig(_context);
 
                    HttpRuntimeSection runtimeConfig = config.HttpRuntime;
                    if (runtimeConfig != null) { 
                        versionHeader = runtimeConfig.VersionHeader; 
                        sendCacheControlHeader = runtimeConfig.SendCacheControlHeader;
                    } 

                    OutputCacheSection outputCacheConfig = config.OutputCache;
                    if (outputCacheConfig != null) {
                        sendCacheControlHeader &= outputCacheConfig.SendCacheControlHeader; 
                    }
 
                    if (!String.IsNullOrEmpty(versionHeader)) { 
                        headers.Add(new HttpResponseHeader("X-AspNet-Version", versionHeader));
                    } 

                    _versionHeaderSent = true;
                }
            } 

            // custom headers 
            if (_customHeaders != null) { 
                int numCustomHeaders = _customHeaders.Count;
                for (int i = 0; i < numCustomHeaders; i++) 
                    headers.Add(_customHeaders[i]);
            }

            // location of redirect 
            if (_redirectLocation != null) {
                headers.Add(new HttpResponseHeader(HttpWorkerRequest.HeaderLocation, _redirectLocation)); 
            } 

            // don't include headers that the cache changes or omits on a cache hit 
            if (!forCache) {
                // cookies
                if (_cookies != null) {
                    int numCookies = _cookies.Count; 

                    for (int i = 0; i < numCookies; i++) { 
                        headers.Add(_cookies[i].GetSetCookieHeader(Context)); 
                    }
                } 

                // cache policy
                if (_cachePolicy != null && _cachePolicy.IsModified()) {
                    _cachePolicy.GetHeaders(headers, this); 
                }
                else { 
                    if (_cacheHeaders != null) { 
                        headers.AddRange(_cacheHeaders);
                    } 

                    /*
                     * Ensure that cacheability is set to cache-control: private
                     * if it is not explicitly set. 
                     */
                    if (!_cacheControlHeaderAdded && sendCacheControlHeader) { 
                        headers.Add(new HttpResponseHeader(HttpWorkerRequest.HeaderCacheControl, "private")); 
                    }
                } 
            }

            //
            // content type 
            //
            if ( _statusCode != 204 && _contentType != null) { 
                String contentType = AppendCharSetToContentType( _contentType ); 
                headers.Add(new HttpResponseHeader(HttpWorkerRequest.HeaderContentType, contentType));
            } 

            // done
            return headers;
        } 

        internal string AppendCharSetToContentType(string contentType) 
        { 
            String newContentType = contentType;
 
            // charset=xxx logic -- append if
            //      not there already and
            //          custom set or response encoding used by http writer to convert bytes to chars
            if (_customCharSet || (_httpWriter != null && _httpWriter.ResponseEncodingUsed)) { 
                if (contentType.IndexOf("charset=", StringComparison.Ordinal) < 0) {
                    String charset = Charset; 
                    if (charset.Length > 0) { // not suppressed 
                        newContentType = contentType + "; charset=" + charset;
                    } 
                }
            }

            return newContentType; 
        }
 
        internal bool UseAdaptiveError { 
            get {
                return _useAdaptiveError; 
            }
            set {
                _useAdaptiveError = value;
            } 
        }
 
        private void WriteHeaders() { 
            if (_wr == null)
                return; 

             // Fire pre-send headers event

            if (_context != null && _context.ApplicationInstance != null) { 
                _context.ApplicationInstance.RaiseOnPreSendRequestHeaders();
            } 
 
            // status
            // VSWhidbey 270635: We need to reset the status code for mobile devices. 
            if (UseAdaptiveError) {

                // VSWhidbey 288054: We should change the status code for cases
                // that cannot be handled by mobile devices 
                // 4xx for Client Error and 5xx for Server Error in HTTP spec
                int statusCode = StatusCode; 
                if (statusCode >= 400 && statusCode < 600) { 
                    this.StatusCode = 200;
                } 
            }

            _wr.SendStatus(this.StatusCode, this.StatusDescription);
 
            // headers encoding
 
            // unicode messes up the response badly 
            Debug.Assert(!this.HeaderEncoding.Equals(Encoding.Unicode));
            _wr.SetHeaderEncoding(this.HeaderEncoding); 

            // headers

            ArrayList headers = GenerateResponseHeaders(false); 
            HttpResponseHeader header = null;
            int n = (headers != null) ? headers.Count : 0; 
            for (int i = 0; i < n; i++) 
            {
                header = headers[i] as HttpResponseHeader; 
                header.Send(_wr);
            }
        }
 
        internal int GetBufferedLength() {
            // if length is greater than Int32.MaxValue, Convert.ToInt32 will throw. 
            // This is okay until we support large response sizes 
            return (_httpWriter != null) ? Convert.ToInt32(_httpWriter.GetBufferedLength()) : 0;
        } 

        private static byte[] s_chunkSuffix = new byte[2] { (byte)'\r', (byte)'\n'};
        private static byte[] s_chunkEnd    = new byte[5] { (byte)'0',  (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n'};
 
        private void Flush(bool finalFlush) {
            // Already completed or inside Flush? 
            if (_completed || _flushing) 
                return;
 
            // Special case for non HTTP Writer
            if (_httpWriter == null) {
                _writer.Flush();
                return; 
            }
 
            // Avoid recursive flushes 
            _flushing = true;
 
            try {

                IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest;
                if (iis7WorkerRequest != null) { 
                    // generate the handler headers if flushing
                    GenerateResponseHeadersForHandler(); 
 
                    // Push buffers across to native side and explicitly flush.
                    // IIS7 handles the chunking as necessary so we can omit that logic 
                    UpdateNativeResponse(true /*sendHeaders*/);

                    // force a synchronous send
                    iis7WorkerRequest.ExplicitFlush(); 

                    _headersWritten = true; 
 
                    return;
                } 

                long bufferedLength = 0;

                // 
                // Headers
                // 
 
                if (!_headersWritten) {
                    if (!_suppressHeaders && !_clientDisconnected) { 
                        if (finalFlush) {
                            bufferedLength = _httpWriter.GetBufferedLength();

                            // suppress content-type for empty responses 
                            if (!_contentLengthSet && bufferedLength == 0 && _httpWriter != null)
                                _contentType = null; 
 
                            // if there are any cookies, do not kernel cache the response
                            if (_cachePolicy != null && _cookies != null && _cookies.Count != 0) { 
                                _cachePolicy.SetHasSetCookieHeader();
                                DisableKernelCache();
                            }
 
                            // generate response headers
                            WriteHeaders(); 
 
                            // recalculate as sending headers might change it (PreSendHeaders)
                            bufferedLength = _httpWriter.GetBufferedLength(); 

                            // Calculate content-length if not set explicitely
                            // WOS #1380818: Content-Length should not be set for response with 304 status (HTTP.SYS doesn't, and HTTP 1.1 spec implies it)
                            if (!_contentLengthSet && _statusCode != 304) 
                                _wr.SendCalculatedContentLength(bufferedLength);
                        } 
                        else { 
                            // Check if need chunking for HTTP/1.1
                            if (!_contentLengthSet && !_transferEncodingSet && _statusCode == 200) { 
                                String protocol = _wr.GetHttpVersion();

                                if (protocol != null && protocol.Equals("HTTP/1.1")) {
                                    AppendHeader(new HttpResponseHeader(HttpWorkerRequest.HeaderTransferEncoding, "chunked")); 
                                    _chunked = true;
                                } 
 
                                bufferedLength = _httpWriter.GetBufferedLength();
                            } 

                            WriteHeaders();
                        }
                    } 

                    _headersWritten = true; 
                } 
                else {
                    bufferedLength = _httpWriter.GetBufferedLength(); 
                }

                //
                // Filter and recalculate length if not done already 
                //
 
                if (!_filteringCompleted) { 
                    _httpWriter.Filter(false);
                    bufferedLength = _httpWriter.GetBufferedLength(); 
                }

                //
                // Content 
                //
 
                // suppress HEAD content unless overriden 
                if (!_suppressContentSet && Request != null && Request.HttpVerb == HttpVerb.HEAD)
                    _suppressContent = true; 

                if (_suppressContent || _ended) {
                    _httpWriter.ClearBuffers();
                    bufferedLength = 0; 
                }
 
                if (!_clientDisconnected) { 
                    // Fire pre-send request event
                    if (_context != null && _context.ApplicationInstance != null) 
                        _context.ApplicationInstance.RaiseOnPreSendRequestContent();

                    if (_chunked) {
                        if (bufferedLength > 0) { 
                            byte[] chunkPrefix = Encoding.ASCII.GetBytes(Convert.ToString(bufferedLength, 16) + "\r\n");
                            _wr.SendResponseFromMemory(chunkPrefix, chunkPrefix.Length); 
 
                            _httpWriter.Send(_wr);
 
                            _wr.SendResponseFromMemory(s_chunkSuffix, s_chunkSuffix.Length);
                        }

                        if (finalFlush) 
                            _wr.SendResponseFromMemory(s_chunkEnd, s_chunkEnd.Length);
                    } 
                    else { 
                        _httpWriter.Send(_wr);
                    } 

                    _wr.FlushResponse(finalFlush);

                    if (!finalFlush) 
                        _httpWriter.ClearBuffers();
                } 
            } 
            finally {
                _flushing = false; 

                // Remember if completed
                if (finalFlush)
                    _completed = true; 
            }
        } 
 
        internal void FinalFlushAtTheEndOfRequestProcessing() {
            FinalFlushAtTheEndOfRequestProcessing(false); 
        }

        internal void FinalFlushAtTheEndOfRequestProcessing(bool needPipelineCompletion) {
                Flush(true); 
        }
 
        // WOS 1555777: kernel cache support 
        // If the response can be kernel cached, return the kernel cache key;
        // otherwise return null.  The kernel cache key is used to invalidate 
        // the entry if a dependency changes or the item is flushed from the
        // managed cache for any reason.
        internal String SetupKernelCaching(String originalCacheUrl) {
            // don't kernel cache if we have a cookie header 
            if (_cookies != null && _cookies.Count != 0) {
                _cachePolicy.SetHasSetCookieHeader(); 
                return null; 
            }
 
            bool enableKernelCacheForVaryByStar = IsKernelCacheEnabledForVaryByStar();

            // check cache policy
            if (!_cachePolicy.IsKernelCacheable(Request, enableKernelCacheForVaryByStar)) { 
                return null;
            } 
 
            // check configuration if the kernel mode cache is enabled
            HttpRuntimeSection runtimeConfig = RuntimeConfig.GetLKGConfig(_context).HttpRuntime; 
            if (runtimeConfig == null || !runtimeConfig.EnableKernelOutputCache) {
                return null;
            }
 
            double seconds = (_cachePolicy.UtcGetAbsoluteExpiration() - DateTime.UtcNow).TotalSeconds;
            if (seconds <= 0) { 
                return null; 
            }
 
            int secondsToLive = seconds < Int32.MaxValue ? (int) seconds : Int32.MaxValue;
            string kernelCacheUrl = _wr.SetupKernelCaching(secondsToLive, originalCacheUrl, enableKernelCacheForVaryByStar);

            if (kernelCacheUrl != null) { 
                // Tell cache policy not to use max-age as kernel mode cache doesn't update it
                _cachePolicy.SetNoMaxAgeInCacheControl(); 
            } 

            return kernelCacheUrl; 
        }

        /*
         * Disable kernel caching for this response.  If kernel caching is not supported, this method 
         * returns without performing any action.
         */ 
        public void DisableKernelCache() { 
            if (_wr == null) {
                return; 
            }

            _wr.DisableKernelCache();
        } 

        private bool IsKernelCacheEnabledForVaryByStar() 
        { 
            OutputCacheSection outputCacheConfig = RuntimeConfig.GetAppConfig().OutputCache;
            return (_cachePolicy.IsVaryByStar && outputCacheConfig.EnableKernelCacheForVaryByStar); 
        }

        internal void FilterOutput() {
            try { 
                if (UsingHttpWriter) {
                    IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest; 
                    if (iis7WorkerRequest != null) { 
                        _httpWriter.FilterIntegrated(true, iis7WorkerRequest);
                    } 
                    else {
                        _httpWriter.Filter(true);
                    }
                } 
            }
            finally { 
                _filteringCompleted = true; 
            }
        } 

        /// <devdoc>
        /// Prevents any other writes to the Response
        /// </devdoc> 
        internal void IgnoreFurtherWrites() {
            if (UsingHttpWriter) { 
                _httpWriter.IgnoreFurtherWrites(); 
            }
        } 

        /*
         * Is the entire response buffered so far
         */ 
        internal bool IsBuffered() {
            return !_headersWritten && UsingHttpWriter; 
        } 

        //  Expose cookie collection to request 
        //    Gets the HttpCookie collection sent by the current request.</para>
        public HttpCookieCollection Cookies {
            get {
                if (_cookies == null) 
                    _cookies = new HttpCookieCollection(this, false);
 
                return _cookies; 
            }
        } 

        public NameValueCollection Headers {
            get {
                if ( !(_wr is IIS7WorkerRequest) ) { 
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode));
                } 
 
                if (_headers == null) {
                    _headers = new HttpHeaderCollection(_wr, this, 16); 
                }

                return _headers;
            } 
        }
 
        /* 
         * Add dependency on a file to the current response
         */ 

        /// <devdoc>
        ///    <para>Adds dependency on a file to the current response.</para>
        /// </devdoc> 
        public void AddFileDependency(String filename) {
            _fileDependencyList.AddDependency(filename, "filename"); 
        } 

        // Add dependency on a list of files to the current response 

        //   Adds dependency on a group of files to the current response.
        public void AddFileDependencies(ArrayList filenames) {
            _fileDependencyList.AddDependencies(filenames, "filenames"); 
        }
 
 
        public void AddFileDependencies(string[] filenames) {
            _fileDependencyList.AddDependencies(filenames, "filenames"); 
        }

        internal void AddVirtualPathDependencies(string[] virtualPaths) {
            _virtualPathDependencyList.AddDependencies(virtualPaths, "virtualPaths", false, Request.Path); 
        }
 
        internal void AddFileDependencies(string[] filenames, DateTime utcTime) { 
            _fileDependencyList.AddDependencies(filenames, "filenames", false, utcTime);
        } 

        // Add dependency on another cache item to the response.
        public void AddCacheItemDependency(string cacheKey) {
            _cacheItemDependencyList.AddDependency(cacheKey, "cacheKey"); 
        }
 
        // Add dependency on a list of cache items to the response. 
        public void AddCacheItemDependencies(ArrayList cacheKeys) {
            _cacheItemDependencyList.AddDependencies(cacheKeys, "cacheKeys"); 
        }


        public void AddCacheItemDependencies(string[] cacheKeys) { 
            _cacheItemDependencyList.AddDependencies(cacheKeys, "cacheKeys");
        } 
 
        // Add dependency on one or more CacheDependency objects to the response
        public void AddCacheDependency(params CacheDependency[] dependencies) { 
            if (_aggDependency == null) {
                _aggDependency = new AggregateCacheDependency();
            }
 
            _aggDependency.Add(dependencies);
            Cache.SetDependencies(true); 
        } 

        public static void RemoveOutputCacheItem(string path) { 
            if (path == null)
                throw new ArgumentNullException("path");
            if (StringUtil.StringStartsWith(path, "\\\\") || path.IndexOf(':') >= 0 || !UrlPath.IsRooted(path))
                throw new ArgumentException(SR.GetString(SR.Invalid_path_for_remove, path)); 

            CacheInternal cacheInternal = HttpRuntime.CacheInternal; 
 
            string key = OutputCacheModule.CreateOutputCachedItemKey(
                    path, HttpVerb.GET, null, null); 

            cacheInternal.Remove(key);

            key = OutputCacheModule.CreateOutputCachedItemKey( 
                    path, HttpVerb.POST, null, null);
 
            cacheInternal.Remove(key); 
        }
 
        // Get the list of file dependencies.
        internal string[] GetFileDependencies() {
            return _fileDependencyList.GetDependencies();
        } 

        // Get the list of cache item dependencies. 
        internal string[] GetCacheItemDependencies() { 
            return _cacheItemDependencyList.GetDependencies();
        } 

        // Check if there are file dependencies.
        internal bool HasFileDependencies() {
            return _fileDependencyList.HasDependencies(); 
        }
 
        // Check if there are item dependencies. 
        internal bool HasCacheItemDependencies() {
            return _cacheItemDependencyList.HasDependencies(); 
        }

        internal CacheDependency GetCacheDependency() {
            return _aggDependency; 
        }
 
        internal CacheDependency GetVirtualPathDependency() { 
            return _virtualPathDependencyList.CreateCacheDependency(CacheDependencyType.VirtualPaths, null);
        } 

        internal CacheDependency CreateCacheDependencyForResponse(CacheDependency dependencyVary) {
            CacheDependency dependency;
 
            // N.B. - add file dependencies last so that we hit the file changes monitor
            // just once. 
            dependency = _cacheItemDependencyList.CreateCacheDependency(CacheDependencyType.CacheItems, dependencyVary); 
            dependency = _fileDependencyList.CreateCacheDependency(CacheDependencyType.Files, dependency);
            dependency = _virtualPathDependencyList.CreateCacheDependency(CacheDependencyType.VirtualPaths, dependency); 

            // N.B. we add in the aggregate dependency here, and return it,
            // so this function should only be called once, because the resulting
            // dependency can only be added to the cache once 
            AggregateCacheDependency aggDependency;
            if (_aggDependency != null) { 
                aggDependency = _aggDependency; 
            }
            else { 
                aggDependency = new AggregateCacheDependency();
            }

            if (dependency != null) { 
                aggDependency.Add(dependency);
            } 
 
            return aggDependency;
        } 

        // Get response headers and content as HttpRawResponse
        internal HttpRawResponse GetSnapshot() {
            int statusCode = 200; 
            string statusDescription = null;
            ArrayList headers = null; 
            ArrayList buffers = null; 
            bool hasSubstBlocks = false;
 
            if (!IsBuffered())
                throw new HttpException(SR.GetString(SR.Cannot_get_snapshot_if_not_buffered));

            IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest; 

            // data 
            if (!_suppressContent) { 
                if (iis7WorkerRequest != null) {
                    buffers = _httpWriter.GetIntegratedSnapshot(out hasSubstBlocks, iis7WorkerRequest); 
                }
                else {
                    buffers = _httpWriter.GetSnapshot(out hasSubstBlocks);
                } 
            }
 
            // headers (after data as the data has side effects (like charset, see ASURT 113202)) 
            if (!_suppressHeaders) {
                statusCode = _statusCode; 
                statusDescription = _statusDescription;
                // In integrated pipeline, we need to use the current response headers
                // from the response (these may have been generated by other handlers, etc)
                // instead of the ASP.NET cached headers 
                if (iis7WorkerRequest != null) {
                    headers = GenerateResponseHeadersIntegrated(true); 
                } 
                else {
                    headers = GenerateResponseHeaders(true); 
                }
            }
            return new HttpRawResponse(statusCode, statusDescription, headers, buffers, hasSubstBlocks);
        } 

        // Send saved response snapshot as the entire response 
        internal void UseSnapshot(HttpRawResponse rawResponse, bool sendBody) { 
            if (_headersWritten)
                throw new HttpException(SR.GetString(SR.Cannot_use_snapshot_after_headers_sent)); 

            if (_httpWriter == null)
                throw new HttpException(SR.GetString(SR.Cannot_use_snapshot_for_TextWriter));
 
            ClearAll();
 
            // restore status 
            StatusCode = rawResponse.StatusCode;
            StatusDescription = rawResponse.StatusDescription; 

            // restore headers
            ArrayList headers = rawResponse.Headers;
            int n = (headers != null) ? headers.Count : 0; 
            for (int i = 0; i < n; i++) {
                HttpResponseHeader h = (HttpResponseHeader)(headers[i]); 
                this.AppendHeader(h.Name, h.Value); 
            }
 
            // restore content
            _httpWriter.UseSnapshot(rawResponse.Buffers);

            _suppressContent = !sendBody; 
        }
 
        internal void CloseConnectionAfterError() { 
            _closeConnectionAfterError = true;
        } 

        private void WriteErrorMessage(Exception e, bool dontShowSensitiveErrors) {
            ErrorFormatter errorFormatter = null;
            CultureInfo uiculture = null, savedUiculture = null; 
            bool needToRestoreUiculture = false;
 
            if (_context.DynamicUICulture != null) { 
                // if the user set the culture dynamically use it
                uiculture =  _context.DynamicUICulture; 
            }
            else  {
                // get the UI culture under which the error text must be created (use LKG to avoid errors while reporting error)
                GlobalizationSection globConfig = RuntimeConfig.GetLKGConfig(_context).Globalization; 
                if ((globConfig != null) && (!String.IsNullOrEmpty(globConfig.UICulture))) {
                    try { 
                        uiculture = HttpServerUtility.CreateReadOnlyCultureInfo(globConfig.UICulture); 
                    }
                    catch { 
                    }
                }
            }
 
            //  In Integrated mode, generate the necessary response headers for the error
            GenerateResponseHeadersForHandler(); 
 
            // set the UI culture
            if (uiculture != null) { 
                savedUiculture = Thread.CurrentThread.CurrentUICulture;
                Thread.CurrentThread.CurrentUICulture = uiculture;
                needToRestoreUiculture = true;
            } 

            try { 
                try { 
                    // Try to get an error formatter
                    errorFormatter = GetErrorFormatter(e); 
#if DBG
                    Debug.Trace("internal", "Error stack for " + Request.Path, e);
#endif
                    if (dontShowSensitiveErrors && !errorFormatter.CanBeShownToAllUsers) 
                        errorFormatter = new GenericApplicationErrorFormatter(Request.IsLocal);
 
                    Debug.Trace("internal", "errorFormatter's type = " +  errorFormatter.GetType()); 

                    if (ErrorFormatter.RequiresAdaptiveErrorReporting(Context)) { 
                        _writer.Write(errorFormatter.GetAdaptiveErrorMessage(Context, dontShowSensitiveErrors));
                    }
                    else {
                        _writer.Write(errorFormatter.GetHtmlErrorMessage(dontShowSensitiveErrors)); 

                        // Write a stack dump in an HTML comment for debugging purposes 
                        // Only show it for Asp permission medium or higher (ASURT 126373) 
                        if (!dontShowSensitiveErrors &&
                            HttpRuntime.HasAspNetHostingPermission(AspNetHostingPermissionLevel.Medium)) { 
                            _writer.Write("<!-- \r\n");
                            WriteExceptionStack(e);
                            _writer.Write("-->");
                        } 
                         if (!dontShowSensitiveErrors && !Request.IsLocal ) {
                             _writer.Write("<!-- \r\n"); 
                             _writer.Write(SR.GetString(SR.Information_Disclosure_Warning)); 
                             _writer.Write("-->");
                         } 
                    }

                    if (_closeConnectionAfterError) {
                        Flush(); 
                        Close();
                    } 
                } 
                finally {
                    // restore ui culture 
                    if (needToRestoreUiculture)
                        Thread.CurrentThread.CurrentUICulture = savedUiculture;
                }
            } 
            catch { // Protect against exception filters
                throw; 
            } 
        }
 
        internal void SetOverrideErrorFormatter(ErrorFormatter errorFormatter) {
            _overrideErrorFormatter = errorFormatter;
        }
 
        internal ErrorFormatter GetErrorFormatter(Exception e) {
            ErrorFormatter  errorFormatter = null; 
 
            if (_overrideErrorFormatter != null) {
                return _overrideErrorFormatter; 
            }

            // Try to get an error formatter
            errorFormatter = HttpException.GetErrorFormatter(e); 

            if (errorFormatter == null) { 
                ConfigurationException ce = e as ConfigurationException; 
                if (ce != null && !String.IsNullOrEmpty(ce.Filename))
                    errorFormatter = new ConfigErrorFormatter(ce); 
            }

            // If we couldn't get one, create one here
            if (errorFormatter == null) { 
                // If it's a 404, use a special error page, otherwise, use a more
                // generic one. 
                if (_statusCode == 404) 
                    errorFormatter = new PageNotFoundErrorFormatter(Request.Path);
                else if (_statusCode == 403) 
                    errorFormatter = new PageForbiddenErrorFormatter(Request.Path);
                else {
                    if (e is System.Security.SecurityException)
                        errorFormatter = new SecurityErrorFormatter(e); 
                    else
                        errorFormatter = new UnhandledErrorFormatter(e); 
                } 
            }
 
            return errorFormatter;
        }

        private void WriteOneExceptionStack(Exception e) { 
            Exception subExcep = e.InnerException;
            if (subExcep != null) 
                WriteOneExceptionStack(subExcep); 

            string title = "[" + e.GetType().Name + "]"; 
            if (e.Message != null && e.Message.Length > 0)
                title += ": " + HttpUtility.HtmlEncode(e.Message);

            _writer.WriteLine(title); 
            if (e.StackTrace != null)
                _writer.WriteLine(e.StackTrace); 
        } 

        private void WriteExceptionStack(Exception e) { 
            ConfigurationErrorsException errors = e as ConfigurationErrorsException;
            if (errors == null) {
                WriteOneExceptionStack(e);
            } 
            else {
                // Write the original exception to get the first error with 
                // a full stack trace 
                WriteOneExceptionStack(e);
 
                // Write additional errors, which will contain truncated stacks
                ICollection col = errors.Errors;
                if (col.Count > 1) {
                    bool firstSkipped = false; 
                    foreach (ConfigurationException ce in col) {
                        if (!firstSkipped) { 
                            firstSkipped = true; 
                            continue;
                        } 

                        _writer.WriteLine("---");
                        WriteOneExceptionStack(ce);
                    } 
                }
            } 
        } 

        internal void ReportRuntimeError(Exception e, bool canThrow, bool localExecute) { 
            CustomErrorsSection customErrorsSetting = null;
            bool useCustomErrors = false;
            int code = -1;
 
            if (_completed)
                return; 
 
            // always try to disable IIS custom errors when we send an error
            if (_wr != null) { 
                _wr.TrySkipIisCustomErrors = true;
            }

            if (!localExecute) { 
                code = HttpException.GetHttpCodeForException(e);
 
                // Don't raise event for 404.  See VSWhidbey 124147. 
                if (code != 404) {
                    WebBaseEvent.RaiseRuntimeError(e, this); 
                }

                // This cannot use the HttpContext.IsCustomErrorEnabled property, since it must call
                // GetSettings() with the canThrow parameter. 
                customErrorsSetting = CustomErrorsSection.GetSettings(_context, canThrow);
                if (customErrorsSetting != null) 
                    useCustomErrors = customErrorsSetting.CustomErrorsEnabled(Request); 
                else
                    useCustomErrors = true; 
            }

            if (!_headersWritten) {
                // nothing sent yet - entire response 

                if (code == -1) { 
                    code = HttpException.GetHttpCodeForException(e); 
                }
 
                // change 401 to 500 in case the config is not to impersonate
                if (code == 401 && !_context.IsClientImpersonationConfigured)
                    code = 500;
 
                if (_context.TraceIsEnabled)
                    _context.Trace.StatusCode = code; 
 
                if (!localExecute && useCustomErrors) {
                    String redirect = (customErrorsSetting != null) ? customErrorsSetting.GetRedirectString(code) : null; 

                    if (redirect == null || !RedirectToErrorPage(redirect)) {
                        // if no redirect display generic error
                        ClearAll(); 
                        StatusCode = code;
                        WriteErrorMessage(e, true); 
                    } 
                }
                else { 
                    ClearAll();
                    StatusCode = code;
                    WriteErrorMessage(e, false);
                } 
            }
            else { 
                Clear(); 

                if (_contentType != null && _contentType.Equals("text/html")) { 
                    // in the middle of Html - break Html
                    Write("\r\n\r\n</pre></table></table></table></table></table>");
                    Write("</font></font></font></font></font>");
                    Write("</i></i></i></i></i></b></b></b></b></b></u></u></u></u></u>"); 
                    Write("<p>&nbsp;</p><hr>\r\n\r\n");
                } 
 
                WriteErrorMessage(e, useCustomErrors);
            } 
        }

        internal void SynchronizeStatus(int statusCode, int subStatusCode, string description) {
            _statusCode = statusCode; 
            _subStatusCode = subStatusCode;
            _statusDescription = description; 
        } 

 
        internal void SynchronizeHeader(int knownHeaderIndex, string name, string value) {
            HttpHeaderCollection headers = Headers as HttpHeaderCollection;
            headers.SynchronizeHeader(name, value);
 
            // unknown headers have an index < 0
            if (knownHeaderIndex < 0) { 
                return; 
            }
 
            switch (knownHeaderIndex) {
                case HttpWorkerRequest.HeaderCacheControl:
                    _cacheControlHeaderAdded = true;
                    break; 
                case HttpWorkerRequest.HeaderContentType:
                    _contentType = value; 
                    _contentTypeSet = false; 
                    break;
                case HttpWorkerRequest.HeaderLocation: 
                    _redirectLocation = value;
                    _redirectLocationSet = false;
                    break;
                case HttpWorkerRequest.HeaderSetCookie: 
                    // If the header is Set-Cookie, update the corresponding
                    // cookie in the cookies collection 
                    if (value != null) { 
                        HttpCookie cookie = HttpRequest.CreateCookieFromString(value);
                        Cookies.Set(cookie); 
                        // do not write this cookie back to IIS
                        cookie.Changed = false;
                        cookie.Added = false;
                    } 
                    break;
            } 
        } 

        internal void SyncStatusIntegrated() { 
            Debug.Assert(_wr is IIS7WorkerRequest, "_wr is IIS7WorkerRequest");
             if (!_headersWritten && _statusSet) {
                 // For integrated pipeline, synchronize the status immediately so that the FREB log
                 // correctly indicates the module and notification that changed the status. 
                 _wr.SendStatus(_statusCode, _subStatusCode, this.StatusDescription);
                 _statusSet = false; 
             } 
        }
 
        // Public properties

        // Http status code
        //    Gets or sets the HTTP status code of output returned to client. 
        public int StatusCode {
            get { 
                return _statusCode; 
            }
 
            set {
                if (_headersWritten)
                    throw new HttpException(SR.GetString(SR.Cannot_set_status_after_headers_sent));
 
                if (_statusCode != value) {
                    _statusCode = value; 
                    _subStatusCode = 0; 
                    _statusDescription = null;
                    _statusSet = true; 
                }
            }
        }
 
        // the IIS sub status code
        // since this doesn't get emitted in the protocol 
        // we won't send it through the worker request interface 
        // directly
        public int SubStatusCode { 
            get {
                if ( !(_wr is IIS7WorkerRequest) ) {
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode));
                } 

                return _subStatusCode; 
            } 
            set {
                if ( !(_wr is IIS7WorkerRequest) ) { 
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode));
                }

                if (_headersWritten) { 
                    throw new HttpException(SR.GetString(SR.Cannot_set_status_after_headers_sent));
                } 
 
                _subStatusCode = value;
                _statusSet = true; 
            }
        }

        /* 
         * Http status description string
         */ 
 
        // Http status description string
        //    Gets or sets the HTTP status string of output returned to the client. 
        public String StatusDescription {
            get {
                if (_statusDescription == null)
                    _statusDescription = HttpWorkerRequest.GetStatusDescription(_statusCode); 

                return _statusDescription; 
            } 

            set { 
                if (_headersWritten)
                    throw new HttpException(SR.GetString(SR.Cannot_set_status_after_headers_sent));

                if (value != null && value.Length > 512)  // ASURT 124743 
                    throw new ArgumentOutOfRangeException("value");
                _statusDescription = value; 
                _statusSet = true; 
            }
        } 

        public bool TrySkipIisCustomErrors {
            get {
                if (_wr != null) { 
                    return _wr.TrySkipIisCustomErrors;
                } 
                return false; 
            }
            set { 
                if (_wr != null) {
                    _wr.TrySkipIisCustomErrors = value;
                }
            } 
        }
 
        // Flag indicating to buffer the output 
        //    Gets or sets a value indicating whether HTTP output is buffered.
        public bool BufferOutput { 
            get {
                return _bufferOutput;
            }
 
            set {
                if (_bufferOutput != value) { 
                    _bufferOutput = value; 

                    if (_httpWriter != null) 
                        _httpWriter.UpdateResponseBuffering();
                }
            }
        } 

        // Gets the Content-Encoding HTTP response header. 
        internal String GetHttpHeaderContentEncoding() { 
            string coding = null;
            if (_wr is IIS7WorkerRequest) { 
                if (_headers != null) {
                    coding = _headers["Content-Encoding"];
                }
            } 
            else if (_customHeaders != null) {
                int numCustomHeaders = _customHeaders.Count; 
                for (int i = 0; i < numCustomHeaders; i++) { 
                    HttpResponseHeader h = (HttpResponseHeader)_customHeaders[i];
                    if (h.Name == "Content-Encoding") { 
                        coding = h.Value;
                        break;
                    }
                } 
            }
            return coding; 
        } 

        /* 
         * Content-type
         */

        /// <devdoc> 
        ///    <para>Gets or sets the
        ///       HTTP MIME type of output.</para> 
        /// </devdoc> 
        public String ContentType {
            get { 
                return _contentType;
            }

            set { 
                if (_headersWritten) {
                    // Don't throw if the new content type is the same as the current one 
                    if (_contentType == value) 
                        return;
 
                    throw new HttpException(SR.GetString(SR.Cannot_set_content_type_after_headers_sent));
                }

                _contentTypeSet = true; 
                _contentType = value;
            } 
        } 

 
        //    Gets or sets the HTTP charset of output.
        public String Charset {
            get {
                if (_charSet == null) 
                    _charSet = ContentEncoding.WebName;
 
                return _charSet; 
            }
 
            set {
                if (_headersWritten)
                    throw new HttpException(SR.GetString(SR.Cannot_set_content_type_after_headers_sent));
 
                if (value != null)
                    _charSet = value; 
                else 
                    _charSet = String.Empty;  // to differentiate between not set (default) and empty chatset
 
                _customCharSet = true;
            }
        }
 
        // Content encoding for conversion
        //   Gets or sets the HTTP character set of output. 
        public Encoding ContentEncoding { 
            get {
                if (_encoding == null) { 
                    // use LKG config because Response.ContentEncoding is need to display [config] error
                    GlobalizationSection globConfig = RuntimeConfig.GetLKGConfig(_context).Globalization;
                    if (globConfig != null)
                        _encoding = globConfig.ResponseEncoding; 

                    if (_encoding == null) 
                        _encoding = Encoding.Default; 
                }
 
                return _encoding;
            }

            set { 
                if (value == null)
                    throw new ArgumentNullException("value"); 
 
                if (_encoding == null || !_encoding.Equals(value)) {
                    _encoding = value; 
                    _encoder = null;   // flush cached encoder

                    if (_httpWriter != null)
                        _httpWriter.UpdateResponseEncoding(); 
                }
            } 
        } 

 
        public Encoding HeaderEncoding {
            get {
                if (_headerEncoding == null) {
                    // use LKG config because Response.ContentEncoding is need to display [config] error 
                    GlobalizationSection globConfig = RuntimeConfig.GetLKGConfig(_context).Globalization;
                    if (globConfig != null) 
                        _headerEncoding = globConfig.ResponseHeaderEncoding; 

                    // default to UTF-8 (also for Unicode as headers cannot be double byte encoded) 
                    if (_headerEncoding == null || _headerEncoding.Equals(Encoding.Unicode))
                        _headerEncoding = Encoding.UTF8;
                }
 
                return _headerEncoding;
            } 
 
            set {
                if (value == null) 
                    throw new ArgumentNullException("value");

                if (value.Equals(Encoding.Unicode)) {
                    throw new HttpException(SR.GetString(SR.Invalid_header_encoding, value.WebName)); 
                }
 
                if (_headerEncoding == null || !_headerEncoding.Equals(value)) { 
                    if (_headersWritten)
                        throw new HttpException(SR.GetString(SR.Cannot_set_header_encoding_after_headers_sent)); 

                    _headerEncoding = value;
                }
            } 
        }
 
        // Encoder cached for the current encoding 
        internal Encoder ContentEncoder {
            get { 
                if (_encoder == null) {
                    Encoding e = ContentEncoding;
                    _encoder = e.GetEncoder();
 
                    // enable best fit mapping accoding to config
                    // (doesn't apply to utf-8 which is the default, thus optimization) 
 
                    if (!e.Equals(Encoding.UTF8)) {
                        bool enableBestFit = false; 

                        GlobalizationSection globConfig = RuntimeConfig.GetLKGConfig(_context).Globalization;
                        if (globConfig != null) {
                            enableBestFit = globConfig.EnableBestFitResponseEncoding; 
                        }
 
                        if (!enableBestFit) { 
                            // setting 'fallback' disables best fit mapping
                            _encoder.Fallback = new EncoderReplacementFallback(); 
                        }
                    }
                }
                return _encoder; 
            }
        } 
 
        // Cache policy
        //    Returns the caching semantics of the Web page (expiration time, privacy, vary clauses). 
        public HttpCachePolicy Cache {
            get {
                if (_cachePolicy == null) {
                    _cachePolicy = new HttpCachePolicy(); 
                }
 
                return _cachePolicy; 
            }
        } 

        // Return whether or not we have cache policy. We don't want to create it in
        // situations where we don't modify it.
        internal bool HasCachePolicy { 
            get {
                return _cachePolicy != null; 
            } 
        }
 
        // Client connected flag
        //   Gets a value indicating whether the client is still connected to the server.
        public bool IsClientConnected {
            get { 
                if (_clientDisconnected)
                    return false; 
 
                if (_wr != null && !_wr.IsClientConnected()) {
                    _clientDisconnected = true; 
                    return false;
                }

                return true; 
            }
        } 
 
        public bool IsRequestBeingRedirected {
            get { 
                return _isRequestBeingRedirected;
            }
        }
 

        /// <devdoc> 
        ///    <para>Gets or Sets a redirection string (value of location resposne header) for redirect response.</para> 
        /// </devdoc>
        public String RedirectLocation { 
            get { return _redirectLocation; }
            set {
                if (_headersWritten)
                    throw new HttpException(SR.GetString(SR.Cannot_append_header_after_headers_sent)); 

                _redirectLocation = value; 
                _redirectLocationSet = true; 
            }
        } 

        /*
         * Disconnect client
         */ 

        /// <devdoc> 
        ///    <para>Closes the socket connection to a client.</para> 
        /// </devdoc>
        public void Close() { 
            if (!_clientDisconnected && !_completed && _wr != null) {
                _wr.CloseConnection();
                _clientDisconnected = true;
            } 
        }
 
        // TextWriter object 
        //    Enables custom output to the outgoing Http content body.
        public TextWriter Output { 
            get { return _writer;}
        }

        internal TextWriter SwitchWriter(TextWriter writer) { 
            TextWriter oldWriter = _writer;
            _writer = writer; 
            return oldWriter; 
        }
 
        // Output stream
        //       Enables binary output to the outgoing Http content body.
        public Stream OutputStream {
            get { 
                if (!UsingHttpWriter)
                    throw new HttpException(SR.GetString(SR.OutputStream_NotAvail)); 
 
                return _httpWriter.OutputStream;
            } 
        }

        // ASP classic compat
        //    Writes a string of binary characters to the HTTP output stream. 
        public void BinaryWrite(byte[] buffer) {
            OutputStream.Write(buffer, 0, buffer.Length); 
        } 

 
        //  Appends a PICS (Platform for Internet Content Selection) label HTTP header to the output stream.
        public void Pics(String value) {
            AppendHeader("PICS-Label", value);
        } 

        // Filtering stream 
        //       Specifies a wrapping filter object to modify HTTP entity body before transmission. 
        public Stream Filter {
            get { 
                if (UsingHttpWriter)
                    return _httpWriter.GetCurrentFilter();
                else
                    return null; 
            }
 
            set { 
                if (UsingHttpWriter) {
                    _httpWriter.InstallFilter(value); 

                    IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest;
                    if (iis7WorkerRequest != null) {
                        iis7WorkerRequest.ResponseFilterInstalled(); 
                    }
                } 
                else 
                    throw new HttpException(SR.GetString(SR.Filtering_not_allowed));
            } 

        }

        // Flag to suppress writing of content 
        //    Gets or sets a value indicating that HTTP content will not be sent to client.
        public bool SuppressContent { 
            get { 
                return _suppressContent;
            } 

            set {
                _suppressContent = value;
                _suppressContentSet = true; 
            }
        } 
 
        //
        // Public methods 
        //

        /*
          * Add Http custom header 
          *
          * @param name header name 
          * @param value header value 
          */
 
        /// <devdoc>
        ///    <para>Adds an HTTP
        ///       header to the output stream.</para>
        /// </devdoc> 
        public void AppendHeader(String name, String value) {
            bool isCacheHeader = false; 
 
            if (_headersWritten)
                throw new HttpException(SR.GetString(SR.Cannot_append_header_after_headers_sent)); 

            // some headers are stored separately or require special action
            int knownHeaderIndex = HttpWorkerRequest.GetKnownResponseHeaderIndex(name);
 
            switch (knownHeaderIndex) {
                case HttpWorkerRequest.HeaderContentType: 
                    ContentType = value; 
                    return; // don't keep as custom header
 
                case HttpWorkerRequest.HeaderContentLength:
                    _contentLengthSet = true;
                    break;
 
                case HttpWorkerRequest.HeaderLocation:
                    RedirectLocation = value; 
                    return; // don't keep as custom header 

                case HttpWorkerRequest.HeaderTransferEncoding: 
                    _transferEncodingSet = true;
                    break;

                case HttpWorkerRequest.HeaderCacheControl: 
                    _cacheControlHeaderAdded = true;
                    goto case HttpWorkerRequest.HeaderExpires; 
                case HttpWorkerRequest.HeaderExpires: 
                case HttpWorkerRequest.HeaderLastModified:
                case HttpWorkerRequest.HeaderEtag: 
                case HttpWorkerRequest.HeaderVary:
                    isCacheHeader = true;
                    break;
            } 

            // In integrated mode, write the headers directly 
            if (_wr is IIS7WorkerRequest) { 
                Headers.Add(HttpResponseHeader.MaybeEncodeHeader(name), HttpResponseHeader.MaybeEncodeHeader(value));
            } 
            else {
                if (isCacheHeader)
                {
                    // don't keep as custom header 
                    if (_cacheHeaders == null) {
                        _cacheHeaders = new ArrayList(); 
                    } 

                    _cacheHeaders.Add(new HttpResponseHeader(knownHeaderIndex, value)); 
                    return;
                }
                else {
                    HttpResponseHeader h; 
                    if (knownHeaderIndex >= 0)
                        h = new HttpResponseHeader(knownHeaderIndex, value); 
                    else 
                        h = new HttpResponseHeader(name, value);
 
                    AppendHeader(h);
                }
            }
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        ///    <para> 
        ///       Adds an HTTP
        ///       cookie to the output stream.
        ///    </para>
        /// </devdoc> 
        public void AppendCookie(HttpCookie cookie) {
            if (_headersWritten) 
                throw new HttpException(SR.GetString(SR.Cannot_append_cookie_after_headers_sent)); 

            Cookies.AddCookie(cookie, true); 
            OnCookieAdd(cookie);
        }

 
        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc> 
        public void SetCookie(HttpCookie cookie) {
            if (_headersWritten) 
                throw new HttpException(SR.GetString(SR.Cannot_append_cookie_after_headers_sent));

            Cookies.AddCookie(cookie, false);
            OnCookieCollectionChange(); 
        }
 
        internal void BeforeCookieCollectionChange() { 
            if (_headersWritten)
                throw new HttpException(SR.GetString(SR.Cannot_modify_cookies_after_headers_sent)); 
        }

        internal void OnCookieAdd(HttpCookie cookie) {
            // add to request's cookies as well 
            Request.AddResponseCookie(cookie);
        } 
 
        internal void OnCookieCollectionChange() {
            // synchronize with request cookie collection 
            Request.ResetCookies();
        }

        // Clear response headers 
        //    Clears all headers from the buffer stream.
        public void ClearHeaders() { 
            if (_headersWritten) 
                throw new HttpException(SR.GetString(SR.Cannot_clear_headers_after_headers_sent));
 
            StatusCode = 200;
            _subStatusCode = 0;
            _statusDescription = null;
 
            _contentType = "text/html";
            _charSet = null; 
            _customCharSet = false; 
            _contentLengthSet = false;
 
            _redirectLocation = null;
            _redirectLocationSet = false;
            _isRequestBeingRedirected = false;
            _handlerHeadersGenerated = false; 

            _customHeaders = null; 
 
            if (_headers != null) {
                _headers.ClearInternal(); 
            }

            _transferEncodingSet = false;
            _chunked = false; 

            if (_cookies != null) { 
                _cookies.Reset(); 
                Request.ResetCookies();
            } 

            if (_cachePolicy != null) {
                _cachePolicy.Reset();
            } 

            _cacheControlHeaderAdded = false; 
            _cacheHeaders = null; 

            _suppressHeaders = false; 
            _suppressContent = false;
            _suppressContentSet = false;

            _expiresInMinutes = 0; 
            _expiresInMinutesSet = false;
            _expiresAbsolute = DateTime.MinValue; 
            _expiresAbsoluteSet = false; 
            _cacheControl = null;
 
            IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest;
            if (iis7WorkerRequest != null) {
                // clear the native response as well
                ClearNativeResponse(false, true, iis7WorkerRequest); 
            }
 
        } 

 
        /// <devdoc>
        ///    <para>Clears all content output from the buffer stream.</para>
        /// </devdoc>
        public void ClearContent() { 
            Clear();
        } 
 
        /*
         * Clear response buffer and headers. (For ASP compat doesn't clear headers) 
         */

        /// <devdoc>
        ///    <para>Clears all headers and content output from the buffer stream.</para> 
        /// </devdoc>
        public void Clear() { 
            if (UsingHttpWriter) 
                _httpWriter.ClearBuffers();
 
            IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest;
            if (iis7WorkerRequest != null) {
                // clear the native response buffers too
                ClearNativeResponse(true, false, iis7WorkerRequest); 
            }
 
 
        }
 
        /*
         * Clear response buffer and headers. Internal. Used to be 'Clear'.
         */
        internal void ClearAll() { 
            if (!_headersWritten)
                ClearHeaders(); 
            Clear(); 
        }
 
        /*
         * Flush response currently buffered
         */
 
        /// <devdoc>
        ///    <para>Sends all currently buffered output to the client.</para> 
        /// </devdoc> 
        public void Flush() {
            if (_completed) 
                throw new HttpException(SR.GetString(SR.Cannot_flush_completed_response));

            Flush(false);
        } 

        /* 
         * Append string to the log record 
         *
         * @param param string to append to the log record 
         */

        /// <devdoc>
        ///    <para>Adds custom log information to the IIS log file.</para> 
        /// </devdoc>
        [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Medium)] 
        public void AppendToLog(String param) { 
            // only makes sense for IIS
            if (_wr is System.Web.Hosting.ISAPIWorkerRequest) 
                ((System.Web.Hosting.ISAPIWorkerRequest)_wr).AppendLogParameter(param);
            else if (_wr is System.Web.Hosting.IIS7WorkerRequest)
                _context.Request.AppendToLogQueryString(param);
        } 

 
        /// <devdoc> 
        ///    <para>Redirects a client to a new URL.</para>
        /// </devdoc> 
        public void Redirect(String url) {
            Redirect(url, true);
        }
 

        /// <devdoc> 
        ///    <para>Redirects a client to a new URL.</para> 
        /// </devdoc>
        public void Redirect(String url, bool endResponse) { 
            if (url == null)
                throw new ArgumentNullException("url");

            if (url.IndexOf('\n') >= 0) 
                throw new ArgumentException(SR.GetString(SR.Cannot_redirect_to_newline));
 
            if (_headersWritten) 
                throw new HttpException(SR.GetString(SR.Cannot_redirect_after_headers_sent));
 
            Page page = _context.Handler as Page;
            if ((page != null) && page.IsCallback) {
                throw new ApplicationException(SR.GetString(SR.Redirect_not_allowed_in_callback));
            } 

            url = ApplyRedirectQueryStringIfRequired(url); 
 
            url = ApplyAppPathModifier(url);
 
            url = ConvertToFullyQualifiedRedirectUrlIfRequired(url);

            url = UrlEncodeRedirect(url);
 
            Clear();
 
            // If it's a Page and SmartNavigation is on, return a short script 
            // to perform the redirect instead of returning a 302 (bugs ASURT 82331/86782)
#pragma warning disable 0618    // To avoid SmartNavigation deprecation warning 
            if (page != null && page.IsPostBack && page.SmartNavigation && (Request["__smartNavPostBack"] == "true")) {
#pragma warning restore 0618
                Write("<BODY><ASP_SMARTNAV_RDIR url=\"");
                Write(HttpUtility.HtmlEncode(url)); 
                Write("\"></ASP_SMARTNAV_RDIR>");
 
                Write("</BODY>"); 
            }
            else { 
                this.StatusCode = 302;
                RedirectLocation = url;
                Write("<html><head><title>Object moved</title></head><body>\r\n");
                Write("<h2>Object moved to <a href=\"" + HttpUtility.HtmlAttributeEncode(url) + "\">here</a>.</h2>\r\n"); 
                Write("</body></html>\r\n");
            } 
 
            _isRequestBeingRedirected = true;
 
            if (endResponse)
                End();
        }
 
        internal string ApplyRedirectQueryStringIfRequired(string url) {
            if (Request == null || (string)Request.Browser["requiresPostRedirectionHandling"] != "true") 
                return url; 

            Page page = _context.Handler as Page; 
            if (page != null && !page.IsPostBack)
                return url;

            //do not add __redir=1 if it already exists 
            int i = url.IndexOf(RedirectQueryStringAssignment, StringComparison.Ordinal);
            if(i == -1) { 
                i = url.IndexOf('?'); 
                if (i >= 0) {
                    url = url.Insert(i + 1, _redirectQueryStringInline); 
                }
                else {
                    url = String.Concat(url, _redirectQueryString);
                } 
            }
            return url; 
        } 

        // 
        // Redirect to error page appending ?aspxerrorpath if no query string in the url.
        // Fails to redirect if request is already for error page.
        // Suppresses all errors.
        // Returns true if redirect performed successfuly 
        //
        internal bool RedirectToErrorPage(String url) { 
            const String qsErrorMark = "aspxerrorpath"; 

            try { 
                if (String.IsNullOrEmpty(url))
                    return false;   // nowhere to redirect

                if (_headersWritten) 
                    return false;
 
                if (Request.QueryString[qsErrorMark] != null) 
                    return false;   // already in error redirect
 
                // append query string
                if (url.IndexOf('?') < 0)
                    url = url + "?" + qsErrorMark + "=" + HttpUtility.UrlEncodeSpaces(Request.Path);
 
                // redirect without response.end
                Redirect(url, false /*endResponse*/); 
            } 
            catch {
                return false; 
            }

            return true;
        } 

        // Implementation of the DefaultHttpHandler for IIS6+ 
        internal bool CanExecuteUrlForEntireResponse { 
            get {
                // if anything is sent, too late 
                if (_headersWritten) {
                    return false;
                }
 
                // must have the right kind of worker request
                if (_wr == null || !_wr.SupportsExecuteUrl) { 
                    return false; 
                }
 
                // must not be capturing output to custom writer
                if (!UsingHttpWriter) {
                    return false;
                } 

                // there is some cached output not yet sent 
                if (_httpWriter.GetBufferedLength() != 0) { 
                    return false;
                } 

                // can't use execute url with filter installed
                if (_httpWriter.FilterInstalled) {
                    return false; 
                }
 
                if (_cachePolicy != null && _cachePolicy.IsModified()) { 
                    return false;
                } 

                return true;
            }
        } 

        internal IAsyncResult BeginExecuteUrlForEntireResponse( 
                                    String pathOverride, NameValueCollection requestHeaders, 
                                    AsyncCallback cb, Object state) {
            Debug.Assert(CanExecuteUrlForEntireResponse); 

            // prepare user information
            String userName, userAuthType;
            if (_context != null && _context.User != null) { 
                userName     = _context.User.Identity.Name;
                userAuthType = _context.User.Identity.AuthenticationType; 
            } 
            else {
                userName = String.Empty; 
                userAuthType = String.Empty;
            }

            // get the path 
            String path = Request.RewrittenUrl; // null is ok
 
            if (pathOverride != null) { 
                path = pathOverride;
            } 

            // get the headers
            String headers = null;
 
            if (requestHeaders != null) {
                int numHeaders = requestHeaders.Count; 
 
                if (numHeaders > 0) {
                    StringBuilder sb = new StringBuilder(); 

                    for (int i = 0; i < numHeaders; i++) {
                        sb.Append(requestHeaders.GetKey(i));
                        sb.Append(": "); 
                        sb.Append(requestHeaders.Get(i));
                        sb.Append("\r\n"); 
                    } 

                    headers = sb.ToString(); 
                }
            }

            byte[] entity = null; 
            if (_context != null && _context.Request != null) {
                entity = _context.Request.EntityBody; 
            } 

            Debug.Trace("ExecuteUrl", "HttpResponse.BeginExecuteUrlForEntireResponse:" + 
                " path=" + path + " headers=" + headers +
                " userName=" + userName + " authType=" + userAuthType);

            // call worker request to start async execute url for this request 
            IAsyncResult ar = _wr.BeginExecuteUrl(
                    path, 
                    null, // this method 
                    headers,
                    true, // let execute url send headers 
                    true, // add user info
                    _wr.GetUserToken(),
                    userName,
                    userAuthType, 
                    entity,
                    cb, 
                    state); 

            // suppress further sends from ASP.NET 
            // (only if succeeded starting async operation - not is 'finally' block)
            _headersWritten = true;
            _ended = true;
 
            return ar;
        } 
 
        internal void EndExecuteUrlForEntireResponse(IAsyncResult result) {
            Debug.Trace("ExecuteUrl", "HttpResponse.EndExecuteUrlForEntireResponse"); 
            _wr.EndExecuteUrl(result);
        }

        // Methods to write from file 

        //    Writes values to an HTTP output content stream. 
        public void Write(String s) { 
            _writer.Write(s);
        } 

        // Writes values to an HTTP output content stream.
        public void Write(Object obj) {
            _writer.Write(obj); 
        }
 
 
        /// <devdoc>
        ///    <para>Writes values to an HTTP output content stream.</para> 
        /// </devdoc>
        public void Write(char ch) {
            _writer.Write(ch);
        } 

 
        /// <devdoc> 
        ///    <para>Writes values to an HTTP output content stream.</para>
        /// </devdoc> 
        public void Write(char[] buffer, int index, int count) {
            _writer.Write(buffer, index, count);
        }
 

        /// <devdoc> 
        ///    <para>Writes a substition block to the response.</para> 
        /// </devdoc>
        public void WriteSubstitution(HttpResponseSubstitutionCallback callback) { 
            // cannot be instance method on a control
            if (callback.Target != null && callback.Target is Control) {
                throw new ArgumentException(SR.GetString(SR.Invalid_substitution_callback), "callback");
            } 

            if (UsingHttpWriter) { 
                // HttpWriter can take substitution blocks 
                _httpWriter.WriteSubstBlock(callback, _wr as IIS7WorkerRequest);
            } 
            else {
                // text writer -- write as string
                _writer.Write(callback(_context));
            } 

            // set the cache policy: reduce cachability from public to server 
            if (_cachePolicy != null && _cachePolicy.GetCacheability() == HttpCacheability.Public) 
                _cachePolicy.SetCacheability(HttpCacheability.Server);
        } 

        /*
         * Helper method to write from file stream
         * 
         * Handles only TextWriter case. For real requests
         * HttpWorkerRequest can take files 
         */ 
        private void WriteStreamAsText(Stream f, long offset, long size) {
            if (size < 0) 
                size = f.Length - offset;

            if (size > 0) {
                if (offset > 0) 
                    f.Seek(offset, SeekOrigin.Begin);
 
                byte[] fileBytes = new byte[(int)size]; 
                int bytesRead = f.Read(fileBytes, 0, (int)size);
                _writer.Write(Encoding.Default.GetChars(fileBytes, 0, bytesRead)); 
            }
        }

        // support for VirtualPathProvider 
        internal void WriteVirtualFile(VirtualFile vf) {
            Debug.Trace("WriteVirtualFile", vf.Name); 
 
            using (Stream s = vf.Open()) {
                if (UsingHttpWriter) { 
                    long size = s.Length;

                    if (size > 0) {
                        // write as memory block 
                        byte[] fileBytes = new byte[(int)size];
                        int bytesRead = s.Read(fileBytes, 0, (int) size); 
                        _httpWriter.WriteBytes(fileBytes, 0, bytesRead); 
                    }
                } 
                else {
                    // Write file contents
                    WriteStreamAsText(s, 0, -1);
                } 
            }
        } 
 
        // Helper method to get absolute physical filename from the argument to WriteFile
        private String GetNormalizedFilename(String fn) { 
            // If it's not a physical path, call MapPath on it
            if (!UrlPath.IsAbsolutePhysicalPath(fn)) {
                if (Request != null)
                    fn = Request.MapPath(fn); // relative to current request 
                else
                    fn = HostingEnvironment.MapPath(fn); 
            } 

            return fn; 
        }

        // Write file
        ///  Writes a named file directly to an HTTP content output stream. 
        public void WriteFile(String filename) {
            if (filename == null) { 
                throw new ArgumentNullException("filename"); 
            }
 
            WriteFile(filename, false);
        }

        /* 
         * Write file
         * 
         * @param filename file to write 
         * @readIntoMemory flag to read contents into memory immediately
         */ 

        /// <devdoc>
        ///    <para> Reads a file into a memory block.</para>
        /// </devdoc> 
        public void WriteFile(String filename, bool readIntoMemory) {
            if (filename == null) { 
                throw new ArgumentNullException("filename"); 
            }
 
            filename = GetNormalizedFilename(filename);

            FileStream f = null;
 
            try {
                f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read); 
 
                if (UsingHttpWriter) {
                    long size = f.Length; 

                    if (size > 0) {
                        if (readIntoMemory) {
                            // write as memory block 
                            byte[] fileBytes = new byte[(int)size];
                            int bytesRead = f.Read(fileBytes, 0, (int) size); 
                            _httpWriter.WriteBytes(fileBytes, 0, bytesRead); 
                        }
                        else { 
                            // write as file block
                            f.Close(); // close before writing
                            f = null;
                            _httpWriter.WriteFile(filename, 0, size); 
                        }
                    } 
                } 
                else {
                    // Write file contents 
                    WriteStreamAsText(f, 0, -1);
                }
            }
            finally { 
                if (f != null)
                    f.Close(); 
            } 
        }
 

        public void TransmitFile(string filename) {
            TransmitFile(filename, 0, -1);
        } 
        public void TransmitFile(string filename, long offset, long length) {
            if (filename == null) { 
                throw new ArgumentNullException("filename"); 
            }
            if (offset < 0) 
                throw new ArgumentException(SR.GetString(SR.Invalid_range), "offset");
            if (length < -1)
                throw new ArgumentException(SR.GetString(SR.Invalid_range), "length");
 
            if (!UsingHttpWriter) {
                if (offset != 0 || length != -1) { 
                    throw new PlatformNotSupportedException(); 
                }
                WriteFile(filename); 
                return;
            }
            if ((offset != 0 || length != -1) && _wr is ISAPIWorkerRequestOutOfProc)
                throw new PlatformNotSupportedException(); 

            filename = GetNormalizedFilename(filename); 
 
            long size;
            using (FileStream f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) { 
                size = f.Length;
            }

            if (size < offset) 
                throw new ArgumentException(SR.GetString(SR.Invalid_range), "offset");
 
            if (length == -1) 
                length = size - offset;
            else if ((size - offset) < length) 
                throw new ArgumentException(SR.GetString(SR.Invalid_range), "offset");

            if (length > 0) {
                bool supportsLongTransmitFile = (_wr != null && _wr.SupportsLongTransmitFile); 

                _httpWriter.TransmitFile(filename, offset, length, 
                   _context.IsClientImpersonationConfigured || HttpRuntime.IsOnUNCShareInternal, supportsLongTransmitFile); 
            }
        } 


        private void ValidateFileRange(String filename, long offset, long length) {
            FileStream f = null; 

            try { 
                f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read); 

                long fileSize = f.Length; 

                if (length == -1)
                    length = fileSize - offset;
 
                if (offset < 0 || length > fileSize - offset)
                    throw new HttpException(SR.GetString(SR.Invalid_range)); 
            } 
            finally {
                if (f != null) 
                    f.Close();
            }
        }
 
        /*
         * Write file 
         * 
         * @param filename file to write
         * @param offset file offset to start writing 
         * @param size number of bytes to write
         */

        /// <devdoc> 
        ///    <para>Writes a file directly to an HTTP content output stream.</para>
        /// </devdoc> 
        public void WriteFile(String filename, long offset, long size) { 
            if (filename == null) {
                throw new ArgumentNullException("filename"); 
            }

            if (size == 0)
                return; 

            filename = GetNormalizedFilename(filename); 
 
            ValidateFileRange(filename, offset, size);
 
            if (UsingHttpWriter) {
                // HttpWriter can take files -- don't open here (but Demand permission)
                InternalSecurityPermissions.FileReadAccess(filename).Demand();
                _httpWriter.WriteFile(filename, offset, size); 
            }
            else { 
                FileStream f = null; 

                try { 
                    f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    WriteStreamAsText(f, offset, size);
                }
                finally { 
                    if (f != null)
                        f.Close(); 
                } 
            }
        } 

        /*
         * Write file
         * 
         * @param handle file to write
         * @param offset file offset to start writing 
         * @param size number of bytes to write 
         */
 
        /// <devdoc>
        ///    <para>Writes a file directly to an HTTP content output stream.</para>
        /// </devdoc>
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)] 
        public void WriteFile(IntPtr fileHandle, long offset, long size) {
            if (size <= 0) 
                return; 

            FileStream f = null; 

            try {
                f = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(fileHandle,false), FileAccess.Read);
 
                if (UsingHttpWriter) {
                    long fileSize = f.Length; 
 
                    if (size == -1)
                        size = fileSize - offset; 

                    if (offset < 0 || size > fileSize - offset)
                        throw new HttpException(SR.GetString(SR.Invalid_range));
 
                    if (offset > 0)
                        f.Seek(offset, SeekOrigin.Begin); 
 
                    // write as memory block
                    byte[] fileBytes = new byte[(int)size]; 
                    int bytesRead = f.Read(fileBytes, 0, (int)size);
                    _httpWriter.WriteBytes(fileBytes, 0, bytesRead);
                }
                else { 
                    WriteStreamAsText(f, offset, size);
                } 
            } 
            finally {
                if (f != null) 
                    f.Close();
            }
        }
 
        //
        // Deprecated ASP compatibility methods and properties 
        // 

 
        /// <devdoc>
        ///    <para>
        ///       Same as StatusDescription. Provided only for ASP compatibility.
        ///    </para> 
        /// </devdoc>
        public string Status { 
            get { 
                return this.StatusCode.ToString(NumberFormatInfo.InvariantInfo) + " " + this.StatusDescription;
            } 

            set {
                int code = 200;
                String descr = "OK"; 

                try { 
                    int i = value.IndexOf(' '); 
                    code = Int32.Parse(value.Substring(0, i), CultureInfo.InvariantCulture);
                    descr = value.Substring(i+1); 
                }
                catch {
                    throw new HttpException(SR.GetString(SR.Invalid_status_string));
                } 

                this.StatusCode = code; 
                this.StatusDescription = descr; 
            }
        } 


        /// <devdoc>
        ///    <para> 
        ///       Same as BufferOutput. Provided only for ASP compatibility.
        ///    </para> 
        /// </devdoc> 
        public bool Buffer {
            get { return this.BufferOutput;} 
            set { this.BufferOutput = value;}
        }

 
        /// <devdoc>
        ///    <para>Same as Appendheader. Provided only for ASP compatibility.</para> 
        /// </devdoc> 
        public void AddHeader(String name, String value) {
            AppendHeader(name, value); 
        }

        /*
         * Cancelles handler processing of the current request 
         * throws special [non-]exception uncatchable by the user code
         * to tell application to stop module execution. 
         */ 

        /// <devdoc> 
        ///    <para>Sends all currently buffered output to the client then closes the
        ///       socket connection.</para>
        /// </devdoc>
        public void End() { 

            if (_context.IsInCancellablePeriod) { 
                InternalSecurityPermissions.ControlThread.Assert(); 
                Thread.CurrentThread.Abort(new HttpApplication.CancelModuleException(false));
            } 
            else {
                // when cannot abort execution, flush and supress further output
                if (!_flushing) { // ignore Reponse.End while flushing (in OnPreSendHeaders)
                    Flush(); 
                    _ended = true;
 
                    if (_context.ApplicationInstance != null) { 
                        _context.ApplicationInstance.CompleteRequest();
                    } 
                }
            }
        }
 
        /*
         * ASP compatible caching properties 
         */ 

 
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the time, in minutes, until cached
        ///       information will be removed from the cache. Provided for ASP compatiblility. Use 
        ///       the <see cref='System.Web.HttpResponse.Cache'/>
        ///       Property instead. 
        ///    </para> 
        /// </devdoc>
        public int Expires { 
            get {
                return _expiresInMinutes;
            }
            set { 
                if (!_expiresInMinutesSet || value < _expiresInMinutes) {
                    _expiresInMinutes = value; 
                    Cache.SetExpires(_context.Timestamp + new TimeSpan(0, _expiresInMinutes, 0)); 
                }
            } 
        }


        /// <devdoc> 
        ///    <para>
        ///       Gets or sets the absolute time that cached information 
        ///       will be removed from the cache. Provided for ASP compatiblility. Use the <see cref='System.Web.HttpResponse.Cache'/> 
        ///       property instead.
        ///    </para> 
        /// </devdoc>
        public DateTime ExpiresAbsolute {
            get {
                return _expiresAbsolute; 
            }
            set { 
                if (!_expiresAbsoluteSet || value < _expiresAbsolute) { 
                    _expiresAbsolute = value;
                    Cache.SetExpires(_expiresAbsolute); 
                }
            }
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       Provided for ASP compatiblility. Use the <see cref='System.Web.HttpResponse.Cache'/>
        ///       property instead. 
        ///    </para>
        /// </devdoc>
        public string CacheControl {
            get { 
                if (_cacheControl == null) {
                    // the default 
                    return "private"; 
                }
 
                return _cacheControl;
            }
            set {
                if (String.IsNullOrEmpty(value)) { 
                    _cacheControl = null;
                    Cache.SetCacheability(HttpCacheability.NoCache); 
                } 
                else if (StringUtil.EqualsIgnoreCase(value, "private")) {
                    _cacheControl = value; 
                    Cache.SetCacheability(HttpCacheability.Private);
                }
                else if (StringUtil.EqualsIgnoreCase(value, "public")) {
                    _cacheControl = value; 
                    Cache.SetCacheability(HttpCacheability.Public);
                } 
                else if (StringUtil.EqualsIgnoreCase(value, "no-cache")) { 
                    _cacheControl = value;
                    Cache.SetCacheability(HttpCacheability.NoCache); 
                }
                else {
                    throw new ArgumentException(SR.GetString(SR.Invalid_value_for_CacheControl, value));
                } 
            }
        } 
 
        internal void SetAppPathModifier(string appPathModifier) {
            if (appPathModifier != null && ( 
                appPathModifier.Length == 0 ||
                appPathModifier[0] == '/' ||
                appPathModifier[appPathModifier.Length - 1] == '/')) {
 
                throw new ArgumentException(SR.GetString(SR.InvalidArgumentValue, "appPathModifier"));
            } 
 
            _appPathModifier = appPathModifier;
        } 


        public string ApplyAppPathModifier(string virtualPath) {
            object ch = _context.CookielessHelper; // This ensures that the cookieless-helper is initialized and applies the AppPathModifier 
            if (virtualPath == null)
                return null; 
 
            if (UrlPath.IsRelativeUrl(virtualPath)) {
                virtualPath = UrlPath.Combine(Request.FilePathObject.VirtualPathString, virtualPath); 
            }
            else {
                // ignore paths with http://server/... or //
                if (!UrlPath.IsRooted(virtualPath) || virtualPath.StartsWith("//", StringComparison.Ordinal)) { 
                    return virtualPath;
                } 
 
                virtualPath = UrlPath.Reduce(virtualPath);
            } 

            if (_appPathModifier == null || virtualPath.IndexOf(_appPathModifier, StringComparison.Ordinal) >= 0)
                return virtualPath;
 
            string appPath = HttpRuntime.AppDomainAppVirtualPathString;
 
            int compareLength = appPath.Length; 
            bool isVirtualPathShort = (virtualPath.Length == appPath.Length - 1);
            if (isVirtualPathShort) { 
                compareLength--;
            }

            // String.Compare will throw exception if there aren't compareLength characters 
            if (virtualPath.Length < compareLength) {
                return virtualPath; 
            } 

            if (!StringUtil.EqualsIgnoreCase(virtualPath, 0, appPath, 0, compareLength)) { 
                return virtualPath;
            }

            if (isVirtualPathShort) { 
                virtualPath += "/";
            } 
 
            Debug.Assert(virtualPath.Length >= appPath.Length);
            if (virtualPath.Length == appPath.Length) { 
                virtualPath = virtualPath.Substring(0, appPath.Length) + _appPathModifier + "/";
            }
            else {
                virtualPath = 
                    virtualPath.Substring(0, appPath.Length) +
                    _appPathModifier + 
                    "/" + 
                    virtualPath.Substring(appPath.Length);
            } 

            return virtualPath;
        }
 
        internal String RemoveAppPathModifier(string virtualPath) {
            if (String.IsNullOrEmpty(_appPathModifier)) 
                return virtualPath; 

            int pos = virtualPath.IndexOf(_appPathModifier, StringComparison.Ordinal); 

            if (pos <= 0 || virtualPath[pos-1] != '/')
                return virtualPath;
 
            return virtualPath.Substring(0, pos-1) + virtualPath.Substring(pos + _appPathModifier.Length);
        } 
 
        private String ConvertToFullyQualifiedRedirectUrlIfRequired(String url) {
            HttpRuntimeSection runtimeConfig = RuntimeConfig.GetConfig(_context).HttpRuntime; 
            if (    runtimeConfig.UseFullyQualifiedRedirectUrl ||
                    (Request != null && (string)Request.Browser["requiresFullyQualifiedRedirectUrl"] == "true")) {
                return (new Uri(Request.Url, url)).AbsoluteUri ;
            } 
            else {
                return url; 
            } 
        }
 
        private String UrlEncodeRedirect(String url) {
            // convert all non-ASCII chars before ? to %XX using UTF-8 and
            // after ? using Response.ContentEncoding
 
            int iqs = url.IndexOf('?');
 
            if (iqs >= 0) { 
                Encoding qsEncoding = (Request != null) ? Request.ContentEncoding : ContentEncoding;
                url = HttpUtility.UrlEncodeSpaces(HttpUtility.UrlEncodeNonAscii(url.Substring(0, iqs), Encoding.UTF8)) + 
                      HttpUtility.UrlEncodeNonAscii(url.Substring(iqs), qsEncoding);
            }
            else {
                url = HttpUtility.UrlEncodeSpaces(HttpUtility.UrlEncodeNonAscii(url, Encoding.UTF8)); 
            }
 
            return url; 
        }
 
        internal void UpdateNativeResponse(bool sendHeaders)
        {
            IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest;
 
            if (null == iis7WorkerRequest) {
                return; 
            } 

            // WOS 1841024 - Don't set _suppressContent to true for HEAD requests.  IIS needs the content 
            // in order to correctly set the Content-Length header.
            // WOS 1634512 - need to clear buffers if _ended == true
            // WOS 1850019 - Breaking Change: ASP.NET v2.0: Content-Length is not correct for pages that call HttpResponse.SuppressContent
            if ((_suppressContent && Request != null && Request.HttpVerb != HttpVerb.HEAD) || _ended) 
                Clear();
 
            bool needPush = false; 
            // NOTE: This also sets the response encoding on the HttpWriter
            long bufferedLength = _httpWriter.GetBufferedLength(); 

            //
            // Set headers and status
            // 
            if (!_headersWritten)
            { 
                // 
                // Set status
                // 
                // VSWhidbey 270635: We need to reset the status code for mobile devices.
                if (UseAdaptiveError) {

                    // VSWhidbey 288054: We should change the status code for cases 
                    // that cannot be handled by mobile devices
                    // 4xx for Client Error and 5xx for Server Error in HTTP spec 
                    int statusCode = StatusCode; 
                    if (statusCode >= 400 && statusCode < 600) {
                        this.StatusCode = 200; 
                    }
                }

                if (_statusSet) { 
                    _wr.SendStatus(this.StatusCode, this.SubStatusCode, this.StatusDescription);
                    _statusSet = false; 
                } 

                // 
                //  Set headers
                //
                if (!_suppressHeaders && !_clientDisconnected)
                { 
                    // If redirect location set, write it through to IIS as a header
                    if (_redirectLocation != null && _redirectLocationSet) { 
                        HttpHeaderCollection headers = Headers as HttpHeaderCollection; 
                        headers.Set("Location", _redirectLocation);
                        _redirectLocationSet = false; 
                    }

                    //
                    // If content-type explicitly set, write it through to IIS as a header 
                    // othervise we won't generate it
                    // We no longer suppress content type for empty responses.  If its 
                    // explicitly set, we generate it. 
                    if (_contentType != null && _contentTypeSet) {
                        HttpHeaderCollection headers = Headers as HttpHeaderCollection; 
                        String contentType = AppendCharSetToContentType(_contentType);
                        headers.Set("Content-Type", contentType);
                        _contentTypeSet = false;
                    } 

                    // 
                    // If cookies have been added/changed, set the corresponding headers 
                    //
                    GenerateResponseHeadersForCookies(); 

                    // Not calling WriteHeaders headers in Integrated mode.
                    // Instead, most headers are generated when the handler runs,
                    // or on demand as necessary. 
                    // The only exception are the cache policy headers.
                    if (sendHeaders) { 
 
                        if (_cachePolicy != null) {
                            // if there are any cookies, do not kernel cache the response 
                            if (_cookies != null && _cookies.Count != 0) {
                                _cachePolicy.SetHasSetCookieHeader();
                                DisableKernelCache();
                            } 

                            if (_cachePolicy.IsModified()) { 
                                ArrayList cacheHeaders = new ArrayList(); 
                                _cachePolicy.GetHeaders(cacheHeaders, this);
                                HttpHeaderCollection headers = Headers as HttpHeaderCollection; 
                                foreach (HttpResponseHeader header in cacheHeaders) {
                                    // set and override the header
                                    headers.Set(header.Name, header.Value);
                                } 
                            }
                        } 
 
                        needPush = true;
                    } 
                }
            }

            if (_flushing && !_filteringCompleted) { 
                _httpWriter.FilterIntegrated(false, iis7WorkerRequest);
                bufferedLength = _httpWriter.GetBufferedLength(); 
            } 

            if (!_clientDisconnected && (bufferedLength > 0 || needPush)) { 

                if (bufferedLength == 0 ) {
                    if (_httpWriter.IgnoringFurtherWrites) {
                        return; 
                    }
                } 
 
                // push HttpWriter buffers to worker request
                _httpWriter.Send(_wr); 
                // push buffers through into native
                iis7WorkerRequest.PushResponseToNative();
                // dispose them (since they're copied or
                // owned by native request) 
                _httpWriter.DisposeIntegratedBuffers();
            } 
        } 

        private void ClearNativeResponse(bool clearEntity, bool clearHeaders, IIS7WorkerRequest wr) { 
            wr.ClearResponse(clearEntity, clearHeaders);
            if (clearEntity) {
                _httpWriter.ClearSubstitutionBlocks();
            } 
        }
 
    } 

    internal enum CacheDependencyType { 
        Files,
        CacheItems,
        VirtualPaths
    } 

    struct ResponseDependencyList { 
        private ArrayList   _dependencies; 
        private string[]    _dependencyArray;
        private DateTime    _oldestDependency; 
        private string      _requestVirtualPath;

        internal void AddDependency(string item, string argname) {
            if (item == null) { 
                throw new ArgumentNullException(argname);
            } 
 
            _dependencyArray = null;
 
            if (_dependencies == null) {
                _dependencies = new ArrayList(1);
            }
 
            DateTime utcNow = DateTime.UtcNow;
 
            _dependencies.Add(new ResponseDependencyInfo( 
                    new string[] {item}, utcNow));
 
            // _oldestDependency is initialized to MinValue and indicates that it must always be set
            if (_oldestDependency == DateTime.MinValue || utcNow < _oldestDependency)
                _oldestDependency = utcNow;
        } 

        internal void AddDependencies(ArrayList items, string argname) { 
            if (items == null) { 
                throw new ArgumentNullException(argname);
            } 

            string[] a = (string[]) items.ToArray(typeof(string));
            AddDependencies(a, argname, false);
        } 

        internal void AddDependencies(string[] items, string argname) { 
            AddDependencies(items, argname, true); 
        }
 
        internal void AddDependencies(string[] items, string argname, bool cloneArray) {
            AddDependencies(items, argname, cloneArray, DateTime.UtcNow);
        }
 
        internal void AddDependencies(string[] items, string argname, bool cloneArray, string requestVirtualPath) {
            if (requestVirtualPath == null) 
                throw new ArgumentNullException("requestVirtualPath"); 

            _requestVirtualPath = requestVirtualPath; 
            AddDependencies(items, argname, cloneArray, DateTime.UtcNow);
        }

        internal void AddDependencies(string[] items, string argname, bool cloneArray, DateTime utcDepTime) { 
            if (items == null) {
                throw new ArgumentNullException(argname); 
            } 

            string [] itemsLocal; 

            if (cloneArray) {
                itemsLocal = (string[]) items.Clone();
            } 
            else {
                itemsLocal = items; 
            } 

            foreach (string item in itemsLocal) { 
                if (String.IsNullOrEmpty(item)) {
                    throw new ArgumentNullException(argname);
                }
            } 

            _dependencyArray = null; 
 
            if (_dependencies == null) {
                _dependencies = new ArrayList(1); 
            }

            _dependencies.Add(new ResponseDependencyInfo(itemsLocal, utcDepTime));
 
            // _oldestDependency is initialized to MinValue and indicates that it must always be set
            if (_oldestDependency == DateTime.MinValue || utcDepTime < _oldestDependency) 
                _oldestDependency = utcDepTime; 
        }
 
        internal bool HasDependencies() {
            if (_dependencyArray == null && _dependencies == null)
                return false;
 
            return true;
        } 
 
        internal string[] GetDependencies() {
            if (_dependencyArray == null && _dependencies != null) { 
                int size = 0;
                foreach (ResponseDependencyInfo info in _dependencies) {
                    size += info.items.Length;
                } 

                _dependencyArray = new string[size]; 
 
                int index = 0;
                foreach (ResponseDependencyInfo info in _dependencies) { 
                    int length = info.items.Length;
                    Array.Copy(info.items, 0, _dependencyArray, index, length);
                    index += length;
                } 
            }
 
            return _dependencyArray; 
        }
 
        internal CacheDependency CreateCacheDependency(CacheDependencyType dependencyType, CacheDependency dependency) {
            if (_dependencies != null) {
                if (dependencyType == CacheDependencyType.Files
                    || dependencyType == CacheDependencyType.CacheItems) { 
                    foreach (ResponseDependencyInfo info in _dependencies) {
                        CacheDependency dependencyOld = dependency; 
                        try { 
                            if (dependencyType == CacheDependencyType.Files) {
                                dependency = new CacheDependency(0, info.items, null, dependencyOld, info.utcDate); 
                            }
                            else {
                                // We create a "public" CacheDepdency here, since the keys are for public items.
                                dependency = new CacheDependency(null, info.items, dependencyOld, 
                                                                 DateTimeUtil.ConvertToLocalTime(info.utcDate));
                            } 
                        } 
                        finally {
                            if (dependencyOld != null) { 
                                dependencyOld.Dispose();
                            }
                        }
                    } 
                }
                else { 
                    CacheDependency virtualDependency = null; 
                    VirtualPathProvider vpp = HostingEnvironment.VirtualPathProvider;
                    if (vpp != null && _requestVirtualPath != null) { 
                        virtualDependency = vpp.GetCacheDependency(_requestVirtualPath, GetDependencies(), _oldestDependency);
                    }
                    if (virtualDependency != null) {
                        AggregateCacheDependency tempDep = new AggregateCacheDependency(); 
                        tempDep.Add(virtualDependency);
                        if (dependency != null) { 
                            tempDep.Add(dependency); 
                        }
                        dependency = tempDep; 
                    }
                }
            }
 
            return dependency;
        } 
    } 

    internal class ResponseDependencyInfo { 
        internal readonly string[]    items;
        internal readonly DateTime    utcDate;

        internal ResponseDependencyInfo(string[] items, DateTime utcDate) { 
            this.items = items;
            this.utcDate = utcDate; 
        } 
    }
} 

//------------------------------------------------------------------------------ 
// <copyright file="HttpResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Response intrinsic 
 */
namespace System.Web { 

    using System.Text;
    using System.Threading;
    using System.Runtime.Serialization; 
    using System.IO;
    using System.Collections; 
    using System.Collections.Specialized; 
    using System.Globalization;
    using System.Web.Util; 
    using System.Web.Hosting;
    using System.Web.Caching;
    using System.Web.Configuration;
    using System.Web.UI; 
    using System.Configuration;
    using System.Security.Permissions; 
    using System.Web.Management; 

 
    /// <devdoc>
    ///    <para>Used in HttpResponse.WriteSubstitution.</para>
    /// </devdoc>
    public delegate String HttpResponseSubstitutionCallback(HttpContext context); 

 
    /// <devdoc> 
    ///    <para> Enables type-safe server to browser communication. Used to
    ///       send Http output to a client.</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class HttpResponse {
        private HttpWorkerRequest _wr;              // some response have HttpWorkerRequest 
        private HttpContext _context;               // context
        private HttpWriter _httpWriter;             // and HttpWriter 
        private TextWriter _writer;                 // others just have Writer 

        private HttpHeaderCollection _headers;      // response header collection (IIS7+) 

        private bool _headersWritten;
        private bool _completed;    // after final flush
        private bool _ended;        // after response.end or execute url 
        private bool _flushing;
        private bool _clientDisconnected; 
        private bool _filteringCompleted; 
        private bool _closeConnectionAfterError;
 
        // simple properties

        private int         _statusCode = 200;
        private String      _statusDescription; 
        private bool        _bufferOutput = true;
        private String      _contentType = "text/html"; 
        private String      _charSet; 
        private bool        _customCharSet;
        private bool        _contentLengthSet; 
        private String      _redirectLocation;
        private bool        _redirectLocationSet;
        private Encoding    _encoding;
        private Encoder     _encoder; // cached encoder for the encoding 
        private Encoding    _headerEncoding; // encoding for response headers, default utf-8
        private bool        _cacheControlHeaderAdded; 
        private HttpCachePolicy _cachePolicy; 
        private ArrayList   _cacheHeaders;
        private bool        _suppressHeaders; 
        private bool        _suppressContentSet;
        private bool        _suppressContent;
        private string      _appPathModifier;
        private bool        _isRequestBeingRedirected; 
        private bool        _useAdaptiveError;
        private bool        _handlerHeadersGenerated; 
 
        // complex properties
 
        private ArrayList               _customHeaders;
        private HttpCookieCollection    _cookies;
        #pragma warning disable 0649
        private ResponseDependencyList  _fileDependencyList; 
        private ResponseDependencyList  _virtualPathDependencyList;
        private ResponseDependencyList  _cacheItemDependencyList; 
        #pragma warning restore 0649 
        private AggregateCacheDependency _aggDependency;
        private ErrorFormatter          _overrideErrorFormatter; 

        // cache properties
        int         _expiresInMinutes;
        bool        _expiresInMinutesSet; 
        DateTime    _expiresAbsolute;
        bool        _expiresAbsoluteSet; 
        string      _cacheControl; 

        private bool        _statusSet; 
        private int         _subStatusCode;
        private bool        _versionHeaderSent;
        private bool        _contentTypeSet;
 
        // chunking
        bool        _transferEncodingSet; 
        bool        _chunked; 

        // mobile redirect properties 
        internal static readonly String RedirectQueryStringVariable = "__redir";
        internal static readonly String RedirectQueryStringValue = "1";
        internal static readonly String RedirectQueryStringAssignment = RedirectQueryStringVariable + "=" + RedirectQueryStringValue;
 
        private static readonly String _redirectQueryString = "?" + RedirectQueryStringAssignment;
        private static readonly String _redirectQueryStringInline = RedirectQueryStringAssignment + "&"; 
 
        internal HttpContext Context {
            get { return _context; } 
            set { _context = value; }
        }

        internal HttpRequest Request { 
            get {
                if (_context == null) 
                    return null; 
                return _context.Request;
            } 
        }

        /*
         * Internal package visible constructor to create responses that 
         * have HttpWorkerRequest
         * 
         * @param wr Worker Request 
         */
        internal HttpResponse(HttpWorkerRequest wr, HttpContext context) { 
            _wr = wr;
            _context = context;
            // HttpWriter is created in InitResponseWriter
        } 

        // Public constructor for responses that go to an arbitrary writer 
        // Initializes a new instance of the <see langword='HttpResponse'/> class.</para> 
        public HttpResponse(TextWriter writer) {
            _wr = null; 
            _httpWriter = null;
            _writer = writer;
        }
 
        private bool UsingHttpWriter {
            get { 
                return (_httpWriter != null && _writer == _httpWriter); 
            }
        } 

        /*
         *  Cleanup code
         */ 
        internal void Dispose() {
            // recycle buffers 
            if (_httpWriter != null) 
                _httpWriter.RecycleBuffers();
        } 

        internal void InitResponseWriter() {
            if (_httpWriter == null) {
                _httpWriter = new HttpWriter(this); 

                _writer = _httpWriter; 
            } 
        }
 
        //
        // Private helper methods
        //
 
        private void AppendHeader(HttpResponseHeader h) {
            if (_customHeaders == null) 
                _customHeaders = new ArrayList(); 
            _customHeaders.Add(h);
            if (_cachePolicy != null && StringUtil.EqualsIgnoreCase("Set-Cookie", h.Name)) { 
                _cachePolicy.SetHasSetCookieHeader();
            }
        }
 
        internal bool HeadersWritten {
            get { return _headersWritten; } 
            set { _headersWritten = value; } 
        }
 
        internal ArrayList GenerateResponseHeadersIntegrated(bool forCache) {
            ArrayList headers = new ArrayList();
            HttpHeaderCollection responseHeaders = Headers as HttpHeaderCollection;
            int headerId = 0; 

            // copy all current response headers 
            foreach(String key in responseHeaders) 
            {
                // skip certain headers that the cache does not cache 
                // this is based on the cache headers saved separately in AppendHeader
                // and not generated in GenerateResponseHeaders in ISAPI mode
                headerId = HttpWorkerRequest.GetKnownResponseHeaderIndex(key);
                if (headerId >= 0 && forCache && 
                     (headerId == HttpWorkerRequest.HeaderServer ||
                      headerId == HttpWorkerRequest.HeaderSetCookie || 
                      headerId == HttpWorkerRequest.HeaderCacheControl || 
                      headerId == HttpWorkerRequest.HeaderExpires ||
                      headerId == HttpWorkerRequest.HeaderLastModified || 
                      headerId == HttpWorkerRequest.HeaderEtag ||
                      headerId == HttpWorkerRequest.HeaderVary)) {
                    continue;
                } 

                if ( headerId >= 0 ) { 
                    headers.Add(new HttpResponseHeader(headerId, responseHeaders[key])); 
                }
                else { 
                    headers.Add(new HttpResponseHeader(key, responseHeaders[key]));
                }
            }
 
            return headers;
        } 
 
        internal void GenerateResponseHeadersForCookies()
        { 
            if (_cookies == null || (_cookies.Count == 0 && !_cookies.Changed))
                return; // no cookies exist

            HttpHeaderCollection headers = Headers as HttpHeaderCollection; 
            HttpResponseHeader cookieHeader = null;
            HttpCookie cookie = null; 
            bool needToReset = false; 

            // Go through all cookies, and check whether any have been added 
            // or changed.  If a cookie was added, we can simply generate a new
            // set cookie header for it.  If the cookie collection has been
            // changed (cleared or cookies removed), or an existing cookie was
            // changed, we have to regenerate all Set-Cookie headers due to an IIS 
            // limitation that prevents us from being able to delete specific
            // Set-Cookie headers for items that changed. 
            if (!_cookies.Changed) 
            {
                for(int c = 0; c < _cookies.Count; c++) 
                {
                    cookie = _cookies[c];
                    if (cookie.Added) {
                        // if a cookie was added, we generate a Set-Cookie header for it 
                        cookieHeader = cookie.GetSetCookieHeader(_context);
                        headers.SetHeader(cookieHeader.Name, cookieHeader.Value, false); 
                        cookie.Added = false; 
                        cookie.Changed = false;
                    } 
                    else if (cookie.Changed) {
                        // if a cookie has changed, we need to clear all cookie
                        // headers and re-write them all since we cant delete
                        // specific existing cookies 
                        needToReset = true;
                        break; 
                    } 
                }
            } 


            if (_cookies.Changed || needToReset)
            { 
                // delete all set cookie headers
                headers.Remove("Set-Cookie"); 
 
                // write all the cookies again
                for(int c = 0; c < _cookies.Count; c++) 
                {
                    // generate a Set-Cookie header for each cookie
                    cookie = _cookies[c];
                    cookieHeader = cookie.GetSetCookieHeader(_context); 
                    headers.SetHeader(cookieHeader.Name, cookieHeader.Value, false);
                    cookie.Added = false; 
                    cookie.Changed = false; 
                }
 
                _cookies.Changed = false;
            }
        }
 
        internal void GenerateResponseHeadersForHandler()
        { 
            if ( !(_wr is IIS7WorkerRequest) ) { 
                return;
            } 

            bool sendCacheControlHeader = false;
            String versionHeader = null;
 
            // Generate the default headers associated with an ASP.NET handler
            if (!_headersWritten && !_handlerHeadersGenerated) { 
                try { 
                    // The "sendCacheControlHeader" is default to true, but a false setting in either
                    // the <httpRuntime> section (legacy) or the <outputCache> section (current) will disable 
                    // sending of that header.
                    RuntimeConfig config = RuntimeConfig.GetLKGConfig(_context);

                    HttpRuntimeSection runtimeConfig = config.HttpRuntime; 
                    if (runtimeConfig != null) {
                        versionHeader = runtimeConfig.VersionHeader; 
                        sendCacheControlHeader = runtimeConfig.SendCacheControlHeader; 
                    }
 
                    OutputCacheSection outputCacheConfig = config.OutputCache;
                    if (outputCacheConfig != null) {
                        sendCacheControlHeader &= outputCacheConfig.SendCacheControlHeader;
                    } 

                    // Ensure that cacheability is set to cache-control: private 
                    // if it is not explicitly set 
                    if (sendCacheControlHeader && !_cacheControlHeaderAdded) {
                        Headers.Set("Cache-Control", "private"); 
                    }

                    // set the version header
                    if (!String.IsNullOrEmpty(versionHeader)) { 
                        Headers.Set("X-AspNet-Version", versionHeader);
                    } 
 
                    // Force content-type generation
                    _contentTypeSet = true; 
                }
                finally {
                    _handlerHeadersGenerated = true;
                } 
            }
        } 
 
        internal ArrayList GenerateResponseHeaders(bool forCache) {
            ArrayList   headers = new ArrayList(); 
            bool sendCacheControlHeader = HttpRuntimeSection.DefaultSendCacheControlHeader;

            // ASP.NET version header
            if (!forCache ) { 

                if (!_versionHeaderSent) { 
                    String versionHeader = null; 

                    // The "sendCacheControlHeader" is default to true, but a false setting in either 
                    // the <httpRuntime> section (legacy) or the <outputCache> section (current) will disable
                    // sending of that header.
                    RuntimeConfig config = RuntimeConfig.GetLKGConfig(_context);
 
                    HttpRuntimeSection runtimeConfig = config.HttpRuntime;
                    if (runtimeConfig != null) { 
                        versionHeader = runtimeConfig.VersionHeader; 
                        sendCacheControlHeader = runtimeConfig.SendCacheControlHeader;
                    } 

                    OutputCacheSection outputCacheConfig = config.OutputCache;
                    if (outputCacheConfig != null) {
                        sendCacheControlHeader &= outputCacheConfig.SendCacheControlHeader; 
                    }
 
                    if (!String.IsNullOrEmpty(versionHeader)) { 
                        headers.Add(new HttpResponseHeader("X-AspNet-Version", versionHeader));
                    } 

                    _versionHeaderSent = true;
                }
            } 

            // custom headers 
            if (_customHeaders != null) { 
                int numCustomHeaders = _customHeaders.Count;
                for (int i = 0; i < numCustomHeaders; i++) 
                    headers.Add(_customHeaders[i]);
            }

            // location of redirect 
            if (_redirectLocation != null) {
                headers.Add(new HttpResponseHeader(HttpWorkerRequest.HeaderLocation, _redirectLocation)); 
            } 

            // don't include headers that the cache changes or omits on a cache hit 
            if (!forCache) {
                // cookies
                if (_cookies != null) {
                    int numCookies = _cookies.Count; 

                    for (int i = 0; i < numCookies; i++) { 
                        headers.Add(_cookies[i].GetSetCookieHeader(Context)); 
                    }
                } 

                // cache policy
                if (_cachePolicy != null && _cachePolicy.IsModified()) {
                    _cachePolicy.GetHeaders(headers, this); 
                }
                else { 
                    if (_cacheHeaders != null) { 
                        headers.AddRange(_cacheHeaders);
                    } 

                    /*
                     * Ensure that cacheability is set to cache-control: private
                     * if it is not explicitly set. 
                     */
                    if (!_cacheControlHeaderAdded && sendCacheControlHeader) { 
                        headers.Add(new HttpResponseHeader(HttpWorkerRequest.HeaderCacheControl, "private")); 
                    }
                } 
            }

            //
            // content type 
            //
            if ( _statusCode != 204 && _contentType != null) { 
                String contentType = AppendCharSetToContentType( _contentType ); 
                headers.Add(new HttpResponseHeader(HttpWorkerRequest.HeaderContentType, contentType));
            } 

            // done
            return headers;
        } 

        internal string AppendCharSetToContentType(string contentType) 
        { 
            String newContentType = contentType;
 
            // charset=xxx logic -- append if
            //      not there already and
            //          custom set or response encoding used by http writer to convert bytes to chars
            if (_customCharSet || (_httpWriter != null && _httpWriter.ResponseEncodingUsed)) { 
                if (contentType.IndexOf("charset=", StringComparison.Ordinal) < 0) {
                    String charset = Charset; 
                    if (charset.Length > 0) { // not suppressed 
                        newContentType = contentType + "; charset=" + charset;
                    } 
                }
            }

            return newContentType; 
        }
 
        internal bool UseAdaptiveError { 
            get {
                return _useAdaptiveError; 
            }
            set {
                _useAdaptiveError = value;
            } 
        }
 
        private void WriteHeaders() { 
            if (_wr == null)
                return; 

             // Fire pre-send headers event

            if (_context != null && _context.ApplicationInstance != null) { 
                _context.ApplicationInstance.RaiseOnPreSendRequestHeaders();
            } 
 
            // status
            // VSWhidbey 270635: We need to reset the status code for mobile devices. 
            if (UseAdaptiveError) {

                // VSWhidbey 288054: We should change the status code for cases
                // that cannot be handled by mobile devices 
                // 4xx for Client Error and 5xx for Server Error in HTTP spec
                int statusCode = StatusCode; 
                if (statusCode >= 400 && statusCode < 600) { 
                    this.StatusCode = 200;
                } 
            }

            _wr.SendStatus(this.StatusCode, this.StatusDescription);
 
            // headers encoding
 
            // unicode messes up the response badly 
            Debug.Assert(!this.HeaderEncoding.Equals(Encoding.Unicode));
            _wr.SetHeaderEncoding(this.HeaderEncoding); 

            // headers

            ArrayList headers = GenerateResponseHeaders(false); 
            HttpResponseHeader header = null;
            int n = (headers != null) ? headers.Count : 0; 
            for (int i = 0; i < n; i++) 
            {
                header = headers[i] as HttpResponseHeader; 
                header.Send(_wr);
            }
        }
 
        internal int GetBufferedLength() {
            // if length is greater than Int32.MaxValue, Convert.ToInt32 will throw. 
            // This is okay until we support large response sizes 
            return (_httpWriter != null) ? Convert.ToInt32(_httpWriter.GetBufferedLength()) : 0;
        } 

        private static byte[] s_chunkSuffix = new byte[2] { (byte)'\r', (byte)'\n'};
        private static byte[] s_chunkEnd    = new byte[5] { (byte)'0',  (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n'};
 
        private void Flush(bool finalFlush) {
            // Already completed or inside Flush? 
            if (_completed || _flushing) 
                return;
 
            // Special case for non HTTP Writer
            if (_httpWriter == null) {
                _writer.Flush();
                return; 
            }
 
            // Avoid recursive flushes 
            _flushing = true;
 
            try {

                IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest;
                if (iis7WorkerRequest != null) { 
                    // generate the handler headers if flushing
                    GenerateResponseHeadersForHandler(); 
 
                    // Push buffers across to native side and explicitly flush.
                    // IIS7 handles the chunking as necessary so we can omit that logic 
                    UpdateNativeResponse(true /*sendHeaders*/);

                    // force a synchronous send
                    iis7WorkerRequest.ExplicitFlush(); 

                    _headersWritten = true; 
 
                    return;
                } 

                long bufferedLength = 0;

                // 
                // Headers
                // 
 
                if (!_headersWritten) {
                    if (!_suppressHeaders && !_clientDisconnected) { 
                        if (finalFlush) {
                            bufferedLength = _httpWriter.GetBufferedLength();

                            // suppress content-type for empty responses 
                            if (!_contentLengthSet && bufferedLength == 0 && _httpWriter != null)
                                _contentType = null; 
 
                            // if there are any cookies, do not kernel cache the response
                            if (_cachePolicy != null && _cookies != null && _cookies.Count != 0) { 
                                _cachePolicy.SetHasSetCookieHeader();
                                DisableKernelCache();
                            }
 
                            // generate response headers
                            WriteHeaders(); 
 
                            // recalculate as sending headers might change it (PreSendHeaders)
                            bufferedLength = _httpWriter.GetBufferedLength(); 

                            // Calculate content-length if not set explicitely
                            // WOS #1380818: Content-Length should not be set for response with 304 status (HTTP.SYS doesn't, and HTTP 1.1 spec implies it)
                            if (!_contentLengthSet && _statusCode != 304) 
                                _wr.SendCalculatedContentLength(bufferedLength);
                        } 
                        else { 
                            // Check if need chunking for HTTP/1.1
                            if (!_contentLengthSet && !_transferEncodingSet && _statusCode == 200) { 
                                String protocol = _wr.GetHttpVersion();

                                if (protocol != null && protocol.Equals("HTTP/1.1")) {
                                    AppendHeader(new HttpResponseHeader(HttpWorkerRequest.HeaderTransferEncoding, "chunked")); 
                                    _chunked = true;
                                } 
 
                                bufferedLength = _httpWriter.GetBufferedLength();
                            } 

                            WriteHeaders();
                        }
                    } 

                    _headersWritten = true; 
                } 
                else {
                    bufferedLength = _httpWriter.GetBufferedLength(); 
                }

                //
                // Filter and recalculate length if not done already 
                //
 
                if (!_filteringCompleted) { 
                    _httpWriter.Filter(false);
                    bufferedLength = _httpWriter.GetBufferedLength(); 
                }

                //
                // Content 
                //
 
                // suppress HEAD content unless overriden 
                if (!_suppressContentSet && Request != null && Request.HttpVerb == HttpVerb.HEAD)
                    _suppressContent = true; 

                if (_suppressContent || _ended) {
                    _httpWriter.ClearBuffers();
                    bufferedLength = 0; 
                }
 
                if (!_clientDisconnected) { 
                    // Fire pre-send request event
                    if (_context != null && _context.ApplicationInstance != null) 
                        _context.ApplicationInstance.RaiseOnPreSendRequestContent();

                    if (_chunked) {
                        if (bufferedLength > 0) { 
                            byte[] chunkPrefix = Encoding.ASCII.GetBytes(Convert.ToString(bufferedLength, 16) + "\r\n");
                            _wr.SendResponseFromMemory(chunkPrefix, chunkPrefix.Length); 
 
                            _httpWriter.Send(_wr);
 
                            _wr.SendResponseFromMemory(s_chunkSuffix, s_chunkSuffix.Length);
                        }

                        if (finalFlush) 
                            _wr.SendResponseFromMemory(s_chunkEnd, s_chunkEnd.Length);
                    } 
                    else { 
                        _httpWriter.Send(_wr);
                    } 

                    _wr.FlushResponse(finalFlush);

                    if (!finalFlush) 
                        _httpWriter.ClearBuffers();
                } 
            } 
            finally {
                _flushing = false; 

                // Remember if completed
                if (finalFlush)
                    _completed = true; 
            }
        } 
 
        internal void FinalFlushAtTheEndOfRequestProcessing() {
            FinalFlushAtTheEndOfRequestProcessing(false); 
        }

        internal void FinalFlushAtTheEndOfRequestProcessing(bool needPipelineCompletion) {
                Flush(true); 
        }
 
        // WOS 1555777: kernel cache support 
        // If the response can be kernel cached, return the kernel cache key;
        // otherwise return null.  The kernel cache key is used to invalidate 
        // the entry if a dependency changes or the item is flushed from the
        // managed cache for any reason.
        internal String SetupKernelCaching(String originalCacheUrl) {
            // don't kernel cache if we have a cookie header 
            if (_cookies != null && _cookies.Count != 0) {
                _cachePolicy.SetHasSetCookieHeader(); 
                return null; 
            }
 
            bool enableKernelCacheForVaryByStar = IsKernelCacheEnabledForVaryByStar();

            // check cache policy
            if (!_cachePolicy.IsKernelCacheable(Request, enableKernelCacheForVaryByStar)) { 
                return null;
            } 
 
            // check configuration if the kernel mode cache is enabled
            HttpRuntimeSection runtimeConfig = RuntimeConfig.GetLKGConfig(_context).HttpRuntime; 
            if (runtimeConfig == null || !runtimeConfig.EnableKernelOutputCache) {
                return null;
            }
 
            double seconds = (_cachePolicy.UtcGetAbsoluteExpiration() - DateTime.UtcNow).TotalSeconds;
            if (seconds <= 0) { 
                return null; 
            }
 
            int secondsToLive = seconds < Int32.MaxValue ? (int) seconds : Int32.MaxValue;
            string kernelCacheUrl = _wr.SetupKernelCaching(secondsToLive, originalCacheUrl, enableKernelCacheForVaryByStar);

            if (kernelCacheUrl != null) { 
                // Tell cache policy not to use max-age as kernel mode cache doesn't update it
                _cachePolicy.SetNoMaxAgeInCacheControl(); 
            } 

            return kernelCacheUrl; 
        }

        /*
         * Disable kernel caching for this response.  If kernel caching is not supported, this method 
         * returns without performing any action.
         */ 
        public void DisableKernelCache() { 
            if (_wr == null) {
                return; 
            }

            _wr.DisableKernelCache();
        } 

        private bool IsKernelCacheEnabledForVaryByStar() 
        { 
            OutputCacheSection outputCacheConfig = RuntimeConfig.GetAppConfig().OutputCache;
            return (_cachePolicy.IsVaryByStar && outputCacheConfig.EnableKernelCacheForVaryByStar); 
        }

        internal void FilterOutput() {
            try { 
                if (UsingHttpWriter) {
                    IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest; 
                    if (iis7WorkerRequest != null) { 
                        _httpWriter.FilterIntegrated(true, iis7WorkerRequest);
                    } 
                    else {
                        _httpWriter.Filter(true);
                    }
                } 
            }
            finally { 
                _filteringCompleted = true; 
            }
        } 

        /// <devdoc>
        /// Prevents any other writes to the Response
        /// </devdoc> 
        internal void IgnoreFurtherWrites() {
            if (UsingHttpWriter) { 
                _httpWriter.IgnoreFurtherWrites(); 
            }
        } 

        /*
         * Is the entire response buffered so far
         */ 
        internal bool IsBuffered() {
            return !_headersWritten && UsingHttpWriter; 
        } 

        //  Expose cookie collection to request 
        //    Gets the HttpCookie collection sent by the current request.</para>
        public HttpCookieCollection Cookies {
            get {
                if (_cookies == null) 
                    _cookies = new HttpCookieCollection(this, false);
 
                return _cookies; 
            }
        } 

        public NameValueCollection Headers {
            get {
                if ( !(_wr is IIS7WorkerRequest) ) { 
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode));
                } 
 
                if (_headers == null) {
                    _headers = new HttpHeaderCollection(_wr, this, 16); 
                }

                return _headers;
            } 
        }
 
        /* 
         * Add dependency on a file to the current response
         */ 

        /// <devdoc>
        ///    <para>Adds dependency on a file to the current response.</para>
        /// </devdoc> 
        public void AddFileDependency(String filename) {
            _fileDependencyList.AddDependency(filename, "filename"); 
        } 

        // Add dependency on a list of files to the current response 

        //   Adds dependency on a group of files to the current response.
        public void AddFileDependencies(ArrayList filenames) {
            _fileDependencyList.AddDependencies(filenames, "filenames"); 
        }
 
 
        public void AddFileDependencies(string[] filenames) {
            _fileDependencyList.AddDependencies(filenames, "filenames"); 
        }

        internal void AddVirtualPathDependencies(string[] virtualPaths) {
            _virtualPathDependencyList.AddDependencies(virtualPaths, "virtualPaths", false, Request.Path); 
        }
 
        internal void AddFileDependencies(string[] filenames, DateTime utcTime) { 
            _fileDependencyList.AddDependencies(filenames, "filenames", false, utcTime);
        } 

        // Add dependency on another cache item to the response.
        public void AddCacheItemDependency(string cacheKey) {
            _cacheItemDependencyList.AddDependency(cacheKey, "cacheKey"); 
        }
 
        // Add dependency on a list of cache items to the response. 
        public void AddCacheItemDependencies(ArrayList cacheKeys) {
            _cacheItemDependencyList.AddDependencies(cacheKeys, "cacheKeys"); 
        }


        public void AddCacheItemDependencies(string[] cacheKeys) { 
            _cacheItemDependencyList.AddDependencies(cacheKeys, "cacheKeys");
        } 
 
        // Add dependency on one or more CacheDependency objects to the response
        public void AddCacheDependency(params CacheDependency[] dependencies) { 
            if (_aggDependency == null) {
                _aggDependency = new AggregateCacheDependency();
            }
 
            _aggDependency.Add(dependencies);
            Cache.SetDependencies(true); 
        } 

        public static void RemoveOutputCacheItem(string path) { 
            if (path == null)
                throw new ArgumentNullException("path");
            if (StringUtil.StringStartsWith(path, "\\\\") || path.IndexOf(':') >= 0 || !UrlPath.IsRooted(path))
                throw new ArgumentException(SR.GetString(SR.Invalid_path_for_remove, path)); 

            CacheInternal cacheInternal = HttpRuntime.CacheInternal; 
 
            string key = OutputCacheModule.CreateOutputCachedItemKey(
                    path, HttpVerb.GET, null, null); 

            cacheInternal.Remove(key);

            key = OutputCacheModule.CreateOutputCachedItemKey( 
                    path, HttpVerb.POST, null, null);
 
            cacheInternal.Remove(key); 
        }
 
        // Get the list of file dependencies.
        internal string[] GetFileDependencies() {
            return _fileDependencyList.GetDependencies();
        } 

        // Get the list of cache item dependencies. 
        internal string[] GetCacheItemDependencies() { 
            return _cacheItemDependencyList.GetDependencies();
        } 

        // Check if there are file dependencies.
        internal bool HasFileDependencies() {
            return _fileDependencyList.HasDependencies(); 
        }
 
        // Check if there are item dependencies. 
        internal bool HasCacheItemDependencies() {
            return _cacheItemDependencyList.HasDependencies(); 
        }

        internal CacheDependency GetCacheDependency() {
            return _aggDependency; 
        }
 
        internal CacheDependency GetVirtualPathDependency() { 
            return _virtualPathDependencyList.CreateCacheDependency(CacheDependencyType.VirtualPaths, null);
        } 

        internal CacheDependency CreateCacheDependencyForResponse(CacheDependency dependencyVary) {
            CacheDependency dependency;
 
            // N.B. - add file dependencies last so that we hit the file changes monitor
            // just once. 
            dependency = _cacheItemDependencyList.CreateCacheDependency(CacheDependencyType.CacheItems, dependencyVary); 
            dependency = _fileDependencyList.CreateCacheDependency(CacheDependencyType.Files, dependency);
            dependency = _virtualPathDependencyList.CreateCacheDependency(CacheDependencyType.VirtualPaths, dependency); 

            // N.B. we add in the aggregate dependency here, and return it,
            // so this function should only be called once, because the resulting
            // dependency can only be added to the cache once 
            AggregateCacheDependency aggDependency;
            if (_aggDependency != null) { 
                aggDependency = _aggDependency; 
            }
            else { 
                aggDependency = new AggregateCacheDependency();
            }

            if (dependency != null) { 
                aggDependency.Add(dependency);
            } 
 
            return aggDependency;
        } 

        // Get response headers and content as HttpRawResponse
        internal HttpRawResponse GetSnapshot() {
            int statusCode = 200; 
            string statusDescription = null;
            ArrayList headers = null; 
            ArrayList buffers = null; 
            bool hasSubstBlocks = false;
 
            if (!IsBuffered())
                throw new HttpException(SR.GetString(SR.Cannot_get_snapshot_if_not_buffered));

            IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest; 

            // data 
            if (!_suppressContent) { 
                if (iis7WorkerRequest != null) {
                    buffers = _httpWriter.GetIntegratedSnapshot(out hasSubstBlocks, iis7WorkerRequest); 
                }
                else {
                    buffers = _httpWriter.GetSnapshot(out hasSubstBlocks);
                } 
            }
 
            // headers (after data as the data has side effects (like charset, see ASURT 113202)) 
            if (!_suppressHeaders) {
                statusCode = _statusCode; 
                statusDescription = _statusDescription;
                // In integrated pipeline, we need to use the current response headers
                // from the response (these may have been generated by other handlers, etc)
                // instead of the ASP.NET cached headers 
                if (iis7WorkerRequest != null) {
                    headers = GenerateResponseHeadersIntegrated(true); 
                } 
                else {
                    headers = GenerateResponseHeaders(true); 
                }
            }
            return new HttpRawResponse(statusCode, statusDescription, headers, buffers, hasSubstBlocks);
        } 

        // Send saved response snapshot as the entire response 
        internal void UseSnapshot(HttpRawResponse rawResponse, bool sendBody) { 
            if (_headersWritten)
                throw new HttpException(SR.GetString(SR.Cannot_use_snapshot_after_headers_sent)); 

            if (_httpWriter == null)
                throw new HttpException(SR.GetString(SR.Cannot_use_snapshot_for_TextWriter));
 
            ClearAll();
 
            // restore status 
            StatusCode = rawResponse.StatusCode;
            StatusDescription = rawResponse.StatusDescription; 

            // restore headers
            ArrayList headers = rawResponse.Headers;
            int n = (headers != null) ? headers.Count : 0; 
            for (int i = 0; i < n; i++) {
                HttpResponseHeader h = (HttpResponseHeader)(headers[i]); 
                this.AppendHeader(h.Name, h.Value); 
            }
 
            // restore content
            _httpWriter.UseSnapshot(rawResponse.Buffers);

            _suppressContent = !sendBody; 
        }
 
        internal void CloseConnectionAfterError() { 
            _closeConnectionAfterError = true;
        } 

        private void WriteErrorMessage(Exception e, bool dontShowSensitiveErrors) {
            ErrorFormatter errorFormatter = null;
            CultureInfo uiculture = null, savedUiculture = null; 
            bool needToRestoreUiculture = false;
 
            if (_context.DynamicUICulture != null) { 
                // if the user set the culture dynamically use it
                uiculture =  _context.DynamicUICulture; 
            }
            else  {
                // get the UI culture under which the error text must be created (use LKG to avoid errors while reporting error)
                GlobalizationSection globConfig = RuntimeConfig.GetLKGConfig(_context).Globalization; 
                if ((globConfig != null) && (!String.IsNullOrEmpty(globConfig.UICulture))) {
                    try { 
                        uiculture = HttpServerUtility.CreateReadOnlyCultureInfo(globConfig.UICulture); 
                    }
                    catch { 
                    }
                }
            }
 
            //  In Integrated mode, generate the necessary response headers for the error
            GenerateResponseHeadersForHandler(); 
 
            // set the UI culture
            if (uiculture != null) { 
                savedUiculture = Thread.CurrentThread.CurrentUICulture;
                Thread.CurrentThread.CurrentUICulture = uiculture;
                needToRestoreUiculture = true;
            } 

            try { 
                try { 
                    // Try to get an error formatter
                    errorFormatter = GetErrorFormatter(e); 
#if DBG
                    Debug.Trace("internal", "Error stack for " + Request.Path, e);
#endif
                    if (dontShowSensitiveErrors && !errorFormatter.CanBeShownToAllUsers) 
                        errorFormatter = new GenericApplicationErrorFormatter(Request.IsLocal);
 
                    Debug.Trace("internal", "errorFormatter's type = " +  errorFormatter.GetType()); 

                    if (ErrorFormatter.RequiresAdaptiveErrorReporting(Context)) { 
                        _writer.Write(errorFormatter.GetAdaptiveErrorMessage(Context, dontShowSensitiveErrors));
                    }
                    else {
                        _writer.Write(errorFormatter.GetHtmlErrorMessage(dontShowSensitiveErrors)); 

                        // Write a stack dump in an HTML comment for debugging purposes 
                        // Only show it for Asp permission medium or higher (ASURT 126373) 
                        if (!dontShowSensitiveErrors &&
                            HttpRuntime.HasAspNetHostingPermission(AspNetHostingPermissionLevel.Medium)) { 
                            _writer.Write("<!-- \r\n");
                            WriteExceptionStack(e);
                            _writer.Write("-->");
                        } 
                         if (!dontShowSensitiveErrors && !Request.IsLocal ) {
                             _writer.Write("<!-- \r\n"); 
                             _writer.Write(SR.GetString(SR.Information_Disclosure_Warning)); 
                             _writer.Write("-->");
                         } 
                    }

                    if (_closeConnectionAfterError) {
                        Flush(); 
                        Close();
                    } 
                } 
                finally {
                    // restore ui culture 
                    if (needToRestoreUiculture)
                        Thread.CurrentThread.CurrentUICulture = savedUiculture;
                }
            } 
            catch { // Protect against exception filters
                throw; 
            } 
        }
 
        internal void SetOverrideErrorFormatter(ErrorFormatter errorFormatter) {
            _overrideErrorFormatter = errorFormatter;
        }
 
        internal ErrorFormatter GetErrorFormatter(Exception e) {
            ErrorFormatter  errorFormatter = null; 
 
            if (_overrideErrorFormatter != null) {
                return _overrideErrorFormatter; 
            }

            // Try to get an error formatter
            errorFormatter = HttpException.GetErrorFormatter(e); 

            if (errorFormatter == null) { 
                ConfigurationException ce = e as ConfigurationException; 
                if (ce != null && !String.IsNullOrEmpty(ce.Filename))
                    errorFormatter = new ConfigErrorFormatter(ce); 
            }

            // If we couldn't get one, create one here
            if (errorFormatter == null) { 
                // If it's a 404, use a special error page, otherwise, use a more
                // generic one. 
                if (_statusCode == 404) 
                    errorFormatter = new PageNotFoundErrorFormatter(Request.Path);
                else if (_statusCode == 403) 
                    errorFormatter = new PageForbiddenErrorFormatter(Request.Path);
                else {
                    if (e is System.Security.SecurityException)
                        errorFormatter = new SecurityErrorFormatter(e); 
                    else
                        errorFormatter = new UnhandledErrorFormatter(e); 
                } 
            }
 
            return errorFormatter;
        }

        private void WriteOneExceptionStack(Exception e) { 
            Exception subExcep = e.InnerException;
            if (subExcep != null) 
                WriteOneExceptionStack(subExcep); 

            string title = "[" + e.GetType().Name + "]"; 
            if (e.Message != null && e.Message.Length > 0)
                title += ": " + HttpUtility.HtmlEncode(e.Message);

            _writer.WriteLine(title); 
            if (e.StackTrace != null)
                _writer.WriteLine(e.StackTrace); 
        } 

        private void WriteExceptionStack(Exception e) { 
            ConfigurationErrorsException errors = e as ConfigurationErrorsException;
            if (errors == null) {
                WriteOneExceptionStack(e);
            } 
            else {
                // Write the original exception to get the first error with 
                // a full stack trace 
                WriteOneExceptionStack(e);
 
                // Write additional errors, which will contain truncated stacks
                ICollection col = errors.Errors;
                if (col.Count > 1) {
                    bool firstSkipped = false; 
                    foreach (ConfigurationException ce in col) {
                        if (!firstSkipped) { 
                            firstSkipped = true; 
                            continue;
                        } 

                        _writer.WriteLine("---");
                        WriteOneExceptionStack(ce);
                    } 
                }
            } 
        } 

        internal void ReportRuntimeError(Exception e, bool canThrow, bool localExecute) { 
            CustomErrorsSection customErrorsSetting = null;
            bool useCustomErrors = false;
            int code = -1;
 
            if (_completed)
                return; 
 
            // always try to disable IIS custom errors when we send an error
            if (_wr != null) { 
                _wr.TrySkipIisCustomErrors = true;
            }

            if (!localExecute) { 
                code = HttpException.GetHttpCodeForException(e);
 
                // Don't raise event for 404.  See VSWhidbey 124147. 
                if (code != 404) {
                    WebBaseEvent.RaiseRuntimeError(e, this); 
                }

                // This cannot use the HttpContext.IsCustomErrorEnabled property, since it must call
                // GetSettings() with the canThrow parameter. 
                customErrorsSetting = CustomErrorsSection.GetSettings(_context, canThrow);
                if (customErrorsSetting != null) 
                    useCustomErrors = customErrorsSetting.CustomErrorsEnabled(Request); 
                else
                    useCustomErrors = true; 
            }

            if (!_headersWritten) {
                // nothing sent yet - entire response 

                if (code == -1) { 
                    code = HttpException.GetHttpCodeForException(e); 
                }
 
                // change 401 to 500 in case the config is not to impersonate
                if (code == 401 && !_context.IsClientImpersonationConfigured)
                    code = 500;
 
                if (_context.TraceIsEnabled)
                    _context.Trace.StatusCode = code; 
 
                if (!localExecute && useCustomErrors) {
                    String redirect = (customErrorsSetting != null) ? customErrorsSetting.GetRedirectString(code) : null; 

                    if (redirect == null || !RedirectToErrorPage(redirect)) {
                        // if no redirect display generic error
                        ClearAll(); 
                        StatusCode = code;
                        WriteErrorMessage(e, true); 
                    } 
                }
                else { 
                    ClearAll();
                    StatusCode = code;
                    WriteErrorMessage(e, false);
                } 
            }
            else { 
                Clear(); 

                if (_contentType != null && _contentType.Equals("text/html")) { 
                    // in the middle of Html - break Html
                    Write("\r\n\r\n</pre></table></table></table></table></table>");
                    Write("</font></font></font></font></font>");
                    Write("</i></i></i></i></i></b></b></b></b></b></u></u></u></u></u>"); 
                    Write("<p>&nbsp;</p><hr>\r\n\r\n");
                } 
 
                WriteErrorMessage(e, useCustomErrors);
            } 
        }

        internal void SynchronizeStatus(int statusCode, int subStatusCode, string description) {
            _statusCode = statusCode; 
            _subStatusCode = subStatusCode;
            _statusDescription = description; 
        } 

 
        internal void SynchronizeHeader(int knownHeaderIndex, string name, string value) {
            HttpHeaderCollection headers = Headers as HttpHeaderCollection;
            headers.SynchronizeHeader(name, value);
 
            // unknown headers have an index < 0
            if (knownHeaderIndex < 0) { 
                return; 
            }
 
            switch (knownHeaderIndex) {
                case HttpWorkerRequest.HeaderCacheControl:
                    _cacheControlHeaderAdded = true;
                    break; 
                case HttpWorkerRequest.HeaderContentType:
                    _contentType = value; 
                    _contentTypeSet = false; 
                    break;
                case HttpWorkerRequest.HeaderLocation: 
                    _redirectLocation = value;
                    _redirectLocationSet = false;
                    break;
                case HttpWorkerRequest.HeaderSetCookie: 
                    // If the header is Set-Cookie, update the corresponding
                    // cookie in the cookies collection 
                    if (value != null) { 
                        HttpCookie cookie = HttpRequest.CreateCookieFromString(value);
                        Cookies.Set(cookie); 
                        // do not write this cookie back to IIS
                        cookie.Changed = false;
                        cookie.Added = false;
                    } 
                    break;
            } 
        } 

        internal void SyncStatusIntegrated() { 
            Debug.Assert(_wr is IIS7WorkerRequest, "_wr is IIS7WorkerRequest");
             if (!_headersWritten && _statusSet) {
                 // For integrated pipeline, synchronize the status immediately so that the FREB log
                 // correctly indicates the module and notification that changed the status. 
                 _wr.SendStatus(_statusCode, _subStatusCode, this.StatusDescription);
                 _statusSet = false; 
             } 
        }
 
        // Public properties

        // Http status code
        //    Gets or sets the HTTP status code of output returned to client. 
        public int StatusCode {
            get { 
                return _statusCode; 
            }
 
            set {
                if (_headersWritten)
                    throw new HttpException(SR.GetString(SR.Cannot_set_status_after_headers_sent));
 
                if (_statusCode != value) {
                    _statusCode = value; 
                    _subStatusCode = 0; 
                    _statusDescription = null;
                    _statusSet = true; 
                }
            }
        }
 
        // the IIS sub status code
        // since this doesn't get emitted in the protocol 
        // we won't send it through the worker request interface 
        // directly
        public int SubStatusCode { 
            get {
                if ( !(_wr is IIS7WorkerRequest) ) {
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode));
                } 

                return _subStatusCode; 
            } 
            set {
                if ( !(_wr is IIS7WorkerRequest) ) { 
                    throw new PlatformNotSupportedException(SR.GetString(SR.Requires_Iis_Integrated_Mode));
                }

                if (_headersWritten) { 
                    throw new HttpException(SR.GetString(SR.Cannot_set_status_after_headers_sent));
                } 
 
                _subStatusCode = value;
                _statusSet = true; 
            }
        }

        /* 
         * Http status description string
         */ 
 
        // Http status description string
        //    Gets or sets the HTTP status string of output returned to the client. 
        public String StatusDescription {
            get {
                if (_statusDescription == null)
                    _statusDescription = HttpWorkerRequest.GetStatusDescription(_statusCode); 

                return _statusDescription; 
            } 

            set { 
                if (_headersWritten)
                    throw new HttpException(SR.GetString(SR.Cannot_set_status_after_headers_sent));

                if (value != null && value.Length > 512)  // ASURT 124743 
                    throw new ArgumentOutOfRangeException("value");
                _statusDescription = value; 
                _statusSet = true; 
            }
        } 

        public bool TrySkipIisCustomErrors {
            get {
                if (_wr != null) { 
                    return _wr.TrySkipIisCustomErrors;
                } 
                return false; 
            }
            set { 
                if (_wr != null) {
                    _wr.TrySkipIisCustomErrors = value;
                }
            } 
        }
 
        // Flag indicating to buffer the output 
        //    Gets or sets a value indicating whether HTTP output is buffered.
        public bool BufferOutput { 
            get {
                return _bufferOutput;
            }
 
            set {
                if (_bufferOutput != value) { 
                    _bufferOutput = value; 

                    if (_httpWriter != null) 
                        _httpWriter.UpdateResponseBuffering();
                }
            }
        } 

        // Gets the Content-Encoding HTTP response header. 
        internal String GetHttpHeaderContentEncoding() { 
            string coding = null;
            if (_wr is IIS7WorkerRequest) { 
                if (_headers != null) {
                    coding = _headers["Content-Encoding"];
                }
            } 
            else if (_customHeaders != null) {
                int numCustomHeaders = _customHeaders.Count; 
                for (int i = 0; i < numCustomHeaders; i++) { 
                    HttpResponseHeader h = (HttpResponseHeader)_customHeaders[i];
                    if (h.Name == "Content-Encoding") { 
                        coding = h.Value;
                        break;
                    }
                } 
            }
            return coding; 
        } 

        /* 
         * Content-type
         */

        /// <devdoc> 
        ///    <para>Gets or sets the
        ///       HTTP MIME type of output.</para> 
        /// </devdoc> 
        public String ContentType {
            get { 
                return _contentType;
            }

            set { 
                if (_headersWritten) {
                    // Don't throw if the new content type is the same as the current one 
                    if (_contentType == value) 
                        return;
 
                    throw new HttpException(SR.GetString(SR.Cannot_set_content_type_after_headers_sent));
                }

                _contentTypeSet = true; 
                _contentType = value;
            } 
        } 

 
        //    Gets or sets the HTTP charset of output.
        public String Charset {
            get {
                if (_charSet == null) 
                    _charSet = ContentEncoding.WebName;
 
                return _charSet; 
            }
 
            set {
                if (_headersWritten)
                    throw new HttpException(SR.GetString(SR.Cannot_set_content_type_after_headers_sent));
 
                if (value != null)
                    _charSet = value; 
                else 
                    _charSet = String.Empty;  // to differentiate between not set (default) and empty chatset
 
                _customCharSet = true;
            }
        }
 
        // Content encoding for conversion
        //   Gets or sets the HTTP character set of output. 
        public Encoding ContentEncoding { 
            get {
                if (_encoding == null) { 
                    // use LKG config because Response.ContentEncoding is need to display [config] error
                    GlobalizationSection globConfig = RuntimeConfig.GetLKGConfig(_context).Globalization;
                    if (globConfig != null)
                        _encoding = globConfig.ResponseEncoding; 

                    if (_encoding == null) 
                        _encoding = Encoding.Default; 
                }
 
                return _encoding;
            }

            set { 
                if (value == null)
                    throw new ArgumentNullException("value"); 
 
                if (_encoding == null || !_encoding.Equals(value)) {
                    _encoding = value; 
                    _encoder = null;   // flush cached encoder

                    if (_httpWriter != null)
                        _httpWriter.UpdateResponseEncoding(); 
                }
            } 
        } 

 
        public Encoding HeaderEncoding {
            get {
                if (_headerEncoding == null) {
                    // use LKG config because Response.ContentEncoding is need to display [config] error 
                    GlobalizationSection globConfig = RuntimeConfig.GetLKGConfig(_context).Globalization;
                    if (globConfig != null) 
                        _headerEncoding = globConfig.ResponseHeaderEncoding; 

                    // default to UTF-8 (also for Unicode as headers cannot be double byte encoded) 
                    if (_headerEncoding == null || _headerEncoding.Equals(Encoding.Unicode))
                        _headerEncoding = Encoding.UTF8;
                }
 
                return _headerEncoding;
            } 
 
            set {
                if (value == null) 
                    throw new ArgumentNullException("value");

                if (value.Equals(Encoding.Unicode)) {
                    throw new HttpException(SR.GetString(SR.Invalid_header_encoding, value.WebName)); 
                }
 
                if (_headerEncoding == null || !_headerEncoding.Equals(value)) { 
                    if (_headersWritten)
                        throw new HttpException(SR.GetString(SR.Cannot_set_header_encoding_after_headers_sent)); 

                    _headerEncoding = value;
                }
            } 
        }
 
        // Encoder cached for the current encoding 
        internal Encoder ContentEncoder {
            get { 
                if (_encoder == null) {
                    Encoding e = ContentEncoding;
                    _encoder = e.GetEncoder();
 
                    // enable best fit mapping accoding to config
                    // (doesn't apply to utf-8 which is the default, thus optimization) 
 
                    if (!e.Equals(Encoding.UTF8)) {
                        bool enableBestFit = false; 

                        GlobalizationSection globConfig = RuntimeConfig.GetLKGConfig(_context).Globalization;
                        if (globConfig != null) {
                            enableBestFit = globConfig.EnableBestFitResponseEncoding; 
                        }
 
                        if (!enableBestFit) { 
                            // setting 'fallback' disables best fit mapping
                            _encoder.Fallback = new EncoderReplacementFallback(); 
                        }
                    }
                }
                return _encoder; 
            }
        } 
 
        // Cache policy
        //    Returns the caching semantics of the Web page (expiration time, privacy, vary clauses). 
        public HttpCachePolicy Cache {
            get {
                if (_cachePolicy == null) {
                    _cachePolicy = new HttpCachePolicy(); 
                }
 
                return _cachePolicy; 
            }
        } 

        // Return whether or not we have cache policy. We don't want to create it in
        // situations where we don't modify it.
        internal bool HasCachePolicy { 
            get {
                return _cachePolicy != null; 
            } 
        }
 
        // Client connected flag
        //   Gets a value indicating whether the client is still connected to the server.
        public bool IsClientConnected {
            get { 
                if (_clientDisconnected)
                    return false; 
 
                if (_wr != null && !_wr.IsClientConnected()) {
                    _clientDisconnected = true; 
                    return false;
                }

                return true; 
            }
        } 
 
        public bool IsRequestBeingRedirected {
            get { 
                return _isRequestBeingRedirected;
            }
        }
 

        /// <devdoc> 
        ///    <para>Gets or Sets a redirection string (value of location resposne header) for redirect response.</para> 
        /// </devdoc>
        public String RedirectLocation { 
            get { return _redirectLocation; }
            set {
                if (_headersWritten)
                    throw new HttpException(SR.GetString(SR.Cannot_append_header_after_headers_sent)); 

                _redirectLocation = value; 
                _redirectLocationSet = true; 
            }
        } 

        /*
         * Disconnect client
         */ 

        /// <devdoc> 
        ///    <para>Closes the socket connection to a client.</para> 
        /// </devdoc>
        public void Close() { 
            if (!_clientDisconnected && !_completed && _wr != null) {
                _wr.CloseConnection();
                _clientDisconnected = true;
            } 
        }
 
        // TextWriter object 
        //    Enables custom output to the outgoing Http content body.
        public TextWriter Output { 
            get { return _writer;}
        }

        internal TextWriter SwitchWriter(TextWriter writer) { 
            TextWriter oldWriter = _writer;
            _writer = writer; 
            return oldWriter; 
        }
 
        // Output stream
        //       Enables binary output to the outgoing Http content body.
        public Stream OutputStream {
            get { 
                if (!UsingHttpWriter)
                    throw new HttpException(SR.GetString(SR.OutputStream_NotAvail)); 
 
                return _httpWriter.OutputStream;
            } 
        }

        // ASP classic compat
        //    Writes a string of binary characters to the HTTP output stream. 
        public void BinaryWrite(byte[] buffer) {
            OutputStream.Write(buffer, 0, buffer.Length); 
        } 

 
        //  Appends a PICS (Platform for Internet Content Selection) label HTTP header to the output stream.
        public void Pics(String value) {
            AppendHeader("PICS-Label", value);
        } 

        // Filtering stream 
        //       Specifies a wrapping filter object to modify HTTP entity body before transmission. 
        public Stream Filter {
            get { 
                if (UsingHttpWriter)
                    return _httpWriter.GetCurrentFilter();
                else
                    return null; 
            }
 
            set { 
                if (UsingHttpWriter) {
                    _httpWriter.InstallFilter(value); 

                    IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest;
                    if (iis7WorkerRequest != null) {
                        iis7WorkerRequest.ResponseFilterInstalled(); 
                    }
                } 
                else 
                    throw new HttpException(SR.GetString(SR.Filtering_not_allowed));
            } 

        }

        // Flag to suppress writing of content 
        //    Gets or sets a value indicating that HTTP content will not be sent to client.
        public bool SuppressContent { 
            get { 
                return _suppressContent;
            } 

            set {
                _suppressContent = value;
                _suppressContentSet = true; 
            }
        } 
 
        //
        // Public methods 
        //

        /*
          * Add Http custom header 
          *
          * @param name header name 
          * @param value header value 
          */
 
        /// <devdoc>
        ///    <para>Adds an HTTP
        ///       header to the output stream.</para>
        /// </devdoc> 
        public void AppendHeader(String name, String value) {
            bool isCacheHeader = false; 
 
            if (_headersWritten)
                throw new HttpException(SR.GetString(SR.Cannot_append_header_after_headers_sent)); 

            // some headers are stored separately or require special action
            int knownHeaderIndex = HttpWorkerRequest.GetKnownResponseHeaderIndex(name);
 
            switch (knownHeaderIndex) {
                case HttpWorkerRequest.HeaderContentType: 
                    ContentType = value; 
                    return; // don't keep as custom header
 
                case HttpWorkerRequest.HeaderContentLength:
                    _contentLengthSet = true;
                    break;
 
                case HttpWorkerRequest.HeaderLocation:
                    RedirectLocation = value; 
                    return; // don't keep as custom header 

                case HttpWorkerRequest.HeaderTransferEncoding: 
                    _transferEncodingSet = true;
                    break;

                case HttpWorkerRequest.HeaderCacheControl: 
                    _cacheControlHeaderAdded = true;
                    goto case HttpWorkerRequest.HeaderExpires; 
                case HttpWorkerRequest.HeaderExpires: 
                case HttpWorkerRequest.HeaderLastModified:
                case HttpWorkerRequest.HeaderEtag: 
                case HttpWorkerRequest.HeaderVary:
                    isCacheHeader = true;
                    break;
            } 

            // In integrated mode, write the headers directly 
            if (_wr is IIS7WorkerRequest) { 
                Headers.Add(HttpResponseHeader.MaybeEncodeHeader(name), HttpResponseHeader.MaybeEncodeHeader(value));
            } 
            else {
                if (isCacheHeader)
                {
                    // don't keep as custom header 
                    if (_cacheHeaders == null) {
                        _cacheHeaders = new ArrayList(); 
                    } 

                    _cacheHeaders.Add(new HttpResponseHeader(knownHeaderIndex, value)); 
                    return;
                }
                else {
                    HttpResponseHeader h; 
                    if (knownHeaderIndex >= 0)
                        h = new HttpResponseHeader(knownHeaderIndex, value); 
                    else 
                        h = new HttpResponseHeader(name, value);
 
                    AppendHeader(h);
                }
            }
        } 

 
        /// <internalonly/> 
        /// <devdoc>
        ///    <para> 
        ///       Adds an HTTP
        ///       cookie to the output stream.
        ///    </para>
        /// </devdoc> 
        public void AppendCookie(HttpCookie cookie) {
            if (_headersWritten) 
                throw new HttpException(SR.GetString(SR.Cannot_append_cookie_after_headers_sent)); 

            Cookies.AddCookie(cookie, true); 
            OnCookieAdd(cookie);
        }

 
        /// <internalonly/>
        /// <devdoc> 
        /// </devdoc> 
        public void SetCookie(HttpCookie cookie) {
            if (_headersWritten) 
                throw new HttpException(SR.GetString(SR.Cannot_append_cookie_after_headers_sent));

            Cookies.AddCookie(cookie, false);
            OnCookieCollectionChange(); 
        }
 
        internal void BeforeCookieCollectionChange() { 
            if (_headersWritten)
                throw new HttpException(SR.GetString(SR.Cannot_modify_cookies_after_headers_sent)); 
        }

        internal void OnCookieAdd(HttpCookie cookie) {
            // add to request's cookies as well 
            Request.AddResponseCookie(cookie);
        } 
 
        internal void OnCookieCollectionChange() {
            // synchronize with request cookie collection 
            Request.ResetCookies();
        }

        // Clear response headers 
        //    Clears all headers from the buffer stream.
        public void ClearHeaders() { 
            if (_headersWritten) 
                throw new HttpException(SR.GetString(SR.Cannot_clear_headers_after_headers_sent));
 
            StatusCode = 200;
            _subStatusCode = 0;
            _statusDescription = null;
 
            _contentType = "text/html";
            _charSet = null; 
            _customCharSet = false; 
            _contentLengthSet = false;
 
            _redirectLocation = null;
            _redirectLocationSet = false;
            _isRequestBeingRedirected = false;
            _handlerHeadersGenerated = false; 

            _customHeaders = null; 
 
            if (_headers != null) {
                _headers.ClearInternal(); 
            }

            _transferEncodingSet = false;
            _chunked = false; 

            if (_cookies != null) { 
                _cookies.Reset(); 
                Request.ResetCookies();
            } 

            if (_cachePolicy != null) {
                _cachePolicy.Reset();
            } 

            _cacheControlHeaderAdded = false; 
            _cacheHeaders = null; 

            _suppressHeaders = false; 
            _suppressContent = false;
            _suppressContentSet = false;

            _expiresInMinutes = 0; 
            _expiresInMinutesSet = false;
            _expiresAbsolute = DateTime.MinValue; 
            _expiresAbsoluteSet = false; 
            _cacheControl = null;
 
            IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest;
            if (iis7WorkerRequest != null) {
                // clear the native response as well
                ClearNativeResponse(false, true, iis7WorkerRequest); 
            }
 
        } 

 
        /// <devdoc>
        ///    <para>Clears all content output from the buffer stream.</para>
        /// </devdoc>
        public void ClearContent() { 
            Clear();
        } 
 
        /*
         * Clear response buffer and headers. (For ASP compat doesn't clear headers) 
         */

        /// <devdoc>
        ///    <para>Clears all headers and content output from the buffer stream.</para> 
        /// </devdoc>
        public void Clear() { 
            if (UsingHttpWriter) 
                _httpWriter.ClearBuffers();
 
            IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest;
            if (iis7WorkerRequest != null) {
                // clear the native response buffers too
                ClearNativeResponse(true, false, iis7WorkerRequest); 
            }
 
 
        }
 
        /*
         * Clear response buffer and headers. Internal. Used to be 'Clear'.
         */
        internal void ClearAll() { 
            if (!_headersWritten)
                ClearHeaders(); 
            Clear(); 
        }
 
        /*
         * Flush response currently buffered
         */
 
        /// <devdoc>
        ///    <para>Sends all currently buffered output to the client.</para> 
        /// </devdoc> 
        public void Flush() {
            if (_completed) 
                throw new HttpException(SR.GetString(SR.Cannot_flush_completed_response));

            Flush(false);
        } 

        /* 
         * Append string to the log record 
         *
         * @param param string to append to the log record 
         */

        /// <devdoc>
        ///    <para>Adds custom log information to the IIS log file.</para> 
        /// </devdoc>
        [AspNetHostingPermission(SecurityAction.Demand, Level=AspNetHostingPermissionLevel.Medium)] 
        public void AppendToLog(String param) { 
            // only makes sense for IIS
            if (_wr is System.Web.Hosting.ISAPIWorkerRequest) 
                ((System.Web.Hosting.ISAPIWorkerRequest)_wr).AppendLogParameter(param);
            else if (_wr is System.Web.Hosting.IIS7WorkerRequest)
                _context.Request.AppendToLogQueryString(param);
        } 

 
        /// <devdoc> 
        ///    <para>Redirects a client to a new URL.</para>
        /// </devdoc> 
        public void Redirect(String url) {
            Redirect(url, true);
        }
 

        /// <devdoc> 
        ///    <para>Redirects a client to a new URL.</para> 
        /// </devdoc>
        public void Redirect(String url, bool endResponse) { 
            if (url == null)
                throw new ArgumentNullException("url");

            if (url.IndexOf('\n') >= 0) 
                throw new ArgumentException(SR.GetString(SR.Cannot_redirect_to_newline));
 
            if (_headersWritten) 
                throw new HttpException(SR.GetString(SR.Cannot_redirect_after_headers_sent));
 
            Page page = _context.Handler as Page;
            if ((page != null) && page.IsCallback) {
                throw new ApplicationException(SR.GetString(SR.Redirect_not_allowed_in_callback));
            } 

            url = ApplyRedirectQueryStringIfRequired(url); 
 
            url = ApplyAppPathModifier(url);
 
            url = ConvertToFullyQualifiedRedirectUrlIfRequired(url);

            url = UrlEncodeRedirect(url);
 
            Clear();
 
            // If it's a Page and SmartNavigation is on, return a short script 
            // to perform the redirect instead of returning a 302 (bugs ASURT 82331/86782)
#pragma warning disable 0618    // To avoid SmartNavigation deprecation warning 
            if (page != null && page.IsPostBack && page.SmartNavigation && (Request["__smartNavPostBack"] == "true")) {
#pragma warning restore 0618
                Write("<BODY><ASP_SMARTNAV_RDIR url=\"");
                Write(HttpUtility.HtmlEncode(url)); 
                Write("\"></ASP_SMARTNAV_RDIR>");
 
                Write("</BODY>"); 
            }
            else { 
                this.StatusCode = 302;
                RedirectLocation = url;
                Write("<html><head><title>Object moved</title></head><body>\r\n");
                Write("<h2>Object moved to <a href=\"" + HttpUtility.HtmlAttributeEncode(url) + "\">here</a>.</h2>\r\n"); 
                Write("</body></html>\r\n");
            } 
 
            _isRequestBeingRedirected = true;
 
            if (endResponse)
                End();
        }
 
        internal string ApplyRedirectQueryStringIfRequired(string url) {
            if (Request == null || (string)Request.Browser["requiresPostRedirectionHandling"] != "true") 
                return url; 

            Page page = _context.Handler as Page; 
            if (page != null && !page.IsPostBack)
                return url;

            //do not add __redir=1 if it already exists 
            int i = url.IndexOf(RedirectQueryStringAssignment, StringComparison.Ordinal);
            if(i == -1) { 
                i = url.IndexOf('?'); 
                if (i >= 0) {
                    url = url.Insert(i + 1, _redirectQueryStringInline); 
                }
                else {
                    url = String.Concat(url, _redirectQueryString);
                } 
            }
            return url; 
        } 

        // 
        // Redirect to error page appending ?aspxerrorpath if no query string in the url.
        // Fails to redirect if request is already for error page.
        // Suppresses all errors.
        // Returns true if redirect performed successfuly 
        //
        internal bool RedirectToErrorPage(String url) { 
            const String qsErrorMark = "aspxerrorpath"; 

            try { 
                if (String.IsNullOrEmpty(url))
                    return false;   // nowhere to redirect

                if (_headersWritten) 
                    return false;
 
                if (Request.QueryString[qsErrorMark] != null) 
                    return false;   // already in error redirect
 
                // append query string
                if (url.IndexOf('?') < 0)
                    url = url + "?" + qsErrorMark + "=" + HttpUtility.UrlEncodeSpaces(Request.Path);
 
                // redirect without response.end
                Redirect(url, false /*endResponse*/); 
            } 
            catch {
                return false; 
            }

            return true;
        } 

        // Implementation of the DefaultHttpHandler for IIS6+ 
        internal bool CanExecuteUrlForEntireResponse { 
            get {
                // if anything is sent, too late 
                if (_headersWritten) {
                    return false;
                }
 
                // must have the right kind of worker request
                if (_wr == null || !_wr.SupportsExecuteUrl) { 
                    return false; 
                }
 
                // must not be capturing output to custom writer
                if (!UsingHttpWriter) {
                    return false;
                } 

                // there is some cached output not yet sent 
                if (_httpWriter.GetBufferedLength() != 0) { 
                    return false;
                } 

                // can't use execute url with filter installed
                if (_httpWriter.FilterInstalled) {
                    return false; 
                }
 
                if (_cachePolicy != null && _cachePolicy.IsModified()) { 
                    return false;
                } 

                return true;
            }
        } 

        internal IAsyncResult BeginExecuteUrlForEntireResponse( 
                                    String pathOverride, NameValueCollection requestHeaders, 
                                    AsyncCallback cb, Object state) {
            Debug.Assert(CanExecuteUrlForEntireResponse); 

            // prepare user information
            String userName, userAuthType;
            if (_context != null && _context.User != null) { 
                userName     = _context.User.Identity.Name;
                userAuthType = _context.User.Identity.AuthenticationType; 
            } 
            else {
                userName = String.Empty; 
                userAuthType = String.Empty;
            }

            // get the path 
            String path = Request.RewrittenUrl; // null is ok
 
            if (pathOverride != null) { 
                path = pathOverride;
            } 

            // get the headers
            String headers = null;
 
            if (requestHeaders != null) {
                int numHeaders = requestHeaders.Count; 
 
                if (numHeaders > 0) {
                    StringBuilder sb = new StringBuilder(); 

                    for (int i = 0; i < numHeaders; i++) {
                        sb.Append(requestHeaders.GetKey(i));
                        sb.Append(": "); 
                        sb.Append(requestHeaders.Get(i));
                        sb.Append("\r\n"); 
                    } 

                    headers = sb.ToString(); 
                }
            }

            byte[] entity = null; 
            if (_context != null && _context.Request != null) {
                entity = _context.Request.EntityBody; 
            } 

            Debug.Trace("ExecuteUrl", "HttpResponse.BeginExecuteUrlForEntireResponse:" + 
                " path=" + path + " headers=" + headers +
                " userName=" + userName + " authType=" + userAuthType);

            // call worker request to start async execute url for this request 
            IAsyncResult ar = _wr.BeginExecuteUrl(
                    path, 
                    null, // this method 
                    headers,
                    true, // let execute url send headers 
                    true, // add user info
                    _wr.GetUserToken(),
                    userName,
                    userAuthType, 
                    entity,
                    cb, 
                    state); 

            // suppress further sends from ASP.NET 
            // (only if succeeded starting async operation - not is 'finally' block)
            _headersWritten = true;
            _ended = true;
 
            return ar;
        } 
 
        internal void EndExecuteUrlForEntireResponse(IAsyncResult result) {
            Debug.Trace("ExecuteUrl", "HttpResponse.EndExecuteUrlForEntireResponse"); 
            _wr.EndExecuteUrl(result);
        }

        // Methods to write from file 

        //    Writes values to an HTTP output content stream. 
        public void Write(String s) { 
            _writer.Write(s);
        } 

        // Writes values to an HTTP output content stream.
        public void Write(Object obj) {
            _writer.Write(obj); 
        }
 
 
        /// <devdoc>
        ///    <para>Writes values to an HTTP output content stream.</para> 
        /// </devdoc>
        public void Write(char ch) {
            _writer.Write(ch);
        } 

 
        /// <devdoc> 
        ///    <para>Writes values to an HTTP output content stream.</para>
        /// </devdoc> 
        public void Write(char[] buffer, int index, int count) {
            _writer.Write(buffer, index, count);
        }
 

        /// <devdoc> 
        ///    <para>Writes a substition block to the response.</para> 
        /// </devdoc>
        public void WriteSubstitution(HttpResponseSubstitutionCallback callback) { 
            // cannot be instance method on a control
            if (callback.Target != null && callback.Target is Control) {
                throw new ArgumentException(SR.GetString(SR.Invalid_substitution_callback), "callback");
            } 

            if (UsingHttpWriter) { 
                // HttpWriter can take substitution blocks 
                _httpWriter.WriteSubstBlock(callback, _wr as IIS7WorkerRequest);
            } 
            else {
                // text writer -- write as string
                _writer.Write(callback(_context));
            } 

            // set the cache policy: reduce cachability from public to server 
            if (_cachePolicy != null && _cachePolicy.GetCacheability() == HttpCacheability.Public) 
                _cachePolicy.SetCacheability(HttpCacheability.Server);
        } 

        /*
         * Helper method to write from file stream
         * 
         * Handles only TextWriter case. For real requests
         * HttpWorkerRequest can take files 
         */ 
        private void WriteStreamAsText(Stream f, long offset, long size) {
            if (size < 0) 
                size = f.Length - offset;

            if (size > 0) {
                if (offset > 0) 
                    f.Seek(offset, SeekOrigin.Begin);
 
                byte[] fileBytes = new byte[(int)size]; 
                int bytesRead = f.Read(fileBytes, 0, (int)size);
                _writer.Write(Encoding.Default.GetChars(fileBytes, 0, bytesRead)); 
            }
        }

        // support for VirtualPathProvider 
        internal void WriteVirtualFile(VirtualFile vf) {
            Debug.Trace("WriteVirtualFile", vf.Name); 
 
            using (Stream s = vf.Open()) {
                if (UsingHttpWriter) { 
                    long size = s.Length;

                    if (size > 0) {
                        // write as memory block 
                        byte[] fileBytes = new byte[(int)size];
                        int bytesRead = s.Read(fileBytes, 0, (int) size); 
                        _httpWriter.WriteBytes(fileBytes, 0, bytesRead); 
                    }
                } 
                else {
                    // Write file contents
                    WriteStreamAsText(s, 0, -1);
                } 
            }
        } 
 
        // Helper method to get absolute physical filename from the argument to WriteFile
        private String GetNormalizedFilename(String fn) { 
            // If it's not a physical path, call MapPath on it
            if (!UrlPath.IsAbsolutePhysicalPath(fn)) {
                if (Request != null)
                    fn = Request.MapPath(fn); // relative to current request 
                else
                    fn = HostingEnvironment.MapPath(fn); 
            } 

            return fn; 
        }

        // Write file
        ///  Writes a named file directly to an HTTP content output stream. 
        public void WriteFile(String filename) {
            if (filename == null) { 
                throw new ArgumentNullException("filename"); 
            }
 
            WriteFile(filename, false);
        }

        /* 
         * Write file
         * 
         * @param filename file to write 
         * @readIntoMemory flag to read contents into memory immediately
         */ 

        /// <devdoc>
        ///    <para> Reads a file into a memory block.</para>
        /// </devdoc> 
        public void WriteFile(String filename, bool readIntoMemory) {
            if (filename == null) { 
                throw new ArgumentNullException("filename"); 
            }
 
            filename = GetNormalizedFilename(filename);

            FileStream f = null;
 
            try {
                f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read); 
 
                if (UsingHttpWriter) {
                    long size = f.Length; 

                    if (size > 0) {
                        if (readIntoMemory) {
                            // write as memory block 
                            byte[] fileBytes = new byte[(int)size];
                            int bytesRead = f.Read(fileBytes, 0, (int) size); 
                            _httpWriter.WriteBytes(fileBytes, 0, bytesRead); 
                        }
                        else { 
                            // write as file block
                            f.Close(); // close before writing
                            f = null;
                            _httpWriter.WriteFile(filename, 0, size); 
                        }
                    } 
                } 
                else {
                    // Write file contents 
                    WriteStreamAsText(f, 0, -1);
                }
            }
            finally { 
                if (f != null)
                    f.Close(); 
            } 
        }
 

        public void TransmitFile(string filename) {
            TransmitFile(filename, 0, -1);
        } 
        public void TransmitFile(string filename, long offset, long length) {
            if (filename == null) { 
                throw new ArgumentNullException("filename"); 
            }
            if (offset < 0) 
                throw new ArgumentException(SR.GetString(SR.Invalid_range), "offset");
            if (length < -1)
                throw new ArgumentException(SR.GetString(SR.Invalid_range), "length");
 
            if (!UsingHttpWriter) {
                if (offset != 0 || length != -1) { 
                    throw new PlatformNotSupportedException(); 
                }
                WriteFile(filename); 
                return;
            }
            if ((offset != 0 || length != -1) && _wr is ISAPIWorkerRequestOutOfProc)
                throw new PlatformNotSupportedException(); 

            filename = GetNormalizedFilename(filename); 
 
            long size;
            using (FileStream f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) { 
                size = f.Length;
            }

            if (size < offset) 
                throw new ArgumentException(SR.GetString(SR.Invalid_range), "offset");
 
            if (length == -1) 
                length = size - offset;
            else if ((size - offset) < length) 
                throw new ArgumentException(SR.GetString(SR.Invalid_range), "offset");

            if (length > 0) {
                bool supportsLongTransmitFile = (_wr != null && _wr.SupportsLongTransmitFile); 

                _httpWriter.TransmitFile(filename, offset, length, 
                   _context.IsClientImpersonationConfigured || HttpRuntime.IsOnUNCShareInternal, supportsLongTransmitFile); 
            }
        } 


        private void ValidateFileRange(String filename, long offset, long length) {
            FileStream f = null; 

            try { 
                f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read); 

                long fileSize = f.Length; 

                if (length == -1)
                    length = fileSize - offset;
 
                if (offset < 0 || length > fileSize - offset)
                    throw new HttpException(SR.GetString(SR.Invalid_range)); 
            } 
            finally {
                if (f != null) 
                    f.Close();
            }
        }
 
        /*
         * Write file 
         * 
         * @param filename file to write
         * @param offset file offset to start writing 
         * @param size number of bytes to write
         */

        /// <devdoc> 
        ///    <para>Writes a file directly to an HTTP content output stream.</para>
        /// </devdoc> 
        public void WriteFile(String filename, long offset, long size) { 
            if (filename == null) {
                throw new ArgumentNullException("filename"); 
            }

            if (size == 0)
                return; 

            filename = GetNormalizedFilename(filename); 
 
            ValidateFileRange(filename, offset, size);
 
            if (UsingHttpWriter) {
                // HttpWriter can take files -- don't open here (but Demand permission)
                InternalSecurityPermissions.FileReadAccess(filename).Demand();
                _httpWriter.WriteFile(filename, offset, size); 
            }
            else { 
                FileStream f = null; 

                try { 
                    f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    WriteStreamAsText(f, offset, size);
                }
                finally { 
                    if (f != null)
                        f.Close(); 
                } 
            }
        } 

        /*
         * Write file
         * 
         * @param handle file to write
         * @param offset file offset to start writing 
         * @param size number of bytes to write 
         */
 
        /// <devdoc>
        ///    <para>Writes a file directly to an HTTP content output stream.</para>
        /// </devdoc>
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)] 
        public void WriteFile(IntPtr fileHandle, long offset, long size) {
            if (size <= 0) 
                return; 

            FileStream f = null; 

            try {
                f = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(fileHandle,false), FileAccess.Read);
 
                if (UsingHttpWriter) {
                    long fileSize = f.Length; 
 
                    if (size == -1)
                        size = fileSize - offset; 

                    if (offset < 0 || size > fileSize - offset)
                        throw new HttpException(SR.GetString(SR.Invalid_range));
 
                    if (offset > 0)
                        f.Seek(offset, SeekOrigin.Begin); 
 
                    // write as memory block
                    byte[] fileBytes = new byte[(int)size]; 
                    int bytesRead = f.Read(fileBytes, 0, (int)size);
                    _httpWriter.WriteBytes(fileBytes, 0, bytesRead);
                }
                else { 
                    WriteStreamAsText(f, offset, size);
                } 
            } 
            finally {
                if (f != null) 
                    f.Close();
            }
        }
 
        //
        // Deprecated ASP compatibility methods and properties 
        // 

 
        /// <devdoc>
        ///    <para>
        ///       Same as StatusDescription. Provided only for ASP compatibility.
        ///    </para> 
        /// </devdoc>
        public string Status { 
            get { 
                return this.StatusCode.ToString(NumberFormatInfo.InvariantInfo) + " " + this.StatusDescription;
            } 

            set {
                int code = 200;
                String descr = "OK"; 

                try { 
                    int i = value.IndexOf(' '); 
                    code = Int32.Parse(value.Substring(0, i), CultureInfo.InvariantCulture);
                    descr = value.Substring(i+1); 
                }
                catch {
                    throw new HttpException(SR.GetString(SR.Invalid_status_string));
                } 

                this.StatusCode = code; 
                this.StatusDescription = descr; 
            }
        } 


        /// <devdoc>
        ///    <para> 
        ///       Same as BufferOutput. Provided only for ASP compatibility.
        ///    </para> 
        /// </devdoc> 
        public bool Buffer {
            get { return this.BufferOutput;} 
            set { this.BufferOutput = value;}
        }

 
        /// <devdoc>
        ///    <para>Same as Appendheader. Provided only for ASP compatibility.</para> 
        /// </devdoc> 
        public void AddHeader(String name, String value) {
            AppendHeader(name, value); 
        }

        /*
         * Cancelles handler processing of the current request 
         * throws special [non-]exception uncatchable by the user code
         * to tell application to stop module execution. 
         */ 

        /// <devdoc> 
        ///    <para>Sends all currently buffered output to the client then closes the
        ///       socket connection.</para>
        /// </devdoc>
        public void End() { 

            if (_context.IsInCancellablePeriod) { 
                InternalSecurityPermissions.ControlThread.Assert(); 
                Thread.CurrentThread.Abort(new HttpApplication.CancelModuleException(false));
            } 
            else {
                // when cannot abort execution, flush and supress further output
                if (!_flushing) { // ignore Reponse.End while flushing (in OnPreSendHeaders)
                    Flush(); 
                    _ended = true;
 
                    if (_context.ApplicationInstance != null) { 
                        _context.ApplicationInstance.CompleteRequest();
                    } 
                }
            }
        }
 
        /*
         * ASP compatible caching properties 
         */ 

 
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the time, in minutes, until cached
        ///       information will be removed from the cache. Provided for ASP compatiblility. Use 
        ///       the <see cref='System.Web.HttpResponse.Cache'/>
        ///       Property instead. 
        ///    </para> 
        /// </devdoc>
        public int Expires { 
            get {
                return _expiresInMinutes;
            }
            set { 
                if (!_expiresInMinutesSet || value < _expiresInMinutes) {
                    _expiresInMinutes = value; 
                    Cache.SetExpires(_context.Timestamp + new TimeSpan(0, _expiresInMinutes, 0)); 
                }
            } 
        }


        /// <devdoc> 
        ///    <para>
        ///       Gets or sets the absolute time that cached information 
        ///       will be removed from the cache. Provided for ASP compatiblility. Use the <see cref='System.Web.HttpResponse.Cache'/> 
        ///       property instead.
        ///    </para> 
        /// </devdoc>
        public DateTime ExpiresAbsolute {
            get {
                return _expiresAbsolute; 
            }
            set { 
                if (!_expiresAbsoluteSet || value < _expiresAbsolute) { 
                    _expiresAbsolute = value;
                    Cache.SetExpires(_expiresAbsolute); 
                }
            }
        }
 

        /// <devdoc> 
        ///    <para> 
        ///       Provided for ASP compatiblility. Use the <see cref='System.Web.HttpResponse.Cache'/>
        ///       property instead. 
        ///    </para>
        /// </devdoc>
        public string CacheControl {
            get { 
                if (_cacheControl == null) {
                    // the default 
                    return "private"; 
                }
 
                return _cacheControl;
            }
            set {
                if (String.IsNullOrEmpty(value)) { 
                    _cacheControl = null;
                    Cache.SetCacheability(HttpCacheability.NoCache); 
                } 
                else if (StringUtil.EqualsIgnoreCase(value, "private")) {
                    _cacheControl = value; 
                    Cache.SetCacheability(HttpCacheability.Private);
                }
                else if (StringUtil.EqualsIgnoreCase(value, "public")) {
                    _cacheControl = value; 
                    Cache.SetCacheability(HttpCacheability.Public);
                } 
                else if (StringUtil.EqualsIgnoreCase(value, "no-cache")) { 
                    _cacheControl = value;
                    Cache.SetCacheability(HttpCacheability.NoCache); 
                }
                else {
                    throw new ArgumentException(SR.GetString(SR.Invalid_value_for_CacheControl, value));
                } 
            }
        } 
 
        internal void SetAppPathModifier(string appPathModifier) {
            if (appPathModifier != null && ( 
                appPathModifier.Length == 0 ||
                appPathModifier[0] == '/' ||
                appPathModifier[appPathModifier.Length - 1] == '/')) {
 
                throw new ArgumentException(SR.GetString(SR.InvalidArgumentValue, "appPathModifier"));
            } 
 
            _appPathModifier = appPathModifier;
        } 


        public string ApplyAppPathModifier(string virtualPath) {
            object ch = _context.CookielessHelper; // This ensures that the cookieless-helper is initialized and applies the AppPathModifier 
            if (virtualPath == null)
                return null; 
 
            if (UrlPath.IsRelativeUrl(virtualPath)) {
                virtualPath = UrlPath.Combine(Request.FilePathObject.VirtualPathString, virtualPath); 
            }
            else {
                // ignore paths with http://server/... or //
                if (!UrlPath.IsRooted(virtualPath) || virtualPath.StartsWith("//", StringComparison.Ordinal)) { 
                    return virtualPath;
                } 
 
                virtualPath = UrlPath.Reduce(virtualPath);
            } 

            if (_appPathModifier == null || virtualPath.IndexOf(_appPathModifier, StringComparison.Ordinal) >= 0)
                return virtualPath;
 
            string appPath = HttpRuntime.AppDomainAppVirtualPathString;
 
            int compareLength = appPath.Length; 
            bool isVirtualPathShort = (virtualPath.Length == appPath.Length - 1);
            if (isVirtualPathShort) { 
                compareLength--;
            }

            // String.Compare will throw exception if there aren't compareLength characters 
            if (virtualPath.Length < compareLength) {
                return virtualPath; 
            } 

            if (!StringUtil.EqualsIgnoreCase(virtualPath, 0, appPath, 0, compareLength)) { 
                return virtualPath;
            }

            if (isVirtualPathShort) { 
                virtualPath += "/";
            } 
 
            Debug.Assert(virtualPath.Length >= appPath.Length);
            if (virtualPath.Length == appPath.Length) { 
                virtualPath = virtualPath.Substring(0, appPath.Length) + _appPathModifier + "/";
            }
            else {
                virtualPath = 
                    virtualPath.Substring(0, appPath.Length) +
                    _appPathModifier + 
                    "/" + 
                    virtualPath.Substring(appPath.Length);
            } 

            return virtualPath;
        }
 
        internal String RemoveAppPathModifier(string virtualPath) {
            if (String.IsNullOrEmpty(_appPathModifier)) 
                return virtualPath; 

            int pos = virtualPath.IndexOf(_appPathModifier, StringComparison.Ordinal); 

            if (pos <= 0 || virtualPath[pos-1] != '/')
                return virtualPath;
 
            return virtualPath.Substring(0, pos-1) + virtualPath.Substring(pos + _appPathModifier.Length);
        } 
 
        private String ConvertToFullyQualifiedRedirectUrlIfRequired(String url) {
            HttpRuntimeSection runtimeConfig = RuntimeConfig.GetConfig(_context).HttpRuntime; 
            if (    runtimeConfig.UseFullyQualifiedRedirectUrl ||
                    (Request != null && (string)Request.Browser["requiresFullyQualifiedRedirectUrl"] == "true")) {
                return (new Uri(Request.Url, url)).AbsoluteUri ;
            } 
            else {
                return url; 
            } 
        }
 
        private String UrlEncodeRedirect(String url) {
            // convert all non-ASCII chars before ? to %XX using UTF-8 and
            // after ? using Response.ContentEncoding
 
            int iqs = url.IndexOf('?');
 
            if (iqs >= 0) { 
                Encoding qsEncoding = (Request != null) ? Request.ContentEncoding : ContentEncoding;
                url = HttpUtility.UrlEncodeSpaces(HttpUtility.UrlEncodeNonAscii(url.Substring(0, iqs), Encoding.UTF8)) + 
                      HttpUtility.UrlEncodeNonAscii(url.Substring(iqs), qsEncoding);
            }
            else {
                url = HttpUtility.UrlEncodeSpaces(HttpUtility.UrlEncodeNonAscii(url, Encoding.UTF8)); 
            }
 
            return url; 
        }
 
        internal void UpdateNativeResponse(bool sendHeaders)
        {
            IIS7WorkerRequest iis7WorkerRequest = _wr as IIS7WorkerRequest;
 
            if (null == iis7WorkerRequest) {
                return; 
            } 

            // WOS 1841024 - Don't set _suppressContent to true for HEAD requests.  IIS needs the content 
            // in order to correctly set the Content-Length header.
            // WOS 1634512 - need to clear buffers if _ended == true
            // WOS 1850019 - Breaking Change: ASP.NET v2.0: Content-Length is not correct for pages that call HttpResponse.SuppressContent
            if ((_suppressContent && Request != null && Request.HttpVerb != HttpVerb.HEAD) || _ended) 
                Clear();
 
            bool needPush = false; 
            // NOTE: This also sets the response encoding on the HttpWriter
            long bufferedLength = _httpWriter.GetBufferedLength(); 

            //
            // Set headers and status
            // 
            if (!_headersWritten)
            { 
                // 
                // Set status
                // 
                // VSWhidbey 270635: We need to reset the status code for mobile devices.
                if (UseAdaptiveError) {

                    // VSWhidbey 288054: We should change the status code for cases 
                    // that cannot be handled by mobile devices
                    // 4xx for Client Error and 5xx for Server Error in HTTP spec 
                    int statusCode = StatusCode; 
                    if (statusCode >= 400 && statusCode < 600) {
                        this.StatusCode = 200; 
                    }
                }

                if (_statusSet) { 
                    _wr.SendStatus(this.StatusCode, this.SubStatusCode, this.StatusDescription);
                    _statusSet = false; 
                } 

                // 
                //  Set headers
                //
                if (!_suppressHeaders && !_clientDisconnected)
                { 
                    // If redirect location set, write it through to IIS as a header
                    if (_redirectLocation != null && _redirectLocationSet) { 
                        HttpHeaderCollection headers = Headers as HttpHeaderCollection; 
                        headers.Set("Location", _redirectLocation);
                        _redirectLocationSet = false; 
                    }

                    //
                    // If content-type explicitly set, write it through to IIS as a header 
                    // othervise we won't generate it
                    // We no longer suppress content type for empty responses.  If its 
                    // explicitly set, we generate it. 
                    if (_contentType != null && _contentTypeSet) {
                        HttpHeaderCollection headers = Headers as HttpHeaderCollection; 
                        String contentType = AppendCharSetToContentType(_contentType);
                        headers.Set("Content-Type", contentType);
                        _contentTypeSet = false;
                    } 

                    // 
                    // If cookies have been added/changed, set the corresponding headers 
                    //
                    GenerateResponseHeadersForCookies(); 

                    // Not calling WriteHeaders headers in Integrated mode.
                    // Instead, most headers are generated when the handler runs,
                    // or on demand as necessary. 
                    // The only exception are the cache policy headers.
                    if (sendHeaders) { 
 
                        if (_cachePolicy != null) {
                            // if there are any cookies, do not kernel cache the response 
                            if (_cookies != null && _cookies.Count != 0) {
                                _cachePolicy.SetHasSetCookieHeader();
                                DisableKernelCache();
                            } 

                            if (_cachePolicy.IsModified()) { 
                                ArrayList cacheHeaders = new ArrayList(); 
                                _cachePolicy.GetHeaders(cacheHeaders, this);
                                HttpHeaderCollection headers = Headers as HttpHeaderCollection; 
                                foreach (HttpResponseHeader header in cacheHeaders) {
                                    // set and override the header
                                    headers.Set(header.Name, header.Value);
                                } 
                            }
                        } 
 
                        needPush = true;
                    } 
                }
            }

            if (_flushing && !_filteringCompleted) { 
                _httpWriter.FilterIntegrated(false, iis7WorkerRequest);
                bufferedLength = _httpWriter.GetBufferedLength(); 
            } 

            if (!_clientDisconnected && (bufferedLength > 0 || needPush)) { 

                if (bufferedLength == 0 ) {
                    if (_httpWriter.IgnoringFurtherWrites) {
                        return; 
                    }
                } 
 
                // push HttpWriter buffers to worker request
                _httpWriter.Send(_wr); 
                // push buffers through into native
                iis7WorkerRequest.PushResponseToNative();
                // dispose them (since they're copied or
                // owned by native request) 
                _httpWriter.DisposeIntegratedBuffers();
            } 
        } 

        private void ClearNativeResponse(bool clearEntity, bool clearHeaders, IIS7WorkerRequest wr) { 
            wr.ClearResponse(clearEntity, clearHeaders);
            if (clearEntity) {
                _httpWriter.ClearSubstitutionBlocks();
            } 
        }
 
    } 

    internal enum CacheDependencyType { 
        Files,
        CacheItems,
        VirtualPaths
    } 

    struct ResponseDependencyList { 
        private ArrayList   _dependencies; 
        private string[]    _dependencyArray;
        private DateTime    _oldestDependency; 
        private string      _requestVirtualPath;

        internal void AddDependency(string item, string argname) {
            if (item == null) { 
                throw new ArgumentNullException(argname);
            } 
 
            _dependencyArray = null;
 
            if (_dependencies == null) {
                _dependencies = new ArrayList(1);
            }
 
            DateTime utcNow = DateTime.UtcNow;
 
            _dependencies.Add(new ResponseDependencyInfo( 
                    new string[] {item}, utcNow));
 
            // _oldestDependency is initialized to MinValue and indicates that it must always be set
            if (_oldestDependency == DateTime.MinValue || utcNow < _oldestDependency)
                _oldestDependency = utcNow;
        } 

        internal void AddDependencies(ArrayList items, string argname) { 
            if (items == null) { 
                throw new ArgumentNullException(argname);
            } 

            string[] a = (string[]) items.ToArray(typeof(string));
            AddDependencies(a, argname, false);
        } 

        internal void AddDependencies(string[] items, string argname) { 
            AddDependencies(items, argname, true); 
        }
 
        internal void AddDependencies(string[] items, string argname, bool cloneArray) {
            AddDependencies(items, argname, cloneArray, DateTime.UtcNow);
        }
 
        internal void AddDependencies(string[] items, string argname, bool cloneArray, string requestVirtualPath) {
            if (requestVirtualPath == null) 
                throw new ArgumentNullException("requestVirtualPath"); 

            _requestVirtualPath = requestVirtualPath; 
            AddDependencies(items, argname, cloneArray, DateTime.UtcNow);
        }

        internal void AddDependencies(string[] items, string argname, bool cloneArray, DateTime utcDepTime) { 
            if (items == null) {
                throw new ArgumentNullException(argname); 
            } 

            string [] itemsLocal; 

            if (cloneArray) {
                itemsLocal = (string[]) items.Clone();
            } 
            else {
                itemsLocal = items; 
            } 

            foreach (string item in itemsLocal) { 
                if (String.IsNullOrEmpty(item)) {
                    throw new ArgumentNullException(argname);
                }
            } 

            _dependencyArray = null; 
 
            if (_dependencies == null) {
                _dependencies = new ArrayList(1); 
            }

            _dependencies.Add(new ResponseDependencyInfo(itemsLocal, utcDepTime));
 
            // _oldestDependency is initialized to MinValue and indicates that it must always be set
            if (_oldestDependency == DateTime.MinValue || utcDepTime < _oldestDependency) 
                _oldestDependency = utcDepTime; 
        }
 
        internal bool HasDependencies() {
            if (_dependencyArray == null && _dependencies == null)
                return false;
 
            return true;
        } 
 
        internal string[] GetDependencies() {
            if (_dependencyArray == null && _dependencies != null) { 
                int size = 0;
                foreach (ResponseDependencyInfo info in _dependencies) {
                    size += info.items.Length;
                } 

                _dependencyArray = new string[size]; 
 
                int index = 0;
                foreach (ResponseDependencyInfo info in _dependencies) { 
                    int length = info.items.Length;
                    Array.Copy(info.items, 0, _dependencyArray, index, length);
                    index += length;
                } 
            }
 
            return _dependencyArray; 
        }
 
        internal CacheDependency CreateCacheDependency(CacheDependencyType dependencyType, CacheDependency dependency) {
            if (_dependencies != null) {
                if (dependencyType == CacheDependencyType.Files
                    || dependencyType == CacheDependencyType.CacheItems) { 
                    foreach (ResponseDependencyInfo info in _dependencies) {
                        CacheDependency dependencyOld = dependency; 
                        try { 
                            if (dependencyType == CacheDependencyType.Files) {
                                dependency = new CacheDependency(0, info.items, null, dependencyOld, info.utcDate); 
                            }
                            else {
                                // We create a "public" CacheDepdency here, since the keys are for public items.
                                dependency = new CacheDependency(null, info.items, dependencyOld, 
                                                                 DateTimeUtil.ConvertToLocalTime(info.utcDate));
                            } 
                        } 
                        finally {
                            if (dependencyOld != null) { 
                                dependencyOld.Dispose();
                            }
                        }
                    } 
                }
                else { 
                    CacheDependency virtualDependency = null; 
                    VirtualPathProvider vpp = HostingEnvironment.VirtualPathProvider;
                    if (vpp != null && _requestVirtualPath != null) { 
                        virtualDependency = vpp.GetCacheDependency(_requestVirtualPath, GetDependencies(), _oldestDependency);
                    }
                    if (virtualDependency != null) {
                        AggregateCacheDependency tempDep = new AggregateCacheDependency(); 
                        tempDep.Add(virtualDependency);
                        if (dependency != null) { 
                            tempDep.Add(dependency); 
                        }
                        dependency = tempDep; 
                    }
                }
            }
 
            return dependency;
        } 
    } 

    internal class ResponseDependencyInfo { 
        internal readonly string[]    items;
        internal readonly DateTime    utcDate;

        internal ResponseDependencyInfo(string[] items, DateTime utcDate) { 
            this.items = items;
            this.utcDate = utcDate; 
        } 
    }
} 

