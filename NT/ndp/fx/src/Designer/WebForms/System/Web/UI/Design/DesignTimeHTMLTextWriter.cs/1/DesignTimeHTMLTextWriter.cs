//------------------------------------------------------------------------------ 
// <copyright file="DesignTimeHtmlTextWriter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.IO;
    using System.Text;
    using System.Globalization;
    using System.Security.Permissions; 
    using System.Web.UI;
    using System.Web.UI.WebControls; 
    using System.Web.Util; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    internal class DesignTimeHtmlTextWriter : System.Web.UI.HtmlTextWriter {

        public DesignTimeHtmlTextWriter(TextWriter writer) : base(writer) { 
        }
 
        public DesignTimeHtmlTextWriter(TextWriter writer, string tabString) : base(writer, tabString) { 
        }
 
        public override void AddAttribute(HtmlTextWriterAttribute key, string value) {
            if (key == HtmlTextWriterAttribute.Src
                || key == HtmlTextWriterAttribute.Href
                || key == HtmlTextWriterAttribute.Background) { 

                base.AddAttribute(key.ToString(), value, key); 
            } 
            else {
                base.AddAttribute(key, value); 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignTimeHtmlTextWriter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.IO;
    using System.Text;
    using System.Globalization;
    using System.Security.Permissions; 
    using System.Web.UI;
    using System.Web.UI.WebControls; 
    using System.Web.Util; 

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    internal class DesignTimeHtmlTextWriter : System.Web.UI.HtmlTextWriter {

        public DesignTimeHtmlTextWriter(TextWriter writer) : base(writer) { 
        }
 
        public DesignTimeHtmlTextWriter(TextWriter writer, string tabString) : base(writer, tabString) { 
        }
 
        public override void AddAttribute(HtmlTextWriterAttribute key, string value) {
            if (key == HtmlTextWriterAttribute.Src
                || key == HtmlTextWriterAttribute.Href
                || key == HtmlTextWriterAttribute.Background) { 

                base.AddAttribute(key.ToString(), value, key); 
            } 
            else {
                base.AddAttribute(key, value); 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
