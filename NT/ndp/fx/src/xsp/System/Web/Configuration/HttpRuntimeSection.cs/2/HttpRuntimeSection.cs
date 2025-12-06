//------------------------------------------------------------------------------ 
// <copyright file="HttpRuntimeSection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
    using System; 
    using System.Xml;
    using System.Configuration; 
    using System.Collections.Specialized;
    using System.Collections;
    using System.Globalization;
    using System.IO; 
    using System.Text;
    using System.Web.Util; 
    using System.ComponentModel; 
    using System.Security.Permissions;
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class HttpRuntimeSection : ConfigurationSection {
#if !FEATURE_PAL // FEATURE_PAL-specific timeout values
        internal const int DefaultExecutionTimeout = 110; 
#else // !FEATURE_PAL
        // The timeout needs to be extended, since Coriolis/Rotor is much slower 
        // especially platforms like Solaris wan't be able to process complicated 
        // requests if in debug mode.
        // Remove or change this once the real timeout is known 
#if DEBUG
        internal const int DefaultExecutionTimeout = 110 * 10;
#else // DEBUG
        internal const int DefaultExecutionTimeout = 110 * 5; 
#endif // DEBUG
 
#endif // !FEATURE_PAL 
        internal const int DefaultMaxRequestLength = 4096 * 1024;  // 4MB
        internal const int DefaultRequestLengthDiskThreshold = 80 * 1024; // 80KB 
        internal const int DefaultMinFreeThreads = 8;
        internal const int DefaultMinLocalRequestFreeThreads = 4;
        internal const int DefaultAppRequestQueueLimit = 100;
        internal const int DefaultShutdownTimeout = 90; 
        internal const int DefaultDelayNotificationTimeout = 5;
        internal const int DefaultWaitChangeNotification = 0; 
        internal const int DefaultMaxWaitChangeNotification = 0; 
        internal const bool DefaultEnableKernelOutputCache = true;
        internal const bool DefaultRequireRootedSaveAsPath = true; 
        internal const bool DefaultSendCacheControlHeader = true;

        private bool enableVersionHeaderCache = true;
        private bool enableVersionHeaderCached = false; 
        private TimeSpan executionTimeoutCache;
        private bool executionTimeoutCached = false; 
 
        private bool sendCacheControlHeaderCached = false;
        private bool sendCacheControlHeaderCache; 

        private static ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propExecutionTimeout =
            new ConfigurationProperty("executionTimeout", 
                                        typeof(TimeSpan),
                                        TimeSpan.FromSeconds((double)DefaultExecutionTimeout), 
                                        StdValidatorsAndConverters.TimeSpanSecondsConverter, 
                                        StdValidatorsAndConverters.PositiveTimeSpanValidator,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propMaxRequestLength =
            new ConfigurationProperty("maxRequestLength",
                                        typeof(int),
                                        4096, 
                                        null,
                                        new IntegerValidator(0, 2097151), // Max from VS 330766 
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propRequestLengthDiskThreshold =
            new ConfigurationProperty("requestLengthDiskThreshold", 
                                        typeof(int),
                                        80,
                                        null,
                                        StdValidatorsAndConverters.NonZeroPositiveIntegerValidator, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propUseFullyQualifiedRedirectUrl = 
            new ConfigurationProperty("useFullyQualifiedRedirectUrl", 
                                        typeof(bool),
                                        false, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propMinFreeThreads =
            new ConfigurationProperty("minFreeThreads",
                                        typeof(int), 
                                        8,
                                        null, 
                                        StdValidatorsAndConverters.PositiveIntegerValidator, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propMinLocalRequestFreeThreads = 
            new ConfigurationProperty("minLocalRequestFreeThreads",
                                        typeof(int),
                                        4,
                                        null, 
                                        StdValidatorsAndConverters.PositiveIntegerValidator,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propAppRequestQueueLimit = 
            new ConfigurationProperty("appRequestQueueLimit",
                                        typeof(int), 
                                        5000,
                                        null,
                                        StdValidatorsAndConverters.NonZeroPositiveIntegerValidator,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propEnableKernelOutputCache =
            new ConfigurationProperty("enableKernelOutputCache", 
                                        typeof(bool), 
                                        true,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propEnableVersionHeader =
            new ConfigurationProperty("enableVersionHeader",
                                        typeof(bool),
                                        true, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propRequireRootedSaveAsPath = 
            new ConfigurationProperty("requireRootedSaveAsPath", 
                                        typeof(bool),
                                        true, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propEnable =
            new ConfigurationProperty("enable",
                                        typeof(bool), 
                                        true,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propShutdownTimeout = 
            new ConfigurationProperty("shutdownTimeout",
                                        typeof(TimeSpan), 
                                        TimeSpan.FromSeconds((double)DefaultShutdownTimeout),
                                        StdValidatorsAndConverters.TimeSpanSecondsConverter,
                                        null,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propDelayNotificationTimeout =
            new ConfigurationProperty("delayNotificationTimeout", 
                                        typeof(TimeSpan), 
                                        TimeSpan.FromSeconds((double)DefaultDelayNotificationTimeout),
                                        StdValidatorsAndConverters.TimeSpanSecondsConverter, 
                                        null,
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propWaitChangeNotification =
            new ConfigurationProperty("waitChangeNotification", 
                                        typeof(int),
                                        0, 
                                        null, 
                                        StdValidatorsAndConverters.PositiveIntegerValidator,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propMaxWaitChangeNotification =
            new ConfigurationProperty("maxWaitChangeNotification",
                                        typeof(int),
                                        0, 
                                        null,
                                        StdValidatorsAndConverters.PositiveIntegerValidator, 
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propEnableHeaderChecking =
            new ConfigurationProperty("enableHeaderChecking", 
                                        typeof(bool),
                                        true,
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propSendCacheControlHeader = 
            new ConfigurationProperty("sendCacheControlHeader",
                                        typeof(bool), 
                                        DefaultSendCacheControlHeader, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propApartmentThreading = 
            new ConfigurationProperty("apartmentThreading",
                                        typeof(bool),
                                        false,
                                        ConfigurationPropertyOptions.None); 

        private int _MaxRequestLengthBytes; 
        private int _RequestLengthDiskThresholdBytes; 
        private static String s_versionHeader = null;
 
                /*         <!--
                httpRuntime Attributes:
                  executionTimeout="[seconds]" - time in seconds before request is automatically timed out
                  maxRequestLength="[KBytes]" - KBytes size of maximum request length to accept 
                  requestLengthDiskThreshold="[KBytes]" - KBytes threshold to use disk for posted content temporary storage
                  useFullyQualifiedRedirectUrl="[true|false]" - Fully qualifiy the URL for client redirects 
                  minFreeThreads="[count]" - minimum number of free thread to allow execution of new requests 
                  minLocalRequestFreeThreads="[count]" - minimum number of free thread to allow execution of new local requests
                  appRequestQueueLimit="[count]" - maximum number of requests queued for the application; the sum of requests in all application queues is bounded from above by the global requestQueueLimit in the processModel section 
                  enableKernelOutputCache="[true|false]" - enable the http.sys cache on IIS6 and higher - default is true
                  enableVersionHeader="[true|false]" - outputs X-AspNet-Version header with each request
                  requireRootedSaveAsPath="[true|false]" - the filename argument to SaveAs methods must be a rooted path
                  enable="[true|false]" - enable processing requests for this application 
                  waitChangeNotification="[seconds]" - time in seconds to wait for another file change notification before restarting the AppDomain
                  maxWaitChangeNotification="[seconds]" - maximum time in seconds to wait from the first file change notification before restarting the AppDomain 
                  enableHeaderChecking="[true|false]" - when true, CRLF pairs in response headers are encoded 
                -->
                <httpRuntime 
                    executionTimeout="110"
                    maxRequestLength="4096"
                    requestLengthDiskThreshold="80"
                    useFullyQualifiedRedirectUrl="false" 
                    minFreeThreads="8"
                    minLocalRequestFreeThreads="4" 
                    appRequestQueueLimit="5000" 
                    enableVersionHeader="true"
                    requireRootedSaveAsPath="true" 
                    enable="true"
        />
 */
 
        static HttpRuntimeSection() {
            // Property initialization 
            _properties = new ConfigurationPropertyCollection(); 
            _properties.Add(_propExecutionTimeout);
            _properties.Add(_propMaxRequestLength); 
            _properties.Add(_propRequestLengthDiskThreshold);
            _properties.Add(_propUseFullyQualifiedRedirectUrl);
            _properties.Add(_propMinFreeThreads);
            _properties.Add(_propMinLocalRequestFreeThreads); 
            _properties.Add(_propAppRequestQueueLimit);
            _properties.Add(_propEnableKernelOutputCache); 
            _properties.Add(_propEnableVersionHeader); 
            _properties.Add(_propRequireRootedSaveAsPath);
            _properties.Add(_propEnable); 

            _properties.Add(_propShutdownTimeout);
            _properties.Add(_propDelayNotificationTimeout);
            _properties.Add(_propWaitChangeNotification); 
            _properties.Add(_propMaxWaitChangeNotification);
 
            _properties.Add(_propEnableHeaderChecking); 
            _properties.Add(_propSendCacheControlHeader);
            _properties.Add(_propApartmentThreading); 
        }

        public HttpRuntimeSection() {
            _MaxRequestLengthBytes = -1; 
            _RequestLengthDiskThresholdBytes = -1;
        } 
 
        protected override ConfigurationPropertyCollection Properties {
            get { 
                return _properties;
            }
        }
 
        [ConfigurationProperty("executionTimeout", DefaultValue = "00:01:50")]
        [TypeConverter(typeof(TimeSpanSecondsConverter))] 
        [TimeSpanValidator(MinValueString="00:00:00", MaxValueString=TimeSpanValidatorAttribute.TimeSpanMaxValue)] 
        public TimeSpan ExecutionTimeout {
            get { 
                if (executionTimeoutCached == false) {
                    executionTimeoutCache = (TimeSpan)base[_propExecutionTimeout];
                    executionTimeoutCached = true;
                } 
                return executionTimeoutCache;
            } 
            set { 
                base[_propExecutionTimeout] = value;
                executionTimeoutCache = value; 
            }


        } 

        [ConfigurationProperty("maxRequestLength", DefaultValue = 4096)] 
        [IntegerValidator(MinValue = 0)] 
        public int MaxRequestLength {
            get { 
                return (int)base[_propMaxRequestLength];
            }
            set {
                if (value < RequestLengthDiskThreshold) { 
                    throw new ConfigurationErrorsException(SR.GetString(
                        SR.Config_max_request_length_smaller_than_max_request_length_disk_threshold), 
                         ElementInformation.Properties[_propMaxRequestLength.Name].Source, 
                         ElementInformation.Properties[_propMaxRequestLength.Name].LineNumber);
                } 
                base[_propMaxRequestLength] = value;
            } //
        }
 
        [ConfigurationProperty("requestLengthDiskThreshold", DefaultValue = 80)]
        [IntegerValidator(MinValue = 1)] 
        public int RequestLengthDiskThreshold { 
            get {
                return (int)base[_propRequestLengthDiskThreshold]; 
            }
            set {
                if (value > MaxRequestLength) {
                    throw new ConfigurationErrorsException(SR.GetString( 
                        SR.Config_max_request_length_disk_threshold_exceeds_max_request_length),
                         ElementInformation.Properties[_propRequestLengthDiskThreshold.Name].Source, 
                         ElementInformation.Properties[_propRequestLengthDiskThreshold.Name].LineNumber); 
                }
                base[_propRequestLengthDiskThreshold] = value; 
            }
        }

        [ConfigurationProperty("useFullyQualifiedRedirectUrl", DefaultValue = false)] 
        public bool UseFullyQualifiedRedirectUrl {
            get { 
                return (bool)base[_propUseFullyQualifiedRedirectUrl]; 
            }
            set { 
                base[_propUseFullyQualifiedRedirectUrl] = value;
            }
        }
 
        [ConfigurationProperty("minFreeThreads", DefaultValue = 8)]
        [IntegerValidator(MinValue = 0)] 
        public int MinFreeThreads { 
            get {
                return (int)base[_propMinFreeThreads]; 
            }
            set {
                base[_propMinFreeThreads] = value;
            } 
        }
 
        [ConfigurationProperty("minLocalRequestFreeThreads", DefaultValue = 4)] 
        [IntegerValidator(MinValue = 0)]
        public int MinLocalRequestFreeThreads { 
            get {
                return (int)base[_propMinLocalRequestFreeThreads];
            }
            set { 
                base[_propMinLocalRequestFreeThreads] = value;
            } 
        } 

        [ConfigurationProperty("appRequestQueueLimit", DefaultValue = 5000)] 
        [IntegerValidator(MinValue = 1)]
        public int AppRequestQueueLimit {
            get {
                return (int)base[_propAppRequestQueueLimit]; 
            }
            set { 
                base[_propAppRequestQueueLimit] = value; 
            }
        } 

        [ConfigurationProperty("enableKernelOutputCache", DefaultValue = true)]
        public bool EnableKernelOutputCache {
            get { 
                return (bool)base[_propEnableKernelOutputCache];
            } 
            set { 
                base[_propEnableKernelOutputCache] = value;
            } 
        }

        [ConfigurationProperty("enableVersionHeader", DefaultValue = true)]
        public bool EnableVersionHeader { 
            get {
                if (enableVersionHeaderCached == false) { 
                    enableVersionHeaderCache = (bool)base[_propEnableVersionHeader]; 
                    enableVersionHeaderCached = true;
                } 
                return enableVersionHeaderCache;
            }
            set {
                base[_propEnableVersionHeader] = value; 
                enableVersionHeaderCache = value;
            } 
        } 

        [ConfigurationProperty("apartmentThreading", DefaultValue = false)] 
        public bool ApartmentThreading {
            get {
                return (bool)base[_propApartmentThreading];
            } 
            set {
                base[_propApartmentThreading] = value; 
            } 
        }
 
        [ConfigurationProperty("requireRootedSaveAsPath", DefaultValue = true)]
        public bool RequireRootedSaveAsPath {
            get {
                return (bool)base[_propRequireRootedSaveAsPath]; 
            }
            set { 
                base[_propRequireRootedSaveAsPath] = value; 
            }
        } 

        [ConfigurationProperty("enable", DefaultValue = true)]
        public bool Enable {
            get { 
                return (bool)base[_propEnable];
            } 
            set { 
                base[_propEnable] = value;
            } 
        }

        [ConfigurationProperty("sendCacheControlHeader", DefaultValue = DefaultSendCacheControlHeader)]
        public bool SendCacheControlHeader { 
            get {
                if (sendCacheControlHeaderCached == false) { 
                    sendCacheControlHeaderCache = (bool)base[_propSendCacheControlHeader]; 
                    sendCacheControlHeaderCached = true;
                } 
                return sendCacheControlHeaderCache;
            }
            set {
                base[_propSendCacheControlHeader] = value; 
                sendCacheControlHeaderCache = value;
            } 
        } 

        [ConfigurationProperty("shutdownTimeout", DefaultValue = "00:01:30")] 
        [TypeConverter(typeof(TimeSpanSecondsConverter))]
        public TimeSpan ShutdownTimeout {
            get {
                return (TimeSpan)base[_propShutdownTimeout]; 
            }
            set { 
                base[_propShutdownTimeout] = value; 
            }
        } 

        [ConfigurationProperty("delayNotificationTimeout", DefaultValue = "00:00:05")]
        [TypeConverter(typeof(TimeSpanSecondsConverter))]
        public TimeSpan DelayNotificationTimeout { 
            get {
                return (TimeSpan)base[_propDelayNotificationTimeout]; 
            } 
            set {
                base[_propDelayNotificationTimeout] = value; 
            }
        }

        [ConfigurationProperty("waitChangeNotification", DefaultValue = 0)] 
        [IntegerValidator(MinValue = 0)]
        public int WaitChangeNotification { 
            get { 
                return (int)base[_propWaitChangeNotification];
            } 
            set {
                base[_propWaitChangeNotification] = value;
            }
        } 

        [ConfigurationProperty("maxWaitChangeNotification", DefaultValue = 0)] 
        [IntegerValidator(MinValue = 0)] 
        public int MaxWaitChangeNotification {
            get { 
                return (int)base[_propMaxWaitChangeNotification];
            }
            set {
                base[_propMaxWaitChangeNotification] = value; 
            }
        } 
 
        [ConfigurationProperty("enableHeaderChecking", DefaultValue = true)]
        public bool EnableHeaderChecking { 
            get {
                return (bool)base[_propEnableHeaderChecking];
            }
            set { 
                base[_propEnableHeaderChecking] = value;
            } 
        } 

        private int BytesFromKilobytes(int kilobytes) { 
            long maxLength = kilobytes * 1024L;
            return ((maxLength < Int32.MaxValue) ? (int)maxLength : Int32.MaxValue);
        }
 
        internal int MaxRequestLengthBytes {
            get { 
                if (_MaxRequestLengthBytes < 0) { 
                    _MaxRequestLengthBytes = BytesFromKilobytes(MaxRequestLength);
                } 
                return _MaxRequestLengthBytes;
            }
        }
 
        internal int RequestLengthDiskThresholdBytes {
            get { 
                if (_RequestLengthDiskThresholdBytes < 0) { 
                    _RequestLengthDiskThresholdBytes = BytesFromKilobytes(RequestLengthDiskThreshold);
                } 
                return _RequestLengthDiskThresholdBytes;
            }
        }
 
        internal String VersionHeader {
            get { 
                if (!EnableVersionHeader) { 
                    return null;
                } 

                if (s_versionHeader == null) {
                    String header = null;
                    // construct once (race condition here doesn't matter) 
                    try {
                        String version = VersionInfo.SystemWebVersion; 
                        int i = version.LastIndexOf('.'); 
                        if (i > 0) {
                            header = version.Substring(0, i); 
                        }
                    }
                    catch {
                    } 

                    if (header == null) { 
                        header = String.Empty; 
                    }
 
                    s_versionHeader = header;
                }

                return s_versionHeader; 
            }
        } 
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="HttpRuntimeSection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
    using System; 
    using System.Xml;
    using System.Configuration; 
    using System.Collections.Specialized;
    using System.Collections;
    using System.Globalization;
    using System.IO; 
    using System.Text;
    using System.Web.Util; 
    using System.ComponentModel; 
    using System.Security.Permissions;
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class HttpRuntimeSection : ConfigurationSection {
#if !FEATURE_PAL // FEATURE_PAL-specific timeout values
        internal const int DefaultExecutionTimeout = 110; 
#else // !FEATURE_PAL
        // The timeout needs to be extended, since Coriolis/Rotor is much slower 
        // especially platforms like Solaris wan't be able to process complicated 
        // requests if in debug mode.
        // Remove or change this once the real timeout is known 
#if DEBUG
        internal const int DefaultExecutionTimeout = 110 * 10;
#else // DEBUG
        internal const int DefaultExecutionTimeout = 110 * 5; 
#endif // DEBUG
 
#endif // !FEATURE_PAL 
        internal const int DefaultMaxRequestLength = 4096 * 1024;  // 4MB
        internal const int DefaultRequestLengthDiskThreshold = 80 * 1024; // 80KB 
        internal const int DefaultMinFreeThreads = 8;
        internal const int DefaultMinLocalRequestFreeThreads = 4;
        internal const int DefaultAppRequestQueueLimit = 100;
        internal const int DefaultShutdownTimeout = 90; 
        internal const int DefaultDelayNotificationTimeout = 5;
        internal const int DefaultWaitChangeNotification = 0; 
        internal const int DefaultMaxWaitChangeNotification = 0; 
        internal const bool DefaultEnableKernelOutputCache = true;
        internal const bool DefaultRequireRootedSaveAsPath = true; 
        internal const bool DefaultSendCacheControlHeader = true;

        private bool enableVersionHeaderCache = true;
        private bool enableVersionHeaderCached = false; 
        private TimeSpan executionTimeoutCache;
        private bool executionTimeoutCached = false; 
 
        private bool sendCacheControlHeaderCached = false;
        private bool sendCacheControlHeaderCache; 

        private static ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propExecutionTimeout =
            new ConfigurationProperty("executionTimeout", 
                                        typeof(TimeSpan),
                                        TimeSpan.FromSeconds((double)DefaultExecutionTimeout), 
                                        StdValidatorsAndConverters.TimeSpanSecondsConverter, 
                                        StdValidatorsAndConverters.PositiveTimeSpanValidator,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propMaxRequestLength =
            new ConfigurationProperty("maxRequestLength",
                                        typeof(int),
                                        4096, 
                                        null,
                                        new IntegerValidator(0, 2097151), // Max from VS 330766 
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propRequestLengthDiskThreshold =
            new ConfigurationProperty("requestLengthDiskThreshold", 
                                        typeof(int),
                                        80,
                                        null,
                                        StdValidatorsAndConverters.NonZeroPositiveIntegerValidator, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propUseFullyQualifiedRedirectUrl = 
            new ConfigurationProperty("useFullyQualifiedRedirectUrl", 
                                        typeof(bool),
                                        false, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propMinFreeThreads =
            new ConfigurationProperty("minFreeThreads",
                                        typeof(int), 
                                        8,
                                        null, 
                                        StdValidatorsAndConverters.PositiveIntegerValidator, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propMinLocalRequestFreeThreads = 
            new ConfigurationProperty("minLocalRequestFreeThreads",
                                        typeof(int),
                                        4,
                                        null, 
                                        StdValidatorsAndConverters.PositiveIntegerValidator,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propAppRequestQueueLimit = 
            new ConfigurationProperty("appRequestQueueLimit",
                                        typeof(int), 
                                        5000,
                                        null,
                                        StdValidatorsAndConverters.NonZeroPositiveIntegerValidator,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propEnableKernelOutputCache =
            new ConfigurationProperty("enableKernelOutputCache", 
                                        typeof(bool), 
                                        true,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propEnableVersionHeader =
            new ConfigurationProperty("enableVersionHeader",
                                        typeof(bool),
                                        true, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propRequireRootedSaveAsPath = 
            new ConfigurationProperty("requireRootedSaveAsPath", 
                                        typeof(bool),
                                        true, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propEnable =
            new ConfigurationProperty("enable",
                                        typeof(bool), 
                                        true,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propShutdownTimeout = 
            new ConfigurationProperty("shutdownTimeout",
                                        typeof(TimeSpan), 
                                        TimeSpan.FromSeconds((double)DefaultShutdownTimeout),
                                        StdValidatorsAndConverters.TimeSpanSecondsConverter,
                                        null,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propDelayNotificationTimeout =
            new ConfigurationProperty("delayNotificationTimeout", 
                                        typeof(TimeSpan), 
                                        TimeSpan.FromSeconds((double)DefaultDelayNotificationTimeout),
                                        StdValidatorsAndConverters.TimeSpanSecondsConverter, 
                                        null,
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propWaitChangeNotification =
            new ConfigurationProperty("waitChangeNotification", 
                                        typeof(int),
                                        0, 
                                        null, 
                                        StdValidatorsAndConverters.PositiveIntegerValidator,
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propMaxWaitChangeNotification =
            new ConfigurationProperty("maxWaitChangeNotification",
                                        typeof(int),
                                        0, 
                                        null,
                                        StdValidatorsAndConverters.PositiveIntegerValidator, 
                                        ConfigurationPropertyOptions.None); 
        private static readonly ConfigurationProperty _propEnableHeaderChecking =
            new ConfigurationProperty("enableHeaderChecking", 
                                        typeof(bool),
                                        true,
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propSendCacheControlHeader = 
            new ConfigurationProperty("sendCacheControlHeader",
                                        typeof(bool), 
                                        DefaultSendCacheControlHeader, 
                                        ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propApartmentThreading = 
            new ConfigurationProperty("apartmentThreading",
                                        typeof(bool),
                                        false,
                                        ConfigurationPropertyOptions.None); 

        private int _MaxRequestLengthBytes; 
        private int _RequestLengthDiskThresholdBytes; 
        private static String s_versionHeader = null;
 
                /*         <!--
                httpRuntime Attributes:
                  executionTimeout="[seconds]" - time in seconds before request is automatically timed out
                  maxRequestLength="[KBytes]" - KBytes size of maximum request length to accept 
                  requestLengthDiskThreshold="[KBytes]" - KBytes threshold to use disk for posted content temporary storage
                  useFullyQualifiedRedirectUrl="[true|false]" - Fully qualifiy the URL for client redirects 
                  minFreeThreads="[count]" - minimum number of free thread to allow execution of new requests 
                  minLocalRequestFreeThreads="[count]" - minimum number of free thread to allow execution of new local requests
                  appRequestQueueLimit="[count]" - maximum number of requests queued for the application; the sum of requests in all application queues is bounded from above by the global requestQueueLimit in the processModel section 
                  enableKernelOutputCache="[true|false]" - enable the http.sys cache on IIS6 and higher - default is true
                  enableVersionHeader="[true|false]" - outputs X-AspNet-Version header with each request
                  requireRootedSaveAsPath="[true|false]" - the filename argument to SaveAs methods must be a rooted path
                  enable="[true|false]" - enable processing requests for this application 
                  waitChangeNotification="[seconds]" - time in seconds to wait for another file change notification before restarting the AppDomain
                  maxWaitChangeNotification="[seconds]" - maximum time in seconds to wait from the first file change notification before restarting the AppDomain 
                  enableHeaderChecking="[true|false]" - when true, CRLF pairs in response headers are encoded 
                -->
                <httpRuntime 
                    executionTimeout="110"
                    maxRequestLength="4096"
                    requestLengthDiskThreshold="80"
                    useFullyQualifiedRedirectUrl="false" 
                    minFreeThreads="8"
                    minLocalRequestFreeThreads="4" 
                    appRequestQueueLimit="5000" 
                    enableVersionHeader="true"
                    requireRootedSaveAsPath="true" 
                    enable="true"
        />
 */
 
        static HttpRuntimeSection() {
            // Property initialization 
            _properties = new ConfigurationPropertyCollection(); 
            _properties.Add(_propExecutionTimeout);
            _properties.Add(_propMaxRequestLength); 
            _properties.Add(_propRequestLengthDiskThreshold);
            _properties.Add(_propUseFullyQualifiedRedirectUrl);
            _properties.Add(_propMinFreeThreads);
            _properties.Add(_propMinLocalRequestFreeThreads); 
            _properties.Add(_propAppRequestQueueLimit);
            _properties.Add(_propEnableKernelOutputCache); 
            _properties.Add(_propEnableVersionHeader); 
            _properties.Add(_propRequireRootedSaveAsPath);
            _properties.Add(_propEnable); 

            _properties.Add(_propShutdownTimeout);
            _properties.Add(_propDelayNotificationTimeout);
            _properties.Add(_propWaitChangeNotification); 
            _properties.Add(_propMaxWaitChangeNotification);
 
            _properties.Add(_propEnableHeaderChecking); 
            _properties.Add(_propSendCacheControlHeader);
            _properties.Add(_propApartmentThreading); 
        }

        public HttpRuntimeSection() {
            _MaxRequestLengthBytes = -1; 
            _RequestLengthDiskThresholdBytes = -1;
        } 
 
        protected override ConfigurationPropertyCollection Properties {
            get { 
                return _properties;
            }
        }
 
        [ConfigurationProperty("executionTimeout", DefaultValue = "00:01:50")]
        [TypeConverter(typeof(TimeSpanSecondsConverter))] 
        [TimeSpanValidator(MinValueString="00:00:00", MaxValueString=TimeSpanValidatorAttribute.TimeSpanMaxValue)] 
        public TimeSpan ExecutionTimeout {
            get { 
                if (executionTimeoutCached == false) {
                    executionTimeoutCache = (TimeSpan)base[_propExecutionTimeout];
                    executionTimeoutCached = true;
                } 
                return executionTimeoutCache;
            } 
            set { 
                base[_propExecutionTimeout] = value;
                executionTimeoutCache = value; 
            }


        } 

        [ConfigurationProperty("maxRequestLength", DefaultValue = 4096)] 
        [IntegerValidator(MinValue = 0)] 
        public int MaxRequestLength {
            get { 
                return (int)base[_propMaxRequestLength];
            }
            set {
                if (value < RequestLengthDiskThreshold) { 
                    throw new ConfigurationErrorsException(SR.GetString(
                        SR.Config_max_request_length_smaller_than_max_request_length_disk_threshold), 
                         ElementInformation.Properties[_propMaxRequestLength.Name].Source, 
                         ElementInformation.Properties[_propMaxRequestLength.Name].LineNumber);
                } 
                base[_propMaxRequestLength] = value;
            } //
        }
 
        [ConfigurationProperty("requestLengthDiskThreshold", DefaultValue = 80)]
        [IntegerValidator(MinValue = 1)] 
        public int RequestLengthDiskThreshold { 
            get {
                return (int)base[_propRequestLengthDiskThreshold]; 
            }
            set {
                if (value > MaxRequestLength) {
                    throw new ConfigurationErrorsException(SR.GetString( 
                        SR.Config_max_request_length_disk_threshold_exceeds_max_request_length),
                         ElementInformation.Properties[_propRequestLengthDiskThreshold.Name].Source, 
                         ElementInformation.Properties[_propRequestLengthDiskThreshold.Name].LineNumber); 
                }
                base[_propRequestLengthDiskThreshold] = value; 
            }
        }

        [ConfigurationProperty("useFullyQualifiedRedirectUrl", DefaultValue = false)] 
        public bool UseFullyQualifiedRedirectUrl {
            get { 
                return (bool)base[_propUseFullyQualifiedRedirectUrl]; 
            }
            set { 
                base[_propUseFullyQualifiedRedirectUrl] = value;
            }
        }
 
        [ConfigurationProperty("minFreeThreads", DefaultValue = 8)]
        [IntegerValidator(MinValue = 0)] 
        public int MinFreeThreads { 
            get {
                return (int)base[_propMinFreeThreads]; 
            }
            set {
                base[_propMinFreeThreads] = value;
            } 
        }
 
        [ConfigurationProperty("minLocalRequestFreeThreads", DefaultValue = 4)] 
        [IntegerValidator(MinValue = 0)]
        public int MinLocalRequestFreeThreads { 
            get {
                return (int)base[_propMinLocalRequestFreeThreads];
            }
            set { 
                base[_propMinLocalRequestFreeThreads] = value;
            } 
        } 

        [ConfigurationProperty("appRequestQueueLimit", DefaultValue = 5000)] 
        [IntegerValidator(MinValue = 1)]
        public int AppRequestQueueLimit {
            get {
                return (int)base[_propAppRequestQueueLimit]; 
            }
            set { 
                base[_propAppRequestQueueLimit] = value; 
            }
        } 

        [ConfigurationProperty("enableKernelOutputCache", DefaultValue = true)]
        public bool EnableKernelOutputCache {
            get { 
                return (bool)base[_propEnableKernelOutputCache];
            } 
            set { 
                base[_propEnableKernelOutputCache] = value;
            } 
        }

        [ConfigurationProperty("enableVersionHeader", DefaultValue = true)]
        public bool EnableVersionHeader { 
            get {
                if (enableVersionHeaderCached == false) { 
                    enableVersionHeaderCache = (bool)base[_propEnableVersionHeader]; 
                    enableVersionHeaderCached = true;
                } 
                return enableVersionHeaderCache;
            }
            set {
                base[_propEnableVersionHeader] = value; 
                enableVersionHeaderCache = value;
            } 
        } 

        [ConfigurationProperty("apartmentThreading", DefaultValue = false)] 
        public bool ApartmentThreading {
            get {
                return (bool)base[_propApartmentThreading];
            } 
            set {
                base[_propApartmentThreading] = value; 
            } 
        }
 
        [ConfigurationProperty("requireRootedSaveAsPath", DefaultValue = true)]
        public bool RequireRootedSaveAsPath {
            get {
                return (bool)base[_propRequireRootedSaveAsPath]; 
            }
            set { 
                base[_propRequireRootedSaveAsPath] = value; 
            }
        } 

        [ConfigurationProperty("enable", DefaultValue = true)]
        public bool Enable {
            get { 
                return (bool)base[_propEnable];
            } 
            set { 
                base[_propEnable] = value;
            } 
        }

        [ConfigurationProperty("sendCacheControlHeader", DefaultValue = DefaultSendCacheControlHeader)]
        public bool SendCacheControlHeader { 
            get {
                if (sendCacheControlHeaderCached == false) { 
                    sendCacheControlHeaderCache = (bool)base[_propSendCacheControlHeader]; 
                    sendCacheControlHeaderCached = true;
                } 
                return sendCacheControlHeaderCache;
            }
            set {
                base[_propSendCacheControlHeader] = value; 
                sendCacheControlHeaderCache = value;
            } 
        } 

        [ConfigurationProperty("shutdownTimeout", DefaultValue = "00:01:30")] 
        [TypeConverter(typeof(TimeSpanSecondsConverter))]
        public TimeSpan ShutdownTimeout {
            get {
                return (TimeSpan)base[_propShutdownTimeout]; 
            }
            set { 
                base[_propShutdownTimeout] = value; 
            }
        } 

        [ConfigurationProperty("delayNotificationTimeout", DefaultValue = "00:00:05")]
        [TypeConverter(typeof(TimeSpanSecondsConverter))]
        public TimeSpan DelayNotificationTimeout { 
            get {
                return (TimeSpan)base[_propDelayNotificationTimeout]; 
            } 
            set {
                base[_propDelayNotificationTimeout] = value; 
            }
        }

        [ConfigurationProperty("waitChangeNotification", DefaultValue = 0)] 
        [IntegerValidator(MinValue = 0)]
        public int WaitChangeNotification { 
            get { 
                return (int)base[_propWaitChangeNotification];
            } 
            set {
                base[_propWaitChangeNotification] = value;
            }
        } 

        [ConfigurationProperty("maxWaitChangeNotification", DefaultValue = 0)] 
        [IntegerValidator(MinValue = 0)] 
        public int MaxWaitChangeNotification {
            get { 
                return (int)base[_propMaxWaitChangeNotification];
            }
            set {
                base[_propMaxWaitChangeNotification] = value; 
            }
        } 
 
        [ConfigurationProperty("enableHeaderChecking", DefaultValue = true)]
        public bool EnableHeaderChecking { 
            get {
                return (bool)base[_propEnableHeaderChecking];
            }
            set { 
                base[_propEnableHeaderChecking] = value;
            } 
        } 

        private int BytesFromKilobytes(int kilobytes) { 
            long maxLength = kilobytes * 1024L;
            return ((maxLength < Int32.MaxValue) ? (int)maxLength : Int32.MaxValue);
        }
 
        internal int MaxRequestLengthBytes {
            get { 
                if (_MaxRequestLengthBytes < 0) { 
                    _MaxRequestLengthBytes = BytesFromKilobytes(MaxRequestLength);
                } 
                return _MaxRequestLengthBytes;
            }
        }
 
        internal int RequestLengthDiskThresholdBytes {
            get { 
                if (_RequestLengthDiskThresholdBytes < 0) { 
                    _RequestLengthDiskThresholdBytes = BytesFromKilobytes(RequestLengthDiskThreshold);
                } 
                return _RequestLengthDiskThresholdBytes;
            }
        }
 
        internal String VersionHeader {
            get { 
                if (!EnableVersionHeader) { 
                    return null;
                } 

                if (s_versionHeader == null) {
                    String header = null;
                    // construct once (race condition here doesn't matter) 
                    try {
                        String version = VersionInfo.SystemWebVersion; 
                        int i = version.LastIndexOf('.'); 
                        if (i > 0) {
                            header = version.Substring(0, i); 
                        }
                    }
                    catch {
                    } 

                    if (header == null) { 
                        header = String.Empty; 
                    }
 
                    s_versionHeader = header;
                }

                return s_versionHeader; 
            }
        } 
    } 
}
