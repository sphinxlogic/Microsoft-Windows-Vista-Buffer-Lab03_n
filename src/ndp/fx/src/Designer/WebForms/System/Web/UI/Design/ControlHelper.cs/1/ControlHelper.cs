//------------------------------------------------------------------------------ 
// <copyright file="ControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.CodeDom;
    using System.Runtime.InteropServices;
    using System.Collections.Generic; 

    internal static class ControlHelper { 
 
        /// <devdoc>
        /// Finds a control with a given ID starting with the siblings of the given control. 
        /// This method walk up naming container boundaries to find the control.
        /// </devdoc>
        internal static Control FindControl(IServiceProvider serviceProvider, Control control, string controlIdToFind) {
            if (String.IsNullOrEmpty(controlIdToFind)) { 
                throw new ArgumentNullException("controlIdToFind");
            } 
 
            while (control != null) {
                if (control.Site == null || control.Site.Container == null) { 
                    return null;
                }
                IComponent component = control.Site.Container.Components[controlIdToFind];
                if (component != null) { 
                    return component as Control;
                } 
 
                // Try to get the parent of this control's naming container
                IDesignerHost designerHost = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost)); 
                if (designerHost == null) {
                    return null;
                }
                ControlDesigner designer = designerHost.GetDesigner(control) as ControlDesigner; 
                if (designer == null || designer.View == null || designer.View.NamingContainerDesigner == null) {
                    return null; 
                } 
                control = designer.View.NamingContainerDesigner.Component as Control;
            } 

            if (serviceProvider != null) {
                IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                if (host != null) { 
                    IContainer container = host.Container;
                    if (container != null) { 
                        return container.Components[controlIdToFind] as System.Web.UI.Control; 
                    }
                } 
            }
            return null;
        }
 
        internal delegate bool IsValidComponentDelegate(IComponent component);
 
        internal static IList<IComponent> GetAllComponents(IComponent component, IsValidComponentDelegate componentFilter) { 
            List<IComponent> foundComponents = new List<IComponent>();
 
            while (component != null) {
                IList<IComponent> components = GetComponentsInContainer(component, componentFilter);
                foundComponents.AddRange(components);
 
                // Walk up to the naming container to get the next level of controls
                IDesignerHost designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost)); 
                ControlDesigner designer = designerHost.GetDesigner(component) as ControlDesigner; 
                component = null;
                if (designer != null && designer.View != null && designer.View.NamingContainerDesigner != null) { 
                    component = designer.View.NamingContainerDesigner.Component;
                }
            }
            return foundComponents; 
        }
 
        private static IList<IComponent> GetComponentsInContainer(IComponent component, IsValidComponentDelegate componentFilter) { 
            System.Diagnostics.Debug.Assert(component != null);
            List<IComponent> foundComponents = new List<IComponent>(); 

            if ((component.Site != null) && (component.Site.Container != null)) {
                foreach (IComponent comp in component.Site.Container.Components) {
                    if (componentFilter(comp) && !Marshal.IsComObject(comp)) { 
                        PropertyDescriptor modifierProp = TypeDescriptor.GetProperties(comp)["Modifiers"];
                        if (modifierProp != null) { 
                            MemberAttributes modifiers = (MemberAttributes)modifierProp.GetValue(comp); 
                            if ((modifiers & MemberAttributes.AccessMask) == MemberAttributes.Private) {
                                // must be declared as public or protected 
                                continue;
                            }
                        }
 
                        foundComponents.Add(comp);
                    } 
                } 
            }
            return foundComponents; 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.CodeDom;
    using System.Runtime.InteropServices;
    using System.Collections.Generic; 

    internal static class ControlHelper { 
 
        /// <devdoc>
        /// Finds a control with a given ID starting with the siblings of the given control. 
        /// This method walk up naming container boundaries to find the control.
        /// </devdoc>
        internal static Control FindControl(IServiceProvider serviceProvider, Control control, string controlIdToFind) {
            if (String.IsNullOrEmpty(controlIdToFind)) { 
                throw new ArgumentNullException("controlIdToFind");
            } 
 
            while (control != null) {
                if (control.Site == null || control.Site.Container == null) { 
                    return null;
                }
                IComponent component = control.Site.Container.Components[controlIdToFind];
                if (component != null) { 
                    return component as Control;
                } 
 
                // Try to get the parent of this control's naming container
                IDesignerHost designerHost = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost)); 
                if (designerHost == null) {
                    return null;
                }
                ControlDesigner designer = designerHost.GetDesigner(control) as ControlDesigner; 
                if (designer == null || designer.View == null || designer.View.NamingContainerDesigner == null) {
                    return null; 
                } 
                control = designer.View.NamingContainerDesigner.Component as Control;
            } 

            if (serviceProvider != null) {
                IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
                if (host != null) { 
                    IContainer container = host.Container;
                    if (container != null) { 
                        return container.Components[controlIdToFind] as System.Web.UI.Control; 
                    }
                } 
            }
            return null;
        }
 
        internal delegate bool IsValidComponentDelegate(IComponent component);
 
        internal static IList<IComponent> GetAllComponents(IComponent component, IsValidComponentDelegate componentFilter) { 
            List<IComponent> foundComponents = new List<IComponent>();
 
            while (component != null) {
                IList<IComponent> components = GetComponentsInContainer(component, componentFilter);
                foundComponents.AddRange(components);
 
                // Walk up to the naming container to get the next level of controls
                IDesignerHost designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost)); 
                ControlDesigner designer = designerHost.GetDesigner(component) as ControlDesigner; 
                component = null;
                if (designer != null && designer.View != null && designer.View.NamingContainerDesigner != null) { 
                    component = designer.View.NamingContainerDesigner.Component;
                }
            }
            return foundComponents; 
        }
 
        private static IList<IComponent> GetComponentsInContainer(IComponent component, IsValidComponentDelegate componentFilter) { 
            System.Diagnostics.Debug.Assert(component != null);
            List<IComponent> foundComponents = new List<IComponent>(); 

            if ((component.Site != null) && (component.Site.Container != null)) {
                foreach (IComponent comp in component.Site.Container.Components) {
                    if (componentFilter(comp) && !Marshal.IsComObject(comp)) { 
                        PropertyDescriptor modifierProp = TypeDescriptor.GetProperties(comp)["Modifiers"];
                        if (modifierProp != null) { 
                            MemberAttributes modifiers = (MemberAttributes)modifierProp.GetValue(comp); 
                            if ((modifiers & MemberAttributes.AccessMask) == MemberAttributes.Private) {
                                // must be declared as public or protected 
                                continue;
                            }
                        }
 
                        foundComponents.Add(comp);
                    } 
                } 
            }
            return foundComponents; 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
