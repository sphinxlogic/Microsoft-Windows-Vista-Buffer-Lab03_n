//------------------------------------------------------------------------------ 
// <copyright file="WebPartZoneBaseDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.IO; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.WebControls;
    using System.Web.UI.WebControls; 
    using System.Web.UI.WebControls.WebParts;
    using System.Xml;

    /// <devdoc> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class WebPartZoneBaseDesigner : WebZoneDesigner { 

        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(WebPartZoneBase));
            base.Initialize(component);
        }
 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 
 
            Attribute[] newAttributes = new Attribute[] {
                new BrowsableAttribute(false), 
                new EditorBrowsableAttribute(EditorBrowsableState.Never),
                new ThemeableAttribute(false),
            };
 
            string propertyName = "VerbStyle";
            PropertyDescriptor property = (PropertyDescriptor) properties[propertyName]; 
            Debug.Assert(property != null, "Property is null: " + propertyName); 
            if (property != null) {
                properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, newAttributes); 
            }
        }
    }
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WebPartZoneBaseDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.IO; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.WebControls;
    using System.Web.UI.WebControls; 
    using System.Web.UI.WebControls.WebParts;
    using System.Xml;

    /// <devdoc> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class WebPartZoneBaseDesigner : WebZoneDesigner { 

        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(WebPartZoneBase));
            base.Initialize(component);
        }
 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 
 
            Attribute[] newAttributes = new Attribute[] {
                new BrowsableAttribute(false), 
                new EditorBrowsableAttribute(EditorBrowsableState.Never),
                new ThemeableAttribute(false),
            };
 
            string propertyName = "VerbStyle";
            PropertyDescriptor property = (PropertyDescriptor) properties[propertyName]; 
            Debug.Assert(property != null, "Property is null: " + propertyName); 
            if (property != null) {
                properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, newAttributes); 
            }
        }
    }
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
