//------------------------------------------------------------------------------ 
// <copyright file="UriSection.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

 
namespace System.Configuration 
{
    using System.Threading; 

    /// <summary>
    /// Summary description for UriSection.
    /// </summary> 
    public sealed class UriSection : ConfigurationSection
    { 
        public UriSection(){ 
            this.properties.Add(this.idn);
            this.properties.Add(this.iriParsing); 
        }

        [ConfigurationProperty(CommonConfigurationStrings.Idn)]
        public IdnElement Idn{ 
            get {
                return (IdnElement)this[this.idn]; 
            } 
        }
 
        [ConfigurationProperty(CommonConfigurationStrings.IriParsing)]
        public IriParsingElement IriParsing
        {
            get{ 
                return (IriParsingElement)this[this.iriParsing];
            } 
        } 

        protected override ConfigurationPropertyCollection Properties 
        {
            get{
                return this.properties;
            } 
        }
 
 	 
        ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection();
 
        readonly ConfigurationProperty idn =
            new ConfigurationProperty(CommonConfigurationStrings.Idn, typeof(IdnElement), null,
                    ConfigurationPropertyOptions.None);
 
        readonly ConfigurationProperty iriParsing =
            new ConfigurationProperty(CommonConfigurationStrings.IriParsing, typeof(IriParsingElement), null, 
                    ConfigurationPropertyOptions.None); 
    }
 
    internal sealed class UriSectionInternal
    {
        internal UriSectionInternal(UriSection section)
        { 
            this.idn = section.Idn.Enabled;
            this.iriParsing = section.IriParsing.Enabled; 
        } 

        internal UriIdnScope Idn 
        {
            get { return this.idn; }
        }
 
        internal bool IriParsing
        { 
            get { return this.iriParsing; } 
        }
 
        bool iriParsing;
        UriIdnScope idn;

        internal static object ClassSyncObject 
        {
            get{ 
                if (classSyncObject == null){ 
                    Interlocked.CompareExchange(ref classSyncObject, new object(), null);
                } 
                return classSyncObject;
            }
        }
 
        internal static UriSectionInternal GetSection()
        { 
            lock (ClassSyncObject){ 
                UriSection section = PrivilegedConfigurationManager.GetSection(CommonConfigurationStrings.UriSectionPath) as UriSection;
                if (section == null) 
                    return null;

                return new UriSectionInternal(section);
            } 
        }
 
        private static object classSyncObject; 
    }
} 

//------------------------------------------------------------------------------ 
// <copyright file="UriSection.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

 
namespace System.Configuration 
{
    using System.Threading; 

    /// <summary>
    /// Summary description for UriSection.
    /// </summary> 
    public sealed class UriSection : ConfigurationSection
    { 
        public UriSection(){ 
            this.properties.Add(this.idn);
            this.properties.Add(this.iriParsing); 
        }

        [ConfigurationProperty(CommonConfigurationStrings.Idn)]
        public IdnElement Idn{ 
            get {
                return (IdnElement)this[this.idn]; 
            } 
        }
 
        [ConfigurationProperty(CommonConfigurationStrings.IriParsing)]
        public IriParsingElement IriParsing
        {
            get{ 
                return (IriParsingElement)this[this.iriParsing];
            } 
        } 

        protected override ConfigurationPropertyCollection Properties 
        {
            get{
                return this.properties;
            } 
        }
 
 	 
        ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection();
 
        readonly ConfigurationProperty idn =
            new ConfigurationProperty(CommonConfigurationStrings.Idn, typeof(IdnElement), null,
                    ConfigurationPropertyOptions.None);
 
        readonly ConfigurationProperty iriParsing =
            new ConfigurationProperty(CommonConfigurationStrings.IriParsing, typeof(IriParsingElement), null, 
                    ConfigurationPropertyOptions.None); 
    }
 
    internal sealed class UriSectionInternal
    {
        internal UriSectionInternal(UriSection section)
        { 
            this.idn = section.Idn.Enabled;
            this.iriParsing = section.IriParsing.Enabled; 
        } 

        internal UriIdnScope Idn 
        {
            get { return this.idn; }
        }
 
        internal bool IriParsing
        { 
            get { return this.iriParsing; } 
        }
 
        bool iriParsing;
        UriIdnScope idn;

        internal static object ClassSyncObject 
        {
            get{ 
                if (classSyncObject == null){ 
                    Interlocked.CompareExchange(ref classSyncObject, new object(), null);
                } 
                return classSyncObject;
            }
        }
 
        internal static UriSectionInternal GetSection()
        { 
            lock (ClassSyncObject){ 
                UriSection section = PrivilegedConfigurationManager.GetSection(CommonConfigurationStrings.UriSectionPath) as UriSection;
                if (section == null) 
                    return null;

                return new UriSectionInternal(section);
            } 
        }
 
        private static object classSyncObject; 
    }
} 

