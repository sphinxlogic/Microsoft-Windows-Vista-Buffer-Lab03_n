 
//------------------------------------------------------------------------------
// <copyright file="TableLayoutPanelCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
*/
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.TableLayoutPanelCodeDomSerializer..ctor()")] 
namespace System.Windows.Forms.Design {

    using System;
    using System.CodeDom; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics; 
    using System.Windows.Forms;
    using System.ComponentModel.Design.Serialization; 

    /// <devdoc>
    ///     Custom serializer for the TableLayoutPanel. We need this so we can push the TableLayoutSettings object
    ///     into the resx in localization mode. This is used by loc tools like WinRes to correctly setup the 
    ///     TableLayoutPanel with all its settings. Note that we don't serialize code to access the settings.
    /// </devdoc> 
    internal class TableLayoutPanelCodeDomSerializer : CodeDomSerializer { 
        private static readonly string LayoutSettingsPropName = "LayoutSettings";
 
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject) {
                return GetBaseSerializer(manager).Deserialize(manager, codeObject);
        }
 
        private CodeDomSerializer GetBaseSerializer(IDesignerSerializationManager manager) {
            return (CodeDomSerializer)manager.GetSerializer(typeof(TableLayoutPanel).BaseType, typeof(CodeDomSerializer)); 
        } 

        /// <devdoc> 
        ///     We don't actually want to serialize any code here, so we just delegate that to the base type's
        ///     serializer. All we want to do is if we are in a localizable form, we want to push a
        ///     'LayoutSettings' entry into the resx.
        /// </devdoc> 
        public override object Serialize(IDesignerSerializationManager manager, object value) {
            // First call the base serializer to serialize the object. 
            object codeObject = GetBaseSerializer(manager).Serialize(manager, value); 

 
            // Now push our layout settings stuff into the resx if we are not inherited read only and
            // are in a localizable Form.
            TableLayoutPanel tlp = value as TableLayoutPanel;
            Debug.Assert(tlp != null, "Huh? We were expecting to be serializing a TableLayoutPanel here."); 

            if (tlp != null) { 
                InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(tlp)[typeof(InheritanceAttribute)]; 

                if (ia == null || ia.InheritanceLevel != InheritanceLevel.InheritedReadOnly) { 
                    IDesignerHost host = (IDesignerHost)manager.GetService(typeof(IDesignerHost));
                    if (IsLocalizable(host)) {
                        PropertyDescriptor lsProp = TypeDescriptor.GetProperties(tlp)[LayoutSettingsPropName];
                        object val = (lsProp != null) ? lsProp.GetValue(tlp) : null; 
                        if (val != null) {
                            string key = manager.GetName(tlp) + "." + LayoutSettingsPropName; 
                            SerializeResourceInvariant(manager, key, val); 
                        }
                    } 
                }
            }

 
            return codeObject;
        } 
 
        private bool IsLocalizable(IDesignerHost host) {
            if (host != null) { 
                PropertyDescriptor prop = TypeDescriptor.GetProperties(host.RootComponent)["Localizable"];
                if (prop != null && prop.PropertyType == typeof(bool)) {
                     return (bool) prop.GetValue(host.RootComponent);
                } 
            }
 
            return false; 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="TableLayoutPanelCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
*/
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.TableLayoutPanelCodeDomSerializer..ctor()")] 
namespace System.Windows.Forms.Design {

    using System;
    using System.CodeDom; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics; 
    using System.Windows.Forms;
    using System.ComponentModel.Design.Serialization; 

    /// <devdoc>
    ///     Custom serializer for the TableLayoutPanel. We need this so we can push the TableLayoutSettings object
    ///     into the resx in localization mode. This is used by loc tools like WinRes to correctly setup the 
    ///     TableLayoutPanel with all its settings. Note that we don't serialize code to access the settings.
    /// </devdoc> 
    internal class TableLayoutPanelCodeDomSerializer : CodeDomSerializer { 
        private static readonly string LayoutSettingsPropName = "LayoutSettings";
 
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject) {
                return GetBaseSerializer(manager).Deserialize(manager, codeObject);
        }
 
        private CodeDomSerializer GetBaseSerializer(IDesignerSerializationManager manager) {
            return (CodeDomSerializer)manager.GetSerializer(typeof(TableLayoutPanel).BaseType, typeof(CodeDomSerializer)); 
        } 

        /// <devdoc> 
        ///     We don't actually want to serialize any code here, so we just delegate that to the base type's
        ///     serializer. All we want to do is if we are in a localizable form, we want to push a
        ///     'LayoutSettings' entry into the resx.
        /// </devdoc> 
        public override object Serialize(IDesignerSerializationManager manager, object value) {
            // First call the base serializer to serialize the object. 
            object codeObject = GetBaseSerializer(manager).Serialize(manager, value); 

 
            // Now push our layout settings stuff into the resx if we are not inherited read only and
            // are in a localizable Form.
            TableLayoutPanel tlp = value as TableLayoutPanel;
            Debug.Assert(tlp != null, "Huh? We were expecting to be serializing a TableLayoutPanel here."); 

            if (tlp != null) { 
                InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(tlp)[typeof(InheritanceAttribute)]; 

                if (ia == null || ia.InheritanceLevel != InheritanceLevel.InheritedReadOnly) { 
                    IDesignerHost host = (IDesignerHost)manager.GetService(typeof(IDesignerHost));
                    if (IsLocalizable(host)) {
                        PropertyDescriptor lsProp = TypeDescriptor.GetProperties(tlp)[LayoutSettingsPropName];
                        object val = (lsProp != null) ? lsProp.GetValue(tlp) : null; 
                        if (val != null) {
                            string key = manager.GetName(tlp) + "." + LayoutSettingsPropName; 
                            SerializeResourceInvariant(manager, key, val); 
                        }
                    } 
                }
            }

 
            return codeObject;
        } 
 
        private bool IsLocalizable(IDesignerHost host) {
            if (host != null) { 
                PropertyDescriptor prop = TypeDescriptor.GetProperties(host.RootComponent)["Localizable"];
                if (prop != null && prop.PropertyType == typeof(bool)) {
                     return (bool) prop.GetValue(host.RootComponent);
                } 
            }
 
            return false; 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
