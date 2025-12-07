//------------------------------------------------------------------------------ 
// <copyright file="ToolStripCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ToolStripCodeDomSerializer..ctor()")] 
namespace System.Windows.Forms.Design {

    using System;
    using System.Design; 
    using System.CodeDom;
    using System.Collections; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;
 
    internal class ToolStripCodeDomSerializer: ControlCodeDomSerializer {
        protected override bool HasSitedNonReadonlyChildren(Control parent) { 
            ToolStrip toolStrip = parent as ToolStrip; 
            if (toolStrip == null) {
                Debug.Fail("why were we passed a non winbar?"); 
                return false;
            }
            if (toolStrip.Items.Count == 0) {
                return false; 
            }
 
            foreach (ToolStripItem item in toolStrip.Items) { 
                if (item.Site != null && toolStrip.Site != null && item.Site.Container == toolStrip.Site.Container) {
                    // We only emit Size/Location information for controls that are sited and not inherrited readonly. 
                    InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(item)[typeof(InheritanceAttribute)];
                    if (ia != null && ia.InheritanceLevel != InheritanceLevel.InheritedReadOnly) {
                        return true;
                    } 
                }
            } 
            return false; 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ToolStripCodeDomSerializer..ctor()")] 
namespace System.Windows.Forms.Design {

    using System;
    using System.Design; 
    using System.CodeDom;
    using System.Collections; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;
 
    internal class ToolStripCodeDomSerializer: ControlCodeDomSerializer {
        protected override bool HasSitedNonReadonlyChildren(Control parent) { 
            ToolStrip toolStrip = parent as ToolStrip; 
            if (toolStrip == null) {
                Debug.Fail("why were we passed a non winbar?"); 
                return false;
            }
            if (toolStrip.Items.Count == 0) {
                return false; 
            }
 
            foreach (ToolStripItem item in toolStrip.Items) { 
                if (item.Site != null && toolStrip.Site != null && item.Site.Container == toolStrip.Site.Container) {
                    // We only emit Size/Location information for controls that are sited and not inherrited readonly. 
                    InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(item)[typeof(InheritanceAttribute)];
                    if (ia != null && ia.InheritanceLevel != InheritanceLevel.InheritedReadOnly) {
                        return true;
                    } 
                }
            } 
            return false; 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
