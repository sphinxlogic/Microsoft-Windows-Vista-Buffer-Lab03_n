//------------------------------------------------------------------------------ 
// <copyright file="MailFileEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Runtime.InteropServices;
 
    using System.Diagnostics;
    using System;
    using System.Design;
    using System.IO; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Drawing.Design; 

    using System.Windows.Forms.Design;
    using System.Windows.Forms.ComponentModel;
    using System.Web.UI.Design.Util; 

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class MailFileEditor : UrlEditor { 
        protected override string Caption {
            get { 
                return SR.GetString(SR.MailFilePicker_Caption);
            }
        }
 
        protected override string Filter {
            get { 
                return SR.GetString(SR.MailFilePicker_Filter); 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="MailFileEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Runtime.InteropServices;
 
    using System.Diagnostics;
    using System;
    using System.Design;
    using System.IO; 
    using System.Collections;
    using System.ComponentModel; 
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Drawing.Design; 

    using System.Windows.Forms.Design;
    using System.Windows.Forms.ComponentModel;
    using System.Web.UI.Design.Util; 

    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class MailFileEditor : UrlEditor { 
        protected override string Caption {
            get { 
                return SR.GetString(SR.MailFilePicker_Caption);
            }
        }
 
        protected override string Filter {
            get { 
                return SR.GetString(SR.MailFilePicker_Filter); 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
