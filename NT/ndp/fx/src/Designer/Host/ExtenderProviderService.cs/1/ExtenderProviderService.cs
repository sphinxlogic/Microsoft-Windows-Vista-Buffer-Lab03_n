//------------------------------------------------------------------------------ 
// <copyright file="ExtenderProviderService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 

    /// <devdoc> 
    ///     The extender provider service actually provides two services:  IExtenderProviderService, 
    ///     which allows other objects to add and remove extender providers, and IExtenderListService,
    ///     which is used by TypeDescriptor to discover the set of extender providers. 
    /// </devdoc>
    internal sealed class ExtenderProviderService : IExtenderProviderService, IExtenderListService {

        private ArrayList _providers; 

        /// <devdoc> 
        ///     Internal ctor to prevent semitrust from creating us. 
        /// </devdoc>
        internal ExtenderProviderService() { 
        }

        /// <devdoc>
        ///    <para>Gets the set of extender providers for the component.</para> 
        /// </devdoc>
        IExtenderProvider[] IExtenderListService.GetExtenderProviders() { 
            if (_providers != null) { 
                IExtenderProvider[] providers = new IExtenderProvider[_providers.Count];
                _providers.CopyTo(providers, 0); 
                return providers;
            }

            return new IExtenderProvider[0]; 
        }
 
        /// <devdoc> 
        ///    <para>
        ///       Adds an extender provider. 
        ///    </para>
        /// </devdoc>
        void IExtenderProviderService.AddExtenderProvider(IExtenderProvider provider) {
 
            if (provider == null) {
                throw new ArgumentNullException("provider"); 
            } 

            if (_providers == null) { 
                _providers = new ArrayList(4);
            }

            if (_providers.Contains(provider)) { 
                throw new ArgumentException(SR.GetString(SR.ExtenderProviderServiceDuplicateProvider, provider));
            } 
 
            _providers.Add(provider);
        } 

        /// <devdoc>
        ///    <para>
        ///       Removes 
        ///       an extender provider.
        ///    </para> 
        /// </devdoc> 
        void IExtenderProviderService.RemoveExtenderProvider(IExtenderProvider provider) {
 
            if (provider == null) {
                throw new ArgumentNullException("provider");
            }
 
            if (_providers != null) {
                _providers.Remove(provider); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ExtenderProviderService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 

    /// <devdoc> 
    ///     The extender provider service actually provides two services:  IExtenderProviderService, 
    ///     which allows other objects to add and remove extender providers, and IExtenderListService,
    ///     which is used by TypeDescriptor to discover the set of extender providers. 
    /// </devdoc>
    internal sealed class ExtenderProviderService : IExtenderProviderService, IExtenderListService {

        private ArrayList _providers; 

        /// <devdoc> 
        ///     Internal ctor to prevent semitrust from creating us. 
        /// </devdoc>
        internal ExtenderProviderService() { 
        }

        /// <devdoc>
        ///    <para>Gets the set of extender providers for the component.</para> 
        /// </devdoc>
        IExtenderProvider[] IExtenderListService.GetExtenderProviders() { 
            if (_providers != null) { 
                IExtenderProvider[] providers = new IExtenderProvider[_providers.Count];
                _providers.CopyTo(providers, 0); 
                return providers;
            }

            return new IExtenderProvider[0]; 
        }
 
        /// <devdoc> 
        ///    <para>
        ///       Adds an extender provider. 
        ///    </para>
        /// </devdoc>
        void IExtenderProviderService.AddExtenderProvider(IExtenderProvider provider) {
 
            if (provider == null) {
                throw new ArgumentNullException("provider"); 
            } 

            if (_providers == null) { 
                _providers = new ArrayList(4);
            }

            if (_providers.Contains(provider)) { 
                throw new ArgumentException(SR.GetString(SR.ExtenderProviderServiceDuplicateProvider, provider));
            } 
 
            _providers.Add(provider);
        } 

        /// <devdoc>
        ///    <para>
        ///       Removes 
        ///       an extender provider.
        ///    </para> 
        /// </devdoc> 
        void IExtenderProviderService.RemoveExtenderProvider(IExtenderProvider provider) {
 
            if (provider == null) {
                throw new ArgumentNullException("provider");
            }
 
            if (_providers != null) {
                _providers.Remove(provider); 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
