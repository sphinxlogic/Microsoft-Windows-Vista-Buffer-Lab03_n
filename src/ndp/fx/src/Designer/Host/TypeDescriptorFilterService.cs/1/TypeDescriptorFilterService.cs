//------------------------------------------------------------------------------ 
// <copyright file="TypeDescriptorFilterService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;

    /// <devdoc> 
    ///     This service is requested by TypeDescriptor when asking for type information
    ///     for a component.  Our implementation forwards this filter onto IDesignerFilter 
    ///     on the component's designer, should one exist. 
    /// </devdoc>
    internal sealed class TypeDescriptorFilterService : ITypeDescriptorFilterService { 

        /// <devdoc>
        ///     Internal ctor to prevent semitrust from creating us.
        /// </devdoc> 
        internal TypeDescriptorFilterService() {
        } 
 
        /// <devdoc>
        ///     Helper method to return the designer for a given component. 
        /// </devdoc>
        private IDesigner GetDesigner(IComponent component) {
            ISite site = component.Site;
            if (site != null) { 
                IDesignerHost host = site.GetService(typeof(IDesignerHost)) as IDesignerHost;
                if (host != null) { 
                    return host.GetDesigner(component); 
                }
            } 
            return null;
        }

        /// <devdoc> 
        ///    <para>
        ///       Provides a way to filter the attributes from a component that are displayed to the user. 
        ///    </para> 
        /// </devdoc>
        bool ITypeDescriptorFilterService.FilterAttributes(IComponent component, IDictionary attributes) { 
            if (component == null) {
                throw new ArgumentNullException("component");
            }
            if (attributes == null) { 
                throw new ArgumentNullException("attributes");
            } 
 
            IDesigner designer = GetDesigner(component);
 
            if (designer is IDesignerFilter) {
                ((IDesignerFilter)designer).PreFilterAttributes(attributes);
                ((IDesignerFilter)designer).PostFilterAttributes(attributes);
            } 
            return designer != null;
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Provides a way to filter the events from a component that are displayed to the user.
        ///    </para>
        /// </devdoc>
        bool ITypeDescriptorFilterService.FilterEvents(IComponent component, IDictionary events) { 
            if (component == null) {
                throw new ArgumentNullException("component"); 
            } 
            if (events == null) {
                throw new ArgumentNullException("events"); 
            }

            IDesigner designer = GetDesigner(component);
 
            if (designer is IDesignerFilter) {
                ((IDesignerFilter)designer).PreFilterEvents(events); 
                ((IDesignerFilter)designer).PostFilterEvents(events); 
            }
            return designer != null; 
        }

        /// <devdoc>
        ///    <para> 
        ///       Provides a way to filter the properties from a component that are displayed to the user.
        ///    </para> 
        /// </devdoc> 
        bool ITypeDescriptorFilterService.FilterProperties(IComponent component, IDictionary properties) {
            if (component == null) { 
                throw new ArgumentNullException("component");
            }
            if (properties == null) {
                throw new ArgumentNullException("properties"); 
            }
 
            IDesigner designer = GetDesigner(component); 

            if (designer is IDesignerFilter) { 
                ((IDesignerFilter)designer).PreFilterProperties(properties);
                ((IDesignerFilter)designer).PostFilterProperties(properties);
            }
            return designer != null; 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TypeDescriptorFilterService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;

    /// <devdoc> 
    ///     This service is requested by TypeDescriptor when asking for type information
    ///     for a component.  Our implementation forwards this filter onto IDesignerFilter 
    ///     on the component's designer, should one exist. 
    /// </devdoc>
    internal sealed class TypeDescriptorFilterService : ITypeDescriptorFilterService { 

        /// <devdoc>
        ///     Internal ctor to prevent semitrust from creating us.
        /// </devdoc> 
        internal TypeDescriptorFilterService() {
        } 
 
        /// <devdoc>
        ///     Helper method to return the designer for a given component. 
        /// </devdoc>
        private IDesigner GetDesigner(IComponent component) {
            ISite site = component.Site;
            if (site != null) { 
                IDesignerHost host = site.GetService(typeof(IDesignerHost)) as IDesignerHost;
                if (host != null) { 
                    return host.GetDesigner(component); 
                }
            } 
            return null;
        }

        /// <devdoc> 
        ///    <para>
        ///       Provides a way to filter the attributes from a component that are displayed to the user. 
        ///    </para> 
        /// </devdoc>
        bool ITypeDescriptorFilterService.FilterAttributes(IComponent component, IDictionary attributes) { 
            if (component == null) {
                throw new ArgumentNullException("component");
            }
            if (attributes == null) { 
                throw new ArgumentNullException("attributes");
            } 
 
            IDesigner designer = GetDesigner(component);
 
            if (designer is IDesignerFilter) {
                ((IDesignerFilter)designer).PreFilterAttributes(attributes);
                ((IDesignerFilter)designer).PostFilterAttributes(attributes);
            } 
            return designer != null;
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Provides a way to filter the events from a component that are displayed to the user.
        ///    </para>
        /// </devdoc>
        bool ITypeDescriptorFilterService.FilterEvents(IComponent component, IDictionary events) { 
            if (component == null) {
                throw new ArgumentNullException("component"); 
            } 
            if (events == null) {
                throw new ArgumentNullException("events"); 
            }

            IDesigner designer = GetDesigner(component);
 
            if (designer is IDesignerFilter) {
                ((IDesignerFilter)designer).PreFilterEvents(events); 
                ((IDesignerFilter)designer).PostFilterEvents(events); 
            }
            return designer != null; 
        }

        /// <devdoc>
        ///    <para> 
        ///       Provides a way to filter the properties from a component that are displayed to the user.
        ///    </para> 
        /// </devdoc> 
        bool ITypeDescriptorFilterService.FilterProperties(IComponent component, IDictionary properties) {
            if (component == null) { 
                throw new ArgumentNullException("component");
            }
            if (properties == null) {
                throw new ArgumentNullException("properties"); 
            }
 
            IDesigner designer = GetDesigner(component); 

            if (designer is IDesignerFilter) { 
                ((IDesignerFilter)designer).PreFilterProperties(properties);
                ((IDesignerFilter)designer).PostFilterProperties(properties);
            }
            return designer != null; 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
