//------------------------------------------------------------------------------ 
// <copyright file="SocketElement.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Configuration 
{ 
    using System;
    using System.Configuration; 
    using System.Reflection;
    using System.Security.Permissions;

    public sealed class SocketElement : ConfigurationElement 
    {
        public SocketElement() 
        { 
            this.properties.Add(this.alwaysUseCompletionPortsForAccept);
            this.properties.Add(this.alwaysUseCompletionPortsForConnect); 
        }

        protected override void PostDeserialize()
        { 
            // Perf optimization. If the configuration is coming from machine.config
            // It is safe and we don't need to check for permissions. 
            if (EvaluationContext.IsMachineLevel) 
                return;
 
            try {
                ExceptionHelper.UnrestrictedSocketPermission.Demand();
            } catch (Exception exception) {
                throw new ConfigurationErrorsException( 
                              SR.GetString(SR.net_config_element_permission,
                                           ConfigurationStrings.Socket), 
                              exception); 
            }
        } 

        [ConfigurationProperty(ConfigurationStrings.AlwaysUseCompletionPortsForAccept, DefaultValue = false)]
        public bool AlwaysUseCompletionPortsForAccept
        { 
            get { return (bool)this[this.alwaysUseCompletionPortsForAccept]; }
            set { this[this.alwaysUseCompletionPortsForAccept] = value; } 
        } 

        [ConfigurationProperty(ConfigurationStrings.AlwaysUseCompletionPortsForConnect, DefaultValue = false)] 
        public bool AlwaysUseCompletionPortsForConnect
        {
            get { return (bool)this[this.alwaysUseCompletionPortsForConnect]; }
            set { this[this.alwaysUseCompletionPortsForConnect] = value; } 
        }
 
        protected override ConfigurationPropertyCollection Properties 
        {
            get 
            {
                return this.properties;
            }
        } 

        ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection(); 
 
        readonly ConfigurationProperty alwaysUseCompletionPortsForConnect =
            new ConfigurationProperty(ConfigurationStrings.AlwaysUseCompletionPortsForConnect, typeof(bool), false, 
                    ConfigurationPropertyOptions.None);

        readonly ConfigurationProperty alwaysUseCompletionPortsForAccept =
            new ConfigurationProperty(ConfigurationStrings.AlwaysUseCompletionPortsForAccept, typeof(bool), false, 
                    ConfigurationPropertyOptions.None);
 
    } 
}
 
//------------------------------------------------------------------------------ 
// <copyright file="SocketElement.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Configuration 
{ 
    using System;
    using System.Configuration; 
    using System.Reflection;
    using System.Security.Permissions;

    public sealed class SocketElement : ConfigurationElement 
    {
        public SocketElement() 
        { 
            this.properties.Add(this.alwaysUseCompletionPortsForAccept);
            this.properties.Add(this.alwaysUseCompletionPortsForConnect); 
        }

        protected override void PostDeserialize()
        { 
            // Perf optimization. If the configuration is coming from machine.config
            // It is safe and we don't need to check for permissions. 
            if (EvaluationContext.IsMachineLevel) 
                return;
 
            try {
                ExceptionHelper.UnrestrictedSocketPermission.Demand();
            } catch (Exception exception) {
                throw new ConfigurationErrorsException( 
                              SR.GetString(SR.net_config_element_permission,
                                           ConfigurationStrings.Socket), 
                              exception); 
            }
        } 

        [ConfigurationProperty(ConfigurationStrings.AlwaysUseCompletionPortsForAccept, DefaultValue = false)]
        public bool AlwaysUseCompletionPortsForAccept
        { 
            get { return (bool)this[this.alwaysUseCompletionPortsForAccept]; }
            set { this[this.alwaysUseCompletionPortsForAccept] = value; } 
        } 

        [ConfigurationProperty(ConfigurationStrings.AlwaysUseCompletionPortsForConnect, DefaultValue = false)] 
        public bool AlwaysUseCompletionPortsForConnect
        {
            get { return (bool)this[this.alwaysUseCompletionPortsForConnect]; }
            set { this[this.alwaysUseCompletionPortsForConnect] = value; } 
        }
 
        protected override ConfigurationPropertyCollection Properties 
        {
            get 
            {
                return this.properties;
            }
        } 

        ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection(); 
 
        readonly ConfigurationProperty alwaysUseCompletionPortsForConnect =
            new ConfigurationProperty(ConfigurationStrings.AlwaysUseCompletionPortsForConnect, typeof(bool), false, 
                    ConfigurationPropertyOptions.None);

        readonly ConfigurationProperty alwaysUseCompletionPortsForAccept =
            new ConfigurationProperty(ConfigurationStrings.AlwaysUseCompletionPortsForAccept, typeof(bool), false, 
                    ConfigurationPropertyOptions.None);
 
    } 
}
 
