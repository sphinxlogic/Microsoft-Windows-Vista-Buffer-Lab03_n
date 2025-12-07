//------------------------------------------------------------------------------ 
// <copyright file="IriParsingElement.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Configuration 
{ 
    using System;
 
    public sealed class IriParsingElement : ConfigurationElement
    {
        public IriParsingElement()
        { 
            this.properties.Add(this.enabled);
        } 
 
        protected override ConfigurationPropertyCollection Properties
        { 
            get{
                return this.properties;
            }
        } 

        [ConfigurationProperty(CommonConfigurationStrings.Enabled, DefaultValue = false)] 
        public bool Enabled 
        {
            get { return (bool)this[this.enabled]; } 
            set { this[this.enabled] = value; }
        }

        ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection(); 

        readonly ConfigurationProperty enabled = 
            new ConfigurationProperty(CommonConfigurationStrings.Enabled, typeof(bool), false, 
                    ConfigurationPropertyOptions.None);
 
    }
}

 
//------------------------------------------------------------------------------ 
// <copyright file="IriParsingElement.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Configuration 
{ 
    using System;
 
    public sealed class IriParsingElement : ConfigurationElement
    {
        public IriParsingElement()
        { 
            this.properties.Add(this.enabled);
        } 
 
        protected override ConfigurationPropertyCollection Properties
        { 
            get{
                return this.properties;
            }
        } 

        [ConfigurationProperty(CommonConfigurationStrings.Enabled, DefaultValue = false)] 
        public bool Enabled 
        {
            get { return (bool)this[this.enabled]; } 
            set { this[this.enabled] = value; }
        }

        ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection(); 

        readonly ConfigurationProperty enabled = 
            new ConfigurationProperty(CommonConfigurationStrings.Enabled, typeof(bool), false, 
                    ConfigurationPropertyOptions.None);
 
    }
}

 
