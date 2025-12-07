 
//------------------------------------------------------------------------------
// <copyright file="HostDesigntimeLicenseContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.ComponentModel.Design { 


    /// <include file='doc\HostDesigntimeLicenseContext.uex' path='docs/doc[@for="HostDesigntimeLicenseContext"]/*' />
    /// <devdoc> 
    /// This class will provide a license context that the LicenseManager can use
    /// to get to the design time services, like ITypeResolutionService. 
    /// </devdoc> 
    internal class HostDesigntimeLicenseContext : DesigntimeLicenseContext {
        private IServiceProvider provider; 

        public HostDesigntimeLicenseContext(IServiceProvider provider) {
            this.provider = provider;
        } 

        public override object GetService(Type serviceClass) { 
            return provider.GetService(serviceClass); 
        }
    } 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="HostDesigntimeLicenseContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.ComponentModel.Design { 


    /// <include file='doc\HostDesigntimeLicenseContext.uex' path='docs/doc[@for="HostDesigntimeLicenseContext"]/*' />
    /// <devdoc> 
    /// This class will provide a license context that the LicenseManager can use
    /// to get to the design time services, like ITypeResolutionService. 
    /// </devdoc> 
    internal class HostDesigntimeLicenseContext : DesigntimeLicenseContext {
        private IServiceProvider provider; 

        public HostDesigntimeLicenseContext(IServiceProvider provider) {
            this.provider = provider;
        } 

        public override object GetService(Type serviceClass) { 
            return provider.GetService(serviceClass); 
        }
    } 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
