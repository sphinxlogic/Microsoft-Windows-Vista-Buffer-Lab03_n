//------------------------------------------------------------------------------ 
// <copyright file="BuildProvider.cs" company="Microsoft">
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
    using System.Web.Compilation; 
    using System.Reflection; 
    using System.Web.Hosting;
    using System.Web.UI; 
    using System.CodeDom.Compiler;
    using System.Web.Util;
    using System.ComponentModel;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class BuildProvider : ConfigurationElement { 
        private static ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propExtension = 
            new ConfigurationProperty("extension",
                                        typeof(string),
                                        null,
                                        null, 
                                        StdValidatorsAndConverters.NonEmptyStringValidator,
                                        ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey); 
        private static readonly ConfigurationProperty _propType = 
            new ConfigurationProperty("type",
                                        typeof(string), 
                                        null,
                                        null,
                                        StdValidatorsAndConverters.NonEmptyStringValidator,
                                        ConfigurationPropertyOptions.IsRequired); 

        private Type _type; 
 
        // AppliesTo value from the BuildProviderAppliesToAttribute
        private BuildProviderAppliesTo _appliesToInternal; 

        static BuildProvider() {
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propExtension); 
            _properties.Add(_propType);
        } 
 
        public BuildProvider(String extension, String type)
            : this() { 
            Extension = extension;
            Type = type;
        }
        internal BuildProvider() { 
        }
 
        protected override ConfigurationPropertyCollection Properties { 
            get {
                return _properties; 
            }
        }

        // this override is required because AppliesTo may be in any order in the 
        // property string but it still and the default equals operator would consider
        // them different depending on order in the persisted string. 
        public override bool Equals(object provider) { 
            BuildProvider o = provider as BuildProvider;
            return (o != null && StringUtil.EqualsIgnoreCase(Extension, o.Extension) && Type == o.Type); 
        }
        public override int GetHashCode() {
            return HashCodeCombiner.CombineHashCodes(Extension.ToLower(CultureInfo.InvariantCulture).GetHashCode(),
                                                     Type.GetHashCode()); 
        }
 
 
        [ConfigurationProperty("extension", IsRequired = true, IsKey = true, DefaultValue = "")]
        [StringValidator(MinLength = 1)] 
        public string Extension {
            get {
                return (string)base[_propExtension];
            } 
            set {
                base[_propExtension] = value; 
            } 
        }
 
        [ConfigurationProperty("type", IsRequired = true, DefaultValue = "")]
        [StringValidator(MinLength = 1)]
        public string Type {
            get { 
                return (string)base[_propType];
            } 
            set { 
                base[_propType] = value;
            } 
        }

        internal Type TypeInternal {
            get { 
                if (_type == null) {
                    lock (this) { 
                        if (_type == null) { 
                            _type = CompilationUtil.LoadTypeWithChecks(Type, typeof(System.Web.Compilation.BuildProvider), null, this, "type");
                        } 
                    }
                }

                return _type; 
            }
        } 
 
        internal BuildProviderAppliesTo AppliesToInternal {
            get { 
                if (_appliesToInternal != 0)
                    return _appliesToInternal;

                // Check whether the control builder's class exposes an AppliesTo attribute 
                object[] attrs = TypeInternal.GetCustomAttributes(
                    typeof(BuildProviderAppliesToAttribute), /*inherit*/ true); 
 
                if ((attrs != null) && (attrs.Length > 0)) {
                    Debug.Assert(attrs[0] is BuildProviderAppliesToAttribute); 
                    _appliesToInternal = ((BuildProviderAppliesToAttribute)attrs[0]).AppliesTo;
                }
                else {
                    // Default to applying to All 
                    _appliesToInternal = BuildProviderAppliesTo.All;
                } 
 
                return _appliesToInternal;
            } 
        }
    }
}
//------------------------------------------------------------------------------ 
// <copyright file="BuildProvider.cs" company="Microsoft">
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
    using System.Web.Compilation; 
    using System.Reflection; 
    using System.Web.Hosting;
    using System.Web.UI; 
    using System.CodeDom.Compiler;
    using System.Web.Util;
    using System.ComponentModel;
    using System.Security.Permissions; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class BuildProvider : ConfigurationElement { 
        private static ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propExtension = 
            new ConfigurationProperty("extension",
                                        typeof(string),
                                        null,
                                        null, 
                                        StdValidatorsAndConverters.NonEmptyStringValidator,
                                        ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey); 
        private static readonly ConfigurationProperty _propType = 
            new ConfigurationProperty("type",
                                        typeof(string), 
                                        null,
                                        null,
                                        StdValidatorsAndConverters.NonEmptyStringValidator,
                                        ConfigurationPropertyOptions.IsRequired); 

        private Type _type; 
 
        // AppliesTo value from the BuildProviderAppliesToAttribute
        private BuildProviderAppliesTo _appliesToInternal; 

        static BuildProvider() {
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propExtension); 
            _properties.Add(_propType);
        } 
 
        public BuildProvider(String extension, String type)
            : this() { 
            Extension = extension;
            Type = type;
        }
        internal BuildProvider() { 
        }
 
        protected override ConfigurationPropertyCollection Properties { 
            get {
                return _properties; 
            }
        }

        // this override is required because AppliesTo may be in any order in the 
        // property string but it still and the default equals operator would consider
        // them different depending on order in the persisted string. 
        public override bool Equals(object provider) { 
            BuildProvider o = provider as BuildProvider;
            return (o != null && StringUtil.EqualsIgnoreCase(Extension, o.Extension) && Type == o.Type); 
        }
        public override int GetHashCode() {
            return HashCodeCombiner.CombineHashCodes(Extension.ToLower(CultureInfo.InvariantCulture).GetHashCode(),
                                                     Type.GetHashCode()); 
        }
 
 
        [ConfigurationProperty("extension", IsRequired = true, IsKey = true, DefaultValue = "")]
        [StringValidator(MinLength = 1)] 
        public string Extension {
            get {
                return (string)base[_propExtension];
            } 
            set {
                base[_propExtension] = value; 
            } 
        }
 
        [ConfigurationProperty("type", IsRequired = true, DefaultValue = "")]
        [StringValidator(MinLength = 1)]
        public string Type {
            get { 
                return (string)base[_propType];
            } 
            set { 
                base[_propType] = value;
            } 
        }

        internal Type TypeInternal {
            get { 
                if (_type == null) {
                    lock (this) { 
                        if (_type == null) { 
                            _type = CompilationUtil.LoadTypeWithChecks(Type, typeof(System.Web.Compilation.BuildProvider), null, this, "type");
                        } 
                    }
                }

                return _type; 
            }
        } 
 
        internal BuildProviderAppliesTo AppliesToInternal {
            get { 
                if (_appliesToInternal != 0)
                    return _appliesToInternal;

                // Check whether the control builder's class exposes an AppliesTo attribute 
                object[] attrs = TypeInternal.GetCustomAttributes(
                    typeof(BuildProviderAppliesToAttribute), /*inherit*/ true); 
 
                if ((attrs != null) && (attrs.Length > 0)) {
                    Debug.Assert(attrs[0] is BuildProviderAppliesToAttribute); 
                    _appliesToInternal = ((BuildProviderAppliesToAttribute)attrs[0]).AppliesTo;
                }
                else {
                    // Default to applying to All 
                    _appliesToInternal = BuildProviderAppliesTo.All;
                } 
 
                return _appliesToInternal;
            } 
        }
    }
}
