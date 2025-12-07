//------------------------------------------------------------------------------ 
// <copyright file="TrustSection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
    using System; 
    using System.Xml;
    using System.Configuration; 
    using System.Collections.Specialized;
    using System.Collections;
    using System.IO;
    using System.Text; 
    using System.Security.Permissions;
 
    /***************************************************************************** 
     From machine.config
    <!--  level="[Full|High|Medium|Low|Minimal]" --> 
        <trust level="Full" originUrl="" />
     [SectionComment("<!--  level=\"[Full|High|Medium|Low|Minimal]\" -->")]
    ******************************************************************************/
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class TrustSection : ConfigurationSection { 
        private static ConfigurationPropertyCollection _properties; 

        private static readonly ConfigurationProperty _propLevel = 
            new ConfigurationProperty("level",
                                        typeof(string),
                                        "Full",
                                        null, 
                                        StdValidatorsAndConverters.NonEmptyStringValidator,
                                        ConfigurationPropertyOptions.IsRequired); 
 
        private static readonly ConfigurationProperty _propOriginUrl =
            new ConfigurationProperty("originUrl", 
                                        typeof(string),
                                        String.Empty,
                                        ConfigurationPropertyOptions.None);
 
        private static readonly ConfigurationProperty _propProcessRequestInApplicationTrust =
            new ConfigurationProperty("processRequestInApplicationTrust", 
                                        typeof(bool), 
                                        true,
                                        ConfigurationPropertyOptions.None); 

        static TrustSection() {
            // Property initialization
            _properties = new ConfigurationPropertyCollection(); 
            _properties.Add(_propLevel);
            _properties.Add(_propOriginUrl); 
            _properties.Add(_propProcessRequestInApplicationTrust); 
        }
 
        public TrustSection() {
        }

        protected override ConfigurationPropertyCollection Properties { 
            get {
                return _properties; 
            } 
        }
 
        [ConfigurationProperty("level", IsRequired = true, DefaultValue = "Full")]
        [StringValidator(MinLength = 1)]
        public string Level {
            get { 
                return (string)base[_propLevel];
            } 
            set { 
                base[_propLevel] = value;
            } 
        }

        [ConfigurationProperty("originUrl", DefaultValue = "")]
        public string OriginUrl { 
            get {
                return (string)base[_propOriginUrl]; 
            } 
            set {
                base[_propOriginUrl] = value; 
            }
        }

        [ConfigurationProperty("processRequestInApplicationTrust", DefaultValue = true)] 
        public bool ProcessRequestInApplicationTrust {
            get { 
                return (bool)base[_propProcessRequestInApplicationTrust]; 
            }
            set { 
                base[_propProcessRequestInApplicationTrust] = value;
            }
        }
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="TrustSection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Configuration { 
    using System; 
    using System.Xml;
    using System.Configuration; 
    using System.Collections.Specialized;
    using System.Collections;
    using System.IO;
    using System.Text; 
    using System.Security.Permissions;
 
    /***************************************************************************** 
     From machine.config
    <!--  level="[Full|High|Medium|Low|Minimal]" --> 
        <trust level="Full" originUrl="" />
     [SectionComment("<!--  level=\"[Full|High|Medium|Low|Minimal]\" -->")]
    ******************************************************************************/
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class TrustSection : ConfigurationSection { 
        private static ConfigurationPropertyCollection _properties; 

        private static readonly ConfigurationProperty _propLevel = 
            new ConfigurationProperty("level",
                                        typeof(string),
                                        "Full",
                                        null, 
                                        StdValidatorsAndConverters.NonEmptyStringValidator,
                                        ConfigurationPropertyOptions.IsRequired); 
 
        private static readonly ConfigurationProperty _propOriginUrl =
            new ConfigurationProperty("originUrl", 
                                        typeof(string),
                                        String.Empty,
                                        ConfigurationPropertyOptions.None);
 
        private static readonly ConfigurationProperty _propProcessRequestInApplicationTrust =
            new ConfigurationProperty("processRequestInApplicationTrust", 
                                        typeof(bool), 
                                        true,
                                        ConfigurationPropertyOptions.None); 

        static TrustSection() {
            // Property initialization
            _properties = new ConfigurationPropertyCollection(); 
            _properties.Add(_propLevel);
            _properties.Add(_propOriginUrl); 
            _properties.Add(_propProcessRequestInApplicationTrust); 
        }
 
        public TrustSection() {
        }

        protected override ConfigurationPropertyCollection Properties { 
            get {
                return _properties; 
            } 
        }
 
        [ConfigurationProperty("level", IsRequired = true, DefaultValue = "Full")]
        [StringValidator(MinLength = 1)]
        public string Level {
            get { 
                return (string)base[_propLevel];
            } 
            set { 
                base[_propLevel] = value;
            } 
        }

        [ConfigurationProperty("originUrl", DefaultValue = "")]
        public string OriginUrl { 
            get {
                return (string)base[_propOriginUrl]; 
            } 
            set {
                base[_propOriginUrl] = value; 
            }
        }

        [ConfigurationProperty("processRequestInApplicationTrust", DefaultValue = true)] 
        public bool ProcessRequestInApplicationTrust {
            get { 
                return (bool)base[_propProcessRequestInApplicationTrust]; 
            }
            set { 
                base[_propProcessRequestInApplicationTrust] = value;
            }
        }
    } 
}
