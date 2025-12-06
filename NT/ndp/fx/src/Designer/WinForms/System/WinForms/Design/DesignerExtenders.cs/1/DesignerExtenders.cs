 
//------------------------------------------------------------------------------
// <copyright file="DesignerExtenders.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.Windows.Forms.Design { 

    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection;
    using System.Diagnostics.CodeAnalysis;
 
    /// <devdoc>
    ///     This class provides the Modifiers property to components.  It is shared between 
    ///     the document designer and the component document designer. 
    /// </devdoc>
    internal class DesignerExtenders { 

        private IExtenderProvider[] providers;
        private IExtenderProviderService extenderService;
 
        /// <include file='doc\DesignerExtenders.uex' path='docs/doc[@for="DesignerExtenders.AddExtenderProviders"]/*' />
        /// <devdoc> 
        ///     This is called by a root designer to add the correct extender providers. 
        /// </devdoc>
        public DesignerExtenders(IExtenderProviderService ex) { 
            this.extenderService = ex;
            if (providers == null) {
                providers = new IExtenderProvider[] {
                    new NameExtenderProvider(), 
                    new NameInheritedExtenderProvider()
                }; 
            } 

            for (int i = 0; i < providers.Length; i++) { 
                ex.AddExtenderProvider(providers[i]);
            }
        }
 
        /// <include file='doc\DesignerExtenders.uex' path='docs/doc[@for="DesignerExtenders.RemoveExtenderProviders"]/*' />
        /// <devdoc> 
        ///      This is called at the appropriate time to remove any extra extender 
        ///      providers previously added to the designer host.
        /// </devdoc> 
        public void Dispose() {
            if (extenderService != null && providers != null) {
                for (int i = 0; i < providers.Length; i++) {
                    extenderService.RemoveExtenderProvider(providers[i]); 
                }
 
                providers = null; 
                extenderService = null;
            } 
        }

        /// <devdoc>
        ///     This is the base extender provider for all winform document 
        ///     designers.  It provides the "Name" property.
        /// </devdoc> 
        [ 
        ProvideProperty("Name", typeof(IComponent))
        ] 
        private class NameExtenderProvider : IExtenderProvider {

            private IComponent baseComponent;
 
            /// <devdoc>
            ///      Creates a new DocumentExtenderProvider. 
            /// </devdoc> 
            internal NameExtenderProvider() {
            } 

            protected IComponent GetBaseComponent(object o) {
                if (baseComponent == null) {
                    ISite site = ((IComponent)o).Site; 
                    if (site != null) {
                        IDesignerHost host = (IDesignerHost)site.GetService(typeof(IDesignerHost)); 
                        if (host != null) { 
                            baseComponent = host.RootComponent;
                        } 
                    }
                }
                return baseComponent;
            } 

            /// <devdoc> 
            ///     Determines if ths extender provider can extend the given object.  We extend 
            ///     all objects, so we always return true.
            /// </devdoc> 
            public virtual bool CanExtend(object o) {

                // We always extend the root
                // 
                IComponent baseComp = GetBaseComponent(o);
                if (baseComp == o) { 
                    return true; 
                }
 
                // See if this object is inherited.  If so, then we don't want to
                // extend.
                //
                if (!TypeDescriptor.GetAttributes(o)[typeof(InheritanceAttribute)].Equals(InheritanceAttribute.NotInherited)) { 
                    return false;
                } 
 
                return true;
            } 

            /// <devdoc>
            ///     This is an extender property that we offer to all components
            ///     on the form.  It implements the "Name" property. 
            /// </devdoc>
            [ 
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
            ParenthesizePropertyName(true),
            MergableProperty(false), 
            SRDescriptionAttribute(SR.DesignerPropName),
            Category("Design")
            ]
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            public virtual string GetName(IComponent comp) {
                ISite site = comp.Site; 
                if (site != null) { 
                    return site.Name;
                } 
                return null;
            }

            /// <devdoc> 
            ///     This is an extender property that we offer to all components
            ///     on the form.  It implements the "Name" property. 
            /// </devdoc> 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void SetName(IComponent comp, string newName) { 
                ISite site = comp.Site;
                if (site != null) {
                    site.Name = newName;
                } 
            }
        } 
 
        /// <devdoc>
        ///      This extender provider offers up read-only versions of "Name" property 
        ///      for inherited components.
        /// </devdoc>
        private class NameInheritedExtenderProvider : NameExtenderProvider {
 
            /// <devdoc>
            ///      Creates a new DocumentInheritedExtenderProvider. 
            /// </devdoc> 
            internal NameInheritedExtenderProvider() {
            } 

            /// <devdoc>
            ///     Determines if ths extender provider can extend the given object.  We extend
            ///     all objects, so we always return true. 
            /// </devdoc>
            public override bool CanExtend(object o) { 
 
                // We never extend the root
                // 
                IComponent baseComp = GetBaseComponent(o);
                if (baseComp == o) {
                    return false;
                } 

                // See if this object is inherited.  If so, then we are interested in it. 
                // 
                if (!TypeDescriptor.GetAttributes(o)[typeof(InheritanceAttribute)].Equals(InheritanceAttribute.NotInherited)) {
                    return true; 
                }

                return false;
            } 

            [ReadOnly(true)] 
            public override string GetName(IComponent comp) { 
                return base.GetName(comp);
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="DesignerExtenders.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.Windows.Forms.Design { 

    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Reflection;
    using System.Diagnostics.CodeAnalysis;
 
    /// <devdoc>
    ///     This class provides the Modifiers property to components.  It is shared between 
    ///     the document designer and the component document designer. 
    /// </devdoc>
    internal class DesignerExtenders { 

        private IExtenderProvider[] providers;
        private IExtenderProviderService extenderService;
 
        /// <include file='doc\DesignerExtenders.uex' path='docs/doc[@for="DesignerExtenders.AddExtenderProviders"]/*' />
        /// <devdoc> 
        ///     This is called by a root designer to add the correct extender providers. 
        /// </devdoc>
        public DesignerExtenders(IExtenderProviderService ex) { 
            this.extenderService = ex;
            if (providers == null) {
                providers = new IExtenderProvider[] {
                    new NameExtenderProvider(), 
                    new NameInheritedExtenderProvider()
                }; 
            } 

            for (int i = 0; i < providers.Length; i++) { 
                ex.AddExtenderProvider(providers[i]);
            }
        }
 
        /// <include file='doc\DesignerExtenders.uex' path='docs/doc[@for="DesignerExtenders.RemoveExtenderProviders"]/*' />
        /// <devdoc> 
        ///      This is called at the appropriate time to remove any extra extender 
        ///      providers previously added to the designer host.
        /// </devdoc> 
        public void Dispose() {
            if (extenderService != null && providers != null) {
                for (int i = 0; i < providers.Length; i++) {
                    extenderService.RemoveExtenderProvider(providers[i]); 
                }
 
                providers = null; 
                extenderService = null;
            } 
        }

        /// <devdoc>
        ///     This is the base extender provider for all winform document 
        ///     designers.  It provides the "Name" property.
        /// </devdoc> 
        [ 
        ProvideProperty("Name", typeof(IComponent))
        ] 
        private class NameExtenderProvider : IExtenderProvider {

            private IComponent baseComponent;
 
            /// <devdoc>
            ///      Creates a new DocumentExtenderProvider. 
            /// </devdoc> 
            internal NameExtenderProvider() {
            } 

            protected IComponent GetBaseComponent(object o) {
                if (baseComponent == null) {
                    ISite site = ((IComponent)o).Site; 
                    if (site != null) {
                        IDesignerHost host = (IDesignerHost)site.GetService(typeof(IDesignerHost)); 
                        if (host != null) { 
                            baseComponent = host.RootComponent;
                        } 
                    }
                }
                return baseComponent;
            } 

            /// <devdoc> 
            ///     Determines if ths extender provider can extend the given object.  We extend 
            ///     all objects, so we always return true.
            /// </devdoc> 
            public virtual bool CanExtend(object o) {

                // We always extend the root
                // 
                IComponent baseComp = GetBaseComponent(o);
                if (baseComp == o) { 
                    return true; 
                }
 
                // See if this object is inherited.  If so, then we don't want to
                // extend.
                //
                if (!TypeDescriptor.GetAttributes(o)[typeof(InheritanceAttribute)].Equals(InheritanceAttribute.NotInherited)) { 
                    return false;
                } 
 
                return true;
            } 

            /// <devdoc>
            ///     This is an extender property that we offer to all components
            ///     on the form.  It implements the "Name" property. 
            /// </devdoc>
            [ 
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
            ParenthesizePropertyName(true),
            MergableProperty(false), 
            SRDescriptionAttribute(SR.DesignerPropName),
            Category("Design")
            ]
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            public virtual string GetName(IComponent comp) {
                ISite site = comp.Site; 
                if (site != null) { 
                    return site.Name;
                } 
                return null;
            }

            /// <devdoc> 
            ///     This is an extender property that we offer to all components
            ///     on the form.  It implements the "Name" property. 
            /// </devdoc> 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void SetName(IComponent comp, string newName) { 
                ISite site = comp.Site;
                if (site != null) {
                    site.Name = newName;
                } 
            }
        } 
 
        /// <devdoc>
        ///      This extender provider offers up read-only versions of "Name" property 
        ///      for inherited components.
        /// </devdoc>
        private class NameInheritedExtenderProvider : NameExtenderProvider {
 
            /// <devdoc>
            ///      Creates a new DocumentInheritedExtenderProvider. 
            /// </devdoc> 
            internal NameInheritedExtenderProvider() {
            } 

            /// <devdoc>
            ///     Determines if ths extender provider can extend the given object.  We extend
            ///     all objects, so we always return true. 
            /// </devdoc>
            public override bool CanExtend(object o) { 
 
                // We never extend the root
                // 
                IComponent baseComp = GetBaseComponent(o);
                if (baseComp == o) {
                    return false;
                } 

                // See if this object is inherited.  If so, then we are interested in it. 
                // 
                if (!TypeDescriptor.GetAttributes(o)[typeof(InheritanceAttribute)].Equals(InheritanceAttribute.NotInherited)) {
                    return true; 
                }

                return false;
            } 

            [ReadOnly(true)] 
            public override string GetName(IComponent comp) { 
                return base.GetName(comp);
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
